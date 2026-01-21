extends CharacterBody3D

# --- CONFIGURATION ---
@export_group("Flight Systems")
@export var speed_max := 25.0
@export var acceleration := 40.0
@export var friction := 15.0
@export var rotation_speed := 5.0

@export_group("Weapon Systems")
@export var weapon_cooldown := 0.25
@export var invert_mesh_fire: bool = true # HARDCODED FIX for your specific mesh

# --- ECONOMY ---
var credits: int = 100
var cargo: Dictionary = {} 
var cargo_capacity: int = 10
var is_docked: bool = false

# --- SIGNALS ---
signal credits_updated(amount)
signal cargo_updated(manifest)
signal shop_toggled(is_open)

# --- INTERNAL VARS ---
var bullet_scene = preload("res://scenes/bullet.tscn")
var my_camera : Camera3D = null
var can_shoot : bool = true

func _ready():
    add_to_group("Player")
    Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
    
    # CAMERA RE-INIT
    if not my_camera:
        my_camera = Camera3D.new()
        get_parent().call_deferred("add_child", my_camera)
        my_camera.position = Vector3(0, 40, 20)
        my_camera.look_at(Vector3.ZERO)
        my_camera.current = true
    
    # UI HANDSHAKE
    # We wait for the tree to settle, then force the UI to update
    await get_tree().process_frame
    _update_ui_state()
    print("[SYSTEM] PLAYER SYSTEMS ONLINE.")

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

    if my_camera:
        var target_pos = global_position + Vector3(0, 40, 20)
        my_camera.global_position = my_camera.global_position.lerp(target_pos, 5.0 * delta)
        my_camera.look_at(global_position)

func shoot():
    if not bullet_scene: return
    
    can_shoot = false
    var new_bullet = bullet_scene.instantiate()
    
    # --- THE WEAPON PROTOCOL ---
    # 1. Position: Start at ship center
    new_bullet.position = position 
    
    # 2. Rotation: Match Ship
    new_bullet.rotation = rotation
    
    # 3. Correction: If mesh is inverted, FLIP the bullet 180 degrees
    if invert_mesh_fire:
        new_bullet.rotate_object_local(Vector3.UP, PI) # Rotate 180 deg (PI radians)
    
    # 4. Offset: Move bullet forward (Local -Z) so it doesn't clip hull
    # Note: Since we rotated it, -Z is now "Forward" relative to the bullet
    new_bullet.position += new_bullet.transform.basis.z * -2.0
    
    # 5. IFF SIGNATURE (CRITICAL FIX)
    new_bullet.shooter = self
    
    get_parent().add_child(new_bullet)
    
    await get_tree().create_timer(weapon_cooldown).timeout
    can_shoot = true

# --- ECONOMY API ---
func _update_ui_state():
    emit_signal("credits_updated", credits)
    emit_signal("cargo_updated", cargo)

func add_cargo(item_id: String, amount: int):
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
    print("[SYSTEM] DOCKING ENGAGED.")

func undock():
    is_docked = false
    emit_signal("shop_toggled", false)
    # Push back slightly to clear clamp
    position -= transform.basis.z * 5.0
