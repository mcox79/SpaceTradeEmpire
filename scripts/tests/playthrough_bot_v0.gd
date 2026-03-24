extends SceneTree

## Playthrough Bot v0 -- scripted critical-path playthrough
## Usage: godot --headless --path . -s res://scripts/tests/playthrough_bot_v0.gd
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
const UPGRADE_MAX_TIER := 4
const UPGRADE_TICK_SKIP := 250
const RESEARCH_TARGET := 5
const RESEARCH_TICK_SKIP := 150
const ENDGAME_TRADE_CYCLES := 50
const ENDGAME_TICK_SKIP := 50
const VICTORY_POLL_MAX := 50
const COMBAT_MAX_FIGHTS := 3

enum Phase { WAIT_BRIDGE, WAIT_READY, TUTORIAL, TRADE, EXPLORE, COMBAT, SCAN, EXTRACT, HAVEN, UPGRADE, RESEARCH, EQUIP, AUTOMATION, ENDGAME, VICTORY, DONE }

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

func _initialize() -> void:
	print(PREFIX + "START|playthrough_bot_v0")

func _process(_delta: float) -> bool:
	if _busy:
		return false
	match _phase:
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.TUTORIAL: _do_tutorial()
		Phase.TRADE: _do_trade()
		Phase.EXPLORE: _do_explore()
		Phase.COMBAT: _do_combat()
		Phase.SCAN: _do_scan()
		Phase.EXTRACT: _do_extract()
		Phase.HAVEN: _do_haven()
		Phase.UPGRADE: _do_upgrade()
		Phase.RESEARCH: _do_research()
		Phase.EQUIP: _do_equip()
		Phase.AUTOMATION: _do_automation()
		Phase.ENDGAME: _do_endgame()
		Phase.VICTORY: _do_victory()
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
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		_start_credits = int(ps.get("credits", 0))
		var loc: String = ps.get("current_node_id", "")
		_visited[loc] = true
		print(PREFIX + "INIT|credits=%d|node=%s|galaxy_nodes=%d" % [_start_credits, loc, _all_nodes.size()])
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
	_phase = Phase.TRADE
	_trade_round = 0

func _do_trade() -> void:
	_trade_round += 1
	if _trade_round > TRADE_MAX_ROUNDS:
		_finish_trade("max_rounds")
		return
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: int = int(ps.get("credits", 0))
	var loc: String = ps.get("current_node_id", "")
	var cargo_count: int = int(ps.get("cargo_count", 0))
	var cargo_cap: int = int(ps.get("cargo_capacity", 50))
	if credits >= TRADE_TARGET_CREDITS and _trade_count >= TRADE_MIN_TRADES and _goods_traded.size() >= TRADE_MIN_GOODS:
		_finish_trade("target_met")
		return

	# Cross-node arbitrage: buy cheap here, travel, sell there
	# Keep upgrade-critical goods — don't sell these during trade phase
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

	# Step 2: Buy cheapest goods for arbitrage (upgrade materials bought at end of trade phase)
	ps = _bridge.call("GetPlayerStateV0")
	credits = int(ps.get("credits", 0))
	cargo_count = int(ps.get("cargo_count", 0))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", loc)
	# Buy cheapest goods for arbitrage
	if cargo_count < cargo_cap and credits > 0:
		var best_buys: Array = []
		for item in market:
			if item is Dictionary:
				var buy_price: int = int(item.get("buy_price", 0))
				var qty_avail: int = int(item.get("quantity", 0))
				if buy_price > 0 and qty_avail > 0:
					best_buys.append({"good_id": item.get("good_id", ""), "buy_price": buy_price, "qty": qty_avail})
		best_buys.sort_custom(func(aa, bb): return aa["buy_price"] < bb["buy_price"])
		var bought := 0
		for entry in best_buys:
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
			var good_id: String = entry["good_id"]
			_bridge.call("DispatchPlayerTradeV0", loc, good_id, buy_qty, true)
			_trade_count += 1
			_goods_traded[good_id] = true
			print(PREFIX + "TRADE|BUY|%s|qty=%d|price=%d|node=%s" % [good_id, buy_qty, entry["buy_price"], loc])
			bought += 1

	# Step 3: Travel to a different node every round (cycle through neighbors)
	var neighbors: Array = _get_neighbors(loc)
	if neighbors.size() > 0:
		var target: String = neighbors[_trade_round % neighbors.size()]
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
		_flags.append("TRADE_FEW_TRADES:%d" % _trade_count)
	if _goods_traded.size() < TRADE_MIN_GOODS:
		_flags.append("TRADE_FEW_GOODS:%d" % _goods_traded.size())
	if credits < TRADE_TARGET_CREDITS:
		_flags.append("TRADE_LOW_CREDITS:%d" % credits)
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
	_phase = Phase.EXPLORE
	_explore_round = 0

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
		_flags.append("EXPLORE_FEW_NODES:%d" % _visited.size())
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
					# Phase 1: Seen → Scanned
					if phase_str == "SEEN" and _bridge.has_method("DispatchScanDiscoveryV0"):
						_bridge.call("DispatchScanDiscoveryV0", did)
						await create_timer(0.05).timeout
						if _bridge.has_method("DebugAdvanceTicksV0"):
							_bridge.call("DebugAdvanceTicksV0", 5)
							await create_timer(0.05).timeout
					# Phase 2: Scanned → Analyzed
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

	# Gate 3: VOID_SURVEY — survey fracture derelicts at void sites
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
	# Find a node with an analyzed RUIN discovery — build extraction station there
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
				# Advance ticks to complete construction (4 steps × 30 ticks = 120)
				if _bridge.has_method("DebugAdvanceTicksV0"):
					_bridge.call("DebugAdvanceTicksV0", 150)
					await create_timer(0.1).timeout
				built_extraction = true

				# Set up trade charter from extraction node to haven
				var haven_status: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
				var haven_node: String = haven_status.get("node_id", "")
				if haven_node and _bridge.has_method("CreateTradeCharterProgram"):
					# Need market IDs — get from node
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
		_flags.append("HAVEN_NOT_FOUND")
	_busy = false
	_phase = Phase.UPGRADE

