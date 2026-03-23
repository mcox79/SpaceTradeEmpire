# Space Trade Empire — Composer Brief v0

**Status**: CONTENT DRAFT
**Date**: 2026-03-21
**Delivery format**: Adaptive stems, 48kHz/24-bit WAV
**Target playtime**: 30-60 hours

---

## 1. Game Overview

Space Trade Empire is a single-player space trading simulation built in Godot 4 with a C# simulation core. The player begins as a small trader running cargo between stations along an ancient network of stable-space corridors ("threads"). Over 30-60 hours, they discover that the thread network is failing, that their experimental drive module is ancient alien technology, and that the galaxy's five faction civilizations are locked in a dependency ring engineered by the thread builders. The tone is literary science fiction — closer to Ursula K. Le Guin and Alastair Reynolds than to Star Wars. Combat exists but is not the primary verb. Trade, exploration, diplomacy, and the slow accumulation of understanding define the experience. The emotional register is wonder first, tension second, melancholy third.

---

## 2. Emotional Arc

The player's journey follows a five-phase emotional trajectory that the score must support:

**Phase 1 — Smallness (Hours 0-5)**: The player is a nobody with a cargo hold and a loan. The galaxy is vast and indifferent. Music should convey the loneliness of space, the small satisfaction of a profitable trade, the comfort of docking at a station. The player is a grain of sand on a beach. The beach is beautiful.

**Phase 2 — Competence (Hours 5-15)**: The player has established trade routes, earned faction reputation, upgraded their ship. Music should convey growing confidence — the galaxy is still vast, but the player is no longer lost in it. Rhythmic elements emerge. The loneliness is still there but it has company now.

**Phase 3 — Discovery (Hours 15-25)**: The fracture module reveals off-thread space. Instability zones, ancient ruins, metric bleed. The galaxy is not what the player thought it was. Music shifts from confidence to awe and unease. What was familiar becomes strange. What was strange becomes home. The emotional register is the vertigo of paradigm shift.

**Phase 4 — Weight (Hours 25-40)**: The player understands the stakes. The thread network is failing. The factions are trapped. The player's choices will determine the galaxy's future. Music should convey the gravity of responsibility without melodrama. Heavy, but not loud. This is not a superhero moment — it is the moment you realize no one else is going to do this.

**Phase 5 — Resolution (Hours 40-60)**: Endgame. The player commits to a path. Music should reflect the specific emotional quality of each path: Reinforce (duty, cost, the weight of choosing the known), Naturalize (hope, risk, the vertigo of choosing the unknown), Renegotiate (transcendence, loss, the strangeness of choosing something that is not a choice at all but a conversation with the metric itself).

---

## 3. Faction Timbral Palettes

Each faction occupies a distinct sonic territory. When the player is in faction space, the ambient music should gradually shift toward that faction's palette. The transitions are the most important part — not sudden cuts, but a slow timbral migration over 30-60 seconds that makes the player feel the border crossing.

### Concord

- **Instruments/Synthesis**: Orchestral strings (warm, not dramatic), woodwinds (clarinet, oboe), subtle brass pads, clean sine-wave synths underneath providing harmonic stability. Piano for melodic statements.
- **Melodic Character**: Diatonic, resolved, formally structured. Phrases complete themselves. Cadences are clear. The music follows rules and finds satisfaction in following them. Counterpoint is present — multiple voices in conversation, each respecting the others.
- **Reference Tracks**: *Civilization VI* main theme (Christopher Tin) for institutional warmth; *Interstellar* "Cornfield Chase" (Hans Zimmer) for the quiet dignity of large-scale responsibility; *Mass Effect* Citadel ambient for the hum of a functioning institution.
- **Emotional Keywords**: Warm, orderly, trustworthy, quietly proud, institutional

### Chitin Syndicates

- **Instruments/Synthesis**: Prepared piano, marimba, metallic percussion (gamelan-adjacent), granular synthesis, probability-driven generative elements (notes triggered by algorithmic processes, not strict sequencing), rapid arpeggiated patterns that feel like computation.
- **Melodic Character**: Modal, unpredictable within structure. Phrases begin with clear motifs but evolve through variation — the same theme never plays exactly the same way twice. Rhythms shift between regular and irregular meters. The music feels alive, twitchy, alert.
- **Reference Tracks**: *Blade Runner 2049* (Benjamin Wallfisch/Hans Zimmer) for industrial alienness; *Ex Machina* (Geoff Barrow/Ben Salisbury) for the clinical beauty of something almost-but-not-quite alive; *FTL* combat music for the kinetic energy of systems in motion.
- **Emotional Keywords**: Calculating, alive, unpredictable, curious, electric

