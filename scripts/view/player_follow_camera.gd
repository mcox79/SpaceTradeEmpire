extends Node3D

# GATE.S1.CAMERA.FOLLOW_MODES.001: PhantomCamera3D-based follow modes.
# Three modes: Flight (chase), Orbit (docked free-orbit), Station (fixed angle).
# Falls back to basic Camera3D follow if PhantomCamera3D is unavailable.

# Tabs-only indentation policy applies.

enum CameraMode {
	FLIGHT,
	ORBIT,
	STATION,
	GALAXY_MAP,  # GATE.S17.REAL_SPACE.GALAXY_MAP.001
	WARP_TRANSIT,  # Galaxy-map zoom during lane transit.
}

@export var target_path: NodePath

# Flight mode: top-down (Starcom Nexus style) — camera directly above player.
@export var flight_offset: Vector3 = Vector3(0, 80, 1)
@export var flight_follow_distance: float = 80.0
@export var flight_damping: Vector3 = Vector3(0.15, 0.15, 0.15)

# Orbit mode: free orbit when docked.
@export var orbit_distance: float = 30.0
@export var orbit_sensitivity: float = 0.01
@export var orbit_min_pitch: float = -1.20
@export var orbit_max_pitch: float = 1.50
@export var orbit_min_distance: float = 8.0
@export var orbit_max_distance: float = 100.0
@export var orbit_zoom_step: float = 3.0

# Station mode: fixed camera transform for station view.
@export var station_offset: Vector3 = Vector3(0, 40, 25)

# FOV swell (flight mode only).
@export var fov_base: float = 60.0
@export var fov_boost_max: float = 2.0
@export var fov_smooth: float = 4.0

# Reset key.
@export var reset_keycode: int = KEY_R

var _current_mode: CameraMode = CameraMode.FLIGHT
var _target: Node3D

# PhantomCamera3D node reference (child of this Node3D, set in _ready or scene).
var _pcam: Node = null
var _pcam_available: bool = false

# Fallback Camera3D (used when PhantomCamera3D is not available).
var _fallback_cam: Camera3D = null
var _using_fallback: bool = false

# Orbit state (used in orbit mode).
var _orbit_yaw: float = 0.0
var _orbit_pitch: float = -0.25
var _orbit_distance: float = 30.0
var _orbit_rotating: bool = false
var _orbit_last_mouse: Vector2 = Vector2.ZERO

# FOV state.
var _current_fov: float = 60.0

# Flight look-around state (right-click drag).
var _flight_yaw_offset: float = 0.0
var _flight_pitch_offset: float = 0.0
var _flight_rotating: bool = false
var _flight_last_mouse: Vector2 = Vector2.ZERO
const FLIGHT_LOOK_SENSITIVITY: float = 0.003
const FLIGHT_LOOK_RETURN_SPEED: float = 3.0
const FLIGHT_MAX_PITCH: float = 1.4
const FLIGHT_MAX_YAW: float = PI

# Seamless zoom: unified altitude variable controls camera height continuously.
# Below PAN_THRESHOLD: PhantomCamera SIMPLE follow (chase mode).
# Above PAN_THRESHOLD: manual position, WASD panning, galaxy map behavior.
const ALTITUDE_MIN: float = 8.0
const ALTITUDE_MAX: float = 15000.0
const PAN_THRESHOLD: float = 200.0    # Above this: WASD panning, manual drive.
const OVERLAY_THRESHOLD: float = 500.0 # Above this: galaxy overlay rendering active.
const STRATEGIC_ALTITUDE: float = 1800.0  # TAB jump target — tuned for galaxy overview at 25x scale.
const GALAXY_MAP_PAN_SPEED: float = 2000.0
const GALAXY_MAP_LERP_SPEED: float = 4.0
var _altitude: float = 80.0  # Unified altitude (replaces flight_follow_distance + galaxy_map_altitude).
var _pre_strategic_altitude: float = 80.0  # Altitude before TAB jump, for TAB toggle-back.
var _pre_transit_altitude: float = 80.0   # Altitude before lane transit, for post-transit restore.
var _galaxy_map_pan_offset: Vector3 = Vector3.ZERO
var _galaxy_panning: bool = false
var _galaxy_pan_last_mouse: Vector2 = Vector2.ZERO
var _galaxy_map_active: bool = false

var _tab_tween: Tween = null
var _pre_transit_altitude_set: bool = false  # True if game_manager pre-saved the pre-transit altitude.
# GalaxyView reference for LOD updates.
var _galaxy_view = null
# Throttle _sync_transit_lod: only call GalaxyView when altitude changes meaningfully.
var _last_transit_lod_alt: float = -1.0

# GameManager reference for state polling.
var _game_manager = null

# SimBridge reference for combat status polling.
var _bridge = null

# Combat auto-zoom state.
var _combat_zoom_active: bool = false
var _pre_combat_distance: float = 80.0
const COMBAT_ZOOM_OUT_DELTA: float = 40.0
const COMBAT_ZOOM_OUT_DURATION: float = 0.5
const COMBAT_ZOOM_IN_DURATION: float = 1.0
var _combat_zoom_tween: Tween = null

# Cinematic lock — blocks player camera input when true.
var cinematic_active: bool = false

# Cinematic orbit state — when set, camera orbits this point instead of following player.
var cinematic_orbit_center: Vector3 = Vector3.ZERO
var cinematic_orbit_angle: float = 0.0    # Current angle (radians).
var cinematic_orbit_radius: float = 80.0  # XZ distance from center.
var cinematic_orbit_altitude: float = 80.0 # Y height above center.
var cinematic_orbit_active: bool = false
var cinematic_orbit_blend: float = 0.0  # 0=top-down (matches transit), 1=full angled orbit
var warp_transit_tilt: float = 0.0  # 0=top-down, 1=forward-looking chase cam

