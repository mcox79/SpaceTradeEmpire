const Intents = preload("res://scripts/core/intents/intents.gd")
const Sim = preload("res://scripts/core/sim/sim.gd")

var rng = RandomNumberGenerator.new()
var galaxy_map 

func apply_intent(intent) -> Dictionary:
	return {}

func _process(delta):
	# Safety gate to prevent division by zero
	if galaxy_map and galaxy_map.lanes.size() > 0:
		var lane = galaxy_map.lanes[rng.randi() % galaxy_map.lanes.size()]
