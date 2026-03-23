extends Node3D

var ship_meshes: Dictionary = {}
var ship_mat: StandardMaterial3D
var star_meshes: Dictionary = {}
var _heat_materials: Dictionary = {}
var _station_identities: Dictionary = {}  # GATE.T46.STATION.NODE_MESH.001

# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: player-highlight ring per star node
var _highlight_rings: Dictionary = {}
var _player_star_id: String = ""
var _pulse_time: float = 0.0

# GATE.T47.AMBIENT.SHUTTLE_TRAFFIC.001: orbiting trade shuttles per station
var _shuttles: Dictionary = {}  # star_id -> Array[Dictionary] with {mesh, orbit_radius, speed, angle, y_offset}

# GATE.T47.AMBIENT.MINING_BEAMS.001: extraction beams from station to asteroids
var _mining_beams: Dictionary = {}  # star_id -> Array[Dictionary] with {mesh, mat, asteroid_pos}
var _asteroid_positions: Dictionary = {}  # star_id -> Array[Vector3]

# GATE.T47.AMBIENT.LANE_TRAFFIC.001: animated sprites along trade lanes
var _lane_traffic: Dictionary = {}  # lane_index -> Array[Dictionary] with {mesh, from, to, t}

# GATE.T47.AMBIENT.PROSPERITY_TIERS.001: prosperity light orbs for capitals
var _prosperity_lights: Dictionary = {}  # star_id -> {mesh, mat, orbit_angle}
var _star_market_breadth: Dictionary = {}  # star_id -> int (cached goods count)

# Shared time accumulator for ambient animations
var _ambient_time: float = 0.0

# GATE.T47.HAVEN.VISUAL_TIERS.001: Haven Precursor visual geometry
var _haven_visual_nodes: Array[Node3D] = []
var _haven_node_id: String = ""
var _haven_current_tier: int = -1
var _haven_refresh_counter: int = 0

# GATE.T47.MEGAPROJECT.MAP_MARKERS.001: megaproject markers on galaxy map
var _megaproject_markers: Dictionary = {}  # megaproject_id -> {root, mat, type_id}
# GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001: construction VFX for active megaprojects
var _megaproject_vfx: Dictionary = {}  # megaproject_id -> {frame, sparks, spark_timer}
var _megaproject_cache: Array = []
var _megaproject_refresh_counter: int = 0

# GATE.T48.DISCOVERY.MAP_MARKERS.001: discovery phase markers per system node
var _discovery_markers: Dictionary = {}  # star_id -> Array[Dictionary] with {mesh, mat, phase}
var _discovery_refresh_counter: int = 0

# GATE.T48.DISCOVERY.SCANNER_VIZ.001: scanner range ring on first visit
var _scanner_visited_nodes: Dictionary = {}  # star_id -> true (tracks first-visit)
var _scanner_ring_active: bool = false
var _scanner_ring_node: MeshInstance3D = null
var _scanner_ring_mat: StandardMaterial3D = null

# Region-based star colors: region 0 = starter hub (bright green), others cycle through blue/teal/gold
const REGION_COLORS := [
	Color(0.2, 1.0, 0.4),   # region 0: starter / trade hub — bright green
	Color(0.0, 0.7, 1.0),   # region 1: cyan-blue
	Color(0.8, 0.5, 0.1),   # region 2: amber / frontier
	Color(0.6, 0.2, 1.0),   # region 3: purple / deep rim
	Color(1.0, 0.3, 0.3),   # region 4+: red / danger
]

func _ready():
	await get_tree().process_frame
	var game_manager = get_node_or_null("/root/Main/GameManager")
	if game_manager and game_manager.sim:
		_draw_galaxy(game_manager.sim)

	ship_mat = StandardMaterial3D.new()
	ship_mat.albedo_color = Color(1.0, 0.1, 0.1)
	ship_mat.emission_enabled = true
	ship_mat.emission = Color(1.0, 0.0, 0.0)
	ship_mat.emission_energy_multiplier = 3.0

func _draw_galaxy(sim):
	# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: build id->region map for star coloring
	var region_by_id: Dictionary = {}
	for star in sim.galaxy_map.stars:
		region_by_id[star.id] = star.get("region", 0)

	for star in sim.galaxy_map.stars:
		var s = MeshInstance3D.new()
		s.mesh = SphereMesh.new()

		# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: size by region — region 0 (hub) is larger
		var region: int = star.get("region", 0)
		var is_hub_region := (region == 0)
		var radius: float = 1.8 if is_hub_region else 1.2
		s.mesh.radius = radius
		s.mesh.height = radius * 2.0
		s.position = star.pos

		var mat = StandardMaterial3D.new()
		# Color by region index
		var col: Color = REGION_COLORS[min(region, REGION_COLORS.size() - 1)]
		mat.albedo_color = col
		mat.emission_enabled = true
		mat.emission = col
		mat.emission_energy_multiplier = 2.0 if is_hub_region else 1.0
		s.material_override = mat
		_heat_materials[star.id] = mat
		star_meshes[star.id] = s
		add_child(s)

		# GATE.T46.STATION.NODE_MESH.001: apply station identity (faction tint + tier detail meshes)
		_apply_station_identity(s, star.id, radius)

		# GATE.S1.SPATIAL_AUDIO.AMBIENT.001: register station ambient audio
		var ambient_audio = get_tree().root.get_node_or_null("AmbientAudio")
		if ambient_audio and ambient_audio.has_method("register_station"):
			ambient_audio.call("register_station", s, star.id)

		# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: highlight ring node (hidden by default)
		var ring = _make_highlight_ring(star.pos, radius)
		ring.visible = false
		add_child(ring)
		_highlight_rings[star.id] = ring

		# GATE.S7.RUNTIME_STABILITY.ASTEROID_VARIETY.001: scatter asteroids near star
		_spawn_asteroids_v0(star.pos, star.id)

	var lane_index := 0
	for lane in sim.galaxy_map.lanes:
		# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: differentiate intra-region (bright) vs inter-region (dim)
		var u_region: int = region_by_id.get(lane.get("u", ""), -1)
		var v_region: int = region_by_id.get(lane.get("v", ""), -1)
		var is_inter_region := (u_region != v_region) or (u_region == -1)

		var mid = (lane.from + lane.to) / 2.0
		var l = MeshInstance3D.new()
		l.mesh = CylinderMesh.new()

		if is_inter_region:
			# Inter-region artery: dimmer gray, thinner (fracture-like)
			l.mesh.top_radius = 0.15
			l.mesh.bottom_radius = 0.15
			var mat_inter = StandardMaterial3D.new()
			mat_inter.albedo_color = Color(0.5, 0.5, 0.55)
			mat_inter.emission_enabled = true
			mat_inter.emission = Color(0.2, 0.2, 0.25)
			mat_inter.emission_energy_multiplier = 0.4
			l.material_override = mat_inter
		else:
			# Intra-region lane: bright cyan/white, wider
			l.mesh.top_radius = 0.3
			l.mesh.bottom_radius = 0.3
			var mat_lane = StandardMaterial3D.new()
			mat_lane.albedo_color = Color(0.6, 0.9, 1.0)
			mat_lane.emission_enabled = true
			mat_lane.emission = Color(0.4, 0.8, 1.0)
			mat_lane.emission_energy_multiplier = 1.2
			l.material_override = mat_lane

		l.mesh.height = lane.from.distance_to(lane.to)
		l.position = mid
		l.look_at_from_position(mid, lane.to, Vector3.UP)
		l.rotate_object_local(Vector3.RIGHT, PI/2)
		add_child(l)

		# GATE.T47.AMBIENT.LANE_TRAFFIC.001: spawn lane traffic on intra-region lanes only
		if not is_inter_region:
			_spawn_lane_traffic_v0(lane_index, lane.from, lane.to)
		lane_index += 1

	# GATE.T47.AMBIENT: spawn shuttles and mining beams after all stars are placed
	_spawn_all_shuttles_v0(sim)
	_spawn_all_mining_beams_v0()

	# GATE.T47.MEGAPROJECT.MAP_MARKERS.001: initial megaproject marker spawn
	_refresh_megaproject_markers_v0()

# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: torus-like ring mesh centered at pos
func _make_highlight_ring(pos: Vector3, star_radius: float) -> MeshInstance3D:
	var ring = MeshInstance3D.new()
	ring.mesh = TorusMesh.new()
	var ring_radius: float = star_radius * 2.2
	ring.mesh.inner_radius = ring_radius - 0.06
	ring.mesh.outer_radius = ring_radius + 0.06
	ring.position = pos
	# Rings lie in the XZ plane by default (in 3D, rotate to face camera-up)
	ring.rotate_x(PI / 2.0)
	var mat = StandardMaterial3D.new()
	mat.albedo_color = Color(0.3, 1.0, 0.5)
	mat.emission_enabled = true
	mat.emission = Color(0.3, 1.0, 0.5)
	mat.emission_energy_multiplier = 3.0
	ring.material_override = mat
	return ring

# GATE.S7.RUNTIME_STABILITY.ASTEROID_VARIETY.001: procedural asteroid field per star
func _spawn_asteroids_v0(star_pos: Vector3, star_id: String) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = hash(star_id + "_asteroids")

	# GATE.T47.AMBIENT.MINING_BEAMS.001: track asteroid positions for beam targeting
	var positions: Array[Vector3] = []

	var count: int = rng.randi_range(5, 12)
	for i in range(count):
		var asteroid := MeshInstance3D.new()
		var mesh := SphereMesh.new()
		mesh.radius = rng.randf_range(0.1, 0.4)
		mesh.height = mesh.radius * 2.0
		asteroid.mesh = mesh

		# Random position in a shell around the star
		var angle := rng.randf() * TAU
		var dist := rng.randf_range(5.0, 25.0)
		var y_offset := rng.randf_range(-3.0, 3.0)
		var ast_pos := star_pos + Vector3(cos(angle) * dist, y_offset, sin(angle) * dist)
		asteroid.position = ast_pos
		positions.append(ast_pos)

		# Non-uniform scale for irregular shape
		asteroid.scale = Vector3(
			rng.randf_range(0.6, 1.4),
			rng.randf_range(0.4, 1.2),
			rng.randf_range(0.6, 1.4)
		)

		# Rocky gray-brown material
		var mat := StandardMaterial3D.new()
		var gray := rng.randf_range(0.15, 0.35)
		mat.albedo_color = Color(gray + 0.05, gray, gray - 0.02)
		mat.roughness = 0.9
		asteroid.material_override = mat

		add_child(asteroid)

	_asteroid_positions[star_id] = positions

func _process(delta):
	var game_manager = get_node_or_null("/root/Main/GameManager")
	if not game_manager or not game_manager.sim: return

	var sim = game_manager.sim
	_ambient_time += delta

	# HEAT UPDATE + GATE.T47.AMBIENT.PROSPERITY_TIERS.001: prosperity emission modulation
	for star_id in star_meshes.keys():
		var heat = sim.info.get_node_heat(star_id)
		var heat_factor = clamp(heat / 50.0, 0.0, 1.0)
		var base_col: Color = _get_region_color(star_id, sim)
		var target_color = base_col.lerp(Color(1.0, 0.0, 0.0), heat_factor)
		var mat = _heat_materials[star_id]
		mat.albedo_color = target_color
		mat.emission = target_color

		# GATE.T47.AMBIENT.PROSPERITY_TIERS.001: modulate emission by market breadth
		var breadth: int = _star_market_breadth.get(star_id, 0)
		var prosperity_mult: float = 0.5  # dim default
		if breadth >= 6:
			prosperity_mult = 2.0
		elif breadth >= 3:
			prosperity_mult = 1.0
		mat.emission_energy_multiplier = (1.0 + (heat_factor * 2.0)) * prosperity_mult

	# GATE.T47.AMBIENT.SHUTTLE_TRAFFIC.001: animate shuttle orbits
	_process_shuttles_v0(delta)

	# GATE.T47.AMBIENT.MINING_BEAMS.001: pulse mining beam emission
	_process_mining_beams_v0(delta)

	# GATE.T47.AMBIENT.LANE_TRAFFIC.001: animate lane traffic sprites
	_process_lane_traffic_v0(delta)

	# GATE.T47.AMBIENT.PROSPERITY_TIERS.001: orbit prosperity lights for capitals
	_process_prosperity_lights_v0(delta)

	# GATE.T47.MEGAPROJECT.MAP_MARKERS.001 + CONSTRUCTION_VFX.001: update megaproject visuals
	_process_megaprojects_v0(delta)

	# GATE.T48.DISCOVERY.MAP_MARKERS.001: update discovery markers periodically
	_process_discovery_markers_v0(delta)

	# GATE.T48.DISCOVERY.SCANNER_VIZ.001: check for first-visit scanner ring trigger
	_process_scanner_ring_v0(delta)

	# GATE.T47.HAVEN.VISUAL_TIERS.001: update Haven Precursor visuals every ~60 frames
	_process_haven_visuals_v0(delta)

	# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: current system highlight with pulse
	_pulse_time += delta
	var new_player_star_id: String = ""
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge:
		var snap = bridge.call("GetGalaxySnapshotV0")
		if snap and snap.has("player_current_node_id"):
			new_player_star_id = snap["player_current_node_id"]

	# Fallback: use GDScript player state if SimBridge unavailable
	if new_player_star_id == "" and game_manager.player:
		new_player_star_id = game_manager.player.current_node_id

	# Update highlight rings: show only the player's current star
	if new_player_star_id != _player_star_id:
		# Hide old ring
		if _player_star_id != "" and _highlight_rings.has(_player_star_id):
			_highlight_rings[_player_star_id].visible = false
		# Show new ring
		if new_player_star_id != "" and _highlight_rings.has(new_player_star_id):
			_highlight_rings[new_player_star_id].visible = true
		_player_star_id = new_player_star_id

	# Pulse the active ring
	if _player_star_id != "" and _highlight_rings.has(_player_star_id):
		var ring_node: MeshInstance3D = _highlight_rings[_player_star_id]
		if ring_node.visible and ring_node.material_override is StandardMaterial3D:
			var pulse: float = 0.5 + 0.5 * sin(_pulse_time * 3.0)
			var mat: StandardMaterial3D = ring_node.material_override
			mat.emission_energy_multiplier = 2.0 + pulse * 2.5

	# FLEET UPDATE
	for fleet in sim.active_fleets:
		if not ship_meshes.has(fleet.id):
			var mesh_inst = MeshInstance3D.new()
			mesh_inst.mesh = BoxMesh.new()
			mesh_inst.mesh.size = Vector3(1.0, 0.5, 2.0)
			mesh_inst.material_override = ship_mat
			add_child(mesh_inst)
			ship_meshes[fleet.id] = mesh_inst

		var visual_ship = ship_meshes[fleet.id]
		visual_ship.position = fleet.current_pos
		visual_ship.position.y += 2.0

		# SAFE ORIENTATION
		var target_look = fleet.to + Vector3(0, 2.0, 0)
		if visual_ship.position.distance_to(target_look) > 0.1:
			visual_ship.look_at(target_look, Vector3.UP)

# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: region color lookup (uses stored heat material base)
func _get_region_color(star_id: String, sim) -> Color:
	for star in sim.galaxy_map.stars:
		if star.id == star_id:
			var region: int = star.get("region", 0)
			return REGION_COLORS[min(region, REGION_COLORS.size() - 1)]
	return Color(0.0, 0.5, 1.0)

# ─── GATE.T47.AMBIENT.SHUTTLE_TRAFFIC.001 ────────────────────────────────────

func _spawn_all_shuttles_v0(sim) -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	for star in sim.galaxy_map.stars:
		var star_id: String = star.id
		var star_pos: Vector3 = star.pos

		# Determine shuttle count from trade activity or market breadth fallback
		var shuttle_count := 1
		if bridge and bridge.has_method("GetNpcTradeActivityV0"):
			var activity: int = bridge.call("GetNpcTradeActivityV0", star_id)
			shuttle_count = clampi(activity, 1, 5)
		elif bridge and bridge.has_method("GetMarketGoodsSnapshotV1"):
			var goods: Array = bridge.call("GetMarketGoodsSnapshotV1", star_id)
			shuttle_count = clampi(ceili(goods.size() / 2.0), 1, 5)

		var rng := RandomNumberGenerator.new()
		rng.seed = hash(star_id + "_shuttles")
		var shuttles: Array = []
		for i in range(shuttle_count):
			var shuttle := MeshInstance3D.new()
			var box := BoxMesh.new()
			box.size = Vector3(0.3, 0.15, 0.6)
			shuttle.mesh = box

			var mat := StandardMaterial3D.new()
			mat.albedo_color = Color(0.8, 0.95, 1.0)
			mat.emission_enabled = true
			mat.emission = Color(0.6, 0.9, 1.0)
			mat.emission_energy_multiplier = 2.0
			shuttle.material_override = mat
			add_child(shuttle)

			var orbit_radius: float = rng.randf_range(3.0, 8.0)
			var speed: float = rng.randf_range(0.3, 0.8)
			var start_angle: float = rng.randf() * TAU
			var y_off: float = rng.randf_range(-1.0, 1.0)

			shuttles.append({
				"mesh": shuttle,
				"center": star_pos,
				"orbit_radius": orbit_radius,
				"speed": speed,
				"angle": start_angle,
				"y_offset": y_off,
			})
		_shuttles[star_id] = shuttles


func _process_shuttles_v0(delta: float) -> void:
	for star_id in _shuttles:
		var shuttles: Array = _shuttles[star_id]
		for s in shuttles:
			s["angle"] += s["speed"] * delta
			var a: float = s["angle"]
			var r: float = s["orbit_radius"]
			var center: Vector3 = s["center"]
			# Elliptical orbit: semi-minor = r * 0.6
			var x := cos(a) * r
			var z := sin(a) * r * 0.6
			var mesh: MeshInstance3D = s["mesh"]
			mesh.position = center + Vector3(x, s["y_offset"], z)
			# Face direction of travel
			var next_x := cos(a + 0.1) * r
			var next_z := sin(a + 0.1) * r * 0.6
			var target := center + Vector3(next_x, s["y_offset"], next_z)
			if mesh.position.distance_to(target) > 0.01:
				mesh.look_at(target, Vector3.UP)


# ─── GATE.T47.AMBIENT.MINING_BEAMS.001 ──────────────────────────────────────

func _spawn_all_mining_beams_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	for star_id in star_meshes:
		if not _asteroid_positions.has(star_id):
			continue
		var positions: Array = _asteroid_positions[star_id]
		if positions.is_empty():
			continue

		# Check if station has mining industry
		var has_mining := false
		if bridge and bridge.has_method("GetNodeIndustryV0"):
			var industries: Array = bridge.call("GetNodeIndustryV0", star_id)
			for ind in industries:
				var ind_str: String = str(ind).to_lower()
				if "mining" in ind_str or "extract" in ind_str:
					has_mining = true
					break
		else:
			# Fallback: use station identity industry count > 0 as proxy
			if _station_identities.has(star_id):
				has_mining = _station_identities[star_id]._industry_count > 0

		if not has_mining:
			continue

		var star_pos: Vector3 = star_meshes[star_id].position
		var beams: Array = []
		# Draw beams to up to 3 nearest asteroids
		var sorted_positions := positions.duplicate()
		sorted_positions.sort_custom(func(a_pos, b_pos): return star_pos.distance_to(a_pos) < star_pos.distance_to(b_pos))
		var beam_count := mini(3, sorted_positions.size())
		for i in range(beam_count):
			var ast_pos: Vector3 = sorted_positions[i]
			var beam := MeshInstance3D.new()
			var cyl := CylinderMesh.new()
			cyl.top_radius = 0.05
			cyl.bottom_radius = 0.05
			var dist := star_pos.distance_to(ast_pos)
			cyl.height = dist
			beam.mesh = cyl

			var beam_mat := StandardMaterial3D.new()
			beam_mat.albedo_color = Color(0.0, 0.8, 1.0, 0.6)
			beam_mat.emission_enabled = true
			beam_mat.emission = Color(0.0, 0.9, 1.0)
			beam_mat.emission_energy_multiplier = 2.0
			beam_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
			beam.material_override = beam_mat

			# Position at midpoint and orient along beam axis
			var mid := (star_pos + ast_pos) / 2.0
			beam.position = mid
			beam.look_at_from_position(mid, ast_pos, Vector3.UP)
			beam.rotate_object_local(Vector3.RIGHT, PI / 2.0)

			add_child(beam)
			beams.append({"mesh": beam, "mat": beam_mat, "asteroid_pos": ast_pos})
		_mining_beams[star_id] = beams


