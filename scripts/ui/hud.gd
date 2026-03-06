extends CanvasLayer

@onready var _credits_label: Label = $CreditsLabel
@onready var _cargo_label: Label = $CargoLabel
@onready var _node_label: Label = $NodeLabel
@onready var _state_label: Label = $StateLabel
@onready var _hull_bar: ProgressBar = $HullBar
@onready var _shield_bar: ProgressBar = $ShieldBar
@onready var _hull_label: Label = $HullLabel
@onready var _shield_label: Label = $ShieldLabel

var _bridge = null
var _combat_label: Label = null

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: game over overlay
var _game_over_panel: Control = null
var _game_over_label: Label = null
var _restart_label: Label = null

# GATE.S1.MISSION.HUD.001: mission objective panel
var _mission_panel: PanelContainer = null
var _mission_title_label: Label = null
var _mission_step_label: Label = null

# GATE.S3.RISK_SINKS.BRIDGE.001: delay status label
var _delay_label: Label = null

# GATE.S5.SEC_LANES.UI.001: security band indicator
var _security_label: Label = null

# GATE.S11.GAME_FEEL.RESEARCH_HUD.001: research progress label
var _research_label: Label = null

# Timer accumulator for slow-poll HUD sections (mission + research)
var _slow_poll_elapsed: float = 0.0
const _SLOW_POLL_INTERVAL: float = 2.0

