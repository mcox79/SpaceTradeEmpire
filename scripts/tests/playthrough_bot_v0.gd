extends SceneTree

## Playthrough Bot v0 -- scripted critical-path playthrough with mode support
## Modes:
##   full    (default): Scripted critical path TUTORIAL -> VICTORY
##   trade:  TUTORIAL -> autonomous trade loop for _tick_budget cycles -> REPORT
##   combat: TUTORIAL -> TRADE -> autonomous combat+trade loop -> REPORT
##   stress: Full critical path but extended ENDGAME (1500 cycles) + stress tracking
## Usage:
##   godot --headless --path . -s res://scripts/tests/playthrough_bot_v0.gd
##   godot --headless --path . -s res://scripts/tests/playthrough_bot_v0.gd -- --mode trade --cycles 400
## Output prefix: PLAY| for structured log parsing.

const PREFIX := "PLAY|"
const MAX_POLLS := 600
const MAX_BUY_QTY := 10
const TICK_ADVANCE := 5
const TRADE_TARGET_CREDITS := 2000
const TRADE_MAX_ROUNDS := 60
const TRADE_MIN_TRADES := 20
const TRADE_MIN_GOODS := 3
const EXPLORE_TARGET_NODES := 5
const EXPLORE_MAX_ROUNDS := 40
const SCAN_MAX_ROUNDS := 30
const SCAN_TICK_SKIP := 20
const HAVEN_POLL_MAX := 60
const UPGRADE_MAX_TIER := 5  # Loop iterations (tier starts at 1, needs to reach 4=Expanded)
const UPGRADE_TICK_SKIP := 250
const RESEARCH_TARGET := 5
const RESEARCH_TICK_SKIP := 150
const ENDGAME_TRADE_CYCLES := 50
const ENDGAME_TICK_SKIP := 50
const VICTORY_POLL_MAX := 50
const COMBAT_MAX_FIGHTS := 3
const EXPLORE_EVERY_N := 4
const ACT_INTERVAL := 3
const COMBAT_COOLDOWN := 5
const MAX_TOTAL_FRAMES := 18000  # ~300s at 60fps -- hard safety timeout
const STALL_FRAME_LIMIT := 600   # If phase unchanged for 600 frames, force-advance or quit

enum Phase { WAIT_BRIDGE, WAIT_READY, TUTORIAL, TRADE, INTEL_CHECK, EXPLORE, COMBAT, SCAN, EXTRACT, HAVEN, MAINTENANCE_CHECK, UPGRADE, RESEARCH, MEGAPROJECT, EQUIP, AUTOMATION, DIPLOMACY, WARFRONT_CHECK, ENDGAME, VICTORY, BOT_LOOP, REPORT, DONE }

# Mode config
var _mode := "full"
var _tick_budget := 0  # 0 = use default per mode

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _adj: Dictionary = {}
var _all_nodes: Array = []
var _trade_count := 0
var _goods_traded: Dictionary = {}
var _trade_round := 0
var _visited: Dictionary = {}
var _explore_round := 0
var _start_credits := 0
var _flags: Array = []
var _haven_node_id := ""
var _upgrade_tier := 0
var _research_count := 0
var _endgame_path := ""
var _phases_completed: Array = []
var _busy := false
var _reinit_done := false
var _total_frames := 0
var _stall_phase: int = -1           # Phase value at last change
var _stall_frame_start := 0          # Frame when current phase began
var _primary_faction := "weavers"  # Concentrate trades here for rep > 25 (megaproject gate)
var _faction_node_cache: Dictionary = {}  # faction_id -> [node_ids]

# Bot loop state (absorbed from exploration_bot_v1)
var _bot_cycle := 0
var _bot_sub_tick := 0
var _exploration_target := ""
var _trades_since_explore := 0
var _consecutive_idles := 0
var _max_consecutive_idles := 0
var _combat_cooldown_remaining := 0
var _combat_hp_init_done := false

# Tracking -- trade (absorbed from exploration_bot_v1)
var _total_buys := 0
var _total_sells := 0
var _total_travels := 0
var _total_idles := 0
var _total_spent := 0
var _total_earned := 0
var _goods_bought: Dictionary = {}
var _goods_sold: Dictionary = {}
var _good_trade_count: Dictionary = {}  # good_id -> times traded (for rotation)
var _actions: Array = []

# Tracking -- combat
var _total_combats := 0
var _total_kills := 0

# Tracking -- stress (absorbed from exploration_bot_v1)
var _profit_snapshots: Array = []
var _price_history: Dictionary = {}
var _credit_trajectory: Array = []
var _consecutive_credit_unchanged := 0
var _last_credit_value := -1
var _idle_cycles_total := 0


func _initialize() -> void:
	# Parse CLI args
	var args := OS.get_cmdline_user_args()
	for i in range(args.size()):
		if args[i] == "--mode" and i + 1 < args.size():
			var m: String = args[i + 1].to_lower()
			if m in ["full", "trade", "combat", "stress"]:
				_mode = m
		if args[i] == "--cycles" and i + 1 < args.size():
			_tick_budget = int(args[i + 1])

	# Apply default tick budgets per mode if not explicitly set
	if _tick_budget == 0:
		match _mode:
			"trade": _tick_budget = 400
			"combat": _tick_budget = 600
			"stress": _tick_budget = 1500
			"full": _tick_budget = 0  # not cycle-limited

	print(PREFIX + "START|playthrough_bot_v0")
	print(PREFIX + "MODE_SELECT|" + _mode)
	if _tick_budget > 0:
		print(PREFIX + "TICK_BUDGET|%d" % _tick_budget)

func _process(_delta: float) -> bool:
	_total_frames += 1

	# ---- Hard safety timeout: force quit if running too long ----
	if _total_frames > MAX_TOTAL_FRAMES and _phase != Phase.DONE:
		print(PREFIX + "SAFETY_TIMEOUT|frame=%d|phase=%d|busy=%s" % [_total_frames, _phase, str(_busy)])
		_add_flag("SAFETY_TIMEOUT", "CRITICAL",
			"Bot exceeded %d frames (phase=%d)" % [MAX_TOTAL_FRAMES, _phase],
			"Process hung or ran too long")
		_busy = false
		_force_quit(1)
		return true

	# ---- Stall watchdog: detect phase stuck for too long ----
	if _phase != _stall_phase:
		_stall_phase = _phase
		_stall_frame_start = _total_frames
	elif _total_frames - _stall_frame_start > STALL_FRAME_LIMIT and _phase != Phase.DONE:
		var stall_duration: int = _total_frames - _stall_frame_start
		print(PREFIX + "STALL_WATCHDOG|phase=%d|stalled=%d_frames|busy=%s" % [_phase, stall_duration, str(_busy)])
		_add_flag("PHASE_STALL", "CRITICAL",
			"Phase %d stalled for %d frames (busy=%s)" % [_phase, stall_duration, str(_busy)],
			"Bot stuck in phase, force-advancing")
		_busy = false
		# Force-advance: skip to next phase or quit
		match _phase:
			Phase.WAIT_BRIDGE, Phase.WAIT_READY:
				_force_quit(1)
				return true
			Phase.REPORT:
				_force_quit(1)
				return true
			Phase.BOT_LOOP:
				_phase = Phase.REPORT
			Phase.VICTORY:
				_phase = Phase.DONE
				_finish()
			_:
				# Skip to next phase in sequence (enum values are sequential ints)
				_phase = _phase + 1
		_stall_phase = _phase
		_stall_frame_start = _total_frames

	if _busy:
		return false
	match _phase:
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.TUTORIAL: _do_tutorial()
		Phase.TRADE: _do_trade()
		Phase.INTEL_CHECK: _do_intel_check()
		Phase.EXPLORE: _do_explore()
		Phase.COMBAT: _do_combat()
		Phase.SCAN: _do_scan()
		Phase.EXTRACT: _do_extract()
		Phase.HAVEN: _do_haven()
		Phase.MAINTENANCE_CHECK: _do_maintenance_check()
		Phase.UPGRADE: _do_upgrade()
		Phase.RESEARCH: _do_research()
		Phase.MEGAPROJECT: _do_megaproject()
		Phase.EQUIP: _do_equip()
		Phase.AUTOMATION: _do_automation()
		Phase.DIPLOMACY: _do_diplomacy()
		Phase.WARFRONT_CHECK: _do_warfront_check()
		Phase.ENDGAME: _do_endgame()
		Phase.VICTORY: _do_victory()
		Phase.BOT_LOOP: _do_bot_loop()
		Phase.REPORT: _do_report()
		Phase.DONE: return true
	return false

func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge != null:
		print(PREFIX + "BRIDGE_FOUND")
		_phase = Phase.WAIT_READY
		_polls = 0
	else:
		_polls += 1
		if _polls > MAX_POLLS:
			print(PREFIX + "FATAL|bridge_not_found")
			quit(1)

func _do_wait_ready() -> void:
	if _bridge.has_method("GetBridgeReadyV0") and bool(_bridge.call("GetBridgeReadyV0")):
		print(PREFIX + "BRIDGE_READY")
		# Force new game ONCE so worldgen changes take effect.
		if not _reinit_done and _bridge.has_method("ReinitializeForNewGameV0"):
			_reinit_done = true
			_busy = true
			_bridge.call("ReinitializeForNewGameV0")
			print(PREFIX + "NEW_GAME|forced")
			for _w in range(MAX_POLLS):
				await create_timer(0.1).timeout
				if _bridge.has_method("GetBridgeReadyV0") and bool(_bridge.call("GetBridgeReadyV0")):
					break
			_busy = false
		_build_adjacency()
		# Retry if read-lock contention returned empty galaxy (seed 1001 race condition)
		for _retry in range(5):
			if _all_nodes.size() > 0:
				break
			print(PREFIX + "RETRY|_build_adjacency attempt %d (nodes=0, read-lock contention)" % (_retry + 1))
			_busy = true
			await create_timer(0.3).timeout
			_busy = false
			_build_adjacency()
		if _all_nodes.size() == 0:
			print(PREFIX + "FLAG|GALAXY_EMPTY|could not read galaxy after retries")
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		_start_credits = int(ps.get("credits", 0))
		var loc: String = ps.get("current_node_id", "")
		_visited[loc] = true
		_credit_trajectory.append(_start_credits)
		_last_credit_value = _start_credits
		print(PREFIX + "INIT|credits=%d|node=%s|galaxy_nodes=%d" % [_start_credits, loc, _all_nodes.size()])

		# Init combat HP for modes that need it
		if _mode in ["combat", "full", "stress"]:
			if _bridge.has_method("InitFleetCombatHpV0"):
				_bridge.call("InitFleetCombatHpV0")
				_combat_hp_init_done = true
				print(PREFIX + "COMBAT_HP_INIT|done")

		_phase = Phase.TUTORIAL
	else:
		_polls += 1
		if _polls > MAX_POLLS:
			print(PREFIX + "FATAL|bridge_not_ready")
			quit(1)

func _do_tutorial() -> void:
	if _bridge.has_method("SkipTutorialV0"):
		_bridge.call("SkipTutorialV0")
		print(PREFIX + "TUTORIAL|skipped")
	else:
		print(PREFIX + "TUTORIAL|no_skip_method")

	# Mode-specific phase routing after tutorial
	match _mode:
		"trade":
			# Jump directly to autonomous bot loop (trade only)
			_phase = Phase.BOT_LOOP
		"combat":
			# Do scripted trade first to build credits, then bot loop
			_phase = Phase.TRADE
			_trade_round = 0
		"stress", "full":
			# Follow scripted path
			_phase = Phase.TRADE
			_trade_round = 0