func _process_mining_beams_v0(_delta: float) -> void:
	for star_id in _mining_beams:
		var beams: Array = _mining_beams[star_id]
		for i in range(beams.size()):
			var b: Dictionary = beams[i]
			var mat: StandardMaterial3D = b["mat"]
			# Stagger pulse per beam index
			var pulse: float = 0.4 + 0.6 * abs(sin(_ambient_time * 2.0 + float(i) * 1.5))
			mat.emission_energy_multiplier = pulse * 3.0
			mat.albedo_color.a = pulse * 0.8


# ─── GATE.T47.AMBIENT.LANE_TRAFFIC.001 ──────────────────────────────────────

func _spawn_lane_traffic_v0(lane_idx: int, from_pos: Vector3, to_pos: Vector3) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = hash("lane_traffic_%d" % lane_idx)
	var sprite_count: int = rng.randi_range(1, 3)

	var sprites: Array = []
	for i in range(sprite_count):
		var sprite := MeshInstance3D.new()
		var prism := PrismMesh.new()
		prism.size = Vector3(0.2, 0.2, 0.4)
		sprite.mesh = prism
		sprite.scale = Vector3(0.2, 0.2, 0.2)

		var mat := StandardMaterial3D.new()
		mat.albedo_color = Color(0.7, 0.85, 1.0)
		mat.emission_enabled = true
		mat.emission = Color(0.5, 0.8, 1.0)
		mat.emission_energy_multiplier = 1.5
		sprite.material_override = mat
		add_child(sprite)

		# Stagger start positions along the lane
		var start_t: float = float(i) / float(sprite_count)
		var speed: float = rng.randf_range(0.03, 0.08)

		sprites.append({
			"mesh": sprite,
			"from": from_pos,
			"to": to_pos,
			"t": start_t,
			"speed": speed,
		})
	_lane_traffic[lane_idx] = sprites


func _process_lane_traffic_v0(delta: float) -> void:
	for lane_idx in _lane_traffic:
		var sprites: Array = _lane_traffic[lane_idx]
		for s in sprites:
			s["t"] += s["speed"] * delta
			if s["t"] >= 1.0:
				s["t"] -= 1.0
			var t: float = s["t"]
			var from_p: Vector3 = s["from"]
			var to_p: Vector3 = s["to"]
			var mesh: MeshInstance3D = s["mesh"]
			mesh.position = from_p.lerp(to_p, t) + Vector3(0.0, 0.5, 0.0)
			# Orient along lane direction
			if from_p.distance_to(to_p) > 0.1:
				mesh.look_at(to_p + Vector3(0.0, 0.5, 0.0), Vector3.UP)


# ─── GATE.T47.AMBIENT.PROSPERITY_TIERS.001 ──────────────────────────────────

func _spawn_prosperity_light_v0(star_id: String, star_pos: Vector3) -> void:
	var orb := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 0.3
	sphere.height = 0.6
	orb.mesh = sphere

	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(1.0, 0.9, 0.6)
	mat.emission_enabled = true
	mat.emission = Color(1.0, 0.85, 0.4)
	mat.emission_energy_multiplier = 4.0
	orb.material_override = mat
	add_child(orb)

	_prosperity_lights[star_id] = {
		"mesh": orb,
		"mat": mat,
		"center": star_pos,
		"orbit_angle": 0.0,
	}


func _process_prosperity_lights_v0(delta: float) -> void:
	for star_id in _prosperity_lights:
		var data: Dictionary = _prosperity_lights[star_id]
		data["orbit_angle"] += delta * 0.5
		var a: float = data["orbit_angle"]
		var center: Vector3 = data["center"]
		var mesh: MeshInstance3D = data["mesh"]
		mesh.position = center + Vector3(cos(a) * 4.0, 1.5, sin(a) * 4.0)
		# Gentle pulse
		var mat: StandardMaterial3D = data["mat"]
		mat.emission_energy_multiplier = 3.0 + sin(_ambient_time * 1.5) * 1.5


# GATE.T46.STATION.NODE_MESH.001: query bridge for faction/industry and add detail meshes
func _apply_station_identity(star_mesh: MeshInstance3D, star_id: String, base_radius: float) -> void:
	var bridge = get_tree().root.get_node_or_null("SimBridge")
	if bridge == null:
		return

	# Get faction from territory overlay
	var faction_id := ""
	if bridge.has_method("GetTerritoryAccessV0"):
		var territory: Dictionary = bridge.call("GetTerritoryAccessV0", star_id)
		faction_id = str(territory.get("faction_id", ""))

	# Get industry count for tier classification
	var industry_count := 0
	if bridge.has_method("GetNodeIndustryV0"):
		var industries: Array = bridge.call("GetNodeIndustryV0", star_id)
		industry_count = industries.size()

	# Get market breadth
	var market_breadth := 0
	if bridge.has_method("GetMarketGoodsSnapshotV1"):
		var goods: Array = bridge.call("GetMarketGoodsSnapshotV1", star_id)
		market_breadth = goods.size()

	# GATE.T47.AMBIENT.PROSPERITY_TIERS.001: cache breadth + spawn light for capitals
	_star_market_breadth[star_id] = market_breadth
	if market_breadth >= 6:
		_spawn_prosperity_light_v0(star_id, star_mesh.position)

	var identity := StationIdentity.new()
	identity.setup(faction_id, industry_count, market_breadth)
	identity.apply_to_mesh(star_mesh)
	identity.add_detail_meshes(star_mesh.get_parent(), base_radius * identity._tier_scale)
	_station_identities[star_id] = identity

# ── GATE.T47.MEGAPROJECT.MAP_MARKERS.001 + CONSTRUCTION_VFX.001 ──────────────

# Type-specific completed colors
const _MP_COLOR_FRACTURE_ANCHOR := Color(0.8, 0.4, 1.0)
const _MP_COLOR_TRADE_CORRIDOR := Color(0.2, 0.8, 0.4)
const _MP_COLOR_SENSOR_PYLON := Color(0.4, 0.7, 1.0)
const _MP_COLOR_GRAY := Color(0.4, 0.4, 0.4)
const _MP_COLOR_ACTIVE := Color(1.0, 0.8, 0.2)

func _get_mp_type_color(type_id: String) -> Color:
	match type_id:
		"fracture_anchor": return _MP_COLOR_FRACTURE_ANCHOR
		"trade_corridor": return _MP_COLOR_TRADE_CORRIDOR
		"sensor_pylon": return _MP_COLOR_SENSOR_PYLON
		_: return _MP_COLOR_FRACTURE_ANCHOR  # fallback

