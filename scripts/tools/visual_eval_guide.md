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

### Combat Vignette (flight mode, combat only)
- Red border glow (shader-based edge tint) during COMBAT state
- Smoothstep from 0.7 to 1.0 creates edge-only effect (center stays clear)
- Fades in 0.4s on combat start, fades out 0.6s on combat end
- Indicates hostile proximity — visible in screenshots as red-tinted screen edges

### Transit Label Suppression (flyby arrival)
- Transit overlay ("→ System X") and WARP TRANSIT HUD hidden during flyby arrival
- `suppress_transit_overlay` flag active from post-approach through flyby cinematic
- Only cleared when state transitions to IN_FLIGHT
- Prevents triple-stack of transit label + warp HUD + arrival cinematic

### Toast System (top-right corner)
- Event log toasts: salvage, turbulence, arrival notifications, mission updates
- "Arrived at [System Name]" toast on lane arrival (2.5s duration)
- Gold-tinted mission toasts, white general toasts
- Stacks vertically if multiple fire simultaneously

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

**Ship & Fleet — Reference: Homeworld 3, Starsector, FTL, SPAZ 2**
- Player ship should be immediately identifiable (glow, outline, or scale)
- NPC ships should communicate role visually (military=angular, trader=bulky)
- Fleet formations should look intentional, not random scatter
- Ship spin should be visible and readable from top-down (SPAZ 2: shield arc rotates with ship)
- Axial/spinal weapons should have a distinct visual (SPAZ 2: thick beam from bow, clearly different from turret fire)

**Combat — Reference: Starsector, Starcom, SPAZ 2**
- Weapon families should be visually distinct (beam vs projectile vs missile)
- Spinal/axial weapons should dominate the visual frame (SPAZ 2: yellow beam cuts across entire screen)
- Combat chaos should still be readable — weapon group indicators (SPAZ 2: numbered slots 1-4 on HUD edge)
- Tractor beams as an active combat/scavenge tool, not passive auto-pickup (SPAZ 2: dedicated tractor binding)

**NPC Interaction — Reference: Starsector, Sunless Skies, Freelancer**
- NPCs should feel like people with agendas — portrait + personality-driven text, not just stat blocks
- Dialogue choices should have visible consequences or at least hint at outcomes (Starsector: commission acceptance, faction standing change)
- Officers/crew should have names, portraits, roles, and narrative prose (Sunless Skies: officer encounter text)
- Job boards and news should feel in-universe — the player reads "news" that is actually faction/economy intel (Freelancer: news feed as world state)

**Mission Journal — Reference: Starsector (intel), Starfield (mission log)**
- Missions should be categorized by type (faction/bounty/exploration/smuggling), not just a flat list
- Each mission should have: briefing text, objective checklist, reward preview, location on galaxy map
- "Show on Map" should be a one-click action from the journal (Starfield: "Show All Targets" + "Set Course")
- Completed missions should remain accessible for narrative context

**Faction/Diplomacy — Reference: Mount & Blade, Starsector, Stellaris**
- Faction standing should be a visible bar/meter, not hidden behind a tooltip
- Relationship modifiers should be enumerated (why they like/dislike you), not just a final number
- Multi-faction view should show all standings at once for strategic comparison (Mount & Blade: at-war/at-peace list)

**Empire Dashboard — Reference: Stellaris, Distant Worlds 2**
- Economy should show revenue AND expenses in a breakdown, not just net credits
- Fleet/ship overview should show state (idle/combat/transit), not just existence
- Situation log / event feed should surface the most urgent items automatically

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

### Boot Frame Critical Checks (boot/01_boot screenshot ONLY)

These checks apply specifically to the very first frame the player sees:

- **Welcome/Onboarding**: Is there a welcome overlay, intro popup, or clear
  guidance for a first-time player? Dropping the player into the game with
  zero context = FAIL. A toast is insufficient — there should be a proper
  welcome screen with controls and a first objective.
- **Camera Introduction**: Does the game open with a cinematic camera movement?
  (Galaxy zoom, system flyby, altitude descent?) Static snap to default = WARN.
