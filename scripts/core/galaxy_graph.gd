extends RefCounted
class_name GalaxyGraph

# ARCHITECTURE: Pure POD Adjacency List. No Godot Resources.
var _adj: Dictionary = {} # Key: String (Node ID), Value: Array[String] (Connected IDs)

func add_node(id: String):
	if not _adj.has(id):
		_adj[id] = []

func connect_nodes(id_a: String, id_b: String):
	if _adj.has(id_a) and _adj.has(id_b):
		if not _adj[id_a].has(id_b): _adj[id_a].append(id_b)
		if not _adj[id_b].has(id_a): _adj[id_b].append(id_a)

# BFS Pathfinding: Returns list of IDs [start, ... , end]
func get_route(start_id: String, end_id: String) -> Array:
	if start_id == end_id: return [start_id]
	
	var queue = [start_id]
	var visited = { start_id: true }
	var parents = {}
	
	while queue.size() > 0:
		var current = queue.pop_front()
		if current == end_id:
			return _reconstruct_path(parents, start_id, end_id)
		
		if _adj.has(current):
			for neighbor in _adj[current]:
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
