# Edgedar: screen-edge direction indicators for off-screen POIs.
# Inspired by Everspace 2 / Elite Dangerous. Shows chevron indicators at viewport
# edges pointing toward lane gates, fleets, stations, planets, and quest targets.
# Features: anti-aliased chevrons, ellipse edge positioning, same-type clustering,
# entry scale animation, asymmetric threat pulse, rotation smoothing.
extends CanvasLayer

const MAX_INDICATORS: int = 20
const EDGE_MARGIN: float = 40.0  # px inset from screen edge
const MIN_DISTANCE: float = 20.0  # hide indicator if POI closer than this (units)
const MAX_DISTANCE: float = 500.0  # fade indicator beyond this distance
const ARROW_SIZE: float = 32.0  # Base chevron size — must be readable from couch distance.
const ARROW_SIZE_HIGHLIGHT: float = 44.0  # Highlighted/quest targets — prominent.
const POI_REFRESH_INTERVAL: float = 0.25
const CLUSTER_RADIUS_PX: float = 30.0  # merge same-type indicators within this distance

enum PoiType { LANE_GATE, HOSTILE_FLEET, QUEST_TARGET, STATION, PLANET, FRIENDLY_FLEET, GATE_HIGHLIGHT }

var _poi_colors: Dictionary = {
	PoiType.LANE_GATE: Color(0.45, 0.75, 1.0, 0.85),     # bright blue
	PoiType.HOSTILE_FLEET: Color(1.0, 0.2, 0.15, 0.95),   # vivid red
	PoiType.QUEST_TARGET: Color(1.0, 0.85, 0.3, 1.0),     # gold
	PoiType.STATION: Color(0.3, 1.0, 0.5, 0.85),          # green
	PoiType.PLANET: Color(0.55, 0.6, 0.65, 0.55),         # gray (subtle but visible)
	PoiType.FRIENDLY_FLEET: Color(0.35, 0.75, 0.85, 0.65),# cyan
	PoiType.GATE_HIGHLIGHT: Color(1.0, 0.9, 0.3, 1.0),    # bright gold
}

var _indicators: Array = []  # Array of {root: Control, arrow: ChevronDraw, label: Label}
var _game_manager = null
var _poi_cache: Array = []
var _poi_refresh_timer: float = 0.0
# Tutorial/destination trade waypoint: set by tutorial_director or route planner.
var tutorial_target_node_id: String = ""
# General destination: set by player (e.g., via galaxy map route selection).
var destination_node_id: String = ""

# Per-indicator state for smooth animations.
var _indicator_angles: Array = []    # Smoothed rotation angles.
var _indicator_spawn_t: Array = []   # Time each indicator last became visible.
var _indicator_was_visible: Array = []  # Previous frame visibility.

var _debug_timer: float = 0.0


func _ready() -> void:
	layer = 10
	_game_manager = get_node_or_null("/root/GameManager")
	_build_indicator_pool()
	# Initialize per-indicator animation state.
	for i in range(MAX_INDICATORS):
		_indicator_angles.append(0.0)
		_indicator_spawn_t.append(0.0)
		_indicator_was_visible.append(false)
	print("DEBUG_EDGEDAR|READY|indicators=%d" % _indicators.size())


func _physics_process(delta: float) -> void:
	# Periodic debug logging (every 3s).
	_debug_timer += delta
	if _debug_timer >= 3.0:
		_debug_timer = 0.0
		var cam = get_viewport().get_camera_3d() if get_viewport() else null
		var gates = get_tree().get_nodes_in_group("LaneGate") if get_tree() else []
		var stations = get_tree().get_nodes_in_group("Station") if get_tree() else []
		print("DEBUG_EDGEDAR|TICK|show=%s|tut_target=%s|cam=%s|gates=%d|stations=%d|pois=%d|gm=%s" % [
			_should_show(), tutorial_target_node_id,
			cam != null, gates.size(), stations.size(), _poi_cache.size(),
			_game_manager != null])

	if not _should_show():
		_hide_all()
		return

	var camera: Camera3D = get_viewport().get_camera_3d()
	if camera == null:
		_hide_all()
		return

	# Refresh POI list periodically (not every frame).
	_poi_refresh_timer += delta
	if _poi_refresh_timer >= POI_REFRESH_INTERVAL:
		_poi_refresh_timer = 0.0
		_poi_cache = _gather_pois(camera)

	_update_indicators(camera, delta)


