extends RefCounted

enum Type { CONTRACT, TRADE, PATROL }
enum Objective { DELIVER, DEFEND, SCOUT }

var id: String
var type: int
var objective: int

var destination_id: String
var pickup_id: String  # NEW: We need to know WHERE to get it
var item_id: String
var quantity: int
var expiration_tick: int
var reward: int

func _init(p_id: String, p_type: int, p_obj: int):
	id = p_id
	type = p_type
	objective = p_obj