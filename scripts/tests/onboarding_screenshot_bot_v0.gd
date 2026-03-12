extends SceneTree

## Onboarding Screenshot Bot — Captures progressive disclosure at each milestone.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Captures:
##   1. fresh_boot        — Flight HUD with minimal disclosure (no fuel, no security)
##   2. first_dock_market  — Dock menu: only Market tab visible
##   3. first_dock_tabs    — Verify Jobs/Ship/Station/Intel hidden
##   4. post_trade_market  — After buy+sell: Jobs tab now visible
##   5. post_trade_tabs    — Jobs tab highlighted/unlocked
##   6. fo_promotion_toast — FO toast visible in dock
##   7. undocked_flight    — Flight HUD with fuel label visible
##   8. arrival_system2    — After warp: FO arrival dialogue fires
##   9. arrival_hud        — HUD now shows security label (2+ nodes)
##  10. system3_full_tabs  — After 3+ nodes: all tabs visible
##  11. milestone_toast    — Gold "first_trade" milestone toast (if timed)
##
## Usage:
##   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode scenario -Script "res://scripts/tests/onboarding_screenshot_bot_v0.gd" -Prefix "ONBD"

const PREFIX := "ONBD|"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/screenshot/onboarding/"

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

# Timing constants (frames at ~60fps)
const SETTLE_SCENE := 60
const SETTLE_ACTION := 20
const SETTLE_TAB := 15
const POST_CAPTURE := 10

enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	# Fresh boot — zero progression
	BOOT_CAPTURE,

	# First dock — market only
	DOCK_1,
	DOCK_1_SETTLE,
	DOCK_1_MARKET_CAPTURE,
	DOCK_1_CHECK_HIDDEN_TABS,

	# Trade — buy then sell
	BUY_GOOD,
	BUY_SETTLE,
	SELL_GOOD,
	SELL_SETTLE,
	POST_TRADE_CAPTURE,
	POST_TRADE_CHECK_TABS,

	# Undock — flight with fuel HUD
	UNDOCK_1,
	UNDOCK_1_SETTLE,
	FLIGHT_HUD_CAPTURE,

	# Warp to system 2 — FO arrival
	WARP_2_TRIGGER,
	WARP_2_WAIT,
	ARRIVAL_2_CAPTURE,
	ARRIVAL_2_HUD_CAPTURE,

	# Dock system 2 — more tabs?
	DOCK_2,
	DOCK_2_SETTLE,
	DOCK_2_CAPTURE,
	UNDOCK_2,

	# Warp to system 3 — full disclosure
	WARP_3_COOLDOWN,
	WARP_3_TRIGGER,
	WARP_3_WAIT,
	DOCK_3,
	DOCK_3_SETTLE,
	DOCK_3_FULL_TABS_CAPTURE,

	# Final
	FINAL,
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _bridge = null
var _gm = null
var _screenshot = null
var _total_frames := 0
const MAX_FRAMES := 3600

var _home_node_id := ""
var _neighbor_ids: Array = []
var _trade_good := ""
var _snapshots: Array = []


