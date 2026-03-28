extends Node3D

# GATE.S1.CAMERA.FOLLOW_MODES.001: Camera follow modes.
# Single Camera3D driven manually for all modes:
#   Flight (top-down fixed), Docked (fixed offset), Galaxy Map, Warp Transit, Cinematic.
# No camera rotation in any gameplay mode — fixed perspective for spatial clarity.
# Reference: Starcom Nexus, Starsector (industry standard for top-down space games).

# Tabs-only indentation policy applies.

enum CameraMode {
	FLIGHT,
	DOCKED,       # Fixed offset when docked at any target (station, planet, etc).
	GALAXY_MAP,   # GATE.S17.REAL_SPACE.GALAXY_MAP.001
	WARP_TRANSIT, # Galaxy-map zoom during lane transit.
}

@export var target_path: NodePath

# Flight mode: top-down (Starcom Nexus style) — camera directly above player.
# FH_3_FIX: Reduced from 120 to 60 — ship was 8-10px at 120u (AESTHETIC FAIL).
# System layout: planets 18-40u, belt 45u. 60u keeps ship visible + system context.
@export var flight_offset: Vector3 = Vector3(0, 60, 1)
@export var flight_follow_distance: float = 40.0

# Docked mode: fixed camera transform when docked at any target.
@export var dock_offset: Vector3 = Vector3(0, 40, 25)

# FOV swell (flight mode only).
@export var fov_base: float = 60.0
@export var fov_boost_max: float = 2.0
@export var fov_smooth: float = 4.0

# FOV cinematic swell: widen during warp transit approach, narrow on arrival.
const FLYBY_FOV_TRANSIT_BOOST: float = 8.0  # Max extra FOV degrees at low transit altitude.
const FLYBY_FOV_SMOOTH: float = 3.0         # Lerp speed for cinematic FOV transitions.

var _current_mode: CameraMode = CameraMode.FLIGHT
var _target: Node3D

# Camera3D — created in _ready, top_level for independent positioning.
var _cam: Camera3D = null

# FOV state.
var _current_fov: float = 60.0

# Seamless zoom: unified altitude variable controls camera height continuously.
# Below PAN_THRESHOLD: rigid top-down above player.
# Above PAN_THRESHOLD: manual position, WASD panning, galaxy map behavior.
const ALTITUDE_MIN: float = 8.0
const ALTITUDE_MAX: float = 15000.0  # FEEL_POST_FIX_6: Raised further — 8000 was insufficient for 25x galaxy bounding box.
const PAN_THRESHOLD: float = 200.0    # Above this: WASD panning, manual drive.
const OVERLAY_THRESHOLD: float = 500.0 # Above this: galaxy overlay rendering active.
const STRATEGIC_ALTITUDE: float = 5000.0  # FEEL_POST_BASELINE: Raised from 2500 so neighbor nodes are visible on map open.
const GALAXY_MAP_PAN_SPEED: float = 2000.0
const GALAXY_MAP_LERP_SPEED: float = 4.0
# GATE.T63.SPATIAL.CAMERA_TUNE.001: Flight altitude cap — prevents galaxy-map altitude
# contaminating flight mode. Must be < PAN_THRESHOLD. ~180u shows full system (lane gates at 85u).
const FLIGHT_ALTITUDE_MAX: float = 180.0
# Default flight altitude. System layout: planets 18-40u, belt 45u, lane gates 85u.
# FH_3_FIX: Reduced from 80 to 50 — closer to the action.
var _altitude: float = 50.0
var _pre_strategic_altitude: float = 50.0  # Altitude before TAB jump, for TAB toggle-back.
var _pre_transit_altitude: float = 50.0   # Altitude before lane transit, for post-transit restore.
var _galaxy_map_pan_offset: Vector3 = Vector3.ZERO
var _galaxy_panning: bool = false
var _galaxy_pan_last_mouse: Vector2 = Vector2.ZERO
var _galaxy_map_active: bool = false

var _tab_tween: Tween = null
var _pre_transit_altitude_set: bool = false  # True if game_manager pre-saved the pre-transit altitude.
var _flyby_arrival_handled: bool = false  # True if flyby cinematic already handled the arrival zoom.
# GalaxyView reference for LOD updates.
var _galaxy_view = null
# Throttle _sync_transit_lod: only call GalaxyView when altitude changes meaningfully.
var _last_transit_lod_alt: float = -1.0
# Throttle _sync_altitude LOD: only call UpdateAltitudeLodV0 when altitude changes by >5u.
var _last_lod_altitude: float = -999.0

