extends SceneTree
## Focused lane travel test: logs every step of the transit sequence.
## Captures screenshots at key moments: pre-transit, zoom-out, marker-moving, zoom-back-in, arrival.

const PREFIX := "LTEST|"
const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL,
	PRE_TRANSIT_CAPTURE,
	TRIGGER_TRANSIT,
	# During transit: capture every 10 frames for 200 frames
	TRANSIT_BURST,
	# After transit completes
	POST_TRANSIT_WAIT,
	POST_TRANSIT_CAPTURE,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _bridge = null
var _game_manager = null
var _screenshot = null
var _total_frames := 0
const MAX_FRAMES := 2400  # 40s at 60fps
const MAX_POLLS := 300

var _neighbor_ids: Array = []
var _capture_count := 0

# Transit burst state
var _burst_frame := 0
const BURST_TOTAL := 200
const BURST_SPACING := 10


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.DONE

	match _phase:
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
				if _polls >= MAX_POLLS:
					print(PREFIX + "FAIL|bridge_not_found")
					_phase = Phase.DONE

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_game_manager = root.get_node_or_null("GameManager")
				_screenshot = ScreenshotScript.new()
				_init_navigation()
				print(PREFIX + "READY|gm=%s" % str(_game_manager != null))
				_polls = 0
				_phase = Phase.WAIT_LOCAL
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					print(PREFIX + "FAIL|bridge_ready_timeout")
					_phase = Phase.DONE

		Phase.WAIT_LOCAL:
			if get_nodes_in_group("Station").size() > 0 or _polls >= 60:
				_polls = 0
				_phase = Phase.PRE_TRANSIT_CAPTURE
			else:
				_polls += 1

		Phase.PRE_TRANSIT_CAPTURE:
			_polls += 1
			if _polls >= 60:
				_log_state("PRE_TRANSIT")
				_capture("pre_transit")
				_polls = 0
				_phase = Phase.TRIGGER_TRANSIT

		Phase.TRIGGER_TRANSIT:
			_polls += 1
			if _polls >= 10:
				if _game_manager == null:
					print(PREFIX + "FAIL|no_game_manager")
					_phase = Phase.DONE
				elif _neighbor_ids.size() >= 1:
					print(PREFIX + "TRIGGERING_TRANSIT|dest=%s" % _neighbor_ids[0])
					_log_state("AT_TRIGGER")
					_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[0])
					_burst_frame = 0
					_phase = Phase.TRANSIT_BURST
				else:
					print(PREFIX + "FAIL|no_neighbors")
					_phase = Phase.DONE

		Phase.TRANSIT_BURST:
			_burst_frame += 1
			# Log state every 10 frames
			if _burst_frame % BURST_SPACING == 0:
				_log_state("TRANSIT_f%03d" % _burst_frame)
				_capture("transit_%03d" % _burst_frame)

			# Check if transit completed (state changed back to IN_FLIGHT)
			var state_val = _game_manager.get("current_player_state") if _game_manager else null
			if state_val != null and int(state_val) == 0 and _burst_frame > 20:  # IN_FLIGHT, after initial frames
				print(PREFIX + "TRANSIT_COMPLETE|frame=%d" % _burst_frame)
				_polls = 0
				_phase = Phase.POST_TRANSIT_WAIT

			if _burst_frame >= BURST_TOTAL:
				print(PREFIX + "TRANSIT_BURST_EXHAUSTED|frame=%d" % _burst_frame)
				_polls = 0
				_phase = Phase.POST_TRANSIT_WAIT

		Phase.POST_TRANSIT_WAIT:
			_polls += 1
			if _polls >= 60:
				_log_state("POST_TRANSIT")
				_capture("post_transit")
				_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


