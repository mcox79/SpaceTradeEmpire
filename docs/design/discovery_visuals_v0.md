# Discovery Visuals — Diegetic Design v0

## Gate Tranche: Diegetic Discovery Visuals (T42 prefix, proposed for T59)

### GATE.T42.DISC_VIZ.FAMILY_MESH.001 — Family-specific 3D compositions

Replace the single orange sphere+ring with per-family procedural meshes in `CreateDiscoverySiteMarkerV0`:

| Family | Visual Composition | Reference |
|---|---|---|
| DERELICT | Damaged hull fragments (2-3 rotated box meshes), flickering point light, small debris particle field | Dead Space, Subnautica wrecks |
| RUIN | Angular geometric structure (stacked rotated cubes/cylinders), faint energy emission lines, stone-like material | Outer Wilds quantum towers |
| SIGNAL | Antenna array (thin cylinder + sphere tip), pulsing electromagnetic distortion ring, periodic beacon flash | Elite Dangerous signal sources |
| RESOURCE_POOL | Mineral cluster (3-5 small irregular spheres), faint resource-colored glow matching good type | Subnautica resource nodes |
| CORRIDOR | Faint trail of navigation markers (3 small spheres in a line), subtle directional glow | Waypoint beacons |

### GATE.T42.DISC_VIZ.PHASE_LOD.002 — Phase-dependent visual states

Each family mesh changes appearance based on discovery phase:

| Phase | Visual Treatment |
|---|---|
| SEEN | Ghostly silhouette — low alpha (0.3), scanner-noise overlay shader, barely visible at distance. Player detects something but can't resolve it |
| SCANNED | Object solidifies — full alpha, ambient particles active, identifiable as wreck/ruin/signal. Material emissive but muted |
| ANALYZED | Full detail — bright emission, environmental storytelling elements (cargo crates near derelict, inscriptions on ruin), green accent glow |

### GATE.T42.DISC_VIZ.APPROACH_FEEDBACK.003 — Progressive approach audio/visual

- Distance brackets: >30u (scanner blip icon only), 15-30u (silhouette visible), <15u (detail resolves)
- Audio: scanner ping intensifies as approach distance decreases
- HUD bracket narrows as player closes in (like Elite Dangerous POI approach)

### GATE.T42.DISC_VIZ.SCAN_CEREMONY.004 — Scan duration + celebration

- Scan is no longer instant — 3-5 second hold with progress ring VFX
- Phase transition: brief pause, distinct audio chime, card showing what was learned
- Per Axiom 6: "Phase transitions deserve celebration"

### GATE.T42.DISC_VIZ.TUTORIAL_BEAT.005 — Guided first-scan introduction

- After player unlocks `sensor_suite` tech, FO triggers: "Commander, sensors are online. I'm detecting... something at bearing 270. Worth investigating."
- First discovery site has a waypoint. FO walks player through first scan
- Teaches the SEEN->SCANNED->ANALYZED lifecycle in a scripted moment (Subnautica pattern)

## Dependencies

- Gates 001-002 are pure GalaxyView.cs visual work (no SimCore changes)
- Gate 003 needs `edgedar_overlay.gd` + `game_manager.gd`
- Gate 004 needs SimCore scan duration + GDScript VFX
- Gate 005 needs TutorialSystem + FirstOfficerSystem integration