# GameManager reference for state polling.
var _game_manager = null

# SimBridge reference for combat status polling.
var _bridge = null

# Cached node references — resolved once in _ready, avoids per-frame find_child().
var _hud_node = null
var _overlay_hud_node = null

# Combat auto-zoom state.
var _combat_zoom_active: bool = false
var _pre_combat_distance: float = 50.0
const COMBAT_ZOOM_OUT_DELTA: float = 20.0
const COMBAT_ZOOM_OUT_DURATION: float = 0.5
const COMBAT_ZOOM_IN_DURATION: float = 1.0
var _combat_zoom_tween: Tween = null

# Flyby state — driven by game_manager tweens during lane transit approach.
# When active, camera lerps to flyby_cam_pos and looks at flyby_look_at.
var flyby_active: bool = false
var flyby_cam_pos: Vector3 = Vector3.ZERO    # World-space camera position target.
var flyby_look_at: Vector3 = Vector3.ZERO    # World-space look-at target.
var flyby_up: Vector3 = Vector3.BACK         # Camera up vector.
var input_locked: bool = false               # Blocks camera input during cinematics.
var warp_transit_tilt: float = 0.0  # 0=top-down, 1=forward-looking chase cam

# === Universal camera spring — enforces C2 continuity on ALL transitions ===
# Every mode sets _spring_target_*. The spring moves _cam toward the target.
# This guarantees smooth transitions even when mode or target changes abruptly.
# THE ONLY place _cam.global_position is written is _update_spring().
var _spring_pos: Vector3 = Vector3.ZERO
var _spring_vel: Vector3 = Vector3.ZERO
var _spring_look: Vector3 = Vector3.ZERO
var _spring_look_vel: Vector3 = Vector3.ZERO
var _spring_up: Vector3 = Vector3.BACK
var _spring_up_vel: Vector3 = Vector3.ZERO
var _spring_initialized: bool = false
var _spring_target_pos: Vector3 = Vector3.ZERO
var _spring_target_look: Vector3 = Vector3.ZERO
var _spring_target_up: Vector3 = Vector3.BACK
# Stiffness per mode (higher omega = tighter tracking).
const SPRING_OMEGA_FLIGHT: float = 30.0     # Near-instant, player expects responsive.
const SPRING_OMEGA_DOCKED: float = 8.0      # Soft settle into dock view.
const SPRING_OMEGA_GALAXY: float = 8.0      # Smooth pan feel.
const SPRING_OMEGA_TRANSIT: float = 30.0    # Transit marker already smooth.
const SPRING_OMEGA_FLYBY: float = 30.0      # Flyby tweens already smooth.
const SPRING_OMEGA_CINEMATIC: float = 6.0   # Slow dramatic arcs (departure/arrival).
const SPRING_OMEGA_POST_CINEMATIC: float = 4.0  # Extra-soft settle after cinematic ends.
const POST_CINEMATIC_HOLD_TIME: float = 1.5     # Seconds to hold soft omega after cinematic.
var _spring_omega: float = SPRING_OMEGA_FLIGHT
var _post_cinematic_timer: float = 0.0          # Counts down after flyby/cinematic ends.
var _was_flyby_active: bool = false              # Edge detection for flyby→normal transition.

# Legacy compat — kept for game_manager references but no longer used for camera.
var flyby_settle_active: bool = false

# === 2.5D orbit/tilt state — RMB drag in galaxy map mode ===
var _orbit_yaw: float = 0.0          # Horizontal rotation around look-at (radians).
var _orbit_pitch: float = 0.0        # Vertical tilt (0 = top-down, max ~60°).
var _orbit_dragging: bool = false
var _orbit_last_mouse: Vector2 = Vector2.ZERO
var _orbit_tween: Tween = null
const ORBIT_PITCH_MIN: float = 0.0
const ORBIT_PITCH_MAX: float = 1.047        # ~60 degrees in radians.
const ORBIT_YAW_SENSITIVITY: float = 0.005  # Radians per pixel of mouse drag.
const ORBIT_PITCH_SENSITIVITY: float = 0.004
const ORBIT_RESET_DURATION: float = 0.4     # Seconds to tween back to top-down.
var _last_rmb_click_time: float = 0.0
const ORBIT_DOUBLE_CLICK_WINDOW: float = 0.3

