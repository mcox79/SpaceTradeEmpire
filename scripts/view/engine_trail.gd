extends GPUParticles3D

# Tabs-only indentation policy applies.

# Engine exhaust trail controller.
# GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001: Always emits at idle level;
# intensifies when thrust input is active or ship has meaningful velocity.
# Fixes DEAD_PARTICLES audit flag (emitting=false when stationary).

const SPEED_EMIT_THRESHOLD: float = 0.5
const IDLE_SPEED_SCALE: float = 0.15
const ACTIVE_SPEED_SCALE: float = 1.0

var _ship: RigidBody3D

func _ready() -> void:
	# Always emit at idle level — engines glow even when parked.
	emitting = true
	speed_scale = IDLE_SPEED_SCALE
	# Walk up the tree to find the RigidBody3D ancestor.
	var node = get_parent()
	while node != null:
		if node is RigidBody3D:
			_ship = node
			break
		node = node.get_parent()

func _process(_delta: float) -> void:
	if _ship == null:
		return
	var speed: float = _ship.linear_velocity.length()
	var thrusting: bool = (
		Input.is_action_pressed("ship_thrust_fwd") or
		Input.is_action_pressed("ship_thrust_back")
	)
	var active: bool = thrusting or speed > SPEED_EMIT_THRESHOLD
	# Scale particle speed: full when thrusting/moving, reduced idle glow otherwise.
	speed_scale = ACTIVE_SPEED_SCALE if active else IDLE_SPEED_SCALE
