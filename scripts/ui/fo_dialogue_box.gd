# scripts/ui/fo_dialogue_box.gd
# JRPG-style dialogue box at screen bottom. Shows FO portrait + name + text.
# Typewriter effect, click/Space/Enter to advance. Blocks game input while visible.
#
# Usage:
#   var box = FODialogueBox.new()
#   add_child(box)
#   box.show_line("Maren", Color(0.4, 0.6, 1.0), "Controls are live...")
#   await box.line_dismissed
extends CanvasLayer

signal line_dismissed  # Emitted when player advances past the current line.

const LAYER := 105  # Above HUD (90), below welcome overlay (110)
const BOX_HEIGHT := 130
const PORTRAIT_SIZE := 80
const MARGIN := 20
const TYPEWRITER_CPS := 40  # Characters per second (0 = instant)
const SLIDE_DURATION := 0.25

# Portrait color tints per FO type.
const PORTRAIT_COLORS := {
	"Analyst": Color(0.4, 0.6, 1.0),    # Blue
	"Veteran": Color(1.0, 0.8, 0.3),    # Amber
	"Pathfinder": Color(0.3, 0.9, 0.5), # Green
}

var _panel: PanelContainer
var _portrait_rect: ColorRect
var _name_label: Label
var _text_label: RichTextLabel
var _advance_label: Label

var _full_text := ""
var _visible_chars := 0
var _typing := false
var _waiting_for_advance := false
var _is_headless := false
var _active_tween: Tween = null

# GATE.T51.VO.DIALOGUE_WIRE.001: VO playback state.
var _vo_duration := 0.0  # Duration of current VO clip (0 = no VO)
var _vo_cps := 0  # Adjusted CPS when VO is playing (synced to audio duration)

func _ready() -> void:
	layer = LAYER
	_is_headless = DisplayServer.get_name() == "headless"
	_build_ui()
	_panel.visible = false

func _build_ui() -> void:
	# Semi-transparent dark background panel.
	_panel = PanelContainer.new()
	_panel.name = "DialoguePanel"

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.05, 0.1, 0.92)
	style.border_color = Color(0.3, 0.5, 0.8, 0.6)
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	style.set_content_margin_all(12)
	_panel.add_theme_stylebox_override("panel", style)

	# Position at screen bottom, centered ~80% width.
	_panel.anchor_left = 0.1
	_panel.anchor_right = 0.9
	_panel.anchor_top = 1.0
	_panel.anchor_bottom = 1.0
	_panel.offset_top = -BOX_HEIGHT - MARGIN
	_panel.offset_bottom = -MARGIN

	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 12)
	_panel.add_child(hbox)

	# Portrait placeholder (colored rect with initials).
	_portrait_rect = ColorRect.new()
	_portrait_rect.custom_minimum_size = Vector2(PORTRAIT_SIZE, PORTRAIT_SIZE)
	_portrait_rect.color = Color(0.3, 0.5, 0.8)
	hbox.add_child(_portrait_rect)

	# Right side: name + text + advance indicator.
	var vbox := VBoxContainer.new()
	vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	vbox.add_theme_constant_override("separation", 4)
	hbox.add_child(vbox)

	_name_label = Label.new()
	_name_label.add_theme_font_size_override("font_size", 16)
	_name_label.add_theme_color_override("font_color", Color(0.7, 0.9, 1.0))
	vbox.add_child(_name_label)

	_text_label = RichTextLabel.new()
	_text_label.bbcode_enabled = false
	_text_label.fit_content = true
	_text_label.scroll_active = false
	_text_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_text_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_text_label.add_theme_font_size_override("normal_font_size", 14)
	_text_label.add_theme_color_override("default_color", Color(0.9, 0.9, 0.85))
	vbox.add_child(_text_label)

	_advance_label = Label.new()
	_advance_label.text = "[Space to continue] \u25bc"
	_advance_label.add_theme_font_size_override("font_size", 11)
	_advance_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.5, 0.7))
	_advance_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_advance_label.visible = false
	vbox.add_child(_advance_label)

	add_child(_panel)


