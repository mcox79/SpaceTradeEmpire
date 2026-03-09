extends CanvasLayer

# GATE.S1.HERO_SHIP_LOOP.MARKET_SCREEN.001
# GATE.S18.EMPIRE_DASH.DOCK_TABS.001: 5-tab dock menu (Market | Jobs | Ship | Station | Intel)
# GATE.S13.DOCK.CONTEXT.001: Station context description
# GATE.S13.DOCK.HIDE_EMPTY.001: Hide sections with no content
# GATE.S13.TERMINOLOGY.001: Programs → Automation, player-friendly terms

var _market_node_id: String = ""
var _collapsed: Dictionary = {"research": true, "refit": true, "maintenance": true, "construction": true, "trade_routes": true, "programs": true, "encounters": false}
var _rows_container: VBoxContainer = null
var _title_label: Label = null
var _context_label: Label = null  # GATE.S13.DOCK.CONTEXT.001
var _planet_info_label: Label = null
var _security_label: Label = null
var _cargo_label: Label = null
var _missions_container: VBoxContainer = null
var _research_container: VBoxContainer = null
var _refit_container: VBoxContainer = null
var _maint_container: VBoxContainer = null
var _construction_container: VBoxContainer = null
var _trade_routes_container: VBoxContainer = null
var _programs_container: VBoxContainer = null
var _encounters_container: VBoxContainer = null  # GATE.S6.ANOMALY.ENCOUNTER_UI.001
var _station_info_container: VBoxContainer = null  # GATE.S18.EMPIRE_DASH.STATION_TAB.001
var _ship_info_container: VBoxContainer = null  # GATE.S18.EMPIRE_DASH.SHIP_TAB.001

# GATE.S7.FACTION.UI_REPUTATION.001: Faction tariff & access display
var _tariff_label: Label = null
var _access_denied_label: Label = null
# GATE.S7.REPUTATION.UI_INDICATORS.001: Rep tier label
var _rep_tier_label: Label = null

# GATE.S18.EMPIRE_DASH.DOCK_TABS.001: Tab state
var _active_dock_tab: int = 0  # 0=Market, 1=Jobs, 2=Ship, 3=Station, 4=Intel
var _tab_market: VBoxContainer = null
var _tab_jobs: VBoxContainer = null
var _tab_ship: VBoxContainer = null
var _tab_station: VBoxContainer = null
var _tab_intel: VBoxContainer = null
var _tab_buttons: Array = []

func _ready():
	visible = false
	var panel = PanelContainer.new()
	panel.anchor_left = 0.5
	panel.anchor_right = 0.5
	panel.offset_left = -275
	panel.offset_right = 275
	panel.anchor_top = 0.0
	panel.anchor_bottom = 1.0
	panel.offset_top = 40
	panel.offset_bottom = -40
	# GATE.S14.DOCK.VISUAL_FRAME.001: dark navy panel with border
	panel.add_theme_stylebox_override("panel", UITheme.make_panel_dock())
	add_child(panel)

	var scroll = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.add_child(scroll)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 6)
	vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(vbox)

	# Station/Planet title
	_title_label = Label.new()
	_title_label.text = "STATION MARKET"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", UITheme.FONT_TITLE)
	vbox.add_child(_title_label)

	# GATE.S13.DOCK.CONTEXT.001: Station context description
	_context_label = Label.new()
	_context_label.text = ""
	_context_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_context_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_context_label.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	_context_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_context_label.visible = false
	vbox.add_child(_context_label)

	# GATE.S7.PLANET.UI.001: Planet info subtitle (hidden for stations).
	_planet_info_label = Label.new()
	_planet_info_label.text = ""
	_planet_info_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_planet_info_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_planet_info_label.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	_planet_info_label.visible = false
	vbox.add_child(_planet_info_label)

	# GATE.S5.SEC_LANES.UI.001: Security band label
	_security_label = Label.new()
	_security_label.text = ""
	_security_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_security_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_security_label.visible = false
	vbox.add_child(_security_label)

	# GATE.S7.FACTION.UI_REPUTATION.001: Tariff rate label
	_tariff_label = Label.new()
	_tariff_label.text = ""
	_tariff_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_tariff_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_tariff_label.add_theme_color_override("font_color", UITheme.ORANGE)
	_tariff_label.visible = false
	vbox.add_child(_tariff_label)

	# GATE.S7.REPUTATION.UI_INDICATORS.001: Rep tier + price modifier label
	_rep_tier_label = Label.new()
	_rep_tier_label.text = ""
	_rep_tier_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_rep_tier_label.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	_rep_tier_label.visible = false
	vbox.add_child(_rep_tier_label)

	# GATE.S7.FACTION.UI_REPUTATION.001: Access denied overlay
	_access_denied_label = Label.new()
	_access_denied_label.text = "ACCESS DENIED — Reputation too low"
	_access_denied_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_access_denied_label.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_access_denied_label.add_theme_color_override("font_color", UITheme.RED)
	_access_denied_label.visible = false
	vbox.add_child(_access_denied_label)

	vbox.add_child(HSeparator.new())

	# GATE.S18.EMPIRE_DASH.DOCK_TABS.001: Tab bar (5 tabs)
	var tab_bar = HBoxContainer.new()
	tab_bar.add_theme_constant_override("separation", 4)
	tab_bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_tab_buttons = []
	for tab_name in ["Market", "Jobs", "Ship", "Station", "Intel"]:
		var btn = Button.new()
		btn.text = tab_name
		btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		btn.toggle_mode = true
		var idx: int = _tab_buttons.size()
		btn.pressed.connect(_switch_dock_tab.bind(idx))
		tab_bar.add_child(btn)
		_tab_buttons.append(btn)
	vbox.add_child(tab_bar)

	vbox.add_child(HSeparator.new())

	# Tab 1: Market
	_tab_market = VBoxContainer.new()
	_tab_market.add_theme_constant_override("separation", 4)
	vbox.add_child(_tab_market)

	# Column header
	var header = HBoxContainer.new()
	for col_name in ["Good", "Buy", "Sell", "Buy", "Sell"]:
		var lbl = Label.new()
		lbl.text = col_name
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		header.add_child(lbl)
	_tab_market.add_child(header)

	# Goods rows
	_rows_container = VBoxContainer.new()
	_rows_container.add_theme_constant_override("separation", 2)
	_tab_market.add_child(_rows_container)

	_tab_market.add_child(HSeparator.new())

	# Cargo summary line
	_cargo_label = Label.new()
	_cargo_label.text = ""
	_tab_market.add_child(_cargo_label)

	# Tab 2: Jobs
	_tab_jobs = VBoxContainer.new()
	_tab_jobs.add_theme_constant_override("separation", 4)
	vbox.add_child(_tab_jobs)

	_missions_container = VBoxContainer.new()
	_missions_container.add_theme_constant_override("separation", 4)
	_tab_jobs.add_child(_missions_container)

	# Tab 3: Ship (refit, maintenance)
	_tab_ship = VBoxContainer.new()
	_tab_ship.add_theme_constant_override("separation", 4)
	vbox.add_child(_tab_ship)

	# GATE.S18.EMPIRE_DASH.SHIP_TAB.001: Ship fitting overview
	_ship_info_container = VBoxContainer.new()
	_ship_info_container.add_theme_constant_override("separation", 4)
	_tab_ship.add_child(_ship_info_container)

	# GATE.S4.UI_INDU.UPGRADE.001: Refit section
	_refit_container = VBoxContainer.new()
	_refit_container.add_theme_constant_override("separation", 4)
	_tab_ship.add_child(_refit_container)

	# GATE.S4.UI_INDU.MAINT.001: Maintenance section
	_maint_container = VBoxContainer.new()
	_maint_container.add_theme_constant_override("separation", 4)
	_tab_ship.add_child(_maint_container)

	# Tab 4: Station (research, construction)
	_tab_station = VBoxContainer.new()
	_tab_station.add_theme_constant_override("separation", 4)
	vbox.add_child(_tab_station)

	# GATE.S18.EMPIRE_DASH.STATION_TAB.001: Station info (health, production, services)
	_station_info_container = VBoxContainer.new()
	_station_info_container.add_theme_constant_override("separation", 4)
	_tab_station.add_child(_station_info_container)

	# GATE.S4.UI_INDU.RESEARCH.001: Research section
	_research_container = VBoxContainer.new()
	_research_container.add_theme_constant_override("separation", 4)
	_tab_station.add_child(_research_container)

	# GATE.S4.CONSTR_PROG.UI.001: Construction section
	_construction_container = VBoxContainer.new()
	_construction_container.add_theme_constant_override("separation", 4)
	_tab_station.add_child(_construction_container)

	# Tab 5: Intel (trade routes, automation, encounters)
	_tab_intel = VBoxContainer.new()
	_tab_intel.add_theme_constant_override("separation", 4)
	vbox.add_child(_tab_intel)

	# GATE.S10.TRADE_INTEL.DOCK_UI.001: Trade Routes section
	_trade_routes_container = VBoxContainer.new()
	_trade_routes_container.add_theme_constant_override("separation", 4)
	_tab_intel.add_child(_trade_routes_container)

	# Automation section (renamed from Programs)
	_programs_container = VBoxContainer.new()
	_programs_container.add_theme_constant_override("separation", 4)
	_tab_intel.add_child(_programs_container)

	# GATE.S6.ANOMALY.ENCOUNTER_UI.001: Anomaly Encounters section
	_encounters_container = VBoxContainer.new()
	_encounters_container.add_theme_constant_override("separation", 4)
	_tab_intel.add_child(_encounters_container)

	# Undock button (always visible)
	var btn_undock = Button.new()
	btn_undock.text = "Undock"
	btn_undock.pressed.connect(_on_undock_pressed)
	vbox.add_child(btn_undock)

	# Start on Market tab
	_switch_dock_tab(0)

