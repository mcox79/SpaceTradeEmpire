# scripts/ui/warfront_panel.gd
# GATE.S7.UI_WARFRONT.DASHBOARD.001: Warfront dashboard panel (N key).
# Shows active warfront conflicts, territory control %, faction strength,
# strategic objective status.
extends PanelContainer

var _bridge = null
var _list_container: VBoxContainer = null
var _stats_label: Label = null

func _ready() -> void:
	name = "WarfrontPanel"
	visible = false
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -360
	offset_right = 360
	offset_top = -280
	offset_bottom = 280
	custom_minimum_size = Vector2(720, 560)
	var style := UITheme.make_panel_standard(UITheme.BORDER_DANGER)
	add_theme_stylebox_override("panel", style)

	var root_vbox := VBoxContainer.new()
	root_vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	add_child(root_vbox)

	# Header row.
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_LG)
	root_vbox.add_child(header)

	var title := Label.new()
	title.text = "WARFRONT DASHBOARD"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.RED_LIGHT)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(title)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.pressed.connect(func(): toggle_v0())
	header.add_child(close_btn)

	# Stats bar.
	_stats_label = Label.new()
	_stats_label.text = ""
	_stats_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_stats_label.add_theme_color_override("font_color", UITheme.GOLD)
	root_vbox.add_child(_stats_label)

	# Separator.
	root_vbox.add_child(HSeparator.new())

	# Scrollable content.
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root_vbox.add_child(scroll)

	_list_container = VBoxContainer.new()
	_list_container.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_list_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_list_container)

	root_vbox.add_child(UITheme.make_dismiss_hint("N"))

	_bridge = get_node_or_null("/root/SimBridge")

func toggle_v0() -> void:
	if visible:
		UITheme.animate_close(self, func(): visible = false)
	else:
		visible = true
		UITheme.animate_open(self)
		_refresh()

func _refresh() -> void:
	for child in _list_container.get_children():
		child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")

	if _bridge == null or not _bridge.has_method("GetWarfrontsV0"):
		_show_empty_state("No warfront data available.")
		_stats_label.text = ""
		return

	var warfronts: Array = _bridge.call("GetWarfrontsV0")
	if warfronts.size() == 0:
		_show_empty_state("No active warfronts.")
		_stats_label.text = "Galaxy at peace"
		return

	_stats_label.text = "%d active warfront%s" % [warfronts.size(), "s" if warfronts.size() > 1 else ""]

	for wf in warfronts:
		_add_warfront_section(wf)

func _show_empty_state(msg: String) -> void:
	_list_container.add_child(UITheme.make_empty_state("⚔", msg, "Warfronts emerge from faction conflicts"))

func _add_warfront_section(wf: Dictionary) -> void:
	var wf_id: String = str(wf.get("id", "unknown"))
	var combatant_a: String = str(wf.get("combatant_a", "?"))
	var combatant_b: String = str(wf.get("combatant_b", "?"))
	var intensity: int = int(wf.get("intensity", 0))
	var intensity_label: String = str(wf.get("intensity_label", "Peace"))
	var war_type: String = str(wf.get("war_type", "Hot"))
	var contested: int = int(wf.get("contested_count", 0))

	# Section header: combatants.
	var header := Label.new()
	header.text = "%s vs %s  [%s]" % [combatant_a.to_upper(), combatant_b.to_upper(), war_type]
	header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	header.add_theme_color_override("font_color", _intensity_color(intensity))
	_list_container.add_child(header)

	# Intensity row.
	var intensity_row := HBoxContainer.new()
	intensity_row.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_list_container.add_child(intensity_row)

	var int_lbl := Label.new()
	int_lbl.text = "Intensity: %s (%d/4)" % [intensity_label, intensity]
	int_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	int_lbl.add_theme_color_override("font_color", _intensity_color(intensity))
	intensity_row.add_child(int_lbl)

	# Contested nodes.
	var contested_lbl := Label.new()
	contested_lbl.text = "   Contested nodes: %d" % contested
	contested_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	contested_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	intensity_row.add_child(contested_lbl)

	# Faction strength bars (read from warfront data if available).
	_add_strength_row(combatant_a, wf)
	_add_strength_row(combatant_b, wf)

	# Supply status.
	if _bridge != null and _bridge.has_method("GetWarSupplyV0"):
		var supply: Dictionary = _bridge.call("GetWarSupplyV0", wf_id)
		var progress: int = int(supply.get("shift_progress_pct", 0))
		var supply_lbl := Label.new()
		supply_lbl.text = "Supply progress: %d%%" % progress
		supply_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		if progress >= 80:
			supply_lbl.add_theme_color_override("font_color", UITheme.GREEN)
		elif progress >= 40:
			supply_lbl.add_theme_color_override("font_color", UITheme.YELLOW)
		else:
			supply_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		_list_container.add_child(supply_lbl)

	# Separator between warfronts.
	_list_container.add_child(HSeparator.new())

func _add_strength_row(faction_name: String, wf: Dictionary) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_list_container.add_child(hbox)

	var name_lbl := Label.new()
	name_lbl.text = faction_name
	name_lbl.custom_minimum_size = Vector2(100, 0)
	name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	hbox.add_child(name_lbl)

	var bar := ProgressBar.new()
	bar.custom_minimum_size = Vector2(200, 16)
	bar.max_value = 100
	bar.show_percentage = false
	hbox.add_child(bar)

	var val_lbl := Label.new()
	val_lbl.custom_minimum_size = Vector2(50, 0)
	val_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	val_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	hbox.add_child(val_lbl)

	# Determine which combatant this is (A or B).
	var combatant_a: String = str(wf.get("combatant_a", ""))
	var strength: int = 50
	if faction_name == combatant_a:
		# Try to get fleet strength if available; use 50 as fallback.
		strength = int(wf.get("fleet_strength_a", 50))
	else:
		strength = int(wf.get("fleet_strength_b", 50))

	bar.value = strength
	val_lbl.text = "%d%%" % strength

	# Color by strength level.
	var fill := StyleBoxFlat.new()
	if strength > 60:
		fill.bg_color = UITheme.GREEN
		val_lbl.add_theme_color_override("font_color", UITheme.GREEN)
	elif strength > 30:
		fill.bg_color = UITheme.ORANGE
		val_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
	else:
		fill.bg_color = UITheme.RED
		val_lbl.add_theme_color_override("font_color", UITheme.RED)
	bar.add_theme_stylebox_override("fill", fill)

	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0.1, 0.12, 0.15, 0.8)
	bar.add_theme_stylebox_override("background", bg)

func _intensity_color(level: int) -> Color:
	match level:
		0: return UITheme.TEXT_DISABLED
		1: return UITheme.YELLOW
		2: return UITheme.ORANGE
		3: return UITheme.RED_LIGHT
		4: return UITheme.RED
		_: return UITheme.TEXT_MUTED
