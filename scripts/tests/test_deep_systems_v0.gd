# scripts/tests/test_deep_systems_v0.gd
# Deep Systems Bot — exercises mid-game systems the first-hour bot cannot reach.
#
# Covered systems:
#   - Research (start, progress, tier advancement)
#   - Systemic missions (WAR_DEMAND/PRICE_SPIKE/SUPPLY_SHORTAGE offers)
#   - Combat depth (heat, battle stations, zone armor, AI shot-back)
#   - Faction reputation & tariffs
#   - Warfront state
#   - Discovery / scan / anomaly
#   - Transaction ledger & profit summary
#   - Knowledge web content
#   - Player stats & milestones
#   - Galaxy map overlays
#   - Save/load round-trip
#   - Refit (install + remove modules)
#   - Automation programs (auto-buy, cancel)
#   - Escort programs (create, query status)
#   - Fracture travel (access check, dispatch attempt)
#   - Construction (list defs, start project)
#   - Mission completion (accept → satisfy trigger → tick to complete)
#   - Haven depth (status, upgrade, fragments, fabricator, ancient hulls, endgame paths)
#   - Endgame state (win/loss conditions, progress, game result)
#   - Story state machine (revelations, pentagon, cascade effects)
#   - Fleet management (doctrine, patrol/survey programs)
#   - Diplomacy (treaties, bounties, sanctions, proposals)
#   - Megaproject depth (detail, start, supply delivery)
#   - Faction colors (visual identity query)
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless -Script deep_systems
#   godot --headless --path . -s res://scripts/tests/test_deep_systems_v0.gd
extends SceneTree

const AssertLib = preload("res://scripts/tools/bot_assert.gd")

const MAX_FRAMES := 5400  # 90s at 60fps
const MAX_POLLS := 600

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	# Core setup
	BOOT, INITIAL_TRADE,
	# Refit & modules
	REFIT_INSTALL, REFIT_REMOVE,
	# Automation & escort programs
	AUTOMATION_CREATE, ESCORT_CREATE,
	# Travel
	TRAVEL_1, SETTLE_1,
	# Research
	CHECK_RESEARCH, START_RESEARCH,
	# Combat depth
	FIND_TARGET, COMBAT_DEPTH, POST_COMBAT_DEPTH,
	# Systemic missions
	WAIT_SYSTEMIC, CHECK_SYSTEMIC,
	# Mission completion (accept + deliver)
	MISSION_ACCEPT, MISSION_DELIVER, MISSION_DELIVER_WAIT,
	# Construction
	CONSTRUCTION,
	# Faction & warfront
	CHECK_FACTION, CHECK_WARFRONT,
	# Discovery & fracture
	CHECK_DISCOVERY, FRACTURE_CHECK,
	# Knowledge & stats
	CHECK_KNOWLEDGE, CHECK_STATS, CHECK_LEDGER,
	# Galaxy map overlays
	CHECK_OVERLAYS,
	# Save/load
	SAVE_LOAD_TEST,
	# Haven depth
	HAVEN_DEPTH,
	# Endgame state
	ENDGAME_CHECK,
	# Story state machine
	STORY_CHECK,
	# Fleet management
	FLEET_MANAGEMENT,
	# Diplomacy & T44 depth
	DIPLOMACY_CHECK, MEGAPROJECT_DEPTH,
	# Bridge coverage sweep (exercises uncalled read-only methods)
	BRIDGE_COVERAGE,
	# Audit
	AUDIT, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _gm = null
var _a: AssertLib = null
var _visited: Dictionary = {}
var _home_node_id := ""
var _all_edges: Array = []


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_a.flag("TIMEOUT_AT_%s" % Phase.keys()[_phase])
		_phase = Phase.AUDIT

	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(Phase.WAIT_SCENE, 60, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait_ready()
		Phase.BOOT: _do_boot()
		Phase.INITIAL_TRADE: _do_initial_trade()
		Phase.REFIT_INSTALL: _do_refit_install()
		Phase.REFIT_REMOVE: _do_refit_remove()
		Phase.AUTOMATION_CREATE: _do_automation_create()
		Phase.ESCORT_CREATE: _do_escort_create()
		Phase.TRAVEL_1: _do_travel()
		Phase.SETTLE_1: _do_wait(Phase.SETTLE_1, 30, Phase.CHECK_RESEARCH)
		Phase.CHECK_RESEARCH: _do_check_research()
		Phase.START_RESEARCH: _do_start_research()
		Phase.FIND_TARGET: _do_find_target()
		Phase.COMBAT_DEPTH: _do_combat_depth()
		Phase.POST_COMBAT_DEPTH: _do_post_combat_depth()
		Phase.WAIT_SYSTEMIC: _do_wait_systemic()
		Phase.CHECK_SYSTEMIC: _do_check_systemic()
		Phase.MISSION_ACCEPT: _do_mission_accept()
		Phase.MISSION_DELIVER: _do_mission_deliver()
		Phase.MISSION_DELIVER_WAIT: _do_mission_deliver_wait()
		Phase.CONSTRUCTION: _do_construction()
		Phase.CHECK_FACTION: _do_check_faction()
		Phase.CHECK_WARFRONT: _do_check_warfront()
		Phase.CHECK_DISCOVERY: _do_check_discovery()
		Phase.FRACTURE_CHECK: _do_fracture_check()
		Phase.CHECK_KNOWLEDGE: _do_check_knowledge()
		Phase.CHECK_STATS: _do_check_stats()
		Phase.CHECK_LEDGER: _do_check_ledger()
		Phase.CHECK_OVERLAYS: _do_check_overlays()
		Phase.SAVE_LOAD_TEST: _do_save_load_test()
		Phase.HAVEN_DEPTH: _do_haven_depth()
		Phase.ENDGAME_CHECK: _do_endgame_check()
		Phase.STORY_CHECK: _do_story_check()
		Phase.FLEET_MANAGEMENT: _do_fleet_management()
		Phase.DIPLOMACY_CHECK: _do_diplomacy_check()
		Phase.MEGAPROJECT_DEPTH: _do_megaproject_depth()
		Phase.BRIDGE_COVERAGE: _do_bridge_coverage()
		Phase.AUDIT: _do_audit()
		Phase.DONE: _do_done()
	return false


# ===================== Setup =====================

func _do_load_scene() -> void:
	var scene = load("res://scenes/playable_prototype.tscn").instantiate()
	root.add_child(scene)
	_a = AssertLib.new("DS1")
	_a.log("SCENE_LOADED")
	_polls = 0
	_phase = Phase.WAIT_SCENE


func _do_wait(current: Phase, settle: int, next: Phase) -> void:
	_polls += 1
	if _polls >= settle:
		_polls = 0
		_phase = next


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge != null:
		_polls = 0
		_phase = Phase.WAIT_READY
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_a.flag("BRIDGE_NOT_FOUND")
			_phase = Phase.AUDIT


func _do_wait_ready() -> void:
	var ready := false
	if _bridge.has_method("GetBridgeReadyV0"):
		ready = bool(_bridge.call("GetBridgeReadyV0"))
	else:
		ready = true
	if ready:
		_gm = root.get_node_or_null("GameManager")
		if _gm:
			_gm.set("_on_main_menu", false)
		_polls = 0
		_phase = Phase.BOOT
	else:
		_polls += 1
		if _polls >= MAX_POLLS:
			_a.flag("BRIDGE_READY_TIMEOUT")
			_phase = Phase.AUDIT


# ===================== Boot & Setup =====================

func _do_boot() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))
	_visited[_home_node_id] = true
	var credits := int(ps.get("credits", 0))
	_a.log("BOOT|node=%s credits=%d" % [_home_node_id, credits])
	_a.hard(credits > 0, "boot_credits", "credits=%d" % credits)

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	_all_edges = galaxy.get("lane_edges", [])

	# Promote FO for dialogue triggers
	if _bridge.has_method("GetFirstOfficerCandidatesV0"):
		var candidates: Array = _bridge.call("GetFirstOfficerCandidatesV0")
		if candidates.size() > 0:
			var ctype := str(candidates[0].get("type", ""))
			if not ctype.is_empty() and _bridge.has_method("PromoteFirstOfficerV0"):
				_bridge.call("PromoteFirstOfficerV0", ctype)
				_a.log("FO_PROMOTED|type=%s" % ctype)

	_polls = 0
	_phase = Phase.INITIAL_TRADE


func _do_initial_trade() -> void:
	# Dock and do a quick buy to generate a transaction
	_dock_at_station()
	_busy = true
	await create_timer(0.3).timeout

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	for item in market:
		var price := int(item.get("buy_price", 0))
		var qty := int(item.get("quantity", 0))
		if price > 0 and qty > 0:
			_bridge.call("DispatchPlayerTradeV0", node_id, str(item.get("good_id", "")), 1, true)
			_a.log("TRADE|buy good=%s price=%d" % [str(item.get("good_id", "")), price])
			break

	_busy = false
	_polls = 0
	_phase = Phase.REFIT_INSTALL


# ===================== Refit (Install / Remove Module) =====================