# GATE.S18.EMPIRE_DASH.DOCK_TABS.001: Switch between dock tabs
func _switch_dock_tab(idx: int) -> void:
	_active_dock_tab = idx
	_tab_market.visible = (idx == 0)
	_tab_jobs.visible = (idx == 1)
	_tab_ship.visible = (idx == 2)
	_tab_station.visible = (idx == 3)
	_tab_intel.visible = (idx == 4)
	for i in range(_tab_buttons.size()):
		_tab_buttons[i].button_pressed = (i == idx)

func open_market_v0(node_id: String) -> void:
	_market_node_id = node_id
	visible = true
	_switch_dock_tab(0)  # Always open on Market tab

	# GATE.S7.PLANET.UI.001: Detect planet vs station for title + info.
	var bridge = get_node_or_null("/root/SimBridge")
	var planet_info: Dictionary = {}
	if bridge and bridge.has_method("GetPlanetInfoV0"):
		planet_info = bridge.call("GetPlanetInfoV0", node_id)

	var is_planet: bool = planet_info.size() > 0 and planet_info.get("effective_landable", false)

	if _title_label:
		if is_planet:
			var pname: String = str(planet_info.get("display_name", node_id))
			_title_label.text = "PLANET: %s" % pname
		else:
			var station_name: String = node_id
			if bridge and bridge.has_method("GetNodeDisplayNameV0"):
				station_name = str(bridge.call("GetNodeDisplayNameV0", node_id))
			_title_label.text = "STATION: %s" % station_name

	# GATE.S13.DOCK.CONTEXT.001: Station context description from production
	if _context_label and bridge:
		var context_text: String = _build_station_context(bridge, node_id)
		if not context_text.is_empty():
			_context_label.text = context_text
			_context_label.visible = true
		else:
			_context_label.visible = false

	if _planet_info_label:
		if is_planet:
			var ptype: String = str(planet_info.get("planet_type", ""))
			var spec: String = str(planet_info.get("specialization", "None"))
			var grav: float = float(planet_info.get("gravity_bps", 5000)) / 5000.0
			var atmo: float = float(planet_info.get("atmosphere_bps", 5000)) / 5000.0
			var temp_bps: int = int(planet_info.get("temperature_bps", 5000))
			var temp_label: String = _temp_label_v0(temp_bps)
			_planet_info_label.text = "%s | %.1fg | %.0f%% atmo | %s | %s" % [
				ptype, grav, atmo * 100.0, temp_label, spec
			]
			_planet_info_label.visible = true
		else:
			_planet_info_label.visible = false

	# GATE.S5.SEC_LANES.UI.001: Security readout for docked location
	if _security_label and bridge and bridge.has_method("GetNodeSecurityBandV0"):
		var band: String = str(bridge.call("GetNodeSecurityBandV0", node_id))
		_security_label.text = "Security: %s" % band.to_upper()
		_security_label.visible = true
		_security_label.add_theme_color_override("font_color", UITheme.security_color(band))

	# GATE.S7.FACTION.UI_REPUTATION.001: Faction tariff & access display
	if bridge and bridge.has_method("GetTerritoryAccessV0"):
		var access: Dictionary = bridge.call("GetTerritoryAccessV0", node_id)
		var faction_id: String = str(access.get("faction_id", ""))
		var can_trade: bool = bool(access.get("can_trade", true))
		var tariff_bps: int = int(access.get("tariff_bps", 0))
		var trade_policy: String = str(access.get("trade_policy", "Open"))
		var rep_tier: String = str(access.get("rep_tier", "Neutral"))
		var price_mod_bps: int = int(access.get("price_modifier_bps", 0))

		if _tariff_label:
			if not faction_id.is_empty() and tariff_bps > 0:
				var tariff_pct: float = tariff_bps / 100.0
				_tariff_label.text = "Tariff: %.1f%% (%s — %s)" % [tariff_pct, faction_id, trade_policy]
				_tariff_label.visible = true
			else:
				_tariff_label.visible = false

		# GATE.S7.REPUTATION.UI_INDICATORS.001: Rep tier + price modifier
		if _rep_tier_label:
			if not faction_id.is_empty():
				var tier_color: Color = _rep_tier_color(rep_tier)
				_rep_tier_label.add_theme_color_override("font_color", tier_color)
				if price_mod_bps != 0:
					var pct: float = price_mod_bps / 100.0
					var sign: String = "+" if price_mod_bps > 0 else ""
					_rep_tier_label.text = "Standing: %s (%s%s%% prices)" % [rep_tier, sign, "%.0f" % pct]
				else:
					_rep_tier_label.text = "Standing: %s" % rep_tier
				_rep_tier_label.visible = true
			else:
				_rep_tier_label.visible = false

		if _access_denied_label:
			_access_denied_label.visible = not can_trade
	else:
		if _tariff_label:
			_tariff_label.visible = false
		if _rep_tier_label:
			_rep_tier_label.visible = false
		if _access_denied_label:
			_access_denied_label.visible = false

	_rebuild_rows()