func _should_show() -> bool:
	if _game_manager == null:
		_game_manager = get_node_or_null("/root/GameManager")
	if _game_manager == null:
		return false
	# Hide during galaxy overlay (map open).
	var overlay = _game_manager.get("galaxy_overlay_open")
	if overlay != null and bool(overlay):
		return false
	# Always show if tutorial/destination waypoint is active (even while docked).
	if not tutorial_target_node_id.is_empty() or not destination_node_id.is_empty():
		return true
	# Otherwise only show in flight.
	var state = _game_manager.get("current_player_state")
	if state == null or int(state) != 0:  # Only IN_FLIGHT
		return false
	return true


func _gather_pois(camera: Camera3D) -> Array:
	var cam_pos: Vector3 = camera.global_position
	var result: Array = []

	# Determine which gate is the "highlighted" destination.
	var highlight_target: String = ""
	if not tutorial_target_node_id.is_empty():
		highlight_target = tutorial_target_node_id
	elif not destination_node_id.is_empty():
		highlight_target = destination_node_id

	# ── Lane Gates ────────────────────────────────────────────────────
	for node in get_tree().get_nodes_in_group("LaneGate"):
		if not (node is Node3D):
			continue
		var dist: float = cam_pos.distance_to(node.global_position)
		if dist < MIN_DISTANCE or dist > MAX_DISTANCE:
			continue

		var neighbor_id: String = str(node.get_meta("neighbor_node_id", ""))
		var is_highlight: bool = (not highlight_target.is_empty() and neighbor_id == highlight_target)

		# Label: shortened neighbor name + distance.
		var lbl_text: String = neighbor_id
		if lbl_text.contains("_"):
			lbl_text = lbl_text.rsplit("_", true, 1)[-1]
		var lbl_with_dist: String = "%s %d AU" % [lbl_text, int(dist)]

		if is_highlight:
			# Highlighted destination gate — gold, large, always on top.
			result.append({
				"pos": node.global_position,
				"type": PoiType.GATE_HIGHLIGHT,
				"dist": dist,
				"label": lbl_with_dist,
				"priority": 0,  # highest
			})
		else:
			result.append({
				"pos": node.global_position,
				"type": PoiType.LANE_GATE,
				"dist": dist,
				"label": lbl_with_dist,
				"priority": 3,
			})

	# ── Stations ──────────────────────────────────────────────────────
	for node in get_tree().get_nodes_in_group("Station"):
		if not (node is Node3D):
			continue
		var dist: float = cam_pos.distance_to(node.global_position)
		if dist < MIN_DISTANCE or dist > MAX_DISTANCE:
			continue
		var station_name: String = "Station"
		if node.has_meta("station_name"):
			station_name = str(node.get_meta("station_name"))
		result.append({
			"pos": node.global_position,
			"type": PoiType.STATION,
			"dist": dist,
			"label": "%s %d AU" % [station_name, int(dist)],
			"priority": 2,
		})

	# ── Planets ───────────────────────────────────────────────────────
	for node in get_tree().get_nodes_in_group("Planet"):
		if not (node is Node3D):
			continue
		var dist: float = cam_pos.distance_to(node.global_position)
		if dist < MIN_DISTANCE or dist > MAX_DISTANCE:
			continue
		result.append({
			"pos": node.global_position,
			"type": PoiType.PLANET,
			"dist": dist,
			"label": "%d AU" % int(dist),
			"priority": 5,
		})

	# ── Fleet Ships (NPC) ─────────────────────────────────────────────
	for node in get_tree().get_nodes_in_group("FleetShip"):
		if not (node is Node3D):
			continue
		var dist: float = cam_pos.distance_to(node.global_position)
		if dist < MIN_DISTANCE or dist > MAX_DISTANCE:
			continue
		var is_hostile: bool = bool(node.get_meta("is_hostile", false))
		var fleet_type: int = PoiType.HOSTILE_FLEET if is_hostile else PoiType.FRIENDLY_FLEET
		var lbl: String = "%d AU" % int(dist)
		result.append({
			"pos": node.global_position,
			"type": fleet_type,
			"dist": dist,
			"label": lbl,
			"priority": 1 if is_hostile else 4,
		})

	# ── Mission Objective Waypoint ────────────────────────────────────
	if _game_manager != null:
		var bridge = get_node_or_null("/root/SimBridge")
		if bridge and bridge.has_method("GetActiveMissionV0"):
			var mission: Dictionary = bridge.call("GetActiveMissionV0")
			var target_id: String = str(mission.get("target_node_id", ""))
			if not target_id.is_empty():
				var obj_text: String = str(mission.get("objective_text", ""))
				var obj_name: String = str(mission.get("target_node_name", "Objective"))
				if obj_text.is_empty():
					obj_text = obj_name
				# Shorten label: use target name only (objective text is shown in HUD).
				var short_label: String = obj_name if obj_name.length() <= 20 else obj_name.left(17) + "..."
				var quest_found: bool = false

				# First: check if the target is in the current system (station name = "LocalStation_<nodeId>").
				for station in get_tree().get_nodes_in_group("Station"):
					if not (station is Node3D):
						continue
					# Station Name format: "LocalStation_node_3" — check if it ends with target_id.
					if station.name.ends_with(target_id):
						var dist: float = cam_pos.distance_to(station.global_position)
						if dist >= MIN_DISTANCE and dist <= MAX_DISTANCE:
							result.append({
								"pos": station.global_position,
								"type": PoiType.QUEST_TARGET,
								"dist": dist,
								"label": "%s %d AU" % [short_label, int(dist)],
								"priority": 0,
							})
							quest_found = true
						break

				# Second: if not found locally, look for a gate leading to the target system.
				if not quest_found:
					# Skip if a GATE_HIGHLIGHT already points to this gate (avoid duplicate).
					var already_highlighted: bool = (highlight_target == target_id)
					if not already_highlighted:
						for gate in get_tree().get_nodes_in_group("LaneGate"):
							var gate_target: String = str(gate.get_meta("neighbor_node_id", ""))
							if gate_target == target_id:
								var dist: float = cam_pos.distance_to(gate.global_position)
								if dist >= MIN_DISTANCE and dist <= MAX_DISTANCE:
									result.append({
										"pos": gate.global_position,
										"type": PoiType.QUEST_TARGET,
										"dist": dist,
										"label": "%s %d AU" % [short_label, int(dist)],
										"priority": 0,
									})
								break

	# Sort by priority (lowest first = most important), then by distance.
	result.sort_custom(func(a, b):
		if a.get("priority", 5) != b.get("priority", 5):
			return a.get("priority", 5) < b.get("priority", 5)
		return a["dist"] < b["dist"]
	)

	# Cluster same-type indicators that project to nearby screen positions.
	result = _cluster_pois(result, camera)

	return result


