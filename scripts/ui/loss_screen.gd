# GATE.S8.WIN.LOSS_SCREEN.001: Game over screen for death/bankruptcy.
extends Control

const MAIN_MENU_SCENE := "res://scenes/main_menu.tscn"

# Loss data from SimBridge.
var _loss_reason: String = ""
var _final_credits: int = 0
var _final_tick: int = 0
var _nodes_visited: int = 0
var _missions_completed: int = 0
var _ship_class: String = ""
var _modules_installed: int = 0
var _captain_name: String = ""
var _chosen_path: String = ""
var _haven_tier: int = 0
var _revelation_count: int = 0

# UI nodes built programmatically.
var _bg: ColorRect
var _content_vbox: VBoxContainer
var _title_label: Label
var _body_label: RichTextLabel
var _stats_panel: PanelContainer
var _btn_restart: Button
var _btn_quit: Button


func _ready() -> void:
	# Fetch loss data from SimBridge.
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetLossInfoV0"):
		var info: Dictionary = bridge.call("GetLossInfoV0")
		_loss_reason = info.get("loss_reason", "death")
		_final_credits = info.get("final_credits", 0)
		_final_tick = info.get("final_tick", 0)
		_nodes_visited = info.get("nodes_visited", 0)
		_missions_completed = info.get("missions_completed", 0)
		_ship_class = info.get("ship_class", "corvette")
		_modules_installed = info.get("modules_installed", 0)
		_captain_name = info.get("captain_name", "Commander")
		_chosen_path = info.get("chosen_path", "")
		_haven_tier = info.get("haven_tier", 0)
		_revelation_count = info.get("revelation_count", 0)

	# Get epilogue text.
	var frame: Dictionary = EpilogueData.get_loss_frame(_loss_reason)
	var frame_title: String = frame.get("title", "Lost to the Void")
	var frame_body: String = frame.get("body", "")

	# Build the scene.
	_build_background()
	_build_content(frame_title, frame_body)

	# Fade entire screen in over 2 seconds.
	modulate.a = 0.0
	var fade_tween := create_tween()
	fade_tween.tween_property(self, "modulate:a", 1.0, 2.0).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)


func _build_background() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)

	# Tint varies by loss reason: deep crimson for death, dark charcoal for bankruptcy.
	var bg_color: Color
	if _loss_reason == "death":
		bg_color = Color(0.07, 0.01, 0.01, 1.0)
	else:
		bg_color = Color(0.05, 0.05, 0.06, 1.0)

	_bg = ColorRect.new()
	_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	_bg.color = bg_color
	add_child(_bg)


func _build_content(frame_title: String, frame_body: String) -> void:
	# Outer centering container.
	_content_vbox = VBoxContainer.new()
	_content_vbox.set_anchors_preset(Control.PRESET_CENTER)
	_content_vbox.grow_horizontal = Control.GROW_DIRECTION_BOTH
	_content_vbox.grow_vertical = Control.GROW_DIRECTION_BOTH
	_content_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_content_vbox.add_theme_constant_override("separation", 28)
	_content_vbox.custom_minimum_size = Vector2(700, 0)
	add_child(_content_vbox)

	# --- Title ---
	_title_label = Label.new()
	_title_label.text = frame_title.to_upper()
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", 48)
	var title_color: Color = Color(0.85, 0.3, 0.25) if _loss_reason == "death" else Color(0.6, 0.6, 0.65)
	_title_label.add_theme_color_override("font_color", title_color)
	_content_vbox.add_child(_title_label)

	# Thin separator line.
	var sep := HSeparator.new()
	sep.add_theme_color_override("color", Color(0.3, 0.2, 0.2, 0.6) if _loss_reason == "death" else Color(0.25, 0.25, 0.3, 0.6))
	_content_vbox.add_child(sep)

	# --- Body text ---
	_body_label = RichTextLabel.new()
	_body_label.bbcode_enabled = true
	_body_label.text = frame_body
	_body_label.fit_content = true
	_body_label.scroll_active = false
	_body_label.custom_minimum_size = Vector2(680, 0)
	_body_label.add_theme_font_size_override("normal_font_size", 18)
	_body_label.add_theme_color_override("default_color", Color(0.75, 0.72, 0.68))
	_content_vbox.add_child(_body_label)

	# --- Stats panel ---
	_build_stats_panel()

	# Spacer before buttons.
	var btn_spacer := Control.new()
	btn_spacer.custom_minimum_size = Vector2(0, 8)
	_content_vbox.add_child(btn_spacer)

	# --- Button row ---
	var btn_row := HBoxContainer.new()
	btn_row.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_row.add_theme_constant_override("separation", 32)
	_content_vbox.add_child(btn_row)

	_btn_restart = _make_button("Restart", btn_row)
	_btn_quit = _make_button("Quit", btn_row)

	_btn_restart.pressed.connect(_on_restart)
	_btn_quit.pressed.connect(_on_quit)


