# scripts/tests/test_fh_experience_v0.gd
# First-Hour Experience Bot — plays the game autonomously for ~60 game-minutes
# using the playthrough bot's decision engine, then scores 92 metrics across
# 12 dimensions: economy, pacing, grind, flow, combat, exploration, disclosure,
# FO engagement, decision quality, robustness, story/tension, and visual quality.
#
# Usage:
#   powershell -File scripts/tools/Run-ExperienceBot.ps1 -Mode headless
#   powershell -File scripts/tools/Run-ExperienceBot.ps1 -Mode visual -Seed 42
#
# Direct usage:
#   godot --headless --path . -s res://scripts/tests/test_fh_experience_v0.gd -- --seed=42
#   godot --path . -s res://scripts/tests/test_fh_experience_v0.gd -- --seed=42 --archetype=explorer
#
# Archetypes: balanced (default), trader, explorer, fighter
# Output prefix: EXP| for structured log parsing.
extends SceneTree

const PREFIX := "EXP"
const MAX_POLLS := 600
const MAX_BUY_QTY := 10
const TICK_ADVANCE := 5
const SAMPLE_INTERVAL := 10  # sample telemetry every N decisions
const MAX_DECISIONS := 720   # ~3600 ticks = ~60 game-minutes

# Archetype-tunable constants (set in _apply_archetype)
var EXPLORE_EVERY_N := 4
var COMBAT_COOLDOWN := 5
var COMBAT_ENABLED := true

# Settle timings (frames at ~60fps)
const SETTLE_SCENE := 60
const SETTLE_ACTION := 20

const AssertScript = preload("res://scripts/tools/bot_assert.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase { LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY, PLAY_LOOP, VISUAL_TOUR, REPORT, DONE }

# Core state
var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _game_manager = null
var _screenshot = null
var _a  # bot_assert instance

# CLI params
var _user_seed := -1
var _archetype := "balanced"

# Graph
var _adj: Dictionary = {}
var _all_nodes: Array = []

# Decision engine state (forked from playthrough_bot_v0)
var _decision := 0
var _exploration_target := ""
var _trades_since_explore := 0
var _consecutive_idles := 0
var _max_consecutive_idles := 0
var _combat_cooldown_remaining := 0
var _combat_hp_init_done := false
var _visited: Dictionary = {}
var _start_credits := 0
var _home_node_id := ""

# ---- Tracking: trade ----
var _total_buys := 0
var _total_sells := 0
var _total_travels := 0
var _total_idles := 0
var _total_spent := 0
var _total_earned := 0
var _goods_bought: Dictionary = {}
var _goods_sold: Dictionary = {}
var _good_trade_count: Dictionary = {}
var _actions: Array = []

# ---- Tracking: combat ----
var _total_combats := 0
var _total_kills := 0
var _combat_hull_mins: Array = []  # hull% after each combat

# ---- Tracking: economy ----
var _price_history: Dictionary = {}  # good_id -> [prices]
var _credit_trajectory: Array = []   # credit value per sample
var _last_credit_value := -1
var _consecutive_credit_unchanged := 0
var _credit_direction_changes := 0
var _last_credit_delta := 0

# ---- Tracking: pacing ----
var _reward_events: Array = []         # decision indices of reward events
var _action_type_history: Array = []   # "BUY","SELL","TRAVEL","COMBAT","EXPLORE","IDLE"
var _systems_introduced: Array = []    # first encounter of each game system
var _system_intro_decisions: Array = []  # decision index of each intro
var _milestone_log: Array = []         # [{decision, name, detail}]

# ---- Tracking: FO ----
var _fo_promoted := false
var _fo_dialogue_count := 0
var _fo_dialogue_decisions: Array = []  # decisions where FO spoke
var _fo_last_count := 0

# ---- Tracking: exploration ----
var _factions_visited: Dictionary = {}
var _faction_map: Dictionary = {}  # node_id -> faction_id
var _backtrack_count := 0
var _total_travel_count := 0
var _discoveries_found := 0
var _fragments_found := 0
var _knowledge_entries := 0

# ---- Tracking: event decision timing ----
var _event_decisions: Dictionary = {}  # key -> decision when event FIRST happened

# ---- Tracking: progressive disclosure ----
var _dock_tab_counts: Array = []  # tab count at each dock

# ---- Tracking: progression (NEW DIM 8) ----
var _milestones_unlocked := 0
var _endgame_paths_revealed := 0
var _stats_snapshot: Dictionary = {}    # end-of-run GetPlayerStatsV0
var _profit_per_trade: Array = []       # per-trade profit samples

# ---- Tracking: market intelligence (NEW DIM 9) ----
var _supply_shocks_seen := 0
var _market_alerts_seen := 0
var _price_spreads: Array = []          # per-node best spread at sample time
var _economy_overview_count := 0        # entries from GetEconomyOverviewV0

# ---- Tracking: missions (NEW DIM 10) ----
var _missions_available := 0
var _missions_accepted := 0
var _bounties_available := 0
var _commissions_active := 0
var _mission_first_seen := -1           # decision when first mission appeared

# ---- Tracking: fleet & loadout (NEW DIM 11) ----
var _modules_installed := 0
var _modules_empty := 0
var _techs_unlocked := 0
var _techs_total := 0
var _refit_attempted := false
var _loadout_snapshot = null             # end-of-run GetHeroShipLoadoutV0

# ---- Tracking: security & tension (NEW DIM 12) ----
var _security_samples: Array = []       # [{node_id, band, war_intensity}]
var _threat_bands_seen: Dictionary = {} # band_name -> count
var _max_war_intensity := 0.0
var _lane_ambush_count := 0             # estimated from lane security checks

# ---- Tracking: haven (NEW DIM 13) ----
var _haven_discovered := false
var _construction_projects := 0
var _haven_residents := 0

# ---- Tracking: narrative depth (NEW DIM 14) ----
var _revelation_stage := 0
var _data_logs_found := 0
var _narrative_npcs_met := 0
var _communion_rep := 0.0
var _fo_competence_tier := 0

# ---- Tracking: economy depth (existing dim enhancement) ----
var _trades_profitable := 0
var _trades_unprofitable := 0
var _best_single_profit := 0
var _worst_single_loss := 0

# ---- Tracking: combat depth (existing dim enhancement) ----
var _heat_max := 0.0
var _doctrine_set := false
var _weapons_equipped := 0
var _drones_active := 0

# ---- Tracking: exploration depth (existing dim enhancement) ----
var _scan_charges_used := 0
var _scan_charges_max := 0
var _fracture_accessible := false
var _planet_scans := 0
var _discovery_phases: Dictionary = {}  # phase -> count

# ---- Tracking: visual (visual mode only) ----
var _capture_points: Dictionary = {}  # milestone -> true (avoid duplicates)
var _is_visual := false               # true when NOT headless (real rendering)

# ---- Tracking: feel presence (runtime VFX/feedback verification) ----
# These flags track whether visual feedback systems ACTUALLY rendered during play.
# Eliminates false positives from LLM evals that can't see runtime state.
var _feel_credits_flash_seen := false   # Credits label flashed green/red on trade
var _feel_damage_flash_seen := false    # Red overlay appeared on taking damage
var _feel_combat_vignette_seen := false # Combat vignette edge glow activated
var _feel_combat_banner_seen := false   # "[ENGAGED]" banner appeared
var _feel_toast_seen := false           # Toast notification appeared
var _feel_galaxy_marker_seen := false   # Player position marker exists on galaxy map
var _feel_shake_intensity_max := 0.0    # Peak screen shake intensity observed
var _feel_checks_run := 0               # Total inline feel checks executed

# ---- Visual tour state machine (VISUAL_TOUR phase) ----
var _tour_step := 0
var _tour_settle := 0                  # frames to wait for UI to settle
const TOUR_SETTLE_FRAMES := 15        # ~250ms at 60fps for panel animation

# ---- Tracking: system engagement (bot actively uses game systems, not just observes) ----
var _missions_attempted := 0        # times bot tried AcceptMissionV0
var _missions_accepted_by_bot := 0  # successful accepts
var _modules_attempted := 0         # times bot tried InstallModuleV0
var _modules_installed_by_bot := 0  # successful installs
var _doctrine_attempted := false    # did bot try to set doctrine
var _doctrine_set_by_bot := false   # did it succeed
var _research_attempted := false    # did bot try to start research
var _research_started_by_bot := false # did it succeed
var _engagement_cooldown := 0       # prevent spamming engagement every decision
var _discoveries_scanned := 0       # discoveries the bot scanned
var _fragments_collected := 0       # adaptation fragments collected
var _automation_attempted := false   # did bot try to create automation
var _automation_created := false     # did it succeed

# ---- Flags ----
var _flags: Array = []

# ---- Telemetry time-series ----
var _samples: Array = []  # [{decision, tick, credits, hull_pct, node_id, action, cargo}]


func _initialize() -> void:
	_a = AssertScript.new(PREFIX)
	_screenshot = ScreenshotScript.new()
	_parse_args()
	_apply_archetype()
	_is_visual = DisplayServer.get_name() != "headless"
	_a.log("START|experience_bot_v0 archetype=%s seed=%d visual=%s" % [_archetype, _user_seed, str(_is_visual)])


func _parse_args() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_user_seed = int(arg.trim_prefix("--seed="))
		if arg.begins_with("--archetype="):
			var a := arg.trim_prefix("--archetype=").to_lower()
			if a in ["balanced", "trader", "explorer", "fighter"]:
				_archetype = a


func _apply_archetype() -> void:
	match _archetype:
		"trader":
			EXPLORE_EVERY_N = 8
			COMBAT_COOLDOWN = 10
			COMBAT_ENABLED = false
		"explorer":
			EXPLORE_EVERY_N = 2
			COMBAT_COOLDOWN = 5
			COMBAT_ENABLED = true
		"fighter":
			EXPLORE_EVERY_N = 6
			COMBAT_COOLDOWN = 2
			COMBAT_ENABLED = true
		_:  # balanced
			EXPLORE_EVERY_N = 4
			COMBAT_COOLDOWN = 5
			COMBAT_ENABLED = true


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames > 12000 and _phase != Phase.DONE:  # ~200s safety
		_a.log("TIMEOUT|frame=%d" % _total_frames)
		_phase = Phase.REPORT
	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(SETTLE_SCENE, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.PLAY_LOOP: _do_play_loop()
		Phase.VISUAL_TOUR: _do_visual_tour()
		Phase.REPORT: _do_report()
		Phase.DONE: return true
	return false


# ===================== Setup =====================

func _do_load_scene() -> void:
	if _user_seed >= 0:
		seed(_user_seed)
	# Delete stale quicksave
	var save_path := "user://quicksave.json"
	if FileAccess.file_exists(save_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(save_path))
		_a.log("CLEANUP|deleted_quicksave")
	var scene = load("res://scenes/playable_prototype.tscn").instantiate()
	root.add_child(scene)
	_a.log("SCENE_LOADED")
	_polls = 0
	_phase = Phase.WAIT_SCENE


func _do_wait(settle: int, next: Phase) -> void:
	_polls += 1
	if _polls >= settle:
		_polls = 0
		_phase = next


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge != null:
		_polls = 0
		_phase = Phase.WAIT_READY
	else:
		_polls += 1
		if _polls > MAX_POLLS:
			_a.hard(false, "bridge_found")
			_phase = Phase.REPORT


func _do_wait_ready() -> void:
	var ready := false
	if _bridge.has_method("GetBridgeReadyV0"):
		ready = bool(_bridge.call("GetBridgeReadyV0"))
	else:
		ready = true
	if ready:
		_game_manager = root.get_node_or_null("GameManager")
		if _game_manager:
			_game_manager.set("_on_main_menu", false)
		# Skip tutorial -- tutorial bot tests this separately
		if _bridge.has_method("SkipTutorialV0"):
			_bridge.call("SkipTutorialV0")
			_a.log("TUTORIAL|skipped")
		# Init combat HP
		if _bridge.has_method("InitFleetCombatHpV0"):
			_bridge.call("InitFleetCombatHpV0")
			_combat_hp_init_done = true
		# Auto-promote FO
		_promote_fo()
		# Build graph (retry on read-lock contention — seed 1001 race condition)
		_build_adjacency()
		for _retry in range(5):
			if _all_nodes.size() > 0:
				break
			print(PREFIX + "RETRY|_build_adjacency attempt %d (nodes=0)" % (_retry + 1))
			_busy = true
			await create_timer(0.3).timeout
			_busy = false
			_build_adjacency()
		if _all_nodes.size() == 0:
			print(PREFIX + "FLAG|GALAXY_EMPTY|could not read galaxy after retries")
		# Init state
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		_start_credits = int(ps.get("credits", 0))
		_home_node_id = str(ps.get("current_node_id", ""))
		_visited[_home_node_id] = true
		_credit_trajectory.append(_start_credits)
		_last_credit_value = _start_credits
		# Build faction map
		_build_faction_map()
		_track_faction(_home_node_id)
		# Log init
		_a.log("INIT|credits=%d node=%s nodes=%d archetype=%s" % [
			_start_credits, _home_node_id, _all_nodes.size(), _archetype])
		_log_milestone("INIT", "credits=%d" % _start_credits)
		# First capture
		_try_capture("01_boot")
		_polls = 0
		_phase = Phase.PLAY_LOOP
	else:
		_polls += 1
		if _polls > MAX_POLLS:
			_a.hard(false, "bridge_ready")
			_phase = Phase.REPORT


# ===================== Main Play Loop =====================

func _do_play_loop() -> void:
	if _decision >= MAX_DECISIONS:
		if _is_visual:
			_phase = Phase.VISUAL_TOUR
			_tour_step = 0
			_tour_settle = 0
		else:
			_phase = Phase.REPORT
		return

	_decision += 1

	# ---- Telemetry sampling ----
	if _decision % SAMPLE_INTERVAL == 0:
		_sample_telemetry()
		_probe_live_systems()

	# ---- FO tracking ----
	_track_fo()

	# ---- Core decision ----
	_bot_decide_and_act()

	# ---- Advance sim time ----
	if _bridge.has_method("DebugAdvanceTicksV0"):
		_bridge.call("DebugAdvanceTicksV0", TICK_ADVANCE)

	# ---- Visual mode: keep player alive (NPCs attack via physics engine) ----
	if _is_visual and _decision % 5 == 0:
		_visual_hull_guard()

	# ---- Credit tracking ----
	_track_credits()

	# ---- Milestone captures (visual mode) ----
	_check_milestones()


func _sample_telemetry() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits := int(ps.get("credits", 0))
	var cargo := int(ps.get("cargo_count", 0))
	var node_id := str(ps.get("current_node_id", ""))
	var hull_pct := 100
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull := int(hp.get("hull", 100))
		var hull_max := int(hp.get("hull_max", 100))
		if hull_max > 0:
			hull_pct = (hull * 100) / hull_max
	var tick := 0
	if _bridge.has_method("GetTickV0"):
		tick = int(_bridge.call("GetTickV0"))
	var last_action: String = str(_action_type_history[-1]) if _action_type_history.size() > 0 else ""
	_samples.append({
		"decision": _decision, "tick": tick, "credits": credits,
		"hull_pct": hull_pct, "node_id": node_id, "action": last_action,
		"cargo": cargo, "fo_lines": _fo_dialogue_count
	})
	_credit_trajectory.append(credits)


func _probe_live_systems() -> void:
	# Runs every SAMPLE_INTERVAL decisions — polls bridge for live system state
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))

	# -- Missions --
	if _bridge.has_method("GetMissionListV0"):
		var missions = _bridge.call("GetMissionListV0")
		if missions is Array:
			var avail := 0
			var active := 0
			for m in missions:
				if m is Dictionary:
					var st: String = str(m.get("status", ""))
					if st == "available":
						avail += 1
					elif st == "active":
						active += 1
			if avail > _missions_available:
				_missions_available = avail
			if active > _missions_accepted:
				_missions_accepted = active
			if avail > 0 and _mission_first_seen < 0:
				_mission_first_seen = _decision
	if _bridge.has_method("GetAvailableBountiesV0"):
		var bounties = _bridge.call("GetAvailableBountiesV0")
		if bounties is Array and bounties.size() > _bounties_available:
			_bounties_available = bounties.size()
	if _bridge.has_method("GetActiveCommissionV0"):
		var comm = _bridge.call("GetActiveCommissionV0")
		if comm is Dictionary and not str(comm.get("id", "")).is_empty():
			_commissions_active += 1

	# -- Security/tension --
	if not loc.is_empty() and _bridge.has_method("GetNodeSecurityV0"):
		var sec = _bridge.call("GetNodeSecurityV0", loc)
		if sec is Dictionary:
			var band: String = str(sec.get("band", "unknown"))
			_threat_bands_seen[band] = _threat_bands_seen.get(band, 0) + 1
			var war_i: float = float(sec.get("war_intensity", 0.0))
			if war_i > _max_war_intensity:
				_max_war_intensity = war_i
			_security_samples.append({"node_id": loc, "band": band, "war_intensity": war_i})

	# -- Haven --
	if _bridge.has_method("GetHavenStatusV0"):
		var haven = _bridge.call("GetHavenStatusV0")
		if haven is Dictionary:
			if bool(haven.get("discovered", false)):
				_haven_discovered = true

	# -- Combat depth (GetHeatSnapshotV0 takes no params) --
	if _bridge.has_method("GetHeatSnapshotV0"):
		var hs = _bridge.call("GetHeatSnapshotV0")
		if hs is Dictionary:
			var h: float = float(hs.get("current_heat", 0.0))
			if h > _heat_max:
				_heat_max = h
	if _bridge.has_method("GetDoctrineStatusV0"):
		var doc = _bridge.call("GetDoctrineStatusV0", "fleet_trader_1")
		if doc is Dictionary and not str(doc.get("doctrine", "")).is_empty():
			_doctrine_set = true

	# -- Scan charges --
	if _bridge.has_method("GetScanChargesV0"):
		var sc = _bridge.call("GetScanChargesV0")
		if sc is Dictionary:
			var used := int(sc.get("used", 0))
			var mx := int(sc.get("max", 0))
			if used > _scan_charges_used:
				_scan_charges_used = used
			if mx > _scan_charges_max:
				_scan_charges_max = mx

	# -- Narrative --
	if _bridge.has_method("GetNarrativeNpcsAtNodeV0") and not loc.is_empty():
		var npcs = _bridge.call("GetNarrativeNpcsAtNodeV0", loc)
		if npcs is Array:
			_narrative_npcs_met += npcs.size()

	# -- Market intelligence (GetMarketAlertsV0 may not exist yet) --
	if _bridge.has_method("GetSupplyShockSummaryV0"):
		var shocks = _bridge.call("GetSupplyShockSummaryV0")
		if shocks is Array:
			_supply_shocks_seen = maxi(_supply_shocks_seen, shocks.size())


