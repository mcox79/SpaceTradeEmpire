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

# GATE.X.WARP.ARRIVAL_DRAMA.001: Letterbox bars + title card on every warp arrival.
var _arrival_bar_top: ColorRect = null
var _arrival_bar_bot: ColorRect = null
var _arrival_card_name: Label = null
var _arrival_card_faction: Label = null
var _arrival_drama_tween: Tween = null

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

# GATE.S6.UI_DISCOVERY.SCAN_VIZ.001: Scan progress display.
var _scan_progress_label: Label = null
var _scan_progress_bar: ProgressBar = null

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
# GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001: Track lockout state for vent burst flash.
var _prev_locked_out: bool = false

# FEEL_POST_FIX_9: Persistent red border vignette during combat state.
var _combat_vignette: ColorRect = null
var _combat_vignette_active: bool = false

# GATE.S7.HUD_ARCH.ALERT_BADGE.001: Alert count badge in Zone A.
var _alert_badge: Control = null
var _alert_badge_label: Label = null
var _alert_count: int = 0

# Overlay mode: when true, HUD status elements are hidden (galaxy map / empire dashboard open)
var _overlay_active: bool = false
# FEEL_POST_FIX_4: Dark scrim behind galaxy map / empire dashboard so beacons pop
# over the Starlight skybox. Without this, skybox brightness drowns out map nodes.
var _overlay_scrim: ColorRect = null

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

# GATE.S7.COMBAT_PHASE2.HEAT_HUD.001: Heat gauge + battle stations indicator.
var _heat_bar: ProgressBar = null
var _heat_label: Label = null
var _battle_stations_label: Label = null

# GATE.S6.FRACTURE_DISCOVERY.UI.001: Track whether fracture unlock toast has been shown.
var _fracture_unlock_shown: bool = false

# GATE.T18.CHARACTER.UI.001: First Officer panel.
var _fo_panel = null

# GATE.T18.NARRATIVE.UI_DATALOG.001: Data log viewer panel.
var _data_log_panel = null

# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Knowledge web panel (K key).
var _knowledge_web_panel = null

# GATE.X.UI_POLISH.MISSION_JOURNAL.001: Mission journal panel (J key).
var _mission_journal_panel = null

# GATE.S8.MEGAPROJECT.UI.001: Megaproject construction panel (M key).
var _megaproject_panel = null

# GATE.S7.UI_WARFRONT.DASHBOARD.001: Warfront dashboard panel (N key).
var _warfront_panel = null

# GATE.X.UI_POLISH.QUEST_TRACKER.001: Persistent quest tracker widget (top-right).
var _quest_tracker_panel: PanelContainer = null
var _quest_tracker_name_label: Label = null
var _quest_tracker_step_label: Label = null
var _quest_tracker_progress: ProgressBar = null

# GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001: Active discovery leads panel (left side, below scan).
var _active_leads_panel: PanelContainer = null
var _active_leads_vbox: VBoxContainer = null
var _active_leads_title: Label = null
var _active_leads_labels: Array = []  # Up to 3 Label nodes

# Captain's Guide: objective breadcrumb label (below HUD status panel).
var _guide_objective_label: Label = null

# Dock confirmation: "Press E to dock" prompt (bottom-center).
var _dock_prompt_label: Label = null

# FEEL_POST_FIX_5: Transit destination overlay (center-bottom during warp transit).
var _transit_dest_label: Label = null
var _transit_progress_bar: ColorRect = null
var _transit_progress_fill: ColorRect = null

# GATE.X.WARP.TRANSIT_HUD.001: Warp transit HUD overlay (destination + ETA + distance).
var _warp_transit_hud = null

# GATE.S8.STORY_STATE.DELIVERY_UI.001: Gold toast + map highlight + FO reaction for revelation moments.
var _last_revelation_count: int = -1

