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

# GATE.S7.SUSTAIN.BRIDGE_PROOF.001: Fuel indicator label.
var _fuel_label: Label = null

# Data overlay mode label (shown when V-key cycles overlay modes)
var _overlay_mode_label: Label = null

# Galaxy map header label (shown only when overlay is active)
var _galaxy_map_label: Label = null

# GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001: Zone G bottom bar.
var _zone_g_bar: HBoxContainer = null
var _zone_g_bg: ColorRect = null
var _zone_g_risk_label: Label = null
var _zone_g_status_label: Label = null
var _zone_g_minimap_label: Label = null

# GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: Keybind hint bar (U7).
var _keybind_hint_label: Label = null

# GATE.S7.RISK_METER_UI.WIDGET.001: Risk meter bars widget in Zone G.
var _risk_meter_widget: Control = null

# GATE.S7.RISK_METER_UI.SCREEN_EDGE.001: Screen edge vignette overlay.
var _screen_edge_tint: ColorRect = null

# FEEL_BASELINE: Screen-space damage flash overlay.
var _damage_flash: ColorRect = null

# GATE.S7.HUD_ARCH.ALERT_BADGE.001: Alert count badge in Zone A.
var _alert_badge: Control = null
var _alert_badge_label: Label = null
var _alert_count: int = 0

# Overlay mode: when true, HUD status elements are hidden (galaxy map / empire dashboard open)
var _overlay_active: bool = false

# Suppress "LOW" fuel warning until player has had fuel at least once (avoid alarming at boot)
var _fuel_ever_had: bool = false

# GATE.S19.ONBOARD.HUD_DISCLOSURE.010: Cached onboarding disclosure state.
var _onboarding_state: Dictionary = {}

# GATE.S7.NARRATIVE_DELIVERY.TEXT_PANEL.001: Narrative text display panel.
var _narrative_panel = null

# GATE.S7.AUTOMATION_MGMT.DASHBOARD.001: Automation management dashboard panel.
var _automation_dashboard: PanelContainer = null

# GATE.S7.AUTOMATION_MGMT.FLEET_INTEGRATION.001: Fleet automation summary.
var _fleet_auto_panel: PanelContainer = null
var _fleet_auto_credits_label: Label = null
var _fleet_auto_failures_label: Label = null
var _fleet_auto_program_label: Label = null

# GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001: Zone armor + combat stance display.
var _combat_hud: Control = null

# GATE.S6.FRACTURE_DISCOVERY.UI.001: Track whether fracture unlock toast has been shown.
var _fracture_unlock_shown: bool = false

# GATE.T18.CHARACTER.UI.001: First Officer panel.
var _fo_panel = null

# GATE.T18.NARRATIVE.UI_DATALOG.001: Data log viewer panel.
var _data_log_panel = null

# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Knowledge web panel (K key).
var _knowledge_web_panel = null

# Dock confirmation: "Press E to dock" prompt (bottom-center).
var _dock_prompt_label: Label = null


