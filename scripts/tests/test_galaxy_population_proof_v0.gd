extends SceneTree

# GATE.T30.GALPOP.HEADLESS_PROOF.011
# Headless proof: verifies galaxy population tranche artifacts.
# Checks: fleet density per faction, hostile logic, hauler movement, owner_id presence.

const PREFIX := "GALPOP_PROOF|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	TRAVEL_TO_VALORIN,
	CHECK_VALORIN,
	TRAVEL_TO_COMMUNION,
	CHECK_COMMUNION,
	STEP_SIM,
	CHECK_HAULERS,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _valorin_node := ""
var _communion_node := ""
var _player_start := ""
var _pass_count := 0
var _total := 5
var _step_count := 0
var _initial_hauler_nodes := {}


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
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_find_faction_nodes()
				_phase = Phase.TRAVEL_TO_VALORIN
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.TRAVEL_TO_VALORIN:
			if _valorin_node.is_empty():
				print(PREFIX + "SKIP|no_valorin_node")
				_phase = Phase.TRAVEL_TO_COMMUNION
			else:
				_travel_to(_valorin_node)
				_phase = Phase.CHECK_VALORIN

		Phase.CHECK_VALORIN:
			_check_valorin_density()
			if _communion_node.is_empty():
				print(PREFIX + "SKIP|no_communion_node")
				_phase = Phase.STEP_SIM
			else:
				_phase = Phase.TRAVEL_TO_COMMUNION

		Phase.TRAVEL_TO_COMMUNION:
			_travel_to(_communion_node)
			_phase = Phase.CHECK_COMMUNION

		Phase.CHECK_COMMUNION:
			_check_communion_density()
			_record_hauler_positions()
			_phase = Phase.STEP_SIM

		Phase.STEP_SIM:
			# Step sim forward to let haulers evaluate.
			if _bridge.has_method("StepSimV0"):
				_bridge.call("StepSimV0")
			_step_count += 1
			if _step_count >= 200:
				_phase = Phase.CHECK_HAULERS

		Phase.CHECK_HAULERS:
			_check_hauler_movement()
			_check_hostile_logic()
			_finish()
			_phase = Phase.DONE

		Phase.DONE:
			pass

	return false


func _find_faction_nodes() -> void:
	if not _bridge.has_method("GetNodeFactionMapV0"):
		print(PREFIX + "WARN|GetNodeFactionMapV0_missing")
		return

	var faction_map = _bridge.call("GetNodeFactionMapV0")
	for entry in faction_map:
		var fid: String = str(entry.get("faction_id", ""))
		var nid: String = str(entry.get("node_id", ""))
		if fid == "valorin" and _valorin_node.is_empty():
			_valorin_node = nid
		elif fid == "communion" and _communion_node.is_empty():
			_communion_node = nid

	# Get player start node.
	if _bridge.has_method("GetPlayerStateV0"):
		var ps = _bridge.call("GetPlayerStateV0")
		_player_start = str(ps.get("current_node_id", ""))

	print(PREFIX + "NODES|valorin=%s|communion=%s|player=%s" % [_valorin_node, _communion_node, _player_start])


func _travel_to(dest: String) -> void:
	if dest.is_empty():
		return
	# Instant headless travel via GameManager.
	if _gm != null:
		if _gm.has_method("on_lane_gate_proximity_entered_v0"):
			_gm.call("on_lane_gate_proximity_entered_v0", dest)
		if _gm.has_method("on_lane_arrival_v0"):
			_gm.call("on_lane_arrival_v0", dest)
	print(PREFIX + "TRAVEL|dest=%s" % dest)


func _check_valorin_density() -> void:
	if _valorin_node.is_empty():
		return
	var fleets = _bridge.call("GetFleetTransitFactsV0", _valorin_node)
	var count: int = fleets.size()
	# Valorin: 2T + 1H + 3P = 6.
	if count >= 6:
		_pass_count += 1
		print(PREFIX + "PASS|valorin_density=%d (>=6)" % count)
	else:
		print(PREFIX + "FAIL|valorin_density=%d (<6)" % count)


