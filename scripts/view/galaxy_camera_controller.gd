extends Camera3D

# Self-contained pan/zoom for the galaxy overlay camera.
# Active only when this camera is the current camera (overlay open).
# GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001: Altitude accessor for semantic zoom.
# Pan: click-drag (left or middle mouse). WASD disabled — ship controls only.

## Current altitude (Y-component of global_position). Used by GalaxyView semantic zoom.
func get_altitude_v0() -> float:
	return global_position.y

## Semantic zoom band: 0 = close (<500), 1 = medium (500-2000), 2 = galaxy (>2000).
func get_zoom_band_v0() -> int:
	var alt := global_position.y
	if alt < 500.0:
		return 0
	elif alt < 2000.0:
		return 1
	else:
		return 2

var _dragging: bool = false
var _drag_button: int = 0

func _unhandled_input(event):
	if not current:
		return

	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.pressed:
			# Scroll wheel zoom (unchanged).
			var zoom_dir: Vector3 = -global_transform.basis.z
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
				global_position += zoom_dir * 5.0
				get_viewport().set_input_as_handled()
				return
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				global_position -= zoom_dir * 5.0
				get_viewport().set_input_as_handled()
				return
			# Start drag on left or middle click.
			if mb.button_index == MOUSE_BUTTON_LEFT or mb.button_index == MOUSE_BUTTON_MIDDLE:
				_dragging = true
				_drag_button = mb.button_index
				get_viewport().set_input_as_handled()
		else:
			# Release drag.
			if mb.button_index == _drag_button:
				_dragging = false
				get_viewport().set_input_as_handled()

	elif event is InputEventMouseMotion and _dragging:
		var motion := event as InputEventMouseMotion
		# Pan speed scales with altitude so dragging feels consistent at any zoom.
		var alt := global_position.y
		var speed := alt * 0.002
		var right := global_transform.basis.x
		var fwd := -global_transform.basis.z
		var pan_right := Vector3(right.x, 0, right.z).normalized()
		var pan_fwd := Vector3(fwd.x, 0, fwd.z).normalized()
		# Invert so dragging "grabs" the map (drag left → map moves right → camera moves left).
		global_position -= pan_right * motion.relative.x * speed
		global_position += pan_fwd * motion.relative.y * speed
		get_viewport().set_input_as_handled()
