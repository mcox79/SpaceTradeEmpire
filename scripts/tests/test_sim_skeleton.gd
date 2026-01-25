extends Node

const Sim = preload("res://scripts/core/sim/sim.gd")

func _ready():
print("--- STARTING SIM SKELETON TESTS ---")
run_all_tests()
print("--- TESTS COMPLETE ---")
get_tree().quit()

func run_all_tests():
test_determinism()
test_rng_isolation()

func test_determinism():
var sim1 = Sim.new(42)
var sim2 = Sim.new(42)
_assert(sim1.state.get_snapshot_hash() == sim2.state.get_snapshot_hash(), "Determinism: Identical seeds produce identical hashes.")

func test_rng_isolation():
var sim = Sim.new(100)
var eco_rng = sim.rng.get_stream("economy")
var combat_rng = sim.rng.get_stream("combat")
var r1 = eco_rng.randf()
var r2 = combat_rng.randf()
_assert(r1 != r2, "RNG Isolation: Economy and Combat streams generate distinct values.")

func _assert(condition: bool, message: String):
if condition:
print("[PASS] " + message)
else:
printerr("[FAIL] " + message)