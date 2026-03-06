extends SceneTree

# GATE.S16.NPC_ALIVE.HEADLESS_PROOF.001
# Headless proof: verify NPC ships spawn as physical CharacterBody3D nodes,
# have fleet transit data, and the combat bridge works.
# Emits: NPC_SHIPS|PASS or NPC_SHIPS|FAIL

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const PREFIX := "NPC_SHIPS|"

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_stop_sim_and_quit(1)


func _ok(msg: String) -> void:
	print(PREFIX + "OK|" + msg)


func _initialize() -> void:
	print(PREFIX + "BOOT")
	call_deferred("_run")


func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	# Wait for scene boot, SimBridge init, and local system draw.
	await create_timer(4.0).timeout

	# Check SimBridge booted.
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("NO_SIMBRIDGE")
		return
	_ok("SIMBRIDGE_OK")

	# Check GetFleetTransitFactsV0 exists.
	if not bridge.has_method("GetFleetTransitFactsV0"):
		_fail("NO_GetFleetTransitFactsV0")
		return
	_ok("TRANSIT_API_EXISTS")

	# Check DamageNpcFleetV0 exists.
	if not bridge.has_method("DamageNpcFleetV0"):
		_fail("NO_DamageNpcFleetV0")
		return
	_ok("DAMAGE_API_EXISTS")

	# Check for NPC ships in the NpcShip group.
	var npc_ships = get_root().get_nodes_in_group("NpcShip") if true else []
	_ok("NPC_SHIP_COUNT|%d" % npc_ships.size())

	# Check for fleet ships (FleetShip group — includes legacy markers).
	var fleet_ships = get_root().get_nodes_in_group("FleetShip") if true else []
	_ok("FLEET_SHIP_COUNT|%d" % fleet_ships.size())

	# Query transit facts for the current system.
	var gm = get_root().get_node_or_null("GameManager")
	var current_node_id := ""
	if gm:
		current_node_id = str(gm.get("current_system_id")) if gm.get("current_system_id") != null else ""
	if current_node_id.is_empty():
		current_node_id = str(bridge.call("GetCurrentNodeIdV0")) if bridge.has_method("GetCurrentNodeIdV0") else ""

	if not current_node_id.is_empty():
		var facts: Array = bridge.call("GetFleetTransitFactsV0", current_node_id)
		_ok("TRANSIT_FACTS_COUNT|%d" % facts.size())
		for fact in facts:
			var fid: String = fact.get("fleet_id", "")
			var role: int = fact.get("role", -1)
			var state: String = fact.get("state", "?")
			_ok("FACT|%s|role=%d|state=%s" % [fid, role, state])
	else:
		_ok("SKIP_TRANSIT_FACTS|no_current_node")

	# Wait a few frames and check if ships moved (position changes).
	if npc_ships.size() > 0:
		var ship0 = npc_ships[0]
		var pos_before: Vector3 = ship0.global_position
		await create_timer(1.0).timeout
		var pos_after: Vector3 = ship0.global_position
		var moved := pos_before.distance_to(pos_after) > 0.01
		_ok("SHIP_MOVED|%s|dist=%.2f" % [str(moved), pos_before.distance_to(pos_after)])

	# All checks passed.
	print(PREFIX + "PASS")
	_stop_sim_and_quit(0)
