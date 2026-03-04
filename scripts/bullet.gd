extends Area3D

@export var speed: float = 60.0
@export var max_lifetime_sec: float = 5.0

var _velocity: Vector3 = Vector3.ZERO
var _age: float = 0.0

# Set by spawner. Damage applied on collision via SimBridge.
var source_is_player: bool = true
var source_fleet_id: String = ""  # AI fleet ID (for AI bullets hitting player)

func _ready() -> void:
	monitoring = true
	monitorable = false

	# Default direction: forward (negative Z) in 3D
	if _velocity == Vector3.ZERO:
		_velocity = -global_transform.basis.z * speed

	# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001: color bullet by source.
	_apply_source_color()

	body_entered.connect(_on_body_entered)
	area_entered.connect(_on_area_entered)

func _apply_source_color() -> void:
	var mesh_node := get_node_or_null("MeshInstance3D") as MeshInstance3D
	if mesh_node == null:
		return
	var mat := StandardMaterial3D.new()
	mat.emission_enabled = true
	if source_is_player:
		# Player bullets: bright cyan/green
		mat.albedo_color = Color(0.0, 1.0, 0.7, 1.0)
		mat.emission = Color(0.0, 1.0, 0.7, 1.0)
		mat.emission_energy_multiplier = 3.0
	else:
		# AI bullets: orange/red
		mat.albedo_color = Color(1.0, 0.3, 0.05, 1.0)
		mat.emission = Color(1.0, 0.3, 0.05, 1.0)
		mat.emission_energy_multiplier = 3.0
	mesh_node.material_override = mat

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
	# AI bullet hitting the player
	if not source_is_player and body.is_in_group("Player"):
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("ApplyAiShotAtPlayerV0") and not source_fleet_id.is_empty():
			bridge.call("ApplyAiShotAtPlayerV0", source_fleet_id)
	# GATE.S1.AUDIO.SFX_CORE.001: hit SFX
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("play_hit_sfx_v0"):
		gm.call("play_hit_sfx_v0")
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
				if result.get("killed", false):
					var gm = get_node_or_null("/root/GameManager")
					if gm and gm.has_method("despawn_fleet_v0"):
						gm.call("despawn_fleet_v0", fleet_id)
	# GATE.S1.AUDIO.SFX_CORE.001: hit SFX
	var gm_sfx = get_node_or_null("/root/GameManager")
	if gm_sfx and gm_sfx.has_method("play_hit_sfx_v0"):
		gm_sfx.call("play_hit_sfx_v0")
	# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001: spawn hit VFX at impact point.
	_spawn_hit_vfx(global_position)
	queue_free()

# GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001: 0.3s burst GPUParticles3D hit effect.
func _spawn_hit_vfx(pos: Vector3) -> void:
	var root = get_tree().get_root() if get_tree() else null
	if root == null:
		return

	var particles := GPUParticles3D.new()
	particles.name = "HitVfx"
	particles.position = pos
	particles.amount = 24
	particles.lifetime = 0.35
	particles.one_shot = true
	particles.explosiveness = 0.92
	particles.randomness = 0.3
	particles.emitting = true

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 1, 0)
	proc_mat.spread = 80.0
	proc_mat.initial_velocity_min = 6.0
	proc_mat.initial_velocity_max = 18.0
	proc_mat.gravity = Vector3(0, 0, 0)
	proc_mat.scale_min = 0.4
	proc_mat.scale_max = 1.0
	if source_is_player:
		proc_mat.color = Color(0.6, 1.0, 0.9, 1.0)
	else:
		proc_mat.color = Color(1.0, 0.7, 0.3, 1.0)
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.35
	mesh.height = 0.7
	particles.draw_pass_1 = mesh

	root.add_child(particles)

	# Auto-free after burst completes (0.3s lifetime + small buffer).
	var timer := root.get_tree().create_timer(0.5) if root.get_tree() else null
	if timer:
		timer.timeout.connect(func(): if is_instance_valid(particles): particles.queue_free())
