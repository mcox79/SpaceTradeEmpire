# Main Menu & Game Shell — Design Bible

> Design doc for the main menu, settings, new game flow, save/load surface,
> and the out-of-game shell that wraps the play session.
> Companion to `AudioDesign.md` (first-launch silence), `NarrativeDesign.md`
> (narrative first impression), `dynamic_tension_v0.md` (campaign structure),
> `HudInformationArchitecture.md` (in-game UI conventions).
> Epic: TBD — not yet gated.

## Why This Doc Exists

The main menu is the game's first and last impression. Every session begins here
and most sessions end here. Games that treat the menu as a launcher (Starfield's
static placeholder) train players to rush past it. Games that treat the menu as
part of the world (Elite Dangerous's cockpit, Homeworld's mothership in the void)
begin the emotional experience before the player presses "Continue."

The game currently launches directly into gameplay with no main menu, no settings
surface, and no new-game flow. This doc designs all of those.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Main menu scene | Not implemented | Game boots straight to Main scene |
| Continue / New Game / Load | Not implemented | 3-slot save exists in pause menu only |
| Settings UI | Not implemented | No graphics, audio, or controls settings |
| Accessibility prompt | Not implemented | No first-launch accessibility flow |
| Credits screen | Not implemented | No credits |
| Background scene | Not implemented | No menu atmosphere |
| New Game wizard | Not implemented | No seed, difficulty, or mode selection |

---

## Design Principles

1. **The menu is the first act of the story.** The player's emotional journey
   begins at the title screen, not at tick 1. The void of space, the distant
   stars, the quiet — all of this says "you are small, the galaxy is vast, and
   something is waiting."

2. **One click to resume.** Returning players (the majority after launch week)
   need exactly one input to continue playing. "Continue" is always the first
   option when a save exists. The most common action has the shortest path.

3. **Settings before gameplay.** A player who needs larger text, colorblind
   mode, or inverted Y-axis must be able to configure these before entering
   gameplay. Accessibility settings are offered on first launch before anything
   else.

4. **Flat, not deep.** The main menu is a flat list of 5-6 items. Maximum two
   levels of depth to reach any setting. No nested submenus from the main screen.
   If the player has to think about where something is, the menu has failed.

5. **Audio is half the menu.** The title screen's music and silence design is
   as important as the visual composition. Cross-reference `AudioDesign.md` —
   the 2-second silence before the engine starts applies to the very first
   "New Game" press.

6. **No unskippable anything.** Splash screens, logos, and intros are skippable
   after first viewing. Returning players see the main menu within 2 seconds of
   launching the executable.

---

## Menu Structure

### Top-Level Flow

```
Launch executable
    │
    ├── First launch ever?
    │       ├── [Splash / logo sequence] (skippable)
    │       ├── [Accessibility Quick-Setup] (font size, colorblind, subtitles)
    │       └── Main Menu (New Game first)
    │
    └── Returning player?
            ├── [Splash auto-skipped]
            └── Main Menu (Continue first)
```

### Main Menu Items

The menu adapts based on whether saves exist:

**No saves exist (first launch):**

| # | Item | Action |
|---|------|--------|
| 1 | **New Voyage** | Open new game wizard |
| 2 | Settings | Open settings screen |
| 3 | Credits | Open credits scroll |
| 4 | Quit | Confirm → exit to desktop |

**Saves exist (returning player):**

| # | Item | Action |
|---|------|--------|
| 1 | **Continue** | Load most recent save, enter gameplay |
| 2 | New Voyage | Open new game wizard |
| 3 | Load Voyage | Open save slot browser |
| 4 | Settings | Open settings screen |
| 5 | Milestones | Open milestone/stats viewer |
| 6 | Credits | Open credits scroll |
| 7 | Quit | Confirm → exit to desktop |

**Naming convention**: "Voyage" instead of "Game" — reinforces the pilot fantasy.
The player embarks on voyages, not play sessions.

---

## Title Screen Atmosphere

### Visual Composition

The title screen is not a static image. It is a living scene that communicates
scale, loneliness, and latent danger.

**Layer stack (back to front):**