func _get_mp_state_color(mp: Dictionary) -> Color:
	if mp.get("completed", false):
		return _get_mp_type_color(str(mp.get("type_id", "")))
	var stage: int = mp.get("stage", 0)
	var ticks: int = mp.get("progress_ticks", 0)
	if stage > 0 or ticks > 0:
		return _MP_COLOR_ACTIVE
	return _MP_COLOR_GRAY

func _get_mp_progress_frac(mp: Dictionary) -> float:
	if mp.get("completed", false):
		return 1.0
	var max_stages: int = mp.get("max_stages", 1)
	if max_stages <= 0:
		return 0.0
	var stage: int = mp.get("stage", 0)
	var ticks: int = mp.get("progress_ticks", 0)
	var tps: int = mp.get("ticks_per_stage", 1)
	if tps <= 0:
		tps = 1
	var stage_frac: float = float(ticks) / float(tps)
	return clamp((float(stage) + stage_frac) / float(max_stages), 0.0, 1.0)

func _refresh_megaproject_markers_v0() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if not bridge or not bridge.has_method("GetMegaprojectsV0"):
		return

	_megaproject_cache = bridge.call("GetMegaprojectsV0")

	# Track which IDs are still present
	var active_ids: Dictionary = {}
	for mp in _megaproject_cache:
		var mp_id: String = str(mp.get("id", ""))
		if mp_id == "":
			continue
		active_ids[mp_id] = true

		var node_id: String = str(mp.get("node_id", ""))
		var type_id: String = str(mp.get("type_id", ""))
		var col: Color = _get_mp_state_color(mp)
		var is_active: bool = not mp.get("completed", false) and (mp.get("stage", 0) > 0 or mp.get("progress_ticks", 0) > 0)
		var progress: float = _get_mp_progress_frac(mp)

		if _megaproject_markers.has(mp_id):
			# Update existing marker color
			var entry: Dictionary = _megaproject_markers[mp_id]
			var mat: StandardMaterial3D = entry["mat"]
			mat.albedo_color = col
			mat.emission = col

			# GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001: add/remove VFX based on state
			if is_active and not _megaproject_vfx.has(mp_id):
				_spawn_construction_vfx_v0(mp_id, entry["root"].position, progress)
			elif not is_active and _megaproject_vfx.has(mp_id):
				_remove_construction_vfx_v0(mp_id)
			elif is_active and _megaproject_vfx.has(mp_id):
				# Update rotation speed based on progress
				_megaproject_vfx[mp_id]["progress"] = progress
		else:
			# Create new marker
			if not star_meshes.has(node_id):
				continue
			var star_pos: Vector3 = star_meshes[node_id].position
			var marker_pos := star_pos + Vector3(0.0, 4.0, 0.0)  # offset above star

			var root := Node3D.new()
			root.position = marker_pos
			add_child(root)

			var mat := StandardMaterial3D.new()
			mat.albedo_color = col
			mat.emission_enabled = true
			mat.emission = col
			mat.emission_energy_multiplier = 2.5

			_build_marker_mesh_v0(root, type_id, mat)
			_megaproject_markers[mp_id] = {"root": root, "mat": mat, "type_id": type_id}

			# GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001: spawn VFX if active
			if is_active:
				_spawn_construction_vfx_v0(mp_id, marker_pos, progress)

	# Remove markers for megaprojects that no longer exist
	for old_id in _megaproject_markers.keys():
		if not active_ids.has(old_id):
			var entry: Dictionary = _megaproject_markers[old_id]
			entry["root"].queue_free()
			_megaproject_markers.erase(old_id)
			_remove_construction_vfx_v0(old_id)

func _build_marker_mesh_v0(parent: Node3D, type_id: String, mat: StandardMaterial3D) -> void:
	match type_id:
		"fracture_anchor":
			# Hexagonal frame: 6 short cylinders arranged in a hex pattern
			for i in range(6):
				var seg := MeshInstance3D.new()
				var cyl := CylinderMesh.new()
				cyl.top_radius = 0.06
				cyl.bottom_radius = 0.06
				cyl.height = 0.7
				seg.mesh = cyl
				var angle: float = float(i) * TAU / 6.0
				seg.position = Vector3(cos(angle) * 0.5, 0.0, sin(angle) * 0.5)
				# Point each cylinder toward the next hex vertex
				var next_angle: float = float(i + 1) * TAU / 6.0
				var next_pos := Vector3(cos(next_angle) * 0.5, 0.0, sin(next_angle) * 0.5)
				var mid := (seg.position + next_pos) / 2.0
				seg.position = mid
				var dir := next_pos - seg.position * 2.0 + seg.position  # direction between vertices
				seg.look_at_from_position(mid, next_pos, Vector3.UP)
				seg.rotate_object_local(Vector3.RIGHT, PI / 2.0)
				seg.material_override = mat
				parent.add_child(seg)
		"trade_corridor":
			# Diamond/rhombus: rotated box
			var diamond := MeshInstance3D.new()
			var box := BoxMesh.new()
			box.size = Vector3(0.5, 0.5, 0.5)
			diamond.mesh = box
			diamond.rotation_degrees = Vector3(0.0, 45.0, 45.0)
			diamond.material_override = mat
			parent.add_child(diamond)
		"sensor_pylon":
			# Upward-pointing cone
			var cone := MeshInstance3D.new()
			var cone_mesh := CylinderMesh.new()
			cone_mesh.top_radius = 0.0
			cone_mesh.bottom_radius = 0.4
			cone_mesh.height = 0.8
			cone.mesh = cone_mesh
			cone.material_override = mat
			parent.add_child(cone)
		_:
			# Generic fallback: small sphere
			var sphere := MeshInstance3D.new()
			var sph := SphereMesh.new()
			sph.radius = 0.3
			sph.height = 0.6
			sphere.mesh = sph
			sphere.material_override = mat
			parent.add_child(sphere)

# GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001: rotating spars + spark particles
func _spawn_construction_vfx_v0(mp_id: String, pos: Vector3, progress: float) -> void:
	var frame := Node3D.new()
	frame.position = pos
	add_child(frame)

	# 3 thin rotating spars
	for i in range(3):
		var spar := MeshInstance3D.new()
		var cyl := CylinderMesh.new()
		cyl.top_radius = 0.03
		cyl.bottom_radius = 0.03
		cyl.height = 1.6
		spar.mesh = cyl
		var spar_mat := StandardMaterial3D.new()
		spar_mat.albedo_color = Color(0.9, 0.7, 0.2)
		spar_mat.emission_enabled = true
		spar_mat.emission = Color(0.9, 0.7, 0.2)
		spar_mat.emission_energy_multiplier = 1.5
		spar.material_override = spar_mat
		# Offset each spar by 120 degrees
		var angle: float = float(i) * TAU / 3.0
		spar.position = Vector3(cos(angle) * 0.8, 0.0, sin(angle) * 0.8)
		spar.rotation_degrees.x = 90.0
		frame.add_child(spar)

	# Spark particles: 2-4 small spheres that blink
	var spark_count: int = 2 + int(progress * 2.0)  # 2 at start, up to 4 near completion
	var sparks: Array = []
	for i in range(spark_count):
		var spark := MeshInstance3D.new()
		var sph := SphereMesh.new()
		sph.radius = 0.08
		sph.height = 0.16
		spark.mesh = sph
		var spark_mat := StandardMaterial3D.new()
		spark_mat.albedo_color = Color(1.0, 0.9, 0.3)
		spark_mat.emission_enabled = true
		spark_mat.emission = Color(1.0, 0.9, 0.3)
		spark_mat.emission_energy_multiplier = 4.0
		spark.material_override = spark_mat
		# Distribute sparks around the construction frame
		var s_angle: float = float(i) * TAU / float(spark_count)
		spark.position = Vector3(cos(s_angle) * 1.0, 0.3, sin(s_angle) * 1.0)
		frame.add_child(spark)
		sparks.append(spark)

	_megaproject_vfx[mp_id] = {
		"frame": frame,
		"sparks": sparks,
		"spark_timer": 0.0,
		"progress": progress,
	}