- **Combat Artifacts**: Are any combat effects visible? (Red screen tint/vignette,
  heat bar, damage flash, shield effects?) These should NEVER be visible at boot.
  If present = CRITICAL BUG — player thinks the game is broken.
- **Unwanted UI Panels**: Are any panels open that shouldn't be? (Data log,
  combat log, mission journal?) This usually indicates a keybind conflict with
  movement keys. If present = CRITICAL BUG.
- **Starting System Richness**: Does the starting system have a star, at least
  one planet, a station, and NPC activity? If it's just empty space with a
  distant star = FAIL. The first system the player sees sets their expectations
  for the entire game.

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

## 4b. OVERLAY PANELS — Keyboard-Toggled (mission_journal, knowledge_web, combat_log)

These panels are toggled via keyboard shortcuts during flight (not docked).

### Mission Journal (J key) — `mission_journal` screenshot
- Panel overlay showing mission list
- Accept/complete status per mission
- Prerequisites display (what must be done before accepting)
- Reward info and description text
- Filter buttons for mission types (if multiple missions exist)
- Should show any active or completed missions from the bot's play session

### Knowledge Web (K key) — `knowledge_web` screenshot
- Faction, character, and lore data log entries
- Thread filter buttons (categories for organizing entries)
- Data entries with timestamps and source tags
- Content populated from discoveries, faction encounters, and mission completions

### Combat Log (L key) — `combat_log` screenshot
- Combat event chronology
- Damage dealt/received entries with timestamps
- Kill/death records
- Should have content from earlier NPC combat phase (bot damages NPCs before this capture)

### Evaluation Criteria (all three panels)
- Panel renders as a proper overlay (not replacing the game world)
- Content is readable (text contrast, font size hierarchy)
- Panel can be dismissed (toggle key noted, close button visible)
- Panel is not blank — should have content from the bot's play session
- Layout follows dark navy panel aesthetic (consistent with dock menu, empire dashboard)

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
- Background scrim dims the game world (0.85 opacity)
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

Evidence sources: Sections 3 (HUD), 4 (Dock), 4b (Overlay Panels), 5 (Galaxy Map), 6 (Empire Dashboard), 8 (Technical).

| Criterion | What to look for |
|-----------|-----------------|
| Glanceability | Health, credits, location identifiable without searching |
| Contrast | Text readable against all backgrounds (space, panels, overlays) |
| Information density | Enough info to act on, not so much the eye bounces without landing |
| Typographic hierarchy | Title > heading > body > caption — sizes and weights distinguish levels |
| State communication | Current game mode (flying/docked/transit/combat) obvious from HUD alone |
| Panel accessibility | Can the player find mission, research, and intel info via keyboard panels (J/K/L)? |

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

Evidence sources: Sections 3-6 (all UI panels), Section 4b (Overlay Panels), Section 8 (Technical Compliance).

| Criterion | What to look for |
|-----------|-----------------|
| Alignment | UI elements aligned to grid. No 1-2px misalignments between related elements |
| Consistency | Same element looks the same everywhere (buttons, panels, labels, borders) |
| Empty states | "No items" is handled gracefully, not blank space or missing panel |
| Edge cases | Long text truncates with ellipsis not overflow. Large numbers formatted correctly |
| Visual coherence | All panels share the same dark navy aesthetic. No rogue colors or mismatched styles |
| Progressive disclosure | Do tabs appear with [NEW] gold badge at the right progression point? Ship tab after combat, Station+Intel after 3+ nodes |

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
| `ref_combat_starsector.jpg` | Starsector | Fleet combat | **The #1 combat reference.** Shield arcs (blue/orange bubbles per facing), flux bars, beam weapons (green lasers), "Overloaded" state on stressed ships, hull/flux/CR readout. Shows how shield facings + flux pressure (= our zone armor + heat) read from top-down during fleet chaos |
| `ref_flight_starcom_cruise.png` | Starcom Nexus | System cruise | Top-down system cruise: star with lens flare, two planets with atmosphere shaders, tiny ship centered, energy/hull bars bottom-right. How a populated system feels vast from top-down — empty space between objects creates scale |
| `ref_flight_spaz.webp` | SPAZ 2 | Flight + HUD | Top-down flight with rotating ship, circular shield/radar indicator (green arc = shield facing), resource bars (REZ/DATA) at top, nebula + asteroid environment. Ship rotation visible during flight — directly comparable to our spin mechanic. Shows how a spinning ship reads from top-down camera |
| `ref_combat_spaz_axial.jpg` | SPAZ 2 | Combat + axial weapon | Capital ship firing axial beam weapon (our spinal mount equivalent) while small ships swarm. Beam trail, debris particles, explosion VFX, fleet chaos. Shows how a spinal weapon reads visually from top-down — the yellow beam is the focal point through the chaos. Weapon group slots (1-4) on left edge |
| `ref_galaxy_spaz.jpg` | SPAZ 1 | Star map | Difficulty-numbered nodes, "YOU ARE HERE" indicator, overlay filters (Colony/Mining/Science), locked/new/vendor icons, F-key command bar. Connected graph with red edges — comparable to our lane topology. Node difficulty numbers = our threat level equivalent |
| `ref_flight_starsector_cruise.png` | Starsector | System travel | Top-down system cruise with planet approach, fleet status bar (bottom), sensor range indicator, star + planets visible. Shows how Starsector communicates travel state — fleet icon trail, destination marker, supply/fuel bars. Directly comparable to our lane transit + system arrival |

