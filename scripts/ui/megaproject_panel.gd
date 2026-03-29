# scripts/ui/megaproject_panel.gd
# GATE.S8.MEGAPROJECT.UI.001: Megaproject construction panel.
# Shows active megaprojects with stage progress, supply delivery, and start construction.
# Accessible from empire dashboard or node context menu.
extends PanelContainer

var _bridge = null
var _list_container: VBoxContainer = null
var _detail_container: VBoxContainer = null
var _start_section: VBoxContainer = null
var _selected_mp_id: String = ""
var _start_node_id: String = ""  # Set when opened from node context.

func _ready() -> void:
	name = "MegaprojectPanel"
	visible = false
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -360
	offset_right = 360
	offset_top = -280
	offset_bottom = 280
	custom_minimum_size = Vector2(720, 560)
	var style := UITheme.make_panel_standard()
	add_theme_stylebox_override("panel", style)

	var root_vbox := VBoxContainer.new()
	root_vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	add_child(root_vbox)

	# Header row.
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_LG)
	root_vbox.add_child(header)

	var title := Label.new()
	title.text = "MEGAPROJECTS"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(title)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.pressed.connect(func(): toggle_v0())
	header.add_child(close_btn)

	# Split: list (left) + detail (right).
	var split := HSplitContainer.new()
	split.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root_vbox.add_child(split)

	# Left: list of active megaprojects.
	var list_scroll := ScrollContainer.new()
	list_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list_scroll.size_flags_stretch_ratio = 0.4
	split.add_child(list_scroll)

	_list_container = VBoxContainer.new()
	_list_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_list_container.add_theme_constant_override("separation", UITheme.SPACE_SM)
	list_scroll.add_child(_list_container)

	# Right: detail view.
	var detail_scroll := ScrollContainer.new()
	detail_scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	detail_scroll.size_flags_stretch_ratio = 0.6
	split.add_child(detail_scroll)

	_detail_container = VBoxContainer.new()
	_detail_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_detail_container.add_theme_constant_override("separation", UITheme.SPACE_SM)
	detail_scroll.add_child(_detail_container)

	# Start construction section (bottom).
	_start_section = VBoxContainer.new()
	_start_section.add_theme_constant_override("separation", UITheme.SPACE_SM)
	root_vbox.add_child(_start_section)

func toggle_v0(node_id: String = "") -> void:
	_start_node_id = node_id
	visible = !visible
	if visible:
		_refresh_v0()

func _refresh_v0() -> void:
	_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		return

	_refresh_list_v0()
	if _selected_mp_id != "":
		_refresh_detail_v0(_selected_mp_id)
	else:
		_clear_detail_v0()
	_refresh_start_section_v0()

func _refresh_list_v0() -> void:
	for c in _list_container.get_children():
		c.queue_free()

	var megaprojects: Array = _bridge.call("GetMegaprojectsV0")
	if megaprojects.is_empty():
		var empty_label := Label.new()
		empty_label.text = "No active megaprojects."
		empty_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_list_container.add_child(empty_label)
		return

	for mp in megaprojects:
		var mp_id: String = str(mp.get("id", ""))
		var mp_name: String = str(mp.get("name", mp_id))
		var stage: int = int(mp.get("stage", 0))
		var max_stages: int = int(mp.get("max_stages", 1))
		var completed: bool = bool(mp.get("completed", false))

		var btn := Button.new()
		if completed:
			btn.text = "%s [COMPLETE]" % mp_name
		else:
			btn.text = "%s (Stage %d/%d)" % [mp_name, stage + 1, max_stages]
		btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		btn.pressed.connect(_on_mp_selected.bind(mp_id))
		_list_container.add_child(btn)

func _on_mp_selected(mp_id: String) -> void:
	_selected_mp_id = mp_id
	_refresh_detail_v0(mp_id)

func _clear_detail_v0() -> void:
	for c in _detail_container.get_children():
		c.queue_free()
	var hint := Label.new()
	hint.text = "Select a megaproject to view details."
	hint.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	hint.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_detail_container.add_child(hint)

