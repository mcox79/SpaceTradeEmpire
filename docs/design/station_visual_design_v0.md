# Station Visual Design v0

## Purpose

Define the modular station part system, faction visual identities, scale rules, and
art direction so that every station in the game reads as a massive, inhabited structure
with clear faction ownership at a glance.

---

## 1. Scale Reference

### The golden rule: stations must dwarf ships

| Element | Game units | Real-world analogy |
|---------|-----------|-------------------|
| Player ship (Insurgent) | ~5u long | Fighter jet |
| NPC freighter | ~8u long | Cargo plane |
| Small outpost | 30-50u across | Oil rig |
| Standard station | 80-120u across | Aircraft carrier |
| Capital station | 150-250u across | Small city block |
| Haven starbase | 200-400u across | Space city |

A docked player ship should look like a boat next to a skyscraper.
Elite Dangerous and X4 sell station scale by making ships feel tiny at approach.
We achieve this through silhouette size, surface detail density, and docking bay
proportions that make the player ship look small.

### Tier scaling (derived from lane-gate count)

| Tier | Gates | Label | Scale factor | Approx diameter |
|------|-------|-------|-------------|-----------------|
| 0 | 1 | Outpost | 0.6x | 30-50u |
| 1 | 2-3 | Hub | 1.0x | 80-120u |
| 2 | 4+ | Capital | 1.5x | 150-250u |

Scale factor applies uniformly to the assembled station scene. Outposts feel
like frontier posts; capitals feel like cities.

---

## 2. Modular Part System

### Core structural parts

Every station is assembled from a subset of these modular categories.
Parts snap together in the Godot editor or via procedural assembly at runtime.

| Category | Purpose | Examples |
|----------|---------|---------|
| **Core hull** | Central body, largest single piece | Cylinder hub, sphere hub, ring hub, slab platform |
| **Arms / spokes** | Radial extensions from core | Docking arms, crane booms, corridor spines |
| **Ring segments** | Orbital or structural rings | Habitat ring, defense ring, cargo ring |
| **Docking bays** | Where ships attach (scale reference!) | Open bay, enclosed hangar, clamp dock |
| **Towers / masts** | Vertical (or radial) protrusions | Command tower, antenna mast, sensor array |
| **Surface greeble** | Small-scale hull detail | Panel lines, vents, pipe runs, conduit bundles |
| **Functional modules** | Role-specific attachable pieces | Refinery drum, solar array, weapon turret, dish antenna |
| **Accent lighting** | Emissive strips, window rows, beacon | Faction-colored running lights, docking guide lights |

### Assembly rules

1. Every station has exactly ONE core hull
2. Arms attach radially (2-8 depending on tier)
3. Ring segments are optional (0-2 rings)
4. Docking bays attach to arm ends or core hull faces
5. Towers attach to core hull top/bottom
6. Surface greeble populates all large flat surfaces procedurally
7. Functional modules attach to arms or ring hardpoints
8. Accent lighting follows faction color rules (see Section 4)

### Level of detail strategy

| Camera altitude | What renders |
|----------------|-------------|
| < 80u (flight) | Full model, all greeble, docking bay interiors lit |
| 80-200u | Full model, greeble simplified, bays are emissive rectangles |
| 200-500u | Silhouette + accent lights only (billboard candidate) |
| > 500u | Point light + label |

---

## 3. Station Archetypes

Each archetype defines a silhouette family. Factions customize within the archetype
using faction-specific parts, colors, and functional modules.

### Trade Hub

- **Silhouette**: Wide, flat, many docking arms radiating outward. Busy.
- **Core**: Large ring or disc hull
- **Signature parts**: 4-6 docking arms, cargo container clusters, antenna arrays
- **Mood**: Bright, well-lit, commercial signage, heavy traffic
- **Reference**: Deep Space Nine, Coriolis stations (Elite)

### Industrial / Refinery

- **Silhouette**: Asymmetric, heavy machinery, pipes, smoke/venting
- **Core**: Slab or cylinder hull, offset center of mass
- **Signature parts**: Refinery drums, pipe networks, ore hoppers, crane booms
- **Mood**: Harsh directional lighting, orange/amber work lights, utilitarian
- **Reference**: Nostromo refinery (Alien), X4 factory stations

### Military / Fortress

- **Silhouette**: Compact, angular, bristling with turrets
- **Core**: Armored sphere or octagonal hull
- **Signature parts**: Turret clusters, armor plating, minimal external docking (internal bays), sensor domes
- **Mood**: Dark hull, red accent lighting, intimidating, few windows
- **Reference**: Imperial Star Destroyers (geometric aggression), Halo UNSC stations

### Research / Observatory

- **Silhouette**: Tall/vertical, delicate, lots of dishes and sensor booms
- **Core**: Small central body with extended instrument booms
- **Signature parts**: Large dish arrays, telescope tubes, lab pods on struts, solar panels
- **Mood**: Clean white/blue, scientific, sparse, purposeful
- **Reference**: ISS (functional modularity), Interstellar Endurance

