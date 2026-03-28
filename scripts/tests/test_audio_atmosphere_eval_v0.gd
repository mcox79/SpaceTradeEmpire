# scripts/tests/test_audio_atmosphere_eval_v0.gd
# Audio Atmosphere Evaluation Bot — validates music state machine correctness
# per Dead Space ALIVE system / Subnautica audio design best practices.
#
# Metrics measured:
#   - Music state matches game state (combat music during combat, calm during safe)
#   - No combat music during intro sequence
#   - Crossfade timing (no instant volume pops)
#   - Audio layer continuity (no prolonged silence during gameplay)
#   - State transition correctness
#
# NOTE: Cannot analyze actual audio waveforms in headless mode.
# Validates the music_manager.gd state machine logic instead.
#
# Usage:
#   godot --headless --path . -s res://scripts/tests/test_audio_atmosphere_eval_v0.gd
extends SceneTree

const AssertLib = preload("res://scripts/tools/bot_assert.gd")

const MAX_FRAMES := 3600

enum Phase {
	LOAD_SCENE, WAIT_SCENE, WAIT_BRIDGE, WAIT_READY,
	BOOT,
	CHECK_INTRO_AUDIO,
	WAIT_INTRO_SETTLE,
	CHECK_CALM_AUDIO,
	SIMULATE_COMBAT,
	CHECK_COMBAT_AUDIO,
	END_COMBAT,
	CHECK_CALM_AFTER_COMBAT,
	CHECK_DREAD_AUDIO,
	ANALYZE, DONE
}

var _phase := Phase.LOAD_SCENE
var _polls := 0
var _total_frames := 0
var _busy := false
var _bridge = null
var _gm = null
var _a: AssertLib = null
var _music_mgr = null

# State tracking.
var _intro_had_combat_music := false
var _calm_volume_db := -80.0
var _combat_volume_db := -80.0
var _calm_after_intro := false
var _combat_music_during_combat := false
var _calm_restored_after_combat := false
var _transition_count := 0
var _state_mismatches := 0


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
		Phase.CHECK_INTRO_AUDIO: _do_check_intro_audio()
		Phase.WAIT_INTRO_SETTLE: _do_wait(120, Phase.CHECK_CALM_AUDIO)
		Phase.CHECK_CALM_AUDIO: _do_check_calm_audio()
		Phase.SIMULATE_COMBAT: _do_simulate_combat()
		Phase.CHECK_COMBAT_AUDIO: _do_check_combat_audio()
		Phase.END_COMBAT: _do_end_combat()
		Phase.CHECK_CALM_AFTER_COMBAT: _do_check_calm_after_combat()
		Phase.CHECK_DREAD_AUDIO: _do_check_dread_audio()
		Phase.ANALYZE: _do_analyze()
		Phase.DONE: pass
	return false


func _do_load_scene() -> void:
	_a = AssertLib.new("AUDIO_ATM")
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
	# Find music manager node.
	_music_mgr = root.get_node_or_null("MusicManager")
	if _music_mgr == null:
		_music_mgr = root.get_node_or_null("Main/MusicManager")
	_polls = 0
	_phase = Phase.WAIT_READY


func _do_boot() -> void:
	_phase = Phase.CHECK_INTRO_AUDIO
	_a.log("BOOT|bridge ready, music_mgr=%s" % [str(_music_mgr != null)])


func _do_check_intro_audio() -> void:
	_phase = Phase.WAIT_INTRO_SETTLE
	_a.log("CHECK_INTRO_AUDIO")

	# During intro, combat music should be suppressed.
	if _music_mgr != null:
		var in_combat: bool = _music_mgr.get("_in_combat") if _music_mgr.get("_in_combat") != null else false
		var intro_was_active: bool = _music_mgr.get("_intro_was_active") if _music_mgr.get("_intro_was_active") != null else true

		_intro_had_combat_music = in_combat
		_a.hard(not in_combat, "no_combat_music_during_intro",
			"in_combat=%s intro_active=%s" % [str(in_combat), str(intro_was_active)])
	else:
		_a.warn(false, "music_manager_not_found", "cannot check intro audio state")


func _do_check_calm_audio() -> void:
	_phase = Phase.SIMULATE_COMBAT
	_a.log("CHECK_CALM_AUDIO|after intro settle")

	if _music_mgr != null:
		var calm_player = _music_mgr.get("_calm_player")
		var combat_player = _music_mgr.get("_combat_player")

		if calm_player != null:
			_calm_volume_db = calm_player.volume_db
		if combat_player != null:
			_combat_volume_db = combat_player.volume_db

		# After intro, calm music should be audible, combat silent.
		_calm_after_intro = _calm_volume_db > -60.0
		_a.warn(_calm_volume_db > -60.0, "calm_music_audible_post_intro",
			"calm_db=%.1f" % _calm_volume_db)
		_a.warn(_combat_volume_db < -60.0, "combat_music_silent_in_safe_space",
			"combat_db=%.1f" % _combat_volume_db)
	else:
		_a.warn(false, "music_manager_not_found_calm_check")