# GATE.S8.THREAT.ALERT_UI.001: Supply shock alert label.
var _supply_alert_label: Label = null


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

	# FEEL_POST_FIX_4: Full-screen dark scrim — dims 3D world behind galaxy map / dashboard.
	# Without this, Starlight skybox drowns out galaxy map beacons.
	_overlay_scrim = ColorRect.new()
	_overlay_scrim.name = "OverlayScrim"
	_overlay_scrim.color = Color(0.0, 0.02, 0.05, 0.7)
	_overlay_scrim.anchor_right = 1.0
	_overlay_scrim.anchor_bottom = 1.0
	_overlay_scrim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_overlay_scrim.visible = false
	add_child(_overlay_scrim)
	move_child(_overlay_scrim, 0)  # Behind all HUD elements

	_combat_label = Label.new()
	_combat_label.name = "CombatLabel"
	_combat_label.text = ""
	_combat_label.add_theme_color_override("font_color", Color.RED)
	_combat_label.position = Vector2(10, 256)
	add_child(_combat_label)

	# Captain's Guide: objective breadcrumb (gold, below status panel).
	_guide_objective_label = Label.new()
	_guide_objective_label.name = "GuideObjective"
	_guide_objective_label.text = ""
	_guide_objective_label.add_theme_font_size_override("font_size", 13)
	_guide_objective_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4))
	_guide_objective_label.position = Vector2(10, 272)
	_guide_objective_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_guide_objective_label)

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

	# GATE.S7.COMBAT_PHASE2.HEAT_HUD.001: Heat gauge bar (below combat label).
	_heat_bar = ProgressBar.new()
	_heat_bar.name = "HeatBar"
	_heat_bar.position = Vector2(10, 258)
	_heat_bar.size = Vector2(120, 14)
	_heat_bar.min_value = 0
	_heat_bar.max_value = 100
	_heat_bar.value = 0
	_heat_bar.show_percentage = false
	_heat_bar.visible = false
	var heat_fill := StyleBoxFlat.new()
	heat_fill.bg_color = Color(0.2, 0.8, 0.2)  # Green by default
	_heat_bar.add_theme_stylebox_override("fill", heat_fill)
	var heat_bg := StyleBoxFlat.new()
	heat_bg.bg_color = Color(0.15, 0.15, 0.15)
	_heat_bar.add_theme_stylebox_override("background", heat_bg)
	add_child(_heat_bar)

	_heat_label = Label.new()
	_heat_label.name = "HeatLabel"
	_heat_label.text = "HEAT"
	_heat_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_heat_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_heat_label.position = Vector2(135, 256)
	_heat_label.visible = false
	add_child(_heat_label)

	# GATE.S7.COMBAT_PHASE2.HEAT_HUD.001: Battle stations state indicator.
	_battle_stations_label = Label.new()
	_battle_stations_label.name = "BattleStationsLabel"
	_battle_stations_label.text = ""
	_battle_stations_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_battle_stations_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_battle_stations_label.position = Vector2(185, 256)
	_battle_stations_label.visible = false
	add_child(_battle_stations_label)

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

	# GATE.S6.UI_DISCOVERY.SCAN_VIZ.001: Scan progress indicator.
	_scan_progress_label = Label.new()
	_scan_progress_label.name = "ScanProgressLabel"
	_scan_progress_label.text = ""
	_scan_progress_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_scan_progress_label.add_theme_color_override("font_color", UITheme.YELLOW)
	_scan_progress_label.position = Vector2(10, 420)
	_scan_progress_label.visible = false
	add_child(_scan_progress_label)

	_scan_progress_bar = ProgressBar.new()
	_scan_progress_bar.name = "ScanProgressBar"
	_scan_progress_bar.custom_minimum_size = Vector2(140, 10)
	_scan_progress_bar.max_value = 100
	_scan_progress_bar.value = 0
	_scan_progress_bar.show_percentage = false
	_scan_progress_bar.position = Vector2(10, 440)
	_scan_progress_bar.visible = false
	var scan_fill := StyleBoxFlat.new()
	scan_fill.bg_color = UITheme.YELLOW
	_scan_progress_bar.add_theme_stylebox_override("fill", scan_fill)
	var scan_bg := StyleBoxFlat.new()
	scan_bg.bg_color = Color(0.1, 0.12, 0.15, 0.8)
	_scan_progress_bar.add_theme_stylebox_override("background", scan_bg)
	add_child(_scan_progress_bar)

	# GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001: Active discovery leads panel.
	_active_leads_panel = PanelContainer.new()
	_active_leads_panel.name = "ActiveLeadsPanel"
	_active_leads_panel.visible = false
	_active_leads_panel.position = Vector2(8, 462)
	_active_leads_panel.custom_minimum_size = Vector2(250, 0)
	_active_leads_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var leads_style := StyleBoxFlat.new()
	leads_style.bg_color = Color(0.05, 0.07, 0.12, 0.85)
	leads_style.border_width_left = 2
	leads_style.border_color = UITheme.PURPLE_LIGHT
	_active_leads_panel.add_theme_stylebox_override("panel", leads_style)
	add_child(_active_leads_panel)

	_active_leads_vbox = VBoxContainer.new()
	_active_leads_vbox.add_theme_constant_override("separation", 2)
	_active_leads_panel.add_child(_active_leads_vbox)

	_active_leads_title = Label.new()
	_active_leads_title.text = "ACTIVE LEADS"
	_active_leads_title.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_active_leads_title.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	_active_leads_vbox.add_child(_active_leads_title)

	_active_leads_labels = []
	for i in range(3):
		var lbl := Label.new()
		lbl.name = "LeadLabel%d" % i
		lbl.text = ""
		lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		lbl.visible = false
		_active_leads_vbox.add_child(lbl)
		_active_leads_labels.append(lbl)

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

	# GATE.X.WARP.ARRIVAL_DRAMA.001: Setup letterbox + title card nodes.
	_setup_arrival_drama_v0()

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

	# FEEL_POST_FIX_9: Combat vignette — red border glow during active combat.
	# Uses a TextureRect with a procedural gradient, but simpler: 4 edge ColorRects.
	_combat_vignette = ColorRect.new()
	_combat_vignette.name = "CombatVignette"
	_combat_vignette.color = Color(0.8, 0.05, 0.02, 0.0)
	_combat_vignette.position = Vector2.ZERO
	_combat_vignette.size = Vector2(1920, 1080)
	_combat_vignette.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_combat_vignette.visible = true
	# Use a shader for edge-only glow (inner area transparent, edges tinted red).
	var vignette_shader := ShaderMaterial.new()
	var shader_code := Shader.new()
	shader_code.code = "shader_type canvas_item;\nuniform vec4 tint_color : source_color = vec4(0.8, 0.05, 0.02, 0.0);\nvoid fragment() {\n\tvec2 uv = UV * 2.0 - 1.0;\n\tfloat d = max(abs(uv.x), abs(uv.y));\n\tfloat edge = smoothstep(0.6, 1.0, d);\n\tCOLOR = vec4(tint_color.rgb, tint_color.a * edge);\n}"
	vignette_shader.shader = shader_code
	_combat_vignette.material = vignette_shader
	add_child(_combat_vignette)

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
	_keybind_hint_label.text = _build_keybind_hint_text()
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
	_fleet_auto_panel.visible = false
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

	# GATE.X.UI_POLISH.MISSION_JOURNAL.001: Mission journal panel (J key).
	var MissionJournalPanelScript := preload("res://scripts/ui/mission_journal_panel.gd")
	_mission_journal_panel = MissionJournalPanelScript.new()
	add_child(_mission_journal_panel)

	# GATE.S8.MEGAPROJECT.UI.001: Megaproject construction panel (M key).
	var MegaprojectPanelScript := preload("res://scripts/ui/megaproject_panel.gd")
	_megaproject_panel = MegaprojectPanelScript.new()
	add_child(_megaproject_panel)

	# GATE.S7.UI_WARFRONT.DASHBOARD.001: Warfront dashboard panel (N key).
	var WarfrontPanelScript := preload("res://scripts/ui/warfront_panel.gd")
	_warfront_panel = WarfrontPanelScript.new()
	add_child(_warfront_panel)

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

	# FEEL_POST_FIX_5: Transit destination overlay (centered, shown during warp transit).
	_transit_dest_label = Label.new()
	_transit_dest_label.name = "TransitDestLabel"
	_transit_dest_label.text = ""
	_transit_dest_label.add_theme_font_size_override("font_size", UITheme.FONT_HUD_MED)
	_transit_dest_label.add_theme_color_override("font_color", Color(0.85, 0.9, 1.0, 0.9))
	_transit_dest_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_transit_dest_label.position = Vector2(0, 480)
	_transit_dest_label.size = Vector2(1920, 40)
	_transit_dest_label.visible = false
	_transit_dest_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_transit_dest_label)

	# Transit progress bar background (thin horizontal line below destination label).
	_transit_progress_bar = ColorRect.new()
	_transit_progress_bar.name = "TransitProgressBg"
	_transit_progress_bar.color = Color(0.2, 0.25, 0.35, 0.4)
	_transit_progress_bar.position = Vector2(810, 525)
	_transit_progress_bar.size = Vector2(300, 3)
	_transit_progress_bar.visible = false
	_transit_progress_bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_transit_progress_bar)

	_transit_progress_fill = ColorRect.new()
	_transit_progress_fill.name = "TransitProgressFill"
	_transit_progress_fill.color = Color(0.6, 0.75, 1.0, 0.7)
	_transit_progress_fill.position = Vector2(810, 525)
	_transit_progress_fill.size = Vector2(0, 3)
	_transit_progress_fill.visible = false
	_transit_progress_fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_transit_progress_fill)

	# GATE.X.WARP.TRANSIT_HUD.001: Warp transit HUD overlay (replaces basic transit label).
	var WarpTransitHudScript := preload("res://scripts/ui/warp_transit_hud.gd")
	_warp_transit_hud = WarpTransitHudScript.new()
	add_child(_warp_transit_hud)

	# GATE.X.UI_POLISH.QUEST_TRACKER.001: Persistent quest tracker widget (top-right).
	_quest_tracker_panel = PanelContainer.new()
	_quest_tracker_panel.name = "QuestTrackerPanel"
	_quest_tracker_panel.visible = false
	_quest_tracker_panel.position = Vector2(1650, 8)
	_quest_tracker_panel.custom_minimum_size = Vector2(260, 0)
	_quest_tracker_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var qt_style := StyleBoxFlat.new()
	qt_style.bg_color = Color(0.05, 0.07, 0.12, 0.85)
	qt_style.border_color = UITheme.GOLD
	qt_style.border_width_left = 2
	qt_style.set_corner_radius_all(UITheme.CORNER_SM)
	qt_style.content_margin_left = 10.0
	qt_style.content_margin_right = 10.0
	qt_style.content_margin_top = 6.0
	qt_style.content_margin_bottom = 6.0
	_quest_tracker_panel.add_theme_stylebox_override("panel", qt_style)
	add_child(_quest_tracker_panel)

	var qt_vbox := VBoxContainer.new()
	qt_vbox.add_theme_constant_override("separation", 3)
	_quest_tracker_panel.add_child(qt_vbox)

	_quest_tracker_name_label = Label.new()
	_quest_tracker_name_label.name = "QuestTrackerName"
	_quest_tracker_name_label.text = ""
	_quest_tracker_name_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_quest_tracker_name_label.add_theme_color_override("font_color", UITheme.GOLD)
	qt_vbox.add_child(_quest_tracker_name_label)

	_quest_tracker_step_label = Label.new()
	_quest_tracker_step_label.name = "QuestTrackerStep"
	_quest_tracker_step_label.text = ""
	_quest_tracker_step_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_quest_tracker_step_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_quest_tracker_step_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_quest_tracker_step_label.custom_minimum_size = Vector2(240, 0)
	qt_vbox.add_child(_quest_tracker_step_label)

	_quest_tracker_progress = ProgressBar.new()
	_quest_tracker_progress.name = "QuestTrackerProgress"
	_quest_tracker_progress.custom_minimum_size = Vector2(200, 8)
	_quest_tracker_progress.min_value = 0.0
	_quest_tracker_progress.max_value = 1.0
	_quest_tracker_progress.value = 0.0
	_quest_tracker_progress.show_percentage = false
	var qt_fill := StyleBoxFlat.new()
	qt_fill.bg_color = UITheme.GOLD
	_quest_tracker_progress.add_theme_stylebox_override("fill", qt_fill)
	var qt_bg := StyleBoxFlat.new()
	qt_bg.bg_color = Color(0.15, 0.15, 0.15)
	_quest_tracker_progress.add_theme_stylebox_override("background", qt_bg)
	qt_vbox.add_child(_quest_tracker_progress)

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

	# GATE.S8.THREAT.ALERT_UI.001: Supply shock alert label (left side, below active leads).
	_supply_alert_label = Label.new()
	_supply_alert_label.name = "SupplyAlertLabel"
	_supply_alert_label.text = ""
	_supply_alert_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_supply_alert_label.add_theme_color_override("font_color", UITheme.RED)
	_supply_alert_label.position = Vector2(8, 520)
	_supply_alert_label.visible = false
	_supply_alert_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_supply_alert_label)