func _do_trade() -> void:
	_trade_round += 1
	if _trade_round > TRADE_MAX_ROUNDS:
		_finish_trade("max_rounds")
		return
	# Build faction node cache once (for rep-focused trading)
	if _faction_node_cache.is_empty() and _bridge.has_method("GetNodeFactionMapV0"):
		var nfm: Array = _bridge.call("GetNodeFactionMapV0")
		for entry in nfm:
			if entry is Dictionary:
				var fid: String = entry.get("faction_id", "")
				var nid: String = entry.get("node_id", "")
				if fid and nid:
					if not _faction_node_cache.has(fid):
						_faction_node_cache[fid] = []
					_faction_node_cache[fid].append(nid)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: int = int(ps.get("credits", 0))
	var loc: String = ps.get("current_node_id", "")
	var cargo_count: int = int(ps.get("cargo_count", 0))
	var cargo_cap: int = int(ps.get("cargo_capacity", 50))
	if credits >= TRADE_TARGET_CREDITS and _trade_count >= TRADE_MIN_TRADES and _goods_traded.size() >= TRADE_MIN_GOODS:
		_finish_trade("target_met")
		return

	# Cross-node arbitrage: buy cheap here, travel, sell there
	# Keep upgrade-critical goods -- don't sell these during trade phase
	var keep_goods := ["exotic_matter", "composites", "electronics", "rare_metals", "exotic_crystals", "salvaged_tech"]
	# Step 1: Sell non-critical cargo we're carrying (bought at previous node)
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	for item in cargo:
		if item is Dictionary:
			var good_id: String = item.get("good_id", "")
			var held: int = int(item.get("qty", 0))
			if held > 0 and good_id not in keep_goods:
				_bridge.call("DispatchPlayerTradeV0", loc, good_id, held, false)
				_trade_count += 1
				_goods_traded[good_id] = true
				print(PREFIX + "TRADE|SELL|%s|qty=%d|node=%s" % [good_id, held, loc])

	# Step 2: Arbitrage-aware buying -- scan neighbor sell prices to find profitable goods
	ps = _bridge.call("GetPlayerStateV0")
	credits = int(ps.get("credits", 0))
	cargo_count = int(ps.get("cargo_count", 0))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", loc)
	var neighbors: Array = _get_neighbors(loc)
	var best_sell_dest := ""
	if cargo_count < cargo_cap and credits > 0 and neighbors.size() > 0:
		# Build sell price map: for each good, find best sell price across neighbors
		var best_sell_prices: Dictionary = {}  # good_id -> {price, node}
		for neighbor_id in neighbors:
			var n_market: Array = _bridge.call("GetPlayerMarketViewV0", neighbor_id)
			for n_item in n_market:
				if n_item is Dictionary:
					var gid: String = n_item.get("good_id", "")
					var sell_p: int = int(n_item.get("sell_price", 0))
					if gid and sell_p > 0:
						if not best_sell_prices.has(gid) or sell_p > best_sell_prices[gid]["price"]:
							best_sell_prices[gid] = {"price": sell_p, "node": neighbor_id}
		# Score each buyable good by profit margin (best_sell - buy_price)
		var best_buys: Array = []
		for item in market:
			if item is Dictionary:
				var buy_price: int = int(item.get("buy_price", 0))
				var qty_avail: int = int(item.get("quantity", 0))
				var gid: String = item.get("good_id", "")
				if buy_price > 0 and qty_avail > 0 and gid:
					var sell_info: Dictionary = best_sell_prices.get(gid, {})
					var best_sell: int = sell_info.get("price", 0)
					var margin: int = best_sell - buy_price
					best_buys.append({"good_id": gid, "buy_price": buy_price, "qty": qty_avail, "margin": margin, "sell_node": sell_info.get("node", "")})
		best_buys.sort_custom(func(aa, bb): return aa["margin"] > bb["margin"])
		var bought := 0
		for entry in best_buys:
			if bought >= 3:
				break
			if entry["margin"] <= 0:
				break  # no profitable goods left
			ps = _bridge.call("GetPlayerStateV0")
			credits = int(ps.get("credits", 0))
			cargo_count = int(ps.get("cargo_count", 0))
			var space: int = cargo_cap - cargo_count
			if space <= 0 or credits < entry["buy_price"]:
				break
			var buy_qty: int = mini(mini(MAX_BUY_QTY, entry["qty"]), mini(space, credits / maxi(entry["buy_price"], 1)))
			if buy_qty <= 0:
				continue
			var good_id: String = entry["good_id"]
			_bridge.call("DispatchPlayerTradeV0", loc, good_id, buy_qty, true)
			_trade_count += 1
			_goods_traded[good_id] = true
			if bought == 0 and entry["sell_node"]:
				best_sell_dest = entry["sell_node"]
			print(PREFIX + "TRADE|BUY|%s|qty=%d|price=%d|margin=%d|node=%s" % [good_id, buy_qty, entry["buy_price"], entry["margin"], loc])
			bought += 1
		# If no profitable goods found, fall back to cheapest-first
		if bought == 0:
			var fallback_buys: Array = []
			for item in market:
				if item is Dictionary:
					var bp: int = int(item.get("buy_price", 0))
					var qa: int = int(item.get("quantity", 0))
					if bp > 0 and qa > 0:
						fallback_buys.append({"good_id": item.get("good_id", ""), "buy_price": bp, "qty": qa})
			fallback_buys.sort_custom(func(aa, bb): return aa["buy_price"] < bb["buy_price"])
			for entry in fallback_buys:
				if bought >= 3:
					break
				ps = _bridge.call("GetPlayerStateV0")
				credits = int(ps.get("credits", 0))
				cargo_count = int(ps.get("cargo_count", 0))
				var space: int = cargo_cap - cargo_count
				if space <= 0 or credits < entry["buy_price"]:
					break
				var buy_qty: int = mini(mini(MAX_BUY_QTY, entry["qty"]), mini(space, credits / maxi(entry["buy_price"], 1)))
				if buy_qty <= 0:
					continue
				_bridge.call("DispatchPlayerTradeV0", loc, entry["good_id"], buy_qty, true)
				_trade_count += 1
				_goods_traded[entry["good_id"]] = true
				print(PREFIX + "TRADE|BUY|%s|qty=%d|price=%d|fallback|node=%s" % [entry["good_id"], buy_qty, entry["buy_price"], loc])
				bought += 1

	# Step 3: Travel to best sell destination, preferring primary faction nodes for rep
	if neighbors.size() > 0:
		var target: String = ""
		if best_sell_dest and best_sell_dest in neighbors:
			target = best_sell_dest
		else:
			# Prefer neighbors belonging to primary faction (builds concentrated rep)
			var faction_neighbors: Array = []
			var primary_nodes: Array = _faction_node_cache.get(_primary_faction, [])
			for n in neighbors:
				if n in primary_nodes:
					faction_neighbors.append(n)
			if faction_neighbors.size() > 0:
				target = faction_neighbors[_trade_round % faction_neighbors.size()]
			else:
				target = neighbors[_trade_round % neighbors.size()]
		_bridge.call("DispatchPlayerArriveV0", target)
		for _i in range(30):
			await create_timer(0.05).timeout
			var new_ps: Dictionary = _bridge.call("GetPlayerStateV0")
			if new_ps.get("current_node_id", "") == target:
				print(PREFIX + "TRADE|MOVE|%s>%s" % [loc, target])
				_visited[target] = true
				break
	if _bridge.has_method("DebugAdvanceTicksV0"):
		_bridge.call("DebugAdvanceTicksV0", TICK_ADVANCE)

func _finish_trade(reason: String) -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: int = int(ps.get("credits", 0))
	var profit: int = credits - _start_credits
	print(PREFIX + "TRADE|DONE|reason=%s|trades=%d|goods=%d|credits=%d|profit=%d" % [
		reason, _trade_count, _goods_traded.size(), credits, profit
	])
	if _trade_count < TRADE_MIN_TRADES:
		_add_flag("TRADE_FEW_TRADES", "WARNING", "Only %d trades" % _trade_count, "Below TRADE_MIN_TRADES threshold")
	if _goods_traded.size() < TRADE_MIN_GOODS:
		_add_flag("TRADE_FEW_GOODS", "WARNING", "Only %d goods" % _goods_traded.size(), "Below TRADE_MIN_GOODS threshold")
	if credits < TRADE_TARGET_CREDITS:
		_add_flag("TRADE_LOW_CREDITS", "WARNING", "Only %d credits" % credits, "Below TRADE_TARGET_CREDITS threshold")
	# Buy upgrade materials at end of trade phase (after accumulating credits)
	var loc2: String = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
	var upgrade_goods := ["exotic_matter", "composites", "electronics", "rare_metals", "exotic_crystals", "salvaged_tech"]
	var mkt: Array = _bridge.call("GetPlayerMarketViewV0", loc2)
	for item in mkt:
		if item is Dictionary:
			var gid: String = item.get("good_id", "")
			if gid in upgrade_goods and int(item.get("quantity", 0)) > 0:
				var buy_qty: int = mini(25, int(item.get("quantity", 0)))
				_bridge.call("DispatchPlayerTradeV0", loc2, gid, buy_qty, true)
				print(PREFIX + "TRADE|BUY_UPGRADE|%s|qty=%d|node=%s" % [gid, buy_qty, loc2])

	# Mode-specific routing after trade
	match _mode:
		"combat":
			# After building credits, switch to autonomous bot loop with combat
			_phase = Phase.BOT_LOOP
		_:
			_phase = Phase.INTEL_CHECK

func _do_explore() -> void:
	_explore_round += 1
	if _explore_round > EXPLORE_MAX_ROUNDS:
		_finish_explore("max_rounds")
		return
	if _visited.size() >= EXPLORE_TARGET_NODES:
		_finish_explore("target_met")
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	var neighbors: Array = _get_neighbors(loc)
	var target := ""
	for n in neighbors:
		if not _visited.has(n):
			target = n
			break
	if target.is_empty() and neighbors.size() > 0:
		var found := false
		for n in neighbors:
			var nn: Array = _get_neighbors(n)
			for nn2 in nn:
				if not _visited.has(nn2):
					target = n
					found = true
					break
			if found:
				break
		if target.is_empty():
			target = neighbors[0]
	if target.is_empty():
		_finish_explore("no_neighbors")
		return
	_bridge.call("DispatchPlayerArriveV0", target)
	var arrived := false
	for _i in range(50):
		await create_timer(0.05).timeout
		var new_ps: Dictionary = _bridge.call("GetPlayerStateV0")
		if new_ps.get("current_node_id", "") == target:
			arrived = true
			break
	if arrived:
		_visited[target] = true
		print(PREFIX + "EXPLORE|ARRIVE|%s|visited=%d/%d" % [target, _visited.size(), EXPLORE_TARGET_NODES])
	else:
		print(PREFIX + "EXPLORE|TIMEOUT|%s" % target)
	if _bridge.has_method("DebugAdvanceTicksV0"):
		_bridge.call("DebugAdvanceTicksV0", TICK_ADVANCE)

func _finish_explore(reason: String) -> void:
	print(PREFIX + "EXPLORE|DONE|reason=%s|visited=%d" % [reason, _visited.size()])
	if _visited.size() < EXPLORE_TARGET_NODES:
		_add_flag("EXPLORE_FEW_NODES", "WARNING", "Only %d nodes visited" % _visited.size(), "Below EXPLORE_TARGET_NODES threshold")
	_phase = Phase.COMBAT

func _do_scan() -> void:
	_busy = true
	var exotic_collected := 0
	# Visit each known node and scan for anomalies/discoveries
	var nodes_to_scan: Array = _visited.keys()
	# Also explore more nodes to find anomalies
	for nid in _adj:
		if nid not in nodes_to_scan:
			nodes_to_scan.append(nid)
		if nodes_to_scan.size() >= 12:
			break

	for scan_node in nodes_to_scan:
		# Travel to scan node
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var loc: String = ps.get("current_node_id", "")
		if loc != scan_node:
			_bridge.call("DispatchPlayerArriveV0", scan_node)
			for _w in range(30):
				await create_timer(0.05).timeout
				if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == scan_node:
					break
			loc = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
			if loc != scan_node:
				continue
			_visited[scan_node] = true

		# Scan discoveries at this node
		var discoveries: Array = _bridge.call("GetDiscoverySnapshotV0", scan_node) if _bridge.has_method("GetDiscoverySnapshotV0") else []
		for disc in discoveries:
			if disc is Dictionary:
				var did: String = disc.get("site_id", "")
				if did:
					var phase_str: String = disc.get("phase", "SEEN")
					# Phase 1: Seen -> Scanned
					if phase_str == "SEEN" and _bridge.has_method("DispatchScanDiscoveryV0"):
						_bridge.call("DispatchScanDiscoveryV0", did)
						await create_timer(0.05).timeout
						if _bridge.has_method("DebugAdvanceTicksV0"):
							_bridge.call("DebugAdvanceTicksV0", 5)
							await create_timer(0.05).timeout
					# Phase 2: Scanned -> Analyzed
					if _bridge.has_method("DispatchScanDiscoveryV0"):
						_bridge.call("DispatchScanDiscoveryV0", did)
						await create_timer(0.05).timeout
						if _bridge.has_method("DebugAdvanceTicksV0"):
							_bridge.call("DebugAdvanceTicksV0", 10)
							await create_timer(0.05).timeout
					print(PREFIX + "SCAN|DISCOVERY|%s|phase=%s|node=%s" % [did, phase_str, scan_node])

		# Advance time to let discovery outcomes resolve
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", SCAN_TICK_SKIP)
			await create_timer(0.05).timeout

		# Collect any loot at this node
		var loot = _bridge.call("GetNearbyLootV0") if _bridge.has_method("GetNearbyLootV0") else []
		if loot is Array:
			for drop in loot:
				if drop is Dictionary:
					var drop_id: String = str(drop.get("drop_id", ""))
					if drop_id and _bridge.has_method("DispatchCollectLootV0"):
						var loot_result = _bridge.call("DispatchCollectLootV0", drop_id)
						print(PREFIX + "SCAN|LOOT|%s|result=%s" % [drop_id, str(loot_result)])

		# Collect any adaptation fragments at this node
		var frags_at_node: Array = _bridge.call("GetAdaptationFragmentsV0") if _bridge.has_method("GetAdaptationFragmentsV0") else []
		for frag in frags_at_node:
			if frag is Dictionary and not bool(frag.get("collected", false)):
				var frag_node: String = frag.get("node_id", "")
				if frag_node == scan_node:
					var fid: String = frag.get("fragment_id", "")
					if fid and _bridge.has_method("CollectFragmentV0"):
						_bridge.call("CollectFragmentV0", fid)
						print(PREFIX + "SCAN|FRAGMENT|%s|node=%s" % [fid, scan_node])
						await create_timer(0.02).timeout

		# Also try orbital scan for planets
		if _bridge.has_method("OrbitalScanV0"):
			_bridge.call("OrbitalScanV0", scan_node, "standard")
			await create_timer(0.05).timeout

	# Gate 3: VOID_SURVEY -- survey fracture derelicts at void sites
	if _bridge.has_method("GetAvailableVoidSitesV0"):
		var void_sites: Array = _bridge.call("GetAvailableVoidSitesV0")
		for vs in void_sites:
			if vs is Dictionary:
				var site_id: String = vs.get("site_id", "")
				var family: String = vs.get("family", "")
				var site_node: String = vs.get("node_id", "")
				if site_id == "":
					continue
				# Navigate to the void site node if needed
				if site_node:
					var cur_ps: Dictionary = _bridge.call("GetPlayerStateV0")
					var cur_loc: String = cur_ps.get("current_node_id", "")
					if cur_loc != site_node:
						_bridge.call("DispatchPlayerArriveV0", site_node)
						for _wv in range(30):
							await create_timer(0.05).timeout
							if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == site_node:
								break
						_visited[site_node] = true
				# Interact with the void site (survey fracture derelict)
				if _bridge.has_method("InteractVoidSiteV0"):
					_bridge.call("InteractVoidSiteV0", site_id)
					await create_timer(0.05).timeout
				if _bridge.has_method("DebugAdvanceTicksV0"):
					_bridge.call("DebugAdvanceTicksV0", 10)
					await create_timer(0.05).timeout
				print(PREFIX + "SCAN|VOID_SITE|%s|%s" % [site_id, family])

	# Check how much exotic matter we got
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	for item in cargo:
		if item is Dictionary and item.get("good_id", "") == "exotic_matter":
			exotic_collected = int(item.get("qty", 0))

	print(PREFIX + "SCAN|DONE|nodes_scanned=%d|exotic_matter=%d" % [nodes_to_scan.size(), exotic_collected])
	_busy = false
	_phase = Phase.EXTRACT