func _do_upgrade() -> void:
	_busy = true
	for tier_attempt in range(UPGRADE_MAX_TIER):
		var haven: Dictionary = _bridge.call("GetHavenStatusV0") if _bridge.has_method("GetHavenStatusV0") else {}
		var current_tier: int = int(haven.get("tier", 0))
		if current_tier >= UPGRADE_MAX_TIER:
			print(PREFIX + "UPGRADE|DONE|tier=%d" % current_tier)
			break
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
		print(PREFIX + "UPGRADE|CARGO|exotic=%d|comp=%d|elec=%d|rare=%d" % [
			cargo_map.get("exotic_matter", 0), cargo_map.get("composites", 0),
			cargo_map.get("electronics", 0), cargo_map.get("rare_metals", 0)
		])
		# If missing upgrade goods, travel to nodes that have them
		var needed: Array = []
		if cargo_map.get("composites", 0) < 20: needed.append("composites")
		if cargo_map.get("electronics", 0) < 30: needed.append("electronics")
		if cargo_map.get("rare_metals", 0) < 30: needed.append("rare_metals")
		if needed.size() > 0:
			for nid in _adj:
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
					if cargo_map.get("composites", 0) < 20: needed.append("composites")
					if cargo_map.get("electronics", 0) < 30: needed.append("electronics")
					if cargo_map.get("rare_metals", 0) < 30: needed.append("rare_metals")
					print(PREFIX + "UPGRADE|RESUPPLY|exotic=%d|comp=%d|elec=%d|rare=%d" % [
						cargo_map.get("exotic_matter", 0), cargo_map.get("composites", 0),
						cargo_map.get("electronics", 0), cargo_map.get("rare_metals", 0)
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
	for _attempt in range(RESEARCH_TARGET):
		var tree: Array = _bridge.call("GetTechTreeV0") if _bridge.has_method("GetTechTreeV0") else []
		var next_tech := ""
		for t in tree:
			if t is Dictionary and not bool(t.get("unlocked", false)):
				next_tech = t.get("tech_id", "")
				break
		if next_tech == "":
			print(PREFIX + "RESEARCH|NO_MORE_TECHS")
			break
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var node_id: String = ps.get("current_node_id", "")
		if _bridge.has_method("StartResearchV0"):
			_bridge.call("StartResearchV0", next_tech, node_id)
			await create_timer(0.05).timeout
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", RESEARCH_TICK_SKIP)
			await create_timer(0.1).timeout
		researched += 1
		print(PREFIX + "RESEARCH|tech=%s|count=%d" % [next_tech, researched])
	_busy = false
	_phase = Phase.EQUIP

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
	# Search ALL adjacency nodes (not just visited) — pirates spawn at frontier/rim nodes
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
				# Gate 4: PIRATE_LOOT — verify pirate loot contents
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
	var charters_created := 0
	var patrols_created := 0
	# Find two nodes with good arbitrage for a trade charter
	var best_source := ""
	var best_dest := ""
	var best_good := ""
	var best_spread := 0
	var visited_nodes: Array = _visited.keys()
	# Compare markets pairwise for price spread
	for i in range(visited_nodes.size()):
		var nid_a: String = visited_nodes[i]
		var market_a: Array = _bridge.call("GetPlayerMarketViewV0", nid_a) if _bridge.has_method("GetPlayerMarketViewV0") else []
		for j in range(visited_nodes.size()):
			if i == j:
				continue
			var nid_b: String = visited_nodes[j]
			var market_b: Array = _bridge.call("GetPlayerMarketViewV0", nid_b) if _bridge.has_method("GetPlayerMarketViewV0") else []
			# Build price map for market_b (sell prices)
			var sell_prices: Dictionary = {}
			for item in market_b:
				if item is Dictionary:
					var gid: String = item.get("good_id", "")
					var sp: int = int(item.get("sell_price", 0))
					if sp > 0:
						sell_prices[gid] = sp
			# Check buy prices in market_a vs sell prices in market_b
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
	# Create trade charter if we found a profitable route
	if best_source and best_dest and best_good and _bridge.has_method("CreateTradeCharterProgram"):
		_bridge.call("CreateTradeCharterProgram", best_source, best_dest, best_good, best_good, 10)
		charters_created += 1
		print(PREFIX + "AUTOMATION|CHARTER|%s>%s|good=%s|spread=%d" % [best_source, best_dest, best_good, best_spread])
		await create_timer(0.05).timeout
	else:
		print(PREFIX + "AUTOMATION|NO_CHARTER|best_spread=%d" % best_spread)
	# Create patrol program if we have a second fleet
	var roster: Array = _bridge.call("GetFleetRosterV0") if _bridge.has_method("GetFleetRosterV0") else []
	if roster.size() > 1 and _bridge.has_method("CreatePatrolProgramV0"):
		# Patrol around visited nodes
		var patrol_node: String = visited_nodes[0] if visited_nodes.size() > 0 else ""
		if patrol_node:
			_bridge.call("CreatePatrolProgramV0", patrol_node)
			patrols_created += 1
			print(PREFIX + "AUTOMATION|PATROL|node=%s" % patrol_node)
			await create_timer(0.05).timeout
	print(PREFIX + "AUTOMATION|DONE|charters=%d|patrols=%d" % [charters_created, patrols_created])
	_busy = false
	_phase = Phase.ENDGAME

func _do_endgame() -> void:
	_busy = true
	# Choose endgame path
	if _bridge.has_method("ChooseEndgamePathV0"):
		_bridge.call("ChooseEndgamePathV0", "reinforce")
		print(PREFIX + "ENDGAME|PATH_CHOSEN|reinforce")
		await create_timer(0.05).timeout

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

	for cycle in range(ENDGAME_TRADE_CYCLES):
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

		# Gate 2: MISSION_TARGET — navigate to mission objectives
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
			# Buy target good if specified
			if target_good and cur_loc:
				_bridge.call("DispatchPlayerTradeV0", cur_loc, target_good, 10, true)
				await create_timer(0.02).timeout
				print(PREFIX + "ENDGAME|MISSION_BUY|%s|node=%s" % [target_good, cur_loc])
			# Advance ticks to let MissionSystem evaluate triggers
			if _bridge.has_method("DebugAdvanceTicksV0"):
				_bridge.call("DebugAdvanceTicksV0", 20)
				await create_timer(0.05).timeout
			# Check if mission completed
			var post_m: Dictionary = _bridge.call("GetActiveMissionV0") if _bridge.has_method("GetActiveMissionV0") else {}
			if post_m == null or post_m.get("mission_id", "") == "":
				print(PREFIX + "ENDGAME|MISSION_COMPLETE")

		# Diplomatic proposals — propose treaties and accept incoming proposals
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
		var market: Array = _bridge.call("GetPlayerMarketViewV0", loc) if loc else []
		for item in market:
			if item is Dictionary and int(item.get("quantity", 0)) > 0:
				var gid: String = item.get("good_id", "")
				_bridge.call("DispatchPlayerTradeV0", loc, gid, 5, true)
				await create_timer(0.02).timeout
				_bridge.call("DispatchPlayerTradeV0", loc, gid, 5, false)
				await create_timer(0.02).timeout

		# Collect any available fragments
		var frags: Array = _bridge.call("GetAdaptationFragmentsV0") if _bridge.has_method("GetAdaptationFragmentsV0") else []
		for frag in frags:
			if frag is Dictionary and not bool(frag.get("collected", false)):
				var fid: String = frag.get("fragment_id", "")
				if fid and _bridge.has_method("CollectFragmentV0"):
					_bridge.call("CollectFragmentV0", fid)
					print(PREFIX + "ENDGAME|FRAGMENT|%s" % fid)

		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", ENDGAME_TICK_SKIP)
			await create_timer(0.05).timeout
		if cycle % 10 == 0:
			var rep_c: Dictionary = _bridge.call("GetPlayerReputationV0", "concord") if _bridge.has_method("GetPlayerReputationV0") else {}
			var rep_w: Dictionary = _bridge.call("GetPlayerReputationV0", "weavers") if _bridge.has_method("GetPlayerReputationV0") else {}
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
				_flags.append("GAME_OVER_NOT_VICTORY:%s" % outcome)
				_phase = Phase.DONE
				_finish()
				return
		if _bridge.has_method("DebugAdvanceTicksV0"):
			_bridge.call("DebugAdvanceTicksV0", 50)
			await create_timer(0.05).timeout
	print(PREFIX + "VICTORY|TIMEOUT|not_reached")
	_flags.append("VICTORY_TIMEOUT")
	_phase = Phase.DONE
	_finish()

func _finish() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: int = int(ps.get("credits", 0))
	var profit: int = credits - _start_credits
	var flag_str: String = "|".join(_flags) if _flags.size() > 0 else "NONE"
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
