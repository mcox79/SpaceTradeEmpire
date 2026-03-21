extends RigidBody3D


# Ship flight controller v2 — Freelancer-style mouse-pointer steering + cruise auto-drive.
# The ship continuously turns toward the mouse cursor's world position.
# WASD overrides pointer steering for direct manual control.
# Press C to toggle cruise (auto-drive: ship thrusts forward at normal speed while you steer with mouse).
# Cruise is for in-system travel — inter-system travel uses lane gates.

# ── Normal flight tuning ──
const THRUST_FORCE_V0: float = 60.0
const TURN_TORQUE_V0: float = 10.0
const MAX_SPEED_V0: float = 18.0
const LINEAR_DAMPING_V0: float = 1.0
const ANGULAR_DAMPING_V0: float = 5.0
const GRAVITY_SCALE_V0: float = 0.0

# ── Click-to-fly autopilot (LMB) ──
const NAV_ARRIVE_DIST: float = 8.0
const NAV_TURN_GAIN: float = 12.0

# ── Mouse-pointer steering ──
const POINTER_TURN_GAIN_V0: float = 8.0     # Steering responsiveness toward cursor.
const POINTER_DEAD_ZONE_V0: float = 3.0     # Ignore cursor within this distance of ship.

# ── Cosmetic banking ──
const BANK_ANGLE_MAX_V0: float = 0.35       # ~20 degrees max bank (radians).
const BANK_LERP_SPEED_V0: float = 5.0       # Bank animation speed.

# ── Solar wind repulsion ──
const SOLAR_REPEL_RADIUS: float = 25.0
const SOLAR_REPEL_FORCE: float = 400.0

# ── Obstacle avoidance ──
const Y_SPRING_K: float = 40.0
const Y_DAMP_K: float = 12.0
const PLANET_LIFT_FORCE: float = 300.0
const STATION_REPEL_FORCE: float = 150.0
const SHIP_SEPARATION_FORCE: float = 80.0
const SHIP_SEPARATION_RADIUS: float = 8.0

# ── State ──
var _nav_target: Vector3 = Vector3.ZERO
var _nav_active: bool = false
var _cruise_active: bool = false

# Deterministic test overrides (null means use live input).
var _test_thrust_axis_v0 = null
var _test_turn_axis_v0 = null

func _ready():
	add_to_group("Player")
	collision_layer = 2  # Ships layer.
	gravity_scale = GRAVITY_SCALE_V0
	linear_damp = LINEAR_DAMPING_V0
	angular_damp = ANGULAR_DAMPING_V0
	axis_lock_linear_y = false  # Unlocked: Y-lift avoidance.
	axis_lock_angular_x = true  # Locked: no pitch (in-system flight stays on orbital plane).
	axis_lock_angular_z = true  # Locked: no roll (cosmetic banking via ShipVisual).
	_build_player_model()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.pressed and mb.button_index == MOUSE_BUTTON_LEFT:
			_try_click_navigate(mb.position)
	# WASD cancels autopilot.
	if event is InputEventKey and event.is_pressed():
		var key := event as InputEventKey
		if key.physical_keycode in [KEY_W, KEY_A, KEY_S, KEY_D]:
			_nav_active = false
	# Cruise toggle (C key).
	if event.is_action_pressed("cruise_toggle"):
		_toggle_cruise()

func _toggle_cruise() -> void:
	_cruise_active = not _cruise_active
	if _cruise_active:
		_nav_active = false  # Cancel click-nav when engaging cruise.
		print("UUIR|CRUISE_ENGAGE")
	else:
		print("UUIR|CRUISE_DISENGAGE")

func disengage_cruise_v0() -> void:
	if _cruise_active:
		_cruise_active = false
		print("UUIR|CRUISE_DISENGAGE|auto")

func _try_click_navigate(screen_pos: Vector2) -> void:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return
	var ray_origin := camera.project_ray_origin(screen_pos)
	var ray_dir := camera.project_ray_normal(screen_pos)
	if absf(ray_dir.y) < 0.001:
		return
	var plane_y := global_position.y
	var t := (plane_y - ray_origin.y) / ray_dir.y
	if t < 0.0:
		return
	var world_pos := ray_origin + ray_dir * t
	_nav_target = world_pos
	_nav_active = true

