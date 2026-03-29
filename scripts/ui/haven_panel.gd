# scripts/ui/haven_panel.gd
# GATE.S8.HAVEN.DOCK_PANEL.001: Haven starbase dock panel
# Shows Haven status, upgrade controls, hangar ship list, and exotic market.
extends VBoxContainer

var bridge: Node = null
var _market_node_id: String = ""

# --- Status section ---
var _tier_label: Label = null
var _upgrade_label: Label = null
var _upgrade_button: Button = null
var _fragments_label: Label = null
var _thread_label: Label = null

# --- Hangar section ---
var _hangar_header: Label = null
var _hangar_list: VBoxContainer = null
var _hangar_empty_label: Label = null

# --- Market section ---
var _market_header: Label = null
var _market_list: VBoxContainer = null
var _market_empty_label: Label = null

# --- Residents section (GATE.S8.HAVEN.RESIDENTS_BRIDGE.001) ---
var _residents_list: VBoxContainer = null

# --- Trophy Wall section (GATE.S8.HAVEN.TROPHY_BRIDGE.001) ---
var _trophy_list: VBoxContainer = null

# --- Endgame Progress section (GATE.S8.WIN.PROGRESS_UI.001) ---
var _endgame_section: VBoxContainer = null

# --- Keeper section (GATE.S8.HAVEN.DEPTH_BRIDGE.001) ---
var _keeper_section: VBoxContainer = null

# --- Resonance Chamber section (GATE.S8.HAVEN.DEPTH_BRIDGE.001) ---
var _resonance_section: VBoxContainer = null

# --- Fabricator section (GATE.S8.HAVEN.DEPTH_BRIDGE.001) ---
var _fabricator_section: VBoxContainer = null

# --- Endgame Path Choice section (GATE.S8.HAVEN.ENDGAME_BRIDGE.001) ---
var _path_choice_section: VBoxContainer = null

# --- Accommodation section (GATE.S8.HAVEN.ENDGAME_BRIDGE.001) ---
var _accommodation_section: VBoxContainer = null

# --- Communion Rep section (GATE.S8.HAVEN.ENDGAME_BRIDGE.001) ---
var _communion_section: VBoxContainer = null

# --- Ancient Hulls section (GATE.S8.ANCIENT_HULLS.BRIDGE.001) ---
var _hulls_list: VBoxContainer = null