func _log_state(label: String) -> void:
	var state := "?"
	var warp_target := Vector3.ZERO
	var warp_alt := 0.0
	var galaxy_open := false
	var hero_pos := Vector3.ZERO
	var cam_pos := Vector3.ZERO
	var transit_marker_pos := "null"

	if _game_manager:
		var sv = _game_manager.get("current_player_state")
		if sv != null:
			match int(sv):
				0: state = "IN_FLIGHT"
				1: state = "DOCKED"
				2: state = "IN_LANE_TRANSIT"
		var wt = _game_manager.get("warp_transit_target")
		if wt != null: warp_target = wt
		var wa = _game_manager.get("warp_transit_altitude")
		if wa != null: warp_alt = float(wa)
		var go = _game_manager.get("galaxy_overlay_open")
		if go != null: galaxy_open = bool(go)
		var hb = _game_manager.get("_hero_body")
		if hb != null and is_instance_valid(hb):
			hero_pos = hb.global_position
		var tm = _game_manager.get("_transit_marker")
		if tm != null and is_instance_valid(tm):
			transit_marker_pos = str(tm.global_position)

	# Get camera position
	var cam = get_root().get_viewport().get_camera_3d()
	if cam:
		cam_pos = cam.global_position

	# Get camera controller mode
	var cam_mode := "?"
	var cam_ctrl = get_root().find_child("Camera3D", true, false)
	if cam_ctrl and cam_ctrl.get("_current_mode") != null:
		var mode_val = cam_ctrl.get("_current_mode")
		match int(mode_val):
			0: cam_mode = "FLIGHT"
			1: cam_mode = "ORBIT"
			2: cam_mode = "STATION"
			3: cam_mode = "GALAXY_MAP"
			4: cam_mode = "WARP_TRANSIT"
	var cam_alt := 0.0
	if cam_ctrl and cam_ctrl.get("_altitude") != null:
		cam_alt = float(cam_ctrl.get("_altitude"))

	# GalaxyView state
	var gv_local_vis := "?"
	var gv_stars_vis := "?"
	var gv_lanes_vis := "?"
	var gv = get_root().find_child("GalaxyView", true, false)
	if gv:
		# Check child node visibility
		for child in gv.get_children():
			if child.name == "LocalSystemRoot":
				gv_local_vis = str(child.visible)
			elif child.name == "PersistentStars":
				gv_stars_vis = str(child.visible)
			elif child.name == "PersistentLanes":
				gv_lanes_vis = str(child.visible)

	print(PREFIX + "%s|state=%s cam_mode=%s cam_alt=%.0f cam_pos=%s hero=%s warp_tgt=%s warp_alt=%.0f galaxy=%s marker=%s local=%s stars=%s lanes=%s" % [
		label, state, cam_mode, cam_alt,
		str(cam_pos), str(hero_pos), str(warp_target), warp_alt,
		str(galaxy_open), transit_marker_pos,
		gv_local_vis, gv_stars_vis, gv_lanes_vis
	])


func _capture(label: String) -> void:
	if _screenshot == null:
		return
	_capture_count += 1
	var path := "res://reports/visual_eval/lane_%s_%04d.png" % [label, _total_frames]
	_screenshot.capture_v0(self, path)
	print(PREFIX + "CAPTURE|%s|%s" % [label, path])


func _init_navigation() -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var home_id: String = str(ps.get("current_node_id", ""))
	print(PREFIX + "HOME|%s" % home_id)

	var galaxy_snap: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var lanes: Array = galaxy_snap.get("lane_edges", [])
	var seen := {}
	for lane in lanes:
		var from_id: String = str(lane.get("from_id", ""))
		var to_id: String = str(lane.get("to_id", ""))
		if from_id == home_id and not seen.has(to_id):
			_neighbor_ids.append(to_id)
			seen[to_id] = true
		elif to_id == home_id and not seen.has(from_id):
			_neighbor_ids.append(from_id)
			seen[from_id] = true

	print(PREFIX + "NEIGHBORS|%s" % str(_neighbor_ids))


func _quit() -> void:
	print(PREFIX + "DONE|captures=%d frames=%d" % [_capture_count, _total_frames])
	# Stop SimBridge thread.
	var bridge = root.get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(0)
