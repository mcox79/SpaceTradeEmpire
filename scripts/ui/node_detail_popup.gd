# GATE.S6.MAP_GALAXY.NODE_CLICK.001: Galaxy map node detail popup.
# L2.4: Enhanced with trade intel — profit vs current system, price opportunities.
# Shows node info when clicking a node on the galaxy overlay.
# Extends CanvasLayer so it renders above the 3D galaxy map.
extends CanvasLayer

var _panel: PanelContainer = null
var _vbox: VBoxContainer = null
var _close_btn: Button = null
var _name_label: Label = null
var _visit_label: Label = null
var _class_label: Label = null
var _fleet_label: Label = null
var _industry_label: Label = null
var _security_label: Label = null
var _regime_label: Label = null  # GATE.S7.TERRITORY.BRIDGE_DISPLAY.001
var _opportunity_label: Label = null  # L2.4: Price opportunity callout
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
	_panel.custom_minimum_size = Vector2(340, 0)

	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_ship_computer())

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 4)

	# Header row: name + close button
	var header := HBoxContainer.new()
	_name_label = Label.new()
	_name_label.add_theme_color_override("font_color", UITheme.CYAN)
	_name_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(_name_label)

	_close_btn = Button.new()
	_close_btn.text = "X"
	_close_btn.custom_minimum_size = Vector2(28, 28)
	_close_btn.pressed.connect(_on_close)
	header.add_child(_close_btn)
	_vbox.add_child(header)

	# Visit status
	_visit_label = Label.new()
	_visit_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_vbox.add_child(_visit_label)

	# Thin rule
	var rule := ColorRect.new()
	rule.custom_minimum_size = Vector2(0, 1)
	rule.color = Color(UITheme.CYAN.r, UITheme.CYAN.g, UITheme.CYAN.b, 0.35)
	rule.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	rule.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_vbox.add_child(rule)

	# Detail labels
	_class_label = Label.new()
	_class_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_class_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_vbox.add_child(_class_label)

	_fleet_label = Label.new()
	_fleet_label.add_theme_color_override("font_color", UITheme.GOLD)
	_fleet_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_vbox.add_child(_fleet_label)

	_industry_label = Label.new()
	_industry_label.add_theme_color_override("font_color", UITheme.GREEN_SOFT)
	_industry_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_vbox.add_child(_industry_label)

	_security_label = Label.new()
	_security_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_vbox.add_child(_security_label)

	# GATE.S7.TERRITORY.BRIDGE_DISPLAY.001: Territory regime label
	_regime_label = Label.new()
	_regime_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_regime_label.visible = false
	_vbox.add_child(_regime_label)

	# L2.4: Price opportunity callout
	_opportunity_label = Label.new()
	_opportunity_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_opportunity_label.add_theme_color_override("font_color", UITheme.GOLD)
	_opportunity_label.visible = false
	_vbox.add_child(_opportunity_label)

	# Market section separator
	var rule2 := ColorRect.new()
	rule2.custom_minimum_size = Vector2(0, 1)
	rule2.color = Color(UITheme.CYAN.r, UITheme.CYAN.g, UITheme.CYAN.b, 0.2)
	rule2.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	rule2.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_vbox.add_child(rule2)

	_market_header = Label.new()
	_market_header.text = "MARKET"
	_market_header.add_theme_color_override("font_color", UITheme.CYAN)
	_market_header.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
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

	_name_label.text = node_name.to_upper()
	_class_label.text = "Class: " + world_class
	_fleet_label.text = "Fleets: " + str(fleet_count)
	_industry_label.text = "Industry: " + str(industry_count) + " sites"

	# L2.4: Visit status indicator
	var gm = get_node_or_null("/root/GameManager")
	var player_node_id: String = ""
	if gm:
		var ps: Dictionary = {}
		if _bridge.has_method("GetPlayerStateV0"):
			ps = _bridge.call("GetPlayerStateV0")
		player_node_id = str(ps.get("current_node_id", ""))
	if node_id == player_node_id:
		_visit_label.text = "You are here"
		_visit_label.add_theme_color_override("font_color", UITheme.CYAN)
	else:
		# Check exploration status
		var explored: bool = false
		if _bridge.has_method("GetExplorationOverlayV0"):
			var expl: Dictionary = _bridge.call("GetExplorationOverlayV0")
			var status: String = str(expl.get(node_id, "unvisited"))
			if status == "visited" or status == "mapped" or status == "anomaly":
				explored = true
		if explored:
			_visit_label.text = "Visited"
			_visit_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		else:
			_visit_label.text = "??? Unexplored"
			_visit_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)

	# Security display
	var sec_pct := security_bps / 100.0
	var band := "safe"
	if security_bps < 3000:
		band = "hostile"
	elif security_bps < 5000:
		band = "dangerous"
	elif security_bps < 8000:
		band = "moderate"
	var sec_icon_text := UITheme.security_icon_text(band)
	var sec_text := "Security: %.0f%% — %s" % [sec_pct, sec_icon_text]
	var sec_color := UITheme.security_color(band)
	_security_label.text = sec_text
	_security_label.add_theme_color_override("font_color", sec_color)

	# GATE.S7.TERRITORY.BRIDGE_DISPLAY.001: Territory regime display
	if _regime_label and _bridge and _bridge.has_method("GetTerritoryRegimeV0"):
		var regime_data: Dictionary = _bridge.call("GetTerritoryRegimeV0", node_id)
		var regime: String = str(regime_data.get("regime", "Open"))
		var regime_color: Color = regime_data.get("regime_color", Color.WHITE)
		var faction_id: String = str(regime_data.get("faction_id", ""))
		_regime_label.text = "Territory: %s" % regime
		_regime_label.add_theme_color_override("font_color", regime_color)
		_regime_label.visible = true
		# GATE.T64.UI.FACTION_ACCENT.001: Tint header label with faction accent color.
		if not faction_id.is_empty():
			_name_label.add_theme_color_override("font_color", UITheme.get_faction_accent(faction_id))

	# Populate market data with profit comparison
	_populate_market_v0(node_id, player_node_id)

	# Position popup near click, clamped to viewport
	var vp_size := get_viewport().get_visible_rect().size
	var popup_x := clampf(screen_pos.x + 20, 0, vp_size.x - 360)
	var popup_y := clampf(screen_pos.y - 40, 0, vp_size.y - 200)
	_panel.position = Vector2(popup_x, popup_y)

	visible = true


