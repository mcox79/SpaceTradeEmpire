extends RefCounted

var credits: int = 1000
var cargo: Dictionary = {}
var cargo_capacity: int = 50
var current_node_id: String = ""

func get_cargo_count(item_id: String) -> int:
	return cargo.get(item_id, 0)