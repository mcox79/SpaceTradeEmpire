# scripts/tests/test_first_hour_proof_v0.gd
# First-Hour Experience Proof Bot — 31 phases across 6 acts.
# Deterministically verifies the full first-hour player journey with
# hard assertions at every milestone + screenshots at key moments.
#
# Reference: docs/design/first_hour_experience_v0.md
#
# Usage (dedicated runner — recommended):
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode visual -Seed 42
#
# Usage (via screenshot skill):
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode first-hour
#
# Usage (headless, assertions only):
#   godot --headless --path . -s res://scripts/tests/test_first_hour_proof_v0.gd
#
# Seed variation (each seed produces different systems/markets/NPCs):
#   godot --headless --path . -s res://scripts/tests/test_first_hour_proof_v0.gd -- --seed=42
extends SceneTree

const PREFIX := "FH1|"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/first_hour/"
var _user_seed := -1  # -1 = no seed override

# Performance tracking
var _fps_samples: Array[float] = []
var _fps_min := 999.0
var _fps_max := 0.0

# Dispatch failure tracking
var _dispatch_failures := 0

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
const AuditScript = preload("res://scripts/tools/aesthetic_audit.gd")

# Settle timings (frames at ~60fps)
const SETTLE_SCENE := 60
const SETTLE_ACTION := 20
const SETTLE_TRAVEL := 30
const POST_CAPTURE := 8

enum Phase {
	# Setup
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY, WAIT_LOCAL_SYSTEM,
	# Act 1: Cold Open (0:00-1:30)
	BOOT, CHECK_NPC, CHECK_HUD, DOCK,
	# Act 2: First Trade (1:30-5:00)
	CHECK_FO, BUY, UNDOCK_1, TRAVEL_1, SETTLE_ARRIVAL_1, ARRIVAL_1, DOCK_2, SELL, PROFIT_CHECK,
	# Act 3: First Mission (5:00-12:00)
	CHECK_MISSIONS, ACCEPT_MISSION, UNDOCK_2, TRAVEL_2, SETTLE_ARRIVAL_2, ARRIVAL_2,
	# Act 4: First Combat + Upgrade (8:00-15:00)
	FIND_HOSTILE, COMBAT, POST_COMBAT, DOCK_3, CHECK_MODULES, INSTALL_MODULE,
	TAB_CYCLE,  # Capture all visible dock tabs
	# Act 5: Galaxy Opens (15:00-30:00)
	GALAXY_MAP, UNDOCK_4, MULTI_HOP, SETTLE_HOP, TRADE_ROUTE, CHECK_SUSTAIN,
	# Act 6: Scale Reveal (30:00-60:00)
	DEEP_EXPLORE, SETTLE_DEEP, PRICE_DIVERSITY, CHECK_RESEARCH, FINAL_TRADE,
	CAPTURE_EMPIRE_DASH, CAPTURE_GALAXY_MAP,  # UI panel captures
	# Act 7: Depth Probes (added for coverage)
	PROBE_SYSTEMIC, PROBE_FACTIONS, PROBE_KNOWLEDGE, PROBE_RESEARCH_START, PROBE_LEDGER,
	PROBE_REFIT, PROBE_AUTOMATION, PROBE_CONSTRUCTION, PROBE_FRACTURE,
	PROBE_DIPLOMACY, PROBE_STORY, PROBE_ENDGAME, PROBE_ECONOMY_SIGNAL,
	PROBE_OVERLAYS, PROBE_PLANET_SCAN, PROBE_ANOMALY_CHAINS,
	AUDIT,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false  # Guard against await re-entry
const MAX_FRAMES := 4200  # 70s at 60fps (extra for tab cycle + panel captures)

var _bridge = null
var _game_manager = null
var _observer = null
var _screenshot = null
var _audit = null
var _snapshots: Array = []

# Navigation state
var _home_node_id := ""
var _all_nodes: Array = []
var _all_edges: Array = []
var _visited: Dictionary = {}
var _neighbor_ids: Array = []
var _current_dest_idx := 0

# Economy tracking
var _credits_at_start := 0
var _credits_before_buy := 0
var _credits_after_sell := 0
var _trades_completed := 0
var _combats_completed := 0
var _credits_before_combat := 0
var _cargo_before := 0
var _bought_good_id := ""

# Market snapshots per node
var _market_snapshots: Dictionary = {}

# Multi-hop tracking
var _hop_queue: Array = []
var _hop_idx := 0

# Soft flags
var _flags: Array[String] = []

# Goal probe tracking
var _fo_dialogue_count := 0

# Hard fail tracking
var _hard_fail := false
var _fail_reason := ""

# Experience dimension tracking (Sea of Thieves / GEQ-inspired)
var _min_hull_seen := 100
var _factions_visited: Dictionary = {}
var _systems_introduced: Array[String] = []
var _reward_moment := false  # True if mission reward or discovery unlock
var _last_phase_change_frame := 0  # Stall watchdog

# Coverage tracking
var _bridge_methods_called: Dictionary = {}
var _goods_traded: Dictionary = {}

# Report card evidence (stashed from phases for scoring)
var _npc_count_at_boot := 0
var _fo_promoted := false
var _fo_post_event_reactions := 0
var _tutorial_text_found := false
var _tech_count := 0
var _empty_slots_at_fit := 0
var _profit_margin := 0
var _fo_reacted_to_profit := false
var _missions_available_count := 0
var _mission_accepted := false
var _systemic_offers := 0
var _ledger_entries := 0
var _warfront_count := 0
var _milestone_count := 0
var _credit_direction_changes := 0  # Computed in audit, saved for report

# Expansion tracking vars (plan changes 1-13)
var _heat_capacity := 0
var _radiator_intact := false
var _systemic_mission_accepted := false
var _boot_tutorial_suppressed := false
var _boot_intro_dismissed := false
var _dock1_onboarding := {}
var _dock3_onboarding := {}
var _price_history_entries := 0
var _discovery_scan_attempted := false
var _overlay_territory_nodes := 0
var _economy_overview_goods := 0
var _ui_panels_found := 0
var _haven_tier := 0
var _fracture_travel_attempted := false
var _faction_bfs_path: Array = []  # Shared BFS path across multi_hop + deep_explore
var _faction_map := {}  # node_id -> faction_id from territory overlay

# Pacing time-series (Valve AI Director inspired)
var _pacing_credits: Array[int] = []
var _pacing_hull_pct: Array[int] = []
var _pacing_tick: Array[int] = []
var _fo_last_dialogue_frame := 0
var _fo_longest_silence := 0
var _act_start_frames: Dictionary = {}  # "ACT_N" -> frame


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	# FPS sampling every 30 frames — start after BOOT to exclude scene load stalls
	if _total_frames % 30 == 0 and _phase >= Phase.BOOT:
		var fps := Engine.get_frames_per_second()
		if fps > 0.0:
			_fps_samples.append(fps)
			if fps < _fps_min:
				_fps_min = fps
			if fps > _fps_max:
				_fps_max = fps
	# Pacing time-series: sample every 30 frames (~0.5s) — headless bots complete in ~200 frames
	if _total_frames % 30 == 0 and _bridge != null and _phase > Phase.WAIT_LOCAL_SYSTEM:
		var ps_snap: Dictionary = _bridge.call("GetPlayerStateV0")
		_pacing_credits.append(int(ps_snap.get("credits", 0)))
		_pacing_tick.append(_get_tick())
		var hp_snap: Dictionary = {}
		if _bridge.has_method("GetFleetCombatHpV0"):
			hp_snap = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull_max_s := int(hp_snap.get("hull_max", 100))
		var hull_s := int(hp_snap.get("hull", hull_max_s))
		_pacing_hull_pct.append((hull_s * 100) / maxi(hull_max_s, 1))
		# FO silence tracking
		var fo_now := _fo_dialogue_count
		if fo_now > 0 and fo_now == int(get_meta("_fo_last_count", 0)):
			var silence := _total_frames - _fo_last_dialogue_frame
			if silence > _fo_longest_silence:
				_fo_longest_silence = silence
		else:
			_fo_last_dialogue_frame = _total_frames
		set_meta("_fo_last_count", fo_now)
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_fail("timeout_at_%s" % Phase.keys()[_phase])
	# Stall watchdog: if phase hasn't changed in 360 frames (~6s), flag soft-lock
	if _total_frames - _last_phase_change_frame > 360 and _phase != Phase.DONE:
		_log("SOFT_LOCK|phase=%s stalled_frames=%d" % [Phase.keys()[_phase], _total_frames - _last_phase_change_frame])
		_flag("SOFT_LOCK_%s" % Phase.keys()[_phase])
		_last_phase_change_frame = _total_frames  # Reset to avoid spamming
	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(_phase, SETTLE_SCENE, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.WAIT_LOCAL_SYSTEM: _do_wait_local()
		# Act 1
		Phase.BOOT: _do_boot()
		Phase.CHECK_NPC: _do_check_npc()
		Phase.CHECK_HUD: _do_check_hud()
		Phase.DOCK: _do_dock()
		# Act 2
		Phase.CHECK_FO: _do_check_fo()
		Phase.BUY: _do_buy()
		Phase.UNDOCK_1: _do_undock(Phase.TRAVEL_1)
		Phase.TRAVEL_1: _do_travel(0, Phase.SETTLE_ARRIVAL_1)
		Phase.SETTLE_ARRIVAL_1: _do_wait(_phase, SETTLE_TRAVEL, Phase.ARRIVAL_1)
		Phase.ARRIVAL_1: _do_arrival_1()
		Phase.DOCK_2: _do_dock_2()
		Phase.SELL: _do_sell()
		Phase.PROFIT_CHECK: _do_profit_check()
		# Act 3
		Phase.CHECK_MISSIONS: _do_check_missions()
		Phase.ACCEPT_MISSION: _do_accept_mission()
		Phase.UNDOCK_2: _do_undock(Phase.TRAVEL_2)
		Phase.TRAVEL_2: _do_travel(1, Phase.SETTLE_ARRIVAL_2)
		Phase.SETTLE_ARRIVAL_2: _do_wait(_phase, SETTLE_TRAVEL, Phase.ARRIVAL_2)
		Phase.ARRIVAL_2: _do_arrival_2()
		# Act 4
		Phase.FIND_HOSTILE: _do_find_hostile()
		Phase.COMBAT: _do_combat()
		Phase.POST_COMBAT: _do_post_combat()
		Phase.DOCK_3: _do_dock_3()
		Phase.CHECK_MODULES: _do_check_modules()
		Phase.INSTALL_MODULE: _do_install_module()
		Phase.TAB_CYCLE: _do_tab_cycle()
		# Act 5
		Phase.GALAXY_MAP: _do_galaxy_map()
		Phase.UNDOCK_4: _do_undock(Phase.MULTI_HOP)
		Phase.MULTI_HOP: _do_multi_hop()
		Phase.SETTLE_HOP: _do_wait(_phase, SETTLE_TRAVEL, Phase.TRADE_ROUTE)
		Phase.TRADE_ROUTE: _do_trade_route()
		Phase.CHECK_SUSTAIN: _do_check_sustain()
		# Act 6
		Phase.DEEP_EXPLORE: _do_deep_explore()
		Phase.SETTLE_DEEP: _do_wait(_phase, SETTLE_TRAVEL, Phase.PRICE_DIVERSITY)
		Phase.PRICE_DIVERSITY: _do_price_diversity()
		Phase.CHECK_RESEARCH: _do_check_research()
		Phase.FINAL_TRADE: _do_final_trade()
		Phase.CAPTURE_EMPIRE_DASH: _do_capture_empire_dash()
		Phase.CAPTURE_GALAXY_MAP: _do_capture_galaxy_map()
		# Act 7: Depth probes
		Phase.PROBE_SYSTEMIC: _do_probe_systemic()
		Phase.PROBE_FACTIONS: _do_probe_factions()
		Phase.PROBE_KNOWLEDGE: _do_probe_knowledge()
		Phase.PROBE_RESEARCH_START: _do_probe_research_start()
		Phase.PROBE_LEDGER: _do_probe_ledger()
		Phase.PROBE_REFIT: _do_probe_refit()
		Phase.PROBE_AUTOMATION: _do_probe_automation()
		Phase.PROBE_CONSTRUCTION: _do_probe_construction()
		Phase.PROBE_FRACTURE: _do_probe_fracture()
		Phase.PROBE_DIPLOMACY: _do_probe_diplomacy()
		Phase.PROBE_STORY: _do_probe_story()
		Phase.PROBE_ENDGAME: _do_probe_endgame()
		Phase.PROBE_ECONOMY_SIGNAL: _do_probe_economy_signal()
		Phase.PROBE_OVERLAYS: _do_probe_overlays()
		Phase.PROBE_PLANET_SCAN: _do_probe_planet_scan()
		Phase.PROBE_ANOMALY_CHAINS: _do_probe_anomaly_chains()
		Phase.AUDIT: _do_audit()
		Phase.DONE: _do_done()
	return false


# ===================== Setup Phases =====================

func _do_load_scene() -> void:
	# Parse --seed=N from CLI args
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_user_seed = int(arg.trim_prefix("--seed="))
	if _user_seed >= 0:
		seed(_user_seed)
		_log("SEED|%d" % _user_seed)

	# Delete stale quicksave to prevent contamination between seeds
	var save_path := "user://quicksave.json"
	if FileAccess.file_exists(save_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(save_path))
		_log("CLEANUP|deleted_quicksave")

	var scene = load("res://scenes/playable_prototype.tscn").instantiate()
	root.add_child(scene)
	_log("SCENE_LOADED")
	_polls = 0
	_phase = Phase.WAIT_SCENE


func _do_wait(current: Phase, settle: int, next: Phase) -> void:
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
		if _polls >= MAX_POLLS:
			_fail("bridge_not_found")


func _do_wait_ready() -> void:
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
		# Bot bypasses main menu — clear menu guard so _process runs game logic.
		if _game_manager:
			_game_manager.set("_on_main_menu", false)
		_init_navigation()
		_phase = Phase.WAIT_LOCAL_SYSTEM
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_fail("bridge_ready_timeout")


func _do_wait_local() -> void:
	if get_nodes_in_group("Station").size() > 0:
		_polls = 0
		_phase = Phase.BOOT
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_phase = Phase.BOOT


# ===================== Act 1: Cold Open =====================

func _do_boot() -> void:
	_log("ACT_1|Cold Open")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_at_start = int(ps.get("credits", 0))
	_home_node_id = str(ps.get("current_node_id", ""))
	_visited[_home_node_id] = true
	_log("BOOT|credits=%d node=%s" % [_credits_at_start, _home_node_id])

	# ASSERT: credits > 0
	_assert(_credits_at_start > 0, "boot_credits_positive", "credits=%d" % _credits_at_start)

	# ASSERT: no HOSTILE Label3D visible
	var hostile_found := false
	for label in _find_all_label3d():
		if label.visible and "HOSTILE" in label.text.to_upper():
			hostile_found = true
			break
	_assert(not hostile_found, "boot_no_hostile", "")
	if hostile_found:
		_flag("HOSTILE_AT_START")

	# ASSERT: no combat visual artifacts at boot (vignette, heat bar)
	var hud = root.find_child("HUD", true, false)
	if hud:
		# Check combat vignette — should be invisible at boot.
		var vignette = hud.get("_combat_vignette_rect")
		if vignette and vignette is CanvasItem and vignette.visible:
			var shader_mat = vignette.get("material")
			if shader_mat and shader_mat.has_method("get_shader_parameter"):
				var tint = shader_mat.get_shader_parameter("tint_color")
				if tint is Color and tint.a > 0.01:
					_flag("COMBAT_VIGNETTE_AT_BOOT")
					_log("GOAL|ALIVE|combat_vignette_alpha=%.2f" % tint.a)

	# Check for keybind conflicts — movement keys (WASD) vs UI toggle keys.
	var movement_keys := [KEY_W, KEY_A, KEY_S, KEY_D]
	var movement_actions := ["ship_thrust_fwd", "ship_thrust_back", "ship_turn_left", "ship_turn_right"]
	var ui_actions := InputMap.get_actions()
	for ui_action in ui_actions:
		if ui_action in movement_actions:
			continue
		if not str(ui_action).begins_with("ui_"):
			continue
		for ev in InputMap.action_get_events(ui_action):
			if ev is InputEventKey:
				if ev.physical_keycode in movement_keys:
					_flag("KEYBIND_CONFLICT_%s" % str(ui_action).to_upper())
					_log("BOOT|keybind_conflict=%s key=%d" % [str(ui_action), ev.physical_keycode])

	# Boot census: T38-T40 bridge method wiring check (non-fatal)
	var _census := {
		"diplomacy": _bridge.has_method("GetActiveTreatiesV0"),
		"story": _bridge.has_method("GetStoryProgressV0"),
		"endgame": _bridge.has_method("GetEndgameProgressV0"),
		"megaprojects": _bridge.has_method("GetMegaprojectsV0"),
		"supply_shock": _bridge.has_method("GetSupplyShockSummaryV0"),
		"upkeep": _bridge.has_method("GetFleetUpkeepV0"),
		"lattice": _bridge.has_method("GetLatticeDroneAlertsV0"),
		"haven_market": _bridge.has_method("GetHavenMarketV0"),
	}
	var census_parts: Array[String] = []
	for k in _census:
		census_parts.append("%s=%s" % [k, "T" if _census[k] else "F"])
	_log("CENSUS|%s" % " ".join(census_parts))
	# Change 4: Boot experience checks
	if _bridge.has_method("IsTutorialActiveV0"):
		var tut_active: bool = _bridge.call("IsTutorialActiveV0")
		_log("BOOT|tutorial_active=%s" % str(tut_active))
		_boot_tutorial_suppressed = not tut_active
	if _game_manager != null:
		var intro = _game_manager.get("intro_active")
		if intro != null:
			_log("BOOT|intro_active=%s" % str(intro))
			_boot_intro_dismissed = not bool(intro)
		else:
			_boot_intro_dismissed = true  # No intro property = dismissed

	_act_start_frames["ACT_1"] = _total_frames

	_capture("01_boot")
	_polls = 0
	_phase = Phase.CHECK_NPC


func _do_check_npc() -> void:
	var npcs = get_nodes_in_group("FleetShip")
	# NPC ships may not be spawned yet due to read-lock contention at boot
	# (SimBridge.TryEnterReadLock(0) fails while sim thread initializes).
	# Wait up to 300 frames (~5s) for at least 1 FleetShip to appear.
	if npcs.size() == 0 and _polls < 300:
		_polls += 1
		return
	_npc_count_at_boot = npcs.size()
	_log("CHECK_NPC|count=%d waited=%d" % [npcs.size(), _polls])
	_assert(npcs.size() >= 1, "npc_present", "count=%d" % npcs.size())

	# Check no hostile NPC at start
	for npc in npcs:
		if npc.has_meta("is_hostile") and bool(npc.get_meta("is_hostile")):
			_flag("HOSTILE_NPC_AT_START")
			break

	# Goal 1 probe: are NPCs alive (have velocity)?
	var npc_with_velocity := 0
	for npc in npcs:
		if npc is Node3D and npc.has_method("get_velocity"):
			if npc.get_velocity().length() > 0.1:
				npc_with_velocity += 1
		elif npc is CharacterBody3D:
			if npc.velocity.length() > 0.1:
				npc_with_velocity += 1
	_log("GOAL|ALIVE|npc_count=%d npc_have_velocity=%d" % [npcs.size(), npc_with_velocity])

	_polls = 0
	_phase = Phase.CHECK_HUD


func _do_check_hud() -> void:
	var hud = root.find_child("HUD", true, false)
	if hud == null:
		_flag("HUD_MISSING")
		_phase = Phase.DOCK
		return

	# Check Tier-1 elements
	var credits_lbl = _find_child_recursive(hud, "CreditsLabel")
	var hull_bar = _find_child_recursive(hud, "HullBar")
	var shield_lbl = _find_child_recursive(hud, "ShieldLabel")
	var system_lbl = _find_child_recursive(hud, "NodeLabel")
	var state_lbl = _find_child_recursive(hud, "StateLabel")

	if credits_lbl == null: _flag("HUD_MISSING_ELEMENT|CreditsLabel")
	if hull_bar == null: _flag("HUD_MISSING_ELEMENT|HullBar")
	if shield_lbl == null: _flag("HUD_MISSING_ELEMENT|ShieldLabel")
	if system_lbl == null: _flag("HUD_MISSING_ELEMENT|NodeLabel")
	if state_lbl == null: _flag("HUD_MISSING_ELEMENT|StateLabel")

	_capture("03_hud")
	_polls = 0
	_phase = Phase.DOCK


func _do_dock() -> void:
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout

	var market: Array = _bridge.call("GetPlayerMarketViewV0", _home_node_id)
	var goods_with_price := 0
	for item in market:
		if int(item.get("buy_price", 0)) > 0:
			goods_with_price += 1
	# Soft flag — starting station should have goods (design issue if not)
	if goods_with_price < 3:
		_flag("HOME_MARKET_EMPTY|goods=%d" % goods_with_price)
	_log("DOCK|goods_with_price=%d" % goods_with_price)

	_market_snapshots[_home_node_id] = market
	# Capture dock panel as-opened (before tab switch) — shows tab disclosure state
	var _dock_menu = root.find_child("HeroTradeMenu", true, false)
	_capture("04a_dock_panel_open")

	# Ensure Market tab is visible for the market screenshot
	if _dock_menu and _dock_menu.has_method("_switch_dock_tab"):
		_dock_menu.call("_switch_dock_tab", 0)
		await create_timer(0.15).timeout
	_capture("04b_dock_market")

	# Goal 2 probe: tutorial text scan + dock tab count
	var tutorial_found := false
	for label in _find_all_label3d():
		var lt: String = label.text.to_lower()
		if "tutorial" in lt or "press x" in lt or "click here" in lt:
			tutorial_found = true
			break
	_tutorial_text_found = tutorial_found
	_log("GOAL|TEACHES|tutorial_text_found=%s" % str(tutorial_found))
	_log("GOAL|TEACHES|system_introduced=market")
	_track_system_introduced("market")
	_probe_dock_tabs()

	# Change 5+13: Onboarding state at first dock
	if _bridge.has_method("GetOnboardingStateV0"):
		_dock1_onboarding = _bridge.call("GetOnboardingStateV0").duplicate()
		_log("ONBOARDING|dock1=%s" % str(_dock1_onboarding))

	_busy = false
	_polls = 0
	_phase = Phase.CHECK_FO


# ===================== Act 2: First Trade =====================

func _do_check_fo() -> void:
	_log("ACT_2|First Trade")
	_act_start_frames["ACT_2"] = _total_frames
	var fo_panel = root.find_child("FOPanel", true, false)
	if fo_panel != null:
		# Scan for dev-facing text
		var all_text := _collect_label_text(fo_panel)
		if "Score:" in all_text: _flag("FO_PANEL_DEV_STATE|Score")
		if "War Faces" in all_text: _flag("FO_PANEL_DEV_STATE|WarFaces")
		if "No known NPCs" in all_text: _flag("FO_PANEL_DEV_STATE|NoKnownNPCs")

	# Auto-promote FO so dialogue triggers fire during the run.
	if _bridge.has_method("GetFirstOfficerCandidatesV0"):
		var candidates: Array = _bridge.call("GetFirstOfficerCandidatesV0")
		if candidates.size() > 0:
			var ctype := str(candidates[0].get("type", ""))
			if not ctype.is_empty() and _bridge.has_method("PromoteFirstOfficerV0"):
				var ok: bool = _bridge.call("PromoteFirstOfficerV0", ctype)
				_fo_promoted = ok
				_log("FO_PROMOTE|candidate=%s success=%s" % [ctype, str(ok)])

	# Goal 3 probe: FO state at first dock
	_probe_fo_state()
	_probe_fo_dialogue("FIRST_DOCK")

	_capture("05_fo_panel")
	_polls = 0
	_phase = Phase.BUY


func _do_buy() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_before_buy = int(ps.get("credits", 0))
	_cargo_before = int(ps.get("cargo_count", 0))
	var node_id := str(ps.get("current_node_id", ""))

	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)

