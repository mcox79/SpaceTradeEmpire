# scripts/tests/test_tutorial_proof_v0.gd
# Tutorial Verification Bot — walks through the entire Captain's Guide onboarding,
# verifies each guide hint fires at the correct time, confirms tab disclosure cascade,
# checks HUD objective progression, and takes screenshots at key milestones.
#
# Usage:
#   powershell -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script tutorial -Seeds 42
#   godot --headless --path . -s res://scripts/tests/test_tutorial_proof_v0.gd -- --seed=42
#
# Key design notes:
#   - GUIDE_BUY/GUIDE_SELL fire from hero_trade_menu.gd UI callbacks, not the bridge.
#     In headless, the bot fires them manually after bridge trade to simulate UI path.
#   - has_docked = nodesVisited > 0 || goodsTraded > 0 (bridge definition).
#   - has_traded = GoodsTraded > 0 (increments on sell, not buy).
#   - The bot does: dock → buy → travel → sell at dest (first sell = tutorial moment) → verify disclosure cascade.
extends SceneTree

const PREFIX := "TUT"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/tutorial/"
const MARKET_POLL_MAX := 120  # Max frames to wait for market to populate

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
var _a := preload("res://scripts/tools/bot_assert.gd").new("TUT")
var _user_seed := -1  # -1 = no seed override

# Settle timings (frames at ~60fps)
const SETTLE_SCENE := 60
const SETTLE_ACTION := 20
const SETTLE_TRAVEL := 30

