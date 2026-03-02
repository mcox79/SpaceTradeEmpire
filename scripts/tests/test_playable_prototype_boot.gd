extends SceneTree

# Headless boot probe for scenes/playable_prototype.tscn that always quits.
# Deterministic: token output only, no timestamps, no random values.
# Two consecutive runs must produce identical PPB| output.

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const BOOT_FRAMES_V0 := 30

func _stop_sim_and_quit(code: int) -> void:
	# Stop SimBridge background thread before quitting so the process exits promptly.
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge and bridge.has_method("StopSimV0"):
		bridge.call("StopSimV0")
	quit(code)

func _fail(msg: String) -> void:
	print("PPB|FAIL|" + msg)
	_stop_sim_and_quit(1)

func _ok(msg: String) -> void:
	print("PPB|OK|" + msg)

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	_ok("BOOT")

	var packed = load(SCENE_PATH)
	if packed == null:
		_fail("SCENE_LOAD_NULL")
		return

	var inst = packed.instantiate()
	if inst == null:
		_fail("SCENE_INSTANTIATE_NULL")
		return

	get_root().add_child(inst)

	# Yield physics frames so _ready() chains and deferred calls complete.
	for i in range(BOOT_FRAMES_V0):
		await physics_frame

	# --- Assertions ---

	# 1. GameManager present
	var gm = inst.get_node_or_null("GameManager")
	if gm == null:
		_fail("NO_GAME_MANAGER")
		return

	# 2. Player present and is RigidBody3D
	var player = inst.get_node_or_null("Player")
	if player == null:
		_fail("NO_PLAYER")
		return
	if not (player is RigidBody3D):
		_fail("PLAYER_NOT_RIGIDBODY3D")
		return

	# 3. Player in "Player" group (required by StationMenu, HUD, and camera wiring)
	if not player.is_in_group("Player"):
		_fail("PLAYER_NOT_IN_GROUP")
		return

	# 4. StationMenu present under UI
	var sm = inst.get_node_or_null("UI/StationMenu")
	if sm == null:
		_fail("NO_STATION_MENU")
		return

	# 5. Local scene is ticking (time_accumulator > 0)
	var acc = gm.get("time_accumulator")
	if acc == null or float(acc) <= 0.0:
		_fail("TIME_ACCUMULATOR_NOT_TICKING")
		return

	_ok("GM_PRESENT")
	_ok("PLAYER_RIGIDBODY3D")
	_ok("PLAYER_IN_GROUP")
	_ok("STATION_MENU_PRESENT")
	_ok("TIME_ACCUMULATOR_TICKING")
	_ok("DONE|frames=" + str(BOOT_FRAMES_V0))
	_stop_sim_and_quit(0)