func _do_extract() -> void:
	_busy = true
	# Find a node with an analyzed RUIN discovery -- build extraction station there
	var built_extraction := false
	var extraction_node := ""

	for nid in _visited:
		var discoveries: Array = _bridge.call("GetDiscoverySnapshotV0", nid) if _bridge.has_method("GetDiscoverySnapshotV0") else []
		for disc in discoveries:
			if disc is Dictionary and disc.get("phase", "") == "ANALYZED":
				var site_id: String = disc.get("site_id", "")
				if site_id.find("RESOURCE_POOL_MARKER") >= 0 or site_id.find("AnomalyFamily") >= 0:
					extraction_node = nid
					break
		if extraction_node:
			break

	if extraction_node:
		# Travel to the node
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		if ps.get("current_node_id", "") != extraction_node:
			_bridge.call("DispatchPlayerArriveV0", extraction_node)
			for _w in range(30):
				await create_timer(0.05).timeout
				if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == extraction_node:
					break

		# Build extraction station
		if _bridge.has_method("StartConstructionV0"):
			var result = _bridge.call("StartConstructionV0", "constr_extraction_v0", extraction_node)
			var success: bool = false
			if result is Dictionary:
				success = bool(result.get("success", false))
			print(PREFIX + "EXTRACT|BUILD|node=%s|success=%s|result=%s" % [extraction_node, str(success), str(result)])

			if success:
				# Advance ticks to complete construction (4 steps x 30 ticks = 120)
				if _bridge.has_method("DebugAdvanceTicksV0"):
					_bridge.call("DebugAdvanceTicksV0", 150)
					await create_timer(0.1).timeout
				built_extraction = true

				# Set up trade charter from extraction node to haven
				var haven_status: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
				var haven_node: String = haven_status.get("node_id", "")
				if haven_node and _bridge.has_method("CreateTradeCharterProgram"):
					# Need market IDs -- get from node
					var ext_market := "mkt_" + extraction_node
					var haven_market := "mkt_" + haven_node
					var charter_id = _bridge.call("CreateTradeCharterProgram", ext_market, haven_market, "exotic_matter", "exotic_matter", 50)
					print(PREFIX + "EXTRACT|CHARTER|from=%s|to=%s|id=%s" % [extraction_node, haven_node, str(charter_id)])

		# Check extraction sites
		if _bridge.has_method("GetExtractionSitesV0"):
			var sites = _bridge.call("GetExtractionSitesV0")
			print(PREFIX + "EXTRACT|SITES|count=%d" % (sites.size() if sites is Array else 0))
	else:
		print(PREFIX + "EXTRACT|NO_ANALYZED_RUIN_FOUND")

	print(PREFIX + "EXTRACT|DONE|built=%s|node=%s" % [str(built_extraction), extraction_node])
	_busy = false
	_phase = Phase.HAVEN

func _do_haven() -> void:
	_busy = true
	# Discover haven if needed
	var haven: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
	if not bool(haven.get("discovered", false)):
		if _bridge.has_method("ForceDiscoverHavenV0"):
			_bridge.call("ForceDiscoverHavenV0")
			print(PREFIX + "HAVEN|FORCE_DISCOVER")
			await create_timer(0.1).timeout
			haven = _bridge.call("GetHavenStatusV0")
	var haven_node: String = haven.get("node_id", "")
	_haven_node_id = haven_node  # Cache for endgame fragment deposits
	if haven_node:
		_bridge.call("DispatchPlayerArriveV0", haven_node)
		for _i in range(50):
			await create_timer(0.05).timeout
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			if ps.get("current_node_id", "") == haven_node:
				break
		print(PREFIX + "HAVEN|ARRIVED|node=%s" % haven_node)
	else:
		print(PREFIX + "HAVEN|NO_NODE")
		_add_flag("HAVEN_NOT_FOUND", "WARNING", "Haven node not found", "GetHavenStatusV0 returned empty node_id")
	_busy = false
	_phase = Phase.MAINTENANCE_CHECK

func _do_upgrade() -> void:
	_busy = true
	var prev_tier := -1
	for tier_attempt in range(UPGRADE_MAX_TIER):
		var haven: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
		var current_tier: int = int(haven.get("tier", 0))
		if current_tier >= 4:  # Game max tier is 4 (Expanded), not UPGRADE_MAX_TIER
			print(PREFIX + "UPGRADE|DONE|tier=%d (max reached)" % current_tier)
			break
		if current_tier == prev_tier and tier_attempt > 0:
			print(PREFIX + "UPGRADE|STALL|tier=%d (no progress after attempt %d)" % [current_tier, tier_attempt])
			break
		prev_tier = current_tier
		# Upgrade checks player CARGO, not haven market.
		# Buy missing upgrade goods at haven market (if stocked) or keep what we have.
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var loc: String = ps.get("current_node_id", "")
		var upgrade_goods := ["exotic_matter", "composites", "electronics", "rare_metals", "exotic_crystals", "salvaged_tech"]
		# Buy what we can from haven market to top up cargo
		for good_id in upgrade_goods:
			if int(ps.get("credits", 0)) > 50:
				_bridge.call("DispatchPlayerTradeV0", loc, good_id, 50, true)
				await create_timer(0.02).timeout
		# Check what we have
		var cargo: Array = _bridge.call("GetPlayerCargoV0")
		var cargo_map: Dictionary = {}
		for item in cargo:
			if item is Dictionary:
				cargo_map[item.get("good_id", "")] = int(item.get("qty", 0))
		# Deposit any collected fragments at haven (needed for tier 3+)
		var frags: Array = _bridge.call("GetAdaptationFragmentsV0") if _bridge.has_method("GetAdaptationFragmentsV0") else []
		for frag in frags:
			if frag is Dictionary and bool(frag.get("collected", false)) and not bool(frag.get("deposited", false)):
				var fid: String = frag.get("fragment_id", "")
				if fid and _bridge.has_method("DepositFragmentV0"):
					_bridge.call("DepositFragmentV0", fid)
					print(PREFIX + "UPGRADE|DEPOSIT_FRAG|%s" % fid)
					await create_timer(0.02).timeout
		print(PREFIX + "UPGRADE|CARGO|exotic=%d|comp=%d|elec=%d|rare=%d|xcryst=%d" % [
			cargo_map.get("exotic_matter", 0), cargo_map.get("composites", 0),
			cargo_map.get("electronics", 0), cargo_map.get("rare_metals", 0),
			cargo_map.get("exotic_crystals", 0)
		])
		# If missing upgrade goods, travel to nodes that have them
		# Tier 2→3: 50 exotic_matter, 20 rare_metals, 1 nav fragment
		# Tier 3→4: 100 exotic_matter, 30 electronics, 30 rare_metals, 20 exotic_crystals, 1 structural fragment
		var needed: Array = []
		if cargo_map.get("exotic_matter", 0) < 100: needed.append("exotic_matter")
		if cargo_map.get("composites", 0) < 20: needed.append("composites")
		if cargo_map.get("electronics", 0) < 30: needed.append("electronics")
		if cargo_map.get("rare_metals", 0) < 30: needed.append("rare_metals")
		if cargo_map.get("exotic_crystals", 0) < 20: needed.append("exotic_crystals")
		if needed.size() > 0:
			# Search ALL known nodes (not just adjacent) — rare_metals may be far away
			var search_nodes: Array = _all_nodes.duplicate() if _all_nodes.size() > 0 else []
			for nid in _adj:
				if nid not in search_nodes:
					search_nodes.append(nid)
			for nid in search_nodes:
				if needed.size() == 0: break
				var n_market: Array = _bridge.call("GetPlayerMarketViewV0", nid) if _bridge.has_method("GetPlayerMarketViewV0") else []
				var found_needed := false
				for item in n_market:
					if item is Dictionary:
						var gid: String = item.get("good_id", "")
						if gid in needed and int(item.get("quantity", 0)) > 0:
							found_needed = true
							break
				if found_needed:
					# Travel there and buy
					_bridge.call("DispatchPlayerArriveV0", nid)
					for _w in range(30):
						await create_timer(0.05).timeout
						if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == nid: break
					for gid in needed.duplicate():
						_bridge.call("DispatchPlayerTradeV0", nid, gid, 50, true)
						await create_timer(0.02).timeout
					# Return to haven
					var haven_st: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
					var hn: String = haven_st.get("node_id", "")
					if hn:
						_bridge.call("DispatchPlayerArriveV0", hn)
						for _w2 in range(30):
							await create_timer(0.05).timeout
							if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == hn: break
					# Recheck cargo
					cargo = _bridge.call("GetPlayerCargoV0")
					cargo_map.clear()
					for item in cargo:
						if item is Dictionary:
							cargo_map[item.get("good_id", "")] = int(item.get("qty", 0))
					needed.clear()
					if cargo_map.get("exotic_matter", 0) < 100: needed.append("exotic_matter")
					if cargo_map.get("composites", 0) < 20: needed.append("composites")
					if cargo_map.get("electronics", 0) < 30: needed.append("electronics")
					if cargo_map.get("rare_metals", 0) < 30: needed.append("rare_metals")
					if cargo_map.get("exotic_crystals", 0) < 20: needed.append("exotic_crystals")
					print(PREFIX + "UPGRADE|RESUPPLY|exotic=%d|comp=%d|elec=%d|rare=%d|xcryst=%d" % [
						cargo_map.get("exotic_matter", 0), cargo_map.get("composites", 0),
						cargo_map.get("electronics", 0), cargo_map.get("rare_metals", 0),
						cargo_map.get("exotic_crystals", 0)
					])
		if _bridge.has_method("UpgradeHavenV0"):
			_bridge.call("UpgradeHavenV0")
			await create_timer(0.05).timeout
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", UPGRADE_TICK_SKIP)
			await create_timer(0.1).timeout
		haven = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
		print(PREFIX + "UPGRADE|tier=%d" % int(haven.get("tier", 0)))
	_busy = false
	_phase = Phase.RESEARCH

func _do_research() -> void:
	_busy = true
	var researched := 0
	# Priority techs: engine_efficiency is prereq for sensor_suite which unlocks trade intel range
	var priority_techs := ["engine_efficiency", "sensor_suite", "trade_network"]
	for _attempt in range(RESEARCH_TARGET):
		var tree: Array = _bridge.call("GetTechTreeV0") if _bridge.has_method("GetTechTreeV0") else []
		var next_tech := ""
		# First try priority techs (enables scanner range validation)
		for ptid in priority_techs:
			for t in tree:
				if t is Dictionary and t.get("tech_id", "") == ptid and not bool(t.get("unlocked", false)):
					# Check prerequisites are met
					var prereqs: Array = t.get("prerequisites", [])
					var prereqs_met := true
					for pre in prereqs:
						var pre_unlocked := false
						for t2 in tree:
							if t2 is Dictionary and t2.get("tech_id", "") == pre and bool(t2.get("unlocked", false)):
								pre_unlocked = true
								break
						if not pre_unlocked:
							prereqs_met = false
							break
					if prereqs_met:
						next_tech = ptid
						break
			if next_tech:
				break
		# Fallback: first unlockable tech
		if next_tech == "":
			for t in tree:
				if t is Dictionary and not bool(t.get("unlocked", false)):
					next_tech = t.get("tech_id", "")
					break
		if next_tech == "":
			print(PREFIX + "RESEARCH|NO_MORE_TECHS")
			break
		# Wait for any active research to finish before starting new
		if _bridge.has_method("GetResearchStatusV0"):
			var rs: Dictionary = _bridge.call("GetResearchStatusV0")
			if bool(rs.get("researching", false)):
				var remaining: int = int(rs.get("total_ticks", 0)) - int(rs.get("progress_ticks", 0)) + 5
				if remaining > 0 and _bridge.has_method("DebugAdvanceTicksV0"):
					_bridge.call("DebugAdvanceTicksV0", remaining)
					await create_timer(0.1).timeout
					print(PREFIX + "RESEARCH|WAIT_COMPLETE|ticks=%d|was=%s" % [remaining, rs.get("tech_id", "?")])
		# Check block reason before starting
		if _bridge.has_method("GetResearchBlockReasonV0"):
			var block_reason: String = _bridge.call("GetResearchBlockReasonV0", next_tech)
			if block_reason and block_reason != "":
				print(PREFIX + "RESEARCH|BLOCKED|tech=%s|reason=%s" % [next_tech, block_reason])
				continue
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var node_id: String = ps.get("current_node_id", "")
		if _bridge.has_method("StartResearchV0"):
			var res: Dictionary = _bridge.call("StartResearchV0", next_tech, node_id)
			var success: bool = bool(res.get("success", false))
			print(PREFIX + "RESEARCH|START|tech=%s|success=%s|reason=%s" % [next_tech, str(success), res.get("reason", "")])
			if not success:
				continue
			await create_timer(0.05).timeout
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", RESEARCH_TICK_SKIP)
			await create_timer(0.1).timeout
		# Verify unlock
		var post_tree: Array = _bridge.call("GetTechTreeV0") if _bridge.has_method("GetTechTreeV0") else []
		var unlocked := false
		for t in post_tree:
			if t is Dictionary and t.get("tech_id", "") == next_tech and bool(t.get("unlocked", false)):
				unlocked = true
				break
		researched += 1
		print(PREFIX + "RESEARCH|tech=%s|count=%d|unlocked=%s" % [next_tech, researched, str(unlocked)])
	_busy = false
	_phase = Phase.MEGAPROJECT