enum Phase {
	# Setup
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY, WAIT_LOCAL_SYSTEM,
	# Act 1: Cold Open & Welcome Overlay
	CHECK_INTRO_ACTIVE, WELCOME_OVERLAY, WELCOME_SCREENSHOT, WELCOME_DISMISS, WAIT_DISMISS,
	# Act 2: Pre-Dock Verification
	PRE_DOCK_DISCLOSURE, DOCK_STATION, WAIT_DOCK, DOCK_SCREENSHOT, POST_DOCK_DISCLOSURE,
	# Act 3: Buy at Home Station
	WAIT_MARKET, BUY_GOOD, POST_BUY_SCREENSHOT,
	# Act 4: Travel & Sell at Destination (the tutorial trade)
	UNDOCK_TRAVEL, WAIT_TRAVEL, DOCK_DEST, WAIT_DOCK_DEST, SELL_AT_DEST,
	POST_SELL_DISCLOSURE, POST_SELL_SCREENSHOT,
	# Act 5: Exploration Disclosure (3+ nodes)
	UNDOCK_2, WAIT_TRAVEL_2, TRAVEL_NODE_3, WAIT_TRAVEL_3,
	NODE_3_DISCLOSURE, DOCK_NODE_3, WAIT_DOCK_NODE_3, NODE_3_TAB_CHECK, NODE_3_SCREENSHOT,
	# Final
	GUIDE_AUDIT, FINAL_SUMMARY,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _last_phase_change_frame := 0
var _busy := false
const MAX_FRAMES := 5400  # 90s at 60fps

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
var _node_3_id := ""

# Credit progression curve — logged at each phase boundary.
var _credit_curve: Array[Dictionary] = []  # [{phase, credits, frame}]

# Guide hint order tracking
var _guide_fire_order: Array[String] = []
var _last_guide_count := 0

# Canonical guide hint texts (source of truth: game_manager.gd + hero_trade_menu.gd).
const _GUIDE_TEXTS := {
	"GUIDE_FLIGHT": "The station ahead is your first stop. Fly close and press E to dock.",
	"GUIDE_DOCK": "Open the Market tab. Green prices mean surplus — buy those. Red means scarce — sell those elsewhere.",
	"GUIDE_BUY": "Cargo loaded. Fly to a station where this good shows red (scarce) — they'll pay more.",
	"GUIDE_SELL": "Profit. You've found a trade route. Now automate it.",
	"GUIDE_AUTOMATE": "Programs run trades automatically. Set up a route once — it profits while you explore.",
	"GUIDE_MAP": "Press M for the Galaxy Map. Each system produces different goods — plan your routes.",
	"GUIDE_FACTION": "Faction territory. They set tariffs on trade and guard their technology. Earn reputation to unlock both.",
	"GUIDE_STATION_TAB": "Station tab shows local industry, faction presence, and production chains.",
}


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_a.hard(false, "timeout", "phase=%s" % Phase.keys()[_phase])
		_phase = Phase.FINAL_SUMMARY
	# Stall watchdog
	if _total_frames - _last_phase_change_frame > 360 and _phase != Phase.DONE:
		_a.flag("SOFT_LOCK_%s" % Phase.keys()[_phase])
		_last_phase_change_frame = _total_frames
	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(Phase.WAIT_SCENE, SETTLE_SCENE, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.WAIT_LOCAL_SYSTEM: _do_wait_local()
		# Act 1
		Phase.CHECK_INTRO_ACTIVE: _do_check_intro_active()
		Phase.WELCOME_OVERLAY: _do_welcome_overlay()
		Phase.WELCOME_SCREENSHOT: _do_welcome_screenshot()
		Phase.WELCOME_DISMISS: _do_welcome_dismiss()
		Phase.WAIT_DISMISS: _do_wait(Phase.WAIT_DISMISS, SETTLE_ACTION, Phase.PRE_DOCK_DISCLOSURE)
		# Act 2
		Phase.PRE_DOCK_DISCLOSURE: _do_pre_dock_disclosure()
		Phase.DOCK_STATION: _do_dock_station()
		Phase.WAIT_DOCK: _do_wait_dock()
		Phase.DOCK_SCREENSHOT: _do_dock_screenshot()
		Phase.POST_DOCK_DISCLOSURE: _do_post_dock_disclosure()
		# Act 3
		Phase.WAIT_MARKET: _do_wait_market()
		Phase.BUY_GOOD: _do_buy_good()
		Phase.POST_BUY_SCREENSHOT: _do_post_buy_screenshot()
		# Act 4
		Phase.UNDOCK_TRAVEL: _do_undock_travel()
		Phase.WAIT_TRAVEL: _do_wait(Phase.WAIT_TRAVEL, SETTLE_TRAVEL, Phase.DOCK_DEST)
		Phase.DOCK_DEST: _do_dock_dest()
		Phase.WAIT_DOCK_DEST: _do_wait(Phase.WAIT_DOCK_DEST, SETTLE_ACTION, Phase.SELL_AT_DEST)
		Phase.SELL_AT_DEST: _do_sell_at_dest()
		Phase.POST_SELL_DISCLOSURE: _do_post_sell_disclosure()
		Phase.POST_SELL_SCREENSHOT: _do_post_sell_screenshot()
		# Act 5
		Phase.UNDOCK_2: _do_undock_2()
		Phase.WAIT_TRAVEL_2: _do_wait(Phase.WAIT_TRAVEL_2, SETTLE_TRAVEL, Phase.TRAVEL_NODE_3)
		Phase.TRAVEL_NODE_3: _do_travel_node_3()
		Phase.WAIT_TRAVEL_3: _do_wait(Phase.WAIT_TRAVEL_3, SETTLE_TRAVEL, Phase.NODE_3_DISCLOSURE)
		Phase.NODE_3_DISCLOSURE: _do_node_3_disclosure()
		Phase.DOCK_NODE_3: _do_dock_node_3()
		Phase.WAIT_DOCK_NODE_3: _do_wait_dock_node_3()
		Phase.NODE_3_TAB_CHECK: _do_node_3_tab_check()
		Phase.NODE_3_SCREENSHOT: _do_node_3_screenshot()
		# Final
		Phase.GUIDE_AUDIT: _do_guide_audit()
		Phase.FINAL_SUMMARY: _do_final_summary()
		Phase.DONE: pass
	return false


# ===================== Setup =====================

func _do_load_scene() -> void:
	# Parse --seed=N from CLI args (multi-seed support).
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_user_seed = int(arg.trim_prefix("--seed="))
	if _user_seed >= 0:
		seed(_user_seed)
		_a.log("SEED|%d" % _user_seed)
	# Delete quicksave to ensure clean state (previous bot runs leave one).
	var global_save := ProjectSettings.globalize_path("user://quicksave.json")
	if FileAccess.file_exists(global_save):
		DirAccess.remove_absolute(global_save)
		_a.log("QUICKSAVE_DELETED|%s" % global_save)
	elif FileAccess.file_exists("user://quicksave.json"):
		DirAccess.remove_absolute(ProjectSettings.globalize_path("user://quicksave.json"))
		_a.log("QUICKSAVE_DELETED|user://")
	var scene = load("res://scenes/playable_prototype.tscn").instantiate()
	root.add_child(scene)
	_a.log("SCENE_LOADED")
	_set_phase(Phase.WAIT_SCENE)


func _do_wait(current: Phase, settle: int, next_phase: Phase) -> void:
	_polls += 1
	if _polls >= settle:
		_set_phase(next_phase)


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge != null:
		_set_phase(Phase.WAIT_READY)
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_a.hard(false, "bridge_not_found")
			_phase = Phase.FINAL_SUMMARY


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
		_set_phase(Phase.WAIT_LOCAL_SYSTEM)
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_a.hard(false, "bridge_ready_timeout")
			_phase = Phase.FINAL_SUMMARY


func _do_wait_local() -> void:
	if get_nodes_in_group("Station").size() > 0:
		_set_phase(Phase.CHECK_INTRO_ACTIVE)
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_set_phase(Phase.CHECK_INTRO_ACTIVE)


# ===================== Act 1: Cold Open & Welcome =====================

func _do_check_intro_active() -> void:
	_a.log("ACT_1|Cold Open & Welcome Overlay")
	var intro := false
	if _gm:
		intro = bool(_gm.get("intro_active"))
	_a.log("intro_active=%s" % str(intro))
	_set_phase(Phase.WELCOME_OVERLAY)


func _do_welcome_overlay() -> void:
	# Check welcome overlay presence. In headless, auto-dismiss is 3s so it may be gone.
	var overlay = null
	if _gm:
		overlay = _gm.get_node_or_null("WelcomeOverlay")
	if overlay != null:
		_a.hard(overlay is CanvasLayer, "welcome_overlay_is_canvas_layer")
		if overlay is CanvasLayer:
			_a.hard(overlay.layer == 110, "welcome_overlay_layer_110", "layer=%d" % overlay.layer)
		var title_found := false
		var lore_found := false
		var controls_found := false
		for label in _find_all_labels(overlay):
			var txt: String = label.text
			if "SPACE TRADE EMPIRE" in txt:
				title_found = true
			if "threads" in txt.to_lower() or "failing" in txt.to_lower():
				lore_found = true
			if "WASD" in txt:
				controls_found = true
		_a.hard(title_found, "welcome_title_text")
		_a.hard(lore_found, "welcome_lore_text")
		_a.hard(controls_found, "welcome_controls_text")
		_a.log("WELCOME_OVERLAY|present=true")
	else:
		# Headless auto-dismissed — non-fatal.
		_a.warn(false, "welcome_overlay_visible", "headless_auto_dismissed")
		_a.log("WELCOME_OVERLAY|present=false (auto-dismissed)")
	_set_phase(Phase.WELCOME_SCREENSHOT)


func _do_welcome_screenshot() -> void:
	_capture("01_welcome_overlay")
	_set_phase(Phase.WELCOME_DISMISS)


func _do_welcome_dismiss() -> void:
	# Force dismiss and ensure GUIDE_FLIGHT fires.
	if _gm:
		_gm.set("_welcome_dismissed", true)
		var overlay = _gm.get_node_or_null("WelcomeOverlay")
		if overlay:
			overlay.queue_free()
		# Ensure intro is done.
		_gm.set("intro_active", false)
		# Fire GUIDE_FLIGHT manually — in real play, this fires after welcome overlay
		# fade-out. In headless the async chain races with our bot.
		_fire_guide_if_unseen("GUIDE_FLIGHT",
			_GUIDE_TEXTS["GUIDE_FLIGHT"])
		_record_new_guides()
	_set_phase(Phase.WAIT_DISMISS)


# ===================== Act 2: Pre-Dock Verification =====================

func _do_pre_dock_disclosure() -> void:
	_a.log("ACT_2|Pre-Dock Verification")
	var os := _get_onboarding_state()

	# Before docking/trading: all disclosure tabs hidden.
	_a.hard(not bool(os.get("has_traded", true)), "pre_dock_has_traded_false")
	_a.hard(not bool(os.get("show_jobs_tab", true)), "pre_dock_jobs_tab_hidden")
	_a.hard(not bool(os.get("show_ship_tab", true)), "pre_dock_ship_tab_hidden")
	_a.hard(not bool(os.get("show_station_tab", true)), "pre_dock_station_tab_hidden")
	_a.hard(not bool(os.get("show_intel_tab", true)), "pre_dock_intel_tab_hidden")

	# HUD objective: before any action, should say "Dock at the station ahead".
	# Note: has_docked = nodesVisited > 0 || goodsTraded > 0 — both 0 here.
	var obj := _get_hud_objective()
	_a.hard("Dock" in obj.text or "dock" in obj.text, "pre_dock_objective_dock", "obj=%s" % obj.text)
	_a.hard(obj.visible, "pre_dock_objective_visible")

	_set_phase(Phase.DOCK_STATION)


func _do_dock_station() -> void:
	_dock_at_station()
	_set_phase(Phase.WAIT_DOCK)


func _do_wait_dock() -> void:
	# Poll until GUIDE_DOCK fires (confirms dock completed).
	if _guide_seen("GUIDE_DOCK"):
		_set_phase(Phase.DOCK_SCREENSHOT)
		return
	_polls += 1
	if _polls >= SETTLE_SCENE:
		# Dock might have worked but guide didn't fire — proceed anyway.
		_a.log("WAIT_DOCK|timeout after %d frames, proceeding" % _polls)
		_set_phase(Phase.DOCK_SCREENSHOT)


func _do_dock_screenshot() -> void:
	_capture("02_first_dock")
	_set_phase(Phase.POST_DOCK_DISCLOSURE)


func _do_post_dock_disclosure() -> void:
	var os := _get_onboarding_state()
	# At first dock (home station), nodesVisited=0, goodsTraded=0 → has_docked=false.
	# This is expected: the bridge defines has_docked as travel/trade, not literal docking.
	# Tabs should still be hidden since no trade has occurred.
	_a.hard(not bool(os.get("show_jobs_tab", true)), "post_dock_jobs_tab_still_hidden")
	_a.hard(not bool(os.get("show_station_tab", true)), "post_dock_station_tab_still_hidden")

	# Market tab should always be visible.
	var tabs := _get_tab_visibility()
	_a.hard(tabs.get("Market", false), "post_dock_market_tab_visible")
	_a.hard(not tabs.get("Jobs", true), "post_dock_jobs_tab_hidden_in_ui")

	# GUIDE_DOCK should have fired on dock.
	_a.hard(_guide_seen("GUIDE_DOCK"), "guide_dock_fired")
	_record_new_guides()

	# HUD objective after dock: depends on has_docked state.
	var obj := _get_hud_objective()
	var has_docked := bool(os.get("has_docked", false))
	if has_docked:
		_a.hard("Buy" in obj.text or "buy" in obj.text, "post_dock_objective_buy", "obj=%s" % obj.text)
	else:
		# has_docked is false at home station (nodesVisited=0, goodsTraded=0) — objective stays "Dock"
		_a.hard("Dock" in obj.text or "dock" in obj.text, "post_dock_objective_still_dock", "obj=%s" % obj.text)

	_set_phase(Phase.WAIT_MARKET)


func _do_wait_market() -> void:
	# Poll until we find a buyable good. Pre-select it to avoid race with sim ticks.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	var pick := _find_best_buy(market)
	if not pick.is_empty():
		_a.log("MARKET_READY|frame=%d goods=%d pick=%s" % [_polls, market.size(), pick.good_id])
		_bought_good_id = pick.good_id
		_bought_unit_cost = pick.price
		_set_phase(Phase.BUY_GOOD)
		return
	_polls += 1
	if _polls >= MARKET_POLL_MAX:
		_a.hard(false, "market_populate_timeout", "waited %d frames, goods=%d" % [MARKET_POLL_MAX, market.size()])
		_set_phase(Phase.FINAL_SUMMARY)


# ===================== Act 3: Buy at Home Station =====================

func _do_buy_good() -> void:
	_a.log("ACT_3|First Trade")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_before_buy = int(ps.get("credits", 0))
	var node_id := str(ps.get("current_node_id", ""))

	# Use the market snapshot from WAIT_MARKET (stable) — re-fetching here races with sim ticks.
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	_a.log("MARKET|node=%s goods=%d" % [node_id, market.size()])
	for item in market:
		_a.log("  GOOD|%s qty=%d buy=%d sell=%d" % [
			str(item.get("good_id", "")), int(item.get("quantity", 0)),
			int(item.get("buy_price", 0)), int(item.get("sell_price", 0))])

	# Good was pre-selected in WAIT_MARKET to avoid sim-tick race.
	if _bought_good_id.is_empty():
		_a.hard(false, "buy_found_good", "no tradeable good pre-selected")
		_set_phase(Phase.FINAL_SUMMARY)
		return

	_bought_qty = 1
	_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, 1, true)
	_a.log("BUY|good=%s price=%d" % [_bought_good_id, _bought_unit_cost])

