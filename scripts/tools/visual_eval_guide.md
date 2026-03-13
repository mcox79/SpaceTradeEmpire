# Visual Evaluation Guide — Space Trade Empire

Use this guide when evaluating screenshots from the visual sweep bot.
Evaluate each screenshot against ALL applicable sections below.
Rate each category: PASS / NEEDS_WORK / FAIL with specific notes.

---

## 0. VISUAL VOCABULARY — What Things Look Like

You MUST read this section before evaluating any screenshot. LLM vision models
frequently misidentify game objects. Use this reference to correctly identify
objects, then judge them against BEST-IN-CLASS standards from reference games.

### Player Ship
- **Kenney Space Kit model** — a small detailed spacecraft (~2-4 unit wingspan)
- **Blue engine glow**: 2-3 bright cyan/blue flame trails behind the ship (always visible when flying)
- Usually center-left of frame in flight mode
- The ship is SMALL relative to planets/stations — this is by design (sense of scale)

### Stations
- **Hub-and-ring model**: central cylindrical hub (r=1.0) with an outer ring (r=2.0)
- Positioned 8 units from a planet, so they appear near planets
- Have Label3D name tags (white text, may be small at distance)
- Dark navy dock menu frame (StyleBoxFlat) when docked

### Stars
- **Procedural spheres** with emission glow, class-scaled:
  - G-type: warm yellow, 1.0x size (most common, guaranteed at player start)
  - M-type: deep red, 0.6x size
  - O-type: blue-white, 1.8x size
- Stars have directional light that tints the whole scene
- 20% chance of a binary companion star nearby

### Planets
- **3D Planet Generator addon** — textured spheres with atmosphere shader
- Types: rocky, gas giant, ice, lava — each visually distinct
- Self-rotating on spinning pivot nodes
- 0-3 moons per planet (small spheres nearby)
- Distributed at 20-38 units from star (G-type reference)

### Asteroid Belt
- Present in ~60% of systems, at ~45 units from star
- Mix of Sphere, Box, and Cylinder shapes (hash-based variety)
- Individual rocks are 1-4 units in size
- Forms an arc/ring pattern — NOT a random scatter

### NPC Ships
- **Kenney Quaternius models** — detailed spacecraft, NOT primitive shapes
- CharacterBody3D with Label3D status display
- Role indicators: T=Trader (gold), H=Hauler (gray), P=Patrol (blue)
- May show HP bar (thin green box) when damaged
- "HOSTILE" label (red) when aggressive toward player

### Lane Gates
- Located at ~90 units from star (outer edge of system)
- Kenney gate_complex.glb model
- Label3D with direction arrow and destination name

### HUD Layout (top-left panel, dark navy background)
- Credits, Cargo, System name, Ship state
- Hull bar (orange/red) + Shield bar (cyan/blue) — thin progress bars
- Security label below (colored by band: green/yellow/orange/red)
- Research label further below (cyan when active, gray when idle)
- Fuel label (warns LOW in red)
- Mission panel (gold title + white objective, hidden when no mission active)
- Combat label (red "COMBAT" during fights)
- Zone G bottom bar: risk meters left, system status center, keybind hints bottom

### First Officer Panel (left sidebar, below status)
- Appears after onboarding gate (2+ nodes visited)
- Shows FO name, status, scrollable dialogue history (teal text)
- Promotion section with 3 candidates (Analyst Maren, Veteran Dask, Pathfinder Lira)
- **Suppressed during**: docked, in transit, when overlay/dashboard is open

### Warp Transit HUD (top-center during lane transit)
- Appears during IN_LANE_TRANSIT state
- "WARP TRANSIT > [System Name]" header text
- Cyan progress bar showing transit completion percentage
- ETA label (left): "ETA: X.Xs" counting down to "Arriving..."
- Distance label (right): remaining distance in ly/units
- Additionally: center-bottom label "→ [Destination Name]" from main HUD

### Edgedar Overlay (screen-edge arrows, flight mode only)
- Blue arrows: lane gates (off-screen navigation targets)
- Red arrows: hostile fleets
- Gold arrows: quest/mission targets
- Green arrows: stations

### Combat HUD (bottom-center during combat)
- Stance indicator label
- 4 zone armor progress bars: Fore / Port / Stbd / Aft (cyan fill with percentage)