func _do_refit_install() -> void:
	if not _bridge.has_method("GetPlayerFleetSlotsV0") or not _bridge.has_method("GetAvailableModulesV0"):
		_a.warn(false, "refit_bridge_exists", "GetPlayerFleetSlotsV0 or GetAvailableModulesV0 missing")
		_phase = Phase.AUTOMATION_CREATE
		return

	var slots: Array = _bridge.call("GetPlayerFleetSlotsV0")
	_a.log("REFIT|slots=%d" % slots.size())

	var modules: Array = _bridge.call("GetAvailableModulesV0")
	_a.log("REFIT|available_modules=%d" % modules.size())

	# Find an empty slot and an installable module
	var installed := false
	var install_slot := -1
	var install_mod := ""
	for i in range(slots.size()):
		var slot = slots[i]
		var slot_kind := str(slot.get("slot_kind", ""))
		var installed_id := str(slot.get("installed_module_id", ""))
		if installed_id.is_empty():
			# Find a module matching this slot kind
			for mod in modules:
				var mod_kind := str(mod.get("slot_kind", ""))
				var can_install: bool = mod.get("can_install", false)
				if can_install and mod_kind == slot_kind:
					install_slot = i
					install_mod = str(mod.get("module_id", ""))
					break
		if install_slot >= 0:
			break

	if install_slot >= 0 and not install_mod.is_empty() and _bridge.has_method("InstallModuleV0"):
		var result: Dictionary = _bridge.call("InstallModuleV0", "fleet_trader_1", install_slot, install_mod)
		var success: bool = result.get("success", false)
		_a.log("REFIT|install slot=%d mod=%s success=%s reason=%s" % [install_slot, install_mod, str(success), str(result.get("reason", ""))])
		_a.warn(success, "refit_install_module", "slot=%d mod=%s" % [install_slot, install_mod])
		if success:
			installed = true
			set_meta("_refit_slot", install_slot)
			set_meta("_refit_mod", install_mod)
	else:
		_a.warn(false, "refit_install_module", "no_empty_slot_or_matching_module")

	_a.goal("REFIT", "install_attempted=%s installed=%s" % [str(install_slot >= 0), str(installed)])
	_polls = 0
	_phase = Phase.REFIT_REMOVE


func _do_refit_remove() -> void:
	var slot_idx: int = get_meta("_refit_slot", -1)
	if slot_idx < 0 or not _bridge.has_method("RemoveModuleV0"):
		_a.log("REFIT|skip_remove no_installed_slot")
		_phase = Phase.AUTOMATION_CREATE
		return

	var result: Dictionary = _bridge.call("RemoveModuleV0", "fleet_trader_1", slot_idx)
	var success: bool = result.get("success", false)
	_a.log("REFIT|remove slot=%d success=%s reason=%s" % [slot_idx, str(success), str(result.get("reason", ""))])
	_a.warn(success, "refit_remove_module", "slot=%d" % slot_idx)
	_a.goal("REFIT", "remove_attempted=true success=%s" % str(success))

	_polls = 0
	_phase = Phase.AUTOMATION_CREATE


# ===================== Automation Programs =====================

func _do_automation_create() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))

	# Create auto-buy program
	if _bridge.has_method("CreateAutoBuyProgram"):
		var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
		var good_id := ""
		for item in market:
			var qty := int(item.get("quantity", 0))
			if qty > 0:
				good_id = str(item.get("good_id", ""))
				break

		if not good_id.is_empty():
			var pid: String = _bridge.call("CreateAutoBuyProgram", node_id, good_id, 1, 60)
			_a.log("AUTOMATION|create_auto_buy market=%s good=%s pid=%s" % [node_id, good_id, pid])
			var created := not pid.is_empty()
			_a.warn(created, "automation_create_program", "pid=%s" % pid)
			_a.goal("AUTOMATION", "program_created=%s" % str(created))

			# Query program explain snapshot
			if created and _bridge.has_method("GetProgramExplainSnapshot"):
				var explain: Array = _bridge.call("GetProgramExplainSnapshot")
				_a.log("AUTOMATION|explain_count=%d" % explain.size())

			# Cancel it (cleanup)
			if created and _bridge.has_method("CancelProgram"):
				var cancelled: bool = _bridge.call("CancelProgram", pid)
				_a.log("AUTOMATION|cancel pid=%s ok=%s" % [pid, str(cancelled)])
				_a.warn(cancelled, "automation_cancel_program", "pid=%s" % pid)
		else:
			_a.warn(false, "automation_create_program", "no_goods_in_market")
	else:
		_a.warn(false, "automation_bridge_exists", "CreateAutoBuyProgram missing")

	# Check automation performance/templates
	if _bridge.has_method("GetProgramTemplatesV0"):
		var templates: Array = _bridge.call("GetProgramTemplatesV0")
		_a.log("AUTOMATION|templates=%d" % templates.size())

	if _bridge.has_method("GetProgramPerformanceV0"):
		var perf: Dictionary = _bridge.call("GetProgramPerformanceV0", "fleet_trader_1")
		_a.log("AUTOMATION|performance=%s" % str(perf))

	_polls = 0
	_phase = Phase.ESCORT_CREATE


# ===================== Escort Programs =====================

func _do_escort_create() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var neighbors := _get_neighbors()

	if not _bridge.has_method("CreateEscortProgramV0"):
		_a.warn(false, "escort_bridge_exists", "CreateEscortProgramV0 missing")
		_phase = Phase.TRAVEL_1
		return

	if neighbors.is_empty():
		_a.warn(false, "escort_create", "no_neighbors_for_route")
		_phase = Phase.TRAVEL_1
		return

	var dest := str(neighbors[0])
	var pid: String = _bridge.call("CreateEscortProgramV0", "fleet_trader_1", node_id, dest, 30)
	_a.log("ESCORT|create origin=%s dest=%s pid=%s" % [node_id, dest, pid])
	var created := not pid.is_empty()
	_a.warn(created, "escort_create_program", "pid=%s" % pid)
	_a.goal("ESCORT", "program_created=%s" % str(created))

	# Query escort status
	if created and _bridge.has_method("GetEscortStatusV0"):
		var status: Dictionary = _bridge.call("GetEscortStatusV0", pid)
		_a.log("ESCORT|status=%s" % str(status))

	# Cancel it (cleanup — we don't want an escort running during other tests)
	if created and _bridge.has_method("CancelProgram"):
		var cancelled: bool = _bridge.call("CancelProgram", pid)
		_a.log("ESCORT|cancel pid=%s ok=%s" % [pid, str(cancelled)])

	_polls = 0
	_phase = Phase.TRAVEL_1


func _do_travel() -> void:
	var neighbors := _get_neighbors()
	if neighbors.is_empty():
		_a.flag("NO_NEIGHBORS")
		_phase = Phase.CHECK_RESEARCH
		return

	var dest := str(neighbors[0])
	_headless_travel(dest)
	_visited[dest] = true
	_a.log("TRAVEL|dest=%s" % dest)
	_polls = 0
	_phase = Phase.SETTLE_1


# ===================== Research =====================

func _do_check_research() -> void:
	if not _bridge.has_method("GetTechTreeV0"):
		_a.warn(false, "research_bridge_exists", "GetTechTreeV0 missing")
		_phase = Phase.FIND_TARGET
		return

	var techs: Array = _bridge.call("GetTechTreeV0")
	_a.hard(techs.size() >= 1, "tech_tree_populated", "count=%d" % techs.size())
	_a.goal("DEPTH", "tech_count=%d" % techs.size())

	if _bridge.has_method("GetTechTierV0"):
		var tier := int(_bridge.call("GetTechTierV0"))
		_a.log("RESEARCH|tier=%d" % tier)

	if _bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = _bridge.call("GetResearchStatusV0")
		_a.log("RESEARCH|status=%s" % str(status))

	_polls = 0
	_phase = Phase.START_RESEARCH


func _do_start_research() -> void:
	if not _bridge.has_method("StartResearchV0") or not _bridge.has_method("GetTechTreeV0"):
		_phase = Phase.FIND_TARGET
		return

	var techs: Array = _bridge.call("GetTechTreeV0")
	# Find first researchable tech
	var started := false
	for tech in techs:
		var tid := str(tech.get("tech_id", ""))
		var can_research: bool = tech.get("can_research", false)
		if can_research and not tid.is_empty():
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id := str(ps.get("current_node_id", ""))
			var result: Dictionary = _bridge.call("StartResearchV0", tid, node_id)
			var success: bool = result.get("success", false)
			_a.log("RESEARCH|start=%s success=%s" % [tid, str(success)])
			if success:
				_a.goal("DEPTH", "research_started=%s" % tid)
				started = true
			break

	_a.warn(started, "research_started", "able_to_start_any_tech")

	# Check research status after starting
	if started and _bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = _bridge.call("GetResearchStatusV0")
		var active_id := str(status.get("active_tech_id", ""))
		_a.log("RESEARCH|active=%s" % active_id)

	_polls = 0
	_phase = Phase.FIND_TARGET


# ===================== Combat Depth =====================

func _do_find_target() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))

	if not _bridge.has_method("GetSystemSnapshotV0"):
		_phase = Phase.WAIT_SYSTEMIC
		return

	var npc_id := ""
	# Check current + visited systems
	for vid in _visited:
		var snap: Dictionary = _bridge.call("GetSystemSnapshotV0", str(vid))
		var fleets: Array = snap.get("fleets", [])
		for fleet in fleets:
			var fid := str(fleet.get("fleet_id", ""))
			var owner := str(fleet.get("owner_id", ""))
			if owner != "player" and not fid.is_empty():
				npc_id = fid
				break
		if not npc_id.is_empty():
			break

	if npc_id.is_empty():
		_a.flag("NO_NPC_FOR_COMBAT_DEPTH")
		_phase = Phase.WAIT_SYSTEMIC
		return

	set_meta("_combat_target", npc_id)
	_a.log("COMBAT_DEPTH|target=%s" % npc_id)
	_polls = 0
	_phase = Phase.COMBAT_DEPTH