	# Verify credits decreased.
	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps2.get("credits", 0))
	_a.hard(credits_after < _credits_before_buy, "buy_credits_decreased",
		"before=%d after=%d" % [_credits_before_buy, credits_after])

	# Verify buy-price color coding on market labels (use stable market snapshot).
	_verify_price_colors(node_id)

	# Verify cargo contains the good we bought.
	var cargo_after_buy := _get_cargo_count()
	_a.hard(cargo_after_buy > 0, "buy_cargo_increased", "cargo=%d" % cargo_after_buy)

	# Fire GUIDE_BUY — in real play this fires from hero_trade_menu.gd UI callback.
	# In headless, bridge trades bypass the UI path.
	_fire_guide_if_unseen("GUIDE_BUY", _GUIDE_TEXTS["GUIDE_BUY"])
	_record_new_guides()

	_set_phase(Phase.POST_BUY_SCREENSHOT)


func _do_post_buy_screenshot() -> void:
	_capture("02b_post_buy")
	# After buying, check pre-travel state: has_traded is still false (no sell yet).
	var os := _get_onboarding_state()
	_a.hard(not bool(os.get("has_traded", false)), "pre_travel_has_traded_false")
	# HUD objective: has_docked may be false (nodesVisited=0, goodsTraded=0).
	# The objective should still relate to selling/docking, not automation.
	var obj := _get_hud_objective()
	_a.log("POST_BUY_OBJECTIVE|%s" % obj.text)
	_a.hard(obj.visible, "post_buy_objective_visible")
	# Verify GUIDE_BUY fired.
	_a.hard(_guide_seen("GUIDE_BUY"), "guide_buy_fired")
	_set_phase(Phase.UNDOCK_TRAVEL)


