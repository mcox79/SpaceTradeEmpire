extends SceneTree

# Minimal, diff-friendly seed determinism regression.
# Executed via:
#   Godot --headless --quit --script res://scripts/tests/test_galaxy_core.gd
# Must inherit SceneTree or MainLoop.

const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")

var _fail_count := 0

func _initialize():
	print("--- STARTING GALAXY CORE TESTS ---")
	_run_seed_determinism_regression()
	if _fail_count > 0:
		printerr("--- GALAXY CORE TESTS FAILED (%d) ---" % _fail_count)
		quit(1)
		return
	print("--- GALAXY CORE TESTS PASSED ---")
	quit(0)

func _run_seed_determinism_regression():
	var region_count := 12
	var seed_same := 12345
	var seed_diff := 54321

	var out1 := _generate(region_count, seed_same)
	var repr1 := _galaxy_repr(out1)
	_assert(repr1.length() > 0, "repr1 non-empty")

	var out2 := _generate(region_count, seed_same)
	var repr2 := _galaxy_repr(out2)
	_assert(repr1 == repr2, "Same Seed yields identical ordered stars%%lanes (h1=%s h2=%s)" % [
		_hex32(_fnv1a32(repr1)),
		_hex32(_fnv1a32(repr2)),
	])

	var out3 := _generate(region_count, seed_diff)
	var repr3 := _galaxy_repr(out3)
	_assert(repr1 != repr3, "Different Seeds yield deterministic structural diff (h1=%s h3=%s)" % [
		_hex32(_fnv1a32(repr1)),
		_hex32(_fnv1a32(repr3)),
	])

func _generate(region_count: int, seed: int) -> Dictionary:
	var gen = GalaxyGenerator.new(seed)
	var out: Dictionary = gen.generate(region_count)
	return out

func _galaxy_repr(out: Dictionary) -> String:
	var lines: Array[String] = []

	var stars: Array = out.get("stars", [])
	var lanes: Array = out.get("lanes", [])

	stars.sort_custom(func(a, b):
		return String(a.id) < String(b.id)
	)

	lines.append("stars_count=%d" % stars.size())
	for s in stars:
		lines.append("S|%s|r=%d|p=%s" % [String(s.id), int(s.region), _fmt_vec3(s.pos)])

	var lane_items: Array = []
	for l in lanes:
		var u := String(l.u)
		var v := String(l.v)
		if v < u:
			var tmp := u
			u = v
			v = tmp
		lane_items.append({
			"u": u,
			"v": v,
			"from": l.from,
			"to": l.to,
		})

	lane_items.sort_custom(func(a, b):
		if a.u == b.u:
			return String(a.v) < String(b.v)
		return String(a.u) < String(b.u)
	)

	lines.append("lanes_count=%d" % lane_items.size())
	for l2 in lane_items:
		lines.append("L|%s-%s|f=%s|t=%s" % [String(l2.u), String(l2.v), _fmt_vec3(l2.from), _fmt_vec3(l2.to)])

	return "\n".join(lines)

func _fmt_vec3(v: Vector3) -> String:
	return "%.6f,%.6f,%.6f" % [float(v.x), float(v.y), float(v.z)]

func _fnv1a32(s: String) -> int:
	var bytes: PackedByteArray = s.to_utf8_buffer()
	var hash: int = 0x811c9dc5
	for b in bytes:
		hash = int((hash ^ int(b)) & 0xffffffff)
		hash = int((hash * 0x01000193) & 0xffffffff)
	return hash

func _hex32(v: int) -> String:
	return "0x%08x" % int(v & 0xffffffff)

func _assert(condition: bool, message: String):
	if condition:
		print("[PASS] " + message)
	else:
		_fail_count += 1
		printerr("[FAIL] " + message)