func _get_pointer_world_pos() -> Vector3:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return global_position - global_transform.basis.z * 20.0
	var mouse_pos := get_viewport().get_mouse_position()
	var ray_origin := camera.project_ray_origin(mouse_pos)
	var ray_dir := camera.project_ray_normal(mouse_pos)
	var plane_y := global_position.y
	if absf(ray_dir.y) < 0.001:
		return global_position
	var t := (plane_y - ray_origin.y) / ray_dir.y
	if t < 0.0:
		return global_position
	return ray_origin + ray_dir * t

func _any_wasd_pressed() -> bool:
	return (Input.is_action_pressed("ship_thrust_fwd")
		or Input.is_action_pressed("ship_thrust_back")
		or Input.is_action_pressed("ship_turn_left")
		or Input.is_action_pressed("ship_turn_right"))

func _physics_process(delta):
	# Freeze input and kill momentum while docked or in lane transit.
	var gm = get_node_or_null("/root/GameManager")
	var ps = gm.get("current_player_state") if gm else 0
	var overlay_open = gm.get("galaxy_overlay_open") if gm else false
	if ps == 1 or ps == 2 or overlay_open:  # DOCKED, IN_LANE_TRANSIT, or galaxy map open
		linear_velocity = Vector3.ZERO
		angular_velocity = Vector3.ZERO
		_nav_active = false
		if _cruise_active:
			disengage_cruise_v0()
		return

	var thrust_axis: float = 0.0
	var turn_axis: float = 0.0
	var thrust_dir: Vector3 = -global_transform.basis.z

	# Priority 1: Click-to-fly autopilot (LMB target).
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
			var cross_y: float = ship_fwd.cross(target_dir).y
			turn_axis = clampf(cross_y * NAV_TURN_GAIN, -1.0, 1.0)
			var dot: float = ship_fwd.dot(target_dir)
			if dot > 0.3:
				thrust_axis = clampf(dot, 0.5, 1.0)

	# Priority 2: Test overrides (headless bots).
	if _test_thrust_axis_v0 != null:
		thrust_axis = float(_test_thrust_axis_v0)
	elif not _nav_active:
		# Priority 3: WASD manual control.
		if _any_wasd_pressed():
			if Input.is_action_pressed("ship_thrust_fwd"):
				thrust_axis += 1.0
			if Input.is_action_pressed("ship_thrust_back"):
				thrust_axis -= 1.0
		else:
			# Priority 4: Mouse-pointer steering (Freelancer-style).
			# Ship turns toward cursor. Cruise adds auto-thrust at normal speed;
			# otherwise the player drives forward with W key or LMB click-to-fly.
			var target_pos := _get_pointer_world_pos()
			var to_target := target_pos - global_position
			to_target.y = 0.0
			if to_target.length() > POINTER_DEAD_ZONE_V0:
				var target_dir := to_target.normalized()
				var ship_fwd := (-global_transform.basis.z)
				ship_fwd.y = 0.0
				ship_fwd = ship_fwd.normalized()
				var cross_y: float = ship_fwd.cross(target_dir).y
				turn_axis = clampf(cross_y * POINTER_TURN_GAIN_V0, -1.0, 1.0)
				# Cruise auto-thrust: ship drives forward at normal speed.
				if _cruise_active:
					var dot: float = ship_fwd.dot(target_dir)
					if dot > 0.0:
						thrust_axis = 1.0

	if _test_turn_axis_v0 != null:
		turn_axis = float(_test_turn_axis_v0)
	elif not _nav_active and not (_test_thrust_axis_v0 != null):
		if _any_wasd_pressed():
			turn_axis = 0.0
			if Input.is_action_pressed("ship_turn_left"):
				turn_axis += 1.0
			if Input.is_action_pressed("ship_turn_right"):
				turn_axis -= 1.0

	# ── Apply forces ──
	if thrust_axis != 0.0:
		apply_central_force(thrust_dir * (THRUST_FORCE_V0 * thrust_axis))

	if turn_axis != 0.0:
		apply_torque(Vector3(0.0, TURN_TORQUE_V0 * turn_axis, 0.0))

	# Solar wind repulsion.
	for star in get_tree().get_nodes_in_group("LocalStar"):
		var to_star: Vector3 = star.global_position - global_position
		to_star.y = 0.0
		var star_dist: float = to_star.length()
		if star_dist < SOLAR_REPEL_RADIUS and star_dist > 0.1:
			var t: float = 1.0 - star_dist / SOLAR_REPEL_RADIUS
			var strength: float = t * t * t * SOLAR_REPEL_FORCE
			apply_central_force(-to_star.normalized() * strength)

	# Obstacle avoidance.
	_apply_obstacle_avoidance(delta)

	# Clamp max speed (XZ only — preserve Y-lift velocity).
	var v: Vector3 = linear_velocity
	var xz_speed: float = Vector2(v.x, v.z).length()
	if xz_speed > MAX_SPEED_V0 and xz_speed > 0.0:
		var scale_factor: float = MAX_SPEED_V0 / xz_speed
		linear_velocity = Vector3(v.x * scale_factor, v.y, v.z * scale_factor)

	# ── Cosmetic banking ──
	var visual := get_node_or_null("ShipVisual")
	if visual:
		var target_bank: float = -turn_axis * BANK_ANGLE_MAX_V0
		visual.rotation.z = lerpf(visual.rotation.z, target_bank, BANK_LERP_SPEED_V0 * delta)


