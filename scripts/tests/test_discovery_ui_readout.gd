extends SceneTree

# Deterministic transcript printer for GATE.S3_6.UI_DISCOVERY_MIN.001
# Output lines are prefixed with "DUIR|" so tooling can filter unrelated Godot logs.
# No timestamps.
# Ordering is inherited from SimBridge snapshot (UnlockId asc, LeadId asc, token lists stable).

enum State { WAIT_BRIDGE, WAIT_READY, WAIT_CONTENT, DONE }

const PREFIX := "DUIR|"

const MAX_BRIDGE_POLLS: int = 200
const MAX_READY_POLLS: int = 400
const MAX_CONTENT_POLLS: int = 600

var _state: int = State.WAIT_BRIDGE
var _poll_ticks: int = 0
var _bridge = null

func _initialize() -> void:
	print(PREFIX + "DISCOVERY_UI_READOUT_V1")

func _process(_delta: float) -> bool:
	match _state:
		State.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_poll_ticks = 0
				_state = State.WAIT_READY
			else:
				_poll_ticks += 1
				if _poll_ticks >= MAX_BRIDGE_POLLS:
					print(PREFIX + "ERROR: SimBridge not found")
					quit(1)
					_state = State.DONE

		State.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetCmdlineReadyV0"):
				ready = bool(_bridge.call("GetCmdlineReadyV0"))
			elif _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true

			var seed_val := int(_bridge.get("WorldSeed"))
			if ready and seed_val == 42:
				_poll_ticks = 0
				_state = State.WAIT_CONTENT
			else:
				_poll_ticks += 1
				if _poll_ticks >= MAX_READY_POLLS:
					print(PREFIX + "SEED:%s" % str(seed_val))
					print(PREFIX + "ERROR: expected_seed_42")
					quit(1)
					_state = State.DONE

		State.WAIT_CONTENT:
			var seed_val2 := int(_bridge.get("WorldSeed"))

			if not _bridge.has_method("GetDiscoverySnapshotV0"):
				print(PREFIX + "SEED:%s" % str(seed_val2))
				print(PREFIX + "ERROR: GetDiscoverySnapshotV0 missing")
				quit(1)
				_state = State.DONE
				return false

			# Get station id if available (some snapshots are station-scoped).
			var station_id := ""
			if _bridge.has_method("GetPlayerSnapshot"):
				var ps = _bridge.call("GetPlayerSnapshot")
				if typeof(ps) == TYPE_DICTIONARY:
					station_id = str(ps.get("location", ""))

			var snap = _bridge.call("GetDiscoverySnapshotV0", station_id)
			if typeof(snap) != TYPE_DICTIONARY:
				print(PREFIX + "SEED:%s" % str(seed_val2))
				print(PREFIX + "ERROR: snapshot not dictionary")
				quit(1)
				_state = State.DONE
				return false

			# Gate acceptance requires:
			# - [DISCOVERY] section with >= 1 entry (unlock or lead line)
			# - >= 1 verb token (deploy-package) for an acquired unlock
			var unlocks = snap.get("unlocks", [])
			var leads = snap.get("rumor_leads", [])

			var entry_count := 0
			if typeof(unlocks) == TYPE_ARRAY:
				entry_count += unlocks.size()
			if typeof(leads) == TYPE_ARRAY:
				entry_count += leads.size()

			var has_verb := false
			if typeof(unlocks) == TYPE_ARRAY:
				for u in unlocks:
					if typeof(u) != TYPE_DICTIONARY:
						continue
					var verbs = u.get("deploy_verb_control_tokens", [])
					if typeof(verbs) == TYPE_ARRAY and verbs.size() > 0:
						has_verb = true
						break

			if entry_count > 0 and has_verb:
				_emit_snapshot(seed_val2, station_id, snap)
				_state = State.DONE
			else:
				_poll_ticks += 1
				if _poll_ticks >= MAX_CONTENT_POLLS:
					print(PREFIX + "SEED:%s" % str(seed_val2))
					print(PREFIX + "ERROR: expected_entries_and_verb_token")
					print(PREFIX + "DEBUG: station_id=" + station_id)
					print(PREFIX + "DEBUG: unlock_count=" + str(unlocks.size() if typeof(unlocks) == TYPE_ARRAY else -1))
					print(PREFIX + "DEBUG: lead_count=" + str(leads.size() if typeof(leads) == TYPE_ARRAY else -1))
					quit(1)
					_state = State.DONE

		State.DONE:
			pass

	return false

func _emit_snapshot(seed_val: int, station_id: String, snap: Dictionary) -> void:
	print(PREFIX + "SEED:%s" % str(seed_val))
	print(PREFIX + "[DISCOVERY]")
	print(PREFIX + "STATION_ID:%s" % station_id)

	var discovered_count = int(snap.get("discovered_site_count", 0))
	var scanned_count = int(snap.get("scanned_site_count", 0))
	var analyzed_count = int(snap.get("analyzed_site_count", 0))
	var exp_tok = str(snap.get("expedition_status_token", ""))

	print(PREFIX + "SITE_COUNTS:%s|%s|%s" % [discovered_count, scanned_count, analyzed_count])
	print(PREFIX + "EXPEDITION_STATUS_TOKEN:%s" % exp_tok)

	# GATE.S3_6.UI_DISCOVERY_MIN.002
	# Emit exceptions only when present so baseline hash remains unchanged if empty.
	var active_ex = snap.get("active_exceptions", [])
	if typeof(active_ex) == TYPE_ARRAY and active_ex.size() > 0:
		for ex in active_ex:
			if typeof(ex) != TYPE_DICTIONARY:
				continue
			var ex_tok = str(ex.get("exception_token", ""))
			var reason_tokens = ex.get("reason_tokens", [])
			var verbs = ex.get("intervention_verbs", [])
			print(PREFIX + "EXCEPTION:%s|REASONS:%s|ACTIONS:%s" % [ex_tok, _join_tokens(reason_tokens), _join_tokens(verbs)])

	var unlocks = snap.get("unlocks", [])
	if typeof(unlocks) == TYPE_ARRAY:
		for u in unlocks:
			if typeof(u) != TYPE_DICTIONARY:
				continue
			var unlock_id = str(u.get("unlock_id", ""))
			var effects = u.get("effect_tokens", [])
			var blocked_reason = str(u.get("blocked_reason_token", ""))
			var blocked_actions = u.get("blocked_action_tokens", [])
			var deploy_verbs = u.get("deploy_verb_control_tokens", [])
			print(PREFIX + "UNLOCK:%s|EFFECTS:%s|BLOCKED_REASON:%s|ACTIONS:%s|DEPLOY_VERB_TOKENS:%s" % [
				unlock_id,
				_join_tokens(effects),
				blocked_reason,
				_join_tokens(blocked_actions),
				_join_tokens(deploy_verbs)
			])

	var leads = snap.get("rumor_leads", [])
	if typeof(leads) == TYPE_ARRAY:
		for r in leads:
			if typeof(r) != TYPE_DICTIONARY:
				continue
			var lead_id = str(r.get("lead_id", ""))
			var hint = r.get("hint_tokens", [])
			print(PREFIX + "LEAD:%s|HINT_TOKENS:%s" % [lead_id, _join_tokens(hint)])

	if _bridge != null and _bridge.has_method("RequestShutdownV0"):
		_bridge.call("RequestShutdownV0")
	else:
		quit(0)

func _join_tokens(tokens) -> String:
	if typeof(tokens) != TYPE_ARRAY:
		return ""
	var parts := PackedStringArray()
	for t in tokens:
		parts.append(str(t))
	return ",".join(parts)
