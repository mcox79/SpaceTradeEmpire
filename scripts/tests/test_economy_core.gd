extends Node

# TEST RUNNER
# Attach this to a lightweight scene to verify logic periodically.

func _ready():
    print("--- STARTING ECONOMY INTEGRATION TESTS ---")
    run_all_tests()
    print("--- TESTS COMPLETE ---")

func run_all_tests():
    test_affordability_check()
    test_cargo_limits()
    test_price_algorithm()

# -----------------------------------------------------
# TEST CASES
# -----------------------------------------------------

func test_affordability_check():
    var balance = 1000
    var price = 50
    
    var can_buy_10 = EconomyEngine.can_afford(balance, price, 10) # Cost 500
    var can_buy_25 = EconomyEngine.can_afford(balance, price, 25) # Cost 1250
    
    _assert(can_buy_10 == true, "Affordability: Should afford 10 items")
    _assert(can_buy_25 == false, "Affordability: Should NOT afford 25 items")

func test_cargo_limits():
    var max_vol = 100.0
    var current_vol = 90.0
    var item_vol = 2.0
    
    var can_fit_4 = EconomyEngine.has_cargo_space(current_vol, max_vol, item_vol, 4) # +8 = 98 (Fits)
    var can_fit_6 = EconomyEngine.has_cargo_space(current_vol, max_vol, item_vol, 6) # +12 = 102 (Fail)
    
    _assert(can_fit_4 == true, "Cargo: Should fit 4 units")
    _assert(can_fit_6 == false, "Cargo: Should NOT fit 6 units")

func test_price_algorithm():
    # Setup a mock item
    var iron = TradeGood.new()
    iron.base_price = 100
    
    # High Supply (Price should drop)
    var cheap_price = EconomyEngine.calculate_price(iron, 200, 100)
    # Low Supply (Price should rise)
    var high_price = EconomyEngine.calculate_price(iron, 50, 100)
    
    _assert(cheap_price < 100, "Economics: High supply should lower price (Got: %s)" % cheap_price)
    _assert(high_price > 100, "Economics: Low supply should raise price (Got: %s)" % high_price)

# -----------------------------------------------------
# ASSERTION HELPER
# -----------------------------------------------------
func _assert(condition: bool, message: String):
    if condition:
        print("[PASS] " + message)
    else:
        printerr("[FAIL] " + message)
