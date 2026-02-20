extends RefCounted

var rng_streams: RngStreams
var rng: RandomNumberGenerator
var seed: int = 0

static func _fnv1a32(s: String) -> int:
	var bytes: PackedByteArray = s.to_utf8_buffer()
	var h: int = 0x811c9dc5
	for b in bytes:
		h = int((h ^ int(b)) & 0xffffffff)
		h = int((h * 0x01000193) & 0xffffffff)
	return h

static func _q1e3(v: float) -> int:
	return int(round(v * 1000.0))

func _init(seed_val: int = 0):
	seed = seed_val
	rng_streams = RngStreams.new(seed_val)
	rng = rng_streams.get_stream(RngStreams.STREAM_GALAXY_GEN)

# Entry point for GameShell worldgen: use the canonical SimState seed, without mutating it.
func generate_for_state(state: SimState, region_count: int) -> Dictionary:
	var local_streams := RngStreams.new(state.seed)
	var local_rng := local_streams.get_stream(RngStreams.STREAM_GALAXY_GEN)
	return _generate_with_rng(local_rng, region_count)

# Back-compat entry point: uses this instance's configured RNG.
func generate(region_count: int) -> Dictionary:
	return _generate_with_rng(rng, region_count)

func _generate_with_rng(r: RandomNumberGenerator, region_count: int) -> Dictionary:
	var stars = []
	var lanes = []
	var region_data = []

	# 1. GENERATE STAR POSITIONS
	for region_i in range(region_count):
		var center = Vector3(r.randf_range(-100, 100), 0, r.randf_range(-100, 100))
		var star_count = r.randi_range(4, 7)
		var current_region_stars = []

		for _s in range(star_count):
			var pos = center + Vector3(r.randf_range(-20, 20), r.randf_range(-5, 5), r.randf_range(-20, 20))
			var star = { 'id': 'star_' + str(stars.size()), 'pos': pos, 'region': region_i }
			stars.append(star)
			current_region_stars.append(star)

		region_data.append(current_region_stars)

	# 2. LOCAL MESH (Smart Connections, stable ordering)
	for r_stars in region_data:
		for star in r_stars:
			var others = r_stars.filter(func(s): return s.id != star.id)
			others.sort_custom(func(a, b):
				var da: float = star.pos.distance_squared_to(a.pos)
				var db: float = star.pos.distance_squared_to(b.pos)
				if da == db:
					return String(a.id) < String(b.id)
				return da < db
			)

			for i in range(min(2, others.size())):
				var target = others[i]
				# Check duplicates using IDs instead of positions
				var duplicate = lanes.any(func(l):
					return (l.u == star.id and l.v == target.id) or (l.u == target.id and l.v == star.id)
				)
				if not duplicate:
					lanes.append({
						'u': star.id,
						'v': target.id,
						'from': star.pos,
						'to': target.pos
					})

	# 3. INTER-REGION ARTERIES
	for region_i in range(region_count - 1):
		var r1 = region_data[region_i]
		var r2 = region_data[region_i + 1]

		# Main Artery
		lanes.append({
			'u': r1[0].id,
			'v': r2[0].id,
			'from': r1[0].pos,
			'to': r2[0].pos
		})

		# Smuggler Route
		lanes.append({
			'u': r1[-1].id,
			'v': r2[-1].id,
			'from': r1[-1].pos,
			'to': r2[-1].pos
		})

	# 4. FACTIONS (Deterministic seeding v0)
	# Create 3 factions with stable ids and stable home selection that varies across seeds (position-derived scoring).
	# Avoid relying on sequential star ids, which can be identical across different seeds for a fixed region_count.

	var scored: Array = []
	for s in stars:
		var sid := String(s.get("id", ""))
		var p: Vector3 = s.get("pos", Vector3.ZERO)
		var score_seed := int(r.seed)
		var score := _fnv1a32("%d|%s|%d|%d|%d" % [score_seed, sid, _q1e3(p.x), _q1e3(p.y), _q1e3(p.z)])
		scored.append({ "id": sid, "score": score })

	scored.sort_custom(func(a, b):
		var sa := int(a.get("score", 0))
		var sb2 := int(b.get("score", 0))
		if sa == sb2:
			return String(a.get("id", "")) < String(b.get("id", ""))
		return sa > sb2
	)

	var home_ids: Array[String] = []
	var used: Dictionary = {}
	for item in scored:
		var sid := String(item.get("id", ""))
		if used.has(sid):
			continue
		used[sid] = true
		home_ids.append(sid)
		if home_ids.size() >= 3:
			break

	var roles: Array[String] = ["Trader", "Miner", "Pirate"]
	var fids := ["faction_0", "faction_1", "faction_2"]

	# Canonical relations pattern (values in {-1,0,+1}).
	var rels: Dictionary = {
		"faction_0": {"faction_1": +1, "faction_2": -1},
		"faction_1": {"faction_0": +1, "faction_2":  0},
		"faction_2": {"faction_0": -1, "faction_1":  0},
	}

	var factions: Array = []
	for i in range(3):
		var fid: String = String(fids[i])
		var home: String = String(home_ids[i]) if i < home_ids.size() else ""
		var role: String = roles[i]
		var rmap: Dictionary = rels.get(fid, {})
		factions.append({
			"FactionId": fid,
			"HomeNodeId": home,
			"RoleTag": role,
			"Relations": rmap,
		})

	# 5. WORLD CLASSES (Deterministic v0)
	# Define exactly 3 classes. Each class has exactly one measurable effect in v0: fee_multiplier.
	# Assignment is deterministic and enforces invariant:
	# starter region (region 0) contains at least 1 node of each class.
	# Approach:
	# - Sort NodeId for stable order.
	# - Force the first 3 nodes in starter region (sorted) to CORE, FRONTIER, RIM.
	# - Assign the rest round-robin deterministically, skipping forced nodes.
	var world_classes: Array = [
		{ "WorldClassId": "CORE", "fee_multiplier": 1.00 },
		{ "WorldClassId": "FRONTIER", "fee_multiplier": 1.10 },
		{ "WorldClassId": "RIM", "fee_multiplier": 1.20 },
	]

	var stars_sorted_for_class := stars.duplicate()
	stars_sorted_for_class.sort_custom(func(a, b):
		return String(a.get("id", "")) < String(b.get("id", ""))
	)

	# Determine forced starter nodes (region 0) in stable NodeId order.
	var starter_sorted: Array = []
	for s0 in stars_sorted_for_class:
		if int(s0.get("region", 0)) == 0:
			starter_sorted.append(s0)

	var forced: Dictionary = {}
	for i in range(min(3, starter_sorted.size())):
		forced[String(starter_sorted[i].get("id", ""))] = i

	var node_classes: Array = []
	var rr := 0
	for s2 in stars_sorted_for_class:
		var sid2 := String(s2.get("id", ""))
		var cidx: int
		if forced.has(sid2):
			cidx = int(forced[sid2])
		else:
			cidx = rr % 3
			rr += 1

		var cdef: Dictionary = world_classes[cidx]
		var cid := String(cdef.get("WorldClassId", ""))
		var fm := float(cdef.get("fee_multiplier", 1.0))

		# Tag the star record itself for convenience/debug (no additional effects in v0).
		s2["WorldClassId"] = cid

		node_classes.append({
			"NodeId": sid2,
			"WorldClassId": cid,
			"fee_multiplier": fm,
		})

	return {
		'stars': stars,
		'lanes': lanes,
		'factions': factions,
		'world_classes': world_classes,
		'node_classes': node_classes,
	}

