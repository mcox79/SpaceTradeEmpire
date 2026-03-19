shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

// Gas giant — Jupiter/Saturn reference.
//
// Key visual features:
// - Strong contrast bands: bright cream/white zones, dark brown/red belts
// - Turbulent chaotic edges between zones and belts (eddies, festoons)
// - Great storm vortex with spiral structure
// - Subtle color variation within each band (not flat color)
// - Atmospheric limb darkening + haze glow at rim
// Per-seed color variation via seed_offset uniform.

uniform vec3 zone_bright : source_color = vec3(0.95, 0.88, 0.72);  // Cream/tan zones
uniform vec3 belt_dark   : source_color = vec3(0.55, 0.32, 0.15);  // Brown/russet belts
uniform vec3 belt_deep   : source_color = vec3(0.35, 0.18, 0.08);  // Deep brown belt cores
uniform vec3 storm_color : source_color = vec3(0.82, 0.45, 0.22);  // Reddish storm
uniform vec3 atmo_color  : source_color = vec3(0.85, 0.70, 0.50);  // Atmospheric haze
uniform float band_freq : hint_range(4.0, 16.0) = 10.0;
uniform float band_warp : hint_range(0.0, 0.8) = 0.4;
uniform float rotation_speed : hint_range(0.0, 0.1) = 0.04;
uniform float storm_latitude : hint_range(-0.6, 0.6) = -0.22;
uniform float seed_offset : hint_range(0.0, 100.0) = 0.0;

varying vec3 model_pos;
void vertex() { model_pos = VERTEX; }

// ── Noise primitives ──
vec3 hash33(vec3 p) {
	p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
	         dot(p, vec3(269.5, 183.3, 246.1)),
	         dot(p, vec3(113.5, 271.9, 124.6)));
	return fract(sin(p) * 43758.5453123) * 2.0 - 1.0;
}
float gnoise3(vec3 p) {
	vec3 i = floor(p); vec3 f = fract(p);
	vec3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	return mix(mix(mix(dot(hash33(i+vec3(0,0,0)), f-vec3(0,0,0)),
	                   dot(hash33(i+vec3(1,0,0)), f-vec3(1,0,0)), u.x),
	               mix(dot(hash33(i+vec3(0,1,0)), f-vec3(0,1,0)),
	                   dot(hash33(i+vec3(1,1,0)), f-vec3(1,1,0)), u.x), u.y),
	           mix(mix(dot(hash33(i+vec3(0,0,1)), f-vec3(0,0,1)),
	                   dot(hash33(i+vec3(1,0,1)), f-vec3(1,0,1)), u.x),
	               mix(dot(hash33(i+vec3(0,1,1)), f-vec3(0,1,1)),
	                   dot(hash33(i+vec3(1,1,1)), f-vec3(1,1,1)), u.x), u.y),
	           u.z) * 0.5 + 0.5;
}
float fbm(vec3 p, int oct) {
	float v = 0.0, a = 0.5, freq = 1.0;
	for (int i = 0; i < oct; i++) { v += a * gnoise3(p * freq); freq *= 2.0; a *= 0.5; }
	return v;
}

// ── Worley noise for storm structure ──
float hash1(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453123);
}
float worley(vec3 p) {
	vec3 i = floor(p);
	float min_d = 10.0;
	for (int x = -1; x <= 1; x++)
	for (int y = -1; y <= 1; y++)
	for (int z = -1; z <= 1; z++) {
		vec3 nb = i + vec3(float(x), float(y), float(z));
		vec3 pt = nb + vec3(hash1(nb), hash1(nb + 31.7), hash1(nb + 17.3));
		min_d = min(min_d, length(p - pt));
	}
	return min_d;
}

// Per-seed color shift — gas giants get wider hue range.
vec3 seed_color_shift(vec3 base, float seed) {
	float h = fract(seed * 0.618034) * 0.25 - 0.125;
	float s = fract(seed * 0.314159) * 0.15 - 0.075;
	return clamp(base + vec3(h, h * 0.5, -h) + s, 0.0, 1.0);
}

