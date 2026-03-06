extends Node3D

# GATE.S1.CAMERA.FOLLOW_MODES.001: PhantomCamera3D-based follow modes.
# Three modes: Flight (chase), Orbit (docked free-orbit), Station (fixed angle).
# Falls back to basic Camera3D follow if PhantomCamera3D is unavailable.

# Tabs-only indentation policy applies.

enum CameraMode {
	FLIGHT,
	ORBIT,
	STATION,
}

@export var target_path: NodePath

# Flight mode: top-down (Starcom Nexus style) — camera directly above player.
@export var flight_offset: Vector3 = Vector3(0, 80, 1)
@export var flight_follow_distance: float = 80.0
@export var flight_damping: Vector3 = Vector3(0.15, 0.15, 0.15)

# Orbit mode: free orbit when docked.
@export var orbit_distance: float = 30.0
@export var orbit_sensitivity: float = 0.01
@export var orbit_min_pitch: float = -1.20
@export var orbit_max_pitch: float = 1.50
@export var orbit_min_distance: float = 8.0
@export var orbit_max_distance: float = 100.0
@export var orbit_zoom_step: float = 3.0

# Station mode: fixed camera transform for station view.
@export var station_offset: Vector3 = Vector3(0, 40, 25)

# FOV swell (flight mode only).
@export var fov_base: float = 60.0
@export var fov_boost_max: float = 2.0
@export var fov_smooth: float = 4.0

# Reset key.
@export var reset_keycode: int = KEY_R

var _current_mode: CameraMode = CameraMode.FLIGHT
var _target: Node3D

# PhantomCamera3D node reference (child of this Node3D, set in _ready or scene).
var _pcam: Node = null
var _pcam_available: bool = false

# Fallback Camera3D (used when PhantomCamera3D is not available).
var _fallback_cam: Camera3D = null
var _using_fallback: bool = false

# Orbit state (used in orbit mode).
var _orbit_yaw: float = 0.0
var _orbit_pitch: float = -0.25
var _orbit_distance: float = 30.0
var _orbit_rotating: bool = false
var _orbit_last_mouse: Vector2 = Vector2.ZERO

# FOV state.
var _current_fov: float = 60.0

# Flight look-around state (right-click drag).
var _flight_yaw_offset: float = 0.0
var _flight_pitch_offset: float = 0.0
var _flight_rotating: bool = false
var _flight_last_mouse: Vector2 = Vector2.ZERO
const FLIGHT_LOOK_SENSITIVITY: float = 0.003
const FLIGHT_LOOK_RETURN_SPEED: float = 3.0
const FLIGHT_MAX_PITCH: float = 1.4
const FLIGHT_MAX_YAW: float = PI

# GameManager reference for state polling.
var _game_manager = null

func _ready() -> void:
	_target = _resolve_target()
	if _target == null:
		push_warning("[FollowCamera] Target not found. Assign target_path or ensure Player is in group 'Player'.")

	# Try to find a PhantomCamera3D child node.
	_pcam = _find_phantom_camera()
	if _pcam != null:
		_pcam_available = true
		_setup_phantom_camera_flight()
	else:
		# Fallback: create a basic Camera3D as child.
		push_warning("[FollowCamera] PhantomCamera3D not found — using fallback Camera3D.")
		_using_fallback = true
		_fallback_cam = Camera3D.new()
		_fallback_cam.name = "FallbackCamera"
		_fallback_cam.fov = fov_base
		_fallback_cam.current = true
		add_child(_fallback_cam)
		_fallback_cam.set_as_top_level(true)
		if _target:
			_fallback_cam.global_position = _target.global_position + flight_offset
			_fallback_cam.look_at(_target.global_position, Vector3.UP)

	_current_fov = fov_base
	_orbit_distance = orbit_distance

	# Find GameManager for state polling.
	_game_manager = _find_game_manager()

