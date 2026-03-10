# GATE.S7.AUTOMATION_MGMT.DOCTRINE_UI.001: Fleet doctrine editing panel.
# PanelContainer for editing engagement doctrine (stance, retreat threshold, patrol radius).
# Opened from the automation dashboard "Edit Doctrine" button.
# Reads/writes doctrine via SimBridge query contracts.
extends PanelContainer

var _bridge: Object = null
var _fleet_id := "fleet_trader_1"

# UI widgets
var _fleet_selector: OptionButton = null
var _stance_selector: OptionButton = null
var _retreat_slider: HSlider = null
var _retreat_value_label: Label = null
var _patrol_slider: HSlider = null
var _patrol_value_label: Label = null

const STANCES := ["Aggressive", "Defensive", "Evasive"]


func _ready() -> void:
	name = "DoctrinePanel"
	visible = false
	mouse_filter = Control.MOUSE_FILTER_STOP
	var style := UITheme.make_panel_standard(UITheme.BORDER_ACCENT)
	add_theme_stylebox_override("panel", style)
	custom_minimum_size = Vector2(340, 0)
	_build_ui()
	_position_panel()


func _build_ui() -> void:
	var vbox := VBoxContainer.new()
	vbox.name = "DoctrineVBox"
	vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	add_child(vbox)

	# Title
	var title := Label.new()
	title.text = "EDIT FLEET DOCTRINE"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	vbox.add_child(HSeparator.new())

	# ── Fleet selector ──
	_add_label(vbox, "Fleet")
	_fleet_selector = OptionButton.new()
	_fleet_selector.add_item("fleet_trader_1")
	_fleet_selector.custom_minimum_size = Vector2(0, 28)
	_fleet_selector.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	vbox.add_child(_fleet_selector)
	_fleet_selector.item_selected.connect(_on_fleet_changed)

	vbox.add_child(HSeparator.new())

	# ── Stance selector ──
	_add_label(vbox, "Engagement Stance")
	_stance_selector = OptionButton.new()
	for s in STANCES:
		_stance_selector.add_item(s)
	_stance_selector.custom_minimum_size = Vector2(0, 28)
	_stance_selector.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	vbox.add_child(_stance_selector)

	# ── Retreat threshold slider ──
	_add_label(vbox, "Retreat Threshold")
	var retreat_row := HBoxContainer.new()
	retreat_row.add_theme_constant_override("separation", UITheme.SPACE_SM)
	vbox.add_child(retreat_row)

	_retreat_slider = HSlider.new()
	_retreat_slider.min_value = 0
	_retreat_slider.max_value = 100
	_retreat_slider.step = 1
	_retreat_slider.value = 25
	_retreat_slider.custom_minimum_size = Vector2(200, 0)
	_retreat_slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	retreat_row.add_child(_retreat_slider)

	_retreat_value_label = Label.new()
	_retreat_value_label.text = "25%"
	_retreat_value_label.custom_minimum_size = Vector2(50, 0)
	_retreat_value_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_retreat_value_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_retreat_value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	retreat_row.add_child(_retreat_value_label)

	_retreat_slider.value_changed.connect(func(v: float) -> void:
		_retreat_value_label.text = "%d%%" % int(v)
	)

	# ── Patrol radius slider ──
	_add_label(vbox, "Patrol Radius")
	var patrol_row := HBoxContainer.new()
	patrol_row.add_theme_constant_override("separation", UITheme.SPACE_SM)
	vbox.add_child(patrol_row)

	_patrol_slider = HSlider.new()
	_patrol_slider.min_value = 15
	_patrol_slider.max_value = 200
	_patrol_slider.step = 1
	_patrol_slider.value = 50
	_patrol_slider.custom_minimum_size = Vector2(200, 0)
	_patrol_slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	patrol_row.add_child(_patrol_slider)

	_patrol_value_label = Label.new()
	_patrol_value_label.text = "50"
	_patrol_value_label.custom_minimum_size = Vector2(50, 0)
	_patrol_value_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_patrol_value_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_patrol_value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	patrol_row.add_child(_patrol_value_label)

	_patrol_slider.value_changed.connect(func(v: float) -> void:
		_patrol_value_label.text = "%d" % int(v)
	)

	vbox.add_child(HSeparator.new())

	# ── Button row ──
	var btn_row := HBoxContainer.new()
	btn_row.add_theme_constant_override("separation", UITheme.SPACE_MD)
	btn_row.alignment = BoxContainer.ALIGNMENT_END
	vbox.add_child(btn_row)

	var cancel_btn := Button.new()
	cancel_btn.text = "Cancel"
	cancel_btn.custom_minimum_size = Vector2(90, 32)
	cancel_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	cancel_btn.pressed.connect(_on_cancel_pressed)
	btn_row.add_child(cancel_btn)

	var apply_btn := Button.new()
	apply_btn.text = "Apply"
	apply_btn.custom_minimum_size = Vector2(90, 32)
	apply_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	apply_btn.pressed.connect(_on_apply_pressed)
	btn_row.add_child(apply_btn)


