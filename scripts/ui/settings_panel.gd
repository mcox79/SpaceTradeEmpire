# GATE.S9.SETTINGS.INFRASTRUCTURE.001: Settings panel — modal overlay with tabbed content.
# CanvasLayer (layer 120) so it renders above all game UI including pause menu.
# Programmatic UI — no .tscn dependency.
# process_mode ALWAYS so it works while tree is paused.
# Toggle with toggle_v0(). Escape dismisses.
extends CanvasLayer

# GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001: added ACCESSIBILITY tab
enum Tab { AUDIO, DISPLAY, GAMEPLAY, ACCESSIBILITY }

## Maps settings keys to AudioServer bus names.
const AUDIO_BUS_MAP := {
	"audio_master": "Master",
	"audio_music": "Music",
	"audio_sfx": "SFX",
	"audio_ambient": "Ambient",
	"audio_ui": "UI",
}

var _panel: PanelContainer = null
var _bg: ColorRect = null
var _tab_buttons: Array[Button] = []
var _content_container: VBoxContainer = null
var _current_tab: Tab = Tab.AUDIO

# GATE.S9.SETTINGS.DISPLAY_REVERT.001: display revert confirmation state
var _revert_overlay: Control = null
var _revert_countdown_label: Label = null
var _revert_timer: float = -1.0  # negative = inactive
var _revert_previous_display_mode: int = 0
const _REVERT_TIMEOUT := 15.0


func _ready() -> void:
	layer = 120
	visible = false
	process_mode = Node.PROCESS_MODE_ALWAYS
	_build_ui()
	_apply_all_settings()
	# Listen for external changes (e.g. reset from another panel).
	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr and mgr.has_signal("settings_changed"):
		mgr.settings_changed.connect(_on_settings_changed)


func _unhandled_input(event: InputEvent) -> void:
	if not visible:
		return
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		toggle_v0()
		get_viewport().set_input_as_handled()


## Show / hide the settings panel. Refreshes content on show.
func toggle_v0() -> void:
	visible = not visible
	if visible:
		_switch_tab(Tab.AUDIO)
		print("UUIR|SETTINGS_PANEL|SHOW")
	else:
		print("UUIR|SETTINGS_PANEL|HIDE")


# ── UI Construction ─────────────────────────────────────────────────────────

func _build_ui() -> void:
	# Semi-transparent scrim covering full screen.
	_bg = ColorRect.new()
	_bg.color = UITheme.PANEL_BG_OVERLAY
	_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	_bg.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_bg)

	# Center container wrapping the panel.
	var center := CenterContainer.new()
	center.name = "CenterWrap"
	center.anchor_left = 0.0
	center.anchor_top = 0.0
	center.anchor_right = 1.0
	center.anchor_bottom = 1.0
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(center)

	# Main panel.
	_panel = PanelContainer.new()
	_panel.name = "SettingsPanel"
	_panel.custom_minimum_size = Vector2(520, 400)
	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_modal())
	center.add_child(_panel)

	var outer_vbox := VBoxContainer.new()
	outer_vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	_panel.add_child(outer_vbox)

	# ── Header row: title + close button ──
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_MD)
	outer_vbox.add_child(header)

	var title := Label.new()
	title.text = "SETTINGS"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(title)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.custom_minimum_size = Vector2(36, 36)
	close_btn.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	close_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	close_btn.pressed.connect(toggle_v0)
	header.add_child(close_btn)

	outer_vbox.add_child(HSeparator.new())

	# ── Tab bar ──
	var tab_bar := HBoxContainer.new()
	tab_bar.add_theme_constant_override("separation", UITheme.SPACE_SM)
	outer_vbox.add_child(tab_bar)

	var tab_names := ["Audio", "Display", "Gameplay", "Accessibility"]
	for i in range(tab_names.size()):
		var btn := Button.new()
		btn.text = tab_names[i]
		btn.custom_minimum_size = Vector2(120, 36)
		btn.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		btn.process_mode = Node.PROCESS_MODE_ALWAYS
		btn.pressed.connect(_on_tab_pressed.bind(i))
		tab_bar.add_child(btn)
		_tab_buttons.append(btn)

	outer_vbox.add_child(HSeparator.new())

	# ── Content area (rebuilt on tab switch) ──
	_content_container = VBoxContainer.new()
	_content_container.name = "TabContent"
	_content_container.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_content_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	outer_vbox.add_child(_content_container)

	# ── Footer: reset defaults button ──
	outer_vbox.add_child(HSeparator.new())

	var footer := HBoxContainer.new()
	outer_vbox.add_child(footer)

	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	footer.add_child(spacer)

	var reset_btn := Button.new()
	reset_btn.text = "Reset Defaults"
	reset_btn.custom_minimum_size = Vector2(140, 32)
	reset_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	reset_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	reset_btn.pressed.connect(_on_reset_defaults)
	footer.add_child(reset_btn)


