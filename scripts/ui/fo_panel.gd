# scripts/ui/fo_panel.gd
# GATE.T18.CHARACTER.UI_FULL.001: First Officer panel overhaul — dialogue history,
# promotion UI, War Faces NPC display (including dead), gold FO toasts.
extends PanelContainer

var _bridge = null
var _name_label: Label = null
var _archetype_label: Label = null
var _tier_label: Label = null
var _status_label: Label = null

# Dialogue history — scrollable, last 5 lines.
var _dialogue_scroll: ScrollContainer = null
var _dialogue_vbox: VBoxContainer = null
var _dialogue_history: Array[String] = []
const _MAX_DIALOGUE_LINES: int = 5

# Promotion section.
var _promo_section: VBoxContainer = null
var _promo_visible: bool = false

# NPC section.
var _npc_sep: HSeparator = null
var _npc_title: Label = null
var _npc_section: VBoxContainer = null

var _slow_poll_elapsed: float = 0.0
const _SLOW_POLL_INTERVAL: float = 2.0

# GATE.T58.UI.FO_SERVICE.001: Service record section.
var _service_sep: HSeparator = null
var _service_title: Label = null
var _service_section: VBoxContainer = null
var _service_visible: bool = false

# GATE.T45.DEEP_DREAD.COMMS_STATIC.001: Comms degradation at distance.
# At hop>=4, randomly replace 5-15% of characters with static glyphs.
# At hop>=6, heavier corruption. Click dialogue to clear static (re-read).
var _comms_hops: int = 0
const _STATIC_GLYPHS: String = "░▒▓█▐▌╫╪╬╩╦╠╣║═"

# Auto-show: flash panel visible on dialogue/promotion, then auto-hide after timeout.
var _auto_show_timer: float = -1.0
const _AUTO_SHOW_DURATION: float = 6.0

# GATE.T59.DISC_VIZ.TUTORIAL_BEAT.001: Scan tutorial — FO-guided first-scan walkthrough.
var _scan_tut_shown: bool = false  # Once true, never fires again.
var _scan_tut_phase: int = -1  # -1=waiting, 0=sensor online, 1=near site, 2=scan done
var _scan_tut_phase_timer: float = 0.0  # Countdown for current phase display.
const _SCAN_TUT_DISPLAY_SECS: float = 8.0
var _scan_tut_discovery_count: int = -1  # Baseline count at phase 1 entry.


func _ready() -> void:
	name = "FOPanel"
	visible = false
	custom_minimum_size = Vector2(280, 0)
	position = Vector2(10, 470)
	var style := UITheme.make_panel_ship_computer()
	add_theme_stylebox_override("panel", style)
	UITheme.add_corner_brackets(self)
	UITheme.add_scanline_overlay(self)
	mouse_filter = Control.MOUSE_FILTER_PASS

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", UITheme.SPACE_XS)
	add_child(vbox)

	# --- Title ---
	var title := Label.new()
	title.text = "FIRST OFFICER"
	title.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	title.add_theme_color_override("font_color", UITheme.CYAN)
	vbox.add_child(title)

	# --- FO identity row: archetype icon + name + tier badge ---
	var identity_row := HBoxContainer.new()
	identity_row.add_theme_constant_override("separation", 6)
	vbox.add_child(identity_row)

	_archetype_label = Label.new()
	_archetype_label.text = ""
	_archetype_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	identity_row.add_child(_archetype_label)

	_name_label = Label.new()
	_name_label.text = ""
	_name_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_name_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	identity_row.add_child(_name_label)

	_tier_label = Label.new()
	_tier_label.text = ""
	_tier_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	identity_row.add_child(_tier_label)

	_status_label = Label.new()
	_status_label.text = ""
	_status_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_status_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	vbox.add_child(_status_label)

	# --- Dialogue history (scrollable, last 5 lines) ---
	var dialogue_sep := HSeparator.new()
	vbox.add_child(dialogue_sep)

	var dialogue_title := Label.new()
	dialogue_title.text = "COMMS LOG"
	dialogue_title.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	dialogue_title.add_theme_color_override("font_color", UITheme.CYAN)
	vbox.add_child(dialogue_title)

	_dialogue_scroll = ScrollContainer.new()
	_dialogue_scroll.custom_minimum_size = Vector2(0, 80)
	_dialogue_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_dialogue_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	vbox.add_child(_dialogue_scroll)

	_dialogue_vbox = VBoxContainer.new()
	_dialogue_vbox.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_dialogue_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_dialogue_scroll.add_child(_dialogue_vbox)

	# --- Promotion section (hidden by default) ---
	_promo_section = VBoxContainer.new()
	_promo_section.add_theme_constant_override("separation", UITheme.SPACE_SM)
	_promo_section.visible = false
	vbox.add_child(_promo_section)

	# --- GATE.T58.UI.FO_SERVICE.001: Service Record section ---
	_service_sep = HSeparator.new()
	_service_sep.visible = false
	vbox.add_child(_service_sep)

	_service_title = Label.new()
	_service_title.text = "SERVICE RECORD"
	_service_title.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_service_title.add_theme_color_override("font_color", UITheme.CYAN)
	_service_title.visible = false
	vbox.add_child(_service_title)

	_service_section = VBoxContainer.new()
	_service_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_service_section.visible = false
	vbox.add_child(_service_section)

	# --- NPC section: Known Contacts (hidden when empty) ---
	_npc_sep = HSeparator.new()
	_npc_sep.visible = false
	vbox.add_child(_npc_sep)

	_npc_title = Label.new()
	_npc_title.text = "Known Contacts"
	_npc_title.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_npc_title.add_theme_color_override("font_color", UITheme.CYAN)
	_npc_title.visible = false
	vbox.add_child(_npc_title)

	_npc_section = VBoxContainer.new()
	_npc_section.add_theme_constant_override("separation", UITheme.SPACE_XS)
	_npc_section.visible = false
	vbox.add_child(_npc_section)

	_bridge = get_node_or_null("/root/SimBridge")


