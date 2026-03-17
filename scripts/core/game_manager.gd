extends Node

# sim.gd (GDScript sim) is kept for galaxy_spawner 3D visual scaffolding ONLY.
# It must NOT be ticked or used for game logic. All game logic routes through SimBridge (C#).
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')
const WarpTunnel = preload('res://scripts/vfx/warp_tunnel.gd')
const GateVortex = preload('res://scripts/vfx/gate_vortex.gd')
const GateTransitPopup = preload('res://scripts/ui/gate_transit_popup.gd')
const TractorBeam = preload('res://scripts/vfx/tractor_beam.gd')


# SimCore fleet ID for the player fleet (matches WorldLoader constant).
const PLAYER_FLEET_ID := "fleet_trader_1"

# Hero ship physics init values (v0). Flight logic is owned by hero_ship_flight_controller.gd.
const HERO_LINEAR_DAMPING_V0: float = 0.8
const HERO_ANGULAR_DAMPING_V0: float = 3.0
const HERO_GRAVITY_SCALE_V0: float = 0.0

# Wired in _ready from the local scene (playable_prototype.tscn)
var _hero_body: RigidBody3D

enum PlayerShipState {
	IN_FLIGHT,
	DOCKED,
	IN_LANE_TRANSIT,
	GATE_APPROACH,
}

var sim: Sim
var player: PlayerState

# Centralized ship state for proximity docking v0.
var current_player_state: PlayerShipState = PlayerShipState.IN_FLIGHT
var dock_target_kind_token: String = ""
var dock_target_id: String = ""
var _lane_cooldown_v0: float = 0.0  # Seconds remaining before lane gates can trigger again.
var _gate_approach_declined: bool = false  # Suppresses re-trigger until full zone exit.
var _gate_available_target: String = ""  # Gate near player awaiting E key to jump.
# Dock confirmation: target near player awaiting E key to dock.
var _dock_available_target: Node = null
var _undock_cooldown: float = 0.0  # Prevents re-dock prompt immediately after undock.
# GATE.S8.HAVEN.COMING_HOME.001: Haven first-dock cinematic flag (once per session).
var _haven_cinematic_played: bool = false

# GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001: fleet targeting
var _targeted_fleet_id: String = ""

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: player death state
var _player_dead: bool = false

# GATE.S7.MAIN_MENU.SCENE.001: Main menu guard — skip gameplay logic when on menu screen.
# Default true: autoload _ready fires before main_menu sets this, so we must assume menu until told otherwise.
var _on_main_menu: bool = true

# True when player chose "New Voyage" (not "Continue"). Controls welcome overlay.
var _is_new_game: bool = false

# GATE.S9.STEAM.SDK.001: Steam integration state.
var _steam_enabled: bool = false

# FEEL_POST_FIX_3: Event-driven combat state. Countdown timer set by on_hit/bullet
# signals. HUD reads this to show "COMBAT" without relying on proximity detection
# (which fails when NPCs drift between ticks).
var combat_state_timer: float = 0.0

# Real-time turret combat v0 — tweakable constants (sourced from CombatTweaksV0 for damage).
const TURRET_COOLDOWN_SEC: float = 0.4
const TURRET_RANGE: float = 80.0
const AI_FIRE_COOLDOWN_SEC: float = 0.8
const AI_AGGRO_RANGE: float = 60.0

var _turret_cooldown: float = 0.0
var _ai_fire_cooldown: float = 0.0
var _bullet_scene: PackedScene = null

# GATE.S1.AUDIO.SFX_CORE.001: Procedural synth audio players.
var _sfx_turret_fire: AudioStreamPlayer = null
var _sfx_bullet_hit: AudioStreamPlayer = null
var _sfx_explosion: AudioStreamPlayer = null
var _sfx_engine_thrust: AudioStreamPlayer = null

# GATE.S1.AUDIO.AMBIENT.001: Ambient audio players.
var _sfx_ambient_drone: AudioStreamPlayer = null
var _sfx_warp_whoosh: AudioStreamPlayer = null
var _sfx_dock_chime: AudioStreamPlayer = null
var _sfx_system_arrival: AudioStreamPlayer = null  # Calm ambient stinger for first-visit reveals.

# GATE.S7.MAIN_MENU.PAUSE.001: Pause menu overlay + state
const PauseMenu = preload('res://scripts/ui/pause_menu.gd')
var _paused: bool = false
var _pause_menu: Control = null

# GATE.S12.UX_POLISH.ONBOARDING.001: First-dock onboarding toasts
var _onboarding_shown: bool = false

# Intro sequence active — suppresses all gameplay (AI, combat, NPC movement, music)
# until the welcome overlay is dismissed. Prevents jarring first frames.
var intro_active: bool = false

# GATE.S19.ONBOARD.MILESTONE_TOAST.014: Track celebrated milestones to avoid repeats.
var _celebrated_milestones: Dictionary = {}

# Captain's Guide: fire-once guide step tracking.
var _guide_steps_seen: Dictionary = {}
var _guide_poll_timer: float = 0.0
const GUIDE_POLL_INTERVAL: float = 2.0

# GATE.S7.MAIN_MENU.AUTO_SAVE.001: Auto-save cooldown (30s between saves)
var _autosave_cooldown: float = 0.0
const AUTOSAVE_COOLDOWN_SEC: float = 30.0

# GATE.S11.GAME_FEEL.TOAST_EVENTS.001: Toast event polling state
var _toast_poll_timer: float = 0.0
const TOAST_POLL_INTERVAL: float = 2.0
var _last_research_tech_id: String = ""
var _last_research_active: bool = false
var _last_player_node_id: String = ""

# GATE.S7.FACTION.REP_TOAST.001: Rep change toast tracking
var _last_rep_snapshot: Dictionary = {}  # faction_id -> last known rep value
# GATE.S7.ENFORCEMENT.BRIDGE.001: Track last seen confiscation count for toast.
var _last_confiscation_count: int = 0
# GATE.S7.INSTABILITY_EFFECTS.BRIDGE.001: Track last known instability phases for toast.
var _last_instability_phases: Dictionary = {}  # node_id -> phase_name

# GATE.S6.OUTCOME.CELEBRATION.001: Discovery completion celebration polling
var _discovery_poll_timer: float = 0.0
const DISCOVERY_POLL_INTERVAL: float = 2.0

# GATE.S5.TRACTOR.VFX.001: Auto-loot collection poll (every 0.5s while in flight).
var _loot_poll_timer: float = 0.0
const LOOT_POLL_INTERVAL: float = 0.5
var _prev_discovery_statuses: Dictionary = {}  # discovery_id -> last known status

# GATE.S8.WIN.GAME_OVER_WIRE.001: Endgame transition state.
var _game_over_triggered: bool = false
var _game_over_poll_timer: float = 0.0
const GAME_OVER_POLL_INTERVAL: float = 1.0
var _lane_origin_node_id: String = ""  # GATE.S13.WORLD.GATE_ARRIVAL.001: track origin for gate positioning
var _lane_dest_node_id: String = ""    # Transit destination — read by camera for transit-mode rendering.
var _lane_dest_is_first_visit: bool = false  # Cached before DispatchTravelCommandV0 marks node visited.
var _cinematic_transit_active: bool = false  # Blocks Esc during approach cinematic.
# FEEL_POST_FIX_9: Suppress transit overlay during flyby arrival (state is still IN_LANE_TRANSIT
# but the player has visually "arrived" — showing transit labels during flyby is confusing).
var suppress_transit_overlay: bool = false
var _last_arrival_first_visit: bool = false  # Read by camera for first-visit vista hold.

# Warp transit camera state — read by player_follow_camera during IN_LANE_TRANSIT.
var warp_transit_target: Vector3 = Vector3.ZERO
var warp_transit_altitude: float = 2000.0
var warp_transit_dest_pos: Vector3 = Vector3.ZERO
var warp_transit_travel_dir: Vector3 = Vector3.ZERO
var _transit_marker: Node3D = null
var _transit_lane_line_ref: Node3D = null  # Stored to clean up during reveal.
var _warp_tunnel_ref: Node3D = null  # GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: warp tunnel VFX instance.

# GATE.X.WARP.TRANSIT_HUD.001: Transit timing for HUD progress bar.
var warp_transit_start_msec: int = 0       # Time.get_ticks_msec() when transit begins.
var warp_transit_duration_sec: float = 0.0 # Total transit duration in seconds.
var warp_transit_origin_pos: Vector3 = Vector3.ZERO  # Origin gate position for distance calc.

# Flyby orbit tuning — tweak these to adjust the arrival cinematic.
# Pace overhaul: compressed timing. First visit ~5s, return visits skip flyby entirely (~2.5s).
const FLYBY_APPROACH_DIST: float = 160.0   # Distance from star to start curving (scaled for 120u systems).
const FLYBY_ORBIT_RADIUS: float = 90.0     # Orbit circle radius around star (breathing room inside 120u system).
const FLYBY_ORBIT_ALT: float = 70.0        # Camera height during orbit sweep (above planet level).
const FLYBY_CURVE_ON_TIME: float = 0.6     # Seconds to curve onto orbit (was 1.5).
const FLYBY_ORBIT_TIME: float = 1.5        # Seconds for orbital sweep — first visit only (was 4.0).
const FLYBY_CURVE_OFF_TIME: float = 0.5    # Seconds to settle at destination gate (was 1.5).

# Gate approach state: player is near a gate, popup visible, awaiting confirm.
var _approach_neighbor_id: String = ""
var _gate_popup: Node = null

# Proof helper: used by headless tests to verify the local scene continues ticking
# while the galaxy overlay is open. Do not print this value.
var time_accumulator: float = 0.0

# Galaxy overlay v0 wiring — GATE.S17.REAL_SPACE.GALAXY_MAP.001:
# TAB toggles galaxy_overlay_open flag; player_follow_camera reads it to raise altitude.
# CanvasLayer / dedicated overlay camera removed — follow camera IS the map camera.
var galaxy_overlay_open: bool = false
var _galaxy_view: Node

# Optional UI surfaces (may be null in headless scenes)
var _station_menu: Node
var _hero_trade_menu: Node
var _discovery_panel: Node
# GATE.S10.EMPIRE.SHELL.001: Empire Dashboard (C# node, created by SimBridge._Ready)
var _empire_dashboard: Node = null
# FEEL_POST_FIX_5: Tracked flag so camera doesn't override HUD suppression during dashboard.
var empire_dashboard_open: bool = false

# GATE.S11.GAME_FEEL.KEYBINDS.001: Help overlay (H key)
var _keybinds_help: Node = null

# GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001: Combat log panel (L key)
var _combat_log_panel: Node = null

# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Knowledge web panel (K key)
var _knowledge_web_panel: Node = null

func _is_main_menu_active() -> bool:
	return _on_main_menu

func _ready():
	process_mode = Node.PROCESS_MODE_ALWAYS
	print('SUCCESS: Global Game Manager initialized.')
	sim = Sim.new()
	player = PlayerState.new()
	_init_steam_v0()

	# GATE.S7.MAIN_MENU.SCENE.001: Skip gameplay wiring on main menu.
	if _is_main_menu_active():
		return

	# Background starfield: CanvasLayer -1 behind 3D scene.
	var bg_starfield_script = load("res://scripts/view/background_starfield.gd")
	if bg_starfield_script:
		var bg_starfield = bg_starfield_script.new()
		add_child(bg_starfield)

	# Bootstrap player start position from GDScript galaxy topology.
	# galaxy_spawner.gd reads game_manager.sim for 3D star/lane mesh generation.
	if sim.galaxy_map.stars.size() > 0:
		player.current_node_id = sim.galaxy_map.stars[0].id

	# Scene-local wiring (GameManager is a child of Main in playable_prototype.tscn)
	var root = get_parent()
	_galaxy_view = root.get_node_or_null("GalaxyView")

	# Hero ship wiring (local-only, physics-driven).
	var p = root.get_node_or_null("Player")
	if p and p is RigidBody3D:
		_hero_body = p
		_hero_body.gravity_scale = HERO_GRAVITY_SCALE_V0
		_hero_body.linear_damp = HERO_LINEAR_DAMPING_V0
		_hero_body.angular_damp = HERO_ANGULAR_DAMPING_V0

	_station_menu = root.get_node_or_null("UI/StationMenu")
	if _station_menu and _station_menu.has_signal("request_undock"):
		var c = Callable(self, "_on_station_menu_request_undock")
		if not _station_menu.is_connected("request_undock", c):
			_station_menu.connect("request_undock", c)
	_hero_trade_menu = root.get_node_or_null("UI/HeroTradeMenu")

	# GATE.S1.DISCOVERY_INTERACT.PANEL.001: discovery site dock panel v0
	_discovery_panel = root.get_node_or_null("UI/DiscoverySitePanel")
	if _discovery_panel == null:
		var dsp_script = load("res://scripts/ui/DiscoverySitePanel.gd")
		if dsp_script:
			_discovery_panel = dsp_script.new()
			_discovery_panel.name = "DiscoverySitePanel"
			var ui = root.get_node_or_null("UI")
			if ui:
				ui.add_child(_discovery_panel)
			else:
				add_child(_discovery_panel)
	if _discovery_panel and _discovery_panel.has_signal("request_undock"):
		var c = Callable(self, "_on_discovery_panel_request_undock")
		if not _discovery_panel.is_connected("request_undock", c):
			_discovery_panel.connect("request_undock", c)

	_bullet_scene = load("res://scenes/bullet.tscn")
	_init_sfx_v0()

	if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
		_galaxy_view.call("SetOverlayOpenV0", false)

	# GATE.S12.UX_POLISH.ONBOARDING.001: Onboarding now triggered from _process
	# after scene transition (camera controller must exist for dramatic reveal).

	# Edgedar: created lazily in _process after _on_main_menu goes false
	# (both _ready instances hit early return while menu is active).

func _show_onboarding_toasts_deferred_v0() -> void:
	# Only the autoload instance shows onboarding (scene-child returns).
	if get_parent() != get_tree().root:
		return
	if _onboarding_shown:
		return
	_onboarding_shown = true
	# Only show full intro sequence on new game, not continue/load.
	if not _is_new_game:
		var toast_mgr = get_node_or_null("/root/ToastManager")
		await get_tree().create_timer(1.0).timeout
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Welcome back, Captain.", 4.0)
		return
	# Activate intro mode — suppresses gameplay (AI fire, NPC movement, combat music).
	intro_active = true
	var cam_ctrl = _find_camera_controller()
	if cam_ctrl:
		cam_ctrl.set("input_locked", true)
	# Freeze hero ship during intro.
	if _hero_body:
		_hero_body.freeze = true
	# Intro sequence: Ship Computer cold open → galaxy cinematic → captain name → FO selection.
	var intro_script = load("res://scripts/ui/intro_sequence.gd")
	if intro_script:
		var intro = intro_script.new()
		add_child(intro)
		intro.start()
		var captain_name: String = await intro.intro_complete
		# Send captain name to SimBridge.
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("SetCaptainNameV0"):
			bridge.call("SetCaptainNameV0", captain_name)
	# Start tutorial (FO selection → guided phases) or legacy welcome overlay.
	var _use_tutorial := true
	var settings_mgr = get_node_or_null("/root/SettingsManager")
	if settings_mgr and settings_mgr.has_method("get_setting"):
		_use_tutorial = bool(settings_mgr.call("get_setting", "gameplay_tutorial_toasts"))
	if _use_tutorial:
		_start_tutorial_v0()
	else:
		_show_welcome_overlay_v0()

func _show_welcome_overlay_v0() -> void:
	var canvas := CanvasLayer.new()
	canvas.layer = 110
	canvas.name = "WelcomeOverlay"
	add_child(canvas)

	# Dim background.
	var bg := ColorRect.new()
	bg.color = Color(0.0, 0.0, 0.05, 0.7)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_STOP
	canvas.add_child(bg)

	# Center panel.
	var panel := PanelContainer.new()
	panel.set_anchors_preset(Control.PRESET_CENTER)
	panel.offset_left = -280
	panel.offset_right = 280
	panel.offset_top = -240
	panel.offset_bottom = 240
	var panel_style := StyleBoxFlat.new()
	panel_style.bg_color = Color(0.06, 0.08, 0.14, 0.95)
	panel_style.border_color = Color(0.4, 0.85, 1.0, 0.6)
	panel_style.set_border_width_all(1)
	panel_style.set_corner_radius_all(4)
	panel_style.set_content_margin_all(30)
	panel.add_theme_stylebox_override("panel", panel_style)
	bg.add_child(panel)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 16)
	panel.add_child(vbox)

	# Title.
	var title := Label.new()
	title.text = "SPACE TRADE EMPIRE"
	title.add_theme_font_size_override("font_size", 22)
	title.add_theme_color_override("font_color", Color(0.4, 0.85, 1.0))
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	# Lore intro.
	var lore := Label.new()
	lore.text = "The threads that held the galaxy together are failing.\nFive factions cling to a network they don't understand,\nlocked in a dependency none of them chose.\nTwo wars are already burning. More will follow.\n\nYour ship carries something ancient — a drive that\nshouldn't exist. It lets you go where the threads don't reach.\n\nNobody knows what you have. Not yet."
	lore.add_theme_font_size_override("font_size", 13)
	lore.add_theme_color_override("font_color", Color(0.75, 0.78, 0.88))
	lore.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lore.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(lore)

	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 6)
	vbox.add_child(spacer)

	# Objective hint.
	var hint := Label.new()
	hint.text = "▸ Dock at the station ahead. Learn the markets."
	hint.add_theme_font_size_override("font_size", 13)
	hint.add_theme_color_override("font_color", Color(1.0, 0.85, 0.4))
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(hint)

	var spacer2 := Control.new()
	spacer2.custom_minimum_size = Vector2(0, 4)
	vbox.add_child(spacer2)

	# Controls (compact).
	var controls := Label.new()
	controls.text = "WASD Fly  ·  E Dock  ·  M Map  ·  H Help"
	controls.add_theme_font_size_override("font_size", 12)
	controls.add_theme_color_override("font_color", Color(0.55, 0.6, 0.7))
	controls.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(controls)

	var spacer3 := Control.new()
	spacer3.custom_minimum_size = Vector2(0, 8)
	vbox.add_child(spacer3)

	# Dismiss prompt.
	var dismiss := Label.new()
	dismiss.text = "[ Press any key to begin ]"
	dismiss.add_theme_font_size_override("font_size", 13)
	dismiss.add_theme_color_override("font_color", Color(0.5, 0.7, 0.9, 0.8))
	dismiss.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(dismiss)

	# Pulse the dismiss text.
	var tw := create_tween().set_loops()
	tw.tween_property(dismiss, "modulate:a", 0.4, 1.0).set_ease(Tween.EASE_IN_OUT)
	tw.tween_property(dismiss, "modulate:a", 1.0, 1.0).set_ease(Tween.EASE_IN_OUT)

	# Wait for any input to dismiss.
	await _wait_for_any_input_v0(bg)
	tw.kill()
	var fade := create_tween()
	fade.tween_property(bg, "modulate:a", 0.0, 0.4)
	await fade.finished
	canvas.queue_free()
	# Descent already completed before overlay was shown. Just unlock controls.
	var cam_ctrl = _find_camera_controller()
	if cam_ctrl:
		cam_ctrl.set("input_locked", false)
	if _hero_body:
		_hero_body.freeze = false
	# End intro mode — gameplay resumes (AI, combat, NPC movement, music).
	intro_active = false
	# Captain's Guide: first hint after welcome overlay dismisses.
	_fire_guide_hint_v0("GUIDE_FLIGHT", "The station ahead is your first stop. Fly close and press E to dock.")

