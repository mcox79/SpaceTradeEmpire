shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Procedural ice world: cracked ice plains, subsurface blue, frosty haze.

uniform vec3 ice_base  : source_color = vec3(0.75, 0.88, 1.0);
uniform vec3 ice_crack : source_color = vec3(0.3, 0.55, 0.8);
uniform vec3 ice_deep  : source_color = vec3(0.05, 0.2, 0.5);
uniform vec3 atmo_color: source_color = vec3(0.6, 0.8, 1.0);
uniform float crack_scale : hint_range(2.0, 8.0) = 4.0;
uniform float rotation_speed : hint_range(0.0, 0.02) = 0.005;

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

// Worley-like crack pattern (uses scalar hash for cell jitter).
float hash1(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453123);
}
float cracks(vec3 p) {
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
	vec3 p = normalize(model_pos);
	float t = TIME * rotation_speed;
	mat3 rot = mat3(vec3(cos(t),0,sin(t)), vec3(0,1,0), vec3(-sin(t),0,cos(t)));
	vec3 q = rot * p * crack_scale;

	float c = cracks(q);
	float n = fbm(q * 0.8, 4);

	vec3 col = mix(ice_crack, ice_base, smoothstep(0.0, 0.4, c));
	col = mix(ice_deep, col, smoothstep(0.0, 0.5, c + n * 0.3));

	ALBEDO = col;
	ROUGHNESS = mix(0.05, 0.25, n);
	METALLIC = 0.1;

	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 4.0) * 0.8;
}