func _do_equip() -> void:
	_busy = true
	var slots: Array = _bridge.call("GetPlayerFleetSlotsV0") if _bridge.has_method("GetPlayerFleetSlotsV0") else []
	var mods: Array = _bridge.call("GetAvailableModulesV0") if _bridge.has_method("GetAvailableModulesV0") else []
	# Group modules by slot_kind for matching
	var mods_by_kind: Dictionary = {}
	for m in mods:
		if m is Dictionary and bool(m.get("can_install", true)):
			var kind: String = m.get("slot_kind", "")
			if not mods_by_kind.has(kind):
				mods_by_kind[kind] = []
			mods_by_kind[kind].append(m)
	for s in range(slots.size()):
		if slots[s] is Dictionary and slots[s].get("installed_module_id", "") == "":
			var slot_kind: String = slots[s].get("slot_kind", "")
			if mods_by_kind.has(slot_kind) and mods_by_kind[slot_kind].size() > 0:
				var mod: Dictionary = mods_by_kind[slot_kind].pop_front()
				var mod_id: String = mod.get("module_id", "")
				if mod_id and _bridge.has_method("InstallModuleV0"):
					var res: Dictionary = _bridge.call("InstallModuleV0", "fleet_trader_1", s, mod_id)
					print(PREFIX + "EQUIP|slot=%d|kind=%s|module=%s|success=%s" % [s, slot_kind, mod_id, str(res.get("success", false))])
					await create_timer(0.05).timeout
			else:
				print(PREFIX + "EQUIP|slot=%d|kind=%s|no_matching_module" % [s, slot_kind])
	_busy = false
	_phase = Phase.AUTOMATION

func _do_combat() -> void:
	_busy = true
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	var fought := 0
	var pirate_rare_metals := 0
	var pirate_salvaged_tech := 0
	# Search ALL adjacency nodes (not just visited) -- pirates spawn at frontier/rim nodes
	var search_nodes: Array = _adj.keys()
	for nid in search_nodes:
		if fought >= COMBAT_MAX_FIGHTS:
			break
		var fleets: Array = _bridge.call("GetFleetTransitFactsV0", nid) if _bridge.has_method("GetFleetTransitFactsV0") else []
		for f in fleets:
			if fought >= COMBAT_MAX_FIGHTS:
				break
			if f is Dictionary and bool(f.get("is_hostile", false)):
				var hostile_id: String = f.get("fleet_id", "")
				if hostile_id == "":
					continue
				# Travel to hostile's node if not already there
				var cur: String = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
				if cur != nid:
					_bridge.call("DispatchPlayerArriveV0", nid)
					for _w in range(30):
						await create_timer(0.05).timeout
						if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == nid:
							break
					cur = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
					if cur != nid:
						print(PREFIX + "COMBAT|TRAVEL_FAIL|%s" % nid)
						continue
				_visited[nid] = true
				# Resolve combat
				if _bridge.has_method("ResolveCombatV0"):
					_bridge.call("ResolveCombatV0", "fleet_trader_1", hostile_id)
					await create_timer(0.05).timeout
					print(PREFIX + "COMBAT|FIGHT|%s|%s" % [hostile_id, nid])
				# Clear combat state
				if _bridge.has_method("DispatchClearCombatV0"):
					_bridge.call("DispatchClearCombatV0")
					await create_timer(0.05).timeout
				# Collect any loot dropped
				var loot = _bridge.call("GetNearbyLootV0") if _bridge.has_method("GetNearbyLootV0") else []
				if loot is Array:
					for drop in loot:
						if drop is Dictionary:
							var drop_id: String = str(drop.get("drop_id", ""))
							if drop_id and _bridge.has_method("DispatchCollectLootV0"):
								_bridge.call("DispatchCollectLootV0", drop_id)
								print(PREFIX + "COMBAT|LOOT|%s" % drop_id)
								await create_timer(0.02).timeout
				# Gate 4: PIRATE_LOOT -- verify pirate loot contents
				var cargo_after: Array = _bridge.call("GetPlayerCargoV0")
				var rm := 0
				var st := 0
				for item in cargo_after:
					if item is Dictionary:
						if item.get("good_id", "") == "rare_metals":
							rm = int(item.get("qty", 0))
						elif item.get("good_id", "") == "salvaged_tech":
							st = int(item.get("qty", 0))
				pirate_rare_metals = rm
				pirate_salvaged_tech = st
				print(PREFIX + "COMBAT|PIRATE_LOOT|rare_metals=%d|salvaged_tech=%d" % [rm, st])
				fought += 1
				break  # one fight per node, move to next
	if fought == 0:
		print(PREFIX + "COMBAT|DONE|fights=0|no_hostiles_found")
	else:
		print(PREFIX + "COMBAT|DONE|fights=%d" % fought)
	_busy = false
	_phase = Phase.SCAN

func _do_automation() -> void:
	_busy = true
	var program_ids: Array = []  # Track created programs for monitoring
	var visited_nodes: Array = _visited.keys()

	# ---- 1. Trade Charter: the core automation program (buy low at A → sell high at B) ----
	var best_source := ""
	var best_dest := ""
	var best_good := ""
	var best_spread := 0
	for i in range(visited_nodes.size()):
		var nid_a: String = visited_nodes[i]
		var market_a: Array = _bridge.call("GetPlayerMarketViewV0", nid_a) if _bridge.has_method("GetPlayerMarketViewV0") else []
		for j in range(visited_nodes.size()):
			if i == j: continue
			var nid_b: String = visited_nodes[j]
			var market_b: Array = _bridge.call("GetPlayerMarketViewV0", nid_b) if _bridge.has_method("GetPlayerMarketViewV0") else []
			var sell_prices: Dictionary = {}
			for item in market_b:
				if item is Dictionary:
					var gid: String = item.get("good_id", "")
					var sp: int = int(item.get("sell_price", 0))
					if sp > 0: sell_prices[gid] = sp
			for item in market_a:
				if item is Dictionary:
					var gid: String = item.get("good_id", "")
					var bp: int = int(item.get("buy_price", 0))
					if bp > 0 and sell_prices.has(gid):
						var spread: int = sell_prices[gid] - bp
						if spread > best_spread:
							best_spread = spread
							best_source = nid_a
							best_dest = nid_b
							best_good = gid
	if best_source and best_dest and best_good and _bridge.has_method("CreateTradeCharterProgram"):
		var charter_id: Variant = _bridge.call("CreateTradeCharterProgram", best_source, best_dest, best_good, best_good, 10)
		if charter_id is String and charter_id != "":
			if _bridge.has_method("StartProgram"):
				_bridge.call("StartProgram", charter_id)
			program_ids.append(charter_id)
		print(PREFIX + "AUTOMATION|CHARTER|%s>%s|good=%s|spread=%d|id=%s" % [best_source, best_dest, best_good, best_spread, str(charter_id)])
		await create_timer(0.05).timeout
	else:
		print(PREFIX + "AUTOMATION|NO_CHARTER|best_spread=%d" % best_spread)

	# ---- 2. ResourceTap: automated resource extraction (produces rare_metals for Haven upgrades) ----
	var taps_created := 0
	if _bridge.has_method("CreateResourceTapProgram"):
		for nid in visited_nodes:
			var rm_market: Array = _bridge.call("GetPlayerMarketViewV0", nid) if _bridge.has_method("GetPlayerMarketViewV0") else []
			for item in rm_market:
				if item is Dictionary and item.get("good_id", "") == "rare_metals" and int(item.get("quantity", 0)) > 0:
					var tap_id: Variant = _bridge.call("CreateResourceTapProgram", nid, "rare_metals", 20)
					if tap_id is String and tap_id != "":
						if _bridge.has_method("StartProgram"):
							_bridge.call("StartProgram", tap_id)
						program_ids.append(tap_id)
						taps_created += 1
						print(PREFIX + "AUTOMATION|RESOURCE_TAP|node=%s|good=rare_metals|id=%s" % [nid, str(tap_id)])
						await create_timer(0.05).timeout
					break
			if taps_created > 0: break

	# ---- 3. AutoBuy: automated purchasing at a market (restocks a station with a good) ----
	var auto_buys := 0
	if _bridge.has_method("CreateAutoBuyProgram"):
		# Set up auto-buy for fuel at first visited node (keeps fuel stocked)
		var fuel_node := ""
		for nid in visited_nodes:
			var mk: Array = _bridge.call("GetPlayerMarketViewV0", nid) if _bridge.has_method("GetPlayerMarketViewV0") else []
			for item in mk:
				if item is Dictionary and item.get("good_id", "") == "fuel" and int(item.get("quantity", 0)) > 0:
					fuel_node = nid
					break
			if fuel_node: break
		if fuel_node:
			var ab_id: Variant = _bridge.call("CreateAutoBuyProgram", fuel_node, "fuel", 5, 30)
			if ab_id is String and ab_id != "":
				if _bridge.has_method("StartProgram"):
					_bridge.call("StartProgram", ab_id)
				program_ids.append(ab_id)
				auto_buys += 1
				print(PREFIX + "AUTOMATION|AUTO_BUY|node=%s|good=fuel|qty=5|cadence=30|id=%s" % [fuel_node, str(ab_id)])
				await create_timer(0.02).timeout

	# ---- 4. AutoSell: automated selling at a market (offloads surplus at best price) ----
	var auto_sells := 0
	if _bridge.has_method("CreateAutoSellProgram"):
		var cargo: Array = _bridge.call("GetPlayerCargoV0")
		for item in cargo:
			if item is Dictionary:
				var gid: String = item.get("good_id", "")
				var qty: int = int(item.get("qty", 0))
				if qty > 15 and gid and gid != "fuel":
					# Find node with best sell price for this surplus good
					var sell_node := ""
					var sell_price := 0
					for nid in visited_nodes:
						var mk: Array = _bridge.call("GetPlayerMarketViewV0", nid) if _bridge.has_method("GetPlayerMarketViewV0") else []
						for mi in mk:
							if mi is Dictionary and mi.get("good_id", "") == gid:
								var sp: int = int(mi.get("sell_price", 0))
								if sp > sell_price:
									sell_price = sp
									sell_node = nid
					if sell_node:
						var as_id: Variant = _bridge.call("CreateAutoSellProgram", sell_node, gid, 5, 30)
						if as_id is String and as_id != "":
							if _bridge.has_method("StartProgram"):
								_bridge.call("StartProgram", as_id)
							program_ids.append(as_id)
							auto_sells += 1
							print(PREFIX + "AUTOMATION|AUTO_SELL|node=%s|good=%s|price=%d|id=%s" % [sell_node, gid, sell_price, str(as_id)])
							await create_timer(0.02).timeout
					break  # One auto-sell is enough to cover the API

	# ---- 5. Let programs execute for a bit, then monitor ----
	if _bridge.has_method("DebugAdvanceTicksV0"):
		_bridge.call("DebugAdvanceTicksV0", 200)
		await create_timer(0.15).timeout

	# Program explain snapshot: overview of all active programs
	if _bridge.has_method("GetProgramExplainSnapshot"):
		var snapshot: Variant = _bridge.call("GetProgramExplainSnapshot")
		var snap_count: int = snapshot.size() if snapshot is Array else 0
		print(PREFIX + "AUTOMATION|PROGRAMS_SNAPSHOT|count=%d" % snap_count)
		if snapshot is Array:
			for prog in snapshot:
				if prog is Dictionary:
					print(PREFIX + "AUTOMATION|PROG|id=%s|kind=%s|status=%s|good=%s|market=%s" % [
						str(prog.get("id", "?")), str(prog.get("kind", "?")),
						str(prog.get("status", "?")), str(prog.get("good_id", "?")),
						str(prog.get("market_id", "?"))
					])

	# Program quote: cost/revenue estimate (exercises the quote engine)
	if program_ids.size() > 0 and _bridge.has_method("GetProgramQuote"):
		var quote: Variant = _bridge.call("GetProgramQuote", program_ids[0])
		if quote is Dictionary:
			print(PREFIX + "AUTOMATION|QUOTE|id=%s|unit_price=%s|est_daily=%s|market_ok=%s|credits_ok=%s" % [
				str(program_ids[0]),
				str(quote.get("unit_price_now", "?")),
				str(quote.get("est_daily_cost_or_value", "?")),
				str(quote.get("market_exists", "?")),
				str(quote.get("has_enough_credits_now", "?"))
			])
			var risks: Variant = quote.get("risks", [])
			if risks is Array and risks.size() > 0:
				print(PREFIX + "AUTOMATION|QUOTE_RISKS|%s" % str(risks))

	# Program outcome: execution metadata after running
	if program_ids.size() > 0 and _bridge.has_method("GetProgramOutcome"):
		var outcome: Variant = _bridge.call("GetProgramOutcome", program_ids[0])
		if outcome is Dictionary:
			print(PREFIX + "AUTOMATION|OUTCOME|id=%s|status=%s|last_run=%s|emission=%s" % [
				str(program_ids[0]),
				str(outcome.get("status", "?")),
				str(outcome.get("last_run_tick", "?")),
				str(outcome.get("last_emission", "?"))
			])

	# Program event log: historical events for the program
	if program_ids.size() > 0 and _bridge.has_method("GetProgramEventLogSnapshot"):
		var events: Variant = _bridge.call("GetProgramEventLogSnapshot", program_ids[0], 10)
		var evt_count: int = events.size() if events is Array else 0
		print(PREFIX + "AUTOMATION|EVENT_LOG|id=%s|events=%d" % [str(program_ids[0]), evt_count])
		if events is Array:
			for evt in events:
				if evt is Dictionary:
					print(PREFIX + "AUTOMATION|EVENT|tick=%s|type=%s|note=%s" % [
						str(evt.get("tick", "?")), str(evt.get("type", "?")), str(evt.get("note", "?"))
					])

	# ---- 6. Test lifecycle: pause and cancel one program ----
	if program_ids.size() > 1:
		var test_pid: String = program_ids[-1]
		if _bridge.has_method("PauseProgram"):
			_bridge.call("PauseProgram", test_pid)
			print(PREFIX + "AUTOMATION|PAUSED|id=%s" % test_pid)
			await create_timer(0.02).timeout
		if _bridge.has_method("CancelProgram"):
			_bridge.call("CancelProgram", test_pid)
			print(PREFIX + "AUTOMATION|CANCELLED|id=%s" % test_pid)
			await create_timer(0.02).timeout

	print(PREFIX + "AUTOMATION|DONE|programs=%d|charters=%d|taps=%d|auto_buys=%d|auto_sells=%d" % [
		program_ids.size(), 1 if best_source else 0, taps_created, auto_buys, auto_sells
	])
	_busy = false
	_phase = Phase.DIPLOMACY


