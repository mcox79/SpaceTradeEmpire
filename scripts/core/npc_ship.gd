extends CharacterBody3D
## NPC ship controller — GATE.S16.NPC_ALIVE.SHIP_SCENE.001 + FLIGHT_CTRL.001
## Sim-driven movement: reads transit facts from SimBridge, interpolates position.
## Kinematic flight via move_and_slide + quaternion slerp rotation. XZ locked.

## Fleet ID assigned at spawn by GalaxyView.
@export var fleet_id: String = ""

## Cached transit data from SimBridge.
var _role: int = 0  # 0=Trader, 1=Hauler, 2=Patrol
var _hull_hp: int = 0
var _hull_hp_max: int = 0
var _has_been_hit: bool = false  # FEEL_POST_FIX_6: Show HP bar on any damage (shield or hull).
var _is_hostile: bool = false
var _travel_progress: float = 0.0
var _fleet_state: String = "Idle"

## GATE.S7.FACTION.PATROL_AGGRO.001: Reputation-based aggro state.
## GATE.T30.GALPOP.HOSTILE_FIX.003: Owner ID from transit data (preferred over territory lookup).
var _owner_id: String = ""
var _faction_id: String = ""
var _aggro_check_timer: float = 0.0
const AGGRO_CHECK_INTERVAL: float = 2.0  # seconds between reputation polls
const AGGRO_REPUTATION_THRESHOLD: int = -50  # matches FactionTweaksV0.AggroReputationThreshold

## Flight controller state.
var target_position: Vector3 = Vector3.ZERO
var target_speed: float = 6.0
var stagger_remaining: float = 0.0
var _current_speed: float = 0.0  # Actual speed (builds up via acceleration)
var _departing: bool = false      # Set true when flying to gate for warp-out.
var _departure_gate_pos: Vector3 = Vector3.ZERO
var _waiting_at_gate: bool = false  # Holding position while gate is busy.
var _warp_out_started: bool = false  # True after warp-out VFX triggered — freeze movement.

## Gate departure queue — only one ship approaches each gate at a time.
## Key = rounded gate position string, Value = fleet_id of the ship with reservation.
static var _gate_reservations: Dictionary = {}
## Gate warp cooldown — minimum time between consecutive warps at any gate.
static var _last_gate_warp_msec: int = 0
const GATE_WARP_COOLDOWN_MS: int = 2000  # 2.0 seconds between gate warps.
const GATE_HOLD_RADIUS: float = 12.0     # Orbit radius while waiting for gate turn.

## Rotation smoothing (higher = snappier turn). Tuned for gradual arcing turns.
@export var rotation_sharpness: float = 2.5

## Thrust physics — ships accelerate in their facing direction, creating natural arcs.
const ACCELERATION: float = 4.0       # Units/s² — how fast ships build speed
const DECELERATION: float = 6.0       # Units/s² — braking rate when approaching target
const BRAKE_DISTANCE: float = 12.0    # Start decelerating this far from target
const MIN_DRIFT_SPEED: float = 0.3    # Minimum creep speed (prevents dead stops mid-turn)

## Stop moving when closer than this to target.
const ARRIVAL_THRESHOLD: float = 1.5

## Orbit mode — smooth continuous circular orbit driven per-frame.
var _orbiting: bool = false
var _orbit_radius: float = 20.0
var _orbit_angular_speed: float = 0.1
var _orbit_angle: float = 0.0  # Current angle on orbit circle (radians).

## Star avoidance — ships steer around the star (at local origin) instead of flying through it.
const STAR_AVOID_RADIUS: float = 25.0  # Minimum clearance from star center
## Binary exclusion zone — set by GalaxyView at spawn for binary systems.
## Extends star avoidance to cover the full binary orbit envelope.
var binary_exclusion_zone: float = 0.0

## Obstacle avoidance — Y-lift over planets, XZ steering around stations/ships.
var _target_y: float = 0.0  # Managed altitude (0 = flight plane, >0 = lifting over planet).

## Patrol seed (used for deterministic initial position).
var _patrol_seed: int = 0

## GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001: Track previous shield state for break detection.
var _prev_shield_remaining: int = -1  # -1 = not yet set

## Visual node references.
@onready var _ship_visual: Node3D = $ShipVisual
@onready var _fleet_area: Area3D = $FleetArea

## Model loaded flag.
var _model_loaded: bool = false
var _cached_role: int = 0

## GATE.S7.FACTION_VIS.SHIP_LIVERY.001: Faction tint color (set at spawn).
var _faction_color: Color = Color.WHITE

## GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001: Status overlay nodes.
var _role_label: Label3D = null
var _hostile_label: Label3D = null  # GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001: Dedicated hostile tag
## M2: Onboarding label suppression — hides role/hostile labels until player explores.
## Defaults to true (hidden) — HUD disclosure poll sets to false once nodes_visited >= 2.
var _onboard_labels_hidden: bool = true
var _hp_bar: MeshInstance3D = null
var _hp_bar_mat: StandardMaterial3D = null
const ROLE_NAMES := ["Trader", "Hauler", "Patrol"]
const ROLE_COLORS := [
	Color(1.0, 0.85, 0.3),   # Trader — gold
	Color(0.4, 0.8, 0.8),    # Hauler — teal
	Color(0.5, 0.7, 1.0),    # Patrol — blue
]
const LABEL_SHOW_DIST := 30.0  # Only show role label when player is close (was 160)
const HP_BAR_HEIGHT := 8.0  # Above ship center (raised for altitude visibility)


func _exit_tree() -> void:
	# Release gate reservation if this ship is removed (combat death, system change, etc.).
	if _departing or _waiting_at_gate:
		_release_gate_reservation()

func _ready() -> void:
	_fleet_area.set_meta("fleet_id", fleet_id)
	add_to_group("NpcShip")
	collision_layer = 4
	collision_mask = 0
	print("DEBUG_NPC|SPAWN|fleet=%s pos=%s role=%d" % [fleet_id, str(position), _role])
	# Seed patrol RNG from fleet ID so each ship patrols differently.
	for c in fleet_id:
		_patrol_seed = _patrol_seed * 31 + c.unicode_at(0)
	_create_status_display()


## GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001 + GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001:
## Create role label + hostile label + HP bar overlay. Sized for camera altitude ~120.
func _create_status_display() -> void:
	# FleetPip sphere removed — was placeholder programmer art.

	# Role label — small, subtle. Only visible on close proximity (< 30u).
	# Ships in space don't broadcast their type — player discovers by getting close.
	_role_label = Label3D.new()
	_role_label.name = "RoleLabel"
	_role_label.pixel_size = 0.04
	_role_label.font_size = 36
	_role_label.outline_size = 8
	_role_label.outline_modulate = Color(0, 0, 0, 0.7)
	_role_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_role_label.no_depth_test = true
	_role_label.render_priority = 10
	_role_label.position = Vector3(5.0, HP_BAR_HEIGHT + 3.0, 0)
	_role_label.modulate = Color(0.7, 0.75, 0.8, 0.8)
	_role_label.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_role_label.visible = false  # Hidden until player is close.
	add_child(_role_label)

	# Hostile label — NOT created at spawn. Only created lazily for patrol ships
	# that actually become hostile. Prevents phantom HOSTILE labels on non-patrols.

	# HP bar — thin box mesh scaled by HP ratio. Enlarged for altitude ~80 visibility.
	_hp_bar = MeshInstance3D.new()
	_hp_bar.name = "HpBar"
	var box := BoxMesh.new()
	box.size = Vector3(8.0, 0.6, 0.1)
	_hp_bar.mesh = box
	_hp_bar_mat = StandardMaterial3D.new()
	_hp_bar_mat.albedo_color = Color(0.2, 0.8, 0.2)
	_hp_bar_mat.emission_enabled = true
	_hp_bar_mat.emission = Color(0.2, 0.8, 0.2)
	_hp_bar_mat.emission_energy_multiplier = 5.0
	_hp_bar_mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	_hp_bar_mat.no_depth_test = true
	_hp_bar_mat.render_priority = 10
	_hp_bar.material_override = _hp_bar_mat
	_hp_bar.position = Vector3(0, HP_BAR_HEIGHT, 0)
	_hp_bar.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_hp_bar.visible = false  # Only show when damaged
	add_child(_hp_bar)

	_update_status_display()


