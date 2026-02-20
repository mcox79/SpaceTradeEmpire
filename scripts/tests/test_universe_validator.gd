extends SceneTree

# Universe validator harness for Slice 2.5 onboarding invariants.
# Determinism contract:
# - No timestamps or time-based entropy
# - Failure records are emitted in a byte-for-byte stable order
# - Records sorted by InvariantName, then PrimaryId, then Seed

const Generator = preload("res://scripts/core/sim/galaxy_generator.gd")

const INV_CONNECTED_GRAPH := "CONNECTED_GRAPH"
const INV_STARTER_REGION_SAFE_PATH := "STARTER_REGION_SAFE_PATH"
const INV_EARLY_LOOPS_MIN3 := "EARLY_LOOPS_MIN3"
const INV_STARTER_REGION_WORLD_CLASSES := "STARTER_REGION_WORLD_CLASSES"

const SEED_COUNT := 1000
const STARTER_SAFE_MAX_CHOKEPOINTS := 1
const EARLY_LOOP_MIN := 3

func _init():
	print("--- STARTING UNIVERSE VALIDATOR HARNESS ---")
	var failures := _run_invariant_sweep(SEED_COUNT)
	_emit_failure_report(failures)

	# GATE.S2_5.WGEN.WORLD_CLASSES.001: deterministic emit of world class assignments (single seed).
	# Keep this constant and independent of the invariant sweep so output is stable.
	print("--- WORLD CLASS REPORT (seed 0) ---")
	var gen0 = Generator.new(0)
	print(gen0.world_class_report(12))
	print("--- END WORLD CLASS REPORT ---")

	var passed := SEED_COUNT - failures.size()
	if failures.size() == 0:
		print("[PASS] Universe Validator: %d/%d seeds passed onboarding invariants." % [passed, SEED_COUNT])
	else:
		printerr("[FAIL] Universe Validator: %d/%d seeds passed onboarding invariants." % [passed, SEED_COUNT])

	print("--- VALIDATOR COMPLETE ---")
	quit(0)

func _run_invariant_sweep(count: int) -> Array:
	var failures: Array = []

	for seed in range(count):
		# Prefer the same generator surface as other tests: generate(region_count).
		# region_count chosen to match typical slice expectations (>= starter region + beyond).
		var gen = Generator.new(seed)
		var out: Dictionary = gen.generate(12)

		var stars: Array = out.get("stars", [])
		var lanes: Array = out.get("lanes", [])

		var g := _build_graph(stars, lanes)

		_check_connected_graph(seed, g, failures)
		_check_starter_region_safe_path(seed, g, failures)
		_check_early_loops_min3(seed, g, failures)
		_check_starter_region_world_classes(seed, out, g, failures)

	return failures

func _check_connected_graph(seed: int, g: Dictionary, failures: Array) -> void:
	var nodes_any = g.get("nodes", [])
	var nodes: Array = nodes_any as Array
	if nodes.size() == 0:
		_fail(failures, seed, INV_CONNECTED_GRAPH, "ALL", {"reason": "no_nodes"})
		return

	var start := String(nodes[0])
	var visited := _bfs_reachable(start, g.get("adj", {}))
	if visited.size() != nodes.size():
		_fail(failures, seed, INV_CONNECTED_GRAPH, "ALL", {
			"nodes": str(nodes.size()),
			"reachable": str(visited.size()),
			"start": start,
		})

func _check_starter_region_safe_path(seed: int, g: Dictionary, failures: Array) -> void:
	var starter_region := _pick_starter_region(g)
	if starter_region == -1:
		_fail(failures, seed, INV_STARTER_REGION_SAFE_PATH, "ALL", {"reason": "no_regions"})
		return

	var starter_nodes: Array[String] = _nodes_in_region(g, starter_region)
	if starter_nodes.size() < 2:
		_fail(failures, seed, INV_STARTER_REGION_SAFE_PATH, "REGION_%d" % starter_region, {
			"reason": "insufficient_starter_nodes",
			"count": str(starter_nodes.size()),
		})
		return

	# Choose a deterministic "starter hub": lexicographically smallest node id in starter region.
	starter_nodes.sort()
	var hub := starter_nodes[0]

	# Compute chokepoint nodes (articulation points) on the full graph (deterministic).
	var chokepoints: Dictionary = _articulation_points(g.get("nodes", []), g.get("adj", {}))

	# For each other starter node, require a path from hub with <= STARTER_SAFE_MAX_CHOKEPOINTS chokepoints on the path.
	for i in range(1, starter_nodes.size()):
		var target := starter_nodes[i]
		var ok := _exists_path_with_chokepoint_budget(hub, target, g.get("adj", {}), chokepoints, STARTER_SAFE_MAX_CHOKEPOINTS)
		if not ok:
			_fail(failures, seed, INV_STARTER_REGION_SAFE_PATH, target, {
				"hub": hub,
				"region": str(starter_region),
				"max_chokepoints": str(STARTER_SAFE_MAX_CHOKEPOINTS),
			})

