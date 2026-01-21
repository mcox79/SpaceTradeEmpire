extends CharacterBody3D

# --- CONFIGURATION ---
@export var speed_max := 25.0
@export var acceleration := 40.0
@export var friction := 15.0
@export var rotation_speed := 5.0

# --- ECONOMY ---
var credits: int = 100
var cargo: Dictionary = {} 
var cargo_capacity: int = 10

# --- SIGNALS ---
signal credits_updated(amount)
signal cargo_updated(manifest)

# --- INTERNAL VARS ---
var bullet_scene = preload("res://scenes/bullet.tscn")
var my_camera : Camera3D = null

func _ready():
    add_to_group("Player")
    
    # Camera Init
    my_camera = Camera3D.new()
    get_parent().call_deferred("add_child", my_camera)
    my_camera.position = Vector3(0, 40, 20)
    my_camera.look_at(Vector3.ZERO)
    my_camera.current = true
    
    _update_ui_state()

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
        
        # FIX 1: SPAWN OFFSET
        # Move the spawn point 2.0 units FORWARD relative to the ship's rotation.
        # In Godot, "Forward" is negative Z (-basis.z).
        var spawn_offset = -transform.basis.z * 2.0
        new_bullet.position = position + spawn_offset
        new_bullet.rotation = rotation
        
        get_parent().add_child(new_bullet)

# --- ECONOMY API ---
func _update_ui_state():
    emit_signal("credits_updated", credits)
    emit_signal("cargo_updated", cargo)

func add_cargo(item_id: String, amount: int):
    # This function allows Asteroids to put loot in the ship
    if not cargo.has(item_id): cargo[item_id] = 0
    cargo[item_id] += amount
    _update_ui_state()
    print("LOOT: +%s %s" % [amount, item_id])

func remove_cargo(item_id: String, amount: int):
    if cargo.has(item_id):
        cargo[item_id] -= amount
        if cargo[item_id] <= 0: cargo.erase(item_id)
        _update_ui_state()

func receive_payment(amount: int):
    credits += amount
    _update_ui_state()
