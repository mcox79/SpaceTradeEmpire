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
const SLOW_MODE_DELAY := 90    # ~1.5s at 60fps between decisions (simulates human pace)

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
var _decision_counter: int = 0  # increments on each meaningful action (buy/sell/travel/dock)

# CLI params
var _user_seed := -1
var _archetype := "balanced"
var _slow_mode := false
var _slow_wait := 0

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
var _fo_dialogue_log: Array = []        # transcript: [{decision, text, speaker}]
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

# ---- Tracking: reward cadence (NEW — industry gap #1) ----
var _last_reward_decision := 0        # decision of most recent reward
var _reward_gaps: Array = []          # gap lengths between consecutive rewards
var _max_reward_gap := 0              # worst drought

# ---- Tracking: credit velocity (NEW — industry gap #3) ----
var _credit_velocity_samples: Array = []  # [{decision, velocity}] — rolling cr/decision
var _credit_velocity_window := 20     # rolling window size in decisions
var _credit_stall_count := 0          # consecutive zero-velocity windows
var _credit_stall_max := 0            # worst stall streak
var _credit_death_spiral_count := 0   # consecutive negative-velocity windows
var _credit_death_spiral_max := 0     # worst death spiral streak

# ---- Tracking: hull timeline (NEW — industry gap #5) ----
var _hull_timeline: Array = []        # [{decision, hull_pct}] — sampled every 50 decisions
var _hull_below_50_count := 0         # times hull sampled below 50%
var _hull_below_20_count := 0         # times hull sampled below 20%
var _hull_never_threatened := true    # stays true if hull never < 90%

# ---- Tracking: margin trend (NEW — industry gap #6) ----
var _margin_trend: Array = []         # per-trade margin (cr/unit) in chronological order
var _declining_margin_streaks := 0    # count of 3+ consecutive declining margins
# _worst_single_loss already declared above in "economy depth" section
var _worst_loss_pct := 0.0            # worst loss as % of credits at time

# ---- Tracking: FO reactivity (NEW — industry gap #4) ----
var _fo_react_pending_event := ""     # event type waiting for FO response
var _fo_react_event_decision := 0     # decision when event fired
var _fo_react_latencies: Array = []   # [{event, latency}] — decisions between event and FO speech
var _fo_react_timeouts := 0           # events where FO never responded within 30 decisions

# ---- Tracking: post-combat loot (NEW — industry gap #2) ----
var _combat_loot_count := 0           # combats that yielded any reward
var _combat_no_loot_count := 0        # combats with zero reward
var _combat_loot_values: Array = []   # salvage amounts per combat

# ---- Tracking: dock system dump (NEW — industry gap #7) ----
var _dock_new_systems: Array = []     # count of NEW systems revealed per dock visit
var _dock_system_dump_count := 0      # docks where 3+ new systems appeared at once
var _last_dock_tab_count := -1        # tabs visible at previous dock (-1 = not yet docked)

# ---- Tracking: combat round log (NEW — uses GetLastCombatLogV0) ----
var _combat_ttk_ratios: Array = []    # time-to-kill ratios per combat
var _shield_break_rounds: Array = []  # round number when enemy shield broke
var _combat_round_counts: Array = []  # total rounds per combat
var _combat_dmg_variance: Array = []  # damage variance per combat

# ---- Tracking: sell rejections (market instability awareness) ----
var _sell_rejections_instability := 0 # sells skipped due to market closure
var _sell_rejections_log: Array = []  # [{decision, node, good}]

# ---- Tracking: automation effectiveness aggregation ----
var _automation_total_earned := 0     # total credits earned by all programs
var _automation_total_expense := 0    # total expense across all programs
var _automation_total_cycles := 0     # total cycles run across all programs

# ---- Tracking: credit history (NEW — uses GetCreditHistoryV0) ----
var _credit_history_available := false  # bridge method exists
var _credit_velocity_from_bridge: Array = []  # velocity samples from bridge

# ---- Tracking: FO adaptation (NEW — uses GetFOAdaptationLogV0) ----
var _fo_adaptation_events := 0        # FO adaptation events observed
var _fo_dialogue_token_ids: Dictionary = {}  # token_id -> count for repetition detection
var _fo_dialogue_repeats := 0         # same token within 100 decisions
var _fo_last_new_token_decision := 0  # decision when last NEW unique token appeared
var _fo_content_exhausted_at := -1    # decision where no new token in 100+ decisions

# ---- Tracking: economy growth rate windows (convergence analysis gap) ----
var _growth_rate_windows: Array = []  # [{window, start_credits, end_credits, rate}] per 50-decision window
var _economy_acceleration := 0.0      # second derivative: are growth rates increasing or decreasing?
var _economy_growth_trend := "UNKNOWN" # ACCELERATING, DECELERATING, STEADY, VOLATILE

# ---- Tracking: route diversity timeline (convergence analysis gap) ----
var _route_diversity_windows: Array = []  # [{window, unique_routes, total_trades, diversity}] per 100d
var _diversity_collapse_decision := -1    # decision where diversity first drops below 0.3

# ---- Tracking: streak detail (convergence analysis gap) ----
var _longest_streak_type := ""        # action type of the longest streak (e.g., "TRAVEL")
var _longest_streak_start := 0        # decision where the longest streak started

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

# ---- Tracking: cognitive load (NEW — research gap #1) ----
var _tabs_at_first_dock := -1        # tab count at very first dock (-1 = not yet docked)
var _tabs_per_dock: Array = []       # tab counts at each dock visit
var _systems_per_dock: Array = []    # new systems introduced at each dock
var _fo_word_counts: Array = []      # word count per FO dialogue message

# ---- Tracking: confusion / dead-end detection (NEW — research gap #2) ----
var _trap_state_count := 0           # times all of: credits<min_buy, cargo=0, no missions
var _trap_state_decisions: Array = []  # decisions where trap state detected
var _min_viable_actions := 999       # minimum viable actions observed at any decision
var _action_reversals := 0           # buy then sell same good at same station within 10 decisions

# ---- Tracking: retention prediction (NEW — research gap #3) ----
var _first_profit_decision := -1     # decision of first net-positive sell
var _core_loop_decision := -1        # decision of first complete buy->warp->sell
var _aha_moment_decision := -1       # decision of first trade with margin > 100cr
var _action_rate_windows: Array = [] # actions per 50-decision window
var _action_rate_declining := false  # true if last 4+ windows are declining

# ---- Tracking: pacing rhythm (NEW — research gap #4) ----
var _high_event_decisions: Array = []  # decisions where HIGH intensity events occurred
var _medium_event_decisions: Array = [] # decisions where MEDIUM events occurred
var _beat_intervals: Array = []      # gaps between consecutive HIGH/MEDIUM events

# ---- Tracking: valence-arousal (NEW — research gap #5) ----
var _valence_samples: Array = []     # [{decision, valence, arousal}] per significant event
var _valence_running_avg := 0.0      # exponentially weighted moving average
var _valence_crossings := 0          # times running avg crossed zero
var _catharsis_count := 0            # high-negative followed by positive within 30d
var _wonder_count := 0               # high-positive spikes
var _last_negative_arousal_d := -1   # decision of last HIGH+negative event (for catharsis detection)

# ---- Tracking: competence / mastery (NEW — research gap #6) ----
var _early_margins: Array = []       # first 10 trade margins
var _late_margins: Array = []        # last 10 trade margins
var _early_kill_rate := 0.0          # kill rate in first 3 combats
var _late_kill_rate := 0.0           # kill rate in last 3 combats
var _milestone_toasts := 0           # visible milestone acknowledgments
var _credits_per_decision_early := 0.0  # credits earned per decision (first 200)
var _credits_per_decision_late := 0.0   # credits earned per decision (last 200)

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
var _automations_created: int = 0              # total automation programs created
var _automation_types_created: Dictionary = {} # type_name -> true

# ---- Tracking: deep system engagement (fracture, planet scan, construction, haven, megaproject, programs) ----
var _fracture_travels: int = 0
var _fracture_travel_cooldown: int = 0         # decisions until next attempt
var _planet_scans_performed: int = 0
var _planet_scan_cooldown: int = 0             # decisions until next attempt
var _constructions_started: int = 0
var _haven_upgraded := false
var _fabrications_started: int = 0
var _megaprojects_started: int = 0
var _program_monitoring_count: int = 0

# ---- Utility AI state (Strategy E: utility-based decisions with diminishing returns) ----
var _route_counts: Dictionary = {}      # "buy>sell>good" -> execution count
var _node_visit_counts: Dictionary = {} # node_id -> total arrivals
var _committed_dest := ""               # node we're traveling toward
var _committed_reason := ""             # "buy_route", "sell_route", "explore", "roam"
var _committed_good := ""               # good_id for trade route commitment
var _committed_sell_node := ""          # sell destination for a buy commitment
var _committed_buy_node := ""           # buy node (for route key tracking)
var _utility_last_scores: Array = []    # top-5 scored actions for diagnostics

# Utility weights (set per archetype in _apply_archetype)
var _uw_trade := 1.0       # weight for trade route actions
var _uw_explore := 1.0     # weight for exploration actions
var _uw_combat := 1.0      # weight for combat actions
var _uw_curiosity := 1.5   # multiplier for untried goods/unvisited nodes
var _uw_freshness_k := 0.3 # exponential decay rate for route repetition

# ---- Experience Checkpoints (10min / 30min / 60min) ----
# Captures a scored snapshot of the player experience at three time marks.
# Each checkpoint scores 5 dimensions 1-5: Orientation, Mastery, Engagement, Feel, Progression.
const CHECKPOINT_10MIN := 120   # ~600 ticks = ~10 game-minutes
const CHECKPOINT_30MIN := 360   # ~1800 ticks = ~30 game-minutes
const CHECKPOINT_60MIN := 720   # ~3600 ticks = ~60 game-minutes (end of run)
var _checkpoints: Array = []    # [{phase, decision, scores, metrics, issues}]
var _checkpoint_captured: Dictionary = {}  # "10min" -> true (prevent duplicates)

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
	_a.log("START|experience_bot_v0 archetype=%s seed=%d visual=%s slow=%s" % [_archetype, _user_seed, str(_is_visual), str(_slow_mode)])


func _parse_args() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_user_seed = int(arg.trim_prefix("--seed="))
		if arg.begins_with("--archetype="):
			var a := arg.trim_prefix("--archetype=").to_lower()
			if a in ["balanced", "trader", "explorer", "fighter"]:
				_archetype = a
		if arg == "--slow":
			_slow_mode = true


func _apply_archetype() -> void:
	match _archetype:
		"trader":
			EXPLORE_EVERY_N = 8
			COMBAT_COOLDOWN = 10
			COMBAT_ENABLED = false
			_uw_trade = 1.5
			_uw_explore = 0.6
			_uw_combat = 0.3
			_uw_curiosity = 1.2
			_uw_freshness_k = 0.2  # slower decay — optimizer repeats good routes longer
		"explorer":
			EXPLORE_EVERY_N = 2
			COMBAT_COOLDOWN = 5
			COMBAT_ENABLED = true
			_uw_trade = 0.7
			_uw_explore = 1.8
			_uw_combat = 1.0
			_uw_curiosity = 2.5   # high curiosity — strongly prefers untried things
			_uw_freshness_k = 0.5 # fast decay — avoids repetition aggressively
		"fighter":
			EXPLORE_EVERY_N = 6
			COMBAT_COOLDOWN = 2
			COMBAT_ENABLED = true
			_uw_trade = 0.8
			_uw_explore = 0.8
			_uw_combat = 2.0
			_uw_curiosity = 1.3
			_uw_freshness_k = 0.3
		_:  # balanced
			EXPLORE_EVERY_N = 4
			COMBAT_COOLDOWN = 5
			COMBAT_ENABLED = true
			_uw_trade = 1.0
			_uw_explore = 1.0
			_uw_combat = 1.0
			_uw_curiosity = 1.5
			_uw_freshness_k = 0.3


func _process(_delta: float) -> bool:
	_total_frames += 1
	# FPS profiling — sample every frame, before busy check
	if _a:
		_a.fps_sample(_delta)
	# Hard safety timeout — MUST run even when _busy (await may have hung)
	var _frame_limit := 108000 if _slow_mode else 12000  # slow: ~30min, fast: ~200s
	var _force_limit := _frame_limit + 3000
	if _total_frames > _frame_limit and _phase != Phase.DONE:
		if _busy and _total_frames > _force_limit:
			# _busy stuck for 3000+ extra frames — force quit to prevent hang
			_a.log("TIMEOUT_FORCE_QUIT|frame=%d|phase=%d|busy=true" % [_total_frames, _phase])
			_busy = false
			if _bridge and _bridge.has_method("StopSimV0"):
				_bridge.call("StopSimV0")
			quit(1)
			return true
		elif not _busy:
			_a.log("TIMEOUT|frame=%d" % _total_frames)
			_phase = Phase.REPORT
			_a.fps_set_phase("REPORT")
	if _busy:
		return false
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
			_a.fps_set_phase("REPORT")


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
		_a.fps_set_phase("PLAY_LOOP")
	else:
		_polls += 1
		if _polls > MAX_POLLS:
			_a.hard(false, "bridge_ready")
			_phase = Phase.REPORT
			_a.fps_set_phase("REPORT")


# ===================== Main Play Loop =====================

func _do_play_loop() -> void:
	# Slow mode: simulate human thinking time between decisions
	if _slow_mode and _slow_wait > 0:
		_slow_wait -= 1
		return

	if _decision >= MAX_DECISIONS:
		# Power moment: try installing a module before leaving the play loop
		_try_power_moment()
		if _is_visual:
			_phase = Phase.VISUAL_TOUR
			_tour_step = 0
			_tour_settle = 0
			_a.fps_set_phase("VISUAL_TOUR")
		else:
			_phase = Phase.REPORT
			_a.fps_set_phase("REPORT")
		return

	_decision += 1

	# Slow mode: schedule delay for next decision (randomized 1-3s at 60fps)
	if _slow_mode:
		_slow_wait = SLOW_MODE_DELAY + randi() % 90  # 90-180 frames = 1.5-3s

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

	# ---- Credit velocity (rolling window, only when we have fresh samples) ----
	if _decision % SAMPLE_INTERVAL == 0:
		_track_credit_velocity()

	# ---- Hull timeline sampling (every 50 decisions) ----
	if _decision % 50 == 0:
		_sample_hull_timeline()

	# ---- FO reactivity tracking ----
	_track_fo_reactivity()

	# ---- Reward cadence tracking ----
	_track_reward_cadence()

	# ---- Margin trend analysis (every sell) ----
	# (tracked inline in _bot_do_sell)

	# ---- Experience checkpoints (10min / 30min) ----
	if _decision == CHECKPOINT_10MIN:
		_capture_checkpoint("10min")
		if _is_visual:
			_try_capture("checkpoint_10min")
	elif _decision == CHECKPOINT_30MIN:
		_capture_checkpoint("30min")
		if _is_visual:
			_try_capture("checkpoint_30min")

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

	# ---- Trap state detection (research gap #2) ----
	# Check if player has ANY viable action
	var viable_actions := 0
	if credits > 0: viable_actions += 1  # can buy something
	if cargo > 0: viable_actions += 1    # can sell something
	if _missions_available > 0: viable_actions += 1  # can do mission
	if viable_actions < _min_viable_actions:
		_min_viable_actions = viable_actions
	if credits <= 0 and cargo <= 0 and _missions_available <= 0 and _decision > 10:
		_trap_state_count += 1
		_trap_state_decisions.append(_decision)


func _probe_live_systems() -> void:
	# Runs every SAMPLE_INTERVAL decisions — polls bridge for live system state
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))

	# -- Missions --
	# GetMissionListV0 returns only available missions (no status field needed)
	if _bridge.has_method("GetMissionListV0"):
		var missions = _bridge.call("GetMissionListV0")
		if missions is Array:
			var avail: int = missions.size()
			if avail > _missions_available:
				_missions_available = avail
			if avail > 0 and _mission_first_seen < 0:
				_mission_first_seen = _decision
	# Check for active mission separately
	if _bridge.has_method("GetActiveMissionV0"):
		var active_m = _bridge.call("GetActiveMissionV0")
		if active_m is Dictionary and not str(active_m.get("mission_id", "")).is_empty():
			if _missions_accepted == 0:
				_missions_accepted = 1
	if _bridge.has_method("GetAvailableBountiesV0"):
		var bounties = _bridge.call("GetAvailableBountiesV0")
		if bounties is Array and bounties.size() > _bounties_available:
			_bounties_available = bounties.size()
	if _bridge.has_method("GetActiveCommissionV0"):
		var comm = _bridge.call("GetActiveCommissionV0")
		if comm is Dictionary and not str(comm.get("id", "")).is_empty():
			_commissions_active += 1

	# -- Security/tension --
	if not loc.is_empty() and _bridge.has_method("GetNodeSecurityBandV0"):
		var band: String = str(_bridge.call("GetNodeSecurityBandV0", loc))
		if not band.is_empty():
			_threat_bands_seen[band] = _threat_bands_seen.get(band, 0) + 1
			_security_samples.append({"node_id": loc, "band": band})
	if _bridge.has_method("GetWarfrontsV0"):
		var wfs = _bridge.call("GetWarfrontsV0")
		if wfs is Array:
			for wf in wfs:
				if wf is Dictionary:
					var war_i: float = float(wf.get("intensity", 0.0))
					if war_i > _max_war_intensity:
						_max_war_intensity = war_i

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

	# -- Market intelligence --
	if _bridge.has_method("GetMarketAlertsV0"):
		var alerts = _bridge.call("GetMarketAlertsV0", 10)
		if alerts is Array:
			_market_alerts_seen = maxi(_market_alerts_seen, alerts.size())
	if _bridge.has_method("GetSupplyShockSummaryV0"):
		var shocks = _bridge.call("GetSupplyShockSummaryV0")
		if shocks is Dictionary:
			_supply_shocks_seen = maxi(_supply_shocks_seen, int(shocks.get("disrupted_count", 0)))

	# -- Credit history (bridge-powered, gap #8) --
	_probe_credit_history()

	# -- FO adaptation log (bridge-powered, gap #10) --
	_probe_fo_adaptation()

	# -- FO content exhaustion detection (token tracking done in _track_fo) --
	if _fo_content_exhausted_at < 0 and _fo_last_new_token_decision > 0:
		if _decision - _fo_last_new_token_decision > 100 and _fo_dialogue_token_ids.size() > 3:
			_fo_content_exhausted_at = _decision
			_a.log("FO|d=%d CONTENT_EXHAUSTED unique_tokens=%d last_new_at=%d" % [
				_decision, _fo_dialogue_token_ids.size(), _fo_last_new_token_decision])

	# -- Automation engagement timing (gap #23) --
	if not _event_decisions.has("first_automation") and _automation_created:
		_event_decisions["first_automation"] = _decision

	# -- Save/load round-trip (gap #21 — periodic, every 200 decisions) --
	if _decision > 0 and _decision % 200 == 0 and not _event_decisions.has("save_load_tested"):
		if _bridge.has_method("RequestSave") and _bridge.has_method("RequestLoad"):
			var pre_credits := int(_bridge.call("GetPlayerStateV0").get("credits", 0))
			_bridge.call("RequestSave")
			# Brief settle for save
			_bridge.call("RequestLoad")
			var post_credits := int(_bridge.call("GetPlayerStateV0").get("credits", 0))
			if pre_credits == post_credits:
				_event_decisions["save_load_tested"] = _decision
				_a.log("SAVE_LOAD|d=%d PASS credits=%d" % [_decision, pre_credits])
			else:
				_a.flag("SAVE_LOAD_MISMATCH|pre=%d post=%d" % [pre_credits, post_credits])

	# -- Camera event tracking (gap #19 — visual mode) --
	# Tracked via _sample_feel_state which already checks for camera nodes

	# -- Visit history (gap #13 — exploration motivation tagging) --
	# Tag visits by reason: already done inline in travel action logging


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
		_record_reward("FO_DIALOGUE")
		_a.event(_decision_counter, "COMPANION_FO_LINE", "lines=%d" % lines)
		if not _event_decisions.has("first_fo_line"):
			_event_decisions["first_fo_line"] = _decision
		# Valence: FO dialogue = medium positive
		_record_valence_event("fo_dialogue", _decision)
		# Capture FO dialogue text for LLM quality analysis
		# Use pending_text from GetFirstOfficerStateV0 (non-consuming read) to avoid
		# mutating sim state — consuming dialogue shifts FO trigger timing and causes
		# run-to-run variance on the same seed.
		var text := str(fo.get("pending_text", ""))
		if not text.is_empty():
			_fo_dialogue_log.append({
				"decision": _decision,
				"text": text,
				"speaker": "FO",
			})
			# Track word count for cognitive load analysis
			var word_count := text.split(" ").size()
			_fo_word_counts.append(word_count)
			# Track unique text for content exhaustion detection
			var token_id: String = str(text.hash())
			if not _fo_dialogue_token_ids.has(token_id):
				_fo_dialogue_token_ids[token_id] = 1
				_fo_last_new_token_decision = _decision
			else:
				_fo_dialogue_token_ids[token_id] = int(_fo_dialogue_token_ids[token_id]) + 1
				_fo_dialogue_repeats += 1
			# Truncate text for log line (max 80 chars)
			var short_text: String = text.substr(0, 80) + ("..." if text.length() > 80 else "")
			_a.log("FO_LINE|d=%d|speaker=FO|text=%s" % [_decision, short_text])


func _track_credit_velocity() -> void:
	if _credit_trajectory.size() < 2:
		return
	# Compute velocity over last N samples (each sample = SAMPLE_INTERVAL decisions)
	var window := mini(_credit_velocity_window, _credit_trajectory.size())
	if window < 2:
		return
	var recent_start: int = _credit_trajectory[_credit_trajectory.size() - window]
	var recent_end: int = _credit_trajectory[_credit_trajectory.size() - 1]
	var velocity := float(recent_end - recent_start) / float(window)
	_credit_velocity_samples.append({"decision": _decision, "velocity": velocity})
	# Detect stalls and death spirals
	if absf(velocity) < 1.0:
		_credit_stall_count += 1
		if _credit_stall_count > _credit_stall_max:
			_credit_stall_max = _credit_stall_count
	else:
		_credit_stall_count = 0
	if velocity < -5.0:
		_credit_death_spiral_count += 1
		if _credit_death_spiral_count > _credit_death_spiral_max:
			_credit_death_spiral_max = _credit_death_spiral_count
	else:
		_credit_death_spiral_count = 0


