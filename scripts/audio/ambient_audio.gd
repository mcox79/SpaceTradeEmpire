extends Node

# GATE.S1.SPATIAL_AUDIO.AMBIENT.001: Ambient audio controller.
# Manages spatial ambient sounds for stations and lanes.
# Uses baked AudioStreamWAV loops — no AudioStreamGenerator, no buffer clicks.

var _station_audio_players: Dictionary = {}  # node_id -> AudioStreamPlayer3D
var _space_drone: AudioStreamPlayer = null
var _drone_wav: AudioStreamWAV = null
var _station_wav: AudioStreamWAV = null

func _ready() -> void:
	_drone_wav = _bake_tone(40.0, 22050)    # Low 40 Hz space drone
	_station_wav = _bake_tone(120.0, 22050)  # Higher station hum
	_space_drone = AudioStreamPlayer.new()
	_space_drone.bus = &"Master"
	_space_drone.volume_db = -24.0
	_space_drone.stream = _drone_wav
	add_child(_space_drone)
	_space_drone.play()

func register_station(station_node: Node3D, node_id: String) -> void:
	if _station_audio_players.has(node_id):
		return
	var player := AudioStreamPlayer3D.new()
	player.max_distance = 40.0
	player.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
	player.volume_db = -12.0
	player.stream = _station_wav
	station_node.add_child(player)
	player.play()
	_station_audio_players[node_id] = player

func unregister_station(node_id: String) -> void:
	if _station_audio_players.has(node_id):
		var player = _station_audio_players[node_id]
		if is_instance_valid(player):
			player.queue_free()
		_station_audio_players.erase(node_id)

func _bake_tone(frequency: float, mix_rate: int) -> AudioStreamWAV:
	var period_samples: int = int(mix_rate / frequency)
	var num_periods: int = ceili(float(mix_rate) / float(period_samples))
	var total_samples: int = period_samples * num_periods
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	for i in total_samples:
		var phase: float = float(i) / float(period_samples)
		var sample: float = sin(phase * TAU) * 0.5
		data.encode_s16(i * 2, clampi(int(sample * 32767.0), -32768, 32767))
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = mix_rate
	wav.stereo = false
	wav.data = data
	wav.loop_mode = AudioStreamWAV.LOOP_FORWARD
	wav.loop_begin = 0
	wav.loop_end = total_samples
	return wav
