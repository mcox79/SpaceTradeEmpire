extends Node

const Sim = preload("res://scripts/core/sim/sim.gd")

func _ready():
	print("--- STARTING HEADLESS SIM & HEAT INTEGRATION TESTS ---")
	
	# Instantiate the pure data Sim
	var sim = Sim.new()
	
	# Pump the Sim manually to generate heat
	for i in range(10):
		sim.advance()
	
	print("--- TESTS COMPLETE ---")
	get_tree().quit()
