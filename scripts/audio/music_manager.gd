# scripts/audio/music_manager.gd
# Dynamic music system with stem-based crossfading.
# Register as autoload "MusicManager" in project.godot.
# GATE.T46.AUDIO.STEM_PIPELINE.001
class_name MusicManager
extends Node

# GATE.T47.MUSIC.FRACTURE_AMBIENCE.001: Added FRACTURE state.
enum MusicState { SILENCE, EXPLORATION, COMBAT, TENSION, DOCK, FRACTURE }

# --- Layer mix targets per state (volume 0.0-1.0) ---
# Order: [bass, pad, melody, percussion]
const LAYER_MIX := {
	MusicState.SILENCE:     [0.0, 0.0, 0.0, 0.0],
	MusicState.EXPLORATION: [0.3, 0.6, 0.2, 0.0],
	MusicState.COMBAT:      [0.8, 0.2, 0.0, 0.9],
	MusicState.TENSION:     [0.5, 0.4, 0.1, 0.3],
	MusicState.DOCK:        [0.2, 0.8, 0.4, 0.0],
	MusicState.FRACTURE:    [0.6, 0.3, 0.0, 0.1],
}

# Placeholder stem frequencies (Hz). Real stems replace these later.
const STEM_FREQ := [80.0, 220.0, 440.0, 0.0]  # 0.0 = white noise for percussion
const STEM_NAMES := ["bass", "pad", "melody", "percussion"]

# GATE.T47.MUSIC.FRACTURE_AMBIENCE.001: Fracture stems use detuned, lower frequencies.
const FRACTURE_STEM_FREQ := [55.0, 147.0, 0.0, 0.0]  # detuned bass, low drone, silence, silence

# Volume mapping: linear 0.0-1.0 -> dB range.
const MAX_DB := -20.0
const SILENT_DB := -80.0

const HOSTILE_CHECK_RANGE := 60.0

# GATE.T47.MUSIC.DISCOVERY_STINGERS.001: Stinger definitions.
# Each stinger: {notes: [[freq_hz, duration_s], ...], total_duration: float}
const STINGER_DEFS := {
	"discovery_minor": {
		"notes": [[523.25, 0.8], [659.25, 0.8], [783.99, 1.4]],  # C5-E5-G5
		"total_duration": 3.0,
	},
	"discovery_major": {
		"notes": [[261.63, 0.8], [329.63, 0.8], [392.0, 0.8], [523.25, 2.6]],  # C4-E4-G4-C5
		"total_duration": 5.0,
	},
	"discovery_revelation": {
		"notes": [],  # Uses sweep generator instead
		"total_duration": 8.0,
	},
}
const STINGER_DUCK_FACTOR := 0.7  # Reduce stem volume to 70% during stinger playback.

# GATE.T47.MUSIC.FACTION_AMBIENT.001: Faction drone frequencies and characteristics.
const FACTION_DRONE_DEFS := {
	"Concord":   {"freq": 220.0, "fm_rate": 0.0, "fm_depth": 0.0},    # Clean A3
	"Chitin":    {"freq": 147.0, "fm_rate": 3.5, "fm_depth": 8.0},    # D3 with FM wobble
	"Weavers":   {"freq": 330.0, "fm_rate": 0.0, "fm_depth": 0.0},    # E4 with overtones
	"Valorin":   {"freq": 110.0, "fm_rate": 0.0, "fm_depth": 0.0},    # A2 (noise mixed in)
	"Communion": {"freq": 440.0, "fm_rate": 0.0, "fm_depth": 0.0},    # A4 ethereal
}
const FACTION_AMBIENT_DB := -38.0  # ~-18dB below main stems (MAX_DB is -20)

# --- State ---
var _current_state: MusicState = MusicState.SILENCE
var _master_volume: float = 1.0
var _layers: Array[AudioStreamPlayer] = []
var _layer_tweens: Array[Tween] = [null, null, null, null]

