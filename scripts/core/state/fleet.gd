extends RefCounted

var id: String
var from: Vector3 
var to: Vector3   
var current_pos: Vector3

# NEW: Pathfinding State
var path: PackedVector3Array = []
var path_index: int = 0
var speed: float = 0.5 # Distance per tick (in Godot units)

func _init(p_id: String, p_pos: Vector3):
	id = p_id
	current_pos = p_pos
	from = p_pos
	to = p_pos
