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
var _is_hostile: bool = false
var _travel_progress: float = 0.0
var _fleet_state: String = "Idle"

## GATE.S7.FACTION.PATROL_AGGRO.001: Reputation-based aggro state.
var _faction_id: String = ""
var _aggro_check_timer: float = 0.0
const AGGRO_CHECK_INTERVAL: float = 2.0  # seconds between reputation polls
const AGGRO_REPUTATION_THRESHOLD: int = -50  # matches FactionTweaksV0.AggroReputationThreshold

## Flight controller state.
var target_position: Vector3 = Vector3.ZERO
var target_speed: float = 6.0
var stagger_remaining: float = 0.0

## Rotation smoothing (higher = snappier turn).
@export var rotation_sharpness: float = 5.0

## Stop moving when closer than this to target.
const ARRIVAL_THRESHOLD: float = 1.5

## Star avoidance — ships steer around the star (at local origin) instead of flying through it.
const STAR_AVOID_RADIUS: float = 25.0  # Minimum clearance from star center

## Local patrol — ships pick new waypoints when they reach their target.
## Radius tuned so ships spread across the playable system area (visible at camera altitude ~80).
const PATROL_RADIUS_MIN: float = 15.0
const PATROL_RADIUS_MAX: float = 45.0
var _patrol_seed: int = 0

## GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001: Track previous shield state for break detection.
var _prev_shield_remaining: int = -1  # -1 = not yet set

## Visual node references.
@onready var _ship_visual: Node3D = $ShipVisual
@onready var _fleet_area: Area3D = $FleetArea

## Model loaded flag.
var _model_loaded: bool = false

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
const ROLE_LETTERS := ["T", "H", "P"]  # Trader, Hauler, Patrol
const LABEL_SHOW_DIST := 160.0  # Visible at camera altitude ~80 (increased for altitude clarity)
const HP_BAR_HEIGHT := 8.0  # Above ship center (raised for altitude visibility)


func _ready() -> void:
	_fleet_area.set_meta("fleet_id", fleet_id)
	add_to_group("NpcShip")
	collision_layer = 4
	collision_mask = 0
	# Seed patrol RNG from fleet ID so each ship patrols differently.
	for c in fleet_id:
		_patrol_seed = _patrol_seed * 31 + c.unicode_at(0)
	_create_status_display()


## GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001 + GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001:
## Create role label + hostile label + HP bar overlay. Sized for camera altitude ~80.
func _create_status_display() -> void:
	# Role label (T/H/P) — billboard facing camera. Enlarged for altitude ~80 visibility.
	_role_label = Label3D.new()
	_role_label.name = "RoleLabel"
	_role_label.pixel_size = 0.10
	_role_label.font_size = 64
	_role_label.outline_size = 16
	_role_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_role_label.no_depth_test = true
	_role_label.render_priority = 10
	_role_label.position = Vector3(0, HP_BAR_HEIGHT + 2.5, 0)
	_role_label.modulate = Color(0.9, 0.95, 1.0)
	_role_label.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_role_label)

	# Hostile label — dedicated "HOSTILE" text below role label. Red, bright, large.
	_hostile_label = Label3D.new()
	_hostile_label.name = "HostileLabel"
	_hostile_label.pixel_size = 0.10
	_hostile_label.font_size = 56
	_hostile_label.outline_size = 16
	_hostile_label.outline_modulate = Color(0, 0, 0, 0.9)
	_hostile_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_hostile_label.no_depth_test = true
	_hostile_label.render_priority = 10
	_hostile_label.position = Vector3(0, HP_BAR_HEIGHT + 5.0, 0)
	_hostile_label.text = "HOSTILE"
	_hostile_label.modulate = Color(1.0, 0.2, 0.15)
	_hostile_label.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_hostile_label.visible = false
	add_child(_hostile_label)

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
		var letter: String = ROLE_LETTERS[clampi(_role, 0, 2)]
		if _is_hostile:
			_role_label.text = letter
			_role_label.modulate = Color(1.0, 0.4, 0.35)
		else:
			_role_label.text = letter
			_role_label.modulate = Color(0.9, 0.95, 1.0)

	# GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001: Dedicated hostile label visibility.
	if _hostile_label:
		_hostile_label.visible = _is_hostile

	if _hp_bar and _hull_hp_max > 0:
		var ratio := clampf(float(_hull_hp) / float(_hull_hp_max), 0.0, 1.0)
		_hp_bar.scale = Vector3(ratio, 1.0, 1.0)
		# Color: green > yellow > red with higher emission for altitude visibility.
		if ratio > 0.5:
			_hp_bar_mat.albedo_color = Color(0.2, 0.9, 0.2)
			_hp_bar_mat.emission = Color(0.2, 0.9, 0.2)
		elif ratio > 0.25:
			_hp_bar_mat.albedo_color = Color(1.0, 0.9, 0.1)
			_hp_bar_mat.emission = Color(1.0, 0.9, 0.1)
		else:
			_hp_bar_mat.albedo_color = Color(1.0, 0.2, 0.1)
			_hp_bar_mat.emission = Color(1.0, 0.2, 0.1)
		_hp_bar.visible = ratio < 1.0  # Only show when damaged

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
	# Hostile label: visible when in range AND hostile.
	if _hostile_label:
		_hostile_label.visible = show_label and _is_hostile


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


