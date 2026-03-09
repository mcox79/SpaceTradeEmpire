# Audio Design — Design Bible

> Design doc for the game's sound architecture: layer model, spatial audio rules,
> combat audio priority, the silence palette, and the emotional role of sound.
> Cross-referenced by `CombatFeel.md`, `RiskMeters.md`, and `ExplorationDiscovery.md`.

## Why This Doc Exists

Audio is the most unconscious contributor to game feel. Players rarely praise good audio
explicitly but immediately notice its absence. Homeworld is inseparable from its score.
FTL's combat music is iconic. Elite Dangerous's data-derived soundscapes create a sense
of place that visuals alone cannot.

Without a design doc, audio becomes the system where debt accumulates invisibly. Assets
get created but never connected (we have 6 unused sound files right now). Systems
get audio pools that never fire (combat_audio.gd's 16 pooled players are never called).
This doc prevents us from shipping a game that "sounds like a game" instead of
"sounds like a universe."

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Engine hum (pitch-modulated by velocity) | Done | Non-3D, -30 dB, pitch 0.95-1.15 |
| Music crossfade (calm ↔ combat) | Done | 1.5s fade, hostile proximity trigger at 60u |
| Combat audio pool (8 fire + 8 impact, 3D) | Done | Pool exists but NEVER CALLED from game code |
| Station ambient hum (120 Hz baked tone, 3D) | Done | Register/unregister API exists but NEVER CALLED |
| Space drone (40 Hz persistent background) | Done | -24 dB, loops infinitely |
| Turret fire SFX | Done | Plays from game_manager on turret cooldown |
| Hit SFX | Done | Plays from game_manager on impact |
| dock_chime.wav | Asset exists | NOT CONNECTED — no code references it |
| explosion.wav | Asset exists | NOT CONNECTED — unused |
| warp_whoosh.wav | Asset exists | NOT CONNECTED — unused |
| ambient_drone.wav | Asset exists | NOT CONNECTED — baked tone used instead |
| Audio bus separation (SFX/Music/Ambient) | Not implemented | All audio on Master bus |
| Discovery/scan audio | Not implemented | Phase transitions are silent |
| Risk meter audio | Not implemented | Threshold crossings are silent |
| Trade transaction audio | Not implemented | Buy/sell confirmations are silent |
| UI interaction audio | Not implemented | Button clicks, tab switches are silent |
| Docking/undocking audio | Not implemented | State changes are silent |
| Warp tunnel audio | Not implemented | Transit has VFX but no sound |
| Dynamic music layers | Not implemented | Binary calm/combat only |

---

## Design Principles

1. **Sound confirms what the eyes see and what the hands do.** Every player action must
   have audio acknowledgment. Firing a turret without sound feels broken. Docking without
   a chime feels like a bug. Buying cargo without a register-click feels uncertain ("did it
   work?"). Audio is the receipt that the game heard the player's input.

2. **Space has a sound design, even though space is silent.** We don't simulate vacuum
   silence — that's realistic but terrible game design. Instead, we use a "cockpit audio"
   conceit: all sounds are what the pilot hears through their ship's systems. Engine hum is
   the drive core vibrating the hull. Weapon impacts are hull resonance from kinetic force.
   Explosions are sensor-reconstructed audio fed to the pilot's headset. This gives us
   creative license while maintaining a coherent fiction.

3. **Silence is a design tool.** The absence of sound is as powerful as its presence.
   Homeworld's use of Adagio for Strings works because the score is sparse — silence
   between notes creates emotional weight. Our rule: every scene has a "silence floor" —
   the quietest it gets — and that floor is intentional, not accidental.

4. **Audio layers mix, not compete.** When 50 sounds could play simultaneously, priority
   rules determine what the player actually hears. The most important sound wins, others
   are ducked or dropped. The player should never hear cacophony.

5. **Music responds to game state, not timers.** The calm→combat crossfade already works
   (hostile proximity trigger). Extend this: music should shift with risk meter levels,
   exploration milestones, and economic crises. The player should FEEL the mood before
   checking the UI.

---

## Audio Layer Architecture

### The Five Layers

| Layer | Content | Volume Range | Spatial? | Priority |
|-------|---------|-------------|----------|----------|
| **Music** | Background score (calm, combat, tension, triumph) | -28 to -20 dB | No (2D) | Lowest (always yields) |
| **Ambient** | Space drone, station hum, system-specific atmosphere | -24 to -12 dB | Mixed | Low |
| **SFX** | Weapons, impacts, explosions, engine, warp | -18 to -6 dB | Yes (3D) | High |
| **UI** | Button clicks, tab switches, toast chimes, transaction confirmations | -12 to -6 dB | No (2D) | High |
| **Alert** | Risk threshold warnings, shield-down alarm, fuel warning | -8 to 0 dB | No (2D) | Highest (never ducked) |

### Audio Bus Structure (Godot)

```
Master
  ├── Music       (-28 dB default, ducked during alerts)
  ├── Ambient     (-24 dB default, ducked during combat)
  ├── SFX         (-12 dB default)
  ├── UI          (-10 dB default, never ducked)
  └── Alert       (-8 dB default, never ducked, ducks Music)
```

### Ducking Rules

When a higher-priority layer plays, lower layers duck (reduce volume):

| Trigger | Music Ducks To | Ambient Ducks To | Duration |
|---------|---------------|-----------------|----------|
| Combat SFX active | -35 dB | -30 dB | While firing |
| Alert sound plays | -40 dB | -28 dB | 2s after alert |
| UI confirmation plays | No change | No change | — |
| Exploration reveal card | -32 dB | -28 dB | During card display |

---

## Sound Palette by Game State

### Peaceful Flight

The default soundscape. Quiet, meditative, a sense of vast empty space.

| Sound | Source | Notes |
|-------|--------|-------|
| Engine hum | `engine_audio.gd` | Pitch modulated by velocity (0.95-1.15) |
| Space drone | `ambient_audio.gd` | 40 Hz sub-bass, persistent |
| Calm music | `music_manager.gd` | Ambient electronic, -28 dB |
| Starfield ambience | Aspiration | Faint, abstract tonal swells. Varies by star class. |

**Silence floor:** -40 dB. The quietest peaceful flight gets. Player should hear
their own breathing (figuratively — the silence is part of the experience).

### Docked at Station

Interior, enclosed. The ambient shifts from vast emptiness to mechanical intimacy.

| Sound | Source | Notes |
|-------|--------|-------|
| Station hum | `ambient_audio.gd` | 120 Hz, 3D positioned at station, -12 dB |
| Engine hum | Muted (-40 dB) | Ship's drive powers down when docked |
| Dock chime | `dock_chime.wav` (unused!) | Plays once on docking confirmation |
| UI ambience | Aspiration | Soft background for menu interaction |
| Calm music | Continues | Same track, slight volume bump to -25 dB |

**Silence floor:** -30 dB. Station interiors feel more enclosed (less silence).

### Combat

Intense, urgent. Audio should communicate danger and action without becoming cacophony.

| Sound | Source | Notes |
|-------|--------|-------|
| Weapon fire | `combat_audio.gd` | 3D positioned, family-specific (see below) |
| Impact hits | `combat_audio.gd` | 3D positioned, shield vs hull distinction |
| Combat music | `music_manager.gd` | Crossfades in over 1.0s when hostiles < 60u |
| Shield warning | Alert layer | Plays at <25% shield, looping alarm |
| Hull warning | Alert layer | Plays at <25% hull, urgent klaxon |
| Explosion | `explosion.wav` (unused!) | On ship destruction, -6 dB |
| Engine hum | Pitch increases | Doppler-like intensity increase during combat |

**Priority rules (max simultaneous SFX):**

| Source | Max Concurrent | Priority |
|--------|---------------|----------|
| Player weapon fire | 2 | Medium |
| Player hit received | 3 | High |
| Enemy weapon fire | 2 | Low (duck if player is firing) |
| Enemy hit received | 2 | Low |
| Explosion | 1 | Highest (all others duck briefly) |

### Warp Transit

The transition between systems. A distinct audio moment that marks passage.

| Sound | Source | Notes |
|-------|--------|-------|
| Warp entry | `warp_whoosh.wav` (unused!) | Rising pitch, 1s, marks the jump |
| Transit drone | Aspiration | Deep resonant hum during tunnel, pitch rises over transit duration |
| Warp exit | Reverse whoosh | Falling pitch + arrival rumble |
| Music | Muted during transit | Brief silence, then new system's ambient fades in |

**The silence beat:** A 0.5s silence gap between warp exit sound and new system
ambient. This pause creates the "arrival moment" — the player's senses reset before
the new system's soundscape begins.

### Exploration Discovery

Discovery milestones get their own audio treatment because they're the game's
primary reward moments.

| Event | Sound | Layer | Character |
|-------|-------|-------|-----------|
| Discovery → Seen | Quiet radar ping | UI | Subtle, informational |
| Discovery → Scanned | Rising chime (3 notes ascending) | UI | Curious, inviting |
| Discovery → Analyzed | Revelation fanfare (brief, 2s) | UI + Music duck | Triumphant, satisfying |
| Lead discovered | Questioning motif (2 notes, second unresolved) | UI | Curious, beckoning |
| Scanner sweep | Radar sweep pulse (expanding ring) | SFX (3D) | Technological, precise |

### Risk Threshold Crossings

(Cross-reference: `RiskMeters.md`)

| Meter | Sound Character | Rising Threshold | Falling Threshold |
|-------|----------------|-----------------|-------------------|
| Heat | Warm analog synth | Tone rises in pitch with each threshold | Tone falls, warm resolution |
| Influence | Metallic bell | Resonant chime, longer sustain at higher levels | Short, clear ding |
| Trace | Digital radar ping | Faster pulse rate at higher thresholds | Pulse slows, fades |

---

## Weapon Audio by Damage Family

(Cross-reference: `CombatFeel.md`)

Each damage family has a distinct audio personality so the player learns the damage
model through sound, not tooltips.

| Family | Fire Sound | Hit Sound (Shield) | Hit Sound (Hull) |
|--------|-----------|-------------------|------------------|
| **Kinetic** | Deep THUD + mechanical recoil | Dull clang + shield absorption hum | Heavy metallic CRUNCH + hull stress |
| **Energy** | High-pitched ZAP + electric sizzle | Crackling spark + overload whine | Searing hiss + material vaporization |
| **Neutral** | Balanced POP | Generic impact | Generic impact |
| **PointDefense** | Rapid TAP-TAP-TAP (burst fire) | Light plink (rapid, quieter) | Light plink |

### Shield Break Sound

The "shields down" moment gets a special, unmistakable sound:
- Electric discharge POP (sharp, percussive)
- Brief silence (0.1s — hitstop)
- Low warning tone begins

This is the audio equivalent of the combat feel hitstop — a punctuation mark that
says "the tactical situation just changed."

---

## Dynamic Music System

### Current: Binary (Calm / Combat)

Two tracks, crossfading on hostile proximity (60u threshold). This works but is
one-dimensional.

### Aspiration: Four-State Music

| State | Trigger | Character |
|-------|---------|-----------|
| **Calm** | No hostiles, all risk meters Calm | Ambient electronic, sparse, meditative |
| **Tension** | Risk meter at Elevated+, OR approaching hostile territory | Calm track + tension layer (subtle drums, discordant undertones) |
| **Combat** | Hostiles within 60u | Full combat track, urgent percussion |
| **Triumph** | Combat won, discovery analyzed, milestone reached | Brief triumph sting (5s), then returns to previous state |

### Music Layer Architecture

Instead of two separate tracks, use a layered composition where elements add:

```
Layer 0 (always):    Ambient pad (synth, evolving)
Layer 1 (tension):   Percussion (light, building)
Layer 2 (combat):    Full drums + bass + intensity
Layer 3 (triumph):   Melodic sting (brief, fades quickly)
```

Transitions: layers fade in/out over 1.5s (existing crossfade timing).

### Star Class Ambience

Different star classes should subtly tint the ambient layer:

| Star Class | Ambient Tint | Emotional Effect |
|------------|-------------|-----------------|
| O/B (Blue giants) | Deep resonant drone, crystalline overtones | Awe, power |
| A/F (White/yellow-white) | Warm, tonal, comfortable | Safety, commerce |
| G (Sun-like) | Neutral, balanced | Home, familiarity |
| K (Orange) | Analog warmth, slight distortion | Frontier, resourcefulness |
| M (Red dwarf) | Low-frequency rumble, distant | Isolation, danger |

This is the Elite Dangerous approach: celestial bodies emit soundscapes based on
their properties. The player subconsciously associates star class with mood.

---

## UI Sound Design

### Interaction Confirmations

Every player action with a game-state consequence should have an audio confirmation:

| Action | Sound | Character |
|--------|-------|-----------|
| Buy/sell goods | Cash register click + coin chime | Commerce, satisfaction |
| Start program | Activation tone (ascending) | Mechanical, productive |
| Pause program | Deactivation tone (descending) | Mechanical, deliberate |
| Cancel program | Flat buzz (brief) | Finality |
| Dock at station | `dock_chime.wav` + engine power-down | Arrival, safety |
| Undock | Engine power-up hum + release clunk | Departure, adventure |
| Open empire screen | Subtle UI swoosh | Non-intrusive transition |
| Switch tabs | Light click/tap | Quick, responsive |
| Open galaxy overlay | Zoom-out whoosh + ambient shift | Scale change |
| Set waypoint | Confirm ping | Navigation |

### Toast Notification Sounds

(Cross-reference: `HudInformationArchitecture.md`)

| Toast Priority | Sound | Notes |
|----------------|-------|-------|
| Critical | Alert chime (two-note, urgent) | Distinct from combat alerts |
| Warning | Attention tone (one-note, moderate) | Similar to risk threshold |
| Info | Quiet chime (soft, brief) | Non-intrusive |
| Confirmation | Positive ding (rising) | Satisfying, brief |

---

## Spatial Audio Rules

### 3D Sound Attenuation

| Sound Source | Max Distance | Model | Rationale |
|-------------|-------------|-------|-----------|
| Weapon fire | 80u | Inverse distance | Hear nearby combat, not distant |
| Bullet impact | 60u | Inverse distance | Impacts are quieter than fire |
| Station hum | 40u | Inverse distance | Only when near station |
| NPC engine | 30u | Inverse distance | Background presence, not prominent |
| Explosion | 120u | Inverse distance | Big events heard from further |
| Player engine | N/A (2D) | No attenuation | Always audible (cockpit sound) |

### Doppler Effect

Not implemented, but if added:
- Only apply to NPC ships passing the player at high relative velocity
- Subtle pitch shift (±10% max) — enough to sense motion, not enough to distort

---

## The Silence Palette

### Intentional Silence Moments

| Moment | Duration | Purpose |
|--------|----------|---------|
| Warp arrival gap | 0.5s | Reset senses between systems |
| Shield break hitstop | 0.1s | Punctuate tactical shift |
| Discovery reveal card | Music ducks | Focus on revelation |
| Game Over | All sound fades over 2s, then silence | Weight of death |
| First launch (new game) | 2s silence before engine starts | The void, then you're alive |

### What Silence Communicates

- In open space: vastness, isolation, freedom
- After combat: relief, survival
- After discovery: contemplation, significance
- At game over: finality

**Rule:** Never fill silence with filler sound. If a moment is quiet, it's quiet
because the design says so.

---

## Implementation Priority

Given that audio is the most forgiving system during early development but has the
most accumulated debt, prioritize connecting existing assets before creating new ones:

### Phase 1: Connect Existing Assets (Zero new audio needed)
1. Wire `combat_audio.gd` pool to actual combat events (fire_sfx, impact_sfx)
2. Wire `ambient_audio.gd` station registration to station spawn
3. Connect `dock_chime.wav` to docking state change
4. Connect `explosion.wav` to ship destruction
5. Connect `warp_whoosh.wav` to warp entry/exit
6. Mute engine hum when docked

### Phase 2: Audio Bus Separation
1. Create Music/Ambient/SFX/UI/Alert buses in Godot
2. Route all existing audio to correct buses
3. Implement ducking rules

### Phase 3: New Audio
1. UI interaction sounds (buy/sell, tab switch, button click)
2. Discovery phase transition chimes
3. Risk meter threshold crossing tones
4. Weapon family differentiation (kinetic vs energy fire/impact sounds)
5. Star class ambient tinting

### Phase 4: Dynamic Music
1. Add tension music layer
2. Add triumph sting
3. Implement risk-meter-driven tension state
4. Star class ambient variations

---

## Anti-Patterns to Avoid

| Anti-Pattern | What Happens | Our Rule |
|---|---|---|
| **Unused audio assets** | Files exist but no code plays them | Current state! Phase 1 fixes this |
| **All sounds same volume** | Nothing stands out, cacophony | Five-layer bus architecture with priority |
| **No silence** | Constant noise fatigues the player | Intentional silence moments are designed |
| **Music independent of game state** | Score feels disconnected, like a playlist | Music responds to risk, combat, exploration |
| **Every NPC makes sound** | 50 ships = wall of noise | Priority rules, max concurrent limits |
| **Audio confirms nothing** | Buy/sell/dock/undock all silent | Every state change has audio confirmation |
| **Stock sound effects** | "Sounds like a game" not "sounds like a universe" | Cockpit audio conceit: all sounds justified through ship systems |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Emotional score | Homeworld | Silence + restraint + context = iconic moments |
| Dynamic music | FTL | Music varies by game state, not just random playlist |
| Cockpit audio conceit | Elite Dangerous | All sounds justified through ship sensors/hull resonance |
| Star-specific soundscapes | Elite Dangerous | Celestial bodies emit data-derived soundscapes |
| Combat audio priority | Halo | Player weapon fire always heard clearly, enemy fire ducked |
| UI confirmation sounds | iOS | Every interaction has a brief, distinct audio response |
| Silence as design | Journey | Silence at the peak of the mountain is the game's most powerful moment |
