extends RefCounted
class_name Sim

const Intents = preload("res://scripts/core/intents/intents.gd")
const Generator = preload("res://scripts/core/sim/galaxy_generator.gd")

var state: SimState
var rng: RngStreams
var galaxy_map: Dictionary

# AI Logistics
var active_fleets: Array = []

func _init(p_seed: int):
	state = SimState.new(p_seed)
	rng = RngStreams.new(p_seed)
	var gen = Generator.new(p_seed)
	galaxy_map = gen.generate(5)
	
	# Inject 5 Autonomous Test Fleets at random lanes
	for i in range(5):
		var lane = galaxy_map.lanes[rng.randi() % galaxy_map.lanes.size()]
		active_fleets.append({
			"id": "fleet_" + str(i),
			"from": lane.from,
			"to": lane.to,
			"progress": rng.randf(), # Start somewhere along the lane
			"speed": rng.randf_range(0.002, 0.008) # Percent per tick
		})

func advance(steps: int) -> Array:
	var events = []
	for step in range(steps):
		state.current_tick += 1
		
		# Advance Fleets along the logistics graph
		for fleet in active_fleets:
			fleet.progress += fleet.speed
			if fleet.progress >= 1.0:
				fleet.progress = 0.0 # Loop for testing
				# In a real game, this triggers the [ARRIVAL] intent.
		
	return events

func apply_intent(intent) -> Dictionary:
	return { "events": [], "error": null }