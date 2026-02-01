# Contract Header
# Purpose: Hybrid Hero Unit Controller. Handles Physics (Input) and Sim Projection (Economy).
# Layer: GameShell (Controller)
# Dependencies: SimBridge (Source of Truth)
# Invariants:
#   1. Physics Authority: Local (Input -> Velocity)
#   2. Economic Authority: Remote (SimBridge -> Snapshot -> Local Cache)

extends CharacterBody3D
class_name PlayerShip

# --- FLIGHT MODEL CONFIGURATION ---
@export_group("Flight Characteristics")
@export var max_speed: float = 80.0
@export var acceleration: float = 60.0
@export var brake_damping: float = 1.5
@export var drift_damping: float = 0.2
@export var turn_speed: float = 3.5
@export var bank_angle: float = 25.0

var input_enabled: bool = true
var _current_bank: float = 0.0
var _bridge = null

# --- SIM INTEGRATION ---
signal credits_updated(amount)
signal shop_toggled(is_open, station_ref)

# PROJECTION CACHE (Read-Only from SimCore)
var credits: int = 0
var cargo: Dictionary = {}
var location_node_id: String = ""

# CLIENT-SIDE PHYSICS STATE (Hybrid Model)
var fuel: float = 100.0
var max_fuel: float = 100.0

func _ready() -> void:
	add_to_group("Player")
	collision_layer = 2
	collision_mask = 1 | 4
	axis_lock_linear_y = true
	axis_lock_angular_x = true
	axis_lock_angular_z = true
	
	# Connect to Bridge (Autoload)
	_bridge = get_tree().root.find_child("SimBridge", true, false)
	if _bridge == null:
		push_warning("[PLAYER] SimBridge not found. Economy sync disabled.")

func _physics_process(delta: float) -> void:
	# 1. Sync Economic State (Reactive)
	_sync_sim_state()
	
	if not input_enabled:
		_apply_passive_physics(delta)
		return
	
	# 2. Handle Physics Input (Authoritative)
	_handle_flight_input(delta)
	move_and_slide()

func _sync_sim_state():
	if _bridge == null: return
	if not _bridge.has_method("GetPlayerSnapshot"): return
	
	# Fetch atomic snapshot from C#
	var snapshot = _bridge.GetPlayerSnapshot()
	if snapshot.is_empty(): return
	
	# Update Credits
	var new_credits = int(snapshot.get("credits", 0))
	if new_credits != credits:
		credits = new_credits
		emit_signal("credits_updated", credits)
	
	# Update Cargo
	var raw_cargo = snapshot.get("cargo", null)
	if raw_cargo != null:
		cargo = raw_cargo
	
	# Update Location (Optional for UI)
	location_node_id = str(snapshot.get("location", ""))

func _handle_flight_input(delta: float):
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
	
	# Signal UI to open shop
	# Pass the station object so UI can query it for prices (via Bridge)
	emit_signal("shop_toggled", true, station)

func undock():
	input_enabled = true
	emit_signal("shop_toggled", false, null)

func get_fuel_status(): return fuel
func refuel_full(): fuel = max_fuel
func receive_payment(amount: int): pass # No-op, we wait for Bridge sync