extends Node

const Sim = preload("res://scripts/core/sim/sim.gd")

func _ready():
	print("[TEST] Starting Logistics Chain Validation...")
	var sim = Sim.new()

	if sim.galaxy_map.stars.is_empty():
		print("[FAILURE] Map empty")
		get_tree().quit(1)
		return

	var target_star = sim.galaxy_map.stars[0].id
	var market = sim.active_markets[target_star]
	market.inventory["staples"] = 0
	market.base_demand["staples"] = 5
	print("[TEST] Induced shortage at " + target_star)

	sim.advance()
	sim.advance()

	if sim.active_fleets.is_empty():
		print("[FAILURE] No fleets")
		get_tree().quit(1)
		return

	var fleet = sim.active_fleets[0]
	if fleet.path.size() > 0:
		print("[SUCCESS] Fleet dispatched.")
	else:
		print("[FAILURE] No dispatch detected.")
		get_tree().quit(1)
		return

	get_tree().quit()