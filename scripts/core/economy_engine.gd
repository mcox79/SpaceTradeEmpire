class_name EconomyEngine

# CONSTANTS
const MINIMUM_PRICE = 1

# STATIC UTILITIES
# Calculates the current price of a good based on local supply
static func calculate_price(good: TradeGood, local_supply: int, base_demand: int) -> int:
    # MVP Algorithm: Simple Scarcity
    # If supply > demand, price drops.
    var scarcity_factor = float(base_demand) / max(1.0, float(local_supply))
    var final_price = int(good.base_price * scarcity_factor)
    return max(final_price, MINIMUM_PRICE)

# Validates if a transaction is mathematically possible
static func can_afford(wallet_balance: int, price_per_unit: int, quantity: int) -> bool:
    return wallet_balance >= (price_per_unit * quantity)

static func has_cargo_space(current_volume: float, max_volume: float, item_volume: float, quantity: int) -> bool:
    var required_space = item_volume * quantity
    return (current_volume + required_space) <= max_volume
