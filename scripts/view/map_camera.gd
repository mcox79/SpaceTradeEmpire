extends Camera3D

@export var pan_speed: float = 70.0
@export var zoom_speed: float = 10.0
@export var min_height: float = 15.0
@export var max_height: float = 240.0

func _ready() -> void:
	set_process(false)
	set_process_unhandled_input(false)
	current = false

func _process(delta: float) -> void:
	var x := 0.0
	var z := 0.0
	if Input.is_key_pressed(KEY_LEFT): x -= 1.0
	if Input.is_key_pressed(KEY_RIGHT): x += 1.0
	if Input.is_key_pressed(KEY_UP): z -= 1.0
	if Input.is_key_pressed(KEY_DOWN): z += 1.0

	var v := Vector3(x, 0.0, z)
	if v.length() > 0.0:
		global_position += v.normalized() * pan_speed * delta

	_clamp_height()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			global_position.y = max(min_height, global_position.y - zoom_speed)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			global_position.y = min(max_height, global_position.y + zoom_speed)

	_clamp_height()

func _clamp_height() -> void:
	global_position.y = clamp(global_position.y, min_height, max_height)