func _cluster_pois(pois: Array, camera: Camera3D) -> Array:
	if pois.size() <= 1:
		return pois
	var vp_size: Vector2 = get_viewport().get_visible_rect().size
	var center: Vector2 = vp_size * 0.5
	var consumed: Dictionary = {}
	var clustered: Array = []

	for i in range(pois.size()):
		if consumed.has(i):
			continue
		var a: Dictionary = pois[i]
		var a_screen: Vector2 = _project_poi_screen(camera, a, center, vp_size)
		var group_count: int = 1

		for j in range(i + 1, pois.size()):
			if consumed.has(j):
				continue
			var b: Dictionary = pois[j]
			if b["type"] != a["type"]:
				continue
			var b_screen: Vector2 = _project_poi_screen(camera, b, center, vp_size)
			if a_screen.distance_to(b_screen) < CLUSTER_RADIUS_PX:
				group_count += 1
				consumed[j] = true

		if group_count > 1:
			var merged: Dictionary = a.duplicate()
			merged["label"] = "\u00d7%d" % group_count
			clustered.append(merged)
		else:
			clustered.append(a)
		consumed[i] = true

	return clustered


func _project_poi_screen(camera: Camera3D, poi: Dictionary, center: Vector2, vp_size: Vector2) -> Vector2:
	var screen_pos: Vector2 = camera.unproject_position(poi["pos"])
	if camera.is_position_behind(poi["pos"]):
		screen_pos = center + (center - screen_pos).normalized() * maxf(vp_size.x, vp_size.y)
	return _clamp_to_ellipse_edge(center, (screen_pos - center).normalized(), vp_size)


