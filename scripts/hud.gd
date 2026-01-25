extends CanvasLayer

const Intents = preload("res://scripts/core/intents/intents.gd")

@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var shop_panel = $Control/ShopPanel
@onready var market_ticker = $Control/ShopPanel/VBoxContainer/MarketTicker
@onready var btn_close = $Control/ShopPanel/VBoxContainer/BtnClose
@onready var btn_buy_iron = $Control/ShopPanel/VBoxContainer/BtnBuyIron
@onready var btn_buy_gold = $Control/ShopPanel/VBoxContainer/BtnBuyGold

var player_ref = null
var current_station = null

func _ready():
	shop_panel.visible = false
	btn_close.pressed.connect(_on_close_shop)
	btn_buy_iron.pressed.connect(_on_buy_iron)
	btn_buy_gold.pressed.connect(_on_buy_gold)
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

func _on_shop_toggled(is_open, station_ref):
	shop_panel.visible = is_open
	current_station = station_ref
	if is_open:
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		_update_ticker()
	else:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _update_ticker():
	if not current_station: return
	market_ticker.text = "IRON: $100 | GOLD: $250" # Hardcoded for Shim test

# --- THE SHIM: SENDING INTENTS INSTEAD OF DIRECT CALLS ---

func _on_buy_iron():
	_send_trade_intent("ore_iron", 1, true)

func _on_buy_gold():
	_send_trade_intent("ore_gold", 1, true)

func _send_trade_intent(item: String, amount: int, is_buy: bool):
	var intent = Intents.Trade.new()
	intent.actor_id = "player_1"
	intent.target_id = current_station.name if current_station else "unknown"
	intent.item_id = item
	intent.amount = amount
	intent.is_buy = is_buy
	
	# Send to the new Headless Sim
	GameManager.send_intent(intent)

func _on_close_shop():
	if player_ref: player_ref.undock()