extends RefCounted
class_name Sim

const Intents = preload("res://scripts/core/intents/intents.gd")
const Generator = preload("res://scripts/core/sim/galaxy_generator.gd")

var state: SimState
var rng: RngStreams
var galaxy_map: Dictionary # Caches the generated 3D topology

var in_transit_ships: Array = [] 

func _init(p_seed: int):
	print("[SIM] Bootstrapping Headless Sim with seed: ", p_seed)
	state = SimState.new(p_seed)
	rng = RngStreams.new(p_seed)
	
	# Generate the canonical 3D Map
	var gen = Generator.new(p_seed)
	galaxy_map = gen.generate(5)
	print("[SIM] Generated ", galaxy_map.stars.size(), " stars across 5 regions.")

func advance(steps: int) -> Array:
	var generated_events = []
	for i in range(steps):
		state.current_tick += 1
	return generated_events

func apply_intent(intent: Intents.Intent) -> Dictionary:
	return { "events": ["intent_processed"], "error": null }