## Called by the spawn system to load the correct Quaternius model.
## May be called before _ready() — resolves ShipVisual lazily.
func load_model(model_scene: PackedScene) -> void:
	if model_scene == null:
		return
	if _ship_visual == null:
		_ship_visual = get_node_or_null("ShipVisual")
	if _ship_visual == null:
		return
	for child in _ship_visual.get_children():
		child.queue_free()
	var instance := model_scene.instantiate()
	instance.name = "FleetModel"
	_ship_visual.add_child(instance)
	_model_loaded = true
	# Apply faction tint if already set.
	if _faction_color != Color.WHITE:
		_apply_faction_tint(instance)


## GATE.S7.FACTION_VIS.SHIP_LIVERY.001: Set faction color and apply tint to loaded model.
func set_faction_color(color: Color) -> void:
	_faction_color = color
	if _ship_visual == null:
		return
	var model := _ship_visual.get_node_or_null("FleetModel")
	if model:
		_apply_faction_tint(model)


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
	_role = data.get("role", 0)
	_hull_hp = data.get("hull_hp", 0)
	_hull_hp_max = data.get("hull_hp_max", 0)
	_travel_progress = data.get("travel_progress", 0.0)
	_fleet_state = data.get("state", "Idle")

	## GATE.S7.FACTION.PATROL_AGGRO.001: Resolve hostility from faction reputation.
	var base_hostile: bool = data.get("is_hostile", false)
	if base_hostile and _role == 2:  # Patrol
		var current_node: String = data.get("current_node_id", "")
		_is_hostile = _check_reputation_aggro(current_node)
	else:
		_is_hostile = base_hostile
	_update_status_display()


## GATE.S7.FACTION.PATROL_AGGRO.001: Check player reputation with the fleet's faction.
## Returns true if reputation is below aggro threshold (player is hostile to this faction).
func _check_reputation_aggro(current_node: String) -> bool:
	var bridge := get_node_or_null("/root/SimBridge")
	if bridge == null:
		return true  # No bridge = assume hostile (safe default for patrols)

	# Resolve faction from the fleet's current node.
	if not current_node.is_empty() and bridge.has_method("GetTerritoryAccessV0"):
		var territory: Dictionary = bridge.call("GetTerritoryAccessV0", current_node)
		var fid: String = territory.get("faction_id", "")
		if not fid.is_empty():
			_faction_id = fid

	# If no faction resolved, fall back to hostile.
	if _faction_id.is_empty():
		return true

	# Query player reputation with this faction.
	if bridge.has_method("GetPlayerReputationV0"):
		var rep_data: Dictionary = bridge.call("GetPlayerReputationV0", _faction_id)
		var reputation: int = rep_data.get("reputation", 0)
		return reputation < AGGRO_REPUTATION_THRESHOLD

	return true  # Fallback: hostile if bridge method missing