### Frontier Outpost

- **Silhouette**: Small, rough, improvised-looking
- **Core**: Single module or repurposed ship hull
- **Signature parts**: Patched plating, external cargo strapped on, minimal infrastructure
- **Mood**: Dim, flickering lights, survivalist
- **Reference**: Mos Eisley cantina in space, Firefly border moons

### Ancient / Precursor

- **Silhouette**: Organic curves, non-human geometry, unsettling symmetry
- **Core**: Grown rather than built — no visible seams or bolts
- **Signature parts**: Crystalline spires, resonance chambers, bioluminescent veins, void-dark surfaces
- **Mood**: Deep purple-blue hull with amber/gold bioluminescent accents. Silent. Alien.
- **Reference**: Mass Effect Citadel (scale), Halo Forerunner structures (alien geometry)

---

## 4. Faction Visual Identity

### Material language per faction

Each faction has a primary color, a material philosophy, and signature parts
that make their stations instantly recognizable from silhouette alone.

#### Concord (Blue — Order, Institutional)

- **Hull color**: Steel blue-gray, clean, well-maintained
- **Accent**: Bright blue emissive strips, standardized beacon patterns
- **Material**: Smooth panels, uniform riveting, regulation-compliant
- **Signature parts**: Symmetrical arm layout, relief convoy docking, medical bay modules, broadcast arrays
- **Silhouette feel**: Government building in space — orderly, predictable, trustworthy
- **Station label style**: Official designations (e.g., "Concord Station C-7")

#### Chitin Syndicates (Amber — Adaptation, Information)

- **Hull color**: Dark bronze/chitin-brown with amber highlights
- **Accent**: Amber/orange emissive, flickering market displays
- **Material**: Compound-eye faceted panels, beetle-shell layered plating, iridescent surfaces
- **Signature parts**: Sensor domes (many, small, clustered), trading floor modules, probability display arrays
- **Silhouette feel**: Casino meets hive — buzzing, alert, covered in eyes
- **Station label style**: Exchange designations (e.g., "Synth-Bazaar 12")

#### Weavers (Green — Structure, Patience)

- **Hull color**: Dark forest green, heavy, fortress-like
- **Accent**: Green emissive along structural stress lines (like a spider web glowing)
- **Material**: Thick layered armor, visible load-bearing geometry, silk-pattern surface texture
- **Signature parts**: Web-pattern structural lattice, anchor cables between modules, reinforced docking clamps, ambush bays (concealed turrets)
- **Silhouette feel**: Fortress — heavy, grounded even in space, nothing decorative
- **Station label style**: Anchor designations (e.g., "Threadhold 9-West")

#### Valorin Clans (Red — Expansion, Frontier)

- **Hull color**: Rust red and bare metal, patched, heterogeneous
- **Accent**: Red warning lights, crude but functional
- **Material**: Visible welds, mismatched panel sizes, functional ugliness, multiple hull generations visible
- **Signature parts**: Many small docking ports (warren-like), external cargo nets, distributed hab pods, escape tunnels
- **Silhouette feel**: Favela in space — sprawling, organic growth, no master plan
- **Station label style**: Informal (e.g., "Red Warren", "Cache 77")

#### Drifter Communion (Violet — Understanding, Shimmer)

- **Hull color**: Pale gray-violet, minimal, open architecture
- **Accent**: Soft violet/white bioluminescence, crystal formations
- **Material**: Smooth curves, minimal hard edges, translucent panels in places, shimmer-crystal inlays
- **Signature parts**: Open docking (no enclosed bays — welcoming), meditation spires, resonance antennae, crystal gardens
- **Silhouette feel**: Temple in space — peaceful, inviting, strange
- **Station label style**: Experiential (e.g., "Stillpoint", "The Listening")

#### Ancient / Accommodation (Pre-faction)

- **Hull color**: Deep purple-black, near-void, absorbs light
- **Accent**: Amber-gold bioluminescent veins that pulse slowly
- **Material**: No visible construction — surfaces flow like grown crystal. Non-Euclidean curves. Unsettling perfection.
- **Signature parts**: Crystalline spires, resonance chambers with visible energy, geometry that shifts at different viewing angles, docking is "absorbed" not "clamped"
- **Silhouette feel**: Something that was here before anyone else. Awe and unease.
- **Station label style**: None — player names them, or they have designation codes (e.g., "ACC-7741")

---

## 5. Starter System Station (Hand-Tuned)

The player's first station must be perfect. It teaches what a station IS.

- **Archetype**: Trade Hub (Concord-owned or neutral)
- **Tier**: Hub (1.0x scale, ~100u diameter)
- **Requirements**:
  - Docking bay clearly visible and inviting from approach angle
  - Running lights guide the player toward the dock
  - At least one NPC ship visibly docked or departing (station feels alive)
  - Faction banner/color immediately readable
  - Silhouette distinct from planets and stars at 200u+ distance
- **What the player learns here**: Stations are big. Docking is spatial. Factions own things.