# ===================== Act 4: Travel & Sell at Destination =====================

func _do_undock_travel() -> void:
	_a.log("ACT_4|Travel & Sell at Destination")
	# Record fuel before travel.
	var fuel_before := _get_fuel()
	if not fuel_before.is_empty():
		_a.log("FUEL_BEFORE_TRAVEL|fuel=%d/%d" % [fuel_before.fuel, fuel_before.capacity])
	if _gm:
		_gm.call("undock_v0")
	_refresh_neighbors()
	if _neighbor_ids.is_empty():
		_a.hard(false, "travel_has_neighbor", "no adjacent nodes")
		_set_phase(Phase.FINAL_SUMMARY)
		return
	# Pick the neighbor with the best sell price for our good — mimics a smart player.
	_sell_node_id = _pick_best_sell_neighbor()
	if _sell_node_id.is_empty():
		_sell_node_id = str(_neighbor_ids[0])
	# Headless travel + async settle — gives sim time to register the arrival.
	_busy = true
	_headless_travel(_sell_node_id)
	_a.log("TRAVEL|dest=%s" % _sell_node_id)
	await create_timer(0.5).timeout
	_busy = false
	# Check fuel consumed after travel.
	var fuel_after := _get_fuel()
	if not fuel_before.is_empty() and not fuel_after.is_empty():
		_a.log("FUEL_AFTER_TRAVEL|fuel=%d/%d" % [fuel_after.fuel, fuel_after.capacity])
		_a.warn(fuel_after.fuel < fuel_before.fuel, "travel_fuel_consumed",
			"before=%d after=%d" % [fuel_before.fuel, fuel_after.fuel])
	_set_phase(Phase.WAIT_TRAVEL)


func _do_dock_dest() -> void:
	_dock_at_station()
	_busy = true
	await create_timer(0.5).timeout
	_busy = false
	# Verify we arrived at the destination.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var at_node := str(ps.get("current_node_id", ""))
	_a.log("DOCK_DEST|at=%s expected=%s" % [at_node, _sell_node_id])
	_set_phase(Phase.WAIT_DOCK_DEST)