func _ready() -> void:
	_target = _resolve_target()
	if _target == null:
		push_warning("[FollowCamera] Target not found. Assign target_path or ensure Player is in group 'Player'.")

	# Try to find a PhantomCamera3D child node.
	_pcam = _find_phantom_camera()
	if _pcam != null:
		_pcam_available = true
		_setup_phantom_camera_flight()
	else:
		# Fallback: create a basic Camera3D as child.
		push_warning("[FollowCamera] PhantomCamera3D not found — using fallback Camera3D.")
		_using_fallback = true
		_fallback_cam = Camera3D.new()
		_fallback_cam.name = "FallbackCamera"
		_fallback_cam.fov = fov_base
		_fallback_cam.current = true
		add_child(_fallback_cam)
		_fallback_cam.set_as_top_level(true)
		if _target:
			_fallback_cam.global_position = _target.global_position + flight_offset
			_fallback_cam.look_at(_target.global_position, Vector3.UP)

	_current_fov = fov_base
	_orbit_distance = orbit_distance

	# Find GameManager for state polling.
	_game_manager = _find_game_manager()
	_bridge = get_node_or_null("/root/SimBridge")

func _physics_process(delta: float) -> void:
	if _target == null:
		_target = _resolve_target()
		if _target == null:
			return

	if _game_manager == null:
		_game_manager = _find_game_manager()

	# Cinematic orbit: bypass ALL normal camera logic (mode switching, altitude
	# sync, overlay state). The cinematic owns hero visibility, HUD, and camera.
	if cinematic_orbit_active:
		_update_cinematic_orbit(delta)
		_apply_shake_offset(delta)
		return

	# Poll game state and switch modes accordingly.
	_poll_and_switch_mode()
	_poll_combat_zoom()
	_sync_altitude()

	if _using_fallback:
		_fallback_process(delta)
	elif _pcam_available:
		_pcam_process(delta)

	# Apply camera shake after all camera movement.
	_apply_shake_offset(delta)

func _unhandled_input(event: InputEvent) -> void:
	if cinematic_active:
		return
	if event is InputEventKey:
		var k := event as InputEventKey
		if k.pressed and not k.echo and k.keycode == reset_keycode:
			_reset_orbit()
			get_viewport().set_input_as_handled()
			return

	# Orbit mode input: mouse drag and zoom.
	if _current_mode == CameraMode.ORBIT:
		if event is InputEventMouseButton:
			var mb := event as InputEventMouseButton
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP and mb.pressed:
				_orbit_distance = clamp(_orbit_distance - orbit_zoom_step, orbit_min_distance, orbit_max_distance)
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN and mb.pressed:
				_orbit_distance = clamp(_orbit_distance + orbit_zoom_step, orbit_min_distance, orbit_max_distance)
			elif mb.button_index == MOUSE_BUTTON_RIGHT:
				_orbit_rotating = mb.pressed
				_orbit_last_mouse = mb.position

		if event is InputEventMouseMotion and _orbit_rotating:
			var mm := event as InputEventMouseMotion
			var mouse_delta := mm.position - _orbit_last_mouse
			_orbit_last_mouse = mm.position
			_orbit_yaw -= mouse_delta.x * orbit_sensitivity
			_orbit_pitch -= mouse_delta.y * orbit_sensitivity
			_orbit_pitch = clamp(_orbit_pitch, orbit_min_pitch, orbit_max_pitch)

	# Seamless zoom: unified scroll for FLIGHT and GALAXY_MAP.
	if _current_mode == CameraMode.FLIGHT or _current_mode == CameraMode.GALAXY_MAP:
		if event is InputEventMouseButton:
			var mb := event as InputEventMouseButton
			if mb.pressed:
				if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
					_altitude = clampf(_altitude - _compute_zoom_step(), ALTITUDE_MIN, ALTITUDE_MAX)
					_sync_altitude()
				elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
					_altitude = clampf(_altitude + _compute_zoom_step(), ALTITUDE_MIN, ALTITUDE_MAX)
					_sync_altitude()
				elif mb.button_index == MOUSE_BUTTON_RIGHT:
					_flight_rotating = mb.pressed
					_flight_last_mouse = mb.position
				elif mb.button_index == MOUSE_BUTTON_LEFT and _altitude >= PAN_THRESHOLD:
					_galaxy_panning = true
					_galaxy_pan_last_mouse = mb.position
			elif not mb.pressed:
				if mb.button_index == MOUSE_BUTTON_RIGHT:
					_flight_rotating = false
				elif mb.button_index == MOUSE_BUTTON_LEFT:
					_galaxy_panning = false
		if event is InputEventMouseMotion:
			var mm := event as InputEventMouseMotion
			# Galaxy map: left-click drag pans the map view.
			if _galaxy_panning and _altitude >= PAN_THRESHOLD:
				var mouse_delta := mm.position - _galaxy_pan_last_mouse
				_galaxy_pan_last_mouse = mm.position
				var pan_scale: float = _altitude * 0.003
				_galaxy_map_pan_offset.x -= mouse_delta.x * pan_scale
				_galaxy_map_pan_offset.z -= mouse_delta.y * pan_scale
			# Right-click drag: rotate view (flight) or orbit (galaxy map future).
			if _flight_rotating:
				var mouse_delta := mm.position - _flight_last_mouse
				_flight_last_mouse = mm.position
				_flight_yaw_offset = clamp(_flight_yaw_offset - mouse_delta.x * FLIGHT_LOOK_SENSITIVITY, -FLIGHT_MAX_YAW, FLIGHT_MAX_YAW)
				_flight_pitch_offset = clamp(_flight_pitch_offset - mouse_delta.y * FLIGHT_LOOK_SENSITIVITY, -FLIGHT_MAX_PITCH, FLIGHT_MAX_PITCH)


