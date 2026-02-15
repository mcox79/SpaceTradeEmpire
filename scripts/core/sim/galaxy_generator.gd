extends RefCounted

var rng_streams: RngStreams

var rng: RandomNumberGenerator

func _init(seed_val: int):
	rng_streams = RngStreams.new(seed_val)
	rng = rng_streams.get_stream(RngStreams.STREAM_GALAXY_GEN)

func generate(region_count: int) -> Dictionary:
	var stars = []
	var lanes = []
	var region_data = []

	# 1. GENERATE STAR POSITIONS
	for r in range(region_count):
		var center = Vector3(rng.randf_range(-100, 100), 0, rng.randf_range(-100, 100))
		var star_count = rng.randi_range(4, 7)
		var current_region_stars = []

		for s in range(star_count):
			var pos = center + Vector3(rng.randf_range(-20, 20), rng.randf_range(-5, 5), rng.randf_range(-20, 20))
			var star = { 'id': 'star_' + str(stars.size()), 'pos': pos, 'region': r }
			stars.append(star)
			current_region_stars.append(star)

		region_data.append(current_region_stars)

	# 2. LOCAL MESH (Smart Connections)
	for r_stars in region_data:
		for star in r_stars:
			var others = r_stars.filter(func(s): return s.id != star.id)
			others.sort_custom(func(a, b): return star.pos.distance_to(a.pos) < star.pos.distance_to(b.pos))

			for i in range(min(2, others.size())):
				var target = others[i]
				# Check duplicates using IDs instead of positions
				var duplicate = lanes.any(func(l):
					return (l.u == star.id and l.v == target.id) or (l.u == target.id and l.v == star.id)
				)
				if not duplicate:
					lanes.append({
						'u': star.id,
						'v': target.id,
						'from': star.pos,
						'to': target.pos
					})

	# 3. INTER-REGION ARTERIES
	for r in range(region_count - 1):
		var r1 = region_data[r]
		var r2 = region_data[r+1]

		# Main Artery
		lanes.append({
			'u': r1[0].id,
			'v': r2[0].id,
			'from': r1[0].pos,
			'to': r2[0].pos
		})

		# Smuggler Route
		lanes.append({
			'u': r1[-1].id,
			'v': r2[-1].id,
			'from': r1[-1].pos,
			'to': r2[-1].pos
		})

	return { 'stars': stars, 'lanes': lanes }
