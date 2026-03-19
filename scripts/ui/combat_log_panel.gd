# GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001: Combat log panel (L key toggle).
# CanvasLayer showing last 20 combat events from SimBridge.
extends CanvasLayer

var _panel: PanelContainer = null
var _scroll: ScrollContainer = null
var _vbox: VBoxContainer = null
var _title_label: Label = null
var _bridge: Node = null

var _refresh_counter: int = 0

func _ready() -> void:
	layer = 115
	visible = false
	_build_ui()


func _process(_delta: float) -> void:
	if not visible:
		return
	_refresh_counter += 1
	if _refresh_counter >= 30:  # Refresh every ~0.5s when visible
		_refresh_counter = 0
		refresh_v0()


func _build_ui() -> void:
	_panel = PanelContainer.new()
	_panel.name = "CombatLogPanelContainer"
	_panel.custom_minimum_size = Vector2(460, 360)

	# Anchor to right side of screen
	_panel.anchor_left = 1.0
	_panel.anchor_right = 1.0
	_panel.anchor_top = 0.0
	_panel.anchor_bottom = 0.0
	_panel.offset_left = -480
	_panel.offset_right = -16
	_panel.offset_top = 60
	_panel.offset_bottom = 420

	_panel.add_theme_stylebox_override("panel", UITheme.make_panel_ship_computer(UITheme.BORDER_DANGER))
	UITheme.add_corner_brackets(_panel, UITheme.BORDER_DANGER)
	UITheme.add_scanline_overlay(_panel)

	var outer_vbox := VBoxContainer.new()
	outer_vbox.add_theme_constant_override("separation", 4)

	# Title
	_title_label = Label.new()
	_title_label.text = "COMBAT LOG"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_color_override("font_color", UITheme.RED_LIGHT)
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	outer_vbox.add_child(_title_label)

	outer_vbox.add_child(HSeparator.new())

	_scroll = ScrollContainer.new()
	_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll.custom_minimum_size = Vector2(0, 280)

	_vbox = VBoxContainer.new()
	_vbox.name = "EventRows"
	_vbox.add_theme_constant_override("separation", 2)
	_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.add_child(_vbox)

	outer_vbox.add_child(_scroll)
	UITheme.add_scroll_fade(_scroll)

	# Footer
	var footer := Label.new()
	footer.text = "Press L to close"
	footer.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	footer.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	footer.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	outer_vbox.add_child(footer)

	_panel.add_child(outer_vbox)
	add_child(_panel)


func toggle_v0() -> void:
	if visible:
		UITheme.animate_close(_panel, func(): visible = false)
	else:
		visible = true
		UITheme.animate_open(_panel)
		refresh_v0()


