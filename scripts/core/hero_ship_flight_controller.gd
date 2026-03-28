extends RigidBody3D

const ShipMeshBuilder = preload("res://scripts/view/ship_mesh_builder.gd")

# Ship flight controller v3 — dual steering modes + combat-aware LMB.
#
# Pattern A "Pointer Flight" (default):
#   Ship faces mouse cursor. WASD = thrust/strafe. A/D = lateral strafe.
#   Click-to-fly: LMB sets autopilot waypoint (suppressed if hostiles in range).
#   Cruise (C): auto-thrust toward cursor.
#
# Pattern B "Manual Flight":
#   A/D rotate ship (tank controls). Mouse aims weapons only — no pointer steering.
#   W/S = thrust fwd/back. A/D = turn.
#   LMB = fire primary when hostiles in range, else click-to-fly.
#
# Steering mode is read from InputManager.steering_mode each frame.

# ── Normal flight tuning ──
const THRUST_FORCE_V0: float = 60.0
const STRAFE_FORCE_V0: float = 45.0    # Strafe is weaker than forward thrust.
const TURN_TORQUE_V0: float = 10.0
const MAX_SPEED_V0: float = 18.0
const LINEAR_DAMPING_V0: float = 1.0
const ANGULAR_DAMPING_V0: float = 5.0
const GRAVITY_SCALE_V0: float = 0.0

# ── Click-to-fly autopilot (LMB) ──
const NAV_ARRIVE_DIST: float = 8.0
const NAV_TURN_GAIN: float = 12.0

# ── Mouse-pointer steering (Pattern A only) ──
const POINTER_TURN_GAIN_V0: float = 8.0     # Steering responsiveness toward cursor.
const POINTER_DEAD_ZONE_V0: float = 3.0     # Ignore cursor within this distance of ship.

# ── Gradient deadzone ──
const STICK_DEADZONE: float = 0.2

# ── Cosmetic banking ──
const BANK_ANGLE_MAX_V0: float = 0.35       # ~20 degrees max bank (radians).
const BANK_LERP_SPEED_V0: float = 5.0       # Bank animation speed.

# GATE.T60.SPIN.VISUAL.001: Visual spin rotation speed (rad/s per RPM unit).
const SPIN_VISUAL_RAD_PER_RPM: float = 0.05
# GATE.T60.SPIN.TURN_FEEL.001: Turn penalty per RPM unit (fraction reduction).
const SPIN_TURN_PENALTY_PER_RPM: float = 0.015  # At 20 RPM → 30% turn reduction.

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

# Cached references (resolved once, reused each frame).
var _bridge: Object = null
var _input_manager: Node = null

# GATE.T60.SPIN: Cached spin RPM from bridge (updated each frame).
var _spin_rpm: int = 0
var _spin_visual_angle: float = 0.0  # Accumulates for visual rotation.
var _prev_spin_state: String = "StandDown"  # For detecting state changes (camera shake).

# GATE.T60.SPIN.AUDIO_VFX.001: Gyro whine audio + running light color.
var _gyro_audio: AudioStreamPlayer3D = null
const GYRO_BASE_PITCH: float = 0.5      # Pitch at lowest RPM.
const GYRO_MAX_PITCH: float = 2.0       # Pitch at full RPM.
const GYRO_MAX_VOLUME_DB: float = -8.0   # Volume at full RPM.
const GYRO_OFF_VOLUME_DB: float = -40.0  # Effectively silent.
var _light_default_colors: Array = []    # Original NavLight emission colors.
var _nav_lights: Array = []              # Cached NavLight MeshInstance3D refs.
const SPIN_LIGHT_RED := Color(1.0, 0.15, 0.1)

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
	_setup_gyro_audio()
	_cache_nav_lights()


func _ensure_refs() -> void:
	if _bridge == null:
		var gm = get_node_or_null("/root/GameManager")
		if gm:
			_bridge = gm.get("bridge")
	if _input_manager == null:
		_input_manager = get_node_or_null("/root/InputManager")


# ── Gradient deadzone: maps [deadzone, 1.0] → [0.0, 1.0] smoothly ──
func _apply_deadzone(value: float, dz: float = STICK_DEADZONE) -> float:
	var abs_val := absf(value)
	if abs_val < dz:
		return 0.0
	return signf(value) * (abs_val - dz) / (1.0 - dz)


