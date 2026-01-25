extends RefCounted

# ARCHITECTURE: The canonical ledger for all sensor data, heat, and notoriety.
# DATA PURITY RULE: Pure dictionary, no Godot Nodes.

var node_heat: Dictionary = {} # Key: node_id (String), Value: heat_level (float)
var faction_heat: Dictionary = {} # Key: faction_id (String), Value: notoriety (float)

func _init(galaxy_map: Dictionary):
	# Initialize ledger at 0 for all nodes
	for star in galaxy_map.stars:
		node_heat[star.id] = 0.0

func add_heat(node_id: String, amount: float):
	if node_heat.has(node_id):
		node_heat[node_id] += amount
