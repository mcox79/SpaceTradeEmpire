extends RefCounted

var rng: RandomNumberGenerator

func _init(seed_val: int):
	rng = RandomNumberGenerator.new()
	rng.seed = seed_val

func generate(region_count: int) -> Dictionary:
	var stars = []
	var lanes = []
	var region_data = [] # Stores arrays of stars per region

	# 1. GENERATE STAR POSITIONS
	for r in range(region_count):
		var center = Vector3(rng.randf_range(-100, 100), 0, rng.randf_range(-100, 100))
		var star_count = rng.randi_range(4, 7)
		var current_region_stars = []

		for s in range(star_count):
			var pos = center + Vector3(rng.randf_range(-20, 20), rng.randf_range(-5, 5), rng.randf_range(-20, 20))
			var star = { "id": "star_" + str(stars.size()), "pos": pos, "region": r }
			stars.append(star)
			current_region_stars.append(star)
		
		region_data.append(current_region_stars)

	# 2. THE LOCAL MESH (Nearest Neighbors for Redundancy)
	for r_stars in region_data:
		for star in r_stars:
			# Sort other stars in the same region by distance
			var others = r_stars.filter(func(s): return s.id != star.id)
			others.sort_custom(func(a, b): return star.pos.distance_to(a.pos) < star.pos.distance_to(b.pos))
			
			# Connect to the 2 nearest neighbors (Creates triangles, not lines)
			for i in range(min(2, others.size())):
				var target = others[i]
				# Prevent duplicate lanes
				var duplicate = lanes.any(func(l): return (l.from == star.pos and l.to == target.pos) or (l.from == target.pos and l.to == star.pos))
				if not duplicate:
					lanes.append({ "from": star.pos, "to": target.pos })

	# 3. INTER-REGION ARTERIES & BYPASSES
	for r in range(region_count - 1):
		var r1 = region_data[r]
		var r2 = region_data[r+1]
		
		# The Main Artery: Capital to Capital (Index 0)
		lanes.append({ "from": r1[0].pos, "to": r2[0].pos })
		
		# The Smuggler's Run: Peripheral to Peripheral (Index -1)
		lanes.append({ "from": r1[-1].pos, "to": r2[-1].pos })

	return { "stars": stars, "lanes": lanes }
