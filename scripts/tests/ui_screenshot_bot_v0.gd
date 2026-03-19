extends SceneTree

## UI Screenshot Bot — Captures every player-facing menu, tab, panel, and overlay.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Usage:
##   .\scripts\tools\Run-Screenshot.ps1 -Mode scenario -Script res://scripts/tests/ui_screenshot_bot_v0.gd -Prefix UISC

const PREFIX := "UISC|"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/screenshot/ui_all/"

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

## --- Timing constants (frames at ~60fps) ---
const SETTLE_SCENE := 60       # After scene load / system rebuild (1.0s)
const SETTLE_CAMERA := 45      # After camera distance change (0.75s)
const SETTLE_UI := 20          # After UI overlay toggle (0.33s)
const SETTLE_TAB := 15         # After dock tab switch (0.25s)
const SETTLE_ACTION := 15      # After game action (dock, undock, buy)
const POST_CAPTURE := 8        # Recovery after PNG save stutter

enum Phase {
	# --- Setup ---
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	# --- Flight HUD ---
	BOOT_CAPTURE,              # 01. Flight HUD: star, planets, station, NPC fleets

	# --- Dock Tab Sweep (Home System) ---
	DOCK_ENTER,
	TAB_MARKET_CAPTURE,        # 02. Market tab
	TAB_JOBS_SWITCH,
	TAB_JOBS_CAPTURE,          # 03. Jobs tab
	TAB_SHIP_SWITCH,
	TAB_SHIP_CAPTURE,          # 04. Ship tab (refit/maintenance)
	TAB_STATION_SWITCH,
	TAB_STATION_CAPTURE,       # 05. Station tab (research/construction)
	TAB_INTEL_SWITCH,
	TAB_INTEL_CAPTURE,         # 06. Intel tab (automation/trade routes)
	TAB_DIPLOMACY_SWITCH,
	TAB_DIPLOMACY_CAPTURE,     # 07. Diplomacy tab (treaties/bounties)
	UNDOCK_1,

	# --- Galaxy Map Overlays ---
	OPEN_GALAXY_MAP,
	GALAXY_DEFAULT_CAPTURE,    # 08. Galaxy map: default security coloring
	GALAXY_V2_FACTION,
	GALAXY_FACTION_CAPTURE,    # 09. Galaxy map: faction territory overlay
	GALAXY_V2_FLEET,
	GALAXY_FLEET_CAPTURE,      # 10. Galaxy map: fleet positions overlay
	GALAXY_V2_HEAT,
	GALAXY_HEAT_CAPTURE,       # 11. Galaxy map: security heat overlay
	GALAXY_V2_EXPLORATION,
	GALAXY_EXPLORATION_CAPTURE, # 12. Galaxy map: exploration overlay
	GALAXY_V2_WARFRONT,
	GALAXY_WARFRONT_CAPTURE,   # 13. Galaxy map: warfront overlay
	CLOSE_GALAXY_MAP,

	# --- Empire Dashboard ---
	OPEN_EMPIRE_DASH,
	EMPIRE_DASH_CAPTURE,       # 14. Empire dashboard
	CLOSE_EMPIRE_DASH,

	# --- Keyboard Overlay Panels ---
	OPEN_MISSION_JOURNAL,
	MISSION_JOURNAL_CAPTURE,   # 15. Mission journal (J key)
	CLOSE_MISSION_JOURNAL,
	OPEN_KNOWLEDGE_WEB,
	KNOWLEDGE_WEB_CAPTURE,     # 16. Knowledge web (K key)
	CLOSE_KNOWLEDGE_WEB,
	OPEN_COMBAT_LOG,
	COMBAT_LOG_CAPTURE,        # 17. Combat log (L key)
	CLOSE_COMBAT_LOG,
	OPEN_DATA_LOG,
	DATA_LOG_CAPTURE,          # 18. Data log
	CLOSE_DATA_LOG,
	OPEN_FO_PANEL,
	FO_PANEL_CAPTURE,          # 19. First Officer panel (F key)
	CLOSE_FO_PANEL,
	OPEN_WARFRONT_DASH,
	WARFRONT_DASH_CAPTURE,     # 20. Warfront dashboard (N key)
	CLOSE_WARFRONT_DASH,

