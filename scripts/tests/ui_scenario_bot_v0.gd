extends SceneTree

## UI Scenario Bot — Captures specific UI states for evaluating Phases 1-4 of the UI overhaul.
## Targets states that other bots miss: cargo full, hull critical, warfront contested,
## research near-completion, empire dashboard with opportunities, galaxy V2 overlays,
## market tier grouping, station faction header, trade feedback.
##
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Usage:
##   Run-Screenshot.ps1 -Mode scenario -Script "res://scripts/tests/ui_scenario_bot_v0.gd" -Prefix UISC

const PREFIX := "UISC|"
const MAX_FRAMES := 7200  # 120s safety — expanded panel coverage
const OUTPUT_DIR := "res://reports/ui_scenarios/"

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
const AssertLib = preload("res://scripts/tools/bot_assert.gd")

## --- Timing constants (frames at ~60fps) ---
const SETTLE_SCENE := 60
const SETTLE_UI := 20
const SETTLE_TAB := 15
const SETTLE_ACTION := 15
const POST_CAPTURE := 8

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY, WAIT_LOCAL_SYSTEM,

	# --- S1: Boot baseline ---
	BOOT,

	# --- S2: Dock — station header (L1.3) then market (L1.2) ---
	DOCK_ENTER,
	STATION_HEADER_CAPTURE,      # Station tab (default) — faction header, production, standing
	MARKET_SWITCH,
	MARKET_CAPTURE,              # Market tab with tier groups, profit column, price context

	# --- S3: Buy to fill cargo (L0.4 cargo capacity + L3.3 cargo full pulse) ---
	BUY_FILL_CARGO,
	CARGO_FULL_CAPTURE,          # Market showing cargo full state

	# --- S4: Undock and show full cargo HUD (L0.4 + L3.3) ---
	UNDOCK_FULL,
	HUD_CARGO_FULL_CAPTURE,      # Flight HUD with red pulsing cargo label

	# --- S5: Simulate hull damage (L3.3 hull critical) ---
	SIMULATE_DAMAGE,
	HUD_HULL_CRITICAL_CAPTURE,   # Flight HUD with pulsing hull bar

	# --- S6: Restore hull, start research, advance near completion (L3.3) ---
	RESTORE_HULL,
	START_RESEARCH,
	ADVANCE_RESEARCH,
	HUD_RESEARCH_NEAR_CAPTURE,   # HUD with "RESEARCH: tech — N ticks left!"

	# --- S7: Empire dashboard with opportunities (L3.1) ---
	OPEN_EMPIRE_DASH,
	EMPIRE_DASH_CAPTURE,         # Dashboard showing trade routes, tech count, credit trend

	# --- S8: Galaxy map default + legend (L2.2) ---
	CLOSE_EMPIRE_DASH,
	OPEN_GALAXY_MAP,
	GALAXY_DEFAULT_CAPTURE,      # Galaxy map with legend (security mode)

	# --- S9: Galaxy V2 overlays (L2.3) — cycle through all modes ---
	V2_FACTION,
	V2_FACTION_CAPTURE,
	V2_FLEET,
	V2_FLEET_CAPTURE,
	V2_HEAT,
	V2_HEAT_CAPTURE,
	V2_EXPLORATION,
	V2_EXPLORATION_CAPTURE,
	V2_WARFRONT,
	V2_WARFRONT_CAPTURE,
	V2_OFF,

	# --- S10: Close galaxy, warp to system 2 for trade comparison ---
	CLOSE_GALAXY_MAP,
	WARP_TRIGGER,
	WARP_WAIT,
	WARP_REBUILD,

	# --- S11: Dock at system 2 — market profit column (L2.4 node detail, L0.3) ---
	DOCK_2_ENTER,
	MARKET_2_CAPTURE,            # Market at different station showing profit vs origin

	# --- S12: Sell for profit (L0.1 trade feedback + L0.2 profit realization) ---
	SELL_FOR_PROFIT,
	POST_SELL_CAPTURE,           # Market after sell — shows feedback, P/L

	# --- S13: Contested zone (L3.3) — check warfront overlay, dock at contested node ---
	CHECK_WARFRONT,
	WARP_CONTESTED,
	WARP_CONTESTED_WAIT,
	WARP_CONTESTED_REBUILD,
	HUD_CONTESTED_CAPTURE,       # Flight HUD with "CONTESTED ZONE" or "WARZONE" badge

	# --- S14: Empire dashboard after trade activity ---
	OPEN_EMPIRE_DASH_2,
	EMPIRE_DASH_2_CAPTURE,       # Dashboard with credit trend populated

	# --- S15: Extended dock tab coverage ---
	REDOCK_FOR_TABS,
	DOCK_JOBS_TAB,
	DOCK_JOBS_CAPTURE,
	DOCK_SHIP_TAB,
	DOCK_SHIP_CAPTURE,
	DOCK_DIPLOMACY_TAB,
	DOCK_DIPLOMACY_CAPTURE,

	# --- S16: Standalone panels ---
	OPEN_COMBAT_LOG,
	COMBAT_LOG_CAPTURE,
	CLOSE_COMBAT_LOG,
	OPEN_AUTOMATION,
	AUTOMATION_CAPTURE,
	CLOSE_AUTOMATION,
	OPEN_KNOWLEDGE,
	KNOWLEDGE_CAPTURE,
	CLOSE_KNOWLEDGE,

	# --- S17: Galaxy node popup ---
	OPEN_GALAXY_FOR_POPUP,
	SHOW_NODE_POPUP,
	NODE_POPUP_CAPTURE,
	CLOSE_GALAXY_POPUP,

	# --- Final ---
	FINAL_CAPTURE,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _bridge = null