func _refresh_detail_v0(mp_id: String) -> void:
	for c in _detail_container.get_children():
		c.queue_free()

	var detail: Dictionary = _bridge.call("GetMegaprojectDetailV0", mp_id)
	if detail.is_empty():
		_clear_detail_v0()
		return

	# Title.
	var title := Label.new()
	title.text = str(detail.get("name", mp_id))
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.GOLD)
	_detail_container.add_child(title)

	# Description.
	var desc := Label.new()
	desc.text = str(detail.get("description", ""))
	desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_detail_container.add_child(desc)

	# Location.
	var loc := Label.new()
	loc.text = "Location: %s" % str(detail.get("node_id", ""))
	loc.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_detail_container.add_child(loc)

	var completed: bool = bool(detail.get("completed", false))
	if completed:
		var done_label := Label.new()
		done_label.text = "CONSTRUCTION COMPLETE"
		done_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		done_label.add_theme_color_override("font_color", UITheme.GREEN)
		_detail_container.add_child(done_label)
		return

	# Stage progress.
	var stage: int = int(detail.get("stage", 0))
	var max_stages: int = int(detail.get("max_stages", 1))
	var ticks: int = int(detail.get("progress_ticks", 0))
	var ticks_per: int = int(detail.get("ticks_per_stage", 1))

	var stage_label := Label.new()
	stage_label.text = "Stage %d / %d" % [stage + 1, max_stages]
	stage_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_detail_container.add_child(stage_label)

	# Progress bar.
	var prog := ProgressBar.new()
	prog.max_value = ticks_per
	prog.value = ticks
	prog.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_detail_container.add_child(prog)

	var prog_label := Label.new()
	prog_label.text = "%d / %d ticks" % [ticks, ticks_per]
	prog_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_detail_container.add_child(prog_label)

	# Supply checklist.
	var supply_header := Label.new()
	supply_header.text = "Supply Requirements (this stage):"
	supply_header.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	supply_header.add_theme_color_override("font_color", UITheme.CYAN)
	_detail_container.add_child(supply_header)

	var supply: Array = detail.get("supply", [])
	for req in supply:
		var good_id: String = str(req.get("good_id", ""))
		var required: int = int(req.get("required", 0))
		var delivered: int = int(req.get("delivered", 0))
		var met: bool = delivered >= required

		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", UITheme.SPACE_SM)
		_detail_container.add_child(row)

		var check := Label.new()
		check.text = "[x]" if met else "[ ]"
		check.add_theme_color_override("font_color", UITheme.GREEN if met else UITheme.TEXT_DISABLED)
		row.add_child(check)

		var supply_label := Label.new()
		supply_label.text = "%s: %d / %d" % [good_id, delivered, required]
		supply_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(supply_label)

		# Deliver button (only if player has cargo).
		if not met:
			var deliver_btn := Button.new()
			deliver_btn.text = "Deliver"
			deliver_btn.pressed.connect(_on_deliver.bind(mp_id, good_id))
			row.add_child(deliver_btn)

func _on_deliver(mp_id: String, good_id: String) -> void:
	if _bridge == null:
		return
	# Deliver up to 10 units at a time.
	var success: bool = _bridge.call("DeliverMegaprojectSupplyV0", mp_id, good_id, 10)
	if success:
		_refresh_detail_v0(mp_id)

func _refresh_start_section_v0() -> void:
	for c in _start_section.get_children():
		c.queue_free()

	if _start_node_id.is_empty():
		return

	var sep := HSeparator.new()
	_start_section.add_child(sep)

	var header := Label.new()
	header.text = "Start New Construction at %s" % _start_node_id
	header.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	header.add_theme_color_override("font_color", UITheme.GOLD)
	_start_section.add_child(header)

	var types: Array = _bridge.call("GetMegaprojectTypesV0")
	for t in types:
		var type_id: String = str(t.get("type_id", ""))
		var t_name: String = str(t.get("name", type_id))
		var credit_cost: int = int(t.get("credit_cost", 0))
		var min_rep: int = int(t.get("min_faction_rep", 0))

		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", UITheme.SPACE_SM)
		_start_section.add_child(row)

		var label := Label.new()
		label.text = "%s (%dc, rep %d+)" % [t_name, credit_cost, min_rep]
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(label)

		var start_btn := Button.new()
		start_btn.text = "Start"
		start_btn.pressed.connect(_on_start_megaproject.bind(type_id))
		row.add_child(start_btn)

func _on_start_megaproject(type_id: String) -> void:
	if _bridge == null or _start_node_id.is_empty():
		return
	var result: Dictionary = _bridge.call("StartMegaprojectV0", type_id, _start_node_id)
	var success: bool = bool(result.get("success", false))
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if success:
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "Megaproject started!", "system", 4.0)
		_refresh_v0()
	else:
		var reason: String = str(result.get("reason", "unknown"))
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "Cannot start: " + reason, "warning", 4.0)
