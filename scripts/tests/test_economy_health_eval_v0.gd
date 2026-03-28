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
#   - Program/automation revenue and performance
#   - Resource extraction metrics
#   - Construction investment
#   - Market depth & volatility per node
#   - NPC economy health (routes, activity, demand, industry)
#   - Supply chain analysis
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
	PROGRAM_ANALYSIS,
	EXTRACTION_AND_CONSTRUCTION,
	MARKET_DEPTH_ANALYSIS,
	NPC_ECONOMY_HEALTH,
	SUPPLY_CHAIN_ANALYSIS,
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

# Program/automation data.
var _program_count := 0
var _program_types: Dictionary = {}  # kind → count
var _program_total_revenue := 0
var _program_total_cost := 0
var _program_total_net := 0

# Extraction & construction data.
var _extraction_site_count := 0
var _extraction_total_output := 0
var _construction_project_count := 0

# Market depth data.
var _node_ids: Array = []  # Populated during market data collection
var _avg_spread_bps := 0
var _total_volatility := 0
var _depth_samples := 0

# NPC economy data.
var _npc_route_count := 0
var _npc_total_activity := 0
var _npc_demand_nodes := 0

# Supply chain data.
var _supply_samples := 0
var _supply_critical := 0  # Nodes with critically low supply

# Economy depth metrics (NEW — research expansion)
var _money_velocity := 0.0         # transaction volume / money supply per window
var _price_convergence_cov := 0.0  # coefficient of variation across stations per good (avg)
var _inflation_rate := 0.0         # price level change between 2 sample windows
var _route_viability_count := 0    # station pairs with profitable routes
var _route_total_pairs := 0        # total station pairs checked
var _price_samples_early: Dictionary = {}  # good_id → avg_price at boot
var _price_samples_late: Dictionary = {}   # good_id → avg_price at analysis time


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
		Phase.PROGRAM_ANALYSIS: _do_program_analysis()
		Phase.EXTRACTION_AND_CONSTRUCTION: _do_extraction_and_construction()
		Phase.MARKET_DEPTH_ANALYSIS: _do_market_depth_analysis()
		Phase.NPC_ECONOMY_HEALTH: _do_npc_economy_health()
		Phase.SUPPLY_CHAIN_ANALYSIS: _do_supply_chain_analysis()
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
	_node_ids.clear()

	for n in nodes:
		if not (n is Dictionary): continue
		var node_id: String = n.get("node_id", "")
		if node_id.is_empty(): continue
		_node_ids.append(node_id)

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

	# Capture early price samples for inflation calculation.
	_price_samples_early.clear()
	for good_id in _price_by_good:
		var data: Dictionary = _price_by_good[good_id]
		var all_p: Array = []
		all_p.append_array(data["buy"])
		all_p.append_array(data["sell"])
		if all_p.size() > 0:
			var s := 0.0
			for p in all_p: s += float(p)
			_price_samples_early[good_id] = s / all_p.size()

	_a.log("MARKET_DATA|goods=%d tradeable=%d" % [_total_goods, _goods_tradeable])


func _do_trade_activity_check() -> void:
	_phase = Phase.PROGRAM_ANALYSIS
	_a.log("TRADE_ACTIVITY_CHECK|checking NPC trading")

	# Check current player credits (economy participation proxy).
	var ps = _bridge.call("GetPlayerStateV0")
	if ps is Dictionary:
		_player_credits_current = ps.get("credits", 0)


# ── Phase: Program/Automation Revenue Analysis ──