func _physics_process(delta: float) -> void:
	# Auto-show countdown: hide panel after duration expires.
	if _auto_show_timer > 0.0:
		_auto_show_timer -= delta
		if _auto_show_timer <= 0.0:
			_auto_show_timer = -1.0
			visible = false

	_slow_poll_elapsed += delta
	if _slow_poll_elapsed < _SLOW_POLL_INTERVAL:
		return
	_slow_poll_elapsed = 0.0

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		return

	_refresh_fo_state()
	_poll_comms_hops()
	_poll_fo_dialogue()
	_check_scan_tutorial()
	_refresh_promotion()
	_refresh_service_record()
	_refresh_npcs()


# ---------- FO State ----------

func _refresh_fo_state() -> void:
	if not _bridge.has_method("GetFirstOfficerStateV0"):
		return
	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	if fo.is_empty():
		visible = false
		return
	# FO panel visibility controlled by F-key toggle in hud.gd/game_manager.gd.
	# Panel does not self-show — user toggles with F key.
	var fo_name: String = str(fo.get("name", "None"))
	var fo_type: String = str(fo.get("type", "None"))
	var fo_tier: String = str(fo.get("tier", "Early"))
	var promoted: bool = fo.get("promoted", false)
	# FEEL_PASS5_P5: Archetype icon with color coding.
	match fo_type:
		"Analyst":
			_archetype_label.text = "◈"
			_archetype_label.add_theme_color_override("font_color", UITheme.GOLD)
		"Veteran":
			_archetype_label.text = "⚔"
			_archetype_label.add_theme_color_override("font_color", UITheme.CYAN)
		"Pathfinder":
			_archetype_label.text = "◉"
			_archetype_label.add_theme_color_override("font_color", UITheme.ORANGE)
		_:
			_archetype_label.text = "◇"
			_archetype_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_name_label.text = fo_name
	# Tier badge.
	match fo_tier:
		"Early": _tier_label.text = "I"
		"Mid": _tier_label.text = "II"
		"Late": _tier_label.text = "III"
		_: _tier_label.text = ""
	_tier_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	if promoted:
		_status_label.text = "Promoted"
		_status_label.add_theme_color_override("font_color", UITheme.GREEN)
	elif fo.get("in_promotion_window", false):
		_status_label.text = "Promotion available"
		_status_label.add_theme_color_override("font_color", UITheme.GOLD)
	else:
		_status_label.text = ""


