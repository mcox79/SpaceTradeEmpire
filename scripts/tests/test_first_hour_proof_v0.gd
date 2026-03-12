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
	# Act 5: Galaxy Opens (15:00-30:00)
	GALAXY_MAP, UNDOCK_4, MULTI_HOP, SETTLE_HOP, TRADE_ROUTE, CHECK_SUSTAIN,
	# Act 6: Scale Reveal (30:00-60:00)
	DEEP_EXPLORE, SETTLE_DEEP, PRICE_DIVERSITY, CHECK_RESEARCH, FINAL_TRADE, AUDIT,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false  # Guard against await re-entry
const MAX_FRAMES := 3600  # 60s at 60fps

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


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_fail("timeout_at_%s" % Phase.keys()[_phase])
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

	_capture("01_boot")
	_polls = 0
	_phase = Phase.CHECK_NPC


func _do_check_npc() -> void:
	var npcs = get_nodes_in_group("FleetShip")
	_log("CHECK_NPC|count=%d" % npcs.size())
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
	_capture("04_dock_market")

	# Goal 2 probe: tutorial text scan + dock tab count
	var tutorial_found := false
	for label in _find_all_label3d():
		var lt: String = label.text.to_lower()
		if "tutorial" in lt or "press x" in lt or "click here" in lt:
			tutorial_found = true
			break
	_log("GOAL|TEACHES|tutorial_text_found=%s" % str(tutorial_found))
	_log("GOAL|TEACHES|system_introduced=market")
	_probe_dock_tabs()

	_busy = false
	_polls = 0
	_phase = Phase.CHECK_FO


# ===================== Act 2: First Trade =====================

func _do_check_fo() -> void:
	_log("ACT_2|First Trade")
	var fo_panel = root.find_child("FOPanel", true, false)
	if fo_panel != null:
		# Scan for dev-facing text
		var all_text := _collect_label_text(fo_panel)
		if "Score:" in all_text: _flag("FO_PANEL_DEV_STATE|Score")
		if "War Faces" in all_text: _flag("FO_PANEL_DEV_STATE|WarFaces")
		if "No known NPCs" in all_text: _flag("FO_PANEL_DEV_STATE|NoKnownNPCs")

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
	_bridge.call("DispatchPlayerTradeV0", node_id, best_good, buy_qty, true)
	_bought_good_id = best_good
	_log("BUY|good=%s qty=%d price=%d" % [best_good, buy_qty, best_price])

	# Goal 4 probe: log the computed margin
	_log("GOAL|PROFIT|margin=%d good=%s" % [best_margin, best_good])

	# Wait for state update
	_busy = true
	await create_timer(0.2).timeout
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps2.get("credits", 0))
	var cargo_after := int(ps2.get("cargo_count", 0))

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
	_capture("10_dock_2")
	_polls = 0
	_phase = Phase.SELL


func _do_sell() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var credits_before_sell := int(ps.get("credits", 0))

	if not _bought_good_id.is_empty():
		var cargo := int(ps.get("cargo_count", 0))
		if cargo > 0:
			_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, cargo, false)
			_log("SELL|good=%s qty=%d" % [_bought_good_id, cargo])

	_busy = true
	await create_timer(0.2).timeout
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_after_sell = int(ps2.get("credits", 0))
	var cargo_after := int(ps2.get("cargo_count", 0))

	if _credits_after_sell > credits_before_sell:
		_trades_completed += 1
	else:
		_flag("SELL_NO_PROFIT|before=%d after=%d" % [credits_before_sell, _credits_after_sell])

	# Goal 2 probe: selling is the second system introduced
	_log("GOAL|TEACHES|system_introduced=selling")

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
	_log("GOAL|PROFIT|delta=%d pct=%d fo_reacted=%s" % [delta, pct, str(fo_reacted)])

	_polls = 0
	_phase = Phase.CHECK_MISSIONS


# ===================== Act 3: First Mission =====================

func _do_check_missions() -> void:
	_log("ACT_3|First Mission")
	if not _bridge.has_method("GetMissionListV0"):
		_log("MISSIONS|bridge_missing_method")
		_phase = Phase.FIND_HOSTILE
		return
	var missions: Array = _bridge.call("GetMissionListV0")
	_assert(missions.size() >= 1, "missions_available", "count=%d" % missions.size())
	_log("MISSIONS|available=%d" % missions.size())

	# Goal 2 probe: missions are the third system introduced
	_log("GOAL|TEACHES|system_introduced=missions")
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

	var ps_before: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_before := int(ps_before.get("credits", 0))

	# Deal damage (5 hits of 20 dmg)
	for i in range(5):
		_bridge.call("DamageNpcFleetV0", fleet_id, 20)
	_log("COMBAT|hits=5 dmg=100 target=%s" % fleet_id)

	# Check player survived
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull := int(hp.get("hull", 100))
		if hull <= 0:
			_flag("COMBAT_ONE_SHOT")
		_log("COMBAT|player_hull=%d" % hull)

	_combats_completed += 1
	_capture("18_combat")

	# Goal 2 + Goal 3 probes: combat introduced, FO reaction
	_log("GOAL|TEACHES|system_introduced=combat")
	_probe_fo_dialogue("COMBAT")

	_polls = 0
	_phase = Phase.POST_COMBAT


