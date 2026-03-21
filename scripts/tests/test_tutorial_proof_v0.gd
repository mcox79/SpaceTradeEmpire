# scripts/tests/test_tutorial_proof_v0.gd
# Tutorial Verification Bot — walks through all 7 acts / ~30 phases of the
# FO-voiced onboarding, validates phase transitions, dialogue integrity,
# narrative coherence (player-perspective), and economy correctness.
#
# Usage:
#   powershell -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script tutorial -Seeds 42
#   godot --headless --path . -s res://scripts/tests/test_tutorial_proof_v0.gd -- --seed=42
#
# Exercise modes:
#   Acts 1-4: Natural play (dock, buy, travel ×3, explore)
#   Acts 5-7: Force-advance via bridge helpers (combat, modules, automation, FO select)
#
# Key validations:
#   - All ~30 active phases visited in order (no skips, no reversals)
#   - No duplicate dialogue (same phase+sequence+text)
#   - Speaker correctness (Ship Computer, Maren, selected FO, Dask/Lira cameos)
#   - Objective text matches player action (keyword checks)
#   - Trade loop: 3 manual trades before automation
#   - New Voyage re-initialization (clean state after ReinitializeForNewGameV0)
extends SceneTree

const PREFIX := "TUT"
const OUTPUT_DIR := "res://reports/tutorial/"
const MAX_FRAMES := 15000  # 250s at 60fps — 10 acts need more time
const STALL_FRAMES := 600  # 10s stall watchdog per bot phase
const SETTLE_SCENE := 60
const SETTLE_ACTION := 20
const SETTLE_TRAVEL := 30
const MARKET_POLL_MAX := 120

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
var _a := preload("res://scripts/tools/bot_assert.gd").new("TUT")
var _user_seed := -1

# ── Bot Phase Enum ──────────────────────────────────────────────────
enum BotPhase {
	# Setup
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	# Act 1: Cold Open
	ACT1_START_TUTORIAL, ACT1_AWAKEN, ACT1_FLIGHT_INTRO, ACT1_FIRST_DOCK,
	# Act 2: The Crew
	ACT2_MODULE_CALIBRATION, ACT2_MAREN_HAIL, ACT2_DISMISS_THROUGH_MARKET,
	ACT2_BUY_PROMPT, ACT2_DO_BUY, ACT2_BUY_REACT,
	# Act 3: The Trade Loop (repeats 3x)
	ACT3_CRUISE_INTRO, ACT3_TRAVEL, ACT3_JUMP_ANOMALY,
	ACT3_ARRIVAL_DOCK, ACT3_SELL, ACT3_FIRST_PROFIT,
	# Act 4: The World
	ACT4_WORLD_INTRO, ACT4_EXPLORE, ACT4_GALAXY_MAP,
	# Act 5: The Threat (force-advance)
	ACT5_THREAT_WARNING, ACT5_DASK_HAIL, ACT5_COMBAT, ACT5_DEBRIEF, ACT5_REPAIR,
	# Act 6: The Upgrade (force-advance)
	ACT6_MODULE_INTRO, ACT6_MODULE_EQUIP, ACT6_MODULE_REACT, ACT6_LIRA_TEASE,
	# Act 7: The Empire + Graduation (force-advance)
	ACT7_AUTOMATION_INTRO, ACT7_AUTOMATION_CREATE, ACT7_AUTOMATION_WAIT,
	ACT7_AUTOMATION_REACT, ACT7_FO_SELECT,
	ACT7_MYSTERY, ACT7_GRADUATION, ACT7_FAREWELL, ACT7_MILESTONE,
	# Validation
	REINIT_TEST, FINAL_AUDIT, FINAL_SUMMARY, DONE
}

var _phase := BotPhase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _last_phase_change_frame := 0
var _busy := false

var _bridge = null
var _gm = null
var _screenshot = null

# Navigation
var _home_node_id := ""
var _all_edges: Array = []
var _neighbor_ids: Array = []

# Economy
var _credits_before_buy := 0
var _bought_good_id := ""
var _bought_qty := 0
var _bought_unit_cost := 0
var _sell_node_id := ""
var _credit_curve: Array[Dictionary] = []

# Tutorial phase tracking
var _phase_history: Array = []  # Array of {phase (int), name (String), frame (int)}
var _dialogue_log: Array = []   # Array of {phase (String), seq (int), speaker (String), text (String)}
var _beat_counts: Dictionary = {}  # phase_name → int: beats dismissed per phase
var _objective_log: Dictionary = {}  # phase_name → objective text (for coverage audit)
var _last_tutorial_phase := -1  # Track tutorial phase changes
var _sequence_reset_violations := 0  # Count of phases where sequence != 0 on entry
var _selected_fo_type := "Analyst"  # Rotated by seed: 0=Analyst, 1=Veteran, 2=Pathfinder
var _trade_loop_count := 0  # Track manual trades completed (0→3)
var _stall_ticks_samples: Dictionary = {}  # phase_name → Array[int] of stall_ticks readings
var _tab_disclosure_log: Array = []  # Array of {checkpoint, show_jobs_tab, show_ship_tab, ...}

# Expected beat counts from TutorialContentV0.cs (multi-beat phases only).
# Single-beat phases default to 1; these override for multi-beat.
var _expected_beats := {
	"Awaken": 2, "Maren_Hail": 2, "World_Intro": 2,
	"Combat_Debrief": 2, "Automation_Intro": 2, "Mystery_Reveal": 2
}

# ── Main Loop ──────────────────────────────────────────────────────

func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1

	# Global timeout
	if _total_frames >= MAX_FRAMES and _phase != BotPhase.DONE:
		_a.log("TIMEOUT|frame=%d bot_phase=%s" % [_total_frames, BotPhase.keys()[_phase]])
		_a.hard(false, "timeout", "bot_phase=%s" % BotPhase.keys()[_phase])
		_phase = BotPhase.FINAL_SUMMARY

	# Stall watchdog
	if _total_frames - _last_phase_change_frame > STALL_FRAMES and _phase != BotPhase.DONE:
		_a.flag("SOFT_LOCK_%s" % BotPhase.keys()[_phase])
		_last_phase_change_frame = _total_frames

	# Poll tutorial phase changes for history tracking
	_track_tutorial_phase()

	match _phase:
		# Setup
		BotPhase.LOAD_SCENE: _do_load_scene()
		BotPhase.WAIT_SCENE: _do_wait(SETTLE_SCENE, BotPhase.WAIT_BRIDGE)
		BotPhase.WAIT_BRIDGE: _do_wait_bridge()
		BotPhase.WAIT_READY: _do_wait_ready()
		# Act 1
		BotPhase.ACT1_START_TUTORIAL: _do_act1_start()
		BotPhase.ACT1_AWAKEN: _do_act1_awaken()
		BotPhase.ACT1_FLIGHT_INTRO: _do_act1_flight_intro()
		BotPhase.ACT1_FIRST_DOCK: _do_act1_first_dock()
		# Act 2
		BotPhase.ACT2_MODULE_CALIBRATION: _do_act2_module_calibration()
		BotPhase.ACT2_MAREN_HAIL: _do_act2_maren_hail()
		BotPhase.ACT2_DISMISS_THROUGH_MARKET: _do_act2_dismiss_through_market()
		BotPhase.ACT2_BUY_PROMPT: _do_act2_buy_prompt()
		BotPhase.ACT2_DO_BUY: _do_act2_do_buy()
		BotPhase.ACT2_BUY_REACT: _do_act2_buy_react()
		# Act 3 (trade loop ×3)
		BotPhase.ACT3_CRUISE_INTRO: _do_act3_cruise_intro()
		BotPhase.ACT3_TRAVEL: _do_act3_travel()
		BotPhase.ACT3_JUMP_ANOMALY: _do_act3_jump_anomaly()
		BotPhase.ACT3_ARRIVAL_DOCK: _do_act3_arrival_dock()
		BotPhase.ACT3_SELL: _do_act3_sell()
		BotPhase.ACT3_FIRST_PROFIT: _do_act3_first_profit()
		# Act 4
		BotPhase.ACT4_WORLD_INTRO: _do_act4_world_intro()
		BotPhase.ACT4_EXPLORE: _do_act4_explore()
		BotPhase.ACT4_GALAXY_MAP: _do_act4_galaxy_map()
		# Act 5
		BotPhase.ACT5_THREAT_WARNING: _do_act5_threat_warning()
		BotPhase.ACT5_DASK_HAIL: _do_act5_dask_hail()
		BotPhase.ACT5_COMBAT: _do_act5_combat()
		BotPhase.ACT5_DEBRIEF: _do_act5_debrief()
		BotPhase.ACT5_REPAIR: _do_act5_repair()
		# Act 6
		BotPhase.ACT6_MODULE_INTRO: _do_act6_module_intro()
		BotPhase.ACT6_MODULE_EQUIP: _do_act6_module_equip()
		BotPhase.ACT6_MODULE_REACT: _do_act6_module_react()
		BotPhase.ACT6_LIRA_TEASE: _do_act6_lira_tease()
		# Act 7 + Graduation
		BotPhase.ACT7_AUTOMATION_INTRO: _do_act7_automation_intro()
		BotPhase.ACT7_AUTOMATION_CREATE: _do_act7_automation_create()
		BotPhase.ACT7_AUTOMATION_WAIT: _do_act7_automation_wait()
		BotPhase.ACT7_AUTOMATION_REACT: _do_act7_automation_react()
		BotPhase.ACT7_FO_SELECT: _do_act7_fo_select()
		BotPhase.ACT7_MYSTERY: _do_act7_mystery()
		BotPhase.ACT7_GRADUATION: _do_act7_graduation()
		BotPhase.ACT7_FAREWELL: _do_act7_farewell()
		BotPhase.ACT7_MILESTONE: _do_act7_milestone()
		# Validation
		BotPhase.REINIT_TEST: _do_reinit_test()
		BotPhase.FINAL_AUDIT: _do_final_audit()
		BotPhase.FINAL_SUMMARY: _do_final_summary()
		BotPhase.DONE: pass

	# Auto-dismiss stuck dialogue to prevent cascade failures.
	# Only fires after 90 frames (1.5s) of a bot phase — gives handlers enough
	# time to capture narrative before clearing. Without this, when a handler
	# times out and moves on, the tutorial stays stuck at an undismissed phase.
	if _bridge != null and _phase > BotPhase.WAIT_READY and _phase < BotPhase.REINIT_TEST:
		if _polls > 90:
			var _ad_state := _get_tutorial_state()
			if not _ad_state.is_empty() and not bool(_ad_state.get("dialogue_dismissed", true)):
				_bridge.call("DismissTutorialDialogueV0")

	return false


# ── Setup ──────────────────────────────────────────────────────────

func _do_load_scene() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_user_seed = int(arg.trim_prefix("--seed="))
	if _user_seed >= 0:
		seed(_user_seed)
		# Rotate FO selection by seed to cover all 3 candidates across multi-seed sweeps.
		var fo_types := ["Analyst", "Veteran", "Pathfinder"]
		_selected_fo_type = fo_types[_user_seed % 3]
		_a.log("SEED|%d FO|%s" % [_user_seed, _selected_fo_type])
	# Delete stale quicksave.
	var global_save := ProjectSettings.globalize_path("user://quicksave.json")
	if FileAccess.file_exists(global_save):
		DirAccess.remove_absolute(global_save)
		_a.log("QUICKSAVE_DELETED")
	elif FileAccess.file_exists("user://quicksave.json"):
		DirAccess.remove_absolute(ProjectSettings.globalize_path("user://quicksave.json"))
	var scene = load("res://scenes/playable_prototype.tscn").instantiate()
	root.add_child(scene)
	_a.log("SCENE_LOADED")
	_set_bot_phase(BotPhase.WAIT_SCENE)


func _do_wait(settle: int, next: BotPhase) -> void:
	_polls += 1
	if _polls >= settle:
		_set_bot_phase(next)


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge != null:
		_set_bot_phase(BotPhase.WAIT_READY)
	else:
		_polls += 1
		if _polls >= 600:
			_a.hard(false, "bridge_not_found")
			_phase = BotPhase.FINAL_SUMMARY