# ── Mode switching ──

func _poll_and_switch_mode() -> void:
	if _game_manager == null:
		return

	var state_val = _game_manager.get("current_player_state")

	# Lane transit takes priority — zoom out to galaxy map with custom framing.
	if state_val != null and int(state_val) == 2:  # IN_LANE_TRANSIT
		if _current_mode != CameraMode.WARP_TRANSIT:
			if not _pre_transit_altitude_set:
				_pre_transit_altitude = _altitude  # Save for restoration after transit.
			_pre_transit_altitude_set = false
			_switch_mode(CameraMode.WARP_TRANSIT)
		# Push transit altitude to LOD + overlay state (HUD, hero, galaxy flag).
		_sync_transit_lod()
		return

	# After warp transit ends, smooth transition to flight.
	if _current_mode == CameraMode.WARP_TRANSIT:
		_galaxy_map_pan_offset = Vector3.ZERO
		# Kill any altitude tweens from toggle_galaxy_map_overlay_v0 calls during transit.
		if _tab_tween and _tab_tween.is_valid():
			_tab_tween.kill()
		# Start from current transit altitude (not snap).
		var current_transit_alt: float = 500.0
		if _game_manager:
			var a = _game_manager.get("warp_transit_altitude")
			if a != null:
				current_transit_alt = float(a)
		_altitude = current_transit_alt
		flight_follow_distance = _altitude
		# Exit transit-mode rendering in GalaxyView.
		_ensure_galaxy_view()
		if _galaxy_view and _galaxy_view.has_method("SetTransitModeV0"):
			_galaxy_view.call("SetTransitModeV0", false, "", "")
		_switch_mode(CameraMode.FLIGHT)
		# Smooth arrival zoom: tween from transit altitude to flight altitude.
		var is_first_visit: bool = false
		if _game_manager:
			var fv = _game_manager.get("_last_arrival_first_visit")
			if fv != null:
				is_first_visit = bool(fv)
		if is_first_visit:
			# First visit: reveal sweep in WARP_TRANSIT mode already descended to
			# flight altitude (80). Camera is already at the right position.
			_altitude = current_transit_alt
			_sync_altitude()
			_sync_overlay_state()
			# Explicitly restore hero + HUD after transit.
			# _sync_overlay_state() only updates when the flag changes, but the hero
			# was hidden directly by _sync_transit_lod() — not through the flag.
			var hero = _game_manager.get("_hero_body") if _game_manager else null
			if hero and is_instance_valid(hero):
				hero.visible = true
			var tree = get_tree()
			if tree:
				var hud = tree.root.find_child("HUD", true, false)
				if hud and hud.has_method("set_overlay_mode_v0"):
					hud.call("set_overlay_mode_v0", false)
		else:
			# Return visit: smooth direct settle.
			_tab_tween = create_tween()
			_tab_tween.set_trans(Tween.TRANS_CUBIC)
			_tab_tween.set_ease(Tween.EASE_OUT)
			_tab_tween.tween_property(self, "_altitude", _pre_transit_altitude, 1.0)
			_sync_altitude()
			_sync_overlay_state()

	if state_val == null:
		return

	# Seamless zoom: altitude drives FLIGHT vs GALAXY_MAP mode.
	if int(state_val) == 0:  # IN_FLIGHT
		if _altitude >= PAN_THRESHOLD:
			if _current_mode != CameraMode.GALAXY_MAP:
				_switch_mode(CameraMode.GALAXY_MAP)
		else:
			if _current_mode != CameraMode.FLIGHT:
				_switch_mode(CameraMode.FLIGHT)
		# Sync galaxy_overlay_open to GameManager for other systems to read.
		_sync_overlay_state()
		return

	# Map DOCKED state to camera mode.
	if int(state_val) == 1:  # DOCKED
		var dock_kind = _game_manager.get("dock_target_kind_token")
		var new_mode: CameraMode
		if dock_kind != null and str(dock_kind) == "STATION":
			new_mode = CameraMode.STATION
		else:
			new_mode = CameraMode.ORBIT
		if new_mode != _current_mode:
			_switch_mode(new_mode)

func _switch_mode(new_mode: CameraMode) -> void:
	_current_mode = new_mode
	match new_mode:
		CameraMode.FLIGHT:
			_enter_flight_mode()
		CameraMode.ORBIT:
			_enter_orbit_mode()
		CameraMode.STATION:
			_enter_station_mode()
		CameraMode.GALAXY_MAP:
			_enter_galaxy_map_mode()
		CameraMode.WARP_TRANSIT:
			_enter_warp_transit_mode()


# ── PhantomCamera3D mode setup ──

func _enter_flight_mode() -> void:
	if _pcam_available and _pcam:
		# SIMPLE follow with damping behind the ship.
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 2  # FollowMode.SIMPLE
		if _pcam.has_method("set_follow_target"):
			_pcam.set_follow_target(_target)
		if _pcam.has_method("set_follow_offset"):
			_pcam.set_follow_offset(flight_offset)
		if _pcam.has_method("set_follow_distance"):
			_pcam.set_follow_distance(flight_follow_distance)
		if _pcam.has_method("set_follow_damping"):
			_pcam.set_follow_damping(true)
		if _pcam.has_method("set_follow_damping_value"):
			_pcam.set_follow_damping_value(flight_damping)
		# Look at the ship.
		if _pcam.has_method("set_look_at_target"):
			_pcam.set_look_at_target(_target)