var _gm = null
var _screenshot = null
var _a: AssertLib = null
var _snapshots: Array = []

# Navigation state
var _home_node_id := ""
var _neighbor_ids: Array = []
var _all_edges: Array = []
var _contested_node_id := ""
var _trade_good := ""


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.DONE

	match _phase:
		# ── Setup ──
		Phase.LOAD_SCENE:
			var scene = load("res://scenes/playable_prototype.tscn").instantiate()
			root.add_child(scene)
			print(PREFIX + "SCENE_LOADED")
			_polls = 0
			_phase = Phase.WAIT_SCENE

		Phase.WAIT_SCENE:
			_polls += 1
			if _polls >= 30:
				_polls = 0
				_phase = Phase.WAIT_BRIDGE

		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= 600:
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_screenshot = ScreenshotScript.new()
				_a = AssertLib.new("UISC")
				_gm = root.get_node_or_null("GameManager")
				if _gm:
					_gm.set("_on_main_menu", false)
				_init_navigation()
				# Ensure fleet combat stats are initialized so hull bars show.
				if _bridge.has_method("DebugInitPlayerCombatV0"):
					_bridge.call("DebugInitPlayerCombatV0")
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= 600:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			if get_nodes_in_group("Station").size() > 0 or get_nodes_in_group("Planet").size() > 0:
				_polls = 0
				print(PREFIX + "LOCAL_SYSTEM|stations=%d planets=%d" % [
					get_nodes_in_group("Station").size(),
					get_nodes_in_group("Planet").size()])
				_phase = Phase.BOOT
			else:
				_polls += 1
				# Force local system rebuild if nothing appeared after 60 frames.
				if _polls == 60:
					_force_local_system_rebuild()
				if _polls >= 600:
					print(PREFIX + "WARN|local_system_timeout|stations=0")
					_phase = Phase.BOOT

		# ── S1: Boot baseline ──
		Phase.BOOT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("01_boot_baseline")
				_polls = 0
				_phase = Phase.DOCK_ENTER

		# ── S2: Dock — station header then market ──
		Phase.DOCK_ENTER:
			_polls += 1
			if _polls == POST_CAPTURE or _polls % 60 == 0:
				_dock_at_current_station()
			# Verify dock worked by checking player state.
			if _polls >= POST_CAPTURE + SETTLE_UI:
				var ps_check: Dictionary = _bridge.call("GetPlayerStateV0")
				var check_state: String = str(ps_check.get("ship_state_token", ""))
				if check_state == "DOCKED":
					_polls = 0
					_phase = Phase.STATION_HEADER_CAPTURE
				elif _polls >= 300:
					print(PREFIX + "WARN|dock_gave_up|state=%s" % check_state)
					_polls = 0
					_phase = Phase.STATION_HEADER_CAPTURE

		Phase.STATION_HEADER_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI + 10:  # Extra settle for dock menu to render
				_capture("02a_station_header")
				_polls = 0
				_phase = Phase.MARKET_SWITCH

		Phase.MARKET_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(0)  # Switch to Market tab
				_polls = 0
				_phase = Phase.MARKET_CAPTURE

		Phase.MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB + SETTLE_UI:  # Settle for market row rebuild
				_capture("02b_market_tier_groups")
				_run_region_assert("market_panel_center", Rect2(200, 200, 500, 300))
				_polls = 0
				_phase = Phase.BUY_FILL_CARGO

		# ── S3: Buy to fill cargo ──
		Phase.BUY_FILL_CARGO:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_buy_fill_cargo()
				_polls = 0
				_phase = Phase.CARGO_FULL_CAPTURE

		Phase.CARGO_FULL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("03_cargo_full_market")
				_polls = 0
				_phase = Phase.UNDOCK_FULL

		# ── S4: Undock with full cargo ──
		Phase.UNDOCK_FULL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.HUD_CARGO_FULL_CAPTURE

		Phase.HUD_CARGO_FULL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("04_hud_cargo_full_pulse")
				_polls = 0
				_phase = Phase.SIMULATE_DAMAGE

		# ── S5: Simulate hull damage ──
		Phase.SIMULATE_DAMAGE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_apply_hull_damage()
				_polls = 0
				_phase = Phase.HUD_HULL_CRITICAL_CAPTURE

		Phase.HUD_HULL_CRITICAL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI + 30:  # Extra settle for pulse to start
				_capture("05_hud_hull_critical_pulse")
				_polls = 0
				_phase = Phase.RESTORE_HULL

		# ── S6: Research near completion ──
		Phase.RESTORE_HULL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_restore_hull()
				_polls = 0
				_phase = Phase.START_RESEARCH

		Phase.START_RESEARCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_start_research()
				_polls = 0
				_phase = Phase.ADVANCE_RESEARCH

		Phase.ADVANCE_RESEARCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_advance_research_near_completion()
				_polls = 0
				_phase = Phase.HUD_RESEARCH_NEAR_CAPTURE

		Phase.HUD_RESEARCH_NEAR_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI + 110:  # Wait >2s for slow-poll timer to fire
				_capture("06_hud_research_near_complete")
				_polls = 0
				_phase = Phase.OPEN_EMPIRE_DASH

		# ── S7: Empire dashboard with opportunities ──
		Phase.OPEN_EMPIRE_DASH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.EMPIRE_DASH_CAPTURE

		Phase.EMPIRE_DASH_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("07_empire_dash_opportunities")
				_polls = 0
				_phase = Phase.CLOSE_EMPIRE_DASH

		# ── S8: Galaxy map default + legend ──
		Phase.CLOSE_EMPIRE_DASH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.OPEN_GALAXY_MAP

		Phase.OPEN_GALAXY_MAP:
			_polls += 1
			if _polls >= SETTLE_UI:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.GALAXY_DEFAULT_CAPTURE

		Phase.GALAXY_DEFAULT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("08_galaxy_legend_default")
				_polls = 0
				_phase = Phase.V2_FACTION

		# ── S9: Galaxy V2 overlay cycle ──
		Phase.V2_FACTION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(1)  # Faction
				_polls = 0
				_phase = Phase.V2_FACTION_CAPTURE

		Phase.V2_FACTION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("09a_galaxy_v2_faction")
				_polls = 0
				_phase = Phase.V2_FLEET

		Phase.V2_FLEET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(2)  # Fleet
				_polls = 0
				_phase = Phase.V2_FLEET_CAPTURE

		Phase.V2_FLEET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("09b_galaxy_v2_fleet")
				_polls = 0
				_phase = Phase.V2_HEAT

		Phase.V2_HEAT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(3)  # Heat
				_polls = 0
				_phase = Phase.V2_HEAT_CAPTURE

		Phase.V2_HEAT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("09c_galaxy_v2_heat")
				_polls = 0
				_phase = Phase.V2_EXPLORATION

		Phase.V2_EXPLORATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(4)  # Exploration
				_polls = 0
				_phase = Phase.V2_EXPLORATION_CAPTURE

		Phase.V2_EXPLORATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("09d_galaxy_v2_exploration")
				_polls = 0
				_phase = Phase.V2_WARFRONT

		Phase.V2_WARFRONT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(5)  # Warfront
				_polls = 0
				_phase = Phase.V2_WARFRONT_CAPTURE

		Phase.V2_WARFRONT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("09e_galaxy_v2_warfront")
				_polls = 0
				_phase = Phase.V2_OFF

		Phase.V2_OFF:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(0)  # Off
				_polls = 0
				_phase = Phase.CLOSE_GALAXY_MAP

		# ── S10: Warp to system 2 ──
		Phase.CLOSE_GALAXY_MAP:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.WARP_TRIGGER

		Phase.WARP_TRIGGER:
			_polls += 1
			if _polls >= SETTLE_UI:
				if _neighbor_ids.size() >= 1:
					_gm.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[0])
					print(PREFIX + "WARP|%s" % _neighbor_ids[0])
				else:
					print(PREFIX + "WARN|no_neighbors")
				_polls = 0
				_phase = Phase.WARP_WAIT

		Phase.WARP_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				if _neighbor_ids.size() >= 1:
					_gm.call("on_lane_arrival_v0", _neighbor_ids[0])
				_polls = 0
				_phase = Phase.WARP_REBUILD

		Phase.WARP_REBUILD:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				if _neighbor_ids.size() >= 1:
					_rebuild_local_system(_neighbor_ids[0])
				_polls = 0
				_phase = Phase.DOCK_2_ENTER

		# ── S11: Dock at system 2 — profit comparison ──
		Phase.DOCK_2_ENTER:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.MARKET_2_CAPTURE

		Phase.MARKET_2_CAPTURE:
			_polls += 1
			if _polls == 1:
				_switch_dock_tab(0)  # Switch to Market tab
			if _polls >= SETTLE_TAB + SETTLE_UI:
				_capture("10_market_profit_comparison")
				_polls = 0
				_phase = Phase.SELL_FOR_PROFIT

		# ── S12: Sell for profit ──
		Phase.SELL_FOR_PROFIT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_sell_cargo()
				_polls = 0
				_phase = Phase.POST_SELL_CAPTURE

		Phase.POST_SELL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION + 75:  # 1.5s for floating profit label to be visible
				_capture("11_post_sell_profit_feedback")
				_run_region_assert("post_sell_feedback", Rect2(400, 200, 400, 300))
				_polls = 0
				_phase = Phase.CHECK_WARFRONT

		# ── S13: Contested zone ──
		Phase.CHECK_WARFRONT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_find_contested_node()
				_polls = 0
				if _contested_node_id.is_empty():
					print(PREFIX + "WARN|no_contested_node")
					_phase = Phase.OPEN_EMPIRE_DASH_2
				else:
					_undock()
					_phase = Phase.WARP_CONTESTED

		Phase.WARP_CONTESTED:
			_polls += 1
			if _polls >= SETTLE_UI:
				_gm.call("on_lane_gate_proximity_entered_v0", _contested_node_id)
				print(PREFIX + "WARP_CONTESTED|%s" % _contested_node_id)
				_polls = 0
				_phase = Phase.WARP_CONTESTED_WAIT

		Phase.WARP_CONTESTED_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_gm.call("on_lane_arrival_v0", _contested_node_id)
				_polls = 0
				_phase = Phase.WARP_CONTESTED_REBUILD

		Phase.WARP_CONTESTED_REBUILD:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_rebuild_local_system(_contested_node_id)
				_polls = 0
				_phase = Phase.HUD_CONTESTED_CAPTURE

		Phase.HUD_CONTESTED_CAPTURE:
			_polls += 1
			if _polls == 1:
				var ps2: Dictionary = _bridge.call("GetPlayerStateV0")
				var cur: String = str(ps2.get("current_node_id", ""))
				var wf: Dictionary = _bridge.call("GetWarfrontOverlayV0")
				print(PREFIX + "CONTESTED_CHECK|current=%s contested=%s overlay_has=%s intensity=%s" % [
					cur, _contested_node_id, str(wf.has(cur)), str(wf.get(cur, 0.0))])
			if _polls >= SETTLE_UI:
				_capture("12_hud_contested_zone")
				_polls = 0
				_phase = Phase.OPEN_EMPIRE_DASH_2

		# ── S14: Empire dashboard after trade ──
		Phase.OPEN_EMPIRE_DASH_2:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Re-dock if we're flying
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				var ship_state: String = str(ps.get("ship_state_token", ""))
				if ship_state != "DOCKED":
					_dock_at_current_station()
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.EMPIRE_DASH_2_CAPTURE

		Phase.EMPIRE_DASH_2_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("13_empire_dash_post_trade")
				_run_region_assert("empire_dash_center", Rect2(300, 100, 600, 400))
				_polls = 0
				_phase = Phase.REDOCK_FOR_TABS

		# ── S15: Extended dock tab coverage ──
		Phase.REDOCK_FOR_TABS:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_empire_dashboard()  # Close dashboard
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				if str(ps.get("ship_state_token", "")) != "DOCKED":
					_dock_at_current_station()
				_polls = 0
				_phase = Phase.DOCK_JOBS_TAB

		Phase.DOCK_JOBS_TAB:
			_polls += 1
			if _polls >= SETTLE_UI:
				_switch_dock_tab(1)  # Jobs
				_polls = 0
				_phase = Phase.DOCK_JOBS_CAPTURE

		Phase.DOCK_JOBS_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("14_dock_jobs_tab")
				_polls = 0
				_phase = Phase.DOCK_SHIP_TAB

		Phase.DOCK_SHIP_TAB:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(2)  # Ship
				_polls = 0
				_phase = Phase.DOCK_SHIP_CAPTURE

		Phase.DOCK_SHIP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("15_dock_ship_tab")
				_polls = 0
				_phase = Phase.DOCK_DIPLOMACY_TAB

		Phase.DOCK_DIPLOMACY_TAB:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(6)  # Diplomacy
				_polls = 0
				_phase = Phase.DOCK_DIPLOMACY_CAPTURE

		Phase.DOCK_DIPLOMACY_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("16_dock_diplomacy_tab")
				_polls = 0
				_phase = Phase.OPEN_COMBAT_LOG

		# ── S16: Standalone panels ──
		Phase.OPEN_COMBAT_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.COMBAT_LOG_CAPTURE

		Phase.COMBAT_LOG_CAPTURE:
			_polls += 1
			if _polls == SETTLE_UI:
				_toggle_combat_log()
			if _polls >= SETTLE_UI + SETTLE_TAB:
				_capture("17_combat_log")
				_polls = 0
				_phase = Phase.CLOSE_COMBAT_LOG

		Phase.CLOSE_COMBAT_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_combat_log()
				_polls = 0
				_phase = Phase.OPEN_AUTOMATION

		Phase.OPEN_AUTOMATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_automation_dashboard()
				_polls = 0
				_phase = Phase.AUTOMATION_CAPTURE

		Phase.AUTOMATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("18_automation_dash")
				_polls = 0
				_phase = Phase.CLOSE_AUTOMATION

		Phase.CLOSE_AUTOMATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_automation_dashboard()
				_polls = 0
				_phase = Phase.OPEN_KNOWLEDGE

		Phase.OPEN_KNOWLEDGE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_knowledge_web()
				_polls = 0
				_phase = Phase.KNOWLEDGE_CAPTURE

		Phase.KNOWLEDGE_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("19_knowledge_web")
				_polls = 0
				_phase = Phase.CLOSE_KNOWLEDGE

		Phase.CLOSE_KNOWLEDGE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_knowledge_web()
				_polls = 0
				_phase = Phase.OPEN_GALAXY_FOR_POPUP

		# ── S17: Galaxy node popup ──
		Phase.OPEN_GALAXY_FOR_POPUP:
			_polls += 1
			if _polls >= SETTLE_UI:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.SHOW_NODE_POPUP

		Phase.SHOW_NODE_POPUP:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				var gv = root.find_child("GalaxyView", true, false)
				if gv != null and gv.has_method("ShowNodePopupForBot"):
					var target_id := _home_node_id if not _home_node_id.is_empty() else ""
					if target_id.is_empty() and _neighbor_ids.size() > 0:
						target_id = _neighbor_ids[0]
					if target_id != "":
						gv.call("ShowNodePopupForBot", target_id)
						print(PREFIX + "NODE_POPUP|%s" % target_id)
				_polls = 0
				_phase = Phase.NODE_POPUP_CAPTURE

		Phase.NODE_POPUP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("20_galaxy_node_popup")
				_polls = 0
				_phase = Phase.CLOSE_GALAXY_POPUP

		Phase.CLOSE_GALAXY_POPUP:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.FINAL_CAPTURE

		# ── Final ──
		Phase.FINAL_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_empire_dashboard()  # Close
				_a.hard(_snapshots.size() >= 18, "CAPTURE_COUNT",
					"captured=%d (expected >=18)" % _snapshots.size())
				_a.summary()
				_write_summary()
				_capture("99_final")
				_polls = 0
				_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# ── Navigation helpers ──