	# Smart trade: peek at neighbor market to find highest-margin good
	var best_good := ""
	var best_price := 999999
	var best_margin := -999999

	# Get first neighbor's market for margin comparison
	var neighbor_market: Array = []
	if _neighbor_ids.size() > 0:
		var nid := str(_neighbor_ids[0])
		neighbor_market = _bridge.call("GetPlayerMarketViewV0", nid)

	# Build neighbor sell price lookup
	var neighbor_sell_prices := {}
	for item in neighbor_market:
		neighbor_sell_prices[str(item.get("good_id", ""))] = int(item.get("sell_price", 0))

	for item in market:
		var price := int(item.get("buy_price", 0))
		var qty := int(item.get("quantity", 0))
		if price <= 0 or qty <= 0:
			continue
		var gid := str(item.get("good_id", ""))
		# Compute expected margin if we sell at neighbor
		var sell_at_neighbor := int(neighbor_sell_prices.get(gid, 0))
		var margin := sell_at_neighbor - price
		if margin > best_margin:
			best_margin = margin
			best_good = gid
			best_price = price
		elif margin == best_margin and price < best_price:
			best_good = gid
			best_price = price

	# Fallback: if no positive margin found, pick cheapest (existing behavior)
	if best_margin <= 0:
		best_good = ""
		best_price = 999999
		for item in market:
			var price := int(item.get("buy_price", 0))
			var qty := int(item.get("quantity", 0))
			if price > 0 and qty > 0 and price < best_price:
				best_price = price
				best_good = str(item.get("good_id", ""))

	if best_good.is_empty():
		_flag("NO_AFFORDABLE_GOOD")
		_phase = Phase.UNDOCK_1
		return

	var buy_qty := mini(5, _credits_before_buy / best_price)
	if buy_qty < 1:
		buy_qty = 1
	var dispatch_result = _bridge.call("DispatchPlayerTradeV0", node_id, best_good, buy_qty, true)
	_bought_good_id = best_good
	_goods_traded[best_good] = true
	_log("BUY|good=%s qty=%d price=%d" % [best_good, buy_qty, best_price])

	# Goal 4 probe: log the computed margin
	_profit_margin = best_margin
	_log("GOAL|PROFIT|margin=%d good=%s" % [best_margin, best_good])

	# Wait for state update
	_busy = true
	await create_timer(0.2).timeout
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps2.get("credits", 0))
	var cargo_after := int(ps2.get("cargo_count", 0))

	# Detect silent dispatch failure
	if credits_after == _credits_before_buy and cargo_after == _cargo_before:
		_dispatch_failures += 1
		_flag("DISPATCH_SILENT_FAIL|buy good=%s dispatch=%s" % [best_good, str(dispatch_result)])

	_assert(credits_after < _credits_before_buy, "buy_credits_decreased",
		"before=%d after=%d" % [_credits_before_buy, credits_after])
	_assert(cargo_after > _cargo_before, "buy_cargo_increased",
		"before=%d after=%d" % [_cargo_before, cargo_after])

	_capture("06_post_buy")
	_busy = false
	_polls = 0
	_phase = Phase.UNDOCK_1


func _do_undock(next_phase: Phase) -> void:
	if _game_manager != null and _game_manager.has_method("undock_v0"):
		_game_manager.call("undock_v0")
		_log("UNDOCK")
	_capture("07_flight_cargo")
	_polls = 0
	_phase = next_phase


func _do_travel(neighbor_idx: int, settle_phase: Phase) -> void:
	# Always refresh neighbors from current position
	_refresh_neighbors()
	# Pick an unvisited neighbor preferentially, then fall back to idx
	var dest := ""
	for nid in _neighbor_ids:
		if not _visited.has(nid):
			dest = str(nid)
			break
	if dest.is_empty() and neighbor_idx < _neighbor_ids.size():
		dest = str(_neighbor_ids[neighbor_idx])
	if dest.is_empty() and _neighbor_ids.size() > 0:
		dest = str(_neighbor_ids[0])
	if dest.is_empty():
		_log("TRAVEL|no_neighbors")
		_polls = 0
		_phase = settle_phase
		return

	_log("TRAVEL|dest=%s" % dest)
	_headless_travel(dest)
	_visited[dest] = true
	_polls = 0
	_phase = settle_phase


func _do_arrival_1() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var current := str(ps.get("current_node_id", ""))
	_assert(current != _home_node_id, "arrival_different_system",
		"home=%s current=%s" % [_home_node_id, current])
	_log("ARRIVAL_1|node=%s" % current)
	_track_faction(current)
	_capture("09_arrival_1")

	# Dock at new station
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout
	_busy = false
	_polls = 0
	_phase = Phase.DOCK_2


func _do_dock_2() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	_market_snapshots[node_id] = market

	# Check prices differ from home station
	if _market_snapshots.has(_home_node_id):
		var home_market: Array = _market_snapshots[_home_node_id]
		var differs := _markets_differ(home_market, market)
		if not differs:
			_flag("PRICE_IDENTICAL|%s vs %s" % [_home_node_id, node_id])

	_log("DOCK_2|node=%s goods=%d" % [node_id, market.size()])
	_capture("10a_dock2_panel_open")  # Tab disclosure state after first trade
	_capture("10b_dock_2")
	_polls = 0
	_phase = Phase.SELL


func _do_sell() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var credits_before_sell := int(ps.get("credits", 0))

	var cargo_before_sell := int(ps.get("cargo_count", 0))
	if not _bought_good_id.is_empty():
		if cargo_before_sell > 0:
			_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, cargo_before_sell, false)
			_log("SELL|good=%s qty=%d" % [_bought_good_id, cargo_before_sell])

	_busy = true
	await create_timer(0.2).timeout
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_after_sell = int(ps2.get("credits", 0))
	var cargo_after := int(ps2.get("cargo_count", 0))

	# Detect silent dispatch failure
	if cargo_before_sell > 0 and cargo_after == cargo_before_sell and _credits_after_sell == credits_before_sell:
		_dispatch_failures += 1
		_flag("DISPATCH_SILENT_FAIL|sell good=%s" % _bought_good_id)

	if _credits_after_sell > credits_before_sell:
		_trades_completed += 1
	else:
		_flag("SELL_NO_PROFIT|before=%d after=%d" % [credits_before_sell, _credits_after_sell])

	# Goal 2 probe: selling is the second system introduced
	_log("GOAL|TEACHES|system_introduced=selling")
	_track_system_introduced("selling")

	# Switch to Market tab so profit flash / updated balances are visible
	var _dock_menu = root.find_child("HeroTradeMenu", true, false)
	if _dock_menu and _dock_menu.has_method("_switch_dock_tab"):
		_dock_menu.call("_switch_dock_tab", 0)
		await create_timer(0.15).timeout

	_capture("11_post_sell")
	_busy = false
	_polls = 0
	_phase = Phase.PROFIT_CHECK


func _do_profit_check() -> void:
	if _credits_after_sell <= _credits_at_start:
		_flag("FIRST_TRADE_NO_PROFIT|start=%d now=%d" % [_credits_at_start, _credits_after_sell])
	var delta := _credits_after_sell - _credits_at_start
	var pct := 0
	if _credits_at_start > 0:
		pct = (delta * 100) / _credits_at_start
	_log("PROFIT|start=%d now=%d delta=%d" % [_credits_at_start, _credits_after_sell, delta])

	# Goal 4 probe: profit delta + FO reaction
	_probe_fo_dialogue("SELL")
	var fo_reacted := _fo_dialogue_count > 0
	_fo_reacted_to_profit = fo_reacted
	_log("GOAL|PROFIT|delta=%d pct=%d fo_reacted=%s" % [delta, pct, str(fo_reacted)])

	_polls = 0
	_phase = Phase.CHECK_MISSIONS