# ---- New coverage phases (B1-B8) ----

func _do_intel_check() -> void:
	_busy = true
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	# B4: Trade intel queries
	if _bridge.has_method("GetPriceIntelV0"):
		var intel: Variant = _bridge.call("GetPriceIntelV0", loc)
		print(PREFIX + "INTEL|price_intel|entries=%s" % str(intel.size() if intel is Array else "dict"))
	if _bridge.has_method("GetIntelFreshnessByNodeV0"):
		var freshness: Variant = _bridge.call("GetIntelFreshnessByNodeV0")
		print(PREFIX + "INTEL|freshness|type=%s" % typeof(freshness))
	if _bridge.has_method("GetTradeRoutesV0"):
		var routes: Variant = _bridge.call("GetTradeRoutesV0")
		print(PREFIX + "INTEL|trade_routes|count=%s" % str(routes.size() if routes is Array else "?"))
	if _bridge.has_method("GetNpcTradeRoutesV0"):
		var npc_routes: Variant = _bridge.call("GetNpcTradeRoutesV0")
		print(PREFIX + "INTEL|npc_routes|count=%s" % str(npc_routes.size() if npc_routes is Array else "?"))
	if _bridge.has_method("GetNpcTradeActivityV0"):
		var npc_activity: Variant = _bridge.call("GetNpcTradeActivityV0", loc)
		print(PREFIX + "INTEL|npc_activity|val=%s" % str(npc_activity))
	if _bridge.has_method("GetNpcDemandV0"):
		var npc_demand: Variant = _bridge.call("GetNpcDemandV0", loc)
		print(PREFIX + "INTEL|npc_demand|count=%s" % str(npc_demand.size() if npc_demand is Array else "?"))
	if _bridge.has_method("GetNpcPatrolRoutesV0"):
		var npc_patrols: Variant = _bridge.call("GetNpcPatrolRoutesV0")
		print(PREFIX + "INTEL|npc_patrols|count=%s" % str(npc_patrols.size() if npc_patrols is Array else "?"))
	# Transit cost check
	var neighbors: Array = _get_neighbors(loc)
	if neighbors.size() > 0 and _bridge.has_method("GetTransitCostV0"):
		var cost: Variant = _bridge.call("GetTransitCostV0", "fleet_trader_1", neighbors[0])
		print(PREFIX + "INTEL|transit_cost|%s>%s|result=%s" % [loc, neighbors[0], str(cost)])
	# B5: Security queries at current node
	if _bridge.has_method("GetNodeSecurityV0"):
		var sec: Variant = _bridge.call("GetNodeSecurityV0", loc)
		print(PREFIX + "INTEL|node_security|val=%s" % str(sec))
	if neighbors.size() > 0 and _bridge.has_method("GetLaneSecurityV0"):
		var lane_sec: Variant = _bridge.call("GetLaneSecurityV0", loc, neighbors[0])
		print(PREFIX + "INTEL|lane_security|from=%s|to=%s|val=%s|type=%s" % [loc, neighbors[0], str(lane_sec), typeof(lane_sec)])
	if _bridge.has_method("GetSensorGhostsV0"):
		var ghosts: Variant = _bridge.call("GetSensorGhostsV0")
		print(PREFIX + "INTEL|sensor_ghosts|count=%s" % str(ghosts.size() if ghosts is Array else "?"))
	if _bridge.has_method("GetConfiscationHistoryV0"):
		var confiscations: Variant = _bridge.call("GetConfiscationHistoryV0")
		print(PREFIX + "INTEL|confiscation_history|count=%s" % str(confiscations.size() if confiscations is Array else "?"))
	print(PREFIX + "INTEL_CHECK|DONE")
	_busy = false
	_phase = Phase.EXPLORE
	_explore_round = 0

func _do_maintenance_check() -> void:
	_busy = true
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	# B6: Maintenance queries
	if _bridge.has_method("GetNodeMaintenanceV0"):
		var maint: Variant = _bridge.call("GetNodeMaintenanceV0", loc)
		print(PREFIX + "MAINT|node_maintenance|count=%s" % str(maint.size() if maint is Array else "?"))
	if _bridge.has_method("GetSupplyLevelV0"):
		var supply: Variant = _bridge.call("GetSupplyLevelV0", loc)
		print(PREFIX + "MAINT|supply_level|type=%s" % typeof(supply))
	if _bridge.has_method("GetRepairBlockReasonV0"):
		var block: Variant = _bridge.call("GetRepairBlockReasonV0", loc)
		print(PREFIX + "MAINT|repair_block|reason=%s" % str(block))
	if _bridge.has_method("DispatchSupplyRepairV0"):
		var repair_res: Variant = _bridge.call("DispatchSupplyRepairV0", loc, 1)
		print(PREFIX + "MAINT|supply_repair|result=%s" % str(repair_res))
	print(PREFIX + "MAINTENANCE_CHECK|DONE")
	_busy = false
	_phase = Phase.UPGRADE

func _do_megaproject() -> void:
	_busy = true
	# B7: Megaproject queries + attempt
	if _bridge.has_method("GetMegaprojectTypesV0"):
		var types: Variant = _bridge.call("GetMegaprojectTypesV0")
		print(PREFIX + "MEGA|types|count=%s" % str(types.size() if types is Array else "?"))
		# Try to start one if types available
		if types is Array and types.size() > 0:
			var first_type: Variant = types[0]
			var type_id: String = first_type.get("type_id", "") if first_type is Dictionary else str(first_type)
			if type_id and _bridge.has_method("StartMegaprojectV0"):
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				var loc: String = ps.get("current_node_id", "")
				var start_res: Variant = _bridge.call("StartMegaprojectV0", type_id, loc)
				print(PREFIX + "MEGA|start|type=%s|result=%s" % [type_id, str(start_res)])
				await create_timer(0.05).timeout
	if _bridge.has_method("GetMegaprojectsV0"):
		var projects: Variant = _bridge.call("GetMegaprojectsV0")
		print(PREFIX + "MEGA|active|count=%s" % str(projects.size() if projects is Array else "?"))
		# Try to deliver supply to first active project
		if projects is Array and projects.size() > 0 and _bridge.has_method("DeliverMegaprojectSupplyV0"):
			var proj: Variant = projects[0]
			var proj_id: String = proj.get("project_id", "") if proj is Dictionary else ""
			if proj_id:
				var del_res: Variant = _bridge.call("DeliverMegaprojectSupplyV0", proj_id)
				print(PREFIX + "MEGA|deliver|project=%s|result=%s" % [proj_id, str(del_res)])
	if _bridge.has_method("GetMegaprojectDetailV0") and _bridge.has_method("GetMegaprojectsV0"):
		var projs2: Variant = _bridge.call("GetMegaprojectsV0")
		if projs2 is Array and projs2.size() > 0:
			var pid: String = projs2[0].get("project_id", "") if projs2[0] is Dictionary else ""
			if pid:
				var detail: Variant = _bridge.call("GetMegaprojectDetailV0", pid)
				print(PREFIX + "MEGA|detail|project=%s|type=%s" % [pid, typeof(detail)])
	print(PREFIX + "MEGAPROJECT|DONE")
	_busy = false
	_phase = Phase.EQUIP

func _do_diplomacy() -> void:
	_busy = true
	# B1: Diplomacy queries (exercised before endgame)
	if _bridge.has_method("GetAllFactionsV0"):
		var factions: Variant = _bridge.call("GetAllFactionsV0")
		print(PREFIX + "DIPLO|factions|count=%s" % str(factions.size() if factions is Array else "?"))
	# Check reputation with known factions
	var faction_ids := ["concord", "weavers", "ironclad", "syndicate"]
	for fid in faction_ids:
		if _bridge.has_method("GetPlayerReputationV0"):
			var rep: Variant = _bridge.call("GetPlayerReputationV0", fid)
			if rep is Dictionary and rep.size() > 0:
				print(PREFIX + "DIPLO|rep|%s=%s" % [fid, str(rep.get("reputation", "?"))])
	# Treaties
	if _bridge.has_method("GetActiveTreatiesV0"):
		var treaties: Variant = _bridge.call("GetActiveTreatiesV0")
		print(PREFIX + "DIPLO|treaties|count=%s" % str(treaties.size() if treaties is Array else "?"))
	# Propose treaty with concord
	if _bridge.has_method("ProposeTreatyV0"):
		_bridge.call("ProposeTreatyV0", "concord")
		print(PREFIX + "DIPLO|PROPOSE_TREATY|concord")
		await create_timer(0.05).timeout
	# Check proposals
	if _bridge.has_method("GetDiplomaticProposalsV0"):
		var proposals: Variant = _bridge.call("GetDiplomaticProposalsV0")
		print(PREFIX + "DIPLO|proposals|count=%s" % str(proposals.size() if proposals is Array else "?"))
	# Bounties
	if _bridge.has_method("GetAvailableBountiesV0"):
		var bounties: Variant = _bridge.call("GetAvailableBountiesV0")
		print(PREFIX + "DIPLO|bounties|count=%s" % str(bounties.size() if bounties is Array else "?"))
	# Sanctions
	if _bridge.has_method("GetSanctionsV0"):
		var sanctions: Variant = _bridge.call("GetSanctionsV0")
		print(PREFIX + "DIPLO|sanctions|count=%s" % str(sanctions.size() if sanctions is Array else "?"))
	# Embargoes (requires marketId — use current node)
	var diplo_ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var diplo_loc: String = diplo_ps.get("current_node_id", "")
	if _bridge.has_method("GetEmbargoesV0") and diplo_loc:
		var embargoes: Variant = _bridge.call("GetEmbargoesV0", diplo_loc)
		print(PREFIX + "DIPLO|embargoes|count=%s" % str(embargoes.size() if embargoes is Array else "?"))
	# Faction detail
	if _bridge.has_method("GetFactionDetailV0"):
		var detail: Variant = _bridge.call("GetFactionDetailV0", "concord")
		print(PREFIX + "DIPLO|faction_detail|concord|type=%s" % typeof(detail))
	# Territory access
	if _bridge.has_method("GetTerritoryAccessV0") and diplo_loc:
		var access: Variant = _bridge.call("GetTerritoryAccessV0", diplo_loc)
		print(PREFIX + "DIPLO|territory_access|type=%s" % typeof(access))
	print(PREFIX + "DIPLOMACY|DONE")
	_busy = false
	_phase = Phase.WARFRONT_CHECK

func _do_warfront_check() -> void:
	_busy = true
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	# B2: Warfront queries
	if _bridge.has_method("GetWarfrontsV0"):
		var warfronts: Variant = _bridge.call("GetWarfrontsV0")
		print(PREFIX + "WAR|warfronts|count=%s" % str(warfronts.size() if warfronts is Array else "?"))
	if _bridge.has_method("GetNodeWarIntensityV0"):
		var intensity: Variant = _bridge.call("GetNodeWarIntensityV0", loc)
		print(PREFIX + "WAR|intensity|node=%s|val=%s" % [loc, str(intensity)])
	if _bridge.has_method("GetWarSupplyV0") and _bridge.has_method("GetWarfrontsV0"):
		var wfs: Variant = _bridge.call("GetWarfrontsV0")
		if wfs is Array and wfs.size() > 0 and wfs[0] is Dictionary:
			var wfid: String = wfs[0].get("warfront_id", "")
			if wfid:
				var supply: Variant = _bridge.call("GetWarSupplyV0", wfid)
				print(PREFIX + "WAR|supply|wf=%s|type=%s" % [wfid, typeof(supply)])
	if _bridge.has_method("GetSupplyShockSummaryV0"):
		var shock: Variant = _bridge.call("GetSupplyShockSummaryV0")
		print(PREFIX + "WAR|supply_shock|type=%s" % typeof(shock))
	if _bridge.has_method("GetActiveWarConsequencesV0"):
		var consequences: Variant = _bridge.call("GetActiveWarConsequencesV0")
		print(PREFIX + "WAR|consequences|type=%s" % typeof(consequences))
	# B3: Planet scanning at current node
	if _bridge.has_method("GetPlanetInfoV0"):
		var planet: Variant = _bridge.call("GetPlanetInfoV0", loc)
		print(PREFIX + "PLANET|info|node=%s|type=%s" % [loc, typeof(planet)])
	if _bridge.has_method("GetStarInfoV0"):
		var star: Variant = _bridge.call("GetStarInfoV0", loc)
		print(PREFIX + "PLANET|star_info|node=%s|type=%s" % [loc, typeof(star)])
	if _bridge.has_method("GetScanChargesV0"):
		var charges: Variant = _bridge.call("GetScanChargesV0")
		print(PREFIX + "PLANET|scan_charges|val=%s" % str(charges))
	if _bridge.has_method("GetScannerRangeV0"):
		var scan_range: Variant = _bridge.call("GetScannerRangeV0")
		print(PREFIX + "PLANET|scanner_range|val=%s" % str(scan_range))
	if _bridge.has_method("GetScanAffinityV0"):
		var affinity: Variant = _bridge.call("GetScanAffinityV0", loc, "orbital")
		print(PREFIX + "PLANET|scan_affinity|val=%s" % str(affinity))
	if _bridge.has_method("LandingScanV0"):
		var landing: Variant = _bridge.call("LandingScanV0", loc, "surface")
		print(PREFIX + "PLANET|landing_scan|node=%s|type=%s" % [loc, typeof(landing)])
	if _bridge.has_method("GetPlanetScanResultsV0"):
		var results: Variant = _bridge.call("GetPlanetScanResultsV0", loc)
		print(PREFIX + "PLANET|scan_results|node=%s|type=%s" % [loc, typeof(results)])
	if _bridge.has_method("AtmosphericSampleV0"):
		var atmo: Variant = _bridge.call("AtmosphericSampleV0", loc, "atmosphere")
		print(PREFIX + "PLANET|atmospheric_sample|node=%s|type=%s" % [loc, typeof(atmo)])
	# B8: Passive report queries (dread, pressure, risk)
	if _bridge.has_method("GetDreadStateV0"):
		var dread: Variant = _bridge.call("GetDreadStateV0")
		print(PREFIX + "REPORT_Q|dread_state|type=%s" % typeof(dread))
	if _bridge.has_method("GetLatticeFaunaV0"):
		var fauna: Variant = _bridge.call("GetLatticeFaunaV0")
		print(PREFIX + "REPORT_Q|lattice_fauna|count=%s" % str(fauna.size() if fauna is Array else "?"))
	if _bridge.has_method("GetPressureDomainsV0"):
		var pressure: Variant = _bridge.call("GetPressureDomainsV0")
		print(PREFIX + "REPORT_Q|pressure_domains|type=%s" % typeof(pressure))
	if _bridge.has_method("GetRiskMetersV0"):
		var risk: Variant = _bridge.call("GetRiskMetersV0")
		print(PREFIX + "REPORT_Q|risk_meters|type=%s" % typeof(risk))
	if _bridge.has_method("GetExposureV0"):
		var exposure: Variant = _bridge.call("GetExposureV0")
		print(PREFIX + "REPORT_Q|exposure|type=%s" % typeof(exposure))
	# Story state queries
	if _bridge.has_method("GetStoryProgressV0"):
		var story: Variant = _bridge.call("GetStoryProgressV0")
		print(PREFIX + "REPORT_Q|story_progress|type=%s" % typeof(story))
	if _bridge.has_method("GetPentagonStateV0"):
		var pentagon: Variant = _bridge.call("GetPentagonStateV0")
		print(PREFIX + "REPORT_Q|pentagon_state|type=%s" % typeof(pentagon))
	print(PREFIX + "WARFRONT_CHECK|DONE")
	_busy = false
	_phase = Phase.ENDGAME