func _track_credits() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits := int(ps.get("credits", 0))
	var delta := credits - _last_credit_value
	if delta != 0 and _last_credit_delta != 0:
		if (delta > 0) != (_last_credit_delta > 0):
			_credit_direction_changes += 1
	if delta != 0:
		_last_credit_delta = delta
	if credits == _last_credit_value:
		_consecutive_credit_unchanged += 1
	else:
		_consecutive_credit_unchanged = 0
	_last_credit_value = credits


func _track_fo() -> void:
	if not _bridge.has_method("GetFirstOfficerStateV0"):
		return
	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	var lines := int(fo.get("dialogue_count", 0))
	if lines > _fo_last_count:
		_fo_dialogue_count = lines
		_fo_dialogue_decisions.append(_decision)
		_fo_last_count = lines
		if not _event_decisions.has("first_fo_line"):
			_event_decisions["first_fo_line"] = _decision


func _check_milestones() -> void:
	# Screenshot at key moments (visual mode only, one per milestone)
	if _total_buys == 1 and not _capture_points.has("first_buy"):
		_capture_points["first_buy"] = true
		_try_capture("02_first_buy")
		_log_milestone("FIRST_BUY", "decision=%d" % _decision)
		_introduce_system("market")
		if not _event_decisions.has("first_buy"):
			_event_decisions["first_buy"] = _decision
	if _total_sells == 1 and not _capture_points.has("first_sell"):
		_capture_points["first_sell"] = true
		_try_capture("03_first_sell")
		_log_milestone("FIRST_SELL", "decision=%d" % _decision)
		_introduce_system("trade")
		_record_reward("FIRST_PROFIT")
		if not _event_decisions.has("first_sell"):
			_event_decisions["first_sell"] = _decision
	if _total_combats == 1 and not _capture_points.has("first_combat"):
		_capture_points["first_combat"] = true
		_try_capture("04_first_combat")
		_log_milestone("FIRST_COMBAT", "decision=%d" % _decision)
		_introduce_system("combat")
		_record_reward("COMBAT_WIN")
		if not _event_decisions.has("first_combat"):
			_event_decisions["first_combat"] = _decision
	if _visited.size() == 3 and not _capture_points.has("three_nodes"):
		_capture_points["three_nodes"] = true
		_try_capture("05_three_nodes")
		_log_milestone("THREE_NODES", "decision=%d" % _decision)
	if _visited.size() == 5 and not _capture_points.has("five_nodes"):
		_capture_points["five_nodes"] = true
		_try_capture("06_five_nodes")
	if _factions_visited.size() == 2 and not _capture_points.has("two_factions"):
		_capture_points["two_factions"] = true
		_try_capture("07_two_factions")
		_log_milestone("TWO_FACTIONS", "decision=%d" % _decision)
	if _total_buys + _total_sells >= 10 and not _capture_points.has("ten_trades"):
		_capture_points["ten_trades"] = true
		_try_capture("08_ten_trades")
	# Exploration milestones (more nodes)
	if _visited.size() == 10 and not _capture_points.has("ten_nodes"):
		_capture_points["ten_nodes"] = true
		_try_capture("11_ten_nodes")
	if _visited.size() == _all_nodes.size() and not _capture_points.has("all_nodes"):
		_capture_points["all_nodes"] = true
		_try_capture("12_all_nodes")
		_log_milestone("ALL_NODES", "visited=%d" % _visited.size())
	# Economy milestones
	if _total_buys + _total_sells >= 30 and not _capture_points.has("thirty_trades"):
		_capture_points["thirty_trades"] = true
		_try_capture("13_thirty_trades")
	# Mid-run and end-run captures
	if _decision == MAX_DECISIONS / 2 and not _capture_points.has("midpoint"):
		_capture_points["midpoint"] = true
		_try_capture("09_midpoint")
	if _decision >= MAX_DECISIONS - 1 and not _capture_points.has("final"):
		_capture_points["final"] = true
		_try_capture("10_final")


# ===================== Decision Engine (forked from playthrough_bot_v0) =====================

func _bot_decide_and_act() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	var credits: int = int(ps.get("credits", 0))
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var cargo_count: int = int(ps.get("cargo_count", 0))

	if _combat_cooldown_remaining > 0:
		_combat_cooldown_remaining -= 1

	if loc.is_empty():
		_record_action(_decision, "IDLE", loc, "", 0, "no location")
		_action_type_history.append("IDLE")
		_total_idles += 1
		return

	# Phase 0: Committed exploration
	if not _exploration_target.is_empty():
		if loc == _exploration_target or _visited.has(_exploration_target):
			_exploration_target = ""
		else:
			var hop := _bot_get_next_hop(loc, _exploration_target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "committed exploration toward %s" % _exploration_target)
				_action_type_history.append("EXPLORE")
				return
			_exploration_target = ""

	# Phase 0b: Exploration pressure
	var has_unvisited := false
	for nid in _adj.keys():
		if not _visited.has(nid):
			has_unvisited = true
			break
	if has_unvisited and _trades_since_explore >= EXPLORE_EVERY_N and cargo_count == 0:
		var target := _bot_find_nearest_unvisited(loc)
		if not target.is_empty():
			_exploration_target = target
			_trades_since_explore = 0
			var hop := _bot_get_next_hop(loc, target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "exploration pressure toward %s" % target)
				_action_type_history.append("EXPLORE")
				return

	# Phase 1: Combat
	if COMBAT_ENABLED and _combat_cooldown_remaining <= 0:
		if _bot_try_combat(loc):
			_action_type_history.append("COMBAT")
			return

	# Phase 1b: Engage ancillary systems (missions, modules, doctrine, research)
	# Non-blocking: tries once per cooldown, doesn't consume the decision
	_bot_try_engage_systems(loc)

	# Phase 2: If carrying cargo -> sell
	if cargo_count > 0 and cargo.size() > 0:
		if _bot_try_sell(loc, cargo, credits):
			return  # action_type logged inside

	# Phase 3: No cargo -> buy
	if cargo_count == 0:
		if _bot_try_buy(loc, credits):
			return  # action_type logged inside

	# Phase 4: Explore unvisited
	if has_unvisited:
		var target := _bot_find_nearest_unvisited(loc)
		if not target.is_empty():
			var hop := _bot_get_next_hop(loc, target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "fallback explore toward %s" % target)
				_action_type_history.append("EXPLORE")
				return

	# Phase 5: Force buy untouched goods
	if cargo_count == 0:
		var untouched := _bot_find_untouched_good(loc, credits)
		if not untouched.node.is_empty():
			if untouched.node == loc:
				_bot_do_buy(loc, untouched.good, 1, credits)
				_action_type_history.append("BUY")
				return
			else:
				var hop := _bot_get_next_hop(loc, untouched.node)
				if not hop.is_empty():
					_bot_do_travel(loc, hop, "hunting untouched good %s" % untouched.good)
					_action_type_history.append("TRAVEL")
					return

	# Phase 6: Roam to least-visited
	var least := _bot_find_least_visited(loc)
	if not least.is_empty() and least != loc:
		var hop := _bot_get_next_hop(loc, least)
		if not hop.is_empty():
			_bot_do_travel(loc, hop, "idle roam toward %s" % least)
			_action_type_history.append("TRAVEL")
			return

	# Phase 7: Idle
	_consecutive_idles += 1
	_total_idles += 1
	if _consecutive_idles > _max_consecutive_idles:
		_max_consecutive_idles = _consecutive_idles
	_record_action(_decision, "IDLE", loc, "", 0, "nothing to do")
	_action_type_history.append("IDLE")


# ---- Combat ----

func _bot_try_combat(loc: String) -> bool:
	if not _bridge.has_method("GetFleetTransitFactsV0"):
		return false
	var fleets: Array = _bridge.call("GetFleetTransitFactsV0", loc)
	var hostile_id := ""
	for f in fleets:
		if bool(f.get("is_hostile", false)):
			hostile_id = str(f.get("fleet_id", ""))
			break
	if hostile_id.is_empty():
		return false
	if not _combat_hp_init_done and _bridge.has_method("InitFleetCombatHpV0"):
		_bridge.call("InitFleetCombatHpV0")
		_combat_hp_init_done = true
	# Spin up battle stations before combat — StandDown (25% dmg) can't kill NPCs in 50 rounds.
	if _bridge.has_method("GetBattleStationsStateV0"):
		var bs: Dictionary = _bridge.call("GetBattleStationsStateV0")
		if str(bs.get("state", "")) == "StandDown" and _bridge.has_method("ToggleBattleStationsV0"):
			_bridge.call("ToggleBattleStationsV0")

	var result: Dictionary = _bridge.call("ResolveCombatV0", "fleet_trader_1", hostile_id)
	_total_combats += 1
	_combat_cooldown_remaining = COMBAT_COOLDOWN
	# Sample feel state during combat — vignette, damage flash, banner should be active.
	_sample_feel_state()
	_try_capture("combat_active_%d" % _total_combats)
	var outcome := str(result.get("outcome", "unknown"))
	var attacker_hull := int(result.get("attacker_hull", 0))
	var hull_max := int(result.get("attacker_hull_max", 100))
	var salvage := int(result.get("salvage", 0))
	var hull_pct := (attacker_hull * 100) / maxi(hull_max, 1)
	_combat_hull_mins.append(hull_pct)
	_record_action(_decision, "COMBAT", loc, hostile_id, 0,
		"%s hull=%d%% salvage=%d" % [outcome, hull_pct, salvage])
	_a.log("COMBAT|d=%d target=%s outcome=%s hull=%d%% salvage=%d" % [
		_decision, hostile_id, outcome, hull_pct, salvage])
	if outcome == "Victory":
		_total_kills += 1
		if salvage > 0:
			_record_reward("COMBAT_SALVAGE")
	# Prevent player death in visual mode (game over screen blocks all UI)
	# We still record the hull damage for metrics, but repair immediately after
	if hull_pct <= 0 and _bridge.has_method("ForceRepairPlayerHullV0"):
		_bridge.call("ForceRepairPlayerHullV0")
	_consecutive_idles = 0
	return true


# ---- Trade: sell ----

func _bot_try_sell(loc: String, cargo: Array, _credits: int) -> bool:
	var best_good := ""
	var best_sell_node := ""
	var best_sell_price := 0
	var global_sell: Dictionary = {}
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			var sp := int(entry.get("sell_price", 0))
			if gid.is_empty() or sp <= 0:
				continue
			if not global_sell.has(gid) or sp > int(global_sell[gid].get("sell_price", 0)):
				global_sell[gid] = {"node_id": nid, "sell_price": sp}
	for item in cargo:
		var gid := str(item.get("good_id", ""))
		var qty := int(item.get("qty", 0))
		if gid.is_empty() or qty <= 0:
			continue
		if global_sell.has(gid):
			var info: Dictionary = global_sell[gid]
			var sp := int(info.get("sell_price", 0))
			if sp > best_sell_price:
				best_sell_price = sp
				best_sell_node = str(info.get("node_id", ""))
				best_good = gid
	if best_sell_node.is_empty():
		return false
	if best_sell_node == loc:
		var qty := 0
		for item in cargo:
			if str(item.get("good_id", "")) == best_good:
				qty = int(item.get("qty", 0))
				break
		if qty > 0:
			_bot_do_sell(loc, best_good, qty)
			_action_type_history.append("SELL")
			return true
	else:
		var hop := _bot_get_next_hop(loc, best_sell_node)
		if not hop.is_empty():
			_bot_do_travel(loc, hop, "toward sell %s at %s" % [best_good, best_sell_node])
			_action_type_history.append("TRAVEL")
			return true
	return false


# ---- Trade: buy ----

func _bot_try_buy(loc: String, credits: int) -> bool:
	var best_node := ""
	var best_good := ""
	var best_profit := 0
	var best_buy_price := 0
	# Build global sell prices
	var best_sell_prices: Dictionary = {}
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			var sp := int(entry.get("sell_price", 0))
			if gid.is_empty() or sp <= 0:
				continue
			if not best_sell_prices.has(gid) or sp > int(best_sell_prices[gid].get("price", 0)):
				best_sell_prices[gid] = {"price": sp, "node_id": nid}
	# Find best buy opportunity
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			var bp := int(entry.get("buy_price", 0))
			var available := int(entry.get("quantity", 0))
			if gid.is_empty() or bp <= 0 or available <= 0 or bp > credits:
				continue
			if not best_sell_prices.has(gid):
				continue
			var sell_info: Dictionary = best_sell_prices[gid]
			var sell_node := str(sell_info.get("node_id", ""))
			var profit := int(sell_info.get("price", 0)) - bp
			if sell_node == nid:
				continue
			# Penalize frequently-traded goods to force rotation
			var effective := profit
			var tc: int = _good_trade_count.get(gid, 0)
			for _k in range(tc):
				effective = effective / 2
			if effective > best_profit:
				best_profit = effective
				best_node = nid
				best_good = gid
				best_buy_price = bp
	if best_node.is_empty() or best_profit <= 0:
		return false
	if best_node == loc:
		var mv2: Array = _bridge.call("GetPlayerMarketViewV0", loc)
		var available := 0
		for entry in mv2:
			if str(entry.get("good_id", "")) == best_good:
				available = int(entry.get("quantity", 0))
				break
		var affordable := credits / best_buy_price if best_buy_price > 0 else 0
		var qty := mini(mini(MAX_BUY_QTY, available), affordable)
		if qty > 0:
			_bot_do_buy(loc, best_good, qty, credits)
			_action_type_history.append("BUY")
			return true
	else:
		var hop := _bot_get_next_hop(loc, best_node)
		if not hop.is_empty():
			_bot_do_travel(loc, hop, "toward buy %s profit=%d" % [best_good, best_profit])
			_action_type_history.append("TRAVEL")
			return true
	return false


