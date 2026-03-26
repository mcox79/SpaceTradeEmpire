extends SceneTree

## Visual Sweep Bot v6 — Consolidated screenshot bot with mode selection.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Modes (via --mode CLI arg):
##   fast         — 4 captures: boot, dock market, galaxy map, final (~15s)
##   onboarding   — 11 milestone captures tracking progressive disclosure (~60s)
##   ui_complete  — Exhaustive UI tab/panel/overlay sweep, 27+ captures (~90s)
##   full         — Comprehensive sweep with NPC, burst, time-advance (~60s) [default]
##
## Usage:
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/visual_sweep_bot_v0.gd" -- --mode fast
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/visual_sweep_bot_v0.gd" -- --mode onboarding
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/visual_sweep_bot_v0.gd" -- --mode ui_complete
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/visual_sweep_bot_v0.gd"

const PREFIX := "VSWP|"
const MAX_POLLS := 600

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
const AuditScript = preload("res://scripts/tools/aesthetic_audit.gd")

## --- Timing constants (frames at ~60fps) ---
## These are deliberately generous to let the engine fully render.
const SETTLE_SCENE := 60       # After scene load / system rebuild (1.0s)
const SETTLE_CAMERA := 45      # After camera distance change (0.75s)
const SETTLE_UI := 20          # After UI overlay toggle (0.33s)
const SETTLE_TAB := 15         # After dock tab switch (0.25s)
const SETTLE_ACTION := 15      # After game action (dock, undock, buy)
const POST_CAPTURE := 8        # Recovery after PNG save stutter

enum Phase {
	# --- Setup (all modes) ---
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	# --- Home System (full + fast + onboarding + ui_complete) ---
	BOOT,                      # 1. Flight view: star, planets, station, NPC fleets, HUD
	DOCK_ENTER,
	DOCK_MARKET_CAPTURE,       # 2. Market tab: goods, prices, station name

	# --- Dock tab sweep (full + ui_complete) ---
	DOCK_JOBS_SWITCH,
	DOCK_JOBS_CAPTURE,         # 3. Jobs tab: missions, automation
	DOCK_SERVICES_SWITCH,
	DOCK_SERVICES_CAPTURE,     # 4. Services tab: refit, maintenance, research

	# --- ui_complete extra dock tabs ---
	UI_TAB_INTEL_SWITCH,
	UI_TAB_INTEL_CAPTURE,      # Intel tab
	UI_TAB_DIPLOMACY_SWITCH,
	UI_TAB_DIPLOMACY_CAPTURE,  # Diplomacy tab

	# --- Trade (full + onboarding) ---
	BUY_GOOD,
	POST_BUY_CAPTURE,          # 5. Market tab with updated quantities after purchase
	UNDOCK_1,
	FLIGHT_CARGO_CAPTURE,      # 6. Flight HUD with cargo loaded

	# --- NPC Showcase (full only) ---
	NPC_ZOOM_IN,               # Lower camera to 20u for close-up
	NPC_APPROACH,              # Teleport hero near nearest NPC ship
	NPC_CLOSEUP_CAPTURE,       # 7. NPC close-up: role label (T/H/P), 3D ship model
	NPC_DAMAGE,                # Apply damage hits to nearby NPC
	NPC_COMBAT_BURST,          # 8. Burst: 3 frames showing HP bar + stagger
	NPC_WARP_VFX,              # Spawn WarpEffect.play_warp_in near hero
	NPC_WARP_VFX_BURST,        # 9. Burst: 4 frames of flash sphere shrinking + particles
	NPC_ZOOM_OUT,              # Restore camera to normal height

	# --- Overlays (full + fast galaxy map + ui_complete) ---
	OPEN_GALAXY_MAP,
	GALAXY_MAP_CAPTURE,        # 10. Galaxy map: network graph, node colors, YOU indicator

	# V2 overlay captures (full + ui_complete)
	V2_FACTION_SET,
	V2_FACTION_CAPTURE,        # 10b. Galaxy map V2 faction territory overlay
	V2_FLEET_SET,              # ui_complete only
	V2_FLEET_CAPTURE,          # ui_complete: fleet overlay
	V2_HEAT_SET,               # ui_complete only
	V2_HEAT_CAPTURE,           # ui_complete: heat overlay
	V2_EXPLORATION_SET,
	V2_EXPLORATION_CAPTURE,    # 10c. Galaxy map V2 exploration overlay
	V2_WARFRONT_SET,
	V2_WARFRONT_CAPTURE,       # 10d. Galaxy map V2 warfront overlay
	V2_OFF_SET,
	CLOSE_GALAXY_MAP,

	# Empire dashboard (full + ui_complete)
	OPEN_EMPIRE_DASH,
	EMPIRE_DASH_CAPTURE,       # 11. Empire dashboard overlay
	CLOSE_EMPIRE_DASH,

	# --- fast: Final (after galaxy map close) ---
	FAST_FINAL_CAPTURE,

	# --- Warp to System 2 (full + onboarding + ui_complete) ---
	WARP_2_TRIGGER,
	WARP_2_TRANSIT_BURST,      # 12. Burst: 3 frames of warp flash fading (full only)
	WARP_2_WAIT,
	WARP_2_REBUILD,
	SYSTEM_2_CAPTURE,          # 13. System 2: different star type

	# --- Onboarding: arrival + HUD captures ---
	ONBOARD_ARRIVAL_2_CAPTURE,   # FO arrival dialogue
	ONBOARD_ARRIVAL_2_HUD,       # HUD with security label

	# --- System 2 dock (full + ui_complete) ---
	SYSTEM_2_DOCK,
	SYSTEM_2_MARKET_CAPTURE,   # 14. System 2 market

	# --- ui_complete: refit/construction/automation setup ---
	UI_SETUP_REFIT,
	UI_SYSTEM_2_SHIP_CAPTURE,    # Ship tab with equipped module
	UI_SETUP_CONSTRUCTION,
	UI_SYSTEM_2_STATION_CAPTURE, # Station tab with construction in progress
	UI_SETUP_AUTOMATION,
	UI_SYSTEM_2_INTEL_CAPTURE,   # Intel tab with active automation

	# --- full: refit + ship tab ---
	SETUP_REFIT,               # Install a module so Ship tab shows equipped state
	SYSTEM_2_SHIP_TAB_SWITCH,  # Switch to Ship tab (unlocked after combat)
	SYSTEM_2_SHIP_TAB_CAPTURE, # 15. Ship tab: refit, maintenance, modules
	SYSTEM_2_UNDOCK,

	# --- Onboarding: dock system 2 ---
	ONBOARD_DOCK_2,
	ONBOARD_DOCK_2_SETTLE,
	ONBOARD_DOCK_2_CAPTURE,
	ONBOARD_UNDOCK_2,

	# --- Warp to System 3 (full + onboarding + ui_complete) ---
	WARP_3_COOLDOWN,
	WARP_3_TRIGGER,
	WARP_3_WAIT,
	WARP_3_REBUILD,
	SYSTEM_3_CAPTURE,          # 15. System 3: dock for variety (full)
	SYSTEM_3_DOCK_CAPTURE,     # 15b. System 3 market (full)

	# --- Onboarding: system 3 full tabs ---
	ONBOARD_DOCK_3,
	ONBOARD_DOCK_3_SETTLE,
	ONBOARD_DOCK_3_FULL_TABS_CAPTURE,

	# --- full: construction/automation/intel ---
	SETUP_CONSTRUCTION,            # Start a construction project
	SYSTEM_3_STATION_TAB_SWITCH,   # Switch to Station tab
	SYSTEM_3_STATION_TAB_CAPTURE,  # 16. Station tab: research, construction
	SETUP_AUTOMATION,              # Create an auto-buy program
	SYSTEM_3_INTEL_TAB_SWITCH,     # Switch to Intel tab
	SYSTEM_3_INTEL_TAB_CAPTURE,    # 17. Intel tab: trade routes, automation
	SYSTEM_3_UNDOCK,

	# --- ui_complete: System 3 dock/market ---
	UI_SYSTEM_3_DOCK,
	UI_SYSTEM_3_MARKET_CAPTURE,
	UI_SYSTEM_3_UNDOCK,

	# --- Overlay Panels (full + ui_complete) ---
	SETUP_MISSION,                 # Accept a mission so journal shows active entry
	OPEN_MISSION_JOURNAL,
	MISSION_JOURNAL_CAPTURE,       # 18. Mission journal (J key)
	CLOSE_MISSION_JOURNAL,
	OPEN_KNOWLEDGE_WEB,
	KNOWLEDGE_WEB_CAPTURE,         # 19. Knowledge web (K key)
	CLOSE_KNOWLEDGE_WEB,
	OPEN_COMBAT_LOG,
	COMBAT_LOG_CAPTURE,            # 20. Combat log (L key)
	CLOSE_COMBAT_LOG,

	# --- ui_complete extra panels ---
	UI_OPEN_DATA_LOG,
	UI_DATA_LOG_CAPTURE,           # Data log
	UI_CLOSE_DATA_LOG,
	UI_OPEN_FO_PANEL,
	UI_FO_PANEL_CAPTURE,           # First Officer panel (F key)
	UI_CLOSE_FO_PANEL,
	UI_OPEN_WARFRONT_DASH,
	UI_WARFRONT_DASH_CAPTURE,      # Warfront dashboard (N key)
	UI_CLOSE_WARFRONT_DASH,

	# --- ui_complete: NPC close-up ---
	UI_NPC_ZOOM_IN,
	UI_NPC_APPROACH,
	UI_NPC_CLOSEUP_CAPTURE,
	UI_NPC_ZOOM_OUT,

	# --- Dock Panels (full: T49) ---
	DOCK_PANEL_REDOCK,             # Re-dock at nearest station for panel captures
	DOCK_PANEL_HAVEN,
	DOCK_PANEL_HAVEN_CAPTURE,      # 21. Haven panel
	DOCK_PANEL_WARFRONT,
	DOCK_PANEL_WARFRONT_CAPTURE,   # 22. Warfront panel
	DOCK_PANEL_DOCTRINE,
	DOCK_PANEL_DOCTRINE_CAPTURE,   # 23. Doctrine panel
	DOCK_PANEL_BUDGET,
	DOCK_PANEL_BUDGET_CAPTURE,     # 24. Budget/ledger panel
	DOCK_PANEL_NARRATIVE,
	DOCK_PANEL_NARRATIVE_CAPTURE,  # 25. Narrative/story panel
	DOCK_PANEL_MEGAPROJECT,
	DOCK_PANEL_MEGAPROJECT_CAPTURE,# 26. Megaproject panel
	DOCK_PANEL_UNDOCK,