func _init_navigation() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var lanes: Array = galaxy.get("lane_edges", [])
	_all_edges = lanes
	var seen := {}
	for lane in lanes:
		var from_id: String = str(lane.get("from_id", ""))
		var to_id: String = str(lane.get("to_id", ""))
		if from_id == _home_node_id and not seen.has(to_id):
			_neighbor_ids.append(to_id)
			seen[to_id] = true
		elif to_id == _home_node_id and not seen.has(from_id):
			_neighbor_ids.append(from_id)
			seen[from_id] = true

	print(PREFIX + "NAV|home=%s neighbors=%d" % [_home_node_id, _neighbor_ids.size()])


func _rebuild_local_system(node_id: String) -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv and gv.has_method("RebuildLocalSystemV0"):
		gv.call("RebuildLocalSystemV0", node_id)
	print(PREFIX + "REBUILD|%s" % node_id)


func _force_local_system_rebuild() -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv and gv.has_method("RebuildLocalSystemV0") and not _home_node_id.is_empty():
		gv.call("RebuildLocalSystemV0", _home_node_id)
		print(PREFIX + "FORCE_REBUILD|%s" % _home_node_id)
	else:
		print(PREFIX + "WARN|cannot_force_rebuild|gv=%s home=%s" % [str(gv != null), _home_node_id])


