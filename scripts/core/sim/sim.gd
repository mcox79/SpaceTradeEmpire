extends Node

# --- FINANCIAL CONFIGURATION ---
const TICK_INTERVAL = 5.0 # Operational cadence

# --- STATE LEDGER ---
var current_tick: int = 0
var _eco_timer: Timer

func _ready():
	print("BOOTSTRAP: Sim Core initializing...")
	_initialize_ledger_clock()

func _initialize_ledger_clock():
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

func _ingest_work_orders():
	# PLACEHOLDER: Ready to ingest WorkOrder data.
	pass
