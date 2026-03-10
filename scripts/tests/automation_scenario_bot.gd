extends SceneTree

## Automation Scenario Bot — Proves automation management workflow end-to-end.
## Queries doctrine settings, program performance, and failure reasons via SimBridge.
## Run WINDOWED for screenshot capture, or HEADLESS for CI.
##
## Usage:
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/automation_scenario_bot.gd"

const PREFIX := "ABOT|"
const MAX_TICKS := 200
const OUTPUT_DIR := "res://reports/screenshot/scenario_automation/"

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum State {
	WAIT_BRIDGE,
	WAIT_READY,
	QUERY_DOCTRINE,
	QUERY_PERF,
	QUERY_FAILURES,
	CAPTURE,
	DONE
}

var _state: int = State.WAIT_BRIDGE
var _bridge = null
var _capturer = null
var _tick: int = 0
var _errors: Array = []

func _initialize() -> void:
	print("%sSTART|scenario=automation_mgmt" % PREFIX)
	_capturer = ScreenshotScript.new()

func _process(delta: float) -> bool:
	_tick += 1
	if _tick > MAX_TICKS:
		_finish("TIMEOUT")
		return true

	match _state:
		State.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				print("%sBRIDGE_FOUND|tick=%d" % [PREFIX, _tick])
				_state = State.WAIT_READY

		State.WAIT_READY:
			if _tick > 30:  # Wait 30 frames for sim to stabilize
				_state = State.QUERY_DOCTRINE

		State.QUERY_DOCTRINE:
			_test_doctrine()
			_state = State.QUERY_PERF

		State.QUERY_PERF:
			_test_performance()
			_state = State.QUERY_FAILURES

		State.QUERY_FAILURES:
			_test_failures()
			_state = State.CAPTURE

		State.CAPTURE:
			_capture_screenshots()
			_state = State.DONE

		State.DONE:
			_finish("PASS" if _errors.is_empty() else "FAIL")
			return true

	return false

func _test_doctrine() -> void:
	if not _bridge.has_method("GetDoctrineSettingsV0"):
		_errors.append("MISSING_METHOD:GetDoctrineSettingsV0")
		print("%sFAIL|GetDoctrineSettingsV0 missing" % PREFIX)
		return
	var data: Dictionary = _bridge.call("GetDoctrineSettingsV0", "fleet_trader_1")
	if data.is_empty():
		_errors.append("EMPTY_RESULT:GetDoctrineSettingsV0")
		print("%sFAIL|GetDoctrineSettingsV0 empty" % PREFIX)
		return
	var stance: String = str(data.get("stance", ""))
	var retreat: int = int(data.get("retreat_threshold_pct", -1))
	print("%sDOCTRINE|stance=%s|retreat=%d|patrol=%.0f" % [PREFIX, stance, retreat, float(data.get("patrol_radius", 0))])
	if stance.is_empty():
		_errors.append("INVALID:stance_empty")

func _test_performance() -> void:
	if not _bridge.has_method("GetProgramPerformanceV0"):
		_errors.append("MISSING_METHOD:GetProgramPerformanceV0")
		print("%sFAIL|GetProgramPerformanceV0 missing" % PREFIX)
		return
	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
	if data.is_empty():
		_errors.append("EMPTY_RESULT:GetProgramPerformanceV0")
		print("%sFAIL|GetProgramPerformanceV0 empty" % PREFIX)
		return
	print("%sPERF|cycles=%d|credits=%d|goods=%d|failures=%d" % [PREFIX,
		int(data.get("cycles_run", 0)),
		int(data.get("credits_earned", 0)),
		int(data.get("goods_moved", 0)),
		int(data.get("failures", 0))
	])

func _test_failures() -> void:
	if not _bridge.has_method("GetProgramFailureReasonsV0"):
		_errors.append("MISSING_METHOD:GetProgramFailureReasonsV0")
		print("%sFAIL|GetProgramFailureReasonsV0 missing" % PREFIX)
		return
	var data: Dictionary = _bridge.call("GetProgramFailureReasonsV0", "fleet_trader_1")
	if data.is_empty():
		_errors.append("EMPTY_RESULT:GetProgramFailureReasonsV0")
		print("%sFAIL|GetProgramFailureReasonsV0 empty" % PREFIX)
		return
	print("%sFAILURES|total=%d|consec=%d|last=%s" % [PREFIX,
		int(data.get("total_failures", 0)),
		int(data.get("consecutive_failures", 0)),
		str(data.get("last_failure_reason", "None"))
	])

func _capture_screenshots() -> void:
	if _capturer:
		_capturer.capture_v0(self, "automation_boot", OUTPUT_DIR)
	print("%sCAPTURE|phase=automation_boot" % PREFIX)

func _finish(result: String) -> void:
	print("%sRESULT|%s|errors=%d|tick=%d" % [PREFIX, result, _errors.size(), _tick])
	for err in _errors:
		print("%sERROR|%s" % [PREFIX, err])
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0 if result == "PASS" else 1)
