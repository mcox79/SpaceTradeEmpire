extends Resource
class_name MarketProfile

@export var station_name: String = "Station"
@export var wealth_tier: float = 1.0
@export var trade_goods: Array[TradeGood] = []

# Key: TradeGood.id, Value: base_demand (int)
@export var base_demands: Dictionary = {}
