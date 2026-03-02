extends RigidBody3D

# Signals expected by UI wiring (C#) in the playable prototype.
# Declared here so ConnectPlayerSignals can bind without crashing.
signal RequestDock
signal RequestUndock

# Ship flight controller v0 (deterministic, force-based).
# Tuning values are centralized here to avoid scattered magic numbers.

const THRUST_FORCE_V0: float = 55.0
const MAX_SPEED_V0: float = 28.0
const LINEAR_DAMPING_V0: float = 0.8
const ANGULAR_DAMPING_V0: float = 1.2
const GRAVITY_SCALE_V0: float = 0.0

# Deterministic test override (null means use live input).
var _test_thrust_axis_v0 = null

func _ready():
	add_to_group("Player")
	gravity_scale = GRAVITY_SCALE_V0
	linear_damp = LINEAR_DAMPING_V0
	angular_damp = ANGULAR_DAMPING_V0

func _physics_process(delta):
	var axis: float = 0.0

	if _test_thrust_axis_v0 != null:
		axis = float(_test_thrust_axis_v0)
	else:
		# Forward: ui_up, Back: ui_down (default actions).
		if Input.is_action_pressed("ui_up"):
			axis += 1.0
		if Input.is_action_pressed("ui_down"):
			axis -= 1.0

	if axis != 0.0:
		# Ship forward is -Z.
		var dir: Vector3 = -global_transform.basis.z
		apply_central_force(dir * (THRUST_FORCE_V0 * axis))

	# Clamp max speed deterministically.
	var v: Vector3 = linear_velocity
	var speed: float = v.length()
	if speed > MAX_SPEED_V0 and speed > 0.0:
		linear_velocity = v * (MAX_SPEED_V0 / speed)

func test_set_thrust_axis_v0(axis: float):
	_test_thrust_axis_v0 = axis

func test_clear_thrust_axis_v0():
	_test_thrust_axis_v0 = null
