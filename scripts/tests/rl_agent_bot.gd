extends SceneTree

## RL Agent Bot — Godot-side RL training agent
## Communicates with Python trainer via TCP socket.
## Tests the FULL Godot stack: SimBridge threading, C# bridge, scene tree.
##
## Usage: godot --headless --path . -s res://scripts/tests/rl_agent_bot.gd -- --port 11008
##
## Protocol (JSON lines over TCP):
##   Python → Godot: {"type":"reset","seed":42} or {"type":"step","action":7} or {"type":"shutdown"}
##   Godot → Python: {"type":"reset_ok","obs":[...],...} or {"type":"step_ok","obs":[...],...}

const PREFIX := "RLAG|"
const MAX_POLLS := 600
const MAX_BUY_QTY := 10
const WAIT_TICKS := 5

# 13 goods in fixed order (must match Python side)
const GOOD_ORDER := [
	"fuel", "ore", "organics", "rare_metals",
	"metal", "food", "composites", "electronics",
	"munitions", "components",
	"exotic_crystals", "salvaged_tech", "exotic_matter"
]
const GOOD_COUNT := 13
const MAX_NEIGHBORS := 6
const OBS_SIZE := 232  # 137 base + 95 expanded (mission+haven+fleet+faction+tech+fragment+endgame+risk+discovery)
const NUM_ACTIONS := 82

const FACTION_IDS := ["concord", "chitin", "valorin", "weavers", "communion"]

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	SERVE_LOOP,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null
var _port := 11008

# TCP server
var _tcp_server: TCPServer = null
var _peer: StreamPeerTCP = null

# Sim state cache
var _adj: Dictionary = {}
var _all_nodes: Array = []
var _neighbor_cache: Array = []
var _max_episode_ticks := 2000
var _prev_credits := 0
var _total_profit := 0

# Reward tracking for new action categories
var _prev_completed_missions := 0
var _prev_haven_tier := 0

# Player fleet ID
const PLAYER_FLEET := "fleet_trader_1"


func _initialize() -> void:
	_parse_args()
	print(PREFIX + "START|port=%d" % _port)

	# Start TCP server
	_tcp_server = TCPServer.new()
	var err = _tcp_server.listen(_port, "127.0.0.1")
	if err != OK:
		print(PREFIX + "FATAL|cannot_listen port=%d err=%d" % [_port, err])
		quit(1)
		return
	print(PREFIX + "LISTENING|port=%d" % _port)


func _parse_args() -> void:
	var args = OS.get_cmdline_user_args()
	for i in range(args.size()):
		if args[i] == "--port" and i + 1 < args.size():
			_port = int(args[i + 1])


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_phase = Phase.WAIT_READY
				_polls = 0
			else:
				_polls += 1
				if _polls > MAX_POLLS:
					print(PREFIX + "FATAL|bridge_not_found")
					quit(1)

		Phase.WAIT_READY:
			if _bridge.has_method("GetBridgeReadyV0") and bool(_bridge.call("GetBridgeReadyV0")):
				print(PREFIX + "BRIDGE_READY")
				_phase = Phase.SERVE_LOOP
			else:
				_polls += 1
				if _polls > MAX_POLLS:
					print(PREFIX + "FATAL|bridge_not_ready")
					quit(1)

		Phase.SERVE_LOOP:
			_serve_tick()

		Phase.DONE:
			pass

	return false  # keep running


func _serve_tick() -> void:
	# Accept new connection if none
	if _peer == null or _peer.get_status() != StreamPeerTCP.STATUS_CONNECTED:
		if _tcp_server.is_connection_available():
			_peer = _tcp_server.take_connection()
			print(PREFIX + "CLIENT_CONNECTED")
		return

	# Poll for data
	_peer.poll()
	if _peer.get_available_bytes() == 0:
		return

	# Read all available data and process line by line
	var data := _peer.get_utf8_string(_peer.get_available_bytes())
	var lines := data.split("\n", false)
	for line in lines:
		if line.strip_edges().is_empty():
			continue
		_handle_message(line.strip_edges())


func _handle_message(line: String) -> void:
	var parsed = JSON.parse_string(line)
	if parsed == null or not parsed is Dictionary:
		_send_response({"type": "error", "error": "parse_failed"})
		return

	var req: Dictionary = parsed
	var msg_type: String = req.get("type", "")

	match msg_type:
		"reset":
			_handle_reset(req)
		"step":
			_handle_step(req)
		"observe":
			_handle_observe()
		"shutdown":
			print(PREFIX + "SHUTDOWN")
			if _bridge and _bridge.has_method("StopSimV0"):
				_bridge.call("StopSimV0")
			_phase = Phase.DONE
			quit(0)
		_:
			_send_response({"type": "error", "error": "unknown_type:" + msg_type})


func _handle_reset(req: Dictionary) -> void:
	# The Godot version can't easily re-seed the sim since SimBridge
	# manages the kernel lifecycle. We wait for the existing sim and
	# treat each reset as "start from current state at tick 0 of episode".
	# For true reseeding, use the headless C# server.

	_max_episode_ticks = int(req.get("max_episode_ticks", 2000))

	# Build adjacency from galaxy snapshot
	_build_adjacency()

	# Get current location and cache neighbors
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	_neighbor_cache = _get_neighbors(loc)
	_prev_credits = int(ps.get("credits", 0))
	_total_profit = 0

	var obs := _encode_obs()
	var mask := _compute_action_mask()

	_send_response({
		"type": "reset_ok",
		"obs": obs,
		"action_mask": mask,
		"info": {
			"node_count": _all_nodes.size(),
			"tick": _bridge.call("GetSimTickV0") if _bridge.has_method("GetSimTickV0") else 0,
		}
	})


