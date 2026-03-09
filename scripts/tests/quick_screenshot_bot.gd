extends SceneTree

## Quick Screenshot Bot — Captures 4 key game states in ~15 seconds.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Phases: boot → dock market → galaxy map → final
## Output: reports/screenshot/quick/
##
## Usage:
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/quick_screenshot_bot.gd"

const PREFIX := "QSCR|"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/screenshot/quick/"

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
const AuditScript = preload("res://scripts/tools/aesthetic_audit.gd")

const SETTLE_SCENE := 60
const SETTLE_ACTION := 15
const SETTLE_UI := 20
const POST_CAPTURE := 8

enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	BOOT_CAPTURE,
	DOCK_ENTER,
	DOCK_MARKET_CAPTURE,
	UNDOCK,
	OPEN_GALAXY_MAP,
	GALAXY_MAP_CAPTURE,
	CLOSE_GALAXY_MAP,
	FINAL_CAPTURE,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
const MAX_FRAMES := 900  # 15s at 60fps

var _bridge = null
var _game_manager = null
var _observer = null
var _screenshot = null
var _audit = null
var _snapshots: Array = []


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.DONE
	match _phase:
		# ── Setup ──────────────────────────────────────────────
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
				_observer = ObserverScript.new()
				_observer.init_v0(self)
				_screenshot = ScreenshotScript.new()
				_audit = AuditScript.new()
				_game_manager = root.get_node_or_null("GameManager")
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			if get_nodes_in_group("Station").size() > 0:
				_polls = 0
				_phase = Phase.BOOT_CAPTURE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_phase = Phase.BOOT_CAPTURE

		# ── Captures ───────────────────────────────────────────
		Phase.BOOT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("boot")
				_polls = 0
				_phase = Phase.DOCK_ENTER

		Phase.DOCK_ENTER:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.DOCK_MARKET_CAPTURE

		Phase.DOCK_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("dock_market")
				_polls = 0
				_phase = Phase.UNDOCK

		Phase.UNDOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.OPEN_GALAXY_MAP

		Phase.OPEN_GALAXY_MAP:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.GALAXY_MAP_CAPTURE

		Phase.GALAXY_MAP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("galaxy_map")
				_polls = 0
				_phase = Phase.CLOSE_GALAXY_MAP

		Phase.CLOSE_GALAXY_MAP:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.FINAL_CAPTURE

		Phase.FINAL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				var report = _observer.capture_full_report_v0()
				var audit_results = _audit.run_audit_v0(report)
				var critical_fails = _audit.count_critical_failures_v0(audit_results)

				for ar in audit_results:
					var status = "PASS" if ar.get("passed", false) else "FAIL"
					print(PREFIX + "AESTHETIC|%s|%s|%s|%s" % [
						status, str(ar.get("flag", "")),
						str(ar.get("severity", "")), str(ar.get("detail", ""))])

				print(PREFIX + "AESTHETIC_CRITICAL_FAILS|%d" % critical_fails)
				_capture("final")
				_write_summary(audit_results, critical_fails)

				if critical_fails == 0:
					print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
				else:
					print(PREFIX + "FAIL|critical=%d screenshots=%d" % [critical_fails, _snapshots.size()])

				_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# --- Helpers ---

func _dock_at_current_station() -> void:
	if _game_manager == null:
		return
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_game_manager.call("on_proximity_dock_entered_v0", targets[0])
		print(PREFIX + "DOCK|%s" % str(targets[0].name))


func _undock() -> void:
	if _game_manager != null and _game_manager.has_method("undock_v0"):
		_game_manager.call("undock_v0")
		print(PREFIX + "UNDOCK")


func _toggle_galaxy_map() -> void:
	if _game_manager != null and _game_manager.has_method("toggle_galaxy_map_overlay_v0"):
		_game_manager.call("toggle_galaxy_map_overlay_v0")


func _capture(label: String) -> void:
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]

	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)

	var report = _observer.capture_full_report_v0()
	var report_path := OUTPUT_DIR.path_join(filename + "_report.json")
	_observer.write_report_json_v0(report, report_path)

	_snapshots.append({
		"phase": label,
		"tick": tick,
		"screenshot": img_path,
		"report": report_path,
	})
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _write_summary(audit_results: Array, critical_fails: int) -> void:
	var summary := {
		"bot": "quick_screenshot_v1",
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
		"aesthetic_audit": audit_results,
		"critical_failures": critical_fails,
	}
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var f := FileAccess.open(OUTPUT_DIR.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
		print(PREFIX + "SUMMARY_SAVED")
	else:
		print(PREFIX + "SUMMARY_SAVE_FAILED")


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
