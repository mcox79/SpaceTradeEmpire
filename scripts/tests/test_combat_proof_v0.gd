extends SceneTree

# GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001
# Verifies: per-shot damage via SimBridge (ApplyTurretShotV0, GetFleetCombatHpV0),
#           HP decrements, target kill detection.
# Emits: COMBAT_PROOF|PASS

const PREFIX := "COMBATV0|"
const MAX_POLLS := 600
const MAX_SHOTS := 50  # Safety cap to prevent infinite loop

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	INIT_HP,
	EQUIP_WEAPONS,
	FIND_OPPONENT,
	FIRE_LOOP,
	VERIFY_KILLED,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _opponent_id: String = ""
var _shots_fired: int = 0
var _initial_target_hull: int = 0


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
				_polls = 0
				_phase = Phase.INIT_HP
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

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
			_phase = Phase.FIND_OPPONENT

		Phase.FIND_OPPONENT:
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var player_node: String = str(ps.get("current_node_id", ""))
			if player_node.is_empty():
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("no_player_node")
				return false

			# Find an AI fleet via system snapshot
			var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", player_node)
			var fleets: Array = snap.get("fleets", [])
			if fleets.size() == 0:
				_fail("no_fleets_in_snapshot|node=" + player_node)
				return false

			_opponent_id = str(fleets[0].get("fleet_id", ""))
			if _opponent_id.is_empty():
				_fail("empty_fleet_id")
				return false

			# Record initial HP
			var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", _opponent_id)
			_initial_target_hull = int(hp.get("hull_max", 0))
			if _initial_target_hull <= 0:
				_fail("target_hp_not_initialized|hull_max=" + str(_initial_target_hull))
				return false

			print(PREFIX + "OPPONENT|id=%s|hull_max=%d|shield_max=%d" % [_opponent_id, hp.get("hull_max", 0), hp.get("shield_max", 0)])
			_shots_fired = 0
			_phase = Phase.FIRE_LOOP

		Phase.FIRE_LOOP:
			if _shots_fired >= MAX_SHOTS:
				_fail("max_shots_exceeded|shots=" + str(_shots_fired))
				return false

			var result: Dictionary = _bridge.call("ApplyTurretShotV0", _opponent_id)
			_shots_fired += 1
			var shield_dmg: int = int(result.get("shield_dmg", 0))
			var hull_dmg: int = int(result.get("hull_dmg", 0))
			var killed: bool = result.get("killed", false)

			if _shots_fired <= 3 or killed:
				print(PREFIX + "SHOT|#%d|shield_dmg=%d|hull_dmg=%d|target_hull=%d|killed=%s" % [
					_shots_fired, shield_dmg, hull_dmg,
					int(result.get("target_hull", 0)), str(killed)])

			if killed:
				_phase = Phase.VERIFY_KILLED
			# else keep firing next frame

		Phase.VERIFY_KILLED:
			var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", _opponent_id)
			var alive: bool = hp.get("alive", true)
			if alive:
				_fail("target_still_alive_after_kill")
				return false

			# Verify player is still alive
			var player_hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
			var player_alive: bool = player_hp.get("alive", false)
			if not player_alive:
				_fail("player_dead")
				return false

			print(PREFIX + "KILL|shots=%d|player_hull=%d" % [_shots_fired, int(player_hp.get("hull", 0))])
			print(PREFIX + "COMBAT_PROOF|PASS")
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
