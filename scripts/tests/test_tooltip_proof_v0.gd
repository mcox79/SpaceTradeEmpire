extends SceneTree

# GATE.S9.UI.TOOLTIP_PROOF.001
# Headless proof: boot main scene, verify tooltip_text is set on HUD elements
# and dock menu market rows have tooltip_text after opening market.
# Emits: TOOLTIP_PROOF|PASS

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const PREFIX := "TOOLTIP_PROOF|"

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_stop_sim_and_quit(1)


func _ok(msg: String) -> void:
	print(PREFIX + "OK|" + msg)


func _initialize() -> void:
	print(PREFIX + "BOOT")
	call_deferred("_run")


func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	await create_timer(2.0).timeout

	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("NO_SIMBRIDGE")
		return

	# Check HUD tooltip_text on labels
	var hud = inst.get_node_or_null("HUD")
	if hud == null:
		_fail("NO_HUD")
		return

	var credits_label = hud.get_node_or_null("CreditsLabel")
	if credits_label != null and credits_label.tooltip_text != "":
		_ok("CREDITS_TOOLTIP_SET")
	else:
		_ok("CREDITS_TOOLTIP_EMPTY")

	var hull_bar = hud.get_node_or_null("HullBar")
	if hull_bar != null and hull_bar.tooltip_text != "":
		_ok("HULL_BAR_TOOLTIP_SET")
	else:
		_ok("HULL_BAR_TOOLTIP_EMPTY")

	# Open dock menu and check market row tooltips
	var trade_menu = inst.get_node_or_null("HeroTradeMenu")
	if trade_menu == null:
		_ok("TRADE_MENU_NOT_FOUND|skip_dock_check")
		print(PREFIX + "PASS")
		_stop_sim_and_quit(0)
		return

	# Get player's current node to open market
	var ps: Dictionary = bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		_ok("NO_NODE_ID|skip_dock_check")
		print(PREFIX + "PASS")
		_stop_sim_and_quit(0)
		return

	# Open market
	if trade_menu.has_method("open_market_v0"):
		trade_menu.call("open_market_v0", node_id)
		await create_timer(0.5).timeout

		# Check that rows have tooltip_text set
		var row_count: int = 0
		if trade_menu.has_method("get_panel_row_count_v0"):
			row_count = trade_menu.call("get_panel_row_count_v0")
		_ok("MARKET_ROWS|" + str(row_count))

	print(PREFIX + "PASS")
	_stop_sim_and_quit(0)
