# scripts/ui/mission_journal_panel.gd
# GATE.X.UI_POLISH.MISSION_JOURNAL.001: Active mission journal panel (J key).
# Shows accepted mission(s) with step progress, objectives, rewards preview, abandon button.
# Reads from SimBridge.Mission queries.
extends PanelContainer

var _bridge = null
var _content_container: VBoxContainer = null
var _no_mission_label: Label = null
var _title_label: Label = null
var _objective_label: Label = null
var _progress_label: Label = null
var _rewards_label: Label = null
var _step_list: VBoxContainer = null
var _abandon_btn: Button = null

func _ready() -> void:
	name = "MissionJournalPanel"
	visible = false
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -300
	offset_right = 300
	offset_top = -240
	offset_bottom = 240
	custom_minimum_size = Vector2(600, 480)
	var style := UITheme.make_panel_standard()
	add_theme_stylebox_override("panel", style)

	var root_vbox := VBoxContainer.new()
	root_vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	add_child(root_vbox)

	# Header row.
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_LG)
	root_vbox.add_child(header)

	var header_title := Label.new()
	header_title.text = "MISSION JOURNAL"
	header_title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	header_title.add_theme_color_override("font_color", UITheme.CYAN)
	header_title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(header_title)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.pressed.connect(func(): toggle_v0())
	header.add_child(close_btn)

	# Separator.
	root_vbox.add_child(HSeparator.new())

	# No mission label (shown when no active mission).
	_no_mission_label = Label.new()
	_no_mission_label.text = "No active mission. Accept a mission at a station dock."
	_no_mission_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_no_mission_label.add_theme_color_override("font_color", UITheme.GOLD)
	_no_mission_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root_vbox.add_child(_no_mission_label)

	# Content container (visible when mission is active).
	_content_container = VBoxContainer.new()
	_content_container.add_theme_constant_override("separation", UITheme.SPACE_MD)
	_content_container.visible = false
	root_vbox.add_child(_content_container)

	# Mission title.
	_title_label = Label.new()
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_title_label.add_theme_color_override("font_color", UITheme.GOLD)
	_content_container.add_child(_title_label)

	# Current objective.
	_objective_label = Label.new()
	_objective_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_objective_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_content_container.add_child(_objective_label)

	# Progress (step X of Y).
	_progress_label = Label.new()
	_progress_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_content_container.add_child(_progress_label)

	# Separator before steps.
	_content_container.add_child(HSeparator.new())

	# Step list (scrollable).
	var step_scroll := ScrollContainer.new()
	step_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_content_container.add_child(step_scroll)

	_step_list = VBoxContainer.new()
	_step_list.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_step_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	step_scroll.add_child(_step_list)

	# Rewards preview.
	_rewards_label = Label.new()
	_rewards_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_rewards_label.add_theme_color_override("font_color", UITheme.GOLD)
	_content_container.add_child(_rewards_label)

	# Abandon button.
	_abandon_btn = Button.new()
	_abandon_btn.text = "ABANDON MISSION"
	_abandon_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_abandon_btn.pressed.connect(_on_abandon_pressed)
	_content_container.add_child(_abandon_btn)

func toggle_v0() -> void:
	visible = not visible
	if visible:
		_refresh()

func _refresh() -> void:
	if _bridge == null:
		var gm = get_tree().root.get_node_or_null("GameManager")
		if gm and gm.has_method("get_bridge"):
			_bridge = gm.get_bridge()
		if _bridge == null:
			_bridge = get_tree().root.get_node_or_null("SimBridge")
	if _bridge == null:
		return

	var mission = _bridge.GetActiveMissionV0()
	var mission_id = str(mission.get("mission_id", ""))

	if mission_id == "":
		_no_mission_label.visible = true
		_content_container.visible = false
		return

	_no_mission_label.visible = false
	_content_container.visible = true

	# Title.
	_title_label.text = str(mission.get("title", "Unknown Mission"))

	# Objective.
	_objective_label.text = "Objective: " + str(mission.get("objective_text", ""))

	# Progress.
	var current = int(mission.get("current_step", 0))
	var total = int(mission.get("total_steps", 0))
	_progress_label.text = "Step %d of %d" % [current + 1, total]

	# Step list.
	for child in _step_list.get_children():
		child.queue_free()

	# Rewards preview.
	var preview = _bridge.GetMissionRewardsPreviewV0(mission_id)
	var credit_reward = int(preview.get("credit_reward", 0))
	_rewards_label.text = "Reward: %d credits" % credit_reward

func _on_abandon_pressed() -> void:
	if _bridge == null:
		return
	if _bridge.has_method("AbandonMissionV0"):
		_bridge.AbandonMissionV0()
	_refresh()
