extends CharacterBody3D

# --- FLIGHT MODEL CONFIGURATION ---
@export_group('Flight Characteristics')
@export var max_speed: float = 80.0
@export var acceleration: float = 60.0
@export var brake_damping: float = 1.5
@export var drift_damping: float = 0.2
@export var turn_speed: float = 3.5
@export var bank_angle: float = 25.0

var input_enabled: bool = true
var _current_bank: float = 0.0

# --- STUBS FOR SIM INTEGRATION ---
signal credits_updated(amount)
signal shop_toggled(is_open, station_ref)
var credits: int = 1000
var fuel: float = 100.0
var max_fuel: float = 100.0
var cargo: Dictionary = {}
var cargo_volume: float = 0.0
var max_cargo_volume: float = 50.0

func _ready() -> void:
	add_to_group('Player')
	collision_layer = 2
	collision_mask = 1 | 4
	axis_lock_linear_y = true
	axis_lock_angular_x = true
	axis_lock_angular_z = true

func _physics_process(delta: float) -> void:
	if not input_enabled:
		_apply_passive_physics(delta)
		return

	# HOTWIRE INPUT
	var throttle: float = 0.0
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP): throttle += 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN): throttle -= 1.0

	var turn: float = 0.0
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT): turn += 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT): turn -= 1.0

	if turn != 0: rotate_y(turn * turn_speed * delta)

	var forward = -transform.basis.z
	if throttle != 0:
		velocity += forward * throttle * acceleration * delta
		velocity = velocity.lerp(Vector3.ZERO, drift_damping * delta)
	else:
		velocity = velocity.lerp(Vector3.ZERO, brake_damping * delta)

	if velocity.length() > max_speed:
		velocity = velocity.normalized() * max_speed

	_handle_visual_banking(turn, delta)
	move_and_slide()

func _handle_visual_banking(turn_input: float, delta: float):
	var mesh = $MeshInstance3D
	if not mesh: return
	var target_z = turn_input * deg_to_rad(bank_angle)
	_current_bank = lerp(_current_bank, target_z, delta * 5.0)
	mesh.rotation.z = _current_bank

func _apply_passive_physics(delta: float):
	velocity = velocity.lerp(Vector3.ZERO, brake_damping * delta)
	move_and_slide()

# --- API ---
func set_input_enabled(val: bool): input_enabled = val

func dock_at_station(station):
	velocity = Vector3.ZERO
	input_enabled = false
	emit_signal('shop_toggled', true, station)

func undock():
	input_enabled = true
	emit_signal('shop_toggled', false, null)

func receive_payment(amt):
	credits += amt
	emit_signal('credits_updated', credits)

func get_fuel_status(): return fuel
func add_cargo(id, qty): 
	if not cargo.has(id): cargo[id] = 0
	cargo[id] += qty
	return true
func remove_cargo(id, qty):
	if cargo.get(id, 0) < qty: return false
	cargo[id] -= qty
	if cargo[id] <= 0: cargo.erase(id)
	return true