func _ready():
	bridge = get_node_or_null("/root/SimBridge")
	add_theme_constant_override("separation", 6)

	# --- Haven Status ---
	var status_header = Label.new()
	status_header.text = "HAVEN STATUS"
	status_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	status_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	status_header.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	add_child(status_header)

	_tier_label = Label.new()
	_tier_label.text = "Tier: --"
	_tier_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_tier_label.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
	add_child(_tier_label)

	_upgrade_label = Label.new()
	_upgrade_label.text = ""
	_upgrade_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_upgrade_label.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	_upgrade_label.visible = false
	add_child(_upgrade_label)

	_fragments_label = Label.new()
	_fragments_label.text = ""
	_fragments_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_fragments_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	add_child(_fragments_label)

	_thread_label = Label.new()
	_thread_label.text = ""
	_thread_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_thread_label.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_thread_label.visible = false
	add_child(_thread_label)

	_upgrade_button = Button.new()
	_upgrade_button.text = "Upgrade Haven"
	_upgrade_button.pressed.connect(_on_upgrade_pressed)
	add_child(_upgrade_button)

	add_child(HSeparator.new())

	# --- Hangar Section ---
	_hangar_header = Label.new()
	_hangar_header.text = "HANGAR"
	_hangar_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hangar_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_hangar_header.add_theme_color_override("font_color", UITheme.CYAN)
	add_child(_hangar_header)

	_hangar_list = VBoxContainer.new()
	_hangar_list.add_theme_constant_override("separation", 4)
	add_child(_hangar_list)

	_hangar_empty_label = Label.new()
	_hangar_empty_label.text = "No ships stored"
	_hangar_empty_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_hangar_empty_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_hangar_list.add_child(_hangar_empty_label)

	add_child(HSeparator.new())

	# --- Market Section ---
	_market_header = Label.new()
	_market_header.text = "EXOTIC MARKET"
	_market_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_market_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_market_header.add_theme_color_override("font_color", UITheme.GOLD)
	add_child(_market_header)

	# Column headers
	var mkt_cols = HBoxContainer.new()
	for col_name in ["Good", "Stock", "Buy", "Sell"]:
		var lbl = Label.new()
		lbl.text = col_name
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		mkt_cols.add_child(lbl)
	add_child(mkt_cols)

	_market_list = VBoxContainer.new()
	_market_list.add_theme_constant_override("separation", 2)
	add_child(_market_list)

	_market_empty_label = Label.new()
	_market_empty_label.text = "No goods available"
	_market_empty_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_market_empty_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_market_list.add_child(_market_empty_label)

	add_child(HSeparator.new())

	# --- Residents Section (GATE.S8.HAVEN.RESIDENTS_BRIDGE.001) ---
	var res_header = Label.new()
	res_header.text = "RESIDENTS"
	res_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	res_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	res_header.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	add_child(res_header)

	_residents_list = VBoxContainer.new()
	_residents_list.add_theme_constant_override("separation", 4)
	add_child(_residents_list)

	add_child(HSeparator.new())

	# --- Trophy Wall Section (GATE.S8.HAVEN.TROPHY_BRIDGE.001) ---
	var trophy_header = Label.new()
	trophy_header.text = "TROPHY WALL"
	trophy_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	trophy_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	trophy_header.add_theme_color_override("font_color", UITheme.GOLD)
	add_child(trophy_header)

	_trophy_list = VBoxContainer.new()
	_trophy_list.add_theme_constant_override("separation", 2)
	add_child(_trophy_list)

	add_child(HSeparator.new())

	# --- Endgame Progress Section (GATE.S8.WIN.PROGRESS_UI.001) ---
	var endgame_header = Label.new()
	endgame_header.text = "ENDGAME PROGRESS"
	endgame_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	endgame_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	endgame_header.add_theme_color_override("font_color", Color(0.9, 0.8, 0.3))
	add_child(endgame_header)

	_endgame_section = VBoxContainer.new()
	_endgame_section.add_theme_constant_override("separation", 4)
	_endgame_section.visible = false
	add_child(_endgame_section)

	add_child(HSeparator.new())

	# --- Keeper Section (GATE.S8.HAVEN.DEPTH_BRIDGE.001) ---
	var keeper_header = Label.new()
	keeper_header.text = "KEEPER"
	keeper_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	keeper_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	keeper_header.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	add_child(keeper_header)

	_keeper_section = VBoxContainer.new()
	_keeper_section.add_theme_constant_override("separation", 2)
	add_child(_keeper_section)

	add_child(HSeparator.new())

	# --- Resonance Chamber Section (GATE.S8.HAVEN.DEPTH_BRIDGE.001) ---
	var resonance_header = Label.new()
	resonance_header.text = "RESONANCE CHAMBER"
	resonance_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	resonance_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	resonance_header.add_theme_color_override("font_color", UITheme.CYAN)
	add_child(resonance_header)

	_resonance_section = VBoxContainer.new()
	_resonance_section.add_theme_constant_override("separation", 2)
	add_child(_resonance_section)

	add_child(HSeparator.new())

	# --- Fabricator Section (GATE.S8.HAVEN.DEPTH_BRIDGE.001) ---
	var fab_header = Label.new()
	fab_header.text = "FABRICATOR"
	fab_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	fab_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	fab_header.add_theme_color_override("font_color", UITheme.ORANGE)
	add_child(fab_header)

	_fabricator_section = VBoxContainer.new()
	_fabricator_section.add_theme_constant_override("separation", 2)
	add_child(_fabricator_section)

	add_child(HSeparator.new())

	# --- Endgame Path Choice Section (GATE.S8.HAVEN.ENDGAME_BRIDGE.001) ---
	var path_header = Label.new()
	path_header.text = "ENDGAME PATH"
	path_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	path_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	path_header.add_theme_color_override("font_color", Color(0.9, 0.8, 0.3))
	add_child(path_header)

	_path_choice_section = VBoxContainer.new()
	_path_choice_section.add_theme_constant_override("separation", 4)
	add_child(_path_choice_section)

	add_child(HSeparator.new())

	# --- Accommodation Section (GATE.S8.HAVEN.ENDGAME_BRIDGE.001) ---
	var accom_header = Label.new()
	accom_header.text = "ACCOMMODATION THREADS"
	accom_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	accom_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	accom_header.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT)
	add_child(accom_header)

	_accommodation_section = VBoxContainer.new()
	_accommodation_section.add_theme_constant_override("separation", 4)
	add_child(_accommodation_section)

	add_child(HSeparator.new())

	# --- Communion Rep Section (GATE.S8.HAVEN.ENDGAME_BRIDGE.001) ---
	var comm_header = Label.new()
	comm_header.text = "COMMUNION REPRESENTATIVE"
	comm_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	comm_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	comm_header.add_theme_color_override("font_color", Color(0.5, 0.9, 1.0))
	add_child(comm_header)

	_communion_section = VBoxContainer.new()
	_communion_section.add_theme_constant_override("separation", 2)
	add_child(_communion_section)

	add_child(HSeparator.new())

	# --- Ancient Hulls Section (GATE.S8.ANCIENT_HULLS.BRIDGE.001) ---
	var hulls_header = Label.new()
	hulls_header.text = "HULL RESTORATION"
	hulls_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hulls_header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	hulls_header.add_theme_color_override("font_color", UITheme.CYAN)
	add_child(hulls_header)

	_hulls_list = VBoxContainer.new()
	_hulls_list.add_theme_constant_override("separation", 4)
	add_child(_hulls_list)


func refresh(node_id: String = "") -> void:
	_market_node_id = node_id
	if bridge == null:
		bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		visible = false
		return

	var status: Dictionary = {}
	if bridge.has_method("GetHavenStatusV0"):
		status = bridge.call("GetHavenStatusV0")

	if status.is_empty() or not status.get("discovered", false):
		visible = false
		return

	visible = true
	_update_status(status)
	_update_hangar(status)
	_update_market()
	_update_residents()
	_update_trophy_wall()
	_update_endgame_progress(status)
	_update_keeper()
	_update_resonance_chamber()
	_update_fabricator()
	_update_path_choice()
	_update_accommodation()
	_update_communion_rep()
	_update_hulls()