func show_game_over_v0() -> void:
	if _game_over_panel != null:
		_game_over_panel.visible = true
	print("UUIR|GAME_OVER_SHOWN")

func set_overlay_mode_v0(active: bool, is_transit: bool = false) -> void:
	_overlay_active = active
	# FEEL_POST_FIX_4: Show dark scrim for empire dashboard (not galaxy map or transit).
	# Galaxy map dims Starlight directly in GalaxyView.SetOverlayOpenV0 instead,
	# because a 2D scrim would dim the 3D beacons equally.
	var gm = get_node_or_null("/root/GameManager")
	if _overlay_scrim:
		var is_galaxy_map: bool = gm != null and gm.get("galaxy_overlay_open") == true
		_overlay_scrim.visible = active and not is_transit and not is_galaxy_map
	var bg = get_node_or_null("HudStatusBg")
	if bg: bg.visible = not active
	for lbl in [_credits_label, _cargo_label, _node_label, _state_label,
				_hull_bar, _shield_bar, _hull_label, _shield_label]:
		if lbl != null: lbl.visible = not active
	if _fuel_label: _fuel_label.visible = not active
	if _galaxy_map_label:
		# FEEL_POST_FIX_5: Only show galaxy map label when galaxy map is the active overlay,
		# not when empire dashboard is open at flight altitude.
		# FEEL_POST_FIX_7: Also hide when empire dashboard is open on top of galaxy map.
		var gm_open = gm != null and gm.get("galaxy_overlay_open") == true
		var dash_open = gm != null and gm.get("empire_dashboard_open") == true
		var show_map_label: bool = active and not is_transit and gm_open and not dash_open
		if show_map_label:
			_galaxy_map_label.size.x = get_viewport().get_visible_rect().size.x
		_galaxy_map_label.visible = show_map_label
	# GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001: Hide Zone G bar during overlay.
	if _zone_g_bg: _zone_g_bg.visible = not active
	if _zone_g_bar: _zone_g_bar.visible = not active
	if _keybind_hint_label: _keybind_hint_label.visible = not active
	if _fleet_auto_panel: _fleet_auto_panel.visible = not active
	if _active_leads_panel: _active_leads_panel.visible = not active
	if _combat_hud: _combat_hud.visible = not active
	if _alert_badge: _alert_badge.visible = (not active) and _alert_count > 0
	# FO panel: hide during galaxy overlay (same as data_log, knowledge_web).
	if _fo_panel and active: _fo_panel.visible = false
	if _data_log_panel and active: _data_log_panel.visible = false
	if _knowledge_web_panel and active: _knowledge_web_panel.visible = false
	if _mission_journal_panel and active: _mission_journal_panel.visible = false
	if _warfront_panel and active: _warfront_panel.visible = false
	if _heat_bar: _heat_bar.visible = not active
	if _heat_label: _heat_label.visible = not active
	if _battle_stations_label: _battle_stations_label.visible = not active
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
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var raw_state = str(ps.get("ship_state_token", ""))
	# FEEL_POST_FIX_9: Belt-and-suspenders transit hide. Check bridge state,
	# game_manager state, AND suppress_transit_overlay flag (set during flyby arrival).
	var gm = get_node_or_null("/root/GameManager")
	var gm_in_transit: bool = gm != null and gm.get("current_player_state") == gm.PlayerShipState.IN_LANE_TRANSIT
	var bridge_in_transit: bool = raw_state == "IN_LANE_TRANSIT"
	var gm_suppress: bool = gm != null and gm.get("suppress_transit_overlay") == true
	if bridge_in_transit and gm_in_transit and not gm_suppress:
		_show_transit_overlay_v0(_get_transit_dest_name())
		if _warp_transit_hud:
			_warp_transit_hud.set_process(true)
	else:
		_hide_transit_overlay_v0()
		if _warp_transit_hud:
			_warp_transit_hud.visible = false
			_warp_transit_hud.set_process(false)
	if _overlay_active:
		return
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	# GATE.S12.UX_POLISH.CARGO_DISPLAY.001: show "X items" suffix
	# FEEL_POST_FIX_7: Correct pluralization ("1 item" not "1 items").
	var _cc: int = int(ps.get("cargo_count", 0))
	_cargo_label.text = "Cargo: %d %s" % [_cc, "item" if _cc == 1 else "items"]
	var node_display = str(ps.get("node_name", ps.get("current_node_id", "")))
	# FEEL_POST_FIX_9: Strip ALL parenthesized tags for clean system name in HUD.
	_node_label.text = _strip_paren_tags(node_display)

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
			# Transit overlay already managed above (FEEL_POST_FIX_8).
		_:
			# FEEL_POST_BASELINE: Show "COMBAT" when hostile NPCs are in aggro range.
			var in_combat := _is_hostile_nearby()
			if in_combat:
				_state_label.text = "COMBAT"
				# FEEL_POST_FIX_4: Bright orange-yellow to distinguish from "DANGEROUS"
				# security label (which uses red). Both using red was ambiguous.
				_state_label.add_theme_color_override("font_color", Color(1.0, 0.7, 0.1))
			else:
				_state_label.text = "Flying"
				_state_label.remove_theme_color_override("font_color")

	# FEEL_POST_FIX_8: Hide combat HUD (zone armor + stance) during non-combat flight.
	var _in_combat_now: bool = raw_state != "DOCKED" and raw_state != "IN_LANE_TRANSIT" and _is_hostile_nearby()
	if _combat_hud:
		_combat_hud.visible = _in_combat_now

	# FEEL_POST_FIX_9: Combat vignette — fade in/out red border glow.
	# Suppress during galaxy map overlay and warp transit to prevent bleed.
	var show_combat_vignette: bool = _in_combat_now and not _overlay_active
	if _combat_vignette:
		var mat = _combat_vignette.material as ShaderMaterial
		if mat:
			if show_combat_vignette and not _combat_vignette_active:
				_combat_vignette_active = true
				var tw := create_tween()
				tw.tween_method(func(v): mat.set_shader_parameter("tint_color", Color(0.8, 0.05, 0.02, v)), 0.0, 0.35, 0.4)
			elif not show_combat_vignette and _combat_vignette_active:
				_combat_vignette_active = false
				var tw := create_tween()
				tw.tween_method(func(v): mat.set_shader_parameter("tint_color", Color(0.8, 0.05, 0.02, v)), 0.35, 0.0, 0.6)

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
			# Hull urgency coloring: red < 20%, yellow 20-50%, white > 50%.
			var hull_pct: float = float(hull) / float(hull_max)
			if hull_pct < 0.15 and hull_pct > 0.0:
				_hull_label.text = "Hull: " + str(hull) + " / " + str(hull_max) + "  CRITICAL"
				_hull_label.add_theme_color_override("font_color", Color(1.0, 0.2, 0.15))
			elif hull_pct < 0.2:
				_hull_label.text = "Hull: " + str(hull) + " / " + str(hull_max)
				_hull_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.2))
			elif hull_pct < 0.5:
				_hull_label.text = "Hull: " + str(hull) + " / " + str(hull_max)
				_hull_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.2))
			else:
				_hull_label.text = "Hull: " + str(hull) + " / " + str(hull_max)
				_hull_label.remove_theme_color_override("font_color")
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

	# GATE.S7.COMBAT_PHASE2.HEAT_HUD.001: Heat gauge + battle stations indicator.
	# Only show heat bar during active combat (BattleReady) — not at rest.
	var _show_heat := false
	if _heat_bar and _bridge and _bridge.has_method("GetHeatSnapshotV0"):
		if _bridge.has_method("GetBattleStationsStateV0"):
			var _bs_check: Dictionary = _bridge.call("GetBattleStationsStateV0")
			_show_heat = _bs_check.get("state", "StandDown") == "BattleReady"
		var heat: Dictionary = _bridge.call("GetHeatSnapshotV0")
		var hc: int = heat.get("heat_current", 0)
		var cap: int = heat.get("heat_capacity", 1000)
		var overheated: bool = heat.get("is_overheated", false)
		var locked_out: bool = heat.get("is_locked_out", false)
		if cap > 0 and _show_heat:
			_heat_bar.max_value = cap * 2  # Show up to 2x capacity for overheat/lockout
			_heat_bar.value = hc
			_heat_bar.visible = true
			_heat_label.visible = true
			# Color coding: green (<50%), yellow (50-99%), red (overheated), pulsing red (lockout)
			var fill_style: StyleBoxFlat = _heat_bar.get_theme_stylebox("fill") as StyleBoxFlat
			if fill_style:
				if locked_out:
					# Pulsing red for lockout: alternate between red and dark red
					var pulse := absf(sin(Time.get_ticks_msec() * 0.005))
					fill_style.bg_color = Color(0.9, 0.1 * pulse, 0.1 * pulse)
				elif overheated:
					fill_style.bg_color = Color(0.9, 0.15, 0.1)
				elif hc > int(cap / 2.0):
					fill_style.bg_color = Color(0.9, 0.75, 0.1)
				else:
					fill_style.bg_color = Color(0.2, 0.8, 0.2)
			# GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001: Feed overheat to screen edge shimmer.
			if _screen_edge_tint and _screen_edge_tint.has_method("set_combat_overheat"):
				_screen_edge_tint.set_combat_overheat(float(hc) / float(cap))
			# GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001: Vent burst flash on lockout transition.
			if locked_out and not _prev_locked_out and _damage_flash:
				_damage_flash.color = Color(1.0, 0.6, 0.1, 0.35)
				var tw := create_tween()
				tw.tween_property(_damage_flash, "color:a", 0.0, 0.6)
			_prev_locked_out = locked_out
		else:
			_heat_bar.visible = false
			_heat_label.visible = false
			if _screen_edge_tint and _screen_edge_tint.has_method("set_combat_overheat"):
				_screen_edge_tint.set_combat_overheat(0.0)

	if _battle_stations_label and _bridge and _bridge.has_method("GetBattleStationsStateV0"):
		var bs: Dictionary = _bridge.call("GetBattleStationsStateV0")
		var bs_state: String = bs.get("state", "StandDown")
		match bs_state:
			"BattleReady":
				_battle_stations_label.text = "BATTLE READY"
				_battle_stations_label.add_theme_color_override("font_color", UITheme.RED)
				_battle_stations_label.visible = true
			"SpinningUp":
				var ticks_left: int = bs.get("spin_up_ticks_remaining", 0)
				_battle_stations_label.text = "SPINNING UP (%d)" % ticks_left
				_battle_stations_label.add_theme_color_override("font_color", UITheme.ORANGE)
				_battle_stations_label.visible = true
			_:
				_battle_stations_label.visible = false

	# GATE.S11.GAME_FEEL.MISSION_HUD.001 + RESEARCH_HUD.001: slow-poll (every 2s)
	_slow_poll_elapsed += _delta
	if _slow_poll_elapsed >= _SLOW_POLL_INTERVAL:
		_slow_poll_elapsed = 0.0
		_update_mission_hud()
		_update_research_hud()
		_update_fuel_hud()
		_update_scan_progress_v0()
		_update_zone_g_v0()
		_update_fleet_auto_summary_v0()
		if _combat_hud and _combat_hud.has_method("refresh_v0"):
			_combat_hud.refresh_v0()
		_check_fracture_unlock_toast_v0()
		# GATE.X.UI_POLISH.QUEST_TRACKER.001: Update quest tracker widget.
		_update_quest_tracker_v0()
		# GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001: Update active leads panel.
		_update_active_leads_v0()
		# GATE.S19.ONBOARD.HUD_DISCLOSURE.010: Update onboarding disclosure state.
		_update_onboarding_disclosure_v0()
		# GATE.S8.STORY_STATE.DELIVERY_UI.001: Check for new revelation moments.
		_check_revelation_delivery_v0()
		# GATE.S8.THREAT.ALERT_UI.001: Check for supply shock alerts.
		_check_supply_alerts_v0()

	# GATE.S5.SEC_LANES.UI.001: security band display
	if _security_label != null and _bridge != null:
		var node_id: String = str(ps.get("current_node_id", ""))
		if not node_id.is_empty() and _bridge.has_method("GetNodeSecurityBandV0"):
			var band: String = str(_bridge.call("GetNodeSecurityBandV0", node_id))
			var display_band: String = _security_display_name(band)
			_security_label.text = display_band
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
			var node_display: String = tgt_node
			if _bridge and _bridge.has_method("GetNodeDisplayNameV0"):
				node_display = str(_bridge.call("GetNodeDisplayNameV0", tgt_node))
			if node_display.is_empty() or node_display == tgt_node:
				node_display = tgt_node.replace("_", " ").capitalize()
			detail_parts.append(node_display)
		if not tgt_good.is_empty():
			var good_display: String = tgt_good.replace("_", " ").capitalize()
			if _bridge and _bridge.has_method("FormatDisplayNameV0"):
				good_display = str(_bridge.call("FormatDisplayNameV0", tgt_good))
			detail_parts.append(good_display)
		if detail_parts.size() > 0:
			step_parts.append("Target: %s" % ", ".join(detail_parts))
		_mission_step_label.text = "\n".join(step_parts)
	else:
		_mission_panel.visible = true
		_mission_title_label.text = "No active mission"
		_mission_title_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_mission_step_label.text = ""

