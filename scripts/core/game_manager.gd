extends Node

# sim.gd (GDScript sim) is kept for galaxy_spawner 3D visual scaffolding ONLY.
# It must NOT be ticked or used for game logic. All game logic routes through SimBridge (C#).
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')
const WarpTunnel = preload('res://scripts/vfx/warp_tunnel.gd')
const GateVortex = preload('res://scripts/vfx/gate_vortex.gd')
const GateTransitPopup = preload('res://scripts/ui/gate_transit_popup.gd')


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

# GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001: fleet targeting
var _targeted_fleet_id: String = ""

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: player death state
var _player_dead: bool = false

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

# GATE.S1.SAVE_UI.PAUSE_MENU.001: Pause state
var _paused: bool = false

# GATE.S12.UX_POLISH.ONBOARDING.001: First-dock onboarding toasts
var _onboarding_shown: bool = false

# GATE.S11.GAME_FEEL.TOAST_EVENTS.001: Toast event polling state
var _toast_poll_timer: float = 0.0
const TOAST_POLL_INTERVAL: float = 2.0
var _last_research_tech_id: String = ""
var _last_research_active: bool = false
var _last_player_node_id: String = ""

# GATE.S6.OUTCOME.CELEBRATION.001: Discovery completion celebration polling
var _discovery_poll_timer: float = 0.0
const DISCOVERY_POLL_INTERVAL: float = 2.0
var _prev_discovery_statuses: Dictionary = {}  # discovery_id -> last known status
var _lane_origin_node_id: String = ""  # GATE.S13.WORLD.GATE_ARRIVAL.001: track origin for gate positioning
var _lane_dest_node_id: String = ""    # Transit destination — read by camera for transit-mode rendering.
var _cinematic_transit_active: bool = false  # Blocks Esc during approach cinematic.
var _last_arrival_first_visit: bool = false  # Read by camera for first-visit vista hold.

# Warp transit camera state — read by player_follow_camera during IN_LANE_TRANSIT.
var warp_transit_target: Vector3 = Vector3.ZERO
var warp_transit_altitude: float = 2000.0
var warp_transit_dest_pos: Vector3 = Vector3.ZERO
var warp_transit_travel_dir: Vector3 = Vector3.ZERO
var _transit_marker: Node3D = null
var _transit_lane_line_ref: Node3D = null  # Stored to clean up during reveal.

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

# GATE.S11.GAME_FEEL.KEYBINDS.001: Help overlay (H key)
var _keybinds_help: Node = null

# GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001: Combat log panel (L key)
var _combat_log_panel: Node = null

func _ready():
	print('SUCCESS: Global Game Manager initialized.')
	sim = Sim.new()
	player = PlayerState.new()

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

	# GATE.S12.UX_POLISH.ONBOARDING.001: Show welcome toasts on game start (delayed).
	_show_onboarding_toasts_deferred_v0()

	# Edgedar: screen-edge direction indicators for off-screen POIs.
	var edgedar_script = load("res://scripts/ui/edgedar_overlay.gd")
	if edgedar_script:
		var edgedar = edgedar_script.new()
		edgedar.name = "EdgedarOverlay"
		get_tree().root.call_deferred("add_child", edgedar)

func _show_onboarding_toasts_deferred_v0() -> void:
	# Only the autoload instance shows onboarding (scene-child returns).
	if get_parent() != get_tree().root:
		return
	if _onboarding_shown:
		return
	_onboarding_shown = true
	# Delay so the screen has loaded and toasts are visible.
	await get_tree().create_timer(2.0).timeout
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_toast"):
		toast_mgr.call("show_toast", "Welcome, Captain! Fly to a station to trade.", 6.0)
		toast_mgr.call("show_toast", "Tab = Galaxy Map, E = Empire, H = Help", 8.0)

func _process(delta):
	# Local ticking must continue while overlay is open. This is used only as a boolean check in tests.
	time_accumulator += float(delta)

	# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: freeze all game logic when dead
	if _player_dead:
		return

	if _lane_cooldown_v0 > 0.0:
		_lane_cooldown_v0 -= float(delta)
	if _turret_cooldown > 0.0:
		_turret_cooldown -= float(delta)
	if _ai_fire_cooldown > 0.0:
		_ai_fire_cooldown -= float(delta)
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

	# G key hold-to-fire: auto-fires turrets at highest rate while held (not during galaxy map)
	if current_player_state == PlayerShipState.IN_FLIGHT and not galaxy_overlay_open and Input.is_key_pressed(KEY_G):
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


func _unhandled_input(event):
	# Only the autoload instance handles input (avoid dual GameManager double-fire).
	if get_parent() != get_tree().root:
		return
	# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: R restarts scene; all other input frozen when dead
	if event is InputEventKey and event.pressed and not event.echo:
		if _player_dead:
			if event.keycode == KEY_R:
				get_tree().reload_current_scene()
			return
		# Gate approach: Enter/Space to confirm, Esc to cancel.
		# Block input during approach cinematic (vortex + zoom-in playing).
		if _cinematic_transit_active:
			return
		if current_player_state == PlayerShipState.GATE_APPROACH:
			if event.keycode == KEY_ENTER or event.keycode == KEY_KP_ENTER or event.keycode == KEY_SPACE:
				_confirm_gate_transit_v0()
				get_viewport().set_input_as_handled()
				return
			if event.keycode == KEY_ESCAPE:
				_cancel_gate_approach_v0()
				get_viewport().set_input_as_handled()
				return
		if event.keycode == KEY_ESCAPE:
			_toggle_pause_v0()
			return
		if event.keycode == KEY_TAB and current_player_state != PlayerShipState.IN_LANE_TRANSIT:
			toggle_galaxy_map_overlay_v0()
		if event.keycode == KEY_E:
			_toggle_empire_dashboard_v0()
		if event.keycode == KEY_H:
			_toggle_keybinds_help_v0()
		if event.keycode == KEY_L:
			_toggle_combat_log_v0()
		if event.keycode == KEY_V:
			_cycle_data_overlay_v0()

