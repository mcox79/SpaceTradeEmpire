# scripts/ui/empire_diamond.gd
# GATE.T58.UI.EMPIRE_DIAMOND.001: Persistent HUD empire health diamond indicator.
# Zone A (top-left). Green/yellow/red reflecting empire health status.
# Click opens Empire tab in dock dashboard. Polls GetEmpireHealthV0 every 60 frames.
extends Control

var _bridge = null
var _diamond_bg: ColorRect = null
var _diamond_icon: Label = null
var _status_label: Label = null
var _poll_counter: int = 0  # STRUCTURAL: frame counter
const _POLL_INTERVAL: int = 60  # STRUCTURAL: poll every 60 frames

# Status color mapping.
const _COLOR_HEALTHY := Color(0.2, 1.0, 0.4)    # Green
const _COLOR_DEGRADED := Color(1.0, 0.85, 0.2)  # Yellow
const _COLOR_CRITICAL := Color(1.0, 0.15, 0.15)  # Red
const _COLOR_NONE := Color(0.5, 0.5, 0.5)        # Gray (no FO)

var _current_status: String = "None"
var _pulse_phase: float = 0.0  # STRUCTURAL: for pulse animation


func _ready() -> void:
	name = "EmpireDiamond"
	size = Vector2(36, 48)
	mouse_filter = Control.MOUSE_FILTER_STOP
	tooltip_text = "Empire Health — click to open Empire tab"

	# GATE.T41.UI.PANEL_CHROME.001: Apply shared panel chrome background to widget.
	var bg_panel := PanelContainer.new()
	bg_panel.name = "EmpireDiamondBg"
	var chrome := UITheme.make_panel_chrome()
	chrome.set_content_margin_all(2.0)
	bg_panel.add_theme_stylebox_override("panel", chrome)
	bg_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg_panel)

	# Diamond background (rotated square).
	_diamond_bg = ColorRect.new()
	_diamond_bg.color = _COLOR_NONE
	_diamond_bg.size = Vector2(24, 24)
	_diamond_bg.position = Vector2(6, 2)
	_diamond_bg.rotation = deg_to_rad(45)  # STRUCTURAL: 45-degree rotation for diamond shape
	_diamond_bg.pivot_offset = Vector2(12, 12)
	_diamond_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_diamond_bg)

	# Diamond icon (centered unicode diamond).
	_diamond_icon = Label.new()
	_diamond_icon.text = "◆"
	_diamond_icon.add_theme_font_size_override("font_size", 20)
	_diamond_icon.add_theme_color_override("font_color", _COLOR_NONE)
	_diamond_icon.position = Vector2(8, -2)
	_diamond_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_diamond_icon)

	# Status text below diamond.
	_status_label = Label.new()
	_status_label.text = ""
	_status_label.add_theme_font_size_override("font_size", 10)
	_status_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	_status_label.position = Vector2(0, 30)
	_status_label.size = Vector2(36, 16)
	_status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_status_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_status_label)

	gui_input.connect(_on_click)
	_bridge = get_node_or_null("/root/SimBridge")


func _physics_process(delta: float) -> void:
	_poll_counter += 1  # STRUCTURAL: increment
	if _poll_counter < _POLL_INTERVAL:
		# Pulse animation for degraded/critical.
		if _current_status == "Critical":
			_pulse_phase += delta * 4.0  # STRUCTURAL: fast pulse
			var alpha: float = 0.6 + 0.4 * abs(sin(_pulse_phase))
			if _diamond_icon:
				_diamond_icon.modulate.a = alpha
		elif _current_status == "Degraded":
			_pulse_phase += delta * 2.0  # STRUCTURAL: slow pulse
			var alpha: float = 0.8 + 0.2 * abs(sin(_pulse_phase))
			if _diamond_icon:
				_diamond_icon.modulate.a = alpha
		return
	_poll_counter = 0  # STRUCTURAL: reset

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		return
	if not _bridge.has_method("GetEmpireHealthV0"):
		return

	var health: Dictionary = _bridge.call("GetEmpireHealthV0")
	var status: String = str(health.get("status", "None"))
	var total: int = health.get("total_managed_routes", 0)

	_current_status = status
	var color: Color = _COLOR_NONE
	var status_text: String = ""

	match status:
		"Healthy":
			color = _COLOR_HEALTHY
			status_text = "OK"
			_diamond_icon.modulate.a = 1.0
			_pulse_phase = 0.0
		"Degraded":
			color = _COLOR_DEGRADED
			status_text = "WARN"
		"Critical":
			color = _COLOR_CRITICAL
			status_text = "CRIT"
		_:
			color = _COLOR_NONE
			status_text = ""
			_diamond_icon.modulate.a = 0.5

	if _diamond_bg:
		_diamond_bg.color = color
	if _diamond_icon:
		_diamond_icon.add_theme_color_override("font_color", color)
	if _status_label:
		_status_label.text = status_text
		_status_label.add_theme_color_override("font_color", color)

	# Visibility: show only when FO is active and has managed routes.
	visible = (status != "None" and total > 0)


func _on_click(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		# Open Empire Dashboard to Empire tab.
		var gm = get_tree().root.find_child("GameManager", true, false)
		if gm and gm.has_method("_toggle_empire_dashboard_v0"):
			gm.call("_toggle_empire_dashboard_v0")