### Warp/Transit
- Lane transit: warp tunnel VFX wraps player (cylinder mesh + speed streak particles + glow ring)
- Transit HUD: "WARP TRANSIT > [Dest]" + progress bar + ETA + distance readout at top-center
- Destination label: "→ [System Name]" at center-bottom of screen during transit
- Warp arrival VFX: expanding cyan sphere, camera shake + flash
- Arrival cinematic: first visit = full flyby cinematic (~5s orbital sweep); return visit = fast descent (~2.5s)

### Galaxy Map
- Strategic altitude (auto-fit to visible nodes, ~3000-8000u) looking down
- Camera auto-centers on the centroid of all visible star nodes (not player position)
- Dark navy-purple background plane for depth (not pure black)
- "GALAXY MAP (TAB to close)" header label at top-center
- Nodes as beacon spheres (25u radius, colored by faction territory)
- Lane edges between connected systems (persistent lines)
- "YOU" indicator (green pulsing beacon) at player location
- Faction territory fill discs (semi-transparent, colored per faction)
- Faction name labels (Label3D)
- Fog-of-war: unvisited neighbors shown as RUMORED ("???" dimmed), truly unknown nodes HIDDEN

### BEST-IN-CLASS REFERENCES — Judge Against These

When evaluating, compare what you see to these genre-leading examples:

**Solar System View — Reference: Everspace 2, Stellaris, Endless Space 2**
- Stars should have visible corona/bloom, not flat circles
- Planets should have atmospheric haze, shadow terminator, visible surface detail
- Space should feel vast but populated — dust, particles, distant objects
- Lighting should create mood: warm systems feel inviting, cold systems feel desolate

**HUD — Reference: Elite Dangerous, Starfield, EVE Online**
- Health/shield bars should be immediately readable at a glance (thick, high contrast)
- Information should have clear visual hierarchy (size, color, position)
- HUD should feel integrated into the game world, not plastered on top
- Status should use icons + color, not just text labels

**Dock/Trade Menu — Reference: X4 Foundations, Elite Dangerous, Starsector**
- Market data should be scannable in a table (clear columns, aligned numbers)
- Tabs should have obvious active/inactive states
- Station identity should be immediately clear (name, faction, services)

**Galaxy Map — Reference: Stellaris, Endless Space 2, Mass Effect**
- Lane-based topology should be instantly readable
- Player position should be unmissable (glowing indicator, not just a label)
- Faction territories should use color-coding, not just text
- Zoom levels should reveal more detail, not just scale

**Ship & Fleet — Reference: Homeworld 3, Starsector, FTL**
- Player ship should be immediately identifiable (glow, outline, or scale)
- NPC ships should communicate role visually (military=angular, trader=bulky)
- Fleet formations should look intentional, not random scatter