func _handle_step(req: Dictionary) -> void:
	var action: int = int(req.get("action", 0))
	var credits_before: int = int(_bridge.call("GetPlayerStateV0").get("credits", 0))

	var result := _execute_action(action)

	# Get state after action
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_after: int = int(ps.get("credits", 0))
	var loc: String = ps.get("current_node_id", "")
	_neighbor_cache = _get_neighbors(loc)

	# Compute reward
	var reward := 0.0
	var credit_delta := (credits_after - credits_before) / 1000.0
	reward += clampf(credit_delta, -1.0, 1.0)

	if result.get("trade_profit", 0) > 0:
		reward += 0.1  # trade completion bonus
	if result.get("new_node", false):
		reward += 0.2  # exploration bonus
	if result.get("mission_accepted", false):
		reward += 0.1
	if result.get("mission_completed", false):
		reward += 0.5
	if result.get("haven_upgraded", false):
		reward += 1.0
	if result.get("research_started", false):
		reward += 0.1
	if result.get("module_installed", false):
		reward += 0.2
	if result.get("fragment_deposited", false):
		reward += 0.5
	if result.get("endgame_chosen", false):
		reward += 1.0
	if result.get("construction_started", false):
		reward += 0.3
	if result.get("megaproject_started", false):
		reward += 0.5
	reward -= 0.01  # time penalty

	# Check termination
	var game_result: Dictionary = _bridge.call("GetGameResultV0") if _bridge.has_method("GetGameResultV0") else {}
	var game_over: bool = game_result.get("game_over", false) if game_result else false
	var terminated := game_over

	if terminated:
		var outcome: String = game_result.get("outcome", "")
		if outcome == "death":
			reward -= 10.0
		elif outcome == "bankruptcy":
			reward -= 5.0
		elif outcome == "victory":
			reward += 20.0

	var tick: int = _bridge.call("GetSimTickV0") if _bridge.has_method("GetSimTickV0") else 0
	var truncated := tick >= _max_episode_ticks

	_total_profit += credits_after - _prev_credits
	_prev_credits = credits_after

	var obs := _encode_obs()
	var mask := _compute_action_mask()

	_send_response({
		"type": "step_ok",
		"obs": obs,
		"reward": reward,
		"terminated": terminated,
		"truncated": truncated,
		"action_mask": mask,
		"info": {
			"tick": tick,
			"credits": credits_after,
			"total_profit": _total_profit,
			"action_label": result.get("label", "unknown"),
		}
	})


func _handle_observe() -> void:
	var obs := _encode_obs()
	var mask := _compute_action_mask()
	_send_response({"type": "observe_ok", "obs": obs, "action_mask": mask})


# ── Observation Encoding ──