func _dock_at_current_station() -> void:
	if _gm == null:
		print(PREFIX + "WARN|dock_fail|no_gm")
		return
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.is_empty():
		# Last resort: force rebuild and try again next frame.
		print(PREFIX + "WARN|dock_fail|no_targets|forcing_rebuild")
		_force_local_system_rebuild()
		return
	_gm.call("on_proximity_dock_entered_v0", targets[0])
	print(PREFIX + "DOCK|%s" % str(targets[0].name))


func _undock() -> void:
	if _gm != null and _gm.has_method("undock_v0"):
		_gm.call("undock_v0")
		print(PREFIX + "UNDOCK")


func _toggle_galaxy_map() -> void:
	if _gm != null and _gm.has_method("toggle_galaxy_map_overlay_v0"):
		_gm.call("toggle_galaxy_map_overlay_v0")


func _toggle_empire_dashboard() -> void:
	if _gm != null:
		_gm.call("_toggle_empire_dashboard_v0")


func _set_v2_overlay(mode: int) -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv != null and gv.has_method("SetV2OverlayModeV0"):
		gv.call("SetV2OverlayModeV0", mode)
		print(PREFIX + "V2_OVERLAY|mode=%d" % mode)


func _switch_dock_tab(idx: int) -> void:
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm != null and htm.has_method("_switch_dock_tab"):
		htm.call("_switch_dock_tab", idx)


