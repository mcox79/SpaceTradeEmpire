# scripts/ui/captain_name_overlay.gd
# In-context captain name input. Ship's registry framing.
# Appears after galaxy cinematic, before FO selection.
# Headless: auto-confirms "Commander" after 1 frame.
extends CanvasLayer

signal name_confirmed(captain_name: String)

const LAYER := 201  # Above intro_sequence (199) and galaxy overlay (200)
const DEFAULT_NAME := "Commander"
const MAX_NAME_LEN := 32

var _line_edit: LineEdit
var _bg: ColorRect  # Root visual — fade target (CanvasLayer has no modulate).
var _center: CenterContainer
var _is_headless := false


func _ready() -> void:
	layer = LAYER
	_is_headless = DisplayServer.get_name() == "headless"
	_build_ui()

	if _is_headless:
		name_confirmed.emit(DEFAULT_NAME)
		queue_free()


func _build_ui() -> void:
	# Dim background.
	_bg = ColorRect.new()
	_bg.color = Color(0.0, 0.0, 0.03, 0.88)
	_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_bg)

	# Centered layout.
	_center = CenterContainer.new()
	_center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_center)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 14)
	vbox.custom_minimum_size = Vector2(400, 0)
	_center.add_child(vbox)

	# Header.
	var header := Label.new()
	header.text = "SHIP'S REGISTRY"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 22)
	header.add_theme_color_override("font_color", Color(0.7, 0.8, 0.95))
	vbox.add_child(header)

	# Subtext.
	var subtext := Label.new()
	subtext.text = "Docking clearance requires captain identification."
	subtext.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtext.add_theme_font_size_override("font_size", 14)
	subtext.add_theme_color_override("font_color", Color(0.5, 0.5, 0.55))
	subtext.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(subtext)

	var sep := HSeparator.new()
	sep.add_theme_constant_override("separation", 8)
	vbox.add_child(sep)

	# Name label.
	var name_label := Label.new()
	name_label.text = "Captain name:"
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.add_theme_font_size_override("font_size", 16)
	name_label.add_theme_color_override("font_color", Color(0.6, 0.65, 0.7))
	vbox.add_child(name_label)

	# Input.
	_line_edit = LineEdit.new()
	_line_edit.text = DEFAULT_NAME
	_line_edit.placeholder_text = "Enter captain name"
	_line_edit.max_length = MAX_NAME_LEN
	_line_edit.custom_minimum_size = Vector2(280, 40)
	_line_edit.alignment = HORIZONTAL_ALIGNMENT_CENTER
	_line_edit.add_theme_font_size_override("font_size", 18)
	_line_edit.text_submitted.connect(func(_t): _on_confirm())
	vbox.add_child(_line_edit)

	# Confirm button.
	var btn := Button.new()
	btn.text = "Confirm"
	btn.custom_minimum_size = Vector2(180, 44)
	btn.add_theme_font_size_override("font_size", 18)

	var btn_style := StyleBoxFlat.new()
	btn_style.bg_color = Color(0.08, 0.1, 0.18, 0.85)
	btn_style.border_color = Color(0.3, 0.45, 0.7, 0.7)
	btn_style.set_border_width_all(1)
	btn_style.set_corner_radius_all(5)
	btn_style.set_content_margin_all(8)
	btn.add_theme_stylebox_override("normal", btn_style)

	var btn_hover := btn_style.duplicate()
	btn_hover.bg_color = Color(0.12, 0.16, 0.3, 0.9)
	btn_hover.border_color = Color(0.4, 0.55, 0.85, 0.9)
	btn.add_theme_stylebox_override("hover", btn_hover)

	btn.pressed.connect(_on_confirm)
	vbox.add_child(btn)


func _on_confirm() -> void:
	var captain_name: String = _line_edit.text.strip_edges()
	if captain_name.is_empty():
		captain_name = DEFAULT_NAME
	# Fade out child controls (CanvasLayer has no modulate).
	var t := create_tween()
	t.tween_property(_bg, "modulate:a", 0.0, 0.3)
	t.parallel().tween_property(_center, "modulate:a", 0.0, 0.3)
	t.tween_callback(func():
		name_confirmed.emit(captain_name)
		queue_free()
	)
