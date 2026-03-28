extends Area3D

@export var speed: float = 60.0
@export var max_lifetime_sec: float = 5.0

var _velocity: Vector3 = Vector3.ZERO
var _age: float = 0.0
# GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001: positional combat audio manager
var combat_audio: Node = null

# Set by spawner. Damage applied on collision via SimBridge.
var source_is_player: bool = true
var source_fleet_id: String = ""  # AI fleet ID (for AI bullets hitting player)
# GATE.S7.COMBAT_JUICE.COMBAT_PRESENT.001: weapon family visual differentiation.
# "kinetic" = short thick white, "energy" = long thin cyan, "point_defense" = rapid thin yellow.
var weapon_type: String = ""  # Set by spawner; empty = auto (energy for player, kinetic for AI)
# GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001: Canonical damage family property.
# Values: "kinetic", "energy", "neutral", "pd". Maps to weapon_type for backward compat.
var damage_family: String = ""  # Set by spawner; empty = falls back to weapon_type

func _ready() -> void:
	monitoring = true
	monitorable = false

	# Default direction: forward (negative Z) in 3D
	if _velocity == Vector3.ZERO:
		_velocity = -global_transform.basis.z * speed

	# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001: color bullet by source.
	_apply_source_color()

	# GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001: positional fire SFX on spawn
	combat_audio = get_tree().root.get_node_or_null("CombatAudio")
	if combat_audio and combat_audio.has_method("play_fire_sfx"):
		combat_audio.call("play_fire_sfx", global_position)

	body_entered.connect(_on_body_entered)
	area_entered.connect(_on_area_entered)

func _apply_source_color() -> void:
	var mesh_node := get_node_or_null("MeshInstance3D") as MeshInstance3D
	if mesh_node == null:
		return
	# GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001: Resolve effective family.
	# Priority: damage_family > weapon_type > auto (energy for player, kinetic for AI).
	var family: String = _resolve_damage_family()
	var color: Color
	var scale_x: float = 1.0
	var scale_z: float = 1.0
	var emission_mult: float = 3.0
	var trail_length: float = 0.0  # 0 = no trail
	match family:
		"kinetic":
			color = Color(1.0, 0.85, 0.2, 1.0)   # Yellow
			scale_x = 1.6
			scale_z = 0.7
			emission_mult = 2.5
			trail_length = 0.15  # Short trail
		"energy":
			color = Color(0.0, 1.0, 0.9, 1.0)    # Cyan
			scale_x = 0.6
			scale_z = 1.5
			emission_mult = 4.0
			trail_length = 0.35  # Bright glow trail
		"pd":
			color = Color(0.2, 1.0, 0.3, 1.0)    # Green
			scale_x = 0.4
			scale_z = 0.4
			emission_mult = 4.0
			trail_length = 0.1   # Rapid small trail
		"neutral", _:
			color = Color(0.9, 0.9, 0.95, 1.0)   # White
			scale_x = 1.0
			scale_z = 1.0
			emission_mult = 3.0
			trail_length = 0.2   # Standard trail
	var mat := StandardMaterial3D.new()
	mat.emission_enabled = true
	mat.albedo_color = color
	mat.emission = color
	mat.emission_energy_multiplier = emission_mult
	mesh_node.material_override = mat
	mesh_node.scale = Vector3(scale_x, 1.0, scale_z)
	# GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001: Add trail particles.
	if trail_length > 0.0:
		_add_trail_particles(color, trail_length)


## GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001: Resolve the effective damage family.
func _resolve_damage_family() -> String:
	# damage_family takes priority.
	if not damage_family.is_empty():
		return damage_family
	# Map legacy weapon_type to damage_family.
	if not weapon_type.is_empty():
		match weapon_type:
			"point_defense":
				return "pd"
			_:
				return weapon_type  # kinetic, energy pass through
	# Auto: player = energy, AI = kinetic.
	return "energy" if source_is_player else "kinetic"


## GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001 + GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001:
## Attach trail GPUParticles3D to bullet. Trail color matches weapon family.
## GATE.T41.COMBAT.VFX_VERIFY.001: Verified visible at game camera altitude ~50u.
func _add_trail_particles(color: Color, trail_life: float) -> void:
	var particles := GPUParticles3D.new()
	particles.name = "BulletTrail"
	particles.amount = 12
	particles.lifetime = trail_life
	particles.one_shot = false
	particles.explosiveness = 0.0
	particles.randomness = 0.2
	particles.emitting = true
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 0, 0)
	proc_mat.spread = 8.0
	proc_mat.initial_velocity_min = 2.0
	proc_mat.initial_velocity_max = 5.0
	proc_mat.gravity = Vector3.ZERO
	proc_mat.scale_min = 0.4
	proc_mat.scale_max = 0.9
	proc_mat.damping_min = 2.0
	proc_mat.damping_max = 4.0
	# Trail gradient: full color -> fade out.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(color.r, color.g, color.b, 0.9))
	gradient.add_point(1.0, Color(color.r, color.g, color.b, 0.0))
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.5
	mesh.height = 1.0
	particles.draw_pass_1 = mesh

	add_child(particles)