# GATE.S13.DOCK.CONTEXT.001: Build station context from production sites
func _build_station_context(bridge, node_id: String) -> String:
	if not bridge.has_method("GetNodeIndustryV0"):
		return ""
	var industry: Array = bridge.call("GetNodeIndustryV0", node_id)
	if industry.size() == 0:
		return ""
	var outputs: Array = []
	for site_info in industry:
		if typeof(site_info) != TYPE_DICTIONARY:
			continue
		var site_outputs: Array = site_info.get("outputs", [])
		for out_good in site_outputs:
			var formatted: String = _format_display_name(str(out_good))
			if formatted not in outputs:
				outputs.append(formatted)
	if outputs.size() == 0:
		return ""
	return "Produces: %s" % ", ".join(outputs)

func _temp_label_v0(temp_bps: int) -> String:
	if temp_bps < 2000: return "Frozen"
	if temp_bps < 4000: return "Cold"
	if temp_bps < 6000: return "Temperate"
	if temp_bps < 8000: return "Hot"
	return "Scorching"

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

# GATE.S12.UX_POLISH.QUANTITY.001: Quantity-aware buy/sell helpers
func _buy_qty_v0(good_id: String, qty: int) -> void:
	if _market_node_id.is_empty() or good_id.is_empty() or qty <= 0:
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, qty, true)
		# GATE.S12.UX_POLISH.TRADE_FEEDBACK.001: toast on buy
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Bought %d x %s" % [qty, _format_display_name(good_id)], 2.0)
		_rebuild_rows()

func _sell_qty_v0(good_id: String, qty: int) -> void:
	if _market_node_id.is_empty() or good_id.is_empty() or qty <= 0:
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("DispatchPlayerTradeV0"):
		bridge.call("DispatchPlayerTradeV0", _market_node_id, good_id, qty, false)
		# GATE.S12.UX_POLISH.TRADE_FEEDBACK.001: toast on sell
		var toast_mgr = get_node_or_null("/root/ToastManager")
		if toast_mgr and toast_mgr.has_method("show_toast"):
			toast_mgr.call("show_toast", "Sold %d x %s" % [qty, _format_display_name(good_id)], 2.0)
		_rebuild_rows()

func _max_buy_v0(_good_id: String, buy_price: int) -> int:
	if buy_price <= 0:
		return 0
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return 0
	var credits: int = 0
	if bridge.has_method("GetPlayerStateV0"):
		var state: Dictionary = bridge.call("GetPlayerStateV0")
		credits = int(state.get("credits", 0))
	# Max units = floor(credits / buy_price). Bridge enforces actual limits server-side.
	@warning_ignore("integer_division")
	return credits / buy_price

func _max_sell_v0(good_id: String) -> int:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return 0
	if bridge.has_method("GetPlayerCargoV0"):
		var cargo: Array = bridge.call("GetPlayerCargoV0")
		for item in cargo:
			if typeof(item) == TYPE_DICTIONARY and str(item.get("good_id", "")) == good_id:
				return int(item.get("qty", 0))
	return 0

func _on_buy_max_v0(good_id: String, buy_price: int) -> void:
	var qty: int = _max_buy_v0(good_id, buy_price)
	if qty > 0:
		_buy_qty_v0(good_id, qty)

func _on_sell_max_v0(good_id: String) -> void:
	var qty: int = _max_sell_v0(good_id)
	if qty > 0:
		_sell_qty_v0(good_id, qty)

func buy_one_v0(good_id: String) -> void:
	_buy_qty_v0(good_id, 1)

func sell_one_v0(good_id: String) -> void:
	_sell_qty_v0(good_id, 1)

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

		# GATE.S7.TERRITORY.EMBARGO_UI.001: Check if good is embargoed at this node
		var _bridge_ref = get_node_or_null("/root/SimBridge")
		var is_embargoed: bool = false
		if _bridge_ref and _bridge_ref.has_method("IsGoodEmbargoedV0"):
			is_embargoed = bool(_bridge_ref.call("IsGoodEmbargoedV0", _market_node_id, good_id))

		var row = HBoxContainer.new()

		var lbl_id = Label.new()
		if is_embargoed:
			lbl_id.text = _format_display_name(good_id) + " [EMBARGOED]"
			lbl_id.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5, 0.7))
		else:
			lbl_id.text = _format_display_name(good_id)
		lbl_id.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl_id)

		var lbl_buy = Label.new()
		lbl_buy.text = "%d cr" % buy_price
		lbl_buy.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		if is_embargoed:
			lbl_buy.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5, 0.7))
		row.add_child(lbl_buy)

		var lbl_sell = Label.new()
		lbl_sell.text = "%d cr" % sell_price
		lbl_sell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		if is_embargoed:
			lbl_sell.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5, 0.7))
		row.add_child(lbl_sell)

		# GATE.S12.UX_POLISH.QUANTITY.001: [1, 5, Max] buy/sell quantity buttons
		var buy_box = HBoxContainer.new()
		buy_box.add_theme_constant_override("separation", 2)
		for bq in [1, 5, -1]:
			var btn_b = Button.new()
			if bq == -1:
				btn_b.text = "Max"
				btn_b.pressed.connect(_on_buy_max_v0.bind(good_id, buy_price))
			else:
				btn_b.text = str(bq)
				btn_b.pressed.connect(_buy_qty_v0.bind(good_id, bq))
			btn_b.custom_minimum_size.x = 36
			# GATE.S7.TERRITORY.EMBARGO_UI.001: Disable buy for embargoed goods
			if is_embargoed:
				btn_b.disabled = true
			buy_box.add_child(btn_b)
		row.add_child(buy_box)

		var sell_box = HBoxContainer.new()
		sell_box.add_theme_constant_override("separation", 2)
		for sq in [1, 5, -1]:
			var btn_s = Button.new()
			if sq == -1:
				btn_s.text = "Max"
				btn_s.pressed.connect(_on_sell_max_v0.bind(good_id))
			else:
				btn_s.text = str(sq)
				btn_s.pressed.connect(_sell_qty_v0.bind(good_id, sq))
			btn_s.custom_minimum_size.x = 36
			sell_box.add_child(btn_s)
		row.add_child(sell_box)

		# GATE.S9.UI.TOOLTIP_DOCK.001: add tooltip to good name label
		_attach_tooltip_v0(lbl_id, "%s\nBuy: %d cr | Sell: %d cr | Stock: %d" % [_format_display_name(good_id), buy_price, sell_price, stock])

		_rows_container.add_child(row)

	# GATE.S4.INDU_STRUCT.PLAYABLE_VIEW.001: show production info for this node
	_rebuild_production_info()
	_rebuild_station_info()
	_rebuild_ship_info()
	_rebuild_missions()
	_rebuild_research()
	_rebuild_refit()
	_rebuild_maintenance()
	_rebuild_construction()
	_rebuild_trade_routes()
	_rebuild_programs()
	_rebuild_encounters()

	# GATE.S12.UX_POLISH.CARGO_DISPLAY.001: Update cargo summary with total item count
	if _cargo_label:
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("GetPlayerCargoV0"):
			var cargo = bridge.call("GetPlayerCargoV0")
			if typeof(cargo) == TYPE_ARRAY and cargo.size() > 0:
				var parts: Array = []
				var total_count: int = 0
				for item in cargo:
					if typeof(item) == TYPE_DICTIONARY:
						var qty: int = int(item.get("qty", 0))
						total_count += qty
						parts.append("%s x%d" % [_format_display_name(str(item.get("good_id", ""))), qty])
				if parts.size() > 0:
					_cargo_label.text = "Cargo: %d items — %s" % [total_count, ", ".join(parts)]
				else:
					_cargo_label.text = "Cargo: empty"
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
	header.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_rows_container.add_child(header)

	for site_info in industry:
		if typeof(site_info) != TYPE_DICTIONARY:
			continue
		var site_id: String = str(site_info.get("site_id", ""))
		# GATE.S7.PRODUCTION.BRIDGE_READOUT.001: Use recipe_name display name.
		var recipe_name: String = str(site_info.get("recipe_name", ""))
		if recipe_name.is_empty():
			recipe_name = str(site_info.get("recipe_id", "(natural)"))
		var eff_pct: int = int(site_info.get("efficiency_pct", 0))
		var health_pct: int = int(site_info.get("health_pct", 0))
		var inputs: Array = site_info.get("inputs", [])
		var outputs: Array = site_info.get("outputs", [])
		var in_str: String = ", ".join(inputs) if inputs.size() > 0 else ""
		var out_str: String = ", ".join(outputs) if outputs.size() > 0 else "none"
		var source: String = _site_source_tag(site_id)

		var row = HBoxContainer.new()
		var lbl = Label.new()
		if in_str.is_empty():
			lbl.text = "%s%s  eff:%d%%  hp:%d%%  -> %s" % [source, recipe_name, eff_pct, health_pct, out_str]
		else:
			lbl.text = "%s%s  [%s] eff:%d%%  hp:%d%%  -> %s" % [source, recipe_name, in_str, eff_pct, health_pct, out_str]
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl)
		_rows_container.add_child(row)

