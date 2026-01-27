extends Node

const TICK_INTERVAL = 0.5
const Sim = preload("res://scripts/core/sim/sim.gd")
const PlayerState = preload("res://scripts/core/state/player_state.gd")
const PlayerInteractionManager = preload("res://scripts/core/player_interaction_manager.gd")
const StationInterface = preload("res://scripts/view/ui/station_interface.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")

var sim: Sim
var player: PlayerState
var interaction: PlayerInteractionManager
var ui_instance: Control
var time_accumulator: float = 0.0
var player_fleet_id: String = "player_1"

func _ready():
	print("SUCCESS: Global Game Manager initialized.")
	sim = Sim.new()
	player = PlayerState.new()
	interaction = PlayerInteractionManager.new(player, sim)

	# BOOTSTRAP: SEED ENTIRE GALAXY
	if sim.galaxy_map.stars.size() > 0:
		# 1. Setup Player at Star 0
		var start_node = sim.galaxy_map.stars[0].id
		player.current_node_id = start_node

		# 2. Iterate ALL stars to generate markets
		for i in range(sim.galaxy_map.stars.size()):
			var star = sim.galaxy_map.stars[i]
			var market = sim.active_markets[star.id]

			# Logic: Even Index = Supplier, Odd Index = Consumer
			if i % 2 == 0:
				# Supplier: Lots of Fuel, Low Demand
				market.inventory["fuel"] = 1000 + (i * 10)
				market.base_demand["fuel"] = 1
				market.inventory["rations"] = 50
				market.base_demand["rations"] = 20
			else:
				# Consumer: Little Fuel, High Demand
				market.inventory["fuel"] = 50
				market.base_demand["fuel"] = 50
				market.inventory["rations"] = 1000
				market.base_demand["rations"] = 5

		# SPAWN PLAYER FLEET
		var p_fleet = Fleet.new(player_fleet_id, sim.galaxy_map.stars[0].pos)
		p_fleet.speed = 30.0
		sim.active_fleets.append(p_fleet)

	# Inject UI
	ui_instance = StationInterface.new()
	add_child(ui_instance)
	ui_instance.setup(interaction, player.current_node_id)

func _process(delta):
	_handle_selection()

	if Input.is_action_just_pressed("ui_focus_next"): # TAB
		toggle_market()

	if sim:
		time_accumulator += delta
		if time_accumulator >= TICK_INTERVAL:
			time_accumulator -= TICK_INTERVAL
			sim.advance()
			_sync_player_pos()
			if ui_instance.visible:
				ui_instance.refresh_market_list()

func _handle_selection():
	if Input.is_action_just_pressed("ui_accept") or Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
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
			if dist < 2.0:
				if dist < closest_dist:
					closest_dist = dist
					closest_star = star

		if closest_star:
			var success = sim.command_fleet_move(player_fleet_id, closest_star.id)
			if success:
				ui_instance.visible = false

func _sync_player_pos():
	var p_fleet = sim.active_fleets.filter(func(f): return f.id == player_fleet_id)
	if not p_fleet.is_empty():
		var f = p_fleet[0]
		if f.path.is_empty():
			var node = sim._get_star_at_pos(f.current_pos)
			if node:
				player.current_node_id = node.id
				ui_instance.current_node_id = node.id

func toggle_market():
	ui_instance.visible = not ui_instance.visible
	if ui_instance.visible:
		ui_instance.refresh_market_list()
