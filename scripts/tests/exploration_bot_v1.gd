extends SceneTree

## Exploration Bot V1 — Parameterized autonomous game-logic tester
## Modes: trade (economy), combat (fighting), stress (long-run), full (all)
## Usage: godot --headless --path . -s res://scripts/tests/exploration_bot_v1.gd -- --mode trade --cycles 400

const PREFIX := "BOT|"
const MAX_POLLS := 600
const MAX_BUY_QTY := 10
const EXPLORE_EVERY_N := 4
const ACT_INTERVAL := 3
const COMBAT_COOLDOWN := 5   # Cycles between engagements

# Mode defaults
const DEFAULT_CYCLES := {
	"trade": 400,
	"combat": 200,
	"stress": 1500,
	"full": 600,
}

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

# Config (parsed from CLI)
var _mode := "trade"
var _tick_budget := 400

# Bot state
var _cycle := 0
var _sub_tick := 0
var _adj: Dictionary = {}
var _all_nodes: Array = []
var _all_lanes: Array = []
var _visited: Dictionary = {}
var _trades_since_explore := 0
var _exploration_target := ""

# Tracking — trade
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

# Tracking — combat
var _total_combats := 0
var _combat_cooldown_remaining := 0
var _total_kills := 0
var _total_damage_dealt := 0
var _combat_hp_init_done := false

# Tracking — stress
var _price_history: Dictionary = {}   # good_id → [prices]
var _price_window_size := 50
var _consecutive_credit_unchanged := 0
var _last_credit_value := -1
var _idle_cycles_total := 0

# Output
var _output_dir := ""


func _initialize() -> void:
	_parse_args()
	_tick_budget = DEFAULT_CYCLES.get(_mode, 400)

	# Re-parse for explicit --cycles override
	var args = OS.get_cmdline_user_args()
	for i in range(args.size()):
		if args[i] == "--cycles" and i + 1 < args.size():
			_tick_budget = int(args[i + 1])

	_output_dir = "res://reports/bot/%s" % _mode
	print(PREFIX + "START|mode=%s cycles=%d" % [_mode, _tick_budget])


func _parse_args() -> void:
	var args = OS.get_cmdline_user_args()
	for i in range(args.size()):
		if args[i] == "--mode" and i + 1 < args.size():
			var m: String = args[i + 1].to_lower()
			if m in ["trade", "combat", "stress", "full"]:
				_mode = m


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
			if _cycle >= _tick_budget:
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

			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var credits: int = int(ps.get("credits", 0))
			_credit_trajectory.append(credits)

			# Stress: track credit plateau
			if _mode in ["stress", "full"]:
				if credits == _last_credit_value:
					_consecutive_credit_unchanged += 1
				else:
					_consecutive_credit_unchanged = 0
				_last_credit_value = credits

		Phase.REPORT:
			_generate_report()
			_write_json_report()
			_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


func _init_galaxy() -> void:
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

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	if not loc.is_empty():
		_visited[loc] = true

	_start_credits = int(ps.get("credits", 0))
	_credit_trajectory.append(_start_credits)
	_last_credit_value = _start_credits

	# Init combat HP for modes that need it
	if _mode in ["combat", "full"]:
		if _bridge.has_method("InitFleetCombatHpV0"):
			_bridge.call("InitFleetCombatHpV0")
			_combat_hp_init_done = true
			print(PREFIX + "COMBAT_HP_INIT|done")

	print(PREFIX + "INIT|nodes=%d lanes=%d start=%s credits=%d mode=%s" % [
		_all_nodes.size(), _all_lanes.size(), loc, _start_credits, _mode])


# ── Decision loop ──

