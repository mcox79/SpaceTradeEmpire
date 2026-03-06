extends SceneTree

## Visual Sweep Bot v2 — Drives game through visual states, capturing screenshots.
## Run WINDOWED (not --headless) — screenshots require a framebuffer.
##
## Usage:
##   & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . -s "res://scripts/tests/visual_sweep_bot_v0.gd"

const PREFIX := "VSWP|"
const MAX_POLLS := 600
const OUTPUT_DIR := "res://reports/visual_eval/"

const ObserverScript = preload("res://scripts/tools/experience_observer.gd")
const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")
const AuditScript = preload("res://scripts/tools/aesthetic_audit.gd")

enum Phase {
	# --- Setup ---
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,

	# --- Home System ---
	BOOT,                      # 1. Flight view: star, planets, station, NPC fleets, HUD
	DOCK_ENTER,
	DOCK_MARKET_CAPTURE,       # 2. Market tab: goods, prices, station name
	DOCK_JOBS_SWITCH,
	DOCK_JOBS_CAPTURE,         # 3. Jobs tab: missions, automation
	DOCK_SERVICES_SWITCH,
	DOCK_SERVICES_CAPTURE,     # 4. Services tab: refit, maintenance, research
	BUY_GOOD,
	POST_BUY_CAPTURE,          # 5. Market tab with updated quantities after purchase
	UNDOCK_1,
	FLIGHT_CARGO_CAPTURE,      # 6. Flight HUD with cargo loaded

	# --- NPC Showcase (Tranche 15) ---
	NPC_ZOOM_IN,               # Lower camera to 30u for close-up
	NPC_APPROACH,              # Teleport hero near nearest NPC ship
	NPC_CLOSEUP_CAPTURE,       # 7. NPC close-up: role label (T/H/P), 3D ship model
	NPC_DAMAGE,                # Apply damage hits to nearby NPC
	NPC_COMBAT_CAPTURE,        # 8. NPC combat: HP bar depleting, stagger
	NPC_WARP_VFX,              # Spawn WarpEffect.play_warp_in near hero
	NPC_WARP_VFX_CAPTURE,      # 9. Warp-in VFX: blue-white particles + flash sphere
	NPC_ZOOM_OUT,              # Restore camera to normal height

	# --- Overlays ---
	OPEN_GALAXY_MAP,
	GALAXY_MAP_CAPTURE,        # 10. Galaxy map: network graph, node colors, YOU indicator
	CLOSE_GALAXY_MAP,
	OPEN_EMPIRE_DASH,
	EMPIRE_DASH_CAPTURE,       # 11. Empire dashboard overlay
	CLOSE_EMPIRE_DASH,

	# --- Warp to System 2 (with visual warp effect) ---
	WARP_2_TRIGGER,            # Trigger lane gate proximity → flash + camera shake
	WARP_2_CAPTURE,            # 12. Warp transit: screen flash mid-fade
	WARP_2_WAIT,               # Wait for async arrival to complete
	WARP_2_REBUILD,
	SYSTEM_2_CAPTURE,          # 13. System 2: different star type, different station
	SYSTEM_2_DOCK,
	SYSTEM_2_MARKET_CAPTURE,   # 14. System 2 market: different goods/prices
	SYSTEM_2_UNDOCK,

	# --- Warp to System 3 ---
	WARP_3_COOLDOWN,           # Wait for lane cooldown to expire (2s)
	WARP_3_TRIGGER,
	WARP_3_WAIT,
	WARP_3_REBUILD,
	SYSTEM_3_CAPTURE,          # 15. System 3: third star variety

	# --- Time Advancement ---
	WAIT_TICK_200,
	TICK_200_CAPTURE,          # 16. Economy evolution at tick 200

	# --- Final ---
	FINAL,                     # 17. Aesthetic audit + final capture
	DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _bridge = null
var _game_manager = null

var _observer = null
var _screenshot = null
var _audit = null

# Navigation state
var _home_node_id := ""
var _neighbor_ids: Array = []
var _trade_good := ""
var _snapshots: Array = []  # [{phase, tick, screenshot, report}]


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		# ── Setup ──────────────────────────────────────────────
		Phase.LOAD_SCENE:
			var scene = load("res://scenes/playable_prototype.tscn").instantiate()
			root.add_child(scene)
			print(PREFIX + "SCENE_LOADED")
			_polls = 0
			_phase = Phase.WAIT_SCENE

