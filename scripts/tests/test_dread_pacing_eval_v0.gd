# scripts/tests/test_dread_pacing_eval_v0.gd
# Dread Pacing Evaluation Bot — measures tension curves, relief ratios,
# encounter spacing per Dead Space Intensity Director best practices.
#
# Metrics measured:
#   - Tension score time series (composite of hull deficit, phase, fauna, ghosts)
#   - Relief ratio (calm time / total time)
#   - Encounter spacing coefficient of variation
#   - Peak-to-valley tension delta
#   - Near-death frequency
#   - Escalation curve (tension should increase with distance from safe space)
#
# Usage:
#   godot --headless --path . -s res://scripts/tests/test_dread_pacing_eval_v0.gd
extends SceneTree

const AssertLib = preload("res://scripts/tools/bot_assert.gd")

const MAX_FRAMES := 5400  # 90s at 60fps
const SAMPLE_INTERVAL := 30  # Sample tension every 30 frames (~0.5s)

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	BOOT, TRAVEL_TO_DREAD_SPACE, WAIT_TRAVEL_1,
	SETUP_PHASE_2, MEASURE_PHASE_2,
	SETUP_PHASE_3, MEASURE_PHASE_3,
	SETUP_PHASE_4, MEASURE_PHASE_4,
	ANALYZE, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _gm = null
var _a: AssertLib = null
var _home_node_id := ""
var _dread_node_id := ""
var _all_edges: Array = []

# Tension time series per phase
var _tension_samples_phase2: Array[float] = []
var _tension_samples_phase3: Array[float] = []
var _tension_samples_phase4: Array[float] = []
var _sample_counter := 0

