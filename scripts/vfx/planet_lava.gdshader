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

float hash3(vec3 p) {
	p = fract(p * vec3(443.8975, 397.2973, 491.1871));
	p += dot(p.zxy, p.yxz + 19.19);
	return fract(p.x * p.y * p.z);
}
float vnoise3(vec3 p) {
	vec3 i = floor(p); vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(
		mix(mix(hash3(i),             hash3(i+vec3(1,0,0)), f.x),
		    mix(hash3(i+vec3(0,1,0)), hash3(i+vec3(1,1,0)), f.x), f.y),
		mix(mix(hash3(i+vec3(0,0,1)), hash3(i+vec3(1,0,1)), f.x),
		    mix(hash3(i+vec3(0,1,1)), hash3(i+vec3(1,1,1)), f.x), f.y),
		f.z);
}
float fbm(vec3 p, int oct) {
	float v = 0.0, a = 0.5, freq = 1.0;
	for (int i = 0; i < oct; i++) { v += a * vnoise3(p * freq); freq *= 2.0; a *= 0.5; }
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