func _encode_obs() -> Array:
	var obs: Array = []
	obs.resize(OBS_SIZE)
	obs.fill(0.0)
	var idx := 0

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits: float = float(ps.get("credits", 0))
	var cargo_count: float = float(ps.get("cargo_count", 0))
	var cargo_cap: float = float(ps.get("cargo_capacity", 50))
	var fuel: float = float(ps.get("fuel", 0))
	var fuel_cap: float = float(ps.get("fuel_capacity", 200))
	var loc: String = ps.get("current_node_id", "")

	# Player fleet HP
	var hull := 1.0
	var shield := 0.0
	var fleet_hp: Dictionary = _bridge.call("GetFleetCombatHpV0", PLAYER_FLEET) if _bridge.has_method("GetFleetCombatHpV0") else {}
	if fleet_hp and fleet_hp.has("hull_max") and int(fleet_hp.get("hull_max", 0)) > 0:
		hull = clampf(float(fleet_hp.get("hull", 0)) / float(fleet_hp.get("hull_max", 1)), 0.0, 1.0)
	if fleet_hp and fleet_hp.has("shield_max") and int(fleet_hp.get("shield_max", 0)) > 0:
		shield = clampf(float(fleet_hp.get("shield", 0)) / float(fleet_hp.get("shield_max", 1)), 0.0, 1.0)

	var tick: int = _bridge.call("GetSimTickV0") if _bridge.has_method("GetSimTickV0") else 0
	var visited_count := 0
	var exploration: Dictionary = _bridge.call("GetExplorationOverlayV0") if _bridge.has_method("GetExplorationOverlayV0") else {}
	if exploration.has("visited_count"):
		visited_count = int(exploration.get("visited_count", 0))

	# [0-6] Player state
	obs[idx] = clampf(credits / 10000.0, 0.0, 5.0); idx += 1
	obs[idx] = clampf(cargo_count / maxf(cargo_cap, 1.0), 0.0, 1.0); idx += 1
	obs[idx] = hull; idx += 1
	obs[idx] = shield; idx += 1
	obs[idx] = clampf(fuel / maxf(fuel_cap, 1.0), 0.0, 1.0); idx += 1
	obs[idx] = clampf(float(tick) / maxf(float(_max_episode_ticks), 1.0), 0.0, 1.0); idx += 1
	obs[idx] = clampf(float(visited_count) / maxf(float(_all_nodes.size()), 1.0), 0.0, 1.0); idx += 1

	# [7-45] Current market prices (13 goods × 3)
	var market: Array = _bridge.call("GetPlayerMarketViewV0", loc) if loc else []
	var market_by_good: Dictionary = {}
	for item in market:
		if item is Dictionary:
			market_by_good[item.get("good_id", "")] = item

	for g in range(GOOD_COUNT):
		var good_id: String = GOOD_ORDER[g]
		if market_by_good.has(good_id):
			var m: Dictionary = market_by_good[good_id]
			obs[idx] = clampf(float(m.get("quantity", 0)) / 50.0, 0.0, 4.0); idx += 1
			obs[idx] = clampf(float(m.get("buy_price", 0)) / 200.0, 0.0, 3.0); idx += 1
			obs[idx] = clampf(float(m.get("sell_price", 0)) / 200.0, 0.0, 3.0); idx += 1
		else:
			idx += 3

	# [46-58] Player cargo (13 goods)
	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var cargo_by_good: Dictionary = {}
	for item in cargo:
		if item is Dictionary:
			cargo_by_good[item.get("good_id", "")] = int(item.get("qty", 0))

	for g in range(GOOD_COUNT):
		var good_id: String = GOOD_ORDER[g]
		var held: int = cargo_by_good.get(good_id, 0)
		obs[idx] = clampf(float(held) / maxf(cargo_cap, 1.0), 0.0, 1.0); idx += 1

	# [59-136] Neighbor sell prices (6 × 13)
	for n in range(MAX_NEIGHBORS):
		if n < _neighbor_cache.size():
			var n_market: Array = _bridge.call("GetPlayerMarketViewV0", _neighbor_cache[n])
			var n_by_good: Dictionary = {}
			for item in n_market:
				if item is Dictionary:
					n_by_good[item.get("good_id", "")] = item
			for g in range(GOOD_COUNT):
				var good_id: String = GOOD_ORDER[g]
				if n_by_good.has(good_id):
					obs[idx] = clampf(float(n_by_good[good_id].get("sell_price", 0)) / 200.0, 0.0, 3.0)
				idx += 1
		else:
			idx += GOOD_COUNT

	# ── Expanded observations (137+) ──

	# [137-146] Mission state (10 dims)
	idx = _encode_mission_state(obs, idx)

	# [147-161] Haven status (15 dims)
	idx = _encode_haven_status(obs, idx)

	# [162-181] Fleet roster (20 dims, 4 fleets × 5)
	idx = _encode_fleet_roster(obs, idx)

	# [182-186] Faction reputation (5 dims)
	idx = _encode_faction_rep(obs, idx)

	# [187-191] Tech tree (5 dims)
	idx = _encode_tech_tree(obs, idx)

	# [192-215] Fragment inventory (24 dims: 16 collected + 8 resonance)
	idx = _encode_fragments(obs, idx)

	# [216-223] Endgame progress (8 dims)
	idx = _encode_endgame(obs, idx)

	# [224-226] Risk meters (3 dims)
	idx = _encode_risk_meters(obs, idx)

	# [227-231] Discovery (5 dims)
	idx = _encode_discovery(obs, idx)

	return obs


func _bcall(method: String, fallback = null):
	if _bridge.has_method(method):
		return _bridge.call(method)
	return fallback


func _bcall1(method: String, arg, fallback = null):
	if _bridge.has_method(method):
		return _bridge.call(method, arg)
	return fallback


func _encode_mission_state(obs: Array, idx: int) -> int:
	var mission: Dictionary = _bcall("GetActiveMissionV0", {})
	if mission == null: mission = {}
	var has_active := 1.0 if mission.get("mission_id", "") != "" else 0.0
	var step_ratio := 0.0
	if has_active > 0.0:
		var total: float = float(mission.get("total_steps", 1))
		step_ratio = clampf(float(mission.get("current_step", 0)) / maxf(total, 1.0), 0.0, 1.0)
	obs[idx] = has_active; idx += 1
	obs[idx] = step_ratio; idx += 1
	# mission type one-hot (3 slots: story=0, systemic=1, contextual=2)
	idx += 3  # leave as 0s for now — type not easily derivable
	var offers: Array = _bcall("GetSystemicOffersV0", [])
	if offers == null: offers = []
	obs[idx] = clampf(float(offers.size()) / 10.0, 0.0, 1.0); idx += 1
	var missions: Array = _bcall("GetMissionListV0", [])
	if missions == null: missions = []
	obs[idx] = clampf(float(missions.size()) / 10.0, 0.0, 1.0); idx += 1
	idx += 3  # padding to 10
	return idx


