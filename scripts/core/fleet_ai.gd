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

# GATE.S7.FACTION.PATROL_AGGRO.001: Reputation-based aggro.
# GATE.T30.GALPOP.HOSTILE_FIX.003: Owner ID from fleet meta (preferred over territory lookup).
var _owner_id: String = ""
var _faction_id: String = ""
var _aggro_check_timer: float = 0.0
const AGGRO_CHECK_INTERVAL: float = 2.0
const AGGRO_REPUTATION_THRESHOLD: int = -50

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
	# GATE.T30.GALPOP.HOSTILE_FIX.003: Read owner_id from meta for faction resolution.
	_owner_id = str(get_meta("owner_id", ""))
	# Capture spawn origin from global_position — _ready fires after AddChild assigns the final position.
	# GalaxyView.cs does not set "spawn_origin" meta; we read it here from the live scene transform.
	_spawn_origin = global_position

	# Seed RNG from node name so each fleet gets a unique but repeatable sequence.
	_rng.seed = _name_to_seed(str(name))

	# GATE.S7.FACTION.PATROL_AGGRO.001: Override hardcoded hostility with reputation check.
	if _is_hostile:
		_is_hostile = _check_reputation_aggro()

	# Sync FleetLabel visibility with resolved hostility.
	_update_fleet_label()

	_enter_idle()

func _process(delta: float) -> void:
	# GATE.S7.FACTION.PATROL_AGGRO.001: Periodic reputation re-check (only for originally-hostile ships).
	if bool(get_meta("is_hostile", false)):
		_aggro_check_timer -= delta
		if _aggro_check_timer <= 0.0:
			_aggro_check_timer = AGGRO_CHECK_INTERVAL
			var old_hostile := _is_hostile
			_is_hostile = _check_reputation_aggro()
			if old_hostile != _is_hostile:
				_update_fleet_label()

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
	# GATE.S7.FACTION.PATROL_AGGRO.001: Disengage if no longer hostile (reputation improved).
	if not _is_hostile:
		_enter_patrol()
		return

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

# --- GATE.S7.FACTION.PATROL_AGGRO.001 + GATE.T30.GALPOP.HOSTILE_FIX.003: Reputation aggro helper ---

## Checks player reputation with this fleet's faction.
## Returns true if reputation is below aggro threshold.
## Default: non-hostile (safe fallback). Hostility only when reputation is explicitly bad.
func _check_reputation_aggro() -> bool:
	var tree := get_tree()
	if tree == null:
		return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: No tree = non-hostile (safe default)
	var bridge := tree.root.get_node_or_null("SimBridge")
	if bridge == null:
		return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: No bridge = non-hostile

	# GATE.T30.GALPOP.HOSTILE_FIX.003: Use owner_id for faction, not territory.
	if not _owner_id.is_empty():
		_faction_id = _owner_id
	elif _faction_id.is_empty():
		# Fallback: resolve faction from territory if no owner_id.
		var fleet_id: String = str(get_meta("fleet_id", ""))
		if not fleet_id.is_empty() and bridge.has_method("GetTerritoryAccessV0"):
			# Determine which node this fleet is at. GameManager tracks current_system_id.
			var gm := tree.root.get_node_or_null("GameManager")
			var node_id: String = ""
			if gm != null and gm.get("current_system_id") != null:
				node_id = str(gm.get("current_system_id"))
			if not node_id.is_empty():
				var territory: Dictionary = bridge.call("GetTerritoryAccessV0", node_id)
				var fid: String = territory.get("faction_id", "")
				if not fid.is_empty():
					_faction_id = fid

	if _faction_id.is_empty():
		return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: No faction = non-hostile

	if bridge.has_method("GetPlayerReputationV0"):
		var rep_data: Dictionary = bridge.call("GetPlayerReputationV0", _faction_id)
		var reputation: int = rep_data.get("reputation", 0)
		return reputation < AGGRO_REPUTATION_THRESHOLD

	return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: Fallback non-hostile


# --- FleetLabel visibility ---

func _update_fleet_label() -> void:
	var label := get_node_or_null("FleetLabel") as Label3D
	if label != null:
		label.visible = _is_hostile


# --- Movement helper ---

func _move_toward_point(target: Vector3, speed: float, delta: float) -> void:
	var direction: Vector3 = (target - global_position)
	direction.y = 0.0
	var dist: float = direction.length()
	if dist < 0.01:
		_apply_managed_y(delta)
		return
	var move_dir := direction.normalized()

	# XZ avoidance: blend away from stations and other ships.
	var avoid := _compute_xz_avoidance()
	if avoid.length_squared() > 0.001:
		move_dir = (move_dir + avoid).normalized()

	var step: float = speed * delta
	if step >= dist:
		global_position.x = target.x
		global_position.z = target.z
	else:
		global_position += Vector3(move_dir.x, 0.0, move_dir.z) * step
	_apply_managed_y(delta)


## Managed Y: lift over planets, return to flight plane when clear.
func _apply_managed_y(delta: float) -> void:
	var target_y: float = 0.0
	var tree := get_tree()
	if tree:
		for planet in tree.get_nodes_in_group("PlanetBody"):
			var to_planet := planet.global_position - global_position
			to_planet.y = 0.0
			var dist := to_planet.length()
			var avoid_r: float = planet.get_meta("avoidance_radius", 12.0)
			if dist < avoid_r and dist > 0.1:
				var visual_r: float = planet.get_meta("visual_radius", 8.0)
				var lift: float = visual_r + 3.0
				var t: float = 1.0 - dist / avoid_r
				target_y = maxf(target_y, lift * t * t * t + lift * 0.3 * (1.0 - t))
	global_position.y = lerpf(global_position.y, target_y, clampf(4.0 * delta, 0.0, 1.0))


## XZ avoidance: repulsion from stations and other ships.
func _compute_xz_avoidance() -> Vector3:
	var avoidance := Vector3.ZERO
	var tree := get_tree()
	if tree == null:
		return avoidance
	for station in tree.get_nodes_in_group("Station"):
		var to_station := station.global_position - global_position
		to_station.y = 0.0
		var dist := to_station.length()
		var avoid_r: float = station.get_meta("avoidance_radius", 8.0)
		if dist < avoid_r and dist > 0.1:
			var t: float = 1.0 - dist / avoid_r
			avoidance -= to_station.normalized() * t * t * 0.5
	return avoidance

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
