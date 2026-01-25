extends RefCounted

const TICK_INTERVAL = 0.5 
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")
const GalaxyGraph = preload("res://scripts/core/galaxy_graph.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")
const WorkOrder = preload("res://scripts/core/state/work_order.gd")
const MarketState = preload("res://scripts/core/state/market_state.gd")
const MarketRules = preload("res://scripts/core/subsystems/market_rules.gd")
const Intents = preload("res://scripts/core/intents/intents.gd")
const InfoState = preload("res://scripts/core/state/info_state.gd")
const SignalRules = preload("res://scripts/core/subsystems/signal_rules.gd")

var current_tick: int = 0
var galaxy_map: Dictionary = {}
var active_fleets: Array = []
var active_markets: Dictionary = {} 
var active_orders: Array = [] 
var _nav_graph: GalaxyGraph 
var info: InfoState 

func _init():
	print("BOOTSTRAP: Sim Core initializing (Data Purity Mode)...")
	_generate_universe()
	_initialize_markets()
	info = InfoState.new(galaxy_map)

func _generate_universe():
	var gen = GalaxyGenerator.new(42)
	galaxy_map = gen.generate(5)
	_nav_graph = GalaxyGraph.new()
	for star in galaxy_map.stars:
		_nav_graph.add_node(star.id) 
	for lane in galaxy_map.lanes:
		var from_star = _get_star_at_pos(lane.from)
		var to_star = _get_star_at_pos(lane.to)
		if from_star != null and to_star != null:
			_nav_graph.connect_nodes(from_star.id, to_star.id)
	if galaxy_map.stars.size() > 0:
		active_fleets.append(Fleet.new("fleet_01", galaxy_map.stars[0].pos))

func _get_star_at_pos(pos: Vector3):
	for star in galaxy_map.stars:
		if star.pos.is_equal_approx(pos): return star
	return null

func _get_star_by_id(id: String):
	for star in galaxy_map.stars:
		if star.id == id: return star
	return null

func advance():
	current_tick += 1
	_update_markets()
	SignalRules.process_decay(info, 1)
	_generate_contracts() 
	_ingest_work_orders() 
	_advance_fleets()

# --- THE INPUT GATEWAY ---
# All user inputs MUST pass through this method to preserve determinism.
func apply_intent(intent) -> Dictionary:
	# 1. TRADE ROUTING
	if intent is Intents.Trade:
		return _process_trade(intent)
	
	return { "error": "Unknown Intent Type" }

func _process_trade(intent: Intents.Trade) -> Dictionary:
	# 1. Validation (Does the market exist?)
	if not active_markets.has(intent.target_id):
		return { "error": "Invalid Market Node" }
	
	var market = active_markets[intent.target_id]
	
	# In a full sim, we would query the Player's POD state here.
	# For the Shim: We simulate the transaction execution.
	var execution_value = intent.amount * 100 # Simulated cost basis
	
	if intent.is_buy:
		market.inventory[intent.item_id] = max(0, market.inventory.get(intent.item_id, 0) - intent.amount)
	else:
		market.inventory[intent.item_id] = market.inventory.get(intent.item_id, 0) + intent.amount
	
	# 2. GENERATE HEAT (VALUE-WEIGHTED)
	# High-value trades generate massive thermal and traffic signatures.
	var heat_generated = float(execution_value) * 0.05
	info.add_heat(intent.target_id, heat_generated)
	print("[SIGNALS] Player Trade at %s generated +%s Heat." % [intent.target_id, heat_generated])
	
	return { "error": null, "events": ["intent_processed"] }

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
		var start_star = _get_star_at_pos(fleet.current_pos)
		var route_ids = _nav_graph.get_route(start_star.id, order.destination_id)
		var waypoints: PackedVector3Array = []
		for rid in route_ids:
			var star = _get_star_by_id(rid)
			waypoints.append(star.pos)
		fleet.path = waypoints
		fleet.path_index = 0
		fleet.active_order_ref = order

func _advance_fleets():
	for fleet in active_fleets:
		if fleet.path.is_empty(): continue
		var target = fleet.path[fleet.path_index]
		fleet.to = target 
		fleet.from = fleet.current_pos 
		var step = fleet.current_pos.move_toward(target, fleet.speed)
		fleet.current_pos = step
		if fleet.current_pos.is_equal_approx(target):
			var node_at_target = _get_star_at_pos(target)
			info.add_heat(node_at_target.id, 0.5) 
			fleet.path_index += 1
			if fleet.path_index >= fleet.path.size():
				if fleet.get("active_order_ref") != null:
					var cargo_heat = float(fleet.active_order_ref.quantity) * 0.2
					info.add_heat(node_at_target.id, cargo_heat)
				fleet.path.clear()

func _initialize_markets():
	for star in galaxy_map.stars:
		active_markets[star.id] = MarketState.new(star.id)