func _update_indicators(camera: Camera3D, delta: float) -> void:
	var vp_size: Vector2 = get_viewport().get_visible_rect().size
	var inner_rect := Rect2(
		Vector2(EDGE_MARGIN, EDGE_MARGIN),
		vp_size - Vector2(EDGE_MARGIN * 2, EDGE_MARGIN * 2)
	)
	var center: Vector2 = vp_size * 0.5
	var now: float = Time.get_ticks_msec() / 1000.0

	for i in range(MAX_INDICATORS):
		if i >= _poi_cache.size():
			_indicators[i]["root"].visible = false
			_indicator_was_visible[i] = false
			continue

		var poi: Dictionary = _poi_cache[i]
		var world_pos: Vector3 = poi["pos"]
		var poi_type: int = poi["type"]
		var dist: float = poi["dist"]

		# Project to screen space.
		var screen_pos: Vector2 = camera.unproject_position(world_pos)
		var is_behind: bool = camera.is_position_behind(world_pos)

		# If behind camera, flip to opposite side.
		if is_behind:
			screen_pos = center + (center - screen_pos).normalized() * maxf(vp_size.x, vp_size.y)

		var indicator: Dictionary = _indicators[i]
		var color: Color = _poi_colors.get(poi_type, Color.WHITE)

		# Distance-based alpha fade (except for highlighted targets).
		var alpha: float = color.a
		if poi_type != PoiType.GATE_HIGHLIGHT and poi_type != PoiType.QUEST_TARGET:
			if dist > MAX_DISTANCE * 0.7:
				alpha = clampf(color.a * (1.0 - (dist - MAX_DISTANCE * 0.7) / (MAX_DISTANCE * 0.3)), 0.15, color.a)

		# Distance-based size scaling: 100% at MIN_DISTANCE → 80% at MAX_DISTANCE.
		var dist_scale: float = lerpf(1.0, 0.8, clampf((dist - MIN_DISTANCE) / (MAX_DISTANCE - MIN_DISTANCE), 0.0, 1.0))

		# Pulse animations — type-specific.
		if poi_type == PoiType.HOSTILE_FLEET:
			# Asymmetric threat pulse: fast rise, slow decay (urgent feel).
			var pulse_t: float = fmod(Time.get_ticks_msec() * 0.005, TAU)
			var pulse: float = pow(0.5 + 0.5 * sin(pulse_t), 0.4)
			alpha *= pulse
		elif poi_type == PoiType.GATE_HIGHLIGHT:
			# Slow calm breathe for navigation waypoints (0.3 Hz).
			var pulse: float = 0.85 + 0.15 * sin(Time.get_ticks_msec() * 0.002)
			alpha *= pulse
		elif poi_type == PoiType.QUEST_TARGET:
			# Moderate pulse for quest targets.
			var pulse: float = 0.75 + 0.25 * sin(Time.get_ticks_msec() * 0.004)
			alpha *= pulse

		# Arrow size: larger for highlighted/quest targets.
		var sz: float = ARROW_SIZE
		if poi_type == PoiType.GATE_HIGHLIGHT or poi_type == PoiType.QUEST_TARGET:
			sz = ARROW_SIZE_HIGHLIGHT
		sz *= dist_scale

		# Entry scale animation: scale from 0 → full over 0.2s when indicator first appears.
		if not _indicator_was_visible[i]:
			_indicator_spawn_t[i] = now
		var entry_t: float = clampf((now - _indicator_spawn_t[i]) / 0.2, 0.0, 1.0)
		sz *= entry_t
		alpha *= entry_t

		# Update chevron draw properties.
		indicator["arrow"].arrow_size = sz
		indicator["arrow"].is_double = (poi_type == PoiType.GATE_HIGHLIGHT or poi_type == PoiType.QUEST_TARGET)

		if inner_rect.has_point(screen_pos) and not is_behind:
			# POI is on-screen — hide indicator (it's visible in 3D).
			# Exception: highlighted gates show a subtle on-screen marker.
			if poi_type == PoiType.GATE_HIGHLIGHT or poi_type == PoiType.QUEST_TARGET:
				indicator["root"].visible = true
				var pivot: Vector2 = indicator["arrow"].pivot_offset
				indicator["root"].position = screen_pos - pivot
				indicator["root"].modulate = Color(color.r, color.g, color.b, alpha * 0.5)
				indicator["arrow"].rotation = 0
				indicator["arrow"].queue_redraw()
				indicator["label"].text = poi.get("label", "")
				_indicator_was_visible[i] = true
			else:
				indicator["root"].visible = false
				_indicator_was_visible[i] = false
		else:
			# Clamp to screen edge using ellipse (smooth corners).
			var dir: Vector2 = (screen_pos - center).normalized()
			var clamped: Vector2 = _clamp_to_ellipse_edge(center, dir, vp_size)

			indicator["root"].visible = true
			var pivot: Vector2 = indicator["arrow"].pivot_offset
			indicator["root"].position = clamped - pivot
			indicator["root"].modulate = Color(color.r, color.g, color.b, alpha)

			# Smooth rotation toward actual direction.
			var target_angle: float = dir.angle()
			_indicator_angles[i] = lerp_angle(_indicator_angles[i], target_angle, delta * 10.0)
			indicator["arrow"].rotation = _indicator_angles[i]
			indicator["arrow"].queue_redraw()

			indicator["label"].text = poi.get("label", "")
			_indicator_was_visible[i] = true


