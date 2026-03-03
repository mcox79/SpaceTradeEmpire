extends SceneTree

# GATE.S1.HERO_SHIP_LOOP.STATION_LOOP_V1.001
# Full playable beat: dock at star_0, buy fuel, undock, enter lane, arrive at neighbor,
# dock at neighbor station, sell fuel. Closes EPIC.S1.HERO_SHIP_LOOP.V0.
# Uses fuel (seeded at 500 at all markets) — food is production-only and not seeded at genesis.
# Emits: LOOP_V1|PASS

const PREFIX := "LPFV1|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	DOCK_1, WAIT_BUY,
	UNDOCK_AND_LANE, WAIT_ARRIVE,
	DOCK_2, WAIT_SELL,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _gm = null
var _neighbor_id := ""


func _initialize() -> void:
	print(PREFIX + "LOOP_V1_START")


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
				_phase = Phase.DOCK_1
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.DOCK_1:
			# Find a neighbor of star_0 from galaxy topology
			var snap = _bridge.call("GetGalaxySnapshotV0")
			var edges = snap.get("lane_edges", [])
			for e in edges:
				if typeof(e) != TYPE_DICTIONARY:
					continue
				var from_id := str(e.get("from_id", ""))
				var to_id := str(e.get("to_id", ""))
				if from_id == "star_0" and not to_id.is_empty():
					_neighbor_id = to_id
					break
				elif to_id == "star_0" and not from_id.is_empty():
					_neighbor_id = from_id
					break

			if _neighbor_id.is_empty():
				_fail("no_neighbor_found_for_star_0")
				return false

			# Dock at star_0
			var mock_sta1 := Node.new()
			mock_sta1.add_to_group("Station")
			mock_sta1.set_meta("dock_target_id", "star_0")
			root.add_child(mock_sta1)
			_gm.call("on_proximity_dock_entered_v0", mock_sta1)

			var state_name := str(_gm.call("get_player_ship_state_name_v0"))
			if state_name != "DOCKED":
				_fail("dock1_state=%s expected=DOCKED" % state_name)
				return false

			print(PREFIX + "DOCK1|PASS|target=star_0")

			# Buy 1 fuel (seeded at 500 in all markets at genesis)
			_bridge.call("DispatchPlayerTradeV0", "star_0", "fuel", 1, true)
			_polls = 0
			_phase = Phase.WAIT_BUY

		Phase.WAIT_BUY:
			var fuel_qty := _get_fuel_qty()
			if fuel_qty > 0:
				print(PREFIX + "BUY|PASS|fuel_qty=%d" % fuel_qty)
				_polls = 0
				_phase = Phase.UNDOCK_AND_LANE
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("buy_fuel_timeout")

		Phase.UNDOCK_AND_LANE:
			_gm.call("undock_v0")
			var state_name2 := str(_gm.call("get_player_ship_state_name_v0"))
			if state_name2 != "IN_FLIGHT":
				_fail("undock_state=%s expected=IN_FLIGHT" % state_name2)
				return false
			print(PREFIX + "UNDOCK|ok")

			_gm.call("on_lane_gate_proximity_entered_v0", _neighbor_id)
			var state_lane := str(_gm.call("get_player_ship_state_name_v0"))
			if state_lane != "IN_LANE_TRANSIT":
				_fail("lane_enter_state=%s expected=IN_LANE_TRANSIT" % state_lane)
				return false
			print(PREFIX + "LANE_ENTER|PASS|neighbor=%s" % _neighbor_id)
			_polls = 0
			_phase = Phase.WAIT_ARRIVE

		Phase.WAIT_ARRIVE:
			# Wait a few sim ticks then simulate arrival
			_polls += 1
			if _polls >= 5:
				_gm.call("on_lane_arrival_v0", _neighbor_id)
				var state_flight := str(_gm.call("get_player_ship_state_name_v0"))
				if state_flight != "IN_FLIGHT":
					_fail("arrive_state=%s expected=IN_FLIGHT" % state_flight)
					return false
				print(PREFIX + "LANE_ARRIVE|PASS|at=%s" % _neighbor_id)
				_polls = 0
				_phase = Phase.DOCK_2

		Phase.DOCK_2:
			var mock_sta2 := Node.new()
			mock_sta2.add_to_group("Station")
			mock_sta2.set_meta("dock_target_id", _neighbor_id)
			root.add_child(mock_sta2)
			_gm.call("on_proximity_dock_entered_v0", mock_sta2)

			var state_d2 := str(_gm.call("get_player_ship_state_name_v0"))
			if state_d2 != "DOCKED":
				_fail("dock2_state=%s expected=DOCKED" % state_d2)
				return false

			print(PREFIX + "DOCK2|PASS|target=%s" % _neighbor_id)

			# Sell fuel at neighbor station
			_bridge.call("DispatchPlayerTradeV0", _neighbor_id, "fuel", 1, false)
			_polls = 0
			_phase = Phase.WAIT_SELL

		Phase.WAIT_SELL:
			var fuel_qty2 := _get_fuel_qty()
			if fuel_qty2 == 0:
				print(PREFIX + "SELL|PASS")
				print(PREFIX + "LOOP_V1|PASS")
				_phase = Phase.DONE
				_quit()
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("sell_fuel_timeout|fuel_qty=%d" % fuel_qty2)

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