func _update_status(status: Dictionary) -> void:
	var tier: int = int(status.get("tier", 0))
	var tier_name: String = str(status.get("tier_name", "Unknown"))
	var upgrade_remaining: int = int(status.get("upgrade_ticks_remaining", 0))
	var upgrade_target: int = int(status.get("upgrade_target_tier", 0))
	var fragment_count: int = int(status.get("installed_fragment_count", 0))
	var bidir: bool = bool(status.get("bidirectional_thread", false))

	if _tier_label:
		_tier_label.text = "Tier %d — %s" % [tier, tier_name]

	if _fragments_label:
		_fragments_label.text = "Installed Fragments: %d" % fragment_count

	if _thread_label:
		_thread_label.text = "Bidirectional Thread: Active" if bidir else "Bidirectional Thread: Inactive"
		_thread_label.visible = true
		if bidir:
			_thread_label.add_theme_color_override("font_color", UITheme.GREEN)
		else:
			_thread_label.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)

	# Upgrade progress / button
	if upgrade_remaining > 0:
		if _upgrade_label:
			_upgrade_label.text = "Upgrading to Tier %d — %d ticks remaining" % [upgrade_target, upgrade_remaining]
			_upgrade_label.visible = true
		if _upgrade_button:
			_upgrade_button.disabled = true
			_upgrade_button.text = "Upgrading..."
	else:
		if _upgrade_label:
			_upgrade_label.visible = false
		if _upgrade_button:
			# Tier 5 (Awakened) is max — no more upgrades
			if tier >= 5:
				_upgrade_button.disabled = true
				_upgrade_button.text = "Max Tier"
			else:
				_upgrade_button.disabled = false
				_upgrade_button.text = "Upgrade to Tier %d" % (tier + 1)


func _update_hangar(status: Dictionary) -> void:
	if _hangar_list == null:
		return

	# Clear old rows (keep empty label reference — we re-add it if needed)
	for child in _hangar_list.get_children():
		_hangar_list.remove_child(child)
		child.queue_free()

	var stored_ids: Array = status.get("stored_ship_ids", [])
	var max_bays: int = int(status.get("max_bays", 0))

	if stored_ids.size() == 0:
		var empty_lbl = Label.new()
		empty_lbl.text = "No ships stored (%d bay%s available)" % [max_bays, "s" if max_bays != 1 else ""]
		empty_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_hangar_list.add_child(empty_lbl)
		return

	# Bay usage header
	var usage_lbl = Label.new()
	usage_lbl.text = "Ships: %d / %d bays" % [stored_ids.size(), max_bays]
	usage_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	usage_lbl.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	_hangar_list.add_child(usage_lbl)

	for ship_id in stored_ids:
		var row = HBoxContainer.new()
		row.add_theme_constant_override("separation", 8)

		var name_lbl = Label.new()
		name_lbl.text = _format_ship_name(str(ship_id))
		name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		row.add_child(name_lbl)

		var swap_btn = Button.new()
		swap_btn.text = "Swap"
		swap_btn.custom_minimum_size.x = 60
		var sid: String = str(ship_id)
		swap_btn.pressed.connect(_on_swap_pressed.bind(sid))
		row.add_child(swap_btn)

		_hangar_list.add_child(row)


func _update_market() -> void:
	if _market_list == null:
		return

	# Clear old rows
	for child in _market_list.get_children():
		_market_list.remove_child(child)
		child.queue_free()

	if bridge == null or not bridge.has_method("GetHavenMarketV0"):
		var empty_lbl = Label.new()
		empty_lbl.text = "No goods available"
		empty_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_market_list.add_child(empty_lbl)
		return

	var market: Array = bridge.call("GetHavenMarketV0")
	if market.size() == 0:
		var empty_lbl = Label.new()
		empty_lbl.text = "No goods available"
		empty_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_market_list.add_child(empty_lbl)
		return

	for entry in market:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var good_id: String = str(entry.get("good_id", ""))
		var stock: int = int(entry.get("stock", 0))
		var buy_price: int = int(entry.get("buy_price", 0))
		var sell_price: int = int(entry.get("sell_price", 0))

		var row = HBoxContainer.new()
		row.add_theme_constant_override("separation", 4)

		# Good name
		var name_lbl = Label.new()
		name_lbl.text = _format_good_name(good_id)
		name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		row.add_child(name_lbl)

		# Stock
		var stock_lbl = Label.new()
		stock_lbl.text = str(stock)
		stock_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		stock_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		stock_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		row.add_child(stock_lbl)

		# Buy price
		var buy_lbl = Label.new()
		buy_lbl.text = "%dcr" % buy_price if buy_price > 0 else "--"
		buy_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		buy_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		buy_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
		row.add_child(buy_lbl)

		# Sell price
		var sell_lbl = Label.new()
		sell_lbl.text = "%dcr" % sell_price if sell_price > 0 else "--"
		sell_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		sell_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		sell_lbl.add_theme_color_override("font_color", UITheme.GREEN)
		row.add_child(sell_lbl)

		_market_list.add_child(row)