### Weavers

- **Instruments/Synthesis**: Low strings (cello, double bass), resonant drones, bowed metal (singing bowls, bowed vibraphone), slow-attack pads with visible harmonic overtones, the sound of large structures vibrating. Tubular bells for structural accents.
- **Melodic Character**: Pentatonic foundations with long, sustained phrases. Music built on sustained tones that change slowly — the listener feels harmonics shift beneath them like tectonic plates. Rhythm is implied by harmonic rhythm, not by percussion. Nothing is rushed. Everything resonates.
- **Reference Tracks**: *Arrival* (Johann Johannsson) for the patience of inhuman intelligence; *Subnautica* deep ocean ambient for the beauty of being inside something vast and structural; *Dark Souls III* Firelink Shrine for the melancholy of builders whose work outlasts them.
- **Emotional Keywords**: Patient, massive, resonant, ancient, enduring

### Valorin Clans

- **Instruments/Synthesis**: Drums (taiko-scale body drums, military snare, rapid hand percussion), distorted guitar tones (not metal — more post-rock, tremolo-picked), brass stabs, aggressive bass synths, the sharp crack of kinetic impacts.
- **Melodic Character**: Minor key, rhythmically driven, built on ostinato patterns that accelerate. Melodies are short, punchy, repeated with increasing intensity. The music wants to go somewhere fast. Dynamic range is extreme — quiet-loud-quiet transitions within single phrases.
- **Reference Tracks**: *Mad Max: Fury Road* (Junkie XL) for controlled aggression; *Hades* combat music (Darren Korb) for the joy of being good at violence; *Battlestar Galactica* (Bear McCreary) taiko-and-strings passages for military honor.
- **Emotional Keywords**: Fierce, fast, fearless, loyal, blunt

### Drifter Communion

- **Instruments/Synthesis**: Human voice (wordless choir, single alto, throat singing), crystal bowls, bowed glass, reverb-heavy processing, field recordings of natural resonance (wind through structures, water over stone), silence as an instrument. Sparse. Every note exists in space.
- **Melodic Character**: Microtonal, unhurried, built on intervals that do not resolve in Western harmony. Phrases hang in the air. The music does not demand attention — it is present, the way a landscape is present. Silence is load-bearing. A passage with eight notes and twelve seconds of silence between them should feel complete.
- **Reference Tracks**: *Outer Wilds* (Andrew Prahlow) for the intimacy of cosmic discovery; Hildegard von Bingen choral works for spiritual resonance without religious specificity; *Stalker* (Edward Artemyev) for the sound of space that is alive but not speaking.
- **Emotional Keywords**: Still, perceptive, luminous, spacious, sacred

---

## 4. Track Descriptions (14 Tracks)

### Track 1: "Open Space" (Default Flight)

**Context**: The player is flying between stations in stable thread-space. No threats. No urgency. The galaxy scrolling past.

**Direction**: This is the track the player will hear most. It must sustain hundreds of hours of play without becoming irritating or monotonous. Build it from 3-4 interchangeable stem layers that the adaptive system can combine in varying arrangements. Base layer: a slow, warm pad (strings or synth) establishing the key center. Layer 2: a sparse melodic voice (piano, or clean synth) playing fragments of a theme that never quite completes — always arriving at the next phrase just as the player's attention drifts. Layer 3: ambient texture (engine hum, distant star noise, the vibration of the hull). Layer 4 (optional, mood-dependent): a rhythmic element that enters during longer transits — not a beat, but a pulse. The heartbeat of a ship in motion.

**Duration**: 4-6 minutes looped, with 3 variation arrangements.
**Key**: Major or Lydian. Resolved. Comfortable.
**Dynamic range**: Quiet. This is background music for thinking and planning.

### Track 2: "The Lanes" (Lane Transit)

**Context**: The player is in lane transit — a 10-30 second journey through a thread corridor, with visual streaks of light and the hum of stable-space compression.