# GATE.S10.EMPIRE.SHELL.001: Toggle empire dashboard panel (created by SimBridge in C#).
func _toggle_empire_dashboard_v0():
	if _empire_dashboard == null:
		_empire_dashboard = get_tree().root.find_child("EmpireDashboard", true, false)
	if _empire_dashboard != null:
		_empire_dashboard.visible = not _empire_dashboard.visible
		# Hide HUD status elements when dashboard is open.
		var hud_ed = get_tree().root.find_child("HUD", true, false)
		if hud_ed and hud_ed.has_method("set_overlay_mode_v0"):
			hud_ed.call("set_overlay_mode_v0", _empire_dashboard.visible)

func toggle_market():
	# No-op stub. Station UI is driven by C# StationMenu via SimBridge.
	return


func toggle_galaxy_map_overlay_v0():
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
	if not _galaxy_view:
		_galaxy_view = get_tree().root.find_child("GalaxyView", true, false)
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

func on_proximity_dock_entered_v0(target: Node):
	if target == null:
		return

	# Don't auto-dock while click-to-fly autopilot is active.
	if _hero_body and is_instance_valid(_hero_body) and _hero_body.get("_nav_active"):
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
	# GATE.S14.STARTER.MISSION_PROMPT.001: first-dock jobs toast
	_check_first_dock_mission_prompt_v0()

	if dock_target_kind_token == "DISCOVERY_SITE":
		# Scan flow wiring can be added later without changing the state machine surface.
		print("UUIR|SCAN_FLOW_OPEN|" + dock_target_id)

func undock_v0():
	if current_player_state != PlayerShipState.DOCKED:
		return

	# Deterministic token for headless assertions (no timestamps).
	print("UUIR|UNDOCK|" + dock_target_kind_token + "|" + dock_target_id)

	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	dock_target_kind_token = ""
	dock_target_id = ""

	# Close station menu if it is open.
	if _station_menu and _station_menu.has_method("OnShopToggled"):
		_station_menu.call("OnShopToggled", false, "")
	var htm = _find_hero_trade_menu()
	if htm and htm.has_method("close_market_v0"):
		htm.call("close_market_v0")
	# Close discovery panel if it is open.
	if _discovery_panel and _discovery_panel.has_method("close_v0"):
		_discovery_panel.call("close_v0")

func on_lane_gate_proximity_entered_v0(neighbor_node_id: String) -> void:
	if _lane_cooldown_v0 > 0.0:
		return
	if not _transition_player_state_v0(PlayerShipState.IN_LANE_TRANSIT):
		return

	print("UUIR|LANE_ENTER|" + neighbor_node_id)
	# GATE.S13.WORLD.GATE_ARRIVAL.001: Remember origin for arrival positioning.
	_lane_origin_node_id = _last_player_node_id

	# GATE.S14.TRANSIT.WARP_EFFECT.001: screen flash + camera shake on warp entry
	_apply_camera_shake_v0(0.4)
	_flash_warp_screen_v0()

	# GATE.S1.AUDIO.AMBIENT.001: warp whoosh on lane jump
	if _sfx_warp_whoosh:
		_sfx_warp_whoosh.play()

	var bridge = get_node_or_null("/root/SimBridge")
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

# ── Gate approach flow (stop-and-confirm) ──
# Called by GalaxyView on the autoload GameManager (owns _unhandled_input).
func on_lane_gate_approach_entered_v0(neighbor_node_id: String) -> void:
	if _lane_cooldown_v0 > 0.0:
		return
	if not _transition_player_state_v0(PlayerShipState.GATE_APPROACH):
		return
	_approach_neighbor_id = neighbor_node_id
	print("UUIR|GATE_APPROACH|" + neighbor_node_id)

	# Auto-decelerate ship on approach.
	_ensure_hero_body()
	if _hero_body and is_instance_valid(_hero_body):
		_hero_body.linear_velocity = Vector3.ZERO
		_hero_body.angular_velocity = Vector3.ZERO

	# Show transit confirmation popup.
	_show_gate_popup_v0(neighbor_node_id)

	# Pre-build galaxy overlay so transit animation starts without a hitch.
	var gv = _find_galaxy_view()
	if gv and gv.has_method("PrewarmOverlayV0"):
		gv.call("PrewarmOverlayV0")

# Called by GalaxyView when player exits approach zone.
func on_lane_gate_approach_exited_v0() -> void:
	if current_player_state != PlayerShipState.GATE_APPROACH:
		return
	_cancel_gate_approach_v0()

func _cancel_gate_approach_v0() -> void:
	print("UUIR|GATE_APPROACH_CANCEL")
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

	# Close popup.
	if _gate_popup and is_instance_valid(_gate_popup):
		_gate_popup.queue_free()
		_gate_popup = null

	# Save origin + dest BEFORE clearing approach_neighbor_id.
	_lane_origin_node_id = _last_player_node_id
	_lane_dest_node_id = neighbor_id
	print("UUIR|LANE_ENTER|" + neighbor_id)

	# Dispatch travel command (deducts fuel + toll in SimCore).
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchTravelCommandV0"):
		bridge.call("DispatchTravelCommandV0", PLAYER_FLEET_ID, neighbor_id)

	# === Phase 1: Approach cinematic ===
	# Stay in GATE_APPROACH so camera remains in FLIGHT mode (close to ship).
	_cinematic_transit_active = true
	var cam_ctrl = _find_camera_controller()
	if cam_ctrl:
		# Pre-save the real flight altitude before zoom changes it.
		cam_ctrl.set("_pre_transit_altitude", cam_ctrl.get("_altitude"))
		cam_ctrl.set("_pre_transit_altitude_set", true)
		cam_ctrl.cinematic_active = true
		# Zoom camera IN to gate close-up (anticipation).
		if cam_ctrl.has_method("tween_altitude_v0"):
			cam_ctrl.call("tween_altitude_v0", 25.0, 1.0)

	# Camera shake + warp sound.
	_apply_camera_shake_v0(0.4)
	if _sfx_warp_whoosh:
		_sfx_warp_whoosh.play()

	# Vortex pulls ship toward gate over 1.5s (uses _approach_neighbor_id for gate pos).
	await _play_departure_vortex_v0()

	# Clear approach_neighbor_id AFTER vortex used it (fixes gate position bug).
	_approach_neighbor_id = ""

	# Brief hold at gate close-up.
	await get_tree().create_timer(0.3).timeout

	# === Phase 2: Launch ===
	_apply_camera_shake_v0(0.6)
	_flash_warp_screen_v0()

	# NOW transition to IN_LANE_TRANSIT → camera switches to WARP_TRANSIT.
	if not _transition_player_state_v0(PlayerShipState.IN_LANE_TRANSIT):
		_cinematic_transit_active = false
		if cam_ctrl:
			cam_ctrl.cinematic_active = false
		return

	_cinematic_transit_active = false
	if cam_ctrl:
		cam_ctrl.cinematic_active = false  # Transit mode handles camera itself.

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
	charge_tween.tween_property(_hero_body, "global_position", gate_pos, 1.5) \
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

