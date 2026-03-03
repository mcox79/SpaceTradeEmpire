extends CanvasLayer

# GATE.S1.HERO_SHIP_LOOP.PLAYER_TRADE.001
# Hero trade menu v0: API surface for the player market at a docked station.
# Actual widget rendering is deferred to a future gate.

var _market_node_id: String = ""

func open_market_v0(node_id: String) -> void:
	_market_node_id = node_id

func close_market_v0() -> void:
	_market_node_id = ""

func get_market_view_v0() -> Array:
	if _market_node_id.is_empty():
		return []
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetPlayerMarketViewV0"):
		return bridge.call("GetPlayerMarketViewV0", _market_node_id)
	return []

func buy_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, true)

func sell_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, false)
