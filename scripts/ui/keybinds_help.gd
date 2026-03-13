# GATE.S11.GAME_FEEL.KEYBINDS.001: Keybinds help overlay (H key toggle).
# CanvasLayer above all game UI; shows all keybindings in a centered panel.
# Rebuilds on show to reflect current InputManager bindings.
extends CanvasLayer

var _panel: PanelContainer = null
var _vbox: VBoxContainer = null

func _ready() -> void:
	layer = 115
	visible = false
	_build_ui()
	# Refresh when bindings change.
	var input_mgr = get_node_or_null("/root/InputManager")
	if input_mgr and input_mgr.has_signal("bindings_changed"):
		input_mgr.bindings_changed.connect(_on_bindings_changed)


func toggle_v0() -> void:
	visible = not visible
	if visible:
		_rebuild_bindings()


func _on_bindings_changed(_action: String) -> void:
	if visible:
		_rebuild_bindings()


func _build_ui() -> void:
	var center := CenterContainer.new()
	center.name = "CenterWrap"
	center.anchor_left = 0.0
	center.anchor_top = 0.0
	center.anchor_right = 1.0
	center.anchor_bottom = 1.0
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(center)

	_panel = PanelContainer.new()
	_panel.name = "HelpPanel"
	_panel.custom_minimum_size = Vector2(380, 0)
	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_modal())

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 4)
	_panel.add_child(_vbox)
	center.add_child(_panel)

	_rebuild_bindings()


func _rebuild_bindings() -> void:
	# Clear existing rows.
	for child in _vbox.get_children():
		child.queue_free()

	# Title.
	var title := Label.new()
	title.text = "=== CONTROLS ==="
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	_vbox.add_child(title)

	_vbox.add_child(HSeparator.new())

	var input_mgr = get_node_or_null("/root/InputManager")

	# Build entries from InputManager if available, else show static fallback.
	var entries: Array = []
	if input_mgr:
		var action_order := [
			"ship_thrust_fwd", "ship_thrust_back", "ship_turn_left", "ship_turn_right",
			"combat_fire_primary", "combat_fire_secondary", "combat_target_nearest",
			"ui_galaxy_map", "ui_dock_confirm", "ui_empire_dashboard",
			"ui_mission_journal", "ui_knowledge_web", "ui_combat_log",
			"ui_data_overlay", "ui_keybinds_help", "ui_pause",
		]
		for action in action_order:
			if not input_mgr.ACTION_LABELS.has(action):
				continue
			var kb: String = input_mgr.get_action_label(action)
			var gp: String = input_mgr.get_action_gamepad_label(action)
			var desc: String = input_mgr.ACTION_LABELS[action]
			var key_text: String = kb
			if gp != "---":
				key_text += "  /  " + gp
			entries.append([key_text, desc])
	else:
		entries = [
			["W / S", "Throttle"],
			["A / D", "Turn"],
			["LMB", "Fire (Primary)"],
			["RMB", "Fire (Secondary)"],
			["R", "Target Nearest / Restart"],
			["M", "Galaxy Map"],
			["E", "Dock / Empire"],
			["H", "Help (this panel)"],
			["J", "Mission Journal"],
			["K", "Knowledge Web"],
			["L", "Combat Log"],
			["V", "Data Overlay"],
			["Esc", "Pause"],
		]

	# Add mouse actions (not remappable).
	entries.append(["Left Click", "Select / Autopilot"])
	entries.append(["Scroll", "Zoom"])

	for entry in entries:
		var row := HBoxContainer.new()

		var key_label := Label.new()
		key_label.text = entry[0]
		key_label.custom_minimum_size = Vector2(180, 0)
		key_label.add_theme_color_override("font_color", UITheme.GOLD)
		row.add_child(key_label)

		var desc_label := Label.new()
		desc_label.text = entry[1]
		desc_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		row.add_child(desc_label)

		_vbox.add_child(row)

	_vbox.add_child(HSeparator.new())

	# Footer.
	var close_key: String = "H"
	if input_mgr:
		close_key = input_mgr.get_action_label("ui_keybinds_help")
	var footer := Label.new()
	footer.text = "Press %s to close" % close_key
	footer.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	footer.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	footer.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_vbox.add_child(footer)