func _on_upgrade_pressed() -> void:
	if bridge == null:
		return
	if bridge.has_method("UpgradeHavenV0"):
		bridge.call("UpgradeHavenV0")
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "Haven upgrade initiated", "system", 2.0)
		refresh(_market_node_id)


func _on_swap_pressed(stored_fleet_id: String) -> void:
	if bridge == null:
		return
	# Get the player's active fleet ID from the roster
	var active_fleet_id: String = ""
	if bridge.has_method("GetFleetRosterV0"):
		var roster: Array = bridge.call("GetFleetRosterV0")
		for entry in roster:
			if typeof(entry) == TYPE_DICTIONARY:
				var fid: String = str(entry.get("fleet_id", ""))
				var is_player: bool = bool(entry.get("is_player", false))
				if is_player and not fid.is_empty():
					active_fleet_id = fid
					break

	if active_fleet_id.is_empty():
		return

	if bridge.has_method("SwapShipV0"):
		var ok: bool = bridge.call("SwapShipV0", active_fleet_id, stored_fleet_id)
		if ok:
			var toast_mgr = get_node_or_null("/root/ToastManager")
			if toast_mgr and toast_mgr.has_method("show_priority_toast"):
				toast_mgr.call("show_priority_toast", "Ship swapped!", "system", 2.0)
		refresh(_market_node_id)


func _format_ship_name(ship_id: String) -> String:
	# Try to get a display name from the bridge; fall back to ID
	if bridge and bridge.has_method("FormatDisplayNameV0"):
		return str(bridge.call("FormatDisplayNameV0", ship_id))
	return ship_id.replace("_", " ").capitalize()


func _format_good_name(good_id: String) -> String:
	if bridge and bridge.has_method("FormatDisplayNameV0"):
		return str(bridge.call("FormatDisplayNameV0", good_id))
	return good_id.replace("_", " ").capitalize()


# GATE.S8.HAVEN.RESIDENTS_BRIDGE.001: Residents list
func _update_residents() -> void:
	if _residents_list == null:
		return
	for child in _residents_list.get_children():
		_residents_list.remove_child(child)
		child.queue_free()

	if bridge == null or not bridge.has_method("GetHavenResidentsV0"):
		return

	var residents: Array = bridge.call("GetHavenResidentsV0")
	if residents.size() == 0:
		var empty_lbl = Label.new()
		empty_lbl.text = "No residents yet"
		empty_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_residents_list.add_child(empty_lbl)
		return

	for entry in residents:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var rname: String = str(entry.get("name", ""))
		var role: String = str(entry.get("role", ""))
		var hint: String = str(entry.get("dialogue_hint", ""))

		var row = HBoxContainer.new()
		row.add_theme_constant_override("separation", 8)

		var name_lbl = Label.new()
		name_lbl.text = "%s (%s)" % [rname, role]
		name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY)
		name_lbl.tooltip_text = hint
		row.add_child(name_lbl)

		_residents_list.add_child(row)


# GATE.S8.HAVEN.TROPHY_BRIDGE.001: Trophy Wall display
func _update_trophy_wall() -> void:
	if _trophy_list == null:
		return
	for child in _trophy_list.get_children():
		_trophy_list.remove_child(child)
		child.queue_free()

	if bridge == null or not bridge.has_method("GetTrophyWallV0"):
		return

	var entries: Array = bridge.call("GetTrophyWallV0")
	if entries.size() == 0:
		return

	# Kind colors
	var kind_colors: Dictionary = {
		"Biological": UITheme.GREEN,
		"Structural": UITheme.CYAN,
		"Energetic": UITheme.ORANGE,
		"Cognitive": UITheme.PURPLE_LIGHT,
	}

	for entry in entries:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var fname: String = str(entry.get("name", ""))
		var kind: String = str(entry.get("kind", ""))
		var collected: bool = bool(entry.get("collected", false))
		var deposited: bool = bool(entry.get("deposited", false))
		var pair_complete: bool = bool(entry.get("resonance_pair_complete", false))

		var row = HBoxContainer.new()
		row.add_theme_constant_override("separation", 6)

		# Status icon
		var icon_lbl = Label.new()
		if deposited:
			icon_lbl.text = "[+]" if pair_complete else "[*]"
			icon_lbl.add_theme_color_override("font_color", UITheme.GOLD if pair_complete else UITheme.GREEN)
		elif collected:
			icon_lbl.text = "[~]"
			icon_lbl.add_theme_color_override("font_color", UITheme.TEXT_INFO)
		else:
			icon_lbl.text = "[ ]"
			icon_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		icon_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		row.add_child(icon_lbl)

		var name_lbl = Label.new()
		name_lbl.text = fname if (collected or deposited) else "???"
		name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		var color = kind_colors.get(kind, UITheme.TEXT_SECONDARY)
		name_lbl.add_theme_color_override("font_color", color if (collected or deposited) else UITheme.TEXT_DISABLED)
		row.add_child(name_lbl)

		# Deposit button for collected but not yet deposited
		if collected and not deposited:
			var dep_btn = Button.new()
			dep_btn.text = "Deposit"
			dep_btn.custom_minimum_size.x = 60
			var fid: String = str(entry.get("fragment_id", ""))
			dep_btn.pressed.connect(_on_deposit_fragment.bind(fid))
			row.add_child(dep_btn)

		_trophy_list.add_child(row)