func _initialize() -> void:
	print(PREFIX + "START")
	_screenshot = ScreenshotScript.new()


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
			_gm = root.get_node_or_null("GameManager")
			if _bridge != null and _gm != null:
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
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_discover_nav()
				_phase = Phase.BOOT_CAPTURE

		# ── 1. Fresh Boot ──
		Phase.BOOT_CAPTURE:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				# Log onboarding state at boot
				_log_onboarding_state("boot")
				_capture("01_fresh_boot")
				_polls = 0
				_phase = Phase.DOCK_1

		# ── 2. First Dock ──
		Phase.DOCK_1:
			_do_dock(_home_node_id)
			_polls = 0
			_phase = Phase.DOCK_1_SETTLE

		Phase.DOCK_1_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.DOCK_1_MARKET_CAPTURE

		Phase.DOCK_1_MARKET_CAPTURE:
			_capture("02_first_dock_market")
			_polls = 0
			_phase = Phase.DOCK_1_CHECK_HIDDEN_TABS

		Phase.DOCK_1_CHECK_HIDDEN_TABS:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Check which tabs are visible
				_log_tab_visibility("first_dock")
				_log_onboarding_state("first_dock")
				_capture("03_first_dock_tabs")
				_polls = 0
				_phase = Phase.BUY_GOOD

		# ── 3. Trade ──
		Phase.BUY_GOOD:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_try_buy_good()
				_polls = 0
				_phase = Phase.BUY_SETTLE

		Phase.BUY_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.SELL_GOOD

		Phase.SELL_GOOD:
			_try_sell_good()
			_polls = 0
			_phase = Phase.SELL_SETTLE

		Phase.SELL_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.POST_TRADE_CAPTURE

		Phase.POST_TRADE_CAPTURE:
			_log_onboarding_state("post_trade")
			_capture("04_post_trade_market")
			_polls = 0
			_phase = Phase.POST_TRADE_CHECK_TABS

		Phase.POST_TRADE_CHECK_TABS:
			_polls += 1
			if _polls >= POST_CAPTURE:
				# Switch to Jobs tab — should now be visible
				_switch_dock_tab(1)
				_polls = 0
				# wait a beat then capture
				_phase = Phase.UNDOCK_1

		# ── 4. Undock + Flight HUD ──
		Phase.UNDOCK_1:
			_polls += 1
			if _polls >= SETTLE_TAB:
				_log_tab_visibility("post_trade")
				_capture("05_post_trade_jobs_tab")
				_gm.call("undock_v0")
				_polls = 0
				_phase = Phase.UNDOCK_1_SETTLE

		Phase.UNDOCK_1_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.FLIGHT_HUD_CAPTURE

		Phase.FLIGHT_HUD_CAPTURE:
			_log_onboarding_state("undocked")
			_capture("06_flight_hud_post_trade")
			_polls = 0
			_phase = Phase.WARP_2_TRIGGER

		# ── 5. Warp to System 2 ──
		Phase.WARP_2_TRIGGER:
			_polls += 1
			if _polls >= POST_CAPTURE:
				if _neighbor_ids.size() >= 1:
					var dest: String = _neighbor_ids[0]
					_gm.call("on_lane_gate_proximity_entered_v0", dest)
					_gm.call("on_lane_arrival_v0", dest)
					print(PREFIX + "WARP|dest=%s" % dest)
				_polls = 0
				_phase = Phase.WARP_2_WAIT

		Phase.WARP_2_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_polls = 0
				_phase = Phase.ARRIVAL_2_CAPTURE

		Phase.ARRIVAL_2_CAPTURE:
			_log_onboarding_state("arrival_sys2")
			_capture("07_arrival_system2")
			_polls = 0
			_phase = Phase.ARRIVAL_2_HUD_CAPTURE

		Phase.ARRIVAL_2_HUD_CAPTURE:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("08_arrival_hud")
				_polls = 0
				_phase = Phase.DOCK_2

		# ── 6. Dock System 2 ──
		Phase.DOCK_2:
			var dest2: String = _neighbor_ids[0] if _neighbor_ids.size() >= 1 else _home_node_id
			_do_dock(dest2)
			_polls = 0
			_phase = Phase.DOCK_2_SETTLE

		Phase.DOCK_2_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.DOCK_2_CAPTURE

		Phase.DOCK_2_CAPTURE:
			_log_tab_visibility("sys2_dock")
			_capture("09_system2_dock_tabs")
			_polls = 0
			_phase = Phase.UNDOCK_2

		Phase.UNDOCK_2:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_gm.call("undock_v0")
				_polls = 0
				_phase = Phase.WARP_3_COOLDOWN

		# ── 7. Warp to System 3 ──
		Phase.WARP_3_COOLDOWN:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.WARP_3_TRIGGER

		Phase.WARP_3_TRIGGER:
			if _neighbor_ids.size() >= 2:
				var dest: String = _neighbor_ids[1]
				_gm.call("on_lane_gate_proximity_entered_v0", dest)
				_gm.call("on_lane_arrival_v0", dest)
				print(PREFIX + "WARP|dest=%s" % dest)
			elif _neighbor_ids.size() >= 1:
				# only 1 neighbor — warp back to home
				_gm.call("on_lane_gate_proximity_entered_v0", _home_node_id)
				_gm.call("on_lane_arrival_v0", _home_node_id)
				print(PREFIX + "WARP|dest=%s (home)" % _home_node_id)
			_polls = 0
			_phase = Phase.WARP_3_WAIT

		Phase.WARP_3_WAIT:
			_polls += 1
			if _polls >= SETTLE_SCENE:
				_polls = 0
				_phase = Phase.DOCK_3

		# ── 8. Dock System 3 — Full Tabs ──
		Phase.DOCK_3:
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var cur_node: String = str(ps.get("current_node_id", _home_node_id))
			_do_dock(cur_node)
			_polls = 0
			_phase = Phase.DOCK_3_SETTLE

		Phase.DOCK_3_SETTLE:
			_polls += 1
			if _polls >= SETTLE_ACTION:
				_polls = 0
				_phase = Phase.DOCK_3_FULL_TABS_CAPTURE

		Phase.DOCK_3_FULL_TABS_CAPTURE:
			_log_onboarding_state("sys3_dock")
			_log_tab_visibility("sys3_dock")
			_capture("10_system3_full_tabs")
			_polls = 0
			_phase = Phase.FINAL

		# ── Final ──
		Phase.FINAL:
			_polls += 1
			if _polls >= POST_CAPTURE:
				_capture("11_final")
				print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
				_phase = Phase.DONE

		Phase.DONE:
			if _bridge and _bridge.has_method("StopSimV0"):
				_bridge.call("StopSimV0")
			quit(0)

	return false


