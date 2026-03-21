# GATE.T43.SCAN_UI.HUD_PANEL.001: Scanner charge HUD indicator + mode selector + orbital scan button.
# Shown when player is at a node with a planet. Hidden during transit and at planet-less nodes.
extends PanelContainer

var _bridge = null
var _charge_label: Label = null
var _mineral_btn: Button = null
var _signal_btn: Button = null
var _arch_btn: Button = null
var _scan_btn: Button = null
var _selected_mode: String = "MineralSurvey"
var _charge_tween: Tween = null

# GATE.T43.SCAN_UI.COMPLETION.001: Completion tracker label.
var _completion_label: Label = null

# GATE.T43.SCAN_UI.CHARGE_RESET.001: Track charges for reset detection.
var _prev_remaining: int = -1
var _prev_node_id: String = ""

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.07, 0.12, 0.85)
	style.border_width_left = 2
	style.border_color = UITheme.YELLOW
	style.set_corner_radius_all(4)
	style.set_content_margin_all(6)
	add_theme_stylebox_override("panel", style)
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	var vbox := VBoxContainer.new()
	vbox.name = "ScannerVBox"
	vbox.add_theme_constant_override("separation", 4)
	add_child(vbox)

	_charge_label = Label.new()
	_charge_label.name = "ChargeLabel"
	_charge_label.text = "SCANNER"
	_charge_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_charge_label.add_theme_color_override("font_color", UITheme.YELLOW)
	vbox.add_child(_charge_label)

	var mode_box := VBoxContainer.new()
	mode_box.name = "ModeContainer"
	mode_box.add_theme_constant_override("separation", 2)
	vbox.add_child(mode_box)

	_mineral_btn = _make_mode_button("Mineral Survey", "MineralSurvey")
	mode_box.add_child(_mineral_btn)
	_signal_btn = _make_mode_button("Signal Sweep", "SignalSweep")
	mode_box.add_child(_signal_btn)
	_arch_btn = _make_mode_button("Archaeological", "Archaeological")
	mode_box.add_child(_arch_btn)

	_scan_btn = Button.new()
	_scan_btn.name = "ScanButton"
	_scan_btn.text = "ORBITAL SCAN"
	_scan_btn.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_scan_btn.custom_minimum_size = Vector2(180, 28)
	_scan_btn.pressed.connect(_on_orbital_scan_pressed)
	vbox.add_child(_scan_btn)

	# GATE.T43.SCAN_UI.COMPLETION.001: Planet types surveyed counter.
	_completion_label = Label.new()
	_completion_label.name = "CompletionLabel"
	_completion_label.text = ""
	_completion_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_completion_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_completion_label.visible = false
	vbox.add_child(_completion_label)

	visible = false


func _make_mode_button(label: String, mode: String) -> Button:
	var btn := Button.new()
	btn.text = label
	btn.flat = true
	btn.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	btn.custom_minimum_size = Vector2(180, 22)
	btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
	btn.pressed.connect(_on_mode_selected.bind(mode))
	return btn


func _on_mode_selected(mode: String) -> void:
	_selected_mode = mode
	_update_mode_buttons()


func _on_orbital_scan_pressed() -> void:
	if _bridge == null or not _bridge.has_method("OrbitalScanV0"):
		return
	var gm = get_node_or_null("/root/GameManager")
	var node_id: String = ""
	if gm:
		var ps: Dictionary = _bridge.call("GetPlayerStateV0") if _bridge.has_method("GetPlayerStateV0") else {}
		node_id = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		return

	var result: Dictionary = _bridge.call("OrbitalScanV0", node_id, _selected_mode)
	if result.has("error"):
		var toast_mgr = get_tree().root.find_child("ToastManager", true, false) if get_tree() else null
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", str(result["error"]), "warning")
		return

	# Fire scan result toast.
	_show_scan_toast(result)
	# Pulse charge counter animation.
	_pulse_charge_label()
	# Play scan audio.
	_play_scan_audio(result)
	# Refresh display.
	refresh_v0()


func _show_scan_toast(result: Dictionary) -> void:
	var category: String = str(result.get("category", ""))
	var mode: String = str(result.get("mode", ""))
	var flavor: String = str(result.get("flavor_text", ""))
	# Truncate flavor text for toast.
	if flavor.length() > 60:
		flavor = flavor.substr(0, 60) + "..."
	var toast_text: String = "SCAN COMPLETE\n%s · %s\n\"%s\"" % [category, mode, flavor]

	# GATE.T43.SCAN_UI.RESULT_TOAST.001: Category-specific priority.
	var priority: String = "milestone"
	if category in ["FragmentCache", "DataArchive", "PhysicalEvidence"]:
		priority = "critical"

	var toast_mgr = get_tree().root.find_child("ToastManager", true, false) if get_tree() else null
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", toast_text, priority)


func _play_scan_audio(result: Dictionary) -> void:
	# GATE.T43.SCAN_AUDIO.CHIMES.001: Category-specific chimes via game_manager.
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("play_scan_chime_v0"):
		var category: String = str(result.get("category", ""))
		gm.call("play_scan_chime_v0", category)


