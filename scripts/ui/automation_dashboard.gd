# GATE.S7.AUTOMATION_MGMT.DASHBOARD_V2.001: Automation management dashboard panel.
# Shows program performance, failure tracking with per-reason detail & suggested fixes,
# per-fleet performance summary (credits, goods, cycles, success rate, history),
# budget status, and doctrine settings per fleet.
# Wired to SimBridge.Automation.cs query contracts.
# Instantiated by hud.gd; toggled via toggle_v0().
extends PanelContainer

var _bridge: Object = null
var _fleet_id := "fleet_trader_1"

# UI sections
var _title_label: Label = null
var _perf_section: VBoxContainer = null
var _perf_history_section: VBoxContainer = null
var _failure_section: VBoxContainer = null
var _failure_detail_section: VBoxContainer = null
var _doctrine_section: VBoxContainer = null
var _budget_section: VBoxContainer = null
var _doctrine_panel = null  # Lazy-created DoctrinePanel
var _budget_panel = null  # Lazy-created BudgetPanel

const PANEL_WIDTH := 360.0
const PANEL_RIGHT_MARGIN := 16.0
const PANEL_TOP_MARGIN := 16.0

## Failure reason → suggested fix mapping for the Failure Detail panel.
const FAILURE_FIX_SUGGESTIONS := {
	"InsufficientFunds": "Increase budget or reduce program scope",
	"NoRoute": "Check fleet location, ensure lane access",
	"TargetGone": "Target no longer exists, reassign program",
	"Timeout": "Program exceeded time limit, check route length",
	"BudgetExceeded": "Raise budget caps in Budget panel",
}


func _ready() -> void:
	name = "AutomationDashboard"
	visible = false
	mouse_filter = Control.MOUSE_FILTER_STOP

	# Panel style: dark bg with blue accent border (standard panel pattern).
	var style := UITheme.make_panel_standard(UITheme.BORDER_ACCENT)
	add_theme_stylebox_override("panel", style)

	custom_minimum_size = Vector2(PANEL_WIDTH, 0)

	_bridge = get_tree().root.get_node_or_null("GameManager")

	_build_ui()
	_position_panel()


func _build_ui() -> void:
	var vbox := VBoxContainer.new()
	vbox.name = "MainVBox"
	vbox.add_theme_constant_override("separation", UITheme.SPACE_MD)
	add_child(vbox)

	# Title
	_title_label = Label.new()
	_title_label.text = "AUTOMATION MANAGEMENT"
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	_title_label.add_theme_color_override("font_color", UITheme.CYAN)
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_title_label)

	vbox.add_child(HSeparator.new())

	# ── Performance Summary section ──
	_add_section_header(vbox, "FLEET PERFORMANCE SUMMARY")
	_perf_section = VBoxContainer.new()
	_perf_section.name = "PerfSection"
	_perf_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_perf_section)

	# ── Performance History sub-section ──
	_perf_history_section = VBoxContainer.new()
	_perf_history_section.name = "PerfHistorySection"
	_perf_history_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_perf_history_section)

	vbox.add_child(HSeparator.new())

	# ── Budget section ──
	_add_section_header(vbox, "BUDGET STATUS")
	_budget_section = VBoxContainer.new()
	_budget_section.name = "BudgetSection"
	_budget_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_budget_section)

	vbox.add_child(HSeparator.new())

	# ── Failure Tracking section ──
	_add_section_header(vbox, "FAILURE TRACKING")
	_failure_section = VBoxContainer.new()
	_failure_section.name = "FailureSection"
	_failure_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_failure_section)

	# ── Failure Detail sub-section (per-reason breakdown + suggested fixes) ──
	_failure_detail_section = VBoxContainer.new()
	_failure_detail_section.name = "FailureDetailSection"
	_failure_detail_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_failure_detail_section)

	vbox.add_child(HSeparator.new())

	# ── Doctrine section ──
	_add_section_header(vbox, "FLEET DOCTRINE")
	_doctrine_section = VBoxContainer.new()
	_doctrine_section.name = "DoctrineSection"
	_doctrine_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	vbox.add_child(_doctrine_section)


## Toggle panel visibility. Refreshes data on show.
func toggle_v0() -> void:
	visible = !visible
	if visible:
		_position_panel()
		refresh_v0()


## Refresh all sections from SimBridge.
func refresh_v0() -> void:
	if _bridge == null:
		_bridge = get_tree().root.get_node_or_null("GameManager")
	if _bridge == null:
		return
	_refresh_performance()
	_refresh_budget()
	_refresh_failures()
	_refresh_failure_details()
	_refresh_doctrine()


