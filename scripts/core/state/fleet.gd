extends RefCounted
class_name Fleet

# ARCHITECTURE: A pure data struct. Zero dependency on the Scene Tree.

var id: String
var from_pos: Vector3
var to_pos: Vector3
var current_pos: Vector3
var progress: float = 0.0 # 0.0 to 1.0
var speed: float = 0.1 # Advances 10% per 5-second tick

func _init(p_id: String, p_from: Vector3, p_to: Vector3):
	id = p_id
	from_pos = p_from
	to_pos = p_to
	current_pos = p_from
