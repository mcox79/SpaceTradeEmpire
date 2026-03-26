# scripts/ui/intro_sequence.gd
# Pre-tutorial intro orchestrator. CanvasLayer at layer 199.
# Sequence: Ship Computer dialogue (cold open) → Galaxy cinematic → Captain name input.
# Emits intro_complete(captain_name) when finished.
# Headless: skips everything, emits immediately with "Commander".
extends CanvasLayer

signal intro_complete(captain_name: String)

const GalaxyIntroOverlay = preload("res://scripts/ui/galaxy_intro_overlay.gd")
const CaptainNameOverlay = preload("res://scripts/ui/captain_name_overlay.gd")

const LAYER := 199
const TYPEWRITER_CPS := 35  # Characters per second for ship computer text
const SETTLE_AFTER_GALAXY := 0.5

# Ship Computer cold open lines. Mundane, grounded, no lore spoilers.
const COMPUTER_LINES: Array[String] = [
	"Route contract terminated. Syndicate restructuring cited. Current position: outer frontier.",
	"Fuel reserves adequate. Cargo hold empty. Credit balance: critical.",
	"Three officers have responded to the open First Officer posting.",
	"Nearest station ahead. Recommend docking for resupply and crew review.",
]

var _bg: ColorRect
var _dialogue_panel: PanelContainer
var _speaker_label: Label
var _text_label: RichTextLabel
var _advance_label: Label

var _is_headless := false
var _full_text := ""
var _visible_chars := 0
var _typing := false
var _waiting_for_advance := false
var _line_advanced := false  # Set true when player advances current line


func _ready() -> void:
	layer = LAYER
	name = "IntroSequence"
	_is_headless = DisplayServer.get_name() == "headless"

	if _is_headless:
		intro_complete.emit("Commander")
		queue_free()
		return

	_build_ui()


func start() -> void:
	if _is_headless:
		return  # Already emitted in _ready
	await _run_sequence()


func _build_ui() -> void:
	# Full-screen black background (prevents game world flash).
	_bg = ColorRect.new()
	_bg.color = Color(0.0, 0.0, 0.02, 1.0)
	_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_bg)

	# Ship computer dialogue panel at bottom of screen.
	_dialogue_panel = PanelContainer.new()
	_dialogue_panel.visible = false

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.03, 0.04, 0.08, 0.95)
	style.border_color = Color(0.3, 0.35, 0.45, 0.5)
	style.set_border_width_all(1)
	style.set_corner_radius_all(4)
	style.set_content_margin_all(14)
	_dialogue_panel.add_theme_stylebox_override("panel", style)

	# Position at bottom, centered 80% width.
	_dialogue_panel.anchor_left = 0.1
	_dialogue_panel.anchor_right = 0.9
	_dialogue_panel.anchor_top = 1.0
	_dialogue_panel.anchor_bottom = 1.0
	_dialogue_panel.offset_top = -120
	_dialogue_panel.offset_bottom = -20

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)
	_dialogue_panel.add_child(vbox)

	# Speaker name.
	_speaker_label = Label.new()
	_speaker_label.text = "SHIP COMPUTER"
	_speaker_label.add_theme_font_size_override("font_size", 14)
	_speaker_label.add_theme_color_override("font_color", Color(0.5, 0.55, 0.65))
	vbox.add_child(_speaker_label)

	# Text body.
	_text_label = RichTextLabel.new()
	_text_label.bbcode_enabled = false
	_text_label.fit_content = true
	_text_label.scroll_active = false
	_text_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_text_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_text_label.add_theme_font_size_override("normal_font_size", 15)
	_text_label.add_theme_color_override("default_color", Color(0.75, 0.8, 0.7))
	vbox.add_child(_text_label)

	# Advance hint.
	_advance_label = Label.new()
	_advance_label.text = "[Space to continue] \u25bc"
	_advance_label.add_theme_font_size_override("font_size", 11)
	_advance_label.add_theme_color_override("font_color", Color(0.45, 0.45, 0.4, 0.6))
	_advance_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_advance_label.visible = false
	vbox.add_child(_advance_label)

	add_child(_dialogue_panel)


func _run_sequence() -> void:
	# Phase A: Ship Computer cold open lines.
	for line in COMPUTER_LINES:
		await _show_computer_line(line)

	# Hide dialogue, brief pause.
	_dialogue_panel.visible = false
	await get_tree().create_timer(0.3).timeout

	# Phase B: Galaxy cinematic.
	var galaxy_overlay = GalaxyIntroOverlay.new()
	var parent_b = get_parent()
	if parent_b == null:
		return
	parent_b.add_child(galaxy_overlay)  # Sibling, not child (layer 200)
	await galaxy_overlay.play_intro()
	galaxy_overlay.queue_free()

	# Settle after galaxy fades.
	await get_tree().create_timer(SETTLE_AFTER_GALAXY).timeout

	# Phase C: Captain name input.
	# Hide intro bg — captain_name_overlay (layer 201) has its own dark background.
	_bg.visible = false

	var name_overlay = CaptainNameOverlay.new()
	name_overlay.name = "CaptainNameOverlay"
	var parent_c = get_parent()
	if parent_c == null:
		return
	parent_c.add_child(name_overlay)

	var captain_name: String = await name_overlay.name_confirmed

	# Clean up and emit.
	intro_complete.emit(captain_name)
	queue_free()


func _show_computer_line(text: String) -> void:
	_full_text = text
	_visible_chars = 0
	_typing = true
	_waiting_for_advance = false
	_line_advanced = false
	_advance_label.visible = false
	_text_label.text = ""
	_dialogue_panel.visible = true

	# Wait for typewriter to finish and player to advance.
	while not _line_advanced:
		await get_tree().process_frame


func _process(delta: float) -> void:
	if not _typing:
		return
	# Typewriter effect.
	_visible_chars += int(TYPEWRITER_CPS * delta) + 1
	if _visible_chars >= _full_text.length():
		_visible_chars = _full_text.length()
		_typing = false
		_waiting_for_advance = true
		_advance_label.visible = true
	_text_label.text = _full_text.left(_visible_chars)


func _unhandled_input(event: InputEvent) -> void:
	if not _dialogue_panel.visible:
		return
	if event is InputEventKey and event.pressed:
		get_viewport().set_input_as_handled()
		# Only Space/Enter advance — WASD consumed but ignored.
		var dominated: bool = event.keycode in [KEY_SPACE, KEY_ENTER, KEY_KP_ENTER]
		if dominated:
			if _typing:
				_text_label.text = _full_text
				_visible_chars = _full_text.length()
				_typing = false
				_waiting_for_advance = true
				_advance_label.visible = true
			elif _waiting_for_advance:
				_waiting_for_advance = false
				_advance_label.visible = false
				_line_advanced = true
	elif event is InputEventMouseButton and event.pressed:
		get_viewport().set_input_as_handled()
		if _typing:
			_text_label.text = _full_text
			_visible_chars = _full_text.length()
			_typing = false
			_waiting_for_advance = true
			_advance_label.visible = true
		elif _waiting_for_advance:
			_waiting_for_advance = false
			_advance_label.visible = false
			_line_advanced = true
