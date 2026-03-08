extends SceneTree

# GATE.S18.TRADE_GOODS.HEADLESS_PROOF.001
# Headless proof for 13-good trade economy through SimBridge.
# Boots game, docks at a market, buys organics and munitions, sells organics,
# verifies credits and cargo changes.
# Output: HTGP|<key=value> pairs

func _initialize():
	var packed = load("res://scenes/playable_prototype.tscn")
	var root = packed.instantiate()
	root.name = "Main"
	get_root().add_child(root)

	await process_frame
	await process_frame

	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		print("HTGP|FAIL|no_bridge")
		quit()
		return

	# Step sim a few ticks to ensure markets populated
	for _i in range(5):
		await process_frame

	var ps = bridge.call("GetPlayerStateV0")
	var credits_start: int = int(ps.get("credits", 0))
	var current_node: String = str(ps.get("current_node_id", ""))

	# Find a market with organics and munitions
	var market_node: String = current_node
	var market_view: Array = []
	var has_organics: bool = false
	var has_munitions: bool = false

	# Try current node first
	market_view = bridge.call("GetPlayerMarketViewV0", current_node)
	if typeof(market_view) == TYPE_ARRAY:
		for item in market_view:
			if typeof(item) != TYPE_DICTIONARY:
				continue
			var gid: String = str(item.get("good_id", ""))
			if gid == "organics":
				has_organics = true
			if gid == "munitions":
				has_munitions = true

	# If current node doesn't have both, search galaxy
	if not (has_organics and has_munitions):
		var galaxy_snap = bridge.call("GetGalaxySnapshotV0")
		if typeof(galaxy_snap) == TYPE_DICTIONARY and galaxy_snap.has("system_nodes"):
			for nd in galaxy_snap["system_nodes"]:
				if typeof(nd) != TYPE_DICTIONARY:
					continue
				var nid: String = str(nd.get("node_id", ""))
				if nid.is_empty():
					continue
				var mv = bridge.call("GetPlayerMarketViewV0", nid)
				if typeof(mv) != TYPE_ARRAY:
					continue
				var found_org: bool = false
				var found_mun: bool = false
				for item in mv:
					if typeof(item) != TYPE_DICTIONARY:
						continue
					var gid: String = str(item.get("good_id", ""))
					if gid == "organics":
						found_org = true
					if gid == "munitions":
						found_mun = true
				if found_org and found_mun:
					market_node = nid
					market_view = mv
					has_organics = true
					has_munitions = true
					break

	var total_goods: int = market_view.size() if typeof(market_view) == TYPE_ARRAY else 0
	var buy_organics_ok: bool = false
	var buy_munitions_ok: bool = false
	var sell_organics_ok: bool = false
	var credits_after_buy: int = credits_start
	var credits_after_sell: int = credits_start

	# Buy 1 organics
	if has_organics:
		var tick_before: int = bridge.call("GetSimTickV0")
		bridge.call("DispatchPlayerTradeV0", market_node, "organics", 1, true)
		for _i in range(500):
			await process_frame
			if bridge.call("GetSimTickV0") >= tick_before + 2:
				break
		var ps2 = bridge.call("GetPlayerStateV0")
		credits_after_buy = int(ps2.get("credits", 0))
		buy_organics_ok = (credits_after_buy < credits_start)

	# Buy 1 munitions
	if has_munitions:
		var cred_before_mun: int = credits_after_buy
		var tick_before2: int = bridge.call("GetSimTickV0")
		bridge.call("DispatchPlayerTradeV0", market_node, "munitions", 1, true)
		for _i in range(500):
			await process_frame
			if bridge.call("GetSimTickV0") >= tick_before2 + 2:
				break
		var ps3 = bridge.call("GetPlayerStateV0")
		var cred_after_mun: int = int(ps3.get("credits", 0))
		buy_munitions_ok = (cred_after_mun < cred_before_mun)
		credits_after_buy = cred_after_mun

	# Sell 1 organics
	if buy_organics_ok:
		var cred_before_sell: int = credits_after_buy
		var tick_before3: int = bridge.call("GetSimTickV0")
		bridge.call("DispatchPlayerTradeV0", market_node, "organics", 1, false)
		for _i in range(500):
			await process_frame
			if bridge.call("GetSimTickV0") >= tick_before3 + 2:
				break
		var ps4 = bridge.call("GetPlayerStateV0")
		credits_after_sell = int(ps4.get("credits", 0))
		sell_organics_ok = (credits_after_sell > cred_before_sell)

	# Check cargo
	var cargo_count: int = 0
	if bridge.has_method("GetPlayerCargoV0"):
		var cargo = bridge.call("GetPlayerCargoV0")
		if typeof(cargo) == TYPE_ARRAY:
			cargo_count = cargo.size()

	# Check economy overview
	var econ_goods: int = 0
	if bridge.has_method("GetEconomyOverviewV0"):
		var econ = bridge.call("GetEconomyOverviewV0")
		if typeof(econ) == TYPE_ARRAY:
			econ_goods = econ.size()

	# Check ship fitting
	var ship_class: String = ""
	if bridge.has_method("GetPlayerShipFittingV0"):
		var fit = bridge.call("GetPlayerShipFittingV0")
		if typeof(fit) == TYPE_DICTIONARY:
			ship_class = str(fit.get("ship_class", ""))

	print("HTGP|total_goods=%d|has_organics=%s|has_munitions=%s|buy_organics=%s|buy_munitions=%s|sell_organics=%s|cargo=%d|econ_goods=%d|ship_class=%s|credits_start=%d|credits_end=%d" % [
		total_goods,
		str(has_organics).to_lower(),
		str(has_munitions).to_lower(),
		str(buy_organics_ok).to_lower(),
		str(buy_munitions_ok).to_lower(),
		str(sell_organics_ok).to_lower(),
		cargo_count,
		econ_goods,
		ship_class,
		credits_start,
		credits_after_sell
	])

	if bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit()
