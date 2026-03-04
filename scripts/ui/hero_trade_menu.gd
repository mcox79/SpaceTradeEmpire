extends CanvasLayer

# GATE.S1.HERO_SHIP_LOOP.MARKET_SCREEN.001
# Hero trade menu: centered panel with station name, goods rows, cargo summary, undock button.

var _market_node_id: String = ""
var _rows_container: VBoxContainer = null
var _title_label: Label = null
var _cargo_label: Label = null
var _missions_container: VBoxContainer = null
var _research_container: VBoxContainer = null
var _refit_container: VBoxContainer = null
var _maint_container: VBoxContainer = null

func _ready():
	visible = false
	# Center the panel on screen using a MarginContainer.
	var margin = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_CENTER)
	margin.grow_horizontal = Control.GROW_DIRECTION_BOTH
	margin.grow_vertical = Control.GROW_DIRECTION_BOTH
	add_child(margin)

	var panel = PanelContainer.new()
	panel.custom_minimum_size = Vector2(420, 0)
	margin.add_child(panel)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 6)
	panel.add_child(vbox)

	# Station title
	_title_label = Label.new()
	_title_label.text = "STATION MARKET"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", 20)
	vbox.add_child(_title_label)

	vbox.add_child(HSeparator.new())

	# Column header
	var header = HBoxContainer.new()
	for col_name in ["Good", "Buy", "Sell", "", ""]:
		var lbl = Label.new()
		lbl.text = col_name
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		header.add_child(lbl)
	vbox.add_child(header)

	# Goods rows
	_rows_container = VBoxContainer.new()
	_rows_container.add_theme_constant_override("separation", 2)
	vbox.add_child(_rows_container)

	vbox.add_child(HSeparator.new())

	# Cargo summary line
	_cargo_label = Label.new()
	_cargo_label.text = ""
	vbox.add_child(_cargo_label)

	vbox.add_child(HSeparator.new())

	# Missions section
	_missions_container = VBoxContainer.new()
	_missions_container.add_theme_constant_override("separation", 4)
	vbox.add_child(_missions_container)

	# GATE.S4.UI_INDU.RESEARCH.001: Research section
	_research_container = VBoxContainer.new()
	_research_container.add_theme_constant_override("separation", 4)
	vbox.add_child(_research_container)

	# GATE.S4.UI_INDU.UPGRADE.001: Refit section
	_refit_container = VBoxContainer.new()
	_refit_container.add_theme_constant_override("separation", 4)
	vbox.add_child(_refit_container)

	# GATE.S4.UI_INDU.MAINT.001: Maintenance section
	_maint_container = VBoxContainer.new()
	_maint_container.add_theme_constant_override("separation", 4)
	vbox.add_child(_maint_container)

	# Undock button
	var btn_undock = Button.new()
	btn_undock.text = "Undock"
	btn_undock.pressed.connect(_on_undock_pressed)
	vbox.add_child(btn_undock)

func open_market_v0(node_id: String) -> void:
	_market_node_id = node_id
	visible = true
	if _title_label:
		_title_label.text = "STATION: %s" % node_id
	_rebuild_rows()

func close_market_v0() -> void:
	_market_node_id = ""
	visible = false

func get_market_view_v0() -> Array:
	if _market_node_id.is_empty():
		return []
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetPlayerMarketViewV0"):
		return bridge.call("GetPlayerMarketViewV0", _market_node_id)
	return []

func buy_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		# DispatchPlayerTradeV0 blocks until sim tick advances — no timer race.
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, true)
		_rebuild_rows()

func sell_one_v0(good_id: String) -> void:
	if _market_node_id.is_empty() or good_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		# DispatchPlayerTradeV0 blocks until sim tick advances — no timer race.
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, 1, false)
		_rebuild_rows()

func get_panel_row_count_v0() -> int:
	if _rows_container == null:
		return 0
	return _rows_container.get_child_count()

func _on_undock_pressed() -> void:
	var gm = get_node_or_null("/root/GameManager")
	if gm and gm.has_method("undock_v0"):
		gm.call("undock_v0")

