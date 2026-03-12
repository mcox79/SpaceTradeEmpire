extends SceneTree

## GATE.S19.ONBOARD.ORCHESTRATE.016 / GATE.S19.ONBOARD.HEADLESS_PROOF.017
## Headless proof: new game → dock → trade → warp → verify disclosure state.
## Validates progressive disclosure, FO events, milestone toasts.

const PREFIX := "ONBOARD|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	VERIFY_INITIAL,
	DOCK_STAR0,
	VERIFY_DOCK_DISCLOSURE,
	BUY_GOOD,
	SELL_GOOD,
	VERIFY_TRADE_DISCLOSURE,
	UNDOCK,
	TRAVEL,
	WAIT_ARRIVAL,
	VERIFY_ARRIVAL_DISCLOSURE,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _fail_count := 0
var _trade_good_id := ""


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			_gm = root.get_node_or_null("GameManager")
			if _bridge != null and _gm != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_or_gm_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_phase = Phase.VERIFY_INITIAL
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.VERIFY_INITIAL:
			_verify_initial_state()
			_phase = Phase.DOCK_STAR0

		Phase.DOCK_STAR0:
			_do_dock()
			_phase = Phase.VERIFY_DOCK_DISCLOSURE

		Phase.VERIFY_DOCK_DISCLOSURE:
			_verify_dock_disclosure()
			_phase = Phase.BUY_GOOD

		Phase.BUY_GOOD:
			_do_buy()
			_phase = Phase.SELL_GOOD

		Phase.SELL_GOOD:
			_do_sell()
			_phase = Phase.VERIFY_TRADE_DISCLOSURE

		Phase.VERIFY_TRADE_DISCLOSURE:
			_verify_trade_disclosure()
			_phase = Phase.UNDOCK

		Phase.UNDOCK:
			_gm.call("undock_v0")
			_phase = Phase.TRAVEL

		Phase.TRAVEL:
			_do_travel()
			_phase = Phase.WAIT_ARRIVAL

		Phase.WAIT_ARRIVAL:
			_polls += 1
			if _polls >= 5:
				_phase = Phase.VERIFY_ARRIVAL_DISCLOSURE

		Phase.VERIFY_ARRIVAL_DISCLOSURE:
			_verify_arrival_disclosure()
			_finish()

		Phase.DONE:
			pass

	return false


# ── Verification Steps ──

func _verify_initial_state() -> void:
	if not _bridge.has_method("GetOnboardingStateV0"):
		_fail("no_GetOnboardingStateV0")
		return
	var os: Dictionary = _bridge.call("GetOnboardingStateV0")
	_assert_eq(os.get("has_traded", true), false, "initial_has_traded")
	_assert_eq(os.get("has_fought", true), false, "initial_has_fought")
	_assert_eq(os.get("has_completed_mission", true), false, "initial_has_completed_mission")
	_assert_eq(os.get("show_jobs_tab", true), false, "initial_show_jobs_tab")
	_assert_eq(os.get("show_ship_tab", true), false, "initial_show_ship_tab")
	print(PREFIX + "INITIAL_STATE|PASS")


func _do_dock() -> void:
	# Clear NPC fleet ships from scene to prevent hostile-nearby dock guard.
	for ship in root.get_tree().get_nodes_in_group("FleetShip"):
		if is_instance_valid(ship):
			ship.remove_from_group("FleetShip")
	var mock_sta := Node.new()
	mock_sta.add_to_group("Station")
	mock_sta.set_meta("dock_target_id", "star_0")
	root.add_child(mock_sta)
	_gm.call("on_proximity_dock_entered_v0", mock_sta)
	print(PREFIX + "DOCK|star_0")


func _verify_dock_disclosure() -> void:
	var os: Dictionary = _bridge.call("GetOnboardingStateV0")
	_assert_eq(os.get("has_traded", true), false, "dock_has_traded")
	_assert_eq(os.get("show_jobs_tab", true), false, "dock_show_jobs_tab")
	print(PREFIX + "DOCK_DISCLOSURE|PASS")


