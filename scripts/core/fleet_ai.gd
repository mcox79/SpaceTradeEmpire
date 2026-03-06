extends Node3D

# GATE.S1.VISUAL_POLISH.FLEET_AI.001 — fleet ship patrol/dock/engage state machine v0.
# Attached to fleet marker Node3D instances created by GalaxyView.cs > CreateFleetMarkerV0.
# This script handles MOVEMENT only. Shooting is owned by game_manager.gd (_ai_fire_v0).

# --- Tunable parameters ---
@export var patrol_speed: float = 6.0            # units/sec during patrol
@export var dock_speed: float = 8.0             # units/sec moving toward station
@export var engage_speed: float = 15.0          # units/sec during engage
@export var patrol_radius: float = 20.0         # waypoint scatter radius around spawn
@export var aggro_range: float = 60.0           # distance to switch to ENGAGE
@export var disengage_range: float = 80.0       # distance to drop out of ENGAGE
@export var dock_arrival_dist: float = 5.0      # distance considered "docked"
@export var dock_chance: float = 0.30           # probability of docking after patrol circuit
@export var idle_min_sec: float = 2.0           # min seconds in IDLE before patrol
@export var idle_max_sec: float = 3.0           # max seconds in IDLE before patrol
@export var dock_pause_min_sec: float = 3.0     # min seconds paused at station
@export var dock_pause_max_sec: float = 5.0     # max seconds paused at station
@export var waypoint_arrival_dist: float = 2.0  # distance considered "waypoint reached"
@export var waypoints_per_circuit: int = 3      # number of waypoints in a patrol loop

# --- State machine ---
enum State { IDLE, PATROL, DOCK, ENGAGE }

var _state: State = State.IDLE
var _is_hostile: bool = false
var _spawn_origin: Vector3 = Vector3.ZERO

# IDLE
var _idle_timer: float = 0.0

# PATROL
var _waypoints: Array = []          # Array of Vector3
var _waypoint_index: int = 0

# DOCK
var _dock_target: Node3D = null
var _dock_pause_timer: float = -1.0  # < 0 = not yet paused

# Internal RNG (seeded from node name for determinism across runs)
var _rng: RandomNumberGenerator = RandomNumberGenerator.new()

func _ready() -> void:
	# Read meta set by GalaxyView.cs at spawn time.
	_is_hostile = bool(get_meta("is_hostile", false))
	# Capture spawn origin from global_position — _ready fires after AddChild assigns the final position.
	# GalaxyView.cs does not set "spawn_origin" meta; we read it here from the live scene transform.
	_spawn_origin = global_position

	# Seed RNG from node name so each fleet gets a unique but repeatable sequence.
	_rng.seed = _name_to_seed(str(name))

	_enter_idle()

func _process(delta: float) -> void:
	match _state:
		State.IDLE:
			_process_idle(delta)
		State.PATROL:
			_process_patrol(delta)
		State.DOCK:
			_process_dock(delta)
		State.ENGAGE:
			_process_engage(delta)

# --- IDLE ---

func _enter_idle() -> void:
	_state = State.IDLE
	_idle_timer = _rng.randf_range(idle_min_sec, idle_max_sec)

func _process_idle(delta: float) -> void:
	_idle_timer -= delta
	if _idle_timer <= 0.0:
		_enter_patrol()

# --- PATROL ---

func _enter_patrol() -> void:
	_state = State.PATROL
	_waypoints = _generate_waypoints(waypoints_per_circuit)
	_waypoint_index = 0

func _process_patrol(delta: float) -> void:
	# Check aggro — only hostile fleets engage.
	if _is_hostile:
		var player := _find_player()
		if player != null:
			var dist: float = global_position.distance_to(player.global_position)
			if dist <= aggro_range:
				_enter_engage()
				return

	if _waypoints.is_empty():
		_enter_idle()
		return

	var target_wp: Vector3 = _waypoints[_waypoint_index]
	_move_toward_point(target_wp, patrol_speed, delta)

	if global_position.distance_to(target_wp) <= waypoint_arrival_dist:
		_waypoint_index += 1
		if _waypoint_index >= _waypoints.size():
			# Completed circuit — maybe dock, else loop.
			if _rng.randf() < dock_chance:
				_enter_dock()
			else:
				_enter_patrol()