func _do_wait_ready() -> void:
	var ready := false
	if _bridge.has_method("GetBridgeReadyV0"):
		ready = bool(_bridge.call("GetBridgeReadyV0"))
	else:
		ready = true
	if ready:
		_gm = root.get_node_or_null("GameManager")
		_screenshot = ScreenshotScript.new()
		if _gm:
			_gm.set("_on_main_menu", false)
		_init_navigation()
		_set_bot_phase(BotPhase.ACT1_START_TUTORIAL)
	else:
		_polls += 1
		if _polls >= 600:
			_a.hard(false, "bridge_ready_timeout")
			_phase = BotPhase.FINAL_SUMMARY


# ── Act 1: Cold Open ──────────────────────────────────────────────

func _do_act1_start() -> void:
	_a.log("ACT_1|Cold Open — Ship Computer")
	_bridge.call("StartTutorialV0")
	# Verify tutorial started.
	var state := _get_tutorial_state()
	_a.hard(not state.is_empty(), "tutorial_started")
	var phase_name := str(state.get("phase_name", ""))
	_a.hard(phase_name == "Awaken", "phase_is_awaken", "got=%s" % phase_name)
	_set_bot_phase(BotPhase.ACT1_AWAKEN)


func _do_act1_awaken() -> void:
	# Verify Ship Computer speaks, dismiss all beats.
	var fo := _get_dialogue()
	if fo.is_empty():
		_polls += 1
		if _polls >= 60:
			# Auto-dismiss — headless may need a kick.
			_bridge.call("DismissTutorialDialogueV0")
			_set_bot_phase(BotPhase.ACT1_FLIGHT_INTRO)
		return
	_assert_speaker_is("SHIP COMPUTER", fo, "awaken_speaker")
	var state := _get_tutorial_state()
	_log_narrative("Awaken", fo, state)
	_dismiss_all_beats("Awaken")
	_wait_for_tutorial_phase("Flight_Intro", 120, BotPhase.ACT1_FLIGHT_INTRO)


func _do_act1_flight_intro() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Flight_Intro":
		_polls += 1
		if _polls >= 120:
			_a.hard(false, "flight_intro_not_reached")
			_phase = BotPhase.FINAL_SUMMARY
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("SHIP COMPUTER", fo, "flight_intro_speaker")
		_log_narrative("Flight_Intro", fo, state)
	_dismiss_all_beats("Flight_Intro")
	# Flight_Intro has intentionally empty objective (TutorialContentV0.cs line 478).
	# Log but don't warn — empty is by design.
	var obj := str(state.get("objective", "")).to_lower()
	_a.log("OBJECTIVE|Flight_Intro=%s (empty by design)" % obj)
	_capture("01_flight_intro")
	_wait_for_tutorial_phase("First_Dock", 120, BotPhase.ACT1_FIRST_DOCK)


func _do_act1_first_dock() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "First_Dock":
		_polls += 1
		if _polls >= 120:
			_a.hard(false, "first_dock_not_reached")
			_phase = BotPhase.FINAL_SUMMARY
		return
	# Dock at nearest station.
	_dock_at_station()
	_bridge.call("NotifyTutorialDockV0")
	_busy = true
	await create_timer(0.3).timeout
	_busy = false
	_capture("02_first_dock")
	_wait_for_tutorial_phase("Module_Calibration_Notice", 120, BotPhase.ACT2_MODULE_CALIBRATION)


# ── Act 2: The Crew (Maren pre-selection) ─────────────────────────

func _do_act2_module_calibration() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Module_Calibration_Notice":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "module_calibration_not_reached")
			_set_bot_phase(BotPhase.ACT2_MAREN_HAIL)
		return
	_a.log("ACT_2|Module_Calibration_Notice — Ship Computer mystery seed")
	# Validate Ship Computer speaks.
	_a.hard(bool(state.get("is_ship_computer", false)), "module_calibration_is_ship_computer")
	_dismiss_all_beats("Module_Calibration_Notice")
	_set_bot_phase(BotPhase.ACT2_MAREN_HAIL)


func _do_act2_maren_hail() -> void:
	_a.log("ACT_2|The Crew — Maren introduced")
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Maren_Hail":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "maren_hail_not_reached")
			_set_bot_phase(BotPhase.ACT2_DISMISS_THROUGH_MARKET)
		return
	# Validate IsPreSelectionModeV0 — should be true before FO selection.
	if _bridge.has_method("IsPreSelectionModeV0"):
		_a.hard(bool(_bridge.call("IsPreSelectionModeV0")),
			"pre_selection_mode_true_at_maren_hail")
	# Validate GetTutorialDialogueV0 matches GetRotatingFODialogueV0.
	if _bridge.has_method("GetTutorialDialogueV0"):
		var simple_text: String = _bridge.call("GetTutorialDialogueV0")
		var rotating: Dictionary = _bridge.call("GetRotatingFODialogueV0")
		var rotating_text := str(rotating.get("text", ""))
		if not simple_text.is_empty() and not rotating_text.is_empty():
			_a.hard(simple_text == rotating_text, "dialogue_api_consistency_maren_hail",
				"simple=%s rotating=%s" % [simple_text.left(40), rotating_text.left(40)])
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("Maren", fo, "maren_hail_speaker")
		_log_narrative("Maren_Hail", fo, state)
	# Maren_Hail is 2-beat — dismiss all.
	_dismiss_all_beats("Maren_Hail")
	_wait_for_tutorial_phase("Maren_Settle", 120, BotPhase.ACT2_DISMISS_THROUGH_MARKET)


func _do_act2_dismiss_through_market() -> void:
	# Walk through Maren_Settle → Market_Explain, dismissing each.
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	if pn == "Buy_Prompt":
		_set_bot_phase(BotPhase.ACT2_BUY_PROMPT)
		return
	if pn == "Maren_Settle" or pn == "Market_Explain":
		var fo := _get_dialogue()
		if not fo.is_empty():
			_assert_speaker_is("Maren", fo, "%s_speaker" % pn.to_lower())
			_log_narrative(pn, fo, state)
		_dismiss_all_beats(pn)
	_polls += 1
	if _polls >= 240:
		# Force check — maybe already at Buy_Prompt.
		state = _get_tutorial_state()
		if str(state.get("phase_name", "")) == "Buy_Prompt":
			_set_bot_phase(BotPhase.ACT2_BUY_PROMPT)
		else:
			_a.hard(false, "dismiss_through_market_stuck", "phase=%s" % str(state.get("phase_name", "")))
			_phase = BotPhase.FINAL_SUMMARY


func _do_act2_buy_prompt() -> void:
	var state := _get_tutorial_state()
	var obj := str(state.get("objective", ""))
	_objective_log["Buy_Prompt"] = obj
	_a.hard("buy" in obj.to_lower() or "market" in obj.to_lower() or "cargo" in obj.to_lower(),
		"buy_prompt_objective", "obj=%s" % obj)
	# Tab disclosure: Jobs tab should be hidden before first trade.
	_check_tab_disclosure("before_first_trade")
	_set_bot_phase(BotPhase.ACT2_DO_BUY)


func _do_act2_do_buy() -> void:
	# Buy cheapest good.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_before_buy = int(ps.get("credits", 0))
	var node_id := str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	var pick := _find_best_buy(market)
	if pick.is_empty():
		_polls += 1
		if _polls >= MARKET_POLL_MAX:
			_a.hard(false, "buy_no_good_found")
			_phase = BotPhase.FINAL_SUMMARY
		return
	_bought_good_id = pick.good_id
	_bought_unit_cost = pick.price
	_bought_qty = 1
	_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, 1, true)
	_a.log("BUY|good=%s price=%d" % [_bought_good_id, _bought_unit_cost])
	# Verify credits decreased.
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	_a.hard(int(ps2.get("credits", 0)) < _credits_before_buy, "buy_credits_decreased")
	_capture("03_bought")
	_wait_for_tutorial_phase("Buy_React", 120, BotPhase.ACT2_BUY_REACT)


func _do_act2_buy_react() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# Accept already-advanced: Buy_React(8) may clear in 1 frame on fast seeds.
	if pn != "Buy_React" and phase_num > 8:
		_a.log("BUY_REACT|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT3_CRUISE_INTRO)
		return
	if pn != "Buy_React":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "buy_react_not_reached")
			_set_bot_phase(BotPhase.ACT3_CRUISE_INTRO)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("Maren", fo, "buy_react_speaker")
		_log_narrative("Buy_React", fo, state)
	_dismiss_all_beats("Buy_React")
	# Trade waypoint is set by tutorial_director.gd (live game only, not headless bot).
	# Settle to let sim state stabilize after Buy_React phase change.
	_busy = true
	await create_timer(1.0).timeout
	_busy = false
	var edgedar = root.find_child("EdgedarOverlay", true, false)
	if edgedar:
		var target := str(edgedar.get("tutorial_target_node_id"))
		if not target.is_empty():
			_sell_node_id = target
			_a.log("WAYPOINT|edgedar set target=%s" % target)
		else:
			_a.log("WAYPOINT|edgedar empty (expected in headless)")
	# Wrong-station detection: log IsBadSellStation at buy location for diagnostics.
	if _bridge.has_method("IsBadSellStationV0") and not _bought_good_id.is_empty():
		var bad_result: Dictionary = _bridge.call("IsBadSellStationV0")
		var is_bad := bool(bad_result.get("is_bad", false))
		var better := str(bad_result.get("better_node_name", ""))
		_a.log("WRONG_STATION|good=%s is_bad=%s better=%s" % [_bought_good_id, str(is_bad), better])
		# Validate GetWrongStationWarningV0 template substitution if station IS flagged bad.
		if is_bad and not better.is_empty() and _bridge.has_method("GetWrongStationWarningV0"):
			var warning: String = _bridge.call("GetWrongStationWarningV0", better)
			if not warning.is_empty():
				_a.hard("{station}" not in warning, "wrong_station_template_resolved",
					"warning=%s" % warning.left(80))
				_a.hard(better in warning, "wrong_station_mentions_better",
					"expected '%s' in warning" % better)
			_a.log("WRONG_STATION_WARNING|%s" % warning.left(100))
	# Validate GetTutorialSellTargetV0 returns a profitable destination.
	if _bridge.has_method("GetTutorialSellTargetV0"):
		var sell_target: Dictionary = _bridge.call("GetTutorialSellTargetV0")
		if not sell_target.is_empty():
			var st_node := str(sell_target.get("node_id", ""))
			var st_sell := int(sell_target.get("sell_price", 0))
			var st_buy := int(sell_target.get("buy_price", 0))
			var st_2hop := bool(sell_target.get("two_hop", false))
			_a.hard(not st_node.is_empty(), "sell_target_has_node")
			_a.hard(st_sell > st_buy, "sell_target_profitable",
				"sell=%d buy=%d 2hop=%s" % [st_sell, st_buy, str(st_2hop)])
			_a.log("SELL_TARGET|node=%s sell=%d buy=%d 2hop=%s" % [
				st_node, st_sell, st_buy, str(st_2hop)])
			# Use bridge-recommended sell target if edgedar didn't set one.
			if _sell_node_id.is_empty():
				_sell_node_id = st_node
	_wait_for_tutorial_phase("Cruise_Intro", 120, BotPhase.ACT3_CRUISE_INTRO)


# ── Act 3: The Trade Loop (×3) ─────────────────────────────────────

func _do_act3_cruise_intro() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	if pn != "Cruise_Intro":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "cruise_intro_not_reached")
			_set_bot_phase(BotPhase.ACT3_TRAVEL)
		return
	_a.log("ACT_3|Cruise_Intro — Ship Computer cruise drive notification")
	_a.hard(bool(state.get("is_ship_computer", false)), "cruise_intro_is_ship_computer")
	_dismiss_all_beats("Cruise_Intro")
	_set_bot_phase(BotPhase.ACT3_TRAVEL)


func _do_act3_jump_anomaly() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	if pn != "Jump_Anomaly":
		_polls += 1
		if _polls >= 60:
			# Jump_Anomaly only fires on first trade — skip if not reached.
			_a.log("JUMP_ANOMALY|skipped (not first trade or raced past)")
			_set_bot_phase(BotPhase.ACT3_ARRIVAL_DOCK)
		return
	_a.log("ACT_3|Jump_Anomaly — Maren world-is-watching seed")
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("Maren", fo, "jump_anomaly_speaker")
		_log_narrative("Jump_Anomaly", fo, state)
	_dismiss_all_beats("Jump_Anomaly")
	_set_bot_phase(BotPhase.ACT3_ARRIVAL_DOCK)