func _update_status_display() -> void:
	if _role_label:
		var role_idx: int = clampi(_role, 0, 2)
		_role_label.text = ROLE_NAMES[role_idx]
		if _is_hostile:
			_role_label.modulate = Color(1.0, 0.4, 0.35)
		else:
			_role_label.modulate = ROLE_COLORS[role_idx]

	# Hostile label: only exists on patrol ships that are hostile.
	# Lazy create: if patrol becomes hostile, create the label. Otherwise never created.
	var should_show_hostile: bool = _is_hostile and _role == 2
	if _is_hostile and _role != 2:
		print("DEBUG_HOSTILE|DISPLAY_BUG|fleet=%s role=%d _is_hostile=true but role!=2 — forcing false" % [fleet_id, _role])
		_is_hostile = false
		set_meta("is_hostile", false)
		should_show_hostile = false
	if should_show_hostile and _hostile_label == null:
		_hostile_label = Label3D.new()
		_hostile_label.name = "HostileLabel"
		_hostile_label.pixel_size = 0.07
		_hostile_label.font_size = 48
		_hostile_label.outline_size = 10
		_hostile_label.outline_modulate = Color(0, 0, 0, 0.9)
		_hostile_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		_hostile_label.no_depth_test = true
		_hostile_label.render_priority = 10
		_hostile_label.position = Vector3(0, HP_BAR_HEIGHT + 5.0, 0)
		_hostile_label.text = "HOSTILE"
		_hostile_label.modulate = Color(1.0, 0.2, 0.15)
		_hostile_label.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		add_child(_hostile_label)
		print("DEBUG_HOSTILE|LABEL_CREATED|fleet=%s role=%d" % [fleet_id, _role])
	if _hostile_label:
		_hostile_label.visible = should_show_hostile

	# FleetPip coloring removed — was placeholder sphere.

	if _hp_bar and _hull_hp_max > 0:
		var ratio := clampf(float(_hull_hp) / float(_hull_hp_max), 0.0, 1.0)
		var should_show := _has_been_hit or ratio < 0.999  # FEEL_POST_FIX_6: Show bar on any hit (shield or hull).
		if _hp_bar.visible != should_show:
			_hp_bar.visible = should_show
		if should_show:
			_hp_bar.scale = Vector3(ratio, 1.0, 1.0)
			# Color: green > yellow > red with higher emission for altitude visibility.
			var new_color: Color
			if ratio > 0.5:
				new_color = Color(0.2, 0.9, 0.2)
			elif ratio > 0.25:
				new_color = Color(1.0, 0.9, 0.1)
			else:
				new_color = Color(1.0, 0.2, 0.1)
			if _hp_bar_mat.albedo_color != new_color:
				_hp_bar_mat.albedo_color = new_color
				_hp_bar_mat.emission = new_color

	# Distance-based visibility for labels.
	var players := get_tree().get_nodes_in_group("Player") if get_tree() else []
	var show_label := false
	for p in players:
		if p is Node3D and global_position.distance_to(p.global_position) < LABEL_SHOW_DIST:
			show_label = true
			break
	# M2: Suppress NPC labels until player has explored (nodes_visited >= 2).
	# Reduces visual clutter at boot — new players don't need "T/H/P" role codes yet.
	if show_label and _onboard_labels_hidden:
		show_label = false
	if _role_label:
		_role_label.visible = show_label
	# Hostile label: visible when in range AND hostile AND patrol.
	if _hostile_label:
		_hostile_label.visible = show_label and _is_hostile and _role == 2


## GATE.S16.NPC_ALIVE.BT_ROLES.001: Attach behavior tree for this ship's role.
## Called by spawn system after role is known.
## Guarded: requires LimboAI addon. Without it, NPC uses sim-driven kinematic flight.
func attach_behavior_tree(role: int) -> void:
	_role = role
	if not ClassDB.class_exists(&"BTPlayer"):
		return
	var bt_player = ClassDB.instantiate(&"BTPlayer")
	if bt_player == null:
		return
	bt_player.name = "BTPlayer"
	var builder_script = load("res://scripts/npc/npc_bt_builder.gd")
	if builder_script:
		bt_player.set("behavior_tree", builder_script.call("build_for_role", role))
	var bb = bt_player.get("blackboard")
	if bb:
		bb.call("set_var", "fleet_id", fleet_id)
		bb.call("set_var", "move_speed", target_speed)
		bb.call("set_var", "target_pos", global_position)
	add_child(bt_player)


## Called by the spawn system to build ship model by role.
## role: 0=trader, 1=hauler, 2=patrol. May be called before _ready().
func load_model_v1(role_int: int) -> void:
	_cached_role = role_int
	if _ship_visual == null:
		_ship_visual = get_node_or_null("ShipVisual")
	if _ship_visual == null:
		return
	for child in _ship_visual.get_children():
		child.queue_free()
	# Hash fleet_id for deterministic model variety.
	var h: int = 0
	for c in fleet_id:
		h = h * 31 + c.unicode_at(0)
	var instance := ShipMeshBuilder.build_ship(role_int, _faction_color, h)
	_ship_visual.add_child(instance)
	_model_loaded = true


## Legacy: accept PackedScene (kept for backward compat, builds procedural instead).
func load_model(_model_scene: PackedScene) -> void:
	load_model_v1(0)  # Default to trader shape.


