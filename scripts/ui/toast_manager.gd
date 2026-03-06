# GATE.S11.GAME_FEEL.TOAST_SYS.001: Toast notification manager.
# Autoloaded as CanvasLayer so toasts render above 3D viewport and all UI.
# Usage: ToastManager.show_toast("Trade complete!", 3.0)
extends CanvasLayer

const MAX_VISIBLE_TOASTS := 5
const SLIDE_DURATION := 0.3
const TOAST_MARGIN := 8

var _container: VBoxContainer = null

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
	if _container == null:
		return

	# Evict oldest if at capacity
	while _container.get_child_count() >= MAX_VISIBLE_TOASTS:
		var oldest := _container.get_child(0)
		_container.remove_child(oldest)
		oldest.queue_free()

	var toast := _create_toast(text)
	_container.add_child(toast)

	# Animate: slide in from right + fade in
	toast.modulate.a = 0.0
	toast.position.x = 60.0
	var tween := create_tween()
	tween.set_parallel(true)
	tween.tween_property(toast, "modulate:a", 1.0, SLIDE_DURATION)
	tween.tween_property(toast, "position:x", 0.0, SLIDE_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

	# Schedule fade out after duration
	var fade_tween := create_tween()
	fade_tween.tween_interval(duration)
	fade_tween.tween_property(toast, "modulate:a", 0.0, SLIDE_DURATION)
	fade_tween.tween_callback(_remove_toast.bind(toast))


func _remove_toast(toast: Control) -> void:
	if is_instance_valid(toast) and toast.get_parent() == _container:
		_container.remove_child(toast)
		toast.queue_free()


func _create_toast(text: String) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE

	panel.add_theme_stylebox_override("panel", UITheme.make_panel_toast())

	var label := Label.new()
	label.text = text
	label.add_theme_color_override("font_color", UITheme.TEXT_WHITE)
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.custom_minimum_size = Vector2(250, 0)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.add_child(label)

	return panel
