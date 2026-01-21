extends CanvasLayer

# NODES
@onready var money_label = $Control/Panel/VBoxContainer/MoneyLabel
@onready var health_bar = $Control/Panel/VBoxContainer/HealthBar
@onready var cargo_label = $Control/Panel/VBoxContainer/CargoLabel

@onready var shop_panel = $Control/ShopPanel
@onready var btn_speed = $Control/ShopPanel/VBoxContainer/BtnSpeed
@onready var btn_weapon = $Control/ShopPanel/VBoxContainer/BtnWeapon
@onready var btn_close = $Control/ShopPanel/VBoxContainer/BtnClose

var player_ref = null

func _ready():
    shop_panel.visible = false
    
    # Wire Buttons
    btn_speed.pressed.connect(_on_buy_speed)
    btn_weapon.pressed.connect(_on_buy_weapon)
    btn_close.pressed.connect(_on_close_shop)

    # SEARCH FOR PLAYER (With Retry)
    _find_player()

func _find_player():
    var player = get_tree().get_first_node_in_group("Player")
    if player:
        print("[HUD] Player Signal Uplink Established.")
        player_ref = player
        player.credits_updated.connect(_on_credits_updated)
        player.cargo_updated.connect(_on_cargo_updated)
        player.health_updated.connect(_on_health_updated)
        player.shop_toggled.connect(_on_shop_toggled)
        
        # Force immediate update
        _on_credits_updated(player.credits)
        _on_cargo_updated(player.cargo)
        _on_health_updated(player.health, player.max_health)
    else:
        # If Player isn't ready, try again in 0.1 seconds
        print("[HUD] Searching for Player...")
        await get_tree().create_timer(0.1).timeout
        _find_player()

func _on_credits_updated(amount):
    money_label.text = "CREDITS: $%s" % amount

func _on_health_updated(current, max_val):
    if health_bar:
        health_bar.max_value = max_val
        health_bar.value = current
        
        # Color Logic (Green -> Red)
        var style = health_bar.get_theme_stylebox("fill")
        if style:
            # We must duplicate the style to avoid changing the original resource permanently
            var new_style = style.duplicate()
            if current <= (max_val * 0.3):
                new_style.bg_color = Color(0.9, 0.1, 0.1) # Red Critical
            else:
                new_style.bg_color = Color(0.2, 0.8, 0.2) # Green Good
            
            health_bar.add_theme_stylebox_override("fill", new_style)

func _on_cargo_updated(manifest):
    var text = "CARGO: "
    if manifest.is_empty():
        text += "Empty"
    else:
        for item in manifest:
            text += "%s(%s) " % [item.replace("ore_", "").capitalize(), manifest[item]]
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