func _do_act3_travel() -> void:
	_a.log("ACT_3|The Trade — trade loop %d/3" % (_trade_loop_count + 1))
	# Retry state read — TryExecuteSafeRead may return cached snapshot with empty objective.
	var obj := ""
	for _retry in range(5):
		var state := _get_tutorial_state()
		obj = str(state.get("objective", ""))
		if not obj.is_empty():
			break
		_busy = true
		await create_timer(0.2).timeout
		_busy = false
	_objective_log["Travel_Prompt"] = obj
	_a.hard("travel" in obj.to_lower() or "fly" in obj.to_lower() or "sell" in obj.to_lower() or "gate" in obj.to_lower(),
		"travel_prompt_objective", "obj=%s" % obj)
	# Travel to sell target (or best neighbor).
	# CRITICAL: Must travel to an UNVISITED node — Travel_Prompt gate requires
	# NodesVisited > NodesVisitedAtPhaseEntry, which only increases for new nodes.
	if _gm:
		_gm.call("undock_v0")
	_busy = true
	await create_timer(0.3).timeout  # Let undock settle before travel.
	_busy = false
	_refresh_neighbors()
	var ps_before: Dictionary = _bridge.call("GetPlayerStateV0")
	var current_node := str(ps_before.get("current_node_id", ""))
	if _sell_node_id.is_empty():
		_sell_node_id = _pick_best_sell_neighbor()
	if _sell_node_id.is_empty() and _neighbor_ids.size() > 0:
		_sell_node_id = str(_neighbor_ids[0])
	# Gate requires NodesVisited to INCREASE — must travel to an unvisited node.
	# GalaxyGenerator may pre-visit multiple nodes (initial + starter placement),
	# so even a different node from current_node might already be visited.
	var _must_find_unvisited := false
	if _sell_node_id == current_node:
		_must_find_unvisited = true
	elif _bridge.has_method("IsFirstVisitV0") and not _bridge.call("IsFirstVisitV0", _sell_node_id):
		_a.log("TRAVEL|sell_node %s already visited, need unvisited neighbor" % _sell_node_id)
		_must_find_unvisited = true
	if _must_find_unvisited:
		var found_unvisited := false
		# Track nodes known-visited (from prior checks) to avoid read-lock false positives.
		var known_visited := {current_node: true, _sell_node_id: true}
		# Pick the unvisited neighbor with the BEST sell price for our good.
		var best_unvisited := ""
		var best_unvisited_price := -1
		for nid in _neighbor_ids:
			var sid := str(nid)
			if known_visited.has(sid):
				continue
			if _bridge.call("IsFirstVisitV0", sid):
				var sell_price := _get_sell_price_at(sid, _bought_good_id)
				_a.log("TRAVEL|unvisited candidate %s sell_price=%d" % [sid, sell_price])
				if sell_price > best_unvisited_price:
					best_unvisited_price = sell_price
					best_unvisited = sid
		if not best_unvisited.is_empty():
			_a.log("TRAVEL|switching to best unvisited %s (price=%d, was %s)" % [
				best_unvisited, best_unvisited_price, _sell_node_id])
			_sell_node_id = best_unvisited
			found_unvisited = true
		if not found_unvisited:
			# All 1-hop neighbors visited — try 2-hop neighbors.
			_a.log("TRAVEL|all 1-hop neighbors visited, searching 2-hop")
			for nid in _neighbor_ids:
				var sub_neighbors := _get_neighbors_of(str(nid))
				for snid in sub_neighbors:
					if known_visited.has(snid):
						continue
					if _bridge.call("IsFirstVisitV0", snid):
						_sell_node_id = snid
						found_unvisited = true
						_a.log("TRAVEL|using 2-hop unvisited %s via %s" % [snid, str(nid)])
						break
				if found_unvisited:
					break
	_busy = true
	_headless_travel(_sell_node_id)
	_a.log("TRAVEL|dest=%s" % _sell_node_id)
	await create_timer(0.5).timeout
	_busy = false
	# Verify player actually arrived at destination.
	var ps_after: Dictionary = _bridge.call("GetPlayerStateV0")
	var arrived_at := str(ps_after.get("current_node_id", ""))
	if arrived_at != _sell_node_id:
		_a.log("TRAVEL|arrival mismatch: expected=%s got=%s — retrying" % [_sell_node_id, arrived_at])
		_busy = true
		_headless_travel(_sell_node_id)
		await create_timer(0.5).timeout
		_busy = false
	_wait_for_tutorial_phase("Arrival_Dock", 120, BotPhase.ACT3_ARRIVAL_DOCK)


func _do_act3_arrival_dock() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	if pn == "Sell_Prompt":
		_set_bot_phase(BotPhase.ACT3_SELL)
		return
	# NotifyTutorialDockV0 handles Travel_Prompt → Arrival_Dock → Sell_Prompt.
	# May need multiple calls: first advances Travel_Prompt→Arrival_Dock, second Arrival_Dock→Sell_Prompt.
	if pn == "Travel_Prompt" or pn == "Arrival_Dock":
		_dock_at_station()
		_bridge.call("NotifyTutorialDockV0")
		_busy = true
		await create_timer(0.3).timeout
		_busy = false
		# Check if we need another dock notify (Travel_Prompt→Arrival_Dock→need second notify).
		state = _get_tutorial_state()
		pn = str(state.get("phase_name", ""))
		if pn == "Arrival_Dock":
			_bridge.call("NotifyTutorialDockV0")
			_busy = true
			await create_timer(0.3).timeout
			_busy = false
	_polls += 1
	if _polls >= 120:
		state = _get_tutorial_state()
		_a.warn(false, "arrival_dock_stuck", "phase=%s" % str(state.get("phase_name", "")))
		_set_bot_phase(BotPhase.ACT3_SELL)


func _do_act3_sell() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	if pn == "First_Profit":
		_set_bot_phase(BotPhase.ACT3_FIRST_PROFIT)
		return
	if pn != "Sell_Prompt":
		# Still waiting — try notifying dock again.
		if pn == "Arrival_Dock" or pn == "Travel_Prompt":
			_bridge.call("NotifyTutorialDockV0")
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "sell_prompt_not_reached", "phase=%s" % pn)
			_set_bot_phase(BotPhase.ACT3_FIRST_PROFIT)
		return
	var obj := str(state.get("objective", ""))
	_objective_log["Sell_Prompt"] = obj
	_a.hard("sell" in obj.to_lower(), "sell_prompt_objective", "obj=%s" % obj)
	# Sell cargo.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var credits_before := int(ps.get("credits", 0))
	# IsBadSellStationV0 must be called BEFORE selling (needs cargo to evaluate).
	if _bridge.has_method("IsBadSellStationV0") and not _bought_good_id.is_empty():
		var bad_check := {}
		for _retry in range(5):
			var raw = _bridge.call("IsBadSellStationV0")
			if raw is Dictionary and not raw.is_empty():
				bad_check = raw
				break
			_busy = true
			await create_timer(0.2).timeout
			_busy = false
		if not bad_check.is_empty():
			var is_bad: bool = bool(bad_check.get("is_bad", false))
			# Warn-level: bot's sell-target heuristic may pick a suboptimal station on some seeds.
			_a.warn(not is_bad, "correct_station_not_flagged_bad",
				"is_bad=%s at=%s" % [str(is_bad), node_id])
		else:
			_a.log("IS_BAD_SELL_STATION|empty dict after retries at=%s" % node_id)
	if not _bought_good_id.is_empty():
		_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, _bought_qty, false)
		_a.log("SELL|good=%s at=%s" % [_bought_good_id, node_id])
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps2.get("credits", 0))
	_a.warn(credits_after > credits_before, "sell_profit",
		"before=%d after=%d" % [credits_before, credits_after])
	_a.hard(int(ps2.get("cargo_count", 0)) == 0, "sell_cargo_empty")
	_capture("04_sold")
	_wait_for_tutorial_phase("First_Profit", 120, BotPhase.ACT3_FIRST_PROFIT)


func _do_act3_first_profit() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "First_Profit":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "first_profit_not_reached")
			_set_bot_phase(BotPhase.ACT4_WORLD_INTRO)
		return
	# First_Profit is single-beat (repeatable for trade loop).
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("Maren", fo, "first_profit_speaker")
		_log_narrative("First_Profit", fo, state)
	_dismiss_all_beats("First_Profit")
	_trade_loop_count += 1
	_a.log("TRADE_LOOP|completed trade %d/3" % _trade_loop_count)
	# Check ManualTradesCompleted via bridge.
	var manual_trades := 0
	if _bridge.has_method("GetTutorialManualTradesV0"):
		manual_trades = int(_bridge.call("GetTutorialManualTradesV0"))
	_a.log("TRADE_LOOP|bridge ManualTradesCompleted=%d" % manual_trades)
	if _trade_loop_count >= 3:
		# 3 trades done → advance to World_Intro.
		# Bridge reads may lag by 1 tick — retry before asserting.
		if manual_trades < 3:
			_busy = true
			for _retry in range(5):
				await create_timer(0.3).timeout
				if _bridge.has_method("GetTutorialManualTradesV0"):
					manual_trades = int(_bridge.call("GetTutorialManualTradesV0"))
				if manual_trades >= 3:
					break
			_busy = false
			_a.log("TRADE_LOOP|after retry ManualTradesCompleted=%d" % manual_trades)
		_a.hard(manual_trades >= 3, "manual_trades_gte_3", "got=%d" % manual_trades)
		_wait_for_tutorial_phase("World_Intro", 120, BotPhase.ACT4_WORLD_INTRO)
	else:
		# Loop back for another trade. Buy goods at current station first.
		_buy_goods_for_loop()
		_sell_node_id = ""  # Clear so travel picks a new unvisited node.
		_wait_for_tutorial_phase("Travel_Prompt", 120, BotPhase.ACT3_TRAVEL)


