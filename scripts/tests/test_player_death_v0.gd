extends SceneTree

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001
# Headless proof: init HP → fire AI shots until player hull <= 0 → verify DEAD state.
# Emits: DEATHV0|PLAYER_DEATH_V0|PASS

const PREFIX := "DEATHV0|"
const MAX_POLLS := 600
const MAX_SHOTS := 200

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	INIT_HP,
	FIND_OPPONENT,
	FIRE_LOOP,
	VERIFY_DEAD,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
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
				_phase = Phase.INIT_HP
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.INIT_HP:
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")
			# Equip player with a weapon so ApplyTurretShotV0 resolves weapon damage
			if _bridge.has_method("DispatchEquipModuleV0"):
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_cannon_mk1")
			var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
			var hull_max: int = hp.get("hull_max", 0)
			if hull_max <= 0:
				_fail("init_hp_failed|hull_max=0")
				return false
			print(PREFIX + "INIT_HP|hull_max=%d|shield_max=%d" % [hull_max, hp.get("shield_max", 0)])
			_polls = 0
			_phase = Phase.FIND_OPPONENT

		Phase.FIND_OPPONENT:
			# Find any fleet at the player's current node
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))
			if node_id.is_empty():
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("no_player_node")
				return false

			var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", node_id)
			var fleets: Array = snap.get("fleets", [])
			if fleets.size() == 0:
				_fail("no_fleets_at_node=" + node_id)
				return false

			_opponent_id = str(fleets[0].get("fleet_id", ""))
			if _opponent_id.is_empty():
				_fail("empty_fleet_id")
				return false

			# Ensure opponent also has combat HP
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")

			print(PREFIX + "TARGET|fleet=" + _opponent_id)
			_shots_fired = 0
			_polls = 0
			_phase = Phase.FIRE_LOOP

		Phase.FIRE_LOOP:
			if _shots_fired >= MAX_SHOTS:
				_fail("max_shots_exceeded|shots=" + str(_shots_fired))
				return false

			# AI fleet fires at player
			var result: Dictionary = _bridge.call("ApplyAiShotAtPlayerV0", _opponent_id)
			_shots_fired += 1

			var player_hull: int = result.get("player_hull", -1)
			var killed: bool = result.get("killed", false)

			if _shots_fired == 1 or killed:
				print(PREFIX + "SHOT|#%d|shield_dmg=%d|hull_dmg=%d|player_hull=%d|killed=%s" % [
					_shots_fired,
					int(result.get("shield_dmg", 0)),
					int(result.get("hull_dmg", 0)),
					player_hull,
					str(killed)])

			if killed:
				print(PREFIX + "KILL|shots=%d" % _shots_fired)
				# Manually trigger death notification (game_manager is not ticking in headless)
				if _gm.has_method("notify_player_killed_v0"):
					_gm.call("notify_player_killed_v0")
				_phase = Phase.VERIFY_DEAD

		Phase.VERIFY_DEAD:
			# Verify game_manager registered the death
			var is_dead: bool = false
			if _gm.has_method("is_player_dead_v0"):
				is_dead = bool(_gm.call("is_player_dead_v0"))
			else:
				_fail("is_player_dead_v0_method_missing")
				return false

			if not is_dead:
				_fail("player_not_dead_after_kill")
				return false

			# Verify HP in SimBridge agrees
			var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
			var hull: int = hp.get("hull", -1)
			if hull > 0:
				_fail("simbridge_hull_still_positive|hull=%d" % hull)
				return false

			print(PREFIX + "VERIFY_DEAD|PASS|hull=%d" % hull)
			print(PREFIX + "PLAYER_DEATH_V0|PASS")
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
