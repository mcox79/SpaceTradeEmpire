extends RefCounted

# Base class for all requests sent to the Sim.
class Intent:
	var actor_id: String

# DOCKING
class Dock extends Intent:
	var target_id: String

class Undock extends Intent:
	pass

# COMMERCE
class Trade extends Intent:
	var target_id: String
	var item_id: String
	var amount: int
	var is_buy: bool # True = Buy from market, False = Sell to market

# LOGISTICS
class Refuel extends Intent:
	var target_id: String
