extends Node3D

var ship_meshes: Dictionary = {}
var ship_mat: StandardMaterial3D
var star_meshes: Dictionary = {}
var _heat_materials: Dictionary = {}

# GATE.S1.VISUAL_POLISH.GALAXY_MAP.001: player-highlight ring per star node
var _highlight_rings: Dictionary = {}
var _player_star_id: String = ""
var _pulse_time: float = 0.0

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
		asteroid.position = star_pos + Vector3(cos(angle) * dist, y_offset, sin(angle) * dist)

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

func _process(delta):
	var game_manager = get_node_or_null("/root/Main/GameManager")
	if not game_manager or not game_manager.sim: return

	var sim = game_manager.sim

	# HEAT UPDATE
	for star_id in star_meshes.keys():
		var heat = sim.info.get_node_heat(star_id)
		var heat_factor = clamp(heat / 50.0, 0.0, 1.0)
		var base_col: Color = _get_region_color(star_id, sim)
		var target_color = base_col.lerp(Color(1.0, 0.0, 0.0), heat_factor)
		var mat = _heat_materials[star_id]
		mat.albedo_color = target_color
		mat.emission = target_color
		mat.emission_energy_multiplier = 1.0 + (heat_factor * 2.0)

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
