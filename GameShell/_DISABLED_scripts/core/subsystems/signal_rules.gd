extends RefCounted

const DECAY_RATE = 0.05

static func process_decay(info_state, _tick_count: int) -> void:
	for node_id in info_state.node_heat.keys():
		var current = info_state.node_heat[node_id]
		info_state.node_heat[node_id] = max(0.0, current * (1.0 - DECAY_RATE))