# ===================== Act 3: First Mission =====================

func _do_check_missions() -> void:
	_log("ACT_3|First Mission")
	_act_start_frames["ACT_3"] = _total_frames
	if not _bridge.has_method("GetMissionListV0"):
		_log("MISSIONS|bridge_missing_method")
		_phase = Phase.FIND_HOSTILE
		return
	var missions: Array = _bridge.call("GetMissionListV0")
	_missions_available_count = missions.size()
	_assert(missions.size() >= 1, "missions_available", "count=%d" % missions.size())
	_log("MISSIONS|available=%d" % missions.size())

	# Goal 2 probe: missions are the third system introduced
	_log("GOAL|TEACHES|system_introduced=missions")
	_track_system_introduced("missions")
	_polls = 0
	_phase = Phase.ACCEPT_MISSION


func _do_accept_mission() -> void:
	if not _bridge.has_method("AcceptMissionV0"):
		_phase = Phase.UNDOCK_2
		return
	var missions: Array = _bridge.call("GetMissionListV0")
	if missions.size() > 0:
		var mission_id := str(missions[0].get("mission_id", ""))
		if not mission_id.is_empty():
			var accepted: bool = _bridge.call("AcceptMissionV0", mission_id)
			_log("ACCEPT|mission=%s success=%s" % [mission_id, str(accepted)])
			if accepted:
				_mission_accepted = true
				var active: Dictionary = _bridge.call("GetActiveMissionV0")
				_assert(not active.is_empty(), "mission_active", "")
	_capture("14_mission_accepted")
	_polls = 0
	_phase = Phase.UNDOCK_2


func _do_arrival_2() -> void:
	_assert(_visited.size() >= 3, "visited_3_nodes", "count=%d" % _visited.size())
	_log("ARRIVAL_2|visited=%d" % _visited.size())
	_capture("16_system_3")
	_polls = 0
	_phase = Phase.FIND_HOSTILE


# ===================== Act 4: First Combat + Upgrade =====================

func _do_find_hostile() -> void:
	_log("ACT_4|First Combat")
	_act_start_frames["ACT_4"] = _total_frames
	# Use GetSystemSnapshotV0 to find NPC fleets at current system
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var npc_fleet_id := ""

	if _bridge.has_method("GetSystemSnapshotV0"):
		var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", node_id)
		var fleets: Array = snap.get("fleets", [])
		for fleet in fleets:
			var fid := str(fleet.get("fleet_id", ""))
			var owner := str(fleet.get("owner_id", ""))
			if owner != "player" and not fid.is_empty():
				npc_fleet_id = fid
				break

	# If none at current system, check all visited systems
	if npc_fleet_id.is_empty():
		for vid in _visited:
			if _bridge.has_method("GetSystemSnapshotV0"):
				var snap2: Dictionary = _bridge.call("GetSystemSnapshotV0", str(vid))
				var fleets2: Array = snap2.get("fleets", [])
				for fleet in fleets2:
					var fid := str(fleet.get("fleet_id", ""))
					var owner := str(fleet.get("owner_id", ""))
					if owner != "player" and not fid.is_empty():
						npc_fleet_id = fid
						break
			if not npc_fleet_id.is_empty():
				break

	if npc_fleet_id.is_empty():
		_log("COMBAT|no_npc_fleet_found")
		_flag("NO_NPC_FLEET_FOR_COMBAT")
		_phase = Phase.DOCK_3
		return
	_log("COMBAT|target=%s" % npc_fleet_id)
	# Store for combat phase
	set_meta("_combat_target", npc_fleet_id)
	_polls = 0
	_phase = Phase.COMBAT


func _do_combat() -> void:
	var fleet_id: String = get_meta("_combat_target", "")
	if fleet_id.is_empty():
		_phase = Phase.POST_COMBAT
		return

	# Initialize combat HP for all fleets (player + NPC) before first hit.
	if _bridge.has_method("InitFleetCombatHpV0"):
		_bridge.call("InitFleetCombatHpV0")

	# Battle stations spin-up (Change 1)
	if _bridge.has_method("ToggleBattleStationsV0"):
		var bs: Dictionary = _bridge.call("ToggleBattleStationsV0")
		var new_state := str(bs.get("new_state", ""))
		_log("COMBAT|battle_stations=%s" % new_state)
	if _bridge.has_method("GetBattleStationsStateV0"):
		var bss: Dictionary = _bridge.call("GetBattleStationsStateV0")
		_log("COMBAT|bs_state=%s" % str(bss))

	var ps_before: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_before_combat = int(ps_before.get("credits", 0))

	# Interleaved combat: player hit → NPC shot → player hit → NPC shot → player hit.
	# NPC must fire BEFORE being destroyed by player's 3rd hit (150 total kills most NPCs).
	_busy = true
	var total_dmg := 0
	var npc_shots_fired := 0
	var has_ai_shot: bool = _bridge.has_method("ApplyAiShotAtPlayerV0")

	# Round 1: player fires, NPC returns fire
	_bridge.call("DamageNpcFleetV0", fleet_id, 50)
	total_dmg += 50
	await create_timer(0.15).timeout
	_capture("18a_combat_hit1")
	if has_ai_shot:
		var shot1: Dictionary = _bridge.call("ApplyAiShotAtPlayerV0", fleet_id)
		_log("COMBAT|npc_shot hull=%d shield=%d" % [int(shot1.get("player_hull", -1)), int(shot1.get("player_shield", -1))])
		npc_shots_fired += 1
		await create_timer(0.1).timeout

	# Round 2: NPC fires multiple shots to deplete shield and hit hull (combat tension)
	# Fire rapid volleys without pausing — SustainSystem auto-repairs between awaits
	for _volley in range(14):
		if has_ai_shot:
			var shot_v: Dictionary = _bridge.call("ApplyAiShotAtPlayerV0", fleet_id)
			var shot_hull: int = int(shot_v.get("player_hull", 100))
			_log("COMBAT|npc_shot hull=%d shield=%d" % [shot_hull, int(shot_v.get("player_shield", -1))])
			# Track min hull inline — auto-repair races the post-combat HP read
			if shot_hull < _min_hull_seen:
				_min_hull_seen = shot_hull
			npc_shots_fired += 1

	# Round 3: player fires twice, finishing blow (NPC likely dead after this)
	_bridge.call("DamageNpcFleetV0", fleet_id, 50)
	total_dmg += 50
	await create_timer(0.1).timeout
	_bridge.call("DamageNpcFleetV0", fleet_id, 50)
	total_dmg += 50

	_log("COMBAT|hits=3 dmg=%d npc_shots=%d target=%s" % [total_dmg, npc_shots_fired, fleet_id])

	# Heat system probe (Change 1)
	if _bridge.has_method("GetHeatSnapshotV0"):
		var heat: Dictionary = _bridge.call("GetHeatSnapshotV0")
		_heat_capacity = int(heat.get("heat_capacity", 0))
		_log("COMBAT|heat_capacity=%d rejection_rate=%s" % [
			_heat_capacity, str(heat.get("rejection_rate", 0))])
	if _bridge.has_method("GetRadiatorStatusV0"):
		var rad: Dictionary = _bridge.call("GetRadiatorStatusV0")
		_radiator_intact = bool(rad.get("is_intact", false))
		_log("COMBAT|radiator_intact=%s bonus_rate=%s" % [
			str(_radiator_intact), str(rad.get("bonus_rate", 0))])
	if _bridge.has_method("GetRecentCombatEventsV0"):
		var events: Array = _bridge.call("GetRecentCombatEventsV0")
		_log("COMBAT|recent_events=%d" % events.size())

	_busy = false

	# Check player survived + track min hull for tension metric
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull := int(hp.get("hull", 100))
		var hull_max := int(hp.get("hull_max", 100))
		if hull_max > 0:
			var hull_pct := (hull * 100) / hull_max
			if hull_pct < _min_hull_seen:
				_min_hull_seen = hull_pct
		if hull <= 0:
			_flag("COMBAT_ONE_SHOT")
		_log("COMBAT|player_hull=%d min_hull_pct=%d" % [hull, _min_hull_seen])

	_combats_completed += 1
	_capture("18_combat")

	# Goal 2 + Goal 3 probes: combat introduced, FO reaction
	_log("GOAL|TEACHES|system_introduced=combat")
	_track_system_introduced("combat")
	_probe_fo_dialogue("COMBAT")

	_polls = 0
	_phase = Phase.POST_COMBAT


func _do_post_combat() -> void:
	# Wait for destruction + auto-loot collection.
	# game_manager._poll_auto_loot_v0 runs every 0.5s while IN_FLIGHT and auto-collects nearby loot.
	# Wait 1.0s to ensure both destruction processing and auto-collection complete.
	_busy = true
	await create_timer(1.0).timeout

	# Measure loot via credit increase since _credits_before_combat (captured in _do_combat).
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps.get("credits", 0))
	var loot_credits := credits_after - _credits_before_combat
	_log("POST_COMBAT|credits=%d loot=%d" % [credits_after, loot_credits])
	_busy = false
	_polls = 0
	_phase = Phase.DOCK_3


func _do_dock_3() -> void:
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout

	# Change 5+13: Onboarding state at third dock (post-combat)
	if _bridge.has_method("GetOnboardingStateV0"):
		_dock3_onboarding = _bridge.call("GetOnboardingStateV0").duplicate()
		_log("ONBOARDING|dock3=%s" % str(_dock3_onboarding))

	_capture("20_dock_upgrade")
	_busy = false
	_polls = 0
	_phase = Phase.CHECK_MODULES


func _do_check_modules() -> void:
	if not _bridge.has_method("GetAvailableModulesV0"):
		_log("MODULES|no_method")
		_phase = Phase.GALAXY_MAP
		return
	var modules: Array = _bridge.call("GetAvailableModulesV0")
	# Retry once on read-lock contention (TryExecuteSafeRead(0) can return empty cache)
	if modules.size() == 0:
		await create_timer(0.2).timeout
		modules = _bridge.call("GetAvailableModulesV0")
	_assert(modules.size() >= 1, "modules_available", "count=%d" % modules.size())
	_log("MODULES|available=%d" % modules.size())
	_polls = 0
	_phase = Phase.INSTALL_MODULE


func _do_install_module() -> void:
	if not _bridge.has_method("InstallModuleV0") or not _bridge.has_method("GetPlayerFleetSlotsV0"):
		_phase = Phase.GALAXY_MAP
		return
	var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
	var modules: Array = _bridge.call("GetAvailableModulesV0")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits := int(ps.get("credits", 0))

	# Find a (module, slot) pair where slot_kind matches and slot is empty
	var install_slot := -1
	var install_module := ""
	for i in range(slots.size()):
		var installed := str(slots[i].get("installed_module_id", ""))
		if not installed.is_empty() and installed != "null" and installed != "None":
			continue
		var slot_kind := str(slots[i].get("slot_kind", ""))
		# Find cheapest affordable module matching this slot kind
		for mod in modules:
			var mod_kind := str(mod.get("slot_kind", ""))
			var cost := int(mod.get("credit_cost", 0))
			var can_install: bool = mod.get("can_install", false)
			if mod_kind == slot_kind and cost > 0 and cost <= credits and can_install:
				install_slot = i
				install_module = str(mod.get("module_id", ""))
				break
		if install_slot >= 0:
			break

	if install_slot >= 0 and not install_module.is_empty():
		var result: Dictionary = _bridge.call("InstallModuleV0", "fleet_trader_1", install_slot, install_module)
		var success: bool = result.get("success", false)
		_log("INSTALL|module=%s slot=%d success=%s" % [install_module, install_slot, str(success)])

		# Change 2: Module remove + re-install
		if success and _bridge.has_method("RemoveModuleV0"):
			var remove_result: Dictionary = _bridge.call("RemoveModuleV0", "fleet_trader_1", install_slot)
			var removed: bool = remove_result.get("success", false)
			_log("REMOVE|slot=%d success=%s" % [install_slot, str(removed)])
			# Re-install so the rest of the bot works with upgraded ship
			if removed:
				_bridge.call("InstallModuleV0", "fleet_trader_1", install_slot, install_module)
	elif install_slot < 0:
		_log("INSTALL|no_matching_slot")
	else:
		_flag("UPGRADE_TOO_EXPENSIVE")
		_log("INSTALL|no_affordable_module")

	# Goal 2 + Goal 5 probes: fitting introduced, remaining empty slots
	_log("GOAL|TEACHES|system_introduced=fitting")
	_track_system_introduced("fitting")
	var empty_slots := 0
	if _bridge.has_method("GetPlayerFleetSlotsV0"):
		var all_slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
		for s in all_slots:
			var inst := str(s.get("installed_module_id", ""))
			if inst.is_empty() or inst == "null" or inst == "None":
				empty_slots += 1
	_log("GOAL|DEPTH|empty_slots=%d" % empty_slots)
	_empty_slots_at_fit = empty_slots

	_capture("22_ship_fitted")
	_polls = 0
	_phase = Phase.TAB_CYCLE


# ── Tab Cycle: Capture all visible dock tabs ──────────────────────

func _do_tab_cycle() -> void:
	_busy = true
	var dock_menu = root.find_child("HeroTradeMenu", true, false)
	if dock_menu == null or not dock_menu.has_method("_switch_dock_tab"):
		_log("TAB_CYCLE|skip no_dock_menu")
		_busy = false
		_phase = Phase.GALAXY_MAP
		return

	# Tab indices: 0=Market, 1=Jobs, 2=Ship, 3=Station, 4=Intel, 6=Diplomacy
	var tabs_to_capture: Array = [
		[1, "23_tab_jobs"],
		[2, "23_tab_ship"],
		[4, "23_tab_intel"],
		[6, "23_tab_diplo"],
	]
	for entry in tabs_to_capture:
		var idx: int = entry[0]
		var label: String = entry[1]
		dock_menu.call("_switch_dock_tab", idx)
		await create_timer(0.15).timeout
		_capture(label)

	# Return to Station tab
	dock_menu.call("_switch_dock_tab", 3)
	await create_timer(0.1).timeout
	_log("TAB_CYCLE|captured %d tabs" % tabs_to_capture.size())
	_busy = false
	_polls = 0
	_phase = Phase.GALAXY_MAP


# ===================== Act 5: Galaxy Opens =====================

func _do_galaxy_map() -> void:
	_log("ACT_5|Galaxy Opens")
	_act_start_frames["ACT_5"] = _total_frames
	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var nodes: Array = galaxy.get("system_nodes", [])
	var edges: Array = galaxy.get("lane_edges", [])
	# Retry on read-lock contention (TryExecuteSafeRead(0) returns stale empty cache)
	if nodes.size() == 0 and _polls < 60:
		_polls += 1
		return
	_assert(nodes.size() >= 8, "galaxy_nodes", "count=%d" % nodes.size())
	_assert(edges.size() >= 7, "galaxy_edges", "count=%d" % edges.size())
	_log("GALAXY|nodes=%d edges=%d" % [nodes.size(), edges.size()])

	# Goal 5 probe: explored percentage
	var explored_pct := 0
	if nodes.size() > 0:
		explored_pct = (_visited.size() * 100) / nodes.size()
	_log("GOAL|DEPTH|explored_pct=%d" % explored_pct)

	_polls = 0
	_phase = Phase.UNDOCK_4


