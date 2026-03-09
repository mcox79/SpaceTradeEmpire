extends SceneTree

## Lane Transfer Diagnostic Bot v2 — Burst-captures every stage of lane transit.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Strategy: After triggering gate transit, capture EVERY 3 frames for 600 frames
## (10 seconds). This catches departure flash, galaxy-map transit, arrival cinematic,
## and post-cinematic control handoff. Dense coverage for diagnosing jankiness.
##
## Then: observe NPC fleet movement for 10 seconds with periodic captures.
##
## Usage:
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/lane_transfer_diag_bot.gd"

const PREFIX := "LTDG|"
const OUTPUT_DIR := "res://reports/lane_transfer_diag/"
const MAX_FRAMES := 5400  # 90s at 60fps

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	# --- Pre-warp ---
	FLY_TO_GATE,
	PRE_WARP_SETTLE,
	PRE_WARP_CAPTURE,

	# --- The big burst: gate trigger through arrival completion ---
	TRIGGER_GATE,
	CONTINUOUS_BURST,       # Capture every N frames for the entire transit sequence

	# --- Post-transit ---
	POST_TRANSIT_SETTLE,
	POST_TRANSIT_CAPTURE,

	# --- NPC Fleet Observation ---
	NPC_OBSERVATION_SETUP,
	NPC_OBSERVATION_BURST,

	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _bridge = null
var _game_manager = null

var _observer = null
var _screenshot = null

var _home_node_id := ""
var _neighbor_ids: Array = []
var _snapshots: Array = []

# Continuous burst state
var _burst_frame := 0
var _burst_max := 0
var _burst_spacing := 0
var _transit_start_frame := 0

