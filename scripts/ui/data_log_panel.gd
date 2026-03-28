# scripts/ui/data_log_panel.gd
# GATE.T18.NARRATIVE.UI_DATALOG.001: Data log viewer panel.
# Shows collected logs list with read/unread indicators, log content display,
# and search/filter by category. Accessible via L key or HUD button.
extends PanelContainer

var _bridge = null
var _list_container: VBoxContainer = null
var _detail_container: VBoxContainer = null
var _detail_title: Label = null
var _detail_content: RichTextLabel = null
var _search_edit: LineEdit = null
var _back_btn: Button = null
var _filter_bar: HBoxContainer = null
var _filter_buttons: Array = []
var _showing_detail: bool = false
var _current_filter: String = ""
var _active_thread_filter: String = ""  # empty = show all threads

func _ready() -> void:
	name = "DataLogPanel"
	visible = false
	set_anchors_preset(Control.PRESET_CENTER)
	offset_left = -300
	offset_right = 300
	offset_top = -250
	offset_bottom = 250
	custom_minimum_size = Vector2(600, 500)
	# GATE.T41.UI.PANEL_CHROME.001: Use shared panel chrome for consistent panel frame.
	var style := UITheme.make_panel_chrome()
	add_theme_stylebox_override("panel", style)

	var root_vbox := VBoxContainer.new()
	root_vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	add_child(root_vbox)

	# Header row.
	var header := HBoxContainer.new()
	header.add_theme_constant_override("separation", UITheme.SPACE_LG)
	root_vbox.add_child(header)

	var title := Label.new()
	title.text = "DATA LOGS"
	# GATE.T41.UI.FONT_HIERARCHY.001: Use header font helper.
	UITheme.apply_header_font(title)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(title)

	_back_btn = Button.new()
	_back_btn.text = "Back"
	_back_btn.visible = false
	_back_btn.pressed.connect(_on_back_pressed)
	header.add_child(_back_btn)

	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.pressed.connect(func(): toggle_v0())
	header.add_child(close_btn)

	# Search bar.
	_search_edit = LineEdit.new()
	_search_edit.placeholder_text = "Search logs..."
	_search_edit.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_search_edit.text_changed.connect(_on_search_changed)
	root_vbox.add_child(_search_edit)

	# GATE.X.UI_POLISH.DATALOG_FILTER.001: Thread category filter bar.
	_filter_bar = HBoxContainer.new()
	_filter_bar.add_theme_constant_override("separation", UITheme.SPACE_XS)
	root_vbox.add_child(_filter_bar)

	var thread_categories: Array = ["All", "Containment", "Lattice", "Departure", "Accommodation", "Warning", "EconTopology"]
	for cat in thread_categories:
		var btn := Button.new()
		btn.text = cat
		btn.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		btn.pressed.connect(_on_thread_filter_pressed.bind("" if cat == "All" else cat))
		_filter_buttons.append({"button": btn, "thread": "" if cat == "All" else cat})
		_filter_bar.add_child(btn)
	_update_filter_button_styles()

	# Log list (scrollable).
	var list_scroll := ScrollContainer.new()
	list_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root_vbox.add_child(list_scroll)

	_list_container = VBoxContainer.new()
	_list_container.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_list_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list_scroll.add_child(_list_container)

	# Detail view (hidden until a log is selected).
	_detail_container = VBoxContainer.new()
	_detail_container.visible = false
	_detail_container.add_theme_constant_override("separation", UITheme.SPACE_SM)
	root_vbox.add_child(_detail_container)

	_detail_title = Label.new()
	_detail_title.text = ""
	_detail_title.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_detail_title.add_theme_color_override("font_color", UITheme.GOLD)
	_detail_container.add_child(_detail_title)

	var detail_scroll := ScrollContainer.new()
	detail_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_detail_container.add_child(detail_scroll)

	_detail_content = RichTextLabel.new()
	_detail_content.bbcode_enabled = true
	_detail_content.fit_content = true
	_detail_content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_detail_content.add_theme_font_size_override("normal_font_size", UITheme.FONT_BODY)
	_detail_content.add_theme_color_override("default_color", UITheme.TEXT_PRIMARY)
	detail_scroll.add_child(_detail_content)

	# Footer hint — navigate up to root_vbox
	var _hint_vbox: Node = detail_scroll.get_parent()
	if _hint_vbox:
		_hint_vbox.add_child(UITheme.make_dismiss_hint("D"))

	_bridge = get_node_or_null("/root/SimBridge")

func toggle_v0() -> void:
	if visible:
		UITheme.animate_close(self, func(): visible = false)
	else:
		visible = true
		UITheme.animate_open(self)
		_showing_detail = false
		_show_list()
		_refresh_list()

