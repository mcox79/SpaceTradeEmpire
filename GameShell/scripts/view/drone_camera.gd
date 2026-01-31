extends Camera3D

@export var target_group: String = 'Player'
@export var follow_distance: float = 25.0
@export var height: float = 35.0
@export var smooth_speed: float = 8.0
@export var look_ahead: float = 1.5

var _target: Node3D = null

func _ready() -> void:
	current = false
	_target = _find_target()
	set_as_top_level(true)

func _physics_process(delta: float) -> void:
	if not is_instance_valid(_target):
		_target = _find_target()
		if not _target: return

	# 1. PLANAR TRACKING (Ignore Target Y)
	# We track the target's XZ position, but enforce our own fixed Y height.
	var target_pos_flat = Vector3(_target.global_position.x, 0, _target.global_position.z)
	
	# 2. OFFSET CALCULATION
	var target_forward = -_target.global_transform.basis.z
	target_forward.y = 0
	target_forward = target_forward.normalized()
	
	# Position camera behind the ship
	var desired_pos = target_pos_flat - (target_forward * follow_distance)
	desired_pos.y = height # Lock Height
	
	# 3. SMOOTHING
	global_position = global_position.lerp(desired_pos, smooth_speed * delta)
	
	# 4. LOOK AT PREDICTION
	var look_target = target_pos_flat
	if 'velocity' in _target:
		var vel_flat = _target.velocity
		vel_flat.y = 0
		look_target += vel_flat * look_ahead
	
	look_at(look_target, Vector3.UP)

func _find_target() -> Node3D:
	var nodes := get_tree().get_nodes_in_group(target_group)
	if nodes.size() > 0 and nodes[0] is Node3D:
		return nodes[0] as Node3D
	return null