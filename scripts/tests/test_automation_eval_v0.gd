# scripts/tests/test_automation_eval_v0.gd
# ─── Automation Eval Bot ───
# Creates ALL available program types and evaluates their behavior,
# lifecycle management, and reporting contracts.
#
# Run:
#   godot --headless --path . -s res://scripts/tests/test_automation_eval_v0.gd -- --seed=42
#
extends SceneTree

# ── Configuration ──
const BOT_PREFIX := "AUTO"
const MAX_TOTAL_FRAMES := 18000  # ~300s safety timeout
const STALL_FRAME_LIMIT := 1200  # ~20s per-phase stall detection

# ── Phase enum ──
enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	INIT,
	SETUP_TRADE,
	CREATE_PROGRAMS,
	MONITOR_PROGRAMS,
	EVALUATE_LIFECYCLE,
	CHECK_FAILURES,
	REPORT,
	DONE
}

# ── State ──
var _phase := Phase.LOAD_SCENE
var _bridge: Object = null
var _gm: Object = null
var _a := preload("res://scripts/tools/bot_assert.gd").new(BOT_PREFIX)
var _total_frames: int = 0
var _busy: bool = false
var _stall_phase: int = -1
var _stall_frame_start: int = 0
var _seed: int = 42

# ── Galaxy data ──
var _nodes: Array = []
var _edges: Array = []
var _current_node_id: String = ""
var _node_ids: Array = []  # all node IDs
var _adjacent_ids: Array = []  # nodes adjacent to current

# ── Trade tracking ──
var _baseline_credits: int = 0
var _trade_cycles: int = 0
var _goods_traded: Array = []

# ── Program tracking ──
var _programs_created: Dictionary = {}  # type_name → program_id
var _programs_failed: Dictionary = {}   # type_name → error_reason
var _monitor_frames: int = 0
var _monitor_target: int = 150  # frames to wait for programs to tick

# ── Lifecycle tracking ──
var _lifecycle_tested: bool = false
var _lifecycle_pause_ok: bool = false
var _lifecycle_resume_ok: bool = false
var _lifecycle_cancel_ok: bool = false
var _lifecycle_postmortem_ok: bool = false

# ── Phase sub-step tracking ──
var _setup_step: int = 0
var _create_step: int = 0


# ── Lifecycle ──
func _init() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_seed = int(arg.split("=")[1])


func _process(delta: float) -> bool:
	_total_frames += 1
	_a.fps_sample(delta)

	# Hard safety timeout
	if _total_frames > MAX_TOTAL_FRAMES and _phase != Phase.DONE:
		_a.flag("TIMEOUT|frame=%d|phase=%d" % [_total_frames, _phase])
		if _busy:
			_a.log("TIMEOUT_FORCE_QUIT|frame=%d|phase=%d|busy=true" % [_total_frames, _phase])
			_busy = false
		_phase = Phase.REPORT

	# Per-phase stall watchdog
	if _phase != _stall_phase:
		_stall_phase = _phase
		_stall_frame_start = _total_frames
	elif _total_frames - _stall_frame_start > STALL_FRAME_LIMIT and _phase != Phase.DONE:
		_a.flag("STALL|phase=%d|frames=%d" % [_phase, _total_frames - _stall_frame_start])
		if _busy:
			_busy = false
		_phase = Phase.REPORT

	if _busy:
		return false

	match _phase:
		Phase.LOAD_SCENE:
			_do_load_scene()
		Phase.WAIT_SCENE:
			_do_wait_scene()
		Phase.WAIT_BRIDGE:
			_do_wait_bridge()
		Phase.INIT:
			_do_init()
		Phase.SETUP_TRADE:
			_do_setup_trade()
		Phase.CREATE_PROGRAMS:
			_do_create_programs()
		Phase.MONITOR_PROGRAMS:
			_do_monitor_programs()
		Phase.EVALUATE_LIFECYCLE:
			_do_evaluate_lifecycle()
		Phase.CHECK_FAILURES:
			_do_check_failures()
		Phase.REPORT:
			_do_report()
		Phase.DONE:
			pass

	return false


# ── Phase: LOAD_SCENE ──

