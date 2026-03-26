# scripts/ui/knowledge_web_panel.gd
# GATE.X.UI_POLISH.KNOWLEDGE_WEB.001: Discovery Web panel for the knowledge graph.
# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Enhanced with connection type labels, source
# attribution, filter by faction/region.
# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Graph layout with zoom, curved edges,
# category coloring, hover tooltips, improved node spacing.
# Shows knowledge graph connections grouped by type, with revealed vs unknown status.
# Toggled via K key.
extends PanelContainer

var _bridge = null
var _list_container: VBoxContainer = null
var _stats_label: Label = null
# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Filter state.
var _filter_buttons: HBoxContainer = null
var _active_filter: String = "all"  # "all", or a connection type like "faction", "region", etc.

# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Graph canvas and zoom state
var _graph_canvas: Control = null
var _graph_zoom: float = 1.0
const _GRAPH_ZOOM_MIN: float = 0.5
const _GRAPH_ZOOM_MAX: float = 2.0
const _GRAPH_ZOOM_STEP: float = 0.1
var _graph_scroll_offset: Vector2 = Vector2.ZERO
var _graph_node_positions: Dictionary = {}  # node_id -> Vector2
var _graph_connections: Array = []  # cached connection data for drawing
var _tooltip_panel: PanelContainer = null
var _tooltip_label: Label = null
var _hovered_node_id: String = ""

# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Category border colors
const _CAT_COLOR_FACTION := Color(0.3, 0.5, 1.0)    # blue
const _CAT_COLOR_LOCATION := Color(0.2, 0.85, 0.35)  # green
const _CAT_COLOR_ARTIFACT := Color(1.0, 0.8, 0.2)    # gold
const _CAT_COLOR_LORE := Color(0.7, 0.3, 0.9)        # purple
const _CAT_COLOR_DEFAULT := Color(0.6, 0.6, 0.6)     # gray fallback

# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Minimum distance between graph nodes (px)
const _NODE_MIN_SPACING: float = 90.0
const _NODE_RADIUS: float = 20.0

# GATE.T58.UI.KG_UNLOCK.001: KG progressive unlock state (7 milestones).
# 0=Geographic, 1=Pin, 2=Relational, 3=Annotate, 4=Flag, 5=Link, 6=Compare
var _kg_milestone: int = 0  # STRUCTURAL: milestone index
var _milestone_labels: Dictionary = {
	0: "Geographic", 1: "Pin", 2: "Relational", 3: "Annotate",
	4: "Flag", 5: "Link", 6: "Compare"
}
var _verb_section: VBoxContainer = null
var _milestone_notification_label: Label = null