func _enter_orbit_mode() -> void:
	_reset_orbit()
	if _pcam_available and _pcam:
		# Disable PhantomCamera follow — we drive position manually in orbit.
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 0  # FollowMode.NONE

func _enter_station_mode() -> void:
	if _pcam_available and _pcam:
		# Fixed offset above the target.
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 2  # FollowMode.SIMPLE
		if _pcam.has_method("set_follow_target"):
			_pcam.set_follow_target(_target)
		if _pcam.has_method("set_follow_offset"):
			_pcam.set_follow_offset(station_offset)
		if _pcam.has_method("set_follow_damping"):
			_pcam.set_follow_damping(false)
		if _pcam.has_method("set_look_at_target"):
			_pcam.set_look_at_target(_target)

# Galaxy map mode — entered when altitude crosses PAN_THRESHOLD.
func _enter_galaxy_map_mode() -> void:
	_galaxy_map_active = true
	# Disable PhantomCamera follow so we can drive position manually.
	if _pcam_available and _pcam:
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 0  # FollowMode.NONE
		if _pcam.has_method("set_look_at_target"):
			_pcam.set_look_at_target(null)

func _exit_galaxy_map_mode() -> void:
	_galaxy_map_active = false
	_galaxy_map_pan_offset = Vector3.ZERO

## Logarithmic zoom step: small at close range, larger at galaxy scale.
func _compute_zoom_step() -> float:
	if _altitude < 100.0:
		return 3.0
	elif _altitude < 500.0:
		return _altitude * 0.1
	else:
		return _altitude * 0.08

## After altitude changes, sync PhantomCamera follow distance and GalaxyView LOD.
func _sync_altitude() -> void:
	# Sync follow distance when in chase range.
	if _altitude < PAN_THRESHOLD:
		flight_follow_distance = _altitude
		if _pcam_available and _pcam and _pcam.has_method("set_follow_distance"):
			_pcam.set_follow_distance(_altitude)

	# Push LOD state to GalaxyView (3D root visibility).
	_ensure_galaxy_view()
	if _galaxy_view and _galaxy_view.has_method("UpdateAltitudeLodV0"):
		_galaxy_view.call("UpdateAltitudeLodV0", _altitude)

## During warp transit, push transit altitude to LOD without modifying _altitude.
## Also sync overlay state (HUD, hero visibility, galaxy_overlay_open flag).
## Activates transit-mode rendering (only origin+dest nodes visible).
func _sync_transit_lod() -> void:
	var transit_alt: float = 500.0
	if _game_manager:
		var a = _game_manager.get("warp_transit_altitude")
		if a != null:
			transit_alt = float(a)
	# Throttle GalaxyView calls: only update when altitude changes meaningfully (>5u).
	# These calls are expensive and running them every frame causes frame drops.
	var alt_changed: bool = absf(transit_alt - _last_transit_lod_alt) > 5.0
	if alt_changed:
		_last_transit_lod_alt = transit_alt
		_ensure_galaxy_view()
		# Use actual transit altitude for LOD — as the camera descends on approach,
		# the destination system naturally appears (local system visible < 500u).
		if _galaxy_view and _galaxy_view.has_method("UpdateAltitudeLodV0"):
			_galaxy_view.call("UpdateAltitudeLodV0", transit_alt)
		# Only enable galaxy overlay once camera is high enough that labels/markers
		# are at a readable size. Below this threshold, labels are enormous and
		# create a jarring "label explosion" during zoom-out.
		var overlay_ready: bool = transit_alt >= 350.0
		if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
			_galaxy_view.call("SetOverlayOpenV0", overlay_ready)
		# Enable transit-mode rendering once overlay is active.
		if overlay_ready and _galaxy_view and _galaxy_view.has_method("SetTransitModeV0"):
			var origin_id: String = ""
			var dest_id: String = ""
			if _game_manager:
				var o = _game_manager.get("_lane_origin_node_id")
				if o != null:
					origin_id = str(o)
				var d = _game_manager.get("_lane_dest_node_id")
				if d != null:
					dest_id = str(d)
			_galaxy_view.call("SetTransitModeV0", true, origin_id, dest_id)
	# During transit: hero hidden, HUD hidden (set once). Overlay flag follows altitude gate.
	if _game_manager:
		# Always hide hero + HUD during transit regardless of altitude.
		var hero = _game_manager.get("_hero_body")
		if hero and is_instance_valid(hero) and hero.visible:
			hero.visible = false
		var tree = get_tree()
		if tree:
			var hud = tree.root.find_child("HUD", true, false)
			if hud and hud.has_method("set_overlay_mode_v0"):
				hud.call("set_overlay_mode_v0", true)
		# Sync overlay flag to match altitude gate (controls label rendering).
		var should_overlay: bool = transit_alt >= 350.0
		var current = _game_manager.get("galaxy_overlay_open")
		if current == null or bool(current) != should_overlay:
			_game_manager.set("galaxy_overlay_open", should_overlay)
			var autoload_gm = get_node_or_null("/root/GameManager")
			if autoload_gm and autoload_gm != _game_manager:
				autoload_gm.set("galaxy_overlay_open", should_overlay)

func _ensure_galaxy_view() -> void:
	if _galaxy_view == null:
		var tree = get_tree()
		if tree:
			_galaxy_view = tree.root.find_child("GalaxyView", true, false)