**Direction**: A transitional piece. Brief, kinetic, slightly accelerated. The sensation of controlled speed — not thrilling, but purposeful. A rhythmic pulse (quarter notes, steady, mechanical) with a rising harmonic progression that resolves as the player arrives at the destination. The same base key as "Open Space" but with added motion. Crossfade smoothly from Open Space on entry and back to Open Space (or faction theme) on exit.

**Duration**: 30-45 seconds, seamless loop during longer transits.
**Key**: Same key center as Open Space, modulating toward destination faction's tonal center.
**Dynamic range**: Moderate. Slightly louder than Open Space.

### Track 3: "Tension Rising" (Contested Space)

**Context**: The player enters a system with active warfront, hostile NPCs, or elevated instability.

**Direction**: The Open Space foundation with added dissonance. The warm pad shifts to minor. The melodic fragments become shorter, more angular. A low rhythmic pulse enters — not drums, but a heartbeat-like throb from the bass register. The texture layers add subtle high-frequency elements (metallic scrapes, distant impacts, sensor pings). The music should create unease without announcing combat. The player should feel watched.

**Duration**: 3-4 minutes looped.
**Key**: Minor or Phrygian mode. Unresolved.
**Dynamic range**: Moderate, with dynamic swells that rise and fall unpredictably.

### Track 4: "Engagement" (Combat)

**Context**: Active combat. Weapons fire, heat management, tactical decisions.

**Direction**: Rhythmically driven but not bombastic. Combat in STE is strategic, not frantic — the player is managing heat budgets and zone armor, not mashing attack buttons. The rhythm should be relentless but measured: a driving pulse at 100-110 BPM, with emphasis on odd beats to create a stumbling urgency. Melodic content is minimal — short brass stabs, percussive synth hits, the crack of railguns translated into musical accents. Build intensity through layering, not volume. The most intense combat moments are when the heat gauge is climbing and the player must choose between firing and cooling — the music should breathe with this rhythm.

**Duration**: 2-3 minutes looped, with intensity stems that layer based on combat heat level.
**Key**: Minor, modulating. Key center shifts toward the enemy faction's tonal center.
**Dynamic range**: Loud, but controlled. Maximum volume should be 80% of system maximum — save headroom for sound effects.

### Track 5: "Station Air" (Docked — Generic)

**Context**: The player is docked at a station. Trading, fitting modules, reading market data. Safe.

**Direction**: Interior ambiance. The sound of being inside a structure — muffled hull resonance, distant machinery, the murmur of a populated space. Musical content is minimal: a sustained chord, a very occasional melodic fragment (as if someone two decks away is playing an instrument), environmental sounds that suggest life. This track is the canvas that faction themes paint over — at faction stations, "Station Air" crossfades into the faction's docked variant over 10-15 seconds.

**Duration**: 3-4 minutes ambient loop.
**Key**: Neutral — fifths and octaves, no strong modal identity.
**Dynamic range**: Very quiet. Below Open Space. This is furniture music.

### Track 6-10: Faction Themes (5 Tracks)

Each faction theme exists in two variants: **ambient** (plays during flight in faction space, blended with Open Space) and **docked** (plays when docked at a faction station, blended with Station Air).

**Track 6: Concord Theme — "The Civil Service"**
Strings and piano. A warm, orderly melody in 4/4 time. The ambient variant is a slow, stately progression. The docked variant adds woodwind countermelody and the subtle hum of efficient machinery. This should sound like the world's most competent waiting room — comfortable, institutional, slightly smug.

**Track 7: Chitin Theme — "The Trading Floor"**
Prepared piano, marimba, granular synthesis. The ambient variant is a restless pattern of interlocking rhythms — two or three rhythmic cells playing at different tempos, occasionally synchronizing. The docked variant adds market sounds processed into music: price ticks become rhythmic clicks, trade confirmations become melodic pings. The station should sound alive with data.

**Track 8: Weaver Theme — "The Long Thread"**
Bowed cello, singing bowls, deep resonant drones. The ambient variant is a single sustained tone that slowly evolves — overtones shifting, harmonics emerging and retreating. The docked variant adds the vibration of large structures: tubular bells at the edge of hearing, the creak of hull plating under tension. Time moves differently in Weaver space. The music should make the player breathe more slowly.

**Track 9: Valorin Theme — "The Forward Line"**
Taiko drums, tremolo guitar, brass stabs. The ambient variant is a military march deconstructed into fragments — a snare roll here, a bass drum hit there, the ghost of a bugle call. The docked variant adds the bustle of a military encampment: weapon maintenance sounds, the bark of orders, the rattle of ammunition feeds. Valorin stations are not comfortable. They are ready.

