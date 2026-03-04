extends AudioStreamPlayer3D

# GATE.S1.SPATIAL_AUDIO.ENGINE_THRUST.001: Engine thrust audio controller.
# Modulates pitch and volume based on parent ship velocity.

@export var min_pitch: float = 0.8
@export var max_pitch: float = 1.4
@export var min_volume_db: float = -20.0
@export var max_volume_db: float = -6.0
@export var max_speed: float = 30.0

var _parent_body: RigidBody3D = null

func _ready() -> void:
	_parent_body = get_parent() as RigidBody3D
	if _parent_body == null:
		# Try grandparent
		_parent_body = get_parent().get_parent() as RigidBody3D
	# Use a simple sine wave as placeholder audio
	# In production, replace with actual engine sound files
	if stream == null:
		var noise := AudioStreamGenerator.new()
		noise.mix_rate = 22050
		noise.buffer_length = 0.1
		stream = noise
	autoplay = true
	max_db = min_volume_db

func _process(delta: float) -> void:
	if _parent_body == null:
		return
	var speed: float = _parent_body.linear_velocity.length()
	var t: float = clampf(speed / max_speed, 0.0, 1.0)
	pitch_scale = lerpf(min_pitch, max_pitch, t)
	volume_db = lerpf(min_volume_db, max_volume_db, t)
