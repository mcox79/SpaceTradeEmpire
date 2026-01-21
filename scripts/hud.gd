extends CanvasLayer

@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var cargo_label = $Control/Panel/VBoxContainer/CargoLabel
@onready var shop_panel = $Control/ShopPanel
@onready var btn_speed = $Control/ShopPanel/VBoxContainer/BtnSpeed
@onready var btn_weapon = $Control/ShopPanel/VBoxContainer/BtnWeapon
@onready var btn_close = $Control/ShopPanel/VBoxContainer/BtnClose

var player_ref = null

func _ready():
    shop_panel.visible = false
    
    # Link Buttons
    btn_speed.pressed.connect(_on_buy_speed)
    btn_weapon.pressed.connect(_on_buy_weapon)
    btn_close.pressed.connect(_on_close_shop)

    # Find Player
    var player = get_tree().get_first_node_in_group("Player")
    if player:
        player_ref = player
        player.credits_updated.connect(_on_credits_updated)
        player.cargo_updated.connect(_on_cargo_updated)
        player.shop_toggled.connect(_on_shop_toggled)
        
        # Init
        _on_credits_updated(player.credits)
        _on_cargo_updated(player.cargo)

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

func _on_shop_toggled(is_open):
    shop_panel.visible = is_open
    if is_open:
        Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
    else:
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _on_buy_speed():
    if player_ref: player_ref.purchase_upgrade("speed", 200)

func _on_buy_weapon():
    if player_ref: player_ref.purchase_upgrade("weapon", 500)

func _on_close_shop():
    if player_ref: player_ref.undock()
