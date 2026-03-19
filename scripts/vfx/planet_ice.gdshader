shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Ice world shader — Europa/Enceladus reference.
//
// Key visual features of real ice moons:
// - Network of thin cracks/lineae at cell BOUNDARIES (F2-F1 Worley)
// - Smooth ice plains between cracks (not uniform — subtle blue variation)
// - Brownish/tan discoloration along major cracks (impurities from subsurface)
// - Some regions smoother (fresh ice), some rougher (chaos terrain)
// - Frosty blue-white base, not pure white
// - Subsurface scattering glow at rim (blue atmosphere haze)

uniform vec3 ice_base   : source_color = vec3(0.78, 0.87, 0.95);   // Blue-white ice
uniform vec3 ice_blue   : source_color = vec3(0.45, 0.65, 0.85);   // Deep blue subsurface
uniform vec3 crack_color: source_color = vec3(0.35, 0.28, 0.22);   // Brownish crack stain
uniform vec3 atmo_color : source_color = vec3(0.5, 0.72, 1.0);     // Blue haze
uniform float crack_scale : hint_range(4.0, 16.0) = 9.0;
uniform float crack_width : hint_range(0.02, 0.15) = 0.06;
uniform float rotation_speed : hint_range(0.0, 0.02) = 0.005;
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

// ── Worley F1 and F2 (returns both closest and second-closest distances) ──
float hash1(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453123);
}
vec2 worley_f1f2(vec3 p) {
	vec3 i = floor(p);
	float f1 = 10.0, f2 = 10.0;
	for (int x = -1; x <= 1; x++)
	for (int y = -1; y <= 1; y++)
	for (int z = -1; z <= 1; z++) {
		vec3 nb = i + vec3(float(x), float(y), float(z));
		vec3 pt = nb + vec3(hash1(nb), hash1(nb + 31.7), hash1(nb + 17.3));
		float d = length(p - pt);
		if (d < f1) { f2 = f1; f1 = d; }
		else if (d < f2) { f2 = d; }
	}
	return vec2(f1, f2);
}

// Per-seed color shift.
vec3 seed_color_shift(vec3 base, float seed) {
	float h = fract(seed * 0.618034) * 0.12 - 0.06;
	return clamp(base + vec3(h * 0.3, h * 0.5, h), 0.0, 1.0);
}

void fragment() {
	vec3 p = normalize(model_pos);
	float t = TIME * rotation_speed;
	mat3 rot = mat3(vec3(cos(t),0,sin(t)), vec3(0,1,0), vec3(-sin(t),0,cos(t)));
	vec3 q = rot * p * crack_scale + vec3(seed_offset);

	// ── Crack network: F2-F1 Worley = thin lines at cell boundaries ──
	// Large-scale crack network.
	vec2 w1 = worley_f1f2(q);
	float edges_large = w1.y - w1.x; // Thin at boundaries, wide in cell interiors.

	// Secondary finer crack layer (2x frequency, offset).
	vec2 w2 = worley_f1f2(q * 2.2 + vec3(17.3, 5.7, 11.1));
	float edges_fine = w2.y - w2.x;

	// Combine: major cracks + minor fractures.
	float crack_mask_large = 1.0 - smoothstep(0.0, crack_width, edges_large);
	float crack_mask_fine = 1.0 - smoothstep(0.0, crack_width * 0.7, edges_fine);
	float crack_mask = max(crack_mask_large, crack_mask_fine * 0.6);

	// ── Surface variation: FBM for albedo patches (chaos terrain vs smooth plains) ──
	float terrain = fbm(q * 0.6, 4);
	float detail = fbm(q * 2.5, 3);

	// ── Color composition ──
	vec3 my_ice_base = seed_color_shift(ice_base, seed_offset);
	vec3 my_ice_blue = seed_color_shift(ice_blue, seed_offset + 1.0);
	vec3 my_crack = seed_color_shift(crack_color, seed_offset + 2.0);

	// Base ice: varies between blue-white and deeper blue.
	vec3 ice = mix(my_ice_base, my_ice_blue, terrain * 0.5 + detail * 0.2);

	// Subtle brightness variation (not uniform albedo).
	ice *= 0.85 + terrain * 0.3;

	// Apply cracks: brownish stain along fracture lines.
	vec3 col = mix(ice, my_crack, crack_mask);

	// Slight bluish tint in the deepest cracks (subsurface ocean showing through).
	col = mix(col, my_ice_blue * 0.6, crack_mask_large * 0.3);

	ALBEDO = col;
	ROUGHNESS = mix(0.08, 0.35, crack_mask + detail * 0.2);
	METALLIC = 0.05;

	// ── Frosty rim glow ──
	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 3.5) * 0.9;
}
