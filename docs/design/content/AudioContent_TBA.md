# Audio Content — To Be Authored

> **Status: TO_BE_AUTHORED**
> This document catalogs all audio assets that must be created, sourced, or wired.
> Organized by priority: existing-but-unwired assets first (zero-cost wins),
> then new assets needed by system.
>
> Companion to: `AudioDesign.md` (5-layer bus architecture, silence palette),
> `CombatFeel.md` (weapon/impact audio), `ExplorationDiscovery.md` (discovery chimes).

---

## Priority Tiers

- **WIRE**: Asset EXISTS on disk, code path EXISTS, but nothing calls it. Zero creative work.
- **WIRE+CODE**: Asset exists, but code path needs building or connecting.
- **SOURCE**: Asset must be sourced (Freesound, purchase, or compose). Then wired.
- **COMPOSE**: Asset must be original composition (music stems, faction themes).

---

## 1. Existing Assets — Unwired (IMMEDIATE)

These assets are on disk and their playback code exists but is never called.
Fix = one line of code per asset.

| ID | Asset | File | Playback Code | Currently Called? | Fix Epic |
|----|-------|------|--------------|------------------|----------|
| AUD.WIRE.001 | Combat fire SFX pool | `combat_audio.gd` pool (8 fire players) | `play_fire_sfx_v0()` | NEVER | S7.AUDIO_WIRING |
| AUD.WIRE.002 | Combat impact SFX pool | `combat_audio.gd` pool (8 impact players) | `play_hit_sfx_v0()` | NEVER (called in code but audio players empty?) | S7.AUDIO_WIRING |
| AUD.WIRE.003 | Station ambient hum | `ambient_audio.gd` | `register_station_v0()` | NEVER | S7.AUDIO_WIRING |
| AUD.WIRE.004 | Dock chime | `dock_chime.wav` (if exists) | Needs connection in dock flow | NEVER | S7.AUDIO_WIRING |
| AUD.WIRE.005 | Explosion SFX | `explosion.wav` (referenced in CombatFeel.md) | Needs death handler | NEVER | S7.AUDIO_WIRING |
| AUD.WIRE.006 | Warp whoosh | `warp_whoosh.wav` (if exists) | Needs warp transit handler | NEVER | S7.AUDIO_WIRING |

**Estimated effort:** 1-2 hours to wire all 6.

---

## 2. Combat Audio (SOURCE or COMPOSE)

Per CombatFeel.md, each damage family should have distinct audio identity.

### Weapon Fire Sounds (4 needed)

| ID | Family | Sound Character | Duration | Notes |
|----|--------|----------------|----------|-------|
| AUD.FIRE.KINETIC | Kinetic | Deep THUD, heavy cannon | 0.2-0.3s | Chunky, impactful |
| AUD.FIRE.ENERGY | Energy | High ZAP, electric sizzle | 0.2-0.3s | Bright, crackling |
| AUD.FIRE.NEUTRAL | Neutral | Balanced POP | 0.15-0.25s | Default weapon sound |
| AUD.FIRE.PD | Point Defense | Rapid TAP-TAP-TAP | 0.1s per shot | Quick, light |

**Source options:** Freesound.org sci-fi weapon packs, Sonniss GDC bundles

### Impact Sounds (4 needed — shield variant + hull variant)

| ID | Target | Sound Character | Duration |
|----|--------|----------------|----------|
| AUD.IMPACT.SHIELD | Shield hit | Electric crackle + absorption hum | 0.2s |
| AUD.IMPACT.HULL | Hull hit | Metallic crunch + hull stress groan | 0.3s |
| AUD.IMPACT.SHIELD_WEAK | Kinetic vs shield (50% eff) | Deflection ping, higher pitch | 0.15s |
| AUD.IMPACT.HULL_WEAK | Energy vs hull (50% eff) | Scattered glow crackle, weak | 0.15s |

### Combat Event Sounds (5 needed)

| ID | Event | Sound Character | Duration | Priority |
|----|-------|----------------|----------|----------|
| AUD.COMBAT.SHIELD_BREAK | Shield drops to 0 | Sharp crack + electric discharge POP | 0.5s | HIGH |
| AUD.COMBAT.EXPLOSION | Ship destroyed | Fireball whoosh + debris scatter | 1.0-1.5s | HIGH |
| AUD.COMBAT.VICTORY | Combat won | Brief triumph sting | 1.0s | MEDIUM |
| AUD.COMBAT.DEFEAT | Combat lost | Low drone fadeout | 1.5s | MEDIUM |
| AUD.COMBAT.CRITICAL_HIT | Oversized damage | Amplified impact + bass hit | 0.3s | LOW |

