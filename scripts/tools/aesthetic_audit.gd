## EPIC.X.EXPERIENCE_PROOF.V0 — Component 5
## 14 visual/audio quality flags. Critical flags hard-fail.

## Flag severity
enum Severity { CRITICAL, WARNING }

## Run all aesthetic checks against an observer report.
## Returns [{flag, severity, passed, detail}].
func run_audit_v0(report: Dictionary) -> Array:
	var results: Array = []
	var scene: Dictionary = report.get("scene", {})
	var materials: Array = report.get("materials", [])
	var particles: Array = report.get("particles", [])
	var audio: Array = report.get("audio", [])
	var camera: Dictionary = report.get("camera", {})
	var hud: Dictionary = report.get("hud", {})
	var local: Dictionary = report.get("local_system", {})

	# --- CRITICAL flags (hard-fail) ---

	# 1. EMPTY_SYSTEM: no stations in local system
	var station_count: int = int(local.get("station_count", 0))
	results.append(_flag("EMPTY_SYSTEM", Severity.CRITICAL,
		station_count > 0,
		"station_count=%d" % station_count))

	# 2. NO_EMISSION: zero materials have emission enabled
	var emission_count := 0
	for m in materials:
		if m.get("emission_enabled", false):
			emission_count += 1
	results.append(_flag("NO_EMISSION", Severity.CRITICAL,
		emission_count > 0 or materials.size() == 0,
		"emission_materials=%d / total=%d" % [emission_count, materials.size()]))

	# 3. MISSING_HUD_LABELS: key HUD labels absent
	var labels: Array = hud.get("labels", [])
	var required_labels := ["CreditsLabel", "CargoLabel", "NodeLabel", "StateLabel"]
	var found_labels: Array = []
	for lbl in labels:
		found_labels.append(str(lbl.get("name", "")))
	var missing: Array = []
	for req in required_labels:
		if req not in found_labels:
			missing.append(req)
	results.append(_flag("MISSING_HUD_LABELS", Severity.CRITICAL,
		missing.size() == 0,
		"missing=%s" % str(missing)))

	# 4. STATION_NO_VISUAL: stations exist in scene but none have MeshInstance3D
	var group_counts: Dictionary = scene.get("group_counts", {})
	var scene_stations: int = int(group_counts.get("Station", 0))
	var station_has_mesh := false
	for m in materials:
		var p: String = str(m.get("path", ""))
		if "Station" in p or "station" in p:
			station_has_mesh = true
			break
	results.append(_flag("STATION_NO_VISUAL", Severity.CRITICAL,
		station_has_mesh or scene_stations == 0,
		"scene_stations=%d, has_mesh=%s" % [scene_stations, str(station_has_mesh)]))

	# 5. NO_FLEET_MESHES: fleet ships in scene but no meshes
	var fleet_count: int = int(group_counts.get("FleetShip", 0))
	var fleet_has_mesh := false
	for m in materials:
		var p: String = str(m.get("path", ""))
		if "Fleet" in p or "fleet" in p:
			fleet_has_mesh = true
			break
	results.append(_flag("NO_FLEET_MESHES", Severity.CRITICAL,
		fleet_has_mesh or fleet_count == 0,
		"fleet_count=%d, has_mesh=%s" % [fleet_count, str(fleet_has_mesh)]))

	# 6. SILENT_SCENE: no audio players exist or none have streams
	var audio_with_stream := 0
	for a in audio:
		if a.get("has_stream", false):
			audio_with_stream += 1
	results.append(_flag("SILENT_SCENE", Severity.CRITICAL,
		audio_with_stream > 0 or audio.size() == 0,
		"audio_players=%d, with_stream=%d" % [audio.size(), audio_with_stream]))

	# --- WARNING flags (report only) ---

	# 7. MONOCHROME_SCENE: hue diversity < 3 buckets
	var hue_score := _hue_diversity(materials)
	results.append(_flag("MONOCHROME_SCENE", Severity.WARNING,
		hue_score >= 3 or materials.size() == 0,
		"hue_buckets=%d" % hue_score))

	# 8. DEAD_PARTICLES: particles exist but none emitting
	var emitting_count := 0
	for p in particles:
		if p.get("emitting", false) and p.get("visible", false):
			emitting_count += 1
	results.append(_flag("DEAD_PARTICLES", Severity.WARNING,
		emitting_count > 0 or particles.size() == 0,
		"emitting=%d / total=%d" % [emitting_count, particles.size()]))

	# 9. HUD_OVERFLOW: any label text > 80 chars
	var overflow_labels: Array = []
	for lbl in labels:
		if str(lbl.get("text", "")).length() > 80:
			overflow_labels.append(str(lbl.get("name", "")))
	results.append(_flag("HUD_OVERFLOW", Severity.WARNING,
		overflow_labels.size() == 0,
		"overflow=%s" % str(overflow_labels)))

	# 10. ZERO_GLOW: emission enabled but energy == 0 on all
	var zero_glow := true
	for m in materials:
		if m.get("emission_enabled", false):
			if m.get("emission_energy", 0.0) > 0.0:
				zero_glow = false
				break
	results.append(_flag("ZERO_GLOW", Severity.WARNING,
		not zero_glow or emission_count == 0,
		"all_emission_energy_zero=%s" % str(zero_glow)))

	# 11. CAMERA_TOO_FAR: camera > 500 units from origin
	var cam_pos: String = str(camera.get("position", "(0.0, 0.0, 0.0)"))
	var cam_dist := _parse_vec3_magnitude(cam_pos)
	results.append(_flag("CAMERA_TOO_FAR", Severity.WARNING,
		cam_dist < 500.0 or not camera.get("found", false),
		"distance=%.1f" % cam_dist))

	# 12. CAMERA_TOO_CLOSE: camera < 1 unit from origin
	results.append(_flag("CAMERA_TOO_CLOSE", Severity.WARNING,
		cam_dist > 1.0 or not camera.get("found", false),
		"distance=%.1f" % cam_dist))

	# 13. NO_LABEL3D: no Label3D nodes in scene (stations/fleets should have names)
	var has_label3d := _has_type_in_tree(report, "Label3D")
	results.append(_flag("NO_LABEL3D", Severity.WARNING,
		has_label3d,
		"label3d_found=%s" % str(has_label3d)))

	# 14. ALL_SAME_REGION_COLOR: all stations have identical albedo
	var unique_albedos := {}
	for m in materials:
		var p: String = str(m.get("path", ""))
		if "Station" in p or "station" in p:
			unique_albedos[str(m.get("albedo_color", ""))] = true
	results.append(_flag("ALL_SAME_REGION_COLOR", Severity.WARNING,
		unique_albedos.size() != 1 or scene_stations <= 1,
		"unique_station_colors=%d" % unique_albedos.size()))

	return results


