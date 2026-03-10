extends Node3D
class_name ExplosionEffect
## GATE.S7.COMBAT_JUICE.EXPLOSION_VFX.001
## Multi-phase kill explosion for NPC ship death.
## Usage:
##   ExplosionEffect.spawn(parent, position)
## Auto-frees after all phases complete (~2.0s).

## Total effect duration before auto-free.
const TOTAL_DURATION: float = 2.0


## Spawn a multi-phase explosion at the given world position.
## Adds itself as a child of `parent`.
static func spawn(parent: Node, pos: Vector3) -> Node3D:
	var effect := Node3D.new()
	effect.name = "ExplosionVfx"
	effect.position = pos
	parent.add_child(effect)

	# Phase 1 (0.0–0.1s): White flash burst (OmniLight3D).
	_add_flash(effect)

	# Phase 2 (0.1–0.5s): Orange/yellow fireball (GPUParticles3D).
	_add_fireball(effect)

	# Phase 3 (0.3–1.0s): Debris chunks flying outward.
	_add_debris(effect)

	# Phase 4 (0.5–1.5s): Lingering smoke/ember particles.
	_add_smoke(effect)

	# Auto-cleanup after total duration.
	var tree := parent.get_tree()
	if tree:
		var timer := tree.create_timer(TOTAL_DURATION + 0.2)
		timer.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect


## Phase 1: Bright white flash that fades quickly.
static func _add_flash(effect: Node3D) -> void:
	var light := OmniLight3D.new()
	light.name = "FlashLight"
	light.light_color = Color(1.0, 0.95, 0.8)
	light.light_energy = 8.0
	light.omni_range = 40.0
	light.omni_attenuation = 1.5
	effect.add_child(light)

	# Visible flash sphere — scaled for camera altitude ~80 visibility.
	var flash_mesh := MeshInstance3D.new()
	flash_mesh.name = "FlashSphere"
	var sphere := SphereMesh.new()
	sphere.radius = 12.0
	sphere.height = 24.0
	flash_mesh.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(1.0, 1.0, 1.0, 0.9)
	mat.emission_enabled = true
	mat.emission = Color(1.0, 0.95, 0.8)
	mat.emission_energy_multiplier = 10.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.no_depth_test = true
	flash_mesh.material_override = mat
	flash_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	effect.add_child(flash_mesh)

	# Tween: flash fades out over 0.15s.
	var tween := effect.create_tween()
	tween.tween_property(light, "light_energy", 0.0, 0.15)
	tween.parallel().tween_property(flash_mesh, "scale", Vector3(3.0, 3.0, 3.0), 0.15)
	tween.parallel().tween_method(
		func(v: float):
			if is_instance_valid(mat):
				mat.albedo_color.a = v,
		0.9, 0.0, 0.15
	)
	tween.tween_callback(func():
		if is_instance_valid(light): light.queue_free()
		if is_instance_valid(flash_mesh): flash_mesh.queue_free()
	)


## Phase 2: Orange/yellow expanding fireball.
static func _add_fireball(effect: Node3D) -> void:
	var particles := GPUParticles3D.new()
	particles.name = "FireballParticles"
	particles.amount = 32
	particles.lifetime = 0.6
	particles.one_shot = true
	particles.explosiveness = 0.85
	particles.randomness = 0.3
	particles.emitting = false
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 0.5, 0)
	proc_mat.spread = 180.0
	proc_mat.initial_velocity_min = 15.0
	proc_mat.initial_velocity_max = 45.0
	proc_mat.gravity = Vector3(0, 0, 0)
	proc_mat.scale_min = 1.5
	proc_mat.scale_max = 4.0
	proc_mat.damping_min = 3.0
	proc_mat.damping_max = 6.0
	# Orange-yellow gradient via color ramp.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 0.9, 0.3, 1.0))   # Bright yellow
	gradient.add_point(0.4, Color(1.0, 0.5, 0.1, 0.9))  # Orange
	gradient.add_point(1.0, Color(0.5, 0.1, 0.0, 0.0))  # Faded dark red
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 1.2
	mesh.height = 2.4
	particles.draw_pass_1 = mesh

	effect.add_child(particles)

	# Delay start by 0.1s (after flash).
	var tree := effect.get_tree()
	if tree:
		var delay_timer := tree.create_timer(0.1)
		delay_timer.timeout.connect(func():
			if is_instance_valid(particles):
				particles.emitting = true
		)


## Phase 3: Debris chunks flying outward.
static func _add_debris(effect: Node3D) -> void:
	var particles := GPUParticles3D.new()
	particles.name = "DebrisParticles"
	particles.amount = 20
	particles.lifetime = 1.0
	particles.one_shot = true
	particles.explosiveness = 0.9
	particles.randomness = 0.5
	particles.emitting = false
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 0.3, 0)
	proc_mat.spread = 180.0
	proc_mat.initial_velocity_min = 30.0
	proc_mat.initial_velocity_max = 70.0
	proc_mat.gravity = Vector3(0, -2.0, 0)
	proc_mat.scale_min = 0.8
	proc_mat.scale_max = 2.5
	proc_mat.angular_velocity_min = -720.0
	proc_mat.angular_velocity_max = 720.0
	proc_mat.damping_min = 1.0
	proc_mat.damping_max = 3.0
	proc_mat.color = Color(0.5, 0.4, 0.3, 1.0)  # Dull metallic
	particles.process_material = proc_mat

	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.8, 0.4, 0.5)
	particles.draw_pass_1 = mesh

	effect.add_child(particles)

	# Delay start by 0.3s.
	var tree := effect.get_tree()
	if tree:
		var delay_timer := tree.create_timer(0.3)
		delay_timer.timeout.connect(func():
			if is_instance_valid(particles):
				particles.emitting = true
		)


## Phase 4: Lingering smoke/ember particles.
static func _add_smoke(effect: Node3D) -> void:
	var particles := GPUParticles3D.new()
	particles.name = "SmokeParticles"
	particles.amount = 24
	particles.lifetime = 1.4
	particles.one_shot = true
	particles.explosiveness = 0.4
	particles.randomness = 0.6
	particles.emitting = false
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 1.0, 0)
	proc_mat.spread = 60.0
	proc_mat.initial_velocity_min = 8.0
	proc_mat.initial_velocity_max = 18.0
	proc_mat.gravity = Vector3(0, 0.5, 0)  # Drift upward in space
	proc_mat.scale_min = 0.8
	proc_mat.scale_max = 2.0
	proc_mat.damping_min = 1.0
	proc_mat.damping_max = 2.0
	# Smoke: grey with embers.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 0.6, 0.2, 0.6))    # Embers
	gradient.add_point(0.3, Color(0.4, 0.35, 0.3, 0.5))  # Dark smoke
	gradient.add_point(1.0, Color(0.3, 0.3, 0.3, 0.0))   # Fade out
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.8
	mesh.height = 1.6
	particles.draw_pass_1 = mesh

	effect.add_child(particles)

	# Delay start by 0.5s.
	var tree := effect.get_tree()
	if tree:
		var delay_timer := tree.create_timer(0.5)
		delay_timer.timeout.connect(func():
			if is_instance_valid(particles):
				particles.emitting = true
		)
