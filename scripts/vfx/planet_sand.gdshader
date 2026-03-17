shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Procedural desert/sand world: dune ridges, rocky outcrops, dusty atmosphere.

uniform vec3 sand_light : source_color = vec3(0.92, 0.78, 0.5);
uniform vec3 sand_dark  : source_color = vec3(0.72, 0.52, 0.28);
uniform vec3 dune_ridge : source_color = vec3(0.98, 0.88, 0.65);
uniform vec3 rock_color : source_color = vec3(0.5, 0.36, 0.22);
uniform vec3 atmo_color : source_color = vec3(0.95, 0.7, 0.4);
uniform float dune_scale : hint_range(2.0, 8.0) = 5.0;
uniform float rotation_speed : hint_range(0.0, 0.02) = 0.008;

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
	vec3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // quintic hermite
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

void fragment() {
	vec3 n = normalize(model_pos);
	float t = TIME * rotation_speed;
	mat3 rot = mat3(vec3(cos(t),0,sin(t)), vec3(0,1,0), vec3(-sin(t),0,cos(t)));
	vec3 p = rot * n * dune_scale;

	float macro = fbm(p * 0.5, 3);
	// Dune ridges: use noise-warped domain instead of axis-aligned sin()
	// to avoid straight-line artifacts on the sphere.
	vec3 warped = p + vec3(macro * 2.5, macro * 1.8, macro * 2.1);
	float dune_dir = fbm(warped * 0.8, 2);
	float micro = fbm(p * 4.0 + vec3(17.3), 2);

	float ridge = smoothstep(0.4, 0.7, dune_dir);
	vec3 col = mix(sand_dark, sand_light, macro);
	col = mix(col, dune_ridge, ridge * 0.6);
	col = mix(col, rock_color, smoothstep(0.6, 0.78, micro));

	ALBEDO = col;
	ROUGHNESS = 0.9; METALLIC = 0.0;

	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 3.5) * 1.2;
}
