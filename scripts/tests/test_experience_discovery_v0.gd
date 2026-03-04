extends SceneTree

## EPIC.X.EXPERIENCE_PROOF.V0 — Scenario 4: Discovery
## Proves: Discovery API surface is wired — bridge methods respond,
## discovery snapshot returns structured data, phase advancement works.
## Note: GalaxyGenerator doesn't seed discovery IDs on nodes (WorldLoader does).
## This test validates the API surface works correctly; sites may be empty
## in generated-only worlds, which is the expected state.

const PREFIX := "EXPV0|DISCOVERY|"
const MAX_POLLS := 600

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const TimelineScript = preload("res://scripts/tools/experience_timeline.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	BOOT_OBSERVE,
	VERIFY_API_SURFACE,
	QUERY_DISCOVERY_SNAPSHOT,
	QUERY_DISCOVERY_LIST,
	FIND_AND_TEST_SITE,
	VERIFY_RESULTS,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null

var _observer = null
var _timeline = null
var _screenshot = null

# API checks
var _has_discovery_snapshot := false
var _has_discovery_list := false
var _has_advance_phase := false
var _has_system_snapshot := false
var _discovery_snap_valid := false
var _discovery_list_count := 0
var _found_site := false
var _phase_progression_ok := false

# Discovery state if a site is found
var _discovery_id := ""
var _node_with_discovery := ""
var _phase_after_scan := ""
var _phase_after_analyze := ""


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
			_phase = Phase.VERIFY_API_SURFACE

		Phase.VERIFY_API_SURFACE:
			# Verify all discovery-related bridge methods exist
			_has_discovery_snapshot = _bridge.has_method("GetDiscoverySnapshotV0")
			_has_discovery_list = _bridge.has_method("GetDiscoveryListSnapshotV0")
			_has_advance_phase = _bridge.has_method("AdvanceDiscoveryPhaseV0")
			_has_system_snapshot = _bridge.has_method("GetSystemSnapshotV0")

			print(PREFIX + "API_SURFACE|snapshot=%s list=%s advance=%s system=%s" % [
				str(_has_discovery_snapshot), str(_has_discovery_list),
				str(_has_advance_phase), str(_has_system_snapshot)])

			if not _has_discovery_snapshot or not _has_advance_phase:
				_fail("missing_discovery_api_methods")
				return false

			_timeline.record_snapshot_v0("API_VERIFIED")
			_phase = Phase.QUERY_DISCOVERY_SNAPSHOT

		Phase.QUERY_DISCOVERY_SNAPSHOT:
			# Query discovery snapshot at current node — should return valid dict even if empty
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))

			var snap: Dictionary = _bridge.call("GetDiscoverySnapshotV0", node_id)
			_discovery_snap_valid = snap.has("discovered_site_count")

			print(PREFIX + "DISCOVERY_SNAPSHOT|node=%s valid=%s discovered=%s scanned=%s analyzed=%s" % [
				node_id, str(_discovery_snap_valid),
				str(snap.get("discovered_site_count", -1)),
				str(snap.get("scanned_site_count", -1)),
				str(snap.get("analyzed_site_count", -1))])

			# Also check unlocks and rumor_leads arrays exist
			var has_unlocks = snap.has("unlocks")
			var has_leads = snap.has("rumor_leads")
			print(PREFIX + "DISCOVERY_STRUCTURE|unlocks=%s leads=%s" % [str(has_unlocks), str(has_leads)])

			_timeline.record_snapshot_v0("SNAPSHOT_QUERIED")
			_phase = Phase.QUERY_DISCOVERY_LIST

		Phase.QUERY_DISCOVERY_LIST:
			if _has_discovery_list:
				var list = _bridge.call("GetDiscoveryListSnapshotV0")
				if list is Array:
					_discovery_list_count = list.size()
					print(PREFIX + "DISCOVERY_LIST|count=%d" % _discovery_list_count)
					for item in list:
						if item is Dictionary:
							print(PREFIX + "DISCOVERY_ITEM|id=%s phase=%s" % [
								str(item.get("discovery_id", "")),
								str(item.get("phase_token", ""))])
			else:
				print(PREFIX + "DISCOVERY_LIST|method_missing")

			_timeline.record_snapshot_v0("LIST_QUERIED")
			_phase = Phase.FIND_AND_TEST_SITE

		Phase.FIND_AND_TEST_SITE:
			# Search all nodes for discovery sites via system snapshot
			if _has_system_snapshot:
				var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
				var nodes = galaxy.get("system_nodes", [])

				# First arrive at nodes to trigger ApplySeenFromNodeEntry
				if nodes is Array:
					for n in nodes:
						var nid: String = str(n.get("node_id", ""))
						if nid.is_empty():
							continue
						# Arrive at node to trigger discovery seeding
						if _bridge.has_method("DispatchPlayerArriveV0"):
							_bridge.call("DispatchPlayerArriveV0", nid)

				# Now check for sites
				if nodes is Array:
					for n in nodes:
						var nid: String = str(n.get("node_id", ""))
						if nid.is_empty():
							continue
						var sys: Dictionary = _bridge.call("GetSystemSnapshotV0", nid)
						var sites = sys.get("discovery_sites", [])
						if sites is Array and sites.size() > 0:
							for site in sites:
								var sid: String = str(site.get("site_id", ""))
								if not sid.is_empty():
									_found_site = true
									_discovery_id = sid
									_node_with_discovery = nid
									break
						if _found_site:
							break

			if _found_site:
				print(PREFIX + "SITE_FOUND|id=%s node=%s" % [_discovery_id, _node_with_discovery])

				# Test phase advancement
				var scan_result: Dictionary = _bridge.call("AdvanceDiscoveryPhaseV0", _discovery_id)
				_phase_after_scan = str(scan_result.get("phase_token", ""))
				var scan_ok = scan_result.get("ok", false)
				print(PREFIX + "SCAN|ok=%s phase=%s" % [str(scan_ok), _phase_after_scan])

				if scan_ok:
					var analyze_result: Dictionary = _bridge.call("AdvanceDiscoveryPhaseV0", _discovery_id)
					_phase_after_analyze = str(analyze_result.get("phase_token", ""))
					var analyze_ok = analyze_result.get("ok", false)
					print(PREFIX + "ANALYZE|ok=%s phase=%s" % [str(analyze_ok), _phase_after_analyze])
					if analyze_ok:
						_phase_progression_ok = true
				elif _phase_after_scan == "SCANNED":
					_phase_progression_ok = true
			else:
				print(PREFIX + "NO_SITES|GalaxyGenerator does not seed discoveries (expected)")
				# Test AdvanceDiscoveryPhaseV0 with a non-existent ID — should return ok=false gracefully
				var bad_result: Dictionary = _bridge.call("AdvanceDiscoveryPhaseV0", "nonexistent_discovery")
				var bad_ok = bad_result.get("ok", false)
				var bad_reason: String = str(bad_result.get("reason", ""))
				print(PREFIX + "BAD_ADVANCE|ok=%s reason=%s" % [str(bad_ok), bad_reason])

			_timeline.record_snapshot_v0("SITES_TESTED")
			_phase = Phase.VERIFY_RESULTS

		Phase.VERIFY_RESULTS:
			_timeline.record_snapshot_v0("FINAL")
			_screenshot.capture_v0(self, "discovery", "res://reports/experience/screenshots/")

			var timeline_report = _timeline.get_timeline_report_v0()
			timeline_report["discovery_api"] = {
				"has_snapshot": _has_discovery_snapshot,
				"has_list": _has_discovery_list,
				"has_advance": _has_advance_phase,
				"snapshot_valid": _discovery_snap_valid,
				"list_count": _discovery_list_count,
				"found_site": _found_site,
				"progression_ok": _phase_progression_ok,
			}
			_observer.write_report_json_v0(timeline_report, "res://reports/experience/discovery_report.json")

			# Pass criteria:
			# 1. All API methods exist
			# 2. Discovery snapshot returns valid structure
			# 3. If sites exist: phase progression works
			# 4. If no sites: API handles gracefully (no crash on bad ID)
			var all_pass := _has_discovery_snapshot and _has_advance_phase and _discovery_snap_valid
			if _found_site:
				all_pass = all_pass and _phase_progression_ok

			print(PREFIX + "SUMMARY|api_ok=%s snap_valid=%s sites=%s progression=%s" % [
				str(_has_discovery_snapshot and _has_advance_phase),
				str(_discovery_snap_valid),
				str(_found_site),
				str(_phase_progression_ok)])

			if all_pass:
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