	# --- Warp to System 2 (different market, different star) ---
	WARP_2_TRIGGER,
	WARP_2_WAIT,
	WARP_2_REBUILD,
	SYSTEM_2_FLIGHT_CAPTURE,   # 21. System 2 flight view
	SYSTEM_2_DOCK,
	SYSTEM_2_MARKET_CAPTURE,   # 22. System 2 market (different goods/prices)
	SETUP_REFIT,
	SYSTEM_2_SHIP_CAPTURE,     # 23. Ship tab with equipped module
	SETUP_CONSTRUCTION,
	SYSTEM_2_STATION_CAPTURE,  # 24. Station tab with construction in progress
	SETUP_AUTOMATION,
	SYSTEM_2_INTEL_CAPTURE,    # 25. Intel tab with active automation
	SYSTEM_2_UNDOCK,

	# --- Warp to System 3 (third market variety) ---
	WARP_3_COOLDOWN,
	WARP_3_TRIGGER,
	WARP_3_WAIT,
	WARP_3_REBUILD,
	SYSTEM_3_DOCK,
	SYSTEM_3_MARKET_CAPTURE,   # 26. System 3 market
	SYSTEM_3_UNDOCK,

	# --- NPC Close-up ---
	NPC_ZOOM_IN,
	NPC_APPROACH,
	NPC_CLOSEUP_CAPTURE,       # 27. NPC ship close-up (role label, 3D model)
	NPC_ZOOM_OUT,

	# --- Final ---
	WRITE_SUMMARY,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _bridge = null
var _game_manager = null
var _total_frames := 0
const MAX_FRAMES := 5400  # 90s at 60fps — safety exit

var _screenshot = null

# Navigation state
var _home_node_id := ""
var _neighbor_ids: Array = []
var _snapshots: Array = []


func _initialize() -> void:
	print(PREFIX + "START|UI_SCREENSHOT_BOT_V0")


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.WRITE_SUMMARY

	match _phase:
		# ── Setup ──────────────────────────────────────────────
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
				if _polls >= MAX_POLLS:
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
				_game_manager = root.get_node_or_null("GameManager")
				if _game_manager:
					_game_manager.set("_on_main_menu", false)
				_init_navigation()
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			if get_nodes_in_group("Station").size() > 0:
				_polls = 0
				_phase = Phase.BOOT_CAPTURE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_phase = Phase.BOOT_CAPTURE

		# ── Flight HUD ─────────────────────────────────────────
		Phase.BOOT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("01_flight_hud")
				_polls = 0
				_phase = Phase.DOCK_ENTER

		# ── Dock Tab Sweep ─────────────────────────────────────
		Phase.DOCK_ENTER:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.TAB_MARKET_CAPTURE

		Phase.TAB_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("02_tab_market")
				_polls = 0
				_phase = Phase.TAB_JOBS_SWITCH

		Phase.TAB_JOBS_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(1)
				_polls = 0
				_phase = Phase.TAB_JOBS_CAPTURE

		Phase.TAB_JOBS_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("03_tab_jobs")
				_polls = 0
				_phase = Phase.TAB_SHIP_SWITCH

		Phase.TAB_SHIP_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(2)
				_polls = 0
				_phase = Phase.TAB_SHIP_CAPTURE

		Phase.TAB_SHIP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("04_tab_ship")
				_polls = 0
				_phase = Phase.TAB_STATION_SWITCH

		Phase.TAB_STATION_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(3)
				_polls = 0
				_phase = Phase.TAB_STATION_CAPTURE

		Phase.TAB_STATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("05_tab_station")
				_polls = 0
				_phase = Phase.TAB_INTEL_SWITCH

		Phase.TAB_INTEL_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(4)
				_polls = 0
				_phase = Phase.TAB_INTEL_CAPTURE

		Phase.TAB_INTEL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("06_tab_intel")
				_polls = 0
				_phase = Phase.TAB_DIPLOMACY_SWITCH

		Phase.TAB_DIPLOMACY_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(6)
				_polls = 0
				_phase = Phase.TAB_DIPLOMACY_CAPTURE

		Phase.TAB_DIPLOMACY_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("07_tab_diplomacy")
				_polls = 0
				_phase = Phase.UNDOCK_1

		Phase.UNDOCK_1:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.OPEN_GALAXY_MAP