func set_direction(direction: Vector3) -> void:
	if direction.length() > 0.0001:
		_velocity = direction.normalized() * speed

func _physics_process(delta: float) -> void:
	_age += delta
	if _age >= max_lifetime_sec:
		queue_free()
		return

	global_position += _velocity * delta

func _on_body_entered(body: Node) -> void:
	# AI bullet hitting the player — skip damage if player is docked
	if not source_is_player and body.is_in_group("Player"):
		var gm_check = get_node_or_null("/root/GameManager")
		var ps = gm_check.get("current_player_state") if gm_check else 0
		if ps != null and ps != 0:  # 0=IN_FLIGHT; skip damage if DOCKED(1) or IN_LANE_TRANSIT(2)
			queue_free()
			return
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("ApplyAiShotAtPlayerV0") and not source_fleet_id.is_empty():
			bridge.call("ApplyAiShotAtPlayerV0", source_fleet_id)
		# GATE.T64.JUICE.SHAKE_SCALE.001: Damage-scaled camera shake.
		var gm_shake = get_node_or_null("/root/GameManager")
		if gm_shake and gm_shake.has_method("_apply_camera_shake_v0"):
			# Scale shake by damage fraction: 0.2 base + up to 0.6 based on hull ratio.
			var fleet_bridge = get_node_or_null("/root/SimBridge")
			var shake_intensity: float = 0.4  # fallback
			if fleet_bridge and fleet_bridge.has_method("GetFleetStateV0"):
				var fleet_state: Dictionary = fleet_bridge.call("GetFleetStateV0")
				var hull_hp: int = int(fleet_state.get("hull_hp", 100))
				var hull_max: int = int(fleet_state.get("hull_max", 100))
				if hull_max > 0:
					var hull_ratio: float = float(hull_hp) / float(hull_max)
					# Low hull = bigger shake. Near-death (< 20%) gets max shake.
					shake_intensity = 0.2 + (1.0 - hull_ratio) * 0.6
					# Near-death extra: longer screen-edge flash via hud.
					if hull_ratio < 0.2:
						shake_intensity = 0.8
			gm_shake.call("_apply_camera_shake_v0", shake_intensity)
		# FEEL_BASELINE: Screen-space red flash on player damage.
		var hud_node = get_node_or_null("/root/Main/HUD")
		if hud_node and hud_node.has_method("flash_damage_v0"):
			hud_node.call("flash_damage_v0")
	# GATE.S1.AUDIO.SFX_CORE.001: hit SFX
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("play_hit_sfx_v0"):
		gm.call("play_hit_sfx_v0")
	# GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001: positional impact SFX
	if combat_audio and combat_audio.has_method("play_impact_sfx"):
		combat_audio.call("play_impact_sfx", global_position)
	# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001: spawn hit VFX at impact point.
	_spawn_hit_vfx(global_position)
	queue_free()

func _on_area_entered(area: Area3D) -> void:
	# Player bullet hitting a fleet marker
	if source_is_player and area.has_meta("fleet_id"):
		var fleet_id: String = str(area.get_meta("fleet_id"))
		if not fleet_id.is_empty():
			var bridge = get_node_or_null("/root/SimBridge")
			if bridge and bridge.has_method("ApplyTurretShotV0"):
				var result: Dictionary = bridge.call("ApplyTurretShotV0", fleet_id)
				print("BULLET_HIT|fleet=%s|hull=%s|shield=%s|killed=%s" % [fleet_id, result.get("target_hull", "?"), result.get("target_shield", "?"), result.get("killed", false)])
				# GATE.S7.COMBAT_JUICE: Spawn shield/damage VFX on the NPC ship.
				_spawn_npc_combat_vfx(area, result)
				if result.get("killed", false):
					var gm = get_node_or_null("/root/GameManager")
					if gm and gm.has_method("despawn_fleet_v0"):
						gm.call("despawn_fleet_v0", fleet_id)
	# GATE.S1.AUDIO.SFX_CORE.001: hit SFX
	var gm_sfx = get_node_or_null("/root/GameManager")
	if gm_sfx and gm_sfx.has_method("play_hit_sfx_v0"):
		gm_sfx.call("play_hit_sfx_v0")
	# GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001: positional impact SFX
	if combat_audio and combat_audio.has_method("play_impact_sfx"):
		combat_audio.call("play_impact_sfx", global_position)
	# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001: spawn hit VFX at impact point.
	_spawn_hit_vfx(global_position)
	queue_free()

# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001 + GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001:
# 0.4s burst GPUParticles3D hit effect. GATE.T41.COMBAT.VFX_VERIFY.001: Verified at ~50u camera.
func _spawn_hit_vfx(pos: Vector3) -> void:
	var root = get_tree().get_root() if get_tree() else null
	if root == null:
		return

	var particles := GPUParticles3D.new()
	particles.name = "HitVfx"
	particles.position = pos
	particles.amount = 20
	particles.lifetime = 0.6
	particles.one_shot = true
	particles.explosiveness = 0.90
	particles.randomness = 0.3
	particles.emitting = true

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 1, 0)
	proc_mat.spread = 90.0
	# GATE.T41.COMBAT.VFX_VERIFY.001: Verified visible at game camera altitude ~50u.
	proc_mat.initial_velocity_min = 200.0
	proc_mat.initial_velocity_max = 450.0
	proc_mat.gravity = Vector3(0, 0, 0)
	proc_mat.scale_min = 20.0
	proc_mat.scale_max = 40.0
	if source_is_player:
		proc_mat.color = Color(0.6, 1.0, 0.9, 1.0)
	else:
		proc_mat.color = Color(1.0, 0.7, 0.3, 1.0)
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	# GATE.T41.COMBAT.VFX_VERIFY.001: Verified visible at ~50u altitude.
	mesh.radius = 12.0
	mesh.height = 24.0
	particles.draw_pass_1 = mesh

	root.add_child(particles)

	# Auto-free after burst completes (0.6s lifetime + small buffer).
	var timer := root.get_tree().create_timer(0.8) if root.get_tree() else null
	if timer:
		timer.timeout.connect(func(): if is_instance_valid(particles): particles.queue_free())


# GATE.S7.COMBAT_JUICE: Spawn shield ripple + damage number on NPC ship hit.
# `area` is the FleetArea child of the NPC ship. `result` is from ApplyTurretShotV0.
func _spawn_npc_combat_vfx(area: Area3D, result: Dictionary) -> void:
	var npc_ship := area.get_parent() if area else null
	if npc_ship == null or not is_instance_valid(npc_ship):
		return
	var vfx_parent := npc_ship.get_parent() if npc_ship.get_parent() else null
	if vfx_parent == null:
		return

	var impact_pos := global_position
	var ship_pos: Vector3 = npc_ship.global_position
	var target_shield: int = int(result.get("target_shield", 0))
	var shield_dmg: int = int(result.get("shield_dmg", 0))
	var hull_dmg: int = int(result.get("hull_dmg", 0))
	var total_dmg: int = shield_dmg + hull_dmg

	# GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001: Shield ripple, hull sparks, or shield break.
	var ShieldVfx = load("res://scripts/vfx/shield_ripple.gd")
	if ShieldVfx:
		if target_shield > 0:
			# Shield still up — blue hex ripple.
			if ShieldVfx.has_method("spawn_hit"):
				ShieldVfx.call("spawn_hit", vfx_parent, ship_pos, impact_pos)
		elif target_shield <= 0 and shield_dmg > 0:
			# Shield just broke this hit — bright flash + discharge + SFX.
			if ShieldVfx.has_method("spawn_break"):
				ShieldVfx.call("spawn_break", vfx_parent, ship_pos)
			var ca = get_tree().root.get_node_or_null("CombatAudio") if get_tree() else null
			if ca and ca.has_method("play_shield_break_sfx"):
				ca.call("play_shield_break_sfx", ship_pos)
		elif hull_dmg > 0:
			# Hull hit (shields already down) — orange spark shower.
			if ShieldVfx.has_method("spawn_hull_sparks"):
				ShieldVfx.call("spawn_hull_sparks", vfx_parent, ship_pos, impact_pos)

	# Damage number.
	if total_dmg > 0:
		var DmgNumVfx = load("res://scripts/vfx/damage_number.gd")
		if DmgNumVfx and DmgNumVfx.has_method("spawn"):
			var dmg_type: String = "shield" if shield_dmg > 0 and target_shield > 0 else "hull"
			DmgNumVfx.call("spawn", vfx_parent, impact_pos, total_dmg, dmg_type)
