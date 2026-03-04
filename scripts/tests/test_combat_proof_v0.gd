extends SceneTree

# GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001
# Verifies: combat dispatch via SimBridge, GetCombatStatusV0 reads in_combat,
#           GetLastCombatLogV0 has outcome, clear combat resets state.
# Emits: COMBAT_PROOF|PASS

const PREFIX := "COMBATV0|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	EQUIP_WEAPONS,
	START_COMBAT, VERIFY_COMBAT_STATUS,
	VERIFY_LOG, CLEAR_COMBAT, VERIFY_CLEARED,
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
				_polls = 0
				_phase = Phase.EQUIP_WEAPONS
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.EQUIP_WEAPONS:
			# Equip a weapon on the hero ship so combat produces damage events.
			if _bridge.has_method("DispatchEquipModuleV0"):
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_cannon_mk1")
			print(PREFIX + "EQUIP|PASS")
			_polls = 0
			_phase = Phase.START_COMBAT

		Phase.START_COMBAT:
			# Find an AI fleet at the player's current node to fight.
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var player_node: String = str(ps.get("current_node_id", ""))
			if player_node.is_empty():
				_fail("no_player_node")
				return false

			# AI fleets are named "ai_fleet_star_N" and seeded at each node.
			var opponent_id := "ai_fleet_" + player_node
			print(PREFIX + "COMBAT_START|opponent=" + opponent_id + "|node=" + player_node)
			_bridge.call("DispatchStartCombatV0", opponent_id)
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
				print(PREFIX + "COMBAT_PROOF|PASS")
				_phase = Phase.DONE
				_quit()
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("clear_timeout|still_in_combat")

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