func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

	# HUD status panel background (dark navy, matches UITheme.PANEL_BG)
	var hud_bg := ColorRect.new()
	hud_bg.name = "HudStatusBg"
	hud_bg.color = UITheme.PANEL_BG
	hud_bg.position = Vector2(8, 8)
	hud_bg.size = Vector2(260, 248)
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
		# Q1: Orange/red hull bar — distinct from shield at a glance
		var hull_style := StyleBoxFlat.new()
		hull_style.bg_color = Color(0.9, 0.3, 0.1)
		_hull_bar.add_theme_stylebox_override("fill", hull_style)
		var hull_bg := StyleBoxFlat.new()
		hull_bg.bg_color = Color(0.2, 0.08, 0.02)
		_hull_bar.add_theme_stylebox_override("background", hull_bg)
	if _shield_bar:
		_shield_bar.tooltip_text = "Shield absorbs damage before hull takes hits"
		# Q1: Cyan/blue shield bar — visually distinct from hull
		var shield_style := StyleBoxFlat.new()
		shield_style.bg_color = Color(0.3, 0.8, 1.0)
		_shield_bar.add_theme_stylebox_override("fill", shield_style)
		var shield_bg := StyleBoxFlat.new()
		shield_bg.bg_color = Color(0.06, 0.16, 0.2)
		_shield_bar.add_theme_stylebox_override("background", shield_bg)

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

	# GATE.S7.SUSTAIN.BRIDGE_PROOF.001: Fuel indicator label.
	_fuel_label = Label.new()
	_fuel_label.name = "FuelLabel"
	_fuel_label.text = ""
	_fuel_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_fuel_label.position = Vector2(10, 446)
	add_child(_fuel_label)

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

	# Data overlay mode indicator (below research label)
	_overlay_mode_label = Label.new()
	_overlay_mode_label.name = "OverlayModeLabel"
	_overlay_mode_label.text = ""
	_overlay_mode_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_overlay_mode_label.add_theme_color_override("font_color", UITheme.CYAN)
	_overlay_mode_label.position = Vector2(10, 422)
	_overlay_mode_label.visible = false
	add_child(_overlay_mode_label)

	# Galaxy map header label (top-center, hidden by default)
	# CanvasLayer is not Control — anchors don't work. Use explicit position + viewport size.
	_galaxy_map_label = Label.new()
	_galaxy_map_label.name = "GalaxyMapLabel"
	_galaxy_map_label.text = "GALAXY MAP  (TAB to close)"
	_galaxy_map_label.add_theme_font_size_override("font_size", UITheme.FONT_HUD_MED)
	_galaxy_map_label.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	_galaxy_map_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_galaxy_map_label.position = Vector2(0, 12)
	_galaxy_map_label.size = Vector2(1920, 40)
	_galaxy_map_label.visible = false
	add_child(_galaxy_map_label)

	# GATE.S7.RISK_METER_UI.SCREEN_EDGE.001: screen edge vignette overlay.
	var ScreenEdgeTintScript := preload("res://scripts/view/screen_edge_tint.gd")
	_screen_edge_tint = ScreenEdgeTintScript.new()
	add_child(_screen_edge_tint)

	# FEEL_BASELINE: Full-screen red flash for combat damage feedback.
	_damage_flash = ColorRect.new()
	_damage_flash.name = "DamageFlash"
	_damage_flash.color = Color(0.8, 0.1, 0.05, 0.0)
	_damage_flash.position = Vector2.ZERO
	_damage_flash.size = Vector2(1920, 1080)
	_damage_flash.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_damage_flash.visible = true
	add_child(_damage_flash)

	# GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001: Zone G bottom bar.
	_zone_g_bg = ColorRect.new()
	_zone_g_bg.name = "ZoneGBg"
	_zone_g_bg.color = Color(0.05, 0.07, 0.12, 0.85)
	_zone_g_bg.position = Vector2(0, 1040)  # Bottom 40px of 1080p screen
	_zone_g_bg.size = Vector2(1920, 40)
	_zone_g_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_zone_g_bg)

	_zone_g_bar = HBoxContainer.new()
	_zone_g_bar.name = "ZoneGBar"
	_zone_g_bar.position = Vector2(8, 1044)
	_zone_g_bar.size = Vector2(1904, 32)
	_zone_g_bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_zone_g_bar.add_theme_constant_override("separation", 24)
	add_child(_zone_g_bar)

	# Left slot: risk meters — widget replaces placeholder label.
	# GATE.S7.RISK_METER_UI.WIDGET.001: instantiate risk meter bars widget.
	var RiskMeterWidgetScript := preload("res://scripts/ui/risk_meter_widget.gd")
	_risk_meter_widget = RiskMeterWidgetScript.new()
	_risk_meter_widget.name = "RiskMeterWidget"
	_risk_meter_widget.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_zone_g_bar.add_child(_risk_meter_widget)

	# Keep fallback risk label (hidden) for Zone G text-only updates.
	_zone_g_risk_label = Label.new()
	_zone_g_risk_label.name = "ZoneGRisk"
	_zone_g_risk_label.text = ""
	_zone_g_risk_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_zone_g_risk_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_zone_g_risk_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_zone_g_risk_label.visible = false
	_zone_g_bar.add_child(_zone_g_risk_label)

	# Center slot: system status
	_zone_g_status_label = Label.new()
	_zone_g_status_label.name = "ZoneGStatus"
	_zone_g_status_label.text = ""
	_zone_g_status_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_zone_g_status_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_zone_g_status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_zone_g_status_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_zone_g_bar.add_child(_zone_g_status_label)

	# Right slot: minimap placeholder
	_zone_g_minimap_label = Label.new()
	_zone_g_minimap_label.name = "ZoneGMinimap"
	_zone_g_minimap_label.text = ""
	_zone_g_minimap_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_zone_g_minimap_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_zone_g_minimap_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_zone_g_minimap_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_zone_g_bar.add_child(_zone_g_minimap_label)

	# GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: Persistent keybind hints (U7).
	_keybind_hint_label = Label.new()
	_keybind_hint_label.name = "KeybindHintLabel"
	_keybind_hint_label.text = "TAB Map  |  E Empire  |  H Help  |  K Web  |  L Log  |  V Overlay  |  ESC Pause"
	_keybind_hint_label.add_theme_font_size_override("font_size", 11)
	_keybind_hint_label.add_theme_color_override("font_color", Color(0.4, 0.45, 0.5, 0.6))
	_keybind_hint_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_keybind_hint_label.position = Vector2(0, 1076)
	_keybind_hint_label.size = Vector2(1920, 20)
	_keybind_hint_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_keybind_hint_label)

	# GATE.S7.NARRATIVE_DELIVERY.TEXT_PANEL.001: Narrative text display panel.
	var NarrativePanelScript := preload("res://scripts/ui/narrative_panel.gd")
	_narrative_panel = NarrativePanelScript.new()
	_narrative_panel.name = "NarrativePanel"
	add_child(_narrative_panel)

	# GATE.S7.AUTOMATION_MGMT.DASHBOARD.001: Automation management dashboard panel.
	var AutomationDashboardScript := preload("res://scripts/ui/automation_dashboard.gd")
	_automation_dashboard = AutomationDashboardScript.new()
	add_child(_automation_dashboard)

	# GATE.S7.AUTOMATION_MGMT.FLEET_INTEGRATION.001: Fleet automation summary.
	_fleet_auto_panel = PanelContainer.new()
	_fleet_auto_panel.name = "FleetAutoPanel"
	_fleet_auto_panel.custom_minimum_size = Vector2(200, 0)
	_fleet_auto_panel.position = Vector2(1700, 960)
	_fleet_auto_panel.visible = true
	var auto_style := StyleBoxFlat.new()
	auto_style.bg_color = Color(0.05, 0.07, 0.12, 0.85)
	auto_style.border_width_left = 2
	auto_style.border_color = UITheme.CYAN
	_fleet_auto_panel.add_theme_stylebox_override("panel", auto_style)
	add_child(_fleet_auto_panel)

	var auto_vbox := VBoxContainer.new()
	auto_vbox.add_theme_constant_override("separation", 2)
	_fleet_auto_panel.add_child(auto_vbox)

	_fleet_auto_program_label = Label.new()
	_fleet_auto_program_label.text = "Program: --"
	_fleet_auto_program_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_fleet_auto_program_label.add_theme_color_override("font_color", UITheme.CYAN)
	auto_vbox.add_child(_fleet_auto_program_label)

	_fleet_auto_credits_label = Label.new()
	_fleet_auto_credits_label.text = "Credits: 0"
	_fleet_auto_credits_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_fleet_auto_credits_label.add_theme_color_override("font_color", UITheme.GOLD)
	auto_vbox.add_child(_fleet_auto_credits_label)

	_fleet_auto_failures_label = Label.new()
	_fleet_auto_failures_label.text = "Failures: 0"
	_fleet_auto_failures_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_fleet_auto_failures_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	auto_vbox.add_child(_fleet_auto_failures_label)

	# GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001: Combat HUD overlay.
	var CombatHudScript := preload("res://scripts/ui/combat_hud.gd")
	_combat_hud = CombatHudScript.new()
	add_child(_combat_hud)

	# GATE.T18.CHARACTER.UI.001: First Officer panel (FO reactions + War Faces NPC dialogue).
	var FOPanelScript := preload("res://scripts/ui/fo_panel.gd")
	_fo_panel = FOPanelScript.new()
	add_child(_fo_panel)

	# GATE.T18.NARRATIVE.UI_DATALOG.001: Data log viewer panel.
	var DataLogPanelScript := preload("res://scripts/ui/data_log_panel.gd")
	_data_log_panel = DataLogPanelScript.new()
	add_child(_data_log_panel)

	# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Knowledge web panel (K key).
	var KnowledgeWebPanelScript := preload("res://scripts/ui/knowledge_web_panel.gd")
	_knowledge_web_panel = KnowledgeWebPanelScript.new()
	add_child(_knowledge_web_panel)

	# Dock confirmation prompt (centered above Zone G bar).
	_dock_prompt_label = Label.new()
	_dock_prompt_label.name = "DockPromptLabel"
	_dock_prompt_label.text = ""
	_dock_prompt_label.add_theme_font_size_override("font_size", UITheme.FONT_HUD_MED)
	_dock_prompt_label.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	_dock_prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_dock_prompt_label.position = Vector2(0, 1000)
	_dock_prompt_label.size = Vector2(1920, 36)
	_dock_prompt_label.visible = false
	_dock_prompt_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_dock_prompt_label)

	# GATE.S7.HUD_ARCH.ALERT_BADGE.001: Alert badge (top-left Zone A).
	_alert_badge = Control.new()
	_alert_badge.name = "AlertBadge"
	_alert_badge.position = Vector2(276, 8)  # Right of HUD status panel
	_alert_badge.size = Vector2(28, 28)
	_alert_badge.visible = false
	_alert_badge.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_alert_badge)

	var badge_bg := ColorRect.new()
	badge_bg.name = "BadgeBg"
	badge_bg.color = UITheme.RED
	badge_bg.position = Vector2.ZERO
	badge_bg.size = Vector2(28, 28)
	badge_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_alert_badge.add_child(badge_bg)

	_alert_badge_label = Label.new()
	_alert_badge_label.name = "BadgeCount"
	_alert_badge_label.text = "0"
	_alert_badge_label.add_theme_font_size_override("font_size", 12)
	_alert_badge_label.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	_alert_badge_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_alert_badge_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_alert_badge_label.position = Vector2(0, 2)
	_alert_badge_label.size = Vector2(28, 24)
	_alert_badge.add_child(_alert_badge_label)

	_alert_badge.gui_input.connect(_on_alert_badge_clicked)

