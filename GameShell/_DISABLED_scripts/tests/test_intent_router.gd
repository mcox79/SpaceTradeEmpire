extends SceneTree

const Sim = preload("res://scripts/core/sim/sim.gd")
const Intents = preload("res://scripts/core/intents/intents.gd")

func _init():
	print("--- STARTING INTENT ROUTER TESTS ---")
	run_all_tests()
	print("--- TESTS COMPLETE ---")
	quit()

func run_all_tests():
	test_validation_rejects_empty_actor()
	test_apply_intent_processes_trade()

func test_validation_rejects_empty_actor():
	var sim = Sim.new(1)
	var bad_intent = Intents.Trade.new()
	bad_intent.actor_id = "" # Invalid state
	
	var result = sim.apply_intent(bad_intent)
	_assert(result.error == "MissingActorId", "Validation: Sim rejected Intent with missing Actor ID.")

func test_apply_intent_processes_trade():
	var sim = Sim.new(1)
	var trade_intent = Intents.Trade.new()
	trade_intent.actor_id = "player_1"
	trade_intent.item_id = "ore_iron"
	trade_intent.amount = 5
	trade_intent.is_buy = true
	
	var result = sim.apply_intent(trade_intent)
	_assert(result.error == null, "Routing: Sim successfully processed Trade Intent.")
	_assert(result.events.has("intent_processed"), "Routing: Sim emitted processing event.")

func _assert(condition: bool, message: String):
	if condition:
		print("[PASS] " + message)
	else:
		printerr("[FAIL] " + message)