extends Node

# sim.gd (GDScript sim) is kept for galaxy_spawner 3D visual scaffolding ONLY.
# It must NOT be ticked or used for game logic. All game logic routes through SimBridge (C#).
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')

# SimCore fleet ID for the player fleet (matches WorldLoader constant).
const PLAYER_FLEET_ID := "fleet_trader_1"

# Hero ship flight tuning (v0). Keep these centralized to avoid magic numbers in physics logic.
const HERO_THRUST_FORCE_V0: float = 55.0
const HERO_MAX_SPEED_V0: float = 28.0
const HERO_LINEAR_DAMPING_V0: float = 0.8
const HERO_ANGULAR_DAMPING_V0: float = 1.2
const HERO_GRAVITY_SCALE_V0: float = 0.0

# Deterministic test override (null means use live input).
var _hero_test_thrust_axis_v0 = null

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

	if _galaxy_overlay_layer:
		_galaxy_overlay_layer.visible = false
	if _galaxy_overlay_camera:
		_galaxy_overlay_camera.current = false
	if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
		_galaxy_view.call("SetOverlayOpenV0", false)

func _process(delta):
	# Local ticking must continue while overlay is open. This is used only as a boolean check in tests.
	time_accumulator += float(delta)

func _physics_process(delta):
	# Hero ship flight v0: force-based thrust with inertia.
	# Deterministic: no wall-clock, no random, no unordered iteration, no printing here.
	if _hero_body == null:
		return

	var thrust_axis: float = 0.0
	if _hero_test_thrust_axis_v0 != null:
		thrust_axis = float(_hero_test_thrust_axis_v0)
	else:
		# Use stock UI actions to avoid requiring a custom InputMap for this gate.
		# Forward: ui_up, Back: ui_down
		if Input.is_action_pressed("ui_up"):
			thrust_axis += 1.0
		if Input.is_action_pressed("ui_down"):
			thrust_axis -= 1.0

	if thrust_axis != 0.0:
		# Ship forward is -Z in Godot.
		var dir: Vector3 = -_hero_body.global_transform.basis.z
		_hero_body.apply_central_force(dir * (HERO_THRUST_FORCE_V0 * thrust_axis))

	# Clamp max speed deterministically.
	var v: Vector3 = _hero_body.linear_velocity
	var speed: float = v.length()
	if speed > HERO_MAX_SPEED_V0 and speed > 0.0:
		_hero_body.linear_velocity = v * (HERO_MAX_SPEED_V0 / speed)

func _unhandled_input(event):
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_TAB:
			toggle_galaxy_map_overlay_v0()

func toggle_market():
	# No-op stub. Station UI is driven by C# StationMenu via SimBridge.
	return

# Test hook: deterministic thrust driving without relying on InputMap.
func hero_test_set_thrust_axis_v0(axis: float):
	_hero_test_thrust_axis_v0 = axis

func hero_test_clear_thrust_axis_v0():
	_hero_test_thrust_axis_v0 = null

func toggle_galaxy_map_overlay_v0():
	galaxy_overlay_open = not galaxy_overlay_open

	if _galaxy_overlay_layer:
		_galaxy_overlay_layer.visible = galaxy_overlay_open

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

	if dock_target_kind_token == "STATION":
		_open_station_menu_v0(target)
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

func on_lane_gate_proximity_entered_v0(neighbor_node_id: String) -> void:
	if not _transition_player_state_v0(PlayerShipState.IN_LANE_TRANSIT):
		return

	print("UUIR|LANE_ENTER|" + neighbor_node_id)

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchTravelCommandV0"):
		bridge.call("DispatchTravelCommandV0", PLAYER_FLEET_ID, neighbor_node_id)

func on_discovery_site_proximity_entered_v0(site_id: String) -> void:
	if not _transition_player_state_v0(PlayerShipState.DOCKED):
		return
	dock_target_kind_token = "DISCOVERY_SITE"
	dock_target_id = site_id
	print("UUIR|DISCOVERY_DOCK|DISCOVERY_SITE|" + site_id)

func on_lane_arrival_v0(arrived_node_id: String) -> void:
	if current_player_state != PlayerShipState.IN_LANE_TRANSIT:
		return

	print("UUIR|LANE_EXIT|" + arrived_node_id)
	_transition_player_state_v0(PlayerShipState.IN_FLIGHT)

func _on_station_menu_request_undock():
	undock_v0()

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
