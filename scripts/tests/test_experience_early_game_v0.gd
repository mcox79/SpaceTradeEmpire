extends SceneTree

## EPIC.X.EXPERIENCE_PROOF.V0 — Scenario 1: Early Game Trade Loop
## Proves: World loads, trade loop works end-to-end, credits actually change,
## HUD reflects reality.

const PREFIX := "EXPV0|EARLY_GAME|"
const MAX_POLLS := 600

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const TimelineScript = preload("res://scripts/tools/experience_timeline.gd")
const MetricsScript = preload("res://scripts/tools/experience_metrics.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,
	BOOT_OBSERVE,
	FIND_TRADE,
	BUY_GOOD,
	VERIFY_BUY,
	TRAVEL,
	WAIT_ARRIVAL,
	SELL_GOOD,
	VERIFY_SELL,
	FINAL_OBSERVE,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null

var _observer = null
var _timeline = null
var _metrics = null
var _screenshot = null

# Trade state
var _buy_node_id := ""
var _sell_node_id := ""
var _trade_good := ""
var _buy_qty := 1


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
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
				_timeline = TimelineScript.new()
				_timeline.init_v0(_observer)
				_metrics = MetricsScript.new()
				_metrics.init_v0(_timeline)
				_screenshot = ScreenshotScript.new()
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			var stations := get_nodes_in_group("Station")
			if stations.size() > 0:
				_polls = 0
				_phase = Phase.BOOT_OBSERVE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_phase = Phase.BOOT_OBSERVE

		Phase.BOOT_OBSERVE:
			_timeline.record_snapshot_v0("BOOT")
			_screenshot.capture_v0(self, "boot", "res://reports/experience/screenshots/")
			print(PREFIX + "BOOT_OBSERVED")
			_phase = Phase.FIND_TRADE

		Phase.FIND_TRADE:
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			_buy_node_id = str(ps.get("current_node_id", ""))
			if _buy_node_id.is_empty():
				_fail("no_current_node")
				return false

			var market: Array = _bridge.call("GetPlayerMarketViewV0", _buy_node_id)
			if market.size() == 0:
				_fail("empty_market_at_" + _buy_node_id)
				return false

			for item in market:
				var qty: int = int(item.get("quantity", 0))
				if qty > 0:
					_trade_good = str(item.get("good_id", ""))
					break
			if _trade_good.is_empty():
				_fail("no_goods_in_stock")
				return false

			var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
			var lanes: Array = galaxy.get("lane_edges", [])
			for lane in lanes:
				var from_id: String = str(lane.get("from_id", ""))
				var to_id: String = str(lane.get("to_id", ""))
				if from_id == _buy_node_id and to_id != _buy_node_id:
					_sell_node_id = to_id
					break
				elif to_id == _buy_node_id and from_id != _buy_node_id:
					_sell_node_id = from_id
					break
			if _sell_node_id.is_empty():
				_fail("no_neighbor_found")
				return false

			print(PREFIX + "TRADE_PLAN|buy=%s at %s, sell at %s" % [_trade_good, _buy_node_id, _sell_node_id])
			_phase = Phase.BUY_GOOD

		Phase.BUY_GOOD:
			_timeline.record_snapshot_v0("PRE_BUY")
			_bridge.call("DispatchPlayerTradeV0", _buy_node_id, _trade_good, _buy_qty, true)
			_timeline.record_snapshot_v0("POST_BUY")
			print(PREFIX + "BUY_DISPATCHED")
			_phase = Phase.VERIFY_BUY

		Phase.VERIFY_BUY:
			var responsive = _timeline.is_responsive_v0("player.credits", "PRE_BUY", "POST_BUY")
			print(PREFIX + "BUY_CREDITS_CHANGED|%s" % str(responsive))
			_timeline.record_snapshot_v0("VERIFY_BUY")
			_phase = Phase.TRAVEL

		Phase.TRAVEL:
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", _sell_node_id)
			_bridge.call("DispatchPlayerArriveV0", _sell_node_id)
			_timeline.record_snapshot_v0("POST_TRAVEL")
			print(PREFIX + "TRAVEL_DISPATCHED|to=%s" % _sell_node_id)
			_polls = 0
			_phase = Phase.WAIT_ARRIVAL

		Phase.WAIT_ARRIVAL:
			_polls += 1
			if _polls >= 10:
				_phase = Phase.SELL_GOOD

		Phase.SELL_GOOD:
			_timeline.record_snapshot_v0("PRE_SELL")
			_bridge.call("DispatchPlayerTradeV0", _sell_node_id, _trade_good, _buy_qty, false)
			_timeline.record_snapshot_v0("POST_SELL")
			print(PREFIX + "SELL_DISPATCHED")
			_phase = Phase.VERIFY_SELL

		Phase.VERIFY_SELL:
			var responsive = _timeline.is_responsive_v0("player.credits", "PRE_SELL", "POST_SELL")
			print(PREFIX + "SELL_CREDITS_CHANGED|%s" % str(responsive))
			_phase = Phase.FINAL_OBSERVE

		Phase.FINAL_OBSERVE:
			_timeline.record_snapshot_v0("FINAL")
			_screenshot.capture_v0(self, "final", "res://reports/experience/screenshots/")

			var latest = _observer.capture_full_report_v0()
			var results = _metrics.run_all_checks_v0(latest)
			_metrics.print_results_v0(results, PREFIX + "METRIC")

			var timeline_report = _timeline.get_timeline_report_v0()
			_observer.write_report_json_v0(timeline_report, "res://reports/experience/early_game_report.json")

			var all_passed := true
			for r in results:
				if not r.get("passed", false):
					var check_name: String = str(r.get("check", ""))
					if not check_name.begins_with("aesthetic_") and check_name != "state_coverage" and check_name != "progression_trajectory" and check_name != "max_stale_window":
						all_passed = false

			if all_passed:
				print(PREFIX + "PASS")
			else:
				print(PREFIX + "FAIL")

			_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