func _do_endgame() -> void:
	_busy = true
	var _endgame_path_chosen := false

	# Try to choose endgame path — requires Haven tier 4 (Expanded).
	# Will retry inside the endgame loop after upgrades.
	if _bridge.has_method("ChooseEndgamePathV0"):
		var haven_st: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
		var cur_tier: int = int(haven_st.get("tier", 0))
		if cur_tier >= 4:
			_bridge.call("ChooseEndgamePathV0", "reinforce")
			_endgame_path_chosen = true
			print(PREFIX + "ENDGAME|PATH_CHOSEN|reinforce|tier=%d" % cur_tier)
		else:
			print(PREFIX + "ENDGAME|PATH_DEFERRED|tier=%d<4" % cur_tier)
		await create_timer(0.05).timeout

	# For stress mode, use extended cycles from _tick_budget
	var endgame_cycles: int = _tick_budget if _mode == "stress" else ENDGAME_TRADE_CYCLES

	# Find Concord and Weaver faction nodes for targeted rep building
	var faction_nodes: Dictionary = {}  # faction_id -> [node_ids]
	var node_faction_map: Array = _bridge.call("GetNodeFactionMapV0") if _bridge.has_method("GetNodeFactionMapV0") else []
	for entry in node_faction_map:
		if entry is Dictionary:
			var fid: String = entry.get("faction_id", "")
			var nid: String = entry.get("node_id", "")
			if fid and nid:
				if not faction_nodes.has(fid):
					faction_nodes[fid] = []
				faction_nodes[fid].append(nid)
	var target_factions := ["concord", "weavers"]

	for cycle in range(endgame_cycles):
		var progress: Dictionary = _bridge.call("GetEndgameProgressV0") if _bridge.has_method("GetEndgameProgressV0") else {}
		var pct: float = float(progress.get("completion_percent", 0))
		if pct >= 100.0:
			print(PREFIX + "ENDGAME|COMPLETE|pct=%.0f" % pct)
			break

		# Accept any available missions (builds rep on completion)
		var active_m: Dictionary = _bridge.call("GetActiveMissionV0") if _bridge.has_method("GetActiveMissionV0") else {}
		if active_m == null or active_m.get("mission_id", "") == "":
			var missions: Array = _bridge.call("GetMissionListV0") if _bridge.has_method("GetMissionListV0") else []
			if missions.size() > 0 and missions[0] is Dictionary:
				var mid: String = missions[0].get("mission_id", "")
				if mid and _bridge.has_method("AcceptMissionV0"):
					_bridge.call("AcceptMissionV0", mid)
					print(PREFIX + "ENDGAME|MISSION_ACCEPT|%s" % mid)
			# Also try systemic offers
			var offers: Array = _bridge.call("GetSystemicOffersV0") if _bridge.has_method("GetSystemicOffersV0") else []
			if offers.size() > 0 and offers[0] is Dictionary:
				var oid: String = offers[0].get("offer_id", "")
				if oid and _bridge.has_method("AcceptSystemicMissionV0"):
					_bridge.call("AcceptSystemicMissionV0", oid)
					print(PREFIX + "ENDGAME|SYSTEMIC_ACCEPT|%s" % oid)

		# Gate 2: MISSION_TARGET -- navigate to mission objectives
		active_m = _bridge.call("GetActiveMissionV0") if _bridge.has_method("GetActiveMissionV0") else {}
		if active_m != null and active_m.get("mission_id", "") != "":
			var mission_summary: Dictionary = _bridge.call("GetActiveMissionSummaryV0") if _bridge.has_method("GetActiveMissionSummaryV0") else {}
			var target_node: String = mission_summary.get("target_node_id", "")
			var target_good: String = mission_summary.get("target_good_id", "")
			var cur_loc: String = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
			# Navigate to target node if specified and different from current
			if target_node and target_node != cur_loc:
				_bridge.call("DispatchPlayerArriveV0", target_node)
				for _wm in range(30):
					await create_timer(0.05).timeout
					if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == target_node:
						break
				cur_loc = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
				if cur_loc == target_node:
					_visited[target_node] = true
					print(PREFIX + "ENDGAME|MISSION_NAVIGATE|%s" % target_node)
			# Buy target good if specified (at current node before navigating)
			if target_good and cur_loc:
				_bridge.call("DispatchPlayerTradeV0", cur_loc, target_good, 10, true)
				await create_timer(0.02).timeout
				print(PREFIX + "ENDGAME|MISSION_BUY|%s|node=%s" % [target_good, cur_loc])
			# GATE.T56.BOT.MISSION_COMPLETE_FIX.001: Sell/deliver target good at destination
			# Delivery missions need the player to sell the good at the target node.
			if target_good and target_node and cur_loc == target_node:
				_bridge.call("DispatchPlayerTradeV0", cur_loc, target_good, 10, false)
				await create_timer(0.02).timeout
				print(PREFIX + "ENDGAME|MISSION_DELIVER|%s|node=%s" % [target_good, cur_loc])
			# GATE.T56.BOT.MISSION_COMPLETE_FIX.001: Advance more ticks (100) to let
			# MissionSystem evaluate triggers after navigation + trade. 20 was too few
			# for delivery missions that need sell confirmation + system processing.
			if _bridge.has_method("DebugAdvanceTicksV0"):
				_bridge.call("DebugAdvanceTicksV0", 100)
				await create_timer(0.1).timeout
			# Check if mission completed
			var post_m: Dictionary = _bridge.call("GetActiveMissionV0") if _bridge.has_method("GetActiveMissionV0") else {}
			if post_m == null or post_m.get("mission_id", "") == "":
				print(PREFIX + "ENDGAME|MISSION_COMPLETE")

		# Retry endgame path choice if haven tier reached 4
		if not _endgame_path_chosen and _bridge.has_method("ChooseEndgamePathV0"):
			var h_st: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
			var h_tier: int = int(h_st.get("tier", 0))
			if h_tier < 4:
				# Try upgrading haven mid-endgame (buy materials + upgrade)
				var eg_ps: Dictionary = _bridge.call("GetPlayerStateV0")
				var eg_loc: String = eg_ps.get("current_node_id", "")
				var upgrade_goods := ["exotic_matter", "composites", "electronics", "rare_metals", "exotic_crystals"]
				for gid in upgrade_goods:
					if int(eg_ps.get("credits", 0)) > 50:
						_bridge.call("DispatchPlayerTradeV0", eg_loc, gid, 50, true)
						await create_timer(0.02).timeout
				if _bridge.has_method("UpgradeHavenV0"):
					_bridge.call("UpgradeHavenV0")
					await create_timer(0.05).timeout
				if _bridge.has_method("DebugAdvanceTicksV0"):
					_bridge.call("DebugAdvanceTicksV0", 100)
					await create_timer(0.05).timeout
				h_st = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
				h_tier = int(h_st.get("tier", 0))
				if h_tier > int(eg_ps.get("tier", 0)):
					print(PREFIX + "ENDGAME|HAVEN_UPGRADED|tier=%d" % h_tier)
			if h_tier >= 4:
				_bridge.call("ChooseEndgamePathV0", "reinforce")
				_endgame_path_chosen = true
				print(PREFIX + "ENDGAME|PATH_CHOSEN|reinforce|tier=%d|cycle=%d" % [h_tier, cycle])
				await create_timer(0.05).timeout

		# Diplomatic proposals -- propose treaties and accept incoming proposals
		if cycle == 0:
			for faction_id in target_factions:
				if _bridge.has_method("ProposeTreatyV0"):
					_bridge.call("ProposeTreatyV0", faction_id)
					print(PREFIX + "ENDGAME|DIPLO|PROPOSE_TREATY|%s" % faction_id)
					await create_timer(0.05).timeout
		if _bridge.has_method("GetDiplomaticProposalsV0"):
			var proposals: Array = _bridge.call("GetDiplomaticProposalsV0")
			for prop in proposals:
				if prop is Dictionary:
					var prop_id: String = prop.get("proposal_id", "")
					if prop_id and _bridge.has_method("AcceptProposalV0"):
						_bridge.call("AcceptProposalV0", prop_id)
						print(PREFIX + "ENDGAME|DIPLO|ACCEPT|%s" % prop_id)
						await create_timer(0.02).timeout

		# Travel to a target faction node and trade there (rep gain per trade now wired)
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var loc: String = ps.get("current_node_id", "")
		var credits: int = int(ps.get("credits", 0))
		var target_faction: String = target_factions[cycle % target_factions.size()]
		if faction_nodes.has(target_faction) and faction_nodes[target_faction].size() > 0:
			var faction_node: String = faction_nodes[target_faction][cycle % faction_nodes[target_faction].size()]
			if faction_node != loc:
				_bridge.call("DispatchPlayerArriveV0", faction_node)
				for _w in range(30):
					await create_timer(0.05).timeout
					if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == faction_node:
						break
				loc = _bridge.call("GetPlayerStateV0").get("current_node_id", "")

		# Trade at current location (buy+sell = 2 rep gain per good with wired trade rep)
		var eg_market: Array = _bridge.call("GetPlayerMarketViewV0", loc) if loc else []
		for item in eg_market:
			if item is Dictionary and int(item.get("quantity", 0)) > 0:
				var gid: String = item.get("good_id", "")
				_bridge.call("DispatchPlayerTradeV0", loc, gid, 5, true)
				await create_timer(0.02).timeout
				_bridge.call("DispatchPlayerTradeV0", loc, gid, 5, false)
				await create_timer(0.02).timeout

		# Stress mode: track credits and prices during endgame
		if _mode == "stress":
			ps = _bridge.call("GetPlayerStateV0")
			credits = int(ps.get("credits", 0))
			_credit_trajectory.append(credits)
			if credits == _last_credit_value:
				_consecutive_credit_unchanged += 1
			else:
				_consecutive_credit_unchanged = 0
			_last_credit_value = credits
			# Snapshot every 100 cycles
			if cycle % 100 == 0:
				_profit_snapshots.append(credits)
			# Track prices at current location
			var stress_market: Array = _bridge.call("GetPlayerMarketViewV0", loc) if loc else []
			for sm_item in stress_market:
				if sm_item is Dictionary:
					var sm_gid: String = sm_item.get("good_id", "")
					var sm_bp: int = int(sm_item.get("buy_price", 0))
					if sm_gid and sm_bp > 0:
						_track_price(sm_gid, sm_bp)

		# Collect available fragments — must travel to each fragment's node first
		var frags: Array = _bridge.call("GetAdaptationFragmentsV0") if _bridge.has_method("GetAdaptationFragmentsV0") else []
		var collected_ids: Array = []
		for frag in frags:
			if frag is Dictionary and not bool(frag.get("collected", false)):
				var fid: String = frag.get("fragment_id", "")
				var frag_node: String = frag.get("node_id", "")
				if fid and frag_node and _bridge.has_method("CollectFragmentV0"):
					# Travel to fragment's node
					var cur_loc: String = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
					if cur_loc != frag_node:
						_bridge.call("DispatchPlayerArriveV0", frag_node)
						for _wf in range(30):
							await create_timer(0.05).timeout
							if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == frag_node:
								break
					var collect_result: Variant = _bridge.call("CollectFragmentV0", fid)
					var success: bool = collect_result is Dictionary and bool(collect_result.get("success", false))
					if success:
						collected_ids.append(fid)
						print(PREFIX + "ENDGAME|FRAGMENT_COLLECT|%s|node=%s" % [fid, frag_node])
					else:
						var reason: String = collect_result.get("reason", "?") if collect_result is Dictionary else "?"
						print(PREFIX + "ENDGAME|FRAGMENT_FAIL|%s|reason=%s" % [fid, reason])
				# Limit to 2 fragment trips per cycle to keep endgame moving
				if collected_ids.size() >= 2:
					break

		# Deposit collected fragments at Haven (required to advance endgame progress)
		if collected_ids.size() > 0 and _haven_node_id and _bridge.has_method("DepositFragmentV0"):
			var eg_loc: String = _bridge.call("GetPlayerStateV0").get("current_node_id", "")
			if eg_loc != _haven_node_id:
				_bridge.call("DispatchPlayerArriveV0", _haven_node_id)
				for _wh in range(30):
					await create_timer(0.05).timeout
					if _bridge.call("GetPlayerStateV0").get("current_node_id", "") == _haven_node_id:
						break
			for fid in collected_ids:
				var dep_result: Variant = _bridge.call("DepositFragmentV0", fid)
				var dep_ok: bool = dep_result is Dictionary and bool(dep_result.get("success", false))
				print(PREFIX + "ENDGAME|FRAGMENT_DEPOSIT|%s|success=%s" % [fid, str(dep_ok)])
				await create_timer(0.02).timeout

		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", ENDGAME_TICK_SKIP)
			await create_timer(0.05).timeout
		if cycle % 10 == 0:
			var rep_c: Dictionary = _bridge.call("GetPlayerReputationV0", "concord") if _bridge.has_method("GetPlayerReputationV0") else {}
			var rep_w: Dictionary = _bridge.call("GetPlayerReputationV0", "weavers") if _bridge.has_method("GetPlayerReputationV0") else {}
			# Log detailed progress requirements on first cycle
			if cycle == 0:
				var prog: Dictionary = _bridge.call("GetEndgameProgressV0") if _bridge.has_method("GetEndgameProgressV0") else {}
				print(PREFIX + "ENDGAME|PROGRESS_DETAIL|haven_tier=%s/%s(met=%s)|rep1=%s=%s/%s(met=%s)|rep2=%s=%s/%s(met=%s)|frag1=%s(met=%s)|frag2=%s(met=%s)|rev=%s/%s(met=%s)" % [
					str(prog.get("haven_tier_current", "?")), str(prog.get("haven_tier_required", "?")), str(prog.get("haven_tier_met", "?")),
					str(prog.get("faction_rep1_id", "?")), str(prog.get("faction_rep1_current", "?")), str(prog.get("faction_rep1_required", "?")), str(prog.get("faction_rep1_met", "?")),
					str(prog.get("faction_rep2_id", "?")), str(prog.get("faction_rep2_current", "?")), str(prog.get("faction_rep2_required", "?")), str(prog.get("faction_rep2_met", "?")),
					str(prog.get("fragment1_id", "?")), str(prog.get("fragment1_met", "?")),
					str(prog.get("fragment2_id", "?")), str(prog.get("fragment2_met", "?")),
					str(prog.get("revelations_current", "?")), str(prog.get("revelations_required", "?")), str(prog.get("revelations_met", "?")),
				])
			print(PREFIX + "ENDGAME|CYCLE|%d|pct=%.0f|concord_rep=%s|weaver_rep=%s" % [
				cycle, pct,
				str(rep_c.get("reputation", 0)) if rep_c else "?",
				str(rep_w.get("reputation", 0)) if rep_w else "?"
			])
	_busy = false
	_phase = Phase.VICTORY

