extends Node3D
## Departure vortex VFX for gate transit charge-up.
## Spawns a swirling particle disc + expanding ring at the gate mouth.
## Plays for ~1.5s during the charge phase, then despawns.

var _ring_particles: GPUParticles3D
var _glow_mesh: MeshInstance3D
var _material: StandardMaterial3D


func setup() -> void:
	name = "GateVortex"
	_build_glow_ring()
	_build_swirl_particles()
	_animate_expansion()


func despawn(duration: float = 0.3) -> void:
	if _material:
		var tween := create_tween()
		tween.tween_property(_material, "albedo_color:a", 0.0, duration)
		tween.parallel().tween_property(_material, "emission_energy_multiplier", 0.0, duration)
		tween.tween_callback(queue_free)
	else:
		queue_free()


func _build_glow_ring() -> void:
	# Torus-like ring using a flattened cylinder (viewed from above, it looks like a ring/disc).
	_glow_mesh = MeshInstance3D.new()
	_glow_mesh.name = "VortexRing"
	var torus := CylinderMesh.new()
	torus.top_radius = 2.0
	torus.bottom_radius = 2.0
	torus.height = 0.3
	torus.radial_segments = 24
	torus.rings = 1
	_glow_mesh.mesh = torus

	_material = StandardMaterial3D.new()
	_material.albedo_color = Color(0.2, 0.15, 0.5, 0.7)
	_material.emission_enabled = true
	_material.emission = Color(0.4, 0.2, 0.9, 1.0)
	_material.emission_energy_multiplier = 5.0
	_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_material.cull_mode = BaseMaterial3D.CULL_DISABLED
	_glow_mesh.material_override = _material

	add_child(_glow_mesh)


func _build_swirl_particles() -> void:
	_ring_particles = GPUParticles3D.new()
	_ring_particles.name = "SwirlParticles"
	_ring_particles.amount = 48
	_ring_particles.lifetime = 1.2
	_ring_particles.randomness = 0.4
	_ring_particles.visibility_aabb = AABB(Vector3(-15, -5, -15), Vector3(30, 10, 30))

	var pmat := ParticleProcessMaterial.new()
	# Emit from a ring.
	pmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	pmat.emission_ring_radius = 4.0
	pmat.emission_ring_inner_radius = 1.5
	pmat.emission_ring_height = 0.5
	pmat.emission_ring_axis = Vector3(0, 1, 0)

	# Orbital motion to create swirl effect.
	pmat.direction = Vector3(1, 0, 0)
	pmat.spread = 180.0
	pmat.initial_velocity_min = 3.0
	pmat.initial_velocity_max = 6.0
	pmat.angular_velocity_min = 120.0
	pmat.angular_velocity_max = 240.0
	pmat.gravity = Vector3.ZERO

	pmat.scale_min = 0.08
	pmat.scale_max = 0.15
	pmat.color = Color(0.4, 0.3, 0.9, 0.8)

	_ring_particles.process_material = pmat

	# Small stretched box for particle look.
	var pmesh := BoxMesh.new()
	pmesh.size = Vector3(0.1, 0.1, 0.6)
	_ring_particles.draw_pass_1 = pmesh

	add_child(_ring_particles)
	_ring_particles.emitting = true


func _animate_expansion() -> void:
	# Start small, expand over 1.5s to full size.
	scale = Vector3(0.3, 0.3, 0.3)
	_material.albedo_color.a = 0.0

	var tween := create_tween()
	# Fade in.
	tween.tween_property(_material, "albedo_color:a", 0.6, 0.3)
	# Expand ring.
	tween.parallel().tween_property(self, "scale", Vector3(1.0, 1.0, 1.0), 1.5) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	# Intensify emission as it charges.
	tween.parallel().tween_property(_material, "emission_energy_multiplier", 6.0, 1.2) \
		.from(2.0)
	# Spin the ring mesh for visual flair.
	tween.parallel().tween_property(_glow_mesh, "rotation:y", TAU * 2.0, 1.5) \
		.from(0.0)
