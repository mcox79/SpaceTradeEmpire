# GATE.S6.FRACTURE.UI_PANEL.001: Fracture travel panel.
# Lists available void sites with distance, cost, trace risk.
# Confirm button dispatches fracture travel, cancel returns to flight.
extends CanvasLayer

var _panel: PanelContainer = null
var _vbox: VBoxContainer = null
var _title_label: Label = null
var _sites_container: VBoxContainer = null
var _cancel_btn: Button = null
var _bridge: Node = null

func _ready() -> void:
	layer = 100
	visible = false
	_bridge = get_node_or_null("/root/SimBridge")
	_build_ui()

func _build_ui() -> void:
	_panel = PanelContainer.new()
	_panel.anchor_left = 0.5
	_panel.anchor_right = 0.5
	_panel.offset_left = -220
	_panel.offset_right = 220
	_panel.anchor_top = 0.15
	_panel.anchor_bottom = 0.85
	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_dock())
	add_child(_panel)

	var scroll = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_panel.add_child(scroll)

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 6)
	_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_vbox)

	_title_label = Label.new()
	_title_label.text = "Fracture Travel"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_title_label.add_theme_color_override("font_color", UITheme.CYAN)
	_vbox.add_child(_title_label)

	var cost_note = Label.new()
	cost_note.text = "WARNING: Fracture jumps consume fuel, damage hull, and leave trace signatures."
	cost_note.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	cost_note.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	cost_note.add_theme_color_override("font_color", UITheme.ORANGE)
	_vbox.add_child(cost_note)

	_vbox.add_child(HSeparator.new())

	_sites_container = VBoxContainer.new()
	_sites_container.add_theme_constant_override("separation", 4)
	_vbox.add_child(_sites_container)

	_vbox.add_child(HSeparator.new())

	_cancel_btn = Button.new()
	_cancel_btn.text = "Cancel"
	_cancel_btn.pressed.connect(close_v0)
	_vbox.add_child(_cancel_btn)

func open_v0() -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	_populate_sites()
	visible = true

func close_v0() -> void:
	visible = false
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("_transition_player_state_v0"):
		gm.call("_transition_player_state_v0", 0)  # IN_FLIGHT

func _populate_sites() -> void:
	if _sites_container == null:
		return
	for child in _sites_container.get_children():
		_sites_container.remove_child(child)
		child.queue_free()

	if _bridge == null or not _bridge.has_method("GetAvailableVoidSitesV0"):
		var no_data = Label.new()
		no_data.text = "No bridge connection"
		no_data.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_sites_container.add_child(no_data)
		return

	var sites: Array = _bridge.call("GetAvailableVoidSitesV0")
	if sites.size() == 0:
		var empty_lbl = Label.new()
		empty_lbl.text = "No void sites discovered yet."
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_sites_container.add_child(empty_lbl)
		return

	for site in sites:
		var site_id: String = str(site.get("id", ""))
		var family: String = str(site.get("family", "Unknown"))
		var dist: float = float(site.get("distance", 0))
		var fuel: int = int(site.get("fuel_cost", 0))
		var hull: int = int(site.get("hull_stress", 0))
		var trace: float = float(site.get("trace_risk", 0))
		var can_afford: bool = bool(site.get("can_afford", false))

		var row = VBoxContainer.new()
		row.add_theme_constant_override("separation", 2)

		# Site header
		var header = Label.new()
		header.text = "%s (%s)" % [site_id, family]
		header.add_theme_color_override("font_color", UITheme.CYAN)
		header.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		row.add_child(header)

		# Cost details
		var cost_lbl = Label.new()
		cost_lbl.text = "Dist: %.0fu | Fuel: %d | Hull dmg: %d | Trace: %.1f" % [dist, fuel, hull, trace]
		cost_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		cost_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		row.add_child(cost_lbl)

		# Jump button
		var btn = Button.new()
		if can_afford:
			btn.text = "Jump to %s" % site_id
			btn.pressed.connect(_on_jump.bind(site_id))
		else:
			btn.text = "Insufficient fuel"
			btn.disabled = true
		row.add_child(btn)

		row.add_child(HSeparator.new())
		_sites_container.add_child(row)

func _on_jump(site_id: String) -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge and _bridge.has_method("DispatchFractureTravelV0"):
		_bridge.call("DispatchFractureTravelV0", "fleet_trader_1", site_id)
		print("UUIR|FRACTURE_JUMP|" + site_id)
	close_v0()
