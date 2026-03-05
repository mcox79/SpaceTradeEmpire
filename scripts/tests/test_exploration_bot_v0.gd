extends SceneTree

## EPIC.X.EXPERIENCE_PROOF.V0 — Layer 3: GDScript Exploration Bot
## Decision-making bot that plays through SimBridge, recording actions and flags.
## Tests full stack: GDScript → SimBridge → SimKernel → state queries back.
##
## Unlike scripted scenarios (fixed path), the bot DECIDES what to do
## based on observable game state. Flags issues that scripted tests miss.

const PREFIX := "EXPV0|BOT|"
const MAX_POLLS := 600
const TICK_BUDGET := 400       # Total decision cycles
const MAX_BUY_QTY := 10
const EXPLORE_EVERY_N := 4    # Force exploration after N trades
const ACT_INTERVAL := 3       # Sim steps between bot actions

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	INIT_GALAXY,
	BOT_LOOP,
	REPORT,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null

# Bot state
var _cycle := 0
var _sub_tick := 0
var _adj: Dictionary = {}             # node_id → [neighbor_ids]
var _market_for_node: Dictionary = {}  # node_id → market_id (not directly available; we use node_id for bridge calls)
var _all_nodes: Array = []
var _all_lanes: Array = []
var _visited: Dictionary = {}         # node_id → true
var _trades_since_explore := 0
var _exploration_target := ""

# Tracking
var _actions: Array = []
var _flags: Array = []
var _credit_trajectory: Array = []
var _total_buys := 0
var _total_sells := 0
var _total_travels := 0
var _total_idles := 0
var _total_spent := 0
var _total_earned := 0
var _goods_bought: Dictionary = {}
var _goods_sold: Dictionary = {}
var _consecutive_idles := 0
var _start_credits := 0


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_phase = Phase.INIT_GALAXY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.INIT_GALAXY:
			_init_galaxy()
			_phase = Phase.BOT_LOOP

		Phase.BOT_LOOP:
			if _cycle >= TICK_BUDGET:
				_phase = Phase.REPORT
				return false

			if _sub_tick == 0:
				_bot_decide_and_act()
				_sub_tick = 1
			else:
				_sub_tick += 1
				if _sub_tick >= ACT_INTERVAL:
					_sub_tick = 0
					_cycle += 1

			# Record credits every cycle
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			_credit_trajectory.append(int(ps.get("credits", 0)))

		Phase.REPORT:
			_generate_report()
			_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


func _init_galaxy() -> void:
	# Build adjacency graph from galaxy snapshot
	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_nodes = galaxy.get("system_nodes", [])
	_all_lanes = galaxy.get("lane_edges", [])

	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if not nid.is_empty():
			_adj[nid] = []

	for lane in _all_lanes:
		var from_id: String = str(lane.get("from_id", ""))
		var to_id: String = str(lane.get("to_id", ""))
		if not from_id.is_empty() and not to_id.is_empty():
			if _adj.has(from_id):
				_adj[from_id].append(to_id)
			if _adj.has(to_id):
				_adj[to_id].append(from_id)

	# Get starting location
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	if not loc.is_empty():
		_visited[loc] = true

	_start_credits = int(ps.get("credits", 0))
	_credit_trajectory.append(_start_credits)

	print(PREFIX + "INIT|nodes=%d lanes=%d start=%s credits=%d" % [
		_all_nodes.size(), _all_lanes.size(), loc, _start_credits])


