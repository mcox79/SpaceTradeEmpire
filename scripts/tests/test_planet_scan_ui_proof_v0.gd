# GATE.T43.SCAN_UI.HEADLESS_PROOF.001: Headless bot for planet scan UI proof.
# Docks at a planet, performs orbital scan, landing scan, investigate, verify results.
extends SceneTree

var _bridge = null
var _gm = null
var _pass_count: int = 0
var _fail_count: int = 0
var _warn_count: int = 0

func _init() -> void:
	print("PS1|START planet_scan_ui_proof_v0")

	# Delete stale quicksave to prevent contamination.
	var save_path: String = "user://quicksave.json"
	if FileAccess.file_exists(save_path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(save_path))
		print("PS1|CLEANUP quicksave deleted")

	# Wait for scene tree to be ready.
	await create_timer(2.0).timeout

	_gm = root.get_node_or_null("GameManager")
	if _gm == null:
		print("PS1|FAIL GameManager not found")
		quit(1)
		return

	# Wait for bridge init.
	await create_timer(3.0).timeout

	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		print("PS1|FAIL SimBridge not found")
		quit(1)
		return

	# Wait for sim to initialize.
	await create_timer(2.0).timeout

	# Check bridge has planet scan methods.
	_hard("HAS_GetScanChargesV0", _bridge.has_method("GetScanChargesV0"))
	_hard("HAS_OrbitalScanV0", _bridge.has_method("OrbitalScanV0"))
	_hard("HAS_LandingScanV0", _bridge.has_method("LandingScanV0"))
	_hard("HAS_GetPlanetScanResultsV0", _bridge.has_method("GetPlanetScanResultsV0"))
	_hard("HAS_InvestigateFindingV0", _bridge.has_method("InvestigateFindingV0"))
	_hard("HAS_GetScanAffinityV0", _bridge.has_method("GetScanAffinityV0"))
	_hard("HAS_GetPlanetInfoV0", _bridge.has_method("GetPlanetInfoV0"))

	# Get player state.
	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var start_node: String = str(ps.get("current_node_id", ""))
	print("PS1|PLAYER_AT %s" % start_node)

	# Find a planet node to test at.
	var planet_node_id: String = _find_planet_node(start_node)
	if planet_node_id.is_empty():
		print("PS1|WARN no planet found near start, checking start node")
		# Try start node itself
		var pi: Dictionary = _bridge.call("GetPlanetInfoV0", start_node)
		if pi.size() > 0:
			planet_node_id = start_node
		else:
			print("PS1|WARN no planet accessible, skipping scan actions")
			_warn("NO_PLANET_ACCESSIBLE")
			_summary()
			_bridge.call("StopSimV0")
			await create_timer(0.5).timeout
			quit(0 if _fail_count == 0 else 1)
			return

	print("PS1|PLANET_FOUND %s" % planet_node_id)

	# Travel to planet node if not already there.
	if start_node != planet_node_id:
		print("PS1|TRAVELING to %s" % planet_node_id)
		_gm.call("on_lane_gate_proximity_entered_v0", planet_node_id)
		_gm.call("on_lane_arrival_v0", planet_node_id)
		await create_timer(1.0).timeout

	# Verify planet info.
	var planet_info: Dictionary = _bridge.call("GetPlanetInfoV0", planet_node_id)
	_hard("PLANET_INFO_EXISTS", planet_info.size() > 0)
	var planet_type: String = str(planet_info.get("planet_type", ""))
	print("PS1|PLANET_TYPE %s" % planet_type)
	_hard("PLANET_TYPE_VALID", not planet_type.is_empty())

	# Check scan charges.
	var charges: Dictionary = _bridge.call("GetScanChargesV0")
	var remaining: int = int(charges.get("remaining", 0))
	var max_charges: int = int(charges.get("max", 0))
	print("PS1|CHARGES %d/%d" % [remaining, max_charges])
	_hard("HAS_CHARGES", remaining > 0)
	_hard("MAX_CHARGES_VALID", max_charges >= 2)

	# Check mode availability.
	var mineral_ok: bool = charges.get("mineral_available", false)
	_hard("MINERAL_MODE_AVAILABLE", mineral_ok)

	# Check affinity.
	var affinity_bps: int = int(_bridge.call("GetScanAffinityV0", planet_node_id, "MineralSurvey"))
	print("PS1|AFFINITY_MINERAL %d bps" % affinity_bps)
	_hard("AFFINITY_POSITIVE", affinity_bps > 0)

	# Orbital scan.
	print("PS1|ORBITAL_SCAN mode=MineralSurvey")
	var result: Dictionary = _bridge.call("OrbitalScanV0", planet_node_id, "MineralSurvey")
	_hard("ORBITAL_SCAN_SUCCESS", not result.has("error"))
	if result.has("error"):
		print("PS1|ERROR %s" % str(result["error"]))
	else:
		var scan_id: String = str(result.get("scan_id", ""))
		var category: String = str(result.get("category", ""))
		var flavor: String = str(result.get("flavor_text", ""))
		var hint: String = str(result.get("hint_text", ""))
		print("PS1|SCAN_RESULT scan_id=%s category=%s" % [scan_id, category])
		_hard("SCAN_ID_VALID", not scan_id.is_empty())
		_hard("CATEGORY_VALID", not category.is_empty())
		_hard("FLAVOR_TEXT_EXISTS", not flavor.is_empty())
		# Hint text is only for orbital scans — should exist.
		_hard("HINT_TEXT_EXISTS", not hint.is_empty())
		print("PS1|FLAVOR \"%s\"" % flavor.substr(0, 60))
		print("PS1|HINT \"%s\"" % hint.substr(0, 60))

	# Verify charges decreased.
	var charges2: Dictionary = _bridge.call("GetScanChargesV0")
	var remaining2: int = int(charges2.get("remaining", 0))
	_hard("CHARGE_CONSUMED", remaining2 == remaining - 1)
	print("PS1|CHARGES_AFTER %d/%d" % [remaining2, max_charges])

	# Verify scan results for this planet.
	var scan_results: Array = _bridge.call("GetPlanetScanResultsV0", planet_node_id)
	_hard("SCAN_RESULTS_COUNT", scan_results.size() >= 1)
	print("PS1|TOTAL_RESULTS %d" % scan_results.size())

	# Dock at planet for landing scan.
	print("PS1|DOCKING at %s" % planet_node_id)
	_gm.call("dock_at_station_v0", planet_node_id)
	await create_timer(0.5).timeout

	# Landing scan (if landable and have charges).
	var is_landable: bool = planet_info.get("effective_landable", false)
	if is_landable and remaining2 > 0:
		print("PS1|LANDING_SCAN mode=MineralSurvey")
		var land_result: Dictionary = _bridge.call("LandingScanV0", planet_node_id, "MineralSurvey")
		_hard("LANDING_SCAN_SUCCESS", not land_result.has("error"))
		if not land_result.has("error"):
			var land_cat: String = str(land_result.get("category", ""))
			var land_flavor: String = str(land_result.get("flavor_text", ""))
			print("PS1|LANDING_RESULT category=%s" % land_cat)
			_hard("LANDING_CATEGORY_VALID", not land_cat.is_empty())
			_hard("LANDING_FLAVOR_EXISTS", not land_flavor.is_empty())

			# Check for investigation opportunity.
			var inv_available: bool = land_result.get("investigation_available", false)
			var land_scan_id: String = str(land_result.get("scan_id", ""))
			if inv_available:
				print("PS1|INVESTIGATING scan_id=%s" % land_scan_id)
				var inv_result: Dictionary = _bridge.call("InvestigateFindingV0", land_scan_id)
				var inv_success: bool = inv_result.get("success", false)
				_hard("INVESTIGATION_SUCCESS", inv_success)
				print("PS1|INVESTIGATION %s" % ("PASS" if inv_success else "FAIL"))
			else:
				print("PS1|NO_INVESTIGATION_AVAILABLE (normal)")
		else:
			print("PS1|LANDING_ERROR %s" % str(land_result["error"]))
	else:
		if not is_landable:
			print("PS1|SKIP landing scan (not landable)")
		else:
			print("PS1|SKIP landing scan (no charges)")

	# Verify total scan results increased.
	var final_results: Array = _bridge.call("GetPlanetScanResultsV0", planet_node_id)
	_hard("FINAL_RESULTS_VALID", final_results.size() >= 1)
	print("PS1|FINAL_RESULTS %d" % final_results.size())

	# Verify scan result structure.
	if final_results.size() > 0:
		var first: Dictionary = final_results[0]
		_hard("RESULT_HAS_SCAN_ID", first.has("scan_id"))
		_hard("RESULT_HAS_CATEGORY", first.has("category"))
		_hard("RESULT_HAS_FLAVOR", first.has("flavor_text"))
		_hard("RESULT_HAS_MODE", first.has("mode"))
		_hard("RESULT_HAS_PHASE", first.has("phase"))
		_hard("RESULT_HAS_AFFINITY", first.has("affinity_bps"))

	_summary()

	# Clean shutdown.
	_bridge.call("StopSimV0")
	await create_timer(0.5).timeout
	quit(0 if _fail_count == 0 else 1)


func _find_planet_node(start_node: String) -> String:
	# Check adjacent nodes for planets.
	if not _bridge.has_method("GetLaneDestinationsV0"):
		return ""
	var dests: Array = _bridge.call("GetLaneDestinationsV0", start_node)
	for dest in dests:
		var node_id: String = str(dest)
		if node_id.is_empty():
			continue
		var pi: Dictionary = _bridge.call("GetPlanetInfoV0", node_id)
		if pi.size() > 0:
			return node_id
	# Also check start node itself.
	var pi_start: Dictionary = _bridge.call("GetPlanetInfoV0", start_node)
	if pi_start.size() > 0:
		return start_node
	return ""


func _hard(name: String, condition: bool) -> void:
	if condition:
		_pass_count += 1
		print("PS1|HARD_PASS %s" % name)
	else:
		_fail_count += 1
		print("PS1|HARD_FAIL %s" % name)


func _warn(name: String) -> void:
	_warn_count += 1
	print("PS1|WARN %s" % name)


func _summary() -> void:
	print("PS1|SUMMARY pass=%d fail=%d warn=%d" % [_pass_count, _fail_count, _warn_count])
	if _fail_count == 0:
		print("PS1|RESULT PASS")
	else:
		print("PS1|RESULT FAIL")
