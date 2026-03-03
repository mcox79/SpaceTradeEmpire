extends SceneTree

# Headless proof for GATE.S1.HERO_SHIP_LOOP.PLAYER_TRADE.001
# Emits exactly one deterministic line:
# HST|credits_before=N|credits_after=M|buy_reduced_credits=<bool>|market_good_count=N

func _initialize():
	var packed = load("res://scenes/playable_prototype.tscn")
	var root = packed.instantiate()
	root.name = "Main"
	get_root().add_child(root)

	# Let _ready run deterministically
	await process_frame
	await process_frame

	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		print("HST|FAIL|no_bridge")
		quit()
		return

	# Get initial player state
	var ps = bridge.call("GetPlayerStateV0")
	var credits_before := int(ps.get("credits", 0))
	var current_node_id: String = ps.get("current_node_id", "")

	# Find a market node: try player's current node first, then scan all galaxy nodes
	var market_view = bridge.call("GetPlayerMarketViewV0", current_node_id)
	var market_node_id := current_node_id

	if typeof(market_view) != TYPE_ARRAY or market_view.size() == 0:
		var galaxy_snap = bridge.call("GetGalaxySnapshotV0")
		if typeof(galaxy_snap) == TYPE_DICTIONARY and galaxy_snap.has("system_nodes"):
			for nd in galaxy_snap["system_nodes"]:
				if typeof(nd) == TYPE_DICTIONARY:
					var nid: String = nd.get("node_id", "")
					if nid.is_empty():
						continue
					var mv = bridge.call("GetPlayerMarketViewV0", nid)
					if typeof(mv) == TYPE_ARRAY and mv.size() > 0:
						market_view = mv
						market_node_id = nid
						break

	var market_good_count := 0
	if typeof(market_view) == TYPE_ARRAY:
		market_good_count = market_view.size()

	var credits_after := credits_before
	var buy_reduced_credits := false

	if market_good_count > 0:
		var first_good = market_view[0]
		if typeof(first_good) == TYPE_DICTIONARY:
			var good_id: String = str(first_good.get("good_id", ""))
			var buy_price: int = int(first_good.get("buy_price", 0))
			var qty_in_market: int = int(first_good.get("quantity", 0))

			if credits_before >= buy_price and qty_in_market > 0 and not good_id.is_empty():
				# Read tick before dispatch so we can poll for the command to be processed.
				var tick_before: int = bridge.call("GetSimTickV0")
				bridge.call("DispatchPlayerTradeV0", market_node_id, good_id, 1, true)

				# Poll until the sim advances at least 2 ticks beyond tick_before.
				# This guarantees the command entered the queue and was executed.
				# (TickDelayMs=100ms; 500 frames at ~16ms/frame = 8s ceiling)
				for _i in range(500):
					await process_frame
					var t: int = bridge.call("GetSimTickV0")
					if t >= 0 and t >= tick_before + 2:
						break

				var ps2 = bridge.call("GetPlayerStateV0")
				credits_after = int(ps2.get("credits", 0))
				buy_reduced_credits = (credits_after < credits_before)

	print("HST|credits_before=%d|credits_after=%d|buy_reduced_credits=%s|market_good_count=%d" % [
		credits_before,
		credits_after,
		str(buy_reduced_credits).to_lower(),
		market_good_count
	])

	if bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit()