func _ready() -> void:
	_target = _resolve_target()
	if _target == null:
		push_warning("[FollowCamera] Target not found. Assign target_path or ensure Player is in group 'Player'.")

	# Create a Camera3D as a top-level child — independent of player hierarchy.
	_cam = Camera3D.new()
	_cam.name = "GameCamera"
	_cam.fov = fov_base
	_cam.current = true
	add_child(_cam)
	_cam.set_as_top_level(true)
	if _target:
		_cam.global_position = _target.global_position + flight_offset
		_cam.look_at(_target.global_position, Vector3.UP)

	_current_fov = fov_base

	# Find GameManager for state polling.
	_game_manager = _find_game_manager()
	_bridge = get_node_or_null("/root/SimBridge")

	# Cache node references that were previously looked up via find_child() per frame.
	_cache_hud_refs()

func _physics_process(delta: float) -> void:
	if _target == null:
		_target = _resolve_target()
		if _target == null:
			return

	if _game_manager == null:
		_game_manager = _find_game_manager()

	# === Detect flyby→normal transition: start post-cinematic soft settle ===
	if _was_flyby_active and not flyby_active:
		_post_cinematic_timer = POST_CINEMATIC_HOLD_TIME
	_was_flyby_active = flyby_active
	if _post_cinematic_timer > 0.0:
		_post_cinematic_timer -= delta

	# === All mode logic sets _spring_target_* and _spring_omega ===
	if flyby_active:
		_spring_target_pos = flyby_cam_pos
		_spring_target_look = flyby_look_at
		_spring_target_up = flyby_up
		_spring_omega = SPRING_OMEGA_FLYBY
	else:
		flyby_settle_active = false  # Clear legacy flag.
		_poll_and_switch_mode()
		_poll_combat_zoom()
		_sync_altitude()
		_update_mode_targets(delta)

	# === Universal spring — THE ONLY place _cam.global_position is written ===
	_update_spring(delta)

	# FOV (separate from position).
	_update_fov_for_mode(delta)

	# Camera shake applied as offset AFTER spring.
	_apply_shake_offset(delta)

