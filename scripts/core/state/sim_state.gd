extends RefCounted
class_name SimState

# The canonical ledger. Total decoupling from the Godot Node tree.

# Stable world seed. Must not be mutated after initialization.
# Enforced: any attempt to set after init will push_error.
var _seed_locked: bool = false
var seed: int:
	set(value):
		if _seed_locked:
			push_error("SimState.seed is immutable after initialization")
			return
		seed = value
	get:
		return seed

# Back-compat alias (read-only mirror of seed).
var initial_seed: int

var current_tick: int = 0

func _init(p_seed: int = 0):
	_seed_locked = false
	seed = p_seed
	initial_seed = p_seed
	_seed_locked = true
	current_tick = 0

func get_seed() -> int:
	return seed

func get_snapshot_hash() -> String:
	var state_string = "seed:%s|tick:%s" % [seed, current_tick]
	return state_string.sha256_text()


