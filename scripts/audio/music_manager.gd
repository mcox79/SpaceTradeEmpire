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
	_calm_player = _make_player(CALM_TRACK, CALM_DB)
	_combat_player = _make_player(COMBAT_TRACK, SILENT_DB)

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

func _check_hostiles_near() -> bool:
	var tree := get_tree()
	if tree == null:
		return false
	var hero: Node3D = null
	for node in tree.get_nodes_in_group("Player"):
		hero = node
		break
	if hero == null:
		return false
	for node in tree.get_nodes_in_group("FleetShip"):
		if is_instance_valid(node) and hero.global_position.distance_to(node.global_position) < HOSTILE_CHECK_RANGE:
			return true
	return false
