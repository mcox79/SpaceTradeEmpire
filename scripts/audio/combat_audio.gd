extends Node

# GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001: Combat audio controller.
# Manages fire and impact SFX for turrets and bullets.
# Uses pre-baked WAV files — no runtime AudioStreamWAV generation.

var _fire_pool: Array[AudioStreamPlayer3D] = []
var _impact_pool: Array[AudioStreamPlayer3D] = []
const POOL_SIZE: int = 8

func _ready() -> void:
	var fire_wav = load("res://assets/audio/laser_fire.wav")
	var impact_wav = load("res://assets/audio/bullet_hit.wav")
	for i in POOL_SIZE:
		var fire_player := AudioStreamPlayer3D.new()
		fire_player.max_distance = 80.0
		fire_player.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
		fire_player.bus = &"SFX"
		fire_player.stream = fire_wav
		add_child(fire_player)
		_fire_pool.append(fire_player)

		var impact_player := AudioStreamPlayer3D.new()
		impact_player.max_distance = 60.0
		impact_player.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
		impact_player.bus = &"SFX"
		impact_player.stream = impact_wav
		add_child(impact_player)
		_impact_pool.append(impact_player)

func play_fire_sfx(position: Vector3) -> void:
	for player in _fire_pool:
		if not player.playing:
			player.global_position = position
			player.play()
			return

func play_impact_sfx(position: Vector3) -> void:
	for player in _impact_pool:
		if not player.playing:
			player.global_position = position
			player.play()
			return