# ---- Action executors ----

func _bot_do_buy(loc: String, good_id: String, qty: int, credits_before: int) -> void:
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, true)
	# Sample feel state immediately after buy — credit flash should be active.
	_sample_feel_state()
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps.get("credits", 0))
	var succeeded := credits_after < credits_before
	_record_action(_decision, "BUY", loc, good_id, qty, "credits %d->%d" % [credits_before, credits_after])
	if succeeded:
		_total_buys += 1
		_trades_since_explore += 1
		var cost := credits_before - credits_after
		_total_spent += cost
		_goods_bought[good_id] = true
		_consecutive_idles = 0
		_good_trade_count[good_id] = _good_trade_count.get(good_id, 0) + 1
		_track_price(good_id, cost / maxi(1, qty))
		if _total_buys <= 3:
			_try_capture("mid_trade_buy_%d" % _total_buys)


func _bot_do_sell(loc: String, good_id: String, qty: int) -> void:
	var ps_pre: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_before := int(ps_pre.get("credits", 0))
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, false)
	# Sample feel state immediately after trade — credit flash should be active.
	_sample_feel_state()
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps.get("credits", 0))
	var succeeded := credits_after > credits_before
	_record_action(_decision, "SELL", loc, good_id, qty, "credits %d->%d" % [credits_before, credits_after])
	if succeeded:
		_total_sells += 1
		_trades_since_explore += 1
		var profit := credits_after - credits_before
		_total_earned += profit
		_goods_sold[good_id] = true
		_consecutive_idles = 0
		_track_price(good_id, profit / maxi(1, qty))
		# Track per-trade profitability
		_profit_per_trade.append(profit)
		if profit > _best_single_profit:
			_best_single_profit = profit
		_trades_profitable += 1
		if profit > 100:
			_record_reward("TRADE_PROFIT_%d" % profit)


func _visual_hull_guard() -> void:
	# Prevent game over in visual mode — NPCs attack via physics engine
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull := int(hp.get("hull", 100))
		var hull_max := int(hp.get("hull_max", 100))
		if hull_max > 0 and hull < hull_max / 2:
			if _bridge.has_method("ForceRepairPlayerHullV0"):
				_bridge.call("ForceRepairPlayerHullV0")
	# Reset game_over flag on GameManager so scene doesn't change to loss_screen
	if _game_manager and _game_manager.get("_game_over_triggered"):
		_game_manager.set("_game_over_triggered", false)
		_game_manager.set("_player_dead", false)


func _bot_do_travel(from: String, to: String, reason: String) -> void:
	if _is_visual and _game_manager:
		# Visual mode: use game_manager to trigger real warp animation + camera follow
		_game_manager.call("on_lane_gate_proximity_entered_v0", to)
		_game_manager.call("on_lane_arrival_v0", to)
	else:
		# Headless: instant teleport via bridge
		_bridge.call("DispatchPlayerArriveV0", to)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var new_loc := str(ps.get("current_node_id", ""))
	var succeeded := new_loc == to
	_record_action(_decision, "TRAVEL", to, "", 0, reason)
	_total_travel_count += 1
	if succeeded:
		_total_travels += 1
		var was_visited := _visited.has(to)
		_visited[to] = true
		_consecutive_idles = 0
		if was_visited:
			_backtrack_count += 1
		else:
			_record_reward("NEW_NODE_%s" % to)
			if not _event_decisions.has("first_new_system"):
				_event_decisions["first_new_system"] = _decision
		_track_faction(to)


# ---- Graph helpers ----

func _bot_get_next_hop(from: String, to: String) -> String:
	if from == to:
		return ""
	var queue: Array = [from]
	var parent: Dictionary = {}
	var seen: Dictionary = {from: true}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if current == to:
			var hop := to
			while parent.has(hop) and parent[hop] != from:
				hop = parent[hop]
			return hop
		if not _adj.has(current):
			continue
		var neighbors: Array = _adj[current]
		for n in neighbors:
			if not seen.has(n):
				seen[n] = true
				parent[n] = current
				queue.append(n)
	return ""

func _bot_find_nearest_unvisited(from: String) -> String:
	var queue: Array = [from]
	var seen: Dictionary = {from: true}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if not _visited.has(current) and current != from:
			return current
		if not _adj.has(current):
			continue
		for n in _adj[current]:
			if not seen.has(n):
				seen[n] = true
				queue.append(n)
	return ""

func _bot_find_untouched_good(loc: String, credits: int) -> Dictionary:
	var result := {"node": "", "good": ""}
	var best_dist := 999
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			var bp := int(entry.get("buy_price", 0))
			var available := int(entry.get("quantity", 0))
			if gid.is_empty() or bp <= 0 or available <= 0 or bp > credits:
				continue
			if _good_trade_count.has(gid):
				continue
			var dist := _bfs_distance(loc, nid)
			if dist < best_dist:
				best_dist = dist
				result = {"node": nid, "good": gid}
	return result

func _bot_find_least_visited(loc: String) -> String:
	var best := ""
	var min_visits := 999999
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty() or nid == loc:
			continue
		if _bfs_distance(loc, nid) >= 999:
			continue
		var visits := 1 if _visited.has(nid) else 0
		if visits < min_visits:
			min_visits = visits
			best = nid
	return best

func _bfs_distance(from: String, to: String) -> int:
	if from == to:
		return 0
	var queue: Array = [from]
	var dist: Dictionary = {from: 0}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if current == to:
			return dist[current]
		if not _adj.has(current):
			continue
		for n in _adj[current]:
			if not dist.has(n):
				dist[n] = dist[current] + 1
				queue.append(n)
	return 999


# ---- System Engagement (reduces false positives by bot actively using game systems) ----

func _bot_try_engage_systems(loc: String) -> void:
	# Called once per dock, after some initial trading. Non-blocking side actions.
	if _engagement_cooldown > 0:
		_engagement_cooldown -= 1
		return
	_engagement_cooldown = 10  # don't spam — try every 10 decisions

	# 1. Accept a mission if available (after 5+ trades)
	if _total_sells >= 5 and _missions_accepted_by_bot == 0:
		_bot_try_accept_mission()

	# 2. Install a module if available (after 3+ trades, try once)
	if _total_sells >= 3 and _modules_installed_by_bot == 0 and _modules_attempted < 3:
		_bot_try_install_module()

	# 3. Set doctrine after first combat
	if _total_combats >= 1 and not _doctrine_set_by_bot:
		_bot_try_set_doctrine()

	# 4. Start research after 8+ trades
	if _total_sells >= 8 and not _research_started_by_bot:
		_bot_try_start_research(loc)

	# 5. Scan discoveries at current node
	if _total_sells >= 2:
		_bot_try_scan_discoveries(loc)

	# 6. Collect adaptation fragments at current node
	_bot_try_collect_fragments(loc)

	# 7. Create a trade charter (automation) after 15+ trades
	if _total_sells >= 15 and not _automation_attempted:
		_bot_try_create_automation(loc)


func _bot_try_accept_mission() -> void:
	if not _bridge.has_method("GetMissionListV0") or not _bridge.has_method("AcceptMissionV0"):
		return
	var missions = _bridge.call("GetMissionListV0")
	if not missions is Array:
		return
	for m in missions:
		if not m is Dictionary:
			continue
		var st: String = str(m.get("status", ""))
		if st != "available":
			continue
		var mid: String = str(m.get("id", ""))
		if mid.is_empty():
			continue
		_missions_attempted += 1
		var accepted = _bridge.call("AcceptMissionV0", mid)
		if accepted is bool and accepted:
			_missions_accepted_by_bot += 1
			_a.log("ENGAGE|d=%d accepted mission %s" % [_decision, mid])
			_record_reward("MISSION_ACCEPTED")
			_introduce_system("missions")
			return
		elif accepted is bool:
			_a.log("ENGAGE|d=%d mission %s rejected (prerequisites?)" % [_decision, mid])


func _bot_try_install_module() -> void:
	if not _bridge.has_method("GetAvailableModulesV0") or not _bridge.has_method("InstallModuleV0"):
		return
	if not _bridge.has_method("GetHeroShipLoadoutV0"):
		return
	var loadout = _bridge.call("GetHeroShipLoadoutV0")
	if not loadout is Array:
		return
	# Find empty slots with their real slot_index and slot_kind
	var empty_slots: Array = []  # [{slot_index, slot_kind}]
	for i in range(loadout.size()):
		var slot = loadout[i]
		if slot is Dictionary and str(slot.get("installed_module_id", "")).is_empty():
			empty_slots.append({
				"slot_index": int(slot.get("slot_index", i)),
				"slot_kind": str(slot.get("slot_kind", "")),
			})
	if empty_slots.is_empty():
		return  # no empty slots
	var available = _bridge.call("GetAvailableModulesV0")
	if not available is Array or available.size() == 0:
		_modules_attempted += 1
		_a.log("ENGAGE|d=%d no modules available for install" % _decision)
		return
	# Try to find a module that matches an empty slot's kind and is installable
	for slot_info in empty_slots:
		var target_kind: String = slot_info["slot_kind"]
		var target_idx: int = slot_info["slot_index"]
		for mod in available:
			if not mod is Dictionary:
				continue
			var mod_id: String = str(mod.get("module_id", ""))
			var mod_kind: String = str(mod.get("slot_kind", ""))
			if mod_id.is_empty():
				continue
			if mod_kind != target_kind:
				continue
			# Skip modules that require tech/faction the player doesn't have
			if not bool(mod.get("can_install", false)):
				continue
			if bool(mod.get("is_locked", false)):
				continue
			_modules_attempted += 1
			var result = _bridge.call("InstallModuleV0", "fleet_trader_1", target_idx, mod_id)
			if result is Dictionary and bool(result.get("success", false)):
				_modules_installed_by_bot += 1
				_a.log("ENGAGE|d=%d installed module %s in slot %d (%s)" % [_decision, mod_id, target_idx, target_kind])
				_record_reward("MODULE_INSTALLED")
				_introduce_system("refit")
				return
			else:
				var reason: String = str(result.get("reason", "unknown")) if result is Dictionary else "non-dict result"
				_a.log("ENGAGE|d=%d module %s install failed: %s" % [_decision, mod_id, reason])
				return  # don't spam — try again next cooldown


func _bot_try_set_doctrine() -> void:
	if not _bridge.has_method("SetFleetDoctrineV0"):
		return
	_doctrine_attempted = true
	var ok = _bridge.call("SetFleetDoctrineV0", "fleet_trader_1", "defensive", 30, 2)
	if ok is bool and ok:
		_doctrine_set_by_bot = true
		_a.log("ENGAGE|d=%d doctrine set to defensive" % _decision)
		_introduce_system("doctrine")
	else:
		_a.log("ENGAGE|d=%d doctrine set failed" % _decision)


func _bot_try_start_research(loc: String) -> void:
	if not _bridge.has_method("GetTechTreeV0") or not _bridge.has_method("StartResearchV0"):
		return
	# Check if already researching
	if _bridge.has_method("GetResearchStatusV0"):
		var rs = _bridge.call("GetResearchStatusV0")
		if rs is Dictionary and bool(rs.get("researching", false)):
			return  # already researching something
	_research_attempted = true
	var tree = _bridge.call("GetTechTreeV0")
	if not tree is Array:
		return
	for tech in tree:
		if not tech is Dictionary:
			continue
		var st: String = str(tech.get("status", ""))
		if st != "available":
			continue
		var tid: String = str(tech.get("tech_id", ""))
		if tid.is_empty():
			continue
		var result = _bridge.call("StartResearchV0", tid, loc)
		if result is Dictionary and bool(result.get("success", false)):
			_research_started_by_bot = true
			_a.log("ENGAGE|d=%d started research %s" % [_decision, tid])
			_record_reward("RESEARCH_STARTED")
			_introduce_system("research")
			return
		else:
			var reason: String = str(result.get("error", "unknown")) if result is Dictionary else "non-dict"
			_a.log("ENGAGE|d=%d research %s start failed: %s" % [_decision, tid, reason])


func _bot_try_scan_discoveries(loc: String) -> void:
	if not _bridge.has_method("GetDiscoverySnapshotV0") or not _bridge.has_method("DispatchScanDiscoveryV0"):
		return
	var discoveries = _bridge.call("GetDiscoverySnapshotV0", loc)
	if not discoveries is Array:
		return
	for disc in discoveries:
		if not disc is Dictionary:
			continue
		var phase: String = str(disc.get("phase", ""))
		var disc_id: String = str(disc.get("discovery_id", ""))
		if disc_id.is_empty():
			continue
		# Scan unseen or partially-scanned discoveries
		if phase == "Unseen" or phase == "Detected" or phase == "Scanned":
			_bridge.call("DispatchScanDiscoveryV0", disc_id)
			_discoveries_scanned += 1
			_a.log("ENGAGE|d=%d scanned discovery %s (phase=%s)" % [_decision, disc_id, phase])
			_record_reward("DISCOVERY_SCAN")
			_introduce_system("discovery")
			return  # one per cooldown


func _bot_try_collect_fragments(loc: String) -> void:
	if not _bridge.has_method("GetAdaptationFragmentsV0") or not _bridge.has_method("CollectFragmentV0"):
		return
	var fragments = _bridge.call("GetAdaptationFragmentsV0")
	if not fragments is Array:
		return
	for frag in fragments:
		if not frag is Dictionary:
			continue
		if bool(frag.get("collected", false)):
			continue
		var frag_id: String = str(frag.get("fragment_id", ""))
		var frag_node: String = str(frag.get("node_id", ""))
		if frag_id.is_empty():
			continue
		# Can only collect if we're at the fragment's node
		if frag_node == loc:
			var result = _bridge.call("CollectFragmentV0", frag_id)
			if result is Dictionary and bool(result.get("success", false)):
				_fragments_collected += 1
				_a.log("ENGAGE|d=%d collected fragment %s" % [_decision, frag_id])
				_record_reward("FRAGMENT_COLLECTED")
				_introduce_system("fragments")


func _bot_try_create_automation(loc: String) -> void:
	if not _bridge.has_method("CreateTradeCharterProgram"):
		return
	_automation_attempted = true
	# Find a profitable route the bot has traded on
	if _goods_sold.is_empty() or _all_nodes.size() < 2:
		return
	# Pick the most-traded good and create a charter between home and best sell node
	var best_good := ""
	var best_count := 0
	for gid in _good_trade_count.keys():
		if int(_good_trade_count[gid]) > best_count:
			best_count = int(_good_trade_count[gid])
			best_good = gid
	if best_good.is_empty() or _home_node_id.is_empty():
		return
	# Find a different node that sells this good at a good price
	var dest_node := ""
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid == _home_node_id or nid.is_empty():
			continue
		dest_node = nid
		break
	if dest_node.is_empty():
		return
	var charter_id = _bridge.call("CreateTradeCharterProgram",
		_home_node_id, dest_node, best_good, best_good, 60)
	if charter_id is String and not charter_id.is_empty():
		_automation_created = true
		_a.log("ENGAGE|d=%d created trade charter %s (%s: %s -> %s)" % [
			_decision, charter_id, best_good, _home_node_id, dest_node])
		_record_reward("AUTOMATION_CREATED")
		_introduce_system("automation")
	else:
		_a.log("ENGAGE|d=%d trade charter creation failed" % _decision)


# ---- Helpers ----