# GATE.S18.EMPIRE_DASH.STATION_TAB.001: Station info panel (health, production, services)
# GATE.S18.EMPIRE_DASH.SHIP_TAB.001: Ship fitting overview in dock Ship tab.
func _rebuild_ship_info() -> void:
	if _ship_info_container == null:
		return
	for child in _ship_info_container.get_children():
		_ship_info_container.remove_child(child)
		child.queue_free()
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetPlayerShipFittingV0"):
		_ship_info_container.visible = false
		return

	var fit: Dictionary = bridge.call("GetPlayerShipFittingV0")
	if fit.is_empty():
		_ship_info_container.visible = false
		return

	# Ship class header
	var class_lbl = Label.new()
	class_lbl.text = "SHIP: %s" % str(fit.get("ship_class", "Unknown"))
	class_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	class_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	_ship_info_container.add_child(class_lbl)

	# Hull / Shield row
	var hp_lbl = Label.new()
	hp_lbl.text = "Hull: %d/%d   Shield: %d/%d" % [
		int(fit.get("hull", 0)), int(fit.get("hull_max", 0)),
		int(fit.get("shield", 0)), int(fit.get("shield_max", 0))]
	hp_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
	_ship_info_container.add_child(hp_lbl)

	# Fitting budget
	var power_used: int = int(fit.get("power_used", 0))
	var power_max: int = int(fit.get("power_max", 0))
	var slots_filled: int = int(fit.get("slots_filled", 0))
	var slot_count: int = int(fit.get("slot_count", 0))
	var budget_lbl = Label.new()
	budget_lbl.text = "Slots: %d/%d   Power: %d/%d" % [slots_filled, slot_count, power_used, power_max]
	if power_used > power_max:
		budget_lbl.add_theme_color_override("font_color", UITheme.RED)
	elif power_used > power_max * 0.8:
		budget_lbl.add_theme_color_override("font_color", UITheme.TEXT_WARNING)
	_ship_info_container.add_child(budget_lbl)

	# GATE.S7.POWER.BRIDGE_UI.001: Power budget bar.
	var power_bar = ProgressBar.new()
	power_bar.min_value = 0
	power_bar.max_value = maxi(power_max, 1)
	power_bar.value = mini(power_used, power_max)
	power_bar.show_percentage = false
	power_bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	power_bar.custom_minimum_size.y = 10
	_ship_info_container.add_child(power_bar)
	if power_used > power_max:
		var warn_lbl = Label.new()
		warn_lbl.text = "OVER BUDGET — modules disabled"
		warn_lbl.add_theme_color_override("font_color", UITheme.RED)
		warn_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		_ship_info_container.add_child(warn_lbl)

	# Zone armor diagram (4 facing bars)
	var zone_header = Label.new()
	zone_header.text = "ZONE ARMOR"
	zone_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	zone_header.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
	zone_header.add_theme_color_override("font_color", UITheme.TEXT_INFO)
	_ship_info_container.add_child(zone_header)

	var zones: Array = [
		["Fore", "zone_fore", "zone_fore_max"],
		["Port", "zone_port", "zone_port_max"],
		["Stbd", "zone_stbd", "zone_stbd_max"],
		["Aft",  "zone_aft",  "zone_aft_max"],
	]
	for z in zones:
		var zname: String = z[0]
		var zhp: int = int(fit.get(z[1], 0))
		var zmax: int = int(fit.get(z[2], 0))
		var row = HBoxContainer.new()
		var name_lbl = Label.new()
		name_lbl.text = "  %s:" % zname
		name_lbl.custom_minimum_size.x = 50
		row.add_child(name_lbl)
		# HP bar using ProgressBar
		var bar = ProgressBar.new()
		bar.min_value = 0
		bar.max_value = maxi(zmax, 1)
		bar.value = zhp
		bar.show_percentage = false
		bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		bar.custom_minimum_size.y = 14
		row.add_child(bar)
		var val_lbl = Label.new()
		val_lbl.text = " %d/%d" % [zhp, zmax]
		val_lbl.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
		row.add_child(val_lbl)
		_ship_info_container.add_child(row)

	# Installed modules list
	if bridge.has_method("GetPlayerFleetSlotsV0"):
		var slots: Array = bridge.call("GetPlayerFleetSlotsV0")
		if slots.size() > 0:
			var mod_header = Label.new()
			mod_header.text = "INSTALLED MODULES"
			mod_header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			mod_header.add_theme_font_size_override("font_size", UITheme.FONT_SMALL)
			mod_header.add_theme_color_override("font_color", UITheme.TEXT_INFO)
			_ship_info_container.add_child(mod_header)
			for slot in slots:
				if typeof(slot) != TYPE_DICTIONARY:
					continue
				var kind: String = str(slot.get("slot_kind", ""))
				var mod_id: String = str(slot.get("installed_module_id", ""))
				# GATE.S7.POWER.BRIDGE_UI.001: condition + power_draw + disabled per module.
				var condition: int = int(slot.get("condition", 100))
				var pwr_draw: int = int(slot.get("power_draw", 0))
				var is_disabled: bool = slot.get("disabled", false)
				var disp_name: String = str(slot.get("display_name", ""))
				var row = HBoxContainer.new()
				var kind_lbl = Label.new()
				kind_lbl.text = "  [%s]" % kind
				kind_lbl.custom_minimum_size.x = 80
				kind_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
				row.add_child(kind_lbl)
				var mod_lbl = Label.new()
				if mod_id.is_empty():
					mod_lbl.text = "Empty"
					mod_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
				else:
					var name_str: String = disp_name if not disp_name.is_empty() else _format_display_name(mod_id)
					var suffix := ""
					if pwr_draw > 0:
						suffix += "  P:%d" % pwr_draw
					suffix += "  C:%d%%" % condition
					if is_disabled:
						suffix += "  [OFF]"
					mod_lbl.text = name_str + suffix
					# Condition color gradient: green -> yellow -> red.
					if is_disabled:
						mod_lbl.add_theme_color_override("font_color", UITheme.RED)
					elif condition < 30:
						mod_lbl.add_theme_color_override("font_color", UITheme.RED)
					elif condition < 60:
						mod_lbl.add_theme_color_override("font_color", UITheme.ORANGE)
					# else default color (green/white)
				row.add_child(mod_lbl)
				_ship_info_container.add_child(row)

	_ship_info_container.visible = true

