# Contract Header
# Purpose: Physical representation of a market node. Acts as a View/Controller for the SimBridge.
# Layer: GameShell (View)
# Dependencies: SimBridge (Authoritative), MarketProfile (Metadata)
# Public API: dock_at_station()
# Invariants: No local economy math. All transactions must pass through SimBridge.

extends Area3D
class_name ActiveStation


# Link to the specific Market ID in SimCore (e.g., "M_1", "market_alpha")
@export var sim_market_id: String = "star_0"
@export var market_profile: MarketProfile

var _goods_by_id: Dictionary = {}
var _bridge = null

func _ready():
	monitoring = true
	monitorable = true
	collision_mask = 2 # Layers: Ships
	set_meta("dock_target_id", sim_market_id)

	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)
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
	var gm = get_node_or_null("/root/GameManager")
	# Dock confirmation: show prompt instead of auto-docking.
	if gm and gm.has_method("on_dock_proximity_v0"):
		gm.on_dock_proximity_v0(self)

func _on_body_exited(_body):
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("on_dock_proximity_exit_v0"):
		gm.on_dock_proximity_exit_v0(self)

func _refuel_via_bridge(player):
	# Placeholder for future Refuel API in SimBridge
	# For now, we assume fuel is cheap/free or handled separately until Energy economy is defined.
	if player.has_method("refuel_full"):
		player.refuel_full()

func get_sim_market_id() -> String:
	return sim_market_id

# GATE.X.STATION_IDENTITY.VISUAL.001: Per-faction color tint and tier-based size variation.
# Faction colors: communion=cyan, valorin=red, weaver=purple, chitin=amber, naturalize=green, neutral=gray.
# Tier scaling: 0 (outpost)=0.6, 1 (hub)=1.0, 2 (capital)=1.5.
func setup(faction_id: String, tier: int) -> void:
	var color := _faction_color(faction_id)
	var tier_scale := _tier_scale(tier)
	var visual := get_node_or_null("StationVisual")
	if visual:
		visual.scale = Vector3.ONE * tier_scale
		for child in visual.get_children():
			if child is MeshInstance3D:
				var mat := StandardMaterial3D.new()
				mat.albedo_color = color
				mat.roughness = 0.5
				mat.metallic = 0.5
				child.material_override = mat

func _faction_color(faction_id: String) -> Color:
	match faction_id:
		"communion": return Color("#00CED1")
		"valorin": return Color("#DC143C")
		"weaver": return Color("#8B008B")
		"chitin": return Color("#FFD700")
		"naturalize": return Color("#228B22")
		_: return Color("#808080")

func _tier_scale(tier: int) -> float:
	match tier:
		0: return 0.6
		2: return 1.5
		_: return 1.0
