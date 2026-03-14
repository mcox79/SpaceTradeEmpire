# GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001: Combat log panel (L key toggle).
# CanvasLayer showing last 20 combat events from SimBridge.
extends CanvasLayer

var _panel: PanelContainer = null
var _scroll: ScrollContainer = null
var _vbox: VBoxContainer = null
var _title_label: Label = null
var _bridge: Node = null

var _refresh_counter: int = 0

func _ready() -> void:
	layer = 115
	visible = false
	_build_ui()


func _process(_delta: float) -> void:
	if not visible:
		return
	_refresh_counter += 1
	if _refresh_counter >= 30:  # Refresh every ~0.5s when visible
		_refresh_counter = 0
		refresh_v0()


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

	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_standard(UITheme.BORDER_DANGER))

	var outer_vbox := VBoxContainer.new()
	outer_vbox.add_theme_constant_override("separation", 4)

	# Title
	_title_label = Label.new()
	_title_label.text = "COMBAT LOG"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_color_override("font_color", UITheme.RED_LIGHT)
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
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
	footer.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	footer.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
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
		no_data.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_vbox.add_child(no_data)
		return

	var events: Array = _bridge.call("GetRecentCombatEventsV0")

	if events.size() == 0:
		var no_events := Label.new()
		no_events.text = "No combat events recorded"
		no_events.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_vbox.add_child(no_events)
		_title_label.text = "COMBAT LOG"
		return

	_title_label.text = "COMBAT LOG (%d)" % events.size()

	# Show events newest-first (events array is oldest-last per bridge contract)
	for i in range(events.size() - 1, -1, -1):
		var evt: Dictionary = events[i]
		var tick: int = int(evt.get("tick", 0))
		var attacker: String = str(evt.get("attacker_id", "?"))
		var defender: String = str(evt.get("defender_id", "?"))
		var damage: int = int(evt.get("damage", 0))
		var outcome: String = str(evt.get("outcome", ""))

		var row_text := "Tick %d: %s → %s  %d dmg" % [tick, _display_name(attacker), _display_name(defender), damage]
		if not outcome.is_empty() and outcome != "InProgress":
			row_text += " (%s)" % _humanize_outcome(outcome)

		var row_label := Label.new()
		row_label.text = row_text
		row_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)

		# Color by who is attacking: player shots gold, AI shots red
		if attacker == "fleet_trader_1":
			row_label.add_theme_color_override("font_color", UITheme.GOLD)
		else:
			row_label.add_theme_color_override("font_color", UITheme.RED_LIGHT)

		_vbox.add_child(row_label)


# Resolve internal fleet IDs to human-readable names.
func _display_name(fleet_id: String) -> String:
	if fleet_id == "fleet_trader_1":
		return "Your Fleet"
	if fleet_id.begins_with("ai_fleet_") or fleet_id.begins_with("at_fleet_"):
		if fleet_id.contains("patrol"):
			return "Raider Patrol"
		return "Raider"
	if fleet_id.begins_with("fleet_patrol"):
		return "Sector Patrol"
	if fleet_id.begins_with("fleet_hauler"):
		return "Hauler"
	if fleet_id.begins_with("fleet_trader"):
		return "Trader"
	if fleet_id.begins_with("fleet_"):
		return fleet_id.substr(6).replace("_", " ").capitalize()
	return fleet_id


# Humanize C# enum outcome strings for player display.
func _humanize_outcome(raw: String) -> String:
	match raw:
		"Win": return "Destroyed"
		"Loss": return "Defeated"
		"Draw": return "Disengaged"
		"Victory": return "Destroyed"
		"Defeat": return "Defeated"
		"Flee": return "Fled"
		_: return raw
