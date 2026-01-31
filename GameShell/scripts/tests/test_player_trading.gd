extends Node

const Sim = preload("res://scripts/core/sim/sim.gd")
const PlayerState = preload("res://scripts/core/state/player_state.gd")
const PlayerInteractionManager = preload("res://scripts/core/player_interaction_manager.gd")

func _ready():
	print("[TEST] Starting Player Trading Validation...")
	var sim = Sim.new()
	var player = PlayerState.new()
	var manager = PlayerInteractionManager.new(player, sim)

	# Setup Market
	var star_id = sim.galaxy_map.stars[0].id
	var market = sim.active_markets[star_id]
	market.inventory["fuel"] = 100
	market.base_demand["fuel"] = 50

	# TEST 1: BUY
	print("Attempting BUY...")
	var success = manager.try_trade(star_id, "fuel", 10, true)
	if success and player.cargo.get("fuel") == 10:
		print("[SUCCESS] Player bought 10 fuel.")
	else:
		print("[FAILURE] Buy failed.")
		get_tree().quit(1)
		return

	# TEST 2: CHECK HEAT
	var heat = sim.info.get_node_heat(star_id)
	if heat > 0:
		print("[SUCCESS] Trade generated heat: " + str(heat))
	else:
		print("[FAILURE] No heat generated.")
		get_tree().quit(1)
		return

	print("[TEST] Trading Systems Verified.")
	get_tree().quit()