func _do_multi_hop() -> void:
	# Navigate to 2 more systems (4th and 5th unique) with settle between hops
	# Build faction BFS path (shared with deep_explore) for up to 5 total hops
	_busy = true

	# Build faction map from territory overlay
	if _bridge.has_method("GetFactionTerritoryOverlayV0"):
		var terr: Dictionary = _bridge.call("GetFactionTerritoryOverlayV0")
		for nid in terr:
			var info: Dictionary = terr[nid]
			var fid := str(info.get("controlling_faction", ""))
			if not fid.is_empty():
				_faction_map[str(nid)] = fid

	# BFS to nearest new-faction node (up to 5 hops: 2 multi_hop + 3 deep_explore)
	var bfs_result: Array = _bfs_to_new_faction(_faction_map)
	if bfs_result.size() > 0:
		_faction_bfs_path = bfs_result[0]
		var bfs_target: String = bfs_result[1] if bfs_result.size() > 1 else ""
		_log("FACTION_BFS|path=%s target=%s faction=%s" % [
			str(_faction_bfs_path), bfs_target, str(_faction_map.get(bfs_target, ""))])

	for _hop_i in range(2):
		var target := ""
		# Use BFS path if available (indexes 0-1 for multi_hop)
		if _hop_i < _faction_bfs_path.size():
			target = str(_faction_bfs_path[_hop_i])
		else:
			_refresh_neighbors()
			# Priority 1: unvisited neighbor in a NEW faction
			for nid in _neighbor_ids:
				if _visited.has(nid):
					continue
				var nid_faction := _get_node_faction(str(nid))
				if not nid_faction.is_empty() and not _factions_visited.has(nid_faction):
					target = str(nid)
					break
			# Priority 2: any unvisited neighbor
			if target.is_empty():
				for nid in _neighbor_ids:
					if not _visited.has(nid):
						target = str(nid)
						break
		if target.is_empty() and _neighbor_ids.size() > 0:
			target = str(_neighbor_ids[0])
		if not target.is_empty():
			_log("MULTI_HOP|dest=%s" % target)
			_headless_travel(target)
			_visited[target] = true
			if _faction_map.has(target) and not str(_faction_map[target]).is_empty():
				_factions_visited[str(_faction_map[target])] = true
			else:
				_track_faction(target)
			await create_timer(0.5).timeout  # Let scene rebuild between hops

	_capture("24_system_5")
	_busy = false
	_polls = 0
	_phase = Phase.SETTLE_HOP


func _do_trade_route() -> void:
	# Execute a 2nd profitable trade at current location — dock first
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var credits_before := int(ps.get("credits", 0))

	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	_market_snapshots[node_id] = market

	# Try to buy something
	var best_good := ""
	var best_price := 999999
	for item in market:
		var price := int(item.get("buy_price", 0))
		var qty := int(item.get("quantity", 0))
		if price > 0 and qty > 0 and price < best_price:
			best_price = price
			best_good = str(item.get("good_id", ""))

	if not best_good.is_empty():
		var buy_qty := mini(2, credits_before / best_price)
		if buy_qty >= 1:
			_bridge.call("DispatchPlayerTradeV0", node_id, best_good, buy_qty, true)
			# Sell immediately at same station (may not profit, but tests the loop)
			_bridge.call("DispatchPlayerTradeV0", node_id, best_good, buy_qty, false)
			_trades_completed += 1
			_goods_traded[best_good] = true
			_log("TRADE_ROUTE|good=%s qty=%d" % [best_good, buy_qty])
	else:
		_log("TRADE_ROUTE|no_goods_at_%s" % node_id)

	_busy = false
	_polls = 0
	_phase = Phase.CHECK_SUSTAIN


func _do_check_sustain() -> void:
	if not _bridge.has_method("GetFleetSustainStatusV0"):
		_log("SUSTAIN|no_method")
		_phase = Phase.DEEP_EXPLORE
		return
	var sustain: Dictionary = _bridge.call("GetFleetSustainStatusV0", "fleet_trader_1")
	var fuel := int(sustain.get("fuel", -1))
	if fuel >= 0:
		_assert(fuel > 0, "sustain_fuel_positive", "fuel=%d" % fuel)
		if fuel > 0 and fuel < 20:
			_flag("FUEL_CRITICAL|fuel=%d" % fuel)
	_log("SUSTAIN|fuel=%d" % fuel)
	_polls = 0
	_phase = Phase.DEEP_EXPLORE


# ===================== Act 6: Scale Reveal =====================

func _do_deep_explore() -> void:
	_log("ACT_6|Scale Reveal")
	_act_start_frames["ACT_6"] = _total_frames
	_busy = true

	_log("DEEP|factions_visited=%s bfs_remaining=%d" % [
		str(_factions_visited.keys()), maxi(0, _faction_bfs_path.size() - 2)])

	# Navigate up to 3 hops — continue BFS path from multi_hop (index 2+), else greedy
	for _hop in range(3):
		var bfs_idx := _hop + 2  # multi_hop used indexes 0-1
		var target := ""
		if bfs_idx < _faction_bfs_path.size():
			target = str(_faction_bfs_path[bfs_idx])
		else:
			_refresh_neighbors()
			# Priority 1: unvisited neighbor in a NEW faction
			for nid in _neighbor_ids:
				if _visited.has(nid):
					continue
				var nid_faction := _get_node_faction(str(nid))
				if not nid_faction.is_empty() and not _factions_visited.has(nid_faction):
					target = str(nid)
					break
			# Priority 2: any unvisited neighbor
			if target.is_empty():
				for nid in _neighbor_ids:
					if not _visited.has(nid):
						target = str(nid)
						break
		if target.is_empty():
			_log("DEEP|hop=%d no_target" % _hop)
			break
		_headless_travel(target)
		_visited[target] = true
		if _faction_map.has(target) and not str(_faction_map[target]).is_empty():
			_factions_visited[str(_faction_map[target])] = true
		else:
			_track_faction(target)
		_log("DEEP|hop=%d dest=%s faction=%s" % [_hop, target, str(_faction_map.get(target, ""))])
		await create_timer(0.5).timeout  # Let scene rebuild between hops

	_capture("27_deep_explore")
	_busy = false
	_polls = 0
	_phase = Phase.SETTLE_DEEP


## BFS from current node to nearest node with a faction not yet visited.
## Searches the full graph, returns [path_array (max 5 steps), target_node_id].
func _bfs_to_new_faction(faction_map: Dictionary) -> Array:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var start := str(ps.get("current_node_id", ""))
	if start.is_empty():
		return []

	# Build adjacency from edge list
	var adj := {}  # node_id -> Array[node_id]
	for lane in _all_edges:
		var from_id := str(lane.get("from_id", ""))
		var to_id := str(lane.get("to_id", ""))
		if not adj.has(from_id): adj[from_id] = []
		if not adj.has(to_id): adj[to_id] = []
		adj[from_id].append(to_id)
		adj[to_id].append(from_id)

	# BFS with parent tracking — no depth limit (galaxy is only 20 nodes)
	var queue: Array = [start]
	var parent := {}  # node_id -> parent_node_id
	parent[start] = ""
	var found := ""

	while queue.size() > 0:
		var node: String = queue.pop_front()
		# Check if this node has a new faction (skip start node)
		if node != start and faction_map.has(node):
			var f: String = faction_map[node]
			if not f.is_empty() and not _factions_visited.has(f):
				found = node
				break
		# Expand neighbors
		if adj.has(node):
			for neighbor in adj[node]:
				if not parent.has(neighbor):
					parent[neighbor] = node
					queue.append(neighbor)

	if found.is_empty():
		return []

	# Reconstruct full path from start to found
	var full_path: Array = []
	var cur := found
	while cur != start and not cur.is_empty():
		full_path.push_front(cur)
		cur = parent.get(cur, "")

	# Return [path (max 5 steps: 2 multi_hop + 3 deep_explore), target_node_id]
	var path: Array = full_path.slice(0, 5) if full_path.size() > 5 else full_path
	return [path, found]


func _do_price_diversity() -> void:
	# Snapshot market at current node
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	_market_snapshots[node_id] = market

	# Count unique price profiles
	var unique_profiles := 0
	var profile_hashes: Dictionary = {}
	for nid in _market_snapshots:
		var mk: Array = _market_snapshots[nid]
		var hash_str := ""
		for item in mk:
			hash_str += "%s:%d," % [str(item.get("good_id", "")), int(item.get("buy_price", 0))]
		if not profile_hashes.has(hash_str):
			profile_hashes[hash_str] = true
			unique_profiles += 1

	_assert(unique_profiles >= 3, "price_diversity", "profiles=%d" % unique_profiles)
	_log("PRICE_DIVERSITY|unique=%d total=%d" % [unique_profiles, _market_snapshots.size()])

	# Goal 1 probe: price diversity re-emit
	_log("GOAL|ALIVE|price_profiles=%d" % unique_profiles)

	_polls = 0
	_phase = Phase.CHECK_RESEARCH


func _do_check_research() -> void:
	if not _bridge.has_method("GetTechTreeV0"):
		_log("RESEARCH|no_method")
		_phase = Phase.FINAL_TRADE
		return
	var techs: Array = _bridge.call("GetTechTreeV0")
	_assert(techs.size() >= 1, "tech_available", "count=%d" % techs.size())
	_log("RESEARCH|techs=%d" % techs.size())
	_tech_count = techs.size()

	# Goal 5 probe: tech depth
	_log("GOAL|DEPTH|tech_count=%d" % techs.size())

	_polls = 0
	_phase = Phase.FINAL_TRADE


func _do_final_trade() -> void:
	# One more profitable trade
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var credits_before := int(ps.get("credits", 0))

	# Dock first
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout

	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	var best_good := ""
	var best_price := 999999
	for item in market:
		var price := int(item.get("buy_price", 0))
		var qty := int(item.get("quantity", 0))
		if price > 0 and qty > 0 and price < best_price:
			best_price = price
			best_good = str(item.get("good_id", ""))

	if not best_good.is_empty() and best_price <= credits_before:
		_bridge.call("DispatchPlayerTradeV0", node_id, best_good, 1, true)
		_bridge.call("DispatchPlayerTradeV0", node_id, best_good, 1, false)
		_trades_completed += 1
		_goods_traded[best_good] = true

	_capture("30_final_trade")
	_busy = false
	_polls = 0
	_phase = Phase.CAPTURE_EMPIRE_DASH


# ── Empire Dashboard + Galaxy Map Captures ──────────────────────

func _do_capture_empire_dash() -> void:
	_busy = true
	# Undock first if still docked
	var gm = root.get_node_or_null("GameManager")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	if str(ps.get("ship_state_token", "")) == "DOCKED" and gm and gm.has_method("undock_v0"):
		gm.call("undock_v0")
		await create_timer(0.3).timeout

	if gm and gm.has_method("_toggle_empire_dashboard_v0"):
		gm.call("_toggle_empire_dashboard_v0")
		await create_timer(0.3).timeout
		_capture("32_empire_dashboard")
		# Close it
		gm.call("_toggle_empire_dashboard_v0")
		await create_timer(0.15).timeout
	else:
		_log("EMPIRE_DASH|skip no_method")
	_busy = false
	_polls = 0
	_phase = Phase.CAPTURE_GALAXY_MAP

func _do_capture_galaxy_map() -> void:
	_busy = true
	var gm = root.get_node_or_null("GameManager")
	if gm and gm.has_method("toggle_galaxy_map_overlay_v0"):
		gm.call("toggle_galaxy_map_overlay_v0")
		# Wait for camera tween (0.6s) + spring convergence (~1.5s)
		await create_timer(2.0).timeout
		_capture("33_galaxy_map")
		# Close it
		gm.call("toggle_galaxy_map_overlay_v0")
		await create_timer(0.15).timeout
	else:
		_log("GALAXY_MAP_CAP|skip no_method")
	_busy = false
	_polls = 0
	_phase = Phase.PROBE_SYSTEMIC


# ===================== Act 7: Depth Probes =====================

func _do_probe_systemic() -> void:
	# Probe systemic mission offers (WAR_DEMAND/PRICE_SPIKE/SUPPLY_SHORTAGE)
	if _bridge.has_method("GetSystemicOffersV0"):
		var offers: Array = _bridge.call("GetSystemicOffersV0")
		_systemic_offers = offers.size()
		_log("SYSTEMIC|offers=%d" % offers.size())
		_log("GOAL|SYSTEMIC|offers=%d" % offers.size())
		if offers.size() > 0:
			var offer = offers[0]
			_log("SYSTEMIC|first_offer trigger=%s good=%s" % [
				str(offer.get("trigger_type", "")), str(offer.get("good_id", ""))])
			# Change 3: Accept the first systemic mission
			if _bridge.has_method("AcceptSystemicMissionV0"):
				var offer_id := str(offer.get("offer_id", ""))
				if not offer_id.is_empty():
					var accepted: bool = _bridge.call("AcceptSystemicMissionV0", offer_id)
					_log("SYSTEMIC|accept=%s success=%s" % [offer_id, str(accepted)])
					_systemic_mission_accepted = accepted
	else:
		_log("SYSTEMIC|no_method")
	_polls = 0
	_phase = Phase.PROBE_FACTIONS


func _do_probe_factions() -> void:
	# Probe faction system — reputation, territory, doctrines
	if _bridge.has_method("GetAllFactionsV0"):
		var factions: Array = _bridge.call("GetAllFactionsV0")
		_log("FACTIONS|count=%d" % factions.size())
		_log("GOAL|ALIVE|factions=%d" % factions.size())

		# Check reputation with each faction
		if _bridge.has_method("GetPlayerReputationV0"):
			for f in factions:
				var fid := str(f.get("faction_id", ""))
				if not fid.is_empty():
					var rep: Dictionary = _bridge.call("GetPlayerReputationV0", fid)
					var tier := str(rep.get("tier", ""))
					_log("FACTIONS|%s rep_tier=%s" % [fid, tier])

	# Probe warfronts
	if _bridge.has_method("GetWarfrontsV0"):
		var warfronts: Array = _bridge.call("GetWarfrontsV0")
		_warfront_count = warfronts.size()
		_log("WARFRONT|count=%d" % warfronts.size())
		_log("GOAL|ALIVE|warfronts=%d" % warfronts.size())

	# Probe territory at current node
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetTerritoryAccessV0"):
		var access: Dictionary = _bridge.call("GetTerritoryAccessV0", node_id)
		_log("FACTIONS|territory=%s" % str(access))

	_polls = 0
	_phase = Phase.PROBE_KNOWLEDGE


func _do_probe_knowledge() -> void:
	# Probe knowledge web content
	if _bridge.has_method("GetKnowledgeGraphV0"):
		var graph: Array = _bridge.call("GetKnowledgeGraphV0")
		_log("KNOWLEDGE|entries=%d" % graph.size())
		_log("GOAL|DEPTH|knowledge_entries=%d" % graph.size())

	if _bridge.has_method("GetKnowledgeGraphStatsV0"):
		var stats: Dictionary = _bridge.call("GetKnowledgeGraphStatsV0")
		_log("KNOWLEDGE|stats=%s" % str(stats))

	# Data logs
	if _bridge.has_method("GetDiscoveredDataLogsV0"):
		var logs: Array = _bridge.call("GetDiscoveredDataLogsV0")
		_log("KNOWLEDGE|data_logs=%d" % logs.size())

	# Station memory
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetStationMemoryV0"):
		var memory: Dictionary = _bridge.call("GetStationMemoryV0", node_id)
		_log("KNOWLEDGE|station_memory_keys=%d" % memory.size())

	_polls = 0
	_phase = Phase.PROBE_RESEARCH_START


func _do_probe_research_start() -> void:
	# Attempt to start research if available
	if _bridge.has_method("StartResearchV0") and _bridge.has_method("GetTechTreeV0"):
		var techs: Array = _bridge.call("GetTechTreeV0")
		for tech in techs:
			var can_research: bool = tech.get("can_research", false)
			if can_research:
				var tid := str(tech.get("tech_id", ""))
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				var node_id := str(ps.get("current_node_id", ""))
				var result: Dictionary = _bridge.call("StartResearchV0", tid, node_id)
				var success: bool = result.get("success", false)
				_log("RESEARCH|start=%s success=%s" % [tid, str(success)])
				_log("GOAL|DEPTH|research_started=%s" % str(success))
				if success:
					_reward_moment = true
				break

	# Check research status
	if _bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = _bridge.call("GetResearchStatusV0")
		_log("RESEARCH|status=%s" % str(status))

	_polls = 0
	_phase = Phase.PROBE_LEDGER


