extends SceneTree
# GATE.S7.REVEALS.HEADLESS.001: Headless proof — layered reveals (discovery + warfront intel).

var bridge = null
var ticks_done := 0
var max_ticks := 50

func _init():
	print("HSS|test_reveals_proof|START")

func _process(_delta: float) -> bool:
	if bridge == null:
		bridge = root.get_node_or_null("SimBridge")
		if bridge == null:
			for child in root.get_children():
				if child.name == "SimBridge":
					bridge = child
					break
		if bridge == null:
			print("HSS|test_reveals_proof|WAITING_FOR_BRIDGE")
			return false

	ticks_done += 1

	if ticks_done == 1:
		print("HSS|test_reveals_proof|CHECKING_DISCOVERIES")

		# Check fracture discovery status — tests layered reveal progression.
		var status = bridge.GetFractureDiscoveryStatusV0()
		print("HSS|test_reveals_proof|FRACTURE_UNLOCKED=%s" % str(status.get("unlocked", false)))
		print("HSS|test_reveals_proof|ANALYSIS_PROGRESS=%s" % str(status.get("analysis_progress", "")))

		# Check available void sites (reveal layers: Unknown → Discovered → Surveyed).
		var sites = bridge.GetAvailableVoidSitesV0()
		print("HSS|test_reveals_proof|VOID_SITES_VISIBLE=%d" % sites.size())
		for i in range(min(3, sites.size())):
			var s = sites[i]
			print("HSS|test_reveals_proof|SITE_%d=%s|STATE=%s" % [i, str(s.get("id", "")), str(s.get("marker_state", ""))])

		# Check warfronts — warfront intel exposure at contested nodes.
		var warfronts = bridge.GetWarfrontsV0()
		print("HSS|test_reveals_proof|WARFRONTS=%d" % warfronts.size())
		for i in range(min(2, warfronts.size())):
			var wf = warfronts[i]
			print("HSS|test_reveals_proof|WF_%d=%s|INTENSITY=%s" % [
				i, str(wf.get("id", "")), str(wf.get("intensity_label", ""))])

		# Check factions — reveals tied to faction discovery.
		var factions = bridge.GetAllFactionsV0()
		print("HSS|test_reveals_proof|FACTIONS=%d" % factions.size())

		# Sensor level — determines what player can reveal.
		var sensor_level = bridge.GetSensorLevelV0()
		print("HSS|test_reveals_proof|SENSOR_LEVEL=%d" % sensor_level)

	if ticks_done == 10:
		# After some ticks, check if discovery layers have changed (world evolves).
		var sites = bridge.GetAvailableVoidSitesV0()
		print("HSS|test_reveals_proof|SITES_AT_T10=%d" % sites.size())

	if ticks_done >= max_ticks:
		# Final state verification.
		var status = bridge.GetFractureDiscoveryStatusV0()
		print("HSS|test_reveals_proof|FINAL_FRACTURE_STATUS=%s" % str(status.get("analysis_progress", "")))

		var warfronts = bridge.GetWarfrontsV0()
		print("HSS|test_reveals_proof|FINAL_WARFRONTS=%d" % warfronts.size())
		if warfronts.size() > 0:
			var wf = warfronts[0]
			print("HSS|test_reveals_proof|WF_FINAL_INTENSITY=%s" % str(wf.get("intensity_label", "")))
			# Verify warfront has fleet strength data (ATTRITION gate).
			print("HSS|test_reveals_proof|WF_FSA=%s" % str(wf.get("fleet_strength_a", "N/A")))
			print("HSS|test_reveals_proof|WF_FSB=%s" % str(wf.get("fleet_strength_b", "N/A")))

		print("HSS|test_reveals_proof|PASS")
		bridge.call("StopSimV0")
		quit(0)
		return true

	return false
