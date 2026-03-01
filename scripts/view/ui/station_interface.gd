extends Control

var manager_ref
var current_node_id: String

var panel: Panel
var lbl_wallet: Label
var lbl_heat: Label
var container: VBoxContainer

func _ready():
	anchor_right = 1.0
	anchor_bottom = 1.0
	visible = false

	panel = Panel.new()
	panel.anchors_preset = Control.PRESET_CENTER
	panel.custom_minimum_size = Vector2(800, 500) # WIDENED (Prev: 500)
	panel.position = Vector2(200, 50)
	add_child(panel)

	var title = Label.new()
	title.text = 'STATION MARKET TERMINAL'
	title.position = Vector2(10, 10)
	panel.add_child(title)

	lbl_wallet = Label.new()
	lbl_wallet.position = Vector2(10, 40)
	panel.add_child(lbl_wallet)

	lbl_heat = Label.new()
	lbl_heat.position = Vector2(400, 40)
	panel.add_child(lbl_heat)

	container = VBoxContainer.new()
	container.position = Vector2(10, 80)
	container.custom_minimum_size = Vector2(780, 400) # WIDENED
	panel.add_child(container)

func setup(p_manager, p_node_id: String):
	manager_ref = p_manager
	current_node_id = p_node_id

func _process(_delta):
	if visible and manager_ref:
		_refresh_header()

func _refresh_header():
	lbl_wallet.text = 'CREDITS: ' + str(manager_ref.player.credits)
	var heat = manager_ref.sim_ref.info.get_node_heat(current_node_id)
	lbl_heat.text = 'LOCAL HEAT: ' + str(snapped(heat, 0.01))