func _bot_decide_and_act() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	var credits: int = int(ps.get("credits", 0))
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var cargo_count: int = int(ps.get("cargo_count", 0))

	if loc.is_empty():
		_record_action(_cycle, "IDLE", loc, "", 0, "no location")
		_total_idles += 1
		return

	# Phase 0: Committed exploration — keep traveling
	if not _exploration_target.is_empty():
		if loc == _exploration_target or _visited.has(_exploration_target):
			_exploration_target = ""
		else:
			var hop := _get_next_hop(loc, _exploration_target)
			if not hop.is_empty():
				_do_travel(loc, hop, "committed exploration toward %s" % _exploration_target)
				return
			_exploration_target = ""

	# Phase 0b: Exploration pressure
	var has_unvisited := false
	for nid in _adj.keys():
		if not _visited.has(nid):
			has_unvisited = true
			break

	if has_unvisited and _trades_since_explore >= EXPLORE_EVERY_N and cargo_count == 0:
		var target := _find_nearest_unvisited(loc)
		if not target.is_empty():
			_exploration_target = target
			_trades_since_explore = 0
			var hop := _get_next_hop(loc, target)
			if not hop.is_empty():
				_do_travel(loc, hop, "starting exploration toward %s" % target)
				return

	# Phase 1: If carrying cargo → find best sell
	if cargo_count > 0 and cargo.size() > 0:
		var sell_result := _try_sell(loc, cargo, credits)
		if sell_result:
			return

	# Phase 2: No cargo → find best buy
	if cargo_count == 0:
		var buy_result := _try_buy(loc, credits)
		if buy_result:
			return

	# Phase 3: Explore unvisited
	if has_unvisited:
		var target := _find_nearest_unvisited(loc)
		if not target.is_empty():
			var hop := _get_next_hop(loc, target)
			if not hop.is_empty():
				_do_travel(loc, hop, "fallback explore toward %s" % target)
				return

	# Phase 4: Nothing to do
	_consecutive_idles += 1
	_total_idles += 1
	_record_action(_cycle, "IDLE", loc, "", 0, "nothing to do")

	if _consecutive_idles >= 10:
		_add_flag("STUCK_NO_ACTIONS", "CRITICAL",
			"Bot idle for %d cycles at %s" % [_consecutive_idles, loc],
			"Economy collapsed or bot stuck.")


func _try_sell(loc: String, cargo: Array, credits: int) -> bool:
	# Get market view at all reachable nodes to find best sell
	var best_good := ""
	var best_sell_node := ""
	var best_sell_price := 0

	for item in cargo:
		var good_id: String = str(item.get("good_id", ""))
		var qty: int = int(item.get("qty", 0))
		if good_id.is_empty() or qty <= 0:
			continue

		# Check sell price at every node we know about
		for n in _all_nodes:
			var nid: String = str(n.get("node_id", ""))
			if nid.is_empty():
				continue
			var market_view: Array = _bridge.call("GetPlayerMarketViewV0", nid)
			for entry in market_view:
				if str(entry.get("good_id", "")) == good_id:
					var sp: int = int(entry.get("sell_price", 0))
					if sp > best_sell_price:
						best_sell_price = sp
						best_sell_node = nid
						best_good = good_id

	if best_sell_node.is_empty():
		return false

	if best_sell_node == loc:
		# Sell here
		var qty := 0
		for item in cargo:
			if str(item.get("good_id", "")) == best_good:
				qty = int(item.get("qty", 0))
				break
		if qty > 0:
			_do_sell(loc, best_good, qty, credits)
			return true
	else:
		var hop := _get_next_hop(loc, best_sell_node)
		if not hop.is_empty():
			_do_travel(loc, hop, "toward sell %s at %s" % [best_good, best_sell_node])
			return true

	return false


func _try_buy(loc: String, credits: int) -> bool:
	# Find best buy opportunity across all markets
	var best_node := ""
	var best_good := ""
	var best_profit := 0
	var best_buy_price := 0

	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var market_view: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in market_view:
			var good_id: String = str(entry.get("good_id", ""))
			var bp: int = int(entry.get("buy_price", 0))
			var available: int = int(entry.get("quantity", 0))
			if good_id.is_empty() or bp <= 0 or available <= 0 or bp > credits:
				continue

			# Find best sell elsewhere
			var max_sell := 0
			for n2 in _all_nodes:
				var nid2: String = str(n2.get("node_id", ""))
				if nid2 == nid or nid2.is_empty():
					continue
				var mv2: Array = _bridge.call("GetPlayerMarketViewV0", nid2)
				for e2 in mv2:
					if str(e2.get("good_id", "")) == good_id:
						var sp: int = int(e2.get("sell_price", 0))
						if sp > max_sell:
							max_sell = sp

			var profit := max_sell - bp
			if profit > best_profit:
				best_profit = profit
				best_node = nid
				best_good = good_id
				best_buy_price = bp

	if best_node.is_empty() or best_profit <= 0:
		return false

	if best_node == loc:
		var market_view: Array = _bridge.call("GetPlayerMarketViewV0", loc)
		var available := 0
		for entry in market_view:
			if str(entry.get("good_id", "")) == best_good:
				available = int(entry.get("quantity", 0))
				break
		var affordable := credits / best_buy_price if best_buy_price > 0 else 0
		var qty: int = mini(mini(MAX_BUY_QTY, available), affordable)
		if qty > 0:
			_do_buy(loc, best_good, qty, credits)
			return true
	else:
		var hop := _get_next_hop(loc, best_node)
		if not hop.is_empty():
			_do_travel(loc, hop, "toward buy %s at %s (profit %d/u)" % [best_good, best_node, best_profit])
			return true

	return false