# GATE.X.UI_POLISH.QUEST_TRACKER.001: Quest tracker update (called every 2s via slow poll).
func _update_quest_tracker_v0() -> void:
	if _quest_tracker_panel == null or _bridge == null:
		return
	if not _bridge.has_method("GetActiveMissionSummaryV0"):
		_quest_tracker_panel.visible = false
		return
	# Hide tracker when docked (dock menu shows mission info) or overlay active.
	if _overlay_active:
		_quest_tracker_panel.visible = false
		return
	var summary: Dictionary = _bridge.call("GetActiveMissionSummaryV0")
	var has_mission: bool = summary.get("has_mission", false)
	if not has_mission:
		_quest_tracker_panel.visible = false
		return
	_quest_tracker_panel.visible = true
	_quest_tracker_name_label.text = str(summary.get("mission_name", ""))
	var step_idx: int = int(summary.get("step_index", 0))
	var total: int = int(summary.get("total_steps", 0))
	var step_text: String = str(summary.get("step_text", ""))
	_quest_tracker_step_label.text = "Step %d/%d: %s" % [step_idx + 1, total, step_text] if total > 0 else step_text
	_quest_tracker_progress.value = float(summary.get("progress", 0.0))

# GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001: Active leads update (called every 2s via slow poll).
func _update_active_leads_v0() -> void:
	if _active_leads_panel == null or _bridge == null:
		return
	if not _bridge.has_method("GetActiveLeadsV0"):
		_active_leads_panel.visible = false
		return
	# Flight mode only: hide when overlay (galaxy map / dashboard) is active.
	if _overlay_active:
		_active_leads_panel.visible = false
		return
	var leads: Array = _bridge.call("GetActiveLeadsV0")
	if leads.size() == 0:
		_active_leads_panel.visible = false
		return
	_active_leads_panel.visible = true
	for i in range(3):
		var lbl: Label = _active_leads_labels[i]
		if i < leads.size():
			var lead: Dictionary = leads[i]
			var node_name: String = str(lead.get("node_name", ""))
			var payoff: String = str(lead.get("payoff_token", "")).replace("_", " ").to_lower()
			var verb: String = str(lead.get("source_verb", ""))
			var icon: String = _lead_type_icon(verb)
			# Humanize raw internal node names (e.g., "T1SECTOR_UNKNOWN" → "Unknown Sector").
			node_name = _humanize_lead_name(node_name)
			if node_name.is_empty():
				lbl.text = "%s %s" % [icon, payoff]
			else:
				lbl.text = "%s %s — %s" % [icon, node_name, payoff]
			lbl.visible = true
		else:
			lbl.text = ""
			lbl.visible = false