func _build_adjacency() -> void:
	_adj.clear()
	_all_nodes.clear()
	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_nodes = galaxy.get("system_nodes", [])
	var lanes: Array = galaxy.get("lane_edges", [])
	for node in _all_nodes:
		if node is Dictionary:
			var nid := str(node.get("node_id", ""))
			if not _adj.has(nid):
				_adj[nid] = []
	for lane in lanes:
		if lane is Dictionary:
			var from_id := str(lane.get("from_id", ""))
			var to_id := str(lane.get("to_id", ""))
			if _adj.has(from_id) and not to_id in _adj[from_id]:
				_adj[from_id].append(to_id)
			if _adj.has(to_id) and not from_id in _adj[to_id]:
				_adj[to_id].append(from_id)

func _build_faction_map() -> void:
	if not _bridge.has_method("GetFactionTerritoryOverlayV0"):
		return
	var terr: Dictionary = _bridge.call("GetFactionTerritoryOverlayV0")
	for nid in terr:
		var info: Dictionary = terr[nid]
		var fid := str(info.get("controlling_faction", ""))
		if not fid.is_empty():
			_faction_map[str(nid)] = fid

func _track_faction(node_id: String) -> void:
	var fid: String = str(_faction_map.get(node_id, ""))
	if fid is String and not fid.is_empty() and not _factions_visited.has(fid):
		_factions_visited[fid] = true

func _track_price(good_id: String, price: int) -> void:
	if not _price_history.has(good_id):
		_price_history[good_id] = []
	_price_history[good_id].append(price)

func _record_action(cycle: int, type: String, node: String, good: String, qty: int, detail: String) -> void:
	_actions.append({"cycle": cycle, "type": type, "node": node, "good": good, "qty": qty, "detail": detail})

func _record_reward(name: String) -> void:
	_reward_events.append(_decision)
	_a.log("REWARD|d=%d|%s" % [_decision, name])

func _introduce_system(name: String) -> void:
	if name not in _systems_introduced:
		_systems_introduced.append(name)
		_system_intro_decisions.append(_decision)
		_a.log("SYSTEM_INTRO|d=%d|%s" % [_decision, name])

func _log_milestone(name: String, detail: String) -> void:
	_milestone_log.append({"decision": _decision, "name": name, "detail": detail})
	_a.log("MILESTONE|d=%d|%s|%s" % [_decision, name, detail])

func _promote_fo() -> void:
	if not _bridge.has_method("GetFirstOfficerCandidatesV0"):
		return
	var candidates: Array = _bridge.call("GetFirstOfficerCandidatesV0")
	if candidates.size() > 0:
		var ctype := str(candidates[0].get("type", ""))
		if not ctype.is_empty() and _bridge.has_method("PromoteFirstOfficerV0"):
			_fo_promoted = _bridge.call("PromoteFirstOfficerV0", ctype)
			_a.log("FO_PROMOTE|%s success=%s" % [ctype, str(_fo_promoted)])

func _try_capture(label: String) -> void:
	_screenshot.capture_v0(self, label, "res://reports/experience/screenshots/")

func _get_neighbors(node_id: String) -> Array:
	if not _adj.has(node_id):
		return []
	return _adj[node_id].duplicate()


# ===================== Visual Tour Phase =====================
# Opens each UI panel, captures a screenshot, then closes it.
# Docks at current station, cycles dock tabs, captures each.
# Forces interesting visual states (hull damage, galaxy map).
# Each step: open (1 frame) -> settle (N frames) -> capture -> close (1 frame) -> settle -> next.

# Tour step table: [action, label]
# Actions: "panel:METHOD", "dock", "dock_tab:N", "undock", "force:METHOD", "done"
const TOUR_STEPS := [
	# -- Flight view baseline (before any panels) --
	["capture", "20_flight_baseline"],
	# -- Galaxy map overlay (safe, no game state change) --
	["panel:toggle_galaxy_map_overlay_v0", "galaxy_map_open"],
	["capture", "30_galaxy_map"],
	["panel:toggle_galaxy_map_overlay_v0", "galaxy_map_close"],
	# -- Knowledge web --
	["panel:_toggle_knowledge_web_v0", "knowledge_open"],
	["capture", "31_knowledge_web"],
	["panel:_toggle_knowledge_web_v0", "knowledge_close"],
	# -- Mission journal --
	["panel:_toggle_mission_journal_v0", "mission_open"],
	["capture", "32_mission_journal"],
	["panel:_toggle_mission_journal_v0", "mission_close"],
	# -- FO panel --
	["panel:_toggle_fo_panel_v0", "fo_open"],
	["capture", "33_fo_panel"],
	["panel:_toggle_fo_panel_v0", "fo_close"],
	# -- Automation dashboard --
	["panel:_toggle_automation_dashboard_v0", "automation_open"],
	["capture", "34_automation"],
	["panel:_toggle_automation_dashboard_v0", "automation_close"],
	# -- Combat log --
	["panel:_toggle_combat_log_v0", "combatlog_open"],
	["capture", "35_combat_log"],
	["panel:_toggle_combat_log_v0", "combatlog_close"],
	# -- Empire dashboard --
	["panel:_toggle_empire_dashboard_v0", "empire_open"],
	["capture", "36_empire_dashboard"],
	["panel:_toggle_empire_dashboard_v0", "empire_close"],
	# -- Data log --
	["panel:_toggle_data_log_v0", "datalog_open"],
	["capture", "37_data_log"],
	["panel:_toggle_data_log_v0", "datalog_close"],
	# -- Megaproject panel --
	["panel:_toggle_megaproject_panel_v0", "mega_open"],
	["capture", "38_megaproject"],
	["panel:_toggle_megaproject_panel_v0", "mega_close"],
	# -- Warfront dashboard --
	["panel:_toggle_warfront_dashboard_v0", "warfront_open"],
	["capture", "39_warfront"],
	["panel:_toggle_warfront_dashboard_v0", "warfront_close"],
	# -- Dock at current location (last — docking changes game state) --
	["dock", "dock_open"],
	["capture", "21_dock_market"],
	["dock_tab:1", "dock_tab_jobs"],
	["capture", "22_dock_jobs"],
	["dock_tab:2", "dock_tab_missions"],
	["capture", "23_dock_missions"],
	["dock_tab:3", "dock_tab_intel"],
	["capture", "24_dock_intel"],
	["dock_tab:4", "dock_tab_ships"],
	["capture", "25_dock_ships"],
	["dock_tab:5", "dock_tab_fuel"],
	["capture", "26_dock_fuel"],
	["dock_tab:6", "dock_tab_explore"],
	["capture", "27_dock_explore"],
	["dock_tab:7", "dock_tab_govt"],
	["capture", "28_dock_govt"],
	["undock", "undock"],
	# -- Final flight view after tour --
	["capture", "40_final_flight"],
	# -- Done --
	["done", "tour_complete"],
]


func _do_visual_tour() -> void:
	# Ensure player is alive and game_over is suppressed for the entire tour
	if _tour_step == 0:
		_visual_hull_guard()
		# Dismiss loss screen if it appeared — change scene back to main
		var loss = root.get_node_or_null("LossScreen")
		if loss and is_instance_valid(loss):
			loss.queue_free()

	# Settle between steps to let UI animate
	if _tour_settle > 0:
		_tour_settle -= 1
		return

	if _tour_step >= TOUR_STEPS.size():
		_phase = Phase.REPORT
		return

	var step: Array = TOUR_STEPS[_tour_step]
	var action: String = str(step[0])
	var label: String = str(step[1])

	if action == "done":
		_a.log("VISUAL_TOUR|DONE|steps=%d" % _tour_step)
		_phase = Phase.REPORT
		return

	if action == "capture":
		_try_capture(label)
		_tour_step += 1
		_tour_settle = 3  # brief settle after capture
		return

	if action.begins_with("panel:"):
		var method := action.trim_prefix("panel:")
		if _game_manager and _game_manager.has_method(method):
			_game_manager.call(method)
			_a.log("VISUAL_TOUR|PANEL|%s" % method)
			# Sample feel state after galaxy map toggle — player marker should be visible.
			if method.contains("galaxy"):
				_sample_feel_state()
		else:
			_a.log("VISUAL_TOUR|PANEL_SKIP|%s" % method)
		_tour_step += 1
		_tour_settle = TOUR_SETTLE_FRAMES
		return

	if action == "dock":
		_visual_dock_open()
		_tour_step += 1
		_tour_settle = TOUR_SETTLE_FRAMES + 5  # dock needs more settle
		return

	if action.begins_with("dock_tab:"):
		var tab_idx := int(action.trim_prefix("dock_tab:"))
		_visual_switch_dock_tab(tab_idx)
		_tour_step += 1
		_tour_settle = TOUR_SETTLE_FRAMES
		return

	if action == "undock":
		_visual_undock()
		_tour_step += 1
		_tour_settle = TOUR_SETTLE_FRAMES
		return

	if action.begins_with("force:"):
		var method := action.trim_prefix("force:")
		if _bridge and _bridge.has_method(method):
			_bridge.call(method)
			_a.log("VISUAL_TOUR|FORCE|%s" % method)
		else:
			_a.log("VISUAL_TOUR|FORCE_SKIP|%s" % method)
		_tour_step += 1
		_tour_settle = TOUR_SETTLE_FRAMES
		return

	# Unknown action — skip
	_tour_step += 1


func _visual_dock_open() -> void:
	if _game_manager == null:
		return
	# Find current location
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	if loc.is_empty():
		_a.log("VISUAL_TOUR|DOCK_SKIP|no_location")
		return
	# Clear NPC fleet ships from scene to prevent hostile-nearby dock guard.
	for ship in root.get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship):
			ship.remove_from_group("FleetShip")
	# Create mock station node for dock trigger
	var mock := Node3D.new()
	mock.add_to_group("Station")
	mock.set_meta("dock_target_id", loc)
	root.add_child(mock)
	_game_manager.call("on_proximity_dock_entered_v0", mock)
	_a.log("VISUAL_TOUR|DOCK|%s" % loc)


func _visual_switch_dock_tab(idx: int) -> void:
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm and htm.has_method("_switch_dock_tab"):
		htm.call("_switch_dock_tab", idx)
		_a.log("VISUAL_TOUR|DOCK_TAB|%d" % idx)
	else:
		_a.log("VISUAL_TOUR|DOCK_TAB_SKIP|%d|no_htm" % idx)


func _visual_undock() -> void:
	if _game_manager and _game_manager.has_method("undock_v0"):
		_game_manager.call("undock_v0")
		_a.log("VISUAL_TOUR|UNDOCK")


# ===================== Report Phase =====================

