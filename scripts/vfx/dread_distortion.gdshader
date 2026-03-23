shader_type canvas_item;
// GATE.T45.DEEP_DREAD.DISTORTION_SHADER.001: Phase distortion post-processing.
// Phase 0: no effect. Phase 1: subtle chromatic aberration at edges.
// Phase 2: heavier aberration + shimmer. Phase 3: HUD geometry distortion + jitter.
// Phase 4: distortion resolves to clarity (slight luminance boost, zero aberration).

uniform float phase : hint_range(0.0, 4.0, 1.0) = 0.0;
uniform float time_offset : hint_range(0.0, 1000.0) = 0.0;

void fragment() {
	vec2 uv = UV;
	vec2 center = vec2(0.5, 0.5);
	float dist_from_center = distance(uv, center);

	vec4 col = texture(TEXTURE, uv);

	if (phase < 0.5) {
		// Phase 0: no distortion
		COLOR = col;
	} else if (phase < 1.5) {
		// Phase 1: subtle chromatic aberration at edges (2px equivalent)
		float aberration = dist_from_center * 0.002;
		float r = texture(TEXTURE, uv + vec2(aberration, 0.0)).r;
		float b = texture(TEXTURE, uv - vec2(aberration, 0.0)).b;
		COLOR = vec4(r, col.g, b, col.a);
	} else if (phase < 2.5) {
		// Phase 2: heavier aberration + star shimmer
		float aberration = dist_from_center * 0.005;
		float shimmer = sin(uv.y * 200.0 + time_offset * 3.0) * 0.02;
		float r = texture(TEXTURE, uv + vec2(aberration + shimmer, 0.0)).r;
		float b = texture(TEXTURE, uv - vec2(aberration, shimmer)).b;
		COLOR = vec4(r, col.g, b, col.a);
	} else if (phase < 3.5) {
		// Phase 3: HUD geometry distortion + numerical jitter overlay
		float warp = sin(uv.y * 50.0 + time_offset * 2.0) * 0.003;
		float warp2 = cos(uv.x * 30.0 + time_offset * 1.5) * 0.002;
		vec2 distorted_uv = uv + vec2(warp, warp2);
		float aberration = dist_from_center * 0.008;
		float r = texture(TEXTURE, distorted_uv + vec2(aberration, 0.0)).r;
		float g = texture(TEXTURE, distorted_uv).g;
		float b = texture(TEXTURE, distorted_uv - vec2(aberration, 0.0)).b;
		// Ghost route flashes: occasional bright line
		float flash = step(0.998, fract(sin(uv.y * 1000.0 + time_offset) * 43758.5453));
		COLOR = vec4(r + flash * 0.1, g, b, col.a);
	} else {
		// Phase 4: Void — distortion resolves to clarity
		// Zero aberration, slight luminance boost (eerie clarity)
		COLOR = vec4(col.rgb * 1.08, col.a);
	}
}
