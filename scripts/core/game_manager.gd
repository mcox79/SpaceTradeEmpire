extends Node

# sim.gd (GDScript sim) is kept for galaxy_spawner 3D visual scaffolding ONLY.
# It must NOT be ticked or used for game logic. All game logic routes through SimBridge (C#).
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')

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

# Proof helper: used by headless tests to verify the local scene continues ticking
# while the galaxy overlay is open. Do not print this value.
var time_accumulator: float = 0.0

# Galaxy overlay v0 wiring (CanvasLayer above local scene; no scene swap)
var galaxy_overlay_open: bool = false
var _galaxy_overlay_layer: CanvasLayer
var _galaxy_overlay_camera: Camera3D
var _galaxy_view: Node
var _prev_camera: Camera3D

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
	_galaxy_overlay_layer = root.get_node_or_null("GalaxyOverlay")
	_galaxy_overlay_camera = root.get_node_or_null("GalaxyOverlayCamera")
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

	if _galaxy_overlay_layer:
		_galaxy_overlay_layer.visible = false
	if _galaxy_overlay_camera:
		_galaxy_overlay_camera.current = false
	if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
		_galaxy_view.call("SetOverlayOpenV0", false)

	# GATE.S12.UX_POLISH.ONBOARDING.001: Show welcome toasts on game start (delayed).
	_show_onboarding_toasts_deferred_v0()

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

	# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: poll player HP each frame for death detection
	_check_player_death_v0()

	# GATE.S11.GAME_FEEL.TOAST_EVENTS.001: poll research + events for toast notifications
	_toast_poll_timer += float(delta)
	if _toast_poll_timer >= TOAST_POLL_INTERVAL:
		_toast_poll_timer = 0.0
		_poll_toast_events_v0()


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
		if event.keycode == KEY_ESCAPE:
			_toggle_pause_v0()
			return
		if event.keycode == KEY_TAB:
			toggle_galaxy_map_overlay_v0()
		if event.keycode == KEY_E:
			_toggle_empire_dashboard_v0()
		if event.keycode == KEY_H:
			_toggle_keybinds_help_v0()
		if event.keycode == KEY_L:
			_toggle_combat_log_v0()

# GATE.S10.EMPIRE.SHELL.001: Toggle empire dashboard panel (created by SimBridge in C#).
func _toggle_empire_dashboard_v0():
	if _empire_dashboard == null:
		_empire_dashboard = get_tree().root.find_child("EmpireDashboard", true, false)
	if _empire_dashboard != null:
		_empire_dashboard.visible = not _empire_dashboard.visible

func toggle_market():
	# No-op stub. Station UI is driven by C# StationMenu via SimBridge.
	return


func toggle_galaxy_map_overlay_v0():
	# Lazy lookup: autoload instance won't find overlay nodes in _ready() because
	# its parent is /root. Defer lookup to first use via scene tree search.
	if not _galaxy_overlay_layer:
		_galaxy_overlay_layer = get_tree().root.find_child("GalaxyOverlay", true, false)
		_galaxy_overlay_camera = get_tree().root.find_child("GalaxyOverlayCamera", true, false)
		_galaxy_view = get_tree().root.find_child("GalaxyView", true, false)
	if not _galaxy_overlay_layer:
		return
	galaxy_overlay_open = not galaxy_overlay_open

	# Sync to autoload so flight controller (which reads /root/GameManager) sees the flag.
	var autoload_gm = get_node_or_null("/root/GameManager")
	if autoload_gm and autoload_gm != self:
		autoload_gm.set("galaxy_overlay_open", galaxy_overlay_open)

	if _galaxy_overlay_layer:
		_galaxy_overlay_layer.visible = galaxy_overlay_open

	# Hide/show player ship — it's not part of LocalSystem so GalaxyView doesn't manage it.
	if _hero_body and is_instance_valid(_hero_body):
		_hero_body.visible = not galaxy_overlay_open

	# Camera switching: overlay uses a dedicated camera; restore previous camera on close.
	if galaxy_overlay_open:
		var active_cam = get_viewport().get_camera_3d()
		if active_cam and active_cam != _galaxy_overlay_camera:
			_prev_camera = active_cam
		if _galaxy_overlay_camera:
			_galaxy_overlay_camera.current = true
	else:
		if _galaxy_overlay_camera:
			_galaxy_overlay_camera.current = false
		if _prev_camera and is_instance_valid(_prev_camera):
			_prev_camera.current = true

	# GalaxyView rendering must be gated behind overlay-mode flag.
	if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
		_galaxy_view.call("SetOverlayOpenV0", galaxy_overlay_open)