func _do_combat_depth() -> void:
	var fleet_id: String = get_meta("_combat_target", "")
	if fleet_id.is_empty():
		_phase = Phase.WAIT_SYSTEMIC
		return

	# === Heat system ===
	if _bridge.has_method("GetHeatSnapshotV0"):
		var heat_before: Dictionary = _bridge.call("GetHeatSnapshotV0")
		_a.log("COMBAT|heat_before=%s" % str(heat_before))
		_a.goal("COMBAT", "heat_system_exists=true")

	# === Battle stations ===
	if _bridge.has_method("ToggleBattleStationsV0"):
		var bs_result: Dictionary = _bridge.call("ToggleBattleStationsV0")
		_a.log("COMBAT|battle_stations_toggle=%s" % str(bs_result))
		_a.goal("COMBAT", "battle_stations_toggled=true")

	if _bridge.has_method("GetBattleStationsStateV0"):
		var bs_state: Dictionary = _bridge.call("GetBattleStationsStateV0")
		_a.log("COMBAT|battle_stations_state=%s" % str(bs_state))

	# === Spin state ===
	if _bridge.has_method("GetSpinStateV0"):
		var spin: Dictionary = _bridge.call("GetSpinStateV0")
		_a.log("COMBAT|spin=%s" % str(spin))

	# === Mount types ===
	if _bridge.has_method("GetMountTypesV0"):
		var mounts: Array = _bridge.call("GetMountTypesV0", "fleet_trader_1")
		_a.log("COMBAT|mounts=%d" % mounts.size())
		_a.warn(mounts.size() >= 1, "combat_has_mounts", "count=%d" % mounts.size())

	# === Turret shot (uses zone armor path) ===
	if _bridge.has_method("ApplyTurretShotV0"):
		var shot: Dictionary = _bridge.call("ApplyTurretShotV0", fleet_id)
		_a.log("COMBAT|turret_shot=%s" % str(shot))
		_a.goal("COMBAT", "turret_shot_applied=true dmg=%s" % str(shot.get("damage_dealt", 0)))

	# === AI shoots back ===
	if _bridge.has_method("ApplyAiShotAtPlayerV0"):
		var ai_shot: Dictionary = _bridge.call("ApplyAiShotAtPlayerV0", fleet_id)
		_a.log("COMBAT|ai_shot_back=%s" % str(ai_shot))

	# === Check heat after shots ===
	if _bridge.has_method("GetHeatSnapshotV0"):
		var heat_after: Dictionary = _bridge.call("GetHeatSnapshotV0")
		_a.log("COMBAT|heat_after=%s" % str(heat_after))

	# === Radiator status ===
	if _bridge.has_method("GetRadiatorStatusV0"):
		var rad: Dictionary = _bridge.call("GetRadiatorStatusV0")
		_a.log("COMBAT|radiator=%s" % str(rad))

	# Now finish the NPC with lethal damage
	_bridge.call("DamageNpcFleetV0", fleet_id, 150)
	_a.log("COMBAT|lethal_hit target=%s" % fleet_id)

	_polls = 0
	_phase = Phase.POST_COMBAT_DEPTH


func _do_post_combat_depth() -> void:
	_busy = true
	await create_timer(1.0).timeout

	# === Check recent combat events ===
	if _bridge.has_method("GetRecentCombatEventsV0"):
		var events: Array = _bridge.call("GetRecentCombatEventsV0")
		_a.log("COMBAT|recent_events=%d" % events.size())
		_a.warn(events.size() >= 1, "combat_events_generated", "count=%d" % events.size())

	# === Loot ===
	if _bridge.has_method("GetNearbyLootV0"):
		var loot: Array = _bridge.call("GetNearbyLootV0")
		_a.log("COMBAT|nearby_loot=%d" % loot.size())

	_busy = false
	_polls = 0
	_phase = Phase.WAIT_SYSTEMIC


# ===================== Systemic Missions =====================

func _do_wait_systemic() -> void:
	# Poll for systemic offers over several ticks
	_polls += 1
	if _polls % 20 == 0:
		if _bridge.has_method("GetSystemicOffersV0"):
			var offers: Array = _bridge.call("GetSystemicOffersV0")
			_a.log("SYSTEMIC|tick=%d offers=%d" % [_polls, offers.size()])
			if offers.size() > 0:
				_polls = 0
				_phase = Phase.CHECK_SYSTEMIC
				return

	if _polls >= 200:
		_a.warn(false, "systemic_offers_appeared", "none_in_200_polls")
		_a.log("SYSTEMIC|timeout_no_offers")
		_polls = 0
		_phase = Phase.MISSION_ACCEPT


func _do_check_systemic() -> void:
	if not _bridge.has_method("GetSystemicOffersV0"):
		_phase = Phase.MISSION_ACCEPT
		return

	var offers: Array = _bridge.call("GetSystemicOffersV0")
	_a.hard(offers.size() >= 1, "systemic_offers_exist", "count=%d" % offers.size())

	if offers.size() > 0:
		var offer = offers[0]
		var offer_id := str(offer.get("offer_id", ""))
		var trigger := str(offer.get("trigger_type", ""))
		var good := str(offer.get("good_id", ""))
		_a.log("SYSTEMIC|offer=%s trigger=%s good=%s" % [offer_id, trigger, good])
		_a.goal("SYSTEMIC", "trigger=%s good=%s" % [trigger, good])

		# Accept it
		if _bridge.has_method("AcceptSystemicMissionV0") and not offer_id.is_empty():
			var ok: bool = _bridge.call("AcceptSystemicMissionV0", offer_id)
			_a.log("SYSTEMIC|accept=%s" % str(ok))
			_a.warn(ok, "systemic_mission_accepted", "offer=%s" % offer_id)

	_polls = 0
	_phase = Phase.MISSION_ACCEPT


# ===================== Mission Accept + Deliver =====================

func _do_mission_accept() -> void:
	if not _bridge.has_method("GetMissionListV0") or not _bridge.has_method("AcceptMissionV0"):
		_a.warn(false, "mission_bridge_exists", "GetMissionListV0 or AcceptMissionV0 missing")
		_phase = Phase.CONSTRUCTION
		return

	# Check if we already have an active mission (from systemic accept)
	var active: Dictionary = _bridge.call("GetActiveMissionV0")
	var active_id := str(active.get("id", ""))
	if not active_id.is_empty():
		_a.log("MISSION|already_active=%s title=%s" % [active_id, str(active.get("title", ""))])
		set_meta("_mission_id", active_id)
		_phase = Phase.MISSION_DELIVER
		return

	# Try to accept from mission list
	var missions: Array = _bridge.call("GetMissionListV0")
	_a.log("MISSION|available=%d" % missions.size())
	var accepted := false
	for m in missions:
		var mid := str(m.get("mission_id", ""))
		if not mid.is_empty():
			var ok: bool = _bridge.call("AcceptMissionV0", mid)
			_a.log("MISSION|accept=%s ok=%s" % [mid, str(ok)])
			if ok:
				accepted = true
				set_meta("_mission_id", mid)
				# Tick once to process acceptance
				if _bridge.has_method("TickV0"):
					_bridge.call("TickV0")
				break

	if accepted:
		_a.goal("MISSIONS", "mission_accepted=true")
		_phase = Phase.MISSION_DELIVER
	else:
		_a.warn(false, "mission_accepted", "no_available_missions_or_accept_failed")
		_phase = Phase.CONSTRUCTION

	_polls = 0


func _do_mission_deliver() -> void:
	# Read active mission to understand what we need to do
	var active: Dictionary = _bridge.call("GetActiveMissionV0")
	var mission_id := str(active.get("id", ""))
	_a.log("MISSION|deliver_check id=%s title=%s" % [mission_id, str(active.get("title", ""))])

	# Check rewards preview
	if not mission_id.is_empty() and _bridge.has_method("GetMissionRewardsPreviewV0"):
		var rewards: Dictionary = _bridge.call("GetMissionRewardsPreviewV0", mission_id)
		_a.log("MISSION|rewards=%s" % str(rewards))
		_a.goal("MISSIONS", "rewards_visible=%s" % str(not rewards.is_empty()))

	# Check prerequisites detail
	if not mission_id.is_empty() and _bridge.has_method("GetMissionPrerequisitesDetailV0"):
		var prereqs: Dictionary = _bridge.call("GetMissionPrerequisitesDetailV0", mission_id)
		_a.log("MISSION|prereqs=%s" % str(prereqs))

	# Get active mission summary (steps, objectives)
	if _bridge.has_method("GetActiveMissionSummaryV0"):
		var summary: Dictionary = _bridge.call("GetActiveMissionSummaryV0")
		_a.log("MISSION|summary=%s" % str(summary))

		# Try to satisfy the current step trigger
		var steps: Array = summary.get("steps", [])
		if steps.size() > 0:
			var step = steps[0]
			var trigger := str(step.get("trigger_type", ""))
			var target_node := str(step.get("target_node_id", ""))
			var target_good := str(step.get("target_good_id", ""))
			var target_qty := int(step.get("target_quantity", 0))
			_a.log("MISSION|step trigger=%s node=%s good=%s qty=%d" % [trigger, target_node, target_good, target_qty])

			# ArriveAtNode: travel there
			if trigger == "ArriveAtNode" and not target_node.is_empty():
				_headless_travel(target_node)
				_a.log("MISSION|traveled_to=%s" % target_node)

			# HaveCargoMin: buy the goods
			if trigger == "HaveCargoMin" and not target_good.is_empty() and target_qty > 0:
				var ps: Dictionary = _bridge.call("GetPlayerStateV0")
				var node_id := str(ps.get("current_node_id", ""))
				_bridge.call("DispatchPlayerTradeV0", node_id, target_good, target_qty, true)
				_a.log("MISSION|bought good=%s qty=%d" % [target_good, target_qty])

			# NoCargoAtNode: sell/dump cargo and travel
			if trigger == "NoCargoAtNode" and not target_node.is_empty():
				_headless_travel(target_node)
				if not target_good.is_empty():
					_bridge.call("DispatchPlayerTradeV0", target_node, target_good, target_qty, false)
					_a.log("MISSION|sold good=%s at=%s" % [target_good, target_node])

	# Tick sim several times to evaluate triggers
	if _bridge.has_method("TickV0"):
		for i in range(5):
			_bridge.call("TickV0")

	_a.log("MISSION|abandon_method_exists=%s" % str(_bridge.has_method("AbandonMissionV0")))
	_polls = 0
	_phase = Phase.MISSION_DELIVER_WAIT


