extends Camera3D

# Tabs-only indentation policy applies.

@export var target_path: NodePath

# Starting camera shape (used for reset + initial framing)
@export var offset: Vector3 = Vector3(0, 40, 25)

@export var follow_smooth: float = 10.0
@export var look_smooth: float = 12.0

@export var min_distance: float = 8.0
@export var max_distance: float = 220.0
@export var zoom_step: float = 3.0

@export var rotate_sensitivity: float = 0.01

# Pitch limits (radians). Allow near-overhead.
# 1.50 rad ~= 86 degrees.
@export var min_pitch: float = -1.20
@export var max_pitch: float = 1.50

# Reset key (always gets you back to the initial high view)
@export var reset_keycode: int = KEY_R

var _target: Node3D
var _yaw: float = 0.0
var _pitch: float = -0.25
var _distance: float = 18.0

var _rotating: bool = false
var _last_mouse: Vector2 = Vector2.ZERO

var _default_yaw: float = 0.0
var _default_pitch: float = -0.25
var _default_distance: float = 18.0

func _ready() -> void:
	set_as_top_level(true)

	_target = _resolve_target()
	if _target == null:
		push_warning("[FollowCamera] Target not found. Assign target_path or ensure Player is in group 'Player'.")
		return

	_compute_defaults_from_offset()
	_reset_to_defaults()

	global_position = _desired_position()
	look_at(_target.global_position, Vector3.UP)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey:
		var k := event as InputEventKey
		if k.pressed and not k.echo and k.keycode == reset_keycode:
			_reset_to_defaults()
			get_viewport().set_input_as_handled()
			return

	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
			_distance = clamp(_distance - zoom_step, min_distance, max_distance)
		elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
			_distance = clamp(_distance + zoom_step, min_distance, max_distance)
		elif mb.button_index == MOUSE_BUTTON_RIGHT:
			_rotating = mb.pressed
			_last_mouse = mb.position

	if event is InputEventMouseMotion and _rotating:
		var mm := event as InputEventMouseMotion
		var delta := mm.position - _last_mouse
		_last_mouse = mm.position

		_yaw -= delta.x * rotate_sensitivity
		_pitch -= delta.y * rotate_sensitivity
		_pitch = clamp(_pitch, min_pitch, max_pitch)

func _physics_process(delta: float) -> void:
	if _target == null:
		_target = _resolve_target()
		if _target == null:
			return

	var desired_pos = _desired_position()
	global_position = global_position.lerp(desired_pos, 1.0 - exp(-follow_smooth * delta))

	var desired_look = _target.global_position
	var current_look = global_transform.origin + (-global_transform.basis.z * 10.0)
	var smoothed_look = current_look.lerp(desired_look, 1.0 - exp(-look_smooth * delta))

	look_at(smoothed_look, Vector3.UP)

func _resolve_target() -> Node3D:
	if target_path != NodePath():
		var n = get_node_or_null(target_path)
		if n is Node3D:
			return n

	var p = get_tree().get_first_node_in_group("Player")
	if p is Node3D:
		return p

	return null

func _desired_position() -> Vector3:
	var tpos = _target.global_position

	var cp = cos(_pitch)
	var sp = sin(_pitch)
	var cy = cos(_yaw)
	var sy = sin(_yaw)

	var away = Vector3(sy * cp, sp, cy * cp).normalized()
	return tpos + away * _distance

func _compute_defaults_from_offset() -> void:
	var d = offset
	var len = d.length()
	if len < 0.001:
		d = Vector3(0, 40, 25)
		len = d.length()

	_default_distance = clamp(len, min_distance, max_distance)

	var dir = d.normalized()
	_default_yaw = atan2(dir.x, dir.z)
	_default_pitch = asin(clamp(dir.y, -0.99, 0.99))
	_default_pitch = clamp(_default_pitch, min_pitch, max_pitch)

func _reset_to_defaults() -> void:
	_yaw = _default_yaw
	_pitch = _default_pitch
	_distance = _default_distance