var _welcome_dismissed := true  # Set false only while welcome overlay is waiting for input.

func _wait_for_any_input_v0(_control: Control) -> void:
	# Block until any key or mouse press. _unhandled_input sets _welcome_dismissed.
	_welcome_dismissed = false
	var deadline := 60.0  # Long timeout for real players; bots set _welcome_dismissed directly.
	if DisplayServer.get_name() == "headless":
		deadline = 3.0
	while not _welcome_dismissed and deadline > 0.0:
		await get_tree().create_timer(0.2).timeout
		deadline -= 0.2
	_welcome_dismissed = true


# Start the FO-voiced tutorial system (replaces welcome overlay for new games).
func _start_tutorial_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("StartTutorialV0"):
		bridge.call("StartTutorialV0")
	# Create and attach the tutorial director as a child of GameManager.
	var director_script = load("res://scripts/ui/tutorial_director.gd")
	if director_script:
		var director = director_script.new()
		director.name = "TutorialDirector"
		add_child(director)
	print("UUIR|TUTORIAL|STARTED")

# Force-remove welcome overlay on any major state change (dock, galaxy map, warp).
func _dismiss_welcome_overlay_v0() -> void:
	_welcome_dismissed = true
	var overlay = get_node_or_null("WelcomeOverlay")
	if overlay:
		overlay.queue_free()
		# Also ensure camera/ship are unlocked.
		var cam_ctrl = _find_camera_controller()
		if cam_ctrl and cam_ctrl.has_method("tween_altitude_v0"):
			cam_ctrl.call("tween_altitude_v0", 80.0, 1.5)
			cam_ctrl.set("input_locked", false)
		if _hero_body:
			_hero_body.freeze = false

# GATE.S7.MAIN_MENU.AUTO_SAVE.001: Auto-save with cooldown and toast.
func _try_autosave_v0() -> void:
	if _autosave_cooldown > 0.0:
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("AutoSaveV0"):
		return
	bridge.call("AutoSaveV0")
	_autosave_cooldown = AUTOSAVE_COOLDOWN_SEC
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_toast"):
		toast_mgr.call("show_toast", "Auto-saved", 1.5)
	print("UUIR|AUTO_SAVE")

func _process(delta):
	# GATE.S7.MAIN_MENU.SCENE.001: Skip gameplay processing on main menu.
	if _is_main_menu_active():
		return

	# Edgedar: lazy-init after main menu clears (both _ready calls early-return).
	if not get_tree().root.has_node("EdgedarOverlay"):
		var edgedar_script = load("res://scripts/ui/edgedar_overlay.gd")
		if edgedar_script:
			var edgedar = edgedar_script.new()
			edgedar.name = "EdgedarOverlay"
			get_tree().root.add_child(edgedar)
			print("DEBUG_EDGEDAR|CREATED_BY|%s" % name)

	# Trigger onboarding once after scene transition.
	# New game: start intro sequence immediately (don't wait for camera — prevents flash).
	# Continue: wait for camera controller (onboarding toasts need HUD).
	if not _onboarding_shown and get_parent() == get_tree().root:
		if _is_new_game:
			_show_onboarding_toasts_deferred_v0()
		else:
			var cam = _find_camera_controller()
			if cam:
				_show_onboarding_toasts_deferred_v0()

	# Local ticking must continue while overlay is open. This is used only as a boolean check in tests.
	time_accumulator += float(delta)

	# Suppress all gameplay during intro cinematic + welcome overlay.
	if intro_active:
		return
	# Start ambient drone once intro finishes (deferred from _init_sfx_v0).
	if _sfx_ambient_drone and not _sfx_ambient_drone.playing:
		_sfx_ambient_drone.play()

	# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: freeze all game logic when dead
	if _player_dead:
		return

	if _lane_cooldown_v0 > 0.0:
		_lane_cooldown_v0 -= float(delta)
	if _undock_cooldown > 0.0:
		_undock_cooldown -= float(delta)
	if _autosave_cooldown > 0.0:
		_autosave_cooldown -= float(delta)
	if _turret_cooldown > 0.0:
		_turret_cooldown -= float(delta)
	if _ai_fire_cooldown > 0.0:
		_ai_fire_cooldown -= float(delta)
	# FEEL_POST_FIX_3: Decay combat state timer.
	if combat_state_timer > 0.0:
		combat_state_timer -= float(delta)
	# GATE.S1.AUDIO.SFX_CORE.001: engine thrust audio loop
	if _sfx_engine_thrust:
		var _thrust_on := current_player_state == PlayerShipState.IN_FLIGHT and not _player_dead
		if _thrust_on and not _sfx_engine_thrust.playing:
			_sfx_engine_thrust.play()
		elif not _thrust_on and _sfx_engine_thrust.playing:
			_sfx_engine_thrust.stop()
	# GATE.S1.AUDIO.AMBIENT.001: duck ambient during combat
	if _sfx_ambient_drone:
		var _hostiles_near := _find_nearest_fleet_v0(AI_AGGRO_RANGE) != null
		_sfx_ambient_drone.volume_db = -24.0 if _hostiles_near else -18.0
	# AI auto-fire at player when in range; check for death after each shot
	if current_player_state == PlayerShipState.IN_FLIGHT and _ai_fire_cooldown <= 0.0:
		_ai_fire_v0()

	# Primary fire: hold-to-fire turrets at highest rate (not during galaxy map)
	if current_player_state == PlayerShipState.IN_FLIGHT and not galaxy_overlay_open and Input.is_action_pressed("combat_fire_primary"):
		_fire_turret_v0()

	# Shield regen: 5 HP/sec for player fleet (SimBridge handles clamping to max)
	var bridge_regen = get_node_or_null("/root/SimBridge")
	if bridge_regen and bridge_regen.has_method("TickShieldRegenV0"):
		bridge_regen.call("TickShieldRegenV0", PLAYER_FLEET_ID, float(delta))

	# Camera follows transit marker during lane travel.
	if current_player_state == PlayerShipState.IN_LANE_TRANSIT and _transit_marker and is_instance_valid(_transit_marker):
		warp_transit_target = _transit_marker.global_position

	# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: poll player HP each frame for death detection
	_check_player_death_v0()

	# GATE.S8.WIN.GAME_OVER_WIRE.001: poll SimBridge for terminal game result
	_game_over_poll_timer += float(delta)
	if _game_over_poll_timer >= GAME_OVER_POLL_INTERVAL:
		_game_over_poll_timer = 0.0
		_poll_game_over_v0()

	# GATE.S11.GAME_FEEL.TOAST_EVENTS.001: poll research + events for toast notifications
	_toast_poll_timer += float(delta)
	if _toast_poll_timer >= TOAST_POLL_INTERVAL:
		_toast_poll_timer = 0.0
		_poll_toast_events_v0()

	# GATE.S6.OUTCOME.CELEBRATION.001: poll discovery completions for celebration
	_discovery_poll_timer += float(delta)
	if _discovery_poll_timer >= DISCOVERY_POLL_INTERVAL:
		_discovery_poll_timer = 0.0
		_poll_discovery_celebrations_v0()
		# GATE.S19.ONBOARD.MILESTONE_TOAST.014: piggyback on discovery poll interval.
		_poll_milestone_celebrations_v0()

	# Captain's Guide: poll contextual hints every 2s.
	_guide_poll_timer += float(delta)
	if _guide_poll_timer >= GUIDE_POLL_INTERVAL:
		_guide_poll_timer = 0.0
		_poll_guide_hints_v0()

	# GATE.S5.TRACTOR.VFX.001: Auto-collect nearby loot and show tractor beam VFX.
	if current_player_state == PlayerShipState.IN_FLIGHT:
		_loot_poll_timer += float(delta)
		if _loot_poll_timer >= LOOT_POLL_INTERVAL:
			_loot_poll_timer = 0.0
			_poll_auto_loot_v0()


func _unhandled_input(event):
	# GATE.S7.MAIN_MENU.SCENE.001: Skip gameplay input on main menu.
	if _is_main_menu_active():
		return
	# Only the autoload instance handles input (avoid dual GameManager double-fire).
	if get_parent() != get_tree().root:
		return
	# Welcome overlay dismiss — any key or click.
	if not _welcome_dismissed:
		if (event is InputEventKey and event.pressed) or (event is InputEventMouseButton and event.pressed):
			_welcome_dismissed = true
			get_viewport().set_input_as_handled()
		return
	# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: R restarts scene; all other input frozen when dead
	if _player_dead:
		if event.is_action_pressed("combat_target_nearest"):
			get_tree().reload_current_scene()
		return
	# Block input during approach cinematic (vortex + zoom-in playing).
	if _cinematic_transit_active:
		return
	# Gate approach: confirm or cancel.
	if current_player_state == PlayerShipState.GATE_APPROACH:
		if event.is_action_pressed("ui_gate_confirm"):
			_confirm_gate_transit_v0()
			get_viewport().set_input_as_handled()
			return
		if event.is_action_pressed("ui_gate_cancel"):
			_cancel_gate_approach_v0()
			get_viewport().set_input_as_handled()
			return
	if event.is_action_pressed("ui_pause"):
		_toggle_pause_v0()
		return
	if event.is_action_pressed("ui_galaxy_map") and current_player_state != PlayerShipState.IN_LANE_TRANSIT and current_player_state != PlayerShipState.DOCKED:
		toggle_galaxy_map_overlay_v0()
	if event.is_action_pressed("ui_dock_confirm"):
		# Dock confirmation: E near a station docks (priority); else E near a gate triggers gate.
		if _dock_available_target != null and current_player_state == PlayerShipState.IN_FLIGHT:
			on_proximity_dock_entered_v0(_dock_available_target)
			_dock_available_target = null
			var hud_dk = get_tree().root.find_child("HUD", true, false) if get_tree() else null
			if hud_dk and hud_dk.has_method("hide_dock_prompt_v0"):
				hud_dk.call("hide_dock_prompt_v0")
		elif not _gate_available_target.is_empty() and current_player_state == PlayerShipState.IN_FLIGHT:
			# E key triggers gate: transition to GATE_APPROACH, stop ship, show popup, pause.
			_trigger_gate_from_prompt_v0()
	if event.is_action_pressed("ui_empire_dashboard"):
		# Empire dashboard: only when dock is NOT available (E shared between dock + empire)
		if _dock_available_target == null or current_player_state != PlayerShipState.IN_FLIGHT:
			_toggle_empire_dashboard_v0()
	if event.is_action_pressed("ui_keybinds_help"):
		_toggle_keybinds_help_v0()
	if event.is_action_pressed("ui_knowledge_web"):
		_toggle_knowledge_web_v0()
	if event.is_action_pressed("ui_mission_journal"):
		_toggle_mission_journal_v0()
	if event.is_action_pressed("ui_combat_log"):
		_toggle_combat_log_v0()
	if event.is_action_pressed("ui_warfront_dashboard"):
		_toggle_warfront_dashboard_v0()
	if event.is_action_pressed("ui_megaproject"):
		_toggle_megaproject_panel_v0()
	if event.is_action_pressed("ui_fo_panel"):
		_toggle_fo_panel_v0()
	if event.is_action_pressed("ui_automation"):
		_toggle_automation_dashboard_v0()
	if event.is_action_pressed("ui_data_log"):
		_toggle_data_log_v0()
	if event.is_action_pressed("ui_data_overlay"):
		_cycle_data_overlay_v0()

# GATE.S10.EMPIRE.SHELL.001: Toggle empire dashboard panel (created by SimBridge in C#).
func _toggle_empire_dashboard_v0():
	if _empire_dashboard == null:
		_empire_dashboard = get_tree().root.find_child("EmpireDashboard", true, false)
	if _empire_dashboard != null:
		_empire_dashboard.visible = not _empire_dashboard.visible
		# FEEL_POST_FIX_5: Track open state so camera doesn't override HUD suppression.
		empire_dashboard_open = _empire_dashboard.visible
		# Hide HUD status elements when dashboard is open.
		var hud_ed = get_tree().root.find_child("HUD", true, false)
		if hud_ed and hud_ed.has_method("set_overlay_mode_v0"):
			hud_ed.call("set_overlay_mode_v0", _empire_dashboard.visible)
		# GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001: Hide/show Label3D nodes when
		# empire dashboard covers viewport (same 3D bleed-through issue as dock menu).
		# GATE.S7.RUNTIME_STABILITY.GALAXY_MAP_FIX.001: When the galaxy overlay is open,
		# keep labels visible so the map remains visible behind the dashboard's dimmer.
		_find_galaxy_view()
		if _galaxy_view and _galaxy_view.has_method("SetLocalLabelsVisibleV0"):
			if galaxy_overlay_open:
				# Galaxy map is active — always keep labels visible so map shows
				# behind the semi-transparent empire dashboard.
				_galaxy_view.call("SetLocalLabelsVisibleV0", true)
			else:
				_galaxy_view.call("SetLocalLabelsVisibleV0", not _empire_dashboard.visible)

func toggle_market():
	# No-op stub. Station UI is driven by C# StationMenu via SimBridge.
	return


func toggle_galaxy_map_overlay_v0():
	_dismiss_welcome_overlay_v0()
	# Seamless zoom: TAB tells the camera to tween altitude up/down.
	# The camera's _sync_overlay_state() handles galaxy_overlay_open, ship visibility, HUD, etc.
	var cam_controller = _find_camera_controller()
	if cam_controller and cam_controller.has_method("toggle_strategic_altitude_v0"):
		cam_controller.call("toggle_strategic_altitude_v0")

var _cached_cam_controller = null
func _find_camera_controller():
	if _cached_cam_controller and is_instance_valid(_cached_cam_controller):
		return _cached_cam_controller
	_cached_cam_controller = get_tree().root.find_child("Camera3D", true, false)
	if _cached_cam_controller == null:
		# Try finding by script — camera is on a Node3D named "Camera3D" in Player scene.
		for node in get_tree().get_nodes_in_group("Player"):
			var mount = node.get_node_or_null("CameraMount/Camera3D")
			if mount and mount.has_method("toggle_strategic_altitude_v0"):
				_cached_cam_controller = mount
				break
	return _cached_cam_controller

## V key: cycle data overlay mode (None → Security → Trade Flow → Intel Age → None).
func _cycle_data_overlay_v0() -> void:
	_find_galaxy_view()
	if _galaxy_view == null or not _galaxy_view.has_method("GetOverlayModeV0"):
		return
	var current: int = _galaxy_view.call("GetOverlayModeV0")
	# Cycle: -1 → 0 → 1 → 2 → -1
	var next: int
	match current:
		-1: next = 0
		0:  next = 1
		1:  next = 2
		2:  next = -1
		_:  next = -1
	_galaxy_view.call("SetOverlayModeV0", next)
	# Show toast feedback.
	var names: Dictionary = {-1: "Off", 0: "Security", 1: "Trade Flow", 2: "Intel Age"}
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_toast"):
		toast_mgr.call("show_toast", "Overlay: " + names.get(next, "Off"), 1.5)
	# Update HUD overlay label.
	var hud_v = get_tree().root.find_child("HUD", true, false)
	if hud_v and hud_v.has_method("set_data_overlay_label_v0"):
		hud_v.call("set_data_overlay_label_v0", next)
	# Sync galaxy_overlay_hud button states.
	var overlay_hud = get_tree().root.find_child("GalaxyOverlayHUD", true, false)
	if overlay_hud and overlay_hud.has_method("_update_button_styles"):
		overlay_hud.call("_update_button_styles", next)

# Valid transitions: IN_FLIGHT<->DOCKED, IN_FLIGHT<->GATE_APPROACH,
# GATE_APPROACH<->IN_LANE_TRANSIT, GATE_APPROACH->IN_FLIGHT, IN_FLIGHT<->IN_LANE_TRANSIT (headless).
# Emits INVALID_STATE_TRANSITION token and returns false on bad transition.
func _transition_player_state_v0(new_state: PlayerShipState) -> bool:
	var valid := false
	match current_player_state:
		PlayerShipState.IN_FLIGHT:
			valid = (new_state == PlayerShipState.DOCKED
				or new_state == PlayerShipState.IN_LANE_TRANSIT
				or new_state == PlayerShipState.GATE_APPROACH)
		PlayerShipState.DOCKED:
			valid = new_state == PlayerShipState.IN_FLIGHT
		PlayerShipState.IN_LANE_TRANSIT:
			valid = new_state == PlayerShipState.IN_FLIGHT
		PlayerShipState.GATE_APPROACH:
			valid = (new_state == PlayerShipState.IN_LANE_TRANSIT
				or new_state == PlayerShipState.IN_FLIGHT)
	if not valid:
		print("INVALID_STATE_TRANSITION|" + get_player_ship_state_name_v0() + "|" + _state_name_v0(new_state))
		return false
	current_player_state = new_state
	return true

func get_player_ship_state_name_v0() -> String:
	return _state_name_v0(current_player_state)