func _do_mission_deliver_wait() -> void:
	# Tick and check if mission completed
	_polls += 1
	if _bridge.has_method("TickV0"):
		_bridge.call("TickV0")

	var active: Dictionary = _bridge.call("GetActiveMissionV0")
	var mission_id := str(active.get("id", ""))

	if mission_id.is_empty():
		# Mission completed (no longer active)
		_a.hard(true, "mission_completed", "mission cleared from active")
		_a.goal("MISSIONS", "mission_completed=true")
		_phase = Phase.CONSTRUCTION
		_polls = 0
		return

	if _polls >= 60:
		# Couldn't complete in 60 ticks — log what we know and move on
		_a.warn(false, "mission_completed", "still_active_after_60_ticks id=%s" % mission_id)
		_a.log("MISSION|timeout still_active=%s" % mission_id)
		# Abandon to clean up
		if _bridge.has_method("AbandonMissionV0"):
			_bridge.call("AbandonMissionV0")
			_a.log("MISSION|abandoned after timeout")
		_phase = Phase.CONSTRUCTION
		_polls = 0


# ===================== Construction =====================

func _do_construction() -> void:
	# List available construction project definitions
	if _bridge.has_method("GetAvailableConstructionDefsV0"):
		var defs: Array = _bridge.call("GetAvailableConstructionDefsV0")
		_a.log("CONSTRUCTION|available_defs=%d" % defs.size())
		_a.goal("CONSTRUCTION", "project_defs=%d" % defs.size())

		# Try to start the first project with met prerequisites
		if defs.size() > 0 and _bridge.has_method("StartConstructionV0"):
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id := str(ps.get("current_node_id", ""))
			for proj_def in defs:
				var def_id := str(proj_def.get("project_def_id", ""))
				var prereqs_met: bool = proj_def.get("prerequisites_met", false)
				if prereqs_met and not def_id.is_empty():
					# Check block reason first
					if _bridge.has_method("GetConstructionBlockReasonV0"):
						var reason: String = _bridge.call("GetConstructionBlockReasonV0", def_id, node_id)
						_a.log("CONSTRUCTION|block_reason def=%s reason=%s" % [def_id, reason])
						if not reason.is_empty():
							continue

					var result: Dictionary = _bridge.call("StartConstructionV0", def_id, node_id)
					var success: bool = result.get("success", false)
					var project_id := str(result.get("project_id", ""))
					_a.log("CONSTRUCTION|start def=%s success=%s pid=%s reason=%s" % [
						def_id, str(success), project_id, str(result.get("reason", ""))])
					_a.warn(success, "construction_start", "def=%s" % def_id)

					# Check progress
					if success and not project_id.is_empty() and _bridge.has_method("GetConstructionProgressV0"):
						var progress: Dictionary = _bridge.call("GetConstructionProgressV0", project_id)
						_a.log("CONSTRUCTION|progress=%s" % str(progress))
					break
	else:
		_a.warn(false, "construction_bridge_exists", "GetAvailableConstructionDefsV0 missing")

	# Check active construction projects
	if _bridge.has_method("GetConstructionProjectsV0"):
		var projects: Array = _bridge.call("GetConstructionProjectsV0")
		_a.log("CONSTRUCTION|active_projects=%d" % projects.size())

	_polls = 0
	_phase = Phase.CHECK_FACTION


# ===================== Faction & Warfront =====================

func _do_check_faction() -> void:
	# Faction list
	if _bridge.has_method("GetAllFactionsV0"):
		var factions: Array = _bridge.call("GetAllFactionsV0")
		_a.warn(factions.size() >= 2, "factions_exist", "count=%d" % factions.size())
		_a.goal("FACTIONS", "count=%d" % factions.size())

		# Check reputation with first faction
		if factions.size() > 0 and _bridge.has_method("GetPlayerReputationV0"):
			var fid := str(factions[0].get("faction_id", ""))
			if not fid.is_empty():
				var rep: Dictionary = _bridge.call("GetPlayerReputationV0", fid)
				_a.log("FACTION|%s rep=%s" % [fid, str(rep)])
				_a.goal("FACTIONS", "reputation_queryable=true faction=%s" % fid)

		# Check faction doctrine
		if factions.size() > 0 and _bridge.has_method("GetFactionDoctrineV0"):
			var fid := str(factions[0].get("faction_id", ""))
			if not fid.is_empty():
				var doctrine: Dictionary = _bridge.call("GetFactionDoctrineV0", fid)
				_a.log("FACTION|%s doctrine=%s" % [fid, str(doctrine)])

	# Territory access at current node
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetTerritoryAccessV0"):
		var access: Dictionary = _bridge.call("GetTerritoryAccessV0", node_id)
		_a.log("FACTION|territory_access=%s" % str(access))

	# Embargo check
	if _bridge.has_method("GetEmbargoesV0"):
		var embargoes: Array = _bridge.call("GetEmbargoesV0", node_id)
		_a.log("FACTION|embargoes=%d" % embargoes.size())

	# Node instability
	if _bridge.has_method("GetNodeInstabilityV0"):
		var instability: Dictionary = _bridge.call("GetNodeInstabilityV0", node_id)
		_a.log("FACTION|instability=%s" % str(instability))

	_polls = 0
	_phase = Phase.CHECK_WARFRONT


func _do_check_warfront() -> void:
	if _bridge.has_method("GetWarfrontsV0"):
		var warfronts: Array = _bridge.call("GetWarfrontsV0")
		_a.log("WARFRONT|count=%d" % warfronts.size())
		_a.goal("WARFRONT", "active_warfronts=%d" % warfronts.size())

		if warfronts.size() > 0 and _bridge.has_method("GetWarSupplyV0"):
			var wid := str(warfronts[0].get("warfront_id", ""))
			if not wid.is_empty():
				var supply: Dictionary = _bridge.call("GetWarSupplyV0", wid)
				_a.log("WARFRONT|%s supply=%s" % [wid, str(supply)])

	# War intensity at current node
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetNodeWarIntensityV0"):
		var intensity := int(_bridge.call("GetNodeWarIntensityV0", node_id))
		_a.log("WARFRONT|node=%s intensity=%d" % [node_id, intensity])

	# Supply shock summary
	if _bridge.has_method("GetSupplyShockSummaryV0"):
		var shock: Dictionary = _bridge.call("GetSupplyShockSummaryV0")
		_a.log("WARFRONT|supply_shock=%s" % str(shock))
		_a.goal("WARFRONT", "disrupted=%d" % int(shock.get("disrupted_count", 0)))

	# Lattice drone alerts at current node
	if _bridge.has_method("GetLatticeDroneAlertsV0"):
		var alerts: Array = _bridge.call("GetLatticeDroneAlertsV0", node_id)
		_a.log("WARFRONT|drone_alerts=%d node=%s" % [alerts.size(), node_id])

	# Drone activity summary
	if _bridge.has_method("GetDroneActivityV0"):
		var drones: Dictionary = _bridge.call("GetDroneActivityV0")
		_a.log("WARFRONT|drone_activity=%s" % str(drones))

	_polls = 0
	_phase = Phase.CHECK_DISCOVERY


# ===================== Discovery =====================