func _do_probe_ledger() -> void:
	# Transaction ledger
	if _bridge.has_method("GetTransactionLogV0"):
		var log_entries: Array = _bridge.call("GetTransactionLogV0", 10)
		_ledger_entries = log_entries.size()
		_log("LEDGER|transactions=%d" % log_entries.size())
		_log("GOAL|ECONOMY|ledger_entries=%d" % log_entries.size())

	# Profit summary
	if _bridge.has_method("GetProfitSummaryV0"):
		var profit: Dictionary = _bridge.call("GetProfitSummaryV0")
		_log("LEDGER|profit=%s" % str(profit))

	# Player stats
	if _bridge.has_method("GetPlayerStatsV0"):
		var stats: Dictionary = _bridge.call("GetPlayerStatsV0")
		_log("STATS|player=%s" % str(stats))
		_log("GOAL|STATS|nodes_visited=%s trades=%s" % [
			str(stats.get("nodes_visited", 0)), str(stats.get("trades_completed", 0))])

	# Milestones — count achieved, not total definitions
	if _bridge.has_method("GetMilestonesV0"):
		var milestones: Array = _bridge.call("GetMilestonesV0")
		var achieved := 0
		for m in milestones:
			if bool(m.get("achieved", false)):
				achieved += 1
		_milestone_count = achieved
		_log("STATS|milestones_achieved=%d/%d" % [achieved, milestones.size()])

	# Change 6: Price history probe
	if _bridge.has_method("GetPriceHistoryV0") and not _bought_good_id.is_empty():
		var history: Array = _bridge.call("GetPriceHistoryV0", _home_node_id, _bought_good_id)
		_price_history_entries = history.size()
		_log("PRICE_HISTORY|entries=%d good=%s node=%s" % [history.size(), _bought_good_id, _home_node_id])

	_polls = 0
	_phase = Phase.PROBE_REFIT


func _do_probe_refit() -> void:
	# Probe current fleet loadout for evaluator evidence
	if _bridge.has_method("GetPlayerFleetSlotsV0"):
		var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
		var equipped := 0
		var empty := 0
		for slot in slots:
			if str(slot.get("installed_module_id", "")).is_empty():
				empty += 1
			else:
				equipped += 1
		_log("REFIT|slots=%d equipped=%d empty=%d" % [slots.size(), equipped, empty])
		_log("GOAL|DEPTH|refit_slots=%d equipped=%d" % [slots.size(), equipped])
		_track_system_introduced("REFIT")

	# Available modules count (promise of depth)
	if _bridge.has_method("GetAvailableModulesV0"):
		var modules: Array = _bridge.call("GetAvailableModulesV0")
		var installable := 0
		for mod in modules:
			if mod.get("can_install", false):
				installable += 1
		_log("REFIT|available=%d installable=%d" % [modules.size(), installable])
		_log("GOAL|DEPTH|modules_available=%d installable=%d" % [modules.size(), installable])

	_polls = 0
	_phase = Phase.PROBE_AUTOMATION


func _do_probe_automation() -> void:
	# Probe automation program templates
	if _bridge.has_method("GetProgramTemplatesV0"):
		var templates: Array = _bridge.call("GetProgramTemplatesV0")
		_log("AUTOMATION|templates=%d" % templates.size())
		_log("GOAL|DEPTH|automation_templates=%d" % templates.size())
		_track_system_introduced("AUTOMATION")

	# Probe current program state
	if _bridge.has_method("GetProgramExplainSnapshot"):
		var programs: Array = _bridge.call("GetProgramExplainSnapshot")
		_log("AUTOMATION|active_programs=%d" % programs.size())

	# Probe performance metrics
	if _bridge.has_method("GetProgramPerformanceV0"):
		var perf: Dictionary = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
		_log("AUTOMATION|performance_cycles=%s" % str(perf.get("cycles_run", 0)))

	_polls = 0
	_phase = Phase.PROBE_CONSTRUCTION


func _do_probe_construction() -> void:
	# Probe construction project definitions
	if _bridge.has_method("GetAvailableConstructionDefsV0"):
		var defs: Array = _bridge.call("GetAvailableConstructionDefsV0")
		var buildable := 0
		for d in defs:
			if d.get("prerequisites_met", false):
				buildable += 1
		_log("CONSTRUCTION|defs=%d buildable=%d" % [defs.size(), buildable])
		_log("GOAL|DEPTH|construction_defs=%d buildable=%d" % [defs.size(), buildable])
		_track_system_introduced("CONSTRUCTION")

	# Check active construction
	if _bridge.has_method("GetConstructionProjectsV0"):
		var projects: Array = _bridge.call("GetConstructionProjectsV0")
		_log("CONSTRUCTION|active=%d" % projects.size())

	_polls = 0
	_phase = Phase.PROBE_FRACTURE


func _do_probe_fracture() -> void:
	# Probe fracture access status
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))

	if _bridge.has_method("GetFractureAccessV0"):
		var access: Dictionary = _bridge.call("GetFractureAccessV0", "fleet_trader_1", node_id)
		var allowed: bool = access.get("allowed", false)
		var reason := str(access.get("reason", ""))
		_log("FRACTURE|access=%s reason=%s" % [str(allowed), reason])
		_log("GOAL|DEPTH|fracture_access=%s" % str(allowed))
		_track_system_introduced("FRACTURE")

	# Probe void sites
	if _bridge.has_method("GetAvailableVoidSitesV0"):
		var sites: Array = _bridge.call("GetAvailableVoidSitesV0")
		_log("FRACTURE|void_sites=%d" % sites.size())
		_log("GOAL|DEPTH|void_sites=%d" % sites.size())

	# Sensor level
	if _bridge.has_method("GetSensorLevelV0"):
		var level := int(_bridge.call("GetSensorLevelV0"))
		_log("FRACTURE|sensor_level=%d" % level)

	# Change 7: Discovery scan probe
	if _bridge.has_method("GetDiscoverySnapshotV0") and _bridge.has_method("DispatchScanDiscoveryV0"):
		for nid in _visited:
			var discoveries: Array = _bridge.call("GetDiscoverySnapshotV0", str(nid))
			if discoveries.size() > 0:
				var disc_id := str(discoveries[0].get("site_id", ""))
				var phase_before := str(discoveries[0].get("phase", ""))
				if not disc_id.is_empty():
					_bridge.call("DispatchScanDiscoveryV0", disc_id)
					_log("DISCOVERY|scan_dispatched=%s phase=%s" % [disc_id, phase_before])
					_discovery_scan_attempted = true
					break

	# Change 12: Fracture travel attempt
	if _bridge.has_method("GetFractureAccessV0") and _bridge.has_method("DispatchFractureTravelV0"):
		var f_access: Dictionary = _bridge.call("GetFractureAccessV0", "fleet_trader_1", node_id)
		var f_allowed: bool = f_access.get("allowed", false)
		if f_allowed and _bridge.has_method("GetAvailableVoidSitesV0"):
			var void_sites: Array = _bridge.call("GetAvailableVoidSitesV0")
			if void_sites.size() > 0:
				var site_id := str(void_sites[0].get("site_id", ""))
				if not site_id.is_empty():
					_bridge.call("DispatchFractureTravelV0", "fleet_trader_1", site_id)
					_log("FRACTURE|travel_dispatched=%s" % site_id)
					_fracture_travel_attempted = true

	_polls = 0
	_phase = Phase.PROBE_DIPLOMACY


func _do_probe_diplomacy() -> void:
	var treaties := 0
	var bounties := 0
	var proposals := 0
	var sanctions := 0
	if _bridge.has_method("GetActiveTreatiesV0"):
		var arr: Array = _bridge.call("GetActiveTreatiesV0")
		treaties = arr.size()
	if _bridge.has_method("GetAvailableBountiesV0"):
		var arr: Array = _bridge.call("GetAvailableBountiesV0")
		bounties = arr.size()
	if _bridge.has_method("GetDiplomaticProposalsV0"):
		var arr: Array = _bridge.call("GetDiplomaticProposalsV0")
		proposals = arr.size()
	if _bridge.has_method("GetSanctionsV0"):
		var arr: Array = _bridge.call("GetSanctionsV0")
		sanctions = arr.size()
	_log("DIPLOMACY|treaties=%d bounties=%d proposals=%d sanctions=%d" % [treaties, bounties, proposals, sanctions])
	_log("GOAL|DEPTH|diplomacy_total=%d" % (treaties + bounties + proposals + sanctions))
	_track_system_introduced("DIPLOMACY")
	_polls = 0
	_phase = Phase.PROBE_STORY


func _do_probe_story() -> void:
	if _bridge.has_method("GetStoryProgressV0"):
		var story: Dictionary = _bridge.call("GetStoryProgressV0")
		var act := str(story.get("act", ""))
		var phase := str(story.get("phase", ""))
		_log("STORY|act=%s phase=%s keys=%d" % [act, phase, story.size()])
	if _bridge.has_method("GetPentagonStateV0"):
		var pentagon: Dictionary = _bridge.call("GetPentagonStateV0")
		_log("STORY|pentagon_keys=%d" % pentagon.size())
		for k in pentagon:
			_log("STORY|pentagon_%s=%s" % [str(k), str(pentagon[k])])
	_track_system_introduced("STORY")
	_polls = 0
	_phase = Phase.PROBE_ENDGAME


func _do_probe_endgame() -> void:
	# Census of late-game bridge wiring — confirms methods exist and return data
	var methods_found := 0
	var total_methods := 11
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))

	if _bridge.has_method("GetEndgameProgressV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetEndgameProgressV0")
		_log("ENDGAME|progress_keys=%d" % d.size())
	if _bridge.has_method("GetEndgamePathsV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetEndgamePathsV0")
		_log("ENDGAME|paths_keys=%d" % d.size())
	if _bridge.has_method("GetGameResultV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetGameResultV0")
		_log("ENDGAME|result=%s" % str(d.get("result", "none")))
	if _bridge.has_method("GetLossInfoV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetLossInfoV0")
		_log("ENDGAME|loss_keys=%d" % d.size())
	if _bridge.has_method("GetMegaprojectsV0"):
		methods_found += 1
		var arr: Array = _bridge.call("GetMegaprojectsV0")
		_log("ENDGAME|megaprojects=%d" % arr.size())
	if _bridge.has_method("GetMegaprojectTypesV0"):
		methods_found += 1
		var arr: Array = _bridge.call("GetMegaprojectTypesV0")
		_log("ENDGAME|megaproject_types=%d" % arr.size())
	if _bridge.has_method("GetSupplyShockSummaryV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetSupplyShockSummaryV0")
		_log("ENDGAME|supply_shock_keys=%d" % d.size())
	if _bridge.has_method("GetFleetUpkeepV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetFleetUpkeepV0", "fleet_trader_1")
		_log("ENDGAME|upkeep=%s" % str(d))
	if _bridge.has_method("GetLatticeDroneAlertsV0"):
		methods_found += 1
		var arr: Array = _bridge.call("GetLatticeDroneAlertsV0", node_id)
		_log("ENDGAME|lattice_alerts=%d" % arr.size())
	if _bridge.has_method("GetHavenMarketV0"):
		methods_found += 1
		var arr: Array = _bridge.call("GetHavenMarketV0")
		_log("ENDGAME|haven_market=%d" % arr.size())
	if _bridge.has_method("GetHavenMarketInfoV0"):
		methods_found += 1
		var d: Dictionary = _bridge.call("GetHavenMarketInfoV0")
		_log("ENDGAME|haven_market_info_keys=%d" % d.size())

	_log("ENDGAME|methods_found=%d/%d" % [methods_found, total_methods])

	# Change 9: Haven probe
	if _bridge.has_method("GetHavenStatusV0"):
		var haven: Dictionary = _bridge.call("GetHavenStatusV0")
		_haven_tier = int(haven.get("tier", 0))
		_log("HAVEN|tier=%d name=%s node=%s" % [_haven_tier, str(haven.get("tier_name", "")), str(haven.get("node_id", ""))])
		if _haven_tier > 0 and _bridge.has_method("UpgradeHavenV0"):
			var upgraded: bool = _bridge.call("UpgradeHavenV0")
			_log("HAVEN|upgrade_attempted=%s" % str(upgraded))

	_polls = 0
	_phase = Phase.PROBE_ECONOMY_SIGNAL


func _do_probe_economy_signal() -> void:
	# Valve-inspired economy signal clarity probe
	# For each good, find best margin (max sell - min buy) across visited markets
	var good_best_buy: Dictionary = {}   # good_id -> min buy price
	var good_best_sell: Dictionary = {}  # good_id -> max sell price
	var good_worst_buy: Dictionary = {}  # good_id -> max buy price
	var good_worst_sell: Dictionary = {} # good_id -> min sell price

	for nid in _market_snapshots:
		var market: Array = _market_snapshots[nid]
		for item in market:
			var gid := str(item.get("good_id", ""))
			var buy_p := int(item.get("buy_price", 0))
			var sell_p := int(item.get("sell_price", 0))
			if gid.is_empty() or buy_p <= 0:
				continue
			if sell_p <= 0:
				sell_p = int(buy_p * 0.9)  # Estimate from bid-ask spread
			if not good_best_buy.has(gid) or buy_p < good_best_buy[gid]:
				good_best_buy[gid] = buy_p
			if not good_best_sell.has(gid) or sell_p > good_best_sell[gid]:
				good_best_sell[gid] = sell_p
			if not good_worst_buy.has(gid) or buy_p > good_worst_buy[gid]:
				good_worst_buy[gid] = buy_p
			if not good_worst_sell.has(gid) or sell_p < good_worst_sell[gid]:
				good_worst_sell[gid] = sell_p

	var profitable_count := 0
	var total_goods := good_best_buy.size()
	var best_margin := -999999
	var worst_margin := 999999
	var all_profitable := true

	for gid in good_best_buy:
		if good_best_sell.has(gid):
			var margin: int = int(good_best_sell[gid]) - int(good_best_buy[gid])
			if margin > best_margin:
				best_margin = margin
			if margin > 0:
				profitable_count += 1
		if good_worst_sell.has(gid) and good_worst_buy.has(gid):
			var w_margin: int = int(good_worst_sell[gid]) - int(good_worst_buy[gid])
			if w_margin < worst_margin:
				worst_margin = w_margin
			if w_margin <= 0:
				all_profitable = false

	_log("ECONOMY_SIGNAL|best_margin=%d worst_margin=%d profitable=%d/%d snapshots=%d" % [
		best_margin, worst_margin, profitable_count, total_goods, _market_snapshots.size()])

	if total_goods > 0 and profitable_count == 0:
		_flag("ECONOMY_NO_PROFITABLE_ROUTE")
	if total_goods > 2 and all_profitable and _market_snapshots.size() >= 3:
		_flag("ECONOMY_ALL_ROUTES_PROFIT")

	# Node economy snapshot (traffic, prosperity, industry type, warfront tier)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var cur_node := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetNodeEconomySnapshotV0") and not cur_node.is_empty():
		var econ: Dictionary = _bridge.call("GetNodeEconomySnapshotV0", cur_node)
		_log("ECONOMY_SIGNAL|node_econ traffic=%s prosperity=%s industry=%s warfront=%s" % [
			str(econ.get("traffic_level", -1)), str(econ.get("prosperity", -1)),
			str(econ.get("industry_type", "none")), str(econ.get("warfront_tier", -1))])

	# Market alerts (stockouts, price spikes/drops across visited nodes)
	if _bridge.has_method("GetMarketAlertsV0"):
		var alerts: Array = _bridge.call("GetMarketAlertsV0", 5)
		_log("ECONOMY_SIGNAL|market_alerts=%d" % alerts.size())
		for alert in alerts:
			_log("ECONOMY_SIGNAL|alert type=%s good=%s change=%s%%" % [
				str(alert.get("type", "?")), str(alert.get("good_id", "?")),
				str(alert.get("change_pct", 0))])

	_polls = 0
	_phase = Phase.PROBE_OVERLAYS


