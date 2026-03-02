extends SceneTree

# Headless proof for GATE.S1.HERO_SHIP_LOOP.CONTROLS.001.
# Contract:
# - No timestamps / wall-clock values in output (SHA256-stable).
# - Prefix: HSC|
# - Exits nonzero on first failure.
# - Asserts angular_velocity is non-zero after turn frames (test hook injected).
# - Asserts required input actions defined in InputMap.

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30
const TURN_FRAMES := 30

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("HSC|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("HSC|OK|" + msg)

func _initialize() -> void:
	print("HSC|BOOT")
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

	# --- Verify required input actions are defined ---
	for action in ["ship_turn_left", "ship_turn_right", "ship_thrust_fwd", "ship_thrust_back"]:
		if not InputMap.has_action(action):
			_fail("MISSING_INPUT_ACTION|" + action)
			return
	_ok("input_actions_defined=true")

	# --- Find flight controller via Player group ---
	var players = get_nodes_in_group("Player")
	if players.is_empty():
		_fail("NO_PLAYER_IN_GROUP")
		return
	var controller = players[0]

	for method in ["test_set_turn_axis_v0", "test_clear_turn_axis_v0"]:
		if not controller.has_method(method):
			_fail("MISSING_" + method)
			return
	_ok("controller_methods_present=true")

	# --- Inject left-yaw turn for TURN_FRAMES ---
	controller.call("test_set_turn_axis_v0", 1.0)
	for _i in range(TURN_FRAMES):
		await physics_frame
	controller.call("test_clear_turn_axis_v0")

	# --- Assert angular velocity is non-zero ---
	var ang_vel: Vector3 = controller.angular_velocity
	_ok("angular_velocity_y=" + str(ang_vel.y))
	if ang_vel.length() < 0.001:
		_fail("ANGULAR_VELOCITY_ZERO")
		return
	_ok("angular_velocity_nonzero=true")

	_ok("DONE")
	_stop_sim_and_quit(0)
