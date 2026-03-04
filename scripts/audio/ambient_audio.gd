extends Node

# GATE.S1.SPATIAL_AUDIO.AMBIENT.001: Ambient audio controller.
# Manages spatial ambient sounds for stations and lanes.

var _station_audio_players: Dictionary = {}  # node_id -> AudioStreamPlayer3D
var _space_drone: AudioStreamPlayer = null

func _ready() -> void:
	# Create a non-positional ambient space drone
	_space_drone = AudioStreamPlayer.new()
	_space_drone.bus = &"Master"
	_space_drone.volume_db = -24.0
	_space_drone.autoplay = true
	# Placeholder: use AudioStreamGenerator for a low hum
	var gen := AudioStreamGenerator.new()
	gen.mix_rate = 22050
	gen.buffer_length = 0.5
	_space_drone.stream = gen
	add_child(_space_drone)

func register_station(station_node: Node3D, node_id: String) -> void:
	if _station_audio_players.has(node_id):
		return
	var player := AudioStreamPlayer3D.new()
	player.max_distance = 40.0
	player.attenuation_model = AudioStreamPlayer3D.ATTENUATION_INVERSE_DISTANCE
	player.volume_db = -12.0
	player.autoplay = true
	# Placeholder station hum
	var gen := AudioStreamGenerator.new()
	gen.mix_rate = 22050
	gen.buffer_length = 0.2
	player.stream = gen
	station_node.add_child(player)
	_station_audio_players[node_id] = player

func unregister_station(node_id: String) -> void:
	if _station_audio_players.has(node_id):
		var player = _station_audio_players[node_id]
		if is_instance_valid(player):
			player.queue_free()
		_station_audio_players.erase(node_id)