func _rebuild_station_info() -> void:
	if _station_info_container == null:
		return
	for child in _station_info_container.get_children():
		_station_info_container.remove_child(child)
		child.queue_free()
	if _market_node_id.is_empty():
		_station_info_container.visible = false
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		_station_info_container.visible = false
		return

	# Station health from sustainment
	var has_content: bool = false
	if bridge.has_method("GetSustainmentSnapshot"):
		var sites: Array = bridge.call("GetSustainmentSnapshot", _market_node_id)
		if sites.size() > 0:
			has_content = true
			_add_section_header(_station_info_container, "station_info", "STATION HEALTH")
			for site in sites:
				if typeof(site) != TYPE_DICTIONARY:
					continue
				var site_id: String = str(site.get("site_id", ""))
				var health_bps: int = int(site.get("health_bps", 0))
				var eff_bps: int = int(site.get("eff_bps_now", 0))
				@warning_ignore("integer_division")
				var health_pct: int = health_bps / 100
				@warning_ignore("integer_division")
				var eff_pct: int = eff_bps / 100
				var source: String = _site_source_tag(site_id)
				var row = HBoxContainer.new()
				var lbl = Label.new()
				lbl.text = "%s HP: %d%%  Eff: %d%%" % [source, health_pct, eff_pct]
				lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				if health_pct < 50:
					lbl.add_theme_color_override("font_color", UITheme.TEXT_WARNING)
				row.add_child(lbl)
				_station_info_container.add_child(row)

	# Local production from GetNodeIndustryV0
	if bridge.has_method("GetNodeIndustryV0"):
		var industry: Array = bridge.call("GetNodeIndustryV0", _market_node_id)
		if industry.size() > 0:
			has_content = true
			_add_section_header(_station_info_container, "station_prod", "LOCAL PRODUCTION")
			for site_info in industry:
				if typeof(site_info) != TYPE_DICTIONARY:
					continue
				# GATE.S7.PRODUCTION.BRIDGE_READOUT.001: Use recipe_name + inputs display names.
				var recipe_name: String = str(site_info.get("recipe_name", ""))
				if recipe_name.is_empty():
					recipe_name = str(site_info.get("recipe_id", ""))
				var eff_pct: int = int(site_info.get("efficiency_pct", 0))
				var inputs: Array = site_info.get("inputs", [])
				var outputs: Array = site_info.get("outputs", [])
				var out_str: String = ", ".join(outputs) if outputs.size() > 0 else "none"
				var row = HBoxContainer.new()
				var lbl = Label.new()
				if inputs.size() > 0:
					var in_str: String = ", ".join(inputs)
					lbl.text = "%s  [%s] Eff: %d%%  -> %s" % [recipe_name, in_str, eff_pct, out_str]
				else:
					lbl.text = "%s  Eff: %d%%  -> %s" % [recipe_name, eff_pct, out_str]
				lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				row.add_child(lbl)
				_station_info_container.add_child(row)

	# Installed services summary
	var services: Array = []
	if bridge.has_method("GetResearchSnapshotV0"):
		services.append("Research")
	if bridge.has_method("GetConstructionSnapshotV0"):
		services.append("Construction")
	if bridge.has_method("GetRefitSnapshotV0"):
		services.append("Refit")
	if bridge.has_method("GetMaintenanceSnapshotV0"):
		services.append("Maintenance")
	if services.size() > 0:
		has_content = true
		var svc_lbl = Label.new()
		svc_lbl.text = "Services: %s" % ", ".join(services)
		svc_lbl.add_theme_color_override("font_color", UITheme.TEXT_SECONDARY)
		_station_info_container.add_child(svc_lbl)

	_station_info_container.visible = has_content

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
			hdr.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
			_missions_container.add_child(hdr)

			var title_lbl = Label.new()
			title_lbl.text = str(active.get("title", active_id))
			title_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
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
			var empty_lbl = Label.new()
			empty_lbl.text = "No missions available at this station."
			empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
			_missions_container.add_child(empty_lbl)
			return

		var hdr = Label.new()
		hdr.text = "AVAILABLE MISSIONS"
		hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		hdr.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
		_missions_container.add_child(hdr)

		for m in missions:
			if typeof(m) != TYPE_DICTIONARY:
				continue
			var mid: String = str(m.get("mission_id", ""))
			var mtitle: String = str(m.get("title", mid))
			var _mdesc: String = str(m.get("description", ""))
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

	# GATE.S13.DOCK.HIDE_EMPTY.001: Hide research if no techs available
	var has_content: bool = false
	if bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = bridge.call("GetResearchStatusV0")
		if bool(status.get("researching", false)):
			has_content = true
	if not has_content and bridge.has_method("GetTechTreeV0"):
		var techs: Array = bridge.call("GetTechTreeV0")
		for t in techs:
			if typeof(t) == TYPE_DICTIONARY and not bool(t.get("unlocked", false)):
				has_content = true
				break
	if not has_content:
		_research_container.visible = false
		return
	_research_container.visible = true

	_add_section_header(_research_container, "research", "RESEARCH")
	if _collapsed.get("research", false):
		return

	# GATE.S4.TECH_INDUSTRIALIZE.BRIDGE_DEPTH.001: Show tech tier
	if bridge.has_method("GetTechTierV0"):
		var tier_lbl = Label.new()
		tier_lbl.text = "Tech Level: %d" % bridge.call("GetTechTierV0")
		tier_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		tier_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		_research_container.add_child(tier_lbl)

	# Show current research status
	if bridge.has_method("GetResearchStatusV0"):
		var status: Dictionary = bridge.call("GetResearchStatusV0")
		var researching: bool = bool(status.get("researching", false))
		if researching:

			var tech_id: String = str(status.get("tech_id", ""))
			var progress: int = int(status.get("progress_pct", 0))
			var lbl = Label.new()
			lbl.text = "Researching: %s (%d%%)" % [tech_id, progress]
			_research_container.add_child(lbl)

			# GATE.S10.TRADE_INTEL.DOCK_UI.001: Sustain/stall status
			var stall_reason: String = str(status.get("stall_reason", ""))
			if not stall_reason.is_empty():
				var stall_lbl = Label.new()
				stall_lbl.text = "STALLED: %s" % stall_reason
				stall_lbl.add_theme_color_override("font_color", UITheme.RED)
				stall_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
				_research_container.add_child(stall_lbl)

			var research_node: String = str(status.get("research_node_id", ""))
			if not research_node.is_empty():
				var node_lbl = Label.new()
				node_lbl.text = "Research Node: %s" % research_node
				_research_container.add_child(node_lbl)

			return

	# Show available techs to research
	if bridge.has_method("GetTechTreeV0"):
		var techs: Array = bridge.call("GetTechTreeV0")
		var available: Array = []
		var locked: Array = []
		for t in techs:
			if typeof(t) != TYPE_DICTIONARY:
				continue
			if bool(t.get("unlocked", false)):
				continue
			if bool(t.get("prerequisites_met", false)):
				available.append(t)
			else:
				locked.append(t)

		if available.size() == 0 and locked.size() == 0:
			return

		for t in available:
			var tid: String = str(t.get("tech_id", ""))
			var tname: String = str(t.get("display_name", tid))
			var tcost: int = int(t.get("credit_cost", 0))
			var row = HBoxContainer.new()
			var info_lbl = Label.new()
			info_lbl.text = "%s (%d cr)" % [tname, tcost]
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			row.add_child(info_lbl)

			# GATE.S4.UI_INDU.WHY_BLOCKED.001: Show block reason if research is blocked
			if bridge.has_method("GetResearchBlockReasonV0"):
				var block_reason: String = str(bridge.call("GetResearchBlockReasonV0", tid))
				if not block_reason.is_empty():
					row.add_child(_make_block_label(_format_block_reason(block_reason)))
				else:
					var btn = Button.new()
					btn.text = "Research"
					btn.pressed.connect(_on_start_research.bind(tid))
					row.add_child(btn)
			else:
				var btn = Button.new()
				btn.text = "Research"
				btn.pressed.connect(_on_start_research.bind(tid))
				row.add_child(btn)
			_research_container.add_child(row)

		# GATE.S4.UI_INDU.WHY_BLOCKED.001: Show locked techs with block reasons
		if locked.size() > 0 and bridge.has_method("GetResearchBlockReasonV0"):
			for t in locked:
				var tid: String = str(t.get("tech_id", ""))
				var tname: String = str(t.get("display_name", tid))
				var block_reason: String = str(bridge.call("GetResearchBlockReasonV0", tid))
				var row = HBoxContainer.new()
				var info_lbl = Label.new()
				info_lbl.text = "%s (locked)" % tname
				info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				info_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
				row.add_child(info_lbl)
				if not block_reason.is_empty():
					row.add_child(_make_block_label(_format_block_reason(block_reason)))
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
		_refit_container.visible = false
		return

	var slots: Array = bridge.call("GetPlayerFleetSlotsV0")
	if slots.size() == 0:
		# GATE.S13.DOCK.HIDE_EMPTY.001: Hide refit if no slots
		_refit_container.visible = false
		return
	_refit_container.visible = true

	_add_section_header(_refit_container, "refit", "REFIT")
	if _collapsed.get("refit", false):
		return

	# GATE.S4.UPGRADE_PIPELINE.BRIDGE_QUEUE.001: Show refit queue status
	if bridge.has_method("GetRefitProgressV0"):
		var refit_progress: Dictionary = bridge.call("GetRefitProgressV0", "fleet_trader_1")
		if refit_progress.get("queue_size", 0) > 0:
			var refit_lbl = Label.new()
			refit_lbl.text = "Refitting: %s (%d ticks left)" % [refit_progress.get("current_module", ""), refit_progress.get("ticks_remaining", 0)]
			refit_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
			_refit_container.add_child(refit_lbl)

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
	var locked_modules: Array = []
	for m in modules:
		if typeof(m) != TYPE_DICTIONARY:
			continue
		if bool(m.get("can_install", false)):
			installable.append(m)
		else:
			locked_modules.append(m)

	if installable.size() > 0:
		var sub_hdr = Label.new()
		sub_hdr.text = "Available Modules:"
		sub_hdr.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
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

			# GATE.S4.UI_INDU.WHY_BLOCKED.001: Check block reason before showing install
			var refit_blocked: String = ""
			if bridge.has_method("GetRefitBlockReasonV0"):
				refit_blocked = str(bridge.call("GetRefitBlockReasonV0", "fleet_trader_1", mid))

			if not refit_blocked.is_empty():
				row.add_child(_make_block_label(_format_block_reason(refit_blocked)))
			else:
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

	# GATE.S4.UI_INDU.WHY_BLOCKED.001: Show locked modules with block reasons
	if locked_modules.size() > 0 and bridge.has_method("GetRefitBlockReasonV0"):
		var locked_hdr = Label.new()
		locked_hdr.text = "Locked Modules:"
		locked_hdr.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		locked_hdr.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_refit_container.add_child(locked_hdr)

		for m in locked_modules:
			var mid: String = str(m.get("module_id", ""))
			var mname: String = str(m.get("display_name", mid))
			var mkind: String = str(m.get("slot_kind", ""))
			var block_reason: String = str(bridge.call("GetRefitBlockReasonV0", "fleet_trader_1", mid))
			var row = HBoxContainer.new()
			var info_lbl = Label.new()
			info_lbl.text = "%s [%s]" % [mname, mkind]
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			info_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
			row.add_child(info_lbl)
			if not block_reason.is_empty():
				row.add_child(_make_block_label(_format_block_reason(block_reason)))
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
		_maint_container.visible = false
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetNodeMaintenanceV0"):
		_maint_container.visible = false
		return

	var sites: Array = bridge.call("GetNodeMaintenanceV0", _market_node_id)
	var damaged: Array = []
	for s in sites:
		if typeof(s) != TYPE_DICTIONARY:
			continue
		if bool(s.get("needs_repair", false)):
			damaged.append(s)

	if damaged.size() == 0:
		# GATE.S13.DOCK.HIDE_EMPTY.001: Hide maintenance if no damaged sites
		_maint_container.visible = false
		return
	_maint_container.visible = true

	_add_section_header(_maint_container, "maintenance", "MAINTENANCE")
	if _collapsed.get("maintenance", false):
		return

	# GATE.S4.MAINT_SUSTAIN.BRIDGE_SUPPLY.001: Show supply level
	if bridge.has_method("GetSupplyLevelV0"):
		var supply: Dictionary = bridge.call("GetSupplyLevelV0", _market_node_id)
		if supply.size() > 0:
			var supply_lbl = Label.new()
			supply_lbl.text = "Supply: %d/%d" % [supply.get("supply_level", 0), supply.get("max_supply", 100)]
			supply_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
			_maint_container.add_child(supply_lbl)

	for s in damaged:
		var sid: String = str(s.get("site_id", ""))
		var recipe: String = str(s.get("recipe_id", ""))
		var hp: int = int(s.get("health_pct", 0))
		var eff: int = int(s.get("efficiency_pct", 0))
		var cost: int = int(s.get("repair_cost", 0))

		var source: String = _site_source_tag(sid)
		var row = HBoxContainer.new()
		var lbl = Label.new()
		lbl.text = "%s%s hp:%d%% eff:%d%%" % [source, recipe if not recipe.is_empty() else sid, hp, eff]
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(lbl)

		# GATE.S4.UI_INDU.WHY_BLOCKED.001: Show block reason or repair button
		var repair_blocked: String = ""
		if bridge.has_method("GetRepairBlockReasonV0"):
			repair_blocked = str(bridge.call("GetRepairBlockReasonV0", sid))

		if not repair_blocked.is_empty():
			row.add_child(_make_block_label(_format_block_reason(repair_blocked)))
		else:
			var btn = Button.new()
			btn.text = "Repair (%d cr)" % cost
			btn.pressed.connect(_on_repair_site.bind(sid))
			row.add_child(btn)
		_maint_container.add_child(row)

