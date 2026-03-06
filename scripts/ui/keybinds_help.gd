# GATE.S11.GAME_FEEL.KEYBINDS.001: Keybinds help overlay (H key toggle).
# CanvasLayer above all game UI; shows all keybindings in a centered panel.
extends CanvasLayer

var _panel: PanelContainer = null

func _ready() -> void:
	layer = 115
	visible = false
	_build_ui()


func _build_ui() -> void:
	# Center container to center the panel on screen
	var center := CenterContainer.new()
	center.name = "CenterWrap"
	center.anchor_left = 0.0
	center.anchor_top = 0.0
	center.anchor_right = 1.0
	center.anchor_bottom = 1.0
	center.offset_left = 0
	center.offset_top = 0
	center.offset_right = 0
	center.offset_bottom = 0
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(center)

	_panel = PanelContainer.new()
	_panel.name = "HelpPanel"
	_panel.custom_minimum_size = Vector2(380, 0)

	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_modal())

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)

	# Title
	var title := Label.new()
	title.text = "=== CONTROLS ==="
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	vbox.add_child(title)

	vbox.add_child(HSeparator.new())

	# Keybinding entries
	var bindings := [
		["W / S", "Throttle"],
		["A / D", "Turn"],
		["G", "Fire Turret"],
		["E", "Empire Dashboard"],
		["Tab", "Galaxy Map Overlay"],
		["H", "Help (this panel)"],
		["L", "Combat Log"],
		["Esc", "Pause Menu"],
		["R", "Restart (when dead)"],
		["Left Click", "Select / Interact"],
	]

	for entry in bindings:
		var row := HBoxContainer.new()

		var key_label := Label.new()
		key_label.text = entry[0]
		key_label.custom_minimum_size = Vector2(140, 0)
		key_label.add_theme_color_override("font_color", UITheme.GOLD)
		row.add_child(key_label)

		var desc_label := Label.new()
		desc_label.text = entry[1]
		desc_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		row.add_child(desc_label)

		vbox.add_child(row)

	vbox.add_child(HSeparator.new())

	# Footer
	var footer := Label.new()
	footer.text = "Press H to close"
	footer.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	footer.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	footer.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	vbox.add_child(footer)

	_panel.add_child(vbox)
	center.add_child(_panel)