func _do_report() -> void:
	_busy = true
	_a.log("REPORT_START|decisions=%d" % _decision)

	# Final sample
	_sample_telemetry()

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var end_credits := int(ps.get("credits", 0))
	var net_profit := end_credits - _start_credits

	# ---- Dimension 1: Economy Health ----
	_a.log("ECONOMY|credits_start=%d end=%d growth=%d%%" % [
		_start_credits, end_credits,
		(net_profit * 100) / maxi(_start_credits, 1)])
	_a.log("ECONOMY|buys=%d sells=%d spent=%d earned=%d" % [
		_total_buys, _total_sells, _total_spent, _total_earned])
	_a.log("ECONOMY|direction_changes=%d plateau_max=%d" % [
		_credit_direction_changes, _consecutive_credit_unchanged])
	_a.log("ECONOMY|goods_bought=%s" % str(_goods_bought.keys()))
	_a.log("ECONOMY|goods_sold=%s" % str(_goods_sold.keys()))
	# Price collapse check
	for gid in _price_history:
		var prices: Array = _price_history[gid]
		if prices.size() < 6:
			continue
		var first_avg := _avg_slice(prices, 0, mini(3, prices.size()))
		var last_avg := _avg_slice(prices, maxi(0, prices.size() - 3), prices.size())
		if first_avg > 0 and last_avg < first_avg * 0.5:
			_a.flag("PRICE_COLLAPSE_%s" % gid)
	# Untraded goods
	var all_goods: Dictionary = {}
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			if not gid.is_empty():
				all_goods[gid] = true
	var untraded: Array = []
	for gid in all_goods:
		if not _goods_bought.has(gid) and not _goods_sold.has(gid):
			untraded.append(gid)
	_a.log("ECONOMY|goods_total=%d untraded=%d untraded_list=%s" % [
		all_goods.size(), untraded.size(), str(untraded)])
	# Idle rate
	var idle_pct := (float(_total_idles) / maxi(_decision, 1)) * 100.0
	_a.log("ECONOMY|idle_pct=%.1f longest_idle=%d" % [idle_pct, _max_consecutive_idles])
	# NPC activity
	if _bridge.has_method("GetNpcTradeRoutesV0"):
		var routes = _bridge.call("GetNpcTradeRoutesV0")
		_a.log("ECONOMY|npc_routes=%d" % (routes.size() if routes is Array else 0))
	# Upkeep — GetFleetUpkeepV0(fleetId) requires a fleet ID
	if _bridge.has_method("GetFleetUpkeepV0"):
		var upkeep = _bridge.call("GetFleetUpkeepV0", "fleet_trader_1")
		_a.log("ECONOMY|upkeep=%s" % str(upkeep))
	# Sink/faucet — include credit trajectory dips as implicit sinks (upkeep, wages, fuel, repairs)
	var implicit_sinks := 0
	for i in range(1, _credit_trajectory.size()):
		var diff: int = _credit_trajectory[i] - _credit_trajectory[i - 1]
		if diff < 0:
			implicit_sinks += absi(diff)
	var total_sinks := _total_spent + implicit_sinks
	var sink_faucet := float(total_sinks) / maxi(_total_earned, 1)
	_a.log("ECONOMY|sink_faucet_ratio=%.2f (explicit=%d implicit=%d)" % [sink_faucet, _total_spent, implicit_sinks])

	# ---- Dimension 2: Pacing ----
	# Credit curve shape
	var curve_shape := _classify_credit_curve()
	_a.log("PACING|credit_curve=%s" % curve_shape)
	# Reward frequency
	var reward_gaps: Array = []
	for i in range(1, _reward_events.size()):
		reward_gaps.append(_reward_events[i] - _reward_events[i - 1])
	var mean_gap := 0.0
	var max_gap := 0
	if reward_gaps.size() > 0:
		var total := 0
		for g in reward_gaps:
			total += g
			if g > max_gap:
				max_gap = g
		mean_gap = float(total) / reward_gaps.size()
	_a.log("PACING|rewards=%d mean_gap=%.1f max_gap=%d" % [
		_reward_events.size(), mean_gap, max_gap])
	# Activity variety (Shannon entropy)
	var entropy := _compute_action_entropy()
	_a.log("PACING|action_entropy=%.2f" % entropy)
	# Longest monotonous streak
	var longest_streak := _compute_longest_streak()
	_a.log("PACING|longest_streak=%d" % longest_streak)
	# FO silence
	var fo_max_silence := _compute_fo_max_silence()
	_a.log("PACING|fo_lines=%d fo_max_silence=%d" % [_fo_dialogue_count, fo_max_silence])

	# ---- Dimension 3: Grind Detection ----
	var grind_score := _compute_grind_score()
	_a.log("GRIND|score=%.2f longest_streak=%d" % [grind_score, longest_streak])
	var route_repeats := _compute_route_repeats()
	_a.log("GRIND|max_route_repeat=%d" % route_repeats)
	var good_repeats := _compute_good_repeats()
	_a.log("GRIND|max_good_repeat=%d" % good_repeats)

	# ---- Dimension 4: Flow/Engagement ----
	# Novelty rate
	var novelty := _compute_novelty_rate()
	_a.log("FLOW|novelty_rate=%.3f" % novelty)
	# Something-new rate in first 300 decisions
	var early_events := 0
	for r in _reward_events:
		if r <= 300:
			early_events += 1
	_a.log("FLOW|early_rewards=%d (first 300 decisions)" % early_events)

	# ---- Dimension 5: Combat ----
	_a.log("COMBAT|total=%d kills=%d" % [_total_combats, _total_kills])
	if _combat_hull_mins.size() > 0:
		var min_hull := 100
		for h in _combat_hull_mins:
			if h < min_hull:
				min_hull = h
		_a.log("COMBAT|hull_min=%d%%" % min_hull)
	# Combat spacing
	var combat_decisions: Array = []
	for i in range(_actions.size()):
		if _actions[i].get("type", "") == "COMBAT":
			combat_decisions.append(_actions[i].get("cycle", 0))
	if combat_decisions.size() >= 2:
		var spacings: Array = []
		for i in range(1, combat_decisions.size()):
			spacings.append(combat_decisions[i] - combat_decisions[i - 1])
		var mean_spacing := 0
		for s in spacings:
			mean_spacing += s
		mean_spacing = mean_spacing / spacings.size()
		_a.log("COMBAT|mean_spacing=%d" % mean_spacing)

	# ---- Dimension 6: Exploration ----
	var visit_pct := (float(_visited.size()) / maxi(_all_nodes.size(), 1)) * 100.0
	var backtrack_pct := (float(_backtrack_count) / maxi(_total_travel_count, 1)) * 100.0
	_a.log("EXPLORE|visited=%d/%d (%.0f%%)" % [_visited.size(), _all_nodes.size(), visit_pct])
	_a.log("EXPLORE|factions=%d list=%s" % [_factions_visited.size(), str(_factions_visited.keys())])
	_a.log("EXPLORE|backtrack=%.0f%% (%d/%d)" % [backtrack_pct, _backtrack_count, _total_travel_count])
	# Geographic spread (max BFS from home)
	var max_depth := 0
	for nid in _visited:
		var d := _bfs_distance(_home_node_id, nid)
		if d < 999 and d > max_depth:
			max_depth = d
	_a.log("EXPLORE|max_depth=%d" % max_depth)
	# Discoveries + fragments + knowledge
	_probe_discoveries()
	_probe_knowledge()

	# ---- Dimension 7: Progressive Disclosure ----
	_a.log("DISCLOSURE|systems=%s" % str(_systems_introduced))
	_a.log("DISCLOSURE|intro_decisions=%s" % str(_system_intro_decisions))
	if _system_intro_decisions.size() >= 2:
		var spacings2: Array = []
		for i in range(1, _system_intro_decisions.size()):
			spacings2.append(_system_intro_decisions[i] - _system_intro_decisions[i - 1])
		var mean_spacing2 := 0
		for s in spacings2:
			mean_spacing2 += s
		mean_spacing2 = mean_spacing2 / spacings2.size()
		_a.log("DISCLOSURE|mean_intro_spacing=%d" % mean_spacing2)

	# ---- Dimension 8: First Officer ----
	_a.log("FO|promoted=%s lines=%d" % [str(_fo_promoted), _fo_dialogue_count])

	# ---- Dimension 9: Decision Quality ----
	_a.log("DECISIONS|total=%d travels=%d buys=%d sells=%d combats=%d idles=%d" % [
		_decision, _total_travels, _total_buys, _total_sells, _total_combats, _total_idles])

	# ---- Dimension 10: Robustness ----
	_a.hard(_total_buys > 0, "economy_has_buys", "buys=%d" % _total_buys)
	_a.hard(_total_sells > 0, "economy_has_sells", "sells=%d" % _total_sells)
	_a.hard(net_profit >= 0, "economy_net_positive", "profit=%d" % net_profit)
	_a.warn(_total_combats > 0, "combat_encountered", "combats=%d" % _total_combats)
	_a.warn(_visited.size() >= 3, "exploration_minimum", "visited=%d" % _visited.size())
	_a.warn(_factions_visited.size() >= 2, "faction_diversity", "factions=%d" % _factions_visited.size())
	_a.warn(idle_pct < 10.0, "idle_rate_acceptable", "%.1f%%" % idle_pct)
	_a.warn(longest_streak < 20, "no_grinding", "streak=%d" % longest_streak)
	_a.warn(grind_score < 0.15, "grind_score_low", "%.2f" % grind_score)
	_a.warn(max_gap < 80, "no_reward_desert", "max_gap=%d" % max_gap)
	_a.warn(_threat_bands_seen.size() >= 2, "security_gradient", "bands=%d" % _threat_bands_seen.size())
	_a.warn(_profit_per_trade.size() > 0, "trades_completed", "trades=%d" % _profit_per_trade.size())

	# ---- Dimension 11: Story/Tension ----
	_probe_story()

	# ---- Dimension 12: Visual (visual mode probes) ----
	_probe_visual()

	# ---- Dimension 13: Progression ----
	_probe_progression()

	# ---- Dimension 14: Market Intelligence ----
	_probe_market_intel()

	# ---- Dimension 15: Mission Engagement ----
	_probe_missions()

	# ---- Dimension 16: Fleet & Loadout ----
	_probe_fleet_loadout()

	# ---- Dimension 17: Security & Tension ----
	_probe_security()

	# ---- Dimension 18: Haven ----
	_probe_haven()

	# ---- Dimension 19: Narrative Depth ----
	_probe_narrative_depth()

	# ---- Dimension 20: Combat Depth ----
	_probe_combat_depth()

	# ---- Summary Report Card ----
	_a.log("")
	_a.log("========= FIRST-HOUR EXPERIENCE REPORT =========")
	_a.log("HEADER|seed=%d archetype=%s decisions=%d ticks=%d" % [
		_user_seed, _archetype, _decision, _decision * TICK_ADVANCE])
	_a.log("SCORE|ECONOMY|growth=%d%% goods=%d/%d idle=%.0f%% curve=%s" % [
		(net_profit * 100) / maxi(_start_credits, 1),
		_goods_bought.size() + _goods_sold.size(), all_goods.size(),
		idle_pct, curve_shape])
	_a.log("SCORE|PACING|rewards=%d mean_gap=%.0f max_gap=%d entropy=%.1f streak=%d" % [
		_reward_events.size(), mean_gap, max_gap, entropy, longest_streak])
	_a.log("SCORE|COMBAT|count=%d hull_min=%d%%" % [
		_total_combats, _combat_hull_mins.min() if _combat_hull_mins.size() > 0 else 100])
	_a.log("SCORE|EXPLORE|visited=%.0f%% factions=%d depth=%d backtrack=%.0f%%" % [
		visit_pct, _factions_visited.size(), max_depth, backtrack_pct])
	_a.log("SCORE|GRIND|score=%.2f route_repeat=%d good_repeat=%d" % [
		grind_score, route_repeats, good_repeats])
	_a.log("SCORE|FO|promoted=%s lines=%d max_silence=%d" % [
		str(_fo_promoted), _fo_dialogue_count, fo_max_silence])
	_a.log("SCORE|DISCLOSURE|systems=%d" % _systems_introduced.size())
	var avg_ppt := 0
	if _profit_per_trade.size() > 0:
		var ppt_total := 0
		for p in _profit_per_trade:
			ppt_total += p
		avg_ppt = ppt_total / _profit_per_trade.size()
	_a.log("SCORE|PROGRESSION|milestones=%d endgame_paths=%d avg_profit_trade=%d" % [
		_milestones_unlocked, _endgame_paths_revealed, avg_ppt])
	_a.log("SCORE|MARKET_INTEL|alerts=%d shocks=%d spreads=%d" % [
		_market_alerts_seen, _supply_shocks_seen, _price_spreads.size()])
	_a.log("SCORE|MISSIONS|available=%d accepted=%d bounties=%d first_d=%d" % [
		_missions_available, _missions_accepted, _bounties_available, _mission_first_seen])
	_a.log("SCORE|FLEET|modules=%d/%d weapons=%d techs=%d/%d" % [
		_modules_installed, _modules_installed + _modules_empty,
		_weapons_equipped, _techs_unlocked, _techs_total])
	_a.log("SCORE|SECURITY|bands=%d max_war=%.1f ambush_lanes=%d" % [
		_threat_bands_seen.size(), _max_war_intensity, _lane_ambush_count])
	_a.log("SCORE|HAVEN|discovered=%s projects=%d residents=%d" % [
		str(_haven_discovered), _construction_projects, _haven_residents])
	_a.log("SCORE|NARRATIVE|revelation=%d data_logs=%d npcs=%d fracture=%s" % [
		_revelation_stage, _data_logs_found, _narrative_npcs_met, str(_fracture_accessible)])
	_a.log("SCORE|COMBAT_DEPTH|heat_max=%.1f doctrine=%s weapons=%d drones=%d" % [
		_heat_max, str(_doctrine_set), _weapons_equipped, _drones_active])
	var feel_active := 0
	for b in [_feel_credits_flash_seen, _feel_damage_flash_seen, _feel_combat_vignette_seen,
			_feel_combat_banner_seen, _feel_toast_seen, _feel_galaxy_marker_seen]:
		if b: feel_active += 1
	if _feel_shake_intensity_max > 0.0: feel_active += 1
	_a.log("SCORE|FEEL_PRESENCE|active=%d/7 checks=%d flash_cr=%s flash_dmg=%s vignette=%s banner=%s toast=%s marker=%s shake=%.2f" % [
		feel_active, _feel_checks_run, str(_feel_credits_flash_seen), str(_feel_damage_flash_seen),
		str(_feel_combat_vignette_seen), str(_feel_combat_banner_seen), str(_feel_toast_seen),
		str(_feel_galaxy_marker_seen), _feel_shake_intensity_max])
	_a.log("SCORE|EVENTS|first_buy=%d first_sell=%d first_combat=%d first_fo_line=%d first_new_system=%d" % [
		_event_decisions.get("first_buy", -1), _event_decisions.get("first_sell", -1),
		_event_decisions.get("first_combat", -1), _event_decisions.get("first_fo_line", -1),
		_event_decisions.get("first_new_system", -1)])
	_a.log("SCORE|ENGAGEMENT|missions=%d/%d modules=%d/%d doctrine=%s research=%s scans=%d frags=%d auto=%s" % [
		_missions_accepted_by_bot, _missions_attempted,
		_modules_installed_by_bot, _modules_attempted,
		str(_doctrine_set_by_bot), str(_research_started_by_bot),
		_discoveries_scanned, _fragments_collected, str(_automation_created)])
	_a.log("=================================================")

	# ---- Milestones ----
	_a.log("MILESTONES|count=%d" % _milestone_log.size())
	for m in _milestone_log:
		_a.log("MILESTONE_DETAIL|d=%d|%s|%s" % [
			int(m.get("decision", 0)), str(m.get("name", "")), str(m.get("detail", ""))])

	# ---- Credit trajectory (compact) ----
	if _credit_trajectory.size() > 0:
		var traj_parts: Array = []
		for c in _credit_trajectory:
			traj_parts.append(str(c))
		_a.log("TRAJECTORY|%s" % ",".join(traj_parts))

	# ---- Issue Analysis Engine ----
	var issues: Array = _analyze_issues(
		net_profit, end_credits, idle_pct, curve_shape,
		entropy, max_gap, longest_streak, grind_score,
		route_repeats, good_repeats, visit_pct, max_depth,
		backtrack_pct, fo_max_silence, mean_gap)
	_a.log("")
	_a.log("========= ISSUES (%d found) ==========" % issues.size())
	var crit_count := 0
	var major_count := 0
	var minor_count := 0
	for issue in issues:
		var sev: String = str(issue.get("severity", ""))
		var cat: String = str(issue.get("category", ""))
		var desc: String = str(issue.get("description", ""))
		var fix: String = str(issue.get("prescription", ""))
		var file: String = str(issue.get("file", ""))
		_a.log("ISSUE|%s|%s|%s|fix=%s|file=%s" % [sev, cat, desc, fix, file])
		match sev:
			"CRITICAL": crit_count += 1
			"MAJOR": major_count += 1
			_: minor_count += 1
	_a.log("ISSUE_SUMMARY|critical=%d major=%d minor=%d total=%d" % [
		crit_count, major_count, minor_count, issues.size()])
	_a.log("======================================")

	# ---- JSON report ----
	_write_json_report(issues, net_profit, end_credits, idle_pct, curve_shape,
		entropy, max_gap, longest_streak, grind_score, route_repeats,
		good_repeats, visit_pct, max_depth, backtrack_pct, fo_max_silence)

	_a.summary()
	_busy = false
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(_a.exit_code())


# ---- Analysis helpers ----

func _classify_credit_curve() -> String:
	if _credit_trajectory.size() < 6:
		return "INSUFFICIENT_DATA"
	var n := _credit_trajectory.size()
	var third := n / 3
	var t1 := _avg_slice(_credit_trajectory, 0, third)
	var t2 := _avg_slice(_credit_trajectory, third, 2 * third)
	var t3 := _avg_slice(_credit_trajectory, 2 * third, n)
	var first_val: int = _credit_trajectory[0]
	var last_val: int = _credit_trajectory[n - 1]
	if last_val <= first_val:
		return "FLAT_OR_DECLINING"
	var growth_t1 := t2 - t1
	var growth_t2 := t3 - t2
	if growth_t1 < growth_t2 * 0.3:
		return "SIGMOID"  # slow start, ramp later
	if absf(growth_t1 - growth_t2) < growth_t1 * 0.3:
		return "LINEAR"
	if growth_t2 < growth_t1 * 0.3:
		return "FRONT_LOADED"
	return "EXPONENTIAL"


func _compute_action_entropy() -> float:
	if _action_type_history.size() == 0:
		return 0.0
	var counts: Dictionary = {}
	for a in _action_type_history:
		counts[a] = counts.get(a, 0) + 1
	var total := float(_action_type_history.size())
	var entropy := 0.0
	for a in counts:
		var p := float(counts[a]) / total
		if p > 0.0:
			entropy -= p * log(p) / log(2.0)
	return entropy


func _compute_longest_streak() -> int:
	if _action_type_history.size() == 0:
		return 0
	var longest := 1
	var current := 1
	for i in range(1, _action_type_history.size()):
		if _action_type_history[i] == _action_type_history[i - 1]:
			current += 1
			if current > longest:
				longest = current
		else:
			current = 1
	return longest


func _compute_fo_max_silence() -> int:
	if _fo_dialogue_decisions.size() < 2:
		return _decision  # FO never spoke or spoke once
	var max_gap := 0
	for i in range(1, _fo_dialogue_decisions.size()):
		var gap: int = int(_fo_dialogue_decisions[i]) - int(_fo_dialogue_decisions[i - 1])
		if gap > max_gap:
			max_gap = gap
	return max_gap


func _compute_grind_score() -> float:
	# Detect repeated buy->travel->sell->travel sequences
	if _action_type_history.size() < 8:
		return 0.0
	var pattern_len := 4  # BUY, TRAVEL, SELL, TRAVEL
	var repeats := 0
	var max_repeats := 0
	var current_repeats := 0
	for i in range(pattern_len, _action_type_history.size()):
		var match_found := true
		for j in range(pattern_len):
			if _action_type_history[i - pattern_len + j] != _action_type_history[i - 2 * pattern_len + j] if i >= 2 * pattern_len else false:
				match_found = false
				break
		if i >= 2 * pattern_len and match_found:
			current_repeats += 1
			if current_repeats > max_repeats:
				max_repeats = current_repeats
		else:
			current_repeats = 0
	return float(max_repeats) / maxf(float(pattern_len * 2), 1.0)


func _compute_route_repeats() -> int:
	var route_counts: Dictionary = {}
	var last_loc := ""
	for a in _actions:
		if a.get("type", "") == "TRAVEL":
			var dest := str(a.get("node", ""))
			if not last_loc.is_empty() and not dest.is_empty():
				var route := "%s>%s" % [last_loc, dest]
				route_counts[route] = route_counts.get(route, 0) + 1
			last_loc = dest
		elif a.get("type", "") in ["BUY", "SELL"]:
			last_loc = str(a.get("node", ""))
	var max_count: int = 0
	for r in route_counts:
		if int(route_counts[r]) > max_count:
			max_count = int(route_counts[r])
	return max_count


func _compute_good_repeats() -> int:
	var max_count: int = 0
	for gid in _good_trade_count:
		if int(_good_trade_count[gid]) > max_count:
			max_count = int(_good_trade_count[gid])
	return max_count


func _compute_novelty_rate() -> float:
	# New things per decision (new nodes, new goods, new systems)
	var total_new := _visited.size() + _goods_bought.size() + _goods_sold.size() + _systems_introduced.size()
	return float(total_new) / maxi(_decision, 1)


# ---- Probe helpers ----

