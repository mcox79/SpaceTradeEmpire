shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_schlick_ggx;

// Procedural barren rocky world: cratered surface, no atmosphere.

uniform vec3 rock_a      : source_color = vec3(0.32, 0.28, 0.25);
uniform vec3 rock_b      : source_color = vec3(0.48, 0.42, 0.35);
uniform vec3 crater_floor: source_color = vec3(0.18, 0.15, 0.12);
uniform float rock_scale : hint_range(2.0, 8.0) = 4.0;

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

// Worley-like craters.
float craters(vec3 p) {
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
