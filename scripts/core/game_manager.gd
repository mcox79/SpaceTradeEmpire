extends Node

# MASTER SIGNAL BUS & RUNTIME BRIDGE
# This Autoload connects the headless Sim to the visual Spawners.

const Sim = preload("res://scripts/core/sim/sim.gd")
var sim: Sim

func _ready():
	print("SUCCESS: Global Game Manager initialized. Instantiating Headless Sim...")
	
	# 1. Instantiate the canonical Sim
	sim = Sim.new()
	add_child(sim)
	
	# 2. Wait one frame so the View Layer can read the generated map.
	await get_tree().process_frame
	print("SUCCESS: Runtime Bridge active.")