func _probe_discoveries() -> void:
	if not _bridge.has_method("GetDiscoverySnapshotV0"):
		return
	for nid in _visited:
		var discs: Array = _bridge.call("GetDiscoverySnapshotV0", str(nid))
		_discoveries_found += discs.size()
	_a.log("EXPLORE|discoveries=%d" % _discoveries_found)
	if _bridge.has_method("GetAdaptationFragmentsV0"):
		var frags: Array = _bridge.call("GetAdaptationFragmentsV0")
		for f in frags:
			if f is Dictionary and bool(f.get("collected", false)):
				_fragments_found += 1
		_a.log("EXPLORE|fragments=%d" % _fragments_found)


func _probe_knowledge() -> void:
	if not _bridge.has_method("GetKnowledgeWebV0"):
		return
	var entries = _bridge.call("GetKnowledgeWebV0")
	_knowledge_entries = entries.size() if entries is Array else 0
	_a.log("EXPLORE|knowledge=%d" % _knowledge_entries)


func _probe_story() -> void:
	if _bridge.has_method("GetStoryProgressV0"):
		var story = _bridge.call("GetStoryProgressV0")
		_a.log("STORY|progress=%s" % str(story))
	if _bridge.has_method("GetDreadStateV0"):
		var dread = _bridge.call("GetDreadStateV0")
		_a.log("STORY|dread=%s" % str(dread))
	if _bridge.has_method("GetRiskMetersV0"):
		var risk = _bridge.call("GetRiskMetersV0")
		_a.log("STORY|risk=%s" % str(risk))
	if _bridge.has_method("GetWarfrontsV0"):
		var wf = _bridge.call("GetWarfrontsV0")
		_a.log("STORY|warfronts=%d" % (wf.size() if wf is Array else 0))


func _probe_progression() -> void:
	# Milestones
	if _bridge.has_method("GetMilestonesV0"):
		var ms = _bridge.call("GetMilestonesV0")
		if ms is Array:
			_milestones_unlocked = ms.size()
	# Endgame paths
	if _bridge.has_method("GetEndgamePathsV0"):
		var paths = _bridge.call("GetEndgamePathsV0")
		if paths is Array:
			_endgame_paths_revealed = 0
			for p in paths:
				if p is Dictionary and bool(p.get("revealed", false)):
					_endgame_paths_revealed += 1
	# Player stats
	if _bridge.has_method("GetPlayerStatsV0"):
		_stats_snapshot = _bridge.call("GetPlayerStatsV0")
		if not _stats_snapshot is Dictionary:
			_stats_snapshot = {}
	# Endgame progress
	if _bridge.has_method("GetEndgameProgressV0"):
		var prog = _bridge.call("GetEndgameProgressV0")
		if prog is Dictionary:
			_a.log("PROGRESSION|endgame=%s" % str(prog))
	# Pentagon state
	if _bridge.has_method("GetPentagonStateV0"):
		var pent = _bridge.call("GetPentagonStateV0")
		if pent is Dictionary:
			_a.log("PROGRESSION|pentagon=%s" % str(pent))
	_a.log("PROGRESSION|milestones=%d endgame_paths_revealed=%d" % [
		_milestones_unlocked, _endgame_paths_revealed])
	# Profit per trade summary
	if _profit_per_trade.size() > 0:
		var total_ppt := 0
		for p in _profit_per_trade:
			total_ppt += p
		var avg_ppt := total_ppt / _profit_per_trade.size()
		_a.log("PROGRESSION|avg_profit_per_trade=%d best=%d trades_profitable=%d" % [
			avg_ppt, _best_single_profit, _trades_profitable])


func _probe_market_intel() -> void:
	if _bridge.has_method("GetEconomyOverviewV0"):
		var econ_arr = _bridge.call("GetEconomyOverviewV0")
		if econ_arr is Array:
			_a.log("MARKET_INTEL|overview_entries=%d" % econ_arr.size())
	# Price spread analysis — check spread at each visited node
	for nid in _visited:
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", str(nid))
		var best_buy := 99999
		var best_sell := 0
		for entry in mv:
			if entry is Dictionary:
				var buy_p := int(entry.get("buy_price", 99999))
				var sell_p := int(entry.get("sell_price", 0))
				if buy_p < best_buy:
					best_buy = buy_p
				if sell_p > best_sell:
					best_sell = sell_p
		if best_sell > best_buy and best_buy < 99999:
			_price_spreads.append(best_sell - best_buy)
	_a.log("MARKET_INTEL|alerts=%d supply_shocks=%d price_spreads=%d" % [
		_market_alerts_seen, _supply_shocks_seen, _price_spreads.size()])
	if _price_spreads.size() > 0:
		var spread_total := 0
		for s in _price_spreads:
			spread_total += s
		_a.log("MARKET_INTEL|avg_spread=%d" % (spread_total / _price_spreads.size()))


func _probe_missions() -> void:
	_a.log("MISSIONS|available=%d accepted=%d bounties=%d commissions=%d first_seen_d=%d" % [
		_missions_available, _missions_accepted, _bounties_available,
		_commissions_active, _mission_first_seen])


func _probe_fleet_loadout() -> void:
	# Ship loadout
	if _bridge.has_method("GetHeroShipLoadoutV0"):
		_loadout_snapshot = _bridge.call("GetHeroShipLoadoutV0")
		_a.log("FLEET|loadout=%s" % str(_loadout_snapshot))
	# Module slots (re-probe for final count)
	if _bridge.has_method("GetPlayerFleetSlotsV0"):
		var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
		_modules_empty = 0
		_modules_installed = 0
		for s in slots:
			if s is Dictionary:
				if str(s.get("installed_module_id", "")).is_empty():
					_modules_empty += 1
				else:
					_modules_installed += 1
	# Weapons count — GetWeaponTrackingV0 returns Array of per-slot Dictionaries
	if _bridge.has_method("GetWeaponTrackingV0"):
		var weap = _bridge.call("GetWeaponTrackingV0", "fleet_trader_1")
		if weap is Array:
			_weapons_equipped = weap.size()
	# Tech tree
	if _bridge.has_method("GetTechTreeV0"):
		var tree: Array = _bridge.call("GetTechTreeV0")
		_techs_total = tree.size()
		_techs_unlocked = 0
		for t in tree:
			if t is Dictionary and bool(t.get("unlocked", false)):
				_techs_unlocked += 1
	# Upkeep
	if _bridge.has_method("GetFleetUpkeepSummaryV0"):
		var upkeep = _bridge.call("GetFleetUpkeepSummaryV0")
		if upkeep is Dictionary:
			_a.log("FLEET|upkeep_summary=%s" % str(upkeep))
	_a.log("FLEET|modules=%d/%d weapons=%d techs=%d/%d" % [
		_modules_installed, _modules_installed + _modules_empty,
		_weapons_equipped, _techs_unlocked, _techs_total])


func _probe_security() -> void:
	_a.log("SECURITY|bands_seen=%s max_war=%.2f samples=%d" % [
		str(_threat_bands_seen), _max_war_intensity, _security_samples.size()])
	# Lane security check
	if _bridge.has_method("GetLaneSecurityV0"):
		var ambush_lanes := 0
		for nid in _visited:
			if _adj.has(nid):
				for neighbor in _adj[nid]:
					var ls = _bridge.call("GetLaneSecurityV0", str(nid), str(neighbor))
					if ls is Dictionary and float(ls.get("ambush_chance", 0.0)) > 0.1:
						ambush_lanes += 1
		_lane_ambush_count = ambush_lanes
		_a.log("SECURITY|ambush_lanes=%d" % ambush_lanes)


func _probe_haven() -> void:
	if _bridge.has_method("GetHavenStatusV0"):
		var haven = _bridge.call("GetHavenStatusV0")
		if haven is Dictionary:
			_haven_discovered = bool(haven.get("discovered", false))
			_a.log("HAVEN|discovered=%s" % str(_haven_discovered))
	if _haven_discovered:
		if _bridge.has_method("GetConstructionProjectsV0"):
			var proj = _bridge.call("GetConstructionProjectsV0")
			if proj is Array:
				_construction_projects = proj.size()
		if _bridge.has_method("GetHavenResidentsV0"):
			var res = _bridge.call("GetHavenResidentsV0")
			if res is Array:
				_haven_residents = res.size()
		_a.log("HAVEN|projects=%d residents=%d" % [_construction_projects, _haven_residents])


func _probe_narrative_depth() -> void:
	if _bridge.has_method("GetRevelationStateV0"):
		var rev = _bridge.call("GetRevelationStateV0")
		if rev is Dictionary:
			_revelation_stage = int(rev.get("stage", 0))
			_a.log("NARRATIVE|revelation_stage=%d" % _revelation_stage)
	if _bridge.has_method("GetDiscoveredDataLogsV0"):
		var logs = _bridge.call("GetDiscoveredDataLogsV0")
		if logs is Array:
			_data_logs_found = logs.size()
	if _bridge.has_method("GetCommunionRepV0"):
		var cr = _bridge.call("GetCommunionRepV0")
		if cr is Dictionary:
			_communion_rep = float(cr.get("reputation", 0.0))
	if _bridge.has_method("GetFOCompetenceTierV0"):
		var fc = _bridge.call("GetFOCompetenceTierV0")
		if fc is Dictionary:
			_fo_competence_tier = int(fc.get("tier", 0))
	# Discovery phases summary (GetDiscoveryPhaseMarkersV0 takes no params)
	if _bridge.has_method("GetDiscoveryPhaseMarkersV0"):
		var phases = _bridge.call("GetDiscoveryPhaseMarkersV0")
		if phases is Array:
			for p in phases:
				if p is Dictionary:
					var ph: String = str(p.get("phase", ""))
					_discovery_phases[ph] = _discovery_phases.get(ph, 0) + 1
	# Fracture access (requires fleetId + nodeId)
	if _bridge.has_method("GetFractureAccessV0"):
		var ps_loc: Dictionary = _bridge.call("GetPlayerStateV0")
		var fa_node: String = str(ps_loc.get("current_node_id", ""))
		if not fa_node.is_empty():
			var fa = _bridge.call("GetFractureAccessV0", "fleet_trader_1", fa_node)
			if fa is Dictionary:
				_fracture_accessible = bool(fa.get("accessible", false))
	_a.log("NARRATIVE|data_logs=%d communion=%.1f fo_tier=%d discovery_phases=%s fracture=%s" % [
		_data_logs_found, _communion_rep, _fo_competence_tier,
		str(_discovery_phases), str(_fracture_accessible)])
	_a.log("NARRATIVE|npcs_met=%d" % _narrative_npcs_met)


func _probe_combat_depth() -> void:
	_a.log("COMBAT_DEPTH|heat_max=%.1f doctrine=%s weapons=%d" % [
		_heat_max, str(_doctrine_set), _weapons_equipped])
	# Drones
	if _bridge.has_method("GetDroneActivityV0"):
		var drones = _bridge.call("GetDroneActivityV0")
		if drones is Array:
			_drones_active = drones.size()
			_a.log("COMBAT_DEPTH|drones=%d" % _drones_active)
	# Battle stations (no params)
	if _bridge.has_method("GetBattleStationsStateV0"):
		var bs = _bridge.call("GetBattleStationsStateV0")
		if bs is Dictionary:
			_a.log("COMBAT_DEPTH|battle_stations=%s" % str(bs))


func _probe_visual() -> void:
	# Scene census (works both headless and visual)
	var npc_count := get_nodes_in_group("FleetShip").size()
	var station_count := get_nodes_in_group("Station").size()
	_a.log("VISUAL|npcs=%d stations=%d" % [npc_count, station_count])
	# Module slots
	if _bridge.has_method("GetPlayerFleetSlotsV0"):
		var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
		var empty := 0
		for s in slots:
			if s is Dictionary and str(s.get("installed_module_id", "")).is_empty():
				empty += 1
		_a.log("VISUAL|module_slots=%d empty=%d" % [slots.size(), empty])
	# Available modules
	if _bridge.has_method("GetAvailableModulesV0"):
		var mods: Array = _bridge.call("GetAvailableModulesV0")
		_a.log("VISUAL|available_modules=%d" % mods.size())
	# Tech tree
	if _bridge.has_method("GetTechTreeV0"):
		var tree: Array = _bridge.call("GetTechTreeV0")
		var unlocked := 0
		for t in tree:
			if t is Dictionary and bool(t.get("unlocked", false)):
				unlocked += 1
		_a.log("VISUAL|techs=%d unlocked=%d" % [tree.size(), unlocked])

	# ---- Feel presence probe (visual mode only) ----
	_probe_feel_presence()


# ---- Dimension 21: Feel Presence (runtime VFX/feedback verification) ----
# Checks whether visual feedback systems exist and have correct node structure.
# Also reports which feedback was observed DURING gameplay via inline sampling.
func _probe_feel_presence() -> void:
	var root_node := get_root()
	if root_node == null:
		_a.log("FEEL_PRESENCE|root=null")
		return

	# --- HUD node existence checks ---
	var hud = root_node.find_child("HUD", true, false)
	var hud_found := hud != null
	var damage_flash_exists := false
	var credits_flash_exists := false
	var combat_vignette_exists := false
	var combat_banner_exists := false
	var hull_bar_exists := false

	if hud:
		# Check for DamageFlash node (ColorRect child of HUD)
		var df = hud.find_child("DamageFlash", true, false)
		damage_flash_exists = df != null
		# Check for CreditsFlash node
		var cf = hud.find_child("CreditsFlash", true, false)
		credits_flash_exists = cf != null
		# Check combat vignette - it's a dynamically created ColorRect
		var cv = hud.find_child("CombatVignette", false, false)
		if cv == null:
			# May be stored as a variable, try to detect by checking children
			for child in hud.get_children():
				if child is ColorRect and child.name.begins_with("Combat"):
					cv = child
					break
		combat_vignette_exists = cv != null
		# Check combat banner
		for child in hud.get_children():
			if child is Label and child.name.contains("Combat"):
				combat_banner_exists = true
				break
		# Hull bar
		var hb = hud.find_child("HullBar", true, false)
		if hb == null:
			hb = hud.find_child("hull_bar", true, false)
		hull_bar_exists = hb != null

	# --- Galaxy map player marker check ---
	var galaxy_marker_exists := false
	var galaxy_view = root_node.find_child("GalaxyView", true, false)
	if galaxy_view == null:
		galaxy_view = root_node.find_child("galaxy_view", true, false)
	if galaxy_view:
		# PlayerRing is a MeshInstance3D child of the player's node root
		var pr = galaxy_view.find_child("PlayerRing", true, false)
		galaxy_marker_exists = pr != null
		if not galaxy_marker_exists:
			# Check scene tree groups
			var rings = get_nodes_in_group("PlayerMarker")
			galaxy_marker_exists = rings.size() > 0

	# --- Camera shake infrastructure ---
	var camera_has_shake := false
	var cam = root_node.find_child("PlayerFollowCamera", true, false)
	if cam == null:
		cam = root_node.find_child("player_follow_camera", true, false)
	if cam and cam.has_method("apply_shake"):
		camera_has_shake = true

	# --- Toast system ---
	var toast_mgr = root_node.get_node_or_null("ToastManager")
	var toast_system_exists := toast_mgr != null

	# --- Report infrastructure existence ---
	var infra_count := 0
	var infra_checks := {
		"hud": hud_found,
		"damage_flash": damage_flash_exists,
		"credits_flash": credits_flash_exists,
		"combat_vignette": combat_vignette_exists,
		"combat_banner": combat_banner_exists,
		"hull_bar": hull_bar_exists,
		"galaxy_marker": galaxy_marker_exists,
		"camera_shake": camera_has_shake,
		"toast_system": toast_system_exists,
	}
	for key in infra_checks:
		if infra_checks[key]:
			infra_count += 1
	_a.log("FEEL_INFRA|found=%d/%d" % [infra_count, infra_checks.size()])
	for key in infra_checks:
		_a.log("FEEL_INFRA|%s=%s" % [key, str(infra_checks[key])])

	# --- Report runtime observations (from inline sampling during gameplay) ---
	var runtime_count := 0
	var runtime_checks := {
		"credits_flash_seen": _feel_credits_flash_seen,
		"damage_flash_seen": _feel_damage_flash_seen,
		"combat_vignette_seen": _feel_combat_vignette_seen,
		"combat_banner_seen": _feel_combat_banner_seen,
		"toast_seen": _feel_toast_seen,
		"galaxy_marker_seen": _feel_galaxy_marker_seen,
		"shake_max": _feel_shake_intensity_max,
	}
	for key in runtime_checks:
		var val = runtime_checks[key]
		if val is bool and val:
			runtime_count += 1
		elif val is float and val > 0.0:
			runtime_count += 1
		_a.log("FEEL_RUNTIME|%s=%s" % [key, str(val)])
	_a.log("FEEL_RUNTIME|observed=%d/%d checks_run=%d" % [runtime_count, 7, _feel_checks_run])

	# --- Issue detection ---
	if not _is_visual:
		_a.log("FEEL_PRESENCE|mode=headless (skipping runtime VFX checks)")
		return

	# In visual mode, missing infrastructure is a real bug
	if not damage_flash_exists:
		_a.log("FEEL_ISSUE|MAJOR|damage_flash_missing|DamageFlash node not found in HUD")
	if not credits_flash_exists:
		_a.log("FEEL_ISSUE|MAJOR|credits_flash_missing|CreditsFlash node not found in HUD")
	if not combat_vignette_exists:
		_a.log("FEEL_ISSUE|MINOR|combat_vignette_missing|Combat vignette node not found in HUD")
	if not toast_system_exists:
		_a.log("FEEL_ISSUE|MINOR|toast_system_missing|ToastManager not found")
	if _total_combats > 0 and not _feel_damage_flash_seen and damage_flash_exists:
		_a.log("FEEL_ISSUE|MAJOR|damage_flash_never_activated|%d combats but damage flash never triggered" % _total_combats)
	if _total_sells > 0 and not _feel_credits_flash_seen and credits_flash_exists:
		_a.log("FEEL_ISSUE|MAJOR|credits_flash_never_activated|%d sells but credits flash never triggered" % _total_sells)
	if _total_combats > 0 and not _feel_combat_vignette_seen and combat_vignette_exists:
		_a.log("FEEL_ISSUE|MINOR|combat_vignette_never_seen|%d combats but vignette never observed active" % _total_combats)


