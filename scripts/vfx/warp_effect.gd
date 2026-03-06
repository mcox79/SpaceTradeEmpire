extends Node3D
class_name WarpEffect
## GATE.S16.NPC_ALIVE.WARP_VFX.001
## Warp-in/warp-out visual effect for NPC ships at lane gates.
## Usage:
##   WarpEffect.play_warp_in(parent, position)
##   WarpEffect.play_warp_out(ship_node, callback)


## Play warp-in effect: blue/white particle burst + scale-up animation.
## Returns the effect node (auto-frees after animation).
static func play_warp_in(parent: Node, pos: Vector3, duration: float = 0.5) -> Node3D:
	var effect := Node3D.new()
	effect.name = "WarpInVfx"
	effect.position = pos
	parent.add_child(effect)

	# Particle burst
	var particles := _create_warp_particles()
	effect.add_child(particles)
	particles.emitting = true

	# Scale flash (bright sphere that shrinks)
	var flash := _create_flash_mesh()
	effect.add_child(flash)

	var tween := effect.create_tween()
	tween.tween_property(flash, "scale", Vector3(0.1, 0.1, 0.1), duration).from(Vector3(3.0, 3.0, 3.0))
	tween.parallel().tween_property(flash, "transparency", 1.0, duration).from(0.0)

	# Auto-cleanup
	var tree := parent.get_tree()
	if tree:
		var timer := tree.create_timer(duration + 0.3)
		timer.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect


## Play warp-out effect on ship: scale down + streak particles, then queue_free.
static func play_warp_out(ship: Node3D, duration: float = 0.5) -> void:
	if ship == null or not is_instance_valid(ship):
		return

	# Streak particles
	var particles := _create_warp_particles()
	particles.lifetime = 0.4
	ship.add_child(particles)
	particles.emitting = true

	# Scale down to zero
	var tween := ship.create_tween()
	tween.tween_property(ship, "scale", Vector3.ZERO, duration)
	tween.tween_callback(ship.queue_free)


static func _create_warp_particles() -> GPUParticles3D:
	var particles := GPUParticles3D.new()
	particles.name = "WarpParticles"
	particles.amount = 16
	particles.lifetime = 0.4
	particles.one_shot = true
	particles.explosiveness = 0.95
	particles.randomness = 0.2
	particles.emitting = false

	var mat := ParticleProcessMaterial.new()
	mat.direction = Vector3(0, 0, 0)
	mat.spread = 180.0
	mat.initial_velocity_min = 5.0
	mat.initial_velocity_max = 15.0
	mat.gravity = Vector3.ZERO
	mat.scale_min = 0.1
	mat.scale_max = 0.3
	mat.color = Color(0.4, 0.7, 1.0, 1.0)  # Blue-white
	particles.process_material = mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.15
	mesh.height = 0.3
	particles.draw_pass_1 = mesh

	return particles


static func _create_flash_mesh() -> MeshInstance3D:
	var mesh := MeshInstance3D.new()
	mesh.name = "WarpFlash"
	var sphere := SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	mesh.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.5, 0.8, 1.0, 0.6)
	mat.emission_enabled = true
	mat.emission = Color(0.5, 0.8, 1.0, 1.0)
	mat.emission_energy_multiplier = 4.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mesh.material_override = mat
	return mesh
