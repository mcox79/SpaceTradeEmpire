# Edgedar: screen-edge direction indicators for off-screen POIs.
# Inspired by Starcom: Nexus. Shows arrows at viewport edges pointing toward
# lane gates, fleets, stations, planets, and quest targets.
extends CanvasLayer

const MAX_INDICATORS: int = 20
const EDGE_MARGIN: float = 40.0  # px inset from screen edge
const MIN_DISTANCE: float = 20.0  # hide indicator if POI closer than this (units)
const MAX_DISTANCE: float = 500.0  # fade indicator beyond this distance
const ARROW_SIZE: float = 12.0
const ARROW_SIZE_HIGHLIGHT: float = 18.0  # larger for highlighted/quest targets
const POI_REFRESH_INTERVAL: float = 0.25

enum PoiType { LANE_GATE, HOSTILE_FLEET, QUEST_TARGET, STATION, PLANET, FRIENDLY_FLEET, GATE_HIGHLIGHT }

var _poi_colors: Dictionary = {
	PoiType.LANE_GATE: Color(0.4, 0.7, 1.0, 0.7),       # blue (dim)
	PoiType.HOSTILE_FLEET: Color(1.0, 0.15, 0.15, 0.9),  # red
	PoiType.QUEST_TARGET: Color(1.0, 0.85, 0.4, 1.0),    # gold
	PoiType.STATION: Color(0.2, 1.0, 0.4, 0.7),          # green
	PoiType.PLANET: Color(0.5, 0.55, 0.6, 0.4),          # gray (subtle)
	PoiType.FRIENDLY_FLEET: Color(0.3, 0.7, 0.8, 0.5),   # dim cyan
	PoiType.GATE_HIGHLIGHT: Color(1.0, 0.9, 0.3, 1.0),   # bright gold
}

var _indicators: Array = []  # Array of {root: Control, arrow: ArrowDraw, label: Label}
var _game_manager = null
var _poi_cache: Array = []
var _poi_refresh_timer: float = 0.0
# Tutorial/destination trade waypoint: set by tutorial_director or route planner.
var tutorial_target_node_id: String = ""
# General destination: set by player (e.g., via galaxy map route selection).
var destination_node_id: String = ""


var _debug_timer: float = 0.0