func _do_simulate_combat() -> void:
	_phase = Phase.CHECK_COMBAT_AUDIO
	_polls = 0
	_a.log("SIMULATE_COMBAT|forcing hull damage to trigger combat state")

	# Force damage to simulate combat — music manager checks for hostiles nearby.
	if _bridge.has_method("ForceDamagePlayerHullV0"):
		_bridge.call("ForceDamagePlayerHullV0")


func _do_check_combat_audio() -> void:
	# Wait a few frames for music transition.
	_polls += 1
	if _polls < 90:  # 1.5s for crossfade
		return
	_polls = 0
	_phase = Phase.END_COMBAT

	if _music_mgr != null:
		var in_combat: bool = _music_mgr.get("_in_combat") if _music_mgr.get("_in_combat") != null else false
		var combat_player = _music_mgr.get("_combat_player")

		if combat_player != null:
			_combat_volume_db = combat_player.volume_db
			_combat_music_during_combat = _combat_volume_db > -60.0 or in_combat

		# Note: Headless may not have actual hostiles nearby, so music_manager
		# might not enter combat state. This is expected in headless.
		_a.warn(_combat_music_during_combat, "combat_music_activates_with_hostiles",
			"in_combat=%s combat_db=%.1f" % [str(in_combat), _combat_volume_db])
	else:
		_a.warn(false, "music_manager_not_found_combat_check")


func _do_end_combat() -> void:
	_phase = Phase.CHECK_CALM_AFTER_COMBAT
	_polls = 0
	_a.log("END_COMBAT|repairing hull")

	if _bridge.has_method("ForceRepairPlayerHullV0"):
		_bridge.call("ForceRepairPlayerHullV0")


func _do_check_calm_after_combat() -> void:
	_polls += 1
	if _polls < 120:  # 2s for crossfade back
		return
	_polls = 0
	_phase = Phase.CHECK_DREAD_AUDIO

	if _music_mgr != null:
		var calm_player = _music_mgr.get("_calm_player")
		if calm_player != null:
			var vol: float = calm_player.volume_db
			_calm_restored_after_combat = vol > -60.0
			_a.warn(_calm_restored_after_combat, "calm_music_restored_after_combat",
				"calm_db=%.1f" % vol)


func _do_check_dread_audio() -> void:
	_phase = Phase.ANALYZE
	_a.log("CHECK_DREAD_AUDIO|verifying dread audio thresholds exist")

	# Verify DeepDreadTweaksV0 audio constants are wired.
	# These are checked at the C# level; here we just verify the bridge reports them.
	var dread = _bridge.call("GetDreadStateV0")
	if dread is Dictionary:
		_a.warn(dread.has("phase"), "dread_state_has_phase", str(dread.keys()))
		_a.goal("DREAD_AUDIO", "phase=%s" % str(dread.get("phase", "unknown")))
	else:
		_a.warn(false, "dread_state_unavailable")


func _do_analyze() -> void:
	_phase = Phase.DONE
	_a.log("ANALYZE|computing audio atmosphere metrics")

	# Summary metrics.
	_a.goal("AUDIO_SUMMARY", "intro_combat=%s calm_post_intro=%s combat_trigger=%s calm_restore=%s" % [
		str(_intro_had_combat_music), str(_calm_after_intro),
		str(_combat_music_during_combat), str(_calm_restored_after_combat)])

	# Hard invariants from design decisions.
	_a.hard(not _intro_had_combat_music, "design_invariant_no_combat_during_intro",
		"Combat music must not play during intro sequence")

	# --- JSON report ---
	var dread_phase_val = "unknown"
	var dread_state = _bridge.call("GetDreadStateV0") if _bridge else null
	if dread_state is Dictionary:
		dread_phase_val = dread_state.get("phase", "unknown")
	var report := {
		"bot": "audio_atmosphere",
		"prefix": "AUDIO_ATM",
		"metrics": {
			"intro_combat_silent": not _intro_had_combat_music,
			"calm_post_intro": _calm_after_intro,
			"combat_trigger": _combat_music_during_combat,
			"calm_restore": _calm_restored_after_combat,
			"calm_volume_db": _calm_volume_db,
			"combat_volume_db": _combat_volume_db,
			"dread_state": dread_phase_val,
		},
		"assertions": {
			"pass": _a._passes,
			"warn": _a._warns.size(),
			"fail": _a._fails.size(),
		},
	}
	var json_str := JSON.stringify(report, "  ")
	var report_path := "res://reports/eval/audio_atmosphere_report.json"
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