#### UI comps (2D panels — perspective-agnostic)

| File | Game | Category | What principle to extract |
|------|------|----------|--------------------------|
| `ref_dock_starsector.png` | Starsector | Market/trade | Grid-based commodity inventory, tabbed categories, faction crest + planet as visual anchor. Dense but scannable |
| `ref_dock_starsector_refit.png` | Starsector | Ship fitting | Ship model center with hardpoint slots, weapon stat panel, hull features. Dense but readable |
| `ref_dock_starsector_refit2.png` | Starsector | Ship fitting (fighter bays) | Expanded refit view showing fighter bay slots, hull mod features panel, capacitor/vent allocation bars, variant naming. Shows how Starsector packs deep fitting into a single screen |
| `ref_dock_starsector_refit3.png` | Starsector | Ship fitting (capital) | Capital ship refit with massive hull, many weapon mounts, hull features list. Scale difference between frigate and capital fitting visible |
| `ref_hud_starsector.jpg` | Starsector | Combat HUD (full) | **Key HUD reference.** Weapon groups list (left), flux/hull/CR bars (bottom-right), ship status modifiers (left panel: CR, speed, maneuver bonuses), target info. Shows how dense combat information stays readable — weapon groups numbered 1-4 with fire mode (linked/alternating/autofire) |
| `ref_hud_starsector_overlay.jpg` | Starsector | Target overlay | Shield arc facing indicator (orange wedge), target ship name/class/variant, range/speed readout, weapon group assignment to target. Shows how to communicate "which face am I hitting" — directly relevant to our zone armor targeting |
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
| `ref_dock_x4_factory.jpg` | X4 Foundations | Station/factory building | Modular station construction: production module catalog (left), 3D station preview (center), module list (right), build resources + builder assignment (bottom). Shows how complex production chains read visually — directly relevant to our Haven starbase upgrades and industry sites |
| `ref_hud_endless2_techweb.jpg` | Endless Space 2 | Tech web (full) | Radial tech web with 4 quadrants (military/science/economy/empire), concentric tier rings, dependency lines, research queue (left panel). The gold standard for tech tree UI — elegant, explorable, communicates breadth and depth simultaneously |
| `ref_hud_endless2_techweb_zoom.jpg` | Endless Space 2 | Tech web (zoomed) | Zoomed tech node: icon + name + tier number, prerequisite lines, reward preview icons below each node. Shows how individual tech nodes communicate value at a glance without tooltip |
| `ref_hud_spaz_controls.jpg` | SPAZ 2 | Controls/bindings | Controller binding screen with dedicated tractor beam, boost shields, boost engines, weapon group cycle, camera zoom bindings. Shows the input vocabulary of a top-down space combat game — tractor beam as a first-class action, shields/engines as active resource toggles |
| `ref_dialogue_starsector_npc.png` | Starsector | NPC conversation | **Key NPC reference.** Bar encounter: NPC portrait (right), dialogue text with faction context, numbered response options ("You decide..."), commission acceptance flow, faction crest. Shows how to make an NPC feel like a person with agenda — text conveys personality, options have visible consequences |
| `ref_hud_starsector_intel.png` | Starsector | Intel/mission screen | **Key mission reference.** Tabbed categories (Factions/Bounties/Exploration/Fleet/Hostilities/Smuggling), mission list (left), detail panel (right) with faction crest + NPC portrait, galaxy map inset showing mission location. Shows how to organize 50+ active missions without overwhelming |
| `ref_hud_starsector_intel_map.png` | Starsector | Intel + galaxy map | Intel screen with galaxy map expanded — mission markers on map, warning beacon list, sector-wide intel at a glance. Shows how mission journal and galaxy map integrate — click mission, see it on the map |
| `ref_hud_starsector_reports.png` | Starsector | Reports/bounties | Report detail with NPC portrait, bounty description, reward amount, location clues ("Ambiguity" field). Shows how to make a mission briefing feel like intelligence — narrative text, not just objective bullets |
| `ref_dock_starsector_cargo.png` | Starsector | Fleet cargo transfer | Cargo transfer dialog: grid of good icons with quantities, value/unit, credit total, source/destination. Shows how to make cargo management fast — icon grid > text list, drag-and-drop implied |
| `ref_hud_starfield_missions.png` | Starfield | Mission log | Tabbed mission log (All/Main/Faction/Misc/Mission/Activity/Completed), tree-structured objectives with checkboxes, quest description left panel. Shows modern mission journal UX — hierarchical objectives, clear completion state, "Show on Map" + "Set Course" CTAs |
| `ref_empire_dw2_summary.png` | Distant Worlds 2 | Empire summary | **Key dashboard reference.** Economy panel (revenue/expenses breakdown), diplomacy status (war/peace per faction), ships & bases count, situation log. Shows how to pack empire-scale information into one screen — column layout, color-coded rows, sparkline-style indicators |
| `ref_hud_stellaris_settings.png` | Stellaris | Settings/new game | Game settings as a clean two-column key-value table with sliders and dropdowns. Shows how complex configuration stays scannable — consistent row height, grouped categories, "Reset to Default" safety net |
| `ref_dock_x4_inventory.png` | X4 Foundations | Trade/inventory | First-person station trade screen with tabular goods list (name/price/qty/supply/demand columns), station identity top-center, ship storage/pricing/transaction detail panels below. Shows how to present trade data in a cockpit context |
| `ref_dialogue_sunlessskies_officers.png` | Sunless Skies | Officers/crew | **Key FO reference.** Named officer with portrait, narrative encounter text (personality-driven prose), numbered response choices, officer roster sidebar with role icons (Signalman, Chief Engineer, Mascot). Shows how to make crew feel like characters — prose descriptions, not stat blocks |
| `ref_dialogue_freelancer_jobboard.png` | Freelancer | Job board / news | News feed with NPC portrait, selectable headlines, narrative text per item, resource ticker at bottom (food/fuel/goods). Shows how to deliver world state through an in-universe news interface — the player reads "news" that is actually faction/economy intel |
| `ref_hud_mountblade_diplomacy.png` | Mount & Blade | Diplomacy/factions | **Key faction reference.** Two-party diplomacy: faction crests, at-war/at-peace list, relationship attributes (Total Strength, Cohesion, Fiefs, etc.), tribute/alliance options. Shows how to present faction standing as a comparison — side-by-side layout with clear asymmetry |