func _on_deposit_fragment(fragment_id: String) -> void:
	if bridge == null or not bridge.has_method("DepositFragmentV0"):
		return
	var result: Dictionary = bridge.call("DepositFragmentV0", fragment_id)
	if bool(result.get("success", false)):
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "Fragment deposited!", "system", 2.0)
	refresh(_market_node_id)


# GATE.S8.ANCIENT_HULLS.BRIDGE.001: Ancient hull restoration
func _update_hulls() -> void:
	if _hulls_list == null:
		return
	for child in _hulls_list.get_children():
		_hulls_list.remove_child(child)
		child.queue_free()

	if bridge == null or not bridge.has_method("GetAncientHullsV0"):
		return

	var hulls: Array = bridge.call("GetAncientHullsV0")
	if hulls.size() == 0:
		var empty_lbl = Label.new()
		empty_lbl.text = "No ancient hulls available"
		empty_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_hulls_list.add_child(empty_lbl)
		return

	for entry in hulls:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var dname: String = str(entry.get("display_name", ""))
		var restored: bool = bool(entry.get("restored", false))
		var can_restore: bool = bool(entry.get("can_restore", false))
		var credit_cost: int = int(entry.get("restore_credit_cost", 0))
		var exotic_cost: int = int(entry.get("restore_exotic_matter_cost", 0))
		var min_tier: int = int(entry.get("min_haven_tier", 3))
		var slots: int = int(entry.get("slot_count", 0))
		var hull_hp: int = int(entry.get("core_hull", 0))

		var row = VBoxContainer.new()
		row.add_theme_constant_override("separation", 2)

		var title_lbl = Label.new()
		title_lbl.text = dname
		title_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		title_lbl.add_theme_color_override("font_color", UITheme.CYAN if not restored else UITheme.GREEN)
		row.add_child(title_lbl)

		var stats_lbl = Label.new()
		stats_lbl.text = "Slots: %d | Hull: %d" % [slots, hull_hp]
		stats_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		stats_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		row.add_child(stats_lbl)

		if restored:
			var done_lbl = Label.new()
			done_lbl.text = "Restored"
			done_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
			done_lbl.add_theme_color_override("font_color", UITheme.GREEN)
			row.add_child(done_lbl)
		else:
			var cost_lbl = Label.new()
			cost_lbl.text = "Cost: %dcr + %d exotic matter | Haven Tier %d+" % [credit_cost, exotic_cost, min_tier]
			cost_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
			cost_lbl.add_theme_color_override("font_color", UITheme.TEXT_INFO)
			row.add_child(cost_lbl)

			var restore_btn = Button.new()
			restore_btn.text = "Restore Hull"
			restore_btn.disabled = not can_restore
			var cid: String = str(entry.get("ship_class_id", ""))
			restore_btn.pressed.connect(_on_restore_hull.bind(cid))
			row.add_child(restore_btn)

		_hulls_list.add_child(row)


# GATE.S8.WIN.PROGRESS_UI.001: Endgame progress section.
func _update_endgame_progress(status: Dictionary) -> void:
	if _endgame_section == null:
		return
	# Clear previous content.
	for c in _endgame_section.get_children():
		c.queue_free()

	var tier: int = int(status.get("tier", 0))
	# Only show endgame progress at Haven Tier 4+.
	if tier < 4:
		_endgame_section.visible = false
		return

	if bridge == null or not bridge.has_method("GetEndgameProgressV0"):
		_endgame_section.visible = false
		return

	var progress: Dictionary = bridge.call("GetEndgameProgressV0")
	var pct: int = int(progress.get("completion_percent", 0))

	# Check if an endgame path has been chosen (completion > 0 or path chosen).
	var game_result: Dictionary = {}
	if bridge.has_method("GetGameResultV0"):
		game_result = bridge.call("GetGameResultV0")
	var path_name: String = str(game_result.get("chosen_path_name", "None"))
	if path_name == "None":
		# No path chosen yet — show prompt.
		var prompt_lbl = Label.new()
		prompt_lbl.text = "Choose your endgame path at Haven."
		prompt_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		prompt_lbl.add_theme_color_override("font_color", UITheme.TEXT_INFO)
		_endgame_section.add_child(prompt_lbl)
		_endgame_section.visible = true
		return

	_endgame_section.visible = true

	# Path name + completion bar.
	var path_lbl = Label.new()
	path_lbl.text = "Path: %s — %d%% Complete" % [path_name, pct]
	path_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	if pct >= 100:
		path_lbl.add_theme_color_override("font_color", UITheme.GREEN)
	else:
		path_lbl.add_theme_color_override("font_color", Color(0.9, 0.8, 0.3))
	_endgame_section.add_child(path_lbl)

	# Progress bar.
	var bar = ProgressBar.new()
	bar.min_value = 0
	bar.max_value = 100
	bar.value = pct
	bar.custom_minimum_size = Vector2(0, 16)
	bar.show_percentage = false
	_endgame_section.add_child(bar)

	# Requirements checklist.
	_add_requirement_row("Haven Tier", progress.get("haven_tier_met", false),
		"%d / %d" % [int(progress.get("haven_tier_current", 0)), int(progress.get("haven_tier_required", 0))])

	var rep1_id: String = str(progress.get("faction_rep1_id", ""))
	if not rep1_id.is_empty():
		_add_requirement_row("%s Rep" % rep1_id.capitalize(), progress.get("faction_rep1_met", false),
			"%d / %d" % [int(progress.get("faction_rep1_current", 0)), int(progress.get("faction_rep1_required", 0))])

	var rep2_id: String = str(progress.get("faction_rep2_id", ""))
	if not rep2_id.is_empty():
		_add_requirement_row("%s Rep" % rep2_id.capitalize(), progress.get("faction_rep2_met", false),
			"%d / %d" % [int(progress.get("faction_rep2_current", 0)), int(progress.get("faction_rep2_required", 0))])

	var frag1_id: String = str(progress.get("fragment1_id", ""))
	if not frag1_id.is_empty():
		_add_requirement_row("Fragment: %s" % frag1_id, progress.get("fragment1_met", false))

	var frag2_id: String = str(progress.get("fragment2_id", ""))
	if not frag2_id.is_empty():
		_add_requirement_row("Fragment: %s" % frag2_id, progress.get("fragment2_met", false))

	var rev_req: int = int(progress.get("revelations_required", 0))
	if rev_req > 0:
		_add_requirement_row("Revelations", progress.get("revelations_met", false),
			"%d / %d" % [int(progress.get("revelations_current", 0)), rev_req])


