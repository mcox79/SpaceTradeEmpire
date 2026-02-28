extends SceneTree

# Deterministic transcript printer for GATE.S3_6.EXPEDITION_PROGRAMS.003
# Output lines are prefixed with "ESR|" so tooling can filter out unrelated Godot logs.
# No timestamps. Ordering is inherited from program listing (Id asc) and token ordering from SimBridge snapshot.

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	# Allow autoloads (SimBridge) to initialize.
	await process_frame
	await process_frame

	var bridge = root.get_node_or_null("SimBridge")
	if bridge == null:
		print("ESR|EXPEDITION_STATUS_READOUT_V0")
		print("ESR|ERROR: SimBridge not found")
		quit(1)
		return

	if not bridge.has_method("GetProgramExplainSnapshot"):
		print("ESR|EXPEDITION_STATUS_READOUT_V0")
		print("ESR|ERROR: GetProgramExplainSnapshot missing")
		quit(1)
		return

	if not bridge.has_method("GetExpeditionStatusSnapshotV0"):
		print("ESR|EXPEDITION_STATUS_READOUT_V0")
		print("ESR|ERROR: GetExpeditionStatusSnapshotV0 missing")
		quit(1)
		return

	var seed_val := 0
	if bridge.has_method("get"):
		seed_val = int(bridge.get("WorldSeed"))

	print("ESR|EXPEDITION_STATUS_READOUT_V0")
	print("ESR|SEED:%s" % str(seed_val))
	print("ESR|FIELDS:program_id|kind_token|status_token|primary_tokens|secondary_tokens|verb_tokens")

	var progs = bridge.call("GetProgramExplainSnapshot")
	if typeof(progs) != TYPE_ARRAY:
		print("ESR|ERROR: programs not array")
		quit(1)
		return

	var expedition_count := 0
	var saw_discoveries_verb := false

	for p in progs:
		if typeof(p) != TYPE_DICTIONARY:
			continue

		var pid = str(p.get("id", ""))
		if pid == "":
			continue

		var kind = str(p.get("kind", ""))
		if kind.findn("expedition") == -1:
			continue

		expedition_count += 1

		var s = bridge.call("GetExpeditionStatusSnapshotV0", pid)
		if typeof(s) != TYPE_DICTIONARY:
			continue

		var kind_tok = str(s.get("expedition_kind_token", ""))
		var status_tok = str(s.get("status_token", ""))

		var prim = s.get("explain_primary_tokens", [])
		var sec = s.get("explain_secondary_tokens", [])
		var verbs = s.get("intervention_verb_tokens", [])

		# Search for a Discoveries.* intervention verb token (gate requirement when programs exist).
		if typeof(verbs) == TYPE_ARRAY:
			for v in verbs:
				var vs = str(v)
				if vs.begins_with("Discoveries."):
					saw_discoveries_verb = true

		print("ESR|%s|%s|%s|P:%s|S:%s|V:%s" % [pid, kind_tok, status_tok, _join_tokens(prim), _join_tokens(sec), _join_tokens(verbs)])

	# Runbook vacuous-pass: do not fail solely because player-created state is absent at init.
	if expedition_count == 0:
		print("ESR|PASS|vacuous_no_programs_at_init")
		quit(0)
		return

	if not saw_discoveries_verb:
		print("ESR|ERROR: missing Discoveries.* verb token")
		quit(1)
		return

	quit(0)

func _join_tokens(tokens) -> String:
	if typeof(tokens) != TYPE_ARRAY:
		return ""
	var out := ""
	for i in range(tokens.size()):
		var t = str(tokens[i])
		if t == "":
			continue
		if out != "":
			out += ","
		out += t
	return out