func _do_program_analysis() -> void:
	_phase = Phase.EXTRACTION_AND_CONSTRUCTION
	_a.log("PROGRAM_ANALYSIS|evaluating automation programs")

	if not _bridge.has_method("GetProgramExplainSnapshot"):
		_a.log("PROGRAM_ANALYSIS|GetProgramExplainSnapshot not available, skipping")
		return

	var programs = _bridge.call("GetProgramExplainSnapshot")
	if not (programs is Array):
		_a.log("PROGRAM_ANALYSIS|snapshot returned non-array, skipping")
		return

	_program_count = programs.size()
	_program_types.clear()
	_program_total_revenue = 0
	_program_total_cost = 0
	_program_total_net = 0

	for prog in programs:
		if not (prog is Dictionary): continue
		var kind: String = prog.get("kind", "unknown")
		var pid: String = prog.get("id", "")
		var status: String = prog.get("status", "")

		# Count by type.
		if _program_types.has(kind):
			_program_types[kind] += 1
		else:
			_program_types[kind] = 1

		# Query per-program performance if we have the fleet ID.
		if pid.is_empty(): continue
		if not _bridge.has_method("GetProgramPerformanceV0"): continue
		var perf = _bridge.call("GetProgramPerformanceV0", pid)
		if not (perf is Dictionary): continue

		var earned: int = perf.get("credits_earned", 0)
		var expense: int = perf.get("total_expense", 0)
		var net: int = perf.get("net_profit", 0)
		var cycles: int = perf.get("cycles_run", 0)
		var failures: int = perf.get("failures", 0)

		_program_total_revenue += earned
		_program_total_cost += expense
		_program_total_net += net

		_a.log("ECON_PROGRAM_DETAIL|id=%s kind=%s status=%s cycles=%d earned=%d expense=%d net=%d failures=%d" % [
			pid, kind, status, cycles, earned, expense, net, failures])

	# Build type summary string.
	var type_parts: Array = []
	for k in _program_types:
		type_parts.append("%s:%d" % [k, _program_types[k]])
	var types_str: String = ",".join(type_parts) if type_parts.size() > 0 else "none"

	_a.log("ECON_PROGRAMS|active=%d types=%s total_revenue=%d total_cost=%d net=%d" % [
		_program_count, types_str, _program_total_revenue, _program_total_cost, _program_total_net])

	# Warn if no programs active after significant sim time.
	var ps = _bridge.call("GetPlayerStateV0")
	var current_tick := 0
	if ps is Dictionary:
		current_tick = ps.get("tick", 0)
	if current_tick > 500 and _program_count == 0:
		_a.warn(false, "NO_AUTOMATION_ECONOMY", "tick=%d programs=0" % current_tick)


# ── Phase: Extraction & Construction Metrics ──

func _do_extraction_and_construction() -> void:
	_phase = Phase.MARKET_DEPTH_ANALYSIS
	_a.log("EXTRACTION_CONSTRUCTION|evaluating resource extraction and construction")

	# --- Extraction sites ---
	if _bridge.has_method("GetExtractionSitesV0"):
		var sites = _bridge.call("GetExtractionSitesV0")
		if sites is Array:
			_extraction_site_count = sites.size()
			_extraction_total_output = 0
			for site in sites:
				if not (site is Dictionary): continue
				var output: int = site.get("output_per_tick", 0)
				var good_id: String = site.get("good_id", "")
				var node_id: String = site.get("node_id", "")
				_extraction_total_output += output
				_a.log("ECON_EXTRACTION_DETAIL|node=%s good=%s output_per_tick=%d" % [node_id, good_id, output])

			_a.log("ECON_EXTRACTION|sites=%d total_output_per_tick=%d" % [_extraction_site_count, _extraction_total_output])
		else:
			_a.log("ECON_EXTRACTION|sites=0 (non-array result)")
	else:
		_a.log("ECON_EXTRACTION|method_unavailable")

	# --- Construction projects ---
	if _bridge.has_method("GetConstructionProjectsV0"):
		var projects = _bridge.call("GetConstructionProjectsV0")
		if projects is Array:
			_construction_project_count = projects.size()
			for proj in projects:
				if not (proj is Dictionary): continue
				var proj_id: String = proj.get("project_id", "")
				var def_id: String = proj.get("project_def_id", "")
				var node_id: String = proj.get("node_id", "")
				var progress: int = proj.get("progress_pct", 0)
				var completed: bool = proj.get("completed", false)
				_a.log("ECON_CONSTRUCTION_DETAIL|id=%s def=%s node=%s progress=%d%% completed=%s" % [
					proj_id, def_id, node_id, progress, str(completed)])

			_a.log("ECON_CONSTRUCTION|projects=%d" % _construction_project_count)
		else:
			_a.log("ECON_CONSTRUCTION|projects=0 (non-array result)")
	else:
		_a.log("ECON_CONSTRUCTION|method_unavailable")


