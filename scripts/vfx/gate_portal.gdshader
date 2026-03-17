shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix;

// Wavy stargate-like event horizon inside lane gate torus.
uniform vec4 color_core : source_color = vec4(0.3, 0.55, 1.0, 1.0);
uniform vec4 color_rim : source_color = vec4(0.15, 0.3, 0.8, 1.0);
uniform vec4 color_highlight : source_color = vec4(0.7, 0.85, 1.0, 1.0);
uniform float ripple_speed : hint_range(0.5, 4.0) = 1.5;
uniform float ripple_count : hint_range(2.0, 12.0) = 6.0;
uniform float swirl_speed : hint_range(0.1, 2.0) = 0.4;
uniform float distortion_strength : hint_range(0.0, 0.15) = 0.06;
uniform float edge_fade_start : hint_range(0.3, 0.9) = 0.7;
uniform float emission_energy : hint_range(1.0, 8.0) = 3.0;
uniform float alpha_base : hint_range(0.1, 1.0) = 0.65;

// Simple hash for pseudo-noise.
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Value noise.
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f); // smoothstep
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Fractional Brownian motion — layered noise for organic feel.
float fbm(vec2 p) {
    float v = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 4; i++) {
        v += amp * noise(p);
        p *= 2.1;
        amp *= 0.5;
    }
    return v;
}

void fragment() {
    // Center UV to [-1, 1].
    vec2 uv = UV * 2.0 - 1.0;
    float dist = length(uv);

    // Circular disc mask — fade to transparent outside radius 1.
    if (dist > 1.0) {
        discard;
    }

    // Polar coordinates.
    float angle = atan(uv.y, uv.x);

    // Swirling distortion — rotate UV over time.
    float swirl = swirl_speed * TIME;
    float angle_distorted = angle + swirl + dist * 1.5;
    vec2 polar_uv = vec2(dist, angle_distorted);

    // Ripple pattern — concentric waves emanating outward.
    float ripple = sin(dist * ripple_count * TAU - TIME * ripple_speed * TAU) * 0.5 + 0.5;

    // Second ripple layer offset for complexity.
    float ripple2 = sin(dist * (ripple_count * 0.7) * TAU + TIME * ripple_speed * 0.6 * TAU + 1.5) * 0.5 + 0.5;

    // FBM noise for organic membrane texture.
    vec2 noise_uv = polar_uv * vec2(1.5, 0.8) + vec2(-TIME * 0.3, TIME * 0.15);
    float membrane = fbm(noise_uv * 3.0);

    // Combine ripples with noise.
    float pattern = mix(ripple, ripple2, 0.4);
    pattern = mix(pattern, membrane, 0.35);

    // Distort pattern with secondary noise for that wavy liquid look.
    vec2 distort_uv = uv + vec2(
        fbm(uv * 4.0 + TIME * 0.2) - 0.5,
        fbm(uv * 4.0 - TIME * 0.15 + 100.0) - 0.5
    ) * distortion_strength;
    float dist_distorted = length(distort_uv);
    float caustic = fbm(distort_uv * 5.0 + TIME * vec2(0.1, -0.2));
    pattern = mix(pattern, caustic, 0.25);

    // Color: blend core → rim based on distance, add highlights on ripple peaks.
    vec3 col = mix(color_core.rgb, color_rim.rgb, smoothstep(0.0, 0.8, dist));
    col = mix(col, color_highlight.rgb, smoothstep(0.65, 0.95, pattern) * 0.6);

    // Bright center glow.
    float center_glow = 1.0 - smoothstep(0.0, 0.5, dist);
    col += color_highlight.rgb * center_glow * 0.3;

    // Edge fade — transparent near rim for blending with torus.
    float edge_alpha = 1.0 - smoothstep(edge_fade_start, 1.0, dist);

    // Pulsing overall brightness.
    float pulse = sin(TIME * 1.2) * 0.1 + 0.9;

    ALBEDO = col * pulse;
    EMISSION = col * emission_energy * pulse;
    ALPHA = alpha_base * edge_alpha * (0.7 + 0.3 * pattern);
}
