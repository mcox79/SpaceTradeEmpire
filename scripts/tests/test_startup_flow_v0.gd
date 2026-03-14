# scripts/tests/test_startup_flow_v0.gd
# Startup Flow Bot — verifies Main Menu → New Voyage → Galaxy Gen → Welcome Overlay → Cinematic Descent.
#
# Usage (dedicated runner — recommended):
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-StartupBot.ps1 -Mode visual
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-StartupBot.ps1 -Mode headless
#
# Usage (direct):
#   godot --path . -s res://scripts/tests/test_startup_flow_v0.gd
#   godot --headless --path . -s res://scripts/tests/test_startup_flow_v0.gd
extends SceneTree

const PREFIX := "SU1|"
const MAX_POLLS := 300  # ~5s per phase at 60fps
const MAX_FRAMES := 4800  # 80s total (galaxy overlay ~6.5s + descent ~5s + welcome)
const OUTPUT_DIR := "res://reports/startup_flow/"

const ScreenshotScript = preload("res://scripts/tools/screenshot_capture.gd")

var _a = preload("res://scripts/tools/bot_assert.gd").new("SU1")
var _screenshot = null

enum Phase {
	LOAD_MENU,
	WAIT_MENU,
	VERIFY_MENU,
	CLICK_NEW_VOYAGE,
	WAIT_GEN_DONE,
	SCREENSHOT_GEN,
	DISMISS_GEN,
	WAIT_SCENE,
	WAIT_BRIDGE,
	# New flow: galaxy image overlay → welcome overlay → playable
	WAIT_GALAXY_OVERLAY,
	WAIT_WELCOME,
	DISMISS_WELCOME,
	VERIFY_PLAYABLE,
	SUMMARY,
	DONE
}

var _phase := Phase.LOAD_MENU
var _polls := 0
var _total_frames := 0
var _last_phase_change_frame := 0

var _bridge = null
var _game_manager = null
var _menu_node = null
var _new_voyage_btn: Button = null
var _snapshots: Array = []


func _process(_delta: float) -> bool:
	_total_frames += 1
	if _total_frames >= MAX_FRAMES and _phase != Phase.DONE:
		_a.flag("TIMEOUT_AT_%s" % Phase.keys()[_phase])
		_log("TIMEOUT|frame=%d phase=%s" % [_total_frames, Phase.keys()[_phase]])
		_phase = Phase.SUMMARY
	# Stall watchdog — extended to 600 frames (~10s) since descent takes ~5s
	if _total_frames - _last_phase_change_frame > 600 and _phase != Phase.DONE and _phase != Phase.SUMMARY:
		_a.flag("STALL_%s" % Phase.keys()[_phase])
		_log("STALL|phase=%s frames=%d" % [Phase.keys()[_phase], _total_frames - _last_phase_change_frame])
		_last_phase_change_frame = _total_frames
	match _phase:
		Phase.LOAD_MENU: _do_load_menu()
		Phase.WAIT_MENU: _do_wait_menu()
		Phase.VERIFY_MENU: _do_verify_menu()
		Phase.CLICK_NEW_VOYAGE: _do_click_new_voyage()
		Phase.WAIT_GEN_DONE: _do_wait_gen_done()
		Phase.SCREENSHOT_GEN: _do_screenshot_gen()
		Phase.DISMISS_GEN: _do_dismiss_gen()
		Phase.WAIT_SCENE: _do_wait_scene()
		Phase.WAIT_BRIDGE: _do_wait_bridge()
		Phase.WAIT_GALAXY_OVERLAY: _do_wait_galaxy_overlay()
		Phase.WAIT_WELCOME: _do_wait_welcome()
		Phase.DISMISS_WELCOME: _do_dismiss_welcome()
		Phase.VERIFY_PLAYABLE: _do_verify_playable()
		Phase.SUMMARY: _do_summary()
		Phase.DONE: _do_done()
	return false


# ===================== Setup =====================

func _do_load_menu() -> void:
	# Parse --seed=N from CLI args
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			var s := int(arg.trim_prefix("--seed="))
			seed(s)
			_log("SEED|%d" % s)
	_screenshot = ScreenshotScript.new()
	var scene = load("res://scenes/main_menu.tscn").instantiate()
	root.add_child(scene)
	_log("MENU_LOADED")
	_set_phase(Phase.WAIT_MENU)


