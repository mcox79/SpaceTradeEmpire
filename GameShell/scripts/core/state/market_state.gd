extends RefCounted

var node_id: String
var inventory: Dictionary = {}
var base_demand: Dictionary = {}
var industries: Dictionary = {}

func _init(p_node_id: String):
	node_id = p_node_id