func on_lane_arrival_v0(arrived_node_id: String) -> void:
	if current_player_state != PlayerShipState.IN_LANE_TRANSIT:
		return

	print("UUIR|LANE_EXIT|" + arrived_node_id)
	# GATE.S14.TRANSIT.WARP_EFFECT.001: arrival rumble
	_apply_camera_shake_v0(0.25)

	var bridge = get_node_or_null("/root/SimBridge")

	# Check first-visit BEFORE dispatch (dispatch marks it as visited).
	var is_first_visit: bool = false
	if bridge and bridge.has_method("IsFirstVisitV0"):
		is_first_visit = bridge.call("IsFirstVisitV0", arrived_node_id)
	# Store for camera to read during arrival zoom.
	_last_arrival_first_visit = is_first_visit

	if bridge and bridge.has_method("DispatchPlayerArriveV0"):
		bridge.call("DispatchPlayerArriveV0", arrived_node_id)
	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	_lane_cooldown_v0 = 2.0  # Prevent immediate re-trigger at destination lane gate.

	# Restore local system labels (suppressed during transit).
	var gv_labels = _find_galaxy_view()
	if gv_labels and gv_labels.has_method("SetLocalLabelsVisibleV0"):
		gv_labels.call("SetLocalLabelsVisibleV0", true)

	# GATE.S15.FEEL.JUMP_EVENT_TOAST.001: Show toasts for jump events on this transit.
	_show_jump_event_toasts_v0(bridge)

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

	# Camera arrival sweep: all arrivals get a rotation; first visits get the full cinematic.
	if is_first_visit:
		_show_system_reveal_v0(arrived_node_id, bridge)
	else:
		# Return visit: brief rotation sweep to orient the player in the system.
		var cam_ctrl = _find_camera_controller()
		if cam_ctrl and cam_ctrl.get("_flight_yaw_offset") != null:
			cam_ctrl._flight_yaw_offset = 0.0
			cam_ctrl._flight_pitch_offset = 0.0
			var sweep_tween := create_tween()
			sweep_tween.tween_property(cam_ctrl, "_flight_yaw_offset", 0.5, 2.0) \
				.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
			sweep_tween.tween_property(cam_ctrl, "_flight_yaw_offset", 0.0, 1.0) \
				.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)

