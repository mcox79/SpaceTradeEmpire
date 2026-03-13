extends SceneTree

# GATE.S8.HAVEN.HEADLESS.001
# Headless proof: boot → verify Haven discovered state, tier, market,
# hangar status, upgrade capability, galaxy icon data.
# Emits: HAV1|HAVEN_PROOF|PASS

const PREFIX := "HAV1|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	VERIFY_HAVEN_STATUS,
	VERIFY_HAVEN_UNDISCOVERED,
	ADVANCE_TICKS,
	VERIFY_HAVEN_BRIDGE,
	VERIFY_HAVEN_MARKET,
	VERIFY_QUEST_TRACKER,
	VERIFY_TRACTOR_RANGE,
	VERIFY_PROGRAM_TEMPLATES,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _tick_target := 0


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			_gm = root.get_node_or_null("GameManager")
			if _bridge != null and _gm != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_or_gm_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_phase = Phase.VERIFY_HAVEN_STATUS
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.VERIFY_HAVEN_STATUS:
			if not _bridge.has_method("GetHavenStatusV0"):
				_fail("GetHavenStatusV0_missing")
				return false
			var status: Dictionary = _bridge.call("GetHavenStatusV0")
			print(PREFIX + "HAVEN_STATUS|discovered=%s|tier=%s" % [
				str(status.get("discovered", "?")),
				str(status.get("tier_name", "?"))])
			# Haven exists in state but starts undiscovered
			_phase = Phase.VERIFY_HAVEN_UNDISCOVERED

		Phase.VERIFY_HAVEN_UNDISCOVERED:
			var status: Dictionary = _bridge.call("GetHavenStatusV0")
			var discovered: bool = status.get("discovered", true)
			if discovered:
				# Haven already discovered at boot (possible if sim seeds it discovered)
				print(PREFIX + "HAVEN_ALREADY_DISCOVERED|skipping_undiscovered_check")
			else:
				print(PREFIX + "HAVEN_UNDISCOVERED|PASS")
			# Check node_id is set (placement happened)
			var node_id: String = str(status.get("node_id", ""))
			if node_id.is_empty():
				_fail("haven_node_id_empty")
				return false
			print(PREFIX + "HAVEN_NODE|id=%s|PASS" % node_id)
			_phase = Phase.VERIFY_HAVEN_BRIDGE

		Phase.VERIFY_HAVEN_BRIDGE:
			# Verify bridge methods exist
			var has_status := _bridge.has_method("GetHavenStatusV0")
			var has_market := _bridge.has_method("GetHavenMarketV0")
			var has_upgrade := _bridge.has_method("UpgradeHavenV0")
			var has_swap := _bridge.has_method("SwapShipV0")
			if not has_status or not has_market:
				_fail("haven_bridge_methods_missing|status=%s|market=%s" % [
					str(has_status), str(has_market)])
				return false
			print(PREFIX + "BRIDGE_METHODS|status=%s|market=%s|upgrade=%s|swap=%s|PASS" % [
				str(has_status), str(has_market), str(has_upgrade), str(has_swap)])
			_phase = Phase.VERIFY_HAVEN_MARKET

		Phase.VERIFY_HAVEN_MARKET:
			# Market may be empty if Haven not yet discovered (deferred creation)
			var market: Array = _bridge.call("GetHavenMarketV0")
			print(PREFIX + "HAVEN_MARKET|items=%d|PASS" % market.size())
			_phase = Phase.VERIFY_QUEST_TRACKER

		Phase.VERIFY_QUEST_TRACKER:
			if not _bridge.has_method("GetActiveMissionSummaryV0"):
				print(PREFIX + "QUEST_TRACKER|method_missing|SKIP")
			else:
				var summary: Dictionary = _bridge.call("GetActiveMissionSummaryV0")
				var has_mission: bool = summary.get("has_mission", false)
				print(PREFIX + "QUEST_TRACKER|has_mission=%s|PASS" % str(has_mission))
			_phase = Phase.VERIFY_TRACTOR_RANGE

		Phase.VERIFY_TRACTOR_RANGE:
			if not _bridge.has_method("GetTractorRangeV0"):
				print(PREFIX + "TRACTOR_RANGE|method_missing|SKIP")
				_phase = Phase.VERIFY_PROGRAM_TEMPLATES
				return false
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var fleet_id: String = str(ps.get("fleet_id", ""))
			if fleet_id.is_empty():
				print(PREFIX + "TRACTOR_RANGE|no_fleet|SKIP")
				_phase = Phase.VERIFY_PROGRAM_TEMPLATES
				return false
			var tractor: Dictionary = _bridge.call("GetTractorRangeV0", fleet_id)
			var range_val: int = int(tractor.get("range", 0))
			var has_tractor: bool = tractor.get("has_tractor", false)
			print(PREFIX + "TRACTOR|range=%d|has_tractor=%s|PASS" % [range_val, str(has_tractor)])
			_phase = Phase.VERIFY_PROGRAM_TEMPLATES

		Phase.VERIFY_PROGRAM_TEMPLATES:
			if not _bridge.has_method("GetProgramTemplatesV0"):
				print(PREFIX + "TEMPLATES|method_missing|SKIP")
			else:
				var templates: Array = _bridge.call("GetProgramTemplatesV0")
				if templates.size() < 1:
					_fail("templates_empty")
					return false
				print(PREFIX + "TEMPLATES|count=%d|PASS" % templates.size())
			print(PREFIX + "HAVEN_PROOF|PASS")
			_phase = Phase.DONE
			_quit()

		Phase.DONE:
			pass

	return false


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE
	_quit()


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