func _physics_process(delta: float) -> void:
	if _target == null:
		_target = _resolve_target()
		if _target == null:
			return

	if _game_manager == null:
		_game_manager = _find_game_manager()

	# Poll game state and switch modes accordingly.
	_poll_and_switch_mode()

	if _using_fallback:
		_fallback_process(delta)
	elif _pcam_available:
		_pcam_process(delta)

	# Apply camera shake after all camera movement.
	_apply_shake_offset(delta)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		var k := event as InputEventKey
		if k.pressed and not k.echo and k.keycode == reset_keycode:
			_reset_orbit()
			get_viewport().set_input_as_handled()
			return

	# Orbit mode input: mouse drag and zoom.
	if _current_mode == CameraMode.ORBIT:
		if event is InputEventMouseButton:
			var mb := event as InputEventMouseButton
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
				_orbit_distance = clamp(_orbit_distance - orbit_zoom_step, orbit_min_distance, orbit_max_distance)
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
				_orbit_distance = clamp(_orbit_distance + orbit_zoom_step, orbit_min_distance, orbit_max_distance)
			elif mb.button_index == MOUSE_BUTTON_RIGHT:
				_orbit_rotating = mb.pressed
				_orbit_last_mouse = mb.position

		if event is InputEventMouseMotion and _orbit_rotating:
			var mm := event as InputEventMouseMotion
			var mouse_delta := mm.position - _orbit_last_mouse
			_orbit_last_mouse = mm.position
			_orbit_yaw -= mouse_delta.x * orbit_sensitivity
			_orbit_pitch -= mouse_delta.y * orbit_sensitivity
			_orbit_pitch = clamp(_orbit_pitch, orbit_min_pitch, orbit_max_pitch)

	# Flight mode: zoom with scroll wheel + right-click drag to look around.
	if _current_mode == CameraMode.FLIGHT:
		if event is InputEventMouseButton:
			var mb := event as InputEventMouseButton
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
				flight_follow_distance = clamp(flight_follow_distance - orbit_zoom_step, orbit_min_distance, orbit_max_distance)
				if _pcam_available and _pcam and _pcam.has_method("set_follow_distance"):
					_pcam.set_follow_distance(flight_follow_distance)
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
				flight_follow_distance = clamp(flight_follow_distance + orbit_zoom_step, orbit_min_distance, orbit_max_distance)
				if _pcam_available and _pcam and _pcam.has_method("set_follow_distance"):
					_pcam.set_follow_distance(flight_follow_distance)
			elif mb.button_index == MOUSE_BUTTON_RIGHT:
				_flight_rotating = mb.pressed
				_flight_last_mouse = mb.position
		if event is InputEventMouseMotion and _flight_rotating:
			var mm := event as InputEventMouseMotion
			var mouse_delta := mm.position - _flight_last_mouse
			_flight_last_mouse = mm.position
			_flight_yaw_offset = clamp(_flight_yaw_offset - mouse_delta.x * FLIGHT_LOOK_SENSITIVITY, -FLIGHT_MAX_YAW, FLIGHT_MAX_YAW)
			_flight_pitch_offset = clamp(_flight_pitch_offset - mouse_delta.y * FLIGHT_LOOK_SENSITIVITY, -FLIGHT_MAX_PITCH, FLIGHT_MAX_PITCH)


# ── Mode switching ──

func _poll_and_switch_mode() -> void:
	if _game_manager == null:
		return
	var state_val = _game_manager.get("current_player_state")
	if state_val == null:
		return

	# Map PlayerShipState enum to CameraMode.
	# PlayerShipState: IN_FLIGHT=0, DOCKED=1, IN_LANE_TRANSIT=2
	var new_mode: CameraMode
	match int(state_val):
		0:  # IN_FLIGHT
			new_mode = CameraMode.FLIGHT
		1:  # DOCKED
			# Check dock target kind to differentiate orbit vs station.
			var dock_kind = _game_manager.get("dock_target_kind_token")
			if dock_kind != null and str(dock_kind) == "STATION":
				new_mode = CameraMode.STATION
			else:
				new_mode = CameraMode.ORBIT
		2:  # IN_LANE_TRANSIT
			new_mode = CameraMode.FLIGHT
		_:
			new_mode = CameraMode.FLIGHT

	if new_mode != _current_mode:
		_switch_mode(new_mode)

func _switch_mode(new_mode: CameraMode) -> void:
	_current_mode = new_mode
	match new_mode:
		CameraMode.FLIGHT:
			_enter_flight_mode()
		CameraMode.ORBIT:
			_enter_orbit_mode()
		CameraMode.STATION:
			_enter_station_mode()


# ── PhantomCamera3D mode setup ──

func _enter_flight_mode() -> void:
	if _pcam_available and _pcam:
		# SIMPLE follow with damping behind the ship.
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 2  # FollowMode.SIMPLE
		if _pcam.has_method("set_follow_target"):
			_pcam.set_follow_target(_target)
		if _pcam.has_method("set_follow_offset"):
			_pcam.set_follow_offset(flight_offset)
		if _pcam.has_method("set_follow_distance"):
			_pcam.set_follow_distance(flight_follow_distance)
		if _pcam.has_method("set_follow_damping"):
			_pcam.set_follow_damping(true)
		if _pcam.has_method("set_follow_damping_value"):
			_pcam.set_follow_damping_value(flight_damping)
		# Look at the ship.
		if _pcam.has_method("set_look_at_target"):
			_pcam.set_look_at_target(_target)