func _bot_decide_and_act() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	var credits: int = int(ps.get("credits", 0))
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var cargo_count: int = int(ps.get("cargo_count", 0))

	if _combat_cooldown_remaining > 0:
		_combat_cooldown_remaining -= 1

	if loc.is_empty():
		_record_action(_cycle, "IDLE", loc, "", 0, "no location")
		_total_idles += 1
		_idle_cycles_total += 1
		return

	# Phase 0: Committed exploration
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

	# Phase 1: Combat (if mode supports it)
	if _mode in ["combat", "full"] and _combat_cooldown_remaining <= 0:
		if _try_combat(loc):
			return

	# Phase 2: If carrying cargo → sell
	if _mode in ["trade", "stress", "full"]:
		if cargo_count > 0 and cargo.size() > 0:
			var sell_result := _try_sell(loc, cargo, credits)
			if sell_result:
				return

	# Phase 3: No cargo → buy
	if _mode in ["trade", "stress", "full"]:
		if cargo_count == 0:
			var buy_result := _try_buy(loc, credits)
			if buy_result:
				return

	# Phase 4: Combat-only mode — move toward hostiles
	if _mode == "combat" and cargo_count == 0:
		var target := _find_node_with_hostiles(loc)
		if not target.is_empty() and target != loc:
			var hop := _get_next_hop(loc, target)
			if not hop.is_empty():
				_do_travel(loc, hop, "hunting hostiles at %s" % target)
				return

	# Phase 5: Explore unvisited
	if has_unvisited:
		var target := _find_nearest_unvisited(loc)
		if not target.is_empty():
			var hop := _get_next_hop(loc, target)
			if not hop.is_empty():
				_do_travel(loc, hop, "fallback explore toward %s" % target)
				return

	# Phase 6: Nothing to do
	_consecutive_idles += 1
	_total_idles += 1
	_idle_cycles_total += 1
	_record_action(_cycle, "IDLE", loc, "", 0, "nothing to do")

	if _consecutive_idles >= 10:
		_add_flag("STUCK_NO_ACTIONS", "CRITICAL",
			"Bot idle for %d cycles at %s" % [_consecutive_idles, loc],
			"Economy collapsed or bot stuck.")


# ── Combat ──

func _try_combat(loc: String) -> bool:
	if not _bridge.has_method("GetFleetTransitFactsV0"):
		return false

	var fleets: Array = _bridge.call("GetFleetTransitFactsV0", loc)
	var hostile_id := ""
	for f in fleets:
		if bool(f.get("is_hostile", false)):
			hostile_id = str(f.get("fleet_id", ""))
			break

	if hostile_id.is_empty():
		return false

	# Ensure combat HP initialized
	if not _combat_hp_init_done:
		_bridge.call("InitFleetCombatHpV0")
		_combat_hp_init_done = true

	# Get pre-combat HP
	var pre_hp: Dictionary = {}
	if _bridge.has_method("GetFleetCombatHpV0"):
		pre_hp = _bridge.call("GetFleetCombatHpV0", hostile_id)

	# Engage
	var result: Dictionary = _bridge.call("ResolveCombatV0", "fleet_trader_1", hostile_id)
	_total_combats += 1
	_combat_cooldown_remaining = COMBAT_COOLDOWN

	var outcome: String = str(result.get("outcome", "unknown"))
	var rounds: int = int(result.get("rounds", 0))
	var attacker_hull: int = int(result.get("attacker_hull", 0))
	var defender_hull: int = int(result.get("defender_hull", 0))
	var salvage: int = int(result.get("salvage", 0))

	_record_action(_cycle, "COMBAT", loc, hostile_id, rounds,
		"vs %s: %s in %d rounds, hull=%d def_hull=%d salvage=%d" % [
			hostile_id, outcome, rounds, attacker_hull, defender_hull, salvage])

	print(PREFIX + "COMBAT|cycle=%d target=%s outcome=%s rounds=%d hull=%d salvage=%d" % [
		_cycle, hostile_id, outcome, rounds, attacker_hull, salvage])

	# Validate combat had effect
	if pre_hp.size() > 0:
		var pre_hull: int = int(pre_hp.get("hull", 0))
		if defender_hull >= pre_hull and pre_hull > 0 and outcome != "defender_fled":
			_add_flag("DAMAGE_NOT_APPLIED", "CRITICAL",
				"Combat vs %s: hull %d→%d unchanged" % [hostile_id, pre_hull, defender_hull],
				"ResolveCombatV0 ran but HP didn't change. Check CombatSystem.cs")

	if outcome == "attacker_won":
		_total_kills += 1

	if attacker_hull <= 0:
		_add_flag("PLAYER_DIED", "WARNING",
			"Player hull reached 0 in combat vs %s" % hostile_id,
			"Not necessarily a bug, but player fleet was destroyed.")

	_consecutive_idles = 0
	return true


