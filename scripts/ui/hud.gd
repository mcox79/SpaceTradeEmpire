extends CanvasLayer

@onready var _credits_label: Label = $CreditsLabel
@onready var _cargo_label: Label = $CargoLabel
@onready var _node_label: Label = $NodeLabel
@onready var _state_label: Label = $StateLabel

var _bridge = null
var _combat_label: Label = null

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")
	_combat_label = Label.new()
	_combat_label.name = "CombatLabel"
	_combat_label.text = ""
	_combat_label.add_theme_color_override("font_color", Color.RED)
	_combat_label.position = Vector2(10, 160)
	add_child(_combat_label)

func _physics_process(_delta: float) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	_cargo_label.text = "Cargo: " + str(ps.get("cargo_count", 0))
	_node_label.text = "System: " + str(ps.get("current_node_id", ""))
	_state_label.text = "State: " + str(ps.get("ship_state_token", ""))

	# Real-time HP display (always on when HP is initialized)
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull: int = hp.get("hull", 0)
		var hull_max: int = hp.get("hull_max", 0)
		var shield: int = hp.get("shield", 0)
		var shield_max: int = hp.get("shield_max", 0)
		if hull_max > 0:
			_combat_label.text = "Hull:" + str(hull) + "/" + str(hull_max) + "  Shield:" + str(shield) + "/" + str(shield_max)
		else:
			_combat_label.text = ""
