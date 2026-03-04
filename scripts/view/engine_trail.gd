extends GPUParticles3D

# Tabs-only indentation policy applies.

# Engine exhaust trail controller.
# Emits orange/blue thruster particles only while the ship has meaningful velocity
# or while thrust input is active.

const SPEED_EMIT_THRESHOLD: float = 0.5

var _ship: RigidBody3D

func _ready() -> void:
	emitting = false
	# Walk up: GPUParticles3D -> Engine (MeshInstance3D) -> ShipVisual (Node3D) -> Player (RigidBody3D)
	var p = get_parent()
	if p:
		var pp = p.get_parent()
		if pp:
			var ppp = pp.get_parent()
			if ppp is RigidBody3D:
				_ship = ppp

func _process(_delta: float) -> void:
	if _ship == null:
		return
	var speed: float = _ship.linear_velocity.length()
	var thrusting: bool = (
		Input.is_action_pressed("ship_thrust_fwd") or
		Input.is_action_pressed("ship_thrust_back")
	)
	emitting = thrusting or speed > SPEED_EMIT_THRESHOLD
