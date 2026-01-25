extends Node

const TICK_INTERVAL = 0.5 
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")
const WorkOrder = preload("res://scripts/core/state/work_order.gd")
const MarketState = preload("res://scripts/core/state/market_state.gd")
const MarketRules = preload("res://scripts/core/subsystems/market_rules.gd")

var current_tick: int = 0
var _eco_timer: Timer

# --- CANONICAL STATE ---
var galaxy_map: Dictionary = {}
var active_fleets: Array = []
var active_markets: Dictionary = {} 
var active_orders: Array = [] 

# NEW: The Headless Navigation Ledger
var _nav_grid: AStar3D 

func _ready():
	print("BOOTSTRAP: Sim Core initializing...")
	_generate_universe()
	_initialize_markets()
	_initialize_ledger_clock()

func _generate_universe():
	var gen = GalaxyGenerator.new(42)
	galaxy_map = gen.generate(5)
	
	# Initialize AStar Grid
	_nav_grid = AStar3D.new()
	for i in range(galaxy_map.stars.size()):
		_nav_grid.add_point(i, galaxy_map.stars[i].pos)
	
	for lane in galaxy_map.lanes:
		# Find the matching indices for the lane's positions
		var from_idx = _find_star_index(lane.from)
		var to_idx = _find_star_index(lane.to)
		if from_idx != -1 and to_idx != -1:
			_nav_grid.connect_points(from_idx, to_idx)

	# INJECT IDLE TEST FLEET
	if galaxy_map.stars.size() > 0:
		active_fleets.append(Fleet.new("fleet_01", galaxy_map.stars[0].pos))

func _find_star_index(pos: Vector3) -> int:
	for i in range(galaxy_map.stars.size()):
		if galaxy_map.stars[i].pos.is_equal_approx(pos): return i
	return -1

func _initialize_markets():
	for star in galaxy_map.stars:
		active_markets[star.id] = MarketState.new(star.id)

func _initialize_ledger_clock():
	_eco_timer = Timer.new()
	_eco_timer.name = "EcoTimer"
	_eco_timer.wait_time = TICK_INTERVAL
	_eco_timer.autostart = true
	_eco_timer.timeout.connect(_on_economic_tick)
	add_child(_eco_timer)

func _on_economic_tick():
	current_tick += 1
	_update_markets()
	_generate_contracts() 
	_ingest_work_orders() 
	_advance_fleets()

func _update_markets():
	for star_id in active_markets.keys():
		MarketRules.consume_inventory(active_markets[star_id], 1)

func _generate_contracts():
	for star_id in active_markets.keys():
		var market = active_markets[star_id]
		if market.inventory["staples"] < 20:
			var order_exists = active_orders.any(func(o): return o.destination_id == star_id and o.item_id == "staples")
			if not order_exists:
				var new_order = WorkOrder.new("wo_" + str(active_orders.size()), WorkOrder.Type.CONTRACT, WorkOrder.Objective.DELIVER)
				new_order.destination_id = star_id
				new_order.item_id = "staples"
				new_order.quantity = 50
				active_orders.append(new_order)

func _ingest_work_orders():
	var idle_fleets = active_fleets.filter(func(f): return f.path.is_empty())
	if idle_fleets.size() > 0 and active_orders.size() > 0:
		var fleet = idle_fleets[0]
		var order = active_orders.pop_front()
		
		# 1. Resolve Pathfinding via AStar
		var start_idx = _find_star_index(fleet.current_pos)
		var dest_star = galaxy_map.stars.filter(func(s): return s.id == order.destination_id)[0]
		var end_idx = _find_star_index(dest_star.pos)
		
		# 2. Assign the strategic route
		fleet.path = _nav_grid.get_point_path(start_idx, end_idx)
		fleet.path_index = 0
		print("[LOGISTICS] Dispatched %s to %s via %s-jump route." % [fleet.id, order.destination_id, fleet.path.size() - 1])

func _advance_fleets():
	for fleet in active_fleets:
		if fleet.path.is_empty(): continue
		
		# Move toward next waypoint
		var target = fleet.path[fleet.path_index]
		fleet.to = target # Sync view contract
		fleet.from = fleet.current_pos # Sync view contract
		
		var step = fleet.current_pos.move_toward(target, fleet.speed)
		fleet.current_pos = step
		
		# Waypoint reached?
		if fleet.current_pos.is_equal_approx(target):
			fleet.path_index += 1
			if fleet.path_index >= fleet.path.size():
				fleet.path.clear() # Order Complete
				print("[LOGISTICS] %s arrived at final destination." % fleet.id)
