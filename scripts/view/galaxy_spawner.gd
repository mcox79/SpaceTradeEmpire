extends Node3D

var ship_meshes: Dictionary = {}
var ship_mat: StandardMaterial3D

# NEW: State storage for visual updates
var star_meshes: Dictionary = {} # Key: star_id, Value: MeshInstance3D
var _heat_materials: Dictionary = {} # Cache for distinct heat materials

func _ready():
	await get_tree().process_frame
	var sim = get_node("/root/GameManager").sim
	_draw_galaxy(sim)
	
	ship_mat = StandardMaterial3D.new()
	ship_mat.albedo_color = Color(1.0, 0.1, 0.1)
	ship_mat.emission_enabled = true
	ship_mat.emission = Color(1.0, 0.0, 0.0)
	ship_mat.emission_energy_multiplier = 3.0

func _draw_galaxy(sim):
	# 1. DRAW STARS
	for star in sim.galaxy_map.stars:
		var s = MeshInstance3D.new()
		s.mesh = SphereMesh.new();
		s.mesh.radius = 0.5; s.mesh.height = 1.0
		s.position = star.pos
		
		# Assign unique material for heat-mapping
		var mat = StandardMaterial3D.new()
		mat.albedo_color = Color(0, 0.5, 1.0) # Cold/Safe Blue
		mat.emission_enabled = true
		s.material_override = mat
		_heat_materials[star.id] = mat 
		star_meshes[star.id] = s
		add_child(s)
		
	# 2. DRAW LANES
	for lane in sim.galaxy_map.lanes:
		var mid = (lane.from + lane.to) / 2.0
		var l = MeshInstance3D.new()
		l.mesh = CylinderMesh.new();
		l.mesh.top_radius=0.05; l.mesh.bottom_radius=0.05; l.mesh.height = lane.from.distance_to(lane.to)
		l.position = mid
		l.look_at_from_position(mid, lane.to, Vector3.UP)
		l.rotate_object_local(Vector3.RIGHT, PI/2)
		add_child(l)

# PASSIVE RENDER LOOP: Strict View-Sim Data Contract
func _process(_delta):
	var game_manager = get_node_or_null("/root/GameManager")
	if not game_manager or not game_manager.sim: return
	
	var sim = game_manager.sim
	
	# UPDATE 1: Star Heat Visualization
	for star_id in star_meshes.keys():
		# Fetch current backend heat (Default to 0.0 if missing)
		var heat = sim.info.node_heat.get(star_id, 0.0) 
		
		# Normalize heat for color blending (Assuming 50.0 is "Maximum Danger")
		var heat_factor = clamp(heat / 50.0, 0.0, 1.0)
		
		# Blend from Blue (Safe) to Red (Hot)
		var target_color = Color(0, 0.5, 1.0).lerp(Color(1.0, 0.0, 0.0), heat_factor)
		
		# Update Material
		var mat = _heat_materials[star_id]
		mat.albedo_color = target_color
		mat.emission = target_color
		mat.emission_energy_multiplier = 1.0 + (heat_factor * 2.0) # Glows brighter when hot
	
	# UPDATE 2: Fleet Positions
	for fleet in sim.active_fleets:
		if not ship_meshes.has(fleet.id):
			var mesh_inst = MeshInstance3D.new()
			mesh_inst.mesh = BoxMesh.new()
			mesh_inst.mesh.size = Vector3(3.0, 3.0, 6.0) 
			mesh_inst.material_override = ship_mat
			add_child(mesh_inst)
			ship_meshes[fleet.id] = mesh_inst
		
		var visual_ship = ship_meshes[fleet.id]
		visual_ship.position = fleet.current_pos
		visual_ship.position.y += 2.0 
		
		var target_look = fleet.to + Vector3(0, 2.0, 0)
		if visual_ship.position.distance_to(target_look) > 0.1:
			visual_ship.look_at(target_look, Vector3.UP)