## Sync galaxy_overlay_open flag and overlay rendering state.
## Called each frame during IN_FLIGHT from _poll_and_switch_mode.
func _sync_overlay_state() -> void:
	if _game_manager == null:
		return
	var should_overlay: bool = _altitude >= OVERLAY_THRESHOLD
	var current = _game_manager.get("galaxy_overlay_open")
	if current == null or bool(current) != should_overlay:
		_game_manager.set("galaxy_overlay_open", should_overlay)
		# Also sync to autoload GameManager.
		var autoload_gm = get_node_or_null("/root/GameManager")
		if autoload_gm and autoload_gm != _game_manager:
			autoload_gm.set("galaxy_overlay_open", should_overlay)
		# Toggle overlay rendering (GalaxyView nodes/edges).
		_ensure_galaxy_view()
		if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
			_galaxy_view.call("SetOverlayOpenV0", should_overlay)
		# Update ship visibility.
		var hero = _game_manager.get("_hero_body")
		if hero and is_instance_valid(hero):
			hero.visible = not should_overlay
		# Update HUD.
		var tree = get_tree()
		if tree:
			var hud = tree.root.find_child("HUD", true, false)
			if hud and hud.has_method("set_overlay_mode_v0"):
				hud.call("set_overlay_mode_v0", should_overlay)
			var overlay_hud = tree.root.find_child("GalaxyOverlayHud", true, false)
			if overlay_hud and overlay_hud.has_method("set_overlay_visible"):
				overlay_hud.call("set_overlay_visible", should_overlay)

## TAB key: tween to strategic altitude or back to previous flight altitude.
func toggle_strategic_altitude_v0() -> void:
	if _tab_tween and _tab_tween.is_valid():
		_tab_tween.kill()
	_tab_tween = create_tween()
	_tab_tween.set_trans(Tween.TRANS_CUBIC)
	_tab_tween.set_ease(Tween.EASE_IN_OUT)

	if _altitude < PAN_THRESHOLD:
		# Going up to strategic view.
		_pre_strategic_altitude = _altitude
		_tab_tween.tween_property(self, "_altitude", STRATEGIC_ALTITUDE, 0.6)
	else:
		# Coming back down to flight.
		_galaxy_map_pan_offset = Vector3.ZERO
		_tab_tween.tween_property(self, "_altitude", _pre_strategic_altitude, 0.6)

## Tween camera altitude to a target over a duration. Used by game_manager for approach cinematic.
func tween_altitude_v0(target: float, duration: float) -> void:
	if _tab_tween and _tab_tween.is_valid():
		_tab_tween.kill()
	_tab_tween = create_tween()
	_tab_tween.set_trans(Tween.TRANS_CUBIC)
	_tab_tween.set_ease(Tween.EASE_IN_OUT)
	_tab_tween.tween_property(self, "_altitude", target, duration)

func _enter_warp_transit_mode() -> void:
	# Reset LOD throttle so first frame gets a full sync.
	_last_transit_lod_alt = -1.0
	# Disable PhantomCamera follow so we drive position manually.
	if _pcam_available and _pcam:
		if _pcam.has_method("set_follow_mode"):
			_pcam.follow_mode = 0  # FollowMode.NONE
		if _pcam.has_method("set_look_at_target"):
			_pcam.set_look_at_target(null)

func _update_warp_transit(_delta: float) -> void:
	# Read transit target and altitude from GameManager.
	var target_pos: Vector3 = Vector3.ZERO
	var altitude: float = 500.0
	if _game_manager:
		var t = _game_manager.get("warp_transit_target")
		if t != null:
			target_pos = t
		var a = _game_manager.get("warp_transit_altitude")
		if a != null:
			altitude = float(a)

	var tilt: float = clampf(warp_transit_tilt, 0.0, 1.0)

	# Get travel direction (constant throughout transit, stored by game_manager).
	var travel_dir: Vector3 = Vector3.ZERO
	if _game_manager:
		var td = _game_manager.get("warp_transit_travel_dir")
		if td != null and td.length() > 0.1:
			travel_dir = td

	# If travel direction is unknown, stay top-down regardless of tilt.
	if travel_dir == Vector3.ZERO:
		tilt = 0.0

	# Get destination position for forward-looking mode.
	var dest_pos: Vector3 = Vector3.ZERO
	if _game_manager:
		var dp = _game_manager.get("warp_transit_dest_pos")
		if dp != null:
			dest_pos = dp

	# === Top-down position (tilt=0): directly above marker with look-ahead ===
	var look_ahead := Vector3.ZERO
	var look_ahead_scale := clampf((altitude - 80.0) / 100.0, 0.0, 1.0)
	if look_ahead_scale > 0.01 and travel_dir.length() > 0.1:
		look_ahead = travel_dir * 80.0 * look_ahead_scale
	var top_down_pos := Vector3(target_pos.x + look_ahead.x, altitude, target_pos.z + look_ahead.z)
	var look_down := Vector3(target_pos.x, 0.0, target_pos.z)

	# === Forward position (tilt=1): chase cam behind marker, looking at destination ===
	# Like a comet approaching its star — destination grows dramatically as you rush in.
	var behind_dist: float = clampf(altitude * 0.4, 20.0, 200.0)
	var fwd_alt: float = clampf(altitude * 0.4, 25.0, 250.0)
	var fwd_pos := Vector3(
		target_pos.x - travel_dir.x * behind_dist,
		fwd_alt,
		target_pos.z - travel_dir.z * behind_dist
	)
	var look_fwd := Vector3(dest_pos.x, 5.0, dest_pos.z) if dest_pos != Vector3.ZERO else look_down

	# === Blend based on tilt ===
	var desired_pos := top_down_pos.lerp(fwd_pos, tilt)
	var look_target := look_down.lerp(look_fwd, tilt)
	var up_vec := Vector3.BACK.slerp(Vector3.UP, tilt)
	if up_vec.length_squared() < 0.01:
		up_vec = Vector3.UP

	var cam: Camera3D = null
	if _using_fallback and _fallback_cam:
		cam = _fallback_cam
	elif _pcam_available:
		cam = get_viewport().get_camera_3d()

	if cam == null:
		return

	# Snap camera — transit marker is smoothly tweened, so snapping produces smooth motion.
	cam.global_position = desired_pos
	cam.look_at(look_target, up_vec)
	cam.fov = 60.0

