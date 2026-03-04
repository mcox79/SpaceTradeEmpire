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
	_combat_label.position = Vector2(10, 120)
	add_child(_combat_label)

func _physics_process(_delta: float) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	_cargo_label.text = "Cargo: " + str(ps.get("cargo_count", 0))
	_node_label.text = "System: " + str(ps.get("current_node_id", ""))
	_state_label.text = "State: " + str(ps.get("ship_state_token", ""))

	if _bridge.has_method("GetCombatStatusV0"):
		var cs: Dictionary = _bridge.call("GetCombatStatusV0")
		if cs.get("in_combat", false):
			var ph: int = cs.get("player_hull", 0)
			var ps2: int = cs.get("player_shield", 0)
			_combat_label.text = "COMBAT  Hull:" + str(ph) + " Shield:" + str(ps2)
		else:
			_combat_label.text = ""