func _do_probe_overlays() -> void:
	# Change 8: Overlay data probes
	if _bridge.has_method("GetFactionTerritoryOverlayV0"):
		var terr: Dictionary = _bridge.call("GetFactionTerritoryOverlayV0")
		_overlay_territory_nodes = terr.size()
		_log("OVERLAY|territory_nodes=%d" % terr.size())
	if _bridge.has_method("GetFleetPositionsOverlayV0"):
		var fleets: Dictionary = _bridge.call("GetFleetPositionsOverlayV0")
		_log("OVERLAY|fleet_nodes=%d" % fleets.size())
	if _bridge.has_method("GetWarfrontOverlayV0"):
		var wf: Dictionary = _bridge.call("GetWarfrontOverlayV0")
		_log("OVERLAY|warfront_nodes=%d" % wf.size())
	if _bridge.has_method("GetHeatOverlayV0"):
		var heat: Dictionary = _bridge.call("GetHeatOverlayV0")
		_log("OVERLAY|heat_nodes=%d" % heat.size())
	if _bridge.has_method("GetPressureDomainsV0"):
		var domains: Array = _bridge.call("GetPressureDomainsV0")
		_log("OVERLAY|pressure_domains=%d" % domains.size())
		for d in domains:
			_log("OVERLAY|domain=%s tier=%s crisis=%s" % [
				str(d.get("domain_id", "")), str(d.get("tier_name", "")), str(d.get("is_crisis", false))])
	if _bridge.has_method("GetEconomyOverviewV0"):
		var econ: Array = _bridge.call("GetEconomyOverviewV0")
		_economy_overview_goods = econ.size()
		_log("OVERLAY|economy_goods=%d" % econ.size())
	if _bridge.has_method("GetNpcTradeActivityV0"):
		var activity: int = _bridge.call("GetNpcTradeActivityV0", _home_node_id)
		_log("OVERLAY|npc_trade_activity=%d node=%s" % [activity, _home_node_id])
	_polls = 0
	_phase = Phase.PROBE_PLANET_SCAN


func _do_probe_planet_scan() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))

	# Scanner charges status
	if _bridge.has_method("GetScanChargesV0"):
		var charges: Dictionary = _bridge.call("GetScanChargesV0")
		if charges is Dictionary and not charges.is_empty():
			var remaining := int(charges.get("remaining", 0))
			var max_charges := int(charges.get("max", 0))
			var tier := int(charges.get("tier", 0))
			var mineral_avail: bool = charges.get("mineral_available", false)
			var signal_avail: bool = charges.get("signal_available", false)
			var arch_avail: bool = charges.get("archaeological_available", false)
			_log("PLANET_SCAN|charges=%d/%d tier=%d mineral=%s signal=%s arch=%s" % [
				remaining, max_charges, tier, str(mineral_avail), str(signal_avail), str(arch_avail)])
			_log("GOAL|DEPTH|scanner_tier=%d scanner_charges=%d" % [tier, remaining])

	# Planet info at current node
	if _bridge.has_method("GetPlanetInfoV0") and not node_id.is_empty():
		var planet: Dictionary = _bridge.call("GetPlanetInfoV0", node_id)
		if planet is Dictionary and not planet.is_empty():
			var planet_type := str(planet.get("planet_type", ""))
			var landable: bool = planet.get("landable", false)
			var specialization := str(planet.get("specialization", ""))
			_log("PLANET_SCAN|planet_type=%s landable=%s spec=%s node=%s" % [
				planet_type, str(landable), specialization, node_id])

	# Star info at current node
	if _bridge.has_method("GetStarInfoV0") and not node_id.is_empty():
		var star: Dictionary = _bridge.call("GetStarInfoV0", node_id)
		if star is Dictionary and not star.is_empty():
			var star_class := str(star.get("star_class", ""))
			_log("PLANET_SCAN|star_class=%s node=%s" % [star_class, node_id])

	# Attempt orbital scan (MineralSurvey — always available)
	if _bridge.has_method("OrbitalScanV0") and not node_id.is_empty():
		var scan_result: Dictionary = _bridge.call("OrbitalScanV0", node_id, "MineralSurvey")
		if scan_result is Dictionary:
			var error := str(scan_result.get("error", ""))
			if error.is_empty():
				var category := str(scan_result.get("category", ""))
				var flavor := str(scan_result.get("flavor_text", ""))
				var scan_id := str(scan_result.get("scan_id", ""))
				_log("PLANET_SCAN|orbital_scan=OK category=%s scan_id=%s" % [category, scan_id])
				_log("GOAL|DEPTH|planet_scan_success=true")
				# Try to investigate if available
				if bool(scan_result.get("investigation_available", false)) and not scan_id.is_empty():
					if _bridge.has_method("InvestigateFindingV0"):
						var inv: Dictionary = _bridge.call("InvestigateFindingV0", scan_id)
						var inv_ok: bool = inv.get("success", false)
						_log("PLANET_SCAN|investigate=%s scan_id=%s" % [str(inv_ok), scan_id])
			else:
				_log("PLANET_SCAN|orbital_scan=FAIL error=%s" % error)

	# Check existing scan results across visited nodes
	if _bridge.has_method("GetPlanetScanResultsV0"):
		var total_scans := 0
		for nid in _visited:
			var results: Array = _bridge.call("GetPlanetScanResultsV0", str(nid))
			if results is Array:
				total_scans += results.size()
		_log("PLANET_SCAN|total_results_across_visited=%d" % total_scans)

	# Instability-revealed discovery sites
	if _bridge.has_method("GetInstabilityRevealedSitesV0"):
		var revealed: Array = _bridge.call("GetInstabilityRevealedSitesV0")
		if revealed is Array:
			var total := revealed.size()
			var visible := 0
			for site in revealed:
				if bool(site.get("is_revealed", false)):
					visible += 1
			_log("PLANET_SCAN|instability_sites=%d revealed=%d" % [total, visible])
			_log("GOAL|DEPTH|instability_revealed=%d" % visible)

	_polls = 0
	_phase = Phase.PROBE_ANOMALY_CHAINS


func _do_probe_anomaly_chains() -> void:
	# Active anomaly chains
	if _bridge.has_method("GetActiveChainsV0"):
		var chains: Array = _bridge.call("GetActiveChainsV0")
		if chains is Array:
			_log("ANOMALY|active_chains=%d" % chains.size())
			_log("GOAL|DEPTH|anomaly_chains=%d" % chains.size())
			for chain in chains:
				var chain_id := str(chain.get("chain_id", ""))
				var status := str(chain.get("status", ""))
				var step := int(chain.get("current_step", 0))
				var total := int(chain.get("total_steps", 0))
				var kind := str(chain.get("current_step_kind", ""))
				_log("ANOMALY|chain=%s status=%s step=%d/%d kind=%s" % [
					chain_id, status, step, total, kind])
				# Get full chain progress
				if _bridge.has_method("GetChainProgressV0") and not chain_id.is_empty():
					var progress: Dictionary = _bridge.call("GetChainProgressV0", chain_id)
					if progress is Dictionary and bool(progress.get("found", false)):
						var steps: Array = progress.get("steps", [])
						var completed := 0
						for s in steps:
							if bool(s.get("is_completed", false)):
								completed += 1
						_log("ANOMALY|chain=%s completed_steps=%d/%d" % [chain_id, completed, total])

	# Discovery trade intel
	if _bridge.has_method("GetDiscoveryTradeIntelV0"):
		var routes: Array = _bridge.call("GetDiscoveryTradeIntelV0")
		if routes is Array:
			_log("ANOMALY|discovery_trade_routes=%d" % routes.size())
			_log("GOAL|DEPTH|discovery_intel_routes=%d" % routes.size())
			for route in routes:
				var good := str(route.get("good_id", ""))
				var profit := int(route.get("estimated_profit", 0))
				var source := str(route.get("source_discovery_id", ""))
				_log("ANOMALY|route_good=%s profit=%d source=%s" % [good, profit, source])

	# Survey program status
	if _bridge.has_method("GetSurveyProgramStatusV0"):
		var survey: Dictionary = _bridge.call("GetSurveyProgramStatusV0")
		if survey is Dictionary:
			var programs: Array = survey.get("programs", [])
			_log("ANOMALY|survey_programs=%d" % programs.size())
			for prog in programs:
				_log("ANOMALY|survey=%s family=%s status=%s" % [
					str(prog.get("id", "")), str(prog.get("family", "")), str(prog.get("status", ""))])

	# Survey unlock check
	if _bridge.has_method("IsSurveyUnlockedV0"):
		var unlocked: bool = _bridge.call("IsSurveyUnlockedV0", "SIGNAL")
		_log("ANOMALY|survey_unlocked_signal=%s" % str(unlocked))

	_polls = 0
	_phase = Phase.AUDIT


func _do_audit() -> void:
	_act_start_frames["ACT_7"] = _total_frames
	# Goal 3 probe: total FO dialogue count
	_log("GOAL|FO|total_lines=%d" % _fo_dialogue_count)

	_log("AUDIT|visited=%d trades=%d combats=%d flags=%d" % [
		_visited.size(), _trades_completed, _combats_completed, _flags.size()])

	_assert(_visited.size() >= 6, "deep_explore_6_nodes", "visited=%d" % _visited.size())
	_assert(_trades_completed >= 1, "trades_completed", "count=%d" % _trades_completed)
	_assert(_combats_completed >= 1, "combat_completed", "count=%d" % _combats_completed)

	# === Experience Dimension Assertions (GEQ/PENS-inspired) ===

	# FLOW: credit growth rate should be positive but not trivially huge
	var credit_growth := 0.0
	if _credits_at_start > 0:
		credit_growth = float(_credits_after_sell - _credits_at_start) / float(_credits_at_start)
	_log("EXPERIENCE|flow_credit_growth=%.2f" % credit_growth)
	_log("GOAL|FLOW|credit_growth=%.2f min_hull=%d factions=%d goods=%d" % [
		credit_growth, _min_hull_seen, _factions_visited.size(), _goods_traded.size()])
	# Warn if credit growth is zero or negative (player isn't progressing)
	if credit_growth <= 0.0:
		_flag("FLOW_NO_PROFIT|growth=%.2f" % credit_growth)
	# Warn if credit growth is absurdly high (trivially broken economy)
	if credit_growth > 10.0:
		_flag("FLOW_TRIVIALLY_RICH|growth=%.2f" % credit_growth)

	# TENSION: player should have experienced some challenge
	_log("EXPERIENCE|tension_min_hull=%d" % _min_hull_seen)
	if _min_hull_seen >= 100:
		_flag("TENSION_NO_DAMAGE_TAKEN")

	# IMMERSION: faction territory diversity
	_log("EXPERIENCE|factions_visited=%d" % _factions_visited.size())

	# COMPETENCE: goods diversity traded
	_log("EXPERIENCE|goods_traded=%d" % _goods_traded.size())

	# COVERAGE: systems introduced during first hour
	_log("EXPERIENCE|systems_introduced=%s" % str(_systems_introduced))

	# === Pacing Time-Series Analysis (Valve AI Director inspired) ===
	var credit_direction_changes := 0
	if _pacing_credits.size() >= 3:
		for i in range(1, _pacing_credits.size() - 1):
			var prev_dir := _pacing_credits[i] - _pacing_credits[i - 1]
			var next_dir := _pacing_credits[i + 1] - _pacing_credits[i]
			if (prev_dir > 0 and next_dir < 0) or (prev_dir < 0 and next_dir > 0):
				credit_direction_changes += 1
		_credit_direction_changes = credit_direction_changes
		if credit_direction_changes < 2:
			_flag("PACING_MONOTONE_CREDITS|changes=%d samples=%d" % [credit_direction_changes, _pacing_credits.size()])

	# Hull tension-relief pattern
	var hull_dropped := false
	var hull_recovered := false
	for hp in _pacing_hull_pct:
		if hp < 80:
			hull_dropped = true
		if hull_dropped and hp > 90:
			hull_recovered = true
	if hull_dropped and hull_recovered:
		_log("PACING|TENSION_RELIEF|hull dropped and recovered")

	# FO silence budget
	if _fo_longest_silence > 600:
		_flag("FO_LONG_SILENCE|frames=%d" % _fo_longest_silence)

	# Act timing balance
	var act_keys := _act_start_frames.keys()
	act_keys.sort()
	if act_keys.size() >= 2:
		var act_durations: Array[String] = []
		for i in range(act_keys.size()):
			var start_f: int = _act_start_frames[act_keys[i]]
			var end_f: int = _total_frames if i == act_keys.size() - 1 else _act_start_frames[act_keys[i + 1]]
			var dur := end_f - start_f
			act_durations.append("%s=%d" % [str(act_keys[i]), dur])
			if _total_frames > 0 and float(dur) / float(_total_frames) > 0.4:
				_flag("PACING_ACT_IMBALANCE|%s took %d%%" % [str(act_keys[i]), (dur * 100) / _total_frames])
		_log("PACING|act_frames=%s" % " ".join(act_durations))

	_log("PACING|samples=%d credit_changes=%d hull_min=%d fo_silence=%d" % [
		_pacing_credits.size(), credit_direction_changes, _min_hull_seen, _fo_longest_silence])

	# Stranded player guard: verify player has viable next action
	var final_ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var final_credits := int(final_ps.get("credits", 0))
	var has_viable_action := final_credits > 50
	if not has_viable_action:
		if _bridge.has_method("GetActiveMissionV0"):
			var active: Dictionary = _bridge.call("GetActiveMissionV0")
			if not active.is_empty() and str(active.get("id", "")) != "":
				has_viable_action = true
	if not has_viable_action:
		_flag("STRANDED_PLAYER|credits=%d" % final_credits)

	# === FPS Performance Report ===
	if _fps_samples.size() > 0:
		var fps_sum := 0.0
		for s in _fps_samples:
			fps_sum += s
		var fps_avg := fps_sum / float(_fps_samples.size())
		_log("PERF|fps_min=%.1f fps_max=%.1f fps_avg=%.1f samples=%d" % [_fps_min, _fps_max, fps_avg, _fps_samples.size()])
		if _fps_min < 30.0:
			_flag("FPS_BELOW_30|min=%.1f" % _fps_min)
		if fps_avg < 45.0:
			_flag("FPS_AVG_LOW|avg=%.1f" % fps_avg)
	else:
		_log("PERF|no_fps_data")

	# === Dispatch Failure Summary ===
	if _dispatch_failures > 0:
		_log("DISPATCH|silent_failures=%d" % _dispatch_failures)
		_flag("DISPATCH_FAILURES_TOTAL|count=%d" % _dispatch_failures)

	# Change 10: UI panel visibility checks
	var panel_names := ["FOPanel", "CombatHud", "KnowledgeWebPanel", "WarfrontPanel", "MegaprojectPanel"]
	var panels_found := 0
	for pname in panel_names:
		var panel_node = root.find_child(pname, true, false)
		if panel_node != null:
			panels_found += 1
			_log("UI|%s found=true visible=%s" % [pname, str(panel_node.visible)])
		else:
			_log("UI|%s found=false" % pname)
	_ui_panels_found = panels_found
	_log("UI|panels=%d/%d" % [panels_found, panel_names.size()])

	# === Content String Lint — scan visible Labels for dev jargon ===
	# Check Label3D nodes
	for label in _find_all_label3d():
		if not label.visible:
			continue
		var txt: String = label.text
		if txt.length() > 40:
			_flag("LABEL_TOO_LONG|%s" % txt.left(50))
		_lint_string(txt, "Label3D")

	# Check HUD Label nodes for dev jargon / raw IDs
	var hud_node = root.find_child("HUD", true, false)
	if hud_node != null:
		var hud_labels := _find_all_labels(hud_node)
		for lbl in hud_labels:
			if not lbl.visible or lbl.text.is_empty():
				continue
			_lint_string(lbl.text, "HUD:" + str(lbl.name))

	# === Save/Load Cycle Test ===
	if _bridge != null and _bridge.has_method("SaveGameV0") and _bridge.has_method("LoadGameV0"):
		var pre_save_ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var pre_credits := int(pre_save_ps.get("credits", 0))
		var pre_node := str(pre_save_ps.get("current_node_id", ""))
		var pre_cargo := int(pre_save_ps.get("cargo_count", 0))
		_bridge.call("SaveGameV0")
		_bridge.call("LoadGameV0")
		_busy = true
		await create_timer(0.3).timeout
		var post_load_ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var post_credits := int(post_load_ps.get("credits", 0))
		var post_node := str(post_load_ps.get("current_node_id", ""))
		var post_cargo := int(post_load_ps.get("cargo_count", 0))
		var save_ok := pre_credits == post_credits and pre_node == post_node and pre_cargo == post_cargo
		_assert(save_ok, "save_load_roundtrip",
			"credits=%d/%d node=%s/%s cargo=%d/%d" % [pre_credits, post_credits, pre_node, post_node, pre_cargo, post_cargo])
		_log("SAVE_LOAD|roundtrip=%s" % str(save_ok))
		_busy = false
	else:
		_log("SAVE_LOAD|not_available")

	# Run aesthetic audit
	var audit_critical := 0
	if _observer != null and _audit != null:
		var report = _observer.capture_full_report_v0()
		# Inject tracked FPS data and jargon count into performance section
		if report.has("performance"):
			report["performance"]["fps_min"] = _fps_min if _fps_samples.size() > 0 else 60.0
			# Count jargon flags
			var jargon_count := 0
			for f in _flags:
				if f.begins_with("DEV_JARGON"):
					jargon_count += 1
			report["performance"]["jargon_flags"] = jargon_count
		var audit_results = _audit.run_audit_v0(report)
		audit_critical = _audit.count_critical_failures_v0(audit_results)
		for ar in audit_results:
			var status = "PASS" if ar.get("passed", false) else "FAIL"
			_log("AESTHETIC|%s|%s|%s" % [status, str(ar.get("flag", "")), str(ar.get("detail", ""))])

	_capture("31_final")

	# Print all flags
	for f in _flags:
		_log("FLAG|%s" % f)

	# Coverage report
	var total_nodes := _all_nodes.size()
	var coverage_pct := 0
	if total_nodes > 0:
		coverage_pct = (_visited.size() * 100) / total_nodes
	_log("COVERAGE|nodes=%d/%d(%d%%) trades=%d goods=%d factions=%d systems_intro=%d" % [
		_visited.size(), total_nodes, coverage_pct,
		_trades_completed, _goods_traded.size(), _factions_visited.size(),
		_systems_introduced.size()])

	_log("SUMMARY|visited=%d trades=%d combats=%d flags=%d audit_critical=%d hard_fail=%s" % [
		_visited.size(), _trades_completed, _combats_completed,
		_flags.size(), audit_critical, str(_hard_fail)])

	if _hard_fail:
		_log("FAIL|%s" % _fail_reason)
	elif audit_critical > 0:
		_log("FAIL|audit_critical=%d" % audit_critical)
	else:
		_log("PASS|screenshots=%d" % _snapshots.size())

	# Self-evaluation report card (rubric-scored)
	_score_and_report()

	_phase = Phase.DONE


