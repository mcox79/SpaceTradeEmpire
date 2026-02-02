extends RefCounted

var id: String
var current_pos: Vector3
var from: Vector3
var to: Vector3
var path: PackedVector3Array = []
var path_index: int = 0
var speed: float = 50.0 # INCREASED: Ensure prompt delivery for tests
var active_order_ref = null
var cargo_hold: Dictionary = {}

func _init(p_id: String, p_pos: Vector3):
	id = p_id
	current_pos = p_pos
	from = p_pos
	to = p_pos