func _get_steering_mode() -> int:
	# 0 = POINTER_FLIGHT, 1 = MANUAL_FLIGHT
	if _input_manager and _input_manager.get("steering_mode") != null:
		return int(_input_manager.get("steering_mode"))
	return 0


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.pressed and mb.button_index == MOUSE_BUTTON_LEFT:
			# LMB context resolution: combat fire > click-to-fly.
			_ensure_refs()
			if _bridge and _bridge.has_method("HasHostileInRangeV0"):
				var hostile: bool = _bridge.call("HasHostileInRangeV0")
				if hostile:
					_fire_primary()
					get_viewport().set_input_as_handled()
					return
			_try_click_navigate(mb.position)
		if mb.pressed and mb.button_index == MOUSE_BUTTON_RIGHT:
			# RMB: fire secondary weapon when hostiles in range.
			_ensure_refs()
			if _bridge and _bridge.has_method("HasHostileInRangeV0"):
				var hostile: bool = _bridge.call("HasHostileInRangeV0")
				if hostile:
					_fire_secondary()
					get_viewport().set_input_as_handled()
					return
	# WASD cancels autopilot.
	if event is InputEventKey and event.is_pressed():
		var key := event as InputEventKey
		if key.physical_keycode in [KEY_W, KEY_A, KEY_S, KEY_D]:
			_nav_active = false
	# Cruise toggle (C key).
	if event.is_action_pressed("cruise_toggle"):
		_toggle_cruise()
	# GATE.T60.SPIN.KEYBIND.001: Battle stations toggle (X key).
	if event.is_action_pressed("battle_stations"):
		_toggle_battle_stations()


func _fire_primary() -> void:
	_ensure_refs()
	if _bridge == null:
		return
	var target_id: String = ""
	if _bridge.has_method("GetLockedTargetV0"):
		target_id = str(_bridge.call("GetLockedTargetV0"))
	if target_id.is_empty() and _bridge.has_method("TargetNearestHostileV0"):
		target_id = str(_bridge.call("TargetNearestHostileV0"))
	if target_id.is_empty():
		return
	if _bridge.has_method("ApplyTurretShotV0"):
		var result: Dictionary = _bridge.call("ApplyTurretShotV0", target_id)
		if result.get("killed", false):
			print("UUIR|COMBAT_KILL|%s" % target_id)


func _fire_secondary() -> void:
	_ensure_refs()
	if _bridge == null:
		return
	var target_id: String = ""
	if _bridge.has_method("GetLockedTargetV0"):
		target_id = str(_bridge.call("GetLockedTargetV0"))
	if target_id.is_empty() and _bridge.has_method("TargetNearestHostileV0"):
		target_id = str(_bridge.call("TargetNearestHostileV0"))
	if target_id.is_empty():
		return
	# Secondary fire uses same turret shot for now — future: secondary weapon system.
	if _bridge.has_method("ApplyTurretShotV0"):
		_bridge.call("ApplyTurretShotV0", target_id)


func _toggle_cruise() -> void:
	_cruise_active = not _cruise_active
	if _cruise_active:
		_nav_active = false  # Cancel click-nav when engaging cruise.
		print("UUIR|CRUISE_ENGAGE")
	else:
		print("UUIR|CRUISE_DISENGAGE")

