extends PanelContainer

# GATE.S6.REVEAL.DISCOVERY_HUD.001: Discovery site HUD panel.
# Shows discovery sites at the current node with phase, icon, and Scan button.
# Polls GetDiscoverySnapshotV0(node_id) every 1 second.
# Progressive disclosure: hidden when no discoveries at current node.
# GATE.S6.OUTCOME.REWARD_BRIDGE.001: Outcome reward summary for Analyzed discoveries.
# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Scan hold timer with progress ring VFX.

const PREFIX := "DSPANEL|"

var _bridge: Node = null
var _title_label: Label = null
var _sites_container: VBoxContainer = null
var _poll_timer: float = 0.0
const _POLL_INTERVAL: float = 1.0
var _last_node_id: String = ""
# Keyed by discovery_id -> outcome dict (from GetDiscoveryOutcomesV0)
var _outcomes_by_discovery: Dictionary = {}
# GATE.S6.FRACTURE_DISCOVERY.UI.001: Derelict analysis progress label.
var _derelict_status_label: Label = null

# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Scan ceremony state.
var _scan_ceremony_active: bool = false
var _scan_ceremony_site_id: String = ""
var _scan_ceremony_family: String = ""
var _galaxy_view = null
# Progress bar shown in the scan button row during ceremony.
var _scan_progress_bar_ref: ProgressBar = null

func _ready() -> void:
	_bridge = get_node_or_null("/root/SimBridge")

	add_theme_stylebox_override("panel", UITheme.make_panel_hud())

	custom_minimum_size = Vector2(220, 0)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 6)
	add_child(vbox)

	_title_label = Label.new()
	_title_label.name = "TitleLabel"
	_title_label.text = "Discovery Site"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_title_label.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	vbox.add_child(_title_label)

	var sep := HSeparator.new()
	vbox.add_child(sep)

	_sites_container = VBoxContainer.new()
	_sites_container.name = "SitesContainer"
	_sites_container.add_theme_constant_override("separation", 8)
	vbox.add_child(_sites_container)

	# GATE.S6.FRACTURE_DISCOVERY.UI.001: Derelict analysis progress label.
	_derelict_status_label = Label.new()
	_derelict_status_label.name = "DerelictStatusLabel"
	_derelict_status_label.text = ""
	_derelict_status_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_derelict_status_label.add_theme_color_override("font_color", UITheme.CYAN)
	_derelict_status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_derelict_status_label.visible = false
	vbox.add_child(_derelict_status_label)

	# Start hidden; poll will show/hide based on data
	visible = false
	print(PREFIX + "READY")


func _process(delta: float) -> void:
	_poll_timer += delta
	if _poll_timer >= _POLL_INTERVAL:
		_poll_timer = 0.0
		_refresh_v0()

	# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Update scan ceremony progress display.
	if _scan_ceremony_active:
		_update_scan_ceremony_progress_v0()


func _refresh_v0() -> void:
	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		visible = false
		return

	if not _bridge.has_method("GetPlayerStateV0"):
		visible = false
		return

	var ps: Dictionary = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))

	if node_id.is_empty():
		visible = false
		return

	# Only query bridge if node changed or forced refresh
	if not _bridge.has_method("GetDiscoverySnapshotV0"):
		visible = false
		return

	var sites: Array = _bridge.call("GetDiscoverySnapshotV0", node_id)

	if sites.size() == 0:
		visible = false
		return

	# GATE.S6.OUTCOME.REWARD_BRIDGE.001: Fetch outcome rewards and index by discovery_id.
	_outcomes_by_discovery = {}
	if _bridge.has_method("GetDiscoveryOutcomesV0"):
		var outcomes: Array = _bridge.call("GetDiscoveryOutcomesV0")
		for outcome in outcomes:
			var disc_id: String = str(outcome.get("discovery_id", ""))
			if not disc_id.is_empty():
				_outcomes_by_discovery[disc_id] = outcome

	# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Cancel ceremony if node changed during scan.
	if node_id != _last_node_id and _scan_ceremony_active:
		_cancel_scan_ceremony_v0()

	# Rebuild site rows if node changed
	if node_id != _last_node_id:
		_last_node_id = node_id
		_rebuild_sites_v0(sites)
	else:
		_update_site_rows_v0(sites)

	# GATE.S6.FRACTURE_DISCOVERY.UI.001: Show derelict analysis progress.
	_update_derelict_status_v0()

	visible = true


