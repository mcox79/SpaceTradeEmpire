extends Node

const TICK_INTERVAL = 0.5 # FIXED: Accelerated 1000% for rapid testing
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")
const WorkOrder = preload("res://scripts/core/state/work_order.gd")
const MarketState = preload("res://scripts/core/state/market_state.gd")
const MarketRules = preload("res://scripts/core/subsystems/market_rules.gd")

var current_tick: int = 0
var _eco_timer: Timer

var galaxy_map: Dictionary = {}
var active_fleets: Array = []
var active_markets: Dictionary = {} 
var active_orders: Array = [] 

func _ready():
	print("BOOTSTRAP: Sim Core initializing...")
	_generate_universe()
	_initialize_markets()
	_initialize_ledger_clock()

func _generate_universe():
	var gen = GalaxyGenerator.new(42)
	galaxy_map = gen.generate(5)
	if galaxy_map.stars.size() > 0:
		var home_star = galaxy_map.stars[0]
		active_fleets.append(Fleet.new("fleet_01", home_star.pos, home_star.pos))

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
		var staples_supply = market.inventory["staples"]
		if staples_supply < 20:
			var order_exists = active_orders.any(func(o): return o.destination_id == star_id and o.item_id == "staples")
			if not order_exists:
				var new_order = WorkOrder.new("wo_" + str(active_orders.size()), WorkOrder.Type.CONTRACT, WorkOrder.Objective.DELIVER)
				new_order.destination_id = star_id
				new_order.item_id = "staples"
				new_order.quantity = 50
				active_orders.append(new_order)
				print("[CONTRACT] Critical Shortage at %s. Issued delivery order." % star_id)

func _ingest_work_orders():
	var idle_fleets = active_fleets.filter(func(f): return f.progress >= 1.0 or f.from == f.to)
	if idle_fleets.size() > 0 and active_orders.size() > 0:
		var fleet = idle_fleets[0]
		var order = active_orders.pop_front()
		var target_star = galaxy_map.stars.filter(func(s): return s.id == order.destination_id)
		if target_star.size() > 0:
			fleet.from = fleet.current_pos
			fleet.to = target_star[0].pos
			fleet.progress = 0.0
			print("[LOGISTICS] Dispatched %s to resolve shortage at %s." % [fleet.id, order.destination_id])

func _advance_fleets():
	for fleet in active_fleets:
		if fleet.progress < 1.0:
			fleet.progress = min(1.0, fleet.progress + fleet.speed)
			fleet.current_pos = fleet.from.lerp(fleet.to, fleet.progress)