func _sample_hull_timeline() -> void:
	var hull_pct := 100
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull := int(hp.get("hull", 100))
		var hull_max := int(hp.get("hull_max", 100))
		if hull_max > 0:
			hull_pct = (hull * 100) / hull_max
	_hull_timeline.append({"decision": _decision, "hull_pct": hull_pct})
	if hull_pct < 90:
		_hull_never_threatened = false
	if hull_pct < 50:
		_hull_below_50_count += 1
	if hull_pct < 20:
		_hull_below_20_count += 1


func _track_fo_reactivity() -> void:
	# Check if FO responded to a pending event
	if not _fo_react_pending_event.is_empty():
		var latency := _decision - _fo_react_event_decision
		# Check if FO spoke since the event
		if _fo_dialogue_decisions.size() > 0 and _fo_dialogue_decisions[-1] >= _fo_react_event_decision:
			_fo_react_latencies.append({"event": _fo_react_pending_event, "latency": latency})
			_fo_react_pending_event = ""
		elif latency > 30:
			# FO never responded within 30 decisions
			_fo_react_timeouts += 1
			_fo_react_latencies.append({"event": _fo_react_pending_event, "latency": -1})
			_fo_react_pending_event = ""


func _track_reward_cadence() -> void:
	# Called every decision — check if a new reward was recorded since last check
	if _reward_events.size() > 0 and _reward_events[-1] == _decision:
		var gap := _decision - _last_reward_decision
		if _last_reward_decision > 0:
			_reward_gaps.append(gap)
			if gap > _max_reward_gap:
				_max_reward_gap = gap
		_last_reward_decision = _decision


func _set_fo_react_event(event_type: String) -> void:
	# Called after significant player actions to start FO reactivity timer
	if _fo_react_pending_event.is_empty() and _fo_promoted:
		_fo_react_pending_event = event_type
		_fo_react_event_decision = _decision


func _check_milestones() -> void:
	# Screenshot at key moments (visual mode only, one per milestone)
	if _total_buys == 1 and not _capture_points.has("first_buy"):
		_capture_points["first_buy"] = true
		_try_capture("02_first_buy")
		_log_milestone("FIRST_BUY", "decision=%d" % _decision)
		_introduce_system("market")
		_record_valence_event("new_station_dock", _decision)
		_milestone_toasts += 1
		if not _event_decisions.has("first_buy"):
			_event_decisions["first_buy"] = _decision
	if _total_sells == 1 and not _capture_points.has("first_sell"):
		_capture_points["first_sell"] = true
		_try_capture("03_first_sell")
		_log_milestone("FIRST_SELL", "decision=%d" % _decision)
		_introduce_system("trade")
		_record_reward("FIRST_PROFIT")
		_record_valence_event("first_profit", _decision)
		_milestone_toasts += 1
		if not _event_decisions.has("first_sell"):
			_event_decisions["first_sell"] = _decision
	if _total_combats == 1 and not _capture_points.has("first_combat"):
		_capture_points["first_combat"] = true
		_try_capture("04_first_combat")
		_log_milestone("FIRST_COMBAT", "decision=%d" % _decision)
		_introduce_system("combat")
		_record_reward("COMBAT_WIN")
		_record_valence_event("combat_victory", _decision)
		_milestone_toasts += 1
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


# ===================== Utility AI Decision Engine (Strategy E) =====================
# Replaces the greedy priority cascade with utility-scored action selection.
# Each candidate action is scored: utility = base_value * freshness * curiosity * phase_alignment * weight
# Freshness decays exponentially per route (buy>sell>good key), preventing repetitive trade loops.
# Curiosity rewards untried goods and unvisited nodes. Phase alignment shifts priorities over the session.

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

	# Track per-node visit frequency
	_node_visit_counts[loc] = _node_visit_counts.get(loc, 0) + 1

	# Non-blocking system engagement (missions, modules, doctrine, research)
	_bot_try_engage_systems(loc)

	# If committed to a multi-hop action, continue it
	if not _committed_dest.is_empty():
		if loc == _committed_dest:
			_execute_arrived_commitment(loc, cargo, cargo_count, credits)
			return
		else:
			var hop := _bot_get_next_hop(loc, _committed_dest)
			if not hop.is_empty():
				var atype := "EXPLORE" if _committed_reason == "explore" else "TRAVEL"
				_bot_do_travel(loc, hop, "committed %s toward %s" % [_committed_reason, _committed_dest])
				_action_type_history.append(atype)
				return
			_clear_commitment()  # unreachable, re-evaluate

	# Score all candidate actions and pick the best
	var candidates: Array = _score_all_actions(loc, credits, cargo, cargo_count)
	if candidates.is_empty():
		_consecutive_idles += 1
		_total_idles += 1
		if _consecutive_idles > _max_consecutive_idles:
			_max_consecutive_idles = _consecutive_idles
		_record_action(_decision, "IDLE", loc, "", 0, "no candidates")
		_action_type_history.append("IDLE")
		return

	# Sort descending by utility
	candidates.sort_custom(func(a, b): return a.utility > b.utility)
	_utility_last_scores = candidates.slice(0, mini(5, candidates.size()))

	# Periodic diagnostic logging
	var best: Dictionary = candidates[0]
	if _decision % 50 == 0:
		_a.log("UTIL|d=%d top=%s u=%.1f fresh=%.2f curiosity=%.1f phase=%.1f n=%d" % [
			_decision, best.get("type", "?"), best.utility,
			best.get("freshness", 1.0), best.get("curiosity", 1.0),
			best.get("phase_align", 1.0), candidates.size()])

	# Execute the winning action
	_execute_best_action(best, loc, credits, cargo, cargo_count)


# ---- Utility scoring ----

func _score_all_actions(loc: String, credits: int, cargo: Array, cargo_count: int) -> Array:
	var candidates: Array = []
	var pa := _get_phase_alignment()

	if cargo_count == 0:
		_score_trade_routes(loc, credits, candidates, pa)
	if cargo_count > 0 and cargo.size() > 0:
		_score_sell_options(loc, cargo, candidates, pa)
	if COMBAT_ENABLED and _combat_cooldown_remaining <= 0:
		_score_combat(loc, candidates, pa)
	_score_exploration(loc, candidates, pa)
	_score_roam(loc, candidates, pa)
	return candidates


func _get_phase_alignment() -> Dictionary:
	var progress := float(_decision) / float(MAX_DECISIONS)
	if progress < 0.33:
		return {trade = 1.3, explore = 1.5, combat = 0.7, sell = 1.3, roam = 1.0}
	elif progress < 0.67:
		return {trade = 1.0, explore = 1.0, combat = 1.3, sell = 1.0, roam = 0.8}
	else:
		return {trade = 0.8, explore = 0.7, combat = 1.5, sell = 0.8, roam = 0.5}


func _score_trade_routes(loc: String, credits: int, candidates: Array, pa: Dictionary) -> void:
	# Build global best sell prices
	var best_sells: Dictionary = {}
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		# Skip markets closed by instability
		if _bridge.has_method("GetInstabilityEffectsV0"):
			var inst = _bridge.call("GetInstabilityEffectsV0", nid)
			if inst is Dictionary and bool(inst.get("market_closed", false)):
				continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			var sp := int(entry.get("sell_price", 0))
			if gid.is_empty() or sp <= 0:
				continue
			if not best_sells.has(gid) or sp > int(best_sells[gid].price):
				best_sells[gid] = {price = sp, node_id = nid}

	# Trade saturation: hyperbolic decay so non-trade actions can compete over time
	var total_trades: float = float(_total_buys + _total_sells)
	var trade_saturation: float = 1.0 / (1.0 + 0.005 * total_trades)

	# Score each buyable good as a complete route (buy_node -> sell_node)
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid := str(entry.get("good_id", ""))
			var bp := int(entry.get("buy_price", 0))
			var avail := int(entry.get("quantity", 0))
			if gid.is_empty() or bp <= 0 or avail <= 0 or bp > credits:
				continue
			if not best_sells.has(gid):
				continue
			var sell_info: Dictionary = best_sells[gid]
			var sell_node := str(sell_info.node_id)
			if sell_node == nid:
				continue
			var profit := int(sell_info.price) - bp
			if profit <= 0:
				continue
			# Route freshness: decays with repeated use of this exact route
			var route_key := "%s>%s>%s" % [nid, sell_node, gid]
			var route_count: int = _route_counts.get(route_key, 0)
			var route_fresh: float = exp(-_uw_freshness_k * float(route_count))
			# Good freshness: gentle decay per good to prevent monopolizing one commodity
			var good_count: int = _good_trade_count.get(gid, 0)
			var good_fresh: float = exp(-0.05 * float(good_count))
			var freshness: float = route_fresh * good_fresh
			# Curiosity: bonus for goods never traded before
			var curiosity: float = _uw_curiosity if good_count == 0 else 1.0
			# pow(0.7) compresses dynamic range — prevents massive profits from drowning other actions
			var utility: float = pow(maxf(1.0, float(profit)), 0.7) * freshness * curiosity * float(pa.trade) * _uw_trade * trade_saturation
			candidates.append({
				type = "TRADE", utility = utility, buy_node = nid,
				sell_node = sell_node, good_id = gid, profit = profit,
				buy_price = bp, freshness = freshness, curiosity = curiosity,
				phase_align = pa.trade,
			})


func _score_sell_options(loc: String, cargo: Array, candidates: Array, pa: Dictionary) -> void:
	var trade_saturation: float = 1.0 / (1.0 + 0.005 * float(_total_buys + _total_sells))
	for item in cargo:
		var gid := str(item.get("good_id", ""))
		var qty := int(item.get("qty", 0))
		if gid.is_empty() or qty <= 0:
			continue
		# Find best sell nodes for this good
		for n in _all_nodes:
			var nid := str(n.get("node_id", ""))
			if nid.is_empty():
				continue
			# Skip markets closed by instability — avoids hammering closed markets
			if _bridge.has_method("GetInstabilityEffectsV0"):
				var inst = _bridge.call("GetInstabilityEffectsV0", nid)
				if inst is Dictionary and bool(inst.get("market_closed", false)):
					_sell_rejections_instability += 1
					continue
			var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
			for entry in mv:
				if str(entry.get("good_id", "")) != gid:
					continue
				var sp := int(entry.get("sell_price", 0))
				if sp <= 0:
					continue
				var route_key := "%s>%s>%s" % [
					_committed_buy_node if not _committed_buy_node.is_empty() else loc,
					nid, gid]
				var route_count: int = _route_counts.get(route_key, 0)
				var freshness: float = exp(-_uw_freshness_k * float(route_count))
				var utility: float = pow(maxf(1.0, float(sp * qty)), 0.7) * freshness * float(pa.sell) * _uw_trade * trade_saturation
				candidates.append({
					type = "SELL", utility = utility, sell_node = nid,
					good_id = gid, qty = qty, sell_price = sp,
					freshness = freshness, curiosity = 1.0, phase_align = pa.sell,
				})


func _score_combat(loc: String, candidates: Array, pa: Dictionary) -> void:
	if not _bridge.has_method("GetFleetTransitFactsV0"):
		return
	# Check current location for hostiles
	var fleets: Array = _bridge.call("GetFleetTransitFactsV0", loc)
	for f in fleets:
		if bool(f.get("is_hostile", false)):
			var fid := str(f.get("fleet_id", ""))
			if fid.is_empty():
				continue
			var base_value := 40.0
			var curiosity: float = 1.3 if _total_combats == 0 else 1.0
			var utility: float = base_value * curiosity * float(pa.combat) * _uw_combat
			candidates.append({
				type = "COMBAT", utility = utility, hostile_id = fid,
				freshness = 1.0, curiosity = curiosity, phase_align = pa.combat,
			})
			return  # found local hostile — no need to seek
	# Combat seeking: check adjacent nodes for hostiles
	if _adj.has(loc):
		for neighbor in _adj[loc]:
			var nfleets: Array = _bridge.call("GetFleetTransitFactsV0", str(neighbor))
			for f in nfleets:
				if bool(f.get("is_hostile", false)):
					var base_value := 30.0  # slightly lower than local combat
					var curiosity: float = 1.5 if _total_combats == 0 else 1.0
					var utility: float = base_value * curiosity * float(pa.combat) * _uw_combat
					candidates.append({
						type = "SEEK_COMBAT", utility = utility,
						target = str(neighbor),
						freshness = 1.0, curiosity = curiosity,
						phase_align = pa.combat,
					})
					return  # one seek target is enough


func _score_exploration(loc: String, candidates: Array, pa: Dictionary) -> void:
	# Priority 1: Unvisited nodes (high curiosity)
	var target := _bot_find_nearest_unvisited(loc)
	if not target.is_empty():
		var dist := _bfs_distance(loc, target)
		if dist < 999:
			var base_value := 50.0 / maxf(1.0, float(dist))
			var curiosity: float = 2.0
			var utility: float = base_value * curiosity * float(pa.explore) * _uw_explore
			candidates.append({
				type = "EXPLORE", utility = utility, target = target,
				distance = dist, freshness = 1.0, curiosity = curiosity,
				phase_align = pa.explore,
			})
		return  # unvisited nodes take priority over revisits

	# Priority 2: Revisit least-visited node (maintains exploration after all nodes discovered)
	# This prevents the bot from becoming a pure trade bot once the map is filled.
	var least_visited := ""
	var min_visits := 999999
	for n in _all_nodes:
		var nid := str(n.get("node_id", ""))
		if nid.is_empty() or nid == loc:
			continue
		var visits: int = _node_visit_counts.get(nid, 0)
		if visits < min_visits:
			min_visits = visits
			least_visited = nid
	if least_visited.is_empty():
		return
	var max_visits := 0
	for nid in _node_visit_counts:
		if int(_node_visit_counts[nid]) > max_visits:
			max_visits = int(_node_visit_counts[nid])
	# Visit imbalance drives exploration value: bigger gap = more reason to explore
	var imbalance: float = maxf(1.0, float(max_visits - min_visits))
	# Scale base with average trade profit so exploration stays competitive
	var avg_trade: float = float(_total_earned) / maxf(1.0, float(_total_sells))
	var dist := _bfs_distance(loc, least_visited)
	var base_value: float = (maxf(30.0, sqrt(avg_trade) * 0.8) + imbalance * 5.0) / maxf(1.0, float(dist))
	var curiosity: float = 1.3  # mild curiosity for revisits
	var utility: float = base_value * curiosity * float(pa.explore) * _uw_explore
	candidates.append({
		type = "EXPLORE", utility = utility, target = least_visited,
		distance = dist, freshness = 1.0, curiosity = curiosity,
		phase_align = pa.explore,
	})


func _score_roam(loc: String, candidates: Array, pa: Dictionary) -> void:
	var least := _bot_find_least_visited(loc)
	if least.is_empty() or least == loc:
		return
	var dist := _bfs_distance(loc, least)
	var base_value: float = 10.0 / maxf(1.0, float(dist))
	var utility: float = base_value * float(pa.roam) * _uw_trade
	candidates.append({
		type = "ROAM", utility = utility, target = least,
		distance = dist, freshness = 1.0, curiosity = 1.0,
		phase_align = pa.roam,
	})


# ---- Utility execution ----

func _execute_best_action(action: Dictionary, loc: String, credits: int, cargo: Array, cargo_count: int) -> void:
	match action.type:
		"TRADE":
			var buy_node: String = action.buy_node
			if buy_node == loc:
				# At buy node: buy and commit to sell destination
				var mv: Array = _bridge.call("GetPlayerMarketViewV0", loc)
				var avail := 0
				for entry in mv:
					if str(entry.get("good_id", "")) == action.good_id:
						avail = int(entry.get("quantity", 0))
						break
				var bp_val: int = int(action.buy_price)
				var affordable: int = credits / bp_val if bp_val > 0 else 0
				var qty := mini(mini(MAX_BUY_QTY, avail), affordable)
				if qty > 0:
					_bot_do_buy(loc, action.good_id, qty, credits)
					_action_type_history.append("BUY")
					_committed_dest = action.sell_node
					_committed_reason = "sell_route"
					_committed_good = action.good_id
					_committed_buy_node = loc
					_committed_sell_node = action.sell_node
					var rk := "%s>%s>%s" % [loc, action.sell_node, action.good_id]
					_route_counts[rk] = _route_counts.get(rk, 0) + 1
					return
			else:
				# Travel toward buy node
				var hop := _bot_get_next_hop(loc, buy_node)
				if not hop.is_empty():
					_committed_dest = buy_node
					_committed_reason = "buy_route"
					_committed_good = action.good_id
					_committed_sell_node = action.sell_node
					_committed_buy_node = buy_node
					_bot_do_travel(loc, hop, "utility trade %s profit=%d" % [action.good_id, action.profit])
					_action_type_history.append("TRAVEL")
					return

		"SELL":
			var sell_node: String = action.sell_node
			if sell_node == loc:
				_bot_do_sell(loc, action.good_id, action.qty)
				_action_type_history.append("SELL")
				_clear_commitment()
				return
			else:
				var hop := _bot_get_next_hop(loc, sell_node)
				if not hop.is_empty():
					_committed_dest = sell_node
					_committed_reason = "sell_route"
					_committed_good = action.good_id
					_bot_do_travel(loc, hop, "toward sell %s at %s" % [action.good_id, sell_node])
					_action_type_history.append("TRAVEL")
					return

		"COMBAT":
			if _bot_try_combat(loc):
				_action_type_history.append("COMBAT")
				return

		"SEEK_COMBAT":
			var hop := _bot_get_next_hop(loc, action.target)
			if not hop.is_empty():
				_committed_dest = action.target
				_committed_reason = "seek_combat"
				_bot_do_travel(loc, hop, "seeking combat at %s" % action.target)
				_action_type_history.append("TRAVEL")
				return

		"EXPLORE":
			var hop := _bot_get_next_hop(loc, action.target)
			if not hop.is_empty():
				_committed_dest = action.target
				_committed_reason = "explore"
				_bot_do_travel(loc, hop, "utility explore toward %s" % action.target)
				_action_type_history.append("EXPLORE")
				return

		"ROAM":
			var hop := _bot_get_next_hop(loc, action.target)
			if not hop.is_empty():
				_committed_dest = action.target
				_committed_reason = "roam"
				_bot_do_travel(loc, hop, "utility roam toward %s" % action.target)
				_action_type_history.append("TRAVEL")
				return

	# Fallthrough: couldn't execute
	_consecutive_idles += 1
	_total_idles += 1
	if _consecutive_idles > _max_consecutive_idles:
		_max_consecutive_idles = _consecutive_idles
	_record_action(_decision, "IDLE", loc, "", 0, "action failed")
	_action_type_history.append("IDLE")


func _execute_arrived_commitment(loc: String, cargo: Array, cargo_count: int, credits: int) -> void:
	match _committed_reason:
		"buy_route":
			var mv: Array = _bridge.call("GetPlayerMarketViewV0", loc)
			var avail := 0
			var bp := 0
			for entry in mv:
				if str(entry.get("good_id", "")) == _committed_good:
					avail = int(entry.get("quantity", 0))
					bp = int(entry.get("buy_price", 0))
					break
			var affordable: int = credits / bp if bp > 0 else 0
			var qty: int = mini(mini(MAX_BUY_QTY, avail), affordable)
			if qty > 0:
				_bot_do_buy(loc, _committed_good, qty, credits)
				_action_type_history.append("BUY")
				var rk := "%s>%s>%s" % [loc, _committed_sell_node, _committed_good]
				_route_counts[rk] = _route_counts.get(rk, 0) + 1
				_committed_dest = _committed_sell_node
				_committed_reason = "sell_route"
				_committed_buy_node = loc
			else:
				_clear_commitment()
				_record_action(_decision, "IDLE", loc, "", 0, "buy target unavailable")
				_action_type_history.append("IDLE")
				_total_idles += 1

		"sell_route":
			for item in cargo:
				var gid := str(item.get("good_id", ""))
				var qty := int(item.get("qty", 0))
				if not _committed_good.is_empty() and gid == _committed_good and qty > 0:
					_bot_do_sell(loc, gid, qty)
					_action_type_history.append("SELL")
					_clear_commitment()
					return
			# Committed good not in cargo — sell anything
			for item in cargo:
				var gid := str(item.get("good_id", ""))
				var qty := int(item.get("qty", 0))
				if qty > 0:
					_bot_do_sell(loc, gid, qty)
					_action_type_history.append("SELL")
					_clear_commitment()
					return
			_clear_commitment()

		"seek_combat":
			# Arrived at node with hostiles — try combat
			if _bot_try_combat(loc):
				_action_type_history.append("COMBAT")
			_clear_commitment()

		"explore":
			_visited[loc] = true
			_clear_commitment()

		"roam":
			_clear_commitment()

		_:
			_clear_commitment()


