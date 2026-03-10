extends CanvasLayer
## Gate transit confirmation popup.
## Shows destination, credit cost (congestion-based), and player balance.
## Displayed during GATE_APPROACH state; closed on confirm/cancel/exit.

var _panel: PanelContainer
var _dest_label: Label
var _cost_label: Label
var _congestion_label: Label
var _balance_label: Label
var _hint_label: Label
var _can_afford: bool = true


func _ready() -> void:
	layer = 80
	_build_ui()


func show_transit_v0(bridge: Node, fleet_id: String, target_node_id: String) -> void:
	if bridge == null or not bridge.has_method("GetTransitCostV0"):
		_dest_label.text = "Transit to: " + target_node_id
		return

	var info: Dictionary = bridge.call("GetTransitCostV0", fleet_id, target_node_id)
	var dest_name: String = str(info.get("destination_name", target_node_id))
	var credit_cost: int = int(info.get("credit_cost", 25))
	var congestion_pct: int = int(info.get("congestion_pct", 0))
	var current_credits: int = int(info.get("current_credits", 0))
	_can_afford = bool(info.get("can_afford", true))

	_dest_label.text = "Transit to: " + dest_name

	_cost_label.text = "Transit fee: %d cr" % credit_cost
	_cost_label.add_theme_color_override("font_color", Color.RED if not _can_afford else Color.WHITE)

	if congestion_pct > 0:
		_congestion_label.text = "Thread traffic: %d%%" % congestion_pct
		# Color from green (low) through yellow to red (high).
		var t: float = clampf(float(congestion_pct) / 100.0, 0.0, 1.0)
		var cong_color := Color(t, 1.0 - t * 0.5, 0.2)
		_congestion_label.add_theme_color_override("font_color", cong_color)
		_congestion_label.visible = true
	else:
		_congestion_label.visible = false

	_balance_label.text = "Balance: %d cr" % current_credits

	if not _can_afford:
		_hint_label.text = "[Insufficient credits]  [Esc] Cancel"
		_hint_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.5))
	else:
		_hint_label.text = "[Enter] Confirm   [Esc] Cancel"
		_hint_label.add_theme_color_override("font_color", Color(0.7, 0.8, 1.0))


func can_confirm() -> bool:
	return _can_afford


func _build_ui() -> void:
	_panel = PanelContainer.new()
	_panel.set_anchors_preset(Control.PRESET_CENTER)
	_panel.custom_minimum_size = Vector2(300, 0)

	# Dark semi-transparent background.
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.08, 0.15, 0.92)
	style.border_color = Color(0.3, 0.5, 0.8, 0.8)
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	style.set_content_margin_all(16)
	_panel.add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	_panel.add_child(vbox)

	_dest_label = Label.new()
	_dest_label.add_theme_font_size_override("font_size", 22)
	_dest_label.add_theme_color_override("font_color", Color(0.9, 0.95, 1.0))
	_dest_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_dest_label)

	# Separator.
	var sep := HSeparator.new()
	sep.add_theme_color_override("separator", Color(0.3, 0.4, 0.6, 0.5))
	vbox.add_child(sep)

	_cost_label = Label.new()
	_cost_label.add_theme_font_size_override("font_size", 18)
	_cost_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_cost_label)

	_congestion_label = Label.new()
	_congestion_label.add_theme_font_size_override("font_size", 14)
	_congestion_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_congestion_label.visible = false
	vbox.add_child(_congestion_label)

	_balance_label = Label.new()
	_balance_label.add_theme_font_size_override("font_size", 14)
	_balance_label.add_theme_color_override("font_color", Color(0.6, 0.7, 0.8))
	_balance_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_balance_label)

	# Separator.
	var sep2 := HSeparator.new()
	sep2.add_theme_color_override("separator", Color(0.3, 0.4, 0.6, 0.5))
	vbox.add_child(sep2)

	_hint_label = Label.new()
	_hint_label.add_theme_font_size_override("font_size", 14)
	_hint_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hint_label.text = "[Enter] Confirm   [Esc] Cancel"
	_hint_label.add_theme_color_override("font_color", Color(0.7, 0.8, 1.0))
	vbox.add_child(_hint_label)

	add_child(_panel)
