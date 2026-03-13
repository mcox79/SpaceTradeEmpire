# Combat Feel & Juice — Design Bible

> Design doc for combat feedback: what the player sees, hears, and feels when damage
> is dealt and received. Covers hero ship combat, fleet-vs-fleet resolution, and the
> visual/audio language that makes combat readable and visceral.
> Companion to `ship_modules_v0.md` (weapon/module catalog, spin/heat systems) and `AudioDesign.md`.
> Content authoring specs: `content/AudioContent_TBA.md` (combat audio),
> `content/VisualContent_TBA.md` (combat VFX). Epic: `EPIC.S7.COMBAT_JUICE.V0`.

## Why This Doc Exists

Combat feel is the system most harmed by ad-hoc design. Each individual decision — screen
shake magnitude, hit flash duration, damage number color — seems trivial in isolation.
But they compound into either "this feels amazing" or "this feels like spreadsheet math."
The #1 complaint across space combat games is not balance or depth — it is "fighting
the interface instead of fighting ships" (X4 Foundations community consensus).

This doc defines the feedback stack: what happens on EVERY channel (visual, audio, UI,
camera) for EVERY combat event (hit, shield break, kill, player damage, spin state change,
heat threshold, radiator destruction). Every future gate that touches combat visual effects
references this doc.

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
| **Battle Stations spin-up** | **Not implemented** | **No spin system exists yet** |
| **Spin-fire cadence** | **Not implemented** | **Turrets fire continuously, no rotation cadence** |
| **Heat gauge HUD** | **Not implemented** | **No heat system exists yet** |
| **Overheat cascade feedback** | **Not implemented** | **No thermal warnings or forced ceasefire** |
| **Radiator targeting/destruction** | **Not implemented** | **No radiator subsystem HP** |
| **Spinal mount fire** | **Not implemented** | **No mount type differentiation** |

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

6. **Spin is the heartbeat of combat — and a commitment.** The ship's rotation creates a rhythmic pulse:
   turrets fire as they sweep past the engagement arc, then silence during the away-arc.
   This cadence — fire-silence-fire-silence — is the combat's tempo. Faster spin =
   faster pulse, shorter bursts. Slower spin = longer sustained volleys. The player
   should feel the rhythm in their bones before they understand the mechanics.
   But spin has a cost: **gyroscopic precession degrades turning.** A spinning Corvette
   is a tank, not a dogfighter. The player must feel the sluggishness when they try to
   chase or reposition while spinning — and the snap of agility when they spin down.
   This tradeoff is the core tactical decision of small-ship combat.

7. **Heat is visible tension.** The heat gauge is a rising threat — like a fuse burning
   toward a bomb. The player should feel mounting anxiety as heat climbs, relief when
   radiators catch up, and panic when overheat forces ceasefire. Heat state is communicated
   through progressive visual degradation: clean ship → heat shimmer → glowing hull →
   white-hot radiators → forced vent eruption.

---

## The Feedback Stack

### Event: Battle Stations (Spin-Up)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Ship mesh begins rotating around long axis. Smooth acceleration from 0 to combat RPM. Running lights shift from white (transit) to red (combat). |
| **Audio** | N/A | Low mechanical groan → rising gyro whine → steady thrum at combat RPM. Pitch and tempo scale with spin rate. |
| **Camera** | N/A | Brief 0.2 shake on spin engagement (RCS thrusters firing). Camera does NOT spin with ship — it stays stable, ship rotates within it. |
| **UI** | N/A | "BATTLE STATIONS" text flash (gold, 1.0s). Spin RPM gauge appears in combat HUD. Heat gauge appears. **Turn rate indicator dims progressively as RPM climbs — visual cue that maneuverability is being traded for defense.** |

**Maneuverability feel during spin-up:** As the ship spins up, steering should feel progressively heavier. The player's turn inputs produce less response. This should NOT feel like input lag — it should feel like mass. The ship is still responding immediately, just turning slower. A Corvette at full 3.5 RPM should feel like flying a Frigate. The moment the player spins down, the snap back to full agility should feel like a weight being lifted.

**Audio signature:** The spin thrum should be a constant low-frequency drone during combat — the ship's heartbeat. It modulates with RPM: emergency 4 RPM has a higher-pitched, urgent thrum; 0.5 RPM Dreadnought has a deep, slow pulse. This is ambient, not attention-demanding — like a submarine's reactor hum in a naval film.