func _state_name_v0(s: PlayerShipState) -> String:
	match s:
		PlayerShipState.IN_FLIGHT:       return "IN_FLIGHT"
		PlayerShipState.DOCKED:          return "DOCKED"
		PlayerShipState.IN_LANE_TRANSIT: return "IN_LANE_TRANSIT"
		PlayerShipState.GATE_APPROACH:   return "GATE_APPROACH"
	return "UNKNOWN"

# Dock confirmation: called by collision triggers to show "Press E to dock" prompt.
func on_dock_proximity_v0(target: Node):
	if target == null:
		return
	if current_player_state != PlayerShipState.IN_FLIGHT:
		return
	if _undock_cooldown > 0.0:
		return
	if _hero_body and is_instance_valid(_hero_body) and _hero_body.get("_nav_active"):
		return
	# Q5: Only block docking if the nearest fleet is hostile (Patrol).
	# Previously ALL fleets (including peaceful traders) blocked docking.
	var nearest_fleet := _find_nearest_fleet_v0(AI_AGGRO_RANGE)
	if nearest_fleet != null and nearest_fleet.get_meta("is_hostile", false):
		return
	_dock_available_target = target
	var hud = get_tree().root.find_child("HUD", true, false) if get_tree() else null
	if hud and hud.has_method("show_dock_prompt_v0"):
		hud.call("show_dock_prompt_v0")

# Dock confirmation: called when player leaves dock trigger area.
func on_dock_proximity_exit_v0(target: Node):
	if _dock_available_target == target:
		_dock_available_target = null
		var hud = get_tree().root.find_child("HUD", true, false) if get_tree() else null
		if hud and hud.has_method("hide_dock_prompt_v0"):
			hud.call("hide_dock_prompt_v0")

# Commit dock: called by E key press or directly by test scripts.
func on_proximity_dock_entered_v0(target: Node):
	_dismiss_welcome_overlay_v0()
	if target == null:
		return

	if not _transition_player_state_v0(PlayerShipState.DOCKED):
		return
	dock_target_kind_token = _dock_target_kind_token_v0(target)
	dock_target_id = _dock_target_id_v0(target)

	# Deterministic token for headless assertions (no timestamps).
	print("UUIR|DOCK_ENTER|" + dock_target_kind_token + "|" + dock_target_id)
	# GATE.S1.AUDIO.AMBIENT.001: dock chime
	if _sfx_dock_chime:
		_sfx_dock_chime.play()

	# GATE.S8.HAVEN.COMING_HOME.001: Haven dock cinematic (first dock per session).
	_try_haven_cinematic_v0()

	# FEEL_POST_FIX_10: Kill any lingering flyby letterbox on dock so text doesn't persist.
	# CanvasLayer inherits Node (no `visible`). Remove children to hide, then free.
	_kill_flyby_letterbox_v0()

	# GATE.X.WARP.ARRIVAL_DRAMA.001: Kill arrival drama on dock.
	var hud_dock = get_tree().root.find_child("HUD", true, false)
	if hud_dock and hud_dock.has_method("hide_arrival_drama_v0"):
		hud_dock.call("hide_arrival_drama_v0")

	if dock_target_kind_token == "STATION":
		_open_station_menu_v0(target)
		var htm = _find_hero_trade_menu()
		if htm and htm.has_method("open_market_v0"):
			htm.call("open_market_v0", dock_target_id)
	elif dock_target_kind_token == "PLANET":
		# GATE.S7.PLANET.DOCK_VISUAL.001: Planet docking — same menu, different title.
		_open_station_menu_v0(target)
		var htm2 = _find_hero_trade_menu()
		if htm2 and htm2.has_method("open_market_v0"):
			htm2.call("open_market_v0", dock_target_id)
	# GATE.S14.STARTER.MISSION_PROMPT.001: first-dock jobs toast (now no-op, replaced by disclosure)
	_check_first_dock_mission_prompt_v0()
	# Notify tutorial system of dock event (advances Dock_Prompt → Docked_First).
	var _tut_bridge = get_node_or_null("/root/SimBridge")
	if _tut_bridge and _tut_bridge.has_method("NotifyTutorialDockV0"):
		_tut_bridge.call("NotifyTutorialDockV0")
	# Captain's Guide: dock hint (legacy — suppressed during tutorial by _poll guard).
	_fire_guide_hint_v0("GUIDE_DOCK", "Look for the BEST BUY tag in the Market. Buy it, then fly to the next station and sell where the price is higher.")
	# GATE.S19.ONBOARD.FO_EVENTS.012: Immediate FO poll after docking.
	_poll_fo_immediate()
	# Tutorial nudge: if stuck on an action phase, FO reminds the player what to do.
	var _tut_dir = get_node_or_null("TutorialDirector")
	if _tut_dir and _tut_dir.has_method("on_dock_nudge"):
		_tut_dir.call("on_dock_nudge")

	# GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Close galaxy overlay if open when docking.
	# Prevents large 3D elements (beacons, labels) from rendering at dock-distance camera.
	if galaxy_overlay_open:
		toggle_galaxy_map_overlay_v0()

	# GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001: Lazy-find GalaxyView (autoload parent=/root/).
	_find_galaxy_view()

	# Hide galaxy overlay labels so they don't bleed through dock menu panel.
	if _galaxy_view and _galaxy_view.has_method("SetLocalLabelsVisibleV0"):
		_galaxy_view.call("SetLocalLabelsVisibleV0", false)

	# GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Notify GalaxyView that a 2D UI panel
	# is covering the screen, so it suppresses all 3D overlay rendering.
	if _galaxy_view and _galaxy_view.has_method("SetUiPanelActiveV0"):
		_galaxy_view.call("SetUiPanelActiveV0", true)

	# GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Hide galaxy_spawner (old GDScript overlay)
	# so its Label3D nodes don't bleed through 2D dock panel.
	var galaxy_spawner = get_tree().root.find_child("GalaxySpawner", true, false)
	if galaxy_spawner:
		galaxy_spawner.visible = false

	if dock_target_kind_token == "DISCOVERY_SITE":
		# Scan flow wiring can be added later without changing the state machine surface.
		print("UUIR|SCAN_FLOW_OPEN|" + dock_target_id)

	# GATE.S7.MAIN_MENU.AUTO_SAVE.001: Auto-save on dock.
	_try_autosave_v0()

func undock_v0():
	if current_player_state != PlayerShipState.DOCKED:
		return

	# Deterministic token for headless assertions (no timestamps).
	print("UUIR|UNDOCK|" + dock_target_kind_token + "|" + dock_target_id)

	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	dock_target_kind_token = ""
	dock_target_id = ""
	_undock_cooldown = 1.5  # Prevent re-dock prompt immediately after undock.

	# Dismiss guide hints so they don't obscure flight view.
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("dismiss_guide_toasts"):
		toast_mgr.call("dismiss_guide_toasts")
	_dock_available_target = null

	# Close station menu if it is open.
	if _station_menu and _station_menu.has_method("OnShopToggled"):
		_station_menu.call("OnShopToggled", false, "")
	var htm = _find_hero_trade_menu()
	if htm and htm.has_method("close_market_v0"):
		htm.call("close_market_v0")
	# Close discovery panel if it is open.
	if _discovery_panel and _discovery_panel.has_method("close_v0"):
		_discovery_panel.call("close_v0")
	# GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001: Lazy-find GalaxyView on undock path too.
	_find_galaxy_view()

	# Restore galaxy overlay labels on undock.
	if _galaxy_view and _galaxy_view.has_method("SetLocalLabelsVisibleV0"):
		_galaxy_view.call("SetLocalLabelsVisibleV0", true)

	# GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Restore GalaxyView rendering on undock.
	if _galaxy_view and _galaxy_view.has_method("SetUiPanelActiveV0"):
		_galaxy_view.call("SetUiPanelActiveV0", false)

	# GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Restore galaxy_spawner visibility on undock.
	var galaxy_spawner = get_tree().root.find_child("GalaxySpawner", true, false)
	if galaxy_spawner:
		galaxy_spawner.visible = true

## GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: Close all dock UI panels defensively.
## Called before warp transit begins so no station/trade/discovery panels remain visible.
func _close_all_dock_ui_v0() -> void:
	if _station_menu and _station_menu.has_method("OnShopToggled"):
		_station_menu.call("OnShopToggled", false, "")
	var htm = _find_hero_trade_menu()
	if htm and htm.has_method("close_market_v0"):
		htm.call("close_market_v0")
	if _discovery_panel and _discovery_panel.has_method("close_v0"):
		_discovery_panel.call("close_v0")
	# GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001: Lazy-find before restoring.
	_find_galaxy_view()
	# Restore GalaxyView rendering in case a panel was covering it.
	if _galaxy_view and _galaxy_view.has_method("SetUiPanelActiveV0"):
		_galaxy_view.call("SetUiPanelActiveV0", false)

func on_lane_gate_proximity_entered_v0(neighbor_node_id: String) -> void:
	_dismiss_welcome_overlay_v0()
	if _lane_cooldown_v0 > 0.0:
		return
	# Guard: ignore lane proximity while docked (gate collision can fire during dock entry).
	if current_player_state == PlayerShipState.DOCKED:
		return
	if not _transition_player_state_v0(PlayerShipState.IN_LANE_TRANSIT):
		return

	# GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: Close dock UI before warp.
	_close_all_dock_ui_v0()

	print("UUIR|LANE_ENTER|" + neighbor_node_id)
	# GATE.S13.WORLD.GATE_ARRIVAL.001: Remember origin for arrival positioning.
	# Eager read: _last_player_node_id may not yet be set if toast poll hasn't fired.
	if _last_player_node_id.is_empty():
		var bridge_snap = get_node_or_null("/root/SimBridge")
		if bridge_snap and bridge_snap.has_method("GetPlayerSnapshot"):
			var ps: Dictionary = bridge_snap.call("GetPlayerSnapshot")
			_last_player_node_id = str(ps.get("location", ""))
	_lane_origin_node_id = _last_player_node_id

	# Save camera pre-transit altitude (gate-approach path saves this in _confirm_gate_transit_v0;
	# proximity path must do it here so post-transit tween restores the correct flight altitude).
	var cam_ctrl_prox = _find_camera_controller()
	if cam_ctrl_prox:
		cam_ctrl_prox.set("_pre_transit_altitude", cam_ctrl_prox.get("_altitude"))
		cam_ctrl_prox.set("_pre_transit_altitude_set", true)

	# GATE.S14.TRANSIT.WARP_EFFECT.001: screen flash + camera shake + FOV punch on warp entry
	_apply_camera_shake_v0(0.4)
	_flash_warp_screen_v0()
	var cam_fov = _find_camera_controller()
	if cam_fov and cam_fov.has_method("punch_fov_v0"):
		cam_fov.call("punch_fov_v0", 8.0, 0.5)
	# GATE.X.WARP.DEPARTURE_VFX.001: 3D departure flash at ship position.
	_ensure_hero_body()
	if _hero_body and is_instance_valid(_hero_body):
		WarpEffect.play_departure_flash(get_tree().root, _hero_body.global_position)

	# GATE.S1.AUDIO.AMBIENT.001: warp whoosh on lane jump
	if _sfx_warp_whoosh:
		_sfx_warp_whoosh.play()

	# Cache first-visit status BEFORE travel command marks the destination visited.
	var bridge = get_node_or_null("/root/SimBridge")
	_lane_dest_is_first_visit = false
	if bridge and bridge.has_method("IsFirstVisitV0"):
		_lane_dest_is_first_visit = bridge.call("IsFirstVisitV0", neighbor_node_id)

	if bridge and bridge.has_method("DispatchTravelCommandV0"):
		bridge.call("DispatchTravelCommandV0", PLAYER_FLEET_ID, neighbor_node_id)

	# Auto-complete lane transit after sim processes the travel command.
	_begin_lane_transit_v0(neighbor_node_id)

# Lazy hero body finder — autoload GameManager doesn't get _hero_body in _ready
# because Player is under /root/Main, not /root.
func _ensure_hero_body() -> void:
	if _hero_body and is_instance_valid(_hero_body):
		return
	_hero_body = get_tree().root.find_child("Player", true, false) as RigidBody3D

# ── Gate approach flow (E-key prompt, no auto-pause) ──
# Called by GalaxyView on the autoload GameManager (owns _unhandled_input).
func on_lane_gate_approach_entered_v0(neighbor_node_id: String) -> void:
	if _lane_cooldown_v0 > 0.0:
		return
	if _gate_approach_declined:
		return
	# Don't trigger a new approach while cinematic departure is in progress.
	if _cinematic_transit_active:
		return
	# Must be in flight to show gate prompt.
	if current_player_state != PlayerShipState.IN_FLIGHT:
		return
	_gate_available_target = neighbor_node_id
	print("UUIR|GATE_PROMPT|" + neighbor_node_id)

	# Resolve destination name for the HUD prompt.
	var dest_name: String = neighbor_node_id
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetTransitCostV0"):
		var info: Dictionary = bridge.call("GetTransitCostV0", PLAYER_FLEET_ID, neighbor_node_id)
		dest_name = str(info.get("destination_name", neighbor_node_id))
	var hud_gate = get_tree().root.find_child("HUD", true, false) if get_tree() else null
	if hud_gate and hud_gate.has_method("show_gate_prompt_v0"):
		hud_gate.call("show_gate_prompt_v0", dest_name)

# Called by GalaxyView when player exits approach zone.
func on_lane_gate_approach_exited_v0() -> void:
	_gate_approach_declined = false
	# If player already confirmed (cinematic active or GATE_APPROACH state), don't cancel.
	if _cinematic_transit_active:
		return
	if current_player_state == PlayerShipState.GATE_APPROACH:
		_cancel_gate_approach_v0()
		return
	# Clear E-key prompt if we were just showing it.
	_gate_available_target = ""
	var hud_gate = get_tree().root.find_child("HUD", true, false) if get_tree() else null
	if hud_gate and hud_gate.has_method("hide_gate_prompt_v0"):
		hud_gate.call("hide_gate_prompt_v0")

# E-key gate trigger: skip popup, go straight to transit.
func _trigger_gate_from_prompt_v0() -> void:
	var neighbor_node_id := _gate_available_target
	_gate_available_target = ""
	# Hide the E-key prompt.
	var hud_gate = get_tree().root.find_child("HUD", true, false) if get_tree() else null
	if hud_gate and hud_gate.has_method("hide_gate_prompt_v0"):
		hud_gate.call("hide_gate_prompt_v0")
	# Transition to GATE_APPROACH state (needed for cinematic transit).
	if not _transition_player_state_v0(PlayerShipState.GATE_APPROACH):
		return
	_approach_neighbor_id = neighbor_node_id
	print("UUIR|GATE_APPROACH|" + neighbor_node_id)
	_ensure_hero_body()
	if _hero_body and is_instance_valid(_hero_body):
		_hero_body.linear_velocity = Vector3.ZERO
		_hero_body.angular_velocity = Vector3.ZERO
	# Pre-build galaxy overlay for transit animation.
	var gv = _find_galaxy_view()
	if gv and gv.has_method("PrewarmOverlayV0"):
		gv.call("PrewarmOverlayV0")
	# Skip popup — go directly to transit confirmation.
	_confirm_gate_transit_v0()

func _cancel_gate_approach_v0() -> void:
	print("UUIR|GATE_APPROACH_CANCEL")
	get_tree().paused = false
	_gate_approach_declined = true
	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	_approach_neighbor_id = ""
	if _gate_popup and is_instance_valid(_gate_popup):
		_gate_popup.queue_free()
		_gate_popup = null

