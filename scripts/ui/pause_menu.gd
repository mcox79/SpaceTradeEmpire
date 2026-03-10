extends Control

# GATE.S7.MAIN_MENU.PAUSE.001: Pause menu overlay with auto-save.
# Programmatic UI — no .tscn dependency. Shown on Escape key via game_manager.gd.
# process_mode ALWAYS so buttons work while tree is paused.

const MAIN_MENU_SCENE := "res://scenes/main_menu.tscn"
const SettingsPanel = preload("res://scripts/ui/settings_panel.gd")

signal resumed  # Emitted when player clicks Resume (game_manager listens to unpause).

var _btn_resume: Button
var _btn_save: Button
var _btn_settings: Button
var _btn_quit_menu: Button
var _settings_panel = null  # Lazy-created on first Settings click.

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false

	# Semi-transparent dark overlay covering the full screen.
	var bg := ColorRect.new()
	bg.color = UITheme.PANEL_BG_OVERLAY
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_STOP  # Block clicks through to game.
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
	title.text = "PAUSED"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", UITheme.FONT_HUD_LARGE)
	title.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	vbox.add_child(title)

	# Spacer.
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 24)
	vbox.add_child(spacer)

	# Buttons.
	_btn_resume = _make_button("Resume", vbox)
	_btn_save = _make_button("Save", vbox)
	_btn_settings = _make_button("Settings", vbox)
	_btn_quit_menu = _make_button("Quit to Menu", vbox)

	_btn_resume.pressed.connect(_on_resume)
	_btn_save.pressed.connect(_on_save)
	_btn_settings.pressed.connect(_on_settings)
	_btn_quit_menu.pressed.connect(_on_quit_to_menu)


func _make_button(text: String, parent: Node) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(280, 50)
	btn.add_theme_font_size_override("font_size", 22)
	btn.process_mode = Node.PROCESS_MODE_ALWAYS
	parent.add_child(btn)
	return btn


## Called by game_manager when Escape is pressed to show the overlay.
func open_v0() -> void:
	visible = true
	_auto_save_v0()
	print("UUIR|PAUSE_MENU|SHOW")


## Called by game_manager on Resume to hide the overlay.
func close_v0() -> void:
	visible = false
	print("UUIR|PAUSE_MENU|HIDE")


func _auto_save_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("RequestSave"):
		bridge.call("RequestSave")
		print("UUIR|PAUSE_AUTOSAVE|OK")


func _on_resume() -> void:
	resumed.emit()


func _on_save() -> void:
	_auto_save_v0()


func _on_settings() -> void:
	# GATE.S9.SETTINGS.INFRASTRUCTURE.001: Lazy-create settings panel and toggle.
	if _settings_panel == null:
		_settings_panel = SettingsPanel.new()
		_settings_panel.name = "SettingsPanel"
		# Add to scene root so it persists above the pause menu overlay.
		get_tree().root.add_child(_settings_panel)
	_settings_panel.toggle_v0()


func _on_quit_to_menu() -> void:
	# Unpause so the main menu scene is not stuck paused.
	get_tree().paused = false
	get_tree().change_scene_to_file(MAIN_MENU_SCENE)