	# --- Flight + Endstate (full: T49 Tier 2) ---
	FLIGHT_SCANNER_HUD,
	FLIGHT_SCANNER_CAPTURE,        # 27. Scanner HUD overlay
	FLIGHT_DATA_LOG,
	FLIGHT_DATA_LOG_CAPTURE,       # 28. Data log panel
	ENDSTATE_LOSS,
	ENDSTATE_LOSS_CAPTURE,         # 29. Loss screen
	ENDSTATE_VICTORY,
	ENDSTATE_VICTORY_CAPTURE,      # 30. Victory screen
	FLIGHT_PAUSE,
	FLIGHT_PAUSE_CAPTURE,          # 31. Pause/settings screen

	# --- Time Advancement (full only) ---
	WAIT_TICK_200,
	TICK_200_ZOOM_IN,          # Zoom in on a planet for close-up variety
	TICK_200_CAPTURE,          # Close-up planet/station view at tick 200

	# --- Final ---
	FINAL_ZOOM_OUT,            # Restore camera for wide final shot
	FINAL,                     # Aesthetic audit + final capture

	# --- Onboarding: final ---
	ONBOARD_FINAL,

	# --- ui_complete: summary ---
	UI_WRITE_SUMMARY,

	DONE
}

## --- Mode: "full" (default), "fast", "onboarding", "ui_complete" ---
var _mode := "full"
var _output_dir := "res://reports/visual_eval/full/"

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _bridge = null
var _game_manager = null
var _total_frames := 0
var _max_frames := 3600  # 60s at 60fps — safety exit (adjusted per mode)

var _observer = null
var _screenshot = null
var _audit = null

# Navigation state
var _home_node_id := ""
var _neighbor_ids: Array = []
var _trade_good := ""
var _snapshots: Array = []  # [{phase, tick, screenshot, report}]

# Burst capture state
var _burst_label := ""
var _burst_remaining := 0
var _burst_spacing := 0
var _burst_frame := 0
var _after_burst_phase: Phase = Phase.DONE