func refresh_market_list():
	for child in container.get_children():
		child.queue_free()

	var market = manager_ref.sim_ref.active_markets.get(current_node_id)
	if not market:
		return

	var market_title = Label.new()
	market_title.text = 'MARKET'
	market_title.custom_minimum_size = Vector2(780, 24)
	container.add_child(market_title)

	var item_ids = market.inventory.keys()
	item_ids.sort()

	for item_id in item_ids:
		var qty = market.inventory[item_id]
		var demand = market.base_demand.get(item_id, 10)
		var price = manager_ref.EconomyEngine.calculate_price(null, qty, demand)
		var player_qty = manager_ref.player.cargo.get(item_id, 0)

		var row = HBoxContainer.new()
		container.add_child(row)

		var info = Label.new()
		# FORMAT: ALIGN LEFT, WIDENED TO PREVENT OVERLAP
		info.text = '%s | QTY: %s | PRICE: %s | OWN: %s' % [item_id.to_upper(), qty, price, player_qty]
		info.custom_minimum_size = Vector2(500, 30) # WIDENED (Prev: 300)
		row.add_child(info)

		var btn_buy = Button.new()
		btn_buy.text = 'BUY'
		btn_buy.pressed.connect(_on_trade.bind(item_id, 1, true))
		row.add_child(btn_buy)

		var btn_sell = Button.new()
		btn_sell.text = 'SELL'
		btn_sell.pressed.connect(_on_trade.bind(item_id, 1, false))
		if player_qty <= 0:
			btn_sell.disabled = true
		row.add_child(btn_sell)

	var spacer = Label.new()
	spacer.text = ''
	spacer.custom_minimum_size = Vector2(780, 12)
	container.add_child(spacer)

	var disc_title = Label.new()
	disc_title.text = '[DISCOVERY]'
	disc_title.custom_minimum_size = Vector2(780, 24)
	container.add_child(disc_title)

	var bridge = get_node_or_null('/root/SimBridge')
	if bridge and bridge.has_method('GetDiscoverySnapshotV0'):
		var snap = bridge.call('GetDiscoverySnapshotV0', current_node_id)
		if typeof(snap) == TYPE_DICTIONARY:
			var discovered_count = int(snap.get('discovered_site_count', 0))
			var scanned_count = int(snap.get('scanned_site_count', 0))
			var analyzed_count = int(snap.get('analyzed_site_count', 0))
			var exp_tok = str(snap.get('expedition_status_token', ''))

			var counts_line = Label.new()
			counts_line.text = 'SITES | DISCOVERED: %s | SCANNED: %s | ANALYZED: %s' % [discovered_count, scanned_count, analyzed_count]
			counts_line.custom_minimum_size = Vector2(780, 22)
			container.add_child(counts_line)

			var exp_line = Label.new()
			exp_line.text = 'EXPEDITION_STATUS_TOKEN: %s' % exp_tok
			exp_line.custom_minimum_size = Vector2(780, 22)
			container.add_child(exp_line)

			# GATE.S3_6.UI_DISCOVERY_MIN.002
			# Render discovery exception summaries only when present (keep Seed 42 baseline unchanged if empty).
			var active_ex = snap.get('active_exceptions', [])
			if typeof(active_ex) == TYPE_ARRAY and active_ex.size() > 0:
				var ex_title = Label.new()
				ex_title.text = 'EXCEPTIONS'
				ex_title.custom_minimum_size = Vector2(780, 24)
				container.add_child(ex_title)

				for ex in active_ex:
					if typeof(ex) != TYPE_DICTIONARY:
						continue
					var ex_tok = str(ex.get('exception_token', ''))
					var reason_tokens = ex.get('reason_tokens', [])
					var verbs = ex.get('intervention_verbs', [])

					var ex_line = Label.new()
					ex_line.text = '%s | REASONS: %s | ACTIONS: %s' % [ex_tok, _join_tokens(reason_tokens), _join_tokens(verbs)]
					ex_line.custom_minimum_size = Vector2(780, 22)
					container.add_child(ex_line)

			var unlocks = snap.get('unlocks', [])
			if typeof(unlocks) == TYPE_ARRAY and unlocks.size() > 0:
				var unlock_title = Label.new()
				unlock_title.text = 'UNLOCKS'
				unlock_title.custom_minimum_size = Vector2(780, 24)
				container.add_child(unlock_title)

				for u in unlocks:
					if typeof(u) != TYPE_DICTIONARY:
						continue
					var unlock_id = str(u.get('unlock_id', ''))
					var effect_tokens = u.get('effect_tokens', [])
					var blocked_reason = str(u.get('blocked_reason_token', ''))
					var blocked_actions = u.get('blocked_action_tokens', [])
					var deploy_verbs = u.get('deploy_verb_control_tokens', [])

					var effects_s = _join_tokens(effect_tokens)
					var deploy_s = _join_tokens(deploy_verbs)

					var uline = Label.new()
					uline.text = '%s | EFFECTS: %s | DEPLOY_VERB_TOKENS: %s' % [unlock_id, effects_s, deploy_s]
					uline.custom_minimum_size = Vector2(780, 22)
					container.add_child(uline)

					if blocked_reason != '':
						var udetail = Label.new()
						var blocked_actions_s = _join_tokens(blocked_actions)
						udetail.text = '  BLOCKED_REASON: %s | ACTIONS: %s' % [blocked_reason, blocked_actions_s]
						udetail.custom_minimum_size = Vector2(780, 20)
						container.add_child(udetail)

			var leads = snap.get('rumor_leads', [])
			if typeof(leads) == TYPE_ARRAY and leads.size() > 0:
				var lead_title = Label.new()
				lead_title.text = 'RUMOR_LEADS'
				lead_title.custom_minimum_size = Vector2(780, 24)
				container.add_child(lead_title)

				for r in leads:
					if typeof(r) != TYPE_DICTIONARY:
						continue
					var lead_id = str(r.get('lead_id', ''))
					var hint_tokens = r.get('hint_tokens', [])
					var hint_s = _join_tokens(hint_tokens)

					var rline = Label.new()
					rline.text = '%s | HINT_TOKENS: %s' % [lead_id, hint_s]
					rline.custom_minimum_size = Vector2(780, 22)
					container.add_child(rline)

	# --- [RUMOR LEADS] section (GATE.S3_6.RUMOR_INTEL_MIN.003) ---
	# Facts-only rumor lead readout min v0.
	# Deterministic: LeadId asc from bridge; token join preserves provided order.
	if bridge and bridge.has_method('GetRumorLeadsSnapshotV0'):
		var rumor_spacer = Label.new()
		rumor_spacer.text = ''
		rumor_spacer.custom_minimum_size = Vector2(780, 12)
		container.add_child(rumor_spacer)

		var rumor_title = Label.new()
		rumor_title.text = '[RUMOR LEADS]'
		rumor_title.custom_minimum_size = Vector2(780, 24)
		container.add_child(rumor_title)

		var leads = bridge.call('GetRumorLeadsSnapshotV0', current_node_id)
		if typeof(leads) == TYPE_ARRAY:
			for r in leads:
				if typeof(r) != TYPE_DICTIONARY:
					continue

				var lead_id = str(r.get('lead_id', ''))
				var hint_tokens = r.get('hint_tokens', [])
				var blocked_reasons = r.get('blocked_reasons', [])

				var hint_s = _join_tokens(hint_tokens)

				var rline = Label.new()
				rline.text = '%s | HINT: %s' % [lead_id, hint_s]
				rline.custom_minimum_size = Vector2(780, 22)
				container.add_child(rline)

				if typeof(blocked_reasons) == TYPE_ARRAY and blocked_reasons.size() > 0:
					var bline = Label.new()
					bline.text = '  BLOCKED: %s' % _join_tokens(blocked_reasons)
					bline.custom_minimum_size = Vector2(780, 20)
					container.add_child(bline)

	# --- [PACKAGE STATUS] section (GATE.S3_6.EXPLOITATION_PACKAGES.003) ---
	# Facts-only exploitation package explainability v0.
	# Renders explain chain tokens, intervention verbs, and exception policy levers.
	# Deterministic: ordering is primary-first then secondary Ordinal asc; verbs Ordinal asc.
	if bridge and bridge.has_method('GetExploitationPackageSummary'):
		var pkg_spacer = Label.new()
		pkg_spacer.text = ''
		pkg_spacer.custom_minimum_size = Vector2(780, 12)
		container.add_child(pkg_spacer)

		var pkg_title = Label.new()
		pkg_title.text = '[PACKAGE STATUS]'
		pkg_title.custom_minimum_size = Vector2(780, 24)
		container.add_child(pkg_title)

		# Enumerate active programs and call GetExploitationPackageSummary for each.
		# Programs list is Facts-only via GetProgramExplainSnapshot (already in bridge).
		var prog_list = bridge.call('GetProgramExplainSnapshot')
		if typeof(prog_list) == TYPE_ARRAY:
			for prog_entry in prog_list:
				if typeof(prog_entry) != TYPE_DICTIONARY:
					continue
				var prog_id = str(prog_entry.get('id', ''))
				if prog_id == '':
					continue
				var pkg = bridge.call('GetExploitationPackageSummary', prog_id)
				if typeof(pkg) != TYPE_DICTIONARY:
					continue

				var pkg_status = str(pkg.get('status', ''))
				var pkg_chain = pkg.get('explain_chain', [])
				var pkg_verbs = pkg.get('intervention_verbs', [])
				var pkg_levers = pkg.get('exception_policy_levers', [])

				var pline = Label.new()
				pline.text = '%s | STATUS: %s' % [prog_id, pkg_status]
				pline.custom_minimum_size = Vector2(780, 22)
				container.add_child(pline)

				# Explain chain: primary entries first, then secondary.
				if typeof(pkg_chain) == TYPE_ARRAY and pkg_chain.size() > 0:
					var chain_s := ''
					for ci in range(pkg_chain.size()):
						var ce = pkg_chain[ci]
						if typeof(ce) != TYPE_DICTIONARY:
							continue
						var tok = str(ce.get('token', ''))
						var is_prim = bool(ce.get('is_primary', false))
						var seg = '%s[%s]' % [tok, 'P' if is_prim else 'S']
						chain_s = seg if chain_s == '' else chain_s + ',' + seg
					if chain_s != '':
						var cline = Label.new()
						cline.text = '  EXPLAIN: %s' % chain_s
						cline.custom_minimum_size = Vector2(780, 20)
						container.add_child(cline)

				# Intervention verbs: >= 1 required per active disruption.
				if typeof(pkg_verbs) == TYPE_ARRAY and pkg_verbs.size() > 0:
					var verbs_s = _join_tokens(pkg_verbs)
					var vline = Label.new()
					vline.text = '  VERBS: %s' % verbs_s
					vline.custom_minimum_size = Vector2(780, 20)
					container.add_child(vline)

				# Exception policy levers.
				if typeof(pkg_levers) == TYPE_ARRAY and pkg_levers.size() > 0:
					var levers_s = _join_tokens(pkg_levers)
					var lline = Label.new()
					lline.text = '  POLICY: %s' % levers_s
					lline.custom_minimum_size = Vector2(780, 20)
					container.add_child(lline)

func _join_tokens(tokens) -> String:
	if typeof(tokens) != TYPE_ARRAY:
		return ''
	var out := ''
	for i in range(tokens.size()):
		var t = str(tokens[i])
		if i == 0:
			out = t
		else:
			out += ',' + t
	return out


func _format_chain(chain) -> String:
	# Determinism: SimBridge provides stable ordering; this formatter preserves array order.
	if typeof(chain) != TYPE_ARRAY:
		return ''
	var parts := ''
	for i in range(chain.size()):
		var e = chain[i]
		if typeof(e) != TYPE_DICTIONARY:
			continue
		var phase = str(e.get('phase', ''))
		var rc = str(e.get('reason_code', ''))
		var seg = '%s:%s' % [phase, rc]
		if parts == '':
			parts = seg
		else:
			parts += ' | ' + seg
	return parts

func _on_trade(item_id, qty, is_buy):
	var success = manager_ref.try_trade(current_node_id, item_id, qty, is_buy)
	if success:
		refresh_market_list()
	else:
		print('Trade Failed')
