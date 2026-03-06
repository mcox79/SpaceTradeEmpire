# GATE.S6.MAP_GALAXY.NODE_CLICK.001: Galaxy map node detail popup.
# Shows node info when clicking a node on the galaxy overlay.
# Extends CanvasLayer so it renders above the 3D galaxy map.
extends CanvasLayer

var _panel: PanelContainer = null
var _vbox: VBoxContainer = null
var _close_btn: Button = null
var _name_label: Label = null
var _class_label: Label = null
var _fleet_label: Label = null
var _industry_label: Label = null
var _security_label: Label = null
var _market_header: Label = null
var _market_container: VBoxContainer = null
var _bridge: Node = null

func _ready() -> void:
	layer = 110
	visible = false
	_bridge = get_node_or_null("/root/SimBridge")
	_build_ui()


func _build_ui() -> void:
	_panel = PanelContainer.new()
	_panel.name = "NodeDetailPanel"
	_panel.custom_minimum_size = Vector2(280, 0)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.06, 0.06, 0.10, 0.92)
	style.border_color = Color(0.3, 0.6, 1.0, 0.8)
	style.border_width_left = 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.content_margin_left = 12.0
	style.content_margin_right = 12.0
	style.content_margin_top = 8.0
	style.content_margin_bottom = 10.0
	_panel.add_theme_stylebox_override("panel", style)

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 4)

	# Header row: name + close button
	var header := HBoxContainer.new()
	_name_label = Label.new()
	_name_label.add_theme_color_override("font_color", Color(0.4, 0.85, 1.0))
	_name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(_name_label)

	_close_btn = Button.new()
	_close_btn.text = "X"
	_close_btn.custom_minimum_size = Vector2(28, 28)
	_close_btn.pressed.connect(_on_close)
	header.add_child(_close_btn)
	_vbox.add_child(header)

	# Separator
	_vbox.add_child(HSeparator.new())

	# Detail labels
	_class_label = Label.new()
	_class_label.add_theme_color_override("font_color", Color(0.8, 0.8, 0.9))
	_vbox.add_child(_class_label)

	_fleet_label = Label.new()
	_fleet_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.3))
	_vbox.add_child(_fleet_label)

	_industry_label = Label.new()
	_industry_label.add_theme_color_override("font_color", Color(0.6, 1.0, 0.6))
	_vbox.add_child(_industry_label)

	_security_label = Label.new()
	_vbox.add_child(_security_label)

	# GATE.S11.GAME_FEEL.NODE_MARKET.001: Market section
	_vbox.add_child(HSeparator.new())

	_market_header = Label.new()
	_market_header.text = "Market"
	_market_header.add_theme_color_override("font_color", Color(0.4, 0.85, 1.0))
	_vbox.add_child(_market_header)

	_market_container = VBoxContainer.new()
	_market_container.name = "MarketRows"
	_market_container.add_theme_constant_override("separation", 2)
	_vbox.add_child(_market_container)

	_panel.add_child(_vbox)
	add_child(_panel)


func show_for_node(node_id: String, screen_pos: Vector2) -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetNodeDetailV0"):
		return

	var detail: Dictionary = _bridge.call("GetNodeDetailV0", node_id)
	if detail.is_empty():
		return

	var node_name: String = detail.get("node_name", node_id)
	var world_class: String = detail.get("world_class_id", "Unknown")
	var fleet_count: int = detail.get("fleet_count", 0)
	var industry_count: int = detail.get("industry_count", 0)
	var security_bps: int = detail.get("security_bps", 5000)

	_name_label.text = node_name
	_class_label.text = "Class: " + world_class
	_fleet_label.text = "Fleets: " + str(fleet_count)
	_industry_label.text = "Industry: " + str(industry_count) + " sites"

	# Security display
	var sec_pct := security_bps / 100.0
	var sec_text := "Security: %.0f%%" % sec_pct
	var sec_color := Color(0.2, 1.0, 0.4)  # green
	if security_bps < 3000:
		sec_color = Color(1.0, 0.15, 0.15)  # red
		sec_text += " (Hostile)"
	elif security_bps < 5000:
		sec_color = Color(1.0, 0.6, 0.2)  # orange
		sec_text += " (Dangerous)"
	elif security_bps >= 8000:
		sec_text += " (Safe)"
	else:
		sec_color = Color(0.4, 0.7, 1.0)  # blue
		sec_text += " (Moderate)"
	_security_label.text = sec_text
	_security_label.add_theme_color_override("font_color", sec_color)

	# GATE.S11.GAME_FEEL.NODE_MARKET.001: Populate market data
	_populate_market_v0(node_id)

	# Position popup near click, clamped to viewport
	var vp_size := get_viewport().get_visible_rect().size
	var popup_x := clampf(screen_pos.x + 20, 0, vp_size.x - 300)
	var popup_y := clampf(screen_pos.y - 40, 0, vp_size.y - 200)
	_panel.position = Vector2(popup_x, popup_y)

	visible = true


