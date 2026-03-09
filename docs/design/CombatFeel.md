# Combat Feel & Juice — Design Bible

> Design doc for combat feedback: what the player sees, hears, and feels when damage
> is dealt and received. Covers hero ship combat, fleet-vs-fleet resolution, and the
> visual/audio language that makes combat readable and visceral.
> Companion to `ship_modules_v0.md` (weapon/module catalog) and `AudioDesign.md`.

## Why This Doc Exists

Combat feel is the system most harmed by ad-hoc design. Each individual decision — screen
shake magnitude, hit flash duration, damage number color — seems trivial in isolation.
But they compound into either "this feels amazing" or "this feels like spreadsheet math."
The #1 complaint across space combat games is not balance or depth — it is "fighting
the interface instead of fighting ships" (X4 Foundations community consensus).

This doc defines the feedback stack: what happens on EVERY channel (visual, audio, UI,
camera) for EVERY combat event (hit, shield break, kill, player damage). Every future
gate that touches combat visual effects references this doc.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| 4 damage families (Kinetic/Energy/Neutral/PointDefense) | Done | Counter-family multipliers |
| Zone armor system (4 facing zones, stance-based distribution) | Done | Fore/Port/Starboard/Aft |
| Bullet projectile with trail | Done | Cyan (player), orange/red (AI) |
| Bullet impact particles (12 bursts, 0.25s) | Done | Color-matched to shooter |
| Screen shake on turret fire (0.15) and hit received (0.4) | Done | Via player_follow_camera |
| Combat audio pool (8 fire + 8 impact, 3D spatial) | Done | Pool exists but NEVER CALLED |
| HUD hull/shield bars (red/blue, numeric) | Done | Per-frame update |
| Combat log panel (L key, last 20 events) | Done | Gold=player, red=AI |
| Strategic resolver (50-round attrition, zone-aware) | Done | SHA256 replay verification |
| NPC HP bar (3D billboard, green) | Done | Distance-gated at 40m |
| Shield impact ripple effect | Not implemented | No visual distinction shield vs hull hit |
| Damage numbers floating text | Not implemented | No per-hit numeric feedback |
| Kill explosion VFX | Not implemented | Ships just disappear |
| Shield break flash | Not implemented | No "shields down" moment |
| Zone damage visualization | Not implemented | 4 zones exist but aren't shown |
| Weapon trail differentiation by family | Not implemented | All bullets look the same |
| Combat music crossfade | Done | Calm→combat transition on hostile proximity |
| Death screen | Done but disabled | "GAME OVER" red text, press R to restart |

---

## Design Principles

1. **Every hit must register on three channels.** Visual (particle + flash), audio
   (impact sound), and UI (health bar reaction + optional damage number). If any channel
   is missing, the hit feels phantom. Starsector achieves this perfectly: flux bars jump,
   shields ripple, and audio confirms — you can assess combat state at a glance.

2. **Distinguish shield hits from hull hits.** These are the two most important combat
   states and the player must know which is happening WITHOUT reading numbers. Shield
   hit = blue/white ripple + electric crackle sound. Hull hit = orange/red spark +
   metallic impact sound. This follows the FTL principle: visual feedback communicates
   system state before the player reads any text.

3. **Damage families must look different.** Kinetic (150% vs hull) and Energy (150% vs
   shields) are tactical choices. If they look identical, the tactical depth is invisible.
   Kinetic = heavy, chunky impacts with screen shake. Energy = bright, crackling impacts
   with glow. This teaches the damage model through feel, not tooltips.

4. **Kill confirmation is a moment.** When a ship is destroyed, the player must know it
   happened and feel the weight. A ship disappearing with no effect is the anti-pattern.
   Explosion VFX + audio + loot drop marker + brief slowdown (hitstop) = the
   "confirmation receipt" that the player's action had consequences.

5. **Combat readability scales with chaos.** One-on-one combat should be fully readable
   in real-time. Fleet combat (5v5+) should use simplified indicators (aggregate HP bars,
   win/loss probability) because individual hits are too fast to track. This is the
   FTL insight: pause-to-plan for complexity, real-time for visceral moments.

---

## The Feedback Stack

### Event: Player Fires Turret

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Cyan bullet projectile spawns | Bullet trail lingers 0.3s, muzzle flash at turret point |
| **Audio** | `_sfx_turret_fire` plays | Weapon-family-specific sound (see Audio section) |
| **Camera** | 0.15 shake | Directional recoil (camera nudges opposite to fire direction) |
| **UI** | None | Cooldown indicator near crosshair (subtle arc sweep) |