func _humanize_lead_name(raw: String) -> String:
	if raw.is_empty():
		return ""
	# Strip internal prefixes and IDs.
	if raw.begins_with("T1") or raw.begins_with("T2") or raw.begins_with("T3"):
		return "Unknown Sector"
	if "UNKNOWN" in raw.to_upper():
		return "Unknown Sector"
	# Convert star_N to "System N+1" for readability.
	if raw.begins_with("star_"):
		var idx_str := raw.substr(5)
		if idx_str.is_valid_int():
			return "System %d" % (int(idx_str) + 1)
	# Replace underscores and title-case.
	return raw.replace("_", " ").capitalize()

func _lead_type_icon(verb: String) -> String:
	match verb:
		"EXPLORE":       return "[E]"
		"HUB_ANALYSIS":  return "[H]"
		"SCAN":          return "[S]"
		"EXPEDITION":    return "[X]"
		_:               return "[?]"

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

# GATE.S6.UI_DISCOVERY.SCAN_VIZ.001: scan progress update
func _update_scan_progress_v0() -> void:
	if _scan_progress_label == null or _bridge == null:
		return
	if not _bridge.has_method("GetDiscoverySnapshotV0") or not _bridge.has_method("GetPlayerStateV0"):
		_scan_progress_label.visible = false
		if _scan_progress_bar: _scan_progress_bar.visible = false
		return

	var ps: Dictionary = _bridge.call("GetPlayerStateV0") if _bridge.has_method("GetPlayerStateV0") else {}
	var node_id: String = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		_scan_progress_label.visible = false
		if _scan_progress_bar: _scan_progress_bar.visible = false
		return

	var discoveries: Array = _bridge.call("GetDiscoverySnapshotV0", node_id)
	if discoveries.size() == 0:
		_scan_progress_label.visible = false
		if _scan_progress_bar: _scan_progress_bar.visible = false
		return

	# Show scan status for the first discoverable site.
	var disc: Dictionary = discoveries[0]
	var phase: String = str(disc.get("phase", "SEEN"))
	var site_id: String = str(disc.get("site_id", ""))

	_scan_progress_label.visible = true
	if _scan_progress_bar: _scan_progress_bar.visible = true

	match phase:
		"SEEN":
			_scan_progress_label.text = "SCAN: %s [READY]" % site_id.substr(0, 16)
			_scan_progress_label.add_theme_color_override("font_color", UITheme.YELLOW)
			if _scan_progress_bar: _scan_progress_bar.value = 0
		"SCANNED":
			_scan_progress_label.text = "SCAN: %s [SCANNED]" % site_id.substr(0, 16)
			_scan_progress_label.add_theme_color_override("font_color", UITheme.GREEN)
			if _scan_progress_bar: _scan_progress_bar.value = 50
		"ANALYZED":
			_scan_progress_label.text = "SCAN: %s [ANALYZED]" % site_id.substr(0, 16)
			_scan_progress_label.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
			if _scan_progress_bar: _scan_progress_bar.value = 100
		_:
			_scan_progress_label.text = "SCAN: %s" % phase
			_scan_progress_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
			if _scan_progress_bar: _scan_progress_bar.value = 0

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

