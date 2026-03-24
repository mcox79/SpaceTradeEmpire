# Dialogue JSON Schema

This document describes the JSON data files that hold all narrative dialogue text
for Space Trade Empire. These files are embedded resources in the SimCore assembly
and loaded at static initialization time.

## Files

| File | Purpose |
|---|---|
| `tutorial_dialogue_v0.json` | Tutorial dialogue: Ship Computer lines, FO hails, selection intros, wrong-station warnings, and all phase-specific tutorial lines |
| `fo_dialogue_v0.json` | First Officer dialogue: candidate profiles and trigger-based dialogue lines for the full game |

## tutorial_dialogue_v0.json

### Top-level fields

| Field | Type | Description |
|---|---|---|
| `version` | int | Schema version (currently 0) |
| `narrator_selection_prompt` | string | Text shown on the FO selection overlay after all 3 auditions |
| `ship_computer_lines` | array | Ship Computer system notification lines |
| `selection_intros` | array | Self-introduction quotes for the FO selection overlay |
| `fo_hail_lines` | array | FO hail lines spoken during Act 2 introduction |
| `wrong_station_lines` | array | FO warning lines when docking at a bad-profit station |
| `tutorial_lines` | array | All phase-specific tutorial dialogue lines |

### ship_computer_lines entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `phase` | string | Any `TutorialPhase` enum name | Tutorial phase this line plays at |
| `sequence` | int | 0+ | 0-based sequence within the phase (for multi-beat lines) |
| `text` | string | Any text | The dialogue text. May contain `{placeholders}` for runtime substitution |

### selection_intros entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `candidate` | string | `Analyst`, `Veteran`, `Pathfinder` | Which FO candidate speaks this intro |
| `quote` | string | Any text | The self-introduction quote |

### fo_hail_lines entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `candidate` | string | `Analyst`, `Veteran`, `Pathfinder` | Which FO candidate speaks |
| `text` | string | Any text | The hail dialogue text |

### wrong_station_lines entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `candidate` | string | `Analyst`, `Veteran`, `Pathfinder` | Which FO candidate speaks |
| `text` | string | Any text | Warning text. Use `{station}` placeholder for the suggested station name |

### tutorial_lines entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `phase` | string | Any `TutorialPhase` enum name | Tutorial phase this line plays at |
| `candidate` | string | `Analyst`, `Veteran`, `Pathfinder` | Which FO candidate speaks this variant |
| `sequence` | int | 0+ | 0-based sequence within the phase (for multi-beat lines) |
| `text` | string | Any text | The dialogue text |

## fo_dialogue_v0.json

### Top-level fields

| Field | Type | Description |
|---|---|---|
| `version` | int | Schema version (currently 0) |
| `candidate_profiles` | array | The three FO candidate personality profiles |
| `dialogue_lines` | array | All trigger-based FO dialogue lines |

### candidate_profiles entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `type` | string | `Analyst`, `Veteran`, `Pathfinder` | Candidate enum value |
| `name` | string | Character name | Display name (Maren, Dask, Lira) |
| `description` | string | Any text | Personality summary |
| `blind_spot` | string | Any text | Character flaw / blind spot |
| `endgame_lean` | string | `Reinforce`, `Naturalize`, `Renegotiate` | Which endgame path this FO favors |

### dialogue_lines entry

| Field | Type | Valid values | Description |
|---|---|---|---|
| `trigger` | string | Any trigger token | Event that fires this line (e.g., `FIRST_DOCK`, `ENDGAME_REINFORCE`) |
| `candidate` | string | `Analyst`, `Veteran`, `Pathfinder` | Which FO candidate speaks |
| `tier` | string | `Early`, `Mid`, `Fracture`, `Revelation`, `Endgame` | Minimum dialogue tier required |
| `text` | string | Any text | The dialogue text. May contain `{GOOD}`, `{STATION}` placeholders |
| `relationship_delta` | int | -5 to +5 | Relationship score change when this line fires |

## Enum reference

### FirstOfficerCandidate
- `None` (0) -- no candidate selected
- `Analyst` (1) -- Maren: probability-driven, dry humor
- `Veteran` (2) -- Dask: institutional, loyal to structures
- `Pathfinder` (3) -- Lira: warm, observational, comfortable with chaos