#### Principle-only (different perspective — extract design principles, not composition)

| File | Game | Category | Limitation | Principle to extract |
|------|------|----------|------------|---------------------|
| `ref_hud_elite.png` | Elite Dangerous | HUD | First-person cockpit | Information hierarchy through spatial position (shields=circle, speed=left, heat=right). Glanceability without searching |
| `ref_atmosphere_outer_wilds.png` | Outer Wilds | Atmosphere | First-person ground | Star as dominant visual anchor, atmosphere shader mood. NOT comparable for composition |
| `ref_atmosphere_endless2.png` | Endless Space 2 | Galaxy mood | Galaxy-scale (not system) | Warm dust lanes against cyan core, minimal HUD. Aspiration for galaxy map backdrop, NOT system view |

#### Known gaps (no reference collected yet)

| Category | What we need | Best source game |
|----------|-------------|-----------------|
| FTL combat cross-section | Both ships visible mid-combat, room-by-room damage (zone paperdoll reference) | FTL |
| Cosmoteer modular ship building | Turret arc visualization during ship construction | Cosmoteer |
| Disco Elysium skill check | Dialogue choice with visible consequence/probability | Disco Elysium |

#### Recently filled gaps

| Category | File | What it provides |
|----------|------|-----------------|
| **NPC conversation** | `ref_dialogue_starsector_npc.png` | Bar encounter with portrait, faction context, numbered responses — the key NPC interaction reference |
| **Mission journal** | `ref_hud_starsector_intel.png`, `ref_hud_starfield_missions.png` | Tabbed mission categories, detail panel, galaxy map integration, hierarchical objectives |
| **Mission briefing** | `ref_hud_starsector_reports.png` | Bounty detail with NPC portrait, narrative description, reward, location clues |
| **Faction diplomacy** | `ref_hud_mountblade_diplomacy.png` | Two-party comparison, relationship attributes, war/peace status, tribute options |
| **Officer/crew panel** | `ref_dialogue_sunlessskies_officers.png` | Named officers with portraits, narrative prose, role icons — FO reference |
| **Job board / news** | `ref_dialogue_freelancer_jobboard.png` | In-universe news as intel delivery, NPC portrait, selectable headlines |
| **Empire dashboard** | `ref_empire_dw2_summary.png` | Economy breakdown, diplomacy status, ships/bases, situation log — empire-at-a-glance |
| **Cargo/inventory** | `ref_dock_starsector_cargo.png`, `ref_dock_x4_inventory.png` | Icon grid transfer, tabular trade data |
| **Settings/config** | `ref_hud_stellaris_settings.png` | Clean key-value settings table with sliders |
| **System travel** | `ref_flight_starsector_cruise.png` | Top-down system cruise with fleet trail, sensor range |
| Top-down fleet combat | `ref_combat_starsector.jpg` | Shield arcs, flux, beam weapons, Overloaded state |
| Top-down combat HUD | `ref_hud_starsector.jpg` | Weapon groups, flux/hull/CR, status modifiers |
| Target overlay / zone facing | `ref_hud_starsector_overlay.jpg` | Shield arc wedge — "which face am I hitting" |
| Ship fitting depth | `ref_dock_starsector_refit2.png`, `ref_dock_starsector_refit3.png` | Fighter bays, capital fitting, hull mods |
| Station/factory | `ref_dock_x4_factory.jpg` | Modular station construction with production chains |
| Tech tree web | `ref_hud_endless2_techweb.jpg`, `ref_hud_endless2_techweb_zoom.jpg` | Radial tech web |
| Top-down combat + axial | `ref_combat_spaz_axial.jpg` | Spinal beam, fleet chaos, weapon groups |
| Top-down flight + spin | `ref_flight_spaz.webp` | Rotating ship with shield arc, HUD overlay |
| Star map topology | `ref_galaxy_spaz.jpg` | Difficulty-gated connected graph, player locator, overlay filters |

