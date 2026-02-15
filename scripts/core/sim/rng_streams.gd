extends RefCounted
class_name RngStreams

# Domain-isolated randomness with stable stream contracts.
# Invariant:
# - Existing legacy streams keep their exact seeds: base_seed + fixed_offset
# - New streams derive seeds from (base_seed, stream_name) only
# - Adding new streams must not perturb existing stream outputs

const STREAM_GALAXY_GEN := "galaxy_gen"
const STREAM_ECONOMY := "economy"
const STREAM_COMBAT := "combat"
const STREAM_TRAVEL := "travel"

var _base_seed: int
var _streams: Dictionary = {}

# Fixed legacy offsets. Never reorder or change these values.
const _LEGACY_OFFSETS := {
	STREAM_GALAXY_GEN: 0,
	STREAM_ECONOMY: 1,
	STREAM_COMBAT: 2,
	STREAM_TRAVEL: 3,
}

func _init(base_seed: int):
	_base_seed = base_seed

	# Pre-warm legacy streams so behavior matches earlier eager creation.
	get_stream(STREAM_GALAXY_GEN)
	get_stream(STREAM_ECONOMY)
	get_stream(STREAM_COMBAT)
	get_stream(STREAM_TRAVEL)

func get_stream(stream_name: String) -> RandomNumberGenerator:
	var existing = _streams.get(stream_name, null)
	if existing != null:
		return existing

	var rng := RandomNumberGenerator.new()
	rng.seed = _seed_for_stream(stream_name)
	_streams[stream_name] = rng
	return rng

func _seed_for_stream(stream_name: String) -> int:
	if _LEGACY_OFFSETS.has(stream_name):
		return _base_seed + int(_LEGACY_OFFSETS[stream_name])

	# For new streams: seed depends ONLY on (base_seed, stream_name).
	# This is stable and does not depend on any list ordering.
	var h := _hash_stream_name(stream_name)
	var s := int((_base_seed ^ h) & 0x7fffffff)
	return s

func _hash_stream_name(stream_name: String) -> int:
	# Deterministic 32-bit FNV-1a over UTF-8 bytes.
	var bytes: PackedByteArray = stream_name.to_utf8_buffer()
	var hash: int = 0x811c9dc5
	for b in bytes:
		hash = int((hash ^ int(b)) & 0xffffffff)
		hash = int((hash * 0x01000193) & 0xffffffff)
	return hash