### DialogueTier
- `Early` (0) -- tick 0-300
- `Mid` (1) -- tick 300-600
- `Fracture` (2) -- tick 600-1000
- `Revelation` (3) -- tick 1000-1500
- `Endgame` (4) -- tick 1500+

### TutorialPhase (active phases only)
- `Awaken` (1), `Flight_Intro` (2), `First_Dock` (3)
- `Maren_Hail` (4), `Maren_Settle` (5), `Market_Explain` (6), `Buy_Prompt` (7), `Buy_React` (8)
- `Travel_Prompt` (9), `Arrival_Dock` (10), `Sell_Prompt` (11), `First_Profit` (12), `FO_Selection` (13)
- `World_Intro` (14), `Explore_Prompt` (15), `Cruise_Intro` (16), `Galaxy_Map_Prompt` (17)
- `Threat_Warning` (18), `Dask_Hail` (19), `Combat_Engage` (20), `Combat_Debrief` (21), `Repair_Prompt` (22)
- `Module_Intro` (23), `Module_Equip` (24), `Module_React` (25), `Lira_Tease` (26)
- `Automation_Intro` (27), `Automation_Create` (28), `Automation_Running` (29), `Automation_React` (30)
- `Module_Calibration_Notice` (32), `Jump_Anomaly` (33)
- `Mystery_Reveal` (41), `Graduation_Summary` (42), `FO_Farewell` (43), `Milestone_Award` (44)

## How to add a new line

### Adding a new tutorial line

1. Open `tutorial_dialogue_v0.json`
2. Add a new entry to the `tutorial_lines` array:
   ```json
   { "phase": "YOUR_PHASE", "candidate": "Analyst", "sequence": 0, "text": "Your dialogue text here." }
   ```
3. Most phases need 3 variants (one per candidate: Analyst, Veteran, Pathfinder)
4. If this is a multi-beat sequence, increment `sequence` (0, 1, 2...)
5. The `phase` must match a valid `TutorialPhase` enum value exactly

### Adding a new FO dialogue line

1. Open `fo_dialogue_v0.json`
2. Add a new entry to the `dialogue_lines` array:
   ```json
   { "trigger": "YOUR_TRIGGER_TOKEN", "candidate": "Analyst", "tier": "Early", "text": "Your dialogue text here.", "relationship_delta": 1 }
   ```
3. Each trigger typically needs 3 entries (one per candidate)
4. Choose `tier` based on when this trigger can first fire:
   - `Early`: tutorial and early game (tick 0-300)
   - `Mid`: mid-game systems revealed (tick 300-600)
   - `Fracture`: fracture mechanics active (tick 600-1000)
   - `Revelation`: midpoint turn revelations (tick 1000-1500)
   - `Endgame`: final act (tick 1500+)
5. Set `relationship_delta` based on emotional weight:
   - 1: routine observation
   - 2: notable moment
   - 3: significant revelation
   - 4-5: major story beat
   - Negative values: FO disagrees with player's choice

### Adding a new Ship Computer line

1. Open `tutorial_dialogue_v0.json`
2. Add to the `ship_computer_lines` array:
   ```json
   { "phase": "YOUR_PHASE", "sequence": 0, "text": "Cold, mechanical notification text." }
   ```
3. Ship Computer lines are impersonal -- no personality, no opinions

### Placeholders

Use curly braces for runtime substitution:
- `{GOOD}` -- replaced with a commodity name
- `{STATION}` -- replaced with a station name
- `{credits_earned}`, `{nodes_visited}`, `{combats_won}`, `{modules_equipped}` -- graduation stats

### Writing guidelines

- FOs **observe and react** -- they never give UI instructions (no keybinds)
- Keybind instructions belong in `GetObjectiveText()` (HUD objectives), not dialogue
- Cover-story naming: no "fracture"/"adaptation"/"ancient"/"organism" before Module Revelation tier
- Each candidate has a distinct voice: Analyst=data-driven, Veteran=institutional, Pathfinder=sensory/poetic