func _show_system_reveal_v0(node_id: String, bridge: Node) -> void:
	var display_name: String = node_id
	if bridge and bridge.has_method("GetNodeDisplayNameV0"):
		display_name = bridge.call("GetNodeDisplayNameV0", node_id)

	# --- Get the star center for orbiting ---
	var star_center := Vector3.ZERO
	var gv = _find_galaxy_view()
	if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
		star_center = gv.call("GetCurrentStarGlobalPositionV0")

	# --- Audio: understated wonder sound for first arrival ---
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

	# --- Hide player ship + HUD + labels during cinematic ---
	var hero_hidden := false
	if _hero_body and is_instance_valid(_hero_body):
		_hero_body.visible = false
		hero_hidden = true
	# Hide HUD during cinematic.
	var hud = get_tree().root.find_child("HUD", true, false) if get_tree() else null
	if hud and hud.has_method("set_overlay_mode_v0"):
		hud.call("set_overlay_mode_v0", true)
	# Suppress labels during cinematic reveal.
	if gv and gv.has_method("SetLocalLabelsVisibleV0"):
		gv.call("SetLocalLabelsVisibleV0", false)

	# --- Camera: gentle sweep around STAR CENTER (not player) ---
	var cam_ctrl = _find_camera_controller()
	var has_cam: bool = cam_ctrl != null and cam_ctrl.get("cinematic_orbit_active") != null
	var total_cinematic_time: float = 4.0
	if has_cam:
		cam_ctrl.cinematic_active = true
		cam_ctrl._flight_yaw_offset = 0.0
		cam_ctrl._flight_pitch_offset = 0.0

		# Activate cinematic orbit mode — camera orbits star_center.
		cam_ctrl.cinematic_orbit_blend = 1.0  # Full angled orbit.
		cam_ctrl.cinematic_orbit_active = true
		cam_ctrl._current_mode = 0  # CameraMode.FLIGHT — prevent stale WARP_TRANSIT triggering post-transit code.
		cam_ctrl.cinematic_orbit_center = star_center
		# Start angle: based on hero position relative to star (approach from gate direction).
		var hero_dir := Vector3(1, 0, 0)
		if _hero_body and is_instance_valid(_hero_body):
			hero_dir = (_hero_body.global_position - star_center)
			hero_dir.y = 0.0
			if hero_dir.length() < 1.0:
				hero_dir = Vector3(1, 0, 0)
			hero_dir = hero_dir.normalized()
		cam_ctrl.cinematic_orbit_angle = atan2(hero_dir.z, hero_dir.x)
		cam_ctrl.cinematic_orbit_radius = 60.0  # Start close (transit already descended)
		cam_ctrl.cinematic_orbit_altitude = star_center.y + 120.0  # Continue from transit descent

		# Tween orbit: half sweep (PI) over 4s — "look around" not full orbit.
		var orbit_tween := create_tween()
		var start_angle: float = cam_ctrl.cinematic_orbit_angle
		orbit_tween.tween_property(cam_ctrl, "cinematic_orbit_angle", start_angle + PI, 4.0) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)

		# Altitude descent: 120 → 80 over 4s — smooth settle into the system.
		var alt_tween := create_tween()
		alt_tween.tween_property(cam_ctrl, "cinematic_orbit_altitude", star_center.y + 80.0, 4.0) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)

		# Radius: 60 → 45 — gently pull in for intimate view.
		var radius_tween := create_tween()
		radius_tween.tween_property(cam_ctrl, "cinematic_orbit_radius", 45.0, 4.0) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)

	# --- Cinematic overlay: letterbox + system name ---
	var canvas := CanvasLayer.new()
	canvas.layer = 90
	add_child(canvas)

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

	# System name — anchored to full rect for proper centering.
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
	subtitle.offset_top = 44.0
	subtitle.add_theme_font_size_override("font_size", 24)
	subtitle.add_theme_color_override("font_color", Color(0.6, 0.75, 0.9, 1.0))
	subtitle.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.15, 0.7))
	subtitle.add_theme_constant_override("shadow_offset_x", 1)
	subtitle.add_theme_constant_override("shadow_offset_y", 1)
	subtitle.modulate.a = 0.0
	canvas.add_child(subtitle)

	# Tween chain: letterbox → text → hold → fade → ship reveal → cleanup.
	var tween := create_tween()
	tween.tween_property(bar_top, "size:y", 80.0, 0.3).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(bar_bot, "offset_top", -80.0, 0.3).set_ease(Tween.EASE_OUT)
	tween.tween_property(lbl, "modulate:a", 1.0, 0.5).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(subtitle, "modulate:a", 1.0, 0.5).set_ease(Tween.EASE_OUT)
	tween.tween_interval(1.5)
	tween.tween_property(lbl, "modulate:a", 0.0, 0.5)
	tween.parallel().tween_property(subtitle, "modulate:a", 0.0, 0.4)
	tween.tween_property(bar_top, "size:y", 0.0, 0.3).set_ease(Tween.EASE_IN)
	tween.parallel().tween_property(bar_bot, "offset_top", 0.0, 0.3).set_ease(Tween.EASE_IN)
	# End cinematic: reveal ship, restore HUD, disable orbit mode.
	var hero_ref = _hero_body
	var hud_ref = hud
	tween.tween_callback(func():
		if is_instance_valid(hero_ref):
			hero_ref.visible = true
		if hud_ref and is_instance_valid(hud_ref) and hud_ref.has_method("set_overlay_mode_v0"):
			hud_ref.call("set_overlay_mode_v0", false)
	)
	if has_cam:
		var cam_ref = cam_ctrl
		tween.tween_callback(func():
			if is_instance_valid(cam_ref):
				cam_ref.cinematic_active = false
				cam_ref.cinematic_orbit_active = false
				cam_ref.cinematic_orbit_blend = 0.0
				cam_ref._current_mode = 0  # CameraMode.FLIGHT
				cam_ref._altitude = 80.0
				cam_ref._flight_yaw_offset = 0.0
				# Clean up transit-mode rendering (bypassed because cinematic skips
				# the camera's normal WARP_TRANSIT exit path).
				cam_ref._galaxy_map_pan_offset = Vector3.ZERO
				var gv_ref = _find_galaxy_view()
				if gv_ref and gv_ref.has_method("SetTransitModeV0"):
					gv_ref.call("SetTransitModeV0", false, "", "")
				if gv_ref and gv_ref.has_method("SetLocalLabelsVisibleV0"):
					gv_ref.call("SetLocalLabelsVisibleV0", true)
		)
	tween.tween_callback(canvas.queue_free)

	# Failsafe: ensure everything restored even if tween is killed.
	if has_cam or hero_hidden:
		var failsafe_cam = cam_ctrl
		var failsafe_hero = _hero_body
		var failsafe_hud = hud
		get_tree().create_timer(total_cinematic_time + 2.0).timeout.connect(func():
			if is_instance_valid(failsafe_cam):
				failsafe_cam.cinematic_active = false
				failsafe_cam.cinematic_orbit_active = false
				failsafe_cam.cinematic_orbit_blend = 0.0
				failsafe_cam._current_mode = 0  # CameraMode.FLIGHT
				failsafe_cam._altitude = 80.0
			if is_instance_valid(failsafe_hero):
				failsafe_hero.visible = true
			if failsafe_hud and is_instance_valid(failsafe_hud) and failsafe_hud.has_method("set_overlay_mode_v0"):
				failsafe_hud.call("set_overlay_mode_v0", false)
		)