# ── Tab switching ────────────────────────────────────────────────────────────

func _on_tab_pressed(index: int) -> void:
	_switch_tab(index as Tab)


func _switch_tab(tab: Tab) -> void:
	_current_tab = tab
	# Highlight active tab button.
	for i in range(_tab_buttons.size()):
		var btn: Button = _tab_buttons[i]
		if i == int(tab):
			btn.add_theme_color_override("font_color", UITheme.CYAN)
		else:
			btn.remove_theme_color_override("font_color")

	# Clear old content.
	for child in _content_container.get_children():
		child.queue_free()

	# Build new content.
	match tab:
		Tab.AUDIO:
			_build_audio_tab()
		Tab.DISPLAY:
			_build_display_tab()
		Tab.GAMEPLAY:
			_build_gameplay_tab()
		Tab.ACCESSIBILITY:
			_build_accessibility_tab()


# ── Audio tab ────────────────────────────────────────────────────────────────

func _build_audio_tab() -> void:
	_add_slider_row("Master Volume", "audio_master", 0.0, 1.0)
	_add_slider_row("Music Volume", "audio_music", 0.0, 1.0)
	_add_slider_row("SFX Volume", "audio_sfx", 0.0, 1.0)
	_add_slider_row("Ambient Volume", "audio_ambient", 0.0, 1.0)
	_add_slider_row("UI Volume", "audio_ui", 0.0, 1.0)


# ── Display tab ──────────────────────────────────────────────────────────────

func _build_display_tab() -> void:
	# GATE.S9.SETTINGS.DISPLAY_REVERT.001: display mode uses revert confirmation
	_add_display_mode_row()
	_add_toggle_row("VSync", "display_vsync")
	_add_option_row("Quality", "display_quality", ["Low", "Medium", "High"])
	_add_info_row("Resolution changes require restart.")


# ── Gameplay tab ─────────────────────────────────────────────────────────────

func _build_gameplay_tab() -> void:
	_add_info_row("Difficulty: Normal (fixed in current build)")
	_add_toggle_row("Auto-Pause on Focus Loss", "gameplay_auto_pause")
	_add_toggle_row("Tutorial Toasts", "gameplay_tutorial_toasts")
	_add_slider_row("Tooltip Delay (sec)", "gameplay_tooltip_delay", 0.0, 3.0)
	_add_slider_row("Camera Sensitivity", "gameplay_camera_sensitivity", 0.5, 3.0)


# ── Accessibility tab ────────────────────────────────────────────────────────
# GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001

func _build_accessibility_tab() -> void:
	_add_option_row("Colorblind Mode", "accessibility_colorblind_mode",
		["None", "Deuteranopia", "Protanopia", "Tritanopia"])
	_add_font_scale_row()
	_add_toggle_row("High Contrast", "accessibility_high_contrast")
	_add_toggle_row("Reduced Screen Shake", "accessibility_reduced_shake")
	_add_info_row("Font scale changes apply to all UI elements globally.")


## GATE.S9.ACCESSIBILITY.FONT_SCALE.001: font scale uses discrete percentage values.
const _FONT_SCALE_VALUES := [100, 125, 150, 200]

func _add_font_scale_row() -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)

	var lbl := Label.new()
	lbl.text = "Font Scale"
	lbl.custom_minimum_size = Vector2(180, 0)
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	hbox.add_child(lbl)

	var option := OptionButton.new()
	for val in _FONT_SCALE_VALUES:
		option.add_item("%d%%" % val)
	# Map current setting value to index.
	var current_val: int = int(_get_setting("accessibility_font_scale"))
	var idx := _FONT_SCALE_VALUES.find(current_val)
	if idx < 0:
		idx = 0
	option.selected = idx
	option.custom_minimum_size = Vector2(160, 0)
	option.process_mode = Node.PROCESS_MODE_ALWAYS
	hbox.add_child(option)

	option.item_selected.connect(func(selected_idx: int) -> void:
		if selected_idx >= 0 and selected_idx < _FONT_SCALE_VALUES.size():
			_set_setting("accessibility_font_scale", _FONT_SCALE_VALUES[selected_idx])
	)

	_content_container.add_child(hbox)


