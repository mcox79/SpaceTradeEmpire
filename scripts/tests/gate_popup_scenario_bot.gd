extends SceneTree

## Gate Popup Scenario Bot — Tests pause-on-approach, cancel suppression, and re-trigger.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Phases: boot → teleport to gate → trigger approach (verify pause + popup) →
##         cancel (verify unpause) → re-trigger in zone (verify suppressed) →
##         simulate exit → re-trigger (verify popup again) → done
##
## Output: reports/screenshot/scenario_gate_popup_scenario_bot/

const PREFIX := "GPOP|"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/screenshot/scenario_gate_popup/"

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

const SETTLE_SCENE := 60
const SETTLE_ACTION := 15
const POST_CAPTURE := 8

enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	TELEPORT_TO_GATE,
	SETTLE_TELEPORT,
	TRIGGER_APPROACH,
	CAPTURE_POPUP_PAUSED,
	CANCEL_APPROACH,
	CAPTURE_CANCELLED,
	RETRIGGER_IN_ZONE,
	SIMULATE_EXIT,
	RETRIGGER_AFTER_EXIT,
	CAPTURE_RETRIGGER,
	FINAL_CANCEL,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
const MAX_FRAMES := 1200  # 20s at 60fps

var _bridge = null
var _game_manager = null
var _screenshot = null
var _neighbor_id := ""
var _pass_count := 0
var _fail_count := 0


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.DONE
	match _phase:
		# ── Setup (same as quick bot) ──────────────────────────
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
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_screenshot = ScreenshotScript.new()
				_game_manager = root.get_node_or_null("GameManager")
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			if get_nodes_in_group("Station").size() > 0:
				_polls = 0
				_phase = Phase.TELEPORT_TO_GATE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_phase = Phase.TELEPORT_TO_GATE

		# ── Teleport to gate ──────────────────────────────────
		Phase.TELEPORT_TO_GATE:
			_neighbor_id = _find_neighbor_id()
			if _neighbor_id.is_empty():
				_fail("no_neighbor_id")
				return false
			_teleport_to_gate(_neighbor_id)
			# Lower camera so ship + gate orbs are visible in screenshots.
			_lower_camera(25.0)
			_polls = 0
			_phase = Phase.SETTLE_TELEPORT

		Phase.SETTLE_TELEPORT:
			_polls += 1
			if _polls >= SETTLE_SCENE:  # 60 frames — let camera fully track to gate area.
				_polls = 0
				_phase = Phase.TRIGGER_APPROACH

		# ── Test 1: Trigger approach → popup + pause ──────────
		Phase.TRIGGER_APPROACH:
			print(PREFIX + "TEST|trigger_approach|dest=%s" % _neighbor_id)
			_game_manager.call("on_lane_gate_approach_entered_v0", _neighbor_id)
			_polls = 0
			_phase = Phase.CAPTURE_POPUP_PAUSED

		Phase.CAPTURE_POPUP_PAUSED:
			# Even though tree is paused, SceneTree._process still fires.
			_polls += 1
			if _polls >= POST_CAPTURE:
				var is_paused: bool = paused
				var has_popup: bool = _game_manager.get("_gate_popup") != null
				var state: String = str(_game_manager.get("current_player_state"))
				_assert("paused_on_approach", is_paused == true,
					"paused=%s popup=%s state=%s" % [is_paused, has_popup, state])
				_assert("popup_visible", has_popup == true,
					"popup=%s" % has_popup)
				_capture("popup_paused")
				_polls = 0
				_phase = Phase.CANCEL_APPROACH

		# ── Test 2: Cancel → unpause + declined flag ──────────
		Phase.CANCEL_APPROACH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				print(PREFIX + "TEST|cancel_approach")
				_game_manager.call("_cancel_gate_approach_v0")
				var is_paused: bool = paused
				var has_popup: bool = _game_manager.get("_gate_popup") != null
				var declined: bool = _game_manager.get("_gate_approach_declined")
				_assert("unpaused_on_cancel", is_paused == false,
					"paused=%s" % is_paused)
				_assert("popup_removed", has_popup == false,
					"popup=%s" % has_popup)
				_assert("declined_flag_set", declined == true,
					"declined=%s" % declined)
				_capture("cancelled_unpaused")
				_polls = 0
				_phase = Phase.RETRIGGER_IN_ZONE

		# ── Test 3: Re-trigger while in zone → suppressed ─────
		Phase.RETRIGGER_IN_ZONE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				print(PREFIX + "TEST|retrigger_in_zone")
				_game_manager.call("on_lane_gate_approach_entered_v0", _neighbor_id)
				var is_paused: bool = paused
				var has_popup: bool = _game_manager.get("_gate_popup") != null
				_assert("no_retrigger_in_zone", has_popup == false,
					"paused=%s popup=%s" % [is_paused, has_popup])
				_assert("stays_unpaused_in_zone", is_paused == false,
					"paused=%s" % is_paused)
				_polls = 0
				_phase = Phase.SIMULATE_EXIT

		# ── Test 4: Exit zone → declined cleared ──────────────
		Phase.SIMULATE_EXIT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				print(PREFIX + "TEST|simulate_exit")
				_game_manager.call("on_lane_gate_approach_exited_v0")
				var declined: bool = _game_manager.get("_gate_approach_declined")
				_assert("declined_cleared_on_exit", declined == false,
					"declined=%s" % declined)
				_polls = 0
				_phase = Phase.RETRIGGER_AFTER_EXIT

		# ── Test 5: Re-trigger after exit → popup + pause ─────
		Phase.RETRIGGER_AFTER_EXIT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				print(PREFIX + "TEST|retrigger_after_exit")
				_game_manager.call("on_lane_gate_approach_entered_v0", _neighbor_id)
				_polls = 0
				_phase = Phase.CAPTURE_RETRIGGER

		Phase.CAPTURE_RETRIGGER:
			_polls += 1
			if _polls >= POST_CAPTURE:
				var is_paused: bool = paused
				var has_popup: bool = _game_manager.get("_gate_popup") != null
				_assert("paused_on_retrigger", is_paused == true,
					"paused=%s popup=%s" % [is_paused, has_popup])
				_assert("popup_on_retrigger", has_popup == true,
					"popup=%s" % has_popup)
				_capture("retrigger_popup")
				_polls = 0
				_phase = Phase.FINAL_CANCEL

		Phase.FINAL_CANCEL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Clean up pause state before exit.
				_game_manager.call("_cancel_gate_approach_v0")
				print(PREFIX + "RESULTS|pass=%d fail=%d total=%d" % [
					_pass_count, _fail_count, _pass_count + _fail_count])
				if _fail_count == 0:
					print(PREFIX + "PASS|all_assertions_passed")
				else:
					print(PREFIX + "FAIL|assertions_failed=%d" % _fail_count)
				_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# --- Helpers ---