func _do_load_scene() -> void:
	_a.fps_set_phase("load")
	var err := change_scene_to_file("res://scenes/main.tscn")
	if err != OK:
		_a.hard(false, "scene_load", "error=%d" % err)
		_force_quit(1)
		return
	_phase = Phase.WAIT_SCENE


func _do_wait_scene() -> void:
	var root_node := root.get_child(root.get_child_count() - 1) if root.get_child_count() > 0 else null
	if root_node == null or root_node.name == "root":
		return
	_gm = root.get_node_or_null("GameManager")
	if _gm == null:
		return
	_phase = Phase.WAIT_BRIDGE


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		return
	_busy = true
	_try_bridge_init()


func _try_bridge_init() -> void:
	var snapshot = await _a.bridge_read(_bridge, "GetGalaxySnapshotV0", [], self)
	if snapshot == null or (snapshot is Dictionary and snapshot.is_empty()):
		_a.hard(false, "bridge_init", "GetGalaxySnapshotV0 returned empty after retries")
		_phase = Phase.REPORT
		_busy = false
		return
	_a.hard(true, "bridge_init", "snapshot_ok")
	_busy = false
	_phase = Phase.INIT


# ── Phase: INIT ──

func _do_init() -> void:
	_a.fps_set_phase("init")
	_a.log("INIT|seed=%d" % _seed)

	# Delete stale quicksave
	var save_path := "user://quicksave.json"
	if FileAccess.file_exists(save_path):
		DirAccess.remove_absolute(save_path)
		_a.log("INIT|deleted_stale_quicksave")

	# Parse galaxy
	var galaxy: Variant = _bridge.call("GetGalaxySnapshotV0")
	if galaxy == null or not (galaxy is Dictionary):
		_a.hard(false, "galaxy_parse", "null_galaxy")
		_phase = Phase.REPORT
		return

	_nodes = galaxy.get("system_nodes", [])
	_edges = galaxy.get("lane_edges", [])
	_current_node_id = str(galaxy.get("player_current_node_id", ""))

	for n in _nodes:
		if n is Dictionary:
			_node_ids.append(str(n.get("node_id", "")))

	_a.hard(_nodes.size() >= 3, "galaxy_nodes", "count=%d" % _nodes.size())
	_a.hard(_current_node_id != "", "player_location", "node=%s" % _current_node_id)

	# Find adjacent nodes
	for e in _edges:
		if e is Dictionary:
			var from_id := str(e.get("from_id", ""))
			var to_id := str(e.get("to_id", ""))
			if from_id == _current_node_id and to_id not in _adjacent_ids:
				_adjacent_ids.append(to_id)
			elif to_id == _current_node_id and from_id not in _adjacent_ids:
				_adjacent_ids.append(from_id)

	_a.log("INIT|adjacent_count=%d" % _adjacent_ids.size())

	# Record baseline credits
	if _bridge.has_method("GetFleetStateV0"):
		var fleet_state: Variant = _bridge.call("GetFleetStateV0", "fleet_trader_1")
		_a.log("INIT|fleet_state=%s" % str(fleet_state))

	if _bridge.has_method("GetProgramPerformanceV0"):
		var perf: Variant = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
		if perf is Dictionary:
			_baseline_credits = int(perf.get("credits_earned", 0))
			_a.log("INIT|baseline_credits=%d" % _baseline_credits)

	_phase = Phase.SETUP_TRADE


# ── Phase: SETUP_TRADE ──

func _do_setup_trade() -> void:
	_a.fps_set_phase("setup_trade")

	if _setup_step == 0:
		_a.log("SETUP_TRADE|starting buy/sell cycles")
		_setup_step = 1

	if _setup_step <= 10:
		# Try to do a buy or sell at current or adjacent node
		_busy = true
		_do_trade_cycle()
		return

	# Done with trade setup
	_a.log("SETUP_TRADE|completed|cycles=%d|goods=%d" % [_trade_cycles, _goods_traded.size()])
	_a.warn(_trade_cycles >= 2, "trade_baseline", "cycles=%d" % _trade_cycles)
	_phase = Phase.CREATE_PROGRAMS


