extends SceneTree

const Sim = preload("res://scripts/core/sim/sim.gd")

const DEFAULT_SEED: int = 42
const DEFAULT_TICKS: int = 120
const DEFAULT_SCENE_PATH: String = "res://scenes/main.tscn"

func _init():
	var seed := DEFAULT_SEED
	var ticks := DEFAULT_TICKS
	var scene_path := DEFAULT_SCENE_PATH

	var args = OS.get_cmdline_args()
	for a in args:
		if a.begins_with("--seed="):
			seed = int(a.trim_prefix("--seed="))
		elif a.begins_with("--ticks="):
			ticks = int(a.trim_prefix("--ticks="))
		elif a.begins_with("--scene="):
			scene_path = a.trim_prefix("--scene=")

	_load_minimal_scene(scene_path)

	var sim := Sim.new(seed)

	print("SMOKE: Seed=" + str(seed))
	print("SMOKE: tick_count=" + str(ticks))

	print("SMOKE: world_hash@0=" + sim.get_world_hash())

	for i in range(ticks):
		sim.advance()
		var t = i + 1
		if t == 60 or t == 120:
			print("SMOKE: world_hash@" + str(t) + "=" + sim.get_world_hash())

	_print_sorted_counts(sim.get_entity_counts())

	quit(0)

func _load_minimal_scene(scene_path: String) -> void:
	var ps = load(scene_path)
	if ps == null:
		printerr("SMOKE: FAIL load scene: " + scene_path)
		quit(1)
		return
	var inst = ps.instantiate()
	if inst == null:
		printerr("SMOKE: FAIL instantiate scene: " + scene_path)
		quit(1)
		return
	root.add_child(inst)

func _print_sorted_counts(counts: Dictionary) -> void:
	var keys: Array[String] = []
	for k in counts.keys():
		keys.append(str(k))
	keys.sort()

	print("SMOKE: counts_begin")
	for k2 in keys:
		print("SMOKE: count." + k2 + "=" + str(counts[k2]))
	print("SMOKE: counts_end")
