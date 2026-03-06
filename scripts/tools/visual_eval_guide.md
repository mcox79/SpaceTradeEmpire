# Visual Evaluation Guide — Space Trade Empire

Use this guide when evaluating screenshots from the visual sweep bot.
Evaluate each screenshot against ALL applicable sections below.
Rate each category: PASS / NEEDS_WORK / FAIL with specific notes.

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