func _do_check_discovery() -> void:
	# Sensor level
	if _bridge.has_method("GetSensorLevelV0"):
		var level := int(_bridge.call("GetSensorLevelV0"))
		_a.log("DISCOVERY|sensor_level=%d" % level)

	# Discovery snapshot at current node
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetDiscoverySnapshotV0"):
		var discoveries: Array = _bridge.call("GetDiscoverySnapshotV0", node_id)
		_a.log("DISCOVERY|node=%s count=%d" % [node_id, discoveries.size()])
		_a.goal("DISCOVERY", "site_count=%d node=%s" % [discoveries.size(), node_id])

		# Try to scan first discovery if any
		if discoveries.size() > 0 and _bridge.has_method("DispatchScanDiscoveryV0"):
			var did := str(discoveries[0].get("discovery_id", ""))
			if not did.is_empty():
				_bridge.call("DispatchScanDiscoveryV0", did)
				_a.log("DISCOVERY|scan=%s" % did)
				_a.goal("DISCOVERY", "scan_attempted=%s" % did)

	# Available void sites
	if _bridge.has_method("GetAvailableVoidSitesV0"):
		var void_sites: Array = _bridge.call("GetAvailableVoidSitesV0")
		_a.log("DISCOVERY|void_sites=%d" % void_sites.size())
		_a.goal("DISCOVERY", "void_sites=%d" % void_sites.size())

	# Fracture discovery status
	if _bridge.has_method("GetFractureDiscoveryStatusV0"):
		var frac_status: Dictionary = _bridge.call("GetFractureDiscoveryStatusV0")
		_a.log("DISCOVERY|fracture_status=%s" % str(frac_status))

	# Active encounters
	if _bridge.has_method("GetActiveEncountersV0"):
		var encounters: Array = _bridge.call("GetActiveEncountersV0")
		_a.log("DISCOVERY|active_encounters=%d" % encounters.size())

	# Trade intel from discoveries
	if _bridge.has_method("GetDiscoveryTradeIntelV0"):
		var intel: Array = _bridge.call("GetDiscoveryTradeIntelV0")
		_a.log("DISCOVERY|trade_intel=%d" % intel.size())
		_a.goal("DISCOVERY", "trade_intel_routes=%d" % intel.size())

	# Anomaly chains
	if _bridge.has_method("GetActiveChainsV0"):
		var chains: Array = _bridge.call("GetActiveChainsV0")
		_a.log("DISCOVERY|active_chains=%d" % chains.size())
		_a.hard(chains.size() >= 1, "anomaly_chains_exist", "count=%d" % chains.size())
		_a.goal("DISCOVERY", "chains=%d" % chains.size())

		# Chain progress for first chain
		if chains.size() > 0 and _bridge.has_method("GetChainProgressV0"):
			var chain_id := str(chains[0].get("chain_id", ""))
			if not chain_id.is_empty():
				var progress: Dictionary = _bridge.call("GetChainProgressV0", chain_id)
				_a.log("DISCOVERY|chain_progress=%s" % str(progress))
				_a.warn(progress.get("found", false), "chain_progress_found", "id=%s" % chain_id)

	# Instability-revealed sites
	if _bridge.has_method("GetInstabilityRevealedSitesV0"):
		var sites: Array = _bridge.call("GetInstabilityRevealedSitesV0")
		_a.log("DISCOVERY|instability_sites=%d" % sites.size())

	_polls = 0
	_phase = Phase.FRACTURE_CHECK


# ===================== Fracture Travel =====================

func _do_fracture_check() -> void:
	# Check fracture access for player fleet at current node
	if _bridge.has_method("GetFractureAccessV0"):
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var node_id := str(ps.get("current_node_id", ""))
		var access: Dictionary = _bridge.call("GetFractureAccessV0", "fleet_trader_1", node_id)
		var allowed: bool = access.get("allowed", false)
		var reason := str(access.get("reason", ""))
		_a.log("FRACTURE|access allowed=%s reason=%s node=%s" % [str(allowed), reason, node_id])
		_a.goal("FRACTURE", "access_checked=true allowed=%s" % str(allowed))

	# Check void sites
	if _bridge.has_method("GetAvailableVoidSitesV0"):
		var sites: Array = _bridge.call("GetAvailableVoidSitesV0")
		_a.log("FRACTURE|void_sites=%d" % sites.size())

		# Attempt fracture travel to first site (may fail — that's expected without unlock)
		if sites.size() > 0 and _bridge.has_method("DispatchFractureTravelV0"):
			var site_id := str(sites[0].get("id", ""))
			if not site_id.is_empty():
				_a.log("FRACTURE|dispatch_attempt site=%s" % site_id)
				# Note: This may throw or fail if fracture drive not unlocked — that's OK
				# We just want to exercise the bridge method
				_bridge.call("DispatchFractureTravelV0", "fleet_trader_1", site_id)
				_a.log("FRACTURE|dispatch_called site=%s" % site_id)
				_a.goal("FRACTURE", "dispatch_attempted=true")
		else:
			_a.log("FRACTURE|no_void_sites_to_dispatch")
	else:
		_a.warn(false, "fracture_bridge_exists", "GetAvailableVoidSitesV0 missing")

	# Fracture discovery status
	if _bridge.has_method("GetFractureDiscoveryStatusV0"):
		var frac_status: Dictionary = _bridge.call("GetFractureDiscoveryStatusV0")
		_a.log("FRACTURE|discovery_status=%s" % str(frac_status))

	_polls = 0
	_phase = Phase.CHECK_KNOWLEDGE


# ===================== Knowledge & Stats =====================

func _do_check_knowledge() -> void:
	# Knowledge graph
	if _bridge.has_method("GetKnowledgeGraphV0"):
		var graph: Array = _bridge.call("GetKnowledgeGraphV0")
		_a.log("KNOWLEDGE|graph_entries=%d" % graph.size())
		_a.warn(graph.size() >= 1, "knowledge_graph_populated", "count=%d" % graph.size())

	if _bridge.has_method("GetKnowledgeGraphStatsV0"):
		var stats: Dictionary = _bridge.call("GetKnowledgeGraphStatsV0")
		_a.log("KNOWLEDGE|stats=%s" % str(stats))

	# Data logs
	if _bridge.has_method("GetDiscoveredDataLogsV0"):
		var logs: Array = _bridge.call("GetDiscoveredDataLogsV0")
		_a.log("KNOWLEDGE|data_logs=%d" % logs.size())

	# Narrative NPCs
	if _bridge.has_method("GetAllNarrativeNpcsV0"):
		var npcs: Array = _bridge.call("GetAllNarrativeNpcsV0")
		_a.log("KNOWLEDGE|narrative_npcs=%d" % npcs.size())

	# Station memory
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetStationMemoryV0"):
		var memory: Dictionary = _bridge.call("GetStationMemoryV0", node_id)
		_a.log("KNOWLEDGE|station_memory=%s" % str(memory))

	_polls = 0
	_phase = Phase.CHECK_STATS


func _do_check_stats() -> void:
	# Player stats
	if _bridge.has_method("GetPlayerStatsV0"):
		var stats: Dictionary = _bridge.call("GetPlayerStatsV0")
		_a.log("STATS|player=%s" % str(stats))
		_a.goal("STATS", "has_stats=%s" % str(not stats.is_empty()))

	# Milestones
	if _bridge.has_method("GetMilestonesV0"):
		var milestones: Array = _bridge.call("GetMilestonesV0")
		_a.log("STATS|milestones=%d" % milestones.size())

	# Onboarding state
	if _bridge.has_method("GetOnboardingStateV0"):
		var onboarding: Dictionary = _bridge.call("GetOnboardingStateV0")
		_a.log("STATS|onboarding=%s" % str(onboarding))

	# Empire summary
	if _bridge.has_method("GetEmpireSummaryV0"):
		var empire: Dictionary = _bridge.call("GetEmpireSummaryV0")
		_a.log("STATS|empire=%s" % str(empire))

	_polls = 0
	_phase = Phase.CHECK_LEDGER


func _do_check_ledger() -> void:
	# Transaction log
	if _bridge.has_method("GetTransactionLogV0"):
		var log: Array = _bridge.call("GetTransactionLogV0", 10)
		_a.log("LEDGER|transactions=%d" % log.size())
		_a.warn(log.size() >= 1, "transaction_log_populated", "count=%d" % log.size())
		_a.goal("ECONOMY", "ledger_entries=%d" % log.size())

	# Profit summary
	if _bridge.has_method("GetProfitSummaryV0"):
		var profit: Dictionary = _bridge.call("GetProfitSummaryV0")
		_a.log("LEDGER|profit=%s" % str(profit))

	# Economy overview
	if _bridge.has_method("GetEconomyOverviewV0"):
		var overview: Array = _bridge.call("GetEconomyOverviewV0")
		_a.log("LEDGER|economy_overview=%d" % overview.size())

	# Price history at current node for first good
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetPriceHistoryV0"):
		var history: Array = _bridge.call("GetPriceHistoryV0", node_id, "ore")
		_a.log("LEDGER|price_history=%d" % history.size())

	# Node economy snapshot (traffic, prosperity, industry, warfront)
	if _bridge.has_method("GetNodeEconomySnapshotV0"):
		var econ: Dictionary = _bridge.call("GetNodeEconomySnapshotV0", node_id)
		_a.log("ECONOMY|node=%s traffic=%s prosperity=%s industry=%s warfront=%s" % [
			node_id,
			str(econ.get("traffic_level", -1)),
			str(econ.get("prosperity", -1)),
			str(econ.get("industry_type", "none")),
			str(econ.get("warfront_tier", -1))])
		_a.hard(econ.size() >= 3, "economy_snapshot_fields", "fields=%d" % econ.size())

	# Market alerts (stockouts, price spikes/drops)
	if _bridge.has_method("GetMarketAlertsV0"):
		var alerts: Array = _bridge.call("GetMarketAlertsV0", 10)
		_a.log("ECONOMY|market_alerts=%d" % alerts.size())
		for alert in alerts:
			_a.log("ECONOMY|alert type=%s good=%s node=%s change=%s%%" % [
				str(alert.get("type", "?")),
				str(alert.get("good_id", "?")),
				str(alert.get("node_id", "?")),
				str(alert.get("change_pct", 0))])

	_polls = 0
	_phase = Phase.CHECK_OVERLAYS