func _unhandled_input(event: InputEvent) -> void:
	if flyby_active or input_locked:
		return

	# Seamless zoom: unified scroll for FLIGHT and GALAXY_MAP.
	if _current_mode == CameraMode.FLIGHT or _current_mode == CameraMode.GALAXY_MAP:
		if event is InputEventMouseButton:
			var mb := event as InputEventMouseButton
			if mb.pressed:
				if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
					# GATE.T63.SPATIAL.CAMERA_TUNE.001: In flight mode, cap at FLIGHT_ALTITUDE_MAX.
					var max_alt: float = ALTITUDE_MAX if _current_mode == CameraMode.GALAXY_MAP else FLIGHT_ALTITUDE_MAX
					_altitude = clampf(_altitude - _compute_zoom_step(), ALTITUDE_MIN, max_alt)
					_sync_altitude()
				elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
					var max_alt_d: float = ALTITUDE_MAX if _current_mode == CameraMode.GALAXY_MAP else FLIGHT_ALTITUDE_MAX
					_altitude = clampf(_altitude + _compute_zoom_step(), ALTITUDE_MIN, max_alt_d)
					_sync_altitude()
				elif mb.button_index == MOUSE_BUTTON_LEFT and _altitude >= PAN_THRESHOLD:
					_galaxy_panning = true
					_galaxy_pan_last_mouse = mb.position
				elif mb.button_index == MOUSE_BUTTON_RIGHT and _altitude >= PAN_THRESHOLD:
					# 2.5D orbit: RMB drag rotates camera around galaxy.
					# Double-click resets to top-down.
					var now := Time.get_ticks_msec() / 1000.0
					if now - _last_rmb_click_time < ORBIT_DOUBLE_CLICK_WINDOW:
						_reset_orbit_to_top_down()
					_last_rmb_click_time = now
					_orbit_dragging = true
					_orbit_last_mouse = mb.position
					get_viewport().set_input_as_handled()
			elif not mb.pressed:
				if mb.button_index == MOUSE_BUTTON_LEFT:
					_galaxy_panning = false
				elif mb.button_index == MOUSE_BUTTON_RIGHT:
					_orbit_dragging = false
		if event is InputEventMouseMotion:
			var mm := event as InputEventMouseMotion
			# Galaxy map: left-click drag pans the map view.
			if _galaxy_panning and _altitude >= PAN_THRESHOLD:
				var mouse_delta := mm.position - _galaxy_pan_last_mouse
				_galaxy_pan_last_mouse = mm.position
				var pan_scale: float = _altitude * 0.003
				_galaxy_map_pan_offset.x -= mouse_delta.x * pan_scale
				_galaxy_map_pan_offset.z -= mouse_delta.y * pan_scale
			# 2.5D orbit: RMB drag rotates camera yaw/pitch.
			if _orbit_dragging and _altitude >= PAN_THRESHOLD:
				var orbit_delta := mm.position - _orbit_last_mouse
				_orbit_last_mouse = mm.position
				_orbit_yaw += orbit_delta.x * ORBIT_YAW_SENSITIVITY
				_orbit_pitch = clampf(_orbit_pitch - orbit_delta.y * ORBIT_PITCH_SENSITIVITY,
					ORBIT_PITCH_MIN, ORBIT_PITCH_MAX)


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
		# Exit transit-mode rendering in GalaxyView.
		_ensure_galaxy_view()
		if _galaxy_view and _galaxy_view.has_method("SetTransitModeV0"):
			_galaxy_view.call("SetTransitModeV0", false, "", "")
		_switch_mode(CameraMode.FLIGHT)
		if _flyby_arrival_handled:
			# Flyby cinematic already positioned the camera and set _altitude.
			# Just clean up the flag; no altitude reset needed.
			_flyby_arrival_handled = false
		else:
			# Non-flyby fallback: tween from transit altitude down to flight altitude.
			var current_transit_alt: float = 500.0
			if _game_manager:
				var a = _game_manager.get("warp_transit_altitude")
				if a != null:
					current_transit_alt = float(a)
			# Clamp to avoid triggering galaxy-map mode during the tween.
			_altitude = minf(current_transit_alt, PAN_THRESHOLD - 1.0)
			flight_follow_distance = _altitude
			_tab_tween = create_tween()
			_tab_tween.set_trans(Tween.TRANS_CUBIC)
			_tab_tween.set_ease(Tween.EASE_OUT)
			# GATE.X.UI_POLISH.CAMERA_BOUNDS.001: Clamp restore altitude to local system view on arrival.
			var restore_alt := minf(_pre_transit_altitude, PAN_THRESHOLD - 10.0)
			_tab_tween.tween_property(self, "_altitude", restore_alt, 1.0)
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

	# Map DOCKED state to camera mode — fixed offset for all dock types.
	if int(state_val) == 1:  # DOCKED
		if _current_mode != CameraMode.DOCKED:
			_switch_mode(CameraMode.DOCKED)

func _switch_mode(new_mode: CameraMode) -> void:
	# GATE.X.UI_POLISH.GALAXY_MAP_UX.001: Clear galaxy map state when leaving galaxy mode.
	if _current_mode == CameraMode.GALAXY_MAP and new_mode != CameraMode.GALAXY_MAP:
		_galaxy_map_active = false
		_orbit_dragging = false
	_current_mode = new_mode
	match new_mode:
		CameraMode.FLIGHT:
			# 2.5D: tween orbit back to top-down when returning to flight.
			_reset_orbit_to_top_down()
		CameraMode.DOCKED:
			_reset_orbit_to_top_down()
		CameraMode.GALAXY_MAP:
			_galaxy_map_active = true
		CameraMode.WARP_TRANSIT:
			_last_transit_lod_alt = -1.0  # Reset LOD throttle.
			_reset_orbit_to_top_down()

## Logarithmic zoom step: small at close range, larger at galaxy scale.
func _compute_zoom_step() -> float:
	if _altitude < 100.0:
		return 3.0
	elif _altitude < 500.0:
		return _altitude * 0.1
	else:
		return _altitude * 0.08

