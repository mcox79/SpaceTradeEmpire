extends SceneTree

# GATE.S7.INFRA.HEADLESS_PROOF.001
# Headless proof: rep tiers, territory regimes, fracture travel infrastructure.
# Verifies all tranche 18 bridge methods are callable, return correct types,
# and handle fresh-game state gracefully.
# Output: HFIP|<key=value> pairs

func _initialize():
	var packed = load("res://scenes/playable_prototype.tscn")
	var root = packed.instantiate()
	root.name = "Main"
	get_root().add_child(root)

	await process_frame
	await process_frame

	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		print("HFIP|FAIL|no_bridge")
		quit()
		return

	# Wait for sim to start ticking
	var tick0: int = bridge.call("GetSimTickV0")
	for _i in range(300):
		await process_frame
		if bridge.call("GetSimTickV0") > tick0 + 2:
			break

	for _j in range(20):
		await process_frame

	var ps = bridge.call("GetPlayerStateV0")
	var current_node: String = str(ps.get("current_node_id", ""))
	var pass_count: int = 0
	var fail_count: int = 0
	var checks: Array = []

	# --- Check 1: GetAllFactionsV0 returns Array ---
	var factions_ok: bool = false
	var faction_count: int = 0
	if bridge.has_method("GetAllFactionsV0"):
		var factions = bridge.call("GetAllFactionsV0")
		if typeof(factions) == TYPE_ARRAY:
			factions_ok = true
			faction_count = factions.size()
	if factions_ok:
		pass_count += 1
		checks.append("PASS:GetAllFactionsV0")
	else:
		fail_count += 1
		checks.append("FAIL:GetAllFactionsV0")

	# --- Check 2: GetPlayerReputationV0 returns Dict with label ---
	var rep_ok: bool = false
	var rep_tier: String = ""
	if bridge.has_method("GetPlayerReputationV0"):
		var rep = bridge.call("GetPlayerReputationV0", "test_faction")
		if typeof(rep) == TYPE_DICTIONARY and rep.has("label") and rep.has("reputation"):
			rep_ok = true
			rep_tier = str(rep.get("label", ""))
	if rep_ok:
		pass_count += 1
		checks.append("PASS:GetPlayerReputationV0(tier=%s)" % rep_tier)
	else:
		fail_count += 1
		checks.append("FAIL:GetPlayerReputationV0")

	# --- Check 3: GetTerritoryAccessV0 returns Dict with rep_tier + price_modifier_bps ---
	var access_ok: bool = false
	var access_rep_tier: String = ""
	var access_price_bps: int = 0
	if bridge.has_method("GetTerritoryAccessV0") and not current_node.is_empty():
		var access = bridge.call("GetTerritoryAccessV0", current_node)
		if typeof(access) == TYPE_DICTIONARY and access.has("rep_tier") and access.has("price_modifier_bps"):
			access_ok = true
			access_rep_tier = str(access.get("rep_tier", ""))
			access_price_bps = int(access.get("price_modifier_bps", 0))
	if access_ok:
		pass_count += 1
		checks.append("PASS:GetTerritoryAccessV0(tier=%s,bps=%d)" % [access_rep_tier, access_price_bps])
	else:
		fail_count += 1
		checks.append("FAIL:GetTerritoryAccessV0")

	# --- Check 4: GetTerritoryRegimeV0 returns Dict with regime ---
	var regime_ok: bool = false
	var regime: String = ""
	if bridge.has_method("GetTerritoryRegimeV0") and not current_node.is_empty():
		var reg = bridge.call("GetTerritoryRegimeV0", current_node)
		if typeof(reg) == TYPE_DICTIONARY and reg.has("regime") and reg.has("regime_color"):
			regime_ok = true
			regime = str(reg.get("regime", ""))
	if regime_ok:
		pass_count += 1
		checks.append("PASS:GetTerritoryRegimeV0(regime=%s)" % regime)
	else:
		fail_count += 1
		checks.append("FAIL:GetTerritoryRegimeV0")

	# --- Check 5: GetFractureAccessV0 returns Dict with allowed + reason ---
	var fracture_ok: bool = false
	if bridge.has_method("GetFractureAccessV0") and not current_node.is_empty():
		var fac = bridge.call("GetFractureAccessV0", "fleet_trader_1", current_node)
		if typeof(fac) == TYPE_DICTIONARY and fac.has("allowed") and fac.has("reason"):
			fracture_ok = true
	if fracture_ok:
		pass_count += 1
		checks.append("PASS:GetFractureAccessV0")
	else:
		fail_count += 1
		checks.append("FAIL:GetFractureAccessV0")

	# --- Check 6: GetAvailableVoidSitesV0 returns Array ---
	var void_ok: bool = false
	var void_count: int = 0
	if bridge.has_method("GetAvailableVoidSitesV0"):
		var sites = bridge.call("GetAvailableVoidSitesV0")
		if typeof(sites) == TYPE_ARRAY:
			void_ok = true
			void_count = sites.size()
	if void_ok:
		pass_count += 1
		checks.append("PASS:GetAvailableVoidSitesV0(sites=%d)" % void_count)
	else:
		fail_count += 1
		checks.append("FAIL:GetAvailableVoidSitesV0")

	# --- Check 7: GetFactionDoctrineV0 returns Dict ---
	var doctrine_ok: bool = false
	if bridge.has_method("GetFactionDoctrineV0"):
		var doc = bridge.call("GetFactionDoctrineV0", "test_faction")
		if typeof(doc) == TYPE_DICTIONARY and doc.has("trade_policy") and doc.has("tariff_rate"):
			doctrine_ok = true
	if doctrine_ok:
		pass_count += 1
		checks.append("PASS:GetFactionDoctrineV0")
	else:
		fail_count += 1
		checks.append("FAIL:GetFactionDoctrineV0")

	# --- Check 8: DispatchFractureTravelV0 method exists ---
	var dispatch_ok: bool = bridge.has_method("GetFractureMarketV0")
	if dispatch_ok:
		pass_count += 1
		checks.append("PASS:GetFractureMarketV0_exists")
	else:
		fail_count += 1
		checks.append("FAIL:GetFractureMarketV0_exists")

	# --- Check 9: GetFactionMapV0 returns Array with faction data ---
	var fmap_ok: bool = false
	var fmap_count: int = 0
	if bridge.has_method("GetFactionMapV0"):
		var fmap = bridge.call("GetFactionMapV0")
		if typeof(fmap) == TYPE_ARRAY:
			fmap_ok = true
			fmap_count = fmap.size()
	if fmap_ok:
		pass_count += 1
		checks.append("PASS:GetFactionMapV0(factions=%d)" % fmap_count)
	else:
		fail_count += 1
		checks.append("FAIL:GetFactionMapV0")

	# --- Check 10: DispatchFractureTravelV0 exists ---
	var dispatch_fracture_ok: bool = bridge.has_method("DispatchFractureTravelV0")
	if dispatch_fracture_ok:
		pass_count += 1
		checks.append("PASS:DispatchFractureTravelV0_exists")
	else:
		fail_count += 1
		checks.append("FAIL:DispatchFractureTravelV0_exists")

	# --- Summary ---
	var status: String = "PASS" if fail_count == 0 else "FAIL"
	print("HFIP|status=%s|pass=%d|fail=%d|factions=%d|fmap_factions=%d|checks=%s" % [
		status,
		pass_count,
		fail_count,
		faction_count,
		fmap_count,
		"|".join(checks)
	])

	if bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit()