# Backward-compat fields read by test_audio_atmosphere_eval_v0.gd.
var _in_combat := false
var _calm_player: AudioStreamPlayer = null   # alias -> pad layer (index 1)
var _combat_player: AudioStreamPlayer = null # alias -> percussion layer (index 3)
var _intro_was_active := true

# GATE.T47.MUSIC.DISCOVERY_STINGERS.001: Stinger state.
var _stinger_player: AudioStreamPlayer = null
var _stinger_active := false
var _stinger_duck_tween: Tween = null
var _stinger_restore_tween: Tween = null
var _pre_stinger_volumes: Array[float] = []

# GATE.T47.MUSIC.FRACTURE_AMBIENCE.001: Fracture stream cache.
var _fracture_streams: Array[AudioStreamWAV] = []
var _normal_streams: Array[AudioStreamWAV] = []
var _in_fracture := false

# GATE.T47.MUSIC.FACTION_AMBIENT.001: Faction ambient state.
var _faction_player: AudioStreamPlayer = null
var _faction_tween: Tween = null
var _current_faction_id: String = ""
var _faction_stream_cache: Dictionary = {}  # faction_id -> AudioStreamWAV

# GATE.T51.VO.BUS_PLAYER.001: VO playback state.
var _vo_player: AudioStreamPlayer = null
var _vo_duck_tween: Tween = null
var _vo_active := false
const VO_DUCK_DB := -8.0  # Duck Music/Ambient by ~8 dB (≈40% volume)
const VO_DUCK_DURATION := 0.2  # 200ms fade
const VO_RESTORE_DURATION := 0.4  # 400ms restore

# ---------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------

func _ready() -> void:
	# Music must keep playing during tree pause (gate transit, pause menu).
	process_mode = Node.PROCESS_MODE_ALWAYS
	# GATE.S7.AUDIO_WIRING.BUS_WIRE.001: 5-layer audio bus setup.
	_setup_audio_buses_v0()

	var music_bus: StringName = &"Music" if AudioServer.get_bus_index("Music") >= 0 else &"Master"
	var ambient_bus: StringName = &"Ambient" if AudioServer.get_bus_index("Ambient") >= 0 else &"Master"

	for i in 4:
		var player := AudioStreamPlayer.new()
		player.name = "Stem_%s" % STEM_NAMES[i]
		player.bus = music_bus
		player.volume_db = SILENT_DB
		player.stream = _make_placeholder_stream(i)
		add_child(player)
		player.play()
		player.finished.connect(player.play)
		_layers.append(player)
		# Cache normal streams for swapping back from fracture.
		_normal_streams.append(player.stream)

	# Pre-generate fracture streams.
	for i in 4:
		_fracture_streams.append(_make_fracture_stream(i))

	# Backward-compat aliases.
	_calm_player = _layers[1]    # pad
	_combat_player = _layers[3]  # percussion

	# GATE.T47.MUSIC.DISCOVERY_STINGERS.001: Stinger player on Music bus.
	_stinger_player = AudioStreamPlayer.new()
	_stinger_player.name = "StingerPlayer"
	_stinger_player.bus = music_bus
	_stinger_player.volume_db = SILENT_DB
	add_child(_stinger_player)

	# GATE.T47.MUSIC.FACTION_AMBIENT.001: Faction ambient player on Ambient bus.
	_faction_player = AudioStreamPlayer.new()
	_faction_player.name = "FactionAmbient"
	_faction_player.bus = ambient_bus
	_faction_player.volume_db = SILENT_DB
	add_child(_faction_player)

	# GATE.T51.VO.BUS_PLAYER.001: VO player on dedicated VO bus.
	var vo_bus: StringName = &"VO" if AudioServer.get_bus_index("VO") >= 0 else &"Master"
	_vo_player = AudioStreamPlayer.new()
	_vo_player.name = "VOPlayer"
	_vo_player.bus = vo_bus
	_vo_player.volume_db = SILENT_DB
	add_child(_vo_player)
	_vo_player.finished.connect(_on_vo_finished)

