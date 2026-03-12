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
- Hull bar (green) + Shield bar (blue) — thin progress bars
- Security label below (colored by band: green/yellow/orange/red)
- Research label further below (cyan when active, gray when idle)
- Zone G bottom bar: risk meters left, system status center, keybind hints bottom

### Warp/Transit
- Lane transit: dark interstellar space, blue lane line, nebula backdrop
- Warp VFX: expanding cyan sphere, camera shake + flash
- Arrival cinematic: letterbox bars + camera sweep from high altitude

### Galaxy Map
- Strategic altitude (2500+ units) looking down
- Nodes as beacon points, edges as lane connections
- "YOU" indicator (green) at player location
- Faction territory labels and colors

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
- 3-tab bar visible at top (Market | Jobs | Services)
- Active tab visually distinguished from inactive tabs
- Station name and context description visible at top
- Security band label present with correct color

### Market Tab
- Goods listed with clear columns (Good, Buy price, Sell price, Quantity)
- Buy/Sell/Max buttons visible and aligned
- Prices readable — not tiny or clipped
- Production section shows what the station makes

### Jobs Tab
- Mission listings (if any) with clear objective text
- Automation programs listed
- Empty state handled gracefully ("No missions available" vs blank space)

### Services Tab
- Refit, maintenance, research sections visible
- Each section clearly separated
- Empty sections hidden (not showing blank areas)

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
- "GALAXY MAP (TAB to close)" header visible
- It's obvious how to navigate (zoom, pan)
- Map centered on player location or galaxy center

### Visual Quality
- Not just dots and lines — nodes have character (size, color, glow)
- Background doesn't compete with map elements
- Overlay dim/scrim makes map readable over 3D scene

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

## 9. GAME FEEL SYNTHESIS

After reviewing all screenshots as a set:

- **Does the game look polished enough to show publicly?** (even as early access)
- **Is there a consistent visual identity?** Dark space theme, navy panels, semantic colors
- **Does information escalate naturally?** Simple in flight → detailed when docked → strategic in map/dashboard
- **Would you want to explore this galaxy?** The ultimate space game question
- **What's the single biggest visual improvement that would raise the bar?**

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

End with:
```
OVERALL SYNTHESIS:
  Top 3 Strengths: ...
  Top 3 Issues: ...
  Priority Fix: ...
```
