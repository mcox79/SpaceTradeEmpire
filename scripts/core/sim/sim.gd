extends Node

const TICK_INTERVAL = 5.0
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")

var current_tick: int = 0
var _eco_timer: Timer

# THE CANONICAL MAP
var galaxy_map: Dictionary = {}

func _ready():
	print("BOOTSTRAP: Sim Core initializing...")
	_generate_universe()
	_initialize_ledger_clock()

func _generate_universe():
	# Executes Slice 3: Generating the strategic geography headless.
	var gen = GalaxyGenerator.new(42) # Fixed seed for determinism
	galaxy_map = gen.generate(5) # Generate 5 regions
	print("SUCCESS: Universe topology generated. Stars: %s | Lanes: %s" % [galaxy_map.stars.size(), galaxy_map.lanes.size()])

func _initialize_ledger_clock():
	_eco_timer = Timer.new()
	_eco_timer.name = "EcoTimer"
	_eco_timer.wait_time = TICK_INTERVAL
	_eco_timer.autostart = true
	_eco_timer.timeout.connect(_on_economic_tick)
	add_child(_eco_timer)

func _on_economic_tick():
	current_tick += 1
	_ingest_work_orders()

func _ingest_work_orders():
	pass