# GATE.S1.SAVE_UI.PAUSE_MENU.001: pause menu overlay
var _pause_panel: Control = null
# GATE.S1.SAVE_UI.SLOTS.001: save slot labels for metadata display
var _slot_labels: Array = []

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

	# HUD status panel background (dark navy, matches UITheme.PANEL_BG)
	var hud_bg := ColorRect.new()
	hud_bg.name = "HudStatusBg"
	hud_bg.color = UITheme.PANEL_BG
	hud_bg.position = Vector2(8, 8)
	hud_bg.size = Vector2(260, 420)
	hud_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(hud_bg)
	# Move bg behind existing labels (labels are scene-defined, added before _ready)
	move_child(hud_bg, 0)

	_combat_label = Label.new()
	_combat_label.name = "CombatLabel"
	_combat_label.text = ""
	_combat_label.add_theme_color_override("font_color", Color.RED)
	_combat_label.position = Vector2(10, 256)
	add_child(_combat_label)

	# GATE.S9.UI.TOOLTIP_HUD.001: tooltips on HUD elements
	if _credits_label:
		_credits_label.tooltip_text = "Current credits available for trade and upgrades"
	if _cargo_label:
		_cargo_label.tooltip_text = "Items in cargo hold"
	if _node_label:
		_node_label.tooltip_text = "Current star system"
	if _state_label:
		_state_label.tooltip_text = "Ship state: Docked, InFlight, Traveling"
	if _hull_bar:
		_hull_bar.tooltip_text = "Hull integrity — reach 0 and your ship is destroyed"
	if _shield_bar:
		_shield_bar.tooltip_text = "Shield absorbs damage before hull takes hits"

	# GATE.S5.SEC_LANES.UI.001: security band indicator (below combat label)
	_security_label = Label.new()
	_security_label.name = "SecurityLabel"
	_security_label.text = ""
	_security_label.position = Vector2(10, 278)
	_security_label.visible = false
	add_child(_security_label)

	# GATE.S3.RISK_SINKS.BRIDGE.001: delay/ETA status label (below security label)
	_delay_label = Label.new()
	_delay_label.name = "DelayLabel"
	_delay_label.text = ""
	_delay_label.add_theme_color_override("font_color", UITheme.ORANGE)
	_delay_label.position = Vector2(10, 300)
	_delay_label.visible = false
	add_child(_delay_label)

	# GATE.S1.MISSION.HUD.001: mission objective panel (below hull/shield bars)
	_mission_panel = PanelContainer.new()
	_mission_panel.name = "MissionPanel"
	_mission_panel.visible = false
	_mission_panel.position = Vector2(10, 322)
	_mission_panel.custom_minimum_size = Vector2(260, 0)
	add_child(_mission_panel)

	var mission_vbox := VBoxContainer.new()
	_mission_panel.add_child(mission_vbox)

	_mission_title_label = Label.new()
	_mission_title_label.name = "MissionTitleLabel"
	_mission_title_label.text = ""
	_mission_title_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_mission_title_label.add_theme_color_override("font_color", UITheme.GOLD)
	mission_vbox.add_child(_mission_title_label)

	_mission_step_label = Label.new()
	_mission_step_label.name = "MissionStepLabel"
	_mission_step_label.text = ""
	_mission_step_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_mission_step_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	mission_vbox.add_child(_mission_step_label)

	# GATE.S11.GAME_FEEL.RESEARCH_HUD.001: research progress label (below mission panel)
	_research_label = Label.new()
	_research_label.name = "ResearchLabel"
	_research_label.text = "Research: Idle"
	_research_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_research_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_research_label.position = Vector2(10, 400)
	add_child(_research_label)

	# Build game over overlay (hidden until player dies)
	_game_over_panel = Control.new()
	_game_over_panel.name = "GameOverPanel"
	_game_over_panel.visible = false
	_game_over_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_game_over_panel)

	_game_over_label = Label.new()
	_game_over_label.name = "GameOverLabel"
	_game_over_label.text = "GAME OVER"
	_game_over_label.add_theme_font_size_override("font_size", UITheme.FONT_HUD_HUGE)
	_game_over_label.add_theme_color_override("font_color", UITheme.RED)
	_game_over_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_game_over_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_game_over_label.set_anchors_preset(Control.PRESET_FULL_RECT)
	_game_over_panel.add_child(_game_over_label)

	_restart_label = Label.new()
	_restart_label.name = "RestartLabel"
	_restart_label.text = "Press R to Restart"
	_restart_label.add_theme_font_size_override("font_size", UITheme.FONT_HUD_MED)
	_restart_label.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	_restart_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_restart_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_restart_label.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_restart_label.offset_top = 100
	_game_over_panel.add_child(_restart_label)

	# GATE.S1.SAVE_UI.PAUSE_MENU.001: pause menu (hidden until Escape pressed)
	_pause_panel = Control.new()
	_pause_panel.name = "PauseMenuPanel"
	_pause_panel.visible = false
	_pause_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	# process_mode ALWAYS so pause menu works while tree is paused
	_pause_panel.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_pause_panel)

	var pause_bg := ColorRect.new()
	pause_bg.color = UITheme.PANEL_BG_OVERLAY
	pause_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	_pause_panel.add_child(pause_bg)

	var pause_vbox := VBoxContainer.new()
	pause_vbox.set_anchors_preset(Control.PRESET_CENTER)
	pause_vbox.offset_left = -120
	pause_vbox.offset_right = 120
	pause_vbox.offset_top = -150
	pause_vbox.offset_bottom = 150
	pause_vbox.add_theme_constant_override("separation", 12)
	_pause_panel.add_child(pause_vbox)

	var pause_title := Label.new()
	pause_title.text = "PAUSED"
	pause_title.add_theme_font_size_override("font_size", UITheme.FONT_HUD_LARGE)
	pause_title.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	pause_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	pause_vbox.add_child(pause_title)

	var resume_btn := Button.new()
	resume_btn.text = "Resume"
	resume_btn.pressed.connect(_on_resume_pressed)
	resume_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	pause_vbox.add_child(resume_btn)

	# GATE.S1.SAVE_UI.SLOTS.001: save/load slot buttons
	for slot_idx in range(1, 4):
		var save_btn := Button.new()
		save_btn.name = "SaveSlot%d" % slot_idx
		save_btn.text = "Save Slot %d" % slot_idx
		save_btn.pressed.connect(_on_save_slot_pressed.bind(slot_idx))
		save_btn.process_mode = Node.PROCESS_MODE_ALWAYS
		pause_vbox.add_child(save_btn)

		var slot_lbl := Label.new()
		slot_lbl.name = "SlotLabel%d" % slot_idx
		slot_lbl.text = ""
		slot_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		slot_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		slot_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		pause_vbox.add_child(slot_lbl)
		_slot_labels.append(slot_lbl)

		var load_btn := Button.new()
		load_btn.name = "LoadSlot%d" % slot_idx
		load_btn.text = "Load Slot %d" % slot_idx
		load_btn.pressed.connect(_on_load_slot_pressed.bind(slot_idx))
		load_btn.process_mode = Node.PROCESS_MODE_ALWAYS
		pause_vbox.add_child(load_btn)

	var quit_btn := Button.new()
	quit_btn.text = "Quit"
	quit_btn.pressed.connect(_on_quit_pressed)
	quit_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	pause_vbox.add_child(quit_btn)

func show_game_over_v0() -> void:
	if _game_over_panel != null:
		_game_over_panel.visible = true
	print("UUIR|GAME_OVER_SHOWN")