func _do_trade_cycle() -> void:
	var target_node := _current_node_id
	if _setup_step % 3 == 0 and _adjacent_ids.size() > 0:
		# Travel to an adjacent node every 3rd cycle
		var dest := _adjacent_ids[_setup_step % _adjacent_ids.size()]
		if _bridge.has_method("DispatchTravelCommandV0"):
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", dest)
		if _bridge.has_method("DispatchPlayerArriveV0"):
			_bridge.call("DispatchPlayerArriveV0", dest)
		_current_node_id = dest
		target_node = dest
		_a.log("SETUP_TRADE|travel|dest=%s" % dest)

	# Get market view and try buy/sell
	if _bridge.has_method("GetPlayerMarketViewV0"):
		var market: Variant = _bridge.call("GetPlayerMarketViewV0", target_node)
		if market is Array and market.size() > 0:
			# Pick first good with quantity > 0
			for item in market:
				if item is Dictionary:
					var good_id := str(item.get("good_id", ""))
					var qty: int = int(item.get("quantity", 0))
					if good_id != "" and qty > 0:
						var is_buy := (_setup_step % 2 == 0)
						if _bridge.has_method("DispatchPlayerTradeV0"):
							_bridge.call("DispatchPlayerTradeV0", target_node, good_id, 1, is_buy)
							_trade_cycles += 1
							if good_id not in _goods_traded:
								_goods_traded.append(good_id)
							_a.log("SETUP_TRADE|%s|node=%s|good=%s" % [
								"buy" if is_buy else "sell", target_node, good_id])
						break

	_setup_step += 1
	# Wait a bit for sim to process
	await create_timer(0.3).timeout
	_busy = false


# ── Phase: CREATE_PROGRAMS ──

func _do_create_programs() -> void:
	_a.fps_set_phase("create_programs")

	if _create_step == 0:
		_a.log("CREATE_PROGRAMS|starting")
		_create_step = 1
		return

	_busy = true

	match _create_step:
		1:
			_create_trade_charter()
		2:
			_create_auto_buy()
		3:
			_create_auto_sell()
		4:
			_create_resource_tap()
		5:
			_create_escort()
		6:
			_create_patrol()
		7:
			_create_survey()
		8:
			_create_expedition()
		9:
			_create_constr_cap()
		_:
			_busy = false
			_a.log("CREATE_PROGRAMS|done|created=%d|failed=%d" % [
				_programs_created.size(), _programs_failed.size()])
			_phase = Phase.MONITOR_PROGRAMS
			return

	_create_step += 1
	await create_timer(0.2).timeout
	_busy = false


func _create_trade_charter() -> void:
	if not _bridge.has_method("CreateTradeCharterProgram"):
		_programs_failed["TradeCharter"] = "method_missing"
		_a.log("CREATE|TradeCharter|SKIP|method_missing")
		return

	# Find a good that exists at both current and an adjacent node
	var dest := _adjacent_ids[0] if _adjacent_ids.size() > 0 else ""
	if dest == "":
		_programs_failed["TradeCharter"] = "no_adjacent_node"
		_a.log("CREATE|TradeCharter|SKIP|no_adjacent_node")
		return

	var good_id := "fuel"
	if _goods_traded.size() > 0:
		good_id = _goods_traded[0]

	var pid: Variant = _bridge.call("CreateTradeCharterProgram",
		_current_node_id, dest, good_id, good_id, 10)
	if pid is String and str(pid) != "":
		_programs_created["TradeCharter"] = str(pid)
		_a.log("CREATE|TradeCharter|OK|pid=%s" % str(pid))
		# Start it
		if _bridge.has_method("StartProgram"):
			_bridge.call("StartProgram", str(pid))
	else:
		_programs_failed["TradeCharter"] = "create_returned_empty"
		_a.log("CREATE|TradeCharter|FAIL|empty_result")


func _create_auto_buy() -> void:
	if not _bridge.has_method("CreateAutoBuyProgram"):
		_programs_failed["AutoBuy"] = "method_missing"
		_a.log("CREATE|AutoBuy|SKIP|method_missing")
		return

	# Get a good available at current node
	var good_id := _pick_market_good(_current_node_id)
	if good_id == "":
		_programs_failed["AutoBuy"] = "no_good_available"
		_a.log("CREATE|AutoBuy|SKIP|no_good_available")
		return

	var pid: Variant = _bridge.call("CreateAutoBuyProgram",
		_current_node_id, good_id, 1, 30)
	if pid is String and str(pid) != "":
		_programs_created["AutoBuy"] = str(pid)
		_a.log("CREATE|AutoBuy|OK|pid=%s|good=%s" % [str(pid), good_id])
		if _bridge.has_method("StartProgram"):
			_bridge.call("StartProgram", str(pid))
	else:
		_programs_failed["AutoBuy"] = "create_returned_empty"
		_a.log("CREATE|AutoBuy|FAIL|empty_result")


