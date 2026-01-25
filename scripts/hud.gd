extends CanvasLayer

const Intents = preload("res://scripts/core/intents/intents.gd")

@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var shop_panel = $Control/ShopPanel
@onready var market_container = $Control/ShopPanel/VBoxContainer # The dynamic parent

var player_ref = null
var current_station_id: String = ""

# Track dynamically generated buttons so we can clear them later
var _dynamic_elements: Array = [] 

func _ready():
	shop_panel.visible = false
	_find_player()

func _find_player():
	var player = get_tree().get_first_node_in_group("Player")
	if player:
		player_ref = player
		player.credits_updated.connect(_on_credits_updated)
		player.shop_toggled.connect(_on_shop_toggled)
	else:
		await get_tree().create_timer(0.1).timeout
		_find_player()

func _on_credits_updated(amount):
	money_label.text = "CREDITS: $" + str(amount)

# --- THE DATA-DRIVEN UI LOOP ---
func _on_shop_toggled(is_open: bool, station_ref):
	shop_panel.visible = is_open
	if is_open and station_ref:
		current_station_id = station_ref.name # Using node name as ID for this slice
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		_build_shop_ui()
	else:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
		_clear_shop_ui()

func _clear_shop_ui():
	for element in _dynamic_elements:
		if is_instance_valid(element):
				element.queue_free()
	_dynamic_elements.clear()

func _build_shop_ui():
	_clear_shop_ui() # Sanity clear
	
	var sim = GameManager.sim
	if not sim.active_markets.has(current_station_id): return
	
	var market = sim.active_markets[current_station_id]
	
	# 1. Create the Close Button
	var btn_close = Button.new()
	btn_close.text = "UNDOCK"
	btn_close.pressed.connect(func(): if player_ref: player_ref.undock())
	market_container.add_child(btn_close)
	_dynamic_elements.append(btn_close)
	
	# 2. Loop the Backend Data and Generate Buttons
	for item_id in market.inventory.keys():
		var available_qty = market.inventory[item_id]
		
		# For Slice 7, price is static. In Slice 8, we pull this from the Rules engine.
		var base_price = 50 if item_id == "staples" else 100 
		
		var btn_buy = Button.new()
		btn_buy.text = "BUY 1 %s (Cost: $%s) [Avail: %s]" % [item_id.to_upper(), base_price, available_qty]
		
		# The Unified Router: Binding the specific item_id to the generic function
		btn_buy.pressed.connect(_request_trade.bind(item_id, 1, true)) 
		
		market_container.add_child(btn_buy)
		_dynamic_elements.append(btn_buy)

# --- THE UNIFIED INTENT ROUTER ---
func _request_trade(item_id: String, amount: int, is_buy: bool):
	var intent = Intents.Trade.new()
	intent.actor_id = "player_1"
	intent.target_id = current_station_id
	intent.item_id = item_id
	intent.amount = amount
	intent.is_buy = is_buy
	
	# Send to Headless Sim
	var result = GameManager.sim.apply_intent(intent)
	
	# If the Sim accepted it, rebuild the UI to show new inventory counts
	if result.error == null:
		_build_shop_ui() 