func _initialize() -> void:
	# Parse --mode from CLI arguments
	var args := OS.get_cmdline_user_args()
	for i in range(args.size()):
		if args[i] == "--mode" and i + 1 < args.size():
			_mode = args[i + 1]
	# Validate mode
	if _mode not in ["full", "fast", "onboarding", "ui_complete"]:
		print(PREFIX + "WARN|unknown_mode=%s, defaulting to full" % _mode)
		_mode = "full"
	# Set output directory per mode
	_output_dir = "res://reports/visual_eval/" + _mode + "/"
	# Adjust timeout per mode
	match _mode:
		"fast":
			_max_frames = 900  # 15s
		"onboarding":
			_max_frames = 3600  # 60s
		"ui_complete":
			_max_frames = 5400  # 90s
		"full":
			_max_frames = 3600  # 60s
	print(PREFIX + "MODE_SELECT|" + _mode)
	print(PREFIX + "START|output=%s" % _output_dir)


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= _max_frames and _phase != Phase.DONE:
		print(PREFIX + "TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.DONE

	# Skip phases not relevant to current mode
	if _phase > Phase.WAIT_LOCAL_SYSTEM and _phase != Phase.DONE and _should_skip_phase(_phase):
		_phase = _next_phase(_phase)
		_polls = 0
		return false

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
				_observer = ObserverScript.new()
				_observer.init_v0(self)
				_screenshot = ScreenshotScript.new()
				_audit = AuditScript.new()
				_game_manager = root.get_node_or_null("GameManager")
				if _game_manager:
					_game_manager.set("_on_main_menu", false)
				# Dismiss first-launch accessibility panel if present (layer 130).
				for node in root.get_children():
					if node is CanvasLayer and node.has_method("_on_continue_pressed"):
						node.call("_on_continue_pressed")
						break
				_init_navigation()
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			if get_nodes_in_group("Station").size() > 0:
				_polls = 0
				_phase = Phase.BOOT
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_phase = Phase.BOOT

		# ── Home System ───────────────────────────────────────
		Phase.BOOT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				if _mode == "onboarding":
					_log_onboarding_state("boot")
				_capture("boot")
				_polls = 0
				_phase = Phase.DOCK_ENTER

		Phase.DOCK_ENTER:
			_polls += 1
			if _polls >= POST_CAPTURE:
				if _mode == "onboarding":
					_do_dock(_home_node_id)
				else:
					_dock_at_current_station()
					# Promote FO so Port Briefing has an FO greeting on station tab.
					if _mode == "full":
						_promote_fo()
				_polls = 0
				_phase = Phase.DOCK_MARKET_CAPTURE

		Phase.DOCK_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("dock_market")
				_polls = 0
				if _mode == "onboarding":
					# Check hidden tabs after first dock
					_log_tab_visibility("first_dock")
					_log_onboarding_state("first_dock")
					_capture("dock_tabs_hidden")
					_phase = Phase.BUY_GOOD
				elif _mode == "fast":
					# fast: undock for galaxy map
					_phase = Phase.UNDOCK_1
				else:
					_phase = Phase.DOCK_JOBS_SWITCH

		# ── Dock tab sweep (full + ui_complete) ───────────────
		Phase.DOCK_JOBS_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(1)
				_polls = 0
				_phase = Phase.DOCK_JOBS_CAPTURE

		Phase.DOCK_JOBS_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("dock_jobs")
				_polls = 0
				_phase = Phase.DOCK_SERVICES_SWITCH

		Phase.DOCK_SERVICES_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(2)
				_polls = 0
				_phase = Phase.DOCK_SERVICES_CAPTURE

		Phase.DOCK_SERVICES_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("dock_services")
				_polls = 0
				if _mode == "ui_complete":
					_phase = Phase.UI_TAB_INTEL_SWITCH
				else:
					_phase = Phase.BUY_GOOD

		# ── ui_complete extra dock tabs ───────────────────────
		Phase.UI_TAB_INTEL_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(4)
				_polls = 0
				_phase = Phase.UI_TAB_INTEL_CAPTURE

		Phase.UI_TAB_INTEL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("tab_intel")
				_polls = 0
				_phase = Phase.UI_TAB_DIPLOMACY_SWITCH

		Phase.UI_TAB_DIPLOMACY_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(6)
				_polls = 0
				_phase = Phase.UI_TAB_DIPLOMACY_CAPTURE

		Phase.UI_TAB_DIPLOMACY_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("tab_diplomacy")
				_polls = 0
				_phase = Phase.UNDOCK_1

		# ── Trade (full + onboarding) ─────────────────────────
		Phase.BUY_GOOD:
			_polls += 1
			if _polls >= POST_CAPTURE:
				if _mode == "onboarding":
					_onboard_try_buy_good()
				else:
					_try_buy_good()
					_switch_dock_tab(0)
				_polls = 0
				_phase = Phase.POST_BUY_CAPTURE

		Phase.POST_BUY_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				if _mode == "onboarding":
					# Sell good for onboarding milestone
					_onboard_try_sell_good()
					_log_onboarding_state("post_trade")
					_capture("post_trade_market")
					# Check tabs after trade
					_switch_dock_tab(1)
				elif not _trade_good.is_empty():
					_capture("post_buy")
				_polls = 0
				_phase = Phase.UNDOCK_1

		Phase.UNDOCK_1:
			_polls += 1
			if _polls >= POST_CAPTURE:
				if _mode == "onboarding":
					_log_tab_visibility("post_trade")
					_capture("post_trade_jobs_tab")
					_game_manager.call("undock_v0")
				else:
					_undock()
				_polls = 0
				_phase = Phase.FLIGHT_CARGO_CAPTURE

		Phase.FLIGHT_CARGO_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				if _mode == "onboarding":
					_log_onboarding_state("undocked")
					_capture("flight_hud_post_trade")
				else:
					_capture("flight_cargo")
				_polls = 0
				if _mode == "fast":
					_phase = Phase.OPEN_GALAXY_MAP
				elif _mode == "full":
					_phase = Phase.NPC_ZOOM_IN
				else:
					# onboarding + ui_complete skip NPC showcase
					_phase = Phase.OPEN_GALAXY_MAP

		# ── NPC Showcase (full only) ──────────────────────────
		Phase.NPC_ZOOM_IN:
			_polls += 1
			if _polls >= POST_CAPTURE:
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
			# Long wait: camera lerp + label visibility update
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_capture("npc_closeup")
				_polls = 0
				_phase = Phase.NPC_DAMAGE

		Phase.NPC_DAMAGE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Hit nearest NPC enough to show HP bar but not destroy (3x15=45 dmg)
				_damage_nearest_npc(3, 15)
				# Force nearest NPC into ENGAGE state so HP bar renders
				_force_npc_engage()
				_polls = 0
				_start_burst("npc_combat", 3, 20, Phase.NPC_WARP_VFX)
				_phase = Phase.NPC_COMBAT_BURST

		Phase.NPC_COMBAT_BURST:
			_process_burst()

		Phase.NPC_WARP_VFX:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_spawn_warp_in_vfx()
				_polls = 0
				# Burst: 4 frames at spacing 6 — catches flash at 4x, 3x, 2x, 1x scale
				_start_burst("warp_vfx", 4, 6, Phase.NPC_ZOOM_OUT)
				_phase = Phase.NPC_WARP_VFX_BURST

		Phase.NPC_WARP_VFX_BURST:
			_process_burst()

		Phase.NPC_ZOOM_OUT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_camera_distance(80.0)
				_polls = 0
				_phase = Phase.OPEN_GALAXY_MAP

		# ── Galaxy Map Overlays ───────────────────────────────
		Phase.OPEN_GALAXY_MAP:
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_toggle_galaxy_map()
				# Force camera to strategic altitude immediately
				_force_strategic_altitude()
				_polls = 0
				_phase = Phase.GALAXY_MAP_CAPTURE

		Phase.GALAXY_MAP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("galaxy_map")
				_polls = 0
				if _mode == "fast":
					_phase = Phase.CLOSE_GALAXY_MAP
				else:
					_phase = Phase.V2_FACTION_SET

		# V2 overlay captures — cycle faction, exploration, warfront
		Phase.V2_FACTION_SET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(1)  # Faction
				_polls = 0
				_phase = Phase.V2_FACTION_CAPTURE

		Phase.V2_FACTION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("galaxy_v2_faction")
				_polls = 0
				if _mode == "ui_complete":
					_phase = Phase.V2_FLEET_SET
				else:
					_phase = Phase.V2_EXPLORATION_SET

		# ui_complete extra overlays: fleet, heat
		Phase.V2_FLEET_SET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(2)  # Fleet
				_polls = 0
				_phase = Phase.V2_FLEET_CAPTURE

		Phase.V2_FLEET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("galaxy_v2_fleet")
				_polls = 0
				_phase = Phase.V2_HEAT_SET

		Phase.V2_HEAT_SET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(3)  # Heat
				_polls = 0
				_phase = Phase.V2_HEAT_CAPTURE

		Phase.V2_HEAT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("galaxy_v2_heat")
				_polls = 0
				_phase = Phase.V2_EXPLORATION_SET

		Phase.V2_EXPLORATION_SET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(4)  # Exploration
				_polls = 0
				_phase = Phase.V2_EXPLORATION_CAPTURE

		Phase.V2_EXPLORATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("galaxy_v2_exploration")
				_polls = 0
				_phase = Phase.V2_WARFRONT_SET

		Phase.V2_WARFRONT_SET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(5)  # Warfront
				_polls = 0
				_phase = Phase.V2_WARFRONT_CAPTURE

		Phase.V2_WARFRONT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("galaxy_v2_warfront")
				_polls = 0
				_phase = Phase.V2_OFF_SET

		Phase.V2_OFF_SET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_v2_overlay(0)  # Off
				_polls = 0
				_phase = Phase.CLOSE_GALAXY_MAP

		Phase.CLOSE_GALAXY_MAP:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_galaxy_map()
				_polls = 0
				if _mode == "fast":
					_phase = Phase.FAST_FINAL_CAPTURE
				elif _mode == "onboarding":
					_phase = Phase.WARP_2_TRIGGER
				else:
					_phase = Phase.OPEN_EMPIRE_DASH

		Phase.OPEN_EMPIRE_DASH:
			_polls += 1
			if _polls >= SETTLE_UI:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.EMPIRE_DASH_CAPTURE

		Phase.EMPIRE_DASH_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("empire_dashboard")
				_polls = 0
				_phase = Phase.CLOSE_EMPIRE_DASH

		Phase.CLOSE_EMPIRE_DASH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.WARP_2_TRIGGER

		# ── fast: Final ───────────────────────────────────────
		Phase.FAST_FINAL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				var report = _observer.capture_full_report_v0()
				var audit_results = _audit.run_audit_v0(report)
				var critical_fails = _audit.count_critical_failures_v0(audit_results)
				for ar in audit_results:
					var status = "PASS" if ar.get("passed", false) else "FAIL"
					print(PREFIX + "AESTHETIC|%s|%s|%s|%s" % [
						status, str(ar.get("flag", "")),
						str(ar.get("severity", "")), str(ar.get("detail", ""))])
				print(PREFIX + "AESTHETIC_CRITICAL_FAILS|%d" % critical_fails)
				_capture("final")
				_write_summary(audit_results, critical_fails)
				if critical_fails == 0:
					print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
				else:
					print(PREFIX + "FAIL|critical=%d screenshots=%d" % [critical_fails, _snapshots.size()])
				_phase = Phase.DONE

		# ── Warp to System 2 ──────────────────────────────────
		Phase.WARP_2_TRIGGER:
			_polls += 1
			if _polls >= SETTLE_UI:
				if _game_manager == null:
					print(PREFIX + "WARN|no_game_manager, skipping warp_2")
					if _mode == "full":
						_phase = Phase.WAIT_TICK_200
					else:
						_phase = Phase.DONE
				elif _neighbor_ids.size() >= 1:
					if _mode == "onboarding":
						var dest: String = _neighbor_ids[0]
						_game_manager.call("on_lane_gate_proximity_entered_v0", dest)
						_game_manager.call("on_lane_arrival_v0", dest)
						print(PREFIX + "WARP|dest=%s" % dest)
						_polls = 0
						_phase = Phase.WARP_2_WAIT
					else:
						_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[0])
						print(PREFIX + "WARP_TRIGGER|%s" % _neighbor_ids[0])
						_polls = 0
						if _mode == "full":
							# Burst: 3 frames at spacing 4 — catches flash peak, mid, fade
							_start_burst("warp_transit", 3, 4, Phase.WARP_2_WAIT)
							_phase = Phase.WARP_2_TRANSIT_BURST
						else:
							_phase = Phase.WARP_2_WAIT
				else:
					print(PREFIX + "WARN|no_neighbors, skipping warp")
					if _mode == "full":
						_phase = Phase.WAIT_TICK_200
					else:
						_phase = Phase.DONE

		Phase.WARP_2_TRANSIT_BURST:
			_process_burst()

		Phase.WARP_2_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_polls = 0
				_phase = Phase.WARP_2_REBUILD

		Phase.WARP_2_REBUILD:
			_force_arrival(_neighbor_ids[0])
			_rebuild_local_system(_neighbor_ids[0])
			_polls = 0
			_phase = Phase.SYSTEM_2_CAPTURE

		Phase.SYSTEM_2_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_capture("system_2")
				_polls = 0
				if _mode == "onboarding":
					_phase = Phase.ONBOARD_ARRIVAL_2_CAPTURE
				else:
					_phase = Phase.SYSTEM_2_DOCK

		# ── Onboarding: arrival captures ──────────────────────
		Phase.ONBOARD_ARRIVAL_2_CAPTURE:
			_log_onboarding_state("arrival_sys2")
			_capture("arrival_system2")
			_polls = 0
			_phase = Phase.ONBOARD_ARRIVAL_2_HUD

		Phase.ONBOARD_ARRIVAL_2_HUD:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("arrival_hud")
				_polls = 0
				_phase = Phase.ONBOARD_DOCK_2

		# ── System 2 dock (full + ui_complete) ────────────────
		Phase.SYSTEM_2_DOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.SYSTEM_2_MARKET_CAPTURE

		Phase.SYSTEM_2_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("system_2_dock")
				_polls = 0
				if _mode == "ui_complete":
					_phase = Phase.UI_SETUP_REFIT
				else:
					_phase = Phase.SETUP_REFIT

		# ── ui_complete: refit/construction/automation at system 2 ──
		Phase.UI_SETUP_REFIT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_refit()
				_switch_dock_tab(2)  # Ship tab
				_polls = 0
				_phase = Phase.UI_SYSTEM_2_SHIP_CAPTURE

		Phase.UI_SYSTEM_2_SHIP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("ship_with_module")
				_polls = 0
				_phase = Phase.UI_SETUP_CONSTRUCTION

		Phase.UI_SETUP_CONSTRUCTION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_construction()
				_switch_dock_tab(3)  # Station tab
				_polls = 0
				_phase = Phase.UI_SYSTEM_2_STATION_CAPTURE

		Phase.UI_SYSTEM_2_STATION_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("station_with_construction")
				_polls = 0
				_phase = Phase.UI_SETUP_AUTOMATION

		Phase.UI_SETUP_AUTOMATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_automation()
				_switch_dock_tab(4)  # Intel tab
				_polls = 0
				_phase = Phase.UI_SYSTEM_2_INTEL_CAPTURE

		Phase.UI_SYSTEM_2_INTEL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("intel_with_automation")
				_polls = 0
				_phase = Phase.SYSTEM_2_UNDOCK

		# ── full: refit + ship tab ────────────────────────────
		Phase.SETUP_REFIT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_refit()
				_polls = 0
				_phase = Phase.SYSTEM_2_SHIP_TAB_SWITCH

		Phase.SYSTEM_2_SHIP_TAB_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(2)  # Ship tab
				_polls = 0
				_phase = Phase.SYSTEM_2_SHIP_TAB_CAPTURE

		Phase.SYSTEM_2_SHIP_TAB_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("ship_tab")
				_polls = 0
				_phase = Phase.SYSTEM_2_UNDOCK

		Phase.SYSTEM_2_UNDOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.WARP_3_COOLDOWN

		# ── Onboarding: dock system 2 ─────────────────────────
		Phase.ONBOARD_DOCK_2:
			var dest2: String = _neighbor_ids[0] if _neighbor_ids.size() >= 1 else _home_node_id
			_do_dock(dest2)
			_polls = 0
			_phase = Phase.ONBOARD_DOCK_2_SETTLE

		Phase.ONBOARD_DOCK_2_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.ONBOARD_DOCK_2_CAPTURE

		Phase.ONBOARD_DOCK_2_CAPTURE:
			_log_tab_visibility("sys2_dock")
			_capture("system2_dock_tabs")
			_polls = 0
			_phase = Phase.ONBOARD_UNDOCK_2

		Phase.ONBOARD_UNDOCK_2:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_game_manager.call("undock_v0")
				_polls = 0
				_phase = Phase.WARP_3_COOLDOWN

		# ── Warp to System 3 ──────────────────────────────────
		Phase.WARP_3_COOLDOWN:
			# Lane cooldown is 2.0s — wait 150 frames (~2.5s at 60fps) for it to expire
			_polls += 1
			if _polls >= 150:
				_polls = 0
				_phase = Phase.WARP_3_TRIGGER

		Phase.WARP_3_TRIGGER:
			if _game_manager == null:
				print(PREFIX + "WARN|no_game_manager, skipping warp_3")
				if _mode == "full":
					_phase = Phase.WAIT_TICK_200
				elif _mode == "ui_complete":
					_phase = Phase.UI_NPC_ZOOM_IN
				else:
					_phase = Phase.ONBOARD_FINAL
			elif _neighbor_ids.size() >= 2:
				if _mode == "onboarding":
					var dest: String = _neighbor_ids[1]
					_game_manager.call("on_lane_gate_proximity_entered_v0", dest)
					_game_manager.call("on_lane_arrival_v0", dest)
					print(PREFIX + "WARP|dest=%s" % dest)
				else:
					_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[1])
					print(PREFIX + "WARP_TRIGGER|%s" % _neighbor_ids[1])
				_polls = 0
				_phase = Phase.WARP_3_WAIT
			else:
				print(PREFIX + "WARN|no_second_neighbor, skipping system_3")
				if _mode == "onboarding":
					# Warp back home as fallback
					if _neighbor_ids.size() >= 1:
						_game_manager.call("on_lane_gate_proximity_entered_v0", _home_node_id)
						_game_manager.call("on_lane_arrival_v0", _home_node_id)
						print(PREFIX + "WARP|dest=%s (home)" % _home_node_id)
					_polls = 0
					_phase = Phase.WARP_3_WAIT
				elif _mode == "full":
					_phase = Phase.WAIT_TICK_200
				elif _mode == "ui_complete":
					_phase = Phase.UI_NPC_ZOOM_IN
				else:
					_phase = Phase.DONE

		Phase.WARP_3_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_polls = 0
				_phase = Phase.WARP_3_REBUILD

		Phase.WARP_3_REBUILD:
			var dest_id: String = _neighbor_ids[1] if _neighbor_ids.size() >= 2 else _home_node_id
			_force_arrival(dest_id)
			_rebuild_local_system(dest_id)
			_polls = 0
			if _mode == "onboarding":
				_phase = Phase.ONBOARD_DOCK_3
			elif _mode == "ui_complete":
				_phase = Phase.UI_SYSTEM_3_DOCK
			else:
				_phase = Phase.SYSTEM_3_CAPTURE

		# ── full: System 3 captures ───────────────────────────
		Phase.SYSTEM_3_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				# Dock at system 3 for variety (different market + star).
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.SYSTEM_3_DOCK_CAPTURE

		Phase.SYSTEM_3_DOCK_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("system_3")
				_polls = 0
				_phase = Phase.SETUP_CONSTRUCTION

		Phase.SETUP_CONSTRUCTION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_construction()
				_polls = 0
				_phase = Phase.SYSTEM_3_STATION_TAB_SWITCH

		Phase.SYSTEM_3_STATION_TAB_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(3)  # Station tab
				_polls = 0
				_phase = Phase.SYSTEM_3_STATION_TAB_CAPTURE

		Phase.SYSTEM_3_STATION_TAB_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("station_tab")
				_polls = 0
				_phase = Phase.SETUP_AUTOMATION

		Phase.SETUP_AUTOMATION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_automation()
				_polls = 0
				_phase = Phase.SYSTEM_3_INTEL_TAB_SWITCH

		Phase.SYSTEM_3_INTEL_TAB_SWITCH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_switch_dock_tab(4)  # Intel tab
				_polls = 0
				_phase = Phase.SYSTEM_3_INTEL_TAB_CAPTURE

		Phase.SYSTEM_3_INTEL_TAB_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_capture("intel_tab")
				_polls = 0
				_phase = Phase.SYSTEM_3_UNDOCK

		Phase.SYSTEM_3_UNDOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.SETUP_MISSION

		# ── Onboarding: system 3 full tabs ────────────────────
		Phase.ONBOARD_DOCK_3:
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var cur_node: String = str(ps.get("current_node_id", _home_node_id))
			_do_dock(cur_node)
			_polls = 0
			_phase = Phase.ONBOARD_DOCK_3_SETTLE

		Phase.ONBOARD_DOCK_3_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.ONBOARD_DOCK_3_FULL_TABS_CAPTURE

		Phase.ONBOARD_DOCK_3_FULL_TABS_CAPTURE:
			_log_onboarding_state("sys3_dock")
			_log_tab_visibility("sys3_dock")
			_capture("system3_full_tabs")
			_polls = 0
			_phase = Phase.ONBOARD_FINAL

		# ── ui_complete: System 3 dock/market ─────────────────
		Phase.UI_SYSTEM_3_DOCK:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.UI_SYSTEM_3_MARKET_CAPTURE

		Phase.UI_SYSTEM_3_MARKET_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_capture("system_3_market")
				_polls = 0
				_phase = Phase.UI_SYSTEM_3_UNDOCK

		Phase.UI_SYSTEM_3_UNDOCK:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_undock()
				_polls = 0
				_phase = Phase.SETUP_MISSION

		# ── Overlay Panels (full + ui_complete) ───────────────
		Phase.SETUP_MISSION:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_setup_mission()
				_polls = 0
				_phase = Phase.OPEN_MISSION_JOURNAL

		Phase.OPEN_MISSION_JOURNAL:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_mission_journal_v0")
				_polls = 0
				_phase = Phase.MISSION_JOURNAL_CAPTURE

		Phase.MISSION_JOURNAL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("mission_journal")
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
				_capture("knowledge_web")
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
				_capture("combat_log")
				_polls = 0
				_phase = Phase.CLOSE_COMBAT_LOG

		Phase.CLOSE_COMBAT_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_combat_log_v0")
				_polls = 0
				if _mode == "ui_complete":
					_phase = Phase.UI_OPEN_DATA_LOG
				else:
					_phase = Phase.DOCK_PANEL_REDOCK

		# ── ui_complete extra panels ──────────────────────────
		Phase.UI_OPEN_DATA_LOG:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_data_log_v0")
				_polls = 0
				_phase = Phase.UI_DATA_LOG_CAPTURE

		Phase.UI_DATA_LOG_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("data_log")
				_polls = 0
				_phase = Phase.UI_CLOSE_DATA_LOG

		Phase.UI_CLOSE_DATA_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_data_log_v0")
				_polls = 0
				_phase = Phase.UI_OPEN_FO_PANEL

		Phase.UI_OPEN_FO_PANEL:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_fo_panel_v0")
				_polls = 0
				_phase = Phase.UI_FO_PANEL_CAPTURE

		Phase.UI_FO_PANEL_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("fo_panel")
				_polls = 0
				_phase = Phase.UI_CLOSE_FO_PANEL

		Phase.UI_CLOSE_FO_PANEL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_fo_panel_v0")
				_polls = 0
				_phase = Phase.UI_OPEN_WARFRONT_DASH

		Phase.UI_OPEN_WARFRONT_DASH:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_toggle_panel("_toggle_warfront_dashboard_v0")
				_polls = 0
				_phase = Phase.UI_WARFRONT_DASH_CAPTURE

		Phase.UI_WARFRONT_DASH_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_UI:
				_capture("warfront_dashboard")
				_polls = 0
				_phase = Phase.UI_CLOSE_WARFRONT_DASH

		Phase.UI_CLOSE_WARFRONT_DASH:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_toggle_panel("_toggle_warfront_dashboard_v0")
				_polls = 0
				_phase = Phase.UI_NPC_ZOOM_IN

		# ── ui_complete: NPC close-up ─────────────────────────
		Phase.UI_NPC_ZOOM_IN:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_set_camera_distance(20.0)
				_polls = 0
				_phase = Phase.UI_NPC_APPROACH

		Phase.UI_NPC_APPROACH:
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_approach_nearest_npc()
				_polls = 0
				_phase = Phase.UI_NPC_CLOSEUP_CAPTURE

		Phase.UI_NPC_CLOSEUP_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_capture("npc_closeup")
				_polls = 0
				_phase = Phase.UI_NPC_ZOOM_OUT

		Phase.UI_NPC_ZOOM_OUT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_camera_distance(80.0)
				_polls = 0
				_phase = Phase.UI_WRITE_SUMMARY

		# ── Dock Panels (full: T49) ───────────────────────────
		Phase.DOCK_PANEL_REDOCK:
			_polls += 1
			if _polls >= 5:
				_dock_at_current_station()
				_polls = 0
				_phase = Phase.DOCK_PANEL_HAVEN

		Phase.DOCK_PANEL_HAVEN:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Haven tab is index 5 in hero_trade_menu
				_switch_dock_tab(5)
				_polls = 0
				_phase = Phase.DOCK_PANEL_HAVEN_CAPTURE

		Phase.DOCK_PANEL_HAVEN_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("dock_haven")
				_polls = 0
				_phase = Phase.DOCK_PANEL_WARFRONT

		Phase.DOCK_PANEL_WARFRONT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Warfront is a HUD overlay, not a dock tab — undock first, toggle
				_undock()
				if _game_manager != null and _game_manager.has_method("_toggle_warfront_dashboard_v0"):
					_game_manager.call("_toggle_warfront_dashboard_v0")
				_polls = 0
				_phase = Phase.DOCK_PANEL_WARFRONT_CAPTURE

		Phase.DOCK_PANEL_WARFRONT_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("panel_warfront")
				# Close warfront
				if _game_manager != null and _game_manager.has_method("_toggle_warfront_dashboard_v0"):
					_game_manager.call("_toggle_warfront_dashboard_v0")
				_polls = 0
				_phase = Phase.DOCK_PANEL_DOCTRINE

		Phase.DOCK_PANEL_DOCTRINE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Doctrine is part of the ship tab (tab 2) — re-dock and show
				_dock_at_current_station()
				_switch_dock_tab(2)
				_polls = 0
				_phase = Phase.DOCK_PANEL_DOCTRINE_CAPTURE

		Phase.DOCK_PANEL_DOCTRINE_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("dock_doctrine")
				_polls = 0
				_phase = Phase.DOCK_PANEL_BUDGET

		Phase.DOCK_PANEL_BUDGET:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Budget/ledger is part of intel tab (tab 4) with automation programs
				_switch_dock_tab(4)
				_polls = 0
				_phase = Phase.DOCK_PANEL_BUDGET_CAPTURE

		Phase.DOCK_PANEL_BUDGET_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("dock_budget_intel")
				_polls = 0
				_phase = Phase.DOCK_PANEL_NARRATIVE

		Phase.DOCK_PANEL_NARRATIVE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Diplomacy tab (index 6) shows narrative/faction interaction
				_switch_dock_tab(6)
				_polls = 0
				_phase = Phase.DOCK_PANEL_NARRATIVE_CAPTURE

		Phase.DOCK_PANEL_NARRATIVE_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("dock_diplomacy")
				_polls = 0
				_phase = Phase.DOCK_PANEL_MEGAPROJECT

		Phase.DOCK_PANEL_MEGAPROJECT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Megaproject is a HUD overlay — undock first
				_undock()
				if _game_manager != null and _game_manager.has_method("_toggle_megaproject_panel_v0"):
					_game_manager.call("_toggle_megaproject_panel_v0")
				_polls = 0
				_phase = Phase.DOCK_PANEL_MEGAPROJECT_CAPTURE

		Phase.DOCK_PANEL_MEGAPROJECT_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("panel_megaproject")
				# Close megaproject
				if _game_manager != null and _game_manager.has_method("_toggle_megaproject_panel_v0"):
					_game_manager.call("_toggle_megaproject_panel_v0")
				_polls = 0
				_phase = Phase.DOCK_PANEL_UNDOCK

		Phase.DOCK_PANEL_UNDOCK:
			_polls += 1
			if _polls >= 5:
				# May already be undocked from warfront/megaproject captures
				_polls = 0
				_phase = Phase.FLIGHT_SCANNER_HUD

		# ── Flight + Endstate (full: T49 Tier 2) ─────────────
		Phase.FLIGHT_SCANNER_HUD:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Toggle scanner HUD if available
				if _game_manager != null and _game_manager.has_method("_toggle_scanner_hud_v0"):
					_game_manager.call("_toggle_scanner_hud_v0")
				_polls = 0
				_phase = Phase.FLIGHT_SCANNER_CAPTURE

		Phase.FLIGHT_SCANNER_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("flight_scanner_hud")
				# Close scanner
				if _game_manager != null and _game_manager.has_method("_toggle_scanner_hud_v0"):
					_game_manager.call("_toggle_scanner_hud_v0")
				_polls = 0
				_phase = Phase.FLIGHT_DATA_LOG

		Phase.FLIGHT_DATA_LOG:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Open data log panel if available
				if _game_manager != null and _game_manager.has_method("_toggle_data_log_v0"):
					_game_manager.call("_toggle_data_log_v0")
				_polls = 0
				_phase = Phase.FLIGHT_DATA_LOG_CAPTURE

		Phase.FLIGHT_DATA_LOG_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("flight_data_log")
				if _game_manager != null and _game_manager.has_method("_toggle_data_log_v0"):
					_game_manager.call("_toggle_data_log_v0")
				_polls = 0
				_phase = Phase.ENDSTATE_LOSS

		Phase.ENDSTATE_LOSS:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Force loss state via bridge
				if _bridge != null and _bridge.has_method("ForceSetGameResultV0"):
					_bridge.call("ForceSetGameResultV0", 2)  # 2 = loss
				_polls = 0
				_phase = Phase.ENDSTATE_LOSS_CAPTURE

		Phase.ENDSTATE_LOSS_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE * 2:  # Extra settle time for endstate UI
				_capture("endstate_loss")
				_polls = 0
				_phase = Phase.ENDSTATE_VICTORY

		Phase.ENDSTATE_VICTORY:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Force victory state via bridge
				if _bridge != null and _bridge.has_method("ForceSetGameResultV0"):
					_bridge.call("ForceSetGameResultV0", 1)  # 1 = victory
				_polls = 0
				_phase = Phase.ENDSTATE_VICTORY_CAPTURE

		Phase.ENDSTATE_VICTORY_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE * 2:
				_capture("endstate_victory")
				# Reset game result to 0 (in progress) to continue normally
				if _bridge != null and _bridge.has_method("ForceSetGameResultV0"):
					_bridge.call("ForceSetGameResultV0", 0)
				_polls = 0
				_phase = Phase.FLIGHT_PAUSE

		Phase.FLIGHT_PAUSE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Toggle pause menu
				if _game_manager != null and _game_manager.has_method("_toggle_pause_menu_v0"):
					_game_manager.call("_toggle_pause_menu_v0")
				_polls = 0
				_phase = Phase.FLIGHT_PAUSE_CAPTURE

		Phase.FLIGHT_PAUSE_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("flight_pause_settings")
				# Close pause menu
				if _game_manager != null and _game_manager.has_method("_toggle_pause_menu_v0"):
					_game_manager.call("_toggle_pause_menu_v0")
				_polls = 0
				_phase = Phase.WAIT_TICK_200

		# ── Time Advancement (full only) ──────────────────────
		Phase.WAIT_TICK_200:
			var tick = _get_tick()
			if tick >= 200:
				_polls = 0
				_phase = Phase.TICK_200_ZOOM_IN
			else:
				_polls += 1
				if _polls >= 300:
					_polls = 0
					_phase = Phase.TICK_200_ZOOM_IN

		Phase.TICK_200_ZOOM_IN:
			# Fly hero toward a planet for a close-up variety shot.
			_set_camera_distance(25.0)
			_approach_nearest_planet()
			_polls = 0
			_phase = Phase.TICK_200_CAPTURE

		Phase.TICK_200_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_CAMERA:
				_capture("tick_200")
				_polls = 0
				_phase = Phase.FINAL_ZOOM_OUT

		Phase.FINAL_ZOOM_OUT:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_set_camera_distance(80.0)
				_polls = 0
				_phase = Phase.FINAL

		# ── Final (full mode) ─────────────────────────────────
		Phase.FINAL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				var report = _observer.capture_full_report_v0()
				var audit_results = _audit.run_audit_v0(report)
				var critical_fails = _audit.count_critical_failures_v0(audit_results)

				for ar in audit_results:
					var status = "PASS" if ar.get("passed", false) else "FAIL"
					print(PREFIX + "AESTHETIC|%s|%s|%s|%s" % [
						status, str(ar.get("flag", "")),
						str(ar.get("severity", "")), str(ar.get("detail", ""))])

				print(PREFIX + "AESTHETIC_CRITICAL_FAILS|%d" % critical_fails)
				_capture("final")

				_write_summary(audit_results, critical_fails)

				if critical_fails == 0:
					print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
				else:
					print(PREFIX + "FAIL|critical=%d screenshots=%d" % [critical_fails, _snapshots.size()])

				_phase = Phase.DONE

		# ── Onboarding: final ─────────────────────────────────
		Phase.ONBOARD_FINAL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("onboard_final")
				_write_summary_simple()
				print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
				_phase = Phase.DONE

		# ── ui_complete: summary ──────────────────────────────
		Phase.UI_WRITE_SUMMARY:
			_write_summary_simple()
			print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
			_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# --- Mode-based phase filtering ---

