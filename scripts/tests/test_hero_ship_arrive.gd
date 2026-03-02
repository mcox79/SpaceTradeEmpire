extends SceneTree

# Headless proof for GATE.S1.HERO_SHIP_LOOP.ARRIVE.001.
# Contract:
# - No timestamps / wall-clock values in output (SHA256-stable).
# - Prefix: HSA|
# - Exits nonzero on first failure.
# - Asserts PlayerLocationNodeId matches destination after on_lane_arrival_v0.
# - Asserts LocalStar/Station/LaneGate groups non-zero via GalaxyView.GetLocalSystemMetricsV0.

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("HSA|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("HSA|OK|" + msg)

func _initialize() -> void:
	print("HSA|BOOT")
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

	# --- Verify required nodes and methods ---
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("NO_SIMBRIDGE")
		return

	for method in ["GetGalaxySnapshotV0", "GetSystemSnapshotV0", "DispatchPlayerArriveV0", "DispatchTravelCommandV0"]:
		if not bridge.has_method(method):
			_fail("MISSING_" + method)
			return

	var gm = get_root().get_node_or_null("GameManager")
	if gm == null:
		_fail("NO_GAME_MANAGER")
		return

	for method in ["on_lane_gate_proximity_entered_v0", "on_lane_arrival_v0"]:
		if not gm.has_method(method):
			_fail("MISSING_" + method)
			return

	var galaxy_view = get_root().get_node_or_null("Main/GalaxyView")
	if galaxy_view == null:
		_fail("NO_GALAXY_VIEW")
		return
	if not galaxy_view.has_method("GetLocalSystemMetricsV0"):
		_fail("MISSING_GetLocalSystemMetricsV0")
		return

	# --- Step 1: get current player node ---
	var galaxy_snap = bridge.call("GetGalaxySnapshotV0")
	if galaxy_snap == null or not galaxy_snap.has("player_current_node_id"):
		_fail("NO_GALAXY_SNAPSHOT")
		return
	var start_node_id: String = galaxy_snap["player_current_node_id"]
	if start_node_id == "":
		_fail("EMPTY_START_NODE_ID")
		return
	_ok("START_NODE_ID|" + start_node_id)

	# --- Step 2: find a neighbor to transit to ---
	var system_snap = bridge.call("GetSystemSnapshotV0", start_node_id)
	if system_snap == null or not system_snap.has("lane_gate"):
		_fail("NO_SYSTEM_SNAPSHOT")
		return
	var lane_gates = system_snap["lane_gate"]
	if lane_gates.size() == 0:
		_fail("NO_LANE_GATES")
		return
	var neighbor_id: String = lane_gates[0]["neighbor_node_id"]
	_ok("NEIGHBOR_NODE_ID|" + neighbor_id)

	# --- Step 3: local system groups non-zero at boot (before transit) ---
	var metrics_before = galaxy_view.call("GetLocalSystemMetricsV0")
	_ok("local_star_count=" + str(metrics_before["star_count"]))
	_ok("station_count=" + str(metrics_before["station_count"]))
	_ok("lane_gate_count=" + str(metrics_before["lane_gate_count"]))
	if metrics_before["star_count"] <= 0:
		_fail("LOCAL_STAR_COUNT_ZERO")
		return
	if metrics_before["station_count"] <= 0:
		_fail("STATION_COUNT_ZERO")
		return
	if metrics_before["lane_gate_count"] <= 0:
		_fail("LANE_GATE_COUNT_ZERO")
		return

	# --- Step 4: enter lane transit ---
	gm.call("on_lane_gate_proximity_entered_v0", neighbor_id)

	# --- Step 5: complete lane transit; on_lane_arrival_v0 dispatches PlayerArriveCommand ---
	gm.call("on_lane_arrival_v0", neighbor_id)

	# --- Step 6: wait for SimCore to process PlayerArriveCommand ---
	await create_timer(0.4).timeout

	# --- Step 7: assert player_current_node_id == destination ---
	var snap_after = bridge.call("GetGalaxySnapshotV0")
	if snap_after == null or not snap_after.has("player_current_node_id"):
		_fail("NO_GALAXY_SNAPSHOT_AFTER")
		return
	var arrived_node_id: String = snap_after["player_current_node_id"]
	_ok("ARRIVED_NODE_ID|" + arrived_node_id)
	if arrived_node_id != neighbor_id:
		_fail("NODE_MISMATCH|expected=" + neighbor_id + "|got=" + arrived_node_id)
		return
	_ok("node_id_matches_destination=true")

	_ok("DONE")
	_stop_sim_and_quit(0)
