shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

// Procedural lava world: flowing magma channels, cooled crust, volcanic glow.

uniform vec3 rock_dark : source_color = vec3(0.08, 0.04, 0.03);
uniform vec3 rock_mid  : source_color = vec3(0.18, 0.1, 0.05);
uniform vec3 lava_glow : source_color = vec3(1.0, 0.35, 0.0);
uniform vec3 lava_hot  : source_color = vec3(1.0, 0.85, 0.4);
uniform vec3 atmo_color: source_color = vec3(1.0, 0.3, 0.05);
uniform float lava_scale : hint_range(1.0, 6.0) = 3.0;
uniform float lava_speed : hint_range(0.01, 0.2) = 0.08;

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

void fragment() {
	vec3 p = normalize(model_pos) * lava_scale;
	float t = TIME * lava_speed;

	float n1 = fbm(p + vec3(t, 0.0, t * 0.7), 5);
	float n2 = fbm(p * 1.7 - vec3(t * 0.4, t * 0.3, 0.0), 3);
	float lava = n1 * 0.7 + n2 * 0.3;

	vec3 col;
	float glow = 0.0;
	if (lava < 0.38) {
		float heat = 1.0 - (lava / 0.38);
		col = mix(lava_glow, lava_hot, heat * heat);
		glow = mix(1.0, 2.5, heat * heat);
	} else {
		float rock_t = (lava - 0.38) / 0.62;
		col = mix(rock_dark, rock_mid, rock_t);
		glow = smoothstep(0.45, 0.38, lava) * 0.3;
	}

	ALBEDO = col;
	ROUGHNESS = 0.95;
	EMISSION = col * glow + atmo_color * pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 2.5) * 2.0;
}