# Track state transitions for smart labeling
var _last_state := ""
var _cinematic_was_active := false


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.DONE

	match _phase:
		# ── Setup ──
		Phase.LOAD_SCENE:
			var scene = load("res://scenes/playable_prototype.tscn").instantiate()
			root.add_child(scene)
			print(PREFIX + "SCENE_LOADED")
			_polls = 0
			_phase = Phase.WAIT_SCENE

		Phase.WAIT_SCENE:
			_polls += 1
			if _polls >= 30:
				_polls = 0
				_phase = Phase.WAIT_BRIDGE

		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= 600:
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_observer = ObserverScript.new()
				_observer.init_v0(self)
				_screenshot = ScreenshotScript.new()
				_game_manager = root.get_node_or_null("GameManager")
				_init_navigation()
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= 600:
					_fail("bridge_not_ready")

		Phase.WAIT_LOCAL_SYSTEM:
			_polls += 1
			if _polls >= 60:
				_polls = 0
				_phase = Phase.FLY_TO_GATE

		# ── Pre-warp ──
		Phase.FLY_TO_GATE:
			if _neighbor_ids.is_empty():
				print(PREFIX + "FAIL|no_neighbors")
				_phase = Phase.NPC_OBSERVATION_SETUP
			else:
				_teleport_to_gate(_neighbor_ids[0])
				_polls = 0
				_phase = Phase.PRE_WARP_SETTLE

		Phase.PRE_WARP_SETTLE:
			_polls += 1
			if _polls >= 30:
				_polls = 0
				_phase = Phase.PRE_WARP_CAPTURE

		Phase.PRE_WARP_CAPTURE:
			_capture_with_state("01_pre_warp_at_gate")
			_polls = 0
			_phase = Phase.TRIGGER_GATE

		# ── Gate trigger + continuous burst ──
		Phase.TRIGGER_GATE:
			_polls += 1
			if _polls >= 5:
				if _game_manager and _neighbor_ids.size() >= 1:
					print(PREFIX + "TRIGGER_GATE|dest=%s" % _neighbor_ids[0])
					_transit_start_frame = _total_frames
					_last_state = "IN_FLIGHT"
					_cinematic_was_active = false
					_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[0])
					# Start continuous burst: capture every 5 frames for up to 3000 frames.
					# Headless runs at ~125fps, so covers ~24s: transit ~4.6s + cinematic 7s + post 12s.
					_burst_frame = 0
					_burst_max = 600
					_burst_spacing = 5
					_polls = 0
					_phase = Phase.CONTINUOUS_BURST
				else:
					print(PREFIX + "FAIL|no_game_manager_or_neighbors")
					_phase = Phase.NPC_OBSERVATION_SETUP

		Phase.CONTINUOUS_BURST:
			_polls += 1
			if _polls >= _burst_spacing:
				_polls = 0
				_burst_frame += 1
				var elapsed := _total_frames - _transit_start_frame
				var state_name := _get_player_state_name()
				var cam_info := _get_camera_info()
				var hero_info := _get_hero_info()
				var cinematic_now := _is_cinematic_active()

				# Detect state transitions and log them prominently
				if state_name != _last_state:
					print(PREFIX + "STATE_CHANGE|%s -> %s|frame=%d (%.2fs)" % [
						_last_state, state_name, elapsed, elapsed / 60.0])
					_last_state = state_name
				if cinematic_now != _cinematic_was_active:
					print(PREFIX + "CINEMATIC|%s -> %s|frame=%d" % [
						str(_cinematic_was_active), str(cinematic_now), elapsed])
					_cinematic_was_active = cinematic_now

				# Auto-label based on what's happening
				var label_prefix := "02_transit"
				if state_name == "IN_LANE_TRANSIT":
					label_prefix = "02_warp_transit"
				elif cinematic_now:
					label_prefix = "03_arrival_cinematic"
				elif state_name == "IN_FLIGHT" and elapsed > 10:
					label_prefix = "04_post_arrival"

				var label := "%s_f%03d" % [label_prefix, _burst_frame]
				_capture_with_state(label)

				# End burst: either hit max captures, or 5s after cinematic ends
				if _burst_frame >= _burst_max:
					_polls = 0
					_phase = Phase.POST_TRANSIT_SETTLE
				elif state_name == "IN_FLIGHT" and not cinematic_now and elapsed > 300:
					# Cinematic done and we've been in flight for a while
					print(PREFIX + "BURST_END|cinematic_done|elapsed=%d" % elapsed)
					_polls = 0
					_phase = Phase.POST_TRANSIT_SETTLE

		Phase.POST_TRANSIT_SETTLE:
			_polls += 1
			if _polls >= 30:
				_polls = 0
				_phase = Phase.POST_TRANSIT_CAPTURE

		Phase.POST_TRANSIT_CAPTURE:
			_capture_with_state("05_player_control_restored")
			var elapsed := _total_frames - _transit_start_frame
			print(PREFIX + "TRANSIT_COMPLETE|elapsed_frames=%d (%.1fs)|captures=%d" % [
				elapsed, elapsed / 60.0, _burst_frame])
			_polls = 0
			_phase = Phase.NPC_OBSERVATION_SETUP

		# ── NPC Fleet Observation ──
		Phase.NPC_OBSERVATION_SETUP:
			_setup_npc_observation()
			_polls = 0
			_burst_frame = 0
			_burst_max = 10
			_burst_spacing = 60  # 1s between captures
			_phase = Phase.NPC_OBSERVATION_BURST

		Phase.NPC_OBSERVATION_BURST:
			_polls += 1
			if _polls >= _burst_spacing:
				_polls = 0
				_burst_frame += 1
				_log_npc_positions()
				_capture_with_state("06_npc_fleet_f%02d" % _burst_frame)
				if _burst_frame >= _burst_max:
					_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# ── Navigation ──

func _init_navigation() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var lanes: Array = galaxy.get("lane_edges", [])
	var seen := {}
	for lane in lanes:
		var from_id: String = str(lane.get("from_id", ""))
		var to_id: String = str(lane.get("to_id", ""))
		if from_id == _home_node_id and not seen.has(to_id):
			_neighbor_ids.append(to_id)
			seen[to_id] = true
		elif to_id == _home_node_id and not seen.has(from_id):
			_neighbor_ids.append(from_id)
			seen[from_id] = true

	print(PREFIX + "NAV|home=%s neighbors=%d ids=%s" % [
		_home_node_id, _neighbor_ids.size(),
		str(_neighbor_ids).left(120)])


