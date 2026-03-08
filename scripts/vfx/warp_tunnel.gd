extends Node3D
## GATE.S17.REAL_SPACE.WARP_TUNNEL.001
## Warp tunnel VFX during lane transit: cylinder mesh with scrolling texture
## + speed streak particles. Spawns around player on transit start, despawns on arrival.
## Exported opacity var for sensor tech integration (SENSOR_REVEAL).

## Opacity: 0.0 = fully transparent, 1.0 = fully opaque.
## Sensor tech can reduce this to reveal void sites during transit.
@export var tunnel_opacity: float = 0.85

var _tunnel_mesh: MeshInstance3D
var _streak_particles: GPUParticles3D
var _material: StandardMaterial3D


func setup(opacity: float = 0.85) -> void:
	name = "WarpTunnel"
	tunnel_opacity = opacity
	_build()


func despawn(duration: float = 0.3) -> void:
	if _material:
		var tween := create_tween()
		tween.tween_property(_material, "albedo_color:a", 0.0, duration)
		tween.parallel().tween_property(_material, "emission_energy_multiplier", 0.0, duration)
		tween.tween_callback(queue_free)
	else:
		queue_free()


func set_opacity(value: float) -> void:
	tunnel_opacity = clampf(value, 0.0, 1.0)
	if _material:
		_material.albedo_color.a = tunnel_opacity


func _build() -> void:
	# Cylinder tunnel around the player (aligned along Z forward).
	_tunnel_mesh = MeshInstance3D.new()
	_tunnel_mesh.name = "TunnelCylinder"
	var cyl := CylinderMesh.new()
	cyl.top_radius = 8.0
	cyl.bottom_radius = 8.0
	cyl.height = 200.0
	cyl.radial_segments = 16
	cyl.rings = 4
	_tunnel_mesh.mesh = cyl
	# Rotate so cylinder extends along Z (default is Y-up).
	_tunnel_mesh.rotation_degrees.x = 90.0

	_material = StandardMaterial3D.new()
	_material.albedo_color = Color(0.1, 0.2, 0.6, tunnel_opacity)
	_material.emission_enabled = true
	_material.emission = Color(0.15, 0.3, 0.8, 1.0)
	_material.emission_energy_multiplier = 2.0
	_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_material.cull_mode = BaseMaterial3D.CULL_FRONT  # Render inside of cylinder.
	# Scrolling effect via UV offset animation.
	_material.uv1_scale = Vector3(1.0, 4.0, 1.0)
	_tunnel_mesh.material_override = _material

	add_child(_tunnel_mesh)

	# Animate UV scroll for motion feel.
	_animate_uv_scroll()

	# Speed streak particles flying past the player.
	_streak_particles = GPUParticles3D.new()
	_streak_particles.name = "StreakParticles"
	_streak_particles.amount = 64
	_streak_particles.lifetime = 0.8
	_streak_particles.randomness = 0.3
	_streak_particles.visibility_aabb = AABB(Vector3(-20, -20, -120), Vector3(40, 40, 240))

	var pmat := ParticleProcessMaterial.new()
	pmat.direction = Vector3(0, 0, -1)
	pmat.spread = 15.0
	pmat.initial_velocity_min = 80.0
	pmat.initial_velocity_max = 150.0
	pmat.gravity = Vector3.ZERO
	pmat.scale_min = 0.02
	pmat.scale_max = 0.05
	pmat.color = Color(0.6, 0.8, 1.0, 0.7)
	# Emit from a ring around the player.
	pmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	pmat.emission_ring_radius = 6.0
	pmat.emission_ring_inner_radius = 3.0
	pmat.emission_ring_height = 0.5
	pmat.emission_ring_axis = Vector3(0, 0, 1)
	_streak_particles.process_material = pmat

	# Elongated mesh for streak look.
	var streak_mesh := BoxMesh.new()
	streak_mesh.size = Vector3(0.05, 0.05, 2.0)
	_streak_particles.draw_pass_1 = streak_mesh

	add_child(_streak_particles)
	_streak_particles.emitting = true

	# Fade in.
	_material.albedo_color.a = 0.0
	var fade_in := create_tween()
	fade_in.tween_property(_material, "albedo_color:a", tunnel_opacity, 0.4)


func _animate_uv_scroll() -> void:
	# Continuously scroll UV offset for motion illusion.
	var tween := create_tween().set_loops()
	tween.tween_property(_material, "uv1_offset:y", -10.0, 2.0).from(0.0)
