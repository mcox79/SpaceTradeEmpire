# Risk Meters (Heat / Influence / Trace) — Design Bible

> Design doc for the three-meter risk system: visual presentation, threshold feedback,
> world-state consequences, and the visual language of "the world is watching you."
> Companion to `HudInformationArchitecture.md` (screen zones) and `dynamic_tension_v0.md`.

## Why This Doc Exists

Three simultaneous risk meters are a game identity pillar. The GTA wanted level works
because it's one variable with one visual and escalating world-state responses (police
cars → helicopters → tanks). Three meters interacting is novel and needs explicit design
to avoid the RPG anti-pattern where "Bounty: 5000 gold" displays with zero perceptible
world change.

This doc defines what each meter means visually, what the player sees at each threshold,
and how the world responds — not just the number, but the EXPERIENCE of accumulating risk.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| RiskSystem in SimCore (lane incident generation) | Done | Deterministic, hash-roll against BPS thresholds |
| SecurityLaneSystem (delay/loss/inspection events) | Done | 4 risk bands: Low/Med/High/VeryHigh |
| Delay display in HUD | Done | Orange/red text, ETA with delay ticks |
| GetDelayStatusV0 / GetTravelEtaV0 bridge queries | Done | Nonblocking, cached |
| Heat meter visualization | Not implemented | No visual meter in HUD |
| Influence meter visualization | Not implemented | No visual meter in HUD |
| Trace meter visualization | Not implemented | No visual meter in HUD |
| Threshold-based world-state changes | Not implemented | Incidents are random, not tied to player risk level |
| Risk meter decay visualization | Not implemented | No visual decay indicator |
| Cross-meter compound effects | Not implemented | Meters are independent |
| Environmental feedback at thresholds | Not implemented | No NPC behavior changes, no visual ambience shifts |

---

## The Three Meters

### What Each Meter Represents

| Meter | Domain | Accumulates When | Decays When |
|-------|--------|-------------------|-------------|
| **Heat** | Commerce | Large trades, repeated routes, market manipulation | Time passes without trading at that market |
| **Influence** | Politics | Faction favors, territorial expansion, diplomatic actions | Time passes, completing faction missions |
| **Trace** | Stealth | Smuggling, operating in hostile territory, security incidents | Time passes in safe territory, paying fines |

### The Three-Meter Narrative

- **Heat** = "The merchants notice you." You're moving too much product through one market,
  and competitors are adjusting. High heat means worse prices, supply restrictions, and
  NPC trade competition focusing on your routes.

- **Influence** = "The factions notice you." You're becoming a political actor. High
  influence means factions react to your presence — friendly factions give better terms,
  hostile factions send patrols, neutral factions choose sides.

- **Trace** = "The authorities notice you." You've left evidence. High trace means
  inspections, lane patrols targeting you, and potential confiscation of contraband.
  The smuggler's meter.

---

## Design Principles

1. **Meters are consequences, not punishments.** Risk is the price of ambition. A player
   who never accumulates risk is playing too conservatively. The optimal play has moderate
   risk on at least one meter. High risk is dangerous but enables high reward. This is the
   EVE Online security trade-off: nullsec is dangerous but has the best resources.

2. **The world changes before the number does.** The player should FEEL rising risk through
   environmental feedback before checking the meter. More NPC patrols in your system
   (Trace). Worse buy prices at your favorite market (Heat). Faction envoys appearing at
   stations (Influence). The meter CONFIRMS what the player already suspects.

3. **Each meter has a distinct visual identity.** Three meters means three visual languages.
   They must be instantly distinguishable at a glance — different colors, different shapes,
   different animation patterns. If the player confuses Heat with Trace, the design has
   failed.

4. **Thresholds are named, not numbered.** Players don't think "my Heat is 3,200 out of
   10,000." Players think "my Heat is Elevated." Named thresholds with distinct visual
   states create memorable, communicable game states. GTA's star system works because
   "3 stars" means something to every player.

---

## Meter Visual Identity

### The Three Meters