func _encode_haven_status(obs: Array, idx: int) -> int:
	var haven: Dictionary = _bcall("GetHavenStatusV0", {})
	if haven == null: haven = {}
	obs[idx] = 1.0 if bool(haven.get("discovered", false)) else 0.0; idx += 1
	obs[idx] = clampf(float(haven.get("tier", 0)) / 5.0, 0.0, 1.0); idx += 1
	obs[idx] = clampf(float(haven.get("upgrade_ticks_remaining", 0)) / 200.0, 0.0, 1.0); idx += 1
	var stored: Array = haven.get("stored_ship_ids", [])
	if stored == null: stored = []
	obs[idx] = clampf(float(stored.size()) / 3.0, 0.0, 1.0); idx += 1
	obs[idx] = clampf(float(haven.get("installed_fragment_count", 0)) / 16.0, 0.0, 1.0); idx += 1
	# Fabricator
	var fab: Dictionary = _bcall("GetFabricatorV0", {})
	if fab == null: fab = {}
	obs[idx] = 1.0 if bool(fab.get("available", false)) else 0.0; idx += 1
	obs[idx] = 1.0 if fab.get("fabricating_module", "") != "" else 0.0; idx += 1
	obs[idx] = clampf(float(fab.get("ticks_remaining", 0)) / 100.0, 0.0, 1.0); idx += 1
	# Research
	var research: Dictionary = _bcall("GetResearchStatusV0", {})
	if research == null: research = {}
	obs[idx] = 1.0 if research.get("active_tech", "") != "" else 0.0; idx += 1
	var prog: float = float(research.get("progress_ticks", 0))
	var total: float = float(research.get("total_ticks", 1))
	obs[idx] = clampf(prog / maxf(total, 1.0), 0.0, 1.0); idx += 1
	var tech_tier: int = _bcall("GetTechTierV0", 0)
	if tech_tier == null: tech_tier = 0
	obs[idx] = clampf(float(tech_tier) / 5.0, 0.0, 1.0); idx += 1
	idx += 4  # padding to 15
	return idx


func _encode_fleet_roster(obs: Array, idx: int) -> int:
	var roster: Array = _bcall("GetFleetRosterV0", [])
	if roster == null: roster = []
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var player_loc: String = ps.get("current_node_id", "")
	for i in range(4):
		if i < roster.size() and roster[i] is Dictionary:
			var f: Dictionary = roster[i]
			var fid: String = f.get("fleet_id", "")
			var hp: Dictionary = _bcall1("GetFleetCombatHpV0", fid, {})
			if hp == null: hp = {}
			var hull_ratio := 1.0
			if hp.has("hull_max") and int(hp.get("hull_max", 0)) > 0:
				hull_ratio = clampf(float(hp.get("hull", 0)) / float(hp.get("hull_max", 1)), 0.0, 1.0)
			obs[idx] = hull_ratio; idx += 1
			var cargo_r := clampf(float(f.get("cargo_count", 0)) / maxf(float(f.get("cargo_capacity", 50)), 1.0), 0.0, 1.0)
			obs[idx] = cargo_r; idx += 1
			obs[idx] = 1.0 if fid == PLAYER_FLEET else 0.0; idx += 1
			obs[idx] = 1.0 if f.get("node_id", "") == player_loc else 0.0; idx += 1
			obs[idx] = 1.0 if f.get("has_job", false) else 0.0; idx += 1
		else:
			idx += 5
	return idx


func _encode_faction_rep(obs: Array, idx: int) -> int:
	for fid in FACTION_IDS:
		var rep: Dictionary = _bcall1("GetPlayerReputationV0", fid, {})
		if rep == null: rep = {}
		obs[idx] = clampf(float(rep.get("reputation", 0)) / 100.0, -1.0, 1.0); idx += 1
	return idx


func _encode_tech_tree(obs: Array, idx: int) -> int:
	var tier: int = _bcall("GetTechTierV0", 0)
	if tier == null: tier = 0
	obs[idx] = clampf(float(tier) / 5.0, 0.0, 1.0); idx += 1
	var tree: Array = _bcall("GetTechTreeV0", [])
	if tree == null: tree = []
	var unlocked := 0
	for t in tree:
		if t is Dictionary and bool(t.get("unlocked", false)):
			unlocked += 1
	obs[idx] = clampf(float(unlocked) / maxf(float(tree.size()), 1.0), 0.0, 1.0); idx += 1
	idx += 3  # padding to 5
	return idx


func _encode_fragments(obs: Array, idx: int) -> int:
	var fragments: Array = _bcall("GetAdaptationFragmentsV0", [])
	if fragments == null: fragments = []
	# 16 collected flags
	var collected_set: Dictionary = {}
	for frag in fragments:
		if frag is Dictionary and bool(frag.get("collected", false)):
			collected_set[frag.get("fragment_id", "")] = true
	for i in range(16):
		var frag_id := "frag_%d" % i
		obs[idx] = 1.0 if collected_set.has(frag_id) else 0.0; idx += 1
	# 8 resonance pair flags
	var pairs: Array = _bcall("GetResonancePairsV0", [])
	if pairs == null: pairs = []
	for i in range(8):
		if i < pairs.size() and pairs[i] is Dictionary:
			obs[idx] = 1.0 if bool(pairs[i].get("complete", false)) else 0.0
		idx += 1
	return idx