func _teleport_to_gate(neighbor_id: String) -> void:
	var hero = _get_hero_body()
	if hero == null:
		print(PREFIX + "WARN|no_hero_body")
		return

	var gv = root.find_child("GalaxyView", true, false)
	var gate_pos := Vector3.ZERO
	if gv and gv.has_method("GetGatePositionV0"):
		gate_pos = gv.call("GetGatePositionV0", neighbor_id)

	if gate_pos == Vector3.ZERO:
		var star_pos := Vector3.ZERO
		if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
			star_pos = gv.call("GetCurrentStarGlobalPositionV0")
		gate_pos = star_pos + Vector3(80, 0, 0)

	var star_center := Vector3.ZERO
	if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
		star_center = gv.call("GetCurrentStarGlobalPositionV0")
	var inward_dir := (star_center - gate_pos).normalized()
	hero.global_position = gate_pos + inward_dir * 12.0
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	hero.angular_velocity = Vector3.ZERO
	if hero.is_inside_tree() and gate_pos != Vector3.ZERO:
		hero.look_at(gate_pos, Vector3.UP)

	print(PREFIX + "TELEPORT|gate_pos=%s hero_pos=%s" % [str(gate_pos), str(hero.global_position)])


func _setup_npc_observation() -> void:
	var hero = _get_hero_body()
	if hero == null:
		print(PREFIX + "WARN|no_hero_for_npc_obs")
		return

	var npcs := get_nodes_in_group("NpcShip")
	if npcs.is_empty():
		print(PREFIX + "WARN|no_npc_ships_found")
		for group_name in ["NpcShip", "FleetShip", "Fleet"]:
			var g = get_nodes_in_group(group_name)
			print(PREFIX + "GROUP|%s|count=%d" % [group_name, g.size()])
		return

	var nearest: Node3D = npcs[0]
	var best_dist: float = hero.global_position.distance_to(nearest.global_position)
	for npc in npcs:
		var d: float = hero.global_position.distance_to(npc.global_position)
		if d < best_dist:
			best_dist = d
			nearest = npc

	hero.global_position = nearest.global_position + Vector3(20, 0, 0)
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	hero.angular_velocity = Vector3.ZERO

	var fleet_id := ""
	if nearest.get("fleet_id") != null:
		fleet_id = str(nearest.get("fleet_id"))
	var target_pos := Vector3.ZERO
	if nearest.get("target_position") != null:
		target_pos = nearest.target_position
	print(PREFIX + "NPC_SETUP|fleet_id=%s|npc_pos=%s|target=%s|speed=%s|state=%s|total_npcs=%d" % [
		fleet_id, str(nearest.global_position), str(target_pos),
		str(nearest.get("target_speed")), str(nearest.get("_fleet_state")),
		npcs.size()])


func _log_npc_positions() -> void:
	var npcs := get_nodes_in_group("NpcShip")
	for i in range(mini(npcs.size(), 5)):
		var npc: Node3D = npcs[i]
		var fleet_id := ""
		if npc.get("fleet_id") != null:
			fleet_id = str(npc.get("fleet_id"))
		var target_pos := Vector3.ZERO
		if npc.get("target_position") != null:
			target_pos = npc.target_position
		var vel := Vector3.ZERO
		if npc is CharacterBody3D:
			vel = npc.velocity
		print(PREFIX + "NPC_POS|%s|pos=%s|target=%s|vel=%s|speed=%.1f" % [
			fleet_id,
			_v3_short(npc.global_position),
			_v3_short(target_pos),
			_v3_short(vel),
			vel.length()])


# ── Capture ──

func _capture_with_state(label: String) -> void:
	var tick := _get_tick()
	var state_name := _get_player_state_name()
	var cam_info := _get_camera_info()
	var hero_info := _get_hero_info()

	var filename := "%s_%04d" % [label, tick]
	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)

	print(PREFIX + "CAPTURE|%s|tick=%d|state=%s|%s|%s" % [
		label, tick, state_name, cam_info, hero_info])

	_snapshots.append({
		"phase": label,
		"tick": tick,
		"screenshot": img_path,
		"player_state": state_name,
		"camera": cam_info,
		"hero": hero_info,
	})