---

## 3. Discovery & Exploration Audio (SOURCE)

Per ExplorationDiscovery.md and AudioDesign.md.

| ID | Event | Sound Character | Duration | Priority |
|----|-------|----------------|----------|----------|
| AUD.DISC.SEEN | Discovery auto-detected | Quiet radar ping | 0.5s | HIGH |
| AUD.DISC.SCANNED | Player scans discovery | Rising chime (discovery!) | 1.0s | HIGH |
| AUD.DISC.ANALYZED | Full analysis complete | Revelation fanfare (brief, distinct) | 2.0s | HIGH |
| AUD.DISC.LEAD | Lead discovered | Questioning motif (curious tone) | 1.0s | MEDIUM |
| AUD.DISC.SCANNER_SWEEP | Scanner reveals new system | Radar ping (repeatable) | 0.3s | MEDIUM |
| AUD.DISC.KNOWLEDGE_CONNECT | Two discoveries linked | Harmonic resolution chime | 0.5s | LOW |

---

## 4. Risk Meter Audio (SOURCE)

Per RiskMeters.md. Each meter has subtly different tone quality.

| ID | Meter | Threshold | Sound Character | Duration |
|----|-------|-----------|----------------|----------|
| AUD.RISK.HEAT.NOTICED | Heat | Noticed | Warm analog synth notification | 0.5s |
| AUD.RISK.HEAT.ELEVATED | Heat | Elevated | Warmer, more urgent synth tone | 0.7s |
| AUD.RISK.HEAT.HIGH | Heat | High | Alert klaxon (warm tone) | 1.0s |
| AUD.RISK.HEAT.CRITICAL | Heat | Critical | Alarm + ambient drone shift | 1.5s |
| AUD.RISK.INFLUENCE.NOTICED | Influence | Noticed | Metallic bell notification | 0.5s |
| AUD.RISK.INFLUENCE.ELEVATED | Influence | Elevated | Resonant bell, more insistent | 0.7s |
| AUD.RISK.INFLUENCE.HIGH | Influence | High | Alert klaxon (metallic tone) | 1.0s |
| AUD.RISK.INFLUENCE.CRITICAL | Influence | Critical | Alarm + bell drone | 1.5s |
| AUD.RISK.TRACE.NOTICED | Trace | Noticed | Digital radar ping | 0.5s |
| AUD.RISK.TRACE.ELEVATED | Trace | Elevated | Scanner sweep tone | 0.7s |
| AUD.RISK.TRACE.HIGH | Trace | High | Alert klaxon (digital tone) | 1.0s |
| AUD.RISK.TRACE.CRITICAL | Trace | Critical | Alarm + scan drone | 1.5s |
| AUD.RISK.DECAY | Any | Below Noticed | Relief chime (tension resolved) | 0.5s |

**Total risk audio:** 13 sounds. Could share base sounds with pitch/filter variants.

---

## 5. UI Audio (SOURCE)

Per HudInformationArchitecture.md.

| ID | Event | Sound Character | Duration | Priority |
|----|-------|----------------|----------|----------|
| AUD.UI.TOAST.INFO | Info notification | Subtle click/chime | 0.2s | MEDIUM |
| AUD.UI.TOAST.WARNING | Warning notification | Attention tone (mid urgency) | 0.3s | MEDIUM |
| AUD.UI.TOAST.CRITICAL | Critical notification | Alert sting | 0.5s | MEDIUM |
| AUD.UI.TOAST.CONFIRM | Confirmation toast | Soft positive ding | 0.2s | MEDIUM |
| AUD.UI.TAB_SWITCH | Dashboard/dock tab change | Subtle click | 0.1s | LOW |
| AUD.UI.OVERLAY_MODE | Galaxy overlay mode switch | Mode-shift whoosh | 0.2s | LOW |
| AUD.UI.DOCK | Station dock | Mechanical lock + atmospheric hiss | 1.0s | MEDIUM |
| AUD.UI.UNDOCK | Station undock | Release clamp + engine spool | 0.8s | MEDIUM |

---

## 6. Ambient Audio (SOURCE or COMPOSE)

Per AudioDesign.md.

### Star Class Ambient (5 needed)

