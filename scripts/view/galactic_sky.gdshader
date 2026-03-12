shader_type spatial;
render_mode blend_add, depth_draw_never, cull_front, shadows_disabled, unshaded;

// Seed — offsets all noise so each universe looks different.
uniform float universe_seed : hint_range(0.0, 1000.0) = 0.0;

// Milky Way band
uniform vec3 milky_way_color_core : source_color = vec3(0.7, 0.75, 0.85);
uniform vec3 milky_way_color_edge : source_color = vec3(0.15, 0.25, 0.55);
uniform float milky_way_brightness : hint_range(0.0, 0.5) = 0.15;
uniform float milky_way_width : hint_range(0.01, 0.5) = 0.14;
uniform float milky_way_tilt_deg : hint_range(0.0, 90.0) = 60.0;

// Dust lanes
uniform float dust_strength : hint_range(0.0, 1.0) = 0.6;

// Central bulge — gentle warm glow at galactic center, blends into MW band.
uniform float bulge_brightness : hint_range(0.0, 0.3) = 0.05;
uniform float bulge_width : hint_range(0.01, 0.5) = 0.25;
uniform float bulge_height : hint_range(0.01, 0.3) = 0.10;

// Nearby galaxies
uniform float galaxy_brightness : hint_range(0.0, 0.5) = 0.10;
uniform float galaxy_angular_size : hint_range(0.02, 0.4) = 0.06;


// --- Simplex noise (Ashima Arts) ---

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

// Ridged noise — absolute value creates sharp ridges (good for dust filaments).
float ridged(vec3 p, float scale, int octaves) {
	float value = 0.0;
	float amplitude = 0.5;
	vec3 pos = p * scale;
	for (int i = 0; i < octaves; i++) {
		float n = 1.0 - abs(snoise(pos));
		value += amplitude * n * n;
		pos *= 2.1;
		amplitude *= 0.45;
	}
	return value;
}


// --- Seed helpers ---

vec3 seeded(vec3 dir) {
	return dir + vec3(universe_seed * 7.31, universe_seed * 13.17, universe_seed * 4.79);
}

float hash_seed(float s) {
	return fract(sin(s * 127.1 + 311.7) * 43758.5453);
}

vec3 seed_direction(float idx) {
	float s = universe_seed * 3.7 + idx * 91.3;
	float theta = acos(2.0 * hash_seed(s) - 1.0);
	float phi = 6.2831853 * hash_seed(s + 47.1);
	return vec3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
}


// --- Galaxy rendering ---
// Galaxy types: 0 = spiral, 1 = barred spiral, 2 = elliptical, 3 = irregular.