---

## 6. Art Asset Requirements

### Per-archetype part list (minimum viable)

| Part | Variants needed | Notes |
|------|----------------|-------|
| Core hull | 6 (one per archetype) | Largest piece, defines silhouette |
| Docking bay | 3 (open, enclosed, ancient) | Must frame player ship to show scale |
| Arm/spoke | 4 (industrial pipe, military angular, trade corridor, organic) | Length varies per tier |
| Ring segment | 3 (habitat, cargo, defense) | Curved, tileable around core |
| Tower/mast | 4 (command, antenna, sensor, spire) | Vertical accent |
| Turret | 2 (single, double) | Already have Kenney versions |
| Dish antenna | 2 (standard, large array) | Already have Kenney versions |
| Surface greeble sheet | 3 (industrial, military, clean) | Normal-map panels, tileable |
| Accent light strip | 1 (recolored per faction via shader) | Emissive, faction-tinted |
| Functional module | 5 (refinery, solar, lab, cargo, medical) | Attach to arm hardpoints |

**Total unique meshes needed**: ~33 parts
**Reuse strategy**: Faction identity comes from color + material + assembly pattern, not unique meshes per faction. Ancient stations are the exception (fully unique geometry).

### Sourcing priority

1. **Turrets, dishes, some greeble**: Kenney Space Kit (already in project)
2. **Core hulls, arms, rings, bays**: AI generation (Meshy/Tripo) or marketplace purchase
3. **Ancient parts**: AI generation with heavy art direction (unique aesthetic)
4. **Surface detail**: Normal map sheets (can be generated or painted)

---

## 7. Assembly Pipeline

### Procedural assembly (runtime)

```
1. Select archetype based on station economic role
2. Select core hull mesh
3. Attach arms (count = f(tier), angle = 360/count + jitter)
4. Attach docking bays to arm ends
5. Optionally add ring (capital stations)
6. Add tower to core top
7. Populate surface greeble via shader or instanced meshes
8. Apply faction material (color, roughness, emission)
9. Add accent lighting strips (faction color)
10. Add functional modules to hardpoints based on station role
```

### Hand-placed assembly (special stations)

- Starter system station
- Haven starbase (5 visual tiers — see haven_starbase_v0.md)
- Story-critical ancient ruins
- Faction capital stations (one per faction)

---

## 8. Lighting & Atmosphere

### Station self-lighting

Every station is a light source in the void. This is critical for readability
and mood.

| Light type | Purpose | Color |
|-----------|---------|-------|
| Docking bay flood | Guide player to dock, show interior scale | Warm white (trade), red (military), blue (Concord) |
| Accent strip emission | Faction identity from distance | Faction primary color, 1.5-2.5x emission |
| Window rows | Station feels inhabited | Warm yellow-white, subtle flicker |
| Beacon/warning | Navigation, territorial claim | Faction accent color, slow pulse |
| Work lights (industrial) | Role identity for refineries | Orange/amber, harsh, directional |

### Ambient occlusion zones

Deep recesses between modules should be dark. This sells the "assembled from
parts" look and adds visual depth without extra geometry.

---

## 9. Station Life — Ambient Traffic & Defense

Stations must feel like defended, inhabited places — not floating props.

### Decorative docked/departing ships

1-3 cosmetic ship models spawn with each station (not sim-driven NPC fleets).
These are visual set dressing parented to the station node.

| Slot | Behavior | Ship size | Purpose |
|------|----------|-----------|---------|
| **Docked** | Static, nestled in/near docking bay | Frigate (5-7u) | Scale reference — tiny ship against massive hull |
| **Departing** | Slow linear drift outward (~2u/s) + engine trail | Frigate (5-7u) | Station feels active, things are happening |
| **Approaching** | Slow linear drift inward (~1.5u/s) | Freighter (8u) | Paired with departing for traffic flow |

Ships use the station owner's faction model. A Concord station has Concord
frigates docked. A Valorin outpost has battered red haulers.

Despawn at ~40u from station, respawn on timer (30-60s). No collision, no sim
interaction. Pure visual.

### Defense perimeter

Every station projects a defended zone. This is both gameplay (NPC patrol AI
already exists) and visual.

#### Visual defense elements

| Element | Tier 0 (Outpost) | Tier 1 (Hub) | Tier 2 (Capital) |
|---------|-----------------|-------------|-----------------|
| **Patrol ships** | 1 fighter, tight orbit | 2 fighters + 1 frigate, wider patrol | 4 fighters + 2 frigates, layered perimeter |
| **Turret hardpoints** | 1, on core hull | 2-4, on arms and core | 6-8, on arms, ring, and core |
| **Defense ring** | None | None | Optional ring with turret mounts |
| **Beacon buoys** | None | 2, marking approach lane | 4, marking perimeter + approach |

#### Patrol orbit patterns (cosmetic)

Patrol ships orbit the station on simple elliptical paths. Not the sim-driven
NPC fleet — these are decorative like the docked ships. They sell "this place
is guarded."