func _ready() -> void:
	name = "KnowledgeWebPanel"
	visible = false
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -360
	offset_right = 360
	offset_top = -300
	offset_bottom = 300
	custom_minimum_size = Vector2(720, 600)
	var style := UITheme.make_panel_standard()
	# FEEL_PASS3: Full opacity — floating panels over space need solid backdrop.
	style.bg_color.a = 1.0
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

	# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Zoom indicator label
	var zoom_label := Label.new()
	zoom_label.name = "ZoomLabel"
	zoom_label.text = "100%"
	zoom_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	zoom_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	header.add_child(zoom_label)

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

	# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Filter button row.
	_filter_buttons = HBoxContainer.new()
	_filter_buttons.add_theme_constant_override("separation", UITheme.SPACE_SM)
	root_vbox.add_child(_filter_buttons)

	# "All" filter button (always present).
	var all_btn := Button.new()
	all_btn.text = "ALL"
	all_btn.pressed.connect(func(): _set_filter("all"))
	_filter_buttons.add_child(all_btn)

	# GATE.T58.UI.KG_UNLOCK.001: KG verb toolbar (shows unlocked verbs).
	_verb_section = VBoxContainer.new()
	_verb_section.name = "VerbSection"
	_verb_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	root_vbox.add_child(_verb_section)

	# Milestone notification label (animated on unlock).
	_milestone_notification_label = Label.new()
	_milestone_notification_label.name = "MilestoneNotification"
	_milestone_notification_label.text = ""
	_milestone_notification_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_milestone_notification_label.add_theme_color_override("font_color", UITheme.GOLD)
	_milestone_notification_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_milestone_notification_label.visible = false
	root_vbox.add_child(_milestone_notification_label)

	# Separator below filters.
	var sep := HSeparator.new()
	root_vbox.add_child(sep)

	# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Graph canvas (clip content, handles draw)
	var graph_clip := Control.new()
	graph_clip.clip_contents = true
	graph_clip.size_flags_vertical = Control.SIZE_EXPAND_FILL
	graph_clip.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root_vbox.add_child(graph_clip)

	_graph_canvas = Control.new()
	_graph_canvas.name = "GraphCanvas"
	_graph_canvas.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_graph_canvas.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_graph_canvas.set_anchors_preset(Control.PRESET_FULL_RECT)
	_graph_canvas.mouse_filter = Control.MOUSE_FILTER_PASS
	graph_clip.add_child(_graph_canvas)

	# Connection list (scrollable) — fallback when no graph data
	var list_scroll := ScrollContainer.new()
	list_scroll.name = "ListScroll"
	list_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	list_scroll.visible = false
	root_vbox.add_child(list_scroll)

	_list_container = VBoxContainer.new()
	_list_container.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_list_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list_scroll.add_child(_list_container)

	# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Hover tooltip panel
	_tooltip_panel = PanelContainer.new()
	_tooltip_panel.visible = false
	_tooltip_panel.z_index = 10
	_tooltip_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var tip_style := StyleBoxFlat.new()
	tip_style.bg_color = Color(0.08, 0.08, 0.12, 0.95)
	tip_style.border_color = UITheme.CYAN
	tip_style.set_border_width_all(1)
	tip_style.set_content_margin_all(8)
	tip_style.set_corner_radius_all(4)
	_tooltip_panel.add_theme_stylebox_override("panel", tip_style)
	add_child(_tooltip_panel)

	_tooltip_label = Label.new()
	_tooltip_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_tooltip_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_tooltip_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_tooltip_label.custom_minimum_size = Vector2(180, 0)
	_tooltip_panel.add_child(_tooltip_label)

	# Footer hint — navigate up from scroll → root_vbox
	root_vbox.add_child(UITheme.make_dismiss_hint("K"))

	_bridge = get_node_or_null("/root/SimBridge")

func toggle_v0() -> void:
	if visible:
		UITheme.animate_close(self, func(): visible = false)
	else:
		visible = true
		UITheme.animate_open(self)
		_refresh()

# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Mouse wheel zoom
func _gui_input(event: InputEvent) -> void:
	if not visible:
		return
	if event is InputEventMouseButton:
		var mb: InputEventMouseButton = event
		if mb.pressed:
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
				_set_zoom(_graph_zoom + _GRAPH_ZOOM_STEP)
				accept_event()
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_set_zoom(_graph_zoom - _GRAPH_ZOOM_STEP)
				accept_event()
	elif event is InputEventMouseMotion:
		_handle_graph_hover(event.position)


func _set_zoom(new_zoom: float) -> void:
	_graph_zoom = clampf(new_zoom, _GRAPH_ZOOM_MIN, _GRAPH_ZOOM_MAX)
	var zoom_label = get_node_or_null("VBoxContainer/HBoxContainer/ZoomLabel")
	# Try finding by name in all header children
	for child in get_children():
		if child is VBoxContainer:
			for hbox in child.get_children():
				if hbox is HBoxContainer:
					for lbl in hbox.get_children():
						if lbl is Label and lbl.name == "ZoomLabel":
							lbl.text = "%d%%" % int(_graph_zoom * 100.0)
	# Smooth tween the canvas scale
	if _graph_canvas:
		var tween := create_tween()
		tween.tween_property(_graph_canvas, "scale", Vector2(_graph_zoom, _graph_zoom), 0.15).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)


# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Set active filter and refresh.
func _set_filter(filter_name: String) -> void:
	_active_filter = filter_name
	_refresh()