func _add_requirement_row(label_text: String, met: bool, value_text: String = "") -> void:
	var row = HBoxContainer.new()
	var icon_lbl = Label.new()
	icon_lbl.text = "OK" if met else "--"
	icon_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	icon_lbl.add_theme_color_override("font_color", UITheme.GREEN if met else UITheme.TEXT_DISABLED)
	icon_lbl.custom_minimum_size = Vector2(24, 0)
	row.add_child(icon_lbl)

	var name_lbl = Label.new()
	name_lbl.text = label_text
	name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	name_lbl.add_theme_color_override("font_color", UITheme.TEXT_PRIMARY if met else UITheme.TEXT_DISABLED)
	name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(name_lbl)

	if not value_text.is_empty():
		var val_lbl = Label.new()
		val_lbl.text = value_text
		val_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		val_lbl.add_theme_color_override("font_color", UITheme.TEXT_INFO if met else UITheme.TEXT_DISABLED)
		row.add_child(val_lbl)

	_endgame_section.add_child(row)


# GATE.S8.HAVEN.DEPTH_BRIDGE.001: Keeper ambient state display.
func _update_keeper() -> void:
	if _keeper_section == null:
		return
	for c in _keeper_section.get_children():
		c.queue_free()

	if bridge == null or not bridge.has_method("GetKeeperStateV0"):
		return

	var keeper: Dictionary = bridge.call("GetKeeperStateV0")
	var level: int = int(keeper.get("keeper_level", 0))
	var name_str: String = str(keeper.get("keeper_name", "Dormant"))
	var exotic: int = int(keeper.get("exotic_matter_delivered", 0))
	var logs: int = int(keeper.get("data_logs_discovered", 0))
	var frags: int = int(keeper.get("installed_fragments", 0))

	var level_lbl = Label.new()
	level_lbl.text = "Level %d — %s" % [level, name_str]
	level_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	level_lbl.add_theme_color_override("font_color", UITheme.PURPLE_LIGHT if level >= 3 else UITheme.TEXT_PRIMARY)
	_keeper_section.add_child(level_lbl)

	var stats_lbl = Label.new()
	stats_lbl.text = "Exotic Matter: %d | Data Logs: %d | Fragments: %d" % [exotic, logs, frags]
	stats_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	stats_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_keeper_section.add_child(stats_lbl)