func refresh_v0() -> void:
	if _vbox == null:
		return

	# Clear old rows
	for child in _vbox.get_children():
		_vbox.remove_child(child)
		child.queue_free()

	if _bridge == null:
		_bridge = get_node_or_null("/root/SimBridge")
	if _bridge == null or not _bridge.has_method("GetRecentCombatEventsV0"):
		var no_data := Label.new()
		no_data.text = "No combat data available"
		no_data.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_vbox.add_child(no_data)
		return

	var events: Array = _bridge.call("GetRecentCombatEventsV0")

	if events.size() == 0:
		_vbox.add_child(UITheme.make_empty_state("⬡", "No combat events recorded", "Events will appear when combat occurs"))
		_title_label.text = "COMBAT LOG"
		return

	# FEEL_PASS4_P2: Aggregate same-tick same-combatant events into single rows.
	# Group events by (tick, attacker, defender) — show hit count + total damage.
	var grouped: Array = _aggregate_events(events)
	_title_label.text = "COMBAT LOG (%d)" % grouped.size()

	# Running totals for footer.
	var total_dealt: int = 0
	var total_taken: int = 0

	# Show events newest-first.
	for i in range(grouped.size() - 1, -1, -1):
		var grp: Dictionary = grouped[i]
		var tick: int = int(grp.get("tick", 0))
		var attacker: String = str(grp.get("attacker_id", "?"))
		var defender: String = str(grp.get("defender_id", "?"))
		var total_dmg: int = int(grp.get("total_damage", 0))
		var hit_count: int = int(grp.get("hit_count", 1))
		var outcome: String = str(grp.get("outcome", ""))
		var is_player_shot: bool = attacker == "fleet_trader_1"

		if is_player_shot:
			total_dealt += total_dmg
		else:
			total_taken += total_dmg

		# Structured row: [tick] [dir] [combatants] [hits] [damage] [outcome]
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 6)

		# Tick number (monospace, muted)
		var tick_lbl := Label.new()
		tick_lbl.text = "%03d" % tick
		tick_lbl.custom_minimum_size = Vector2(36, 0)
		tick_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		tick_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		UITheme.apply_mono(tick_lbl)
		row.add_child(tick_lbl)

		# Direction indicator
		var dir_lbl := Label.new()
		dir_lbl.text = "▸" if is_player_shot else "◂"
		dir_lbl.custom_minimum_size = Vector2(14, 0)
		dir_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		dir_lbl.add_theme_color_override("font_color", UITheme.GOLD if is_player_shot else UITheme.RED_LIGHT)
		row.add_child(dir_lbl)

		# Combatants (with hit count if > 1)
		var names_lbl := Label.new()
		var names_text: String = "%s → %s" % [_display_name(attacker), _display_name(defender)]
		if hit_count > 1:
			names_text += "  ×%d" % hit_count
		names_lbl.text = names_text
		names_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		names_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		names_lbl.add_theme_color_override("font_color", UITheme.GOLD if is_player_shot else UITheme.RED_LIGHT)
		row.add_child(names_lbl)

		# Total damage (monospace, right-aligned)
		var dmg_lbl := Label.new()
		dmg_lbl.text = "%d" % total_dmg
		dmg_lbl.custom_minimum_size = Vector2(40, 0)
		dmg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		dmg_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		dmg_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
		UITheme.apply_mono(dmg_lbl)
		row.add_child(dmg_lbl)

		# Outcome badge (only if resolved)
		if not outcome.is_empty() and outcome != "InProgress":
			var out_lbl := Label.new()
			out_lbl.text = _humanize_outcome(outcome)
			out_lbl.custom_minimum_size = Vector2(70, 0)
			out_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			out_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
			var out_color := UITheme.GOLD if outcome in ["Win", "Victory"] else UITheme.RED_LIGHT
			out_lbl.add_theme_color_override("font_color", out_color)
			row.add_child(out_lbl)

		_vbox.add_child(row)

	# Summary footer row.
	if total_dealt > 0 or total_taken > 0:
		_vbox.add_child(HSeparator.new())
		var summary := Label.new()
		summary.text = "Dealt: %d dmg  ·  Taken: %d dmg" % [total_dealt, total_taken]
		summary.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		summary.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		summary.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
		_vbox.add_child(summary)


# FEEL_PASS4_P2: Aggregate same-tick same-combatant events into single rows.
func _aggregate_events(events: Array) -> Array:
	var result: Array = []
	for evt in events:
		var tick: int = int(evt.get("tick", 0))
		var attacker: String = str(evt.get("attacker_id", "?"))
		var defender: String = str(evt.get("defender_id", "?"))
		var damage: int = int(evt.get("damage", 0))
		var outcome: String = str(evt.get("outcome", ""))
		# Try to merge with last group if same tick + same combatants.
		if result.size() > 0:
			var last: Dictionary = result[result.size() - 1]
			if int(last.get("tick", -1)) == tick \
				and str(last.get("attacker_id", "")) == attacker \
				and str(last.get("defender_id", "")) == defender:
				last["total_damage"] = int(last.get("total_damage", 0)) + damage
				last["hit_count"] = int(last.get("hit_count", 1)) + 1
				if not outcome.is_empty() and outcome != "InProgress":
					last["outcome"] = outcome
				continue
		result.append({
			"tick": tick,
			"attacker_id": attacker,
			"defender_id": defender,
			"total_damage": damage,
			"hit_count": 1,
			"outcome": outcome,
		})
	return result


# Resolve internal fleet IDs to human-readable names.
func _display_name(fleet_id: String) -> String:
	if fleet_id == "fleet_trader_1":
		return "Your Fleet"
	if fleet_id.begins_with("ai_fleet_") or fleet_id.begins_with("at_fleet_"):
		if fleet_id.contains("patrol"):
			return "Raider Patrol"
		return "Raider"
	if fleet_id.begins_with("fleet_patrol"):
		return "Sector Patrol"
	if fleet_id.begins_with("fleet_hauler"):
		return "Hauler"
	if fleet_id.begins_with("fleet_trader"):
		return "Trader"
	if fleet_id.begins_with("fleet_"):
		return fleet_id.substr(6).replace("_", " ").capitalize()
	return fleet_id


# Humanize C# enum outcome strings for player display.
func _humanize_outcome(raw: String) -> String:
	match raw:
		"Win": return "Destroyed"
		"Loss": return "Defeated"
		"Draw": return "Disengaged"
		"Victory": return "Destroyed"
		"Defeat": return "Defeated"
		"Flee": return "Fled"
		_: return raw