# GATE.X.UI_POLISH.MISSION_JOURNAL.001: toggle mission journal panel.
func toggle_mission_journal_v0() -> void:
	if _mission_journal_panel != null and _mission_journal_panel.has_method("toggle_v0"):
		_mission_journal_panel.toggle_v0()

# GATE.S8.MEGAPROJECT.UI.001: toggle megaproject panel.
func toggle_megaproject_panel_v0(node_id: String = "") -> void:
	if _megaproject_panel != null and _megaproject_panel.has_method("toggle_v0"):
		_megaproject_panel.toggle_v0(node_id)

# GATE.S7.UI_WARFRONT.DASHBOARD.001: toggle warfront dashboard panel.
func toggle_warfront_dashboard_v0() -> void:
	if _warfront_panel != null and _warfront_panel.has_method("toggle_v0"):
		_warfront_panel.toggle_v0()

# Toggle FO panel visibility (F key). Replaces blanket suppression.
func toggle_fo_panel_v0() -> void:
	if _fo_panel != null:
		_fo_panel.visible = not _fo_panel.visible

# GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001: Update Zone G bottom bar content.
func _update_zone_g_v0() -> void:
	if _zone_g_status_label == null or _bridge == null:
		return
	# Center: show current system + security band.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0") if _bridge.has_method("GetPlayerStateV0") else {}
	var node_id: String = str(ps.get("current_node_id", ""))
	var node_name: String = str(ps.get("node_name", node_id))
	# FEEL_POST_FIX_7: Strip parenthesized resource tags from bottom bar name.
	var paren_pos: int = node_name.find("(")
	if paren_pos > 0:
		node_name = node_name.substr(0, paren_pos).strip_edges()
	var sec_band: String = ""
	if not node_id.is_empty() and _bridge.has_method("GetNodeSecurityBandV0"):
		sec_band = str(_bridge.call("GetNodeSecurityBandV0", node_id))
	var sec_display: String = _security_display_name(sec_band) if not sec_band.is_empty() else ""
	_zone_g_status_label.text = "%s  |  %s" % [node_name, sec_display] if not sec_display.is_empty() else node_name
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
	if not _bridge.has_method("GetProgramPerformanceV0"):
		_fleet_auto_panel.visible = false
		return
	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
	if data.is_empty():
		_fleet_auto_panel.visible = false
		return
	var credits: int = int(data.get("credits_earned", 0))
	var failures: int = int(data.get("failures", 0))
	var cycles: int = int(data.get("cycles_run", 0))
	# Only show when automation has actually run at least one cycle.
	if cycles <= 0:
		_fleet_auto_panel.visible = false
		return
	_fleet_auto_panel.visible = true
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
		# GATE.X.COVER_STORY.UI_ENFORCE.001: Use cover name pre-revelation.
		var drive_name: String = "FRACTURE DRIVE"
		if _bridge.has_method("GetCoverNameV0"):
			drive_name = str(_bridge.call("GetCoverNameV0", "Fracture Drive")).to_upper()
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "%s UNLOCKED — Off-lane travel is now possible." % drive_name, "critical")
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

	# Captain's Guide: update objective breadcrumb.
	_update_guide_objective_v0()

# Captain's Guide: objective breadcrumb progression.
# Disappears after first program/automation setup (not after first trade).
func _update_guide_objective_v0() -> void:
	if _guide_objective_label == null:
		return
	# Respect tutorial toggle.
	var settings_mgr = get_node_or_null("/root/SettingsManager")
	if settings_mgr and settings_mgr.has_method("get_setting"):
		if not bool(settings_mgr.call("get_setting", "gameplay_tutorial_toasts")):
			_guide_objective_label.visible = false
			return

	var has_docked: bool = bool(_onboarding_state.get("has_docked", false))
	var has_traded: bool = bool(_onboarding_state.get("has_traded", false))
	var nodes_visited: int = int(_onboarding_state.get("nodes_visited", 0))

	# Check if player has created a program (automation learned — objective complete).
	var has_program: bool = false
	if _bridge and _bridge.has_method("GetActiveProgramCountV0"):
		has_program = int(_bridge.call("GetActiveProgramCountV0")) > 0

	if has_program:
		_guide_objective_label.visible = false
	elif has_traded and nodes_visited >= 2:
		_guide_objective_label.text = "\u25b8 Set up a program to automate this route"
		_guide_objective_label.visible = true
	elif has_traded:
		_guide_objective_label.text = "\u25b8 Sell at another system for profit"
		_guide_objective_label.visible = true
	elif has_docked:
		_guide_objective_label.text = "\u25b8 Buy goods from the Market"
		_guide_objective_label.visible = true
	elif not _onboarding_state.is_empty():
		_guide_objective_label.text = "\u25b8 Dock at the station ahead"
		_guide_objective_label.visible = true
	else:
		_guide_objective_label.visible = false

# Dock confirmation: show dock prompt with dynamic key label.
func show_dock_prompt_v0(station_name: String = "") -> void:
	if _dock_prompt_label == null:
		return
	var dock_key: String = "E"
	var input_mgr = get_node_or_null("/root/InputManager")
	if input_mgr:
		dock_key = input_mgr.get_action_label("ui_dock_confirm")
	if station_name.is_empty():
		_dock_prompt_label.text = "Press %s to dock" % dock_key
	else:
		_dock_prompt_label.text = "Press %s to dock at %s" % [dock_key, station_name]
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
func _build_keybind_hint_text() -> String:
	var input_mgr = get_node_or_null("/root/InputManager")
	if input_mgr == null:
		return "M Map  |  E Empire  |  F FO  |  K Web  |  L Log  |  D Data  |  B Auto  |  V Overlay  |  H Help"
	var parts: Array[String] = []
	parts.append("%s Map" % input_mgr.get_action_label("ui_galaxy_map"))
	parts.append("%s Empire" % input_mgr.get_action_label("ui_empire_dashboard"))
	parts.append("%s FO" % input_mgr.get_action_label("ui_fo_panel"))
	parts.append("%s Web" % input_mgr.get_action_label("ui_knowledge_web"))
	parts.append("%s Log" % input_mgr.get_action_label("ui_combat_log"))
	parts.append("%s Data" % input_mgr.get_action_label("ui_data_log"))
	parts.append("%s Auto" % input_mgr.get_action_label("ui_automation"))
	parts.append("%s Overlay" % input_mgr.get_action_label("ui_data_overlay"))
	parts.append("%s Help" % input_mgr.get_action_label("ui_keybinds_help"))
	return "  |  ".join(parts)


func _get_transit_dest_name() -> String:
	var gm = get_node_or_null("/root/GameManager")
	if gm == null:
		return ""
	var dest_id = gm.get("_lane_dest_node_id")
	if dest_id == null or str(dest_id).is_empty():
		return ""
	# Resolve display name from bridge snapshot.
	if _bridge and _bridge.has_method("GetNodeDisplayNameV0"):
		var raw_name: String = str(_bridge.call("GetNodeDisplayNameV0", str(dest_id)))
		return _strip_paren_tags(raw_name)
	return str(dest_id)