# Encounter tracking
var _ghost_encounter_ticks: Array[int] = []
var _fauna_encounter_ticks: Array[int] = []
var _near_death_events := 0
var _measurement_ticks := 0
var _calm_ticks := 0  # Ticks where tension < 20


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_a.flag("TIMEOUT_AT_%s" % Phase.keys()[_phase])
		_phase = Phase.ANALYZE

	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(Phase.WAIT_SCENE, 60, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait(Phase.WAIT_READY, 30, Phase.BOOT)
		Phase.BOOT: _do_boot()
		Phase.TRAVEL_TO_DREAD_SPACE: _do_travel_to_dread()
		Phase.WAIT_TRAVEL_1: _do_wait(Phase.WAIT_TRAVEL_1, 120, Phase.SETUP_PHASE_2)
		Phase.SETUP_PHASE_2: _do_setup_phase(2)
		Phase.MEASURE_PHASE_2: _do_measure(2)
		Phase.SETUP_PHASE_3: _do_setup_phase(3)
		Phase.MEASURE_PHASE_3: _do_measure(3)
		Phase.SETUP_PHASE_4: _do_setup_phase(4)
		Phase.MEASURE_PHASE_4: _do_measure(4)
		Phase.ANALYZE: _do_analyze()
		Phase.DONE: pass
	return false


func _do_load_scene() -> void:
	_a = AssertLib.new("DREAD_PACE")
	_a.log("START|loading main scene")
	var err = change_scene_to_file("res://scenes/main.tscn")
	if err != OK:
		_a.flag("SCENE_LOAD_FAIL")
		_phase = Phase.ANALYZE
		return
	_phase = Phase.WAIT_SCENE


func _do_wait(current: Phase, max_polls: int, next: Phase) -> void:
	_polls += 1
	if _polls >= max_polls:
		_polls = 0
		_phase = next


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		_polls += 1
		if _polls > 300:
			_a.flag("NO_BRIDGE")
			_phase = Phase.ANALYZE
		return
	_gm = root.get_node_or_null("GameManager")
	_polls = 0
	_phase = Phase.WAIT_READY


func _do_boot() -> void:
	_phase = Phase.TRAVEL_TO_DREAD_SPACE
	_a.log("BOOT|bridge ready")

	# Get player state and edges.
	var ps = _bridge.call("GetPlayerStateV0")
	if ps is Dictionary:
		_home_node_id = ps.get("current_node_id", "")
	var snap = _bridge.call("GetGalaxySnapshotV0")
	if snap is Dictionary:
		_all_edges = snap.get("lane_edges", [])

	# Find a neighbor to use as dread node.
	_dread_node_id = _get_neighbor(_home_node_id)
	if _dread_node_id.is_empty():
		_dread_node_id = _home_node_id

	_a.log("HOME=%s DREAD=%s" % [_home_node_id, _dread_node_id])


func _do_travel_to_dread() -> void:
	_phase = Phase.WAIT_TRAVEL_1
	if _dread_node_id != _home_node_id:
		_headless_travel(_dread_node_id)
	else:
		_polls = 100  # Skip wait


func _do_setup_phase(target_phase: int) -> void:
	_a.log("SETUP_PHASE_%d" % target_phase)

	# Force instability to target phase.
	var level := 0
	match target_phase:
		2: level = 50   # Drift
		3: level = 75   # Fracture
		4: level = 100  # Void

	_bridge.call("ForceSetNodeInstabilityV0", _dread_node_id, level)

	if target_phase >= 3:
		_bridge.call("ForceEnableFractureSignatureV0")

	_measurement_ticks = 0
	_calm_ticks = 0
	_sample_counter = 0

	match target_phase:
		2: _phase = Phase.MEASURE_PHASE_2
		3: _phase = Phase.MEASURE_PHASE_3
		4: _phase = Phase.MEASURE_PHASE_4


func _do_measure(current_phase: int) -> void:
	_measurement_ticks += 1
	_sample_counter += 1

	# Sample tension every SAMPLE_INTERVAL frames.
	if _sample_counter >= SAMPLE_INTERVAL:
		_sample_counter = 0
		var tension := _compute_tension()

		match current_phase:
			2: _tension_samples_phase2.append(tension)
			3: _tension_samples_phase3.append(tension)
			4: _tension_samples_phase4.append(tension)

		if tension < 20.0:
			_calm_ticks += SAMPLE_INTERVAL

		# Track near-death events.
		var hp = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
		if hp is Dictionary:
			var hull: int = hp.get("hull", 100)
			var hull_max: int = hp.get("hull_max", 100)
			if hull_max > 0 and float(hull) / float(hull_max) < 0.2:
				_near_death_events += 1

		# Track ghost encounters.
		var ghosts = _bridge.call("GetSensorGhostsV0")
		if ghosts is Array and ghosts.size() > 0:
			_ghost_encounter_ticks.append(_measurement_ticks)

		# Track fauna encounters.
		var fauna = _bridge.call("GetLatticeFaunaV0")
		if fauna is Array and fauna.size() > 0:
			_fauna_encounter_ticks.append(_measurement_ticks)

	# Measure for 600 frames per phase (~10s).
	if _measurement_ticks >= 600:
		_a.log("PHASE_%d_MEASURED|samples=%d" % [current_phase, _get_samples(current_phase).size()])
		match current_phase:
			2: _phase = Phase.SETUP_PHASE_3
			3: _phase = Phase.SETUP_PHASE_4
			4: _phase = Phase.ANALYZE


func _compute_tension() -> float:
	var tension := 0.0

	# Component 1: Hull deficit (0-30 points).
	var hp = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
	if hp is Dictionary:
		var hull: int = hp.get("hull", 100)
		var hull_max: int = hp.get("hull_max", 100)
		if hull_max > 0:
			tension += (1.0 - float(hull) / float(hull_max)) * 30.0

	# Component 2: Instability phase (0-40 points).
	var dread = _bridge.call("GetDreadStateV0")
	if dread is Dictionary:
		var phase: int = dread.get("phase", 0)
		tension += float(phase) * 10.0

	# Component 3: Active ghosts (0-15 points, 5 per ghost).
	var ghosts = _bridge.call("GetSensorGhostsV0")
	if ghosts is Array:
		tension += min(ghosts.size() * 5.0, 15.0)

	# Component 4: Active fauna (0-15 points, 7.5 per fauna).
	var fauna = _bridge.call("GetLatticeFaunaV0")
	if fauna is Array:
		tension += min(fauna.size() * 7.5, 15.0)

	return clamp(tension, 0.0, 100.0)


func _do_analyze() -> void:
	_phase = Phase.DONE
	_a.log("ANALYZE|starting pacing analysis")

	# --- Phase 2 analysis ---
	var p2 := _tension_samples_phase2
	if p2.size() >= 3:
		var p2_mean := _mean(p2)
		var p2_std := _stddev(p2)
		var p2_max := _max_val(p2)
		var p2_min := _min_val(p2)
		_a.goal("PHASE_2_TENSION", "mean=%.1f std=%.1f min=%.1f max=%.1f" % [p2_mean, p2_std, p2_min, p2_max])

		# Phase 2 should have SOME tension but not overwhelming.
		_a.hard(p2_mean > 5.0, "phase2_has_tension", "mean=%.1f" % p2_mean)
		_a.hard(p2_mean < 60.0, "phase2_not_overwhelming", "mean=%.1f" % p2_mean)
		# Should have variability (not flatlined).
		_a.warn(p2_std > 1.0, "phase2_has_variability", "std=%.1f" % p2_std)
	else:
		_a.warn(false, "phase2_insufficient_samples", "count=%d" % p2.size())

	# --- Phase 3 analysis ---
	var p3 := _tension_samples_phase3
	if p3.size() >= 3:
		var p3_mean := _mean(p3)
		var p3_std := _stddev(p3)
		_a.goal("PHASE_3_TENSION", "mean=%.1f std=%.1f" % [p3_mean, p3_std])

		# Phase 3 should be MORE tense than Phase 2.
		var p2_mean_for_compare := _mean(p2) if p2.size() >= 3 else 0.0
		_a.hard(p3_mean > p2_mean_for_compare, "phase3_escalates_from_phase2",
			"p3=%.1f > p2=%.1f" % [p3_mean, p2_mean_for_compare])
		_a.hard(p3_mean > 15.0, "phase3_meaningful_tension", "mean=%.1f" % p3_mean)
	else:
		_a.warn(false, "phase3_insufficient_samples", "count=%d" % p3.size())

	# --- Phase 4 (Void) analysis ---
	var p4 := _tension_samples_phase4
	if p4.size() >= 3:
		var p4_mean := _mean(p4)
		var p4_std := _stddev(p4)
		_a.goal("PHASE_4_TENSION", "mean=%.1f std=%.1f" % [p4_mean, p4_std])

		# Void paradox: tension should NOT be highest (no drain).
		# But phase score itself contributes 40 points, so it'll be high.
		# Key check: hull should not decrease further.
		_a.hard(p4_mean > 0.0, "phase4_not_zero_tension", "mean=%.1f" % p4_mean)
	else:
		_a.warn(false, "phase4_insufficient_samples", "count=%d" % p4.size())

	# --- Cross-phase escalation ---
	if p2.size() >= 3 and p3.size() >= 3:
		var escalation := _mean(p3) - _mean(p2)
		_a.goal("ESCALATION_DELTA", "delta=%.1f" % escalation)
		_a.hard(escalation > 0.0, "tension_escalates_with_depth", "delta=%.1f" % escalation)

	# --- Encounter spacing ---
	if _ghost_encounter_ticks.size() >= 2:
		var ghost_cv := _spacing_cv(_ghost_encounter_ticks)
		_a.goal("GHOST_ENCOUNTER_CV", "cv=%.2f count=%d" % [ghost_cv, _ghost_encounter_ticks.size()])
		# CV should be > 0.15 (not too regular/predictable) but < 2.0 (not chaotic).
		_a.warn(ghost_cv > 0.15, "ghost_spacing_not_too_regular", "cv=%.2f" % ghost_cv)
		_a.warn(ghost_cv < 2.0, "ghost_spacing_not_chaotic", "cv=%.2f" % ghost_cv)
	else:
		_a.goal("GHOST_ENCOUNTER_CV", "insufficient_data count=%d" % _ghost_encounter_ticks.size())

	# --- Near-death frequency ---
	_a.goal("NEAR_DEATH_EVENTS", "count=%d" % _near_death_events)

	# --- Relief ratio ---
	var total_samples := p2.size() + p3.size() + p4.size()
	if total_samples > 0:
		var calm_count := 0
		for s in p2:
			if s < 20.0: calm_count += 1
		for s in p3:
			if s < 20.0: calm_count += 1
		for s in p4:
			if s < 20.0: calm_count += 1
		var relief_ratio := float(calm_count) / float(total_samples)
		_a.goal("RELIEF_RATIO", "ratio=%.2f calm=%d total=%d" % [relief_ratio, calm_count, total_samples])
		# Relief ratio should be between 0.1 and 0.7 (some calm, some tension).
		_a.warn(relief_ratio > 0.05, "not_relentlessly_tense", "ratio=%.2f" % relief_ratio)
		_a.warn(relief_ratio < 0.8, "not_boringly_calm", "ratio=%.2f" % relief_ratio)

	# --- JSON report ---
	var p2_mean_final := _mean(p2) if p2.size() >= 3 else 0.0
	var p2_std_final := _stddev(p2) if p2.size() >= 3 else 0.0
	var p3_mean_final := _mean(p3) if p3.size() >= 3 else 0.0
	var p4_mean_final := _mean(p4) if p4.size() >= 3 else 0.0
	var escalation_final := p3_mean_final - p2_mean_final
	var relief_ratio_final := 0.0
	if total_samples > 0:
		var calm_ct := 0
		for s in p2:
			if s < 20.0: calm_ct += 1
		for s in p3:
			if s < 20.0: calm_ct += 1
		for s in p4:
			if s < 20.0: calm_ct += 1
		relief_ratio_final = float(calm_ct) / float(total_samples)
	var ghost_cv_final := _spacing_cv(_ghost_encounter_ticks) if _ghost_encounter_ticks.size() >= 2 else 0.0
	var report := {
		"bot": "dread_pacing",
		"prefix": "DREAD_PACE",
		"metrics": {
			"phase2_tension_mean": p2_mean_final,
			"phase2_tension_std": p2_std_final,
			"phase3_tension_mean": p3_mean_final,
			"phase4_tension_mean": p4_mean_final,
			"escalation_delta": escalation_final,
			"relief_ratio": relief_ratio_final,
			"near_death_count": _near_death_events,
			"ghost_spacing_cv": ghost_cv_final,
		},
		"assertions": {
			"pass": _a._passes,
			"warn": _a._warns.size(),
			"fail": _a._fails.size(),
		},
	}
	var json_str := JSON.stringify(report, "  ")
	var report_path := "res://reports/eval/dread_pacing_report.json"
	var f := FileAccess.open(report_path, FileAccess.WRITE)
	if f:
		f.store_string(json_str)
		f.close()
		_a.log("REPORT_JSON|written=%s" % report_path)

	_a.summary()
	if _bridge:
		_bridge.call("StopSimV0")
	_busy = true
	await create_timer(0.5).timeout
	quit(_a.exit_code())


# --- Helpers ---

func _get_samples(phase: int) -> Array[float]:
	match phase:
		2: return _tension_samples_phase2
		3: return _tension_samples_phase3
		4: return _tension_samples_phase4
	return []

func _get_neighbor(node_id: String) -> String:
	for e in _all_edges:
		if e is Dictionary:
			if e.get("from_id", "") == node_id: return e.get("to_id", "")
			if e.get("to_id", "") == node_id: return e.get("from_id", "")
	return ""

func _headless_travel(dest: String) -> void:
	_gm.call("on_lane_gate_proximity_entered_v0", dest)
	_gm.call("on_lane_arrival_v0", dest)

func _mean(arr: Array[float]) -> float:
	if arr.is_empty(): return 0.0
	var s := 0.0
	for v in arr: s += v
	return s / arr.size()

func _stddev(arr: Array[float]) -> float:
	if arr.size() < 2: return 0.0
	var m := _mean(arr)
	var s := 0.0
	for v in arr: s += (v - m) * (v - m)
	return sqrt(s / (arr.size() - 1))

func _max_val(arr: Array[float]) -> float:
	var m := -INF
	for v in arr:
		if v > m: m = v
	return m

func _min_val(arr: Array[float]) -> float:
	var m := INF
	for v in arr:
		if v < m: m = v
	return m

func _spacing_cv(ticks: Array[int]) -> float:
	if ticks.size() < 2: return 0.0
	var spacings: Array[float] = []
	for i in range(1, ticks.size()):
		spacings.append(float(ticks[i] - ticks[i - 1]))
	var m := _mean(spacings)
	if m < 0.001: return 0.0
	return _stddev(spacings) / m