| Layer | Content | Motion | GPU Cost |
|-------|---------|--------|----------|
| 0 — Deep background | Dark gradient (near-black to deep blue) | Static | Negligible |
| 1 — Distant stars | Point stars, 200-400 particles | Very slow drift (0.1 px/s parallax) | Low |
| 2 — Nebula clouds | 2 noise-texture layers, colored per-seed | Slow scroll (0.3 px/s), different directions | Low (shader) |
| 3 — Mid-field stars | Brighter points, 50-80 particles | Medium drift (0.5 px/s parallax) | Low |
| 4 — Foreground element | A single object silhouette (station, derelict, gate) | Gentle rotation or bob | Low |
| 5 — Title text | Game title, understated, no glow | Fade in over 2s | Negligible |
| 6 — Menu items | Text list, left-aligned or centered | Fade in after title | Negligible |

**Parallax depth**: Layers 1-3 shift subtly on mouse movement (±5px max) to
create depth without distraction. Controller users see gentle auto-drift instead.

**Foreground element**: Changes based on game state:
- No saves: a lone gate floating in the void (the beginning)
- Mid-campaign save: the player's ship class silhouette (your ship, waiting)
- Completed campaign: a Haven starbase silhouette (you've been here before)

### Color Palette

The menu's color temperature reflects the game's emotional arc:

| State | Dominant Tone | Accent | Feeling |
|-------|---------------|--------|---------|
| No saves | Cool blue-black | Faint cyan gate glow | Vast, unknown, inviting |
| Active campaign | Warm blue-black | Amber ship running lights | Familiar, welcoming back |
| Completed campaign | Deep indigo | Soft gold Haven glow | Accomplishment, mystery remains |

### Title Treatment

- **Game title**: Clean sans-serif or thin serif. No effects, no glow, no
  animation beyond the initial fade-in. The restraint communicates confidence.
- **Subtitle** (if any): Smaller, below title, italic. Could be a rotating
  ancient fragment quote that changes per session.
- **No tagline on-screen.** The atmosphere IS the tagline.

---

## Audio at the Menu

Cross-reference: `AudioDesign.md` — Silence Palette, Layer Architecture.

### First Launch (No Saves)

| Time | Audio | Visual |
|------|-------|--------|
| 0.0s | **Silence.** | Black screen |
| 0.5s | Silence continues | Stars begin fading in (Layer 1) |
| 1.5s | A single, low sustained note (cello or synth pad, -30 dB) | Nebula layers visible |
| 3.0s | Note sustains. Faint space drone fades in (-35 dB) | Title text fades in |
| 4.0s | A second note joins (minor interval — tension, not resolution) | Menu items appear |
| 6.0s+ | Ambient menu theme at full level (-28 dB). Sparse, evolving. | Fully interactive |

**The void, then you're alive.** The 2-second silence at launch is sacred.
The player should feel the emptiness before the game fills it.

### Returning Player

| Time | Audio | Visual |
|------|-------|--------|
| 0.0s | Space drone fades in immediately (-30 dB) | Scene visible quickly |
| 0.5s | Menu theme at -28 dB (no dramatic build) | Title + menu items visible |

Returning players don't need the dramatic entrance. They are coming home.
The audio acknowledges this by skipping the silence build-up.

### Menu Interaction Sounds

| Action | Sound | Character |
|--------|-------|-----------|
| Hover menu item | Faint tick (UI layer, -18 dB) | Precise, minimal |
| Select menu item | Confirm chime (UI layer, -12 dB) | Short rising tone |
| Back / Cancel | Soft descending note | Brief, non-punishing |
| Start new voyage | Engine ignition sequence (1.5s) | Crescendo into loading |
| Continue | Quick engine hum swell (0.5s) | Welcoming, efficient |

---

## New Voyage Wizard

The new game flow is a single screen (not multi-page). All options visible,
sensible defaults pre-selected, one "Launch" button.

### Parameters

| Parameter | Type | Default | Options | Notes |
|-----------|------|---------|---------|-------|
| **Captain Name** | Text input | "Captain" | Free text (20 char max) | Used in logs, intel, faction dialogue |
| **Galaxy Seed** | Text/numeric | Random | Free text or "Randomize" button | Displayed as hex string. Determines warfront layout, resource distribution, faction territories |
| **Difficulty** | 3-option selector | Standard | Lenient / Standard / Unforgiving | See difficulty section below |
| **Save Slot** | 3-slot picker | First empty slot | Slot 1-3 | Shows existing save metadata if slot occupied (with overwrite warning) |

**What is NOT configurable**: Galaxy size, number of factions, number of warfronts,
starting location algorithm. These are tuned constants. Exposing them creates
balance-breaking edge cases and analysis paralysis. The seed handles variety.

### Difficulty Modes

Difficulty adjusts economic pressure and combat lethality, not content access.
All content is available in all modes. No difficulty-gated achievements.

| Mode | Sustain Drain | Tariff Multiplier | Combat Damage Taken | Trace Decay | Target Player |
|------|--------------|-------------------|--------------------|--------------|----|
| **Lenient** | 0.5x | 0.75x | 0.7x | 1.5x | First space game, wants the story |
| **Standard** | 1.0x | 1.0x | 1.0x | 1.0x | Core audience, balanced challenge |
| **Unforgiving** | 1.5x | 1.25x | 1.3x | 0.7x | Veteran, wants the dual doom clock to bite |

**No ironman/permadeath at launch.** This is a 15-30 hour campaign. Permadeath
in a game this long is niche. If players request it post-launch, add it as a
fourth mode.

### Difficulty Descriptions (In-Menu Text)

- **Lenient**: "The galaxy is dangerous, but forgiving. Sustain costs are halved
  and combat is gentler. Recommended for players who want to explore the story
  at their own pace."
- **Standard**: "The galaxy is indifferent to your survival. This is the intended
  experience — economic pressure is real, combat is lethal, and every decision
  matters."
- **Unforgiving**: "The galaxy is hostile. Sustain bleeds faster, tariffs cut
  deeper, and Trace lingers longer. For players who want every voyage to feel
  like a knife's edge."

---

## Save/Load System

### Current State

The pause menu (Esc) has 3 save slots with timestamp metadata. This works but
lacks the polish expected of a shipping title.

### Enhanced Save Slot Design

Each save slot displays:

| Field | Source | Example |
|-------|--------|---------|
| **Slot number** | Fixed | "Voyage 1" |
| **Captain name** | Save data | "Captain Vasquez" |
| **Screenshot thumbnail** | Captured at save time | 320×180 image |
| **Play time** | Accumulated session time | "12h 34m" |
| **Credits** | SimState snapshot | "42,380 cr" |
| **Current system** | SimState snapshot | "Proxima Station" |
| **Difficulty** | Save data | "Standard" |
| **Real-world date** | File metadata | "2026-03-09 14:22" |
| **Game version** | Build metadata | "v0.8.2" |

### Save Slot States

| State | Display | Actions Available |
|-------|---------|-------------------|
| **Empty** | "Empty Slot" + "Start New Voyage" prompt | New Voyage (pre-selects this slot) |
| **Occupied** | Full metadata card with thumbnail | Continue / Load / Delete |
| **Auto-save** | Labeled "Auto-Save" with lock icon | Load only (no delete, no overwrite) |

### Save Behaviors

- **Manual save**: From pause menu (Esc → Save to Slot N). Overwrites with
  confirmation if slot occupied.
- **Auto-save**: Triggers on: docking at station, completing a warp transit,
  completing a mission step. Writes to a reserved auto-save slot (separate from
  the 3 manual slots).
- **Save and Quit**: Single action from pause menu. Saves to most recent slot,
  then returns to main menu. This is a first-class action, not "save, then
  separately quit."
- **Quit without saving**: Requires confirmation dialog showing time since last
  save: "You have 8 minutes of unsaved progress. Quit anyway?"

### Load Flow

From main menu "Load Voyage":
1. Show all 4 slots (3 manual + 1 auto-save) as cards with metadata
2. Select slot → confirm → load
3. No confirmation dialog for loading (loading is never destructive)
4. Loading transition: fade to black → progress indicator → "Press any key"

---

## Settings Screen

### Category Tabs

Settings are organized into 4 tabs, accessible from main menu and pause menu.

#### Tab 1: Gameplay

| Setting | Type | Default | Range |
|---------|------|---------|-------|
| Difficulty | Selector | (per save) | Lenient / Standard / Unforgiving |
| Auto-pause on focus loss | Toggle | On | On / Off |
| Tutorial toasts | Toggle | On | On / Off |
| Edge-scroll camera (mouse) | Toggle | Off | On / Off |
| Tooltip delay | Slider | 0.5s | 0.0–2.0s |
| Language | Selector | English | (future: localization list) |

#### Tab 2: Display

| Setting | Type | Default | Range |
|---------|------|---------|-------|
| Display mode | Selector | Borderless Windowed | Fullscreen / Windowed / Borderless |
| Resolution | Selector | Native | (detected resolutions) |
| V-Sync | Toggle | On | On / Off |
| Max FPS | Selector | Unlimited | 30 / 60 / 120 / 144 / Unlimited |
| Anti-aliasing | Selector | FXAA | Off / FXAA / MSAA 2x / MSAA 4x |
| Quality preset | Selector | High | Low / Medium / High |
| Bloom | Toggle | On | On / Off |
| Star particle density | Slider | 100% | 25–100% |
| UI scale | Slider | 100% | 75–200% |

**Resolution revert timer**: Changing resolution shows a 15-second countdown
dialog: "Keep this resolution? Reverting in 15s..." If the player can't see
the dialog (bad resolution), it auto-reverts.

#### Tab 3: Audio

| Setting | Type | Default | Range |
|---------|------|---------|-------|
| Master volume | Slider | 80% | 0–100% |
| Music volume | Slider | 70% | 0–100% |
| SFX volume | Slider | 80% | 0–100% |
| Ambient volume | Slider | 60% | 0–100% |
| UI volume | Slider | 70% | 0–100% |

Maps directly to the 5-bus architecture in `AudioDesign.md`.

#### Tab 4: Accessibility

| Setting | Type | Default | Range |
|---------|------|---------|-------|
| Colorblind mode | Selector | Off | Off / Deuteranopia / Protanopia / Tritanopia |
| High contrast UI | Toggle | Off | On / Off |
| Reduced screen shake | Toggle | Off | On / Off |
| Font size override | Slider | 100% | 100–200% |
| HUD opacity | Slider | 100% | 50–100% |
| Hold-to-confirm (for destructive actions) | Toggle | On | On / Off |

### Settings Behaviors

- **Auto-save on change.** No "Apply" button. Every setting takes effect
  immediately when changed. This is the modern standard.
- **Accessible from both main menu and pause menu.** Identical UI in both
  locations. Settings persist globally (not per-save), except Difficulty which
  is per-save.
- **Reset to defaults** button per tab, with confirmation.
- **Keybinds**: Defer to a future gate. Current keybinds are hardcoded. When
  implemented, keybinds get their own sub-screen under Gameplay or a 5th tab.

---

## Milestones Screen

Accessible from main menu when saves exist. Shows aggregate progression
across all save slots.

### Content

Cross-reference: `MilestoneContentV0.cs` — 8 milestone definitions, `PlayerStats.cs`.

| Section | Content |
|---------|---------|
| **Milestones achieved** | Grid of milestone cards (First Trade, Explorer, Merchant, etc.). Achieved = full color + date. Unachieved = silhouette + "???" |
| **Lifetime stats** | Aggregate: total play time, total credits earned, total goods traded, systems visited, voyages completed |
| **Per-voyage stats** | Select a save slot to see its individual stats |

**No gameplay-affecting unlocks.** Milestones are recognition, not progression
gates. No "unlock the Cruiser by reaching Tycoon." All content is available in
every voyage.

---

## Credits Screen

A slow vertical scroll over the menu background scene. Music continues from
the menu theme (no separate credits track at this stage).

### Sections (in order)

1. **Game title**
2. **Design & Development** (names/roles)
3. **Audio** (composer/sound designer credits)
4. **Special Thanks** (playtesters, community)
5. **Tools & Technology** (Godot Engine, .NET, etc.)
6. **Legal** (licenses, disclaimers)

**Skippable** with any input. Pressing Esc or clicking returns to main menu.

---

## First-Launch Accessibility Prompt

On the very first launch (no settings file exists), before the main menu
appears, a single-screen prompt offers the three most impactful accessibility
settings:

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│       Welcome to Space Trade Empire.                 │
│                                                      │
│   Before you begin, would you like to adjust         │
│   any of these settings?                             │
│                                                      │
│   Font Size:     [100%] ─────●───── [200%]           │
│                                                      │
│   Colorblind:    ( ) Off                             │
│                  ( ) Deuteranopia (red-green)         │
│                  ( ) Protanopia (red-green)           │
│                  ( ) Tritanopia (blue-yellow)         │
│                                                      │
│   UI Scale:      [100%] ─────●───── [200%]           │
│                                                      │
│           [ Continue to Main Menu ]                   │
│                                                      │
│   (You can change these anytime in Settings)          │
│                                                      │
└──────────────────────────────────────────────────────┘
```

**This screen is never shown again.** It exists for one purpose: ensure players
who need accessibility features can set them before encountering any UI that
requires default settings to navigate.

---

## Loading & Transitions

### Menu → Gameplay Transition

| Trigger | Transition | Duration |
|---------|-----------|----------|
| **Continue** | Quick fade to black → load → fade in on ship | 1-2s |
| **New Voyage** | Engine ignition sound → fade to black → galaxy generation → "Press any key to begin" → fade in | 3-5s |
| **Load Voyage** | Fade to black → load → fade in on ship | 1-2s |

### Galaxy Generation Screen (New Voyage Only)

When the player starts a new voyage, the generation screen shows:

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│              Charting the void...                     │
│                                                      │
│   ░░░░░░░░░░░░░░░░░████████░░░░░░░░░░  63%          │
│                                                      │
│   Seeding star systems...                            │
│   Establishing trade threads...                        │
│   Igniting warfronts...                              │
│                                                      │
└──────────────────────────────────────────────────────┘
```

Progress text updates as generation phases complete. The messages are thematic
("Igniting warfronts" not "Initializing WarfrontDemandSystem"). Background is
the parallax star scene, same as menu.

After generation completes:

```
        Press any key to begin your voyage.
```

This "press any key" gate gives the player control of the transition moment.
They enter gameplay when they're ready, not when the loading bar fills.

---

## Pause Menu (In-Game)

The existing pause menu (Esc) is enhanced to match the main menu's design:

| # | Item | Action |
|---|------|--------|
| 1 | **Resume** | Unpause, close menu |
| 2 | Save | Save to slot picker (3 slots) |
| 3 | Load | Load from slot picker (3 manual + 1 auto) |
| 4 | Settings | Same settings screen as main menu |
| 5 | Save and Quit | Save to current slot → return to main menu |
| 6 | Quit to Menu | Confirm (shows time since last save) → main menu |

**"Save and Quit" is a single action.** Not "save, then quit." The player
should never have to do two things when they want to stop playing.

**Quit warning**: "You have X minutes of unsaved progress. Save and quit?"
with three options: [Save and Quit] / [Quit Without Saving] / [Cancel].

---

## Keyboard / Controller Navigation

### Keyboard

| Key | Action |
|-----|--------|
| Up/Down arrows | Navigate menu items |
| Enter | Select highlighted item |
| Escape | Back / Cancel (from any submenu) |
| Any key | Dismiss "Press any key" prompts |

### Mouse

| Action | Effect |
|--------|--------|
| Hover | Highlight menu item |
| Click | Select menu item |
| Right-click | Back (in submenus) |

### Controller (Future)

| Button | Action |
|--------|--------|
| D-pad / Left stick | Navigate |
| A / Cross | Select |
| B / Circle | Back |

All menus must work with both keyboard and mouse from day one. Controller
support is a future gate but the menu structure should not require mouse-only
interactions (no drag-only sliders, no hover-only tooltips).

---

## Technical Implementation Notes

### Scene Structure

```
MainMenu (Control)
├── BackgroundScene (SubViewportContainer)
│   └── SubViewport
│       ├── StarfieldBG (parallax layers 0-3)
│       └── ForegroundElement (3D or 2D silhouette)
├── TitleLabel
├── MenuList (VBoxContainer)
│   ├── MenuItem_Continue
│   ├── MenuItem_NewVoyage
│   ├── MenuItem_LoadVoyage
│   ├── MenuItem_Settings
│   ├── MenuItem_Milestones
│   ├── MenuItem_Credits
│   └── MenuItem_Quit
├── SettingsPanel (hidden by default)
├── NewVoyagePanel (hidden by default)
├── LoadVoyagePanel (hidden by default)
├── MilestonesPanel (hidden by default)
├── CreditsPanel (hidden by default)
└── AccessibilityPrompt (first launch only)
```

### Godot Integration

- Main menu is a separate scene loaded before `Main.tscn`
- Project Settings → Run → Main Scene = `main_menu.tscn`
- "Continue" and "Load" trigger `get_tree().change_scene_to_file("res://Main.tscn")`
  after loading save data via SimBridge
- Pause menu remains in `hud.gd` (it's an in-game overlay, not a separate scene)
- Settings file: `user://settings.json` — loaded at boot, persists across sessions
- First-launch detection: check for `user://settings.json` existence

### SimBridge Interaction

The main menu does NOT start the sim. SimBridge initializes only when
`Main.tscn` loads. The menu scene is pure Godot/GDScript — no C# dependency.

Save/load flow:
1. Menu calls `SimBridge.LoadSaveV0(slot)` after scene change
2. SimBridge deserializes save data into SimState
3. GameManager reads SimState and positions player

New voyage flow:
1. Menu passes seed + difficulty to `Main.tscn` via autoload globals
2. SimBridge initializes fresh SimState with the provided seed
3. Difficulty multipliers are stored in SimState and read by all systems

---

## Anti-Patterns to Avoid

| Anti-Pattern | Example | Our Rule |
|---|---|---|
| **Static placeholder menu** | Starfield (widely criticized) | Living parallax scene with atmospheric audio |
| **Unskippable splash screens** | Every Ubisoft launch | Skippable after first view, auto-skip on return |
| **No "Continue" button** | Older games requiring Load → find save → click | Continue is always item #1 when saves exist |
| **Settings require "Apply"** | Legacy pattern | Auto-save every setting on change |
| **Accessibility buried in submenus** | Most games pre-2020 | First-launch prompt + dedicated Settings tab |
| **Deep menu nesting** | X4 Foundations | Max 2 levels from main screen |
| **Form over function** | Starfield's stylish-but-confusing menus | Menu is a tool. Clarity over aesthetics |
| **Quit requires 4+ clicks** | Assassin's Creed (original) | Max 2 clicks: Quit → Confirm |
| **Save without metadata** | "Slot 1 - 03/09/2026" tells nothing | Screenshot + captain + system + playtime + credits |
| **No time-loss warning on quit** | "Unsaved progress will be lost" (how much?) | Show exact minutes since last save |

---

## Reference Games

| Game | What It Does Well | What We Take |
|------|------------------|--------------|
| **Homeworld** | Mothership in the void + Adagio for Strings = profound emotional tone from menu alone | Single foreground silhouette against vast emptiness. Music as emotional architecture |
| **Elite Dangerous** | Menu rendered inside your actual ship/station — you are "returning to your cockpit" | Foreground element adapts to game state (ship class silhouette for returning players) |
| **FTL** | 5 items, no nesting, pixel-art nebula. Clean, humble, music does the work | Flat menu list. Let the soundtrack carry the atmosphere |
| **Outer Wilds** | Stars dispersing from constellation. Developer names as constellation. Melancholic banjo/guitar theme | Subtle title animation. Understated is more memorable than flashy |
| **Stellaris** | Continue at top. Animated galaxy with parallax. Galaxy generation screen shows the cosmos forming | Dynamic first item. Parallax star layers. Thematic generation messages |
| **Civilization VI** | Leader animation behind menu. "Sogno di Volare" score. Resume Game prominent | Living background scene. Music inseparable from the experience |
| **Subnautica** | Underwater ambience with real hydrophone recordings. Saves show metadata + thumbnails | Audio atmosphere from frame 1. Rich save slot metadata |
| **No Man's Sky** | Mode-select with one auto-save per mode — essentially one-click continue per mode | Simplicity of the continue flow |

---

## Narrative Integration

The main menu participates in the narrative design (see `NarrativeDesign.md`):

| Menu Element | Narrative Function |
|---|---|
| **Foreground silhouette** | Foreshadowing: the gate (beginning), the ship (journey), the Haven (revelation) |
| **Subtitle quote** | Rotating ancient fragment — a different line each session. Player never sees them all. Mystery through accumulation |
| **Generation messages** | "Seeding fracture topology..." — the lore is embedded in the loading text. The player reads world-building while waiting |
| **Milestone names** | "First Trade," "Pathfinder," "Tycoon" — progression language that reinforces the pilot fantasy |
| **Difficulty descriptions** | Written in-universe: "The galaxy is indifferent to your survival" — not "Normal difficulty" |
| **Silence at first launch** | The void is real. Before the game exists, there is nothing. Then there is a sound, and you exist |

---

## Version History

- v0 (2026-03-09): Initial document. Menu structure, atmosphere, save/load, settings, accessibility, new game wizard, technical notes.