func _should_skip_phase(phase: Phase) -> bool:
	## Returns true if the given phase should be skipped for the current mode.
	## The state machine uses explicit branching for most transitions, but this
	## catches any phases that fall through sequentially via _next_phase().
	match _mode:
		"fast":
			# fast only runs: BOOT, DOCK_ENTER, DOCK_MARKET_CAPTURE, UNDOCK_1,
			# OPEN_GALAXY_MAP, GALAXY_MAP_CAPTURE, CLOSE_GALAXY_MAP, FAST_FINAL_CAPTURE
			return phase in [
				Phase.DOCK_JOBS_SWITCH, Phase.DOCK_JOBS_CAPTURE,
				Phase.DOCK_SERVICES_SWITCH, Phase.DOCK_SERVICES_CAPTURE,
				Phase.UI_TAB_INTEL_SWITCH, Phase.UI_TAB_INTEL_CAPTURE,
				Phase.UI_TAB_DIPLOMACY_SWITCH, Phase.UI_TAB_DIPLOMACY_CAPTURE,
				Phase.BUY_GOOD, Phase.POST_BUY_CAPTURE,
				Phase.FLIGHT_CARGO_CAPTURE,
				Phase.NPC_ZOOM_IN, Phase.NPC_APPROACH, Phase.NPC_CLOSEUP_CAPTURE,
				Phase.NPC_DAMAGE, Phase.NPC_COMBAT_BURST, Phase.NPC_WARP_VFX,
				Phase.NPC_WARP_VFX_BURST, Phase.NPC_ZOOM_OUT,
				Phase.V2_FACTION_SET, Phase.V2_FACTION_CAPTURE,
				Phase.V2_FLEET_SET, Phase.V2_FLEET_CAPTURE,
				Phase.V2_HEAT_SET, Phase.V2_HEAT_CAPTURE,
				Phase.V2_EXPLORATION_SET, Phase.V2_EXPLORATION_CAPTURE,
				Phase.V2_WARFRONT_SET, Phase.V2_WARFRONT_CAPTURE,
				Phase.V2_OFF_SET,
				Phase.OPEN_EMPIRE_DASH, Phase.EMPIRE_DASH_CAPTURE, Phase.CLOSE_EMPIRE_DASH,
			]
		"onboarding":
			# Onboarding skips: dock tab sweep beyond market, NPC showcase,
			# V2 overlays, empire dash, system 2/3 dock tabs, overlay panels,
			# dock panels, flight/endstate, time advancement, full final
			return phase in [
				Phase.DOCK_JOBS_SWITCH, Phase.DOCK_JOBS_CAPTURE,
				Phase.DOCK_SERVICES_SWITCH, Phase.DOCK_SERVICES_CAPTURE,
				Phase.UI_TAB_INTEL_SWITCH, Phase.UI_TAB_INTEL_CAPTURE,
				Phase.UI_TAB_DIPLOMACY_SWITCH, Phase.UI_TAB_DIPLOMACY_CAPTURE,
				Phase.NPC_ZOOM_IN, Phase.NPC_APPROACH, Phase.NPC_CLOSEUP_CAPTURE,
				Phase.NPC_DAMAGE, Phase.NPC_COMBAT_BURST, Phase.NPC_WARP_VFX,
				Phase.NPC_WARP_VFX_BURST, Phase.NPC_ZOOM_OUT,
				Phase.V2_FACTION_SET, Phase.V2_FACTION_CAPTURE,
				Phase.V2_FLEET_SET, Phase.V2_FLEET_CAPTURE,
				Phase.V2_HEAT_SET, Phase.V2_HEAT_CAPTURE,
				Phase.V2_EXPLORATION_SET, Phase.V2_EXPLORATION_CAPTURE,
				Phase.V2_WARFRONT_SET, Phase.V2_WARFRONT_CAPTURE,
				Phase.V2_OFF_SET,
				Phase.OPEN_EMPIRE_DASH, Phase.EMPIRE_DASH_CAPTURE, Phase.CLOSE_EMPIRE_DASH,
				Phase.SYSTEM_2_DOCK, Phase.SYSTEM_2_MARKET_CAPTURE,
				Phase.UI_SETUP_REFIT, Phase.UI_SYSTEM_2_SHIP_CAPTURE,
				Phase.UI_SETUP_CONSTRUCTION, Phase.UI_SYSTEM_2_STATION_CAPTURE,
				Phase.UI_SETUP_AUTOMATION, Phase.UI_SYSTEM_2_INTEL_CAPTURE,
				Phase.SETUP_REFIT, Phase.SYSTEM_2_SHIP_TAB_SWITCH, Phase.SYSTEM_2_SHIP_TAB_CAPTURE,
				Phase.SYSTEM_2_UNDOCK,
				Phase.SYSTEM_3_CAPTURE, Phase.SYSTEM_3_DOCK_CAPTURE,
				Phase.SETUP_CONSTRUCTION, Phase.SYSTEM_3_STATION_TAB_SWITCH,
				Phase.SYSTEM_3_STATION_TAB_CAPTURE,
				Phase.SETUP_AUTOMATION, Phase.SYSTEM_3_INTEL_TAB_SWITCH,
				Phase.SYSTEM_3_INTEL_TAB_CAPTURE, Phase.SYSTEM_3_UNDOCK,
				Phase.UI_SYSTEM_3_DOCK, Phase.UI_SYSTEM_3_MARKET_CAPTURE, Phase.UI_SYSTEM_3_UNDOCK,
				Phase.SETUP_MISSION, Phase.OPEN_MISSION_JOURNAL, Phase.MISSION_JOURNAL_CAPTURE,
				Phase.CLOSE_MISSION_JOURNAL,
				Phase.OPEN_KNOWLEDGE_WEB, Phase.KNOWLEDGE_WEB_CAPTURE, Phase.CLOSE_KNOWLEDGE_WEB,
				Phase.OPEN_COMBAT_LOG, Phase.COMBAT_LOG_CAPTURE, Phase.CLOSE_COMBAT_LOG,
				Phase.UI_OPEN_DATA_LOG, Phase.UI_DATA_LOG_CAPTURE, Phase.UI_CLOSE_DATA_LOG,
				Phase.UI_OPEN_FO_PANEL, Phase.UI_FO_PANEL_CAPTURE, Phase.UI_CLOSE_FO_PANEL,
				Phase.UI_OPEN_WARFRONT_DASH, Phase.UI_WARFRONT_DASH_CAPTURE, Phase.UI_CLOSE_WARFRONT_DASH,
				Phase.UI_NPC_ZOOM_IN, Phase.UI_NPC_APPROACH, Phase.UI_NPC_CLOSEUP_CAPTURE, Phase.UI_NPC_ZOOM_OUT,
				Phase.DOCK_PANEL_REDOCK, Phase.DOCK_PANEL_HAVEN, Phase.DOCK_PANEL_HAVEN_CAPTURE,
				Phase.DOCK_PANEL_WARFRONT, Phase.DOCK_PANEL_WARFRONT_CAPTURE,
				Phase.DOCK_PANEL_DOCTRINE, Phase.DOCK_PANEL_DOCTRINE_CAPTURE,
				Phase.DOCK_PANEL_BUDGET, Phase.DOCK_PANEL_BUDGET_CAPTURE,
				Phase.DOCK_PANEL_NARRATIVE, Phase.DOCK_PANEL_NARRATIVE_CAPTURE,
				Phase.DOCK_PANEL_MEGAPROJECT, Phase.DOCK_PANEL_MEGAPROJECT_CAPTURE,
				Phase.DOCK_PANEL_UNDOCK,
				Phase.FLIGHT_SCANNER_HUD, Phase.FLIGHT_SCANNER_CAPTURE,
				Phase.FLIGHT_DATA_LOG, Phase.FLIGHT_DATA_LOG_CAPTURE,
				Phase.ENDSTATE_LOSS, Phase.ENDSTATE_LOSS_CAPTURE,
				Phase.ENDSTATE_VICTORY, Phase.ENDSTATE_VICTORY_CAPTURE,
				Phase.FLIGHT_PAUSE, Phase.FLIGHT_PAUSE_CAPTURE,
				Phase.WAIT_TICK_200, Phase.TICK_200_ZOOM_IN, Phase.TICK_200_CAPTURE,
				Phase.FINAL_ZOOM_OUT, Phase.FINAL,
				Phase.FAST_FINAL_CAPTURE, Phase.UI_WRITE_SUMMARY,
			]
		"ui_complete":
			# ui_complete skips: NPC showcase (full), full-specific system captures,
			# dock panels (full T49), flight/endstate, time advancement
			return phase in [
				Phase.NPC_ZOOM_IN, Phase.NPC_APPROACH, Phase.NPC_CLOSEUP_CAPTURE,
				Phase.NPC_DAMAGE, Phase.NPC_COMBAT_BURST, Phase.NPC_WARP_VFX,
				Phase.NPC_WARP_VFX_BURST, Phase.NPC_ZOOM_OUT,
				Phase.WARP_2_TRANSIT_BURST,
				Phase.ONBOARD_ARRIVAL_2_CAPTURE, Phase.ONBOARD_ARRIVAL_2_HUD,
				Phase.ONBOARD_DOCK_2, Phase.ONBOARD_DOCK_2_SETTLE, Phase.ONBOARD_DOCK_2_CAPTURE,
				Phase.ONBOARD_UNDOCK_2,
				Phase.SETUP_REFIT, Phase.SYSTEM_2_SHIP_TAB_SWITCH, Phase.SYSTEM_2_SHIP_TAB_CAPTURE,
				Phase.SYSTEM_3_CAPTURE, Phase.SYSTEM_3_DOCK_CAPTURE,
				Phase.SETUP_CONSTRUCTION, Phase.SYSTEM_3_STATION_TAB_SWITCH,
				Phase.SYSTEM_3_STATION_TAB_CAPTURE,
				Phase.SETUP_AUTOMATION, Phase.SYSTEM_3_INTEL_TAB_SWITCH,
				Phase.SYSTEM_3_INTEL_TAB_CAPTURE, Phase.SYSTEM_3_UNDOCK,
				Phase.ONBOARD_DOCK_3, Phase.ONBOARD_DOCK_3_SETTLE, Phase.ONBOARD_DOCK_3_FULL_TABS_CAPTURE,
				Phase.DOCK_PANEL_REDOCK, Phase.DOCK_PANEL_HAVEN, Phase.DOCK_PANEL_HAVEN_CAPTURE,
				Phase.DOCK_PANEL_WARFRONT, Phase.DOCK_PANEL_WARFRONT_CAPTURE,
				Phase.DOCK_PANEL_DOCTRINE, Phase.DOCK_PANEL_DOCTRINE_CAPTURE,
				Phase.DOCK_PANEL_BUDGET, Phase.DOCK_PANEL_BUDGET_CAPTURE,
				Phase.DOCK_PANEL_NARRATIVE, Phase.DOCK_PANEL_NARRATIVE_CAPTURE,
				Phase.DOCK_PANEL_MEGAPROJECT, Phase.DOCK_PANEL_MEGAPROJECT_CAPTURE,
				Phase.DOCK_PANEL_UNDOCK,
				Phase.FLIGHT_SCANNER_HUD, Phase.FLIGHT_SCANNER_CAPTURE,
				Phase.FLIGHT_DATA_LOG, Phase.FLIGHT_DATA_LOG_CAPTURE,
				Phase.ENDSTATE_LOSS, Phase.ENDSTATE_LOSS_CAPTURE,
				Phase.ENDSTATE_VICTORY, Phase.ENDSTATE_VICTORY_CAPTURE,
				Phase.FLIGHT_PAUSE, Phase.FLIGHT_PAUSE_CAPTURE,
				Phase.WAIT_TICK_200, Phase.TICK_200_ZOOM_IN, Phase.TICK_200_CAPTURE,
				Phase.FINAL_ZOOM_OUT, Phase.FINAL,
				Phase.FAST_FINAL_CAPTURE,
				Phase.ONBOARD_FINAL,
			]
		"full":
			# full skips: mode-specific phases
			return phase in [
				Phase.UI_TAB_INTEL_SWITCH, Phase.UI_TAB_INTEL_CAPTURE,
				Phase.UI_TAB_DIPLOMACY_SWITCH, Phase.UI_TAB_DIPLOMACY_CAPTURE,
				Phase.V2_FLEET_SET, Phase.V2_FLEET_CAPTURE,
				Phase.V2_HEAT_SET, Phase.V2_HEAT_CAPTURE,
				Phase.FAST_FINAL_CAPTURE,
				Phase.ONBOARD_ARRIVAL_2_CAPTURE, Phase.ONBOARD_ARRIVAL_2_HUD,
				Phase.UI_SETUP_REFIT, Phase.UI_SYSTEM_2_SHIP_CAPTURE,
				Phase.UI_SETUP_CONSTRUCTION, Phase.UI_SYSTEM_2_STATION_CAPTURE,
				Phase.UI_SETUP_AUTOMATION, Phase.UI_SYSTEM_2_INTEL_CAPTURE,
				Phase.ONBOARD_DOCK_2, Phase.ONBOARD_DOCK_2_SETTLE, Phase.ONBOARD_DOCK_2_CAPTURE,
				Phase.ONBOARD_UNDOCK_2,
				Phase.ONBOARD_DOCK_3, Phase.ONBOARD_DOCK_3_SETTLE, Phase.ONBOARD_DOCK_3_FULL_TABS_CAPTURE,
				Phase.UI_SYSTEM_3_DOCK, Phase.UI_SYSTEM_3_MARKET_CAPTURE, Phase.UI_SYSTEM_3_UNDOCK,
				Phase.UI_OPEN_DATA_LOG, Phase.UI_DATA_LOG_CAPTURE, Phase.UI_CLOSE_DATA_LOG,
				Phase.UI_OPEN_FO_PANEL, Phase.UI_FO_PANEL_CAPTURE, Phase.UI_CLOSE_FO_PANEL,
				Phase.UI_OPEN_WARFRONT_DASH, Phase.UI_WARFRONT_DASH_CAPTURE, Phase.UI_CLOSE_WARFRONT_DASH,
				Phase.UI_NPC_ZOOM_IN, Phase.UI_NPC_APPROACH, Phase.UI_NPC_CLOSEUP_CAPTURE, Phase.UI_NPC_ZOOM_OUT,
				Phase.UI_WRITE_SUMMARY,
				Phase.ONBOARD_FINAL,
			]
	return false