func _clear_commitment() -> void:
	_committed_dest = ""
	_committed_reason = ""
	_committed_good = ""
	_committed_sell_node = ""
	_committed_buy_node = ""


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
	# Set BattleReady before combat — StandDown (25% dmg) can't kill NPCs in 50 rounds.
	# SpinningUp→BattleReady tick transition not yet implemented; force directly.
	if _bridge.has_method("ForceBattleReadyV0"):
		_bridge.call("ForceBattleReadyV0")

	var result: Dictionary = _bridge.call("ResolveCombatV0", "fleet_trader_1", hostile_id)
	_total_combats += 1
	_combat_cooldown_remaining = COMBAT_COOLDOWN
	_decision_counter += 1
	_a.event(_decision_counter, "DANGER_COMBAT", "target=%s combats=%d" % [hostile_id, _total_combats])
	# Sample feel state during combat — vignette, damage flash, banner should be active.
	_sample_feel_state()
	_try_capture("combat_active_%d" % _total_combats)
	var outcome := str(result.get("outcome", "unknown"))
	var attacker_hull := int(result.get("attacker_hull", 0))
	var hull_max := int(result.get("attacker_hull_max", 100))
	var salvage := int(result.get("salvage", 0))
	var hull_pct := (attacker_hull * 100) / maxi(hull_max, 1)
	_combat_hull_mins.append(hull_pct)
	# Sample hull timeline immediately after combat — the 50-decision cadence
	# misses combat damage because SustainSystem auto-repairs between samples.
	_hull_timeline.append({"decision": _decision, "hull_pct": hull_pct})
	if hull_pct < 90:
		_hull_never_threatened = false
	if hull_pct < 50:
		_hull_below_50_count += 1
	if hull_pct < 20:
		_hull_below_20_count += 1
	_record_action(_decision, "COMBAT", loc, hostile_id, 0,
		"%s hull=%d%% salvage=%d" % [outcome, hull_pct, salvage])
	_a.log("COMBAT|d=%d target=%s outcome=%s hull=%d%% salvage=%d" % [
		_decision, hostile_id, outcome, hull_pct, salvage])
	# Track combat loot (industry gap #2)
	_combat_loot_values.append(salvage)
	# Record hull_damage as negative valence when hull drops below 80% — regardless
	# of win/loss. This enables catharsis detection (danger → relief) even when the
	# player wins every fight. Previously hull_damage was only recorded on losses,
	# so catharsis was always 0 when kill_rate=1.0.
	if hull_pct < 80:
		_record_valence_event("hull_damage", _decision)
	if outcome == "Victory":
		_total_kills += 1
		if salvage > 0:
			_record_reward("COMBAT_SALVAGE")
			_combat_loot_count += 1
		else:
			_combat_no_loot_count += 1
		_record_valence_event("combat_victory", _decision)
		# Competence: track early vs late kill rates
		if _total_combats <= 3:
			_early_kill_rate = float(_total_kills) / float(_total_combats)
		_late_kill_rate = float(_total_kills) / maxf(float(_total_combats), 1.0)
	else:
		_record_valence_event("hull_damage", _decision)
	# Track combat round details via GetLastCombatLogV0 (now returns full event array)
	_probe_combat_round_log()
	# Capture heat immediately after combat — heat resets outside combat
	if _bridge.has_method("GetHeatSnapshotV0"):
		var hs = _bridge.call("GetHeatSnapshotV0")
		if hs is Dictionary:
			var h: float = float(hs.get("current_heat", hs.get("heat_current", 0.0)))
			if h > _heat_max:
				_heat_max = h
	# Also capture rounds from ResolveCombatV0 result as backup
	var resolve_rounds := int(result.get("rounds", 0))
	if resolve_rounds > 0 and _combat_round_counts.size() == 0:
		_combat_round_counts.append(resolve_rounds)
	# FO reactivity: combat is a significant event
	_set_fo_react_event("combat")
	# Prevent player death in visual mode (game over screen blocks all UI)
	# We still record the hull damage for metrics, but repair immediately after
	if hull_pct <= 0 and _bridge.has_method("ForceRepairPlayerHullV0"):
		_bridge.call("ForceRepairPlayerHullV0")
	_consecutive_idles = 0
	return true


# ---- Action executors ----

func _bot_do_buy(loc: String, good_id: String, qty: int, credits_before: int) -> void:
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, true)
	# Sample feel state immediately after buy — credit flash should be active.
	_sample_feel_state()
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps.get("credits", 0))
	var succeeded := credits_after < credits_before
	_record_action(_decision, "BUY", loc, good_id, qty, "credits %d->%d" % [credits_before, credits_after], credits_after - credits_before)
	if succeeded:
		_total_buys += 1
		_trades_since_explore += 1
		var cost := credits_before - credits_after
		_total_spent += cost
		_goods_bought[good_id] = true
		_consecutive_idles = 0
		_good_trade_count[good_id] = _good_trade_count.get(good_id, 0) + 1
		_track_price(good_id, cost / maxi(1, qty))
		_decision_counter += 1
		_a.event(_decision_counter, "TRADE_EXECUTED", "type=BUY good=%s qty=%d cost=%d" % [good_id, qty, cost])
		if _total_buys == 1:
			_set_fo_react_event("first_buy")
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
	var sell_profit := credits_after - credits_before
	_record_action(_decision, "SELL", loc, good_id, qty, "credits %d->%d" % [credits_before, credits_after], sell_profit)
	if succeeded:
		_total_sells += 1
		_trades_since_explore += 1
		var profit := sell_profit
		_total_earned += profit
		_goods_sold[good_id] = true
		_consecutive_idles = 0
		_track_price(good_id, profit / maxi(1, qty))
		_decision_counter += 1
		_a.event(_decision_counter, "TRADE_EXECUTED", "type=SELL good=%s qty=%d profit=%d" % [good_id, qty, profit])
		if _total_sells == 1 and profit > 0:
			_a.event(_decision_counter, "HEIST_FIRST_SELL", "margin=%d" % profit)
		# Track per-trade profitability
		_profit_per_trade.append(profit)
		if profit > _best_single_profit:
			_best_single_profit = profit
		_trades_profitable += 1
		# Track margin trend (cr/unit)
		var margin_per_unit := profit / maxi(1, qty)
		_margin_trend.append(margin_per_unit)
		# Detect declining margin streaks
		if _margin_trend.size() >= 4:
			var last4 := _margin_trend.slice(-4)
			if last4[3] < last4[2] and last4[2] < last4[1] and last4[1] < last4[0]:
				_declining_margin_streaks += 1
		# FO reactivity: first sell is significant
		if _total_sells == 1:
			_set_fo_react_event("first_sell")
		if profit > 100:
			_record_reward("TRADE_PROFIT_%d" % profit)
		# Valence: profitable sell = positive
		_record_valence_event("profitable_sell", _decision)
		# Retention: track first profit, core loop, aha moment
		if _first_profit_decision < 0 and profit > 0:
			_first_profit_decision = _decision
		if _core_loop_decision < 0 and _total_buys >= 1 and _total_sells >= 1 and _total_travel_count >= 1:
			_core_loop_decision = _decision
		if _aha_moment_decision < 0 and margin_per_unit > 100:
			_aha_moment_decision = _decision
		# Competence: track early vs late margins
		if _profit_per_trade.size() <= 10:
			_early_margins.append(margin_per_unit)
		_late_margins.append(margin_per_unit)
		if _late_margins.size() > 10:
			_late_margins.pop_front()
	else:
		# Track failed/loss trades
		var loss := credits_before - credits_after
		if loss > 0:
			_trades_unprofitable += 1
			if loss > _worst_single_loss:
				_worst_single_loss = loss
			if credits_before > 0:
				var loss_pct := (float(loss) / float(credits_before)) * 100.0
				if loss_pct > _worst_loss_pct:
					_worst_loss_pct = loss_pct
			# Valence: loss trade = negative
			_record_valence_event("loss_trade", _decision)


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
		_decision_counter += 1
		var was_visited := _visited.has(to)
		_visited[to] = true
		_consecutive_idles = 0
		if was_visited:
			_backtrack_count += 1
		else:
			_a.event(_decision_counter, "NEW_NODE_VISITED", "node=%s total=%d" % [to, _visited.size()])
			_record_reward("NEW_NODE_%s" % to)
			if not _event_decisions.has("first_new_system"):
				_event_decisions["first_new_system"] = _decision
			# FO reactivity: arriving at a new system is significant
			_set_fo_react_event("new_system")
		_track_faction(to)
		_a.event(_decision_counter, "DOCK_ENTER", "node=%s" % to)
		# Dock system dump check: see how many new systems surface at this node
		_check_dock_system_dump(to)


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

	# 4. Start research after 4+ trades (lowered from 8 — missions reduce sell count)
	if _total_sells >= 4 and not _research_started_by_bot:
		_bot_try_start_research(loc)

	# 5. Scan discoveries at current node
	if _total_sells >= 2:
		_bot_try_scan_discoveries(loc)

	# 6. Collect adaptation fragments at current node
	_bot_try_collect_fragments(loc)

	# 7. Create automation programs at various thresholds
	if _total_sells >= 8:
		_bot_try_create_automation(loc)

	# 8. Accept a bounty (after 3+ combats)
	if _total_combats >= 3:
		_bot_try_accept_bounty()

	# 9. Discover haven (after 10+ nodes visited)
	if _visited.size() >= 10 and not _haven_discovered:
		_bot_try_discover_haven()

	# 10. Accept diplomacy proposals
	if _total_sells >= 10:
		_bot_try_accept_treaty()

	# 11. Planet scanning (after 5+ nodes visited)
	if _visited.size() >= 5:
		_bot_try_planet_scan(loc)

	# 12. Fracture travel (after 15+ nodes visited)
	if _visited.size() >= 15:
		_bot_try_fracture_travel(loc)

	# 13. Construction (after haven discovered)
	if _haven_discovered and _constructions_started == 0:
		_bot_try_start_construction()

	# 14. Haven upgrade + fabrication (after haven discovered and 100+ decisions)
	if _haven_discovered and _decision >= 100:
		_bot_try_haven_actions()

	# 15. Megaproject (after 200+ decisions and haven discovered)
	if _decision >= 200 and _haven_discovered:
		_bot_try_megaproject()

	# 16. Program monitoring (every 50 decisions after first automation)
	if _automations_created > 0 and _decision % 50 == 0:
		_bot_monitor_programs()


var _bounties_logged := false

func _bot_try_accept_bounty() -> void:
	# Bounties are auto-tracked (kill target fleet to claim). Log awareness once.
	if not _bridge.has_method("GetAvailableBountiesV0"):
		return
	var bounties = _bridge.call("GetAvailableBountiesV0")
	if not bounties is Array or bounties.size() == 0:
		return
	_introduce_system("bounties")
	if not _bounties_logged:
		_bounties_logged = true
		for b in bounties:
			if b is Dictionary:
				_a.log("ENGAGE|d=%d bounty available: target=%s reward=%d" % [
					_decision, str(b.get("target_fleet_id", "")), int(b.get("reward_credits", 0))])


func _bot_try_discover_haven() -> void:
	if not _bridge.has_method("ForceDiscoverHavenV0"):
		return
	_bridge.call("ForceDiscoverHavenV0")
	_haven_discovered = true
	_a.log("ENGAGE|d=%d forced haven discovery" % _decision)
	_introduce_system("haven")


func _bot_try_accept_treaty() -> void:
	if not _bridge.has_method("GetDiplomaticProposalsV0") or not _bridge.has_method("AcceptProposalV0"):
		return
	var proposals = _bridge.call("GetDiplomaticProposalsV0")
	if not proposals is Array:
		return
	for p in proposals:
		if not p is Dictionary:
			continue
		var pid: String = str(p.get("id", ""))
		if pid.is_empty():
			continue
		var accepted: bool = _bridge.call("AcceptProposalV0", pid)
		if accepted:
			_a.log("ENGAGE|d=%d accepted diplomatic proposal %s" % [_decision, pid])
			_introduce_system("diplomacy")
			return


func _bot_try_fracture_travel(loc: String) -> void:
	# Cooldown: attempt every ~50 decisions (5 engage cycles * 10 decision cooldown)
	if _fracture_travel_cooldown > 0:
		_fracture_travel_cooldown -= 1
		return
	_fracture_travel_cooldown = 5
	if not _bridge.has_method("GetFractureAccessV0") or not _bridge.has_method("DispatchFractureTravelV0"):
		return
	if loc.is_empty():
		return
	var access = _bridge.call("GetFractureAccessV0", "fleet_trader_1", loc)
	if not access is Dictionary:
		return
	if not bool(access.get("accessible", false)):
		return
	_bridge.call("DispatchFractureTravelV0", "fleet_trader_1", loc)
	_fracture_travels += 1
	_a.event(_decision, "FRACTURE_TRAVEL", "node=%s" % loc)
	_a.log("ENGAGE|d=%d fracture travel dispatched from %s" % [_decision, loc])
	_introduce_system("fracture_travel")


func _bot_try_planet_scan(loc: String) -> void:
	# Cooldown: attempt every ~20 decisions (2 engage cycles * 10 decision cooldown)
	if _planet_scan_cooldown > 0:
		_planet_scan_cooldown -= 1
		return
	_planet_scan_cooldown = 2
	if not _bridge.has_method("GetScanChargesV0") or not _bridge.has_method("OrbitalScanV0"):
		return
	if loc.is_empty():
		return
	var charges = _bridge.call("GetScanChargesV0")
	if not charges is Dictionary:
		return
	var current_charges: int = int(charges.get("current", 0))
	if current_charges <= 0:
		return
	var result = _bridge.call("OrbitalScanV0", loc, "standard")
	if result is Dictionary:
		_planet_scans_performed += 1
		_a.event(_decision, "PLANET_SCAN", "node=%s charges_left=%d" % [loc, current_charges - 1])
		_a.log("ENGAGE|d=%d orbital scan at %s" % [_decision, loc])
		_record_reward("PLANET_SCAN")
		_introduce_system("planet_scan")


func _bot_try_start_construction() -> void:
	if not _bridge.has_method("GetAvailableConstructionDefsV0") or not _bridge.has_method("StartConstructionV0"):
		return
	# Get haven node for construction
	var haven_node := ""
	if _bridge.has_method("GetHavenStatusV0"):
		var hs = _bridge.call("GetHavenStatusV0")
		if hs is Dictionary:
			haven_node = str(hs.get("node_id", ""))
	if haven_node.is_empty():
		return
	var defs = _bridge.call("GetAvailableConstructionDefsV0")
	if not defs is Array or defs.size() == 0:
		return
	# Pick first def that has prerequisites met
	for d in defs:
		if not d is Dictionary:
			continue
		if not bool(d.get("prerequisites_met", false)):
			continue
		var def_id: String = str(d.get("project_def_id", ""))
		if def_id.is_empty():
			continue
		var result = _bridge.call("StartConstructionV0", def_id, haven_node)
		if result is Dictionary and bool(result.get("success", false)):
			_constructions_started += 1
			_construction_projects += 1
			_a.event(_decision, "CONSTRUCTION_STARTED", "def=%s node=%s" % [def_id, haven_node])
			_a.log("ENGAGE|d=%d started construction %s at %s" % [_decision, def_id, haven_node])
			_record_reward("CONSTRUCTION_STARTED")
			_introduce_system("construction")
			return
		else:
			var reason: String = str(result.get("reason", "")) if result is Dictionary else "null"
			_a.log("ENGAGE|d=%d construction %s failed: %s" % [_decision, def_id, reason])


func _bot_try_haven_actions() -> void:
	# Haven upgrade (try once)
	if not _haven_upgraded and _bridge.has_method("UpgradeHavenV0"):
		var upgraded: bool = _bridge.call("UpgradeHavenV0")
		if upgraded:
			_haven_upgraded = true
			_a.event(_decision, "HAVEN_UPGRADED", "")
			_a.log("ENGAGE|d=%d haven upgraded" % _decision)
			_record_reward("HAVEN_UPGRADED")
			_introduce_system("haven_upgrade")

	# Fabrication (try if fabricator is available)
	if _bridge.has_method("GetFabricatorV0") and _bridge.has_method("StartFabricationV0"):
		var fab = _bridge.call("GetFabricatorV0")
		if fab is Dictionary and bool(fab.get("available", false)):
			# Only try if not already fabricating
			var currently_fabricating: String = str(fab.get("fabricating_module", ""))
			if currently_fabricating.is_empty():
				# Try to fabricate any completed or available module
				# Check loadout for an installed module id to fabricate another copy
				if _bridge.has_method("GetHeroShipLoadoutV0"):
					var loadout = _bridge.call("GetHeroShipLoadoutV0")
					if loadout is Array:
						for slot in loadout:
							if not slot is Dictionary:
								continue
							var mod_id: String = str(slot.get("module_id", ""))
							if mod_id.is_empty() or mod_id == "empty":
								continue
							var fab_result = _bridge.call("StartFabricationV0", mod_id)
							if fab_result is Dictionary and bool(fab_result.get("success", false)):
								_fabrications_started += 1
								_a.event(_decision, "FABRICATION_STARTED", "module=%s" % mod_id)
								_a.log("ENGAGE|d=%d started fabrication of %s" % [_decision, mod_id])
								_record_reward("FABRICATION_STARTED")
								_introduce_system("fabrication")
								return
							break  # only try one


func _bot_try_megaproject() -> void:
	if _megaprojects_started > 0:
		return  # only attempt once
	if not _bridge.has_method("GetMegaprojectsV0") or not _bridge.has_method("StartMegaprojectV0"):
		return
	# Check if there are already any megaprojects (started by the system or player)
	var existing = _bridge.call("GetMegaprojectsV0")
	if existing is Array and existing.size() > 0:
		# Already have megaprojects — try delivering supply to the first one
		_bot_try_deliver_megaproject_supply(existing)
		return
	# No megaprojects yet — try to start one
	# Need haven node for placement
	var haven_node := ""
	if _bridge.has_method("GetHavenStatusV0"):
		var hs = _bridge.call("GetHavenStatusV0")
		if hs is Dictionary:
			haven_node = str(hs.get("node_id", ""))
	if haven_node.is_empty():
		return
	# Try starting a megaproject (use a known type_id — pick first available from content)
	# We don't have a content listing method, so try common type ids
	for type_id in ["beacon_network", "trade_hub", "defense_array", "research_station"]:
		var result = _bridge.call("StartMegaprojectV0", type_id, haven_node)
		if result is Dictionary and bool(result.get("success", false)):
			_megaprojects_started += 1
			var mp_id: String = str(result.get("megaproject_id", ""))
			_a.event(_decision, "MEGAPROJECT_STARTED", "type=%s id=%s" % [type_id, mp_id])
			_a.log("ENGAGE|d=%d started megaproject %s (%s) at %s" % [_decision, mp_id, type_id, haven_node])
			_record_reward("MEGAPROJECT_STARTED")
			_introduce_system("megaproject")
			return


func _bot_try_deliver_megaproject_supply(megaprojects: Array) -> void:
	if not _bridge.has_method("DeliverMegaprojectSupplyV0"):
		return
	for mp in megaprojects:
		if not mp is Dictionary:
			continue
		if bool(mp.get("completed", false)):
			continue
		var mp_id: String = str(mp.get("id", ""))
		if mp_id.is_empty():
			continue
		# Try delivering 10 units of common goods
		for good_id in _goods_bought.keys():
			var delivered: bool = _bridge.call("DeliverMegaprojectSupplyV0", mp_id, str(good_id), 10)
			if delivered:
				_a.log("ENGAGE|d=%d delivered supply to megaproject %s (good=%s qty=10)" % [_decision, mp_id, str(good_id)])
				return
		return  # only try first non-completed megaproject


func _bot_monitor_programs() -> void:
	if not _bridge.has_method("GetProgramExplainSnapshot"):
		return
	var programs = _bridge.call("GetProgramExplainSnapshot")
	if not programs is Array:
		return
	_program_monitoring_count += 1
	var active_count := 0
	var failed_count := 0
	var total_earned := 0
	var total_expense := 0
	var unprofitable_count := 0
	for p in programs:
		if not p is Dictionary:
			continue
		active_count += 1
		var status: String = str(p.get("status", ""))
		if status == "failed" or status == "error":
			failed_count += 1
		# Query per-program performance for ALL active programs
		var pid: String = str(p.get("id", ""))
		var kind: String = str(p.get("kind", ""))
		if not pid.is_empty() and _bridge.has_method("GetProgramPerformanceV0"):
			var perf = _bridge.call("GetProgramPerformanceV0", pid)
			if perf is Dictionary:
				var earned: int = int(perf.get("credits_earned", 0))
				var expense: int = int(perf.get("total_expense", 0))
				var net: int = int(perf.get("net_profit", 0))
				var cycles: int = int(perf.get("cycles_run", 0))
				var failures: int = int(perf.get("failures", 0))
				total_earned += earned
				total_expense += expense
				if cycles >= 50 and net < 0:
					unprofitable_count += 1
				_a.log("PROGRAM_PERF|d=%d id=%s kind=%s earned=%d expense=%d net=%d cycles=%d failures=%d" % [
					_decision, pid, kind, earned, expense, net, cycles, failures])
	# Accumulate session-wide totals for final summary
	_automation_total_earned = total_earned
	_automation_total_expense = total_expense
	_automation_total_cycles += active_count  # approximate cycle count from monitoring samples
	_a.log("PROGRAMS|d=%d monitoring: active=%d failed=%d total_earned=%d total_expense=%d unprofitable=%d" % [
		_decision, active_count, failed_count, total_earned, total_expense, unprofitable_count])
	if unprofitable_count > 0:
		_a.log("PROGRAM_ISSUE|WARN|%d programs unprofitable after 50+ cycles" % unprofitable_count)


func _bot_try_accept_mission() -> void:
	if not _bridge.has_method("GetMissionListV0") or not _bridge.has_method("AcceptMissionV0"):
		return
	var missions = _bridge.call("GetMissionListV0")
	if not missions is Array:
		return
	# GetMissionListV0 returns only available missions — all entries are candidates
	for m in missions:
		if not m is Dictionary:
			continue
		var mid: String = str(m.get("mission_id", ""))
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
				_decision_counter += 1
				_a.event(_decision_counter, "POWER_MODULE_INSTALL", "module=%s slot=%d" % [mod_id, target_idx])
				_a.log("ENGAGE|d=%d installed module %s in slot %d (%s)" % [_decision, mod_id, target_idx, target_kind])
				_record_reward("MODULE_INSTALLED")
				_introduce_system("refit")
				return
			else:
				var reason: String = str(result.get("reason", "unknown")) if result is Dictionary else "non-dict result"
				_a.log("ENGAGE|d=%d module %s install failed: %s" % [_decision, mod_id, reason])
				return  # don't spam — try again next cooldown


