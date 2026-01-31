extends RefCounted

static func consume_inventory(market_state, _tick_count: int) -> void:
	for good_id in market_state.base_demand.keys():
		var burn = market_state.base_demand[good_id]
		var current = market_state.inventory.get(good_id, 0)
		market_state.inventory[good_id] = max(0, current - burn)