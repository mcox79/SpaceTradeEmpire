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
