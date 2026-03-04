extends StaticBody3D

# GATE.S1.VISUAL_POLISH.CELESTIAL.001 — slow random rotation for asteroid visual interest.

var _angular_velocity: Vector3 = Vector3.ZERO

func _ready() -> void:
	# Derive a deterministic-ish angular velocity from the node's position hash.
	var seed_val: int = int(abs(global_position.x * 17.0 + global_position.z * 31.0)) & 0xFFFF
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_val
	_angular_velocity = Vector3(
		rng.randf_range(-0.12, 0.12),
		rng.randf_range(-0.18, 0.18),
		rng.randf_range(-0.08, 0.10)
	)

func _process(delta: float) -> void:
	rotate_x(_angular_velocity.x * delta)
	rotate_y(_angular_velocity.y * delta)
	rotate_z(_angular_velocity.z * delta)
