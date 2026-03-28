# scripts/ui/combat_hud.gd
# GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001: Zone armor bars + combat stance display.
# GATE.S7.COMBAT_DEPTH2.HUD.001: Tracking accuracy per weapon, armor pen indicator,
# pre-combat projection panel.
# GATE.T41.COMBAT.HUD_WIRE.001: Heat bar, weapon cooldowns, target HP, per-frame refresh.
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

# GATE.T41.COMBAT.HUD_WIRE.001: Heat bar + target HP display.
var _heat_bar: ProgressBar = null
var _heat_label: Label = null
var _heat_warning_label: Label = null
var _target_hp_container: HBoxContainer = null
var _target_hull_bar: ProgressBar = null
var _target_hull_label: Label = null
var _target_shield_bar: ProgressBar = null
var _target_shield_label: Label = null
var _target_name_label: Label = null

# Target lock indicator.
var _target_lock_label: Label = null

# GATE.T41.COMBAT.HUD_WIRE.001: Per-frame refresh throttle.
# Heat + target HP update every frame for responsiveness. Tracking + projection
# update on slow poll (refresh_v0, called every 2s from hud.gd).
var _fast_poll_elapsed: float = 0.0
const _FAST_POLL_INTERVAL: float = 0.1  # 10 Hz for heat/HP (smooth but not wasteful)

# GATE.T63.JUICE.DAMAGE_FLASH.001: Screen-edge red vignette on hull damage.
var _damage_vignette: TextureRect = null
var _damage_vignette_tween: Tween = null
var _prev_hull_hp: int = -1


func _ready() -> void:
	name = "CombatHud"
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_bridge = get_tree().root.get_node_or_null("SimBridge")
	_build_ui()

