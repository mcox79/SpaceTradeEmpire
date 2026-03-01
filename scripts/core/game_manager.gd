extends Node

# sim.gd (GDScript sim) is kept for galaxy_spawner 3D visual scaffolding ONLY.
# It must NOT be ticked or used for game logic. All game logic routes through SimBridge (C#).
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')

var sim: Sim
var player: PlayerState

# Proof helper: used by headless tests to verify the local scene continues ticking
# while the galaxy overlay is open. Do not print this value.
var time_accumulator: float = 0.0

# Galaxy overlay v0 wiring (CanvasLayer above local scene; no scene swap)
var galaxy_overlay_open: bool = false
var _galaxy_overlay_layer: CanvasLayer
var _galaxy_overlay_camera: Camera3D
var _galaxy_view: Node
var _prev_camera: Camera3D

func _ready():
	print('SUCCESS: Global Game Manager initialized.')
	sim = Sim.new()
	player = PlayerState.new()

	# Bootstrap player start position from GDScript galaxy topology.
	# galaxy_spawner.gd reads game_manager.sim for 3D star/lane mesh generation.
	if sim.galaxy_map.stars.size() > 0:
		player.current_node_id = sim.galaxy_map.stars[0].id

	# Scene-local wiring (GameManager is a child of Main in playable_prototype.tscn)
	var root = get_parent()
	_galaxy_overlay_layer = root.get_node_or_null("GalaxyOverlay")
	_galaxy_overlay_camera = root.get_node_or_null("GalaxyOverlayCamera")
	_galaxy_view = root.get_node_or_null("GalaxyView")

	if _galaxy_overlay_layer:
		_galaxy_overlay_layer.visible = false
	if _galaxy_overlay_camera:
		_galaxy_overlay_camera.current = false
	if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
		_galaxy_view.call("SetOverlayOpenV0", false)

func _process(delta):
	# Local ticking must continue while overlay is open. This is used only as a boolean check in tests.
	time_accumulator += float(delta)

func _unhandled_input(event):
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_TAB:
			toggle_galaxy_map_overlay_v0()

func toggle_market():
	# No-op stub. Station UI is driven by C# StationMenu via SimBridge.
	return

func toggle_galaxy_map_overlay_v0():
	galaxy_overlay_open = not galaxy_overlay_open

	if _galaxy_overlay_layer:
		_galaxy_overlay_layer.visible = galaxy_overlay_open

	# Camera switching: overlay uses a dedicated camera; restore previous camera on close.
	if galaxy_overlay_open:
		var active_cam = get_viewport().get_camera_3d()
		if active_cam and active_cam != _galaxy_overlay_camera:
			_prev_camera = active_cam
		if _galaxy_overlay_camera:
			_galaxy_overlay_camera.current = true
	else:
		if _galaxy_overlay_camera:
			_galaxy_overlay_camera.current = false
		if _prev_camera and is_instance_valid(_prev_camera):
			_prev_camera.current = true

	# GalaxyView rendering must be gated behind overlay-mode flag.
	if _galaxy_view and _galaxy_view.has_method("SetOverlayOpenV0"):
		_galaxy_view.call("SetOverlayOpenV0", galaxy_overlay_open)
