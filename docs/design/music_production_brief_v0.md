# Music Production Brief — Space Trade Empire

> **Purpose**: Complete production specification for external composer.
> **Game**: Space Trade Empire — a single-player space trading sim with exploration, combat,
> faction politics, and a cosmic horror discovery arc.
> **Engine**: Godot 4 with a 4-layer dynamic stem system (bass, pad/harmony, melody, percussion).
> **Companion docs**: `AudioDesign.md` (bus architecture), `AudioContent_TBA.md` (full asset catalog),
> `factions_and_lore_v0.md` (world lore).

---

## 1. Game Identity & Emotional Arc

### Core Feel
Space Trade Empire sits at the intersection of **Homeworld** (vast, melancholy, beautiful),
**FTL** (intimate, tense, procedurally dangerous), and **Sunless Sea** (creeping dread,
strange beauty). The player is a lone captain building trade routes across a galaxy of
feuding factions, gradually discovering that the star lanes ("threads") are ancient alien
containment infrastructure — and that using a forbidden fracture drive is destabilizing reality.

### Emotional Journey (First Hour → Endgame)

| Phase | Duration | Emotional State | Musical Character |
|-------|----------|----------------|-------------------|
| **Opening** | 0-5 min | Wonder, curiosity | Sparse, contemplative. Single notes in silence. |
| **First trade** | 5-15 min | Competence, warmth | Gentle melodic motifs emerge. Safe harbor feel. |
| **First combat** | 15-25 min | Urgency, danger | Driving rhythm, intensity. But still survivable. |
| **Exploration** | 25-60 min | Awe, growing unease | Vast soundscapes. Occasional wrong notes hint something's off. |
| **Fracture space** | Mid-game | Dread, isolation | Detuned, unstable. Metrics bleed into the music itself. |
| **Revelations** | Late-game | Horror, understanding | The music "remembers" earlier motifs but distorted. |
| **Endgame** | Final hours | Resolve, sacrifice | Full emotional weight. All themes converge. |

### Reference Tracks (Mood, Not Imitation)

| Reference | What to Take | What to Avoid |
|-----------|-------------|---------------|
| **Homeworld OST** — Adagio for Strings arrangement | Vast emptiness. Silence between notes = emotional weight. | Not this orchestral. We're more electronic. |
| **FTL OST** — Ben Prunty | Layered electronic stems. Combat builds from exploration. | Not this chip-tune. Warmer, more analog. |
| **Sunless Sea** — Mickymar | Creeping unease under beauty. Minor key warmth. | Not this whimsical. More cosmic. |
| **Elite Dangerous** — Erasmus Talbot | Data-derived ambient. Space as texture, not melody. | Not this generic. More personality. |
| **Outer Wilds** — Andrew Prahlow | Discovery moments that earn their fanfare. Banjo intimacy. | Not the instrument choices. The emotional honesty. |
| **Subnautica** — Simon Chylinski | Isolation + beauty. Deep ocean = deep space. Tonal shifts with depth. | Not the tropical warmth. Replace with cold vastness. |
| **Stellaris** — Andreas Waldetoft | Grand strategic sweep. Faction themes that feel like civilizations. | Not the bombast. More intimate — you're one ship, not an empire. |

---

## 2. Technical Specifications

### Delivery Format

| Spec | Value |
|------|-------|
| **Format** | WAV, 44.1 kHz, 16-bit stereo |
| **Stems per state** | 4 layers: Bass, Pad/Harmony, Melody, Percussion |
| **Loop points** | Seamless loop. Export with 2-bar lead-in and lead-out for crossfade overlap. |
| **Duration** | Each stem: **3-4 minutes** (loopable). Discovery stingers: see individual specs. |
| **Naming** | `stem_{state}_{layer}.wav` — e.g., `stem_exploration_melody.wav` |
| **Loudness** | -14 LUFS integrated per stem (allows headroom for 4-layer mix) |
| **Peak** | -1 dBTP (true peak ceiling) |

### Layer Architecture

The game's music system crossfades between **states** by independently fading each of
4 **layers**. A layer from one state can briefly overlap with layers from another during
transitions (1.5-3 second crossfade). All stems in a state must be:

