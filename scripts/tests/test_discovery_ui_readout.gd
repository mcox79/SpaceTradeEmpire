extends SceneTree

# Deterministic transcript printer for GATE.S3_6.DISCOVERY_STATE.006
# Output lines are prefixed with "DUIR|" so tooling can filter out unrelated Godot logs.
# No timestamps. Ordering is inherited from SimBridge snapshot (DiscoveryId asc).

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	# Allow autoloads (SimBridge) to initialize.
	await process_frame
	await process_frame

	var bridge = root.get_node_or_null("SimBridge")
	if bridge == null:
		print("DUIR|DISCOVERY_UI_READOUT_V0")
		print("DUIR|ERROR: SimBridge not found")
		quit(1)
		return

	if not bridge.has_method("GetDiscoveryListSnapshotV0"):
		print("DUIR|DISCOVERY_UI_READOUT_V0")
		print("DUIR|ERROR: GetDiscoveryListSnapshotV0 missing")
		quit(1)
		return

	var seed_val := 0
	if bridge.has_method("get"):
		seed_val = int(bridge.get("WorldSeed"))

	print("DUIR|DISCOVERY_UI_READOUT_V0")
	print("DUIR|SEED:%s" % str(seed_val))
	print("DUIR|FIELDS:discovery_id|seen_bps|scanned_bps|analyzed_bps")

	var list = bridge.call("GetDiscoveryListSnapshotV0")
	if typeof(list) != TYPE_ARRAY:
		print("DUIR|ERROR: snapshot not array")
		quit(1)
		return

	for e in list:
		if typeof(e) != TYPE_DICTIONARY:
			continue
		var discovery_id = str(e.get("discovery_id", ""))
		var seen_bps = int(e.get("seen_bps", 0))
		var scanned_bps = int(e.get("scanned_bps", 0))
		var analyzed_bps = int(e.get("analyzed_bps", 0))
		print("DUIR|%s|%s|%s|%s" % [discovery_id, seen_bps, scanned_bps, analyzed_bps])

	quit(0)