func _physics_process(delta: float) -> void:
	# GATE.S7.FACTION.PATROL_AGGRO.001: Periodic reputation re-check for patrol ships.
	if _role == 2:  # Patrol
		_aggro_check_timer -= delta
		if _aggro_check_timer <= 0.0:
			_aggro_check_timer = AGGRO_CHECK_INTERVAL
			var old_hostile := _is_hostile
			_is_hostile = _check_reputation_aggro("")
			if old_hostile != _is_hostile:
				_update_status_display()

	# Combat stagger — freeze movement.
	if stagger_remaining > 0.0:
		stagger_remaining -= delta
		velocity = Vector3.ZERO
		move_and_slide()
		return

	# Direction to target (XZ only).
	# Use local `position` — targets are in local system coordinates (relative
	# to _localSystemRoot which sits at the star's galactic position).
	var to_target := target_position - position
	to_target.y = 0.0
	var dist := to_target.length()

	if dist < ARRIVAL_THRESHOLD:
		# Pick a new local patrol waypoint so the ship keeps moving visibly.
		_pick_next_patrol_waypoint()
		velocity = Vector3.ZERO
		move_and_slide()
		return

	var dir := to_target / dist

	# Star avoidance: star is at local origin (0,0,0) since _localSystemRoot
	# is centered on the star. Steer around it if our path passes too close.
	dir = _apply_star_avoidance_local(dir, dist)

	# Smooth rotation toward movement direction (ship forward = -Z).
	if dir.length_squared() > 0.001:
		var target_basis := Basis.looking_at(-dir, Vector3.UP)
		var current_quat := transform.basis.get_rotation_quaternion()
		var target_quat := target_basis.get_rotation_quaternion()
		var weight := clampf(rotation_sharpness * delta, 0.0, 1.0)
		transform.basis = Basis(current_quat.slerp(target_quat, weight))

	velocity = dir * target_speed
	velocity.y = 0.0
	move_and_slide()
	position.y = 0.0


## Pick a new random orbit waypoint for local patrol movement.
func _pick_next_patrol_waypoint() -> void:
	_patrol_seed += 1
	# Simple hash for pseudo-random angle.
	var h: int = _patrol_seed * 2654435761  # Knuth multiplicative hash
	var angle: float = float(h % 3600) / 3600.0 * TAU
	var radius: float = PATROL_RADIUS_MIN + float(h % 1000) / 1000.0 * (PATROL_RADIUS_MAX - PATROL_RADIUS_MIN)
	target_position = Vector3(cos(angle) * radius, 0.0, sin(angle) * radius)


## Steer around the star (at local origin) if the direct path passes too close.
## All positions are in local coordinates — star center is (0,0,0).
func _apply_star_avoidance_local(dir: Vector3, dist_to_target: float) -> Vector3:
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

	if perp_dist >= STAR_AVOID_RADIUS:
		return dir  # Path clears the star.

	# Steer perpendicular to the travel line, away from the star.
	var avoid_dir: Vector3
	if perp_dist < 0.1:
		# Headed dead center — pick a consistent side based on position.
		avoid_dir = Vector3(-dir.z, 0.0, dir.x)
	else:
		avoid_dir = -perp.normalized()  # Away from star center

	# Blend: more avoidance when closer to the star.
	var urgency := 1.0 - clampf(perp_dist / STAR_AVOID_RADIUS, 0.0, 1.0)
	var blended := (dir + avoid_dir * urgency * 2.0).normalized()
	return blended


## Apply combat stagger (seconds of movement freeze).
func apply_stagger(duration: float) -> void:
	stagger_remaining += duration


## Set movement target from world position.
func set_target(pos: Vector3, spd: float) -> void:
	target_position = Vector3(pos.x, 0.0, pos.z)
	target_speed = spd


## GATE.S16.NPC_ALIVE.COMBAT_BRIDGE.001: Called when this ship takes a hit.
## Applies stagger and routes damage through SimBridge command queue.
func on_hit(damage: int) -> void:
	apply_stagger(0.3)
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

	# Damage number.
	var DmgNumVfx := load("res://scripts/vfx/damage_number.gd")
	if DmgNumVfx and DmgNumVfx.has_method("spawn"):
		var dmg_type: String = "shield" if shield_remaining > 0 else "hull"
		DmgNumVfx.call("spawn", vfx_parent, impact_pos, damage_amount, dmg_type)
