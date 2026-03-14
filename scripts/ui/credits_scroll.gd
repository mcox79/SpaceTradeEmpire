extends Control

# GATE.S9.CREDITS.SCROLL.001: Credits scroll overlay with scrolling text over starfield.

const SCROLL_SPEED: float = 40.0  # Pixels per second.

const CREDITS_TEXT: String = """


SPACE TRADE EMPIRE




Design & Development
Solo Developer




Engine
Godot 4.6 — Mono (C#)




AI Assistance
Claude (Anthropic)




3D Assets
Kenney Space Kit
Quaternius Low-Poly Models




Addons
PhantomCamera3D
LimboAI v1.7
3D Planet Generator
Atmosphere Shader
Starlight
Tooltips Pro




Audio
(Coming Soon)




Inspired By
Elite Dangerous
EVE Online
Freelancer
X Series




Special Thanks
The Godot Community
Open Source Contributors
Everyone who playtested




Made with love and
an unreasonable number of gates.




"""

var _scroll_label: Label
var _y_offset: float = 0.0


func _ready() -> void:
	# Full-screen dark background.
	var bg := ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.01, 0.01, 0.04, 0.97)
	add_child(bg)

	# Scrolling label — starts below screen, scrolls upward.
	_scroll_label = Label.new()
	_scroll_label.text = CREDITS_TEXT
	_scroll_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_scroll_label.set_anchors_preset(Control.PRESET_CENTER_TOP)
	_scroll_label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	_scroll_label.add_theme_font_size_override("font_size", 22)
	_scroll_label.add_theme_color_override("font_color", Color(0.75, 0.8, 0.95))
	_scroll_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_scroll_label.custom_minimum_size = Vector2(500, 0)
	add_child(_scroll_label)

	# Start below screen.
	_y_offset = get_viewport_rect().size.y


func _process(delta: float) -> void:
	_y_offset -= SCROLL_SPEED * delta
	_scroll_label.position.y = _y_offset

	# When text has scrolled fully past the top, close.
	if _y_offset < -_scroll_label.size.y - 100:
		queue_free()


func _input(event: InputEvent) -> void:
	# Skip on any key press or mouse click.
	if event is InputEventKey or event is InputEventMouseButton:
		if event.is_pressed():
			get_viewport().set_input_as_handled()
			queue_free()