func _check_early_loops_min3(seed: int, g: Dictionary, failures: Array) -> void:
	var starter_region := _pick_starter_region(g)
	if starter_region == -1:
		_fail(failures, seed, INV_EARLY_LOOPS_MIN3, "ALL", {"reason": "no_regions"})
		return

	var starter_nodes: Array[String] = _nodes_in_region(g, starter_region)
	if starter_nodes.size() < 3:
		_fail(failures, seed, INV_EARLY_LOOPS_MIN3, "REGION_%d" % starter_region, {
			"reason": "insufficient_nodes_for_loop",
			"count": str(starter_nodes.size()),
		})
		return

	# Induce adjacency within starter region only.
	var starter_set: Dictionary = {}
	for n in starter_nodes:
		starter_set[n] = true

	var adj_all: Dictionary = g.get("adj", {})
	var adj: Dictionary = {}
	for n in starter_nodes:
		var neigh: Array = adj_all.get(n, [])
		var filtered: Array[String] = []
		for m in neigh:
			if starter_set.has(String(m)):
				filtered.append(String(m))
		filtered.sort()
		adj[n] = filtered

	# Count unique simple cycles (length 3..8) in starter region. Canonicalize to avoid duplicates.
	var cycles: Dictionary = _enumerate_cycles(adj, starter_nodes, 3, 8, 64)
	var cycle_count := cycles.size()

	if cycle_count < EARLY_LOOP_MIN:
		_fail(failures, seed, INV_EARLY_LOOPS_MIN3, "REGION_%d" % starter_region, {
			"cycle_count": str(cycle_count),
			"required": str(EARLY_LOOP_MIN),
		})

func _check_starter_region_world_classes(seed: int, out: Dictionary, g: Dictionary, failures: Array) -> void:
	var starter_region := _pick_starter_region(g)
	if starter_region == -1:
		_fail(failures, seed, INV_STARTER_REGION_WORLD_CLASSES, "ALL", {"reason": "no_regions"})
		return

	var starter_nodes: Array[String] = _nodes_in_region(g, starter_region)
	if starter_nodes.size() == 0:
		_fail(failures, seed, INV_STARTER_REGION_WORLD_CLASSES, "REGION_%d" % starter_region, {"reason": "no_starter_nodes"})
		return

	# Build NodeId -> WorldClassId map from emitted node_classes.
	var node_classes: Array = out.get("node_classes", [])
	var cls_by_node: Dictionary = {}
	for nc in node_classes:
		var nid := String(nc.get("NodeId", ""))
		var cid := String(nc.get("WorldClassId", ""))
		if nid != "":
			cls_by_node[nid] = cid

	var present: Dictionary = {}
	for n in starter_nodes:
		var nid := String(n)
		var cid := String(cls_by_node.get(nid, ""))
		if cid != "":
			present[cid] = true

	var required: Array[String] = ["CORE", "FRONTIER", "RIM"]
	var missing: Array[String] = []
	for r in required:
		if not present.has(r):
			missing.append(r)
	missing.sort()

	if missing.size() > 0:
		_fail(failures, seed, INV_STARTER_REGION_WORLD_CLASSES, "REGION_%d" % starter_region, {
			"missing": ",".join(missing),
			"starter_nodes": str(starter_nodes.size()),
		})

