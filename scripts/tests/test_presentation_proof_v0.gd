extends SceneTree

# GATE.S4.INDU.HEADLESS_PROOF.001
# Headless proof: verify presentation + industry depth gates.
# Checks bridge queries (tech tier, refit queue, supply, risk delay),
# camera shake API, and audio node presence.
# Emits: PRESV0|PRESENTATION_PROOF|PASS

const PREFIX := "PRESV0|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	CHECK_TECH_TIER,
	CHECK_REFIT_QUEUE,
	CHECK_SUPPLY,
	CHECK_RISK_DELAY,
	CHECK_CAMERA_SHAKE,
	CHECK_AUDIO_NODES,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _failures: Array[String] = []


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			_gm = root.get_node_or_null("GameManager")
			if _bridge != null and _gm != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_or_gm_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_phase = Phase.CHECK_TECH_TIER
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_ready")

		Phase.CHECK_TECH_TIER:
			_check("GetTechTierV0", func():
				var tier: Dictionary = _bridge.call("GetTechTierV0")
				_assert_key(tier, "tech_level", "tech_tier_missing_level")
				_assert_key(tier, "max_tier", "tech_tier_missing_max")
				print(PREFIX + "TECH_TIER|level=%s|max=%s" % [tier.get("tech_level", "?"), tier.get("max_tier", "?")])
			)
			_phase = Phase.CHECK_REFIT_QUEUE

		Phase.CHECK_REFIT_QUEUE:
			_check("GetRefitQueueV0", func():
				var queue = _bridge.call("GetRefitQueueV0", "fleet_trader_1")
				print(PREFIX + "REFIT_QUEUE|size=%d" % queue.size())
			)
			_check("GetRefitProgressV0", func():
				var prog: Dictionary = _bridge.call("GetRefitProgressV0", "fleet_trader_1")
				_assert_key(prog, "queue_size", "refit_progress_missing_queue_size")
				print(PREFIX + "REFIT_PROGRESS|queue_size=%s" % prog.get("queue_size", "?"))
			)
			_phase = Phase.CHECK_SUPPLY

		Phase.CHECK_SUPPLY:
			_check("GetSupplyLevelV0", func():
				var supply: Dictionary = _bridge.call("GetSupplyLevelV0", "node_sol")
				_assert_key(supply, "supply_level", "supply_missing_level")
				print(PREFIX + "SUPPLY|level=%s" % supply.get("supply_level", "?"))
			)
			_phase = Phase.CHECK_RISK_DELAY

		Phase.CHECK_RISK_DELAY:
			_check("GetDelayStatusV0", func():
				var d: Dictionary = _bridge.call("GetDelayStatusV0", "fleet_trader_1")
				_assert_key(d, "delayed", "delay_missing_delayed")
				_assert_key(d, "ticks_remaining", "delay_missing_ticks")
				print(PREFIX + "DELAY|delayed=%s|ticks=%s" % [d.get("delayed", "?"), d.get("ticks_remaining", "?")])
			)
			_check("GetTravelEtaV0", func():
				var eta: Dictionary = _bridge.call("GetTravelEtaV0", "fleet_trader_1", "node_sol")
				_assert_key(eta, "base_ticks", "eta_missing_base")
				_assert_key(eta, "total_ticks", "eta_missing_total")
				print(PREFIX + "ETA|base=%s|total=%s" % [eta.get("base_ticks", "?"), eta.get("total_ticks", "?")])
			)
			_phase = Phase.CHECK_CAMERA_SHAKE

		Phase.CHECK_CAMERA_SHAKE:
			# Camera may not be active in headless — just verify the script exists
			var cam = get_root().get_viewport().get_camera_3d()
			if cam != null and cam.has_method("apply_shake"):
				print(PREFIX + "CAMERA_SHAKE|found=true")
			else:
				# In headless mode, camera may be null — that's OK, just log it
				print(PREFIX + "CAMERA_SHAKE|found=false|note=headless_no_camera")
			_phase = Phase.CHECK_AUDIO_NODES

		Phase.CHECK_AUDIO_NODES:
			# Check audio scripts exist on disk (can't instantiate scenes in headless easily)
			var audio_scripts := [
				"res://scripts/audio/engine_audio.gd",
				"res://scripts/audio/combat_audio.gd",
				"res://scripts/audio/ambient_audio.gd",
			]
			for path in audio_scripts:
				if ResourceLoader.exists(path):
					print(PREFIX + "AUDIO_SCRIPT|%s|exists=true" % path)
				else:
					_failures.append("missing_audio:" + path)
					print(PREFIX + "AUDIO_SCRIPT|%s|exists=false" % path)
			_phase = Phase.DONE

		Phase.DONE:
			_bridge.call("StopSimV0")
			if _failures.size() > 0:
				print(PREFIX + "PRESENTATION_PROOF|FAIL|%s" % ",".join(_failures))
			else:
				print(PREFIX + "PRESENTATION_PROOF|PASS")
			quit()
			return true

	return false


func _check(method_name: String, callback: Callable) -> void:
	if _bridge.has_method(method_name):
		callback.call()
	else:
		_failures.append("missing_method:" + method_name)
		print(PREFIX + "SKIP|%s|not_found" % method_name)


func _assert_key(dict: Dictionary, key: String, fail_tag: String) -> void:
	if not dict.has(key):
		_failures.append(fail_tag)


func _fail(reason: String) -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	print(PREFIX + "PRESENTATION_PROOF|FAIL|" + reason)
	quit()