func _next_phase(phase: Phase) -> Phase:
	## Returns the next Phase enum value (by ordinal + 1).
	var idx: int = phase
	if idx + 1 < Phase.size():
		return (idx + 1) as Phase
	return Phase.DONE


# --- Burst capture ---

func _start_burst(label: String, count: int, spacing: int, after_phase: Phase) -> void:
	_burst_label = label
	_burst_remaining = count
	_burst_spacing = spacing
	_burst_frame = 0
	_after_burst_phase = after_phase
	_polls = 0

func _process_burst() -> void:
	_polls += 1
	if _polls >= _burst_spacing:
		_polls = 0
		_burst_frame += 1
		_capture("%s_f%02d" % [_burst_label, _burst_frame])
		_burst_remaining -= 1
		if _burst_remaining <= 0:
			_polls = 0
			_phase = _after_burst_phase


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


func _set_v2_overlay(mode: int) -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv != null and gv.has_method("SetV2OverlayModeV0"):
		gv.call("SetV2OverlayModeV0", mode)
		print(PREFIX + "V2_OVERLAY|mode=%d" % mode)


func _rebuild_local_system(node_id: String) -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv and gv.has_method("RebuildLocalSystemV0"):
		gv.call("RebuildLocalSystemV0", node_id)
	print(PREFIX + "REBUILD|%s" % node_id)


