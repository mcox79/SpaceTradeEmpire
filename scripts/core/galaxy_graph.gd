extends RefCounted
class_name GalaxyGraph

# The Master Ledger of all stars
var sectors: Dictionary = {} # { "id": Sector }

func add_sector(sec: Resource):
	sectors[sec.id] = sec

func connect_sectors(id_a: String, id_b: String):
	if sectors.has(id_a) and sectors.has(id_b):
		sectors[id_a].connect_to(id_b)
		sectors[id_b].connect_to(id_a) # Bidirectional Jump Gate

# BFS Pathfinding: Returns list of IDs [start, ... , end]
func get_route(start_id: String, end_id: String) -> Array:
	if start_id == end_id: return [start_id]
	
	var queue = []
	var visited = { start_id: true }
	var parents = {} # To reconstruct path
	
	queue.append(start_id)
	
	while queue.size() > 0:
		var current = queue.pop_front()
		
		if current == end_id:
			return _reconstruct_path(parents, start_id, end_id)
			
		var current_sector = sectors.get(current)
		if current_sector:
			for neighbor in current_sector.connected_ids:
				if not visited.has(neighbor):
					visited[neighbor] = true
					parents[neighbor] = current
					queue.append(neighbor)
					
	return [] # No path found

func _reconstruct_path(parents: Dictionary, start: String, end: String) -> Array:
	var path = []
	var curr = end
	while curr != start:
		path.push_front(curr)
		curr = parents[curr]
	path.push_front(start)
	return path
