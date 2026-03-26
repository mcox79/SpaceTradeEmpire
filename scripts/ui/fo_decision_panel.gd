# scripts/ui/fo_decision_panel.gd
# GATE.T58.UI.FO_DECISION.001: FO decision dialogue panel.
# Per fo_trade_manager_v0.md §Decision Dialogue Design Rules:
# 1. FO recommendation highlighted (personality-driven)
# 2. All options visible (no collapse)
# 3. Context → Stakes → Options structure
# 4. Quantified consequences
# 5. One briefing at a time (queue by severity)
extends PanelContainer

var _bridge = null
var _situation_label: Label = null
var _stakes_label: Label = null
var _options_vbox: VBoxContainer = null
var _queue_label: Label = null
var _type_label: Label = null
var _active_decision_id: String = ""
var _poll_counter: int = 0  # STRUCTURAL: frame counter
const _POLL_INTERVAL: int = 30  # STRUCTURAL: poll every 30 frames


func _ready() -> void:
	name = "FODecisionPanel"
	visible = false
	# Center screen — modal-style.
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -260
	offset_right = 260
	offset_top = -200
	offset_bottom = 200
	custom_minimum_size = Vector2(520, 0)

	var style := UITheme.make_panel_standard(UITheme.CYAN)
	style.bg_color = Color(0.08, 0.08, 0.12, 0.97)
	add_theme_stylebox_override("panel", style)
	UITheme.add_corner_brackets(self)
	UITheme.add_scanline_overlay(self)
	mouse_filter = Control.MOUSE_FILTER_STOP

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	add_child(vbox)

	# Header: Decision type badge + queue count.
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_LG)
	vbox.add_child(header)

	var fo_icon := Label.new()
	fo_icon.text = "◈ FO BRIEFING"
	fo_icon.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	fo_icon.add_theme_color_override("font_color", UITheme.GOLD)
	fo_icon.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(fo_icon)

	_type_label = Label.new()
	_type_label.text = ""
	_type_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_type_label.add_theme_color_override("font_color", UITheme.CYAN)
	header.add_child(_type_label)

	_queue_label = Label.new()
	_queue_label.text = ""
	_queue_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_queue_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	header.add_child(_queue_label)

	var sep1 := HSeparator.new()
	vbox.add_child(sep1)

	# Rule 3: Context → Stakes → Options structure.
	# Situation (context).
	_situation_label = Label.new()
	_situation_label.text = ""
	_situation_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_situation_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_situation_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(_situation_label)

	# Stakes.
	_stakes_label = Label.new()
	_stakes_label.text = ""
	_stakes_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_stakes_label.add_theme_color_override("font_color", UITheme.ORANGE)
	_stakes_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(_stakes_label)

	var sep2 := HSeparator.new()
	vbox.add_child(sep2)

	# Options container — Rule 2: all options visible.
	_options_vbox = VBoxContainer.new()
	_options_vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	vbox.add_child(_options_vbox)

	_bridge = get_node_or_null("/root/SimBridge")


func _physics_process(_delta: float) -> void:
	_poll_counter += 1  # STRUCTURAL: increment
	if _poll_counter < _POLL_INTERVAL:
		return
	_poll_counter = 0  # STRUCTURAL: reset

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		return
	if not _bridge.has_method("GetActiveDecisionV0"):
		return

	var decision: Dictionary = _bridge.call("GetActiveDecisionV0")
	if not decision.get("has_decision", false):
		if visible:
			visible = false
			_active_decision_id = ""
		return

	var decision_id: String = str(decision.get("decision_id", ""))
	if decision_id == _active_decision_id:
		return  # Already showing this decision.

	_active_decision_id = decision_id
	_show_decision(decision)


func _show_decision(decision: Dictionary) -> void:
	# Rule 3: Context → Stakes → Options.
	_situation_label.text = str(decision.get("situation", ""))
	_stakes_label.text = "⚠ " + str(decision.get("stakes", ""))

	# Type badge.
	var dtype: String = str(decision.get("type", ""))
	_type_label.text = dtype.to_upper()

	# Queue indicator (Rule 5: one at a time, show queue depth).
	var queue_size: int = decision.get("queue_size", 0)
	_queue_label.text = "+%d pending" % queue_size if queue_size > 0 else ""

	# Build option buttons — Rule 2: all visible, Rule 1: recommendation highlighted.
	for child in _options_vbox.get_children():
		child.queue_free()

	var options: Array = decision.get("options", [])
	var rec_idx: int = decision.get("recommended_index", -1)

	for i in range(options.size()):
		var opt: Dictionary = options[i]
		var is_rec: bool = opt.get("is_recommended", false)
		var card := _build_option_card(i, opt, is_rec)
		_options_vbox.add_child(card)

	visible = true