# ── Trade & cargo helpers ──

func _buy_fill_cargo() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	var cap: int = int(ps.get("cargo_capacity", 50))
	var cargo: int = int(ps.get("cargo_count", 0))
	var credits: int = int(ps.get("credits", 0))

	if node_id.is_empty():
		return

	# Give player enough credits to fill cargo.
	if credits < 10000 and _bridge.has_method("DebugSetCreditsV0"):
		_bridge.call("DebugSetCreditsV0", 50000)
		print(PREFIX + "CREDITS_SET|50000")

	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	var slots_free: int = cap - cargo
	for item in market:
		if slots_free <= 0:
			break
		var good_id: String = str(item.get("good_id", ""))
		var qty: int = int(item.get("quantity", 0))
		var buy_qty: int = min(qty, slots_free)
		if buy_qty > 0 and good_id != "":
			_bridge.call("DispatchPlayerTradeV0", node_id, good_id, buy_qty, true)
			_trade_good = good_id
			slots_free -= buy_qty
			print(PREFIX + "BUY|%s x%d" % [good_id, buy_qty])

	print(PREFIX + "CARGO_FILL|filled=%d" % (cap - slots_free))


func _sell_cargo() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		return

	# Sell first cargo item we find.
	if _bridge.has_method("GetCargoWithCostBasisV0"):
		var cargo: Array = _bridge.call("GetCargoWithCostBasisV0", node_id)
		for item in cargo:
			var good_id: String = str(item.get("good_id", ""))
			var qty: int = int(item.get("quantity", 0))
			if qty > 0 and good_id != "":
				var sell_qty: int = min(qty, 3)
				_bridge.call("DispatchPlayerTradeV0", node_id, good_id, sell_qty, false)
				print(PREFIX + "SELL|%s x%d" % [good_id, sell_qty])
				return


