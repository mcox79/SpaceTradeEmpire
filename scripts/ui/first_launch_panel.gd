# GATE.S9.ACCESSIBILITY.FIRST_LAUNCH.001: First-launch accessibility prompt.
# CanvasLayer (layer 130) shown once on first run to let the player configure
# font scale and colorblind mode before gameplay begins.
# Dismissed by clicking "Continue" — sets "first_launch_shown" to true.
extends CanvasLayer

const _FONT_SCALE_VALUES := [100, 125, 150, 200]
const _COLORBLIND_NAMES := ["None", "Deuteranopia", "Protanopia", "Tritanopia"]

var _font_scale_option: OptionButton = null
var _colorblind_option: OptionButton = null


func _ready() -> void:
	layer = 130
	process_mode = Node.PROCESS_MODE_ALWAYS

	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr == null:
		queue_free()
		return

	# Only show on true first launch (no settings file existed).
	var already_shown = mgr.get_setting("first_launch_shown")
	if already_shown:
		queue_free()
		return

	visible = true
	_build_ui()
	print("UUIR|FIRST_LAUNCH_PANEL|SHOW")


func _build_ui() -> void:
	# Scrim
	var bg := ColorRect.new()
	bg.color = UITheme.PANEL_BG_OVERLAY
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(bg)

	# Center container
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(center)

	# Panel
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(460, 340)
	panel.add_theme_stylebox_override("panel", UITheme.make_panel_modal())
	center.add_child(panel)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	panel.add_child(vbox)

	# Title
	var title := Label.new()
	title.text = "ACCESSIBILITY SETTINGS"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	vbox.add_child(HSeparator.new())

	# Info text
	var info := Label.new()
	info.text = "Welcome! Configure accessibility options before you begin.\nYou can change these later in Settings > Accessibility."
	info.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	info.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	info.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(info)

	vbox.add_child(HSeparator.new())

	# Font Scale row
	var font_hbox := HBoxContainer.new()
	font_hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	vbox.add_child(font_hbox)

	var font_lbl := Label.new()
	font_lbl.text = "Font Scale"
	font_lbl.custom_minimum_size = Vector2(160, 0)
	font_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	font_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	font_hbox.add_child(font_lbl)

	_font_scale_option = OptionButton.new()
	for val in _FONT_SCALE_VALUES:
		_font_scale_option.add_item("%d%%" % val)
	_font_scale_option.selected = 0  # 100% default
	_font_scale_option.custom_minimum_size = Vector2(140, 0)
	_font_scale_option.process_mode = Node.PROCESS_MODE_ALWAYS
	font_hbox.add_child(_font_scale_option)

	# Colorblind Mode row
	var cb_hbox := HBoxContainer.new()
	cb_hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	vbox.add_child(cb_hbox)

	var cb_lbl := Label.new()
	cb_lbl.text = "Colorblind Mode"
	cb_lbl.custom_minimum_size = Vector2(160, 0)
	cb_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	cb_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	cb_hbox.add_child(cb_lbl)

	_colorblind_option = OptionButton.new()
	for cb_name in _COLORBLIND_NAMES:
		_colorblind_option.add_item(cb_name)
	_colorblind_option.selected = 0  # None default
	_colorblind_option.custom_minimum_size = Vector2(140, 0)
	_colorblind_option.process_mode = Node.PROCESS_MODE_ALWAYS
	cb_hbox.add_child(_colorblind_option)

	# UI scale info
	var scale_info := Label.new()
	scale_info.text = "Font scale uses Godot's ThemeDB to uniformly scale all UI elements."
	scale_info.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	scale_info.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	scale_info.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(scale_info)

	vbox.add_child(HSeparator.new())

	# Continue button
	var btn_hbox := HBoxContainer.new()
	btn_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_child(btn_hbox)

	var continue_btn := Button.new()
	continue_btn.text = "Continue"
	continue_btn.custom_minimum_size = Vector2(140, 40)
	continue_btn.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	continue_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	continue_btn.pressed.connect(_on_continue_pressed)
	btn_hbox.add_child(continue_btn)


func _on_continue_pressed() -> void:
	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr == null:
		queue_free()
		return

	# Apply font scale selection.
	var font_idx: int = _font_scale_option.selected if _font_scale_option else 0
	if font_idx >= 0 and font_idx < _FONT_SCALE_VALUES.size():
		mgr.set_setting("accessibility_font_scale", _FONT_SCALE_VALUES[font_idx])

	# Apply colorblind mode selection.
	var cb_idx: int = _colorblind_option.selected if _colorblind_option else 0
	mgr.set_setting("accessibility_colorblind_mode", cb_idx)

	# Mark as shown so it never appears again.
	mgr.set_setting("first_launch_shown", true)

	print("UUIR|FIRST_LAUNCH_PANEL|DISMISS|font=%d|colorblind=%d" % [
		_FONT_SCALE_VALUES[font_idx] if font_idx >= 0 and font_idx < _FONT_SCALE_VALUES.size() else 100,
		cb_idx
	])
	queue_free()