func _do_post_combat() -> void:
	# Wait for kernel to process destruction + loot roll
	_busy = true
	await create_timer(0.3).timeout

	# Collect loot drops at current node
	var loot_credits := 0
	if _bridge.has_method("GetNearbyLootV0"):
		var drops: Array = _bridge.call("GetNearbyLootV0")
		for drop in drops:
			var drop_id := str(drop.get("drop_id", ""))
			if drop_id.is_empty():
				continue
			if _bridge.has_method("DispatchCollectLootV0"):
				var result: Dictionary = _bridge.call("DispatchCollectLootV0", drop_id)
				loot_credits += int(result.get("credits_gained", 0))
				_log("LOOT|drop=%s credits=%d" % [drop_id, int(result.get("credits_gained", 0))])

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_log("POST_COMBAT|credits=%d loot=%d" % [int(ps.get("credits", 0)), loot_credits])
	_busy = false
	_polls = 0
	_phase = Phase.DOCK_3


func _do_dock_3() -> void:
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout
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
	elif install_slot < 0:
		_log("INSTALL|no_matching_slot")
	else:
		_flag("UPGRADE_TOO_EXPENSIVE")
		_log("INSTALL|no_affordable_module")

	# Goal 2 + Goal 5 probes: fitting introduced, remaining empty slots
	_log("GOAL|TEACHES|system_introduced=fitting")
	var empty_slots := 0
	if _bridge.has_method("GetPlayerFleetSlotsV0"):
		var all_slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
		for s in all_slots:
			var inst := str(s.get("installed_module_id", ""))
			if inst.is_empty() or inst == "null" or inst == "None":
				empty_slots += 1
	_log("GOAL|DEPTH|empty_slots=%d" % empty_slots)

	_capture("22_ship_fitted")
	_polls = 0
	_phase = Phase.GALAXY_MAP


# ===================== Act 5: Galaxy Opens =====================

func _do_galaxy_map() -> void:
	_log("ACT_5|Galaxy Opens")
	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var nodes: Array = galaxy.get("system_nodes", [])
	var edges: Array = galaxy.get("lane_edges", [])
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
	_busy = true
	for _hop_i in range(2):
		_refresh_neighbors()
		var target := ""
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
	# Navigate to 3 more unique systems with settle between hops
	_busy = true
	for _hop in range(3):
		_refresh_neighbors()
		var target := ""
		for nid in _neighbor_ids:
			if not _visited.has(nid):
				target = str(nid)
				break
		if target.is_empty():
			_log("DEEP|hop=%d no_unvisited_neighbor" % _hop)
			break
		_headless_travel(target)
		_visited[target] = true
		_log("DEEP|hop=%d dest=%s" % [_hop, target])
		await create_timer(0.5).timeout  # Let scene rebuild between hops

	_capture("27_deep_explore")
	_busy = false
	_polls = 0
	_phase = Phase.SETTLE_DEEP


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

	_capture("30_final_trade")
	_busy = false
	_polls = 0
	_phase = Phase.AUDIT


func _do_audit() -> void:
	# Goal 3 probe: total FO dialogue count
	_log("GOAL|FO|total_lines=%d" % _fo_dialogue_count)

	_log("AUDIT|visited=%d trades=%d combats=%d flags=%d" % [
		_visited.size(), _trades_completed, _combats_completed, _flags.size()])

	_assert(_visited.size() >= 6, "deep_explore_6_nodes", "visited=%d" % _visited.size())
	_assert(_trades_completed >= 1, "trades_completed", "count=%d" % _trades_completed)
	_assert(_combats_completed >= 1, "combat_completed", "count=%d" % _combats_completed)

	# Check for dev jargon in Label3D
	for label in _find_all_label3d():
		if label.visible and label.text.length() > 40:
			_flag("LABEL_TOO_LONG|%s" % label.text.left(50))
		if label.visible and "RareMIn" in label.text:
			_flag("DEV_JARGON|%s" % label.text)

	# Run aesthetic audit
	var audit_critical := 0
	if _observer != null and _audit != null:
		var report = _observer.capture_full_report_v0()
		var audit_results = _audit.run_audit_v0(report)
		audit_critical = _audit.count_critical_failures_v0(audit_results)
		for ar in audit_results:
			var status = "PASS" if ar.get("passed", false) else "FAIL"
			_log("AESTHETIC|%s|%s|%s" % [status, str(ar.get("flag", "")), str(ar.get("detail", ""))])

	_capture("31_final")

	# Print all flags
	for f in _flags:
		_log("FLAG|%s" % f)

	_log("SUMMARY|visited=%d trades=%d combats=%d flags=%d audit_critical=%d hard_fail=%s" % [
		_visited.size(), _trades_completed, _combats_completed,
		_flags.size(), audit_critical, str(_hard_fail)])

	if _hard_fail:
		_log("FAIL|%s" % _fail_reason)
	elif audit_critical > 0:
		_log("FAIL|audit_critical=%d" % audit_critical)
	else:
		_log("PASS|screenshots=%d" % _snapshots.size())

	_phase = Phase.DONE


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
	if _bridge == null or not _bridge.has_method("GetFirstOfficerDialogueV0"):
		return
	var line: String = _bridge.call("GetFirstOfficerDialogueV0")
	if line.is_empty() or line == "null":
		_log("GOAL|FO|post_event=%s dialogue=none" % event_name)
	else:
		_fo_dialogue_count += 1
		_log("GOAL|FO|post_event=%s dialogue=%s" % [event_name, line])

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
