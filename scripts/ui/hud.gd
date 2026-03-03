extends CanvasLayer

@onready var _credits_label: Label = $CreditsLabel
@onready var _cargo_label: Label = $CargoLabel
@onready var _node_label: Label = $NodeLabel
@onready var _state_label: Label = $StateLabel

var _bridge = null

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

func _physics_process(_delta: float) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	_cargo_label.text = "Cargo: " + str(ps.get("cargo_count", 0))
	_node_label.text = "System: " + str(ps.get("current_node_id", ""))
	_state_label.text = "State: " + str(ps.get("ship_state_token", ""))