# ── Combat / hull helpers ──

func _apply_hull_damage() -> void:
	if _bridge.has_method("DebugSetPlayerHullV0"):
		# Set hull to 15% of max to trigger critical pulse.
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull_max: int = int(hp.get("hull_max", 100))
		var target: int = max(1, int(hull_max * 0.15))
		_bridge.call("DebugSetPlayerHullV0", target)
		print(PREFIX + "HULL_SET|%d/%d (critical)" % [target, hull_max])
	else:
		print(PREFIX + "WARN|no_debug_hull_method")


func _restore_hull() -> void:
	if _bridge.has_method("DebugRestorePlayerHullV0"):
		_bridge.call("DebugRestorePlayerHullV0")
		print(PREFIX + "HULL_RESTORED")
	else:
		print(PREFIX + "WARN|no_restore_hull_method")


# ── Research helpers ──

func _start_research() -> void:
	if not _bridge.has_method("GetTechTreeV0"):
		print(PREFIX + "WARN|no_tech_tree")
		return

	var techs: Array = _bridge.call("GetTechTreeV0")
	for tech in techs:
		var status: String = str(tech.get("status", ""))
		if status == "available" or status == "Available":
			var tech_id: String = str(tech.get("tech_id", ""))
			if tech_id != "" and _bridge.has_method("StartResearchV0"):
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				var node_id: String = str(ps.get("current_node_id", ""))
				_bridge.call("StartResearchV0", tech_id, node_id)
				print(PREFIX + "RESEARCH_START|%s" % tech_id)
				return

	print(PREFIX + "WARN|no_available_tech")