| Pattern | Description | Used by |
|---------|-------------|---------|
| **Tight orbit** | 15-20u radius, fast, close to hull | Fighters at outposts |
| **Wide patrol** | 30-50u radius, slower, perimeter sweep | Frigates at hubs |
| **Figure-8** | Crosses between two arms or bays | Capital station fighter pairs |
| **Sentry post** | Stationary + slow drift at perimeter point | Gate-side guard |

#### Faction defense personality

| Faction | Defense style | Visual read |
|---------|-------------|-------------|
| **Concord** | Orderly patrols, symmetric formation, by-the-book | Clean lines, predictable movement |
| **Chitin** | Many small scouts, erratic movement, eyes everywhere | Swarm of sensors, jittery orbits |
| **Weavers** | Few but heavy ships, minimal movement, ambush-ready | Still and menacing, turrets prominent |
| **Valorin** | Fast loose patrols, more ships than warranted | Chaotic energy, red streaks everywhere |
| **Communion** | Minimal defense, open posture, a single watcher | Almost undefended — intentionally welcoming |
| **Ancient** | Lattice drones (autonomous, non-faction) | Alien movement patterns, geometric formation |

---

## 10. What We Are NOT Doing

- No station interiors (camera is always external, top-down)
- No destructible stations (stations are permanent fixtures)
- No station construction animation (Haven tiers swap whole models)
- No per-station unique models (faction + archetype + tier = enough variety)
- No Kenney kitbash for core structure (pieces are too small-scale and interior-focused)
  - Kenney turrets and dishes ARE useful as attachable detail parts

---

## 11. AI 3D Generation Prompt Guide

### Which tool for which part

| Tool | Best for | Why | Cost |
|------|----------|-----|------|
| **Meshy** | Station hulls, arms, rings, mechanical modules | Structure control, polycount slider, reliable for hard-surface | Free tier: 5/day, Pro: $20/mo |
| **Tripo** (Smart Mesh) | Ships, ancient/organic parts, anything curved | Clean topology out of box, good with organic forms | Free tier: limited, Pro: $10/mo |
| **Sloyd** | Batch consistency within a style family | Custom Style feature: upload reference image, all generations match it | Free tier: 20/mo, Pro: $15/mo |

**Recommended workflow**: Generate your FIRST piece (the Trade Hub core) on
Meshy. If you like it, screenshot that result and use it as Sloyd's Custom
Style reference image to generate all remaining station parts in the same
visual language. For ships and ancient parts, use Tripo.

### Solving the consistency problem

This is the #1 risk with AI generation. Each generation is independent — parts
from separate sessions will look like they belong to different games.

**Strategy: Reference-image anchoring**

1. Generate the hero asset first (Trade Hub core hull) using text-to-3D on Meshy
2. Pick the best result — this becomes your **style anchor**
3. Screenshot it from multiple angles
4. Use that screenshot as input for image-to-3D on all subsequent parts
5. Every part inherits the same material language, panel density, edge style

**Strategy: Batch by visual family**

Generate parts in these groups. All parts within a group should be generated
in the same session, using the same style reference:

| Batch | Parts | Session goal |
|-------|-------|-------------|
| **Batch 1: Standard station** | Trade Hub core, docking bay, arm, ring, tower | Establish the baseline human station look |
| **Batch 2: Station modules** | Refinery drum, solar array, cargo pods, medical pod, lab pod, turrets, dishes, beacon buoy | Detail parts matching Batch 1 |
| **Batch 3: Variant cores** | Industrial, Military, Research, Frontier cores | Same material language as Batch 1, different silhouettes |
| **Batch 4: Ancient** | Ancient core, crystalline spire, resonance chamber, organic arm, ancient docking void | Completely different style — generate as its own family |
| **Batch 5: Concord ships** | Fighter, frigate, cruiser | One faction's full fleet in one session |
| **Batch 6: Chitin ships** | Fighter, frigate, cruiser | Same process, different faction |
| **Batch 7: Weavers ships** | Fighter, frigate, cruiser | Same process |
| **Batch 8: Valorin ships** | Fighter, frigate, cruiser | Same process |
| **Batch 9: Communion ships** | Fighter, frigate, cruiser | Same process |
| **Batch 10: Ancient ships** | Seeker, Bastion, Threshold | Match Batch 4's organic style |

**Strategy: Material unification in-engine**

Even if AI generates inconsistent textures, we override materials in Godot
per faction (Section 4). So the SILHOUETTE matters more than the texture.
Pick for shape, re-skin in engine.

### Prompt formula

All prompts follow this structure. Place the most important terms first — AI
weights early tokens more heavily.

```
[Style] [Subject], [Key shape/silhouette descriptors], [Material/surface],
[Detail features], [Technical spec]. Negative: [exclusions]
```

### Tool settings

