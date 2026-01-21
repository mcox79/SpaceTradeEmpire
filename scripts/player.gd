extends CharacterBody3D

# --- CONFIGURATION ---
@export_group("Ship Settings")
@export var speed_max := 25.0
@export var acceleration := 40.0
@export var friction := 15.0
@export var rotation_speed := 5.0
@export var max_health := 10

@export_group("Weapon Systems")
@export var weapon_cooldown := 0.25
@export var invert_mesh_fire: bool = true

# --- STATE ---
var health: int = 10
var credits: int = 100
var cargo: Dictionary = {} 
var cargo_capacity: int = 10
var is_docked: bool = false
var can_shoot : bool = true

# --- SIGNALS ---
signal credits_updated(amount)
signal cargo_updated(manifest)
signal health_updated(current, max)
signal shop_toggled(is_open)

# --- INTERNAL ---
var bullet_scene = preload("res://scenes/bullet.tscn")
var my_camera : Camera3D = null

func _ready():
    add_to_group("Player")
    Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
    health = max_health
    
    # Cam Init
    if not my_camera:
        my_camera = Camera3D.new()
        get_parent().call_deferred("add_child", my_camera)
        my_camera.position = Vector3(0, 40, 20)
        my_camera.look_at(Vector3.ZERO)
        my_camera.current = true
    
    await get_tree().process_frame
    _update_ui_state()
    print("[SYSTEM] HULL INTEGRITY: %s/%s" % [health, max_health])

func _physics_process(delta):
    if is_docked: return
    
    var input_dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
    var direction = Vector3(input_dir.x, 0, input_dir.y).normalized()

    if direction:
        velocity = velocity.move_toward(direction * speed_max, acceleration * delta)
        var target_rotation = atan2(velocity.x, velocity.z)
        rotation.y = lerp_angle(rotation.y, target_rotation, rotation_speed * delta)
    else:
        velocity = velocity.move_toward(Vector3.ZERO, friction * delta)

    move_and_slide()

    if Input.is_action_pressed("ui_accept") and can_shoot:
        shoot()
        
    # Cam Follow
    if my_camera:
        var target_pos = global_position + Vector3(0, 40, 20)
        my_camera.global_position = my_camera.global_position.lerp(target_pos, 5.0 * delta)
        my_camera.look_at(global_position)

func shoot():
    if not bullet_scene: return
    can_shoot = false
    var new_bullet = bullet_scene.instantiate()
    new_bullet.position = position 
    new_bullet.rotation = rotation
    if invert_mesh_fire: new_bullet.rotate_object_local(Vector3.UP, PI)
    new_bullet.position += new_bullet.transform.basis.z * -2.0
    new_bullet.shooter = self
    get_parent().add_child(new_bullet)
    await get_tree().create_timer(weapon_cooldown).timeout
    can_shoot = true

# --- DAMAGE PROTOCOL (NEW) ---
func take_damage(amount: int):
    health -= amount
    emit_signal("health_updated", health, max_health)
    print("WARNING: Hull Breach! Integrity: %s" % health)
    
    # Screen Shake / Flash could go here
    
    if health <= 0:
        _die()

func _die():
    print("CRITICAL FAILURE: SHIP DESTROYED.")
    # For now, instant reset. Later: Game Over Screen.
    get_tree().reload_current_scene()

# --- ECONOMY API ---
func _update_ui_state():
    emit_signal("credits_updated", credits)
    emit_signal("cargo_updated", cargo)
    emit_signal("health_updated", health, max_health)

func add_cargo(item_id: String, amount: int):
    if not cargo.has(item_id): cargo[item_id] = 0
    cargo[item_id] += amount
    _update_ui_state()

func remove_cargo(item_id: String, amount: int):
    if cargo.has(item_id):
        cargo[item_id] -= amount
        if cargo[item_id] <= 0: cargo.erase(item_id)
        _update_ui_state()

func receive_payment(amount: int):
    credits += amount
    _update_ui_state()

func purchase_upgrade(type: String, cost: int):
    if credits >= cost:
        credits -= cost
        _apply_upgrade(type)
        _update_ui_state()

func _apply_upgrade(type: String):
    match type:
        "speed": speed_max += 5.0
        "weapon": weapon_cooldown *= 0.8

# --- DOCKING API ---
func dock_at_station():
    if is_docked: return
    is_docked = true
    velocity = Vector3.ZERO
    emit_signal("shop_toggled", true)

func undock():
    is_docked = false
    emit_signal("shop_toggled", false)
    position -= transform.basis.z * 5.0
