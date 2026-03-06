extends PanelContainer

# GATE.S6.REVEAL.DISCOVERY_HUD.001: Discovery site HUD panel.
# Shows discovery sites at the current node with phase, icon, and Scan button.
# Polls GetDiscoverySnapshotV0(node_id) every 1 second.
# Progressive disclosure: hidden when no discoveries at current node.
# GATE.S6.OUTCOME.REWARD_BRIDGE.001: Outcome reward summary for Analyzed discoveries.

const PREFIX := "DSPANEL|"

var _bridge: Node = null
var _title_label: Label = null
var _sites_container: VBoxContainer = null
var _poll_timer: float = 0.0
const _POLL_INTERVAL: float = 1.0
var _last_node_id: String = ""
# Keyed by discovery_id -> outcome dict (from GetDiscoveryOutcomesV0)
var _outcomes_by_discovery: Dictionary = {}

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

	# Start hidden; poll will show/hide based on data
	visible = false
	print(PREFIX + "READY")


func _process(delta: float) -> void:
	_poll_timer += delta
	if _poll_timer >= _POLL_INTERVAL:
		_poll_timer = 0.0
		_refresh_v0()


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

	# Rebuild site rows if node changed
	if node_id != _last_node_id:
		_last_node_id = node_id
		_rebuild_sites_v0(sites)
	else:
		_update_site_rows_v0(sites)

	visible = true


func _rebuild_sites_v0(sites: Array) -> void:
	# Clear existing rows
	for child in _sites_container.get_children():
		_sites_container.remove_child(child)
		child.queue_free()

	for site_dict in sites:
		var site_id: String = str(site_dict.get("site_id", ""))
		var phase: String = str(site_dict.get("phase", "SEEN"))
		_add_site_row_v0(site_id, phase)


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
			scan_btn.disabled = (phase != "SEEN")

	# If site count changed, do a full rebuild on next cycle
	if sites.size() != rows.size():
		_last_node_id = ""


func _add_site_row_v0(site_id: String, phase: String) -> void:
	var row := VBoxContainer.new()
	row.name = "SiteRow_" + site_id.replace(".", "_")

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
	scan_btn.pressed.connect(_on_scan_pressed_v0.bind(site_id))
	hbox.add_child(scan_btn)

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


func _on_scan_pressed_v0(site_id: String) -> void:
	if _bridge == null or site_id.is_empty():
		return
	if _bridge.has_method("DispatchScanDiscoveryV0"):
		_bridge.call("DispatchScanDiscoveryV0", site_id)
		print(PREFIX + "SCAN|site_id=" + site_id)
		# Force immediate refresh after scan
		_last_node_id = ""
		_refresh_v0()


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
