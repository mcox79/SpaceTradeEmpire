extends CanvasLayer

@onready var _credits_label: Label = $CreditsLabel
@onready var _cargo_label: Label = $CargoLabel
@onready var _node_label: Label = $NodeLabel
@onready var _state_label: Label = $StateLabel
@onready var _hull_bar: ProgressBar = $HullBar
@onready var _shield_bar: ProgressBar = $ShieldBar
@onready var _hull_label: Label = $HullLabel
@onready var _shield_label: Label = $ShieldLabel

var _bridge = null
var _combat_label: Label = null

# GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001: game over overlay
var _game_over_panel: Control = null
var _game_over_label: Label = null
var _restart_label: Label = null

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")
	_combat_label = Label.new()
	_combat_label.name = "CombatLabel"
	_combat_label.text = ""
	_combat_label.add_theme_color_override("font_color", Color.RED)
	_combat_label.position = Vector2(10, 160)
	add_child(_combat_label)

	# Build game over overlay (hidden until player dies)
	_game_over_panel = Control.new()
	_game_over_panel.name = "GameOverPanel"
	_game_over_panel.visible = false
	_game_over_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_game_over_panel)

	_game_over_label = Label.new()
	_game_over_label.name = "GameOverLabel"
	_game_over_label.text = "GAME OVER"
	_game_over_label.add_theme_font_size_override("font_size", 72)
	_game_over_label.add_theme_color_override("font_color", Color(1.0, 0.15, 0.15, 1.0))
	_game_over_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_game_over_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_game_over_label.set_anchors_preset(Control.PRESET_FULL_RECT)
	_game_over_panel.add_child(_game_over_label)

	_restart_label = Label.new()
	_restart_label.name = "RestartLabel"
	_restart_label.text = "Press R to Restart"
	_restart_label.add_theme_font_size_override("font_size", 32)
	_restart_label.add_theme_color_override("font_color", Color(1.0, 1.0, 1.0, 1.0))
	_restart_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_restart_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_restart_label.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_restart_label.offset_top = 100
	_game_over_panel.add_child(_restart_label)

func show_game_over_v0() -> void:
	if _game_over_panel != null:
		_game_over_panel.visible = true
	print("UUIR|GAME_OVER_SHOWN")

func _physics_process(_delta: float) -> void:
	if _bridge == null:
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_credits_label.text = "Credits: " + str(ps.get("credits", 0))
	_cargo_label.text = "Cargo: " + str(ps.get("cargo_count", 0))
	_node_label.text = "System: " + str(ps.get("current_node_id", ""))
	_state_label.text = "State: " + str(ps.get("ship_state_token", ""))

	# HP bars: hull (red/green) and shield (blue)
	if _bridge.has_method("GetFleetCombatHpV0"):
		var hp: Dictionary = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		var hull: int = hp.get("hull", 0)
		var hull_max: int = hp.get("hull_max", 0)
		var shield: int = hp.get("shield", 0)
		var shield_max: int = hp.get("shield_max", 0)

		if hull_max > 0:
			_hull_bar.max_value = hull_max
			_hull_bar.value = hull
			_hull_label.text = "Hull: " + str(hull) + " / " + str(hull_max)
			_hull_bar.visible = true
			_hull_label.visible = true
		else:
			_hull_bar.visible = false
			_hull_label.visible = false

		if shield_max > 0:
			_shield_bar.max_value = shield_max
			_shield_bar.value = shield
			_shield_label.text = "Shield: " + str(shield) + " / " + str(shield_max)
			_shield_bar.visible = true
			_shield_label.visible = true
		else:
			_shield_bar.visible = false
			_shield_label.visible = false

		# Legacy text label (hidden when bars are shown)
		_combat_label.text = ""
