extends Node

# MASTER SIGNAL BUS & RUNTIME BRIDGE
# This Autoload connects the headless Sim to the visual Spawners.
const Sim = preload("res://scripts/core/sim/sim.gd")

var sim: Sim
var _tick_timer: Timer

func _ready():
	print("SUCCESS: Global Game Manager initialized. Instantiating Headless Sim...")
	
	# 1. Instantiate the canonical Sim (Pure Data, NOT a Node)
	sim = Sim.new()
	
	# 2. The Deterministic Clock
	# GameManager is a Node, so it owns the Timer and pumps the headless Sim.
	_tick_timer = Timer.new()
	_tick_timer.name = "SimClock"
	_tick_timer.wait_time = sim.TICK_INTERVAL
	_tick_timer.autostart = true
	_tick_timer.timeout.connect(_on_tick)
	add_child(_tick_timer)
	
	await get_tree().process_frame
	print("SUCCESS: Runtime Bridge active.")

func _on_tick():
	if sim:
		sim.advance()
