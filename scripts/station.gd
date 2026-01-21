extends Area3D
class_name GameStation

# MARKET DATA
var buy_prices = {
    "ore_iron": 50,
    "ore_gold": 100
}

func _ready():
    monitoring = true
    monitorable = true
    body_entered.connect(_on_body_entered)
    print("[STATION] MARKET OPEN. BUYING: Iron ($50), Gold ($100).")

func _on_body_entered(body):
    # Check if the body can receive money (i.e., is it the Player?)
    if body.has_method("receive_payment"):
        print("!!! DOCKING SUCCESSFUL: " + body.name + " !!!")
        _execute_transaction(body)

func _execute_transaction(player):
    var manifest = player.cargo.duplicate()
    
    if manifest.is_empty():
        print("[STATION] Cargo hold is empty. Nothing to buy.")
        return

    for item in manifest:
        if buy_prices.has(item):
            var qty = manifest[item]
            var price = buy_prices[item]
            var total = qty * price
            
            # The Swap
            player.remove_cargo(item, qty)
            player.receive_payment(total)
            print("[STATION] Purchased %s x%s for $%s." % [item, qty, total])
        else:
            print("[STATION] We do not buy " + item)