## Position hero at the arrival gate corresponding to the origin system.
## Extracted from on_lane_arrival_v0 for reuse in reveal sweep.
func _position_hero_at_gate_v0(arrived_node_id: String, gv) -> void:
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
func _show_reveal_overlay_v0(node_id: String, bridge: Node) -> void:
	var display_name: String = node_id
	if bridge and bridge.has_method("GetNodeDisplayNameV0"):
		display_name = bridge.call("GetNodeDisplayNameV0", node_id)

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
	canvas.layer = 90
	add_child(canvas)
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
	subtitle.offset_top = 44.0
	subtitle.add_theme_font_size_override("font_size", 24)
	subtitle.add_theme_color_override("font_color", Color(0.6, 0.75, 0.9, 1.0))
	subtitle.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.15, 0.7))
	subtitle.add_theme_constant_override("shadow_offset_x", 1)
	subtitle.add_theme_constant_override("shadow_offset_y", 1)
	subtitle.modulate.a = 0.0
	canvas.add_child(subtitle)
	var tween := create_tween()
	tween.tween_property(bar_top, "size:y", 80.0, 0.3).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(bar_bot, "offset_top", -80.0, 0.3).set_ease(Tween.EASE_OUT)
	tween.tween_property(lbl, "modulate:a", 1.0, 0.5).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(subtitle, "modulate:a", 1.0, 0.5).set_ease(Tween.EASE_OUT)
	tween.tween_interval(1.5)
	tween.tween_property(lbl, "modulate:a", 0.0, 0.5)
	tween.parallel().tween_property(subtitle, "modulate:a", 0.0, 0.4)
	tween.tween_property(bar_top, "size:y", 0.0, 0.3).set_ease(Tween.EASE_IN)
	tween.parallel().tween_property(bar_bot, "offset_top", 0.0, 0.3).set_ease(Tween.EASE_IN)
	tween.tween_callback(canvas.queue_free)


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
	if not nearest.get_meta("is_hostile", false):
		return
	var fleet_id: String = _get_fleet_id_from_marker(nearest)
	if fleet_id.is_empty():
		return
	_spawn_bullet_v0(nearest.global_position, _hero_body.global_position, false, fleet_id)
	_ai_fire_cooldown = AI_FIRE_COOLDOWN_SEC

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

func _get_fleet_id_from_marker(marker: Node3D) -> String:
	var n: String = str(marker.name)
	if n.begins_with("Fleet_"):
		return n.substr(6)
	for child in marker.get_children():
		if child.has_meta("fleet_id"):
			return str(child.get_meta("fleet_id"))
	return ""

