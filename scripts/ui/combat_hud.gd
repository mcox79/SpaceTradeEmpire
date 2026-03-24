# scripts/ui/combat_hud.gd
# GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001: Zone armor bars + combat stance display.
# GATE.S7.COMBAT_DEPTH2.HUD.001: Tracking accuracy per weapon, armor pen indicator,
# pre-combat projection panel.
# Fixes C8 (zone armor invisible) and C9 (no combat HUD).
extends Control

var _bridge: Object = null
var _zone_bars: Dictionary = {}  # "fore"/"port"/"stbd"/"aft" -> { "bar": ProgressBar, "label": Label }
var _stance_label: Label = null
# GATE.S7.COMBAT_PHASE2.ZONE_HUD.001: Spin RPM + radiator status display.
var _spin_label: Label = null
var _radiator_label: Label = null
# GATE.S7.COMBAT_DEPTH2.HUD.001: Weapon tracking + projection displays.
var _tracking_container: VBoxContainer = null
var _projection_label: Label = null
# GATE.S5.LOSS_RECOVERY.CAPTURE_UI.001: Capturable targets display.
var _capture_container: VBoxContainer = null


func _ready() -> void:
	name = "CombatHud"
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_bridge = get_tree().root.get_node_or_null("SimBridge")
	_build_ui()

func _build_ui() -> void:
	# Position: bottom-center-left, above Zone G bar (y ~920-1020, x ~400)
	position = Vector2(380, 920)
	custom_minimum_size = Vector2(260, 100)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 2)
	add_child(vbox)

	# Stance indicator
	_stance_label = Label.new()
	_stance_label.text = "STANCE: --"
	_stance_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_stance_label.add_theme_color_override("font_color", UITheme.CYAN)
	vbox.add_child(_stance_label)

	# 4 zone armor bars: Fore, Port, Starboard, Aft
	for zone_name in ["Fore", "Port", "Stbd", "Aft"]:
		var hbox := HBoxContainer.new()
		hbox.add_theme_constant_override("separation", 4)

		var lbl := Label.new()
		lbl.text = zone_name.substr(0, 4).to_upper()
		lbl.custom_minimum_size = Vector2(40, 0)
		lbl.add_theme_font_size_override("font_size", 10)
		lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		hbox.add_child(lbl)

		var bar := ProgressBar.new()
		bar.custom_minimum_size = Vector2(140, 14)
		bar.max_value = 100
		bar.value = 100
		bar.show_percentage = false
		# Style the bar
		var fill := StyleBoxFlat.new()
		fill.bg_color = UITheme.CYAN
		bar.add_theme_stylebox_override("fill", fill)
		var bg := StyleBoxFlat.new()
		bg.bg_color = Color(0.1, 0.12, 0.15, 0.8)
		bar.add_theme_stylebox_override("background", bg)
		hbox.add_child(bar)

		var val_lbl := Label.new()
		val_lbl.text = "100%"
		val_lbl.custom_minimum_size = Vector2(40, 0)
		val_lbl.add_theme_font_size_override("font_size", 10)
		val_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		val_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		hbox.add_child(val_lbl)

		vbox.add_child(hbox)
		_zone_bars[zone_name.to_lower()] = {"bar": bar, "label": val_lbl}

	# GATE.S7.COMBAT_PHASE2.ZONE_HUD.001: Spin RPM indicator.
	_spin_label = Label.new()
	_spin_label.text = "SPIN: 0 RPM"
	_spin_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_spin_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	vbox.add_child(_spin_label)

	# GATE.S7.COMBAT_PHASE2.ZONE_HUD.001: Radiator status.
	_radiator_label = Label.new()
	_radiator_label.text = ""
	_radiator_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_radiator_label.add_theme_color_override("font_color", UITheme.GREEN)
	vbox.add_child(_radiator_label)

	# GATE.S7.COMBAT_DEPTH2.HUD.001: Pre-combat projection display.
	_projection_label = Label.new()
	_projection_label.text = ""
	_projection_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_projection_label.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	vbox.add_child(_projection_label)

	# GATE.S7.COMBAT_DEPTH2.HUD.001: Weapon tracking details container.
	_tracking_container = VBoxContainer.new()
	_tracking_container.add_theme_constant_override("separation", 1)
	vbox.add_child(_tracking_container)

	# GATE.S5.LOSS_RECOVERY.CAPTURE_UI.001: Capturable targets section.
	_capture_container = VBoxContainer.new()
	_capture_container.add_theme_constant_override("separation", 2)
	vbox.add_child(_capture_container)