func _find_node_with_hostiles(from: String) -> String:
	# BFS to find nearest node with hostile fleets
	if not _bridge.has_method("GetFleetTransitFactsV0"):
		return ""

	var queue: Array = [from]
	var seen: Dictionary = {from: true}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if current != from:
			var fleets: Array = _bridge.call("GetFleetTransitFactsV0", current)
			for f in fleets:
				if bool(f.get("is_hostile", false)):
					return current
		if not _adj.has(current):
			continue
		var neighbors: Array = _adj[current]
		for n in neighbors:
			if not seen.has(n):
				seen[n] = true
				queue.append(n)
	return ""


# ── Trade (same as v0) ──

func _try_sell(loc: String, cargo: Array, credits: int) -> bool:
	var best_good := ""
	var best_sell_node := ""
	var best_sell_price := 0

	for item in cargo:
		var good_id: String = str(item.get("good_id", ""))
		var qty: int = int(item.get("qty", 0))
		if good_id.is_empty() or qty <= 0:
			continue
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
		# Stress: track buy price
		if _mode in ["stress", "full"]:
			var unit_price := (credits_before - credits_after) / max(1, qty)
			_track_price(good_id, unit_price)
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
		# Stress: track sell price
		if _mode in ["stress", "full"]:
			var unit_price := (credits_after - credits_before) / max(1, qty)
			_track_price(good_id, unit_price)
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


# ── Stress tracking ──

func _track_price(good_id: String, price: int) -> void:
	if not _price_history.has(good_id):
		_price_history[good_id] = []
	_price_history[good_id].append(price)


# ── Graph helpers ──