func _encode_endgame(obs: Array, idx: int) -> int:
	var progress: Dictionary = _bcall("GetEndgameProgressV0", {})
	if progress == null: progress = {}
	var path: String = progress.get("chosen_path", "")
	obs[idx] = 1.0 if path != "" else 0.0; idx += 1
	# path one-hot: reinforce=0, naturalize=1, renegotiate=2
	obs[idx] = 1.0 if path == "reinforce" else 0.0; idx += 1
	obs[idx] = 1.0 if path == "naturalize" else 0.0; idx += 1
	obs[idx] = 1.0 if path == "renegotiate" else 0.0; idx += 1
	obs[idx] = clampf(float(progress.get("completion_percent", 0)) / 100.0, 0.0, 1.0); idx += 1
	var game_result: Dictionary = _bcall("GetGameResultV0", {})
	if game_result == null: game_result = {}
	obs[idx] = 1.0 if bool(game_result.get("game_over", false)) else 0.0; idx += 1
	var outcome: String = game_result.get("outcome", "")
	obs[idx] = 1.0 if outcome == "victory" else 0.0; idx += 1
	obs[idx] = 1.0 if outcome == "death" else 0.0; idx += 1
	return idx


func _encode_risk_meters(obs: Array, idx: int) -> int:
	var risk: Dictionary = _bcall("GetRiskMetersV0", {})
	if risk == null: risk = {}
	obs[idx] = clampf(float(risk.get("heat", 0)) / 100.0, 0.0, 1.0); idx += 1
	obs[idx] = clampf(float(risk.get("influence", 0)) / 100.0, 0.0, 1.0); idx += 1
	obs[idx] = clampf(float(risk.get("trace", 0)) / 100.0, 0.0, 1.0); idx += 1
	return idx


func _encode_discovery(obs: Array, idx: int) -> int:
	var charges: Dictionary = _bcall("GetScanChargesV0", {})
	if charges == null: charges = {}
	var remaining: float = float(charges.get("charges_remaining", 0))
	var max_charges: float = float(charges.get("max_charges", 5))
	obs[idx] = clampf(remaining / maxf(max_charges, 1.0), 0.0, 1.0); idx += 1
	var chains: Array = _bcall("GetActiveChainsV0", [])
	if chains == null: chains = []
	obs[idx] = clampf(float(chains.size()) / 5.0, 0.0, 1.0); idx += 1
	var expl: Dictionary = _bcall("GetExplorationOverlayV0", {})
	if expl == null: expl = {}
	var visited: float = float(expl.get("visited_count", 0))
	obs[idx] = clampf(visited / maxf(float(_all_nodes.size()), 1.0), 0.0, 1.0); idx += 1
	idx += 2  # padding to 5
	return idx


# ── Action Mask ──

