extends SceneTree

const Generator = preload("res://scripts/core/sim/galaxy_generator.gd")

func _init():
	print("--- STARTING UNIVERSE VALIDATOR HARNESS ---")
	run_all_tests()
	print("--- VALIDATOR COMPLETE ---")
	quit()

func run_all_tests():
	print("[TEST] Generating 1,000 distinct galaxies...")
	var passes = 0
	var failures = 0
	
	for i in range(1000):
		var gen = Generator.new(i)
		var graph = gen.generate_topology(5) # 5 Regions
		
		# VALIDATION CONSTRAINTS [cite: 70-76, 603-608]
		# 1. Must have multiple chokepoints.
		# 2. Inter-region travel must be sparse.
		if graph.chokepoints >= 2 and graph.edges < (graph.nodes * 4):
			passes += 1
		else:
			failures += 1
			printerr("Seed %s FAILED constraints: %s" % [i, graph])
	
	_assert(failures == 0, "Universe Validator: 1,000/1,000 galaxies meet strategic geography constraints.")

func _assert(condition: bool, message: String):
	if condition:
		print("[PASS] " + message)
	else:
		printerr("[FAIL] " + message)