func _confirm_gate_transit_v0() -> void:
	if current_player_state != PlayerShipState.GATE_APPROACH:
		return
	var neighbor_id := _approach_neighbor_id
	if neighbor_id.is_empty():
		return

	# Block confirm if player cannot afford transit.
	if _gate_popup and is_instance_valid(_gate_popup) and _gate_popup.has_method("can_confirm"):
		if not _gate_popup.call("can_confirm"):
			return

	# Close popup and unpause (game was frozen during popup).
	if _gate_popup and is_instance_valid(_gate_popup):
		_gate_popup.queue_free()
		_gate_popup = null
	get_tree().paused = false

	# GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: Close dock UI before warp.
	_close_all_dock_ui_v0()

	# Save origin + dest BEFORE clearing approach_neighbor_id.
	# Eager read: _last_player_node_id may not yet be set if toast poll hasn't fired.
	if _last_player_node_id.is_empty():
		var bridge_snap = get_node_or_null("/root/SimBridge")
		if bridge_snap and bridge_snap.has_method("GetPlayerSnapshot"):
			var ps: Dictionary = bridge_snap.call("GetPlayerSnapshot")
			_last_player_node_id = str(ps.get("location", ""))
	_lane_origin_node_id = _last_player_node_id
	_lane_dest_node_id = neighbor_id
	print("UUIR|LANE_ENTER|" + neighbor_id)

	# Cache first-visit status BEFORE travel command marks the destination visited.
	var bridge = get_node_or_null("/root/SimBridge")
	_lane_dest_is_first_visit = false
	if bridge and bridge.has_method("IsFirstVisitV0"):
		_lane_dest_is_first_visit = bridge.call("IsFirstVisitV0", neighbor_id)

	# Dispatch travel command (deducts fuel + toll in SimCore).
	if bridge and bridge.has_method("DispatchTravelCommandV0"):
		bridge.call("DispatchTravelCommandV0", PLAYER_FLEET_ID, neighbor_id)

	# === Cinematic departure: ship→gate→bubble→warp ===
	_cinematic_transit_active = true
	var cam_ctrl = _find_camera_controller()
	if cam_ctrl:
		cam_ctrl.set("_pre_transit_altitude", cam_ctrl.get("_altitude"))
		cam_ctrl.set("_pre_transit_altitude_set", true)
		cam_ctrl.input_locked = true

	# Resolve gate position from the ACTUAL scene node (single source of truth).
	var gv = _find_galaxy_view()
	var gate_pos: Vector3 = _hero_body.global_position if _hero_body and is_instance_valid(_hero_body) else Vector3.ZERO
	# Primary: find the LaneGate scene node directly.
	var gate_nodes := get_tree().get_nodes_in_group("LaneGate") if get_tree() else []
	for gn in gate_nodes:
		if gn is Node3D and gn.has_meta("neighbor_node_id") and str(gn.get_meta("neighbor_node_id")) == _approach_neighbor_id:
			gate_pos = gn.global_position
			break
	# Fallback: cache (if no scene node found).
	if gate_pos == (_hero_body.global_position if _hero_body and is_instance_valid(_hero_body) else Vector3.ZERO):
		if gv and gv.has_method("GetGatePositionV0") and not _approach_neighbor_id.is_empty():
			var gp: Vector3 = gv.call("GetGatePositionV0", _approach_neighbor_id)
			if gp != Vector3.ZERO:
				gate_pos = gp
	var star_center: Vector3 = Vector3.ZERO
	if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
		star_center = gv.call("GetCurrentStarGlobalPositionV0")
	var gate_dir: Vector3 = (gate_pos - star_center)
	gate_dir.y = 0.0
	gate_dir = gate_dir.normalized() if gate_dir.length() > 0.1 else Vector3(1, 0, 0)

	# === Departure cinematic with C2-continuous camera ===
	# Key principle: initialize flyby from CURRENT camera position, then TWEEN to
	# the cinematic view. k=30 lerp tracks a smooth tween = C2 at the join.
	_ensure_hero_body()
	# Ship lines up just outside the gate ring (inner radius 6u).
	var gate_ring_edge: Vector3 = gate_pos - gate_dir * 8.0
	gate_ring_edge.y = 0.0

	# Cinematic camera target: behind + above ship, looking through gate toward destination.
	var cam_behind: Vector3 = gate_ring_edge - gate_dir * 25.0
	cam_behind.y = 18.0
	var cam_look_at: Vector3 = gate_pos + gate_dir * 50.0  # Look PAST the gate toward destination.
	cam_look_at.y = 0.0

	# Phase 1: Activate flyby FROM CURRENT spring state (C2 join — velocity preserved).
	# Then simultaneously tween camera to cinematic view AND pull ship to gate ring.
	if cam_ctrl:
		cam_ctrl.flyby_cam_pos = cam_ctrl._spring_pos   # Start where spring IS.
		cam_ctrl.flyby_look_at = cam_ctrl._spring_look   # Preserve current look target.
		cam_ctrl.flyby_up = cam_ctrl._spring_up           # Preserve current up vector.
		cam_ctrl.flyby_active = true  # Spring target = current pos → zero movement.

	if _hero_body and is_instance_valid(_hero_body):
		# Phase 0: Smooth turn toward gate (0.5s quaternion slerp).
		# AAA pattern: ship locks onto target with deliberate rotation before accelerating.
		var start_quat: Quaternion = _hero_body.global_transform.basis.get_rotation_quaternion()
		var target_basis: Basis = Basis.looking_at(gate_dir, Vector3.UP)
		var target_quat: Quaternion = target_basis.get_rotation_quaternion()
		var turn_steps := 15
		var turn_step_time := 0.5 / float(turn_steps)
		for step in range(turn_steps):
			if not (_hero_body and is_instance_valid(_hero_body)):
				break
			var t: float = float(step + 1) / float(turn_steps)
			var eased_t: float = t * t * (3.0 - 2.0 * t)  # smoothstep
			_hero_body.global_transform.basis = Basis(start_quat.slerp(target_quat, eased_t))
			await get_tree().create_timer(turn_step_time).timeout

		# Phase 1: Ship pull + camera swoop in parallel (0.8s cubic ease).
		var pull_tween := create_tween()
		pull_tween.tween_property(_hero_body, "global_position", gate_ring_edge, 0.8) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
		if cam_ctrl:
			var cam_tween := create_tween()
			cam_tween.tween_property(cam_ctrl, "flyby_cam_pos", cam_behind, 0.8) \
				.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
			cam_tween.parallel().tween_property(cam_ctrl, "flyby_look_at", cam_look_at, 0.8) \
				.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
			cam_tween.parallel().tween_property(cam_ctrl, "flyby_up", Vector3.UP, 0.8) \
				.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
		await pull_tween.finished

	# Phase 2: Vortex charge-up (0.8s) — ship drifts forward into the ring while trembling.
	if _sfx_warp_whoosh:
		_sfx_warp_whoosh.play()
	var vortex := GateVortex.new()
	get_tree().root.add_child(vortex)
	vortex.global_position = gate_pos
	vortex.setup()

	# Phase 2: Vortex charge-up (0.8s) — smooth drift into ring.
	# AAA principle: VFX + camera shake carry the drama, ship stays on a clean trajectory.
	var charge_time := 0.8
	if _hero_body and is_instance_valid(_hero_body):
		var drift_target: Vector3 = _hero_body.global_position.lerp(gate_pos, 0.6)
		drift_target.y = 0.0
		var drift_tween := create_tween()
		drift_tween.tween_property(_hero_body, "global_position", drift_target, charge_time) \
			.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)
		# Escalating camera shake provides the "energy building" feel.
		var shake_steps := 8
		for i in range(shake_steps):
			var t: float = float(i + 1) / float(shake_steps)
			_apply_camera_shake_v0(0.05 + t * 0.4)
			await get_tree().create_timer(charge_time / float(shake_steps)).timeout
		if drift_tween.is_running():
			await drift_tween.finished
	else:
		await get_tree().create_timer(charge_time).timeout

	# Phase 3: Ship pulled THROUGH the gate ring (0.4s) — exits the other side.
	var gate_exit: Vector3 = gate_pos + gate_dir * 8.0
	gate_exit.y = 0.0
	if _hero_body and is_instance_valid(_hero_body):
		var enter_tween := create_tween()
		enter_tween.tween_property(_hero_body, "global_position", gate_exit, 0.4) \
			.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
		if cam_ctrl and cam_ctrl.has_method("punch_fov_v0"):
			cam_ctrl.call("punch_fov_v0", 10.0, 0.4)
		await enter_tween.finished

	# Phase 4: Flash + big shake at moment of entry.
	_flash_warp_screen_v0()
	_apply_camera_shake_v0(0.8)
	WarpEffect.play_departure_flash(get_tree().root, gate_pos)

	if vortex and is_instance_valid(vortex) and vortex.has_method("despawn"):
		vortex.call("despawn", 0.3)

	_approach_neighbor_id = ""

	# Phase 5: Disable flyby — the universal spring automatically carries the
	# camera from the cinematic position to wherever WARP_TRANSIT targets it.
	# No manual zoom-up needed; C2 continuity is guaranteed by the spring.
	if cam_ctrl:
		cam_ctrl.flyby_active = false

	if not _transition_player_state_v0(PlayerShipState.IN_LANE_TRANSIT):
		_cinematic_transit_active = false
		if cam_ctrl:
			cam_ctrl.input_locked = false
		return

	_cinematic_transit_active = false
	if cam_ctrl:
		cam_ctrl.input_locked = false

	_begin_lane_transit_v0(neighbor_id)

func _show_gate_popup_v0(neighbor_node_id: String) -> void:
	if _gate_popup and is_instance_valid(_gate_popup):
		_gate_popup.queue_free()
	_gate_popup = GateTransitPopup.new()
	add_child(_gate_popup)
	var bridge = get_node_or_null("/root/SimBridge")
	if _gate_popup.has_method("show_transit_v0"):
		_gate_popup.call("show_transit_v0", bridge, PLAYER_FLEET_ID, neighbor_node_id)

func _play_departure_vortex_v0() -> void:
	_ensure_hero_body()
	if _hero_body == null or not is_instance_valid(_hero_body):
		await get_tree().create_timer(0.3).timeout
		return

	# Find the gate marker position for the approach target.
	var gate_pos: Vector3 = _hero_body.global_position
	var gv = _find_galaxy_view()
	if gv and gv.has_method("GetGatePositionV0") and not _approach_neighbor_id.is_empty():
		var gp: Vector3 = gv.call("GetGatePositionV0", _approach_neighbor_id)
		if gp != Vector3.ZERO:
			gate_pos = gp
	elif gv and gv.has_method("GetGatePositionV0") and not _lane_origin_node_id.is_empty():
		# Fallback: use last approach neighbor stored in _lane_origin_node_id context.
		pass

	# Spawn vortex at gate position.
	var vortex := GateVortex.new()
	get_tree().root.add_child(vortex)
	vortex.global_position = gate_pos
	vortex.setup()

	# Pull ship toward vortex center over charge time.
	var charge_tween := create_tween()
	# Pace overhaul: faster vortex pull (was 1.5s).
	charge_tween.tween_property(_hero_body, "global_position", gate_pos, 0.8) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	await charge_tween.finished

	# Despawn vortex.
	if vortex and is_instance_valid(vortex) and vortex.has_method("despawn"):
		vortex.call("despawn", 0.3)

func on_discovery_site_proximity_entered_v0(site_id: String) -> void:
	if not _transition_player_state_v0(PlayerShipState.DOCKED):
		return
	dock_target_kind_token = "DISCOVERY_SITE"
	dock_target_id = site_id
	print("UUIR|DISCOVERY_DOCK|DISCOVERY_SITE|" + site_id)
	if _discovery_panel and _discovery_panel.has_method("open_v0"):
		_discovery_panel.call("open_v0", site_id)

# GATE.S6.FRACTURE.PLAYER_DISPATCH.001: Player initiates fracture travel to a void site.
func on_fracture_travel_v0(void_site_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("DispatchFractureTravelV0"):
		return
	var fleet_id: String = "fleet_trader_1"
	bridge.call("DispatchFractureTravelV0", fleet_id, void_site_id)
	print("UUIR|FRACTURE_TRAVEL|" + void_site_id)

## Fast-path arrival for proximity entry and headless bots.
## TODO: Migrate to _arrive_at_system_v0() once approach_dir can be computed
## for non-cinematic arrivals (needs gate position lookup).
func on_lane_arrival_v0(arrived_node_id: String) -> void:
	if current_player_state != PlayerShipState.IN_LANE_TRANSIT:
		return

	print("UUIR|LANE_EXIT|" + arrived_node_id)
	# GATE.S14.TRANSIT.WARP_EFFECT.001: arrival flash + rumble
	_flash_warp_screen_v0()
	_apply_camera_shake_v0(0.25)

	# GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: Safety cleanup of warp tunnel on arrival.
	if _warp_tunnel_ref and is_instance_valid(_warp_tunnel_ref):
		_warp_tunnel_ref.queue_free()
		_warp_tunnel_ref = null

	var bridge = get_node_or_null("/root/SimBridge")

	# Use cached first-visit (saved before DispatchTravelCommandV0 marked node visited).
	var is_first_visit: bool = _lane_dest_is_first_visit
	# Store for camera to read during arrival zoom.
	_last_arrival_first_visit = is_first_visit

	if bridge and bridge.has_method("DispatchPlayerArriveV0"):
		bridge.call("DispatchPlayerArriveV0", arrived_node_id)
	# Eager-update: ensure _last_player_node_id reflects arrival so the next
	# lane entry reads the correct origin (toast poll may not have fired yet).
	_last_player_node_id = arrived_node_id
	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	_lane_cooldown_v0 = 1.0  # Pace overhaul: shorter cooldown (was 2.0s).

	# Restore local system labels (suppressed during transit).
	var gv_labels = _find_galaxy_view()
	if gv_labels and gv_labels.has_method("SetLocalLabelsVisibleV0"):
		gv_labels.call("SetLocalLabelsVisibleV0", true)

	# GATE.S15.FEEL.JUMP_EVENT_TOAST.001: Show toasts for jump events on this transit.
	_show_jump_event_toasts_v0(bridge)
	# GATE.S19.ONBOARD.FO_EVENTS.012: Immediate FO poll after lane arrival.
	_poll_fo_immediate()

	# GATE.S7.MAIN_MENU.AUTO_SAVE.001: Auto-save on warp arrival.
	_try_autosave_v0()

	# Local system is pre-rendered during transit (before this function is called).
	# Only rebuild persistent lanes to reveal newly discovered fog-of-war lanes.
	var gv = _find_galaxy_view()
	if gv and gv.has_method("RebuildPersistentLanesV0"):
		gv.call("RebuildPersistentLanesV0")

	# GATE.S13.WORLD.GATE_ARRIVAL.001: Position player at the gate that corresponds to the origin system.
	# GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Use star center instead of world origin.
	var star_center: Vector3 = Vector3.ZERO
	if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
		star_center = gv.call("GetCurrentStarGlobalPositionV0")
	if _hero_body and is_instance_valid(_hero_body):
		var arrival_pos: Vector3 = Vector3.ZERO
		if gv and gv.has_method("GetGatePositionV0") and not _lane_origin_node_id.is_empty():
			arrival_pos = gv.call("GetGatePositionV0", _lane_origin_node_id)
			print("UUIR|ARRIVAL_GATE|origin=" + _lane_origin_node_id + "|pos=" + str(arrival_pos))
		# If gate position found, offset slightly inward so player faces the system center
		if arrival_pos != Vector3.ZERO:
			var inward_dir: Vector3 = (star_center - arrival_pos).normalized()
			_hero_body.global_position = arrival_pos + inward_dir * 10.0
			# Face toward system center
			if _hero_body.is_inside_tree():
				_hero_body.look_at(star_center, Vector3.UP)
		else:
			# Fallback: compute gate direction from galaxy data and offset from star.
			print("UUIR|ARRIVAL_GATE_FALLBACK|origin=" + _lane_origin_node_id + "|star=" + str(star_center))
			var fallback_pos: Vector3 = star_center + Vector3(50.0, 0.0, 0.0) # Safe default: 50u from star
			if gv and gv.has_method("GetNodeScaledPositionV0") and not _lane_origin_node_id.is_empty():
				var origin_star_pos: Vector3 = gv.call("GetNodeScaledPositionV0", _lane_origin_node_id)
				if origin_star_pos != Vector3.ZERO and star_center != Vector3.ZERO:
					var dir_to_origin: Vector3 = (origin_star_pos - star_center).normalized()
					fallback_pos = star_center + dir_to_origin * 90.0
			# Safety: never place player closer than 15u from star center.
			if fallback_pos.distance_to(star_center) < 15.0:
				var escape_dir: Vector3 = (fallback_pos - star_center)
				if escape_dir.length() < 0.1:
					escape_dir = Vector3(1.0, 0.0, 0.0)
				fallback_pos = star_center + escape_dir.normalized() * 50.0
			_hero_body.global_position = fallback_pos
			if _hero_body.is_inside_tree() and star_center != Vector3.ZERO \
					and not _hero_body.global_position.is_equal_approx(star_center):
				_hero_body.look_at(star_center, Vector3.UP)
		_hero_body.linear_velocity = Vector3.ZERO
		_hero_body.angular_velocity = Vector3.ZERO

	# Camera rotation sweep removed (fixed top-down camera, no yaw/pitch offsets).

	# GATE.X.WARP.ARRIVAL_DRAMA.001: Show arrival drama (letterbox + title card).
	# Skip if this is a first-visit (which uses the full flyby letterbox instead).
	if not is_first_visit:
		_show_arrival_drama_v0(arrived_node_id, bridge)


## Position hero at the arrival gate corresponding to the origin system.
## Extracted from on_lane_arrival_v0 for reuse in reveal sweep.
func _position_hero_at_gate_v0(_arrived_node_id: String, gv) -> void:
	var star_center: Vector3 = Vector3.ZERO
	if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
		star_center = gv.call("GetCurrentStarGlobalPositionV0")
	if _hero_body == null or not is_instance_valid(_hero_body):
		return
	var arrival_pos: Vector3 = Vector3.ZERO
	if gv and gv.has_method("GetGatePositionV0") and not _lane_origin_node_id.is_empty():
		arrival_pos = gv.call("GetGatePositionV0", _lane_origin_node_id)
	if arrival_pos != Vector3.ZERO:
		var inward_dir: Vector3 = (star_center - arrival_pos).normalized()
		_hero_body.global_position = arrival_pos + inward_dir * 10.0
		if _hero_body.is_inside_tree():
			_hero_body.look_at(star_center, Vector3.UP)
	else:
		var fallback_pos: Vector3 = star_center + Vector3(50.0, 0.0, 0.0)
		if gv and gv.has_method("GetNodeScaledPositionV0") and not _lane_origin_node_id.is_empty():
			var origin_star_pos: Vector3 = gv.call("GetNodeScaledPositionV0", _lane_origin_node_id)
			if origin_star_pos != Vector3.ZERO and star_center != Vector3.ZERO:
				var dir_to_origin: Vector3 = (origin_star_pos - star_center).normalized()
				fallback_pos = star_center + dir_to_origin * 90.0
		if fallback_pos.distance_to(star_center) < 15.0:
			var escape_dir: Vector3 = (fallback_pos - star_center)
			if escape_dir.length() < 0.1:
				escape_dir = Vector3(1.0, 0.0, 0.0)
			fallback_pos = star_center + escape_dir.normalized() * 50.0
		_hero_body.global_position = fallback_pos
		if _hero_body.is_inside_tree() and star_center != Vector3.ZERO \
				and not _hero_body.global_position.is_equal_approx(star_center):
			_hero_body.look_at(star_center, Vector3.UP)
	_hero_body.linear_velocity = Vector3.ZERO
	_hero_body.angular_velocity = Vector3.ZERO



## Show letterbox overlay with system name during first-visit reveal.
## Letterbox overlay for first-visit flyby cinematic.
## display_duration = total time the letterbox is visible (bars + text + hold).
func _show_flyby_letterbox_v0(node_id: String, bridge: Node, display_duration: float) -> void:
	# FEEL_POST_FIX_10: Don't create letterbox if player is already docked
	# (transit coroutine may still be running after _force_arrival + dock).
	if current_player_state == PlayerShipState.DOCKED:
		return
	var display_name: String = node_id
	if bridge and bridge.has_method("GetNodeDisplayNameV0"):
		display_name = bridge.call("GetNodeDisplayNameV0", node_id)
	# FEEL_POST_FIX_8: Strip parenthesized production tags for clean arrival text.
	var paren_idx: int = display_name.find("(")
	if paren_idx > 0:
		display_name = display_name.substr(0, paren_idx).strip_edges()

	# Audio.
	if _sfx_system_arrival:
		_sfx_system_arrival.volume_db = -14.0
		_sfx_system_arrival.play()
		var vol_tween := create_tween()
		vol_tween.tween_property(_sfx_system_arrival, "volume_db", -4.0, 2.5) \
			.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_SINE)
		vol_tween.tween_interval(5.0)
		vol_tween.tween_property(_sfx_system_arrival, "volume_db", -40.0, 3.0) \
			.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_SINE)
		vol_tween.tween_callback(_sfx_system_arrival.stop)
	if _sfx_warp_whoosh:
		_sfx_warp_whoosh.volume_db = -18.0
		_sfx_warp_whoosh.play()

	# Letterbox + system name overlay.
	var canvas := CanvasLayer.new()
	canvas.name = "FlybyLetterbox"
	canvas.layer = 90
	canvas.add_to_group("flyby_letterbox")
	add_child(canvas)
	# FEEL_POST_FIX_10: Full-screen dark blue scrim for cooler cinematic tone.
	var scrim := ColorRect.new()
	scrim.color = Color(0.02, 0.04, 0.10, 0.70)
	scrim.set_anchors_preset(Control.PRESET_FULL_RECT)
	scrim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas.add_child(scrim)
	var bar_top := ColorRect.new()
	bar_top.color = Color(0, 0, 0, 0.85)
	bar_top.set_anchors_preset(Control.PRESET_TOP_WIDE)
	bar_top.offset_bottom = 0.0
	bar_top.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas.add_child(bar_top)
	var bar_bot := ColorRect.new()
	bar_bot.color = Color(0, 0, 0, 0.85)
	bar_bot.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	bar_bot.anchor_top = 1.0
	bar_bot.anchor_bottom = 1.0
	bar_bot.offset_top = 0.0
	bar_bot.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas.add_child(bar_bot)
	var lbl := Label.new()
	lbl.text = display_name
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	lbl.add_theme_font_size_override("font_size", 64)
	lbl.add_theme_color_override("font_color", Color(0.85, 0.92, 1.0, 1.0))
	lbl.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.2, 0.9))
	lbl.add_theme_constant_override("shadow_offset_x", 3)
	lbl.add_theme_constant_override("shadow_offset_y", 3)
	lbl.modulate.a = 0.0
	canvas.add_child(lbl)
	var subtitle := Label.new()
	subtitle.text = "~ First Discovery ~"
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	subtitle.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	subtitle.offset_top = 90.0
	subtitle.add_theme_font_size_override("font_size", 24)
	subtitle.add_theme_color_override("font_color", Color(0.6, 0.75, 0.9, 1.0))
	subtitle.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.15, 0.7))
	subtitle.add_theme_constant_override("shadow_offset_x", 1)
	subtitle.add_theme_constant_override("shadow_offset_y", 1)
	subtitle.modulate.a = 0.0
	canvas.add_child(subtitle)
	# Hold time: total duration minus bars + text fade in/out.
	var hold_time: float = maxf(display_duration - 1.6, 1.0)
	var tween := create_tween()
	tween.tween_property(bar_top, "size:y", 80.0, 0.3).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(bar_bot, "offset_top", -80.0, 0.3).set_ease(Tween.EASE_OUT)
	tween.tween_property(lbl, "modulate:a", 1.0, 0.5).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(subtitle, "modulate:a", 1.0, 0.5).set_ease(Tween.EASE_OUT)
	tween.tween_interval(hold_time)
	tween.tween_property(lbl, "modulate:a", 0.0, 0.5)
	tween.parallel().tween_property(subtitle, "modulate:a", 0.0, 0.4)
	tween.tween_property(bar_top, "size:y", 0.0, 0.3).set_ease(Tween.EASE_IN)
	tween.parallel().tween_property(bar_bot, "offset_top", 0.0, 0.3).set_ease(Tween.EASE_IN)
	tween.tween_callback(canvas.queue_free)

