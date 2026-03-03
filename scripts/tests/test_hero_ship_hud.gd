extends SceneTree

# Headless proof for GATE.S1.HERO_SHIP_LOOP.HUD.001.
# Contract:
# - No timestamps / wall-clock values in output (SHA256-stable).
# - Prefix: HSH|
# - Exits nonzero on first failure.

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES := 30

func _stop_sim_and_quit(code: int) -> void:
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("HSH|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("HSH|OK|" + msg)

func _initialize() -> void:
	print("HSH|BOOT")
	call_deferred("_run")

func _run() -> void:
	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	get_root().add_child(inst)

	for _i in range(BOOT_FRAMES):
		await physics_frame

	# --- Verify HUD node exists in scene ---
	var hud = inst.get_node_or_null("HUD")
	if hud == null:
		_fail("HUD_NODE_NOT_FOUND")
		return
	_ok("hud_node_found=true")

	# --- Verify SimBridge method ---
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("SIMBRIDGE_NOT_FOUND")
		return
	if not bridge.has_method("GetPlayerStateV0"):
		_fail("MISSING_GetPlayerStateV0")
		return
	_ok("bridge_method_present=true")

	# --- Verify player state data ---
	var ps: Dictionary = bridge.call("GetPlayerStateV0")
	var credits = ps.get("credits", -1)
	if credits < 0:
		_fail("CREDITS_NEGATIVE|" + str(credits))
		return
	_ok("credits=" + str(credits))

	var current_node_id: String = ps.get("current_node_id", "")
	if current_node_id.is_empty():
		_fail("CURRENT_NODE_ID_EMPTY")
		return
	_ok("current_node_id=" + current_node_id)

	_ok("DONE")
	_stop_sim_and_quit(0)