## GATE.S7.FACTION_VIS.SHIP_LIVERY.001: Set faction color and apply tint to loaded model.
func set_faction_color(color: Color) -> void:
	_faction_color = color
	if not _model_loaded:
		return
	# Rebuild with new faction color baked into hull material.
	load_model_v1(_cached_role)


## Apply faction tint to all MeshInstance3D children of a model node.
func _apply_faction_tint(node: Node) -> void:
	if node is MeshInstance3D:
		var mi := node as MeshInstance3D
		var mat := mi.get_active_material(0)
		if mat is StandardMaterial3D:
			var tinted := mat.duplicate() as StandardMaterial3D
			# Blend faction color with existing albedo (modulate).
			tinted.albedo_color = tinted.albedo_color.lerp(_faction_color, 0.4)
			mi.material_override = tinted
	for child in node.get_children():
		_apply_faction_tint(child)


## Called by the spawn system each poll to update sim-driven state.
func update_transit(data: Dictionary) -> void:
	var old_state := _fleet_state
	_role = data.get("role", 0)
	_hull_hp = data.get("hull_hp", 0)
	_hull_hp_max = data.get("hull_hp_max", 0)
	_travel_progress = data.get("travel_progress", 0.0)
	_fleet_state = data.get("state", "Idle")
	var _dest_node: String = data.get("destination_node_id", "")
	var _final_dest: String = data.get("final_destination_node_id", "")
	var _current_task: String = data.get("current_task", "Idle")
	if old_state != _fleet_state:
		print("DEBUG_NPC|TRANSIT|fleet=%s state=%s→%s role=%d task=%s dest=%s final=%s" % [fleet_id, old_state, _fleet_state, _role, _current_task, _dest_node, _final_dest])
	## GATE.T30.GALPOP.HOSTILE_FIX.003: Read owner_id from transit data for faction resolution.
	_owner_id = data.get("owner_id", "")

	## GATE.S7.FACTION.PATROL_AGGRO.001: Resolve hostility from faction reputation.
	## Only Patrol ships (role 2) can ever be hostile. Haulers/Traders are always non-hostile.
	var base_hostile: bool = data.get("is_hostile", false)
	# DEBUG: Trace hostility resolution for all ships.
	if base_hostile and _role != 2:
		print("DEBUG_HOSTILE|BUG|fleet=%s role=%d base_hostile=true — forcing non-hostile (only patrols can be hostile)" % [fleet_id, _role])
	if _role == 2 and base_hostile:  # Patrol only
		var current_node: String = data.get("current_node_id", "")
		_is_hostile = _check_reputation_aggro(current_node)
		print("DEBUG_HOSTILE|PATROL|fleet=%s hostile=%s owner=%s faction=%s" % [fleet_id, str(_is_hostile), _owner_id, _faction_id])
	else:
		_is_hostile = false
	# Sync meta so game_manager._ai_fire_v0() reads current hostility.
	set_meta("is_hostile", _is_hostile)
	_update_status_display()


## GATE.S7.FACTION.PATROL_AGGRO.001 + GATE.T30.GALPOP.HOSTILE_FIX.003:
## Check player reputation with the fleet's faction.
## Returns true if reputation is below aggro threshold (player is hostile to this faction).
## Default: non-hostile (safe fallback). Hostility only when reputation is explicitly bad.
func _check_reputation_aggro(current_node: String) -> bool:
	var bridge := get_node_or_null("/root/SimBridge")
	if bridge == null:
		return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: No bridge = non-hostile (safe default)

	# GATE.T30.GALPOP.HOSTILE_FIX.003: Use fleet owner_id for faction, not territory.
	if not _owner_id.is_empty():
		_faction_id = _owner_id
	elif not current_node.is_empty() and bridge.has_method("GetTerritoryAccessV0"):
		# Fallback: resolve faction from territory if no owner_id.
		var territory: Dictionary = bridge.call("GetTerritoryAccessV0", current_node)
		var fid: String = territory.get("faction_id", "")
		if not fid.is_empty():
			_faction_id = fid

	# If no faction resolved, default NON-hostile.
	if _faction_id.is_empty():
		return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: was true, now false

	# Query player reputation with this faction.
	if bridge.has_method("GetPlayerReputationV0"):
		var rep_data: Dictionary = bridge.call("GetPlayerReputationV0", _faction_id)
		var reputation: int = rep_data.get("reputation", 0)
		return reputation < AGGRO_REPUTATION_THRESHOLD

	return false  # GATE.T30.GALPOP.HOSTILE_FIX.003: Fallback non-hostile if bridge method missing