		# ── Galaxy Map Overlays ────────────────────────────────
		Phase.OPEN_GALAXY_MAP:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.GALAXY_DEFAULT_CAPTURE

		Phase.GALAXY_DEFAULT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("08_galaxy_default")
				_polls = 0
				_phase = Phase.GALAXY_V2_FACTION

		Phase.GALAXY_V2_FACTION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(1)  # Faction
				_polls = 0
				_phase = Phase.GALAXY_FACTION_CAPTURE

		Phase.GALAXY_FACTION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("09_galaxy_faction")
				_polls = 0
				_phase = Phase.GALAXY_V2_FLEET

		Phase.GALAXY_V2_FLEET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(2)  # Fleet
				_polls = 0
				_phase = Phase.GALAXY_FLEET_CAPTURE

		Phase.GALAXY_FLEET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("10_galaxy_fleet")
				_polls = 0
				_phase = Phase.GALAXY_V2_HEAT

		Phase.GALAXY_V2_HEAT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(3)  # Heat
				_polls = 0
				_phase = Phase.GALAXY_HEAT_CAPTURE

		Phase.GALAXY_HEAT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("11_galaxy_heat")
				_polls = 0
				_phase = Phase.GALAXY_V2_EXPLORATION

		Phase.GALAXY_V2_EXPLORATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(4)  # Exploration
				_polls = 0
				_phase = Phase.GALAXY_EXPLORATION_CAPTURE

		Phase.GALAXY_EXPLORATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("12_galaxy_exploration")
				_polls = 0
				_phase = Phase.GALAXY_V2_WARFRONT

		Phase.GALAXY_V2_WARFRONT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(5)  # Warfront
				_polls = 0
				_phase = Phase.GALAXY_WARFRONT_CAPTURE

		Phase.GALAXY_WARFRONT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("13_galaxy_warfront")
				_polls = 0
				_phase = Phase.CLOSE_GALAXY_MAP

		Phase.CLOSE_GALAXY_MAP:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(0)  # Reset overlay
				_toggle_galaxy_map()
				_polls = 0
				_phase = Phase.OPEN_EMPIRE_DASH

		# ── Empire Dashboard ───────────────────────────────────
		Phase.OPEN_EMPIRE_DASH:
			_polls += 1
			if _polls >= SETTLE_UI:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.EMPIRE_DASH_CAPTURE

		Phase.EMPIRE_DASH_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("14_empire_dashboard")
				_polls = 0
				_phase = Phase.CLOSE_EMPIRE_DASH

		Phase.CLOSE_EMPIRE_DASH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.OPEN_MISSION_JOURNAL

		# ── Keyboard Overlay Panels ────────────────────────────
		Phase.OPEN_MISSION_JOURNAL:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_setup_mission()
				_toggle_panel("_toggle_mission_journal_v0")
				_polls = 0
				_phase = Phase.MISSION_JOURNAL_CAPTURE

		Phase.MISSION_JOURNAL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("15_mission_journal")
				_polls = 0
				_phase = Phase.CLOSE_MISSION_JOURNAL

		Phase.CLOSE_MISSION_JOURNAL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_mission_journal_v0")
				_polls = 0
				_phase = Phase.OPEN_KNOWLEDGE_WEB

		Phase.OPEN_KNOWLEDGE_WEB:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_knowledge_web_v0")
				_polls = 0
				_phase = Phase.KNOWLEDGE_WEB_CAPTURE

		Phase.KNOWLEDGE_WEB_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("16_knowledge_web")
				_polls = 0
				_phase = Phase.CLOSE_KNOWLEDGE_WEB

		Phase.CLOSE_KNOWLEDGE_WEB:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_knowledge_web_v0")
				_polls = 0
				_phase = Phase.OPEN_COMBAT_LOG

		Phase.OPEN_COMBAT_LOG:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_combat_log_v0")
				_polls = 0
				_phase = Phase.COMBAT_LOG_CAPTURE

		Phase.COMBAT_LOG_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("17_combat_log")
				_polls = 0
				_phase = Phase.CLOSE_COMBAT_LOG

		Phase.CLOSE_COMBAT_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_combat_log_v0")
				_polls = 0
				_phase = Phase.OPEN_DATA_LOG

		Phase.OPEN_DATA_LOG:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_data_log_v0")
				_polls = 0
				_phase = Phase.DATA_LOG_CAPTURE