## After altitude changes, sync GalaxyView LOD.
## Throttled: only calls UpdateAltitudeLodV0 when altitude changes by >5u to avoid
## expensive per-frame LOD recalculations during smooth zoom tweens.
func _sync_altitude() -> void:
	if _altitude < PAN_THRESHOLD:
		flight_follow_distance = _altitude

	# Push LOD state to GalaxyView (3D root visibility) — throttled by delta.
	if absf(_altitude - _last_lod_altitude) > 5.0:
		_last_lod_altitude = _altitude
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
		# Use cached HUD ref instead of per-frame find_child().
		if _hud_node == null or not is_instance_valid(_hud_node):
			_cache_hud_refs()
		if _hud_node and is_instance_valid(_hud_node) and _hud_node.has_method("set_overlay_mode_v0"):
			_hud_node.call("set_overlay_mode_v0", true, true)  # is_transit=true — suppress "GALAXY MAP" label
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
	# FEEL_POST_FIX_5: Don't override HUD suppression when empire dashboard is open.
	var ed_open: bool = false
	var ed_val = _game_manager.get("empire_dashboard_open")
	if ed_val != null:
		ed_open = bool(ed_val)
	var should_overlay: bool = _altitude >= OVERLAY_THRESHOLD
	if ed_open and not should_overlay:
		return  # Empire dashboard is suppressing HUD — don't toggle it back on.
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
		# Update HUD — use cached refs instead of per-frame find_child().
		if _hud_node == null or not is_instance_valid(_hud_node):
			_cache_hud_refs()
		if _hud_node and is_instance_valid(_hud_node) and _hud_node.has_method("set_overlay_mode_v0"):
			_hud_node.call("set_overlay_mode_v0", should_overlay)
		if _overlay_hud_node == null or not is_instance_valid(_overlay_hud_node):
			_cache_hud_refs()
		if _overlay_hud_node and is_instance_valid(_overlay_hud_node) and _overlay_hud_node.has_method("set_overlay_visible"):
			_overlay_hud_node.call("set_overlay_visible", should_overlay)

## TAB key: tween to strategic altitude or back to previous flight altitude.
func toggle_strategic_altitude_v0() -> void:
	if _tab_tween and _tab_tween.is_valid():
		_tab_tween.kill()
	_tab_tween = create_tween()
	_tab_tween.set_trans(Tween.TRANS_CUBIC)
	_tab_tween.set_ease(Tween.EASE_IN_OUT)

	if _altitude < PAN_THRESHOLD:
		# Going up to strategic view.
		# GATE.T63.SPATIAL.CAMERA_TUNE.001: Clamp saved altitude to FLIGHT_ALTITUDE_MAX.
		_pre_strategic_altitude = minf(_altitude, FLIGHT_ALTITUDE_MAX)
		# FEEL_POST_FIX_6: Auto-fit altitude + center on galaxy centroid.
		var target_alt := STRATEGIC_ALTITUDE
		_ensure_galaxy_view()
		if _galaxy_view and _galaxy_view.has_method("GetAutoFitFrameV0"):
			var frame: Dictionary = _galaxy_view.call("GetAutoFitFrameV0")
			target_alt = float(frame.get("altitude", STRATEGIC_ALTITUDE))
			var cx: float = float(frame.get("center_x", 0.0))
			var cz: float = float(frame.get("center_z", 0.0))
			# Center camera on galaxy centroid, not player position.
			if _target and is_instance_valid(_target):
				_galaxy_map_pan_offset = Vector3(cx - _target.global_position.x, 0.0, cz - _target.global_position.z)
			else:
				_galaxy_map_pan_offset = Vector3(cx, 0.0, cz)
		_tab_tween.tween_property(self, "_altitude", target_alt, 0.6)
	else:
		# Coming back down to flight.
		_galaxy_map_pan_offset = Vector3.ZERO
		# GATE.T63.SPATIAL.CAMERA_TUNE.001: Ensure restore altitude is within flight range.
		var restore_to: float = minf(_pre_strategic_altitude, FLIGHT_ALTITUDE_MAX)
		_tab_tween.tween_property(self, "_altitude", restore_to, 0.6)

