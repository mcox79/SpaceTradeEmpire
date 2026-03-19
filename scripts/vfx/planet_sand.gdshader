shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Desert/sand world — Mars/Arrakis reference.
//
// Key visual features:
// - Large-scale albedo variation (dark basaltic lowlands vs bright dusty highlands)
// - Visible dune ridge patterns (domain-warped parallel ridges)
// - Impact craters (Worley-based, multi-scale)
// - Polar dust caps (lighter tone at poles)
// - Warm dusty atmosphere haze at rim
// Per-seed color variation via seed_offset uniform.

uniform vec3 sand_bright : source_color = vec3(0.90, 0.75, 0.48); // Bright dusty highland
uniform vec3 sand_mid    : source_color = vec3(0.75, 0.55, 0.30); // Mid-tone terrain
uniform vec3 dark_plain  : source_color = vec3(0.42, 0.28, 0.16); // Dark basaltic lowland
uniform vec3 dune_crest  : source_color = vec3(0.98, 0.85, 0.60); // Bright wind-blown dune crests
uniform vec3 crater_floor: source_color = vec3(0.30, 0.22, 0.12); // Dark crater interiors
uniform vec3 atmo_color  : source_color = vec3(0.95, 0.65, 0.35); // Dusty orange atmosphere
uniform float terrain_scale : hint_range(2.0, 8.0) = 4.5;
uniform float rotation_speed : hint_range(0.0, 0.02) = 0.006;
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

// ── Worley craters (multi-scale) ──
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
	float h = fract(seed * 0.618034) * 0.15 - 0.075;
	float s = fract(seed * 0.314159) * 0.1 - 0.05;
	return clamp(base + vec3(h, h * 0.7, -h * 0.3) + s, 0.0, 1.0);
}

void fragment() {
	vec3 n = normalize(model_pos);
	float t = TIME * rotation_speed;
	mat3 rot = mat3(vec3(cos(t),0,sin(t)), vec3(0,1,0), vec3(-sin(t),0,cos(t)));
	vec3 p = rot * n * terrain_scale + vec3(seed_offset);

	// ── Large-scale albedo: bright highlands vs dark lowlands (Mars dichotomy) ──
	float terrain = fbm(p * 0.4, 4);
	float highland_mask = smoothstep(0.35, 0.65, terrain);

	vec3 my_bright = seed_color_shift(sand_bright, seed_offset);
	vec3 my_mid = seed_color_shift(sand_mid, seed_offset + 1.0);
	vec3 my_dark = seed_color_shift(dark_plain, seed_offset + 2.0);
	vec3 my_crest = seed_color_shift(dune_crest, seed_offset + 3.0);
	vec3 my_crater = seed_color_shift(crater_floor, seed_offset + 4.0);

	// Base terrain: dark lowlands → mid → bright highlands.
	vec3 col = mix(my_dark, my_mid, smoothstep(0.25, 0.45, terrain));
	col = mix(col, my_bright, smoothstep(0.55, 0.75, terrain));

	// ── Dune ridges: domain-warped parallel ridges in lowlands ──
	vec3 dune_warp = p + vec3(
		gnoise3(p * 0.3 + vec3(seed_offset)) * 2.0,
		gnoise3(p * 0.3 + vec3(seed_offset + 5.0)) * 1.5,
		gnoise3(p * 0.3 + vec3(seed_offset + 10.0)) * 2.0
	);
	// Parallel ridges via sin of warped coordinate.
	float ridges = sin(dune_warp.x * 4.0 + dune_warp.z * 3.0) * 0.5 + 0.5;
	ridges = pow(ridges, 0.6); // Sharpen ridge crests.
	// Dunes only in lowlands (where there's loose sand to blow).
	float dune_region = 1.0 - highland_mask;
	col = mix(col, my_crest, ridges * dune_region * 0.35);

	// ── Craters: two scales for visual variety ──
	// Large craters.
	float c_large = worley(p * 0.6);
	float crater_lg = smoothstep(0.08, 0.35, c_large);
	vec3 crater_rim_lg = my_bright * 1.1;
	col = mix(my_crater, col, crater_lg);
	// Bright crater rims.
	col = mix(col, crater_rim_lg, smoothstep(0.3, 0.42, c_large) * (1.0 - smoothstep(0.42, 0.6, c_large)));

	// Small craters (highlands only — lowlands are young lava plains).
	float c_small = worley(p * 2.5 + vec3(17.3));
	float crater_sm = smoothstep(0.05, 0.25, c_small);
	col = mix(my_crater * 1.2, col, mix(1.0, crater_sm, highland_mask * 0.6));

	// ── Fine surface detail ──
	float detail = fbm(p * 3.5 + vec3(11.7), 3);
	col += (detail - 0.5) * 0.06;

	// ── Polar dust caps (slightly lighter) ──
	float pole = abs(n.y);
	col = mix(col, my_bright * 1.05, smoothstep(0.78, 0.92, pole));

	ALBEDO = col;
	ROUGHNESS = 0.92; METALLIC = 0.0;

	// Dusty atmosphere haze at rim.
	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 3.0) * 1.4;
}
