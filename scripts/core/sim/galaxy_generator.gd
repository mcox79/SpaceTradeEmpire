extends RefCounted
class_name GalaxyGenerator

var rng: RandomNumberGenerator

func _init(seed_val: int):
	rng = RandomNumberGenerator.new()
	rng.seed = seed_val

# Generates actual 3D coordinates for regions and stars.
func generate(region_count: int) -> Dictionary:
	var stars = []
	for r in range(region_count):
		# 1. Place the Region Center
		var region_center = Vector3(rng.randf_range(-100, 100), 0, rng.randf_range(-100, 100))
		
		# 2. Populate the Region with Stars
		var star_count = rng.randi_range(5, 10)
		for s in range(star_count):
			var pos = region_center + Vector3(rng.randf_range(-15, 15), rng.randf_range(-5, 5), rng.randf_range(-15, 15))
			stars.append({ "id": "star_" + str(stars.size()), "pos": pos, "region": r })
			
	return { "stars": stars }