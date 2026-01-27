extends RefCounted

const TICK_INTERVAL = 0.5
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")
const GalaxyGraph = preload("res://scripts/core/galaxy_graph.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")
const WorkOrder = preload("res://scripts/core/state/work_order.gd")
const MarketState = preload("res://scripts/core/state/market_state.gd")
const MarketRules = preload("res://scripts/core/subsystems/market_rules.gd")
const ProductionRules = preload("res://scripts/core/subsystems/production_rules.gd")
const InfoState = preload("res://scripts/core/state/info_state.gd")
const SignalRules = preload("res://scripts/core/subsystems/signal_rules.gd")

var current_tick: int = 0
var galaxy_map: Dictionary = {}
var active_fleets: Array = []
var active_markets: Dictionary = {}
var active_orders: Array = []
var _nav_graph: GalaxyGraph
var info: InfoState

func _init(seed_val: int = 42):
_generate_universe(seed_val)
_initialize_markets()
info = InfoState.new(galaxy_map)

func command_fleet_move(fleet_id: String, target_star_id: String) -> bool:
var fleet_list = active_fleets.filter(func(f): return f.id == fleet_id)
if fleet_list.is_empty():
print("ERR: Fleet not found: " + fleet_id)
return false

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

func _generate_universe(seed_val: int):
var gen = GalaxyGenerator.new(seed_val)
galaxy_map = gen.generate(5)
_nav_graph = GalaxyGraph.new()
for star in galaxy_map.stars:
_nav_graph.add_node(star.id)
for lane in galaxy_map.lanes:
var s1 = _get_star_at_pos(lane.from)
var s2 = _get_star_at_pos(lane.to)
if s1 and s2: _nav_graph.connect_nodes(s1.id, s2.id)
if galaxy_map.stars.size() > 0:
active_fleets.append(Fleet.new("fleet_01", galaxy_map.stars[0].pos))

func advance():
current_tick += 1
_update_markets()
SignalRules.process_decay(info, current_tick)
_generate_contracts()
_ingest_work_orders()
_advance_fleets()

func _update_markets():
for k in active_markets: 
# 1. Production (Source)
ProductionRules.process_production(active_markets[k], current_tick)
# 2. Consumption (Sink)
MarketRules.consume_inventory(active_markets[k], current_tick)

func _generate_contracts():
for k in active_markets:
var m = active_markets[k]
if m.inventory.get("staples", 0) < 20:
var exists = active_orders.any(func(o): return o.destination_id == k and o.item_id == "staples")
if not exists:
var wo = WorkOrder.new("wo_"+str(active_orders.size()), WorkOrder.Type.CONTRACT, WorkOrder.Objective.DELIVER)
wo.destination_id = k
wo.item_id = "staples"
wo.quantity = 50
active_orders.append(wo)

func _ingest_work_orders():
# REFACTOR: Fixed variable shadowing 'f' to 'fleet'
var idle = active_fleets.filter(func(f): return f.path.is_empty())
if idle.size() > 0 and active_orders.size() > 0:
var fleet = idle[0]
if fleet.id.begins_with("player"): return

var o = active_orders.pop_front()
var start = _get_star_at_pos(fleet.current_pos)
var route = _nav_graph.get_route(start.id, o.destination_id)
if route.size() > 0:
fleet.path = route.map(func(id): return _get_star_by_id(id).pos)
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
if node: info.add_heat(node.id, 0.5)
f.path_index += 1
if f.path_index >= f.path.size(): f.path.clear()

func _initialize_markets():
for i in range(galaxy_map.stars.size()): 
var s = galaxy_map.stars[i]
var m = MarketState.new(s.id)

# PHASE 2: Define Producers (Even) vs Consumers (Odd)
# Producers generate the raw materials and fuel.
if i % 2 == 0:
m.industries["mining"] = 1
m.industries["agri"] = 1
m.industries["refinery"] = 1

active_markets[s.id] = m

func _get_star_at_pos(p):
for s in galaxy_map.stars: if s.pos.is_equal_approx(p): return s
return null

func _get_star_by_id(id):
for s in galaxy_map.stars: if s.id == id: return s
return null
