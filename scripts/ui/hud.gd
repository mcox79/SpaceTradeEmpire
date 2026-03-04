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

# GATE.S1.SAVE_UI.PAUSE_MENU.001: pause menu overlay
var _pause_panel: Control = null
# GATE.S1.SAVE_UI.SLOTS.001: save slot labels for metadata display
var _slot_labels: Array = []

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

	# GATE.S1.SAVE_UI.PAUSE_MENU.001: pause menu (hidden until Escape pressed)
	_pause_panel = Control.new()
	_pause_panel.name = "PauseMenuPanel"
	_pause_panel.visible = false
	_pause_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	# process_mode ALWAYS so pause menu works while tree is paused
	_pause_panel.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(_pause_panel)

	var pause_bg := ColorRect.new()
	pause_bg.color = Color(0.0, 0.0, 0.0, 0.6)
	pause_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	_pause_panel.add_child(pause_bg)

	var pause_vbox := VBoxContainer.new()
	pause_vbox.set_anchors_preset(Control.PRESET_CENTER)
	pause_vbox.offset_left = -120
	pause_vbox.offset_right = 120
	pause_vbox.offset_top = -150
	pause_vbox.offset_bottom = 150
	pause_vbox.add_theme_constant_override("separation", 12)
	_pause_panel.add_child(pause_vbox)

	var pause_title := Label.new()
	pause_title.text = "PAUSED"
	pause_title.add_theme_font_size_override("font_size", 48)
	pause_title.add_theme_color_override("font_color", Color.WHITE)
	pause_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	pause_vbox.add_child(pause_title)

	var resume_btn := Button.new()
	resume_btn.text = "Resume"
	resume_btn.pressed.connect(_on_resume_pressed)
	resume_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	pause_vbox.add_child(resume_btn)

	# GATE.S1.SAVE_UI.SLOTS.001: save/load slot buttons
	for slot_idx in range(1, 4):
		var save_btn := Button.new()
		save_btn.name = "SaveSlot%d" % slot_idx
		save_btn.text = "Save Slot %d" % slot_idx
		save_btn.pressed.connect(_on_save_slot_pressed.bind(slot_idx))
		save_btn.process_mode = Node.PROCESS_MODE_ALWAYS
		pause_vbox.add_child(save_btn)

		var slot_lbl := Label.new()
		slot_lbl.name = "SlotLabel%d" % slot_idx
		slot_lbl.text = ""
		slot_lbl.add_theme_font_size_override("font_size", 12)
		slot_lbl.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7, 1.0))
		slot_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		pause_vbox.add_child(slot_lbl)
		_slot_labels.append(slot_lbl)

		var load_btn := Button.new()
		load_btn.name = "LoadSlot%d" % slot_idx
		load_btn.text = "Load Slot %d" % slot_idx
		load_btn.pressed.connect(_on_load_slot_pressed.bind(slot_idx))
		load_btn.process_mode = Node.PROCESS_MODE_ALWAYS
		pause_vbox.add_child(load_btn)

	var quit_btn := Button.new()
	quit_btn.text = "Quit"
	quit_btn.pressed.connect(_on_quit_pressed)
	quit_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	pause_vbox.add_child(quit_btn)

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

# GATE.S1.SAVE_UI.PAUSE_MENU.001: toggle pause overlay
func toggle_pause_menu_v0(show: bool) -> void:
	if _pause_panel == null:
		return
	_pause_panel.visible = show
	if show:
		_refresh_slot_labels_v0()
	print("UUIR|PAUSE_MENU|" + ("SHOW" if show else "HIDE"))

func _on_resume_pressed() -> void:
	var gm = get_tree().root.find_child("GameManager", true, false)
	if gm and gm.has_method("_toggle_pause_v0"):
		gm.call("_toggle_pause_v0")

func _on_quit_pressed() -> void:
	get_tree().quit()

# GATE.S1.SAVE_UI.SLOTS.001: save/load slot handlers
func _on_save_slot_pressed(slot: int) -> void:
	if _bridge == null:
		return
	if _bridge.has_method("SetActiveSaveSlotV0"):
		_bridge.call("SetActiveSaveSlotV0", slot)
	if _bridge.has_method("RequestSave"):
		_bridge.call("RequestSave")
	# Refresh labels after a short delay to allow save to complete
	await get_tree().create_timer(0.5).timeout
	_refresh_slot_labels_v0()
	print("UUIR|SAVE_SLOT|" + str(slot))

func _on_load_slot_pressed(slot: int) -> void:
	if _bridge == null:
		return
	if _bridge.has_method("SetActiveSaveSlotV0"):
		_bridge.call("SetActiveSaveSlotV0", slot)
	if _bridge.has_method("RequestLoad"):
		_bridge.call("RequestLoad")
	# Unpause and hide menu after load
	var gm = get_tree().root.find_child("GameManager", true, false)
	if gm and gm.has_method("_toggle_pause_v0"):
		gm.call("_toggle_pause_v0")
	print("UUIR|LOAD_SLOT|" + str(slot))

func _refresh_slot_labels_v0() -> void:
	if _bridge == null or not _bridge.has_method("GetSaveSlotMetadataV0"):
		return
	for i in range(_slot_labels.size()):
		var slot := i + 1
		var meta: Dictionary = _bridge.call("GetSaveSlotMetadataV0", slot)
		if meta.get("exists", false):
			_slot_labels[i].text = "%s | Credits: %s | %s" % [
				str(meta.get("timestamp", "")),
				str(meta.get("credits", 0)),
				str(meta.get("system_name", ""))
			]
		else:
			_slot_labels[i].text = "[Empty]"