func _process(_delta: float) -> void:
	# Swell to EXPLORATION when intro finishes.
	if _intro_was_active:
		var gm = get_tree().root.get_node_or_null("GameManager") if get_tree() else null
		var intro: bool = gm.get("intro_active") if gm else true
		if not intro:
			_intro_was_active = false
			transition_to(MusicState.EXPLORATION)

	# Auto-detect combat from hostile proximity.
	var hostiles_near := _check_hostiles_near()
	if hostiles_near and _current_state != MusicState.COMBAT:
		transition_to(MusicState.COMBAT)
		_in_combat = true
	elif not hostiles_near and _in_combat:
		_in_combat = false
		# Return to exploration (or dock if docked -- caller should manage via transition_to).
		if _current_state == MusicState.COMBAT:
			if _in_fracture:
				transition_to(MusicState.FRACTURE)
			else:
				transition_to(MusicState.EXPLORATION)

# ---------------------------------------------------------------
# Public API
# ---------------------------------------------------------------

## Crossfade all layers to the target state's mix over fade_duration seconds.
func transition_to(state: MusicState, fade_duration: float = 2.0) -> void:
	# GATE.T47.MUSIC.FRACTURE_AMBIENCE.001: Swap stem streams when entering/leaving FRACTURE.
	if state == MusicState.FRACTURE and _current_state != MusicState.FRACTURE:
		_swap_stems_to_fracture()
	elif state != MusicState.FRACTURE and _current_state == MusicState.FRACTURE:
		_swap_stems_to_normal()

	_current_state = state
	var mix: Array = LAYER_MIX[state]
	for i in 4:
		var target_db := _linear_to_db(mix[i] * _master_volume)
		_fade_layer(i, target_db, fade_duration)

## Returns the current music state.
func get_current_state() -> MusicState:
	return _current_state

## Scales all layers by a master volume (0.0-1.0).
func set_master_volume(vol: float) -> void:
	_master_volume = clampf(vol, 0.0, 1.0)
	# Re-apply current state mix with new master volume (instant).
	var mix: Array = LAYER_MIX[_current_state]
	for i in 4:
		var target_db := _linear_to_db(mix[i] * _master_volume)
		_layers[i].volume_db = target_db

## True if any layer is audible (volume above silence threshold).
func is_playing() -> bool:
	for layer in _layers:
		if layer.volume_db > SILENT_DB + 5.0:
			return true
	return false

# ---------------------------------------------------------------
# GATE.T47.MUSIC.DISCOVERY_STINGERS.001: Discovery Stingers
# ---------------------------------------------------------------

## Play a one-shot stinger that ducks the stem layers during playback.
## stinger_name: "discovery_minor", "discovery_major", or "discovery_revelation"
func play_stinger(stinger_name: String) -> void:
	if not STINGER_DEFS.has(stinger_name):
		push_warning("MusicManager: Unknown stinger '%s'" % stinger_name)
		return
	if _stinger_active:
		# Already playing a stinger -- skip (don't stack).
		return

	var def: Dictionary = STINGER_DEFS[stinger_name]
	var total_dur: float = def["total_duration"]

	# Generate stinger audio.
	var stream := _make_stinger_stream(stinger_name)
	_stinger_player.stream = stream
	_stinger_player.volume_db = MAX_DB  # Audible level.
	_stinger_player.play()
	_stinger_active = true

	# Duck stem layers by STINGER_DUCK_FACTOR.
	_pre_stinger_volumes.clear()
	for i in 4:
		_pre_stinger_volumes.append(_layers[i].volume_db)
	_duck_stems(true)

	# Schedule un-duck and cleanup after stinger finishes.
	if _stinger_restore_tween and _stinger_restore_tween.is_valid():
		_stinger_restore_tween.kill()
	_stinger_restore_tween = create_tween()
	_stinger_restore_tween.tween_callback(_on_stinger_finished).set_delay(total_dur)

func _on_stinger_finished() -> void:
	_stinger_active = false
	_stinger_player.stop()
	_stinger_player.volume_db = SILENT_DB
	_duck_stems(false)

