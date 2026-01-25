extends RefCounted
class_name GalaxyGenerator

# Strategic Geography Generator
# Purpose: Create dense clusters separated by sparse chokepoints to ensure market inefficiencies.

var rng: RandomNumberGenerator

func _init(seed_val: int):
	rng = RandomNumberGenerator.new()
	rng.seed = seed_val

# Generates a simplified strategic graph for validation.
# Returns: { "nodes": int, "edges": int, "chokepoints": int }
func generate_topology(region_count: int) -> Dictionary:
	# Each region has 5-10 systems.
	var nodes = 0
	for i in range(region_count):
		nodes += rng.randi_range(5, 10)
	
	# Intra-region connections are dense (roughly 2.5 edges per node).
	# Inter-region connections are sparse (chokepoints).
	# A region graph of N regions has at most N-1 critical chokepoints if spanning.
	var chokepoints = region_count - 1
	
	var edges = int(nodes * 2.5) + chokepoints
	
	return {
		"nodes": nodes,
		"edges": edges,
		"chokepoints": chokepoints
	}