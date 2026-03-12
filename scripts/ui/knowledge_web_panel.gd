# scripts/ui/knowledge_web_panel.gd
# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Discovery Web panel for the knowledge graph.
# Shows knowledge graph connections grouped by type, with revealed vs unknown status.
# Toggled via K key.
extends PanelContainer

var _bridge = null
var _list_container: VBoxContainer = null
var _stats_label: Label = null

func _ready() -> void:
	name = "KnowledgeWebPanel"
	visible = false
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -320
	offset_right = 320
	offset_top = -260
	offset_bottom = 260
	custom_minimum_size = Vector2(640, 520)
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
	title.text = "DISCOVERY WEB"
	title.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(title)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.pressed.connect(func(): toggle_v0())
	header.add_child(close_btn)

	# Stats bar.
	_stats_label = Label.new()
	_stats_label.text = ""
	_stats_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_stats_label.add_theme_color_override("font_color", UITheme.GOLD)
	root_vbox.add_child(_stats_label)

	# Separator below stats.
	var sep := HSeparator.new()
	root_vbox.add_child(sep)

	# Connection list (scrollable).
	var list_scroll := ScrollContainer.new()
	list_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root_vbox.add_child(list_scroll)

	_list_container = VBoxContainer.new()
	_list_container.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_list_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list_scroll.add_child(_list_container)

	_bridge = get_node_or_null("/root/SimBridge")

func toggle_v0() -> void:
	visible = not visible
	if visible:
		_refresh()

func _refresh() -> void:
	# Clear existing children.
	for child in _list_container.get_children():
		child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")

	# Update stats bar.
	_update_stats()

	# Fetch connections.
	if _bridge == null or not _bridge.has_method("GetKnowledgeGraphV0"):
		_show_empty_state()
		return

	var connections: Array = _bridge.call("GetKnowledgeGraphV0")
	if connections.size() == 0:
		_show_empty_state()
		return

	# Group connections by type.
	var groups: Dictionary = {}
	for conn in connections:
		var conn_type: String = str(conn.get("type", "unknown"))
		if not groups.has(conn_type):
			groups[conn_type] = []
		groups[conn_type].append(conn)

	# Render each group.
	var sorted_types: Array = groups.keys()
	sorted_types.sort()
	for conn_type in sorted_types:
		var group_conns: Array = groups[conn_type]
		_add_group_header(conn_type, group_conns)
		for conn in group_conns:
			_add_connection_row(conn)

func _update_stats() -> void:
	if _bridge == null or not _bridge.has_method("GetKnowledgeGraphStatsV0"):
		_stats_label.text = ""
		return
	var stats: Dictionary = _bridge.call("GetKnowledgeGraphStatsV0")
	var total: int = int(stats.get("total", 0))
	var revealed: int = int(stats.get("revealed", 0))
	var question_marks: int = int(stats.get("question_marks", 0))
	_stats_label.text = "%d revealed / %d total" % [revealed, total]
	if question_marks > 0:
		_stats_label.text += "  (%d unknown)" % question_marks

func _show_empty_state() -> void:
	var empty := Label.new()
	empty.text = "No discoveries yet."
	empty.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	empty.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_list_container.add_child(empty)

func _add_group_header(conn_type: String, group_conns: Array) -> void:
	# Count revealed in this group.
	var revealed_count: int = 0
	for conn in group_conns:
		if conn.get("revealed", false):
			revealed_count += 1
	var header := Label.new()
	header.text = "%s  (%d/%d)" % [conn_type.to_upper(), revealed_count, group_conns.size()]
	header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	header.add_theme_color_override("font_color", UITheme.CYAN)
	_list_container.add_child(header)

func _add_connection_row(conn: Dictionary) -> void:
	var source_id: String = str(conn.get("source_id", "?"))
	var target_id: String = str(conn.get("target_id", "?"))
	var _conn_type: String = str(conn.get("type", ""))
	var is_revealed: bool = conn.get("revealed", false)
	var row_visible: bool = conn.get("visible", false)
	var description: String = str(conn.get("description", ""))

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_list_container.add_child(row)

	# Revealed status indicator.
	var status_lbl := Label.new()
	if is_revealed:
		status_lbl.text = "[+]"
		status_lbl.add_theme_color_override("font_color", UITheme.GREEN)
	elif row_visible:
		status_lbl.text = "[?]"
		status_lbl.add_theme_color_override("font_color", UITheme.YELLOW)
	else:
		status_lbl.text = "[-]"
		status_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	status_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	row.add_child(status_lbl)

	# Connection label: source -> target.
	var conn_lbl := Label.new()
	if is_revealed:
		conn_lbl.text = "%s -> %s" % [source_id, target_id]
		conn_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	elif row_visible:
		conn_lbl.text = "%s -> ???" % source_id
		conn_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	else:
		conn_lbl.text = "??? -> ???"
		conn_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	conn_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	conn_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(conn_lbl)

	# Description tooltip (if revealed and has description).
	if is_revealed and not description.is_empty():
		var desc_lbl := Label.new()
		desc_lbl.text = description
		desc_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		desc_lbl.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
		desc_lbl.clip_text = true
		desc_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(desc_lbl)