func _build_ui() -> void:
	# Position: bottom-center-left, above Zone G bar (y ~920-1020, x ~400)
	position = Vector2(380, 880)
	custom_minimum_size = Vector2(280, 100)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 2)
	add_child(vbox)

	# Stance indicator
	_stance_label = Label.new()
	_stance_label.text = "STANCE: --"
	_stance_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_stance_label.add_theme_color_override("font_color", UITheme.CYAN)
	vbox.add_child(_stance_label)

	# Target lock indicator — shows locked enemy fleet ID.
	_target_lock_label = Label.new()
	_target_lock_label.text = ""
	_target_lock_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_target_lock_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3, 1.0))
	_target_lock_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(_target_lock_label)

	# GATE.T41.COMBAT.HUD_WIRE.001: Heat bar (reads GetHeatSnapshotV0).
	var heat_hbox := HBoxContainer.new()
	heat_hbox.add_theme_constant_override("separation", 4)

	var heat_lbl := Label.new()
	heat_lbl.text = "HEAT"
	heat_lbl.custom_minimum_size = Vector2(40, 0)
	heat_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
	heat_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	heat_hbox.add_child(heat_lbl)

	_heat_bar = ProgressBar.new()
	_heat_bar.custom_minimum_size = Vector2(140, 14)
	_heat_bar.max_value = 100
	_heat_bar.value = 0
	_heat_bar.show_percentage = false
	var heat_fill := StyleBoxFlat.new()
	heat_fill.bg_color = UITheme.ORANGE
	_heat_bar.add_theme_stylebox_override("fill", heat_fill)
	var heat_bg := StyleBoxFlat.new()
	heat_bg.bg_color = Color(0.1, 0.12, 0.15, 0.8)
	_heat_bar.add_theme_stylebox_override("background", heat_bg)
	heat_hbox.add_child(_heat_bar)

	_heat_label = Label.new()
	_heat_label.text = "0%"
	_heat_label.custom_minimum_size = Vector2(40, 0)
	_heat_label.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
	_heat_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_heat_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	heat_hbox.add_child(_heat_label)

	vbox.add_child(heat_hbox)

	# Heat warning label (OVERHEATED / LOCKED OUT).
	_heat_warning_label = Label.new()
	_heat_warning_label.text = ""
	_heat_warning_label.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
	_heat_warning_label.add_theme_color_override("font_color", UITheme.RED)
	vbox.add_child(_heat_warning_label)

	# 4 zone armor bars: Fore, Port, Starboard, Aft
	for zone_name in ["Fore", "Port", "Stbd", "Aft"]:
		var hbox := HBoxContainer.new()
		hbox.add_theme_constant_override("separation", 4)

		var lbl := Label.new()
		lbl.text = zone_name.substr(0, 4).to_upper()
		lbl.custom_minimum_size = Vector2(40, 0)
		lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
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
		val_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
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

	# GATE.T41.COMBAT.HUD_WIRE.001: Target HP display (hull + shield bars).
	_target_name_label = Label.new()
	_target_name_label.text = ""
	_target_name_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_target_name_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	vbox.add_child(_target_name_label)

	_target_hp_container = HBoxContainer.new()
	_target_hp_container.add_theme_constant_override("separation", 4)
	_target_hp_container.visible = false

	# Target shield bar.
	var tgt_shield_lbl := Label.new()
	tgt_shield_lbl.text = "SH"
	tgt_shield_lbl.custom_minimum_size = Vector2(20, 0)
	tgt_shield_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
	tgt_shield_lbl.add_theme_color_override("font_color", Color(0.4, 0.6, 1.0))
	_target_hp_container.add_child(tgt_shield_lbl)

	_target_shield_bar = ProgressBar.new()
	_target_shield_bar.custom_minimum_size = Vector2(80, 12)
	_target_shield_bar.max_value = 100
	_target_shield_bar.value = 100
	_target_shield_bar.show_percentage = false
	var tgt_sh_fill := StyleBoxFlat.new()
	tgt_sh_fill.bg_color = Color(0.3, 0.5, 1.0)
	_target_shield_bar.add_theme_stylebox_override("fill", tgt_sh_fill)
	var tgt_sh_bg := StyleBoxFlat.new()
	tgt_sh_bg.bg_color = Color(0.08, 0.1, 0.15, 0.8)
	_target_shield_bar.add_theme_stylebox_override("background", tgt_sh_bg)
	_target_hp_container.add_child(_target_shield_bar)

	_target_shield_label = Label.new()
	_target_shield_label.text = ""
	_target_shield_label.custom_minimum_size = Vector2(30, 0)
	_target_shield_label.add_theme_font_size_override("font_size", 9)
	_target_shield_label.add_theme_color_override("font_color", Color(0.4, 0.6, 1.0))
	_target_shield_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_target_hp_container.add_child(_target_shield_label)

	# Target hull bar.
	var tgt_hull_lbl := Label.new()
	tgt_hull_lbl.text = "HP"
	tgt_hull_lbl.custom_minimum_size = Vector2(20, 0)
	tgt_hull_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
	tgt_hull_lbl.add_theme_color_override("font_color", UITheme.RED)
	_target_hp_container.add_child(tgt_hull_lbl)

	_target_hull_bar = ProgressBar.new()
	_target_hull_bar.custom_minimum_size = Vector2(80, 12)
	_target_hull_bar.max_value = 100
	_target_hull_bar.value = 100
	_target_hull_bar.show_percentage = false
	var tgt_hp_fill := StyleBoxFlat.new()
	tgt_hp_fill.bg_color = UITheme.RED
	_target_hull_bar.add_theme_stylebox_override("fill", tgt_hp_fill)
	var tgt_hp_bg := StyleBoxFlat.new()
	tgt_hp_bg.bg_color = Color(0.08, 0.1, 0.15, 0.8)
	_target_hull_bar.add_theme_stylebox_override("background", tgt_hp_bg)
	_target_hp_container.add_child(_target_hull_bar)

	_target_hull_label = Label.new()
	_target_hull_label.text = ""
	_target_hull_label.custom_minimum_size = Vector2(30, 0)
	_target_hull_label.add_theme_font_size_override("font_size", 9)
	_target_hull_label.add_theme_color_override("font_color", UITheme.RED)
	_target_hull_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_target_hp_container.add_child(_target_hull_label)

	vbox.add_child(_target_hp_container)

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

	# GATE.T63.JUICE.DAMAGE_FLASH.001: Full-screen damage vignette overlay.
	# Red edges with transparent center — flashes on hull damage.
	# Must be added to root viewport overlay, not combat_hud layout (which is positioned in-panel).
	_damage_vignette = TextureRect.new()
	_damage_vignette.name = "DamageVignette"
	# Generate a radial gradient: opaque red edges, transparent center.
	var grad_tex := GradientTexture2D.new()
	grad_tex.width = 512
	grad_tex.height = 512
	grad_tex.fill = GradientTexture2D.FILL_RADIAL
	grad_tex.fill_from = Vector2(0.5, 0.5)
	grad_tex.fill_to = Vector2(0.0, 0.0)
	var grad := Gradient.new()
	grad.set_color(0, Color(0, 0, 0, 0))       # Center: fully transparent
	grad.set_color(1, Color(0.9, 0.1, 0.05, 0.7))  # Edges: red
	grad.set_offset(0, 0.4)   # Transparent until 40% radius
	grad.set_offset(1, 1.0)
	grad_tex.gradient = grad
	_damage_vignette.texture = grad_tex
	_damage_vignette.stretch_mode = TextureRect.STRETCH_SCALE
	_damage_vignette.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_damage_vignette.modulate.a = 0.0
	_damage_vignette.visible = false
	# Deferred add to root CanvasLayer for full-screen coverage.
	call_deferred("_attach_vignette")