func _physics_process(delta: float) -> void:
	# Warp-out in progress — freeze all movement, let tween handle scale-down + queue_free.
	if _warp_out_started:
		velocity = Vector3.ZERO
		move_and_slide()
		return

	# GATE.S7.FACTION.PATROL_AGGRO.001: Periodic reputation re-check for patrol ships.
	if _role == 2:  # Patrol
		_aggro_check_timer -= delta
		if _aggro_check_timer <= 0.0:
			_aggro_check_timer = AGGRO_CHECK_INTERVAL
			var old_hostile := _is_hostile
			_is_hostile = _check_reputation_aggro("")
			if old_hostile != _is_hostile:
				set_meta("is_hostile", _is_hostile)
				_update_status_display()

	# Combat stagger — freeze movement.
	if stagger_remaining > 0.0:
		stagger_remaining -= delta
		velocity = Vector3.ZERO
		move_and_slide()
		return

	# Orbit mode: advance angle per-frame for smooth circular motion.
	if _orbiting and not _departing:
		_orbit_angle += _orbit_angular_speed * delta
		# Target is a small lead ahead on the circle — ship chases it smoothly.
		var lead_angle := _orbit_angle + 0.4
		target_position = Vector3(cos(lead_angle) * _orbit_radius, 0.0, sin(lead_angle) * _orbit_radius)
		target_speed = _orbit_radius * _orbit_angular_speed * 1.2  # Slightly faster than orbit to keep up.

	# Direction to target (XZ only).
	# Use local `position` — targets are in local system coordinates (relative
	# to _localSystemRoot which sits at the star's galactic position).
	var to_target := target_position - position
	to_target.y = 0.0
	var dist := to_target.length()

	# Waiting NPC that hasn't started departing yet — retry gate reservation each frame.
	if _waiting_at_gate and not _departing and _departure_gate_pos != Vector3.ZERO:
		if _try_reserve_gate(_departure_gate_pos):
			# Got the reservation — start actual departure.
			_departing = true
			_waiting_at_gate = false
			_orbiting = false
			target_position = Vector3(_departure_gate_pos.x, 0.0, _departure_gate_pos.z)
			target_speed = 8.0

	if dist < ARRIVAL_THRESHOLD:
		# Departing ship reached gate — check warp cooldown, then warp out.
		if _departing:
			var now_ms: int = Time.get_ticks_msec()
			if now_ms - _last_gate_warp_msec < GATE_WARP_COOLDOWN_MS:
				# Warp cooldown active — orbit near gate briefly.
				var hold_angle := float(now_ms) / 2000.0 + float(_patrol_seed % 360) * 0.0174533
				var hold_pos := _departure_gate_pos + Vector3(cos(hold_angle) * GATE_HOLD_RADIUS, 0.0, sin(hold_angle) * GATE_HOLD_RADIUS)
				target_position = Vector3(hold_pos.x, 0.0, hold_pos.z)
			else:
				# Gate is clear — warp out! Release reservation for next ship.
				_last_gate_warp_msec = now_ms
				_waiting_at_gate = false
				_warp_out_started = true
				_release_gate_reservation()
				print("DEBUG_NPC|WARP_OUT|fleet=%s at_gate=%s" % [fleet_id, str(position).substr(0,25)])
				var warp_script = load("res://scripts/vfx/warp_effect.gd")
				if warp_script and get_parent():
					warp_script.call("play_warp_out", self)
				else:
					queue_free()
				return
		else:
			# Non-departing ship at target — decelerate to stop.
			_current_speed = maxf(_current_speed - DECELERATION * delta, 0.0)
			if _current_speed < 0.1:
				_current_speed = 0.0
				velocity = Vector3.ZERO
				move_and_slide()
				return
			# Drift forward in facing direction while decelerating.
			var drift_dir := -transform.basis.z
			drift_dir.y = 0.0
			if drift_dir.length_squared() > 0.001:
				drift_dir = drift_dir.normalized()
			velocity = drift_dir * _current_speed
			velocity.y = 0.0
			move_and_slide()
			_apply_managed_y(delta)
			return

	var desired_dir := to_target / dist

	# Star avoidance: re-enabled for binary systems where ships must avoid the
	# exclusion zone. Solo systems keep it disabled (oscillation issue at 25u).
	if binary_exclusion_zone > 0.0:
		desired_dir = _apply_star_avoidance_local(desired_dir, dist)

	# Smooth rotation toward desired direction (ship forward = -Z).
	# Lower rotation_sharpness = wider arcing turns.
	# Boost rotation when facing away from target to avoid slow jittery 180° turns.
	if desired_dir.length_squared() > 0.001:
		var target_basis := Basis.looking_at(desired_dir, Vector3.UP)
		var current_quat := transform.basis.get_rotation_quaternion()
		var target_quat := target_basis.get_rotation_quaternion()
		var raw_align := (-transform.basis.z).normalized().dot(desired_dir)
		var turn_boost := 1.0 if raw_align > 0.0 else lerpf(3.0, 1.0, clampf(raw_align + 1.0, 0.0, 1.0))
		var weight := clampf(rotation_sharpness * turn_boost * delta, 0.0, 1.0)
		transform.basis = Basis(current_quat.slerp(target_quat, weight))

	# Thrust in facing direction (not directly at target) — creates natural arcs.
	var facing := -transform.basis.z
	facing.y = 0.0
	if facing.length_squared() > 0.001:
		facing = facing.normalized()

	# How aligned are we with the target? Reduce thrust when facing away (prevents overshoot on turns).
	var alignment := clampf(facing.dot(desired_dir), 0.0, 1.0)

	# Target speed modulation: brake near target, reduce when misaligned.
	var desired_speed := target_speed
	if dist < BRAKE_DISTANCE:
		# Smooth deceleration as we approach.
		desired_speed *= clampf(dist / BRAKE_DISTANCE, 0.1, 1.0)
	desired_speed *= lerpf(0.3, 1.0, alignment)  # Slow in sharp turns
	desired_speed = maxf(desired_speed, MIN_DRIFT_SPEED)

	# Accelerate/decelerate toward desired speed.
	if _current_speed < desired_speed:
		_current_speed = minf(_current_speed + ACCELERATION * delta, desired_speed)
	else:
		_current_speed = maxf(_current_speed - DECELERATION * delta, desired_speed)

	# XZ obstacle avoidance: blend away from stations and other ships.
	var avoid_xz := _compute_xz_avoidance()
	var final_dir := (facing + avoid_xz).normalized() if avoid_xz.length_squared() > 0.001 else facing
	velocity = final_dir * _current_speed
	velocity.y = 0.0
	move_and_slide()
	_apply_managed_y(delta)