func _create_auto_sell() -> void:
	if not _bridge.has_method("CreateAutoSellProgram"):
		_programs_failed["AutoSell"] = "method_missing"
		_a.log("CREATE|AutoSell|SKIP|method_missing")
		return

	var target := _adjacent_ids[0] if _adjacent_ids.size() > 0 else _current_node_id
	var good_id := _pick_market_good(target)
	if good_id == "":
		good_id = "fuel"  # fallback

	var pid: Variant = _bridge.call("CreateAutoSellProgram",
		target, good_id, 1, 30)
	if pid is String and str(pid) != "":
		_programs_created["AutoSell"] = str(pid)
		_a.log("CREATE|AutoSell|OK|pid=%s|good=%s" % [str(pid), good_id])
		if _bridge.has_method("StartProgram"):
			_bridge.call("StartProgram", str(pid))
	else:
		_programs_failed["AutoSell"] = "create_returned_empty"
		_a.log("CREATE|AutoSell|FAIL|empty_result")


func _create_resource_tap() -> void:
	if not _bridge.has_method("CreateResourceTapProgram"):
		_programs_failed["ResourceTap"] = "method_missing"
		_a.log("CREATE|ResourceTap|SKIP|method_missing")
		return

	var good_id := _pick_market_good(_current_node_id)
	if good_id == "":
		good_id = "ore"  # fallback

	var pid: Variant = _bridge.call("CreateResourceTapProgram",
		_current_node_id, good_id, 20)
	if pid is String and str(pid) != "":
		_programs_created["ResourceTap"] = str(pid)
		_a.log("CREATE|ResourceTap|OK|pid=%s|good=%s" % [str(pid), good_id])
		if _bridge.has_method("StartProgram"):
			_bridge.call("StartProgram", str(pid))
	else:
		_programs_failed["ResourceTap"] = "create_returned_empty"
		_a.log("CREATE|ResourceTap|FAIL|empty_result")


func _create_escort() -> void:
	if not _bridge.has_method("CreateEscortProgramV0"):
		_programs_failed["Escort"] = "method_missing"
		_a.log("CREATE|Escort|SKIP|method_missing")
		return

	var dest := _adjacent_ids[0] if _adjacent_ids.size() > 0 else ""
	if dest == "":
		_programs_failed["Escort"] = "no_adjacent_node"
		_a.log("CREATE|Escort|SKIP|no_adjacent_node")
		return

	var pid: Variant = _bridge.call("CreateEscortProgramV0",
		"fleet_trader_1", _current_node_id, dest, 30)
	if pid is String and str(pid) != "":
		_programs_created["Escort"] = str(pid)
		_a.log("CREATE|Escort|OK|pid=%s" % str(pid))
	else:
		_programs_failed["Escort"] = "create_returned_empty"
		_a.log("CREATE|Escort|FAIL|empty_result")


func _create_patrol() -> void:
	if not _bridge.has_method("CreatePatrolProgramV0"):
		_programs_failed["Patrol"] = "method_missing"
		_a.log("CREATE|Patrol|SKIP|method_missing")
		return

	var dest := ""
	if _adjacent_ids.size() > 1:
		dest = _adjacent_ids[1]
	elif _adjacent_ids.size() > 0:
		dest = _adjacent_ids[0]
	else:
		_programs_failed["Patrol"] = "no_adjacent_node"
		_a.log("CREATE|Patrol|SKIP|no_adjacent_node")
		return

	var pid: Variant = _bridge.call("CreatePatrolProgramV0",
		"fleet_trader_1", _current_node_id, dest, 30)
	if pid is String and str(pid) != "":
		_programs_created["Patrol"] = str(pid)
		_a.log("CREATE|Patrol|OK|pid=%s" % str(pid))
	else:
		_programs_failed["Patrol"] = "create_returned_empty"
		_a.log("CREATE|Patrol|FAIL|empty_result")