**Track 10: Communion Theme — "The Listening"**
Wordless alto voice, crystal bowls, processed silence. The ambient variant is the most minimal music in the game: a single voice singing a phrase that floats in reverb, surrounded by 8-12 seconds of near-silence before the next phrase. The docked variant adds glass harmonics and the barely-audible hum of crystal resonance. Communion stations should feel like the quietest places the player has ever been. The music is the sound of someone paying very close attention to something the player cannot yet hear.

### Track 11: "Haven" (Home Station)

**Context**: The player is docked at Haven — their personal station. The only place in the galaxy that is entirely theirs.

**Direction**: The emotional heart of the score. This track must feel like coming home. Build it from a simple melodic theme — the "Haven theme" — that appears nowhere else in the score. Piano or acoustic guitar, unadorned. The melody should be singable, memorable, and slightly sad — the sadness of having built something beautiful in a galaxy that is falling apart. As Haven upgrades through Citadel stages, the arrangement grows: Stage 1 is solo piano. Stage 2 adds strings. Stage 3 adds a full chamber arrangement. Stage 4 adds choir — wordless, warm, the sound of a community that exists because the player built them a place to exist.

**Duration**: 3-5 minutes, with 4 arrangement variants (one per Haven tier).
**Key**: Major, with modal mixture (borrowed minor chords for depth). The Haven theme should share its first four notes with the Open Space theme — "home" is "the galaxy, made small and safe."
**Dynamic range**: Moderate. Warmer than Station Air but not loud.

### Track 12: "The Threading" (Fracture Travel)

**Context**: The player is using the fracture module to travel through metric-variant space — off-thread, through instability, into the unknown.

**Direction**: The most experimental track in the score. This is what broken space sounds like to a pilot whose perception is adapting to metric bleed. Start with the Open Space base layer, then progressively degrade it: pitch-shift the pad in microtonal increments, granulate the melodic fragments until they become textural clouds, replace the ambient engine hum with processed field recordings of ice cracking, metal expanding, glass resonating. The music should sound like the player's familiar sonic world coming apart and reassembling into something alien but beautiful. At high instability (Phase 3+), the degradation becomes total — all recognizable musical elements dissolve into pure texture and resonance. At Phase 4 (Void), paradoxically, the music becomes clearer: a single, clean tone — the accommodation geometry stabilizing the pilot's perception. Clarity in the heart of chaos.

**Duration**: 2-4 minutes, with 4 instability-phase variants.
**Key**: Starts in Open Space key, drifts microtonally, arrives at Communion key center by Phase 4.
**Dynamic range**: Variable. Phase 1-2: quiet. Phase 3: moderately loud (the only time ambient music competes with the soundscape). Phase 4: very quiet.

### Track 13: "Pentagon Break" (Crisis Event)

**Context**: A major narrative event — warfront escalation, faction betrayal, thread network failure, Lattice drone incursion. The galaxy is breaking.

**Direction**: A stinger followed by sustained tension. The stinger: a single orchestral hit (full orchestra, fff) that decays into tremolo strings. The sustained section: a slow, grinding bass drone with dissonant upper harmonics, rhythmic tension from irregular time signatures (7/8 or 5/4), and fragmented versions of faction themes playing simultaneously in different keys — the pentagon falling apart, musically. This track should be rare and terrifying. It plays only during scripted narrative moments and major systemic events. The player should associate this sound with "something has changed and cannot be unchanged."

**Duration**: 15-second stinger + 2-minute sustained section.
**Key**: Polytonal. All five faction key centers sounding simultaneously = dissonance.
**Dynamic range**: The loudest moment in the score. Brief.

### Track 14: "Resolution" (Credits/Endgame)

**Context**: The game is ending. The player has committed to a path. The consequences unfold.

**Direction**: Three variants required.

**Variant A — "The Cage Holds" (Reinforce)**: The Haven theme, slowed, in a minor key arrangement with full orchestral support. Brass provides gravity. Strings provide warmth. The melody is recognizable but heavier — the same home, carrying more weight. A military snare enters in the final minute, providing structure. The cage holds. It is not a bad cage. The music should make the player feel proud and burdened simultaneously.