# Inline feel sampling — call during key gameplay moments to detect active VFX.
func _sample_feel_state() -> void:
	_feel_checks_run += 1
	var root_node := get_root()
	if root_node == null:
		return

	# Sample HUD feedback state
	var hud = root_node.find_child("HUD", true, false)
	if hud:
		# Damage flash: check if ColorRect child has alpha > 0
		var df = hud.find_child("DamageFlash", true, false)
		if df and df is CanvasItem and df.visible:
			if df is ColorRect and df.color.a > 0.01:
				_feel_damage_flash_seen = true

		# Credits flash
		var cf = hud.find_child("CreditsFlash", true, false)
		if cf and cf is CanvasItem and cf.visible:
			if cf is ColorRect and cf.color.a > 0.01:
				_feel_credits_flash_seen = true

		# Combat vignette
		for child in hud.get_children():
			if child is ColorRect and child.name.contains("Combat") and child.visible:
				_feel_combat_vignette_seen = true
				break

		# Combat banner
		for child in hud.get_children():
			if child is Label and child.name.contains("Combat") and child.visible and child.text.length() > 0:
				_feel_combat_banner_seen = true
				break

	# Sample toast system
	var toast_mgr = root_node.get_node_or_null("ToastManager")
	if toast_mgr:
		for child in toast_mgr.get_children():
			if child is CanvasItem and child.visible:
				_feel_toast_seen = true
				break

	# Galaxy map marker (only when galaxy overlay is open)
	var galaxy_view = root_node.find_child("GalaxyView", true, false)
	if galaxy_view:
		var pr = galaxy_view.find_child("PlayerRing", true, false)
		if pr and pr is Node3D and pr.visible:
			_feel_galaxy_marker_seen = true


func _compute_avg_ppt() -> int:
	if _profit_per_trade.size() == 0:
		return 0
	var total := 0
	for p in _profit_per_trade:
		total += p
	return total / _profit_per_trade.size()


func _avg_slice(arr: Array, from_idx: int, to_idx: int) -> float:
	var total := 0.0
	var count := 0
	for i in range(from_idx, mini(to_idx, arr.size())):
		total += float(arr[i])
		count += 1
	return total / maxi(count, 1)


# ---- Issue Analysis Engine ----
# Applies thresholds to all metrics and produces a ranked issue list.
# Each issue: {severity, category, description, prescription, file}
# Severity: CRITICAL (blocks fun), MAJOR (hurts experience), MINOR (polish)

func _analyze_issues(
	net_profit: int, end_credits: int, idle_pct: float, curve_shape: String,
	entropy: float, max_gap: int, longest_streak: int, grind_score: float,
	route_repeats: int, good_repeats: int, visit_pct: float, max_depth: int,
	backtrack_pct: float, fo_max_silence: int, mean_gap: float) -> Array:

	var issues: Array = []

	# ---- COMBAT ----
	if _total_combats > 0 and _total_kills == 0:
		issues.append({
			"severity": "CRITICAL", "category": "COMBAT",
			"description": "Player wins 0/%d combats — combat is unwinnable in the first hour" % _total_combats,
			"prescription": "Reduce early pirate HP/damage or grant starter weapon module. Consider auto-equipping a basic weapon on game start",
			"file": "SimCore/Content/ShipClassContentV0.cs OR SimCore/Systems/NpcFleetCombatSystem.cs"
		})
	elif _total_combats > 0:
		var kill_rate := float(_total_kills) / float(_total_combats)
		if kill_rate < 0.2:
			issues.append({
				"severity": "MAJOR", "category": "COMBAT",
				"description": "Kill rate %.0f%% — player rarely wins combat" % (kill_rate * 100),
				"prescription": "Reduce early pirate difficulty or improve starter loadout",
				"file": "SimCore/Tweaks/CombatTweaksV0.cs"
			})
	if _total_combats == 0 and COMBAT_ENABLED:
		issues.append({
			"severity": "MAJOR", "category": "COMBAT",
			"description": "Zero combats in first hour — no hostile encounters spawned or reached",
			"prescription": "Check pirate spawn density near starting area",
			"file": "SimCore/Gen/GalaxyGenerator.cs"
		})
	# Hull never threatened
	var hull_min: int = _combat_hull_mins.min() if _combat_hull_mins.size() > 0 else 100
	if hull_min >= 80 and _total_combats > 5:
		issues.append({
			"severity": "MINOR", "category": "COMBAT",
			"description": "Hull never drops below %d%% — combat has no tension" % hull_min,
			"prescription": "Increase pirate damage or reduce starter shields to create tension moments",
			"file": "SimCore/Tweaks/CombatTweaksV0.cs"
		})

	# ---- ECONOMY ----
	if curve_shape == "EXPONENTIAL":
		issues.append({
			"severity": "MAJOR", "category": "ECONOMY",
			"description": "Credit curve is EXPONENTIAL — growth accelerates without bound, no money sinks",
			"prescription": "Add upkeep costs, fuel costs, repair fees, or market saturation to create sinks. Ideal curve is SIGMOID (fast early, plateau late)",
			"file": "SimCore/Tweaks/FleetUpkeepTweaksV0.cs OR SimCore/Systems/MarketSystem.cs"
		})
	if curve_shape == "FLAT_OR_DECLINING":
		issues.append({
			"severity": "CRITICAL", "category": "ECONOMY",
			"description": "Credit curve is flat/declining — player makes no money",
			"prescription": "Check starting area market spreads, ensure profitable trades exist within 2 hops of start",
			"file": "SimCore/Gen/MarketInitGen.cs"
		})
	var growth_pct: int = (net_profit * 100) / maxi(_start_credits, 1)
	if growth_pct > 50000:
		issues.append({
			"severity": "MAJOR", "category": "ECONOMY",
			"description": "Credit growth %d%% — money is trivially easy, removes all economic tension" % growth_pct,
			"prescription": "Introduce money sinks (docking fees, fuel, repairs, insurance) or reduce trade margins",
			"file": "SimCore/Tweaks/MarketTweaksV0.cs"
		})
	elif growth_pct < 100:
		issues.append({
			"severity": "MAJOR", "category": "ECONOMY",
			"description": "Credit growth only %d%% — player doesn't feel progression" % growth_pct,
			"prescription": "Increase starting area trade margins or add early-game bonus trades",
			"file": "SimCore/Gen/MarketInitGen.cs"
		})
	if idle_pct > 10.0:
		issues.append({
			"severity": "CRITICAL", "category": "ECONOMY",
			"description": "Idle rate %.1f%% — player has nothing profitable to do" % idle_pct,
			"prescription": "Add more trade routes, missions, or exploration incentives near idle locations",
			"file": "SimCore/Gen/MarketInitGen.cs OR SimCore/Systems/MissionSystem.cs"
		})
	var issue_implicit_sinks := 0
	for i in range(1, _credit_trajectory.size()):
		var traj_diff: int = _credit_trajectory[i] - _credit_trajectory[i - 1]
		if traj_diff < 0:
			issue_implicit_sinks += absi(traj_diff)
	var issue_sink_faucet := float(_total_spent + issue_implicit_sinks) / maxf(float(_total_earned), 1.0)
	if issue_sink_faucet < 0.05 and _total_earned > 1000:
		issues.append({
			"severity": "MAJOR", "category": "ECONOMY",
			"description": "Sink/faucet ratio %.2f — almost no money leaving the economy" % issue_sink_faucet,
			"prescription": "Add meaningful costs: fuel, repairs, upkeep, insurance, docking fees",
			"file": "SimCore/Tweaks/FleetUpkeepTweaksV0.cs"
		})

	# ---- PACING ----
	if max_gap > 50:
		issues.append({
			"severity": "CRITICAL", "category": "PACING",
			"description": "Reward desert of %d decisions — player goes too long without positive feedback" % max_gap,
			"prescription": "Add micro-rewards (discovery pings, FO commentary, lore fragments) in dead zones",
			"file": "SimCore/Systems/AdaptationFragmentSystem.cs OR scripts/bridge/SimBridge.Story.cs"
		})
	elif max_gap > 30:
		issues.append({
			"severity": "MAJOR", "category": "PACING",
			"description": "Max reward gap %d decisions — approaching boredom threshold" % max_gap,
			"prescription": "Add ambient rewards (scan results, NPC encounters, market alerts) to fill gaps",
			"file": "SimCore/Systems/AdaptationFragmentSystem.cs"
		})
	if entropy < 1.0:
		issues.append({
			"severity": "MAJOR", "category": "PACING",
			"description": "Action entropy %.2f — player is doing the same thing repeatedly" % entropy,
			"prescription": "Incentivize variety: bonus XP for trying new activities, FO suggestions for unexplored systems",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})
	if longest_streak > 15:
		issues.append({
			"severity": "MAJOR", "category": "PACING",
			"description": "Monotonous streak of %d identical actions — gameplay feels repetitive" % longest_streak,
			"prescription": "Break monotony with interrupts: random events, NPC encounters, FO dialogue triggers",
			"file": "SimCore/Systems/MissionTemplateSystem.cs"
		})

	# ---- GRIND ----
	if grind_score > 0.15:
		issues.append({
			"severity": "MAJOR", "category": "GRIND",
			"description": "Grind score %.2f — player repeating buy-travel-sell-travel loops" % grind_score,
			"prescription": "Add diminishing returns on repeated routes, increase exploration incentives",
			"file": "SimCore/Systems/MarketSystem.cs"
		})
	if route_repeats > 30:
		issues.append({
			"severity": "MAJOR", "category": "GRIND",
			"description": "Same route traveled %d times — player found one route and grinds it" % route_repeats,
			"prescription": "Market price adjustment on high-volume routes, or new opportunities appearing elsewhere",
			"file": "SimCore/Systems/MarketSystem.cs"
		})
	if good_repeats > 10 and _goods_bought.size() >= 3:
		issues.append({
			"severity": "MINOR", "category": "GRIND",
			"description": "Same good traded %d times — not exploring trade variety" % good_repeats,
			"prescription": "Add trade variety bonuses or make repeated trades less profitable over time",
			"file": "SimCore/Systems/MarketSystem.cs"
		})

	# ---- EXPLORATION ----
	if visit_pct < 30.0:
		issues.append({
			"severity": "MAJOR", "category": "EXPLORATION",
			"description": "Only %.0f%% of map explored — player stuck in small area" % visit_pct,
			"prescription": "Add exploration incentives: FO hints about distant systems, visible rewards on galaxy map",
			"file": "scripts/bridge/SimBridge.Story.cs OR scripts/ui/galaxy_map.gd"
		})
	if _factions_visited.size() < 2:
		issues.append({
			"severity": "MAJOR", "category": "EXPLORATION",
			"description": "Only %d faction(s) encountered — missing faction diversity" % _factions_visited.size(),
			"prescription": "Ensure 2+ factions within 3 hops of starting node",
			"file": "SimCore/Gen/GalaxyGenerator.cs"
		})
	if backtrack_pct > 70.0 and _visited.size() > 5:
		issues.append({
			"severity": "MINOR", "category": "EXPLORATION",
			"description": "Backtrack rate %.0f%% — player retreading old ground too much" % backtrack_pct,
			"prescription": "Add forward-exploration incentives: cheaper fuel on new routes, bonus for first visits",
			"file": "SimCore/Systems/MarketSystem.cs"
		})

	# ---- PROGRESSIVE DISCLOSURE ----
	if _systems_introduced.size() < 3:
		issues.append({
			"severity": "MAJOR", "category": "DISCLOSURE",
			"description": "Only %d game systems introduced — most systems never surfaced" % _systems_introduced.size(),
			"prescription": "Add pacing gates: introduce modules at 5 trades, missions at 3 nodes, tech at 10 trades",
			"file": "scripts/bridge/SimBridge.Story.cs OR SimCore/Systems/WinConditionSystem.cs"
		})
	if _systems_introduced.size() >= 3:
		# Check if disclosures are front-loaded
		if _system_intro_decisions.size() >= 3:
			var last_intro: int = int(_system_intro_decisions[-1])
			if last_intro < 20:
				issues.append({
					"severity": "MINOR", "category": "DISCLOSURE",
					"description": "All %d systems introduced by decision %d — too front-loaded" % [_systems_introduced.size(), last_intro],
					"prescription": "Space out system introductions across the first hour (every ~100 decisions)",
					"file": "scripts/bridge/SimBridge.Story.cs"
				})

	# ---- FIRST OFFICER ----
	if not _fo_promoted:
		issues.append({
			"severity": "MAJOR", "category": "FO",
			"description": "First Officer not promoted — player missing narrative companion",
			"prescription": "Ensure FO selection prompt triggers early (before first trade)",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})
	if fo_max_silence > 150 and _fo_promoted:
		issues.append({
			"severity": "MAJOR", "category": "FO",
			"description": "FO silent for %d decisions — companion feels absent" % fo_max_silence,
			"prescription": "Add FO ambient commentary: market tips, combat warnings, exploration encouragement. Target: speak every 30-50 decisions",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})
	elif fo_max_silence > 80 and _fo_promoted:
		issues.append({
			"severity": "MINOR", "category": "FO",
			"description": "FO silence gap %d decisions — could be more talkative" % fo_max_silence,
			"prescription": "Add situational FO lines for: entering new territory, price changes, low fuel, idle periods",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})
	if _fo_dialogue_count < 5 and _fo_promoted:
		issues.append({
			"severity": "MAJOR", "category": "FO",
			"description": "FO only spoke %d times in entire first hour — too quiet" % _fo_dialogue_count,
			"prescription": "FO should comment on major milestones, first trades, first combat, new systems",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})

	# ---- STORY/PROGRESSION ----
	if _knowledge_entries == 0:
		issues.append({
			"severity": "MINOR", "category": "STORY",
			"description": "Zero knowledge entries — lore/world-building not surfacing",
			"prescription": "Add knowledge unlocks at key milestones (first dock, first faction, first combat)",
			"file": "SimCore/Systems/KnowledgeSystem.cs"
		})
	if _discoveries_found == 0 and _visited.size() >= 5:
		issues.append({
			"severity": "MINOR", "category": "STORY",
			"description": "No discoveries found despite visiting %d nodes" % _visited.size(),
			"prescription": "Add early-game discoverable objects (abandoned ships, data caches) near starting area",
			"file": "SimCore/Gen/DiscoverySeedGen.cs"
		})
	if _fragments_found == 0 and _visited.size() >= 10:
		issues.append({
			"severity": "MINOR", "category": "STORY",
			"description": "No adaptation fragments found — precursor storyline not starting",
			"prescription": "Seed at least 1 fragment within 3 hops of start",
			"file": "SimCore/Systems/AdaptationFragmentSystem.cs"
		})

	# ---- MISSIONS ----
	if _missions_available == 0 and _decision >= 200:
		var desc := "Zero missions available after %d decisions — mission system invisible" % _decision
		if _missions_attempted > 0:
			desc += " (bot tried %d times, none accepted)" % _missions_attempted
		issues.append({
			"severity": "MAJOR", "category": "MISSIONS",
			"description": desc,
			"prescription": "Ensure missions appear at stations within 3 hops of start by decision 100",
			"file": "SimCore/Systems/MissionSystem.cs OR SimCore/Gen/GalaxyGenerator.cs"
		})
	if _mission_first_seen > 300:
		issues.append({
			"severity": "MINOR", "category": "MISSIONS",
			"description": "First mission not seen until decision %d — too late for first hour" % _mission_first_seen,
			"prescription": "Seed starter missions earlier; player should see missions by decision 50-100",
			"file": "SimCore/Systems/MissionSystem.cs"
		})
	if _bounties_available == 0 and _total_combats > 3:
		issues.append({
			"severity": "MINOR", "category": "MISSIONS",
			"description": "No bounties available despite %d combats — missing combat-mission loop" % _total_combats,
			"prescription": "Generate bounty contracts for pirate-heavy systems",
			"file": "SimCore/Systems/MissionSystem.cs"
		})

	# ---- FLEET & LOADOUT ----
	if _modules_installed == 0 and _decision >= 300:
		var desc := "Zero modules installed after %d decisions — ship customization not surfaced" % _decision
		if _modules_attempted > 0 and _modules_installed_by_bot == 0:
			desc += " (bot tried %d installs, all failed — no modules available?)" % _modules_attempted
		elif _modules_installed_by_bot > 0:
			desc = ""  # Bot installed a module but observer didn't pick it up — skip issue
		if not desc.is_empty():
			issues.append({
				"severity": "MAJOR", "category": "FLEET",
				"description": desc,
				"prescription": "Grant a starter module or make modules available at first dock",
				"file": "SimCore/Content/ShipClassContentV0.cs OR scripts/bridge/SimBridge.Refit.cs"
			})
	if _weapons_equipped == 0 and _total_combats > 0:
		issues.append({
			"severity": "MAJOR", "category": "FLEET",
			"description": "No weapons equipped despite %d combats — player fights unarmed" % _total_combats,
			"prescription": "Auto-equip a basic weapon on game start or grant one after first combat",
			"file": "SimCore/Content/ShipClassContentV0.cs"
		})
	if _techs_unlocked == 0 and _decision >= 400:
		var desc := "No techs unlocked after %d decisions — research not surfacing" % _decision
		if _research_attempted and not _research_started_by_bot:
			desc += " (bot tried to start research but failed — no available techs?)"
		elif _research_started_by_bot:
			desc += " (bot started research but no tech completed in time)"
		issues.append({
			"severity": "MINOR", "category": "FLEET",
			"description": desc,
			"prescription": "Prompt player to start research at a station after 10+ trades",
			"file": "SimCore/Systems/ResearchSystem.cs"
		})

	# ---- SECURITY & TENSION ----
	if _threat_bands_seen.size() <= 1 and _visited.size() >= 5:
		issues.append({
			"severity": "MINOR", "category": "SECURITY",
			"description": "Only %d threat band(s) encountered — no safety gradient" % _threat_bands_seen.size(),
			"prescription": "Ensure galaxy has safe/moderate/dangerous zones within first 5 systems",
			"file": "SimCore/Gen/GalaxyGenerator.cs"
		})
	if _max_war_intensity > 0.7 and _decision < 200:
		issues.append({
			"severity": "MAJOR", "category": "SECURITY",
			"description": "War intensity %.1f near start — player thrown into high-threat zone too early" % _max_war_intensity,
			"prescription": "Ensure starting area has low war intensity; ramp up with distance from home",
			"file": "SimCore/Systems/WarfrontSystem.cs"
		})

	# ---- HAVEN ----
	if not _haven_discovered and _visited.size() >= 8:
		issues.append({
			"severity": "MINOR", "category": "HAVEN",
			"description": "Haven not discovered after visiting %d nodes — base-building not surfaced" % _visited.size(),
			"prescription": "Ensure Haven is discoverable within 4-5 hops of start",
			"file": "SimCore/Gen/GalaxyGenerator.cs OR SimCore/Systems/HavenSystem.cs"
		})

	# ---- NARRATIVE DEPTH ----
	if _data_logs_found == 0 and _visited.size() >= 5:
		issues.append({
			"severity": "MINOR", "category": "NARRATIVE",
			"description": "No data logs found in %d visited nodes — world feels empty" % _visited.size(),
			"prescription": "Seed data logs at stations; at least 1 per 3 systems",
			"file": "SimCore/Gen/DiscoverySeedGen.cs"
		})
	if _narrative_npcs_met == 0 and _visited.size() >= 5:
		issues.append({
			"severity": "MINOR", "category": "NARRATIVE",
			"description": "No narrative NPCs encountered — universe feels unpopulated",
			"prescription": "Place narrative NPCs (quest givers, lore characters) at key stations",
			"file": "SimCore/Content/NarrativeContentV0.cs"
		})
	if _revelation_stage == 0 and _decision >= 500:
		issues.append({
			"severity": "MINOR", "category": "NARRATIVE",
			"description": "Revelation stage 0 after %d decisions — main story not progressing" % _decision,
			"prescription": "Trigger first revelation earlier; player should feel story pull by mid-first-hour",
			"file": "SimCore/Systems/RevelationSystem.cs"
		})

	# ---- MARKET INTELLIGENCE ----
	if _market_alerts_seen == 0 and _total_buys + _total_sells >= 10:
		issues.append({
			"severity": "MINOR", "category": "MARKET_INTEL",
			"description": "No market alerts after %d trades — market intelligence not surfacing" % (_total_buys + _total_sells),
			"prescription": "Generate price alerts when spreads change or opportunities appear",
			"file": "SimCore/Systems/MarketSystem.cs OR scripts/bridge/SimBridge.Market.cs"
		})
	if _supply_shocks_seen == 0 and _decision >= 400:
		issues.append({
			"severity": "MINOR", "category": "MARKET_INTEL",
			"description": "No supply shocks seen — economy feels static",
			"prescription": "Add supply disruptions, price spikes, or shortage events for dynamism",
			"file": "SimCore/Systems/MarketSystem.cs"
		})

	# ---- PROGRESSION ----
	if _milestones_unlocked == 0 and _decision >= 300:
		issues.append({
			"severity": "MAJOR", "category": "PROGRESSION",
			"description": "Zero milestones unlocked after %d decisions — no sense of advancement" % _decision,
			"prescription": "Add milestone achievements for first trade, first combat, first faction, exploration distance",
			"file": "SimCore/Systems/MilestoneSystem.cs"
		})
	var avg_ppt_check := 0
	if _profit_per_trade.size() > 0:
		var ppt_t := 0
		for p in _profit_per_trade:
			ppt_t += p
		avg_ppt_check = ppt_t / _profit_per_trade.size()
	if avg_ppt_check < 10 and _profit_per_trade.size() >= 5:
		issues.append({
			"severity": "MAJOR", "category": "PROGRESSION",
			"description": "Average profit per trade only %d cr — trades feel unrewarding" % avg_ppt_check,
			"prescription": "Increase market spread near starting area or reduce tariffs for early trades",
			"file": "SimCore/Gen/MarketInitGen.cs OR SimCore/Tweaks/MarketTweaksV0.cs"
		})

	# ---- COMBAT DEPTH ----
	if _total_combats >= 3 and not _doctrine_set and not _doctrine_set_by_bot:
		var desc := "No combat doctrine set after %d combats — tactical layer not surfacing" % _total_combats
		if _doctrine_attempted:
			desc += " (bot tried to set doctrine but call failed)"
		issues.append({
			"severity": "MINOR", "category": "COMBAT_DEPTH",
			"description": desc,
			"prescription": "Prompt player to set doctrine after first combat; auto-set a default",
			"file": "SimCore/Systems/DoctrineSystem.cs"
		})

	# Sort: CRITICAL first, then MAJOR, then MINOR
	var severity_order := {"CRITICAL": 0, "MAJOR": 1, "MINOR": 2}
	issues.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return severity_order.get(a.get("severity", "MINOR"), 2) < severity_order.get(b.get("severity", "MINOR"), 2))

	return issues