func _create_survey() -> void:
	if not _bridge.has_method("CreateSurveyProgramV0"):
		_programs_failed["Survey"] = "method_missing"
		_a.log("CREATE|Survey|SKIP|method_missing")
		return

	var pid: Variant = _bridge.call("CreateSurveyProgramV0",
		"ore", _current_node_id, 2, 60)
	if pid is String and str(pid) != "":
		_programs_created["Survey"] = str(pid)
		_a.log("CREATE|Survey|OK|pid=%s" % str(pid))
	else:
		_programs_failed["Survey"] = "create_returned_empty"
		_a.log("CREATE|Survey|FAIL|empty_result")


func _create_expedition() -> void:
	if not _bridge.has_method("CreateExpeditionProgram"):
		_programs_failed["Expedition"] = "method_missing"
		_a.log("CREATE|Expedition|SKIP|method_missing")
		return

	# Expedition requires a lead_id (discovery lead) and fleet_id
	# We may not have a valid discovery lead; try anyway
	var pid: Variant = _bridge.call("CreateExpeditionProgram",
		"lead_placeholder", "fleet_trader_1", 30)
	if pid is String and str(pid) != "":
		_programs_created["Expedition"] = str(pid)
		_a.log("CREATE|Expedition|OK|pid=%s" % str(pid))
	else:
		_programs_failed["Expedition"] = "create_returned_empty_or_no_lead"
		_a.log("CREATE|Expedition|FAIL|likely_no_valid_lead")


func _create_constr_cap() -> void:
	if not _bridge.has_method("CreateConstrCapModuleProgram"):
		_programs_failed["ConstrCapModule"] = "method_missing"
		_a.log("CREATE|ConstrCapModule|SKIP|method_missing")
		return

	# ConstrCapModule requires a construction site_id
	var pid: Variant = _bridge.call("CreateConstrCapModuleProgram",
		_current_node_id, 30)
	if pid is String and str(pid) != "":
		_programs_created["ConstrCapModule"] = str(pid)
		_a.log("CREATE|ConstrCapModule|OK|pid=%s" % str(pid))
	else:
		_programs_failed["ConstrCapModule"] = "create_returned_empty_or_no_site"
		_a.log("CREATE|ConstrCapModule|FAIL|likely_no_valid_site")


# ── Phase: MONITOR_PROGRAMS ──

func _do_monitor_programs() -> void:
	_a.fps_set_phase("monitor")

	_monitor_frames += 1
	if _monitor_frames < _monitor_target:
		return  # Wait for programs to tick

	_busy = true
	_do_monitor_check()


func _do_monitor_check() -> void:
	_a.log("MONITOR|checking after %d frames" % _monitor_frames)

	# Check GetProgramExplainSnapshot
	if _bridge.has_method("GetProgramExplainSnapshot"):
		var explain: Variant = _bridge.call("GetProgramExplainSnapshot")
		if explain is Array:
			_a.log("MONITOR|explain_snapshot|count=%d" % explain.size())
			for prog in explain:
				if prog is Dictionary:
					_a.log("MONITOR|program|id=%s|kind=%s|status=%s" % [
						str(prog.get("id", "")),
						str(prog.get("kind", "")),
						str(prog.get("status", ""))])
			_a.hard(explain.size() >= 1, "programs_visible", "count=%d" % explain.size())
		else:
			_a.warn(false, "explain_snapshot_type", "got=%s" % str(typeof(explain)))

	# Check performance for each created program
	for prog_type in _programs_created:
		var pid: String = str(_programs_created[prog_type])

		# GetProgramPerformanceV0 (fleet-level)
		if _bridge.has_method("GetProgramPerformanceV0"):
			var perf: Variant = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
			if perf is Dictionary and not perf.is_empty():
				_a.log("MONITOR|perf|type=%s|cycles=%s|goods_moved=%s|credits=%s" % [
					prog_type,
					str(perf.get("cycles_run", 0)),
					str(perf.get("goods_moved", 0)),
					str(perf.get("credits_earned", 0))])

		# GetProgramOutcome
		if _bridge.has_method("GetProgramOutcome"):
			var outcome: Variant = _bridge.call("GetProgramOutcome", pid)
			if outcome is Dictionary and not outcome.is_empty():
				_a.log("MONITOR|outcome|type=%s|status=%s|last_emission=%s" % [
					prog_type,
					str(outcome.get("status", "")),
					str(outcome.get("last_emission", ""))])

		# GetProgramEventLogSnapshot
		if _bridge.has_method("GetProgramEventLogSnapshot"):
			var events: Variant = _bridge.call("GetProgramEventLogSnapshot", pid, 10)
			if events is Array:
				_a.log("MONITOR|events|type=%s|count=%d" % [prog_type, events.size()])
				for ev in events:
					if ev is Dictionary:
						_a.log("MONITOR|event|type=%s|tick=%s|etype=%s|note=%s" % [
							prog_type,
							str(ev.get("tick", "")),
							str(ev.get("type", "")),
							str(ev.get("note", ""))])

	_a.hard(_programs_created.size() >= 2, "programs_running",
		"active=%d" % _programs_created.size())

	_busy = false
	_phase = Phase.EVALUATE_LIFECYCLE


