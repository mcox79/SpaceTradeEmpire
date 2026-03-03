extends CanvasLayer

# GATE.S1.HERO_SHIP_LOOP.MARKET_SCREEN.001
# Hero trade menu: centered panel with station name, goods rows, cargo summary, undock button.

var _market_node_id: String = ""
var _rows_container: VBoxContainer = null
var _title_label: Label = null
var _cargo_label: Label = null

func _ready():
	visible = false
	# Center the panel on screen using a MarginContainer.
	var margin = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_CENTER)
	margin.grow_horizontal = Control.GROW_DIRECTION_BOTH
	margin.grow_vertical = Control.GROW_DIRECTION_BOTH
	add_child(margin)

	var panel = PanelContainer.new()
	panel.custom_minimum_size = Vector2(420, 0)
	margin.add_child(panel)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 6)
	panel.add_child(vbox)

	# Station title
	_title_label = Label.new()
	_title_label.text = "STATION MARKET"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", 20)
	vbox.add_child(_title_label)

	vbox.add_child(HSeparator.new())

	# Column header
	var header = HBoxContainer.new()
	for col_name in ["Good", "Buy", "Sell", "", ""]:
		var lbl = Label.new()
		lbl.text = col_name
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		header.add_child(lbl)
	vbox.add_child(header)

	# Goods rows
	_rows_container = VBoxContainer.new()
	_rows_container.add_theme_constant_override("separation", 2)
	vbox.add_child(_rows_container)

	vbox.add_child(HSeparator.new())

	# Cargo summary line
	_cargo_label = Label.new()
	_cargo_label.text = ""
	vbox.add_child(_cargo_label)

	# Undock button
	var btn_undock = Button.new()
	btn_undock.text = "Undock"
	btn_undock.pressed.connect(_on_undock_pressed)
	vbox.add_child(btn_undock)

func open_market_v0(node_id: String) -> void:
	_market_node_id = node_id
	visible = true
	if _title_label:
		_title_label.text = "STATION: %s" % node_id
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
		# DispatchPlayerTradeV0 blocks until sim tick advances — no timer race.
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, true)
		_rebuild_rows()

func sell_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		# DispatchPlayerTradeV0 blocks until sim tick advances — no timer race.
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, false)
		_rebuild_rows()

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
		var good_id: String = str(entry.get("good_id", ""))
		var buy_price: int = int(entry.get("buy_price", 0))
		var sell_price: int = int(entry.get("sell_price", 0))
		var stock: int = int(entry.get("qty", 0))

		var row = HBoxContainer.new()

		var lbl_id = Label.new()
		lbl_id.text = good_id
		lbl_id.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_id)

		var lbl_buy = Label.new()
		lbl_buy.text = "%d cr" % buy_price
		lbl_buy.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_buy)

		var lbl_sell = Label.new()
		lbl_sell.text = "%d cr" % sell_price
		lbl_sell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_sell)

		var btn_buy = Button.new()
		btn_buy.text = "Buy 1"
		btn_buy.pressed.connect(buy_one_v0.bind(good_id))
		row.add_child(btn_buy)

		var btn_sell = Button.new()
		btn_sell.text = "Sell 1"
		btn_sell.pressed.connect(sell_one_v0.bind(good_id))
		row.add_child(btn_sell)

		_rows_container.add_child(row)

	# Update cargo summary
	if _cargo_label:
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("GetPlayerCargoV0"):
			var cargo = bridge.call("GetPlayerCargoV0")
			if typeof(cargo) == TYPE_ARRAY and cargo.size() > 0:
				var parts: Array = []
				for item in cargo:
					if typeof(item) == TYPE_DICTIONARY:
						parts.append("%s x%d" % [str(item.get("good_id", "")), int(item.get("qty", 0))])
				_cargo_label.text = "Cargo: " + (", ".join(parts) if parts.size() > 0 else "empty")
			else:
				_cargo_label.text = "Cargo: empty"
