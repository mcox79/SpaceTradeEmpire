extends SceneTree

# GATE.S1.HERO_SHIP_LOOP.STATION_DOCK_PROXIMITY.001
# GATE.S1.HERO_SHIP_LOOP.MARKET_UNDOCK_V0.001
# Verifies: active_station routes dock enter through game_manager.on_proximity_dock_entered_v0()
# Verifies: hero_trade_menu.open_market_v0() populates panel rows; undock_v0() transitions state.

const PREFIX := "SDV0|"
const MAX_POLLS := 400

enum Phase { WAIT_BRIDGE, WAIT_READY, RUN, DONE }

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null


func _initialize() -> void:
	print(PREFIX + "test_station_dock_v0")


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
					print(PREFIX + "ERROR: SimBridge or GameManager not found")
					_quit()

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_phase = Phase.RUN
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					print(PREFIX + "ERROR: bridge ready timeout")
					_quit()

		Phase.RUN:
			_phase = Phase.DONE
			_run()

		Phase.DONE:
			pass

	return false


func _run() -> void:
	# --- DOCK PROXIMITY ---
	var mock_sta := Node.new()
	mock_sta.add_to_group("Station")
	mock_sta.set_meta("dock_target_id", "star_0")
	root.add_child(mock_sta)

	_gm.call("on_proximity_dock_entered_v0", mock_sta)

	var kind := str(_gm.get("dock_target_kind_token"))
	var target_id := str(_gm.get("dock_target_id"))
	var state_name := str(_gm.call("get_player_ship_state_name_v0"))

	if state_name != "DOCKED":
		print(PREFIX + "FAIL|dock_state=%s expected=DOCKED" % state_name)
		_quit()
		return

	if kind != "STATION":
		print(PREFIX + "FAIL|dock_kind=%s expected=STATION" % kind)
		_quit()
		return

	print(PREFIX + "DOCK|PASS|kind=STATION|target=%s" % target_id)

	# --- MARKET VIEW ---
	var view = _bridge.call("GetPlayerMarketViewV0", target_id)
	if typeof(view) != TYPE_ARRAY or view.size() == 0:
		print(PREFIX + "FAIL|market_view_empty for target=%s" % target_id)
		_quit()
		return

	print(PREFIX + "MARKET|PASS|rows=%d" % view.size())

	# --- HERO TRADE MENU PANEL ROWS ---
	var htm_script = preload("res://scripts/ui/hero_trade_menu.gd")
	var htm = htm_script.new()
	root.add_child(htm)
	htm.call("open_market_v0", "star_0")
	var row_count := int(htm.call("get_panel_row_count_v0"))

	if row_count <= 0:
		print(PREFIX + "FAIL|panel_row_count=%d expected>0" % row_count)
		_quit()
		return

	print(PREFIX + "PANEL|PASS|rows=%d" % row_count)

	# --- UNDOCK ---
	_gm.call("undock_v0")
	var state_after := str(_gm.call("get_player_ship_state_name_v0"))

	if state_after != "IN_FLIGHT":
		print(PREFIX + "FAIL|undock_state=%s expected=IN_FLIGHT" % state_after)
		_quit()
		return

	print(PREFIX + "UNDOCK|ok")
	print(PREFIX + "ALL|PASS")
	_quit()


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
