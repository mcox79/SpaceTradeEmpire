extends RefCounted

# Handles the atomic transfer of goods between Fleets and Markets.

static func handle_arrival(fleet, market, tick: int) -> void:
	if fleet.active_order_ref == null: return

	var order = fleet.active_order_ref

	# CASE 1: ARRIVED AT SOURCE (PICKUP)
	if market.node_id == order.pickup_id and not fleet.cargo_hold.has(order.item_id):
		_execute_pickup(fleet, market, order)

	# CASE 2: ARRIVED AT DESTINATION (DELIVERY)
	elif market.node_id == order.destination_id and fleet.cargo_hold.get(order.item_id, 0) > 0:
		_execute_delivery(fleet, market, order)

static func _execute_pickup(fleet, market, order) -> void:
	var available = market.inventory.get(order.item_id, 0)
	var to_take = min(available, order.quantity)

	if to_take > 0:
		# Transfer Atoms: Market -> Fleet
		market.inventory[order.item_id] = available - to_take
		fleet.cargo_hold[order.item_id] = to_take
		# print('LOGISTICS: Fleet ', fleet.id, ' picked up ', to_take, ' ', order.item_id, ' at ', market.node_id)

static func _execute_delivery(fleet, market, order) -> void:
	var amount = fleet.cargo_hold.get(order.item_id, 0)

	if amount > 0:
		# Transfer Atoms: Fleet -> Market
		var current_stock = market.inventory.get(order.item_id, 0)
		market.inventory[order.item_id] = current_stock + amount
		fleet.cargo_hold.erase(order.item_id)

		# Close Contract
		fleet.active_order_ref = null
		# print('LOGISTICS: Fleet ', fleet.id, ' delivered ', amount, ' ', order.item_id, ' to ', market.node_id)