# GATE.T63.JUICE.DAMAGE_FLASH.001: Attach vignette to a CanvasLayer above all UI.
func _attach_vignette() -> void:
	if _damage_vignette == null:
		return
	var canvas := CanvasLayer.new()
	canvas.name = "DamageVignetteLayer"
	canvas.layer = 100  # Above all other UI layers
	canvas.add_child(_damage_vignette)
	_damage_vignette.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	get_tree().root.add_child(canvas)


# GATE.T63.JUICE.DAMAGE_FLASH.001: Flash vignette + camera shake proportional to damage.
func flash_damage_v0(damage_pct: float) -> void:
	if _damage_vignette == null:
		return
	# Intensity: 0.3 for light hits, up to 0.8 for heavy hits.
	var intensity: float = clampf(0.3 + damage_pct * 0.5, 0.3, 0.8)
	_damage_vignette.visible = true
	_damage_vignette.modulate.a = intensity
	if _damage_vignette_tween and _damage_vignette_tween.is_valid():
		_damage_vignette_tween.kill()
	_damage_vignette_tween = create_tween()
	_damage_vignette_tween.tween_property(_damage_vignette, "modulate:a", 0.0, 0.25) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	_damage_vignette_tween.tween_callback(func(): _damage_vignette.visible = false)
	# Camera shake — proportional to damage.
	var gm = get_tree().root.get_node_or_null("GameManager")
	if gm and gm.has_method("_apply_camera_shake_v0"):
		gm.call("_apply_camera_shake_v0", clampf(damage_pct * 0.8, 0.1, 0.6))


# GATE.T41.COMBAT.HUD_WIRE.001: Per-frame fast poll for combat-critical data.
# Heat and target HP need responsive updates; tracking/projection use slow poll.
func _process(delta: float) -> void:
	if not visible:
		return
	_fast_poll_elapsed += delta
	if _fast_poll_elapsed < _FAST_POLL_INTERVAL:
		return
	_fast_poll_elapsed = 0.0

	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("SimBridge")
	if _bridge == null:
		return

	_update_heat_v0()
	_update_target_hp_v0()
	_check_hull_damage_v0()
	_update_target_lock_v0()


# GATE.T63.JUICE.DAMAGE_FLASH.001: Detect hull HP decrease and trigger vignette.
func _check_hull_damage_v0() -> void:
	if _bridge == null or not _bridge.has_method("GetFleetCombatHpV0"):
		return
	var fleet: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
	if fleet.is_empty():
		return
	var hull_hp: int = int(fleet.get("hull", 0))
	var hull_max: int = int(fleet.get("hull_max", 100))
	if _prev_hull_hp < 0:
		_prev_hull_hp = hull_hp  # First frame — no flash
		return
	if hull_hp < _prev_hull_hp and hull_max > 0:
		var damage_amount: int = _prev_hull_hp - hull_hp
		var damage_pct: float = float(damage_amount) / float(hull_max)
		flash_damage_v0(damage_pct)
	_prev_hull_hp = hull_hp


# Target lock display: shows "[TARGET] fleet_id" when a target is locked.
func _update_target_lock_v0() -> void:
	if _target_lock_label == null:
		return
	if _bridge == null or not _bridge.has_method("GetLockedTargetV0"):
		_target_lock_label.text = ""
		return
	var target_id: String = str(_bridge.call("GetLockedTargetV0"))
	if target_id.is_empty():
		_target_lock_label.text = ""
	else:
		# Clean up fleet ID for display: "fleet_pirate_1" → "Pirate 1"
		var display_name: String = target_id.replace("fleet_", "").replace("_", " ").capitalize()
		_target_lock_label.text = "[TARGET] %s" % display_name