# ── Widget builders ──────────────────────────────────────────────────────────

func _add_slider_row(label_text: String, key: String, min_val: float, max_val: float) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)

	var lbl := Label.new()
	lbl.text = label_text
	lbl.custom_minimum_size = Vector2(180, 0)
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	hbox.add_child(lbl)

	var slider := HSlider.new()
	slider.min_value = min_val
	slider.max_value = max_val
	slider.step = 0.05
	slider.value = float(_get_setting(key))
	slider.custom_minimum_size = Vector2(200, 0)
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.process_mode = Node.PROCESS_MODE_ALWAYS
	hbox.add_child(slider)

	var val_label := Label.new()
	val_label.text = "%.0f%%" % (float(_get_setting(key)) * 100.0) if max_val <= 1.0 else "%.1f" % float(_get_setting(key))
	val_label.custom_minimum_size = Vector2(50, 0)
	val_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	val_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	val_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	hbox.add_child(val_label)

	slider.value_changed.connect(func(v: float) -> void:
		_set_setting(key, v)
		if max_val <= 1.0:
			val_label.text = "%.0f%%" % (v * 100.0)
		else:
			val_label.text = "%.1f" % v
		# Apply audio bus volume immediately for real-time feedback.
		if AUDIO_BUS_MAP.has(key):
			_apply_audio_volume(key, v)
	)

	_content_container.add_child(hbox)


func _add_toggle_row(label_text: String, key: String) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)

	var lbl := Label.new()
	lbl.text = label_text
	lbl.custom_minimum_size = Vector2(180, 0)
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	hbox.add_child(lbl)

	var toggle := CheckButton.new()
	toggle.button_pressed = bool(_get_setting(key))
	toggle.process_mode = Node.PROCESS_MODE_ALWAYS
	hbox.add_child(toggle)

	toggle.toggled.connect(func(pressed: bool) -> void:
		_set_setting(key, pressed)
		# Apply display/gameplay changes immediately.
		if key.begins_with("display_"):
			_apply_display_settings()
		elif key == "gameplay_auto_pause":
			_apply_gameplay_settings()
	)

	_content_container.add_child(hbox)


func _add_option_row(label_text: String, key: String, options: Array) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)

	var lbl := Label.new()
	lbl.text = label_text
	lbl.custom_minimum_size = Vector2(180, 0)
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	hbox.add_child(lbl)

	var option := OptionButton.new()
	for opt_text in options:
		option.add_item(opt_text)
	option.selected = int(_get_setting(key))
	option.custom_minimum_size = Vector2(160, 0)
	option.process_mode = Node.PROCESS_MODE_ALWAYS
	hbox.add_child(option)

	option.item_selected.connect(func(idx: int) -> void:
		_set_setting(key, idx)
		# Apply display changes immediately.
		if key.begins_with("display_"):
			_apply_display_settings()
	)

	_content_container.add_child(hbox)


func _add_info_row(text: String) -> void:
	var lbl := Label.new()
	lbl.text = text
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_content_container.add_child(lbl)


# ── Display mode with revert confirmation ───────────────────────────────────
# GATE.S9.SETTINGS.DISPLAY_REVERT.001

func _add_display_mode_row() -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_MD)

	var lbl := Label.new()
	lbl.text = "Window Mode"
	lbl.custom_minimum_size = Vector2(180, 0)
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	hbox.add_child(lbl)

	var options := ["Windowed", "Fullscreen", "Borderless"]
	var option := OptionButton.new()
	for opt_text in options:
		option.add_item(opt_text)
	option.selected = int(_get_setting("display_mode"))
	option.custom_minimum_size = Vector2(160, 0)
	option.process_mode = Node.PROCESS_MODE_ALWAYS
	hbox.add_child(option)

	option.item_selected.connect(func(idx: int) -> void:
		# Store previous value before applying the change.
		_revert_previous_display_mode = int(_get_setting("display_mode"))
		_set_setting("display_mode", idx)
		_apply_display_settings()
		# Show revert confirmation only if value actually changed.
		if idx != _revert_previous_display_mode:
			_show_revert_confirmation()
	)

	_content_container.add_child(hbox)


