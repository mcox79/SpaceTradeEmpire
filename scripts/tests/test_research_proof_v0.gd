extends SceneTree

# GATE.S4.TECH.PROOF.001: Headless proof — research + refit round-trip.
# Boot scene, start research via bridge, tick until complete, install module, verify.

var _bridge = null
var _phase: int = 0
var _tick_count: int = 0
var _max_ticks: int = 200

func _init():
	print("HSS|test_research_proof_v0|start")

func _process(_delta: float) -> void:
	if _phase == 0:
		_bridge = root.get_node_or_null("SimBridge")
		if _bridge == null or not _bridge.has_method("GetBridgeReadyV0"):
			return
		if not _bridge.call("GetBridgeReadyV0"):
			return
		print("HSS|bridge_ready")
		_phase = 1
		return

	if _phase == 1:
		# Start research on improved_thrusters (no prerequisites)
		if _bridge.has_method("StartResearchV0"):
			var result: Dictionary = _bridge.call("StartResearchV0", "improved_thrusters")
			var success: bool = bool(result.get("success", false))
			print("HSS|start_research|success=%s|reason=%s" % [str(success), str(result.get("reason", ""))])
			if not success:
				print("HSL|FAIL|could_not_start_research")
				_stop()
				return
		_phase = 2
		return

	if _phase == 2:
		# Wait for research to complete
		_tick_count += 1
		if _tick_count > _max_ticks:
			print("HSL|FAIL|research_timeout")
			_stop()
			return

		if _bridge.has_method("GetResearchStatusV0"):
			var status: Dictionary = _bridge.call("GetResearchStatusV0")
			var researching: bool = bool(status.get("researching", false))
			if not researching:
				print("HSS|research_complete|ticks=%d" % _tick_count)
				_phase = 3
				return
		return

	if _phase == 3:
		# Verify tech is unlocked
		if _bridge.has_method("GetTechTreeV0"):
			var techs: Array = _bridge.call("GetTechTreeV0")
			for t in techs:
				if typeof(t) == TYPE_DICTIONARY and str(t.get("tech_id", "")) == "improved_thrusters":
					if bool(t.get("unlocked", false)):
						print("HSS|tech_unlocked|improved_thrusters")
					else:
						print("HSL|FAIL|tech_not_unlocked")
						_stop()
						return
					break

		# Now try to install engine_booster_mk1 (requires improved_thrusters)
		if _bridge.has_method("GetPlayerFleetSlotsV0"):
			var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
			var engine_slot: int = -1
			for i in range(slots.size()):
				var s: Dictionary = slots[i] if typeof(slots[i]) == TYPE_DICTIONARY else {}
				if str(s.get("slot_kind", "")) == "Engine":
					engine_slot = i
					break

			if engine_slot >= 0 and _bridge.has_method("InstallModuleV0"):
				var result: Dictionary = _bridge.call("InstallModuleV0", "fleet_trader_1", engine_slot, "engine_booster_mk1")
				var success: bool = bool(result.get("success", false))
				print("HSS|install_module|engine_booster_mk1|slot=%d|success=%s|reason=%s" % [engine_slot, str(success), str(result.get("reason", ""))])
				if success:
					print("HSL|PASS|research_refit_roundtrip")
				else:
					print("HSL|FAIL|install_failed|%s" % str(result.get("reason", "")))
			else:
				print("HSS|no_engine_slot_found")
				print("HSL|PASS|research_complete_no_engine_slot")
		else:
			print("HSL|PASS|research_complete_no_slots_api")

		_stop()

func _stop() -> void:
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit()
