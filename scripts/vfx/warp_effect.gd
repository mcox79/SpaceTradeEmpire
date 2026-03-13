extends Node3D
class_name WarpEffect
## GATE.S16.NPC_ALIVE.WARP_VFX.001
## GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001
## GATE.X.WARP.DEPARTURE_VFX.001
## Warp-in/warp-out visual effect for NPC ships at lane gates.
## Enhanced with pulsating sphere, speed streaks, and color shifts.
## Player departure flash for warp launch feedback.
## Usage:
##   WarpEffect.play_warp_in(parent, position)
##   WarpEffect.play_warp_out(ship_node, callback)
##   WarpEffect.play_departure_flash(parent, position)


## Play warp-in effect: blue/white particle burst + expanding flash + speed streaks.
## Returns the effect node (auto-frees after animation).
static func play_warp_in(parent: Node, pos: Vector3, duration: float = 0.5) -> Node3D:
	var effect := Node3D.new()
	effect.name = "WarpInVfx"
	effect.position = pos
	parent.add_child(effect)

	# Particle burst — more particles, wider spread, color gradient.
	var particles := _create_warp_particles()
	effect.add_child(particles)
	particles.emitting = true

	# Speed streak particles — elongated lines radiating inward (implosion feel).
	var streaks := _create_speed_streaks(true)
	effect.add_child(streaks)
	streaks.emitting = true

	# Scale flash (bright sphere that expands then contracts).
	var flash := _create_flash_mesh()
	effect.add_child(flash)

	var tween := effect.create_tween()
	# Phase 1: Sphere rapidly expands with bright flash.
	tween.tween_property(flash, "scale", Vector3(4.0, 4.0, 4.0), duration * 0.3) \
		.from(Vector3(0.1, 0.1, 0.1)) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	# Phase 2: Sphere contracts and fades.
	tween.tween_property(flash, "scale", Vector3(0.3, 0.3, 0.3), duration * 0.7) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)
	tween.parallel().tween_property(flash, "transparency", 1.0, duration * 0.7)

	# Color shift on the flash: white -> cyan -> blue.
	var mat: StandardMaterial3D = flash.material_override
	if mat:
		var color_tween := effect.create_tween()
		color_tween.tween_method(
			func(t: float):
				if is_instance_valid(mat):
					var c: Color
					if t < 0.5:
						c = Color(1.0, 1.0, 1.0).lerp(Color(0.3, 0.9, 1.0), t * 2.0)
					else:
						c = Color(0.3, 0.9, 1.0).lerp(Color(0.2, 0.4, 1.0), (t - 0.5) * 2.0)
					mat.emission = c,
			0.0, 1.0, duration
		)

	# Auto-cleanup
	var tree := parent.get_tree()
	if tree:
		var timer := tree.create_timer(duration + 0.5)
		timer.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect


## Play warp-out effect on ship: streak particles + pulsating scale-down, then queue_free.
static func play_warp_out(ship: Node3D, duration: float = 0.5) -> void:
	if ship == null or not is_instance_valid(ship):
		return

	# Streak particles — elongated lines radiating outward.
	var particles := _create_warp_particles()
	particles.lifetime = 0.4
	ship.add_child(particles)
	particles.emitting = true

	# Outward speed streaks.
	var streaks := _create_speed_streaks(false)
	ship.add_child(streaks)
	streaks.emitting = true

	# Pulsating scale-down: brief expand then shrink to zero.
	var tween := ship.create_tween()
	# Quick pulse outward.
	tween.tween_property(ship, "scale", Vector3(1.3, 1.3, 1.3), duration * 0.15) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	# Rapid collapse.
	tween.tween_property(ship, "scale", Vector3.ZERO, duration * 0.85) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)
	tween.tween_callback(ship.queue_free)


## GATE.X.WARP.DEPARTURE_VFX.001
## Play departure flash at the player's position when initiating warp travel.
## Shorter and slightly less intense than warp_in — purposeful launch, not impact.
## Creates: outward particle burst + rapid white flash sphere + diverging speed streaks.
## Returns the effect node (auto-frees after animation).
static func play_departure_flash(parent: Node, pos: Vector3, duration: float = 0.4) -> Node3D:
	var effect := Node3D.new()
	effect.name = "WarpDepartureVfx"
	effect.position = pos
	parent.add_child(effect)

	# Outward particle burst — energy releasing as ship launches.
	var particles := _create_departure_particles()
	effect.add_child(particles)
	particles.emitting = true

	# Diverging speed streaks — radiate outward from departure point.
	var streaks := _create_speed_streaks(false)
	streaks.lifetime = 0.3
	effect.add_child(streaks)
	streaks.emitting = true

	# Bright white flash sphere — rapid expand and fade (shorter than warp_in).
	var flash := _create_departure_flash_mesh()
	effect.add_child(flash)

	var tween := effect.create_tween()
	# Rapid expansion — white sphere bursts outward quickly.
	tween.tween_property(flash, "scale", Vector3(3.0, 3.0, 3.0), duration * 0.25) \
		.from(Vector3(0.2, 0.2, 0.2)) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	# Quick fade — sphere dissipates as ship enters warp.
	tween.tween_property(flash, "scale", Vector3(5.0, 5.0, 5.0), duration * 0.75) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	tween.parallel().tween_property(flash, "transparency", 1.0, duration * 0.75)

	# Color shift: bright white -> cyan (departure is cleaner/sharper than arrival).
	var mat: StandardMaterial3D = flash.material_override
	if mat:
		var color_tween := effect.create_tween()
		color_tween.tween_method(
			func(t: float):
				if is_instance_valid(mat):
					var c := Color(1.0, 1.0, 1.0).lerp(Color(0.4, 0.8, 1.0), t)
					mat.emission = c,
			0.0, 1.0, duration
		)

	# Auto-cleanup.
	var tree := parent.get_tree()
	if tree:
		var timer := tree.create_timer(duration + 0.5)
		timer.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect


## Departure-specific particles: fewer, faster, outward-directed.
static func _create_departure_particles() -> GPUParticles3D:
	var particles := GPUParticles3D.new()
	particles.name = "DepartureParticles"
	particles.amount = 24
	particles.lifetime = 0.35
	particles.one_shot = true
	particles.explosiveness = 0.95
	particles.randomness = 0.25
	particles.emitting = false

	var mat := ParticleProcessMaterial.new()
	mat.direction = Vector3(0, 0, 0)
	mat.spread = 180.0
	mat.initial_velocity_min = 12.0
	mat.initial_velocity_max = 35.0
	mat.gravity = Vector3.ZERO
	mat.scale_min = 0.06
	mat.scale_max = 0.18
	# Color gradient: bright white core to cyan trail, fast fade.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 1.0, 1.0, 1.0))       # White flash
	gradient.add_point(0.25, Color(0.7, 0.9, 1.0, 0.85))    # Bright cyan
	gradient.add_point(0.7, Color(0.3, 0.6, 0.9, 0.4))      # Fading blue
	gradient.add_point(1.0, Color(0.15, 0.3, 0.7, 0.0))     # Gone
	var ramp := GradientTexture1D.new()
	ramp.gradient = gradient
	mat.color_ramp = ramp
	particles.process_material = mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.12
	mesh.height = 0.24
	particles.draw_pass_1 = mesh

	return particles


## Departure flash mesh: brighter white, higher initial emission than warp_in flash.
static func _create_departure_flash_mesh() -> MeshInstance3D:
	var mesh := MeshInstance3D.new()
	mesh.name = "DepartureFlash"
	var sphere := SphereMesh.new()
	sphere.radius = 0.8
	sphere.height = 1.6
	sphere.radial_segments = 12
	sphere.rings = 6
	mesh.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.85, 0.92, 1.0, 0.6)
	mat.emission_enabled = true
	mat.emission = Color(1.0, 1.0, 1.0, 1.0)
	mat.emission_energy_multiplier = 8.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.no_depth_test = true
	mesh.material_override = mat
	return mesh


static func _create_warp_particles() -> GPUParticles3D:
	var particles := GPUParticles3D.new()
	particles.name = "WarpParticles"
	particles.amount = 32
	particles.lifetime = 0.5
	particles.one_shot = true
	particles.explosiveness = 0.9
	particles.randomness = 0.3
	particles.emitting = false

	var mat := ParticleProcessMaterial.new()
	mat.direction = Vector3(0, 0, 0)
	mat.spread = 180.0
	mat.initial_velocity_min = 8.0
	mat.initial_velocity_max = 25.0
	mat.gravity = Vector3.ZERO
	mat.scale_min = 0.08
	mat.scale_max = 0.25
	# Color gradient: white core to blue-cyan trail.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 1.0, 1.0, 1.0))      # White flash
	gradient.add_point(0.3, Color(0.5, 0.8, 1.0, 0.9))     # Bright blue
	gradient.add_point(1.0, Color(0.2, 0.4, 0.8, 0.0))     # Fade out blue
	var ramp := GradientTexture1D.new()
	ramp.gradient = gradient
	mat.color_ramp = ramp
	particles.process_material = mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.15
	mesh.height = 0.3
	particles.draw_pass_1 = mesh

	return particles


## Speed streaks: elongated particles that converge (warp_in) or diverge (warp_out).
static func _create_speed_streaks(converging: bool) -> GPUParticles3D:
	var streaks := GPUParticles3D.new()
	streaks.name = "SpeedStreaks"
	streaks.amount = 24
	streaks.lifetime = 0.4
	streaks.one_shot = true
	streaks.explosiveness = 0.85
	streaks.randomness = 0.2
	streaks.emitting = false

	var mat := ParticleProcessMaterial.new()
	mat.direction = Vector3(0, 0, 0)
	mat.spread = 180.0
	if converging:
		# Inward convergence (particles start far, move in).
		mat.initial_velocity_min = -20.0
		mat.initial_velocity_max = -8.0
	else:
		# Outward divergence.
		mat.initial_velocity_min = 15.0
		mat.initial_velocity_max = 40.0
	mat.gravity = Vector3.ZERO
	mat.scale_min = 0.01
	mat.scale_max = 0.03
	# Emit from a sphere shell.
	mat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_SPHERE
	mat.emission_sphere_radius = 4.0
	# Bright cyan-white.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(0.8, 0.95, 1.0, 1.0))
	gradient.add_point(0.6, Color(0.4, 0.7, 1.0, 0.6))
	gradient.add_point(1.0, Color(0.1, 0.3, 0.8, 0.0))
	var ramp := GradientTexture1D.new()
	ramp.gradient = gradient
	mat.color_ramp = ramp
	streaks.process_material = mat

	# Elongated box for speed-line look.
	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.03, 0.03, 1.5)
	streaks.draw_pass_1 = mesh

	return streaks


static func _create_flash_mesh() -> MeshInstance3D:
	var mesh := MeshInstance3D.new()
	mesh.name = "WarpFlash"
	var sphere := SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	sphere.radial_segments = 16
	sphere.rings = 8
	mesh.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.6, 0.85, 1.0, 0.7)
	mat.emission_enabled = true
	mat.emission = Color(0.7, 0.9, 1.0, 1.0)
	mat.emission_energy_multiplier = 6.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.no_depth_test = true
	mesh.material_override = mat
	return mesh
