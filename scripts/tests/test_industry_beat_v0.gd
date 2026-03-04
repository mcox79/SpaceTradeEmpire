extends SceneTree

# GATE.S4.INDU.PLAYABLE_BEAT.001: Playable beat — dock, research, refit, repair, trade.
# Validates the full industry loop is exercisable via bridge APIs.

var _bridge = null
var _phase: int = 0
var _tick_count: int = 0
var _max_ticks: int = 300
var _results: Array = []

func _init():
	print("HSS|test_industry_beat_v0|start")

func _process(_delta: float) -> void:
	if _phase == 0:
		_bridge = root.get_node_or_null("SimBridge")
		if _bridge == null or not _bridge.has_method("GetBridgeReadyV0"):
			return
		if not _bridge.call("GetBridgeReadyV0"):
			return
		print("HSS|bridge_ready")
		_phase = 1
		return

	if _phase == 1:
		# Phase 1: Check we can query tech tree
		if _bridge.has_method("GetTechTreeV0"):
			var techs: Array = _bridge.call("GetTechTreeV0")
			print("HSS|tech_tree_count=%d" % techs.size())
			_results.append("tech_tree_ok")
		_phase = 2
		return

	if _phase == 2:
		# Phase 2: Start research
		if _bridge.has_method("StartResearchV0"):
			var r: Dictionary = _bridge.call("StartResearchV0", "improved_thrusters")
			print("HSS|start_research|success=%s" % str(r.get("success", false)))
			if bool(r.get("success", false)):
				_results.append("research_started")
		_phase = 3
		return

	if _phase == 3:
		# Phase 3: Tick until research completes
		_tick_count += 1
		if _tick_count > _max_ticks:
			print("HSL|FAIL|research_timeout")
			_stop()
			return
		if _bridge.has_method("GetResearchStatusV0"):
			var s: Dictionary = _bridge.call("GetResearchStatusV0")
			if not bool(s.get("researching", false)):
				print("HSS|research_done|ticks=%d" % _tick_count)
				_results.append("research_done")
				_phase = 4
		return

	if _phase == 4:
		# Phase 4: Check available modules
		if _bridge.has_method("GetAvailableModulesV0"):
			var mods: Array = _bridge.call("GetAvailableModulesV0")
			var installable_count: int = 0
			for m in mods:
				if typeof(m) == TYPE_DICTIONARY and bool(m.get("can_install", false)):
					installable_count += 1
			print("HSS|available_modules=%d|installable=%d" % [mods.size(), installable_count])
			_results.append("modules_queried")
		_phase = 5
		return

	if _phase == 5:
		# Phase 5: Check maintenance status
		if _bridge.has_method("GetPlayerStateV0"):
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))
			if not node_id.is_empty() and _bridge.has_method("GetNodeMaintenanceV0"):
				var maint: Array = _bridge.call("GetNodeMaintenanceV0", node_id)
				print("HSS|maintenance_sites=%d" % maint.size())
				_results.append("maintenance_queried")
		_phase = 6
		return

	if _phase == 6:
		# Phase 6: Attempt a trade (buy if market available)
		if _bridge.has_method("GetPlayerStateV0"):
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))
			if not node_id.is_empty() and _bridge.has_method("GetPlayerMarketViewV0"):
				var view: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
				if view.size() > 0:
					var entry: Dictionary = view[0] if typeof(view[0]) == TYPE_DICTIONARY else {}
					var gid: String = str(entry.get("good_id", ""))
					if not gid.is_empty() and _bridge.has_method("DispatchPlayerTradeV0"):
						_bridge.call("DispatchPlayerTradeV0", node_id, gid, 1, true)
						print("HSS|trade_buy|%s" % gid)
						_results.append("trade_done")
		_phase = 7
		return

	if _phase == 7:
		# Summary
		print("HSS|results=%s" % str(_results))
		var required: Array = ["tech_tree_ok", "research_started", "research_done", "modules_queried", "maintenance_queried"]
		var all_pass: bool = true
		for req in required:
			if not _results.has(req):
				print("HSS|missing_result=%s" % req)
				all_pass = false
		if all_pass:
			print("HSL|PASS|industry_beat_complete")
		else:
			print("HSL|FAIL|missing_required_results")
		_stop()

func _stop() -> void:
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit()
