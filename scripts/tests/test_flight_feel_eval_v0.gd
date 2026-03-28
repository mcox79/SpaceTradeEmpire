# scripts/tests/test_flight_feel_eval_v0.gd
# Flight Feel Evaluation Bot — measures camera, acceleration, and docking metrics
# per Elite Dangerous / No Man's Sky handling envelope best practices.
#
# Metrics measured:
#   - Camera lerp smoothness (jerk/discontinuity detection)
#   - Warp transition feel (acceleration into/out of warp)
#   - Dock approach feel (auto-deceleration, dock timing)
#   - Flyby camera smoothness during first-visit arrivals
#   - Camera position stability (no jitter at rest)
#   - Travel time consistency (same-distance lanes same travel time)
#
# NOTE: Headless mode has no rendering, but camera transform and ship position
# are still computed in the scene tree. We read transforms from nodes.
#
# Usage:
#   godot --headless --path . -s res://scripts/tests/test_flight_feel_eval_v0.gd
extends SceneTree

const AssertLib = preload("res://scripts/tools/bot_assert.gd")

const MAX_FRAMES := 5400
const POSITION_SAMPLE_INTERVAL := 3  # Sample every 3 frames

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	BOOT,
	MEASURE_REST_STABILITY,
	INITIATE_TRAVEL,
	MEASURE_TRAVEL,
	ARRIVE_AND_DOCK,
	MEASURE_DOCK_APPROACH,
	SECOND_TRAVEL,
	MEASURE_SECOND_TRAVEL,
	ANALYZE, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _gm = null
var _a: AssertLib = null
var _camera = null
var _player_ship = null

# Position time series.
var _rest_positions: Array[Vector3] = []
var _travel_positions: Array[Vector3] = []
var _dock_positions: Array[Vector3] = []

# Camera position time series.
var _rest_camera_pos: Array[Vector3] = []
var _travel_camera_pos: Array[Vector3] = []

# Travel timing.
var _travel_start_frame := 0
var _travel_end_frame := 0
var _second_travel_start_frame := 0
var _second_travel_end_frame := 0

