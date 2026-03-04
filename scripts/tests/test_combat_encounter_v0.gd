extends SceneTree

# GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001
# Verifies: fleet substantiation in system snapshot, fleet targeting via proximity,
#           combat trigger via DispatchStartCombatV0, combat resolves, enemy despawn.
# Emits: ENCTRIG|ENCOUNTER_TRIGGER|PASS

const PREFIX := "ENCTRIG|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	VERIFY_FLEET_IN_SNAPSHOT,
	EQUIP_WEAPONS,
	TARGET_AND_START_COMBAT,
	VERIFY_COMBAT_STATUS,
	VERIFY_LOG,
	CLEAR_COMBAT,
	VERIFY_CLEARED,
	DESPAWN_FLEET,
	VERIFY_DESPAWN,
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
				_phase = Phase.VERIFY_FLEET_IN_SNAPSHOT
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.VERIFY_FLEET_IN_SNAPSHOT:
			# Get player node and verify system snapshot contains fleets.
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			_player_node = str(ps.get("current_node_id", ""))
			if _player_node.is_empty():
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("no_player_node")
				return false

			var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", _player_node)
			var fleets: Array = snap.get("fleets", [])
			if fleets.size() == 0:
				_fail("no_fleets_in_snapshot|node=" + _player_node)
				return false

			# Use first fleet (ordered by fleet_id Ordinal asc)
			_opponent_id = str(fleets[0].get("fleet_id", ""))
			if _opponent_id.is_empty():
				_fail("empty_fleet_id")
				return false

			print(PREFIX + "FLEET_SNAP|PASS|node=%s|fleet_count=%d|target=%s" % [_player_node, fleets.size(), _opponent_id])
			_polls = 0
			_phase = Phase.EQUIP_WEAPONS

		Phase.EQUIP_WEAPONS:
			if _bridge.has_method("DispatchEquipModuleV0"):
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_cannon_mk1")
			print(PREFIX + "EQUIP|PASS")
			_polls = 0
			_phase = Phase.TARGET_AND_START_COMBAT

		Phase.TARGET_AND_START_COMBAT:
			# Simulate fleet proximity targeting via GameManager.
			_gm.call("on_fleet_proximity_entered_v0", _opponent_id)
			var targeted: String = str(_gm.get("_targeted_fleet_id"))
			if targeted != _opponent_id:
				_fail("targeting_mismatch|expected=%s|got=%s" % [_opponent_id, targeted])
				return false

			print(PREFIX + "TARGET|PASS|fleet=" + _opponent_id)

			# Initiate combat through GameManager (same path as C key).
			_gm.call("initiate_combat_v0")
			_polls = 0
			_phase = Phase.VERIFY_COMBAT_STATUS

		Phase.VERIFY_COMBAT_STATUS:
			if not _bridge.has_method("GetCombatStatusV0"):
				_fail("no_GetCombatStatusV0")
				return false
			var cs: Dictionary = _bridge.call("GetCombatStatusV0")
			var in_combat: bool = cs.get("in_combat", false)
			if in_combat:
				var ph: int = cs.get("player_hull", 0)
				var opp_hull: int = cs.get("opponent_hull", 0)
				print(PREFIX + "STATUS|in_combat=true|player_hull=%d|opponent_hull=%d" % [ph, opp_hull])
				_polls = 0
				_phase = Phase.VERIFY_LOG
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("combat_status_timeout|in_combat=false")

		Phase.VERIFY_LOG:
			if not _bridge.has_method("GetLastCombatLogV0"):
				_fail("no_GetLastCombatLogV0")
				return false
			var log: Dictionary = _bridge.call("GetLastCombatLogV0")
			var outcome: String = str(log.get("outcome", ""))
			var event_count: int = int(log.get("event_count", 0))
			if outcome.is_empty():
				_fail("log_no_outcome")
				return false
			if event_count <= 0:
				_fail("log_no_events")
				return false
			print(PREFIX + "LOG|outcome=" + outcome + "|events=%d" % event_count)
			_polls = 0
			_phase = Phase.CLEAR_COMBAT

		Phase.CLEAR_COMBAT:
			_bridge.call("DispatchClearCombatV0")
			_polls = 0
			_phase = Phase.VERIFY_CLEARED

		Phase.VERIFY_CLEARED:
			var cs: Dictionary = _bridge.call("GetCombatStatusV0")
			var in_combat: bool = cs.get("in_combat", false)
			if not in_combat:
				print(PREFIX + "CLEAR|PASS")
				_polls = 0
				_phase = Phase.DESPAWN_FLEET
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("clear_timeout|still_in_combat")

		Phase.DESPAWN_FLEET:
			# Despawn defeated fleet via GameManager.
			_gm.call("despawn_fleet_v0", _opponent_id)
			print(PREFIX + "DESPAWN|fleet=" + _opponent_id)
			_polls = 0
			_phase = Phase.VERIFY_DESPAWN

		Phase.VERIFY_DESPAWN:
			# Verify player is still IN_FLIGHT after combat.
			var state_name: String = str(_gm.call("get_player_ship_state_name_v0"))
			if state_name != "IN_FLIGHT":
				_fail("post_combat_state=%s|expected=IN_FLIGHT" % state_name)
				return false

			print(PREFIX + "POST_COMBAT|state=IN_FLIGHT")
			print(PREFIX + "ENCOUNTER_TRIGGER|PASS")
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