# GATE.S4.UI_INDU.WHY_BLOCKED.001: Format block reason strings for display
func _format_block_reason(reason: String) -> String:
	if reason.begins_with("missing_prereq:"):
		return "Requires: %s" % reason.substr(len("missing_prereq:"))
	elif reason.begins_with("missing_tech:"):
		return "Requires: %s" % reason.substr(len("missing_tech:"))
	elif reason.begins_with("tier_locked:"):
		return "Tech Level too low (Tier %s)" % reason.substr(len("tier_locked:"))
	elif reason.begins_with("insufficient_credits:"):
		return "Need %s cr" % reason.substr(len("insufficient_credits:"))
	elif reason == "already_researching":
		return "Already researching"
	elif reason == "already_unlocked":
		return "Already unlocked"
	elif reason == "already_full_health":
		return "Full health"
	elif reason == "no_supply":
		return "No supply available"
	elif reason == "unknown_fleet":
		return "Fleet not found"
	elif reason == "unknown_site":
		return "Site not found"
	elif reason == "max_total_projects":
		return "Max projects reached"
	elif reason == "max_projects_at_node":
		return "Node at capacity"
	elif reason == "prerequisites_not_met":
		return "Prerequisites needed"
	elif reason == "insufficient_credits":
		return "Not enough credits"
	return reason

