extends Node3D
class_name LaneTrafficSprite
## Billboarded ship sprite drifting along a hyperlane.
## economy_simulation_v0.md Category 4: Lane Traffic

## Lane endpoints in world space.
var _start_pos: Vector3 = Vector3.ZERO
var _end_pos: Vector3 = Vector3.ZERO
## Drift speed in units per second.
var _speed: float = 1.0

## Internal state.
var _progress: float = 0.0  # 0.0 = start, 1.0 = end
var _lateral_offset: Vector3 = Vector3.ZERO
var _lane_length: float = 0.0
var _lane_dir: Vector3 = Vector3.ZERO

## Internal refs.
var _sprite: Sprite3D = null


## Initialize the lane traffic sprite with start/end positions and speed.
func setup(start_pos: Vector3, end_pos: Vector3, speed: float) -> void:
	_start_pos = start_pos
	_end_pos = end_pos
	_speed = clampf(speed, 0.5, 2.0)

	var diff := _end_pos - _start_pos
	_lane_length = diff.length()
	_lane_dir = diff.normalized() if _lane_length > 0.001 else Vector3.FORWARD

	# Randomize starting progress so sprites spread along lane.
	_progress = randf()
	_randomize_lateral_offset()

	_build_sprite()


func _randomize_lateral_offset() -> void:
	# Perpendicular offset so sprites don't all travel the exact centerline.
	var perp := _lane_dir.cross(Vector3.UP).normalized()
	_lateral_offset = perp * randf_range(-0.3, 0.3)


func _build_sprite() -> void:
	_sprite = Sprite3D.new()
	_sprite.name = "TrafficSprite"
	_sprite.pixel_size = 0.01
	_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_sprite.modulate = Color(0.8, 0.85, 1.0, 0.6)
	_sprite.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	# Generate a tiny procedural diamond texture (4x4 white pixels, alpha shaped).
	var img := Image.create(4, 4, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))
	# Diamond shape: center pixels bright.
	img.set_pixel(1, 0, Color.WHITE)
	img.set_pixel(2, 0, Color.WHITE)
	img.set_pixel(0, 1, Color.WHITE)
	img.set_pixel(1, 1, Color.WHITE)
	img.set_pixel(2, 1, Color.WHITE)
	img.set_pixel(3, 1, Color.WHITE)
	img.set_pixel(0, 2, Color.WHITE)
	img.set_pixel(1, 2, Color.WHITE)
	img.set_pixel(2, 2, Color.WHITE)
	img.set_pixel(3, 2, Color.WHITE)
	img.set_pixel(1, 3, Color.WHITE)
	img.set_pixel(2, 3, Color.WHITE)
	var tex := ImageTexture.create_from_image(img)
	_sprite.texture = tex

	# Small scale for distant ship appearance.
	scale = Vector3(0.15, 0.15, 0.15)

	add_child(_sprite)


func _process(delta: float) -> void:
	if _lane_length < 0.001:
		return

	# Advance progress along lane.
	_progress += (delta * _speed) / _lane_length
	if _progress >= 1.0:
		_progress -= 1.0
		_randomize_lateral_offset()

	# Interpolate position along lane with lateral offset.
	global_position = _start_pos.lerp(_end_pos, _progress) + _lateral_offset