### Event: Player Fires Turret (Spin Cadence)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | Cyan bullet projectile spawns | Bullet trail lingers 0.3s, muzzle flash at turret point. **Turrets fire in bursts as they rotate past engagement arc — visible rhythmic pattern of fire-dark-fire-dark.** |
| **Audio** | `_sfx_turret_fire` plays | Weapon-family-specific sound. **Firing cadence synced to spin: rhythmic bursts, not continuous.** At 2 RPM, ~10s fire / ~20s silent per turret per revolution. Multiple turrets offset around hull create overlapping cadences. |
| **Camera** | 0.15 shake | Directional recoil (camera nudges opposite to fire direction). **Shake intensity modulated by burst density — more turrets firing simultaneously = bigger shake.** |
| **UI** | None | Heat gauge ticks up per shot. Cooldown indicator near crosshair (subtle arc sweep). |

**The spin-fire rhythm:** With 2 standard turrets on a Corvette at 3.5 RPM, each turret fires for ~3 seconds per revolution, offset by ~180°. The player hears: *burst — brief pause — burst — brief pause* as turrets alternate engagement windows. A Frigate with broadside mounts gets longer bursts (broadside arc is perpendicular to spin axis, maximizing engagement time). A Dreadnought at 0.5 RPM has long, slow volleys — almost continuous, with slight modulation.

**Spinal mount exception:** The spinal weapon fires along the spin axis — continuous, steady, unaffected by rotation. While turrets pulse rhythmically, the spinal gun is a constant stream. This creates an audio/visual layering: steady spinal beam + rhythmic turret bursts. The spinal weapon is the bass note; turrets are the percussion.

### Event: Player Fires Spinal Mount

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Heavy projectile/beam from ship nose. Spinal kinetic: massive slug with thick trail. Spinal energy: sustained beam from bow. Much larger visual than turret fire. Ship shudders from recoil along spin axis. |
| **Audio** | N/A | Deep bass THOOM (kinetic) or sustained crackling ROAR (energy). Louder and deeper than any turret. The sound should carry weight — this is the biggest gun. |
| **Camera** | N/A | 0.25 shake (heavier than turret). Camera nudges backward from recoil along aim axis. |
| **UI** | N/A | Large heat spike visible on gauge. "SPINAL FIRING" indicator if the weapon has a charge-up cycle. |

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

### Event: Player Hit Lands on Enemy Radiator

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Distinct silver/white sparks + liquid metal spray (tin droplets from Droplet Radiator). Radiator panel visibly deforms/fragments. |
| **Audio** | N/A | Metallic TINK + hissing spray (pressurized coolant venting). Distinct from hull/shield sounds. |
| **Camera** | N/A | None |
| **UI** | N/A | Enemy radiator HP bar appears briefly. Combat log: "Radiator damaged — heat rejection -X" |

**Radiator destruction visual:** When a Droplet Radiator is destroyed, it releases a burst of liquid tin droplets that scatter and glow orange as they cool — a distinctive visual that tells the player "I just crippled their cooling." The droplet cloud lingers for 2-3 seconds as a brief local hazard.

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

### Event: Player Radiator Hit

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Silver sparks at ship flank + brief coolant spray visible from cockpit edge |
| **Audio** | N/A | Metallic ping + "radiator damage" warning chime (distinct from hull/shield warnings) |
| **Camera** | N/A | 0.3 shake (between shield and hull severity) |
| **UI** | N/A | Radiator HP bar flashes. If destroyed: "RADIATOR DESTROYED — heat rejection critical" warning text. Heat rejection rate drops visibly on heat gauge. |

### Event: Heat Threshold — Warm (50%)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Subtle heat shimmer around ship hull (distortion shader, very light). Radiator panels begin to glow dull orange. |
| **Audio** | N/A | Low heat tick added to ambient — a periodic soft ping, like a Geiger counter. |
| **Camera** | N/A | None |
| **UI** | N/A | Heat gauge turns from green to yellow. No warning text — just the color shift. |