## Count critical failures.
func count_critical_failures_v0(results: Array) -> int:
	var count := 0
	for r in results:
		if r.get("severity", "") == "CRITICAL" and not r.get("passed", true):
			count += 1
	return count


## Color palette: count distinct hue buckets (30-degree bins).
func _hue_diversity(materials: Array) -> int:
	var buckets := {}
	for m in materials:
		var color_str: String = str(m.get("albedo_color", ""))
		if color_str.is_empty():
			continue
		var c := _parse_color(color_str)
		if c.a < 0.01:
			continue
		var hue_bucket: int = int(c.h * 12.0)  # 12 buckets = 30 degrees each
		buckets[hue_bucket] = true
	return buckets.size()


func _flag(name: String, severity: Severity, passed: bool, detail: String) -> Dictionary:
	return {
		"flag": name,
		"severity": "CRITICAL" if severity == Severity.CRITICAL else "WARNING",
		"passed": passed,
		"detail": detail,
	}


func _parse_color(s: String) -> Color:
	# Parse "(r, g, b, a)" format
	var clean := s.replace("(", "").replace(")", "").strip_edges()
	var parts := clean.split(",")
	if parts.size() < 3:
		return Color.BLACK
	var r := float(parts[0].strip_edges())
	var g := float(parts[1].strip_edges())
	var b := float(parts[2].strip_edges())
	var a := float(parts[3].strip_edges()) if parts.size() >= 4 else 1.0
	return Color(r, g, b, a)


func _parse_vec3_magnitude(s: String) -> float:
	var clean := s.replace("(", "").replace(")", "").strip_edges()
	var parts := clean.split(",")
	if parts.size() < 3:
		return 0.0
	var x := float(parts[0].strip_edges())
	var y := float(parts[1].strip_edges())
	var z := float(parts[2].strip_edges())
	return sqrt(x * x + y * y + z * z)


func _has_type_in_tree(report: Dictionary, type_name: String) -> bool:
	# Check materials/particles/audio for Label3D presence
	# Since we don't have a direct Label3D scan, check scene total vs group counts
	# For now, this is a heuristic — Label3D would appear in a separate observer pass
	# We mark this as false by default; the observer could be extended to track Label3D
	return false