func _advance_research_near_completion() -> void:
	# Tick the sim forward to get research close to finishing.
	if not _bridge.has_method("GetResearchStatusV0"):
		return
	var status: Dictionary = _bridge.call("GetResearchStatusV0")
	if not status.get("researching", false):
		print(PREFIX + "WARN|research_not_active")
		return
	var total: int = int(status.get("total_ticks", 100))
	var progress: int = int(status.get("progress_ticks", 0))
	var target: int = max(0, total - 5)  # Leave 5 ticks remaining.
	var ticks_needed: int = target - progress
	if ticks_needed > 0:
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", ticks_needed)
			print(PREFIX + "RESEARCH_ADVANCE|%d ticks (target=%d remaining=5)" % [ticks_needed, total])
		else:
			print(PREFIX + "WARN|no_debug_advance_method")
	else:
		print(PREFIX + "RESEARCH_ADVANCE|already_near_completion")


# ── Warfront helpers ──

func _find_contested_node() -> void:
	if not _bridge.has_method("GetWarfrontOverlayV0"):
		return
	var overlay: Dictionary = _bridge.call("GetWarfrontOverlayV0")
	if overlay.is_empty():
		print(PREFIX + "WARN|no_warfronts_active")
		return

	# Find a contested node reachable from current position (any neighbor).
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var current: String = str(ps.get("current_node_id", ""))

	# First check: is current node contested?
	if overlay.has(current):
		_contested_node_id = current
		print(PREFIX + "CONTESTED|current_node=%s" % current)
		return

	# Check neighbors.
	for edge in _all_edges:
		var from_id: String = str(edge.get("from_id", ""))
		var to_id: String = str(edge.get("to_id", ""))
		var neighbor := ""
		if from_id == current:
			neighbor = to_id
		elif to_id == current:
			neighbor = from_id
		if neighbor != "" and overlay.has(neighbor):
			_contested_node_id = neighbor
			print(PREFIX + "CONTESTED|neighbor=%s" % neighbor)
			return

	# Fallback: pick any contested node (may not be directly reachable).
	for node_id in overlay.keys():
		_contested_node_id = str(node_id)
		print(PREFIX + "CONTESTED|any=%s (may not be adjacent)" % _contested_node_id)
		return