		Phase.WAIT_SCENE:
			_polls += 1
			if _polls >= 30:
				_polls = 0
				_phase = Phase.WAIT_BRIDGE

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
				_observer = ObserverScript.new()
				_observer.init_v0(self)
				_screenshot = ScreenshotScript.new()
				_audit = AuditScript.new()
				_game_manager = root.get_node_or_null("GameManager")
				_init_navigation()
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			if get_nodes_in_group("Station").size() > 0:
				_polls = 0
				_phase = Phase.BOOT
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_phase = Phase.BOOT

		# ── Home System ───────────────────────────────────────
		Phase.BOOT:
			_polls += 1
			if _polls >= 30:
				_capture("boot")
				_polls = 0
				_phase = Phase.DOCK_ENTER

		Phase.DOCK_ENTER:
			_dock_at_current_station()
			_polls = 0
			_phase = Phase.DOCK_MARKET_CAPTURE

		Phase.DOCK_MARKET_CAPTURE:
			_polls += 1
			if _polls >= 5:
				_capture("dock_market")
				_polls = 0
				_phase = Phase.DOCK_JOBS_SWITCH

		Phase.DOCK_JOBS_SWITCH:
			_switch_dock_tab(1)
			_polls = 0
			_phase = Phase.DOCK_JOBS_CAPTURE

		Phase.DOCK_JOBS_CAPTURE:
			_polls += 1
			if _polls >= 5:
				_capture("dock_jobs")
				_polls = 0
				_phase = Phase.DOCK_SERVICES_SWITCH

		Phase.DOCK_SERVICES_SWITCH:
			_switch_dock_tab(2)
			_polls = 0
			_phase = Phase.DOCK_SERVICES_CAPTURE

		Phase.DOCK_SERVICES_CAPTURE:
			_polls += 1
			if _polls >= 5:
				_capture("dock_services")
				_polls = 0
				_phase = Phase.BUY_GOOD

		Phase.BUY_GOOD:
			_try_buy_good()
			# Switch back to market tab to show updated quantities
			_switch_dock_tab(0)
			_polls = 0
			_phase = Phase.POST_BUY_CAPTURE

		Phase.POST_BUY_CAPTURE:
			_polls += 1
			if _polls >= 5:
				if not _trade_good.is_empty():
					_capture("post_buy")
				_polls = 0
				_phase = Phase.UNDOCK_1

		Phase.UNDOCK_1:
			_undock()
			_polls = 0
			_phase = Phase.FLIGHT_CARGO_CAPTURE

		Phase.FLIGHT_CARGO_CAPTURE:
			_polls += 1
			if _polls >= 10:
				_capture("flight_cargo")
				_polls = 0
				_phase = Phase.NPC_ZOOM_IN

		# ── NPC Showcase ──────────────────────────────────────
		Phase.NPC_ZOOM_IN:
			_set_camera_distance(30.0)
			_polls = 0
			_phase = Phase.NPC_APPROACH

		Phase.NPC_APPROACH:
			_approach_nearest_npc()
			_polls = 0
			_phase = Phase.NPC_CLOSEUP_CAPTURE

		Phase.NPC_CLOSEUP_CAPTURE:
			# Wait for camera to lerp to new position + label visibility update
			_polls += 1
			if _polls >= 25:
				_capture("npc_closeup")
				_polls = 0
				_phase = Phase.NPC_DAMAGE

		Phase.NPC_DAMAGE:
			# Hit nearest NPC 5 times to deplete HP and show bar
			_damage_nearest_npc(5, 20)
			_polls = 0
			_phase = Phase.NPC_COMBAT_CAPTURE

		Phase.NPC_COMBAT_CAPTURE:
			_polls += 1
			if _polls >= 5:
				_capture("npc_combat")
				_polls = 0
				_phase = Phase.NPC_WARP_VFX