# ===================== Report Card (Rubric Self-Evaluation) =====================

func _score_and_report() -> void:
	# --- Goal Scores (1-5 scale from first_hour_rubric.md) ---
	var price_profiles := _market_snapshots.size()

	# Goal 1: Alive — world feels inhabited and dynamic
	var g1 := 1
	if _npc_count_at_boot >= 3 and price_profiles >= 4 and _factions_visited.size() >= 2:
		g1 = 5
	elif _npc_count_at_boot >= 2 and price_profiles >= 3:
		g1 = 4
	elif _npc_count_at_boot >= 1 and price_profiles >= 2:
		g1 = 3
	elif _npc_count_at_boot >= 1:
		g1 = 2

	# Goal 2: Teaches — learn by doing, no tutorial text
	var g2 := 1
	if not _tutorial_text_found and _systems_introduced.size() >= 5:
		g2 = 5
	elif not _tutorial_text_found and _systems_introduced.size() >= 4:
		g2 = 4
	elif _systems_introduced.size() >= 3:
		g2 = 3
	elif _systems_introduced.size() >= 1 or _tutorial_text_found:
		g2 = 2

	# Goal 3: First Officer feels like a person
	var g3 := 1
	if _fo_promoted and _fo_dialogue_count >= 3 and _fo_post_event_reactions >= 2:
		g3 = 5
	elif _fo_promoted and _fo_dialogue_count >= 2:
		g3 = 4
	elif _fo_promoted and _fo_dialogue_count >= 1:
		g3 = 3
	elif _fo_promoted:
		g3 = 2

	# Goal 4: Profit discovery — player finds profitable trade
	var credit_growth := 0.0
	if _credits_at_start > 0:
		credit_growth = float(_credits_after_sell - _credits_at_start) / float(_credits_at_start) * 100.0
	var g4 := 1
	if _profit_margin > 100 and credit_growth > 50.0 and _fo_reacted_to_profit:
		g4 = 5
	elif credit_growth > 30.0:
		g4 = 4
	elif credit_growth > 10.0:
		g4 = 3
	elif credit_growth > 0.0:
		g4 = 2

	# Goal 5: Promise of depth — tantalizing unseen content
	var explored_pct := 0.0
	if _all_nodes.size() > 0:
		explored_pct = float(_visited.size()) / float(_all_nodes.size()) * 100.0
	var g5 := 1
	if explored_pct < 50.0 and _tech_count >= 8 and _empty_slots_at_fit >= 2:
		g5 = 5
	elif explored_pct < 50.0 and _tech_count >= 4:
		g5 = 4
	elif _tech_count >= 1:
		g5 = 3
	elif explored_pct >= 50.0:
		g5 = 2

	# --- Supplemental Scores (1-5) ---

	# Combat Feel (enhanced with heat data — Change 15)
	var s_combat := 1
	if _min_hull_seen < 50 and _combats_completed >= 2 and _heat_capacity > 0:
		s_combat = 5
	elif _min_hull_seen < 70 and _combats_completed >= 1 and _heat_capacity > 0:
		s_combat = 4
	elif _min_hull_seen < 90 and _combats_completed >= 1:
		s_combat = 3
	elif _combats_completed >= 1:
		s_combat = 2

	# Systemic Economy — ledger entries are reliable; systemic offers are seed-dependent
	var s_economy := 1
	if _ledger_entries >= 6 and _systemic_offers >= 1:
		s_economy = 5
	elif _ledger_entries >= 6:
		s_economy = 4
	elif _ledger_entries >= 3:
		s_economy = 3
	elif _ledger_entries >= 1:
		s_economy = 2

	# Faction Presence
	var s_faction := 1
	if _factions_visited.size() >= 3 and _warfront_count >= 2:
		s_faction = 5
	elif _factions_visited.size() >= 2 and _warfront_count >= 1:
		s_faction = 4
	elif _factions_visited.size() >= 1 and _warfront_count >= 1:
		s_faction = 3
	elif _factions_visited.size() >= 1:
		s_faction = 2

	# Mission Quality
	var s_mission := 1
	if _mission_accepted and _missions_available_count >= 3:
		s_mission = 5
	elif _mission_accepted and _missions_available_count >= 2:
		s_mission = 4
	elif _missions_available_count >= 1:
		s_mission = 3
	elif _missions_available_count > 0:
		s_mission = 2

	# Player Progression (18 milestones exist; first-hour bot achieves ~3-4)
	var s_progress := 1
	if _milestone_count >= 4 and explored_pct >= 30.0:
		s_progress = 5
	elif _milestone_count >= 3 and explored_pct >= 20.0:
		s_progress = 4
	elif _milestone_count >= 2:
		s_progress = 3
	elif _milestone_count >= 1:
		s_progress = 2

	# Heat System (Change 15)
	var s_heat := 1
	if _heat_capacity > 0 and _radiator_intact:
		s_heat = 4
	elif _heat_capacity > 0:
		s_heat = 3
	elif _combats_completed >= 1:
		s_heat = 2

	# Boot Experience (Change 15)
	var s_boot := 1
	if _boot_tutorial_suppressed and _boot_intro_dismissed:
		s_boot = 5
	elif _boot_intro_dismissed:
		s_boot = 4
	elif _boot_tutorial_suppressed:
		s_boot = 3

	# Progressive Disclosure (Change 15)
	var s_disclosure := 1
	var d1_tabs := int(_dock1_onboarding.get("show_jobs_tab", false)) + int(_dock1_onboarding.get("show_ship_tab", false)) + int(_dock1_onboarding.get("show_intel_tab", false))
	var d3_tabs := int(_dock3_onboarding.get("show_jobs_tab", false)) + int(_dock3_onboarding.get("show_ship_tab", false)) + int(_dock3_onboarding.get("show_intel_tab", false))
	if d3_tabs > d1_tabs:
		s_disclosure = 5
	elif d3_tabs == d1_tabs and d3_tabs > 0:
		s_disclosure = 3

	# Overlay Health (Change 15) — territory is always populated; economy may be empty on some seeds
	var s_overlay := 1
	if _overlay_territory_nodes >= 10 and _economy_overview_goods > 0:
		s_overlay = 5
	elif _overlay_territory_nodes >= 10:
		s_overlay = 4
	elif _overlay_territory_nodes > 0:
		s_overlay = 3
	elif _economy_overview_goods > 0:
		s_overlay = 2

	# --- Pacing Ratings ---
	var pacing_credit := "PASS"
	if _credit_direction_changes < 2:
		pacing_credit = "WARN"

	var pacing_tension := "PASS"
	if _min_hull_seen >= 100:
		pacing_tension = "WARN"

	var pacing_fo := "PASS"
	if _fo_longest_silence > 600:
		pacing_fo = "WARN"

	var pacing_act := "PASS"
	for f in _flags:
		if f.begins_with("PACING_ACT_IMBALANCE"):
			pacing_act = "WARN"
			break

	# --- Economy Signal ---
	var econ_profitable_routes := 0
	var econ_total_goods := 0
	var good_best_buy: Dictionary = {}
	var good_best_sell: Dictionary = {}
	for node_id in _market_snapshots:
		var market: Array = _market_snapshots[node_id]
		for item in market:
			var gid := str(item.get("good_id", ""))
			var buy_p := int(item.get("buy_price", 0))
			var sell_p := int(item.get("sell_price", 0))
			if gid.is_empty():
				continue
			if not good_best_buy.has(gid) or buy_p < int(good_best_buy[gid]):
				good_best_buy[gid] = buy_p
			if not good_best_sell.has(gid) or sell_p > int(good_best_sell[gid]):
				good_best_sell[gid] = sell_p
	for gid in good_best_buy:
		if good_best_sell.has(gid):
			econ_total_goods += 1
			var margin: int = int(good_best_sell[gid]) - int(good_best_buy[gid])
			if margin > 0:
				econ_profitable_routes += 1

	var econ_route := "PASS"
	if econ_total_goods > 0 and econ_profitable_routes == 0:
		econ_route = "FAIL"
	elif econ_total_goods > 0 and float(econ_profitable_routes) / float(econ_total_goods) < 0.3:
		econ_route = "WARN"

	var econ_diversity := "PASS"
	if price_profiles < 3:
		econ_diversity = "WARN"

	# --- Prescriptions (severity-ranked) ---
	var prescriptions: Array[Dictionary] = []

	if s_combat <= 2:
		prescriptions.append({
			"severity": "CRITICAL", "confidence": "high",
			"issue": "Combat has zero tension - player takes no damage",
			"evidence": "min_hull=%d combats=%d" % [_min_hull_seen, _combats_completed],
			"fix": "Tune NPC aggression so player takes 10-30% hull damage in first combat",
			"metric": "min_hull < 90 in next run"
		})

	if pacing_credit == "WARN":
		prescriptions.append({
			"severity": "CRITICAL" if _credit_direction_changes == 0 else "MAJOR",
			"confidence": "high",
			"issue": "Credit flow is monotonically increasing - no tension dips",
			"evidence": "credit_direction_changes=%d samples=%d" % [_credit_direction_changes, _pacing_credits.size()],
			"fix": "Add a cost event (repair, fuel, toll) between trades",
			"metric": "credit_direction_changes >= 3"
		})

	if g3 < 4:
		prescriptions.append({
			"severity": "MAJOR", "confidence": "medium",
			"issue": "First Officer feels flat - insufficient dialogue or reactions",
			"evidence": "promoted=%s lines=%d reactions=%d" % [str(_fo_promoted), _fo_dialogue_count, _fo_post_event_reactions],
			"fix": "Add FO reactions to combat/trade/arrival events",
			"metric": "fo_dialogue >= 3 and fo_reactions >= 2"
		})

	if s_faction <= 2:
		prescriptions.append({
			"severity": "MINOR", "confidence": "medium",
			"issue": "Faction presence weak - few territories visited",
			"evidence": "factions_visited=%d warfronts=%d" % [_factions_visited.size(), _warfront_count],
			"fix": "Seed starting system closer to faction borders",
			"metric": "factions_visited >= 2"
		})

	if g1 < 4:
		prescriptions.append({
			"severity": "MAJOR", "confidence": "high",
			"issue": "World doesn't feel alive - insufficient NPCs or market diversity",
			"evidence": "npcs=%d price_profiles=%d factions=%d" % [_npc_count_at_boot, price_profiles, _factions_visited.size()],
			"fix": "Increase NPC count at boot and market price variance",
			"metric": "npcs >= 3 and price_profiles >= 4"
		})

	if g2 < 4:
		prescriptions.append({
			"severity": "MAJOR", "confidence": "medium",
			"issue": "Teaching through play incomplete - too few systems introduced",
			"evidence": "systems_introduced=%d tutorial_text=%s" % [_systems_introduced.size(), str(_tutorial_text_found)],
			"fix": "Ensure dock, trade, travel, combat, fitting all happen naturally",
			"metric": "systems_introduced >= 5"
		})

	if g5 < 4:
		prescriptions.append({
			"severity": "MINOR", "confidence": "medium",
			"issue": "Depth promise weak - player sees too much or too little tech",
			"evidence": "explored=%.0f%% techs=%d empty_slots=%d" % [explored_pct, _tech_count, _empty_slots_at_fit],
			"fix": "Ensure tech tree has 4+ visible items and 2+ open slots",
			"metric": "techs >= 4 and empty_slots >= 2"
		})

	if econ_route == "FAIL":
		prescriptions.append({
			"severity": "CRITICAL", "confidence": "high",
			"issue": "No profitable trade routes exist",
			"evidence": "profitable=%d/%d goods" % [econ_profitable_routes, econ_total_goods],
			"fix": "Increase inter-station price variance in MarketInitGen",
			"metric": "profitable_routes > 0"
		})

	if s_heat <= 2:
		prescriptions.append({
			"severity": "MINOR", "confidence": "medium",
			"issue": "Heat system invisible - capacity=0 or radiator missing",
			"evidence": "heat_capacity=%d radiator=%s" % [_heat_capacity, str(_radiator_intact)],
			"fix": "Ensure default ship has heat profile and radiator module",
			"metric": "heat_capacity > 0"
		})

	if s_disclosure <= 1 and _dock1_onboarding.size() > 0:
		prescriptions.append({
			"severity": "MINOR", "confidence": "medium",
			"issue": "Progressive disclosure not working - tabs unchanged between docks",
			"evidence": "dock1_tabs=%d dock3_tabs=%d" % [d1_tabs, d3_tabs],
			"fix": "Verify onboarding state gates (trade/combat/visit) unlock tabs",
			"metric": "dock3_tabs > dock1_tabs"
		})

	if s_overlay <= 2:
		prescriptions.append({
			"severity": "MINOR", "confidence": "low",
			"issue": "Overlay data sparse - territory or economy overview empty",
			"evidence": "territory=%d economy=%d" % [_overlay_territory_nodes, _economy_overview_goods],
			"fix": "Ensure faction territory and economy overview are populated",
			"metric": "territory_nodes > 0 and economy_goods > 0"
		})

	# Sort prescriptions: CRITICAL > MAJOR > MINOR
	var severity_order := {"CRITICAL": 0, "MAJOR": 1, "MINOR": 2}
	prescriptions.sort_custom(func(a, b): return severity_order.get(a["severity"], 9) < severity_order.get(b["severity"], 9))

	# --- Output Report Card ---
	var goal_avg := float(g1 + g2 + g3 + g4 + g5) / 5.0

	_log("REPORT|=============== FIRST-HOUR REPORT CARD ===============")
	_log("REPORT|")
	_log("REPORT|GOAL SCORES:")
	_log("REPORT|  Goal 1 (Alive):           %d/5 - NPCs=%d prices=%d factions=%d" % [g1, _npc_count_at_boot, price_profiles, _factions_visited.size()])
	_log("REPORT|  Goal 2 (Teaches):         %d/5 - %ssystems=%d introduced" % [g2, "tutorial_text " if _tutorial_text_found else "", _systems_introduced.size()])
	_log("REPORT|  Goal 3 (FO):              %d/5 - promoted=%s lines=%d reactions=%d" % [g3, str(_fo_promoted), _fo_dialogue_count, _fo_post_event_reactions])
	_log("REPORT|  Goal 4 (Profit):          %d/5 - margin=%d growth=%.0f%% fo_reacted=%s" % [g4, _profit_margin, credit_growth, str(_fo_reacted_to_profit)])
	_log("REPORT|  Goal 5 (Depth):           %d/5 - explored=%.0f%% techs=%d slots=%d" % [g5, explored_pct, _tech_count, _empty_slots_at_fit])
	_log("REPORT|")
	_log("REPORT|SUPPLEMENTAL:")
	_log("REPORT|  Combat Feel:              %d/5 - min_hull=%d combats=%d heat=%d" % [s_combat, _min_hull_seen, _combats_completed, _heat_capacity])
	_log("REPORT|  Systemic Economy:         %d/5 - offers=%d ledger=%d" % [s_economy, _systemic_offers, _ledger_entries])
	_log("REPORT|  Faction Presence:         %d/5 - visited=%d warfronts=%d" % [s_faction, _factions_visited.size(), _warfront_count])
	_log("REPORT|  Mission Quality:          %d/5 - available=%d accepted=%s" % [s_mission, _missions_available_count, str(_mission_accepted)])
	_log("REPORT|  Player Progression:       %d/5 - milestones=%d explored=%.0f%%" % [s_progress, _milestone_count, explored_pct])
	_log("REPORT|  Heat System:              %d/5 - capacity=%d radiator=%s" % [s_heat, _heat_capacity, str(_radiator_intact)])
	_log("REPORT|  Boot Experience:          %d/5 - tutorial_off=%s intro_off=%s" % [s_boot, str(_boot_tutorial_suppressed), str(_boot_intro_dismissed)])
	_log("REPORT|  Progressive Disclosure:   %d/5 - dock1_tabs=%d dock3_tabs=%d" % [s_disclosure, d1_tabs, d3_tabs])
	_log("REPORT|  Overlay Health:           %d/5 - territory=%d economy=%d" % [s_overlay, _overlay_territory_nodes, _economy_overview_goods])
	_log("REPORT|")
	_log("REPORT|PACING:")
	_log("REPORT|  Credit Flow:              %s - %d direction changes in %d samples" % [pacing_credit, _credit_direction_changes, _pacing_credits.size()])
	_log("REPORT|  Tension Arc:              %s - hull min=%d%%" % [pacing_tension, _min_hull_seen])
	_log("REPORT|  FO Engagement:            %s - silence gap %d frames" % [pacing_fo, _fo_longest_silence])
	_log("REPORT|  Act Balance:              %s" % pacing_act)
	_log("REPORT|")
	_log("REPORT|ECONOMY SIGNAL:")
	_log("REPORT|  Route Profitability:      %s - %d/%d goods profitable" % [econ_route, econ_profitable_routes, econ_total_goods])
	_log("REPORT|  Market Diversity:          %s - %d unique price profiles" % [econ_diversity, price_profiles])
	_log("REPORT|")

	if prescriptions.size() > 0:
		_log("REPORT|PRESCRIPTIONS (ranked by severity):")
		for i in range(prescriptions.size()):
			var p: Dictionary = prescriptions[i]
			_log("REPORT|  #%d [%s|%s] %s" % [i + 1, p["severity"], p["confidence"], p["issue"]])
			_log("REPORT|     Evidence: %s" % p["evidence"])
			_log("REPORT|     Fix: %s" % p["fix"])
			_log("REPORT|     Metric: %s" % p["metric"])
		_log("REPORT|")
	else:
		_log("REPORT|PRESCRIPTIONS: None - all goals met!")
		_log("REPORT|")

	var top_fix := "None"
	if prescriptions.size() > 0:
		top_fix = prescriptions[0]["issue"]
	_log("REPORT|OVERALL: %.1f/5.0 avg - Top fix: %s" % [goal_avg, top_fix])
	_log("REPORT|SCORES|g1=%d g2=%d g3=%d g4=%d g5=%d s_combat=%d s_economy=%d s_faction=%d s_mission=%d s_progress=%d s_heat=%d s_boot=%d s_disclosure=%d s_overlay=%d" % [
		g1, g2, g3, g4, g5, s_combat, s_economy, s_faction, s_mission, s_progress, s_heat, s_boot, s_disclosure, s_overlay])
	_log("REPORT|===============================================")


