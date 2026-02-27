extends SceneTree

# Deterministic transcript printer for GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.006
# Output lines are prefixed with "UUIR|" so tooling can filter out unrelated Godot logs.
# No timestamps. Ordering is inherited from SimBridge snapshot (UnlockId asc).

# State machine for non-blocking main-thread polling.
enum State { WAIT_BRIDGE, WAIT_READY, RUN, DONE }

var _state: int = State.WAIT_BRIDGE
var _poll_ticks: int = 0
const MAX_BRIDGE_POLLS: int = 100   # 100 frames ~= 100+ iterations at process rate
const MAX_READY_POLLS: int = 200    # 200 frames for seed+ready convergence

var _bridge = null

func _initialize() -> void:
	print("UUIR|UNLOCK_UI_READOUT_V0")
	# Do NOT block here. Let _process() do the polling.

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
					print("UUIR|ERROR: SimBridge not found")
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
				_run_output(seed_val)
				_state = State.DONE
			else:
				_poll_ticks += 1
				if _poll_ticks >= MAX_READY_POLLS:
					print("UUIR|SEED:%s" % str(seed_val))
					print("UUIR|ERROR: expected_seed_42")
					quit(1)
					_state = State.DONE

		State.RUN:
			pass  # run_output handles this synchronously; state goes to DONE immediately

		State.DONE:
			pass  # waiting for quit() to take effect

	return false  # false = keep running; SceneTree._process returns bool to stop iteration

func _run_output(seed_val: int) -> void:
	if not _bridge.has_method("GetUnlockListSnapshotV0"):
		print("UUIR|ERROR: GetUnlockListSnapshotV0 missing")
		quit(1)
		return

	print("UUIR|SEED:%s" % str(seed_val))
	print("UUIR|FIELDS:unlock_id|blocked_reason_code|effect_tokens|blocked_actions")

	var list = _bridge.call("GetUnlockListSnapshotV0")
	if typeof(list) != TYPE_ARRAY:
		print("UUIR|ERROR: snapshot not array")
		quit(1)
		return

	for e in list:
		if typeof(e) != TYPE_DICTIONARY:
			continue
		var unlock_id = str(e.get("unlock_id", ""))
		var blocked_reason_code = str(e.get("blocked_reason_code", ""))
		var effect_tokens = e.get("effect_tokens", [])
		var blocked_actions = e.get("blocked_actions", [])
		var effect_s = _join_tokens(effect_tokens)
		var actions_s = _join_tokens(blocked_actions)
		print("UUIR|%s|%s|%s|%s" % [unlock_id, blocked_reason_code, effect_s, actions_s])

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