		Phase.DATA_LOG_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("18_data_log")
				_polls = 0
				_phase = Phase.CLOSE_DATA_LOG

		Phase.CLOSE_DATA_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_data_log_v0")
				_polls = 0
				_phase = Phase.OPEN_FO_PANEL

		Phase.OPEN_FO_PANEL:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_fo_panel_v0")
				_polls = 0
				_phase = Phase.FO_PANEL_CAPTURE

		Phase.FO_PANEL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("19_fo_panel")
				_polls = 0
				_phase = Phase.CLOSE_FO_PANEL

		Phase.CLOSE_FO_PANEL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_fo_panel_v0")
				_polls = 0
				_phase = Phase.OPEN_WARFRONT_DASH

		Phase.OPEN_WARFRONT_DASH:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_warfront_dashboard_v0")
				_polls = 0
				_phase = Phase.WARFRONT_DASH_CAPTURE

		Phase.WARFRONT_DASH_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("20_warfront_dashboard")
				_polls = 0
				_phase = Phase.CLOSE_WARFRONT_DASH

		Phase.CLOSE_WARFRONT_DASH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_warfront_dashboard_v0")
				_polls = 0
				_phase = Phase.WARP_2_TRIGGER

		# ── Warp to System 2 ──────────────────────────────────
		Phase.WARP_2_TRIGGER:
			_polls += 1
			if _polls >= SETTLE_UI:
				if _game_manager == null or _neighbor_ids.size() < 1:
					print(PREFIX + "WARN|no_neighbors, skipping warp_2")
					_phase = Phase.NPC_ZOOM_IN
				else:
					_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[0])
					print(PREFIX + "WARP_TRIGGER|%s" % _neighbor_ids[0])
					_polls = 0
					_phase = Phase.WARP_2_WAIT

		Phase.WARP_2_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_polls = 0
				_phase = Phase.WARP_2_REBUILD

		Phase.WARP_2_REBUILD:
			_force_arrival(_neighbor_ids[0])
			_rebuild_local_system(_neighbor_ids[0])
			_polls = 0
			_phase = Phase.SYSTEM_2_FLIGHT_CAPTURE

		Phase.SYSTEM_2_FLIGHT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("21_system_2_flight")
				_polls = 0
				_phase = Phase.SYSTEM_2_DOCK

		Phase.SYSTEM_2_DOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.SYSTEM_2_MARKET_CAPTURE

		Phase.SYSTEM_2_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("22_system_2_market")
				_polls = 0
				_phase = Phase.SETUP_REFIT

		Phase.SETUP_REFIT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_refit()
				_switch_dock_tab(2)  # Ship tab
				_polls = 0
				_phase = Phase.SYSTEM_2_SHIP_CAPTURE

		Phase.SYSTEM_2_SHIP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("23_ship_with_module")
				_polls = 0
				_phase = Phase.SETUP_CONSTRUCTION

		Phase.SETUP_CONSTRUCTION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_construction()
				_switch_dock_tab(3)  # Station tab
				_polls = 0
				_phase = Phase.SYSTEM_2_STATION_CAPTURE

		Phase.SYSTEM_2_STATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("24_station_with_construction")
				_polls = 0
				_phase = Phase.SETUP_AUTOMATION

		Phase.SETUP_AUTOMATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_automation()
				_switch_dock_tab(4)  # Intel tab
				_polls = 0
				_phase = Phase.SYSTEM_2_INTEL_CAPTURE

		Phase.SYSTEM_2_INTEL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("25_intel_with_automation")
				_polls = 0
				_phase = Phase.SYSTEM_2_UNDOCK

		Phase.SYSTEM_2_UNDOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.WARP_3_COOLDOWN

		# ── Warp to System 3 ──────────────────────────────────
		Phase.WARP_3_COOLDOWN:
			_polls += 1
			if _polls >= 150:  # Lane cooldown ~2.5s
				_polls = 0
				_phase = Phase.WARP_3_TRIGGER

		Phase.WARP_3_TRIGGER:
			if _game_manager == null or _neighbor_ids.size() < 2:
				print(PREFIX + "WARN|no_second_neighbor, skipping system_3")
				_phase = Phase.NPC_ZOOM_IN
			else:
				_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[1])
				print(PREFIX + "WARP_TRIGGER|%s" % _neighbor_ids[1])
				_polls = 0
				_phase = Phase.WARP_3_WAIT

		Phase.WARP_3_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_polls = 0
				_phase = Phase.WARP_3_REBUILD

		Phase.WARP_3_REBUILD:
			_force_arrival(_neighbor_ids[1])
			_rebuild_local_system(_neighbor_ids[1])
			_polls = 0
			_phase = Phase.SYSTEM_3_DOCK

		Phase.SYSTEM_3_DOCK:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.SYSTEM_3_MARKET_CAPTURE

		Phase.SYSTEM_3_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("26_system_3_market")
				_polls = 0
				_phase = Phase.SYSTEM_3_UNDOCK

		Phase.SYSTEM_3_UNDOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.NPC_ZOOM_IN

		# ── NPC Close-up ──────────────────────────────────────
		Phase.NPC_ZOOM_IN:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_set_camera_distance(20.0)
				_polls = 0
				_phase = Phase.NPC_APPROACH

		Phase.NPC_APPROACH:
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_approach_nearest_npc()
				_polls = 0
				_phase = Phase.NPC_CLOSEUP_CAPTURE

		Phase.NPC_CLOSEUP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_capture("27_npc_closeup")
				_polls = 0
				_phase = Phase.NPC_ZOOM_OUT

		Phase.NPC_ZOOM_OUT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_camera_distance(80.0)
				_polls = 0
				_phase = Phase.WRITE_SUMMARY

		# ── Done ──────────────────────────────────────────────
		Phase.WRITE_SUMMARY:
			_write_summary()
			print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
			_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# --- Navigation helpers ---