# GATE.T60.SPIN.KEYBIND.001: Toggle battle stations via SimBridge.
func _toggle_battle_stations() -> void:
	_ensure_refs()
	if _bridge == null:
		return
	var result = _bridge.call("ToggleBattleStationsV0")
	if result is Dictionary:
		print("UUIR|BATTLE_STATIONS|%s" % result.get("new_state", "unknown"))

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
	_ensure_refs()
	# GATE.T60.SPIN: Read spin RPM from bridge each frame.
	var gm = get_node_or_null("/root/GameManager")
	var spin_state_str: String = "StandDown"
	if _bridge and _bridge.has_method("GetBattleStationsStateV0"):
		var bs = _bridge.call("GetBattleStationsStateV0")
		if bs is Dictionary:
			spin_state_str = bs.get("state", "StandDown")
			if spin_state_str == "BattleReady":
				_spin_rpm = 20  # Default combat spin RPM
			elif spin_state_str == "SpinningUp":
				_spin_rpm = 10  # Half RPM during spin-up
			else:
				_spin_rpm = 0
		else:
			_spin_rpm = 0
	else:
		_spin_rpm = 0
	# GATE.T60.SPIN.CAMERA_SHAKE.001: Brief position shake on spin state change.
	if spin_state_str != _prev_spin_state:
		if spin_state_str == "SpinningUp" or spin_state_str == "BattleReady":
			# Apply brief RCS shake — small random offset that decays naturally.
			var shake_impulse := Vector3(randf_range(-0.3, 0.3), randf_range(-0.1, 0.1), randf_range(-0.3, 0.3))
			apply_central_impulse(shake_impulse)
			print("UUIR|SPIN_SHAKE|%s" % spin_state_str)
		_prev_spin_state = spin_state_str

	# Freeze input and kill momentum while docked or in lane transit.
	var ps = gm.get("current_player_state") if gm else 0
	var overlay_open = gm.get("galaxy_overlay_open") if gm else false
	if ps == 1 or ps == 2 or overlay_open:  # DOCKED, IN_LANE_TRANSIT, or galaxy map open
		linear_velocity = Vector3.ZERO
		angular_velocity = Vector3.ZERO
		_nav_active = false
		if _cruise_active:
			disengage_cruise_v0()
		return

	var steering_mode: int = _get_steering_mode()
	var thrust_axis: float = 0.0
	var turn_axis: float = 0.0
	var strafe_axis: float = 0.0
	var thrust_dir: Vector3 = -global_transform.basis.z
	var strafe_dir: Vector3 = global_transform.basis.x  # Ship's right vector.

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
		# Priority 3: Player input (steering-mode dependent).
		if _any_wasd_pressed():
			# Thrust: W/S always control forward/back in both modes.
			if Input.is_action_pressed("ship_thrust_fwd"):
				thrust_axis += 1.0
			if Input.is_action_pressed("ship_thrust_back"):
				thrust_axis -= 1.0

			if steering_mode == 0:
				# Pattern A (Pointer Flight): A/D = strafe. Heading from mouse.
				if Input.is_action_pressed("ship_turn_left"):
					strafe_axis -= 1.0
				if Input.is_action_pressed("ship_turn_right"):
					strafe_axis += 1.0
			else:
				# Pattern B (Manual Flight): A/D = turn.
				if Input.is_action_pressed("ship_turn_left"):
					turn_axis += 1.0
				if Input.is_action_pressed("ship_turn_right"):
					turn_axis -= 1.0
		elif steering_mode == 0:
			# Pattern A: Mouse-pointer steering (Freelancer-style) when WASD not held.
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
		# Pattern B no-WASD: no pointer steering. Ship holds heading.
		# Cruise in Pattern B: auto-thrust forward without steering.
		if steering_mode == 1 and _cruise_active and not _any_wasd_pressed():
			thrust_axis = 1.0

	if _test_turn_axis_v0 != null:
		turn_axis = float(_test_turn_axis_v0)
	elif not _nav_active and not (_test_thrust_axis_v0 != null):
		# In Pattern B with WASD: turn was already set above.
		# In Pattern A with WASD: pointer steering handles turn when WASD not held;
		# when WASD is held, we still want pointer steering for heading.
		if steering_mode == 0 and _any_wasd_pressed():
			# Pattern A: pointer steering for heading even when strafing with A/D.
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

	# ── Apply forces ──
	if thrust_axis != 0.0:
		apply_central_force(thrust_dir * (THRUST_FORCE_V0 * thrust_axis))

	if strafe_axis != 0.0:
		apply_central_force(strafe_dir * (STRAFE_FORCE_V0 * strafe_axis))

	if turn_axis != 0.0:
		# GATE.T60.SPIN.TURN_FEEL.001: Reduce turn rate while spinning (gyroscopic resistance).
		var effective_torque: float = TURN_TORQUE_V0
		if _spin_rpm > 0:
			var penalty: float = clampf(float(_spin_rpm) * SPIN_TURN_PENALTY_PER_RPM, 0.0, 0.6)
			effective_torque *= (1.0 - penalty)
		apply_torque(Vector3(0.0, effective_torque * turn_axis, 0.0))

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

	# ── Cosmetic banking (bank on turn + strafe) ──
	var visual := get_node_or_null("ShipVisual")
	var bank_input: float = turn_axis + strafe_axis * 0.5  # Strafe contributes less bank.
	if visual:
		var target_bank: float = -bank_input * BANK_ANGLE_MAX_V0
		visual.rotation.z = lerpf(visual.rotation.z, target_bank, BANK_LERP_SPEED_V0 * delta)
		# GATE.T60.SPIN.VISUAL.001: Visual forward-axis rotation when spinning.
		if _spin_rpm > 0:
			_spin_visual_angle += float(_spin_rpm) * SPIN_VISUAL_RAD_PER_RPM * delta
			if _spin_visual_angle > TAU:
				_spin_visual_angle -= TAU
			visual.rotation.x = _spin_visual_angle
		elif _spin_visual_angle != 0.0:
			# Decelerate visual spin smoothly back to 0.
			_spin_visual_angle = lerpf(_spin_visual_angle, 0.0, 3.0 * delta)
			if absf(_spin_visual_angle) < 0.01:
				_spin_visual_angle = 0.0
			visual.rotation.x = _spin_visual_angle

	# GATE.T60.SPIN.AUDIO_VFX.001: Gyro whine + running light color.
	_update_spin_audio_vfx(delta)


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
		if not station is Node3D:
			continue
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


