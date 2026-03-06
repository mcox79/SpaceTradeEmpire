extends RigidBody3D


# Ship flight controller v1 (force-based thrust + yaw turning).
# Tuning values are centralized here to avoid scattered magic numbers.

const THRUST_FORCE_V0: float = 55.0
const TURN_TORQUE_V0: float = 10.0
const MAX_SPEED_V0: float = 18.0
const LINEAR_DAMPING_V0: float = 1.5
const ANGULAR_DAMPING_V0: float = 6.0
const GRAVITY_SCALE_V0: float = 0.0

# Deterministic test overrides (null means use live input).
var _test_thrust_axis_v0 = null
var _test_turn_axis_v0 = null

func _ready():
	add_to_group("Player")
	collision_layer = 2  # Ships layer — station Area3D (mask=2) detects us via body_entered.
	gravity_scale = GRAVITY_SCALE_V0
	linear_damp = LINEAR_DAMPING_V0
	angular_damp = ANGULAR_DAMPING_V0
	axis_lock_linear_y = true
	axis_lock_angular_x = true
	axis_lock_angular_z = true

func _physics_process(_delta):
	# Freeze input and kill momentum while docked or in lane transit.
	var gm = get_node_or_null("/root/GameManager")
	var ps = gm.get("current_player_state") if gm else 0
	var overlay_open = gm.get("galaxy_overlay_open") if gm else false
	if ps == 1 or ps == 2 or overlay_open:  # DOCKED, IN_LANE_TRANSIT, or galaxy map open
		linear_velocity = Vector3.ZERO
		angular_velocity = Vector3.ZERO
		return

	var thrust_axis: float = 0.0
	var turn_axis: float = 0.0

	if _test_thrust_axis_v0 != null:
		thrust_axis = float(_test_thrust_axis_v0)
	else:
		if Input.is_action_pressed("ship_thrust_fwd"):
			thrust_axis += 1.0
		if Input.is_action_pressed("ship_thrust_back"):
			thrust_axis -= 1.0

	if _test_turn_axis_v0 != null:
		turn_axis = float(_test_turn_axis_v0)
	else:
		if Input.is_action_pressed("ship_turn_left"):
			turn_axis += 1.0
		if Input.is_action_pressed("ship_turn_right"):
			turn_axis -= 1.0

	if thrust_axis != 0.0:
		# Ship forward is -Z.
		var dir: Vector3 = -global_transform.basis.z
		apply_central_force(dir * (THRUST_FORCE_V0 * thrust_axis))

	if turn_axis != 0.0:
		apply_torque(Vector3(0.0, TURN_TORQUE_V0 * turn_axis, 0.0))

	# Clamp max speed deterministically.
	var v: Vector3 = linear_velocity
	var speed: float = v.length()
	if speed > MAX_SPEED_V0 and speed > 0.0:
		linear_velocity = v * (MAX_SPEED_V0 / speed)

func test_set_thrust_axis_v0(axis: float):
	_test_thrust_axis_v0 = axis

func test_clear_thrust_axis_v0():
	_test_thrust_axis_v0 = null

func test_set_turn_axis_v0(axis: float):
	_test_turn_axis_v0 = axis

func test_clear_turn_axis_v0():
	_test_turn_axis_v0 = null