func refresh_v0() -> void:
	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("SimBridge")
	if _bridge == null:
		return

	# Zone armor
	if _bridge.has_method("GetPlayerShipFittingV0"):
		var fit: Dictionary = _bridge.call("GetPlayerShipFittingV0")
		_update_zone("fore", int(fit.get("zone_fore", 0)), int(fit.get("zone_fore_max", 1)))
		_update_zone("port", int(fit.get("zone_port", 0)), int(fit.get("zone_port_max", 1)))
		_update_zone("stbd", int(fit.get("zone_stbd", 0)), int(fit.get("zone_stbd_max", 1)))
		_update_zone("aft", int(fit.get("zone_aft", 0)), int(fit.get("zone_aft_max", 1)))

	# GATE.S7.COMBAT_PHASE2.ZONE_HUD.001: Spin RPM + radiator status.
	if _bridge.has_method("GetSpinStateV0"):
		var spin: Dictionary = _bridge.call("GetSpinStateV0")
		var rpm: int = spin.get("spin_rpm", 0)
		var penalty_bps: int = spin.get("turn_penalty_bps", 0)
		if _spin_label:
			_spin_label.text = "SPIN: %d RPM (%d%% penalty)" % [rpm, int(penalty_bps / 100.0)]
			if penalty_bps > 3000:
				_spin_label.add_theme_color_override("font_color", UITheme.RED)
			elif penalty_bps > 1000:
				_spin_label.add_theme_color_override("font_color", UITheme.ORANGE)
			else:
				_spin_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)

	if _bridge.has_method("GetRadiatorStatusV0"):
		var rad: Dictionary = _bridge.call("GetRadiatorStatusV0")
		var intact: bool = rad.get("is_intact", true)
		var bonus: int = rad.get("bonus_rate", 0)
		if _radiator_label:
			if bonus > 0:
				if intact:
					_radiator_label.text = "RAD: +%d cooling" % bonus
					_radiator_label.add_theme_color_override("font_color", UITheme.GREEN)
				else:
					_radiator_label.text = "RAD: DESTROYED"
					_radiator_label.add_theme_color_override("font_color", UITheme.RED)
			else:
				_radiator_label.text = ""

	# Stance
	if _bridge.has_method("GetDoctrineSettingsV0"):
		var doc: Dictionary = _bridge.call("GetDoctrineSettingsV0", "fleet_trader_1")
		var stance: String = str(doc.get("stance", "Unknown"))
		_stance_label.text = "STANCE: %s" % stance.to_upper()
		match stance.to_lower():
			"aggressive": _stance_label.add_theme_color_override("font_color", UITheme.RED)
			"defensive": _stance_label.add_theme_color_override("font_color", UITheme.ORANGE)
			"evasive": _stance_label.add_theme_color_override("font_color", UITheme.GREEN)
			_: _stance_label.add_theme_color_override("font_color", UITheme.CYAN)

	# GATE.S7.COMBAT_DEPTH2.HUD.001: Pre-combat projection.
	_update_projection_v0()

	# GATE.S7.COMBAT_DEPTH2.HUD.001: Weapon tracking details.
	_update_tracking_v0()

	# GATE.S5.LOSS_RECOVERY.CAPTURE_UI.001: Capturable targets.

# GATE.S7.COMBAT_DEPTH2.HUD.001: Show combat projection against current target.
func _update_projection_v0() -> void:
	if _projection_label == null:
		return
	if _bridge == null or not _bridge.has_method("GetCombatStatusV0"):
		_projection_label.text = ""
		return

	var status: Dictionary = _bridge.call("GetCombatStatusV0")
	var in_combat: bool = status.get("in_combat", false)
	var opponent_id: String = str(status.get("opponent_id", ""))

	if not in_combat or opponent_id.is_empty():
		_projection_label.text = ""
		return

	if not _bridge.has_method("GetCombatProjectionV0"):
		_projection_label.text = ""
		return

	var proj: Dictionary = _bridge.call("GetCombatProjectionV0", "fleet_trader_1", opponent_id)
	var outcome: String = str(proj.get("outcome", "stalemate"))
	var atk_loss: int = int(proj.get("attacker_loss_pct", 0))
	var def_loss: int = int(proj.get("defender_loss_pct", 0))
	var rounds: int = int(proj.get("estimated_rounds", 0))

	_projection_label.text = "PROJ: %s  You:-%d%%  Them:-%d%%  ~%d rds" % [
		outcome.to_upper(), atk_loss, def_loss, rounds
	]

	match outcome:
		"victory": _projection_label.add_theme_color_override("font_color", UITheme.GREEN)
		"defeat": _projection_label.add_theme_color_override("font_color", UITheme.RED)
		"pyrrhic": _projection_label.add_theme_color_override("font_color", UITheme.ORANGE)
		_: _projection_label.add_theme_color_override("font_color", UITheme.YELLOW)

