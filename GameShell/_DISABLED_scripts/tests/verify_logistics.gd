extends MainLoop
const Sim = preload('res://scripts/core/sim/sim.gd')
func _initialize():
	print('--- PHASE 4 LOGISTICS VERIFICATION ---')
	var sim = Sim.new()
	var miner = sim.active_markets[sim.galaxy_map.stars[0].id]
	var refinery = sim.active_markets[sim.galaxy_map.stars[1].id]

	print('Starting Sim. Miner: ', miner.node_id, ' Refinery: ', refinery.node_id)
	print('Initial Ore at Miner: ', miner.inventory.get('ore_iron', 0))
	print('Initial Ore at Refinery: ', refinery.inventory.get('ore_iron', 0))

	# Run for 200 ticks to allow for mining -> travel -> delivery
	var delivered = false
	for i in range(200):
		sim.advance()
		if refinery.inventory.get('ore_iron', 0) > 0:
			print('SUCCESS: Ore Delivered at tick ', i)
			print('Refinery Inventory: ', refinery.inventory['ore_iron'])
			delivered = true
			break

	if delivered:
		print('--- PHASE 4 SUCCESS ---')
	else:
		print('FAIL: Logistics timed out. No ore delivered.')
		print('Miner Stock: ', miner.inventory.get('ore_iron', 0))
	return false # Exit