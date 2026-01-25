extends Camera3D

@export var target_group: String = "player"
@export var follow_distance: float = 18.0
@export var height: float = 8.0
@export var look_ahead: float = 2.0
@export var follow_speed: float = 8.0

var _target: Node3D = null

func _ready() -> void:
current = true
_target = _find_target()
get_tree().node_added.connect(_on_node_added)

func _on_node_added(n: Node) -> void:
if _target == null and n is Node3D and n.is_in_group(target_group):
_target = n

func _process(delta: float) -> void:
if _target == null:
_target = _find_target()
if _target == null:
return

var forward := -_target.global_transform.basis.z
var desired_pos := _target.global_position - forward * follow_distance + Vector3.UP * height
var t := clamp(delta * follow_speed, 0.0, 1.0)

global_position = global_position.lerp(desired_pos, t)
look_at(_target.global_position + forward * look_ahead, Vector3.UP)

func _find_target() -> Node3D:
var nodes := get_tree().get_nodes_in_group(target_group)
if nodes.size() > 0 and nodes[0] is Node3D:
return nodes[0]
return null