func _build_graph(stars: Array, lanes: Array) -> Dictionary:
	var nodes: Array[String] = []
	var region_by_node: Dictionary = {}
	for s in stars:
		var sid := String(s.get("id", ""))
		if sid == "":
			continue
		nodes.append(sid)
		region_by_node[sid] = int(s.get("region", -1))

	nodes.sort()

	var adj: Dictionary = {}
	for n in nodes:
		adj[n] = []

	for l in lanes:
		var u := String(l.get("u", ""))
		var v := String(l.get("v", ""))
		if u == "" or v == "":
			continue
		if not adj.has(u) or not adj.has(v):
			# Ignore edges to unknown nodes; topology should be self-contained.
			continue
		adj[u].append(v)
		adj[v].append(u)

	for n in nodes:
		var neigh: Array = adj.get(n, [])
		var uniq: Dictionary = {}
		for m in neigh:
			uniq[String(m)] = true

		var cleaned_any = uniq.keys()
		var cleaned_src: Array = cleaned_any as Array
		var cleaned: Array = []
		for k in cleaned_src:
			cleaned.append(String(k))
		cleaned.sort()

		adj[n] = cleaned

	return {
		"nodes": nodes,
		"adj": adj,
		"region_by_node": region_by_node,
	}

func _pick_starter_region(g: Dictionary) -> int:
	var region_by_node: Dictionary = g.get("region_by_node", {})
	if region_by_node.size() == 0:
		return -1

	# Deterministic: choose the smallest region id present.
	var regions: Dictionary = {}
	for k in region_by_node.keys():
		var r := int(region_by_node.get(k, -1))
		if r >= 0:
			regions[str(r)] = true
	if regions.size() == 0:
		return -1
	var keys_any = regions.keys()
	var keys_src: Array = keys_any as Array
	var keys: Array = []
	for k2 in keys_src:
		keys.append(String(k2))
	keys.sort_custom(func(a, b): return int(a) < int(b))
	return int(keys[0])

func _nodes_in_region(g: Dictionary, region_id: int) -> Array[String]:
	var nodes_any = g.get("nodes", [])
	var nodes: Array = nodes_any as Array
	var region_by_node: Dictionary = g.get("region_by_node", {})
	var out: Array[String] = []
	for n in nodes:
		var ns := String(n)
		if int(region_by_node.get(ns, -1)) == region_id:
			out.append(ns)
	return out

func _bfs_reachable(start: String, adj: Dictionary) -> Dictionary:
	var visited: Dictionary = {}
	var q: Array[String] = [start]
	visited[start] = true

	while q.size() > 0:
		var cur: String = q.pop_front()
		var neigh_any = adj.get(cur, [])
		var neigh: Array = neigh_any as Array
		for n in neigh:
			var ns := String(n)
			if not visited.has(ns):
				visited[ns] = true
				q.append(ns)

	return visited

func _exists_path_with_chokepoint_budget(src: String, dst: String, adj: Dictionary, chokepoints: Dictionary, max_cp: int) -> bool:
	# BFS on state (node, cp_used). Chokepoint is counted when entering a chokepoint node (excluding src).
	var visited: Dictionary = {}
	var q: Array = []
	q.append({"n": src, "cp": 0})
	visited[_state_key(src, 0)] = true

	while q.size() > 0:
		var cur: Dictionary = q.pop_front()
		var n := String(cur.get("n", ""))
		var cp := int(cur.get("cp", 0))

		if n == dst:
			return true

		var neigh: Array = adj.get(n, [])
		for m in neigh:
			var ms := String(m)
			var add := 0
			if ms != src and chokepoints.has(ms):
				add = 1
			var next_cp := cp + add
			if next_cp > max_cp:
				continue
			var k := _state_key(ms, next_cp)
			if not visited.has(k):
				visited[k] = true
				q.append({"n": ms, "cp": next_cp})

	return false

func _state_key(n: String, cp: int) -> String:
	return "%s|%d" % [n, cp]

func _articulation_points(nodes: Array, adj: Dictionary) -> Dictionary:
	# Standard DFS articulation points (Tarjan) with deterministic traversal order.
	var visited: Dictionary = {}
	var disc: Dictionary = {}
	var low: Dictionary = {}
	var parent: Dictionary = {}
	var ap: Dictionary = {}
	var time := 0

	for n_any in nodes:
		var n := String(n_any)
		if not visited.has(n):
			time = _ap_dfs(n, visited, disc, low, parent, ap, adj, time)

	return ap

