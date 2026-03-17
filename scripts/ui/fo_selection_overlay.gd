# scripts/ui/fo_selection_overlay.gd
# Full-screen FO candidate selection overlay. Shows 3 candidate cards with
# name, description, and self-introduction quote. Player picks one.
# Replaces the welcome overlay for new games.
extends CanvasLayer

signal candidate_selected(candidate_type: String)

const LAYER := 110  # Same layer as old welcome overlay

var _bg: ColorRect
var _container: VBoxContainer
var _cards: Array = []  # Array of PanelContainer
var _is_headless := false

const CONTROLS_TEXT := "WASD Fly  ·  E Dock  ·  M Map  ·  H Help"

func _ready() -> void:
	layer = LAYER
	_is_headless = DisplayServer.get_name() == "headless"
	_build_ui()


func _build_ui() -> void:
	# Full-screen dim background.
	_bg = ColorRect.new()
	_bg.color = Color(0.0, 0.0, 0.05, 0.85)
	_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_bg)

	# Centered vertical layout.
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)

	_container = VBoxContainer.new()
	_container.add_theme_constant_override("separation", 16)
	_container.custom_minimum_size = Vector2(800, 0)
	center.add_child(_container)

	# Header.
	var header := Label.new()
	header.text = "FIRST OFFICER SELECTION"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 22)
	header.add_theme_color_override("font_color", Color(0.7, 0.8, 0.95))
	_container.add_child(header)


## Populate the selection overlay with candidate data from the bridge.
## candidates: Array of {type, name, description, quote}
## narrator_prompt: text shown above the cards
func populate(candidates: Array, narrator_prompt: String) -> void:
	# Narrator prompt.
	var prompt_label := Label.new()
	prompt_label.text = narrator_prompt
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.add_theme_font_size_override("font_size", 16)
	prompt_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4))
	prompt_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_container.add_child(prompt_label)

	# Cards in horizontal row.
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 16)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_container.add_child(hbox)

	var portrait_colors := {
		"Analyst": Color(0.4, 0.6, 1.0),
		"Veteran": Color(1.0, 0.8, 0.3),
		"Pathfinder": Color(0.3, 0.9, 0.5),
	}

	for candidate in candidates:
		var card := _build_card(candidate, portrait_colors.get(str(candidate.get("type", "")), Color(0.5, 0.5, 0.5)))
		hbox.add_child(card)
		_cards.append(card)

	# Controls hint at bottom.
	var controls := Label.new()
	controls.text = CONTROLS_TEXT
	controls.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	controls.add_theme_font_size_override("font_size", 12)
	controls.add_theme_color_override("font_color", Color(0.4, 0.4, 0.45))
	_container.add_child(controls)


func _build_card(candidate: Dictionary, accent_color: Color) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(230, 0)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.08, 0.08, 0.12, 0.9)
	style.border_color = accent_color * 0.6
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	style.set_content_margin_all(16)
	panel.add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	panel.add_child(vbox)

	# Portrait placeholder.
	var portrait := ColorRect.new()
	portrait.custom_minimum_size = Vector2(60, 60)
	portrait.color = accent_color
	portrait.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	vbox.add_child(portrait)

	# Name.
	var name_label := Label.new()
	name_label.text = str(candidate.get("name", ""))
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.add_theme_font_size_override("font_size", 18)
	name_label.add_theme_color_override("font_color", accent_color)
	vbox.add_child(name_label)

	# Description.
	var desc := Label.new()
	desc.text = str(candidate.get("description", ""))
	desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc.add_theme_font_size_override("font_size", 12)
	desc.add_theme_color_override("font_color", Color(0.7, 0.7, 0.75))
	vbox.add_child(desc)

	# Self-intro quote.
	var quote := Label.new()
	quote.text = "\"" + str(candidate.get("quote", "")) + "\""
	quote.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	quote.add_theme_font_size_override("font_size", 13)
	quote.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4))
	vbox.add_child(quote)

	# Memorable line from rotating audition (post-trade selection only).
	var memorable: String = str(candidate.get("memorable_line", ""))
	if not memorable.is_empty():
		var mem_header := Label.new()
		mem_header.text = "They said:"
		mem_header.add_theme_font_size_override("font_size", 11)
		mem_header.add_theme_color_override("font_color", Color(0.5, 0.5, 0.55))
		vbox.add_child(mem_header)

		var mem_quote := Label.new()
		mem_quote.text = "\"" + memorable + "\""
		mem_quote.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		mem_quote.add_theme_font_size_override("font_size", 12)
		mem_quote.add_theme_color_override("font_color", Color(0.8, 0.9, 1.0))
		vbox.add_child(mem_quote)

	# Select button.
	var btn := Button.new()
	btn.text = "Select"
	btn.custom_minimum_size = Vector2(0, 36)
	var candidate_type := str(candidate.get("type", ""))
	btn.pressed.connect(func(): _on_select(candidate_type))

	var btn_style := StyleBoxFlat.new()
	btn_style.bg_color = Color(0.1, 0.1, 0.15, 0.8)
	btn_style.border_color = accent_color * 0.8
	btn_style.set_border_width_all(1)
	btn_style.set_corner_radius_all(4)
	btn.add_theme_stylebox_override("normal", btn_style)

	var btn_hover := btn_style.duplicate()
	btn_hover.bg_color = accent_color * 0.3
	btn.add_theme_stylebox_override("hover", btn_hover)

	vbox.add_child(btn)

	# Store type on the panel for programmatic access.
	panel.set_meta("candidate_type", candidate_type)

	return panel


func _on_select(candidate_type: String) -> void:
	# Fade out non-selected cards.
	for card in _cards:
		if str(card.get_meta("candidate_type")) != candidate_type:
			card.modulate.a = 0.3

	# Emit selection after brief delay for visual feedback.
	if _is_headless:
		candidate_selected.emit(candidate_type)
		queue_free()
	else:
		await get_tree().create_timer(0.5).timeout
		candidate_selected.emit(candidate_type)
		# Fade out entire overlay.
		var t := create_tween()
		t.tween_property(_bg, "modulate:a", 0.0, 0.4)
		t.parallel().tween_property(_container, "modulate:a", 0.0, 0.4)
		t.tween_callback(queue_free)


## Programmatic selection for bots.
func select_candidate(candidate_type: String) -> void:
	_on_select(candidate_type)
