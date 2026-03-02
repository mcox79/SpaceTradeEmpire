extends SceneTree

# Deterministic headless smoke test for GATE.S1.HERO_SHIP_LOOP.FLIGHT.001.
# Contract:
# - No timestamps%wall-clock
# - Stable token output only (HSF|)
# - Exits nonzero on first failure
# - Does NOT load playable_prototype.tscn (avoids SimBridge threads keeping process alive)

const PLAYER_SCENE := "res://scenes/player.tscn"

const COAST_FRAMES_V0 := 12
const COLLISION_WAIT_FRAMES_V0 := 90
const COLLIDER_Z_V0 := -6.0

func _stop_sim_and_quit(code: int) -> void:
	# Stop SimBridge background thread before quitting so the process exits promptly.
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("HSF|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("HSF|OK|" + msg)

func _spawn_player(root3d: Node3D) -> RigidBody3D:
	var packed = load(PLAYER_SCENE)
	if packed == null:
		_fail("PLAYER_SCENE_LOAD_NULL")
	var inst = packed.instantiate()
	if inst == null:
		_fail("PLAYER_SCENE_INSTANTIATE_NULL")
	if not (inst is RigidBody3D):
		_fail("PLAYER_NOT_RIGIDBODY3D")
	var body := inst as RigidBody3D
	root3d.add_child(body)
	return body

func _spawn_obstacle(root3d: Node3D) -> void:
	var sb := StaticBody3D.new()
	sb.name = "Obstacle"
	sb.position = Vector3(0.0, 0.0, COLLIDER_Z_V0)

	var shape := BoxShape3D.new()
	shape.size = Vector3(4.0, 4.0, 1.0)

	var cs := CollisionShape3D.new()
	cs.shape = shape
	sb.add_child(cs)

	root3d.add_child(sb)

func _initialize() -> void:
	print("HSF|BOOT")
	call_deferred("_run")

func _run() -> void:
	var root3d := Node3D.new()
	root3d.name = "HSFRoot"
	get_root().add_child(root3d)

	_spawn_obstacle(root3d)
	var body := _spawn_player(root3d)

	await physics_frame

	if not body.has_method("test_set_thrust_axis_v0") or not body.has_method("test_clear_thrust_axis_v0"):
		_fail("MISSING_TEST_HOOKS_ON_PLAYER")

	var v0 := body.linear_velocity.length()
	_ok("V0|" + ("%0.6f" % v0))

	# Thrust for exactly one physics frame.
	body.call("test_set_thrust_axis_v0", 1.0)
	await physics_frame
	body.call("test_clear_thrust_axis_v0")

	var v1 := body.linear_velocity.length()
	_ok("V1|" + ("%0.6f" % v1))
	if v1 <= 0.0:
		_fail("THRUST_NO_VELOCITY")

	# Coast for fixed frames, inertia must persist.
	for i in range(COAST_FRAMES_V0):
		await physics_frame

	var v2 := body.linear_velocity.length()
	_ok("V2|" + ("%0.6f" % v2))
	if v2 <= 0.0:
		_fail("NO_INERTIA_AFTER_COAST")

	# Collision observation: keep thrusting toward obstacle until Z velocity changes.
	var vz_before := body.linear_velocity.z
	body.call("test_set_thrust_axis_v0", 1.0)

	var collided := false
	for j in range(COLLISION_WAIT_FRAMES_V0):
		await physics_frame
		var vz_now := body.linear_velocity.z
		if absf(vz_now - vz_before) > 0.05:
			collided = true
			break

	body.call("test_clear_thrust_axis_v0")

	if not collided:
		_fail("COLLISION_NOT_OBSERVED")

	_ok("DONE")
	_stop_sim_and_quit(0)