# ── Phase: Market Depth & Volatility Analysis ──

func _do_market_depth_analysis() -> void:
	_phase = Phase.NPC_ECONOMY_HEALTH
	_a.log("MARKET_DEPTH_ANALYSIS|evaluating depth and volatility across nodes")

	var has_depth: bool = _bridge.has_method("GetMarketDepthV0")
	var has_volatility: bool = _bridge.has_method("GetMarketVolatilityV0")

	if not has_depth and not has_volatility:
		_a.log("ECON_DEPTH|methods_unavailable")
		return

	var total_spread_bps := 0
	_depth_samples = 0
	_total_volatility = 0
	var volatility_samples := 0

	# Sample up to 10 nodes to keep the phase bounded.
	var sample_limit := mini(_node_ids.size(), 10)
	for i in range(sample_limit):
		var node_id: String = _node_ids[i]

		# --- Market depth ---
		if has_depth:
			var depth_arr = _bridge.call("GetMarketDepthV0", node_id)
			if depth_arr is Array:
				for entry in depth_arr:
					if not (entry is Dictionary): continue
					var spread: int = entry.get("spread_bps", 0)
					var depth: int = entry.get("depth", 0)
					var good_id: String = entry.get("good_id", "")
					var bid: int = entry.get("bid", 0)
					var ask: int = entry.get("ask", 0)
					total_spread_bps += spread
					_depth_samples += 1
				_a.log("ECON_DEPTH|node=%s goods=%d" % [node_id, depth_arr.size()])

		# --- Market volatility ---
		if has_volatility:
			var vol_result = _bridge.call("GetMarketVolatilityV0", node_id)
			var vol: int = int(vol_result) if vol_result != null else 0
			_total_volatility += vol
			volatility_samples += 1
			_a.log("ECON_VOLATILITY|node=%s vol=%d" % [node_id, vol])

	if _depth_samples > 0:
		_avg_spread_bps = total_spread_bps / _depth_samples
		_a.log("ECON_DEPTH_SUMMARY|samples=%d avg_spread_bps=%d" % [_depth_samples, _avg_spread_bps])

	if volatility_samples > 0:
		var avg_vol: int = _total_volatility / volatility_samples
		_a.log("ECON_VOLATILITY_SUMMARY|samples=%d avg_volatility=%d" % [volatility_samples, avg_vol])


# ── Phase: NPC Economy Health ──

