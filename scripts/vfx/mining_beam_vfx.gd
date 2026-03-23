extends Node3D
class_name MiningBeamVFX
## Green/amber extraction beam at industry nodes.
## economy_simulation_v0.md Category 2: Mining Operations

## Industry type determines beam color.
var _industry_type: String = "mine"
## Prosperity scales particle amount.
var _prosperity: float = 1.0

## Beam endpoint offset (downward toward imaginary asteroid/surface).
const BEAM_LENGTH: float = 3.0
## Internal refs.
var _particles: GPUParticles3D = null


## Initialize the mining beam with industry type and prosperity level.
func setup(industry_type: String, prosperity: float) -> void:
	_industry_type = industry_type
	_prosperity = clampf(prosperity, 0.0, 2.0)
	_build_particles()


func _build_particles() -> void:
	_particles = GPUParticles3D.new()
	_particles.name = "MiningBeamParticles"

	# Scale particle count with prosperity (4-12).
	var amount := clampi(int(4 + _prosperity * 4.0), 4, 12)
	_particles.amount = amount
	_particles.lifetime = 0.8
	_particles.explosiveness = 0.1
	_particles.randomness = 0.2
	_particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	# Beam direction: downward from station.
	proc_mat.direction = Vector3(0, -1, 0)
	proc_mat.spread = 8.0
	proc_mat.initial_velocity_min = 2.0
	proc_mat.initial_velocity_max = 4.0
	# Pull particles toward beam endpoint.
	proc_mat.gravity = Vector3(0, -2.0, 0)
	proc_mat.scale_min = 0.1
	proc_mat.scale_max = 0.3
	proc_mat.damping_min = 1.0
	proc_mat.damping_max = 3.0

	# Color based on industry type.
	var beam_color: Color
	if _industry_type == "fuel_well":
		beam_color = Color(0.2, 0.8, 0.3)  # Green
	else:
		beam_color = Color(0.9, 0.7, 0.2)  # Amber (mine and others)

	var gradient := Gradient.new()
	gradient.set_color(0, Color(beam_color.r, beam_color.g, beam_color.b, 1.0))
	gradient.add_point(0.6, Color(beam_color.r, beam_color.g, beam_color.b, 0.7))
	gradient.add_point(1.0, Color(beam_color.r * 0.5, beam_color.g * 0.5, beam_color.b * 0.3, 0.0))
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp

	_particles.process_material = proc_mat

	# Elongated draw pass for beam-like appearance.
	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.05, 0.4, 0.05)
	_particles.draw_pass_1 = mesh

	# Emit from slightly above center (station base).
	_particles.position = Vector3(0, 0.5, 0)

	add_child(_particles)