# ---------- GATE.T59.DISC_VIZ.TUTORIAL_BEAT.001: Scan Tutorial ----------

## FO-guided first-scan walkthrough. Three phases:
##   Phase 0 — sensor_suite tech unlocked → "Sensors online" dialogue.
##   Phase 1 — player near a discovery site → "Readings strengthening" dialogue.
##   Phase 2 — first scan completed → "Excellent scan" dialogue.
## Each phase shows for ~8 seconds via the existing dialogue + flash pattern.
## Fires once per playthrough (guarded by _scan_tut_shown).
func _check_scan_tutorial() -> void:
	if _scan_tut_shown:
		return
	# Respect tutorial toggle (same pattern as game_manager milestone toasts).
	var settings_mgr = get_node_or_null("/root/SettingsManager")
	if settings_mgr and settings_mgr.has_method("get_setting"):
		if not bool(settings_mgr.call("get_setting", "gameplay_tutorial_toasts")):
			_scan_tut_shown = true
			return

	# Phase timer countdown — don't advance until current phase display finishes.
	if _scan_tut_phase_timer > 0.0:
		_scan_tut_phase_timer -= _SLOW_POLL_INTERVAL
		return

	# ── Phase -1 → 0: Wait for sensor_suite tech unlock ──
	if _scan_tut_phase == -1:
		if not _bridge.has_method("GetTechTreeV0"):
			return
		var techs: Array = _bridge.call("GetTechTreeV0")
		var sensor_unlocked: bool = false
		for t in techs:
			if typeof(t) != TYPE_DICTIONARY:
				continue
			if str(t.get("tech_id", "")) == "sensor_suite" and bool(t.get("unlocked", false)):
				sensor_unlocked = true
				break
		if not sensor_unlocked:
			return
		# Sensor suite just became available.
		_scan_tut_phase = 0
		var msg: String = "Commander, our new sensor suite is online. I'm detecting anomalous readings nearby. Let's investigate."
		_add_scan_tutorial_dialogue(msg)
		print("DEBUG_SCAN_TUT_|PHASE_0|sensor_suite_unlocked")
		return

	# ── Phase 0 → 1: Wait until player is near a discovery site ──
	if _scan_tut_phase == 0:
		if not _bridge.has_method("GetPlayerStateV0") or not _bridge.has_method("GetDiscoverySnapshotV0"):
			return
		var ps: Dictionary = _bridge.call("GetPlayerStateV0")
		var node_id: String = str(ps.get("current_node_id", ""))
		if node_id.is_empty():
			return
		var snap = _bridge.call("GetDiscoverySnapshotV0", node_id)
		if typeof(snap) != TYPE_ARRAY or snap.is_empty():
			return
		# Player is at a node with discovery sites — close enough.
		_scan_tut_phase = 1
		# Record baseline scanned count to detect first scan completion.
		_scan_tut_discovery_count = _count_scanned_discoveries(snap)
		var msg: String = "Readings are strengthening. Hold position and initiate a full scan — look for the scan indicator on your HUD."
		_add_scan_tutorial_dialogue(msg)
		print("DEBUG_SCAN_TUT_|PHASE_1|near_discovery_site|node=" + node_id)
		return

	# ── Phase 1 → 2: Wait for first successful scan ──
	if _scan_tut_phase == 1:
		# Check via game_manager's first_scan_complete flag (most reliable).
		var gm = get_node_or_null("/root/GameManager")
		if gm and gm.has_method("is_first_scan_complete_v0"):
			if gm.call("is_first_scan_complete_v0"):
				_scan_tut_phase = 2
				var msg: String = "Excellent scan, Commander. The data reveals promising signatures. We can analyze further with deeper sensor sweeps."
				_add_scan_tutorial_dialogue(msg)
				_scan_tut_shown = true
				print("DEBUG_SCAN_TUT_|PHASE_2|first_scan_complete")
				return
		# Fallback: poll discovery snapshot for phase changes (SEEN→SCANNED).
		if _bridge.has_method("GetPlayerStateV0") and _bridge.has_method("GetDiscoverySnapshotV0"):
			var ps: Dictionary = _bridge.call("GetPlayerStateV0")
			var node_id: String = str(ps.get("current_node_id", ""))
			if not node_id.is_empty():
				var snap = _bridge.call("GetDiscoverySnapshotV0", node_id)
				if typeof(snap) == TYPE_ARRAY and not snap.is_empty():
					var current_count: int = _count_scanned_discoveries(snap)
					if _scan_tut_discovery_count >= 0 and current_count > _scan_tut_discovery_count:
						_scan_tut_phase = 2
						var kind: String = _get_first_scanned_kind(snap)
						var kind_label: String = kind if not kind.is_empty() else "promising signatures"
						var msg: String = "Excellent scan, Commander. The data reveals %s. We can analyze further with deeper sensor sweeps." % kind_label
						_add_scan_tutorial_dialogue(msg)
						_scan_tut_shown = true
						print("DEBUG_SCAN_TUT_|PHASE_2|scan_detected_via_snapshot|kind=" + kind_label)
						return


