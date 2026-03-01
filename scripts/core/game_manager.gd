extends Node

# sim.gd (GDScript sim) is kept for galaxy_spawner 3D visual scaffolding ONLY.
# It must NOT be ticked or used for game logic. All game logic routes through SimBridge (C#).
# TAB key is a stub here; GATE.S1.GALAXY_MAP.RENDER.001 will rebind it to the galaxy overlay.
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')

var sim: Sim
var player: PlayerState

func _ready():
	print('SUCCESS: Global Game Manager initialized.')
	sim = Sim.new()
	player = PlayerState.new()

	# Bootstrap player start position from GDScript galaxy topology.
	# galaxy_spawner.gd reads game_manager.sim for 3D star/lane mesh generation.
	if sim.galaxy_map.stars.size() > 0:
		player.current_node_id = sim.galaxy_map.stars[0].id

func _unhandled_input(event):
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_TAB:
			toggle_market()

func toggle_market():
	# No-op stub. Station UI is driven by C# StationMenu via SimBridge.
	# GATE.S1.GALAXY_MAP.RENDER.001 will rebind TAB to the galaxy map overlay.
	return