## Called by game_manager after flyby cinematic completes.
## Marks that the flyby already handled the arrival zoom so the WARP_TRANSIT exit
## in _poll_and_switch_mode skips the altitude reset.
func notify_flyby_arrival_v0(target_altitude: float) -> void:
	_flyby_arrival_handled = true
	# Don't override altitude if galaxy map is open (player or bot toggled it mid-transit).
	if _current_mode == CameraMode.GALAXY_MAP or _altitude >= PAN_THRESHOLD:
		return
	_altitude = target_altitude
	flight_follow_distance = _altitude
	_galaxy_map_pan_offset = Vector3.ZERO

## FOV punch: quick widen then return to base. Used for warp departure impact.
func punch_fov_v0(extra_degrees: float = 8.0, duration: float = 0.5) -> void:
	if _cam == null:
		return
	var peak_fov: float = _current_fov + extra_degrees
	var restore_fov: float = _current_fov
	var tw := create_tween()
	tw.tween_property(self, "_current_fov", peak_fov, duration * 0.3) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	tw.tween_property(self, "_current_fov", restore_fov, duration * 0.7) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_CUBIC)

## Tween camera altitude to a target over a duration. Used by game_manager for approach cinematic.
func tween_altitude_v0(target: float, duration: float) -> void:
	if _tab_tween and _tab_tween.is_valid():
		_tab_tween.kill()
	_tab_tween = create_tween()
	_tab_tween.set_trans(Tween.TRANS_CUBIC)
	_tab_tween.set_ease(Tween.EASE_IN_OUT)
	_tab_tween.tween_property(self, "_altitude", target, duration)


# ── Per-frame mode target update — sets _spring_target_* for the universal spring ──

func _update_mode_targets(delta: float) -> void:
	if _cam == null or _target == null:
		return

	match _current_mode:
		CameraMode.FLIGHT:
			_spring_target_pos = _target.global_position + Vector3(0.0, _altitude, 0.0)
			_spring_target_look = _target.global_position
			_spring_target_up = Vector3.BACK
			_spring_omega = SPRING_OMEGA_FLIGHT
			# FEEL_POST_FIX_4: Far clip scales with altitude to avoid clipping galaxy beacons.
			if _cam:
				_cam.far = 40000.0 if _altitude >= PAN_THRESHOLD else 10000.0

		CameraMode.DOCKED:
			_spring_target_pos = _target.global_position + dock_offset
			_spring_target_look = _target.global_position
			_spring_target_up = Vector3.UP
			_spring_omega = SPRING_OMEGA_DOCKED
			if _cam:
				_cam.far = 10000.0  # Neighbors visible while docked.

		CameraMode.GALAXY_MAP:
			_compute_galaxy_map_targets(delta)

		CameraMode.WARP_TRANSIT:
			_compute_warp_transit_targets(delta)

## Universal spring update — called ONCE per frame. THE ONLY place _cam.global_position is set.
## Critically-damped spring: no overshoot, fastest convergence, C2 continuous.
func _update_spring(delta: float) -> void:
	if _cam == null:
		return
	if not _spring_initialized:
		_spring_pos = _spring_target_pos
		_spring_look = _spring_target_look
		_spring_up = _spring_target_up
		_spring_vel = Vector3.ZERO
		_spring_look_vel = Vector3.ZERO
		_spring_up_vel = Vector3.ZERO
		_spring_initialized = true

	# Adaptive omega: soften during large target jumps (mode switches),
	# tighten as camera converges. This turns snappy mode switches into
	# visible smooth arcs while keeping steady-state tracking crisp.
	var displacement := _spring_pos.distance_to(_spring_target_pos)
	var omega := _spring_omega
	if displacement > 50.0:
		# Major mode switch (e.g. flight→galaxy, transit→flyby): full cinematic
		omega = SPRING_OMEGA_CINEMATIC
	elif displacement > 10.0:
		# Medium jump: blend between cinematic and mode omega
		var blend := (displacement - 10.0) / 40.0  # 0..1 over 10..50 range
		omega = lerpf(_spring_omega, SPRING_OMEGA_CINEMATIC, blend)
	# else: steady-state, use full mode omega for responsive tracking

	# Post-cinematic hold: keep omega extra-soft after flyby ends so the
	# camera settles gracefully back to flight view instead of snapping.
	if _post_cinematic_timer > 0.0:
		var hold_blend := _post_cinematic_timer / POST_CINEMATIC_HOLD_TIME  # 1→0
		omega = lerpf(omega, SPRING_OMEGA_POST_CINEMATIC, hold_blend)

	# Position spring: F = -omega^2 * displacement - 2*omega * velocity
	var d := _spring_pos - _spring_target_pos
	var f := -(omega * omega) * d - 2.0 * omega * _spring_vel
	_spring_vel += f * delta
	_spring_pos += _spring_vel * delta
	# Look-at spring
	d = _spring_look - _spring_target_look
	f = -(omega * omega) * d - 2.0 * omega * _spring_look_vel
	_spring_look_vel += f * delta
	_spring_look += _spring_look_vel * delta
	# Up vector spring
	d = _spring_up - _spring_target_up
	f = -(omega * omega) * d - 2.0 * omega * _spring_up_vel
	_spring_up_vel += f * delta
	_spring_up += _spring_up_vel * delta

	_cam.global_position = _spring_pos
	var up := _spring_up.normalized()
	if up.length_squared() < 0.01:
		up = Vector3.BACK
	if _spring_look.distance_squared_to(_spring_pos) > 0.1:
		_cam.look_at(_spring_look, up)

