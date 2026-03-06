# GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001: Combat log panel (L key toggle).
# CanvasLayer showing last 20 combat events from SimBridge.
extends CanvasLayer

var _panel: PanelContainer = null
var _scroll: ScrollContainer = null
var _vbox: VBoxContainer = null
var _title_label: Label = null
var _bridge: Node = null

func _ready() -> void:
	layer = 115
	visible = false
	_build_ui()


func _build_ui() -> void:
	_panel = PanelContainer.new()
	_panel.name = "CombatLogPanelContainer"
	_panel.custom_minimum_size = Vector2(460, 360)

	# Anchor to right side of screen
	_panel.anchor_left = 1.0
	_panel.anchor_right = 1.0
	_panel.anchor_top = 0.0
	_panel.anchor_bottom = 0.0
	_panel.offset_left = -480
	_panel.offset_right = -16
	_panel.offset_top = 60
	_panel.offset_bottom = 420

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.04, 0.04, 0.08, 0.92)
	style.border_color = Color(1.0, 0.3, 0.3, 0.7)
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

	var outer_vbox := VBoxContainer.new()
	outer_vbox.add_theme_constant_override("separation", 4)

	# Title
	_title_label = Label.new()
	_title_label.text = "=== COMBAT LOG ==="
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_color_override("font_color", Color(1.0, 0.4, 0.4))
	_title_label.add_theme_font_size_override("font_size", 16)
	outer_vbox.add_child(_title_label)

	outer_vbox.add_child(HSeparator.new())

	_scroll = ScrollContainer.new()
	_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll.custom_minimum_size = Vector2(0, 280)

	_vbox = VBoxContainer.new()
	_vbox.name = "EventRows"
	_vbox.add_theme_constant_override("separation", 2)
	_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.add_child(_vbox)

	outer_vbox.add_child(_scroll)

	# Footer
	var footer := Label.new()
	footer.text = "Press L to close"
	footer.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	footer.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
	footer.add_theme_font_size_override("font_size", 12)
	outer_vbox.add_child(footer)

	_panel.add_child(outer_vbox)
	add_child(_panel)


func refresh_v0() -> void:
	if _vbox == null:
		return

	# Clear old rows
	for child in _vbox.get_children():
		_vbox.remove_child(child)
		child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetRecentCombatEventsV0"):
		var no_data := Label.new()
		no_data.text = "No combat data available"
		no_data.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
		_vbox.add_child(no_data)
		return

	var events: Array = _bridge.call("GetRecentCombatEventsV0")

	if events.size() == 0:
		var no_events := Label.new()
		no_events.text = "No combat events recorded"
		no_events.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
		_vbox.add_child(no_events)
		_title_label.text = "=== COMBAT LOG (0) ==="
		return

	_title_label.text = "=== COMBAT LOG (%d) ===" % events.size()

	# Show events newest-first (events array is oldest-last per bridge contract)
	for i in range(events.size() - 1, -1, -1):
		var evt: Dictionary = events[i]
		var tick: int = int(evt.get("tick", 0))
		var attacker: String = str(evt.get("attacker_id", "?"))
		var defender: String = str(evt.get("defender_id", "?"))
		var damage: int = int(evt.get("damage", 0))
		var outcome: String = str(evt.get("outcome", ""))

		var row_text := "Tick %d: %s -> %s  %d dmg" % [tick, _short_id(attacker), _short_id(defender), damage]
		if not outcome.is_empty():
			row_text += " (%s)" % outcome

		var row_label := Label.new()
		row_label.text = row_text
		row_label.add_theme_font_size_override("font_size", 13)

		# Color by who is attacking: player shots gold, AI shots red
		if attacker == "fleet_trader_1":
			row_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4))
		else:
			row_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.5))

		_vbox.add_child(row_label)


# Shorten fleet IDs for display: "fleet_trader_1" -> "trader_1", "fleet_patrol_4" -> "patrol_4"
func _short_id(fleet_id: String) -> String:
	if fleet_id.begins_with("fleet_"):
		return fleet_id.substr(6)
	return fleet_id