# Determinism probe: canonical digest of ordered stars+lanes for a given seed and region_count.
# This is intended for headless proofs only. Call on an instance initialized with the seed.
func determinism_digest(region_count: int) -> String:
	var out: Dictionary = _generate_with_rng(rng, region_count)
	return _canonical_digest(out)

# Diff-friendly deterministic report: per-class summary plus per-node assignment list (sorted by NodeId).
# Intended for headless proof output and debugging.
func world_class_report(region_count: int) -> String:
	var out: Dictionary = _generate_with_rng(rng, region_count)

	var world_classes: Array = out.get("world_classes", [])
	var node_classes: Array = out.get("node_classes", [])

	var classes_sorted := world_classes.duplicate()
	classes_sorted.sort_custom(func(a, b):
		return String(a.get("WorldClassId", "")) < String(b.get("WorldClassId", ""))
	)

	var nodes_sorted := node_classes.duplicate()
	nodes_sorted.sort_custom(func(a, b):
		return String(a.get("NodeId", "")) < String(b.get("NodeId", ""))
	)

	var sb: Array[String] = []
	sb.append("WorldClassId\tfee_multiplier")
	for c in classes_sorted:
		sb.append("%s\t%.2f" % [String(c.get("WorldClassId", "")), float(c.get("fee_multiplier", 1.0))])

	sb.append("NodeId\tWorldClassId\tfee_multiplier")
	for nc in nodes_sorted:
		sb.append("%s\t%s\t%.2f" % [
			String(nc.get("NodeId", "")),
			String(nc.get("WorldClassId", "")),
			float(nc.get("fee_multiplier", 1.0)),
		])

	return "\n".join(sb)