func _init_navigation() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var lanes: Array = galaxy.get("lane_edges", [])
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


func _dock_at_current_station() -> void:
	if _game_manager == null:
		return
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_game_manager.call("on_proximity_dock_entered_v0", targets[0])
		print(PREFIX + "DOCK|%s" % str(targets[0].name))


func _undock() -> void:
	if _game_manager != null and _game_manager.has_method("undock_v0"):
		_game_manager.call("undock_v0")
		print(PREFIX + "UNDOCK")


func _toggle_galaxy_map() -> void:
	if _game_manager != null and _game_manager.has_method("toggle_galaxy_map_overlay_v0"):
		_game_manager.call("toggle_galaxy_map_overlay_v0")


func _toggle_empire_dashboard() -> void:
	if _game_manager != null:
		_game_manager.call("_toggle_empire_dashboard_v0")


func _force_arrival(node_id: String) -> void:
	if _game_manager != null and _game_manager.has_method("on_lane_arrival_v0"):
		_game_manager.call("on_lane_arrival_v0", node_id)
		print(PREFIX + "FORCE_ARRIVAL|%s" % node_id)


func _toggle_panel(method_name: String) -> void:
	if _game_manager != null and _game_manager.has_method(method_name):
		_game_manager.call(method_name)
		print(PREFIX + "PANEL_TOGGLE|%s" % method_name)
	else:
		print(PREFIX + "WARN|panel_method_missing|%s" % method_name)


func _switch_dock_tab(idx: int) -> void:
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm != null and htm.has_method("_switch_dock_tab"):
		htm.call("_switch_dock_tab", idx)


func _set_v2_overlay(mode: int) -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv and gv.has_method("SetV2OverlayModeV0"):
		gv.call("SetV2OverlayModeV0", mode)
		print(PREFIX + "V2_OVERLAY|%d" % mode)


func _set_camera_distance(dist: float) -> void:
	var cam_ctrl = root.find_child("PlayerFollowCamera", true, false)
	if cam_ctrl:
		cam_ctrl.set("flight_follow_distance", dist)
		var offset := Vector3(0, dist, dist * 0.05)
		cam_ctrl.set("flight_offset", offset)
		print(PREFIX + "CAMERA|dist=%s" % str(dist))


func _get_hero_body():
	var players = get_nodes_in_group("Player")
	if players.size() > 0:
		return players[0]
	return null


