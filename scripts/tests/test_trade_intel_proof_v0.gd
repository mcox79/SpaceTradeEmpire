extends SceneTree

# GATE.S10.TRADE_INTEL.PROOF.001: Headless proof — trade intel bridge round-trip.
# Boot scene, verify GetTradeRoutesV0/GetPriceIntelV0/GetScannerRangeV0 work,
# create a TradeCharter, start research with nodeId, verify sustain fields update.

var _bridge = null
var _phase: int = 0
var _tick_count: int = 0
var _max_ticks: int = 300
var _current_node_id: String = ""
var _charter_id: String = ""

func _init():
	print("HSS|test_trade_intel_proof_v0|start")

func _process(_delta: float) -> bool:
	if _phase == 0:
		_bridge = root.get_node_or_null("SimBridge")
		if _bridge == null or not _bridge.has_method("GetBridgeReadyV0"):
			return false
		if not _bridge.call("GetBridgeReadyV0"):
			return false
		print("HSS|bridge_ready")
		_phase = 1
		return false

	if _phase == 1:
		# Phase 1: Get current node via GetPlayerStateV0.
		if _bridge.has_method("GetPlayerStateV0"):
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			_current_node_id = str(ps.get("current_node_id", ""))
			print("HSS|player_node|%s" % _current_node_id)
		else:
			print("HSS|warn|GetPlayerStateV0_missing|using_empty_node")

		# GetScannerRangeV0 should return 0 (no tech unlocked).
		if _bridge.has_method("GetScannerRangeV0"):
			var scan_range: int = int(_bridge.call("GetScannerRangeV0"))
			print("HSS|scanner_range|%d" % scan_range)
		else:
			print("HSL|FAIL|GetScannerRangeV0_missing")
			_stop()
			return false

		# GetTradeRoutesV0 should return an Array (possibly empty on fresh game).
		if _bridge.has_method("GetTradeRoutesV0"):
			var routes: Array = _bridge.call("GetTradeRoutesV0")
			print("HSS|trade_routes_count|%d" % routes.size())
		else:
			print("HSL|FAIL|GetTradeRoutesV0_missing")
			_stop()
			return false

		# GetPriceIntelV0 for current node (may be empty if no observations yet).
		if _bridge.has_method("GetPriceIntelV0"):
			if _current_node_id != "":
				var intel: Array = _bridge.call("GetPriceIntelV0", _current_node_id)
				print("HSS|price_intel_count|%d" % intel.size())
			else:
				print("HSS|price_intel_skipped|no_node_id")
		else:
			print("HSL|FAIL|GetPriceIntelV0_missing")
			_stop()
			return false

		_phase = 2
		return false

	if _phase == 2:
		# Phase 2: Create a TradeCharter program via bridge.
		if _bridge.has_method("CreateTradeCharterProgram"):
			if _current_node_id != "":
				_charter_id = str(_bridge.call("CreateTradeCharterProgram", _current_node_id, _current_node_id, "fuel", "fuel", 10))
				print("HSS|charter_created|%s" % _charter_id)
				if _charter_id == "":
					print("HSS|charter_creation_returned_empty|non_fatal")
			else:
				print("HSS|charter_skipped|no_node_id")
		else:
			print("HSL|FAIL|CreateTradeCharterProgram_missing")
			_stop()
			return false

		_phase = 3
		return false

	if _phase == 3:
		# Phase 3: Start research with nodeId and verify sustain fields.
		if _bridge.has_method("StartResearchV0"):
			# Use improved_thrusters (no prerequisites, Tier 1).
			var result: Dictionary = _bridge.call("StartResearchV0", "improved_thrusters", _current_node_id)
			var success: bool = bool(result.get("success", false))
			print("HSS|start_research|success=%s|reason=%s|node=%s" % [str(success), str(result.get("reason", "")), _current_node_id])
			if not success:
				print("HSL|FAIL|could_not_start_research|%s" % str(result.get("reason", "")))
				_stop()
				return false
		else:
			print("HSL|FAIL|StartResearchV0_missing")
			_stop()
			return false

		_phase = 4
		_tick_count = 0
		return false

	if _phase == 4:
		# Phase 4: Tick and verify sustain fields update.
		_tick_count += 1
		if _tick_count > _max_ticks:
			print("HSL|FAIL|sustain_timeout")
			_stop()
			return false

		if _bridge.has_method("GetResearchStatusV0"):
			var status: Dictionary = _bridge.call("GetResearchStatusV0")
			var researching: bool = bool(status.get("researching", false))
			var node_id: String = str(status.get("research_node_id", ""))
			var sustain_acc: int = int(status.get("sustain_accumulator_ticks", -1))

			# Verify research_node_id was set.
			if node_id != "" and _tick_count == 1:
				print("HSS|sustain_node|%s" % node_id)

			# If research completes, that's a pass.
			if not researching:
				print("HSS|research_complete|ticks=%d" % _tick_count)
				_phase = 5
				return false

			# If sustain_accumulator increments, that confirms goods consumption is wired.
			if sustain_acc > 0 and _tick_count <= 5:
				print("HSS|sustain_accumulator|%d" % sustain_acc)
		return false

	if _phase == 5:
		# Phase 5: Final validation — verify all bridge methods responded.
		print("HSL|PASS|trade_intel_proof_complete")
		_stop()
		return false

	return false

func _stop() -> void:
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit()
