# scripts/ui/fo_panel.gd
# GATE.T18.CHARACTER.UI_FULL.001: First Officer panel overhaul — dialogue history,
# promotion UI, War Faces NPC display (including dead), gold FO toasts.
extends PanelContainer

var _bridge = null
var _name_label: Label = null
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


func _ready() -> void:
	name = "FOPanel"
	visible = false
	custom_minimum_size = Vector2(280, 0)
	position = Vector2(10, 470)
	var style := UITheme.make_panel_hud()
	add_theme_stylebox_override("panel", style)
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

	# --- FO identity ---
	_name_label = Label.new()
	_name_label.text = ""
	_name_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_name_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	vbox.add_child(_name_label)

	_status_label = Label.new()
	_status_label.text = ""
	_status_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_status_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	vbox.add_child(_status_label)

	# --- Dialogue history (scrollable, last 5 lines) ---
	var dialogue_sep := HSeparator.new()
	vbox.add_child(dialogue_sep)

	var dialogue_title := Label.new()
	dialogue_title.text = "Dialogue"
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
	_slow_poll_elapsed += delta
	if _slow_poll_elapsed < _SLOW_POLL_INTERVAL:
		return
	_slow_poll_elapsed = 0.0

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null:
		return

	_refresh_fo_state()
	_poll_fo_dialogue()
	_refresh_promotion()
	_refresh_npcs()


# ---------- FO State ----------

func _refresh_fo_state() -> void:
	if not _bridge.has_method("GetFirstOfficerStateV0"):
		return
	var fo: Dictionary = _bridge.call("GetFirstOfficerStateV0")
	if fo.is_empty():
		visible = false
		return
	# FEEL_POST_FIX_2: Never self-show — FO panel is always suppressed until an
	# F-key toggle is implemented. Without this, _refresh_fo_state overrides the
	# hud.gd suppression every 2 seconds and the panel bleeds into galaxy map/overlays.
	var fo_name: String = str(fo.get("name", "None"))
	var promoted: bool = fo.get("promoted", false)
	_name_label.text = fo_name
	if promoted:
		_status_label.text = "Promoted"
		_status_label.add_theme_color_override("font_color", UITheme.GREEN)
	elif fo.get("in_promotion_window", false):
		_status_label.text = "Promotion available"
		_status_label.add_theme_color_override("font_color", UITheme.GOLD)
	else:
		_status_label.text = ""


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
	# Show as toast with "fo" category (gold).
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", "FO: " + line, "fo")


func _rebuild_dialogue_ui() -> void:
	for child in _dialogue_vbox.get_children():
		child.queue_free()
	for i in range(_dialogue_history.size()):
		var lbl := Label.new()
		lbl.text = "\"%s\"" % _dialogue_history[i]
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
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "First Officer promoted!", "fo")


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


func _clear_children(container: Control) -> void:
	for child in container.get_children():
		child.queue_free()