func _ready() -> void:
	layer = 10
	_game_manager = get_node_or_null("/root/GameManager")
	_build_indicator_pool()
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

	_update_indicators(camera)


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

		# Label: shortened neighbor name.
		var lbl_text: String = neighbor_id
		if lbl_text.contains("_"):
			lbl_text = lbl_text.rsplit("_", true, 1)[-1]

		if is_highlight:
			# Highlighted destination gate — gold, large, always on top.
			result.append({
				"pos": node.global_position,
				"type": PoiType.GATE_HIGHLIGHT,
				"dist": dist,
				"label": lbl_text,
				"priority": 0,  # highest
			})
		else:
			result.append({
				"pos": node.global_position,
				"type": PoiType.LANE_GATE,
				"dist": dist,
				"label": lbl_text,
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
			"label": station_name,
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
			"label": "",  # planets don't need labels
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
		var lbl: String = ""
		if is_hostile:
			lbl = "%du" % int(dist)
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
				for gate in get_tree().get_nodes_in_group("LaneGate"):
					var gate_target: String = str(gate.get_meta("neighbor_node_id", ""))
					if gate_target == target_id:
						var dist: float = cam_pos.distance_to(gate.global_position)
						if dist >= MIN_DISTANCE and dist <= MAX_DISTANCE:
							var obj_text: String = str(mission.get("objective_text", "Objective"))
							if obj_text.is_empty():
								obj_text = str(mission.get("target_node_name", "Objective"))
							result.append({
								"pos": gate.global_position,
								"type": PoiType.QUEST_TARGET,
								"dist": dist,
								"label": obj_text,
								"priority": 0,
							})
						break

	# Sort by priority (lowest first = most important), then by distance.
	result.sort_custom(func(a, b):
		if a.get("priority", 5) != b.get("priority", 5):
			return a.get("priority", 5) < b.get("priority", 5)
		return a["dist"] < b["dist"]
	)
	return result


func _update_indicators(camera: Camera3D) -> void:
	var vp_size: Vector2 = get_viewport().get_visible_rect().size
	var inner_rect := Rect2(
		Vector2(EDGE_MARGIN, EDGE_MARGIN),
		vp_size - Vector2(EDGE_MARGIN * 2, EDGE_MARGIN * 2)
	)
	var center: Vector2 = vp_size * 0.5

	for i in range(MAX_INDICATORS):
		if i >= _poi_cache.size():
			_indicators[i]["root"].visible = false
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
				alpha = clampf(color.a * (1.0 - (dist - MAX_DISTANCE * 0.7) / (MAX_DISTANCE * 0.3)), 0.05, color.a)

		# Highlighted targets pulse gently.
		if poi_type == PoiType.GATE_HIGHLIGHT or poi_type == PoiType.QUEST_TARGET:
			var pulse: float = 0.75 + 0.25 * sin(Time.get_ticks_msec() * 0.004)
			alpha *= pulse

		# Arrow size: larger for highlighted/quest targets.
		var sz: float = ARROW_SIZE
		if poi_type == PoiType.GATE_HIGHLIGHT or poi_type == PoiType.QUEST_TARGET:
			sz = ARROW_SIZE_HIGHLIGHT

		if inner_rect.has_point(screen_pos) and not is_behind:
			# POI is on-screen — hide indicator (it's visible in 3D).
			# Exception: highlighted gates show a subtle on-screen diamond marker.
			if poi_type == PoiType.GATE_HIGHLIGHT or poi_type == PoiType.QUEST_TARGET:
				indicator["root"].visible = true
				indicator["root"].position = screen_pos - Vector2(sz, sz)
				indicator["root"].modulate = Color(color.r, color.g, color.b, alpha * 0.5)
				indicator["arrow"].rotation = 0
				indicator["label"].text = poi.get("label", "")
			else:
				indicator["root"].visible = false
		else:
			# Clamp to screen edge.
			var dir: Vector2 = (screen_pos - center).normalized()
			var clamped: Vector2 = _clamp_to_rect_edge(center, dir, inner_rect)

			indicator["root"].visible = true
			indicator["root"].position = clamped - Vector2(sz, sz)
			indicator["root"].modulate = Color(color.r, color.g, color.b, alpha)

			# Rotate arrow to point toward actual position.
			var angle: float = dir.angle()
			indicator["arrow"].rotation = angle
			indicator["label"].text = poi.get("label", "")


func _clamp_to_rect_edge(origin: Vector2, dir: Vector2, rect: Rect2) -> Vector2:
	# Ray-rect intersection to find the edge point.
	var best_t: float = INF
	if absf(dir.x) > 0.001:
		# Left edge
		var t_left: float = (rect.position.x - origin.x) / dir.x
		if t_left > 0 and t_left < best_t:
			var y: float = origin.y + dir.y * t_left
			if y >= rect.position.y and y <= rect.end.y:
				best_t = t_left
		# Right edge
		var t_right: float = (rect.end.x - origin.x) / dir.x
		if t_right > 0 and t_right < best_t:
			var y: float = origin.y + dir.y * t_right
			if y >= rect.position.y and y <= rect.end.y:
				best_t = t_right
	if absf(dir.y) > 0.001:
		# Top edge
		var t_top: float = (rect.position.y - origin.y) / dir.y
		if t_top > 0 and t_top < best_t:
			var x: float = origin.x + dir.x * t_top
			if x >= rect.position.x and x <= rect.end.x:
				best_t = t_top
		# Bottom edge
		var t_bot: float = (rect.end.y - origin.y) / dir.y
		if t_bot > 0 and t_bot < best_t:
			var x: float = origin.x + dir.x * t_bot
			if x >= rect.position.x and x <= rect.end.x:
				best_t = t_bot
	if best_t == INF:
		return origin + dir * 100.0
	return origin + dir * best_t


func _build_indicator_pool() -> void:
	var pool_sz := Vector2(ARROW_SIZE_HIGHLIGHT * 2 + 80, ARROW_SIZE_HIGHLIGHT * 2)
	for i in range(MAX_INDICATORS):
		var root := Control.new()
		root.name = "Indicator%d" % i
		root.visible = false
		root.mouse_filter = Control.MOUSE_FILTER_IGNORE
		root.size = pool_sz
		root.custom_minimum_size = pool_sz
		root.clip_contents = false
		add_child(root)

		var arrow := ArrowDraw.new()
		arrow.name = "Arrow"
		arrow.arrow_size = ARROW_SIZE
		arrow.size = Vector2(ARROW_SIZE_HIGHLIGHT * 2, ARROW_SIZE_HIGHLIGHT * 2)
		arrow.custom_minimum_size = Vector2(ARROW_SIZE_HIGHLIGHT * 2, ARROW_SIZE_HIGHLIGHT * 2)
		arrow.position = Vector2.ZERO
		arrow.mouse_filter = Control.MOUSE_FILTER_IGNORE
		arrow.pivot_offset = Vector2(ARROW_SIZE_HIGHLIGHT, ARROW_SIZE_HIGHLIGHT)
		root.add_child(arrow)

		var lbl := Label.new()
		lbl.name = "DistLabel"
		lbl.add_theme_font_size_override("font_size", 11)
		lbl.add_theme_color_override("font_color", Color.WHITE)
		lbl.position = Vector2(ARROW_SIZE_HIGHLIGHT * 2 + 2, 2)
		lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
		root.add_child(lbl)

		_indicators.append({"root": root, "arrow": arrow, "label": lbl})


func _hide_all() -> void:
	for indicator in _indicators:
		indicator["root"].visible = false


# Simple arrow triangle drawn via _draw().
class ArrowDraw extends Control:
	var arrow_size: float = 12.0

	func _draw() -> void:
		var points := PackedVector2Array([
			Vector2(arrow_size * 2, arrow_size),       # tip (right)
			Vector2(0, 0),                             # top-left
			Vector2(0, arrow_size * 2),                # bottom-left
		])
		draw_polygon(points, PackedColorArray([Color.WHITE, Color.WHITE, Color.WHITE]))