### Event: Heat Threshold — Hot (75%)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Heat shimmer intensifies. Radiators glow bright orange. Hull surface begins subtle color shift toward orange at hottest points (turret bases, engine mounts). |
| **Audio** | N/A | Heat tick increases tempo (faster pinging). Occasional hull stress creak (thermal expansion). |
| **Camera** | N/A | Very slight 0.02 constant micro-vibration (heat-induced structural stress) |
| **UI** | N/A | Heat gauge turns orange. "THERMAL WARNING" text pulse. Weapon accuracy penalty indicator: "-15% ACC" shown near crosshair. |

### Event: Heat Threshold — Overheat (90%)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Heavy heat shimmer. Radiators glow white-hot. Hull visibly discolored (heat treatment effect). Occasional small venting jets from hull seams. |
| **Audio** | N/A | Alarm klaxon (distinct from shield/hull warnings). Heat tick is now rapid. Metal groaning. |
| **Camera** | N/A | 0.05 sustained micro-shake (not combat shake — this is structural vibration) |
| **UI** | N/A | Heat gauge turns red, pulsing. "OVERHEAT" warning. "-30% ACC" and "-25% TRACKING" shown. "VENT HEAT OR CEASE FIRE" advisory text. |

### Event: Heat Critical — Forced Vent (100%)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | **Dramatic vent sequence:** All radiators flash white. Jets of superheated gas erupt from hull vents (visible steam/plasma). Ship briefly glows. The visual reads as "the ship is screaming heat into space." 5-second duration. |
| **Audio** | N/A | LOUD venting roar (pressurized release). All weapon audio cuts to silence. Only the vent roar and spin thrum remain. Then silence — eerie quiet as weapons are offline. |
| **Camera** | N/A | 0.5 shake on vent trigger. Then camera stabilizes — the stillness after the vent is its own feedback. |
| **UI** | N/A | Heat gauge flashes red/white. "WEAPONS OFFLINE — EMERGENCY VENT" in red. 5-second countdown timer. All weapon cooldown indicators locked. Engine throttle indicator shows "50% MAX". Heat gauge visibly dropping during vent. |

