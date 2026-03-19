shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

// Lava/volcanic world — Io/Mustafar reference.
//
// Key visual features:
// - Dark volcanic calderas (Worley-based irregular shapes)
// - Bright magma veins/channels (ridge noise) between cooled crust plates
// - Cooled crust plates with subtle color variation (dark basalt to mid-gray)
// - Hottest magma at caldera edges and vein centers (yellow-white)
// - Strong emissive glow from magma, visible at distance
// - Red-orange atmospheric haze at rim
// Per-seed color variation via seed_offset uniform.

uniform vec3 crust_cold  : source_color = vec3(0.06, 0.03, 0.02);  // Cold basalt crust
uniform vec3 crust_warm  : source_color = vec3(0.20, 0.08, 0.03);  // Warm crust near magma
uniform vec3 magma_glow  : source_color = vec3(1.0, 0.35, 0.0);    // Orange magma
uniform vec3 magma_hot   : source_color = vec3(1.0, 0.85, 0.40);   // Yellow-white hottest
uniform vec3 atmo_color  : source_color = vec3(1.0, 0.3, 0.05);    // Volcanic haze
uniform float lava_scale : hint_range(1.0, 6.0) = 3.5;
uniform float lava_speed : hint_range(0.01, 0.2) = 0.06;
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

// Ridge noise: bright crack lines.
float ridge_noise(vec3 p) {
	return 1.0 - abs(gnoise3(p) * 2.0 - 1.0);
}
float ridge_fbm(vec3 p, int oct) {
	float v = 0.0, a = 0.5, freq = 1.0;
	for (int i = 0; i < oct; i++) { v += a * ridge_noise(p * freq); freq *= 2.0; a *= 0.5; }
	return v;
}

// ── Worley for volcanic calderas ──
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
	float h = fract(seed * 0.618034) * 0.2 - 0.1;
	return clamp(base + vec3(h, h * 0.3, -h * 0.5), 0.0, 1.0);
}

void fragment() {
	vec3 p = normalize(model_pos) * lava_scale + vec3(seed_offset);
	float t = TIME * lava_speed;

	vec3 my_crust_cold = seed_color_shift(crust_cold, seed_offset);
	vec3 my_crust_warm = seed_color_shift(crust_warm, seed_offset + 1.0);
	vec3 my_magma_glow = seed_color_shift(magma_glow, seed_offset + 2.0);
	vec3 my_magma_hot = seed_color_shift(magma_hot, seed_offset + 3.0);

	// ── Volcanic calderas (Worley F1 — dark pools at cell centers) ──
	vec2 w = worley_f1f2(p * 0.8 + vec3(t * 0.01));
	float caldera_mask = 1.0 - smoothstep(0.15, 0.40, w.x);
	// Caldera rim: ring of activity around each volcanic basin.
	float caldera_rim = smoothstep(0.12, 0.22, w.x) * (1.0 - smoothstep(0.22, 0.40, w.x));

	// ── Magma veins between crust plates (F2-F1 at cell boundaries) ──
	float vein_width = w.y - w.x;
	float vein_mask = 1.0 - smoothstep(0.0, 0.12, vein_width);

	// ── Ridge noise for finer magma channels ──
	float ridges = ridge_fbm(p * 1.5 + vec3(t, 0.0, t * 0.7), 3);
	float fine_veins = smoothstep(0.55, 0.85, ridges);

	// ── Crust variation: cold basalt with warm zones near activity ──
	float crust_var = fbm(p * 2.0 + vec3(5.3), 3);
	vec3 crust_col = mix(my_crust_cold, my_crust_warm, crust_var * 0.6 + caldera_rim * 0.4);

	// ── Color composition ──
	vec3 col = crust_col;
	float glow = 0.0;

	// Magma veins at plate boundaries: bright orange.
	float total_vein = max(vein_mask, fine_veins * 0.5);
	col = mix(col, my_magma_glow, total_vein);
	glow = total_vein * 1.8;

	// Caldera interior: glowing magma pool.
	float caldera_heat = caldera_mask * (0.6 + fbm(p * 3.0 + vec3(t * 0.5), 2) * 0.4);
	col = mix(col, my_magma_hot, caldera_heat);
	glow = max(glow, caldera_heat * 2.5);

	// Caldera rim: brightest activity (eruption zone).
	col = mix(col, my_magma_hot * 1.1, caldera_rim * 0.7);
	glow = max(glow, caldera_rim * 2.0);

	// Slight FBM shimmer in hot areas (convection).
	float shimmer = fbm(p * 4.0 + vec3(t * 2.0, t * 1.3, t * 0.7), 2);
	glow *= 0.85 + shimmer * 0.3;

	ALBEDO = col;
	ROUGHNESS = 0.95;

	// ── Emission: magma glow + volcanic atmosphere rim ──
	float rim = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 2.5);
	EMISSION = col * glow + atmo_color * rim * 2.0;
}