func _dock_at_current_station() -> void:
	if _game_manager == null:
		return
	# Prefer Station group (actual stations), fallback to Planet group.
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_game_manager.call("on_proximity_dock_entered_v0", targets[0])
		print(PREFIX + "DOCK|%s|groups=%s" % [str(targets[0].name), str(targets[0].get_groups())])


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
	var htm = _find_hero_trade_menu()
	if htm != null and htm.has_method("_switch_dock_tab"):
		htm.call("_switch_dock_tab", idx)


func _find_hero_trade_menu():
	return root.find_child("HeroTradeMenu", true, false)


func _try_buy_good() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		return
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	for item in market:
		if int(item.get("quantity", 0)) > 0:
			_trade_good = str(item.get("good_id", ""))
			_bridge.call("DispatchPlayerTradeV0", node_id, _trade_good, 1, true)
			print(PREFIX + "BUY|%s" % _trade_good)
			return
	print(PREFIX + "WARN|no_goods_to_buy")


# --- Onboarding-specific helpers (from onboarding_screenshot_bot_v0.gd) ---

func _do_dock(node_id: String) -> void:
	## Dock using mock node (onboarding pattern: clears NPC fleets for safe dock).
	if _game_manager == null:
		return
	# Clear NPC fleet ships from scene to prevent hostile-nearby dock guard.
	for ship in root.get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship):
			ship.remove_from_group("FleetShip")
	var mock := Node.new()
	mock.add_to_group("Station")
	mock.set_meta("dock_target_id", node_id)
	root.add_child(mock)
	_game_manager.call("on_proximity_dock_entered_v0", mock)
	print(PREFIX + "DOCK|%s" % node_id)


