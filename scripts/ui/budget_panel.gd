# GATE.S7.AUTOMATION_MGMT.BUDGET_UI.001: Budget editing panel for fleet automation.
# Allows player to set credit and goods caps per fleet.
# Reads current values from GetProgramPerformanceV0, writes via SetBudgetCapsV0.
extends PanelContainer

signal budget_applied

var _bridge: Object = null
var _fleet_id := "fleet_trader_1"

# UI elements
var _fleet_selector: OptionButton = null
var _credit_cap_spin: SpinBox = null
var _goods_cap_spin: SpinBox = null
var _credit_info_label: Label = null
var _spend_label: Label = null
var _apply_btn: Button = null
var _cancel_btn: Button = null


func _ready() -> void:
	name = "BudgetPanel"
	visible = false
	mouse_filter = Control.MOUSE_FILTER_STOP

	var style := UITheme.make_panel_standard(UITheme.BORDER_ACCENT)
	add_theme_stylebox_override("panel", style)
	custom_minimum_size = Vector2(340, 0)

	_bridge = get_tree().root.get_node_or_null("GameManager")

	_build_ui()
	_position_panel()


func _build_ui() -> void:
	var vbox := VBoxContainer.new()
	vbox.name = "BudgetVBox"
	vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	add_child(vbox)

	# Title
	var title := Label.new()
	title.text = "EDIT BUDGET"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	vbox.add_child(HSeparator.new())

	# Fleet selector
	var fleet_label := Label.new()
	fleet_label.text = "Fleet"
	fleet_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	fleet_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	vbox.add_child(fleet_label)

	_fleet_selector = OptionButton.new()
	_fleet_selector.add_item("fleet_trader_1", 0)
	_fleet_selector.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_fleet_selector.item_selected.connect(_on_fleet_selected)
	vbox.add_child(_fleet_selector)

	vbox.add_child(HSeparator.new())

	# Credit cap
	var credit_header := Label.new()
	credit_header.text = "Credit Cap"
	credit_header.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	credit_header.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	vbox.add_child(credit_header)

	_credit_cap_spin = SpinBox.new()
	_credit_cap_spin.min_value = 0
	_credit_cap_spin.max_value = 10000
	_credit_cap_spin.step = 100
	_credit_cap_spin.value = 0
	_credit_cap_spin.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	vbox.add_child(_credit_cap_spin)

	_credit_info_label = Label.new()
	_credit_info_label.text = "0 = unlimited"
	_credit_info_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_credit_info_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	vbox.add_child(_credit_info_label)

	# Current spend display
	_spend_label = Label.new()
	_spend_label.text = "Current spend: -- / --"
	_spend_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_spend_label.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	vbox.add_child(_spend_label)

	vbox.add_child(HSeparator.new())

	# Goods cap
	var goods_header := Label.new()
	goods_header.text = "Goods Cap"
	goods_header.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	goods_header.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	vbox.add_child(goods_header)

	_goods_cap_spin = SpinBox.new()
	_goods_cap_spin.min_value = 0
	_goods_cap_spin.max_value = 100
	_goods_cap_spin.step = 1
	_goods_cap_spin.value = 0
	_goods_cap_spin.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	vbox.add_child(_goods_cap_spin)

	var goods_info := Label.new()
	goods_info.text = "0 = unlimited"
	goods_info.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	goods_info.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	vbox.add_child(goods_info)

	vbox.add_child(HSeparator.new())

	# Button row
	var btn_row := HBoxContainer.new()
	btn_row.add_theme_constant_override("separation", UITheme.SPACE_LG)
	btn_row.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_child(btn_row)

	_cancel_btn = Button.new()
	_cancel_btn.text = "Cancel"
	_cancel_btn.custom_minimum_size = Vector2(100, 30)
	_cancel_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_cancel_btn.pressed.connect(_on_cancel_pressed)
	btn_row.add_child(_cancel_btn)

	_apply_btn = Button.new()
	_apply_btn.text = "Apply"
	_apply_btn.custom_minimum_size = Vector2(100, 30)
	_apply_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_apply_btn.pressed.connect(_on_apply_pressed)
	btn_row.add_child(_apply_btn)


## Position panel center-right (offset from doctrine panel which is center-left).
func _position_panel() -> void:
	var vp_size := get_viewport_rect().size
	var x := vp_size.x * 0.5 + 20.0
	var y := (vp_size.y - custom_minimum_size.y) * 0.5
	position = Vector2(x, max(y, 16.0))


## Show the panel and load current budget values from bridge.
func show_for_fleet(fleet_id: String) -> void:
	_fleet_id = fleet_id
	_load_current_values()
	visible = true
	_position_panel()


## Load current budget values from SimBridge.
func _load_current_values() -> void:
	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("GameManager")
	if _bridge == null:
		return
	if not _bridge.has_method("GetProgramPerformanceV0"):
		return

	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", _fleet_id)
	if data.is_empty():
		return

	var credit_cap: int = int(data.get("budget_credit_cap", 0))
	var goods_cap: int = int(data.get("budget_goods_cap", 0))
	var spent_credits: int = int(data.get("spent_credits_this_cycle", 0))

	_credit_cap_spin.value = credit_cap
	_goods_cap_spin.value = goods_cap

	var cap_str := "unlimited" if credit_cap == 0 else str(credit_cap)
	_spend_label.text = "Current spend: %d / %s" % [spent_credits, cap_str]


func _on_fleet_selected(index: int) -> void:
	_fleet_id = _fleet_selector.get_item_text(index)
	_load_current_values()


func _on_apply_pressed() -> void:
	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("GameManager")
	if _bridge == null:
		visible = false
		return
	if _bridge.has_method("SetBudgetCapsV0"):
		var credit_cap := int(_credit_cap_spin.value)
		var goods_cap := int(_goods_cap_spin.value)
		_bridge.call("SetBudgetCapsV0", _fleet_id, credit_cap, goods_cap)
	visible = false
	budget_applied.emit()


func _on_cancel_pressed() -> void:
	visible = false
