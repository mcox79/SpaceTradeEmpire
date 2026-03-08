# Edgedar: screen-edge direction indicators for off-screen POIs.
# Inspired by Starcom: Nexus. Shows arrows at viewport edges pointing toward
# lane gates, hostile fleets, quest targets, and stations.
extends CanvasLayer

const MAX_INDICATORS: int = 12
const EDGE_MARGIN: float = 40.0  # px inset from screen edge
const MIN_DISTANCE: float = 30.0  # hide indicator if POI closer than this (units)
const MAX_DISTANCE: float = 500.0  # fade indicator beyond this distance
const ARROW_SIZE: float = 12.0
const POI_REFRESH_INTERVAL: float = 0.25

enum PoiType { LANE_GATE, HOSTILE_FLEET, QUEST_TARGET, STATION }

var _poi_colors: Dictionary = {
	0: Color(0.4, 0.7, 1.0),   # LANE_GATE: blue
	1: Color(1.0, 0.15, 0.15), # HOSTILE: red
	2: Color(1.0, 0.85, 0.4),  # QUEST: gold
	3: Color(0.2, 1.0, 0.4),   # STATION: green
}

var _indicators: Array = []  # Array of {root: Control, arrow: ArrowDraw, label: Label}
var _game_manager = null
var _poi_cache: Array = []
var _poi_refresh_timer: float = 0.0


func _ready() -> void:
	layer = 10
	_game_manager = get_node_or_null("/root/GameManager")
	_build_indicator_pool()


func _physics_process(delta: float) -> void:
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
	var state = _game_manager.get("current_player_state")
	if state == null or int(state) != 0:  # Only IN_FLIGHT
		return false
	var overlay = _game_manager.get("galaxy_overlay_open")
	if overlay != null and bool(overlay):
		return false
	return true


func _gather_pois(camera: Camera3D) -> Array:
	var cam_pos: Vector3 = camera.global_position
	var result: Array = []

	# Lane gates
	for node in get_tree().get_nodes_in_group("LaneGate"):
		if node is Node3D:
			var dist: float = cam_pos.distance_to(node.global_position)
			if dist >= MIN_DISTANCE and dist <= MAX_DISTANCE:
				var lbl_text: String = ""
				if node.has_meta("neighbor_node_id"):
					lbl_text = str(node.get_meta("neighbor_node_id"))
					# Shorten long IDs to last segment
					if lbl_text.contains("_"):
						lbl_text = lbl_text.rsplit("_", true, 1)[-1]
				result.append({
					"pos": node.global_position,
					"type": PoiType.LANE_GATE,
					"dist": dist,
					"label": lbl_text
				})

	# Stations
	for node in get_tree().get_nodes_in_group("Station"):
		if node is Node3D:
			var dist: float = cam_pos.distance_to(node.global_position)
			if dist >= MIN_DISTANCE and dist <= MAX_DISTANCE:
				result.append({
					"pos": node.global_position,
					"type": PoiType.STATION,
					"dist": dist,
					"label": "Station"
				})

	# Fleet ships (hostile detection via meta)
	for node in get_tree().get_nodes_in_group("FleetShip"):
		if node is Node3D:
			var is_hostile: bool = false
			if node.has_meta("is_hostile"):
				is_hostile = bool(node.get_meta("is_hostile"))
			if not is_hostile:
				continue
			var dist: float = cam_pos.distance_to(node.global_position)
			if dist >= MIN_DISTANCE and dist <= MAX_DISTANCE:
				result.append({
					"pos": node.global_position,
					"type": PoiType.HOSTILE_FLEET,
					"dist": dist,
					"label": "%du" % int(dist)
				})

	# Sort by distance (closest first)
	result.sort_custom(func(a, b): return a["dist"] < b["dist"])
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

		# Distance-based alpha fade.
		var alpha: float = 1.0
		if dist > MAX_DISTANCE * 0.7:
			alpha = clampf(1.0 - (dist - MAX_DISTANCE * 0.7) / (MAX_DISTANCE * 0.3), 0.1, 1.0)

		if inner_rect.has_point(screen_pos) and not is_behind:
			# POI is on-screen — hide indicator (it's visible in 3D)
			indicator["root"].visible = false
		else:
			# Clamp to screen edge.
			var dir: Vector2 = (screen_pos - center).normalized()
			var clamped: Vector2 = _clamp_to_rect_edge(center, dir, inner_rect)

			indicator["root"].visible = true
			indicator["root"].position = clamped - Vector2(ARROW_SIZE, ARROW_SIZE)
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
	for i in range(MAX_INDICATORS):
		var root := Control.new()
		root.name = "Indicator%d" % i
		root.visible = false
		root.mouse_filter = Control.MOUSE_FILTER_IGNORE
		root.custom_minimum_size = Vector2(ARROW_SIZE * 2, ARROW_SIZE * 2)
		add_child(root)

		var arrow := ArrowDraw.new()
		arrow.name = "Arrow"
		arrow.arrow_size = ARROW_SIZE
		arrow.custom_minimum_size = Vector2(ARROW_SIZE * 2, ARROW_SIZE * 2)
		arrow.position = Vector2.ZERO
		arrow.mouse_filter = Control.MOUSE_FILTER_IGNORE
		arrow.pivot_offset = Vector2(ARROW_SIZE, ARROW_SIZE)
		root.add_child(arrow)

		var lbl := Label.new()
		lbl.name = "DistLabel"
		lbl.add_theme_font_size_override("font_size", 11)
		lbl.add_theme_color_override("font_color", Color.WHITE)
		lbl.position = Vector2(ARROW_SIZE * 2 + 2, 2)
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
