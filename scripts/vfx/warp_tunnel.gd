extends Node3D
## GATE.S17.REAL_SPACE.WARP_TUNNEL.001
## GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001
## Warp tunnel VFX during lane transit: cylinder mesh with scrolling texture
## + multi-layer speed streak particles + color-shifting glow.
## Spawns around player on transit start, despawns on arrival.
## Exported opacity var for sensor tech integration (SENSOR_REVEAL).

## Opacity: 0.0 = fully transparent, 1.0 = fully opaque.
## Sensor tech can reduce this to reveal void sites during transit.
@export var tunnel_opacity: float = 0.85

var _tunnel_mesh: MeshInstance3D
var _streak_particles: GPUParticles3D
var _fast_streaks: GPUParticles3D
var _ambient_glow: GPUParticles3D
var _outer_haze: GPUParticles3D
var _material: StandardMaterial3D
var _inner_material: StandardMaterial3D
var _inner_tunnel: MeshInstance3D
var _elapsed: float = 0.0
var _base_emission_energy: float = 3.0


func setup(opacity: float = 0.85) -> void:
	name = "WarpTunnel"
	tunnel_opacity = opacity
	_build()


func despawn(duration: float = 0.3) -> void:
	if _material:
		# Stop particle emission immediately so streaks die out naturally.
		if _streak_particles:
			_streak_particles.emitting = false
		if _fast_streaks:
			_fast_streaks.emitting = false
		if _ambient_glow:
			_ambient_glow.emitting = false
		if _outer_haze:
			_outer_haze.emitting = false
		var tween := create_tween()
		tween.tween_property(_material, "albedo_color:a", 0.0, duration)
		tween.parallel().tween_property(_material, "emission_energy_multiplier", 0.0, duration)
		if _inner_material:
			tween.parallel().tween_property(_inner_material, "albedo_color:a", 0.0, duration)
		tween.tween_callback(queue_free)
	else:
		queue_free()


func set_opacity(value: float) -> void:
	tunnel_opacity = clampf(value, 0.0, 1.0)
	if _material:
		_material.albedo_color.a = tunnel_opacity


func _process(delta: float) -> void:
	_elapsed += delta

	# Pulsating tunnel emission — rhythmic energy surges.
	if _material:
		var pulse: float = 1.0 + 0.4 * sin(_elapsed * 6.0) + 0.2 * sin(_elapsed * 13.0)
		_material.emission_energy_multiplier = _base_emission_energy * pulse

	# Color shift: blue -> white -> cyan over time (cycles every ~3s).
	if _material:
		var t: float = fmod(_elapsed, 3.0) / 3.0
		var color: Color
		if t < 0.33:
			# Blue to white
			var st: float = t / 0.33
			color = Color(0.1, 0.2, 0.6).lerp(Color(0.7, 0.8, 1.0), st)
		elif t < 0.66:
			# White to cyan
			var st: float = (t - 0.33) / 0.33
			color = Color(0.7, 0.8, 1.0).lerp(Color(0.0, 0.6, 0.8), st)
		else:
			# Cyan back to blue
			var st: float = (t - 0.66) / 0.34
			color = Color(0.0, 0.6, 0.8).lerp(Color(0.1, 0.2, 0.6), st)
		_material.albedo_color = Color(color.r, color.g, color.b, tunnel_opacity)
		_material.emission = Color(color.r + 0.1, color.g + 0.1, color.b + 0.2, 1.0)

	# Inner tunnel pulses complementary.
	if _inner_material:
		var inner_pulse: float = 0.6 + 0.3 * sin(_elapsed * 8.0 + 1.0)
		_inner_material.emission_energy_multiplier = 1.5 * inner_pulse


