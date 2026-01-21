extends CanvasLayer

@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var cargo_label = $Control/Panel/VBoxContainer/CargoLabel

func _ready():
    # Find the player and connect to their signals
    var player = get_tree().get_first_node_in_group("Player")
    if player:
        player.credits_updated.connect(_on_credits_updated)
        player.cargo_updated.connect(_on_cargo_updated)
        
        # Initial Update
        _on_credits_updated(player.credits)
        _on_cargo_updated(player.cargo)
    else:
        print("ERROR: HUD could not find Player!")

func _on_credits_updated(amount):
    money_label.text = "CREDITS: $%s" % amount

func _on_cargo_updated(manifest):
    var text = "CARGO HOLD:\n"
    if manifest.is_empty():
        text += "- Empty -"
    else:
        for item in manifest:
            text += "- %s: %s\n" % [item, manifest[item]]
    cargo_label.text = text
