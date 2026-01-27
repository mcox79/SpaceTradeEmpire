extends Camera3D

var move_speed: float = 50.0
var zoom_speed: float = 5.0
var min_height: float = 10.0
var max_height: float = 200.0

func _process(delta):
	var velocity = Vector3.ZERO

	# WASD / ARROW MOVEMENT
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		velocity.z -= 1
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		velocity.z += 1
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		velocity.x -= 1
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		velocity.x += 1

	if velocity.length() > 0:
		global_position += velocity.normalized() * move_speed * delta

func _unhandled_input(event):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			global_position.y = max(min_height, global_position.y - zoom_speed)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			global_position.y = min(max_height, global_position.y + zoom_speed)