func _build() -> void:
	# === Outer cylinder tunnel around the player (aligned along Z forward). ===
	_tunnel_mesh = MeshInstance3D.new()
	_tunnel_mesh.name = "TunnelCylinder"
	var cyl := CylinderMesh.new()
	cyl.top_radius = 3.2
	cyl.bottom_radius = 3.2
	cyl.height = 80.0
	# FEEL_BASELINE: Doubled segments to smooth hard polygon edges at altitude.
	cyl.radial_segments = 48
	cyl.rings = 8
	_tunnel_mesh.mesh = cyl
	# Rotate so cylinder extends along Z (default is Y-up).
	_tunnel_mesh.rotation_degrees.x = 90.0

	_material = StandardMaterial3D.new()
	_material.albedo_color = Color(0.1, 0.2, 0.6, tunnel_opacity)
	_material.emission_enabled = true
	_material.emission = Color(0.15, 0.3, 0.8, 1.0)
	_material.emission_energy_multiplier = _base_emission_energy
	_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_material.cull_mode = BaseMaterial3D.CULL_FRONT  # Render inside of cylinder.
	# Scrolling effect via UV offset animation.
	_material.uv1_scale = Vector3(2.0, 6.0, 1.0)
	_tunnel_mesh.material_override = _material

	add_child(_tunnel_mesh)

	# === Inner tunnel layer — tighter cylinder for depth/layering. ===
	_inner_tunnel = MeshInstance3D.new()
	_inner_tunnel.name = "InnerTunnel"
	var inner_cyl := CylinderMesh.new()
	inner_cyl.top_radius = 2.0
	inner_cyl.bottom_radius = 2.0
	inner_cyl.height = 72.0
	# FEEL_BASELINE: Doubled segments to smooth edges.
	inner_cyl.radial_segments = 32
	inner_cyl.rings = 6
	_inner_tunnel.mesh = inner_cyl
	_inner_tunnel.rotation_degrees.x = 90.0

	_inner_material = StandardMaterial3D.new()
	_inner_material.albedo_color = Color(0.2, 0.5, 1.0, 0.15)
	_inner_material.emission_enabled = true
	_inner_material.emission = Color(0.3, 0.6, 1.0, 1.0)
	_inner_material.emission_energy_multiplier = 1.5
	_inner_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_inner_material.cull_mode = BaseMaterial3D.CULL_FRONT
	_inner_material.uv1_scale = Vector3(3.0, 8.0, 1.0)
	_inner_tunnel.material_override = _inner_material

	add_child(_inner_tunnel)

	# Animate UV scroll for both tunnels (motion illusion).
	_animate_uv_scroll()

	# === Layer 1: Main speed streak particles flying past the player. ===
	_streak_particles = GPUParticles3D.new()
	_streak_particles.name = "StreakParticles"
	_streak_particles.amount = 96
	_streak_particles.lifetime = 0.6
	_streak_particles.randomness = 0.3
	_streak_particles.visibility_aabb = AABB(Vector3(-8, -8, -48), Vector3(16, 16, 96))

	var pmat := ParticleProcessMaterial.new()
	pmat.direction = Vector3(0, 0, -1)
	pmat.spread = 12.0
	pmat.initial_velocity_min = 48.0
	pmat.initial_velocity_max = 80.0
	pmat.gravity = Vector3.ZERO
	pmat.scale_min = 0.02
	pmat.scale_max = 0.06
	# Color gradient: bright white core fading to cyan trail.
	var streak_gradient := Gradient.new()
	streak_gradient.set_color(0, Color(1.0, 1.0, 1.0, 1.0))   # White flash
	streak_gradient.add_point(0.3, Color(0.6, 0.85, 1.0, 0.9))  # Bright cyan
	streak_gradient.add_point(1.0, Color(0.2, 0.5, 1.0, 0.0))   # Fade to blue
	var streak_ramp := GradientTexture1D.new()
	streak_ramp.gradient = streak_gradient
	pmat.color_ramp = streak_ramp
	# Emit from a ring around the player.
	pmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	pmat.emission_ring_radius = 2.8
	pmat.emission_ring_inner_radius = 1.2
	pmat.emission_ring_height = 0.5
	pmat.emission_ring_axis = Vector3(0, 0, 1)
	_streak_particles.process_material = pmat

	# Elongated mesh for streak look.
	var streak_mesh := BoxMesh.new()
	streak_mesh.size = Vector3(0.04, 0.04, 1.2)
	_streak_particles.draw_pass_1 = streak_mesh

	add_child(_streak_particles)
	_streak_particles.emitting = true

	# === Layer 2: Fast inner streaks — tighter ring, faster, brighter. ===
	_fast_streaks = GPUParticles3D.new()
	_fast_streaks.name = "FastStreaks"
	_fast_streaks.amount = 48
	_fast_streaks.lifetime = 0.35
	_fast_streaks.randomness = 0.2
	_fast_streaks.visibility_aabb = AABB(Vector3(-5, -5, -40), Vector3(10, 10, 80))

	var fmat := ParticleProcessMaterial.new()
	fmat.direction = Vector3(0, 0, -1)
	fmat.spread = 8.0
	fmat.initial_velocity_min = 80.0
	fmat.initial_velocity_max = 140.0
	fmat.gravity = Vector3.ZERO
	fmat.scale_min = 0.01
	fmat.scale_max = 0.04
	# Bright white-cyan with fast fade.
	var fast_gradient := Gradient.new()
	fast_gradient.set_color(0, Color(1.0, 1.0, 1.0, 1.0))
	fast_gradient.add_point(0.5, Color(0.8, 0.95, 1.0, 0.7))
	fast_gradient.add_point(1.0, Color(0.4, 0.7, 1.0, 0.0))
	var fast_ramp := GradientTexture1D.new()
	fast_ramp.gradient = fast_gradient
	fmat.color_ramp = fast_ramp
	fmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	fmat.emission_ring_radius = 1.6
	fmat.emission_ring_inner_radius = 0.4
	fmat.emission_ring_height = 0.3
	fmat.emission_ring_axis = Vector3(0, 0, 1)
	_fast_streaks.process_material = fmat

	var fast_mesh := BoxMesh.new()
	fast_mesh.size = Vector3(0.03, 0.03, 2.0)
	_fast_streaks.draw_pass_1 = fast_mesh

	add_child(_fast_streaks)
	_fast_streaks.emitting = true

	# === Layer 3: Ambient glow particles — slower, larger, diffuse fog. ===
	_ambient_glow = GPUParticles3D.new()
	_ambient_glow.name = "AmbientGlow"
	_ambient_glow.amount = 24
	_ambient_glow.lifetime = 1.5
	_ambient_glow.randomness = 0.6
	_ambient_glow.visibility_aabb = AABB(Vector3(-6, -6, -32), Vector3(12, 12, 64))

	var gmat := ParticleProcessMaterial.new()
	gmat.direction = Vector3(0, 0, -1)
	gmat.spread = 30.0
	gmat.initial_velocity_min = 12.0
	gmat.initial_velocity_max = 24.0
	gmat.gravity = Vector3.ZERO
	# FEEL_BASELINE: Larger glow particles for softer boundary.
	gmat.scale_min = 0.2
	gmat.scale_max = 0.5
	# Soft blue-purple fog.
	var glow_gradient := Gradient.new()
	glow_gradient.set_color(0, Color(0.3, 0.4, 1.0, 0.3))
	glow_gradient.add_point(0.5, Color(0.2, 0.5, 0.9, 0.2))
	glow_gradient.add_point(1.0, Color(0.1, 0.2, 0.6, 0.0))
	var glow_ramp := GradientTexture1D.new()
	glow_ramp.gradient = glow_gradient
	gmat.color_ramp = glow_ramp
	gmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	# FEEL_BASELINE: Wider ring softens the outer cylinder edge at altitude.
	gmat.emission_ring_radius = 4.0
	gmat.emission_ring_inner_radius = 1.6
	gmat.emission_ring_height = 1.5
	gmat.emission_ring_axis = Vector3(0, 0, 1)
	_ambient_glow.process_material = gmat

	var glow_mesh := SphereMesh.new()
	# FEEL_BASELINE: Larger glow particles for altitude.
	glow_mesh.radius = 0.4
	glow_mesh.height = 0.8
	_ambient_glow.draw_pass_1 = glow_mesh

	add_child(_ambient_glow)
	_ambient_glow.emitting = true

	# === Layer 4: Outer haze — wide, faint ring to soften cylinder boundary. ===
	# FEEL_POST_FIX_7: Softens the hard cylinder edge visible at camera altitude.
	_outer_haze = GPUParticles3D.new()
	_outer_haze.name = "OuterHaze"
	_outer_haze.amount = 32
	_outer_haze.lifetime = 1.2
	_outer_haze.randomness = 0.7
	_outer_haze.visibility_aabb = AABB(Vector3(-12, -12, -48), Vector3(24, 24, 96))

	var hmat := ParticleProcessMaterial.new()
	hmat.direction = Vector3(0, 0, -1)
	hmat.spread = 25.0
	hmat.initial_velocity_min = 16.0
	hmat.initial_velocity_max = 32.0
	hmat.gravity = Vector3.ZERO
	hmat.scale_min = 0.4
	hmat.scale_max = 1.0
	# Very faint blue-white fog to blur the boundary.
	var haze_gradient := Gradient.new()
	haze_gradient.set_color(0, Color(0.4, 0.6, 1.0, 0.15))
	haze_gradient.add_point(0.5, Color(0.3, 0.5, 0.9, 0.1))
	haze_gradient.add_point(1.0, Color(0.2, 0.3, 0.7, 0.0))
	var haze_ramp := GradientTexture1D.new()
	haze_ramp.gradient = haze_gradient
	hmat.color_ramp = haze_ramp
	hmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	hmat.emission_ring_radius = 5.5
	hmat.emission_ring_inner_radius = 3.0
	hmat.emission_ring_height = 2.0
	hmat.emission_ring_axis = Vector3(0, 0, 1)
	_outer_haze.process_material = hmat

	var haze_mesh := SphereMesh.new()
	haze_mesh.radius = 0.6
	haze_mesh.height = 1.2
	_outer_haze.draw_pass_1 = haze_mesh

	add_child(_outer_haze)
	_outer_haze.emitting = true

	# Fade in.
	_material.albedo_color.a = 0.0
	_inner_material.albedo_color.a = 0.0
	var fade_in := create_tween()
	fade_in.tween_property(_material, "albedo_color:a", tunnel_opacity, 0.4)
	fade_in.parallel().tween_property(_inner_material, "albedo_color:a", 0.15, 0.6)


func _animate_uv_scroll() -> void:
	# Continuously scroll UV offset for motion illusion — outer tunnel.
	var tween := create_tween().set_loops()
	tween.tween_property(_material, "uv1_offset:y", -10.0, 1.2).from(0.0)

	# Inner tunnel scrolls faster for parallax depth effect.
	var inner_tween := create_tween().set_loops()
	inner_tween.tween_property(_inner_material, "uv1_offset:y", -10.0, 0.7).from(0.0)