```
┌─ RISK METERS (Bottom-Left, Zone G) ──────────────────────┐
│                                                            │
│  🔥 HEAT         ░░░░░░░░░░░░░░░░░░░░  Calm              │
│  ◆  INFLUENCE    ████░░░░░░░░░░░░░░░░  Noticed            │
│  👁 TRACE        ████████░░░░░░░░░░░░  Watched            │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

| Meter | Color | Icon | Bar Style |
|-------|-------|------|-----------|
| **Heat** | Orange → Red gradient | Flame / market symbol | Warm glow, flickers at high values |
| **Influence** | Purple → Magenta gradient | Diamond / crown | Solid fill, pulses at thresholds |
| **Trace** | Cyan → White gradient | Eye / radar sweep | Scanning line animation overlaid on bar |

### Threshold Names and Visual States

Each meter has 5 thresholds (matching the Pressure system's 5-state ladder):

| Level | Name | Fill % | Visual Treatment |
|-------|------|--------|------------------|
| 0 | **Calm** | 0-20% | Bar barely visible, muted color, no animation |
| 1 | **Noticed** | 20-40% | Bar visible, gentle color, subtle pulse |
| 2 | **Elevated** | 40-60% | Bar prominent, bright color, steady pulse |
| 3 | **High** | 60-80% | Bar flashing, intense color, warning border |
| 4 | **Critical** | 80-100% | Bar fully lit + glow halo, rapid pulse, screen-edge tint |

### Threshold Transitions

When a meter crosses a threshold, provide clear feedback:

```
HEAT: Calm → Noticed
  [Bar expands with brief flash]
  [Toast: "Heat: Noticed — merchants are adjusting to your presence"]
  [Subtle warm tint at screen edges for 1s]

TRACE: Elevated → High
  [Bar flashes with warning border]
  [Toast: "⚠ Trace: High — expect increased inspections"]
  [Scanner sweep sound effect]
  [Screen-edge cyan tint persists until decay]
```

---

## World-State Feedback Per Threshold

### Heat (Commerce Risk)

| Level | World Response | Player Perceives |
|-------|---------------|------------------|
| **Calm** | Normal market behavior | Nothing unusual |
| **Noticed** | Buy prices at frequently-traded markets increase 5% | Slightly worse deals |
| **Elevated** | NPC traders begin competing on your routes (+1 NPC convoy) | More trade traffic on your lanes |
| **High** | Supply at overtraded markets drops 20%, prices volatile | "Where did the ore go?" |
| **Critical** | Market temporarily closes to outsiders (1-day cooldown), prices spike | Forced route diversification |

### Influence (Political Risk)

| Level | World Response | Player Perceives |
|-------|---------------|------------------|
| **Calm** | Factions ignore you | No faction interactions |
| **Noticed** | Friendly factions offer 5% better terms | Slightly better deals with allies |
| **Elevated** | Hostile factions send patrol to your system (+1 patrol fleet) | "Why are Chitin patrols here?" |
| **High** | Neutral factions demand tribute or allegiance | Diplomatic popup: "Choose a side" |
| **Critical** | Hostile faction declares targeted embargo on player routes | Trade lanes to hostile territory blocked |

### Trace (Stealth Risk)

| Level | World Response | Player Perceives |
|-------|---------------|------------------|
| **Calm** | Normal lane travel | No incidents |
| **Noticed** | Inspection probability increases 2x | Occasional "halt for inspection" events |
| **Elevated** | Patrol fleets appear on your most-used routes | Armed ships at lane gates |
| **High** | Contraband confiscation on inspection (lose goods) | "They took my cargo!" |
| **Critical** | Active pursuit — hostile fleet dispatched to player's current system | "I need to run" |

---

## Meter Decay Visualization

Risk meters decay over time. The decay should be VISIBLE so the player understands
"if I lay low, this goes away."

### Decay Indicator

When a meter is decaying (player not generating new risk), show a downward arrow
and decay rate:

```
🔥 HEAT  ████████░░░░  Elevated  ↓ decaying (-5/tick)
```

When a meter is RISING (player actively generating risk), show an upward arrow:

```
👁 TRACE ████████████░  High  ↑ rising (+12 from inspection)
```

When stable (no change):

```
◆  INFLUENCE ████░░░░░░  Noticed  ── stable
```

### Decay Location Bonus

Decay should be faster in safe territory (player "lays low at home") and slower or
paused in hostile territory. Show this:

```
👁 TRACE ████████░░░░  Elevated  ↓ decaying (fast — Safe territory)
👁 TRACE ████████░░░░  Elevated  ── stable (Hostile territory — no decay)
```

---

## Cross-Meter Interactions

Three meters create compound risk. Define how they interact:

### Compound Threat Levels

| Combination | Effect | Visual |
|-------------|--------|--------|
| High Heat + High Trace | "Notorious Trader" — inspections focus on trade cargo | Both bars linked with a connecting line, pulsing |
| High Influence + High Trace | "Political Fugitive" — factions send hunter fleets | Red warning border around both bars |
| High Heat + High Influence | "Market Kingpin" — competitors AND factions react | Empire Dashboard shows special advisory |
| All three High+ | "Most Wanted" — all world responses active simultaneously | Full screen-edge warning tint, special status icon |

### Compound Visualization

When two or more meters are at Elevated+, show a compound threat indicator:

```
┌─ RISK METERS ──────────────────────────────────────────────┐
│                                                              │
│  🔥 ████████░░░░  Elevated ↑    ⚠ COMPOUND RISK: 2 meters │
│  ◆  ████████████  High     ──     "Market Kingpin"          │
│  👁 ░░░░░░░░░░░░  Calm     ──                               │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Screen-Edge Ambient Feedback