func _do_victory() -> void:
	_busy = true
	for _poll in range(VICTORY_POLL_MAX):
		var game_result: Dictionary = _bridge.call("GetGameResultV0") if _bridge.has_method("GetGameResultV0") else {}
		if bool(game_result.get("game_over", false)):
			var outcome: String = game_result.get("outcome", "")
			if outcome == "victory":
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				print(PREFIX + "VICTORY|ticks=%d|credits=%d|nodes=%d" % [
					int(ps.get("tick", 0)), int(ps.get("credits", 0)), _visited.size()
				])
				_phase = Phase.DONE
				_finish()
				return
			else:
				print(PREFIX + "VICTORY|GAME_OVER|outcome=%s" % outcome)
				_add_flag("GAME_OVER_NOT_VICTORY", "WARNING", "Game ended with outcome=%s" % outcome, "Expected victory but got different outcome")
				_phase = Phase.DONE
				_finish()
				return
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", 50)
			await create_timer(0.05).timeout
	print(PREFIX + "VICTORY|TIMEOUT|not_reached")
	_add_flag("VICTORY_TIMEOUT", "WARNING", "Victory not reached in %d polls" % VICTORY_POLL_MAX, "Endgame may need more cycles")
	_phase = Phase.DONE
	_finish()


# ---- Autonomous bot loop (absorbed from exploration_bot_v1) ----

func _do_bot_loop() -> void:
	if _bot_cycle >= _tick_budget:
		_phase = Phase.REPORT
		return

	if _bot_sub_tick == 0:
		_bot_decide_and_act()
		_bot_sub_tick = 1
	else:
		_bot_sub_tick += 1
		if _bot_sub_tick >= ACT_INTERVAL:
			_bot_sub_tick = 0
			_bot_cycle += 1
			# Snapshot credits every 100 cycles for profit trend
			if _bot_cycle % 100 == 0:
				var snap_ps: Dictionary = _bridge.call("GetPlayerStateV0")
				_profit_snapshots.append(int(snap_ps.get("credits", 0)))

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: int = int(ps.get("credits", 0))
	_credit_trajectory.append(credits)

	# Track credit plateau
	if credits == _last_credit_value:
		_consecutive_credit_unchanged += 1
	else:
		_consecutive_credit_unchanged = 0
	_last_credit_value = credits

func _bot_decide_and_act() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = str(ps.get("current_node_id", ""))
	var credits: int = int(ps.get("credits", 0))
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var cargo_count: int = int(ps.get("cargo_count", 0))

	if _combat_cooldown_remaining > 0:
		_combat_cooldown_remaining -= 1

	if loc.is_empty():
		_record_action(_bot_cycle, "IDLE", loc, "", 0, "no location")
		_total_idles += 1
		_idle_cycles_total += 1
		return

	# Phase 0: Committed exploration
	if not _exploration_target.is_empty():
		if loc == _exploration_target or _visited.has(_exploration_target):
			_exploration_target = ""
		else:
			var hop := _bot_get_next_hop(loc, _exploration_target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "committed exploration toward %s" % _exploration_target)
				return
			_exploration_target = ""

	# Phase 0b: Exploration pressure
	var has_unvisited := false
	for nid in _adj.keys():
		if not _visited.has(nid):
			has_unvisited = true
			break

	if has_unvisited and _trades_since_explore >= EXPLORE_EVERY_N and cargo_count == 0:
		var target := _bot_find_nearest_unvisited(loc)
		if not target.is_empty():
			_exploration_target = target
			_trades_since_explore = 0
			var hop := _bot_get_next_hop(loc, target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "starting exploration toward %s" % target)
				return

	# Phase 1: Combat (if mode supports it)
	if _mode in ["combat"] and _combat_cooldown_remaining <= 0:
		if _bot_try_combat(loc):
			return

	# Phase 2: If carrying cargo -> sell
	if cargo_count > 0 and cargo.size() > 0:
		var sell_result := _bot_try_sell(loc, cargo, credits)
		if sell_result:
			return

	# Phase 3: No cargo -> buy
	if cargo_count == 0:
		var buy_result := _bot_try_buy(loc, credits)
		if buy_result:
			return

	# Phase 4: Combat-only mode -- move toward hostiles
	if _mode == "combat" and cargo_count == 0:
		var target := _bot_find_node_with_hostiles(loc)
		if not target.is_empty() and target != loc:
			var hop := _bot_get_next_hop(loc, target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "hunting hostiles at %s" % target)
				return

	# Phase 5: Explore unvisited
	if has_unvisited:
		var target := _bot_find_nearest_unvisited(loc)
		if not target.is_empty():
			var hop := _bot_get_next_hop(loc, target)
			if not hop.is_empty():
				_bot_do_travel(loc, hop, "fallback explore toward %s" % target)
				return

	# Phase 6: Force buy untouched goods (good rotation)
	if cargo_count == 0:
		var untouched := _bot_find_untouched_good(loc, credits)
		if not untouched.node.is_empty():
			if untouched.node == loc:
				_bot_do_buy(loc, untouched.good, 1, credits)
				return
			else:
				var hop := _bot_get_next_hop(loc, untouched.node)
				if not hop.is_empty():
					_bot_do_travel(loc, hop, "hunting untouched good %s at %s" % [untouched.good, untouched.node])
					return

	# Phase 7: Roam to least-visited node
	var least_visited_node := _bot_find_least_visited(loc)
	if not least_visited_node.is_empty() and least_visited_node != loc:
		var hop := _bot_get_next_hop(loc, least_visited_node)
		if not hop.is_empty():
			_bot_do_travel(loc, hop, "idle fallback: roaming toward %s" % least_visited_node)
			return

	# Phase 8: Nothing to do
	_consecutive_idles += 1
	_total_idles += 1
	_idle_cycles_total += 1
	if _consecutive_idles > _max_consecutive_idles:
		_max_consecutive_idles = _consecutive_idles
	_record_action(_bot_cycle, "IDLE", loc, "", 0, "nothing to do")

	if _consecutive_idles >= 10:
		_add_flag("STUCK_NO_ACTIONS", "CRITICAL",
			"Bot idle for %d cycles at %s" % [_consecutive_idles, loc],
			"Economy collapsed or bot stuck.")


# ---- Bot combat ----

func _bot_try_combat(loc: String) -> bool:
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

	if not _combat_hp_init_done:
		if _bridge.has_method("InitFleetCombatHpV0"):
			_bridge.call("InitFleetCombatHpV0")
			_combat_hp_init_done = true

	var pre_hp: Dictionary = {}
	if _bridge.has_method("GetFleetCombatHpV0"):
		pre_hp = _bridge.call("GetFleetCombatHpV0", hostile_id)

	var result: Dictionary = _bridge.call("ResolveCombatV0", "fleet_trader_1", hostile_id)
	_total_combats += 1
	_combat_cooldown_remaining = COMBAT_COOLDOWN

	var outcome: String = str(result.get("outcome", "unknown"))
	var rounds: int = int(result.get("rounds", 0))
	var attacker_hull: int = int(result.get("attacker_hull", 0))
	var defender_hull: int = int(result.get("defender_hull", 0))
	var salvage: int = int(result.get("salvage", 0))

	_record_action(_bot_cycle, "COMBAT", loc, hostile_id, rounds,
		"vs %s: %s in %d rounds, hull=%d def_hull=%d salvage=%d" % [
			hostile_id, outcome, rounds, attacker_hull, defender_hull, salvage])
	print(PREFIX + "BOT_COMBAT|cycle=%d|target=%s|outcome=%s|rounds=%d|hull=%d|salvage=%d" % [
		_bot_cycle, hostile_id, outcome, rounds, attacker_hull, salvage])

	if pre_hp.size() > 0:
		var pre_hull: int = int(pre_hp.get("hull", 0))
		if defender_hull >= pre_hull and pre_hull > 0 and outcome != "defender_fled":
			_add_flag("DAMAGE_NOT_APPLIED", "CRITICAL",
				"Combat vs %s: hull %d->%d unchanged" % [hostile_id, pre_hull, defender_hull],
				"ResolveCombatV0 ran but HP didn't change.")

	if outcome == "Victory":
		_total_kills += 1
	if attacker_hull <= 0:
		_add_flag("PLAYER_DIED", "WARNING",
			"Player hull reached 0 in combat vs %s" % hostile_id,
			"Player fleet was destroyed.")

	_consecutive_idles = 0
	return true

func _bot_find_node_with_hostiles(from: String) -> String:
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


# ---- Bot trade ----

func _bot_try_sell(loc: String, cargo: Array, credits: int) -> bool:
	var best_good := ""
	var best_sell_node := ""
	var best_sell_price := 0

	var global_sell: Dictionary = {}
	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var market_view: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in market_view:
			var gid: String = str(entry.get("good_id", ""))
			var sp: int = int(entry.get("sell_price", 0))
			if gid.is_empty() or sp <= 0:
				continue
			if not global_sell.has(gid) or sp > int(global_sell[gid].get("sell_price", 0)):
				global_sell[gid] = {"node_id": nid, "sell_price": sp}

	for item in cargo:
		var good_id: String = str(item.get("good_id", ""))
		var qty: int = int(item.get("qty", 0))
		if good_id.is_empty() or qty <= 0:
			continue
		if global_sell.has(good_id):
			var info: Dictionary = global_sell[good_id]
			var sp: int = int(info.get("sell_price", 0))
			if sp > best_sell_price:
				best_sell_price = sp
				best_sell_node = str(info.get("node_id", ""))
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
			_bot_do_sell(loc, best_good, qty, credits)
			return true
	else:
		var hop := _bot_get_next_hop(loc, best_sell_node)
		if not hop.is_empty():
			_bot_do_travel(loc, hop, "toward sell %s at %s" % [best_good, best_sell_node])
			return true
	return false

func _bot_try_buy(loc: String, credits: int) -> bool:
	var best_node := ""
	var best_good := ""
	var best_profit := 0
	var best_buy_price := 0
	var best_sell_node := ""

	var best_sell_prices: Dictionary = {}
	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var market_view: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in market_view:
			var good_id: String = str(entry.get("good_id", ""))
			var sp: int = int(entry.get("sell_price", 0))
			if good_id.is_empty() or sp <= 0:
				continue
			if not best_sell_prices.has(good_id) or sp > int(best_sell_prices[good_id].get("price", 0)):
				best_sell_prices[good_id] = {"price": sp, "node_id": nid}

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
			if not best_sell_prices.has(good_id):
				continue
			var sell_info: Dictionary = best_sell_prices[good_id]
			var sell_node: String = str(sell_info.get("node_id", ""))
			var global_profit: int = int(sell_info.get("price", 0)) - bp
			if sell_node == nid:
				continue
			# Penalize frequently-traded goods to force rotation
			var effective_profit := global_profit
			var tc: int = _good_trade_count.get(good_id, 0)
			for _k in range(tc):
				effective_profit = effective_profit / 2
			if effective_profit > best_profit:
				best_profit = effective_profit
				best_node = nid
				best_good = good_id
				best_buy_price = bp
				best_sell_node = sell_node

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
			_bot_do_buy(loc, best_good, qty, credits)
			return true
	else:
		var hop := _bot_get_next_hop(loc, best_node)
		if not hop.is_empty():
			_bot_do_travel(loc, hop, "toward buy %s at %s (sell@%s profit %d/u)" % [best_good, best_node, best_sell_node, best_profit])
			return true
	return false


