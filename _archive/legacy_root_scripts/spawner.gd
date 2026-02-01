extends Node3D

@export var asteroid_scene: PackedScene
@export var enemy_scene: PackedScene
@export var field_radius: float = 100.0
@export var asteroid_count: int = 30
@export var threat_level: float = 0.1 # 10% chance

func _ready():
	# Load enemy scene dynamically if not assigned in Inspector
	if not enemy_scene:
		enemy_scene = load("res://scenes/enemy.tscn")
	
	if not asteroid_scene:
		print("ERROR: No Asteroid Scene assigned!")
		return
		
	print("[SYSTEM] GENERATING SECTOR: Threat Level %s" % threat_level)
	_generate_field()

func _generate_field():
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	
	for i in range(asteroid_count):
		var obj = null
		
		# Roll the dice: Enemy or Rock?
		if rng.randf() < threat_level:
			obj = enemy_scene.instantiate()
		else:
			obj = asteroid_scene.instantiate()
		
		# Position Logic
		var x = rng.randf_range(-field_radius, field_radius)
		var z = rng.randf_range(-field_radius, field_radius)
		if abs(x) < 20 and abs(z) < 20: x += 40 # Clear zone
		
		obj.position = Vector3(x, 0, z)
		obj.rotation.y = rng.randf_range(0, 6.28)
		
		add_child(obj)
		
	print("[SYSTEM] SECTOR POPULATED.")