func _approach_nearest_npc() -> void:
	var hero = _get_hero_body()
	if hero == null:
		print(PREFIX + "WARN|no_hero_body")
		return
	var npcs = get_nodes_in_group("NpcShip")
	if npcs.is_empty():
		npcs = get_nodes_in_group("FleetShip")
	if npcs.is_empty():
		print(PREFIX + "WARN|no_npc_ships")
		return
	var nearest = npcs[0]
	var best_dist: float = hero.global_position.distance_to(nearest.global_position)
	for npc in npcs:
		var d: float = hero.global_position.distance_to(npc.global_position)
		if d < best_dist:
			best_dist = d
			nearest = npc
	var dir: Vector3 = (hero.global_position - nearest.global_position).normalized()
	if dir.length_squared() < 0.01:
		dir = Vector3(1, 0, 0)
	hero.global_position = nearest.global_position + dir * 10.0
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	print(PREFIX + "NPC_APPROACH|%s" % str(nearest.name))


# --- State setup helpers (populate UI before capture) ---

func _setup_refit() -> void:
	if _bridge == null or not _bridge.has_method("GetPlayerFleetSlotsV0"):
		print(PREFIX + "WARN|setup_refit_no_method")
		return
	var fleet_id := "fleet_trader_1"
	var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
	var avail: Array = _bridge.call("GetAvailableModulesV0")
	for i in range(slots.size()):
		var slot: Dictionary = slots[i]
		var slot_kind: String = str(slot.get("slot_kind", ""))
		var current_id: String = str(slot.get("installed_module_id", ""))
		if not current_id.is_empty():
			_bridge.call("RemoveModuleV0", fleet_id, i)
		for mod in avail:
			var mod_id: String = str(mod.get("module_id", ""))
			if str(mod.get("slot_kind", "")) == slot_kind and mod_id != current_id:
				var result: Dictionary = _bridge.call("InstallModuleV0", fleet_id, i, mod_id)
				if bool(result.get("success", false)):
					print(PREFIX + "SETUP_REFIT|slot=%d installed=%s" % [i, mod_id])
					return
	print(PREFIX + "WARN|setup_refit_failed")


func _setup_construction() -> void:
	if _bridge == null or not _bridge.has_method("GetAvailableConstructionDefsV0"):
		print(PREFIX + "WARN|setup_construction_no_method")
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	var defs: Array = _bridge.call("GetAvailableConstructionDefsV0")
	for d in defs:
		var def_id: String = str(d.get("def_id", ""))
		var reason: String = str(_bridge.call("GetConstructionBlockReasonV0", def_id, node_id))
		if reason.is_empty() or reason == "none" or reason == "<null>":
			var result: Dictionary = _bridge.call("StartConstructionV0", def_id, node_id)
			print(PREFIX + "SETUP_CONSTRUCTION|def=%s ok=%s" % [def_id, str(result.get("success", false))])
			return
	print(PREFIX + "WARN|setup_construction_all_blocked")


func _setup_automation() -> void:
	if _bridge == null or not _bridge.has_method("CreateAutoBuyProgram"):
		print(PREFIX + "WARN|setup_automation_no_method")
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	for item in market:
		if int(item.get("quantity", 0)) > 0:
			var good_id: String = str(item.get("good_id", ""))
			var pid: String = str(_bridge.call("CreateAutoBuyProgram", node_id, good_id, 1, 30))
			print(PREFIX + "SETUP_AUTOMATION|good=%s pid=%s" % [good_id, pid])
			return
	print(PREFIX + "WARN|setup_automation_no_goods")


func _setup_mission() -> void:
	if _bridge == null or not _bridge.has_method("GetMissionListV0"):
		return
	if _bridge.has_method("GetActiveMissionV0"):
		var active: Dictionary = _bridge.call("GetActiveMissionV0")
		if not str(active.get("mission_id", "")).is_empty():
			return
	var missions: Array = _bridge.call("GetMissionListV0")
	for m in missions:
		var mid: String = str(m.get("mission_id", ""))
		if not mid.is_empty():
			_bridge.call("AcceptMissionV0", mid)
			print(PREFIX + "SETUP_MISSION|%s" % mid)
			return


# --- Capture helpers ---

func _capture(label: String) -> void:
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]
	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)
	_snapshots.append({"label": label, "tick": tick, "path": img_path})
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _write_summary() -> void:
	var summary := {
		"bot": "ui_screenshot_bot_v0",
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
	}
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var f := FileAccess.open(OUTPUT_DIR.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()


func _fail(reason: String) -> void:
	print(PREFIX + "FAIL|%s" % reason)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