		Phase.NPC_WARP_VFX:
			_spawn_warp_in_vfx()
			_polls = 0
			_phase = Phase.NPC_WARP_VFX_CAPTURE

		Phase.NPC_WARP_VFX_CAPTURE:
			# Capture 4 frames in — flash sphere still ~2x scale, particles bursting
			_polls += 1
			if _polls >= 4:
				_capture("warp_in_vfx")
				_polls = 0
				_phase = Phase.NPC_ZOOM_OUT

		Phase.NPC_ZOOM_OUT:
			_set_camera_distance(80.0)
			_polls = 0
			_phase = Phase.OPEN_GALAXY_MAP

		# ── Overlays ──────────────────────────────────────────
		Phase.OPEN_GALAXY_MAP:
			_toggle_galaxy_map()
			_polls = 0
			_phase = Phase.GALAXY_MAP_CAPTURE

		Phase.GALAXY_MAP_CAPTURE:
			_polls += 1
			if _polls >= 8:
				_capture("galaxy_map")
				_polls = 0
				_phase = Phase.CLOSE_GALAXY_MAP

		Phase.CLOSE_GALAXY_MAP:
			_toggle_galaxy_map()
			_polls = 0
			_phase = Phase.OPEN_EMPIRE_DASH

		Phase.OPEN_EMPIRE_DASH:
			_polls += 1
			if _polls >= 3:
				_toggle_empire_dashboard()
				_polls = 0
				_phase = Phase.EMPIRE_DASH_CAPTURE

		Phase.EMPIRE_DASH_CAPTURE:
			_polls += 1
			if _polls >= 8:
				_capture("empire_dashboard")
				_polls = 0
				_phase = Phase.CLOSE_EMPIRE_DASH

		Phase.CLOSE_EMPIRE_DASH:
			_toggle_empire_dashboard()
			_polls = 0
			_phase = Phase.WARP_2_TRIGGER

