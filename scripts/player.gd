extends CharacterBody3D

@export var visual_scale: float = 0.35
var _input_enabled: bool = true
# --- CONFIG ---
@export_group("Ship Settings")
@export var speed_max := 25.0
@export var acceleration := 40.0
@export var friction := 15.0
@export var rotation_speed := 5.0
@export var max_health := 10
@export var max_fuel := 1000.0
@export var fuel_burn_rate := 15.0 # Cost to fly

@export_group("Weapon Systems")
@export var weapon_cooldown := 0.25
@export var invert_mesh_fire: bool = true

# Phase 1 Economy
@export var max_cargo_volume: float = 10.0
@export var trade_goods: Array[TradeGood] = []

# --- STATE ---
var health: int = 10
var fuel: float = 1000.0
var credits: int = 100
var cargo: Dictionary = {}
var cargo_capacity: int = 10 # legacy, kept for now
var cargo_volume: float = 0.0

var is_docked: bool = false
var can_shoot: bool = true
var current_station = null

# --- SIGNALS ---
signal credits_updated(amount)
signal cargo_updated(manifest)
signal health_updated(current, max)
signal fuel_updated(current, max)
signal shop_toggled(is_open, station_ref)

# --- INTERNAL ---
var bullet_scene = preload("res://scenes/bullet.tscn")
var my_camera: Camera3D = null
var _goods_by_id: Dictionary = {}

func _ready():
	add_to_group("player")
	scale = Vector3.ONE * visual_scale
	health = max_health
	fuel = max_fuel

	# Cam Init (scene camera only)
	my_camera = get_node_or_null("CameraMount/Camera3D")
	if my_camera:
		my_camera.current = true

	_build_goods_index()
	_recalc_cargo_volume_from_manifest()

	await get_tree().process_frame
	_update_ui_state()

func _build_goods_index():
	_goods_by_id.clear()
	for g in trade_goods:
		if g == null:
			continue
		_goods_by_id[g.id] = g

func _get_item_volume(item_id: String) -> float:
	var g: TradeGood = _goods_by_id.get(item_id, null)
	if g == null:
		return 1.0
	return g.volume

func _recalc_cargo_volume_from_manifest():
	cargo_volume = 0.0
	for item_id in cargo.keys():
		var amt := int(cargo[item_id])
		cargo_volume += _get_item_volume(item_id) * float(amt)

func add_cargo(item_id: String, amount: int) -> bool:
	if amount <= 0:
		return false
	var item_vol := _get_item_volume(item_id)
	var required := item_vol * float(amount)
	if (cargo_volume + required) > max_cargo_volume:
		return false
	if not cargo.has(item_id):
		cargo[item_id] = 0
	cargo[item_id] = int(cargo[item_id]) + amount
	cargo_volume += required
	_update_ui_state()
	return true

func remove_cargo(item_id: String, amount: int) -> bool:
	if amount <= 0:
		return false
	if not cargo.has(item_id):
		return false
	var available: int = int(cargo[item_id])
	var removed: int = mini(available, amount)
	cargo[item_id] = available - removed
	if int(cargo[item_id]) <= 0:
		cargo.erase(item_id)
	var item_vol := _get_item_volume(item_id)
	cargo_volume = max(0.0, cargo_volume - (item_vol * float(removed)))
	_update_ui_state()
	return true
func _physics_process(delta):
	if not _input_enabled:
		velocity = Vector3.ZERO
		move_and_slide()
		return


	if is_docked:
		return

	# Flight controls: yaw + thrust (ship-forward)
	var yaw_input: float = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
	var thrust_input: float = Input.get_action_strength("ui_up") - Input.get_action_strength("ui_down")

	# Rotate ship (yaw). rotation_speed is now yaw speed (rad/s).
	if yaw_input != 0.0:
		rotation.y += yaw_input * rotation_speed * delta

	# Forward is -Z in Godot 3D
	var forward: Vector3 = -global_transform.basis.z

	# Thrust / reverse thrust
	if thrust_input != 0.0 and fuel > 0.0:
		var target_vel: Vector3 = forward * (speed_max * thrust_input)
		velocity = velocity.move_toward(target_vel, acceleration * delta)

		fuel -= fuel_burn_rate * abs(thrust_input) * delta
		if fuel < 0.0:
			fuel = 0.0
		emit_signal("fuel_updated", int(fuel), int(max_fuel))
	else:
		# Damping (lower friction for more inertia)
		velocity = velocity.move_toward(Vector3.ZERO, friction * delta)
	# Orthonormalize basis to avoid any scaling artifacts.
	var direction: Vector3 = Vector3(input_dir.x, 0, input_dir.y).normalized()

	if direction != Vector3.ZERO and fuel > 0.0:
		velocity = velocity.move_toward(direction * speed_max, acceleration * delta)

		fuel -= fuel_burn_rate * delta
		if fuel < 0.0:
			fuel = 0.0
		emit_signal("fuel_updated", int(fuel), int(max_fuel))

		var target_rotation: float = atan2(-direction.x, -direction.z)
		rotation.y = lerp_angle(rotation.y, target_rotation, rotation_speed * delta)
	else:
		velocity = velocity.move_toward(Vector3.ZERO, friction * delta)

	move_and_slide()

	if Input.is_action_pressed("ui_accept") and can_shoot:
		shoot()
func receive_payment(amount: int):
	credits += amount
	emit_signal("credits_updated", credits)
func _update_ui_state():
	emit_signal("credits_updated", credits)
	emit_signal("cargo_updated", cargo)
	emit_signal("health_updated", health, max_health)
	emit_signal("fuel_updated", int(fuel), int(max_fuel))
func shoot():
	can_shoot = false

	if bullet_scene:
		var b = bullet_scene.instantiate()
		b.shooter = self
		b.top_level = true
		get_tree().current_scene.add_child(b)
		b.global_transform = global_transform

	await get_tree().create_timer(weapon_cooldown).timeout
	can_shoot = true

func dock_at_station(station_ref):
	is_docked = true
	current_station = station_ref
	emit_signal("shop_toggled", true, station_ref)

func undock():
	is_docked = false
	current_station = null
	emit_signal("shop_toggled", false, null)
func purchase_upgrade(_kind: String, cost: int):
	if credits < cost:
		return false
	credits -= cost
	emit_signal("credits_updated", credits)
	return true
func take_damage(amount: int):
	if amount <= 0:
		return
	health = max(0, health - amount)
	emit_signal("health_updated", health, max_health)

# hook test

# eol test

# eol test


func set_input_enabled(enabled: bool) -> void:
	_input_enabled = enabled



