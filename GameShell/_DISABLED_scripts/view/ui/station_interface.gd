extends Control

var manager_ref
var current_node_id: String

var panel: Panel
var lbl_wallet: Label
var lbl_heat: Label
var container: VBoxContainer

func _ready():
	anchor_right = 1.0
	anchor_bottom = 1.0
	visible = false

	panel = Panel.new()
	panel.anchors_preset = Control.PRESET_CENTER
	panel.custom_minimum_size = Vector2(800, 500) # WIDENED (Prev: 500)
	panel.position = Vector2(200, 50)
	add_child(panel)

	var title = Label.new()
	title.text = 'STATION MARKET TERMINAL'
	title.position = Vector2(10, 10)
	panel.add_child(title)

	lbl_wallet = Label.new()
	lbl_wallet.position = Vector2(10, 40)
	panel.add_child(lbl_wallet)

	lbl_heat = Label.new()
	lbl_heat.position = Vector2(400, 40)
	panel.add_child(lbl_heat)

	container = VBoxContainer.new()
	container.position = Vector2(10, 80)
	container.custom_minimum_size = Vector2(780, 400) # WIDENED
	panel.add_child(container)

func setup(p_manager, p_node_id: String):
	manager_ref = p_manager
	current_node_id = p_node_id

func _process(_delta):
	if visible and manager_ref:
		_refresh_header()

func _refresh_header():
	lbl_wallet.text = 'CREDITS: ' + str(manager_ref.player.credits)
	var heat = manager_ref.sim_ref.info.get_node_heat(current_node_id)
	lbl_heat.text = 'LOCAL HEAT: ' + str(snapped(heat, 0.01))

func refresh_market_list():
	for child in container.get_children():
		child.queue_free()

	var market = manager_ref.sim_ref.active_markets.get(current_node_id)
	if not market: return

	for item_id in market.inventory.keys():
		var qty = market.inventory[item_id]
		var demand = market.base_demand.get(item_id, 10)
		var price = manager_ref.EconomyEngine.calculate_price(null, qty, demand)
		var player_qty = manager_ref.player.cargo.get(item_id, 0)

		var row = HBoxContainer.new()
		container.add_child(row)

		var info = Label.new()
		# FORMAT: ALIGN LEFT, WIDENED TO PREVENT OVERLAP
		info.text = '%s | QTY: %s | PRICE: %s | OWN: %s' % [item_id.to_upper(), qty, price, player_qty]
		info.custom_minimum_size = Vector2(500, 30) # WIDENED (Prev: 300)
		row.add_child(info)

		var btn_buy = Button.new()
		btn_buy.text = 'BUY'
		btn_buy.pressed.connect(_on_trade.bind(item_id, 1, true))
		row.add_child(btn_buy)

		var btn_sell = Button.new()
		btn_sell.text = 'SELL'
		btn_sell.pressed.connect(_on_trade.bind(item_id, 1, false))
		if player_qty <= 0: btn_sell.disabled = true
		row.add_child(btn_sell)

func _on_trade(item_id, qty, is_buy):
	var success = manager_ref.try_trade(current_node_id, item_id, qty, is_buy)
	if success:
		refresh_market_list()
	else:
		print('Trade Failed')