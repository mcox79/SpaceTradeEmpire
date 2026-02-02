extends Control
class_name MarketUI

# These @export lines tell Godot to show them in the Inspector
@export var container: Control
@export var price_list: VBoxContainer

var _bridge = null
var _station = null

func _ready():
	# Locate the SimBridge (Economy System)
	_bridge = get_tree().root.find_child("SimBridge", true, false)
	
	# Listen for the Player's docking signal
	var players = get_tree().get_nodes_in_group("Player")
	if players.size() > 0:
		var p = players[0]
		if p.has_signal("shop_toggled"):
			p.connect("shop_toggled", _on_shop)
	
	# Hide panel at start
	if container: 
		container.visible = false

# Signal Receiver: Opens/Closes the UI
func _on_shop(is_open, station):
	if container: 
		container.visible = is_open
	
	_station = station
	
	if is_open:
		_refresh_prices()

# Refreshes the list of goods from the Station
func _refresh_prices():
	if not _station or not price_list: return
	
	# Clear old items
	for child in price_list.get_children():
		child.queue_free()
	
	# Add new items (Fuel, Ore, Metal)
	var goods = ["fuel", "ore", "metal"]
	for good in goods:
		# Ask station for price (Station asks Bridge)
		if _station.has_method("get_ask_price"):
			var price = _station.get_ask_price(good)
			
			var label = Label.new()
			label.text = "%s: %d Cr" % [good.capitalize(), price]
			label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			price_list.add_child(label)
