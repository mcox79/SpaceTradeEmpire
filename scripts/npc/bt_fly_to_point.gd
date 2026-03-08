@tool
extends BTAction
class_name BtFlyToPoint
## GATE.S16.NPC_ALIVE.BT_TASKS.001
## BT action: moves ship toward blackboard "target_pos" via flight controller.
## Returns RUNNING while in transit, SUCCESS on arrival.

const ARRIVE_DIST := 2.0

func _get_name() -> StringName:
	return &"FlyToPoint"

func _tick(delta: float) -> int:
	var ship = agent
	if ship == null:
		return FAILURE

	var target_pos: Vector3 = blackboard.get_var("target_pos", Vector3.ZERO)
	var spd: float = blackboard.get_var("move_speed", 6.0)

	ship.set_target(target_pos, spd)

	var dist: float = ship.global_position.distance_to(target_pos)
	if dist < ARRIVE_DIST:
		return SUCCESS
	return RUNNING