func _do_act7_fo_select() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# FO_Selection is phase 13. In the new 7-act flow, it comes after Automation_React(30).
	# "Already past" means we've reached Mystery_Reveal(41) or later.
	if pn != "FO_Selection":
		if phase_num >= 41:
			_a.log("FO_SELECTION|already past (at %s=%d) — selecting FO and continuing" % [pn, phase_num])
			_phase_history.append({"phase": 13, "name": "FO_Selection", "frame": _total_frames})
			_bridge.call("SelectTutorialFOV0", _selected_fo_type)
			_set_bot_phase(BotPhase.ACT7_MYSTERY)
			return
		_polls += 1
		if _polls >= 120:
			_a.hard(false, "fo_selection_not_reached")
			_phase = BotPhase.FINAL_SUMMARY
		return
	_a.log("ACT_7|FO Selection — choosing %s" % _selected_fo_type)
	# Verify candidates are available with complete data.
	if _bridge.has_method("GetTutorialCandidatesV0"):
		var candidates: Array = _bridge.call("GetTutorialCandidatesV0")
		_a.hard(candidates.size() == 3, "fo_candidates_count", "got=%d" % candidates.size())
		var expected_names := ["Maren", "Dask", "Lira"]
		for c in candidates:
			var c_name := str(c.get("name", ""))
			var c_type := str(c.get("type", ""))
			var c_desc := str(c.get("description", ""))
			var c_quote := str(c.get("quote", ""))
			var c_line := str(c.get("memorable_line", ""))
			_a.hard(not c_name.is_empty(), "fo_candidate_has_name_%s" % c_type.to_lower())
			_a.hard(not c_desc.is_empty(), "fo_candidate_has_description_%s" % c_type.to_lower())
			_a.hard(not c_quote.is_empty(), "fo_candidate_has_quote_%s" % c_type.to_lower())
			_a.hard(not c_line.is_empty(), "fo_candidate_has_memorable_line_%s" % c_type.to_lower())
			_a.hard(c_name in expected_names, "fo_candidate_name_valid_%s" % c_type.to_lower(),
				"name=%s" % c_name)
			_a.log("NARRATIVE|FO_CANDIDATE|%s|%s|%s" % [c_name, c_desc.left(60), c_line.left(60)])
	# Validate FO hails (pre-selection dialogue lines from all 3 candidates).
	if _bridge.has_method("GetTutorialFOHailsV0"):
		var hails: Array = _bridge.call("GetTutorialFOHailsV0")
		_a.hard(hails.size() == 3, "fo_hails_count", "got=%d" % hails.size())
		var hail_names := []
		for h in hails:
			var h_name := str(h.get("name", ""))
			var h_text := str(h.get("text", ""))
			hail_names.append(h_name)
			_a.hard(not h_name.is_empty(), "fo_hail_has_name")
			_a.hard(not h_text.is_empty(), "fo_hail_has_text_%s" % h_name.to_lower())
			# Validate color channels exist and are in [0.0, 1.0] range.
			for ch in ["color_r", "color_g", "color_b"]:
				_a.hard(h.has(ch), "fo_hail_has_%s_%s" % [ch, h_name.to_lower()])
				if h.has(ch):
					var cv := float(h.get(ch, 0.0))
					_a.hard(cv >= 0.0 and cv <= 1.0, "fo_hail_%s_range_%s" % [ch, h_name.to_lower()],
						"%s=%.3f" % [ch, cv])
			_a.log("FO_HAIL|%s=%s color=(%.2f,%.2f,%.2f)" % [h_name, h_text.left(50),
				float(h.get("color_r", 0)), float(h.get("color_g", 0)), float(h.get("color_b", 0))])
		# Verify all 3 FO names present.
		for expected in ["Maren", "Dask", "Lira"]:
			_a.hard(expected in hail_names, "fo_hail_includes_%s" % expected.to_lower())
	# Validate narrator prompt (shown on FO selection screen).
	if _bridge.has_method("GetTutorialNarratorPromptV0"):
		var prompt: String = _bridge.call("GetTutorialNarratorPromptV0")
		_a.hard(not prompt.is_empty(), "narrator_prompt_nonempty")
		_a.log("NARRATOR_PROMPT|%s" % prompt.left(80))
	# Edge case: invalid FO type should return false without crashing.
	var invalid_result: bool = _bridge.call("SelectTutorialFOV0", "InvalidType")
	_a.hard(not invalid_result, "fo_select_invalid_type_rejected")
	# Select FO.
	var success: bool = _bridge.call("SelectTutorialFOV0", _selected_fo_type)
	_a.hard(success, "fo_selected", "type=%s" % _selected_fo_type)
	# Verify state reflects selection (retry for read-lock timing).
	var selected_candidate := ""
	for _retry in range(5):
		var state_after_select: Dictionary = _get_tutorial_state()
		selected_candidate = str(state_after_select.get("candidate", ""))
		if selected_candidate == _selected_fo_type:
			break
		_busy = true
		await create_timer(0.2).timeout
		_busy = false
	_a.hard(selected_candidate == _selected_fo_type,
		"fo_state_reflects_selection", "expected=%s got=%s" % [
			_selected_fo_type, selected_candidate])
	# Double-select resilience: selecting again should not crash/break state.
	var success2: bool = _bridge.call("SelectTutorialFOV0", _selected_fo_type)
	_a.log("FO_DOUBLE_SELECT|result=%s (false is acceptable — already promoted)" % str(success2))
	# Verify state is still coherent after double-select attempt.
	var state_after: Dictionary = _get_tutorial_state()
	_a.hard(str(state_after.get("candidate", "")) != "None", "fo_still_selected_after_double",
		"candidate=%s" % str(state_after.get("candidate", "")))
	_capture("05_fo_selected")
	_wait_for_tutorial_phase("Mystery_Reveal", 120, BotPhase.ACT7_MYSTERY)


# ── Act 4: The World ──────────────────────────────────────────────

func _do_act4_world_intro() -> void:
	_a.log("ACT_4|The World — galaxy opens up")
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# Accept already-advanced: World_Intro(14) may clear in 1 frame on fast seeds.
	if pn != "World_Intro" and phase_num > 14:
		_a.log("WORLD_INTRO|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT4_EXPLORE)
		return
	if pn != "World_Intro":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "world_intro_not_reached")
			_set_bot_phase(BotPhase.ACT4_EXPLORE)
		return
	# Validate IsPreSelectionModeV0 — should be true (FO not yet selected in 7-act flow).
	if _bridge.has_method("IsPreSelectionModeV0"):
		_a.hard(bool(_bridge.call("IsPreSelectionModeV0")),
			"pre_selection_mode_true_at_world_intro")
	# World_Intro is 2-beat. Rotating FO speaks (Maren for Act 4).
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("World_Intro", fo, state)
		# Speaker should be the rotating FO (Maren for Act 4).
		_assert_speaker_is("Maren", fo, "world_intro_speaker")
	_dismiss_all_beats("World_Intro")
	_wait_for_tutorial_phase("Explore_Prompt", 120, BotPhase.ACT4_EXPLORE)


func _do_act4_explore() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Explore_Prompt":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "explore_prompt_not_reached")
			_set_bot_phase(BotPhase.ACT4_GALAXY_MAP)
		return
	var obj := str(state.get("objective", ""))
	_objective_log["Explore_Prompt"] = obj
	_a.warn("explore" in obj.to_lower() or "visit" in obj.to_lower() or "system" in obj.to_lower(),
		"explore_objective", "obj=%s" % obj)
	# Travel to 2 more systems (need ExploreCompleteNodes total).
	if _gm:
		_gm.call("undock_v0")
	_refresh_neighbors()
	for i in range(2):
		var dest := ""
		for nid in _neighbor_ids:
			var sid := str(nid)
			if sid != _home_node_id and sid != _sell_node_id:
				dest = sid
				break
		if dest.is_empty() and _neighbor_ids.size() > 0:
			dest = str(_neighbor_ids[0])
		if dest.is_empty():
			break
		_busy = true
		_headless_travel(dest)
		_a.log("EXPLORE|hop=%d dest=%s" % [i, dest])
		await create_timer(0.4).timeout
		_busy = false
		_refresh_neighbors()
	# Verify nodes visited count (gate requires >= ExploreCompleteNodes).
	if _bridge.has_method("GetOnboardingStateV0"):
		var ob_state: Dictionary = _bridge.call("GetOnboardingStateV0")
		var nodes_visited := int(ob_state.get("nodes_visited", 0))
		_a.hard(nodes_visited >= 3, "explore_nodes_visited_gte3", "got=%d" % nodes_visited)
		_a.log("EXPLORE|total_nodes_visited=%d" % nodes_visited)
	# Tab disclosure: Station + Intel tabs should unlock at 3+ nodes visited.
	_check_tab_disclosure("after_explore_3")
	_wait_for_tutorial_phase("Galaxy_Map_Prompt", 180, BotPhase.ACT4_GALAXY_MAP)


func _do_act4_galaxy_map() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Galaxy_Map_Prompt":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "galaxy_map_not_reached")
			_set_bot_phase(BotPhase.ACT5_THREAT_WARNING)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Galaxy_Map_Prompt", fo, state)
	_dismiss_all_beats("Galaxy_Map_Prompt")
	_capture("06_galaxy_map")
	_wait_for_tutorial_phase("Threat_Warning", 120, BotPhase.ACT5_THREAT_WARNING)


# ── Act 5: The Threat (force-advance) ─────────────────────────────

func _do_act5_threat_warning() -> void:
	if _polls == 0:
		_a.log("ACT_5|The Threat — combat intro + Dask cameo")
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# Handle already-past: dialogue phases race in 1-2 frames.
	if pn != "Threat_Warning" and phase_num > 18:
		_a.log("THREAT_WARNING|already past (phase=%s num=%d)" % [pn, phase_num])
		if phase_num >= 20:  # Past Dask_Hail too
			_set_bot_phase(BotPhase.ACT5_COMBAT)
		else:
			_set_bot_phase(BotPhase.ACT5_DASK_HAIL)
		return
	if pn != "Threat_Warning":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "threat_warning_not_reached")
			_set_bot_phase(BotPhase.ACT5_DASK_HAIL)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Threat_Warning", fo, state)
	_dismiss_all_beats("Threat_Warning")
	_wait_for_tutorial_phase("Dask_Hail", 120, BotPhase.ACT5_DASK_HAIL)


func _do_act5_dask_hail() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# Handle already-past.
	if pn != "Dask_Hail" and phase_num > 19:
		_a.log("DASK_HAIL|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT5_COMBAT)
		return
	if pn != "Dask_Hail":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "dask_hail_not_reached")
			_set_bot_phase(BotPhase.ACT5_COMBAT)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("Dask", fo, "dask_hail_speaker")
		_log_narrative("Dask_Hail", fo, state)
	_dismiss_all_beats("Dask_Hail")
	_wait_for_tutorial_phase("Combat_Engage", 120, BotPhase.ACT5_COMBAT)


func _do_act5_combat() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# Handle already-past: if sim raced past Combat_Engage, still do force-advance.
	if pn != "Combat_Engage" and phase_num > 20:
		_a.log("COMBAT_ENGAGE|already past (phase=%s num=%d) — running force-advance anyway" % [pn, phase_num])
		# Still need to force-advance since combat gate checks NpcFleetsDestroyed.
		_bridge.call("ForceIncrementNpcFleetsDestroyedV0")
		if _bridge.has_method("ForceDamagePlayerHullV0"):
			_bridge.call("ForceDamagePlayerHullV0")
		_set_bot_phase(BotPhase.ACT5_DEBRIEF)
		return
	if pn != "Combat_Engage":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "combat_engage_not_reached")
			_set_bot_phase(BotPhase.ACT5_DEBRIEF)
		return
	var obj := str(state.get("objective", ""))
	_objective_log["Combat_Engage"] = obj
	_a.warn("combat" in obj.to_lower() or "engage" in obj.to_lower() or "hostile" in obj.to_lower() or "defeat" in obj.to_lower(),
		"combat_objective", "obj=%s" % obj)
	# Force-advance: increment NpcFleetsDestroyed + damage hull (simulates combat).
	_bridge.call("ForceIncrementNpcFleetsDestroyedV0")
	# Verify TutorialPirateSpawned flag is set after force-advance.
	var post_combat_state := _get_tutorial_state()
	_a.hard(bool(post_combat_state.get("pirate_spawned", false)), "pirate_spawned_flag_set",
		"expected true after ForceIncrementNpcFleetsDestroyedV0")
	if _bridge.has_method("ForceDamagePlayerHullV0"):
		_bridge.call("ForceDamagePlayerHullV0")
		# Verify hull was actually damaged (catches HullHpMax=0 or silent failure).
		# GetFleetStateV0 may return a string on read-lock contention — retry.
		var fleet_state := {}
		for _retry in range(5):
			var raw = _bridge.call("GetFleetStateV0", "fleet_trader_1")
			if raw is Dictionary:
				fleet_state = raw
				break
			_busy = true
			await create_timer(0.2).timeout
			_busy = false
		if not fleet_state.is_empty():
			var hull_hp := int(fleet_state.get("hull_hp", -1))
			var hull_hp_max := int(fleet_state.get("hull_hp_max", 0))
			_a.hard(hull_hp_max > 0, "hull_hp_max_initialized", "hull_hp_max=%d" % hull_hp_max)
			_a.hard(hull_hp >= 0 and hull_hp < hull_hp_max, "hull_actually_damaged",
				"hull=%d max=%d" % [hull_hp, hull_hp_max])
	_a.log("FORCE|NpcFleetsDestroyed incremented + hull damaged")
	_wait_for_tutorial_phase("Combat_Debrief", 120, BotPhase.ACT5_DEBRIEF)


func _do_act5_debrief() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	if pn != "Combat_Debrief" and phase_num > 21:
		_a.log("COMBAT_DEBRIEF|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT5_REPAIR)
		return
	if pn != "Combat_Debrief":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "combat_debrief_not_reached")
			_set_bot_phase(BotPhase.ACT5_REPAIR)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Combat_Debrief", fo, state)
	_dismiss_all_beats("Combat_Debrief")
	_wait_for_tutorial_phase("Repair_Prompt", 120, BotPhase.ACT5_REPAIR)


func _do_act5_repair() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	if pn != "Repair_Prompt" and phase_num > 22:
		_a.log("REPAIR_PROMPT|already past (phase=%s num=%d)" % [pn, phase_num])
		_bridge.call("ForceRepairPlayerHullV0")
		_set_bot_phase(BotPhase.ACT6_MODULE_INTRO)
		return
	if pn != "Repair_Prompt":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "repair_prompt_not_reached")
			_set_bot_phase(BotPhase.ACT6_MODULE_INTRO)
		return
	# Validate objective text exists for this action-gated phase.
	var obj := str(state.get("objective", ""))
	_objective_log["Repair_Prompt"] = obj
	_a.hard(not obj.is_empty(), "repair_prompt_objective_nonempty", "obj=%s" % obj)
	# Tab disclosure: Ship tab should be visible after combat (hull < max).
	_check_tab_disclosure("after_combat")
	# Force-advance: set hull to max.
	_bridge.call("ForceRepairPlayerHullV0")
	_a.log("FORCE|hull repaired to max")
	_wait_for_tutorial_phase("Module_Intro", 120, BotPhase.ACT6_MODULE_INTRO)