func _add_section_header(container: VBoxContainer, key: String, title: String) -> void:
	var is_collapsed: bool = _collapsed.get(key, false)
	var btn = Button.new()
	btn.text = "%s  %s" % [title, "[+]" if is_collapsed else "[-]"]
	btn.flat = true
	btn.add_theme_font_size_override("font_size", UITheme.FONT_SECTION)
	btn.alignment = HORIZONTAL_ALIGNMENT_CENTER
	btn.pressed.connect(_toggle_section.bind(key))
	container.add_child(btn)

func _toggle_section(key: String) -> void:
	_collapsed[key] = not _collapsed.get(key, false)
	_rebuild_rows()

func _site_source_tag(site_id: String) -> String:
	if site_id.begins_with("planet_"): return "[Planet] "
	if site_id.begins_with("fac_"): return "[Factory] "
	if site_id.begins_with("forge_"): return "[Forge] "
	if site_id.begins_with("well_"): return "[Well] "
	if site_id.begins_with("mine_"): return "[Mine] "
	return ""

func _make_block_label(text: String) -> Label:
	var lbl = Label.new()
	lbl.text = text
	lbl.add_theme_color_override("font_color", UITheme.ORANGE)
	lbl.clip_text = true
	lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return lbl

# GATE.S7.REPUTATION.UI_INDICATORS.001: Color by rep tier
func _rep_tier_color(tier: String) -> Color:
	match tier:
		"Allied": return Color(0.2, 0.8, 1.0)    # Cyan
		"Friendly": return Color(0.4, 1.0, 0.4)   # Green
		"Neutral": return Color(0.8, 0.8, 0.8)    # Light gray
		"Hostile": return Color(1.0, 0.6, 0.2)     # Orange
		"Enemy": return Color(1.0, 0.2, 0.2)       # Red
		_: return Color(0.8, 0.8, 0.8)

