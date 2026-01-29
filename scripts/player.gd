extends CharacterBody3D

# --- CONFIGURATION ---
@export_group('Flight Characteristics')
@export var max_speed: float = 60.0
@export var acceleration: float = 50.0
@export var brake_damping: float = 2.0  # Friction when no input
@export var drift_damping: float = 0.5  # Friction when thrusting
@export var yaw_speed: float = 4.5	  # Turning speed (radians/sec)

# --- STATE ---
var input_enabled: bool = true

# --- SIGNALS ---
signal credits_updated(amount)
signal shop_toggled(is_open, station_ref)

# --- DATA STUBS (For Economy Compatibility) ---
@export var max_fuel: float = 100.0
var fuel: float = 100.0
var credits: int = 1000
var cargo: Dictionary = {}
var cargo_volume: float = 0.0
@export var max_cargo_volume: float = 50.0

func _ready() -> void:
	add_to_group('Player')
	collision_layer = 2
	collision_mask = 1 | 4
	motion_mode = MOTION_MODE_FLOATING
	
	# HARD CONSTRAINT: Lock Verticality (Planar Gameplay)
	# This prevents the physics engine from ever moving us up/down
	axis_lock_linear_y = true
	axis_lock_angular_x = true # No Pitch
	axis_lock_angular_z = true # No Roll

func _physics_process(delta: float) -> void:
	if not input_enabled:
		_apply_passive_physics(delta)
		return

	_handle_planar_flight(delta)
	move_and_slide()

func _handle_planar_flight(delta: float) -> void:
	# 1. ROTATION (Yaw Only)
	# A/D or Left/Right
	var turn := Input.get_axis('ui_right', 'ui_left')
	if turn != 0:
		rotate_y(turn * yaw_speed * delta)

	# 2. THRUST (Forward/Back Only)
	# W/S or Up/Down
	var throttle := Input.get_axis('ui_down', 'ui_up')
	
	# Global Direction Vectors (Planar)
	var forward_dir := -transform.basis.z
	
	# 3. ACCELERATION
	if throttle != 0:
		velocity += forward_dir * throttle * acceleration * delta
	
	# 4. DRAG (Space Friction)
	# High drag if no throttle (Brake), Low drag if thrusting (Drift)
	var current_drag = drift_damping
	if throttle == 0:
		current_drag = brake_damping
	
	velocity = velocity.lerp(Vector3.ZERO, current_drag * delta)
	
	# 5. SPEED CAP
	if velocity.length() > max_speed:
		velocity = velocity.normalized() * max_speed

func _apply_passive_physics(delta: float) -> void:
	velocity = velocity.lerp(Vector3.ZERO, brake_damping * delta)
	move_and_slide()

func set_input_enabled(enabled: bool) -> void:
	input_enabled = enabled

# --- STUBS ---
func dock_at_station(station_node): emit_signal('shop_toggled', true, station_node)
func undock(): emit_signal('shop_toggled', false, null)
func receive_payment(amount: int): credits += amount; emit_signal('credits_updated', credits)
func add_cargo(item_id: String, amount: int) -> bool:
	if not cargo.has(item_id): cargo[item_id] = 0
	cargo[item_id] += amount
	return true
func remove_cargo(item_id: String, amount: int) -> bool:
	if not cargo.has(item_id) or cargo[item_id] < amount: return false
	cargo[item_id] -= amount
	if cargo[item_id] <= 0: cargo.erase(item_id)
	return true
func get_fuel_status() -> float: return fuel