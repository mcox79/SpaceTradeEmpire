shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Barren rocky world — Mercury/Moon reference.
//
// Key visual features:
// - Multi-scale craters (large basins + medium + small)
// - Large-scale albedo patches (dark "maria" lava plains vs bright highlands)
// - Bright crater rims, dark crater floors
// - Ray ejecta patterns from largest craters
// - No atmosphere — subtle rim silhouette glow only
// Per-seed color variation via seed_offset uniform.

uniform vec3 highland    : source_color = vec3(0.48, 0.44, 0.40);  // Bright anorthosite highlands
uniform vec3 lowland     : source_color = vec3(0.22, 0.20, 0.18);  // Dark basaltic maria
uniform vec3 crater_dark : source_color = vec3(0.12, 0.10, 0.08);  // Deep crater shadows
uniform vec3 rim_bright  : source_color = vec3(0.58, 0.54, 0.50);  // Fresh bright crater rims
uniform vec3 rim_color   : source_color = vec3(0.15, 0.15, 0.18);  // Rim silhouette glow
uniform float rock_scale : hint_range(2.0, 8.0) = 4.0;
uniform float rim_strength : hint_range(0.0, 1.0) = 0.3;
uniform float seed_offset : hint_range(0.0, 100.0) = 0.0;

varying vec3 model_pos;
void vertex() { model_pos = VERTEX; }

// ── Noise ──
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

// ── Worley craters ──
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

// Per-seed color shift.
vec3 seed_color_shift(vec3 base, float seed) {
	float h = fract(seed * 0.618034) * 0.2 - 0.1;
	return clamp(base + vec3(h * 0.8, h * 0.5, h * 0.3), 0.0, 1.0);
}

void fragment() {
	vec3 p = normalize(model_pos) * rock_scale + vec3(seed_offset);

	// ── Large-scale albedo: maria (dark lava plains) vs highlands ──
	float terrain = fbm(p * 0.35, 4);
	float maria_mask = smoothstep(0.3, 0.55, terrain); // 0 = maria, 1 = highlands

	vec3 my_highland = seed_color_shift(highland, seed_offset);
	vec3 my_lowland = seed_color_shift(lowland, seed_offset + 1.0);
	vec3 my_crater_dark = seed_color_shift(crater_dark, seed_offset + 2.0);
	vec3 my_rim_bright = seed_color_shift(rim_bright, seed_offset + 3.0);

	// Base terrain with visible albedo dichotomy.
	vec3 col = mix(my_lowland, my_highland, maria_mask);

	// ── Craters: 3 scales for realistic density ──
	// Large basins (few, dominant).
	float c1 = worley(p * 0.5);
	float basin_floor = smoothstep(0.06, 0.30, c1);
	float basin_rim = smoothstep(0.25, 0.38, c1) * (1.0 - smoothstep(0.38, 0.55, c1));
	col = mix(my_crater_dark, col, basin_floor);
	col = mix(col, my_rim_bright, basin_rim * 0.8);

	// Medium craters (many).
	float c2 = worley(p * 1.8 + vec3(11.3, 5.7, 17.1));
	float med_floor = smoothstep(0.05, 0.28, c2);
	float med_rim = smoothstep(0.22, 0.35, c2) * (1.0 - smoothstep(0.35, 0.52, c2));
	col = mix(my_crater_dark * 1.1, col, med_floor);
	col = mix(col, my_rim_bright * 0.9, med_rim * 0.6);

	// Small craters (dense — mostly in highlands, less in maria).
	float c3 = worley(p * 5.0 + vec3(23.1, 8.3, 31.7));
	float small_floor = smoothstep(0.04, 0.20, c3);
	float small_rim = smoothstep(0.16, 0.25, c3) * (1.0 - smoothstep(0.25, 0.38, c3));
	float small_density = 0.3 + maria_mask * 0.5; // More small craters in highlands.
	col = mix(my_crater_dark * 1.2, col, mix(1.0, small_floor, small_density));
	col = mix(col, my_rim_bright * 0.85, small_rim * small_density * 0.4);

	// ── Ray ejecta from large impacts (bright radial streaks) ──
	// Use FBM at an angle relative to basin center — simplified as noise streaks.
	float ejecta = fbm(p * 2.0, 3);
	float ejecta_mask = (1.0 - smoothstep(0.3, 0.6, c1)) * smoothstep(0.1, 0.3, c1);
	col = mix(col, my_rim_bright * 0.95, ejecta_mask * smoothstep(0.55, 0.75, ejecta) * 0.3);

	// ── Fine surface roughness ──
	float detail = fbm(p * 4.0 + vec3(7.3), 3);
	col += (detail - 0.5) * 0.04;

	ALBEDO = col;
	ROUGHNESS = 0.95 - detail * 0.1;
	METALLIC = 0.0;

	// Subtle rim silhouette — not atmosphere, just edge visibility.
	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = rim_color * pow(rim, 5.0) * rim_strength;
}