func _on_close() -> void:
	visible = false


# L2.4: Enhanced market display with profit vs player's current system.
func _populate_market_v0(node_id: String, player_node_id: String) -> void:
	# Clear previous rows
	if _market_container:
		for child in _market_container.get_children():
			_market_container.remove_child(child)
			child.queue_free()
	_opportunity_label.visible = false

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetPlayerMarketViewV0"):
		_market_header.text = "MARKET"
		var no_data := Label.new()
		no_data.text = "No market data available"
		no_data.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_market_container.add_child(no_data)
		return

	var goods: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	if goods.size() == 0:
		_market_header.text = "MARKET"
		var no_market := Label.new()
		no_market.text = "No market at this node"
		no_market.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_market_container.add_child(no_market)
		return

	# Get player's current market for profit comparison
	var player_market: Dictionary = {}
	if player_node_id != "" and player_node_id != node_id:
		var player_goods: Array = _bridge.call("GetPlayerMarketViewV0", player_node_id)
		for pg in player_goods:
			var gid: String = str(pg.get("good_id", ""))
			if gid != "":
				player_market[gid] = pg

	_market_header.text = "MARKET (%d goods)" % goods.size()

	# Column header row
	var hdr := HBoxContainer.new()
	for col_data in [
		{"text": "Good", "w": 90},
		{"text": "Buy", "w": 45},
		{"text": "Sell", "w": 45},
		{"text": "Qty", "w": 30},
		{"text": "Profit", "w": 50},
	]:
		var lbl := Label.new()
		lbl.text = col_data["text"]
		lbl.custom_minimum_size = Vector2(col_data["w"], 0)
		lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		hdr.add_child(lbl)
	_market_container.add_child(hdr)

	# Track best profit opportunity
	var best_profit: int = 0
	var best_good: String = ""

	# Goods rows
	for entry in goods:
		var good_id: String = str(entry.get("good_id", "?"))
		var buy_price: int = int(entry.get("buy_price", 0))
		var sell_price: int = int(entry.get("sell_price", 0))
		var qty: int = int(entry.get("quantity", 0))

		var row := HBoxContainer.new()

		var good_lbl := Label.new()
		good_lbl.text = _format_good_name(good_id)
		good_lbl.custom_minimum_size = Vector2(90, 0)
		good_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		good_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		row.add_child(good_lbl)

		var buy_lbl := Label.new()
		buy_lbl.text = str(buy_price)
		buy_lbl.custom_minimum_size = Vector2(45, 0)
		buy_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
		buy_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		UITheme.apply_mono(buy_lbl)
		row.add_child(buy_lbl)

		var sell_lbl := Label.new()
		sell_lbl.text = str(sell_price)
		sell_lbl.custom_minimum_size = Vector2(45, 0)
		sell_lbl.add_theme_color_override("font_color", UITheme.profit_color())
		sell_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		UITheme.apply_mono(sell_lbl)
		row.add_child(sell_lbl)

		var qty_lbl := Label.new()
		qty_lbl.text = str(qty)
		qty_lbl.custom_minimum_size = Vector2(30, 0)
		qty_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		qty_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		UITheme.apply_mono(qty_lbl)
		row.add_child(qty_lbl)

		# L2.4: Profit vs player's current market
		var profit_lbl := Label.new()
		profit_lbl.custom_minimum_size = Vector2(50, 0)
		profit_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		UITheme.apply_mono(profit_lbl)
		if player_market.has(good_id):
			# Player can buy at their market, sell here
			var player_buy: int = int(player_market[good_id].get("buy_price", 0))
			var profit: int = sell_price - player_buy
			if profit > 0:
				profit_lbl.text = "+%d" % profit
				profit_lbl.add_theme_color_override("font_color", UITheme.profit_color())
				if profit > best_profit:
					best_profit = profit
					best_good = good_id
			elif profit < 0:
				profit_lbl.text = str(profit)
				profit_lbl.add_theme_color_override("font_color", UITheme.loss_color())
			else:
				profit_lbl.text = "—"
				profit_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		else:
			profit_lbl.text = "—"
			profit_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		row.add_child(profit_lbl)

		_market_container.add_child(row)

	# L2.4: Show best price opportunity callout
	if best_profit > 0 and best_good != "":
		_opportunity_label.text = "TRADE: %s +%d cr/unit" % [_format_good_name(best_good), best_profit]
		_opportunity_label.visible = true


func _format_good_name(good_id: String) -> String:
	return good_id.replace("_", " ").capitalize()


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
