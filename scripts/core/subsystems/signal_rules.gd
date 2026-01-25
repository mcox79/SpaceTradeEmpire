extends RefCounted

# CONFIG: The systemic friction cost. Heat decays by 5% per economic tick.
const DECAY_RATE = 0.05 

static func process_decay(info_state, _tick_count: int):
	for node_id in info_state.node_heat.keys():
		var current_heat = info_state.node_heat[node_id]
		# Apply depreciation to the thermal ledger
		info_state.node_heat[node_id] = max(0.0, current_heat * (1.0 - DECAY_RATE))

# Returns the localized risk modifier based on traffic density
static func get_interdiction_risk(info_state, node_id: String) -> float:
	return info_state.node_heat.get(node_id, 0.0)
