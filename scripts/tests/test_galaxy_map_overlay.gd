extends SceneTree

# Headless proof for GATE.S1.GALAXY_MAP.RENDER.001
# Emits exactly one deterministic line:
# UUIR|node_count=<n>|edge_count=<n>|player_node_highlighted=<bool>|local_scene_ticking=<bool>

func _initialize():
	var packed = load("res://scenes/playable_prototype.tscn")
	var root = packed.instantiate()
	root.name = "Main"
	get_root().add_child(root)

	# Let _ready run deterministically
	await process_frame
	await process_frame

	var gm = get_root().get_node_or_null("Main/GameManager")
	var gv = get_root().get_node_or_null("Main/GalaxyView")

	# Open overlay deterministically without synthesizing input events.
	if gm and gm.has_method("toggle_galaxy_map_overlay_v0"):
		gm.call("toggle_galaxy_map_overlay_v0")

	# Allow one frame of GalaxyView refresh
	await process_frame

	var node_count = 0
	var edge_count = 0
	var player_node_highlighted = false
	if gv and gv.has_method("GetOverlayMetricsV0"):
		var m = gv.call("GetOverlayMetricsV0")
		if typeof(m) == TYPE_DICTIONARY:
			node_count = int(m.get("node_count", 0))
			edge_count = int(m.get("edge_count", 0))
			player_node_highlighted = bool(m.get("player_node_highlighted", false))

	# Verify local scene ticking continues while overlay is open (boolean only).
	var t0 = 0.0
	var t1 = 0.0
	if gm:
		t0 = float(gm.get("time_accumulator"))
	await process_frame
	await process_frame
	if gm:
		t1 = float(gm.get("time_accumulator"))
	var local_scene_ticking = (t1 != t0)

	print("UUIR|node_count=%d|edge_count=%d|player_node_highlighted=%s|local_scene_ticking=%s" % [
		node_count,
		edge_count,
		str(player_node_highlighted).to_lower(),
		str(local_scene_ticking).to_lower()
	])

	quit()