func _rebuild_rows() -> void:
	if _rows_container == null:
		return
	for child in _rows_container.get_children():
		_rows_container.remove_child(child)
		child.queue_free()
	var view = get_market_view_v0()
	for entry in view:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var good_id: String = str(entry.get("good_id", ""))
		var buy_price: int = int(entry.get("buy_price", 0))
		var sell_price: int = int(entry.get("sell_price", 0))
		var stock: int = int(entry.get("qty", 0))

		var row = HBoxContainer.new()

		var lbl_id = Label.new()
		lbl_id.text = good_id
		lbl_id.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_id)

		var lbl_buy = Label.new()
		lbl_buy.text = "%d cr" % buy_price
		lbl_buy.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_buy)

		var lbl_sell = Label.new()
		lbl_sell.text = "%d cr" % sell_price
		lbl_sell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_sell)

		var btn_buy = Button.new()
		btn_buy.text = "Buy 1"
		btn_buy.pressed.connect(buy_one_v0.bind(good_id))
		row.add_child(btn_buy)

		var btn_sell = Button.new()
		btn_sell.text = "Sell 1"
		btn_sell.pressed.connect(sell_one_v0.bind(good_id))
		row.add_child(btn_sell)

		_rows_container.add_child(row)

	# GATE.S4.INDU_STRUCT.PLAYABLE_VIEW.001: show production info for this node
	_rebuild_production_info()
	_rebuild_missions()
	_rebuild_research()
	_rebuild_refit()
	_rebuild_maintenance()

	# Update cargo summary
	if _cargo_label:
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("GetPlayerCargoV0"):
			var cargo = bridge.call("GetPlayerCargoV0")
			if typeof(cargo) == TYPE_ARRAY and cargo.size() > 0:
				var parts: Array = []
				for item in cargo:
					if typeof(item) == TYPE_DICTIONARY:
						parts.append("%s x%d" % [str(item.get("good_id", "")), int(item.get("qty", 0))])
				_cargo_label.text = "Cargo: " + (", ".join(parts) if parts.size() > 0 else "empty")
			else:
				_cargo_label.text = "Cargo: empty"

func _rebuild_production_info() -> void:
	if _rows_container == null or _market_node_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if not bridge or not bridge.has_method("GetNodeIndustryV0"):
		return
	var industry: Array = bridge.call("GetNodeIndustryV0", _market_node_id)
	if industry.size() == 0:
		return

	# Add a separator and header
	_rows_container.add_child(HSeparator.new())
	var header = Label.new()
	header.text = "PRODUCTION"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 16)
	_rows_container.add_child(header)

	for site_info in industry:
		if typeof(site_info) != TYPE_DICTIONARY:
			continue
		var recipe_id: String = str(site_info.get("recipe_id", ""))
		var eff_pct: int = int(site_info.get("efficiency_pct", 0))
		var health_pct: int = int(site_info.get("health_pct", 0))
		var outputs: Array = site_info.get("outputs", [])
		var out_str: String = ", ".join(outputs) if outputs.size() > 0 else "none"

		var row = HBoxContainer.new()
		var lbl = Label.new()
		lbl.text = "%s  eff:%d%%  hp:%d%%  -> %s" % [recipe_id if recipe_id != "" else "(natural)", eff_pct, health_pct, out_str]
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl)
		_rows_container.add_child(row)