func _get_player_state_name() -> String:
	if _game_manager == null:
		return "no_gm"
	var state_val = _game_manager.get("current_player_state")
	if state_val == null:
		return "null"
	match int(state_val):
		0: return "IN_FLIGHT"
		1: return "DOCKED"
		2: return "IN_LANE_TRANSIT"
		3: return "GATE_APPROACH"
		_: return "UNKNOWN_%d" % int(state_val)


func _is_cinematic_active() -> bool:
	var cam_ctrl = root.find_child("PlayerFollowCamera", true, false)
	if cam_ctrl == null:
		cam_ctrl = _find_camera_controller_fallback()
	if cam_ctrl == null:
		return false
	var fb = cam_ctrl.get("flyby_active")
	var il = cam_ctrl.get("input_locked")
	var fs = cam_ctrl.get("flyby_settle_active")
	return (bool(fb) if fb != null else false) or (bool(il) if il != null else false) or (bool(fs) if fs != null else false)


func _get_camera_info() -> String:
	var cam_ctrl = root.find_child("PlayerFollowCamera", true, false)
	if cam_ctrl == null:
		cam_ctrl = _find_camera_controller_fallback()
	if cam_ctrl == null:
		return "cam=null"
	var alt = cam_ctrl.get("_altitude")
	var yaw = cam_ctrl.get("_flight_yaw_offset")
	var pitch = cam_ctrl.get("_flight_pitch_offset")
	var flyby = cam_ctrl.get("flyby_active")
	var input_lock = cam_ctrl.get("input_locked")
	var mode = cam_ctrl.get("_current_mode")
	var flyby_pos = cam_ctrl.get("flyby_cam_pos")
	var flyby_look = cam_ctrl.get("flyby_look_at")
	var settle = cam_ctrl.get("flyby_settle_active")
	var fov_val = cam_ctrl.get("_current_fov")
	# Also read transit altitude from GameManager to see descent curve.
	var transit_alt_val = null
	if _game_manager:
		transit_alt_val = _game_manager.get("warp_transit_altitude")
	var info := "cam_alt=%.0f|yaw=%.2f|pitch=%.2f|flyby=%s|locked=%s|mode=%s|fov=%.1f|settle=%s" % [
		float(alt) if alt else 0.0,
		float(yaw) if yaw else 0.0,
		float(pitch) if pitch else 0.0,
		str(flyby),
		str(input_lock),
		str(mode),
		float(fov_val) if fov_val != null else 60.0,
		str(settle)]
	if transit_alt_val != null:
		info += "|transit_alt=%.0f" % float(transit_alt_val)
	if flyby and flyby_pos != null:
		info += "|fb_pos=%s|fb_look=%s" % [
			_v3_short(flyby_pos),
			_v3_short(flyby_look) if flyby_look != null else "null"]
	return info


func _find_camera_controller_fallback():
	# Try multiple search strategies
	for name in ["PlayerFollowCamera", "Camera3D", "CameraMount"]:
		var found = root.find_child(name, true, false)
		if found and found.get("_altitude") != null:
			return found
	# Try player group
	var players = get_nodes_in_group("Player")
	for p in players:
		var mount = p.get_node_or_null("CameraMount/Camera3D")
		if mount and mount.get("_altitude") != null:
			return mount
	return null


func _get_hero_info() -> String:
	var hero = _get_hero_body()
	if hero == null:
		return "hero=null"
	return "hero_pos=%s|hero_vis=%s" % [
		_v3_short(hero.global_position),
		str(hero.visible)]


func _get_hero_body():
	var players = get_nodes_in_group("Player")
	if players.size() > 0:
		return players[0]
	return null


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _v3_short(v: Vector3) -> String:
	return "(%.0f,%.0f,%.0f)" % [v.x, v.y, v.z]


func _fail(reason: String) -> void:
	print(PREFIX + "FAIL|%s" % reason)
	_phase = Phase.DONE


func _quit() -> void:
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var summary := {
		"bot": "lane_transfer_diag_v2",
		"total_frames": _total_frames,
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
	}
	var f := FileAccess.open(OUTPUT_DIR.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
	print(PREFIX + "SUMMARY|snapshots=%d|frames=%d" % [_snapshots.size(), _total_frames])

	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	print(PREFIX + "DONE")
	quit(0)
