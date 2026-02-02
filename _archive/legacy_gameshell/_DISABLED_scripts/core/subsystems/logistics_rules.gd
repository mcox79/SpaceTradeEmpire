extends RefCounted

# Returns the credit value of the completed transaction (or 0)
static func handle_arrival(fleet, market, tick: int) -> int:
	if fleet.active_order_ref == null: return 0

	var order = fleet.active_order_ref

	# CASE 1: PICKUP
	if market.node_id == order.pickup_id and not fleet.cargo_hold.has(order.item_id):
		_execute_pickup(fleet, market, order)
		return 0

	# CASE 2: DELIVERY
	elif market.node_id == order.destination_id and fleet.cargo_hold.get(order.item_id, 0) > 0:
		return _execute_delivery(fleet, market, order)

	return 0

static func _execute_pickup(fleet, market, order) -> void:
	var available = market.inventory.get(order.item_id, 0)
	var to_take = min(available, order.quantity)

	if to_take > 0:
		market.inventory[order.item_id] = available - to_take
		fleet.cargo_hold[order.item_id] = to_take

static func _execute_delivery(fleet, market, order) -> int:
	var amount = fleet.cargo_hold.get(order.item_id, 0)

	if amount > 0:
		# Transfer Goods
		var current_stock = market.inventory.get(order.item_id, 0)
		market.inventory[order.item_id] = current_stock + amount
		fleet.cargo_hold.erase(order.item_id)

		# Payout
		var payout = order.reward
		fleet.active_order_ref = null
		# print('LOGISTICS: Delivery complete. Payout: $', payout)
		return payout

	return 0