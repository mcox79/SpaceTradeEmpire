extends Control

# GATE.S9.MILESTONES.VIEWER.001: Milestone viewer panel with card grid + lifetime stats sidebar.

var _bridge: Node
var _card_grid: GridContainer
var _stats_vbox: VBoxContainer
var _back_btn: Button


func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

	# Full-screen dark background.
	var bg := ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.02, 0.02, 0.06, 0.97)
	add_child(bg)

	# Main layout: HBoxContainer (cards | stats sidebar).
	var root_hbox := HBoxContainer.new()
	root_hbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	root_hbox.set_anchor_and_offset(SIDE_LEFT, 0.0, 40)
	root_hbox.set_anchor_and_offset(SIDE_RIGHT, 1.0, -40)
	root_hbox.set_anchor_and_offset(SIDE_TOP, 0.0, 40)
	root_hbox.set_anchor_and_offset(SIDE_BOTTOM, 1.0, -40)
	root_hbox.add_theme_constant_override("separation", 24)
	add_child(root_hbox)

	# Left side: title + card grid in scroll.
	var left_vbox := VBoxContainer.new()
	left_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	left_vbox.size_flags_stretch_ratio = 3.0
	left_vbox.add_theme_constant_override("separation", 16)
	root_hbox.add_child(left_vbox)

	var title := Label.new()
	title.text = "MILESTONES"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 36)
	title.add_theme_color_override("font_color", Color(0.85, 0.9, 1.0))
	left_vbox.add_child(title)

	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	left_vbox.add_child(scroll)

	_card_grid = GridContainer.new()
	_card_grid.columns = 3
	_card_grid.add_theme_constant_override("h_separation", 12)
	_card_grid.add_theme_constant_override("v_separation", 12)
	_card_grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_card_grid)

	# Right side: stats sidebar.
	var right_panel := PanelContainer.new()
	right_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	right_panel.size_flags_stretch_ratio = 1.0
	var sidebar_style := StyleBoxFlat.new()
	sidebar_style.bg_color = Color(0.04, 0.05, 0.1, 0.9)
	sidebar_style.border_color = Color(0.15, 0.2, 0.35)
	sidebar_style.set_border_width_all(1)
	sidebar_style.set_corner_radius_all(6)
	sidebar_style.set_content_margin_all(16)
	right_panel.add_theme_stylebox_override("panel", sidebar_style)
	root_hbox.add_child(right_panel)

	var right_vbox := VBoxContainer.new()
	right_vbox.add_theme_constant_override("separation", 8)
	right_panel.add_child(right_vbox)

	var stats_title := Label.new()
	stats_title.text = "LIFETIME STATS"
	stats_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	stats_title.add_theme_font_size_override("font_size", 22)
	stats_title.add_theme_color_override("font_color", Color(0.7, 0.8, 1.0))
	right_vbox.add_child(stats_title)

	var stats_sep := HSeparator.new()
	right_vbox.add_child(stats_sep)

	_stats_vbox = VBoxContainer.new()
	_stats_vbox.add_theme_constant_override("separation", 6)
	right_vbox.add_child(_stats_vbox)

	# Back button at bottom-right.
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	right_vbox.add_child(spacer)

	_back_btn = Button.new()
	_back_btn.text = "Back"
	_back_btn.custom_minimum_size = Vector2(120, 40)
	_back_btn.add_theme_font_size_override("font_size", 18)
	_back_btn.pressed.connect(_on_back)
	right_vbox.add_child(_back_btn)

	_populate()


func _populate() -> void:
	# Milestone cards.
	for child in _card_grid.get_children():
		child.queue_free()

	if _bridge and _bridge.has_method("GetMilestonesV0"):
		var milestones: Array = _bridge.call("GetMilestonesV0")
		for m in milestones:
			var card := _make_milestone_card(m)
			_card_grid.add_child(card)

	# Lifetime stats.
	for child in _stats_vbox.get_children():
		child.queue_free()

	if _bridge and _bridge.has_method("GetLifetimeStatsV0"):
		var stats: Dictionary = _bridge.call("GetLifetimeStatsV0")
		_add_stat_row("Systems Visited", str(stats.get("nodes_visited", 0)))
		_add_stat_row("Goods Traded", str(stats.get("goods_traded", 0)))
		_add_stat_row("Credits Earned", _format_number(stats.get("total_credits_earned", 0)))
		_add_stat_row("Techs Unlocked", str(stats.get("techs_unlocked", 0)))
		_add_stat_row("Missions Done", str(stats.get("missions_completed", 0)))
		_add_stat_row("Fleets Destroyed", str(stats.get("npc_fleets_destroyed", 0)))
		_add_stat_row("Milestones", str(stats.get("milestones_achieved", 0)))
		_add_stat_row("Ticks Elapsed", str(stats.get("tick", 0)))


func _make_milestone_card(m: Dictionary) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(200, 100)

	var achieved: bool = m.get("achieved", false)
	var style := StyleBoxFlat.new()
	if achieved:
		style.bg_color = Color(0.08, 0.15, 0.12, 0.9)
		style.border_color = Color(0.3, 0.7, 0.4, 0.7)
	else:
		style.bg_color = Color(0.06, 0.06, 0.1, 0.7)
		style.border_color = Color(0.15, 0.15, 0.25, 0.5)
	style.set_border_width_all(1)
	style.set_corner_radius_all(6)
	style.set_content_margin_all(10)
	panel.add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)
	panel.add_child(vbox)

	var name_label := Label.new()
	var milestone_name: String = m.get("name", "Unknown")
	name_label.text = milestone_name if achieved else "???"
	name_label.add_theme_font_size_override("font_size", 16)
	if achieved:
		name_label.add_theme_color_override("font_color", Color(0.85, 0.95, 0.85))
	else:
		name_label.add_theme_color_override("font_color", Color(0.4, 0.4, 0.5))
	name_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(name_label)

	var progress_label := Label.new()
	var current: int = m.get("current", 0)
	var threshold: int = m.get("threshold", 1)
	if achieved:
		progress_label.text = "Achieved"
		progress_label.add_theme_color_override("font_color", Color(0.4, 0.8, 0.5))
	else:
		progress_label.text = "%d / %d" % [current, threshold]
		progress_label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
	progress_label.add_theme_font_size_override("font_size", 13)
	vbox.add_child(progress_label)

	return panel


func _add_stat_row(label_text: String, value_text: String) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 8)

	var label := Label.new()
	label.text = label_text
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.add_theme_font_size_override("font_size", 15)
	label.add_theme_color_override("font_color", Color(0.55, 0.6, 0.75))
	hbox.add_child(label)

	var value := Label.new()
	value.text = value_text
	value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	value.add_theme_font_size_override("font_size", 15)
	value.add_theme_color_override("font_color", Color(0.85, 0.9, 1.0))
	hbox.add_child(value)

	_stats_vbox.add_child(hbox)


func _format_number(val) -> String:
	var s := str(int(val))
	var result := ""
	var count := 0
	for i in range(s.length() - 1, -1, -1):
		if count > 0 and count % 3 == 0:
			result = "," + result
		result = s[i] + result
		count += 1
	return result


func _on_back() -> void:
	queue_free()
