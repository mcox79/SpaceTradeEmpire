# scripts/audio/action_sfx.gd
# GATE.T64.AUDIO.ACTION_SFX.001: Placeholder action SFX via AudioStreamGenerator.
# Register as autoload "ActionSfx" in project.godot.
# Generates short sine-burst WAVs for buy/sell/dock/undock/install actions.
# Replace with real audio stems when available.
extends Node

var _sfx_bus: StringName = &"SFX"
var _cache: Dictionary = {}  # name -> AudioStreamWAV

# SFX definitions: { name: { freq, duration, volume, type } }
const SFX_DEFS := {
	"buy": {"freq": 880.0, "duration": 0.12, "volume": -25.0, "type": "sine"},
	"sell": {"freq": 660.0, "duration": 0.18, "volume": -22.0, "type": "sine"},
	"sell_reward": {"freq": 440.0, "duration": 0.35, "volume": -20.0, "type": "chord"},
	"dock_clamp": {"freq": 120.0, "duration": 0.25, "volume": -20.0, "type": "noise_burst"},
	"undock_spool": {"freq": 200.0, "duration": 0.4, "volume": -22.0, "type": "sweep_up"},
	"module_install": {"freq": 300.0, "duration": 0.3, "volume": -20.0, "type": "mechanical"},
}

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	var bus_idx := AudioServer.get_bus_index("SFX")
	if bus_idx >= 0:
		_sfx_bus = &"SFX"
	else:
		_sfx_bus = &"Master"
	# Pre-generate all SFX streams.
	for sfx_name in SFX_DEFS:
		_cache[sfx_name] = _generate_stream(sfx_name)


## Play a named SFX. No-op if name unknown.
func play_sfx(sfx_name: String) -> void:
	if not _cache.has(sfx_name):
		return
	var player := AudioStreamPlayer.new()
	player.bus = _sfx_bus
	player.stream = _cache[sfx_name]
	player.volume_db = SFX_DEFS[sfx_name]["volume"]
	add_child(player)
	player.play()
	player.finished.connect(player.queue_free)


## Play buy SFX. Called from hero_trade_menu.gd on purchase.
func play_buy() -> void:
	play_sfx("buy")


## Play sell SFX. If profit > threshold, play reward chord instead.
func play_sell(profit_cr: int = 0) -> void:
	if profit_cr >= 1000:
		play_sfx("sell_reward")
	else:
		play_sfx("sell")


## Play dock clamp SFX. Called on DOCK_ENTER.
func play_dock() -> void:
	play_sfx("dock_clamp")


## Play undock spool-up SFX.
func play_undock() -> void:
	play_sfx("undock_spool")


## Play module install SFX + emission pulse on ship mesh.
func play_module_install(ship_mesh: MeshInstance3D = null) -> void:
	play_sfx("module_install")
	# Emission pulse: brief bright flash on ship mesh.
	if ship_mesh != null and is_instance_valid(ship_mesh):
		var mat := ship_mesh.get_active_material(0)
		if mat is StandardMaterial3D:
			var orig_energy: float = mat.emission_energy_multiplier
			mat.emission_energy_multiplier = orig_energy + 3.0
			var tw := create_tween()
			tw.tween_property(mat, "emission_energy_multiplier", orig_energy, 0.5)


func _generate_stream(sfx_name: String) -> AudioStreamWAV:
	var def: Dictionary = SFX_DEFS[sfx_name]
	var freq: float = def["freq"]
	var dur: float = def["duration"]
	var sfx_type: String = def["type"]
	var sample_rate := 22050
	var num_samples := int(sample_rate * dur)
	var data := PackedByteArray()
	data.resize(num_samples)

	for s in num_samples:
		var t: float = float(s) / float(sample_rate)
		var norm_t: float = float(s) / float(num_samples)
		# Envelope: quick attack (5%), sustain, fade last 30%.
		var env: float = 1.0
		if norm_t < 0.05:
			env = norm_t / 0.05
		elif norm_t > 0.7:
			env = (1.0 - norm_t) / 0.3
		var val: float = 0.0
		match sfx_type:
			"sine":
				val = sin(2.0 * PI * freq * t) * env
			"chord":
				# Major chord: root + major third + fifth.
				val = (sin(2.0 * PI * freq * t)
					+ 0.6 * sin(2.0 * PI * freq * 1.25 * t)
					+ 0.4 * sin(2.0 * PI * freq * 1.5 * t)) * env * 0.5
			"noise_burst":
				# Low rumble + noise.
				var tone: float = sin(2.0 * PI * freq * t)
				var noise: float = randf_range(-1.0, 1.0)
				val = (tone * 0.6 + noise * 0.4) * env
			"sweep_up":
				# Frequency sweep from freq to freq*3 over duration.
				var sweep_freq: float = freq * (1.0 + 2.0 * norm_t)
				val = sin(2.0 * PI * sweep_freq * t) * env
			"mechanical":
				# Chunky click: two quick tones + noise burst.
				if norm_t < 0.3:
					val = sin(2.0 * PI * freq * t) * env
				elif norm_t < 0.5:
					val = sin(2.0 * PI * freq * 1.5 * t) * env * 0.8
				else:
					val = randf_range(-0.5, 0.5) * env * 0.6
		data[s] = int(clampf(val * 40.0 + 128.0, 0.0, 255.0))

	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_8_BITS
	stream.mix_rate = sample_rate
	stream.data = data
	stream.loop_mode = AudioStreamWAV.LOOP_DISABLED
	return stream