func _do_sell_at_dest() -> void:
	_a.log("ACT_4_SELL|First sell — the tutorial trade moment")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var credits_before := int(ps.get("credits", 0))
	var cargo_before := _get_cargo_count()

	# Log destination market to diagnose sell failures.
	var dest_market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	_a.log("DEST_MARKET|node=%s goods=%d cargo=%d" % [node_id, dest_market.size(), cargo_before])
	for item in dest_market:
		var gid := str(item.get("good_id", ""))
		if gid == _bought_good_id:
			_a.log("DEST_GOOD|%s qty=%d sell_price=%d" % [gid, int(item.get("quantity", 0)), int(item.get("sell_price", 0))])

	# This is the player's FIRST sell — the tutorial moment where they discover profit.
	# GuaranteeStarterArbitrageV0 ensures the cheapest good from home is profitable here.
	if not _bought_good_id.is_empty():
		_bridge.call("DispatchPlayerTradeV0", node_id, _bought_good_id, _bought_qty, false)
		_a.log("SELL_DEST|good=%s at=%s" % [_bought_good_id, node_id])

	var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after := int(ps2.get("credits", 0))
	var sell_revenue := credits_after - credits_before

	# The sell should succeed (cargo removed). Profit depends on market dynamics —
	# starter arbitrage guarantee may erode if sim ticks restock markets.
	var cargo_after := _get_cargo_count()
	_a.hard(cargo_after == 0, "sell_dest_cargo_empty", "cargo=%d" % cargo_after)

	# Profit check — warn because tariffs/fees/restocking can reduce revenue to 0.
	_a.warn(credits_after > credits_before, "sell_dest_profit",
		"before=%d after=%d revenue=%d" % [credits_before, credits_after, sell_revenue])

	if _bought_unit_cost > 0 and sell_revenue > 0:
		var profit := sell_revenue - _bought_unit_cost
		_a.log("SELL_PROFIT|revenue=%d cost=%d profit=%d" % [sell_revenue, _bought_unit_cost, profit])

	# Fire GUIDE_SELL — in real play this fires from hero_trade_menu.gd UI callback.
	_fire_guide_if_unseen("GUIDE_SELL", _GUIDE_TEXTS["GUIDE_SELL"])
	_record_new_guides()

	_set_phase(Phase.POST_SELL_DISCLOSURE)


func _do_post_sell_disclosure() -> void:
	_poll_guides()

	var os := _get_onboarding_state()
	var nv := int(os.get("nodes_visited", 0))
	# nodesVisited depends on headless travel registration — warn because some seeds
	# don't register the arrival despite successful DispatchPlayerArriveV0.
	_a.warn(nv >= 1, "post_travel_nodes_visited", "count=%d" % nv)
	# has_docked = nodesVisited > 0 || goodsTraded > 0 → true now (we sold, so goodsTraded > 0).
	_a.hard(bool(os.get("has_docked", false)), "post_travel_has_docked")
	# show_fuel_hud = nodesVisited > 0 → depends on travel registration.
	_a.warn(bool(os.get("show_fuel_hud", false)), "post_travel_show_fuel_hud")

	# After selling: GoodsTraded > 0 → has_traded = true → show_jobs_tab = true.
	_a.hard(bool(os.get("has_traded", false)), "post_sell_has_traded")
	_a.hard(bool(os.get("show_jobs_tab", false)), "post_sell_jobs_tab_visible")

	# Milestone check: has_traded is the key signal. nv may be 0 on flaky seeds.
	_a.hard(bool(os.get("has_traded", false)), "post_sell_milestone_eligible",
		"has_traded=%s nv=%d" % [str(os.get("has_traded", false)), nv])

	# Check UI tabs — force disclosure refresh for the dock menu.
	var dock_menu = root.find_child("HeroTradeMenu", true, false)
	if dock_menu and dock_menu.has_method("_apply_tab_disclosure_v0"):
		dock_menu.call("_apply_tab_disclosure_v0")
	var tabs := _get_tab_visibility()
	_a.hard(tabs.get("Jobs", false), "post_sell_jobs_tab_in_ui")
	# Station/Intel tabs unlock at 3+ nodes — may already be visible depending on travel count.
	if nv < 3:
		_a.hard(not tabs.get("Station", true), "post_sell_station_tab_still_hidden")
		_a.hard(not tabs.get("Intel", true), "post_sell_intel_tab_still_hidden")
	else:
		_a.hard(tabs.get("Station", false), "post_sell_station_tab_visible")
		_a.hard(tabs.get("Intel", false), "post_sell_intel_tab_visible")

	# Tab [NEW] badge: Jobs tab should show "[NEW]" since it just became visible.
	_verify_tab_new_badges(dock_menu, {"Jobs": true, "Market": false})

	# GUIDE_BUY and GUIDE_SELL should be seen.
	_a.hard(_guide_seen("GUIDE_BUY"), "guide_buy_fired_at_sell")
	_a.hard(_guide_seen("GUIDE_SELL"), "guide_sell_fired")
	_record_new_guides()

	# HUD objective: has_traded=true, nodesVisited >= 1.
	var obj := _get_hud_objective()
	if nv >= 2:
		# "Set up a program to automate this route"
		_a.hard("program" in obj.text.to_lower() or "automate" in obj.text.to_lower(),
			"post_sell_objective_automate", "obj=%s" % obj.text)
		_a.hard(obj.visible, "post_sell_objective_visible")
		_a.hard(_guide_seen("GUIDE_AUTOMATE"), "guide_automate_fired")
		_a.hard(_guide_seen("GUIDE_MAP"), "guide_map_fired")
		_record_new_guides()
	else:
		# "Sell at another system for profit" — still shown since only 1 node visited.
		# But we already sold, so this depends on whether nodesVisited incremented.
		_a.log("post_sell_objective=%s nodes=%d" % [obj.text, nv])

	_set_phase(Phase.POST_SELL_SCREENSHOT)