func show_game_over_v0() -> void:
	if _game_over_panel != null:
		_game_over_panel.visible = true
	print("UUIR|GAME_OVER_SHOWN")

func set_overlay_mode_v0(active: bool, is_transit: bool = false) -> void:
	_overlay_active = active
	var bg = get_node_or_null("HudStatusBg")
	if bg: bg.visible = not active
	for lbl in [_credits_label, _cargo_label, _node_label, _state_label,
				_hull_bar, _shield_bar, _hull_label, _shield_label]:
		if lbl != null: lbl.visible = not active
	if _fuel_label: _fuel_label.visible = not active
	if _galaxy_map_label:
		if active and not is_transit:
			_galaxy_map_label.size.x = get_viewport().get_visible_rect().size.x
		_galaxy_map_label.visible = active and not is_transit
	# GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001: Hide Zone G bar during overlay.
	if _zone_g_bg: _zone_g_bg.visible = not active
	if _zone_g_bar: _zone_g_bar.visible = not active
	if _keybind_hint_label: _keybind_hint_label.visible = not active
	if _fleet_auto_panel: _fleet_auto_panel.visible = not active
	if _combat_hud: _combat_hud.visible = not active
	if _alert_badge: _alert_badge.visible = (not active) and _alert_count > 0
	# FEEL_POST_BASELINE: FO panel always suppressed (no toggle key exists).
	if _fo_panel: _fo_panel.visible = false
	if _data_log_panel and active: _data_log_panel.visible = false
	if _knowledge_web_panel and active: _knowledge_web_panel.visible = false
	if active:
		if _combat_label: _combat_label.visible = false
		if _security_label: _security_label.visible = false
		if _delay_label: _delay_label.visible = false
		if _mission_panel: _mission_panel.visible = false
		if _research_label: _research_label.visible = false
		if _overlay_mode_label: _overlay_mode_label.visible = false
		if _narrative_panel and _narrative_panel.has_method("hide_narrative"):
			_narrative_panel.hide_narrative()