func _enter_orbit_mode() -> void:
	_reset_orbit()
	if _pcam_available and _pcam:
		# Disable PhantomCamera follow — we drive position manually in orbit.
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 0  # FollowMode.NONE

func _enter_station_mode() -> void:
	if _pcam_available and _pcam:
		# Fixed offset above the target.
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 2  # FollowMode.SIMPLE
		if _pcam.has_method("set_follow_target"):
			_pcam.set_follow_target(_target)
		if _pcam.has_method("set_follow_offset"):
			_pcam.set_follow_offset(station_offset)
		if _pcam.has_method("set_follow_damping"):
			_pcam.set_follow_damping(false)
		if _pcam.has_method("set_look_at_target"):
			_pcam.set_look_at_target(_target)


# ── Per-frame processing ──

func _pcam_process(delta: float) -> void:
	match _current_mode:
		CameraMode.FLIGHT:
			_update_fov_pcam(delta)
		CameraMode.ORBIT:
			_update_orbit_pcam(delta)
		CameraMode.STATION:
			pass  # PhantomCamera3D handles station view via SIMPLE follow.

func _update_fov_pcam(_delta: float) -> void:
	# FOV swell based on ship velocity (flight mode only).
	var speed: float = 0.0
	if _target is RigidBody3D:
		speed = (_target as RigidBody3D).linear_velocity.length()
	var t: float = clamp(speed / 18.0, 0.0, 1.0)
	var target_fov: float = fov_base + fov_boost_max * t
	_current_fov = lerp(_current_fov, target_fov, 1.0 - exp(-fov_smooth * _delta))
	# Apply FOV to the viewport camera (PhantomCameraHost drives the actual Camera3D).
	var cam := get_viewport().get_camera_3d()
	if cam:
		cam.fov = _current_fov

func _update_orbit_pcam(delta: float) -> void:
	# Manual orbit: drive the PhantomCamera3D global_position around target.
	if _target == null:
		return
	var tpos := _target.global_position
	var desired := _orbit_desired_position(tpos)
	if _pcam:
		_pcam.global_position = _pcam.global_position.lerp(desired, 1.0 - exp(-5.0 * delta))
		_pcam.look_at(tpos, Vector3.UP)


# ── Fallback Camera3D processing ──

func _fallback_process(delta: float) -> void:
	if _fallback_cam == null or _target == null:
		return

	# GATE.S13.CAMERA.PERSIST.001: Camera holds rotation on mouse release.
	# No snap-back — yaw/pitch offsets persist until next right-click drag.

	match _current_mode:
		CameraMode.FLIGHT:
			# Use flight_follow_distance to compute offset so scroll zoom works.
			var offset_dir := flight_offset.normalized()
			# Apply right-click look-around yaw/pitch to the camera offset direction.
			var rotated_dir := offset_dir
			if absf(_flight_yaw_offset) > 0.001 or absf(_flight_pitch_offset) > 0.001:
				rotated_dir = offset_dir.rotated(Vector3.UP, _flight_yaw_offset)
				var right := rotated_dir.cross(Vector3.UP).normalized()
				rotated_dir = rotated_dir.rotated(right, _flight_pitch_offset)
			var desired_pos := _target.global_position + rotated_dir * flight_follow_distance
			_fallback_cam.global_position = _fallback_cam.global_position.lerp(desired_pos, 1.0 - exp(-5.0 * delta))
			var look_target := _target.global_position
			_fallback_cam.look_at(look_target, Vector3.UP)
			_update_fov_fallback(delta)

		CameraMode.ORBIT:
			var tpos := _target.global_position
			var desired := _orbit_desired_position(tpos)
			_fallback_cam.global_position = _fallback_cam.global_position.lerp(desired, 1.0 - exp(-5.0 * delta))
			_fallback_cam.look_at(tpos, Vector3.UP)

		CameraMode.STATION:
			var desired_pos := _target.global_position + station_offset
			_fallback_cam.global_position = _fallback_cam.global_position.lerp(desired_pos, 1.0 - exp(-5.0 * delta))
			_fallback_cam.look_at(_target.global_position, Vector3.UP)

func _update_fov_fallback(delta: float) -> void:
	var speed: float = 0.0
	if _target is RigidBody3D:
		speed = (_target as RigidBody3D).linear_velocity.length()
	var t: float = clamp(speed / 18.0, 0.0, 1.0)
	var target_fov: float = fov_base + fov_boost_max * t
	_current_fov = lerp(_current_fov, target_fov, 1.0 - exp(-fov_smooth * delta))
	_fallback_cam.fov = _current_fov


