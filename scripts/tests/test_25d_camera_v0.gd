extends SceneTree

# 2.5D Camera Evaluation Bot
# Captures galaxy map screenshots at different orbit angles to verify Y-spread visibility.
# Run: godot --path . -s res://scripts/tests/test_25d_camera_v0.gd -- --seed=42

var _phase: int = 0
var _frame: int = 0
var _gm = null
var _cam = null
var _bridge = null
var _screenshot_dir: String = "user://screenshots_25d/"
var _phases_done: bool = false

func _initialize() -> void:
	# Delete stale quicksave to prevent contamination.
	var qpath := OS.get_user_data_dir() + "/quicksave.json"
	if FileAccess.file_exists(qpath):
		DirAccess.remove_absolute(qpath)

func _process(_delta: float) -> bool:
	_frame += 1
	if _frame < 10:
		return false  # Let scene load.

	if _gm == null:
		_gm = root.get_node_or_null("GameManager")
		if _gm == null:
			_gm = root.find_child("GameManager", true, false)
	if _cam == null:
		_cam = root.find_child("FollowCamera", true, false)
		if _cam == null:
			_cam = root.find_child("PlayerFollowCamera", true, false)
	if _bridge == null:
		_bridge = root.get_node_or_null("SimBridge")

	if _gm == null or _cam == null:
		if _frame > 120:
			print("25D|FAIL: Could not find GameManager or Camera after 120 frames")
			quit(1)
		return false

	if _phases_done:
		return false

	match _phase:
		0:
			_do_phase_0_setup()
		1:
			_do_phase_1_galaxy_map()
		2:
			_do_phase_2_tilt_30()
		3:
			_do_phase_3_tilt_60()
		4:
			_do_phase_4_yaw_rotate()
		5:
			_do_phase_5_report_and_quit()

	return false

func _do_phase_0_setup() -> void:
	# Wait for bridge to be ready.
	if _bridge == null or not _bridge.has_method("GetGalaxySnapshotV0"):
		return
	var snap = _bridge.call("GetGalaxySnapshotV0")
	if snap == null:
		return

	# Check Y-spread in galaxy data.
	var nodes = snap.get("system_nodes")
	if nodes == null or nodes.size() == 0:
		return

	var min_y: float = 999999.0
	var max_y: float = -999999.0
	var y_values: Array = []
	for n in nodes:
		var py: float = float(n.get("pos_y", 0.0))
		y_values.append(py)
		if py < min_y: min_y = py
		if py > max_y: max_y = py

	var y_range: float = max_y - min_y
	print("25D|Y-SPREAD: min=%.2f max=%.2f range=%.2f (sim units)" % [min_y, max_y, y_range])
	print("25D|Y-SPREAD_VISUAL: range=%.0f units (at 25x scale)" % [y_range * 25.0])

	if y_range < 0.1:
		print("25D|FAIL: Y-spread is effectively zero — stars are still flat!")
		quit(1)
		return
	else:
		print("25D|PASS: Y-spread detected across %d stars" % nodes.size())

	# Ensure screenshot dir exists.
	DirAccess.make_dir_recursive_absolute(OS.get_user_data_dir() + "/screenshots_25d")

	_phase = 1
	_frame = 0
	print("25D|Phase 1: Opening galaxy map...")

func _do_phase_1_galaxy_map() -> void:
	# Open galaxy map via TAB toggle.
	if _frame == 1:
		if _cam.has_method("toggle_strategic_altitude_v0"):
			_cam.call("toggle_strategic_altitude_v0")
		else:
			print("25D|WARN: No toggle_strategic_altitude_v0 method")
	# Wait for zoom animation.
	if _frame < 60:
		return

	# Capture top-down screenshot.
	_capture_screenshot("galaxy_top_down")
	print("25D|SCREENSHOT: galaxy_top_down (pitch=0, yaw=0)")

	# Report camera state.
	var pitch = _cam.get("_orbit_pitch")
	var yaw = _cam.get("_orbit_yaw")
	var alt = _cam.get("_altitude")
	print("25D|CAMERA: pitch=%.3f yaw=%.3f altitude=%.0f" % [pitch if pitch != null else 0.0, yaw if yaw != null else 0.0, alt if alt != null else 0.0])

	_phase = 2
	_frame = 0
	print("25D|Phase 2: Tilting to 30 degrees...")

func _do_phase_2_tilt_30() -> void:
	# Set orbit pitch to ~30 degrees (0.524 rad).
	if _frame == 1:
		_cam.set("_orbit_pitch", 0.524)
	if _frame < 30:
		return

	_capture_screenshot("galaxy_tilt_30deg")
	var pitch = _cam.get("_orbit_pitch")
	print("25D|SCREENSHOT: galaxy_tilt_30deg (pitch=%.3f)" % [pitch if pitch != null else 0.0])

	_phase = 3
	_frame = 0
	print("25D|Phase 3: Tilting to 60 degrees...")

func _do_phase_3_tilt_60() -> void:
	# Set orbit pitch to ~60 degrees (1.047 rad).
	if _frame == 1:
		_cam.set("_orbit_pitch", 1.047)
	if _frame < 30:
		return

	_capture_screenshot("galaxy_tilt_60deg")
	var pitch = _cam.get("_orbit_pitch")
	print("25D|SCREENSHOT: galaxy_tilt_60deg (pitch=%.3f)" % [pitch if pitch != null else 0.0])

	_phase = 4
	_frame = 0
	print("25D|Phase 4: Adding yaw rotation...")

func _do_phase_4_yaw_rotate() -> void:
	# Keep 45° pitch, rotate yaw to ~45 degrees.
	if _frame == 1:
		_cam.set("_orbit_pitch", 0.785)  # 45 degrees
		_cam.set("_orbit_yaw", 0.785)    # 45 degrees yaw
	if _frame < 30:
		return

	_capture_screenshot("galaxy_orbit_45_45")
	var pitch = _cam.get("_orbit_pitch")
	var yaw = _cam.get("_orbit_yaw")
	print("25D|SCREENSHOT: galaxy_orbit_45_45 (pitch=%.3f yaw=%.3f)" % [pitch if pitch != null else 0.0, yaw if yaw != null else 0.0])

	_phase = 5
	_frame = 0

func _do_phase_5_report_and_quit() -> void:
	print("25D|COMPLETE: All screenshots captured in %s" % _screenshot_dir)
	print("25D|SUMMARY:")
	print("25D|  - galaxy_top_down: Default view (should look same as before)")
	print("25D|  - galaxy_tilt_30deg: 30° tilt (should see disc thickness)")
	print("25D|  - galaxy_tilt_60deg: 60° tilt (dramatic angle, disc shape visible)")
	print("25D|  - galaxy_orbit_45_45: 45° pitch + 45° yaw (orbited view)")

	# Stop sim before quit.
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")

	_phases_done = true
	await create_timer(0.5).timeout
	quit(0)

func _capture_screenshot(name: String) -> void:
	var img := get_root().get_viewport().get_texture().get_image()
	if img == null:
		print("25D|WARN: Could not capture screenshot for %s" % name)
		return
	var path := _screenshot_dir + name + ".png"
	var full_path := OS.get_user_data_dir() + "/screenshots_25d/" + name + ".png"
	img.save_png(full_path)
	print("25D|SAVED: %s" % full_path)