func _spawn_bullet_v0(origin: Vector3, target_pos: Vector3, is_player_bullet: bool, ai_fleet_id: String) -> void:
	if _bullet_scene == null:
		return
	var bullet = _bullet_scene.instantiate()
	# Set collision and source properties BEFORE adding to tree to prevent
	# spurious contacts on the first physics frame.
	bullet.set("source_is_player", is_player_bullet)
	if not is_player_bullet:
		bullet.set("source_fleet_id", ai_fleet_id)
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

	# Get origin and destination star positions in galactic space.
	# Use the gate position within the local system if available, so the transit
	# marker starts from the gate the player just entered (not the star center).
	var origin_pos: Vector3 = Vector3.ZERO
	var dest_pos: Vector3 = Vector3.ZERO
	if gv and gv.has_method("GetNodeScaledPositionV0"):
		if not _lane_origin_node_id.is_empty():
			origin_pos = gv.call("GetNodeScaledPositionV0", _lane_origin_node_id)
		dest_pos = gv.call("GetNodeScaledPositionV0", neighbor_node_id)
	# Offset origin to the gate position so the transit marker starts at the gate.
	# neighbor_node_id = destination, so the origin gate pointing there has that neighbor meta.
	if gv and gv.has_method("GetGatePositionV0"):
		var gate_pos: Vector3 = gv.call("GetGatePositionV0", neighbor_node_id)
		if gate_pos != Vector3.ZERO:
			origin_pos = gate_pos

	if dest_pos == Vector3.ZERO or _hero_body == null or not is_instance_valid(_hero_body):
		# Fallback: instant transit.
		await get_tree().create_timer(0.3).timeout
		on_lane_arrival_v0(neighbor_node_id)
		return

	if origin_pos == Vector3.ZERO:
		origin_pos = _hero_body.global_position

	warp_transit_dest_pos = dest_pos
	var raw_dir := (dest_pos - origin_pos)
	raw_dir.y = 0.0
	warp_transit_travel_dir = raw_dir.normalized() if raw_dir.length() > 0.1 else Vector3(1, 0, 0)

	var distance: float = origin_pos.distance_to(dest_pos)
	var transit_time: float = clampf(distance / 2000.0, 1.5, 3.0)

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

	# Pre-render destination system now so it becomes visible as camera descends.
	if gv and gv.has_method("RebuildLocalSystemV0"):
		gv.call("RebuildLocalSystemV0", neighbor_node_id)
	# Suppress all labels during transit — player should discover them, not read them in passing.
	if gv and gv.has_method("SetLocalLabelsVisibleV0"):
		gv.call("SetLocalLabelsVisibleV0", false)

	# Compute destination gate position (gate in dest system pointing back to origin).
	# Lane line and transit marker end here, not at the star center.
	var dest_gate_pos: Vector3 = dest_pos  # Fallback to star center.
	if gv and gv.has_method("GetGatePositionV0") and not _lane_origin_node_id.is_empty():
		var dgp: Vector3 = gv.call("GetGatePositionV0", _lane_origin_node_id)
		if dgp != Vector3.ZERO:
			dest_gate_pos = dgp

	# Create bright lane line from origin gate to destination gate.
	var _transit_lane_line := _create_transit_lane_line_v0(origin_pos, dest_gate_pos)

	# Smooth zoom-out: tween altitude from current camera height to cruise altitude.
	var zoom_out_time: float = 0.6
	var zoom_out_tween := create_tween()
	zoom_out_tween.tween_property(self, "warp_transit_altitude", cruise_altitude, zoom_out_time) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

	# Tilt camera forward so destination is visible during transit (comet approach).
	if cam_ctrl and cam_ctrl.get("warp_transit_tilt") != null:
		var tilt_tween := create_tween()
		tilt_tween.tween_property(cam_ctrl, "warp_transit_tilt", 1.0, 0.8) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

	# Wait for zoom-out to complete before marker starts moving.
	await zoom_out_tween.finished

	# Check first visit BEFORE transit starts (dispatch marks it as visited).
	var is_first_visit: bool = false
	if bridge and bridge.has_method("IsFirstVisitV0"):
		is_first_visit = bridge.call("IsFirstVisitV0", neighbor_node_id)

	# Store lane line ref for cleanup.
	_transit_lane_line_ref = _transit_lane_line

	if is_first_visit:
		# === FIRST VISIT: Curved approach + orbital sweep ===
		# The transit path curves off to one side before reaching the system,
		# naturally entering an orbital sweep around the star.

		# Phase 1: Straight approach from origin toward a "diversion point" 150u from star.
		var approach_dir := (dest_pos - origin_pos)
		approach_dir.y = 0.0
		approach_dir = approach_dir.normalized()
		var divert_dist: float = 150.0  # Distance from star center to start curving.
		var divert_pos := dest_pos - approach_dir * divert_dist
		var straight_time: float = transit_time * 0.7

		var tween := create_tween()
		tween.tween_property(_transit_marker, "global_position", divert_pos, straight_time) \
			.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)

		# Parallel altitude descent during straight approach.
		var alt_tween := create_tween()
		var descent_delay: float = straight_time * 0.3
		var descent_time: float = straight_time * 0.7
		alt_tween.tween_interval(descent_delay)
		alt_tween.tween_property(self, "warp_transit_altitude", 200.0, descent_time) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

		# Fade lane line at 70% of straight approach.
		var fade_delay: float = straight_time * 0.7
		var fade_time: float = straight_time * 0.3
		if _transit_lane_line and is_instance_valid(_transit_lane_line):
			var lane_mat = _transit_lane_line.material_override as StandardMaterial3D
			if lane_mat:
				var lane_fade := create_tween()
				lane_fade.tween_interval(fade_delay)
				lane_fade.tween_property(lane_mat, "albedo_color:a", 0.0, fade_time)
		if _transit_marker and is_instance_valid(_transit_marker):
			var marker_mat = _transit_marker.material_override as StandardMaterial3D
			if marker_mat:
				var marker_fade := create_tween()
				marker_fade.tween_interval(fade_delay)
				marker_fade.tween_property(marker_mat, "albedo_color:a", 0.0, fade_time)
			for child in _transit_marker.get_children():
				if child is MeshInstance3D:
					var glow_mat = child.material_override as StandardMaterial3D
					if glow_mat:
						var glow_fade := create_tween()
						glow_fade.tween_interval(fade_delay)
						glow_fade.tween_property(glow_mat, "albedo_color:a", 0.0, fade_time)

		await tween.finished

		# Clean up transit marker.
		if _transit_marker and is_instance_valid(_transit_marker):
			_transit_marker.queue_free()
			_transit_marker = null
	else:
		# === RETURN VISIT: Straight transit to destination gate ===
		var tween := create_tween()
		tween.tween_property(_transit_marker, "global_position", dest_gate_pos, transit_time) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

		# Altitude descent.
		var alt_tween := create_tween()
		var descent_delay: float = transit_time * 0.25
		var descent_time: float = transit_time * 0.75
		alt_tween.tween_interval(descent_delay)
		alt_tween.tween_property(self, "warp_transit_altitude", 120.0, descent_time) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

		# Fade lane line + marker.
		var fade_delay: float = transit_time * 0.7
		var fade_time: float = transit_time * 0.3
		if _transit_lane_line and is_instance_valid(_transit_lane_line):
			var lane_mat = _transit_lane_line.material_override as StandardMaterial3D
			if lane_mat:
				var lane_fade := create_tween()
				lane_fade.tween_interval(fade_delay)
				lane_fade.tween_property(lane_mat, "albedo_color:a", 0.0, fade_time)
		if _transit_marker and is_instance_valid(_transit_marker):
			var marker_mat = _transit_marker.material_override as StandardMaterial3D
			if marker_mat:
				var marker_fade := create_tween()
				marker_fade.tween_interval(fade_delay)
				marker_fade.tween_property(marker_mat, "albedo_color:a", 0.0, fade_time)
			for child in _transit_marker.get_children():
				if child is MeshInstance3D:
					var glow_mat = child.material_override as StandardMaterial3D
					if glow_mat:
						var glow_fade := create_tween()
						glow_fade.tween_interval(fade_delay)
						glow_fade.tween_property(glow_mat, "albedo_color:a", 0.0, fade_time)

		await tween.finished

		# Clean up transit marker.
		if _transit_marker and is_instance_valid(_transit_marker):
			_transit_marker.queue_free()
			_transit_marker = null

	if is_first_visit:
		# === FIRST VISIT: Cinematic orbital approach ===
		# The camera stays in forward-tilt (comet view) and we move warp_transit_target
		# on a circular path around the star. The forward-looking camera naturally creates
		# a dramatic orbital sweep — no mode switch, just path manipulation.
		_last_arrival_first_visit = true
		_apply_camera_shake_v0(0.25)

		# Dispatch sim arrival.
		if bridge and bridge.has_method("DispatchPlayerArriveV0"):
			bridge.call("DispatchPlayerArriveV0", neighbor_node_id)
		# Re-suppress labels: dispatch may create new overlay nodes with fresh labels.
		if gv and gv.has_method("SetLocalLabelsVisibleV0"):
			gv.call("SetLocalLabelsVisibleV0", false)

		# Rebuild persistent lanes for newly visible connections.
		if gv and gv.has_method("RebuildPersistentLanesV0"):
			gv.call("RebuildPersistentLanesV0")

		# Position hero at arrival gate.
		_position_hero_at_gate_v0(neighbor_node_id, gv)
		if _hero_body and is_instance_valid(_hero_body):
			_hero_body.visible = true

		# Get star center and hero position.
		var star_center: Vector3 = Vector3.ZERO
		if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
			star_center = gv.call("GetCurrentStarGlobalPositionV0")
		var hero_pos: Vector3 = _hero_body.global_position if (_hero_body and is_instance_valid(_hero_body)) else star_center

		# Clean up lane line.
		if _transit_lane_line_ref and is_instance_valid(_transit_lane_line_ref):
			_transit_lane_line_ref.queue_free()
			_transit_lane_line_ref = null

		# Letterbox overlay with system name + jump event toasts.
		_show_reveal_overlay_v0(neighbor_node_id, bridge)
		_show_jump_event_toasts_v0(bridge)

		# --- Curved orbital approach from diversion point ---
		# Camera is forward-tilted and was following the transit marker to the
		# diversion point (150u from star). Now move warp_transit_target on a
		# spiral path from that point inward around the star, and rotate
		# warp_transit_travel_dir to the orbit tangent so the chase-cam naturally
		# swings around — creating the "curved approach" visible BEFORE arrival.
		var approach_dir2 := (dest_pos - origin_pos)
		approach_dir2.y = 0.0
		approach_dir2 = approach_dir2.normalized()
		var divert_offset := (dest_pos - approach_dir2 * 150.0) - star_center
		divert_offset.y = 0.0
		var orbit_start_radius: float = divert_offset.length()
		if orbit_start_radius < 20.0:
			orbit_start_radius = 150.0
		var start_angle: float = atan2(divert_offset.z, divert_offset.x)

		# Spiral from diversion radius (~150u) down to 50u, sweeping 300°.
		var orbit_duration: float = 5.0
		var orbit_sweep: float = PI * 5.0 / 3.0  # 300 degrees
		var orbit_steps: int = 60
		var step_time: float = orbit_duration / float(orbit_steps)
		var orbit_start_alt: float = warp_transit_altitude
		var orbit_tween := create_tween()

		for i in range(orbit_steps):
			var t: float = float(i + 1) / float(orbit_steps)
			var angle: float = start_angle + orbit_sweep * t
			# Spiral: diversion radius → 50u (first 40%), 50→45 (next 30%), 45→60 (last 30%).
			var r: float
			if t < 0.4:
				r = lerpf(orbit_start_radius, 50.0, t / 0.4)
			elif t < 0.7:
				r = lerpf(50.0, 45.0, (t - 0.4) / 0.3)
			else:
				r = lerpf(45.0, 60.0, (t - 0.7) / 0.3)
			var orbit_x: float = star_center.x + cos(angle) * r
			var orbit_z: float = star_center.z + sin(angle) * r
			var target_pos := Vector3(orbit_x, 0.0, orbit_z)
			orbit_tween.tween_property(self, "warp_transit_target", target_pos, step_time)
			# Rotate travel_dir to orbit tangent — camera follows behind.
			# Blend from approach_dir to tangent over first 15% to avoid snap.
			var tangent := Vector3(-sin(angle), 0.0, cos(angle)).normalized()
			var blend_t: float = clampf(t / 0.15, 0.0, 1.0)
			var dir := approach_dir2.lerp(tangent, blend_t).normalized()
			orbit_tween.parallel().tween_property(self, "warp_transit_travel_dir", dir, step_time)
			# Altitude: dip from start → 40u at 40%, rise to 60u.
			var alt: float
			if t < 0.4:
				alt = lerpf(orbit_start_alt, 40.0, t / 0.4)
			elif t < 0.7:
				alt = lerpf(40.0, 45.0, (t - 0.4) / 0.3)
			else:
				alt = lerpf(45.0, 60.0, (t - 0.7) / 0.3)
			orbit_tween.parallel().tween_property(self, "warp_transit_altitude", alt, step_time)

		await orbit_tween.finished

		# --- Settle phase (2.0s) — smooth refocus on hero ship ---
		# Move warp_transit_target to hero position while rising to flight altitude.
		# Untilt camera from forward to top-down.
		cam_ctrl = _find_camera_controller()
		var settle_tween := create_tween()
		settle_tween.set_parallel(true)
		settle_tween.tween_property(self, "warp_transit_target", hero_pos, 2.0) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
		settle_tween.tween_property(self, "warp_transit_altitude", 80.0, 2.0) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
		if cam_ctrl and cam_ctrl.get("warp_transit_tilt") != null:
			settle_tween.tween_property(cam_ctrl, "warp_transit_tilt", 0.0, 2.0) \
				.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)
		await settle_tween.finished

		# Transition to IN_FLIGHT.
		_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
		_lane_cooldown_v0 = 2.0

		# Restore labels.
		var gv_restore = _find_galaxy_view()
		if gv_restore and gv_restore.has_method("SetLocalLabelsVisibleV0"):
			gv_restore.call("SetLocalLabelsVisibleV0", true)

		# Restore hero + HUD.
		if _hero_body and is_instance_valid(_hero_body):
			_hero_body.visible = true
		var hud = get_tree().root.find_child("HUD", true, false) if get_tree() else null
		if hud and hud.has_method("set_overlay_mode_v0"):
			hud.call("set_overlay_mode_v0", false)
	else:
		# Return visit: fast gate-to-gate — quick untilt and arrive.
		if cam_ctrl and cam_ctrl.get("warp_transit_tilt") != null and cam_ctrl.warp_transit_tilt > 0.01:
			var untilt := create_tween()
			untilt.tween_property(cam_ctrl, "warp_transit_tilt", 0.0, 0.3) \
				.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
			await untilt.finished
		if _transit_lane_line_ref and is_instance_valid(_transit_lane_line_ref):
			_transit_lane_line_ref.queue_free()
			_transit_lane_line_ref = null
		on_lane_arrival_v0(neighbor_node_id)

