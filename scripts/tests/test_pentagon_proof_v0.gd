extends SceneTree

# GATE.S8.PENTAGON.HEADLESS.001
# Headless proof: boot -> verify pentagon state bridge methods exist,
# verify revelation text delivery, verify cascade effects query.
# Emits: PNT1|PENTAGON_PROOF|PASS

const PREFIX := "PNT1|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	VERIFY_PENTAGON_STATE,
	VERIFY_CASCADE_EFFECTS,
	VERIFY_REVELATION_TEXT,
	VERIFY_STORY_PROGRESS,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			_gm = root.get_node_or_null("GameManager")
			if _bridge != null and _gm != null:
				print(PREFIX + "BRIDGE_FOUND")
				_phase = Phase.WAIT_READY
			elif _polls > MAX_POLLS:
				_fail("bridge_timeout")
				return false
			_polls += 1

		Phase.WAIT_READY:
			# Wait for sim to be ready (tick > 0).
			if _bridge.has_method("GetRevelationStateV0"):
				var state: Dictionary = _bridge.call("GetRevelationStateV0")
				if state.size() > 0:
					print(PREFIX + "SIM_READY")
					_phase = Phase.VERIFY_PENTAGON_STATE
					return false
			if _polls > MAX_POLLS:
				_fail("sim_ready_timeout")
				return false
			_polls += 1

		Phase.VERIFY_PENTAGON_STATE:
			if not _bridge.has_method("GetPentagonStateV0"):
				_fail("GetPentagonStateV0_missing")
				return false
			var ps: Dictionary = _bridge.call("GetPentagonStateV0")
			print(PREFIX + "PENTAGON_STATE|flags=%s|cascade=%s|r3=%s" % [
				str(ps.get("pentagon_trade_flags", "?")),
				str(ps.get("cascade_active", "?")),
				str(ps.get("has_r3", "?")),
			])
			# At game start, no trades — all flags should be false.
			var all_traded: bool = ps.get("all_traded", true)
			if all_traded:
				_fail("unexpected_all_traded_at_start")
				return false
			print(PREFIX + "PENTAGON_STATE|PASS")
			_phase = Phase.VERIFY_CASCADE_EFFECTS

		Phase.VERIFY_CASCADE_EFFECTS:
			if not _bridge.has_method("GetCascadeEffectsV0"):
				_fail("GetCascadeEffectsV0_missing")
				return false
			var ce: Dictionary = _bridge.call("GetCascadeEffectsV0")
			print(PREFIX + "CASCADE_EFFECTS|active=%s|gdp_bps=%s|nodes=%s" % [
				str(ce.get("cascade_active", "?")),
				str(ce.get("gdp_impact_bps", "?")),
				str(ce.get("communion_nodes_affected", "?")),
			])
			# At start, cascade should not be active.
			if ce.get("cascade_active", true):
				_fail("unexpected_cascade_active_at_start")
				return false
			print(PREFIX + "CASCADE_EFFECTS|PASS")
			_phase = Phase.VERIFY_REVELATION_TEXT

		Phase.VERIFY_REVELATION_TEXT:
			if not _bridge.has_method("GetRevelationTextV0"):
				_fail("GetRevelationTextV0_missing")
				return false
			var r3_text: Dictionary = _bridge.call("GetRevelationTextV0", "R3")
			var title: String = str(r3_text.get("gold_toast_title", ""))
			print(PREFIX + "REVELATION_TEXT|R3_title=%s" % title)
			if title.is_empty():
				_fail("R3_text_empty")
				return false
			if not "PATTERN" in title.to_upper():
				_fail("R3_text_missing_pattern")
				return false
			print(PREFIX + "REVELATION_TEXT|PASS")
			_phase = Phase.VERIFY_STORY_PROGRESS

		Phase.VERIFY_STORY_PROGRESS:
			if not _bridge.has_method("GetStoryProgressV0"):
				_fail("GetStoryProgressV0_missing")
				return false
			var sp: Dictionary = _bridge.call("GetStoryProgressV0")
			print(PREFIX + "STORY_PROGRESS|pentagon_flags=%s|all_traded=%s" % [
				str(sp.get("pentagon_trade_flags", "?")),
				str(sp.get("all_pentagon_traded", "?")),
			])
			print(PREFIX + "STORY_PROGRESS|PASS")
			_phase = Phase.DONE

		Phase.DONE:
			print(PREFIX + "PENTAGON_PROOF|PASS")
			_bridge.call("StopSimV0")
			quit(0)
			return false
	return false


func _fail(reason: String) -> void:
	print(PREFIX + "FAIL|" + reason)
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(1)
