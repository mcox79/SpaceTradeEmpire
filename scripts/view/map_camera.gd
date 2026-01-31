extends Camera3D

# --- CONFIGURATION ---
@export var smooth_speed: float = 5.0
@export var offset: Vector3 = Vector3(0, 50, 30) # High angle top-down view
@export var min_height: float = 20.0
@export var max_height: float = 150.0
@export var zoom_step: float = 5.0

var _target: Node3D

func _ready() -> void:
    # Auto-acquire target from 'Player' group
    # This decouples the camera from the scene tree structure
    var players = get_tree().get_nodes_in_group("Player")
    if players.size() > 0:
        _target = players[0]
    else:
        print_rich("[color=yellow][CAMERA] No Player found in group 'Player'. Waiting...[/color]")

func _physics_process(delta: float) -> void:
    if not is_instance_valid(_target):
        _attempt_reacquire()
        return

    # 1. Calculate Target Position
    var desired_position = _target.global_position + offset
    
    # 2. Smoothly Interpolate (Damping)
    global_position = global_position.lerp(desired_position, smooth_speed * delta)
    
    # 3. Look at the ship (Keeps the action centered)
    look_at(_target.global_position, Vector3.UP)

func _unhandled_input(event: InputEvent) -> void:
    if event is InputEventMouseButton and event.pressed:
        if event.button_index == MOUSE_BUTTON_WHEEL_UP:
            _zoom(-1)
        elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
            _zoom(1)

func _zoom(direction: int) -> void:
    # Adjust Y and Z to maintain viewing angle while zooming
    var zoom_vector = offset.normalized() * zoom_step * direction
    var new_offset = offset + zoom_vector
    
    # Clamp Height
    if new_offset.y >= min_height and new_offset.y <= max_height:
        offset = new_offset

func _attempt_reacquire():
    var players = get_tree().get_nodes_in_group("Player")
    if players.size() > 0:
        _target = players[0]