# FEEL_POST_FIX_8: Strip all parenthesized tags from system names for clean player-facing text.
# "System 10 (Rare Min)(Mining)(Munitions)" → "System 10"
static func _strip_paren_tags(s: String) -> String:
	var idx: int = s.find("(")
	if idx > 0:
		return s.substr(0, idx).strip_edges()
	return s

# FEEL_BASELINE: Flash red overlay on player damage. Called from bullet.gd.
func flash_damage_v0() -> void:
	if _damage_flash == null:
		return
	# FEEL_POST_FIX_7: Stronger flash (0.22 alpha, 0.35s fade) for visible combat feedback.
	_damage_flash.color.a = 0.22
	var tween := create_tween()
	tween.tween_property(_damage_flash, "color:a", 0.0, 0.35)

# FEEL_POST_BASELINE: Check if combat is happening near the player.
# Returns true if a hostile NPC is within 60u OR any fleet is within 25u (close engagement).
func _is_hostile_nearby() -> bool:
	var gm = get_node_or_null("/root/GameManager")
	if gm == null:
		return false
	# FEEL_POST_FIX_3: Event-driven combat detection. combat_state_timer is set
	# by on_hit(), turret fire, and AI fire — reliable regardless of NPC drift.
	if gm.get("combat_state_timer") != null and float(gm.combat_state_timer) > 0.0:
		return true
	if not gm.has_method("_find_nearest_fleet_v0"):
		return false
	# Check for hostile fleet in aggro range.
	var nearest = gm.call("_find_nearest_fleet_v0", 60.0)
	if nearest != null and nearest.get_meta("is_hostile", false):
		return true
	return false

# FEEL_POST_FIX_5: Show/hide transit destination overlay.
func _show_transit_overlay_v0(dest_name: String) -> void:
	if _transit_dest_label == null:
		return
	if dest_name.is_empty():
		_transit_dest_label.text = "Traveling..."
	else:
		_transit_dest_label.text = "→  %s" % dest_name
	_transit_dest_label.visible = true
	if _transit_progress_bar:
		_transit_progress_bar.visible = true
	if _transit_progress_fill:
		_transit_progress_fill.visible = true
		# Animate fill: grow over time using a simple approach
		# (full progress not available — use a pulse animation instead).
		var t := fmod(Time.get_ticks_msec() / 3000.0, 1.0)
		_transit_progress_fill.size.x = t * 300.0

func _hide_transit_overlay_v0() -> void:
	if _transit_dest_label:
		_transit_dest_label.visible = false
	if _transit_progress_bar:
		_transit_progress_bar.visible = false
	if _transit_progress_fill:
		_transit_progress_fill.visible = false

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

# ============================================================================
# GATE.X.WARP.ARRIVAL_DRAMA.001: Warp arrival letterbox + title card
# ============================================================================

## Creates letterbox bars (top/bottom) and centered title card labels.
## All start hidden/off-screen; activated by show_arrival_drama_v0().
func _setup_arrival_drama_v0() -> void:
	# Top bar: anchored full width at top, 0 height initially (slides down).
	_arrival_bar_top = ColorRect.new()
	_arrival_bar_top.name = "ArrivalBarTop"
	_arrival_bar_top.color = Color(0.0, 0.0, 0.0, 0.4)
	_arrival_bar_top.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_arrival_bar_top.offset_bottom = 0.0  # height = 0 (collapsed)
	_arrival_bar_top.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_arrival_bar_top.visible = false
	add_child(_arrival_bar_top)

	# Bottom bar: anchored full width at bottom, offset_top = 0 (collapsed).
	_arrival_bar_bot = ColorRect.new()
	_arrival_bar_bot.name = "ArrivalBarBot"
	_arrival_bar_bot.color = Color(0.0, 0.0, 0.0, 0.4)
	_arrival_bar_bot.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	_arrival_bar_bot.anchor_top = 1.0
	_arrival_bar_bot.anchor_bottom = 1.0
	_arrival_bar_bot.offset_top = 0.0  # collapsed upward
	_arrival_bar_bot.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_arrival_bar_bot.visible = false
	add_child(_arrival_bar_bot)

	# System name label: large, centered, white with shadow.
	_arrival_card_name = Label.new()
	_arrival_card_name.name = "ArrivalCardName"
	_arrival_card_name.text = ""
	_arrival_card_name.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_arrival_card_name.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_arrival_card_name.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_arrival_card_name.offset_top = -30.0  # Slightly above center
	_arrival_card_name.add_theme_font_size_override("font_size", 48)
	_arrival_card_name.add_theme_color_override("font_color", Color(0.85, 0.92, 1.0, 1.0))
	_arrival_card_name.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.2, 0.9))
	_arrival_card_name.add_theme_constant_override("shadow_offset_x", 2)
	_arrival_card_name.add_theme_constant_override("shadow_offset_y", 2)
	_arrival_card_name.modulate.a = 0.0
	_arrival_card_name.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_arrival_card_name.visible = false
	add_child(_arrival_card_name)

	# Faction / context subtitle: smaller, centered below system name.
	_arrival_card_faction = Label.new()
	_arrival_card_faction.name = "ArrivalCardFaction"
	_arrival_card_faction.text = ""
	_arrival_card_faction.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_arrival_card_faction.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_arrival_card_faction.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_arrival_card_faction.offset_top = 30.0  # Below center
	_arrival_card_faction.add_theme_font_size_override("font_size", 22)
	_arrival_card_faction.add_theme_color_override("font_color", Color(0.6, 0.75, 0.9, 1.0))
	_arrival_card_faction.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.15, 0.7))
	_arrival_card_faction.add_theme_constant_override("shadow_offset_x", 1)
	_arrival_card_faction.add_theme_constant_override("shadow_offset_y", 1)
	_arrival_card_faction.modulate.a = 0.0
	_arrival_card_faction.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_arrival_card_faction.visible = false
	add_child(_arrival_card_faction)

