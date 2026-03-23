extends Node3D
class_name StationProsperity
## Station lighting tiers from prosperity signal.
## economy_simulation_v0.md Category 5: Station Visual State

## Current prosperity value.
var _prosperity: float = 0.5
## Current tier (0=Struggling, 1=Stable, 2=Prosperous, 3=Booming).
var _tier: int = 1

## Internal refs.
var _light: OmniLight3D = null
var _aura_particles: GPUParticles3D = null
var _time: float = 0.0

## Tier thresholds and visual parameters.
const TIER_STRUGGLING := 0
const TIER_STABLE := 1
const TIER_PROSPEROUS := 2
const TIER_BOOMING := 3

## Tier colors and energies.
const TIER_COLORS: Array = [
	null,  # Placeholder — Color() can't be const in Array; set in _apply_tier.
	null,
	null,
	null,
]
const TIER_ENERGIES: Array = [0.3, 0.6, 1.0, 1.3]


## Initialize with a prosperity value.
func setup(prosperity: float) -> void:
	_prosperity = prosperity
	_build_light()
	_apply_tier(_prosperity_to_tier(prosperity))


func _build_light() -> void:
	_light = OmniLight3D.new()
	_light.name = "ProsperityLight"
	_light.omni_range = 5.0
	_light.omni_attenuation = 1.5
	_light.shadow_enabled = false
	add_child(_light)


## Map prosperity float to tier index.
func _prosperity_to_tier(p: float) -> int:
	if p < 0.3:
		return TIER_STRUGGLING
	elif p < 0.7:
		return TIER_STABLE
	elif p <= 1.0:
		return TIER_PROSPEROUS
	else:
		return TIER_BOOMING


## Apply visual parameters for the given tier.
func _apply_tier(new_tier: int) -> void:
	_tier = new_tier

	var color: Color
	match _tier:
		TIER_STRUGGLING:
			color = Color(0.6, 0.5, 0.4)
		TIER_STABLE:
			color = Color(0.9, 0.8, 0.6)
		TIER_PROSPEROUS:
			color = Color(1.0, 0.9, 0.7)
		TIER_BOOMING:
			color = Color(1.0, 0.95, 0.8)
		_:
			color = Color(0.9, 0.8, 0.6)

	if _light:
		_light.light_color = color
		_light.light_energy = TIER_ENERGIES[_tier]

	# Booming tier gets a subtle particle aura.
	if _tier == TIER_BOOMING and _aura_particles == null:
		_add_aura()
	elif _tier != TIER_BOOMING and _aura_particles != null:
		_aura_particles.queue_free()
		_aura_particles = null


## Update prosperity dynamically (e.g., from periodic bridge poll).
func update_prosperity(new_val: float) -> void:
	_prosperity = new_val
	var new_tier := _prosperity_to_tier(new_val)
	if new_tier != _tier:
		_apply_tier(new_tier)


func _process(delta: float) -> void:
	_time += delta
	# Struggling tier: flicker effect.
	if _tier == TIER_STRUGGLING and _light:
		var flicker := sin(_time * 8.0) * 0.15
		_light.light_energy = TIER_ENERGIES[TIER_STRUGGLING] + flicker


## Add subtle golden particle aura for booming stations.
func _add_aura() -> void:
	_aura_particles = GPUParticles3D.new()
	_aura_particles.name = "BoomAuraParticles"
	_aura_particles.amount = 8
	_aura_particles.lifetime = 1.5
	_aura_particles.randomness = 0.5
	_aura_particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 1, 0)
	proc_mat.spread = 180.0
	proc_mat.initial_velocity_min = 0.2
	proc_mat.initial_velocity_max = 0.6
	proc_mat.gravity = Vector3(0, 0.1, 0)
	proc_mat.scale_min = 0.05
	proc_mat.scale_max = 0.15
	proc_mat.damping_min = 0.5
	proc_mat.damping_max = 1.5

	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 0.95, 0.7, 0.4))
	gradient.add_point(0.5, Color(1.0, 0.9, 0.5, 0.2))
	gradient.add_point(1.0, Color(1.0, 0.85, 0.4, 0.0))
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp

	_aura_particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.1
	mesh.height = 0.2
	_aura_particles.draw_pass_1 = mesh

	add_child(_aura_particles)