func _write_json_report(issues: Array, net_profit: int, end_credits: int,
	idle_pct: float, curve_shape: String, entropy: float, max_gap: int,
	longest_streak: int, grind_score: float, route_repeats: int,
	good_repeats: int, visit_pct: float, max_depth: int,
	backtrack_pct: float, fo_max_silence: int) -> void:

	var hull_min: int = _combat_hull_mins.min() if _combat_hull_mins.size() > 0 else 100
	var kill_rate := 0.0
	if _total_combats > 0:
		kill_rate = float(_total_kills) / float(_total_combats)

	var report := {
		"version": "experience_bot_v0",
		"seed": _user_seed,
		"archetype": _archetype,
		"decisions": _decision,
		"ticks": _decision * TICK_ADVANCE,
		"metrics": {
			"economy": {
				"start_credits": _start_credits,
				"end_credits": end_credits,
				"net_profit": net_profit,
				"growth_pct": (net_profit * 100) / maxi(_start_credits, 1),
				"buys": _total_buys,
				"sells": _total_sells,
				"spent": _total_spent,
				"earned": _total_earned,
				"idle_pct": idle_pct,
				"curve_shape": curve_shape,
				"goods_bought": _goods_bought.size(),
				"goods_sold": _goods_sold.size(),
				"sink_faucet_ratio": float(_total_spent) / maxf(float(_total_earned), 1.0),
			},
			"pacing": {
				"reward_count": _reward_events.size(),
				"mean_gap": snapped(float(max_gap) if _reward_events.size() < 2 else float(_reward_events[-1] - _reward_events[0]) / maxf(float(_reward_events.size() - 1), 1.0), 0.1),
				"max_gap": max_gap,
				"entropy": snapped(entropy, 0.01),
				"longest_streak": longest_streak,
			},
			"combat": {
				"total": _total_combats,
				"kills": _total_kills,
				"kill_rate": snapped(kill_rate, 0.01),
				"hull_min_pct": hull_min,
			},
			"exploration": {
				"visited": _visited.size(),
				"total_nodes": _all_nodes.size(),
				"visit_pct": snapped(visit_pct, 0.1),
				"factions": _factions_visited.size(),
				"max_depth": max_depth,
				"backtrack_pct": snapped(backtrack_pct, 0.1),
				"discoveries": _discoveries_found,
				"fragments": _fragments_found,
				"knowledge": _knowledge_entries,
			},
			"grind": {
				"score": snapped(grind_score, 0.01),
				"route_repeats": route_repeats,
				"good_repeats": good_repeats,
			},
			"fo": {
				"promoted": _fo_promoted,
				"dialogue_lines": _fo_dialogue_count,
				"max_silence": fo_max_silence,
			},
			"disclosure": {
				"systems_introduced": _systems_introduced.size(),
				"system_list": _systems_introduced,
			},
			"progression": {
				"milestones_unlocked": _milestones_unlocked,
				"endgame_paths_revealed": _endgame_paths_revealed,
				"avg_profit_per_trade": _compute_avg_ppt(),
				"best_single_profit": _best_single_profit,
				"trades_profitable": _trades_profitable,
			},
			"market_intel": {
				"alerts": _market_alerts_seen,
				"supply_shocks": _supply_shocks_seen,
				"price_spreads_count": _price_spreads.size(),
			},
			"missions": {
				"available_peak": _missions_available,
				"accepted": _missions_accepted,
				"bounties_available": _bounties_available,
				"commissions": _commissions_active,
				"first_seen_decision": _mission_first_seen,
			},
			"fleet": {
				"modules_installed": _modules_installed,
				"modules_empty": _modules_empty,
				"weapons_equipped": _weapons_equipped,
				"techs_unlocked": _techs_unlocked,
				"techs_total": _techs_total,
			},
			"security": {
				"threat_bands_seen": _threat_bands_seen.size(),
				"band_distribution": _threat_bands_seen,
				"max_war_intensity": snapped(_max_war_intensity, 0.01),
				"ambush_lanes": _lane_ambush_count,
			},
			"haven": {
				"discovered": _haven_discovered,
				"construction_projects": _construction_projects,
				"residents": _haven_residents,
			},
			"narrative": {
				"revelation_stage": _revelation_stage,
				"data_logs_found": _data_logs_found,
				"narrative_npcs_met": _narrative_npcs_met,
				"communion_rep": snapped(_communion_rep, 0.01),
				"fo_competence_tier": _fo_competence_tier,
				"fracture_accessible": _fracture_accessible,
				"discovery_phases": _discovery_phases,
			},
			"combat_depth": {
				"heat_max": snapped(_heat_max, 0.1),
				"doctrine_set": _doctrine_set,
				"weapons_equipped": _weapons_equipped,
				"drones_active": _drones_active,
			},
			"feel_presence": {
				"credits_flash_seen": _feel_credits_flash_seen,
				"damage_flash_seen": _feel_damage_flash_seen,
				"combat_vignette_seen": _feel_combat_vignette_seen,
				"combat_banner_seen": _feel_combat_banner_seen,
				"toast_seen": _feel_toast_seen,
				"galaxy_marker_seen": _feel_galaxy_marker_seen,
				"shake_intensity_max": snapped(_feel_shake_intensity_max, 0.01),
				"checks_run": _feel_checks_run,
			},
			"system_engagement": {
				"missions_attempted": _missions_attempted,
				"missions_accepted": _missions_accepted_by_bot,
				"modules_attempted": _modules_attempted,
				"modules_installed": _modules_installed_by_bot,
				"doctrine_attempted": _doctrine_attempted,
				"doctrine_set": _doctrine_set_by_bot,
				"research_attempted": _research_attempted,
				"research_started": _research_started_by_bot,
				"discoveries_scanned": _discoveries_scanned,
				"fragments_collected": _fragments_collected,
				"automation_attempted": _automation_attempted,
				"automation_created": _automation_created,
			},
		},
		"issues": [],
		"milestones": [],
		"credit_trajectory": _credit_trajectory,
		"pass": _a.exit_code() == 0,
	}

	# Serialize issues
	for issue in issues:
		report["issues"].append({
			"severity": str(issue.get("severity", "")),
			"category": str(issue.get("category", "")),
			"description": str(issue.get("description", "")),
			"prescription": str(issue.get("prescription", "")),
			"file": str(issue.get("file", "")),
		})

	# Serialize milestones
	for m in _milestone_log:
		report["milestones"].append({
			"decision": int(m.get("decision", 0)),
			"name": str(m.get("name", "")),
			"detail": str(m.get("detail", "")),
		})

	# Write JSON to reports directory
	var report_path := "res://reports/experience/%s/seed_%d/report.json" % [_archetype, _user_seed]
	var json_str := JSON.stringify(report, "  ")
	var f := FileAccess.open(report_path, FileAccess.WRITE)
	if f:
		f.store_string(json_str)
		f.close()
		_a.log("REPORT_JSON|%s" % report_path)
	else:
		_a.log("REPORT_JSON|FAILED|%s" % report_path)
