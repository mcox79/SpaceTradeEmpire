extends Control
const EpilogueData = preload("res://scripts/ui/epilogue_data.gd")
# GATE.S8.WIN.VICTORY_SCREEN.001 — Timed text card sequence for victory epilogue

@onready var _bg: ColorRect = $Background
@onready var _card_container: VBoxContainer = $VBoxContainer/CardArea
@onready var _title_label: Label = $VBoxContainer/CardArea/TitleLabel
@onready var _body_label: RichTextLabel = $VBoxContainer/CardArea/BodyLabel
@onready var _card_counter: Label = $VBoxContainer/CardArea/CardCounter
@onready var _stats_panel: Panel = $VBoxContainer/StatsPanel
@onready var _stats_label: Label = $VBoxContainer/StatsPanel/MarginContainer/StatsInner/StatsLabel
@onready var _menu_button: Button = $VBoxContainer/MainMenuButton
@onready var _skip_hint: Label = $VBoxContainer/CardArea/SkipHint

var _cards: Array = []
var _current_card: int = 0
var _victory_info: Dictionary = {}
var _animating: bool = false
var _skip_requested: bool = false

const FADE_DURATION: float = 1.0

func _ready() -> void:
	_menu_button.hide()
	_stats_panel.hide()

	var bridge = get_tree().root.get_node_or_null("SimBridge")
	if bridge == null:
		push_warning("VictoryScreen: SimBridge not found — using fallback data")
		_victory_info = {
			"chosen_path": "reinforce",
			"final_credits": 0,
			"final_tick": 0,
			"nodes_visited": 0,
			"ship_class": "unknown",
			"modules_installed": 0,
			"fragments_collected": 0,
			"captain_name": "Unknown",
			"haven_tier": 0,
			"revelation_count": 0,
			"missions_completed": 0,
		}
	else:
		_victory_info = bridge.call("GetVictoryInfoV0")

	var path_name: String = str(_victory_info.get("chosen_path", "reinforce")).to_lower()
	_cards = EpilogueData.get_path(path_name)

	if _cards.is_empty():
		push_error("VictoryScreen: no cards for path '%s'" % path_name)
		_show_stats_and_button()
		return

	modulate.a = 0.0
	var tween := create_tween()
	tween.tween_property(self, "modulate:a", 1.0, 0.5)
	await tween.finished

	_play_card_sequence()


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_accept") or event.is_action_pressed("ui_select") \
			or (event is InputEventMouseButton and event.pressed):
		if _animating:
			_skip_requested = true


func _play_card_sequence() -> void:
	for i in range(_cards.size()):
		_current_card = i
		await _show_card(_cards[i], i + 1, _cards.size())
		if not is_inside_tree():
			return
	_show_stats_and_button()


func _show_card(card: Dictionary, index: int, total: int) -> void:
	var title: String = card.get("title", "")
	var body: String = card.get("body", "")
	var hold: float = card.get("duration_secs", 6.0)

	_title_label.text = title
	_body_label.text = body
	_card_counter.text = "%d / %d" % [index, total]
	_card_counter.modulate.a = 0.4
	_skip_hint.modulate.a = 0.0

	_card_container.modulate.a = 0.0
	_card_container.show()

	# Fade in
	_animating = true
	_skip_requested = false
	var tween_in := create_tween()
	tween_in.tween_property(_card_container, "modulate:a", 1.0, FADE_DURATION)
	tween_in.parallel().tween_property(_skip_hint, "modulate:a", 0.5, FADE_DURATION * 2.0)
	await tween_in.finished

	if _skip_requested:
		_skip_requested = false
		_animating = false
		_card_container.modulate.a = 0.0
		return

	# Hold — poll for skip in small increments
	var elapsed: float = 0.0
	while elapsed < hold:
		await get_tree().create_timer(0.1).timeout
		elapsed += 0.1
		if _skip_requested:
			break

	_skip_requested = false

	# Fade out
	var tween_out := create_tween()
	tween_out.tween_property(_card_container, "modulate:a", 0.0, FADE_DURATION)
	await tween_out.finished
	_animating = false


func _show_stats_and_button() -> void:
	_card_container.hide()

	var credits: int = _victory_info.get("final_credits", 0)
	var ticks: int = _victory_info.get("final_tick", 0)
	var nodes: int = _victory_info.get("nodes_visited", 0)
	var ship: String = str(_victory_info.get("ship_class", "unknown")).capitalize()
	var modules: int = _victory_info.get("modules_installed", 0)
	var fragments: int = _victory_info.get("fragments_collected", 0)
	var captain: String = str(_victory_info.get("captain_name", "Unknown"))
	var haven_tier: int = _victory_info.get("haven_tier", 0)
	var missions: int = _victory_info.get("missions_completed", 0)
	var revelations: int = _victory_info.get("revelation_count", 0)

	var stats_lines: PackedStringArray = [
		"Captain          %s" % captain,
		"Ship Class       %s" % ship,
		"Modules Fitted   %d" % modules,
		"Credits          %s" % _format_credits(credits),
		"Nodes Visited    %d" % nodes,
		"Missions Done    %d" % missions,
		"Fragments        %d" % fragments,
		"Revelations      %d" % revelations,
		"Haven Tier       %d" % haven_tier,
		"Ticks Elapsed    %s" % _format_ticks(ticks),
	]

	_stats_label.text = "\n".join(stats_lines)

	_stats_panel.modulate.a = 0.0
	_stats_panel.show()
	_menu_button.modulate.a = 0.0
	_menu_button.show()

	var tween := create_tween()
	tween.tween_property(_stats_panel, "modulate:a", 1.0, 1.2)
	tween.parallel().tween_property(_menu_button, "modulate:a", 1.0, 1.2)
	await tween.finished

	_menu_button.grab_focus()


func _on_main_menu_button_pressed() -> void:
	var bridge = get_tree().root.get_node_or_null("SimBridge")
	if bridge != null:
		bridge.call("StopSimV0")

	var tween := create_tween()
	tween.tween_property(self, "modulate:a", 0.0, 0.6)
	await tween.finished

	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")


func _format_credits(amount: int) -> String:
	if amount >= 1_000_000:
		return "%.1fM cr" % (float(amount) / 1_000_000.0)
	if amount >= 1_000:
		return "%.1fK cr" % (float(amount) / 1_000.0)
	return "%d cr" % amount


func _format_ticks(ticks: int) -> String:
	# Approximate: 60 ticks ≈ 1 minute of sim time
	var minutes := ticks / 60
	if minutes >= 60:
		return "%dh %dm" % [minutes / 60, minutes % 60]
	return "%dm" % minutes
