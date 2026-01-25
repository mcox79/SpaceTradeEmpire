extends RefCounted
class_name Sim

# The Orchestrator. The only system with write-access to SimState.

var state: SimState
var rng: RngStreams

func _init(p_seed: int):
print("[SIM] Bootstrapping Headless Sim with seed: ", p_seed)
state = SimState.new(p_seed)
rng = RngStreams.new(p_seed)

# Advances the simulation by strictly defined steps.
func advance(steps: int) -> Array:
var generated_events = []
for i in range(steps):
state.current_tick += 1
# TICK ORDER (Locked)
# 1. Resolve arrivals
# 2. Resolve encounters
# 3. Markets update
return generated_events

func apply_intent(_intent) -> Dictionary:
return { "events": [], "error": "NotImplemented" }