func _compute_action_mask() -> Array:
	var mask: Array = []
	mask.resize(NUM_ACTIONS)
	mask.fill(false)

	mask[0] = true  # WAIT always valid

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")
	var credits: int = int(ps.get("credits", 0))
	var cargo_count: int = int(ps.get("cargo_count", 0))
	var cargo_cap: int = int(ps.get("cargo_capacity", 50))
	var ship_state: String = ps.get("ship_state_token", "idle")
	var is_idle: bool = ship_state == "idle" or ship_state == "Idle"

	var market: Array = _bridge.call("GetPlayerMarketViewV0", loc) if loc else []
	var market_by_good: Dictionary = {}
	for item in market:
		if item is Dictionary:
			market_by_good[item.get("good_id", "")] = item

	var cargo: Array = _bridge.call("GetPlayerCargoV0")
	var cargo_by_good: Dictionary = {}
	for item in cargo:
		if item is Dictionary:
			cargo_by_good[item.get("good_id", "")] = int(item.get("qty", 0))

	# BUY (1-13)
	for g in range(GOOD_COUNT):
		var good_id: String = GOOD_ORDER[g]
		var can_buy: bool = is_idle and market_by_good.has(good_id) \
			and int(market_by_good[good_id].get("quantity", 0)) > 0 \
			and credits > 0 and cargo_count < cargo_cap
		mask[1 + g] = can_buy

	# SELL (14-26)
	for g in range(GOOD_COUNT):
		var good_id: String = GOOD_ORDER[g]
		mask[14 + g] = is_idle and cargo_by_good.get(good_id, 0) > 0

	# TRAVEL (27-32)
	for n in range(MAX_NEIGHBORS):
		mask[27 + n] = is_idle and n < _neighbor_cache.size()

	# COMBAT (33)
	if is_idle:
		var fleets: Array = _bridge.call("GetFleetTransitFactsV0", loc) if _bridge.has_method("GetFleetTransitFactsV0") else []
		for f in fleets:
			if f is Dictionary and bool(f.get("is_hostile", false)):
				mask[33] = true
				break

	# MISSION (34-37)
	var has_active_mission: bool = false
	var active_m: Dictionary = _bcall("GetActiveMissionV0", {})
	if active_m and active_m.get("mission_id", "") != "":
		has_active_mission = true
	var mission_list: Array = _bcall("GetMissionListV0", [])
	if mission_list == null: mission_list = []
	mask[34] = is_idle and not has_active_mission and mission_list.size() > 0
	mask[35] = has_active_mission
	var systemic: Array = _bcall("GetSystemicOffersV0", [])
	if systemic == null: systemic = []
	mask[36] = is_idle and not has_active_mission and systemic.size() > 0
	var ctx_templates: Array = _bcall1("GetContextualTemplatesV0", loc, [])
	if ctx_templates == null: ctx_templates = []
	mask[37] = is_idle and not has_active_mission and ctx_templates.size() > 0

	# MODULE (38-47: install 0-4, remove 0-4)
	var avail_mods: Array = _bcall("GetAvailableModulesV0", [])
	if avail_mods == null: avail_mods = []
	var slots: Array = _bcall("GetPlayerFleetSlotsV0", [])
	if slots == null: slots = []
	for s in range(5):
		mask[38 + s] = is_idle and avail_mods.size() > 0 and s < slots.size()
		mask[43 + s] = is_idle and s < slots.size() and slots[s] is Dictionary and slots[s].get("installed_module_id", "") != ""

	# HAVEN (48-52)
	var haven: Dictionary = _bcall("GetHavenStatusV0", {})
	if haven == null: haven = {}
	var haven_discovered: bool = bool(haven.get("discovered", false))
	mask[48] = is_idle and haven_discovered and int(haven.get("tier", 0)) < 5 and int(haven.get("upgrade_ticks_remaining", 0)) <= 0
	var tech_tree: Array = _bcall("GetTechTreeV0", [])
	if tech_tree == null: tech_tree = []
	var has_unlockable_tech := false
	for t in tech_tree:
		if t is Dictionary and not bool(t.get("unlocked", false)):
			has_unlockable_tech = true
			break
	var research_st: Dictionary = _bcall("GetResearchStatusV0", {})
	if research_st == null: research_st = {}
	mask[49] = is_idle and has_unlockable_tech and research_st.get("active_tech", "") == ""
	var fab: Dictionary = _bcall("GetFabricatorV0", {})
	if fab == null: fab = {}
	mask[50] = is_idle and bool(fab.get("available", false)) and fab.get("fabricating_module", "") == ""
	mask[51] = false  # swap ship — complex, skip for now
	var fragments: Array = _bcall("GetAdaptationFragmentsV0", [])
	if fragments == null: fragments = []
	var has_depositable := false
	for frag in fragments:
		if frag is Dictionary and bool(frag.get("collected", false)) and not bool(frag.get("deposited", false)):
			has_depositable = true
			break
	mask[52] = is_idle and has_depositable and haven_discovered

	# FLEET (58-59)
	mask[58] = false  # capture — needs weak hostile, skip
	mask[59] = false  # set destination — complex

	# DIPLOMACY (64-68)
	var proposals: Array = _bcall("GetDiplomaticProposalsV0", [])
	if proposals == null: proposals = []
	mask[64] = is_idle  # propose treaty always available
	mask[65] = proposals.size() > 0
	mask[66] = proposals.size() > 0
	mask[67] = false  # collect fragment — needs proximity

	# EXPLORE (69-73)
	var disc_snap: Array = _bcall1("GetDiscoverySnapshotV0", loc, [])
	if disc_snap == null: disc_snap = []
	mask[69] = is_idle and disc_snap.size() > 0
	mask[70] = is_idle and disc_snap.size() > 0
	mask[71] = false  # fracture travel — complex
	mask[72] = is_idle and _bridge.has_method("OrbitalScanV0")
	mask[73] = is_idle and _bridge.has_method("LandingScanV0")

	# CONSTRUCTION (74-76)
	var constr_defs: Array = _bcall("GetAvailableConstructionDefsV0", [])
	if constr_defs == null: constr_defs = []
	mask[74] = is_idle and constr_defs.size() > 0
	mask[75] = false  # megaproject — complex
	mask[76] = false  # deliver supply

	# ENDGAME (79-81)
	var endgame: Dictionary = _bcall("GetEndgameProgressV0", {})
	if endgame == null: endgame = {}
	var can_choose: bool = int(haven.get("tier", 0)) >= 4 and endgame.get("chosen_path", "") == ""
	mask[79] = can_choose
	mask[80] = can_choose
	mask[81] = can_choose

	return mask


# ── Action Execution ──