vec3 render_galaxy(vec3 dir, vec3 center_dir, float ang_size, float tilt,
                   float twist, int arms, float rot, float seed_offset, int galaxy_type,
                   float arm_sharpness, float core_size_mult) {
	float angular_dist = acos(clamp(dot(dir, center_dir), -1.0, 1.0));
	if (angular_dist > ang_size * 3.0) return vec3(0.0);

	// Local tangent frame.
	vec3 up_hint = abs(center_dir.y) < 0.9 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
	vec3 right = normalize(cross(up_hint, center_dir));
	vec3 up = normalize(cross(center_dir, right));

	vec3 delta = dir - center_dir;
	float u = dot(delta, right);
	float v = dot(delta, up);

	// Disc tilt with seed-derived axis rotation.
	float tilt_angle = seed_offset * 3.7;
	float ct = cos(tilt_angle); float st = sin(tilt_angle);
	float u2 = u * ct - v * st;
	float v2 = u * st + v * ct;
	v2 /= max(0.15, 1.0 - tilt);

	float r = length(vec2(u2, v2));
	float theta = atan(v2, u2) + rot;
	float rn = r / ang_size;
	if (rn > 3.0) return vec3(0.0);

	// Core — tight central bulge.
	float core = exp(-rn * rn * (30.0 / core_size_mult));

	if (galaxy_type == 2) {
		// ELLIPTICAL: compact, smooth, yellow-red glow. No arms.
		float disc = exp(-rn * rn * 4.0);
		vec3 noise_pos = vec3(u2, v2, seed_offset) * 20.0;
		float en = snoise(noise_pos) * 0.1 + 0.9;
		return vec3(0.0, core * 1.5 * en, disc * 0.3 * en);
	}

	if (galaxy_type == 3) {
		// IRREGULAR: amorphous, clumpy, no spiral structure (Magellanic Cloud-like).
		float disc = exp(-rn * rn * 1.5);
		vec3 noise_pos = vec3(u2, v2, seed_offset) * 12.0;
		float clumps = max(0.0, snoise(noise_pos)) * 0.7 + 0.3;
		float fine = snoise(noise_pos * 3.5) * 0.3 + 0.7;
		// Asymmetric shape — real irregulars are lopsided.
		float asym = snoise(vec3(u2 * 4.0 + seed_offset * 50.0, v2 * 4.0, seed_offset * 3.0)) * 0.4;
		float shape = disc * (0.6 + asym) * clumps * fine;
		// HII knots — bright pink-blue star-forming clumps.
		float hii = max(0.0, snoise(noise_pos * 5.0)) * 0.5 * disc;
		return vec3(shape + hii * 0.3, core * 0.4, disc * 0.15);
	}

	// Disc envelope — steep falloff so arms stand out against dark background.
	float disc = exp(-rn * rn * 3.0);

	// Spiral arms with per-galaxy variation.
	float spiral_angle = theta;
	if (galaxy_type == 1) {
		// BARRED SPIRAL: straight bar through center, spirals start further out.
		float bar_strength = smoothstep(0.4, 0.15, rn);
		float bar = exp(-pow(abs(sin(theta)), 3.0) * 8.0) * bar_strength;
		// Spiral starts at bar ends.
		spiral_angle = theta - twist * log(max(rn - 0.15, 0.01));
		float arm_pattern = 0.0;
		for (int a = 0; a < arms; a++) {
			float arm_offset = float(a) * 6.2831853 / float(arms);
			float arm_dist = abs(sin((spiral_angle + arm_offset) * 0.5));
			arm_pattern += exp(-arm_dist * arm_dist * arm_sharpness);
		}
		arm_pattern = clamp(arm_pattern, 0.0, 1.0);
		float arm_radial = smoothstep(0.15, 0.5, rn) * smoothstep(2.5, 0.6, rn);

		// Clumpy noise unique to this galaxy.
		vec3 np = vec3(u2 * 25.0 + seed_offset * 100.0, v2 * 25.0, seed_offset * 7.0);
		float arm_noise = snoise(np) * 0.35 + 0.65;
		float fine_noise = snoise(np * 2.5) * 0.2 + 0.8;

		float arm_val = (arm_pattern * arm_radial + bar) * arm_noise * fine_noise * disc;
		return vec3(arm_val, core, disc);
	}

	// NORMAL SPIRAL
	spiral_angle = theta - twist * log(max(rn, 0.01));
	float arm_pattern = 0.0;
	for (int a = 0; a < arms; a++) {
		float arm_offset = float(a) * 6.2831853 / float(arms);
		float arm_dist = abs(sin((spiral_angle + arm_offset) * 0.5));
		arm_pattern += exp(-arm_dist * arm_dist * arm_sharpness);
	}
	arm_pattern = clamp(arm_pattern, 0.0, 1.0);

	float arm_radial = smoothstep(0.0, 0.25, rn) * smoothstep(2.2, 0.5, rn);

	// Multi-scale noise unique to this galaxy.
	vec3 np = vec3(u2 * 20.0 + seed_offset * 100.0, v2 * 20.0, seed_offset * 7.0);
	float arm_noise = snoise(np) * 0.35 + 0.65;
	float fine_noise = snoise(np * 3.0) * 0.2 + 0.8;
	// Bright HII region clumps along arms.
	float clumps = max(0.0, snoise(np * 5.0)) * 0.4;

	float arm_val = arm_pattern * arm_radial * arm_noise * fine_noise * disc + clumps * arm_pattern * disc * 0.3;
	return vec3(arm_val, core, disc);
}


