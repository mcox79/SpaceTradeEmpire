extends RefCounted
# class_name removed

const EconomyEngine = preload("res://scripts/core/economy_engine.gd")

var player
var sim_ref

func _init(p_player, p_sim):
	player = p_player
	sim_ref = p_sim

func try_trade(node_id: String, item_id: String, qty: int, is_buy: bool) -> bool:
	var market = sim_ref.active_markets.get(node_id)
	if not market: return false
	var supply = market.inventory.get(item_id, 0)
	var demand = market.base_demand.get(item_id, 10)
	var unit_price = EconomyEngine.calculate_price(null, supply, demand)
	var total_cost = unit_price * qty

	if is_buy:
		if not EconomyEngine.can_afford(player.credits, unit_price, qty): return false
		if not EconomyEngine.has_cargo_space(0, player.cargo_capacity, 1.0, qty): return false
		if supply < qty: return false
		player.credits -= total_cost
		player.cargo[item_id] = player.cargo.get(item_id, 0) + qty
		market.inventory[item_id] -= qty
		sim_ref.info.add_heat(node_id, float(qty) * 0.1)
		return true

	else:
		if player.cargo.get(item_id, 0) < qty: return false
		player.credits += total_cost
		player.cargo[item_id] -= qty
		market.inventory[item_id] = supply + qty
		sim_ref.info.add_heat(node_id, float(qty) * 0.05)
		return true