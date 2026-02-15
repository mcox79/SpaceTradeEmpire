extends RefCounted
class_name SimState

# The canonical ledger. Total decoupling from the Godot Node tree.

# Stable world seed. Must not be mutated after initialization.
var initial_seed: int
var current_tick: int = 0

func _init(p_seed: int = 0):
	initial_seed = p_seed
	current_tick = 0

func get_seed() -> int:
	return initial_seed

func get_snapshot_hash() -> String:
	var state_string = "seed:%s|tick:%s" % [initial_seed, current_tick]
	return state_string.sha256_text()