| ID | Star Class | Sound Character | Duration |
|----|-----------|----------------|----------|
| AUD.AMB.STAR.G | G-class (yellow) | Warm steady hum | Loop |
| AUD.AMB.STAR.K | K-class (orange) | Lower rumble, slight crackle | Loop |
| AUD.AMB.STAR.M | M-class (red) | Deep drone, irregular pulse | Loop |
| AUD.AMB.STAR.B | B-class (blue) | High-frequency shimmer | Loop |
| AUD.AMB.STAR.NEUTRON | Neutron star | Rapid pulsing + radiation hiss | Loop |

### Faction Territory Ambient (5 needed)

| ID | Faction | Sound Character | Duration |
|----|---------|----------------|----------|
| AUD.AMB.FACTION.CONCORD | Concord | Clean, regulated, slight machinery | Loop |
| AUD.AMB.FACTION.CHITIN | Chitin | Organic clicks, swarm undertone | Loop |
| AUD.AMB.FACTION.WEAVERS | Weavers | Construction resonance, harmonic | Loop |
| AUD.AMB.FACTION.VALORIN | Valorin | Engine rumble, frontier static | Loop |
| AUD.AMB.FACTION.COMMUNION | Communion | Ethereal choir, metric shimmer | Loop |

### Space State Ambient (3 needed)

| ID | State | Sound Character | Duration |
|----|-------|----------------|----------|
| AUD.AMB.LANE_TRANSIT | In-lane travel | Doppler hum + field harmonics | Loop |
| AUD.AMB.WARP_TUNNEL | Warp tunnel transit | Deep whoosh + reality strain | Loop |
| AUD.AMB.FRACTURE_SPACE | Off-lane fracture space | Unsettling metric shimmer | Loop |

---

## 7. Music (COMPOSE — Late Priority)

Per AudioDesign.md and S9.MUSIC epic.

### Dynamic Music Stems (4 states x 5 layers)

The music system uses layered stems that cross-fade based on game state:

| State | Melody | Harmony | Bass | Percussion | Texture |
|-------|--------|---------|------|------------|---------|
| **Calm** | Sparse, contemplative | Open chords, slow movement | Subtle drone | None or very sparse | Ambient pad |
| **Tension** | Melody fragments | Dissonance introduced | Pulsing bass | Light ticking rhythm | Strained pad |
| **Combat** | Urgent motif | Aggressive chords | Heavy, driving | Full combat drums | Distorted |
| **Triumph** | Resolved melody | Major resolution | Triumphant bass | Cadence hit | Bright shimmer |

**Volume:** 20 stems (4 states x 5 layers). Each stem ~2-4 minutes, loopable.

### Discovery Stingers (3 needed)

| ID | Trigger | Character | Duration |
|----|---------|-----------|----------|
| AUD.MUSIC.DISCOVERY_MINOR | Scanned phase complete | Curious 4-note phrase | 3s |
| AUD.MUSIC.DISCOVERY_MAJOR | Analyzed phase complete | Resolved 6-note phrase | 5s |
| AUD.MUSIC.REVELATION | Major lore unlock | Full orchestral swell | 8s |

---

## Summary

| Category | ID Prefix | Count | Type | Priority |
|----------|-----------|-------|------|----------|
| Unwired existing assets | AUD.WIRE | 6 | WIRE | IMMEDIATE |
| Combat fire/impact | AUD.FIRE, AUD.IMPACT | 8 | SOURCE | HIGH |
| Combat events | AUD.COMBAT | 5 | SOURCE | HIGH |
| Discovery | AUD.DISC | 6 | SOURCE | HIGH |
| Risk meters | AUD.RISK | 13 | SOURCE | MEDIUM |
| UI notifications | AUD.UI | 8 | SOURCE | MEDIUM |
| Ambient (star/faction/state) | AUD.AMB | 13 | SOURCE/COMPOSE | MEDIUM |
| Music stems + stingers | AUD.MUSIC | 23 | COMPOSE | LOW (S9) |
| **Total** | | **~82** | | |

### Sourcing Strategy

1. **Freesound.org** — CC0/CC-BY sci-fi packs for weapon, impact, UI sounds
2. **Sonniss GDC Audio Bundle** — annual free commercial-use pack, excellent for ambient
3. **Custom composition** — music stems and faction ambient require original work
4. **Godot AudioStreamRandomizer** — already integrated; use pitch/filter variation
   to multiply a smaller set of source assets