func _show_list() -> void:
	_showing_detail = false
	_list_container.get_parent().visible = true
	_search_edit.visible = true
	_filter_bar.visible = true
	_detail_container.visible = false
	_back_btn.visible = false

func _show_detail(log_id: String) -> void:
	_showing_detail = true
	_list_container.get_parent().visible = false
	_search_edit.visible = false
	_filter_bar.visible = false
	_detail_container.visible = true
	_back_btn.visible = true
	_load_detail(log_id)

func _on_back_pressed() -> void:
	_show_list()

func _on_search_changed(new_text: String) -> void:
	_current_filter = new_text.to_lower()
	_refresh_list()

func _refresh_list() -> void:
	for child in _list_container.get_children():
		child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetDiscoveredDataLogsV0"):
		return

	var logs: Array = _bridge.call("GetDiscoveredDataLogsV0")
	for log_entry in logs:
		var log_id: String = str(log_entry.get("log_id", ""))
		var thread: String = str(log_entry.get("thread", ""))
		var is_new: bool = log_entry.get("is_new", false)
		var tier: String = str(log_entry.get("tier", ""))

		# Apply thread category filter.
		if not _active_thread_filter.is_empty():
			if thread != _active_thread_filter:
				continue

		# Apply search filter.
		if not _current_filter.is_empty():
			var searchable: String = (log_id + " " + thread + " " + tier).to_lower()
			if searchable.find(_current_filter) == -1:
				continue

		var row := Button.new()
		row.alignment = HORIZONTAL_ALIGNMENT_LEFT
		var indicator: String = "[NEW] " if is_new else ""
		row.text = "%s%s — %s" % [indicator, thread, log_id]
		row.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		if is_new:
			row.add_theme_color_override("font_color", UITheme.GOLD)
		else:
			row.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		row.pressed.connect(_on_log_selected.bind(log_id))
		_list_container.add_child(row)

	if _list_container.get_child_count() == 0:
		var empty := Label.new()
		empty.text = "No data logs discovered yet."
		empty.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_list_container.add_child(empty)

func _on_log_selected(log_id: String) -> void:
	_show_detail(log_id)

func _load_detail(log_id: String) -> void:
	if _bridge == null or not _bridge.has_method("GetDataLogDetailV0"):
		return
	var detail: Dictionary = _bridge.call("GetDataLogDetailV0", log_id)
	var thread: String = str(detail.get("thread", log_id))
	var tier: String = str(detail.get("tier", ""))
	_detail_title.text = "%s [%s]" % [thread, tier]

	var bbcode: String = ""
	var entries: Array = detail.get("entries", [])
	for entry in entries:
		var speaker: String = str(entry.get("speaker", ""))
		var text: String = str(entry.get("text", ""))
		var is_personal: bool = entry.get("is_personal", false)
		if is_personal:
			bbcode += "[color=#%s][i]%s: %s[/i][/color]\n\n" % [UITheme.PURPLE_LIGHT.to_html(false), speaker, text]
		else:
			bbcode += "[color=#%s]%s:[/color] %s\n\n" % [UITheme.CYAN.to_html(false), speaker, text]

	if bbcode.is_empty():
		bbcode = "[color=#%s]No entries.[/color]" % UITheme.TEXT_DISABLED.to_html(false)

	_detail_content.text = ""
	_detail_content.append_text(bbcode)

# GATE.X.UI_POLISH.DATALOG_FILTER.001: Thread filter toggle.
func _on_thread_filter_pressed(thread: String) -> void:
	if _active_thread_filter == thread:
		_active_thread_filter = ""  # Toggle off → show all.
	else:
		_active_thread_filter = thread
	_update_filter_button_styles()
	_refresh_list()

func _update_filter_button_styles() -> void:
	for entry in _filter_buttons:
		var btn: Button = entry["button"]
		var thr: String = entry["thread"]
		var is_active: bool = (_active_thread_filter == thr)
		if is_active:
			btn.add_theme_color_override("font_color", UITheme.CYAN)
			var active_style := StyleBoxFlat.new()
			active_style.bg_color = Color(UITheme.CYAN, 0.12)
			active_style.border_color = UITheme.CYAN
			active_style.border_width_bottom = 1
			active_style.border_width_top = 1
			active_style.border_width_left = 1
			active_style.border_width_right = 1
			active_style.content_margin_left = 4
			active_style.content_margin_right = 4
			active_style.content_margin_top = 2
			active_style.content_margin_bottom = 2
			btn.add_theme_stylebox_override("normal", active_style)
		else:
			btn.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
			btn.remove_theme_stylebox_override("normal")