# Valid transitions: IN_FLIGHT<->DOCKED, IN_FLIGHT<->IN_LANE_TRANSIT.
# Emits INVALID_STATE_TRANSITION token and returns false on bad transition.
func _transition_player_state_v0(new_state: PlayerShipState) -> bool:
	var valid := false
	match current_player_state:
		PlayerShipState.IN_FLIGHT:
			valid = new_state == PlayerShipState.DOCKED or new_state == PlayerShipState.IN_LANE_TRANSIT
		PlayerShipState.DOCKED:
			valid = new_state == PlayerShipState.IN_FLIGHT
		PlayerShipState.IN_LANE_TRANSIT:
			valid = new_state == PlayerShipState.IN_FLIGHT
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
	return "UNKNOWN"

func on_proximity_dock_entered_v0(target: Node):
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
	elif dock_target_kind_token == "DISCOVERY_SITE":
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
	# GATE.S1.AUDIO.AMBIENT.001: warp whoosh on lane jump
	if _sfx_warp_whoosh:
		_sfx_warp_whoosh.play()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchTravelCommandV0"):
		bridge.call("DispatchTravelCommandV0", PLAYER_FLEET_ID, neighbor_node_id)

	# Auto-complete lane transit after sim processes the travel command.
	_begin_lane_transit_v0(neighbor_node_id)

func on_discovery_site_proximity_entered_v0(site_id: String) -> void:
	if not _transition_player_state_v0(PlayerShipState.DOCKED):
		return
	dock_target_kind_token = "DISCOVERY_SITE"
	dock_target_id = site_id
	print("UUIR|DISCOVERY_DOCK|DISCOVERY_SITE|" + site_id)
	if _discovery_panel and _discovery_panel.has_method("open_v0"):
		_discovery_panel.call("open_v0", site_id)

func on_lane_arrival_v0(arrived_node_id: String) -> void:
	if current_player_state != PlayerShipState.IN_LANE_TRANSIT:
		return

	print("UUIR|LANE_EXIT|" + arrived_node_id)
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerArriveV0"):
		bridge.call("DispatchPlayerArriveV0", arrived_node_id)
	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)
	_lane_cooldown_v0 = 2.0  # Prevent immediate re-trigger at destination lane gate.

	# Rebuild local scene for the new star system.
	var gv = _find_galaxy_view()
	if gv and gv.has_method("RebuildLocalSystemV0"):
		gv.call("RebuildLocalSystemV0", arrived_node_id)

	# Reset hero ship to origin of new system.
	if _hero_body and is_instance_valid(_hero_body):
		_hero_body.global_position = Vector3.ZERO
		_hero_body.linear_velocity = Vector3.ZERO
		_hero_body.angular_velocity = Vector3.ZERO

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
	# Wait 300ms for the sim thread to process the TravelCommand, then auto-arrive.
	await get_tree().create_timer(0.3).timeout

	# GATE.S3.RISK_SINKS.BRIDGE.001: Check for delay status before completing transit.
	# If the fleet is delayed by a risk event, wait additional ticks before arriving.
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetDelayStatusV0"):
		var delay_info: Dictionary = bridge.call("GetDelayStatusV0", PLAYER_FLEET_ID)
		if delay_info.get("delayed", false):
			var ticks_remaining: int = int(delay_info.get("ticks_remaining", 0))
			# Convert sim ticks to approximate real-time seconds (100ms per tick default).
			var delay_sec: float = ticks_remaining * 0.1
			if delay_sec > 0.0:
				print("UUIR|LANE_DELAY|" + str(ticks_remaining) + "|" + neighbor_node_id)
				await get_tree().create_timer(delay_sec).timeout

	on_lane_arrival_v0(neighbor_node_id)

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

