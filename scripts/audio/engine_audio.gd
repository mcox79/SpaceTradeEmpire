extends AudioStreamPlayer

# GATE.S1.SPATIAL_AUDIO.ENGINE_THRUST.001: Engine thrust audio controller.
# Modulates pitch based on parent ship velocity.
# Uses AudioStreamPlayer (non-3D) to avoid the known AudioStreamPlayer3D
# mixer-thread pop bug (godotengine/godot#35689).

@export var min_pitch: float = 0.95
@export var max_pitch: float = 1.15
@export var max_speed: float = 30.0

var _parent_body: RigidBody3D = null

func _ready() -> void:
	_parent_body = get_parent() as RigidBody3D
	if _parent_body == null:
		_parent_body = get_parent().get_parent() as RigidBody3D
	if stream == null:
		stream = load("res://assets/audio/engine_hum.wav")
	volume_db = -18.0
	play()

func _process(_delta: float) -> void:
	if _parent_body == null:
		return
	var speed: float = _parent_body.linear_velocity.length()
	var t: float = clampf(speed / max_speed, 0.0, 1.0)
	pitch_scale = lerpf(min_pitch, max_pitch, t)