## Helper: inject a scan-tutorial FO dialogue line and flash the panel.
func _add_scan_tutorial_dialogue(text: String) -> void:
	_dialogue_history.append(text)
	if _dialogue_history.size() > _MAX_DIALOGUE_LINES:
		_dialogue_history = _dialogue_history.slice(_dialogue_history.size() - _MAX_DIALOGUE_LINES)
	_rebuild_dialogue_ui()
	_flash_panel(_SCAN_TUT_DISPLAY_SECS)
	# Also show as FO toast.
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", "FO: " + text, "fo")
	_scan_tut_phase_timer = _SCAN_TUT_DISPLAY_SECS


## Count how many discoveries in a snapshot array are in SCANNED or ANALYZED phase.
func _count_scanned_discoveries(snap: Array) -> int:
	var count: int = 0
	for d in snap:
		if typeof(d) != TYPE_DICTIONARY:
			continue
		var phase: String = str(d.get("phase", ""))
		if phase == "SCANNED" or phase == "ANALYZED":
			count += 1
	return count


## Get the kind/family of the first SCANNED discovery in the snapshot.
func _get_first_scanned_kind(snap: Array) -> String:
	for d in snap:
		if typeof(d) != TYPE_DICTIONARY:
			continue
		if str(d.get("phase", "")) == "SCANNED":
			var kind: String = str(d.get("kind", ""))
			if not kind.is_empty():
				return kind
	return ""


# ---------- Dialogue History ----------

func _poll_fo_dialogue() -> void:
	if not _bridge.has_method("GetFirstOfficerDialogueV0"):
		return
	var line: String = _bridge.call("GetFirstOfficerDialogueV0")
	if line.is_empty():
		return
	# Append to history, cap at _MAX_DIALOGUE_LINES.
	_dialogue_history.append(line)
	if _dialogue_history.size() > _MAX_DIALOGUE_LINES:
		_dialogue_history = _dialogue_history.slice(_dialogue_history.size() - _MAX_DIALOGUE_LINES)
	_rebuild_dialogue_ui()
	# Auto-show panel briefly so player sees the FO character.
	_flash_panel()
	# Show as toast with "fo" category (gold).
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", "FO: " + line, "fo")


func _rebuild_dialogue_ui() -> void:
	for child in _dialogue_vbox.get_children():
		child.queue_free()
	for i in range(_dialogue_history.size()):
		var lbl := Label.new()
		var display_text: String = _apply_comms_static(_dialogue_history[i])
		lbl.text = "\"%s\"" % display_text
		lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		lbl.add_theme_color_override("font_color", UITheme.GOLD)
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_dialogue_vbox.add_child(lbl)
	# Scroll to bottom after rebuild.
	await get_tree().process_frame
	_dialogue_scroll.scroll_vertical = int(_dialogue_scroll.get_v_scroll_bar().max_value)


# ---------- Promotion UI ----------

