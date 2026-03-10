# GATE.S11.GAME_FEEL.TOAST_SYS.001: Toast notification manager.
# GATE.S7.HUD_ARCH.TOAST_PRIORITY.001: Priority levels, color borders, bundling.
# Autoloaded as CanvasLayer so toasts render above 3D viewport and all UI.
# Usage: ToastManager.show_toast("Trade complete!", 3.0)
#        ToastManager.show_priority_toast("CONFISCATED!", "critical")
extends CanvasLayer

const MAX_VISIBLE_TOASTS := 4
const SLIDE_DURATION := 0.3
const FADE_OUT_DURATION := 0.2
const TOAST_MARGIN := 10

# GATE.S7.HUD_ARCH.TOAST_PRIORITY.001: Priority config.
# priority -> {color (left border), duration, persist (bool)}
const PRIORITY_CONFIG := {
	"critical": {"color": Color(1.0, 0.2, 0.2), "duration": 5.0, "persist": true},
	"warning": {"color": Color(1.0, 0.6, 0.1), "duration": 4.0, "persist": false},
	"info": {"color": Color(0.5, 0.7, 1.0), "duration": 3.0, "persist": false},
	"confirm": {"color": Color(0.3, 0.9, 0.4), "duration": 2.0, "persist": false},
}

var _container: VBoxContainer = null
# GATE.S7.HUD_ARCH.TOAST_PRIORITY.001: Bundle tracking (text -> {panel, count_label, count}).
var _active_bundles: Dictionary = {}

func _ready() -> void:
	layer = 120  # Above EmpireDashboard (100)

	# MarginContainer for top-right positioning
	var margin := MarginContainer.new()
	margin.name = "ToastMargin"
	margin.anchor_left = 1.0
	margin.anchor_right = 1.0
	margin.anchor_top = 0.0
	margin.anchor_bottom = 0.0
	margin.offset_left = -320
	margin.offset_right = -16
	margin.offset_top = 16
	margin.offset_bottom = 600
	margin.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(margin)

	_container = VBoxContainer.new()
	_container.name = "ToastContainer"
	_container.alignment = BoxContainer.ALIGNMENT_BEGIN
	_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_container.add_theme_constant_override("separation", TOAST_MARGIN)
	margin.add_child(_container)


func show_toast(text: String, duration: float = 3.0) -> void:
	show_priority_toast(text, "info", duration)


# GATE.S7.HUD_ARCH.TOAST_PRIORITY.001: Priority-aware toast with color border + bundling.
func show_priority_toast(text: String, priority: String = "info", duration_override: float = -1.0) -> void:
	if _container == null:
		return

	var cfg: Dictionary = PRIORITY_CONFIG.get(priority, PRIORITY_CONFIG["info"])
	var duration: float = duration_override if duration_override > 0 else float(cfg["duration"])

	# Bundling: if same text toast is already visible, increment count badge.
	if _active_bundles.has(text) and is_instance_valid(_active_bundles[text].get("panel")):
		var bundle: Dictionary = _active_bundles[text]
		bundle["count"] = int(bundle["count"]) + 1
		var count_lbl = bundle.get("count_label")
		if count_lbl and is_instance_valid(count_lbl):
			count_lbl.text = "x%d" % int(bundle["count"])
			count_lbl.visible = true
		return

	# Evict oldest if at capacity — fade out gracefully instead of instant remove.
	while _container.get_child_count() >= MAX_VISIBLE_TOASTS:
		var oldest := _container.get_child(0)
		var oldest_text: String = str(oldest.get_meta("toast_text", ""))
		_active_bundles.erase(oldest_text)
		_fade_and_remove(oldest, oldest_text)

	var toast := _create_priority_toast(text, cfg)
	_container.add_child(toast)

	# Track for bundling.
	var count_label = toast.get_node_or_null("HBox/CountLabel")
	_active_bundles[text] = {"panel": toast, "count_label": count_label, "count": 1}

	# Animate: slide in from right + fade in
	toast.modulate.a = 0.0
	toast.position.x = 60.0
	var tween := create_tween()
	tween.set_parallel(true)
	tween.tween_property(toast, "modulate:a", 1.0, SLIDE_DURATION)
	tween.tween_property(toast, "position:x", 0.0, SLIDE_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

	# Schedule fade out after duration (unless persistent critical toast)
	if not bool(cfg.get("persist", false)):
		var fade_tween := create_tween()
		fade_tween.tween_interval(duration)
		fade_tween.tween_property(toast, "modulate:a", 0.0, SLIDE_DURATION)
		fade_tween.tween_callback(_remove_toast.bind(toast, text))
	else:
		# Persistent toasts fade out after extended duration (15s)
		var fade_tween := create_tween()
		fade_tween.tween_interval(15.0)
		fade_tween.tween_property(toast, "modulate:a", 0.0, SLIDE_DURATION)
		fade_tween.tween_callback(_remove_toast.bind(toast, text))


func _remove_toast(toast, text: String = "") -> void:
	if is_instance_valid(toast) and toast.get_parent() == _container:
		_container.remove_child(toast)
		toast.queue_free()
	if not text.is_empty():
		_active_bundles.erase(text)


## Fade out an evicted toast over FADE_OUT_DURATION then free it.
## Immediately removes the toast from _container (so child count decreases
## synchronously for the while-loop guard) and queues it for freeing.
## The brief fade gives visual feedback that the toast was dismissed.
func _fade_and_remove(toast, text: String = "") -> void:
	if not is_instance_valid(toast):
		return
	# Snapshot position before removal from VBoxContainer layout.
	var pos: Vector2 = toast.global_position
	# Remove from _container so get_child_count() decreases immediately.
	_container.remove_child(toast)
	# Re-parent under the MarginContainer's parent (this CanvasLayer) at the
	# same screen position so the fade is visible in-place.
	add_child(toast)
	toast.global_position = pos
	# Animate fade-out, then free.
	var tween := create_tween()
	tween.tween_property(toast, "modulate:a", 0.0, FADE_OUT_DURATION)
	tween.tween_callback(toast.queue_free)


func _create_priority_toast(text: String, cfg: Dictionary) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.set_meta("toast_text", text)

	# Use base toast style.
	panel.add_theme_stylebox_override("panel", UITheme.make_panel_toast())

	# HBox: color border + text + count badge
	var hbox := HBoxContainer.new()
	hbox.name = "HBox"
	hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_theme_constant_override("separation", 6)
	panel.add_child(hbox)

	# Left color border strip.
	var border := ColorRect.new()
	border.name = "PriorityBorder"
	border.custom_minimum_size = Vector2(4, 0)
	border.color = Color(cfg.get("color", Color.WHITE))
	border.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(border)

	# Text label.
	var label := Label.new()
	label.text = text
	label.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.custom_minimum_size = Vector2(220, 0)
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(label)

	# Count badge (hidden until bundled).
	var count_label := Label.new()
	count_label.name = "CountLabel"
	count_label.text = ""
	count_label.visible = false
	count_label.add_theme_color_override("font_color", Color(1.0, 0.8, 0.2))
	count_label.add_theme_font_size_override("font_size", 11)
	count_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(count_label)

	return panel