void fragment() {
	vec3 n = normalize(model_pos);
	float t = TIME * rotation_speed;

	vec3 my_zone = seed_color_shift(zone_bright, seed_offset);
	vec3 my_belt = seed_color_shift(belt_dark, seed_offset + 1.0);
	vec3 my_deep = seed_color_shift(belt_deep, seed_offset + 2.0);
	vec3 my_storm = seed_color_shift(storm_color, seed_offset + 3.0);

	// ── Latitude bands with turbulent edges ──
	// Low-freq warp for large-scale band distortion.
	float warp_large = fbm(n * 2.5 + vec3(t * 0.3, 0.0, seed_offset), 3) * band_warp;
	// High-freq warp for turbulent eddies at band boundaries.
	float warp_fine = fbm(n * 8.0 + vec3(t * 0.8, 0.0, seed_offset + 7.3), 3) * band_warp * 0.3;

	float lat = n.y + warp_large + warp_fine;
	float band = sin(lat * PI * band_freq);

	// Sharp band transitions (not smooth gradients) — zones have hard edges.
	float zone_mask = smoothstep(-0.15, 0.15, band);
	vec3 col = mix(my_belt, my_zone, zone_mask);

	// Deep belt cores: darkest at band centers.
	float belt_core = (1.0 - zone_mask) * smoothstep(0.5, 0.9, abs(band));
	col = mix(col, my_deep, belt_core * 0.7);

	// ── Intra-band detail: streaks within zones/belts ──
	float lon = atan(n.x, n.z) + t;
	float streak = gnoise3(vec3(lon * 3.0, n.y * band_freq * 2.0, seed_offset)) * 0.5 + 0.5;
	col *= 0.88 + streak * 0.24; // ±12% brightness variation within bands.

	// ── Turbulent eddies at band boundaries (festoons) ──
	float boundary = 1.0 - smoothstep(0.0, 0.25, abs(band));
	float eddy = worley(vec3(lon * 2.0, n.y * 15.0, t * 0.5 + seed_offset)) * 0.6;
	eddy += fbm(vec3(lon * 4.0, n.y * 20.0, t * 0.3), 2) * 0.4;
	// Eddies brighten/darken at boundaries — chaotic mixing.
	col = mix(col, mix(my_zone, my_belt, eddy), boundary * 0.5);

	// ── Great storm vortex ──
	float storm_lon = atan(n.x, n.z) / PI - t * 0.5;
	float storm_lat = n.y - storm_latitude;
	float storm_dist = length(vec2(storm_lon, storm_lat * 1.5));

	// Spiral structure within the storm.
	float spiral_angle = atan(storm_lat, storm_lon);
	float spiral = sin(spiral_angle * 3.0 - storm_dist * 12.0 + t * 2.0) * 0.5 + 0.5;
	float storm_detail = fbm(vec3(storm_dist * 10.0, spiral_angle * 2.0, t * 0.4), 3);

	float storm_mask = smoothstep(0.22, 0.06, storm_dist);
	vec3 storm_col = my_storm * (0.7 + spiral * 0.3 + storm_detail * 0.2);
	// Storm eye: slightly darker center.
	storm_col = mix(storm_col, my_deep * 1.5, smoothstep(0.04, 0.0, storm_dist));
	col = mix(col, storm_col, storm_mask);

	// ── Viewing angle effects ──
	float mu = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);

	// Limb darkening — atmospheric absorption at shallow angles.
	col *= 0.7 + 0.3 * mu;

	ALBEDO = col;
	ROUGHNESS = 0.6; METALLIC = 0.0;

	// Atmospheric rim haze.
	float rim = pow(1.0 - mu, 3.0);
	EMISSION = atmo_color * rim * 1.5;
}