func _clamp_to_ellipse_edge(center: Vector2, dir: Vector2, vp_size: Vector2) -> Vector2:
	# Ellipse inscribed within the safe zone — smooth corner transitions.
	var margin := Vector2(EDGE_MARGIN + 20.0, EDGE_MARGIN + 20.0)
	var radii: Vector2 = (vp_size * 0.5) - margin
	var angle: float = dir.angle()
	var ellipse_point: Vector2 = center + Vector2(cos(angle) * radii.x, sin(angle) * radii.y)
	# Clamp to stay within 8px of the actual screen edge (prevents ultrawide disconnect).
	var edge_max := Rect2(Vector2(8, 8), vp_size - Vector2(16, 16))
	ellipse_point.x = clampf(ellipse_point.x, edge_max.position.x, edge_max.end.x)
	ellipse_point.y = clampf(ellipse_point.y, edge_max.position.y, edge_max.end.y)
	return ellipse_point


func _build_indicator_pool() -> void:
	var pool_sz := Vector2(ARROW_SIZE_HIGHLIGHT * 3 + 100, ARROW_SIZE_HIGHLIGHT * 3)
	for i in range(MAX_INDICATORS):
		var root := Control.new()
		root.name = "Indicator%d" % i
		root.visible = false
		root.mouse_filter = Control.MOUSE_FILTER_IGNORE
		root.size = pool_sz
		root.custom_minimum_size = pool_sz
		root.clip_contents = false
		add_child(root)

		var arrow := ChevronDraw.new()
		arrow.name = "Arrow"
		arrow.arrow_size = ARROW_SIZE
		arrow.size = Vector2(ARROW_SIZE_HIGHLIGHT * 3, ARROW_SIZE_HIGHLIGHT * 3)
		arrow.custom_minimum_size = Vector2(ARROW_SIZE_HIGHLIGHT * 3, ARROW_SIZE_HIGHLIGHT * 3)
		arrow.position = Vector2.ZERO
		arrow.mouse_filter = Control.MOUSE_FILTER_IGNORE
		arrow.pivot_offset = Vector2(ARROW_SIZE_HIGHLIGHT * 1.5, ARROW_SIZE_HIGHLIGHT * 1.5)
		root.add_child(arrow)

		var lbl := Label.new()
		lbl.name = "DistLabel"
		lbl.add_theme_font_size_override("font_size", 14)
		lbl.add_theme_color_override("font_color", Color.WHITE)
		lbl.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.7))
		lbl.add_theme_constant_override("shadow_offset_x", 1)
		lbl.add_theme_constant_override("shadow_offset_y", 1)
		lbl.position = Vector2(ARROW_SIZE_HIGHLIGHT * 3 + 4, 8)
		lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
		root.add_child(lbl)

		_indicators.append({"root": root, "arrow": arrow, "label": lbl})