func _physics_process(_delta: float) -> void:
	if _overlay_active or _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	# GATE.S12.UX_POLISH.CARGO_DISPLAY.001: show "X items" suffix
	_cargo_label.text = "Cargo: %d items" % int(ps.get("cargo_count", 0))
	var node_display = str(ps.get("node_name", ps.get("current_node_id", "")))
	_node_label.text = _truncate_resource_types(node_display)
	var raw_state = str(ps.get("ship_state_token", ""))
	match raw_state:
		"DOCKED":
			_state_label.text = "Docked"
		"IN_LANE_TRANSIT":
			# FEEL_BASELINE: Show destination name during warp transit.
			var dest_name := _get_transit_dest_name()
			if dest_name.is_empty():
				_state_label.text = "Traveling..."
			else:
				_state_label.text = "Traveling to %s" % dest_name
		_:
			# FEEL_POST_BASELINE: Show "COMBAT" when hostile NPCs are in aggro range.
			var in_combat := _is_hostile_nearby()
			if in_combat:
				_state_label.text = "COMBAT"
				_state_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.2))
			else:
				_state_label.text = "Flying"
				_state_label.remove_theme_color_override("font_color")

	# FEEL_POST_BASELINE: FO panel always hidden — it ate 20% screen width with
	# no actionable content. No F-key toggle exists yet, so suppress entirely.
	if _fo_panel:
		_fo_panel.visible = false

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
		_update_fuel_hud()
		_update_zone_g_v0()
		_update_fleet_auto_summary_v0()
		if _combat_hud and _combat_hud.has_method("refresh_v0"):
			_combat_hud.refresh_v0()
		_check_fracture_unlock_toast_v0()
		# GATE.S19.ONBOARD.HUD_DISCLOSURE.010: Update onboarding disclosure state.
		_update_onboarding_disclosure_v0()

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

