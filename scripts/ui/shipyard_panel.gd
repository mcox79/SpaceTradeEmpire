# scripts/ui/shipyard_panel.gd
# GATE.T59.SHIP.DOCK_SHIPYARD_TAB.001: Shipyard dock tab UI
# Shows available ships for purchase at the current station with stats, pricing, and comparison.
extends VBoxContainer

var _bridge: Node = null
var _node_id: String = ""
var _scroll: ScrollContainer = null
var _cards_container: VBoxContainer = null
var _empty_state: VBoxContainer = null
var _comparison_panel = null  # ship_comparison_panel.gd instance (set externally)
var _player_class_id: String = ""

func _ready():
	add_theme_constant_override("separation", UITheme.SPACE_MD)
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL

	# Header
	var header := UITheme.make_section_header("Shipyard", UITheme.CYAN)
	add_child(header)

	# Scroll container for ship cards
	_scroll = ScrollContainer.new()
	_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.custom_minimum_size = Vector2(0, 200)
	add_child(_scroll)

	_cards_container = VBoxContainer.new()
	_cards_container.add_theme_constant_override("separation", UITheme.SPACE_LG)
	_cards_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.add_child(_cards_container)

	# Empty state (hidden by default)
	_empty_state = UITheme.make_empty_state("⚓", "No ships available", "This station has no shipyard.")
	_empty_state.visible = false
	add_child(_empty_state)


func setup(bridge_ref: Node, comparison_ref) -> void:
	_bridge = bridge_ref
	_comparison_panel = comparison_ref
	print("DEBUG_SHIPYARD_ setup complete, bridge=%s, comparison=%s" % [bridge_ref != null, comparison_ref != null])


func refresh(node_id: String) -> void:
	_node_id = node_id
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		print("DEBUG_SHIPYARD_ no bridge, cannot refresh")
		return

	# Get player's current ship class for comparison
	var player_state: Dictionary = _bridge.call("GetPlayerStateV0")
	_player_class_id = str(player_state.get("ship_class_id", ""))
	print("DEBUG_SHIPYARD_ refresh node=%s player_class=%s" % [node_id, _player_class_id])

	# Get catalog
	var catalog: Array = _bridge.call("GetShipyardCatalogV0", node_id)
	print("DEBUG_SHIPYARD_ catalog size=%d" % catalog.size())

	# Clear old cards
	for child in _cards_container.get_children():
		child.queue_free()

	if catalog.size() == 0:
		_scroll.visible = false
		_empty_state.visible = true
		return

	_scroll.visible = true
	_empty_state.visible = false

	# Build ship cards
	for ship_data in catalog:
		var card := _build_ship_card(ship_data)
		_cards_container.add_child(card)