func _refresh() -> void:
	# Clear graph canvas children
	for child in _graph_canvas.get_children():
		child.queue_free()

	# Clear existing list children.
	for child in _list_container.get_children():
		child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")

	# GATE.T58.UI.KG_UNLOCK.001: Refresh milestone state.
	_refresh_milestone_state()

	# Update stats bar.
	_update_stats()

	# Fetch connections.
	if _bridge == null or not _bridge.has_method("GetKnowledgeGraphV0"):
		_show_graph_or_list(false)
		_show_empty_state()
		return

	var connections: Array = _bridge.call("GetKnowledgeGraphV0")
	if connections.size() == 0:
		_show_graph_or_list(false)
		_show_empty_state()
		return

	# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Build dynamic filter buttons from available types.
	_rebuild_filter_buttons(connections)

	# Apply filter
	var filtered: Array = []
	for conn in connections:
		var conn_type: String = str(conn.get("type", "unknown"))
		if _active_filter != "all" and conn_type != _active_filter:
			continue
		filtered.append(conn)

	_graph_connections = filtered

	# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Build graph layout if we have nodes
	if filtered.size() > 0:
		_show_graph_or_list(true)
		_build_graph_layout(filtered)
		_draw_graph(filtered)
	else:
		_show_graph_or_list(false)
		_show_empty_state()


func _show_graph_or_list(show_graph: bool) -> void:
	if _graph_canvas and _graph_canvas.get_parent():
		_graph_canvas.get_parent().visible = show_graph
	var list_scroll = _list_container.get_parent() if _list_container else null
	if list_scroll:
		list_scroll.visible = not show_graph


# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Force-directed layout for graph nodes
func _build_graph_layout(connections: Array) -> void:
	_graph_node_positions.clear()

	# Collect unique node IDs
	var node_ids: Dictionary = {}
	var node_connections: Dictionary = {}  # node_id -> count
	var node_types: Dictionary = {}  # node_id -> category type (from first connection)
	for conn in connections:
		var src: String = str(conn.get("source_id", ""))
		var tgt: String = str(conn.get("target_id", ""))
		var conn_type: String = str(conn.get("type", "unknown"))
		if not src.is_empty():
			node_ids[src] = true
			node_connections[src] = node_connections.get(src, 0) + 1
			if not node_types.has(src):
				node_types[src] = conn_type
		if not tgt.is_empty():
			node_ids[tgt] = true
			node_connections[tgt] = node_connections.get(tgt, 0) + 1
			if not node_types.has(tgt):
				node_types[tgt] = conn_type

	var ids: Array = node_ids.keys()
	if ids.is_empty():
		return

	# Canvas center
	var canvas_size: Vector2 = _graph_canvas.size if _graph_canvas.size.x > 0 else Vector2(600, 400)
	var center := canvas_size / 2.0

	# Initial placement: circular layout
	var radius: float = min(canvas_size.x, canvas_size.y) * 0.35
	for i in range(ids.size()):
		var angle: float = float(i) * TAU / float(ids.size())
		var pos := center + Vector2(cos(angle) * radius, sin(angle) * radius)
		_graph_node_positions[ids[i]] = pos

	# Simple force-directed relaxation (few iterations to space nodes)
	for _iter in range(30):
		var forces: Dictionary = {}
		for nid in ids:
			forces[nid] = Vector2.ZERO

		# Repulsion between all node pairs
		for i in range(ids.size()):
			for j in range(i + 1, ids.size()):
				var a_id: String = ids[i]
				var b_id: String = ids[j]
				var a_pos: Vector2 = _graph_node_positions[a_id]
				var b_pos: Vector2 = _graph_node_positions[b_id]
				var delta: Vector2 = a_pos - b_pos
				var dist: float = delta.length()
				if dist < 1.0:
					delta = Vector2(1.0, 0.0)
					dist = 1.0
				# Strong repulsion to enforce minimum spacing
				var repel_force: float = (_NODE_MIN_SPACING * _NODE_MIN_SPACING) / dist
				var direction: Vector2 = delta.normalized()
				forces[a_id] += direction * repel_force
				forces[b_id] -= direction * repel_force

		# Attraction along edges (connected nodes pull toward each other)
		for conn in connections:
			var src: String = str(conn.get("source_id", ""))
			var tgt: String = str(conn.get("target_id", ""))
			if src.is_empty() or tgt.is_empty():
				continue
			if not _graph_node_positions.has(src) or not _graph_node_positions.has(tgt):
				continue
			var a_pos: Vector2 = _graph_node_positions[src]
			var b_pos: Vector2 = _graph_node_positions[tgt]
			var delta: Vector2 = b_pos - a_pos
			var dist: float = delta.length()
			var ideal: float = _NODE_MIN_SPACING * 1.5
			var attract: float = (dist - ideal) * 0.05
			var direction: Vector2 = delta.normalized()
			forces[src] += direction * attract
			forces[tgt] -= direction * attract

		# Center gravity (pull toward center to prevent drift)
		for nid in ids:
			var to_center: Vector2 = center - _graph_node_positions[nid]
			forces[nid] += to_center * 0.02

		# Apply forces with damping
		for nid in ids:
			var f: Vector2 = forces[nid]
			if f.length() > 10.0:
				f = f.normalized() * 10.0
			_graph_node_positions[nid] += f

	# Clamp positions to canvas bounds with margin
	var margin: float = _NODE_RADIUS + 10.0
	for nid in ids:
		var pos: Vector2 = _graph_node_positions[nid]
		pos.x = clampf(pos.x, margin, canvas_size.x - margin)
		pos.y = clampf(pos.y, margin, canvas_size.y - margin)
		_graph_node_positions[nid] = pos


# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Draw graph nodes and curved edges
func _draw_graph(connections: Array) -> void:
	# Draw edges first (behind nodes)
	for conn in connections:
		var src: String = str(conn.get("source_id", ""))
		var tgt: String = str(conn.get("target_id", ""))
		if src.is_empty() or tgt.is_empty():
			continue
		if not _graph_node_positions.has(src) or not _graph_node_positions.has(tgt):
			continue
		var from_pos: Vector2 = _graph_node_positions[src]
		var to_pos: Vector2 = _graph_node_positions[tgt]
		var is_revealed: bool = conn.get("revealed", false)
		_draw_curved_edge(from_pos, to_pos, is_revealed)

	# Draw nodes on top
	var node_descriptions: Dictionary = {}  # node_id -> {title, desc, conn_count, type}
	for conn in connections:
		var src: String = str(conn.get("source_id", ""))
		var tgt: String = str(conn.get("target_id", ""))
		var conn_type: String = str(conn.get("type", "unknown"))
		var desc: String = str(conn.get("description", ""))
		if not src.is_empty() and not node_descriptions.has(src):
			node_descriptions[src] = {"title": src, "desc": desc, "conn_count": 0, "type": conn_type}
		if not tgt.is_empty() and not node_descriptions.has(tgt):
			node_descriptions[tgt] = {"title": tgt, "desc": "", "conn_count": 0, "type": conn_type}
		if node_descriptions.has(src):
			node_descriptions[src]["conn_count"] += 1
		if node_descriptions.has(tgt):
			node_descriptions[tgt]["conn_count"] += 1

	for node_id in _graph_node_positions:
		if not _graph_node_positions.has(node_id):
			continue
		var pos: Vector2 = _graph_node_positions[node_id]
		var info: Dictionary = node_descriptions.get(node_id, {"title": node_id, "desc": "", "conn_count": 0, "type": "unknown"})
		_draw_graph_node(node_id, pos, info)


func _get_category_color(conn_type: String) -> Color:
	var lower := conn_type.to_lower()
	if "faction" in lower:
		return _CAT_COLOR_FACTION
	elif "location" in lower or "region" in lower or "system" in lower:
		return _CAT_COLOR_LOCATION
	elif "artifact" in lower or "item" in lower or "module" in lower:
		return _CAT_COLOR_ARTIFACT
	elif "lore" in lower or "history" in lower or "precursor" in lower:
		return _CAT_COLOR_LORE
	else:
		return _CAT_COLOR_DEFAULT


# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Draw curved Line2D edge between nodes
func _draw_curved_edge(from_pos: Vector2, to_pos: Vector2, is_revealed: bool) -> void:
	var line := Line2D.new()
	line.width = 2.0 if is_revealed else 1.0
	line.default_color = UITheme.CYAN.lerp(Color.WHITE, 0.2) if is_revealed else Color(0.3, 0.3, 0.4, 0.5)
	line.antialiased = true

	var dist: float = from_pos.distance_to(to_pos)

	# Use curved path for close nodes, straight for distant ones
	if dist < _NODE_MIN_SPACING * 3.0 and dist > 1.0:
		# Quadratic bezier curve: control point offset perpendicular to the line
		var mid: Vector2 = (from_pos + to_pos) / 2.0
		var perp: Vector2 = (to_pos - from_pos).orthogonal().normalized()
		var curve_amount: float = dist * 0.3
		var control: Vector2 = mid + perp * curve_amount

		var segments: int = 12
		for i in range(segments + 1):
			var t: float = float(i) / float(segments)
			# Quadratic bezier: B(t) = (1-t)^2 * P0 + 2*(1-t)*t * P1 + t^2 * P2
			var p: Vector2 = (1.0 - t) * (1.0 - t) * from_pos + 2.0 * (1.0 - t) * t * control + t * t * to_pos
			line.add_point(p)
	else:
		line.add_point(from_pos)
		line.add_point(to_pos)

	_graph_canvas.add_child(line)


# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Draw a knowledge graph node
func _draw_graph_node(node_id: String, pos: Vector2, info: Dictionary) -> void:
	var conn_type: String = str(info.get("type", "unknown"))
	var border_color: Color = _get_category_color(conn_type)
	var conn_count: int = int(info.get("conn_count", 0))
	var node_title: String = str(info.get("title", node_id))

	# Node container (button-like for hover detection)
	var node_btn := Button.new()
	node_btn.flat = true
	node_btn.position = pos - Vector2(_NODE_RADIUS, _NODE_RADIUS)
	node_btn.custom_minimum_size = Vector2(_NODE_RADIUS * 2, _NODE_RADIUS * 2)
	node_btn.size = Vector2(_NODE_RADIUS * 2, _NODE_RADIUS * 2)

	# Style with category-colored border
	var node_style := StyleBoxFlat.new()
	node_style.bg_color = Color(0.1, 0.12, 0.18, 0.9)
	node_style.border_color = border_color
	node_style.set_border_width_all(2)
	node_style.set_corner_radius_all(int(_NODE_RADIUS))
	node_btn.add_theme_stylebox_override("normal", node_style)

	var hover_style := StyleBoxFlat.new()
	hover_style.bg_color = Color(0.15, 0.18, 0.25, 0.95)
	hover_style.border_color = border_color.lightened(0.3)
	hover_style.set_border_width_all(3)
	hover_style.set_corner_radius_all(int(_NODE_RADIUS))
	node_btn.add_theme_stylebox_override("hover", hover_style)

	var pressed_style := node_style.duplicate()
	node_btn.add_theme_stylebox_override("pressed", pressed_style)

	# Short label inside node
	var short_name: String = node_title.substr(0, 3).to_upper() if node_title.length() > 3 else node_title.to_upper()
	node_btn.text = short_name
	node_btn.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	node_btn.add_theme_color_override("font_color", border_color.lightened(0.4))

	# Hover signals for tooltip
	var nid := node_id  # capture for closure
	var ninfo := info
	node_btn.mouse_entered.connect(func(): _show_node_tooltip(nid, ninfo))
	node_btn.mouse_exited.connect(func(): _hide_tooltip())

	_graph_canvas.add_child(node_btn)

	# Full name label below node
	var name_label := Label.new()
	name_label.text = node_title
	name_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	name_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.position = pos + Vector2(-40, _NODE_RADIUS + 2)
	name_label.custom_minimum_size = Vector2(80, 0)
	name_label.clip_text = true
	_graph_canvas.add_child(name_label)

	# Connection count badge (small number near top-right)
	if conn_count > 1:
		var badge := Label.new()
		badge.text = str(conn_count)
		badge.add_theme_font_size_override("font_size", 10)
		badge.add_theme_color_override("font_color", UITheme.GOLD)
		badge.position = pos + Vector2(_NODE_RADIUS - 6, -_NODE_RADIUS - 4)
		_graph_canvas.add_child(badge)


# GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001: Show tooltip on node hover
func _show_node_tooltip(node_id: String, info: Dictionary) -> void:
	_hovered_node_id = node_id
	if _tooltip_panel == null:
		return

	var title: String = str(info.get("title", node_id))
	var desc: String = str(info.get("desc", ""))
	var conn_count: int = int(info.get("conn_count", 0))
	var conn_type: String = str(info.get("type", ""))

	var text := title
	if not conn_type.is_empty():
		text += "\nCategory: %s" % conn_type.capitalize()
	text += "\nConnections: %d" % conn_count
	if not desc.is_empty():
		text += "\n%s" % desc

	_tooltip_label.text = text
	_tooltip_panel.visible = true

	# Position near the node
	if _graph_node_positions.has(node_id):
		var pos: Vector2 = _graph_node_positions[node_id]
		_tooltip_panel.position = pos + Vector2(_NODE_RADIUS + 10, -20)


