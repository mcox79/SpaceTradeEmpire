extends MainLoop

const Sim = preload('res://scripts/core/sim/sim.gd')

func _initialize():
	print('--- PHASE 3 ARCHITECTURE VERIFICATION ---')

	# 1. Initialize Simulation
	var sim = Sim.new()
	var map_size = sim.galaxy_map.stars.size()
	print('Galaxy Generated. Star Count: ', map_size)

	# 2. Verify Geographic Distribution (The Design Fix)
	# We expect: Node 0 = Miner, Node 1 = Refinery, Node 2 = Consumer
	var node0 = sim.active_markets[sim.galaxy_map.stars[0].id]
	var node1 = sim.active_markets[sim.galaxy_map.stars[1].id]

	if node0.industries.has('mining') and not node0.industries.has('refinery'):
		print('PASS: Node 0 is a Dedicated Miner.')
	else:
		print('FAIL: Node 0 configuration incorrect.')
		return quit(1)

	if node1.industries.has('refinery') and not node1.industries.has('mining'):
		print('PASS: Node 1 is a Dedicated Refinery.')
	else:
		print('FAIL: Node 1 configuration incorrect.')
		return quit(1)

	# 3. Verify Production Logic (The Engine Fix)
	# Run 4 ticks. Mining (Cost 4) should trigger once. Refinery (Cost 2) should trigger twice but FAIL due to no input.
	print('Simulating 4 Ticks...')
	for i in range(4):
		sim.advance()

	# Check Miner Output
	var ore = node0.inventory.get('ore_iron', 0)
	print('Node 0 Inventory (Ore): ', ore, ' (Expected: 5)')

	if ore == 5:
		print('PASS: Production Rules are ticking.')
	else:
		print('FAIL: Production halted.')
		return quit(1)

	# Check Refinery Starvation (Correct Behavior)
	var fuel = node1.inventory.get('fuel', 0)
	print('Node 1 Inventory (Fuel): ', fuel, ' (Expected: 0)')

	if fuel == 0:
		print('PASS: Refinery is correctly starved (Logistics Gap confirmed).')
	else:
		print('FAIL: Refinery produced fuel out of thin air!')
		return quit(1)

	print('--- PHASE 3 SUCCESS: ARCHITECTURE VALIDATED ---')
	return quit(0)

func quit(code):
	var os = OS
	os.exit_code = code
	return false # Stops the MainLoop