# GATE.T41.COMBAT.HUD_WIRE.001: Heat bar from GetHeatSnapshotV0.
func _update_heat_v0() -> void:
	if _heat_bar == null:
		return
	if _bridge == null or not _bridge.has_method("GetHeatSnapshotV0"):
		return

	var heat: Dictionary = _bridge.call("GetHeatSnapshotV0")
	var current: int = int(heat.get("heat_current", 0))
	var capacity: int = int(heat.get("heat_capacity", 100))
	var is_overheated: bool = heat.get("is_overheated", false)
	var is_locked_out: bool = heat.get("is_locked_out", false)

	_heat_bar.max_value = max(capacity, 1)
	_heat_bar.value = current
	var pct: float = 100.0 * float(current) / float(max(capacity, 1))

	if _heat_label:
		_heat_label.text = "%d%%" % int(pct)

	# Color the heat bar by severity.
	var heat_fill: StyleBoxFlat = _heat_bar.get_theme_stylebox("fill")
	if is_locked_out:
		heat_fill.bg_color = Color(1.0, 0.0, 0.0)  # Bright red: weapons locked
		if _heat_label:
			_heat_label.add_theme_color_override("font_color", UITheme.RED)
	elif is_overheated or pct > 80.0:
		heat_fill.bg_color = UITheme.RED
		if _heat_label:
			_heat_label.add_theme_color_override("font_color", UITheme.RED)
	elif pct > 50.0:
		heat_fill.bg_color = UITheme.ORANGE
		if _heat_label:
			_heat_label.add_theme_color_override("font_color", UITheme.ORANGE)
	else:
		heat_fill.bg_color = Color(0.2, 0.7, 1.0)  # Cool blue
		if _heat_label:
			_heat_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)

	# Warning text.
	if _heat_warning_label:
		if is_locked_out:
			_heat_warning_label.text = "WEAPONS LOCKED — OVERHEATED"
			_heat_warning_label.add_theme_color_override("font_color", UITheme.RED)
		elif is_overheated:
			_heat_warning_label.text = "OVERHEATED — COOLING"
			_heat_warning_label.add_theme_color_override("font_color", UITheme.ORANGE)
		else:
			_heat_warning_label.text = ""


# GATE.T41.COMBAT.HUD_WIRE.001: Target HP bars from GetCombatStatusV0 + GetFleetCombatHpV0.
func _update_target_hp_v0() -> void:
	if _target_hp_container == null:
		return
	if _bridge == null or not _bridge.has_method("GetCombatStatusV0"):
		_target_hp_container.visible = false
		return

	var status: Dictionary = _bridge.call("GetCombatStatusV0")
	var in_combat: bool = status.get("in_combat", false)
	var opponent_id: String = str(status.get("opponent_id", ""))

	if not in_combat or opponent_id.is_empty():
		_target_hp_container.visible = false
		if _target_name_label:
			_target_name_label.text = ""
		return

	# Show target name.
	if _target_name_label:
		# Strip fleet prefix for cleaner display.
		var display_id: String = opponent_id
		if display_id.begins_with("fleet_"):
			display_id = display_id.substr(6)
		_target_name_label.text = "TARGET: %s" % display_id.to_upper()

	# Get detailed HP via GetFleetCombatHpV0.
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", opponent_id)
		var hull: int = int(hp.get("hull", 0))
		var hull_max: int = int(hp.get("hull_max", 1))
		var shield: int = int(hp.get("shield", 0))
		var shield_max: int = int(hp.get("shield_max", 1))

		_target_hp_container.visible = true

		if _target_shield_bar:
			_target_shield_bar.max_value = max(shield_max, 1)
			_target_shield_bar.value = max(shield, 0)
		if _target_shield_label:
			_target_shield_label.text = "%d" % max(shield, 0)

		if _target_hull_bar:
			_target_hull_bar.max_value = max(hull_max, 1)
			_target_hull_bar.value = max(hull, 0)
		if _target_hull_label:
			_target_hull_label.text = "%d" % max(hull, 0)

		# Color hull bar by health percentage.
		var hull_pct: float = 100.0 * float(max(hull, 0)) / float(max(hull_max, 1))
		var hp_fill: StyleBoxFlat = _target_hull_bar.get_theme_stylebox("fill")
		if hull_pct > 60.0:
			hp_fill.bg_color = UITheme.GREEN
		elif hull_pct > 30.0:
			hp_fill.bg_color = UITheme.ORANGE
		else:
			hp_fill.bg_color = UITheme.RED
	else:
		# Fallback: use basic HP from combat status.
		var opp_hull: int = int(status.get("opponent_hull", 0))
		var opp_shield: int = int(status.get("opponent_shield", 0))
		_target_hp_container.visible = opp_hull > 0 or opp_shield > 0
		if _target_shield_label:
			_target_shield_label.text = "%d" % max(opp_shield, 0)
		if _target_hull_label:
			_target_hull_label.text = "%d" % max(opp_hull, 0)


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
	header.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
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
		name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
		name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		name_lbl.clip_text = true
		row.add_child(name_lbl)

		# Hit chance indicator.
		var hit_lbl := Label.new()
		hit_lbl.text = "%d%% hit" % hit_pct
		hit_lbl.custom_minimum_size = Vector2(50, 0)
		hit_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
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
		pen_lbl.add_theme_font_size_override("font_size", UITheme.FONT_MICRO)
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
