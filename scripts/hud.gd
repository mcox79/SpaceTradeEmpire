extends CanvasLayer

@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var health_bar = $Control/Panel/VBoxContainer/HealthBar
@onready var fuel_bar = $Control/Panel/VBoxContainer/FuelBar
@onready var cargo_label = $Control/Panel/VBoxContainer/CargoLabel

@onready var shop_panel = $Control/ShopPanel
@onready var market_ticker = $Control/ShopPanel/VBoxContainer/MarketTicker
@onready var btn_sell = $Control/ShopPanel/VBoxContainer/BtnSellAll
@onready var btn_speed = $Control/ShopPanel/VBoxContainer/BtnSpeed
@onready var btn_weapon = $Control/ShopPanel/VBoxContainer/BtnWeapon
@onready var btn_close = $Control/ShopPanel/VBoxContainer/BtnClose

# Optional Phase 1 buttons (safe if missing)
@onready var btn_buy_iron = $Control/ShopPanel/VBoxContainer/BtnBuyIron if has_node("Control/ShopPanel/VBoxContainer/BtnBuyIron") else null
@onready var btn_buy_gold = $Control/ShopPanel/VBoxContainer/BtnBuyGold if has_node("Control/ShopPanel/VBoxContainer/BtnBuyGold") else null

var player_ref = null
var current_station = null

func _ready():
	shop_panel.visible = false
	btn_sell.pressed.connect(_on_sell_all)
	btn_speed.pressed.connect(_on_buy_speed)
	btn_weapon.pressed.connect(_on_buy_weapon)
	btn_close.pressed.connect(_on_close_shop)

	if btn_buy_iron:
		btn_buy_iron.pressed.connect(_on_buy_iron)
	if btn_buy_gold:
		btn_buy_gold.pressed.connect(_on_buy_gold)

	_find_player()

func _find_player():
	var player = get_tree().get_first_node_in_group("Player")
	if player:
		player_ref = player
		player.credits_updated.connect(_on_credits_updated)
		player.cargo_updated.connect(_on_cargo_updated)
		player.health_updated.connect(_on_health_updated)
		player.fuel_updated.connect(_on_fuel_updated)
		player.shop_toggled.connect(_on_shop_toggled)

		_on_credits_updated(player.credits)
		_on_cargo_updated(player.cargo)
		_on_health_updated(player.health, player.max_health)
		_on_fuel_updated(player.fuel, player.max_fuel)
	else:
		await get_tree().create_timer(0.1).timeout
		_find_player()

func _on_credits_updated(amount):
	money_label.text = "CREDITS: $" + str(amount)

func _on_health_updated(current, max_val):
	if health_bar:
		health_bar.max_value = max_val
		health_bar.value = current

func _on_fuel_updated(current, max_val):
	if fuel_bar:
		fuel_bar.max_value = max_val
		fuel_bar.value = current

func _on_cargo_updated(manifest):
	var text = "CARGO: "
	if manifest.is_empty():
		text += "Empty"
	else:
		for item in manifest:
			text += "%s(%s) " % [item.replace("ore_", "").capitalize(), manifest[item]]
	cargo_label.text = text

func _on_shop_toggled(is_open, station_ref):
	shop_panel.visible = is_open
	current_station = station_ref
	if is_open:
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		_update_ticker()
	else:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _update_ticker():
	if not current_station:
		return
	var iron_ask = current_station.get_ask_price("ore_iron")
	var iron_bid = current_station.get_bid_price("ore_iron")
	var gold_ask = current_station.get_ask_price("ore_gold")
	var gold_bid = current_station.get_bid_price("ore_gold")
	market_ticker.text = "IRON: BUY $" + str(iron_ask) + " | SELL $" + str(iron_bid) + "   GOLD: BUY $" + str(gold_ask) + " | SELL $" + str(gold_bid)

func _on_sell_all():
	if not player_ref or not current_station:
		return
	var manifest = player_ref.cargo.duplicate()
	for item in manifest:
		current_station.sell_cargo(player_ref, item, int(manifest[item]))
	_update_ticker()

func _on_buy_iron():
	if not player_ref or not current_station:
		return
	current_station.buy_cargo(player_ref, "ore_iron", 1)
	_update_ticker()

func _on_buy_gold():
	if not player_ref or not current_station:
		return
	current_station.buy_cargo(player_ref, "ore_gold", 1)
	_update_ticker()

func _on_buy_speed():
	if player_ref:
		player_ref.purchase_upgrade("speed", 200)

func _on_buy_weapon():
	if player_ref:
		player_ref.purchase_upgrade("weapon", 500)

func _on_close_shop():
	if player_ref:
		player_ref.undock()



func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		Input.mouse_mode = Input.MOUSE_MODE_VISIBLE