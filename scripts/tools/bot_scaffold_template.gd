# scripts/tools/bot_scaffold_template.gd
# ─── Standardized Bot Scaffold ───
# Copy this file to scripts/tests/test_<name>_v0.gd and customize.
#
# Features:
#   - Correct bridge discovery (root.get_node_or_null("SimBridge"))
#   - Phase machine with stall watchdog
#   - Read-lock retry via bot_assert.bridge_read()
#   - Hard safety timeout (MAX_TOTAL_FRAMES)
#   - FPS profiling per phase
#   - Decision event stream for emotional arc analysis
#   - StopSimV0 before quit (prevents C# thread hang)
#
# Run:
#   godot --headless --path . -s res://scripts/tests/test_<name>_v0.gd -- --seed=42
#
extends SceneTree

# ── Configuration ──
const BOT_PREFIX := "SCAFFOLD"
const MAX_TOTAL_FRAMES := 12000  # ~200s safety timeout
const STALL_FRAME_LIMIT := 600  # ~10s per-phase stall detection

# ── Phase enum — customize for your bot ──
enum Phase {
	LOAD_SCENE,
	WAIT_SCENE,
	WAIT_BRIDGE,
	INIT,
	# --- Add your phases here ---
	PLAY_LOOP,
	REPORT,
	DONE
}

# ── State ──
var _phase := Phase.LOAD_SCENE
var _bridge: Object = null
var _gm: Object = null  # GameManager (if needed for UI interaction)
var _a := preload("res://scripts/tools/bot_assert.gd").new(BOT_PREFIX)
var _total_frames: int = 0
var _busy: bool = false
var _stall_phase: int = -1
var _stall_frame_start: int = 0
var _seed: int = 42

# ── Lifecycle ──
func _init() -> void:
	# Parse --seed=N from command line
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			_seed = int(arg.split("=")[1])


func _process(delta: float) -> bool:
	_total_frames += 1

	# FPS sampling (every frame)
	_a.fps_sample(delta)

	# Hard safety timeout
	if _total_frames > MAX_TOTAL_FRAMES and _phase != Phase.DONE:
		_a.flag("TIMEOUT|frame=%d|phase=%d" % [_total_frames, _phase])
		if _busy:
			_a.log("TIMEOUT_FORCE_QUIT|frame=%d|phase=%d|busy=true" % [_total_frames, _phase])
			_busy = false
		_phase = Phase.REPORT

	# Per-phase stall watchdog
	if _phase != _stall_phase:
		_stall_phase = _phase
		_stall_frame_start = _total_frames
	elif _total_frames - _stall_frame_start > STALL_FRAME_LIMIT and _phase != Phase.DONE:
		_a.flag("STALL|phase=%d|frames=%d" % [_phase, _total_frames - _stall_frame_start])
		if _busy:
			_busy = false
		_phase = Phase.REPORT

	if _busy:
		return false

	match _phase:
		Phase.LOAD_SCENE:
			_do_load_scene()
		Phase.WAIT_SCENE:
			_do_wait_scene()
		Phase.WAIT_BRIDGE:
			_do_wait_bridge()
		Phase.INIT:
			_do_init()
		Phase.PLAY_LOOP:
			_do_play_loop()
		Phase.REPORT:
			_do_report()
		Phase.DONE:
			pass  # Waiting for quit

	return false


# ── Phase implementations ──

func _do_load_scene() -> void:
	_a.fps_set_phase("load")
	var err := change_scene_to_file("res://scenes/main.tscn")
	if err != OK:
		_a.hard(false, "scene_load", "error=%d" % err)
		_force_quit(1)
		return
	_phase = Phase.WAIT_SCENE


func _do_wait_scene() -> void:
	# Wait for scene tree to be ready
	var root_node := root.get_child(root.get_child_count() - 1) if root.get_child_count() > 0 else null
	if root_node == null or root_node.name == "root":
		return  # Scene not loaded yet
	_gm = root.get_node_or_null("GameManager")
	if _gm == null:
		return  # GameManager autoload not ready yet
	_phase = Phase.WAIT_BRIDGE


func _do_wait_bridge() -> void:
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge == null:
		return  # SimBridge not ready yet
	# Verify bridge is responsive with read-lock retry
	_busy = true
	_try_bridge_init()


func _try_bridge_init() -> void:
	var snapshot = await _a.bridge_read(_bridge, "GetGalaxySnapshotV0", [], self)
	if snapshot == null or (snapshot is Dictionary and snapshot.is_empty()):
		_a.hard(false, "bridge_init", "GetGalaxySnapshotV0 returned empty after retries")
		_phase = Phase.REPORT
		_busy = false
		return
	_a.hard(true, "bridge_init", "nodes=%s" % str(snapshot.size()) if snapshot is Dictionary else "ok")
	_busy = false
	_phase = Phase.INIT


func _do_init() -> void:
	_a.fps_set_phase("init")
	# TODO: Your initialization logic here
	# Example: parse galaxy, set up routes, etc.
	_phase = Phase.PLAY_LOOP


func _do_play_loop() -> void:
	_a.fps_set_phase("play")
	# TODO: Your main loop logic here
	# Example: trade, travel, combat, screenshot capture
	#
	# Use _a.event(decision_num, "EVENT_NAME", "detail") for emotional arc tracking
	# Use _a.hard/warn for assertions
	# Set _phase = Phase.REPORT when done
	_phase = Phase.REPORT


func _do_report() -> void:
	_a.fps_set_phase("report")
	# FPS profiling report
	var fps_data := _a.fps_report()

	# Dead zone analysis
	var dead_zones: Array[Dictionary] = _a.analyze_dead_zones(50)
	for dz in dead_zones:
		_a.log("DEAD_ZONE|start=d%d|end=d%d|gap=%d" % [dz["start"], dz["end"], dz["gap"]])
	_a.warn(dead_zones.is_empty(), "no_dead_zones", "count=%d" % dead_zones.size())

	# Summary
	_a.summary()
	_phase = Phase.DONE
	_force_quit(_a.exit_code())


# ── Utilities ──

func _force_quit(code: int) -> void:
	# Always stop sim before quit to prevent C# thread hang
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	_phase = Phase.DONE
	await create_timer(0.2).timeout
	quit(code)