# ── Performance Summary ──
func _refresh_performance() -> void:
	_clear_section(_perf_section)
	_clear_section(_perf_history_section)
	if not _bridge.has_method("GetProgramPerformanceV0"):
		_add_row(_perf_section, "No bridge method", "", UITheme.TEXT_DISABLED)
		return
	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", _fleet_id)
	if data.is_empty():
		_add_row(_perf_section, "No data available", "", UITheme.TEXT_DISABLED)
		return

	var cycles_run: int = int(data.get("cycles_run", 0))
	var goods_moved: int = int(data.get("goods_moved", 0))
	var credits_earned: int = int(data.get("credits_earned", 0))
	var failures: int = int(data.get("failures", 0))

	# Success rate calculation
	var success_rate := 0.0
	if cycles_run > 0:
		success_rate = 100.0 * float(cycles_run - failures) / float(cycles_run)
	var rate_color: Color
	if success_rate >= 90.0:
		rate_color = UITheme.GREEN
	elif success_rate >= 70.0:
		rate_color = UITheme.ORANGE
	else:
		rate_color = UITheme.RED

	_add_row(_perf_section, "Cycles Run", str(cycles_run))
	_add_row(_perf_section, "Goods Moved", str(goods_moved), UITheme.GREEN)
	_add_row(_perf_section, "Credits Earned", str(credits_earned), UITheme.GOLD)
	var fail_color: Color = UITheme.RED if failures > 0 else UITheme.TEXT_PRIMARY
	_add_row(_perf_section, "Failures", str(failures), fail_color)
	_add_row(_perf_section, "Success Rate", "%.1f%%" % success_rate, rate_color)
	_add_row(_perf_section, "Last Active Tick", str(data.get("last_active_tick", 0)), UITheme.TEXT_MUTED)

	# ── Recent History ──
	var history: Array = data.get("history", [])
	if history.size() > 0:
		var hist_header := Label.new()
		hist_header.text = "Recent Activity:"
		hist_header.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		hist_header.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		_perf_history_section.add_child(hist_header)

		for entry in history:
			var tick: int = int(entry.get("tick", 0))
			var success: bool = entry.get("success", false)
			var h_goods: int = int(entry.get("goods_moved", 0))
			var h_credits: int = int(entry.get("credits_earned", 0))
			var h_reason: String = str(entry.get("failure_reason", ""))

			if success:
				var detail := "+%dc / %dg" % [h_credits, h_goods]
				_add_row(_perf_history_section, "  T%d  OK" % tick, detail, UITheme.GREEN)
			else:
				_add_row(_perf_history_section, "  T%d  FAIL" % tick, h_reason, UITheme.RED)


# ── Budget ──
func _refresh_budget() -> void:
	_clear_section(_budget_section)
	if not _bridge.has_method("GetProgramPerformanceV0"):
		_add_row(_budget_section, "No bridge method", "", UITheme.TEXT_DISABLED)
		return
	var data: Dictionary = _bridge.call("GetProgramPerformanceV0", _fleet_id)
	if data.is_empty():
		_add_row(_budget_section, "No data available", "", UITheme.TEXT_DISABLED)
		return

	var credit_cap: int = int(data.get("budget_credit_cap", 0))
	var goods_cap: int = int(data.get("budget_goods_cap", 0))
	var spent_credits: int = int(data.get("spent_credits_this_cycle", 0))
	var spent_goods: int = int(data.get("spent_goods_this_cycle", 0))

	var credit_cap_str := "unlimited" if credit_cap == 0 else str(credit_cap)
	var goods_cap_str := "unlimited" if goods_cap == 0 else str(goods_cap)

	var credit_color: Color = UITheme.ORANGE if credit_cap > 0 and spent_credits >= credit_cap else UITheme.TEXT_PRIMARY
	var goods_color: Color = UITheme.ORANGE if goods_cap > 0 and spent_goods >= goods_cap else UITheme.TEXT_PRIMARY

	_add_row(_budget_section, "Credits Spent", "%d / %s" % [spent_credits, credit_cap_str], credit_color)
	_add_row(_budget_section, "Goods Spent", "%d / %s" % [spent_goods, goods_cap_str], goods_color)

	var edit_btn := Button.new()
	edit_btn.text = "Edit Budget"
	edit_btn.custom_minimum_size = Vector2(120, 28)
	edit_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	edit_btn.pressed.connect(_on_edit_budget_pressed)
	_budget_section.add_child(edit_btn)


# ── Failures ──
func _refresh_failures() -> void:
	_clear_section(_failure_section)
	if not _bridge.has_method("GetProgramFailureReasonsV0"):
		_add_row(_failure_section, "No bridge method", "", UITheme.TEXT_DISABLED)
		return
	var data: Dictionary = _bridge.call("GetProgramFailureReasonsV0", _fleet_id)
	if data.is_empty():
		_add_row(_failure_section, "No data available", "", UITheme.TEXT_DISABLED)
		return

	var total: int = int(data.get("total_failures", 0))
	var consec: int = int(data.get("consecutive_failures", 0))
	var last_reason: String = str(data.get("last_failure_reason", "None"))

	_add_row(_failure_section, "Total Failures", str(total), UITheme.RED if total > 0 else UITheme.TEXT_PRIMARY)
	var consec_color: Color = UITheme.RED if consec >= 3 else (UITheme.ORANGE if consec > 0 else UITheme.TEXT_PRIMARY)
	_add_row(_failure_section, "Consecutive", str(consec), consec_color)
	_add_row(_failure_section, "Last Reason", last_reason, UITheme.TEXT_INFO)