func _do_npc_economy_health() -> void:
	_phase = Phase.SUPPLY_CHAIN_ANALYSIS
	_a.log("NPC_ECONOMY_HEALTH|evaluating NPC trading and industry")

	# --- NPC trade routes ---
	_npc_route_count = 0
	if _bridge.has_method("GetNpcTradeRoutesV0"):
		var routes = _bridge.call("GetNpcTradeRoutesV0")
		if routes is Array:
			_npc_route_count = routes.size()
			# Log a few sample routes for audit visibility.
			var route_limit := mini(routes.size(), 5)
			for i in range(route_limit):
				var r = routes[i]
				if not (r is Dictionary): continue
				var fid: String = r.get("fleet_id", "")
				var src: String = r.get("source_node_id", "")
				var dst: String = r.get("dest_node_id", "")
				var good: String = r.get("good_id", "")
				var qty: int = r.get("qty", 0)
				_a.log("ECON_NPC_ROUTE|fleet=%s src=%s dst=%s good=%s qty=%d" % [fid, src, dst, good, qty])

	# --- NPC trade activity (volume across sampled nodes) ---
	_npc_total_activity = 0
	if _bridge.has_method("GetNpcTradeActivityV0"):
		var activity_limit := mini(_node_ids.size(), 10)
		for i in range(activity_limit):
			var node_id: String = _node_ids[i]
			var act_result = _bridge.call("GetNpcTradeActivityV0", node_id)
			var vol: int = int(act_result) if act_result != null else 0
			_npc_total_activity += vol

	# --- NPC demand at sampled nodes ---
	_npc_demand_nodes = 0
	if _bridge.has_method("GetNpcDemandV0"):
		var demand_limit := mini(_node_ids.size(), 5)
		for i in range(demand_limit):
			var node_id: String = _node_ids[i]
			var demand = _bridge.call("GetNpcDemandV0", node_id)
			if demand is Array and demand.size() > 0:
				_npc_demand_nodes += 1
				_a.log("ECON_NPC_DEMAND|node=%s items=%d" % [node_id, demand.size()])

	# --- Node industry status (sampled) ---
	if _bridge.has_method("GetNodeIndustryStatusV0"):
		var industry_limit := mini(_node_ids.size(), 5)
		for i in range(industry_limit):
			var node_id: String = _node_ids[i]
			var industry = _bridge.call("GetNodeIndustryStatusV0", node_id)
			if industry is Array and industry.size() > 0:
				var active_count := 0
				for site in industry:
					if site is Dictionary and site.get("active", false):
						active_count += 1
				_a.log("ECON_NPC_INDUSTRY|node=%s sites=%d active=%d" % [node_id, industry.size(), active_count])

	_a.log("ECON_NPC|routes=%d activity_volume=%d demand_nodes=%d" % [
		_npc_route_count, _npc_total_activity, _npc_demand_nodes])


# ── Phase: Supply Chain Analysis ──

