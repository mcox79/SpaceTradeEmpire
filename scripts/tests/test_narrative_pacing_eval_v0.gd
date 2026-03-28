# scripts/tests/test_narrative_pacing_eval_v0.gd
# Narrative Pacing Evaluation Bot — measures FO dialogue delivery metrics
# per Hades priority queue / Firewatch conversation density best practices.
#
# Metrics measured:
#   - Dialogue density (events per time window)
#   - Silence ratio (time with no active dialogue)
#   - Speaker variety index (unique speakers / available speakers)
#   - Content burndown (unique lines seen / total available)
#   - FO phase advancement rate
#   - Reactive hail timing (lag between trigger event and FO response)
#
# Usage:
#   godot --headless --path . -s res://scripts/tests/test_narrative_pacing_eval_v0.gd
extends SceneTree

const AssertLib = preload("res://scripts/tools/bot_assert.gd")

const MAX_FRAMES := 5400

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	BOOT, START_TUTORIAL,
	OBSERVE_EARLY_GAME, FORCE_ADVANCE_PHASES,
	OBSERVE_MIDGAME,
	ANALYZE, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _gm = null
var _a: AssertLib = null

# Dialogue tracking.
var _dialogue_events: Array[Dictionary] = []
var _dialogue_ticks: Array[int] = []
var _unique_dialogue_ids: Dictionary = {}
var _speakers_seen: Dictionary = {}
var _total_dialogue_time := 0  # Frames with active dialogue
var _total_observation_time := 0  # Total frames observed
var _phase_history: Array[String] = []
var _fo_type := ""
var _last_dialogue_text := ""
var _current_tick := 0


func _process(_delta: float) -> bool:
	if _busy:
		return false
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.ANALYZE

	match _phase:
		Phase.LOAD_SCENE: _do_load_scene()
		Phase.WAIT_SCENE: _do_wait(60, Phase.WAIT_BRIDGE)
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_READY: _do_wait(30, Phase.BOOT)
		Phase.BOOT: _do_boot()
		Phase.START_TUTORIAL: _do_start_tutorial()
		Phase.OBSERVE_EARLY_GAME: _do_observe_early()
		Phase.FORCE_ADVANCE_PHASES: _do_force_advance()
		Phase.OBSERVE_MIDGAME: _do_observe_midgame()
		Phase.ANALYZE: _do_analyze()
		Phase.DONE: pass
	return false


func _do_load_scene() -> void:
	_a = AssertLib.new("NARR_PACE")
	_a.log("START|loading main scene")
	var err = change_scene_to_file("res://scenes/main.tscn")
	if err != OK:
		_a.flag("SCENE_LOAD_FAIL")
		_phase = Phase.ANALYZE
		return
	_phase = Phase.WAIT_SCENE


func _do_wait(max_polls: int, next: Phase) -> void:
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
	_phase = Phase.START_TUTORIAL
	_a.log("BOOT|bridge ready")

	# Delete quicksave to avoid contamination.
	var save_path := "user://quicksave.json"
	if FileAccess.file_exists(save_path):
		DirAccess.remove_absolute(save_path)


func _do_start_tutorial() -> void:
	_phase = Phase.OBSERVE_EARLY_GAME
	_polls = 0

	# Start tutorial via bridge.
	if _bridge.has_method("StartTutorialV0"):
		_bridge.call("StartTutorialV0")
		_a.log("TUTORIAL_STARTED")

	# Get initial tutorial state.
	var tut = _bridge.call("GetTutorialStateV0") if _bridge.has_method("GetTutorialStateV0") else {}
	if tut is Dictionary:
		_fo_type = tut.get("candidate", "unknown")
		_a.log("FO_TYPE=%s" % _fo_type)