# GATE.S1.AUDIO.SFX_CORE.001: Procedural synth tone generator (placeholder audio).
func _generate_tone_v0(freq_hz: float, duration_sec: float, vol: float = 0.5, apply_decay: bool = true) -> AudioStreamWAV:
	var sr := 22050
	var n := int(sr * duration_sec)
	if n < 1:
		n = 1
	var data := PackedByteArray()
	data.resize(n * 2)
	for i in range(n):
		var t := float(i) / sr
		var s := sin(2.0 * PI * freq_hz * t) * vol
		if apply_decay:
			s *= 1.0 - float(i) / n
		var s16 := int(clampf(s * 32767.0, -32768.0, 32767.0))
		data[i * 2] = s16 & 0xFF
		data[i * 2 + 1] = (s16 >> 8) & 0xFF
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = sr
	stream.stereo = false
	stream.data = data
	return stream

func _generate_noise_v0(duration_sec: float, vol: float = 0.3) -> AudioStreamWAV:
	var sr := 22050
	var n := int(sr * duration_sec)
	if n < 1:
		n = 1
	var data := PackedByteArray()
	data.resize(n * 2)
	var rng := RandomNumberGenerator.new()
	rng.seed = 42
	for i in range(n):
		var env := 1.0 - float(i) / n
		var s := (rng.randf() * 2.0 - 1.0) * vol * env
		var s16 := int(clampf(s * 32767.0, -32768.0, 32767.0))
		data[i * 2] = s16 & 0xFF
		data[i * 2 + 1] = (s16 >> 8) & 0xFF
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = sr
	stream.stereo = false
	stream.data = data
	return stream

func _init_sfx_v0() -> void:
	_sfx_turret_fire = AudioStreamPlayer.new()
	_sfx_turret_fire.name = "SfxTurretFire"
	_sfx_turret_fire.stream = _generate_tone_v0(440.0, 0.1)
	_sfx_turret_fire.volume_db = -6.0
	add_child(_sfx_turret_fire)

	_sfx_bullet_hit = AudioStreamPlayer.new()
	_sfx_bullet_hit.name = "SfxBulletHit"
	_sfx_bullet_hit.stream = _generate_tone_v0(800.0, 0.05, 0.4)
	_sfx_bullet_hit.volume_db = -6.0
	add_child(_sfx_bullet_hit)

	_sfx_explosion = AudioStreamPlayer.new()
	_sfx_explosion.name = "SfxExplosion"
	_sfx_explosion.stream = _generate_noise_v0(0.3)
	_sfx_explosion.volume_db = -3.0
	add_child(_sfx_explosion)

	var thrust_stream := _generate_tone_v0(80.0, 1.0, 0.15, false)
	thrust_stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	thrust_stream.loop_begin = 0
	thrust_stream.loop_end = 22050
	_sfx_engine_thrust = AudioStreamPlayer.new()
	_sfx_engine_thrust.name = "SfxEngineThrust"
	_sfx_engine_thrust.stream = thrust_stream
	_sfx_engine_thrust.volume_db = -12.0
	add_child(_sfx_engine_thrust)

	# GATE.S1.AUDIO.AMBIENT.001: ambient drone + event chimes
	var drone := _generate_tone_v0(50.0, 2.0, 0.08, false)
	drone.loop_mode = AudioStreamWAV.LOOP_FORWARD
	drone.loop_begin = 0
	drone.loop_end = 44100
	_sfx_ambient_drone = AudioStreamPlayer.new()
	_sfx_ambient_drone.name = "SfxAmbientDrone"
	_sfx_ambient_drone.stream = drone
	_sfx_ambient_drone.volume_db = -18.0
	add_child(_sfx_ambient_drone)
	_sfx_ambient_drone.play()

	_sfx_warp_whoosh = AudioStreamPlayer.new()
	_sfx_warp_whoosh.name = "SfxWarpWhoosh"
	_sfx_warp_whoosh.stream = _generate_noise_v0(0.5, 0.25)
	_sfx_warp_whoosh.volume_db = -6.0
	add_child(_sfx_warp_whoosh)

	_sfx_dock_chime = AudioStreamPlayer.new()
	_sfx_dock_chime.name = "SfxDockChime"
	_sfx_dock_chime.stream = _generate_tone_v0(880.0, 0.3, 0.3)
	_sfx_dock_chime.volume_db = -6.0
	add_child(_sfx_dock_chime)

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
