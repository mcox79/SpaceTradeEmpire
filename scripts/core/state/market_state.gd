extends RefCounted

# ARCHITECTURE: A pure data struct. Holds inventory for a single node.

var node_id: String
var inventory: Dictionary = {} # Key: good_id, Value: quantity
var base_demand: Dictionary = {} # Key: good_id, Value: baseline consumption rate

func _init(p_node_id: String):
	node_id = p_node_id
	# SKELETON: Injecting default commodities for the test run
	inventory["staples"] = 100
	inventory["minerals"] = 100
	base_demand["staples"] = 5  # Consumes 5 per tick
	base_demand["minerals"] = 2 # Consumes 2 per tick