func _duck_stems(duck: bool) -> void:
	if _stinger_duck_tween and _stinger_duck_tween.is_valid():
		_stinger_duck_tween.kill()
	_stinger_duck_tween = create_tween()
	_stinger_duck_tween.set_parallel(true)
	for i in 4:
		if duck:
			# Duck: reduce current volume by duck factor (in dB, subtract ~3.5 dB for 30% reduction).
			var ducked_db := _layers[i].volume_db - 3.5
			_stinger_duck_tween.tween_property(_layers[i], "volume_db", ducked_db, 0.3)
		else:
			# Restore to current state mix level.
			var mix: Array = LAYER_MIX[_current_state]
			var restore_db := _linear_to_db(mix[i] * _master_volume)
			_stinger_duck_tween.tween_property(_layers[i], "volume_db", restore_db, 0.5)

## Generate a stinger AudioStreamWAV from note definitions.
func _make_stinger_stream(stinger_name: String) -> AudioStreamWAV:
	var sample_rate := 22050
	var def: Dictionary = STINGER_DEFS[stinger_name]
	var total_dur: float = def["total_duration"]
	var num_samples := int(sample_rate * total_dur)
	var data := PackedByteArray()
	data.resize(num_samples)

	if stinger_name == "discovery_revelation":
		# Dramatic sweep: C3 (130.81 Hz) rising to C5 (523.25 Hz) with volume crescendo.
		var freq_start := 130.81
		var freq_end := 523.25
		for s in num_samples:
			var t: float = float(s) / float(num_samples)
			# Exponential frequency sweep.
			var freq: float = freq_start * pow(freq_end / freq_start, t)
			# Volume crescendo: start quiet, end loud.
			var amplitude: float = 0.3 + 0.7 * t
			# Fade out last 10%.
			if t > 0.9:
				amplitude *= (1.0 - t) / 0.1
			var phase: float = 0.0
			# Accumulate phase for smooth sweep (approximate via instantaneous frequency).
			if s > 0:
				phase = 2.0 * PI * freq * float(s) / float(sample_rate)
			var val: float = sin(phase) * amplitude
			data[s] = int(clampf(val * 40.0 + 128.0, 0.0, 255.0))
	else:
		# Note-based stinger: play each note in sequence.
		var notes: Array = def["notes"]
		var sample_offset := 0
		for note in notes:
			var freq: float = note[0]
			var note_dur: float = note[1]
			var note_samples := int(sample_rate * note_dur)
			for s in note_samples:
				if sample_offset + s >= num_samples:
					break
				var t: float = float(s) / float(note_samples)
				# Envelope: quick attack, sustain, fade last 20%.
				var env: float = 1.0
				if t < 0.05:
					env = t / 0.05
				elif t > 0.8:
					env = (1.0 - t) / 0.2
				var val: float = sin(2.0 * PI * freq * float(s) / float(sample_rate)) * env
				data[sample_offset + s] = int(clampf(val * 40.0 + 128.0, 0.0, 255.0))
			sample_offset += note_samples

	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.mix_rate = sample_rate
	stream.data = data
	stream.loop_mode = AudioStreamWAV.LOOP_DISABLED
	return stream

# ---------------------------------------------------------------
# GATE.T47.MUSIC.FRACTURE_AMBIENCE.001: Fracture Space Ambience
# ---------------------------------------------------------------

## Enter fracture ambience mode. Called by game_manager when instability >= phase 2.
func enter_fracture_ambience() -> void:
	if _in_fracture:
		return
	_in_fracture = true
	transition_to(MusicState.FRACTURE, 3.0)  # 3-second crossfade

## Leave fracture ambience mode. Called when leaving fracture space.
func leave_fracture_ambience() -> void:
	if not _in_fracture:
		return
	_in_fracture = false
	transition_to(MusicState.EXPLORATION, 3.0)

func _swap_stems_to_fracture() -> void:
	for i in 4:
		_layers[i].stream = _fracture_streams[i]
		# Restart playback with new stream.
		_layers[i].play()

func _swap_stems_to_normal() -> void:
	for i in 4:
		_layers[i].stream = _normal_streams[i]
		_layers[i].play()

