extends RefCounted

var node_heat: Dictionary = {}

func _init(galaxy_map: Dictionary):
	for star in galaxy_map.get("stars", []):
		node_heat[star.id] = 0.0

func add_heat(node_id: String, amount: float) -> void:
	if node_heat.has(node_id):
		node_heat[node_id] += amount

func get_node_heat(node_id: String) -> float:
	return node_heat.get(node_id, 0.0)