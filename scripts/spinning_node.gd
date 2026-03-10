extends Node3D

# GATE.S1.VISUAL_POLISH.STRUCTURES.001 — subtle slow rotation for station ring visual.
@export var spin_speed_y: float = 0.18   # radians/sec
@export var spin_speed_x: float = 0.0
@export var spin_speed_z: float = 0.0
@export var pause_when_docked: bool = true

var _game_manager = null

func _process(delta: float) -> void:
	if pause_when_docked:
		if _game_manager == null:
			_game_manager = get_node_or_null("/root/GameManager")
		if _game_manager and _game_manager.get("current_player_state") == 1:  # DOCKED
			return
	rotate_y(spin_speed_y * delta)
	if spin_speed_x != 0.0:
		rotate_x(spin_speed_x * delta)
	if spin_speed_z != 0.0:
		rotate_z(spin_speed_z * delta)
