extends RefCounted

# ARCHITECTURE: Pure functions that compute economic outcomes.

const MIN_PRICE = 1

static func calculate_price(supply: int, demand: int, base_value: int) -> int:
	# ALGORITHM: Simple Scarcity (Price = Value * (Demand / Supply))
	var safe_supply = max(1, supply)
	var scarcity_factor = float(demand) / float(safe_supply)
	# Apply a standardizing buffer so prices don't collapse instantly
	var final_price = int(base_value * (1.0 + scarcity_factor)) 
	return max(MIN_PRICE, final_price)

static func consume_inventory(market_state, tick_count: int):
	# Reduce local supply based on the node's base_demand
	for good_id in market_state.base_demand.keys():
		var burn_rate = market_state.base_demand[good_id]
		var current_supply = market_state.inventory.get(good_id, 0)
		market_state.inventory[good_id] = max(0, current_supply - burn_rate)