# GATE.S8.HAVEN.DEPTH_BRIDGE.001: Resonance chamber display.
func _update_resonance_chamber() -> void:
	if _resonance_section == null:
		return
	for c in _resonance_section.get_children():
		c.queue_free()

	if bridge == null or not bridge.has_method("GetResonanceChamberV0"):
		return

	var chamber: Dictionary = bridge.call("GetResonanceChamberV0")
	if not bool(chamber.get("available", false)):
		var locked_lbl = Label.new()
		locked_lbl.text = "Requires Haven Tier 3+"
		locked_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		locked_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_resonance_section.add_child(locked_lbl)
		return

	var cooldown: int = int(chamber.get("cooldown_remaining", 0))
	if cooldown > 0:
		var cd_lbl = Label.new()
		cd_lbl.text = "Cooldown: %d ticks" % cooldown
		cd_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		cd_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
		_resonance_section.add_child(cd_lbl)

	var activated: Array = chamber.get("activated_pairs", [])
	if activated.size() > 0:
		var act_lbl = Label.new()
		act_lbl.text = "Active Pairs: %d" % activated.size()
		act_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		act_lbl.add_theme_color_override("font_color", UITheme.GREEN)
		_resonance_section.add_child(act_lbl)

	var available_pairs: Array = chamber.get("available_pairs", [])
	if available_pairs.size() > 0:
		var avail_lbl = Label.new()
		avail_lbl.text = "Ready to Activate: %d" % available_pairs.size()
		avail_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		avail_lbl.add_theme_color_override("font_color", UITheme.CYAN)
		_resonance_section.add_child(avail_lbl)

		for pair_id in available_pairs:
			var btn = Button.new()
			btn.text = "Activate: %s" % str(pair_id).replace("_", " ").capitalize()
			var pid: String = str(pair_id)
			btn.pressed.connect(_on_activate_resonance.bind(pid))
			btn.disabled = cooldown > 0
			_resonance_section.add_child(btn)

	if activated.size() == 0 and available_pairs.size() == 0:
		var none_lbl = Label.new()
		none_lbl.text = "Deposit fragment pairs to unlock resonance"
		none_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		none_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_resonance_section.add_child(none_lbl)


func _on_activate_resonance(pair_id: String) -> void:
	if bridge == null or not bridge.has_method("ActivateResonancePairV0"):
		return
	bridge.call("ActivateResonancePairV0", pair_id)
	var toast_mgr = get_node_or_null("/root/ToastManager")
	if toast_mgr and toast_mgr.has_method("show_priority_toast"):
		toast_mgr.call("show_priority_toast", "Resonance pair activated!", "system", 2.0)
	refresh(_market_node_id)


# GATE.S8.HAVEN.DEPTH_BRIDGE.001: Fabricator display.
func _update_fabricator() -> void:
	if _fabricator_section == null:
		return
	for c in _fabricator_section.get_children():
		c.queue_free()

	if bridge == null or not bridge.has_method("GetFabricatorV0"):
		return

	var fab: Dictionary = bridge.call("GetFabricatorV0")
	if not bool(fab.get("available", false)):
		var locked_lbl = Label.new()
		locked_lbl.text = "Requires Haven Tier 3+"
		locked_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		locked_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_fabricator_section.add_child(locked_lbl)
		return

	var fabricating: String = str(fab.get("fabricating_module", ""))
	var ticks_left: int = int(fab.get("ticks_remaining", 0))

	if not fabricating.is_empty() and ticks_left > 0:
		var progress_lbl = Label.new()
		progress_lbl.text = "Fabricating: %s (%d ticks)" % [fabricating.replace("_", " ").capitalize(), ticks_left]
		progress_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		progress_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
		_fabricator_section.add_child(progress_lbl)
	else:
		var idle_lbl = Label.new()
		idle_lbl.text = "Idle — ready to fabricate"
		idle_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		idle_lbl.add_theme_color_override("font_color", UITheme.GREEN)
		_fabricator_section.add_child(idle_lbl)

	var completed: Array = fab.get("completed_modules", [])
	if completed.size() > 0:
		var done_lbl = Label.new()
		done_lbl.text = "Completed: %d modules" % completed.size()
		done_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		done_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		_fabricator_section.add_child(done_lbl)

	var cost_lbl = Label.new()
	cost_lbl.text = "Cost: %d exotic matter | Duration: %d ticks" % [int(fab.get("exotic_matter_cost", 0)), int(fab.get("duration_ticks", 0))]
	cost_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	cost_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
	_fabricator_section.add_child(cost_lbl)


# GATE.S8.HAVEN.ENDGAME_BRIDGE.001: Endgame path choice display.
func _update_path_choice() -> void:
	if _path_choice_section == null:
		return
	for c in _path_choice_section.get_children():
		c.queue_free()

	if bridge == null or not bridge.has_method("GetEndgamePathsV0"):
		return

	var paths: Dictionary = bridge.call("GetEndgamePathsV0")
	var chosen: String = str(paths.get("chosen_path", "None"))

	if chosen != "None":
		var chosen_lbl = Label.new()
		chosen_lbl.text = "Chosen: %s" % chosen
		chosen_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		chosen_lbl.add_theme_color_override("font_color", UITheme.GOLD)
		_path_choice_section.add_child(chosen_lbl)
		return

	if not bool(paths.get("can_choose", false)):
		var locked_lbl = Label.new()
		locked_lbl.text = "Requires Haven Tier %d+" % int(paths.get("min_tier", 4))
		locked_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		locked_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_path_choice_section.add_child(locked_lbl)
		return

	var available: Array = paths.get("available_paths", [])
	for p in available:
		if typeof(p) != TYPE_DICTIONARY:
			continue
		var pid: String = str(p.get("id", ""))
		var desc: String = str(p.get("description", ""))

		var row = VBoxContainer.new()
		row.add_theme_constant_override("separation", 2)

		var btn = Button.new()
		btn.text = "Choose: %s" % pid
		btn.pressed.connect(_on_choose_path.bind(pid))
		row.add_child(btn)

		var desc_lbl = Label.new()
		desc_lbl.text = desc
		desc_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		desc_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(desc_lbl)

		_path_choice_section.add_child(row)


