extends Control

var manager_ref
var container: VBoxContainer

func setup(p_manager):
	manager_ref = p_manager

func _ready():
	visible = false
	var panel = Panel.new()
	panel.custom_minimum_size = Vector2(700, 500)
	panel.position = Vector2(150, 50) # Offset to center better
	add_child(panel)

	# HEADER ROW
	var header = HBoxContainer.new()
	header.position = Vector2(10, 10)
	header.custom_minimum_size = Vector2(680, 30)
	panel.add_child(header)

	var title = Label.new()
	title.text = 'AVAILABLE LOGISTICS CONTRACTS'
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(title)

	var close_btn = Button.new()
	close_btn.text = ' [ X ] CLOSE '
	close_btn.pressed.connect(toggle)
	header.add_child(close_btn)

	# LIST CONTAINER
	container = VBoxContainer.new()
	container.position = Vector2(10, 50)
	container.custom_minimum_size = Vector2(680, 440)
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
		l.text = 'No contracts available at this time.'
		container.add_child(l)
		return

	for i in range(orders.size()):
		var o = orders[i]
		var row = HBoxContainer.new()

		var info = Label.new()
		# CLEANER TEXT FORMAT
		info.text = ' %s ' % o.item_id.to_upper()
		info.custom_minimum_size = Vector2(100, 30)
		row.add_child(info)

		var route = Label.new()
		route.text = '%s -> %s ' % [o.pickup_id, o.destination_id]
		route.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(route)

		var btn = Button.new()
		btn.text = ' ACCEPT CONTRACT '
		btn.pressed.connect(_on_accept.bind(o.id))
		row.add_child(btn)

		container.add_child(row)

func _on_accept(order_id):
	var success = manager_ref.sim.player_accept_contract('player_1', order_id)
	if success:
		print('UI: Contract accepted.')
		visible = false # Auto-close on accept