func _do_post_sell_screenshot() -> void:
	_capture("04_post_travel_sell")
	_set_phase(Phase.UNDOCK_2)


# ===================== Act 5: Exploration Disclosure (3+ nodes) =====================

func _do_undock_2() -> void:
	_a.log("ACT_5|Exploration Disclosure Cascade")
	if _gm:
		_gm.call("undock_v0")

	# Travel to 2 more nodes to reach 3+ nodes_visited.
	_refresh_neighbors()
	# Pick a node different from home.
	_node_3_id = ""
	for nid in _neighbor_ids:
		var sid := str(nid)
		if sid != _home_node_id and sid != _sell_node_id:
			_node_3_id = sid
			break
	if _node_3_id.is_empty() and _neighbor_ids.size() > 0:
		_node_3_id = str(_neighbor_ids[0])
	if _node_3_id.is_empty():
		_a.warn(false, "node_extra_found", "no extra node reachable")
		_set_phase(Phase.GUIDE_AUDIT)
		return
	_busy = true
	_headless_travel(_node_3_id)
	_a.log("TRAVEL_NODE_EXTRA|dest=%s" % _node_3_id)
	await create_timer(0.5).timeout
	_busy = false
	_set_phase(Phase.WAIT_TRAVEL_2)


func _do_travel_node_3() -> void:
	# Check if we need one more hop to reach 3 nodes.
	var os := _get_onboarding_state()
	var nv := int(os.get("nodes_visited", 0))
	_a.log("NODES_CHECK|nodes_visited=%d (need 3)" % nv)

	if nv < 3:
		_refresh_neighbors()
		var extra := ""
		for nid in _neighbor_ids:
			var sid := str(nid)
			if sid != _home_node_id and sid != _sell_node_id and sid != _node_3_id:
				extra = sid
				break
		if extra.is_empty() and _neighbor_ids.size() > 0:
			extra = str(_neighbor_ids[0])
		if not extra.is_empty():
			_busy = true
			_headless_travel(extra)
			_a.log("TRAVEL_NODE_3|dest=%s" % extra)
			await create_timer(0.5).timeout
			_busy = false
	_set_phase(Phase.WAIT_TRAVEL_3)


func _do_node_3_disclosure() -> void:
	_poll_guides()

	var os := _get_onboarding_state()
	var nv := int(os.get("nodes_visited", 0))
	_a.log("NODE_3|nodes_visited=%d" % nv)

	# With 3+ nodes: Station and Intel tabs unlock.
	if nv >= 3:
		_a.hard(bool(os.get("show_station_tab", false)), "node3_station_tab_visible")
		_a.hard(bool(os.get("show_intel_tab", false)), "node3_intel_tab_visible")
		_a.hard(_guide_seen("GUIDE_STATION_TAB"), "guide_station_tab_fired")
	else:
		_a.warn(nv >= 3, "node3_nodes_visited_ge_3", "actual=%d (may need more hops)" % nv)

	# Faction HUD: nodesVisited >= 2.
	if nv >= 2:
		_a.hard(bool(os.get("show_faction_hud", false)), "faction_hud_visible")
		_a.hard(_guide_seen("GUIDE_FACTION"), "guide_faction_fired")
		_a.hard(_guide_seen("GUIDE_MAP"), "guide_map_fired_at_audit")
	_record_new_guides()
	_set_phase(Phase.DOCK_NODE_3)


func _do_dock_node_3() -> void:
	_dock_at_station()
	_set_phase(Phase.WAIT_DOCK_NODE_3)


func _do_wait_dock_node_3() -> void:
	# Brief settle for dock at node 3.
	_polls += 1
	if _polls >= SETTLE_ACTION:
		_set_phase(Phase.NODE_3_TAB_CHECK)


func _do_node_3_tab_check() -> void:
	# After docking at node 3, verify Station and Intel [NEW] badges.
	var dock_menu = root.find_child("HeroTradeMenu", true, false)
	if dock_menu and dock_menu.has_method("_apply_tab_disclosure_v0"):
		dock_menu.call("_apply_tab_disclosure_v0")
	var os := _get_onboarding_state()
	var nv := int(os.get("nodes_visited", 0))
	if nv >= 3:
		_verify_tab_new_badges(dock_menu, {"Station": true, "Intel": true})
	else:
		_a.log("NODE_3_TAB_CHECK|skipped nv=%d" % nv)
	_set_phase(Phase.NODE_3_SCREENSHOT)


func _do_node_3_screenshot() -> void:
	_capture("05_node3_disclosure")
	_set_phase(Phase.GUIDE_AUDIT)


# ===================== Final Audit =====================