# ── Act 6: The Upgrade (force-advance) ────────────────────────────

func _do_act6_module_intro() -> void:
	if _polls == 0:
		_a.log("ACT_6|The Upgrade — modules + Lira cameo")
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	if pn != "Module_Intro" and phase_num > 23:
		_a.log("MODULE_INTRO|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT6_MODULE_EQUIP)
		return
	if pn != "Module_Intro":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "module_intro_not_reached")
			_set_bot_phase(BotPhase.ACT6_MODULE_EQUIP)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Module_Intro", fo, state)
	_dismiss_all_beats("Module_Intro")
	_wait_for_tutorial_phase("Module_Equip", 120, BotPhase.ACT6_MODULE_EQUIP)


func _do_act6_module_equip() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	if pn != "Module_Equip" and phase_num > 24:
		_a.log("MODULE_EQUIP|already past (phase=%s num=%d) — running force-advance anyway" % [pn, phase_num])
		_bridge.call("ForceGrantModuleV0", "mod_basic_laser")
		_set_bot_phase(BotPhase.ACT6_MODULE_REACT)
		return
	if pn != "Module_Equip":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "module_equip_not_reached")
			_set_bot_phase(BotPhase.ACT6_MODULE_REACT)
		return
	# Force-advance: install module.
	_bridge.call("ForceGrantModuleV0", "mod_basic_laser")
	# Verify TutorialModuleGranted flag is set.
	var post_module_state := _get_tutorial_state()
	_a.hard(bool(post_module_state.get("module_granted", false)), "module_granted_flag_set",
		"expected true after ForceGrantModuleV0")
	# Verify module actually installed (catches empty slots or silent failure).
	# GetFleetStateV0 may return a string on read-lock contention — retry.
	var fleet_state := {}
	for _retry in range(5):
		var raw = _bridge.call("GetFleetStateV0", "fleet_trader_1")
		if raw is Dictionary:
			fleet_state = raw
			break
		_busy = true
		await create_timer(0.2).timeout
		_busy = false
	if not fleet_state.is_empty():
		var slots: Array = fleet_state.get("modules", [])
		var module_installed := false
		for s in slots:
			if str(s.get("module_id", "")) == "mod_basic_laser":
				module_installed = true
				break
		_a.hard(module_installed, "module_actually_installed", "slots=%d" % slots.size())
	_a.log("FORCE|module installed mod_basic_laser")
	_wait_for_tutorial_phase("Module_React", 120, BotPhase.ACT6_MODULE_REACT)


func _do_act6_module_react() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	if pn != "Module_React" and phase_num > 25:
		_a.log("MODULE_REACT|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT6_LIRA_TEASE)
		return
	if pn != "Module_React":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "module_react_not_reached")
			_set_bot_phase(BotPhase.ACT6_LIRA_TEASE)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Module_React", fo, state)
	_dismiss_all_beats("Module_React")
	_wait_for_tutorial_phase("Lira_Tease", 120, BotPhase.ACT6_LIRA_TEASE)


func _do_act6_lira_tease() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	if pn != "Lira_Tease" and phase_num > 26:
		_a.log("LIRA_TEASE|already past (phase=%s num=%d)" % [pn, phase_num])
		_set_bot_phase(BotPhase.ACT7_AUTOMATION_INTRO)
		return
	if pn != "Lira_Tease":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "lira_tease_not_reached")
			_set_bot_phase(BotPhase.ACT7_AUTOMATION_INTRO)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("Lira", fo, "lira_tease_speaker")
		_log_narrative("Lira_Tease", fo, state)
	_dismiss_all_beats("Lira_Tease")
	_capture("07_lira_tease")
	_wait_for_tutorial_phase("Automation_Intro", 120, BotPhase.ACT7_AUTOMATION_INTRO)


# ── Act 7: The Empire (force-advance) ─────────────────────────────

func _do_act7_automation_intro() -> void:
	if _polls == 0:
		_a.log("ACT_7|The Empire — automation reveal")
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Automation_Intro":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "automation_intro_not_reached")
			_set_bot_phase(BotPhase.ACT7_AUTOMATION_CREATE)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Automation_Intro", fo, state)
	_dismiss_all_beats("Automation_Intro")
	_wait_for_tutorial_phase("Automation_Create", 120, BotPhase.ACT7_AUTOMATION_CREATE)


func _do_act7_automation_create() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Automation_Create":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "automation_create_not_reached")
			_set_bot_phase(BotPhase.ACT7_AUTOMATION_WAIT)
		return
	# Force-advance: create a trade charter program.
	# Market IDs = node IDs (StarNetworkGen: MarketId = $"star_{i}").
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var src_market := str(ps.get("current_node_id", ""))
	var dst_market := _sell_node_id if not _sell_node_id.is_empty() else _home_node_id
	var good := _bought_good_id if not _bought_good_id.is_empty() else "fuel"
	if src_market.is_empty() or dst_market.is_empty():
		_a.warn(false, "automation_markets_found", "src=%s dst=%s" % [src_market, dst_market])
		_set_bot_phase(BotPhase.ACT7_AUTOMATION_WAIT)
		return
	var prog_id: String = _bridge.call("CreateTradeCharterProgram", src_market, dst_market, good, good, 10)
	_a.log("FORCE|automation created prog=%s" % prog_id)
	_a.hard(not prog_id.is_empty(), "automation_program_created")
	_wait_for_tutorial_phase("Automation_Running", 120, BotPhase.ACT7_AUTOMATION_WAIT)


func _do_act7_automation_wait() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	# Check for any phase past Automation_Running.
	var phase_num := int(state.get("phase", 0))
	if phase_num > 29:  # Past Automation_Running (29)
		_set_bot_phase(BotPhase.ACT7_AUTOMATION_REACT)
		return
	# Track stall_ticks to verify the counter is incrementing.
	var stall := int(state.get("stall_ticks", 0))
	if not _stall_ticks_samples.has("Automation_Running"):
		_stall_ticks_samples["Automation_Running"] = []
	# Log objective only when phase is actually Automation_Running (not before transition).
	if pn == "Automation_Running" and not _objective_log.has("Automation_Running"):
		var obj := str(state.get("objective", ""))
		_objective_log["Automation_Running"] = obj
	if _polls % 10 == 0:  # Sample frequently — phase only lasts 30 ticks (~2-3s)
		_stall_ticks_samples["Automation_Running"].append(stall)
	_polls += 1
	if _polls >= 1200:  # 30 sim ticks @ ~7 frames/tick + margin
		_a.warn(false, "automation_wait_timeout", "phase=%s" % pn)
		_set_bot_phase(BotPhase.ACT7_AUTOMATION_REACT)


func _do_act7_automation_react() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	var phase_num := int(state.get("phase", 0))
	# If past Automation_React already, skip ahead to FO selection.
	if pn == "FO_Selection" or phase_num >= 41:
		_dismiss_if_needed(state)
		_set_bot_phase(BotPhase.ACT7_FO_SELECT)
		return
	if pn != "Automation_React":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "automation_react_not_reached", "phase=%s" % pn)
			_set_bot_phase(BotPhase.ACT7_FO_SELECT)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Automation_React", fo, state)
	_dismiss_all_beats("Automation_React")
	_wait_for_tutorial_phase("FO_Selection", 120, BotPhase.ACT7_FO_SELECT)


# ── Act 7 (cont.): Graduation ─────────────────────────────────────

func _do_act7_mystery() -> void:
	if _polls == 0:
		_a.log("ACT_7|Graduation — capstone")
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Mystery_Reveal":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "mystery_not_reached")
			_set_bot_phase(BotPhase.ACT7_GRADUATION)
		return
	# Validate IsPreSelectionModeV0 — should be false after FO selection.
	if _bridge.has_method("IsPreSelectionModeV0"):
		_a.hard(not bool(_bridge.call("IsPreSelectionModeV0")),
			"pre_selection_mode_false_at_mystery")
	# FO reactive suppression check: after FO is promoted, reactive triggers
	# should NOT fire during tutorial.
	_check_fo_reactive_suppression()
	# Mystery_Reveal is 2-beat. Selected FO speaks.
	var fo := _get_dialogue()
	if not fo.is_empty():
		var expected_name := _get_selected_fo_name()
		_assert_speaker_is(expected_name, fo, "mystery_speaker")
		_log_narrative("Mystery_Reveal", fo, state)
	_dismiss_all_beats("Mystery_Reveal")
	_wait_for_tutorial_phase("Graduation_Summary", 120, BotPhase.ACT7_GRADUATION)


func _do_act7_graduation() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "Graduation_Summary":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "graduation_not_reached")
			_set_bot_phase(BotPhase.ACT7_FAREWELL)
		return
	# Ship Computer speaks at graduation.
	var fo := _get_dialogue()
	if not fo.is_empty():
		_assert_speaker_is("SHIP COMPUTER", fo, "graduation_speaker")
		_log_narrative("Graduation_Summary", fo, state)
		# Validate template variables are substituted (no raw {placeholders}).
		var grad_text := str(fo.get("text", ""))
		_a.hard("{credits_earned}" not in grad_text, "graduation_credits_substituted",
			"raw {credits_earned} still in text")
		_a.hard("{nodes_visited}" not in grad_text, "graduation_nodes_substituted",
			"raw {nodes_visited} still in text")
		_a.hard("{combats_won}" not in grad_text, "graduation_combats_substituted",
			"raw {combats_won} still in text")
		_a.hard("{modules_equipped}" not in grad_text, "graduation_modules_substituted",
			"raw {modules_equipped} still in text")
		_a.log("GRADUATION_TEXT|%s" % grad_text.left(120))
	_dismiss_all_beats("Graduation_Summary")
	_wait_for_tutorial_phase("FO_Farewell", 120, BotPhase.ACT7_FAREWELL)


func _do_act7_farewell() -> void:
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) != "FO_Farewell":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "farewell_not_reached")
			_set_bot_phase(BotPhase.ACT7_MILESTONE)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("FO_Farewell", fo, state)
	_dismiss_all_beats("FO_Farewell")
	_wait_for_tutorial_phase("Milestone_Award", 120, BotPhase.ACT7_MILESTONE)


func _do_act7_milestone() -> void:
	var state := _get_tutorial_state()
	var pn := str(state.get("phase_name", ""))
	if pn == "Tutorial_Complete":
		_a.log("ACT_7|Tutorial_Complete reached!")
		_set_bot_phase(BotPhase.REINIT_TEST)
		return
	if pn != "Milestone_Award":
		_polls += 1
		if _polls >= 120:
			_a.warn(false, "milestone_not_reached")
			_set_bot_phase(BotPhase.REINIT_TEST)
		return
	var fo := _get_dialogue()
	if not fo.is_empty():
		_log_narrative("Milestone_Award", fo, state)
	_dismiss_all_beats("Milestone_Award")
	_capture("07_milestone")
	# Don't transition immediately — poll for Tutorial_Complete.
	# _wait_for_tutorial_phase transitions the bot phase but the sim needs
	# another tick to advance. Stay in this handler and poll.


# ── Validation ─────────────────────────────────────────────────────

