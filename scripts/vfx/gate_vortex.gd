extends Node3D
## Departure vortex VFX for gate transit charge-up.
## Uses the gate_portal ripple shader on a disc, intensifying during charge.
## Plays for ~1.5s during the charge phase, then despawns.

var _portal_mesh: MeshInstance3D
var _shader_mat: ShaderMaterial
var _swirl_particles: GPUParticles3D


func setup() -> void:
	name = "GateVortex"
	_build_portal_disc()
	_build_swirl_particles()
	_animate_charge()


## Staged setup: portal fades in gently (30% intensity). Call charge_full() later.
## Used by the E-key gate approach so the gate "wakes up" during ship approach.
func setup_staged() -> void:
	name = "GateVortex"
	_build_portal_disc()
	_build_swirl_particles()
	var tween := create_tween()
	tween.tween_method(_set_alpha, 0.0, 0.3, 0.6)
	tween.parallel().tween_method(_set_emission_energy, 0.5, 2.0, 0.6)
	tween.parallel().tween_property(self, "scale", Vector3(0.9, 0.9, 0.9), 0.6) \
		.from(Vector3(0.5, 0.5, 0.5)).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)


## Full charge: ramp from staged state to peak intensity.
func charge_full(duration: float = 1.0) -> void:
	var tween := create_tween()
	tween.tween_method(_set_alpha, 0.3, 0.85, duration * 0.3)
	tween.parallel().tween_method(_set_emission_energy, 2.0, 8.0, duration) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	tween.parallel().tween_method(_set_ripple_speed, 1.0, 3.5, duration)
	tween.parallel().tween_method(_set_swirl_speed, 0.2, 1.5, duration)
	tween.parallel().tween_method(_set_distortion, 0.03, 0.12, duration)
	tween.parallel().tween_property(self, "scale", Vector3(1.15, 1.15, 1.15), duration) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)


## Brief emission spike at aperture moment (commitment flash).
func flash_peak(emission: float = 12.0, duration: float = 0.15) -> void:
	var tween := create_tween()
	tween.tween_method(_set_emission_energy, 8.0, emission, duration * 0.3)
	tween.tween_method(_set_emission_energy, emission, 6.0, duration * 0.7)


func despawn(duration: float = 0.3) -> void:
	var tween := create_tween()
	if _shader_mat:
		tween.tween_method(_set_emission_energy, 8.0, 0.0, duration)
		tween.parallel().tween_method(_set_alpha, 0.85, 0.0, duration)
	tween.tween_callback(queue_free)


func _set_emission_energy(val: float) -> void:
	if _shader_mat:
		_shader_mat.set_shader_parameter("emission_energy", val)

func _set_alpha(val: float) -> void:
	if _shader_mat:
		_shader_mat.set_shader_parameter("alpha_base", val)

func _set_ripple_speed(val: float) -> void:
	if _shader_mat:
		_shader_mat.set_shader_parameter("ripple_speed", val)

func _set_swirl_speed(val: float) -> void:
	if _shader_mat:
		_shader_mat.set_shader_parameter("swirl_speed", val)

func _set_distortion(val: float) -> void:
	if _shader_mat:
		_shader_mat.set_shader_parameter("distortion_strength", val)


func _build_portal_disc() -> void:
	var shader := load("res://scripts/vfx/gate_portal.gdshader") as Shader
	if shader == null:
		return

	_shader_mat = ShaderMaterial.new()
	_shader_mat.shader = shader
	# Start dim — will animate to full intensity.
	_shader_mat.set_shader_parameter("emission_energy", 1.0)
	_shader_mat.set_shader_parameter("alpha_base", 0.0)
	_shader_mat.set_shader_parameter("ripple_speed", 1.0)
	_shader_mat.set_shader_parameter("swirl_speed", 0.2)
	_shader_mat.set_shader_parameter("distortion_strength", 0.03)
	# Shift colors toward brighter cyan-white for the charge-up.
	_shader_mat.set_shader_parameter("color_core", Color(0.4, 0.65, 1.0, 1.0))
	_shader_mat.set_shader_parameter("color_rim", Color(0.2, 0.4, 0.9, 1.0))
	_shader_mat.set_shader_parameter("color_highlight", Color(0.85, 0.95, 1.0, 1.0))

	_portal_mesh = MeshInstance3D.new()
	_portal_mesh.name = "VortexPortal"
	# Match the torus inner radius (6u) → diameter 12u disc.
	var plane := PlaneMesh.new()
	plane.size = Vector2(12.0, 12.0)
	_portal_mesh.mesh = plane
	_portal_mesh.material_override = _shader_mat
	# Upright disc — same orientation as the torus (rotated 90° on X).
	_portal_mesh.rotation = Vector3(deg_to_rad(90.0), 0.0, 0.0)
	_portal_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_portal_mesh)


func _build_swirl_particles() -> void:
	_swirl_particles = GPUParticles3D.new()
	_swirl_particles.name = "VortexSwirl"
	_swirl_particles.amount = 48
	_swirl_particles.lifetime = 1.0
	_swirl_particles.randomness = 0.3
	_swirl_particles.visibility_aabb = AABB(Vector3(-15, -5, -15), Vector3(30, 10, 30))

	var pmat := ParticleProcessMaterial.new()
	# Emit from a ring matching the torus.
	pmat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING
	pmat.emission_ring_radius = 5.0
	pmat.emission_ring_inner_radius = 2.0
	pmat.emission_ring_height = 0.3
	pmat.emission_ring_axis = Vector3(0, 1, 0)
	# Orbital swirl — particles spiral inward.
	pmat.direction = Vector3(1, 0, 0)
	pmat.spread = 180.0
	pmat.initial_velocity_min = 2.0
	pmat.initial_velocity_max = 5.0
	pmat.angular_velocity_min = 150.0
	pmat.angular_velocity_max = 280.0
	pmat.gravity = Vector3.ZERO
	pmat.scale_min = 0.06
	pmat.scale_max = 0.12

	# Cyan-white to match portal shader palette.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(0.7, 0.9, 1.0, 0.9))
	gradient.add_point(0.5, Color(0.4, 0.7, 1.0, 0.7))
	gradient.add_point(1.0, Color(0.2, 0.4, 0.9, 0.0))
	var ramp := GradientTexture1D.new()
	ramp.gradient = gradient
	pmat.color_ramp = ramp
	_swirl_particles.process_material = pmat

	var pmesh := BoxMesh.new()
	pmesh.size = Vector3(0.08, 0.08, 0.5)
	_swirl_particles.draw_pass_1 = pmesh
	add_child(_swirl_particles)
	_swirl_particles.emitting = true


func _animate_charge() -> void:
	# Portal fades in and intensifies over 1.5s charge time.
	var tween := create_tween()

	# Fade in alpha.
	tween.tween_method(_set_alpha, 0.0, 0.85, 0.4)
	# Ramp emission energy (dim → bright).
	tween.parallel().tween_method(_set_emission_energy, 1.0, 8.0, 1.3) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	# Accelerate ripples (slow → frantic).
	tween.parallel().tween_method(_set_ripple_speed, 1.0, 3.5, 1.5)
	# Accelerate swirl.
	tween.parallel().tween_method(_set_swirl_speed, 0.2, 1.5, 1.5)
	# Increase distortion (calm → turbulent).
	tween.parallel().tween_method(_set_distortion, 0.03, 0.12, 1.5)
	# Scale the disc slightly larger as energy builds.
	tween.parallel().tween_property(self, "scale", Vector3(1.15, 1.15, 1.15), 1.5) \
		.from(Vector3(0.8, 0.8, 0.8)) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
