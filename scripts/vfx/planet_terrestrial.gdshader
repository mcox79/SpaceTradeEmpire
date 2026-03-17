shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Procedural terrestrial planet: continents, oceans, polar ice caps, slow rotation.

uniform vec3 ocean_deep    : source_color = vec3(0.01, 0.12, 0.4);
uniform vec3 ocean_shallow : source_color = vec3(0.05, 0.35, 0.7);
uniform vec3 land_low      : source_color = vec3(0.25, 0.55, 0.15);
uniform vec3 land_high     : source_color = vec3(0.45, 0.38, 0.28);
uniform vec3 snow_cap      : source_color = vec3(0.9, 0.92, 1.0);
uniform vec3 atmo_color    : source_color = vec3(0.3, 0.55, 1.0);
uniform float continent_scale : hint_range(1.0, 6.0) = 2.5;
uniform float sea_level : hint_range(0.3, 0.6) = 0.48;
uniform float atmo_strength : hint_range(0.0, 3.0) = 1.8;
uniform float rotation_speed : hint_range(0.0, 0.05) = 0.01;

varying vec3 model_pos;
void vertex() { model_pos = VERTEX; }

// ── Noise ──
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
	vec3 p = normalize(model_pos) * continent_scale;
	float t = TIME * rotation_speed;
	mat3 rot = mat3(vec3(cos(t),0,sin(t)), vec3(0,1,0), vec3(-sin(t),0,cos(t)));
	p = rot * p;

	float n = fbm(p, 5);
	float detail = fbm(p * 3.1 + vec3(7.3), 3);

	vec3 col;
	if (n < sea_level) {
		float depth = smoothstep(0.0, sea_level, n);
		col = mix(ocean_deep, ocean_shallow, depth);
		ROUGHNESS = 0.05; METALLIC = 0.3;
	} else {
		float elev = (n - sea_level) / (1.0 - sea_level) + detail * 0.15;
		col = mix(land_low, land_high, smoothstep(0.2, 0.7, elev));
		col = mix(col, snow_cap, smoothstep(0.75, 0.95, elev));
		ROUGHNESS = 0.85; METALLIC = 0.0;
	}

	// Polar ice caps.
	float pole = abs(normalize(model_pos).y);
	col = mix(col, snow_cap, smoothstep(0.7, 0.85, pole));

	ALBEDO = col;

	// Rim atmosphere glow.
	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 3.5) * atmo_strength;
}
