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
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Haven upgrade initiated", 2.0)
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
			if toast_mgr and toast_mgr.has_method("show_toast"):
				toast_mgr.call("show_toast", "Ship swapped!", 2.0)
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
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Fragment deposited!", 2.0)
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


func _on_restore_hull(ship_class_id: String) -> void:
	if bridge == null or not bridge.has_method("RestoreAncientHullV0"):
		return
	var result: Dictionary = bridge.call("RestoreAncientHullV0", ship_class_id)
	if bool(result.get("success", false)):
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Hull restoration complete!", 2.0)
	refresh(_market_node_id)
