extends Area3D
class_name GameStation

# MARKET DATA
var buy_prices = {"ore_iron": 50, "ore_gold": 100}

func _ready():
    monitoring = true
    monitorable = true
    body_entered.connect(_on_body_entered)

func _on_body_entered(body):
    if body.has_method("dock_at_station"):
        print("!!! DOCKING SEQUENCE !!!")
        
        # 1. Buy Cargo
        _execute_transaction(body)
        
        # 2. Open Menu
        body.dock_at_station()

func _execute_transaction(player):
    var manifest = player.cargo.duplicate()
    if manifest.is_empty(): return

    for item in manifest:
        if buy_prices.has(item):
            var qty = manifest[item]
            var total = qty * buy_prices[item]
            player.remove_cargo(item, qty)
            player.receive_payment(total)