func _do_wait_menu() -> void:
	# Wait for SimBridge to be ready (autoload)
	var bridge = root.get_node_or_null("SimBridge")
	if bridge != null and bridge.has_method("GetBridgeReadyV0"):
		if bool(bridge.call("GetBridgeReadyV0")):
			_bridge = bridge
			_game_manager = root.get_node_or_null("GameManager")
			# Find the menu node (it's a child of root after instantiation)
			for child in root.get_children():
				if child.has_method("_on_new_voyage") or child.get("_btn_new_voyage") != null:
					_menu_node = child
					break
			if _menu_node == null:
				# Fallback: search by script method
				_menu_node = _find_node_with_property(root, "_gen_complete")
			_set_phase(Phase.VERIFY_MENU)
			return
	_polls += 1
	if _polls >= MAX_POLLS:
		_a.hard(false, "bridge_ready", "SimBridge not ready after %d polls" % MAX_POLLS)
		_set_phase(Phase.SUMMARY)


# ===================== Menu Verification =====================

func _do_verify_menu() -> void:
	# Find the title label and New Voyage button
	var title_found := false
	var btn_found := false
	if _menu_node != null:
		var title_label := _find_label_with_text(_menu_node, "SPACE TRADE EMPIRE")
		title_found = title_label != null
		_new_voyage_btn = _find_button_with_text(_menu_node, "New Voyage")
		btn_found = _new_voyage_btn != null
	_a.hard(title_found, "menu_title_visible", "title=%s" % str(title_found))
	_a.hard(btn_found, "new_voyage_btn_exists", "btn=%s" % str(btn_found))
	_capture("01_main_menu")
	if not btn_found:
		_set_phase(Phase.SUMMARY)
		return
	_set_phase(Phase.CLICK_NEW_VOYAGE)


func _do_click_new_voyage() -> void:
	# Set _is_new_game on GameManager (normally done by main_menu._on_new_voyage)
	if _game_manager:
		_game_manager.set("_is_new_game", true)
	# Simulate the button press
	_new_voyage_btn.emit_signal("pressed")
	_log("NEW_VOYAGE_CLICKED")
	_set_phase(Phase.WAIT_GEN_DONE)


# ===================== Galaxy Gen =====================

func _do_wait_gen_done() -> void:
	if _menu_node != null:
		var gen_complete = _menu_node.get("_gen_complete")
		if gen_complete == true:
			_a.hard(true, "galaxy_gen_completes", "elapsed_polls=%d" % _polls)
			_set_phase(Phase.SCREENSHOT_GEN)
			return
	_polls += 1
	if _polls >= MAX_POLLS:
		_a.hard(false, "galaxy_gen_completes", "timeout")
		_set_phase(Phase.SUMMARY)


func _do_screenshot_gen() -> void:
	_capture("02_galaxy_gen")
	_set_phase(Phase.DISMISS_GEN)


func _do_dismiss_gen() -> void:
	# Inject a key press to dismiss the "Press any key" prompt
	_inject_key()
	_log("GEN_DISMISSED")
	_set_phase(Phase.WAIT_SCENE)


# ===================== Scene Transition =====================

func _do_wait_scene() -> void:
	# After dismissing galaxy gen, main_menu calls _transition_to_game() which
	# changes scene to playable_prototype after 0.6s. Poll for Player or GalaxyView node.
	var player = root.get_node_or_null("Main/Player")
	var gv = root.get_node_or_null("Main/GalaxyView")
	if player != null or gv != null:
		_log("PLAYABLE_SCENE_LOADED")
		_a.hard(true, "scene_transition_ok", "")
		# Clear menu guard on GameManager so _process runs game logic
		_game_manager = root.get_node_or_null("GameManager")
		if _game_manager:
			_game_manager.set("_on_main_menu", false)
		_set_phase(Phase.WAIT_BRIDGE)
		return
	_polls += 1
	if _polls >= MAX_POLLS:
		# Also try finding nodes at root level
		var any_station = get_nodes_in_group("Station")
		if any_station.size() > 0:
			_log("PLAYABLE_SCENE_LOADED|via_station_group")
			_a.hard(true, "scene_transition_ok", "via_group")
			_game_manager = root.get_node_or_null("GameManager")
			if _game_manager:
				_game_manager.set("_on_main_menu", false)
			_set_phase(Phase.WAIT_BRIDGE)
			return
		_a.hard(false, "scene_transition_ok", "timeout")
		_set_phase(Phase.SUMMARY)


func _do_wait_bridge() -> void:
	# Re-acquire bridge reference after scene change
	_bridge = root.get_node_or_null("SimBridge")
	if _bridge != null:
		_set_phase(Phase.WAIT_GALAXY_OVERLAY)
		return
	_polls += 1
	if _polls >= MAX_POLLS:
		_a.hard(false, "bridge_after_transition", "timeout")
		_set_phase(Phase.SUMMARY)


# ===================== Galaxy Intro Overlay =====================