# ===================== Galaxy Map Overlays =====================

func _do_check_overlays() -> void:
	if _bridge.has_method("GetFactionTerritoryOverlayV0"):
		var overlay: Dictionary = _bridge.call("GetFactionTerritoryOverlayV0")
		_a.log("OVERLAY|faction_territory=%d entries" % overlay.size())
		_a.warn(overlay.size() >= 1, "faction_overlay_populated", "count=%d" % overlay.size())

	if _bridge.has_method("GetHeatOverlayV0"):
		var heat: Dictionary = _bridge.call("GetHeatOverlayV0")
		_a.log("OVERLAY|heat=%d entries" % heat.size())

	if _bridge.has_method("GetExplorationOverlayV0"):
		var explore: Dictionary = _bridge.call("GetExplorationOverlayV0")
		_a.log("OVERLAY|exploration=%d entries" % explore.size())

	if _bridge.has_method("GetFleetPositionsOverlayV0"):
		var fleets: Dictionary = _bridge.call("GetFleetPositionsOverlayV0")
		_a.log("OVERLAY|fleet_positions=%d entries" % fleets.size())

	# Warfront overlay
	if _bridge.has_method("GetWarfrontOverlayV0"):
		var wf: Dictionary = _bridge.call("GetWarfrontOverlayV0")
		_a.log("OVERLAY|warfront=%d entries" % wf.size())

	_polls = 0
	_phase = Phase.SAVE_LOAD_TEST


# ===================== Save/Load =====================

func _do_save_load_test() -> void:
	# Capture state before save
	var ps_before: Dictionary = _bridge.call("GetPlayerStateV0")
	var credits_before := int(ps_before.get("credits", 0))
	var node_before := str(ps_before.get("current_node_id", ""))

	if _bridge.has_method("AutoSaveV0"):
		_bridge.call("AutoSaveV0")
		_a.log("SAVE|triggered")

		_busy = true
		await create_timer(0.5).timeout

		# Verify state survived (AutoSave doesn't reload, but verifies serialization)
		var ps_after: Dictionary = _bridge.call("GetPlayerStateV0")
		var credits_after := int(ps_after.get("credits", 0))
		var node_after := str(ps_after.get("current_node_id", ""))
		# Credits may shift slightly due to sim ticking during await — allow 10% drift
		var drift := absf(float(credits_after - credits_before)) / maxf(float(credits_before), 1.0)
		_a.hard(drift < 0.10, "save_credits_stable",
			"before=%d after=%d drift=%.1f%%" % [credits_before, credits_after, drift * 100])
		_a.hard(node_after == node_before, "save_node_stable",
			"before=%s after=%s" % [node_before, node_after])
		_busy = false
	else:
		_a.warn(false, "save_method_exists", "AutoSaveV0 missing")

	_polls = 0
	_phase = Phase.HAVEN_DEPTH


# ===================== Haven Depth =====================

func _do_haven_depth() -> void:
	# Force-discover haven if available
	if _bridge.has_method("ForceDiscoverHavenV0"):
		_bridge.call("ForceDiscoverHavenV0")
		_a.log("HAVEN|force_discovered")
		# Wait for write lock to release so read cache refreshes
		await create_timer(0.3).timeout

	# Haven status
	if _bridge.has_method("GetHavenStatusV0"):
		var haven: Dictionary = _bridge.call("GetHavenStatusV0")
		var tier := int(haven.get("tier", -1))
		var discovered: bool = haven.get("discovered", false)
		var node_id := str(haven.get("node_id", ""))
		_a.log("HAVEN|discovered=%s tier=%d node=%s" % [str(discovered), tier, node_id])
		_a.hard(discovered, "haven_discovered", "discovered=%s" % str(discovered))
		_a.goal("HAVEN", "tier=%d discovered=%s node=%s" % [tier, str(discovered), node_id])

		# Haven market
		if _bridge.has_method("GetHavenMarketV0"):
			var market: Array = _bridge.call("GetHavenMarketV0")
			_a.log("HAVEN|market_goods=%d" % market.size())
			_a.goal("HAVEN", "market_goods=%d" % market.size())

		# Haven market info
		if _bridge.has_method("GetHavenMarketInfoV0"):
			var info: Dictionary = _bridge.call("GetHavenMarketInfoV0")
			_a.log("HAVEN|market_info=%s" % str(info))

		# Haven residents
		if _bridge.has_method("GetHavenResidentsV0"):
			var residents: Array = _bridge.call("GetHavenResidentsV0")
			_a.log("HAVEN|residents=%d" % residents.size())
			_a.goal("HAVEN", "residents=%d" % residents.size())

		# Keeper state
		if _bridge.has_method("GetKeeperStateV0"):
			var keeper: Dictionary = _bridge.call("GetKeeperStateV0")
			_a.log("HAVEN|keeper=%s" % str(keeper))
			_a.warn(keeper.has("keeper_level"), "haven_keeper_state", "has_level=%s" % str(keeper.has("keeper_level")))

		# Upgrade attempt
		if _bridge.has_method("UpgradeHavenV0"):
			var upgrade_ok: bool = _bridge.call("UpgradeHavenV0")
			_a.log("HAVEN|upgrade_attempt=%s" % str(upgrade_ok))
			_a.warn(true, "haven_upgrade_called", "result=%s" % str(upgrade_ok))

	# Adaptation fragments
	if _bridge.has_method("GetAdaptationFragmentsV0"):
		var fragments: Array = _bridge.call("GetAdaptationFragmentsV0")
		_a.log("HAVEN|fragments=%d" % fragments.size())
		_a.hard(fragments.size() == 16, "fragment_count", "count=%d expected=16" % fragments.size())
		var collected := 0
		for f in fragments:
			if f.get("collected", false):
				collected += 1
		_a.goal("FRAGMENTS", "total=%d collected=%d" % [fragments.size(), collected])

	# Resonance pairs
	if _bridge.has_method("GetResonancePairsV0"):
		var pairs: Array = _bridge.call("GetResonancePairsV0")
		_a.log("HAVEN|resonance_pairs=%d" % pairs.size())
		_a.warn(pairs.size() == 8, "resonance_pair_count", "count=%d expected=8" % pairs.size())

	# Trophy wall
	if _bridge.has_method("GetTrophyWallV0"):
		var trophies: Array = _bridge.call("GetTrophyWallV0")
		_a.log("HAVEN|trophies=%d" % trophies.size())

	# Resonance chamber
	if _bridge.has_method("GetResonanceChamberV0"):
		var chamber: Dictionary = _bridge.call("GetResonanceChamberV0")
		_a.log("HAVEN|chamber=%s" % str(chamber))

	# Fabricator
	if _bridge.has_method("GetFabricatorV0"):
		var fab: Dictionary = _bridge.call("GetFabricatorV0")
		_a.log("HAVEN|fabricator=%s" % str(fab))
		_a.warn(fab.has("available"), "fabricator_state", "keys=%s" % str(fab.keys()))

	# Ancient hulls
	if _bridge.has_method("GetAncientHullsV0"):
		var hulls: Array = _bridge.call("GetAncientHullsV0")
		_a.log("HAVEN|ancient_hulls=%d" % hulls.size())
		_a.goal("HAVEN_DEPTH", "hulls=%d" % hulls.size())

	# Endgame paths
	if _bridge.has_method("GetEndgamePathsV0"):
		var paths: Dictionary = _bridge.call("GetEndgamePathsV0")
		_a.log("HAVEN|endgame_paths=%s" % str(paths))
		var available_paths: Array = paths.get("available_paths", [])
		# Endgame paths are 0 at game start — this is expected, not a warning
		_a.goal("HAVEN_DEPTH", "endgame_paths=%d" % available_paths.size())

	# Accommodation progress
	if _bridge.has_method("GetAccommodationProgressV0"):
		var accom: Dictionary = _bridge.call("GetAccommodationProgressV0")
		_a.log("HAVEN|accommodation=%s" % str(accom))

	# Communion rep
	if _bridge.has_method("GetCommunionRepV0"):
		var comm: Dictionary = _bridge.call("GetCommunionRepV0")
		_a.log("HAVEN|communion_rep=%s" % str(comm))

	_polls = 0
	_phase = Phase.ENDGAME_CHECK


# ===================== Endgame State =====================