func _remove_construction_vfx_v0(mp_id: String) -> void:
	if not _megaproject_vfx.has(mp_id):
		return
	var entry: Dictionary = _megaproject_vfx[mp_id]
	if entry.has("frame") and is_instance_valid(entry["frame"]):
		entry["frame"].queue_free()
	_megaproject_vfx.erase(mp_id)

func _process_megaprojects_v0(delta: float) -> void:
	# Refresh megaproject data from bridge every ~120 frames
	_megaproject_refresh_counter += 1
	if _megaproject_refresh_counter >= 120:
		_megaproject_refresh_counter = 0
		_refresh_megaproject_markers_v0()

	# GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001: animate construction VFX
	for mp_id in _megaproject_vfx.keys():
		var entry: Dictionary = _megaproject_vfx[mp_id]
		var frame: Node3D = entry["frame"]
		if not is_instance_valid(frame):
			continue

		var progress: float = entry.get("progress", 0.0)
		# Rotation speed scales with progress: 0.3 rad/s at 0%, 1.0 rad/s near completion
		var rot_speed: float = 0.3 + progress * 0.7
		frame.rotate_y(delta * rot_speed)

		# Spark blinking: toggle visibility every 0.3s (faster with more progress)
		var blink_interval: float = 0.3 - progress * 0.15  # 0.3s → 0.15s
		entry["spark_timer"] += delta
		if entry["spark_timer"] >= blink_interval:
			entry["spark_timer"] = 0.0
			var sparks: Array = entry.get("sparks", [])
			for spark in sparks:
				if is_instance_valid(spark):
					spark.visible = not spark.visible


# ─── GATE.T48.DISCOVERY.MAP_MARKERS.001 ────────────────────────────────────────

const _DISC_COLOR_SEEN := Color(0.5, 0.5, 0.5)     # gray — unseen/undiscovered
const _DISC_COLOR_SCANNED := Color(1.0, 0.75, 0.1)  # amber — scanned but not analyzed
const _DISC_COLOR_ANALYZED := Color(0.2, 0.9, 0.3)  # green — analyzed

func _process_discovery_markers_v0(_delta: float) -> void:
	_discovery_refresh_counter += 1

	# Pulse animation on unscanned (SEEN) markers
	for star_id in _discovery_markers:
		var markers: Array = _discovery_markers[star_id]
		for m in markers:
			if not is_instance_valid(m["mesh"]):
				continue
			var phase: String = m["phase"]
			if phase == "SEEN":
				# Pulse scale oscillation for undiscovered sites
				var pulse: float = 0.8 + 0.4 * sin(_ambient_time * 3.0 + hash(star_id) * 0.01)
				m["mesh"].scale = Vector3(pulse, pulse, pulse)
			elif phase == "SCANNED":
				# Amber pulsing emission for scanned-but-not-analyzed
				var mat: StandardMaterial3D = m["mat"]
				var pulse: float = 2.0 + 1.5 * sin(_ambient_time * 2.5)
				mat.emission_energy_multiplier = pulse

	# Full refresh every ~90 frames
	if _discovery_refresh_counter < 90:
		return
	_discovery_refresh_counter = 0

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetDiscoverySnapshotV0"):
		return

	# Refresh markers for all visited star nodes
	for star_id in star_meshes:
		var sites: Array = bridge.call("GetDiscoverySnapshotV0", star_id)
		if sites.size() == 0:
			# No sites — clean up old markers if present
			if _discovery_markers.has(star_id):
				_clear_discovery_markers_for_node(star_id)
			continue

		# Cap at 3 markers per system to avoid clutter
		var capped_sites: Array = sites.slice(0, mini(3, sites.size()))
		var star_pos: Vector3 = star_meshes[star_id].position

		# Check if markers already exist and phases match
		if _discovery_markers.has(star_id):
			var existing: Array = _discovery_markers[star_id]
			var needs_rebuild := existing.size() != capped_sites.size()
			if not needs_rebuild:
				for i in range(capped_sites.size()):
					if existing[i]["phase"] != str(capped_sites[i].get("phase", "SEEN")):
						needs_rebuild = true
						break
			if not needs_rebuild:
				continue
			_clear_discovery_markers_for_node(star_id)

		# Build new markers
		var markers: Array = []
		for i in range(capped_sites.size()):
			var site: Dictionary = capped_sites[i]
			var phase: String = str(site.get("phase", "SEEN"))
			var marker_data := _create_discovery_marker_v0(star_pos, i, capped_sites.size(), phase)
			markers.append(marker_data)
		_discovery_markers[star_id] = markers


func _create_discovery_marker_v0(star_pos: Vector3, index: int, total: int, phase: String) -> Dictionary:
	var mesh_inst := MeshInstance3D.new()
	var mat := StandardMaterial3D.new()
	mat.emission_enabled = true

	match phase:
		"SEEN":
			# Small diamond mesh — gray
			var box := BoxMesh.new()
			box.size = Vector3(0.3, 0.3, 0.3)
			mesh_inst.mesh = box
			mesh_inst.rotation_degrees = Vector3(45.0, 0.0, 45.0)
			mat.albedo_color = _DISC_COLOR_SEEN
			mat.emission = _DISC_COLOR_SEEN
			mat.emission_energy_multiplier = 1.0
		"SCANNED":
			# Pulsing amber sphere
			var sph := SphereMesh.new()
			sph.radius = 0.25
			sph.height = 0.5
			mesh_inst.mesh = sph
			mat.albedo_color = _DISC_COLOR_SCANNED
			mat.emission = _DISC_COLOR_SCANNED
			mat.emission_energy_multiplier = 2.5
		"ANALYZED":
			# Steady green checkmark approximation (small box + slight offset to suggest a mark)
			var box := BoxMesh.new()
			box.size = Vector3(0.35, 0.12, 0.12)
			mesh_inst.mesh = box
			mat.albedo_color = _DISC_COLOR_ANALYZED
			mat.emission = _DISC_COLOR_ANALYZED
			mat.emission_energy_multiplier = 2.0
		_:
			var sph := SphereMesh.new()
			sph.radius = 0.2
			sph.height = 0.4
			mesh_inst.mesh = sph
			mat.albedo_color = _DISC_COLOR_SEEN
			mat.emission = _DISC_COLOR_SEEN
			mat.emission_energy_multiplier = 1.0

	mesh_inst.material_override = mat

	# Position markers in a small arc offset from the star node (above and spaced)
	var angle_offset: float = (float(index) - float(total - 1) / 2.0) * 0.8
	var marker_pos := star_pos + Vector3(sin(angle_offset) * 3.5, 3.0, cos(angle_offset) * 3.5)
	mesh_inst.position = marker_pos

	add_child(mesh_inst)
	return {"mesh": mesh_inst, "mat": mat, "phase": phase}


