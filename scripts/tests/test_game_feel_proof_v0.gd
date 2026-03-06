extends SceneTree

# GATE.S11.GAME_FEEL.HEADLESS_PROOF.001
# Headless proof: verify game feel features from Tranche 10.
# Checks: toast_manager autoload, tech tree bridge, node detail bridge,
# mission list (>=3 missions), combat events bridge, NPC routes bridge,
# price history bridge, fleet breakdown bridge, keybinds help script.
# Emits: GFV0|GAME_FEEL_PROOF|PASS

const PREFIX := "GFV0|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	CHECK_TOAST_MANAGER,
	CHECK_TECH_TREE,
	CHECK_NODE_DETAIL,
	CHECK_MISSION_LIST,
	CHECK_COMBAT_EVENTS,
	CHECK_NPC_ROUTES,
	CHECK_PRICE_HISTORY,
	CHECK_FLEET_BREAKDOWN,
	CHECK_UI_SCRIPTS,
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
				_phase = Phase.CHECK_TOAST_MANAGER
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_ready")

		Phase.CHECK_TOAST_MANAGER:
			var toast = root.get_node_or_null("ToastManager")
			if toast != null:
				print(PREFIX + "TOAST_MANAGER|found=true")
				if toast.has_method("show_toast"):
					print(PREFIX + "TOAST_MANAGER|has_show_toast=true")
				else:
					_failures.append("toast_missing_show_toast")
					print(PREFIX + "TOAST_MANAGER|has_show_toast=false")
			else:
				_failures.append("toast_manager_not_found")
				print(PREFIX + "TOAST_MANAGER|found=false")
			_phase = Phase.CHECK_TECH_TREE

		Phase.CHECK_TECH_TREE:
			_check("GetTechTreeV0", func():
				var tree: Array = _bridge.call("GetTechTreeV0")
				print(PREFIX + "TECH_TREE|count=%d" % tree.size())
				if tree.size() == 0:
					_failures.append("tech_tree_empty")
				else:
					var first: Dictionary = tree[0]
					_assert_key(first, "tech_id", "tech_tree_missing_id")
					_assert_key(first, "name", "tech_tree_missing_name")
					_assert_key(first, "status", "tech_tree_missing_status")
					print(PREFIX + "TECH_TREE|first_id=%s|status=%s" % [first.get("tech_id", "?"), first.get("status", "?")])
			)
			_phase = Phase.CHECK_NODE_DETAIL

		Phase.CHECK_NODE_DETAIL:
			# Use player's starting node (star_0) — generated node IDs are star_N
			_check("GetNodeDetailV0", func():
				var detail: Dictionary = _bridge.call("GetNodeDetailV0", "star_0")
				_assert_key(detail, "node_name", "node_detail_missing_name")
				_assert_key(detail, "world_class_id", "node_detail_missing_class")
				print(PREFIX + "NODE_DETAIL|name=%s|class=%s" % [detail.get("node_name", "?"), detail.get("world_class_id", "?")])
			)
			_phase = Phase.CHECK_MISSION_LIST

		Phase.CHECK_MISSION_LIST:
			_check("GetMissionListV0", func():
				var missions: Array = _bridge.call("GetMissionListV0")
				print(PREFIX + "MISSION_LIST|count=%d" % missions.size())
				if missions.size() < 3:
					_failures.append("mission_list_under_3:%d" % missions.size())
			)
			_phase = Phase.CHECK_COMBAT_EVENTS

		Phase.CHECK_COMBAT_EVENTS:
			_check("GetRecentCombatEventsV0", func():
				var events: Array = _bridge.call("GetRecentCombatEventsV0")
				print(PREFIX + "COMBAT_EVENTS|count=%d" % events.size())
			)
			_phase = Phase.CHECK_NPC_ROUTES

		Phase.CHECK_NPC_ROUTES:
			_check("GetNpcPatrolRoutesV0", func():
				var routes: Array = _bridge.call("GetNpcPatrolRoutesV0")
				print(PREFIX + "NPC_PATROL_ROUTES|count=%d" % routes.size())
			)
			_phase = Phase.CHECK_PRICE_HISTORY

		Phase.CHECK_PRICE_HISTORY:
			_check("GetPriceHistoryV0", func():
				var history: Array = _bridge.call("GetPriceHistoryV0", "star_0", "fuel")
				print(PREFIX + "PRICE_HISTORY|count=%d" % history.size())
			)
			_phase = Phase.CHECK_FLEET_BREAKDOWN

		Phase.CHECK_FLEET_BREAKDOWN:
			_check("GetNodeFleetBreakdownV0", func():
				var bd: Dictionary = _bridge.call("GetNodeFleetBreakdownV0", "star_0")
				_assert_key(bd, "traders", "fleet_bd_missing_traders")
				_assert_key(bd, "summary", "fleet_bd_missing_summary")
				print(PREFIX + "FLEET_BREAKDOWN|summary=%s" % bd.get("summary", "?"))
			)
			_phase = Phase.CHECK_UI_SCRIPTS

		Phase.CHECK_UI_SCRIPTS:
			var ui_scripts := [
				"res://scripts/ui/toast_manager.gd",
				"res://scripts/ui/keybinds_help.gd",
				"res://scripts/ui/combat_log_panel.gd",
				"res://scripts/ui/node_detail_popup.gd",
			]
			for path in ui_scripts:
				if ResourceLoader.exists(path):
					print(PREFIX + "UI_SCRIPT|%s|exists=true" % path)
				else:
					_failures.append("missing_ui:" + path)
					print(PREFIX + "UI_SCRIPT|%s|exists=false" % path)
			_phase = Phase.DONE

		Phase.DONE:
			_bridge.call("StopSimV0")
			if _failures.size() > 0:
				print(PREFIX + "GAME_FEEL_PROOF|FAIL|%s" % ",".join(_failures))
			else:
				print(PREFIX + "GAME_FEEL_PROOF|PASS")
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
	print(PREFIX + "GAME_FEEL_PROOF|FAIL|" + reason)
	quit()