func _execute_action(action: int) -> Dictionary:
	var result := {"label": "unknown"}
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var loc: String = ps.get("current_node_id", "")

	if action == 0:
		# WAIT — let sim tick naturally
		# In Godot mode, we wait for physics frames
		await create_timer(0.1).timeout
		result["label"] = "wait"
		return result

	if action >= 1 and action <= GOOD_COUNT:
		# BUY
		var good_id: String = GOOD_ORDER[action - 1]
		var credits_before: int = int(ps.get("credits", 0))
		_bridge.call("DispatchPlayerTradeV0", loc, good_id, MAX_BUY_QTY, true)
		await create_timer(0.05).timeout
		var credits_after: int = int(_bridge.call("GetPlayerStateV0").get("credits", 0))
		result["label"] = "buy_" + good_id
		result["trade_profit"] = credits_after - credits_before
		return result

	if action >= 14 and action <= 13 + GOOD_COUNT:
		# SELL
		var good_id: String = GOOD_ORDER[action - 14]
		var cargo: Array = _bridge.call("GetPlayerCargoV0")
		var held := 0
		for item in cargo:
			if item is Dictionary and item.get("good_id", "") == good_id:
				held = int(item.get("qty", 0))
				break
		if held > 0:
			var credits_before: int = int(ps.get("credits", 0))
			_bridge.call("DispatchPlayerTradeV0", loc, good_id, held, false)
			await create_timer(0.05).timeout
			var credits_after: int = int(_bridge.call("GetPlayerStateV0").get("credits", 0))
			result["label"] = "sell_" + good_id
			result["trade_profit"] = credits_after - credits_before
		else:
			result["label"] = "sell_noop"
		return result

	if action >= 27 and action <= 26 + MAX_NEIGHBORS:
		# TRAVEL
		var n_idx: int = action - 27
		if n_idx < _neighbor_cache.size():
			var target: String = _neighbor_cache[n_idx]
			_bridge.call("DispatchPlayerArriveV0", target)
			# Wait for arrival (poll ship state)
			var new_node := false
			for _i in range(50):
				await create_timer(0.05).timeout
				var new_ps: Dictionary = _bridge.call("GetPlayerStateV0")
				if new_ps.get("current_node_id", "") == target:
					new_node = true
					break
			result["label"] = "travel_" + target
			result["new_node"] = new_node
		else:
			result["label"] = "travel_invalid"
		return result

	if action == 33:
		# COMBAT
		var fleets: Array = _bridge.call("GetFleetTransitFactsV0", loc)
		var hostile_id := ""
		for f in fleets:
			if f is Dictionary and bool(f.get("is_hostile", false)):
				hostile_id = str(f.get("fleet_id", ""))
				break
		if hostile_id:
			var combat_result: Dictionary = _bridge.call("ResolveCombatV0", PLAYER_FLEET, hostile_id)
			_bridge.call("DispatchClearCombatV0")
			result["label"] = "combat_" + hostile_id
		else:
			result["label"] = "combat_noop"
		return result

	# ── MISSION (34-37) ──
	if action == 34:
		var missions: Array = _bcall("GetMissionListV0", [])
		if missions and missions.size() > 0 and missions[0] is Dictionary:
			var mid: String = missions[0].get("mission_id", "")
			if mid and _bridge.has_method("AcceptMissionV0"):
				_bridge.call("AcceptMissionV0", mid)
				await create_timer(0.05).timeout
				result["label"] = "accept_mission_" + mid
				return result
		result["label"] = "accept_mission_noop"
		return result

	if action == 35:
		if _bridge.has_method("AbandonMissionV0"):
			_bridge.call("AbandonMissionV0")
		await create_timer(0.05).timeout
		result["label"] = "abandon_mission"
		return result

	if action == 36:
		var offers: Array = _bcall("GetSystemicOffersV0", [])
		if offers and offers.size() > 0 and offers[0] is Dictionary:
			var oid: String = offers[0].get("offer_id", "")
			if oid and _bridge.has_method("AcceptSystemicMissionV0"):
				_bridge.call("AcceptSystemicMissionV0", oid)
				await create_timer(0.05).timeout
				result["label"] = "accept_systemic_" + oid
				return result
		result["label"] = "accept_systemic_noop"
		return result

	if action == 37:
		var templates: Array = _bcall1("GetContextualTemplatesV0", loc, [])
		if templates and templates.size() > 0 and templates[0] is Dictionary:
			var tid: String = templates[0].get("template_id", "")
			if tid and _bridge.has_method("AcceptContextualTemplateV0"):
				_bridge.call("AcceptContextualTemplateV0", tid)
				await create_timer(0.05).timeout
				result["label"] = "accept_contextual_" + tid
				return result
		result["label"] = "accept_contextual_noop"
		return result

	# ── MODULE (38-47) ──
	if action >= 38 and action <= 42:
		var slot_idx: int = action - 38
		var mods: Array = _bcall("GetAvailableModulesV0", [])
		if mods and mods.size() > 0 and mods[0] is Dictionary:
			var mod_id: String = mods[0].get("module_id", "")
			if mod_id and _bridge.has_method("InstallModuleV0"):
				_bridge.call("InstallModuleV0", PLAYER_FLEET, slot_idx, mod_id)
				await create_timer(0.05).timeout
				result["label"] = "install_mod_%d_%s" % [slot_idx, mod_id]
				return result
		result["label"] = "install_mod_noop"
		return result

	if action >= 43 and action <= 47:
		var slot_idx: int = action - 43
		if _bridge.has_method("RemoveModuleV0"):
			_bridge.call("RemoveModuleV0", PLAYER_FLEET, slot_idx)
			await create_timer(0.05).timeout
			result["label"] = "remove_mod_%d" % slot_idx
		else:
			result["label"] = "remove_mod_noop"
		return result

	# ── HAVEN (48-52) ──
	if action == 48:
		if _bridge.has_method("UpgradeHavenV0"):
			_bridge.call("UpgradeHavenV0")
			await create_timer(0.05).timeout
			result["label"] = "upgrade_haven"
		else:
			result["label"] = "upgrade_haven_noop"
		return result

	if action == 49:
		var tree: Array = _bcall("GetTechTreeV0", [])
		if tree:
			for t in tree:
				if t is Dictionary and not bool(t.get("unlocked", false)):
					var tech_id: String = t.get("tech_id", "")
					if tech_id and _bridge.has_method("StartResearchV0"):
						_bridge.call("StartResearchV0", tech_id, loc)
						await create_timer(0.05).timeout
						result["label"] = "start_research_" + tech_id
						return result
		result["label"] = "start_research_noop"
		return result

	if action == 50:
		var catalog: Array = _bcall("GetT3ModuleCatalogV0", [])
		if catalog and catalog.size() > 0 and catalog[0] is Dictionary:
			var mod_id: String = catalog[0].get("module_id", "")
			if mod_id and _bridge.has_method("StartFabricationV0"):
				_bridge.call("StartFabricationV0", mod_id)
				await create_timer(0.05).timeout
				result["label"] = "start_fab_" + mod_id
				return result
		result["label"] = "start_fab_noop"
		return result

	if action == 52:
		var frags: Array = _bcall("GetAdaptationFragmentsV0", [])
		if frags:
			for frag in frags:
				if frag is Dictionary and bool(frag.get("collected", false)) and not bool(frag.get("deposited", false)):
					var fid: String = frag.get("fragment_id", "")
					if fid and _bridge.has_method("DepositFragmentV0"):
						_bridge.call("DepositFragmentV0", fid)
						await create_timer(0.05).timeout
						result["label"] = "deposit_frag_" + fid
						return result
		result["label"] = "deposit_frag_noop"
		return result

	# ── DIPLOMACY (64-66) ──
	if action == 64:
		if _bridge.has_method("ProposeTreatyV0"):
			_bridge.call("ProposeTreatyV0", FACTION_IDS[0])
			await create_timer(0.05).timeout
			result["label"] = "propose_treaty"
		else:
			result["label"] = "propose_treaty_noop"
		return result

	if action == 65:
		var proposals: Array = _bcall("GetDiplomaticProposalsV0", [])
		if proposals and proposals.size() > 0 and proposals[0] is Dictionary:
			var aid: String = proposals[0].get("act_id", "")
			if aid and _bridge.has_method("AcceptProposalV0"):
				_bridge.call("AcceptProposalV0", aid)
				await create_timer(0.05).timeout
				result["label"] = "accept_proposal_" + aid
				return result
		result["label"] = "accept_proposal_noop"
		return result

	if action == 66:
		var proposals: Array = _bcall("GetDiplomaticProposalsV0", [])
		if proposals and proposals.size() > 0 and proposals[0] is Dictionary:
			var aid: String = proposals[0].get("act_id", "")
			if aid and _bridge.has_method("RejectProposalV0"):
				_bridge.call("RejectProposalV0", aid)
				await create_timer(0.05).timeout
				result["label"] = "reject_proposal_" + aid
				return result
		result["label"] = "reject_proposal_noop"
		return result

	# ── EXPLORE (69-73) ──
	if action == 69 or action == 70:
		var disc: Array = _bcall1("GetDiscoverySnapshotV0", loc, [])
		if disc and disc.size() > 0 and disc[0] is Dictionary:
			var did: String = disc[0].get("discovery_id", "")
			if did:
				if action == 69 and _bridge.has_method("DispatchScanDiscoveryV0"):
					_bridge.call("DispatchScanDiscoveryV0", did)
					await create_timer(0.05).timeout
					result["label"] = "scan_discovery_" + did
					return result
				if action == 70 and _bridge.has_method("AdvanceDiscoveryPhaseV0"):
					_bridge.call("AdvanceDiscoveryPhaseV0", did)
					await create_timer(0.05).timeout
					result["label"] = "advance_disc_" + did
					return result
		result["label"] = "explore_noop"
		return result

	if action == 72:
		if _bridge.has_method("OrbitalScanV0"):
			_bridge.call("OrbitalScanV0", loc, "standard")
			await create_timer(0.05).timeout
			result["label"] = "orbital_scan"
		else:
			result["label"] = "orbital_scan_noop"
		return result

	if action == 73:
		if _bridge.has_method("LandingScanV0"):
			_bridge.call("LandingScanV0", loc, "standard")
			await create_timer(0.05).timeout
			result["label"] = "landing_scan"
		else:
			result["label"] = "landing_scan_noop"
		return result

	# ── CONSTRUCTION (74) ──
	if action == 74:
		var defs: Array = _bcall("GetAvailableConstructionDefsV0", [])
		if defs and defs.size() > 0 and defs[0] is Dictionary:
			var def_id: String = defs[0].get("project_def_id", "")
			if def_id and _bridge.has_method("StartConstructionV0"):
				_bridge.call("StartConstructionV0", def_id, loc)
				await create_timer(0.05).timeout
				result["label"] = "start_construction_" + def_id
				return result
		result["label"] = "construction_noop"
		return result

	# ── ENDGAME (79-81) ──
	if action >= 79 and action <= 81:
		var paths := ["reinforce", "naturalize", "renegotiate"]
		var chosen: String = paths[action - 79]
		if _bridge.has_method("ChooseEndgamePathV0"):
			_bridge.call("ChooseEndgamePathV0", chosen)
			await create_timer(0.05).timeout
			result["label"] = "choose_endgame_" + chosen
		else:
			result["label"] = "endgame_noop"
		return result

	# Unknown — noop
	await create_timer(0.05).timeout
	result["label"] = "noop_%d" % action
	return result


# ── Adjacency ──

func _build_adjacency() -> void:
	_adj.clear()
	_all_nodes.clear()

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_nodes = galaxy.get("system_nodes", [])
	var lanes: Array = galaxy.get("lane_edges", [])

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
	var neighbors: Array = _adj[node_id]
	neighbors.sort()
	return neighbors.slice(0, MAX_NEIGHBORS)


# ── TCP ──

func _send_response(data: Dictionary) -> void:
	if _peer == null or _peer.get_status() != StreamPeerTCP.STATUS_CONNECTED:
		return
	var json_str: String = JSON.stringify(data) + "\n"
	_peer.put_data(json_str.to_utf8_buffer())