**The forced vent is the "shields down" moment for heat.** It should feel as significant as losing shields — a catastrophic failure state that the player felt building through the previous thresholds. The visual and audio spectacle serves both as punishment (you mismanaged heat) and as information (everyone in the area knows you're vulnerable for 5 seconds).

### Event: Emergency Heat Sink Activated

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Brief white flash at ship flank (heat sink module ejecting thermal mass). Glowing white cube tumbles away from ship. |
| **Audio** | N/A | Sharp metallic CLUNK (ejection) + brief hissing cool-down. Satisfying "relief" sound. |
| **Camera** | N/A | None |
| **UI** | N/A | Heat gauge drops sharply (50 heat points). "HEAT SINK DEPLOYED" text. Heat sink icon shows "SPENT" (grayed out, 120-tick recharge timer). |

### Event: Enemy Shield Breaks (-> 0)

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
| **Visual** | Ship mesh disappears | Explosion VFX (fireball + debris chunks + lingering smoke, 1.5s). **If enemy had radiators: additional liquid metal spray in explosion debris (tin/coolant).** |
| **Audio** | None | `explosion.wav` (asset exists, unused) + distant rumble |
| **Camera** | None | 0.2s hitstop (time slows to 0.3x) + 0.5 shake |
| **UI** | NPC removed from scene | Kill confirmation text ("DESTROYED" + loot summary), combat log entry |
| **World** | Fleet removed, respawn queued | Loot drop marker spawns at wreck location. **Radiator debris field (hot metal hazard, 3s).** |

### Event: Player Destroyed

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | "GAME OVER" red text (disabled) | Explosion VFX on player ship, camera pulls back to wide shot. **Ship spin decelerates during death sequence (angular momentum decays).** |
| **Audio** | None | Explosion + silence (engines die, **spin thrum winds down**, music fades) |
| **Camera** | None | Camera detaches, slow orbit around wreck debris |
| **UI** | "Press R to Restart" | Death summary (damage taken, killer ID, survival time, **peak heat reached**, **radiators destroyed**), [Restart] [Load Save] |

### Event: Spin-Down (Disengaging)

| Channel | Current | Aspiration |
|---------|---------|------------|
| **Visual** | N/A | Ship rotation visibly decelerates. Running lights shift from red back to white. |
| **Audio** | N/A | Spin thrum pitch drops, winds down to silence. Mechanical deceleration sound. |
| **Camera** | N/A | None |
| **UI** | N/A | "STAND DOWN" text (subtle, white). Spin RPM gauge decreases to 0. Heat gauge fades out. Zone armor display collapses to compact view. **Turn rate indicator brightens as agility returns — the ship feels lighter.** |

**Spin-down agility recovery:** The return of full turn rate during spin-down is a reward moment. The player should feel the ship "wake up" — snappy, responsive, free. This contrast is what makes the spin decision meaningful. If the player never feels the penalty, the recovery has no payoff. If they never feel the recovery, the penalty feels like a permanent tax. Both halves must be felt.

---

## Damage Family Visual Language

Each damage family should have a distinct visual identity so the player learns the
damage model through feel rather than reading tooltips.

| Family | Projectile Trail | Impact Effect | Sound Character | Screen Shake | Heat Feel |
|--------|-----------------|---------------|-----------------|-------------|-----------|
| **Kinetic** | Short, thick, white-yellow trail | Heavy sparks, debris chunks, smoke puff | Deep THUD + metallic crunch | 1.2x base (heavy) | Moderate heat — feels workmanlike, sustainable |
| **Energy** | Long, thin, colored beam trail | Electric crackle, bright flash, no debris | High ZAP + electric sizzle | 0.8x base (light) | **High heat — firing feels "expensive," each shot visibly moves the heat gauge** |
| **Neutral** | Medium trail, default color | Standard particle burst | Balanced POP | 1.0x base | Moderate heat |
| **PointDefense** | Rapid thin streaks | Small flash, fast dissipation | Rapid TAP-TAP-TAP | 0.5x base (minimal) | Low heat — feels "free" to fire |
| **Missile** | Smoke trail + engine glow | Explosion at impact (large) | WHOOSH (launch) + BOOM (impact) | 1.5x base on impact | **Near-zero heat — launch feels thermally consequence-free** |

### Teaching the Damage Model Through Feel

The player should FEEL that kinetic weapons are better against hull (heavy, chunky
impacts that feel like they're tearing metal) and energy weapons are better against
shields (bright, electric impacts that feel like they're overloading electronics).

When kinetic hits shields (50% effectiveness), the impact should feel "deflected" —
smaller particles, higher-pitched bounce sound. When energy hits hull (50% effectiveness),
the impact should feel "dispersed" — scattered glow, weak crackle.

**Teaching the heat model through feel:** Energy weapons should feel "hot" to fire — the
heat gauge visibly jumps, the heat tick tempo increases, and after 4-5 shots the player
feels the thermal pressure mounting. Kinetic weapons feel cooler — more shots before the
gauge moves noticeably. Missiles feel thermally free — the player dumps the magazine
without any heat consequence, reinforcing why missiles exist as alpha-strike weapons.

The spin-fire cadence also teaches intuitively: energy weapons get shorter effective
bursts (high heat means the player wants to fire less per window) while kinetics can
sustain fire through the full engagement arc.

---

## Combat HUD — Integrated Status Display

The combat HUD must show zone armor, heat, spin, and radiator state in a glanceable layout.

### Ship Status Display (Aspiration)

```
┌─ COMBAT HUD ────────────────────────────────────────┐
│                                                       │
│         ╭── FORE: 25/25 ──╮                         │
│        ╱                    ╲                        │
│  PORT ║   [ship silhouette]  ║ STBD                 │
│  20/20 ║   (rotating)       ║ 20/20                 │
│        ╲                    ╱                        │
│         ╰── AFT: 15/15 ───╯                         │
│                                                       │
│  Shield: ████████████ 50/50                          │
│  Hull:   ████████████ 100/100                        │
│  Heat:   ██████░░░░░░ 58/100  [WARM]                │
│                                                       │
│  Spin: 3.5 RPM ◎ COMBAT     Radiators: 40/40 HP    │
│  Stance: BROADSIDE (35% sides)                       │
│                                                       │
│  [HEAT SINK: READY]                                  │
└───────────────────────────────────────────────────────┘
```

**Key HUD elements:**

- **Ship silhouette rotates** in the paperdoll display at the ship's actual RPM. This gives an immediate visual read on spin state without checking numbers.
- **Heat gauge** sits below Hull, with color coding: green (cold) → yellow (warm) → orange (hot) → red (overheat) → flashing red/white (critical). The threshold label ([WARM], [HOT], [OVERHEAT]) provides text confirmation.
- **Radiator HP** shown as a simple bar. When damaged, turns yellow. When destroyed, turns red with "DESTROYED" label.
- **Heat Sink button** shows READY (green), SPENT (gray + recharge timer), or NOT INSTALLED (absent).
- **Spin RPM** with combat/transit mode indicator. The rotating silhouette is the primary feedback; the number is secondary.

Each zone HP bar is positioned around the ship silhouette. As zones take damage, their
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
- **Spin cadence creates natural combat rhythm**
- **Heat gauge provides tension arc across the engagement**

### Strategic Fleet Combat (Resolver)
- 50-round attrition resolved in <1 second
- Too fast for individual hit effects
- Show: aggregate result summary with replay option
- **NEW: Heat and radiator state shown per fleet in summary**

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
│  Peak heat:     72% (no forced vent)                      │
│  Radiators:     Intact                                     │
│  Enemy radiators destroyed: Round 4 (crippled heat rej.)  │
│                                                            │
│  Salvage: Metal ×3, Components ×1, +200 credits           │
│                                                            │
│  [View Replay]  [Collect Loot]  [Continue]                │
└────────────────────────────────────────────────────────────┘
```

### Replay (Optional Aspiration)
The strategic resolver already captures `ReplayFrame` data per round. A replay viewer
could show round-by-round HP AND heat progression:

```
Round 1:  HP ████████████████████  Heat ██░░░░  vs  HP ████████████████████  Heat ██░░░░
Round 4:  HP ██████████████████░░  Heat ████░░  vs  HP █████████████░░░░░░░  Heat ██████  ← radiator destroyed!
Round 8:  HP █████████████████░░░  Heat ███░░░  vs  HP ████░░░░░░░░░░░░░░░░  Heat ██████  OVERHEAT!
Round 12: HP ████████████████░░░░  Heat ██░░░░  vs  DESTROYED
```

---

## Screen Shake Intensity Scale

Define a consistent shake scale referenced by all combat events:

| Intensity | Value | Used For |
|-----------|-------|----------|
| Micro | 0.05 | PointDefense fire, minor UI feedback, **heat micro-vibration (75%+)** |
| Light | 0.15 | Turret fire (current), weapon recoil |
| Medium-Light | 0.25 | **Spinal mount fire recoil** |
| Medium | 0.40 | Damage received (shield) (current), warp arrival, **radiator hit** |
| Medium-Heavy | 0.50 | **Forced heat vent trigger**, kill explosion |
| Heavy | 0.60 | Damage received (hull), warp exit |
| Critical | 0.80 | Shield break, near-death hit |
| Catastrophic | 1.00 | Player destruction, nearby explosion |

**Shake decay:** 0.15s duration, exponential falloff. Never sustained — shake is a
punctuation mark, not a constant state.

**Exception:** Heat micro-vibration (0.02-0.05) at 75%+ heat is sustained (not decaying)
but extremely subtle. It represents structural thermal stress, not impact. It should be
barely perceptible — felt more than seen. It stops immediately when heat drops below 75%.

**Shake frequency:** 12-15 Hz (perceptible but not nauseating).

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Ships vanish on death** | Many indie space games | Explosion VFX + debris + loot marker + hitstop |
| **Shield/hull hits look identical** | X4 Foundations | Blue ripple (shield) vs orange sparks (hull) — always distinct |
| **All weapons feel the same** | Games with only one projectile visual | Family-specific trails, impacts, sounds, shake, **and heat cost** |
| **Combat log replaces visual feedback** | EVE Online (for new players) | Log is supplementary; visual/audio is primary |
| **Screen shake on every frame** | Overjuiced indie games | Shake is punctuation, not constant. Exponential decay, 0.15s max |
| **No kill confirmation** | Ships just disappear from the list | Explosion + hitstop + loot + "DESTROYED" text |
| **UI obscures combat** | X4 (menus during dogfights) | Combat HUD is minimal: HP bars + crosshair + zone status + heat. No menus |
| **Heat gauge ignored** | Elite Dangerous | Heat thresholds have dramatic, escalating feedback on ALL channels. Impossible to ignore. |
| **Spin is cosmetic only** | Most space games with rotating ships | Spin directly affects DPS cadence, armor effectiveness, and heat distribution. Player feels the mechanical consequence. |
| **Overheat is just a number** | Many RPGs with overheating weapons | Forced vent is a dramatic 5-second spectacle. The player DREADS hitting 100%. |
| **Radiator destruction is invisible** | N/A (most games lack radiators) | Liquid metal spray + "RADIATOR DESTROYED" callout. Crippling consequence is immediately visible. |

---

## Cover-Story Naming Compliance

Per `NarrativeDesign.md` → Cover-Story Naming Discipline, ALL player-facing combat UI elements must follow pre-revelation naming rules.

**Before Module Revelation (~Hour 8):**
- Module names in combat HUD: use cover-story names (e.g., "SRE" not "fracture module")
- Damage source attribution: T3 modules displayed as "prototype" or "experimental" technology
- No combat UI element may contain: `fracture`, `adaptation`, `accommodation`, `ancient`, `organism`
- Weapon fire labels, kill attribution, and damage log entries must use cover-story names

**After Module Revelation:**
- True names permitted: "Adaptation Drive", "Graviton Shear", "Accommodation Hull"
- Combat log entries retroactively update? **OPEN QUESTION** — or do historical entries keep the cover-story names (Principle #4, knowledge IS the progression)?

**CI enforcement**: The existing grep-based lint on `scripts/ui/*.gd` and `scripts/bridge/*.cs` must also cover combat HUD scripts and combat log string literals.

---

## Battle Stations — Module Interaction

The Battle Stations spin-up (2-second transition from transit to combat RPM) creates a combat state boundary. Module behavior during this transition needs specification.

**Module activation timing during spin-up:**

| Module Type | During Spin-Up | At Combat RPM | Design Intent |
|-------------|---------------|---------------|---------------|
| Shields | Active immediately (shields don't need spin) | Full effectiveness | Shields protect during vulnerability window |
| Turret weapons | Cannot fire (not yet at engagement RPM) | Fire in spin-cadence | The 2s vulnerability IS the cost of entering combat |
| Spinal weapons | Can fire (axis-aligned, independent of spin) | Full effectiveness | Spinal advantage: fires before turrets |
| Active abilities (Silk Lattice, Burrow Protocol) | Available immediately | Full effectiveness | Player agency shouldn't wait for spin-up |
| Passive modules (armor, sensors) | Active immediately | Full effectiveness | Passive effects don't depend on spin state |
| Heat system | Heat rejection active, weapons generate heat when fired | Full thermal model | Heat management begins at spin-up |

**Design intent**: The 2-second spin-up creates a brief vulnerability window — turrets can't fire but the ship can be hit. This rewards ambush tactics (Weaver identity) and penalizes being caught off-guard. Spinal weapons firing during spin-up gives capital ships a first-strike advantage. Active abilities being available immediately preserves player agency during the most dangerous moment.

**Cross-reference**: `faction_equipment_and_research_v0.md` → Faction equipment catalogs (active abilities, weapon modules), `ship_modules_v0.md` → Spin RPM table, heat system.

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
| **Heat as rising tension** | **Starsector (flux)** | **Single resource that creates mounting pressure. Overload = catastrophic, visible, and punishing. Player manages tension, not a spreadsheet.** |
| **Combat rhythm** | **Starsector (weapon groups)** | **Weapon groups firing in alternating cadence creates rhythm. Our spin-fire achieves this physically instead of through keybinds.** |
| **Radiator as vulnerability** | **Children of a Dead Earth** | **Targeting cooling systems as dominant opening strategy. "Shoot the radiators" is a learned tactic, not a tutorial prompt.** |
| **Ship rotation** | **The Expanse** | **Ships visually rotating during combat for armor distribution. Makes combat feel physical and mechanical, not magical.** |
| **Forced cooldown spectacle** | **MechWarrior (shutdown)** | **Overheat shutdown is dramatic, punishing, and the player's own fault. Creates self-imposed tension: "one more shot or cool down?"** |