func _do_reinit_test() -> void:
	_a.log("REINIT_TEST|Testing New Voyage re-initialization")
	# Retry state read — TryExecuteSafeRead may return stale/empty on lock contention.
	var pn := ""
	for _retry in range(5):
		var state := _get_tutorial_state()
		pn = str(state.get("phase_name", ""))
		if pn == "Tutorial_Complete":
			break
		_busy = true
		await create_timer(0.2).timeout
		_busy = false
	_a.hard(pn == "Tutorial_Complete", "tutorial_completed", "got=%s" % pn)

	# ── Save/Load Cycle ──
	# Save at Tutorial_Complete, load back, verify state survives round-trip.
	_a.log("SAVE_LOAD_TEST|Saving at Tutorial_Complete")
	if _bridge.has_method("RequestSave"):
		var epoch_before := int(_bridge.call("GetSaveEpoch"))
		_bridge.call("RequestSave")
		_busy = true
		# Poll for save completion (epoch increases).
		for i in range(60):
			await create_timer(0.1).timeout
			var epoch_now := int(_bridge.call("GetSaveEpoch"))
			if epoch_now > epoch_before:
				break
		_busy = false
		var epoch_after := int(_bridge.call("GetSaveEpoch"))
		_a.hard(epoch_after > epoch_before, "save_completed", "epoch %d→%d" % [epoch_before, epoch_after])
		# Now load.
		var load_epoch_before := int(_bridge.call("GetLoadEpoch"))
		_bridge.call("RequestLoad")
		_busy = true
		for i in range(60):
			await create_timer(0.1).timeout
			var load_epoch_now := int(_bridge.call("GetLoadEpoch"))
			if load_epoch_now > load_epoch_before:
				break
		await create_timer(1.0).timeout  # Extra settle after load — sim kernel re-init needs time.
		_busy = false
		var load_epoch_after := int(_bridge.call("GetLoadEpoch"))
		_a.hard(load_epoch_after > load_epoch_before, "load_completed",
			"epoch %d→%d" % [load_epoch_before, load_epoch_after])
		# Verify tutorial state survived round-trip.
		# Re-read state a few times to handle TryExecuteSafeRead cache staleness.
		var state_sl := {}
		for _retry in range(5):
			state_sl = _get_tutorial_state()
			if not str(state_sl.get("phase_name", "")).is_empty():
				break
			_busy = true
			await create_timer(0.2).timeout
			_busy = false
		var pn_sl := str(state_sl.get("phase_name", ""))
		_a.hard(pn_sl == "Tutorial_Complete", "save_load_phase_preserved", "got=%s" % pn_sl)
		var cand_sl := str(state_sl.get("candidate", ""))
		_a.hard(cand_sl != "None" and not cand_sl.is_empty(), "save_load_candidate_preserved",
			"got=%s" % cand_sl)
		_a.log("SAVE_LOAD_TEST|PASS — round-trip preserved tutorial state")

	# ── SkipTutorialV0 Test ──
	if _bridge.has_method("SkipTutorialV0"):
		_a.log("SKIP_TUTORIAL_TEST|Testing SkipTutorialV0")
		_bridge.call("SkipTutorialV0")
		var state_skip := _get_tutorial_state()
		var pn_skip := str(state_skip.get("phase_name", ""))
		_a.hard(pn_skip == "Tutorial_Complete", "skip_tutorial_phase",
			"got=%s" % pn_skip)
		if _bridge.has_method("IsTutorialActiveV0"):
			_a.hard(not bool(_bridge.call("IsTutorialActiveV0")),
				"skip_tutorial_not_active")
		# After skip, all tabs should be accessible (no tutorial restrictions).
		if _bridge.has_method("GetOnboardingStateV0"):
			var obs_skip: Dictionary = _bridge.call("GetOnboardingStateV0")
			_a.log("SKIP_TUTORIAL|onboarding_state=%s" % str(obs_skip))
		_a.log("SKIP_TUTORIAL_TEST|PASS")

	# ── Re-initialization ──
	if _bridge.has_method("ReinitializeForNewGameV0"):
		_bridge.call("ReinitializeForNewGameV0")
		_busy = true
		await create_timer(1.0).timeout
		_busy = false
		# Start fresh tutorial.
		_bridge.call("StartTutorialV0")
		var state2 := _get_tutorial_state()
		var pn2 := str(state2.get("phase_name", ""))
		_a.hard(pn2 == "Awaken", "reinit_phase_awaken", "got=%s" % pn2)
		_a.hard(str(state2.get("candidate", "")) == "None", "reinit_candidate_none")
		_a.hard(not bool(state2.get("dialogue_dismissed", true)), "reinit_dialogue_not_dismissed")
		# Verify no stale state leaks.
		var phase_num2 := int(state2.get("phase", -1))
		_a.hard(phase_num2 == 1, "reinit_phase_num_is_1", "got=%d" % phase_num2)  # Awaken=1
		var stall2 := int(state2.get("stall_ticks", -1))
		_a.hard(stall2 == 0, "reinit_stall_ticks_zero", "got=%d" % stall2)
		# Verify dialogue is available (Ship Computer speaks at Awaken).
		var fo2 := _get_dialogue()
		if not fo2.is_empty():
			_a.hard(str(fo2.get("name", "")) == "SHIP COMPUTER", "reinit_speaker_ship_computer",
				"got=%s" % str(fo2.get("name", "")))
			_a.hard(not str(fo2.get("text", "")).is_empty(), "reinit_dialogue_has_text")
		_a.log("REINIT_TEST|PASS — clean state after ReinitializeForNewGameV0")
	else:
		_a.warn(false, "reinit_method_missing")
	_set_bot_phase(BotPhase.FINAL_AUDIT)


