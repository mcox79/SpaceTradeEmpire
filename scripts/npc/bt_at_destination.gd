@tool
extends BTCondition
class_name BtAtDestination
## GATE.S16.NPC_ALIVE.BT_TASKS.001
## BT condition: checks if ship is within arrival distance of target_pos.
## Returns SUCCESS if arrived, FAILURE otherwise.

@export var arrival_distance: float = 3.0

func _get_name() -> StringName:
	return &"AtDestination"

func _tick(_delta: float) -> int:
	var ship = agent
	if ship == null:
		return FAILURE

	var target_pos: Vector3 = blackboard.get_var("target_pos", Vector3.ZERO)
	var dist := ship.global_position.distance_to(target_pos)

	if dist <= arrival_distance:
		return SUCCESS
	return FAILURE
