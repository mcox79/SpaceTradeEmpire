shader_type canvas_item;

// GATE.S9.MENU_ATMOSPHERE.STARFIELD.001: 4-layer parallax starfield for main menu.
// Layers: (1) deep dim stars, (2) blue nebula wash, (3) purple nebula wisps, (4) mid-field bright stars.
// Gentle parallax drift via TIME uniform.

uniform float drift_speed : hint_range(0.0, 0.1) = 0.008;


// --- Hash / noise primitives ---

float hash21(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float hash31(vec3 p) {
	p = fract(p * vec3(0.1031, 0.1030, 0.0973));
	p += dot(p, p.yxz + 33.33);
	return fract((p.x + p.y) * p.z);
}

vec3 mod289(vec3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec4 mod289v4(vec4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
vec4 permute(vec4 x) { return mod289v4(((x * 34.0) + 10.0) * x); }
vec4 taylor_inv_sqrt(vec4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

float snoise(vec3 v) {
	const vec2 C = vec2(1.0 / 6.0, 1.0 / 3.0);
	const vec4 D = vec4(0.0, 0.5, 1.0, 2.0);
	vec3 i = floor(v + dot(v, C.yyy));
	vec3 x0 = v - i + dot(i, C.xxx);
	vec3 g = step(x0.yzx, x0.xyz);
	vec3 l = 1.0 - g;
	vec3 i1 = min(g.xyz, l.zxy);
	vec3 i2 = max(g.xyz, l.zxy);
	vec3 x1 = x0 - i1 + C.xxx;
	vec3 x2 = x0 - i2 + C.yyy;
	vec3 x3 = x0 - D.yyy;
	i = mod289(i);
	vec4 p = permute(permute(permute(
		i.z + vec4(0.0, i1.z, i2.z, 1.0))
		+ i.y + vec4(0.0, i1.y, i2.y, 1.0))
		+ i.x + vec4(0.0, i1.x, i2.x, 1.0));
	float n_ = 0.142857142857;
	vec3 ns = n_ * D.wyz - D.xzx;
	vec4 j = p - 49.0 * floor(p * ns.z * ns.z);
	vec4 x_ = floor(j * ns.z);
	vec4 y_ = floor(j - 7.0 * x_);
	vec4 x = x_ * ns.x + ns.yyyy;
	vec4 y = y_ * ns.x + ns.yyyy;
	vec4 h = 1.0 - abs(x) - abs(y);
	vec4 b0 = vec4(x.xy, y.xy);
	vec4 b1 = vec4(x.zw, y.zw);
	vec4 s0 = floor(b0) * 2.0 + 1.0;
	vec4 s1 = floor(b1) * 2.0 + 1.0;
	vec4 sh = -step(h, vec4(0.0));
	vec4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
	vec4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
	vec3 p0 = vec3(a0.xy, h.x);
	vec3 p1 = vec3(a0.zw, h.y);
	vec3 p2 = vec3(a1.xy, h.z);
	vec3 p3 = vec3(a1.zw, h.w);
	vec4 norm = taylor_inv_sqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2,p2), dot(p3,p3)));
	p0 *= norm.x; p1 *= norm.y; p2 *= norm.z; p3 *= norm.w;
	vec4 m = max(0.5 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
	m = m * m;
	return 105.0 * dot(m * m, vec4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3)));
}

float fbm(vec3 p, float scale, int octaves) {
	float value = 0.0;
	float amplitude = 0.5;
	vec3 pos = p * scale;
	for (int i = 0; i < octaves; i++) {
		value += amplitude * snoise(pos);
		pos *= 2.03;
		amplitude *= 0.5;
	}
	return value;
}


// --- Star field via cell hashing ---
// Returns brightness of a single star at UV, for a given grid density.

