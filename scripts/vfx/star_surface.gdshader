shader_type spatial;
render_mode cull_back, diffuse_lambert, specular_disabled;

// Star photosphere — SDO/H-alpha aesthetic.
//
// Architecture:
// - Inverted Worley at 90% weight = bright polygonal convection cells + thin dark lanes
// - Light domain warping (1.5) keeps cells organic but still readable as structures
// - Separate low-freq Worley for sunspot shapes (irregular, not polka dots)
// - Faculae: bright patches at sunspot boundaries (magnetic field brightening)
// - Strong limb darkening + wide Fresnel = bloom corona

uniform vec3 color_center : source_color = vec3(1.0, 0.82, 0.40);
uniform vec3 color_mid    : source_color = vec3(1.0, 0.50, 0.12);
uniform vec3 color_limb   : source_color = vec3(0.85, 0.22, 0.04);
uniform float granule_scale : hint_range(10.0, 35.0) = 20.0;
uniform float granule_speed : hint_range(0.005, 0.08) = 0.02;
uniform float limb_darken : hint_range(0.2, 0.7) = 0.45;
uniform float emission_peak : hint_range(1.0, 8.0) = 3.0;
uniform float fresnel_glow : hint_range(0.0, 5.0) = 2.5;

varying vec3 model_pos;
void vertex() { model_pos = VERTEX; }

// ── Noise primitives ──
vec3 hash33(vec3 p) {
	p = vec3(dot(p, vec3(127.1, 311.7, 74.7)),
	         dot(p, vec3(269.5, 183.3, 246.1)),
	         dot(p, vec3(113.5, 271.9, 124.6)));
	return fract(sin(p) * 43758.5453123) * 2.0 - 1.0;
}
float gnoise3(vec3 p) {
	vec3 i = floor(p); vec3 f = fract(p);
	vec3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
	return mix(mix(mix(dot(hash33(i+vec3(0,0,0)), f-vec3(0,0,0)),
	                   dot(hash33(i+vec3(1,0,0)), f-vec3(1,0,0)), u.x),
	               mix(dot(hash33(i+vec3(0,1,0)), f-vec3(0,1,0)),
	                   dot(hash33(i+vec3(1,1,0)), f-vec3(1,1,0)), u.x), u.y),
	           mix(mix(dot(hash33(i+vec3(0,0,1)), f-vec3(0,0,1)),
	                   dot(hash33(i+vec3(1,0,1)), f-vec3(1,0,1)), u.x),
	               mix(dot(hash33(i+vec3(0,1,1)), f-vec3(0,1,1)),
	                   dot(hash33(i+vec3(1,1,1)), f-vec3(1,1,1)), u.x), u.y),
	           u.z) * 0.5 + 0.5;
}
float fbm(vec3 p, int oct) {
	float v = 0.0, a = 0.5, freq = 1.0;
	for (int i = 0; i < oct; i++) { v += a * gnoise3(p * freq); freq *= 2.0; a *= 0.5; }
	return v;
}

// ── Worley noise ──
float hash1(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453123);
}
float worley(vec3 p) {
	vec3 i = floor(p);
	float min_d = 10.0;
	for (int x = -1; x <= 1; x++)
	for (int y = -1; y <= 1; y++)
	for (int z = -1; z <= 1; z++) {
		vec3 nb = i + vec3(float(x), float(y), float(z));
		vec3 pt = nb + vec3(hash1(nb), hash1(nb + 31.7), hash1(nb + 17.3));
		min_d = min(min_d, length(p - pt));
	}
	return min_d;
}

