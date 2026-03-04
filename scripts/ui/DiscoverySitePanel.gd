extends Control

# GATE.S1.DISCOVERY_INTERACT.PANEL.001: Minimal discovery site dock panel v0.
# Shows site_id, phase, and undock button when docked at a discovery site.
# Data: reads from SimBridge.GetDiscoveryListSnapshotV0() (existing query).

signal request_undock

const PREFIX := "DSPANEL|"

var _site_id_label: Label
var _phase_label: Label
var _undock_btn: Button
var _bridge: Node
# GATE.S1.DISCOVERY_INTERACT.SCAN.001: Scan/Analyze buttons
var _scan_btn: Button
var _analyze_btn: Button
var _current_site_id: String = ""
# GATE.S1.DISCOVERY_INTERACT.RESULTS.001: Results display
var _results_label: Label

func _ready() -> void:
	visible = false

	_bridge = get_node_or_null("/root/SimBridge")

	var panel := PanelContainer.new()
	panel.anchor_left = 0.5
	panel.anchor_top = 0.3
	panel.offset_left = -160
	panel.offset_right = 160
	panel.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	add_child(panel)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)
	panel.add_child(vbox)

	var title := Label.new()
	title.text = "Discovery Site"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	_site_id_label = Label.new()
	_site_id_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_site_id_label)

	_phase_label = Label.new()
	_phase_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_phase_label)

	# GATE.S1.DISCOVERY_INTERACT.SCAN.001: Scan/Analyze buttons
	_scan_btn = Button.new()
	_scan_btn.text = "Scan"
	_scan_btn.pressed.connect(_on_scan_pressed)
	vbox.add_child(_scan_btn)

	_analyze_btn = Button.new()
	_analyze_btn.text = "Analyze"
	_analyze_btn.pressed.connect(_on_analyze_pressed)
	vbox.add_child(_analyze_btn)

	# GATE.S1.DISCOVERY_INTERACT.RESULTS.001: results display
	_results_label = Label.new()
	_results_label.name = "ResultsLabel"
	_results_label.text = ""
	_results_label.add_theme_font_size_override("font_size", 13)
	_results_label.add_theme_color_override("font_color", Color(0.7, 1.0, 0.7, 1.0))
	_results_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	vbox.add_child(_results_label)

	_undock_btn = Button.new()
	_undock_btn.text = "Undock"
	_undock_btn.pressed.connect(_on_undock_pressed)
	vbox.add_child(_undock_btn)


func open_v0(site_id: String) -> void:
	_current_site_id = site_id
	_site_id_label.text = site_id if site_id != "" else "Unknown Site"

	var phase_text := _resolve_phase_v0(site_id)
	_phase_label.text = "Phase: " + phase_text
	_update_button_states_v0(phase_text)
	_update_results_v0(site_id)

	visible = true
	print(PREFIX + "OPEN|site_id=" + site_id + "|phase=" + phase_text)


func close_v0() -> void:
	if not visible:
		return
	visible = false
	print(PREFIX + "CLOSE")


func get_site_id_text_v0() -> String:
	return _site_id_label.text if _site_id_label else ""


func get_phase_text_v0() -> String:
	return _phase_label.text if _phase_label else ""


func _resolve_phase_v0(site_id: String) -> String:
	if _bridge == null or not _bridge.has_method("GetDiscoveryListSnapshotV0"):
		return "Unknown"

	var list: Array = _bridge.call("GetDiscoveryListSnapshotV0")
	for entry in list:
		if str(entry.get("discovery_id", "")) == site_id:
			var scanned: int = int(entry.get("scanned_bps", 0))
			var analyzed: int = int(entry.get("analyzed_bps", 0))
			if analyzed >= 10000:
				return "Analyzed"
			elif scanned >= 10000:
				return "Scanned"
			else:
				return "Seen"

	return "Unknown"


# GATE.S1.DISCOVERY_INTERACT.SCAN.001: button state management
func _update_button_states_v0(phase_text: String) -> void:
	if _scan_btn:
		_scan_btn.disabled = phase_text != "Seen"
	if _analyze_btn:
		_analyze_btn.disabled = phase_text != "Scanned"

func _on_scan_pressed() -> void:
	if _bridge == null or _current_site_id.is_empty():
		return
	if _bridge.has_method("AdvanceDiscoveryPhaseV0"):
		var result: Dictionary = _bridge.call("AdvanceDiscoveryPhaseV0", _current_site_id)
		print(PREFIX + "SCAN|site_id=" + _current_site_id + "|ok=" + str(result.get("ok", false)))
		var phase_text := _resolve_phase_v0(_current_site_id)
		_phase_label.text = "Phase: " + phase_text
		_update_button_states_v0(phase_text)
		_update_results_v0(_current_site_id)

func _on_analyze_pressed() -> void:
	if _bridge == null or _current_site_id.is_empty():
		return
	if _bridge.has_method("AdvanceDiscoveryPhaseV0"):
		var result: Dictionary = _bridge.call("AdvanceDiscoveryPhaseV0", _current_site_id)
		print(PREFIX + "ANALYZE|site_id=" + _current_site_id + "|ok=" + str(result.get("ok", false)))
		var phase_text := _resolve_phase_v0(_current_site_id)
		_phase_label.text = "Phase: " + phase_text
		_update_button_states_v0(phase_text)
		_update_results_v0(_current_site_id)

# GATE.S1.DISCOVERY_INTERACT.RESULTS.001: show scan results
func _update_results_v0(site_id: String) -> void:
	if _results_label == null:
		return
	if _bridge == null or not _bridge.has_method("GetDiscoverySnapshotV0"):
		_results_label.text = ""
		return

	var snap: Dictionary = _bridge.call("GetDiscoverySnapshotV0", site_id)
	var scanned: int = int(snap.get("scanned_site_count", 0))
	var analyzed: int = int(snap.get("analyzed_site_count", 0))

	if analyzed > 0:
		_results_label.text = "Analysis complete. Discoveries unlocked."
	elif scanned > 0:
		_results_label.text = "Scan data collected. Analyze to reveal findings."
	else:
		_results_label.text = ""

func _on_undock_pressed() -> void:
	request_undock.emit()