# ── Failure Detail Panel (per-reason breakdown + suggested fixes) ──
func _refresh_failure_details() -> void:
	_clear_section(_failure_detail_section)
	if not _bridge.has_method("GetProgramFailureReasonsV0"):
		return
	var data: Dictionary = _bridge.call("GetProgramFailureReasonsV0", _fleet_id)
	if data.is_empty():
		return

	var breakdown: Dictionary = data.get("failure_breakdown", {})
	if breakdown.is_empty():
		_add_row(_failure_detail_section, "No failures recorded", "", UITheme.GREEN)
		return

	var detail_header := Label.new()
	detail_header.text = "Failure Detail:"
	detail_header.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	detail_header.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_failure_detail_section.add_child(detail_header)

	for reason in breakdown:
		var count: int = int(breakdown[reason])
		var reason_str: String = str(reason)

		# Reason row: name and count in red
		_add_row(_failure_detail_section, "  %s" % reason_str, "x%d" % count, UITheme.RED)

		# Suggested fix row: indented hint text in muted color
		var fix_text: String = FAILURE_FIX_SUGGESTIONS.get(reason_str, "Review program configuration")
		var fix_label := Label.new()
		fix_label.text = "    > %s" % fix_text
		fix_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		fix_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		fix_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		fix_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_failure_detail_section.add_child(fix_label)


# ── Doctrine ──
func _refresh_doctrine() -> void:
	_clear_section(_doctrine_section)
	if not _bridge.has_method("GetDoctrineSettingsV0"):
		_add_row(_doctrine_section, "No bridge method", "", UITheme.TEXT_DISABLED)
		return
	var data: Dictionary = _bridge.call("GetDoctrineSettingsV0", _fleet_id)
	if data.is_empty():
		_add_row(_doctrine_section, "No data available", "", UITheme.TEXT_DISABLED)
		return

	var stance: String = str(data.get("stance", "Unknown"))
	var stance_color: Color = UITheme.CYAN
	match stance.to_lower():
		"aggressive": stance_color = UITheme.RED
		"defensive": stance_color = UITheme.ORANGE
		"passive": stance_color = UITheme.GREEN
		"evasive": stance_color = UITheme.YELLOW

	_add_row(_doctrine_section, "Stance", stance, stance_color)
	_add_row(_doctrine_section, "Retreat Threshold", "%d%% hull" % int(data.get("retreat_threshold_pct", 25)))
	_add_row(_doctrine_section, "Patrol Radius", "%.0f" % float(data.get("patrol_radius", 50.0)))

	var edit_btn := Button.new()
	edit_btn.text = "Edit Doctrine"
	edit_btn.custom_minimum_size = Vector2(120, 28)
	edit_btn.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	edit_btn.pressed.connect(_on_edit_doctrine_pressed)
	_doctrine_section.add_child(edit_btn)


# ── Helpers ──

## Position panel at top-right of viewport.
func _position_panel() -> void:
	var vp_size := get_viewport_rect().size
	position = Vector2(vp_size.x - PANEL_WIDTH - PANEL_RIGHT_MARGIN, PANEL_TOP_MARGIN)


## Clear all children from a section container.
func _clear_section(section: VBoxContainer) -> void:
	for child in section.get_children():
		child.queue_free()


## Add a section header label.
func _add_section_header(parent: VBoxContainer, text: String) -> void:
	var label := Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	parent.add_child(label)


## Add a key-value row as an HBox with left-aligned key and right-aligned value.
func _add_row(parent: VBoxContainer, key: String, value: String, value_color: Color = UITheme.TEXT_PRIMARY) -> void:
	var hbox := HBoxContainer.new()
	hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var key_label := Label.new()
	key_label.text = key
	key_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	key_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	key_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	key_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(key_label)

	var val_label := Label.new()
	val_label.text = value
	val_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	val_label.add_theme_color_override("font_color", value_color)
	val_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	val_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(val_label)

	parent.add_child(hbox)


## Open the doctrine editing panel (lazy-created).
func _on_edit_doctrine_pressed() -> void:
	if _doctrine_panel == null:
		var DoctrinePanel := load("res://scripts/ui/doctrine_panel.gd")
		_doctrine_panel = DoctrinePanel.new()
		add_child(_doctrine_panel)
	_doctrine_panel.show_for_fleet(_fleet_id)


## Open the budget editing panel (lazy-created).
func _on_edit_budget_pressed() -> void:
	if _budget_panel == null:
		var BudgetPanel := load("res://scripts/ui/budget_panel.gd")
		_budget_panel = BudgetPanel.new()
		_budget_panel.budget_applied.connect(refresh_v0)
		add_child(_budget_panel)
	_budget_panel.show_for_fleet(_fleet_id)
