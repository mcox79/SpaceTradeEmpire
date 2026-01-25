extends Node

enum Mode { PILOT, MAP }

@export var start_mode: int = Mode.PILOT

var _mode: int = Mode.PILOT
var _player: Node = null
var _map_camera: Camera3D = null
var _drone_camera: Camera3D = null

func _ready() -> void:
_mode = start_mode
_map_camera = get_tree().current_scene.get_node_or_null("MapCamera")
_drone_camera = get_tree().current_scene.get_node_or_null("DroneCamera")

_player = _find_player()
_apply_mode()

get_tree().node_added.connect(_on_node_added)

func _on_node_added(n: Node) -> void:
if _player == null and n.is_in_group("player"):
_player = n
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
if nodes.size() > 0:
return nodes[0]
return null