**DO NOT grade on a curve because this is indie.** The player doesn't care about
development context — they compare against every game they've ever played. Flag
real gaps honestly, but classify severity fairly (CRITICAL = broken, not just "not
as good as EVE").

---

## 0b. UNIVERSAL EVALUATION HEURISTICS

Apply these to EVERY screenshot, regardless of type. Based on Nielsen's 10
Usability Heuristics and Pinelle's Game Usability Heuristics (CHI 2008).

### Visibility of System Status (Nielsen #1)
- Can the player tell what state the game is in? (flying, docked, in transit, in combat)
- Is there feedback for the current action? (health bars, progress, ETA)
- Are important changes communicated? (damage taken, credits earned, destination reached)

### Match Between System and Real World (Nielsen #2)
- Do labels use player-facing language, not internal identifiers?
- Do icons/colors match intuitive meanings? (red=danger, green=safe, gold=wealth)
- Does the visual style match the genre? (space game should feel like space)

### User Control and Freedom (Nielsen #3)
- Can the player see how to exit the current state? (undock, close menu, resume)
- Are navigation options visible? (keybind hints, back buttons, escape)

### Consistency and Standards (Nielsen #4)
- Do similar elements look the same across screens? (buttons, panels, labels)
- Is the color palette consistent? (same blue for shields everywhere)
- Do fonts follow a clear hierarchy? (title > body > caption)

### Recognition Rather Than Recall (Nielsen #6)
- Is important information visible without memorization? (current system, credits, health)
- Are interactive elements obviously interactive? (buttons look like buttons)

### Aesthetic and Minimalist Design (Nielsen #8)
- Does every visible element serve a purpose?
- Is the screen cluttered or does it have appropriate breathing room?
- Does the visual design direct attention to what matters?

### Game-Specific: Visual Clarity During Action (Pinelle)
- Can the player identify their ship, enemies, allies, and objects?
- Are game objects distinguishable from each other and from the background?
- Is the camera framing appropriate for the current game state?

### Game-Specific: Appropriate Feedback (Pinelle)
- Does the game respond visually to player actions?
- Are state transitions visible? (entering/leaving warp, docking, combat start)
- Do important moments feel important? (discovery, arrival, danger)

---

## 0c. EVALUATION RULES — Avoiding False Reports

**CRITICAL: Follow these rules to prevent hallucination-based issues.**

1. **Never claim absence without certainty.** Do NOT say "no engine glow" or
   "no station visible" unless you have carefully examined the entire frame.
   Small objects at distance are easy to miss. Say "not clearly visible" instead.

2. **Separate bugs from design opinions.** Tag each issue with its classification:
   - **BUG**: Something is visually broken (overlap, clipping, missing element, wrong color)
   - **UX**: Information architecture problem (hard to read, confusing layout, unclear state)
   - **POLISH**: Works but could look better (thin bars, low contrast, spacing)
   - **GAP**: Falls short of best-in-class reference (no bloom on star, flat lighting)
   - **OPINION**: Subjective design preference (composition, focal point, "wow factor")

   The issue table must include the tag:
   | # | Severity | Tag | Issue | Detail |

3. **Check bot log before claiming a mechanic didn't work.** If the bot log says
   combat occurred, don't mark "no combat visible" as a bug — mark it as a
   camera/zoom issue at most.

4. **Account for development stage.** This is an indie game in active development.
   Placeholder names ("System 0") and missing polish are EXPECTED. Rate them as
   POLISH, not CRITICAL.

5. **Scale matters.** The player ship is deliberately small relative to celestial
   objects. This creates a sense of scale and wonder. Do not flag "ship too small"
   as an issue unless it is genuinely unidentifiable.

6. **Screenshot compression.** Static screenshots cannot show: particle animation,
   ship rotation, planet spinning, engine thrust trails in motion. Do not penalize
   the game for things that only exist in motion.

---

## 1. FIRST IMPRESSION (every screenshot)

The "3-second test" — what a new player sees before reading anything.

- **Does the scene look like a space game?** Dark background, stars, celestial bodies, sci-fi feel
- **Is there a clear focal point?** Eye should be drawn to something interesting, not lost in emptiness
- **Does it feel alive?** Particles, glow, movement traces, NPC presence — or does it feel static/dead?
- **Is there visual variety?** Different colors, shapes, sizes — or monotone sameness?
- **Would a player screenshot this to show a friend?** The bar for a space game — it should look cool

Reference: EVE Online, Stellaris, Endless Space 2 — even their basic views have visual drama.

---

## 2. SOLAR SYSTEM VIEW (boot, hud_flight, system_2, system_3, tick_*)

### Star
- Star is prominent and correctly colored for its class (G=warm yellow, M=deep red, O=blue-white)
- Star size matches class (ClassG=1.0x baseline, ClassO=1.8x, ClassM=0.6x)
- Light tints the scene — warm systems feel warm, blue systems feel cool
- Star has glow/emission — not a flat colored sphere

### Planets
- Multiple planets visible at varying distances from star
- Planet types visually distinct (gas giants large, rocky small, ice/lava different textures)
- Planets are self-rotating (not static)
- Moons visible near larger planets
- Orbit spacing feels natural — not bunched up or too sparse

### Stations
- At least one station visible in each system
- Station has a recognizable 3D model (not a placeholder sphere/cube)
- Station label readable at current zoom level

### NPC Fleets
- NPC ships visible in the system (not an empty void)
- Fleet ships use Quaternius models (detailed spacecraft, not wedges/primitives)
- Different fleet roles look different (traders vs patrol vs haulers)
- Fleet labels show composition (patrol=blue, traders=gold, hauler=gray)

### Asteroid Belt
- If present (~60% of systems): visible ring of varied rocks at belt radius
- Mixed shapes (sphere/box/cylinder) — not all identical
- Appropriate scale — not dominating the scene

### Atmosphere
- Starfield visible in background (thousands of stars, not black void)
- Galactic sky/nebula visible (subtle color wash in background)
- Ambient particles (dust motes) add depth
- Scene has depth — foreground (player/station), midground (planets), background (stars)

### Variety Across Systems
- System 1, 2, 3 look meaningfully different (different star color, planet layout, station type)
- Security level affects feel (safe=calm, dangerous=tense colors)

---

## 3. HUD & INFORMATION DISPLAY (hud_flight, all non-menu shots)

### Readability
- All HUD text legible against the space background (sufficient contrast)
- Font sizes follow hierarchy: titles 20px, body 14px, captions 12px
- No text overflow or truncation
- Numbers formatted sensibly (credits with separators, not raw integers)

### Information Architecture
- Most important info (credits, location, health) immediately findable
- Hull/shield bars clearly communicate current state (color-coded: green=healthy, red=critical)
- Player state label (Flying/Docked/In Lane Transit) always visible
- Security band shown with correct color (hostile=red, dangerous=orange, safe=green)

### Layout
- HUD elements don't overlap each other
- HUD doesn't obscure the game world excessively
- Information grouped logically (health bars together, economy info together)
- Keybinds hint visible but unobtrusive (top-right)

### Visual Polish
- Text colors follow ui_theme.gd hierarchy (primary=0.85/0.85/0.9, secondary=0.6/0.6/0.7)
- Panel backgrounds are dark navy (PANEL_BG), not pure black
- Borders are subtle navy (BORDER_DEFAULT), 2px width
- Corner radius: 6px for compact panels, 8px for standard

---

## 4. DOCK MENU (dock_market, dock_jobs, dock_services, post_buy)

### Layout & Structure
- Menu centered on screen, ~550px wide
- 5-tab bar at top: Market | Jobs | Ship | Station | Intel
  - **Progressive disclosure**: Jobs revealed after first trade; Ship after first mission/combat; Station and Intel after 3+ nodes visited. Tabs show "[NEW]" gold badge when first revealed
- Active tab visually distinguished from inactive tabs
- Station name and context description visible at top ("Produces: X")
- Security band label, tariff rate label, reputation tier label present
- Planet info (type/gravity/atmosphere/temperature) shown when docked at planet

### Market Tab
- Goods listed with clear columns (Good, Buy price, Sell price, Quantity)
- Buy/Sell/Max buttons visible and aligned
- Prices readable — not tiny or clipped
- Production section shows what the station makes

### Jobs Tab
- Mission listings (if any) with clear objective text and Accept buttons
- Empty state handled gracefully ("No missions available" vs blank space)
- Revealed after first trade (progressive disclosure)

### Ship Tab
- Ship fitting overview: slot count, power draw, cargo, zone armor, hull/shield totals
- Refit section: install modules (timed installation)
- Maintenance section: ship health and repair options
- Revealed after first mission or combat

### Station Tab
- Station health, production, and services info
- Research section: start tech research projects (consume goods per sustain)
- Construction section: build station upgrades
- Revealed after 3+ nodes visited

### Intel Tab
- Trade routes: discovered price differentials and profitable routes
- Automation section: configure trade/fleet automation programs
- Anomaly encounters: review and resolve anomaly results
- Revealed after 3+ nodes visited

### Polish
- ScrollContainer works if content overflows
- Cargo display shows current cargo count
- "Undock" button clearly visible and reachable
- Panel uses make_panel_dock() style (dark navy bg, subtle border, 6px corners)

### Best Practice Reference
- Compare to: Elite Dangerous station menu, X4 Foundations trade screen
- Key test: can a new player understand what to do in 5 seconds?
- Information density: enough to be useful, not so much it's overwhelming

---

## 5. GALAXY MAP (galaxy_map)

### Readability
- Star nodes clearly visible as distinct points
- Lane edges (connections) visible between nodes
- Current player location marked with "YOU" indicator (green, prominent)
- Node names readable when zoomed in

### Information Density
- Galaxy map conveys useful information at a glance (which systems are connected)
- Security zones distinguishable by color
- Faction territories have subtle indicators
- Trade flow visualization (if enabled) readable

### Navigation
- "GALAXY MAP (TAB to close)" header visible at top-center
- Map auto-fits to show all visible nodes centered in the viewport
- Node detail popup appears on left-click (name, star class, fleet count, prices)

### Visual Quality
- Nodes are beacon spheres with glow, colored by faction territory
- Dark navy-purple background plane provides depth contrast
- Overlay scrim dims the 3D world behind (prevents starlight washout)
- Lane connections visible as persistent lines between nodes
- Faction territory shown as semi-transparent colored discs

### Best Practice Reference
- Stellaris: galaxy map with clear territory coloring, zoom levels, search
- Endless Space 2: lane-based map with distinct node styling per system type
- Key test: can you plan a trade route by looking at the map for 10 seconds?

---

## 6. EMPIRE DASHBOARD (empire_dashboard)

### Layout
- Full-screen modal with proper scrim (dark overlay behind panel)
- Panel has margins (not edge-to-edge)
- Title "EMPIRE DASHBOARD" centered and prominent
- Tab bar (Overview | Research | Stats) visible

### Content
- Key empire metrics shown: credits, fleets, automation, research, missions, production
- Values are current and meaningful (not placeholder "0" everywhere)
- Guidance text helpful for empty states ("dock at a station to start research")

### Polish
- Background scrim dims the game world (0.55 opacity)
- Panel styling: dark navy bg, border, 8px corners
- Scrollable if content overflows
- Close button (X) visible

### Best Practice Reference
- Stellaris: situation log, outliner — dense but scannable
- Key test: does this make the player feel like they're running an empire?

---

## 7. TIME PROGRESSION (tick_200, tick_500)

### Economy Feel
- Credits have changed from initial state (economy is doing something)
- NPC fleets have moved (not frozen in place)
- Market prices may have shifted
- Trade flow visible (if NPC trade routes active)

### Visual Continuity
- Scene still looks correct after many ticks (no visual glitches)
- Labels still readable, no accumulation of visual artifacts
- Performance: no obvious lag or frame issues evident in screenshot

---

## 8. TECHNICAL COMPLIANCE (every screenshot)

All color, typography, spacing, and panel styling specs live in `scripts/ui/ui_theme.gd`.
Do not duplicate those values here — read the source file for exact tokens.

Check that visible UI elements conform to ui_theme.gd:
- [ ] Semantic colors used correctly (interactive=CYAN, safe=GREEN, danger=RED, etc.)
- [ ] Text color hierarchy respected (PRIMARY, SECONDARY, INFO, DISABLED)
- [ ] Panel backgrounds are dark navy (not black, not gray, not transparent)
- [ ] Borders present where expected, correct width and color
- [ ] Spacing consistent — no random pixel gaps or misaligned elements

### Known Issues to Watch For
- Gray/blank screenshots = scene didn't load or window was minimized
- All systems look identical = star tinting or GalaxyView not refreshing
- Dock menu shows same market everywhere = node_id not passed correctly
- HUD labels missing = scene tree path issue (Main/HUD vs root/HUD)

---

## 9. FEEL SCORING DIMENSIONS

After reviewing all screenshots as a set, evaluate the game across five **feel
dimensions**. These synthesize the per-screenshot evidence from Sections 1-8 into
a holistic verdict. Each dimension asks: "Does this game meet the standard that
successful space games have established?"

**Important:** You are not grading isolated screenshots. You are grading the
*experience arc* visible across the full screenshot set — boot → flight → dock →
travel → combat → galaxy map → progression.

### Dimension 1: COMPOSITION

*Does the visual frame direct the player's eye correctly?*

Evidence sources: Every screenshot (Section 1 first impression applies to all).

| Criterion | What to look for |
|-----------|-----------------|
| Focal point | Each frame has a clear primary subject (ship, star, menu, planet) |
| Visual hierarchy | Most important element is largest/brightest/most contrasted |
| Negative space | Space feels vast but not empty — background stars, dust, distant objects fill void |
| Balance | UI elements don't cluster on one side; 3D scene uses depth (fore/mid/background) |
| Framing | Camera position puts the action where the eye naturally goes (center-left for flight, center for menus) |

AAA standard: Everspace 2 flight views always have a clear focal planet/station with the ship framed
against it. Stellaris galaxy map uses node size + glow to create visual hierarchy. Starsector combat
keeps the player ship center with enemies clearly distributed around it.

### Dimension 2: READABILITY

*Can the player parse the game state in under 3 seconds?*

Evidence sources: Sections 3 (HUD), 4 (Dock), 5 (Galaxy Map), 6 (Empire Dashboard), 8 (Technical).

| Criterion | What to look for |
|-----------|-----------------|
| Glanceability | Health, credits, location identifiable without searching |
| Contrast | Text readable against all backgrounds (space, panels, overlays) |
| Information density | Enough info to act on, not so much the eye bounces without landing |
| Typographic hierarchy | Title > heading > body > caption — sizes and weights distinguish levels |
| State communication | Current game mode (flying/docked/transit/combat) obvious from HUD alone |

AAA standard: Elite Dangerous — the entire ship state reads in one glance (shields as
circle, hull as bar, target in center, speed left, heat right). FTL — health, fuel,
crew, systems — all visible simultaneously without scrolling. EVE Online — despite
extreme complexity, the overview panel prioritizes what you need NOW.

### Dimension 3: SCALE & SPACE

*Does the universe feel appropriately vast and the player appropriately small?*

Evidence sources: Section 2 (Solar System View), Section 5 (Galaxy Map).

| Criterion | What to look for |
|-----------|-----------------|
| Relative sizing | Stars >> planets >> stations >> ships. Hierarchy consistently maintained |
| Depth cues | Near objects larger/brighter than far ones. Parallax between layers |
| Vastness | Space between objects creates awe, not boredom. Systems don't feel like cluttered rooms |
| Galaxy scope | Galaxy map conveys dozens-to-hundreds of systems. Universe feels explorable |
| Variety across systems | Different systems genuinely look different (star color, planet count, belt presence) |

AAA standard: Elite Dangerous — dropping out of supercruise near a ringed gas giant
creates genuine awe. Stellaris — zooming from galaxy to system to planet smoothly
conveys scale. Outer Wilds — small solar system but every angle reveals new depth.

### Dimension 4: POLISH

*Does the game feel finished or prototypey?*

Evidence sources: Sections 3-6 (all UI panels), Section 8 (Technical Compliance).

| Criterion | What to look for |
|-----------|-----------------|
| Alignment | UI elements aligned to grid. No 1-2px misalignments between related elements |
| Consistency | Same element looks the same everywhere (buttons, panels, labels, borders) |
| Empty states | "No items" is handled gracefully, not blank space or missing panel |
| Edge cases | Long text truncates with ellipsis not overflow. Large numbers formatted correctly |
| Visual coherence | All panels share the same dark navy aesthetic. No rogue colors or mismatched styles |

AAA standard: Slay the Spire — even as indie, every card, every panel, every tooltip
is pixel-perfect. Starsector — complex UI but every panel follows the same design
language consistently. FTL — tiny budget, immaculate polish.

### Dimension 5: ATMOSPHERE

*Does this feel like a space game? Would you want to explore this galaxy?*

Evidence sources: Section 2 (Solar System View), Section 7 (Time Progression), overall set.

| Criterion | What to look for |
|-----------|-----------------|
| Mood | Lighting creates emotional tone: warm sun = safe harbor, red dwarf = desolate frontier |
| Star field | Background stars visible, dense enough to feel cosmic, not uniform noise |
| Emission/glow | Stars, engines, stations have glow/bloom. Space isn't lit by invisible flat lights |
| Color identity | Each system has a distinct color personality driven by its star class |
| Life signs | NPC ships, particle trails, station lights — the galaxy feels inhabited, not static |

AAA standard: Everspace 2 — every system has a distinct color palette and mood.
Mass Effect galaxy map — each cluster feels like a real region with character.
Homeworld — minimal HUD, maximum atmosphere, emptiness that feels intentional.

---

## 10. REFERENCE COMPARISON

When reference screenshots are available in `reports/references/`, use them for
**principle comparison** — not visual copying.

### How to use references

**DO:**
- Compare UX principles: "Reference shows health as a thick arc wrapping the reticle.
  Our thin bar in the top-left corner requires the player to look away from the action."
- Compare information hierarchy: "Reference puts credits front-center during trade.
  Ours buries credits in a side panel."
- Compare spatial composition: "Reference frames the station as the visual anchor
  with the ship approaching. Ours shows station as a small object among many."
- Compare polish level: "Reference has consistent 8px padding on all panels.
  Ours varies between 4px and 12px."

**DO NOT:**
- Suggest copying art style, color scheme, or specific layout
- Penalize for having a different visual identity
- Assume the reference game's choices are always correct for this game
- Compare asset quality (AAA budget vs indie budget is not the point)

### Reference categories

Three tiers of references:
- **Strong comp** — same camera perspective (top-down) and real-time gameplay. Directly comparable.
- **UI comp** — 2D menu/panel design. Camera perspective doesn't matter for UI.
- **Principle-only** — different perspective but demonstrates a UX principle worth extracting.

**IMPORTANT:** Our game uses a top-down camera (Y=120 altitude). Only compare 3D
scene composition against other top-down games (Starsector, Starcom). Do NOT
penalize our flight/combat views for lacking depth or drama that requires a
third-person or first-person camera (Everspace 2, Elite Dangerous, Outer Wilds).
2D UI panels (dock, galaxy map, dashboard) are perspective-agnostic and can be
compared against any game.

#### Strong comps (top-down, real-time — directly comparable)

| File | Game | Category | What principle to extract |
|------|------|----------|--------------------------|
| `ref_flight_starcom.jpg` | Starcom | Flight view | Top-down flight composition: ship framed inside gate ring, HUD overlay (speed/energy/hull), crosshair, dark space background. How to make a top-down view feel spatial |
| `ref_combat_starcom.png` | Starcom | Combat | Top-down combat readability: shield bubbles, projectile trails connecting attacker to target, planet depth layers. The standard for our combat view |

#### UI comps (2D panels — perspective-agnostic)

| File | Game | Category | What principle to extract |
|------|------|----------|--------------------------|
| `ref_dock_starsector.png` | Starsector | Market/trade | Grid-based commodity inventory, tabbed categories, faction crest + planet as visual anchor. Dense but scannable |
| `ref_dock_starsector_refit.png` | Starsector | Ship fitting | Ship model center with hardpoint slots, weapon stat panel, hull features. Dense but readable |
| `ref_galaxy_stellaris.png` | Stellaris | Galaxy map (zoomed) | Hyperlane topology with system info panel, territory coloring, system icons |
| `ref_galaxy_stellaris_full.jpg` | Stellaris | Galaxy map (full) | Full galaxy with color-coded empire territories, faction logos, outliner panel (planets/fleets/ships). Strategic overview gold standard |
| `ref_empire_stellaris.png` | Stellaris | Empire overview | Outliner sidebar: planets, military fleets, civilian ships with details. How to make a dashboard feel like running an empire |
| `ref_galaxy_moo.png` | MOO 2016 | Galaxy map | Star visual variety as language (color/size = type), minimalist lane topology, fleet panel |
| `ref_dock_moo_planet.png` | MOO 2016 | Planet management | Visual anchor (giant planet) + compact stat cards + resource breakdown tiles around it |
| `ref_hud_moo_select.png` | MOO 2016 | Character select | Master-detail pattern: grid → portrait → traits. Personality conveyed visually. Relevant to FO promotion |
| `ref_hud_moo_research.png` | MOO 2016 | Research celebration | Milestone as event, not notification. Show the thing, explain reward, one clear action |
| `ref_dock_starcom.png` | Starcom | Tech tree | Tab navigation, node-graph tech tree, color-coded completion state, hover tooltips, station identity |
| `ref_dialogue_starcom.png` | Starcom Nexus | Dialogue | Dialogue panel layout, character portrait, response options, planet backdrop |
| `ref_dock_moo.png` | MOO 2016 | Diplomacy | Two-party mirror layout, disposition bar, clear CTAs. Future faction negotiation reference |

#### Principle-only (different perspective — extract design principles, not composition)

| File | Game | Category | Limitation | Principle to extract |
|------|------|----------|------------|---------------------|
| `ref_hud_elite.png` | Elite Dangerous | HUD | First-person cockpit | Information hierarchy through spatial position (shields=circle, speed=left, heat=right). Glanceability without searching |
| `ref_atmosphere_outer_wilds.png` | Outer Wilds | Atmosphere | First-person ground | Star as dominant visual anchor, atmosphere shader mood. NOT comparable for composition |
| `ref_atmosphere_endless2.png` | Endless Space 2 | Galaxy mood | Galaxy-scale (not system) | Warm dust lanes against cyan core, minimal HUD. Aspiration for galaxy map backdrop, NOT system view |

#### Known gaps (no reference collected yet)

| Category | What we need | Best source game |
|----------|-------------|-----------------|
| Top-down fleet combat (shields, flux) | Starsector fleet battle with shield arcs + flux bars | Starsector |
| Top-down system cruise (populated) | Starsector patrol/travel through a system with planets | Starsector |
| Top-down HUD during real-time flight | Starsector or Starcom flight HUD overlay | Starsector |

If no reference images exist for a category, evaluate against the AAA text
descriptions in Section 0 and the per-dimension AAA standards in Section 9.

---

## 11. PRESCRIPTION OUTPUT FORMAT

When evaluating for the `/feel` skill, produce **semantic prescriptions** — describe
what should change in game-design terms, NOT in code terms. The main Claude context
(which has codebase access) will map prescriptions to actual file/parameter changes.

### Prescription structure

Each prescription must include:

```
PRESCRIPTION #N
  Dimension:    [composition | readability | scale_space | polish | atmosphere]
  Confidence:   [high | medium | low]
  Severity:     [critical | major | minor | suggestion]
  Tag:          [BUG | UX | POLISH | GAP | OPINION]
  Issue:        One-sentence description of what's wrong
  Evidence:     Which screenshot(s) show the problem, and what specifically to look at
  Standard:     What the AAA reference standard is for this element
  Prescription: Specific, measurable change to make (e.g., "increase explosion radius
                to 2-3x ship width" not "make explosions bigger")
  Metric:       How to verify the fix worked in the next iteration
```

### Confidence levels

- **High**: Objectively measurable (contrast ratio, element overlap, text truncation,
  missing UI element). Safe to act on immediately.
- **Medium**: Subjective but well-supported by reference standards (composition
  imbalance, insufficient visual hierarchy). Worth addressing.
- **Low**: Pure aesthetic opinion (color preference, layout alternative). Present
  as option, don't prioritize.

### Iteration comparison (iterations 2+)

When evaluating a re-run after changes, compare against the previous iteration's
scores and prescriptions:

```
ITERATION DELTA:
  Improved:   [list prescriptions that were addressed and are now better]
  Regressed:  [list dimensions that got worse — possible side effects of changes]
  Unchanged:  [list prescriptions that were not addressed or had no visible effect]
  New issues: [list problems that appeared in this iteration but not the previous]
```

**Regression is the most important signal.** If fixing one thing broke another, flag
it immediately. This prevents oscillation.

---

## RATING TEMPLATE

For each screenshot, rate:

```
[screenshot_name]
  First Impression:  PASS / NEEDS_WORK / FAIL — notes
  Scene Content:     PASS / NEEDS_WORK / FAIL — notes
  Readability:       PASS / NEEDS_WORK / FAIL — notes
  Polish:            PASS / NEEDS_WORK / FAIL — notes
  Theme Compliance:  PASS / NEEDS_WORK / FAIL — notes
```

Then synthesize across ALL screenshots using the five feel dimensions:

```
FEEL SYNTHESIS:
  Composition:    PASS / NEEDS_WORK / FAIL — notes
  Readability:    PASS / NEEDS_WORK / FAIL — notes
  Scale & Space:  PASS / NEEDS_WORK / FAIL — notes
  Polish:         PASS / NEEDS_WORK / FAIL — notes
  Atmosphere:     PASS / NEEDS_WORK / FAIL — notes
```

Then list prescriptions in priority order:

```
PRESCRIPTIONS (ranked by severity × confidence):
  #1: [prescription block]
  #2: [prescription block]
  ...

OVERALL:
  Top 3 Strengths: ...
  Top 3 Issues: ...
  Priority Fix: The single most impactful change for the next iteration
```