func _find_neighbor_id() -> String:
	if _bridge == null:
		return ""
	if _bridge.has_method("GetNeighborIdsV0"):
		var ids = _bridge.call("GetNeighborIdsV0")
		if ids is Array and ids.size() > 0:
			return str(ids[0])
	# Fallback: look for lane gate nodes in scene.
	var gates = get_nodes_in_group("LaneGate")
	if gates.size() > 0:
		return gates[0].name.replace("LaneGate_", "")
	return ""


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

	# Place hero just outside the 8u approach zone (12u from gate, zone is 8u).
	var star_center := Vector3.ZERO
	if gv and gv.has_method("GetCurrentStarGlobalPositionV0"):
		star_center = gv.call("GetCurrentStarGlobalPositionV0")
	var inward_dir := (star_center - gate_pos).normalized()
	hero.global_position = gate_pos + inward_dir * 12.0
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	hero.angular_velocity = Vector3.ZERO

	print(PREFIX + "TELEPORT|gate=%s hero=%s" % [_v3(gate_pos), _v3(hero.global_position)])


func _capture(label: String) -> void:
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]
	_screenshot.capture_v0(self, filename, OUTPUT_DIR)
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _assert(name: String, condition: bool, detail: String) -> void:
	if condition:
		_pass_count += 1
		print(PREFIX + "ASSERT|PASS|%s|%s" % [name, detail])
	else:
		_fail_count += 1
		print(PREFIX + "ASSERT|FAIL|%s|%s" % [name, detail])


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _get_hero_body():
	var players = get_nodes_in_group("Player")
	if players.size() > 0:
		return players[0]
	return null


func _lower_camera(alt: float) -> void:
	# The camera script (player_follow_camera.gd) is on Player/CameraMount/Camera3D.
	var hero = _get_hero_body()
	if hero == null:
		print(PREFIX + "WARN|no_hero_for_camera")
		return
	var cam_script = hero.find_child("Camera3D", true, false)
	if cam_script and "_altitude" in cam_script:
		cam_script.set("_altitude", alt)
		print(PREFIX + "CAMERA|altitude=%.1f" % alt)
	else:
		print(PREFIX + "WARN|camera_script_not_found")


func _v3(v: Vector3) -> String:
	return "(%.1f, %.1f, %.1f)" % [v.x, v.y, v.z]


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0 if _fail_count == 0 else 1)
