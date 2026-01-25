extends Node

const Sim = preload("res://scripts/core/sim/sim.gd")
const PlayerScene = preload("res://scenes/player.tscn")
const HudScene = preload("res://scenes/ui_hud.tscn")

var sim: Sim
var _tick_timer: Timer

func _ready():
	print("SUCCESS: Global Game Manager initialized.")
	
	# 1. Boot the Headless Sim
	sim = Sim.new()
	
	# 2. Boot the Deterministic Clock
	_tick_timer = Timer.new()
	_tick_timer.name = "SimClock"
	_tick_timer.wait_time = sim.TICK_INTERVAL
	_tick_timer.autostart = true
	_tick_timer.timeout.connect(_on_tick)
	add_child(_tick_timer)
	
	# 3. BOOTSTRAP THE PLAYER (New)
	# We dynamically inject the player into the world at the first star's location.
	var hud = HudScene.instantiate()
	var player = PlayerScene.instantiate()
	
	add_child(hud)
	add_child(player)
	
	# Teleport player to the first star in the Simulation
	if sim.galaxy_map.stars.size() > 0:
		player.global_position = sim.galaxy_map.stars[0].pos
		print("SUCCESS: Player spawned at Star 0.")

func _on_tick():
	if sim:
		sim.advance()