# GATE.S7.SUSTAIN.BRIDGE_PROOF.001: fuel indicator update
func _update_fuel_hud() -> void:
	if _fuel_label == null or _bridge == null:
		return
	if not _bridge.has_method("GetFleetSustainStatusV0"):
		_fuel_label.visible = false
		return
	var sustain: Dictionary = _bridge.call("GetFleetSustainStatusV0", "fleet_trader_1")
	if sustain.size() == 0:
		_fuel_label.visible = false
		return
	var fuel: int = int(sustain.get("fuel", 0))
	var immobilized: bool = sustain.get("is_immobilized", false)
	if fuel > 0:
		_fuel_ever_had = true
	_fuel_label.visible = true
	if immobilized:
		_fuel_label.text = "FUEL: %d  [IMMOBILIZED]" % fuel
		_fuel_label.add_theme_color_override("font_color", UITheme.RED)
	elif fuel <= 3 and _fuel_ever_had:
		_fuel_label.text = "FUEL: %d  LOW" % fuel
		_fuel_label.add_theme_color_override("font_color", UITheme.ORANGE)
	else:
		_fuel_label.text = "Fuel: %d" % fuel
		_fuel_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)

## Called by game_manager when V-key cycles overlay mode. mode: -1=Off, 0-2=active.
func set_data_overlay_label_v0(mode: int) -> void:
	if _overlay_mode_label == null:
		return
	var names: Dictionary = {-1: "", 0: "[Security]", 1: "[Trade Flow]", 2: "[Intel Age]"}
	_overlay_mode_label.text = names.get(mode, "")
	_overlay_mode_label.visible = mode >= 0

# GATE.S7.NARRATIVE_DELIVERY.TEXT_PANEL.001: Show narrative text with faction styling.
## Called by game_manager.gd or station events to display flavor text / faction greetings.
func show_narrative_v0(text: String, faction_id: String = "") -> void:
	if _narrative_panel != null and _narrative_panel.has_method("show_narrative"):
		_narrative_panel.show_narrative(text, faction_id)

# GATE.S7.AUTOMATION_MGMT.DASHBOARD.001: toggle automation dashboard panel.
func toggle_automation_dashboard_v0() -> void:
	if _automation_dashboard != null:
		_automation_dashboard.toggle_v0()

# GATE.T18.NARRATIVE.UI_DATALOG.001: toggle data log viewer panel.
func toggle_data_log_v0() -> void:
	if _data_log_panel != null and _data_log_panel.has_method("toggle_v0"):
		_data_log_panel.toggle_v0()

# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: toggle knowledge web panel.
func toggle_knowledge_web_v0() -> void:
	if _knowledge_web_panel != null and _knowledge_web_panel.has_method("toggle_v0"):
		_knowledge_web_panel.toggle_v0()

# GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001: Update Zone G bottom bar content.
func _update_zone_g_v0() -> void:
	if _zone_g_status_label == null or _bridge == null:
		return
	# Center: show current system + security band.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0") if _bridge.has_method("GetPlayerStateV0") else {}
	var node_id: String = str(ps.get("current_node_id", ""))
	var node_name: String = str(ps.get("node_name", node_id))
	var sec_band: String = ""
	if not node_id.is_empty() and _bridge.has_method("GetNodeSecurityBandV0"):
		sec_band = str(_bridge.call("GetNodeSecurityBandV0", node_id))
	_zone_g_status_label.text = "%s  |  %s" % [node_name, sec_band.to_upper()] if not sec_band.is_empty() else node_name
	# GATE.S7.RISK_METER_UI.SCREEN_EDGE.001 + COMPOUND.001: feed risk values to vignette overlay.
	if _screen_edge_tint != null and _bridge != null and _bridge.has_method("GetRiskMetersV0"):
		var risk: Dictionary = _bridge.call("GetRiskMetersV0")
		_screen_edge_tint.update_risk_levels(
			float(risk.get("heat", 0.0)),
			float(risk.get("influence", 0.0)),
			float(risk.get("trace", 0.0))
		)
		if _risk_meter_widget != null and _risk_meter_widget.has_method("get_compound_threat"):
			_screen_edge_tint.set_compound_threat(_risk_meter_widget.get_compound_threat())

	# Left: heat indicator for current edge (if traveling).
	if _zone_g_risk_label != null:
		var ship_state: String = str(ps.get("ship_state_token", ""))
		if ship_state == "IN_LANE_TRANSIT" and _bridge.has_method("GetEdgeHeatV0"):
			# Approximate: use player node as from, destination as to.
			var heat_info: Dictionary = _bridge.call("GetEdgeHeatV0", node_id, "")
			var threshold: String = str(heat_info.get("threshold_name", "safe"))
			_zone_g_risk_label.text = "Heat: %s" % threshold.to_upper()
			match threshold:
				"confiscation":
					_zone_g_risk_label.add_theme_color_override("font_color", UITheme.RED)
				"elevated":
					_zone_g_risk_label.add_theme_color_override("font_color", UITheme.ORANGE)
				_:
					_zone_g_risk_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		else:
			_zone_g_risk_label.text = ""

# GATE.S7.HUD_ARCH.ALERT_BADGE.001: Update alert badge count.
func set_alert_count_v0(count: int) -> void:
	_alert_count = count
	if _alert_badge == null:
		return
	_alert_badge.visible = count > 0 and not _overlay_active
	if _alert_badge_label:
		_alert_badge_label.text = str(count) if count < 100 else "99+"
	# Color: red for critical alerts, orange for warnings.
	var badge_bg = _alert_badge.get_node_or_null("BadgeBg")
	if badge_bg:
		badge_bg.color = UITheme.RED if count >= 3 else UITheme.ORANGE

func _on_alert_badge_clicked(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		# Open Empire Dashboard overview tab.
		var gm = get_tree().root.find_child("GameManager", true, false)
		if gm and gm.has_method("_toggle_empire_dashboard_v0"):
			gm.call("_toggle_empire_dashboard_v0")

# GATE.S7.AUTOMATION_MGMT.FLEET_INTEGRATION.001: Update fleet automation summary.
func _update_fleet_auto_summary_v0() -> void:
	if _fleet_auto_panel == null or _bridge == null:
		return
	if _overlay_active:
		_fleet_auto_panel.visible = false
		return
	_fleet_auto_panel.visible = true
	if not _bridge.has_method("GetProgramPerformanceV0"):
		return
	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
	if data.is_empty():
		_fleet_auto_program_label.text = "Program: None"
		return
	var credits: int = int(data.get("credits_earned", 0))
	var failures: int = int(data.get("failures", 0))
	var cycles: int = int(data.get("cycles_run", 0))
	_fleet_auto_program_label.text = "Auto: %d cycles" % cycles
	_fleet_auto_credits_label.text = "Credits: %d" % credits
	_fleet_auto_failures_label.text = "Failures: %d" % failures
	if failures > 0:
		_fleet_auto_failures_label.add_theme_color_override("font_color", UITheme.RED)
	else:
		_fleet_auto_failures_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)