func _update_galaxy_map(delta: float) -> void:
	# Get the player star world position as our anchor point.
	var anchor: Vector3 = Vector3.ZERO
	if _target and is_instance_valid(_target):
		anchor = _target.global_position

	# WASD panning — speed scales proportional to altitude for consistent feel.
	var pan_speed_scaled: float = GALAXY_MAP_PAN_SPEED * (_altitude / STRATEGIC_ALTITUDE)
	if Input.is_key_pressed(KEY_W) or Input.is_action_pressed("ship_thrust_fwd"):
		_galaxy_map_pan_offset.z -= pan_speed_scaled * delta
	if Input.is_key_pressed(KEY_S) or Input.is_action_pressed("ship_thrust_back"):
		_galaxy_map_pan_offset.z += pan_speed_scaled * delta
	if Input.is_key_pressed(KEY_A) or Input.is_action_pressed("ship_turn_left"):
		_galaxy_map_pan_offset.x -= pan_speed_scaled * delta
	if Input.is_key_pressed(KEY_D) or Input.is_action_pressed("ship_turn_right"):
		_galaxy_map_pan_offset.x += pan_speed_scaled * delta

	# Compute desired camera position: above player anchor + pan offset, looking straight down.
	var desired_pos := anchor + _galaxy_map_pan_offset
	desired_pos.y = _altitude

	var cam: Camera3D = null
	if _using_fallback and _fallback_cam:
		cam = _fallback_cam
	elif _pcam_available:
		cam = get_viewport().get_camera_3d()

	if cam == null:
		return

	# Smooth lerp to desired position for cinematic feel.
	cam.global_position = cam.global_position.lerp(desired_pos, 1.0 - exp(-GALAXY_MAP_LERP_SPEED * delta))

	# Look straight down. Use Vector3.BACK as up-vector to avoid gimbal lock when looking along -Y.
	var look_point := Vector3(cam.global_position.x, 0.0, cam.global_position.z)
	cam.look_at(look_point, Vector3.BACK)

	# Use perspective projection with wide FOV for good galaxy overview.
	cam.fov = 60.0


# ── Per-frame processing ──

func _pcam_process(delta: float) -> void:
	match _current_mode:
		CameraMode.FLIGHT:
			_update_fov_pcam(delta)
		CameraMode.ORBIT:
			_update_orbit_pcam(delta)
		CameraMode.STATION:
			pass  # PhantomCamera3D handles station view via SIMPLE follow.
		CameraMode.GALAXY_MAP:
			_update_galaxy_map(delta)
		CameraMode.WARP_TRANSIT:
			_update_warp_transit(delta)

func _update_fov_pcam(_delta: float) -> void:
	# FOV swell based on ship velocity (flight mode only).
	var speed: float = 0.0
	if _target is RigidBody3D:
		speed = (_target as RigidBody3D).linear_velocity.length()
	var t: float = clamp(speed / 18.0, 0.0, 1.0)
	var target_fov: float = fov_base + fov_boost_max * t
	_current_fov = lerp(_current_fov, target_fov, 1.0 - exp(-fov_smooth * _delta))
	# Apply FOV to the viewport camera (PhantomCameraHost drives the actual Camera3D).
	var cam := get_viewport().get_camera_3d()
	if cam:
		cam.fov = _current_fov
	# Apply right-click look-around / cinematic orbit: rotate the follow offset by yaw/pitch.
	if _pcam_available and _pcam and _pcam.has_method("set_follow_offset"):
		var offset_dir := flight_offset.normalized()
		if absf(_flight_yaw_offset) > 0.001 or absf(_flight_pitch_offset) > 0.001:
			var rotated := offset_dir.rotated(Vector3.UP, _flight_yaw_offset)
			var right_vec := rotated.cross(Vector3.UP).normalized()
			rotated = rotated.rotated(right_vec, _flight_pitch_offset)
			_pcam.set_follow_offset(rotated * _altitude)
		else:
			_pcam.set_follow_offset(offset_dir * _altitude)
	# Sync follow distance from altitude (and combat zoom tween).
	if _pcam_available and _pcam and _pcam.has_method("set_follow_distance"):
		var dist: float = flight_follow_distance if _combat_zoom_active else _altitude
		_pcam.set_follow_distance(dist)

