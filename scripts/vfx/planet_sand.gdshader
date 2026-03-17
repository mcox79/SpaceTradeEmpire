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
	vec3 n = normalize(model_pos);
	float t = TIME * rotation_speed;
	mat3 rot = mat3(vec3(cos(t),0,sin(t)), vec3(0,1,0), vec3(-sin(t),0,cos(t)));
	vec3 p = rot * n * dune_scale;

	float macro = fbm(p * 0.5, 3);
	float dune_dir = sin(p.x * 2.0 + p.z * 1.4 + macro * 3.0);
	float micro = fbm(p * 4.0, 2);

	float ridge = smoothstep(0.5, 0.85, dune_dir);
	vec3 col = mix(sand_dark, sand_light, macro);
	col = mix(col, dune_ridge, ridge * 0.6);
	col = mix(col, rock_color, smoothstep(0.65, 0.8, micro));

	ALBEDO = col;
	ROUGHNESS = 0.9; METALLIC = 0.0;

	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 3.5) * 1.2;
}
