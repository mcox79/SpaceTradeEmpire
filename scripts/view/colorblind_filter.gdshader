// GATE.S9.ACCESSIBILITY.COLORBLIND.001: Colorblind simulation post-processing shader.
// mode: 0=None, 1=Deuteranopia (green-blind), 2=Protanopia (red-blind), 3=Tritanopia (blue-blind).
// Standard simulation matrices from Machado et al. (2009).
shader_type canvas_item;

uniform int mode : hint_range(0, 3) = 0;

void fragment() {
	vec4 c = texture(TEXTURE, UV);

	if (mode == 1) {
		// Deuteranopia (green-blind)
		float r = c.r * 0.625 + c.g * 0.375 + c.b * 0.0;
		float g = c.r * 0.7   + c.g * 0.3   + c.b * 0.0;
		float b = c.r * 0.0   + c.g * 0.3   + c.b * 0.7;
		COLOR = vec4(r, g, b, c.a);
	} else if (mode == 2) {
		// Protanopia (red-blind)
		float r = c.r * 0.567 + c.g * 0.433 + c.b * 0.0;
		float g = c.r * 0.558 + c.g * 0.442 + c.b * 0.0;
		float b = c.r * 0.0   + c.g * 0.242 + c.b * 0.758;
		COLOR = vec4(r, g, b, c.a);
	} else if (mode == 3) {
		// Tritanopia (blue-blind)
		float r = c.r * 0.95  + c.g * 0.05  + c.b * 0.0;
		float g = c.r * 0.0   + c.g * 0.433 + c.b * 0.567;
		float b = c.r * 0.0   + c.g * 0.475 + c.b * 0.525;
		COLOR = vec4(r, g, b, c.a);
	} else {
		// mode == 0: pass-through
		COLOR = c;
	}
}