func _build_ship_card(data: Dictionary) -> PanelContainer:
	var class_id: String = str(data.get("class_id", ""))
	var display_name: String = str(data.get("display_name", "Unknown"))
	var price: int = int(data.get("price", 0))
	var can_afford: bool = bool(data.get("can_afford", false))
	var meets_rep: bool = bool(data.get("meets_rep_requirement", true))
	var is_variant: bool = bool(data.get("is_variant", false))
	var faction_id: String = str(data.get("faction_id", ""))

	# Card panel
	var card := PanelContainer.new()
	var accent := UITheme.BORDER_ACCENT
	if is_variant:
		accent = UITheme.GOLD
	card.add_theme_stylebox_override("panel", UITheme.make_panel_ship_computer(accent))
	card.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var card_vbox := VBoxContainer.new()
	card_vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	card_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	card.add_child(card_vbox)

	# --- Title row: name + faction badge ---
	var title_row := HBoxContainer.new()
	title_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var name_label := Label.new()
	name_label.text = display_name.to_upper()
	name_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	name_label.add_theme_color_override("font_color", UITheme.CYAN if not is_variant else UITheme.GOLD)
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title_row.add_child(name_label)

	if is_variant and faction_id != "":
		var faction_badge := Label.new()
		faction_badge.text = "[%s]" % faction_id.to_upper()
		faction_badge.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		faction_badge.add_theme_color_override("font_color", UITheme.GOLD)
		title_row.add_child(faction_badge)

	card_vbox.add_child(title_row)

	# --- Stats grid (2 columns of stats) ---
	var stats_grid := GridContainer.new()
	stats_grid.columns = 4  # label, value, label, value
	stats_grid.add_theme_constant_override("h_separation", UITheme.SPACE_LG)
	stats_grid.add_theme_constant_override("v_separation", UITheme.SPACE_XS)
	stats_grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var stat_defs := [
		["Hull", str(data.get("core_hull", 0))],
		["Shield", str(data.get("base_shield", 0))],
		["Cargo", str(data.get("cargo_capacity", 0))],
		["Power", str(data.get("base_power", 0))],
		["Slots", str(data.get("slot_count", 0))],
		["Mass", str(data.get("mass", 0))],
		["Scan", str(data.get("scan_range", 0))],
		["Fuel", str(data.get("base_fuel_capacity", 0))],
	]

	for stat in stat_defs:
		var stat_label := Label.new()
		stat_label.text = stat[0] + ":"
		stat_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		stat_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		stats_grid.add_child(stat_label)

		var stat_value := Label.new()
		stat_value.text = stat[1]
		stat_value.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		stat_value.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		UITheme.apply_mono(stat_value)
		stats_grid.add_child(stat_value)

	card_vbox.add_child(stats_grid)

	# --- Price + action buttons row ---
	var action_row := HBoxContainer.new()
	action_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	action_row.add_theme_constant_override("separation", UITheme.SPACE_LG)

	# Price label
	var price_label := Label.new()
	price_label.text = UITheme.fmt_credits(price)
	price_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	if can_afford:
		price_label.add_theme_color_override("font_color", UITheme.GREEN)
	else:
		price_label.add_theme_color_override("font_color", UITheme.RED_LIGHT)
	UITheme.apply_mono(price_label)
	price_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	action_row.add_child(price_label)

	# Rep requirement warning
	if not meets_rep:
		var rep_warn := Label.new()
		rep_warn.text = "REP LOCKED"
		rep_warn.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		rep_warn.add_theme_color_override("font_color", UITheme.RED_LIGHT)
		action_row.add_child(rep_warn)

	# Compare button
	var compare_btn := Button.new()
	compare_btn.text = "Compare"
	UITheme.style_action_button(compare_btn, "neutral")
	compare_btn.pressed.connect(_on_compare_pressed.bind(class_id))
	action_row.add_child(compare_btn)

	# Buy button
	var buy_btn := Button.new()
	buy_btn.text = "Buy"
	if can_afford and meets_rep:
		UITheme.style_action_button(buy_btn, "accept")
	else:
		UITheme.style_action_button(buy_btn, "reject")
		buy_btn.disabled = true
	buy_btn.pressed.connect(_on_buy_pressed.bind(class_id, display_name))
	action_row.add_child(buy_btn)

	card_vbox.add_child(action_row)

	return card


func _on_compare_pressed(class_id: String) -> void:
	print("DEBUG_SHIPYARD_ compare: current=%s vs target=%s" % [_player_class_id, class_id])
	if _comparison_panel and _comparison_panel.has_method("show_comparison") and _bridge:
		_comparison_panel.show_comparison(_bridge, _player_class_id, class_id)
	else:
		print("DEBUG_SHIPYARD_ comparison panel not available")


func _on_buy_pressed(class_id: String, display_name: String) -> void:
	if _bridge == null:
		return
	print("DEBUG_SHIPYARD_ purchasing ship: %s (%s) at node %s" % [display_name, class_id, _node_id])
	var result: Dictionary = _bridge.call("PurchaseShipV0", class_id, _node_id)
	var success: bool = bool(result.get("success", false))
	var message: String = str(result.get("message", ""))
	print("DEBUG_SHIPYARD_ purchase result: success=%s message=%s" % [success, message])

	# Show toast if available
	var toast_mgr = null
	if is_inside_tree() and get_tree() != null:
		toast_mgr = get_tree().root.find_child("ToastManager", true, false)
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		if success:
			toast_mgr.call("show_priority_toast", "Ship acquired: %s" % display_name, UITheme.GREEN)
		else:
			toast_mgr.call("show_priority_toast", "Purchase failed: %s" % message, UITheme.RED_LIGHT)

	# Refresh catalog after purchase
	if success:
		refresh(_node_id)