func _hide_tooltip() -> void:
	_hovered_node_id = ""
	if _tooltip_panel:
		_tooltip_panel.visible = false


func _handle_graph_hover(mouse_pos: Vector2) -> void:
	# Update tooltip position to follow mouse slightly
	if _tooltip_panel and _tooltip_panel.visible:
		_tooltip_panel.position = mouse_pos + Vector2(15, -10)


# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Rebuild filter buttons dynamically.
func _rebuild_filter_buttons(connections: Array) -> void:
	# Collect unique types.
	var types: Dictionary = {}
	for conn in connections:
		var t: String = str(conn.get("type", "unknown"))
		types[t] = true

	# Remove old dynamic buttons (keep first "ALL" button).
	while _filter_buttons.get_child_count() > 1:
		var child = _filter_buttons.get_child(_filter_buttons.get_child_count() - 1)
		_filter_buttons.remove_child(child)
		child.queue_free()

	# Add a button per type.
	var sorted_types: Array = types.keys()
	sorted_types.sort()
	for t in sorted_types:
		var btn := Button.new()
		btn.text = str(t).to_upper()
		var filter_name: String = t
		btn.pressed.connect(func(): _set_filter(filter_name))
		_filter_buttons.add_child(btn)

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
	if _active_filter != "all":
		_stats_label.text += "  [filter: %s]" % _active_filter.to_upper()

func _show_empty_state() -> void:
	# Show active leads as "signals detected" rumors instead of a blank panel.
	if _bridge and _bridge.has_method("GetActiveLeadsV0"):
		var leads: Array = _bridge.call("GetActiveLeadsV0")
		if leads.size() > 0:
			_show_rumors(leads)
			return
	_list_container.add_child(UITheme.make_empty_state("◈", "No discoveries yet", "Explore distant systems and equip sensor modules to scan for anomalies"))


func _show_rumors(leads: Array) -> void:
	var header := Label.new()
	header.text = "SIGNALS DETECTED"
	header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	header.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	_list_container.add_child(header)

	var hint := Label.new()
	hint.text = "Your sensors have picked up faint readings. Explore to reveal more."
	hint.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	hint.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_list_container.add_child(hint)

	var sep := HSeparator.new()
	_list_container.add_child(sep)

	for lead in leads:
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", UITheme.SPACE_SM)
		_list_container.add_child(row)

		var icon_lbl := Label.new()
		icon_lbl.text = "[?]"
		icon_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		icon_lbl.add_theme_color_override("font_color", UITheme.YELLOW)
		row.add_child(icon_lbl)

		var location: String = str(lead.get("location_token", "UNKNOWN")).replace("_", " ").to_lower().capitalize()
		var payoff: String = str(lead.get("payoff_token", "")).replace("_", " ").to_lower().capitalize()
		var desc_lbl := Label.new()
		desc_lbl.text = "Signal from %s — %s" % [location, payoff]
		desc_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		desc_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		desc_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(desc_lbl)

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
	var conn_type: String = str(conn.get("type", ""))
	var is_revealed: bool = conn.get("revealed", false)
	var row_visible: bool = conn.get("visible", false)
	var description: String = str(conn.get("description", ""))
	# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Source attribution and faction/region info.
	var source_label: String = str(conn.get("source_label", ""))
	var faction_id: String = str(conn.get("faction_id", ""))
	var region_id: String = str(conn.get("region_id", ""))

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

	# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Connection type label.
	var type_lbl := Label.new()
	type_lbl.text = "[%s]" % conn_type.to_upper()
	type_lbl.custom_minimum_size = Vector2(80, 0)
	type_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	type_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	row.add_child(type_lbl)

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

	# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Source attribution (who/what discovered this).
	if is_revealed and not source_label.is_empty():
		var src_lbl := Label.new()
		src_lbl.text = "via %s" % source_label
		src_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		src_lbl.add_theme_color_override("font_color", UITheme.TEXT_INFO)
		row.add_child(src_lbl)

	# GATE.S6.UI_DISCOVERY.KG_PANEL.001: Faction/region tag.
	if is_revealed and (not faction_id.is_empty() or not region_id.is_empty()):
		var tag_lbl := Label.new()
		var tag_parts: Array = []
		if not faction_id.is_empty():
			tag_parts.append(faction_id)
		if not region_id.is_empty():
			tag_parts.append(region_id)
		tag_lbl.text = " ".join(tag_parts)
		tag_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		tag_lbl.add_theme_color_override("font_color", UITheme.GOLD)
		row.add_child(tag_lbl)

	# Description tooltip (if revealed and has description).
	if is_revealed and not description.is_empty():
		var desc_lbl := Label.new()
		desc_lbl.text = description
		desc_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		desc_lbl.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
		desc_lbl.clip_text = true
		desc_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(desc_lbl)


