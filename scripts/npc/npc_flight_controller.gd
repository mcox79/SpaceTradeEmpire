extends CharacterBody3D
## NPC flight controller — GATE.S16.NPC_ALIVE.FLIGHT_CTRL.001
## Kinematic (move_and_slide) movement toward a target_position at target_speed.
## Smooth rotation via quaternion slerp. Locked to XZ plane.
## Can be paused (combat stagger) by setting stagger_remaining > 0.

## Movement target set by BT or spawn system.
var target_position: Vector3 = Vector3.ZERO
var target_speed: float = 6.0

## Rotation smoothing (radians per second equivalent; higher = snappier).
@export var rotation_sharpness: float = 5.0

## Combat stagger: while > 0, ship doesn't move. Decremented each _physics_process.
var stagger_remaining: float = 0.0

## Arrival threshold — stop moving when closer than this.
const ARRIVAL_THRESHOLD: float = 1.5


func _ready() -> void:
	# Lock to XZ plane — no vertical drift.
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	up_direction = Vector3.UP
	# NPC ships should not block each other.
	collision_layer = 4   # NPC layer
	collision_mask = 0    # Don't collide with anything (purely kinematic)


func _physics_process(delta: float) -> void:
	# Stagger: skip movement while staggered.
	if stagger_remaining > 0.0:
		stagger_remaining -= delta
		velocity = Vector3.ZERO
		move_and_slide()
		return

	# Direction to target (XZ only).
	var to_target := target_position - global_position
	to_target.y = 0.0
	var dist := to_target.length()

	if dist < ARRIVAL_THRESHOLD:
		velocity = Vector3.ZERO
		move_and_slide()
		return

	var dir := to_target / dist  # normalized

	# Smooth rotation toward movement direction.
	_rotate_toward(dir, delta)

	# Move toward target.
	velocity = dir * target_speed
	velocity.y = 0.0
	move_and_slide()

	# Keep Y locked after move_and_slide (in case of slope/step).
	global_position.y = 0.0


func _rotate_toward(dir: Vector3, delta: float) -> void:
	if dir.length_squared() < 0.001:
		return
	# Ship forward is -Z.
	var target_basis := Basis.looking_at(-dir, Vector3.UP)
	var current_quat := global_transform.basis.get_rotation_quaternion()
	var target_quat := target_basis.get_rotation_quaternion()
	var weight := clampf(rotation_sharpness * delta, 0.0, 1.0)
	var result := current_quat.slerp(target_quat, weight)
	global_transform.basis = Basis(result)


## Apply stagger from combat hit (seconds).
func apply_stagger(duration: float) -> void:
	stagger_remaining += duration


## Set movement target from world position.
func set_target(pos: Vector3, spd: float) -> void:
	target_position = Vector3(pos.x, 0.0, pos.z)
	target_speed = spd