## Cinematic orbit: camera orbits a world-space point (star center) at a given radius/altitude.
## Angle, altitude, and radius are tweened by game_manager. This just positions the camera.
##
## cinematic_orbit_blend controls the camera orientation:
##   0.0 = top-down (Vector3.BACK up) — matches WARP_TRANSIT / FLIGHT camera exactly
##   1.0 = full angled orbit (Vector3.UP up) — dramatic 3D view of the system
## Smoothly tweening blend 0→1→0 creates seamless entry and exit from the cinematic.
func _update_cinematic_orbit(_delta: float) -> void:
	var cam: Camera3D = null
	if _using_fallback and _fallback_cam:
		cam = _fallback_cam
	elif get_viewport():
		cam = get_viewport().get_camera_3d()
	if cam == null:
		return

	var blend: float = clampf(cinematic_orbit_blend, 0.0, 1.0)

	# Full orbit position: on a circle around center at radius distance.
	var orbit_x: float = cinematic_orbit_center.x + cos(cinematic_orbit_angle) * cinematic_orbit_radius
	var orbit_z: float = cinematic_orbit_center.z + sin(cinematic_orbit_angle) * cinematic_orbit_radius
	var orbit_pos := Vector3(orbit_x, cinematic_orbit_altitude, orbit_z)

	# Top-down position: directly above center (matches WARP_TRANSIT / FLIGHT).
	var top_down_pos := Vector3(cinematic_orbit_center.x, cinematic_orbit_altitude, cinematic_orbit_center.z)

	# Blend between top-down and orbit positions.
	var desired := top_down_pos.lerp(orbit_pos, blend)

	# Smooth lerp for butter-smooth motion.
	cam.global_position = cam.global_position.lerp(desired, 1.0 - exp(-8.0 * _delta))

	# Look-at target: star center (slightly above ground for nicer framing).
	var look_target := Vector3(cinematic_orbit_center.x, 2.0, cinematic_orbit_center.z)

	# Up vector: blend between top-down and angled orientations.
	# At blend=0: Vector3.BACK (looking straight down, matches transit/flight).
	# At blend=1: Vector3.UP (normal 3D orientation for dramatic angled view).
	var up_vec := Vector3.BACK.slerp(Vector3.UP, blend)
	if up_vec.length_squared() < 0.01:
		up_vec = Vector3.UP  # Safety fallback.

	cam.look_at(look_target, up_vec)
	cam.fov = 60.0


func _update_orbit_pcam(delta: float) -> void:
	# Manual orbit: drive the PhantomCamera3D global_position around target.
	if _target == null:
		return
	var tpos := _target.global_position
	var desired := _orbit_desired_position(tpos)
	if _pcam:
		_pcam.global_position = _pcam.global_position.lerp(desired, 1.0 - exp(-5.0 * delta))
		_pcam.look_at(tpos, Vector3.UP)


# ── Fallback Camera3D processing ──

func _fallback_process(delta: float) -> void:
	if _fallback_cam == null or _target == null:
		return

	# GATE.S13.CAMERA.PERSIST.001: Camera holds rotation on mouse release.
	# No snap-back — yaw/pitch offsets persist until next right-click drag.

	match _current_mode:
		CameraMode.FLIGHT:
			# Fixed top-down camera: directly above player, looking straight down.
			# No chase/follow lag — camera is rigidly locked to player position.
			# Right-click yaw offset shifts the view horizontally for look-around.
			var offset := Vector3(0.0, _altitude, 0.0)
			if absf(_flight_yaw_offset) > 0.001:
				offset.x += sin(_flight_yaw_offset) * _altitude * 0.3
				offset.z += (1.0 - cos(_flight_yaw_offset)) * _altitude * 0.3
			_fallback_cam.global_position = _target.global_position + offset
			# Look straight down — use BACK as up vector to avoid gimbal lock.
			var look_point := Vector3(_fallback_cam.global_position.x, 0.0, _fallback_cam.global_position.z)
			_fallback_cam.look_at(look_point, Vector3.BACK)
			_update_fov_fallback(delta)

		CameraMode.ORBIT:
			var tpos := _target.global_position
			var desired := _orbit_desired_position(tpos)
			_fallback_cam.global_position = _fallback_cam.global_position.lerp(desired, 1.0 - exp(-5.0 * delta))
			_fallback_cam.look_at(tpos, Vector3.UP)

		CameraMode.STATION:
			var desired_pos := _target.global_position + station_offset
			_fallback_cam.global_position = _fallback_cam.global_position.lerp(desired_pos, 1.0 - exp(-5.0 * delta))
			_fallback_cam.look_at(_target.global_position, Vector3.UP)

		CameraMode.GALAXY_MAP:
			_update_galaxy_map(delta)

		CameraMode.WARP_TRANSIT:
			_update_warp_transit(delta)

func _update_fov_fallback(delta: float) -> void:
	var speed: float = 0.0
	if _target is RigidBody3D:
		speed = (_target as RigidBody3D).linear_velocity.length()
	var t: float = clamp(speed / 18.0, 0.0, 1.0)
	var target_fov: float = fov_base + fov_boost_max * t
	_current_fov = lerp(_current_fov, target_fov, 1.0 - exp(-fov_smooth * delta))
	_fallback_cam.fov = _current_fov


# ── Orbit helpers ──

func _orbit_desired_position(center: Vector3) -> Vector3:
	var cp := cos(_orbit_pitch)
	var sp := sin(_orbit_pitch)
	var cy := cos(_orbit_yaw)
	var sy := sin(_orbit_yaw)
	var away := Vector3(sy * cp, sp, cy * cp).normalized()
	return center + away * _orbit_distance

func _reset_orbit() -> void:
	_orbit_yaw = 0.0
	_orbit_pitch = -0.25
	_orbit_distance = orbit_distance


# ── Target resolution ──

func _resolve_target() -> Node3D:
	if target_path != NodePath():
		var n = get_node_or_null(target_path)
		if n is Node3D:
			return n

	var tree = get_tree()
	if tree == null:
		return null
	var p = tree.get_first_node_in_group("Player")
	if p is Node3D:
		return p

	return null

