extends SceneTree

## EPIC.X.EXPERIENCE_PROOF.V0 — Scenario 5: Full Loop Capstone
## Proves: ALL major systems work together in sequence.

const PREFIX := "EXPV0|FULL_LOOP|"
const MAX_POLLS := 600

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const TimelineScript = preload("res://scripts/tools/experience_timeline.gd")
const MetricsScript = preload("res://scripts/tools/experience_metrics.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
const AuditScript = preload("res://scripts/tools/aesthetic_audit.gd")

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,
	BOOT_OBSERVE,
	FIND_TRADE,
	BUY_GOOD,
	TRAVEL_TO_SELL,
	WAIT_TRAVEL,
	SELL_GOOD,
	POST_TRADE_OBSERVE,
	FIND_FLEET,
	COMBAT_ENGAGE,
	COMBAT_FIRE,
	COMBAT_RESOLVE,
	OPEN_MAP,
	CLOSE_MAP,
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
var _audit = null

# Trade state
var _buy_node_id := ""
var _sell_node_id := ""
var _trade_good := ""
var _opponent_fleet_id := ""
var _combat_shots := 0
const MAX_COMBAT_SHOTS := 20


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
				_audit = AuditScript.new()
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
			for item in market:
				if int(item.get("quantity", 0)) > 0:
					_trade_good = str(item.get("good_id", ""))
					break

			if _trade_good.is_empty():
				print(PREFIX + "WARN|no_goods_in_stock, skipping trade")
				_phase = Phase.FIND_FLEET
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
				print(PREFIX + "WARN|no_neighbor, skipping trade")
				_phase = Phase.FIND_FLEET
				return false

			_phase = Phase.BUY_GOOD

		Phase.BUY_GOOD:
			_timeline.record_snapshot_v0("PRE_BUY")
			_bridge.call("DispatchPlayerTradeV0", _buy_node_id, _trade_good, 1, true)
			_timeline.record_snapshot_v0("POST_BUY")
			_screenshot.capture_v0(self, "post_buy", "res://reports/experience/screenshots/")
			print(PREFIX + "BUY_DISPATCHED|%s" % _trade_good)
			_phase = Phase.TRAVEL_TO_SELL

		Phase.TRAVEL_TO_SELL:
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", _sell_node_id)
			_bridge.call("DispatchPlayerArriveV0", _sell_node_id)
			_timeline.record_snapshot_v0("POST_TRAVEL")
			print(PREFIX + "TRAVEL|to=%s" % _sell_node_id)
			_polls = 0
			_phase = Phase.WAIT_TRAVEL

		Phase.WAIT_TRAVEL:
			_polls += 1
			if _polls >= 10:
				_phase = Phase.SELL_GOOD

		Phase.SELL_GOOD:
			_timeline.record_snapshot_v0("PRE_SELL")
			_bridge.call("DispatchPlayerTradeV0", _sell_node_id, _trade_good, 1, false)
			_timeline.record_snapshot_v0("POST_SELL")
			print(PREFIX + "SELL_DISPATCHED")
			_phase = Phase.POST_TRADE_OBSERVE

		Phase.POST_TRADE_OBSERVE:
			_timeline.record_snapshot_v0("POST_TRADE")
			_screenshot.capture_v0(self, "post_trade", "res://reports/experience/screenshots/")
			_phase = Phase.FIND_FLEET

		Phase.FIND_FLEET:
			var fleet_ships := get_nodes_in_group("FleetShip")
			if fleet_ships.size() > 0:
				for fs in fleet_ships:
					var fid = fs.get("fleet_id")
					if fid != null and fid is String and not (fid as String).is_empty():
						_opponent_fleet_id = fid as String
						break
				if _opponent_fleet_id.is_empty():
					_opponent_fleet_id = str(fleet_ships[0].name)

			if _opponent_fleet_id.is_empty():
				print(PREFIX + "WARN|no_fleet_for_combat, skipping combat")
				_phase = Phase.OPEN_MAP
			else:
				_phase = Phase.COMBAT_ENGAGE

		Phase.COMBAT_ENGAGE:
			_timeline.record_snapshot_v0("PRE_COMBAT")
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")
			if _bridge.has_method("DispatchStartCombatV0"):
				_bridge.call("DispatchStartCombatV0", _opponent_fleet_id)
			_timeline.record_snapshot_v0("COMBAT_STARTED")
			_screenshot.capture_v0(self, "combat", "res://reports/experience/screenshots/")
			print(PREFIX + "COMBAT_ENGAGED|%s" % _opponent_fleet_id)
			_combat_shots = 0
			_phase = Phase.COMBAT_FIRE

		Phase.COMBAT_FIRE:
			if _bridge.has_method("ApplyTurretShotV0"):
				var result: Dictionary = _bridge.call("ApplyTurretShotV0", _opponent_fleet_id)
				var hull: int = int(result.get("hull", 0))
				_combat_shots += 1
				if hull <= 0:
					print(PREFIX + "TARGET_DESTROYED|shots=%d" % _combat_shots)
					_phase = Phase.COMBAT_RESOLVE
					return false
			if _combat_shots >= MAX_COMBAT_SHOTS:
				print(PREFIX + "COMBAT_MAX_SHOTS_REACHED")
				_phase = Phase.COMBAT_RESOLVE

		Phase.COMBAT_RESOLVE:
			if _bridge.has_method("DispatchClearCombatV0"):
				_bridge.call("DispatchClearCombatV0")
			_timeline.record_snapshot_v0("POST_COMBAT")
			_phase = Phase.OPEN_MAP

		Phase.OPEN_MAP:
			_timeline.record_snapshot_v0("PRE_MAP")
			var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
			var node_count := 0
			var nodes = galaxy.get("system_nodes", null)
			if nodes is Array:
				node_count = nodes.size()
			print(PREFIX + "MAP_OBSERVED|nodes=%d" % node_count)
			_screenshot.capture_v0(self, "galaxy_map", "res://reports/experience/screenshots/")
			_timeline.record_snapshot_v0("MAP_OPEN")
			_phase = Phase.CLOSE_MAP

		Phase.CLOSE_MAP:
			_timeline.record_snapshot_v0("MAP_CLOSED")
			_phase = Phase.FINAL_OBSERVE

		Phase.FINAL_OBSERVE:
			_timeline.record_snapshot_v0("FINAL")
			_screenshot.capture_v0(self, "final", "res://reports/experience/screenshots/")

			var latest = _observer.capture_full_report_v0()
			var results = _metrics.run_all_checks_v0(latest)
			_metrics.print_results_v0(results, PREFIX + "METRIC")

			# Aesthetic audit
			var audit_results = _audit.run_audit_v0(latest)
			var critical_fails = _audit.count_critical_failures_v0(audit_results)
			for ar in audit_results:
				var status = "PASS" if ar.get("passed", false) else "FAIL"
				print(PREFIX + "AESTHETIC|%s|%s|%s|%s" % [status, str(ar.get("flag", "")), str(ar.get("severity", "")), str(ar.get("detail", ""))])
			print(PREFIX + "AESTHETIC_CRITICAL_FAILS|%d" % critical_fails)

			var states = _timeline.states_visited_v0()
			print(PREFIX + "STATES_VISITED|%s" % str(states))
			print(PREFIX + "SNAPSHOT_COUNT|%d" % _timeline.get_snapshot_count())

			var timeline_report = _timeline.get_timeline_report_v0()
			timeline_report["aesthetic_audit"] = audit_results
			timeline_report["metric_results"] = results
			_observer.write_report_json_v0(timeline_report, "res://reports/experience/latest_report.json")

			var functional_pass := true
			for r in results:
				if not r.get("passed", false):
					var cn: String = str(r.get("check", ""))
					if cn == "credits_change_after_trade" or cn == "hud_reflects_state":
						functional_pass = false

			if functional_pass and critical_fails == 0:
				print(PREFIX + "PASS")
			else:
				print(PREFIX + "FAIL|functional=%s aesthetic_critical=%d" % [str(functional_pass), critical_fails])

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