func _try_power_moment() -> void:
	# End-of-play-loop power moment: ensure at least one module install was attempted
	if _modules_installed_by_bot > 0:
		return  # already achieved
	if not _bridge.has_method("GetAvailableModulesV0") or not _bridge.has_method("InstallModuleV0"):
		_a.warn(false, "power_moment_reached", "modules=0 (bridge missing)")
		return
	var available = _bridge.call("GetAvailableModulesV0")
	if not available is Array or available.size() == 0:
		_a.warn(false, "power_moment_reached", "modules=0 (none available)")
		return
	# Try to install the first installable module
	if not _bridge.has_method("GetHeroShipLoadoutV0"):
		_a.warn(false, "power_moment_reached", "modules=0 (no loadout)")
		return
	var loadout = _bridge.call("GetHeroShipLoadoutV0")
	if not loadout is Array:
		_a.warn(false, "power_moment_reached", "modules=0 (loadout invalid)")
		return
	for i in range(loadout.size()):
		var slot = loadout[i]
		if not slot is Dictionary:
			continue
		if not str(slot.get("installed_module_id", "")).is_empty():
			continue
		var slot_kind: String = str(slot.get("slot_kind", ""))
		var slot_index: int = int(slot.get("slot_index", i))
		for mod in available:
			if not mod is Dictionary:
				continue
			var mod_id: String = str(mod.get("module_id", ""))
			var mod_kind: String = str(mod.get("slot_kind", ""))
			if mod_id.is_empty() or mod_kind != slot_kind:
				continue
			if bool(mod.get("is_locked", false)):
				continue
			var result = _bridge.call("InstallModuleV0", "fleet_trader_1", slot_index, mod_id)
			if result is Dictionary and bool(result.get("success", false)):
				_modules_installed_by_bot += 1
				_decision_counter += 1
				_a.event(_decision_counter, "POWER_MODULE_INSTALL", "module=%s slot=%d (power_moment)" % [mod_id, slot_index])
				_a.log("POWER_MOMENT|installed module %s" % mod_id)
				_a.warn(true, "power_moment_reached", "modules=%d" % _modules_installed_by_bot)
				return
	_a.warn(false, "power_moment_reached", "modules=0 (no compatible slot)")


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
	# Prioritize sensor_suite — unlocks discovery scanning
	var priority_techs: Array = ["sensor_suite"]
	var available_techs: Array = []
	for tech in tree:
		if not tech is Dictionary:
			continue
		var st: String = str(tech.get("status", ""))
		if st != "available":
			continue
		var tid: String = str(tech.get("tech_id", ""))
		if tid.is_empty():
			continue
		available_techs.append(tid)
	# Try priority techs first, then any available
	var ordered: Array = []
	for pt in priority_techs:
		if pt in available_techs:
			ordered.append(pt)
	for at in available_techs:
		if at not in ordered:
			ordered.append(at)
	for tid in ordered:
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
	_automation_attempted = true

	# TradeCharter: d >= 8 sells (first automation type)
	if _total_sells >= 8 and not _automation_types_created.has("TradeCharter"):
		if _bridge.has_method("CreateTradeCharterProgram"):
			if not _goods_sold.is_empty() and _all_nodes.size() >= 2 and not _home_node_id.is_empty():
				var best_good := ""
				var best_count := 0
				for gid in _good_trade_count.keys():
					if int(_good_trade_count[gid]) > best_count:
						best_count = int(_good_trade_count[gid])
						best_good = gid
				if not best_good.is_empty():
					var dest_node := ""
					for n in _all_nodes:
						var nid := str(n.get("node_id", ""))
						if nid == _home_node_id or nid.is_empty():
							continue
						dest_node = nid
						break
					if not dest_node.is_empty():
						var charter_id = _bridge.call("CreateTradeCharterProgram",
							_home_node_id, dest_node, best_good, best_good, 60)
						if charter_id is String and not charter_id.is_empty():
							_automation_created = true
							_automations_created += 1
							_automation_types_created["TradeCharter"] = true
							_a.log("ENGAGE|d=%d created trade charter %s (%s: %s -> %s)" % [
								_decision, charter_id, best_good, _home_node_id, dest_node])
							_record_reward("AUTOMATION_CREATED")
							_introduce_system("automation")
						else:
							_a.log("ENGAGE|d=%d trade charter creation failed" % _decision)

	# AutoBuy: d >= 15 sells — buy a frequently-traded good at current location
	if _total_sells >= 15 and not _automation_types_created.has("AutoBuy"):
		if _bridge.has_method("CreateAutoBuyProgram"):
			var best_good := ""
			var best_count := 0
			for gid in _good_trade_count.keys():
				if int(_good_trade_count[gid]) > best_count:
					best_count = int(_good_trade_count[gid])
					best_good = gid
			if not best_good.is_empty() and not loc.is_empty():
				var ab_id = _bridge.call("CreateAutoBuyProgram", loc, best_good, 5, 120)
				if ab_id is String and not ab_id.is_empty():
					_automations_created += 1
					_automation_types_created["AutoBuy"] = true
					_a.log("ENGAGE|d=%d created auto-buy %s (good=%s at %s)" % [
						_decision, ab_id, best_good, loc])
					_record_reward("AUTOBUY_CREATED")
					_introduce_system("auto_buy")
				else:
					_a.log("ENGAGE|d=%d auto-buy creation failed" % _decision)

	# ResourceTap: d >= 20 sells — extract raw material at a node
	if _total_sells >= 20 and not _automation_types_created.has("ResourceTap"):
		if _bridge.has_method("CreateResourceTapProgram"):
			# Pick a good from what the bot has bought (likely a raw material source)
			var tap_good := ""
			for gid in _goods_bought.keys():
				tap_good = str(gid)
				break
			if not tap_good.is_empty() and not loc.is_empty():
				var rt_id = _bridge.call("CreateResourceTapProgram", loc, tap_good, 120)
				if rt_id is String and not rt_id.is_empty():
					_automations_created += 1
					_automation_types_created["ResourceTap"] = true
					_a.log("ENGAGE|d=%d created resource tap %s (good=%s at %s)" % [
						_decision, rt_id, tap_good, loc])
					_record_reward("RESOURCE_TAP_CREATED")
					_introduce_system("resource_tap")
				else:
					_a.log("ENGAGE|d=%d resource tap creation failed" % _decision)

	# Patrol: d >= 25 sells — patrol between two visited nodes
	if _total_sells >= 25 and not _automation_types_created.has("Patrol"):
		if _bridge.has_method("CreatePatrolProgramV0"):
			var visited_nodes: Array = _visited.keys()
			if visited_nodes.size() >= 2:
				var node_a: String = str(visited_nodes[0])
				var node_b: String = str(visited_nodes[1])
				var patrol_id = _bridge.call("CreatePatrolProgramV0",
					"fleet_trader_1", node_a, node_b, 120)
				if patrol_id is String and not patrol_id.is_empty():
					_automations_created += 1
					_automation_types_created["Patrol"] = true
					_a.log("ENGAGE|d=%d created patrol %s (%s <-> %s)" % [
						_decision, patrol_id, node_a, node_b])
					_record_reward("PATROL_CREATED")
					_introduce_system("patrol")
				else:
					_a.log("ENGAGE|d=%d patrol creation failed" % _decision)

	# Survey: d >= 30 sells — survey from current node
	if _total_sells >= 30 and not _automation_types_created.has("Survey"):
		if _bridge.has_method("CreateSurveyProgramV0"):
			if not loc.is_empty():
				var survey_id = _bridge.call("CreateSurveyProgramV0",
					"general", loc, 3, 120)
				if survey_id is String and not survey_id.is_empty():
					_automations_created += 1
					_automation_types_created["Survey"] = true
					_a.log("ENGAGE|d=%d created survey %s (home=%s range=3)" % [
						_decision, survey_id, loc])
					_record_reward("SURVEY_CREATED")
					_introduce_system("survey")
				else:
					_a.log("ENGAGE|d=%d survey creation failed" % _decision)


# ---- Helpers ----

func _probe_combat_round_log() -> void:
	# Wire GetLastCombatLogV0 for round-by-round combat feel analysis
	if not _bridge.has_method("GetLastCombatLogV0"):
		return
	var log = _bridge.call("GetLastCombatLogV0")
	if not log is Dictionary:
		return
	var rounds = log.get("rounds", [])
	if not rounds is Array or rounds.size() == 0:
		return
	_combat_round_counts.append(rounds.size())
	# Find shield-break round (first round where defender shield = 0)
	var shield_broke := -1
	var total_dmg := 0
	var dmg_values: Array = []
	for i in range(rounds.size()):
		var r: Dictionary = rounds[i] if rounds[i] is Dictionary else {}
		var defender_shield := int(r.get("defender_shield", -1))
		if defender_shield == 0 and shield_broke < 0:
			shield_broke = i + 1
		var dmg := int(r.get("damage_dealt", 0))
		total_dmg += dmg
		dmg_values.append(dmg)
	if shield_broke > 0:
		_shield_break_rounds.append(shield_broke)
	# TTK ratio: rounds to kill / total rounds (1.0 = killed on last round)
	var attacker_alive := bool(log.get("attacker_alive", true))
	var defender_alive := bool(log.get("defender_alive", true))
	if not defender_alive and rounds.size() > 0:
		# Player killed enemy — TTK is rounds.size()
		_combat_ttk_ratios.append(float(rounds.size()))
	# Damage variance (coefficient of variation)
	if dmg_values.size() >= 2:
		var mean_dmg := float(total_dmg) / float(dmg_values.size())
		var variance := 0.0
		for d in dmg_values:
			variance += (float(d) - mean_dmg) * (float(d) - mean_dmg)
		variance /= float(dmg_values.size())
		var cv := sqrt(variance) / maxf(mean_dmg, 1.0)
		_combat_dmg_variance.append(cv)


func _check_dock_system_dump(_node_id: String) -> void:
	# Count NEW tabs that became visible at this dock vs the previous dock.
	# This measures what the PLAYER perceives as new (tab disclosure), not what
	# the backend has available (which includes many systems from tick 0).
	# Previous bug: checked backend data existence → always reported 6 at first dock.
	var current_tabs := 1  # Market tab always visible
	if _bridge.has_method("GetOnboardingStateV0"):
		var obs = _bridge.call("GetOnboardingStateV0")
		if obs is Dictionary:
			if obs.get("show_jobs_tab", false): current_tabs += 1
			if obs.get("show_station_tab", false): current_tabs += 1
			if obs.get("show_ship_tab", false): current_tabs += 1
			if obs.get("show_intel_tab", false): current_tabs += 1
	var prev_tabs: int = _last_dock_tab_count if _last_dock_tab_count >= 0 else 0
	var new_count: int = maxi(0, current_tabs - prev_tabs)
	_last_dock_tab_count = current_tabs
	if new_count > 0:
		_dock_new_systems.append(new_count)
		if new_count >= 3:
			_dock_system_dump_count += 1
	else:
		_dock_new_systems.append(0)
	# Track systems per dock for cognitive load
	_systems_per_dock.append(new_count)
	# Track tab count at this dock (from progressive disclosure state)
	# Query the actual bridge disclosure state, not just known systems count.
	# GetOnboardingStateV0 returns show_jobs_tab, show_ship_tab, etc.
	var tab_count := 1  # Market tab is always visible
	if _bridge.has_method("GetOnboardingStateV0"):
		var obs = _bridge.call("GetOnboardingStateV0")
		if obs is Dictionary:
			if obs.get("show_jobs_tab", false): tab_count += 1
			if obs.get("show_station_tab", false): tab_count += 1
			if obs.get("show_ship_tab", false): tab_count += 1
			if obs.get("show_intel_tab", false): tab_count += 1
	_tabs_per_dock.append(tab_count)
	if _tabs_at_first_dock < 0:
		_tabs_at_first_dock = tab_count
	# Record dock as a valence event
	if not _visited.has(str(_bridge.call("GetPlayerStateV0").get("current_node_id", ""))):
		_record_valence_event("new_station_dock", _decision)
	else:
		_record_valence_event("travel", _decision)


func _probe_credit_history() -> void:
	# Wire GetCreditHistoryV0 for richer credit curve
	if not _bridge.has_method("GetCreditHistoryV0"):
		return
	_credit_history_available = true
	var history = _bridge.call("GetCreditHistoryV0")
	if not history is Array:
		return
	# Sample velocity from bridge history (more accurate than manual tracking)
	if history.size() >= 2:
		var last: Dictionary = history[-1] if history[-1] is Dictionary else {}
		var prev: Dictionary = history[-2] if history[-2] is Dictionary else {}
		var tick_delta := int(last.get("tick", 0)) - int(prev.get("tick", 0))
		var credit_delta := int(last.get("credits", 0)) - int(prev.get("credits", 0))
		if tick_delta > 0:
			_credit_velocity_from_bridge.append(float(credit_delta) / float(tick_delta))


func _probe_fo_adaptation() -> void:
	# Wire GetFOAdaptationLogV0 for FO personality tracking
	if not _bridge.has_method("GetFOAdaptationLogV0"):
		return
	var log = _bridge.call("GetFOAdaptationLogV0")
	if not log is Array:
		return
	_fo_adaptation_events = log.size()


# ============================================================================
# EXPERIENCE CHECKPOINTS — 10min / 30min / 60min phased player experience
# ============================================================================
# Captures a scored snapshot at each time mark. Scores 5 dimensions (1-5):
#   Orientation  — Does the player know what to do?
#   Mastery      — Can they do it effectively?
#   Engagement   — Do they want to keep playing?
#   Feel         — Does it feel good? (tension, reward, feedback)
#   Progression  — Are things getting better?
# Thresholds are phase-calibrated: expectations at 10min << 30min << 60min.

func _capture_checkpoint(phase: String) -> void:
	if _checkpoint_captured.has(phase):
		return
	_checkpoint_captured[phase] = true

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits := int(ps.get("credits", 0))
	var credit_growth_pct := ((credits - _start_credits) * 100) / maxi(_start_credits, 1)
	var nodes_visited := _visited.size()
	var total_nodes := _all_nodes.size()
	var visit_pct := (float(nodes_visited) / maxf(float(total_nodes), 1.0)) * 100.0
	var goods_traded := _goods_bought.size() + _goods_sold.size()
	var total_trades := _total_buys + _total_sells
	var fo_lines := _fo_dialogue_count
	var combats := _total_combats
	var kills := _total_kills
	var sys_intros := _systems_introduced.size()
	var has_first_buy := _event_decisions.has("first_buy")
	var has_first_sell := _event_decisions.has("first_sell")
	var has_first_combat := _event_decisions.has("first_combat")
	var has_fo := _event_decisions.has("first_fo_line")
	var hull_min_val: int = _combat_hull_mins.min() if _combat_hull_mins.size() > 0 else 100
	var profitable_pct := (float(_trades_profitable) / maxf(float(_trades_profitable + _trades_unprofitable), 1.0)) * 100.0

	# Compute current action entropy for engagement scoring
	var action_counts: Dictionary = {}
	for at in _action_type_history:
		action_counts[at] = action_counts.get(at, 0) + 1
	var entropy := 0.0
	var total_actions := _action_type_history.size()
	if total_actions > 0:
		for at in action_counts:
			var p: float = float(action_counts[at]) / float(total_actions)
			if p > 0.0:
				entropy -= p * log(p) / log(2.0)

	# Build metrics snapshot
	var snap := {
		"phase": phase,
		"decision": _decision,
		"credits": credits,
		"credit_growth_pct": credit_growth_pct,
		"nodes_visited": nodes_visited,
		"total_nodes": total_nodes,
		"visit_pct": snapped(visit_pct, 0.1),
		"goods_traded": goods_traded,
		"total_trades": total_trades,
		"fo_lines": fo_lines,
		"combats": combats,
		"kills": kills,
		"hull_min_pct": hull_min_val,
		"systems_introduced": sys_intros,
		"system_list": _systems_introduced.duplicate(),
		"modules_installed": _modules_installed,
		"haven_discovered": _haven_discovered,
		"automation_created": _automation_created,
		"grind_score": snapped(_compute_grind_score(), 0.01),
		"entropy": snapped(entropy, 0.01),
		"max_reward_gap": _max_reward_gap,
		"fo_react_timeouts": _fo_react_timeouts,
		"profitable_pct": snapped(profitable_pct, 0.1),
		"event_timeline": _event_decisions.duplicate(),
	}

	# Score 5 dimensions
	snap["orientation"] = _score_orientation(phase, snap)
	snap["mastery"] = _score_mastery(phase, snap)
	snap["engagement"] = _score_engagement(phase, snap)
	snap["feel"] = _score_feel(phase, snap)
	snap["progression"] = _score_progression(phase, snap)
	snap["overall"] = snapped((float(snap["orientation"]) + float(snap["mastery"]) + float(snap["engagement"]) + float(snap["feel"]) + float(snap["progression"])) / 5.0, 0.1)

	# Phase-specific issues
	snap["issues"] = _check_checkpoint_issues(phase, snap)

	_checkpoints.append(snap)
	_log_checkpoint(snap)


func _score_orientation(phase: String, s: Dictionary) -> int:
	# "Does the player know what to do?"
	var score := 1
	match phase:
		"10min":
			# At 10 min: have they found the core loop?
			if s.get("event_timeline", {}).has("first_buy") and s.get("event_timeline", {}).has("first_sell"):
				score += 2  # Core loop discovered
			elif s.get("event_timeline", {}).has("first_buy"):
				score += 1  # Half the loop
			if s.get("event_timeline", {}).has("first_fo_line"):
				score += 1  # FO is guiding them
			if int(s.get("nodes_visited", 0)) >= 2:
				score += 1  # Can navigate
		"30min":
			if int(s.get("nodes_visited", 0)) >= 4: score += 1
			if int(s.get("goods_traded", 0)) >= 3: score += 1
			if int(s.get("fo_lines", 0)) >= 5: score += 1
			if int(s.get("systems_introduced", 0)) >= 3: score += 1
			if float(s.get("grind_score", 0.0)) < 0.4: score += 0  # Not repeating = understanding
			# Bonus: not stuck in one place
			if float(s.get("visit_pct", 0.0)) >= 15.0: score += 1
		"60min":
			if float(s.get("visit_pct", 0.0)) >= 35.0: score += 1
			if int(s.get("goods_traded", 0)) >= 5: score += 1
			if int(s.get("systems_introduced", 0)) >= 5: score += 1
			if int(s.get("nodes_visited", 0)) >= 8: score += 1
			# No confusion: backtrack rate reasonable
			var bt_pct := (float(_backtrack_count) / maxf(float(_total_travel_count), 1.0)) * 100.0
			if bt_pct < 35.0: score += 1
	return mini(score, 5)


func _score_mastery(phase: String, s: Dictionary) -> int:
	# "Can they do it effectively?"
	var score := 1
	match phase:
		"10min":
			if int(s.get("total_trades", 0)) >= 1: score += 1
			if _trades_profitable >= 1: score += 2  # At least one win
			if int(s.get("credits", 0)) > _start_credits: score += 1
		"30min":
			if float(s.get("profitable_pct", 0.0)) >= 50.0: score += 1
			if int(s.get("combats", 0)) > 0 and int(s.get("kills", 0)) > 0: score += 1
			if int(s.get("total_trades", 0)) >= 5: score += 1
			if _credit_death_spiral_max < 5: score += 1
			if int(s.get("modules_installed", 0)) > 0 or _missions_accepted_by_bot > 0: score += 1
		"60min":
			if float(s.get("profitable_pct", 0.0)) >= 60.0: score += 1
			if int(s.get("combats", 0)) > 0 and float(int(s.get("kills", 0))) / maxf(float(int(s.get("combats", 0))), 1.0) > 0.5: score += 1
			if bool(s.get("automation_created", false)) or _missions_accepted_by_bot > 0: score += 1
			if _research_started_by_bot or _techs_unlocked > 0: score += 1
			# Multi-system competency
			var systems_used := 0
			if int(s.get("modules_installed", 0)) > 0: systems_used += 1
			if int(s.get("combats", 0)) > 3: systems_used += 1
			if _missions_accepted_by_bot > 0: systems_used += 1
			if bool(s.get("automation_created", false)): systems_used += 1
			if systems_used >= 3: score += 1
	return mini(score, 5)


func _score_engagement(phase: String, s: Dictionary) -> int:
	# "Do they want to keep playing?"
	var score := 1
	match phase:
		"10min":
			# Multiple action types used (not just sitting)
			var action_types: Dictionary = {}
			for at in _action_type_history:
				action_types[at] = true
			if action_types.size() >= 2: score += 1
			# Recent reward (within last 20 decisions)
			if _reward_events.size() > 0 and (_decision - int(_reward_events[-1])) < 20: score += 1
			# Not stuck
			if int(s.get("total_trades", 0)) + int(s.get("combats", 0)) >= 2: score += 1
			# FO is talking
			if int(s.get("fo_lines", 0)) > 0: score += 1
		"30min":
			if float(s.get("entropy", 0.0)) > 0.8: score += 1
			if int(s.get("max_reward_gap", 999)) < 40: score += 1
			if float(s.get("visit_pct", 0.0)) > 15.0: score += 1
			if int(s.get("systems_introduced", 0)) > int(_checkpoints[0].get("systems_introduced", 0)) if _checkpoints.size() > 0 else int(s.get("systems_introduced", 0)) > 1: score += 1
			if int(s.get("goods_traded", 0)) > 2: score += 1
		"60min":
			# GATE.T66.BOT.EXPERIENCE_CALIBRATE.001: Tightened from 0.3.
			if float(s.get("grind_score", 0.0)) < 0.20: score += 1
			# Still discovering new things (compare to 30min checkpoint)
			var prev_nodes := 0
			for cp in _checkpoints:
				if cp.get("phase", "") == "30min":
					prev_nodes = int(cp.get("nodes_visited", 0))
			if int(s.get("nodes_visited", 0)) > prev_nodes + 2: score += 1
			# Depth systems engaged
			var depth := 0
			if bool(s.get("haven_discovered", false)): depth += 1
			if _revelation_stage > 0: depth += 1
			if _knowledge_entries > 0: depth += 1
			if _discoveries_found > 0: depth += 1
			if depth >= 2: score += 1
			# Story progressing
			if _narrative_npcs_met > 0 or _revelation_stage > 0: score += 1
	return mini(score, 5)


func _score_feel(phase: String, s: Dictionary) -> int:
	# "Does it feel good?"
	var score := 1
	match phase:
		"10min":
			# No long dead gaps (> 20 decisions with no reward)
			if int(s.get("max_reward_gap", 0)) < 25 or _reward_events.size() < 2: score += 1
			# Credits going up
			if int(s.get("credit_growth_pct", 0)) > 0: score += 2
			# FO responded to something
			if _fo_react_latencies.size() > 0: score += 1
		"30min":
			# Credit curve rising
			if int(s.get("credit_growth_pct", 0)) > 30: score += 1
			# Hull tension exists (combat creates suspense)
			if int(s.get("hull_min_pct", 100)) < 80 or int(s.get("combats", 0)) == 0: score += 1
			# Margins not declining
			if _declining_margin_streaks <= 2: score += 1
			# FO reactive (0-1 timeouts)
			if int(s.get("fo_react_timeouts", 0)) <= 1: score += 1
			# Got loot from combat
			if _combat_loot_count > 0 or int(s.get("combats", 0)) == 0: score += 1
		"60min":
			# Credit crescendo (healthy growth curve)
			if int(s.get("credit_growth_pct", 0)) > 100: score += 1
			# Combat creates real tension
			if int(s.get("hull_min_pct", 100)) < 70 or int(s.get("combats", 0)) < 3: score += 1
			# FO feels like companion
			var fo_silence := _compute_fo_max_silence()
			if fo_silence < 80 or int(s.get("fo_lines", 0)) > 10: score += 1
			# Reward cadence maintained
			if int(s.get("max_reward_gap", 999)) < 35: score += 1
			# Visual feedback working (visual mode) or good telemetry (headless)
			if _is_visual:
				var feel_count := 0
				for b in [_feel_credits_flash_seen, _feel_damage_flash_seen, _feel_combat_vignette_seen, _feel_toast_seen]:
					if b: feel_count += 1
				if feel_count >= 2: score += 1
			else:
				# Headless: use reward/tension proxies
				if _reward_events.size() >= 10: score += 1
	return mini(score, 5)


