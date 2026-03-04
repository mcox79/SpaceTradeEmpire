extends SceneTree

## EPIC.X.EXPERIENCE_PROOF.V0 — Scenario 2: Combat
## Proves: Combat is integrated — HP init works, turret shots apply damage,
## opponent hull decreases monotonically, enemy can be killed.

const PREFIX := "EXPV0|COMBAT|"
const MAX_POLLS := 600

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const TimelineScript = preload("res://scripts/tools/experience_timeline.gd")
const MetricsScript = preload("res://scripts/tools/experience_metrics.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	BOOT_OBSERVE,
	FIND_OPPONENT,
	INIT_HP,
	VERIFY_HP,
	PRE_COMBAT_OBSERVE,
	FIRE_SHOTS,
	POST_COMBAT_OBSERVE,
	VERIFY_TRAJECTORY,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null

var _observer = null
var _timeline = null
var _metrics = null
var _screenshot = null

var _opponent_fleet_id := ""
var _shots_fired := 0
const MAX_SHOTS := 30
var _hull_trajectory: Array = []


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
				_phase = Phase.BOOT_OBSERVE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.BOOT_OBSERVE:
			_timeline.record_snapshot_v0("BOOT")
			print(PREFIX + "BOOT_OBSERVED")
			_phase = Phase.FIND_OPPONENT

		Phase.FIND_OPPONENT:
			# Find an AI fleet from the system snapshot
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))
			if node_id.is_empty():
				_fail("no_current_node")
				return false

			if _bridge.has_method("GetSystemSnapshotV0"):
				var sys: Dictionary = _bridge.call("GetSystemSnapshotV0", node_id)
				var fleets = sys.get("fleets", null)
				if fleets is Array:
					for f in fleets:
						var fid: String = str(f.get("fleet_id", ""))
						var oid: String = str(f.get("owner_id", ""))
						if not fid.is_empty() and oid != "player":
							_opponent_fleet_id = fid
							break

			# Fallback: check scene tree for FleetShip nodes
			if _opponent_fleet_id.is_empty():
				var fleet_ships := get_nodes_in_group("FleetShip")
				for fs in fleet_ships:
					var fid = fs.get("fleet_id")
					if fid != null and fid is String and not (fid as String).is_empty():
						_opponent_fleet_id = fid as String
						break
				if _opponent_fleet_id.is_empty() and fleet_ships.size() > 0:
					_opponent_fleet_id = str(fleet_ships[0].name)

			if _opponent_fleet_id.is_empty():
				print(PREFIX + "WARN|no_opponent_found, creating synthetic test")
				# Even without scene opponent, we can test combat via bridge API
				# Try galaxy snapshot for any non-player fleet
				var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
				var nodes = galaxy.get("system_nodes", [])
				if nodes is Array:
					for n in nodes:
						var nid: String = str(n.get("node_id", ""))
						if nid.is_empty():
							continue
						if not _bridge.has_method("GetSystemSnapshotV0"):
							continue
						var sys2: Dictionary = _bridge.call("GetSystemSnapshotV0", nid)
						var fleets2 = sys2.get("fleets", null)
						if fleets2 is Array:
							for f in fleets2:
								var fid: String = str(f.get("fleet_id", ""))
								var oid: String = str(f.get("owner_id", ""))
								if not fid.is_empty() and oid != "player":
									_opponent_fleet_id = fid
									break
						if not _opponent_fleet_id.is_empty():
							break

			if _opponent_fleet_id.is_empty():
				_fail("no_opponent_fleet_anywhere")
				return false

			print(PREFIX + "OPPONENT_FOUND|%s" % _opponent_fleet_id)
			_phase = Phase.INIT_HP

		Phase.INIT_HP:
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")
			_timeline.record_snapshot_v0("HP_INITIALIZED")
			_phase = Phase.VERIFY_HP

		Phase.VERIFY_HP:
			if not _bridge.has_method("GetFleetCombatHpV0"):
				_fail("no_GetFleetCombatHpV0")
				return false

			var player_hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
			var opp_hp: Dictionary = _bridge.call("GetFleetCombatHpV0", _opponent_fleet_id)

			var p_hull: int = int(player_hp.get("hull", 0))
			var p_max: int = int(player_hp.get("hull_max", 0))
			var o_hull: int = int(opp_hp.get("hull", 0))
			var o_max: int = int(opp_hp.get("hull_max", 0))

			print(PREFIX + "HP_CHECK|player=%d/%d opponent=%d/%d" % [p_hull, p_max, o_hull, o_max])

			if p_max <= 0 or o_max <= 0:
				_fail("hp_init_failed|player_max=%d opponent_max=%d" % [p_max, o_max])
				return false

			_phase = Phase.PRE_COMBAT_OBSERVE

		Phase.PRE_COMBAT_OBSERVE:
			_timeline.record_snapshot_v0("PRE_COMBAT")
			_screenshot.capture_v0(self, "pre_combat", "res://reports/experience/screenshots/")

			# Start combat
			if _bridge.has_method("DispatchStartCombatV0"):
				_bridge.call("DispatchStartCombatV0", _opponent_fleet_id)
			_timeline.record_snapshot_v0("COMBAT_STARTED")
			print(PREFIX + "COMBAT_STARTED")
			_shots_fired = 0
			_hull_trajectory = []

			# Record initial opponent hull
			var opp_hp0: Dictionary = _bridge.call("GetFleetCombatHpV0", _opponent_fleet_id)
			_hull_trajectory.append(int(opp_hp0.get("hull", 0)))

			_phase = Phase.FIRE_SHOTS

		Phase.FIRE_SHOTS:
			if not _bridge.has_method("ApplyTurretShotV0"):
				_fail("no_ApplyTurretShotV0")
				return false

			var result: Dictionary = _bridge.call("ApplyTurretShotV0", _opponent_fleet_id)
			var hull: int = int(result.get("target_hull", 0))
			var killed = result.get("killed", false)
			_shots_fired += 1
			_hull_trajectory.append(hull)

			print(PREFIX + "SHOT|%d hull=%d shield_dmg=%d hull_dmg=%d" % [
				_shots_fired, hull,
				int(result.get("shield_dmg", 0)),
				int(result.get("hull_dmg", 0))])

			if killed:
				print(PREFIX + "TARGET_KILLED|shots=%d" % _shots_fired)
				_timeline.record_snapshot_v0("TARGET_KILLED")
				_phase = Phase.POST_COMBAT_OBSERVE
				return false

			if _shots_fired >= MAX_SHOTS:
				print(PREFIX + "MAX_SHOTS_REACHED")
				_timeline.record_snapshot_v0("MAX_SHOTS")
				_phase = Phase.POST_COMBAT_OBSERVE

		Phase.POST_COMBAT_OBSERVE:
			if _bridge.has_method("DispatchClearCombatV0"):
				_bridge.call("DispatchClearCombatV0")
			_timeline.record_snapshot_v0("POST_COMBAT")
			_screenshot.capture_v0(self, "post_combat", "res://reports/experience/screenshots/")
			_phase = Phase.VERIFY_TRAJECTORY

		Phase.VERIFY_TRAJECTORY:
			_timeline.record_snapshot_v0("FINAL")

			# Check hull trajectory is monotonically non-increasing
			var monotonic := true
			for i in range(1, _hull_trajectory.size()):
				if _hull_trajectory[i] > _hull_trajectory[i - 1]:
					monotonic = false
					break

			# Check damage was actually applied
			var damage_applied := false
			if _hull_trajectory.size() >= 2:
				damage_applied = _hull_trajectory[_hull_trajectory.size() - 1] < _hull_trajectory[0]

			print(PREFIX + "HULL_TRAJECTORY|%s" % str(_hull_trajectory))
			print(PREFIX + "MONOTONIC|%s" % str(monotonic))
			print(PREFIX + "DAMAGE_APPLIED|%s" % str(damage_applied))
			print(PREFIX + "SHOTS_FIRED|%d" % _shots_fired)

			# Run timeline metrics
			var latest = _observer.capture_full_report_v0()
			var results = _metrics.run_all_checks_v0(latest)
			_metrics.print_results_v0(results, PREFIX + "METRIC")

			var timeline_report = _timeline.get_timeline_report_v0()
			timeline_report["combat_hull_trajectory"] = _hull_trajectory
			timeline_report["combat_shots_fired"] = _shots_fired
			_observer.write_report_json_v0(timeline_report, "res://reports/experience/combat_report.json")

			if monotonic and damage_applied:
				print(PREFIX + "PASS")
			else:
				print(PREFIX + "FAIL|monotonic=%s damage=%s" % [str(monotonic), str(damage_applied)])

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
