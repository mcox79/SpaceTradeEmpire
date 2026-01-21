extends CharacterBody3D

# --- CONFIGURATION ---
@export var speed_max := 25.0
@export var acceleration := 40.0
@export var friction := 15.0
@export var rotation_speed := 5.0

# --- INTERNAL VARS ---
var bullet_scene = preload("res://scenes/bullet.tscn")
var my_camera : Camera3D = null

func _ready():
	# 1. SPAWN THE CAMERA
	# We create a brand new camera from thin air.
	my_camera = Camera3D.new()
	
	# 2. PLACE IT IN THE WORLD (Not inside the Player)
	# "get_parent()" puts it in the Main scene, keeping it independent.
	get_parent().call_deferred("add_child", my_camera)
	
	# 3. CONFIGURE IT
	# Move it high up (Y=40) and back (Z=20)
	my_camera.position = Vector3(0, 40, 20)
	# Point it at the origin (where the player starts)
	my_camera.look_at(Vector3.ZERO)
	# Force this to be the active eye
	my_camera.current = true

func _physics_process(delta):
	# --- MOVEMENT LOGIC ---
	var input_dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	var direction = Vector3(input_dir.x, 0, input_dir.y).normalized()
	
	if direction:
		velocity = velocity.move_toward(direction * speed_max, acceleration * delta)
		# Face direction
		var target_rotation = atan2(velocity.x, velocity.z)
		rotation.y = lerp_angle(rotation.y, target_rotation, rotation_speed * delta)
	else:
		velocity = velocity.move_toward(Vector3.ZERO, friction * delta)

	move_and_slide()
	
	# --- SHOOTING ---
	if Input.is_action_just_pressed("ui_accept"):
		shoot()
		
	# --- CAMERA FOLLOW LOGIC ---
	if my_camera:
		# 1. Move to the offset position (High and Back)
		var target_pos = global_position + Vector3(0, 40, 20)
		my_camera.global_position = my_camera.global_position.lerp(target_pos, 5.0 * delta)
		
		# 2. CRITICAL FIX: LOOK AT THE SHIP
		# This forces the camera to angle down and center the ship on screen.
		my_camera.look_at(global_position)
func shoot():
	var new_bullet = bullet_scene.instantiate()
	new_bullet.position = position
	new_bullet.rotation = rotation
	get_parent().add_child(new_bullet)
