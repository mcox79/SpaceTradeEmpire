extends Node

# Explicit loads for headless stability
const EconomyEngine = preload("res://scripts/core/economy_engine.gd")
const TradeGood = preload("res://scripts/resources/trade_good.gd")

func _ready():
	print("--- STARTING ECONOMY INTEGRATION TESTS ---")
	run_all_tests()
	print("--- TESTS COMPLETE ---")
	get_tree().quit() # Auto-close on success

func run_all_tests():
	test_affordability_check()
	test_cargo_limits()
	test_price_algorithm()

func test_affordability_check():
	var balance = 1000
	var price = 50
	var can_buy_10 = EconomyEngine.can_afford(balance, price, 10)
	var can_buy_25 = EconomyEngine.can_afford(balance, price, 25)
	_assert(can_buy_10 == true, "Affordability: Should afford 10 items")
	_assert(can_buy_25 == false, "Affordability: Should NOT afford 25 items")

func test_cargo_limits():
	var max_vol = 100.0
	var current_vol = 90.0
	var item_vol = 2.0
	var can_fit_4 = EconomyEngine.has_cargo_space(current_vol, max_vol, item_vol, 4)
	var can_fit_6 = EconomyEngine.has_cargo_space(current_vol, max_vol, item_vol, 6)
	_assert(can_fit_4 == true, "Cargo: Should fit 4 units")
	_assert(can_fit_6 == false, "Cargo: Should NOT fit 6 units")

func test_price_algorithm():
	# Instantiate the Resource manually
	var iron = TradeGood.new()
	iron.base_price = 100
	
	# We pass the object. The Engine treats it as a generic Resource.
	var cheap_price = EconomyEngine.calculate_price(iron, 200, 100)
	var high_price = EconomyEngine.calculate_price(iron, 50, 100)
	
	_assert(cheap_price < 100, "Economics: High supply should lower price (Got: %s)" % cheap_price)
	_assert(high_price > 100, "Economics: Low supply should raise price (Got: %s)" % high_price)

func _assert(condition: bool, message: String):
	if condition:
		print("[PASS] " + message)
	else:
		printerr("[FAIL] " + message)
