extends RefCounted
class_name Sim

const Intents = preload("res://scripts/core/intents/intents.gd")

var state: SimState
var rng: RngStreams

func _init(p_seed: int):
	print("[SIM] Bootstrapping Headless Sim with seed: ", p_seed)
	state = SimState.new(p_seed)
	rng = RngStreams.new(p_seed)

func advance(steps: int) -> Array:
	var generated_events = []
	for i in range(steps):
		state.current_tick += 1
	return generated_events

# The only entry point for external actions.
func apply_intent(intent: Intents.Intent) -> Dictionary:
	# Validation (Mocked for Slice 2)
	if intent.actor_id == "":
		return { "events": [], "error": "MissingActorId" }

	# Routing
	if intent is Intents.Dock:
		print("[SIM] Processed Dock Intent for: ", intent.actor_id)
	elif intent is Intents.Trade:
		var action = "BUY" if intent.is_buy else "SELL"
		print("[SIM] Processed Trade Intent: %s %s x%s" % [action, intent.item_id, intent.amount])
	
	return { "events": ["intent_processed"], "error": null }