func _build_stats_panel() -> void:
	_stats_panel = PanelContainer.new()
	_stats_panel.custom_minimum_size = Vector2(680, 0)

	var panel_style := StyleBoxFlat.new()
	panel_style.bg_color = Color(0.06, 0.04, 0.04, 0.9) if _loss_reason == "death" else Color(0.05, 0.05, 0.07, 0.9)
	panel_style.border_color = Color(0.3, 0.15, 0.12, 0.7) if _loss_reason == "death" else Color(0.2, 0.2, 0.28, 0.7)
	panel_style.set_border_width_all(1)
	panel_style.set_corner_radius_all(6)
	panel_style.set_content_margin_all(20)
	_stats_panel.add_theme_stylebox_override("panel", panel_style)
	_content_vbox.add_child(_stats_panel)

	var stats_vbox := VBoxContainer.new()
	stats_vbox.add_theme_constant_override("separation", 6)
	_stats_panel.add_child(stats_vbox)

	# Panel header.
	var header := Label.new()
	header.text = "FINAL RECORD"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 14)
	header.add_theme_color_override("font_color", Color(0.45, 0.42, 0.40))
	stats_vbox.add_child(header)

	var header_sep := HSeparator.new()
	header_sep.add_theme_color_override("color", Color(0.2, 0.18, 0.16, 0.5))
	stats_vbox.add_child(header_sep)

	var top_spacer := Control.new()
	top_spacer.custom_minimum_size = Vector2(0, 4)
	stats_vbox.add_child(top_spacer)

	# Two-column grid for stats.
	var grid := GridContainer.new()
	grid.columns = 4
	grid.add_theme_constant_override("h_separation", 20)
	grid.add_theme_constant_override("v_separation", 8)
	stats_vbox.add_child(grid)

	# Helper to add a stat cell pair (label + value).
	var stat_pairs: Array[Dictionary] = [
		{"label": "Captain", "value": _captain_name},
		{"label": "Ship Class", "value": _ship_class.capitalize()},
		{"label": "Modules Installed", "value": str(_modules_installed)},
		{"label": "Credits", "value": _format_credits(_final_credits)},
		{"label": "Systems Visited", "value": str(_nodes_visited)},
		{"label": "Missions Completed", "value": str(_missions_completed)},
		{"label": "Ticks Survived", "value": str(_final_tick)},
		{"label": "Revelations", "value": str(_revelation_count)},
	]

	for pair in stat_pairs:
		var lbl := Label.new()
		lbl.text = pair["label"] + ":"
		lbl.add_theme_font_size_override("font_size", 15)
		lbl.add_theme_color_override("font_color", Color(0.5, 0.48, 0.45))
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		grid.add_child(lbl)

		var val := Label.new()
		val.text = pair["value"]
		val.add_theme_font_size_override("font_size", 15)
		val.add_theme_color_override("font_color", Color(0.88, 0.84, 0.78))
		val.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		grid.add_child(val)


func _make_button(label_text: String, parent: Node) -> Button:
	var btn := Button.new()
	btn.text = label_text
	btn.custom_minimum_size = Vector2(200, 50)
	btn.add_theme_font_size_override("font_size", 20)

	var style_normal := StyleBoxFlat.new()
	style_normal.bg_color = Color(0.08, 0.06, 0.06, 0.8)
	style_normal.border_color = Color(0.28, 0.2, 0.18, 0.7)
	style_normal.set_border_width_all(1)
	style_normal.set_corner_radius_all(6)
	style_normal.set_content_margin_all(8)
	btn.add_theme_stylebox_override("normal", style_normal)

	var style_hover := StyleBoxFlat.new()
	style_hover.bg_color = Color(0.14, 0.10, 0.10, 0.9)
	style_hover.border_color = Color(0.45, 0.30, 0.26, 0.9)
	style_hover.set_border_width_all(1)
	style_hover.set_corner_radius_all(6)
	style_hover.set_content_margin_all(8)
	btn.add_theme_stylebox_override("hover", style_hover)

	var style_pressed := StyleBoxFlat.new()
	style_pressed.bg_color = Color(0.18, 0.12, 0.12, 1.0)
	style_pressed.border_color = Color(0.55, 0.35, 0.30, 1.0)
	style_pressed.set_border_width_all(1)
	style_pressed.set_corner_radius_all(6)
	style_pressed.set_content_margin_all(8)
	btn.add_theme_stylebox_override("pressed", style_pressed)

	btn.add_theme_color_override("font_color", Color(0.80, 0.75, 0.70))
	btn.add_theme_color_override("font_hover_color", Color(0.95, 0.88, 0.82))
	parent.add_child(btn)
	return btn


func _on_restart() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	# Brief fade before scene change.
	var fade := create_tween()
	fade.tween_property(self, "modulate:a", 0.0, 0.5).set_ease(Tween.EASE_IN)
	fade.tween_callback(func():
		get_tree().change_scene_to_file(MAIN_MENU_SCENE)
	)


func _on_quit() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	get_tree().quit()


func _format_credits(amount: int) -> String:
	# Format with comma separators: 1234567 -> "1,234,567".
	var s := str(amount)
	var result := ""
	var count := 0
	for i in range(s.length() - 1, -1, -1):
		if count > 0 and count % 3 == 0:
			result = "," + result
		result = s[i] + result
		count += 1
	return result
