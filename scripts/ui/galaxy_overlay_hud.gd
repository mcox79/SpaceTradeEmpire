# GATE.S6.MAP_GALAXY.OVERLAY_SYS.001: Galaxy map overlay mode selector toolbar.
# Shown when galaxy overlay is open (Tab key). Mode buttons switch GalaxyView overlay coloring.
extends HBoxContainer

var _galaxy_view: Node3D = null

func _ready() -> void:
	visible = false
	# Find GalaxyView in the scene tree.
	_galaxy_view = get_tree().root.find_child("GalaxyView", true, false)
	_build_buttons()

func _build_buttons() -> void:
	# Clear any existing children.
	for child in get_children():
		child.queue_free()

	var modes := [
		{ "label": "None", "mode": -1 },
		{ "label": "Security", "mode": 0 },
		{ "label": "Trade Flow", "mode": 1 },
		{ "label": "Intel Age", "mode": 2 },
	]

	for entry in modes:
		var btn := Button.new()
		btn.text = entry["label"]
		btn.custom_minimum_size = Vector2(100, 30)
		btn.pressed.connect(_on_mode_pressed.bind(entry["mode"]))
		add_child(btn)

func _on_mode_pressed(mode: int) -> void:
	if _galaxy_view == null:
		_galaxy_view = get_tree().root.find_child("GalaxyView", true, false)
	if _galaxy_view != null and _galaxy_view.has_method("SetOverlayModeV0"):
		_galaxy_view.call("SetOverlayModeV0", mode)
	_update_button_styles(mode)

func _update_button_styles(active_mode: int) -> void:
	# Mode-to-index map: None=-1→0, Security=0→1, Trade=1→2, Intel=2→3
	var active_idx: int = active_mode + 1
	var idx := 0
	for child in get_children():
		if child is Button:
			child.disabled = (idx == active_idx)
			idx += 1

## Called by game_manager.gd when galaxy overlay opens/closes.
func set_overlay_visible(vis: bool) -> void:
	visible = vis
	if vis and _galaxy_view != null and _galaxy_view.has_method("GetOverlayModeV0"):
		var current_mode: int = _galaxy_view.call("GetOverlayModeV0")
		_update_button_styles(current_mode)