| Setting | Station parts | Ships | Small detail parts |
|---------|--------------|-------|-------------------|
| **Mode** | Low Poly (game-ready) | Low Poly (game-ready) | Low Poly (game-ready) |
| **Target polycount** | 3000-8000 tris | 1500-4000 tris | 500-1500 tris |
| **Texture resolution** | 2K (2048×2048) | 2K (2048×2048) | 1K (1024×1024) |
| **Texture richness** | High | High | Medium |
| **Topology** | Quads preferred | Quads preferred | Triangles OK |
| **PBR maps** | Diffuse, Normal, Roughness, Metallic, Emissive | Same | Diffuse, Normal, Roughness |
| **Export format** | GLB | GLB | GLB |

---

### Complete part prompt catalog

Every part needed for the full station and ship system, organized by generation
batch. Generate within a batch in a single session for consistency.

---

#### BATCH 1 — Standard station structure (Meshy, then Sloyd with reference)

##### Trade Hub core (~100u assembled diameter)
```
Low-poly sci-fi space station central hub, wide flat disc shape with octagonal
symmetry, heavy docking clamps on perimeter, recessed panel lines across hull
surface, antenna cluster on top face, docking bay openings on 4 cardinal faces,
industrial gray metal with subtle blue-white panel lighting, PBR metallic
surface with weathering, game-ready asset. Negative: no interior geometry, no
text, no logos, no organic shapes, no floating parts.
```

##### Docking bay module
**Scale note**: Opening must be ~8-10u wide (2x player ship width). This is
what sells station scale.
```
Low-poly sci-fi space station docking bay module, rectangular open hangar
frame 2x wider than tall, guide light strips along entry edges, magnetic clamp
rails on interior floor, structural cross-bracing on sides, industrial gray
metal with warm white interior flood lighting, PBR metallic surface,
game-ready asset. Negative: no interior detail beyond floor rails, no text, no
ships inside.
```

##### Arm / spoke segment
**Length**: Generate at ~20u length, we scale per tier. Must tile cleanly.
```
Low-poly sci-fi space station arm segment, elongated rectangular truss
structure with corridor spine, cable runs along exterior, attachment hardpoints
every 5 units, structural cross-bracing, connection flanges at both ends,
industrial gray metal, PBR surface with panel line normal map detail,
game-ready asset. Negative: no interior, no text, no curves, no organic shapes.
```

##### Ring segment
**Curvature**: Generate as a 45-degree arc. 8 segments = full ring.
```
Low-poly sci-fi space station ring segment, 45-degree curved arc section,
habitat windows along inner face, hull panels along outer face, structural
ribbing at both cut ends for tiling, connection flanges, medium gray metal
with rows of warm-white window lights on inner curve, PBR metallic surface,
game-ready asset. Negative: no interior geometry, no text, no flat sections.
```

##### Command tower
```
Low-poly sci-fi space station command tower, vertical cylindrical body with
observation deck ring near top, antenna array on peak, docking beacon light at
apex, structural base flange for mounting, panel detail on hull surface, dark
gray metal with blue-white accent lighting on observation ring, PBR metallic
surface, game-ready asset. Negative: no interior, no text, no organic shapes.
```

---

#### BATCH 2 — Station detail parts (same session as Batch 1 or use Batch 1 reference)

##### Refinery drum
```
Low-poly sci-fi refinery processing drum, horizontal cylinder with pipe
connections at both ends, pressure gauge panels on surface, exhaust port on
top, structural mounting bracket on bottom, dark metal with orange hazard
markings, industrial weathered PBR surface, game-ready asset.
```

##### Solar array
```
Low-poly sci-fi solar panel array, flat rectangular panel grid on articulated
arm, 4x2 panel layout with visible cell grid pattern, pivot joint at base,
dark blue-black panels with silver frame, reflective PBR surface, game-ready
asset.
```

##### Cargo pod cluster
```
Low-poly sci-fi cargo container cluster, 6 rectangular containers grouped in
2x3 grid on mounting frame, each container slightly different color tone,
magnetic clamp mounts, cargo markings on faces, mixed gray-brown metal tones,
utilitarian PBR surface, game-ready asset.
```

##### Medical module
```
Low-poly sci-fi space station medical bay module, compact rectangular pod with
red cross marking on roof, docking port on one end, observation window on
front face, life support vents on sides, clean white hull with red accent
strips, sterile PBR metallic surface, game-ready asset. Negative: no interior
detail, no text beyond cross symbol.
```

##### Research lab module
```
Low-poly sci-fi space station laboratory module, cylindrical pod with
transparent dome viewport on top, sensor antenna array on one end, data cable
conduits along hull, equipment access hatch on side, white hull with blue
accent panels, clean PBR metallic surface, game-ready asset. Negative: no
interior detail, no text.
```

##### Station turret (single mount)
```
Low-poly sci-fi space station defense turret, compact rotating platform base
with single barrel cannon, armored housing around barrel root, targeting sensor
dome on top, mounting bracket on bottom for hull attachment, dark gunmetal with
red targeting laser emitter tip, military PBR metallic surface, game-ready
asset. Negative: no interior, no text, no organic shapes.
```

