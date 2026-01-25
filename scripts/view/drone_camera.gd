extends Camera3D

@export var target_group: String = "player"
@export var follow_distance: float = 18.0
@export var height: float = 8.0
@export var follow_speed: float = 8.0

var _target: Node3D = null

func _ready() -> void:
	current = false
	_target = _find_target()

func _process(delta: float) -> void:
	if _target == null:
		_target = _find_target()
		if _target == null:
			return

	var target_basis: Basis = _target.global_transform.basis
	var forward: Vector3 = -target_basis.z
	var desired: Vector3 = _target.global_position - forward * follow_distance + Vector3.UP * height
	var t: float = clamp(delta * follow_speed, 0.0, 1.0)

	global_position = global_position.lerp(desired, t)
	look_at(_target.global_position, Vector3.UP)

func _find_target() -> Node3D:
	var nodes: Array[Node] = get_tree().get_nodes_in_group(target_group)
	if nodes.size() > 0 and nodes[0] is Node3D:
		return nodes[0] as Node3D
	return null