func _do_observe_early() -> void:
	_total_observation_time += 1
	_polls += 1

	# Sample dialogue state every frame.
	_sample_dialogue()

	# Record tutorial phase if changed.
	var tut = _bridge.call("GetTutorialStateV0") if _bridge.has_method("GetTutorialStateV0") else {}
	if tut is Dictionary:
		var current_phase: String = str(tut.get("phase_name", ""))
		if current_phase.length() > 0 and (
			_phase_history.is_empty() or _phase_history[-1] != current_phase):
			_phase_history.append(current_phase)

	# Observe for 600 frames (~10s).
	if _polls >= 600:
		_polls = 0
		_phase = Phase.FORCE_ADVANCE_PHASES


func _do_force_advance() -> void:
	_phase = Phase.OBSERVE_MIDGAME
	_polls = 0
	_a.log("FORCE_ADVANCE|advancing tutorial phases")

	# Dismiss any pending dialogue.
	if _bridge.has_method("GetTutorialDialogueV0"):
		for i in range(20):
			var dlg = _bridge.call("GetTutorialDialogueV0")
			if dlg is String and dlg.length() > 0:
				_record_dialogue("tutorial", dlg)
				if _bridge.has_method("DismissTutorialDialogueV0"):
					_bridge.call("DismissTutorialDialogueV0")
			else:
				break

	# Check FO dialogue too.
	if _bridge.has_method("GetFirstOfficerDialogueV0"):
		var fo_dlg = _bridge.call("GetFirstOfficerDialogueV0")
		if fo_dlg is String and fo_dlg.length() > 0:
			_record_dialogue("first_officer", fo_dlg)


func _do_observe_midgame() -> void:
	_total_observation_time += 1
	_polls += 1

	_sample_dialogue()

	# Observe for another 600 frames.
	if _polls >= 600:
		_phase = Phase.ANALYZE


func _sample_dialogue() -> void:
	_current_tick += 1

	# Check for tutorial dialogue.
	if _bridge.has_method("GetTutorialDialogueV0"):
		var dlg = _bridge.call("GetTutorialDialogueV0")
		if dlg is String and dlg.length() > 0:
			if dlg != _last_dialogue_text:
				_record_dialogue("tutorial", dlg)
				_last_dialogue_text = dlg
			_total_dialogue_time += 1
		else:
			_last_dialogue_text = ""

	# Check for FO dialogue.
	if _bridge.has_method("GetFirstOfficerDialogueV0"):
		var fo_dlg = _bridge.call("GetFirstOfficerDialogueV0")
		if fo_dlg is String and fo_dlg.length() > 0:
			_record_dialogue("first_officer", fo_dlg)


func _record_dialogue(speaker: String, text: String) -> void:
	var event := {"speaker": speaker, "text": text, "tick": _current_tick}
	_dialogue_events.append(event)
	_dialogue_ticks.append(_current_tick)
	_speakers_seen[speaker] = _speakers_seen.get(speaker, 0) + 1

	# Track unique dialogue by hash of text.
	var text_hash := text.hash()
	_unique_dialogue_ids[text_hash] = true