func _find_phantom_camera() -> Node:
	# Look for a PhantomCamera3D child node.
	for child in get_children():
		if child.get_class() == "PhantomCamera3D":
			return child
		# Also check script class_name for GDScript-based PhantomCamera3D.
		if child.has_method("set_follow_target") and child.has_method("set_follow_mode"):
			return child

	# Try to find one in the scene tree as a sibling or nearby node.
	var parent = get_parent()
	if parent:
		for child in parent.get_children():
			if child == self:
				continue
			if child.get_class() == "PhantomCamera3D":
				return child
			if child.has_method("set_follow_target") and child.has_method("set_follow_mode"):
				return child

	return null

func _find_game_manager():
	# Try scene-child GameManager first, then autoload.
	var parent = get_parent()
	if parent:
		var gm = parent.get_node_or_null("GameManager")
		if gm:
			return gm
	var tree = get_tree()
	if tree == null:
		return null
	var gm_auto = tree.root.get_node_or_null("GameManager")
	if gm_auto:
		return gm_auto
	# Last resort: search the tree.
	var gm_found = tree.root.find_child("GameManager", true, false)
	return gm_found

func _setup_phantom_camera_flight() -> void:
	if _pcam == null:
		return
	# Snap PhantomCamera to the correct follow position immediately so it
	# doesn't lerp from global origin (top_level = true starts at 0,0,0).
	if _target:
		_pcam.global_position = _target.global_position + flight_offset
		_pcam.look_at(_target.global_position, Vector3.UP)
	# Initial setup: flight mode with follow on the target.
	if _pcam.has_method("set_follow_mode"):
		_pcam.follow_mode = 2  # FollowMode.SIMPLE
	if _target and _pcam.has_method("set_follow_target"):
		_pcam.set_follow_target(_target)
	if _pcam.has_method("set_follow_offset"):
		_pcam.set_follow_offset(flight_offset)
	if _pcam.has_method("set_follow_distance"):
		_pcam.set_follow_distance(flight_follow_distance)
	if _pcam.has_method("set_follow_damping"):
		_pcam.set_follow_damping(true)
	if _pcam.has_method("set_follow_damping_value"):
		_pcam.set_follow_damping_value(flight_damping)
	if _target and _pcam.has_method("set_look_at_target"):
		_pcam.set_look_at_target(_target)


# ── Combat auto-zoom ──

func _poll_combat_zoom() -> void:
	if _current_mode != CameraMode.FLIGHT:
		if _combat_zoom_active:
			_exit_combat_zoom()
		return
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetCombatStatusV0"):
		return
	var status: Dictionary = _bridge.call("GetCombatStatusV0")
	var in_combat: bool = status.get("in_combat", false)
	if in_combat and not _combat_zoom_active:
		_enter_combat_zoom()
	elif not in_combat and _combat_zoom_active:
		_exit_combat_zoom()

func _enter_combat_zoom() -> void:
	_combat_zoom_active = true
	_pre_combat_distance = flight_follow_distance
	if _combat_zoom_tween and _combat_zoom_tween.is_valid():
		_combat_zoom_tween.kill()
	_combat_zoom_tween = create_tween()
	_combat_zoom_tween.set_trans(Tween.TRANS_CUBIC)
	_combat_zoom_tween.set_ease(Tween.EASE_OUT)
	var target_dist: float = clamp(_pre_combat_distance + COMBAT_ZOOM_OUT_DELTA, orbit_min_distance, orbit_max_distance)
	_combat_zoom_tween.tween_property(self, "flight_follow_distance", target_dist, COMBAT_ZOOM_OUT_DURATION)

func _exit_combat_zoom() -> void:
	_combat_zoom_active = false
	if _combat_zoom_tween and _combat_zoom_tween.is_valid():
		_combat_zoom_tween.kill()
	_combat_zoom_tween = create_tween()
	_combat_zoom_tween.set_trans(Tween.TRANS_CUBIC)
	_combat_zoom_tween.set_ease(Tween.EASE_IN)
	_combat_zoom_tween.tween_property(self, "flight_follow_distance", _pre_combat_distance, COMBAT_ZOOM_IN_DURATION)


# ── Camera shake (GATE.S1.CAMERA.COMBAT_SHAKE.001) ──

var _shake_trauma: float = 0.0
var _shake_decay: float = 3.0
const SHAKE_MAX_OFFSET: float = 0.5
const SHAKE_MAX_ROTATION: float = 0.03

## Call to apply camera shake. intensity 0.0-1.0.
func apply_shake(intensity: float) -> void:
	_shake_trauma = clampf(_shake_trauma + intensity, 0.0, 1.0)

func _apply_shake_offset(delta: float) -> void:
	if _shake_trauma <= 0.0:
		return
	var shake_amount: float = _shake_trauma * _shake_trauma  # quadratic for snappier feel
	var cam := get_viewport().get_camera_3d() if not _using_fallback else _fallback_cam
	if cam == null:
		return
	var offset_x: float = randf_range(-1.0, 1.0) * SHAKE_MAX_OFFSET * shake_amount
	var offset_y: float = randf_range(-1.0, 1.0) * SHAKE_MAX_OFFSET * shake_amount
	var rot_z: float = randf_range(-1.0, 1.0) * SHAKE_MAX_ROTATION * shake_amount
	cam.h_offset = offset_x
	cam.v_offset = offset_y
	cam.rotation.z = rot_z
	_shake_trauma = maxf(0.0, _shake_trauma - _shake_decay * delta)
