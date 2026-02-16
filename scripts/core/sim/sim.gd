extends RefCounted

const TICK_INTERVAL = 0.5
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")
const GalaxyGraph = preload("res://scripts/core/galaxy_graph.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")
const WorkOrder = preload("res://scripts/core/state/work_order.gd")
const MarketState = preload("res://scripts/core/state/market_state.gd")
const MarketRules = preload("res://scripts/core/subsystems/market_rules.gd")
const ProductionRules = preload("res://scripts/core/subsystems/production_rules.gd")
const LogisticsRules = preload("res://scripts/core/subsystems/logistics_rules.gd")
const InfoState = preload("res://scripts/core/state/info_state.gd")
const SignalRules = preload("res://scripts/core/subsystems/signal_rules.gd")


var current_tick: int = 0
var galaxy_map: Dictionary = {}
var active_fleets: Array = []
var active_markets: Dictionary = {}
var active_orders: Array = []
var _nav_graph: GalaxyGraph
var info: InfoState
var pending_player_rewards: int = 0

func _init(seed_val: int = 42):
	_generate_universe(seed_val)
	_initialize_markets()
	info = InfoState.new(galaxy_map)

func command_fleet_move(fleet_id: String, target_star_id: String) -> bool:
	var fleet_list = active_fleets.filter(func(flt): return flt.id == fleet_id)
	if fleet_list.is_empty(): return false
	var f = fleet_list[0]
	var start = _get_star_at_pos(f.current_pos)
	if not start: return false
	var route_ids = _nav_graph.get_route(start.id, target_star_id)
	if route_ids.is_empty(): return false
	var waypoints: PackedVector3Array = []
	for rid in route_ids:
		var star = _get_star_by_id(rid)
		waypoints.append(star.pos)
	f.path = waypoints
	f.path_index = 0
	return true

func player_accept_contract(fleet_id: String, order_id: String) -> bool:
	var fleet_list = active_fleets.filter(func(flt): return flt.id == fleet_id)
	if fleet_list.is_empty(): return false
	var fleet = fleet_list[0]
	var order_idx = -1
	for i in range(active_orders.size()):
		if active_orders[i].id == order_id:
			order_idx = i
			break
	if order_idx == -1: return false
	var order = active_orders.pop_at(order_idx)
	fleet.active_order_ref = order
	var start = _get_star_at_pos(fleet.current_pos)
	var leg1 = _nav_graph.get_route(start.id, order.pickup_id)
	var leg2 = _nav_graph.get_route(order.pickup_id, order.destination_id)
	if leg1.size() > 0 and leg2.size() > 0:
		var full_path = []
		full_path.append_array(leg1.map(func(id): return _get_star_by_id(id).pos))
		var leg2_pos = leg2.map(func(id): return _get_star_by_id(id).pos)
		if leg2_pos.size() > 1:
			full_path.append_array(leg2_pos.slice(1))
		fleet.path = full_path
		fleet.path_index = 0
		return true
	return false

func _generate_universe(seed_val: int):
	var gen = GalaxyGenerator.new(seed_val)
	galaxy_map = gen.generate(5)
	_nav_graph = GalaxyGraph.new()
	for star in galaxy_map.stars:
		_nav_graph.add_node(star.id)
	for lane in galaxy_map.lanes:
		# New format uses 'u' and 'v', but 'from'/'to' are kept for compatibility
		_nav_graph.connect_nodes(lane.u, lane.v)
	if galaxy_map.stars.size() > 0:
		active_fleets.append(Fleet.new('fleet_01', galaxy_map.stars[0].pos))

func advance():
	current_tick += 1
	_update_markets()
	SignalRules.process_decay(info, current_tick)
	_generate_contracts()
	_ingest_work_orders()
	_advance_fleets()

func _update_markets():
	for k in active_markets:
		ProductionRules.process_production(active_markets[k], current_tick)
		MarketRules.consume_inventory(active_markets[k], current_tick)

func _generate_contracts():
	for k in active_markets:
		var buyer = active_markets[k]
		for item_id in buyer.base_demand.keys():
			var threshold = buyer.base_demand[item_id]
			if buyer.inventory.get(item_id, 0) < threshold:
				var exists = active_orders.any(func(o): return o.destination_id == k and o.item_id == item_id)
				if exists: continue
				var best_seller_id = ''
				for s_id in active_markets:
					if s_id == k: continue
					var seller = active_markets[s_id]
					if seller.inventory.get(item_id, 0) > 5:
						best_seller_id = s_id
						break
				if best_seller_id != '':
					var wo = WorkOrder.new('wo_' + str(active_orders.size()), WorkOrder.Type.CONTRACT, WorkOrder.Objective.DELIVER)
					wo.destination_id = k
					wo.pickup_id = best_seller_id
					wo.item_id = item_id
					wo.quantity = 10
					wo.reward = 500
					active_orders.append(wo)

func _ingest_work_orders():
	var idle = active_fleets.filter(func(flt): return flt.path.is_empty() and flt.active_order_ref == null)
	if idle.size() > 0 and active_orders.size() > 0:
		var fleet = idle[0]
		if fleet.id.begins_with('player'): return
		var o = active_orders.pop_front()
		var start = _get_star_at_pos(fleet.current_pos)
		var leg1 = _nav_graph.get_route(start.id, o.pickup_id)
		var leg2 = _nav_graph.get_route(o.pickup_id, o.destination_id)
		if leg1.size() > 0 and leg2.size() > 0:
			var full_path = []
			full_path.append_array(leg1.map(func(id): return _get_star_by_id(id).pos))
			var leg2_pos = leg2.map(func(id): return _get_star_by_id(id).pos)
			if leg2_pos.size() > 1:
				full_path.append_array(leg2_pos.slice(1))
			fleet.path = full_path
			fleet.path_index = 0
			fleet.active_order_ref = o

func _advance_fleets():
	for f in active_fleets:
		if f.path.is_empty(): continue
		var target = f.path[f.path_index]
		f.from = f.current_pos
		f.to = target
		f.current_pos = f.current_pos.move_toward(target, f.speed * 0.1)
		if f.current_pos.is_equal_approx(target):
			var node = _get_star_at_pos(target)
			if node:
				info.add_heat(node.id, 0.5)
				if active_markets.has(node.id):
					var earned = LogisticsRules.handle_arrival(f, active_markets[node.id], current_tick)
					if earned > 0 and f.id.begins_with('player'):
						pending_player_rewards += earned
			f.path_index += 1
			if f.path_index >= f.path.size():
				f.path.clear()

func _initialize_markets():
	for i in range(galaxy_map.stars.size()):
		var s = galaxy_map.stars[i]
		var m = MarketState.new(s.id)
		var role = i % 3
		if role == 0:
			m.industries['mining'] = 1
		elif role == 1:
			m.industries['refinery'] = 1
			m.base_demand['ore_iron'] = 20
		else:
			m.base_demand['fuel'] = 20
			m.base_demand['rations'] = 20
		active_markets[s.id] = m

func _get_star_at_pos(p):
	for s in galaxy_map.stars: if s.pos.is_equal_approx(p): return s
	return null

func _get_star_by_id(id):
	for s in galaxy_map.stars: if s.id == id: return s
	return null