func _onboard_try_buy_good() -> void:
	## Buy a good using onboarding pattern (picks non-fuel first).
	var view: Array = _bridge.call("GetPlayerMarketViewV0", _home_node_id)
	for row in view:
		var gid: String = str(row.get("good_id", ""))
		var qty: int = int(row.get("quantity", 0))
		if gid != "fuel" and not gid.is_empty() and qty > 0:
			_trade_good = gid
			break
	if _trade_good.is_empty():
		for row in view:
			var gid: String = str(row.get("good_id", ""))
			var qty: int = int(row.get("quantity", 0))
			if not gid.is_empty() and qty > 0:
				_trade_good = gid
				break
	if not _trade_good.is_empty():
		_bridge.call("DispatchPlayerTradeV0", _home_node_id, _trade_good, 1, true)
		print(PREFIX + "BUY|%s" % _trade_good)
	else:
		print(PREFIX + "BUY|SKIP|no_goods")


func _onboard_try_sell_good() -> void:
	## Sell good using HTM path to trigger tab disclosure update (real player flow).
	if _trade_good.is_empty() or _trade_good == "fuel":
		print(PREFIX + "SELL|SKIP")
		return
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm and htm.has_method("_sell_qty_v0"):
		htm.call("_sell_qty_v0", _trade_good, 1)
	else:
		_bridge.call("DispatchPlayerTradeV0", _home_node_id, _trade_good, 1, false)
	print(PREFIX + "SELL|%s" % _trade_good)


func _log_tab_visibility(label: String) -> void:
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm == null:
		print(PREFIX + "TABS|%s|HTM_NOT_FOUND" % label)
		return
	var btns = htm.get("_tab_buttons")
	if btns == null or typeof(btns) != TYPE_ARRAY:
		print(PREFIX + "TABS|%s|NO_BUTTONS" % label)
		return
	var vis_arr: Array[String] = []
	for i in range(btns.size()):
		var btn = btns[i]
		if btn != null and is_instance_valid(btn):
			vis_arr.append("tab%d=%s" % [i, "visible" if btn.visible else "HIDDEN"])
	print(PREFIX + "TABS|%s|%s" % [label, "|".join(vis_arr)])


func _log_onboarding_state(label: String) -> void:
	if not _bridge.has_method("GetOnboardingStateV0"):
		print(PREFIX + "ONBOARD|%s|NO_METHOD" % label)
		return
	var os: Dictionary = _bridge.call("GetOnboardingStateV0")
	var keys := ["has_traded", "has_fought", "has_completed_mission", "has_docked",
				 "nodes_visited", "show_jobs_tab", "show_ship_tab",
				 "show_station_tab", "show_intel_tab", "show_fuel_hud", "show_faction_hud"]
	var parts: Array[String] = []
	for k in keys:
		parts.append("%s=%s" % [k, str(os.get(k, "?"))])
	print(PREFIX + "ONBOARD|%s|%s" % [label, "|".join(parts)])


# --- State setup helpers (populate UI before capture) ---

func _promote_fo() -> void:
	## Promote FO so Port Briefing has a named speaker for observations.
	if _bridge == null:
		return
	if not _bridge.has_method("PromoteFirstOfficerV0"):
		return
	# Check if already promoted.
	if _bridge.has_method("GetFirstOfficerStateV0"):
		var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
		if fo.get("promoted", false):
			return
	# Try Analyst first (best for trade observations).
	var ok: bool = _bridge.call("PromoteFirstOfficerV0", "Analyst")
	if ok:
		print(PREFIX + "FO_PROMOTE|Analyst")
	else:
		# Fallback to any candidate.
		for candidate in ["Veteran", "Pathfinder"]:
			ok = _bridge.call("PromoteFirstOfficerV0", candidate)
			if ok:
				print(PREFIX + "FO_PROMOTE|%s" % candidate)
				break


func _setup_refit() -> void:
	## Install a module into an empty slot so Ship tab shows equipped state.
	if _bridge == null:
		print(PREFIX + "WARN|setup_refit_no_bridge")
		return
	if not _bridge.has_method("GetPlayerFleetSlotsV0"):
		print(PREFIX + "WARN|setup_refit_no_method")
		return
	var fleet_id := "fleet_trader_1"
	var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
	var avail: Array = _bridge.call("GetAvailableModulesV0")
	# Try to install a module into any slot (empty or occupied after remove)
	for i in range(slots.size()):
		var slot: Dictionary = slots[i]
		var slot_kind: String = str(slot.get("slot_kind", ""))
		var current_id: String = str(slot.get("installed_module_id", ""))
		# If slot occupied, remove first
		if not current_id.is_empty():
			_bridge.call("RemoveModuleV0", fleet_id, i)
		# Find a module matching this slot kind
		for mod in avail:
			var mod_id: String = str(mod.get("module_id", ""))
			if str(mod.get("slot_kind", "")) == slot_kind and mod_id != current_id:
				var result: Dictionary = _bridge.call("InstallModuleV0", fleet_id, i, mod_id)
				if bool(result.get("success", false)):
					print(PREFIX + "SETUP_REFIT|slot=%d kind=%s installed=%s" % [i, slot_kind, mod_id])
					return
	print(PREFIX + "WARN|setup_refit_failed|slots=%d avail=%d" % [slots.size(), avail.size()])


func _setup_construction() -> void:
	## Start a construction project so Station tab shows in-progress work.
	if _bridge == null:
		print(PREFIX + "WARN|setup_construction_no_bridge")
		return
	if not _bridge.has_method("GetAvailableConstructionDefsV0"):
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
			print(PREFIX + "SETUP_CONSTRUCTION|def=%s node=%s ok=%s" % [def_id, node_id, str(result.get("success", false))])
			return
	print(PREFIX + "WARN|setup_construction_all_blocked|defs=%d" % defs.size())


func _setup_automation() -> void:
	## Create an auto-buy program so Intel tab shows active automation.
	if _bridge == null:
		print(PREFIX + "WARN|setup_automation_no_bridge")
		return
	if not _bridge.has_method("CreateAutoBuyProgram"):
		print(PREFIX + "WARN|setup_automation_no_method")
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	# Find a good available at this market
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	for item in market:
		if int(item.get("quantity", 0)) > 0:
			var good_id: String = str(item.get("good_id", ""))
			var pid: String = str(_bridge.call("CreateAutoBuyProgram", node_id, good_id, 1, 30))
			print(PREFIX + "SETUP_AUTOMATION|good=%s pid=%s" % [good_id, pid])
			return
	print(PREFIX + "WARN|setup_automation_no_goods")


func _setup_mission() -> void:
	## Accept a mission so Mission Journal shows an active entry.
	if _bridge == null:
		print(PREFIX + "WARN|setup_mission_no_bridge")
		return
	if not _bridge.has_method("GetMissionListV0"):
		print(PREFIX + "WARN|setup_mission_no_method")
		return
	# Check if already have an active mission
	if _bridge.has_method("GetActiveMissionV0"):
		var active: Dictionary = _bridge.call("GetActiveMissionV0")
		if not str(active.get("mission_id", "")).is_empty():
			print(PREFIX + "SETUP_MISSION|already_active=%s" % str(active.get("mission_id", "")))
			return
	var missions: Array = _bridge.call("GetMissionListV0")
	for m in missions:
		var mid: String = str(m.get("mission_id", ""))
		if not mid.is_empty():
			var result = _bridge.call("AcceptMissionV0", mid)
			print(PREFIX + "SETUP_MISSION|id=%s ok=%s" % [mid, str(result)])
			return
	print(PREFIX + "WARN|setup_mission_no_available")


# --- Galaxy map helpers ---

func _force_strategic_altitude() -> void:
	## Bypass the 0.6s altitude tween — jump camera to strategic altitude
	## so _sync_overlay_state triggers SetOverlayOpenV0(true) immediately.
	# Camera node is named "Camera3D" in the scene tree (script: player_follow_camera.gd).
	var cam_ctrl = root.find_child("Camera3D", true, false)
	if cam_ctrl == null:
		print(PREFIX + "WARN|no_camera_for_strategic")
		return
	# Kill any active tween to prevent it from overriding our altitude.
	var tab_tween = cam_ctrl.get("_tab_tween")
	if tab_tween != null and tab_tween is Tween and tab_tween.is_valid():
		tab_tween.kill()
	# Set altitude above OVERLAY_THRESHOLD (500) to trigger overlay state.
	# STRATEGIC_ALTITUDE is a const (5000.0) — not accessible via .get().
	var strategic: float = 5000.0
	cam_ctrl.set("_altitude", strategic)
	cam_ctrl.set("flight_follow_distance", strategic)
	# Center camera on galaxy centroid (same as toggle_strategic_altitude_v0 does).
	var galaxy_view = root.find_child("GalaxyView", true, false)
	if galaxy_view and galaxy_view.has_method("GetAutoFitFrameV0"):
		var frame: Dictionary = galaxy_view.call("GetAutoFitFrameV0")
		var alt: float = float(frame.get("altitude", strategic))
		cam_ctrl.set("_altitude", alt)
		cam_ctrl.set("flight_follow_distance", alt)
		var cx: float = float(frame.get("center_x", 0.0))
		var cz: float = float(frame.get("center_z", 0.0))
		var hero = _get_hero_body()
		if hero:
			var pan_offset := Vector3(cx - hero.global_position.x, 0.0, cz - hero.global_position.z)
			cam_ctrl.set("_galaxy_map_pan_offset", pan_offset)
			# Teleport spring state directly — bypasses spring convergence delay.
			var target_pos := Vector3(cx, alt, cz)
			var target_look := Vector3(cx, 0.0, cz)
			cam_ctrl.set("_spring_pos", target_pos)
			cam_ctrl.set("_spring_look", target_look)
			cam_ctrl.set("_spring_vel", Vector3.ZERO)
			cam_ctrl.set("_spring_look_vel", Vector3.ZERO)
		# Directly call SetOverlayOpenV0(true) — can't wait for _sync_overlay_state.
		if galaxy_view.has_method("SetOverlayOpenV0"):
			galaxy_view.call("SetOverlayOpenV0", true)
		# Also call RefreshFromSnapshotV0 to force node/edge visibility restoration.
		if galaxy_view.has_method("RefreshFromSnapshotV0"):
			galaxy_view.call("RefreshFromSnapshotV0")
		print(PREFIX + "GALAXY_MAP|auto_fit|alt=%s|cx=%s|cz=%s" % [str(alt), str(cx), str(cz)])
	else:
		print(PREFIX + "GALAXY_MAP|forced_altitude=%s" % str(strategic))