func _create_transit_marker_v0(pos: Vector3) -> MeshInstance3D:
	var marker := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 8.0
	sphere.height = 16.0
	sphere.radial_segments = 16
	sphere.rings = 8
	marker.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.4, 0.7, 1.0, 0.95)
	mat.emission_enabled = true
	mat.emission = Color(0.5, 0.8, 1.0)
	mat.emission_energy_multiplier = 8.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	marker.material_override = mat
	# Outer glow — slightly larger transparent sphere for bloom-like effect.
	var glow := MeshInstance3D.new()
	var glow_sphere := SphereMesh.new()
	glow_sphere.radius = 10.0
	glow_sphere.height = 20.0
	glow_sphere.radial_segments = 12
	glow_sphere.rings = 6
	glow.mesh = glow_sphere
	var glow_mat := StandardMaterial3D.new()
	glow_mat.albedo_color = Color(0.3, 0.6, 1.0, 0.12)
	glow_mat.emission_enabled = true
	glow_mat.emission = Color(0.4, 0.7, 1.0)
	glow_mat.emission_energy_multiplier = 3.0
	glow_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	glow_mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	glow.material_override = glow_mat
	marker.add_child(glow)
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
	_sfx_turret_fire = AudioStreamPlayer.new()
	_sfx_turret_fire.name = "SfxTurretFire"
	_sfx_turret_fire.stream = load("res://assets/audio/laser_fire.wav")
	_sfx_turret_fire.volume_db = -12.0
	add_child(_sfx_turret_fire)

	_sfx_bullet_hit = AudioStreamPlayer.new()
	_sfx_bullet_hit.name = "SfxBulletHit"
	_sfx_bullet_hit.stream = load("res://assets/audio/bullet_hit.wav")
	_sfx_bullet_hit.volume_db = -10.0
	add_child(_sfx_bullet_hit)

	_sfx_explosion = AudioStreamPlayer.new()
	_sfx_explosion.name = "SfxExplosion"
	_sfx_explosion.stream = load("res://assets/audio/explosion.wav")
	_sfx_explosion.volume_db = -6.0
	add_child(_sfx_explosion)

	# Engine thrust handled by EngineAudio node on player — no duplicate here
	_sfx_engine_thrust = null

	# GATE.S1.AUDIO.AMBIENT.001: ambient drone + event chimes
	_sfx_ambient_drone = AudioStreamPlayer.new()
	_sfx_ambient_drone.name = "SfxAmbientDrone"
	_sfx_ambient_drone.stream = load("res://assets/audio/ambient_drone.wav")
	_sfx_ambient_drone.volume_db = -18.0
	add_child(_sfx_ambient_drone)
	_sfx_ambient_drone.play()

	_sfx_warp_whoosh = AudioStreamPlayer.new()
	_sfx_warp_whoosh.name = "SfxWarpWhoosh"
	_sfx_warp_whoosh.stream = load("res://assets/audio/warp_whoosh.wav")
	_sfx_warp_whoosh.volume_db = -12.0
	add_child(_sfx_warp_whoosh)

	_sfx_dock_chime = AudioStreamPlayer.new()
	_sfx_dock_chime.name = "SfxDockChime"
	_sfx_dock_chime.stream = load("res://assets/audio/dock_chime.wav")
	_sfx_dock_chime.volume_db = -6.0
	add_child(_sfx_dock_chime)

	# System arrival stinger — calm ambient snippet for first-visit reveals.
	_sfx_system_arrival = AudioStreamPlayer.new()
	_sfx_system_arrival.name = "SfxSystemArrival"
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

