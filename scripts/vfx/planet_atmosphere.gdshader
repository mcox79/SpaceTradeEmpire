shader_type spatial;
render_mode cull_front, blend_add, depth_draw_never, unshaded;

// Fresnel atmosphere glow — applied to a sphere scaled ~1.08x larger than the planet body.
// Cheap: single pow() per fragment on an 8-segment sphere.

uniform vec4 atmo_color : source_color = vec4(0.3, 0.6, 1.0, 1.0);
uniform float atmo_power : hint_range(1.0, 8.0) = 4.0;
uniform float atmo_strength : hint_range(0.0, 4.0) = 1.2;

void fragment() {
	float rim = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	float fresnel = pow(rim, atmo_power);
	ALBEDO = atmo_color.rgb * fresnel * atmo_strength;
	ALPHA  = fresnel * atmo_color.a;
}