## Show a single dialogue line with typewriter effect.
## fo_name: "Maren", "Dask", or "Lira"
## portrait_color: color for the portrait rect
## text: the dialogue line
## vo_key: optional VO lookup key (empty = no VO)
## vo_speaker: optional speaker for VO lookup (empty = no VO)
## vo_sequence: optional sequence index for multi-line VO
func show_line(fo_name: String, portrait_color: Color, text: String,
		vo_key: String = "", vo_speaker: String = "", vo_sequence: int = 0) -> void:
	_name_label.text = fo_name.to_upper()
	_portrait_rect.color = portrait_color
	_full_text = text
	_visible_chars = 0
	_typing = true
	_waiting_for_advance = false
	_advance_label.visible = false
	_text_label.text = ""
	_vo_duration = 0.0
	_vo_cps = 0

	# Kill any pending dismiss tween (its callback would hide the panel after us).
	if _active_tween and _active_tween.is_valid():
		_active_tween.kill()
		_active_tween = null

	# GATE.T51.VO.DIALOGUE_WIRE.001: Try to play VO if key provided.
	if not vo_key.is_empty() and not vo_speaker.is_empty() and not _is_headless:
		var vo_lookup = get_node_or_null("/root/VOLookup")
		if vo_lookup:
			var stream = vo_lookup.lookup(vo_speaker, vo_key, vo_sequence)
			if stream:
				var music_mgr = get_node_or_null("/root/MusicManager")
				if music_mgr and music_mgr.has_method("play_vo"):
					_vo_duration = music_mgr.play_vo(stream)
					# Sync typewriter speed to VO duration.
					if _vo_duration > 0.0 and _full_text.length() > 0:
						_vo_cps = int(ceil(float(_full_text.length()) / _vo_duration))

	# Slide in.
	_panel.visible = true
	if not _is_headless:
		_panel.modulate.a = 0.0
		_active_tween = create_tween()
		_active_tween.tween_property(_panel, "modulate:a", 1.0, SLIDE_DURATION)

	# In headless, show full text immediately.
	if _is_headless:
		_text_label.text = _full_text
		_visible_chars = _full_text.length()
		_typing = false
		_waiting_for_advance = true
		_advance_label.visible = true


## Show a line using the FO type name to auto-resolve portrait color.
## vo_key/vo_speaker/vo_sequence are passed through to show_line for VO playback.
func show_line_by_type(fo_type: String, fo_name: String, text: String,
		vo_key: String = "", vo_speaker: String = "", vo_sequence: int = 0) -> void:
	var color: Color = PORTRAIT_COLORS.get(fo_type, Color(0.5, 0.5, 0.5))
	show_line(fo_name, color, text, vo_key, vo_speaker, vo_sequence)


## Returns true if the dialogue box is visible and waiting for player to advance.
func is_waiting_for_advance() -> bool:
	return _waiting_for_advance


## Programmatically advance (for bots).
func advance_dialogue() -> void:
	if _typing:
		# Skip to full text.
		_text_label.text = _full_text
		_visible_chars = _full_text.length()
		_typing = false
		_waiting_for_advance = true
		_advance_label.visible = true
	elif _waiting_for_advance:
		_dismiss()


## Hide the dialogue box with slide-out animation.
func hide_box() -> void:
	_dismiss()


func _process(delta: float) -> void:
	if not _typing:
		return
	# Typewriter effect.
	var cps := TYPEWRITER_CPS
	# GATE.T51.VO.DIALOGUE_WIRE.001: Use VO-synced CPS when available.
	if _vo_cps > 0:
		cps = _vo_cps
	if _is_headless:
		cps = 0  # Instant
	if cps <= 0:
		_visible_chars = _full_text.length()
	else:
		_visible_chars += int(cps * delta) + 1  # +1 ensures at least 1 char per frame
	if _visible_chars >= _full_text.length():
		_visible_chars = _full_text.length()
		_typing = false
		_waiting_for_advance = true
		_advance_label.visible = true
	_text_label.text = _full_text.left(_visible_chars)


func _unhandled_input(event: InputEvent) -> void:
	if not _panel.visible:
		return
	# Only Space, Enter, or mouse click advance/dismiss dialogue.
	# Other keys (WASD etc.) are consumed but do NOT advance — prevents
	# accidental dismissal while flying.
	if event is InputEventKey and event.pressed:
		get_viewport().set_input_as_handled()
		var dominated: bool = event.keycode in [KEY_SPACE, KEY_ENTER, KEY_KP_ENTER]
		if dominated:
			if _typing:
				advance_dialogue()
			elif _waiting_for_advance:
				_dismiss()
	elif event is InputEventMouseButton and event.pressed:
		get_viewport().set_input_as_handled()
		if _typing:
			advance_dialogue()
		elif _waiting_for_advance:
			_dismiss()


func _dismiss() -> void:
	_typing = false
	_waiting_for_advance = false
	_advance_label.visible = false
	if not _is_headless:
		_active_tween = create_tween()
		_active_tween.tween_property(_panel, "modulate:a", 0.0, 0.15)
		_active_tween.tween_callback(func(): _panel.visible = false)
	else:
		_panel.visible = false
	line_dismissed.emit()
