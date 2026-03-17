# Station & Ship Asset Generation Checklist

Step-by-step. Do them in order. Check off as you go.
For design context (why, faction lore, scale rationale), see `station_visual_design_v0.md`.

---

## SETUP

- [ ] Sign up at https://www.meshy.ai (free: 5 generations/day, Pro: $20/mo)
- [ ] Sign up at https://www.tripo3d.ai (free: limited, Pro: $10/mo)
- [ ] Install Blender (https://www.blender.org) if not already installed
- [ ] Create `assets/models/stations/` folder in project
- [ ] Create `assets/models/ships/` folder in project

---

## BLENDER PASS (do this for EVERY model after downloading)

1. File → Import → glTF 2.0 (.glb)
2. Orientation: -Z forward, +Y up. If wrong → select → R, X, 90, Enter
3. Scale: should be 1-10 Blender units. If wrong → S, type factor, Enter
4. Polycount: bottom-right shows "Tris". Cores < 8000, ships < 4000, details < 1500. If over → Tab, Edit Mode, Mesh → Decimate
5. Normals: Edit Mode → Overlays → Face Orientation. Blue = good, red = flip (select, Mesh → Normals → Flip)
6. Export → glTF 2.0 (.glb), "Selected Objects" only, to correct folder

## GODOT TEST (do this for EVERY model after Blender pass)

1. Drop GLB into `assets/models/stations/` or `assets/models/ships/`
2. Open test scene, drag model in
3. Place a 5×5×5 BoxMesh nearby (represents your ship)
4. Station cores should be 16-50x bigger than the box
5. View top-down (Numpad 7). Silhouette clear? Archetype recognizable?
6. If it looks wrong at game scale → regenerate, don't fix

---

## BATCH 1 — Standard station structure

**Tool**: https://www.meshy.ai → Text to 3D
**Settings**: Low Poly mode, Texture Richness: High, Export: GLB, Quad topology

### 1.1 Trade Hub core (STYLE ANCHOR — do this first)

- [ ] **Generate**: Paste prompt below into Meshy. Generate 4 variants.
- [ ] **Pick**: Choose the best TOP-DOWN silhouette
- [ ] **Refine**: Click Refine on chosen model (adds PBR textures)
- [ ] **Export**: GLB, target 5000 faces, Quad topology
- [ ] **Screenshot**: Save 3 angles (top-down, 45°, side) as `reference_station_style.png`
- [ ] **Blender pass**
- [ ] **Godot test**
- [ ] **Save as**: `assets/models/stations/station_core_trade_hub.glb`

**DO NOT CONTINUE until you like this model. It sets the style for everything.**

```
Low-poly sci-fi space station central hub, wide flat disc shape with octagonal
symmetry, heavy docking clamps on perimeter, recessed panel lines across hull
surface, antenna cluster on top face, docking bay openings on 4 cardinal faces,
industrial gray metal with subtle blue-white panel lighting, PBR metallic
surface with weathering, game-ready asset. Negative: no interior geometry, no
text, no logos, no organic shapes, no floating parts.
```

### 1.2 Docking bay module

- [ ] **Generate** (Meshy, 4 variants, pick best match to 1.1)
- [ ] **Refine** → **Export** GLB, target 3000 faces
- [ ] **Blender pass**
- [ ] **Godot test** (opening should be ~2x the 5u reference cube width)
- [ ] **Save as**: `assets/models/stations/station_docking_bay.glb`

```
Low-poly sci-fi space station docking bay module, rectangular open hangar
frame 2x wider than tall, guide light strips along entry edges, magnetic clamp
rails on interior floor, structural cross-bracing on sides, industrial gray
metal with warm white interior flood lighting, PBR metallic surface,
game-ready asset. Negative: no interior detail beyond floor rails, no text, no
ships inside.
```

### 1.3 Arm / spoke segment

- [ ] **Generate** → **Pick** → **Refine** → **Export** GLB, target 3000 faces
- [ ] **Blender pass**
- [ ] **Godot test**
- [ ] **Save as**: `assets/models/stations/station_arm_segment.glb`

```
Low-poly sci-fi space station arm segment, elongated rectangular truss
structure with corridor spine, cable runs along exterior, attachment hardpoints
every 5 units, structural cross-bracing, connection flanges at both ends,
industrial gray metal, PBR surface with panel line normal map detail,
game-ready asset. Negative: no interior, no text, no curves, no organic shapes.
```

### 1.4 Ring segment

- [ ] **Generate** → **Pick** → **Refine** → **Export** GLB, target 4000 faces
- [ ] **Blender pass**
- [ ] **Godot test**
- [ ] **Save as**: `assets/models/stations/station_ring_segment.glb`

```
Low-poly sci-fi space station ring segment, 45-degree curved arc section,
habitat windows along inner face, hull panels along outer face, structural
ribbing at both cut ends for tiling, connection flanges, medium gray metal
with rows of warm-white window lights on inner curve, PBR metallic surface,
game-ready asset. Negative: no interior geometry, no text, no flat sections.
```

### 1.5 Command tower

- [ ] **Generate** → **Pick** → **Refine** → **Export** GLB, target 3000 faces
- [ ] **Blender pass**
- [ ] **Godot test**
- [ ] **Save as**: `assets/models/stations/station_command_tower.glb`

```
Low-poly sci-fi space station command tower, vertical cylindrical body with
observation deck ring near top, antenna array on peak, docking beacon light at
apex, structural base flange for mounting, panel detail on hull surface, dark
gray metal with blue-white accent lighting on observation ring, PBR metallic
surface, game-ready asset. Negative: no interior, no text, no organic shapes.
```

### Batch 1 checkpoint

- [ ] All 5 parts look like they belong together (same material language)
- [ ] Trade Hub core is 16-50x bigger than the 5u reference cube in Godot

---

## BATCH 2 — Station detail parts

**Tool**: Meshy (same session or use `reference_station_style.png` as Image-to-3D reference)
**Face targets**: 500-1500 tris for all detail parts

### 2.1 Refinery drum

- [ ] Generate → Pick → Refine → Export GLB, 1000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_module_refinery.glb`

```
Low-poly sci-fi refinery processing drum, horizontal cylinder with pipe
connections at both ends, pressure gauge panels on surface, exhaust port on
top, structural mounting bracket on bottom, dark metal with orange hazard
markings, industrial weathered PBR surface, game-ready asset.
```

### 2.2 Solar array

- [ ] Generate → Pick → Refine → Export GLB, 1000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_module_solar.glb`

```
Low-poly sci-fi solar panel array, flat rectangular panel grid on articulated
arm, 4x2 panel layout with visible cell grid pattern, pivot joint at base,
dark blue-black panels with silver frame, reflective PBR surface, game-ready
asset.
```

### 2.3 Cargo pod cluster

- [ ] Generate → Pick → Refine → Export GLB, 1000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_module_cargo.glb`

```
Low-poly sci-fi cargo container cluster, 6 rectangular containers grouped in
2x3 grid on mounting frame, each container slightly different color tone,
magnetic clamp mounts, cargo markings on faces, mixed gray-brown metal tones,
utilitarian PBR surface, game-ready asset.
```

### 2.4 Medical module

- [ ] Generate → Pick → Refine → Export GLB, 1000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_module_medical.glb`

```
Low-poly sci-fi space station medical bay module, compact rectangular pod with
red cross marking on roof, docking port on one end, observation window on
front face, life support vents on sides, clean white hull with red accent
strips, sterile PBR metallic surface, game-ready asset. Negative: no interior
detail, no text beyond cross symbol.
```

### 2.5 Research lab module

- [ ] Generate → Pick → Refine → Export GLB, 1000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_module_lab.glb`

```
Low-poly sci-fi space station laboratory module, cylindrical pod with
transparent dome viewport on top, sensor antenna array on one end, data cable
conduits along hull, equipment access hatch on side, white hull with blue
accent panels, clean PBR metallic surface, game-ready asset. Negative: no
interior detail, no text.
```

### 2.6 Station turret (single)

- [ ] Generate → Pick → Refine → Export GLB, 800 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_turret_single.glb`

```
Low-poly sci-fi space station defense turret, compact rotating platform base
with single barrel cannon, armored housing around barrel root, targeting sensor
dome on top, mounting bracket on bottom for hull attachment, dark gunmetal with
red targeting laser emitter tip, military PBR metallic surface, game-ready
asset. Negative: no interior, no text, no organic shapes.
```

### 2.7 Station turret (double)

- [ ] Generate → Pick → Refine → Export GLB, 1000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_turret_double.glb`

```
Low-poly sci-fi space station heavy defense turret, wide rotating platform
base with twin parallel barrel cannons, heavy armored housing, sensor dome
between barrels, reinforced mounting bracket on bottom, dark gunmetal with
red accent on barrel tips, battle-worn PBR metallic surface, game-ready asset.
Negative: no interior, no text.
```

### 2.8 Sensor dish (standard)

- [ ] Generate → Pick → Refine → Export GLB, 600 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_dish_standard.glb`

```
Low-poly sci-fi satellite dish antenna, concave circular dish on articulated
arm mount, feed horn at dish focal point, cable conduit along arm, structural
base plate for hull mounting, light gray dish with silver metallic frame, PBR
reflective surface, game-ready asset. Negative: no interior, no text.
```

### 2.9 Sensor dish (large array)

- [ ] Generate → Pick → Refine → Export GLB, 1200 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_dish_array.glb`

```
Low-poly sci-fi large sensor array, 3 concave dishes in triangular cluster on
heavy structural frame, each dish with individual feed horn, central processing
unit between dishes, heavy base mount, industrial gray with blue accent
lighting on processing unit, PBR metallic surface, game-ready asset.
```

### 2.10 Beacon buoy

- [ ] Generate → Pick → Refine → Export GLB, 500 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_beacon_buoy.glb`

```
Low-poly sci-fi navigation beacon buoy, vertical cylindrical body with
flashing light dome on top, faction banner mounting ring around middle,
stabilization fins at base, antenna spike below, dark gray body with bright
emissive dome (color set in-engine per faction), PBR metallic surface,
game-ready asset. Negative: no text, no interior.
```

### Batch 2 checkpoint

- [ ] All 10 detail parts match the Batch 1 style
- [ ] Each is roughly 1/10th to 1/20th the size of the Trade Hub core in Godot

---

## BATCH 3 — Variant station cores

**Tool**: Meshy (use `reference_station_style.png` as Image-to-3D reference if style drifts)
**Face target**: 5000 tris each

### 3.1 Industrial/Refinery core

- [ ] Generate → Pick → Refine → Export GLB, 5000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_core_industrial.glb`

```
Low-poly sci-fi industrial space station core module, asymmetric cylindrical
body with offset machinery block, exposed pipe bundles along hull, large
exhaust vents on rear face, heavy structural ribbing, ore processing drum
mounted laterally, dark gunmetal with orange hazard accent strips, scratched
worn PBR metal, game-ready asset. Negative: no interior, no text, no clean
surfaces, no symmetry.
```

### 3.2 Military/Fortress core

- [ ] Generate → Pick → Refine → Export GLB, 5000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_core_military.glb`

```
Low-poly sci-fi military space station core, compact armored octagonal hull,
thick angular armor plating with beveled edges, recessed weapon hardpoints on
all faces, minimal windows, heavy blast doors on docking faces, command tower
stub on top, dark charcoal metal with red warning strips, battle-scarred PBR
surface, game-ready asset. Negative: no interior, no text, no organic curves,
no fragile parts.
```

### 3.3 Research/Observatory core

- [ ] Generate → Pick → Refine → Export GLB, 5000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_core_research.glb`

```
Low-poly sci-fi research space station core, small central cylinder body with
4 extended instrument boom attachment points, clean white hull panels, large
observation dome on top face, solar panel mounting brackets on sides, delicate
structural framework visible, bright white with blue accent panels, clean PBR
metal, game-ready asset. Negative: no interior, no text, no heavy armor, no
weapons.
```

### 3.4 Frontier Outpost core

- [ ] Generate → Pick → Refine → Export GLB, 3000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/station_core_frontier.glb`

```
Low-poly sci-fi frontier outpost, single repurposed cargo container hull with
bolted-on extensions, mismatched hull panels of different metal tones, antenna
mast welded to top, external cargo nets and strapped containers, patched hull
breach visible, rust-brown and bare metal with flickering yellow work light
spots, weathered scratched PBR surface, game-ready asset. Negative: no
interior, no text, no clean surfaces, no uniformity.
```

### Batch 3 checkpoint

- [ ] Each core has a DIFFERENT silhouette from the Trade Hub
- [ ] All share the same material quality/language (even if colors differ)

---

## BATCH 4 — Ancient / Precursor station parts

**Tool**: Switch to https://www.tripo3d.ai → Text to 3D
**This is a completely different style family. No human station reference.**
**Face target**: 5000 tris for core, 1000-2000 for other parts

### 4.1 Ancient station core

- [ ] Generate on Tripo → Pick → Export GLB
- [ ] Blender pass → Godot test
- [ ] **Screenshot as `reference_ancient_style.png`** (style anchor for Batch 10)
- [ ] **Save as**: `assets/models/stations/station_core_ancient.glb`

```
Low-poly alien space station core, organic flowing geometry with bilateral
symmetry, no visible seams bolts or rivets, smooth curved surfaces with
crystalline ridge lines, resonance chamber void in center, spire attachment
points at cardinal directions, deep purple-black hull absorbing light with
amber-gold glowing vein patterns on surface, bioluminescent PBR emissive
surface, game-ready asset. Negative: no human architecture, no right angles,
no industrial elements, no text, no mechanical joints.
```

### 4.2 Crystalline spire

- [ ] Generate → Pick → Export GLB, 1500 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/ancient_spire.glb`

```
Low-poly alien crystalline spire, tall tapering organic crystal formation,
faceted surfaces catching light, internal amber-gold glow visible through
translucent edges, smooth base mounting surface, deep purple-black crystal
body with amber bioluminescent veins, emissive PBR surface, game-ready asset.
Negative: no human elements, no metal, no mechanical parts, no text.
```

### 4.3 Resonance chamber

- [ ] Generate → Pick → Export GLB, 2000 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/ancient_resonance_chamber.glb`

```
Low-poly alien resonance chamber, hollow organic torus shape with internal
energy visible through lattice openings, crystalline nodes at cardinal points
around ring, smooth flowing connection arms at top and bottom, deep purple-black
with intense amber-gold energy glow from interior, bioluminescent PBR emissive
surface, game-ready asset. Negative: no human architecture, no right angles,
no pipes, no text.
```

### 4.4 Ancient organic arm

- [ ] Generate → Pick → Export GLB, 1500 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/ancient_arm.glb`

```
Low-poly alien space station arm, elongated organic tendril structure with
smooth flowing curves, crystalline ridge along dorsal surface, amber-gold
bioluminescent vein patterns, connection bulb at each end, deep purple-black
body, organic PBR surface with subtle iridescence, game-ready asset. Negative:
no human architecture, no straight lines, no bolts, no text.
```

### 4.5 Ancient docking void

- [ ] Generate → Pick → Export GLB, 1500 faces
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/stations/ancient_docking_void.glb`

```
Low-poly alien docking structure, organic opening in smooth curved hull,
petal-like guide surfaces around opening, amber-gold bioluminescent guide
lights lining the opening edges, interior depth visible as dark void,
connection surface flows seamlessly from surrounding hull, deep purple-black
with amber accents, organic PBR surface, game-ready asset. Negative: no
mechanical clamps, no human elements, no right angles, no text.
```

### Batch 4 checkpoint

- [ ] All 5 ancient parts look ALIEN — no human station vibes
- [ ] All share the purple-black + amber-gold palette
- [ ] Ancient core looks completely different from all Batch 1-3 cores

---

## BATCH 5 — Concord fleet (blue, official, symmetric)

**Tool**: https://www.tripo3d.ai → Text to 3D
**Face target**: Fighters 1500, Frigates 2500, Cruisers 4000
**Key rule**: Pick for TOP-DOWN silhouette. Front/back must be obvious. Engine glow at rear.

### 5.1 Concord Fighter

- [ ] Generate → Pick best top-down silhouette → Export GLB
- [ ] Blender pass → Godot test (should be ~3-4u long vs 5u cube)
- [ ] **Save as**: `assets/models/ships/concord_fighter.glb`

```
Low-poly sci-fi military fighter spaceship, compact symmetric arrowhead hull,
tapered nose, paired small engine nacelles at rear with blue glow zones,
dorsal sensor bump, stubby weapon pylons on wing edges, brushed steel-blue
hull with white regulation stripe, PBR metallic surface, optimized for
top-down view with clear bow-stern distinction, game-ready asset. Negative:
no interior, no text, no organic shapes, no thin vertical fins, no landing gear.
```

### 5.2 Concord Frigate

- [ ] Generate → Pick → Export GLB
- [ ] Blender pass → Godot test (should be ~5-7u long)
- [ ] **Save as**: `assets/models/ships/concord_frigate.glb`

```
Low-poly sci-fi military frigate spaceship, clean symmetric hull design,
tapered nose with sensor array, paired engine nacelles at rear with blue glow
zones, shield emitter dome on dorsal center, wing-mounted weapon hardpoints,
official regulation markings, brushed steel-blue hull with white panel accents,
PBR metallic surface with subtle weathering, optimized for top-down view with
clear bow-stern distinction, game-ready asset. Negative: no interior, no text,
no organic shapes, no thin vertical fins, no landing gear.
```

### 5.3 Concord Cruiser

- [ ] Generate → Pick → Export GLB
- [ ] Blender pass → Godot test (should be ~8-12u long)
- [ ] **Save as**: `assets/models/ships/concord_cruiser.glb`

```
Low-poly sci-fi military cruiser spaceship, wide imposing symmetric hull,
broad tapered prow with heavy sensor array, 4 engine nacelles at rear in
paired configuration with blue glow zones, large dorsal shield emitter dome,
turret hardpoints along hull edges, command bridge raised section on dorsal
centerline, brushed steel-blue hull with white panel accents and fleet
insignia areas, PBR metallic surface with weathering, optimized for top-down
view, game-ready asset. Negative: no interior, no text, no organic shapes, no
thin vertical fins, no landing gear.
```

### Batch 5 checkpoint

- [ ] All 3 ships are clearly Concord (blue steel, symmetric, official)
- [ ] Fighter < Frigate < Cruiser in size
- [ ] Each has a distinct top-down silhouette (not just scaled versions of each other)
- [ ] Front/back obvious on all 3 when spinning

---

## BATCH 6 — Chitin Syndicate fleet (bronze, faceted, beetle-like)

**Tool**: Tripo

### 6.1 Chitin Fighter

- [ ] Generate → Pick → Export GLB
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/ships/chitin_fighter.glb`

```
Low-poly sci-fi alien fighter spaceship, compact faceted beetle-shell hull,
compound-eye sensor cluster on dorsal nose, swept angular wings with sharp
edges, single wide exhaust port at stern with amber glow, iridescent dark
bronze hull panels, PBR metallic surface with iridescent sheen, optimized for
top-down view with readable rotation, game-ready asset. Negative: no interior,
no text, no human cockpit, no thin vertical fins.
```

### 6.2 Chitin Frigate

- [ ] Generate → Pick → Export GLB
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/ships/chitin_frigate.glb`

```
Low-poly sci-fi alien trader spaceship, faceted beetle-shell hull with
compound-eye sensor dome cluster on dorsal surface, wide cargo bay midsection,
asymmetric antenna boom on starboard side, dual exhaust ports at stern with
amber glow, iridescent dark bronze hull panels with amber accent lighting,
PBR metallic surface with iridescent sheen, optimized for top-down view with
readable rotation, game-ready asset. Negative: no interior, no text, no human
cockpit, no thin vertical fins.
```

### 6.3 Chitin Cruiser

- [ ] Generate → Pick → Export GLB
- [ ] Blender pass → Godot test
- [ ] **Save as**: `assets/models/ships/chitin_cruiser.glb`

```
Low-poly sci-fi alien cruiser spaceship, large faceted beetle-shell hull with
layered chitin plating, massive compound-eye sensor array covering dorsal bow
section, multiple small turret bumps along hull edges, 4 exhaust ports at
stern with amber glow, asymmetric sensor boom on port side, iridescent dark
bronze hull with amber accent lighting and probability display panels,
PBR metallic surface with iridescent sheen, optimized for top-down view,
game-ready asset. Negative: no interior, no text, no human elements, no thin
vertical fins.
```

---

## BATCH 7 — Weavers fleet (green, angular, fortress-like)

**Tool**: Tripo

### 7.1 Weavers Fighter

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/weavers_fighter.glb`

```
Low-poly sci-fi heavy fighter spaceship, compact angular hull with thick armor
plating, wide armored prow with reinforced ram edge, web-lattice wing struts
on both sides, single heavy engine at rear with green glow, dark forest green
matte hull with visible structural stress lines, heavy PBR metallic surface,
optimized for top-down view with clear facing direction, game-ready asset.
Negative: no interior, no text, no curves, no thin vertical fins.
```

### 7.2 Weavers Frigate

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/weavers_frigate.glb`

```
Low-poly sci-fi armored frigate spaceship, heavy angular hull with layered
thick plating, wide armored prow tapering to reinforced point, web-lattice
structural framework visible between hull plates on wings, concealed weapon
bays along flanks, dual heavy engines at rear with green glow, dark forest
green matte hull with stress-line accents, fortress-like PBR metallic surface,
optimized for top-down view, game-ready asset. Negative: no interior, no text,
no organic curves, no thin vertical fins, no fragile parts.
```

### 7.3 Weavers Cruiser

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/weavers_cruiser.glb`

```
Low-poly sci-fi heavy cruiser spaceship, massive angular hull with fortress-like
layered armor, very wide armored prow with battering ram geometry, web-lattice
wing struts extending laterally, multiple concealed turret bays along hull
edges, 4 heavy engines at rear with green glow, anchor cable attachment
points visible, dark forest green matte hull with structural stress line
accents, heavily armored PBR metallic surface, optimized for top-down view,
game-ready asset. Negative: no interior, no text, no organic curves, no thin
fins, no decorative elements.
```

---

## BATCH 8 — Valorin Clans fleet (red, rough, kitbashed)

**Tool**: Tripo

### 8.1 Valorin Fighter

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/valorin_fighter.glb`

```
Low-poly sci-fi kitbashed fighter spaceship, compact rough hull with
mismatched panel sizes, oversized single engine at rear relative to small body
with orange-red glow, bolted-on weapon pod on one wing, patched hull plating,
rust red and bare metal hull with visible welds, weathered scratched PBR
surface, optimized for top-down view with clear asymmetry for spin readability,
game-ready asset. Negative: no interior, no text, no clean surfaces, no
uniformity, no thin vertical fins.
```

### 8.2 Valorin Frigate

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/valorin_frigate.glb`

```
Low-poly sci-fi kitbashed freighter spaceship, rough asymmetric hull with
bolted-on cargo pods strapped to starboard side, oversized dual engines at
rear with orange-red glow, mismatched hull panels of different metal tones,
antenna mast welded to port side, external fuel tank lashed to hull, rust red
and bare metal with visible welds and patches, weathered PBR surface,
optimized for top-down view with readable rotation, game-ready asset.
Negative: no interior, no text, no clean surfaces, no thin vertical fins.
```

### 8.3 Valorin Cruiser

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/valorin_cruiser.glb`

```
Low-poly sci-fi kitbashed cruiser spaceship, large rough hull assembled from
multiple repurposed ship sections, 3 mismatched oversized engines at rear with
orange-red glow, cargo container clusters strapped to hull sides, multiple
welded-on weapon platforms, antenna forest on dorsal surface, rust red with
patches of bare metal and yellow hazard paint, heavily weathered PBR surface,
optimized for top-down view, game-ready asset. Negative: no interior, no text,
no uniformity, no thin vertical fins, no elegance.
```

---

## BATCH 9 — Drifter Communion fleet (violet, smooth, spiritual)

**Tool**: Tripo

### 9.1 Communion Fighter

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/communion_fighter.glb`

```
Low-poly sci-fi spiritual scout spaceship, sleek minimal hull with smooth
curves and no hard edges, open lattice wing structure on both sides, single
crystal formation on dorsal ridge, gentle engine glow at rear in soft violet,
pale gray-violet hull with crystal inlay accents, smooth PBR surface with
subtle luminescence, optimized for top-down view with readable facing,
game-ready asset. Negative: no interior, no text, no weapons visible, no
angular shapes, no thin vertical fins.
```

### 9.2 Communion Frigate

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/communion_frigate.glb`

```
Low-poly sci-fi spiritual frigate spaceship, flowing curved hull with minimal
hard edges, open wing structures with crystalline lattice supports, crystal
garden formation on dorsal center, resonance antenna extending from bow,
gentle dual engine glow at rear in soft violet, pale gray-violet hull with
shimmer-crystal inlays, translucent panel sections on wings, smooth PBR
surface with luminescence, optimized for top-down view with readable rotation,
game-ready asset. Negative: no interior, no text, no heavy armor, no angular
shapes, no thin vertical fins.
```

### 9.3 Communion Cruiser

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/communion_cruiser.glb`

```
Low-poly sci-fi spiritual cruiser spaceship, large flowing curved hull,
sweeping open wing structures with crystalline lattice framework, large crystal
garden formation on dorsal surface, multiple resonance antennae at bow,
meditation spire rising from center, gentle quad engine glow at rear in soft
violet, pale gray-violet hull with extensive shimmer-crystal inlays,
translucent panel sections, smooth PBR surface with ethereal luminescence,
optimized for top-down view, game-ready asset. Negative: no interior, no text,
no heavy armor, no weapons visible, no angular shapes, no thin vertical fins.
```

---

## BATCH 10 — Ancient relic ships (match Batch 4 style)

**Tool**: Tripo (use `reference_ancient_style.png` from 4.1 if style drifts)

### 10.1 Seeker (relic frigate/clipper)

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/ancient_seeker.glb`

```
Low-poly alien scout spaceship, organic flowing hull with bilateral symmetry,
no visible seams or mechanical joints, elongated teardrop body with sensor
array tendrils extending from bow, crystalline ridge running along dorsal
spine, resonance emitter node at stern glowing amber-gold, deep purple-black
hull with bioluminescent amber vein patterns across surface, alien
bioluminescent PBR emissive surface, optimized for top-down silhouette with
clear directional facing, game-ready asset. Negative: no human architecture,
no right angles, no engines, no cockpit, no mechanical parts.
```

### 10.2 Bastion (relic dreadnought)

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/ancient_bastion.glb`

```
Low-poly alien capital warship, massive organic hull with imposing bilateral
symmetry, thick flowing armor curves with crystalline weapon ridge along
dorsal centerline, lattice drone bays visible as dark recesses on flanks,
containment field emitter array at bow, resonance drive core glowing
amber-gold at stern, deep purple-black hull with dense bioluminescent amber
vein network, heavy alien PBR emissive surface, optimized for top-down
silhouette where it reads as a small station, game-ready asset. Negative: no
human architecture, no right angles, no mechanical turrets, no text.
```

### 10.3 Threshold (relic cruiser)

- [ ] Generate → Pick → Export GLB → Blender → Godot
- [ ] **Save as**: `assets/models/ships/ancient_threshold.glb`

```
Low-poly alien cruiser spaceship, medium organic hull with unsettling bilateral
symmetry, void-dark surfaces with geometry that suggests folded dimensions,
phase-shift apertures along hull edges glowing deep violet, crystalline sensor
crown on dorsal bow, resonance drive at stern glowing amber-gold, deep
purple-black hull that absorbs surrounding light with amber and violet
bioluminescent patterns, alien PBR emissive surface, optimized for top-down
silhouette with clear directional facing, game-ready asset. Negative: no human
elements, no right angles, no mechanical parts, no text.
```

---

## FINAL CHECKPOINT

- [ ] 24 station/detail parts in `assets/models/stations/`
- [ ] 18 ship models in `assets/models/ships/`
- [ ] Every faction's ships look different from every other faction at a glance
- [ ] Ancient parts look alien — completely different from human stations
- [ ] All models pass: orientation, polycount, normals, scale
- [ ] Station cores dwarf the 5u reference cube in Godot
- [ ] Ship front/back distinguishable from top-down while spinning

---

## TROUBLESHOOTING

| Problem | Fix |
|---------|-----|
| Wrong silhouette | Regenerate. Don't fix in Blender. |
| Right shape, wrong texture | Keep it. We re-material in Godot per faction. |
| Too many polys | Decimate in Blender |
| Wrong orientation | Rotate in Blender |
| Parts don't match style | Use Trade Hub screenshot as Image-to-3D reference |
| AI adds text/logos | Regenerate with stronger negative prompt |
| Model has holes | Blender → Edit Mode → Mesh → Clean Up → Fill Holes. If bad, regenerate |
