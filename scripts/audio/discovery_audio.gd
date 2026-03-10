extends Node

# GATE.S7.AUDIO_WIRING.DISCOVERY_CHIMES.001: Discovery phase transition audio.
# Seen = quiet radar ping (0.5s), Scanned = rising chime (1.0s),
# Analyzed = revelation fanfare (2.0s).
# Uses baked AudioStreamWAV — no external files required.

var _seen_player: AudioStreamPlayer = null
var _scanned_player: AudioStreamPlayer = null
var _analyzed_player: AudioStreamPlayer = null

func _ready() -> void:
	_seen_player = _make_player(_bake_ping(440.0, 0.5, 22050), -14.0)
	_scanned_player = _make_player(_bake_rising_chime(440.0, 880.0, 1.0, 22050), -10.0)
	_analyzed_player = _make_player(_bake_fanfare(22050), -6.0)

func play_phase_chime(phase: String) -> void:
	match phase:
		"SEEN":
			if _seen_player and not _seen_player.playing:
				_seen_player.play()
		"SCANNED":
			if _scanned_player and not _scanned_player.playing:
				_scanned_player.play()
		"ANALYZED":
			if _analyzed_player and not _analyzed_player.playing:
				_analyzed_player.play()

func _make_player(stream: AudioStreamWAV, vol_db: float) -> AudioStreamPlayer:
	var p := AudioStreamPlayer.new()
	p.bus = &"UI"
	p.volume_db = vol_db
	p.stream = stream
	add_child(p)
	return p

# Short radar ping: single tone with quick attack/decay envelope.
func _bake_ping(freq: float, duration: float, mix_rate: int) -> AudioStreamWAV:
	var total_samples := int(duration * mix_rate)
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	for i in total_samples:
		var t: float = float(i) / float(mix_rate)
		var env: float = exp(-t * 8.0)  # Fast decay
		var sample: float = sin(t * freq * TAU) * env * 0.4
		data.encode_s16(i * 2, clampi(int(sample * 32767.0), -32768, 32767))
	return _make_wav(data, mix_rate, total_samples)

# Rising chime: frequency sweeps from low to high with sustain.
func _bake_rising_chime(freq_start: float, freq_end: float, duration: float, mix_rate: int) -> AudioStreamWAV:
	var total_samples := int(duration * mix_rate)
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	for i in total_samples:
		var t: float = float(i) / float(mix_rate)
		var progress: float = t / duration
		var freq: float = lerpf(freq_start, freq_end, progress * progress)
		var env: float = sin(progress * PI)  # Bell curve envelope
		var sample: float = sin(t * freq * TAU) * env * 0.35
		# Add harmonic for richness
		sample += sin(t * freq * 1.5 * TAU) * env * 0.15
		data.encode_s16(i * 2, clampi(int(sample * 32767.0), -32768, 32767))
	return _make_wav(data, mix_rate, total_samples)

# Revelation fanfare: chord (root + major third + fifth) with crescendo.
func _bake_fanfare(mix_rate: int) -> AudioStreamWAV:
	var duration := 2.0
	var total_samples := int(duration * mix_rate)
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	var root := 523.25  # C5
	var third := 659.25  # E5
	var fifth := 783.99  # G5
	for i in total_samples:
		var t: float = float(i) / float(mix_rate)
		var progress: float = t / duration
		# Crescendo then sustain then fade
		var env: float
		if progress < 0.3:
			env = progress / 0.3  # Attack
		elif progress < 0.7:
			env = 1.0  # Sustain
		else:
			env = (1.0 - progress) / 0.3  # Release
		var sample: float = 0.0
		sample += sin(t * root * TAU) * 0.25
		sample += sin(t * third * TAU) * 0.2
		sample += sin(t * fifth * TAU) * 0.2
		sample *= env
		data.encode_s16(i * 2, clampi(int(sample * 32767.0), -32768, 32767))
	return _make_wav(data, mix_rate, total_samples)

func _make_wav(data: PackedByteArray, mix_rate: int, total_samples: int) -> AudioStreamWAV:
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = mix_rate
	wav.stereo = false
	wav.data = data
	wav.loop_mode = AudioStreamWAV.LOOP_DISABLED
	return wav