func _refresh_promotion() -> void:
	if not _bridge.has_method("GetFirstOfficerStateV0"):
		_promo_section.visible = false
		return
	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	var in_window: bool = fo.get("in_promotion_window", false)
	var promoted: bool = fo.get("promoted", false)
	if not in_window or promoted:
		if _promo_visible:
			_promo_section.visible = false
			_promo_visible = false
			_clear_children(_promo_section)
		return
	# Show promotion candidates.
	if not _bridge.has_method("GetFirstOfficerCandidatesV0"):
		return
	var candidates: Array = _bridge.call("GetFirstOfficerCandidatesV0")
	if candidates.is_empty():
		_promo_section.visible = false
		return
	# Only rebuild if not already showing (avoid flicker on every poll).
	if _promo_visible:
		return
	_promo_visible = true
	_clear_children(_promo_section)
	_promo_section.visible = true

	var promo_title := Label.new()
	promo_title.text = "-- Promote First Officer --"
	promo_title.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	promo_title.add_theme_color_override("font_color", UITheme.GOLD)
	promo_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_promo_section.add_child(promo_title)

	for candidate in candidates:
		var card := _build_candidate_card(candidate)
		_promo_section.add_child(card)


func _build_candidate_card(candidate: Dictionary) -> VBoxContainer:
	var card := VBoxContainer.new()
	card.add_theme_constant_override("separation", UITheme.SPACE_XS)

	var cand_name: String = str(candidate.get("name", "Unknown"))
	var cand_type: String = str(candidate.get("type", ""))
	var cand_desc: String = str(candidate.get("description", ""))

	var name_lbl := Label.new()
	name_lbl.text = cand_name
	name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	card.add_child(name_lbl)

	if not cand_desc.is_empty():
		var desc_lbl := Label.new()
		desc_lbl.text = cand_desc
		desc_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		desc_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		card.add_child(desc_lbl)

	var btn := Button.new()
	btn.text = "Promote"
	btn.custom_minimum_size = Vector2(80, 24)
	btn.pressed.connect(_on_promote_pressed.bind(cand_type))
	card.add_child(btn)

	return card


func _on_promote_pressed(candidate_type: String) -> void:
	if _bridge == null:
		return
	if not _bridge.has_method("PromoteFirstOfficerV0"):
		return
	var success: bool = _bridge.call("PromoteFirstOfficerV0", candidate_type)
	if success:
		# Force immediate refresh to update status and hide promo section.
		_promo_visible = false
		_clear_children(_promo_section)
		_promo_section.visible = false
		_refresh_fo_state()
		# Auto-show panel so player sees the promoted FO.
		_flash_panel(8.0)
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "First Officer promoted!", "fo")


# ---------- GATE.T58.UI.FO_SERVICE.001: Service Record ----------

func _refresh_service_record() -> void:
	if not _bridge.has_method("GetServiceRecordV0"):
		_set_service_visible(false)
		return
	var sr: Dictionary = _bridge.call("GetServiceRecordV0")
	if sr.is_empty():
		_set_service_visible(false)
		return

	var routes: int = sr.get("routes_managed", 0)
	var rec_taken: int = sr.get("recommendations_taken", 0)
	var rec_offered: int = sr.get("recommendations_offered", 0)
	var crises: int = sr.get("crises_handled", 0)

	# Only show when there's data to display.
	if routes == 0 and rec_offered == 0 and crises == 0:
		_set_service_visible(false)
		return

	_set_service_visible(true)

	# Rebuild if not already showing (avoid flicker).
	if _service_visible:
		return
	_service_visible = true
	_clear_children(_service_section)

	# Routes managed.
	var routes_lbl := Label.new()
	routes_lbl.text = "Routes managed: %d" % routes
	routes_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	routes_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	_service_section.add_child(routes_lbl)

	# Success rate.
	if rec_offered > 0:
		var rate: float = float(rec_taken) / float(rec_offered) * 100.0
		var rate_lbl := Label.new()
		rate_lbl.text = "Advice followed: %d%% (%d/%d)" % [int(rate), rec_taken, rec_offered]
		rate_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		rate_lbl.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		_service_section.add_child(rate_lbl)

	# Crises handled.
	if crises > 0:
		var crises_lbl := Label.new()
		crises_lbl.text = "Crises handled: %d" % crises
		crises_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		crises_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
		_service_section.add_child(crises_lbl)

	# Worst call — transparency text.
	var worst_desc: String = str(sr.get("worst_call_description", ""))
	if not worst_desc.is_empty():
		var worst_lbl := Label.new()
		var worst_cost: int = sr.get("worst_call_cost", 0)
		worst_lbl.text = "Worst call: %s (%d cr)" % [worst_desc, worst_cost]
		worst_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		worst_lbl.add_theme_color_override("font_color", UITheme.RED)
		worst_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_service_section.add_child(worst_lbl)

	# Notable moment.
	var notable: String = str(sr.get("notable_description", ""))
	if not notable.is_empty():
		var notable_lbl := Label.new()
		notable_lbl.text = "Notable: %s" % notable
		notable_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		notable_lbl.add_theme_color_override("font_color", UITheme.GOLD)
		notable_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_service_section.add_child(notable_lbl)


