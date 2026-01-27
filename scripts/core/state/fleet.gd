extends RefCounted

var id: String
var current_pos: Vector3
var from: Vector3    # Visual Interpolation Start
var to: Vector3      # Visual Interpolation End

var speed: float = 20.0
var path: PackedVector3Array = []
var path_index: int = 0
var active_order_ref = null

func _init(p_id: String, p_pos: Vector3):
	id = p_id
	current_pos = p_pos
	from = p_pos
	to = p_pos