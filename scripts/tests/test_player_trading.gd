extends SceneTree

const Sim = preload("res://scripts/core/sim/sim.gd")
const PlayerState = preload("res://scripts/core/state/player_state.gd")
const PlayerInteractionManager = preload("res://scripts/core/player_interaction_manager.gd")
const Fleet = preload("res://scripts/core/state/fleet.gd")

const PLAYER_FLEET_ID := "player_1"
const GOOD_ID := "fuel"
const QTY := 10

# Deterministic, line-oriented transcript builder.
var _lines: Array[String] = []

func _t(line: String) -> void:
	_lines.append(line)

func _dump_and_exit(code: int) -> void:
	for line in _lines:
		print(line)
	quit(code)

func _get_credits_safe(player: Object) -> int:
	# Determinism: do not branch on reflection results beyond existence; default stable 0.
	# Godot Object.get("prop") returns null if missing.
	var v = player.get("credits")
	if v == null:
		return 0
	return int(v)

func _initialize():
	_t("[TRADELOOP] begin")
	_t("[TRADELOOP] init_sim")
	var sim = Sim.new()
	var player = PlayerState.new()
	player.current_node_id = sim.galaxy_map.stars[0].id

	var manager = PlayerInteractionManager.new(player, sim)

	# Require at least 2 stations for a loop.
	if sim.galaxy_map.stars.size() < 2:
		_t("[FAIL] requires >=2 stars")
		_dump_and_exit(1)
		return

	var start_id: String = sim.galaxy_map.stars[0].id
	var dest_id: String = sim.galaxy_map.stars[1].id

	# Spawn a player fleet at the starter hub (mirrors game_manager.gd behavior).
	_t("[TRADELOOP] spawn_fleet id=" + PLAYER_FLEET_ID + " at=" + start_id)
	var p_fleet = Fleet.new(PLAYER_FLEET_ID, sim.galaxy_map.stars[0].pos)
	p_fleet.speed = 30.0
	sim.active_fleets.append(p_fleet)

	# Configure markets deterministically so trades can succeed.
	# We do not assume pricing internals; we just ensure inventory/demand are present.
	_t("[TRADELOOP] setup_markets start=" + start_id + " dest=" + dest_id)
	if not sim.active_markets.has(start_id) or not sim.active_markets.has(dest_id):
		_t("[FAIL] missing market(s) for required stars")
		_dump_and_exit(1)
		return

	var m_start = sim.active_markets[start_id]
	var m_dest = sim.active_markets[dest_id]

	# Ensure supply at start.
	m_start.inventory[GOOD_ID] = 100
	m_start.base_demand[GOOD_ID] = 10

	# Ensure demand at destination (helps profit signaling if sim uses demand-driven prices).
	m_dest.inventory[GOOD_ID] = 0
	m_dest.base_demand[GOOD_ID] = 200

	var credits_before := _get_credits_safe(player)
	_t("[TRADELOOP] credits_before=" + str(credits_before))

	# STEP 1: BUY
	_t("[TRADELOOP] step=1 action=buy market=" + start_id + " good=" + GOOD_ID + " qty=" + str(QTY))
	var buy_ok = manager.try_trade(start_id, GOOD_ID, QTY, true)
	if not buy_ok:
		_t("[FAIL] buy rejected")
		_dump_and_exit(1)
		return
	if int(player.cargo.get(GOOD_ID, 0)) != QTY:
		_t("[FAIL] cargo_after_buy expected=" + str(QTY) + " got=" + str(int(player.cargo.get(GOOD_ID, 0))))
		_dump_and_exit(1)
		return
	_t("[TRADELOOP] step=1 result=ok cargo_" + GOOD_ID + "=" + str(int(player.cargo.get(GOOD_ID, 0))))

	# STEP 2: SHIP (logistics)
	_t("[TRADELOOP] step=2 action=ship dest=" + dest_id)
	sim.command_fleet_move(PLAYER_FLEET_ID, dest_id)

	# Advance deterministically until arrival or hard cap.
	# Cap is fixed to keep runtime stable and deterministic.
	var arrived := false
	var max_ticks := 2000
	var tick := 0
	while tick < max_ticks:
		sim.advance()
		tick += 1

		# Deterministically update player.current_node_id using the same rule as game_manager.gd.
		var fleets = sim.active_fleets.filter(func(f): return f.id == PLAYER_FLEET_ID)
		if fleets.is_empty():
			_t("[FAIL] player fleet missing during travel")
			_dump_and_exit(1)
			return

		var f = fleets[0]
		if f.path.is_empty():
			var node = sim._get_star_at_pos(f.current_pos)
			if node and String(node.id) == dest_id:
				player.current_node_id = dest_id
				arrived = true
				break

	if not arrived:
		_t("[FAIL] did_not_arrive ticks=" + str(tick) + " cap=" + str(max_ticks))
		_dump_and_exit(1)
		return

	_t("[TRADELOOP] step=2 result=ok arrived_tick=" + str(tick))

	# STEP 3: SELL
	_t("[TRADELOOP] step=3 action=sell market=" + dest_id + " good=" + GOOD_ID + " qty=" + str(QTY))
	var sell_ok = manager.try_trade(dest_id, GOOD_ID, QTY, false)
	if not sell_ok:
		_t("[FAIL] sell rejected")
		_dump_and_exit(1)
		return
	if int(player.cargo.get(GOOD_ID, 0)) != 0:
		_t("[FAIL] cargo_after_sell expected=0 got=" + str(int(player.cargo.get(GOOD_ID, 0))))
		_dump_and_exit(1)
		return

	var credits_after := _get_credits_safe(player)
	var delta := credits_after - credits_before
	_t("[TRADELOOP] credits_after=" + str(credits_after))
	_t("[TRADELOOP] profit_delta=" + str(delta))

	# If credits are not modeled in PlayerState yet, delta will be 0, but transcript remains deterministic.
	_t("[TRADELOOP] end ok")
	_dump_and_exit(0)
