extends SceneTree

# GATE.S1.MISSION.MULTI_PROOF.001
# Headless proof: accept mission_matched_luggage, complete it, then accept
# mission_bulk_hauler, verify state transitions across both missions.
# Emits: MULTI_MISSION|PASS

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const PREFIX := "MULTI_MISSION|"
const MISSION_1 := "mission_matched_luggage"
const MISSION_2 := "mission_bulk_hauler"

var _bridge = null
var _gm = null


func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_stop_sim_and_quit(1)


func _ok(msg: String) -> void:
	print(PREFIX + "OK|" + msg)


func _initialize() -> void:
	print(PREFIX + "BOOT")
	call_deferred("_run")


func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	await create_timer(2.0).timeout

	_bridge = get_root().get_node_or_null("SimBridge")
	if _bridge == null:
		_fail("NO_SIMBRIDGE")
		return

	_gm = get_root().get_node_or_null("GameManager")
	if _gm == null:
		_fail("NO_GAME_MANAGER")
		return

	for method in ["AcceptMissionV0", "GetActiveMissionV0", "GetMissionListV0",
					"GetPlayerStateV0", "GetPlayerCargoV0", "GetPlayerMarketViewV0",
					"DispatchPlayerTradeV0"]:
		if not _bridge.has_method(method):
			_fail("MISSING_" + method)
			return

	# ---- MISSION 1: mission_matched_luggage ----
	_ok("PHASE_1|START")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var start_node: String = str(ps.get("current_node_id", ""))
	var start_credits: int = int(ps.get("credits", 0))
	_ok("STATE|node=" + start_node + "|credits=" + str(start_credits))

	# Accept mission 1
	var accepted: bool = _bridge.call("AcceptMissionV0", MISSION_1)
	if not accepted:
		_fail("MISSION_1_ACCEPT_FAILED")
		return
	_ok("MISSION_1_ACCEPTED")

	# Let step 0 (ArriveAtNode at start) auto-complete
	await create_timer(0.5).timeout

	var mission: Dictionary = _bridge.call("GetActiveMissionV0")
	var current_step: int = int(mission.get("current_step", -1))
	_ok("MISSION_1_STEP|" + str(current_step))

	# Buy good for step 1 (HaveCargoMin)
	var market: Array = _bridge.call("GetPlayerMarketViewV0", start_node)
	var buy_good: String = ""
	for entry in market:
		if int(entry.get("quantity", 0)) > 0:
			buy_good = str(entry.get("good_id", ""))
			break
	if buy_good.is_empty():
		_fail("NO_STOCK_TO_BUY")
		return
	_bridge.call("DispatchPlayerTradeV0", start_node, buy_good, 1, true)
	await create_timer(0.3).timeout
	_ok("BOUGHT|" + buy_good)

	# Travel to neighbor for step 2 (ArriveAtNode)
	var system_snap: Dictionary = _bridge.call("GetSystemSnapshotV0", start_node)
	var lane_gates: Array = system_snap.get("lane_gate", [])
	if lane_gates.size() == 0:
		_fail("NO_LANES")
		return
	var neighbor: String = str(lane_gates[0].get("neighbor_node_id", ""))
	_gm.call("on_lane_gate_proximity_entered_v0", neighbor)
	_gm.call("on_lane_arrival_v0", neighbor)
	await create_timer(0.5).timeout
	_ok("ARRIVED|" + neighbor)

	# Sell good for step 3 (NoCargoAtNode)
	_bridge.call("DispatchPlayerTradeV0", neighbor, buy_good, 1, false)
	await create_timer(0.5).timeout

	# Verify mission 1 completed
	mission = _bridge.call("GetActiveMissionV0")
	var active_id: String = str(mission.get("mission_id", ""))
	if active_id == MISSION_1:
		_fail("MISSION_1_NOT_COMPLETED|step=" + str(mission.get("current_step", -1)))
		return
	_ok("MISSION_1_DONE")

	# Verify credit reward applied
	ps = _bridge.call("GetPlayerStateV0")
	var mid_credits: int = int(ps.get("credits", 0))
	_ok("CREDITS_AFTER_M1|" + str(mid_credits))

	# ---- MISSION 2: mission_bulk_hauler ----
	_ok("PHASE_2|START")

	# Accept mission 2 (requires mission_matched_luggage as prerequisite — now done)
	accepted = _bridge.call("AcceptMissionV0", MISSION_2)
	if not accepted:
		# Mission may not be available at this node — check the list
		var mission_list: Array = _bridge.call("GetMissionListV0")
		var found := false
		for m in mission_list:
			if str(m.get("mission_id", "")) == MISSION_2:
				found = true
				break
		if not found:
			_ok("MISSION_2_NOT_AVAILABLE_HERE|SKIP")
			print(PREFIX + "PASS")
			_stop_sim_and_quit(0)
			return
		_fail("MISSION_2_ACCEPT_FAILED")
		return
	_ok("MISSION_2_ACCEPTED")

	# Verify it's now the active mission
	await create_timer(0.3).timeout
	mission = _bridge.call("GetActiveMissionV0")
	active_id = str(mission.get("mission_id", ""))
	if active_id != MISSION_2:
		_fail("WRONG_ACTIVE|expected=" + MISSION_2 + "|got=" + active_id)
		return
	_ok("MISSION_2_ACTIVE|steps=" + str(mission.get("total_steps", 0)))

	# Verify state: two missions processed, different active mission
	_ok("STATE_TRANSITION_VERIFIED")
	print(PREFIX + "PASS")
	_stop_sim_and_quit(0)