# ── Orbit helpers ──

func _orbit_desired_position(center: Vector3) -> Vector3:
	var cp := cos(_orbit_pitch)
	var sp := sin(_orbit_pitch)
	var cy := cos(_orbit_yaw)
	var sy := sin(_orbit_yaw)
	var away := Vector3(sy * cp, sp, cy * cp).normalized()
	return center + away * _orbit_distance

func _reset_orbit() -> void:
	_orbit_yaw = 0.0
	_orbit_pitch = -0.25
	_orbit_distance = orbit_distance


# ── Target resolution ──

func _resolve_target() -> Node3D:
	if target_path != NodePath():
		var n = get_node_or_null(target_path)
		if n is Node3D:
			return n

	var tree = get_tree()
	if tree == null:
		return null
	var p = tree.get_first_node_in_group("Player")
	if p is Node3D:
		return p

	return null

func _find_phantom_camera() -> Node:
	# Look for a PhantomCamera3D child node.
	for child in get_children():
		if child.get_class() == "PhantomCamera3D":
			return child
		# Also check script class_name for GDScript-based PhantomCamera3D.
		if child.has_method("set_follow_target") and child.has_method("set_follow_mode"):
			return child

	# Try to find one in the scene tree as a sibling or nearby node.
	var parent = get_parent()
	if parent:
		for child in parent.get_children():
			if child == self:
				continue
			if child.get_class() == "PhantomCamera3D":
				return child
			if child.has_method("set_follow_target") and child.has_method("set_follow_mode"):
				return child

	return null

func _find_game_manager():
	# Try scene-child GameManager first, then autoload.
	var parent = get_parent()
	if parent:
		var gm = parent.get_node_or_null("GameManager")
		if gm:
			return gm
	var tree = get_tree()
	if tree == null:
		return null
	var gm_auto = tree.root.get_node_or_null("GameManager")
	if gm_auto:
		return gm_auto
	# Last resort: search the tree.
	var gm_found = tree.root.find_child("GameManager", true, false)
	return gm_found

func _setup_phantom_camera_flight() -> void:
	if _pcam == null:
		return
	# Snap PhantomCamera to the correct follow position immediately so it
	# doesn't lerp from global origin (top_level = true starts at 0,0,0).
	if _target:
		_pcam.global_position = _target.global_position + flight_offset
		_pcam.look_at(_target.global_position, Vector3.UP)
	# Initial setup: flight mode with follow on the target.
	if _pcam.has_method("set_follow_mode"):
		_pcam.follow_mode = 2  # FollowMode.SIMPLE
	if _target and _pcam.has_method("set_follow_target"):
		_pcam.set_follow_target(_target)
	if _pcam.has_method("set_follow_offset"):
		_pcam.set_follow_offset(flight_offset)
	if _pcam.has_method("set_follow_distance"):
		_pcam.set_follow_distance(flight_follow_distance)
	if _pcam.has_method("set_follow_damping"):
		_pcam.set_follow_damping(true)
	if _pcam.has_method("set_follow_damping_value"):
		_pcam.set_follow_damping_value(flight_damping)
	if _target and _pcam.has_method("set_look_at_target"):
		_pcam.set_look_at_target(_target)


# ── Camera shake (GATE.S1.CAMERA.COMBAT_SHAKE.001) ──

var _shake_trauma: float = 0.0
var _shake_decay: float = 3.0
const SHAKE_MAX_OFFSET: float = 0.5
const SHAKE_MAX_ROTATION: float = 0.03

## Call to apply camera shake. intensity 0.0-1.0.
func apply_shake(intensity: float) -> void:
	_shake_trauma = clampf(_shake_trauma + intensity, 0.0, 1.0)

func _apply_shake_offset(delta: float) -> void:
	if _shake_trauma <= 0.0:
		return
	var shake_amount: float = _shake_trauma * _shake_trauma  # quadratic for snappier feel
	var cam := get_viewport().get_camera_3d() if not _using_fallback else _fallback_cam
	if cam == null:
		return
	var offset_x: float = randf_range(-1.0, 1.0) * SHAKE_MAX_OFFSET * shake_amount
	var offset_y: float = randf_range(-1.0, 1.0) * SHAKE_MAX_OFFSET * shake_amount
	var rot_z: float = randf_range(-1.0, 1.0) * SHAKE_MAX_ROTATION * shake_amount
	cam.h_offset = offset_x
	cam.v_offset = offset_y
	cam.rotation.z = rot_z
	_shake_trauma = maxf(0.0, _shake_trauma - _shake_decay * delta)