// --- Galactic plane ---

vec3 galactic_coords(vec3 dir) {
	float tilt_variation = (hash_seed(universe_seed * 5.3) - 0.5) * 20.0;
	float rad = radians(milky_way_tilt_deg + tilt_variation);
	float az = hash_seed(universe_seed * 2.1) * 6.2831853;
	float c = cos(rad); float s = sin(rad);
	float ca = cos(az); float sa = sin(az);
	vec3 d = vec3(dir.x * ca + dir.z * sa, dir.y, -dir.x * sa + dir.z * ca);
	return vec3(s * d.y + c * d.x, c * d.y - s * d.x, d.z);
}


void vertex() {
	POSITION = PROJECTION_MATRIX * vec4((VIEW_MATRIX * vec4(VERTEX + CAMERA_POSITION_WORLD, 1.0)).xyz, 1.0);
	// Push to far plane in both sRGB and linear pipelines.
	// Godot 4 reverse-Z: z=0 is far plane.
	POSITION.z = 0.0;
}


void fragment() {
	// Derive ray direction from screen UV — avoids sphere mesh equator seam artifact.
	vec2 ndc = vec2(SCREEN_UV.x * 2.0 - 1.0, SCREEN_UV.y * 2.0 - 1.0);
	vec4 unproj = INV_PROJECTION_MATRIX * vec4(ndc, 0.5, 1.0);
	vec3 dir = normalize((INV_VIEW_MATRIX * vec4(normalize(unproj.xyz / unproj.w), 0.0)).xyz);
	vec3 sdir = seeded(dir);

	vec3 gdir = galactic_coords(dir);
	float gal_lat = gdir.y;
	float plane_dist = abs(gal_lat);
	float gal_lon = atan(gdir.z, gdir.x);

	// Galactic center longitude (seed-derived).
	float center_lon = hash_seed(universe_seed * 9.3) * 6.2831853;

	// Longitude offset from center, wrapped to [-PI, PI].
	float dlon = gal_lon - center_lon;
	dlon = dlon - round(dlon / 6.2831853) * 6.2831853;

	// --- Milky Way band ---
	// Variable width: wider near galactic center (real MW bulge region), narrower at anti-center.
	float width_mult = 0.7 + 0.6 * exp(-dlon * dlon * 0.5);
	float base_width = milky_way_width * width_mult;

	// Irregular envelope: gaussian base modulated by large-scale noise for ragged edges.
	float edge_noise = fbm(sdir, 2.0, 3) * 0.4;
	float effective_width = base_width + edge_noise * base_width;
	float band = exp(-plane_dist * plane_dist / (2.0 * effective_width * effective_width));

	// Large-scale bright/dark patches along the band (like Sagittarius, Cygnus regions).
	float region_noise = fbm(sdir, 3.5, 4) * 0.5 + 0.5;
	// Longitudinal brightness: generally brighter toward center.
	float lon_variation = snoise(vec3(gal_lon * 0.8, universe_seed * 3.0, 0.0)) * 0.3 + 0.7;
	float center_boost = 0.3 * exp(-dlon * dlon * 0.8) + 0.7;
	lon_variation *= center_boost;

	// Fine structure: wispy filaments using ridged noise.
	float filaments = ridged(sdir, 12.0, 4);
	// Medium clouds.
	vec3 warp = sdir + vec3(fbm(sdir, 2.5, 3)) * 0.2;
	float clouds = fbm(warp, 7.0, 5) * 0.5 + 0.5;

	// Dust lanes — multiple scales for variety.
	// Large dark rift (like the Great Rift).
	float rift = fbm(sdir + vec3(universe_seed * 1.1, 0.0, 0.0), 4.0, 4);
	float rift_mask = smoothstep(-0.15, 0.2, rift) * 0.7;
	// Fine dust filaments.
	float fine_dust = ridged(sdir + vec3(0.0, universe_seed * 2.3, 0.0), 10.0, 4);
	float fine_dust_mask = smoothstep(0.3, 0.7, fine_dust) * 0.4;
	// Combined dust — strongest near center plane.
	float plane_factor = smoothstep(base_width * 1.5, 0.0, plane_dist);
	float dust = (rift_mask + fine_dust_mask) * dust_strength * plane_factor;
	dust = clamp(dust, 0.0, 0.95);

	// Combine MW layers.
	float mw_raw = region_noise * lon_variation * (0.4 + 0.6 * clouds) * (0.5 + 0.5 * filaments);
	float mw = band * milky_way_brightness * mw_raw * (1.0 - dust);

	// Star clouds — distinct bright patches along the band (Scutum, Norma, Carina-like).
	for (int ci = 0; ci < 4; ci++) {
		float cs = universe_seed * 5.7 + float(ci) * 31.3;
		float cl = center_lon + (hash_seed(cs) - 0.5) * 2.5;
		float cb = (hash_seed(cs + 1.0) - 0.5) * base_width * 0.4;
		float cr = 0.025 + hash_seed(cs + 2.0) * 0.035;
		float cdl = gal_lon - cl;
		cdl = cdl - round(cdl / 6.2831853) * 6.2831853;
		float cdb = gal_lat - cb;
		float cloud_d2 = cdl * cdl + cdb * cdb;
		float cv = exp(-cloud_d2 / (2.0 * cr * cr));
		cv *= fbm(sdir + vec3(float(ci) * 10.0), 12.0, 3) * 0.4 + 0.6;
		mw += cv * 0.02 * band;
	}

	// Color: cool blue-white center, deeper blue edges.
	float color_t = smoothstep(0.0, base_width * 1.2, plane_dist);
	vec3 mw_color = mix(milky_way_color_core, milky_way_color_edge, color_t);
	// Dust darkens rather than reddens — avoids pink with additive blending.
	mw_color *= (1.0 - dust * 0.3);

	// --- Central bulge — elongated elliptical glow, not a point ---
	// Real MW bulge: ~10° wide along plane, ~5° tall, warm yellow.
	float bulge = bulge_brightness * exp(
		-dlon * dlon / (2.0 * bulge_width * bulge_width)
		- gal_lat * gal_lat / (2.0 * bulge_height * bulge_height)
	);
	float bn = fbm(sdir, 8.0, 3) * 0.15 + 0.85;
	vec3 bulge_color = vec3(0.7, 0.75, 0.9) * bn * bulge;

	// --- Nearby galaxies (4-6, realistic types and Zone of Avoidance) ---
	int num_galaxies = 4 + int(hash_seed(universe_seed * 11.0) * 2.99);
	vec3 galaxy_total = vec3(0.0);

	for (int gi = 0; gi < 6; gi++) {
		if (gi >= num_galaxies) break;
		float gi_f = float(gi);
		vec3 gal_dir = seed_direction(gi_f + 1.0);

		// Zone of Avoidance — galaxies near the galactic plane are dimmed by MW dust.
		vec3 gal_in_galcoords = galactic_coords(gal_dir);
		float gal_elev = abs(gal_in_galcoords.y);
		float zoa = smoothstep(0.0, 0.15, gal_elev);  // dims within ~9 deg of plane
		if (zoa < 0.05) continue;

		float g_seed = universe_seed * 3.1 + gi_f * 77.7;

		// Galaxy type: 45% spiral, 25% barred spiral, 10% elliptical, 20% irregular.
		float type_roll = hash_seed(g_seed + 7.0);
		int g_type = 0;
		if (type_roll > 0.80) {
			g_type = 3;  // irregular (Magellanic Cloud-like)
		} else if (type_roll > 0.70) {
			g_type = 2;  // elliptical
		} else if (type_roll > 0.45) {
			g_type = 1;  // barred spiral
		}

		// Distance tier — based on real galaxy apparent sizes.
		// Andromeda (nearest large spiral): ~3deg = 0.05 rad.
		// Most visible galaxies: 0.5-1.5deg = 0.01-0.03 rad.
		// Distant faint smudges: <0.5deg = <0.01 rad.
		float dist_roll = hash_seed(g_seed + 10.0);
		float size_mult;
		float bright_mult;
		if (dist_roll > 0.90) {
			// ~10% chance: Andromeda-scale neighbor.
			size_mult = 0.7 + hash_seed(g_seed + 11.0) * 0.6;
			bright_mult = 1.0;
		} else if (dist_roll > 0.50) {
			// ~40% chance: medium — clearly visible shape.
			size_mult = 0.25 + hash_seed(g_seed + 11.0) * 0.35;
			bright_mult = 0.85;
		} else {
			// ~50% chance: distant — small faint smudge.
			size_mult = 0.08 + hash_seed(g_seed + 11.0) * 0.17;
			bright_mult = 0.6;
		}

		// Irregulars are nearby — larger apparent size, lower surface brightness.
		if (g_type == 3) {
			size_mult *= 1.5;
			bright_mult *= 0.7;
		}

		float g_size = galaxy_angular_size * size_mult;

		// Tilt: bias toward edge-on.
		float g_tilt_raw = hash_seed(g_seed + 1.0);
		float g_tilt = (1.0 - (1.0 - g_tilt_raw) * (1.0 - g_tilt_raw)) * 0.85;

		// Per-galaxy twist, arms, rotation — all varied.
		float g_twist = 1.0 + hash_seed(g_seed + 2.0) * 3.5;
		float arm_roll = hash_seed(g_seed + 3.0);
		int g_arms = 2 + int(arm_roll * 4.0);
		g_arms = clamp(g_arms, 2, 5);
		float g_rot = hash_seed(g_seed + 4.0) * 6.2831853;

		// Per-galaxy arm sharpness and core size.
		float g_sharpness = 6.0 + hash_seed(g_seed + 8.0) * 18.0;
		float g_core_size = 0.5 + hash_seed(g_seed + 9.0) * 1.5;

		// Type-correlated colors with slight per-galaxy tint variation.
		float tint_v = hash_seed(g_seed + 6.0) * 0.15;
		vec3 arm_color;
		vec3 core_color;
		if (g_type == 0) {
			// Spiral: blue-white arms, cool white core.
			arm_color = vec3(0.3 + tint_v, 0.5, 1.0);
			core_color = vec3(0.7, 0.75, 0.85);
		} else if (g_type == 1) {
			// Barred spiral: blue-white arms, pale core.
			arm_color = vec3(0.35 + tint_v, 0.5, 0.9);
			core_color = vec3(0.7, 0.75, 0.8);
		} else if (g_type == 2) {
			// Elliptical: pale amber, old stellar population.
			arm_color = vec3(0.6, 0.6 + tint_v, 0.5);
			core_color = vec3(0.7, 0.7, 0.65);
		} else {
			// Irregular: bright blue with cyan star-forming regions.
			arm_color = vec3(0.25, 0.5 + tint_v, 1.0);
			core_color = vec3(0.5, 0.6, 0.9);
		}

		vec3 gal = render_galaxy(dir, gal_dir, g_size, g_tilt, g_twist, g_arms,
		                         g_rot, gi_f * 17.3, g_type, g_sharpness, g_core_size);

		// Arms dominate, core is accent, disc glow is very faint.
		vec3 gal_color = arm_color * gal.x * 1.0
			+ core_color * gal.y * 0.8
			+ arm_color * 0.3 * gal.z * 0.05;
		galaxy_total += gal_color * galaxy_brightness * bright_mult * zoa;
	}

	// --- Faint background haze ---
	float haze = fbm(sdir, 2.0, 3) * 0.5 + 0.5;
	haze *= haze;
	vec3 haze_color = mix(vec3(0.04, 0.06, 0.14), vec3(0.04, 0.1, 0.16), haze) * 0.002;

	// --- Combine ---
	vec3 color = mw_color * mw
		+ bulge_color
		+ galaxy_total
		+ haze_color;

	if (color.r < 0.001 && color.g < 0.001 && color.b < 0.001)
		discard;

	ALBEDO = color;
}