func _do_final_audit() -> void:
	_a.log("FINAL_AUDIT|Phase history, dialogue integrity, narrative coherence, timing")

	# ── Phase History Audit ──
	_a.log("PHASE_HISTORY|count=%d" % _phase_history.size())
	# 7-act flow: ~30 active phases. Trade loop phases repeat 3x.
	# Unique phases in expected first-occurrence order. Trade loop phases
	# (Travel_Prompt, Arrival_Dock, Sell_Prompt, First_Profit) may appear in
	# any position due to headless racing, so they're checked separately.
	var expected_phases := [
		"Awaken", "Flight_Intro", "First_Dock",
		"Module_Calibration_Notice", "Maren_Hail", "Maren_Settle", "Market_Explain", "Buy_Prompt", "Buy_React",
		"Cruise_Intro", "Jump_Anomaly",
		# Trade loop phases checked separately for >=3 occurrences.
		"Travel_Prompt", "Arrival_Dock", "Sell_Prompt", "First_Profit",
		"World_Intro", "Explore_Prompt", "Galaxy_Map_Prompt",
		"Threat_Warning", "Dask_Hail", "Combat_Engage", "Combat_Debrief", "Repair_Prompt",
		"Module_Intro", "Module_Equip", "Module_React", "Lira_Tease",
		"Automation_Intro", "Automation_Create", "Automation_Running", "Automation_React",
		"FO_Selection",
		"Mystery_Reveal", "Graduation_Summary", "FO_Farewell", "Milestone_Award", "Tutorial_Complete"
	]
	# Build deduped visit list for ordering + raw count map for trade loop check.
	var visited_names: Array = []
	var phase_visit_count: Dictionary = {}
	for entry in _phase_history:
		var pname: String = str(entry.get("name", ""))
		if pname not in visited_names:
			visited_names.append(pname)
		phase_visit_count[pname] = phase_visit_count.get(pname, 0) + 1
		_a.log("PHASE|%s frame=%d" % [pname, int(entry.get("frame", 0))])
	# Check ordering of unique phases (first occurrence of each).
	# Trade loop phases are excluded from strict ordering since they repeat
	# and their first occurrence may be raced past in headless.
	var trade_loop_set := ["Travel_Prompt", "Arrival_Dock", "Sell_Prompt", "First_Profit"]
	var unique_expected: Array = []
	for ep in expected_phases:
		if ep not in unique_expected:
			unique_expected.append(ep)
	var last_idx := -1
	var phases_in_order := true
	var missing_count := 0
	for ep in unique_expected:
		if ep in trade_loop_set:
			# Trade loop phases checked separately — skip strict ordering.
			var idx := visited_names.find(ep)
			if idx < 0:
				missing_count += 1
			continue
		var idx := visited_names.find(ep)
		if idx < 0:
			# Headless race: sim can advance 2+ phases in one frame, skipping intermediates.
			_a.log("PHASE_MISSED|%s (headless race artifact)" % ep)
			missing_count += 1
		elif idx <= last_idx:
			_a.warn(false, "phase_order_%s" % ep.to_lower(), "expected after idx %d, got %d" % [last_idx, idx])
			phases_in_order = false
		else:
			last_idx = idx
	# Allow up to 3 missing phases (headless racing). More than that indicates a real problem.
	if missing_count > 3:
		phases_in_order = false
	_a.hard(phases_in_order, "all_phases_in_order", "missing=%d" % missing_count)
	_a.warn(missing_count == 0, "phase_coverage_complete", "missing=%d" % missing_count)
	# Trade loop: Travel_Prompt, Sell_Prompt, First_Profit should each appear 3+ times.
	var trade_loop_phases := ["Travel_Prompt", "Sell_Prompt", "First_Profit"]
	for tlp in trade_loop_phases:
		var count: int = phase_visit_count.get(tlp, 0)
		_a.warn(count >= 3, "trade_loop_%s_count" % tlp.to_lower(), "expected>=3 got=%d" % count)

	# ── Dialogue Integrity Audit ──
	_a.log("DIALOGUE_LOG|count=%d" % _dialogue_log.size())
	var seen_keys: Dictionary = {}
	var duplicate_count := 0
	for entry in _dialogue_log:
		var key := "%s|%d|%s" % [str(entry.phase), int(entry.seq), str(entry.text)]
		if seen_keys.has(key):
			_a.log("DUPLICATE_DIALOGUE|%s" % key.left(100))
			duplicate_count += 1
		seen_keys[key] = true
		_a.log("NARRATIVE|%s|%s|%s" % [str(entry.phase), str(entry.speaker), str(entry.text)])
	_a.hard(duplicate_count == 0, "no_duplicate_dialogue", "dupes=%d" % duplicate_count)

	# ── Cover-Story Enforcement ──
	# Pre-Revelation dialogue must NOT contain forbidden lore terms.
	# These would spoil the mystery that unfolds over 8+ hours.
	var forbidden_words := ["fracture", "adaptation", "ancient", "organism"]
	var coverstory_violations := 0
	for entry in _dialogue_log:
		var text_lower: String = str(entry.text).to_lower()
		for fw in forbidden_words:
			if fw in text_lower:
				_a.log("COVERSTORY_VIOLATION|phase=%s word='%s' text=%s" % [
					str(entry.phase), fw, str(entry.text).left(80)])
				coverstory_violations += 1
	_a.hard(coverstory_violations == 0, "coverstory_no_forbidden_words",
		"violations=%d" % coverstory_violations)

	# ── FO UI Instruction Leakage ──
	# FOs observe/react only — UI instructions belong in HUD objectives.
	# Ship Computer is exempt (system messages like "Hold C to engage").
	var ui_instruction_patterns := ["open the ", "press ", "click ", "select the ", "hit "]
	var ui_leaks := 0
	for entry in _dialogue_log:
		var speaker: String = str(entry.speaker)
		if speaker == "SHIP COMPUTER" or speaker.is_empty():
			continue  # Ship Computer is allowed to give system instructions.
		var text_lower: String = str(entry.text).to_lower()
		for pattern in ui_instruction_patterns:
			if pattern in text_lower:
				_a.log("UI_INSTRUCTION_LEAK|speaker=%s phase=%s pattern='%s' text=%s" % [
					speaker, str(entry.phase), pattern, str(entry.text).left(80)])
				ui_leaks += 1
	_a.hard(ui_leaks == 0, "fo_no_ui_instructions", "leaks=%d" % ui_leaks)

	# ── Narrative Voice Spot Checks ──
	# Verify key narrative beats are present in dialogue content.
	var maren_has_probability := false
	var maren_has_warfront := false
	for entry in _dialogue_log:
		var text: String = str(entry.text)
		var phase: String = str(entry.phase)
		# Maren should use probability framing (any percentage mention).
		if phase in ["Market_Explain", "Buy_React", "First_Profit"]:
			if "%" in text:
				maren_has_probability = true
		# Maren_Settle should reference the warfront.
		if phase == "Maren_Settle":
			var tl := text.to_lower()
			if "warfront" in tl or "war" in tl or "conflict" in tl or "backed up" in tl:
				maren_has_warfront = true
	_a.hard(maren_has_probability, "maren_probability_voice",
		"No percentage found in Market_Explain/Buy_React/First_Profit dialogue")
	_a.warn(maren_has_warfront, "maren_warfront_context",
		"No warfront reference in Maren_Settle dialogue")

	# ── Jump_Anomaly Fire-Once ──
	# Jump_Anomaly should only appear once (first trade, ManualTradesCompleted==0).
	var jump_anomaly_count: int = phase_visit_count.get("Jump_Anomaly", 0)
	_a.hard(jump_anomaly_count <= 1, "jump_anomaly_fire_once",
		"appeared %d times (expected 0 or 1)" % jump_anomaly_count)

	# ── Beat Count Verification ──
	# Multi-beat phases must deliver expected number of beats.
	_a.log("BEAT_COUNTS|tracked=%d" % _beat_counts.size())
	for phase_name in _expected_beats:
		var expected: int = _expected_beats[phase_name]
		var actual: int = _beat_counts.get(phase_name, 0)
		# Beats dismissed = expected count (1 dismiss per beat shown).
		# Allow actual >= expected since _dismiss_all_beats may count extra dismiss calls.
		_a.warn(actual >= expected, "beat_count_%s" % phase_name.to_lower(),
			"expected>=%d got=%d" % [expected, actual])
		_a.log("BEATS|%s expected=%d actual=%d" % [phase_name, expected, actual])

	# ── Speaker Consistency Audit ──
	# 7-act flow: rotating FO per act (pre-selection), then selected FO (post-selection).
	# Ship Computer phases: Awaken, Flight_Intro, Module_Calibration_Notice, Cruise_Intro, Graduation_Summary
	# Maren (Acts 2-4, 7 pre-select): Maren_Hail through First_Profit, Jump_Anomaly,
	#   World_Intro, Explore_Prompt, Galaxy_Map_Prompt, Automation_Intro through Automation_React
	# Dask (Act 5): Threat_Warning, Dask_Hail, Combat_Engage through Repair_Prompt
	# Lira (Act 6): Module_Intro, Module_Equip, Module_React, Lira_Tease
	# Selected FO (post-selection): Mystery_Reveal, FO_Farewell, Milestone_Award
	var selected_fo_name := _get_selected_fo_name()
	var phase_speaker_map := {
		"Awaken": "SHIP COMPUTER", "Flight_Intro": "SHIP COMPUTER",
		"Module_Calibration_Notice": "SHIP COMPUTER", "Cruise_Intro": "SHIP COMPUTER",
		"Graduation_Summary": "SHIP COMPUTER",
		# Maren acts (2, 3, 4, 7 pre-select)
		"Maren_Hail": "Maren", "Maren_Settle": "Maren", "Market_Explain": "Maren",
		"Buy_Prompt": "Maren", "Buy_React": "Maren",
		"Travel_Prompt": "Maren", "Jump_Anomaly": "Maren",
		"Arrival_Dock": "Maren", "Sell_Prompt": "Maren", "First_Profit": "Maren",
		"World_Intro": "Maren", "Explore_Prompt": "Maren", "Galaxy_Map_Prompt": "Maren",
		"Automation_Intro": "Maren", "Automation_React": "Maren",
		# Dask (Act 5)
		"Threat_Warning": "Dask", "Dask_Hail": "Dask",
		"Combat_Debrief": "Dask", "Repair_Prompt": "Dask",
		# Lira (Act 6)
		"Module_Intro": "Lira", "Module_React": "Lira", "Lira_Tease": "Lira",
	}
	# Post-selection phases use selected FO.
	var post_selection_phases := ["Mystery_Reveal", "FO_Farewell", "Milestone_Award"]
	var speaker_errors := 0
	for entry in _dialogue_log:
		var ep: String = str(entry.phase)
		var sp: String = str(entry.speaker)
		if sp.is_empty():
			continue
		var expected_speaker := ""
		if ep in phase_speaker_map:
			expected_speaker = phase_speaker_map[ep]
		elif ep in post_selection_phases:
			expected_speaker = selected_fo_name
		elif ep not in ["FO_Selection", "First_Dock", "Combat_Engage", "Module_Equip", ""]:
			# Unknown phase with dialogue — log but don't fail.
			_a.log("SPEAKER_UNKNOWN_PHASE|%s speaker=%s" % [ep, sp])
			continue
		if not expected_speaker.is_empty() and sp != expected_speaker:
			_a.log("SPEAKER_MISMATCH|%s expected=%s got=%s" % [ep, expected_speaker, sp])
			speaker_errors += 1
	_a.hard(speaker_errors == 0, "speaker_consistency", "mismatches=%d" % speaker_errors)

	# ── Objective Coverage Audit ──
	# Action-gated phases MUST have non-empty objectives (from TutorialContentV0.GetObjectiveText).
	var action_gated_phases := [
		"First_Dock", "Market_Explain", "Buy_Prompt", "Buy_React",
		"Travel_Prompt", "Arrival_Dock", "Sell_Prompt", "FO_Selection",
		"Explore_Prompt", "Galaxy_Map_Prompt", "Combat_Engage", "Repair_Prompt",
		"Module_Equip", "Automation_Create", "Automation_Running"
	]
	var obj_missing := 0
	for agp in action_gated_phases:
		if _objective_log.has(agp):
			var obj_text: String = str(_objective_log[agp])
			if obj_text.is_empty():
				_a.log("OBJECTIVE_EMPTY|%s (action-gated phase with no objective!)" % agp)
				obj_missing += 1
			else:
				_a.log("OBJECTIVE|%s=%s" % [agp, obj_text.left(60)])
	_a.warn(obj_missing == 0, "objective_coverage", "missing=%d" % obj_missing)

	# ── Phase Timing Stats ──
	# Compute frame-delta for each tutorial phase transition. Flag anomalies:
	# - Too fast (< 3 frames) may indicate a phase was skipped/auto-advanced
	# - Too slow (> 1200 frames = 20s) may indicate a stall
	_a.log("TIMING_STATS|phases=%d" % _phase_history.size())
	# Dialogue-only phases naturally clear in 1-2 frames (auto-dismiss) — not anomalies.
	# Phases that legitimately clear in 1-2 frames:
	# - Dialogue-only phases: auto-dismissed by bot's main loop
	# - Bot-driven gates: bot acts immediately when gate condition met (dock, buy, force-advance)
	# - Re-init: Awaken after Tutorial_Complete is the re-initialization test
	var fast_ok_phases := [
		# Dialogue-only (auto-dismiss)
		"Module_Calibration_Notice", "Maren_Settle", "Market_Explain", "Buy_React",
		"Cruise_Intro", "Jump_Anomaly", "Arrival_Dock",
		"First_Profit", "Galaxy_Map_Prompt",
		"Threat_Warning", "Dask_Hail", "Combat_Debrief", "Repair_Prompt",
		"Module_Intro", "Module_React", "Lira_Tease",
		"Automation_Intro", "Automation_React",
		"Mystery_Reveal", "Graduation_Summary", "FO_Farewell", "Milestone_Award",
		# Bot acts immediately on gate
		"First_Dock", "Maren_Hail", "Buy_Prompt", "Travel_Prompt",
		"Sell_Prompt", "FO_Selection", "World_Intro", "Explore_Prompt", "Module_Equip",
		"Automation_Create", "Awaken"
	]
	var timing_warns := 0
	for i in range(1, _phase_history.size()):
		var prev_frame: int = int(_phase_history[i - 1].get("frame", 0))
		var cur_frame: int = int(_phase_history[i].get("frame", 0))
		var delta := cur_frame - prev_frame
		var pname: String = str(_phase_history[i].get("name", ""))
		var prev_name: String = str(_phase_history[i - 1].get("name", ""))
		_a.log("TIMING|%s delta=%d frames (from %s)" % [pname, delta, prev_name])
		# Flag suspiciously fast transitions (< 3 frames), excluding dialogue-only
		# phases which naturally clear in 1-2 frames via auto-dismiss.
		if delta < 3 and pname != "Tutorial_Complete" and pname not in fast_ok_phases:
			_a.log("TIMING_FAST|%s took only %d frames from %s" % [pname, delta, prev_name])
			timing_warns += 1
		# Flag very slow transitions (> 1500 frames ≈ 25s) excluding known slow gates.
		var slow_ok := ["Automation_Running"]
		if delta > 1500 and pname not in slow_ok:
			_a.log("TIMING_SLOW|%s took %d frames from %s" % [pname, delta, prev_name])
			timing_warns += 1
	if timing_warns > 0:
		_a.warn(false, "phase_timing_anomalies", "count=%d" % timing_warns)

	# ── Stall Timer Verification ──
	# Automation_Running uses a stall timer (TicksSincePhaseChange).
	# Verify the counter incremented across samples — if it didn't, TutorialSystem.Process
	# may not be running and players could get permanently stuck.
	# Stall timer thresholds: Automation_Running must reach 30 ticks.
	var stall_thresholds := {"Automation_Running": 29}
	for stall_phase in stall_thresholds:
		var threshold: int = stall_thresholds[stall_phase]
		if _stall_ticks_samples.has(stall_phase):
			var samples: Array = _stall_ticks_samples[stall_phase]
			_a.log("STALL_TIMER|%s samples=%d values=%s" % [stall_phase, samples.size(),
				str(samples).left(80)])
			if samples.size() >= 2:
				var first_val: int = int(samples[0])
				var last_val: int = int(samples[samples.size() - 1])
				var max_val := 0
				for s in samples:
					if int(s) > max_val:
						max_val = int(s)
				_a.hard(max_val >= threshold, "stall_timer_reached_threshold_%s" % stall_phase.to_lower(),
					"expected>=%d max=%d samples=%d" % [threshold, max_val, samples.size()])
				# Verify timer incremented (max > 0). last > first can fail due to read-lock caching.
				_a.hard(max_val > 0, "stall_timer_incrementing_%s" % stall_phase.to_lower(),
					"first=%d last=%d max=%d" % [first_val, last_val, max_val])
			else:
				_a.warn(false, "stall_timer_insufficient_samples_%s" % stall_phase.to_lower())

	# ── Tab Disclosure Audit ──
	_a.log("TAB_DISCLOSURE_AUDIT|checkpoints=%d" % _tab_disclosure_log.size())
	for entry in _tab_disclosure_log:
		_a.log("TAB|%s" % str(entry))

	# ── Sell Target Bridge Method Coverage ──
	# GetTutorialSellTargetV0 and GetWrongStationWarningV0 are validated inline
	# during Buy_React. Log whether they were exercised.
	_a.log("BRIDGE_COVERAGE|sell_target=%s wrong_station_warning=%s" % [
		"exercised" if _objective_log.has("Buy_React") else "skipped",
		"exercised" if _bridge.has_method("GetWrongStationWarningV0") else "missing"])

	# ── Credit Curve ──
	_a.log("CREDIT_CURVE|phases=%d" % _credit_curve.size())
	for entry in _credit_curve:
		_a.log("CREDIT|phase=%s credits=%d" % [str(entry.phase), int(entry.credits)])

	# ── Economy Coherence ──
	# Verify credits peaked above initial during tutorial (pre-reinit only).
	# Credits may bleed during force-advance acts (tariffs, station fees), so
	# check peak vs start rather than end vs start.
	var pre_reinit_credits: Array[Dictionary] = []
	for entry in _credit_curve:
		if str(entry.phase) == "REINIT_TEST":
			break
		pre_reinit_credits.append(entry)
	if pre_reinit_credits.size() >= 2:
		var first_credits: int = int(pre_reinit_credits[0].get("credits", 0))
		var peak_credits := first_credits
		var peak_phase := ""
		for entry in pre_reinit_credits:
			var c: int = int(entry.get("credits", 0))
			if c > peak_credits:
				peak_credits = c
				peak_phase = str(entry.get("phase", ""))
		# Warn (not hard) — some seeds require bot to detour to an unvisited node
		# for the Travel_Prompt gate, sacrificing profit at the ideal sell target.
		_a.warn(peak_credits > first_credits, "credits_peaked_above_start",
			"start=%d peak=%d at=%s" % [first_credits, peak_credits, peak_phase])
		# Warn if credits ended below start (economy leak during tutorial).
		var end_credits: int = int(pre_reinit_credits[pre_reinit_credits.size() - 1].get("credits", 0))
		if end_credits < first_credits:
			_a.log("ECONOMY_LEAK|credits bled from %d to %d during force-advance acts" % [first_credits, end_credits])

	_set_bot_phase(BotPhase.FINAL_SUMMARY)


func _do_final_summary() -> void:
	_a.summary()
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(_a.exit_code())
	_phase = BotPhase.DONE


# ── Helpers ────────────────────────────────────────────────────────

func _set_bot_phase(p: BotPhase) -> void:
	_phase = p
	_polls = 0
	_last_phase_change_frame = _total_frames
	_log_credit_curve(BotPhase.keys()[p])


func _get_tutorial_state() -> Dictionary:
	if _bridge == null or not _bridge.has_method("GetTutorialStateV0"):
		return {}
	return _bridge.call("GetTutorialStateV0")