func _get_next_hop(from: String, to: String) -> String:
	if from == to:
		return ""
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

	# Trade flags (trade, stress, full)
	if _mode in ["trade", "stress", "full"]:
		if _total_buys > 0 and _total_sells > 0 and net_profit < 0:
			_add_flag("NET_LOSS", "WARNING",
				"Bot lost money: %d→%d (%d buys, %d sells)" % [_start_credits, end_credits, _total_buys, _total_sells],
				"Trading not profitable through bridge. Check price model.")

		if _total_buys == 0 and _tick_budget > 50:
			_add_flag("NEVER_BOUGHT", "CRITICAL",
				"Bot never bought anything in %d cycles" % _tick_budget,
				"DispatchPlayerTradeV0 buy path may be broken, or no profitable goods found.")

		if _total_sells == 0 and _total_buys > 0:
			_add_flag("NEVER_SOLD", "CRITICAL",
				"Bot bought but never sold in %d cycles" % _tick_budget,
				"Sell path broken or bot couldn't find sell destination.")

	# Combat flags (combat, full)
	if _mode in ["combat", "full"]:
		if _total_combats == 0 and _tick_budget > 50:
			_add_flag("NEVER_FOUGHT", "CRITICAL",
				"No combat in %d cycles (mode=%s)" % [_tick_budget, _mode],
				"No hostile fleets found, or GetFleetTransitFactsV0 broken.")

	# Stress flags (stress, full)
	if _mode in ["stress", "full"]:
		# Price collapse: any good lost >50% value
		for good_id in _price_history.keys():
			var prices: Array = _price_history[good_id]
			if prices.size() < 10:
				continue
			var first_avg := _avg_slice(prices, 0, mini(5, prices.size()))
			var last_avg := _avg_slice(prices, maxi(0, prices.size() - 5), prices.size())
			if first_avg > 0 and last_avg < first_avg * 0.5:
				_add_flag("PRICE_COLLAPSE", "CRITICAL",
					"%s price collapsed: %.0f→%.0f" % [good_id, first_avg, last_avg],
					"Good lost >50%% value over the run. Economy may be broken.")

		# Economy stall: idle >20% of cycles
		var idle_pct: float = float(_idle_cycles_total) / max(1, _tick_budget)
		if idle_pct > 0.2 and _tick_budget >= 100:
			_add_flag("ECONOMY_STALL", "WARNING",
				"Bot idle for %d/%d cycles (%d%%)" % [_idle_cycles_total, _tick_budget, int(idle_pct * 100)],
				"Bot couldn't find profitable actions for >20%% of the run.")

		# Credit plateau: unchanged for >100 consecutive cycles
		if _consecutive_credit_unchanged > 100:
			_add_flag("CREDIT_PLATEAU", "WARNING",
				"Credits unchanged for %d consecutive cycles" % _consecutive_credit_unchanged,
				"Economy stagnant — no profitable trades happening.")

	# Coverage
	var visited_count: int = _visited.size()
	var total_nodes: int = _all_nodes.size()
	var visit_pct: float = float(visited_count) / max(1, total_nodes)
	if visit_pct < 0.5 and _tick_budget >= 100:
		_add_flag("LOW_EXPLORATION", "WARNING",
			"Visited %d/%d nodes (%d%%)" % [visited_count, total_nodes, int(visit_pct * 100)],
			"Bot didn't reach half the galaxy.")

	# Untraded goods (trade/stress/full)
	if _mode in ["trade", "stress", "full"]:
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
	print(PREFIX + "SUMMARY|mode=%s ticks=%d nodes=%d/%d net=%d buys=%d sells=%d travels=%d idles=%d combats=%d kills=%d" % [
		_mode, _tick_budget, visited_count, total_nodes, net_profit,
		_total_buys, _total_sells, _total_travels, _total_idles, _total_combats, _total_kills])
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

	var critical_count := 0
	for f in _flags:
		if str(f.get("severity", "")) == "CRITICAL":
			critical_count += 1

	if critical_count == 0:
		print(PREFIX + "PASS")
	else:
		print(PREFIX + "FAIL|%d critical flags" % critical_count)


func _avg_slice(arr: Array, from_idx: int, to_idx: int) -> float:
	var total := 0.0
	var count := 0
	for i in range(from_idx, to_idx):
		total += float(arr[i])
		count += 1
	return total / max(1, count)


func _write_json_report() -> void:
	var dir := DirAccess.open("res://")
	if dir:
		dir.make_dir_recursive(_output_dir.replace("res://", ""))

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var end_credits: int = int(ps.get("credits", 0))

	var report := {
		"mode": _mode,
		"cycles": _tick_budget,
		"start_credits": _start_credits,
		"end_credits": end_credits,
		"net_profit": end_credits - _start_credits,
		"buys": _total_buys,
		"sells": _total_sells,
		"travels": _total_travels,
		"idles": _total_idles,
		"combats": _total_combats,
		"kills": _total_kills,
		"nodes_visited": _visited.size(),
		"nodes_total": _all_nodes.size(),
		"flags": _flags,
		"goods_bought": _goods_bought.keys(),
		"goods_sold": _goods_sold.keys(),
	}

	var json_str := JSON.stringify(report, "  ")
	var path := _output_dir + "/report.json"
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string(json_str)
		file.close()
		print(PREFIX + "REPORT_WRITTEN|%s" % path)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
