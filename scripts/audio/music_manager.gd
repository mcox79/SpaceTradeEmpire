extends Node

# Background music manager — crossfades between calm and combat tracks.

const CALM_TRACK := preload("res://assets/audio/music/calm_ambient_01.ogg")
const COMBAT_TRACK := preload("res://assets/audio/music/combat_01.ogg")

# Volume-leveled: combat source is 3.2 dB hotter than calm, compensated here.
const CALM_DB := -28.0
const COMBAT_DB := -31.0
const SILENT_DB := -80.0

const FADE_IN_SEC := 1.0
const FADE_OUT_SEC := 1.5
const HOSTILE_CHECK_RANGE := 60.0

var _calm_player: AudioStreamPlayer = null
var _combat_player: AudioStreamPlayer = null
var _calm_tween: Tween = null
var _combat_tween: Tween = null
var _in_combat := false

func _ready() -> void:
	# Music must keep playing during tree pause (gate transit popup, pause menu).
	process_mode = Node.PROCESS_MODE_ALWAYS
	# GATE.S7.AUDIO_WIRING.BUS_WIRE.001: 5-layer audio bus setup.
	_setup_audio_buses_v0()
	_calm_player = _make_player(CALM_TRACK, CALM_DB)
	_combat_player = _make_player(COMBAT_TRACK, SILENT_DB)
	# Assign music players to Music bus.
	_calm_player.bus = &"Music"
	_combat_player.bus = &"Music"

func _make_player(track: AudioStream, vol_db: float) -> AudioStreamPlayer:
	var p := AudioStreamPlayer.new()
	p.bus = &"Master"
	p.volume_db = vol_db
	p.stream = track
	add_child(p)
	p.play()
	p.finished.connect(p.play)
	return p

func _process(_delta: float) -> void:
	var hostiles_near := _check_hostiles_near()
	if hostiles_near and not _in_combat:
		_in_combat = true
		_fade(_calm_player, _calm_tween, SILENT_DB, FADE_OUT_SEC)
		_fade(_combat_player, _combat_tween, COMBAT_DB, FADE_IN_SEC)
	elif not hostiles_near and _in_combat:
		_in_combat = false
		_fade(_combat_player, _combat_tween, SILENT_DB, FADE_OUT_SEC)
		_fade(_calm_player, _calm_tween, CALM_DB, FADE_IN_SEC)

func _fade(player: AudioStreamPlayer, tween: Tween, target_db: float, duration: float) -> void:
	if tween and tween.is_valid():
		tween.kill()
	var t := create_tween()
	t.tween_property(player, "volume_db", target_db, duration)\
		.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	# Store reference — can't assign to the parameter directly
	if player == _calm_player:
		_calm_tween = t
	else:
		_combat_tween = t

# GATE.S7.AUDIO_WIRING.BUS_WIRE.001: Create 5-layer audio bus hierarchy.
# Music, Ambient, SFX, UI, Alert — all send to Master.
# Ducking: Alert ducks all (compressor sidechain), SFX ducks Ambient.
func _setup_audio_buses_v0() -> void:
	var bus_names := ["Music", "Ambient", "SFX", "UI", "Alert"]
	for bus_name in bus_names:
		# Skip if bus already exists (e.g., from default_bus_layout.tres).
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
		# Only trigger combat music for actually hostile ships, not friendly traders/haulers.
		if node.get_meta("is_hostile", false):
			return true
	return false