# GATE.T58.UI.KG_UNLOCK.001: Refresh milestone state from bridge.
func _refresh_milestone_state() -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetKGMilestoneV0"):
		return

	var ms: Dictionary = _bridge.call("GetKGMilestoneV0")
	var new_milestone: int = ms.get("highest_milestone_int", 0)
	var has_notification: bool = ms.get("pending_notification", false)

	# Check for milestone advancement.
	if new_milestone > _kg_milestone and has_notification:
		var milestone_name: String = _milestone_labels.get(new_milestone, "Unknown")
		_show_milestone_unlock(milestone_name)
		# Consume the notification.
		if _bridge.has_method("ConsumeKGMilestoneNotificationV0"):
			_bridge.call("ConsumeKGMilestoneNotificationV0")

	_kg_milestone = new_milestone
	_rebuild_verb_toolbar()


# Build verb toolbar showing unlocked/locked verbs.
func _rebuild_verb_toolbar() -> void:
	if _verb_section == null:
		return
	for child in _verb_section.get_children():
		child.queue_free()

	# All 7 verbs with their unlock milestone.
	var verbs: Array = [
		{"name": "View", "milestone": 0, "icon": "◉", "desc": "View discovered locations"},
		{"name": "Pin", "milestone": 1, "icon": "📌", "desc": "Pin discoveries for reference"},
		{"name": "Relate", "milestone": 2, "icon": "⟷", "desc": "See connections between nodes"},
		{"name": "Annotate", "milestone": 3, "icon": "✎", "desc": "Add notes to discoveries"},
		{"name": "Flag", "milestone": 4, "icon": "⚑", "desc": "Flag discoveries for FO review"},
		{"name": "Link", "milestone": 5, "icon": "⬡", "desc": "Manually link related nodes"},
		{"name": "Compare", "milestone": 6, "icon": "⇔", "desc": "Compare discoveries side-by-side"},
	]

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_verb_section.add_child(row)

	for verb in verbs:
		var is_unlocked: bool = verb["milestone"] <= _kg_milestone
		var btn := Button.new()
		btn.text = "%s %s" % [verb["icon"], verb["name"]]
		btn.custom_minimum_size = Vector2(70, 24)
		btn.disabled = not is_unlocked
		btn.tooltip_text = verb["desc"] if is_unlocked else "Unlock: %s milestone" % _milestone_labels.get(verb["milestone"], "?")

		if is_unlocked:
			btn.add_theme_color_override("font_color", UITheme.CYAN)
		else:
			btn.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
			btn.modulate.a = 0.5

		row.add_child(btn)


# Animate milestone unlock notification.
func _show_milestone_unlock(milestone_name: String) -> void:
	if _milestone_notification_label == null:
		return
	_milestone_notification_label.text = "NEW VERB UNLOCKED: %s" % milestone_name.to_upper()
	_milestone_notification_label.visible = true
	_milestone_notification_label.modulate.a = 0.0

	var tween := create_tween()
	tween.tween_property(_milestone_notification_label, "modulate:a", 1.0, 0.3)
	tween.tween_interval(3.0)
	tween.tween_property(_milestone_notification_label, "modulate:a", 0.5, 0.5)
	tween.tween_callback(func(): _milestone_notification_label.visible = false)
