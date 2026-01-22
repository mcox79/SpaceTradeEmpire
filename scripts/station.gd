extends Area3D
class_name GameStation

# --- CONFIGURATION ---
@export var market_update_interval: float = 5.0
@export var fuel_cost_per_unit: int = 2

# --- MARKET DATA ---
var base_prices = { "ore_iron": 50, "ore_gold": 100 }
var current_prices = {}
var market_trend = { "ore_iron": 0, "ore_gold": 0 } # -1 Bear, 1 Bull

var timer: float = 0.0

func _ready():
    monitoring = true
    monitorable = true
    body_entered.connect(_on_body_entered)
    
    # Initialize Market
    for item in base_prices:
        current_prices[item] = base_prices[item]
    
    print("[STATION] COMMODITY EXCHANGE OPEN.")

func _process(delta):
    # Update Market every X seconds
    timer += delta
    if timer >= market_update_interval:
        timer = 0.0
        _fluctuate_market()

func _fluctuate_market():
    var rng = RandomNumberGenerator.new()
    rng.randomize()
    
    for item in base_prices:
        # 1. Shift Trend (10% chance)
        if rng.randf() < 0.1:
            market_trend[item] = rng.randi_range(-1, 1)
            
        # 2. Apply Volatility
        var volatility = rng.randf_range(-5, 5) + (market_trend[item] * 2)
        current_prices[item] += int(volatility)
        
        # 3. Safety Clamp (50% to 200% of base)
        var min_p = int(base_prices[item] * 0.5)
        var max_p = int(base_prices[item] * 2.0)
        current_prices[item] = clamp(current_prices[item], min_p, max_p)
    
    print("[MARKET] TICK: %s" % current_prices)

func _on_body_entered(body):
    if body.has_method("dock_at_station"):
        print("!!! DOCKING SEQUENCE INITIATED !!!")
        _refuel_ship(body)
        body.dock_at_station(self)

func _refuel_ship(player):
    if not player.has_method("get_fuel_status"): return
    
    var needed = player.max_fuel - player.fuel
    if needed > 1.0:
        var cost = int(needed * fuel_cost_per_unit)
        if player.credits >= cost:
            player.credits -= cost
            player.fuel = player.max_fuel
            player.receive_payment(0) # Force UI Update
            print("[STATION] FULL TANK: -$%s" % cost)
        else:
            # Partial Fill
            var units = int(player.credits / fuel_cost_per_unit)
            if units > 0:
                player.credits -= (units * fuel_cost_per_unit)
                player.fuel += units
                player.receive_payment(0)
                print("[STATION] PARTIAL FILL: +%s units" % units)

# API
func get_market_price(item_id: String) -> int:
    return current_prices.get(item_id, 0)

func sell_cargo(player, item_id, amount):
    var price = get_market_price(item_id)
    var total = price * amount
    player.receive_payment(total)
    print("[MARKET] SOLD %s x%s @ $%s" % [item_id, amount, price])