func _do_endgame_check() -> void:
	# Game result (should be 0 = not terminal during mid-game)
	if _bridge.has_method("GetGameResultV0"):
		var result: Dictionary = _bridge.call("GetGameResultV0")
		var result_code := int(result.get("result", -1))
		var result_name := str(result.get("result_name", ""))
		_a.log("ENDGAME|result=%d name=%s" % [result_code, result_name])
		_a.hard(result_code == 0, "game_not_terminal", "result=%d name=%s" % [result_code, result_name])
		_a.goal("ENDGAME", "result=%d terminal=%s" % [result_code, str(result.get("is_terminal", false))])

	# Endgame progress
	if _bridge.has_method("GetEndgameProgressV0"):
		var progress: Dictionary = _bridge.call("GetEndgameProgressV0")
		var pct := int(progress.get("completion_percent", -1))
		_a.log("ENDGAME|progress=%s" % str(progress))
		_a.hard(progress.has("completion_percent"), "endgame_progress_structure", "has_pct=%s" % str(progress.has("completion_percent")))
		_a.goal("ENDGAME", "progress_pct=%d" % pct)

	# Loss info (should return empty/default during normal play)
	if _bridge.has_method("GetLossInfoV0"):
		var loss: Dictionary = _bridge.call("GetLossInfoV0")
		_a.log("ENDGAME|loss_info_keys=%d" % loss.size())
		_a.warn(loss.has("loss_reason"), "loss_info_structure", "keys=%d" % loss.size())

	# Victory info (should return empty/default during normal play)
	if _bridge.has_method("GetVictoryInfoV0"):
		var victory: Dictionary = _bridge.call("GetVictoryInfoV0")
		_a.log("ENDGAME|victory_info_keys=%d" % victory.size())
		_a.warn(victory.has("chosen_path"), "victory_info_structure", "keys=%d" % victory.size())

	# Force-set endgame states to verify bridge data contracts
	if _bridge.has_method("ForceSetGameResultV0"):
		# Test Victory state
		_bridge.call("ForceSetGameResultV0", 1)
		var win_result: Dictionary = _bridge.call("GetGameResultV0")
		_a.hard(int(win_result.get("result", -1)) == 1, "force_victory_readback", "result=%d" % int(win_result.get("result", -1)))
		_a.hard(win_result.get("is_terminal", false) == true, "victory_is_terminal", "")

		# Test Death state
		_bridge.call("ForceSetGameResultV0", 2)
		var death_result: Dictionary = _bridge.call("GetGameResultV0")
		_a.hard(int(death_result.get("result", -1)) == 2, "force_death_readback", "result=%d" % int(death_result.get("result", -1)))

		# Restore to InProgress so later phases aren't affected
		_bridge.call("ForceSetGameResultV0", 0)
		var restored: Dictionary = _bridge.call("GetGameResultV0")
		_a.hard(int(restored.get("result", -1)) == 0, "force_restore_in_progress", "result=%d" % int(restored.get("result", -1)))

	_polls = 0
	_phase = Phase.STORY_CHECK


# ===================== Story State Machine =====================

func _do_story_check() -> void:
	# Revelation state
	if _bridge.has_method("GetRevelationStateV0"):
		var rev: Dictionary = _bridge.call("GetRevelationStateV0")
		var count := int(rev.get("revelation_count", 0))
		_a.log("STORY|revelations=%d act=%s" % [count, str(rev.get("current_act", ""))])
		_a.hard(rev.has("revelation_count"), "revelation_state_structure", "has_count=%s" % str(rev.has("revelation_count")))
		_a.goal("STORY", "revelations=%d" % count)

	# Story progress
	if _bridge.has_method("GetStoryProgressV0"):
		var progress: Dictionary = _bridge.call("GetStoryProgressV0")
		_a.log("STORY|progress=%s" % str(progress))

	# Pentagon state
	if _bridge.has_method("GetPentagonStateV0"):
		var pent: Dictionary = _bridge.call("GetPentagonStateV0")
		var all_traded: bool = pent.get("all_traded", false)
		var cascade: bool = pent.get("cascade_active", false)
		_a.log("STORY|pentagon all_traded=%s cascade=%s" % [str(all_traded), str(cascade)])
		_a.warn(pent.has("all_traded"), "pentagon_state_complete", "keys=%s" % str(pent.keys()))
		_a.goal("STORY", "pentagon_traded=%s cascade=%s" % [str(all_traded), str(cascade)])

	# Cascade effects
	if _bridge.has_method("GetCascadeEffectsV0"):
		var cascade: Dictionary = _bridge.call("GetCascadeEffectsV0")
		_a.log("STORY|cascade_effects=%s" % str(cascade))

	# Pending revelation
	if _bridge.has_method("GetPendingRevelationV0"):
		var pending: Dictionary = _bridge.call("GetPendingRevelationV0")
		_a.log("STORY|pending=%s" % str(pending))

	_polls = 0
	_phase = Phase.FLEET_MANAGEMENT


# ===================== Fleet Management =====================

func _do_fleet_management() -> void:
	# Advance phase FIRST to prevent re-entry if any bridge call crashes
	_polls = 0
	_phase = Phase.DIPLOMACY_CHECK

	# Doctrine status
	if _bridge.has_method("GetDoctrineStatusV0"):
		var doctrine: Dictionary = _bridge.call("GetDoctrineStatusV0", "fleet_trader_1")
		_a.log("FLEET|doctrine=%s" % str(doctrine))
		_a.warn(doctrine.has("escort_active"), "doctrine_status", "keys=%s" % str(doctrine.keys()))
		_a.goal("FLEET", "doctrine_checked=true")

	# Survey unlock check
	if _bridge.has_method("IsSurveyUnlockedV0"):
		var unlocked: bool = _bridge.call("IsSurveyUnlockedV0", "SIGNAL")
		_a.log("FLEET|survey_signal_unlocked=%s" % str(unlocked))
		_a.goal("FLEET", "survey_unlocked=%s" % str(unlocked))

	# Survey program status
	if _bridge.has_method("GetSurveyProgramStatusV0"):
		var survey: Dictionary = _bridge.call("GetSurveyProgramStatusV0")
		_a.log("FLEET|survey_programs=%s" % str(survey))


# ===================== Diplomacy =====================

func _do_diplomacy_check() -> void:
	_polls = 0
	_phase = Phase.MEGAPROJECT_DEPTH

	if _bridge.has_method("GetActiveTreatiesV0"):
		var treaties: Array = _bridge.call("GetActiveTreatiesV0")
		_a.log("DIPLOMACY|treaties=%d" % treaties.size())
		_a.goal("DIPLOMACY", "treaties_queried=true count=%d" % treaties.size())

	if _bridge.has_method("GetAvailableBountiesV0"):
		var bounties: Array = _bridge.call("GetAvailableBountiesV0")
		_a.log("DIPLOMACY|bounties=%d" % bounties.size())

	if _bridge.has_method("GetDiplomaticProposalsV0"):
		var proposals: Array = _bridge.call("GetDiplomaticProposalsV0")
		_a.log("DIPLOMACY|proposals=%d" % proposals.size())

	if _bridge.has_method("GetSanctionsV0"):
		var sanctions: Array = _bridge.call("GetSanctionsV0")
		_a.log("DIPLOMACY|sanctions=%d" % sanctions.size())

	# Faction colors (T44 visual identity)
	if _bridge.has_method("GetFactionColorsV0"):
		var colors: Dictionary = _bridge.call("GetFactionColorsV0", "concord")
		_a.log("DIPLOMACY|faction_colors=%s" % str(colors))
		_a.warn(colors.size() >= 1, "faction_colors_populated", "fields=%d" % colors.size())


# ===================== Megaproject Depth =====================

func _do_megaproject_depth() -> void:
	_polls = 0
	_phase = Phase.BRIDGE_COVERAGE

	if _bridge.has_method("GetMegaprojectsV0"):
		var projects: Array = _bridge.call("GetMegaprojectsV0")
		_a.log("MEGAPROJECT|active=%d" % projects.size())

		# If any active project, query its detail
		if projects.size() > 0 and _bridge.has_method("GetMegaprojectDetailV0"):
			var pid := str(projects[0].get("id", ""))
			if not pid.is_empty():
				var detail: Dictionary = _bridge.call("GetMegaprojectDetailV0", pid)
				_a.log("MEGAPROJECT|detail=%s" % str(detail))
				_a.warn(detail.size() >= 1, "megaproject_detail_populated", "fields=%d" % detail.size())

	if _bridge.has_method("GetMegaprojectTypesV0"):
		var types: Array = _bridge.call("GetMegaprojectTypesV0")
		_a.log("MEGAPROJECT|types=%d" % types.size())
		_a.warn(types.size() >= 1, "megaproject_types_exist", "count=%d" % types.size())

	# Attempt to start a megaproject at current node (may fail gracefully — that's fine)
	var mp_ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var mp_nid := str(mp_ps.get("current_node_id", ""))
	if _bridge.has_method("StartMegaprojectV0") and _bridge.has_method("GetMegaprojectTypesV0"):
		var mp_types: Array = _bridge.call("GetMegaprojectTypesV0")
		if mp_types.size() > 0:
			var type_id := str(mp_types[0].get("id", mp_types[0].get("type_id", "")))
			if not type_id.is_empty():
				var mp_result: Dictionary = _bridge.call("StartMegaprojectV0", type_id, mp_nid)
				_a.log("MEGAPROJECT|start_attempt type=%s node=%s result=%s" % [type_id, mp_nid, str(mp_result)])
				_a.goal("MEGAPROJECT", "start_attempted=true")