func _pulse_charge_label() -> void:
	if _charge_label == null:
		return
	if _charge_tween and _charge_tween.is_running():
		_charge_tween.kill()
	_charge_tween = create_tween()
	_charge_label.scale = Vector2(1.0, 1.0)
	_charge_tween.tween_property(_charge_label, "scale", Vector2(1.2, 1.2), 0.15)
	_charge_tween.tween_property(_charge_label, "scale", Vector2(1.0, 1.0), 0.15)


func refresh_v0() -> void:
	if _bridge == null or not _bridge.has_method("GetScanChargesV0"):
		visible = false
		return

	# Check if at a planet node.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0") if _bridge.has_method("GetPlayerStateV0") else {}
	var node_id: String = str(ps.get("current_node_id", ""))
	var raw_state: String = str(ps.get("ship_state_token", ""))

	if node_id.is_empty() or raw_state == "IN_LANE_TRANSIT":
		visible = false
		return

	var planet_info: Dictionary = {}
	if _bridge.has_method("GetPlanetInfoV0"):
		planet_info = _bridge.call("GetPlanetInfoV0", node_id)
	if planet_info.size() == 0:
		visible = false
		return

	# We have a planet — show the panel.
	visible = true

	var charges: Dictionary = _bridge.call("GetScanChargesV0")
	var remaining: int = int(charges.get("remaining", 0))
	var max_charges: int = int(charges.get("max", 2))
	var tier: int = int(charges.get("tier", 0))

	# GATE.T43.SCAN_UI.CHARGE_RESET.001: Detect charge reset on travel.
	if _prev_node_id != "" and _prev_node_id != node_id:
		if remaining > _prev_remaining and _prev_remaining >= 0:
			var toast_mgr = get_tree().root.find_child("ToastManager", true, false) if get_tree() else null
			if toast_mgr and toast_mgr.has_method("show_priority_toast"):
				toast_mgr.call("show_priority_toast", "Scanner charges refreshed (%d/%d)" % [remaining, max_charges], "info")
	_prev_remaining = remaining
	_prev_node_id = node_id

	# Charge display with color coding.
	var charge_color: Color = UITheme.GREEN
	if remaining == 0:
		charge_color = UITheme.RED
	elif remaining == 1:
		charge_color = UITheme.ORANGE
	_charge_label.text = "<%d/%d>  SCANNER" % [remaining, max_charges]
	_charge_label.add_theme_color_override("font_color", charge_color)

	# Mode availability.
	var mineral_ok: bool = charges.get("mineral_available", true)
	var signal_ok: bool = charges.get("signal_available", false)
	var arch_ok: bool = charges.get("archaeological_available", false)
	_update_mode_availability(_mineral_btn, mineral_ok, "MineralSurvey")
	_update_mode_availability(_signal_btn, signal_ok, "SignalSweep")
	_update_mode_availability(_arch_btn, arch_ok, "Archaeological")

	# Scan button state.
	_scan_btn.disabled = remaining <= 0
	if remaining <= 0:
		_scan_btn.tooltip_text = "Charges depleted. Travel to another system to reset."
	else:
		_scan_btn.tooltip_text = "Scan planet from orbit (costs 1 charge)"

	# GATE.T43.SCAN_UI.COMPLETION.001: Update completion tracker.
	_update_completion(node_id)


func _update_mode_availability(btn: Button, available: bool, mode: String) -> void:
	if btn == null:
		return
	btn.disabled = not available
	if not available:
		btn.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		btn.tooltip_text = "Requires higher scanner tier"
	else:
		btn.remove_theme_color_override("font_color")
		btn.tooltip_text = ""
	_update_mode_buttons()


func _update_mode_buttons() -> void:
	# Highlight selected mode.
	for entry in [[_mineral_btn, "MineralSurvey"], [_signal_btn, "SignalSweep"], [_arch_btn, "Archaeological"]]:
		var btn: Button = entry[0]
		var mode: String = entry[1]
		if btn == null:
			continue
		if not btn.disabled:
			if mode == _selected_mode:
				btn.add_theme_color_override("font_color", UITheme.YELLOW)
				btn.text = "[x] %s" % _mode_display_name(mode)
			else:
				btn.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
				btn.text = "[ ] %s" % _mode_display_name(mode)
		else:
			btn.text = "[ ] %s (locked)" % _mode_display_name(mode)


func _mode_display_name(mode: String) -> String:
	match mode:
		"MineralSurvey": return "Mineral"
		"SignalSweep": return "Signal"
		"Archaeological": return "Arch"
	return mode


func _update_completion(node_id: String) -> void:
	# Count distinct planet types the player has scanned.
	if _completion_label == null or _bridge == null:
		return
	if not _bridge.has_method("GetPlanetScanResultsV0"):
		_completion_label.visible = false
		return

	# Simple: count distinct planet types across all scan results available.
	# For now, just show scan count at this planet.
	var results: Array = _bridge.call("GetPlanetScanResultsV0", node_id)
	if results.size() > 0:
		_completion_label.text = "%d scan(s) at this planet" % results.size()
		_completion_label.visible = true
	else:
		_completion_label.visible = false