func _on_close() -> void:
	visible = false


# GATE.S11.GAME_FEEL.NODE_MARKET.001: Query bridge for market inventory at this node.
func _populate_market_v0(node_id: String) -> void:
	# Clear previous rows
	if _market_container:
		for child in _market_container.get_children():
			_market_container.remove_child(child)
			child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetPlayerMarketViewV0"):
		_market_header.text = "Market"
		var no_data := Label.new()
		no_data.text = "No market data available"
		no_data.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
		_market_container.add_child(no_data)
		return

	var goods: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	if goods.size() == 0:
		_market_header.text = "Market"
		var no_market := Label.new()
		no_market.text = "No market at this node"
		no_market.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
		_market_container.add_child(no_market)
		return

	_market_header.text = "Market (%d goods)" % goods.size()

	# Column header row
	var hdr := HBoxContainer.new()
	var h_good := Label.new()
	h_good.text = "Good"
	h_good.custom_minimum_size = Vector2(100, 0)
	h_good.add_theme_color_override("font_color", Color(0.6, 0.6, 0.7))
	h_good.add_theme_font_size_override("font_size", 12)
	hdr.add_child(h_good)
	var h_buy := Label.new()
	h_buy.text = "Buy"
	h_buy.custom_minimum_size = Vector2(50, 0)
	h_buy.add_theme_color_override("font_color", Color(0.6, 0.6, 0.7))
	h_buy.add_theme_font_size_override("font_size", 12)
	hdr.add_child(h_buy)
	var h_sell := Label.new()
	h_sell.text = "Sell"
	h_sell.custom_minimum_size = Vector2(50, 0)
	h_sell.add_theme_color_override("font_color", Color(0.6, 0.6, 0.7))
	h_sell.add_theme_font_size_override("font_size", 12)
	hdr.add_child(h_sell)
	var h_qty := Label.new()
	h_qty.text = "Qty"
	h_qty.custom_minimum_size = Vector2(40, 0)
	h_qty.add_theme_color_override("font_color", Color(0.6, 0.6, 0.7))
	h_qty.add_theme_font_size_override("font_size", 12)
	hdr.add_child(h_qty)
	_market_container.add_child(hdr)

	# Goods rows
	for entry in goods:
		var row := HBoxContainer.new()

		var good_lbl := Label.new()
		good_lbl.text = str(entry.get("good_id", "?"))
		good_lbl.custom_minimum_size = Vector2(100, 0)
		good_lbl.add_theme_color_override("font_color", Color(0.85, 0.85, 0.9))
		good_lbl.add_theme_font_size_override("font_size", 13)
		row.add_child(good_lbl)

		var buy_lbl := Label.new()
		buy_lbl.text = str(entry.get("buy_price", 0))
		buy_lbl.custom_minimum_size = Vector2(50, 0)
		buy_lbl.add_theme_color_override("font_color", Color(1.0, 0.6, 0.3))
		buy_lbl.add_theme_font_size_override("font_size", 13)
		row.add_child(buy_lbl)

		var sell_lbl := Label.new()
		sell_lbl.text = str(entry.get("sell_price", 0))
		sell_lbl.custom_minimum_size = Vector2(50, 0)
		sell_lbl.add_theme_color_override("font_color", Color(0.3, 1.0, 0.5))
		sell_lbl.add_theme_font_size_override("font_size", 13)
		row.add_child(sell_lbl)

		var qty_lbl := Label.new()
		qty_lbl.text = str(entry.get("quantity", 0))
		qty_lbl.custom_minimum_size = Vector2(40, 0)
		qty_lbl.add_theme_color_override("font_color", Color(0.8, 0.8, 0.9))
		qty_lbl.add_theme_font_size_override("font_size", 13)
		row.add_child(qty_lbl)

		_market_container.add_child(row)


func _unhandled_input(event: InputEvent) -> void:
	if not visible:
		return
	# Click outside to close
	if event is InputEventMouseButton and event.pressed:
		if _panel != null:
			var rect := Rect2(_panel.position, _panel.size)
			if not rect.has_point(event.position):
				visible = false
				get_viewport().set_input_as_handled()
