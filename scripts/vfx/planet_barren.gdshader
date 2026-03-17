shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Procedural barren rocky world: cratered surface, no atmosphere.

uniform vec3 rock_a      : source_color = vec3(0.32, 0.28, 0.25);
uniform vec3 rock_b      : source_color = vec3(0.48, 0.42, 0.35);
uniform vec3 crater_floor: source_color = vec3(0.18, 0.15, 0.12);
uniform float rock_scale : hint_range(2.0, 8.0) = 4.0;

varying vec3 model_pos;
void vertex() { model_pos = VERTEX; }

// ── Gradient noise (no lattice artifacts) ──
vec3 hash33(vec3 p) {
	p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
	         dot(p, vec3(269.5, 183.3, 246.1)),
	         dot(p, vec3(113.5, 271.9, 124.6)));
	return fract(sin(p) * 43758.5453123) * 2.0 - 1.0;
}
float gnoise3(vec3 p) {
	vec3 i = floor(p); vec3 f = fract(p);
	vec3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	return mix(mix(mix(dot(hash33(i + vec3(0,0,0)), f - vec3(0,0,0)),
	                   dot(hash33(i + vec3(1,0,0)), f - vec3(1,0,0)), u.x),
	               mix(dot(hash33(i + vec3(0,1,0)), f - vec3(0,1,0)),
	                   dot(hash33(i + vec3(1,1,0)), f - vec3(1,1,0)), u.x), u.y),
	           mix(mix(dot(hash33(i + vec3(0,0,1)), f - vec3(0,0,1)),
	                   dot(hash33(i + vec3(1,0,1)), f - vec3(1,0,1)), u.x),
	               mix(dot(hash33(i + vec3(0,1,1)), f - vec3(0,1,1)),
	                   dot(hash33(i + vec3(1,1,1)), f - vec3(1,1,1)), u.x), u.y),
	           u.z) * 0.5 + 0.5;
}
float fbm(vec3 p, int oct) {
	float v = 0.0, a = 0.5, freq = 1.0;
	for (int i = 0; i < oct; i++) { v += a * gnoise3(p * freq); freq *= 2.0; a *= 0.5; }
	return v;
}

// Worley-like craters (uses scalar hash for cell jitter).
float hash1(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453123);
}
float craters(vec3 p) {
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

void fragment() {
	vec3 p = normalize(model_pos) * rock_scale;
	float n  = fbm(p, 5);
	float n2 = fbm(p * 3.7 + 17.3, 3);
	float c  = craters(p * 0.7);

	vec3 col = mix(rock_a, rock_b, n);
	col = mix(crater_floor, col, smoothstep(0.1, 0.5, c));
	col += (n2 - 0.5) * 0.1;

	ALBEDO = col;
	ROUGHNESS = 0.95 - n2 * 0.1;
	METALLIC = 0.0;
	// No EMISSION — airless body.
}
