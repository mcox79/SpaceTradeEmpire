extends SceneTree

# Headless proof for GATE.S1.GALAXY_MAP.DISCOVERY_STATES.001.
# Contract:
# - No timestamps / wall-clock
# - Stable token output only (GMD|)
# - Exits nonzero on first failure
# - Proves: HIDDEN nodes absent from overlay, VISITED shows name,
#   RUMORED shows ???, MAPPED shows name+count; per GetGalaxySnapshotV0.

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 60
const OVERLAY_FRAMES := 5   # frames after opening overlay for deferred RefreshFromSnapshotV0

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("GMD|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("GMD|OK|" + msg)

func _initialize() -> void:
	print("GMD|BOOT")
	call_deferred("_run")

func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	for _i in range(BOOT_FRAMES):
		await physics_frame

	# --- Read galaxy snapshot from SimBridge ---
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null or not bridge.has_method("GetGalaxySnapshotV0"):
		_fail("NO_SIMBRIDGE")
		return

	var galaxy_snap = bridge.call("GetGalaxySnapshotV0")
	if galaxy_snap == null:
		_fail("GALAXY_SNAP_NULL")
		return

	# --- Validate display_state_token contracts from snapshot ---
	var nodes = galaxy_snap.get("system_nodes", [])
	var hidden_count := 0
	var visited_count := 0
	var rumored_count := 0
	var mapped_count := 0
	var visited_has_name := false

	for node_dict in nodes:
		var token = str(node_dict.get("display_state_token", ""))
		var text = str(node_dict.get("display_text", ""))
		match token:
			"HIDDEN":
				hidden_count += 1
				if text != "":
					_fail("HIDDEN_HAS_DISPLAY_TEXT|" + text)
					return
			"VISITED":
				visited_count += 1
				if text == "":
					_fail("VISITED_EMPTY_DISPLAY_TEXT")
					return
				visited_has_name = true
			"RUMORED":
				rumored_count += 1
				if text != "???":
					_fail("RUMORED_WRONG_TEXT|" + text)
					return
			"MAPPED":
				mapped_count += 1
				if "+" not in text:
					_fail("MAPPED_WRONG_TEXT|" + text)
					return

	_ok("total_nodes=" + str(nodes.size()))
	_ok("hidden_count=" + str(hidden_count))
	_ok("visited_count=" + str(visited_count))
	_ok("rumored_count=" + str(rumored_count))
	_ok("mapped_count=" + str(mapped_count))
	_ok("visited_has_name=" + str(visited_has_name))

	if visited_count < 1:
		_fail("NO_VISITED_NODE")
		return
	if hidden_count < 1:
		_fail("NO_HIDDEN_NODES_EXPECTED_IN_FRESH_WORLD")
		return

	# --- Open overlay and verify HIDDEN suppression ---
	var game_manager = inst.get_node_or_null("GameManager")
	if game_manager == null or not game_manager.has_method("toggle_galaxy_map_overlay_v0"):
		_fail("NO_GAME_MANAGER")
		return

	game_manager.call("toggle_galaxy_map_overlay_v0")

	for _i in range(OVERLAY_FRAMES):
		await physics_frame

	var galaxy_view = inst.get_node_or_null("GalaxyView")
	if galaxy_view == null or not galaxy_view.has_method("GetOverlayMetricsV0"):
		_fail("NO_GALAXY_VIEW")
		return

	var metrics = galaxy_view.call("GetOverlayMetricsV0")
	var overlay_node_count = int(metrics.get("node_count", -1))
	var player_highlighted = bool(metrics.get("player_node_highlighted", false))

	_ok("overlay_node_count=" + str(overlay_node_count))
	_ok("player_highlighted=" + str(player_highlighted))

	# Non-hidden count: VISITED + RUMORED + MAPPED rendered in overlay.
	var non_hidden_count = visited_count + rumored_count + mapped_count
	if overlay_node_count != non_hidden_count:
		_fail("HIDDEN_NOT_SUPPRESSED|overlay=" + str(overlay_node_count) + "|expected=" + str(non_hidden_count))
		return

	if not player_highlighted:
		_fail("PLAYER_NODE_NOT_HIGHLIGHTED")
		return

	_ok("DONE")
	_stop_sim_and_quit(0)