If no reference images exist for a category, evaluate against the AAA text
descriptions in Section 0 and the per-dimension AAA standards in Section 9.

---

## 10b. MISSING UI SURFACE CRITERIA (v2 expansion)

These UI surfaces exist in the game but had no prior visual evaluation criteria.
Apply the same scoring methodology (1-5 per dimension) when screenshots capture
these elements.

### Combat HUD

When combat is active, the following elements should be visible:

| Element | Expected State | Score 5 | Score 1 |
|---------|---------------|---------|---------|
| Heat bar | Shows accumulation during sustained fire | Smooth fill, color transitions (green→yellow→red), clear overheat warning | Absent or always at 0 |
| Battle stations indicator | Shows SpinningUp/BattleReady state | Clear icon + label, state transition visible | Missing or always shows "StandDown" |
| Zone armor display | Shows directional HP (fore/port/starboard/aft) | 4-zone diagram or bars, damage visible per zone | No zone display |
| Damage numbers | Float above targets | Readable, color-coded (shield=blue, hull=red) | Invisible or overlapping |
| Radiator status | Shows cooling rate | Present during combat, responds to damage | Absent |

**Anti-hallucination:** If the bot used one-shot kills, combat HUD elements may
never have had time to display. Score based on what IS visible, not what should
be. Note: "combat too fast for heat to accumulate" as a prescriptive observation.

