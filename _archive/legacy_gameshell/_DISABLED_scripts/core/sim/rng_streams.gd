extends RefCounted
class_name RngStreams

# Domain-isolated randomness.

var _streams: Dictionary = {}

func _init(base_seed: int):
	_create_stream("galaxy_gen", base_seed)
	_create_stream("economy", base_seed + 1)
	_create_stream("combat", base_seed + 2)
	_create_stream("travel", base_seed + 3)

func _create_stream(stream_name: String, stream_seed: int):
	var rng = RandomNumberGenerator.new()
	rng.seed = stream_seed
	_streams[stream_name] = rng

func get_stream(stream_name: String) -> RandomNumberGenerator:
	return _streams.get(stream_name, null)