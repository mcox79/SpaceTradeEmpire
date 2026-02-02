# Market UI Controller
# Purpose: Listen for Player docking events and display SimBridge market data.
# Layer: GameShell (View)

extends Control
class_name MarketUI

# Dependencies
var _bridge = null
var _current_station = null

# UI Elements (Assumed to exist in Scene)
@export var container: Control
@export var title_label: Label
@export var price_list: VBoxContainer

func _ready():
	# Locate Bridge
	_bridge = get_tree().root.find_child("SimBridge", true, false)
	
	# Connect to Player (dynamically finding player in group)
	var players = get_tree().get_nodes_in_group("Player")
	if players.size() > 0:
		var p = players[0]
		if p.has_signal("shop_toggled"):
			p.connect("shop_toggled", _on_shop_toggled)
	
	if container:
		container.visible = false

func _on_shop_toggled(is_open: bool, station_ref):
	if container:
		container.visible = is_open
	
	if is_open and station_ref:
		_current_station = station_ref
		_refresh_prices()
	else:
		_current_station = null

func _refresh_prices():
	if _bridge == null or _current_station == null: return
	if not _current_station.has_method("get_ask_price"): return
	
	# Clear old list
	for child in price_list.get_children():
		child.queue_free()
	
	# Populate (hardcoded list for prototype, later use MarketProfile)
	# We query the STATION, which queries the BRIDGE
	var goods = ["fuel", "ore", "metal"]
	
	for good in goods:
		var price = _current_station.get_ask_price(good)
		if price > 0:
			var label = Label.new()
			label.text = "%s: %d Cr" % [good.capitalize(), price]
			price_list.add_child(label)
			
			# Simple Buy Button
			var btn = Button.new()
			btn.text = "Buy 10"
			btn.connect("pressed", func(): _on_buy_pressed(good, 10))
			price_list.add_child(btn)

func _on_buy_pressed(item_id: String, amount: int):
	if _current_station and _current_station.has_method("buy_cargo"):
		# Station handles the transaction via Bridge
		var players = get_tree().get_nodes_in_group("Player")
		if players.size() > 0:
			var success = _current_station.buy_cargo(players[0], item_id, amount)
			if success:
				print("Trade Successful!")
				_refresh_prices() # Refresh to show stock changes if implemented
			else:
				print("Trade Failed.")