func _score_progression(phase: String, s: Dictionary) -> int:
	# "Are things getting better?"
	var score := 1
	match phase:
		"10min":
			if int(s.get("credits", 0)) > _start_credits: score += 2
			if int(s.get("nodes_visited", 0)) >= 2: score += 1
			if int(s.get("systems_introduced", 0)) >= 1: score += 1
		"30min":
			if int(s.get("credit_growth_pct", 0)) > 50: score += 1
			if int(s.get("modules_installed", 0)) > 0: score += 1
			# Multiple trade routes (visited 4+ nodes)
			if int(s.get("nodes_visited", 0)) >= 4: score += 1
			if _mission_first_seen > 0 and _mission_first_seen <= _decision: score += 1
			# Exploration depth
			if float(s.get("visit_pct", 0.0)) > 20.0: score += 1
		"60min":
			if int(s.get("credit_growth_pct", 0)) > 200: score += 1
			if _milestones_unlocked > 0: score += 1
			if _techs_unlocked > 0: score += 1
			if int(s.get("modules_installed", 0)) > 0: score += 1
			if bool(s.get("haven_discovered", false)) or _endgame_paths_revealed > 0: score += 1
	return mini(score, 5)


func _check_checkpoint_issues(phase: String, s: Dictionary) -> Array:
	var issues: Array = []
	match phase:
		"10min":
			if not s.get("event_timeline", {}).has("first_buy"):
				issues.append({"severity": "CRITICAL", "issue": "Player hasn't bought anything in 10 minutes — core loop not found"})
			if not s.get("event_timeline", {}).has("first_fo_line"):
				issues.append({"severity": "MAJOR", "issue": "FO hasn't spoken in 10 minutes — player has no guidance"})
			if int(s.get("nodes_visited", 0)) < 2:
				issues.append({"severity": "MAJOR", "issue": "Still at starting node after 10 minutes — player may be stuck"})
			if int(s.get("credits", 0)) < _start_credits and int(s.get("total_trades", 0)) > 0:
				issues.append({"severity": "MAJOR", "issue": "Credits below starting after trades — first experience is losing money"})
		"30min":
			if not s.get("event_timeline", {}).has("first_sell"):
				issues.append({"severity": "CRITICAL", "issue": "No sell completed in 30 minutes — core loop broken"})
			if int(s.get("credit_growth_pct", 0)) < 0:
				issues.append({"severity": "MAJOR", "issue": "Credits declining after 30 minutes — player can't find profitable routes"})
			if int(s.get("goods_traded", 0)) < 2:
				issues.append({"severity": "MAJOR", "issue": "Only 1 good traded after 30 min — market diversity not apparent"})
			# GATE.T66.BOT.EXPERIENCE_CALIBRATE.001: Tightened from 0.3 (T66 economy changes).
			if float(s.get("grind_score", 0.0)) > 0.20:
				issues.append({"severity": "MAJOR", "issue": "Grind pattern detected at 30 min — player stuck in repetitive loop"})
			if int(s.get("systems_introduced", 0)) < 2:
				issues.append({"severity": "MAJOR", "issue": "Fewer than 2 systems introduced — progressive disclosure too slow"})
			if int(s.get("systems_introduced", 0)) > 6:
				issues.append({"severity": "MINOR", "issue": "6+ systems introduced by 30 min — possible information overload"})
		"60min":
			if float(s.get("visit_pct", 0.0)) < 25.0:
				issues.append({"severity": "MAJOR", "issue": "Less than 25%% of galaxy explored — world feels small or player is stuck"})
			# GATE.T66.BOT.EXPERIENCE_CALIBRATE.001: Tightened from 0.4 (T66 economy changes).
			if float(s.get("grind_score", 0.0)) > 0.30:
				issues.append({"severity": "MAJOR", "issue": "Grind pattern persists at 60 min — no variety in gameplay"})
			if not bool(s.get("haven_discovered", false)) and not bool(s.get("automation_created", false)):
				issues.append({"severity": "MINOR", "issue": "No deep systems (haven/automation) reached — depth promise unfulfilled"})
			if _revelation_stage == 0 and _narrative_npcs_met == 0:
				issues.append({"severity": "MINOR", "issue": "No narrative engagement after full hour — story invisible"})
			if int(s.get("fo_react_timeouts", 0)) > 3:
				issues.append({"severity": "MAJOR", "issue": "FO silent on %d player events — companion feels absent" % int(s.get("fo_react_timeouts", 0))})
	return issues


func _log_checkpoint(snap: Dictionary) -> void:
	var phase: String = snap.get("phase", "?")
	var d: int = int(snap.get("decision", 0))
	_a.log("")
	_a.log("╔══════════════════════════════════════════════════════════════╗")
	_a.log("║  EXPERIENCE CHECKPOINT: %s  (decision %d)                  " % [phase.to_upper(), d])
	_a.log("╠══════════════════════════════════════════════════════════════╣")
	_a.log("║  Orientation  %d/5  — Does the player know what to do?     " % int(snap.get("orientation", 0)))
	_a.log("║  Mastery      %d/5  — Can they do it effectively?          " % int(snap.get("mastery", 0)))
	_a.log("║  Engagement   %d/5  — Do they want to keep playing?        " % int(snap.get("engagement", 0)))
	_a.log("║  Feel         %d/5  — Does it feel good?                   " % int(snap.get("feel", 0)))
	_a.log("║  Progression  %d/5  — Are things getting better?           " % int(snap.get("progression", 0)))
	_a.log("║  ──────────────────────────────────────────────────────     ")
	_a.log("║  OVERALL      %.1f/5                                       " % float(snap.get("overall", 0.0)))
	_a.log("╚══════════════════════════════════════════════════════════════╝")
	_a.log("  credits=%d (+%d%%) nodes=%d/%d (%.0f%%) trades=%d goods=%d" % [
		int(snap.get("credits", 0)), int(snap.get("credit_growth_pct", 0)),
		int(snap.get("nodes_visited", 0)), int(snap.get("total_nodes", 0)),
		float(snap.get("visit_pct", 0.0)),
		int(snap.get("total_trades", 0)), int(snap.get("goods_traded", 0))])
	_a.log("  combats=%d kills=%d hull_min=%d%% fo_lines=%d systems=%d" % [
		int(snap.get("combats", 0)), int(snap.get("kills", 0)),
		int(snap.get("hull_min_pct", 100)), int(snap.get("fo_lines", 0)),
		int(snap.get("systems_introduced", 0))])
	# Log key events that have happened by this checkpoint
	var timeline: Dictionary = snap.get("event_timeline", {})
	var events_str := ""
	for key in ["first_buy", "first_sell", "first_combat", "first_fo_line", "first_new_system", "first_automation"]:
		if timeline.has(key):
			events_str += "%s@d%d " % [key.trim_prefix("first_"), int(timeline[key])]
	if events_str.length() > 0:
		_a.log("  milestones: %s" % events_str.strip_edges())
	# Log issues
	var cp_issues: Array = snap.get("issues", [])
	if cp_issues.size() > 0:
		_a.log("  --- %s checkpoint issues ---" % phase)
		for iss in cp_issues:
			_a.log("  [%s] %s" % [str(iss.get("severity", "?")), str(iss.get("issue", ""))])
	else:
		_a.log("  No issues at %s checkpoint" % phase)
	_a.log("")


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

