extends Node3D
## Thrust-reactive engine glow controller.
## Attached to the EngineGlow node built by ShipMeshBuilder.
## Scales cone length, emission intensity, and light energy with thrust.
##
## For player ship: reads Input actions + RigidBody3D velocity.
## For NPC ships: reads CharacterBody3D velocity (no input).

# Glow intensity range.
const IDLE_EMISSION: float = 0.5       # Faint glow when engines off.
const FULL_EMISSION: float = 6.0       # Bright burn at full thrust.
const IDLE_CONE_SCALE: float = 0.3     # Cone barely visible at idle.
const FULL_CONE_SCALE: float = 1.0     # Full cone at full thrust.
const IDLE_LIGHT_ENERGY: float = 0.1
const FULL_LIGHT_ENERGY: float = 0.8
const SMOOTHING: float = 8.0           # How fast glow responds (lerp rate).

var _cone: MeshInstance3D
var _light: OmniLight3D
var _mat: StandardMaterial3D
var _ship_body: Node  # RigidBody3D or CharacterBody3D (found at ready).
var _current_intensity: float = 0.0

func _ready() -> void:
	_cone = get_node_or_null("GlowCone")
	_light = get_node_or_null("EngineLight")
	if _cone:
		_mat = _cone.material_override as StandardMaterial3D
	# Walk up the tree to find ship body.
	var node := get_parent()
	while node != null:
		if node is RigidBody3D or node is CharacterBody3D:
			_ship_body = node
			break
		node = node.get_parent()

func _process(delta: float) -> void:
	var target_intensity := _compute_thrust_factor()
	_current_intensity = lerpf(_current_intensity, target_intensity, 1.0 - exp(-SMOOTHING * delta))

	if _cone:
		# Scale cone length with thrust (Y is length because we rotated it).
		var s := lerpf(IDLE_CONE_SCALE, FULL_CONE_SCALE, _current_intensity)
		_cone.scale = Vector3(s, s, s)

	if _mat:
		_mat.emission_energy_multiplier = lerpf(IDLE_EMISSION, FULL_EMISSION, _current_intensity)
		_mat.albedo_color.a = lerpf(0.15, 0.7, _current_intensity)

	if _light:
		_light.light_energy = lerpf(IDLE_LIGHT_ENERGY, FULL_LIGHT_ENERGY, _current_intensity)


## Compute 0..1 thrust factor from ship state.
func _compute_thrust_factor() -> float:
	if _ship_body == null:
		return 0.0

	var speed: float = 0.0
	var thrusting: bool = false

	if _ship_body is RigidBody3D:
		# Player ship: check input + velocity.
		speed = (_ship_body as RigidBody3D).linear_velocity.length()
		thrusting = (
			Input.is_action_pressed("ship_thrust_fwd") or
			Input.is_action_pressed("ship_thrust_back")
		)
	elif _ship_body is CharacterBody3D:
		# NPC ship: velocity only (no input).
		speed = (_ship_body as CharacterBody3D).velocity.length()
		thrusting = speed > 1.0  # NPCs are "thrusting" when moving.

	# Combine: thrusting = strong glow, coasting = medium, stopped = idle.
	if thrusting:
		return clampf(0.5 + speed / 36.0, 0.5, 1.0)
	else:
		return clampf(speed / 18.0, 0.0, 0.4)  # Coasting: faint proportional glow.
