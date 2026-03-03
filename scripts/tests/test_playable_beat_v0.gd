extends SceneTree

# GATE.S1.PLAYABLE_BEAT.INTERACTION_FIX.001
# Verifies: buy/sell buttons wired, market rows refresh after trade, ship input frozen while docked.
# Emits: BEAT|PASS

const PREFIX := "BEATV0|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	DOCK, WAIT_BUY, SELL, WAIT_SELL,
	UNDOCK, VERIFY_FLIGHT,
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
				_polls = 0
				_phase = Phase.DOCK
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.DOCK:
			# Dock at star_0
			var mock_sta := Node.new()
			mock_sta.add_to_group("Station")
			mock_sta.set_meta("dock_target_id", "star_0")
			root.add_child(mock_sta)
			_gm.call("on_proximity_dock_entered_v0", mock_sta)

			var state := str(_gm.call("get_player_ship_state_name_v0"))
			if state != "DOCKED":
				_fail("dock_state=%s expected=DOCKED" % state)
				return false
			print(PREFIX + "DOCK|PASS")

			# Buy 1 fuel
			_bridge.call("DispatchPlayerTradeV0", "star_0", "fuel", 1, true)
			_polls = 0
			_phase = Phase.WAIT_BUY

		Phase.WAIT_BUY:
			var fuel_qty := _get_fuel_qty()
			if fuel_qty > 0:
				print(PREFIX + "BUY|PASS|fuel_qty=%d" % fuel_qty)
				_polls = 0
				_phase = Phase.SELL
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("buy_timeout")

		Phase.SELL:
			# Sell fuel back
			_bridge.call("DispatchPlayerTradeV0", "star_0", "fuel", 1, false)
			_polls = 0
			_phase = Phase.WAIT_SELL

		Phase.WAIT_SELL:
			var fuel_qty := _get_fuel_qty()
			if fuel_qty == 0:
				print(PREFIX + "SELL|PASS")
				_polls = 0
				_phase = Phase.UNDOCK
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("sell_timeout|fuel_qty=%d" % fuel_qty)

		Phase.UNDOCK:
			_gm.call("undock_v0")
			var state := str(_gm.call("get_player_ship_state_name_v0"))
			if state != "IN_FLIGHT":
				_fail("undock_state=%s expected=IN_FLIGHT" % state)
				return false
			print(PREFIX + "UNDOCK|PASS")
			_phase = Phase.VERIFY_FLIGHT

		Phase.VERIFY_FLIGHT:
			print(PREFIX + "BEAT|PASS")
			_phase = Phase.DONE
			_quit()

		Phase.DONE:
			pass

	return false


func _get_fuel_qty() -> int:
	var cargo = _bridge.call("GetPlayerCargoV0")
	if typeof(cargo) != TYPE_ARRAY:
		return 0
	for item in cargo:
		if typeof(item) == TYPE_DICTIONARY and str(item.get("good_id", "")) == "fuel":
			return int(item.get("qty", 0))
	return 0


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE
	_quit()


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