func _rebuild_missions() -> void:
	if _missions_container == null:
		return
	for child in _missions_container.get_children():
		_missions_container.remove_child(child)
		child.queue_free()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return

	# Show active mission status
	if bridge.has_method("GetActiveMissionV0"):
		var active: Dictionary = bridge.call("GetActiveMissionV0")
		var active_id: String = str(active.get("mission_id", ""))
		if not active_id.is_empty():
			var hdr = Label.new()
			hdr.text = "ACTIVE MISSION"
			hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			hdr.add_theme_font_size_override("font_size", 16)
			_missions_container.add_child(hdr)

			var title_lbl = Label.new()
			title_lbl.text = str(active.get("title", active_id))
			title_lbl.add_theme_font_size_override("font_size", 14)
			_missions_container.add_child(title_lbl)

			var obj_lbl = Label.new()
			var step_cur: int = int(active.get("current_step", 0))
			var step_max: int = int(active.get("total_steps", 0))
			var obj_text: String = str(active.get("objective_text", ""))
			var target_node: String = str(active.get("target_node_id", ""))
			var target_good: String = str(active.get("target_good_id", ""))
			var detail_parts: Array = []
			if not target_node.is_empty():
				detail_parts.append(target_node)
			if not target_good.is_empty():
				detail_parts.append(target_good)
			if detail_parts.size() > 0:
				obj_text += " (%s)" % ", ".join(detail_parts)
			obj_lbl.text = "Step %d/%d: %s" % [step_cur + 1, step_max, obj_text]
			obj_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
			_missions_container.add_child(obj_lbl)
			return  # Only one active mission at a time

	# No active mission — show available missions
	if bridge.has_method("GetMissionListV0"):
		var missions: Array = bridge.call("GetMissionListV0")
		if missions.size() == 0:
			return

		var hdr = Label.new()
		hdr.text = "AVAILABLE MISSIONS"
		hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		hdr.add_theme_font_size_override("font_size", 16)
		_missions_container.add_child(hdr)

		for m in missions:
			if typeof(m) != TYPE_DICTIONARY:
				continue
			var mid: String = str(m.get("mission_id", ""))
			var mtitle: String = str(m.get("title", mid))
			var mdesc: String = str(m.get("description", ""))
			var mreward: int = int(m.get("reward", 0))

			var row = HBoxContainer.new()
			var info_lbl = Label.new()
			info_lbl.text = "%s (%d cr)" % [mtitle, mreward]
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			row.add_child(info_lbl)

			var btn_accept = Button.new()
			btn_accept.text = "Accept"
			btn_accept.pressed.connect(_on_accept_mission.bind(mid))
			row.add_child(btn_accept)

			_missions_container.add_child(row)