# ── Navigation ──

func _discover_nav() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", "star_0"))
	# Find neighbors from galaxy snapshot
	var snap: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var edges: Array = snap.get("lane_edges", [])
	for edge in edges:
		var from_id: String = str(edge.get("from_id", ""))
		var to_id: String = str(edge.get("to_id", ""))
		if from_id == _home_node_id and not _neighbor_ids.has(to_id):
			_neighbor_ids.append(to_id)
		elif to_id == _home_node_id and not _neighbor_ids.has(from_id):
			_neighbor_ids.append(from_id)
	print(PREFIX + "NAV|home=%s neighbors=%d" % [_home_node_id, _neighbor_ids.size()])


# ── Dock ──

func _do_dock(node_id: String) -> void:
	# Clear NPC fleet ships from scene to prevent hostile-nearby dock guard.
	for ship in root.get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship):
			ship.remove_from_group("FleetShip")
	var mock := Node.new()
	mock.add_to_group("Station")
	mock.set_meta("dock_target_id", node_id)
	root.add_child(mock)
	_gm.call("on_proximity_dock_entered_v0", mock)
	print(PREFIX + "DOCK|%s" % node_id)


# ── Trade ──

func _try_buy_good() -> void:
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


func _try_sell_good() -> void:
	if _trade_good.is_empty() or _trade_good == "fuel":
		print(PREFIX + "SELL|SKIP")
		return
	# Use HTM's sell path to trigger tab disclosure update (real player flow).
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm and htm.has_method("_sell_qty_v0"):
		htm.call("_sell_qty_v0", _trade_good, 1)
	else:
		_bridge.call("DispatchPlayerTradeV0", _home_node_id, _trade_good, 1, false)
	print(PREFIX + "SELL|%s" % _trade_good)


# ── UI Inspection ──

func _switch_dock_tab(idx: int) -> void:
	var htm = root.find_child("HeroTradeMenu", true, false)
	if htm != null and htm.has_method("_switch_dock_tab"):
		htm.call("_switch_dock_tab", idx)


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


# ── Capture ──

func _capture(label: String) -> void:
	var tick := 0
	if _bridge and _bridge.has_method("GetPlayerStateV0"):
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		tick = int(ps.get("tick", 0))
	var path: String = _screenshot.capture_v0(self, "%s_%04d" % [label, tick], OUTPUT_DIR)
	_snapshots.append({"phase": label, "tick": tick, "path": path})
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _fail(reason: String) -> void:
	print(PREFIX + "FAIL|" + reason)
	_phase = Phase.DONE