# --- DOCK ---

func _enter_dock() -> void:
	_dock_target = _find_nearest_station()
	if _dock_target == null:
		# No station found — just go back to patrol.
		_enter_patrol()
		return
	_state = State.DOCK
	_dock_pause_timer = -1.0

func _process_dock(delta: float) -> void:
	if _dock_target == null or not is_instance_valid(_dock_target):
		_enter_patrol()
		return

	if _dock_pause_timer < 0.0:
		# Approach station.
		var dist: float = global_position.distance_to(_dock_target.global_position)
		if dist <= dock_arrival_dist:
			# Start docking pause.
			_dock_pause_timer = _rng.randf_range(dock_pause_min_sec, dock_pause_max_sec)
		else:
			_move_toward_point(_dock_target.global_position, dock_speed, delta)
	else:
		# Paused at station; count down.
		_dock_pause_timer -= delta
		if _dock_pause_timer <= 0.0:
			_enter_patrol()

# --- ENGAGE ---

func _enter_engage() -> void:
	_state = State.ENGAGE

func _process_engage(delta: float) -> void:
	var player := _find_player()
	if player == null:
		_enter_patrol()
		return

	var dist: float = global_position.distance_to(player.global_position)
	if dist > disengage_range:
		_enter_patrol()
		return

	# GATE.S13.NPC.VISIBLE.001: Orbit at 15-20u instead of flying to 0u.
	var orbit_dist: float = 17.5
	var dir_away: Vector3 = (global_position - player.global_position).normalized()
	if dist < orbit_dist - 2.0:
		# Too close — back away
		_move_toward_point(global_position + dir_away * 5.0, engage_speed, delta)
	elif dist > orbit_dist + 2.0:
		# Too far — close in
		_move_toward_point(player.global_position, engage_speed, delta)
	else:
		# In orbit band — strafe around player
		var tangent := dir_away.cross(Vector3.UP).normalized()
		_move_toward_point(global_position + tangent * 5.0, engage_speed * 0.5, delta)

# --- Movement helper ---

func _move_toward_point(target: Vector3, speed: float, delta: float) -> void:
	var direction: Vector3 = (target - global_position)
	var dist: float = direction.length()
	if dist < 0.01:
		return
	var step: float = speed * delta
	if step >= dist:
		global_position = target
	else:
		global_position += direction.normalized() * step

# --- Scene helpers ---

func _find_player() -> Node3D:
	var tree := get_tree()
	if tree == null:
		return null
	var players := tree.get_nodes_in_group("Player")
	if players.is_empty():
		return null
	var p = players[0]
	if not is_instance_valid(p):
		return null
	return p as Node3D

func _find_nearest_station() -> Node3D:
	var tree := get_tree()
	if tree == null:
		return null
	var stations := tree.get_nodes_in_group("Station")
	if stations.is_empty():
		return null
	var best: Node3D = null
	var best_dist: float = INF
	for s in stations:
		if not is_instance_valid(s):
			continue
		var d: float = global_position.distance_to((s as Node3D).global_position)
		if d < best_dist:
			best_dist = d
			best = s as Node3D
	return best

func _generate_waypoints(count: int) -> Array:
	var pts: Array = []
	for i in range(count):
		var angle: float = _rng.randf() * TAU
		var radius: float = _rng.randf_range(patrol_radius * 0.3, patrol_radius)
		pts.append(_spawn_origin + Vector3(cos(angle) * radius, 0.0, sin(angle) * radius))
	return pts

func _name_to_seed(node_name: String) -> int:
	# FNV-1a 32-bit over the node name characters.
	var h: int = 2166136261
	for i in range(node_name.length()):
		h = h ^ node_name.unicode_at(i)
		h = (h * 16777619) & 0xFFFFFFFF
	return h