## Generate fracture-space placeholder stream: detuned, LFO-modulated, unsettling drone.
func _make_fracture_stream(layer_index: int) -> AudioStreamWAV:
	var sample_rate := 22050
	var duration_s := 4.0
	var num_samples := int(sample_rate * duration_s)
	var data := PackedByteArray()
	data.resize(num_samples)
	var freq: float = FRACTURE_STEM_FREQ[layer_index]

	for s in num_samples:
		var val: float
		if freq <= 0.0:
			val = 0.0  # Silent layers in fracture.
		else:
			var t: float = float(s) / float(sample_rate)
			# Slow LFO modulation on amplitude (0.3 Hz tremolo).
			var lfo: float = 0.6 + 0.4 * sin(2.0 * PI * 0.3 * t)
			# Slight detuning: add a second oscillator ~2 Hz off.
			var detune_offset := 2.0
			var osc1: float = sin(2.0 * PI * freq * t)
			var osc2: float = sin(2.0 * PI * (freq + detune_offset) * t)
			val = (osc1 + osc2 * 0.6) * 0.5 * lfo
		data[s] = int(clampf(val * 20.0 + 128.0, 0.0, 255.0))

	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.mix_rate = sample_rate
	stream.data = data
	stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	stream.loop_end = num_samples
	return stream

# ---------------------------------------------------------------
# GATE.T47.MUSIC.FACTION_AMBIENT.001: Faction Territory Ambient
# ---------------------------------------------------------------

## Crossfade to faction-specific ambient drone. Pass "" to fade to silence.
func set_faction_ambient(faction_id: String) -> void:
	if faction_id == _current_faction_id:
		return  # Already playing this faction's drone.
	_current_faction_id = faction_id

	if _faction_tween and _faction_tween.is_valid():
		_faction_tween.kill()

	if faction_id.is_empty() or not FACTION_DRONE_DEFS.has(faction_id):
		# Fade to silence over 2 seconds.
		_faction_tween = create_tween()
		_faction_tween.tween_property(_faction_player, "volume_db", SILENT_DB, 2.0)\
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
		_faction_tween.tween_callback(_faction_player.stop)
		return

	# Get or create the faction drone stream.
	var stream: AudioStreamWAV = _get_faction_stream(faction_id)
	_faction_player.stream = stream
	_faction_player.volume_db = SILENT_DB
	_faction_player.play()

	# Crossfade in over 2 seconds.
	_faction_tween = create_tween()
	_faction_tween.tween_property(_faction_player, "volume_db", FACTION_AMBIENT_DB, 2.0)\
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

func _get_faction_stream(faction_id: String) -> AudioStreamWAV:
	if _faction_stream_cache.has(faction_id):
		return _faction_stream_cache[faction_id]

	var stream := _make_faction_drone_stream(faction_id)
	_faction_stream_cache[faction_id] = stream
	return stream

## Generate a faction-characteristic placeholder drone.
func _make_faction_drone_stream(faction_id: String) -> AudioStreamWAV:
	var sample_rate := 22050
	var duration_s := 4.0
	var num_samples := int(sample_rate * duration_s)
	var data := PackedByteArray()
	data.resize(num_samples)

	var def: Dictionary = FACTION_DRONE_DEFS.get(faction_id, {"freq": 220.0, "fm_rate": 0.0, "fm_depth": 0.0})
	var freq: float = def["freq"]
	var fm_rate: float = def["fm_rate"]
	var fm_depth: float = def["fm_depth"]

	for s in num_samples:
		var t: float = float(s) / float(sample_rate)
		var val: float

		match faction_id:
			"Concord":
				# Clean, steady sine.
				val = sin(2.0 * PI * freq * t)
			"Chitin":
				# FM wobble: carrier modulated by slow LFO.
				var mod: float = fm_depth * sin(2.0 * PI * fm_rate * t)
				val = sin(2.0 * PI * (freq + mod) * t)
			"Weavers":
				# Harmonic overtones: fundamental + 2nd + 3rd partial.
				val = sin(2.0 * PI * freq * t) * 0.6 \
					+ sin(2.0 * PI * freq * 2.0 * t) * 0.25 \
					+ sin(2.0 * PI * freq * 3.0 * t) * 0.15
			"Valorin":
				# Low tone with noise mixed in (frontier static).
				var tone: float = sin(2.0 * PI * freq * t)
				var noise: float = randf_range(-1.0, 1.0)
				val = tone * 0.7 + noise * 0.3
			"Communion":
				# Ethereal: fundamental + soft octave above, slow amplitude shimmer.
				var shimmer: float = 0.7 + 0.3 * sin(2.0 * PI * 1.5 * t)
				val = (sin(2.0 * PI * freq * t) * 0.6 \
					+ sin(2.0 * PI * freq * 2.0 * t) * 0.4) * shimmer
			_:
				val = sin(2.0 * PI * freq * t)

		data[s] = int(clampf(val * 15.0 + 128.0, 0.0, 255.0))

	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.mix_rate = sample_rate
	stream.data = data
	stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	stream.loop_end = num_samples
	return stream