# GATE.S6.FRACTURE_DISCOVERY.UI.001: Check fracture unlock status and show one-time toast.
func _check_fracture_unlock_toast_v0() -> void:
	if _fracture_unlock_shown or _bridge == null:
		return
	if not _bridge.has_method("GetFractureDiscoveryStatusV0"):
		return
	var status: Dictionary = _bridge.call("GetFractureDiscoveryStatusV0")
	if status.get("unlocked", false):
		_fracture_unlock_shown = true
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "FRACTURE DRIVE UNLOCKED — Off-lane travel is now possible.", "critical")
		print("UUIR|FRACTURE_UNLOCKED")

# GATE.S19.ONBOARD.HUD_DISCLOSURE.010: Progressive HUD element reveal.
func _update_onboarding_disclosure_v0() -> void:
	# GATE.S19.ONBOARD.SETTINGS_WIRE.015: Skip disclosure if tutorials disabled.
	var settings_mgr = get_node_or_null("/root/SettingsManager")
	if settings_mgr and settings_mgr.has_method("get_setting"):
		if not bool(settings_mgr.call("get_setting", "gameplay_tutorial_toasts")):
			return  # All HUD elements stay visible.
	if _bridge == null or not _bridge.has_method("GetOnboardingStateV0"):
		return
	_onboarding_state = _bridge.call("GetOnboardingStateV0")
	if _onboarding_state.is_empty():
		return

	# Fuel label: hidden until player has moved to another node
	if _fuel_label != null:
		var show_fuel: bool = bool(_onboarding_state.get("show_fuel_hud", true))
		if not show_fuel and not _fuel_ever_had:
			_fuel_label.visible = false

	# Security label: hidden until player has visited 2+ nodes
	if _security_label != null:
		var show_faction: bool = bool(_onboarding_state.get("show_faction_hud", true))
		if not show_faction:
			_security_label.visible = false

	# M2: Suppress NPC ship role/hostile labels until player has explored enough.
	var hide_npc_labels: bool = int(_onboarding_state.get("nodes_visited", 0)) < 2
	for ship in get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship) and "_onboard_labels_hidden" in ship:
			ship._onboard_labels_hidden = hide_npc_labels

# Dock confirmation: show "Press E to dock" prompt.
func show_dock_prompt_v0(station_name: String = "") -> void:
	if _dock_prompt_label == null:
		return
	if station_name.is_empty():
		_dock_prompt_label.text = "Press E to dock"
	else:
		_dock_prompt_label.text = "Press E to dock at %s" % station_name
	_dock_prompt_label.visible = true

# Dock confirmation: hide dock prompt.
func hide_dock_prompt_v0() -> void:
	if _dock_prompt_label == null:
		return
	_dock_prompt_label.visible = false


# Truncate system names with multiple resource type tags.
# "System 10 (RareMin)(Mining)..." → "System 10 (RareMin)..."
# Mirrors GalaxyView.cs TruncateResourceTypesV0.
# FEEL_BASELINE: Read transit destination name from GameManager autoload.
func _get_transit_dest_name() -> String:
	var gm = get_node_or_null("/root/GameManager")
	if gm == null:
		return ""
	var dest_id = gm.get("_lane_dest_node_id")
	if dest_id == null or str(dest_id).is_empty():
		return ""
	# Resolve display name from bridge snapshot.
	if _bridge and _bridge.has_method("GetNodeDisplayNameV0"):
		return str(_bridge.call("GetNodeDisplayNameV0", str(dest_id)))
	return str(dest_id)

# FEEL_BASELINE: Flash red overlay on player damage. Called from bullet.gd.
func flash_damage_v0() -> void:
	if _damage_flash == null:
		return
	_damage_flash.color.a = 0.12
	var tween := create_tween()
	tween.tween_property(_damage_flash, "color:a", 0.0, 0.2)

# FEEL_POST_BASELINE: Check if a hostile NPC is within aggro range of the player.
func _is_hostile_nearby() -> bool:
	var gm = get_node_or_null("/root/GameManager")
	if gm == null:
		return false
	if gm.has_method("_find_nearest_fleet_v0"):
		var nearest = gm.call("_find_nearest_fleet_v0", 60.0)
		if nearest != null and nearest.get_meta("is_hostile", false):
			return true
	return false

func _truncate_resource_types(display_text: String) -> String:
	var first_open := display_text.find("(")
	if first_open < 0:
		return display_text
	var first_close := display_text.find(")", first_open)
	if first_close < 0:
		return display_text
	var second_open := display_text.find("(", first_close + 1)
	if second_open >= 0:
		return display_text.left(first_close + 1) + "..."
	return display_text
