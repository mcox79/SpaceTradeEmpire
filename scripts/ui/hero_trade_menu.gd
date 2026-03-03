extends CanvasLayer

# GATE.S1.HERO_SHIP_LOOP.MARKET_SCREEN.001
# Hero trade menu: Panel+VBoxContainer with one row per market good.

var _market_node_id: String = ""
var _rows_container: VBoxContainer = null

func _ready():
	visible = false
	var panel = Panel.new()
	panel.custom_minimum_size = Vector2(400, 300)
	add_child(panel)
	var vbox = VBoxContainer.new()
	panel.add_child(vbox)
	_rows_container = VBoxContainer.new()
	vbox.add_child(_rows_container)
	var btn_undock = Button.new()
	btn_undock.text = "Undock"
	btn_undock.pressed.connect(_on_undock_pressed)
	vbox.add_child(btn_undock)

func open_market_v0(node_id: String) -> void:
	_market_node_id = node_id
	visible = true
	_rebuild_rows()

func close_market_v0() -> void:
	_market_node_id = ""
	visible = false

func get_market_view_v0() -> Array:
	if _market_node_id.is_empty():
		return []
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetPlayerMarketViewV0"):
		return bridge.call("GetPlayerMarketViewV0", _market_node_id)
	return []

func buy_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, true)

func sell_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, false)

func get_panel_row_count_v0() -> int:
	if _rows_container == null:
		return 0
	return _rows_container.get_child_count()

func _on_undock_pressed() -> void:
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("undock_v0"):
		gm.call("undock_v0")

func _rebuild_rows() -> void:
	if _rows_container == null:
		return
	for child in _rows_container.get_children():
		_rows_container.remove_child(child)
		child.queue_free()
	var view = get_market_view_v0()
	for entry in view:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var row = HBoxContainer.new()
		var lbl_id = Label.new()
		lbl_id.text = str(entry.get("good_id", ""))
		var lbl_buy = Label.new()
		lbl_buy.text = "Buy:%d" % int(entry.get("buy_price", 0))
		var lbl_sell = Label.new()
		lbl_sell.text = "Sell:%d" % int(entry.get("sell_price", 0))
		var btn_buy = Button.new()
		btn_buy.text = "Buy"
		var btn_sell = Button.new()
		btn_sell.text = "Sell"
		row.add_child(lbl_id)
		row.add_child(lbl_buy)
		row.add_child(lbl_sell)
		row.add_child(btn_buy)
		row.add_child(btn_sell)
		_rows_container.add_child(row)