- **Same tempo** (locked to state's BPM)
- **Same key** (or compatible mode — see harmonic plan)
- **Same loop length** (must align perfectly when layered)
- **Independently listenable** (each layer should sound intentional alone)

### Mix Bus Integration

```
Master (-0 dB)
  ├── Music (-28 dB default, ducked to -40 dB during alerts)
  │     ├── Bass stem
  │     ├── Pad stem
  │     ├── Melody stem
  │     └── Percussion stem
  ├── Faction Ambient (-30 dB, separate from music stems)
  └── Stinger (-18 dB, ducks Music by 6 dB during playback)
```

---

## 3. Dynamic Music States (20 Stems)

### State: EXPLORATION (Primary — 70% of playtime)

**Tempo**: 72 BPM (slow, contemplative)
**Key**: D minor / D dorian (warm minor, not tragic)
**Character**: Vast, beautiful emptiness. The player is a small ship in a big galaxy.
Think "driving alone at 2 AM through desert highway." Meditative but not sleepy.

| Layer | Instrument Palette | Role | Notes |
|-------|-------------------|------|-------|
| **Bass** | Warm analog sub-bass synth, slow filter sweeps | Foundation, movement | Long sustained notes (whole/half notes). Occasional octave drops. Gentle sidechaining to percussion. |
| **Pad** | Lush pad with slow attack (~2s), reverb tail. Strings-like synth. | Harmonic bed, warmth | Chord changes every 2-4 bars. Open voicings (root + 5th + 9th). Slight detuning for width. |
| **Melody** | Clean electric piano or soft pluck synth. Crystalline. | Emotional anchor, memorability | **Sparse**. 4-8 notes per phrase, long rests between phrases. This is the motif players will remember. Leave space. Silence IS the melody. |
| **Percussion** | Soft hi-hat patterns, occasional muted kick. NO snare. | Subtle pulse, not rhythm | Quarter-note hi-hat at barely audible levels. Think heartbeat, not groove. Panning automation for width. |

**Crossfade behavior**: Always playing. Other states crossfade FROM this. Return TO this is
the "relief" moment.

### State: COMBAT (15% of playtime)

**Tempo**: 128 BPM (driving, urgent — exactly 16/9 of exploration tempo for tempo-sync)
**Key**: D minor (same tonic as exploration for seamless transition)
**Character**: Dangerous but survivable. Not metal — more "intense electronic thriller."
The player chose this fight or stumbled into it. Either way, it's personal.

| Layer | Instrument Palette | Role | Notes |
|-------|-------------------|------|-------|
| **Bass** | Distorted sub-bass, sidechained to kick. Aggressive filter resonance. | Driving energy, threat | Eighth-note patterns with syncopation. Hit hard on downbeats. Slight portamento between notes. |
| **Pad** | Strained, dissonant pad. Minor 2nd intervals. Bitcrushed edges. | Tension, urgency | Power chord voicings (root + 5th). Occasional chromatic shifts. Automation on filter cutoff — opens during intense moments. |
| **Melody** | Aggressive lead synth. Saw wave with moderate attack. | Urgency, directionality | Short, punchy phrases (2-4 notes). Rhythmic — aligned with percussion. Think "alarm that's also music." Use the exploration motif but compressed and urgent. |
| **Percussion** | Full kit: kick, snare, hi-hat, crash. Electronic but punchy. | Drive, energy | Four-on-the-floor kick. Snare on 2 and 4. Hi-hat sixteenths with velocity variation. Crash on phrase changes. Fills every 8 bars. |

**Crossfade behavior**: Fades in over 1.5s when hostiles detected within 60 units.
Percussion enters first, then bass, then pad+melody. Creates "building threat" feel.

### State: TENSION (10% of playtime)

**Tempo**: 96 BPM (between exploration and combat — transitional)
**Key**: D minor, emphasis on b6 (Bb) for Phrygian color
**Character**: Something is wrong. Risk meters are climbing. Warfront nearby. The player
hasn't been attacked yet but feels the pressure. "The music knows before you do."

| Layer | Instrument Palette | Role | Notes |
|-------|-------------------|------|-------|
| **Bass** | Pulsing sub-bass, steady eighth notes. Low filter. | Anxiety, heartbeat | Repetitive single-note pulse. Like a submarine sonar. Gradual filter opening over 32 bars. |
| **Pad** | Strained pad with slow tremolo. Minor 2nd clusters. | Unease, wrongness | Dissonant intervals that almost resolve but don't. Tritone relationships. Slow volume automation (breathing). |
| **Melody** | Distant, reverb-heavy pluck. Fragments of exploration melody. | Recognition, dread | Play the exploration motif but drop notes. Gaps where melody should be. Player recognizes something familiar but broken. |
| **Percussion** | Ticking hi-hat, very sparse. Occasional deep tom hit. | Clock, countdown | Tick-tock pattern at quarter notes. Tom hits every 4 bars (irregular placement). No kick or snare — this isn't combat yet. |

**Crossfade behavior**: Triggered by elevated risk meters or warfront proximity. Bridges
between EXPLORATION and COMBAT — the player often passes through TENSION on the way to
either state.

### State: DOCK (5% of playtime)

**Tempo**: 60 BPM (slower than exploration — you're safe now)
**Key**: D major / D mixolydian (brighter than exploration — relief, safety)
**Character**: Safe harbor. The station hum surrounds you. Markets are open. Time to think,
plan, trade. Not exciting — comforting. Think "the warm inn after a cold journey."

| Layer | Instrument Palette | Role | Notes |
|-------|-------------------|------|-------|
| **Bass** | Warm round bass. Simple root notes. Long sustain. | Grounding, stability | Whole notes on chord roots. No rhythmic movement. Warm and present but unobtrusive. |
| **Pad** | Warm analog pad with chorus. Major 7th voicings. | Comfort, safety | Rich chords with extensions (maj7, add9). Very slow changes (8-bar holds). Wide stereo. |
| **Melody** | Soft Rhodes/EP with gentle velocity. Music box quality. | Intimacy, welcome | Gentle arpeggiated patterns. Echoes of exploration melody in major mode. Feels like "coming home." |
| **Percussion** | None or barely perceptible shaker. | Absence = safety | The lack of percussion IS the point. Silence signals safety. Optional: extremely quiet ride cymbal wash. |

**Crossfade behavior**: Immediate on dock. All combat/tension layers cut (no gradual fade
for combat→dock — the safety should be sudden and relieving). Dock layers fade in over 2s.

### State: FRACTURE (Instability Phase 2+ regions — late-game areas)

**Tempo**: 72 BPM (same as exploration, but feels wrong)
**Key**: D minor but with **quarter-tone detuning** on the 3rd and 7th
**Character**: The universe is broken here. Measurements don't agree. The music itself
is infected by metric bleed — familiar motifs become uncanny. This is where the cosmic
horror lives. Reference: Subnautica's deep ocean biomes, Sunless Sea's Unterzee.

| Layer | Instrument Palette | Role | Notes |
|-------|-------------------|------|-------|
| **Bass** | Sub-bass with slow pitch drift (±5 cents over 8 bars). Unpredictable. | Instability, wrongness | Same notes as exploration bass but the pitch wobbles. Unsettling without being obvious. |
| **Pad** | Granular synthesis pad. Exploration pad run through grain delay. | Familiar but alien | Take the exploration pad texture and process it: grain clouds, spectral freeze, reverse portions. It should sound like a memory of the exploration pad. |
| **Melody** | Exploration melody played by a different instrument, wrong octave, missing notes. | Recognition + dread | The player should think "I know this tune but something is wrong." Drop 1-2 notes per phrase. Add a grace note that wasn't there before. Slightly behind the beat. |
| **Percussion** | Irregular clicks, metallic resonance. No recognizable rhythm. | Disorder | Abandon the grid. Random-feeling (but composed) metallic hits. Like machinery that's breaking down. Occasional reverse cymbal swells. |

**Crossfade behavior**: Fades in over 3 seconds when entering fracture space (instability
phase >= 2). The crossfade itself should feel "wrong" — layers from EXPLORATION linger
slightly longer than they should, creating momentary polytonality.

---

## 4. Discovery Stingers (3 Assets)

These are one-shot musical moments that play over the stem layers (ducking stems by 6 dB).
They are the game's primary reward sounds — they must feel EARNED.

### Stinger: Discovery Minor
**Trigger**: Scan phase complete (player scanned a discovery site)
**Duration**: 3 seconds
**Character**: Curious, inviting. "What's this?"
**Spec**: Ascending 3-note phrase (D4 → F4 → A4) on crystalline pluck synth. Each note
has a gentle reverb tail. Slight volume swell on the third note. End with a sustain that
fades naturally.
**File**: `stinger_discovery_minor.wav`

### Stinger: Discovery Major
**Trigger**: Analysis phase complete (player fully analyzed a discovery)
**Duration**: 5 seconds
**Character**: Triumphant, satisfying. "I found something."
**Spec**: Resolved 4-note phrase (D3 → F3 → A3 → D4) with a harmony voice entering on
the third note (A3+E4). Warm pad swell underneath. Brief percussion hit (soft cymbal) on
the final note. The resolution should feel complete — a full cadence.
**File**: `stinger_discovery_major.wav`

### Stinger: Revelation
**Trigger**: Major lore unlock (one of 5 story revelations — the game's biggest moments)
**Duration**: 8 seconds
**Character**: Awe, dread, understanding. "Everything I thought I knew was wrong."
**Spec**: Starts with a low drone (D2) that swells over 3 seconds. At second 3, the
exploration melody plays in full but in a new mode (D Lydian — the raised 4th creates
wonder). Harmony voices enter in canon at second 5. Full crescendo to second 7, then
sudden cut to silence with a long reverb tail. The silence after is as important as the
sound. This is the player's "Adagio for Strings" moment.
**File**: `stinger_revelation.wav`

---

## 5. Faction Territory Ambient Loops (5 Assets)

These play on a dedicated ambient layer (-30 dB) underneath the music stems. They
create a subtle sense of place when the player enters faction territory. Each should be
a **loopable drone** that reflects the faction's character.

### Concord (Regulatory Authority)
**Character**: Clean, regulated, orderly. The sound of efficient bureaucracy.
**Spec**: Steady 220Hz (A3) sine-wave base with gentle harmonic overtones at 440Hz and
660Hz. Subtle filter sweep (very slow, 30-second cycle). Slight mechanical undertone —
like well-oiled machinery humming beneath the floor. Clean and precise.
**Duration**: 60 seconds (seamless loop)
**File**: `ambient_faction_concord.wav`

### Chitin Hegemony (Organic Expansionists)
**Character**: Organic, alive, slightly unsettling. Clicks and chitinous resonance.
**Spec**: 147Hz (D3) base with FM synthesis wobble (modulator at 3Hz, depth ±8 cents).
Layered with quiet organic clicks (random timing, 1-3 per second). A living, breathing
sound — like being inside a vast organism's respiratory system.
**Duration**: 60 seconds (seamless loop)
**File**: `ambient_faction_chitin.wav`

### Weavers (Constructors)
**Character**: Harmonic, constructive, mathematical beauty. The sound of building.
**Spec**: 330Hz (E4) base with rich harmonic series (E4, B4, E5, G#5 — overtone series).
Slow additive harmonic fading in and out. Occasional metallic resonance ping (like tapping
a tuning fork). Feels precise and intentional — every frequency is placed.
**Duration**: 60 seconds (seamless loop)
**File**: `ambient_faction_weavers.wav`

### Valorin Sovereignty (Military Frontier)
**Character**: Raw, frontier, engine-rumble. The sound of a military outpost.
**Spec**: 110Hz (A2) base with broadband noise layer (filtered white noise, -24dB below
fundamental). Slight engine rumble character — low-frequency vibration. Occasional radio
static burst (very subtle, 1 per 10 seconds). Feels like standing on a ship deck with
engines running below.
**Duration**: 60 seconds (seamless loop)
**File**: `ambient_faction_valorin.wav`

### Communion (Transcendent Collective)
**Character**: Ethereal, transcendent, slightly alien. The sound of something beyond human.
**Spec**: 440Hz (A4) base with ethereal reverb (long tail, 4+ seconds). Layered vocal-like
formant synthesis (no words — just vowel shapes shifting between "ah" and "oh"). Occasional
harmonic that doesn't belong to the overtone series (quarter-tone intervals). Beautiful but
subtly wrong. The most musically complex of the five.
**Duration**: 60 seconds (seamless loop)
**File**: `ambient_faction_communion.wav`

---

## 6. Fracture Space Ambient (1 Asset)

**Context**: Played when the player is in deep fracture space (instability phase >= 2).
Replaces the faction ambient layer entirely.

**Character**: Unsettling, vast, wrong. The sound of reality breaking down. NOT scary in
a jump-scare way — scary in a "the rules don't apply here" way. Reference: the deep ocean
biomes in Subnautica where familiar sounds become alien.

**Spec**: Detuned drone based on 180Hz (between D3 and Eb3 — quarter-tone). Slow LFO on
pitch (±15 cents, 20-second cycle). Granular texture layer — like the exploration ambient
run through a broken processor. Occasional "metric bleed" moments: brief snippets of other
faction ambients bleeding through at random intervals (1-2 per minute, lasting 0.5-1
second each, heavily filtered). The suggestion that the containment infrastructure is
leaking other spaces into this one.

**Duration**: 90 seconds (seamless loop — longer than faction ambients to reduce repetition
in these high-attention areas)
**File**: `ambient_fracture_space.wav`

---

## 7. Harmonic Plan (Key Relationships)

All music and ambients share a harmonic framework to ensure clean crossfades:

```
Tonic: D (all states share this root)

EXPLORATION:  D dorian    (D E F G A B C)     — warm, open, medieval
COMBAT:       D minor     (D E F G A Bb C)    — dark, driving
TENSION:      D phrygian  (D Eb F G A Bb C)   — exotic, anxious
DOCK:         D major     (D E F# G A B C#)   — bright, safe
FRACTURE:     D minor + quarter-tone 3rd/7th  — uncanny valley

Faction ambients:
  Concord:    A (dominant of D)   — resolves naturally to tonic
  Chitin:     D (same as tonic)   — blends seamlessly
  Weavers:    E (supertonic)      — slight tension, constructive
  Valorin:    A (dominant, low)   — military strength
  Communion:  A (dominant, high)  — transcendent resolution
```

This means ANY state can crossfade to ANY other without key clashes. The harmonic
relationships are designed so transitions always sound intentional.

---

## 8. Mixing Notes

### Stem Isolation
Each stem must work in isolation AND in combination. Test each stem:
1. Solo — does it sound intentional?
2. With one other layer — does the combination add something?
3. All four layers — does the full mix feel balanced?
4. With a faction ambient underneath — any frequency conflicts?

### Crossfade Compatibility
Test these specific transitions (the most common in gameplay):
- EXPLORATION → COMBAT (1.5s fade, percussion leads)
- COMBAT → EXPLORATION (3s fade, percussion drops first)
- EXPLORATION → TENSION (2s fade)
- EXPLORATION → DOCK (immediate cut + 2s fade-in)
- EXPLORATION → FRACTURE (3s fade, deliberate overlap)
- Any state → EXPLORATION (the "return to normal" — must feel relieving)

### Dynamic Layer Behavior
The game may fade individual layers independently. Common scenarios:
- **Early game**: Only bass + pad play (melody and percussion added after first trade)
- **Low-danger exploration**: Bass + pad + melody (no percussion)
- **Rising tension**: Add percussion from exploration gradually before switching to tension state
- **Combat winding down**: Drop percussion first, then melody, leaving bass + pad as "aftermath"

---

## 9. Delivery Checklist

### Stems (20 files)
- [ ] `stem_exploration_bass.wav`
- [ ] `stem_exploration_pad.wav`
- [ ] `stem_exploration_melody.wav`
- [ ] `stem_exploration_percussion.wav`
- [ ] `stem_combat_bass.wav`
- [ ] `stem_combat_pad.wav`
- [ ] `stem_combat_melody.wav`
- [ ] `stem_combat_percussion.wav`
- [ ] `stem_tension_bass.wav`
- [ ] `stem_tension_pad.wav`
- [ ] `stem_tension_melody.wav`
- [ ] `stem_tension_percussion.wav`
- [ ] `stem_dock_bass.wav`
- [ ] `stem_dock_pad.wav`
- [ ] `stem_dock_melody.wav`
- [ ] `stem_dock_percussion.wav`
- [ ] `stem_fracture_bass.wav`
- [ ] `stem_fracture_pad.wav`
- [ ] `stem_fracture_melody.wav`
- [ ] `stem_fracture_percussion.wav`

### Stingers (3 files)
- [ ] `stinger_discovery_minor.wav` (3s)
- [ ] `stinger_discovery_major.wav` (5s)
- [ ] `stinger_revelation.wav` (8s)

### Faction Ambients (5 files)
- [ ] `ambient_faction_concord.wav` (60s loop)
- [ ] `ambient_faction_chitin.wav` (60s loop)
- [ ] `ambient_faction_weavers.wav` (60s loop)
- [ ] `ambient_faction_valorin.wav` (60s loop)
- [ ] `ambient_faction_communion.wav` (60s loop)

### Fracture Ambient (1 file)
- [ ] `ambient_fracture_space.wav` (90s loop)

### Total: 29 audio files

---

## 10. Priority Order

If producing in phases:

1. **Phase 1 (MVP)**: Exploration stems (4) + Combat stems (4) + Discovery Minor stinger (1) = **9 files**
   - This covers 85% of playtime and the core emotional loop.

2. **Phase 2 (Depth)**: Tension stems (4) + Dock stems (4) + Discovery Major stinger (1) = **9 files**
   - Adds the transitional states and full discovery reward.

3. **Phase 3 (Polish)**: Fracture stems (4) + Revelation stinger (1) + Fracture ambient (1) = **6 files**
   - The cosmic horror layer. Late-game content.

4. **Phase 4 (Atmosphere)**: Faction ambients (5) = **5 files**
   - Subtle territorial flavor. Lowest priority but high impact per effort.
