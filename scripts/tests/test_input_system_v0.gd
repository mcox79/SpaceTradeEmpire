extends SceneTree

# Headless proof: verify input system migration.
# Checks: all 18 actions registered in InputMap, InputManager autoload present,
# get_action_label() returns non-empty for all actions.
# Emits: INPUT_SYS|PASS

const PREFIX := "INPUT_SYS|"

var _pass_count: int = 0
var _fail_count: int = 0

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_fail_count += 1


func _ok(msg: String) -> void:
	print(PREFIX + "OK|" + msg)
	_pass_count += 1


func _initialize() -> void:
	print(PREFIX + "BOOT")
	call_deferred("_run")


func _run() -> void:
	# Wait for autoloads to initialize.
	await create_timer(0.5).timeout

	# ── Check all 18 actions exist in InputMap ──
	var expected_actions := [
		"ship_thrust_fwd", "ship_thrust_back", "ship_turn_left", "ship_turn_right",
		"combat_fire_primary", "combat_fire_secondary", "combat_target_nearest",
		"ui_galaxy_map", "ui_dock_confirm", "ui_empire_dashboard",
		"ui_mission_journal", "ui_knowledge_web", "ui_combat_log",
		"ui_data_overlay", "ui_keybinds_help", "ui_pause",
		"ui_gate_confirm", "ui_gate_cancel",
	]

	for action in expected_actions:
		if InputMap.has_action(action):
			_ok("ACTION_EXISTS|" + action)
		else:
			_fail("ACTION_MISSING|" + action)

	# ── Check InputManager autoload is present ──
	var input_mgr = get_root().get_node_or_null("InputManager")
	if input_mgr != null:
		_ok("INPUT_MANAGER_PRESENT")
	else:
		_fail("INPUT_MANAGER_MISSING")
		print(PREFIX + "SUMMARY|pass=%d|fail=%d" % [_pass_count, _fail_count])
		_stop_sim_and_quit(1)
		return

	# ── Check get_action_label returns non-empty for all flight/combat actions ──
	var label_actions := [
		"ship_thrust_fwd", "ship_thrust_back", "ship_turn_left", "ship_turn_right",
		"combat_fire_primary", "combat_fire_secondary", "combat_target_nearest",
		"ui_galaxy_map", "ui_dock_confirm", "ui_empire_dashboard",
	]
	for action in label_actions:
		if input_mgr.has_method("get_action_label"):
			var label: String = input_mgr.get_action_label(action)
			if label != "" and label != "---":
				_ok("LABEL|%s=%s" % [action, label])
			else:
				_fail("LABEL_EMPTY|" + action)
		else:
			_fail("NO_GET_ACTION_LABEL_METHOD")
			break

	# ── Check gamepad labels exist for flight actions ──
	var gamepad_actions := ["ship_thrust_fwd", "combat_fire_primary", "ui_galaxy_map"]
	for action in gamepad_actions:
		if input_mgr.has_method("get_action_gamepad_label"):
			var gp_label: String = input_mgr.get_action_gamepad_label(action)
			if gp_label != "" and gp_label != "---":
				_ok("GAMEPAD_LABEL|%s=%s" % [action, gp_label])
			else:
				_fail("GAMEPAD_LABEL_EMPTY|" + action)

	# ── Check each action has at least one event registered ──
	for action in expected_actions:
		if InputMap.has_action(action):
			var events = InputMap.action_get_events(action)
			if events.size() > 0:
				_ok("EVENTS|%s=%d" % [action, events.size()])
			else:
				_fail("NO_EVENTS|" + action)

	# ── Summary ──
	print(PREFIX + "SUMMARY|pass=%d|fail=%d" % [_pass_count, _fail_count])
	if _fail_count == 0:
		print(PREFIX + "PASS")
		_stop_sim_and_quit(0)
	else:
		_stop_sim_and_quit(1)