# ── Action executors ──

func _do_buy(loc: String, good_id: String, qty: int, credits_before: int) -> void:
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, true)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after: int = int(ps.get("credits", 0))
	var succeeded := credits_after < credits_before
	_record_action(_cycle, "BUY", loc, good_id, qty,
		"buy %dx %s @ %s, credits %d→%d" % [qty, good_id, loc, credits_before, credits_after])
	if succeeded:
		_total_buys += 1
		_trades_since_explore += 1
		_total_spent += credits_before - credits_after
		_goods_bought[good_id] = true
		_consecutive_idles = 0
	else:
		_add_flag("TRADE_NO_EFFECT", "CRITICAL",
			"BUY %dx %s at %s had no effect (credits %d→%d)" % [qty, good_id, loc, credits_before, credits_after],
			"DispatchPlayerTradeV0 buy failed. Check SimBridge.Market.cs, BuyCommand.cs")


func _do_sell(loc: String, good_id: String, qty: int, credits_before: int) -> void:
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, false)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after: int = int(ps.get("credits", 0))
	var succeeded := credits_after > credits_before
	_record_action(_cycle, "SELL", loc, good_id, qty,
		"sell %dx %s @ %s, credits %d→%d" % [qty, good_id, loc, credits_before, credits_after])
	if succeeded:
		_total_sells += 1
		_trades_since_explore += 1
		_total_earned += credits_after - credits_before
		_goods_sold[good_id] = true
		_consecutive_idles = 0
	else:
		_add_flag("TRADE_NO_EFFECT", "CRITICAL",
			"SELL %dx %s at %s had no effect (credits %d→%d)" % [qty, good_id, loc, credits_before, credits_after],
			"DispatchPlayerTradeV0 sell failed. Check SimBridge.Market.cs, SellCommand.cs")


func _do_travel(from: String, to: String, reason: String) -> void:
	_bridge.call("DispatchPlayerArriveV0", to)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var new_loc: String = str(ps.get("current_node_id", ""))
	var succeeded := new_loc == to
	_record_action(_cycle, "TRAVEL", to, "", 0, reason)
	if succeeded:
		_total_travels += 1
		_visited[to] = true
		_consecutive_idles = 0
	else:
		_add_flag("TRAVEL_FAILED", "WARNING",
			"Travel to %s from %s failed (now at %s)" % [to, from, new_loc],
			"DispatchPlayerArriveV0 failed. Check PlayerArriveCommand.cs")


# ── Graph helpers ──

func _get_next_hop(from: String, to: String) -> String:
	if from == to:
		return ""
	# BFS
	var queue: Array = [from]
	var parent: Dictionary = {}
	var seen: Dictionary = {from: true}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if current == to:
			var hop := to
			while parent.has(hop) and parent[hop] != from:
				hop = parent[hop]
			return hop
		if not _adj.has(current):
			continue
		var neighbors: Array = _adj[current]
		neighbors.sort()
		for n in neighbors:
			if not seen.has(n):
				seen[n] = true
				parent[n] = current
				queue.append(n)
	return ""


func _find_nearest_unvisited(from: String) -> String:
	var queue: Array = [from]
	var seen: Dictionary = {from: true}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if not _visited.has(current) and current != from:
			return current
		if not _adj.has(current):
			continue
		var neighbors: Array = _adj[current]
		neighbors.sort()
		for n in neighbors:
			if not seen.has(n):
				seen[n] = true
				queue.append(n)
	return ""