func _record_action(cycle: int, type: String, node: String, good: String, qty: int, detail: String, profit: int = 0) -> void:
	_actions.append({"decision": cycle, "type": type, "node": node, "good": good, "qty": qty, "detail": detail, "profit": profit})

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
		_a.fps_set_phase("REPORT")
		return

	var step: Array = TOUR_STEPS[_tour_step]
	var action: String = str(step[0])
	var label: String = str(step[1])

	if action == "done":
		_a.log("VISUAL_TOUR|DONE|steps=%d" % _tour_step)
		_phase = Phase.REPORT
		_a.fps_set_phase("REPORT")
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

	# ---- 60-minute checkpoint (end of run) ----
	_capture_checkpoint("60min")
	if _is_visual:
		_try_capture("checkpoint_60min")

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
	# Growth rate windows — per-window economy acceleration analysis
	_compute_growth_rate_windows()
	_a.log("ECONOMY|growth_trend=%s acceleration=%.3f windows=%d" % [
		_economy_growth_trend, _economy_acceleration, _growth_rate_windows.size()])
	if _growth_rate_windows.size() > 0:
		var rate_str := ""
		for w in _growth_rate_windows:
			if not rate_str.is_empty():
				rate_str += ","
			rate_str += "%.3f" % float(w.get("rate", 0))
		_a.log("ECONOMY|growth_rates=[%s]" % rate_str)

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
	# Longest monotonous streak (with detail)
	var longest_streak := _compute_longest_streak()
	_compute_longest_streak_detail()
	_a.log("PACING|longest_streak=%d type=%s start_d=%d" % [
		longest_streak, _longest_streak_type, _longest_streak_start])
	# FO silence
	var fo_max_silence := _compute_fo_max_silence()
	_a.log("PACING|fo_lines=%d fo_max_silence=%d" % [_fo_dialogue_count, fo_max_silence])
	# FO content exhaustion
	_a.log("FO|unique_tokens=%d repeats=%d last_new_at=%d exhausted_at=%d" % [
		_fo_dialogue_token_ids.size(), _fo_dialogue_repeats,
		_fo_last_new_token_decision, _fo_content_exhausted_at])

	# ---- Dimension 3: Grind Detection ----
	var grind_score := _compute_grind_score()
	_a.log("GRIND|score=%.2f longest_streak=%d streak_type=%s" % [grind_score, longest_streak, _longest_streak_type])
	var route_repeats := _compute_route_repeats()
	_a.log("GRIND|max_route_repeat=%d" % route_repeats)
	var good_repeats := _compute_good_repeats()
	_a.log("GRIND|max_good_repeat=%d" % good_repeats)
	var route_concentration := _compute_route_concentration()
	_a.log("GRIND|route_concentration=%.2f" % route_concentration)
	# Route diversity timeline
	_compute_route_diversity_timeline()
	if _route_diversity_windows.size() > 0:
		var div_str := ""
		for w in _route_diversity_windows:
			if not div_str.is_empty():
				div_str += ","
			div_str += "%.2f" % float(w.get("diversity", 0))
		_a.log("GRIND|diversity_timeline=[%s] collapse_at=%d" % [div_str, _diversity_collapse_decision])

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
	# GATE.T66.BOT.EXPERIENCE_CALIBRATE.001: Tightened from 20/0.15 — T66 economy changes
	# (stronger dampening, novelty bonus, first-visit bonus) make grinding suboptimal faster.
	_a.warn(longest_streak < 15, "no_grinding", "streak=%d" % longest_streak)
	_a.warn(grind_score < 0.10, "grind_score_low", "%.2f" % grind_score)
	_a.warn(max_gap < 80, "no_reward_desert", "max_gap=%d" % max_gap)
	_a.warn(_threat_bands_seen.size() >= 2, "security_gradient", "bands=%d" % _threat_bands_seen.size())
	_a.warn(_profit_per_trade.size() > 0, "trades_completed", "trades=%d" % _profit_per_trade.size())
	if _total_combats >= 3:
		var loot_pct := float(_combat_loot_count) / float(_total_combats) * 100.0
		_a.warn(loot_pct >= 80.0, "combat_loot_rate", "rate=%.0f%% (target>=80%%)" % loot_pct)
	_a.warn(_automations_created >= 2, "multi_automation", "created=%d" % _automations_created)
	_a.warn(_planet_scans_performed > 0 or _visited.size() < 5, "planet_scan_attempted", "scans=%d" % _planet_scans_performed)
	_a.warn(_constructions_started > 0 or not _haven_discovered, "construction_attempted", "started=%d" % _constructions_started)

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

	# ---- Dimension 21: Reward Cadence (NEW) ----
	var reward_cadence_mean := 0.0
	if _reward_gaps.size() > 0:
		var rg_total := 0
		for g in _reward_gaps:
			rg_total += g
		reward_cadence_mean = float(rg_total) / float(_reward_gaps.size())
	_a.log("REWARD_CADENCE|gaps=%d mean=%.1f max=%d" % [
		_reward_gaps.size(), reward_cadence_mean, _max_reward_gap])

	# ---- Dimension 22: Credit Velocity (NEW) ----
	_a.log("CREDIT_VELOCITY|samples=%d stall_max=%d death_spiral_max=%d" % [
		_credit_velocity_samples.size(), _credit_stall_max, _credit_death_spiral_max])
	if _credit_history_available:
		_a.log("CREDIT_VELOCITY|bridge_samples=%d" % _credit_velocity_from_bridge.size())

	# ---- Dimension 23: Hull Timeline (NEW) ----
	_a.log("HULL_TIMELINE|samples=%d below_50=%d below_20=%d never_threatened=%s" % [
		_hull_timeline.size(), _hull_below_50_count, _hull_below_20_count, str(_hull_never_threatened)])

	# ---- Dimension 24: Margin Trend (NEW) ----
	_a.log("MARGIN_TREND|trades=%d declining_streaks=%d worst_loss=%d worst_loss_pct=%.1f%%" % [
		_margin_trend.size(), _declining_margin_streaks, _worst_single_loss, _worst_loss_pct])

	# ---- Dimension 25: FO Reactivity (NEW) ----
	var fo_mean_latency := 0.0
	var fo_reacted := 0
	for r in _fo_react_latencies:
		if int(r.get("latency", -1)) > 0:
			fo_mean_latency += float(r.get("latency", 0))
			fo_reacted += 1
	if fo_reacted > 0:
		fo_mean_latency /= float(fo_reacted)
	_a.log("FO_REACTIVITY|events=%d reacted=%d timeouts=%d mean_latency=%.1f" % [
		_fo_react_latencies.size(), fo_reacted, _fo_react_timeouts, fo_mean_latency])
	_a.log("FO_REACTIVITY|adaptation_events=%d repeats=%d unique_tokens=%d" % [
		_fo_adaptation_events, _fo_dialogue_repeats, _fo_dialogue_token_ids.size()])

	# ---- Dimension 26: Post-Combat Loot (NEW) ----
	var loot_rate := 0.0
	if _total_combats > 0:
		loot_rate = float(_combat_loot_count) / float(_total_combats) * 100.0
	_a.log("COMBAT_LOOT|with_loot=%d without=%d rate=%.0f%%" % [
		_combat_loot_count, _combat_no_loot_count, loot_rate])

	# ---- Dimension 27: Combat Feel (NEW — from GetLastCombatLogV0) ----
	if _combat_round_counts.size() > 0:
		var avg_rounds := 0
		for r in _combat_round_counts:
			avg_rounds += r
		avg_rounds = avg_rounds / _combat_round_counts.size()
		_a.log("COMBAT_FEEL|avg_rounds=%d shield_break_data=%d ttk_data=%d" % [
			avg_rounds, _shield_break_rounds.size(), _combat_ttk_ratios.size()])
		if _shield_break_rounds.size() > 0:
			var sb_avg := 0
			for s in _shield_break_rounds:
				sb_avg += s
			sb_avg = sb_avg / _shield_break_rounds.size()
			_a.log("COMBAT_FEEL|avg_shield_break_round=%d" % sb_avg)
		if _combat_dmg_variance.size() > 0:
			var cv_avg := 0.0
			for v in _combat_dmg_variance:
				cv_avg += v
			cv_avg /= float(_combat_dmg_variance.size())
			_a.log("COMBAT_FEEL|avg_dmg_cv=%.2f" % cv_avg)

	# ---- Dimension 28: Dock System Dump (NEW) ----
	_a.log("DOCK_SYSTEMS|dumps=%d total_docks=%d max_new=%d" % [
		_dock_system_dump_count, _dock_new_systems.size(),
		_dock_new_systems.max() if _dock_new_systems.size() > 0 else 0])

	# ---- Dimension 29: Exploration Motivation (NEW) ----
	var visit_reasons: Dictionary = {}
	for act in _actions:
		if str(act.get("type", "")) == "TRAVEL":
			var detail: String = str(act.get("detail", ""))
			var reason := "unknown"
			if "explore" in detail: reason = "exploration"
			elif "sell" in detail: reason = "trade_sell"
			elif "buy_route" in detail or "buy" in detail or "profit" in detail: reason = "trade_buy"
			elif "combat" in detail or "seeking" in detail: reason = "combat_seek"
			elif "untouched" in detail: reason = "diversity"
			elif "roam" in detail: reason = "roam"
			visit_reasons[reason] = visit_reasons.get(reason, 0) + 1
	_a.log("VISIT_MOTIVATION|reasons=%s" % str(visit_reasons))

	# ---- Dimension 30: Session End Portfolio (NEW) ----
	_a.log("PORTFOLIO|credits=%d modules=%d techs=%d missions_done=%d nodes=%d" % [
		end_credits, _modules_installed, _techs_unlocked,
		_missions_accepted, _visited.size()])

	# ---- Summary Report Card ----
	var avg_ppt := 0
	if _profit_per_trade.size() > 0:
		var ppt_total := 0
		for p in _profit_per_trade:
			ppt_total += p
		avg_ppt = ppt_total / _profit_per_trade.size()
	var feel_active := 0
	var feel_total := 7
	if _is_visual:
		for b in [_feel_credits_flash_seen, _feel_damage_flash_seen, _feel_combat_vignette_seen,
				_feel_combat_banner_seen, _feel_toast_seen, _feel_galaxy_marker_seen]:
			if b: feel_active += 1
		if _feel_shake_intensity_max > 0.0: feel_active += 1
	else:
		# Headless mode: visual runtime probes are N/A — use bridge-verifiable count only
		feel_total = 0  # No visual probes are meaningful headless
	var growth_pct := (net_profit * 100) / maxi(_start_credits, 1)
	var hull_min_r: int = _combat_hull_mins.min() if _combat_hull_mins.size() > 0 else 100

	_a.log("")
	_a.log("================================================================")
	_a.log("  FIRST-HOUR EXPERIENCE REPORT  |  seed=%d  arch=%s  d=%d" % [
		_user_seed, _archetype, _decision])
	_a.log("================================================================")
	_a.log("")

	# --- Dimension verdicts (PASS/WARN/FAIL + key metric) ---
	_a.log("--- DIMENSION VERDICTS ---")
	_a.log("  ECONOMY      %s  growth=%d%% curve=%s idle=%.0f%% sink/faucet=%.2f" % [
		"PASS" if growth_pct >= 100 and idle_pct < 10.0 and curve_shape != "EXPONENTIAL" else ("FAIL" if growth_pct < 50 or idle_pct > 15.0 else "WARN"),
		growth_pct, curve_shape, idle_pct,
		float(_total_spent) / maxf(float(_total_earned), 1.0)])
	_a.log("  PACING       %s  max_gap=%d entropy=%.1f streak=%d rewards=%d" % [
		"PASS" if max_gap <= 30 and entropy >= 1.0 and longest_streak <= 15 else ("FAIL" if max_gap > 50 else "WARN"),
		max_gap, entropy, longest_streak, _reward_events.size()])
	_a.log("  COMBAT       %s  fights=%d kills=%d hull_min=%d%%" % [
		"PASS" if _total_combats > 0 and _total_kills > 0 else ("FAIL" if _total_combats > 0 and _total_kills == 0 else "WARN"),
		_total_combats, _total_kills, hull_min_r])
	_a.log("  EXPLORATION  %s  visited=%.0f%% factions=%d depth=%d" % [
		"PASS" if visit_pct >= 50.0 and _factions_visited.size() >= 2 else ("FAIL" if visit_pct < 20.0 else "WARN"),
		visit_pct, _factions_visited.size(), max_depth])
	_a.log("  GRIND        %s  score=%.2f route_repeat=%d" % [
		"PASS" if grind_score < 0.10 and route_repeats <= 15 else ("FAIL" if grind_score > 0.25 else "WARN"),
		grind_score, route_repeats])
	_a.log("  FO           %s  promoted=%s lines=%d silence=%d" % [
		"PASS" if _fo_promoted and fo_max_silence < 40 else ("FAIL" if not _fo_promoted or fo_max_silence > 60 else "WARN"),
		str(_fo_promoted), _fo_dialogue_count, fo_max_silence])
	_a.log("  DISCLOSURE   %s  systems=%d" % [
		"PASS" if _systems_introduced.size() >= 5 else ("FAIL" if _systems_introduced.size() < 3 else "WARN"),
		_systems_introduced.size()])
	_a.log("  PROGRESSION  %s  milestones=%d avg_ppt=%d" % [
		"PASS" if _milestones_unlocked > 0 else "WARN",
		_milestones_unlocked, avg_ppt])
	_a.log("  MARKET_INTEL %s  alerts=%d shocks=%d" % [
		"PASS" if _market_alerts_seen > 0 else "WARN",
		_market_alerts_seen, _supply_shocks_seen])
	_a.log("  MISSIONS     %s  available=%d accepted=%d bounties=%d" % [
		"PASS" if _missions_available > 0 and _missions_accepted > 0 else ("FAIL" if _missions_available == 0 and _decision > 200 else "WARN"),
		_missions_available, _missions_accepted, _bounties_available])
	_a.log("  FLEET        %s  modules=%d/%d weapons=%d techs=%d/%d" % [
		"PASS" if _modules_installed > 0 and _weapons_equipped > 0 else ("FAIL" if _weapons_equipped == 0 and _total_combats > 0 else "WARN"),
		_modules_installed, _modules_installed + _modules_empty,
		_weapons_equipped, _techs_unlocked, _techs_total])
	_a.log("  SECURITY     %s  bands=%d max_war=%.1f" % [
		"PASS" if _threat_bands_seen.size() >= 2 else "WARN",
		_threat_bands_seen.size(), _max_war_intensity])
	_a.log("  HAVEN        %s  discovered=%s projects=%d" % [
		"PASS" if _haven_discovered else "WARN",
		str(_haven_discovered), _construction_projects])
	_a.log("  NARRATIVE    %s  revelation=%d logs=%d npcs=%d" % [
		"PASS" if _revelation_stage > 0 or _data_logs_found > 0 else "WARN",
		_revelation_stage, _data_logs_found, _narrative_npcs_met])
	_a.log("  COMBAT_DEPTH %s  heat=%.1f doctrine=%s drones=%d" % [
		"PASS" if _doctrine_set and _heat_max > 0.0 else "WARN",
		_heat_max, str(_doctrine_set), _drones_active])
	_a.log("  REWARD_CAD   %s  mean=%.0f max=%d" % [
		"PASS" if _max_reward_gap <= 30 else ("FAIL" if _max_reward_gap > 50 else "WARN"),
		reward_cadence_mean, _max_reward_gap])
	_a.log("  CREDIT_VEL   %s  stall_max=%d spiral=%d" % [
		"PASS" if _credit_stall_max < 5 else ("FAIL" if _credit_stall_max > 10 else "WARN"),
		_credit_stall_max, _credit_death_spiral_count])
	_a.log("  HULL_TENSION %s  below50=%d never_safe=%s" % [
		"PASS" if not _hull_never_threatened else ("WARN" if _total_combats > 5 else "PASS"),
		_hull_below_50_count, str(not _hull_never_threatened)])
	_a.log("  MARGIN_TREND %s  declines=%d worst_loss_pct=%.0f%%" % [
		"PASS" if _declining_margin_streaks <= 2 else "WARN",
		_declining_margin_streaks, _worst_loss_pct])
	_a.log("  FO_REACTIVE  %s  timeouts=%d mean_lat=%.0f repeats=%d" % [
		"PASS" if _fo_react_timeouts <= 1 else ("FAIL" if _fo_react_timeouts > 3 else "WARN"),
		_fo_react_timeouts, fo_mean_latency, _fo_dialogue_repeats])
	_a.log("  COMBAT_LOOT  %s  rate=%.0f%% no_loot=%d" % [
		"PASS" if loot_rate >= 80.0 or _total_combats == 0 else ("FAIL" if loot_rate < 50.0 else "WARN"),
		loot_rate, _combat_no_loot_count])
	_a.log("  DOCK_DUMP    %s  dumps=%d" % [
		"PASS" if _dock_system_dump_count == 0 else "WARN",
		_dock_system_dump_count])
	_a.log("")

	# --- System engagement (what the bot tried vs succeeded) ---
	_a.log("--- SYSTEM ENGAGEMENT ---")
	var _eng := func(name: String, attempted: bool, succeeded: bool) -> String:
		if succeeded: return "  %-16s EXERCISED" % name
		elif attempted: return "  %-16s ATTEMPTED (failed)" % name
		else: return "  %-16s NOT REACHED" % name
	_a.log(_eng.call("Trading", _total_buys > 0, _total_sells > 0))
	_a.log(_eng.call("Combat", COMBAT_ENABLED, _total_kills > 0))
	_a.log(_eng.call("Exploration", true, _visited.size() >= 3))
	_a.log(_eng.call("Modules", _modules_attempted > 0, _modules_installed_by_bot > 0))
	_a.log(_eng.call("Research", _research_attempted, _research_started_by_bot))
	_a.log(_eng.call("Missions", _missions_attempted > 0, _missions_accepted_by_bot > 0))
	_a.log(_eng.call("Doctrine", _doctrine_attempted, _doctrine_set_by_bot))
	_a.log(_eng.call("Automation", _automation_attempted, _automation_created))
	_a.log(_eng.call("Haven", _haven_discovered, _construction_projects > 0))
	_a.log(_eng.call("Discoveries", _discoveries_scanned > 0, _discoveries_found > 0))
	_a.log(_eng.call("Fragments", _fragments_collected > 0, _fragments_found > 0))
	_a.log(_eng.call("Diplomacy", _bounties_available > 0, false))  # no bounty completion yet
	_a.log(_eng.call("Market Intel", _market_alerts_seen > 0, _supply_shocks_seen > 0))
	_a.log(_eng.call("Knowledge", _knowledge_entries > 0, _knowledge_entries > 0))
	_a.log(_eng.call("Planet Scan", _visited.size() >= 5, _planet_scans_performed > 0))
	_a.log(_eng.call("Fracture", _visited.size() >= 15, _fracture_travels > 0))
	_a.log(_eng.call("Construction", _haven_discovered, _constructions_started > 0))
	_a.log(_eng.call("Haven Upgrade", _haven_discovered, _haven_upgraded))
	_a.log(_eng.call("Fabrication", _haven_discovered, _fabrications_started > 0))
	_a.log(_eng.call("Megaproject", _haven_discovered and _decision >= 200, _megaprojects_started > 0))
	_a.log(_eng.call("Prog Monitor", _automations_created > 0, _program_monitoring_count > 0))
	_a.log("")

	# --- Key events timeline ---
	_a.log("--- KEY EVENTS TIMELINE ---")
	_a.log("  first_buy=%d  first_sell=%d  first_combat=%d  first_fo=%d  first_new_system=%d" % [
		_event_decisions.get("first_buy", -1), _event_decisions.get("first_sell", -1),
		_event_decisions.get("first_combat", -1), _event_decisions.get("first_fo_line", -1),
		_event_decisions.get("first_new_system", -1)])
	_a.log("")

	# --- Gaps (systems never exercised) ---
	var gaps: Array = []
	if _missions_accepted_by_bot == 0: gaps.append("MISSIONS (never accepted)")
	if _discoveries_found == 0: gaps.append("DISCOVERIES (none found)")
	if _knowledge_entries == 0: gaps.append("KNOWLEDGE_GRAPH (empty)")
	if _data_logs_found == 0: gaps.append("DATA_LOGS (none found)")
	if _narrative_npcs_met == 0: gaps.append("NARRATIVE_NPCS (none met)")
	if _milestones_unlocked == 0: gaps.append("MILESTONES (none unlocked)")
	if _market_alerts_seen == 0: gaps.append("MARKET_ALERTS (none seen)")
	if _supply_shocks_seen == 0: gaps.append("SUPPLY_SHOCKS (none seen)")
	if not _haven_discovered: gaps.append("HAVEN (not discovered)")
	if _revelation_stage == 0: gaps.append("REVELATION (stage 0)")
	if not _fracture_accessible: gaps.append("FRACTURE (not accessible)")
	if _endgame_paths_revealed == 0: gaps.append("ENDGAME_PATHS (none revealed)")
	if not _automation_created: gaps.append("AUTOMATION (not created)")
	if _automations_created < 2: gaps.append("MULTI_AUTOMATION (only %d types)" % _automations_created)
	if _planet_scans_performed == 0 and _visited.size() >= 5: gaps.append("PLANET_SCAN (never performed)")
	if _fracture_travels == 0 and _visited.size() >= 15: gaps.append("FRACTURE_TRAVEL (never used)")
	if _constructions_started == 0 and _haven_discovered: gaps.append("CONSTRUCTION (never started)")
	if not _haven_upgraded and _haven_discovered: gaps.append("HAVEN_UPGRADE (not upgraded)")
	if _fabrications_started == 0 and _haven_discovered: gaps.append("FABRICATION (never started)")
	if _megaprojects_started == 0 and _haven_discovered and _decision >= 200: gaps.append("MEGAPROJECT (never started)")
	if _drones_active == 0: gaps.append("DRONES (none active)")
	if _commissions_active == 0: gaps.append("COMMISSIONS (none active)")
	if gaps.size() > 0:
		_a.log("--- GAPS (%d systems not exercised) ---" % gaps.size())
		for g in gaps:
			_a.log("  GAP: %s" % g)
	else:
		_a.log("--- GAPS: None! All systems exercised ---")
	_a.log("")

	# --- SCORE lines (machine-parseable, kept for backward compat) ---
	_a.log("SCORE|ECONOMY|growth=%d%% goods=%d/%d idle=%.0f%% curve=%s" % [
		growth_pct, _goods_bought.size() + _goods_sold.size(), all_goods.size(),
		idle_pct, curve_shape])
	_a.log("SCORE|PACING|rewards=%d mean_gap=%.0f max_gap=%d entropy=%.1f streak=%d" % [
		_reward_events.size(), mean_gap, max_gap, entropy, longest_streak])
	_a.log("SCORE|COMBAT|count=%d kills=%d hull_min=%d%%" % [_total_combats, _total_kills, hull_min_r])
	_a.log("SCORE|EXPLORE|visited=%.0f%% factions=%d depth=%d backtrack=%.0f%%" % [
		visit_pct, _factions_visited.size(), max_depth, backtrack_pct])
	_a.log("SCORE|GRIND|score=%.2f route_repeat=%d good_repeat=%d concentration=%.2f" % [grind_score, route_repeats, good_repeats, route_concentration])
	_a.log("SCORE|FO|promoted=%s lines=%d max_silence=%d" % [str(_fo_promoted), _fo_dialogue_count, fo_max_silence])
	_a.log("SCORE|DISCLOSURE|systems=%d" % _systems_introduced.size())
	_a.log("SCORE|PROGRESSION|milestones=%d endgame_paths=%d avg_profit_trade=%d" % [
		_milestones_unlocked, _endgame_paths_revealed, avg_ppt])
	_a.log("SCORE|MARKET_INTEL|alerts=%d shocks=%d spreads=%d instability_skips=%d" % [
		_market_alerts_seen, _supply_shocks_seen, _price_spreads.size(), _sell_rejections_instability])
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
	_a.log("SCORE|REWARD_CADENCE|mean=%.0f max=%d gaps=%d" % [
		reward_cadence_mean, _max_reward_gap, _reward_gaps.size()])
	_a.log("SCORE|CREDIT_VELOCITY|stall_max=%d death_spiral=%d" % [
		_credit_stall_max, _credit_death_spiral_count])
	_a.log("SCORE|HULL_TIMELINE|below50=%d below20=%d never_threatened=%s" % [
		_hull_below_50_count, _hull_below_20_count, str(_hull_never_threatened)])
	_a.log("SCORE|MARGIN_TREND|declining_streaks=%d worst_loss=%d worst_pct=%.0f" % [
		_declining_margin_streaks, _worst_single_loss, _worst_loss_pct])
	_a.log("SCORE|FO_REACTIVITY|timeouts=%d mean_latency=%.0f repeats=%d adapt_readiness=%d" % [
		_fo_react_timeouts, fo_mean_latency, _fo_dialogue_repeats, _fo_adaptation_events])
	_a.log("SCORE|COMBAT_LOOT|with=%d without=%d rate=%.0f%%" % [
		_combat_loot_count, _combat_no_loot_count, loot_rate])
	_a.log("SCORE|COMBAT_FEEL|round_data=%d shield_breaks=%d ttk_data=%d dmg_cv=%d" % [
		_combat_round_counts.size(), _shield_break_rounds.size(),
		_combat_ttk_ratios.size(), _combat_dmg_variance.size()])
	_a.log("SCORE|DOCK_DUMP|dumps=%d max_new=%d" % [
		_dock_system_dump_count,
		_dock_new_systems.max() if _dock_new_systems.size() > 0 else 0])
	_a.log("SCORE|VISIT_MOTIVATION|reasons=%s" % str(visit_reasons))
	_a.log("SCORE|FEEL_PRESENCE|active=%d/%d mode=%s" % [feel_active, feel_total, "visual" if _is_visual else "headless"])
	_a.log("SCORE|ENGAGEMENT|missions=%d/%d modules=%d/%d doctrine=%s research=%s scans=%d frags=%d auto=%s" % [
		_missions_accepted_by_bot, _missions_attempted,
		_modules_installed_by_bot, _modules_attempted,
		str(_doctrine_set_by_bot), str(_research_started_by_bot),
		_discoveries_scanned, _fragments_collected, str(_automation_created)])
	var auto_net := _automation_total_earned - _automation_total_expense
	_a.log("SCORE|PROGRAMS|created=%d types=%s monitoring=%d earned=%d expense=%d net=%d" % [
		_automations_created, str(_automation_types_created.keys()), _program_monitoring_count,
		_automation_total_earned, _automation_total_expense, auto_net])
	_a.log("SCORE|FRACTURE|travels=%d" % _fracture_travels)
	_a.log("SCORE|PLANET_SCAN|scans=%d" % _planet_scans_performed)
	_a.log("SCORE|CONSTRUCTION|started=%d" % _constructions_started)
	_a.log("SCORE|HAVEN_DEPTH|upgraded=%s fabrications=%d" % [str(_haven_upgraded), _fabrications_started])
	_a.log("SCORE|MEGAPROJECT|started=%d" % _megaprojects_started)
	_a.log("================================================================")

	# ---- EXPERIENCE ARC — phased checkpoint summary ----
	if _checkpoints.size() > 0:
		_a.log("")
		_a.log("╔══════════════════════════════════════════════════════════════╗")
		_a.log("║            PLAYER EXPERIENCE ARC (10/30/60 min)             ║")
		_a.log("╠══════════════════════════════════════════════════════════════╣")
		_a.log("║  Phase   │ Orient │ Master │ Engage │  Feel  │ Progrs │ AVG ║")
		_a.log("║──────────┼────────┼────────┼────────┼────────┼────────┼─────║")
		for cp in _checkpoints:
			_a.log("║  %-7s │  %d/5   │  %d/5   │  %d/5   │  %d/5   │  %d/5   │ %.1f ║" % [
				str(cp.get("phase", "?")),
				int(cp.get("orientation", 0)), int(cp.get("mastery", 0)),
				int(cp.get("engagement", 0)), int(cp.get("feel", 0)),
				int(cp.get("progression", 0)), float(cp.get("overall", 0.0))])
		_a.log("╚══════════════════════════════════════════════════════════════╝")
		# Arc trend: is the experience improving over time?
		if _checkpoints.size() >= 2:
			var first_overall: float = float(_checkpoints[0].get("overall", 0.0))
			var last_overall: float = float(_checkpoints[-1].get("overall", 0.0))
			var arc_trend := "CRESCENDO" if last_overall > first_overall + 0.5 else ("FLAT" if absf(last_overall - first_overall) <= 0.5 else "DECLINING")
			_a.log("  ARC_TREND: %s (%.1f → %.1f)" % [arc_trend, first_overall, last_overall])
			_a.log("SCORE|EXPERIENCE_ARC|trend=%s first=%.1f last=%.1f checkpoints=%d" % [
				arc_trend, first_overall, last_overall, _checkpoints.size()])
		# Count total checkpoint issues
		var cp_issue_count := 0
		var cp_critical := 0
		for cp in _checkpoints:
			for iss in cp.get("issues", []):
				cp_issue_count += 1
				if str(iss.get("severity", "")) == "CRITICAL":
					cp_critical += 1
		if cp_issue_count > 0:
			_a.log("  CHECKPOINT_ISSUES: %d total (%d critical)" % [cp_issue_count, cp_critical])
		_a.log("")

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
	var crit_count := 0
	var major_count := 0
	var minor_count := 0
	for issue in issues:
		match str(issue.get("severity", "")):
			"CRITICAL": crit_count += 1
			"MAJOR": major_count += 1
			_: minor_count += 1
	_a.log("================================================================")
	_a.log("  ISSUES: %d total  |  %d CRITICAL  %d MAJOR  %d MINOR" % [
		issues.size(), crit_count, major_count, minor_count])
	_a.log("================================================================")
	var issue_num := 0
	for issue in issues:
		issue_num += 1
		var sev: String = str(issue.get("severity", ""))
		var cat: String = str(issue.get("category", ""))
		var desc: String = str(issue.get("description", ""))
		var fix: String = str(issue.get("prescription", ""))
		var file: String = str(issue.get("file", ""))
		_a.log("  #%d [%s] %s" % [issue_num, sev, cat])
		_a.log("     %s" % desc)
		_a.log("     Fix: %s" % fix)
		_a.log("     File: %s" % file)
		# Keep machine-parseable line for audit compatibility
		_a.log("ISSUE|%s|%s|%s|fix=%s|file=%s" % [sev, cat, desc, fix, file])
	_a.log("ISSUE_SUMMARY|critical=%d major=%d minor=%d total=%d" % [
		crit_count, major_count, minor_count, issues.size()])
	_a.log("================================================================")

	# ---- Dead zone analysis ----
	var dead_zones: Array[Dictionary] = _a.analyze_dead_zones(50)
	for dz in dead_zones:
		_a.log("DEAD_ZONE|start=d%d|end=d%d|gap=%d" % [dz["start"], dz["end"], dz["gap"]])
	_a.warn(dead_zones.size() <= 2, "dead_zone_count", "zones=%d threshold=50" % dead_zones.size())

	# ---- FPS profiling report ----
	_a.fps_report()

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


## Compute route diversity: what fraction of total trades happen on the top 2 routes.
## Returns 0.0-1.0 where 1.0 means ALL trades on 2 routes (bad diversity).
func _compute_route_concentration() -> float:
	var node_trade_counts: Dictionary = {}
	for a in _actions:
		var atype: String = str(a.get("type", ""))
		if atype == "BUY" or atype == "SELL":
			var node: String = str(a.get("node", ""))
			if not node.is_empty():
				node_trade_counts[node] = node_trade_counts.get(node, 0) + 1
	var total_trades := 0
	var sorted_counts: Array = []
	for n in node_trade_counts:
		var c: int = int(node_trade_counts[n])
		total_trades += c
		sorted_counts.append(c)
	if total_trades < 5 or sorted_counts.size() < 2:
		return 0.0  # Not enough data
	sorted_counts.sort()
	sorted_counts.reverse()
	# Top 2 nodes' share of all trades
	var top2: int = sorted_counts[0] + sorted_counts[1]
	return float(top2) / float(total_trades)


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


## Economy growth rate per 50-decision window — shows WHERE acceleration happens.
func _compute_growth_rate_windows() -> void:
	_growth_rate_windows.clear()
	if _credit_trajectory.size() < 10:
		_economy_growth_trend = "INSUFFICIENT_DATA"
		return
	var window_size := 15  # ~15 samples per window → 5+ windows from ~77 samples
	var n := _credit_trajectory.size()
	# Sample credit trajectory at window boundaries
	var window_idx := 0
	var i := 0
	while i + window_size <= n:
		var start_val := float(_credit_trajectory[i])
		var end_val := float(_credit_trajectory[mini(i + window_size - 1, n - 1)])
		var rate := (end_val - start_val) / maxf(start_val, 100.0)  # growth rate as fraction
		_growth_rate_windows.append({
			"window": window_idx,
			"start_credits": int(start_val),
			"end_credits": int(end_val),
			"rate": snapped(rate, 0.001),
		})
		window_idx += 1
		i += window_size
	# Compute acceleration (rate of change of growth rates)
	if _growth_rate_windows.size() >= 3:
		var early_rates: Array = []
		var late_rates: Array = []
		var half := _growth_rate_windows.size() / 2
		for j in range(half):
			early_rates.append(float(_growth_rate_windows[j].get("rate", 0)))
		for j in range(half, _growth_rate_windows.size()):
			late_rates.append(float(_growth_rate_windows[j].get("rate", 0)))
		var early_avg := 0.0
		for r in early_rates:
			early_avg += r
		early_avg /= maxf(float(early_rates.size()), 1.0)
		var late_avg := 0.0
		for r in late_rates:
			late_avg += r
		late_avg /= maxf(float(late_rates.size()), 1.0)
		_economy_acceleration = late_avg - early_avg
		if _economy_acceleration > 0.05:
			_economy_growth_trend = "ACCELERATING"
		elif _economy_acceleration < -0.05:
			_economy_growth_trend = "DECELERATING"
		else:
			# Check variance to distinguish STEADY from VOLATILE
			var all_rates: Array = []
			for w in _growth_rate_windows:
				all_rates.append(float(w.get("rate", 0)))
			var mean_rate := 0.0
			for r in all_rates:
				mean_rate += r
			mean_rate /= maxf(float(all_rates.size()), 1.0)
			var variance := 0.0
			for r in all_rates:
				variance += (r - mean_rate) * (r - mean_rate)
			variance /= maxf(float(all_rates.size()), 1.0)
			if variance > 0.01:
				_economy_growth_trend = "VOLATILE"
			else:
				_economy_growth_trend = "STEADY"


## Enhanced longest streak with action type detail.
func _compute_longest_streak_detail() -> void:
	if _action_type_history.size() == 0:
		return
	var longest := 1
	var current := 1
	var streak_type: String = _action_type_history[0]
	var streak_start := 0
	var best_type: String = _action_type_history[0]
	var best_start := 0
	for i in range(1, _action_type_history.size()):
		if _action_type_history[i] == _action_type_history[i - 1]:
			current += 1
			if current > longest:
				longest = current
				best_type = str(_action_type_history[i])
				best_start = streak_start
		else:
			current = 1
			streak_start = i
	_longest_streak_type = best_type
	_longest_streak_start = best_start


## Route diversity per 100-decision window — detects when diversity collapses.
func _compute_route_diversity_timeline() -> void:
	_route_diversity_windows.clear()
	var window_size := 100
	var window_idx := 0
	var i := 0
	while i < _actions.size():
		var unique_routes: Dictionary = {}
		var trade_count := 0
		var last_loc := ""
		var j := i
		while j < _actions.size() and j < i + window_size:
			var atype: String = str(_actions[j].get("type", ""))
			if atype == "TRAVEL":
				var dest := str(_actions[j].get("node", ""))
				if not last_loc.is_empty() and not dest.is_empty():
					var route := "%s>%s" % [last_loc, dest]
					unique_routes[route] = true
				last_loc = dest
			elif atype == "BUY" or atype == "SELL":
				trade_count += 1
				last_loc = str(_actions[j].get("node", ""))
			j += 1
		var diversity := float(unique_routes.size()) / maxf(float(trade_count), 1.0)
		_route_diversity_windows.append({
			"window": window_idx,
			"unique_routes": unique_routes.size(),
			"total_trades": trade_count,
			"diversity": snapped(diversity, 0.01),
		})
		if _diversity_collapse_decision < 0 and diversity < 0.3 and trade_count >= 5:
			_diversity_collapse_decision = window_idx * window_size
		window_idx += 1
		i += window_size


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
	if not _bridge.has_method("GetKnowledgeGraphV0"):
		return
	var entries = _bridge.call("GetKnowledgeGraphV0")
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

	# ---- New research-based probes ----
	_probe_cognitive_load()
	_probe_dead_end_detection()
	_probe_retention_signals()
	_probe_pacing_rhythm()
	_probe_valence_arc()
	_probe_competence()


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

	# --- Headless bridge-verified feel events (from GetRecentHudEventsV0) ---
	if not _is_visual and _bridge.has_method("GetRecentHudEventsV0"):
		var hud_events = _bridge.call("GetRecentHudEventsV0")
		if hud_events is Array:
			var bridge_feel_types: Dictionary = {}
			for ev in hud_events:
				if ev is Dictionary:
					var etype: String = ev.get("event_type", "")
					if not etype.is_empty():
						bridge_feel_types[etype] = bridge_feel_types.get(etype, 0) + 1
			_a.log("FEEL_BRIDGE|events=%d types=%s" % [hud_events.size(), str(bridge_feel_types)])
			for etype in bridge_feel_types:
				_a.log("FEEL_BRIDGE|%s=%d" % [etype, bridge_feel_types[etype]])

	# --- Issue detection ---
	if not _is_visual:
		_a.log("FEEL_PRESENCE|mode=headless (runtime VFX checks N/A — excluded from scoring)")
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


