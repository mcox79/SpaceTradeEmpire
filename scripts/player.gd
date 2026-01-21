extends CharacterBody3D

# --- SIGNALS (NEW) ---
signal credits_updated(amount)
signal cargo_updated(manifest)

# --- CONFIGURATION ---
@export var speed_max := 25.0
@export var acceleration := 40.0
@export var friction := 15.0
@export var rotation_speed := 5.0

# --- ECONOMY ---
var credits: int = 100
var cargo: Dictionary = {} 
var cargo_capacity: int = 10

# --- INTERNAL VARS ---
var bullet_scene = preload("res://scenes/bullet.tscn")
var my_camera : Camera3D = null

func _ready():
    # 1. IDENTIFICATION (For UI to find us)
    add_to_group("Player")

    # 2. CAMERA SETUP
    my_camera = Camera3D.new()
    get_parent().call_deferred("add_child", my_camera)
    my_camera.position = Vector3(0, 40, 20)
    my_camera.look_at(Vector3.ZERO)
    my_camera.current = true
    
    # 3. ECONOMY INIT
    _update_ui_state()
    _add_test_cargo()

func _physics_process(delta):
    var input_dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
    var direction = Vector3(input_dir.x, 0, input_dir.y).normalized()

    if direction:
        velocity = velocity.move_toward(direction * speed_max, acceleration * delta)
        var target_rotation = atan2(velocity.x, velocity.z)
        rotation.y = lerp_angle(rotation.y, target_rotation, rotation_speed * delta)
    else:
        velocity = velocity.move_toward(Vector3.ZERO, friction * delta)

    move_and_slide()

    if Input.is_action_just_pressed("ui_accept"):
        shoot()

    if my_camera:
        var target_pos = global_position + Vector3(0, 40, 20)
        my_camera.global_position = my_camera.global_position.lerp(target_pos, 5.0 * delta)
        my_camera.look_at(global_position)

func shoot():
    if bullet_scene:
        var new_bullet = bullet_scene.instantiate()
        new_bullet.position = position
        new_bullet.rotation = rotation
        get_parent().add_child(new_bullet)

# --- ECONOMY API ---
func _update_ui_state():
    emit_signal("credits_updated", credits)
    emit_signal("cargo_updated", cargo)

func _add_test_cargo():
    cargo["ore_iron"] = 5
    _update_ui_state() # Broadcast change

func remove_cargo(item_id: String, amount: int):
    if cargo.has(item_id):
        cargo[item_id] -= amount
        if cargo[item_id] <= 0: cargo.erase(item_id)
        _update_ui_state() # Broadcast change

func receive_payment(amount: int):
    credits += amount
    _update_ui_state() # Broadcast change
    print("PAYMENT: +$%s" % amount)
