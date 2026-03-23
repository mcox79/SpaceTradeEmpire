# GATE.T51.VO.LOOKUP_SYSTEM.001: VO file lookup by speaker+key+sequence.
# Autoload "VOLookup" — resolves VO audio files with graceful fallback.
# Path pattern: res://assets/audio/vo/{speaker}/{vo_key}_{seq:02d}.mp3
# Supported speakers: computer, maren, analyst, veteran, pathfinder
extends Node

# Voice preset for ship computer (affects speaker subfolder).
# Values: "female" (default), "male", "neutral"
var computer_voice_preset: String = "female"

# Cache of resolved AudioStream references (speaker+key+seq -> stream or null).
var _cache: Dictionary = {}


## Resolve a VO audio file. Returns AudioStream or null if file doesn't exist.
## speaker: "computer", "maren", "analyst", "veteran", "pathfinder"
## vo_key: the dialogue key (e.g., "awaken", "flight_intro")
## sequence: 0-based sequence index for multi-line phases
func lookup(speaker: String, vo_key: String, sequence: int = 0) -> AudioStream:
	if vo_key.is_empty():
		return null

	# Resolve actual speaker folder (computer uses voice preset subfolder).
	var folder := _resolve_speaker_folder(speaker)

	var cache_key := "%s/%s_%02d" % [folder, vo_key, sequence]
	if _cache.has(cache_key):
		return _cache[cache_key]

	var path := "res://assets/audio/vo/%s/%s_%02d.mp3" % [folder, vo_key, sequence]

	# Also try without sequence suffix for single-line phases.
	var stream: AudioStream = null
	if ResourceLoader.exists(path):
		stream = load(path)
	elif sequence == 0:
		# Fallback: try without _00 suffix.
		var alt_path := "res://assets/audio/vo/%s/%s.mp3" % [folder, vo_key]
		if ResourceLoader.exists(alt_path):
			stream = load(alt_path)

	_cache[cache_key] = stream
	return stream


## Check if any VO exists for a speaker+key (any sequence).
func has_vo(speaker: String, vo_key: String) -> bool:
	return lookup(speaker, vo_key, 0) != null


## Clear the lookup cache (e.g., when voice preset changes).
func clear_cache() -> void:
	_cache.clear()


## Resolve speaker to filesystem folder name.
func _resolve_speaker_folder(speaker: String) -> String:
	match speaker.to_lower():
		"computer":
			# Map voice preset to subfolder.
			match computer_voice_preset:
				"male":    return "computer_male"
				"neutral": return "computer_neutral"
				_:         return "computer"
		"maren", "analyst":
			return "maren"
		"dask", "veteran":
			return "dask"
		"lira", "pathfinder":
			return "lira"
		_:
			return speaker.to_lower()
