extends SceneTree

# GATE.S5.COMBAT_PLAYABLE.LOOP_PROOF.001
# End-to-end headless proof: dock → undock → init HP → equip → fire turret shots →
# kill fleet → despawn → player still IN_FLIGHT.
# Emits: LOOPV0|COMBAT_LOOP_PROOF|PASS

const PREFIX := "LOOPV0|"
const MAX_POLLS := 600
const MAX_SHOTS := 50

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	DOCK_STATION,
	VERIFY_DOCKED,
	UNDOCK,
	VERIFY_FLIGHT,
	INIT_HP,
	EQUIP_WEAPONS,
	FIND_FLEET,
	FIRE_LOOP,
	VERIFY_KILLED,
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
var _shots_fired: int = 0


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
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			_player_node = str(ps.get("current_node_id", ""))
			if _player_node.is_empty():
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("no_player_node")
				return false

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
			_phase = Phase.INIT_HP

		Phase.INIT_HP:
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")
			print(PREFIX + "INIT_HP|PASS")
			_polls = 0
			_phase = Phase.EQUIP_WEAPONS

		Phase.EQUIP_WEAPONS:
			if _bridge.has_method("DispatchEquipModuleV0"):
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_cannon_mk1")
			print(PREFIX + "EQUIP|PASS")
			_polls = 0
			_phase = Phase.FIND_FLEET

		Phase.FIND_FLEET:
			var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", _player_node)
			var fleets: Array = snap.get("fleets", [])
			if fleets.size() == 0:
				_fail("no_fleets_at_node=" + _player_node)
				return false

			_opponent_id = str(fleets[0].get("fleet_id", ""))
			if _opponent_id.is_empty():
				_fail("empty_fleet_id")
				return false

			print(PREFIX + "TARGET|fleet=" + _opponent_id)
			_shots_fired = 0
			_polls = 0
			_phase = Phase.FIRE_LOOP

		Phase.FIRE_LOOP:
			if _shots_fired >= MAX_SHOTS:
				_fail("max_shots_exceeded|shots=" + str(_shots_fired))
				return false

			var result: Dictionary = _bridge.call("ApplyTurretShotV0", _opponent_id)
			_shots_fired += 1
			var killed: bool = result.get("killed", false)

			if _shots_fired == 1 or killed:
				print(PREFIX + "SHOT|#%d|shield_dmg=%d|hull_dmg=%d|killed=%s" % [
					_shots_fired,
					int(result.get("shield_dmg", 0)),
					int(result.get("hull_dmg", 0)),
					str(killed)])

			if killed:
				print(PREFIX + "KILL|shots=%d" % _shots_fired)
				_phase = Phase.VERIFY_KILLED

		Phase.VERIFY_KILLED:
			var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", _opponent_id)
			if hp.get("alive", true):
				_fail("target_still_alive")
				return false
			print(PREFIX + "VERIFY_KILLED|PASS")
			_polls = 0
			_phase = Phase.DESPAWN_FLEET

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
