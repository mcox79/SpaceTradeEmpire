extends Node3D
class_name WarfrontAtmosphere
## Red-tinted particles at warfront nodes.
## Per vfx_visual_roadmap_v0.md warfront visuals

## Current warfront tier (1-3).
var _warfront_tier: int = 1

## Internal refs.
var _particles: GPUParticles3D = null
var _light: OmniLight3D = null

## Tier-based particle amounts.
const TIER_AMOUNTS: Array = [0, 8, 16, 32]
## Tier-based light energies.
const TIER_LIGHT_ENERGIES: Array = [0.0, 0.4, 0.7, 1.2]


## Initialize with warfront tier (1, 2, or 3).
func setup(warfront_tier: int) -> void:
	_warfront_tier = clampi(warfront_tier, 1, 3)
	_build_particles()
	_build_light()


func _build_particles() -> void:
	_particles = GPUParticles3D.new()
	_particles.name = "WarfrontEmbers"
	_particles.amount = TIER_AMOUNTS[_warfront_tier]
	_particles.lifetime = 2.0
	_particles.randomness = 0.6
	_particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	# Spherical spread around station.
	proc_mat.direction = Vector3(0, 0.3, 0)
	proc_mat.spread = 180.0
	proc_mat.initial_velocity_min = 0.5
	proc_mat.initial_velocity_max = 1.5
	proc_mat.gravity = Vector3(0, 0.2, 0)
	proc_mat.scale_min = 0.05
	proc_mat.scale_max = 0.15
	proc_mat.damping_min = 0.3
	proc_mat.damping_max = 1.0
	proc_mat.angular_velocity_min = -90.0
	proc_mat.angular_velocity_max = 90.0

	# Emission sphere for spatial spread (radius 2-4 based on tier).
	proc_mat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_SPHERE
	proc_mat.emission_sphere_radius = 2.0 + _warfront_tier * 0.7

	# Red-orange ember gradient.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 0.5, 0.1, 0.9))       # Orange start
	gradient.add_point(0.3, Color(0.9, 0.2, 0.1, 0.8))      # Deep red
	gradient.add_point(0.7, Color(1.0, 0.3, 0.1, 0.5))      # Flickering red-orange
	gradient.add_point(1.0, Color(0.5, 0.1, 0.05, 0.0))     # Fade out
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp

	_particles.process_material = proc_mat

	# Small sphere draw pass for ember/debris look.
	var mesh := SphereMesh.new()
	mesh.radius = 0.08
	mesh.height = 0.16
	_particles.draw_pass_1 = mesh

	add_child(_particles)


func _build_light() -> void:
	_light = OmniLight3D.new()
	_light.name = "WarfrontGlow"
	_light.light_color = Color(1.0, 0.3, 0.2)
	_light.light_energy = TIER_LIGHT_ENERGIES[_warfront_tier]
	_light.omni_range = 4.0 + _warfront_tier * 1.0
	_light.omni_attenuation = 1.5
	_light.shadow_enabled = false
	add_child(_light)


## Update tier dynamically (e.g., warfront escalation/de-escalation).
func update_tier(new_tier: int) -> void:
	new_tier = clampi(new_tier, 1, 3)
	if new_tier == _warfront_tier:
		return
	_warfront_tier = new_tier

	# Rebuild particles and light with new tier parameters.
	if _particles:
		_particles.queue_free()
		_particles = null
	if _light:
		_light.queue_free()
		_light = null

	_build_particles()
	_build_light()
