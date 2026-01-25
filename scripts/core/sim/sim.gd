extends Node

const TICK_INTERVAL = 5.0
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")

# Bypassing cache lock for new features
const MarketState = preload("res://scripts/core/state/market_state.gd")
const MarketRules = preload("res://scripts/core/subsystems/market_rules.gd")

var current_tick: int = 0
var _eco_timer: Timer

# --- CANONICAL STATE ---
var galaxy_map: Dictionary = {}
var active_fleets: Array = []
var active_markets: Dictionary = {} # Key: star_id, Value: MarketState

func _ready():
	print("BOOTSTRAP: Sim Core initializing...")
	_generate_universe()
	_initialize_markets()
	_initialize_ledger_clock()

func _generate_universe():
	var gen = GalaxyGenerator.new(42)
	galaxy_map = gen.generate(5)
	
	# INJECT TEST FLEET
	if galaxy_map.lanes.size() > 0:
		var test_lane = galaxy_map.lanes[0]
		active_fleets.append(Fleet.new("fleet_01", test_lane.from, test_lane.to))

func _initialize_markets():
	# Generate a dynamic economy for every star
	for star in galaxy_map.stars:
		active_markets[star.id] = MarketState.new(star.id)
	print("SUCCESS: Autonomous Markets initialized at %s nodes." % active_markets.size())

func _initialize_ledger_clock():
	_eco_timer = Timer.new()
	_eco_timer.name = "EcoTimer"
	_eco_timer.wait_time = TICK_INTERVAL
	_eco_timer.autostart = true
	_eco_timer.timeout.connect(_on_economic_tick)
	add_child(_eco_timer)

func _on_economic_tick():
	current_tick += 1
	_advance_fleets()
	_update_markets()
	_ingest_work_orders()

func _update_markets():
	# Slice 5: Autonomous Markets tick independently of the player.
	for star_id in active_markets.keys():
		var market = active_markets[star_id]
		MarketRules.consume_inventory(market, 1)
	
	# AUDIT LOG: Print the price of Staples at Star 0 to prove dynamic scarcity
	var test_market = active_markets.values()[0]
	var supply = test_market.inventory["staples"]
	var demand = test_market.base_demand["staples"]
	var price = MarketRules.calculate_price(supply, demand, 10) # 10 = base value of staples
	print("[ECONOMY] Tick %s | Star 0 Staples: Supply %s, Price $%s" % [current_tick, supply, price])

func _advance_fleets():
	for fleet in active_fleets:
		if fleet.progress < 1.0:
			fleet.progress = min(1.0, fleet.progress + fleet.speed)
			fleet.current_pos = fleet.from.lerp(fleet.to, fleet.progress)

func _ingest_work_orders():
	pass