## Get the Y position of the current star system (for Y-spring reference).
## Uses GalaxyView's local system root position (already at star's galaxy-scaled Y).
var _cached_galaxy_view = null
func _get_current_system_y() -> float:
	if _cached_galaxy_view == null or not is_instance_valid(_cached_galaxy_view):
		_cached_galaxy_view = get_tree().root.find_child("GalaxyView", true, false) if get_tree() else null
	if _cached_galaxy_view and _cached_galaxy_view.has_method("GetCurrentStarGlobalPositionV0"):
		var star_pos: Vector3 = _cached_galaxy_view.call("GetCurrentStarGlobalPositionV0")
		return star_pos.y
	return 0.0

func set_nav_target_v0(world_pos: Vector3) -> void:
	_nav_target = world_pos
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


## Obstacle avoidance: Y-lift over planets, XZ repulsion around stations/ships.
func _apply_obstacle_avoidance(_delta: float = 0.0) -> void:
	# ── Y-return spring: pull ship toward current system's Y ──
	var system_y: float = _get_current_system_y()
	var y_offset: float = global_position.y - system_y
	var y_vel: float = linear_velocity.y
	apply_central_force(Vector3(0.0, -y_offset * Y_SPRING_K - y_vel * Y_DAMP_K, 0.0))

	# ── Planets: Y-lift (fly over the sphere) ──
	for planet in get_tree().get_nodes_in_group("PlanetBody"):
		var to_planet: Vector3 = planet.global_position - global_position
		to_planet.y = 0.0
		var dist: float = to_planet.length()
		var avoid_r: float = planet.get_meta("avoidance_radius", 12.0)
		if dist < avoid_r and dist > 0.1:
			var visual_r: float = planet.get_meta("visual_radius", 8.0)
			var target_y: float = visual_r + 3.0
			var t: float = 1.0 - dist / avoid_r
			var lift: float = t * t * t * PLANET_LIFT_FORCE
			var y_deficit: float = target_y - global_position.y
			if y_deficit > 0.0:
				apply_central_force(Vector3(0.0, lift * clampf(y_deficit / target_y, 0.0, 1.0), 0.0))

	# ── Stations: XZ repulsion ──
	for station in get_tree().get_nodes_in_group("Station"):
		var to_station: Vector3 = station.global_position - global_position
		to_station.y = 0.0
		var dist: float = to_station.length()
		var avoid_r: float = station.get_meta("avoidance_radius", 8.0)
		if dist < avoid_r and dist > 0.1:
			var t: float = 1.0 - dist / avoid_r
			var strength: float = t * t * t * STATION_REPEL_FORCE
			apply_central_force(-to_station.normalized() * strength)

	# ── Ship-ship separation: XZ repulsion from NPC ships ──
	for ship in get_tree().get_nodes_in_group("NpcShip"):
		var to_ship: Vector3 = ship.global_position - global_position
		to_ship.y = 0.0
		var dist: float = to_ship.length()
		if dist < SHIP_SEPARATION_RADIUS and dist > 0.1:
			var t: float = 1.0 - dist / SHIP_SEPARATION_RADIUS
			var strength: float = t * t * SHIP_SEPARATION_FORCE
			apply_central_force(-to_ship.normalized() * strength)


## Build the player ship model procedurally.
func _build_player_model() -> void:
	var visual := get_node_or_null("ShipVisual")
	if visual == null:
		return
	for child in visual.get_children():
		child.queue_free()
	var model := ShipMeshBuilder.build_ship(-1)
	visual.add_child(model)