func _show_revert_confirmation() -> void:
	# Remove any existing revert overlay.
	if _revert_overlay != null and is_instance_valid(_revert_overlay):
		_revert_overlay.queue_free()
		_revert_overlay = null

	# Build revert confirmation panel overlaying settings.
	_revert_overlay = Control.new()
	_revert_overlay.name = "RevertOverlay"
	_revert_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_revert_overlay.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_revert_overlay)

	var overlay_bg := ColorRect.new()
	overlay_bg.color = Color(0.0, 0.0, 0.0, 0.5)
	overlay_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay_bg.mouse_filter = Control.MOUSE_FILTER_STOP
	_revert_overlay.add_child(overlay_bg)

	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_revert_overlay.add_child(center)

	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(380, 180)
	panel.add_theme_stylebox_override("panel", UITheme.make_panel_modal())
	center.add_child(panel)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	panel.add_child(vbox)

	var title := Label.new()
	title.text = "KEEP DISPLAY CHANGES?"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	_revert_countdown_label = Label.new()
	_revert_countdown_label.text = "Reverting in 15 seconds..."
	_revert_countdown_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_revert_countdown_label.add_theme_color_override("font_color", UITheme.ORANGE)
	_revert_countdown_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_revert_countdown_label)

	var btn_row := HBoxContainer.new()
	btn_row.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_row.add_theme_constant_override("separation", UITheme.SPACE_LG)
	vbox.add_child(btn_row)

	var keep_btn := Button.new()
	keep_btn.text = "Keep Changes"
	keep_btn.custom_minimum_size = Vector2(140, 36)
	keep_btn.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	keep_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	keep_btn.pressed.connect(_on_revert_keep)
	btn_row.add_child(keep_btn)

	var revert_btn := Button.new()
	revert_btn.text = "Revert"
	revert_btn.custom_minimum_size = Vector2(140, 36)
	revert_btn.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	revert_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	revert_btn.pressed.connect(_on_revert_revert)
	btn_row.add_child(revert_btn)

	# Start countdown.
	_revert_timer = _REVERT_TIMEOUT
	print("UUIR|SETTINGS_PANEL|DISPLAY_REVERT_SHOW|prev=%d" % _revert_previous_display_mode)


func _process(delta: float) -> void:
	if _revert_timer < 0.0:
		return
	_revert_timer -= delta
	if _revert_countdown_label != null and is_instance_valid(_revert_countdown_label):
		_revert_countdown_label.text = "Reverting in %d seconds..." % ceili(_revert_timer)
	if _revert_timer <= 0.0:
		_on_revert_revert()


func _on_revert_keep() -> void:
	_revert_timer = -1.0
	if _revert_overlay != null and is_instance_valid(_revert_overlay):
		_revert_overlay.queue_free()
		_revert_overlay = null
	# Rebuild display tab to show current value.
	if _current_tab == Tab.DISPLAY:
		_switch_tab(Tab.DISPLAY)
	print("UUIR|SETTINGS_PANEL|DISPLAY_REVERT_KEEP")


func _on_revert_revert() -> void:
	_revert_timer = -1.0
	# Revert to previous display mode.
	_set_setting("display_mode", _revert_previous_display_mode)
	_apply_display_settings()
	if _revert_overlay != null and is_instance_valid(_revert_overlay):
		_revert_overlay.queue_free()
		_revert_overlay = null
	# Rebuild display tab to show reverted value.
	if _current_tab == Tab.DISPLAY:
		_switch_tab(Tab.DISPLAY)
	print("UUIR|SETTINGS_PANEL|DISPLAY_REVERT_REVERTED|to=%d" % _revert_previous_display_mode)


# ── Settings application ────────────────────────────────────────────────────

## Apply all saved settings to engine subsystems. Called on startup and after reset.
func _apply_all_settings() -> void:
	for key in AUDIO_BUS_MAP:
		var val = _get_setting(key)
		_apply_audio_volume(key, float(val) if val != null else 1.0)
	_apply_display_settings()
	_apply_gameplay_settings()