At High and Critical thresholds, the screen edges carry a persistent ambient tint
matching the meter's color:

| Meter at High+ | Screen Edge Effect |
|----------------|-------------------|
| Heat | Warm orange vignette at screen edges (subtle, 10% opacity) |
| Influence | Purple shimmer at screen edges (pulsing, 8% opacity) |
| Trace | Cyan scan-line at top of screen (sweeps left-to-right every 5s) |
| Two meters High+ | Both tints overlay, compound opacity (15%) |
| All three High+ | All tints + slight screen desaturation (muted colors = tension) |

These ambient effects follow the principle "the world changes before the number does."
The player FEELS the tension before checking the meters.

---

## HUD Placement (Zone G — Bottom)

Risk meters live in Zone G (bottom of screen), as defined in `HudInformationArchitecture.md`.

```
┌─────────────────────────────────────────────────────────────────────────┐
│  (Zone A: Status)                              (Zone B: Toasts)        │
│                                                                         │
│                                                                         │
│                          (gameplay area)                                │
│                                                                         │
│                                                                         │
├── ZONE G: BOTTOM ───────────────────────────────────────────────────────┤
│  🔥 ████░░░░  ◆ ████████░░  👁 ░░░░░░░░░░              [minimap]     │
│  Heat: Noticed  Influence: Elevated  Trace: Calm                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### Visibility Rules (Progressive Disclosure)

| Condition | Display |
|-----------|---------|
| All meters at Calm (0%) | Meters hidden entirely (Zone G empty) |
| Any meter > 0% | All three meters shown (even Calm ones, for context) |
| Any meter at High+ | Meters + threshold name + trend arrow |
| Any meter at Critical | Meters + screen-edge tint + toast warning |

When all meters are calm, the bottom bar is clean — no clutter for peaceful play.
Meters appear when they become relevant and persist until the player resolves the risk.

---

## Audio Feedback

(Cross-reference: `AudioDesign.md`)

| Threshold Crossed | Sound |
|-------------------|-------|
| Any meter → Noticed | Subtle notification chime (low urgency) |
| Any meter → Elevated | Warning tone (mid urgency, distinct from toast chime) |
| Any meter → High | Alert klaxon (brief, one-shot, not looping) |
| Any meter → Critical | Alarm + ambient drone shift (music layer becomes tense) |
| Any meter decays below Noticed | Relief chime (tension resolved) |

Each meter has a subtly different tone quality:
- Heat: warm, analog synth tone
- Influence: metallic, resonant bell
- Trace: digital, radar-like ping

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Number without world response** | RPGs with "Bounty: 5000g" and no NPC change | World responds at EVERY threshold — more patrols, worse prices, inspections |
| **Single risk meter** | GTA (works for GTA, not for a trade empire) | Three distinct meters for three risk domains |
| **Meters always visible** | Cluttered HUDs | Hidden when all calm, progressive disclosure |
| **Instant consequences** | Games where one mistake = game over | Graduated response: Calm→Noticed→Elevated→High→Critical. Time to react |
| **No decay visualization** | "Am I still in danger?" ambiguity | Arrows (↑/↓/──) and decay rate text |
| **Color-only differentiation** | Color-blind inaccessibility | Each meter has icon + color + name + unique animation pattern |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Wanted level escalation | GTA V | Named thresholds + escalating world response (stars + police + helicopters + tanks) |
| Stealth meter | Hitman (2016+) | Continuous notoriety bar that persists across missions, decays through specific actions |
| Multi-meter risk | No direct exemplar | Our unique design — three interacting meters is novel |
| World-state feedback | Assassin's Creed (notoriety) | Tear down posters to reduce notoriety — player ACTIONS reduce risk, not just time |
| Ambient tension | Dead Space | Screen-edge visual effects communicate danger without UI elements |
| Security gradient | EVE Online | Green→yellow→red = universal. Traffic-light intuition works instantly |

---

## SimBridge Queries — Existing and Needed

### Existing
| Query | Purpose |
|-------|---------|
| `GetDelayStatusV0(fleetId)` | Current delay state |
| `GetTravelEtaV0(fleetId, nodeId)` | Travel time with delay |

### Needed
| Query | Purpose |
|-------|---------|
| `GetRiskMetersV0()` | All three meter values (0-10000), threshold names, trend direction |
| `GetRiskDecayRateV0()` | Current decay rate per meter (affected by location) |
| `GetCompoundThreatV0()` | Compound threat level when multiple meters high |
| `GetRiskWorldEffectsV0()` | Active world-state modifications from current risk level |
| `GetRiskHistoryV0(ticks)` | Meter values over time for trend visualization |