# GATE.S7.COMBAT_DEPTH2.HUD.001: Show per-weapon tracking accuracy and armor pen.
func _update_tracking_v0() -> void:
	if _tracking_container == null:
		return

	# Clear old rows.
	for child in _tracking_container.get_children():
		child.queue_free()

	if _bridge == null or not _bridge.has_method("GetWeaponTrackingV0"):
		return

	var weapons: Array = _bridge.call("GetWeaponTrackingV0", "fleet_trader_1")
	if weapons.size() == 0:
		return

	# Header row.
	var header := Label.new()
	header.text = "WEAPON TRACKING"
	header.add_theme_font_size_override("font_size", 10)
	header.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_tracking_container.add_child(header)

	for wep in weapons:
		var _slot_id: String = str(wep.get("slot_id", "?"))
		var module_id: String = str(wep.get("module_id", "?"))
		var hit_pct: int = int(wep.get("hit_pct", 0))
		var armor_pen_bps: int = int(wep.get("armor_pen_bps", 0))

		# Use short module name (strip prefix).
		var display_name: String = module_id
		if display_name.begins_with("weapon_"):
			display_name = display_name.substr(7)

		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 4)

		var name_lbl := Label.new()
		name_lbl.text = display_name
		name_lbl.custom_minimum_size = Vector2(90, 0)
		name_lbl.add_theme_font_size_override("font_size", 10)
		name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		name_lbl.clip_text = true
		row.add_child(name_lbl)

		# Hit chance indicator.
		var hit_lbl := Label.new()
		hit_lbl.text = "%d%% hit" % hit_pct
		hit_lbl.custom_minimum_size = Vector2(50, 0)
		hit_lbl.add_theme_font_size_override("font_size", 10)
		if hit_pct >= 70:
			hit_lbl.add_theme_color_override("font_color", UITheme.GREEN)
		elif hit_pct >= 40:
			hit_lbl.add_theme_color_override("font_color", UITheme.YELLOW)
		else:
			hit_lbl.add_theme_color_override("font_color", UITheme.RED)
		row.add_child(hit_lbl)

		# Armor pen indicator.
		var pen_pct: int = int(armor_pen_bps / 100.0)
		var pen_lbl := Label.new()
		pen_lbl.text = "%d%% pen" % pen_pct
		pen_lbl.custom_minimum_size = Vector2(50, 0)
		pen_lbl.add_theme_font_size_override("font_size", 10)
		if pen_pct >= 50:
			pen_lbl.add_theme_color_override("font_color", UITheme.CYAN)
		else:
			pen_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		row.add_child(pen_lbl)

		_tracking_container.add_child(row)

func _update_zone(zone: String, hp: int, hp_max: int) -> void:
	if not _zone_bars.has(zone):
		return
	var entry: Dictionary = _zone_bars[zone]
	var bar: ProgressBar = entry["bar"]
	var lbl: Label = entry["label"]
	bar.max_value = max(hp_max, 1)
	bar.value = hp
	var pct: float = 100.0 * float(hp) / float(max(hp_max, 1))
	lbl.text = "%d%%" % int(pct)
	# Color by health
	var fill: StyleBoxFlat = bar.get_theme_stylebox("fill")
	if pct > 60:
		fill.bg_color = UITheme.CYAN
		lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	elif pct > 30:
		fill.bg_color = UITheme.ORANGE
		lbl.add_theme_color_override("font_color", UITheme.ORANGE)
	else:
		fill.bg_color = UITheme.RED
		lbl.add_theme_color_override("font_color", UITheme.RED)

# GATE.S5.LOSS_RECOVERY.CAPTURE_UI.001: Show capturable NPC targets (hull < 10%).