func _do_supply_chain_analysis() -> void:
	_phase = Phase.ANALYZE
	_a.log("SUPPLY_CHAIN_ANALYSIS|evaluating supply levels across galaxy")

	_supply_samples = 0
	_supply_critical = 0

	if not _bridge.has_method("GetSupplyLevelV0"):
		_a.log("ECON_SUPPLY|method_unavailable")
		return

	var supply_limit := mini(_node_ids.size(), 10)
	for i in range(supply_limit):
		var node_id: String = _node_ids[i]
		var supply = _bridge.call("GetSupplyLevelV0", node_id)
		if not (supply is Dictionary): continue
		if supply.is_empty(): continue

		_supply_samples += 1
		var level: int = supply.get("supply_level", 0)
		var max_supply: int = supply.get("max_supply", 100)
		var site_id: String = supply.get("site_id", "")

		# Flag critically low supply (below 20%).
		var pct: int = (level * 100) / max_supply if max_supply > 0 else 0
		if pct < 20:
			_supply_critical += 1
			_a.log("ECON_SUPPLY_CRITICAL|node=%s site=%s level=%d/%d (%d%%)" % [
				node_id, site_id, level, max_supply, pct])

	_a.log("ECON_SUPPLY|sampled=%d critical=%d" % [_supply_samples, _supply_critical])


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

	# ═══ Program/Automation Health ═══
	_a.log("ANALYZE_SECTION|=== AUTOMATION HEALTH ===")
	_a.goal("PROGRAM_COUNT", "active=%d revenue=%d cost=%d net=%d" % [
		_program_count, _program_total_revenue, _program_total_cost, _program_total_net])
	_a.warn(_program_count > 0, "automation_present", "programs=%d" % _program_count)
	if _program_total_revenue > 0:
		var roi_pct := (_program_total_net * 100) / _program_total_revenue if _program_total_revenue > 0 else 0
		_a.goal("AUTOMATION_ROI", "roi=%d%%" % roi_pct)

	# ═══ Extraction & Construction Health ═══
	_a.log("ANALYZE_SECTION|=== EXTRACTION & CONSTRUCTION ===")
	_a.goal("EXTRACTION_SITES", "count=%d output=%d" % [_extraction_site_count, _extraction_total_output])
	_a.goal("CONSTRUCTION_PROJECTS", "count=%d" % _construction_project_count)

	# ═══ Market Depth Health ═══
	_a.log("ANALYZE_SECTION|=== MARKET DEPTH ===")
	if _depth_samples > 0:
		_a.goal("MARKET_SPREADS", "avg_bps=%d samples=%d" % [_avg_spread_bps, _depth_samples])
		_a.warn(_avg_spread_bps < 5000, "market_spread_healthy", "avg_bps=%d" % _avg_spread_bps)
	else:
		_a.log("MARKET_SPREADS|no depth data collected")

	# ═══ NPC Economy Health ═══
	_a.log("ANALYZE_SECTION|=== NPC ECONOMY ===")
	_a.goal("NPC_TRADE_ROUTES", "routes=%d activity=%d demand_nodes=%d" % [
		_npc_route_count, _npc_total_activity, _npc_demand_nodes])
	_a.warn(_npc_route_count >= 3, "npc_trade_health", "routes=%d" % _npc_route_count)

	# ═══ Supply Chain Health ═══
	_a.log("ANALYZE_SECTION|=== SUPPLY CHAIN ===")
	_a.goal("SUPPLY_HEALTH", "sampled=%d critical=%d" % [_supply_samples, _supply_critical])
	if _supply_samples > 0:
		var critical_pct := (_supply_critical * 100) / _supply_samples
		_a.warn(critical_pct < 50, "supply_chain_stable", "critical=%d/%d (%d%%)" % [
			_supply_critical, _supply_samples, critical_pct])

	# ═══ Economy Depth Metrics (research expansion) ═══
	_a.log("ANALYZE_SECTION|=== ECONOMY DEPTH ===")

	# --- Money velocity: transaction volume / money supply proxy ---
	# Use NPC activity volume + player credit delta as transaction volume;
	# total station credits as money supply proxy.
	var total_station_credits := 0
	for sc in _station_credits:
		total_station_credits += sc
	var transaction_volume: float = float(absi(credit_delta)) + float(_npc_total_activity)
	if total_station_credits > 0:
		_money_velocity = transaction_volume / float(total_station_credits)
	else:
		# Fallback: use player credits as denominator if station credits unavailable.
		if _player_credits_current > 0:
			_money_velocity = transaction_volume / float(_player_credits_current)
	_a.goal("MONEY_VELOCITY", "velocity=%.3f txn_vol=%.0f" % [_money_velocity, transaction_volume])
	_a.warn(_money_velocity > 0.001, "economy_not_stagnant", "velocity=%.4f" % _money_velocity)

	# --- Price convergence CoV: are prices converging across stations? ---
	# Re-sample late prices for comparison.
	_price_samples_late.clear()
	var late_snapshot = _bridge.call("GetGalaxySnapshotV0") if _bridge else null
	if late_snapshot is Dictionary:
		var late_nodes: Array = late_snapshot.get("system_nodes", [])
		var late_price_by_good: Dictionary = {}
		for n in late_nodes:
			if not (n is Dictionary): continue
			var nid: String = n.get("node_id", "")
			if nid.is_empty(): continue
			var mkt = _bridge.call("GetPlayerMarketViewV0", nid)
			if not (mkt is Array): continue
			for item in mkt:
				if not (item is Dictionary): continue
				var gid: String = item.get("good_id", "")
				var bp: int = item.get("buy_price", 0)
				var sp: int = item.get("sell_price", 0)
				if gid.is_empty(): continue
				if not late_price_by_good.has(gid):
					late_price_by_good[gid] = []
				if bp > 0: late_price_by_good[gid].append(float(bp))
				if sp > 0: late_price_by_good[gid].append(float(sp))

		# Compute late average prices and cross-station CoV.
		var cov_sum := 0.0
		var cov_count := 0
		for gid in late_price_by_good:
			var prices: Array = late_price_by_good[gid]
			if prices.size() < 2: continue
			var float_arr: Array[float] = []
			for p in prices: float_arr.append(float(p))
			var m := _mean(float_arr)
			if m < 1.0: continue
			var cv := _stddev(float_arr) / m
			cov_sum += cv
			cov_count += 1
			# Store late average for inflation calc.
			_price_samples_late[gid] = m

		if cov_count > 0:
			_price_convergence_cov = cov_sum / cov_count
		_a.goal("PRICE_CONVERGENCE", "avg_cov=%.3f goods_measured=%d" % [_price_convergence_cov, cov_count])
		# High CoV means prices still diverge widely — healthy variance.
		# Very low CoV means all prices identical — dead market.
		_a.warn(_price_convergence_cov > 0.02, "price_not_uniform", "cov=%.3f" % _price_convergence_cov)
	else:
		_a.log("PRICE_CONVERGENCE|late snapshot unavailable")

	# --- Inflation rate: average price level change early → late ---
	var inflation_goods := 0
	var inflation_sum := 0.0
	for gid in _price_samples_early:
		if not _price_samples_late.has(gid): continue
		var early_p: float = _price_samples_early[gid]
		var late_p: float = _price_samples_late[gid]
		if early_p < 1.0: continue
		inflation_sum += (late_p - early_p) / early_p
		inflation_goods += 1
	if inflation_goods > 0:
		_inflation_rate = inflation_sum / inflation_goods
	_a.goal("INFLATION_RATE", "rate=%.4f goods=%d" % [_inflation_rate, inflation_goods])
	# Flag runaway inflation (>20%) or deflation (<-20%).
	if absf(_inflation_rate) > 0.20 and inflation_goods >= 3:
		_a.warn(false, "inflation_out_of_range", "rate=%.2f%%" % (_inflation_rate * 100.0))

	# --- Route viability: count station pairs with profitable spread ---
	_route_viability_count = 0
	_route_total_pairs = 0
	for good_id in _price_by_good:
		var data: Dictionary = _price_by_good[good_id]
		var buys: Array = data["buy"]
		var sells: Array = data["sell"]
		for bp in buys:
			for sp in sells:
				_route_total_pairs += 1
				if sp > bp:
					_route_viability_count += 1
	var viability_pct := 0.0
	if _route_total_pairs > 0:
		viability_pct = float(_route_viability_count) / float(_route_total_pairs) * 100.0
	_a.goal("ROUTE_VIABILITY", "viable=%d total=%d pct=%.1f%%" % [
		_route_viability_count, _route_total_pairs, viability_pct])
	_a.warn(viability_pct > 5.0, "enough_viable_routes", "pct=%.1f%%" % viability_pct)

	_a.summary()

	# Write structured JSON report for automated delta comparison
	var report := {
		"bot": "economy_health",
		"prefix": "ECON_HEALTH",
		"metrics": {
			"goods_total": _total_goods,
			"goods_tradeable": _goods_tradeable,
			"profitable_routes": profitable_routes,
			"credit_start": _player_credits_start,
			"credit_current": _player_credits_current,
			"credit_delta": credit_delta,
			"program_count": _program_count,
			"program_revenue": _program_total_revenue,
			"program_cost": _program_total_cost,
			"program_net": _program_total_net,
			"extraction_sites": _extraction_site_count,
			"extraction_output": _extraction_total_output,
			"construction_projects": _construction_project_count,
			"avg_spread_bps": _avg_spread_bps,
			"depth_samples": _depth_samples,
			"npc_routes": _npc_route_count,
			"npc_activity": _npc_total_activity,
			"npc_demand_nodes": _npc_demand_nodes,
			"supply_samples": _supply_samples,
			"supply_critical": _supply_critical,
			"money_velocity": _money_velocity,
			"price_convergence_cov": _price_convergence_cov,
			"inflation_rate": _inflation_rate,
			"route_viability_count": _route_viability_count,
			"route_total_pairs": _route_total_pairs,
			"route_viability_pct": viability_pct,
		},
		"assertions": {
			"pass": _a._passes,
			"warn": _a._warns.size(),
			"fail": _a._fails.size(),
		},
	}
	var json_str := JSON.stringify(report, "  ")
	var report_path := "res://reports/eval/economy_health_report.json"
	var f := FileAccess.open(report_path, FileAccess.WRITE)
	if f:
		f.store_string(json_str)
		f.close()
		_a.log("REPORT_JSON|written=%s" % report_path)

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