func _do_wait_galaxy_overlay() -> void:
	# Galaxy intro overlay plays for ~9s then removes itself.
	# Wait for it to disappear, capturing screenshots at key moments.
	_game_manager = root.get_node_or_null("GameManager")
	if _game_manager:
		var overlay = _game_manager.get_node_or_null("GalaxyIntroOverlay")
		if overlay != null:
			if _polls == 0:
				# First detection — screenshot the full galaxy
				_capture("03_galaxy_full")
				_a.hard(true, "galaxy_intro_visible", "")
			elif _polls == 180:
				# Mid-zoom (~3s in) — zooming into the arm
				_capture("03_galaxy_zoom")
			_polls += 1
			return  # Still playing
		elif _polls > 0:
			# Overlay was visible and is now gone — it finished
			_log("GALAXY_INTRO_COMPLETE")
			_set_phase(Phase.WAIT_WELCOME)
			return
	_polls += 1
	if _polls >= 900:  # ~15s timeout
		_a.warn(false, "galaxy_intro_visible", "timeout_or_headless")
		_set_phase(Phase.WAIT_WELCOME)


# ===================== Welcome Overlay =====================

func _do_wait_welcome() -> void:
	# Welcome overlay appears after descent completes (~4.5s after scene load)
	_game_manager = root.get_node_or_null("GameManager")
	if _game_manager:
		var overlay = _game_manager.get_node_or_null("WelcomeOverlay")
		if overlay != null:
			_a.hard(true, "welcome_overlay_appears", "")
			_capture("04_welcome_overlay")
			_set_phase(Phase.DISMISS_WELCOME)
			return
	_polls += 1
	if _polls >= MAX_POLLS:
		if DisplayServer.get_name() == "headless":
			_a.warn(true, "welcome_overlay_appears", "headless_may_auto_dismiss")
		else:
			_a.hard(false, "welcome_overlay_appears", "timeout")
		_set_phase(Phase.VERIFY_PLAYABLE)


func _do_dismiss_welcome() -> void:
	# Input.parse_input_event doesn't route through gui_input, so set the flag directly.
	if _game_manager:
		_game_manager.set("_welcome_dismissed", true)
	_log("WELCOME_DISMISSED")
	# Settle: overlay fade (0.4s) + async unlock needs ~1s total
	_set_phase(Phase.VERIFY_PLAYABLE)

const SETTLE_DISMISS := 60  # ~1s at 60fps
var _dismiss_settle := 0


# ===================== Final Verification =====================

func _do_verify_playable() -> void:
	# Wait for overlay fade + unlock to complete
	_dismiss_settle += 1
	if _dismiss_settle < SETTLE_DISMISS:
		return
	# Verify ship is unfrozen and camera is unlocked
	var hero = root.find_child("Player", true, false)
	if hero != null and hero is RigidBody3D:
		var frozen: bool = hero.freeze
		_a.hard(not frozen, "ship_unfrozen", "freeze=%s" % str(frozen))
	else:
		_a.warn(false, "ship_unfrozen", "player_not_found")

	var cam = _find_camera_controller()
	if cam != null:
		var locked = cam.get("input_locked")
		if locked != null:
			_a.hard(not bool(locked), "camera_unlocked", "locked=%s" % str(locked))
		else:
			_a.warn(false, "camera_unlocked", "no_input_locked_property")
	else:
		_a.warn(false, "camera_unlocked", "no_camera")

	_capture("05_final_playable")
	_set_phase(Phase.SUMMARY)


func _do_summary() -> void:
	_a.summary()
	_set_phase(Phase.DONE)


func _do_done() -> void:
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(_a.exit_code())


# ===================== Helpers =====================

func _set_phase(p: Phase) -> void:
	_phase = p
	_last_phase_change_frame = _total_frames
	_polls = 0


func _log(msg: String) -> void:
	print(PREFIX + msg)


func _capture(label: String) -> void:
	if _screenshot == null:
		return
	var filename := label
	var img_path = _screenshot.capture_v0(self, filename, OUTPUT_DIR)
	_snapshots.append(label)
	_log("CAPTURE|%s" % label)


func _inject_key() -> void:
	var ev := InputEventKey.new()
	ev.keycode = KEY_SPACE
	ev.pressed = true
	Input.parse_input_event(ev)


func _find_camera_controller():
	return root.find_child("Camera3D", true, false)


func _find_label_with_text(node: Node, text: String) -> Label:
	if node is Label and node.text == text:
		return node
	for child in node.get_children():
		var result := _find_label_with_text(child, text)
		if result != null:
			return result
	return null


func _find_button_with_text(node: Node, text: String) -> Button:
	if node is Button and node.text == text:
		return node
	for child in node.get_children():
		var result := _find_button_with_text(child, text)
		if result != null:
			return result
	return null


func _find_node_with_property(node: Node, prop: String) -> Node:
	if node.get(prop) != null:
		return node
	for child in node.get_children():
		var result := _find_node_with_property(child, prop)
		if result != null:
			return result
	return null
