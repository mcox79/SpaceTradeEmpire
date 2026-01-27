extends Node

const TICK_INTERVAL = 0.5
const Sim = preload('res://scripts/core/sim/sim.gd')
const PlayerState = preload('res://scripts/core/state/player_state.gd')
const PlayerInteractionManager = preload('res://scripts/core/player_interaction_manager.gd')
const StationInterface = preload('res://scripts/view/ui/station_interface.gd')
const ContractBoard = preload('res://scripts/view/ui/contract_board.gd')
const Fleet = preload('res://scripts/core/state/fleet.gd')

var sim: Sim
var player: PlayerState
var interaction: PlayerInteractionManager
var ui_station: Control
var ui_contracts: Control
var time_accumulator: float = 0.0
var player_fleet_id: String = 'player_1'

func _ready():
	print('SUCCESS: Global Game Manager initialized.')
	sim = Sim.new()
	player = PlayerState.new()
	interaction = PlayerInteractionManager.new(player, sim)

	# BOOTSTRAP GALAXY
	if sim.galaxy_map.stars.size() > 0:
		var start_node = sim.galaxy_map.stars[0].id
		player.current_node_id = start_node

		# SPAWN PLAYER FLEET
		var p_fleet = Fleet.new(player_fleet_id, sim.galaxy_map.stars[0].pos)
		p_fleet.speed = 30.0
		sim.active_fleets.append(p_fleet)

	# INJECT UIs
	ui_station = StationInterface.new()
	add_child(ui_station)
	ui_station.setup(interaction, player.current_node_id)

	ui_contracts = ContractBoard.new()
	add_child(ui_contracts)
	ui_contracts.setup(self)

func _process(delta):
	# Process Selection (Mouse Click)
	_handle_selection()

	# Process Sim Tick
	if sim:
		time_accumulator += delta
		if time_accumulator >= TICK_INTERVAL:
			time_accumulator -= TICK_INTERVAL
			sim.advance()
			_sync_player_pos()
			if ui_station.visible: ui_station.refresh_market_list()
			if ui_contracts.visible: ui_contracts.refresh()

# FIX: Proper Input Handling
func _unhandled_input(event):
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_TAB:
			toggle_market()
		elif event.keycode == KEY_C:
			toggle_contracts()

func _handle_selection():
	if Input.is_action_just_pressed('ui_accept') or Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		var cam = get_viewport().get_camera_3d()
		if not cam: return
		var mouse_pos = get_viewport().get_mouse_position()
		var from = cam.project_ray_origin(mouse_pos)
		var dir = cam.project_ray_normal(mouse_pos)

		var closest_star = null
		var closest_dist = 999.0
		for star in sim.galaxy_map.stars:
			var diff = star.pos - from
			var cross = diff.cross(dir)
			var dist = cross.length()
			if dist < 2.0 and dist < closest_dist:
				closest_dist = dist
				closest_star = star

		if closest_star:
			sim.command_fleet_move(player_fleet_id, closest_star.id)
			ui_station.visible = false

func _sync_player_pos():
	var p_fleet = sim.active_fleets.filter(func(f): return f.id == player_fleet_id)
	if not p_fleet.is_empty():
		var f = p_fleet[0]
		if f.path.is_empty():
			var node = sim._get_star_at_pos(f.current_pos)
			if node:
				player.current_node_id = node.id
				ui_station.current_node_id = node.id

func toggle_market():
	ui_station.visible = not ui_station.visible
	if ui_station.visible: ui_station.refresh_market_list()

func toggle_contracts():
	ui_contracts.toggle()