**Variant B — "The Cage Opens" (Naturalize)**: The Haven theme, transposed up a whole step, in an open arrangement with Communion vocal elements. The melody begins in the original key and modulates upward — literally ascending. Crystal bowls and glass harmonics replace brass. The arrangement thins rather than thickens: by the final minute, only the vocal line and a single sustained chord remain. Freedom sounds like less, not more. The music should make the player feel hopeful and exposed.

**Variant C — "The Conversation" (Renegotiate)**: The Haven theme, deconstructed. The four-note motif plays once, clearly, then is repeated with microtonal variation — the metric responding, altering the phrase. A call-and-response develops between the original melody and its transformed echo. The echo is not random; it develops its own logic, its own beauty. By the final minute, the original melody and the transformed version are playing simultaneously in harmony that no Western tonal system would produce, but that sounds right — the accommodation geometry applied to music itself. The music should make the player feel awe and uncertainty in equal measure.

**Duration**: 4-6 minutes each. No loop — these play once.
**Dynamic range**: Full range. These are the only tracks in the score that use the full dynamic spectrum from ppp to fff.

---

## 5. Adaptive Music System

The game's music system manages 5 simultaneous states with crossfade transitions:

| State | Trigger | Active Tracks | Transition Time |
|-------|---------|--------------|-----------------|
| **Ambient** | Default flight, no threats | Open Space + faction ambient blend | N/A (default) |
| **Transit** | Lane entry | The Lanes | 3-second crossfade |
| **Alert** | Hostile contact, warfront zone, elevated instability | Tension Rising | 8-second crossfade |
| **Combat** | Weapons engagement | Engagement | 2-second crossfade (fast) |
| **Docked** | Station dock | Station Air + faction docked blend | 5-second crossfade |

### State Transition Rules

- **Ambient to Alert**: 8-second crossfade. The warm pad dims; the tension pulse fades in underneath. The player should notice the change subconsciously before they notice it consciously.
- **Alert to Combat**: 2-second crossfade. Fast. The tension track cuts; the combat rhythm enters immediately. This is the only abrupt transition in the system. Combat should feel like it interrupts everything.
- **Combat to Alert**: 10-second crossfade. Slow return. The combat rhythm fades, the tension pulse re-enters. The player should feel the adrenaline draining.
- **Combat to Ambient**: 15-second crossfade (if no remaining threats). Even slower. The return to normalcy should feel earned.
- **Any to Docked**: 5-second crossfade. Docking is always a relief. The transition should feel like a door closing behind you.
- **Fracture Travel**: Overrides all states. The Threading plays with instability-phase-appropriate stems active. On exit, 10-second crossfade back to the appropriate state.

### Phase-Density Guidelines (Audio Density Arc)

The game's overall audio density follows the player's emotional journey.
Music is not wallpaper — it is pacing. The density arc ensures that when
music plays, it *means something*.

| Phase | Hours | Density | Principle |
|-------|-------|---------|-----------|
| **Smallness** (0-5) | Early | **Silence-forward** | Engine hum, environmental audio, occasional musical stings. The galaxy is empty and vast. When music plays, the player pays attention. Long stretches with zero musical content — only the ship, the stars, and the void. This establishes: music = significance. |
| **Competence** (5-15) | Mid-early | **Emerging** | Faction territory themes fade in as the player learns to read the galaxy. Pentagon adjacency blending activates. Music becomes a navigational tool — the player *hears* when they cross faction borders. Still sparse compared to AAA convention. Stations are the primary "music happens here" locations. |
| **Discovery** (15-25) | Mid | **Present but fractured** | Fracture travel introduces "The Threading" — the most experimental track. Standard flight music plays more frequently but competes with instability audio design. The music is *changing*, matching the player's paradigm shift. Familiar sounds degrade; new ones emerge. |
| **Weight** (25-40) | Mid-late | **Full presence** | The galaxy is alive with consequence. Combat stems, warfront dissonance, Haven theme growing with each tier. Music is continuous during meaningful activities (warfront, faction chain missions, megaproject construction). Flight silence is still used but feels like "the calm before" rather than "emptiness." |
| **Resolution** (40-60) | Late | **Orchestral** | Full musical support. Track 14 variants play once at maximum dynamic range. The score earns its loudest moment because 40 hours of restraint precede it. |

**Critical exception**: Communion space is ALWAYS silence-forward, regardless of
phase. Even at hour 50, entering Communion territory drops audio density to
Phase 1 levels. The contrast with surrounding faction spaces makes Communion
feel alien after 40 hours. This is the Communion's sonic identity — it never
normalizes. The silence IS the theme.

