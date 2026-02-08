extends CanvasLayer

const Intents = preload("res://scripts/core/intents/intents.gd")

@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var shop_panel = $Control/ShopPanel
@onready var market_container = $Control/ShopPanel/VBoxContainer

var player_ref = null
var current_station_id: String = ""
var _dynamic_elements: Array = [] 

func _ready():
        # If the new C# StationMenu exists, this legacy HUD shop must not run.
        # This prevents duplicate menus.
        var station_menu = get_tree().root.find_child("StationMenu", true, false)
        if station_menu != null:
                push_warning("[HUD] StationMenu found. Disabling legacy hud.gd ShopPanel UI.")
                queue_free()
                return

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

func _on_shop_toggled(is_open: bool, station_ref):
        print("[HUD SHOP] toggled=", is_open, " station_ref=", station_ref, " name=", station_ref.name if station_ref else "null")

        # Disable legacy shop UI (we use StationMenu + SimBridge now)
        shop_panel.visible = false
        return

        shop_panel.visible = is_open
        if is_open and station_ref:
                current_station_id = station_ref.name
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
	_clear_shop_ui()
	
	# STRICT LOOKUP: Fetching via Tree to satisfy static analyzer
	var game_manager = get_node_or_null("/root/GameManager")
	if not game_manager or not game_manager.sim: return
	
	var sim = game_manager.sim
	if not sim.active_markets.has(current_station_id): return
	
	var market = sim.active_markets[current_station_id]
	
	var btn_close = Button.new()
	btn_close.text = "UNDOCK"
	btn_close.pressed.connect(func(): if player_ref: player_ref.undock())
	market_container.add_child(btn_close)
	_dynamic_elements.append(btn_close)
	
	for item_id in market.inventory.keys():
		var available_qty = market.inventory[item_id]
		var base_price = 50 if item_id == "staples" else 100 
		
		var btn_buy = Button.new()
		btn_buy.text = "BUY 1 %s (Cost: $%s) [Avail: %s]" % [item_id.to_upper(), base_price, available_qty]
		btn_buy.pressed.connect(_request_trade.bind(item_id, 1, true)) 
		
		market_container.add_child(btn_buy)
		_dynamic_elements.append(btn_buy)

func _request_trade(item_id: String, amount: int, is_buy: bool):
	var game_manager = get_node_or_null("/root/GameManager")
	if not game_manager or not game_manager.sim: return
	
	var intent = Intents.Trade.new()
	intent.actor_id = "player_1"
	intent.target_id = current_station_id
	intent.item_id = item_id
	intent.amount = amount
	intent.is_buy = is_buy
	
	# Send to Headless Sim
	var result = game_manager.sim.apply_intent(intent)
	
	if result.error == null:
		_build_shop_ui() 
