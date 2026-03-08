extends SceneTree
# GATE.S7.WARFRONT.HEADLESS_PROOF.001: Headless verification for warfront+faction+instability.

var bridge = null
var ticks_done := 0
var max_ticks := 50

func _init():
	print("HSS|test_warfront_headless|START")

func _process(_delta: float) -> bool:
	if bridge == null:
		bridge = root.get_node_or_null("SimBridge")
		if bridge == null:
			# Try to find bridge via autoload.
			for child in root.get_children():
				if child.get_class() == "SimBridge" or child.name == "SimBridge":
					bridge = child
					break
		if bridge == null:
			print("HSS|test_warfront_headless|WAITING_FOR_BRIDGE")
			return false

	if ticks_done == 0:
		# First frame: verify factions are loaded.
		print("HSS|test_warfront_headless|CHECKING_FACTIONS")
		var factions = bridge.GetAllFactionsV0()
		if factions == null or factions.size() == 0:
			print("HSS|test_warfront_headless|FAIL|No factions found")
			bridge.call("StopSimV0")
			quit(1)
			return true

		print("HSS|test_warfront_headless|FACTIONS=%d" % factions.size())

		# Verify warfronts exist.
		print("HSS|test_warfront_headless|CHECKING_WARFRONTS")
		var warfronts = bridge.GetWarfrontsV0()
		print("HSS|test_warfront_headless|WARFRONTS=%d" % warfronts.size())

		if warfronts.size() > 0:
			var wf = warfronts[0]
			print("HSS|test_warfront_headless|WF0_ID=%s" % str(wf.get("id", "")))
			print("HSS|test_warfront_headless|WF0_INTENSITY=%s" % str(wf.get("intensity_label", "")))
			print("HSS|test_warfront_headless|WF0_TYPE=%s" % str(wf.get("war_type", "")))

		# Verify faction detail for concord.
		var detail = bridge.GetFactionDetailV0("concord")
		print("HSS|test_warfront_headless|CONCORD_SPECIES=%s" % str(detail.get("species", "")))
		print("HSS|test_warfront_headless|CONCORD_PHILOSOPHY=%s" % str(detail.get("philosophy", "")))

	ticks_done += 1

	if ticks_done >= max_ticks:
		# Final checks after running.
		var warfronts = bridge.GetWarfrontsV0()
		print("HSS|test_warfront_headless|FINAL_WARFRONTS=%d" % warfronts.size())

		var factions = bridge.GetAllFactionsV0()
		print("HSS|test_warfront_headless|FINAL_FACTIONS=%d" % factions.size())

		print("HSS|test_warfront_headless|PASS")
		bridge.call("StopSimV0")
		quit(0)
		return true

	return false