# GATE.S1.SAVE_UI.PAUSE_MENU.001: toggle pause state.
func _toggle_pause_v0() -> void:
	if _player_dead:
		return
	_paused = not _paused
	get_tree().paused = _paused
	if _paused and galaxy_overlay_open:
		toggle_galaxy_map_overlay_v0()
	var hud = get_tree().root.find_child("HUD", true, false)
	if hud and hud.has_method("toggle_pause_menu_v0"):
		hud.call("toggle_pause_menu_v0", _paused)
	print("UUIR|PAUSE|" + str(_paused))

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
	if bridge.has_method("GetPlayerSnapshotV0"):
		var snap: Dictionary = bridge.call("GetPlayerSnapshotV0")
		var node_id: String = str(snap.get("location", ""))
		if not node_id.is_empty() and node_id != _last_player_node_id and not _last_player_node_id.is_empty():
			var display_name: String = node_id
			if bridge.has_method("GetNodeDisplayNameV0"):
				display_name = str(bridge.call("GetNodeDisplayNameV0", node_id))
			toast_mgr.call("show_toast", "Arrived at %s" % display_name, 2.5)
		_last_player_node_id = node_id

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

# GATE.S14.TRANSIT.WARP_EFFECT.001: White screen flash on warp entry.
func _flash_warp_screen_v0() -> void:
	var canvas := CanvasLayer.new()
	canvas.layer = 100
	add_child(canvas)
	var rect := ColorRect.new()
	rect.color = Color(1.0, 1.0, 1.0, 0.5)
	rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(rect)
	var tween := create_tween()
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
		match kind:
			"salvage":
				color = "#22AA22"
				var good_id: String = str(evt.get("good_id", ""))
				var qty: int = int(evt.get("quantity", 0))
				message = "Lane Salvage: found %d x %s!" % [qty, good_id]
			"signal":
				color = "#2288DD"
				message = "Lane Signal: anomaly detected nearby!"
			"turbulence":
				color = "#DD4444"
				var hull_dmg: int = int(evt.get("hull_damage", 0))
				message = "Lane Turbulence: hull took %d damage!" % hull_dmg
			_:
				continue

		print("UUIR|JUMP_EVENT_TOAST|" + kind + "|" + event_id)
		if toast_mgr.has_method("show_toast_colored"):
			toast_mgr.call("show_toast_colored", message, 4.0, color)
		else:
			toast_mgr.call("show_toast", message, 4.0)

# GATE.S14.STARTER.MISSION_PROMPT.001: Show jobs toast on first dock at star_0.
var _first_dock_jobs_shown: bool = false
func _check_first_dock_mission_prompt_v0() -> void:
	if _first_dock_jobs_shown:
		return
	if dock_target_kind_token != "STATION" and dock_target_kind_token != "PLANET":
		return
	# Only at the starter system
	if _last_player_node_id != "star_0":
		return
	_first_dock_jobs_shown = true
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_toast"):
		toast_mgr.call("show_toast", "Check the Jobs tab for available work!", 5.0)

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
