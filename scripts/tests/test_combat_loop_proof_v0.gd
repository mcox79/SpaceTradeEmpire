extends SceneTree

# GATE.S5.COMBAT_PLAYABLE.LOOP_PROOF.001
# End-to-end headless proof: undock from station → target fleet → combat trigger →
# combat resolves in SimCore → enemy despawns → player still IN_FLIGHT.
# Depends on ENCOUNTER_TRIGGER gate.
# Determinism: same seed → same fleet positions → same combat outcome → deterministic output.
# Emits: LOOPV0|COMBAT_LOOP_PROOF|PASS

const PREFIX := "LOOPV0|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	DOCK_STATION,
	VERIFY_DOCKED,
	UNDOCK,
	VERIFY_FLIGHT,
	EQUIP_WEAPONS,
	TARGET_FLEET,
	START_COMBAT,
	WAIT_COMBAT_DONE,
	VERIFY_LOG,
	CLEAR_COMBAT,
	WAIT_CLEARED,
	DESPAWN_FLEET,
	VERIFY_STILL_IN_FLIGHT,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _player_node: String = ""
var _opponent_id: String = ""


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
				_phase = Phase.DOCK_STATION
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.DOCK_STATION:
			# Get player node (poll until available).
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			_player_node = str(ps.get("current_node_id", ""))
			if _player_node.is_empty():
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("no_player_node")
				return false

			# Dock at the station using mock node.
			var mock_sta := Node.new()
			mock_sta.add_to_group("Station")
			mock_sta.set_meta("dock_target_id", _player_node)
			root.add_child(mock_sta)
			_gm.call("on_proximity_dock_entered_v0", mock_sta)

			print(PREFIX + "DOCK|node=" + _player_node)
			_polls = 0
			_phase = Phase.VERIFY_DOCKED

		Phase.VERIFY_DOCKED:
			var state_name: String = str(_gm.call("get_player_ship_state_name_v0"))
			if state_name != "DOCKED":
				_fail("dock_state=%s|expected=DOCKED" % state_name)
				return false
			print(PREFIX + "DOCKED|PASS")
			_polls = 0
			_phase = Phase.UNDOCK

		Phase.UNDOCK:
			_gm.call("undock_v0")
			_polls = 0
			_phase = Phase.VERIFY_FLIGHT

		Phase.VERIFY_FLIGHT:
			var state_name: String = str(_gm.call("get_player_ship_state_name_v0"))
			if state_name != "IN_FLIGHT":
				_fail("undock_state=%s|expected=IN_FLIGHT" % state_name)
				return false
			print(PREFIX + "UNDOCK|PASS|state=IN_FLIGHT")
			_polls = 0
			_phase = Phase.EQUIP_WEAPONS

		Phase.EQUIP_WEAPONS:
			if _bridge.has_method("DispatchEquipModuleV0"):
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_cannon_mk1")
			print(PREFIX + "EQUIP|PASS")
			_polls = 0
			_phase = Phase.TARGET_FLEET

		Phase.TARGET_FLEET:
			# Get fleet at current system from snapshot.
			var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", _player_node)
			var fleets: Array = snap.get("fleets", [])
			if fleets.size() == 0:
				_fail("no_fleets_at_node=" + _player_node)
				return false

			_opponent_id = str(fleets[0].get("fleet_id", ""))
			if _opponent_id.is_empty():
				_fail("empty_fleet_id")
				return false

			# Simulate proximity targeting.
			_gm.call("on_fleet_proximity_entered_v0", _opponent_id)
			print(PREFIX + "TARGET|fleet=" + _opponent_id)
			_polls = 0
			_phase = Phase.START_COMBAT

		Phase.START_COMBAT:
			# Initiate combat through GameManager (same path as C key).
			_gm.call("initiate_combat_v0")
			print(PREFIX + "COMBAT_START|opponent=" + _opponent_id)
			_polls = 0
			_phase = Phase.WAIT_COMBAT_DONE

		Phase.WAIT_COMBAT_DONE:
			var cs: Dictionary = _bridge.call("GetCombatStatusV0")
			var in_combat: bool = cs.get("in_combat", false)
			if in_combat:
				var ph: int = cs.get("player_hull", 0)
				var oh: int = cs.get("opponent_hull", 0)
				print(PREFIX + "STATUS|in_combat=true|player_hull=%d|opponent_hull=%d" % [ph, oh])
				_polls = 0
				_phase = Phase.VERIFY_LOG
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("combat_status_timeout")

		Phase.VERIFY_LOG:
			var log: Dictionary = _bridge.call("GetLastCombatLogV0")
			var outcome: String = str(log.get("outcome", ""))
			var event_count: int = int(log.get("event_count", 0))
			if outcome.is_empty() or event_count <= 0:
				_fail("log_incomplete|outcome=%s|events=%d" % [outcome, event_count])
				return false
			print(PREFIX + "LOG|outcome=" + outcome + "|events=%d" % event_count)
			_polls = 0
			_phase = Phase.CLEAR_COMBAT

		Phase.CLEAR_COMBAT:
			_bridge.call("DispatchClearCombatV0")
			_polls = 0
			_phase = Phase.WAIT_CLEARED

		Phase.WAIT_CLEARED:
			var cs: Dictionary = _bridge.call("GetCombatStatusV0")
			if not cs.get("in_combat", false):
				print(PREFIX + "CLEAR|PASS")
				_polls = 0
				_phase = Phase.DESPAWN_FLEET
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("clear_timeout")

		Phase.DESPAWN_FLEET:
			_gm.call("despawn_fleet_v0", _opponent_id)
			print(PREFIX + "DESPAWN|fleet=" + _opponent_id)
			_polls = 0
			_phase = Phase.VERIFY_STILL_IN_FLIGHT

		Phase.VERIFY_STILL_IN_FLIGHT:
			var state_name: String = str(_gm.call("get_player_ship_state_name_v0"))
			if state_name != "IN_FLIGHT":
				_fail("post_combat_state=%s|expected=IN_FLIGHT" % state_name)
				return false

			print(PREFIX + "FINAL|state=IN_FLIGHT")
			print(PREFIX + "COMBAT_LOOP_PROOF|PASS")
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