## Show arrival drama: letterbox bars slide in, title card fades in, holds, then all fade out.
## system_name: display name of the arrived system.
## faction_name: controlling faction (empty string if unclaimed).
func show_arrival_drama_v0(system_name: String, faction_name: String) -> void:
	# Kill any existing drama tween to avoid overlap.
	hide_arrival_drama_v0()

	if not _arrival_bar_top or not _arrival_bar_bot:
		return

	# Reset bar sizes.
	_arrival_bar_top.offset_bottom = 0.0
	_arrival_bar_bot.offset_top = 0.0
	_arrival_bar_top.visible = true
	_arrival_bar_bot.visible = true

	# Set card text.
	_arrival_card_name.text = system_name
	_arrival_card_name.modulate.a = 0.0
	_arrival_card_name.visible = true

	var subtitle_text: String = ""
	if not faction_name.is_empty():
		subtitle_text = faction_name + " Territory"
	_arrival_card_faction.text = subtitle_text
	_arrival_card_faction.modulate.a = 0.0
	_arrival_card_faction.visible = not subtitle_text.is_empty()

	# Tween sequence:
	# 0.0-0.3s: bars slide in (60px each)
	# 0.3-0.7s: title card fades in
	# 0.7-2.2s: hold (1.5s)
	# 2.2-2.6s: title card fades out
	# 2.6-2.9s: bars slide out
	_arrival_drama_tween = create_tween()

	# Phase 1: Bars slide in.
	_arrival_drama_tween.tween_property(_arrival_bar_top, "offset_bottom", 60.0, 0.3) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	_arrival_drama_tween.parallel().tween_property(_arrival_bar_bot, "offset_top", -60.0, 0.3) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)

	# Phase 2: Title card fades in.
	_arrival_drama_tween.tween_property(_arrival_card_name, "modulate:a", 1.0, 0.4) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_SINE)
	if not subtitle_text.is_empty():
		_arrival_drama_tween.parallel().tween_property(_arrival_card_faction, "modulate:a", 1.0, 0.4) \
			.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_SINE)

	# Phase 3: Hold.
	_arrival_drama_tween.tween_interval(1.5)

	# Phase 4: Title card fades out.
	_arrival_drama_tween.tween_property(_arrival_card_name, "modulate:a", 0.0, 0.4) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_SINE)
	if not subtitle_text.is_empty():
		_arrival_drama_tween.parallel().tween_property(_arrival_card_faction, "modulate:a", 0.0, 0.3)

	# Phase 5: Bars slide out.
	_arrival_drama_tween.tween_property(_arrival_bar_top, "offset_bottom", 0.0, 0.3) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	_arrival_drama_tween.parallel().tween_property(_arrival_bar_bot, "offset_top", 0.0, 0.3) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)

	# Cleanup: hide all elements when done.
	_arrival_drama_tween.tween_callback(_hide_arrival_drama_elements)

## Immediately hide/cancel arrival drama (e.g., on dock, menu open, etc.).
func hide_arrival_drama_v0() -> void:
	if _arrival_drama_tween and _arrival_drama_tween.is_valid():
		_arrival_drama_tween.kill()
		_arrival_drama_tween = null
	_hide_arrival_drama_elements()

func _hide_arrival_drama_elements() -> void:
	if _arrival_bar_top:
		_arrival_bar_top.visible = false
		_arrival_bar_top.offset_bottom = 0.0
	if _arrival_bar_bot:
		_arrival_bar_bot.visible = false
		_arrival_bar_bot.offset_top = 0.0
	if _arrival_card_name:
		_arrival_card_name.visible = false
		_arrival_card_name.modulate.a = 0.0
	if _arrival_card_faction:
		_arrival_card_faction.visible = false
		_arrival_card_faction.modulate.a = 0.0

# ============================================================================
# GATE.S8.STORY_STATE.DELIVERY_UI.001: Revelation moment delivery
# ============================================================================

## Polls bridge for new revelation state every slow-poll cycle (~2s).
## When a new revelation fires, emits a gold toast using the milestone priority.
## FO reaction is handled by fo_panel.gd via its own bridge polling.
## Defensive: uses has_method() guards since SimBridge.Story.cs may not exist yet.
func _check_revelation_delivery_v0() -> void:
	if _bridge == null:
		return
	if not _bridge.has_method("GetRevelationStateV0"):
		return
	var state: Dictionary = _bridge.call("GetRevelationStateV0")
	var count: int = int(state.get("revelation_count", 0))
	# First poll: initialise baseline without firing toasts.
	if _last_revelation_count < 0:
		_last_revelation_count = count
		return
	if count <= _last_revelation_count:
		return
	# One or more new revelations have fired since last poll.
	# Fetch the most recent revelation ID and its display text.
	var revelation_id: String = str(state.get("latest_revelation_id", ""))
	var toast_title: String = "REVELATION"
	var toast_body: String = ""
	if not revelation_id.is_empty() and _bridge.has_method("GetRevelationTextV0"):
		var text_data: Dictionary = _bridge.call("GetRevelationTextV0", revelation_id)
		toast_title = str(text_data.get("gold_toast_title", toast_title))
		toast_body = str(text_data.get("gold_toast_body", ""))
	_last_revelation_count = count
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr == null:
		return
	# Use milestone priority (gold border, larger font) for revelation moments.
	var msg: String = toast_title if toast_body.is_empty() else "%s\n%s" % [toast_title, toast_body]
	if toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", msg, "milestone")
	elif toast_mgr.has_method("show_toast"):
		toast_mgr.call("show_toast", msg, 5.0)
	print("UUIR|REVELATION_TOAST|id=%s" % revelation_id)

# ============================================================================
# GATE.S8.THREAT.ALERT_UI.001: Supply shock alert HUD element
# ============================================================================

## Polls bridge for supply shock summary every slow-poll cycle (~2s).
## Shows a red "Supply Disrupted: X goods" label briefly on count change, then auto-hides.
## Defensive: skips gracefully if GetSupplyShockSummaryV0 does not exist yet.
func _check_supply_alerts_v0() -> void:
	if _supply_alert_label == null or _bridge == null:
		return
	if not _bridge.has_method("GetSupplyShockSummaryV0"):
		_supply_alert_label.visible = false
		return
	# Don't show supply alerts until the player has explored enough to understand them.
	if int(_onboarding_state.get("nodes_visited", 0)) < 5:
		_supply_alert_label.visible = false
		return
	var summary: Dictionary = _bridge.call("GetSupplyShockSummaryV0")
	var disrupted: int = int(summary.get("disrupted_count", 0))
	var prev_count: int = int(_supply_alert_label.get_meta("prev_disrupted", 0))
	if disrupted > 0 and disrupted != prev_count:
		_supply_alert_label.set_meta("prev_disrupted", disrupted)
		_supply_alert_label.text = "Supply Disrupted: %d %s" % [disrupted, "good" if disrupted == 1 else "goods"]
		_supply_alert_label.visible = true
		# Auto-hide after 8 seconds — don't persist as permanent HUD clutter.
		var tw := create_tween()
		tw.tween_interval(8.0)
		tw.tween_property(_supply_alert_label, "modulate:a", 0.0, 1.0)
		tw.tween_callback(func():
			if is_instance_valid(_supply_alert_label):
				_supply_alert_label.visible = false
				_supply_alert_label.modulate.a = 1.0)
		# Emit a red warning toast once per disruption spike.
		var already_toasted: bool = bool(_supply_alert_label.get_meta("supply_toasted", false))
		if not already_toasted:
			_supply_alert_label.set_meta("supply_toasted", true)
			var toast_mgr = get_node_or_null("/root/ToastManager")
			if toast_mgr and toast_mgr.has_method("show_priority_toast"):
				toast_mgr.call("show_priority_toast",
					"Supply Disrupted: %d %s affected" % [disrupted, "good" if disrupted == 1 else "goods"],
					"warning")
			print("UUIR|SUPPLY_ALERT|disrupted=%d" % disrupted)
	elif disrupted <= 0:
		_supply_alert_label.visible = false
		_supply_alert_label.set_meta("prev_disrupted", 0)
		# Reset toast flag so a new disruption wave will toast again.
		_supply_alert_label.set_meta("supply_toasted", false)


## Map internal security band names to player-friendly labels.
func _security_display_name(band: String) -> String:
	match band:
		"hostile":   return "Threat: High"
		"dangerous": return "Threat: Elevated"
		"safe":      return "Secure Space"
		_:           return "Threat: Moderate"