# ── Phase: EVALUATE_LIFECYCLE ──

func _do_evaluate_lifecycle() -> void:
	_a.fps_set_phase("lifecycle")
	_busy = true
	_do_lifecycle_tests()


func _do_lifecycle_tests() -> void:
	_a.log("LIFECYCLE|starting")

	var pids: Array = []
	for key in _programs_created:
		pids.append(str(_programs_created[key]))

	if pids.size() < 1:
		_a.warn(false, "lifecycle_skip", "no_programs_to_test")
		_busy = false
		_phase = Phase.CHECK_FAILURES
		return

	# ── Test 1: Pause + Resume on first program ──
	var test_pid := pids[0]
	var test_type := _programs_created.keys()[0]
	_a.log("LIFECYCLE|pause_test|pid=%s|type=%s" % [test_pid, test_type])

	if _bridge.has_method("PauseProgram"):
		var paused: Variant = _bridge.call("PauseProgram", test_pid)
		_lifecycle_pause_ok = (paused is bool and paused == true)
		_a.log("LIFECYCLE|pause|result=%s" % str(paused))

		# Wait a moment, then verify paused state via outcome
		await create_timer(0.3).timeout

		if _bridge.has_method("GetProgramOutcome"):
			var outcome: Variant = _bridge.call("GetProgramOutcome", test_pid)
			if outcome is Dictionary:
				var status := str(outcome.get("status", ""))
				_a.log("LIFECYCLE|pause_verify|status=%s" % status)
				_a.warn(status == "Paused", "pause_status", "got=%s" % status)

	if _bridge.has_method("StartProgram"):
		var resumed: Variant = _bridge.call("StartProgram", test_pid)
		_lifecycle_resume_ok = (resumed is bool and resumed == true)
		_a.log("LIFECYCLE|resume|result=%s" % str(resumed))

		await create_timer(0.3).timeout

		if _bridge.has_method("GetProgramOutcome"):
			var outcome: Variant = _bridge.call("GetProgramOutcome", test_pid)
			if outcome is Dictionary:
				var status := str(outcome.get("status", ""))
				_a.log("LIFECYCLE|resume_verify|status=%s" % status)
				_a.warn(status == "Running", "resume_status", "got=%s" % status)

	# ── Test 2: Cancel on second program (or first if only one) ──
	var cancel_pid := pids[1] if pids.size() > 1 else pids[0]
	var cancel_type := _programs_created.keys()[1] if _programs_created.size() > 1 else _programs_created.keys()[0]
	_a.log("LIFECYCLE|cancel_test|pid=%s|type=%s" % [cancel_pid, cancel_type])

	if _bridge.has_method("CancelProgram"):
		var cancelled: Variant = _bridge.call("CancelProgram", cancel_pid)
		_lifecycle_cancel_ok = (cancelled is bool and cancelled == true)
		_a.log("LIFECYCLE|cancel|result=%s" % str(cancelled))

		await create_timer(0.3).timeout

		if _bridge.has_method("GetProgramOutcome"):
			var outcome: Variant = _bridge.call("GetProgramOutcome", cancel_pid)
			if outcome is Dictionary:
				var status := str(outcome.get("status", ""))
				_a.log("LIFECYCLE|cancel_verify|status=%s" % status)
				_a.warn(status == "Cancelled", "cancel_status", "got=%s" % status)

	# ── Test 3: Postmortem on cancelled program ──
	if _bridge.has_method("GetProgramPostmortemV0"):
		var postmortem: Variant = _bridge.call("GetProgramPostmortemV0", cancel_pid)
		if postmortem is Dictionary:
			_lifecycle_postmortem_ok = true
			_a.log("LIFECYCLE|postmortem|cause=%s|cause_label=%s" % [
				str(postmortem.get("cause_code", "")),
				str(postmortem.get("cause_label", ""))])
			var dec_facts: Variant = postmortem.get("decision_facts", {})
			var cur_facts: Variant = postmortem.get("current_facts", {})
			_a.log("LIFECYCLE|postmortem|decision_facts=%d|current_facts=%d" % [
				dec_facts.size() if dec_facts is Dictionary else 0,
				cur_facts.size() if cur_facts is Dictionary else 0])
		else:
			_a.log("LIFECYCLE|postmortem|FAIL|result_type=%s" % str(typeof(postmortem)))

	# ── Test 4: Failure history ──
	if _bridge.has_method("GetFailureHistoryV0"):
		var history: Variant = _bridge.call("GetFailureHistoryV0", "fleet_trader_1", 10)
		if history is Array:
			_a.log("LIFECYCLE|failure_history|entries=%d" % history.size())
			for entry in history:
				if entry is Dictionary:
					_a.log("LIFECYCLE|failure|cause=%s|count=%s|last_tick=%s" % [
						str(entry.get("cause_code", "")),
						str(entry.get("count", 0)),
						str(entry.get("last_tick", 0))])
		else:
			_a.log("LIFECYCLE|failure_history|type=%s" % str(typeof(history)))

	_lifecycle_tested = _lifecycle_pause_ok or _lifecycle_resume_ok or _lifecycle_cancel_ok
	_busy = false
	_phase = Phase.CHECK_FAILURES