## Managed Y: lift over planets, return to flight plane when clear.
func _apply_managed_y(delta: float) -> void:
	_target_y = 0.0
	for planet in get_tree().get_nodes_in_group("PlanetBody"):
		var p_node: Node3D = planet as Node3D
		if p_node == null:
			continue
		var to_planet := Vector3(p_node.global_position.x - global_position.x, 0.0, p_node.global_position.z - global_position.z)
		var dist: float = to_planet.length()
		var avoid_r: float = p_node.get_meta("avoidance_radius", 12.0)
		if dist < avoid_r and dist > 0.1:
			var visual_r: float = p_node.get_meta("visual_radius", 8.0)
			var lift: float = visual_r + 3.0
			# Scale lift by proximity (cubic ramp — gentle at edge).
			var t: float = 1.0 - dist / avoid_r
			_target_y = maxf(_target_y, lift * t * t * t + lift * 0.3 * (1.0 - t))
	position.y = lerpf(position.y, _target_y, clampf(4.0 * delta, 0.0, 1.0))


## XZ avoidance: repulsion from stations and other ships.
func _compute_xz_avoidance() -> Vector3:
	var avoidance := Vector3.ZERO
	# Stations.
	for station in get_tree().get_nodes_in_group("Station"):
		var s_node: Node3D = station as Node3D
		if s_node == null:
			continue
		var to_station := Vector3(s_node.global_position.x - global_position.x, 0.0, s_node.global_position.z - global_position.z)
		var dist: float = to_station.length()
		var avoid_r: float = s_node.get_meta("avoidance_radius", 8.0)
		if dist < avoid_r and dist > 0.1:
			var t: float = 1.0 - dist / avoid_r
			avoidance -= to_station.normalized() * t * t * 0.6
	# Other NPC ships.
	for ship in get_tree().get_nodes_in_group("NpcShip"):
		if ship == self:
			continue
		var sh_node: Node3D = ship as Node3D
		if sh_node == null:
			continue
		var to_ship := Vector3(sh_node.global_position.x - global_position.x, 0.0, sh_node.global_position.z - global_position.z)
		var dist: float = to_ship.length()
		if dist < 10.0 and dist > 0.1:
			var t: float = 1.0 - dist / 10.0
			avoidance -= to_ship.normalized() * t * t * 0.4
	# Player ship.
	for player in get_tree().get_nodes_in_group("Player"):
		# Skip player avoidance during ENGAGE state (combat approach is intentional).
		if _fleet_state == "Engaging" or _fleet_state == "Attacking":
			continue
		var pl_node: Node3D = player as Node3D
		if pl_node == null:
			continue
		var to_player := Vector3(pl_node.global_position.x - global_position.x, 0.0, pl_node.global_position.z - global_position.z)
		var dist: float = to_player.length()
		if dist < 8.0 and dist > 0.1:
			var t: float = 1.0 - dist / 8.0
			avoidance -= to_player.normalized() * t * t * 0.4
	return avoidance


