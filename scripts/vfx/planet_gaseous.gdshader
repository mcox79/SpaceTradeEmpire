shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

// Procedural gas giant: color banding, storm vortex, atmospheric haze.

uniform vec3 band_light : source_color = vec3(0.85, 0.65, 0.45);
uniform vec3 band_dark  : source_color = vec3(0.62, 0.42, 0.28);
uniform vec3 band_deep  : source_color = vec3(0.45, 0.3, 0.18);
uniform vec3 storm_color: source_color = vec3(0.78, 0.5, 0.35);
uniform vec3 atmo_color : source_color = vec3(0.9, 0.75, 0.55);
uniform float band_freq : hint_range(4.0, 16.0) = 8.0;
uniform float band_warp : hint_range(0.0, 0.8) = 0.3;
uniform float rotation_speed : hint_range(0.0, 0.1) = 0.04;
uniform float storm_latitude : hint_range(-0.6, 0.6) = -0.2;

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

	// Latitude bands warped by noise.
	float warp = fbm(n * 3.0 + vec3(t, 0.0, 0.0), 3) * band_warp;
	float band = sin((n.y + warp) * PI * band_freq);

	vec3 col = mix(band_dark, band_light, smoothstep(-0.3, 0.3, band));
	col = mix(col, band_deep, smoothstep(0.6, 0.9, abs(band)));

	// Great storm vortex.
	float storm_dist = length(vec2(
		atan(n.x, n.z) / PI - t * 0.5,
		n.y - storm_latitude
	));
	float storm_mask = smoothstep(0.18, 0.05, storm_dist);
	float swirl = fbm(vec3(storm_dist * 8.0, atan(n.y - storm_latitude, n.x) * 2.0, t * 0.5), 2);
	col = mix(col, storm_color * (0.8 + swirl * 0.4), storm_mask);

	ALBEDO = col;
	ROUGHNESS = 0.6; METALLIC = 0.0;

	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	EMISSION = atmo_color * pow(rim, 3.0) * 1.5;
}
