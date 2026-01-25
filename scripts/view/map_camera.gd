extends Camera3D

@export var pan_speed: float = 70.0
@export var zoom_speed: float = 220.0
@export var min_height: float = 15.0
@export var max_height: float = 240.0
@export var boost_multiplier: float = 3.0

func _ready() -> void:
# Map camera is controlled by InputModeRouter. Default to inert.
set_process(false)
set_process_unhandled_input(false)
current = false

func _unhandled_input(event: InputEvent) -> void:
if event is InputEventMouseButton and event.pressed:
if event.button_index == MOUSE_BUTTON_WHEEL_UP:
_apply_zoom(-1.0)
elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
_apply_zoom(1.0)

func _process(delta: float) -> void:
# Arrow-only to avoid overlapping with ship controls (WASD).
var x := 0.0
var y := 0.0
if Input.is_key_pressed(KEY_LEFT): x -= 1.0
if Input.is_key_pressed(KEY_RIGHT): x += 1.0
if Input.is_key_pressed(KEY_UP): y += 1.0
if Input.is_key_pressed(KEY_DOWN): y -= 1.0

var boost := boost_multiplier if Input.is_key_pressed(KEY_SHIFT) else 1.0
var move := Vector3(x, 0.0, y)
if move.length() > 0.0:
move = move.normalized() * pan_speed * boost * delta
global_position += move

_clamp_height()

func _apply_zoom(dir: float) -> void:
# dir: +1 zoom out, -1 zoom in
global_position.y += dir * zoom_speed * 0.05
_clamp_height()

func _clamp_height() -> void:
global_position.y = clamp(global_position.y, min_height, max_height)