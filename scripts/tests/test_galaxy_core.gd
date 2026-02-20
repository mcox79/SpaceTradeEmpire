extends SceneTree

# Minimal, diff-friendly seed determinism regression.
# Executed via:
#   Godot --headless --quit --script res://scripts/tests/test_galaxy_core.gd
# Must inherit SceneTree or MainLoop.

const GalaxyGenerator = preload("res://scripts/core/sim/galaxy_generator.gd")

var _fail_count := 0

func _init() -> void:
	print("--- STARTING GALAXY CORE TESTS ---")
	_run_seed_determinism_regression()
	if _fail_count > 0:
		printerr("--- GALAXY CORE TESTS FAILED (%d) ---" % _fail_count)
		quit(1)
		return
	print("--- GALAXY CORE TESTS PASSED ---")
	quit(0)
	return

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
	var factions: Array = out.get("factions", [])

	# Stars and lanes are Dictionaries; use get() for stable access.
	var stars_sorted := stars.duplicate()
	stars_sorted.sort_custom(func(a, b):
		return String(a.get("id", "")) < String(b.get("id", ""))
	)

	lines.append("stars_count=%d" % stars_sorted.size())
	for s in stars_sorted:
		var sid := String(s.get("id", ""))
		var region := int(s.get("region", 0))
		var pos: Vector3 = s.get("pos", Vector3.ZERO)
		lines.append("S|%s|r=%d|p=%s" % [sid, region, _fmt_vec3(pos)])

	var lane_items: Array = []
	for l in lanes:
		var u := String(l.get("u", ""))
		var v := String(l.get("v", ""))
		if v < u:
			var tmp := u
			u = v
			v = tmp
		lane_items.append({
			"u": u,
			"v": v,
			"from": l.get("from", Vector3.ZERO),
			"to": l.get("to", Vector3.ZERO),
		})

	lane_items.sort_custom(func(a, b):
		var au := String(a.get("u", ""))
		var av := String(a.get("v", ""))
		var bu := String(b.get("u", ""))
		var bv := String(b.get("v", ""))
		if au == bu:
			return av < bv
		return au < bu
	)

	lines.append("lanes_count=%d" % lane_items.size())
	for l2 in lane_items:
		lines.append("L|%s-%s|f=%s|t=%s" % [
			String(l2.get("u", "")),
			String(l2.get("v", "")),
			_fmt_vec3(l2.get("from", Vector3.ZERO)),
			_fmt_vec3(l2.get("to", Vector3.ZERO)),
		])

	# Factions: diff-friendly table sorted by FactionId + relations matrix with rows%cols sorted.
	var factions_sorted := factions.duplicate()
	factions_sorted.sort_custom(func(a, b):
		return String(a.get("FactionId", "")) < String(b.get("FactionId", ""))
	)

	var fids: Array[String] = []
	for f in factions_sorted:
		fids.append(String(f.get("FactionId", "")))

	lines.append("factions_count=%d" % factions_sorted.size())
	for f in factions_sorted:
		lines.append("F|%s|home=%s|role=%s" % [
			String(f.get("FactionId", "")),
			String(f.get("HomeNodeId", "")),
			String(f.get("RoleTag", "")),
		])

	for f in factions_sorted:
		var row := String(f.get("FactionId", ""))
		var rel: Dictionary = f.get("Relations", {})
		var vals: Array[String] = []
		for col in fids:
			if col == row:
				vals.append("0")
			else:
				vals.append(str(int(rel.get(col, 0))))
		lines.append("M|%s|%s" % [row, ",".join(vals)])

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