## FOV update dispatched per mode.
func _update_fov_for_mode(delta: float) -> void:
	if _cam == null:
		return
	match _current_mode:
		CameraMode.FLIGHT:
			var speed: float = 0.0
			if _target is RigidBody3D:
				speed = (_target as RigidBody3D).linear_velocity.length()
			var ref_speed: float = 18.0
			var fov_boost: float = fov_boost_max
			var t: float = clamp(speed / ref_speed, 0.0, 1.0)
			var target_fov: float = fov_base + fov_boost * t
			_current_fov = lerp(_current_fov, target_fov, 1.0 - exp(-fov_smooth * delta))
		CameraMode.DOCKED:
			_current_fov = lerpf(_current_fov, fov_base, 1.0 - exp(-fov_smooth * delta))
		CameraMode.GALAXY_MAP:
			_current_fov = lerpf(_current_fov, 60.0, 1.0 - exp(-fov_smooth * delta))
			_cam.far = 40000.0
		CameraMode.WARP_TRANSIT:
			var transit_alt: float = 500.0
			if _game_manager:
				var a = _game_manager.get("warp_transit_altitude")
				if a != null:
					transit_alt = float(a)
			var transit_fov_t: float = 1.0 - clampf(transit_alt / 1200.0, 0.0, 1.0)
			var target_transit_fov: float = fov_base + FLYBY_FOV_TRANSIT_BOOST * transit_fov_t
			_current_fov = lerpf(_current_fov, target_transit_fov, 1.0 - exp(-FLYBY_FOV_SMOOTH * delta))
	if flyby_active:
		_current_fov = lerpf(_current_fov, fov_base, 1.0 - exp(-FLYBY_FOV_SMOOTH * delta))
	_cam.fov = _current_fov

func _compute_warp_transit_targets(_delta: float) -> void:
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

	var travel_dir: Vector3 = Vector3.ZERO
	if _game_manager:
		var td = _game_manager.get("warp_transit_travel_dir")
		if td != null and td.length() > 0.1:
			travel_dir = td
	if travel_dir == Vector3.ZERO:
		tilt = 0.0

	var dest_pos: Vector3 = Vector3.ZERO
	if _game_manager:
		var dp = _game_manager.get("warp_transit_dest_pos")
		if dp != null:
			dest_pos = dp

	# Top-down position (tilt=0).
	var look_ahead := Vector3.ZERO
	var look_ahead_scale := clampf((altitude - 80.0) / 100.0, 0.0, 1.0)
	if look_ahead_scale > 0.01 and travel_dir.length() > 0.1:
		look_ahead = travel_dir * 80.0 * look_ahead_scale
	var top_down_pos := Vector3(target_pos.x + look_ahead.x, altitude, target_pos.z + look_ahead.z)
	var look_down := Vector3(target_pos.x, 0.0, target_pos.z)

	# Forward position (tilt=1): chase cam behind marker.
	var behind_dist: float = clampf(altitude * 0.4, 20.0, 200.0)
	var fwd_alt: float = clampf(altitude * 0.4, 25.0, 250.0)
	var fwd_pos := Vector3(
		target_pos.x - travel_dir.x * behind_dist,
		fwd_alt,
		target_pos.z - travel_dir.z * behind_dist
	)
	var look_fwd := Vector3(dest_pos.x, 5.0, dest_pos.z) if dest_pos != Vector3.ZERO else look_down

	# Blend based on tilt → set spring targets.
	_spring_target_pos = top_down_pos.lerp(fwd_pos, tilt)
	_spring_target_look = look_down.lerp(look_fwd, tilt)
	var up_vec := Vector3.BACK.slerp(Vector3.UP, tilt)
	if up_vec.length_squared() < 0.01:
		up_vec = Vector3.UP
	_spring_target_up = up_vec
	_spring_omega = SPRING_OMEGA_TRANSIT