##### Station turret (double mount)
```
Low-poly sci-fi space station heavy defense turret, wide rotating platform
base with twin parallel barrel cannons, heavy armored housing, sensor dome
between barrels, reinforced mounting bracket on bottom, dark gunmetal with
red accent on barrel tips, battle-worn PBR metallic surface, game-ready asset.
Negative: no interior, no text.
```

##### Sensor dish (standard)
```
Low-poly sci-fi satellite dish antenna, concave circular dish on articulated
arm mount, feed horn at dish focal point, cable conduit along arm, structural
base plate for hull mounting, light gray dish with silver metallic frame, PBR
reflective surface, game-ready asset. Negative: no interior, no text.
```

##### Sensor dish (large array)
```
Low-poly sci-fi large sensor array, 3 concave dishes in triangular cluster on
heavy structural frame, each dish with individual feed horn, central processing
unit between dishes, heavy base mount, industrial gray with blue accent
lighting on processing unit, PBR metallic surface, game-ready asset.
```

##### Beacon buoy
```
Low-poly sci-fi navigation beacon buoy, vertical cylindrical body with
flashing light dome on top, faction banner mounting ring around middle,
stabilization fins at base, antenna spike below, dark gray body with bright
emissive dome (color set in-engine per faction), PBR metallic surface,
game-ready asset. Negative: no text, no interior.
```

---

#### BATCH 3 — Variant station cores (use Batch 1 Trade Hub as style reference)

##### Industrial/Refinery core
```
Low-poly sci-fi industrial space station core module, asymmetric cylindrical
body with offset machinery block, exposed pipe bundles along hull, large
exhaust vents on rear face, heavy structural ribbing, ore processing drum
mounted laterally, dark gunmetal with orange hazard accent strips, scratched
worn PBR metal, game-ready asset. Negative: no interior, no text, no clean
surfaces, no symmetry.
```

##### Military/Fortress core
```
Low-poly sci-fi military space station core, compact armored octagonal hull,
thick angular armor plating with beveled edges, recessed weapon hardpoints on
all faces, minimal windows, heavy blast doors on docking faces, command tower
stub on top, dark charcoal metal with red warning strips, battle-scarred PBR
surface, game-ready asset. Negative: no interior, no text, no organic curves,
no fragile parts.
```

##### Research/Observatory core
```
Low-poly sci-fi research space station core, small central cylinder body with
4 extended instrument boom attachment points, clean white hull panels, large
observation dome on top face, solar panel mounting brackets on sides, delicate
structural framework visible, bright white with blue accent panels, clean PBR
metal, game-ready asset. Negative: no interior, no text, no heavy armor, no
weapons.
```

##### Frontier Outpost core
```
Low-poly sci-fi frontier outpost, single repurposed cargo container hull with
bolted-on extensions, mismatched hull panels of different metal tones, antenna
mast welded to top, external cargo nets and strapped containers, patched hull
breach visible, rust-brown and bare metal with flickering yellow work light
spots, weathered scratched PBR surface, game-ready asset. Negative: no
interior, no text, no clean surfaces, no uniformity.
```

---

#### BATCH 4 — Ancient / Precursor (Tripo, separate style family — no human reference)

##### Ancient station core
```
Low-poly alien space station core, organic flowing geometry with bilateral
symmetry, no visible seams bolts or rivets, smooth curved surfaces with
crystalline ridge lines, resonance chamber void in center, spire attachment
points at cardinal directions, deep purple-black hull absorbing light with
amber-gold glowing vein patterns on surface, bioluminescent PBR emissive
surface, game-ready asset. Negative: no human architecture, no right angles,
no industrial elements, no text, no mechanical joints.
```

##### Crystalline spire
```
Low-poly alien crystalline spire, tall tapering organic crystal formation,
faceted surfaces catching light, internal amber-gold glow visible through
translucent edges, smooth base mounting surface, deep purple-black crystal
body with amber bioluminescent veins, emissive PBR surface, game-ready asset.
Negative: no human elements, no metal, no mechanical parts, no text.
```

##### Resonance chamber module
```
Low-poly alien resonance chamber, hollow organic torus shape with internal
energy visible through lattice openings, crystalline nodes at cardinal points
around ring, smooth flowing connection arms at top and bottom, deep purple-black
with intense amber-gold energy glow from interior, bioluminescent PBR emissive
surface, game-ready asset. Negative: no human architecture, no right angles,
no pipes, no text.
```

##### Ancient organic arm
```
Low-poly alien space station arm, elongated organic tendril structure with
smooth flowing curves, crystalline ridge along dorsal surface, amber-gold
bioluminescent vein patterns, connection bulb at each end, deep purple-black
body, organic PBR surface with subtle iridescence, game-ready asset. Negative:
no human architecture, no straight lines, no bolts, no text.
```

##### Ancient docking void
```
Low-poly alien docking structure, organic opening in smooth curved hull,
petal-like guide surfaces around opening, amber-gold bioluminescent guide
lights lining the opening edges, interior depth visible as dark void,
connection surface flows seamlessly from surrounding hull, deep purple-black
with amber accents, organic PBR surface, game-ready asset. Negative: no
mechanical clamps, no human elements, no right angles, no text.
```

