extends SceneTree

# Deterministic rumor lead UI readout transcript v0 (GATE.S3_6.RUMOR_INTEL_MIN.003)
# Output format:
#   RUIR|Seed=<seed>|Station=<stationId>|Lead=<leadId>|Hint=<comma-joined-tokens>|Blocked=<comma-joined-tokens>
#
# Determinism rules:
# - No timestamps.
# - Lead lines emitted in LeadId asc order as provided by SimBridge snapshot.
# - Token joining is stable: preserves array order from SimBridge (already deterministic).

const PREFIX := "RUIR|"

func _init():
	_run()

func _run() -> void:
	var seed := _get_seed_arg_or_default(42)

	# Wait for autoload SimBridge to exist and be ready.
	var bridge = null
	for _i in range(600):
		bridge = get_root().get_node_or_null("SimBridge")
		if bridge and bridge.has_method("GetBridgeReadyV0") and bridge.call("GetBridgeReadyV0"):
			break
		await process_frame

	if not bridge:
		print(PREFIX + "ERROR|SimBridgeMissing")
		quit(2)
		return

	# Prefer reading the seed from bridge when available for truth-in-output.
	var seed_out := seed
	if bridge.has_method("GetWorldSeed"):
		seed_out = int(bridge.call("GetWorldSeed"))

	var ticks := _get_ticks_arg_or_default(120)

	# Wait deterministically for station id to populate (sim thread needs time to tick).
	# Bound the wait by ticks to avoid hangs.
	var station_id := ""
	if bridge.has_method("GetPlayerSnapshot"):
		for _j in range(max(30, ticks * 2)):
			var ps = bridge.call("GetPlayerSnapshot")
			if typeof(ps) == TYPE_DICTIONARY:
				station_id = str(ps.get("location", ""))
				if station_id != "":
					break
			await process_frame

	# Call the new snapshot method.
	if not bridge.has_method("GetRumorLeadsSnapshotV0"):
		print(PREFIX + "ERROR|MissingMethod=GetRumorLeadsSnapshotV0")
		quit(2)
		return

	var leads = bridge.call("GetRumorLeadsSnapshotV0", station_id)
	if typeof(leads) != TYPE_ARRAY:
		print(PREFIX + "ERROR|BadReturnType")
		quit(2)
		return

	var emitted := 0
	for r in leads:
		if typeof(r) != TYPE_DICTIONARY:
			continue
		var lead_id := str(r.get("lead_id", ""))
		var hint_tokens = r.get("hint_tokens", [])
		var blocked = r.get("blocked_reasons", [])

		var hint_s := _join_tokens(hint_tokens)
		var blocked_s := _join_tokens(blocked)

		print(PREFIX + "Seed=" + str(seed_out) + "|Station=" + station_id + "|Lead=" + lead_id + "|Hint=" + hint_s + "|Blocked=" + blocked_s)
		emitted += 1

	# Gate expectation is ">= 1 lead line". This script emits a deterministic diagnostic line if none exist.
	if emitted <= 0:
		print(PREFIX + "Seed=" + str(seed_out) + "|Station=" + station_id + "|NO_LEADS")
		quit(1)
		return

	quit(0)

func _get_seed_arg_or_default(default_seed: int) -> int:
	var args := OS.get_cmdline_args()
	for i in range(args.size()):
		if args[i] == "--seed" and i + 1 < args.size():
			return int(args[i + 1])
	return default_seed

func _get_ticks_arg_or_default(default_ticks: int) -> int:
	var args := OS.get_cmdline_args()
	for i in range(args.size()):
		if args[i] == "--ticks" and i + 1 < args.size():
			return int(args[i + 1])
	return default_ticks

func _join_tokens(tokens) -> String:
	if typeof(tokens) != TYPE_ARRAY:
		return ""
	var s := ""
	for t in tokens:
		var seg := str(t)
		if seg == "":
			continue
		s = seg if s == "" else (s + "," + seg)
	return s
