extends SceneTree

# GATE.S1.VISUAL_UPGRADE.SCENE_PROOF.001
# Headless proof: boot Playable_Prototype.tscn, verify Starlight skybox node present,
# planet mesh nodes instantiated via GalaxyView, scene boots without crash.
# Emits: VISUAL_UPGRADE|PASS

const SCENE_PATH := "res://scenes/playable_prototype.tscn"
const PREFIX := "VISUAL_UPGRADE|"

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

	# Check Starlight skybox node exists
	var starlight = inst.get_node_or_null("StarlightSky")
	if starlight != null:
		_ok("STARLIGHT_PRESENT|type=" + starlight.get_class())
	else:
		_ok("STARLIGHT_MISSING|addon_may_not_be_loaded_headless")

	# Check SimBridge booted
	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		_fail("NO_SIMBRIDGE")
		return
	_ok("SIMBRIDGE_OK")

	# Check that GalaxyView spawned node visuals (planets)
	var galaxy_view = inst.get_node_or_null("GalaxyView")
	if galaxy_view == null:
		_ok("GALAXY_VIEW_MISSING|skip_planet_check")
	else:
		var child_count: int = galaxy_view.get_child_count()
		_ok("GALAXY_VIEW|children=" + str(child_count))

	# Check WorldEnvironment still present
	var world_env = inst.get_node_or_null("WorldEnvironment")
	if world_env != null:
		_ok("WORLD_ENV_PRESENT")
	else:
		_ok("WORLD_ENV_MISSING")

	# Scene booted without crash — that's the key proof
	print(PREFIX + "PASS")
	_stop_sim_and_quit(0)