---

#### BATCH 5-9 — Faction ships (Tripo, one batch per faction)

##### Critical constraint: top-down spinning readability

Ships in this game are viewed from a **fixed top-down camera** and **rotate
during combat** (spin mechanic, visible RPM). This means:

1. **Silhouette must read from directly above** — the top-down profile IS the
   ship's identity. No detail should be wasted on the underside.
2. **Asymmetric features help** — a wing, fin, or weapon mount that breaks
   symmetry makes rotation visible and readable.
3. **Front/back must be distinguishable** from top-down — the player needs to
   know which way the ship points while it spins. A clear bow/stern shape,
   engine glow at rear, or tapered nose solves this.
4. **No thin vertical fins** — from top-down these are invisible lines.
   Horizontal spread matters, vertical height does not.
5. **Engine glow zone at rear** — the emissive area where engine trail particles
   spawn. Must be clearly at the back of the top-down silhouette.

##### Ship scale reference

| Ship class | Length (u) | Top-down footprint | Role |
|-----------|-----------|-------------------|------|
| Fighter | 3-4u | Narrow, fast silhouette | Player starter, escorts, patrol decoration |
| Frigate | 5-7u | Medium, balanced | Player mid-game, NPC traders, docked decoration |
| Cruiser | 8-12u | Wide, imposing | Player late-game, NPC patrol |
| Capital | 15-25u | Massive, reads as a small station | Faction flagships, ancient hulls |

##### Faction ship prompt patterns

| Faction | Shape language | Material keywords | Distinguishing feature (top-down) |
|---------|--------------|-------------------|----------------------------------|
| Concord | Clean, symmetric, official | Brushed steel-blue, regulation white markings | Paired engine nacelles, shield emitter dome |
| Chitin | Faceted, compound-eye panels, beetle-like | Dark bronze, iridescent amber accents | Many small sensor bumps across hull top |
| Weavers | Heavy, angular, fortress-like | Dark forest green, matte, thick plating | Wide armored prow, web-lattice wing struts |
| Valorin | Rough, kitbashed, fast | Rust red, mismatched panels, visible welds | Oversized engines relative to hull, cargo pods strapped externally |
| Communion | Smooth, curved, minimal | Pale violet-gray, crystal inlays | Open wing structure, crystal formations on dorsal ridge |
| Ancient | Organic, alien, flowing | Deep purple-black, amber bioluminescent veins | Non-human curves, no visible mechanical joints |

##### BATCH 5 — Concord fleet (Tripo)

**Concord Fighter**:
```
Low-poly sci-fi military fighter spaceship, compact symmetric arrowhead hull,
tapered nose, paired small engine nacelles at rear with blue glow zones,
dorsal sensor bump, stubby weapon pylons on wing edges, brushed steel-blue
hull with white regulation stripe, PBR metallic surface, optimized for
top-down view with clear bow-stern distinction, game-ready asset. Negative:
no interior, no text, no organic shapes, no thin vertical fins, no landing gear.
```

**Concord Frigate**:
```
Low-poly sci-fi military frigate spaceship, clean symmetric hull design,
tapered nose with sensor array, paired engine nacelles at rear with blue glow
zones, shield emitter dome on dorsal center, wing-mounted weapon hardpoints,
official regulation markings, brushed steel-blue hull with white panel accents,
PBR metallic surface with subtle weathering, optimized for top-down view with
clear bow-stern distinction, game-ready asset. Negative: no interior, no text,
no organic shapes, no thin vertical fins, no landing gear.
```

**Concord Cruiser**:
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

##### BATCH 6 — Chitin Syndicate fleet (Tripo)

**Chitin Fighter**:
```
Low-poly sci-fi alien fighter spaceship, compact faceted beetle-shell hull,
compound-eye sensor cluster on dorsal nose, swept angular wings with sharp
edges, single wide exhaust port at stern with amber glow, iridescent dark
bronze hull panels, PBR metallic surface with iridescent sheen, optimized for
top-down view with readable rotation, game-ready asset. Negative: no interior,
no text, no human cockpit, no thin vertical fins.
```

**Chitin Frigate**:
```
Low-poly sci-fi alien trader spaceship, faceted beetle-shell hull with
compound-eye sensor dome cluster on dorsal surface, wide cargo bay midsection,
asymmetric antenna boom on starboard side, dual exhaust ports at stern with
amber glow, iridescent dark bronze hull panels with amber accent lighting,
PBR metallic surface with iridescent sheen, optimized for top-down view with
readable rotation, game-ready asset. Negative: no interior, no text, no human
cockpit, no thin vertical fins.
```

**Chitin Cruiser**:
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

##### BATCH 7 — Weavers fleet (Tripo)