# ── Capture & utility ──

func _capture(label: String) -> void:
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]
	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)
	_snapshots.append({"phase": label, "tick": tick, "screenshot": img_path})
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _write_summary() -> void:
	var summary := {
		"bot_version": "ui_scenario_v0",
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
	}
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var f := FileAccess.open(OUTPUT_DIR.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
		print(PREFIX + "SUMMARY_SAVED")


func _toggle_combat_log() -> void:
	if _gm != null and _gm.has_method("_toggle_combat_log_v0"):
		_gm.call("_toggle_combat_log_v0")
		print(PREFIX + "TOGGLE|combat_log")
	else:
		print(PREFIX + "WARN|no_combat_log_toggle")


func _toggle_automation_dashboard() -> void:
	# Direct toggle — find the panel and flip visibility ourselves.
	var panel = root.find_child("AutomationDashboard", true, false)
	if panel != null:
		panel.visible = not panel.visible
		if panel.visible and panel.has_method("refresh_v0"):
			panel.call("refresh_v0")
		print(PREFIX + "TOGGLE|automation_dash|vis=%s" % str(panel.visible))
	else:
		print(PREFIX + "WARN|no_automation_panel")


func _toggle_knowledge_web() -> void:
	# Direct toggle — find the panel and flip visibility ourselves.
	var panel = root.find_child("KnowledgeWebPanel", true, false)
	if panel != null:
		panel.visible = not panel.visible
		if panel.visible and panel.has_method("refresh_v0"):
			panel.call("refresh_v0")
		print(PREFIX + "TOGGLE|knowledge_web|vis=%s" % str(panel.visible))
	else:
		# Try via HUD method which lazy-creates
		var hud = root.find_child("HUD", true, false)
		if hud != null and hud.has_method("toggle_knowledge_web_v0"):
			hud.call("toggle_knowledge_web_v0")
			print(PREFIX + "TOGGLE|knowledge_web_via_hud")
		else:
			print(PREFIX + "WARN|no_knowledge_panel")


## Run a non-empty region assertion on the last captured screenshot.
func _run_region_assert(label: String, rect: Rect2) -> void:
	var viewport := root.get_viewport()
	if viewport == null:
		return
	var img := viewport.get_texture().get_image()
	if img == null:
		return
	var result: String = ScreenshotScript.assert_region_nonempty(img, rect, label)
	if result != "":
		print(PREFIX + "WARN|" + result)
	else:
		print(PREFIX + "ASSERT_OK|%s" % label)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	print(PREFIX + "DONE|snapshots=%d" % _snapshots.size())
	quit(0 if (_a == null or not _a.has_failures()) else 1)