void fragment() {
	vec3 n_pos = normalize(model_pos);
	float t = TIME * granule_speed;

	// ── Light domain warping: organic distortion without destroying cell structure ──
	vec3 p = n_pos * granule_scale;
	vec3 warp = vec3(
		gnoise3(p * 0.25 + vec3(t, 0.0, 0.0)),
		gnoise3(p * 0.25 + vec3(0.0, t * 0.7, 0.0)),
		gnoise3(p * 0.25 + vec3(0.0, 0.0, t * 0.5))
	);
	vec3 warped = p + warp * 1.5 + vec3(t * 0.3, -t * 0.2, t * 0.15);

	// ── GRANULATION: inverted Worley = bright cell centers, thin dark lanes ──
	float w = worley(warped);
	// pow() sharpens: cell centers are bright, boundaries drop off steeply.
	float cells = pow(1.0 - clamp(w, 0.0, 1.0), 2.0);

	// ── Subtle FBM detail within cells (10% weight — texture, not structure) ──
	float fine_detail = fbm(warped * 1.8, 3) * 0.1;

	// ── Combined granulation: Worley dominates ──
	float granulation = cells * 0.9 + fine_detail;

	// ── SUNSPOTS: low-freq Worley for large irregular dark patches ──
	// Using Worley at 1/5 scale creates ~3-5 potential spot locations.
	// Only the deepest cells become spots (threshold selects ~20% of surface).
	float spot_w = worley(n_pos * 4.0 + vec3(t * 0.002));
	// Spots where Worley distance is small (cell centers = spot centers).
	float spot_mask = 1.0 - smoothstep(0.15, 0.45, spot_w);
	// Add FBM to make spot edges irregular (not perfect circles).
	float spot_irregular = fbm(n_pos * 12.0 + vec3(t * 0.004), 3);
	spot_mask *= smoothstep(0.3, 0.55, spot_irregular);
	// Penumbra: gradual brightening from umbra (dark core) to normal surface.
	float spot_darken = mix(0.06, 1.0, smoothstep(0.0, 0.6, spot_w + spot_irregular * 0.2));
	spot_darken = max(spot_darken, 1.0 - spot_mask); // Only darken where mask is active.

	// ── FACULAE: bright regions near sunspot boundaries (magnetic brightening) ──
	float facula_ring = smoothstep(0.35, 0.55, spot_w) * (1.0 - smoothstep(0.55, 0.80, spot_w));
	float facula_noise = fbm(n_pos * 15.0 - vec3(t * 0.006), 3);
	float faculae = facula_ring * smoothstep(0.45, 0.65, facula_noise) * 1.8;

	// ── Mu = viewing angle ──
	float mu = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);

	// ── Color: bright orange center → deep orange mid → deep red limb ──
	vec3 disk_color;
	if (mu > 0.5) {
		float blend = (mu - 0.5) / 0.5;
		disk_color = mix(color_mid, color_center, blend);
	} else if (mu > 0.1) {
		float blend = (mu - 0.1) / 0.4;
		disk_color = mix(color_limb, color_mid, blend);
	} else {
		disk_color = color_limb;
	}

	// ── Apply granulation: bright cells, dark lanes ──
	float gran_brightness = 0.35 + granulation * 0.75;
	disk_color *= gran_brightness;

	// ── Sunspot darkening ──
	disk_color *= spot_darken;

	// ── Faculae brightening (white-hot patches near spot edges) ──
	disk_color += color_center * faculae * 0.5;

	// ── Limb darkening: strong — center bright, edges dim ──
	float limb = 1.0 - limb_darken * pow(1.0 - mu, 0.8);
	disk_color *= limb;

	// ── Fresnel corona: wide bloom halo ──
	float fresnel_narrow = pow(1.0 - mu, 3.5);
	float fresnel_wide = pow(1.0 - mu, 1.2);
	vec3 rim = color_mid * (fresnel_narrow * 1.5 + fresnel_wide * 0.5) * fresnel_glow;

	// ── Emission ──
	float emission_scale = mix(emission_peak * 0.5, emission_peak, mu);

	ALBEDO = vec3(0.0);
	ROUGHNESS = 1.0;
	EMISSION = (disk_color + rim) * emission_scale;
}
