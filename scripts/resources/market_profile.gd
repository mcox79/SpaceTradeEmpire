extends Resource
class_name MarketProfile

# This defines a Station's economic personality
@export var station_name: String = "Station"
@export var wealth_tier: float = 1.0
@export var trade_goods: Array[TradeGood] = []
# Future: Add supply/demand curves here