func _physics_process(_delta: float) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	# GATE.S12.UX_POLISH.CARGO_DISPLAY.001: show "X items" suffix
	_cargo_label.text = "Cargo: %d items" % int(ps.get("cargo_count", 0))
	var node_display = str(ps.get("node_name", ps.get("current_node_id", "")))
	_node_label.text = node_display
	var raw_state = str(ps.get("ship_state_token", ""))
	match raw_state:
		"DOCKED":
			_state_label.text = "Docked"
		"IN_LANE_TRANSIT":
			_state_label.text = "Traveling"
		_:
			_state_label.text = "Flying"

	# HP bars: hull (red/green) and shield (blue)
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull: int = hp.get("hull", 0)
		var hull_max: int = hp.get("hull_max", 0)
		var shield: int = hp.get("shield", 0)
		var shield_max: int = hp.get("shield_max", 0)

		if hull_max > 0:
			_hull_bar.max_value = hull_max
			_hull_bar.value = hull
			_hull_label.text = "Hull: " + str(hull) + " / " + str(hull_max)
			_hull_bar.visible = true
			_hull_label.visible = true
		else:
			_hull_bar.visible = false
			_hull_label.visible = false

		if shield_max > 0:
			_shield_bar.max_value = shield_max
			_shield_bar.value = shield
			_shield_label.text = "Shield: " + str(shield) + " / " + str(shield_max)
			_shield_bar.visible = true
			_shield_label.visible = true
		else:
			_shield_bar.visible = false
			_shield_label.visible = false

		# Legacy text label (hidden when bars are shown)
		_combat_label.text = ""

	# GATE.S11.GAME_FEEL.MISSION_HUD.001 + RESEARCH_HUD.001: slow-poll (every 2s)
	_slow_poll_elapsed += _delta
	if _slow_poll_elapsed >= _SLOW_POLL_INTERVAL:
		_slow_poll_elapsed = 0.0
		_update_mission_hud()
		_update_research_hud()

	# GATE.S5.SEC_LANES.UI.001: security band display
	if _security_label != null and _bridge != null:
		var node_id: String = str(ps.get("current_node_id", ""))
		if not node_id.is_empty() and _bridge.has_method("GetNodeSecurityBandV0"):
			var band: String = str(_bridge.call("GetNodeSecurityBandV0", node_id))
			_security_label.text = "Security: %s" % band.to_upper()
			_security_label.visible = true
			_security_label.add_theme_color_override("font_color", UITheme.security_color(band))
		else:
			_security_label.visible = false

	# GATE.S3.RISK_SINKS.HUD_INDICATOR.001: delay/ETA + risk level display
	if _delay_label != null and _bridge != null:
		var show_delay := false
		var delay_text := ""
		var risk_color := UITheme.ORANGE
		var ship_state: String = str(ps.get("ship_state_token", ""))
		if ship_state == "Traveling" or ship_state == "FractureTraveling":
			if _bridge.has_method("GetDelayStatusV0"):
				var delay_info: Dictionary = _bridge.call("GetDelayStatusV0", "fleet_trader_1")
				var ticks_rem: int = int(delay_info.get("ticks_remaining", 0))
				if delay_info.get("delayed", false):
					show_delay = true
					delay_text = "DELAYED: %d ticks" % ticks_rem
					# Color by severity: red if > 5 ticks, orange otherwise
					if ticks_rem > 5:
						risk_color = UITheme.RED
			if _bridge.has_method("GetTravelEtaV0"):
				var node_id: String = str(ps.get("current_node_id", ""))
				var eta_info: Dictionary = _bridge.call("GetTravelEtaV0", "fleet_trader_1", node_id)
				var total_ticks: int = int(eta_info.get("total_ticks", 0))
				var delay_ticks: int = int(eta_info.get("delay_ticks", 0))
				if total_ticks > 0:
					show_delay = true
					var eta_str := "ETA: %d ticks" % total_ticks
					if delay_ticks > 0:
						eta_str += " (+%d delay)" % delay_ticks
					if delay_text.is_empty():
						delay_text = eta_str
					else:
						delay_text += " | " + eta_str
					# Green if no delay, orange if some, red if heavy
					if delay_ticks == 0:
						risk_color = UITheme.GREEN
		_delay_label.visible = show_delay
		_delay_label.text = delay_text
		_delay_label.add_theme_color_override("font_color", risk_color)