## Position panel at center-left of viewport.
func _position_panel() -> void:
	var vp_size := get_viewport_rect().size
	var panel_x := 16.0
	var panel_y := (vp_size.y - size.y) * 0.5
	position = Vector2(panel_x, panel_y)


## Show panel and load current doctrine values from bridge.
func show_for_fleet(fleet_id: String) -> void:
	_fleet_id = fleet_id
	visible = true
	_position_panel()
	_load_current_values()


## Load current doctrine settings from SimBridge.
func _load_current_values() -> void:
	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("GameManager")
	if _bridge == null:
		return
	if not _bridge.has_method("GetDoctrineSettingsV0"):
		return
	var data: Dictionary = _bridge.call("GetDoctrineSettingsV0", _fleet_id)
	if data.is_empty():
		return

	# Set stance dropdown
	var stance_str: String = str(data.get("stance", "Defensive"))
	for i in range(STANCES.size()):
		if STANCES[i].to_lower() == stance_str.to_lower():
			_stance_selector.selected = i
			break

	# Set retreat threshold
	var retreat_val: int = int(data.get("retreat_threshold_pct", 25))
	_retreat_slider.value = retreat_val
	_retreat_value_label.text = "%d%%" % retreat_val

	# Set patrol radius
	var patrol_val: float = float(data.get("patrol_radius", 50.0))
	_patrol_slider.value = patrol_val
	_patrol_value_label.text = "%d" % int(patrol_val)


func _on_fleet_changed(_index: int) -> void:
	_fleet_id = _fleet_selector.get_item_text(_fleet_selector.selected)
	_load_current_values()


func _on_apply_pressed() -> void:
	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("GameManager")
	if _bridge == null:
		return
	if not _bridge.has_method("SetDoctrineV0"):
		print("UUIR|DOCTRINE_PANEL|NO_BRIDGE_METHOD|SetDoctrineV0")
		return

	var stance_text: String = STANCES[_stance_selector.selected]
	var retreat_threshold: int = int(_retreat_slider.value)
	var patrol_radius: int = int(_patrol_slider.value)

	_bridge.call("SetDoctrineV0", _fleet_id, stance_text, retreat_threshold, patrol_radius)
	print("UUIR|DOCTRINE_PANEL|APPLY|%s|%s|%d|%d" % [_fleet_id, stance_text, retreat_threshold, patrol_radius])
	visible = false


func _on_cancel_pressed() -> void:
	print("UUIR|DOCTRINE_PANEL|CANCEL")
	visible = false


# ── Helpers ──

func _add_label(parent: VBoxContainer, text: String) -> void:
	var lbl := Label.new()
	lbl.text = text
	lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	parent.add_child(lbl)