## FEEL_POST_FIX_10: Remove flyby letterbox from both GameManagers.
## CanvasLayer has no `visible` property — must remove all children to hide instantly.
func _kill_flyby_letterbox_v0() -> void:
	# Search both this GM and the autoload GM for any FlybyLetterbox nodes.
	var targets: Array = []
	var lb = get_node_or_null("FlybyLetterbox")
	if lb:
		targets.append(lb)
	# Also check autoload GM (dual-GM architecture).
	var autoload_gm = get_node_or_null("/root/GameManager")
	if autoload_gm and autoload_gm != self:
		var lb2 = autoload_gm.get_node_or_null("FlybyLetterbox")
		if lb2:
			targets.append(lb2)
	# Also check scene GM.
	if get_tree():
		var scene_gm = get_tree().root.find_child("GameManager", true, false)
		if scene_gm and scene_gm != self and scene_gm != autoload_gm:
			var lb3 = scene_gm.get_node_or_null("FlybyLetterbox")
			if lb3:
				targets.append(lb3)
	for t in targets:
		# Remove all children (labels, bars, scrim) so nothing renders.
		for child in t.get_children():
			t.remove_child(child)
			child.queue_free()
		# Then remove the CanvasLayer itself.
		if t.get_parent():
			t.get_parent().remove_child(t)
		t.queue_free()
	if targets.size() > 0:
		print("UUIR|LETTERBOX_KILLED|count=%d" % targets.size())

## GATE.X.WARP.ARRIVAL_DRAMA.001: Trigger HUD arrival drama (letterbox + title card).
## Used for return visits (non-first-visit arrivals). First visits use _show_flyby_letterbox_v0.
func _show_arrival_drama_v0(node_id: String, bridge_ref: Node) -> void:
	# Get system display name.
	var display_name: String = node_id
	if bridge_ref and bridge_ref.has_method("GetNodeDisplayNameV0"):
		display_name = bridge_ref.call("GetNodeDisplayNameV0", node_id)
	# Strip parenthesized production tags for clean display.
	var paren_idx: int = display_name.find("(")
	if paren_idx > 0:
		display_name = display_name.substr(0, paren_idx).strip_edges()

	# Get controlling faction name (if any).
	var faction_name: String = ""
	if bridge_ref and bridge_ref.has_method("GetTerritoryAccessV0"):
		var territory: Dictionary = bridge_ref.call("GetTerritoryAccessV0", node_id)
		faction_name = str(territory.get("faction_id", ""))

	# Find HUD and trigger drama.
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud and hud.has_method("show_arrival_drama_v0"):
		hud.call("show_arrival_drama_v0", display_name, faction_name)
		print("UUIR|ARRIVAL_DRAMA|system=%s|faction=%s" % [display_name, faction_name])

func _on_station_menu_request_undock():
	undock_v0()

func _on_discovery_panel_request_undock():
	undock_v0()

# GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001: fleet proximity targeting
func on_fleet_proximity_entered_v0(fleet_id: String) -> void:
	if current_player_state != PlayerShipState.IN_FLIGHT:
		return
	_targeted_fleet_id = fleet_id
	print("UUIR|FLEET_TARGET|" + fleet_id)

# Real-time turret fire: G key spawns a projectile toward nearest fleet in range.
# Damage is applied by bullet.gd on collision via SimBridge.ApplyTurretShotV0.
func _fire_turret_v0() -> void:
	if _turret_cooldown > 0.0:
		return
	var target := _find_nearest_fleet_v0(TURRET_RANGE)
	if target == null:
		return
	if _hero_body == null or not is_instance_valid(_hero_body):
		return
	var muzzle = _hero_body.get_node_or_null("Muzzle")
	var fire_pos = muzzle.global_position if muzzle else _hero_body.global_position
	_spawn_bullet_v0(fire_pos, target.global_position, true, "")
	_turret_cooldown = TURRET_COOLDOWN_SEC
	# FEEL_POST_FIX_3: Player firing also triggers combat state.
	signal_combat_v0()
	if _sfx_turret_fire:
		_sfx_turret_fire.play()
	# GATE.S1.CAMERA.COMBAT_SHAKE.001: small shake on turret fire
	_apply_camera_shake_v0(0.15)

# GATE.S1.CAMERA.COMBAT_SHAKE.001: relay shake to player_follow_camera.
func _apply_camera_shake_v0(intensity: float) -> void:
	var cam = get_viewport().get_camera_3d()
	if cam and cam.has_method("apply_shake"):
		cam.call("apply_shake", intensity)

# AI auto-fire: nearest hostile fleet in aggro range fires at player each cooldown tick.
func _ai_fire_v0() -> void:
	if _hero_body == null or not is_instance_valid(_hero_body):
		return
	var nearest := _find_nearest_fleet_v0(AI_AGGRO_RANGE)
	if nearest == null:
		return
	# Only hostile fleets fire at the player (traders/haulers are peaceful).
	var is_hostile: bool = nearest.get_meta("is_hostile", false)
	if not is_hostile:
		return
	var fleet_id: String = _get_fleet_id_from_marker(nearest)
	if fleet_id.is_empty():
		return
	_spawn_bullet_v0(nearest.global_position, _hero_body.global_position, false, fleet_id)
	_ai_fire_cooldown = AI_FIRE_COOLDOWN_SEC
	# FEEL_POST_FIX_3: AI firing at player triggers combat state.
	signal_combat_v0()

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: poll SimBridge each frame for player hull <= 0.
func _check_player_death_v0() -> void:
	return  # Disabled: player death turned off while debugging fleet combat
	#var bridge = get_node_or_null("/root/SimBridge")
	#if bridge == null or not bridge.has_method("GetFleetCombatHpV0"):
	#	return
	#var hp: Dictionary = bridge.call("GetFleetCombatHpV0", PLAYER_FLEET_ID)
	#var hull: int = hp.get("hull", -1)
	#if hull_max_is_set_v0(hp) and hull <= 0:
	#	notify_player_killed_v0()

func hull_max_is_set_v0(hp: Dictionary) -> bool:
	return hp.get("hull_max", 0) > 0

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: called after any damage source kills the player.
func notify_player_killed_v0() -> void:
	if _player_dead:
		return
	_player_dead = true
	# Close galaxy overlay if open so Game Over screen is visible
	if galaxy_overlay_open:
		toggle_galaxy_map_overlay_v0()
	print("UUIR|PLAYER_DEAD")
	# Notify HUD to show game over overlay
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud and hud.has_method("show_game_over_v0"):
		hud.call("show_game_over_v0")

func is_player_dead_v0() -> bool:
	return _player_dead

# GATE.S8.WIN.GAME_OVER_WIRE.001: Poll SimBridge for terminal game state and transition.
func _poll_game_over_v0() -> void:
	if _game_over_triggered:
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetGameResultV0"):
		return
	var result: Dictionary = bridge.call("GetGameResultV0")
	if not result.get("is_terminal", false):
		return
	_game_over_triggered = true
	# Auto-save before transition.
	if bridge.has_method("AutoSaveV0"):
		bridge.call("AutoSaveV0")
	# Stop the sim before scene change.
	if bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	var result_code: int = result.get("result", 0)
	print("UUIR|GAME_OVER|result=%d" % result_code)
	# Victory = 1, Death = 2, Bankruptcy = 3
	if result_code == 1:
		get_tree().change_scene_to_file("res://scenes/ui/victory_screen.tscn")
	elif result_code == 2 or result_code == 3:
		get_tree().change_scene_to_file("res://scenes/ui/loss_screen.tscn")

func _find_nearest_fleet_v0(max_range: float) -> Node3D:
	if _hero_body == null or not is_instance_valid(_hero_body):
		return null
	var player_pos: Vector3 = _hero_body.global_position
	var best: Node3D = null
	var best_dist: float = max_range + 1.0
	for node in get_tree().get_nodes_in_group("FleetShip"):
		if not is_instance_valid(node):
			continue
		var dist: float = player_pos.distance_to(node.global_position)
		if dist < best_dist:
			best_dist = dist
			best = node
		elif dist == best_dist and best != null:
			if str(node.name) < str(best.name):
				best = node
	if best_dist > max_range:
		return null
	return best

## FEEL_POST_FIX_3: Called by npc_ship.on_hit() and bullet.gd to signal active combat.
## Keeps HUD in "COMBAT" state for 5 seconds after the last combat event.
func signal_combat_v0() -> void:
	combat_state_timer = 5.0

func _get_fleet_id_from_marker(marker: Node3D) -> String:
	var n: String = str(marker.name)
	if n.begins_with("Fleet_"):
		return n.substr(6)
	for child in marker.get_children():
		if child.has_meta("fleet_id"):
			return str(child.get_meta("fleet_id"))
	return ""

func _spawn_bullet_v0(origin: Vector3, target_pos: Vector3, is_player_bullet: bool, ai_fleet_id: String, p_damage_family: String = "") -> void:
	if _bullet_scene == null:
		return
	var bullet = _bullet_scene.instantiate()
	# Set collision and source properties BEFORE adding to tree to prevent
	# spurious contacts on the first physics frame.
	bullet.set("source_is_player", is_player_bullet)
	if not is_player_bullet:
		bullet.set("source_fleet_id", ai_fleet_id)
	# GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001: Set damage family for visual differentiation.
	var family: String = p_damage_family
	if family.is_empty():
		family = "energy" if is_player_bullet else "kinetic"
	bullet.set("damage_family", family)
	if is_player_bullet:
		bullet.collision_layer = 0
		bullet.collision_mask = 4  # Detect FleetTarget layer (bit 2)
	else:
		bullet.collision_layer = 0
		bullet.collision_mask = 2  # Detect Ships layer (bit 1)
	# Compute direction before add_child so _ready() sees non-zero velocity.
	var direction: Vector3 = (target_pos - origin).normalized()
	if bullet.has_method("set_direction"):
		bullet.call("set_direction", direction)
	get_tree().root.add_child(bullet)
	bullet.global_position = origin

# Despawn defeated fleet marker from local scene.
# Uses FleetShip group for lookup — fleet markers live under LocalSystem (sibling of GalaxyView).
func despawn_fleet_v0(fleet_id: String) -> void:
	var target_name := "Fleet_" + fleet_id
	for node in get_tree().get_nodes_in_group("FleetShip"):
		if str(node.name) == target_name:
			print("UUIR|FLEET_DESPAWN|" + target_name)
			if _sfx_explosion:
				_sfx_explosion.play()
			# GATE.S7.COMBAT_JUICE.EXPLOSION_VFX.001: Spawn explosion before removing ship.
			var ExplosionVfx = load("res://scripts/vfx/explosion_effect.gd")
			if ExplosionVfx and ExplosionVfx.has_method("spawn"):
				var vfx_parent = node.get_parent() if node.get_parent() else get_tree().root
				ExplosionVfx.call("spawn", vfx_parent, node.global_position)
			node.remove_from_group("FleetShip")  # Immediate removal stops AI targeting
			node.queue_free()
			return

func _find_galaxy_view():
	if _galaxy_view and is_instance_valid(_galaxy_view):
		return _galaxy_view
	# Autoload GameManager can't find scene-child GalaxyView in _ready(); lazy-find here.
	_galaxy_view = get_tree().root.find_child("GalaxyView", true, false)
	return _galaxy_view

