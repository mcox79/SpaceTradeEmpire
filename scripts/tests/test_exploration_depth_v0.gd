extends SceneTree

# GATE.S15.FEEL.EXPLORATION_PROOF.001
# Headless proof: verifies exploration gates work end-to-end.
# Checks: DiscoverySitePanel exists, DirectionalLight3D present, NPC fleet markers active,
# GetDiscoverySnapshotV0 wired, GetDiscoveryOutcomesV0 wired.
# Emits: PROOF|EXPLORATION_DEPTH|PASS or PROOF|EXPLORATION_DEPTH|FAIL

const PREFIX := "EXPD|"
const MAX_POLLS := 600  # 10 seconds at ~60fps

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_SCENE,
	RUN_CHECKS,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null


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
				_phase = Phase.WAIT_SCENE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_SCENE:
			# Wait until at least one Station node is in the scene (scene loaded)
			var stations := get_nodes_in_group("Station")
			if stations.size() > 0:
				_polls = 0
				_phase = Phase.RUN_CHECKS
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					# Scene may not have stations; run checks anyway
					_phase = Phase.RUN_CHECKS

		Phase.RUN_CHECKS:
			_run_checks()
			_phase = Phase.DONE

		Phase.DONE:
			pass

	return false


func _run_checks() -> void:
	var pass_count := 0
	var total := 5

	# --- CHECK 1: DiscoverySitePanel exists in scene ---
	var panel_pass := false
	var panel = root.find_child("DiscoverySitePanel", true, false)
	panel_pass = panel != null
	print("HSS: DiscoverySitePanel ... " + ("PASS" if panel_pass else "FAIL"))
	if panel_pass:
		pass_count += 1

	# --- CHECK 2: DirectionalLight3D exists (star lighting) ---
	var light_pass := false
	var lights := get_nodes_in_group("DirectionalLight3D")
	if lights.size() > 0:
		light_pass = true
	else:
		# find_child fallback: search scene tree for any DirectionalLight3D
		var found = root.find_child("DirectionalLight3D", true, false)
		light_pass = found != null
	print("HSL: StarLighting ... " + ("PASS" if light_pass else "FAIL"))
	if light_pass:
		pass_count += 1

	# --- CHECK 3: NPC fleets exist in sim state ---
	var fleet_pass := false
	if _bridge.has_method("GetNodeFleetBreakdownV0"):
		# Query a known starting node; any non-empty fleet count at star_0 is valid
		var bd: Dictionary = _bridge.call("GetNodeFleetBreakdownV0", "star_0")
		# Also check galaxy-wide fleet snapshot
		var fleet_snap: Array = []
		if _bridge.has_method("GetFleetExplainSnapshot"):
			fleet_snap = _bridge.call("GetFleetExplainSnapshot")
		fleet_pass = bd.size() > 0 or fleet_snap.size() > 0
	print("HSF: NpcFleetMarkers ... " + ("PASS" if fleet_pass else "FAIL"))
	if fleet_pass:
		pass_count += 1

	# --- CHECK 4: GetDiscoverySnapshotV0 bridge method exists ---
	var snap_pass := _bridge.has_method("GetDiscoverySnapshotV0")
	if snap_pass:
		# Verify it returns a valid Array (even if empty)
		var snap: Array = _bridge.call("GetDiscoverySnapshotV0", "star_0")
		snap_pass = snap is Array
	print("HSS: DiscoverySnapshotBridge ... " + ("PASS" if snap_pass else "FAIL"))
	if snap_pass:
		pass_count += 1

	# --- CHECK 5: GetDiscoveryOutcomesV0 bridge method exists ---
	var outcomes_pass := _bridge.has_method("GetDiscoveryOutcomesV0")
	if outcomes_pass:
		var outcomes: Array = _bridge.call("GetDiscoveryOutcomesV0")
		outcomes_pass = outcomes is Array
	print("HSS: DiscoveryOutcomesBridge ... " + ("PASS" if outcomes_pass else "FAIL"))
	if outcomes_pass:
		pass_count += 1

	# --- Summary ---
	print("---")
	if pass_count == total:
		print("PROOF|EXPLORATION_DEPTH|PASS (%d/%d)" % [pass_count, total])
	else:
		print("PROOF|EXPLORATION_DEPTH|FAIL (%d/%d)" % [pass_count, total])

	_quit()


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	print("PROOF|EXPLORATION_DEPTH|FAIL")
	_phase = Phase.DONE
	_quit()


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