func _on_choose_path(path_id: String) -> void:
	if bridge == null or not bridge.has_method("ChooseEndgamePathV0"):
		return
	var ok: bool = bridge.call("ChooseEndgamePathV0", path_id)
	if ok:
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "Endgame path chosen: %s" % path_id, "milestone", 3.0)
	refresh(_market_node_id)


# GATE.S8.HAVEN.ENDGAME_BRIDGE.001: Accommodation thread progress display.
func _update_accommodation() -> void:
	if _accommodation_section == null:
		return
	for c in _accommodation_section.get_children():
		c.queue_free()

	if bridge == null or not bridge.has_method("GetAccommodationProgressV0"):
		return

	var progress: Dictionary = bridge.call("GetAccommodationProgressV0")
	var max_prog: int = int(progress.get("max_progress", 100))
	var _tier_req: int = int(progress.get("tier_required", 3))

	var threads := ["Discovery", "Commerce", "Conflict", "Harmony"]
	var thread_colors := {
		"Discovery": UITheme.CYAN,
		"Commerce": UITheme.GOLD,
		"Conflict": UITheme.RED_LIGHT,
		"Harmony": UITheme.GREEN,
	}

	var any_progress := false
	for thread_name in threads:
		var val: int = int(progress.get(thread_name, 0))
		if val > 0:
			any_progress = true

		var row = HBoxContainer.new()
		row.add_theme_constant_override("separation", 8)

		var name_lbl = Label.new()
		name_lbl.text = thread_name
		name_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		name_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		name_lbl.add_theme_color_override("font_color", thread_colors.get(thread_name, UITheme.TEXT_PRIMARY))
		row.add_child(name_lbl)

		var val_lbl = Label.new()
		val_lbl.text = "%d / %d" % [val, max_prog]
		val_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		val_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		row.add_child(val_lbl)

		_accommodation_section.add_child(row)

	if not any_progress:
		var hint_lbl = Label.new()
		hint_lbl.text = "Trade, explore, and fight to advance threads"
		hint_lbl.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
		hint_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_accommodation_section.add_child(hint_lbl)


# GATE.S8.HAVEN.ENDGAME_BRIDGE.001 + GATE.T47.HAVEN.COMMUNION_REP.001: Communion Representative display.
func _update_communion_rep() -> void:
	if _communion_section == null:
		return
	for c in _communion_section.get_children():
		c.queue_free()

	if bridge == null or not bridge.has_method("GetCommunionRepV0"):
		return

	var rep: Dictionary = bridge.call("GetCommunionRepV0")
	if not bool(rep.get("present", false)):
		var absent_lbl = Label.new()
		absent_lbl.text = "Not yet arrived (Haven Tier 3+)"
		absent_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		absent_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_communion_section.add_child(absent_lbl)
		return

	var tier: int = int(rep.get("dialogue_tier", 0))
	var tier_names := ["Introduction", "Trust", "Revelation"]
	var tier_name: String = tier_names[mini(tier, tier_names.size() - 1)]

	var status_lbl = Label.new()
	status_lbl.text = "Dialogue: %s (Tier %d)" % [tier_name, tier]
	status_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	status_lbl.add_theme_color_override("font_color", Color(0.5, 0.9, 1.0))
	_communion_section.add_child(status_lbl)

	# GATE.T47.HAVEN.COMMUNION_REP.001: Communion Representative dialogue line display.
	# Shows one ethereal purple dialogue line + "Speak Again" button to cycle.
	if bridge.has_method("GetCommunionRepDialogueV0"):
		var dialogue: Dictionary = bridge.call("GetCommunionRepDialogueV0")
		if not dialogue.is_empty():
			var text: String = str(dialogue.get("text", ""))
			if not text.is_empty():
				var dialogue_lbl = Label.new()
				dialogue_lbl.text = "\"%s\"" % text
				dialogue_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
				dialogue_lbl.add_theme_color_override("font_color", Color(0.7, 0.4, 1.0))
				dialogue_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
				_communion_section.add_child(dialogue_lbl)

				var tag_lbl = Label.new()
				tag_lbl.text = "— %s" % str(dialogue.get("tag", ""))
				tag_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
				tag_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
				_communion_section.add_child(tag_lbl)

		var speak_btn = Button.new()
		speak_btn.text = "Speak Again"
		speak_btn.pressed.connect(_on_communion_speak_again)
		_communion_section.add_child(speak_btn)


# GATE.T47.HAVEN.COMMUNION_REP.001: Cycle to next Communion Representative dialogue line.
func _on_communion_speak_again() -> void:
	if bridge == null:
		return
	if bridge.has_method("CycleCommunionRepDialogueV0"):
		bridge.call("CycleCommunionRepDialogueV0")
	_update_communion_rep()


func _on_restore_hull(ship_class_id: String) -> void:
	if bridge == null or not bridge.has_method("RestoreAncientHullV0"):
		return
	var result: Dictionary = bridge.call("RestoreAncientHullV0", ship_class_id)
	if bool(result.get("success", false)):
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_priority_toast"):
			toast_mgr.call("show_priority_toast", "Hull restoration complete!", "system", 2.0)
	refresh(_market_node_id)
