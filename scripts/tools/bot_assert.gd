# scripts/tools/bot_assert.gd
# Shared assertion library for headless bot scripts.
#
# Usage:
#   var _a := preload("res://scripts/tools/bot_assert.gd").new("FH1")
#   _a.hard(condition, "name", "detail")     # PASS/FAIL — affects exit code
#   _a.warn(condition, "name", "detail")      # PASS/WARN — logged but non-fatal
#   _a.goal(goal_name, "key=value ...")       # Goal evidence probe
#   _a.flag("ISSUE_NAME")                     # Soft flag
#   _a.summary()                              # Print SUMMARY line
#   _a.exit_code()                            # 0 if no hard fails, else 1

var _prefix: String
var _passes := 0
var _fails: Array[String] = []
var _warns: Array[String] = []
var _flags: Array[String] = []


func _init(prefix: String = "BOT") -> void:
	_prefix = prefix


func hard(condition: bool, name: String, detail: String = "") -> void:
	if condition:
		print("%s|ASSERT_PASS|%s|%s" % [_prefix, name, detail])
		_passes += 1
	else:
		print("%s|ASSERT_FAIL|%s|%s" % [_prefix, name, detail])
		_fails.append(name)


func warn(condition: bool, name: String, detail: String = "") -> void:
	if condition:
		print("%s|ASSERT_PASS|%s|%s" % [_prefix, name, detail])
		_passes += 1
	else:
		print("%s|ASSERT_WARN|%s|%s" % [_prefix, name, detail])
		_warns.append(name)


func goal(goal_name: String, detail: String) -> void:
	print("%s|GOAL|%s|%s" % [_prefix, goal_name, detail])


func flag(msg: String) -> void:
	_flags.append(msg)
	print("%s|FLAG|%s" % [_prefix, msg])


func log(msg: String) -> void:
	print("%s|%s" % [_prefix, msg])


func summary() -> void:
	print("%s|SUMMARY|pass=%d fail=%d warn=%d flags=%d" % [
		_prefix, _passes, _fails.size(), _warns.size(), _flags.size()])
	if _fails.is_empty():
		print("%s|PASS" % _prefix)
	else:
		print("%s|FAIL|%s" % [_prefix, ",".join(_fails)])


func exit_code() -> int:
	return 0 if _fails.is_empty() else 1


func has_failures() -> bool:
	return not _fails.is_empty()


# ── Read-lock retry helper ──
# Bridge read calls can return empty/null on lock contention.
# This retries up to `max_retries` times with `delay_ms` wait between attempts.
# Usage: var data = await _a.bridge_read(bridge, "GetGalaxySnapshotV0", [], tree)
func bridge_read(bridge: Object, method: String, args: Array, tree: SceneTree, max_retries: int = 3, delay_ms: float = 0.3) -> Variant:
	for i in range(max_retries + 1):
		var result: Variant
		if args.is_empty():
			result = bridge.call(method)
		elif args.size() == 1:
			result = bridge.call(method, args[0])
		elif args.size() == 2:
			result = bridge.call(method, args[0], args[1])
		elif args.size() == 3:
			result = bridge.call(method, args[0], args[1], args[2])
		else:
			result = bridge.callv(method, args)
		# Check if result is valid (non-null, non-empty dict/array)
		if result != null:
			if result is Dictionary and result.is_empty():
				pass  # retry
			elif result is Array and result.is_empty():
				pass  # retry
			else:
				return result
		if i < max_retries:
			self.log("BRIDGE_RETRY|method=%s|attempt=%d" % [method, i + 1])
			await tree.create_timer(delay_ms).timeout
	self.warn(false, "bridge_read_exhausted", "method=%s after %d retries" % [method, max_retries])
	return null


# ── Decision event stream ──
# Records timestamped game events for emotional arc analysis.
# Usage: _a.event(decision_num, "HEIST_FIRST_SELL", "margin=42")
var _events: Array[Dictionary] = []

func event(decision: int, event_name: String, detail: String = "") -> void:
	var entry := { "decision": decision, "event": event_name, "detail": detail }
	_events.append(entry)
	print("%s|EVENT|d=%d|%s|%s" % [_prefix, decision, event_name, detail])


func get_events() -> Array[Dictionary]:
	return _events


# Analyze events for dead zones (gaps > threshold with no HIGH/MEDIUM events).
func analyze_dead_zones(threshold: int = 50) -> Array[Dictionary]:
	if _events.is_empty():
		return []
	var sorted_events := _events.duplicate()
	sorted_events.sort_custom(func(a, b): return a["decision"] < b["decision"])
	var zones: Array[Dictionary] = []
	var prev_decision: int = 0
	for ev in sorted_events:
		var gap: int = ev["decision"] - prev_decision
		if gap > threshold:
			zones.append({ "start": prev_decision, "end": ev["decision"], "gap": gap })
		prev_decision = ev["decision"]
	return zones


# ── FPS profiling ──
# Samples frame times per phase for performance analysis.
var _fps_samples: Dictionary = {}  # phase_name -> Array[float]
var _fps_current_phase: String = ""

func fps_set_phase(phase_name: String) -> void:
	_fps_current_phase = phase_name
	if not _fps_samples.has(phase_name):
		_fps_samples[phase_name] = []


func fps_sample(delta: float) -> void:
	if _fps_current_phase.is_empty():
		return
	_fps_samples[_fps_current_phase].append(delta)


func fps_report() -> Dictionary:
	var report := {}
	for phase in _fps_samples:
		var samples: Array = _fps_samples[phase]
		if samples.is_empty():
			continue
		var min_dt := 999.0
		var max_dt := 0.0
		var sum_dt := 0.0
		for dt in samples:
			if dt < min_dt: min_dt = dt
			if dt > max_dt: max_dt = dt
			sum_dt += dt
		var avg_dt: float = sum_dt / samples.size()
		var min_fps: float = 1.0 / max_dt if max_dt > 0 else 0.0
		var max_fps: float = 1.0 / min_dt if min_dt > 0 else 0.0
		var avg_fps: float = 1.0 / avg_dt if avg_dt > 0 else 0.0
		report[phase] = {
			"samples": samples.size(),
			"min_fps": snapped(min_fps, 0.1),
			"max_fps": snapped(max_fps, 0.1),
			"avg_fps": snapped(avg_fps, 0.1),
			"min_dt_ms": snapped(min_dt * 1000, 0.1),
			"max_dt_ms": snapped(max_dt * 1000, 0.1),
		}
		print("%s|PERF|phase=%s|samples=%d|min_fps=%.1f|avg_fps=%.1f|max_fps=%.1f|max_dt_ms=%.1f" % [
			_prefix, phase, samples.size(), min_fps, avg_fps, max_fps, max_dt * 1000])
	return report
