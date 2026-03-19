shader_type spatial;
render_mode cull_front, blend_add, depth_draw_never, unshaded;

// Star corona halo — identical approach to planet_atmosphere.gdshader (KNOWN to work).
// Fresnel glow on a sphere slightly larger than the star body.

uniform vec4 corona_color : source_color = vec4(1.0, 0.8, 0.4, 1.0);
uniform float corona_power : hint_range(1.0, 8.0) = 3.5;
uniform float corona_strength : hint_range(0.0, 4.0) = 1.8;

void fragment() {
	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	float fresnel = pow(rim, corona_power);
	ALBEDO = corona_color.rgb * fresnel * corona_strength;
	ALPHA = fresnel * corona_color.a;
}