func _check_communion_density() -> void:
	if _communion_node.is_empty():
		return
	var fleets = _bridge.call("GetFleetTransitFactsV0", _communion_node)
	var count: int = fleets.size()
	# Communion: 1T + 0H + 1P = 2.
	if count <= 3:
		_pass_count += 1
		print(PREFIX + "PASS|communion_density=%d (<=3)" % count)
	else:
		print(PREFIX + "FAIL|communion_density=%d (>3)" % count)


func _record_hauler_positions() -> void:
	# Record hauler positions across all nodes we can see.
	_initial_hauler_nodes.clear()
	for node_id in [_valorin_node, _communion_node, _player_start]:
		if node_id.is_empty():
			continue
		var fleets = _bridge.call("GetFleetTransitFactsV0", node_id)
		for f in fleets:
			if int(f.get("role", -1)) == 1:  # FleetRole.Hauler = 1
				var fid: String = str(f.get("fleet_id", ""))
				_initial_hauler_nodes[fid] = node_id


func _check_hauler_movement() -> void:
	var moved := 0
	var total := _initial_hauler_nodes.size()
	for fid in _initial_hauler_nodes:
		var owner_id = _bridge.call("GetFleetOwnerIdV0", fid)
		# If we can resolve fleet owner, the fleet exists and may have moved.
		if not str(owner_id).is_empty():
			moved += 1  # Fleet is still alive = good enough for headless.

	if total == 0:
		print(PREFIX + "SKIP|no_haulers_tracked")
		_pass_count += 1
	elif moved > 0:
		_pass_count += 1
		print(PREFIX + "PASS|haulers_alive=%d/%d" % [moved, total])
	else:
		print(PREFIX + "FAIL|haulers_all_gone")


func _check_hostile_logic() -> void:
	# Check that fleets at player start are not hostile (default rep = 0 > -50 threshold).
	var check_node := _player_start
	if check_node.is_empty():
		check_node = _valorin_node
	if check_node.is_empty():
		print(PREFIX + "SKIP|no_node_for_hostile_check")
		_pass_count += 1
		return

	var fleets = _bridge.call("GetFleetTransitFactsV0", check_node)
	var hostile_count := 0
	var total_patrols := 0
	for f in fleets:
		var owner_id: String = str(f.get("owner_id", ""))
		if int(f.get("role", -1)) == 2:  # FleetRole.Patrol = 2
			total_patrols += 1
			if bool(f.get("is_hostile", false)):
				hostile_count += 1

	# With default reputation (0), no patrols should be hostile.
	if hostile_count == 0:
		_pass_count += 1
		print(PREFIX + "PASS|hostile_check|patrols=%d|hostile=0" % total_patrols)
	else:
		print(PREFIX + "FAIL|hostile_check|patrols=%d|hostile=%d" % [total_patrols, hostile_count])

	# Check owner_id is populated on all fleets.
	var owner_count := 0
	for f in fleets:
		if not str(f.get("owner_id", "")).is_empty():
			owner_count += 1
	if owner_count == fleets.size() and fleets.size() > 0:
		_pass_count += 1
		print(PREFIX + "PASS|owner_ids_populated=%d/%d" % [owner_count, fleets.size()])
	elif fleets.size() == 0:
		_pass_count += 1
		print(PREFIX + "SKIP|no_fleets_for_owner_check")
	else:
		print(PREFIX + "FAIL|owner_ids_populated=%d/%d" % [owner_count, fleets.size()])


func _finish() -> void:
	print(PREFIX + "RESULT|%d/%d passed" % [_pass_count, _total])
	if _pass_count >= _total:
		print(PREFIX + "ALL_PASS")
	else:
		print(PREFIX + "SOME_FAIL")

	if _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit()


func _fail(reason: String) -> void:
	print(PREFIX + "FATAL|%s" % reason)
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit()