func _begin_lane_transit_v0(neighbor_node_id: String) -> void:
	# GATE.S17.REAL_SPACE.LANE_TRANSIT.001: Galaxy-map zoom transit.
	# Camera zooms out to galaxy map, a transit marker flies from origin to destination
	# star, then camera zooms back in at the destination.
	_lane_dest_node_id = neighbor_node_id
	_ensure_hero_body()
	var gv = _find_galaxy_view()

	# Get star center positions and pre-computed gate positions from GalaxyView cache.
	var origin_star_center: Vector3 = Vector3.ZERO
	var dest_pos: Vector3 = Vector3.ZERO  # Destination star center.
	var origin_gate_pos: Vector3 = Vector3.ZERO
	var dest_gate_pos: Vector3 = Vector3.ZERO
	if gv and gv.has_method("GetNodeScaledPositionV0"):
		if not _lane_origin_node_id.is_empty():
			origin_star_center = gv.call("GetNodeScaledPositionV0", _lane_origin_node_id)
		dest_pos = gv.call("GetNodeScaledPositionV0", neighbor_node_id)
	# Use pre-computed gate positions from GalaxyView (includes nudging for close gates).
	if gv and gv.has_method("GetCachedGateGlobalPositionV0"):
		if not _lane_origin_node_id.is_empty():
			origin_gate_pos = gv.call("GetCachedGateGlobalPositionV0", _lane_origin_node_id, neighbor_node_id)
		dest_gate_pos = gv.call("GetCachedGateGlobalPositionV0", neighbor_node_id, _lane_origin_node_id)
	# Fallback if cache returned star center (gate at star center means offset failed).
	# A star CAN be at (0,0,0) so check if gate equals star center, not if gate is zero.
	if dest_pos == origin_star_center:
		# Stars overlap — use a synthetic direction so transit has nonzero length
		var synth_angle := float(neighbor_node_id.hash()) * 0.001
		var synth_dir := Vector3(cos(synth_angle), 0.0, sin(synth_angle))
		origin_gate_pos = origin_star_center - synth_dir * 90.0
		dest_gate_pos = origin_star_center + synth_dir * 90.0
	else:
		if origin_gate_pos == origin_star_center:
			var lane_dir_fb := (dest_pos - origin_star_center).normalized()
			origin_gate_pos = origin_star_center + lane_dir_fb * 90.0
		if dest_gate_pos == dest_pos:
			var lane_dir_fb := (dest_pos - origin_star_center).normalized()
			dest_gate_pos = dest_pos - lane_dir_fb * 90.0

	# Origin position = hero body (ship is visually at the gate after cinematic pull).
	var origin_pos: Vector3 = origin_gate_pos
	if _hero_body and is_instance_valid(_hero_body):
		origin_pos = Vector3(_hero_body.global_position.x, 0.0, _hero_body.global_position.z)
	# Compute lane direction from ACTUAL origin (hero pos) to destination gate.
	# Using hero pos instead of cached origin_gate_pos avoids tunnel misalignment
	# when the scene node gate position differs from the cache.
	var lane_dir := (dest_gate_pos - origin_pos)
	lane_dir.y = 0.0
	lane_dir = lane_dir.normalized() if lane_dir.length() > 0.1 else Vector3(1, 0, 0)
	print("UUIR|LANE_GEOM|origin_star=%s|dest_star=%s|origin_gate=%s|dest_gate=%s" % [
		str(origin_star_center), str(dest_pos), str(origin_gate_pos), str(dest_gate_pos)])

	if dest_pos == Vector3.ZERO or _hero_body == null or not is_instance_valid(_hero_body):
		# Fallback: instant transit.
		await get_tree().create_timer(0.3).timeout
		on_lane_arrival_v0(neighbor_node_id)
		return

	if origin_pos == Vector3.ZERO:
		origin_pos = _hero_body.global_position

	warp_transit_dest_pos = dest_gate_pos
	warp_transit_travel_dir = lane_dir

	var distance: float = origin_pos.distance_to(dest_gate_pos)
	# Pace overhaul: faster transit (was distance/2000, 1.5-3.0s).
	var transit_time: float = clampf(distance / 2500.0, 1.0, 2.0)

	# GATE.S3.RISK_SINKS.BRIDGE.001: Add delay from risk events to transit time.
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetDelayStatusV0"):
		var delay_info: Dictionary = bridge.call("GetDelayStatusV0", PLAYER_FLEET_ID)
		if delay_info.get("delayed", false):
			var ticks_remaining: int = int(delay_info.get("ticks_remaining", 0))
			var delay_sec: float = ticks_remaining * 0.1
			if delay_sec > 0.0:
				print("UUIR|LANE_DELAY|" + str(ticks_remaining) + "|" + neighbor_node_id)
				transit_time += delay_sec

	# Freeze ship movement.
	_hero_body.linear_velocity = Vector3.ZERO
	_hero_body.angular_velocity = Vector3.ZERO

	# GATE.X.WARP.TRANSIT_HUD.001: Record transit timing for HUD progress overlay.
	warp_transit_start_msec = Time.get_ticks_msec()
	warp_transit_duration_sec = transit_time
	warp_transit_origin_pos = origin_pos

	# === ZOOM OUT & FOLLOW ALONG THE LANE ===
	# Camera follows the transit marker as it moves along the lane.
	warp_transit_target = origin_pos
	# Start at the camera's current altitude so there's no sudden jump.
	var cam_ctrl = _find_camera_controller()
	var start_alt: float = 80.0
	if cam_ctrl:
		var a = cam_ctrl.get("_altitude")
		if a != null:
			start_alt = float(a)
	warp_transit_altitude = start_alt
	# Reset tilt to top-down (will be tweened forward during zoom-out).
	if cam_ctrl and cam_ctrl.get("warp_transit_tilt") != null:
		cam_ctrl.warp_transit_tilt = 0.0
	var cruise_altitude: float = clampf(distance * 0.04, 200.0, 450.0)

	# Ensure persistent lanes are built before we zoom out.
	if gv and gv.has_method("EnsurePersistentLanesBuiltV0"):
		gv.call("EnsurePersistentLanesBuiltV0")

	# Create a glowing transit marker at origin gate position.
	_transit_marker = _create_transit_marker_v0(origin_pos)

	# GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: Spawn warp tunnel VFX around transit marker.
	# Scale to galaxy-map proportions so it is visible at cruise altitude (200-450 units).
	if _warp_tunnel_ref and is_instance_valid(_warp_tunnel_ref):
		_warp_tunnel_ref.queue_free()
	_warp_tunnel_ref = WarpTunnel.new()
	_warp_tunnel_ref.setup(0.7)
	_transit_marker.add_child(_warp_tunnel_ref)
	# Orient tunnel along lane direction (tunnel extends along Z by default).
	if lane_dir.length() > 0.1:
		_warp_tunnel_ref.look_at(_warp_tunnel_ref.global_position + lane_dir, Vector3.UP)
	# Scale up: tunnel is designed for ship-level (radius 8); at galaxy map scale (altitude 200-450)
	# we need 4x to make the cylinder and particles visible and dramatic.
	_warp_tunnel_ref.scale = Vector3(4.0, 4.0, 4.0)

	# Pre-render destination system now so it becomes visible as camera descends.
	if gv and gv.has_method("RebuildLocalSystemV0"):
		gv.call("RebuildLocalSystemV0", neighbor_node_id)
	# Suppress all labels during transit — player should discover them, not read them in passing.
	if gv and gv.has_method("SetLocalLabelsVisibleV0"):
		gv.call("SetLocalLabelsVisibleV0", false)

	# Create bright lane line from origin gate to destination gate.
	var _transit_lane_line := _create_transit_lane_line_v0(origin_pos, dest_gate_pos)

	# Smooth zoom-out: tween altitude from current camera height to cruise altitude.
	# Pace overhaul: faster zoom-out (was 0.6s).
	var zoom_out_time: float = 0.3
	var zoom_out_tween := create_tween()
	zoom_out_tween.tween_property(self, "warp_transit_altitude", cruise_altitude, zoom_out_time) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

	# Tilt camera forward so destination is visible during transit (comet approach).
	if cam_ctrl and cam_ctrl.get("warp_transit_tilt") != null:
		var tilt_tween := create_tween()
		# Pace overhaul: faster tilt (was 0.8s).
		tilt_tween.tween_property(cam_ctrl, "warp_transit_tilt", 1.0, 0.4) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

	# Wait for zoom-out to complete before marker starts moving.
	await zoom_out_tween.finished

	# Use cached first-visit (saved before DispatchTravelCommandV0 marked node visited).
	var is_first_visit: bool = _lane_dest_is_first_visit

	# Store lane line ref for cleanup.
	_transit_lane_line_ref = _transit_lane_line

	# === Compute flyby geometry ===
	var star_center: Vector3 = dest_pos  # Destination star center.
	var entry_point := star_center - lane_dir * FLYBY_APPROACH_DIST
	entry_point.y = 0.0

	# Approach: marker flies to entry point (not all the way to dest gate).
	var dist_to_entry: float = origin_pos.distance_to(entry_point)
	var dist_to_dest: float = origin_pos.distance_to(dest_gate_pos)
	var approach_ratio: float = clampf(dist_to_entry / maxf(dist_to_dest, 1.0), 0.3, 0.95)
	var approach_time: float = transit_time * approach_ratio

	var tween := create_tween()
	tween.tween_property(_transit_marker, "global_position", entry_point, approach_time) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

	# Altitude descent during approach.
	var alt_tween := create_tween()
	alt_tween.tween_property(self, "warp_transit_altitude", 120.0, approach_time) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

	# Fade lane line + marker near end of approach.
	var fade_delay: float = approach_time * 0.6
	var fade_time: float = approach_time * 0.4
	if _transit_lane_line and is_instance_valid(_transit_lane_line):
		var lane_mat = _transit_lane_line.material_override as StandardMaterial3D
		if lane_mat:
			var lane_fade := create_tween()
			lane_fade.tween_interval(fade_delay)
			lane_fade.tween_property(lane_mat, "albedo_color:a", 0.0, fade_time)
	# Transit marker is now invisible (sphere removed) — no fade needed.

	# GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001: Fade warp tunnel before cleanup.
	if _warp_tunnel_ref and is_instance_valid(_warp_tunnel_ref) and _warp_tunnel_ref.has_method("despawn"):
		_warp_tunnel_ref.despawn(fade_time)

	await tween.finished

	# Clean up transit marker + lane line (warp tunnel is child of marker, freed with it).
	if _transit_marker and is_instance_valid(_transit_marker):
		_transit_marker.queue_free()
		_transit_marker = null
	_warp_tunnel_ref = null
	if _transit_lane_line_ref and is_instance_valid(_transit_lane_line_ref):
		_transit_lane_line_ref.queue_free()
		_transit_lane_line_ref = null

	# === Universal arrival — decoupled from travel method ===
	await _arrive_at_system_v0(
		neighbor_node_id,
		star_center,
		dest_gate_pos,
		lane_dir,           # approach_dir
		is_first_visit,
		cruise_altitude
	)

## Universal arrival handler — called by any travel method after transit completes.
## Dispatches SimBridge arrival, positions hero, triggers appropriate cinematic.
## Decoupled from travel method so fracture, haven, story travel all share it.
func _arrive_at_system_v0(
	node_id: String,
	star_center: Vector3,
	dest_gate_pos: Vector3,
	approach_dir: Vector3,     # direction camera approaches from (lane_dir for warp, rift_dir for fracture)
	is_first_visit: bool,
	cruise_altitude: float     # camera altitude at handoff from transit
) -> void:
	# Suppress transit overlay — player has visually arrived.
	suppress_transit_overlay = true
	_last_arrival_first_visit = is_first_visit
	_apply_camera_shake_v0(0.25)

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerArriveV0"):
		bridge.call("DispatchPlayerArriveV0", node_id)

	var gv = _find_galaxy_view()
	# Suppress labels during flyby/descent.
	if gv and gv.has_method("SetLocalLabelsVisibleV0"):
		gv.call("SetLocalLabelsVisibleV0", false)
	if gv and gv.has_method("RebuildPersistentLanesV0"):
		gv.call("RebuildPersistentLanesV0")

	# Position hero at arrival gate (hidden during flyby).
	if _hero_body and is_instance_valid(_hero_body):
		var inward_dir := (star_center - dest_gate_pos).normalized()
		_hero_body.global_position = dest_gate_pos + inward_dir * 10.0
		_hero_body.global_position.y = 0.0
		_hero_body.linear_velocity = Vector3.ZERO
		_hero_body.angular_velocity = Vector3.ZERO
		if _hero_body.is_inside_tree():
			_hero_body.look_at(star_center, Vector3.UP)
		_hero_body.visible = false  # Hidden during flyby.
	var hero_pos: Vector3 = _hero_body.global_position if (_hero_body and is_instance_valid(_hero_body)) else dest_gate_pos
	print("UUIR|HERO_AT_GATE|gate=%s|hero=%s|star=%s" % [str(dest_gate_pos), str(hero_pos), str(star_center)])

	# Show jump event toasts (warfront changes, discoveries, etc).
	_show_jump_event_toasts_v0(bridge)

	# Cinematic: first visit gets full flyby, return visit gets fast descent.
	var cam_ctrl = _find_camera_controller()
	var has_cam: bool = cam_ctrl != null and cam_ctrl.get("flyby_active") != null

	if has_cam and is_first_visit:
		await _play_first_visit_flyby_v0(star_center, dest_gate_pos, approach_dir, hero_pos, cruise_altitude, cam_ctrl)
	elif has_cam:
		await _play_return_arrival_v0(hero_pos, cruise_altitude, cam_ctrl)

	# === Transition to IN_FLIGHT ===
	suppress_transit_overlay = false
	if current_player_state != PlayerShipState.IN_FLIGHT:
		_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	_lane_cooldown_v0 = 1.0

	# Restore labels + hero + HUD.
	var gv_restore = _find_galaxy_view()
	if gv_restore and gv_restore.has_method("SetLocalLabelsVisibleV0"):
		gv_restore.call("SetLocalLabelsVisibleV0", true)
	if _hero_body and is_instance_valid(_hero_body):
		_hero_body.visible = true
	var hud = get_tree().root.find_child("HUD", true, false) if get_tree() else null
	if hud and hud.has_method("set_overlay_mode_v0"):
		hud.call("set_overlay_mode_v0", false)

	# Post-arrival steps (previously missing from cinematic path).
	_try_autosave_v0()
	_poll_fo_immediate()
	if not is_first_visit:
		_show_arrival_drama_v0(node_id, bridge)

## Return-visit arrival: fast camera descent, no flyby. Quick and respectful of player time.
func _play_return_arrival_v0(hero_pos: Vector3, cruise_altitude: float, cam_ctrl: Node) -> void:
	warp_transit_target = hero_pos
	var descent := create_tween()
	descent.tween_property(self, "warp_transit_altitude", 80.0, 0.8) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
	if cam_ctrl.get("warp_transit_tilt") != null:
		var tilt_reset := create_tween()
		tilt_reset.tween_property(cam_ctrl, "warp_transit_tilt", 0.0, 0.8) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
	await descent.finished
	if cam_ctrl.has_method("notify_flyby_arrival_v0"):
		cam_ctrl.call("notify_flyby_arrival_v0", 80.0)
	else:
		cam_ctrl._altitude = 80.0
		cam_ctrl._galaxy_map_pan_offset = Vector3.ZERO
	var gv_fast = _find_galaxy_view()
	if gv_fast and gv_fast.has_method("SetTransitModeV0"):
		gv_fast.call("SetTransitModeV0", false, "", "")
	print("UUIR|FAST_RETURN|hero=%s" % str(hero_pos))

## First-visit flyby cinematic: Euler spiral entry → orbit sweep → spiral exit → letterbox.
## Decoupled from travel method — receives geometry as parameters.
func _play_first_visit_flyby_v0(
	star_center: Vector3,
	dest_gate_pos: Vector3,
	approach_dir: Vector3,     # normalized direction camera approaches from
	hero_pos: Vector3,         # where the hero ship is (for exit spiral depart angle)
	cruise_altitude: float,    # starting altitude (for entry transition)
	cam_ctrl: Node             # camera controller reference
) -> void:
	# Capture current camera position for seamless transition from WARP_TRANSIT.
	var entry_cam_pos: Vector3 = cam_ctrl._cam.global_position if cam_ctrl._cam else Vector3(hero_pos.x, 120.0, hero_pos.z)
	var current_tilt: float = cam_ctrl.warp_transit_tilt if cam_ctrl.get("warp_transit_tilt") != null else 0.0
	var current_up: Vector3 = Vector3.BACK.slerp(Vector3.UP, current_tilt)
	if current_up.length_squared() < 0.01:
		current_up = Vector3.UP

	# Compute flyby orbit geometry using TANGENT entry.
	var approach_angle: float = atan2(-approach_dir.z, -approach_dir.x)  # From star toward camera.
	var dest_gate_offset: Vector3 = dest_gate_pos - star_center
	dest_gate_offset.y = 0.0
	var dest_gate_angle: float = atan2(dest_gate_offset.z, dest_gate_offset.x)

	# Tangent entry points: 90° offset from approach gives tangential join.
	var tangent_cw: float = approach_angle - PI / 2.0
	var tangent_ccw: float = approach_angle + PI / 2.0

	# Pick the side that gives the LONGER sweep to dest gate (more cinematic).
	var cw_sweep: float = fmod(tangent_cw - dest_gate_angle + TAU, TAU)
	var ccw_sweep: float = fmod(dest_gate_angle - tangent_ccw + TAU, TAU)

	var tangent_angle: float
	var orbit_dir: float
	var sweep_total: float
	if cw_sweep >= ccw_sweep:
		tangent_angle = tangent_cw
		orbit_dir = -1.0
		sweep_total = cw_sweep
	else:
		tangent_angle = tangent_ccw
		orbit_dir = 1.0
		sweep_total = ccw_sweep
	if sweep_total < PI:
		sweep_total = PI * 1.25  # Ensure at least 225° for cinematic impact.

	# Activate flyby — camera switches from WARP_TRANSIT to direct position control.
	cam_ctrl.flyby_cam_pos = entry_cam_pos
	cam_ctrl.flyby_look_at = Vector3(dest_gate_pos.x, 5.0, dest_gate_pos.z)
	cam_ctrl.flyby_up = current_up
	cam_ctrl.flyby_active = true
	cam_ctrl.input_locked = true

	var actual_orbit_time: float = FLYBY_ORBIT_TIME
	var actual_sweep: float = sweep_total

	# First-visit letterbox overlay (shown during orbit).
	var bridge = get_node_or_null("/root/SimBridge")
	_show_flyby_letterbox_v0(_lane_dest_node_id, bridge, FLYBY_CURVE_ON_TIME + actual_orbit_time)

	print("UUIR|FLYBY_START|approach=%.2f|tangent=%.2f|dest=%.2f|sweep=%.2f|dir=%.1f" % [approach_angle, tangent_angle, dest_gate_angle, sweep_total, orbit_dir])

	# Camera's actual position relative to star — used for spiral deviation.
	var entry_star_off := Vector3(entry_cam_pos.x - star_center.x, 0.0, entry_cam_pos.z - star_center.z)
	var entry_radius: float = maxf(entry_star_off.length(), FLYBY_ORBIT_RADIUS + 10.0)
	var entry_alt: float = entry_cam_pos.y
	var actual_entry_angle: float = atan2(entry_star_off.z, entry_star_off.x)

	# --- Phase 1: Deviation — spiral from approach onto orbit circle tangent ---
	var dev_steps: int = 24
	var dev_step_time: float = FLYBY_CURVE_ON_TIME / float(dev_steps)
	var dev_tween := create_tween()
	for i in range(dev_steps):
		var t: float = float(i + 1) / float(dev_steps)
		var smooth_t: float = ease(t, 2.0)
		var angle: float = lerp_angle(actual_entry_angle, tangent_angle, smooth_t)
		var dev_radius: float = lerpf(entry_radius, FLYBY_ORBIT_RADIUS, ease(t, 0.8))
		var dev_alt: float = lerpf(entry_alt, FLYBY_ORBIT_ALT, ease(t, 0.6))
		var pos := Vector3(
			star_center.x + cos(angle) * dev_radius,
			dev_alt,
			star_center.z + sin(angle) * dev_radius
		)
		dev_tween.tween_property(cam_ctrl, "flyby_cam_pos", pos, dev_step_time)
		var look_t: float = ease(t, 1.5)
		var look_target := Vector3(
			lerpf(dest_gate_pos.x, star_center.x, look_t),
			lerpf(5.0, 2.0, look_t),
			lerpf(dest_gate_pos.z, star_center.z, look_t)
		)
		dev_tween.parallel().tween_property(cam_ctrl, "flyby_look_at", look_target, dev_step_time)
		var up_target := current_up.slerp(Vector3.UP, t)
		if up_target.length_squared() < 0.01:
			up_target = Vector3.UP
		dev_tween.parallel().tween_property(cam_ctrl, "flyby_up", up_target, dev_step_time)
	await dev_tween.finished

	# --- Phase 2: Orbital sweep around star ---
	var orbit_steps: int = 48
	var step_time: float = actual_orbit_time / float(orbit_steps)
	var orbit_tween := create_tween()
	for i in range(orbit_steps):
		var t: float = float(i + 1) / float(orbit_steps)
		var angle: float = tangent_angle + actual_sweep * orbit_dir * t
		var alt: float
		if t < 0.5:
			alt = lerpf(FLYBY_ORBIT_ALT, FLYBY_ORBIT_ALT - 10.0, ease(t / 0.5, 0.5))
		else:
			alt = lerpf(FLYBY_ORBIT_ALT - 10.0, FLYBY_ORBIT_ALT + 10.0, ease((t - 0.5) / 0.5, 2.0))
		var radius: float
		if t < 0.5:
			radius = lerpf(FLYBY_ORBIT_RADIUS, FLYBY_ORBIT_RADIUS * 0.75, ease(t / 0.5, 0.5))
		else:
			radius = lerpf(FLYBY_ORBIT_RADIUS * 0.75, FLYBY_ORBIT_RADIUS, ease((t - 0.5) / 0.5, 2.0))
		var pos := Vector3(
			star_center.x + cos(angle) * radius,
			alt,
			star_center.z + sin(angle) * radius
		)
		orbit_tween.tween_property(cam_ctrl, "flyby_cam_pos", pos, step_time)
		orbit_tween.parallel().tween_property(cam_ctrl, "flyby_look_at",
			Vector3(star_center.x, 2.0, star_center.z), step_time)
	await orbit_tween.finished

	# --- Phase 4: Exit spiral — reverse Euler spiral from orbit back to flight altitude ---
	var orbit_exit_angle: float = tangent_angle + actual_sweep * orbit_dir
	var hero_star_off := hero_pos - star_center
	hero_star_off.y = 0.0
	var depart_angle: float
	if hero_star_off.length() < 5.0:
		depart_angle = orbit_exit_angle + PI * orbit_dir
	else:
		depart_angle = atan2(hero_star_off.z, hero_star_off.x)
	var exit_settle_radius: float = 100.0
	var exit_settle_alt: float = 80.0

	var exit_steps: int = 24
	var exit_step_time: float = FLYBY_CURVE_OFF_TIME / float(exit_steps)
	var exit_tween := create_tween()
	for i in range(exit_steps):
		var t: float = float(i + 1) / float(exit_steps)
		var rev_t: float = 1.0 - ease(1.0 - t, 2.0)
		var angle: float = lerp_angle(orbit_exit_angle, depart_angle, rev_t)
		var exit_radius: float = lerpf(FLYBY_ORBIT_RADIUS, exit_settle_radius, ease(t, 0.8))
		var exit_alt: float = lerpf(FLYBY_ORBIT_ALT, exit_settle_alt, ease(t, 0.6))
		var pos := Vector3(
			star_center.x + cos(angle) * exit_radius,
			exit_alt,
			star_center.z + sin(angle) * exit_radius
		)
		exit_tween.tween_property(cam_ctrl, "flyby_cam_pos", pos, exit_step_time)
		var look_t: float = ease(t, 1.5)
		var look_target := Vector3(
			lerpf(star_center.x, hero_pos.x, look_t),
			lerpf(2.0, 0.0, look_t),
			lerpf(star_center.z, hero_pos.z, look_t)
		)
		exit_tween.parallel().tween_property(cam_ctrl, "flyby_look_at", look_target, exit_step_time)
		var up_target: Vector3 = Vector3.UP.slerp(Vector3.BACK, t)
		if up_target.length_squared() < 0.01:
			up_target = Vector3.BACK
		exit_tween.parallel().tween_property(cam_ctrl, "flyby_up", up_target, exit_step_time)
	await exit_tween.finished

	# --- Phase 5: Handoff — flyby → flight mode with critically-damped spring settle ---
	cam_ctrl.flyby_active = false
	if cam_ctrl.has_method("notify_flyby_arrival_v0"):
		cam_ctrl.call("notify_flyby_arrival_v0", 80.0)
	else:
		cam_ctrl._altitude = 80.0
		cam_ctrl._galaxy_map_pan_offset = Vector3.ZERO
	cam_ctrl.input_locked = false
	var gv_cleanup = _find_galaxy_view()
	if gv_cleanup and gv_cleanup.has_method("SetTransitModeV0"):
		gv_cleanup.call("SetTransitModeV0", false, "", "")
	print("UUIR|FLYBY_END|hero=%s" % str(hero_pos))