## Steer around the star (at local origin) if the direct path passes too close.
## All positions are in local coordinates — star center is (0,0,0).
func _apply_star_avoidance_local(dir: Vector3, dist_to_target: float) -> Vector3:
	var avoid_radius := maxf(STAR_AVOID_RADIUS, binary_exclusion_zone)
	var to_star := -position  # Star is at origin, so direction = (0,0,0) - position
	to_star.y = 0.0

	# Project star onto the travel direction to find closest approach.
	var dot := to_star.dot(dir)
	# Only avoid if the star is ahead of us (not behind).
	if dot < 0.0 or dot > dist_to_target:
		return dir

	# Perpendicular distance from star center to our travel line.
	var closest_point := position + dir * dot
	var perp := -closest_point  # Star at origin
	perp.y = 0.0
	var perp_dist := perp.length()

	if perp_dist >= avoid_radius:
		return dir  # Path clears the star.

	# Steer perpendicular to the travel line, away from the star.
	var avoid_dir: Vector3
	if perp_dist < 0.1:
		# Headed dead center — pick a consistent side based on position.
		avoid_dir = Vector3(-dir.z, 0.0, dir.x)
	else:
		avoid_dir = -perp.normalized()  # Away from star center

	# Blend: more avoidance when closer to the star.
	var urgency := 1.0 - clampf(perp_dist / avoid_radius, 0.0, 1.0)
	var blended := (dir + avoid_dir * urgency * 2.0).normalized()
	return blended


## Gate key for reservation dictionary (rounded position to group near-same gates).
static func _gate_key(gate_pos: Vector3) -> String:
	return "%d_%d" % [int(gate_pos.x), int(gate_pos.z)]

## Try to reserve a gate for departure. Returns true if reservation acquired.
func _try_reserve_gate(gate_pos: Vector3) -> bool:
	var key := _gate_key(gate_pos)
	if _gate_reservations.has(key):
		var holder: String = str(_gate_reservations[key])
		if holder != fleet_id:
			return false  # Another ship has the reservation.
	_gate_reservations[key] = fleet_id
	return true

## Release gate reservation (called after warp-out or if departure is cancelled).
func _release_gate_reservation() -> void:
	var key := _gate_key(_departure_gate_pos)
	if _gate_reservations.has(key) and str(_gate_reservations[key]) == fleet_id:
		_gate_reservations.erase(key)

## Begin departure sequence: fly to gate, then warp out on arrival.
## If gate is occupied by another departing ship, stays in orbit and retries.
func begin_departure_v0(gate_pos: Vector3) -> void:
	if _departing:
		return  # Already departing — don't spam.
	_departure_gate_pos = gate_pos
	# Try to reserve the gate. If busy, mark as waiting (stay in orbit).
	if not _try_reserve_gate(gate_pos):
		_waiting_at_gate = true
		return
	_departing = true
	_waiting_at_gate = false
	_orbiting = false  # Exit orbit — head straight for gate.
	target_position = Vector3(gate_pos.x, 0.0, gate_pos.z)
	target_speed = 8.0  # Faster than normal — heading for the exit.
	print("DEBUG_NPC|DEPART|fleet=%s gate=%s" % [fleet_id, str(gate_pos).substr(0,25)])


## Enter orbit mode: ship circles the star at given radius and angular speed.
## Orbit angle advances per-frame in _physics_process — no polling jitter.
func set_orbit_v0(radius: float, angular_speed: float) -> void:
	if not _orbiting:
		# Initialize angle from current position so ship doesn't snap.
		_orbit_angle = atan2(position.z, position.x)
	_orbiting = true
	_orbit_radius = radius
	_orbit_angular_speed = angular_speed
	# Set target ahead on circle so ship starts moving immediately.
	var lead_angle := _orbit_angle + 0.4  # ~23° ahead — gentle lead.
	target_position = Vector3(cos(lead_angle) * radius, 0.0, sin(lead_angle) * radius)


## Exit orbit mode (e.g. when ship starts departing).
func stop_orbit_v0() -> void:
	_orbiting = false


## Apply combat stagger (seconds of movement freeze).
func apply_stagger(duration: float) -> void:
	stagger_remaining += duration