# GATE.S11.GAME_FEEL.MISSION_HUD.001: mission objective update (called every 2s)
func _update_mission_hud() -> void:
	if _bridge == null or not _bridge.has_method("GetActiveMissionV0"):
		return
	# GATE.S14.HUD.DOCK_CLEANUP.001: hide mission panel when docked to avoid overlap
	if _bridge.has_method("GetPlayerStateV0"):
		var ps_check: Dictionary = _bridge.call("GetPlayerStateV0")
		if str(ps_check.get("ship_state_token", "")) == "DOCKED":
			_mission_panel.visible = false
			return
	var mission: Dictionary = _bridge.call("GetActiveMissionV0")
	var mid: String = str(mission.get("mission_id", ""))
	if mid != "":
		_mission_panel.visible = true
		var title: String = str(mission.get("title", mid))
		_mission_title_label.text = "MISSION: %s" % title
		_mission_title_label.add_theme_color_override("font_color", UITheme.GOLD)
		var obj: String = str(mission.get("objective_text", ""))
		var tgt_node: String = str(mission.get("target_node_id", ""))
		var tgt_good: String = str(mission.get("target_good_id", ""))
		var step_parts: Array = []
		if not obj.is_empty():
			step_parts.append(obj)
		var detail_parts: Array = []
		if not tgt_node.is_empty():
			detail_parts.append(tgt_node)
		if not tgt_good.is_empty():
			detail_parts.append(tgt_good)
		if detail_parts.size() > 0:
			step_parts.append("Target: %s" % ", ".join(detail_parts))
		_mission_step_label.text = "\n".join(step_parts)
	else:
		_mission_panel.visible = true
		_mission_title_label.text = "No active mission"
		_mission_title_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_mission_step_label.text = ""

# GATE.S11.GAME_FEEL.RESEARCH_HUD.001: research progress update (called every 2s)
func _update_research_hud() -> void:
	if _research_label == null or _bridge == null:
		return
	if not _bridge.has_method("GetResearchStatusV0"):
		_research_label.text = "Research: Idle"
		_research_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		return
	var status: Dictionary = _bridge.call("GetResearchStatusV0")
	var is_researching: bool = status.get("researching", false)
	if is_researching:
		var tech_id: String = str(status.get("tech_id", ""))
		var pct: int = int(status.get("progress_pct", 0))
		var stall: String = str(status.get("stall_reason", ""))
		if not stall.is_empty():
			_research_label.text = "RESEARCH: %s STALLED: %s" % [tech_id, stall]
			_research_label.add_theme_color_override("font_color", UITheme.RED)
		else:
			_research_label.text = "RESEARCH: %s %d%%" % [tech_id, pct]
			_research_label.add_theme_color_override("font_color", UITheme.CYAN)
	else:
		_research_label.text = "Research: Idle"
		_research_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)

# GATE.S1.SAVE_UI.PAUSE_MENU.001: toggle pause overlay
func toggle_pause_menu_v0(visible_flag: bool) -> void:
	if _pause_panel == null:
		return
	_pause_panel.visible = visible_flag
	if visible_flag:
		_refresh_slot_labels_v0()
	print("UUIR|PAUSE_MENU|" + ("SHOW" if visible_flag else "HIDE"))

func _on_resume_pressed() -> void:
	var gm = get_tree().root.find_child("GameManager", true, false)
	if gm and gm.has_method("_toggle_pause_v0"):
		gm.call("_toggle_pause_v0")

func _on_quit_pressed() -> void:
	get_tree().quit()

# GATE.S1.SAVE_UI.SLOTS.001: save/load slot handlers
func _on_save_slot_pressed(slot: int) -> void:
	if _bridge == null:
		return
	if _bridge.has_method("SetActiveSaveSlotV0"):
		_bridge.call("SetActiveSaveSlotV0", slot)
	if _bridge.has_method("RequestSave"):
		_bridge.call("RequestSave")
	# Refresh labels after a short delay to allow save to complete
	await get_tree().create_timer(0.5).timeout
	_refresh_slot_labels_v0()
	print("UUIR|SAVE_SLOT|" + str(slot))

func _on_load_slot_pressed(slot: int) -> void:
	if _bridge == null:
		return
	if _bridge.has_method("SetActiveSaveSlotV0"):
		_bridge.call("SetActiveSaveSlotV0", slot)
	if _bridge.has_method("RequestLoad"):
		_bridge.call("RequestLoad")
	# Unpause and hide menu after load
	var gm = get_tree().root.find_child("GameManager", true, false)
	if gm and gm.has_method("_toggle_pause_v0"):
		gm.call("_toggle_pause_v0")
	print("UUIR|LOAD_SLOT|" + str(slot))

func _refresh_slot_labels_v0() -> void:
	if _bridge == null or not _bridge.has_method("GetSaveSlotMetadataV0"):
		return
	for i in range(_slot_labels.size()):
		var slot := i + 1
		var meta: Dictionary = _bridge.call("GetSaveSlotMetadataV0", slot)
		if meta.get("exists", false):
			_slot_labels[i].text = "%s | Credits: %s | %s" % [
				str(meta.get("timestamp", "")),
				str(meta.get("credits", 0)),
				str(meta.get("system_name", ""))
			]
		else:
			_slot_labels[i].text = "[Empty]"