		# ── Warp to System 2 ──────────────────────────────────
		Phase.WARP_2_TRIGGER:
			_polls += 1
			if _polls >= 5:
				if _neighbor_ids.size() >= 1:
					# Use GameManager warp flow for visual effects (flash + shake)
					_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[0])
					print(PREFIX + "WARP_TRIGGER|%s" % _neighbor_ids[0])
					_polls = 0
					_phase = Phase.WARP_2_CAPTURE
				else:
					print(PREFIX + "WARN|no_neighbors, skipping warp")
					_phase = Phase.WAIT_TICK_200

		Phase.WARP_2_CAPTURE:
			# Capture 3 frames after trigger — flash still fading (alpha ~0.7)
			_polls += 1
			if _polls >= 3:
				_capture("warp_transit")
				_polls = 0
				_phase = Phase.WARP_2_WAIT

		Phase.WARP_2_WAIT:
			# Wait for async _begin_lane_transit_v0 to complete (~0.3s + arrival)
			_polls += 1
			if _polls >= 40:
				_polls = 0
				_phase = Phase.WARP_2_REBUILD

		Phase.WARP_2_REBUILD:
			# Rebuild local system visuals for new system
			_rebuild_local_system(_neighbor_ids[0])
			_polls = 0
			_phase = Phase.SYSTEM_2_CAPTURE

		Phase.SYSTEM_2_CAPTURE:
			_polls += 1
			if _polls >= 30:
				_capture("system_2")
				_polls = 0
				_phase = Phase.SYSTEM_2_DOCK

		Phase.SYSTEM_2_DOCK:
			_dock_at_current_station()
			_polls = 0
			_phase = Phase.SYSTEM_2_MARKET_CAPTURE

		Phase.SYSTEM_2_MARKET_CAPTURE:
			_polls += 1
			if _polls >= 5:
				_capture("system_2_dock")
				_polls = 0
				_phase = Phase.SYSTEM_2_UNDOCK

		Phase.SYSTEM_2_UNDOCK:
			_undock()
			_polls = 0
			_phase = Phase.WARP_3_COOLDOWN

		# ── Warp to System 3 ──────────────────────────────────
		Phase.WARP_3_COOLDOWN:
			# Lane cooldown is 2.0s — wait 150 frames (~2.5s at 60fps) for it to expire
			_polls += 1
			if _polls >= 150:
				_polls = 0
				_phase = Phase.WARP_3_TRIGGER

		Phase.WARP_3_TRIGGER:
			if _neighbor_ids.size() >= 2:
				_game_manager.call("on_lane_gate_proximity_entered_v0", _neighbor_ids[1])
				print(PREFIX + "WARP_TRIGGER|%s" % _neighbor_ids[1])
				_polls = 0
				_phase = Phase.WARP_3_WAIT
			else:
				print(PREFIX + "WARN|no_second_neighbor, skipping system_3")
				_phase = Phase.WAIT_TICK_200

		Phase.WARP_3_WAIT:
			_polls += 1
			if _polls >= 40:
				_polls = 0
				_phase = Phase.WARP_3_REBUILD

		Phase.WARP_3_REBUILD:
			_rebuild_local_system(_neighbor_ids[1])
			_polls = 0
			_phase = Phase.SYSTEM_3_CAPTURE

		Phase.SYSTEM_3_CAPTURE:
			_polls += 1
			if _polls >= 30:
				_capture("system_3")
				_polls = 0
				_phase = Phase.WAIT_TICK_200

		# ── Time Advancement ──────────────────────────────────
		Phase.WAIT_TICK_200:
			var tick = _get_tick()
			if tick >= 200:
				_polls = 0
				_phase = Phase.TICK_200_CAPTURE
			else:
				_polls += 1
				if _polls >= 300:
					_polls = 0
					_phase = Phase.TICK_200_CAPTURE

		Phase.TICK_200_CAPTURE:
			_capture("tick_200")
			_phase = Phase.FINAL

		# ── Final ─────────────────────────────────────────────
		Phase.FINAL:
			var report = _observer.capture_full_report_v0()
			var audit_results = _audit.run_audit_v0(report)
			var critical_fails = _audit.count_critical_failures_v0(audit_results)

			for ar in audit_results:
				var status = "PASS" if ar.get("passed", false) else "FAIL"
				print(PREFIX + "AESTHETIC|%s|%s|%s|%s" % [
					status, str(ar.get("flag", "")),
					str(ar.get("severity", "")), str(ar.get("detail", ""))])

			print(PREFIX + "AESTHETIC_CRITICAL_FAILS|%d" % critical_fails)
			_capture("final")

			_write_summary(audit_results, critical_fails)

			if critical_fails == 0:
				print(PREFIX + "PASS|screenshots=%d" % _snapshots.size())
			else:
				print(PREFIX + "FAIL|critical=%d screenshots=%d" % [critical_fails, _snapshots.size()])

			_phase = Phase.DONE

		Phase.DONE:
			_quit()

	return false


# --- Navigation helpers ---

func _init_navigation() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	_home_node_id = str(ps.get("current_node_id", ""))

	var galaxy: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var lanes: Array = galaxy.get("lane_edges", [])
	var seen := {}
	for lane in lanes:
		var from_id: String = str(lane.get("from_id", ""))
		var to_id: String = str(lane.get("to_id", ""))
		if from_id == _home_node_id and not seen.has(to_id):
			_neighbor_ids.append(to_id)
			seen[to_id] = true
		elif to_id == _home_node_id and not seen.has(from_id):
			_neighbor_ids.append(from_id)
			seen[from_id] = true

	print(PREFIX + "NAV|home=%s neighbors=%d" % [_home_node_id, _neighbor_ids.size()])


func _rebuild_local_system(node_id: String) -> void:
	var gv = root.find_child("GalaxyView", true, false)
	if gv and gv.has_method("RebuildLocalSystemV0"):
		gv.call("RebuildLocalSystemV0", node_id)
	print(PREFIX + "REBUILD|%s" % node_id)


func _dock_at_current_station() -> void:
	if _game_manager == null:
		return
	var stations = get_nodes_in_group("Station")
	if stations.size() > 0:
		_game_manager.call("on_proximity_dock_entered_v0", stations[0])
		print(PREFIX + "DOCK|%s" % str(stations[0].name))


func _undock() -> void:
	if _game_manager != null and _game_manager.has_method("undock_v0"):
		_game_manager.call("undock_v0")
		print(PREFIX + "UNDOCK")