func _rebuild_sites_v0(sites: Array) -> void:
	# Clear existing rows
	for child in _sites_container.get_children():
		_sites_container.remove_child(child)
		child.queue_free()

	for site_dict in sites:
		var site_id: String = str(site_dict.get("site_id", ""))
		var phase: String = str(site_dict.get("phase", "SEEN"))
		# Bridge GetDiscoverySnapshotV0 returns "kind" for family (e.g., DERELICT, RUIN, SIGNAL).
		var family: String = str(site_dict.get("kind", ""))
		_add_site_row_v0(site_id, phase, family)


func _update_site_rows_v0(sites: Array) -> void:
	# Update existing rows in-place (same count and order assumed for same node)
	var rows := _sites_container.get_children()
	for i in range(min(sites.size(), rows.size())):
		var site_dict: Dictionary = sites[i]
		var phase: String = str(site_dict.get("phase", "SEEN"))
		var row: Control = rows[i]
		var phase_lbl: Label = row.get_node_or_null("PhaseLabel")
		var scan_btn: Button = row.get_node_or_null("ScanButton")
		if phase_lbl:
			phase_lbl.text = _phase_display_v0(phase)
			phase_lbl.add_theme_color_override("font_color", _phase_color_v0(phase))
		if scan_btn:
			# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Disable button during active ceremony.
			if _scan_ceremony_active:
				scan_btn.disabled = true
			else:
				scan_btn.disabled = (phase != "SEEN")
				scan_btn.text = "Scan"

	# If site count changed, do a full rebuild on next cycle
	if sites.size() != rows.size():
		_last_node_id = ""


func _add_site_row_v0(site_id: String, phase: String, family: String = "") -> void:
	var row := VBoxContainer.new()
	row.name = "SiteRow_" + site_id.replace(".", "_")
	# Store family as metadata for ceremony color lookup.
	row.set_meta("family", family)

	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 6)
	row.add_child(hbox)

	var phase_lbl := Label.new()
	phase_lbl.name = "PhaseLabel"
	phase_lbl.text = _phase_display_v0(phase)
	phase_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	phase_lbl.add_theme_color_override("font_color", _phase_color_v0(phase))
	phase_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(phase_lbl)

	var scan_btn := Button.new()
	scan_btn.name = "ScanButton"
	scan_btn.text = "Scan"
	scan_btn.disabled = (phase != "SEEN")
	scan_btn.custom_minimum_size = Vector2(60, 0)
	# Bind the site_id for this row
	scan_btn.pressed.connect(_on_scan_pressed_v0.bind(site_id, family))
	hbox.add_child(scan_btn)

	# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Progress bar for scan ceremony (hidden by default).
	var progress_bar := ProgressBar.new()
	progress_bar.name = "ScanProgressBar"
	progress_bar.custom_minimum_size = Vector2(0, 6)
	progress_bar.max_value = 100
	progress_bar.value = 0
	progress_bar.show_percentage = false
	progress_bar.visible = false
	var scan_fill := StyleBoxFlat.new()
	scan_fill.bg_color = UITheme.CYAN
	progress_bar.add_theme_stylebox_override("fill", scan_fill)
	var scan_bg := StyleBoxFlat.new()
	scan_bg.bg_color = Color(0.1, 0.12, 0.15, 0.8)
	progress_bar.add_theme_stylebox_override("background", scan_bg)
	row.add_child(progress_bar)

	# GATE.S6.OUTCOME.REWARD_BRIDGE.001: Show reward summary for Analyzed discoveries.
	if phase == "ANALYZED" and _outcomes_by_discovery.has(site_id):
		var outcome: Dictionary = _outcomes_by_discovery[site_id]
		var reward_text := _build_reward_text_v0(outcome)
		if not reward_text.is_empty():
			var reward_lbl := Label.new()
			reward_lbl.name = "RewardLabel"
			reward_lbl.text = reward_text
			reward_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
			reward_lbl.add_theme_color_override("font_color", UITheme.GOLD)
			reward_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
			row.add_child(reward_lbl)

	_sites_container.add_child(row)


