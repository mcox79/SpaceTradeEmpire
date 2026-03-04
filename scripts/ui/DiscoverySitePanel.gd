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

	_undock_btn = Button.new()
	_undock_btn.text = "Undock"
	_undock_btn.pressed.connect(_on_undock_pressed)
	vbox.add_child(_undock_btn)


func open_v0(site_id: String) -> void:
	_site_id_label.text = site_id if site_id != "" else "Unknown Site"

	var phase_text := _resolve_phase_v0(site_id)
	_phase_label.text = "Phase: " + phase_text

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


func _on_undock_pressed() -> void:
	request_undock.emit()