float star_layer(vec2 uv, float density, float brightness_floor, float brightness_ceil) {
	vec2 cell = floor(uv * density);
	vec2 local = fract(uv * density);

	// One star per cell, positioned by hash.
	vec2 star_pos = vec2(hash21(cell), hash21(cell + vec2(127.1, 311.7)));
	float dist = length(local - star_pos);

	// Star radius varies per cell.
	float radius = 0.015 + hash21(cell + vec2(73.3, 43.7)) * 0.025;

	// Sharp core + soft glow.
	float core = smoothstep(radius, radius * 0.2, dist);
	float glow = smoothstep(radius * 4.0, radius * 0.5, dist) * 0.3;
	float star_val = core + glow;

	// Per-star brightness variation.
	float brightness = mix(brightness_floor, brightness_ceil, hash21(cell + vec2(53.9, 97.1)));

	// Gentle twinkle.
	float twinkle = sin(TIME * (1.5 + hash21(cell + vec2(19.3, 71.7)) * 3.0) + hash21(cell) * 6.28) * 0.15 + 0.85;

	return star_val * brightness * twinkle;
}


void fragment() {
	vec2 uv = UV;
	float t = TIME * drift_speed;

	// === LAYER 1: Deep dim stars — dense field, faint, slow drift ===
	vec2 uv_deep = uv + vec2(t * 0.3, t * 0.15);
	float deep_stars = star_layer(uv_deep, 80.0, 0.15, 0.45);
	// Slight warm tint for variety.
	float deep_temp = hash21(floor(uv_deep * 80.0) + vec2(200.0, 300.0));
	vec3 deep_color = mix(vec3(0.7, 0.75, 1.0), vec3(1.0, 0.9, 0.7), deep_temp);
	vec3 layer1 = deep_color * deep_stars;

	// === LAYER 2: Blue nebula wash — large-scale noise, blue-cyan tint ===
	vec3 neb1_pos = vec3(uv * 2.0 + vec2(t * 0.5, t * 0.25), t * 0.1);
	float neb1_val = fbm(neb1_pos, 1.5, 5) * 0.5 + 0.5;
	neb1_val = pow(neb1_val, 2.5); // sharpen contrast — dark regions stay dark.
	vec3 neb1_color = mix(vec3(0.02, 0.06, 0.18), vec3(0.08, 0.15, 0.35), neb1_val);
	vec3 layer2 = neb1_color * neb1_val * 0.6;

	// === LAYER 3: Purple nebula wisps — offset noise, magenta-purple tint ===
	vec3 neb2_pos = vec3(uv * 3.0 + vec2(t * -0.4, t * 0.6), t * 0.08 + 50.0);
	float neb2_val = fbm(neb2_pos, 2.0, 4) * 0.5 + 0.5;
	neb2_val = pow(neb2_val, 3.0);
	vec3 neb2_color = mix(vec3(0.08, 0.02, 0.12), vec3(0.2, 0.06, 0.25), neb2_val);
	vec3 layer3 = neb2_color * neb2_val * 0.45;

	// === LAYER 4: Mid-field brighter stars — sparser, larger, faster drift ===
	vec2 uv_mid = uv + vec2(t * 0.8, t * 0.35);
	float mid_stars = star_layer(uv_mid, 30.0, 0.5, 1.0);
	// Color temperature per star: blue-white to warm yellow.
	float mid_temp = hash21(floor(uv_mid * 30.0) + vec2(400.0, 500.0));
	vec3 mid_color = mix(vec3(0.6, 0.7, 1.0), vec3(1.0, 0.92, 0.75), mid_temp);
	vec3 layer4 = mid_color * mid_stars;

	// === Compose ===
	// Dark space base.
	vec3 base = vec3(0.005, 0.005, 0.015);

	vec3 color = base + layer1 + layer2 + layer3 + layer4;

	// Soft vignette — darken edges for depth.
	vec2 vig_uv = uv * 2.0 - 1.0;
	float vignette = 1.0 - dot(vig_uv, vig_uv) * 0.25;
	color *= clamp(vignette, 0.0, 1.0);

	COLOR = vec4(color, 1.0);
}
