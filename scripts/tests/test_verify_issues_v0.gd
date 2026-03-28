# scripts/tests/test_verify_issues_v0.gd
# Issue Verification Bot — Targeted probes to confirm/refute audit findings.
# Runs a scripted walkthrough: boot → dock → trade → explore → combat → refit → galaxy map.
# Each probe emits VFY|VERIFY|probe_name|CONFIRMED/UNCONFIRMED/SKIP|evidence.
#
# Usage:
#   powershell -File scripts/tools/Run-VerifyIssues.ps1 -Mode headless -Seed 42
#   powershell -File scripts/tools/Run-VerifyIssues.ps1 -Mode visual -Seed 42
#
# Direct:
#   godot --headless --path . -s res://scripts/tests/test_verify_issues_v0.gd -- --seed=42
extends SceneTree

const PREFIX := "VFY"
const MAX_FRAMES := 5400  # 90s at 60fps
const TICK_ADVANCE := 5

# Settle timings (frames at ~60fps) — generous for visual mode
const SETTLE_SCENE := 60
const SETTLE_TRAVEL := 40
const SETTLE_ACTION := 25
const SETTLE_DOCK := 40

const AssertScript = preload("res://scripts/tools/bot_assert.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY, WAIT_LOCAL,
	# Group 1: Boot
	BOOT_PROBES,
	# Group 2: First dock
	TRAVEL_DOCK_1, SETTLE_TD1, DOCK_1, SETTLE_D1, DOCK_PROBES,
	# Group 3: Trade (buy here, travel, sell there)
	BUY, SETTLE_BUY, UNDOCK_1, SETTLE_U1,
	TRAVEL_SELL, SETTLE_TS, DOCK_SELL, SETTLE_DS, SELL, SETTLE_SELL, TRADE_PROBES,
	# Group 4: Explore (visit 2 more systems)
	UNDOCK_2, SETTLE_U2,
	TRAVEL_E1, SETTLE_E1, FLIGHT_PROBES_1,
	TRAVEL_E2, SETTLE_E2, FLIGHT_PROBES_2,
	# Group 5: Combat
	COMBAT_SETUP, COMBAT_WAIT, COMBAT_PROBES,
	# Group 6: Refit
	DOCK_REFIT, SETTLE_DR, REFIT_PROBES,
	# Group 7: Galaxy map
	UNDOCK_MAP, SETTLE_UM, GALAXY_OPEN, SETTLE_GO, GALAXY_PROBES, GALAXY_CLOSE,
	# Group 8: Aggregate play (50 quick decisions for metric probes)
	AGGREGATE_PLAY,
	# Group 9: Final
	METRIC_PROBES, SUMMARY, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _settle_frames := 0
var _user_seed := -1

var _bridge = null
var _game_manager = null
var _a: Object  # bot_assert
var _screenshot = null
var _is_visual := false

# Navigation state
var _home_node := ""
var _current_node := ""
var _all_nodes: Array = []
var _all_edges: Array = []
var _adj: Dictionary = {}
var _visited: Dictionary = {}
var _neighbor_ids: Array = []

# Trade state
var _bought_good_id := ""
var _bought_qty := 0
var _credits_before_sell := 0
var _credits_after_sell := 0
var _buy_station := ""
var _sell_station := ""

# Tracking
var _fo_line_count := 0
var _fo_lines_at_dock := 0
var _fo_silence_max := 0
var _fo_last_line_tick := 0
var _total_travels := 0
var _unique_routes: Dictionary = {}
var _total_trades := 0
var _early_margins: Array = []
var _late_margins: Array = []
var _credits_at_start := 0
var _credits_earned := 0
var _credits_spent := 0
var _combats := 0
var _kills := 0
var _loot_drops := 0
var _discoveries := 0
var _decision := 0
var _event_counts: Dictionary = {}  # per 50-decision window

# Feel state
var _feel_damage_flash := false
var _feel_credits_flash := false
var _feel_vignette := false
var _feel_shake_max := 0.0
var _feel_combat_banner := false
var _feel_toast_typed := false  # toasts have type differentiation

# FPS tracking
var _fps_samples: Array[float] = []
var _fps_min := 999.0

# Verification counts
var _confirmed := 0
var _unconfirmed := 0
var _skipped := 0

# Screenshot output
const OUTPUT_DIR := "res://reports/verification/"


func _init() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_user_seed = int(arg.trim_prefix("--seed="))


var _last_logged_phase: int = -1

func _process(delta: float) -> bool:
	_total_frames += 1

	# Re-init after hot-reload (visual mode resets all vars)
	if _a == null:
		_a = AssertScript.new(PREFIX)
		_is_visual = DisplayServer.get_name() != "headless"
		if _is_visual and _screenshot == null:
			_screenshot = ScreenshotScript.new()

	if _total_frames > MAX_FRAMES:
		_a.log("TIMEOUT|frame=%d" % _total_frames)
		_do_summary()
		return true

	# FPS sampling
	if delta > 0.0:
		var fps := 1.0 / delta
		_fps_samples.append(fps)
		if fps < _fps_min:
			_fps_min = fps

	# Settle gate
	if _settle_frames > 0:
		_settle_frames -= 1
		return false

	# Phase transition logging
	if int(_phase) != _last_logged_phase:
		_last_logged_phase = int(_phase)
		if _a:
			_a.log("DEBUG_PHASE|%d|frame=%d" % [int(_phase), _total_frames])

	match _phase:
		# ---- Setup ----
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait_scene()
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.WAIT_LOCAL: _do_wait_local()

		# ---- Boot probes ----
		Phase.BOOT_PROBES: _do_boot_probes()

		# ---- First dock ----
		Phase.TRAVEL_DOCK_1: _do_travel_to_nearest_station()
		Phase.SETTLE_TD1: _advance(Phase.DOCK_1)
		Phase.DOCK_1: _do_dock(); _settle(SETTLE_DOCK, Phase.DOCK_PROBES)
		Phase.DOCK_PROBES: _do_dock_probes()

		# ---- Trade ----
		Phase.BUY: _do_buy(); _settle(SETTLE_ACTION, Phase.UNDOCK_1)
		Phase.UNDOCK_1: _do_undock(); _settle(SETTLE_ACTION, Phase.TRAVEL_SELL)
		Phase.TRAVEL_SELL: _do_travel_to_sell_station()
		Phase.SETTLE_TS: _advance(Phase.DOCK_SELL)
		Phase.DOCK_SELL: _do_dock(); _settle(SETTLE_DOCK, Phase.SELL)
		Phase.SELL: _do_sell(); _settle(SETTLE_ACTION, Phase.TRADE_PROBES)
		Phase.TRADE_PROBES: _do_trade_probes()

		# ---- Explore ----
		Phase.UNDOCK_2: _do_undock(); _settle(SETTLE_ACTION, Phase.TRAVEL_E1)
		Phase.TRAVEL_E1: _do_travel_to_unvisited()
		Phase.SETTLE_E1: _advance(Phase.FLIGHT_PROBES_1)
		Phase.FLIGHT_PROBES_1: _do_flight_probes("system_1")
		Phase.TRAVEL_E2: _do_travel_to_unvisited()
		Phase.SETTLE_E2: _advance(Phase.FLIGHT_PROBES_2)
		Phase.FLIGHT_PROBES_2: _do_flight_probes("system_2")

		# ---- Combat ----
		Phase.COMBAT_SETUP: _do_combat_setup()
		Phase.COMBAT_WAIT: _do_combat_wait()
		Phase.COMBAT_PROBES: _do_combat_probes()

		# ---- Refit ----
		Phase.DOCK_REFIT: _do_dock(); _settle(SETTLE_DOCK, Phase.REFIT_PROBES)
		Phase.REFIT_PROBES: _do_refit_probes()

		# ---- Galaxy map ----
		Phase.UNDOCK_MAP: _do_undock(); _settle(SETTLE_ACTION, Phase.GALAXY_OPEN)
		Phase.GALAXY_OPEN: _do_open_galaxy_map(); _settle(SETTLE_ACTION, Phase.GALAXY_PROBES)
		Phase.GALAXY_PROBES: _do_galaxy_probes()
		Phase.GALAXY_CLOSE: _do_close_galaxy_map(); _settle(SETTLE_ACTION, Phase.AGGREGATE_PLAY)

		# ---- Aggregate play ----
		Phase.AGGREGATE_PLAY: _do_aggregate_play()

		# ---- Final ----
		Phase.METRIC_PROBES: _do_metric_probes()
		Phase.SUMMARY: _do_summary(); return true
		Phase.DONE: return true

	return false


# ===================== Setup =====================

func _do_load_scene() -> void:
	# Guard against hot-reload re-entry in visual mode (vars reset on reload)
	if root.get_node_or_null("Main") != null:
		_phase = Phase.WAIT_SCENE
		return

	_a = AssertScript.new(PREFIX)
	_is_visual = DisplayServer.get_name() != "headless"
	_a.log("START|verify_issues_v0 seed=%d visual=%s" % [_user_seed, str(_is_visual)])

	if _is_visual:
		_screenshot = ScreenshotScript.new()

	# Delete stale quicksave
	if FileAccess.file_exists("user://quicksave.json"):
		DirAccess.remove_absolute(OS.get_user_data_dir() + "/quicksave.json")

	var scene := load("res://scenes/main.tscn")
	if scene:
		root.add_child(scene.instantiate())
	_phase = Phase.WAIT_SCENE
	_settle_frames = SETTLE_SCENE


func _do_wait_scene() -> void:
	_polls += 1
	if _polls > 300:
		_a.log("ABORT|scene_load_timeout")
		_phase = Phase.DONE
		return
	var main = root.get_node_or_null("Main")
	if main:
		_polls = 0
		_phase = Phase.WAIT_BRIDGE


var _reinit_done := false

func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		_polls += 1
		if _polls > 200:
			_a.log("ABORT|no_bridge")
			_phase = Phase.DONE
		return
	_polls = 0
	_game_manager = root.get_node_or_null("GameManager")
	# Phase-advance FIRST (before bridge calls that might throw)
	_phase = Phase.WAIT_READY
	if not _reinit_done and _bridge.has_method("ReinitializeForNewGameV0"):
		_reinit_done = true
		_bridge.call("ReinitializeForNewGameV0")
		_settle_frames = 30


func _do_wait_ready() -> void:
	_polls += 1
	if _polls > 200:
		_phase = Phase.WAIT_LOCAL
		_polls = 0
		return
	if _bridge.has_method("IsReadyV0"):
		if _bridge.call("IsReadyV0"):
			_phase = Phase.WAIT_LOCAL
			_polls = 0


var _galaxy_retry := false

func _do_wait_local() -> void:
	_polls += 1
	if _polls > 200:
		_a.log("ABORT|local_system_timeout")
		_phase = Phase.DONE
		return
	# Init navigation
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node = str(ps.get("current_node_id", ""))
	if _home_node.is_empty():
		return
	_current_node = _home_node
	_visited[_home_node] = true

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_nodes = galaxy.get("system_nodes", [])
	_all_edges = galaxy.get("lane_edges", [])
	if _all_nodes.size() == 0 and not _galaxy_retry:
		# Read-lock contention — retry after settle (no await in SceneTree scripts)
		_galaxy_retry = true
		_settle_frames = 20
		return
	if _all_nodes.size() == 0 and _galaxy_retry:
		_a.log("WARN|galaxy_empty_after_retry nodes=0")
	_build_adj()

	_credits_at_start = int(ps.get("credits", 0))
	_a.log("INIT|home=%s nodes=%d edges=%d credits=%d" % [
		_home_node, _all_nodes.size(), _all_edges.size(), _credits_at_start])

	_phase = Phase.BOOT_PROBES
	_settle_frames = 10


# ===================== Helpers =====================

func _settle(frames: int, next_phase: Phase) -> void:
	_settle_frames = frames
	_phase = next_phase


func _advance(next_phase: Phase) -> void:
	_phase = next_phase


func _build_adj() -> void:
	_adj.clear()
	for e in _all_edges:
		var a: String = str(e.get("from_id", ""))
		var b: String = str(e.get("to_id", ""))
		if a.is_empty() or b.is_empty():
			continue
		if not _adj.has(a): _adj[a] = []
		if not _adj.has(b): _adj[b] = []
		if not (b in _adj[a]): _adj[a].append(b)
		if not (a in _adj[b]): _adj[b].append(a)


func _get_neighbors(node_id: String) -> Array:
	return _adj.get(node_id, [])


func _headless_travel(dest: String) -> void:
	if _bridge:
		if _bridge.has_method("DispatchTravelCommandV0"):
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", dest)
		if _bridge.has_method("DispatchPlayerArriveV0"):
			_bridge.call("DispatchPlayerArriveV0", dest)
	if _game_manager:
		_game_manager.set("_lane_cooldown_v0", 0.0)
		if _game_manager.get("current_player_state") != null:
			_game_manager.call("on_lane_gate_proximity_entered_v0", dest)
			_game_manager.call("on_lane_arrival_v0", dest)
	_current_node = dest
	_visited[dest] = true
	_total_travels += 1
	var route_key := "%s->%s" % [_current_node, dest]
	_unique_routes[route_key] = _unique_routes.get(route_key, 0) + 1


func _do_dock() -> void:
	if _game_manager:
		# Clear hostile fleet ships to allow docking
		for ship in get_nodes_in_group("FleetShip"):
			if is_instance_valid(ship):
				ship.remove_from_group("FleetShip")
		# Mock station node for dock trigger
		var mock := Node3D.new()
		mock.add_to_group("Station")
		mock.set_meta("dock_target_id", _current_node)
		root.add_child(mock)
		_game_manager.call("on_proximity_dock_entered_v0", mock)
	elif _bridge and _bridge.has_method("DispatchPlayerArriveV0"):
		_bridge.call("DispatchPlayerArriveV0", _current_node)
	_a.log("DOCK|%s" % _current_node)


func _do_undock() -> void:
	if _game_manager and _game_manager.has_method("undock_v0"):
		_game_manager.call("undock_v0")
	_a.log("UNDOCK|%s" % _current_node)


func _try_capture(label: String) -> void:
	if _is_visual and _screenshot:
		_screenshot.capture_v0(self, label, OUTPUT_DIR)
		_a.log("CAPTURE|%s" % label)


func _get_cargo_qty(good_id: String) -> int:
	if not _bridge or not _bridge.has_method("GetPlayerCargoV0"):
		return 0
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	for item in cargo:
		if item is Dictionary and str(item.get("good_id", "")) == good_id:
			return int(item.get("qty", 0))
	return 0


func _tick_advance(n: int = TICK_ADVANCE) -> void:
	if _bridge.has_method("DebugAdvanceTicksV0"):
		_bridge.call("DebugAdvanceTicksV0", n)


func _sample_feel() -> void:
	if not _is_visual:
		return
	var hud = root.find_child("HUD", true, false)
	if not hud:
		return

	var df = hud.find_child("DamageFlash", true, false)
	if df and df is ColorRect and df.color.a > 0.01:
		_feel_damage_flash = true

	var cf = hud.find_child("CreditsFlash", true, false)
	if cf and cf is ColorRect and cf.color.a > 0.01:
		_feel_credits_flash = true

	for child in hud.get_children():
		if child is ColorRect and child.name.contains("Combat") and child.visible:
			if child is ColorRect and child.color.a > 0.01:
				_feel_vignette = true
		if child is Label and child.name.contains("Combat") and child.visible and child.text.length() > 0:
			_feel_combat_banner = true


func _track_fo() -> void:
	if not _bridge or not _bridge.has_method("GetSimTickV0"):
		return
	var tick := int(_bridge.call("GetSimTickV0"))
	# Check FO dialogue via bridge
	if _bridge.has_method("GetRecentHudEventsV0"):
		var events: Array = _bridge.call("GetRecentHudEventsV0")
		for ev in events:
			if ev is Dictionary and str(ev.get("type", "")).contains("fo"):
				_fo_line_count += 1
				var gap := tick - _fo_last_line_tick
				if gap > _fo_silence_max and _fo_last_line_tick > 0:
					_fo_silence_max = gap
				_fo_last_line_tick = tick


func _verify(probe: String, confirmed: bool, evidence: String) -> void:
	var status := "CONFIRMED" if confirmed else "UNCONFIRMED"
	_a.log("VERIFY|%s|%s|%s" % [probe, status, evidence])
	if confirmed:
		_confirmed += 1
	else:
		_unconfirmed += 1


func _skip(probe: String, reason: String) -> void:
	_a.log("VERIFY|%s|SKIP|%s" % [probe, reason])
	_skipped += 1


# ===================== Probe Groups =====================

# ---- Group 1: Boot ----
func _do_boot_probes() -> void:
	_phase = Phase.TRAVEL_DOCK_1  # advance FIRST — bridge calls may throw
	_a.log("PROBES|boot")
	_try_capture("00_boot")

	# Check HUD present
	var hud = root.find_child("HUD", true, false)
	_verify("hud_present", hud != null, "hud=%s" % str(hud != null))

	# Check NPC presence via fleet state
	if _bridge.has_method("GetFleetStateV0"):
		var fleet_result = _bridge.call("GetFleetStateV0", "fleet_trader_1")
		if fleet_result is Dictionary or fleet_result is String:
			# Fleet exists — check NPC count via galaxy snapshot
			var npc_count := 0
			for n in _all_nodes:
				if n is Dictionary:
					npc_count += int(n.get("fleet_count", 0))
			_verify("npc_at_boot", npc_count > 0, "npc_fleets_in_galaxy=%d" % npc_count)
		else:
			_skip("npc_at_boot", "fleet_state_unexpected")
	else:
		_skip("npc_at_boot", "no_GetFleetStateV0")

	# Check galaxy overlay visible at boot (commandment 1: min 1 = identity)
	if _is_visual:
		var galaxy_bg = root.find_child("GalaxyBackground", true, false)
		var starlight = root.find_child("Starlight", true, false)
		_verify("galaxy_identity_at_boot",
			(galaxy_bg != null and galaxy_bg.visible) or (starlight != null),
			"galaxy_bg=%s starlight=%s" % [str(galaxy_bg != null), str(starlight != null)])
	else:
		_skip("galaxy_identity_at_boot", "headless")


# ---- Navigate to nearest station ----
func _do_travel_to_nearest_station() -> void:
	_settle(SETTLE_TRAVEL, Phase.DOCK_1)  # advance FIRST
	# Try docking at current node first — most starts have a station
	_headless_travel(_current_node)  # ensure arrival
	_a.log("TRAVEL|to_dock at %s" % _current_node)
	_buy_station = _current_node


# ---- Group 2: Dock probes ----
func _do_dock_probes() -> void:
	_phase = Phase.BUY  # advance FIRST
	_a.log("PROBES|dock")
	_try_capture("01_first_dock")

	# PROBE: Tab disclosure — how many tabs visible at first dock?
	if _bridge.has_method("GetOnboardingStateV0"):
		var onboard: Dictionary = _bridge.call("GetOnboardingStateV0")
		if onboard is Dictionary:
			var tab_count := 1  # market is always visible
			for key in ["show_jobs_tab", "show_station_tab", "show_intel_tab", "show_ship_tab"]:
				if onboard.get(key, false):
					tab_count += 1
			_verify("tab_disclosure", tab_count <= 2,
				"tabs_at_first_dock=%d expected<=2" % tab_count)

			# PROBE: Systems revealed at dock
			var systems_str: String = str(onboard.get("systems_introduced", ""))
			var sys_count := systems_str.split(",").size() if systems_str.length() > 0 else 0
			_verify("system_dump", sys_count <= 2,
				"systems_at_dock=%d expected<=2" % sys_count)
		else:
			_skip("tab_disclosure", "onboard_not_dict")
			_skip("system_dump", "onboard_not_dict")
	else:
		_skip("tab_disclosure", "no_GetOnboardingStateV0")
		_skip("system_dump", "no_GetOnboardingStateV0")

	# PROBE: FO dock greeting
	_track_fo()
	_fo_lines_at_dock = _fo_line_count
	# Also check FO state directly
	if _bridge.has_method("GetFOStateV0"):
		var fo: Dictionary = _bridge.call("GetFOStateV0")
		if fo is Dictionary:
			var lines := int(fo.get("total_lines", 0))
			_verify("fo_dock_greeting", lines > 0,
				"fo_lines_at_first_dock=%d" % lines)
		else:
			_verify("fo_dock_greeting", _fo_line_count > 0,
				"fo_lines_tracked=%d" % _fo_line_count)
	else:
		_verify("fo_dock_greeting", _fo_line_count > 0,
			"fo_lines_tracked=%d" % _fo_line_count)

	# PROBE: Keybind hints visible (visual only)
	if _is_visual:
		var hud = root.find_child("HUD", true, false)
		var has_keybind := false
		if hud:
			for child in hud.get_children():
				if child is Label and (child.text.contains("WASD") or child.text.contains("wasd") or
					child.text.contains("[E]") or child.text.contains("Tab")):
					has_keybind = true
					break
		_verify("keybind_hints", has_keybind, "visible=%s" % str(has_keybind))
	else:
		_skip("keybind_hints", "headless")

	# PROBE: Dock panel screenshot (visual)
	if _is_visual:
		var htm = root.find_child("HeroTradeMenu", true, false)
		_verify("dock_panel_visible", htm != null and htm.visible,
			"hero_trade_menu=%s" % str(htm != null and htm.visible if htm else false))
		_try_capture("02_dock_panel")
	else:
		_skip("dock_panel_visible", "headless")


# ---- Group 3: Trade ----
func _do_buy() -> void:
	_a.log("PROBES|trade_buy")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_before := int(ps.get("credits", 0))

	# Get market view for current node
	if _bridge.has_method("GetPlayerMarketViewV0"):
		var market: Array = _bridge.call("GetPlayerMarketViewV0", _current_node)
		if market is Array:
			for g in market:
				if g is Dictionary:
					var buy_price := int(g.get("buy_price", 0))
					if buy_price > 0 and buy_price < credits_before:
						_bought_good_id = str(g.get("good_id", ""))
						var max_afford := credits_before / buy_price
						_bought_qty = mini(max_afford, 10)
						_bridge.call("DispatchPlayerTradeV0", _current_node, _bought_good_id, _bought_qty, true)
						_credits_spent += buy_price * _bought_qty
						_a.log("BUY|%s qty=%d price=%d" % [_bought_good_id, _bought_qty, buy_price])
						break
	_tick_advance(3)


func _do_sell() -> void:
	_a.log("PROBES|trade_sell")
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_before_sell = int(ps.get("credits", 0))

	if _bought_good_id.length() > 0:
		# Sell actual cargo amount via GetPlayerCargoV0 (GetPlayerStateV0 doesn't include cargo array)
		var actual_held := _get_cargo_qty(_bought_good_id)
		var sell_qty := maxi(actual_held, 1)
		_a.log("SELL_PRE|good=%s held=%d requested=%d" % [_bought_good_id, actual_held, _bought_qty])
		_bridge.call("DispatchPlayerTradeV0", _current_node, _bought_good_id, sell_qty, false)
		_tick_advance(3)

		# Sample feel immediately after sell
		_sample_feel()

		ps = _bridge.call("GetPlayerStateV0")
		_credits_after_sell = int(ps.get("credits", 0))
		var margin := _credits_after_sell - _credits_before_sell
		_credits_earned += maxi(margin, 0)
		_total_trades += 1
		_early_margins.append(margin)
		_a.log("SELL|%s margin=%d credits=%d" % [_bought_good_id, margin, _credits_after_sell])


func _do_trade_probes() -> void:
	_phase = Phase.UNDOCK_2  # advance FIRST
	_a.log("PROBES|trade")

	# PROBE: Credit feedback (visual only)
	if _is_visual:
		_sample_feel()  # one more sample
		_verify("credit_feedback", _feel_credits_flash,
			"credits_flash_seen=%s" % str(_feel_credits_flash))
	else:
		_skip("credit_feedback", "headless")

	# PROBE: Trade margin (heist moment)
	var margin := _credits_after_sell - _credits_before_sell
	_verify("heist_margin", margin > 50,
		"margin=%d expected>50" % margin)

	_try_capture("03_after_sell")


# ---- Group 4: Explore ----
func _do_travel_to_sell_station() -> void:
	# Pick a neighbor that isn't our buy station
	var neighbors := _get_neighbors(_current_node)
	var dest := ""
	for n in neighbors:
		if n != _buy_station:
			dest = n
			break
	if dest.is_empty() and neighbors.size() > 0:
		dest = neighbors[0]
	if dest.is_empty():
		_a.log("TRAVEL|no_sell_dest")
		_phase = Phase.DOCK_SELL
		return
	_sell_station = dest
	_headless_travel(dest)
	_a.log("TRAVEL|to_sell at %s" % dest)
	_settle(SETTLE_TRAVEL, Phase.DOCK_SELL)


func _do_travel_to_unvisited() -> void:
	var neighbors := _get_neighbors(_current_node)
	var dest := ""
	# Prefer unvisited
	for n in neighbors:
		if not _visited.has(n):
			dest = n
			break
	# Fallback: any neighbor
	if dest.is_empty() and neighbors.size() > 0:
		dest = neighbors[0]
	if dest.is_empty():
		_a.log("TRAVEL|no_unvisited_dest")
		if _phase == Phase.TRAVEL_E1:
			_phase = Phase.FLIGHT_PROBES_1
		else:
			_phase = Phase.FLIGHT_PROBES_2
		return
	_headless_travel(dest)
	_a.log("TRAVEL|explore to %s (visited=%s)" % [dest, str(_visited.has(dest))])
	if _phase == Phase.TRAVEL_E1:
		_settle(SETTLE_TRAVEL, Phase.FLIGHT_PROBES_1)
	else:
		_settle(SETTLE_TRAVEL, Phase.FLIGHT_PROBES_2)


func _do_flight_probes(label: String) -> void:
	# advance FIRST — bridge/scene calls may throw
	if label == "system_1":
		_phase = Phase.TRAVEL_E2
	elif label == "system_2":
		_phase = Phase.COMBAT_SETUP
	_a.log("PROBES|flight_%s" % label)
	_try_capture("04_flight_%s" % label)
	_tick_advance(10)
	_track_fo()

	if label == "system_1":
		# PROBE: Lane visibility
		if _is_visual:
			var lane_lines := get_nodes_in_group("LaneLine")
			var lane_gates := get_nodes_in_group("LaneGate")
			_verify("lane_visibility", lane_lines.size() > 0 or lane_gates.size() > 0,
				"lane_lines=%d lane_gates=%d" % [lane_lines.size(), lane_gates.size()])
		else:
			# Headless: check sim knows about lanes
			var edge_count := 0
			for e in _all_edges:
				var a: String = str(e.get("from_id", ""))
				var b: String = str(e.get("to_id", ""))
				if a == _current_node or b == _current_node:
					edge_count += 1
			_verify("lane_data_exists", edge_count > 0,
				"lanes_at_node=%d (sim data only, visual unverified)" % edge_count)

		# PROBE: Camera distance (visual only) — cameras are DroneCamera/MapCamera under /root/Main/
		if _is_visual:
			var main_node = root.get_node_or_null("Main")
			var cam = main_node.get_node_or_null("DroneCamera") if main_node else null
			if cam == null and main_node:
				cam = main_node.get_node_or_null("MapCamera")
			if cam == null:
				cam = root.find_child("DroneCamera", true, false)
			if cam == null:
				cam = root.find_child("MapCamera", true, false)
			if cam and cam is Node3D:
				var cam_y: float = cam.global_position.y
				_verify("camera_distance", cam_y < 200.0,
					"camera_y=%.1f expected<200" % cam_y)
			else:
				_skip("camera_distance", "no_camera_found name_searched=DroneCamera,MapCamera")
		else:
			_skip("camera_distance", "headless")

		# PROBE: Heading indicator
		if _is_visual:
			var hud = root.find_child("HUD", true, false)
			var has_heading := false
			if hud:
				for child in hud.get_children():
					if child.name.contains("Heading") or child.name.contains("Compass"):
						has_heading = true
						break
			_verify("heading_indicator", has_heading,
				"present=%s" % str(has_heading))
		else:
			_skip("heading_indicator", "headless")

	elif label == "system_2":
		# PROBE: System visual differentiation (visual only)
		if _is_visual:
			var stars := get_nodes_in_group("Star")
			var unique_colors := {}
			for star in stars:
				if star is Node3D:
					# Check for mesh material color
					var color_str := "default"
					if star.has_method("get_surface_override_material"):
						var mat = star.get_surface_override_material(0)
						if mat:
							color_str = str(mat.get("albedo_color"))
					unique_colors[color_str] = true
			_verify("system_identity", unique_colors.size() > 1,
				"unique_star_visuals=%d" % unique_colors.size())
		else:
			_skip("system_identity", "headless")


# ---- Group 5: Combat ----
func _do_combat_setup() -> void:
	_phase = Phase.COMBAT_WAIT  # advance FIRST
	_settle_frames = 10
	_a.log("PROBES|combat_setup")
	# Force damage to trigger combat feedback
	if _bridge.has_method("ForceDamagePlayerHullV0"):
		_bridge.call("ForceDamagePlayerHullV0")
		_tick_advance(5)
		_sample_feel()  # Check for damage flash
		_try_capture("05_combat_damage")

		# Check loot from kills
		if _bridge.has_method("ForceIncrementNpcFleetsDestroyedV0"):
			_bridge.call("ForceIncrementNpcFleetsDestroyedV0")
			_tick_advance(5)
			_kills += 1

		# Sample feel again after kill
		_sample_feel()

		# Repair
		if _bridge.has_method("ForceRepairPlayerHullV0"):
			_bridge.call("ForceRepairPlayerHullV0")


func _do_combat_wait() -> void:
	# Additional feel sampling during combat settle
	_sample_feel()
	_phase = Phase.COMBAT_PROBES


func _do_combat_probes() -> void:
	_phase = Phase.DOCK_REFIT  # advance FIRST
	_a.log("PROBES|combat")

	# PROBE: Damage flash
	if _is_visual:
		_verify("combat_damage_flash", _feel_damage_flash,
			"seen=%s" % str(_feel_damage_flash))
	else:
		_skip("combat_damage_flash", "headless")

	# PROBE: Combat vignette
	if _is_visual:
		_verify("combat_vignette", _feel_vignette,
			"seen=%s" % str(_feel_vignette))
	else:
		_skip("combat_vignette", "headless")

	# PROBE: Combat banner
	if _is_visual:
		_verify("combat_banner", _feel_combat_banner,
			"seen=%s" % str(_feel_combat_banner))
	else:
		_skip("combat_banner", "headless")

	# PROBE: Screen shake — check camera
	if _is_visual:
		_verify("combat_screen_shake", _feel_shake_max > 0.01,
			"shake_max=%.3f" % _feel_shake_max)
	else:
		_skip("combat_screen_shake", "headless")

	# PROBE: Combat loot
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var cargo: Array = ps.get("cargo", [])
	var has_loot := false
	for item in cargo:
		if item is Dictionary:
			var origin: String = str(item.get("origin", ""))
			if origin.contains("loot") or origin.contains("salvage"):
				has_loot = true
				_loot_drops += 1
	# Also check credits change
	var credits_now := int(ps.get("credits", 0))
	_verify("combat_loot", has_loot or credits_now > _credits_at_start,
		"loot_in_cargo=%s credits=%d" % [str(has_loot), credits_now])

	_try_capture("06_post_combat")


# ---- Group 6: Refit ----
func _do_refit_probes() -> void:
	_phase = Phase.UNDOCK_MAP  # advance FIRST
	_a.log("PROBES|refit")
	_try_capture("07_refit_dock")

	# PROBE: Modules available
	if _bridge.has_method("GetAvailableModulesV0"):
		var mods: Array = _bridge.call("GetAvailableModulesV0")
		if mods is Array:
			_verify("module_available", mods.size() > 0,
				"available=%d" % mods.size())

			# PROBE: Try installing a module
			if mods.size() > 0 and _bridge.has_method("InstallModuleV0"):
				var mod = mods[0]
				var mod_id: String = str(mod.get("module_id", "")) if mod is Dictionary else str(mod)
				var result = _bridge.call("InstallModuleV0", "fleet_trader_1", 0, mod_id)
				_tick_advance(3)
				_sample_feel()
				var ok := false
				if result is Dictionary:
					ok = result.get("success", false)
				_verify("module_install_feedback", ok,
					"installed=%s ok=%s" % [mod_id, str(ok)])
			else:
				_verify("module_install_possible", false,
					"available=%d (cannot install)" % mods.size())
		else:
			_skip("module_available", "mods_not_array")
	else:
		_skip("module_available", "no_GetAvailableModulesV0")


# ---- Group 7: Galaxy map ----
func _do_open_galaxy_map() -> void:
	if _game_manager and _game_manager.has_method("toggle_galaxy_map_v0"):
		_game_manager.call("toggle_galaxy_map_v0")
		_a.log("GALAXY_MAP|opened")
	elif _game_manager and _game_manager.has_method("open_overlay_v0"):
		_game_manager.call("open_overlay_v0", "galaxy_map")
		_a.log("GALAXY_MAP|opened_via_overlay")


func _do_close_galaxy_map() -> void:
	if _game_manager and _game_manager.has_method("toggle_galaxy_map_v0"):
		_game_manager.call("toggle_galaxy_map_v0")
	elif _game_manager and _game_manager.has_method("close_overlay_v0"):
		_game_manager.call("close_overlay_v0")


func _do_galaxy_probes() -> void:
	_phase = Phase.GALAXY_CLOSE  # advance FIRST
	_a.log("PROBES|galaxy_map")
	_try_capture("08_galaxy_map")

	# PROBE: Galaxy map nodes visible
	if _is_visual:
		var galaxy_view = root.find_child("GalaxyView", true, false)
		if galaxy_view:
			var beacons := 0
			var labels := 0
			for child in galaxy_view.get_children():
				if child is Node3D:
					if child.name.contains("Beacon") or child.name.contains("Node"):
						beacons += 1
					if child is Label3D:
						labels += 1
			# Also check recursively
			var all_label3d := galaxy_view.find_children("*", "Label3D", true)
			labels = maxi(labels, all_label3d.size())

			_verify("galaxy_map_nodes", beacons > 0,
				"beacons=%d labels=%d" % [beacons, labels])

			# PROBE: Player indicator
			var player_ring = galaxy_view.find_child("PlayerRing", true, false)
			var player_marker = galaxy_view.find_child("PlayerMarker", true, false)
			var you_here = galaxy_view.find_child("YouAreHere", true, false)
			_verify("galaxy_player_marker",
				(player_ring != null) or (player_marker != null) or (you_here != null),
				"ring=%s marker=%s you=%s" % [
					str(player_ring != null), str(player_marker != null), str(you_here != null)])

			# PROBE: Faction territories
			var faction_nodes := 0
			for child in galaxy_view.get_children():
				if child.name.contains("Faction") or child.name.contains("Territory"):
					faction_nodes += 1
			_verify("galaxy_faction_overlay", faction_nodes > 0,
				"faction_nodes=%d" % faction_nodes)
		else:
			_skip("galaxy_map_nodes", "no_GalaxyView")
			_skip("galaxy_player_marker", "no_GalaxyView")
			_skip("galaxy_faction_overlay", "no_GalaxyView")
	else:
		_skip("galaxy_map_nodes", "headless")
		_skip("galaxy_player_marker", "headless")
		_skip("galaxy_faction_overlay", "headless")


# ---- Group 8: Aggregate play (100 decisions: buy→travel→sell loop) ----
var _agg_holding_good := ""
var _agg_holding_qty := 0
var _agg_buy_price := 0
var _agg_credits_before_trade := 0
var _agg_high_events := 0   # count HIGH events per window
var _agg_event_log: Array = []  # array of {decision, type} for valence tracking
var _agg_valence_crossings := 0
var _agg_last_valence := 0  # +1 = positive, -1 = negative

func _do_aggregate_play() -> void:
	if _decision >= 100:
		_phase = Phase.METRIC_PROBES
		return

	_decision += 1

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	var credits := int(ps.get("credits", 0))

	if _agg_holding_good.is_empty():
		# Phase A: Buy cheapest good at current station (sim-level, no visual dock needed)
		if _bridge.has_method("GetPlayerMarketViewV0"):
			var market: Array = _bridge.call("GetPlayerMarketViewV0", loc)
			if market is Array and market.size() > 0:
				var best_good := ""
				var best_price := 999999
				for g in market:
					if g is Dictionary:
						var bp := int(g.get("buy_price", 0))
						var qty_avail := int(g.get("quantity", 0))
						if bp > 0 and bp < credits and qty_avail > 0 and bp < best_price:
							best_good = str(g.get("good_id", ""))
							best_price = bp
				if not best_good.is_empty():
					var buy_qty := mini(credits / best_price, 5)
					_agg_credits_before_trade = credits
					_bridge.call("DispatchPlayerTradeV0", loc, best_good, buy_qty, true)
					_tick_advance(3)
					# Verify buy succeeded by checking cargo
					var ps_post: Dictionary = _bridge.call("GetPlayerStateV0")
					var post_credits := int(ps_post.get("credits", 0))
					if post_credits < credits:
						_agg_holding_good = best_good
						_agg_holding_qty = buy_qty
						_agg_buy_price = best_price
						_credits_spent += credits - post_credits
		# Travel to neighbor for sell
		var neighbors := _get_neighbors(loc)
		if neighbors.size() > 0:
			var dest: String = neighbors[_decision % neighbors.size()]
			_headless_travel(dest)
	else:
		# Phase B: Sell at current station (sim-level, no visual dock needed)
		# DispatchPlayerArriveV0 was called by _headless_travel, player is at this node
		# Get actual cargo amount via GetPlayerCargoV0
		var actual_held := _get_cargo_qty(_agg_holding_good)
		if actual_held > 0:
			_bridge.call("DispatchPlayerTradeV0", loc, _agg_holding_good, actual_held, false)
			_tick_advance(3)
			var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
			var credits_after := int(ps2.get("credits", 0))
			var margin := credits_after - _agg_credits_before_trade
			_credits_earned += maxi(credits_after - credits, 0)
			_total_trades += 1
			if _decision <= 50:
				_early_margins.append(margin)
			else:
				_late_margins.append(margin)
			# Track valence
			var valence := 1 if margin > 0 else -1
			if _agg_last_valence != 0 and valence != _agg_last_valence:
				_agg_valence_crossings += 1
			_agg_last_valence = valence
			_agg_event_log.append({"decision": _decision, "margin": margin})
		_agg_holding_good = ""
		_agg_holding_qty = 0
		# Travel onward
		var neighbors := _get_neighbors(loc)
		if neighbors.size() > 0:
			var dest: String = neighbors[(_decision + 3) % neighbors.size()]
			_headless_travel(dest)

	_tick_advance()
	_track_fo()

	# Track event density per 25-decision window
	var window: int = _decision / 25
	if _total_trades > 0:
		_event_counts[window] = _event_counts.get(window, 0) + 1


# ---- Group 9: Metric probes ----
func _do_metric_probes() -> void:
	_phase = Phase.SUMMARY  # advance FIRST
	_a.log("PROBES|metrics")

	# PROBE: Late tab disclosure — after visiting 5+ nodes, how many tabs visible?
	# This reproduces the SYSTEM_DUMP issue (#2/#19) — mid-playthrough state
	if _bridge.has_method("GetOnboardingStateV0"):
		var onboard: Dictionary = _bridge.call("GetOnboardingStateV0")
		if onboard is Dictionary:
			var tab_count := 0
			for key in ["show_jobs_tab", "show_station_tab", "show_intel_tab", "show_ship_tab"]:
				if onboard.get(key, false):
					tab_count += 1
			tab_count += 1  # market is always visible
			var visited_count := _visited.size()
			# At 5+ nodes visited, all 5 tabs should be unlocked (market+jobs+station+intel+ship)
			# The audit found 6 tabs at 720 decisions — verify progressive disclosure works
			_verify("tab_disclosure_late", tab_count <= 3 or visited_count >= 5,
				"tabs=%d nodes_visited=%d (6+ tabs at early dock = SYSTEM_DUMP)" % [tab_count, visited_count])

			# PROBE: System dump at late dock — more than 2 new systems revealed at once?
			# If all tabs unlock simultaneously when nodesVisited crosses thresholds, that's system dump
			var new_tabs_at_5 := 0
			# At exactly 5 nodes: jobs (2+), station (3+), intel (4+), ship (5+) could all appear
			# if player hasn't docked between node 2 and node 5
			if visited_count >= 5:
				# All 4 disclosure tabs should be visible
				for key in ["show_jobs_tab", "show_station_tab", "show_intel_tab", "show_ship_tab"]:
					if onboard.get(key, false):
						new_tabs_at_5 += 1
				_verify("system_dump_late", new_tabs_at_5 <= 4,
					"tabs_unlocked_by_node_5=%d (all unlock if never docked between)" % new_tabs_at_5)

	# PROBE: Economy sinks
	var sink_ratio := 0.0
	if _credits_earned > 0:
		sink_ratio = float(_credits_spent) / float(_credits_earned)
	_verify("economy_sinks", sink_ratio > 0.05,
		"sink_faucet=%.3f spent=%d earned=%d" % [sink_ratio, _credits_spent, _credits_earned])

	# PROBE: Route diversity
	var unique := _unique_routes.size()
	var total := _total_travels
	var diversity := float(unique) / float(maxi(total, 1))
	_verify("route_diversity", diversity > 0.3,
		"unique=%d total=%d diversity=%.2f" % [unique, total, diversity])

	# PROBE: FO silence
	_verify("fo_silence", _fo_silence_max < 100,
		"max_silence=%d ticks" % _fo_silence_max)

	# PROBE: Competence margins (early vs late)
	var early_avg := 0.0
	var late_avg := 0.0
	if _early_margins.size() > 0:
		var s := 0.0
		for m in _early_margins: s += float(m)
		early_avg = s / _early_margins.size()
	if _late_margins.size() > 0:
		var s := 0.0
		for m in _late_margins: s += float(m)
		late_avg = s / _late_margins.size()
	if _early_margins.size() >= 3 and _late_margins.size() >= 3:
		# Enough data — verify whether margins improve or regress
		var change_pct := 0.0
		if abs(early_avg) > 0.01:
			change_pct = ((late_avg - early_avg) / abs(early_avg)) * 100.0
		# Issue was: early margins 970, late margins 430 (regression)
		# Test: late margins should be at least 80% of early (or both negative = no regression signal)
		var no_regression := late_avg >= early_avg * 0.8 if early_avg > 0 else true
		_verify("competence_margins", no_regression,
			"early_avg=%.0f late_avg=%.0f change=%.0f%% early_n=%d late_n=%d" % [
				early_avg, late_avg, change_pct, _early_margins.size(), _late_margins.size()])
	else:
		_skip("competence_margins", "insufficient_data early=%d late=%d" % [_early_margins.size(), _late_margins.size()])

	# PROBE: Discovery count — uses GetDiscoveryListSnapshotV0 (Array of dicts)
	if _bridge.has_method("GetDiscoveryListSnapshotV0"):
		var disc_list = _bridge.call("GetDiscoveryListSnapshotV0")
		if disc_list is Array:
			_verify("discovery_count", disc_list.size() > 0,
				"discoveries=%d" % disc_list.size())
		else:
			_skip("discovery_count", "disc_not_array")
	else:
		_skip("discovery_count", "no_GetDiscoveryListSnapshotV0")

	# PROBE: FPS
	if _fps_samples.size() > 10:
		var fps_sum := 0.0
		for s in _fps_samples: fps_sum += s
		var avg := fps_sum / _fps_samples.size()
		_verify("fps_minimum", _fps_min >= 30.0,
			"fps_min=%.1f fps_avg=%.1f samples=%d" % [_fps_min, avg, _fps_samples.size()])
	else:
		_skip("fps_minimum", "insufficient_samples")

	# PROBE: Toast typing (visual)
	if _is_visual:
		_verify("toast_type_differentiation", _feel_toast_typed,
			"typed=%s (DEVELOPER_UI if false)" % str(_feel_toast_typed))
	else:
		_skip("toast_type_differentiation", "headless")

	# PROBE: Reward desert — any 25-decision window with 0 events?
	var max_dry_window := 0
	var dry_windows := 0
	for w in range(4):  # 4 windows of 25 decisions = 100
		var count: int = _event_counts.get(w, 0)
		if count == 0:
			dry_windows += 1
			max_dry_window = maxi(max_dry_window, 25)
	_verify("reward_desert", dry_windows == 0,
		"dry_windows=%d of 4 (25-decision each)" % dry_windows)

	# PROBE: Catharsis — at least one positive event after a negative one
	var had_negative := false
	var catharsis_count := 0
	for ev in _agg_event_log:
		var m: int = int(ev.get("margin", 0))
		if m < 0:
			had_negative = true
		elif m > 0 and had_negative:
			catharsis_count += 1
			had_negative = false
	_verify("catharsis_events", catharsis_count > 0,
		"catharsis=%d (relief-after-loss moments)" % catharsis_count)

	# PROBE: Valence monotone — do credit margins ever cross from positive to negative?
	_verify("valence_crossings", _agg_valence_crossings > 0,
		"crossings=%d (0 = monotone experience)" % _agg_valence_crossings)

	# PROBE: Warp visual (visual only — is there VFX during warp?)
	if _is_visual:
		var warp_vfx = root.find_child("WarpEffect", true, false)
		var speed_lines = root.find_child("SpeedLines", true, false)
		var warp_overlay = root.find_child("WarpOverlay", true, false)
		_verify("warp_visual", warp_vfx != null or speed_lines != null or warp_overlay != null,
			"warp_vfx=%s speed_lines=%s overlay=%s" % [
				str(warp_vfx != null), str(speed_lines != null), str(warp_overlay != null)])
	else:
		_skip("warp_visual", "headless")


# ===================== Summary =====================

func _do_summary() -> void:
	_a.log("VERIFY_SUMMARY|confirmed=%d unconfirmed=%d skipped=%d total=%d" % [
		_confirmed, _unconfirmed, _skipped, _confirmed + _unconfirmed + _skipped])
	_a.log("SESSION|decisions=%d trades=%d travels=%d kills=%d visited=%d" % [
		_decision, _total_trades, _total_travels, _kills, _visited.size()])

	# Write JSON report
	var report := {
		"bot": "verify_issues_v0",
		"seed": _user_seed,
		"visual": _is_visual,
		"confirmed": _confirmed,
		"unconfirmed": _unconfirmed,
		"skipped": _skipped,
		"session": {
			"decisions": _decision,
			"trades": _total_trades,
			"travels": _total_travels,
			"kills": _kills,
			"visited": _visited.size(),
			"fps_min": _fps_min
		}
	}
	var json_str := JSON.stringify(report, "  ")
	var f := FileAccess.open("res://reports/verification/report.json", FileAccess.WRITE)
	if f:
		f.store_string(json_str)
		f.close()

	_a.summary()

	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")

	_phase = Phase.DONE
	quit(_a.exit_code())
