extends Control

# GATE.S7.MAIN_MENU.SCENE.001: Main menu with New Voyage, Continue, Settings, Quit.
# Programmatic UI — no .tscn dependency for layout.

const PLAYABLE_SCENE := "res://scenes/playable_prototype.tscn"

var _btn_continue: Button
var _btn_new_voyage: Button
var _btn_settings: Button
var _btn_quit: Button
var _save_card: PanelContainer
# GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Captain name input for new voyage wizard.
var _captain_name_input: LineEdit

func _ready() -> void:
	# Full-screen dark background.
	var bg := ColorRect.new()
	bg.color = Color(0.02, 0.02, 0.06, 1.0)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	# Center container for menu items.
	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_CENTER)
	vbox.grow_horizontal = Control.GROW_DIRECTION_BOTH
	vbox.grow_vertical = Control.GROW_DIRECTION_BOTH
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_theme_constant_override("separation", 16)
	add_child(vbox)

	# Title label.
	var title := Label.new()
	title.text = "SPACE TRADE EMPIRE"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 48)
	vbox.add_child(title)

	# Spacer.
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 40)
	vbox.add_child(spacer)

	# GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Captain name input.
	var name_label := Label.new()
	name_label.text = "Captain Name"
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.add_theme_font_size_override("font_size", 18)
	name_label.add_theme_color_override("font_color", Color(0.6, 0.7, 0.85))
	vbox.add_child(name_label)

	_captain_name_input = LineEdit.new()
	_captain_name_input.text = "Commander"
	_captain_name_input.placeholder_text = "Enter captain name"
	_captain_name_input.max_length = 32
	_captain_name_input.custom_minimum_size = Vector2(280, 40)
	_captain_name_input.alignment = HORIZONTAL_ALIGNMENT_CENTER
	_captain_name_input.add_theme_font_size_override("font_size", 18)
	vbox.add_child(_captain_name_input)

	var name_spacer := Control.new()
	name_spacer.custom_minimum_size = Vector2(0, 8)
	vbox.add_child(name_spacer)

	# Buttons.
	_btn_continue = _make_button("Continue", vbox)
	_btn_new_voyage = _make_button("New Voyage", vbox)
	_btn_settings = _make_button("Settings", vbox)
	_btn_quit = _make_button("Quit", vbox)

	_btn_continue.pressed.connect(_on_continue)
	_btn_new_voyage.pressed.connect(_on_new_voyage)
	_btn_settings.pressed.connect(_on_settings)
	_btn_quit.pressed.connect(_on_quit)

	# Check if save file exists for Continue button and show metadata card.
	var bridge = get_node_or_null("/root/SimBridge")
	var has_save := false
	if bridge and bridge.has_method("GetSaveSlotMetadataV0"):
		var meta: Dictionary = bridge.call("GetSaveSlotMetadataV0", 1)
		has_save = meta.get("exists", false)
		if has_save:
			_build_save_card(vbox, meta)
	_btn_continue.disabled = not has_save

	# Tell GameManager we're on the main menu.
	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.set("_on_main_menu", true)


func _make_button(text: String, parent: Node) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(280, 50)
	btn.add_theme_font_size_override("font_size", 22)
	parent.add_child(btn)
	return btn


func _build_save_card(parent_vbox: VBoxContainer, meta: Dictionary) -> void:
	# Insert save metadata preview card right after the Continue button.
	_save_card = PanelContainer.new()
	_save_card.custom_minimum_size = Vector2(280, 0)

	# Dark semi-transparent panel style.
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.08, 0.08, 0.15, 0.85)
	style.border_color = Color(0.25, 0.3, 0.5, 0.6)
	style.set_border_width_all(1)
	style.set_corner_radius_all(6)
	style.set_content_margin_all(12)
	_save_card.add_theme_stylebox_override("panel", style)

	var card_vbox := VBoxContainer.new()
	card_vbox.add_theme_constant_override("separation", 4)
	_save_card.add_child(card_vbox)

	# Saved date row.
	var timestamp: String = meta.get("timestamp", "")
	if timestamp != "":
		var date_label := Label.new()
		date_label.text = "Saved: %s" % timestamp
		date_label.add_theme_font_size_override("font_size", 14)
		date_label.add_theme_color_override("font_color", Color(0.65, 0.7, 0.8))
		date_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		card_vbox.add_child(date_label)

	# Credits row.
	var credits: int = meta.get("credits", 0)
	if credits > 0:
		var credits_label := Label.new()
		credits_label.text = "Credits: %s" % _format_credits(credits)
		credits_label.add_theme_font_size_override("font_size", 14)
		credits_label.add_theme_color_override("font_color", Color(0.9, 0.85, 0.4))
		credits_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		card_vbox.add_child(credits_label)

	# System/location row.
	var system_name: String = meta.get("system_name", "")
	if system_name != "":
		var loc_label := Label.new()
		loc_label.text = "Location: %s" % system_name
		loc_label.add_theme_font_size_override("font_size", 14)
		loc_label.add_theme_color_override("font_color", Color(0.5, 0.75, 0.9))
		loc_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		card_vbox.add_child(loc_label)

	# Insert the card after the Continue button (index 3: bg=scene-child, title=0, spacer=1, continue=2).
	var continue_idx := _btn_continue.get_index()
	parent_vbox.add_child(_save_card)
	parent_vbox.move_child(_save_card, continue_idx + 1)


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


func _on_continue() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("RequestLoad"):
		bridge.call("RequestLoad")
	_transition_to_game()


func _on_new_voyage() -> void:
	# GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Pass captain name to SimBridge before starting.
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("SetCaptainNameV0"):
		var name_text: String = _captain_name_input.text.strip_edges()
		if name_text == "":
			name_text = "Commander"
		bridge.call("SetCaptainNameV0", name_text)
	_transition_to_game()


func _on_settings() -> void:
	# Settings not yet implemented — placeholder.
	pass


func _on_quit() -> void:
	get_tree().quit()


func _transition_to_game() -> void:
	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.set("_on_main_menu", false)
	get_tree().change_scene_to_file(PLAYABLE_SCENE)
