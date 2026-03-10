extends Camera3D

# Self-contained pan/zoom for the galaxy overlay camera.
# Active only when this camera is the current camera (overlay open).
# GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001: Altitude accessor for semantic zoom.

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

func _process(delta):
	if not current:
		return
	var pan_speed: float = 40.0 * delta
	var right := global_transform.basis.x
	var fwd := -global_transform.basis.z
	# Project to XZ plane so pan is horizontal regardless of camera pitch
	var pan_right := Vector3(right.x, 0, right.z).normalized()
	var pan_fwd := Vector3(fwd.x, 0, fwd.z).normalized()
	var move := Vector3.ZERO
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		move += pan_fwd * pan_speed
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		move -= pan_fwd * pan_speed
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		move -= pan_right * pan_speed
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		move += pan_right * pan_speed
	if move.length() > 0.01:
		global_position += move

func _unhandled_input(event):
	if not current:
		return
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.pressed:
			var zoom_dir: Vector3 = -global_transform.basis.z
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
				global_position += zoom_dir * 5.0
				get_viewport().set_input_as_handled()
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				global_position -= zoom_dir * 5.0
				get_viewport().set_input_as_handled()