## Set movement target from world position.
func set_target(pos: Vector3, spd: float) -> void:
	var old_target := target_position
	target_position = Vector3(pos.x, 0.0, pos.z)
	target_speed = spd
	var moved := old_target.distance_to(target_position) > 1.0
	if moved:
		print("DEBUG_NPC|TARGET|fleet=%s from=%s to=%s spd=%.1f state=%s dist=%.0f" % [fleet_id, str(old_target).substr(0,20), str(target_position).substr(0,20), spd, _fleet_state, position.distance_to(target_position)])
	# Snap facing toward target on first call (ship hasn't moved yet) so NPC
	# ships don't all spawn facing the default +Z direction.
	if _current_speed < 0.01:
		var dir := target_position - position
		dir.y = 0.0
		if dir.length_squared() > 1.0:
			transform.basis = Basis.looking_at(dir.normalized(), Vector3.UP)


## GATE.S16.NPC_ALIVE.COMBAT_BRIDGE.001: Called when this ship takes a hit.
## Applies stagger and routes damage through SimBridge command queue.
func on_hit(damage: int) -> void:
	_has_been_hit = true  # FEEL_POST_FIX_6: Flag for HP bar display even on shield-only hits.
	apply_stagger(0.3)
	# FEEL_POST_FIX_3: Signal combat event so HUD shows "COMBAT" state.
	var gm := get_node_or_null("/root/GameManager")
	if gm and gm.has_method("signal_combat_v0"):
		gm.call("signal_combat_v0")
	var bridge := get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DamageNpcFleetV0") and not fleet_id.is_empty():
		var result: Dictionary = bridge.call("DamageNpcFleetV0", fleet_id, damage)
		# GATE.S7.COMBAT_JUICE: Spawn combat VFX on hit.
		var shield_left: int = result.get("shield_remaining", 0)
		_spawn_hit_vfx(global_position, damage, shield_left)
		if result.get("destroyed", false):
			_spawn_explosion_vfx()
			queue_free()


## GATE.S7.COMBAT_JUICE.EXPLOSION_VFX.001: Spawn multi-phase explosion at ship position.
## Called before queue_free on death. Adds effect to parent so it outlives the ship.
func _spawn_explosion_vfx() -> void:
	var vfx_parent := get_parent() if get_parent() else null
	if vfx_parent == null:
		return
	var ExplosionVfx := load("res://scripts/vfx/explosion_effect.gd")
	if ExplosionVfx and ExplosionVfx.has_method("spawn"):
		ExplosionVfx.call("spawn", vfx_parent, global_position)


## GATE.S7.COMBAT_JUICE.SHIELD_VFX.001 + DAMAGE_NUMBERS.001 +
## GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001: Spawn shield ripple, hull sparks,
## shield break flash + SFX, and floating damage number at the hit position.
func _spawn_hit_vfx(impact_pos: Vector3, damage_amount: int, shield_remaining: int) -> void:
	var vfx_parent := get_parent() if get_parent() else null
	if vfx_parent == null:
		return

	# Detect shield break transition: shield was >0 last hit, now <=0.
	var shield_just_broke: bool = (_prev_shield_remaining > 0 and shield_remaining <= 0)
	_prev_shield_remaining = shield_remaining

	var ShieldVfx := load("res://scripts/vfx/shield_ripple.gd")
	if ShieldVfx:
		if shield_remaining > 0:
			# Shield still up — blue hex ripple at impact point.
			if ShieldVfx.has_method("spawn_hit"):
				ShieldVfx.call("spawn_hit", vfx_parent, global_position, impact_pos)
		elif shield_just_broke:
			# Shield just broke this hit — bright flash + electric discharge + SFX.
			if ShieldVfx.has_method("spawn_break"):
				ShieldVfx.call("spawn_break", vfx_parent, global_position)
			# Play shield break SFX via combat audio.
			var ca := get_tree().root.get_node_or_null("CombatAudio") if get_tree() else null
			if ca and ca.has_method("play_shield_break_sfx"):
				ca.call("play_shield_break_sfx", global_position)
		else:
			# Hull hit (shields already down) — orange spark shower.
			if ShieldVfx.has_method("spawn_hull_sparks"):
				ShieldVfx.call("spawn_hull_sparks", vfx_parent, global_position, impact_pos)

	# Damage number — use class_name directly (static func on loaded GDScript
	# resource isn't reliably callable via .has_method / .call).
	var dmg_type: String = "shield" if shield_remaining > 0 else "hull"
	DamageNumber.spawn(vfx_parent, impact_pos, damage_amount, dmg_type)
