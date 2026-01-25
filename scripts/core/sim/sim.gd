extends Node
class_name Sim

# --- FINANCIAL CONFIGURATION ---
const TICK_INTERVAL = 5.0 # Operational cadence: 1 tick = 5.0 real seconds

# --- STATE LEDGER ---
var current_tick: int = 0
var _eco_timer: Timer

func _ready():
	print("BOOTSTRAP: Space Trade Empire Sim Core initializing...")
	_initialize_ledger_clock()

func _initialize_ledger_clock():
	# Capitalizing on a decoupled architecture to de-risk the tick loop.
	_eco_timer = Timer.new()
	_eco_timer.name = "EcoTimer"
	_eco_timer.wait_time = TICK_INTERVAL
	_eco_timer.autostart = true
	_eco_timer.timeout.connect(_on_economic_tick)
	add_child(_eco_timer)
	print("SUCCESS: Master ledger clock online. Cadence: %s seconds." % TICK_INTERVAL)

func _on_economic_tick():
	current_tick += 1
	_ingest_work_orders()
	
	# Audit logging for the CI/CD pipeline
	print("[LEDGER] Tick %s processed." % current_tick)

func _ingest_work_orders():
	# SKELETON: Primary ingestor for Route, Contract, and Special orders.
	# Next Iteration: Query current work orders and route to Subsystems.
	pass