# ---------------------------------------------------------------
# Internal
# ---------------------------------------------------------------

func _fade_layer(index: int, target_db: float, duration: float) -> void:
	if _layer_tweens[index] and _layer_tweens[index].is_valid():
		_layer_tweens[index].kill()
	var t := create_tween()
	t.tween_property(_layers[index], "volume_db", target_db, duration)\
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	_layer_tweens[index] = t

func _linear_to_db(linear: float) -> float:
	if linear <= 0.001:
		return SILENT_DB
	# Map 0.0-1.0 to SILENT_DB-MAX_DB range via standard log conversion.
	return maxf(20.0 * log(linear) / log(10.0) + MAX_DB, SILENT_DB)

## Generate a placeholder AudioStreamWAV -- sine wave (or white noise for percussion).
func _make_placeholder_stream(layer_index: int) -> AudioStreamWAV:
	var sample_rate := 22050
	var duration_s := 4.0  # 4-second loop
	var num_samples := int(sample_rate * duration_s)
	var data := PackedByteArray()
	data.resize(num_samples)
	var freq: float = STEM_FREQ[layer_index]
	for s in num_samples:
		var val: float
		if freq <= 0.0:
			# White noise for percussion.
			val = randf_range(-1.0, 1.0)
		else:
			val = sin(2.0 * PI * freq * float(s) / float(sample_rate))
		# 8-bit unsigned: 0-255, center at 128.
		data[s] = int(clampf(val * 20.0 + 128.0, 0.0, 255.0))
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.mix_rate = sample_rate
	stream.data = data
	stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	stream.loop_end = num_samples
	return stream

func _check_hostiles_near() -> bool:
	var tree := get_tree()
	if tree == null:
		return false
	# Suppress combat music during intro cinematic.
	var gm = tree.root.get_node_or_null("GameManager")
	if gm and gm.get("intro_active"):
		return false
	var hero: Node3D = null
	for node in tree.get_nodes_in_group("Player"):
		hero = node
		break
	if hero == null:
		return false
	for node in tree.get_nodes_in_group("FleetShip"):
		if not is_instance_valid(node):
			continue
		if hero.global_position.distance_to(node.global_position) > HOSTILE_CHECK_RANGE:
			continue
		if node.get_meta("is_hostile", false):
			return true
	return false