## Map a linear 0-1 slider value to an AudioServer bus.
func _apply_audio_volume(key: String, value: float) -> void:
	var bus_name: String = AUDIO_BUS_MAP.get(key, "")
	if bus_name.is_empty():
		return
	var idx := AudioServer.get_bus_index(bus_name)
	if idx < 0:
		return
	AudioServer.set_bus_volume_db(idx, linear_to_db(value))
	# Mute the bus when the slider is essentially at zero to avoid residual noise.
	AudioServer.set_bus_mute(idx, value <= 0.01)


## Apply display settings (window mode, vsync, quality) to DisplayServer / RenderingServer.
func _apply_display_settings() -> void:
	# -- Window mode --
	var mode_val := int(_get_setting("display_mode")) if _get_setting("display_mode") != null else 0
	match mode_val:
		0:  # Windowed
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, false)
		1:  # Fullscreen
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, false)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		2:  # Borderless fullscreen
			DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, true)
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)

	# -- VSync --
	var vsync_val = _get_setting("display_vsync")
	if vsync_val == null:
		vsync_val = true
	if bool(vsync_val):
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED)
	else:
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)

	# -- Quality preset --
	var quality := int(_get_setting("display_quality")) if _get_setting("display_quality") != null else 1
	match quality:
		0:  # Low
			RenderingServer.directional_shadow_atlas_set_size(1024, false)
			get_viewport().scaling_3d_scale = 0.67
		1:  # Medium
			RenderingServer.directional_shadow_atlas_set_size(2048, false)
			get_viewport().scaling_3d_scale = 0.85
		2:  # High
			RenderingServer.directional_shadow_atlas_set_size(4096, true)
			get_viewport().scaling_3d_scale = 1.0


## Apply gameplay settings that affect engine behaviour.
func _apply_gameplay_settings() -> void:
	# Auto-pause is handled via _notification — nothing to set on engine here,
	# but we log the current state for diagnostics.
	var auto_pause = _get_setting("gameplay_auto_pause")
	if auto_pause == null:
		auto_pause = true
	print("UUIR|SETTINGS_PANEL|AUTO_PAUSE=%s" % str(bool(auto_pause)))

	# Apply tooltip delay to Godot's built-in tooltip timer.
	var delay: float = float(_get_setting("gameplay_tooltip_delay"))
	ProjectSettings.set_setting("gui/timers/tooltip_delay_sec", delay)


## Respond to focus-out notification for auto-pause feature.
func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT:
		var auto_pause = _get_setting("gameplay_auto_pause")
		if auto_pause == null:
			auto_pause = true
		if bool(auto_pause):
			get_tree().paused = true
			print("UUIR|SETTINGS_PANEL|FOCUS_LOST_PAUSE")
	elif what == NOTIFICATION_APPLICATION_FOCUS_IN:
		var auto_pause = _get_setting("gameplay_auto_pause")
		if auto_pause == null:
			auto_pause = true
		if bool(auto_pause):
			get_tree().paused = false
			print("UUIR|SETTINGS_PANEL|FOCUS_REGAINED_UNPAUSE")


## Handle settings changed from external sources (e.g. another panel, console command).
func _on_settings_changed(key: String, value: Variant) -> void:
	if AUDIO_BUS_MAP.has(key):
		_apply_audio_volume(key, float(value) if value != null else 1.0)
	elif key.begins_with("display_"):
		_apply_display_settings()
	elif key == "gameplay_auto_pause":
		_apply_gameplay_settings()
	# GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001: accessibility keys handled by their own autoloads
	# (SettingsManager handles font_scale, ColorblindFilter handles colorblind_mode)


# ── Settings access helpers ──────────────────────────────────────────────────

func _get_setting(key: String) -> Variant:
	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr and mgr.has_method("get_setting"):
		return mgr.get_setting(key)
	# Fallback if autoload not available yet.
	return null


func _set_setting(key: String, value: Variant) -> void:
	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr and mgr.has_method("set_setting"):
		mgr.set_setting(key, value)


func _on_reset_defaults() -> void:
	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr and mgr.has_method("reset_all"):
		mgr.reset_all()
	# Re-apply all settings to engine after reset.
	_apply_all_settings()
	# Re-build current tab to reflect new values.
	_switch_tab(_current_tab)
	print("UUIR|SETTINGS_PANEL|RESET_DEFAULTS")
