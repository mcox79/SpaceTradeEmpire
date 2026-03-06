@tool
extends BTAction
class_name BtWarpOut
## GATE.S16.NPC_ALIVE.BT_TASKS.001
## BT action: plays warp-out effect then queue_frees the ship.
## Returns SUCCESS immediately (fire-and-forget).

func _get_name() -> StringName:
	return &"WarpOut"

func _tick(_delta: float) -> int:
	var ship = agent
	if ship == null:
		return FAILURE

	# Emit signal for VFX system to play warp effect at this position.
	if ship.has_signal("warp_out"):
		ship.emit_signal("warp_out")

	ship.queue_free()
	return SUCCESS