func _compute_galaxy_map_targets(_delta: float) -> void:
	var anchor: Vector3 = Vector3.ZERO
	if _target and is_instance_valid(_target):
		anchor = _target.global_position

	# Galaxy map pans by click-drag only (handled in _unhandled_input).
	# WASD is reserved for ship controls — no keyboard panning on the map.

	var look_at_point := anchor + _galaxy_map_pan_offset
	look_at_point.y = 0.0  # Look at the galactic plane.

	# 2.5D orbit: spherical offset from orbit yaw/pitch.
	# pitch=0 → directly above (top-down, identical to previous behavior).
	# pitch>0 → tilted view, yaw rotates around the look-at point.
	var cam_offset := Vector3.ZERO
	cam_offset.x = _altitude * sin(_orbit_pitch) * sin(_orbit_yaw)
	cam_offset.y = _altitude * cos(_orbit_pitch)
	cam_offset.z = _altitude * sin(_orbit_pitch) * cos(_orbit_yaw)

	_spring_target_pos = look_at_point + cam_offset
	_spring_target_look = look_at_point
	# Up vector: blend from BACK (top-down) to UP (tilted) based on pitch.
	var pitch_t := clampf(_orbit_pitch / ORBIT_PITCH_MAX, 0.0, 1.0)
	_spring_target_up = Vector3.BACK.slerp(Vector3.UP, pitch_t)
	if _spring_target_up.length_squared() < 0.01:
		_spring_target_up = Vector3.UP
	_spring_omega = SPRING_OMEGA_GALAXY


# ── 2.5D orbit reset ──

## Tween orbit angles back to top-down. Called on mode switch and RMB double-click.
func _reset_orbit_to_top_down() -> void:
	if absf(_orbit_pitch) < 0.01 and absf(_orbit_yaw) < 0.01:
		return
	if _orbit_tween and _orbit_tween.is_valid():
		_orbit_tween.kill()
	_orbit_tween = create_tween()
	_orbit_tween.set_trans(Tween.TRANS_CUBIC)
	_orbit_tween.set_ease(Tween.EASE_OUT)
	_orbit_tween.set_parallel(true)
	_orbit_tween.tween_property(self, "_orbit_pitch", 0.0, ORBIT_RESET_DURATION)
	_orbit_tween.tween_property(self, "_orbit_yaw", 0.0, ORBIT_RESET_DURATION)


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

## Cache HUD and GalaxyOverlayHud references. Safe to call multiple times — only
## does the find_child() lookup when the cached ref is null or freed.
func _cache_hud_refs() -> void:
	if _hud_node == null or not is_instance_valid(_hud_node):
		var tree = get_tree()
		if tree:
			_hud_node = tree.root.find_child("HUD", true, false)
	if _overlay_hud_node == null or not is_instance_valid(_overlay_hud_node):
		var tree = get_tree()
		if tree:
			_overlay_hud_node = tree.root.find_child("GalaxyOverlayHud", true, false)


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
	var target_dist: float = clamp(_pre_combat_distance + COMBAT_ZOOM_OUT_DELTA, ALTITUDE_MIN, 200.0)
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
	if _cam == null:
		return
	var offset_x: float = randf_range(-1.0, 1.0) * SHAKE_MAX_OFFSET * shake_amount
	var offset_y: float = randf_range(-1.0, 1.0) * SHAKE_MAX_OFFSET * shake_amount
	var rot_z: float = randf_range(-1.0, 1.0) * SHAKE_MAX_ROTATION * shake_amount
	_cam.h_offset = offset_x
	_cam.v_offset = offset_y
	_cam.rotation.z = rot_z
	_shake_trauma = maxf(0.0, _shake_trauma - _shake_decay * delta)