# --- NPC showcase helpers ---

var _saved_cam_distance: float = 80.0

func _set_camera_distance(dist: float) -> void:
	var cam_ctrl = root.find_child("Camera3D", true, false)
	if cam_ctrl:
		_saved_cam_distance = float(cam_ctrl.get("_altitude"))
		# Set _altitude — _sync_altitude() derives flight_follow_distance from it.
		cam_ctrl.set("_altitude", dist)
		cam_ctrl.set("flight_follow_distance", dist)
		var offset := Vector3(0, dist, dist * 0.05)
		cam_ctrl.set("flight_offset", offset)
		# Reset galaxy map pan offset so camera centers on player again.
		cam_ctrl.set("_galaxy_map_pan_offset", Vector3.ZERO)
		# Teleport spring to avoid multi-second convergence delay.
		var hero = _get_hero_body()
		if hero:
			var hero_pos: Vector3 = hero.global_position
			var target_pos: Vector3 = hero_pos + Vector3(0, dist, dist * 0.05)
			cam_ctrl.set("_spring_pos", target_pos)
			cam_ctrl.set("_spring_vel", Vector3.ZERO)
			cam_ctrl.set("_spring_look", hero_pos)
			cam_ctrl.set("_spring_look_vel", Vector3.ZERO)
		print(PREFIX + "CAMERA|dist=%s" % str(dist))


func _get_hero_body():
	var players = get_nodes_in_group("Player")
	if players.size() > 0:
		return players[0]
	return null


func _find_npc_ships() -> Array:
	# Try NpcShip group first (Tranche 15 ships), then FleetShip (legacy markers)
	var npcs = get_nodes_in_group("NpcShip")
	if npcs.is_empty():
		npcs = get_nodes_in_group("FleetShip")
	return npcs


func _find_nearest_npc(hero: Node3D) -> Node3D:
	var npcs = _find_npc_ships()
	if npcs.is_empty():
		return null
	var nearest = npcs[0]
	var best_dist: float = hero.global_position.distance_to(nearest.global_position)
	for npc in npcs:
		var d: float = hero.global_position.distance_to(npc.global_position)
		if d < best_dist:
			best_dist = d
			nearest = npc
	return nearest


func _approach_nearest_npc() -> void:
	var hero = _get_hero_body()
	if hero == null:
		print(PREFIX + "WARN|no_hero_body")
		return
	var nearest = _find_nearest_npc(hero)
	if nearest == null:
		print(PREFIX + "WARN|no_npc_ships")
		return
	# Teleport hero to within 10u of the NPC for close-up visibility.
	var dir: Vector3 = (hero.global_position - nearest.global_position).normalized()
	if dir.length_squared() < 0.01:
		dir = Vector3(1, 0, 0)
	hero.global_position = nearest.global_position + dir * 10.0
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	print(PREFIX + "NPC_APPROACH|dist=15|npc=%s|has_on_hit=%s|groups=%s" % [
		str(nearest.name),
		str(nearest.has_method("on_hit")),
		str(nearest.get_groups())])


func _approach_nearest_planet() -> void:
	var hero = _get_hero_body()
	if hero == null:
		return
	var planets = get_nodes_in_group("Planet")
	if planets.is_empty():
		print(PREFIX + "WARN|no_planets")
		return
	var nearest = planets[0]
	var best_dist: float = hero.global_position.distance_to(nearest.global_position)
	for p in planets:
		var d: float = hero.global_position.distance_to(p.global_position)
		if d < best_dist:
			best_dist = d
			nearest = p
	var dir: Vector3 = (hero.global_position - nearest.global_position).normalized()
	if dir.length_squared() < 0.01:
		dir = Vector3(1, 0, 0)
	hero.global_position = nearest.global_position + dir * 12.0
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	print(PREFIX + "PLANET_APPROACH|%s" % str(nearest.name))


func _damage_nearest_npc(hits: int, dmg: int) -> void:
	var hero = _get_hero_body()
	if hero == null:
		return
	var nearest = _find_nearest_npc(hero)
	if nearest == null:
		print(PREFIX + "WARN|no_npc_to_damage")
		return
	# Try on_hit (NpcShip method — applies stagger + routes to bridge)
	var hit_count := 0
	if nearest.has_method("on_hit"):
		for i in range(hits):
			nearest.call("on_hit", dmg)
			hit_count += 1
	else:
		# Fallback: damage via bridge directly using fleet_id from metadata
		var fleet_id: String = ""
		if nearest.has_meta("fleet_id"):
			fleet_id = str(nearest.get_meta("fleet_id"))
		elif nearest.has_method("get") and nearest.get("fleet_id") != null:
			fleet_id = str(nearest.get("fleet_id"))
		if not fleet_id.is_empty() and _bridge != null and _bridge.has_method("DamageNpcFleetV0"):
			for i in range(hits):
				_bridge.call("DamageNpcFleetV0", fleet_id, dmg)
				hit_count += 1
	print(PREFIX + "NPC_DAMAGE|hits=%d|dmg=%d|target=%s" % [hit_count, dmg, str(nearest.name)])


func _force_npc_engage() -> void:
	## Force nearest NPC fleet_ai into ENGAGE state so the overhead HP bar renders.
	## Normally ENGAGE requires _is_hostile + proximity check on the 2s aggro timer.
	var hero = _get_hero_body()
	if hero == null:
		return
	var nearest = _find_nearest_npc(hero)
	if nearest == null:
		return
	# Fleet marker root node has fleet_ai.gd with _state, _is_hostile, _hp_bar_timer.
	# If _find_npc_ships returned a child (NpcShip group), walk up to the fleet root.
	var fleet_node: Node3D = nearest
	if not fleet_node.is_in_group("FleetShip"):
		var parent = fleet_node.get_parent()
		if parent is Node3D and parent.is_in_group("FleetShip"):
			fleet_node = parent
	# fleet_ai.gd State enum: IDLE=0, PATROL=1, DOCK=2, ENGAGE=3
	fleet_node.set("_state", 3)  # State.ENGAGE
	fleet_node.set("_is_hostile", true)
	fleet_node.set("_hp_bar_timer", 5.0)  # Ensure bar stays visible
	print(PREFIX + "NPC_ENGAGE|forced|target=%s" % str(fleet_node.name))


func _spawn_warp_in_vfx() -> void:
	var hero = _get_hero_body()
	if hero == null:
		return
	# Inline warp-in VFX (avoids preload parse issues with warp_effect.gd)
	var pos: Vector3 = hero.global_position + Vector3(10, 0, 0)
	var effect := Node3D.new()
	effect.name = "WarpInVfx"
	effect.position = pos
	root.add_child(effect)

	# Particle burst (blue-white)
	var particles := GPUParticles3D.new()
	particles.amount = 16
	particles.lifetime = 0.6
	particles.one_shot = true
	particles.explosiveness = 0.95
	var pmat := ParticleProcessMaterial.new()
	pmat.direction = Vector3.ZERO
	pmat.spread = 180.0
	pmat.initial_velocity_min = 5.0
	pmat.initial_velocity_max = 15.0
	pmat.gravity = Vector3.ZERO
	pmat.scale_min = 0.2
	pmat.scale_max = 0.5
	pmat.color = Color(0.4, 0.7, 1.0, 1.0)
	particles.process_material = pmat
	var pmesh := SphereMesh.new()
	pmesh.radius = 0.2
	pmesh.height = 0.4
	particles.draw_pass_1 = pmesh
	effect.add_child(particles)
	particles.emitting = true

	# Flash sphere (shrinks from 4x to 0.1x over 0.8s)
	var flash := MeshInstance3D.new()
	flash.name = "WarpFlash"
	var sphere := SphereMesh.new()
	sphere.radius = 1.5
	sphere.height = 3.0
	flash.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.5, 0.8, 1.0, 0.7)
	mat.emission_enabled = true
	mat.emission = Color(0.5, 0.8, 1.0, 1.0)
	mat.emission_energy_multiplier = 5.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	flash.material_override = mat
	effect.add_child(flash)

	var tween := effect.create_tween()
	tween.tween_property(flash, "scale", Vector3(0.1, 0.1, 0.1), 0.8).from(Vector3(4.0, 4.0, 4.0))
	tween.parallel().tween_property(mat, "albedo_color:a", 0.0, 0.8).from(0.7)
	tween.tween_callback(effect.queue_free)

	print(PREFIX + "WARP_VFX|pos=%s" % str(pos))


# --- Capture helpers ---

func _capture(label: String) -> void:
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]

	var img_path = _screenshot.capture_v0(self, filename, _output_dir)

	var report = _observer.capture_full_report_v0()
	var report_path := _output_dir.path_join(filename + "_report.json")
	_observer.write_report_json_v0(report, report_path)

	_snapshots.append({
		"phase": label,
		"tick": tick,
		"screenshot": img_path,
		"report": report_path,
	})
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _write_summary(audit_results: Array, critical_fails: int) -> void:
	var summary := {
		"bot": "visual_sweep_bot_v6",
		"mode": _mode,
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
		"aesthetic_audit": audit_results,
		"critical_failures": critical_fails,
	}
	DirAccess.make_dir_recursive_absolute(_output_dir)
	var f := FileAccess.open(_output_dir.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
		print(PREFIX + "SUMMARY_SAVED")
	else:
		print(PREFIX + "SUMMARY_SAVE_FAILED")


func _write_summary_simple() -> void:
	## Summary without aesthetic audit (used by onboarding + ui_complete modes).
	var summary := {
		"bot": "visual_sweep_bot_v6",
		"mode": _mode,
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
	}
	DirAccess.make_dir_recursive_absolute(_output_dir)
	var f := FileAccess.open(_output_dir.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
		print(PREFIX + "SUMMARY_SAVED")
	else:
		print(PREFIX + "SUMMARY_SAVE_FAILED")


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