func _create_transit_marker_v0(pos: Vector3) -> Node3D:
	# Invisible anchor node for warp tunnel VFX — sphere removed (was placeholder).
	var marker := Node3D.new()
	get_tree().root.add_child(marker)
	marker.global_position = pos
	return marker

## Create a bright glowing line between origin and destination stars during transit.
func _create_transit_lane_line_v0(from_pos: Vector3, to_pos: Vector3) -> MeshInstance3D:
	var line := MeshInstance3D.new()
	var mid := (from_pos + to_pos) / 2.0
	var dist := from_pos.distance_to(to_pos)
	var dir := (to_pos - from_pos).normalized()

	# Cylinder oriented along the lane path.
	var cyl := CylinderMesh.new()
	cyl.top_radius = 2.0
	cyl.bottom_radius = 2.0
	cyl.height = dist
	cyl.radial_segments = 8
	cyl.rings = 1
	line.mesh = cyl

	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.3, 0.5, 1.0, 0.6)
	mat.emission_enabled = true
	mat.emission = Color(0.3, 0.5, 1.0)
	mat.emission_energy_multiplier = 3.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	line.material_override = mat

	get_tree().root.add_child(line)
	line.global_position = mid
	# Rotate cylinder to point from origin to dest.
	# CylinderMesh is aligned along Y by default; we need to rotate it to align with dir.
	if dir != Vector3.UP and dir != Vector3.DOWN:
		line.look_at(line.global_position + dir, Vector3.UP)
		line.rotate_object_local(Vector3.RIGHT, PI / 2.0)
	else:
		# Lane is vertical (unlikely but safe).
		line.rotation = Vector3.ZERO
	return line

func _find_hero_trade_menu():
	if _hero_trade_menu and is_instance_valid(_hero_trade_menu):
		return _hero_trade_menu
	# Autoload GameManager can't find scene-child UI in _ready(); lazy-find here.
	_hero_trade_menu = get_tree().root.find_child("HeroTradeMenu", true, false)
	return _hero_trade_menu

func _open_station_menu_v0(target: Node):
	if _station_menu == null:
		return
	if not _station_menu.has_method("OnShopToggled"):
		return
	_station_menu.call("OnShopToggled", true, target)

func _dock_target_kind_token_v0(target: Node) -> String:
	# Priority: explicit meta token, then stable group-based inference, then UNKNOWN.
	if target.has_meta("dock_target_kind"):
		var v = str(target.get_meta("dock_target_kind"))
		if v != "":
			return v

	if target.is_in_group("Station"):
		return "STATION"
	if target.is_in_group("Planet"):
		return "PLANET"
	if target.is_in_group("DiscoverySite"):
		return "DISCOVERY_SITE"

	return "UNKNOWN"

func _dock_target_id_v0(target: Node) -> String:
	# Priority: explicit meta id, then sim_market_id, then node name.
	if target.has_meta("dock_target_id"):
		var v = str(target.get_meta("dock_target_id"))
		if v != "":
			return v

	if target.has_meta("sim_market_id"):
		var v2 = str(target.get_meta("sim_market_id"))
		if v2 != "":
			return v2

	return str(target.name)

# GATE.S1.AUDIO.SFX_CORE.001: Load pre-baked WAV files for all SFX.
func _init_sfx_v0() -> void:
	# GATE.S7.AUDIO_WIRING.BUS_WIRE.001: assign all SFX to correct buses.
	_sfx_turret_fire = AudioStreamPlayer.new()
	_sfx_turret_fire.name = "SfxTurretFire"
	_sfx_turret_fire.bus = &"SFX"
	_sfx_turret_fire.stream = load("res://assets/audio/laser_fire.wav")
	_sfx_turret_fire.volume_db = -12.0
	add_child(_sfx_turret_fire)

	_sfx_bullet_hit = AudioStreamPlayer.new()
	_sfx_bullet_hit.name = "SfxBulletHit"
	_sfx_bullet_hit.bus = &"SFX"
	_sfx_bullet_hit.stream = load("res://assets/audio/bullet_hit.wav")
	_sfx_bullet_hit.volume_db = -10.0
	add_child(_sfx_bullet_hit)

	_sfx_explosion = AudioStreamPlayer.new()
	_sfx_explosion.name = "SfxExplosion"
	_sfx_explosion.bus = &"SFX"
	_sfx_explosion.stream = load("res://assets/audio/explosion.wav")
	_sfx_explosion.volume_db = -6.0
	add_child(_sfx_explosion)

	# Engine thrust handled by EngineAudio node on player — no duplicate here
	_sfx_engine_thrust = null

	# GATE.S1.AUDIO.AMBIENT.001: ambient drone + event chimes
	_sfx_ambient_drone = AudioStreamPlayer.new()
	_sfx_ambient_drone.name = "SfxAmbientDrone"
	_sfx_ambient_drone.bus = &"Ambient"
	_sfx_ambient_drone.stream = load("res://assets/audio/ambient_drone.wav")
	_sfx_ambient_drone.volume_db = -18.0
	add_child(_sfx_ambient_drone)
	# Defer drone start until intro finishes — prevents audio leak during galaxy cinematic.
	if not intro_active:
		_sfx_ambient_drone.play()

	_sfx_warp_whoosh = AudioStreamPlayer.new()
	_sfx_warp_whoosh.name = "SfxWarpWhoosh"
	_sfx_warp_whoosh.bus = &"SFX"
	_sfx_warp_whoosh.stream = load("res://assets/audio/warp_whoosh.wav")
	_sfx_warp_whoosh.volume_db = -12.0
	add_child(_sfx_warp_whoosh)

	_sfx_dock_chime = AudioStreamPlayer.new()
	_sfx_dock_chime.name = "SfxDockChime"
	_sfx_dock_chime.bus = &"UI"
	_sfx_dock_chime.stream = load("res://assets/audio/dock_chime.wav")
	_sfx_dock_chime.volume_db = -6.0
	add_child(_sfx_dock_chime)

	# System arrival stinger — calm ambient snippet for first-visit reveals.
	_sfx_system_arrival = AudioStreamPlayer.new()
	_sfx_system_arrival.name = "SfxSystemArrival"
	_sfx_system_arrival.bus = &"Music"
	_sfx_system_arrival.stream = load("res://assets/audio/music/calm_ambient_01.ogg")
	_sfx_system_arrival.volume_db = -8.0
	add_child(_sfx_system_arrival)

# GATE.S1.AUDIO.SFX_CORE.001: public SFX methods for bullet.gd.
func play_hit_sfx_v0() -> void:
	if _sfx_bullet_hit:
		_sfx_bullet_hit.play()

func play_explosion_sfx_v0() -> void:
	if _sfx_explosion:
		_sfx_explosion.play()

# GATE.S7.MAIN_MENU.PAUSE.001: toggle pause state with overlay menu.
func _toggle_pause_v0() -> void:
	if _player_dead:
		return
	_paused = not _paused
	get_tree().paused = _paused
	if _paused and galaxy_overlay_open:
		toggle_galaxy_map_overlay_v0()
	# Lazy-create the pause menu overlay on first use.
	if _pause_menu == null:
		_pause_menu = PauseMenu.new()
		_pause_menu.name = "PauseMenuOverlay"
		_pause_menu.set_anchors_preset(Control.PRESET_FULL_RECT)
		add_child(_pause_menu)
		_pause_menu.resumed.connect(_on_pause_menu_resume)
	if _paused:
		_pause_menu.open_v0()
	else:
		_pause_menu.close_v0()
	print("UUIR|PAUSE|" + str(_paused))

# GATE.S7.MAIN_MENU.PAUSE.001: Resume callback from pause menu overlay.
func _on_pause_menu_resume() -> void:
	if _paused:
		_toggle_pause_v0()

# GATE.S11.GAME_FEEL.TOAST_EVENTS.001: Poll bridge for research/travel state changes and show toasts.
func _poll_toast_events_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr == null:
		return

	# Research completion detection
	if bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = bridge.call("GetResearchStatusV0")
		var is_researching: bool = status.get("researching", false)
		var tech_id: String = str(status.get("tech_id", ""))

		# Detect research completion: was researching, now not (or tech changed)
		if _last_research_active and not is_researching and not _last_research_tech_id.is_empty():
			toast_mgr.call("show_toast", "Research complete: %s!" % _last_research_tech_id, 5.0)
		# Detect new research started
		elif not _last_research_active and is_researching and not tech_id.is_empty():
			toast_mgr.call("show_toast", "Researching: %s" % tech_id, 3.0)

		_last_research_active = is_researching
		_last_research_tech_id = tech_id

	# Node arrival detection (player moved to a new system)
	if bridge.has_method("GetPlayerSnapshot"):
		var snap: Dictionary = bridge.call("GetPlayerSnapshot")
		var node_id: String = str(snap.get("location", ""))
		if not node_id.is_empty() and node_id != _last_player_node_id and not _last_player_node_id.is_empty():
			var display_name: String = node_id
			if bridge.has_method("GetNodeDisplayNameV0"):
				display_name = str(bridge.call("GetNodeDisplayNameV0", node_id))
			# FEEL_POST_FIX_9: Strip parenthesized production tags from arrival toast.
			var _pti: int = display_name.find("(")
			if _pti > 0:
				display_name = display_name.substr(0, _pti).strip_edges()
			toast_mgr.call("show_toast", "Arrived at %s" % display_name, 2.5)
		_last_player_node_id = node_id

	# GATE.S7.FACTION.REP_TOAST.001: Rep change toast (threshold >= 5 points)
	if bridge.has_method("GetAllFactionsV0"):
		var factions: Array = bridge.call("GetAllFactionsV0")
		for f_var in factions:
			if typeof(f_var) != TYPE_DICTIONARY:
				continue
			var f: Dictionary = f_var
			var fid: String = str(f.get("faction_id", ""))
			var rep: int = int(f.get("reputation", 0))
			if fid.is_empty():
				continue
			if _last_rep_snapshot.has(fid):
				var prev_rep: int = int(_last_rep_snapshot[fid])
				var delta_rep: int = rep - prev_rep
				if abs(delta_rep) >= 5:
					var sign_str: String = "+" if delta_rep > 0 else ""
					var msg: String = "Reputation with %s: %s%d" % [fid, sign_str, delta_rep]
					var color: String = "#66FF66" if delta_rep > 0 else "#FF6666"
					if toast_mgr.has_method("show_toast_colored"):
						toast_mgr.call("show_toast_colored", msg, 4.0, color)
					else:
						toast_mgr.call("show_toast", msg, 4.0)
			_last_rep_snapshot[fid] = rep

	# GATE.S7.ENFORCEMENT.BRIDGE.001: Confiscation toast on new confiscation events.
	if bridge.has_method("GetConfiscationHistoryV0"):
		var history: Array = bridge.call("GetConfiscationHistoryV0")
		var count: int = history.size()
		if count > _last_confiscation_count and _last_confiscation_count > 0:
			# Show toast for the newest confiscation event.
			var latest: Dictionary = history[0] if count > 0 else {}
			var good_id: String = str(latest.get("good_id", "cargo"))
			var units: int = int(latest.get("units", 0))
			var fine: int = int(latest.get("fine_credits", 0))
			var msg: String = "CONFISCATED: %d %s seized, %d cr fine" % [units, good_id, fine]
			toast_mgr.call("show_toast", msg, 5.0)
		_last_confiscation_count = count

	# GATE.S7.INSTABILITY_EFFECTS.BRIDGE.001: Phase transition toast when node instability changes.
	if bridge.has_method("GetNodeInstabilityV0") and bridge.has_method("GetPlayerStateV0"):
		var ps_check: Dictionary = bridge.call("GetPlayerStateV0")
		var cur_node: String = str(ps_check.get("current_node_id", ""))
		if not cur_node.is_empty():
			var instab: Dictionary = bridge.call("GetNodeInstabilityV0", cur_node)
			var phase: String = str(instab.get("phase", "Stable"))
			if _last_instability_phases.has(cur_node):
				var prev_phase: String = str(_last_instability_phases[cur_node])
				if prev_phase != phase:
					toast_mgr.call("show_toast", "Instability shift: %s -> %s" % [prev_phase, phase], 4.0)
			_last_instability_phases[cur_node] = phase

# GATE.S11.GAME_FEEL.KEYBINDS.001: Toggle keybinds help overlay (H key).
func _toggle_keybinds_help_v0() -> void:
	if _keybinds_help == null:
		_keybinds_help = get_tree().root.find_child("KeybindsHelp", true, false)
	if _keybinds_help == null:
		# Lazy-create if not found (autoload or scene child)
		var script = load("res://scripts/ui/keybinds_help.gd")
		if script:
			_keybinds_help = script.new()
			_keybinds_help.name = "KeybindsHelp"
			get_tree().root.add_child(_keybinds_help)
	if _keybinds_help != null:
		_keybinds_help.visible = not _keybinds_help.visible

# GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001: Toggle combat log panel (L key).
func _toggle_combat_log_v0() -> void:
	if _combat_log_panel == null:
		_combat_log_panel = get_tree().root.find_child("CombatLogPanel", true, false)
	if _combat_log_panel == null:
		var script = load("res://scripts/ui/combat_log_panel.gd")
		if script:
			_combat_log_panel = script.new()
			_combat_log_panel.name = "CombatLogPanel"
			get_tree().root.add_child(_combat_log_panel)
	if _combat_log_panel != null:
		_combat_log_panel.visible = not _combat_log_panel.visible
		if _combat_log_panel.visible and _combat_log_panel.has_method("refresh_v0"):
			_combat_log_panel.call("refresh_v0")

# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Toggle knowledge web panel (K key).
func _toggle_knowledge_web_v0() -> void:
	# Prefer HUD-owned instance (instantiated in hud.gd _ready).
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_knowledge_web_v0"):
		hud.toggle_knowledge_web_v0()
		return
	# Fallback: find or lazy-create panel at scene root.
	if _knowledge_web_panel == null:
		_knowledge_web_panel = get_tree().root.find_child("KnowledgeWebPanel", true, false)
	if _knowledge_web_panel == null:
		var script = load("res://scripts/ui/knowledge_web_panel.gd")
		if script:
			_knowledge_web_panel = script.new()
			_knowledge_web_panel.name = "KnowledgeWebPanel"
			get_tree().root.add_child(_knowledge_web_panel)
	if _knowledge_web_panel != null and _knowledge_web_panel.has_method("toggle_v0"):
		_knowledge_web_panel.toggle_v0()

# GATE.X.UI_POLISH.MISSION_JOURNAL.001: Toggle mission journal panel (J key).
func _toggle_mission_journal_v0() -> void:
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_mission_journal_v0"):
		hud.toggle_mission_journal_v0()

# GATE.S7.UI_WARFRONT.DASHBOARD.001: Toggle warfront dashboard panel (N key).
func _toggle_warfront_dashboard_v0() -> void:
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_warfront_dashboard_v0"):
		hud.toggle_warfront_dashboard_v0()

