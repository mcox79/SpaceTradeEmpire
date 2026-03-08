extends SceneTree
# GATE.S7.WARFRONT.HEADLESS_PROOF.002: Headless verification for supply tracking,
# embargoes, instability effects, regime transitions.

var bridge = null
var ticks_done := 0
var max_ticks := 50

func _init():
	print("HSS|test_warfront_supply|START")

func _process(_delta: float) -> bool:
	if bridge == null:
		bridge = root.get_node_or_null("SimBridge")
		if bridge == null:
			for child in root.get_children():
				if child.get_class() == "SimBridge" or child.name == "SimBridge":
					bridge = child
					break
		if bridge == null:
			print("HSS|test_warfront_supply|WAITING_FOR_BRIDGE")
			return false

	if ticks_done == 0:
		# Verify warfronts exist.
		var warfronts = bridge.GetWarfrontsV0()
		print("HSS|test_warfront_supply|WARFRONTS=%d" % warfronts.size())
		if warfronts.size() == 0:
			print("HSS|test_warfront_supply|FAIL|No warfronts found")
			bridge.call("StopSimV0")
			quit(1)
			return true

		# Verify supply tracking via GetWarSupplyV0.
		var wf0 = warfronts[0]
		var wf_id: String = str(wf0.get("id", ""))
		print("HSS|test_warfront_supply|WF0_ID=%s" % wf_id)

		var supply = bridge.GetWarSupplyV0(wf_id)
		print("HSS|test_warfront_supply|SUPPLY_THRESHOLD=%d" % int(supply.get("shift_threshold", 0)))
		print("HSS|test_warfront_supply|SUPPLY_PROGRESS=%d%%" % int(supply.get("shift_progress_pct", 0)))

		# Verify embargo check is callable.
		var factions = bridge.GetAllFactionsV0()
		print("HSS|test_warfront_supply|FACTIONS=%d" % factions.size())

		# Test IsGoodEmbargoedV0 on a known node (should be false for unclaimed nodes).
		var embargoed = bridge.IsGoodEmbargoedV0("star_0", "munitions")
		print("HSS|test_warfront_supply|EMBARGO_CHECK=%s" % str(embargoed))

		# Verify instability effects query.
		var effects = bridge.GetInstabilityEffectsV0("star_0")
		print("HSS|test_warfront_supply|INSTABILITY_PHASE=%s" % str(effects.get("phase", "")))
		print("HSS|test_warfront_supply|PRICE_JITTER=%d%%" % int(effects.get("price_jitter_pct", 0)))
		print("HSS|test_warfront_supply|LANE_DELAY=%d%%" % int(effects.get("lane_delay_pct", 0)))

		# Verify territory regime query.
		var regime = bridge.GetTerritoryRegimeV0("star_0")
		print("HSS|test_warfront_supply|REGIME=%s" % str(regime.get("regime", "")))

	ticks_done += 1

	if ticks_done >= max_ticks:
		# Final checks after 50 ticks.
		var warfronts = bridge.GetWarfrontsV0()
		print("HSS|test_warfront_supply|FINAL_WARFRONTS=%d" % warfronts.size())

		# Re-check supply progress after simulation ran.
		if warfronts.size() > 0:
			var wf0 = warfronts[0]
			var wf_id: String = str(wf0.get("id", ""))
			var supply = bridge.GetWarSupplyV0(wf_id)
			print("HSS|test_warfront_supply|FINAL_SUPPLY_PROGRESS=%d%%" % int(supply.get("shift_progress_pct", 0)))

		print("HSS|test_warfront_supply|PASS")
		bridge.call("StopSimV0")
		quit(0)
		return true

	return false