func _toggle_galaxy_map() -> void:
	if _game_manager != null and _game_manager.has_method("toggle_galaxy_map_overlay_v0"):
		_game_manager.call("toggle_galaxy_map_overlay_v0")


func _toggle_empire_dashboard() -> void:
	if _game_manager != null:
		_game_manager.call("_toggle_empire_dashboard_v0")


func _switch_dock_tab(idx: int) -> void:
	var htm = _find_hero_trade_menu()
	if htm != null and htm.has_method("_switch_dock_tab"):
		htm.call("_switch_dock_tab", idx)


func _find_hero_trade_menu():
	return root.find_child("HeroTradeMenu", true, false)


func _try_buy_good() -> void:
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		return
	var market: Array = _bridge.call("GetPlayerMarketViewV0", node_id)
	for item in market:
		if int(item.get("quantity", 0)) > 0:
			_trade_good = str(item.get("good_id", ""))
			_bridge.call("DispatchPlayerTradeV0", node_id, _trade_good, 1, true)
			print(PREFIX + "BUY|%s" % _trade_good)
			return
	print(PREFIX + "WARN|no_goods_to_buy")


# --- NPC showcase helpers ---

var _saved_cam_distance: float = 80.0

func _set_camera_distance(dist: float) -> void:
	var cam_ctrl = root.find_child("PlayerFollowCamera", true, false)
	if cam_ctrl:
		_saved_cam_distance = float(cam_ctrl.get("flight_follow_distance"))
		cam_ctrl.set("flight_follow_distance", dist)
		# Also adjust offset direction to match new distance
		var offset := Vector3(0, dist, dist * 0.05)
		cam_ctrl.set("flight_offset", offset)
		print(PREFIX + "CAMERA|dist=%s" % str(dist))


func _get_hero_body():
	var players = get_nodes_in_group("Player")
	if players.size() > 0:
		return players[0]
	return null


func _find_npc_ships() -> Array:
	# Try NpcShip group first (Tranche 15 ships), then FleetShip (legacy markers)
	var npcs = get_nodes_in_group("NpcShip")
	if npcs.is_empty():
		npcs = get_nodes_in_group("FleetShip")
	return npcs


func _find_nearest_npc(hero: Node3D) -> Node3D:
	var npcs = _find_npc_ships()
	if npcs.is_empty():
		return null
	var nearest = npcs[0]
	var best_dist: float = hero.global_position.distance_to(nearest.global_position)
	for npc in npcs:
		var d: float = hero.global_position.distance_to(npc.global_position)
		if d < best_dist:
			best_dist = d
			nearest = npc
	return nearest


func _approach_nearest_npc() -> void:
	var hero = _get_hero_body()
	if hero == null:
		print(PREFIX + "WARN|no_hero_body")
		return
	var nearest = _find_nearest_npc(hero)
	if nearest == null:
		print(PREFIX + "WARN|no_npc_ships")
		return
	# Teleport hero to within 15u of the NPC (inside LABEL_SHOW_DIST of 40u)
	var dir: Vector3 = (hero.global_position - nearest.global_position).normalized()
	if dir.length_squared() < 0.01:
		dir = Vector3(1, 0, 0)
	hero.global_position = nearest.global_position + dir * 15.0
	hero.global_position.y = 0.0
	hero.linear_velocity = Vector3.ZERO
	print(PREFIX + "NPC_APPROACH|dist=15|npc=%s|has_on_hit=%s|groups=%s" % [
		str(nearest.name),
		str(nearest.has_method("on_hit")),
		str(nearest.get_groups())])