# ── Phase: CHECK_FAILURES ──

func _do_check_failures() -> void:
	_a.fps_set_phase("check_failures")
	_busy = true
	_do_failure_checks()


func _do_failure_checks() -> void:
	_a.log("CHECK_FAILURES|starting")

	# GetProgramTemplatesV0
	if _bridge.has_method("GetProgramTemplatesV0"):
		var templates: Variant = _bridge.call("GetProgramTemplatesV0")
		if templates is Array:
			_a.log("CHECK|templates|count=%d" % templates.size())
			_a.hard(templates.size() >= 1, "templates_exist", "count=%d" % templates.size())
			for tmpl in templates:
				if tmpl is Dictionary:
					_a.log("CHECK|template|id=%s|name=%s|kind=%s" % [
						str(tmpl.get("template_id", "")),
						str(tmpl.get("display_name", "")),
						str(tmpl.get("program_kind", ""))])
		else:
			_a.warn(false, "templates_type", "got=%s" % str(typeof(templates)))
	else:
		_a.warn(false, "templates_missing", "GetProgramTemplatesV0 not found")

	# GetSurveyProgramStatusV0
	if _bridge.has_method("GetSurveyProgramStatusV0"):
		var survey_status: Variant = _bridge.call("GetSurveyProgramStatusV0")
		if survey_status is Dictionary:
			_a.log("CHECK|survey_status|keys=%s" % str(survey_status.keys()))
		else:
			_a.log("CHECK|survey_status|type=%s" % str(typeof(survey_status)))
	else:
		_a.log("CHECK|survey_status|method_missing")

	# GetSurveyResultsV0
	if _bridge.has_method("GetSurveyResultsV0"):
		var survey_results: Variant = _bridge.call("GetSurveyResultsV0")
		if survey_results is Array:
			_a.log("CHECK|survey_results|count=%d" % survey_results.size())
		else:
			_a.log("CHECK|survey_results|type=%s" % str(typeof(survey_results)))
	else:
		_a.log("CHECK|survey_results|method_missing")

	# GetProgramFailureReasonsV0
	if _bridge.has_method("GetProgramFailureReasonsV0"):
		var failure_reasons: Variant = _bridge.call("GetProgramFailureReasonsV0", "fleet_trader_1")
		if failure_reasons is Dictionary:
			_a.log("CHECK|failure_reasons|total=%s|consecutive=%s|last=%s" % [
				str(failure_reasons.get("total_failures", 0)),
				str(failure_reasons.get("consecutive_failures", 0)),
				str(failure_reasons.get("last_failure_reason", ""))])
		else:
			_a.log("CHECK|failure_reasons|type=%s" % str(typeof(failure_reasons)))

	# GetProgramQuote on a created program
	if _bridge.has_method("GetProgramQuote") and _programs_created.size() > 0:
		var first_pid: String = str(_programs_created.values()[0])
		var quote: Variant = _bridge.call("GetProgramQuote", first_pid)
		if quote is Dictionary and not quote.is_empty():
			_a.log("CHECK|quote|kind=%s|unit_price=%s|est_daily=%s" % [
				str(quote.get("kind", "")),
				str(quote.get("unit_price_now", 0)),
				str(quote.get("est_daily_cost_or_value", 0))])
			_a.log("CHECK|quote|market_exists=%s|has_credits=%s|has_supply=%s|has_cargo=%s" % [
				str(quote.get("market_exists", false)),
				str(quote.get("has_enough_credits_now", false)),
				str(quote.get("has_enough_supply_now", false)),
				str(quote.get("has_enough_cargo_now", false))])

	_busy = false
	_phase = Phase.REPORT


