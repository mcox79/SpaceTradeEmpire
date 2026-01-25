extends RefCounted
class_name GalaxyGenerator

var rng: RandomNumberGenerator

func _init(seed_val: int):
	rng = RandomNumberGenerator.new()
	rng.seed = seed_val

func generate(region_count: int) -> Dictionary:
	var stars = []
	var lanes = []
	
	# 1. Generate Stars (Same as before)
	var region_centers = []
	for r in range(region_count):
		var center = Vector3(rng.randf_range(-100, 100), 0, rng.randf_range(-100, 100))
		region_centers.append(center)
		var star_count = rng.randi_range(5, 8)
		for s in range(star_count):
			var pos = center + Vector3(rng.randf_range(-15, 15), rng.randf_range(-5, 5), rng.randf_range(-15, 15))
			stars.append({ "id": "star_" + str(stars.size()), "pos": pos, "region": r })

	# 2. Generate Intra-Region Lanes (Low Risk, Low Margin)
	# Connect each star to its nearest neighbor in the same region
	for star in stars:
		var best_target = null
		var min_dist = 9999.0
		for other in stars:
			if star.id != other.id and star.region == other.region:
				var d = star.pos.distance_to(other.pos)
				if d < min_dist:
					min_dist = d
					best_target = other
		if best_target != null:
			lanes.append({ "from": star.pos, "to": best_target.pos })

	# 3. Generate Inter-Region Chokepoints (High Risk, High Margin)
	for r in range(region_count - 1):
		var r1_stars = stars.filter(func(s): return s.region == r)
		var r2_stars = stars.filter(func(s): return s.region == r + 1)
		if r1_stars.size() > 0 and r2_stars.size() > 0:
			lanes.append({ "from": r1_stars[0].pos, "to": r2_stars[0].pos })

	return { "stars": stars, "lanes": lanes }