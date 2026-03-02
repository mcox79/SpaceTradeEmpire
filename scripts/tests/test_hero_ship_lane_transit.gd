extends SceneTree

# Headless proof for GATE.S1.HERO_SHIP_LOOP.LANE.001.
# Contract:
# - No timestamps / wall-clock values in output (SHA256-stable).
# - Prefix: HSL|
# - Exits nonzero on first failure.
# - State assertions via SimBridge.GetPlayerShipStateNameV0().
# - Tick advance assertion via SimBridge.GetSimTickV0() (boolean result only).

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("HSL|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("HSL|OK|" + msg)

func _initialize() -> void:
	print("HSL|BOOT")
	call_deferred("_run")

func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	for _i in range(BOOT_FRAMES):
		await physics_frame

	# --- Verify required nodes and methods ---
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("NO_SIMBRIDGE")
		return

	for method in ["GetPlayerShipStateNameV0", "GetSimTickV0", "DispatchTravelCommandV0", "GetFleetStateV0"]:
		if not bridge.has_method(method):
			_fail("MISSING_" + method)
			return

	var gm = get_root().get_node_or_null("GameManager")
	if gm == null:
		_fail("NO_GAME_MANAGER")
		return

	for method in ["on_lane_gate_proximity_entered_v0", "on_lane_arrival_v0"]:
		if not gm.has_method(method):
			_fail("MISSING_" + method)
			return

	# --- Step 1: initial state is IN_FLIGHT ---
	var s0: String = bridge.call("GetPlayerShipStateNameV0")
	_ok("STATE_INITIAL|" + s0)
	if s0 != "IN_FLIGHT":
		_fail("EXPECTED_IN_FLIGHT_GOT_" + s0)
		return

	# --- Step 2: find a neighbor node to travel to ---
	var galaxy_snap = bridge.call("GetGalaxySnapshotV0")
	if galaxy_snap == null or not galaxy_snap.has("player_current_node_id"):
		_fail("NO_GALAXY_SNAPSHOT")
		return
	var current_node_id: String = galaxy_snap["player_current_node_id"]
	if current_node_id == "":
		_fail("EMPTY_CURRENT_NODE_ID")
		return

	var system_snap = bridge.call("GetSystemSnapshotV0", current_node_id)
	if system_snap == null or not system_snap.has("lane_gate"):
		_fail("NO_SYSTEM_SNAPSHOT_LANE_GATE")
		return
	var lane_gates = system_snap["lane_gate"]
	if lane_gates.size() == 0:
		_fail("NO_LANE_GATES_IN_SYSTEM")
		return
	# Use the first neighbor (deterministic: galaxy seed is fixed).
	var neighbor_id: String = lane_gates[0]["neighbor_node_id"]
	_ok("NEIGHBOR_NODE_ID|" + neighbor_id)

	# --- Step 3: snapshot tick before dispatch ---
	var tick_before: int = bridge.call("GetSimTickV0")

	# --- Step 4: trigger lane gate entry ---
	gm.call("on_lane_gate_proximity_entered_v0", neighbor_id)
	var s1: String = bridge.call("GetPlayerShipStateNameV0")
	_ok("STATE_AFTER_LANE_ENTER|" + s1)
	if s1 != "IN_LANE_TRANSIT":
		_fail("EXPECTED_IN_LANE_TRANSIT_GOT_" + s1)
		return

	# --- Step 5: wait for SimCore to process TravelCommand and advance ticks ---
	# TickDelayMs=100ms; 0.6s real time guarantees >= 5 SimCore ticks.
	await create_timer(0.6).timeout

	var tick_after: int = bridge.call("GetSimTickV0")
	var tick_advanced: bool = tick_after > tick_before
	_ok("tick_advance=" + str(tick_advanced).to_lower())
	if not tick_advanced:
		_fail("TICK_DID_NOT_ADVANCE|before=" + str(tick_before) + "|after=" + str(tick_after))
		return

	# --- Step 6: verify fleet dispatched to Traveling or arrived (Idle at destination) ---
	var fleet_state: String = bridge.call("GetFleetStateV0", "fleet_trader_1")
	_ok("fleet_state=" + fleet_state)
	if fleet_state != "Traveling" and fleet_state != "Idle" and fleet_state != "Docked":
		_fail("UNEXPECTED_FLEET_STATE|" + fleet_state)
		return

	# --- Step 7: complete lane transit → IN_FLIGHT ---
	gm.call("on_lane_arrival_v0", neighbor_id)
	var s2: String = bridge.call("GetPlayerShipStateNameV0")
	_ok("STATE_AFTER_LANE_EXIT|" + s2)
	if s2 != "IN_FLIGHT":
		_fail("EXPECTED_IN_FLIGHT_AFTER_LANE_EXIT_GOT_" + s2)
		return

	_ok("DONE")
	_stop_sim_and_quit(0)
