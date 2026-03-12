# GATE.S9.ACCESSIBILITY.COLORBLIND.001: Full-screen colorblind simulation filter.
# CanvasLayer (layer 125) with a full-screen ColorRect using colorblind_filter.gdshader.
# Reads mode from SettingsManager key "accessibility_colorblind_mode" (int: 0-3).
# Updates on settings_changed signal.
extends CanvasLayer

var _rect: ColorRect = null
var _material: ShaderMaterial = null


func _ready() -> void:
	layer = 125  # Above settings panel (120) but acts as post-process
	var shader := preload("res://scripts/view/colorblind_filter.gdshader")
	_material = ShaderMaterial.new()
	_material.shader = shader

	_rect = ColorRect.new()
	_rect.name = "ColorblindRect"
	_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_rect.material = _material
	add_child(_rect)

	# Read initial mode from settings.
	var mgr = get_node_or_null("/root/SettingsManager")
	if mgr and mgr.has_method("get_setting"):
		var mode_val: int = int(mgr.get_setting("accessibility_colorblind_mode"))
		_apply_mode(mode_val)
	if mgr and mgr.has_signal("settings_changed"):
		mgr.settings_changed.connect(_on_settings_changed)

	print("UUIR|COLORBLIND_FILTER|READY")


func _apply_mode(mode: int) -> void:
	var clamped := clampi(mode, 0, 3)
	if _material:
		_material.set_shader_parameter("mode", clamped)
	# Hide rect entirely when mode is 0 (no filter) to save fillrate.
	if _rect:
		_rect.visible = (clamped != 0)
	print("UUIR|COLORBLIND_FILTER|MODE=%d" % clamped)


func _on_settings_changed(key: String, value: Variant) -> void:
	if key == "accessibility_colorblind_mode":
		_apply_mode(int(value))
