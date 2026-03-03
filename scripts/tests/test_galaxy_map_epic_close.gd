extends SceneTree

# Headless proof for GATE.S1.GALAXY_MAP_PROTO.EPIC_CLOSE.001
# Closes EPIC.S1.GALAXY_MAP_PROTO.V0
# Emits: GME|PASS|node_count=N|edge_count=M|player_highlighted=true|ticking=true
#    or: GME|FAIL|reason=<msg>

func _initialize():
	var packed = load("res://scenes/playable_prototype.tscn")
	var root = packed.instantiate()
	root.name = "Main"
	get_root().add_child(root)

	await process_frame
	await process_frame

	var gm = get_root().get_node_or_null("Main/GameManager")
	var gv = get_root().get_node_or_null("Main/GalaxyView")

	# Open overlay
	if gm and gm.has_method("toggle_galaxy_map_overlay_v0"):
		gm.call("toggle_galaxy_map_overlay_v0")

	await process_frame

	# Gather metrics
	var node_count = 0
	var edge_count = 0
	var player_highlighted = false
	if gv and gv.has_method("GetOverlayMetricsV0"):
		var m = gv.call("GetOverlayMetricsV0")
		if typeof(m) == TYPE_DICTIONARY:
			node_count = int(m.get("node_count", 0))
			edge_count = int(m.get("edge_count", 0))
			player_highlighted = bool(m.get("player_node_highlighted", false))

	# Check scene ticking
	var t0 = 0.0
	var t1 = 0.0
	if gm:
		t0 = float(gm.get("time_accumulator"))
	await process_frame
	await process_frame
	if gm:
		t1 = float(gm.get("time_accumulator"))
	var ticking = (t1 != t0)

	# Assertions
	if node_count < 1:
		print("GME|FAIL|reason=node_count=%d_lt_1" % node_count)
		quit()
		return
	if edge_count < 1:
		print("GME|FAIL|reason=edge_count=%d_lt_1" % edge_count)
		quit()
		return
	if not player_highlighted:
		print("GME|FAIL|reason=player_node_highlighted=false")
		quit()
		return
	if not ticking:
		print("GME|FAIL|reason=local_scene_ticking=false")
		quit()
		return

	print("GME|PASS|node_count=%d|edge_count=%d|player_highlighted=%s|ticking=%s" % [
		node_count,
		edge_count,
		str(player_highlighted).to_lower(),
		str(ticking).to_lower()
	])

	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit()