func _do_buy() -> void:
	if not _bridge.has_method("GetPlayerMarketViewV0"):
		_fail("no_market_view")
		return
	var view: Array = _bridge.call("GetPlayerMarketViewV0", "star_0")
	if view.is_empty():
		_fail("market_empty")
		return
	# Find a non-fuel tradeable good (fuel goes to fuel tank, not cargo).
	for row in view:
		var gid: String = str(row.get("good_id", ""))
		if gid != "fuel" and not gid.is_empty():
			var qty: int = int(row.get("quantity", 0))
			if qty > 0:
				_trade_good_id = gid
				break
	if _trade_good_id.is_empty():
		# Fallback: use first good with any quantity.
		for row in view:
			var gid: String = str(row.get("good_id", ""))
			var qty: int = int(row.get("quantity", 0))
			if qty > 0 and not gid.is_empty():
				_trade_good_id = gid
				break
	if _trade_good_id.is_empty():
		print(PREFIX + "WARN|no_buyable_good|using_fuel_fallback")
		_trade_good_id = "fuel"
	if _bridge.has_method("DispatchPlayerTradeV0"):
		_bridge.call("DispatchPlayerTradeV0", "star_0", _trade_good_id, 1, true)
		print(PREFIX + "BUY|good=%s|qty=1" % _trade_good_id)


func _do_sell() -> void:
	# Sell the good we just bought (GoodsTraded increments on sell, not buy).
	if _trade_good_id.is_empty() or _trade_good_id == "fuel":
		print(PREFIX + "SELL|SKIP|fuel_only")
		return
	if _bridge.has_method("DispatchPlayerTradeV0"):
		_bridge.call("DispatchPlayerTradeV0", "star_0", _trade_good_id, 1, false)
		print(PREFIX + "SELL|good=%s|qty=1" % _trade_good_id)


func _verify_trade_disclosure() -> void:
	var os: Dictionary = _bridge.call("GetOnboardingStateV0")
	_assert_eq(os.get("has_traded", false), true, "trade_has_traded")
	_assert_eq(os.get("show_jobs_tab", false), true, "trade_show_jobs_tab")
	print(PREFIX + "TRADE_DISCLOSURE|PASS")


func _do_travel() -> void:
	# Find adjacent node via galaxy snapshot edges.
	if not _bridge.has_method("GetGalaxySnapshotV0"):
		_fail("no_GetGalaxySnapshotV0")
		return
	var snap: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var edges: Array = snap.get("lane_edges", [])
	var dest := ""
	for edge in edges:
		var from_id: String = str(edge.get("from_id", ""))
		var to_id: String = str(edge.get("to_id", ""))
		if from_id == "star_0":
			dest = to_id
			break
		elif to_id == "star_0":
			dest = from_id
			break
	if dest.is_empty():
		_fail("no_adjacent_node")
		return
	# Simulate lane travel.
	_gm.call("on_lane_gate_proximity_entered_v0", dest)
	_gm.call("on_lane_arrival_v0", dest)
	_polls = 0
	print(PREFIX + "TRAVEL|dest=%s" % dest)


func _verify_arrival_disclosure() -> void:
	var os: Dictionary = _bridge.call("GetOnboardingStateV0")
	var nodes_visited: int = int(os.get("nodes_visited", 0))
	_assert_ge(nodes_visited, 1, "arrival_nodes_visited")
	_assert_eq(os.get("has_docked", false), true, "arrival_has_docked")
	_assert_eq(os.get("show_fuel_hud", false), true, "arrival_show_fuel_hud")
	print(PREFIX + "ARRIVAL_DISCLOSURE|PASS")


# ── Helpers ──

func _assert_eq(actual: Variant, expected: Variant, label: String) -> void:
	if actual != expected:
		print(PREFIX + "ASSERT_FAIL|%s|actual=%s|expected=%s" % [label, str(actual), str(expected)])
		_fail_count += 1
	else:
		print(PREFIX + "ASSERT_OK|%s" % label)


func _assert_ge(actual: int, threshold: int, label: String) -> void:
	if actual < threshold:
		print(PREFIX + "ASSERT_FAIL|%s|actual=%d|threshold=%d" % [label, actual, threshold])
		_fail_count += 1
	else:
		print(PREFIX + "ASSERT_OK|%s" % label)


func _fail(reason: String) -> void:
	print(PREFIX + "FAIL|" + reason)
	_fail_count += 1
	_finish()


func _finish() -> void:
	_phase = Phase.DONE
	if _fail_count == 0:
		print(PREFIX + "ALL|PASS")
	else:
		print(PREFIX + "RESULT|FAIL|failures=%d" % _fail_count)
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0 if _fail_count == 0 else 1)