func _build_reward_text_v0(outcome: Dictionary) -> String:
	var parts: Array[String] = []
	var credits: int = int(outcome.get("credit_reward", 0))
	if credits > 0:
		parts.append("+%d cr" % credits)
	var loot: Array = outcome.get("loot_items", [])
	for item in loot:
		var good_id: String = str(item.get("good_id", ""))
		var qty: int = int(item.get("qty", 0))
		if not good_id.is_empty() and qty > 0:
			parts.append("+%d %s" % [qty, good_id])
	return " ".join(parts)


# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Scan button now initiates a 4-second scan ceremony
# instead of dispatching instantly. The actual DispatchScanDiscoveryV0 is called by
# GalaxyView when the ceremony completes.
func _on_scan_pressed_v0(site_id: String, family: String = "") -> void:
	if _bridge == null or site_id.is_empty():
		return
	if _scan_ceremony_active:
		# Already scanning — cancel current ceremony.
		_cancel_scan_ceremony_v0()
		return

	# Start scan ceremony via GalaxyView.
	_ensure_galaxy_view()
	if _galaxy_view and _galaxy_view.has_method("BeginScanCeremonyV0"):
		_galaxy_view.call("BeginScanCeremonyV0", site_id, family)
		_scan_ceremony_active = true
		_scan_ceremony_site_id = site_id
		_scan_ceremony_family = family
		print(PREFIX + "SCAN_CEREMONY_START|site_id=" + site_id + "|family=" + family)

		# Connect to completion signal (if not already connected).
		if not _galaxy_view.is_connected("scan_ceremony_completed", _on_scan_ceremony_completed_v0):
			_galaxy_view.connect("scan_ceremony_completed", _on_scan_ceremony_completed_v0)

		# Update button text and show progress bar.
		_set_scan_button_scanning_v0(site_id, true)
	else:
		# Fallback: no GalaxyView available — instant scan (legacy behavior for headless bots).
		if _bridge.has_method("DispatchScanDiscoveryV0"):
			_bridge.call("DispatchScanDiscoveryV0", site_id)
			print(PREFIX + "SCAN|site_id=" + site_id + " (instant fallback)")
			_play_discovery_chime_v0("SCANNED")
			_last_node_id = ""
			_refresh_v0()


# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Cancel active scan ceremony.
func _cancel_scan_ceremony_v0() -> void:
	if not _scan_ceremony_active:
		return
	_ensure_galaxy_view()
	if _galaxy_view and _galaxy_view.has_method("CancelScanCeremonyV0"):
		_galaxy_view.call("CancelScanCeremonyV0")
	_scan_ceremony_active = false
	_set_scan_button_scanning_v0(_scan_ceremony_site_id, false)
	_scan_ceremony_site_id = ""
	_scan_ceremony_family = ""
	print(PREFIX + "SCAN_CEREMONY_CANCEL")


# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Called when GalaxyView completes the scan ceremony.
func _on_scan_ceremony_completed_v0(site_id: String) -> void:
	if site_id != _scan_ceremony_site_id:
		return
	_scan_ceremony_active = false
	_set_scan_button_scanning_v0(site_id, false)
	print(PREFIX + "SCAN_CEREMONY_COMPLETE|site_id=" + site_id)
	# GATE.S7.AUDIO_WIRING.DISCOVERY_CHIMES.001: play phase transition chime on completion.
	_play_discovery_chime_v0("SCANNED")
	_scan_ceremony_site_id = ""
	_scan_ceremony_family = ""
	# Force immediate refresh after scan to update phase display.
	_last_node_id = ""
	_refresh_v0()


# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Update button text and progress bar during ceremony.
func _update_scan_ceremony_progress_v0() -> void:
	_ensure_galaxy_view()
	if _galaxy_view == null or not _galaxy_view.has_method("GetScanCeremonyProgressV0"):
		return

	var progress: float = _galaxy_view.call("GetScanCeremonyProgressV0")

	# Check if ceremony ended without signal (e.g., externally cancelled).
	if not _galaxy_view.call("IsScanCeremonyActiveV0"):
		if _scan_ceremony_active:
			# Ceremony ended — reset.
			_scan_ceremony_active = false
			_set_scan_button_scanning_v0(_scan_ceremony_site_id, false)
			_scan_ceremony_site_id = ""
			_scan_ceremony_family = ""
		return

	# Update scan button text with percentage.
	var pct: int = int(progress * 100.0)
	for row in _sites_container.get_children():
		var scan_btn: Button = row.get_node_or_null("ScanButton")
		if scan_btn and scan_btn.disabled:
			# Find if this is the scanning row.
			var row_name: String = row.name
			var expected_name: String = "SiteRow_" + _scan_ceremony_site_id.replace(".", "_")
			if row_name == expected_name:
				scan_btn.text = "Scanning %d%%" % pct
				# Update progress bar.
				var prog_bar: ProgressBar = row.get_node_or_null("ScanProgressBar")
				if prog_bar:
					prog_bar.value = pct


# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Toggle scan button appearance during ceremony.
func _set_scan_button_scanning_v0(site_id: String, scanning: bool) -> void:
	var expected_name: String = "SiteRow_" + site_id.replace(".", "_")
	for row in _sites_container.get_children():
		if row.name != expected_name:
			continue
		var scan_btn: Button = row.get_node_or_null("ScanButton")
		var prog_bar: ProgressBar = row.get_node_or_null("ScanProgressBar")
		if scan_btn:
			if scanning:
				scan_btn.text = "Scanning 0%"
				scan_btn.disabled = true
			else:
				scan_btn.text = "Scan"
				# Phase check will re-enable/disable on next refresh.
		if prog_bar:
			prog_bar.visible = scanning
			prog_bar.value = 0
		break


# GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Find GalaxyView reference.
func _ensure_galaxy_view() -> void:
	if _galaxy_view and is_instance_valid(_galaxy_view):
		return
	if get_tree():
		_galaxy_view = get_tree().root.find_child("GalaxyView", true, false)


# GATE.S7.AUDIO_WIRING.DISCOVERY_CHIMES.001: play discovery phase audio.
func _play_discovery_chime_v0(phase: String) -> void:
	var disc_audio := get_node_or_null("/root/DiscoveryAudio")
	if disc_audio == null:
		disc_audio = load("res://scripts/audio/discovery_audio.gd").new()
		disc_audio.name = "DiscoveryAudio"
		get_tree().root.add_child(disc_audio)
	if disc_audio.has_method("play_phase_chime"):
		disc_audio.call("play_phase_chime", phase)


func _phase_display_v0(phase: String) -> String:
	match phase:
		"SEEN":
			return "?  Seen"
		"SCANNED":
			return "~  Scanned"
		"ANALYZED":
			return "!  Analyzed"
	return "?  Unknown"


func _phase_color_v0(phase: String) -> Color:
	return UITheme.discovery_phase_color(phase)


# GATE.S6.FRACTURE_DISCOVERY.UI.001: Update derelict analysis progress label.
func _update_derelict_status_v0() -> void:
	if _derelict_status_label == null or _bridge == null:
		return
	if not _bridge.has_method("GetFractureDiscoveryStatusV0"):
		_derelict_status_label.visible = false
		return
	var status: Dictionary = _bridge.call("GetFractureDiscoveryStatusV0")
	var progress: String = str(status.get("analysis_progress", "unknown"))
	if progress == "unknown" or progress.is_empty():
		_derelict_status_label.visible = false
		return
	_derelict_status_label.visible = true
	if status.get("unlocked", false):
		_derelict_status_label.text = "Fracture Drive: ACTIVE"
		_derelict_status_label.add_theme_color_override("font_color", UITheme.GREEN)
	else:
		_derelict_status_label.text = "Derelict Analysis: %s" % progress
		_derelict_status_label.add_theme_color_override("font_color", UITheme.CYAN)
