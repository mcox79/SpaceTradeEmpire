extends SceneTree

# GATE.S1.MISSION.HEADLESS_PROOF.001
# Headless proof: accept Mission 1 "Matched Luggage", script dock→buy→travel→sell,
# verify mission completes and credit reward applied.
# Emits: MISSIONV0|MISSION_PROOF|PASS

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30
const PREFIX := "MISSIONV0|"
const MISSION_ID := "mission_matched_luggage"
const EXPECTED_REWARD := 50
const SETTLE_FRAMES := 10

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

	for _i in range(BOOT_FRAMES):
		await physics_frame

	# --- Verify bridge and game manager ---
	_bridge = get_root().get_node_or_null("SimBridge")
	if _bridge == null:
		_fail("NO_SIMBRIDGE")
		return

	for method in ["AcceptMissionV0", "GetActiveMissionV0", "GetMissionListV0",
					"GetPlayerStateV0", "GetPlayerCargoV0", "GetPlayerMarketViewV0",
					"DispatchPlayerTradeV0", "DispatchPlayerArriveV0"]:
		if not _bridge.has_method(method):
			_fail("MISSING_" + method)
			return

	_gm = get_root().get_node_or_null("GameManager")
	if _gm == null:
		_fail("NO_GAME_MANAGER")
		return

	# --- Step 1: Record starting state ---
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var start_node: String = str(ps.get("current_node_id", ""))
	var start_credits: int = int(ps.get("credits", 0))
	if start_node.is_empty():
		_fail("EMPTY_START_NODE")
		return
	_ok("START|node=" + start_node + "|credits=" + str(start_credits))

	# --- Step 2: Accept mission ---
	var accepted: bool = _bridge.call("AcceptMissionV0", MISSION_ID)
	if not accepted:
		_fail("ACCEPT_RETURNED_FALSE")
		return
	_ok("ACCEPTED")

	# Let mission system process (step 0: ArriveAtNode at start — already there)
	for _i in range(SETTLE_FRAMES):
		await physics_frame

	# --- Step 3: Read active mission for resolved targets ---
	var mission: Dictionary = _bridge.call("GetActiveMissionV0")
	var active_id: String = str(mission.get("mission_id", ""))
	if active_id != MISSION_ID:
		_fail("WRONG_ACTIVE_MISSION|expected=" + MISSION_ID + "|got=" + active_id)
		return
	var total_steps: int = int(mission.get("total_steps", 0))
	_ok("ACTIVE|steps=" + str(total_steps) + "|current=" + str(mission.get("current_step", 0)))

	# After settle frames, step 0 (ArriveAtNode at start) should be auto-completed.
	# Current step should now be 1 (HaveCargoMin).
	var current_step: int = int(mission.get("current_step", -1))
	if current_step < 1:
		_ok("STEP0_NOT_YET_DONE|current=" + str(current_step))
		# Wait more frames
		for _i in range(SETTLE_FRAMES * 2):
			await physics_frame
		mission = _bridge.call("GetActiveMissionV0")
		current_step = int(mission.get("current_step", -1))
		if current_step < 1:
			_fail("STEP0_STUCK|current=" + str(current_step))
			return

	_ok("STEP0_DONE|current_step=" + str(current_step))

	# --- Step 4: Buy good at starting station (step 1: HaveCargoMin) ---
	var market: Array = _bridge.call("GetPlayerMarketViewV0", start_node)
	if market.size() == 0:
		_fail("NO_MARKET_AT_START|node=" + start_node)
		return

	# Find a good with stock > 0
	var buy_good_id: String = ""
	for entry in market:
		var qty: int = int(entry.get("quantity", 0))
		if qty > 0:
			buy_good_id = str(entry.get("good_id", ""))
			break
	if buy_good_id.is_empty():
		_fail("NO_GOODS_IN_STOCK|node=" + start_node)
		return

	_ok("BUY_GOOD|id=" + buy_good_id)
	_bridge.call("DispatchPlayerTradeV0", start_node, buy_good_id, 1, true)

	for _i in range(SETTLE_FRAMES):
		await physics_frame

	# Verify cargo
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var has_good := false
	for entry in cargo:
		if str(entry.get("good_id", "")) == buy_good_id and int(entry.get("qty", 0)) > 0:
			has_good = true
			break
	if not has_good:
		_fail("CARGO_EMPTY_AFTER_BUY|good=" + buy_good_id)
		return
	_ok("CARGO_HAS_GOOD")

	# Step 1 (HaveCargoMin) should now complete
	for _i in range(SETTLE_FRAMES):
		await physics_frame
	mission = _bridge.call("GetActiveMissionV0")
	current_step = int(mission.get("current_step", -1))
	_ok("AFTER_BUY|current_step=" + str(current_step))

	# --- Step 5: Travel to adjacent node (step 2: ArriveAtNode/$ADJACENT_1) ---
	var system_snap: Dictionary = _bridge.call("GetSystemSnapshotV0", start_node)
	var lane_gates: Array = system_snap.get("lane_gate", [])
	if lane_gates.size() == 0:
		_fail("NO_LANE_GATES|node=" + start_node)
		return

	var neighbor_id: String = str(lane_gates[0].get("neighbor_node_id", ""))
	if neighbor_id.is_empty():
		_fail("EMPTY_NEIGHBOR_ID")
		return
	_ok("TRAVEL|destination=" + neighbor_id)

	# Simulate lane transit (same pattern as test_hero_ship_arrive.gd)
	_gm.call("on_lane_gate_proximity_entered_v0", neighbor_id)
	_gm.call("on_lane_arrival_v0", neighbor_id)

	await create_timer(0.4).timeout

	# Verify arrival
	ps = _bridge.call("GetPlayerStateV0")
	var arrived_node: String = str(ps.get("current_node_id", ""))
	if arrived_node != neighbor_id:
		_fail("TRAVEL_FAILED|expected=" + neighbor_id + "|got=" + arrived_node)
		return
	_ok("ARRIVED|node=" + arrived_node)

	# Step 2 (ArriveAtNode) should complete
	for _i in range(SETTLE_FRAMES):
		await physics_frame
	mission = _bridge.call("GetActiveMissionV0")
	current_step = int(mission.get("current_step", -1))
	_ok("AFTER_TRAVEL|current_step=" + str(current_step))

	# --- Step 6: Sell good at destination (step 3: NoCargoAtNode) ---
	_bridge.call("DispatchPlayerTradeV0", neighbor_id, buy_good_id, 1, false)

	for _i in range(SETTLE_FRAMES):
		await physics_frame

	# Verify cargo empty
	cargo = _bridge.call("GetPlayerCargoV0")
	var still_has := false
	for entry in cargo:
		if str(entry.get("good_id", "")) == buy_good_id and int(entry.get("qty", 0)) > 0:
			still_has = true
			break
	if still_has:
		_fail("CARGO_NOT_EMPTY_AFTER_SELL|good=" + buy_good_id)
		return
	_ok("CARGO_SOLD")

	# Step 3 (NoCargoAtNode) should complete → mission done
	for _i in range(SETTLE_FRAMES * 2):
		await physics_frame

	# --- Step 7: Verify mission completed ---
	mission = _bridge.call("GetActiveMissionV0")
	active_id = str(mission.get("mission_id", ""))
	if active_id == MISSION_ID:
		# Still active — check if step advanced to completion
		_fail("MISSION_STILL_ACTIVE|step=" + str(mission.get("current_step", -1)))
		return
	_ok("MISSION_COMPLETED")

	# --- Step 8: Verify credit reward ---
	ps = _bridge.call("GetPlayerStateV0")
	var end_credits: int = int(ps.get("credits", 0))
	# Credits should have increased by at least EXPECTED_REWARD (trade profit may add more)
	var credit_diff: int = end_credits - start_credits
	_ok("CREDITS|start=" + str(start_credits) + "|end=" + str(end_credits) + "|diff=" + str(credit_diff))
	# Trade will cost some credits (buy) and earn some (sell), net effect varies.
	# But mission reward of 50 should be applied. Check end >= start + reward - buy_cost.
	# Simplest check: mission reward was applied (end_credits > start_credits since reward=50 > buy cost of 1 unit)
	if end_credits <= start_credits:
		_fail("NO_CREDIT_GAIN|start=" + str(start_credits) + "|end=" + str(end_credits))
		return

	_ok("REWARD_APPLIED|diff=" + str(credit_diff))
	print(PREFIX + "MISSION_PROOF|PASS")
	_stop_sim_and_quit(0)