func _ap_dfs(u: String, visited: Dictionary, disc: Dictionary, low: Dictionary, parent: Dictionary, ap: Dictionary, adj: Dictionary, time: int) -> int:
	visited[u] = true
	time += 1
	disc[u] = time
	low[u] = time

	var children := 0
	var neigh: Array = adj.get(u, [])
	# neigh already sorted by _build_graph.
	for v_any in neigh:
		var v := String(v_any)
		if not visited.has(v):
			children += 1
			parent[v] = u
			time = _ap_dfs(v, visited, disc, low, parent, ap, adj, time)
			low[u] = min(int(low.get(u, time)), int(low.get(v, time)))

			# Case 1: u is root of DFS tree and has two or more children.
			if not parent.has(u) and children > 1:
				ap[u] = true

			# Case 2: u is not root and low value of one child is >= discovery time of u.
			if parent.has(u) and int(low.get(v, time)) >= int(disc.get(u, time)):
				ap[u] = true
		elif parent.get(u, "") != v:
			low[u] = min(int(low.get(u, time)), int(disc.get(v, time)))

	return time

func _enumerate_cycles(adj: Dictionary, nodes: Array[String], min_len: int, max_len: int, max_cycles: int) -> Dictionary:
	# Enumerate cycles deterministically by anchoring each cycle at its lexicographically smallest node.
	# cycles dict key is canonical string "n1>n2>...>n1" with canonical rotation and direction.
	var cycles: Dictionary = {}

	var nodes_sorted := nodes.duplicate()
	nodes_sorted.sort()

	for start in nodes_sorted:
		# DFS paths starting at start; only visit nodes >= start to ensure canonical smallest node anchor.
		var stack: Array = []
		stack.append({"path": [start], "seen": {start: true}})

		while stack.size() > 0:
			var cur: Dictionary = stack.pop_back()
			var path: Array = cur.get("path", [])
			var seen: Dictionary = cur.get("seen", {})
			var last := String(path[path.size() - 1])

			var neigh: Array = adj.get(last, [])
			for n_any in neigh:
				var n := String(n_any)

				if n == start and path.size() >= min_len and path.size() <= max_len:
					var key := _canon_cycle_key(path)
					cycles[key] = true
					if cycles.size() >= max_cycles:
						return cycles
					continue

				if seen.has(n):
					continue
				if n < start:
					continue
				if path.size() >= max_len:
					continue

				var next_path := path.duplicate()
				next_path.append(n)
				var next_seen := seen.duplicate()
				next_seen[n] = true
				stack.append({"path": next_path, "seen": next_seen})

	return cycles

func _canon_cycle_key(path: Array) -> String:
	# path is like [start, ..., x] and implies closure back to start.
	# Canonicalize direction by comparing forward vs reversed (excluding implicit closure).
	var fwd: Array[String] = []
	for p in path:
		fwd.append(String(p))

	var rev: Array[String] = []
	rev.append(fwd[0])
	for i in range(fwd.size() - 1, 0, -1):
		rev.append(fwd[i])

	var a := ">".join(fwd) + ">" + fwd[0]
	var b := ">".join(rev) + ">" + rev[0]
	if b < a:
		return b
	return a

func _fail(failures: Array, seed: int, inv: String, primary_id: String, details_kv: Dictionary) -> void:
	var kv_str := _format_kv(details_kv)
	failures.append({
		"Seed": seed,
		"InvariantName": inv,
		"PrimaryId": primary_id,
		"DetailsKV": kv_str,
	})

func _format_kv(kv: Dictionary) -> String:
	if kv.size() == 0:
		return ""
	var keys: Array[String] = []
	for k in kv.keys():
		keys.append(String(k))
	keys.sort()

	var parts: Array[String] = []
	for k in keys:
		parts.append("%s=%s" % [k, String(kv.get(k, ""))])
	return ",".join(parts)

func _emit_failure_report(failures: Array) -> void:
	if failures.size() == 0:
		return

	# Stable sort: InvariantName, PrimaryId, Seed
	failures.sort_custom(func(a, b):
		var ai := String(a.get("InvariantName", ""))
		var bi := String(b.get("InvariantName", ""))
		if ai != bi:
			return ai < bi
		var ap := String(a.get("PrimaryId", ""))
		var bp := String(b.get("PrimaryId", ""))
		if ap != bp:
			return ap < bp
		return int(a.get("Seed", 0)) < int(b.get("Seed", 0))
	)

	print("FAILURE_REPORT_BEGIN")
	for r in failures:
		print("Seed=%d|InvariantName=%s|PrimaryId=%s|DetailsKV=%s" % [
			int(r.get("Seed", 0)),
			String(r.get("InvariantName", "")),
			String(r.get("PrimaryId", "")),
			String(r.get("DetailsKV", "")),
		])
	print("FAILURE_REPORT_END")

func _assert(condition: bool, message: String):
	if condition:
		print("[PASS] " + message)
	else:
		printerr("[FAIL] " + message)