### Event: Player Hit Lands on Enemy Shield

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Cyan/green particle burst (12 particles, 0.25s) | Shield ripple effect (blue hexagonal flash at impact point, fades 0.3s) |
| **Audio** | `bullet_hit.wav` (3D positioned) | Electric crackle + shield absorption tone |
| **Camera** | None | None (hits on enemies don't shake player camera) |
| **UI** | Enemy HP bar updates | Floating damage number (blue text, "-8", drifts upward 0.5s) |

### Event: Player Hit Lands on Enemy Hull

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Orange/red particle burst | Spark shower (orange/yellow, more particles, debris chunks) |
| **Audio** | `bullet_hit.wav` | Metallic crunch + hull stress groan |
| **Camera** | None | None |
| **UI** | Enemy HP bar updates | Floating damage number (orange text, "-12", larger than shield hits) |

### Event: Player Receives Damage (Shield)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Particle burst at impact | Blue screen-edge flash (vignette pulse, 0.2s) |
| **Audio** | `play_hit_sfx_v0()` | Shield absorption hum + warning tone if <25% |
| **Camera** | 0.4 shake | 0.4 shake + slight color desaturation pulse |
| **UI** | Shield bar decreases | Shield bar flashes white momentarily, number pulses |

### Event: Player Receives Damage (Hull)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Particle burst | Red screen-edge flash (damage vignette), intensity scales with damage |
| **Audio** | `play_hit_sfx_v0()` | Heavy metallic impact + hull stress creak |
| **Camera** | 0.4 shake | 0.6 shake (hull hits feel heavier than shield hits) |
| **UI** | Hull bar decreases | Hull bar flashes red, warning klaxon below 25% |

### Event: Enemy Shield Breaks (→ 0)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | None | Blue-white flash burst (entire ship briefly glows), electric discharge particles |
| **Audio** | None | Shield collapse crack (sharp, distinctive "pop") |
| **Camera** | None | Brief 0.1s slowdown (hitstop) — marks the tactical shift |
| **UI** | Shield bar empties | "SHIELDS DOWN" text flash on enemy HP bar (0.5s) |

### Event: Player Shield Breaks

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | None | Full-screen blue flash + shield-down vignette |
| **Audio** | None | Alarm klaxon + "shields offline" (if voice lines exist) |
| **Camera** | None | 0.8 shake (the biggest shake — this is the "oh no" moment) |
| **UI** | Shield bar empties | Shield bar turns red, flashing "SHIELDS DOWN" warning |

### Event: Enemy Destroyed

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Ship mesh disappears | Explosion VFX (fireball + debris chunks + lingering smoke, 1.5s) |
| **Audio** | None | `explosion.wav` (asset exists, unused) + distant rumble |
| **Camera** | None | 0.2s hitstop (time slows to 0.3x) + 0.5 shake |
| **UI** | NPC removed from scene | Kill confirmation text ("DESTROYED" + loot summary), combat log entry |
| **World** | Fleet removed, respawn queued | Loot drop marker spawns at wreck location |

### Event: Player Destroyed

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | "GAME OVER" red text (disabled) | Explosion VFX on player ship, camera pulls back to wide shot |
| **Audio** | None | Explosion + silence (engines die, music fades) |
| **Camera** | None | Camera detaches, slow orbit around wreck debris |
| **UI** | "Press R to Restart" | Death summary (damage taken, killer ID, survival time), [Restart] [Load Save] |

---

## Damage Family Visual Language

Each damage family should have a distinct visual identity so the player learns the
damage model through feel rather than reading tooltips.

| Family | Projectile Trail | Impact Effect | Sound Character | Screen Shake |
|--------|-----------------|---------------|-----------------|-------------|
| **Kinetic** | Short, thick, white-yellow trail | Heavy sparks, debris chunks, smoke puff | Deep THUD + metallic crunch | 1.2x base (heavy) |
| **Energy** | Long, thin, colored beam trail | Electric crackle, bright flash, no debris | High ZAP + electric sizzle | 0.8x base (light) |
| **Neutral** | Medium trail, default color | Standard particle burst | Balanced POP | 1.0x base |
| **PointDefense** | Rapid thin streaks | Small flash, fast dissipation | Rapid TAP-TAP-TAP | 0.5x base (minimal) |

### Teaching the Damage Model Through Feel

The player should FEEL that kinetic weapons are better against hull (heavy, chunky
impacts that feel like they're tearing metal) and energy weapons are better against
shields (bright, electric impacts that feel like they're overloading electronics).

When kinetic hits shields (50% effectiveness), the impact should feel "deflected" —
smaller particles, higher-pitched bounce sound. When energy hits hull (50% effectiveness),
the impact should feel "dispersed" — scattered glow, weak crackle.

This is the Star Citizen design principle: shield behavior per damage type is
communicated through visual feedback, not just numbers.

---

## Zone Armor Visualization

The zone armor system (Fore/Port/Starboard/Aft) exists in SimCore but is invisible
to the player. Making it visible creates tactical depth.

### Ship Status Display (Aspiration)

```
┌─ COMBAT HUD ──────────────────────────┐
│                                         │
│         ╭── FORE: 25/25 ──╮           │
│        ╱                    ╲          │
│  PORT ║   [ship silhouette]  ║ STBD   │
│  20/20 ║                    ║ 20/20   │
│        ╲                    ╱          │
│         ╰── AFT: 15/15 ───╯           │
│                                         │
│  Shield: ████████████ 50/50            │
│  Hull:   ████████████ 100/100          │
│                                         │
│  Stance: BROADSIDE (35% sides)         │
└─────────────────────────────────────────┘
```

Each zone HP bar is positioned around a ship silhouette. As zones take damage, their
bars deplete. A depleted zone is highlighted in red — hits to that facing go directly
to hull, which creates tactical incentive to present undamaged zones.

### Combat Stance Feedback

The current stance (Charge/Broadside/Kite) determines which zones get hit most. Show
this to the player:

```
CHARGE stance: ████ Fore gets 50% of hits
BROADSIDE:     ██ sides get 35% each
KITE stance:   ████ Aft gets 60% of hits
```

---

## Fleet Combat Readability

Hero ship combat is real-time and visceral. Fleet-vs-fleet (strategic resolver) is
faster and needs a different readability approach.

### Real-Time Hero Combat
- Individual hit effects on all channels (as defined in feedback stack above)
- One enemy at a time (or small groups)
- Player aims, fires, dodges manually

### Strategic Fleet Combat (Resolver)
- 50-round attrition resolved in <1 second
- Too fast for individual hit effects
- Show: aggregate result summary with replay option

```
┌─ COMBAT RESOLUTION ──────────────────────────────────────┐
│                                                            │
│  VANGUARD vs. CHITIN RAIDER                               │
│                                                            │
│  ██████████████████████░░░░  Your fleet: 78% hull         │
│  ░░░░░░░░░░░░░░░░░░░░░░░░  Enemy fleet: DESTROYED        │
│                                                            │
│  Rounds: 12/50                                             │
│  Outcome: VICTORY                                          │
│                                                            │
│  Damage dealt:  245 (Kinetic: 180, Energy: 65)            │
│  Damage taken:   42 (Hull: 22, Shield absorbed: 20)       │
│                                                            │
│  Salvage: Metal ×3, Components ×1, +200 credits           │
│                                                            │
│  [View Replay]  [Collect Loot]  [Continue]                │
└────────────────────────────────────────────────────────────┘
```

### Replay (Optional Aspiration)
The strategic resolver already captures `ReplayFrame` data per round. A replay viewer
could show round-by-round HP progression as an animated bar chart:

```
Round 1:  ████████████████████  vs  ████████████████████
Round 5:  ████████████████░░░░  vs  ██████████░░░░░░░░░░
Round 8:  ███████████████░░░░░  vs  ████░░░░░░░░░░░░░░░░
Round 12: █████████████░░░░░░░  vs  DESTROYED
```

---

## Screen Shake Intensity Scale

Define a consistent shake scale referenced by all combat events:

| Intensity | Value | Used For |
|-----------|-------|----------|
| Micro | 0.05 | PointDefense fire, minor UI feedback |
| Light | 0.15 | Turret fire (current), weapon recoil |
| Medium | 0.40 | Damage received (shield) (current), warp arrival |
| Heavy | 0.60 | Damage received (hull), warp exit |
| Critical | 0.80 | Shield break, near-death hit |
| Catastrophic | 1.00 | Player destruction, nearby explosion |

**Shake decay:** 0.15s duration, exponential falloff. Never sustained — shake is a
punctuation mark, not a constant state.

**Shake frequency:** 12-15 Hz (perceptible but not nauseating).

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Ships vanish on death** | Many indie space games | Explosion VFX + debris + loot marker + hitstop |
| **Shield/hull hits look identical** | X4 Foundations | Blue ripple (shield) vs orange sparks (hull) — always distinct |
| **All weapons feel the same** | Games with only one projectile visual | Family-specific trails, impacts, sounds, and shake |
| **Combat log replaces visual feedback** | EVE Online (for new players) | Log is supplementary; visual/audio is primary |
| **Screen shake on every frame** | Overjuiced indie games | Shake is punctuation, not constant. Exponential decay, 0.15s max |
| **No kill confirmation** | Ships just disappear from the list | Explosion + hitstop + loot + "DESTROYED" text |
| **UI obscures combat** | X4 (menus during dogfights) | Combat HUD is minimal: HP bars + crosshair + zone status. No menus |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Feedback stack per hit | Starsector | Flux bars + shield color + overload sparks = glanceable combat state |
| Damage family identity | Star Citizen | Shield behavior per damage type communicated visually |
| Combat readability | FTL | Pause-to-plan for complexity, real-time for visceral moments |
| Kill confirmation | Halo (any) | Hitstop + sound + visual confirms "your action mattered" |
| Screen shake discipline | Vlambeer (Nuclear Throne) | Shake as punctuation with fast decay, never sustained |
| Zone/directional armor | MechWarrior | Paperdoll showing zone HP around silhouette |
| Fleet combat summary | Into the Breach | Post-combat summary showing each action and outcome |
