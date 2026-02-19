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

# Damping is applied as:
# - Lateral damping: kill sideways slip (feels ship-like)
# - Forward damping: mild drag while thrusting, stronger drag while coasting
@export var lateral_damping: float = 6.0
@export var forward_drag_thrusting: float = 0.4
@export var forward_drag_coasting: float = 1.8

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

	if turn != 0.0:
		rotate_y(turn * turn_speed * delta)

	var forward_dir = -global_transform.basis.z
	var right_dir = global_transform.basis.x

	# Decompose velocity into forward + lateral components in ship space
	var v_fwd = forward_dir * velocity.dot(forward_dir)
	var v_lat = right_dir * velocity.dot(right_dir)

	# Thrust affects forward component only (ship-like)
	if throttle != 0.0:
		v_fwd += forward_dir * (throttle * acceleration * delta)

	# Kill lateral slip quickly (space-ish but controllable)
	v_lat = v_lat.lerp(Vector3.ZERO, _exp_lerp_t(lateral_damping, delta))

	# Forward drag: mild while thrusting, stronger while coasting
	var drag = forward_drag_thrusting if throttle != 0.0 else forward_drag_coasting
	v_fwd = v_fwd.lerp(Vector3.ZERO, _exp_lerp_t(drag, delta))

	velocity = v_fwd + v_lat

	# Clamp speed
	var spd = velocity.length()
	if spd > max_speed:
		velocity = velocity * (max_speed / spd)

	_handle_visual_banking(turn, delta)

func _handle_visual_banking(turn_input: float, delta: float):
	var vis := get_node_or_null("ShipVisual") as Node3D
	if vis == null:
		return
	var target_z = turn_input * deg_to_rad(bank_angle)
	_current_bank = lerp(_current_bank, target_z, delta * 5.0)
	vis.rotation.z = _current_bank


func _apply_passive_physics(delta: float):
	# When input is disabled, smoothly come to rest.
	velocity = velocity.lerp(Vector3.ZERO, _exp_lerp_t(2.5, delta))
	move_and_slide()

func _exp_lerp_t(speed: float, delta: float) -> float:
	return 1.0 - exp(-max(0.0, speed) * max(0.0, delta))

# --- API ---
func set_input_enabled(val: bool): input_enabled = val

func dock_at_station(station):
	print("[PLAYER] dock_at_station called with: " + str(station))
	if station != null:
		print("[PLAYER] station name: " + str(station.name))

	velocity = Vector3.ZERO
	input_enabled = false

	# Resolve a canonical id for UI:
	# 1) Legacy stations: get_sim_market_id()
	# 2) Any dock target with meta: sim_market_id
	# 3) Fallback: node name (StarNode is named star_16 etc)
	var id := ""

	if station != null and station.has_method("get_sim_market_id"):
		id = str(station.call("get_sim_market_id"))
	elif station != null and station.has_meta("sim_market_id"):
		id = str(station.get_meta("sim_market_id"))
	elif station != null:
		id = str(station.name)

	# Open menu for ANY valid dock id
	if id != "":
		emit_signal("shop_toggled", true, id)
	else:
		emit_signal("shop_toggled", false, "")

func undock():
	input_enabled = true
	emit_signal("shop_toggled", false, "")

func get_fuel_status(): return fuel
func refuel_full(): fuel = max_fuel
func receive_payment(amount: int): pass # No-op, we wait for Bridge sync