static func _canonical_digest(out: Dictionary) -> String:
	var stars: Array = out.get("stars", [])
	var lanes: Array = out.get("lanes", [])
	var factions: Array = out.get("factions", [])
	var world_classes: Array = out.get("world_classes", [])
	var node_classes: Array = out.get("node_classes", [])

	# Sort stars by id (string)
	var stars_sorted := stars.duplicate()
	stars_sorted.sort_custom(func(a, b):
		return String(a.get("id", "")) < String(b.get("id", ""))
	)

	# Normalize lanes to (min,max) id order, then sort by (u,v)
	var lanes_norm: Array = []
	for l in lanes:
		var u := String(l.get("u", ""))
		var v := String(l.get("v", ""))
		if v < u:
			var tmp := u
			u = v
			v = tmp
		lanes_norm.append({ "u": u, "v": v })
	lanes_norm.sort_custom(func(a, b):
		var au := String(a.get("u", ""))
		var av := String(a.get("v", ""))
		var bu := String(b.get("u", ""))
		var bv := String(b.get("v", ""))
		if au == bu:
			return av < bv
		return au < bu
	)

	# Sort factions by FactionId and render as table + relations matrix with sorted rows%cols.
	var factions_sorted := factions.duplicate()
	factions_sorted.sort_custom(func(a, b):
		return String(a.get("FactionId", "")) < String(b.get("FactionId", ""))
	)
	var fids: Array[String] = []
	for f in factions_sorted:
		fids.append(String(f.get("FactionId", "")))

	var sb: Array[String] = []
	for s in stars_sorted:
		var id := String(s.get("id", ""))
		var region := int(s.get("region", 0))
		var p: Vector3 = s.get("pos", Vector3.ZERO)
		sb.append("S|%s|%s|%.6f,%.6f,%.6f" % [id, region, p.x, p.y, p.z])
	for l2 in lanes_norm:
		sb.append("L|%s|%s" % [String(l2.get("u", "")), String(l2.get("v", ""))])

	for f in factions_sorted:
		sb.append("F|%s|%s|%s" % [
			String(f.get("FactionId", "")),
			String(f.get("HomeNodeId", "")),
			String(f.get("RoleTag", "")),
		])

	# World class summary (sorted by WorldClassId), then per-node assignments (sorted by NodeId).
	var classes_sorted := world_classes.duplicate()
	classes_sorted.sort_custom(func(a, b):
		return String(a.get("WorldClassId", "")) < String(b.get("WorldClassId", ""))
	)
	for c in classes_sorted:
		sb.append("C|%s|fm=%.2f" % [String(c.get("WorldClassId", "")), float(c.get("fee_multiplier", 1.0))])

	var nodes_sorted := node_classes.duplicate()
	nodes_sorted.sort_custom(func(a, b):
		return String(a.get("NodeId", "")) < String(b.get("NodeId", ""))
	)
	for nc in nodes_sorted:
		sb.append("W|%s|%s|fm=%.2f" % [
			String(nc.get("NodeId", "")),
			String(nc.get("WorldClassId", "")),
			float(nc.get("fee_multiplier", 1.0)),
		])

	# Matrix rows: M|row_fid|v0,v1,v2... where cols are sorted by fid.
	for f in factions_sorted:
		var row := String(f.get("FactionId", ""))
		var rel: Dictionary = f.get("Relations", {})
		var vals: Array[String] = []
		for col in fids:
			if col == row:
				vals.append("0")
			else:
				vals.append(str(int(rel.get(col, 0))))
		sb.append("M|%s|%s" % [row, ",".join(vals)])

	return String("\n").join(sb).sha256_text()
