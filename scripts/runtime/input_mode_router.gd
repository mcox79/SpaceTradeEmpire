extends Node
class_name InputModeRouter

enum Mode { PILOT, MAP, DOCKED }

@export var start_mode: Mode = Mode.PILOT
@export var map_camera_name: String = "MapCamera"
@export var drone_camera_name: String = "DroneCamera"

var _mode: Mode
var _player: Node = null
var _map_camera: Camera3D = null
var _drone_camera: Camera3D = null

func _ready() -> void:
	_mode = start_mode
	_map_camera = get_tree().current_scene.get_node_or_null(map_camera_name)
	_drone_camera = get_tree().current_scene.get_node_or_null(drone_camera_name)
	_player = _find_player()
	_apply_mode()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_TAB:
			_mode = Mode.MAP if _mode == Mode.PILOT else Mode.PILOT
			_apply_mode()

func _apply_mode() -> void:
	var pilot := (_mode == Mode.PILOT)
	var map := (_mode == Mode.MAP)

	if _map_camera:
		_map_camera.set_process(map)
		_map_camera.set_process_unhandled_input(map)
		_map_camera.current = map

	if _drone_camera:
		_drone_camera.set_process(pilot)
		_drone_camera.current = pilot

	if _player and _player.has_method("set_input_enabled"):
		_player.call("set_input_enabled", pilot)

func _find_player() -> Node:
	var nodes := get_tree().get_nodes_in_group("player")
	return nodes[0] if nodes.size() > 0 else null
