# scripts/ui/risk_meter_widget.gd
# GATE.S7.RISK_METER_UI.WIDGET.001: HUD risk meter bars widget.
# Displays Heat / Influence / Trace as animated horizontal ProgressBars.
# Located in Zone G (bottom-left per HUD architecture).
# Reads from SimBridge.GetRiskMetersV0() every 0.5 seconds.
#
# GATE.S7.RISK_METER_UI.COMPOUND.001: Compound threat indicator.
# When 2+ meters exceed 0.7, shows a blinking "!! THREAT !!" badge.
extends Control

const POLL_INTERVAL: float = 0.5
const COMPOUND_THRESHOLD: float = 0.7
const BLINK_INTERVAL: float = 0.5

# Bar configuration: label letter, bar color, threshold key
const BAR_DEFS: Array = [
	{"key": "heat",      "label": "H", "color": Color(1.0, 0.15, 0.15)},   # Red
	{"key": "influence", "label": "I", "color": Color(0.4, 0.7, 1.0)},     # Blue
	{"key": "trace",     "label": "T", "color": Color(0.2, 1.0, 0.4)},     # Green
]

var _bridge: Node = null
var _bars: Array = []       # ProgressBar references
var _labels: Array = []     # Label references (H, I, T)
var _poll_elapsed: float = 0.0
var _compound_threat: bool = false
var _threat_badge: Label = null
var _blink_elapsed: float = 0.0
var _badge_visible: bool = true

# Tween animation duration for bar value changes
const TWEEN_DURATION: float = 0.3

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

	# Widget container: horizontal layout holding 3 mini bar groups
	custom_minimum_size = Vector2(240, 32)
	size = Vector2(240, 32)
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	var hbox := HBoxContainer.new()
	hbox.name = "RiskBarsHBox"
	hbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	hbox.add_theme_constant_override("separation", 8)
	hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(hbox)

	for i in range(BAR_DEFS.size()):
		var def: Dictionary = BAR_DEFS[i]

		# Each bar group: [Label] [ProgressBar]
		var group := HBoxContainer.new()
		group.name = "BarGroup_%s" % def["key"]
		group.add_theme_constant_override("separation", 3)
		group.mouse_filter = Control.MOUSE_FILTER_IGNORE
		hbox.add_child(group)

		# Letter label (H / I / T)
		var lbl := Label.new()
		lbl.name = "Lbl_%s" % def["key"]
		lbl.text = def["label"]
		lbl.add_theme_font_size_override("font_size", 11)
		lbl.add_theme_color_override("font_color", def["color"])
		lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		lbl.custom_minimum_size = Vector2(10, 0)
		lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
		group.add_child(lbl)
		_labels.append(lbl)

		# Progress bar
		var bar := ProgressBar.new()
		bar.name = "Bar_%s" % def["key"]
		bar.min_value = 0.0
		bar.max_value = 1.0
		bar.value = 0.0
		bar.show_percentage = false
		bar.custom_minimum_size = Vector2(52, 10)
		bar.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		bar.tooltip_text = "Calm"
		bar.mouse_filter = Control.MOUSE_FILTER_PASS

		# Style the fill color
		var fill_style := StyleBoxFlat.new()
		fill_style.bg_color = def["color"]
		fill_style.set_corner_radius_all(2)
		bar.add_theme_stylebox_override("fill", fill_style)

		# Style the background (dark track)
		var bg_style := StyleBoxFlat.new()
		bg_style.bg_color = Color(0.1, 0.1, 0.15, 0.8)
		bg_style.set_corner_radius_all(2)
		bar.add_theme_stylebox_override("background", bg_style)

		group.add_child(bar)
		_bars.append(bar)

	# Compound threat badge — appended after the three bar groups.
	_threat_badge = Label.new()
	_threat_badge.name = "CompoundThreatBadge"
	_threat_badge.text = "!! THREAT !!"
	_threat_badge.add_theme_font_size_override("font_size", 12)
	_threat_badge.add_theme_color_override("font_color", Color.WHITE)
	_threat_badge.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_threat_badge.custom_minimum_size = Vector2(80, 0)
	_threat_badge.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_threat_badge.visible = false

	# Red background panel for the badge.
	var badge_bg := StyleBoxFlat.new()
	badge_bg.bg_color = Color(0.85, 0.05, 0.05, 0.9)
	badge_bg.set_corner_radius_all(3)
	badge_bg.content_margin_left = 4.0
	badge_bg.content_margin_right = 4.0
	badge_bg.content_margin_top = 1.0
	badge_bg.content_margin_bottom = 1.0
	_threat_badge.add_theme_stylebox_override("normal", badge_bg)

	hbox.add_child(_threat_badge)

func _process(delta: float) -> void:
	if _bridge == null:
		return
	_poll_elapsed += delta
	if _poll_elapsed >= POLL_INTERVAL:
		_poll_elapsed = 0.0
		_update_meters()
	# Blink the compound threat badge when active.
	if _compound_threat and _threat_badge != null:
		_blink_elapsed += delta
		if _blink_elapsed >= BLINK_INTERVAL:
			_blink_elapsed = 0.0
			_badge_visible = not _badge_visible
			_threat_badge.visible = _badge_visible

func _update_meters() -> void:
	if _bridge == null or not _bridge.has_method("GetRiskMetersV0"):
		return

	var data: Dictionary = _bridge.call("GetRiskMetersV0")

	for i in range(BAR_DEFS.size()):
		var def: Dictionary = BAR_DEFS[i]
		var key: String = def["key"]
		var bar: ProgressBar = _bars[i]

		var new_val: float = float(data.get(key, 0.0))
		var threshold_name: String = str(data.get(key + "_threshold", "Calm"))

		# Update tooltip with threshold name
		bar.tooltip_text = "%s: %s" % [key.capitalize(), threshold_name]

		# Tween-animate value change (only if value actually changed)
		if not is_equal_approx(bar.value, new_val):
			var tw := create_tween()
			tw.tween_property(bar, "value", new_val, TWEEN_DURATION)\
				.set_ease(Tween.EASE_OUT)\
				.set_trans(Tween.TRANS_CUBIC)

	# Compound threat: 2+ meters above COMPOUND_THRESHOLD.
	var over_count: int = 0
	for def_i in range(BAR_DEFS.size()):
		var k: String = BAR_DEFS[def_i]["key"]
		if float(data.get(k, 0.0)) > COMPOUND_THRESHOLD:
			over_count += 1
	var new_compound: bool = over_count >= 2
	if new_compound != _compound_threat:
		_compound_threat = new_compound
		if _threat_badge != null:
			if _compound_threat:
				_badge_visible = true
				_blink_elapsed = 0.0
				_threat_badge.visible = true
			else:
				_threat_badge.visible = false


## Returns true when 2+ risk meters exceed the compound threat threshold (0.7).
func get_compound_threat() -> bool:
	return _compound_threat
