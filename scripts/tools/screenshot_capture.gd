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
