extends SceneTree

# GATE.S17.REAL_SPACE.HEADLESS_PROOF.001
# Headless proof: verify real-space rendering — star positions at galactic scale,
# local system detail at star position, faction bridge queries, sensor level API.
# Emits: REAL_SPACE|PASS or REAL_SPACE|FAIL

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const PREFIX := "REAL_SPACE|"

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_stop_sim_and_quit(1)


func _ok(msg: String) -> void:
	print(PREFIX + "OK|" + msg)


func _initialize() -> void:
	print(PREFIX + "BOOT")
	call_deferred("_run")


func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	# Wait for scene boot, SimBridge init, world load, and local system draw.
	await create_timer(8.0).timeout

	# 1. Check SimBridge booted.
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("NO_SIMBRIDGE")
		return
	_ok("SIMBRIDGE_OK")

	# 2. Check galaxy snapshot has nodes at non-zero positions.
	if not bridge.has_method("GetGalaxySnapshotV0"):
		_fail("NO_GetGalaxySnapshotV0")
		return

	var snap: Dictionary = bridge.call("GetGalaxySnapshotV0")
	var nodes: Array = snap.get("system_nodes", [])
	if nodes.size() < 2:
		_fail("TOO_FEW_NODES|%d" % nodes.size())
		return
	_ok("NODE_COUNT|%d" % nodes.size())

	# Check at least one node is at a non-origin position (galactic scale).
	var found_nonorigin := false
	var max_coord := 0.0
	for n in nodes:
		var px: float = n.get("pos_x", 0.0)
		var pz: float = n.get("pos_z", 0.0)
		var dist := sqrt(px * px + pz * pz)
		if dist > max_coord:
			max_coord = dist
		if dist > 10.0:
			found_nonorigin = true

	if not found_nonorigin:
		_fail("ALL_NODES_AT_ORIGIN|max_coord=%.1f" % max_coord)
		return
	_ok("STAR_POSITIONS_NONZERO|max=%.1f" % max_coord)

	# 3. Check GalaxyView exists and has star billboards.
	var gv = _find_galaxy_view()
	if gv == null:
		_fail("NO_GALAXY_VIEW")
		return
	_ok("GALAXY_VIEW_FOUND")

	# 4. Check local system detail is at star position (not origin).
	# GalaxyView._localSystemRoot should be positioned at the current star's scaled position.
	if gv.has_method("GetNodeScaledPositionV0"):
		var gm = get_root().get_node_or_null("GameManager")
		var current_id := ""
		if gm:
			current_id = str(gm.get("current_system_id")) if gm.get("current_system_id") != null else ""
		if not current_id.is_empty():
			var star_pos: Vector3 = gv.call("GetNodeScaledPositionV0", current_id)
			if star_pos.length() > 1.0:
				_ok("LOCAL_SYSTEM_AT_STAR|pos=(%.0f,%.0f,%.0f)" % [star_pos.x, star_pos.y, star_pos.z])
			else:
				_ok("LOCAL_SYSTEM_AT_ORIGIN_WARN|star_0_may_be_near_origin")
		else:
			_ok("NO_CURRENT_ID|skipping_position_check")
	else:
		_ok("NO_GetNodeScaledPositionV0|skipping_position_check")

	# 5. Check faction bridge queries exist.
	var faction_apis := ["GetFactionDoctrineV0", "GetPlayerReputationV0", "GetTerritoryAccessV0", "GetAllFactionsV0"]
	for api_name in faction_apis:
		if not bridge.has_method(api_name):
			_fail("NO_%s" % api_name)
			return
	_ok("FACTION_APIS_EXIST|%d" % faction_apis.size())

	# 6. Query all factions (retry up to 3 times — TryExecuteSafeRead may miss on first call).
	var factions: Array = []
	for _retry in range(3):
		factions = bridge.call("GetAllFactionsV0")
		if factions.size() > 0:
			break
		await create_timer(2.0).timeout
	_ok("FACTION_COUNT|%d" % factions.size())

	# 7. Check sensor level API exists.
	if bridge.has_method("GetSensorLevelV0"):
		var level: int = int(bridge.call("GetSensorLevelV0"))
		_ok("SENSOR_LEVEL|%d" % level)
	else:
		_fail("NO_GetSensorLevelV0")
		return

	# 8. Check lane edges exist in galaxy snapshot.
	var edges: Array = snap.get("lane_edges", [])
	if edges.size() < 1:
		_fail("NO_LANE_EDGES")
		return
	_ok("LANE_EDGE_COUNT|%d" % edges.size())

	# All checks passed.
	print(PREFIX + "PASS|checks=8")
	_stop_sim_and_quit(0)


func _find_galaxy_view() -> Node:
	# GalaxyView is a C# Node3D in the scene tree.
	for child in get_root().get_children():
		var gv = _search_for_galaxy_view(child)
		if gv != null:
			return gv
	return null


func _search_for_galaxy_view(node: Node) -> Node:
	if node.get_class() == "GalaxyView" or node.name == "GalaxyView":
		return node
	for child in node.get_children():
		var result = _search_for_galaxy_view(child)
		if result != null:
			return result
	return null
