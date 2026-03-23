extends Node

# GATE.T45.DEEP_DREAD.AMBIENT_AUDIO.001: Phase-aware ambient dread audio layers.
# GATE.T45.DEEP_DREAD.FAUNA_AUDIO.001: Lattice Fauna audio presence.
# 5 ambient layers crossfade by instability phase:
#   Safe = busy chatter/radio hum, Shimmer = thinning hum, Drift = low drone,
#   Fracture = deep resonance, Void = near-silence with clarity tone.
# Fauna: distant harmonic pulse when fauna present, phantom plays at 20% chance.

var _bridge = null

# Ambient layers (one AudioStreamPlayer per phase tier)
var _layer_safe: AudioStreamPlayer = null     # Phase 0: station chatter
var _layer_shimmer: AudioStreamPlayer = null   # Phase 1: thinning
var _layer_drift: AudioStreamPlayer = null     # Phase 2: low drone
var _layer_fracture: AudioStreamPlayer = null  # Phase 3: deep resonance
var _layer_void: AudioStreamPlayer = null      # Phase 4: near-silence clarity

# Fauna audio
var _fauna_pulse: AudioStreamPlayer = null     # Fauna proximity harmonic
var _fauna_phantom_timer: float = 0.0
const _FAUNA_PHANTOM_INTERVAL: float = 15.0    # Check for phantom play every 15s
const _FAUNA_PHANTOM_CHANCE: float = 0.20      # 20% chance of false fauna sound

# Current state
var _current_phase: int = -1
var _fauna_present: bool = false
var _poll_elapsed: float = 0.0
const _POLL_INTERVAL: float = 1.0  # Poll bridge every second

# Crossfade
var _fade_tween: Tween = null
const _FADE_DURATION: float = 3.0  # Slow crossfade between phase layers


func _ready() -> void:
	# Bake procedural tones for each layer.
	_layer_safe = _create_layer(_bake_noise_tone(200.0, 22050, 0.3), -20.0)
	_layer_shimmer = _create_layer(_bake_tone(80.0, 22050), -28.0)
	_layer_drift = _create_layer(_bake_tone(35.0, 22050), -22.0)
	_layer_fracture = _create_layer(_bake_tone(22.0, 22050), -18.0)
	_layer_void = _create_layer(_bake_tone(440.0, 22050), -36.0)  # High clarity tone

	# Fauna pulse: 200Hz warbling
	_fauna_pulse = _create_layer(_bake_tone(200.0, 22050), -30.0)
	_fauna_pulse.volume_db = -80.0  # Start silent

	# Start all layers (crossfade via volume)
	for layer in [_layer_safe, _layer_shimmer, _layer_drift, _layer_fracture, _layer_void, _fauna_pulse]:
		layer.volume_db = -80.0
		layer.play()

	# Default: safe layer audible
	_layer_safe.volume_db = -20.0
	_current_phase = 0


func _physics_process(delta: float) -> void:
	_poll_elapsed += delta
	if _poll_elapsed >= _POLL_INTERVAL:
		_poll_elapsed = 0.0
		_poll_dread_state()

	# Fauna phantom play timer
	_fauna_phantom_timer += delta
	if _fauna_phantom_timer >= _FAUNA_PHANTOM_INTERVAL:
		_fauna_phantom_timer = 0.0
		_try_phantom_fauna_play()


func _poll_dread_state() -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		return
	if not _bridge.has_method("GetDreadStateV0"):
		return

	var dread: Dictionary = _bridge.call("GetDreadStateV0")
	var phase: int = dread.get("phase", 0)

	if phase != _current_phase:
		_crossfade_to_phase(phase)
		_current_phase = phase

	# Fauna check
	if _bridge.has_method("GetLatticeFaunaV0"):
		var fauna: Array = _bridge.call("GetLatticeFaunaV0")
		var any_present: bool = false
		for f in fauna:
			if f is Dictionary and f.get("state", 0) == 1:  # Present state
				any_present = true
				break
		if any_present != _fauna_present:
			_fauna_present = any_present
			_crossfade_layer(_fauna_pulse, -12.0 if any_present else -80.0)


func _crossfade_to_phase(phase: int) -> void:
	if _fade_tween and _fade_tween.is_valid():
		_fade_tween.kill()
	_fade_tween = create_tween()
	_fade_tween.set_parallel(true)

	# Target volumes per phase
	var targets: Dictionary = {
		_layer_safe: -80.0,
		_layer_shimmer: -80.0,
		_layer_drift: -80.0,
		_layer_fracture: -80.0,
		_layer_void: -80.0,
	}

	match phase:
		0:
			targets[_layer_safe] = -20.0
		1:
			targets[_layer_safe] = -30.0
			targets[_layer_shimmer] = -24.0
		2:
			targets[_layer_shimmer] = -28.0
			targets[_layer_drift] = -20.0
		3:
			targets[_layer_drift] = -26.0
			targets[_layer_fracture] = -16.0
		4:  # Void: near-silence with high clarity tone
			targets[_layer_void] = -30.0

	for layer in targets:
		_fade_tween.tween_property(layer, "volume_db", targets[layer], _FADE_DURATION)


func _crossfade_layer(layer: AudioStreamPlayer, target_db: float) -> void:
	var tw := create_tween()
	tw.tween_property(layer, "volume_db", target_db, 1.5)


func _try_phantom_fauna_play() -> void:
	# Only play phantom sounds at Phase 2+ when no real fauna is present
	if _current_phase < 2 or _fauna_present:
		return
	if randf() < _FAUNA_PHANTOM_CHANCE:
		# Brief phantom pulse — fade in and back out
		var tw := create_tween()
		tw.tween_property(_fauna_pulse, "volume_db", -20.0, 0.5)
		tw.tween_property(_fauna_pulse, "volume_db", -80.0, 2.0)


func _create_layer(stream: AudioStreamWAV, vol_db: float) -> AudioStreamPlayer:
	var player := AudioStreamPlayer.new()
	player.bus = &"Ambient"
	player.volume_db = vol_db
	player.stream = stream
	add_child(player)
	return player


func _bake_tone(frequency: float, mix_rate: int) -> AudioStreamWAV:
	var period_samples: int = int(mix_rate / frequency)
	var num_periods: int = ceili(float(mix_rate) / float(period_samples))
	var total_samples: int = period_samples * num_periods
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	for i in total_samples:
		var phase_val: float = float(i) / float(period_samples)
		var sample: float = sin(phase_val * TAU) * 0.4
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


func _bake_noise_tone(frequency: float, mix_rate: int, noise_amount: float) -> AudioStreamWAV:
	## Bakes a tone with noise mixed in (simulates radio chatter/static).
	var period_samples: int = int(mix_rate / frequency)
	var num_periods: int = ceili(float(mix_rate) / float(period_samples))
	var total_samples: int = period_samples * num_periods
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	for i in total_samples:
		var phase_val: float = float(i) / float(period_samples)
		var tone: float = sin(phase_val * TAU) * (1.0 - noise_amount)
		var noise: float = (randf() * 2.0 - 1.0) * noise_amount
		var sample: float = (tone + noise) * 0.3
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
