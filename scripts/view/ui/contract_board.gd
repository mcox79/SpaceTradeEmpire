extends Control

var manager_ref
var container: VBoxContainer

func setup(p_manager):
	manager_ref = p_manager

func _ready():
	visible = false
	var panel = Panel.new()
	panel.custom_minimum_size = Vector2(600, 400)
	panel.position = Vector2(200, 100)
	add_child(panel)

	var title = Label.new()
	title.text = 'AVAILABLE LOGISTICS CONTRACTS (PRESS C)'
	title.position = Vector2(10, 10)
	panel.add_child(title)

	container = VBoxContainer.new()
	container.position = Vector2(10, 50)
	container.custom_minimum_size = Vector2(580, 340)
	panel.add_child(container)

func toggle():
	visible = not visible
	if visible:
		refresh()

func refresh():
	if not manager_ref or not manager_ref.sim: return

	for c in container.get_children(): c.queue_free()

	var orders = manager_ref.sim.active_orders
	if orders.is_empty():
		var l = Label.new()
		l.text = 'No contracts available.'
		container.add_child(l)
		return

	for i in range(orders.size()):
		var o = orders[i]
		var row = HBoxContainer.new()

		var info = Label.new()
		info.text = '%s %s -> %s [Qty: %s]' % [o.item_id, o.pickup_id, o.destination_id, o.quantity]
		info.custom_minimum_size = Vector2(450, 30)
		row.add_child(info)

		var btn = Button.new()
		btn.text = 'ACCEPT'
		btn.pressed.connect(_on_accept.bind(o.id))
		row.add_child(btn)

		container.add_child(row)

func _on_accept(order_id):
	# Player accepting a contract
	var success = manager_ref.sim.player_accept_contract('player_1', order_id)
	if success:
		print('UI: Contract accepted.')
		visible = false
	else:
		print('UI: Failed to accept contract.')