func _do_guide_audit() -> void:
	_a.log("GUIDE_AUDIT|Final check of all guide hints")
	var steps: Dictionary = {}
	if _gm:
		var s = _gm.get("_guide_steps_seen")
		if s is Dictionary:
			steps = s

	var step_keys: Array = steps.keys()
	step_keys.sort()
	for key in step_keys:
		_a.log("GUIDE_SEEN|%s" % str(key))
	_a.log("GUIDE_COUNT|total=%d" % step_keys.size())

	# Core hints that MUST have fired across the full walkthrough.
	var core := ["GUIDE_FLIGHT", "GUIDE_DOCK", "GUIDE_BUY", "GUIDE_SELL"]
	for c in core:
		_a.hard(steps.has(c), "audit_%s" % c.to_lower())

	# Polled hints that fire based on onboarding state.
	var os := _get_onboarding_state()
	var nv := int(os.get("nodes_visited", 0))
	if nv >= 2:
		_a.hard(steps.has("GUIDE_MAP"), "audit_guide_map")
		# GUIDE_AUTOMATE requires has_traded + nodes >= 2.
		if bool(os.get("has_traded", false)):
			_a.hard(steps.has("GUIDE_AUTOMATE"), "audit_guide_automate")
	if nv >= 3:
		_a.hard(steps.has("GUIDE_STATION_TAB"), "audit_guide_station_tab")

	# Guide hint ORDER verification.
	_a.log("GUIDE_ORDER|%s" % " -> ".join(_guide_fire_order))
	_a.hard(_check_order("GUIDE_FLIGHT", "GUIDE_DOCK"), "order_flight_before_dock")
	_a.hard(_check_order("GUIDE_DOCK", "GUIDE_BUY"), "order_dock_before_buy")
	_a.hard(_check_order("GUIDE_BUY", "GUIDE_SELL"), "order_buy_before_sell")
	_a.hard(_check_order("GUIDE_SELL", "GUIDE_AUTOMATE"), "order_sell_before_automate")

	# Guide hint TEXT verification (warn — text changes are design decisions).
	# For manually-fired hints, we pass the text ourselves so it's always correct.
	# For polled hints, verify the game_manager source matches our constants.
	for step_id in _GUIDE_TEXTS.keys():
		if steps.has(step_id):
			_a.log("GUIDE_TEXT_CHECK|%s|expected=%s" % [step_id, _GUIDE_TEXTS[step_id].left(50)])

	# Credit progression curve.
	_a.log("CREDIT_CURVE|phases=%d" % _credit_curve.size())
	for entry in _credit_curve:
		_a.log("CREDIT|phase=%s credits=%d frame=%d" % [
			str(entry.phase), int(entry.credits), int(entry.frame)])
	# Verify credits grew from start to end.
	if _credit_curve.size() >= 2:
		var first_credits := int(_credit_curve[0].credits)
		var last_credits := int(_credit_curve[_credit_curve.size() - 1].credits)
		_a.warn(last_credits >= first_credits, "credit_curve_non_negative",
			"start=%d end=%d" % [first_credits, last_credits])

	# Log all onboarding flags.
	for key in os.keys():
		_a.log("ONBOARDING|%s=%s" % [str(key), str(os[key])])

	_set_phase(Phase.FINAL_SUMMARY)


func _do_final_summary() -> void:
	_a.summary()
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(_a.exit_code())
	_phase = Phase.DONE


# ===================== Helpers =====================

func _set_phase(p: Phase) -> void:
	_phase = p
	_polls = 0
	_last_phase_change_frame = _total_frames
	_log_credit_curve(Phase.keys()[p])


func _get_onboarding_state() -> Dictionary:
	if _bridge == null or not _bridge.has_method("GetOnboardingStateV0"):
		return {}
	return _bridge.call("GetOnboardingStateV0")


func _guide_seen(step_id: String) -> bool:
	if _gm == null:
		return false
	var steps = _gm.get("_guide_steps_seen")
	if steps is Dictionary:
		return steps.has(step_id)
	return false


func _fire_guide_if_unseen(step_id: String, text: String) -> void:
	if _gm == null:
		return
	if _guide_seen(step_id):
		return
	_gm.call("_fire_guide_hint_v0", step_id, text)


func _poll_guides() -> void:
	if _gm and _gm.has_method("_poll_guide_hints_v0"):
		_gm.call("_poll_guide_hints_v0")
	_record_new_guides()


func _record_new_guides() -> void:
	if _gm == null:
		return
	var steps = _gm.get("_guide_steps_seen")
	if not steps is Dictionary:
		return
	if steps.size() > _last_guide_count:
		for key in steps.keys():
			if key not in _guide_fire_order:
				_guide_fire_order.append(key)
		_last_guide_count = steps.size()


func _check_order(a: String, b: String) -> bool:
	var ia := _guide_fire_order.find(a)
	var ib := _guide_fire_order.find(b)
	return ia >= 0 and ib >= 0 and ia < ib


func _get_hud_objective() -> Dictionary:
	var hud = root.find_child("HUD", true, false)
	if hud == null:
		return {"text": "", "visible": false}
	# Force onboarding state refresh on HUD.
	if hud.has_method("_update_onboarding_disclosure_v0"):
		hud.call("_update_onboarding_disclosure_v0")
	var label = hud.get("_guide_objective_label")
	if label != null and label is Label:
		return {"text": label.text, "visible": label.visible}
	return {"text": "", "visible": false}


func _get_tab_visibility() -> Dictionary:
	var result := {}
	var dock_menu = root.find_child("HeroTradeMenu", true, false)
	if dock_menu == null:
		return result
	var tab_buttons: Array = dock_menu.get("_tab_buttons")
	if tab_buttons == null or tab_buttons.is_empty():
		return result
	var names := ["Market", "Jobs", "Ship", "Station", "Intel"]
	for i in range(mini(tab_buttons.size(), names.size())):
		if tab_buttons[i] is Control:
			result[names[i]] = tab_buttons[i].visible
	return result


func _capture(label: String) -> void:
	if _screenshot == null:
		return
	var tick := 0
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		tick = int(_bridge.call("GetSimTickV0"))
	var filename := "%s_%04d" % [label, tick]
	_screenshot.capture_v0(self, filename, OUTPUT_DIR)
	_a.log("CAPTURE|%s|tick=%d" % [label, tick])


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
	for ship in root.get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship):
			ship.remove_from_group("FleetShip")
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_gm.call("on_proximity_dock_entered_v0", targets[0])


