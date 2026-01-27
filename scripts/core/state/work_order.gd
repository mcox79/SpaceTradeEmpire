extends RefCounted

enum Type { ROUTE, CONTRACT, SPECIAL }
enum Objective { DELIVER, MINE, PATROL, ESCORT, RECON }

var id: String
var type: Type
var objective: Objective
var origin_id: String
var destination_id: String
var item_id: String = ""
var quantity: int = 0
var is_active: bool = true

func _init(p_id: String, p_type: Type, p_obj: Objective):
	id = p_id
	type = p_type
	objective = p_obj