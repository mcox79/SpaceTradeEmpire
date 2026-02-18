# FILE: scripts/tests/phase3_explain_capstone.gd
extends SceneTree

var _bridge: Node = null
var _market_id := ""
var _t0 := ""
var _t1 := ""
var _t2 := ""

func _initialize() -> void:
	print("--- PHASE 3 EXPLAINABILITY CAPSTONE ---")
	call_deferred("_run")

func _fail(msg: String) -> void:
	print("FAIL: ", msg)
	if _bridge != null:
		_bridge.queue_free()
	quit(1)

func _extract_hash64(transcript: String) -> String:
	for l in transcript.split("\n"):
		if l.begins_with("hash64="):
			return l.strip_edges()
	return ""

func _pick_first_good_id(transcript: String) -> String:
	var in_inv := false
	for l in transcript.split("\n"):
		if l == "market_inventory:":
			in_inv = true
			continue
		if in_inv:
			if l.begins_with("  "):
				var s := l.strip_edges()
				var eq := s.find("=")
				if eq > 0:
					return s.substr(0, eq)
			else:
				break
	return ""

func _wait_epoch(getter_name: String, start_val: int, max_frames: int) -> bool:
	for _i in range(max_frames):
		var now := int(_bridge.call(getter_name))
		if now > start_val:
			return true
		await process_frame
	return false

func _bridge_call(pascal: String, snake: String, args: Array) -> Variant:
	if _bridge == null:
		_fail("SimBridge is null")
		return null
	if _bridge.has_method(pascal):
		return _bridge.callv(pascal, args)
	if _bridge.has_method(snake):
		return _bridge.callv(snake, args)
	_fail("SimBridge missing method: " + pascal + " (also tried: " + snake + ")")
	return null

func _run() -> void:
	# Acquire SimBridge. If project already provides one (autoload/scene), reuse it to avoid double threads.
	_bridge = get_root().get_node_or_null("SimBridge")
	if _bridge == null:
		var bridge_script = load("res://scripts/bridge/SimBridge.cs")
		if bridge_script == null:
			_fail("Could not load res://scripts/bridge/SimBridge.cs")
			return

		_bridge = bridge_script.new()
		if _bridge == null:
			_fail("Could not instantiate SimBridge")
			return

		_bridge.name = "SimBridge"
		get_root().add_child(_bridge)

	# Wait until bridge is ready (player snapshot/location available), not a fixed frame count.
	var ps = null
	var ready := false
	for _i in range(300):
		await process_frame
		ps = _bridge_call("GetPlayerSnapshot", "get_player_snapshot", [])
		if typeof(ps) == TYPE_DICTIONARY and str(ps.get("location", "")) != "":
			ready = true
			break

	if not ready:
		_fail("SimBridge not ready (player snapshot/location not available)")
		return

	_market_id = str(ps.get("location", ""))
	if _market_id == "":
		_fail("Player location missing; cannot choose market_id")
		return

	_t0 = str(_bridge_call("GetMarketExplainTranscript", "get_market_explain_transcript", [_market_id]))
	if _t0 == "":
		_fail("GetMarketExplainTranscript returned empty (pre)")
		return

	print("--- EXPLAIN TRANSCRIPT (PRE) ---")
	print(_t0)

	var h0 := _extract_hash64(_t0)
	if h0 == "":
		_fail("Missing hash64 line (pre)")
		return

	# Create representative failure: absurd quantity should fail credits constraint deterministically.
	var good_id := _pick_first_good_id(_t0)
	if good_id == "":
		_fail("Could not pick a good_id from market_inventory")
		return

	var pid := str(_bridge_call("CreateAutoBuyProgram", "create_auto_buy_program", [_market_id, good_id, 1000000, 1]))
	if pid == "":
		_fail("CreateAutoBuyProgram returned empty id")
		return

	_bridge_call("StartProgram", "start_program", [pid])

	for _i in range(10):
		await process_frame

	_t1 = str(_bridge_call("GetMarketExplainTranscript", "get_market_explain_transcript", [_market_id]))
	if _t1 == "":
		_fail("Transcript empty after program creation")
		return

	var h1 := _extract_hash64(_t1)
	if h1 == "":
		_fail("Missing hash64 line (mid)")
		return

	if _t1.find("constraints:") < 0:
		_fail("Transcript missing constraints line")
		return
	if _t1.find("credits_now=False") < 0:
		_fail("Representative failure not observed: expected credits_now=False")
		return

	var save0 := int(_bridge_call("GetSaveEpoch", "get_save_epoch", []))
	var load0 := int(_bridge_call("GetLoadEpoch", "get_load_epoch", []))

	_bridge_call("RequestSave", "request_save", [])
	if not await _wait_epoch("GetSaveEpoch", save0, 600) and not await _wait_epoch("get_save_epoch", save0, 600):
		_fail("Timed out waiting for save completion epoch")
		return

	_bridge_call("RequestLoad", "request_load", [])
	if not await _wait_epoch("GetLoadEpoch", load0, 600) and not await _wait_epoch("get_load_epoch", load0, 600):
		_fail("Timed out waiting for load completion epoch")
		return

	_t2 = str(_bridge_call("GetMarketExplainTranscript", "get_market_explain_transcript", [_market_id]))
	if _t2 == "":
		_fail("Transcript empty after load")
		return

	print("--- EXPLAIN TRANSCRIPT (POST) ---")
	print(_t2)

	var h2 := _extract_hash64(_t2)
	if h2 == "":
		_fail("Missing hash64 line (post)")
		return

	if h1 != h2:
		_fail("Explanation state not preserved across save/load (hash64 mismatch)")
		return

	print("PASS: deterministic explain transcript preserved across save/load: ", h2)

	if _bridge != null:
		_bridge.queue_free()
	for _i in range(5):
		await process_frame

	quit(0)
