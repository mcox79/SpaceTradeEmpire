extends Node3D

func _ready():
	# Wait one frame to ensure GameManager is fully booted
	await get_tree().process_frame
	
	print("[SPAWNER] Instantiating Galaxy View Model...")
	var sim = GameManager.sim
	
	# Spawn a 3D sphere for every star in the Sim's memory
	for star in sim.galaxy_map.stars:
		var mesh_inst = MeshInstance3D.new()
		var sphere = SphereMesh.new()
		sphere.radius = 1.0
		sphere.height = 2.0
		mesh_inst.mesh = sphere
		mesh_inst.position = star.pos
		add_child(mesh_inst)
		
	print("[SPAWNER] Galaxy rendering complete.")