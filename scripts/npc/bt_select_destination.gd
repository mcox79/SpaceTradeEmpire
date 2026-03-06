@tool
extends BTAction
class_name BtSelectDestination
## GATE.S16.NPC_ALIVE.BT_TASKS.001
## BT action: queries SimBridge for fleet's next destination, resolves to 3D position.
## Writes "target_pos" and "move_speed" to blackboard.
## Returns SUCCESS if destination found, FAILURE if none available.

func _get_name() -> StringName:
	return &"SelectDestination"

func _tick(_delta: float) -> int:
	var ship = agent
	if ship == null:
		return FAILURE

	var fleet_id: String = blackboard.get_var("fleet_id", "")
	if fleet_id.is_empty():
		return FAILURE

	# Query SimBridge for transit data.
	var bridge = _get_bridge()
	if bridge == null:
		return FAILURE

	var current_node: String = blackboard.get_var("current_node_id", "")
	var facts: Array = bridge.call("GetFleetTransitFactsV0", current_node)

	# Find our fleet in the transit facts.
	for fact in facts:
		if fact.get("fleet_id", "") == fleet_id:
			var state: String = fact.get("state", "Idle")
			var dest_node: String = fact.get("destination_node_id", "")
			var speed: float = fact.get("speed", 6.0)

			# If traveling, find the lane gate position for the destination.
			if state == "Traveling" and not dest_node.is_empty():
				var gate_pos := _find_lane_gate_position(dest_node)
				if gate_pos != Vector3.ZERO:
					blackboard.set_var("target_pos", gate_pos)
					blackboard.set_var("move_speed", speed)
					return SUCCESS

			# If idle, pick a random patrol/orbit point.
			var orbit_pos := _pick_orbit_point(ship.global_position)
			blackboard.set_var("target_pos", orbit_pos)
			blackboard.set_var("move_speed", speed * 0.5)
			return SUCCESS

	return FAILURE


func _get_bridge() -> Node:
	var tree := agent.get_tree()
	if tree == null:
		return null
	return tree.root.get_node_or_null("SimBridge")


func _find_lane_gate_position(dest_node_id: String) -> Vector3:
	# Search for lane gate markers in the scene that match the destination.
	var tree := agent.get_tree()
	if tree == null:
		return Vector3.ZERO
	for node in tree.get_nodes_in_group("LaneGate"):
		if node.has_meta("dest_node_id") and node.get_meta("dest_node_id") == dest_node_id:
			return node.global_position
	return Vector3.ZERO


func _pick_orbit_point(origin: Vector3) -> Vector3:
	# Simple random offset for idle orbit.
	var angle := randf() * TAU
	var radius := 15.0 + randf() * 10.0
	return origin + Vector3(cos(angle) * radius, 0.0, sin(angle) * radius)