func _hide_all() -> void:
	for i in range(_indicators.size()):
		_indicators[i]["root"].visible = false
		_indicator_was_visible[i] = false


# Polished edge indicator: filled chevron with colored glow and bright outline.
# Parent modulate sets the POI color. Chevron draws in that color with proper layers.
# Double-chevron variant for highlighted/quest targets (Everspace 2 style).
class ChevronDraw extends Control:
	var arrow_size: float = 26.0
	var is_double: bool = false  # Double chevron for highlighted targets.

	func _draw() -> void:
		if arrow_size < 2.0:
			return
		var sz: float = arrow_size
		# Draw at the pivot center so rotation works correctly.
		var cx: float = pivot_offset.x
		var cy: float = pivot_offset.y

		# Draw in WHITE — parent's modulate provides the POI color tint.
		# This avoids double-multiplication (modulate already tints all child draws).

		# Chevron pointing right: filled polygon with slight notch at back.
		var tip := Vector2(cx + sz * 0.9, cy)
		var top_out := Vector2(cx - sz * 0.5, cy - sz * 0.7)
		var top_in := Vector2(cx - sz * 0.1, cy - sz * 0.08)
		var bot_in := Vector2(cx - sz * 0.1, cy + sz * 0.08)
		var bot_out := Vector2(cx - sz * 0.5, cy + sz * 0.7)

		# === Layer 1: Outer glow (large, soft halo) ===
		var glow_scale: float = 1.5
		var glow_pts := PackedVector2Array()
		for pt in [tip, top_out, top_in, bot_in, bot_out]:
			glow_pts.append(Vector2(cx + (pt.x - cx) * glow_scale, cy + (pt.y - cy) * glow_scale))
		var glow_colors := PackedColorArray()
		for _k in range(5):
			glow_colors.append(Color(1.0, 1.0, 1.0, 0.2))
		draw_polygon(glow_pts, glow_colors)

		# === Layer 2: Filled chevron body (strong, readable) ===
		var fill_pts := PackedVector2Array([tip, top_out, top_in, bot_in, bot_out])
		var fill_colors := PackedColorArray()
		for _k in range(5):
			fill_colors.append(Color(1.0, 1.0, 1.0, 0.75))
		draw_polygon(fill_pts, fill_colors)

		# === Layer 3: Bright outline (AA'd polyline, crisp edges) ===
		var outline_pts := PackedVector2Array([top_out, tip, bot_out])
		draw_polyline(outline_pts, Color(1.0, 1.0, 1.0, 1.0), 2.5, true)

		# === Layer 4: Inner bright core (hot center glow) ===
		var core_scale: float = 0.5
		var core_pts := PackedVector2Array()
		for pt in [tip, top_out, top_in, bot_in, bot_out]:
			core_pts.append(Vector2(cx + (pt.x - cx) * core_scale, cy + (pt.y - cy) * core_scale))
		var core_colors := PackedColorArray()
		for _k in range(5):
			core_colors.append(Color(1.0, 1.0, 1.0, 0.55))
		draw_polygon(core_pts, core_colors)

		# === Double chevron: second V behind the first for highlighted targets ===
		if is_double:
			var back_offset: float = -sz * 0.55
			var d_pts := PackedVector2Array()
			for pt in [tip, top_out, bot_out]:
				d_pts.append(Vector2(pt.x + back_offset, pt.y))
			draw_polyline(d_pts, Color(1.0, 1.0, 1.0, 0.6), 2.0, true)