# ---- Bot action executors ----

func _bot_do_buy(loc: String, good_id: String, qty: int, credits_before: int) -> void:
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, true)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after: int = int(ps.get("credits", 0))
	var succeeded := credits_after < credits_before
	_record_action(_bot_cycle, "BUY", loc, good_id, qty,
		"buy %dx %s @ %s, credits %d->%d" % [qty, good_id, loc, credits_before, credits_after])
	if succeeded:
		_total_buys += 1
		_trades_since_explore += 1
		_total_spent += credits_before - credits_after
		_goods_bought[good_id] = true
		_consecutive_idles = 0
		_good_trade_count[good_id] = _good_trade_count.get(good_id, 0) + 1
		var unit_price: int = (credits_before - credits_after) / max(1, qty)
		_track_price(good_id, unit_price)
	else:
		_add_flag("TRADE_NO_EFFECT", "CRITICAL",
			"BUY %dx %s at %s had no effect (credits %d->%d)" % [qty, good_id, loc, credits_before, credits_after],
			"DispatchPlayerTradeV0 buy failed.")

func _bot_do_sell(loc: String, good_id: String, qty: int, credits_before: int) -> void:
	_bridge.call("DispatchPlayerTradeV0", loc, good_id, qty, false)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after: int = int(ps.get("credits", 0))
	var succeeded := credits_after > credits_before
	_record_action(_bot_cycle, "SELL", loc, good_id, qty,
		"sell %dx %s @ %s, credits %d->%d" % [qty, good_id, loc, credits_before, credits_after])
	if succeeded:
		_total_sells += 1
		_trades_since_explore += 1
		_total_earned += credits_after - credits_before
		_goods_sold[good_id] = true
		_consecutive_idles = 0
		var unit_price: int = (credits_after - credits_before) / max(1, qty)
		_track_price(good_id, unit_price)
	else:
		_add_flag("TRADE_NO_EFFECT", "CRITICAL",
			"SELL %dx %s at %s had no effect (credits %d->%d)" % [qty, good_id, loc, credits_before, credits_after],
			"DispatchPlayerTradeV0 sell failed.")

func _bot_do_travel(from: String, to: String, reason: String) -> void:
	_bridge.call("DispatchPlayerArriveV0", to)
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var new_loc: String = str(ps.get("current_node_id", ""))
	var succeeded := new_loc == to
	_record_action(_bot_cycle, "TRAVEL", to, "", 0, reason)
	if succeeded:
		_total_travels += 1
		_visited[to] = true
		_consecutive_idles = 0
	else:
		_add_flag("TRAVEL_FAILED", "WARNING",
			"Travel to %s from %s failed (now at %s)" % [to, from, new_loc],
			"DispatchPlayerArriveV0 failed.")


# ---- Bot graph helpers ----

func _bot_get_next_hop(from: String, to: String) -> String:
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

func _bot_find_nearest_unvisited(from: String) -> String:
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

func _bot_find_untouched_good(loc: String, credits: int) -> Dictionary:
	var result := {"node": "", "good": ""}
	var best_dist := 999
	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if nid.is_empty():
			continue
		var market_view: Array = _bridge.call("GetPlayerMarketViewV0", nid)
		for entry in market_view:
			var gid: String = str(entry.get("good_id", ""))
			var bp: int = int(entry.get("buy_price", 0))
			var available: int = int(entry.get("quantity", 0))
			if gid.is_empty() or bp <= 0 or available <= 0 or bp > credits:
				continue
			if _good_trade_count.has(gid):
				continue
			var dist := _bot_bfs_distance(loc, nid)
			if dist < best_dist:
				best_dist = dist
				result = {"node": nid, "good": gid}
	return result

func _bot_find_least_visited(loc: String) -> String:
	var best_node := ""
	var min_visits := 999999
	for n in _all_nodes:
		var nid: String = str(n.get("node_id", ""))
		if nid.is_empty() or nid == loc:
			continue
		if _bot_bfs_distance(loc, nid) >= 999:
			continue
		var visits: int = 0
		if _visited.has(nid):
			visits = 1
		if visits < min_visits:
			min_visits = visits
			best_node = nid
	return best_node

func _bot_bfs_distance(from: String, to: String) -> int:
	if from == to:
		return 0
	var queue: Array = [from]
	var dist: Dictionary = {from: 0}
	while queue.size() > 0:
		var current: String = queue.pop_front()
		if current == to:
			return dist[current]
		if not _adj.has(current):
			continue
		var neighbors: Array = _adj[current]
		for n in neighbors:
			if not dist.has(n):
				dist[n] = dist[current] + 1
				queue.append(n)
	return 999


# ---- Stress tracking ----

func _track_price(good_id: String, price: int) -> void:
	if not _price_history.has(good_id):
		_price_history[good_id] = []
	_price_history[good_id].append(price)


# ---- Recording ----

func _record_action(cycle: int, type: String, node: String, good: String, qty: int, detail: String) -> void:
	_actions.append({
		"cycle": cycle,
		"type": type,
		"node": node,
		"good": good,
		"qty": qty,
		"detail": detail
	})


# ---- Flag system (absorbed from exploration_bot_v1) ----

func _add_flag(id: String, severity: String, detail: String, diagnostic: String) -> void:
	for f in _flags:
		if f is Dictionary and f.get("id", "") == id:
			return
	_flags.append({
		"id": id,
		"severity": severity,
		"detail": detail,
		"diagnostic": diagnostic
	})
	print(PREFIX + "FLAG|[%s] %s: %s" % [severity, id, detail])


# ---- Report phase (for trade/combat/stress bot loop modes) ----

func _do_report() -> void:
	_busy = true
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var end_credits: int = int(ps.get("credits", 0))
	var net_profit: int = end_credits - _start_credits

	# Trade flags
	if _total_buys > 0 and _total_sells > 0 and net_profit < 0:
		_add_flag("NET_LOSS", "WARNING",
			"Bot lost money: %d->%d (%d buys, %d sells)" % [_start_credits, end_credits, _total_buys, _total_sells],
			"Trading not profitable through bridge.")

	if _total_buys == 0 and _tick_budget > 50:
		_add_flag("NEVER_BOUGHT", "CRITICAL",
			"Bot never bought anything in %d cycles" % _tick_budget,
			"DispatchPlayerTradeV0 buy path may be broken.")

	if _total_sells == 0 and _total_buys > 0:
		_add_flag("NEVER_SOLD", "CRITICAL",
			"Bot bought but never sold in %d cycles" % _tick_budget,
			"Sell path broken or bot couldn't find sell destination.")

	# Combat flags
	if _mode == "combat" and _total_combats == 0 and _tick_budget > 50:
		_add_flag("NEVER_FOUGHT", "CRITICAL",
			"No combat in %d cycles" % _tick_budget,
			"No hostile fleets found.")

	# Stress/economy flags
	for good_id in _price_history.keys():
		var prices: Array = _price_history[good_id]
		if prices.size() < 10:
			continue
		var first_avg := _avg_slice(prices, 0, mini(5, prices.size()))
		var last_avg := _avg_slice(prices, maxi(0, prices.size() - 5), prices.size())
		if first_avg > 0 and last_avg < first_avg * 0.5:
			_add_flag("PRICE_COLLAPSE", "CRITICAL",
				"%s price collapsed: %.0f->%.0f" % [good_id, first_avg, last_avg],
				"Good lost >50%% value over the run.")

	var idle_pct: float = float(_idle_cycles_total) / max(1, _tick_budget)
	if idle_pct > 0.2 and _tick_budget >= 100:
		_add_flag("ECONOMY_STALL", "WARNING",
			"Bot idle for %d/%d cycles (%d%%)" % [_idle_cycles_total, _tick_budget, int(idle_pct * 100)],
			"Bot couldn't find profitable actions for >20%% of the run.")

	if _consecutive_credit_unchanged > 100:
		_add_flag("CREDIT_PLATEAU", "WARNING",
			"Credits unchanged for %d consecutive cycles" % _consecutive_credit_unchanged,
			"Economy stagnant.")

	# Coverage
	var visited_count: int = _visited.size()
	var total_nodes: int = _all_nodes.size()
	var visit_pct: float = float(visited_count) / max(1, total_nodes)
	if visit_pct < 0.5 and _tick_budget >= 100:
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
			"Some goods lack profitable routes.")

	# Print summary
	print(PREFIX + "REPORT|mode=%s|cycles=%d|nodes=%d/%d|net=%d|buys=%d|sells=%d|travels=%d|idles=%d|combats=%d|kills=%d" % [
		_mode, _tick_budget, visited_count, total_nodes, net_profit,
		_total_buys, _total_sells, _total_travels, _total_idles, _total_combats, _total_kills])
	print(PREFIX + "REPORT|CREDITS|start=%d|end=%d|spent=%d|earned=%d" % [
		_start_credits, end_credits, _total_spent, _total_earned])
	print(PREFIX + "REPORT|GOODS_BOUGHT|%s" % str(_goods_bought.keys()))
	print(PREFIX + "REPORT|GOODS_SOLD|%s" % str(_goods_sold.keys()))

	var idle_pct_i: int = int(float(_total_idles) / max(1, _tick_budget) * 100)
	print(PREFIX + "REPORT|IDLE_PATTERN|longest=%d|idle_pct=%d%%" % [_max_consecutive_idles, idle_pct_i])

	if _profit_snapshots.size() >= 2:
		var trend_parts: Array = []
		for i in range(_profit_snapshots.size()):
			trend_parts.append(str(_profit_snapshots[i]))
		print(PREFIX + "REPORT|ECONOMY|profit_trend=[%s]" % ",".join(trend_parts))

	print(PREFIX + "REPORT|FLAGS|count=%d" % _flags.size())
	for f in _flags:
		if f is Dictionary:
			print(PREFIX + "REPORT|FLAG_DETAIL|[%s] %s: %s" % [
				str(f.get("severity", "")),
				str(f.get("id", "")),
				str(f.get("detail", ""))])

	var critical_count := 0
	for f in _flags:
		if f is Dictionary and str(f.get("severity", "")) == "CRITICAL":
			critical_count += 1

	if critical_count == 0:
		print(PREFIX + "RESULT|PASS")
	else:
		print(PREFIX + "RESULT|FAIL|%d critical flags" % critical_count)

	_busy = false
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0 if critical_count == 0 else 1)


# ---- Helpers shared by both scripted and bot paths ----

func _avg_slice(arr: Array, from_idx: int, to_idx: int) -> float:
	var total := 0.0
	var count := 0
	for i in range(from_idx, to_idx):
		total += float(arr[i])
		count += 1
	return total / max(1, count)

func _finish() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: int = int(ps.get("credits", 0))
	var profit: int = credits - _start_credits
	var flag_str: String = ""
	if _flags.size() > 0:
		var flag_ids: Array = []
		for f in _flags:
			if f is Dictionary:
				flag_ids.append(str(f.get("id", str(f))))
			else:
				flag_ids.append(str(f))
		flag_str = "|".join(flag_ids)
	else:
		flag_str = "NONE"
	print(PREFIX + "SUMMARY|credits=%d|profit=%d|trades=%d|goods=%d|visited=%d|flags=%s" % [
		credits, profit, _trade_count, _goods_traded.size(), _visited.size(),
		flag_str
	])
	if _flags.size() > 0:
		print(PREFIX + "RESULT|WARN|flag_count=%d" % _flags.size())
	else:
		print(PREFIX + "RESULT|PASS")
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0 if _flags.size() == 0 else 1)

func _force_quit(code: int) -> void:
	## Emergency exit — called by safety timeout and stall watchdog.
	## Ensures StopSimV0 is called to prevent C# thread hang, then quits.
	print(PREFIX + "FORCE_QUIT|code=%d|frame=%d|flags=%d" % [code, _total_frames, _flags.size()])
	for f in _flags:
		if f is Dictionary:
			print(PREFIX + "FLAG_DETAIL|[%s] %s: %s" % [
				str(f.get("severity", "")),
				str(f.get("id", "")),
				str(f.get("detail", ""))])
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(code)

func _build_adjacency() -> void:
	_adj.clear()
	_all_nodes.clear()
	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_nodes = galaxy.get("system_nodes", [])
	var lanes: Array = galaxy.get("lane_edges", [])
	print(PREFIX + "ADJ|nodes=%d|lanes=%d" % [_all_nodes.size(), lanes.size()])
	if lanes.size() > 0 and lanes[0] is Dictionary:
		print(PREFIX + "ADJ|SAMPLE_LANE|keys=%s" % str(lanes[0].keys()))
	# Debug: print all node IDs and their neighbor counts
	var node_ids: Array = []
	for node in _all_nodes:
		if node is Dictionary:
			node_ids.append(node.get("node_id", ""))
	print(PREFIX + "ADJ|NODE_IDS=%s" % str(node_ids))
	for nid in _adj:
		if _adj[nid].size() > 0:
			print(PREFIX + "ADJ|%s|neighbors=%d|%s" % [nid, _adj[nid].size(), str(_adj[nid])])
	for node in _all_nodes:
		if node is Dictionary:
			var nid: String = node.get("node_id", "")
			if not _adj.has(nid):
				_adj[nid] = []
	for lane in lanes:
		if lane is Dictionary:
			var from_id: String = lane.get("from_id", "")
			var to_id: String = lane.get("to_id", "")
			if _adj.has(from_id) and not to_id in _adj[from_id]:
				_adj[from_id].append(to_id)
			if _adj.has(to_id) and not from_id in _adj[to_id]:
				_adj[to_id].append(from_id)

func _get_neighbors(node_id: String) -> Array:
	if not _adj.has(node_id):
		return []
	var neighbors: Array = _adj[node_id].duplicate()
	neighbors.sort()
	return neighbors