### First Officer Panel

| Element | Expected State | Score 5 | Score 1 |
|---------|---------------|---------|---------|
| FO name | Visible in panel header | Name displayed prominently, archetype shown | No name or "unnamed" |
| Dialogue area | Shows recent FO lines | Multiple lines scrollable, personality evident | Empty or single static text |
| Tier indicator | Shows promotion tier (0-3) | Clear tier badge or progress | No progression visible |
| Portrait area | Reserved for character art | Placeholder or art present | Nothing — blank space |

### Toast Notification System

| Element | Expected State | Score 5 | Score 1 |
|---------|---------------|---------|---------|
| Toast position | Top-center or side panel | Non-overlapping, visible against game scene | Overlapping HUD or cut off |
| Toast duration | 3-5 seconds | Enough time to read, fades gracefully | Too fast or permanent |
| Toast priority | Important events prominent | FO dialogue, loot, discovery distinct from routine | All toasts look identical |

### Discovery / Scan UI

| Element | Expected State | Score 5 | Score 1 |
|---------|---------------|---------|---------|
| Discovery site panel | Shows phase progression | Clear phase indicator (Unknown→Scanned→Analyzed) | Raw IDs or no phase info |
| Scan progress | Visual feedback during scan | Progress bar or animation | Instant with no feedback |
| Outcome display | Shows what was found | Reward preview, lore snippet, FO reaction | Raw data or nothing |

### Warfront / Territory UI

| Element | Expected State | Score 5 | Score 1 |
|---------|---------------|---------|---------|
| Faction territory labels | Visible on galaxy map | Color-coded regions, faction names at borders | No territory indication |
| War intensity indicators | Show conflict zones | Heat coloring on nodes/edges, intensity scale | No conflict visibility |
| Tariff/embargo warnings | Show at faction borders | Clear warning before entering hostile territory | No warning — player surprised |

### Haven Starbase

| Element | Expected State | Score 5 | Score 1 |
|---------|---------------|---------|---------|
| Haven icon on galaxy map | Visible as distinct marker | Unique icon, clearly different from regular stations | Same as any station |
| Tier display | Shows current upgrade tier | Clear tier indicator (1-5) with progress | No tier info |
| Hangar / stored ships | Shows available bay slots | Ship previews, swap interface clear | No hangar UI |
| Trophy wall | Shows collected fragments | Visual fragment display with names | Empty or missing |

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
  Tag:          [BUG | UX | POLISH | GAP | OPINION | SUPPRESSED | UNWIRED]
  Issue:        One-sentence description of what's wrong
  Evidence:     Which screenshot(s) show the problem, and what specifically to look at
  Standard:     What the AAA reference standard is for this element
  Prescription: Specific, measurable change to make (e.g., "increase explosion radius
                to 2-3x ship width" not "make explosions bigger")

Tag notes:
  SUPPRESSED = UI panel/element exists and is fully built but is force-hidden
               (visible=false). Fix is a visibility toggle, not new development.
  UNWIRED    = Bridge data exists but no UI element renders it. Needs a new
               GDScript consumer (design input required).
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
  #1: [prescription block]  EA_TIER: EA_BLOCKER / EA_HIGH / EA_NICE / EA_LATER
  #2: [prescription block]  EA_TIER: ...
  ...

EA READINESS:
  Status: NOT_READY / CONDITIONAL / READY
  Blockers: [list any EA_BLOCKER prescriptions]
  High-priority: [count of EA_HIGH items]

OVERALL:
  Top 3 Strengths: ...
  Top 3 Issues: ...
  Priority Fix: The single most impactful change for the next iteration
```
