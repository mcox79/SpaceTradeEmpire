# Contract Header
# Purpose: Physical representation of a market node. Acts as a View/Controller for the SimBridge.
# Layer: GameShell (View)
# Dependencies: SimBridge (Authoritative), MarketProfile (Metadata)
# Public API: dock_at_station()
# Invariants: No local economy math. All transactions must pass through SimBridge.

extends Area3D
class_name GameStation

const MarketProfile = preload("res://scripts/resources/market_profile.gd")
const TradeGood = preload("res://scripts/resources/trade_good.gd")

# Link to the specific Market ID in SimCore (e.g., "M_1", "market_alpha")
@export var sim_market_id: String = "market_alpha"
@export var market_profile: MarketProfile

var _goods_by_id: Dictionary = {}
var _bridge = null

func _ready():
	monitoring = true
	monitorable = true
	collision_mask = 2 # Layers: Ships
	
	body_entered.connect(_on_body_entered)
	_build_goods_index()
	
	# Locate Bridge (Expect it to be an Autoload or in Root)
	_bridge = get_tree().root.find_child("SimBridge", true, false)
	if _bridge == null:
		push_error("[STATION] CRITICAL: SimBridge not found in Scene Tree!")

func _build_goods_index():
	_goods_by_id.clear()
	if market_profile == null:
		return
	for g in market_profile.trade_goods:
		if g == null: continue
		_goods_by_id[g.id] = g

# --- PRICE QUOTES (READ ONLY) ---

func get_ask_price(item_id: String) -> int:
	if _bridge == null: return 0
	# In SimCore, Ask = Bid for now (Single Price Model)
	return _bridge.GetMarketPrice(sim_market_id, item_id)

func get_bid_price(item_id: String) -> int:
	if _bridge == null: return 0
	return _bridge.GetMarketPrice(sim_market_id, item_id)

# --- TRANSACTIONS (WRITE) ---

func buy_cargo(player, item_id: String, amount: int) -> bool:
	if _bridge == null: return false
	
	# 1. Execute Transaction on SimCore
	var success: bool = _bridge.TryBuyCargo(sim_market_id, item_id, amount)
	
	if success:
		# 2. Sync Visuals (Optional/Legacy)
		# TODO: Player script should reactively listen to SimBridge signals instead of manual updates here.
		if player.has_method("receive_payment"):
			player.receive_payment(0) # Trigger UI refresh
		print("[STATION] Sold %d %s to player." % [amount, item_id])
	else:
		print("[STATION] Transaction Failed (Funds? Supply?)")
	
	return success

func sell_cargo(player, item_id: String, amount: int) -> bool:
	if _bridge == null: return false
	
	var success: bool = _bridge.TrySellCargo(sim_market_id, item_id, amount)
	
	if success:
		if player.has_method("receive_payment"):
			player.receive_payment(0)
		print("[STATION] Bought %d %s from player." % [amount, item_id])
	
	return success

# --- INTERACTION ---

func _on_body_entered(body):
	print("[STATION] Contact: %s" % body.name)
	if body.has_method("dock_at_station"):
		_refuel_via_bridge(body)
		body.dock_at_station(self)

func _refuel_via_bridge(player):
	# Placeholder for future Refuel API in SimBridge
	# For now, we assume fuel is cheap/free or handled separately until Energy economy is defined.
	if player.has_method("refuel_full"):
		player.refuel_full()