# ── Phase: REPORT ──

func _do_report() -> void:
	_a.fps_set_phase("report")

	_a.log("=== AUTOMATION EVAL REPORT ===")
	_a.log("REPORT|seed=%d|total_frames=%d" % [_seed, _total_frames])

	# Program creation results
	_a.log("REPORT|programs_created=%d" % _programs_created.size())
	for prog_type in _programs_created:
		_a.log("REPORT|CREATED|%s|pid=%s" % [prog_type, str(_programs_created[prog_type])])
	_a.log("REPORT|programs_failed=%d" % _programs_failed.size())
	for prog_type in _programs_failed:
		_a.log("REPORT|FAILED|%s|reason=%s" % [prog_type, str(_programs_failed[prog_type])])

	# Lifecycle results
	_a.log("REPORT|lifecycle_tested=%s" % str(_lifecycle_tested))
	_a.log("REPORT|pause=%s|resume=%s|cancel=%s|postmortem=%s" % [
		str(_lifecycle_pause_ok), str(_lifecycle_resume_ok),
		str(_lifecycle_cancel_ok), str(_lifecycle_postmortem_ok)])

	# Trade baseline
	_a.log("REPORT|trade_cycles=%d|goods_traded=%d" % [_trade_cycles, _goods_traded.size()])

	# Summary assertions
	_a.hard(_programs_created.size() >= 3, "program_variety",
		"types=%d" % _programs_created.size())
	_a.warn(_programs_created.size() >= 5, "program_breadth",
		"types=%d" % _programs_created.size())
	_a.hard(_lifecycle_tested, "lifecycle_complete",
		"pause=%s|resume=%s|cancel=%s" % [
			str(_lifecycle_pause_ok), str(_lifecycle_resume_ok),
			str(_lifecycle_cancel_ok)])
	_a.warn(_lifecycle_postmortem_ok, "postmortem_read",
		"postmortem_ok=%s" % str(_lifecycle_postmortem_ok))

	# FPS report
	var fps_data := _a.fps_report()

	# Dead zone analysis
	var dead_zones: Array[Dictionary] = _a.analyze_dead_zones(50)
	for dz in dead_zones:
		_a.log("DEAD_ZONE|start=d%d|end=d%d|gap=%d" % [dz["start"], dz["end"], dz["gap"]])
	_a.warn(dead_zones.is_empty(), "no_dead_zones", "count=%d" % dead_zones.size())

	# Summary
	_a.summary()
	_phase = Phase.DONE
	_force_quit(_a.exit_code())


# ── Utilities ──

func _pick_market_good(node_id: String) -> String:
	if not _bridge.has_method("GetPlayerMarketViewV0"):
		return ""
	var market: Variant = _bridge.call("GetPlayerMarketViewV0", node_id)
	if market is Array:
		for item in market:
			if item is Dictionary:
				var good_id := str(item.get("good_id", ""))
				var qty: int = int(item.get("quantity", 0))
				if good_id != "" and qty > 0:
					return good_id
	return ""


func _force_quit(code: int) -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	_phase = Phase.DONE
	await create_timer(0.2).timeout
	quit(code)
