extends Node

# Explicit Preloads for Headless Stability
const Sector = preload("res://scripts/resources/sector.gd")
const GalaxyGraph = preload("res://scripts/core/galaxy_graph.gd")

func _ready():
	print("--- STARTING GALAXY NAVIGATION TESTS ---")
	run_tests()
	print("--- GALAXY TESTS COMPLETE ---")
	get_tree().quit()

func run_tests():
	# SETUP: Create a Map: A <-> B <-> C
	var graph = GalaxyGraph.new()
	
	var s1 = Sector.new(); s1.id = "A"; graph.add_sector(s1)
	var s2 = Sector.new(); s2.id = "B"; graph.add_sector(s2)
	var s3 = Sector.new(); s3.id = "C"; graph.add_sector(s3)
	
	graph.connect_sectors("A", "B")
	graph.connect_sectors("B", "C")
	
	# TEST 1: Direct Neighbor
	var route_ab = graph.get_route("A", "B")
	_assert(route_ab.size() == 2, "Path A->B should be 2 steps. Got: %s" % str(route_ab))
	
	# TEST 2: Multi-Jump
	var route_ac = graph.get_route("A", "C")
	_assert(route_ac.size() == 3, "Path A->C should be 3 steps (A,B,C). Got: %s" % str(route_ac))
	
	# TEST 3: Invalid Path (Isolated Node)
	var s4 = Sector.new(); s4.id = "D"; graph.add_sector(s4) # D is alone
	var route_ad = graph.get_route("A", "D")
	_assert(route_ad.size() == 0, "Path A->D should be empty. Got: %s" % str(route_ad))

func _assert(condition: bool, message: String):
	if condition:
		print("[PASS] " + message)
	else:
		printerr("[FAIL] " + message)
