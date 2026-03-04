extends Node

# GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001: Combat audio controller.
# Manages fire and impact SFX for turrets and bullets.

# Pool of AudioStreamPlayer3D nodes for fire SFX
var _fire_pool: Array[AudioStreamPlayer3D] = []
var _impact_pool: Array[AudioStreamPlayer3D] = []
const POOL_SIZE: int = 8

func _ready() -> void:
	for i in POOL_SIZE:
		var fire_player := AudioStreamPlayer3D.new()
		fire_player.max_distance = 80.0
		fire_player.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
		add_child(fire_player)
		_fire_pool.append(fire_player)

		var impact_player := AudioStreamPlayer3D.new()
		impact_player.max_distance = 60.0
		impact_player.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
		add_child(impact_player)
		_impact_pool.append(impact_player)

func play_fire_sfx(position: Vector3) -> void:
	for player in _fire_pool:
		if not player.playing:
			player.global_position = position
			# Use a simple generated tone as placeholder
			if player.stream == null:
				var gen := AudioStreamGenerator.new()
				gen.mix_rate = 22050
				gen.buffer_length = 0.05
				player.stream = gen
			player.play()
			return

func play_impact_sfx(position: Vector3) -> void:
	for player in _impact_pool:
		if not player.playing:
			player.global_position = position
			if player.stream == null:
				var gen := AudioStreamGenerator.new()
				gen.mix_rate = 22050
				gen.buffer_length = 0.08
				player.stream = gen
			player.play()
			return
