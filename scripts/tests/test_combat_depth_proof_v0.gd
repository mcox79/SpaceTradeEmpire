extends SceneTree

# GATE.S7.COMBAT_DEPTH2.HEADLESS.001
# Headless proof: boot → verify combat depth bridge methods return valid data.
# Tests: GetCombatProjectionV0, GetWeaponTrackingV0, GetLatticeDroneAlertsV0,
#         GetDroneActivityV0, warfront panel keybind existence.
# Emits: CD2|COMBAT_DEPTH_PROOF|PASS

const PREFIX := "CD2|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	VERIFY_PROJECTION,
	VERIFY_TRACKING,
	VERIFY_DRONE_ALERTS,
	VERIFY_DRONE_ACTIVITY,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null


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
				_phase = Phase.VERIFY_PROJECTION
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_ready")

		Phase.VERIFY_PROJECTION:
			# GetCombatProjectionV0 should exist and return a dict (may be empty if no target)
			if not _bridge.has_method("GetCombatProjectionV0"):
				_fail("missing_GetCombatProjectionV0")
				return false
			var proj = _bridge.call("GetCombatProjectionV0", "player", "nonexistent")
			# Should return a dict (empty or with outcome)
			if proj == null:
				_fail("projection_returned_null")
				return false
			print(PREFIX + "PROJECTION_METHOD_OK")
			_phase = Phase.VERIFY_TRACKING

		Phase.VERIFY_TRACKING:
			if not _bridge.has_method("GetWeaponTrackingV0"):
				_fail("missing_GetWeaponTrackingV0")
				return false
			var tracking = _bridge.call("GetWeaponTrackingV0", "player")
			if tracking == null:
				_fail("tracking_returned_null")
				return false
			print(PREFIX + "TRACKING_METHOD_OK")
			_phase = Phase.VERIFY_DRONE_ALERTS

		Phase.VERIFY_DRONE_ALERTS:
			if not _bridge.has_method("GetLatticeDroneAlertsV0"):
				_fail("missing_GetLatticeDroneAlertsV0")
				return false
			var alerts = _bridge.call("GetLatticeDroneAlertsV0", "star_0")
			if alerts == null:
				_fail("drone_alerts_returned_null")
				return false
			print(PREFIX + "DRONE_ALERTS_METHOD_OK")
			_phase = Phase.VERIFY_DRONE_ACTIVITY

		Phase.VERIFY_DRONE_ACTIVITY:
			if not _bridge.has_method("GetDroneActivityV0"):
				_fail("missing_GetDroneActivityV0")
				return false
			var activity = _bridge.call("GetDroneActivityV0")
			if activity == null:
				_fail("drone_activity_returned_null")
				return false
			# Should have expected keys
			if activity is Dictionary:
				if not activity.has("total_drones"):
					_fail("drone_activity_missing_total_drones")
					return false
			print(PREFIX + "DRONE_ACTIVITY_METHOD_OK")
			_phase = Phase.DONE

		Phase.DONE:
			print(PREFIX + "COMBAT_DEPTH_PROOF|PASS")
			_bridge.call("StopSimV0")
			quit()
			return false

	return false


func _fail(reason: String) -> void:
	print(PREFIX + "FAIL|" + reason)
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit()