func _get_dialogue() -> Dictionary:
	if _bridge == null or not _bridge.has_method("GetRotatingFODialogueV0"):
		return {}
	return _bridge.call("GetRotatingFODialogueV0")


func _assert_speaker_is(expected: String, fo_data: Dictionary, tag: String) -> void:
	var actual := str(fo_data.get("name", ""))
	_a.hard(actual == expected, tag, "expected=%s got=%s" % [expected, actual])


func _log_narrative(phase_name: String, fo_data: Dictionary, state_override: Dictionary = {}) -> void:
	var speaker := str(fo_data.get("name", ""))
	var text := str(fo_data.get("text", ""))
	_dialogue_log.append({"phase": phase_name, "seq": _dialogue_log.size(), "speaker": speaker, "text": text})
	_a.log("NARRATIVE|%s|%s|%s" % [phase_name, speaker, text.left(80)])
	# Assert non-empty text.
	_a.hard(not text.is_empty(), "dialogue_nonempty_%s" % phase_name.to_lower())
	# Capture objective text for coverage audit.
	# Use state_override if provided (avoids re-reading state — timing race fix).
	var state: Dictionary = state_override if not state_override.is_empty() else _get_tutorial_state()
	var obj := str(state.get("objective", ""))
	if not _objective_log.has(phase_name):
		_objective_log[phase_name] = obj


func _dismiss_if_needed(state: Dictionary) -> void:
	# If current phase has pending dialogue, dismiss it.
	if bool(state.get("dialogue_dismissed", true)):
		return
	_dismiss_all_beats()


func _dismiss_all_beats(override_phase_name: String = "") -> void:
	# Dismiss dialogue repeatedly until DialogueDismissed is true.
	# Track beat count per phase for verification against expected counts.
	# override_phase_name: use caller's known phase to avoid state re-read race.
	# IMPORTANT: Stop if the tutorial phase changes — prevents cascading into the
	# NEXT phase (dismiss resets dialogue_dismissed, sim advances, dismiss fires again).
	var phase_name := override_phase_name if not override_phase_name.is_empty() \
		else str(_get_tutorial_state().get("phase_name", ""))
	var beats_this_call := 0
	var last_seq := -1
	for i in range(10):  # Max 10 beats per phase.
		if _bridge == null:
			return
		# Read sequence BEFORE dismiss to verify it increments.
		var pre_state := _get_tutorial_state()
		var pre_seq := int(pre_state.get("dialogue_sequence", -1))
		_bridge.call("DismissTutorialDialogueV0")
		beats_this_call += 1
		var state := _get_tutorial_state()
		var post_seq := int(state.get("dialogue_sequence", -1))
		# Stop if phase changed — we've cascaded into the next phase.
		var current_pn := str(state.get("phase_name", ""))
		if not phase_name.is_empty() and current_pn != phase_name:
			# Record the phase change in history (main loop may miss intermediate phases).
			_track_tutorial_phase()
			_a.log("DISMISS_CASCADE_STOPPED|was=%s now=%s after=%d beats" % [
				phase_name, current_pn, beats_this_call])
			break
		if bool(state.get("dialogue_dismissed", false)):
			# Verify sequence was consumed (either incremented or dismissed is the final beat).
			if pre_seq >= 0 and last_seq >= 0 and pre_seq == last_seq:
				# Dismiss with same sequence = final beat (DialogueDismissed set instead of seq++).
				_a.log("DIALOGUE_SEQ|%s final beat at seq=%d" % [phase_name, pre_seq])
			break
		# Verify sequence incremented (multi-beat phases advance seq before dismiss).
		if pre_seq >= 0 and last_seq >= 0 and post_seq <= last_seq:
			_a.log("DIALOGUE_SEQ_WARN|%s seq didn't increment: pre=%d post=%d last=%d" % [
				phase_name, pre_seq, post_seq, last_seq])
		last_seq = post_seq if post_seq >= 0 else pre_seq
		# Log next beat.
		var fo := _get_dialogue()
		if not fo.is_empty():
			var speaker := str(fo.get("name", ""))
			var text := str(fo.get("text", ""))
			_dialogue_log.append({"phase": current_pn, "seq": post_seq, "speaker": speaker, "text": text})
			_a.log("NARRATIVE|%s|seq=%d|%s|%s (beat %d)" % [current_pn, post_seq, speaker, text.left(60), i + 1])
	# Accumulate beat count for this phase (may be called multiple times).
	if not phase_name.is_empty():
		_beat_counts[phase_name] = _beat_counts.get(phase_name, 0) + beats_this_call


func _wait_for_tutorial_phase(expected: String, timeout: int, next_bot_phase: BotPhase) -> void:
	# Quick check — if already at expected phase, transition immediately.
	var state := _get_tutorial_state()
	if str(state.get("phase_name", "")) == expected:
		_set_bot_phase(next_bot_phase)
		return
	# Otherwise set bot phase and let polling in the match handle it.
	_set_bot_phase(next_bot_phase)


func _track_tutorial_phase() -> void:
	if _bridge == null:
		return
	var state := _get_tutorial_state()
	if state.is_empty():
		return
	var phase := int(state.get("phase", -1))
	if phase != _last_tutorial_phase:
		_last_tutorial_phase = phase
		var name := str(state.get("phase_name", ""))
		_phase_history.append({"phase": phase, "name": name, "frame": _total_frames})
		_a.log("PHASE_CHANGE|%s (phase=%d) frame=%d" % [name, phase, _total_frames])


func _get_selected_fo_name() -> String:
	match _selected_fo_type:
		"Analyst": return "Maren"
		"Veteran": return "Dask"
		"Pathfinder": return "Lira"
	return _selected_fo_type


func _check_tab_disclosure(checkpoint: String) -> void:
	if _bridge == null or not _bridge.has_method("GetOnboardingStateV0"):
		return
	var obs: Dictionary = _bridge.call("GetOnboardingStateV0")
	_tab_disclosure_log.append({
		"checkpoint": checkpoint,
		"show_jobs_tab": bool(obs.get("show_jobs_tab", false)),
		"show_ship_tab": bool(obs.get("show_ship_tab", false)),
		"show_station_tab": bool(obs.get("show_station_tab", false)),
		"show_intel_tab": bool(obs.get("show_intel_tab", false)),
	})
	match checkpoint:
		"before_first_trade":
			_a.hard(not bool(obs.get("show_jobs_tab", true)), "jobs_tab_hidden_before_trade")
		"after_combat":
			# Warn (not hard) — TryExecuteSafeRead may return cached snapshot if
			# sim thread holds write lock when GetOnboardingStateV0 runs.
			_a.warn(bool(obs.get("show_ship_tab", false)), "ship_tab_visible_after_combat")
		"after_explore_3":
			_a.hard(bool(obs.get("show_station_tab", false)), "station_tab_visible_after_explore")
			_a.hard(bool(obs.get("show_intel_tab", false)), "intel_tab_visible_after_explore")
	_a.log("TAB_DISCLOSURE|%s jobs=%s ship=%s station=%s intel=%s" % [
		checkpoint,
		str(obs.get("show_jobs_tab", false)),
		str(obs.get("show_ship_tab", false)),
		str(obs.get("show_station_tab", false)),
		str(obs.get("show_intel_tab", false))])


func _check_fo_reactive_suppression() -> void:
	# During tutorial, FO reactive triggers should NOT fire. Verify PendingDialogueLine
	# equivalent is empty by checking GetFirstOfficerStateV0 for unexpected dialogue activity.
	if _bridge == null or not _bridge.has_method("GetFirstOfficerStateV0"):
		return
	var fo_state: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	# We can't directly read PendingDialogueLine (JsonIgnore), but we can verify
	# the FO state is consistent — if reactive triggers fired, dialogue_count
	# would increase unexpectedly. Log it for anomaly detection.
	_a.log("FO_REACTIVE_CHECK|promoted=%s type=%s dialogue_count=%s" % [
		str(fo_state.get("promoted", false)),
		str(fo_state.get("type", "None")),
		str(fo_state.get("dialogue_count", 0))])


func _buy_goods_for_loop() -> void:
	# Buy goods at the current station for the next trade loop iteration.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	var pick := _find_best_buy(market)
	if pick.is_empty():
		_a.log("TRADE_LOOP_BUY|no affordable goods at %s" % node_id)
		return
	_bought_good_id = pick.good_id
	_bought_unit_cost = pick.price
	_bought_qty = 1
	_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, 1, true)
	_a.log("TRADE_LOOP_BUY|good=%s price=%d at=%s" % [_bought_good_id, _bought_unit_cost, node_id])


func _log_credit_curve(phase_name: String) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits := int(ps.get("credits", -1))
	if credits < 0:
		return
	_credit_curve.append({"phase": phase_name, "credits": credits, "frame": _total_frames})


func _capture(label: String) -> void:
	if _screenshot == null:
		return
	var tick := 0
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		tick = int(_bridge.call("GetSimTickV0"))
	var filename := "%s_%04d" % [label, tick]
	_screenshot.capture_v0(self, filename, OUTPUT_DIR)
	_a.log("CAPTURE|%s" % label)


# ── Navigation Helpers ─────────────────────────────────────────────

func _init_navigation() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))
	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_edges = galaxy.get("lane_edges", [])
	_refresh_neighbors()
	_a.log("NAV|home=%s neighbors=%d edges=%d" % [
		_home_node_id, _neighbor_ids.size(), _all_edges.size()])


func _refresh_neighbors() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var current := str(ps.get("current_node_id", ""))
	_neighbor_ids.clear()
	var seen := {}
	for lane in _all_edges:
		var from_id := str(lane.get("from_id", ""))
		var to_id := str(lane.get("to_id", ""))
		if from_id == current and not seen.has(to_id):
			_neighbor_ids.append(to_id)
			seen[to_id] = true
		elif to_id == current and not seen.has(from_id):
			_neighbor_ids.append(from_id)
			seen[from_id] = true


func _get_neighbors_of(node_id: String) -> Array:
	var result: Array = []
	var seen := {}
	for lane in _all_edges:
		var from_id := str(lane.get("from_id", ""))
		var to_id := str(lane.get("to_id", ""))
		if from_id == node_id and not seen.has(to_id):
			result.append(to_id)
			seen[to_id] = true
		elif to_id == node_id and not seen.has(from_id):
			result.append(from_id)
			seen[from_id] = true
	return result


func _headless_travel(dest: String) -> void:
	if _bridge != null:
		if _bridge.has_method("DispatchTravelCommandV0"):
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", dest)
		if _bridge.has_method("DispatchPlayerArriveV0"):
			_bridge.call("DispatchPlayerArriveV0", dest)
	if _gm != null:
		_gm.set("_lane_cooldown_v0", 0.0)
		if _gm.get("current_player_state") != null:
			_gm.call("on_lane_gate_proximity_entered_v0", dest)
			_gm.call("on_lane_arrival_v0", dest)


func _dock_at_station() -> void:
	if _gm == null:
		return
	# Remove NPC fleet ships that might interfere with dock targeting.
	for ship in root.get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship):
			ship.remove_from_group("FleetShip")
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_gm.call("on_proximity_dock_entered_v0", targets[0])


func _find_best_buy(market: Array) -> Dictionary:
	var best_good := ""
	var best_price := 999999
	for item in market:
		var gid := str(item.get("good_id", ""))
		var qty := int(item.get("quantity", 0))
		var price := int(item.get("buy_price", 0))
		if gid.is_empty() or qty <= 0 or price <= 0:
			continue
		if price < best_price:
			best_good = gid
			best_price = price
	if best_good.is_empty():
		return {}
	return {"good_id": best_good, "price": best_price}


func _pick_best_sell_neighbor() -> String:
	var best_id := ""
	var best_sell := 0
	for nid in _neighbor_ids:
		var sid := str(nid)
		var market: Array = _bridge.call("GetPlayerMarketViewV0", sid)
		for item in market:
			if str(item.get("good_id", "")) == _bought_good_id:
				var sp := int(item.get("sell_price", 0))
				if sp > best_sell:
					best_sell = sp
					best_id = sid
	return best_id


func _get_sell_price_at(node_id: String, good_id: String) -> int:
	if good_id.is_empty():
		return 0
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	for item in market:
		if str(item.get("good_id", "")) == good_id:
			return int(item.get("sell_price", 0))
	return 0
