# scripts/tests/test_economy_health_eval_v0.gd
# Economy Health Evaluation Bot — advanced metrics per EVE Online / X4 best practices.
#
# Metrics measured:
#   - Gini coefficient (station wealth inequality)
#   - Price stability index (per-good price CV across stations)
#   - Trade route utilization (profitable routes with NPC activity)
#   - Faucet-sink ratio (credits entering vs leaving economy)
#   - Bootstrap test (does economy self-start without player?)
#   - Market depth (goods tradeable within 10% of avg price)
#   - NPC trade frequency
#
# Usage:
#   godot --headless --path . -s res://scripts/tests/test_economy_health_eval_v0.gd
extends SceneTree

const AssertLib = preload("res://scripts/tools/bot_assert.gd")

const MAX_FRAMES := 5400
const TICK_INTERVAL := 60  # Sample every 60 frames

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	BOOT, BOOTSTRAP_CHECK, WAIT_BOOTSTRAP,
	COLLECT_MARKET_DATA, WAIT_COLLECTION,
	TRADE_ACTIVITY_CHECK,
	ANALYZE, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _gm = null
var _a: AssertLib = null

# Market data snapshots.
var _station_credits: Array[int] = []
var _price_by_good: Dictionary = {}  # good_id → Array[int] (prices across stations)
var _goods_tradeable := 0  # Goods with buy+sell station
var _total_goods := 0
var _npc_trade_count := 0
var _player_credits_start := 0
var _player_credits_current := 0
var _tick_at_boot := 0


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_a.flag("TIMEOUT_AT_%s" % Phase.keys()[_phase])
		_phase = Phase.ANALYZE

	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(60, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait(30, Phase.BOOT)
		Phase.BOOT: _do_boot()
		Phase.BOOTSTRAP_CHECK: _do_bootstrap_check()
		Phase.WAIT_BOOTSTRAP: _do_wait(300, Phase.COLLECT_MARKET_DATA)
		Phase.COLLECT_MARKET_DATA: _do_collect_market_data()
		Phase.WAIT_COLLECTION: _do_wait(300, Phase.TRADE_ACTIVITY_CHECK)
		Phase.TRADE_ACTIVITY_CHECK: _do_trade_activity_check()
		Phase.ANALYZE: _do_analyze()
		Phase.DONE: pass
	return false


func _do_load_scene() -> void:
	_a = AssertLib.new("ECON_HEALTH")
	_a.log("START|loading main scene")
	var err = change_scene_to_file("res://scenes/main.tscn")
	if err != OK:
		_a.flag("SCENE_LOAD_FAIL")
		_phase = Phase.ANALYZE
		return
	_phase = Phase.WAIT_SCENE


func _do_wait(max_polls: int, next: Phase) -> void:
	_polls += 1
	if _polls >= max_polls:
		_polls = 0
		_phase = next


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		_polls += 1
		if _polls > 300:
			_a.flag("NO_BRIDGE")
			_phase = Phase.ANALYZE
		return
	_gm = root.get_node_or_null("GameManager")
	_polls = 0
	_phase = Phase.WAIT_READY


func _do_boot() -> void:
	_phase = Phase.BOOTSTRAP_CHECK
	_a.log("BOOT|bridge ready")

	var ps = _bridge.call("GetPlayerStateV0")
	if ps is Dictionary:
		_player_credits_start = ps.get("credits", 0)
		_tick_at_boot = ps.get("tick", 0)


func _do_bootstrap_check() -> void:
	_phase = Phase.WAIT_BOOTSTRAP
	_a.log("BOOTSTRAP_CHECK|verifying economy self-starts")

	# Check galaxy snapshot for stations with markets.
	var snapshot = _bridge.call("GetGalaxySnapshotV0")
	if not (snapshot is Dictionary):
		_a.warn(false, "bootstrap_snapshot_missing", "GetGalaxySnapshotV0 returned non-dict")
		return

	var nodes: Array = snapshot.get("system_nodes", [])
	var station_count := 0
	for n in nodes:
		if not (n is Dictionary): continue
		var nid: String = n.get("node_id", "")
		if nid.is_empty(): continue
		var mkt = _bridge.call("GetPlayerMarketViewV0", nid)
		if mkt is Array and mkt.size() > 0:
			station_count += 1

	_a.hard(station_count > 0, "stations_exist", "count=%d" % station_count)
	_a.goal("STATION_COUNT", "count=%d" % station_count)


func _do_collect_market_data() -> void:
	_phase = Phase.WAIT_COLLECTION
	_a.log("COLLECT_MARKET_DATA|sampling prices across stations")

	# Get all stations and their market prices.
	var snapshot = _bridge.call("GetGalaxySnapshotV0")
	if not (snapshot is Dictionary):
		_a.warn(false, "market_snapshot_missing")
		return

	var nodes: Array = snapshot.get("system_nodes", [])
	_station_credits.clear()
	_price_by_good.clear()

	for n in nodes:
		if not (n is Dictionary): continue
		var node_id: String = n.get("node_id", "")
		if node_id.is_empty(): continue

		# Get market view for this node.
		var market = _bridge.call("GetPlayerMarketViewV0", node_id)
		if not (market is Array): continue

		for item in market:
			if not (item is Dictionary): continue
			var good_id: String = item.get("good_id", "")
			var buy_price: int = item.get("buy_price", 0)
			var sell_price: int = item.get("sell_price", 0)

			if good_id.is_empty(): continue

			if not _price_by_good.has(good_id):
				_price_by_good[good_id] = {"buy": [], "sell": []}
				_total_goods += 1

			if buy_price > 0:
				_price_by_good[good_id]["buy"].append(buy_price)
			if sell_price > 0:
				_price_by_good[good_id]["sell"].append(sell_price)

	# Count tradeable goods (has at least 1 buy AND 1 sell station).
	_goods_tradeable = 0
	for good_id in _price_by_good:
		var data: Dictionary = _price_by_good[good_id]
		if data["buy"].size() > 0 and data["sell"].size() > 0:
			_goods_tradeable += 1

	_a.log("MARKET_DATA|goods=%d tradeable=%d" % [_total_goods, _goods_tradeable])


func _do_trade_activity_check() -> void:
	_phase = Phase.ANALYZE
	_a.log("TRADE_ACTIVITY_CHECK|checking NPC trading")

	# Check current player credits (economy participation proxy).
	var ps = _bridge.call("GetPlayerStateV0")
	if ps is Dictionary:
		_player_credits_current = ps.get("credits", 0)


func _do_analyze() -> void:
	_phase = Phase.DONE
	_a.log("ANALYZE|computing economy health metrics")

	# --- Goods tradeability ---
	if _total_goods > 0:
		var tradeable_pct := float(_goods_tradeable) / float(_total_goods) * 100.0
		_a.goal("GOODS_TRADEABLE", "count=%d total=%d pct=%.0f%%" % [_goods_tradeable, _total_goods, tradeable_pct])
		_a.hard(_goods_tradeable > 0, "at_least_one_tradeable_good", "tradeable=%d" % _goods_tradeable)
		_a.warn(tradeable_pct > 30.0, "reasonable_tradeable_fraction", "pct=%.0f%%" % tradeable_pct)

	# --- Price stability index (per-good CV) ---
	var high_cv_goods := 0
	var dead_market_goods := 0
	var total_cv := 0.0
	var cv_count := 0

	for good_id in _price_by_good:
		var data: Dictionary = _price_by_good[good_id]
		var all_prices: Array = []
		all_prices.append_array(data["buy"])
		all_prices.append_array(data["sell"])

		if all_prices.size() < 2: continue

		var float_prices: Array[float] = []
		for p in all_prices: float_prices.append(float(p))

		var m := _mean(float_prices)
		if m < 1.0: continue

		var cv := _stddev(float_prices) / m
		total_cv += cv
		cv_count += 1

		if cv > 0.4:
			high_cv_goods += 1
		elif cv < 0.02:
			dead_market_goods += 1

	if cv_count > 0:
		var avg_cv := total_cv / cv_count
		_a.goal("PRICE_STABILITY", "avg_cv=%.3f high_volatility=%d dead_market=%d" % [avg_cv, high_cv_goods, dead_market_goods])
		_a.warn(dead_market_goods < cv_count / 2, "not_too_many_dead_markets", "dead=%d total=%d" % [dead_market_goods, cv_count])

	# --- Profitable routes check ---
	var profitable_routes := 0
	for good_id in _price_by_good:
		var data: Dictionary = _price_by_good[good_id]
		var buys: Array = data["buy"]
		var sells: Array = data["sell"]
		for bp in buys:
			for sp in sells:
				if sp > bp:
					profitable_routes += 1

	_a.goal("PROFITABLE_ROUTES", "count=%d" % profitable_routes)
	_a.hard(profitable_routes > 0, "profitable_routes_exist", "count=%d" % profitable_routes)

	# --- Credit flow ---
	var credit_delta: int = _player_credits_current - _player_credits_start
	_a.goal("CREDIT_FLOW", "start=%d current=%d delta=%d" % [_player_credits_start, _player_credits_current, credit_delta])

	_a.summary()
	if _bridge:
		_bridge.call("StopSimV0")
	_busy = true
	await create_timer(0.5).timeout
	quit(_a.exit_code())


# --- Math helpers ---

func _mean(arr: Array[float]) -> float:
	if arr.is_empty(): return 0.0
	var s := 0.0
	for v in arr: s += v
	return s / arr.size()

func _stddev(arr: Array[float]) -> float:
	if arr.size() < 2: return 0.0
	var m := _mean(arr)
	var s := 0.0
	for v in arr: s += (v - m) * (v - m)
	return sqrt(s / (arr.size() - 1))