# ---- Valence-Arousal Event Recording ----
# Call this whenever a significant game event occurs during the play loop.
# Classifies the event and records it for emotional arc analysis.
func _record_valence_event(event_type: String, decision: int) -> void:
	var valence := 0   # +1 positive, -1 negative, 0 neutral
	var arousal := 0   # 2=HIGH, 1=MEDIUM, 0=LOW

	match event_type:
		"first_profit", "profitable_sell", "discovery", "upgrade", "milestone", "automation_created":
			valence = 1; arousal = 2
		"new_station_dock", "mission_accept", "fo_dialogue", "price_alert":
			valence = 1; arousal = 1
		"travel", "undock", "idle":
			valence = 0; arousal = 0
		"loss_trade", "hull_damage", "death", "credit_decrease":
			valence = -1; arousal = 2
		"combat_start":
			valence = -1; arousal = 2
		"combat_victory":
			valence = 1; arousal = 2
		_:
			valence = 0; arousal = 1

	_valence_samples.append({"decision": decision, "valence": valence, "arousal": arousal, "event": event_type})

	# Update running average (EMA with alpha=0.2)
	var prev_avg := _valence_running_avg
	_valence_running_avg = _valence_running_avg * 0.8 + float(valence) * 0.2

	# Detect zero-crossing
	if (prev_avg > 0.0 and _valence_running_avg <= 0.0) or (prev_avg < 0.0 and _valence_running_avg >= 0.0):
		_valence_crossings += 1

	# Wonder detection: HIGH + positive
	if valence > 0 and arousal == 2:
		_wonder_count += 1

	# Catharsis detection: HIGH+negative followed by positive within 30 decisions
	if valence < 0 and arousal == 2:
		_last_negative_arousal_d = decision
	if valence > 0 and arousal >= 1 and _last_negative_arousal_d >= 0:
		if decision - _last_negative_arousal_d <= 30:
			_catharsis_count += 1
			_last_negative_arousal_d = -1  # consume

	# Track HIGH events for rhythm analysis
	if arousal == 2:
		_high_event_decisions.append(decision)
	elif arousal == 1:
		_medium_event_decisions.append(decision)


# ---- Cognitive Load Probe ----
func _probe_cognitive_load() -> void:
	var tabs_first := _tabs_at_first_dock if _tabs_at_first_dock >= 0 else 0
	var max_systems_per_dock: int = int(_systems_per_dock.max()) if _systems_per_dock.size() > 0 else 0
	var avg_fo_words := 0.0
	if _fo_word_counts.size() > 0:
		var total_words := 0
		for w in _fo_word_counts:
			total_words += w
		avg_fo_words = float(total_words) / float(_fo_word_counts.size())
	var max_fo_words: int = int(_fo_word_counts.max()) if _fo_word_counts.size() > 0 else 0

	_a.log("SCORE|COGNITIVE_LOAD|tabs_first_dock=%d max_systems_per_dock=%d avg_fo_words=%.0f max_fo_words=%d dock_count=%d" % [
		tabs_first, max_systems_per_dock, avg_fo_words, max_fo_words, _tabs_per_dock.size()])


# ---- Dead-End / Confusion Probe ----
func _probe_dead_end_detection() -> void:
	_a.log("SCORE|DEAD_END|trap_states=%d min_viable_actions=%d action_reversals=%d" % [
		_trap_state_count, _min_viable_actions, _action_reversals])
	if _trap_state_decisions.size() > 0:
		_a.log("SCORE|DEAD_END|trap_decisions=%s" % str(_trap_state_decisions))


# ---- Retention Signals Probe ----
func _probe_retention_signals() -> void:
	# Compute action rate windows
	var window_size := 50
	var total_d := _decision
	var windows_computed := 0
	for w_start in range(0, total_d, window_size):
		var w_end := w_start + window_size
		var count := 0
		for act in _actions:
			var act_d: int = int(act.get("decision", 0))
			if act_d >= w_start and act_d < w_end:
				count += 1
		_action_rate_windows.append(count)
		windows_computed += 1

	# Check for declining action rate (last 4+ windows declining)
	if _action_rate_windows.size() >= 4:
		var declining_count := 0
		for i in range(_action_rate_windows.size() - 1, 0, -1):
			if _action_rate_windows[i] < _action_rate_windows[i - 1]:
				declining_count += 1
			else:
				break
		_action_rate_declining = declining_count >= 4

	_a.log("SCORE|RETENTION|first_profit_d=%d core_loop_d=%d aha_moment_d=%d trap_states=%d action_declining=%s" % [
		_first_profit_decision, _core_loop_decision, _aha_moment_decision,
		_trap_state_count, str(_action_rate_declining)])


# ---- Pacing Rhythm Probe ----
func _probe_pacing_rhythm() -> void:
	# Compute inter-beat intervals (between HIGH+MEDIUM events)
	var all_beats: Array = []
	for d in _high_event_decisions:
		all_beats.append(d)
	for d in _medium_event_decisions:
		all_beats.append(d)
	all_beats.sort()

	_beat_intervals.clear()
	for i in range(1, all_beats.size()):
		_beat_intervals.append(all_beats[i] - all_beats[i - 1])

	# Beat density per 100 decisions
	var density_per_100 := 0.0
	if _decision > 0:
		density_per_100 = float(_high_event_decisions.size()) / float(_decision) * 100.0

	# Coefficient of variation of inter-beat intervals
	var beat_cov := 0.0
	if _beat_intervals.size() >= 2:
		var mean_interval := 0.0
		for bi in _beat_intervals:
			mean_interval += float(bi)
		mean_interval /= float(_beat_intervals.size())
		var variance := 0.0
		for bi in _beat_intervals:
			variance += (float(bi) - mean_interval) * (float(bi) - mean_interval)
		variance /= float(_beat_intervals.size())
		var stddev := sqrt(variance)
		if mean_interval > 0.0:
			beat_cov = stddev / mean_interval

	_a.log("SCORE|PACING_RHYTHM|high_events=%d medium_events=%d density_per_100=%.1f beat_interval_cov=%.2f valence_crossings=%d" % [
		_high_event_decisions.size(), _medium_event_decisions.size(),
		density_per_100, beat_cov, _valence_crossings])


# ---- Valence-Arousal Arc Probe ----
func _probe_valence_arc() -> void:
	var emotional_range := 0.0
	if _valence_samples.size() >= 2:
		var min_v := 999.0
		var max_v := -999.0
		# Compute running avg at each point
		var running := 0.0
		for vs in _valence_samples:
			running = running * 0.8 + float(vs.get("valence", 0)) * 0.2
			min_v = minf(min_v, running)
			max_v = maxf(max_v, running)
		emotional_range = max_v - min_v

	_a.log("SCORE|VALENCE_ARC|samples=%d crossings=%d catharsis=%d wonder=%d emotional_range=%.2f" % [
		_valence_samples.size(), _valence_crossings, _catharsis_count,
		_wonder_count, emotional_range])


