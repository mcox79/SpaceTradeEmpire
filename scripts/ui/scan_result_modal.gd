# GATE.T43.SCAN_UI.RESULT_MODAL.001: Scan result modal with progressive reveal animation.
# Center screen overlay showing scan results with typewriter + fade effects.
extends PanelContainer

var _category_label: Label = null
var _flavor_label: Label = null
var _hint_label: Label = null
var _stats_label: Label = null
var _dismiss_btn: Button = null
var _reveal_tween: Tween = null
var _full_flavor: String = ""
var _revealed: bool = false

# Category-specific accent colors.
const CATEGORY_COLORS := {
	"ResourceIntel": Color(0.3, 0.6, 1.0),
	"SignalLead": Color(0.9, 0.85, 1.0),
	"PhysicalEvidence": Color(1.0, 0.6, 0.2),
	"FragmentCache": Color(0.2, 1.0, 0.4),
	"DataArchive": Color(0.4, 0.85, 1.0),
}

func _ready() -> void:
	# Center positioning.
	anchor_left = 0.5
	anchor_right = 0.5
	anchor_top = 0.3
	anchor_bottom = 0.3
	offset_left = -220
	offset_right = 220
	offset_top = -120
	offset_bottom = 120
	visible = false
	z_index = 50

	var style := UITheme.make_panel_modal()
	add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	add_child(vbox)

	_category_label = Label.new()
	_category_label.name = "CategoryLabel"
	_category_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_category_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	vbox.add_child(_category_label)

	_flavor_label = Label.new()
	_flavor_label.name = "FlavorLabel"
	_flavor_label.text = ""
	_flavor_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_flavor_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_flavor_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(_flavor_label)

	_hint_label = Label.new()
	_hint_label.name = "HintLabel"
	_hint_label.text = ""
	_hint_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_hint_label.add_theme_color_override("font_color", UITheme.GOLD)
	_hint_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_hint_label.modulate.a = 0.0
	vbox.add_child(_hint_label)

	var sep := HSeparator.new()
	vbox.add_child(sep)

	_stats_label = Label.new()
	_stats_label.name = "StatsLabel"
	_stats_label.text = ""
	_stats_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_stats_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_stats_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_stats_label.modulate.a = 0.0
	vbox.add_child(_stats_label)

	_dismiss_btn = Button.new()
	_dismiss_btn.text = "DISMISS"
	_dismiss_btn.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_dismiss_btn.pressed.connect(dismiss)
	vbox.add_child(_dismiss_btn)

	# Click anywhere to skip animation or dismiss.
	gui_input.connect(_on_gui_input)


func show_result(scan: Dictionary) -> void:
	if scan.is_empty():
		return
	visible = true
	_revealed = false

	var category: String = str(scan.get("category", ""))
	var mode: String = str(scan.get("mode", ""))
	var phase: String = str(scan.get("phase", ""))
	var flavor: String = str(scan.get("flavor_text", ""))
	var hint: String = str(scan.get("hint_text", ""))
	var affinity_bps: int = int(scan.get("affinity_bps", 10000))
	var remaining: int = int(scan.get("remaining_charges", -1))
	var max_charges: int = int(scan.get("max_charges", -1))

	var accent: Color = CATEGORY_COLORS.get(category, UITheme.CYAN)

	# Category header — immediate.
	var icon: String = _category_icon(category)
	_category_label.text = "%s  %s" % [icon, category.to_upper()]
	_category_label.add_theme_color_override("font_color", accent)
	_category_label.modulate.a = 0.0

	# Prepare flavor text for typewriter.
	_full_flavor = flavor
	_flavor_label.text = ""

	# Hint text.
	_hint_label.text = hint if not hint.is_empty() else ""
	_hint_label.modulate.a = 0.0
	_hint_label.visible = not hint.is_empty()

	# Stats line.
	var stats_parts: Array = ["Mode: %s" % mode, "Affinity: %.1fx" % (float(affinity_bps) / 10000.0), "Phase: %s" % phase]
	if remaining >= 0 and max_charges >= 0:
		stats_parts.append("Charges: %d/%d" % [remaining, max_charges])
	_stats_label.text = "  ·  ".join(stats_parts)
	_stats_label.modulate.a = 0.0

	# Progressive reveal animation.
	if _reveal_tween and _reveal_tween.is_running():
		_reveal_tween.kill()

	_reveal_tween = create_tween()
	# 1. Category slides in.
	_reveal_tween.tween_property(_category_label, "modulate:a", 1.0, 0.3)
	# 2. Flavor text typewriter (40 chars/s).
	var typewriter_duration: float = max(0.5, float(flavor.length()) / 40.0)
	_reveal_tween.tween_method(_typewriter_update, 0, flavor.length(), typewriter_duration)
	# 3. Hint fades in (0.5s delay after flavor).
	if not hint.is_empty():
		_reveal_tween.tween_interval(0.5)
		_reveal_tween.tween_property(_hint_label, "modulate:a", 1.0, 0.3)
	# 4. Stats fade in.
	_reveal_tween.tween_property(_stats_label, "modulate:a", 1.0, 0.3)
	_reveal_tween.tween_callback(func(): _revealed = true)

	# Auto-dismiss after 8s.
	var auto_dismiss := create_tween()
	auto_dismiss.tween_interval(8.0)
	auto_dismiss.tween_callback(dismiss)


func _typewriter_update(char_count: int) -> void:
	_flavor_label.text = "\"%s\"" % _full_flavor.substr(0, char_count)


func _category_icon(category: String) -> String:
	match category:
		"ResourceIntel": return "◆"
		"SignalLead": return "◈"
		"PhysicalEvidence": return "▣"
		"FragmentCache": return "◇"
		"DataArchive": return "▦"
	return "◉"


func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		if not _revealed:
			# Skip animation — show everything immediately.
			if _reveal_tween and _reveal_tween.is_running():
				_reveal_tween.kill()
			_category_label.modulate.a = 1.0
			_flavor_label.text = "\"%s\"" % _full_flavor
			_hint_label.modulate.a = 1.0
			_stats_label.modulate.a = 1.0
			_revealed = true
		else:
			dismiss()


func dismiss() -> void:
	if _reveal_tween and _reveal_tween.is_running():
		_reveal_tween.kill()
	visible = false