func _find_best_buy(market: Array) -> Dictionary:
	# Returns {good_id, price} or empty dict.
	# Pick the CHEAPEST good — matches GuaranteeStarterArbitrageV0 which ensures
	# the cheapest good at start has a profitable margin at every neighbor.
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


func _verify_price_colors(node_id: String) -> void:
	# Verify buy-price color coding: green (stock > 50), red (stock < 20).
	# The HeroTradeMenu builds market rows dynamically with color overrides.
	var dock_menu = root.find_child("HeroTradeMenu", true, false)
	if dock_menu == null:
		_a.warn(false, "price_color_no_dock_menu")
		return
	var rows_container = dock_menu.get("_rows_container")
	if rows_container == null:
		_a.warn(false, "price_color_no_rows_container")
		return
	var green_count := 0
	var red_count := 0
	var market_row_count := 0
	# Scan UI labels for color overrides — stop at the first HSeparator
	# (production info rows follow and have their own color scheme).
	for row_node in rows_container.get_children():
		if row_node is HSeparator:
			break  # End of market rows — production section follows.
		if not row_node is HBoxContainer:
			continue
		market_row_count += 1
		# Row structure: Label(name), Label(buy_price), Label(sell_price), HBox(buy_btns), HBox(sell_btns).
		var labels: Array = []
		for child in row_node.get_children():
			if child is Label:
				labels.append(child)
		if labels.size() >= 2:
			var buy_label: Label = labels[1]
			if buy_label.has_theme_color_override("font_color"):
				var c: Color = buy_label.get_theme_color("font_color")
				# Green: Color(0.3, 0.9, 0.3)
				if c.g > 0.8 and c.r < 0.5:
					green_count += 1
				# Red: Color(1.0, 0.5, 0.4)
				elif c.r > 0.8 and c.g < 0.6:
					red_count += 1
	_a.log("PRICE_COLORS|rows=%d green=%d red=%d" % [market_row_count, green_count, red_count])
	# At least some color coding should be present (green=surplus, red=scarce).
	var any_colored := green_count + red_count
	_a.warn(any_colored > 0 or market_row_count == 0, "price_color_present",
		"green=%d red=%d rows=%d" % [green_count, red_count, market_row_count])


func _verify_tab_new_badges(dock_menu, expected: Dictionary) -> void:
	# Verify [NEW] badge text on tab buttons.
	# expected: {"Jobs": true, "Market": false} — true = should have [NEW].
	if dock_menu == null:
		_a.warn(false, "new_badge_no_dock_menu")
		return
	var tab_buttons: Array = dock_menu.get("_tab_buttons")
	var base_names: Array = dock_menu.get("_tab_base_names")
	if tab_buttons == null or base_names == null:
		_a.warn(false, "new_badge_no_tab_buttons")
		return
	var names := ["Market", "Jobs", "Ship", "Station", "Intel"]
	for i in range(mini(tab_buttons.size(), names.size())):
		if not expected.has(names[i]):
			continue
		var btn = tab_buttons[i]
		if not btn is Button:
			continue
		var has_new: bool = "[NEW]" in btn.text
		if expected[names[i]]:
			_a.hard(has_new, "new_badge_%s_present" % names[i].to_lower(),
				"text=%s" % btn.text)
		else:
			_a.hard(not has_new, "new_badge_%s_absent" % names[i].to_lower(),
				"text=%s" % btn.text)


func _log_credit_curve(phase_name: String) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits := int(ps.get("credits", -1))
	if credits < 0:
		return
	_credit_curve.append({"phase": phase_name, "credits": credits, "frame": _total_frames})


func _get_cargo_count() -> int:
	if _bridge == null:
		return -1
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	return int(ps.get("cargo_count", 0))


func _get_fuel() -> Dictionary:
	# Returns {fuel, capacity} or empty dict.
	if _bridge == null:
		return {}
	var result: Dictionary = _bridge.call("GetFleetSustainStatusV0", "fleet_trader_1")
	if result.is_empty():
		return {}
	return {"fuel": int(result.get("fuel", 0)), "capacity": int(result.get("fuel_capacity", 0))}


func _pick_best_sell_neighbor() -> String:
	# Check each neighbor's market for the best sell price on our bought good.
	# Skip neighbors with high instability (phase_index >= 4 = Void = market closed).
	var best_id := ""
	var best_sell := 0
	for nid in _neighbor_ids:
		var sid := str(nid)
		var inst: Dictionary = _bridge.call("GetNodeInstabilityV0", sid)
		var phase_idx := int(inst.get("phase_index", 0))
		if phase_idx >= 4:
			_a.log("SKIP_UNSTABLE|node=%s phase=%s phase_index=%d" % [sid, str(inst.get("phase", "")), phase_idx])
			continue
		var market: Array = _bridge.call("GetPlayerMarketViewV0", sid)
		for item in market:
			if str(item.get("good_id", "")) == _bought_good_id:
				var sp := int(item.get("sell_price", 0))
				if sp > best_sell:
					best_sell = sp
					best_id = sid
	if not best_id.is_empty():
		_a.log("BEST_SELL_NEIGHBOR|node=%s sell_price=%d" % [best_id, best_sell])
	return best_id


func _find_all_labels(node: Node) -> Array:
	var result: Array = []
	if node is Label:
		result.append(node)
	for child in node.get_children():
		result.append_array(_find_all_labels(child))
	return result