func _clear_discovery_markers_for_node(star_id: String) -> void:
	if not _discovery_markers.has(star_id):
		return
	var markers: Array = _discovery_markers[star_id]
	for m in markers:
		if is_instance_valid(m["mesh"]):
			m["mesh"].queue_free()
	_discovery_markers.erase(star_id)


# ─── GATE.T48.DISCOVERY.SCANNER_VIZ.001 ────────────────────────────────────────

func _process_scanner_ring_v0(_delta: float) -> void:
	# Detect player node change for first-visit scanner ring
	if _player_star_id.is_empty():
		return

	if _scanner_visited_nodes.has(_player_star_id):
		return  # Already visited — no ring

	# First visit to this node — mark visited and spawn scanner ring
	_scanner_visited_nodes[_player_star_id] = true

	if not star_meshes.has(_player_star_id):
		return

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetScannerRangeV0"):
		return

	var scan_range: int = bridge.call("GetScannerRangeV0")
	if scan_range <= 0:
		return  # No scanner equipped — no ring

	var star_pos: Vector3 = star_meshes[_player_star_id].position
	# Convert scan range (in hops) to a visual radius (units)
	# Each hop ~ 15 units of visual distance on the map
	var max_radius: float = float(scan_range) * 15.0

	_spawn_scanner_ring_v0(star_pos, max_radius)


func _spawn_scanner_ring_v0(center: Vector3, max_radius: float) -> void:
	# Clean up previous ring if still active
	if _scanner_ring_node != null and is_instance_valid(_scanner_ring_node):
		_scanner_ring_node.queue_free()

	_scanner_ring_node = MeshInstance3D.new()
	_scanner_ring_node.mesh = TorusMesh.new()
	# Start small, will expand via tween
	_scanner_ring_node.mesh.inner_radius = 0.1
	_scanner_ring_node.mesh.outer_radius = 0.2
	_scanner_ring_node.position = center
	_scanner_ring_node.rotate_x(PI / 2.0)

	_scanner_ring_mat = StandardMaterial3D.new()
	_scanner_ring_mat.albedo_color = Color(0.0, 0.9, 1.0, 0.8)
	_scanner_ring_mat.emission_enabled = true
	_scanner_ring_mat.emission = Color(0.0, 0.85, 1.0)
	_scanner_ring_mat.emission_energy_multiplier = 3.0
	_scanner_ring_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_scanner_ring_node.material_override = _scanner_ring_mat

	add_child(_scanner_ring_node)
	_scanner_ring_active = true

	# Animate expansion: inner/outer radius from 0 to max_radius over 1.5s
	var tween := create_tween()
	tween.set_parallel(true)

	# Expand ring radius
	var target_inner: float = max_radius - 0.15
	var target_outer: float = max_radius + 0.15
	tween.tween_method(func(v: float):
		if is_instance_valid(_scanner_ring_node) and _scanner_ring_node.mesh is TorusMesh:
			var torus: TorusMesh = _scanner_ring_node.mesh
			torus.inner_radius = v - 0.15
			torus.outer_radius = v + 0.15
	, 0.3, max_radius, 1.5).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

	# Fade out alpha at the end (start fading at 1.0s, fully gone at 2.0s)
	tween.tween_method(func(a: float):
		if _scanner_ring_mat != null:
			_scanner_ring_mat.albedo_color.a = a
	, 0.8, 0.0, 0.8).set_delay(1.2)

	# Pulse discovery markers briefly as ring passes them
	var current_star_id := _player_star_id
	tween.tween_callback(func():
		_pulse_discovered_sites_v0(current_star_id)
	).set_delay(0.6)

	# Cleanup after animation completes
	tween.set_parallel(false)
	tween.tween_callback(func():
		if is_instance_valid(_scanner_ring_node):
			_scanner_ring_node.queue_free()
			_scanner_ring_node = null
		_scanner_ring_active = false
	).set_delay(2.2)


func _pulse_discovered_sites_v0(star_id: String) -> void:
	# Briefly brighten discovery markers at this node
	if not _discovery_markers.has(star_id):
		return
	var markers: Array = _discovery_markers[star_id]
	for m in markers:
		if not is_instance_valid(m["mesh"]):
			continue
		var mat: StandardMaterial3D = m["mat"]
		var original_energy: float = mat.emission_energy_multiplier
		# Flash bright
		var pulse_tween := create_tween()
		pulse_tween.tween_property(mat, "emission_energy_multiplier", original_energy + 4.0, 0.2)
		pulse_tween.tween_property(mat, "emission_energy_multiplier", original_energy, 0.4)


# ─── GATE.T47.HAVEN.VISUAL_TIERS.001 ──────────────────────────────────────────

func _process_haven_visuals_v0(_delta: float) -> void:
	_haven_refresh_counter += 1
	if _haven_refresh_counter < 60:
		# Animate pulsing beacon if present
		_animate_haven_beacon_v0(_delta)
		return
	_haven_refresh_counter = 0

	var bridge = get_node_or_null("/root/SimBridge")
	if bridge == null or not bridge.has_method("GetHavenStatusV0"):
		return
	var status: Dictionary = bridge.call("GetHavenStatusV0")
	if not status.get("discovered", false):
		return

	var node_id: String = str(status.get("node_id", ""))
	var tier: int = int(status.get("tier", 0))
	if node_id.is_empty() or not star_meshes.has(node_id):
		return

	# Only rebuild if tier changed or first initialization
	if node_id == _haven_node_id and tier == _haven_current_tier:
		_animate_haven_beacon_v0(_delta)
		return

	_haven_node_id = node_id
	_haven_current_tier = tier

	# Cleanup previous visuals
	_clear_haven_visuals_v0()

	# Build tier-specific Precursor geometry at the Haven star position
	var star_mesh: MeshInstance3D = star_meshes[node_id]
	var star_pos: Vector3 = star_mesh.position
	_build_haven_visuals_v0(star_pos, tier)

	_animate_haven_beacon_v0(_delta)