func _do_done() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0 if not _hard_fail else 1)


# ===================== Goal Probes =====================

func _probe_fo_state() -> void:
	if _bridge == null or not _bridge.has_method("GetFirstOfficerStateV0"):
		_log("GOAL|FO|state=no_bridge_method")
		return
	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	var promoted: bool = fo.get("promoted", false)
	var name_str := str(fo.get("name", ""))
	var archetype := str(fo.get("archetype", ""))
	var tier := int(fo.get("tier", 0))
	_log("GOAL|FO|state=promoted=%s name=%s archetype=%s tier=%d" % [
		str(promoted), name_str, archetype, tier])

func _probe_fo_dialogue(event_name: String) -> void:
	# Use dialogue_count from state (non-consuming) instead of GetFirstOfficerDialogueV0
	# which consumes the pending line and races with FOPanel's _poll_fo_dialogue.
	if _bridge == null or not _bridge.has_method("GetFirstOfficerStateV0"):
		return
	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	var count := int(fo.get("dialogue_count", 0))
	if count > _fo_dialogue_count:
		var tick_now := _get_tick()
		var pending_text: String = str(fo.get("pending_text", ""))
		var text_preview := pending_text.substr(0, 100) if pending_text.length() > 0 else "(consumed)"
		_log("GOAL|FO|post_event=%s dialogue_count=%d (was %d) tick=%d frame=%d text=%s" % [event_name, count, _fo_dialogue_count, tick_now, _total_frames, text_preview])
		_fo_dialogue_count = count
		_fo_post_event_reactions += 1
	else:
		_log("GOAL|FO|post_event=%s dialogue=none" % event_name)

func _probe_dock_tabs() -> void:
	var dock_menu = root.find_child("HeroTradeMenu", true, false)
	if dock_menu == null:
		dock_menu = root.find_child("DockMenu", true, false)
	if dock_menu == null:
		_log("GOAL|TEACHES|dock_tabs_visible=unknown")
		return
	var visible_tabs := 0
	var tab_bar = dock_menu.find_child("TabBar", true, false)
	if tab_bar != null and tab_bar is TabBar:
		visible_tabs = tab_bar.tab_count
	else:
		# Fallback: count visible direct children that look like tabs
		for child in dock_menu.get_children():
			if child.visible:
				visible_tabs += 1
	_log("GOAL|TEACHES|dock_tabs_visible=%d" % visible_tabs)


# ===================== Helpers =====================

func _set_phase(p: Phase) -> void:
	_phase = p
	_last_phase_change_frame = _total_frames
	_polls = 0


func _log(msg: String) -> void:
	print(PREFIX + msg)


func _fail(reason: String) -> void:
	_hard_fail = true
	_fail_reason = reason
	_log("HARD_FAIL|%s" % reason)
	_phase = Phase.AUDIT


func _assert(condition: bool, name: String, detail: String) -> void:
	if condition:
		_log("ASSERT_PASS|%s|%s" % [name, detail])
	else:
		_log("ASSERT_FAIL|%s|%s" % [name, detail])
		_hard_fail = true
		_fail_reason = name


func _flag(msg: String) -> void:
	_flags.append(msg)
	_log("FLAG|%s" % msg)


func _capture(label: String) -> void:
	if _screenshot == null:
		return
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]
	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)
	_snapshots.append({"phase": label, "tick": tick})
	_log("CAPTURE|%s|tick=%d" % [label, tick])

	# Automated blank-region detection on dock/panel screenshots.
	# Check center panel region (~350-610 x, 90-500 y at 960x540) for blank content.
	if DisplayServer.get_name() != "headless":
		var viewport := root.get_viewport()
		if viewport != null:
			var img := viewport.get_texture().get_image()
			if img != null:
				var iw := img.get_width()
				var ih := img.get_height()
				# Dock panel content region (center ~37%-63% x, 15%-85% y)
				var dock_rect := Rect2(iw * 0.37, ih * 0.15, iw * 0.26, ih * 0.70)
				# Only check dock/tab screenshots (labels containing "dock", "tab", "market", "trade", "empire", "galaxy")
				var check_labels := ["dock", "tab_", "market", "trade", "empire", "galaxy", "sell", "mission"]
				var should_check := false
				for cl in check_labels:
					if label.to_lower().find(cl) >= 0:
						should_check = true
						break
				if should_check:
					var warn: String = _screenshot.assert_region_nonempty(img, dock_rect, label)
					if warn != "":
						_log("BLANK_WARN|%s" % warn)
						_flag("BLANK_PANEL_%s" % label)


func _lint_string(txt: String, source: String) -> void:
	# Flag raw internal IDs (star_10, fleet_trader_1, good_organics_v0, etc.)
	var raw_id_pattern := RegEx.new()
	raw_id_pattern.compile("\\b(star_\\d+|fleet_\\w+_\\d+|good_\\w+_v\\d+|at_fleet_|ai_fleet_)")
	if raw_id_pattern.search(txt) != null:
		_flag("DEV_JARGON_ID|%s|%s" % [source, txt.left(60)])
	# Flag v0/v1 suffix patterns (method names leaking to UI)
	var version_pattern := RegEx.new()
	version_pattern.compile("\\w+V\\d+$|_v\\d+$")
	if version_pattern.search(txt) != null:
		_flag("DEV_JARGON_VERSION|%s|%s" % [source, txt.left(60)])
	# Flag underscore_case identifiers (3+ chars with underscore in middle)
	var underscore_pattern := RegEx.new()
	underscore_pattern.compile("\\b\\w{3,}_\\w{3,}_\\w+\\b")
	if underscore_pattern.search(txt) != null and "%" not in txt:
		_flag("DEV_JARGON_UNDERSCORE|%s|%s" % [source, txt.left(60)])


func _find_all_labels(node: Node) -> Array:
	var result: Array = []
	if node is Label:
		result.append(node)
	for child in node.get_children():
		result.append_array(_find_all_labels(child))
	return result


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _headless_travel(dest: String) -> void:
	# Travel via sim bridge directly — bypasses game_manager scene-rebuild
	# and lane cooldown issues. Reliable for multi-hop headless testing.
	if _bridge != null:
		if _bridge.has_method("DispatchTravelCommandV0"):
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", dest)
		if _bridge.has_method("DispatchPlayerArriveV0"):
			_bridge.call("DispatchPlayerArriveV0", dest)
	# Also trigger game_manager if possible (for scene rebuild)
	if _game_manager != null:
		_game_manager.set("_lane_cooldown_v0", 0.0)
		# Only call if state allows (IN_FLIGHT -> IN_LANE_TRANSIT -> IN_FLIGHT)
		if _game_manager.get("current_player_state") != null:
			_game_manager.call("on_lane_gate_proximity_entered_v0", dest)
			_game_manager.call("on_lane_arrival_v0", dest)


func _dock_at_station() -> void:
	if _game_manager == null:
		return
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_game_manager.call("on_proximity_dock_entered_v0", targets[0])


func _init_navigation() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))
	_visited[_home_node_id] = true

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_nodes = galaxy.get("system_nodes", [])
	_all_edges = galaxy.get("lane_edges", [])
	# Retry on read-lock contention (TryExecuteSafeRead(0) can return empty cache)
	if _all_nodes.size() == 0 or _all_edges.size() == 0:
		await create_timer(0.3).timeout
		galaxy = _bridge.call("GetGalaxySnapshotV0")
		_all_nodes = galaxy.get("system_nodes", [])
		_all_edges = galaxy.get("lane_edges", [])
	_refresh_neighbors()
	_log("NAV|home=%s neighbors=%d nodes=%d edges=%d" % [
		_home_node_id, _neighbor_ids.size(), _all_nodes.size(), _all_edges.size()])


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
	_log("NEIGHBORS|current=%s count=%d ids=%s" % [current, _neighbor_ids.size(), str(_neighbor_ids)])


func _find_all_label3d() -> Array:
	var result: Array = []
	_collect_label3d(root, result)
	return result


func _collect_label3d(node: Node, result: Array) -> void:
	if node is Label3D:
		result.append(node)
	for child in node.get_children():
		_collect_label3d(child, result)


func _find_child_recursive(parent: Node, child_name: String):
	if parent.name == child_name:
		return parent
	for child in parent.get_children():
		var found = _find_child_recursive(child, child_name)
		if found != null:
			return found
	return null


func _collect_label_text(node: Node) -> String:
	var text := ""
	if node is Label:
		text += node.text + " "
	for child in node.get_children():
		text += _collect_label_text(child)
	return text


func _track_faction(node_id: String) -> void:
	if _bridge == null or not _bridge.has_method("GetTerritoryAccessV0"):
		return
	var access: Dictionary = _bridge.call("GetTerritoryAccessV0", node_id)
	var faction := str(access.get("faction_id", ""))
	if not faction.is_empty():
		_factions_visited[faction] = true


func _get_node_faction(node_id: String) -> String:
	if _bridge == null or not _bridge.has_method("GetTerritoryAccessV0"):
		return ""
	var access: Dictionary = _bridge.call("GetTerritoryAccessV0", node_id)
	return str(access.get("faction_id", ""))


func _track_system_introduced(system_name: String) -> void:
	if system_name not in _systems_introduced:
		_systems_introduced.append(system_name)


func _markets_differ(market_a: Array, market_b: Array) -> bool:
	var prices_a := {}
	for item in market_a:
		prices_a[str(item.get("good_id", ""))] = int(item.get("buy_price", 0))
	for item in market_b:
		var gid := str(item.get("good_id", ""))
		var price_b := int(item.get("buy_price", 0))
		if prices_a.has(gid) and prices_a[gid] != price_b:
			return true
	return false
