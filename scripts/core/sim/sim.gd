extends Node

const TICK_INTERVAL = 5.0
const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")

# Bypassing cache locks for CI/CD safety
const Fleet = preload("res://scripts/core/state/fleet.gd")

var current_tick: int = 0
var _eco_timer: Timer

# --- CANONICAL STATE ---
var galaxy_map: Dictionary = {}
var active_fleets: Array = []

func _ready():
	print("BOOTSTRAP: Sim Core initializing...")
	_generate_universe()
	_initialize_ledger_clock()

func _generate_universe():
	var gen = GalaxyGenerator.new(42)
	galaxy_map = gen.generate(5)
	print("SUCCESS: Universe topology generated.")
	
	# INJECT TEST LOGISTICS (SLICE 4)
	if galaxy_map.lanes.size() > 0:
		var test_lane = galaxy_map.lanes[0]
		var test_fleet = Fleet.new("fleet_01", test_lane.from, test_lane.to)
		active_fleets.append(test_fleet)
		print("SUCCESS: 'fleet_01' injected into Primary Artery.")

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
	_ingest_work_orders()

func _advance_fleets():
	# Deterministic Movement via Tick Interpolation
	for fleet in active_fleets:
		if fleet.progress < 1.0:
			fleet.progress = min(1.0, fleet.progress + fleet.speed)
			# Using standardized 'from' and 'to' properties
			fleet.current_pos = fleet.from.lerp(fleet.to, fleet.progress)
			if fleet.progress >= 1.0:
				print("[LOGISTICS] Fleet %s arrived at destination." % fleet.id)

func _ingest_work_orders():
	pass
