extends RefCounted

# ARCHITECTURE: Pure POD State Container for a logistical unit.

var id: String
var from: Vector3 
var to: Vector3   
var current_pos: Vector3

# Pathfinding State
var path: PackedVector3Array = []
var path_index: int = 0
var speed: float = 0.5 

# NEW: Logistics Tracking
# Holds the WorkOrder data so the Sim knows the financial value of the cargo upon arrival.
var active_order_ref = null

func _init(p_id: String, p_pos: Vector3):
	id = p_id
	current_pos = p_pos
	from = p_pos
	to = p_pos
