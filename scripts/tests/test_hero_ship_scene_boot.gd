extends SceneTree

# Headless local system boot proof for GATE.S1.HERO_SHIP_LOOP.SCENE.001.
# Contract:
# - No timestamps / wall-clock
# - Stable token output only (HSB|)
# - Exits nonzero on first failure
# - Proves local system interior renders: star, station, lane gates, ship spawn valid

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 60  # 30 normal boot + 30 for DrawLocalSystemBootV0 deferred call

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("HSB|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("HSB|OK|" + msg)

func _initialize() -> void:
	print("HSB|BOOT")
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

	# --- Verify star present ---
	var star_nodes = get_nodes_in_group("LocalStar")
	var star_present := star_nodes.size() > 0
	_ok("star_present=" + str(star_present))
	if not star_present:
		_fail("NO_STAR_IN_LOCAL_SYSTEM")
		return

	# --- Verify station present ---
	var station_nodes = get_nodes_in_group("Station")
	var station_count := station_nodes.size()
	_ok("station_count=" + str(station_count))
	if station_count < 1:
		_fail("NO_STATION_IN_LOCAL_SYSTEM")
		return

	# --- Verify lane gate present ---
	var lane_gate_nodes = get_nodes_in_group("LaneGate")
	var lane_gate_count := lane_gate_nodes.size()
	_ok("lane_gate_count=" + str(lane_gate_count))
	if lane_gate_count < 1:
		_fail("NO_LANE_GATE_IN_LOCAL_SYSTEM")
		return

	# --- Verify ship spawn valid ---
	var player = inst.get_node_or_null("Player")
	var ship_spawn_valid := player != null and player is RigidBody3D
	_ok("ship_spawn_valid=" + str(ship_spawn_valid))
	if not ship_spawn_valid:
		_fail("PLAYER_NOT_RIGIDBODY3D")
		return

	_ok("DONE")
	_stop_sim_and_quit(0)