# ── Recording ──

func _record_action(cycle: int, type: String, node: String, good: String, qty: int, detail: String) -> void:
	_actions.append({
		"cycle": cycle,
		"type": type,
		"node": node,
		"good": good,
		"qty": qty,
		"detail": detail
	})


func _add_flag(id: String, severity: String, detail: String, diagnostic: String) -> void:
	# Deduplicate
	for f in _flags:
		if f.get("id", "") == id:
			return
	_flags.append({
		"id": id,
		"severity": severity,
		"detail": detail,
		"diagnostic": diagnostic
	})
	print(PREFIX + "FLAG|[%s] %s: %s" % [severity, id, detail])


# ── Report ──

func _generate_report() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var end_credits: int = int(ps.get("credits", 0))
	var net_profit: int = end_credits - _start_credits

	# Post-run analysis
	if _total_buys > 0 and _total_sells > 0 and net_profit < 0:
		_add_flag("NET_LOSS", "WARNING",
			"Bot lost money: %d→%d (%d buys, %d sells)" % [_start_credits, end_credits, _total_buys, _total_sells],
			"Trading not profitable through bridge. Check price model.")

	if _total_buys == 0 and TICK_BUDGET > 50:
		_add_flag("NEVER_BOUGHT", "CRITICAL",
			"Bot never bought anything in %d cycles" % TICK_BUDGET,
			"DispatchPlayerTradeV0 buy path may be broken, or no profitable goods found.")

	if _total_sells == 0 and _total_buys > 0:
		_add_flag("NEVER_SOLD", "CRITICAL",
			"Bot bought but never sold in %d cycles" % TICK_BUDGET,
			"Sell path broken or bot couldn't find sell destination.")

	# Coverage
	var visited_count: int = _visited.size()
	var total_nodes: int = _all_nodes.size()
	var visit_pct: float = float(visited_count) / max(1, total_nodes)
	if visit_pct < 0.5 and TICK_BUDGET >= 100:
		_add_flag("LOW_EXPLORATION", "WARNING",
			"Visited %d/%d nodes (%d%%)" % [visited_count, total_nodes, int(visit_pct * 100)],
			"Bot didn't reach half the galaxy.")

	# Untraded goods
	var all_goods: Dictionary = {}
	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var mv: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in mv:
			var gid: String = str(entry.get("good_id", ""))
			if not gid.is_empty():
				all_goods[gid] = true

	var untraded: Array = []
	for gid in all_goods.keys():
		if not _goods_bought.has(gid) and not _goods_sold.has(gid):
			untraded.append(gid)
	if untraded.size() > 0:
		untraded.sort()
		_add_flag("UNTRADED_GOODS", "INFO",
			"Never traded: %s" % str(untraded),
			"Some goods lack profitable routes or bot prioritization skips them.")

	# Print summary
	print(PREFIX + "SUMMARY|ticks=%d nodes=%d/%d net=%d buys=%d sells=%d travels=%d idles=%d" % [
		TICK_BUDGET, visited_count, total_nodes, net_profit,
		_total_buys, _total_sells, _total_travels, _total_idles])
	print(PREFIX + "CREDITS|start=%d end=%d spent=%d earned=%d" % [
		_start_credits, end_credits, _total_spent, _total_earned])
	print(PREFIX + "GOODS_BOUGHT|%s" % str(_goods_bought.keys()))
	print(PREFIX + "GOODS_SOLD|%s" % str(_goods_sold.keys()))
	print(PREFIX + "FLAGS|count=%d" % _flags.size())

	for f in _flags:
		print(PREFIX + "FLAG_DETAIL|[%s] %s: %s" % [
			str(f.get("severity", "")),
			str(f.get("id", "")),
			str(f.get("detail", ""))])

	# Critical flag count determines pass/fail
	var critical_count := 0
	for f in _flags:
		if str(f.get("severity", "")) == "CRITICAL":
			critical_count += 1

	if critical_count == 0:
		print(PREFIX + "PASS")
	else:
		print(PREFIX + "FAIL|%d critical flags" % critical_count)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