func _on_accept_mission(mission_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("AcceptMissionV0"):
		bridge.call("AcceptMissionV0", mission_id)
		_rebuild_rows()

# GATE.S4.UI_INDU.RESEARCH.001: Research UI panel
func _rebuild_research() -> void:
	if _research_container == null:
		return
	for child in _research_container.get_children():
		_research_container.remove_child(child)
		child.queue_free()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return

	# Show current research status
	if bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = bridge.call("GetResearchStatusV0")
		var researching: bool = bool(status.get("researching", false))
		if researching:
			var hdr = Label.new()
			hdr.text = "RESEARCH"
			hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			hdr.add_theme_font_size_override("font_size", 16)
			_research_container.add_child(hdr)

			var tech_id: String = str(status.get("tech_id", ""))
			var progress: int = int(status.get("progress_pct", 0))
			var lbl = Label.new()
			lbl.text = "Researching: %s (%d%%)" % [tech_id, progress]
			_research_container.add_child(lbl)
			return

	# Show available techs to research
	if bridge.has_method("GetTechTreeV0"):
		var techs: Array = bridge.call("GetTechTreeV0")
		var available: Array = []
		for t in techs:
			if typeof(t) != TYPE_DICTIONARY:
				continue
			if bool(t.get("unlocked", false)):
				continue
			if not bool(t.get("prerequisites_met", false)):
				continue
			available.append(t)

		if available.size() == 0:
			return

		var hdr = Label.new()
		hdr.text = "RESEARCH"
		hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		hdr.add_theme_font_size_override("font_size", 16)
		_research_container.add_child(hdr)

		for t in available:
			var tid: String = str(t.get("tech_id", ""))
			var tname: String = str(t.get("display_name", tid))
			var tcost: int = int(t.get("credit_cost", 0))
			var row = HBoxContainer.new()
			var info_lbl = Label.new()
			info_lbl.text = "%s (%d cr)" % [tname, tcost]
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			row.add_child(info_lbl)

			var btn = Button.new()
			btn.text = "Research"
			btn.pressed.connect(_on_start_research.bind(tid))
			row.add_child(btn)
			_research_container.add_child(row)

func _on_start_research(tech_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("StartResearchV0"):
		bridge.call("StartResearchV0", tech_id)
		_rebuild_rows()

# GATE.S4.UI_INDU.UPGRADE.001: Refit/upgrade UI panel
func _rebuild_refit() -> void:
	if _refit_container == null:
		return
	for child in _refit_container.get_children():
		_refit_container.remove_child(child)
		child.queue_free()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return
	if not bridge.has_method("GetPlayerFleetSlotsV0") or not bridge.has_method("GetAvailableModulesV0"):
		return

	var slots: Array = bridge.call("GetPlayerFleetSlotsV0")
	if slots.size() == 0:
		return

	var hdr = Label.new()
	hdr.text = "REFIT"
	hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hdr.add_theme_font_size_override("font_size", 16)
	_refit_container.add_child(hdr)

	# Show current slots
	for i in range(slots.size()):
		var slot: Dictionary = slots[i] if typeof(slots[i]) == TYPE_DICTIONARY else {}
		var slot_kind: String = str(slot.get("slot_kind", ""))
		var installed: String = str(slot.get("installed_module_id", ""))
		var row = HBoxContainer.new()
		var lbl = Label.new()
		lbl.text = "[%s] %s" % [slot_kind, installed if not installed.is_empty() else "(empty)"]
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl)
		_refit_container.add_child(row)

	# Show installable modules
	var modules: Array = bridge.call("GetAvailableModulesV0")
	var installable: Array = []
	for m in modules:
		if typeof(m) != TYPE_DICTIONARY:
			continue
		if bool(m.get("can_install", false)):
			installable.append(m)

	if installable.size() > 0:
		var sub_hdr = Label.new()
		sub_hdr.text = "Available Modules:"
		sub_hdr.add_theme_font_size_override("font_size", 14)
		_refit_container.add_child(sub_hdr)

		for m in installable:
			var mid: String = str(m.get("module_id", ""))
			var mname: String = str(m.get("display_name", mid))
			var mcost: int = int(m.get("credit_cost", 0))
			var mkind: String = str(m.get("slot_kind", ""))
			var row = HBoxContainer.new()
			var info_lbl = Label.new()
			info_lbl.text = "%s [%s] %d cr" % [mname, mkind, mcost]
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			row.add_child(info_lbl)

			# Find first matching empty slot
			var target_slot: int = -1
			for si in range(slots.size()):
				var s: Dictionary = slots[si] if typeof(slots[si]) == TYPE_DICTIONARY else {}
				if str(s.get("slot_kind", "")) == mkind and str(s.get("installed_module_id", "")).is_empty():
					target_slot = si
					break

			if target_slot >= 0:
				var btn = Button.new()
				btn.text = "Install"
				btn.pressed.connect(_on_install_module.bind(target_slot, mid))
				row.add_child(btn)
			_refit_container.add_child(row)

func _on_install_module(slot_index: int, module_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("InstallModuleV0"):
		bridge.call("InstallModuleV0", "fleet_trader_1", slot_index, module_id)
		_rebuild_rows()

# GATE.S4.UI_INDU.MAINT.001: Maintenance/repair UI panel
func _rebuild_maintenance() -> void:
	if _maint_container == null:
		return
	for child in _maint_container.get_children():
		_maint_container.remove_child(child)
		child.queue_free()

	if _market_node_id.is_empty():
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetNodeMaintenanceV0"):
		return

	var sites: Array = bridge.call("GetNodeMaintenanceV0", _market_node_id)
	var damaged: Array = []
	for s in sites:
		if typeof(s) != TYPE_DICTIONARY:
			continue
		if bool(s.get("needs_repair", false)):
			damaged.append(s)

	if damaged.size() == 0:
		return

	var hdr = Label.new()
	hdr.text = "MAINTENANCE"
	hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hdr.add_theme_font_size_override("font_size", 16)
	_maint_container.add_child(hdr)

	for s in damaged:
		var sid: String = str(s.get("site_id", ""))
		var recipe: String = str(s.get("recipe_id", ""))
		var hp: int = int(s.get("health_pct", 0))
		var eff: int = int(s.get("efficiency_pct", 0))
		var cost: int = int(s.get("repair_cost", 0))

		var row = HBoxContainer.new()
		var lbl = Label.new()
		lbl.text = "%s hp:%d%% eff:%d%%" % [recipe if not recipe.is_empty() else sid, hp, eff]
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl)

		var btn = Button.new()
		btn.text = "Repair (%d cr)" % cost
		btn.pressed.connect(_on_repair_site.bind(sid))
		row.add_child(btn)
		_maint_container.add_child(row)

func _on_repair_site(site_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("RepairSiteV0"):
		bridge.call("RepairSiteV0", site_id)
		_rebuild_rows()
