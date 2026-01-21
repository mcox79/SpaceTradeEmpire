extends RefCounted
class_name EconomyEngine

const MINIMUM_PRICE = 1

# Note: 'good' is typed as Resource to prevent headless dependency errors
static func calculate_price(good: Resource, local_supply: int, base_demand: int) -> int:
    # MVP Algorithm: Simple Scarcity
    # We access properties dynamically
    var base = good.get("base_price")
    if base == null:
        base = 10 # Fallback
        
    var scarcity_factor = float(base_demand) / max(1.0, float(local_supply))
    var final_price = int(base * scarcity_factor)
    return max(final_price, MINIMUM_PRICE)

static func can_afford(wallet_balance: int, price_per_unit: int, quantity: int) -> bool:
    return wallet_balance >= (price_per_unit * quantity)

static func has_cargo_space(current_volume: float, max_volume: float, item_volume: float, quantity: int) -> bool:
    var required_space = item_volume * quantity
    return (current_volume + required_space) <= max_volume