func _on_repair_site(site_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("RepairSiteV0"):
		bridge.call("RepairSiteV0", site_id)
		_rebuild_rows()

# GATE.S4.CONSTR_PROG.UI.001: Construction project panel
func _rebuild_construction() -> void:
	if _construction_container == null:
		return
	for child in _construction_container.get_children():
		_construction_container.remove_child(child)
		child.queue_free()

	if _market_node_id.is_empty():
		_construction_container.visible = false
		return
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		_construction_container.visible = false
		return

	# GATE.S13.DOCK.HIDE_EMPTY.001: Hide construction unless there are defs or active projects
	var has_content: bool = false
	if bridge.has_method("GetConstructionProjectsV0"):
		var projects: Array = bridge.call("GetConstructionProjectsV0")
		for p in projects:
			if typeof(p) == TYPE_DICTIONARY and str(p.get("node_id", "")) == _market_node_id and not bool(p.get("completed", true)):
				has_content = true
				break
	if not has_content and bridge.has_method("GetAvailableConstructionDefsV0"):
		var defs: Array = bridge.call("GetAvailableConstructionDefsV0")
		if defs.size() > 0:
			has_content = true
	if not has_content:
		_construction_container.visible = false
		return
	_construction_container.visible = true

	_add_section_header(_construction_container, "construction", "CONSTRUCTION")
	if _collapsed.get("construction", false):
		return

	# Show active construction projects
	var _has_active: bool = false
	if bridge.has_method("GetConstructionProjectsV0"):
		var projects: Array = bridge.call("GetConstructionProjectsV0")
		for p in projects:
			if typeof(p) != TYPE_DICTIONARY:
				continue
			if str(p.get("node_id", "")) != _market_node_id:
				continue
			if bool(p.get("completed", true)):
				continue
			_has_active = true
			var pid: String = str(p.get("project_def_id", ""))
			var pct: int = int(p.get("progress_pct", 0))
			var step: int = int(p.get("current_step", 0))
			var total: int = int(p.get("total_steps", 0))
			var row = HBoxContainer.new()
			var lbl = Label.new()
			lbl.text = "%s  Step %d/%d  (%d%%)" % [pid, step, total, pct]
			lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			row.add_child(lbl)
			_construction_container.add_child(row)

	# Show available construction defs to start
	if bridge.has_method("GetAvailableConstructionDefsV0") and bridge.has_method("GetConstructionBlockReasonV0"):
		var defs: Array = bridge.call("GetAvailableConstructionDefsV0")
		for d in defs:
			if typeof(d) != TYPE_DICTIONARY:
				continue
			var def_id: String = str(d.get("project_def_id", ""))
			var dname: String = str(d.get("display_name", def_id))
			var cost: int = int(d.get("credit_cost_per_step", 0))
			var prereqs_met: bool = bool(d.get("prerequisites_met", false))

			var block_reason: String = str(bridge.call("GetConstructionBlockReasonV0", def_id, _market_node_id))

			var row = HBoxContainer.new()
			var info_lbl = Label.new()
			info_lbl.text = "%s (%d cr/step)" % [dname, cost]
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			if not prereqs_met:
				info_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
			row.add_child(info_lbl)

			if not block_reason.is_empty():
				row.add_child(_make_block_label(_format_block_reason(block_reason)))
			else:
				var btn = Button.new()
				btn.text = "Build"
				btn.pressed.connect(_on_start_construction.bind(def_id))
				row.add_child(btn)
			_construction_container.add_child(row)

func _on_start_construction(project_def_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("StartConstructionV0"):
		bridge.call("StartConstructionV0", project_def_id, _market_node_id)
		_rebuild_rows()

# GATE.S10.TRADE_INTEL.DOCK_UI.001: Trade Routes UI panel
func _rebuild_trade_routes() -> void:
	if _trade_routes_container == null:
		return
	for child in _trade_routes_container.get_children():
		_trade_routes_container.remove_child(child)
		child.queue_free()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		_trade_routes_container.visible = false
		return

	# GATE.S13.DOCK.HIDE_EMPTY.001: Hide trade routes until scanner tech is researched
	if not bridge.has_method("GetScannerRangeV0"):
		_trade_routes_container.visible = false
		return
	var scan_range: int = int(bridge.call("GetScannerRangeV0"))
	if scan_range <= 0:
		_trade_routes_container.visible = false
		return
	_trade_routes_container.visible = true

	_add_section_header(_trade_routes_container, "trade_routes", "TRADE ROUTES")
	if _collapsed.get("trade_routes", false):
		return

	# Scanner range line
	var range_lbl = Label.new()
	range_lbl.text = "Scanner Range: %d hop(s)" % scan_range
	range_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_trade_routes_container.add_child(range_lbl)

	if not bridge.has_method("GetTradeRoutesV0"):
		return

	var routes: Array = bridge.call("GetTradeRoutesV0")

	if routes.size() == 0:
		var empty_lbl = Label.new()
		empty_lbl.text = "No routes discovered. Dock at stations to gather price intel."
		empty_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		empty_lbl.add_theme_color_override("font_color", UITheme.TEXT_DISABLED)
		_trade_routes_container.add_child(empty_lbl)
		return

	for route in routes:
		if typeof(route) != TYPE_DICTIONARY:
			continue

		var _route_id: String = str(route.get("route_id", ""))
		var source_node: String = str(route.get("source_node_id", ""))
		var dest_node: String = str(route.get("dest_node_id", ""))
		var good_id: String = str(route.get("good_id", ""))
		var profit: int = int(route.get("estimated_profit_per_unit", 0))
		var status: String = str(route.get("status", ""))

		var row = HBoxContainer.new()
		var info_lbl = Label.new()
		info_lbl.text = "Good: %s | %s -> %s | +%d/unit | %s" % [_format_display_name(good_id), _format_display_name(source_node), _format_display_name(dest_node), profit, status]
		info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		info_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(info_lbl)

		if status == "Active" or status == "Discovered":
			var btn = Button.new()
			btn.text = "Launch Charter"
			btn.pressed.connect(_on_launch_charter.bind(source_node, dest_node, good_id))
			row.add_child(btn)

		_trade_routes_container.add_child(row)

func _on_launch_charter(source_node_id: String, dest_node_id: String, good_id: String) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		return
	if not bridge.has_method("CreateTradeCharterProgram") or not bridge.has_method("StartProgram"):
		return
	var program_id: String = str(bridge.call("CreateTradeCharterProgram", source_node_id, dest_node_id, good_id, good_id, 10))
	if not program_id.is_empty():
		bridge.call("StartProgram", program_id)
	_rebuild_rows()

# GATE.S13.TERMINOLOGY.001: "Programs" renamed to "Automation"
func _rebuild_programs() -> void:
	if _programs_container == null:
		return
	for child in _programs_container.get_children():
		_programs_container.remove_child(child)
		child.queue_free()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null:
		_programs_container.visible = false
		return

	if not bridge.has_method("GetProgramExplainSnapshot"):
		_programs_container.visible = false
		return

	var programs: Array = bridge.call("GetProgramExplainSnapshot")
	# GATE.S13.DOCK.HIDE_EMPTY.001: Hide automation until first program exists
	if programs.size() == 0:
		_programs_container.visible = false
		return
	_programs_container.visible = true

	_add_section_header(_programs_container, "programs", "AUTOMATION")

	if _collapsed.get("programs", false):
		return

	for p in programs:
		if typeof(p) != TYPE_DICTIONARY:
			continue
		var pid: String = str(p.get("id", ""))
		var kind: String = str(p.get("kind", ""))
		var status: String = str(p.get("status", ""))
		var row = HBoxContainer.new()
		var lbl = Label.new()
		lbl.text = "%s (%s) — %s" % [_format_display_name(pid), kind, status]
		lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(lbl)
		_programs_container.add_child(row)

# GATE.S9.UI.TOOLTIP_DOCK.001: attach tooltip to a Control via built-in tooltip_text
func _attach_tooltip_v0(control: Control, text: String) -> void:
	control.tooltip_text = text

# GATE.S12.UX_POLISH.DISPLAY_NAMES.001: Convert snake_case IDs to readable display names via SimBridge.
func _format_display_name(id: String) -> String:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("FormatDisplayNameV0"):
		return str(bridge.call("FormatDisplayNameV0", id))
	# Fallback: simple capitalize with underscore-to-space
	return id.replace("_", " ").capitalize()

# GATE.S6.ANOMALY.ENCOUNTER_UI.001: Anomaly encounters panel in Services tab.
func _rebuild_encounters() -> void:
	if _encounters_container == null:
		return
	for child in _encounters_container.get_children():
		_encounters_container.remove_child(child)
		child.queue_free()

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetActiveEncountersV0"):
		_encounters_container.visible = false
		return

	var encounters: Array = bridge.call("GetActiveEncountersV0")
	# GATE.S13.DOCK.HIDE_EMPTY.001: Hide encounters until at least one is active
	if encounters.size() == 0:
		_encounters_container.visible = false
		return
	_encounters_container.visible = true

	_add_section_header(_encounters_container, "encounters", "ANOMALY ENCOUNTERS")
	if _collapsed.get("encounters", false):
		return

	for enc in encounters:
		if typeof(enc) != TYPE_DICTIONARY:
			continue
		var enc_id: String = str(enc.get("encounter_id", ""))
		var family: String = str(enc.get("family", ""))
		var difficulty: int = int(enc.get("difficulty", 0))
		var status: String = str(enc.get("status", ""))
		var loot_items: Array = enc.get("loot_items", [])
		var credit_reward: int = int(enc.get("credit_reward", 0))

		# Family icon
		var family_icon: String
		match family:
			"DERELICT": family_icon = "[D]"
			"RUIN":     family_icon = "[R]"
			"SIGNAL":   family_icon = "[S]"
			_:           family_icon = "[?]"

		# Loot preview (first item or credit reward)
		var loot_preview: String = ""
		if loot_items.size() > 0:
			loot_preview = str(loot_items[0])
		elif credit_reward > 0:
			loot_preview = "%d cr" % credit_reward

		var row = VBoxContainer.new()
		row.add_theme_constant_override("separation", 2)

		# Info label: icon | difficulty | loot | status
		var info_lbl = Label.new()
		info_lbl.text = "%s  Diff: %d  Loot: %s  [%s]" % [family_icon, difficulty, loot_preview, status]
		info_lbl.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
		info_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(info_lbl)

		# Engage button for Pending encounters
		if status == "Pending":
			var btn_engage = Button.new()
			btn_engage.text = "Engage"
			btn_engage.pressed.connect(_on_encounter_engage_v0.bind(enc_id))
			row.add_child(btn_engage)

		_encounters_container.add_child(row)

func _on_encounter_engage_v0(encounter_id: String) -> void:
	print("ENCOUNTER_ENGAGE|" + encounter_id)