# GATE.S8.MEGAPROJECT.UI.001: Toggle megaproject panel (M key).
func _toggle_megaproject_panel_v0(node_id: String = "") -> void:
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_megaproject_panel_v0"):
		hud.toggle_megaproject_panel_v0(node_id)

# Toggle FO panel visibility (F key).
func _toggle_fo_panel_v0() -> void:
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_fo_panel_v0"):
		hud.toggle_fo_panel_v0()

# Toggle automation dashboard (B key).
func _toggle_automation_dashboard_v0() -> void:
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_automation_dashboard_v0"):
		hud.toggle_automation_dashboard_v0()

# Toggle data log panel (D key).
func _toggle_data_log_v0() -> void:
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud != null and hud.has_method("toggle_data_log_v0"):
		hud.toggle_data_log_v0()

# GATE.S14.TRANSIT.WARP_EFFECT.001: Two-stage warp screen flash.
# Phase 1: Bright white flash (0.15s). Phase 2: Blue-shift afterglow fade (0.4s).
# Gives visible animation progression across multiple captured frames.
func _flash_warp_screen_v0() -> void:
	var canvas := CanvasLayer.new()
	canvas.layer = 100
	add_child(canvas)
	var rect := ColorRect.new()
	rect.color = Color(1.0, 1.0, 1.0, 0.6)
	rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(rect)
	var tween := create_tween()
	# Phase 1: bright white flash fades quickly
	tween.tween_property(rect, "color:a", 0.3, 0.12)
	# Phase 2: shift to blue-cyan afterglow
	tween.tween_property(rect, "color", Color(0.2, 0.5, 1.0, 0.25), 0.15)
	# Phase 3: afterglow fades out
	tween.tween_property(rect, "color:a", 0.0, 0.35)
	tween.tween_callback(canvas.queue_free)

# GATE.S15.FEEL.JUMP_EVENT_TOAST.001: Track seen jump event IDs to avoid duplicate toasts.
var _seen_jump_event_ids: Dictionary = {}

# GATE.S15.FEEL.JUMP_EVENT_TOAST.001: Show toasts for any new jump events recorded during lane transit.
func _show_jump_event_toasts_v0(bridge: Node) -> void:
	if bridge == null or not bridge.has_method("GetJumpEventsV0"):
		return
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr == null or not toast_mgr.has_method("show_toast"):
		return

	var events: Array = bridge.call("GetJumpEventsV0")
	for evt in events:
		var event_id: String = str(evt.get("event_id", ""))
		if event_id.is_empty() or _seen_jump_event_ids.has(event_id):
			continue
		_seen_jump_event_ids[event_id] = true

		var kind: String = str(evt.get("kind", "none"))
		var color: String
		var message: String
		# FEEL_POST_FIX_8: Clean player-facing event text (no "Thread" prefix).
		match kind:
			"salvage":
				color = "#22AA22"
				var good_id: String = str(evt.get("good_id", ""))
				var qty: int = int(evt.get("quantity", 0))
				message = "Salvage: found %d x %s!" % [qty, good_id]
			"signal":
				color = "#2288DD"
				message = "Anomaly detected nearby!"
			"turbulence":
				color = "#DD4444"
				var hull_dmg: int = int(evt.get("hull_damage", 0))
				message = "Turbulence: hull took %d damage!" % hull_dmg
			_:
				continue

		print("UUIR|JUMP_EVENT_TOAST|" + kind + "|" + event_id)
		if toast_mgr.has_method("show_toast_colored"):
			toast_mgr.call("show_toast_colored", message, 4.0, color)
		else:
			toast_mgr.call("show_toast", message, 4.0)

# GATE.S14.STARTER.MISSION_PROMPT.001: Replaced by GATE.S19.ONBOARD.DOCK_DISCLOSURE.009 (progressive tabs).
# Kept as no-op for headless test compat.
func _check_first_dock_mission_prompt_v0() -> void:
	pass


# GATE.S8.HAVEN.COMING_HOME.001: Haven dock cinematic — camera pullback + sweep on first dock.
func _try_haven_cinematic_v0() -> void:
	if _haven_cinematic_played:
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetHavenStatusV0"):
		return
	var status: Dictionary = bridge.call("GetHavenStatusV0")
	if not status.get("discovered", false):
		return
	var haven_node_id: String = str(status.get("node_id", ""))
	if haven_node_id.is_empty() or haven_node_id != dock_target_id:
		return

	_haven_cinematic_played = true
	print("UUIR|HAVEN_CINEMATIC|dock_at_haven")

	# Camera pullback + slow approach tween.
	var cam := get_viewport().get_camera_3d()
	if cam == null:
		return

	var original_pos := cam.global_position
	var pullback_pos := original_pos + Vector3(0, 30, 40)

	var tween := create_tween()
	# Pull back.
	tween.tween_property(cam, "global_position", pullback_pos, 1.5).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	# Hold for a beat.
	tween.tween_interval(0.8)
	# Sweep back in slowly.
	tween.tween_property(cam, "global_position", original_pos, 2.0).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_QUAD)

# GATE.S19.ONBOARD.FO_EVENTS.012: Force immediate FO dialogue poll after key game events.
# Ensures FO reacts within 0.5s instead of waiting for the 2s slow-poll cycle.
func _poll_fo_immediate() -> void:
	var fo_panel = get_node_or_null("/root/Main/HUD/FOPanel")
	if fo_panel == null:
		fo_panel = get_tree().root.find_child("FOPanel", true, false)
	if fo_panel and fo_panel.has_method("_poll_fo_dialogue"):
		# Brief delay so SimCore has processed the trigger on the next tick.
		await get_tree().create_timer(0.5).timeout
		fo_panel.call("_poll_fo_dialogue")

# GATE.S19.ONBOARD.MILESTONE_TOAST.014: Poll milestones and celebrate newly achieved ones.
func _poll_milestone_celebrations_v0() -> void:
	# GATE.S19.ONBOARD.SETTINGS_WIRE.015: Respect tutorial toggle.
	var settings_mgr = get_node_or_null("/root/SettingsManager")
	if settings_mgr and settings_mgr.has_method("get_setting"):
		if not bool(settings_mgr.call("get_setting", "gameplay_tutorial_toasts")):
			return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetMilestonesV0"):
		return
	var milestones: Array = bridge.call("GetMilestonesV0")
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr == null or not toast_mgr.has_method("show_priority_toast"):
		return
	for m in milestones:
		var mid: String = str(m.get("id", ""))
		var achieved: bool = m.get("achieved", false)
		if mid.is_empty() or not achieved:
			continue
		if _celebrated_milestones.has(mid):
			continue
		_celebrated_milestones[mid] = true
		var mname: String = str(m.get("name", mid))
		toast_mgr.call("show_priority_toast", "Milestone: " + mname, "milestone")
		print("UUIR|MILESTONE_CELEBRATE|" + mid)
		# GATE.S9.STEAM.ACHIEVEMENTS.001: Unlock Steam achievement on milestone.
		_unlock_steam_achievement_v0(mid)

# Captain's Guide: fire a one-shot guide toast. Returns true if fired.
func _fire_guide_hint_v0(step_id: String, text: String) -> bool:
	if _guide_steps_seen.has(step_id):
		return false
	# Respect tutorial toggle.
	var settings_mgr = get_node_or_null("/root/SettingsManager")
	if settings_mgr and settings_mgr.has_method("get_setting"):
		if not bool(settings_mgr.call("get_setting", "gameplay_tutorial_toasts")):
			return false
	_guide_steps_seen[step_id] = true
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", text, "guide")
	print("UUIR|GUIDE_HINT|" + step_id)
	return true

# Captain's Guide: polled hints — check game state and fire contextual guide toasts.
# Suppressed during FO-voiced tutorial (tutorial director handles all guidance).
func _poll_guide_hints_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetOnboardingStateV0"):
		return
	# Skip polled hints while tutorial is active — tutorial director handles all guidance.
	if bridge.has_method("IsTutorialActiveV0") and bool(bridge.call("IsTutorialActiveV0")):
		return
	var os: Dictionary = bridge.call("GetOnboardingStateV0")
	if os.is_empty():
		return

	var nodes_visited: int = int(os.get("nodes_visited", 0))
	var has_traded: bool = bool(os.get("has_traded", false))
	var has_fought: bool = bool(os.get("has_fought", false))

	# Phase 1: Automation bridge — after 2+ goods traded, hint toward programs.
	if has_traded:
		_fire_guide_hint_v0("GUIDE_AUTOMATE", "Programs run trades automatically. Set up a route once — it profits while you explore.")

	# Phase 2: World systems.
	if nodes_visited >= 2:
		_fire_guide_hint_v0("GUIDE_MAP", "Press M for the Galaxy Map. Each system produces different goods — plan your routes.")

	if bool(os.get("show_faction_hud", false)):
		_fire_guide_hint_v0("GUIDE_FACTION", "Faction territory. They set tariffs on trade and guard their technology. Earn reputation to unlock both.")

	if has_fought:
		_fire_guide_hint_v0("GUIDE_COMBAT", "Shields absorb first, then zone armor, then hull. Weapons generate heat — manage it.")

	if bool(os.get("show_ship_tab", false)):
		_fire_guide_hint_v0("GUIDE_SHIP_TAB", "Ship tab unlocked. Install modules to improve weapons, engines, shields, and cargo.")

	if bool(os.get("show_jobs_tab", false)):
		_fire_guide_hint_v0("GUIDE_JOBS", "Missions pay credits and build faction reputation. Check the Jobs tab.")

	# FO promotion window check.
	if bridge.has_method("IsInFOPromotionWindowV0"):
		if bool(bridge.call("IsInFOPromotionWindowV0")):
			_fire_guide_hint_v0("GUIDE_FO_PROMO", "A crew member wants to serve as First Officer. Open the FO panel to choose.")

	# Phase 3: Depth systems.
	if nodes_visited >= 5:
		_fire_guide_hint_v0("GUIDE_RESEARCH", "Research unlocks better modules. Factions guard their best tech — earn trust first.")

	if nodes_visited >= 7:
		_fire_guide_hint_v0("GUIDE_SUPPLY_CHAIN", "Every faction needs what another produces. The dependency is galaxy-wide — and wars break the chain.")

	if bool(os.get("show_station_tab", false)):
		_fire_guide_hint_v0("GUIDE_STATION_TAB", "Station tab shows local industry, faction presence, and production chains.")

	# Warfront hints: check if player is at a warfront node.
	if bridge.has_method("GetWarfrontStatusV0"):
		var wf_status: Dictionary = bridge.call("GetWarfrontStatusV0")
		if not wf_status.is_empty() and bool(wf_status.get("at_warfront", false)):
			_fire_guide_hint_v0("GUIDE_WARFRONT", "Active warzone. Munitions demand surges here — prices spike. Profit or peril, Captain.")
			if has_traded:
				_fire_guide_hint_v0("GUIDE_WAR_ECONOMY", "Wars reshape the economy. Contested stations pay premiums for fuel and munitions. Supply the front — or avoid it.")

	# Instability hint.
	if bridge.has_method("GetNodeInstabilityV0") and bridge.has_method("GetPlayerStateV0"):
		var ps_hint: Dictionary = bridge.call("GetPlayerStateV0")
		var hint_node: String = str(ps_hint.get("current_node_id", ""))
		if not hint_node.is_empty():
			var instab_d: Dictionary = bridge.call("GetNodeInstabilityV0", hint_node)
			var instab_level: float = float(instab_d.get("level", 0))
			if instab_level > 25.0:
				_fire_guide_hint_v0("GUIDE_INSTABILITY", "Spacetime is unstable here. Prices fluctuate. Travel times shift. Tread carefully.")

	# Phase 4: Late game.
	if nodes_visited >= 10:
		_fire_guide_hint_v0("GUIDE_FRACTURE", "Your drive reaches where threads don't. The void holds salvage, ruins, and answers.")

	# Pentagon insight — check faction reputation count.
	if bridge.has_method("GetFactionReputationsV0"):
		var reps: Dictionary = bridge.call("GetFactionReputationsV0")
		var positive_count: int = 0
		for val in reps.values():
			if int(val) > 0:
				positive_count += 1
		if positive_count >= 3:
			_fire_guide_hint_v0("GUIDE_PENTAGON", "Five factions. Each needs what another produces. The dependency was engineered. Wars break the chain — and create opportunity.")

	# Diplomacy hint.
	if bridge.has_method("IsDiplomacyAvailableV0"):
		if bool(bridge.call("IsDiplomacyAvailableV0")):
			_fire_guide_hint_v0("GUIDE_DIPLOMACY", "Diplomacy shapes faction relationships. Propose treaties, set bounties, broker peace — or profit from the chaos.")

	# Megaproject hint.
	if bridge.has_method("IsMegaprojectAvailableV0"):
		if bool(bridge.call("IsMegaprojectAvailableV0")):
			_fire_guide_hint_v0("GUIDE_MEGAPROJECT", "Megaprojects reshape the galaxy. They require massive resources and define your legacy.")

	# Endgame hint.
	if bridge.has_method("GetStoryActV0"):
		if int(bridge.call("GetStoryActV0")) >= 2:
			_fire_guide_hint_v0("GUIDE_ENDGAME", "Three paths: reinforce what exists, build something new, or renegotiate reality itself.")

# GATE.S5.TRACTOR.VFX.001: Auto-collect nearby loot drops and spawn tractor beam VFX.
func _poll_auto_loot_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return
	if not bridge.has_method("GetNearbyLootV0") or not bridge.has_method("DispatchCollectLootV0"):
		return

	var drops: Array = bridge.call("GetNearbyLootV0")
	if drops.is_empty():
		return

	_ensure_hero_body()
	var ship_pos := Vector3.ZERO
	if _hero_body and is_instance_valid(_hero_body):
		ship_pos = _hero_body.global_position

	for drop in drops:
		var drop_id: String = str(drop.get("drop_id", ""))
		if drop_id.is_empty():
			continue

		var result: Dictionary = bridge.call("DispatchCollectLootV0", drop_id)
		var success: bool = result.get("success", false)
		if not success:
			continue

		# Determine loot marker world position for the beam target.
		# Loot markers are children of GalaxyView named "LootMarker_<dropId>".
		var target_pos := ship_pos + Vector3(0, 2, 0)  # Fallback: slightly above ship.
		if _galaxy_view and is_instance_valid(_galaxy_view):
			var marker = _galaxy_view.find_child("LootMarker_" + drop_id, true, false)
			if marker and is_instance_valid(marker):
				target_pos = marker.global_position

		# Spawn tractor beam VFX from ship to loot position.
		TractorBeam.spawn(get_tree().current_scene, ship_pos, target_pos)

		var credits: int = int(result.get("credits_gained", 0))
		var goods: int = int(result.get("goods_gained", 0))
		print("UUIR|LOOT_COLLECTED|drop=%s credits=%d goods=%d" % [drop_id, credits, goods])

		# Show toast for collected loot.
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast_colored"):
			var msg := "Loot collected!"
			if credits > 0:
				msg = "Collected %d credits!" % credits
			elif goods > 0:
				msg = "Salvage collected!"
			toast_mgr.call("show_toast_colored", msg, 2.5, "#22CCFF")

# GATE.S6.OUTCOME.CELEBRATION.001: Poll bridge for discovery transitions to Analyzed phase.
func _poll_discovery_celebrations_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetDiscoverySnapshotV0"):
		return
	var node_id: String = _last_player_node_id
	if node_id.is_empty():
		return

	var snap = bridge.call("GetDiscoverySnapshotV0", node_id)
	if typeof(snap) != TYPE_DICTIONARY:
		return

	var discoveries = snap.get("discoveries", [])
	if typeof(discoveries) != TYPE_ARRAY:
		return

	var toast_mgr = get_node_or_null("/root/ToastManager")

	for disc in discoveries:
		if typeof(disc) != TYPE_DICTIONARY:
			continue
		var disc_id: String = str(disc.get("discovery_id", ""))
		if disc_id.is_empty():
			continue
		var current_status: String = str(disc.get("status", ""))
		var prev_status: String = str(_prev_discovery_statuses.get(disc_id, ""))
		var credit_reward: int = int(disc.get("credit_reward", 0))

		# Detect transition to Analyzed from a prior non-Analyzed state
		if current_status == "Analyzed" and prev_status != "Analyzed" and not prev_status.is_empty():
			print("CELEBRATION|DISCOVERY_COMPLETE|" + disc_id)
			if toast_mgr and toast_mgr.has_method("show_toast"):
				var msg: String
				if credit_reward > 0:
					msg = "Discovery Complete: %s — +%d credits" % [disc_id, credit_reward]
				else:
					msg = "Discovery Complete: %s" % disc_id
				if toast_mgr.has_method("show_toast_colored"):
					toast_mgr.call("show_toast_colored", msg, 6.0, "#FFD700")
				else:
					toast_mgr.call("show_toast", msg, 6.0)

		_prev_discovery_statuses[disc_id] = current_status

# GATE.S9.STEAM.SDK.001: Initialize Steam with graceful fallback.
func _init_steam_v0():
	if not ClassDB.class_exists(&"Steam"):
		print("[Steam] GodotSteam not available — running without Steam integration.")
		_steam_enabled = false
		return

	var steam = Engine.get_singleton("Steam")
	if steam == null:
		print("[Steam] Steam singleton not found — running without Steam.")
		_steam_enabled = false
		return

	var init_result = steam.steamInitEx(false, 480)
	if init_result["status"] != 0:
		print("[Steam] Steam init failed (status %d) — running without Steam." % init_result["status"])
		_steam_enabled = false
		return

	_steam_enabled = true
	print("[Steam] Steam initialized. User: %s" % steam.getPersonaName())

func is_steam_enabled() -> bool:
	return _steam_enabled

# GATE.S9.STEAM.ACHIEVEMENTS.001: Milestone → Steam achievement mapping.
# Achievement API names match milestone IDs (configured in Steamworks dashboard).
func _unlock_steam_achievement_v0(milestone_id: String) -> void:
	if not _steam_enabled:
		return
	var steam = Engine.get_singleton("Steam")
	if steam == null:
		return
	# Steam achievement API name = milestone ID (e.g., "first_trade", "explorer_5").
	steam.setAchievement(milestone_id)
	steam.storeStats()
	print("[Steam] Achievement unlocked: %s" % milestone_id)
