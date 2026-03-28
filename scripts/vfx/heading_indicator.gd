extends Node3D

# GATE.T63.SPATIAL.HEADING_INDICATOR.001: Velocity direction indicator.
# Shows a subtle arrow/chevron in front of the player ship pointing in the
# direction of movement. Fades in when moving, fades out when stationary.
# Reference: Elite Dangerous compass, Freelancer velocity vector.

const SPEED_SHOW_THRESHOLD: float = 1.0   # Min speed to show indicator.
const ARROW_DISTANCE: float = 5.0          # Distance ahead of ship.
const FADE_SPEED: float = 4.0              # Alpha lerp speed.

var _ship: RigidBody3D
var _arrow_mesh: MeshInstance3D
var _material: StandardMaterial3D
var _current_alpha: float = 0.0

func _ready() -> void:
	# Walk up the tree to find the RigidBody3D ancestor.
	var node = get_parent()
	while node != null:
		if node is RigidBody3D:
			_ship = node
			break
		node = node.get_parent()
	_build_arrow()

func _build_arrow() -> void:
	_arrow_mesh = MeshInstance3D.new()
	_arrow_mesh.name = "HeadingArrow"
	# Create a simple triangular prism as a chevron/arrow.
	var mesh := PrismMesh.new()
	mesh.size = Vector3(1.5, 0.1, 2.0)  # Wide, flat, pointing forward
	_arrow_mesh.mesh = mesh
	# Rotate so the prism points along +Z (forward direction)
	_arrow_mesh.rotation_degrees.x = -90.0

	_material = StandardMaterial3D.new()
	_material.albedo_color = Color(0.3, 0.85, 1.0, 0.0)  # Cyan, starts invisible
	_material.emission_enabled = true
	_material.emission = Color(0.2, 0.6, 1.0, 1.0)
	_material.emission_energy_multiplier = 2.0
	_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_material.no_depth_test = true
	_material.render_priority = 10
	_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	_arrow_mesh.material_override = _material
	_arrow_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	add_child(_arrow_mesh)

func _process(delta: float) -> void:
	if _ship == null or _material == null:
		return

	var velocity: Vector3 = _ship.linear_velocity
	# Flatten to XZ plane (top-down game).
	velocity.y = 0.0
	var speed: float = velocity.length()

	# Fade in/out based on speed.
	var target_alpha: float = 0.0
	if speed > SPEED_SHOW_THRESHOLD:
		target_alpha = clampf((speed - SPEED_SHOW_THRESHOLD) * 0.15, 0.0, 0.6)
	_current_alpha = lerpf(_current_alpha, target_alpha, delta * FADE_SPEED)
	_material.albedo_color.a = _current_alpha

	if speed > SPEED_SHOW_THRESHOLD:
		# Position arrow ahead of the ship in the velocity direction.
		var dir: Vector3 = velocity.normalized()
		global_position = _ship.global_position + dir * ARROW_DISTANCE
		global_position.y = _ship.global_position.y + 0.5  # Slightly above ship
		# Point the arrow in the velocity direction.
		if dir.length() > 0.1:
			look_at(global_position + dir, Vector3.UP)