# ---- Competence / Mastery Probe ----
func _probe_competence() -> void:
	# Compare early vs late margins
	var early_avg := 0.0
	if _early_margins.size() > 0:
		var total := 0.0
		for m in _early_margins:
			total += float(m)
		early_avg = total / float(_early_margins.size())
	var late_avg := 0.0
	if _late_margins.size() > 0:
		var total := 0.0
		for m in _late_margins:
			total += float(m)
		late_avg = total / float(_late_margins.size())
	var margin_improvement := late_avg - early_avg

	# Compare early vs late kill rate
	var early_combats := mini(_total_combats, 3)
	var late_combats := mini(_total_combats, 3)
	# These are already tracked in _early_kill_rate / _late_kill_rate

	# Credits per decision efficiency
	if _decision > 200:
		var early_earned := 0
		var late_earned := 0
		for act in _actions:
			var act_d: int = int(act.get("decision", 0))
			var profit: int = int(act.get("profit", 0))
			if act_d < 200:
				early_earned += profit
			elif act_d >= _decision - 200:
				late_earned += profit
		_credits_per_decision_early = float(early_earned) / 200.0
		_credits_per_decision_late = float(late_earned) / 200.0

	_a.log("SCORE|COMPETENCE|early_margin=%.0f late_margin=%.0f improvement=%.0f early_kill=%.2f late_kill=%.2f milestone_toasts=%d" % [
		early_avg, late_avg, margin_improvement, _early_kill_rate, _late_kill_rate, _milestone_toasts])
	_a.log("SCORE|COMPETENCE|credits_per_d_early=%.1f credits_per_d_late=%.1f" % [
		_credits_per_decision_early, _credits_per_decision_late])


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
	if grind_score > 0.3:
		issues.append({
			"severity": "CRITICAL", "category": "GRIND",
			"description": "Grind score %.2f — player stuck in repetitive buy-travel-sell-travel loop (3+ repeats)" % grind_score,
			"prescription": "Add diminishing returns on repeated routes, increase exploration incentives",
			"file": "SimCore/Systems/MarketSystem.cs"
		})
	elif grind_score > 0.15:
		issues.append({
			"severity": "MAJOR", "category": "GRIND",
			"description": "Grind score %.2f — early grind pattern detected (2+ repeats)" % grind_score,
			"prescription": "Add diminishing returns on repeated routes, increase exploration incentives",
			"file": "SimCore/Systems/MarketSystem.cs"
		})
	if route_repeats > 20:
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
	var route_conc := _compute_route_concentration()
	if route_conc > 0.6:
		issues.append({
			"severity": "MAJOR", "category": "GRIND",
			"description": "Route concentration %.0f%% — over 60%% of trades happen at just 2 nodes" % (route_conc * 100),
			"prescription": "Add price dampening on high-volume nodes, or create trade opportunities at distant stations",
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
	if fo_max_silence > 60 and _fo_promoted:
		issues.append({
			"severity": "MAJOR", "category": "FO",
			"description": "FO silent for %d decisions — companion feels absent (industry standard: 30-50 max)" % fo_max_silence,
			"prescription": "Add FO ambient commentary: market tips, combat warnings, exploration encouragement. Target: speak every 30-50 decisions",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})
	elif fo_max_silence > 40 and _fo_promoted:
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
	# Check if FO only speaks during tutorial and goes silent post-graduation
	if _fo_dialogue_decisions.size() > 0 and _fo_promoted:
		var last_fo_decision: int = int(_fo_dialogue_decisions[-1])
		if last_fo_decision < 100 and _decision > 300:
			issues.append({
				"severity": "MAJOR", "category": "FO",
				"description": "FO last spoke at d=%d but session is at d=%d — FO silent post-tutorial" % [last_fo_decision, _decision],
				"prescription": "FO needs ambient cadence system: comment on new territory, combat outcomes, trade milestones, idle periods",
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
	if _revelation_stage == 0 and _decision >= 500 and _fo_competence_tier >= 2:
		issues.append({
			"severity": "MINOR", "category": "NARRATIVE",
			"description": "Revelation stage 0 after %d decisions with FO tier %d — main story not progressing" % [_decision, _fo_competence_tier],
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

	# ---- REWARD CADENCE (gap #1) ----
	if _max_reward_gap > 50:
		issues.append({
			"severity": "CRITICAL", "category": "REWARD_CADENCE",
			"description": "Reward drought of %d decisions — exceeds 50-decision industry threshold for dead time" % _max_reward_gap,
			"prescription": "Add micro-rewards every 30 decisions: FO comments, scan pings, price alerts, lore fragments",
			"file": "SimCore/Systems/FirstOfficerSystem.cs OR SimCore/Systems/AdaptationFragmentSystem.cs"
		})
	elif _max_reward_gap > 30:
		issues.append({
			"severity": "MAJOR", "category": "REWARD_CADENCE",
			"description": "Max reward gap %d decisions — approaching boredom threshold (industry: 30-90s minor, 5-10min major)" % _max_reward_gap,
			"prescription": "Inject ambient rewards in gaps: market alerts, NPC encounters, discovery pings",
			"file": "SimCore/Systems/AdaptationFragmentSystem.cs"
		})

	# ---- POST-COMBAT LOOT (gap #2) ----
	if _total_combats > 3 and _combat_no_loot_count > 0:
		var no_loot_rate := float(_combat_no_loot_count) / float(_total_combats) * 100.0
		if no_loot_rate > 50.0:
			issues.append({
				"severity": "MAJOR", "category": "COMBAT_LOOT",
				"description": "%.0f%% of combats yield no reward — fighting feels unrewarding (industry: 100%% should drop something)" % no_loot_rate,
				"prescription": "Guarantee salvage/credits/intel drop on every combat victory (FTL pattern: always loot)",
				"file": "SimCore/Systems/NpcFleetCombatSystem.cs OR SimCore/Systems/LootSystem.cs"
			})
		elif no_loot_rate > 20.0:
			issues.append({
				"severity": "MINOR", "category": "COMBAT_LOOT",
				"description": "%.0f%% of combats yield no reward — some fights feel pointless" % no_loot_rate,
				"prescription": "Add minimum salvage drop to all combat victories",
				"file": "SimCore/Systems/NpcFleetCombatSystem.cs"
			})

	# ---- CREDIT VELOCITY (gap #3) ----
	if _credit_stall_max > 10:
		issues.append({
			"severity": "MAJOR", "category": "CREDIT_VELOCITY",
			"description": "Credit velocity stalled for %d consecutive windows — economy feels frozen" % _credit_stall_max,
			"prescription": "Detect credit stalls and inject opportunities: FO tips about nearby profitable trades, emergency missions",
			"file": "SimCore/Systems/FirstOfficerSystem.cs OR SimCore/Systems/MissionTemplateSystem.cs"
		})
	if _credit_death_spiral_max > 5:
		issues.append({
			"severity": "CRITICAL", "category": "CREDIT_VELOCITY",
			"description": "Credit death spiral — %d consecutive negative-velocity windows" % _credit_death_spiral_max,
			"prescription": "Add safety net: reduce upkeep when credits low, offer emergency trade mission, FO warns about spending",
			"file": "SimCore/Tweaks/FleetUpkeepTweaksV0.cs OR SimCore/Systems/MissionSystem.cs"
		})

	# ---- FO REACTIVITY (gap #4) ----
	if _fo_react_timeouts > 3 and _fo_promoted:
		issues.append({
			"severity": "MAJOR", "category": "FO_REACTIVITY",
			"description": "FO failed to react to %d significant events — companion feels disconnected" % _fo_react_timeouts,
			"prescription": "Add FO reactive triggers: post-combat commentary, first-trade acknowledgment, new-system observation",
			"file": "SimCore/Systems/FirstOfficerSystem.cs OR SimCore/Content/FirstOfficerContentV0.cs"
		})
	if _fo_dialogue_repeats > 5:
		issues.append({
			"severity": "MINOR", "category": "FO_REACTIVITY",
			"description": "FO repeated %d dialogue tokens — companion feels scripted" % _fo_dialogue_repeats,
			"prescription": "Add more dialogue variety per trigger type; track recently-used tokens and avoid repeats",
			"file": "SimCore/Content/FirstOfficerContentV0.cs"
		})
	if _fo_content_exhausted_at > 0:
		issues.append({
			"severity": "MAJOR", "category": "FO_CONTENT_DENSITY",
			"description": "FO content exhausted at decision %d — no new dialogue after %d unique tokens" % [_fo_content_exhausted_at, _fo_dialogue_token_ids.size()],
			"prescription": "Add 50+ ambient observation lines (station commentary, space weather, trade tips, lore fragments). Content pool should last the full first hour",
			"file": "SimCore/Content/FirstOfficerContentV0.cs"
		})

	# ---- ECONOMY GROWTH TREND ----
	if _economy_growth_trend == "ACCELERATING" and _growth_rate_windows.size() >= 3:
		issues.append({
			"severity": "MAJOR", "category": "ECONOMY_GROWTH",
			"description": "Economy growth is ACCELERATING (accel=%.3f) — exponential runaway, no sigmoid transition" % _economy_acceleration,
			"prescription": "Add percentage-based sinks (fuel as %% of cargo value, insurance premiums) to create natural deceleration at higher credit levels",
			"file": "SimCore/Tweaks/FleetUpkeepTweaksV0.cs OR SimCore/Systems/MarketSystem.cs"
		})
	if _diversity_collapse_decision > 0:
		issues.append({
			"severity": "MAJOR", "category": "ROUTE_DIVERSITY",
			"description": "Route diversity collapsed below 0.3 at decision %d — player locked into grind pattern" % _diversity_collapse_decision,
			"prescription": "Increase RecentTradeDampenBps or add novelty bonus for unvisited routes. Break grind patterns with FO suggestions",
			"file": "SimCore/Systems/MarketSystem.cs OR SimCore/Tweaks/MarketTweaksV0.cs"
		})

	# ---- HULL TENSION (gap #5) ----
	if _hull_never_threatened and _total_combats > 5:
		issues.append({
			"severity": "MINOR", "category": "HULL_TENSION",
			"description": "Hull never dropped below 90%% in %d combats — no combat tension" % _total_combats,
			"prescription": "Increase pirate damage or reduce starter shields; near-death (20-50%%) should happen at least once",
			"file": "SimCore/Tweaks/CombatTweaksV0.cs OR SimCore/Tweaks/FactionTweaksV0.cs"
		})
	if _hull_below_20_count > 3:
		issues.append({
			"severity": "MINOR", "category": "HULL_TENSION",
			"description": "Hull dropped below 20%% %d times — combat may be too punishing" % _hull_below_20_count,
			"prescription": "Add health pickups, reduce pirate DPS, or add retreat mechanic",
			"file": "SimCore/Tweaks/CombatTweaksV0.cs"
		})

	# ---- MARGIN TREND (gap #6) ----
	if _declining_margin_streaks > 3:
		issues.append({
			"severity": "MAJOR", "category": "MARGIN_TREND",
			"description": "%d declining margin streaks — trades feel progressively less rewarding (early grind indicator)" % _declining_margin_streaks,
			"prescription": "Add new trade opportunities at higher tiers, unlock better goods with exploration, FO hints about distant profitable routes",
			"file": "SimCore/Systems/MarketSystem.cs OR SimCore/Gen/MarketInitGen.cs"
		})
	if _worst_loss_pct > 30.0:
		issues.append({
			"severity": "MAJOR", "category": "MARGIN_TREND",
			"description": "Worst trade lost %.0f%% of credits — bad trade nearly game-ending (industry: < 20%%)" % _worst_loss_pct,
			"prescription": "Add trade confirmation for large purchases, FO warning on bad deals, undo grace period",
			"file": "scripts/ui/hero_trade_menu.gd OR SimCore/Commands/BuyCommand.cs"
		})

	# ---- DOCK SYSTEM DUMP (gap #7) ----
	if _dock_system_dump_count > 0:
		issues.append({
			"severity": "MAJOR", "category": "DOCK_DUMP",
			"description": "%d dock(s) revealed 3+ new systems at once — information overload (one-system-per-encounter rule)" % _dock_system_dump_count,
			"prescription": "Gate system reveals: missions after 3 trades, research after 5 nodes, construction after haven discovery",
			"file": "scripts/bridge/SimBridge.Story.cs OR SimCore/Systems/WinConditionSystem.cs"
		})

	# ---- COMBAT FEEL (gap #9 — uses GetLastCombatLogV0) ----
	if _combat_round_counts.size() > 0:
		var avg_rc := 0
		for rc in _combat_round_counts:
			avg_rc += rc
		avg_rc = avg_rc / _combat_round_counts.size()
		if avg_rc < 3:
			issues.append({
				"severity": "MINOR", "category": "COMBAT_FEEL",
				"description": "Average combat only %d rounds — fights end too fast, no tension build" % avg_rc,
				"prescription": "Increase NPC hull to extend fights to 5-8 rounds (FTL pattern: weapon charge → fire → impact = 3-stage feedback)",
				"file": "SimCore/Tweaks/FactionTweaksV0.cs"
			})
		if avg_rc > 20:
			issues.append({
				"severity": "MINOR", "category": "COMBAT_FEEL",
				"description": "Average combat %d rounds — fights drag on too long" % avg_rc,
				"prescription": "Increase player damage or reduce NPC hull; target 5-10 rounds for satisfying fights",
				"file": "SimCore/Tweaks/CombatTweaksV0.cs"
			})
	if _shield_break_rounds.size() > 0:
		var never_broke_shield := true
		for sb in _shield_break_rounds:
			if sb > 0:
				never_broke_shield = false
				break
		# This check is actually about whether shields broke — if all broke, that's fine
	if _combat_dmg_variance.size() > 0:
		var low_variance := 0
		for cv in _combat_dmg_variance:
			if cv < 0.1:
				low_variance += 1
		if low_variance == _combat_dmg_variance.size() and _combat_dmg_variance.size() > 3:
			issues.append({
				"severity": "MINOR", "category": "COMBAT_FEEL",
				"description": "Zero damage variance in all combats — every hit deals the same damage, combat feels mechanical",
				"prescription": "Add damage randomness (80-120% range), critical hits, or weapon-specific effects",
				"file": "SimCore/Systems/NpcFleetCombatSystem.cs"
			})

	# ---- EXPLORATION MOTIVATION (gap #13) ----
	if _total_travel_count > 10:
		var trade_travel := 0
		for act in _actions:
			if str(act.get("type", "")) == "TRAVEL":
				var detail: String = str(act.get("detail", ""))
				if "sell" in detail or "buy" in detail or "profit" in detail:
					trade_travel += 1
		var trade_travel_pct := float(trade_travel) / float(maxi(_total_travel_count, 1)) * 100.0
		if trade_travel_pct > 80.0:
			issues.append({
				"severity": "MINOR", "category": "EXPLORATION",
				"description": "%.0f%% of travel is trade-motivated — player never explores for curiosity" % trade_travel_pct,
				"prescription": "Add exploration rewards: hidden stations, data caches, anomalies that don't require trade logic",
				"file": "SimCore/Gen/DiscoverySeedGen.cs OR SimCore/Systems/AdaptationFragmentSystem.cs"
			})

	# ---- SAVE/LOAD INTEGRITY (gap #21) ----
	if _event_decisions.has("save_load_tested"):
		pass  # PASS — save/load worked
	elif _decision >= 400:
		issues.append({
			"severity": "MINOR", "category": "SAVE_LOAD",
			"description": "Save/load round-trip not tested during session",
			"prescription": "Bot should test save/load at decision 200; bridge may lack RequestSave/RequestLoad",
			"file": "scripts/bridge/SimBridge.cs"
		})

	# ---- AUTOMATION ENGAGEMENT (gap #23) ----
	if not _automation_created and _decision >= 500 and _total_buys + _total_sells >= 20:
		issues.append({
			"severity": "MINOR", "category": "AUTOMATION",
			"description": "No automation created after %d trades — core mechanic (automation IS the game) never surfaced" % (_total_buys + _total_sells),
			"prescription": "FO should suggest automation after 10+ manual trades; Factorio principle: show the relief after the pain",
			"file": "SimCore/Systems/FirstOfficerSystem.cs OR scripts/bridge/SimBridge.Automation.cs"
		})
	if _automations_created == 0 and _decision >= 200:
		issues.append({
			"severity": "MAJOR", "category": "AUTOMATION",
			"description": "Zero automation programs after %d decisions — automation is the core loop but never exercised" % _decision,
			"prescription": "Ensure bot can create programs: check bridge methods, market availability, or lower sell threshold",
			"file": "scripts/bridge/SimBridge.Programs.cs OR scripts/bridge/SimBridge.Automation.cs"
		})

	# ---- PLANET SCAN (gap #24) ----
	if _planet_scans_performed == 0 and _visited.size() >= 10:
		issues.append({
			"severity": "MINOR", "category": "PLANET_SCAN",
			"description": "No planet scans after visiting %d nodes — scanning system never exercised" % _visited.size(),
			"prescription": "Ensure scan charges are available; FO should prompt scanning at interesting nodes",
			"file": "scripts/bridge/SimBridge.Planet.cs OR SimCore/Systems/ScanSystem.cs"
		})

	# ---- CONSTRUCTION (gap #25) ----
	if _haven_discovered and _constructions_started == 0 and _decision >= 300:
		issues.append({
			"severity": "MINOR", "category": "CONSTRUCTION",
			"description": "Haven discovered but no construction started after %d decisions — base-building not exercised" % _decision,
			"prescription": "Ensure construction defs are available and prerequisites met; FO should suggest building at haven",
			"file": "SimCore/Content/ConstructionContentV0.cs OR scripts/bridge/SimBridge.Construction.cs"
		})

	# ---- COGNITIVE LOAD (research gap #1) ----
	if _tabs_at_first_dock > 5:
		issues.append({
			"severity": "MAJOR", "category": "COGNITIVE_LOAD",
			"description": "First dock shows %d tabs — too many choices for a new player (Miller's Law: 7±2 max)" % _tabs_at_first_dock,
			"prescription": "Hide advanced tabs until player demonstrates competence; show 2-3 tabs at first dock",
			"file": "scripts/ui/hero_trade_menu.gd OR scripts/bridge/SimBridge.Story.cs"
		})
	var max_sys_per_dock: int = int(_systems_per_dock.max()) if _systems_per_dock.size() > 0 else 0
	if max_sys_per_dock > 2:
		issues.append({
			"severity": "CRITICAL", "category": "COGNITIVE_LOAD",
			"description": "One dock revealed %d new systems at once — violates one-system-per-encounter rule" % max_sys_per_dock,
			"prescription": "Gate system reveals: space them across multiple docks, use FO to introduce each",
			"file": "scripts/bridge/SimBridge.Story.cs OR SimCore/Systems/WinConditionSystem.cs"
		})
	if _fo_word_counts.size() > 0:
		var max_words: int = _fo_word_counts.max()
		if max_words > 80:
			issues.append({
				"severity": "MINOR", "category": "COGNITIVE_LOAD",
				"description": "FO sent a %d-word message — wall of text (target: 15-40 words)" % max_words,
				"prescription": "Break long FO messages into 2-3 shorter beats; max 40 words per message",
				"file": "SimCore/Content/FirstOfficerContentV0.cs"
			})
	# Information desert
	if _systems_introduced.size() > 0 and _system_intro_decisions.size() > 0:
		var last_intro_d: int = int(_system_intro_decisions[-1])
		if _decision - last_intro_d > 200 and _decision > 300:
			issues.append({
				"severity": "MAJOR", "category": "COGNITIVE_LOAD",
				"description": "No new system introduced for %d decisions — information desert" % (_decision - last_intro_d),
				"prescription": "Introduce new systems at regular intervals; player should encounter something new every ~100 decisions",
				"file": "scripts/bridge/SimBridge.Story.cs"
			})

	# ---- DEAD-END / CONFUSION (research gap #2) ----
	if _trap_state_count > 0:
		issues.append({
			"severity": "CRITICAL", "category": "DEAD_END",
			"description": "Trap state detected %d time(s) — player stuck with no viable action (credits<buy, cargo=0, no missions)" % _trap_state_count,
			"prescription": "Add safety net: emergency mission with advance payment, free fuel when credits=0, or minimum salvage from scanning",
			"file": "SimCore/Systems/MissionSystem.cs OR SimCore/Systems/MarketSystem.cs"
		})
	if _action_reversals > 3:
		issues.append({
			"severity": "MINOR", "category": "DEAD_END",
			"description": "%d action reversals (buy then sell same good at same station) — player confused about what to do" % _action_reversals,
			"prescription": "Add clearer price comparison UI, FO tips about profitable routes, or highlight best sell station",
			"file": "scripts/ui/hero_trade_menu.gd OR scripts/bridge/SimBridge.Story.cs"
		})

	# ---- RETENTION PREDICTION (research gap #3) ----
	if _first_profit_decision > 80:
		issues.append({
			"severity": "MAJOR", "category": "RETENTION",
			"description": "First profit not until decision %d — too slow for retention (target: <30)" % _first_profit_decision,
			"prescription": "Ensure starting area has an obvious profitable route within 1 hop",
			"file": "SimCore/Gen/MarketInitGen.cs"
		})
	elif _first_profit_decision < 0 and _decision > 100:
		issues.append({
			"severity": "CRITICAL", "category": "RETENTION",
			"description": "No profitable trade in entire session (%d decisions) — aha moment never occurred" % _decision,
			"prescription": "Critical: starting market must have clear profitable trade within 2 hops",
			"file": "SimCore/Gen/MarketInitGen.cs"
		})
	if _core_loop_decision > 100:
		issues.append({
			"severity": "MAJOR", "category": "RETENTION",
			"description": "Core loop (buy→warp→sell) not completed until decision %d — player lost (target: <50)" % _core_loop_decision,
			"prescription": "FO should guide player through first buy-warp-sell cycle by decision 40",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})
	if _aha_moment_decision > 150:
		issues.append({
			"severity": "MAJOR", "category": "RETENTION",
			"description": "Aha moment (margin>100cr) not until decision %d — first trade doesn't feel like a heist (target: <80)" % _aha_moment_decision,
			"prescription": "Increase starting-area spreads so first sell yields >100cr margin",
			"file": "SimCore/Gen/MarketInitGen.cs OR SimCore/Tweaks/MarketTweaksV0.cs"
		})
	if _action_rate_declining:
		issues.append({
			"severity": "MAJOR", "category": "RETENTION",
			"description": "Action rate declining for 4+ consecutive windows — player is disengaging",
			"prescription": "Add escalating incentives: FO challenges, new mission types, discovery pings in later session",
			"file": "SimCore/Systems/FirstOfficerSystem.cs OR SimCore/Systems/MissionTemplateSystem.cs"
		})

	# ---- PACING RHYTHM (research gap #4) ----
	if _beat_intervals.size() >= 3:
		var mean_bi := 0.0
		for bi in _beat_intervals:
			mean_bi += float(bi)
		mean_bi /= float(_beat_intervals.size())
		var bi_var := 0.0
		for bi in _beat_intervals:
			bi_var += (float(bi) - mean_bi) * (float(bi) - mean_bi)
		bi_var /= float(_beat_intervals.size())
		var bi_cov := sqrt(bi_var) / maxf(mean_bi, 0.01)
		if bi_cov < 0.2:
			issues.append({
				"severity": "MAJOR", "category": "PACING_RHYTHM",
				"description": "Beat interval CoV %.2f — pacing is monotone (events at rigid intervals)" % bi_cov,
				"prescription": "Vary event timing: some close together (clusters of 2-3), some spread apart (quiet valleys)",
				"file": "SimCore/Systems/AdaptationFragmentSystem.cs OR SimCore/Systems/FirstOfficerSystem.cs"
			})
	# Check for event clustering (3+ HIGH in 30 decisions)
	if _high_event_decisions.size() >= 3:
		for i in range(2, _high_event_decisions.size()):
			var span: int = _high_event_decisions[i] - _high_event_decisions[i - 2]
			if span < 30:
				issues.append({
					"severity": "MINOR", "category": "PACING_RHYTHM",
					"description": "3 HIGH events within %d decisions — overwhelming rush" % span,
					"prescription": "Space high-intensity events by 30-50 decisions; insert calm valleys between peaks",
					"file": "SimCore/Systems/AdaptationFragmentSystem.cs"
				})
				break

	# ---- VALENCE-AROUSAL ARC (research gap #5) ----
	if _valence_samples.size() >= 10 and _valence_crossings == 0:
		issues.append({
			"severity": "MAJOR", "category": "VALENCE_ARC",
			"description": "Valence never crosses zero (%d events) — emotional experience is monotone (always positive or always negative)" % _valence_samples.size(),
			"prescription": "Add contrast: if mostly positive, add danger/loss moments; if mostly negative, add small wins",
			"file": "SimCore/Systems/NpcFleetCombatSystem.cs OR SimCore/Systems/MarketSystem.cs"
		})
	if _catharsis_count == 0 and _total_combats > 0:
		issues.append({
			"severity": "MAJOR", "category": "VALENCE_ARC",
			"description": "Zero catharsis events — no relief-after-danger moments in %d combats" % _total_combats,
			"prescription": "Ensure combat victory triggers positive feedback (loot + FO reaction + credit gain) within 30 decisions of damage",
			"file": "SimCore/Systems/NpcFleetCombatSystem.cs"
		})
	if _wonder_count == 0 and _decision > 200:
		issues.append({
			"severity": "MAJOR", "category": "VALENCE_ARC",
			"description": "Zero wonder moments (HIGH+positive) in %d decisions — no exciting peaks" % _decision,
			"prescription": "Ensure first profitable trade, first discovery, and first victory are all HIGH-arousal events with strong feedback",
			"file": "scripts/bridge/SimBridge.Story.cs"
		})

	# ---- COMPETENCE / MASTERY (research gap #6) ----
	if _early_margins.size() >= 5 and _late_margins.size() >= 5:
		var early_avg := 0.0
		for m in _early_margins:
			early_avg += float(m)
		early_avg /= float(_early_margins.size())
		var late_avg := 0.0
		for m in _late_margins:
			late_avg += float(m)
		late_avg /= float(_late_margins.size())
		if late_avg < early_avg * 0.8:
			issues.append({
				"severity": "MAJOR", "category": "COMPETENCE",
				"description": "Trade margins declining: early avg %.0f, late avg %.0f — player getting worse, not better" % [early_avg, late_avg],
				"prescription": "Unlock better goods/routes as player explores more; margins should increase with knowledge",
				"file": "SimCore/Gen/MarketInitGen.cs OR SimCore/Systems/MarketSystem.cs"
			})
	if _milestone_toasts == 0 and _decision >= 300:
		issues.append({
			"severity": "MAJOR", "category": "COMPETENCE",
			"description": "Zero milestone acknowledgments in %d decisions — game never celebrates player progress" % _decision,
			"prescription": "Add toast/FO reaction for: first trade, 10th trade, first kill, 5 nodes explored, first automation",
			"file": "SimCore/Systems/MilestoneSystem.cs OR scripts/bridge/SimBridge.Story.cs"
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
		"slow_mode": _slow_mode,
		"decisions": _decision,
		"ticks": _decision * TICK_ADVANCE,
		"dimension_triage": {
			"active": ["economy", "grind", "cognitive_load", "valence_arc", "fo",
				"competence", "pacing", "pacing_rhythm", "combat", "combat_depth",
				"exploration", "narrative", "economy_depth", "disclosure", "progression"],
			"stable": ["security", "haven", "dead_end", "missions", "fleet", "retention"],
		},
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
				"growth_trend": _economy_growth_trend,
				"growth_acceleration": snapped(_economy_acceleration, 0.001),
				"growth_rate_windows": _growth_rate_windows,
			},
			"pacing": {
				"reward_count": _reward_events.size(),
				"mean_gap": snapped(float(max_gap) if _reward_events.size() < 2 else float(_reward_events[-1] - _reward_events[0]) / maxf(float(_reward_events.size() - 1), 1.0), 0.1),
				"max_gap": max_gap,
				"entropy": snapped(entropy, 0.01),
				"longest_streak": longest_streak,
				"longest_streak_type": _longest_streak_type,
				"longest_streak_start": _longest_streak_start,
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
				"discoveries_seeded": _discoveries_found,
				"discoveries_scanned": _discoveries_scanned,
				"fragments": _fragments_found,
				"knowledge": _knowledge_entries,
			},
			"grind": {
				"score": snapped(grind_score, 0.01),
				"route_repeats": route_repeats,
				"good_repeats": good_repeats,
				"diversity_timeline": _route_diversity_windows,
				"diversity_collapse_decision": _diversity_collapse_decision,
			},
			"fo": {
				"promoted": _fo_promoted,
				"dialogue_lines": _fo_dialogue_count,
				"max_silence": fo_max_silence,
				"transcript": _fo_dialogue_log,
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
				"automations_created": _automations_created,
				"automation_types": _automation_types_created.keys(),
				"fracture_travels": _fracture_travels,
				"planet_scans": _planet_scans_performed,
				"constructions_started": _constructions_started,
				"haven_upgraded": _haven_upgraded,
				"fabrications_started": _fabrications_started,
				"megaprojects_started": _megaprojects_started,
				"program_monitoring_count": _program_monitoring_count,
			},
			"reward_cadence": {
				"max_gap": _max_reward_gap,
				"mean_gap": snapped(float(_reward_gaps.reduce(func(a, b): return a + b, 0)) / maxf(float(_reward_gaps.size()), 1.0), 0.1) if _reward_gaps.size() > 0 else 0.0,
				"gap_count": _reward_gaps.size(),
			},
			"credit_velocity": {
				"stall_max": _credit_stall_max,
				"death_spiral_max": _credit_death_spiral_max,
				"samples": _credit_velocity_samples.size(),
				"bridge_available": _credit_history_available,
			},
			"hull_timeline": {
				"samples": _hull_timeline.size(),
				"below_50": _hull_below_50_count,
				"below_20": _hull_below_20_count,
				"never_threatened": _hull_never_threatened,
			},
			"margin_trend": {
				"trades": _margin_trend.size(),
				"declining_streaks": _declining_margin_streaks,
				"worst_single_loss": _worst_single_loss,
				"worst_loss_pct": snapped(_worst_loss_pct, 0.1),
			},
			"fo_reactivity": {
				"events_tracked": _fo_react_latencies.size(),
				"timeouts": _fo_react_timeouts,
				"dialogue_repeats": _fo_dialogue_repeats,
				"unique_tokens": _fo_dialogue_token_ids.size(),
				"adaptation_readiness": _fo_adaptation_events,
				"last_new_token_decision": _fo_last_new_token_decision,
				"content_exhausted_at": _fo_content_exhausted_at,
			},
			"combat_loot": {
				"with_loot": _combat_loot_count,
				"without_loot": _combat_no_loot_count,
				"loot_rate_pct": snapped(float(_combat_loot_count) / maxf(float(_total_combats), 1.0) * 100.0, 0.1),
			},
			"combat_feel": {
				"round_data": _combat_round_counts.size(),
				"shield_break_data": _shield_break_rounds.size(),
				"ttk_data": _combat_ttk_ratios.size(),
				"dmg_variance_data": _combat_dmg_variance.size(),
			},
			"dock_system_dump": {
				"dumps": _dock_system_dump_count,
				"max_new_at_once": _dock_new_systems.max() if _dock_new_systems.size() > 0 else 0,
			},
			"cognitive_load": {
				"tabs_first_dock": _tabs_at_first_dock,
				"max_systems_per_dock": _systems_per_dock.max() if _systems_per_dock.size() > 0 else 0,
				"avg_fo_words": snapped(float(_fo_word_counts.reduce(func(a, b): return a + b, 0)) / maxf(float(_fo_word_counts.size()), 1.0), 0.1) if _fo_word_counts.size() > 0 else 0.0,
				"max_fo_words": _fo_word_counts.max() if _fo_word_counts.size() > 0 else 0,
			},
			"dead_end": {
				"trap_states": _trap_state_count,
				"trap_decisions": _trap_state_decisions,
				"min_viable_actions": _min_viable_actions,
				"action_reversals": _action_reversals,
			},
			"retention": {
				"first_profit_decision": _first_profit_decision,
				"core_loop_decision": _core_loop_decision,
				"aha_moment_decision": _aha_moment_decision,
				"action_rate_declining": _action_rate_declining,
				"action_rate_windows": _action_rate_windows,
			},
			"pacing_rhythm": {
				"high_events": _high_event_decisions.size(),
				"medium_events": _medium_event_decisions.size(),
				"beat_intervals": _beat_intervals,
				"density_per_100": snapped(float(_high_event_decisions.size()) / maxf(float(_decision), 1.0) * 100.0, 0.1),
			},
			"valence_arc": {
				"samples": _valence_samples.size(),
				"crossings": _valence_crossings,
				"catharsis": _catharsis_count,
				"wonder": _wonder_count,
				"final_running_avg": snapped(_valence_running_avg, 0.01),
			},
			"competence": {
				"early_margin_avg": snapped(float(_early_margins.reduce(func(a, b): return a + b, 0)) / maxf(float(_early_margins.size()), 1.0), 0.1) if _early_margins.size() > 0 else 0.0,
				"late_margin_avg": snapped(float(_late_margins.reduce(func(a, b): return a + b, 0)) / maxf(float(_late_margins.size()), 1.0), 0.1) if _late_margins.size() > 0 else 0.0,
				"early_kill_rate": snapped(_early_kill_rate, 0.01),
				"late_kill_rate": snapped(_late_kill_rate, 0.01),
				"milestone_toasts": _milestone_toasts,
				"credits_per_d_early": snapped(_credits_per_decision_early, 0.1),
				"credits_per_d_late": snapped(_credits_per_decision_late, 0.1),
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

	# Serialize experience checkpoints (10min / 30min / 60min)
	report["experience_arc"] = {
		"checkpoints": [],
		"arc_trend": "",
	}
	for cp in _checkpoints:
		var cp_data := {
			"phase": str(cp.get("phase", "")),
			"decision": int(cp.get("decision", 0)),
			"scores": {
				"orientation": int(cp.get("orientation", 0)),
				"mastery": int(cp.get("mastery", 0)),
				"engagement": int(cp.get("engagement", 0)),
				"feel": int(cp.get("feel", 0)),
				"progression": int(cp.get("progression", 0)),
				"overall": snapped(float(cp.get("overall", 0.0)), 0.1),
			},
			"metrics": {
				"credits": int(cp.get("credits", 0)),
				"credit_growth_pct": int(cp.get("credit_growth_pct", 0)),
				"nodes_visited": int(cp.get("nodes_visited", 0)),
				"visit_pct": float(cp.get("visit_pct", 0.0)),
				"goods_traded": int(cp.get("goods_traded", 0)),
				"total_trades": int(cp.get("total_trades", 0)),
				"fo_lines": int(cp.get("fo_lines", 0)),
				"combats": int(cp.get("combats", 0)),
				"kills": int(cp.get("kills", 0)),
				"hull_min_pct": int(cp.get("hull_min_pct", 100)),
				"systems_introduced": int(cp.get("systems_introduced", 0)),
				"modules_installed": int(cp.get("modules_installed", 0)),
				"haven_discovered": bool(cp.get("haven_discovered", false)),
				"automation_created": bool(cp.get("automation_created", false)),
				"entropy": float(cp.get("entropy", 0.0)),
				"grind_score": float(cp.get("grind_score", 0.0)),
				"max_reward_gap": int(cp.get("max_reward_gap", 0)),
				"profitable_pct": float(cp.get("profitable_pct", 0.0)),
			},
			"issues": [],
		}
		for iss in cp.get("issues", []):
			cp_data["issues"].append({
				"severity": str(iss.get("severity", "")),
				"issue": str(iss.get("issue", "")),
			})
		report["experience_arc"]["checkpoints"].append(cp_data)
	# Arc trend
	if _checkpoints.size() >= 2:
		var first_o: float = float(_checkpoints[0].get("overall", 0.0))
		var last_o: float = float(_checkpoints[-1].get("overall", 0.0))
		report["experience_arc"]["arc_trend"] = "CRESCENDO" if last_o > first_o + 0.5 else ("FLAT" if absf(last_o - first_o) <= 0.5 else "DECLINING")

	# Write JSON to reports directory
	var arch_label := "%s_slow" % _archetype if _slow_mode else _archetype
	var report_path := "res://reports/experience/%s/seed_%d/report.json" % [arch_label, _user_seed]
	var json_str := JSON.stringify(report, "  ")
	var f := FileAccess.open(report_path, FileAccess.WRITE)
	if f:
		f.store_string(json_str)
		f.close()
		_a.log("REPORT_JSON|%s" % report_path)
	else:
		_a.log("REPORT_JSON|FAILED|%s" % report_path)
