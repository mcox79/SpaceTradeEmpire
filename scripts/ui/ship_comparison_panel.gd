# scripts/ui/ship_comparison_panel.gd
# GATE.T59.SHIP.COMPARISON_PANEL.001: Side-by-side ship comparison overlay
# Shows current ship vs target ship stats with delta indicators.
# This is a full-rect overlay Control: scrim fills parent, centered panel on top.
extends Control

var _bridge: Node = null
var _panel: PanelContainer = null
var _vbox: VBoxContainer = null
var _stats_container: VBoxContainer = null
var _title_a: Label = null
var _title_b: Label = null
var _close_btn: Button = null

# Stat display order and labels
const STAT_ROWS := [
	{"key": "core_hull", "label": "Hull", "higher_better": true},
	{"key": "base_shield", "label": "Shield", "higher_better": true},
	{"key": "cargo_capacity", "label": "Cargo", "higher_better": true},
	{"key": "base_power", "label": "Power", "higher_better": true},
	{"key": "slot_count", "label": "Slots", "higher_better": true},
	{"key": "mass", "label": "Mass", "higher_better": false},
	{"key": "scan_range", "label": "Scan Range", "higher_better": true},
	{"key": "base_fuel_capacity", "label": "Fuel", "higher_better": true},
	{"key": "price", "label": "Price", "higher_better": false},
]

func _ready():
	visible = false
	mouse_filter = Control.MOUSE_FILTER_STOP
	set_anchors_preset(Control.PRESET_FULL_RECT)

	# Semi-transparent dark background overlay (scrim) — fills entire parent
	var scrim := ColorRect.new()
	scrim.color = Color(0.0, 0.0, 0.0, 0.7)
	scrim.set_anchors_preset(Control.PRESET_FULL_RECT)
	scrim.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(scrim)

	# Centered panel container
	_panel = PanelContainer.new()
	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_ship_computer(UITheme.BORDER_ACCENT))
	_panel.set_anchors_preset(Control.PRESET_CENTER)
	_panel.custom_minimum_size = Vector2(520, 0)
	_panel.offset_left = -260
	_panel.offset_right = 260
	_panel.offset_top = -220
	_panel.offset_bottom = 220
	add_child(_panel)

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_panel.add_child(_vbox)

	# Header
	var header_section := UITheme.make_section_header("Ship Comparison", UITheme.CYAN)
	_vbox.add_child(header_section)

	# Column headers: Ship A name | stat label | Ship B name
	var name_row := HBoxContainer.new()
	name_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_title_a = Label.new()
	_title_a.text = "Current Ship"
	_title_a.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_title_a.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_a.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_title_a.add_theme_color_override("font_color", UITheme.CYAN)
	name_row.add_child(_title_a)

	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(100, 0)
	name_row.add_child(spacer)

	_title_b = Label.new()
	_title_b.text = "Target Ship"
	_title_b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_title_b.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_b.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_title_b.add_theme_color_override("font_color", UITheme.GOLD)
	name_row.add_child(_title_b)
	_vbox.add_child(name_row)

	_vbox.add_child(HSeparator.new())

	# Stats rows container
	_stats_container = VBoxContainer.new()
	_stats_container.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_stats_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_vbox.add_child(_stats_container)

	# Close button
	_close_btn = Button.new()
	_close_btn.text = "Close"
	_close_btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	UITheme.style_action_button(_close_btn, "neutral")
	_close_btn.pressed.connect(_on_close)
	_vbox.add_child(_close_btn)


func show_comparison(bridge_ref: Node, class_id_a: String, class_id_b: String) -> void:
	_bridge = bridge_ref
	if _bridge == null:
		print("DEBUG_COMPARE_ no bridge ref")
		return

	var result: Dictionary = _bridge.call("GetShipComparisonV0", class_id_a, class_id_b)
	if result.is_empty():
		print("DEBUG_COMPARE_ empty comparison result for %s vs %s" % [class_id_a, class_id_b])
		return

	var a_stats: Dictionary = result.get("a", {})
	var b_stats: Dictionary = result.get("b", {})
	var deltas: Dictionary = result.get("deltas", {})

	# Update titles
	_title_a.text = str(a_stats.get("display_name", "Current"))
	_title_b.text = str(b_stats.get("display_name", "Target"))

	# Clear old rows
	for child in _stats_container.get_children():
		child.queue_free()

	# Build stat rows
	for stat_def in STAT_ROWS:
		var key: String = stat_def["key"]
		var label_text: String = stat_def["label"]
		var higher_better: bool = stat_def["higher_better"]

		var val_a = a_stats.get(key, 0)
		var val_b = b_stats.get(key, 0)
		var delta = deltas.get(key, 0)

		var row := HBoxContainer.new()
		row.size_flags_horizontal = Control.SIZE_EXPAND_FILL

		# Value A
		var lbl_val_a := Label.new()
		lbl_val_a.text = _format_stat(key, val_a)
		lbl_val_a.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		lbl_val_a.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		lbl_val_a.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		lbl_val_a.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		UITheme.apply_mono(lbl_val_a)
		row.add_child(lbl_val_a)

		# Stat name (center)
		var lbl_name := Label.new()
		lbl_name.text = label_text
		lbl_name.custom_minimum_size = Vector2(100, 0)
		lbl_name.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl_name.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		lbl_name.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		row.add_child(lbl_name)

		# Delta arrow + Value B
		var lbl_val_b := Label.new()
		var delta_f: float = float(delta)
		var arrow := ""
		var delta_color := UITheme.TEXT_PRIMARY
		if abs(delta_f) > 0.001:
			var is_improvement: bool = (delta_f > 0) == higher_better
			if is_improvement:
				arrow = " ▲"
				delta_color = UITheme.GREEN
			else:
				arrow = " ▼"
				delta_color = UITheme.RED_LIGHT
		lbl_val_b.text = _format_stat(key, val_b) + arrow
		lbl_val_b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		lbl_val_b.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
		lbl_val_b.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		lbl_val_b.add_theme_color_override("font_color", delta_color)
		UITheme.apply_mono(lbl_val_b)
		row.add_child(lbl_val_b)

		_stats_container.add_child(row)

	visible = true
	print("DEBUG_COMPARE_ showing comparison: %s vs %s" % [_title_a.text, _title_b.text])


func _format_stat(key: String, value) -> String:
	if key == "price":
		return UITheme.fmt_credits(int(value))
	return str(value)


func _on_close() -> void:
	visible = false
