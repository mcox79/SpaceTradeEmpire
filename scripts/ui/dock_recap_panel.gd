# scripts/ui/dock_recap_panel.gd
# GATE.T58.UI.DOCK_RECAP.001: "While You Were Away" dock recap overlay.
# Displays 3-line batch summary on dock arrival. Auto-dismiss after 5s or click.
# Wired to SimBridge GetDockRecapV0 / ConsumeDockRecapV0.
extends PanelContainer

var _bridge = null
var _title_label: Label = null
var _lines_vbox: VBoxContainer = null
var _dismiss_btn: Button = null
var _auto_dismiss_timer: float = -1.0
const _AUTO_DISMISS_SECONDS: float = 5.0  # STRUCTURAL: auto-dismiss timeout
var _fade_tween: Tween = null


func _ready() -> void:
	name = "DockRecapPanel"
	visible = false
	# Center-top positioning — overlays the dock menu.
	set_anchors_preset(Control.PRESET_CENTER_TOP)
	offset_left = -200
	offset_right = 200
	offset_top = 60
	offset_bottom = 260
	custom_minimum_size = Vector2(400, 0)

	var style := UITheme.make_panel_standard(UITheme.GOLD)
	style.bg_color.a = 0.95
	add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", UITheme.SPACE_SM)
	add_child(vbox)

	# Title: "WHILE YOU WERE AWAY"
	_title_label = Label.new()
	_title_label.text = "WHILE YOU WERE AWAY"
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_title_label.add_theme_color_override("font_color", UITheme.GOLD)
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_title_label)

	var sep := HSeparator.new()
	vbox.add_child(sep)

	# Recap lines container.
	_lines_vbox = VBoxContainer.new()
	_lines_vbox.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_lines_vbox)

	# Summary stats row.
	var stats_row := HBoxContainer.new()
	stats_row.name = "StatsRow"
	stats_row.add_theme_constant_override("separation", UITheme.SPACE_LG)
	vbox.add_child(stats_row)

	# Dismiss button.
	_dismiss_btn = Button.new()
	_dismiss_btn.text = "Acknowledged"
	_dismiss_btn.custom_minimum_size = Vector2(120, 28)
	_dismiss_btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	_dismiss_btn.pressed.connect(_dismiss)
	vbox.add_child(_dismiss_btn)

	_bridge = get_node_or_null("/root/SimBridge")


func _physics_process(delta: float) -> void:
	if not visible:
		return
	# Auto-dismiss countdown.
	if _auto_dismiss_timer > 0.0:
		_auto_dismiss_timer -= delta
		if _auto_dismiss_timer <= 0.0:
			_dismiss()


## Called by dock menu or game_manager when player docks.
func show_recap_v0() -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetDockRecapV0"):
		return

	var recap: Dictionary = _bridge.call("GetDockRecapV0")
	if not recap.get("pending", false):
		return

	var lines: Array = recap.get("lines", [])
	if lines.is_empty():
		return

	# Populate lines.
	for child in _lines_vbox.get_children():
		child.queue_free()

	for line_text in lines:
		var lbl := Label.new()
		lbl.text = "• " + str(line_text)
		lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_lines_vbox.add_child(lbl)

	# Stats summary.
	var trades: int = recap.get("trades_completed", 0)
	var credits: int = recap.get("credits_earned", 0)
	var stats_row = get_node_or_null("VBoxContainer/StatsRow")
	if stats_row == null:
		stats_row = find_child("StatsRow", true, false)
	if stats_row:
		for child in stats_row.get_children():
			child.queue_free()
		if trades > 0:
			var trade_lbl := Label.new()
			trade_lbl.text = "%d trades" % trades
			trade_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
			trade_lbl.add_theme_color_override("font_color", UITheme.CYAN)
			stats_row.add_child(trade_lbl)
		if credits != 0:
			var credit_lbl := Label.new()
			var sign: String = "+" if credits > 0 else ""
			credit_lbl.text = "%s%d cr" % [sign, credits]
			credit_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
			credit_lbl.add_theme_color_override("font_color", UITheme.GOLD if credits > 0 else UITheme.RED)
			stats_row.add_child(credit_lbl)

	# Show with fade-in.
	visible = true
	modulate.a = 0.0
	if _fade_tween:
		_fade_tween.kill()
	_fade_tween = create_tween()
	_fade_tween.tween_property(self, "modulate:a", 1.0, 0.3)

	# Start auto-dismiss timer.
	_auto_dismiss_timer = _AUTO_DISMISS_SECONDS


func _dismiss() -> void:
	# Consume the recap on the bridge side.
	if _bridge and _bridge.has_method("ConsumeDockRecapV0"):
		_bridge.call("ConsumeDockRecapV0")

	# Fade out.
	if _fade_tween:
		_fade_tween.kill()
	_fade_tween = create_tween()
	_fade_tween.tween_property(self, "modulate:a", 0.0, 0.2)
	_fade_tween.tween_callback(func(): visible = false)
	_auto_dismiss_timer = -1.0