**Weavers Fighter**:
```
Low-poly sci-fi heavy fighter spaceship, compact angular hull with thick armor
plating, wide armored prow with reinforced ram edge, web-lattice wing struts
on both sides, single heavy engine at rear with green glow, dark forest green
matte hull with visible structural stress lines, heavy PBR metallic surface,
optimized for top-down view with clear facing direction, game-ready asset.
Negative: no interior, no text, no curves, no thin vertical fins.
```

**Weavers Frigate**:
```
Low-poly sci-fi armored frigate spaceship, heavy angular hull with layered
thick plating, wide armored prow tapering to reinforced point, web-lattice
structural framework visible between hull plates on wings, concealed weapon
bays along flanks, dual heavy engines at rear with green glow, dark forest
green matte hull with stress-line accents, fortress-like PBR metallic surface,
optimized for top-down view, game-ready asset. Negative: no interior, no text,
no organic curves, no thin vertical fins, no fragile parts.
```

**Weavers Cruiser**:
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

##### BATCH 8 — Valorin Clans fleet (Tripo)

**Valorin Fighter**:
```
Low-poly sci-fi kitbashed fighter spaceship, compact rough hull with
mismatched panel sizes, oversized single engine at rear relative to small body
with orange-red glow, bolted-on weapon pod on one wing, patched hull plating,
rust red and bare metal hull with visible welds, weathered scratched PBR
surface, optimized for top-down view with clear asymmetry for spin readability,
game-ready asset. Negative: no interior, no text, no clean surfaces, no
uniformity, no thin vertical fins.
```

**Valorin Frigate**:
```
Low-poly sci-fi kitbashed freighter spaceship, rough asymmetric hull with
bolted-on cargo pods strapped to starboard side, oversized dual engines at
rear with orange-red glow, mismatched hull panels of different metal tones,
antenna mast welded to port side, external fuel tank lashed to hull, rust red
and bare metal with visible welds and patches, weathered PBR surface,
optimized for top-down view with readable rotation, game-ready asset.
Negative: no interior, no text, no clean surfaces, no thin vertical fins.
```

**Valorin Cruiser**:
```
Low-poly sci-fi kitbashed cruiser spaceship, large rough hull assembled from
multiple repurposed ship sections, 3 mismatched oversized engines at rear with
orange-red glow, cargo container clusters strapped to hull sides, multiple
welded-on weapon platforms, antenna forest on dorsal surface, rust red with
patches of bare metal and yellow hazard paint, heavily weathered PBR surface,
optimized for top-down view, game-ready asset. Negative: no interior, no text,
no uniformity, no thin vertical fins, no elegance.
```

##### BATCH 9 — Drifter Communion fleet (Tripo)

**Communion Fighter**:
```
Low-poly sci-fi spiritual scout spaceship, sleek minimal hull with smooth
curves and no hard edges, open lattice wing structure on both sides, single
crystal formation on dorsal ridge, gentle engine glow at rear in soft violet,
pale gray-violet hull with crystal inlay accents, smooth PBR surface with
subtle luminescence, optimized for top-down view with readable facing,
game-ready asset. Negative: no interior, no text, no weapons visible, no
angular shapes, no thin vertical fins.
```

**Communion Frigate**:
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

**Communion Cruiser**:
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

##### BATCH 10 — Ancient relic ships (Tripo, match Batch 4 style)

**Seeker (relic frigate/clipper)**:
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

**Bastion (relic dreadnought)**:
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

**Threshold (relic cruiser, Phase 4)**:
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

### Post-generation checklist

After generating any asset, verify before importing to Godot:

- [ ] Polycount within budget (check tri count in Blender/viewer)
- [ ] Model is watertight (no holes, no inverted normals)
- [ ] UV mapping present and reasonable (no extreme stretching)
- [ ] PBR maps exported (Diffuse, Normal, Roughness, Metallic minimum)
- [ ] Model oriented correctly: **-Z is forward, +Y is up** (Godot convention)
- [ ] Scale is sane: 1 Blender unit = 1 Godot unit
- [ ] No embedded lights or cameras in GLB
- [ ] For ships: **top-down silhouette is distinctive** and front/back are clear
- [ ] For ships: **rear engine zone** is identifiable for particle emitter placement
- [ ] For stations: **attachment faces are flat** with clear connection geometry
- [ ] **Style check**: hold new part next to Batch 1 reference — same visual family?

---

## 12. Execution Checklist

**See `station_asset_checklist_v0.md`** — the step-by-step checkable guide
with every prompt, filename, and polycount target inline. That file is the
"do this now" document. This file (station_visual_design_v0.md) is the
design reference for why things are the way they are.

---

## 13. Success Criteria

When this system is working, a player should be able to:

1. **Identify faction ownership from silhouette alone** at 200u+ distance
2. **Feel small** when approaching a capital station
3. **Distinguish trade hub from military station** without reading labels
4. **Notice visual difference** between every system they visit (no two stations identical)
5. **Feel awe** approaching an ancient station (completely alien visual language)
6. **Navigate to docking bay** by following visual cues (lights, bay geometry)
