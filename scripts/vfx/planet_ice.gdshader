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

// Worley-like crack pattern.
float cracks(vec3 p) {
	vec3 i = floor(p);
	float min_d = 10.0;
	for (int x = -1; x <= 1; x++)
	for (int y = -1; y <= 1; y++)
	for (int z = -1; z <= 1; z++) {
		vec3 nb = i + vec3(float(x), float(y), float(z));
		vec3 pt = nb + vec3(hash3(nb), hash3(nb + 31.7), hash3(nb + 17.3));
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