# GATE.S7.AUDIO_WIRING.BUS_WIRE.001: Create 5-layer audio bus hierarchy.
# Music, Ambient, SFX, UI, Alert -- all send to Master.
# Ducking: Alert ducks all (compressor sidechain), SFX ducks Ambient.
func _setup_audio_buses_v0() -> void:
	# GATE.T51.VO.BUS_PLAYER.001: Added VO bus (6th bus).
	var bus_names := ["Music", "Ambient", "SFX", "UI", "Alert", "VO"]
	for bus_name in bus_names:
		var existing_idx := AudioServer.get_bus_index(bus_name)
		if existing_idx >= 0:
			continue
		var idx := AudioServer.bus_count
		AudioServer.add_bus(idx)
		AudioServer.set_bus_name(idx, bus_name)
		AudioServer.set_bus_send(idx, &"Master")
	# Ducking: SFX ducks Ambient (compressor sidechain on Ambient bus).
	var ambient_idx := AudioServer.get_bus_index("Ambient")
	if ambient_idx >= 0 and AudioServer.get_bus_effect_count(ambient_idx) == 0:
		var comp := AudioEffectCompressor.new()
		comp.sidechain = &"SFX"
		comp.threshold = -20.0
		comp.ratio = 4.0
		comp.attack_us = 20.0
		comp.release_ms = 200.0
		AudioServer.add_bus_effect(ambient_idx, comp)
	# Ducking: Alert ducks Music + Ambient (compressor sidechain on both).
	var music_idx := AudioServer.get_bus_index("Music")
	if music_idx >= 0 and AudioServer.get_bus_effect_count(music_idx) == 0:
		var comp2 := AudioEffectCompressor.new()
		comp2.sidechain = &"Alert"
		comp2.threshold = -20.0
		comp2.ratio = 6.0
		comp2.attack_us = 10.0
		comp2.release_ms = 300.0
		AudioServer.add_bus_effect(music_idx, comp2)

# ---------------------------------------------------------------
# GATE.T51.VO.BUS_PLAYER.001: Voice-Over Playback + Ducking
# ---------------------------------------------------------------

## Play a voice-over audio stream. Ducks Music/Ambient while playing.
## Returns the duration in seconds (0 if stream is null).
func play_vo(stream: AudioStream) -> float:
	if stream == null:
		return 0.0
	if _vo_active:
		_vo_player.stop()
		_on_vo_finished()
	_vo_player.stream = stream
	_vo_player.volume_db = MAX_DB
	_vo_player.play()
	_vo_active = true
	_duck_for_vo(true)
	return stream.get_length()

## Stop any currently playing VO and restore ducking.
func stop_vo() -> void:
	if _vo_active:
		_vo_player.stop()
		_on_vo_finished()

## Returns true if VO is currently playing.
func is_vo_playing() -> bool:
	return _vo_active and _vo_player.playing

func _on_vo_finished() -> void:
	_vo_active = false
	_duck_for_vo(false)

## Duck Music and Ambient buses when VO plays, restore when done.
func _duck_for_vo(duck: bool) -> void:
	if _vo_duck_tween and _vo_duck_tween.is_valid():
		_vo_duck_tween.kill()
	_vo_duck_tween = create_tween()
	_vo_duck_tween.set_parallel(true)

	var music_idx := AudioServer.get_bus_index("Music")
	var ambient_idx := AudioServer.get_bus_index("Ambient")

	if duck:
		var dur := VO_DUCK_DURATION
		if music_idx >= 0:
			var cur_db := AudioServer.get_bus_volume_db(music_idx)
			_vo_duck_tween.tween_method(func(db: float) -> void:
				AudioServer.set_bus_volume_db(music_idx, db)
			, cur_db, cur_db + VO_DUCK_DB, dur)
		if ambient_idx >= 0:
			var cur_db := AudioServer.get_bus_volume_db(ambient_idx)
			_vo_duck_tween.tween_method(func(db: float) -> void:
				AudioServer.set_bus_volume_db(ambient_idx, db)
			, cur_db, cur_db + VO_DUCK_DB, dur)
	else:
		var dur := VO_RESTORE_DURATION
		if music_idx >= 0:
			var cur_db := AudioServer.get_bus_volume_db(music_idx)
			_vo_duck_tween.tween_method(func(db: float) -> void:
				AudioServer.set_bus_volume_db(music_idx, db)
			, cur_db, cur_db - VO_DUCK_DB, dur)
		if ambient_idx >= 0:
			var cur_db := AudioServer.get_bus_volume_db(ambient_idx)
			_vo_duck_tween.tween_method(func(db: float) -> void:
				AudioServer.set_bus_volume_db(ambient_idx, db)
			, cur_db, cur_db - VO_DUCK_DB, dur)
