extends RigidBody3D


# Ship flight controller v1 (force-based thrust + yaw turning).
# Tuning values are centralized here to avoid scattered magic numbers.

const THRUST_FORCE_V0: float = 55.0
const TURN_TORQUE_V0: float = 10.0
const MAX_SPEED_V0: float = 18.0
const LINEAR_DAMPING_V0: float = 1.5
const ANGULAR_DAMPING_V0: float = 6.0
const GRAVITY_SCALE_V0: float = 0.0

# Click-to-fly autopilot.
const NAV_ARRIVE_DIST: float = 8.0  # Stop autopilot when within this distance.
const NAV_TURN_GAIN: float = 12.0   # Torque multiplier for autopilot steering.

# Solar wind repulsion — progressive nudge away from stars.
# Cubic falloff: gentle at edge, strong only very close. Player can graze the star
# but cannot reach the core. Force exceeds thrust only inside ~40% of the radius.
const SOLAR_REPEL_RADIUS: float = 25.0  # Distance at which gentle nudge begins.
const SOLAR_REPEL_FORCE: float = 400.0  # Max force at star surface (cubic peak).

var _nav_target: Vector3 = Vector3.ZERO
var _nav_active: bool = false

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

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.pressed and mb.button_index == MOUSE_BUTTON_LEFT:
			_try_click_navigate(mb.position)
		# Any manual input cancels autopilot.
	if event is InputEventKey and event.is_pressed():
		var key := event as InputEventKey
		if key.physical_keycode in [KEY_W, KEY_A, KEY_S, KEY_D]:
			_nav_active = false

func _try_click_navigate(screen_pos: Vector2) -> void:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return
	# Project a ray from the camera through the click position onto the Y=0 plane.
	var ray_origin := camera.project_ray_origin(screen_pos)
	var ray_dir := camera.project_ray_normal(screen_pos)
	# Intersect with Y=0 plane (all gameplay happens on XZ plane).
	if absf(ray_dir.y) < 0.001:
		return  # Ray nearly parallel to plane.
	var t := -ray_origin.y / ray_dir.y
	if t < 0.0:
		return  # Plane is behind camera.
	var world_pos := ray_origin + ray_dir * t
	world_pos.y = 0.0
	_nav_target = world_pos
	_nav_active = true

func _physics_process(_delta):
	# Freeze input and kill momentum while docked or in lane transit.
	var gm = get_node_or_null("/root/GameManager")
	var ps = gm.get("current_player_state") if gm else 0
	var overlay_open = gm.get("galaxy_overlay_open") if gm else false
	if ps == 1 or ps == 2 or overlay_open:  # DOCKED, IN_LANE_TRANSIT, or galaxy map open
		linear_velocity = Vector3.ZERO
		angular_velocity = Vector3.ZERO
		_nav_active = false
		return

	var thrust_axis: float = 0.0
	var turn_axis: float = 0.0

	# Autopilot: steer toward click target.
	if _nav_active:
		var to_target := _nav_target - global_position
		to_target.y = 0.0
		var dist := to_target.length()
		if dist < NAV_ARRIVE_DIST:
			_nav_active = false
		else:
			var target_dir := to_target.normalized()
			var ship_fwd := (-global_transform.basis.z)
			ship_fwd.y = 0.0
			ship_fwd = ship_fwd.normalized()
			# Cross product Y gives signed turn direction.
			var cross_y: float = ship_fwd.cross(target_dir).y
			turn_axis = clampf(cross_y * NAV_TURN_GAIN, -1.0, 1.0)
			# Thrust forward when roughly facing target.
			var dot: float = ship_fwd.dot(target_dir)
			if dot > 0.3:
				thrust_axis = clampf(dot, 0.5, 1.0)

	if _test_thrust_axis_v0 != null:
		thrust_axis = float(_test_thrust_axis_v0)
	elif not _nav_active:
		if Input.is_action_pressed("ship_thrust_fwd"):
			thrust_axis += 1.0
		if Input.is_action_pressed("ship_thrust_back"):
			thrust_axis -= 1.0

	if _test_turn_axis_v0 != null:
		turn_axis = float(_test_turn_axis_v0)
	elif not _nav_active:
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

	# Solar wind repulsion — progressive nudge away from nearby stars.
	# Cubic falloff: imperceptible at edge, gentle in middle, overwhelming near core.
	for star in get_tree().get_nodes_in_group("LocalStar"):
		var to_star: Vector3 = star.global_position - global_position
		to_star.y = 0.0
		var star_dist: float = to_star.length()
		if star_dist < SOLAR_REPEL_RADIUS and star_dist > 0.1:
			var t: float = 1.0 - star_dist / SOLAR_REPEL_RADIUS  # 0 at edge, 1 at center
			var strength: float = t * t * t * SOLAR_REPEL_FORCE  # Cubic: gentle nudge → hard push
			apply_central_force(-to_star.normalized() * strength)

	# Clamp max speed deterministically.
	var v: Vector3 = linear_velocity
	var speed: float = v.length()
	if speed > MAX_SPEED_V0 and speed > 0.0:
		linear_velocity = v * (MAX_SPEED_V0 / speed)

func set_nav_target_v0(world_pos: Vector3) -> void:
	_nav_target = world_pos
	_nav_target.y = 0.0
	_nav_active = true

func cancel_nav_v0() -> void:
	_nav_active = false

func test_set_thrust_axis_v0(axis: float):
	_test_thrust_axis_v0 = axis

func test_clear_thrust_axis_v0():
	_test_thrust_axis_v0 = null

func test_set_turn_axis_v0(axis: float):
	_test_turn_axis_v0 = axis

func test_clear_turn_axis_v0():
	_test_turn_axis_v0 = null
