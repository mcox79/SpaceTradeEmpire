extends Node

const Sim = preload("res://scripts/core/sim/sim.gd")
const Intents = preload("res://scripts/core/intents/intents.gd")

var sim: Sim

func _ready():
	# Initialize the Sim with a fixed seed for deterministic testing
	sim = Sim.new(12345)
	print("[GAME_MANAGER] Headless Sim Online.")

func _physics_process(_delta):
	# In a live game, this might only advance once per second.
	# For now, we tick it every physics frame.
	sim.advance(1)

func send_intent(intent: Intents.Intent):
	var result = sim.apply_intent(intent)
	if result.error:
		printerr("[GAME_MANAGER] Intent Rejected: ", result.error)
	else:
		print("[GAME_MANAGER] Intent Processed.")