func _do_analyze() -> void:
	_phase = Phase.DONE
	_a.log("ANALYZE|computing narrative pacing metrics")

	var total_dialogues := _dialogue_events.size()
	var unique_dialogues := _unique_dialogue_ids.size()
	var unique_speakers := _speakers_seen.size()

	# --- Dialogue density ---
	# Events per 10-minute equivalent (600 frames at 60fps = 10s, scale to 10min).
	var density_per_10min := 0.0
	if _total_observation_time > 0:
		density_per_10min = float(total_dialogues) / float(_total_observation_time) * 36000.0
	_a.goal("DIALOGUE_DENSITY", "events=%d per_10min=%.1f" % [total_dialogues, density_per_10min])

	# Density should be 1-8 per 10 minutes.
	_a.warn(density_per_10min < 15.0, "not_overwhelming_dialogue",
		"density=%.1f per 10min" % density_per_10min)

	# --- Silence ratio ---
	var silence_ratio := 1.0
	if _total_observation_time > 0:
		silence_ratio = 1.0 - float(_total_dialogue_time) / float(_total_observation_time)
	_a.goal("SILENCE_RATIO", "ratio=%.2f dialogue_frames=%d total=%d" % [
		silence_ratio, _total_dialogue_time, _total_observation_time])

	# Silence should be 40-90% during exploration.
	_a.warn(silence_ratio > 0.3, "enough_silence_for_exploration", "ratio=%.2f" % silence_ratio)
	_a.warn(silence_ratio < 0.95, "not_completely_silent", "ratio=%.2f" % silence_ratio)

	# --- Speaker variety ---
	_a.goal("SPEAKER_VARIETY", "unique=%d histogram=%s" % [unique_speakers, str(_speakers_seen)])
	if total_dialogues > 3:
		# No single speaker should dominate > 80%.
		for speaker in _speakers_seen:
			var pct := float(_speakers_seen[speaker]) / float(total_dialogues) * 100.0
			_a.warn(pct < 85.0, "speaker_%s_not_dominating" % speaker,
				"pct=%.0f%%" % pct)

	# --- Content burndown proxy ---
	# How many unique lines vs total delivery.
	if total_dialogues > 0:
		var repeat_ratio := 1.0 - float(unique_dialogues) / float(total_dialogues)
		_a.goal("CONTENT_BURNDOWN", "unique=%d total=%d repeat_ratio=%.2f" % [
			unique_dialogues, total_dialogues, repeat_ratio])
		_a.warn(repeat_ratio < 0.5, "not_too_repetitive", "repeat=%.2f" % repeat_ratio)

	# --- Dialogue spacing ---
	if _dialogue_ticks.size() >= 3:
		var spacings: Array[float] = []
		for i in range(1, _dialogue_ticks.size()):
			spacings.append(float(_dialogue_ticks[i] - _dialogue_ticks[i - 1]))
		var m := _mean(spacings)
		var cv := _stddev(spacings) / m if m > 0.001 else 0.0
		_a.goal("DIALOGUE_SPACING", "mean=%.1f cv=%.2f" % [m, cv])
		_a.warn(cv > 0.1, "dialogue_spacing_not_too_regular", "cv=%.2f" % cv)

	# --- Phase advancement ---
	_a.goal("TUTORIAL_PHASES", "count=%d phases=%s" % [_phase_history.size(), str(_phase_history)])
	_a.hard(_phase_history.size() > 0, "tutorial_phases_advance",
		"at least one phase reached")

	# --- FO type coverage ---
	_a.goal("FO_TYPE", "type=%s" % _fo_type)
	_a.hard(_fo_type.length() > 0, "fo_type_assigned", "type=%s" % _fo_type)

	# --- JSON report ---
	var _dialogue_spacing_cv := 0.0
	if _dialogue_ticks.size() >= 3:
		var _spacings: Array[float] = []
		for i in range(1, _dialogue_ticks.size()):
			_spacings.append(float(_dialogue_ticks[i] - _dialogue_ticks[i - 1]))
		var _sp_m := _mean(_spacings)
		_dialogue_spacing_cv = _stddev(_spacings) / _sp_m if _sp_m > 0.001 else 0.0
	var report := {
		"bot": "narrative_pacing",
		"prefix": "NARR_PACE",
		"metrics": {
			"dialogue_density": density_per_10min,
			"silence_ratio": silence_ratio,
			"speaker_variety": unique_speakers,
			"content_burndown": unique_dialogues,
			"fo_type": _fo_type,
			"phase_history_size": _phase_history.size(),
			"dialogue_spacing_cv": _dialogue_spacing_cv,
		},
		"assertions": {
			"pass": _a._passes,
			"warn": _a._warns.size(),
			"fail": _a._fails.size(),
		},
	}
	var json_str := JSON.stringify(report, "  ")
	var report_path := "res://reports/eval/narrative_pacing_report.json"
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


# --- Math helpers ---

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