func _clear_haven_visuals_v0() -> void:
	for node in _haven_visual_nodes:
		if is_instance_valid(node):
			node.queue_free()
	_haven_visual_nodes.clear()


func _make_haven_material_v0(col: Color, energy: float) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = col
	mat.emission_enabled = true
	mat.emission = col
	mat.emission_energy_multiplier = energy
	return mat


func _build_haven_visuals_v0(star_pos: Vector3, tier: int) -> void:
	if tier < 1:
		return

	# T1 Powered: Faint purple glow ring
	if tier >= 1:
		var ring := MeshInstance3D.new()
		ring.mesh = TorusMesh.new()
		var ring_r: float = 3.5
		ring.mesh.inner_radius = ring_r - 0.08
		ring.mesh.outer_radius = ring_r + 0.08
		ring.position = star_pos
		ring.rotate_x(PI / 2.0)
		var energy: float = 1.5 if tier == 1 else (2.5 if tier <= 3 else 4.0)
		ring.material_override = _make_haven_material_v0(Color(0.5, 0.2, 0.8), energy)
		add_child(ring)
		_haven_visual_nodes.append(ring)

	# T2 Inhabited: 2 small orbiting satellite spheres
	if tier >= 2:
		for i in range(2):
			var sat := MeshInstance3D.new()
			var sph := SphereMesh.new()
			sph.radius = 0.25
			sph.height = 0.5
			sat.mesh = sph
			var angle: float = float(i) * PI
			sat.position = star_pos + Vector3(cos(angle) * 4.5, 0.5, sin(angle) * 4.5)
			sat.material_override = _make_haven_material_v0(Color(0.6, 0.3, 0.9), 2.0)
			add_child(sat)
			_haven_visual_nodes.append(sat)

	# T3 Operational: Larger outer orbital ring
	if tier >= 3:
		var outer_ring := MeshInstance3D.new()
		outer_ring.mesh = TorusMesh.new()
		var outer_r: float = 5.5
		outer_ring.mesh.inner_radius = outer_r - 0.06
		outer_ring.mesh.outer_radius = outer_r + 0.06
		outer_ring.position = star_pos
		outer_ring.rotate_x(PI / 2.0)
		outer_ring.material_override = _make_haven_material_v0(Color(0.6, 0.3, 0.9), 2.5)
		add_child(outer_ring)
		_haven_visual_nodes.append(outer_ring)

	# T4 Expanded: Hexagonal frame (6 cylinder spars) + pulsing beacon sphere
	if tier >= 4:
		var hex_frame := Node3D.new()
		hex_frame.position = star_pos
		add_child(hex_frame)
		_haven_visual_nodes.append(hex_frame)
		var hex_r: float = 4.0
		for i in range(6):
			var spar := MeshInstance3D.new()
			var cyl := CylinderMesh.new()
			cyl.top_radius = 0.05
			cyl.bottom_radius = 0.05
			var a1: float = float(i) * TAU / 6.0
			var a2: float = float(i + 1) * TAU / 6.0
			var p1 := Vector3(cos(a1) * hex_r, 0.0, sin(a1) * hex_r)
			var p2 := Vector3(cos(a2) * hex_r, 0.0, sin(a2) * hex_r)
			cyl.height = p1.distance_to(p2)
			spar.mesh = cyl
			var mid := (p1 + p2) / 2.0
			spar.position = mid
			spar.look_at_from_position(mid, p2, Vector3.UP)
			spar.rotate_object_local(Vector3.RIGHT, PI / 2.0)
			spar.material_override = _make_haven_material_v0(Color(0.7, 0.4, 1.0), 2.0)
			hex_frame.add_child(spar)

		# Pulsing beacon sphere above Haven
		var beacon := MeshInstance3D.new()
		var bsph := SphereMesh.new()
		bsph.radius = 0.4
		bsph.height = 0.8
		beacon.mesh = bsph
		beacon.position = star_pos + Vector3(0.0, 3.0, 0.0)
		beacon.name = "HavenBeacon"
		beacon.material_override = _make_haven_material_v0(Color(0.7, 0.4, 1.0), 3.0)
		add_child(beacon)
		_haven_visual_nodes.append(beacon)

	# T5 Awakened: Full Precursor aesthetic — golden emission, multiple angled rings, bright beacon
	if tier >= 5:
		# Override beacon to golden
		for node in _haven_visual_nodes:
			if is_instance_valid(node) and node.name == "HavenBeacon":
				node.material_override = _make_haven_material_v0(Color(1.0, 0.85, 0.3), 5.0)
				var bsph5 := SphereMesh.new()
				bsph5.radius = 0.6
				bsph5.height = 1.2
				node.mesh = bsph5

		# Tilted ring 1 (30 degrees)
		var ring_t1 := MeshInstance3D.new()
		ring_t1.mesh = TorusMesh.new()
		var rt1_r: float = 4.8
		ring_t1.mesh.inner_radius = rt1_r - 0.05
		ring_t1.mesh.outer_radius = rt1_r + 0.05
		ring_t1.position = star_pos
		ring_t1.rotate_x(PI / 2.0 + PI / 6.0)
		ring_t1.material_override = _make_haven_material_v0(Color(1.0, 0.85, 0.3), 3.5)
		add_child(ring_t1)
		_haven_visual_nodes.append(ring_t1)

		# Tilted ring 2 (-30 degrees)
		var ring_t2 := MeshInstance3D.new()
		ring_t2.mesh = TorusMesh.new()
		ring_t2.mesh.inner_radius = rt1_r - 0.05
		ring_t2.mesh.outer_radius = rt1_r + 0.05
		ring_t2.position = star_pos
		ring_t2.rotate_x(PI / 2.0 - PI / 6.0)
		ring_t2.material_override = _make_haven_material_v0(Color(1.0, 0.85, 0.3), 3.5)
		add_child(ring_t2)
		_haven_visual_nodes.append(ring_t2)

		# Additional orbiting satellites (3 total for T5)
		for i in range(3):
			var sat5 := MeshInstance3D.new()
			var sph5 := SphereMesh.new()
			sph5.radius = 0.2
			sph5.height = 0.4
			sat5.mesh = sph5
			var angle5: float = float(i) * TAU / 3.0
			sat5.position = star_pos + Vector3(cos(angle5) * 6.0, 1.0, sin(angle5) * 6.0)
			sat5.material_override = _make_haven_material_v0(Color(1.0, 0.9, 0.5), 3.0)
			add_child(sat5)
			_haven_visual_nodes.append(sat5)


func _animate_haven_beacon_v0(_delta: float) -> void:
	# Pulse the beacon sphere (if present)
	for node in _haven_visual_nodes:
		if is_instance_valid(node) and node.name == "HavenBeacon":
			if node.material_override is StandardMaterial3D:
				var mat: StandardMaterial3D = node.material_override
				var pulse: float = 2.5 + 2.0 * sin(_ambient_time * 2.0)
				mat.emission_energy_multiplier = pulse
