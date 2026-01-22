extends Area3D
class_name GameStation

const EconomyEngine = preload("res://scripts/core/economy_engine.gd")
const MarketProfile = preload("res://scripts/resources/market_profile.gd")
const TradeGood = preload("res://scripts/resources/trade_good.gd")

@export var fuel_cost_per_unit: int = 2
@export var market_profile: MarketProfile

@export var local_supply: Dictionary = {
	"ore_iron": 100,
	"ore_gold": 20,
}

var _goods_by_id: Dictionary = {}

const ASK_MULT := 1.10
const BID_MULT := 0.90

func _ready():
	monitoring = true
	monitorable = true
	body_entered.connect(_on_body_entered)
	_build_goods_index()

func _build_goods_index():
	_goods_by_id.clear()
	if market_profile == null:
		return
	for g in market_profile.trade_goods:
		if g == null:
			continue
		_goods_by_id[g.id] = g

func get_mid_price(item_id: String) -> int:
	var good: TradeGood = _goods_by_id.get(item_id, null)
	if good == null:
		return 0
	var base_demand := 100
	if market_profile != null:
		base_demand = int(market_profile.base_demands.get(item_id, 100))
		base_demand = int(float(base_demand) * market_profile.wealth_tier)
	var supply_val := int(local_supply.get(item_id, 1))
	return EconomyEngine.calculate_price(good, supply_val, base_demand)

func get_ask_price(item_id: String) -> int:
	var mid := get_mid_price(item_id)
	return int(ceil(float(mid) * ASK_MULT))

func get_bid_price(item_id: String) -> int:
	var mid := get_mid_price(item_id)
	return int(floor(float(mid) * BID_MULT))

func _on_body_entered(body):
	if body.has_method("dock_at_station"):
		_refuel_ship(body)
		body.dock_at_station(self)

func _refuel_ship(player):
	if not player.has_method("get_fuel_status"):
		return
	var needed: float = float(player.max_fuel) - float(player.fuel)
	if needed > 1.0:
		var cost := int(needed * fuel_cost_per_unit)
		if player.credits >= cost:
			player.credits -= cost
			player.fuel = player.max_fuel
			player.receive_payment(0)
		else:
			var units := int(player.credits / fuel_cost_per_unit)
			if units > 0:
				player.credits -= (units * fuel_cost_per_unit)
				player.fuel += units
				player.receive_payment(0)

func buy_cargo(player, item_id: String, amount: int) -> bool:
	if amount <= 0:
		return false
	var good: TradeGood = _goods_by_id.get(item_id, null)
	if good == null:
		return false
	var price := get_ask_price(item_id)
	if not EconomyEngine.can_afford(player.credits, price, amount):
		return false
	if not EconomyEngine.has_cargo_space(player.cargo_volume, player.max_cargo_volume, good.volume, amount):
		return false
	var total := price * amount
	player.credits -= total
	var ok: bool = player.add_cargo(item_id, amount)
	if not ok:
		player.credits += total
		return false
	local_supply[item_id] = int(local_supply.get(item_id, 1)) + amount
	player.receive_payment(0)
	return true

func sell_cargo(player, item_id: String, amount: int) -> bool:
	if amount <= 0:
		return false
	var price := get_bid_price(item_id)
	if price <= 0:
		return false
	if not player.cargo.has(item_id):
		return false
	if int(player.cargo[item_id]) < amount:
		return false
	var removed: bool = player.remove_cargo(item_id, amount)
	if not removed:
		return false
	player.receive_payment(price * amount)
	local_supply[item_id] = max(1, int(local_supply.get(item_id, 1)) - amount)
	return true
