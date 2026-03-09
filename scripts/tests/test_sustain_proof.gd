extends SceneTree

# GATE.S7.SUSTAIN.BRIDGE_PROOF.001
# Verifies: GetFleetSustainStatusV0 bridge query returns valid fuel data.
# Emits: SUSTAIN_PROOF|PASS

const PREFIX := "SUSTAIN|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	CHECK_SUSTAIN,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_phase = Phase.CHECK_SUSTAIN
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_ready")

		Phase.CHECK_SUSTAIN:
			if not _bridge.has_method("GetFleetSustainStatusV0"):
				_fail("missing_GetFleetSustainStatusV0")
				return false

			var status: Dictionary = _bridge.call("GetFleetSustainStatusV0", "fleet_trader_1")
			if status.size() == 0:
				_fail("empty_sustain_status")
				return false

			var fuel: int = int(status.get("fuel", -1))
			var fleet_id: String = str(status.get("fleet_id", ""))
			var modules: Array = status.get("modules", [])

			print(PREFIX + "fleet_id=%s fuel=%d modules=%d" % [fleet_id, fuel, modules.size()])

			if fleet_id != "fleet_trader_1":
				_fail("wrong_fleet_id:" + fleet_id)
				return false

			if fuel < 0:
				_fail("negative_fuel")
				return false

			# Sustain status query works.
			print(PREFIX + "SUSTAIN_PROOF|PASS")
			_phase = Phase.DONE
			if _bridge.has_method("StopSimV0"):
				_bridge.call("StopSimV0")
			quit(0)

		Phase.DONE:
			pass

	return false


func _fail(reason: String) -> void:
	print(PREFIX + "SUSTAIN_PROOF|FAIL|" + reason)
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(1)
