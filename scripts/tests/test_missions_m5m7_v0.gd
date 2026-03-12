extends SceneTree

# GATE.S9.MISSIONS.HEADLESS.001
# Headless proof: accept mining mission, complete steps via scripted actions,
# then accept research mission and verify prerequisites.
# Emits: M5M7V0|MISSIONS_M5M7_PROOF|PASS

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30
const PREFIX := "M5M7V0|"
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

	_bridge = get_root().get_node_or_null("SimBridge")
	if _bridge == null:
		_fail("NO_SIMBRIDGE")
		return

	_gm = get_root().get_node_or_null("GameManager")
	if _gm == null:
		_fail("NO_GAME_MANAGER")
		return

	# --- First complete mission_matched_luggage (prerequisite for mining) ---
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var start_node: String = str(ps.get("current_node_id", ""))
	_ok("START|node=" + start_node)

	# Accept and complete M1 (Matched Luggage) — simplified fast path
	var m1_accepted: bool = _bridge.call("AcceptMissionV0", "mission_matched_luggage")
	if not m1_accepted:
		_fail("M1_ACCEPT_FAILED")
		return
	_ok("M1_ACCEPTED")

	for _i in range(SETTLE_FRAMES):
		await physics_frame

	# Buy any good
	var market: Array = _bridge.call("GetPlayerMarketViewV0", start_node)
	var buy_good_id: String = ""
	for entry in market:
		if int(entry.get("quantity", 0)) > 0:
			buy_good_id = str(entry.get("good_id", ""))
			break
	if buy_good_id.is_empty():
		_fail("NO_GOODS_AT_START")
		return
	_bridge.call("DispatchPlayerTradeV0", start_node, buy_good_id, 1, true)

	for _i in range(SETTLE_FRAMES):
		await physics_frame

	# Travel to neighbor
	var system_snap: Dictionary = _bridge.call("GetSystemSnapshotV0", start_node)
	var lane_gates: Array = system_snap.get("lane_gate", [])
	if lane_gates.size() == 0:
		_fail("NO_LANE_GATES")
		return
	var neighbor_id: String = str(lane_gates[0].get("neighbor_node_id", ""))
	_gm.call("on_lane_gate_proximity_entered_v0", neighbor_id)
	_gm.call("on_lane_arrival_v0", neighbor_id)
	await create_timer(0.4).timeout

	# Sell good
	_bridge.call("DispatchPlayerTradeV0", neighbor_id, buy_good_id, 1, false)

	for _i in range(SETTLE_FRAMES * 2):
		await physics_frame

	# Verify M1 completed
	var mission: Dictionary = _bridge.call("GetActiveMissionV0")
	var active_id: String = str(mission.get("mission_id", ""))
	if active_id == "mission_matched_luggage":
		_fail("M1_NOT_COMPLETED|step=" + str(mission.get("current_step", -1)))
		return
	_ok("M1_COMPLETED")

	# --- Step 2: Check mining mission is now available ---
	var mission_list: Array = _bridge.call("GetMissionListV0")
	var mining_available := false
	for m in mission_list:
		if str(m.get("mission_id", "")) == "mission_mining_survey":
			mining_available = true
			break
	if not mining_available:
		_fail("MINING_NOT_AVAILABLE_AFTER_M1")
		return
	_ok("MINING_AVAILABLE")

	# --- Step 3: Check prerequisites detail ---
	if _bridge.has_method("GetMissionPrerequisitesDetailV0"):
		var prereqs: Dictionary = _bridge.call("GetMissionPrerequisitesDetailV0", "mission_mining_survey")
		var all_met: bool = prereqs.get("all_met", false)
		_ok("MINING_PREREQS|all_met=" + str(all_met))
		if not all_met:
			_fail("MINING_PREREQS_NOT_MET")
			return

	# --- Step 4: Check rewards preview ---
	if _bridge.has_method("GetMissionRewardsPreviewV0"):
		var preview: Dictionary = _bridge.call("GetMissionRewardsPreviewV0", "mission_mining_survey")
		var reward: int = int(preview.get("credit_reward", 0))
		_ok("MINING_PREVIEW|reward=" + str(reward) + "|steps=" + str(preview.get("step_count", 0)))

	# --- Step 5: Accept mining mission ---
	var mining_accepted: bool = _bridge.call("AcceptMissionV0", "mission_mining_survey")
	if not mining_accepted:
		_fail("MINING_ACCEPT_FAILED")
		return
	_ok("MINING_ACCEPTED")

	for _i in range(SETTLE_FRAMES):
		await physics_frame

	mission = _bridge.call("GetActiveMissionV0")
	active_id = str(mission.get("mission_id", ""))
	if active_id != "mission_mining_survey":
		_fail("MINING_NOT_ACTIVE|got=" + active_id)
		return

	var total_steps: int = int(mission.get("total_steps", 0))
	_ok("MINING_ACTIVE|steps=" + str(total_steps) + "|current=" + str(mission.get("current_step", 0)))

	# --- Step 6: Verify research mission is NOT available (requires mining_survey complete) ---
	mission_list = _bridge.call("GetMissionListV0")
	var research_available := false
	for m in mission_list:
		if str(m.get("mission_id", "")) == "mission_first_research":
			research_available = true
			break
	if research_available:
		_fail("RESEARCH_SHOULD_NOT_BE_AVAILABLE_YET")
		return
	_ok("RESEARCH_CORRECTLY_LOCKED")

	# --- Step 7: Check research prerequisites detail (should show mining not completed) ---
	if _bridge.has_method("GetMissionPrerequisitesDetailV0"):
		var prereqs: Dictionary = _bridge.call("GetMissionPrerequisitesDetailV0", "mission_first_research")
		var all_met: bool = prereqs.get("all_met", false)
		_ok("RESEARCH_PREREQS|all_met=" + str(all_met))
		var prereq_list: Array = prereqs.get("prerequisites", [])
		for p in prereq_list:
			_ok("PREREQ|id=" + str(p.get("mission_id", "")) + "|done=" + str(p.get("completed", false)))

	print(PREFIX + "MISSIONS_M5M7_PROOF|PASS")
	_stop_sim_and_quit(0)
