extends RefCounted

var rng_streams: RngStreams
var rng: RandomNumberGenerator
var seed: int = 0

func _init(seed_val: int = 0):
	seed = seed_val
	rng_streams = RngStreams.new(seed_val)
	rng = rng_streams.get_stream(RngStreams.STREAM_GALAXY_GEN)

# Entry point for GameShell worldgen: use the canonical SimState seed, without mutating it.
func generate_for_state(state: SimState, region_count: int) -> Dictionary:
	var local_streams := RngStreams.new(state.initial_seed)
	var local_rng := local_streams.get_stream(RngStreams.STREAM_GALAXY_GEN)
	return _generate_with_rng(local_rng, region_count)

# Back-compat entry point: uses this instance's configured RNG.
func generate(region_count: int) -> Dictionary:
	return _generate_with_rng(rng, region_count)

func _generate_with_rng(r: RandomNumberGenerator, region_count: int) -> Dictionary:
	var stars = []
	var lanes = []
	var region_data = []

	# 1. GENERATE STAR POSITIONS
	for region_i in range(region_count):
		var center = Vector3(r.randf_range(-100, 100), 0, r.randf_range(-100, 100))
		var star_count = r.randi_range(4, 7)
		var current_region_stars = []

		for _s in range(star_count):
			var pos = center + Vector3(r.randf_range(-20, 20), r.randf_range(-5, 5), r.randf_range(-20, 20))
			var star = { 'id': 'star_' + str(stars.size()), 'pos': pos, 'region': region_i }
			stars.append(star)
			current_region_stars.append(star)

		region_data.append(current_region_stars)

	# 2. LOCAL MESH (Smart Connections, stable ordering)
	for r_stars in region_data:
		for star in r_stars:
			var others = r_stars.filter(func(s): return s.id != star.id)
			others.sort_custom(func(a, b):
				var da: float = star.pos.distance_squared_to(a.pos)
				var db: float = star.pos.distance_squared_to(b.pos)
				if da == db:
					return String(a.id) < String(b.id)
				return da < db
			)

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
	for region_i in range(region_count - 1):
		var r1 = region_data[region_i]
		var r2 = region_data[region_i + 1]

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