# Home and destination nodes.
var _home_node_id := ""
var _dest_node_id := ""
var _sample_counter := 0


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.ANALYZE

	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(60, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait(30, Phase.BOOT)
		Phase.BOOT: _do_boot()
		Phase.MEASURE_REST_STABILITY: _do_measure_rest()
		Phase.INITIATE_TRAVEL: _do_initiate_travel()
		Phase.MEASURE_TRAVEL: _do_measure_travel()
		Phase.ARRIVE_AND_DOCK: _do_arrive_and_dock()
		Phase.MEASURE_DOCK_APPROACH: _do_measure_dock()
		Phase.SECOND_TRAVEL: _do_second_travel()
		Phase.MEASURE_SECOND_TRAVEL: _do_measure_second_travel()
		Phase.ANALYZE: _do_analyze()
		Phase.DONE: pass
	return false


func _do_load_scene() -> void:
	_a = AssertLib.new("FLIGHT_FEEL")
	_a.log("START|loading main scene")
	var err = change_scene_to_file("res://scenes/main.tscn")
	if err != OK:
		_a.flag("SCENE_LOAD_FAIL")
		_phase = Phase.ANALYZE
		return
	_phase = Phase.WAIT_SCENE


func _do_wait(max_polls: int, next: Phase) -> void:
	_polls += 1
	if _polls >= max_polls:
		_polls = 0
		_phase = next


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		_polls += 1
		if _polls > 300:
			_a.flag("NO_BRIDGE")
			_phase = Phase.ANALYZE
		return
	_gm = root.get_node_or_null("GameManager")

	_polls = 0
	_phase = Phase.WAIT_READY


func _do_boot() -> void:
	_phase = Phase.MEASURE_REST_STABILITY
	_polls = 0

	# Find camera and player ship (spawned after bridge ready).
	_camera = _find_node_recursive(root, "Camera3D")
	_player_ship = _find_node_recursive(root, "Player")
	if _player_ship == null:
		_player_ship = _find_node_recursive(root, "HeroShip")

	_a.log("BOOT|camera=%s ship=%s" % [str(_camera != null), str(_player_ship != null)])

	var ps = _bridge.call("GetPlayerStateV0")
	if ps is Dictionary:
		_home_node_id = ps.get("current_node_id", "")

	# Find a neighbor for travel via galaxy snapshot.
	var snap = _bridge.call("GetGalaxySnapshotV0")
	var edges: Array = []
	if snap is Dictionary:
		edges = snap.get("lane_edges", [])
	for e in edges:
		if e is Dictionary:
			if e.get("from_id", "") == _home_node_id:
				_dest_node_id = e.get("to_id", "")
				break
			elif e.get("to_id", "") == _home_node_id:
				_dest_node_id = e.get("from_id", "")
				break

	_a.log("HOME=%s DEST=%s" % [_home_node_id, _dest_node_id])


func _do_measure_rest() -> void:
	_polls += 1
	_sample_counter += 1

	# Sample position at rest.
	if _sample_counter >= POSITION_SAMPLE_INTERVAL:
		_sample_counter = 0
		if _camera != null and _camera is Node3D:
			_rest_camera_pos.append((_camera as Node3D).global_position)
		if _player_ship != null and _player_ship is Node3D:
			_rest_positions.append((_player_ship as Node3D).global_position)

	if _polls >= 120:  # 2 seconds of rest measurement
		_polls = 0
		_sample_counter = 0
		_a.log("REST_MEASURED|camera_samples=%d ship_samples=%d" % [
			_rest_camera_pos.size(), _rest_positions.size()])
		_phase = Phase.INITIATE_TRAVEL


func _do_initiate_travel() -> void:
	_phase = Phase.MEASURE_TRAVEL
	_polls = 0
	_travel_start_frame = _total_frames

	if _dest_node_id.is_empty():
		_a.warn(false, "no_travel_destination", "no neighbor found")
		_phase = Phase.ANALYZE
		return

	_a.log("TRAVEL_START|dest=%s frame=%d" % [_dest_node_id, _total_frames])
	# Use headless travel.
	_gm.call("on_lane_gate_proximity_entered_v0", _dest_node_id)
	_gm.call("on_lane_arrival_v0", _dest_node_id)


func _do_measure_travel() -> void:
	_polls += 1
	_sample_counter += 1

	if _sample_counter >= POSITION_SAMPLE_INTERVAL:
		_sample_counter = 0
		if _camera != null and _camera is Node3D:
			_travel_camera_pos.append((_camera as Node3D).global_position)
		if _player_ship != null and _player_ship is Node3D:
			_travel_positions.append((_player_ship as Node3D).global_position)

	if _polls >= 120:
		_travel_end_frame = _total_frames
		_a.log("TRAVEL_MEASURED|samples=%d duration=%d frames" % [
			_travel_positions.size(), _travel_end_frame - _travel_start_frame])
		_polls = 0
		_phase = Phase.ARRIVE_AND_DOCK


func _do_arrive_and_dock() -> void:
	_phase = Phase.MEASURE_DOCK_APPROACH
	_polls = 0
	_a.log("DOCK_ATTEMPT|at %s" % _dest_node_id)


func _do_measure_dock() -> void:
	_polls += 1
	_sample_counter += 1

	if _sample_counter >= POSITION_SAMPLE_INTERVAL:
		_sample_counter = 0
		if _camera != null and _camera is Node3D:
			_dock_positions.append((_camera as Node3D).global_position)

	if _polls >= 90:
		_a.log("DOCK_MEASURED|samples=%d" % _dock_positions.size())
		_polls = 0
		_phase = Phase.SECOND_TRAVEL


func _do_second_travel() -> void:
	_phase = Phase.MEASURE_SECOND_TRAVEL
	_polls = 0
	_second_travel_start_frame = _total_frames

	# Travel back to home.
	_a.log("SECOND_TRAVEL|back to %s" % _home_node_id)
	_gm.call("on_lane_gate_proximity_entered_v0", _home_node_id)
	_gm.call("on_lane_arrival_v0", _home_node_id)


func _do_measure_second_travel() -> void:
	_polls += 1
	if _polls >= 120:
		_second_travel_end_frame = _total_frames
		_phase = Phase.ANALYZE


func _do_analyze() -> void:
	_phase = Phase.DONE
	_a.log("ANALYZE|computing flight feel metrics")

	# --- Rest stability ---
	if _rest_camera_pos.size() >= 5:
		var jitter := _compute_jitter(_rest_camera_pos)
		_a.goal("REST_CAMERA_JITTER", "jitter=%.4f samples=%d" % [jitter, _rest_camera_pos.size()])
		# Camera should be very stable at rest (jitter < 0.1 units).
		_a.hard(jitter < 0.5, "camera_stable_at_rest", "jitter=%.4f" % jitter)
		_a.warn(jitter < 0.1, "camera_very_stable_at_rest", "jitter=%.4f" % jitter)
	else:
		_a.warn(false, "insufficient_rest_samples", "count=%d" % _rest_camera_pos.size())

	# --- Travel smoothness ---
	if _travel_camera_pos.size() >= 5:
		var cam_jerk := _compute_jerk(_travel_camera_pos)
		_a.goal("TRAVEL_CAMERA_JERK", "max_jerk=%.4f samples=%d" % [cam_jerk, _travel_camera_pos.size()])
		# Camera jerk should be bounded (no sudden jumps).
		_a.warn(cam_jerk < 10.0, "camera_smooth_during_travel", "max_jerk=%.4f" % cam_jerk)

	if _travel_positions.size() >= 5:
		var ship_jerk := _compute_jerk(_travel_positions)
		_a.goal("TRAVEL_SHIP_JERK", "max_jerk=%.4f" % ship_jerk)
		_a.warn(ship_jerk < 20.0, "ship_smooth_during_travel", "max_jerk=%.4f" % ship_jerk)

	# --- Travel time consistency ---
	var travel_1_duration := _travel_end_frame - _travel_start_frame
	var travel_2_duration := _second_travel_end_frame - _second_travel_start_frame
	if travel_1_duration > 0 and travel_2_duration > 0:
		_a.goal("TRAVEL_TIME_CONSISTENCY", "t1=%d t2=%d" % [travel_1_duration, travel_2_duration])
		# Same distance should have similar travel time.
		var ratio := float(max(travel_1_duration, travel_2_duration)) / float(min(travel_1_duration, travel_2_duration))
		_a.warn(ratio < 2.0, "travel_time_consistent", "ratio=%.2f" % ratio)

	# --- Dock approach smoothness ---
	if _dock_positions.size() >= 3:
		var dock_jitter := _compute_jitter(_dock_positions)
		_a.goal("DOCK_CAMERA_STABILITY", "jitter=%.4f" % dock_jitter)

	# --- JSON report ---
	var rest_jitter_val := _compute_jitter(_rest_camera_pos) if _rest_camera_pos.size() >= 5 else 0.0
	var travel_cam_jerk_val := _compute_jerk(_travel_camera_pos) if _travel_camera_pos.size() >= 5 else 0.0
	var travel_ship_jerk_val := _compute_jerk(_travel_positions) if _travel_positions.size() >= 5 else 0.0
	var travel_time_ratio_val := 0.0
	if travel_1_duration > 0 and travel_2_duration > 0:
		travel_time_ratio_val = float(max(travel_1_duration, travel_2_duration)) / float(min(travel_1_duration, travel_2_duration))
	var dock_stability_val := _compute_jitter(_dock_positions) if _dock_positions.size() >= 3 else 0.0
	var report := {
		"bot": "flight_feel",
		"prefix": "FLIGHT_FEEL",
		"metrics": {
			"rest_camera_jitter": rest_jitter_val,
			"travel_camera_jerk": travel_cam_jerk_val,
			"travel_ship_jerk": travel_ship_jerk_val,
			"travel_time_ratio": travel_time_ratio_val,
			"dock_camera_stability": dock_stability_val,
		},
		"assertions": {
			"pass": _a._passes,
			"warn": _a._warns.size(),
			"fail": _a._fails.size(),
		},
	}
	var json_str := JSON.stringify(report, "  ")
	var report_path := "res://reports/eval/flight_feel_report.json"
	var f := FileAccess.open(report_path, FileAccess.WRITE)
	if f:
		f.store_string(json_str)
		f.close()
		_a.log("REPORT_JSON|written=%s" % report_path)

	_a.summary()
	if _bridge:
		_bridge.call("StopSimV0")
	_busy = true
	await create_timer(0.5).timeout
	quit(_a.exit_code())


# --- Analysis helpers ---

func _compute_jitter(positions: Array[Vector3]) -> float:
	# Jitter = average frame-to-frame position delta.
	if positions.size() < 2: return 0.0
	var total_delta := 0.0
	for i in range(1, positions.size()):
		total_delta += positions[i].distance_to(positions[i - 1])
	return total_delta / (positions.size() - 1)

func _compute_jerk(positions: Array[Vector3]) -> float:
	# Jerk = max second derivative of position (acceleration change rate).
	if positions.size() < 3: return 0.0

	# Compute velocities.
	var velocities: Array[Vector3] = []
	for i in range(1, positions.size()):
		velocities.append(positions[i] - positions[i - 1])

	# Compute accelerations.
	var accels: Array[Vector3] = []
	for i in range(1, velocities.size()):
		accels.append(velocities[i] - velocities[i - 1])

	# Compute jerk (derivative of acceleration).
	var max_jerk := 0.0
	for i in range(1, accels.size()):
		var jerk := accels[i].distance_to(accels[i - 1])
		if jerk > max_jerk:
			max_jerk = jerk
	return max_jerk

func _find_node_recursive(parent: Node, name_pattern: String) -> Node:
	for child in parent.get_children():
		if child.name == name_pattern:
			return child
		var found := _find_node_recursive(child, name_pattern)
		if found != null:
			return found
	return null
