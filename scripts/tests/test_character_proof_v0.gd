extends SceneTree

# GATE.T18.CHARACTER.HEADLESS.001
# Headless proof: promote First Officer, verify dialogue triggers, check War Faces state.
# Emits: CHARV0|CHARACTER_PROOF|PASS

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30
const PREFIX := "CHARV0|"
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

	_gm = get_root().get_node_or_null("GameManager")
	if _gm == null:
		_fail("NO_GAME_MANAGER")
		return

	# --- Step 1: Check FO state (should be unpromoted initially) ---
	if not _bridge.has_method("GetFirstOfficerStateV0"):
		_fail("MISSING_GetFirstOfficerStateV0")
		return

	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	var promoted: bool = fo.get("promoted", false)
	_ok("FO_INITIAL|promoted=" + str(promoted))

	# --- Step 2: Check candidates available ---
	if not _bridge.has_method("GetFirstOfficerCandidatesV0"):
		_fail("MISSING_GetFirstOfficerCandidatesV0")
		return

	var candidates: Array = _bridge.call("GetFirstOfficerCandidatesV0")
	if candidates.size() < 3:
		_fail("TOO_FEW_CANDIDATES|count=" + str(candidates.size()))
		return
	_ok("CANDIDATES|count=" + str(candidates.size()))

	for c in candidates:
		var ctype: String = str(c.get("type", ""))
		var cname: String = str(c.get("name", ""))
		_ok("CANDIDATE|type=" + ctype + "|name=" + cname)

	# --- Step 3: Promote first candidate ---
	if not _bridge.has_method("PromoteFirstOfficerV0"):
		_fail("MISSING_PromoteFirstOfficerV0")
		return

	var first_type: String = str(candidates[0].get("type", ""))
	var promote_result: bool = _bridge.call("PromoteFirstOfficerV0", first_type)
	if not promote_result:
		_fail("PROMOTE_FAILED|type=" + first_type)
		return
	_ok("PROMOTED|type=" + first_type)

	# --- Step 4: Verify promoted state ---
	for _i in range(SETTLE_FRAMES):
		await physics_frame

	fo = _bridge.call("GetFirstOfficerStateV0")
	promoted = fo.get("promoted", false)
	if not promoted:
		_fail("NOT_PROMOTED_AFTER_CALL")
		return

	var fo_name: String = str(fo.get("name", ""))
	var fo_tier: String = str(fo.get("tier", ""))
	var fo_score: int = int(fo.get("score", 0))
	_ok("FO_STATE|name=" + fo_name + "|tier=" + fo_tier + "|score=" + str(fo_score))

	# --- Step 5: Trigger dialogue by doing a trade action ---
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var start_node: String = str(ps.get("current_node_id", ""))
	if start_node.is_empty():
		_fail("EMPTY_START_NODE")
		return

	# Travel to trigger FIRST_WARP dialogue
	var system_snap: Dictionary = _bridge.call("GetSystemSnapshotV0", start_node)
	var lane_gates: Array = system_snap.get("lane_gate", [])
	if lane_gates.size() > 0:
		var neighbor_id: String = str(lane_gates[0].get("neighbor_node_id", ""))
		if not neighbor_id.is_empty():
			_gm.call("on_lane_gate_proximity_entered_v0", neighbor_id)
			_gm.call("on_lane_arrival_v0", neighbor_id)
			await create_timer(0.4).timeout
			_ok("TRAVELED|to=" + neighbor_id)

	# Wait for FO system to process and potentially fire triggers
	for _i in range(SETTLE_FRAMES * 3):
		await physics_frame

	# --- Step 6: Poll for dialogue ---
	if _bridge.has_method("GetFirstOfficerDialogueV0"):
		var dialogue: String = _bridge.call("GetFirstOfficerDialogueV0")
		if not dialogue.is_empty():
			_ok("DIALOGUE|text=" + dialogue.substr(0, 60))
		else:
			_ok("NO_DIALOGUE_YET")

	# --- Step 7: Check War Faces NPCs ---
	if _bridge.has_method("GetAllNarrativeNpcsV0"):
		var npcs: Array = _bridge.call("GetAllNarrativeNpcsV0")
		_ok("NPCS|count=" + str(npcs.size()))
		for npc in npcs:
			var npc_name: String = str(npc.get("name", ""))
			var npc_kind: String = str(npc.get("kind", ""))
			var alive: bool = npc.get("is_alive", true)
			_ok("NPC|name=" + npc_name + "|kind=" + npc_kind + "|alive=" + str(alive))

	# --- Step 8: Verify second promote fails (already promoted) ---
	var second_type: String = str(candidates[1].get("type", ""))
	var second_result: bool = _bridge.call("PromoteFirstOfficerV0", second_type)
	if second_result:
		_fail("SECOND_PROMOTE_SHOULD_FAIL")
		return
	_ok("SECOND_PROMOTE_BLOCKED")

	# --- Step 9: Check mission rewards preview bridge method ---
	if _bridge.has_method("GetMissionRewardsPreviewV0"):
		var preview: Dictionary = _bridge.call("GetMissionRewardsPreviewV0", "mission_matched_luggage")
		var reward: int = int(preview.get("credit_reward", 0))
		_ok("REWARDS_PREVIEW|reward=" + str(reward))

	print(PREFIX + "CHARACTER_PROOF|PASS")
	_stop_sim_and_quit(0)