func _damage_nearest_npc(hits: int, dmg: int) -> void:
	var hero = _get_hero_body()
	if hero == null:
		return
	var nearest = _find_nearest_npc(hero)
	if nearest == null:
		print(PREFIX + "WARN|no_npc_to_damage")
		return
	# Try on_hit (NpcShip method — applies stagger + routes to bridge)
	var hit_count := 0
	if nearest.has_method("on_hit"):
		for i in range(hits):
			nearest.call("on_hit", dmg)
			hit_count += 1
	else:
		# Fallback: damage via bridge directly using fleet_id from metadata
		var fleet_id: String = ""
		if nearest.has_meta("fleet_id"):
			fleet_id = str(nearest.get_meta("fleet_id"))
		elif nearest.has_method("get") and nearest.get("fleet_id") != null:
			fleet_id = str(nearest.get("fleet_id"))
		if not fleet_id.is_empty() and _bridge != null and _bridge.has_method("DamageNpcFleetV0"):
			for i in range(hits):
				_bridge.call("DamageNpcFleetV0", fleet_id, dmg)
				hit_count += 1
	print(PREFIX + "NPC_DAMAGE|hits=%d|dmg=%d|target=%s" % [hit_count, dmg, str(nearest.name)])


func _spawn_warp_in_vfx() -> void:
	var hero = _get_hero_body()
	if hero == null:
		return
	# Inline warp-in VFX (avoids preload parse issues with warp_effect.gd)
	var pos: Vector3 = hero.global_position + Vector3(10, 0, 0)
	var effect := Node3D.new()
	effect.name = "WarpInVfx"
	effect.position = pos
	root.add_child(effect)

	# Particle burst (blue-white)
	var particles := GPUParticles3D.new()
	particles.amount = 16
	particles.lifetime = 0.6
	particles.one_shot = true
	particles.explosiveness = 0.95
	var pmat := ParticleProcessMaterial.new()
	pmat.direction = Vector3.ZERO
	pmat.spread = 180.0
	pmat.initial_velocity_min = 5.0
	pmat.initial_velocity_max = 15.0
	pmat.gravity = Vector3.ZERO
	pmat.scale_min = 0.2
	pmat.scale_max = 0.5
	pmat.color = Color(0.4, 0.7, 1.0, 1.0)
	particles.process_material = pmat
	var pmesh := SphereMesh.new()
	pmesh.radius = 0.2
	pmesh.height = 0.4
	particles.draw_pass_1 = pmesh
	effect.add_child(particles)
	particles.emitting = true

	# Flash sphere (shrinks from 4x to 0.1x)
	var flash := MeshInstance3D.new()
	flash.name = "WarpFlash"
	var sphere := SphereMesh.new()
	sphere.radius = 1.5
	sphere.height = 3.0
	flash.mesh = sphere
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.5, 0.8, 1.0, 0.7)
	mat.emission_enabled = true
	mat.emission = Color(0.5, 0.8, 1.0, 1.0)
	mat.emission_energy_multiplier = 5.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	flash.material_override = mat
	effect.add_child(flash)

	var tween := effect.create_tween()
	tween.tween_property(flash, "scale", Vector3(0.1, 0.1, 0.1), 0.8).from(Vector3(4.0, 4.0, 4.0))
	tween.parallel().tween_property(mat, "albedo_color:a", 0.0, 0.8).from(0.7)
	tween.tween_callback(effect.queue_free)

	print(PREFIX + "WARP_VFX|pos=%s" % str(pos))


# --- Capture helpers ---

func _capture(label: String) -> void:
	var tick := _get_tick()
	var filename := "%s_%04d" % [label, tick]

	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)

	var report = _observer.capture_full_report_v0()
	var report_path := OUTPUT_DIR.path_join(filename + "_report.json")
	_observer.write_report_json_v0(report, report_path)

	_snapshots.append({
		"phase": label,
		"tick": tick,
		"screenshot": img_path,
		"report": report_path,
	})
	print(PREFIX + "CAPTURE|%s|tick=%d" % [label, tick])


func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _write_summary(audit_results: Array, critical_fails: int) -> void:
	var summary := {
		"sweep_version": 3,
		"snapshot_count": _snapshots.size(),
		"snapshots": _snapshots,
		"aesthetic_audit": audit_results,
		"critical_failures": critical_fails,
	}
	DirAccess.make_dir_recursive_absolute(OUTPUT_DIR)
	var f := FileAccess.open(OUTPUT_DIR.path_join("summary.json"), FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
		print(PREFIX + "SUMMARY_SAVED")
	else:
		print(PREFIX + "SUMMARY_SAVE_FAILED")


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE


func _quit() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
