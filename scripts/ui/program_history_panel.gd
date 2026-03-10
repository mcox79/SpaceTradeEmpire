# GATE.S7.AUTOMATION_MGMT.HISTORY_VIEW.001: Scrollable timeline of program outcomes per fleet.
# Modal PanelContainer showing last 20 program outcomes from GetProgramPerformanceV0.
# Instantiated by hud.gd; toggled via toggle_v0().
extends PanelContainer

var _bridge: Object = null
var _fleet_id := "fleet_trader_1"
var _scroll: ScrollContainer = null
var _timeline: VBoxContainer = null
var _title_label: Label = null


func _ready() -> void:
	name = "ProgramHistoryPanel"
	visible = false
	mouse_filter = Control.MOUSE_FILTER_STOP

	var style := UITheme.make_panel_modal()
	add_theme_stylebox_override("panel", style)

	custom_minimum_size = Vector2(380, 500)

	_bridge = get_tree().root.get_node_or_null("SimBridge")

	_build_ui()
	_position_panel()


func _build_ui() -> void:
	var vbox := VBoxContainer.new()
	vbox.name = "MainVBox"
	vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	add_child(vbox)

	# ── Header row: title + close button ──
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_MD)
	vbox.add_child(header)

	_title_label = Label.new()
	_title_label.text = "PROGRAM HISTORY"
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	_title_label.add_theme_color_override("font_color", UITheme.CYAN)
	_title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_child(_title_label)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.custom_minimum_size = Vector2(36, 36)
	close_btn.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	close_btn.pressed.connect(toggle_v0)
	header.add_child(close_btn)

	vbox.add_child(HSeparator.new())

	# ── Scrollable timeline ──
	_scroll = ScrollContainer.new()
	_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll.custom_minimum_size = Vector2(0, 400)

	_timeline = VBoxContainer.new()
	_timeline.name = "Timeline"
	_timeline.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_timeline.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.add_child(_timeline)

	vbox.add_child(_scroll)


## Toggle panel visibility. Refreshes timeline data on show.
func toggle_v0() -> void:
	visible = not visible
	if visible:
		_position_panel()
		_refresh_timeline()


## Set the fleet to display history for and refresh.
func set_fleet_id(fleet_id: String) -> void:
	_fleet_id = fleet_id
	if visible:
		_refresh_timeline()


## Refresh the timeline from SimBridge data.
func _refresh_timeline() -> void:
	# Clear existing rows.
	for child in _timeline.get_children():
		child.queue_free()

	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("SimBridge")
	if _bridge == null or not _bridge.has_method("GetProgramPerformanceV0"):
		_add_empty_label("No bridge available")
		return

	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", _fleet_id)
	if data.is_empty():
		_add_empty_label("No data available")
		return

	var history: Array = data.get("history", [])
	if history.size() == 0:
		_add_empty_label("No history yet")
		return

	_title_label.text = "PROGRAM HISTORY (%d)" % history.size()

	# Show newest first — iterate in reverse.
	for i in range(history.size() - 1, -1, -1):
		var entry: Dictionary = history[i]
		var tick: int = int(entry.get("tick", 0))
		var success: bool = entry.get("success", false)
		var goods: int = int(entry.get("goods_moved", 0))
		var credits: int = int(entry.get("credits_earned", 0))
		var reason: String = str(entry.get("failure_reason", ""))

		_add_history_row(tick, success, credits, goods, reason)


## Build a single history row as an HBoxContainer.
func _add_history_row(tick: int, success: bool, credits: int, goods: int, reason: String) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Tick number (left, muted)
	var tick_label := Label.new()
	tick_label.text = "T%d" % tick
	tick_label.custom_minimum_size = Vector2(60, 0)
	tick_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	tick_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	tick_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(tick_label)

	# Outcome indicator
	var outcome_label := Label.new()
	outcome_label.custom_minimum_size = Vector2(40, 0)
	outcome_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	outcome_label.mouse_filter = Control.MOUSE_FILTER_IGNORE

	if success:
		outcome_label.text = "OK"
		outcome_label.add_theme_color_override("font_color", UITheme.GREEN)
	else:
		outcome_label.text = "FAIL"
		outcome_label.add_theme_color_override("font_color", UITheme.RED)

	hbox.add_child(outcome_label)

	# Detail: credits/goods for success, reason for failure
	var detail_label := Label.new()
	detail_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	detail_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	detail_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	detail_label.mouse_filter = Control.MOUSE_FILTER_IGNORE

	if success:
		detail_label.text = "+%dc / %dg" % [credits, goods]
		detail_label.add_theme_color_override("font_color", UITheme.GREEN)
	else:
		detail_label.text = reason if reason != "" else "Unknown"
		# Budget-related failures get yellow, others get red.
		if reason.containsn("budget"):
			detail_label.add_theme_color_override("font_color", UITheme.YELLOW)
		else:
			detail_label.add_theme_color_override("font_color", UITheme.RED)

	hbox.add_child(detail_label)

	_timeline.add_child(hbox)


## Show a placeholder label when there is no data.
func _add_empty_label(text: String) -> void:
	var label := Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_timeline.add_child(label)


## Center the panel in the viewport (modal position).
func _position_panel() -> void:
	var vp_size := get_viewport_rect().size
	position = Vector2(
		(vp_size.x - custom_minimum_size.x) * 0.5,
		(vp_size.y - custom_minimum_size.y) * 0.5
	)
