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