# GATE.T60.SPIN.AUDIO_VFX.001: Gyro whine audio (pitch scales with RPM).
func _setup_gyro_audio() -> void:
	_gyro_audio = AudioStreamPlayer3D.new()
	_gyro_audio.name = "GyroWhine"
	_gyro_audio.bus = &"SFX"
	_gyro_audio.volume_db = GYRO_OFF_VOLUME_DB
	_gyro_audio.max_distance = 50.0
	_gyro_audio.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
	# Use AudioStreamGenerator for procedural gyro hum (sine tone).
	var gen := AudioStreamGenerator.new()
	gen.mix_rate = 22050.0
	gen.buffer_length = 0.1
	_gyro_audio.stream = gen
	add_child(_gyro_audio)


# GATE.T60.SPIN.AUDIO_VFX.001: Cache running light references for color transition.
func _cache_nav_lights() -> void:
	var visual := get_node_or_null("ShipVisual")
	if visual == null:
		return
	var model := visual.get_child(0) if visual.get_child_count() > 0 else null
	if model == null:
		return
	for child in model.get_children():
		if child is MeshInstance3D and child.name == "NavLight":
			_nav_lights.append(child)
			var mat: StandardMaterial3D = child.material_override
			if mat:
				_light_default_colors.append(mat.emission)
			else:
				_light_default_colors.append(Color.WHITE)


# GATE.T60.SPIN.AUDIO_VFX.001: Update gyro audio pitch/volume + light colors per frame.
func _update_spin_audio_vfx(delta: float) -> void:
	# --- Gyro audio ---
	if _gyro_audio:
		if _spin_rpm > 0:
			if not _gyro_audio.playing:
				_gyro_audio.play()
			var t: float = clampf(float(_spin_rpm) / 20.0, 0.0, 1.0)
			_gyro_audio.pitch_scale = lerpf(GYRO_BASE_PITCH, GYRO_MAX_PITCH, t)
			_gyro_audio.volume_db = lerpf(GYRO_OFF_VOLUME_DB, GYRO_MAX_VOLUME_DB, t)
			# Feed sine wave samples into the generator buffer.
			_fill_gyro_buffer(t)
		else:
			_gyro_audio.volume_db = lerpf(_gyro_audio.volume_db, GYRO_OFF_VOLUME_DB, 5.0 * delta)
			if _gyro_audio.volume_db <= GYRO_OFF_VOLUME_DB + 1.0:
				_gyro_audio.stop()

	# --- Running lights white→red ---
	var target_color: Color
	var blend: float = clampf(float(_spin_rpm) / 20.0, 0.0, 1.0)
	for i in range(_nav_lights.size()):
		var light: MeshInstance3D = _nav_lights[i]
		if not is_instance_valid(light):
			continue
		var mat: StandardMaterial3D = light.material_override
		if mat == null:
			continue
		var default_col: Color = _light_default_colors[i] if i < _light_default_colors.size() else Color.WHITE
		target_color = default_col.lerp(SPIN_LIGHT_RED, blend)
		mat.emission = target_color
		mat.albedo_color = target_color


# Fill the AudioStreamGenerator playback buffer with a sine tone.
func _fill_gyro_buffer(intensity: float) -> void:
	if _gyro_audio == null or _gyro_audio.stream == null:
		return
	var playback = _gyro_audio.get_stream_playback()
	if playback == null:
		return
	var frames_available: int = playback.get_frames_available()
	if frames_available <= 0:
		return
	# Dual-frequency hum: base 120 Hz + harmonic 240 Hz modulated by intensity.
	var base_freq: float = 120.0 + intensity * 180.0  # 120→300 Hz
	var mix_rate: float = 22050.0
	var phase_inc: float = base_freq / mix_rate
	# Use a class-level phase accumulator stored in metadata to avoid clicks.
	var phase: float = get_meta("_gyro_phase", 0.0)
	for i in range(frames_available):
		var sample: float = sin(phase * TAU) * 0.5 + sin(phase * TAU * 2.0) * 0.25
		playback.push_frame(Vector2(sample, sample))
		phase += phase_inc
		if phase > 1.0:
			phase -= 1.0
	set_meta("_gyro_phase", phase)
