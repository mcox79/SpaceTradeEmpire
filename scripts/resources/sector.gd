extends Resource
class_name Sector

@export var id: String = "sector_id"
@export var name: String = "Sector Name"
@export var coordinates: Vector2 = Vector2.ZERO # For the 2D Map UI later
@export var connected_ids: Array = [] # List of Strings (IDs of neighbors)

func connect_to(other_id: String):
	if not connected_ids.has(other_id):
		connected_ids.append(other_id)