func _build_option_card(idx: int, opt: Dictionary, is_recommended: bool) -> PanelContainer:
	var card := PanelContainer.new()
	var card_style := StyleBoxFlat.new()
	card_style.bg_color = Color(0.12, 0.14, 0.18) if not is_recommended else Color(0.15, 0.2, 0.12)
	card_style.border_color = UITheme.GOLD if is_recommended else Color(0.3, 0.3, 0.4)
	card_style.set_border_width_all(2 if is_recommended else 1)  # STRUCTURAL: border width
	card_style.set_content_margin_all(8)
	card_style.set_corner_radius_all(4)
	card.add_theme_stylebox_override("panel", card_style)
	card.mouse_filter = Control.MOUSE_FILTER_STOP

	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	card.add_child(hbox)

	# Left: option content.
	var content_vbox := VBoxContainer.new()
	content_vbox.add_theme_constant_override("separation", 2)  # STRUCTURAL: tight spacing
	content_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(content_vbox)

	# Option label + recommended tag.
	var label_row := HBoxContainer.new()
	content_vbox.add_child(label_row)

	var label := Label.new()
	label.text = str(opt.get("label", "Option %d" % (idx + 1)))
	label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	label_row.add_child(label)

	# Rule 1: FO recommendation highlighted.
	if is_recommended:
		var rec_tag := Label.new()
		rec_tag.text = " ★ FO RECOMMENDS"
		rec_tag.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		rec_tag.add_theme_color_override("font_color", UITheme.GOLD)
		label_row.add_child(rec_tag)

	# Description.
	var desc := Label.new()
	desc.text = str(opt.get("description", ""))
	desc.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	desc.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	content_vbox.add_child(desc)

	# Rule 4: Quantified consequences.
	var consequence_row := HBoxContainer.new()
	consequence_row.add_theme_constant_override("separation", UITheme.SPACE_LG)
	content_vbox.add_child(consequence_row)

	var credit_impact: int = opt.get("credit_impact", 0)
	if credit_impact != 0:
		var credit_lbl := Label.new()
		var sign: String = "+" if credit_impact > 0 else ""
		credit_lbl.text = "%s%d cr" % [sign, credit_impact]
		credit_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		credit_lbl.add_theme_color_override("font_color", UITheme.GREEN if credit_impact > 0 else UITheme.RED)
		consequence_row.add_child(credit_lbl)

	var risk_level: int = opt.get("risk_level", 0)
	if risk_level > 0:
		var risk_lbl := Label.new()
		risk_lbl.text = "Risk: " + _risk_text(risk_level)
		risk_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		risk_lbl.add_theme_color_override("font_color", _risk_color(risk_level))
		consequence_row.add_child(risk_lbl)

	var explore_val: int = opt.get("exploration_value", 0)
	if explore_val > 0:
		var explore_lbl := Label.new()
		explore_lbl.text = "Discovery: +" + str(explore_val)
		explore_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		explore_lbl.add_theme_color_override("font_color", UITheme.CYAN)
		consequence_row.add_child(explore_lbl)

	# Right: select button.
	var btn := Button.new()
	btn.text = "Select"
	btn.custom_minimum_size = Vector2(80, 32)
	btn.pressed.connect(_on_option_selected.bind(idx))
	hbox.add_child(btn)

	return card


func _on_option_selected(idx: int) -> void:
	if _bridge == null:
		return
	if not _bridge.has_method("ResolveDecisionV0"):
		return
	var success: bool = _bridge.call("ResolveDecisionV0", idx)
	if success:
		visible = false
		_active_decision_id = ""
		# Toast confirmation.
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Decision resolved.", "fo")


func _risk_text(level: int) -> String:
	match level:
		1: return "Low"
		2: return "Medium"
		3: return "High"
		_: return "Extreme" if level >= 4 else "None"


func _risk_color(level: int) -> Color:
	match level:
		1: return UITheme.GREEN
		2: return UITheme.ORANGE
		3: return UITheme.RED
		_: return UITheme.RED if level >= 4 else UITheme.TEXT_MUTED