func _do_bridge_coverage() -> void:
	# Sweep of previously-UNCALLED read-only bridge methods.
	# Each call verifies the method exists and returns non-null.
	_phase = Phase.AUDIT
	var covered := 0
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	var snap: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var nodes: Array = snap.get("system_nodes", [])
	var node2 := str(nodes[1].get("node_id", "")) if nodes.size() > 1 else node_id

	# --- Simple no-arg queries ---
	var no_arg_methods := [
		"GetCaptainNameV0", "GetCreditHistoryV0", "GetLastCombatLogV0",
		"GetLootDiagV0", "GetAllIndustryV0", "GetAllNodeHealthSummaryV0",
		"GetActiveCommissionV0", "GetActiveWarConsequencesV0",
		"GetCargoFractureWeightV0", "GetDiscoveryPhaseMarkersV0",
		"GetMutableEdgesV0", "GetHavenLogsV0", "GetTotalUpkeepV0",
	]
	for method_name in no_arg_methods:
		if _bridge.has_method(method_name):
			var result = _bridge.call(method_name)
			_a.warn(result != null, "bc_%s" % method_name, "returned null")
			covered += 1

	# --- Single-arg queries (node_id) ---
	var node_arg_methods := [
		"GetInstabilityEffectsV0", "GetNodeIndustryStatusV0",
		"GetNpcDemandV0", "GetNarrativeNpcsAtNodeV0",
	]
	for method_name in node_arg_methods:
		if _bridge.has_method(method_name):
			var result = _bridge.call(method_name, node_id)
			_a.warn(result != null, "bc_%s" % method_name, "returned null")
			covered += 1

	# --- Single-arg queries (faction_id) ---
	var faction_arg_methods := [
		"GetFactionDetailV0", "GetRepModifierStackV0",
	]
	for method_name in faction_arg_methods:
		if _bridge.has_method(method_name):
			var result = _bridge.call(method_name, "concord")
			_a.warn(result != null, "bc_%s" % method_name, "returned null")
			covered += 1

	# --- Specialized queries ---
	if _bridge.has_method("GetIndustryEventsV0"):
		var result = _bridge.call("GetIndustryEventsV0", 0)
		_a.warn(result != null, "bc_GetIndustryEventsV0", "returned null")
		covered += 1

	if _bridge.has_method("GetDualReadingsV0"):
		var result = _bridge.call("GetDualReadingsV0", node_id, "fuel")
		_a.warn(result != null, "bc_GetDualReadingsV0", "returned null")
		covered += 1

	if _bridge.has_method("GetDomainForecastV0"):
		var result = _bridge.call("GetDomainForecastV0", "economy")
		_a.warn(result != null, "bc_GetDomainForecastV0", "returned null")
		covered += 1

	if _bridge.has_method("GetRoutePathV0"):
		var result = _bridge.call("GetRoutePathV0", node2)
		_a.warn(result != null, "bc_GetRoutePathV0", "returned null")
		covered += 1

	if _bridge.has_method("GetRouteEtaRangeV0") and snap.get("lane_edges", []).size() > 0:
		var edge_id := str(snap.get("lane_edges", [])[0].get("edge_id", ""))
		if not edge_id.is_empty():
			var result = _bridge.call("GetRouteEtaRangeV0", edge_id)
			_a.warn(result != null, "bc_GetRouteEtaRangeV0", "returned null")
			covered += 1

	if _bridge.has_method("GetSecurityBandV0"):
		var result = _bridge.call("GetSecurityBandV0", node_id, node2)
		_a.warn(result != null, "bc_GetSecurityBandV0", "returned null")
		covered += 1

	if _bridge.has_method("GetSystemSearchV0"):
		var result = _bridge.call("GetSystemSearchV0", "star")
		_a.warn(result != null, "bc_GetSystemSearchV0", "returned null")
		covered += 1

	if _bridge.has_method("GetTechRequirementsV0"):
		var result = _bridge.call("GetTechRequirementsV0", "tech_fracture_drive")
		_a.warn(result != null, "bc_GetTechRequirementsV0", "returned null")
		covered += 1

	if _bridge.has_method("GetPressureAlertCountV0"):
		var result: int = _bridge.call("GetPressureAlertCountV0", "economy")
		_a.warn(result >= -1, "bc_GetPressureAlertCountV0", "val=%d" % result)
		covered += 1

	if _bridge.has_method("GetFleetShipDetailV0"):
		var result = _bridge.call("GetFleetShipDetailV0", "fleet_trader_1")
		_a.warn(result != null, "bc_GetFleetShipDetailV0", "returned null")
		covered += 1

	if _bridge.has_method("GetPatrolStatusV0"):
		var result = _bridge.call("GetPatrolStatusV0", "nonexistent")
		_a.warn(result != null, "bc_GetPatrolStatusV0", "returned null")
		covered += 1

	if _bridge.has_method("GetFragmentLoreV0"):
		var result = _bridge.call("GetFragmentLoreV0", "frag_test")
		_a.warn(result != null, "bc_GetFragmentLoreV0", "returned null")
		covered += 1

	if _bridge.has_method("GetAnomalyEncounterSnapshotV0"):
		var result = _bridge.call("GetAnomalyEncounterSnapshotV0", "enc_test")
		_a.warn(result != null, "bc_GetAnomalyEncounterSnapshotV0", "returned null")
		covered += 1

	# --- Mutation methods (safe in test context) ---
	if _bridge.has_method("FleetRenameV0"):
		var result = _bridge.call("FleetRenameV0", "fleet_trader_1", "TestShip")
		_a.warn(result != null, "bc_FleetRenameV0", "returned null")
		covered += 1

	if _bridge.has_method("CreatePatrolProgramV0"):
		var result = _bridge.call("CreatePatrolProgramV0", "fleet_trader_1", node_id, node2, 30)
		_a.warn(result is String, "bc_CreatePatrolProgramV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("CreateSurveyProgramV0"):
		var result = _bridge.call("CreateSurveyProgramV0", "ore", node_id, 2, 60)
		_a.warn(result is String, "bc_CreateSurveyProgramV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("FleetRecallV0"):
		var result = _bridge.call("FleetRecallV0", "fleet_trader_1")
		_a.warn(result != null, "bc_FleetRecallV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("FleetDismissV0"):
		# Try dismissing a non-hero ship (should fail gracefully if none exist)
		var result = _bridge.call("FleetDismissV0", "fleet_nonexistent_99")
		_a.warn(result != null, "bc_FleetDismissV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("SetEscortDoctrineV0"):
		var result = _bridge.call("SetEscortDoctrineV0", "fleet_trader_1", "escort", false, "")
		_a.warn(result is Dictionary, "bc_SetEscortDoctrineV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("ProposeTreatyV0"):
		var result = _bridge.call("ProposeTreatyV0", "concord")
		_a.warn(result != null, "bc_ProposeTreatyV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("CollectFragmentV0"):
		var result = _bridge.call("CollectFragmentV0", "frag_nonexistent")
		_a.warn(result is Dictionary, "bc_CollectFragmentV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("DispatchSupplyRepairV0"):
		var result = _bridge.call("DispatchSupplyRepairV0", "site_nonexistent", 1)
		_a.warn(result is Dictionary, "bc_DispatchSupplyRepairV0", "result=%s" % str(result))
		covered += 1

	if _bridge.has_method("ForceStartResearchV0"):
		_bridge.call("ForceStartResearchV0")
		_a.warn(true, "bc_ForceStartResearchV0", "called")
		covered += 1

	_a.log("BRIDGE_COVERAGE|methods_exercised=%d" % covered)
	_a.warn(covered >= 34, "bridge_coverage_sweep", "exercised=%d (target>=34)" % covered)


# ===================== Audit =====================

func _do_audit() -> void:
	# Pressure domains
	if _bridge.has_method("GetPressureDomainsV0"):
		var domains: Array = _bridge.call("GetPressureDomainsV0")
		_a.log("PRESSURE|domains=%d" % domains.size())

	# Risk meters
	if _bridge.has_method("GetRiskMetersV0"):
		var risk: Dictionary = _bridge.call("GetRiskMetersV0")
		_a.log("RISK|meters=%s" % str(risk))

	# Haven status
	if _bridge.has_method("GetHavenStatusV0"):
		var haven: Dictionary = _bridge.call("GetHavenStatusV0")
		_a.log("HAVEN|status=%s" % str(haven))
		_a.goal("HAVEN", "tier=%s" % str(haven.get("tier", "unknown")))

	# NPC trade activity
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id := str(ps.get("current_node_id", ""))
	if _bridge.has_method("GetNpcTradeActivityV0"):
		var activity := int(_bridge.call("GetNpcTradeActivityV0", node_id))
		_a.log("NPC|trade_activity=%d node=%s" % [activity, node_id])

	# Security
	if _bridge.has_method("GetNodeSecurityV0"):
		var sec := int(_bridge.call("GetNodeSecurityV0", node_id))
		_a.log("SECURITY|node=%s level=%d" % [node_id, sec])

	_a.summary()
	_phase = Phase.DONE


func _do_done() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(_a.exit_code())


# ===================== Helpers =====================

func _get_neighbors() -> Array:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var current := str(ps.get("current_node_id", ""))
	var result: Array = []
	for lane in _all_edges:
		var from_id := str(lane.get("from_id", ""))
		var to_id := str(lane.get("to_id", ""))
		if from_id == current and not to_id in result:
			result.append(to_id)
		elif to_id == current and not from_id in result:
			result.append(from_id)
	return result


func _headless_travel(dest: String) -> void:
	if _bridge != null:
		if _bridge.has_method("DispatchTravelCommandV0"):
			_bridge.call("DispatchTravelCommandV0", "fleet_trader_1", dest)
		if _bridge.has_method("DispatchPlayerArriveV0"):
			_bridge.call("DispatchPlayerArriveV0", dest)
	if _gm != null:
		_gm.set("_lane_cooldown_v0", 0.0)
		if _gm.get("current_player_state") != null:
			_gm.call("on_lane_gate_proximity_entered_v0", dest)
			_gm.call("on_lane_arrival_v0", dest)


func _dock_at_station() -> void:
	if _gm == null:
		return
	var targets = get_nodes_in_group("Station")
	if targets.is_empty():
		targets = get_nodes_in_group("Planet")
	if targets.size() > 0:
		_gm.call("on_proximity_dock_entered_v0", targets[0])
