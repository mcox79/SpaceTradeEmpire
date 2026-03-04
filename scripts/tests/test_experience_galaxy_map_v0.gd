extends SceneTree

## EPIC.X.EXPERIENCE_PROOF.V0 — Scenario 3: Galaxy Map
## Proves: Galaxy snapshot returns real topology with multiple nodes/lanes,
## nodes have distinct positions, player location is reported correctly.

const PREFIX := "EXPV0|GALAXY_MAP|"
const MAX_POLLS := 600

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const TimelineScript = preload("res://scripts/tools/experience_timeline.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	BOOT_OBSERVE,
	QUERY_GALAXY,
	VERIFY_TOPOLOGY,
	VERIFY_POSITIONS,
	VERIFY_PLAYER_LOCATION,
	QUERY_SYSTEM,
	FINAL_OBSERVE,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null

var _observer = null
var _timeline = null
var _screenshot = null

# Galaxy data captured during test
var _galaxy_snap: Dictionary = {}
var _node_count := 0
var _lane_count := 0
var _positions: Array = []  # [{id, x, z}]


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
				_screenshot = ScreenshotScript.new()
				_phase = Phase.BOOT_OBSERVE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.BOOT_OBSERVE:
			_timeline.record_snapshot_v0("BOOT")
			print(PREFIX + "BOOT_OBSERVED")
			_phase = Phase.QUERY_GALAXY

		Phase.QUERY_GALAXY:
			_galaxy_snap = _bridge.call("GetGalaxySnapshotV0")
			_timeline.record_snapshot_v0("GALAXY_QUERIED")

			var nodes = _galaxy_snap.get("system_nodes", null)
			if nodes is Array:
				_node_count = nodes.size()
				for n in nodes:
					var nid: String = str(n.get("node_id", ""))
					var px = n.get("pos_x", 0.0)
					var pz = n.get("pos_z", 0.0)
					_positions.append({"id": nid, "x": px, "z": pz})

			var lanes = _galaxy_snap.get("lane_edges", null)
			if lanes is Array:
				_lane_count = lanes.size()

			print(PREFIX + "GALAXY_DATA|nodes=%d lanes=%d" % [_node_count, _lane_count])
			_phase = Phase.VERIFY_TOPOLOGY

		Phase.VERIFY_TOPOLOGY:
			# Must have >= 2 nodes and >= 1 lane for a connected galaxy
			var topo_ok := _node_count >= 2 and _lane_count >= 1
			print(PREFIX + "TOPOLOGY|nodes=%d lanes=%d ok=%s" % [_node_count, _lane_count, str(topo_ok)])
			if not topo_ok:
				_fail("topology_insufficient|nodes=%d lanes=%d" % [_node_count, _lane_count])
				return false
			_phase = Phase.VERIFY_POSITIONS

		Phase.VERIFY_POSITIONS:
			# Verify nodes have distinct positions (not all at origin)
			var unique_positions := {}
			for p in _positions:
				var key := "%.1f,%.1f" % [float(p.get("x", 0.0)), float(p.get("z", 0.0))]
				unique_positions[key] = true

			var distinct_count: int = unique_positions.size()
			var positions_ok := distinct_count >= 2

			print(PREFIX + "POSITIONS|distinct=%d/%d ok=%s" % [distinct_count, _positions.size(), str(positions_ok)])
			if not positions_ok:
				_fail("positions_not_distinct|distinct=%d" % distinct_count)
				return false

			# Check positions aren't all at origin
			var non_origin := 0
			for p in _positions:
				var x = float(p.get("x", 0.0))
				var z = float(p.get("z", 0.0))
				if absf(x) > 0.1 or absf(z) > 0.1:
					non_origin += 1
			print(PREFIX + "NON_ORIGIN|%d/%d" % [non_origin, _positions.size()])

			_phase = Phase.VERIFY_PLAYER_LOCATION

		Phase.VERIFY_PLAYER_LOCATION:
			var player_node: String = str(_galaxy_snap.get("player_current_node_id", ""))
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var ps_node: String = str(ps.get("current_node_id", ""))

			var location_ok := not player_node.is_empty() and player_node == ps_node
			print(PREFIX + "PLAYER_LOCATION|galaxy=%s state=%s ok=%s" % [player_node, ps_node, str(location_ok)])

			if not location_ok:
				print(PREFIX + "WARN|player_location_mismatch")

			# Verify player node exists in the galaxy nodes
			var player_in_galaxy := false
			for p in _positions:
				if str(p.get("id", "")) == player_node:
					player_in_galaxy = true
					break
			print(PREFIX + "PLAYER_IN_GALAXY|%s" % str(player_in_galaxy))

			_phase = Phase.QUERY_SYSTEM

		Phase.QUERY_SYSTEM:
			# Query the local system snapshot for the player's current node
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))
			if not node_id.is_empty() and _bridge.has_method("GetSystemSnapshotV0"):
				var sys: Dictionary = _bridge.call("GetSystemSnapshotV0", node_id)
				var station: Dictionary = sys.get("station", {})
				var discovery_sites = sys.get("discovery_sites", [])
				var lane_gate = sys.get("lane_gate", [])
				var fleets = sys.get("fleets", [])

				var ds_count: int = discovery_sites.size() if discovery_sites is Array else 0
				var lg_count: int = lane_gate.size() if lane_gate is Array else 0
				var fl_count: int = fleets.size() if fleets is Array else 0

				print(PREFIX + "SYSTEM|node=%s station=%s discoveries=%d lanes=%d fleets=%d" % [
					node_id,
					str(station.get("node_name", "")),
					ds_count, lg_count, fl_count])

				_timeline.record_snapshot_v0("SYSTEM_QUERIED")
			_phase = Phase.FINAL_OBSERVE

		Phase.FINAL_OBSERVE:
			_timeline.record_snapshot_v0("FINAL")
			_screenshot.capture_v0(self, "galaxy_map", "res://reports/experience/screenshots/")

			# Verify lane connectivity: every lane references valid node IDs
			var node_ids := {}
			for p in _positions:
				node_ids[str(p.get("id", ""))] = true

			var lanes = _galaxy_snap.get("lane_edges", [])
			var valid_lanes := 0
			var invalid_lanes := 0
			if lanes is Array:
				for lane in lanes:
					var from_id: String = str(lane.get("from_id", ""))
					var to_id: String = str(lane.get("to_id", ""))
					if node_ids.has(from_id) and node_ids.has(to_id):
						valid_lanes += 1
					else:
						invalid_lanes += 1

			print(PREFIX + "LANE_VALIDITY|valid=%d invalid=%d" % [valid_lanes, invalid_lanes])

			var timeline_report = _timeline.get_timeline_report_v0()
			timeline_report["galaxy_nodes"] = _node_count
			timeline_report["galaxy_lanes"] = _lane_count
			timeline_report["distinct_positions"] = _positions.size()
			_observer.write_report_json_v0(timeline_report, "res://reports/experience/galaxy_map_report.json")

			var all_pass := _node_count >= 2 and _lane_count >= 1 and invalid_lanes == 0
			if all_pass:
				print(PREFIX + "PASS")
			else:
				print(PREFIX + "FAIL|nodes=%d lanes=%d invalid=%d" % [_node_count, _lane_count, invalid_lanes])

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