func _set_service_visible(vis: bool) -> void:
	if _service_sep: _service_sep.visible = vis
	if _service_title: _service_title.visible = vis
	if _service_section: _service_section.visible = vis
	if not vis:
		_service_visible = false


# ---------- NPC Section (Known Contacts — all NPCs including dead) ----------

func _refresh_npcs() -> void:
	if not _bridge.has_method("GetAllNarrativeNpcsV0"):
		_clear_children(_npc_section)
		_set_npc_section_visible(false)
		return
	var npcs: Array = _bridge.call("GetAllNarrativeNpcsV0")
	_clear_children(_npc_section)
	if npcs.is_empty():
		_set_npc_section_visible(false)
		return
	_set_npc_section_visible(true)
	for npc in npcs:
		var npc_lbl := Label.new()
		var npc_name: String = str(npc.get("name", "Unknown"))
		var npc_kind: String = str(npc.get("kind", ""))
		var is_alive: bool = npc.get("is_alive", true)
		var vanish_reason: String = str(npc.get("vanish_reason", ""))
		var faction: String = str(npc.get("faction", ""))

		var display_parts: Array[String] = []
		if not is_alive:
			display_parts.append("[Lost]")
		display_parts.append(npc_name)
		if not npc_kind.is_empty():
			display_parts.append("(%s)" % npc_kind)
		if not faction.is_empty():
			display_parts.append("- %s" % faction)

		var text: String = " ".join(display_parts)
		if not is_alive and not vanish_reason.is_empty():
			text += " [%s]" % vanish_reason

		npc_lbl.text = text
		npc_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		if is_alive:
			npc_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		else:
			npc_lbl.add_theme_color_override("font_color", UITheme.RED)
		npc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_npc_section.add_child(npc_lbl)


# ---------- Helpers ----------

func _set_npc_section_visible(vis: bool) -> void:
	if _npc_sep:
		_npc_sep.visible = vis
	if _npc_title:
		_npc_title.visible = vis
	if _npc_section:
		_npc_section.visible = vis


## Flash the panel visible for a duration, then auto-hide.
## If the player manually toggled the panel (F key), auto-hide is skipped.
func _flash_panel(duration: float = _AUTO_SHOW_DURATION) -> void:
	if visible:
		# Already visible (player toggled) — extend auto-hide or skip.
		if _auto_show_timer > 0.0:
			_auto_show_timer = duration  # Reset timer
		return
	visible = true
	_auto_show_timer = duration


# GATE.T45.DEEP_DREAD.COMMS_STATIC.001: Poll hop distance for comms degradation.
func _poll_comms_hops() -> void:
	if not _bridge.has_method("GetDreadStateV0"):
		_comms_hops = 0
		return
	var dread: Dictionary = _bridge.call("GetDreadStateV0")
	_comms_hops = dread.get("hops_from_capital", 0)


# GATE.T45.DEEP_DREAD.COMMS_STATIC.001: Apply static corruption to text.
# At hop>=4: 5-10% char corruption. At hop>=6: 10-15% corruption.
func _apply_comms_static(text: String) -> String:
	if _comms_hops < 4:
		return text
	var corruption_rate: float = 0.05 + (_comms_hops - 4) * 0.025
	corruption_rate = clampf(corruption_rate, 0.0, 0.20)
	var result: String = ""
	for i in text.length():
		if text[i] == " " or text[i] == "\n":
			result += text[i]
		elif randf() < corruption_rate:
			result += _STATIC_GLYPHS[randi() % _STATIC_GLYPHS.length()]
		else:
			result += text[i]
	return result


func _clear_children(container: Control) -> void:
	for child in container.get_children():
		child.queue_free()
