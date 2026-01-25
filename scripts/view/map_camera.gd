extends Camera3D

# Strategic Camera Controller

var pan_speed: float = 50.0
var zoom_speed: float = 10.0
var min_height: float = 20.0
var max_height: float = 200.0

func _process(delta):
	var move_dir = Vector3.ZERO
	
	# WASD / Arrow Keys for Panning
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		move_dir.z -= 1
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		move_dir.z += 1
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		move_dir.x -= 1
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		move_dir.x += 1
		
	position += move_dir.normalized() * pan_speed * delta

func _unhandled_input(event):
	# Mouse Wheel for Zooming (Moves the camera up and down)
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			position.y = clamp(position.y - zoom_speed, min_height, max_height)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			position.y = clamp(position.y + zoom_speed, min_height, max_height)