## EPIC.X.EXPERIENCE_PROOF.V0 — Component 3
## Viewport screenshot capture with JSON metadata sidecar.
## Gracefully skips in headless mode.

const OUTPUT_DIR := "res://reports/experience/screenshots/"


func capture_v0(tree: SceneTree, label: String, output_dir: String = OUTPUT_DIR) -> String:
	if tree == null:
		print("EXPV0|SCREENSHOT|SKIP|no_tree")
		return ""

	# Detect headless — no window server means no framebuffer to capture.
	if DisplayServer.get_name() == "headless":
		print("EXPV0|SCREENSHOT|SKIP|headless")
		return ""

	var viewport := tree.root.get_viewport()
	if viewport == null:
		print("EXPV0|SCREENSHOT|SKIP|no_viewport")
		return ""

	var img := viewport.get_texture().get_image()
	if img == null:
		print("EXPV0|SCREENSHOT|SKIP|no_image")
		return ""

	# Ensure output dir exists
	DirAccess.make_dir_recursive_absolute(output_dir)

	var timestamp := str(Time.get_unix_time_from_system()).replace(".", "_")
	var safe_label := label.replace(" ", "_").replace("/", "_")
	var filename := "%s_%s.png" % [safe_label, timestamp]
	var filepath := output_dir.path_join(filename)

	var err := img.save_png(filepath)
	if err != OK:
		print("EXPV0|SCREENSHOT|FAIL|save_error|%d" % err)
		return ""

	# Write JSON metadata sidecar
	var meta := {
		"label": label,
		"timestamp": timestamp,
		"resolution": "%dx%d" % [img.get_width(), img.get_height()],
		"filepath": filepath,
	}
	var meta_path := filepath.replace(".png", ".json")
	var f := FileAccess.open(meta_path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(meta, "\t"))
		f.close()

	print("EXPV0|SCREENSHOT|OK|%s" % filepath)
	return filepath


## Sample the average color of a rectangular region (20 evenly-spaced pixels).
static func sample_region_avg_color(image: Image, rect: Rect2) -> Color:
	var r_sum := 0.0
	var g_sum := 0.0
	var b_sum := 0.0
	var count := 0
	var samples := 20
	var x0 := int(rect.position.x)
	var y0 := int(rect.position.y)
	var w := int(rect.size.x)
	var h := int(rect.size.y)
	var img_w := image.get_width()
	var img_h := image.get_height()
	for i in range(samples):
		var t := float(i) / float(samples - 1) if samples > 1 else 0.5
		# Sample diagonally across the rect
		var px := clampi(x0 + int(t * w), 0, img_w - 1)
		var py := clampi(y0 + int(t * h), 0, img_h - 1)
		var c := image.get_pixel(px, py)
		r_sum += c.r
		g_sum += c.g
		b_sum += c.b
		count += 1
	if count == 0:
		return Color.BLACK
	return Color(r_sum / count, g_sum / count, b_sum / count)


## Returns a WARN string if >95% of sampled pixels are near-black (blank panel).
## Returns empty string if the region has visible content.
static func assert_region_nonempty(image: Image, rect: Rect2, label: String) -> String:
	var dark_count := 0
	var samples := 20
	var x0 := int(rect.position.x)
	var y0 := int(rect.position.y)
	var w := int(rect.size.x)
	var h := int(rect.size.y)
	var img_w := image.get_width()
	var img_h := image.get_height()
	for i in range(samples):
		var t := float(i) / float(samples - 1) if samples > 1 else 0.5
		var px := clampi(x0 + int(t * w), 0, img_w - 1)
		var py := clampi(y0 + int(t * h), 0, img_h - 1)
		var c := image.get_pixel(px, py)
		if c.r + c.g + c.b < 0.1:
			dark_count += 1
	var dark_ratio := float(dark_count) / float(samples)
	if dark_ratio > 0.95:
		return "%s: region appears blank (%.0f%% dark)" % [label, dark_ratio * 100]
	return ""


## Returns a WARN string if the average color deviates from expected beyond tolerance.
## Returns empty string if within tolerance.
static func assert_region_color(image: Image, rect: Rect2, expected: Color, tolerance: float, label: String) -> String:
	var avg := sample_region_avg_color(image, rect)
	var dr := absf(avg.r - expected.r)
	var dg := absf(avg.g - expected.g)
	var db := absf(avg.b - expected.b)
	var max_dev := maxf(dr, maxf(dg, db))
	if max_dev > tolerance:
		return "%s: color deviation %.2f > %.2f (avg=%.2f,%.2f,%.2f expected=%.2f,%.2f,%.2f)" % [
			label, max_dev, tolerance, avg.r, avg.g, avg.b, expected.r, expected.g, expected.b]
	return ""
