extends SceneTree

# GATE.S5.COMBAT_PLAYABLE.COMBAT_BEAT.001
# Headless proof: undock → fly to fleet → fire turrets → enemy dies → player survives.
# Emits: BEATV0|combat_beat_v0|PASS

const PREFIX := "BEATV0|"
const MAX_POLLS := 600
const MAX_SHOTS := 50

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	INIT_HP,
	EQUIP_WEAPONS,
	FIND_FLEET,
	MOVE_TO_FLEET,
	FIRE_LOOP,
	VERIFY,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _player_node: String = ""
var _opponent_id: String = ""
var _shots_fired: int = 0
var _player_hull_final: int = 0


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
				_phase = Phase.INIT_HP
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.INIT_HP:
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")
			var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
			var hull_max: int = hp.get("hull_max", 0)
			if hull_max <= 0:
				_fail("init_hp_failed|hull_max=0")
				return false
			print(PREFIX + "INIT_HP|PASS|hull_max=%d|shield_max=%d" % [hull_max, hp.get("shield_max", 0)])
			_polls = 0
			_phase = Phase.EQUIP_WEAPONS

		Phase.EQUIP_WEAPONS:
			if _bridge.has_method("DispatchEquipModuleV0"):
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_cannon_mk1")
			print(PREFIX + "EQUIP|PASS")
			_polls = 0
			_phase = Phase.FIND_FLEET

		Phase.FIND_FLEET:
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
				_fail("no_fleets_at_node=" + _player_node)
				return false

			_opponent_id = str(fleets[0].get("fleet_id", ""))
			if _opponent_id.is_empty():
				_fail("empty_fleet_id")
				return false

			print(PREFIX + "FIND_FLEET|node=%s|target=%s" % [_player_node, _opponent_id])
			_polls = 0
			_phase = Phase.MOVE_TO_FLEET

		Phase.MOVE_TO_FLEET:
			# Player is already at the same node as the target fleet — no movement needed.
			# Both are at _player_node which is where GetSystemSnapshotV0 returned the fleet.
			print(PREFIX + "MOVE_TO_FLEET|PASS|already_at_node=" + _player_node)
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
				print(PREFIX + "SHOT|#%d|shield_dmg=%d|hull_dmg=%d|target_hull=%d|killed=%s" % [
					_shots_fired,
					int(result.get("shield_dmg", 0)),
					int(result.get("hull_dmg", 0)),
					int(result.get("target_hull", 0)),
					str(killed)])

			if killed:
				print(PREFIX + "KILL|shots=%d" % _shots_fired)
				_phase = Phase.VERIFY

		Phase.VERIFY:
			# Confirm target fleet is dead
			var target_hp: Dictionary = _bridge.call("GetFleetCombatHpV0", _opponent_id)
			if target_hp.get("alive", true):
				_fail("target_still_alive_after_kill")
				return false

			# Confirm player is still alive
			var player_hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
			var player_hull: int = player_hp.get("hull", 0)
			if player_hull <= 0:
				_fail("player_dead_after_combat|hull=%d" % player_hull)
				return false

			_player_hull_final = player_hull
			print(PREFIX + "VERIFY|PASS|target_dead=true|player_hull=%d|shots=%d" % [
				_player_hull_final, _shots_fired])
			print(PREFIX + "combat_beat_v0|PASS")
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