**Implementation note**: The density arc is not a volume curve. It is a
*probability* curve — the likelihood that any given moment of gameplay has
active musical content. Phase 1: ~20% of gameplay has music. Phase 4: ~70%.
Phase 5: ~90%. Communion space: always ~15%, overriding phase.

### Intensity Stems (Combat)

The Engagement track ships with 4 intensity stems:

1. **Base**: Rhythmic pulse, bass drone. Always active during combat.
2. **Escalation**: Added percussion, harmonic dissonance. Activates when player hull drops below 70%.
3. **Crisis**: Full arrangement, maximum rhythmic density. Activates when player hull drops below 40% OR heat exceeds 80%.
4. **Resolution**: Rhythmic deceleration, harmonic resolution. Activates when last hostile is destroyed (holds for 5 seconds before transitioning to Alert/Ambient).

---

## 6. Pentagon Adjacency Blending Rules

When the player is in space bordering two faction territories, the ambient music blends both faction palettes:

| Border | Blend Character |
|--------|----------------|
| Concord-Weavers | Strings (Concord) + deep drones (Weavers). The most consonant border — both factions value structure. |
| Concord-Communion | Piano (Concord) + vocal (Communion). The warmest border — institutional care meets spiritual warmth. |
| Chitin-Valorin | Prepared piano (Chitin) + military percussion (Valorin). The most energetic border — both factions move fast. |
| Chitin-Weavers | Granular synthesis (Chitin) + bowed metal (Weavers). The most alien border — mechanical meets organic. |
| Valorin-Communion | Drums (Valorin) + silence (Communion). The most dramatic border — aggression meets emptiness. Drums play into reverb that stretches into Communion silence. |
| Warfront zones | Both faction themes play simultaneously in different keys. Dissonance proportional to warfront intensity. |

Blend ratio is determined by proximity: at the exact border, 50/50. Moving deeper into one faction's territory shifts to 80/20 over 60 seconds of travel.

---

## 7. Technical Requirements

### Stem Format
- 48kHz / 24-bit WAV, stereo
- Each track delivered as separated stems (minimum 4: bass/pad, melody, percussion/rhythm, texture/ambient)
- Stems must be perfectly time-aligned for runtime mixing
- Provide a premixed reference for each track alongside the stems

### Loop Points
- All looping tracks must have marked loop points with zero-crossing at both splice boundaries
- Provide loop point timestamps in a companion metadata file (JSON format)
- Loop transitions must be inaudible at any playback volume

### Dynamic Range
- Master all stems to -14 LUFS integrated, -1 dBTP maximum
- Leave headroom for game SFX — music should never compete with combat audio or UI feedback
- The game's audio engine applies real-time ducking during dialogue; stems must sound correct at -6dB attenuation

### Delivery
- Per-track folders containing stems, premix, and metadata
- Naming convention: `[TrackNumber]_[TrackName]_[StemName].wav` (e.g., `04_Engagement_Bass.wav`)
- Version control: append `_v[N]` for revisions

---

## 8. Silence as Design Element

The Communion's sonic identity is built on silence. This is not a metaphor — silence is a composed element with specific rules:

- In Communion space, the ambient music volume drops to 40% of normal. The reduction is gradual (30-second fade) and should feel like the galaxy itself getting quieter.
- Between musical phrases in the Communion theme, there must be minimum 8 seconds of near-silence (ambient noise floor only — no musical content). These silences are part of the composition, not gaps.
- When the player is at maximum Communion reputation (Pathfinder tier), a subtle additional layer becomes audible in the silence: a very low-frequency hum (30-50 Hz, barely perceptible) that suggests something listening. This layer is inaudible at lower reputation tiers.
- The Silence Field module (Communion T2 module) triggers 10 seconds of complete audio silence (all music stems muted, only essential UI sounds remain). When the silence ends, the music returns at 60% volume and fades back to normal over 15 seconds. The player should feel the silence as a physical sensation.
- During the Renegotiate endgame sequence, silences in the Communion theme are replaced by the metric's "response" — microtonal echoes of the Haven theme, as if the universe is singing back. This is the only time the silence is filled. The filling should feel significant.

Silence is the Communion's gift to the score. Use it deliberately. Every second of silence in Communion space should feel like someone holding their breath.
