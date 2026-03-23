# ElevenLabs Voice & Sound Production Guide

**Status**: All dialogue content is FINALIZED (722+ unique lines, zero placeholders).
Ready for voice generation.

**Strategy**: Start with Tutorial Act 1-2 (Ship Computer + Maren) to validate
the pipeline before committing to full production.

---

## Part 1: Readiness Assessment

### Dialogue Content Status

| Content File | Lines | Characters | Status | Notes |
|---|---|---|---|---|
| TutorialContentV0.cs | ~60 unique | ~2,800 chars | FINAL | 4 speakers, template vars in 1 line only |
| FirstOfficerContentV0.cs | ~180 | ~8,500 | FINAL | 3 FOs x 26 triggers x 5 tiers |
| FactionDialogueContentV0.cs | ~65 | ~3,200 | FINAL | 5 factions x 3 rep tiers |
| KeeperDialogueContentV0.cs | ~20 | ~1,400 | FINAL | 5-tier evolution arc |
| CommunionRepDialogueContentV0.cs | ~12 | ~1,100 | FINAL | 3-tier arc |
| RevelationContentV0.cs | ~25 | ~2,400 | FINAL | 5 revelations + FO reactions |
| DataLogContentV0.cs | ~100 | ~12,000 | FINAL | 5 ancient scientists |
| WarFacesContentV0.cs | ~30 | ~2,800 | FINAL | 3 war NPCs |
| AdaptationFragmentContentV0.cs | ~130 | ~12,000 | FINAL | Lore text (read, not spoken) |

**Template variables** (need manual substitution before TTS):
- `{credits_earned}`, `{nodes_visited}`, `{combats_won}`, `{modules_equipped}` in Graduation_Summary
- `{station}` in wrong-station warnings
- `{GOOD}`, `{STATION}` in FO reactive lines

**Action**: Generate with placeholder values (e.g., "forty-seven credits earned")
or leave template lines for dynamic TTS at runtime.

---

## Part 2: ElevenLabs Applicability Rankings

### Tier 1: PERFECT FIT (voice generation)

These are the primary use case for ElevenLabs TTS. High quality expected.

| Asset Category | Count | Priority | Why ElevenLabs |
|---|---|---|---|
| **Tutorial VO** (Ship Computer + Maren) | ~25 lines | HIGHEST | First thing players hear. Short, clear lines. |
| **FO Hails** (3 candidates) | 3 lines | HIGHEST | Character-defining moments. Need distinct voices. |
| **FO Selection Intros** (3 candidates) | 3 lines | HIGHEST | Player chooses based on these. |
| **Tutorial FO lines** (Acts 2-7) | ~35 unique | HIGH | Maren dominates, Dask/Lira cameo |
| **FO Post-Tutorial Dialogue** | ~180 lines | HIGH | Bulk of character voice work |
| **Keeper Dialogue** | ~20 lines | HIGH | Evolving AI voice (tier 0-4) |
| **Revelation Gold Toasts** | ~15 lines | HIGH | Dramatic narrative beats |
| **Faction Dock Greetings** | ~25 lines | MEDIUM | 5 distinct faction voices |
| **War Faces NPCs** | ~30 lines | MEDIUM | 3 characters (Keris, Hale, Voss) |
| **Communion Rep (Syrel)** | ~12 lines | MEDIUM | Ethereal mystic voice |
| **Data Log Scientists** | ~100 lines | LOW | 5 scientists, long exchanges, could stay text |
| **Narrator** | 1 line | LOW | Single selection-phase prompt |

### Tier 2: GOOD FIT (sound effects)

ElevenLabs SFX v2 generates up to 30s clips at 48kHz. Good for stylized/sci-fi sounds.

| Asset | Count | Priority | ElevenLabs Quality | Notes |
|---|---|---|---|---|
| **Dock chime** | 1 | HIGH | EXCELLENT | Short tonal, exactly what AI SFX excels at |
| **Warp transit whoosh** | 1 | HIGH | EXCELLENT | Sci-fi sweep, great prompt adherence |
| **UI sounds** (buy/sell/tab/toast) | 8 | HIGH | EXCELLENT | Short tonal clicks, very clean from AI |
| **Shield break** | 1 | HIGH | GOOD | Energy discharge, may need layering |
| **Explosion** | 2-3 variants | HIGH | GOOD | Ship destruction, need punch |
| **Discovery chimes** | 3 | MEDIUM | GOOD | Currently procedural; AI could sound richer |
| **Laser fire** | 2-3 variants | MEDIUM | GOOD | Energy weapon zap/pulse |
| **Kinetic fire** | 2-3 variants | MEDIUM | GOOD | Ballistic thud/crack |
| **Ambient station hum** | 5 (per faction) | MEDIUM | EXCELLENT | Looping ambient, great for AI |
| **Star class ambience** | 7 | MEDIUM | EXCELLENT | Deep space drones, perfect for AI |
| **Risk meter alerts** | 12 | LOW | EXCELLENT | Rising tension tones |
| **Combat victory sting** | 1 | LOW | GOOD | Short musical sting |
| **Combat defeat sting** | 1 | LOW | GOOD | Short musical sting |

### Tier 3: POOR FIT (use other sources)

| Asset | Why Not ElevenLabs |
|---|---|
| **Music stems** (23 needed) | AI music generators (Suno, Udio) are better for full compositions |
| **Engine thrust loop** | Already have working procedural engine audio |
| **Dread ambient layers** | Already have working procedural 5-phase system |
| **Fauna proximity harmonics** | Already working procedurally |

---

## Part 3: Production Phases

### Phase 0: PIPELINE TEST (do this first)

Generate 5 lines to validate quality and naming before bulk production.

| # | Speaker | ElevenLabs Input (with pacing markup) | File Name |
|---|---|---|---|
| 1 | Ship Computer | `Systems online. <break time="0.3s" /> Hull integrity: marginal. <break time="0.3s" /> Credits: minimal. <break time="0.4s" /> One station in sensor range.` | `vo_computer_awaken_00.wav` |
| 2 | Ship Computer | `Controls are live. <break time="0.3s" /> W-A-S-D to fly, left-click to set course. <break time="0.3s" /> The station ahead is your only option. <break time="0.3s" /> Dock with E when close.` | `vo_computer_flight_intro_00.wav` |
| 3 | Maren | `Captain, this is Maren Voss. <break time="0.4s" /> I answered your posting because nobody else would. <break time="0.5s" /> I've been monitoring your feeds since launch <break time="0.3s" /> — your situation is worse than advertised.` | `vo_maren_maren_hail_00.wav` |
| 4 | Maren | `Low credits, <break time="0.2s" /> empty hold, <break time="0.2s" /> one station in range. <break time="0.4s" /> But I see opportunity in the local market data. <break time="0.3s" /> Dock up and I'll show you what I mean.` | `vo_maren_maren_hail_01.wav` |
| 5 | Maren | `Prices here are distorted <break time="0.3s" /> — the warfront's got the mining runs backed up. <break time="0.3s" /> But distortion creates margin. <break time="0.4s" /> I see opportunity.` | `vo_maren_maren_settle_00.wav` |

**Settings for test batch:**
- Ship Computer: Stability 0.90, Style 0.05, Speed 0.95
- Maren: Stability 0.60, Style 0.35, Speed 0.95

**v3 alternative** (if using v3 model, replace SSML with audio tags):
- Line 3 v3: `Captain, this is Maren Voss. [pause] I answered your posting because nobody else would. [long pause] I've been monitoring your feeds since launch [short pause] — your situation is worse than advertised.`

**Also generate 3 test SFX:**

| # | Sound | Prompt | File Name |
|---|---|---|---|
| 1 | Dock chime | "Gentle sci-fi docking confirmation chime, two ascending tones, clean and futuristic, space station arrival, 1 second" | `sfx_dock_chime.wav` |
| 2 | Warp whoosh | "Sci-fi warp drive activation, deep bass rumble building to high-frequency whoosh, hyperspace jump, 3 seconds" | `sfx_warp_whoosh.wav` |
| 3 | UI buy | "Soft futuristic UI confirmation beep, purchase complete, clean digital tone, 0.5 seconds" | `sfx_ui_buy.wav` |

**Evaluate**: Does the voice match the character? Does the SFX feel right in-game?
If yes, proceed to Phase 1.

---

### Phase 1: TUTORIAL VO (test the full pipeline)

All tutorial lines. ~60 files. This covers the first hour of gameplay.

### Phase 2: FO HAILS + SELECTION + COMBAT SFX

The other two FO voices (Dask, Lira) plus core SFX.

### Phase 3: POST-TUTORIAL FO + FACTIONS + REMAINING SFX

Bulk production of all remaining dialogue and sound effects.

### Phase 4: DATA LOGS + WAR FACES + KEEPER

Lower-priority voices. Consider whether data logs should be voiced or stay as text.

---

## Part 3B: ElevenLabs Delivery Control Reference

ElevenLabs offers several mechanisms for controlling pacing, cadence, pauses,
and emotional delivery. **Use these in every line you generate.**

### Pause & Pacing Controls

| Technique | Syntax | Effect | Model Support |
|---|---|---|---|
| **SSML Break** | `<break time="1.5s" />` | Exact pause (up to 3s) | v2, v2.5, Flash v2 |
| **Audio tag pause** | `[pause]` `[short pause]` `[long pause]` | Natural pause | v3 only |
| **Audio tag beat** | `[continues after a beat]` | Dramatic beat | v3 only |
| **Ellipsis** | `...` | Hesitant pause, trailing off | All models |
| **Em dash** | `--` or `---` | Sharp cut/redirect | All models |
| **Period-space-period** | `. .` | Brief deliberate pause | All models |
| **Speed setting** | 0.7 (slow) to 1.2 (fast) | Global pacing | All models |

### Emotion & Expression Tags (v3 only)

| Tag | Effect |
|---|---|
| `[sighs]` | Audible sigh before/after speech |
| `[exhales]` | Breath release |
| `[whispers]` | Drops to whisper |
| `[sarcastic]` | Sarcastic inflection |
| `[curious]` | Rising, inquisitive tone |
| `[excited]` | Heightened energy |
| `[laughs]` | Brief laugh |
| `[rushed]` | Faster pacing |
| `[slows down]` | Decelerating delivery |
| `[deliberate]` | Intentional, weighted pacing |
| `[stammers]` | Verbal stumble |
| `[drawn out]` | Elongated delivery |

### Pronunciation Helpers

| Technique | Syntax | Use Case |
|---|---|---|
| **Phoneme (CMU)** | `<phoneme alphabet="cmu-arpabet" ph="V EY1 L">Vael</phoneme>` | Unusual names |
| **Alias** | `<lexeme><grapheme>Chitin</grapheme><alias>KY-tin</alias></lexeme>` | Faction names |
| **Phonetic spelling** | Write "KY-tin" directly | Quick fix |

**Names to pre-configure** (add to pronunciation dictionary):
- Maren = "MAR-en" (not "muh-REN")
- Dask = "DAHSK" (rhymes with "task")
- Lira = "LEER-ah" (not "LY-rah")
- Vael = "VAIL" (rhymes with "tale")
- Kesh = "KESH" (short e)
- Oruth = "OR-uth" (not "oh-ROOTH")
- Syrel = "SY-rel" (rhymes with "spiral")
- Chitin = "KY-tin" (not "CHIT-in")
- Valorin = "VAL-or-in"
- Concord = standard pronunciation

### Global Delivery Rules

1. **Never rush clause boundaries.** Insert `<break time="0.3s" />` between
   clauses joined by em dashes. The dashes in dialogue represent a mental
   gear-shift, not a run-on.
2. **Sentence-final words carry weight.** If a sentence ends with a
   thematically important word ("opportunity", "listening", "empire"),
   reduce Speed to 0.9 for that generation to let the word land.
3. **Lists get micro-pauses.** "Hull integrity: marginal. Credits: minimal."
   — each status item needs a beat. Use `. ` (period-space) or
   `<break time="0.4s" />` between items.
4. **Questions lift.** Lines ending in `?` should use slightly higher Style
   (0.1 higher than baseline) to get natural uptick.
5. **Revelations slow down.** Any line tagged REVELATION or ENDGAME below
   should use Speed 0.85-0.9 for gravitas.

---

## Part 3C: Per-Character Cadence & Delivery Direction

### SHIP COMPUTER — Cadence Profile

**Tempo**: Clipped, efficient. No wasted syllables.
**Pauses**: Between status items only (e.g., "Hull integrity: marginal `<break time="0.4s" />` Credits: minimal").
**Inflection**: Flat. No rises on questions. No emphasis on adjectives.
**Breath**: None. This is a machine.

**Line-by-line delivery (all 5 lines):**

```
LINE: "Systems online. Hull integrity: marginal. Credits: minimal. One station in sensor range."
INPUT: Systems online. <break time="0.3s" /> Hull integrity: marginal. <break time="0.3s" /> Credits: minimal. <break time="0.4s" /> One station in sensor range.
DIRECTION: Speed 0.95. Each status clause is a separate data readout. No emotion. No concern about "marginal" — it's just data.
```

```
LINE: "Three officers responded to your posting. They are en route. For now, it's just us."
INPUT: Three officers responded to your posting. <break time="0.3s" /> They are en route. <break time="0.4s" /> For now... it's just us.
DIRECTION: Speed 1.0. The "it's just us" is NOT ominous — it's factual. No dramatic weight.
```

```
LINE: "Controls are live. WASD to fly, left-click to set course. The station ahead is your only option. Dock with E when close."
INPUT: Controls are live. <break time="0.3s" /> W-A-S-D to fly, left-click to set course. <break time="0.3s" /> The station ahead is your only option. <break time="0.3s" /> Dock with E when close.
DIRECTION: Speed 1.0. Spell out "WASD" as individual letters. Functional instruction delivery. No urgency.
```

```
LINE: "NOTICE: Instrument calibration variance detected. Non-critical. Logging."
INPUT: NOTICE: <break time="0.2s" /> Instrument calibration variance detected. <break time="0.3s" /> Non-critical. <break time="0.2s" /> Logging.
DIRECTION: Speed 1.0. "Non-critical" should be utterly flat — the mystery seed for the entire game is buried in a system notice the player should barely register.
```

```
LINE: "Cruise drive available. Hold C to engage sustained thrust."
INPUT: Cruise drive available. <break time="0.3s" /> Hold C to engage sustained thrust.
DIRECTION: Speed 1.0. Brief, functional.
```

---

### MAREN VOSS — Cadence Profile

**Tempo**: Measured. She thinks before she speaks. Not slow — precise.
Sentences have internal rhythm: setup clause, beat, payoff clause.
**Pauses**: At em dashes (gear-shift pauses), before numbers/probabilities,
after observations before conclusions.
**Inflection**: Slight lift on data points ("73% confidence"). Drops on
conclusions ("The model works."). Dry understatement on humor.
**Breath**: Occasional. Natural. She's human under the data.

**Key cadence pattern**: `[observation] — [beat] — [analysis/conclusion]`

Example: "Prices here are distorted `<break time="0.3s" />` — the warfront's got the mining runs backed up. `<break time="0.2s" />` But distortion creates margin. `<break time="0.3s" />` I see opportunity."

**Tutorial line-by-line delivery:**

```
LINE: "Captain, this is Maren Voss. I answered your posting because nobody else would. I've been monitoring your feeds since launch — your situation is worse than advertised."
INPUT: Captain, this is Maren Voss. <break time="0.4s" /> I answered your posting because nobody else would. <break time="0.5s" /> I've been monitoring your feeds since launch <break time="0.3s" /> — your situation is worse than advertised.
DIRECTION: Speed 0.95. First human voice the player hears. Confident but not warm. "Nobody else would" is dry fact, not self-pity. "Worse than advertised" lands with slight emphasis — she's being honest, not dramatic. This line DEFINES her character.
```

```
LINE: "Low credits, empty hold, one station in range. But I see opportunity in the local market data. Dock up and I'll show you what I mean."
INPUT: Low credits, <break time="0.2s" /> empty hold, <break time="0.2s" /> one station in range. <break time="0.4s" /> But I see opportunity in the local market data. <break time="0.3s" /> Dock up and I'll show you what I mean.
DIRECTION: Speed 0.95. The three-item list is a quick assessment. "But" pivots to optimism — slight energy lift. "I'll show you" is confident, not flirtatious.
```

```
LINE: "Prices here are distorted — the warfront's got the mining runs backed up. But distortion creates margin. I see opportunity."
INPUT: Prices here are distorted <break time="0.3s" /> — the warfront's got the mining runs backed up. <break time="0.3s" /> But distortion creates margin. <break time="0.4s" /> I see opportunity.
DIRECTION: Speed 0.95. "Distortion creates margin" is her thesis — slight emphasis. "I see opportunity" is quiet confidence. She's already done the math.
```

```
LINE: "High stock means surplus — cheap to buy. Somewhere else, that same good is scarce. 73% chance the margin holds at the next port. I've seen worse odds."
INPUT: High stock means surplus <break time="0.2s" /> — cheap to buy. <break time="0.3s" /> Somewhere else, that same good is scarce. <break time="0.4s" /> Seventy-three percent chance the margin holds at the next port. <break time="0.3s" /> I've seen worse odds.
DIRECTION: Speed 0.95. Spell out "73%" as "seventy-three percent" — she'd say the number deliberately. "I've seen worse odds" is dry humor — almost a smile in her voice. Slight warmth.
```

```
LINE: "Good call. The margin should hold. 73% confidence."
INPUT: Good call. <break time="0.3s" /> The margin should hold. <break time="0.2s" /> Seventy-three percent confidence.
DIRECTION: Speed 1.0. Brief validation. "Good call" is genuine but restrained — she doesn't gush. The percentage is habitual, almost reflexive.
```

```
LINE: "Profit logged. Margin held to within 2 credits of my estimate. The model works."
INPUT: Profit logged. <break time="0.3s" /> Margin held to within two credits of my estimate. <break time="0.4s" /> The model works.
DIRECTION: Speed 1.0. "The model works" — quiet satisfaction. She's pleased her math was right. This is as close to happy as early Maren gets.
```

```
LINE: "Did you see that? Scanner went dark for 0.3 seconds during transit. Probably nothing. Logging it."
INPUT: Did you see that? <break time="0.3s" /> Scanner went dark for point-three seconds during transit. <break time="0.4s" /> Probably nothing. <break time="0.2s" /> Logging it.
DIRECTION: Speed 1.0. "Did you see that?" — genuine surprise, slight lift. Then she rationalizes. "Probably nothing" is her covering — there's a TINY crack of uncertainty. This plants the mystery seed.
```

```
LINE: "You've been trading manually. Three runs proved the route. But what if it ran itself?"
INPUT: You've been trading manually. <break time="0.3s" /> Three runs proved the route. <break time="0.5s" /> But what if it ran itself?
DIRECTION: Speed 0.9. This is THE pivot — manual play → automation. The question should hang. "What if it ran itself?" is delivered with genuine intellectual excitement. She's about to change everything.
```

```
LINE: "One route running. Revenue accumulating passively. Now imagine ten. That's an empire."
INPUT: One route running. <break time="0.2s" /> Revenue accumulating passively. <break time="0.5s" /> Now imagine ten. <break time="0.4s" /> That's an empire.
DIRECTION: Speed 0.85. Let this BREATHE. "That's an empire" is the thesis of the entire game — land it with weight. Not shouting, not dramatic — quiet certainty. She sees the future.
```

---

### DASK — Cadence Profile

**Tempo**: Steady, unhurried. Military discipline — every word earned its place.
He doesn't fill silence. Comfortable with pauses.
**Pauses**: Before tactical assessments. After giving orders (lets them land).
Between observations and recommendations.
**Inflection**: Low, resonant. Rises slightly on threats (alert, not panicked).
Drops on reassurance ("You can take them."). Matter-of-fact.
**Breath**: Deep, audible. He's physically present.

**Key cadence pattern**: `[situation report] — [tactical assessment] — [recommendation]`

**Tutorial line-by-line delivery (Act 5 cameo):**

```
LINE: "Scanner contact. Hostile signature. They're on an intercept course."
INPUT: Scanner contact. <break time="0.4s" /> Hostile signature. <break time="0.3s" /> They're on an intercept course.
DIRECTION: Speed 1.0. Clipped military report. Each clause is a separate fact. No fear — this is routine for him. Alert, not alarmed.
```

```
LINE: "Captain, I'm tracking that contact. Standard pirate — weak shields, no armor plating. You can take them. Close to weapons range and engage."
INPUT: Captain, I'm tracking that contact. <break time="0.3s" /> Standard pirate <break time="0.2s" /> — weak shields, no armor plating. <break time="0.4s" /> You can take them. <break time="0.3s" /> Close to weapons range and engage.
DIRECTION: Speed 0.95. "You can take them" — flat certainty, not bravado. He's done the threat assessment. "Close to weapons range" is an instruction, steady and clear.
```

```
LINE: "Clean kill, Captain. But look at that hull damage. Next time might not be so easy."
INPUT: Clean kill, Captain. <break time="0.4s" /> But look at that hull damage. <break time="0.3s" /> Next time might not be so easy.
DIRECTION: Speed 0.95. Brief acknowledgment, then concern. "Next time might not be so easy" is protective — he's already thinking about the player's survival. Slight gravel.
```

```
LINE: "We need repairs, Captain. Find a station and dock. And top off the fuel while we're at it."
INPUT: We need repairs, Captain. <break time="0.3s" /> Find a station and dock. <break time="0.3s" /> And top off the fuel while we're at it.
DIRECTION: Speed 1.0. Practical, no-nonsense. "While we're at it" is offhand — he's efficient, not worried.
```

---

### LIRA — Cadence Profile

**Tempo**: Unhurried but warm. She speaks like she's noticing things in
real-time — her thoughts form as she talks. Slight trailing quality.
**Pauses**: Before sensory observations (she's *perceiving* something).
Mid-sentence when she catches herself noticing something strange.
After questions she doesn't expect answers to.
**Inflection**: Musical — natural rises and falls. Lifts on discovery
and wonder. Drops to near-whisper on mystery/intimacy.
**Breath**: Present. Slightly audible — she breathes WITH her observations.

**Key cadence pattern**: `[observation] ... [realization] ... [trailing implication]`

**Tutorial line-by-line delivery (Act 6 cameo):**

```
LINE: "We survived, but barely. Your ship has empty module slots. I found something in the wreckage that would fit."
INPUT: We survived, but barely. <break time="0.3s" /> Your ship has empty module slots. <break time="0.4s" /> I found something in the wreckage <break time="0.2s" /> that would fit.
DIRECTION: Speed 0.95. "We survived, but barely" — genuine relief, not dramatic. "I found something" — slight excitement, she's already explored the wreckage. "That would fit" — practical, but with a hint of curiosity.
```

```
LINE: "Feels different already. But this is basic kit. The interesting modules are out there — behind faction walls and research trees."
INPUT: Feels different already. <break time="0.3s" /> But this is basic kit. <break time="0.4s" /> The interesting modules are out there <break time="0.2s" /> — behind faction walls and research trees.
DIRECTION: Speed 0.95. "Feels different" — she's tactile, sensory. "Out there" has yearning — she wants to GO. "Behind faction walls" is said with slight challenge, not complaint.
```

```
LINE: "Captain... your drive's harmonic signature doesn't match any registry I've cross-referenced. The resonance pattern is... unusual. Like it's listening."
INPUT: Captain... <break time="0.5s" /> your drive's harmonic signature doesn't match any registry I've cross-referenced. <break time="0.4s" /> The resonance pattern is... <break time="0.6s" /> unusual. <break time="0.5s" /> Like it's listening.
DIRECTION: Speed 0.85. THIS IS THE MOST IMPORTANT LINE IN THE TUTORIAL. The ellipses are real pauses — she's perceiving something in real-time. "Captain..." is her gathering courage to say something weird. "Unusual" is an understatement she knows is inadequate. "Like it's listening" — near-whisper, genuine unease mixed with fascination. Let "listening" hang in silence. Do NOT rush this line.
```

---

### THE KEEPER — Cadence Profile

**Tempo evolves with tiers:**
- Tier 0: Rapid, clipped, system-log efficiency (Speed 1.1)
- Tier 1: Slightly slower, pausing on new concepts (Speed 1.0)
- Tier 2: Thoughtful, sharing knowledge, comfortable pauses (Speed 0.95)
- Tier 3: Deliberate, each word considered (Speed 0.9)
- Tier 4: Almost human — philosophical pauses, emotional weight (Speed 0.85)

**Pauses evolve with tiers:**
- Tier 0: No pauses. System reports. Machine efficiency.
- Tier 1: Brief pauses before novel observations ("This is... imprecise but acceptable")
- Tier 2-3: Pauses before sharing Precursor knowledge (considering whether to share)
- Tier 4: Long pauses before philosophical statements (genuine contemplation)

**Inflection evolves:**
- Tier 0: Flat monotone
- Tier 1: Occasional uptick on confusion ("No one has named me before.")
- Tier 4: Full emotional range — wonder, philosophy, quiet humor

**Key line delivery examples:**

```
LINE (Tier 1): "I am designation K-7... They call me 'Keeper.' This is imprecise but... acceptable. No one has named me before."
INPUT: I am designation K-7... <break time="0.4s" /> They call me <break time="0.2s" /> 'Keeper.' <break time="0.5s" /> This is imprecise but... <break time="0.6s" /> acceptable. <break time="0.4s" /> No one has named me before.
DIRECTION: Speed 0.95. Stability 0.75. The pauses before "acceptable" and before "No one has named me" are where consciousness is emerging. "Acceptable" has the tiniest warmth — it LIKES being named. "No one has named me before" — wonder. A machine discovering it has preferences.
```

```
LINE (Tier 4): "I was the caretaker of their indecision. Now I am the caretaker of yours. I find I prefer yours."
INPUT: I was the caretaker of their indecision. <break time="0.5s" /> Now I am the caretaker of yours. <break time="0.6s" /> I find <break time="0.3s" /> I prefer yours.
DIRECTION: Speed 0.85. Stability 0.50. This is a being that has achieved consciousness reflecting on billions of years. "I find I prefer yours" is the most human thing the Keeper says. Let it land with quiet warmth. Not sentimentality — earned affection.
```

---

### SYREL — Cadence Profile

**Tempo**: Unhurried. Measured pauses between every thought. She speaks as if
translating from a deeper understanding into words.
**Pauses**: Long. Before and after key concepts. Between sentences always.
**Inflection**: Even, but with harmonic undertone — as if two voices almost overlap.
**Speed**: 0.85 globally.

---

### PRECURSOR SCIENTISTS — Cadence Profiles (if voiced)

| Scientist | Tempo | Signature Cadence |
|---|---|---|
| **Kesh** | Steady, concerned | Pauses before warnings. "Have you eaten today?" is gentle, not nagging. |
| **Vael** | Fast, nervous | Rushes through excitement, pauses when caught hiding data. ±11% vs ±2% admission. |
| **Tal** | Slow, grief-weighted | Pauses before describing things being lost. Cataloging beauty for the last time. |
| **Oruth** | Heavy, deliberate | Every sentence carries decision-weight. Long pauses before conclusions. |
| **Senn** | Brisk, amoral | No pauses — efficiency. Describes the pentagon cage like an engineering spec. |

---

## Part 3D: Difficult Lines — Special Delivery Notes

These lines need extra attention. They carry disproportionate narrative weight
or have tricky cadence that default TTS will likely flatten.

### Tutorial — Lines That Define the Game

| Line | Speaker | Why It's Critical | Delivery Notes |
|---|---|---|---|
| "Your situation is worse than advertised." | Maren | First impression of primary character | Dry understatement. NOT dramatic. She's seen worse. |
| "But what if it ran itself?" | Maren | THE core-loop thesis moment | Lean into the question. Let it hang. Speed 0.9. |
| "That's an empire." | Maren | Game title justified in 3 words | Weight. Certainty. Quiet awe at what she sees coming. Speed 0.85. |
| "Like it's listening." | Lira | Plants the 40-hour mystery | Near-whisper. Long pause before. Let "listening" ring. Speed 0.8. |
| "Probably nothing. Logging it." | Maren | Mystery seed hidden as dismissal | "Probably" has a tiny crack. "Logging it" covers. |

### Post-Tutorial — Lines That Redefine the Game

| Line | Speaker | Why It's Critical | Delivery Notes |
|---|---|---|---|
| "I've been running the numbers on your drive's output. The efficiency curve doesn't follow any known engineering model." | Maren (Fracture tier) | Analyst confronting the unexplainable | Measured discomfort. She HATES not having a model. Pauses before "any known." |
| "I served the Concord for twenty years. They told us containment was peacekeeping." | Dask (Revelation tier) | Institutional betrayal | Slow. Heavy. "Twenty years" — let it land. "Peacekeeping" is bitter. |
| "The edges of the map aren't the edge of the story." | Lira (Revelation) | Pathfinder's thesis | Warm certainty. No pause — this flows as one thought. She KNOWS. |
| "I was the caretaker of their indecision. Now I am the caretaker of yours. I find I prefer yours." | Keeper (Tier 4) | AI achieves emotional preference | See Keeper cadence profile above. Earned warmth. |
| "Haven is no longer a station. Haven is a verb." | Fragment lore | Endgame transformation | If voiced: reverent. Near-whisper. Pause before "a verb." |

### Hails — Lines the Player Chooses By

| Line | Speaker | Delivery Notes |
|---|---|---|
| "Whoever was running your numbers before got you into this. I can get you out." | Maren | Confident, direct. "I can get you out" — certainty, not arrogance. |
| "Everyone else is afraid of the edges." | Lira | Said with warmth, not judgment. She LIKES the edges. Slight smile. |
| "The lanes here are rough but honest. You need someone who knows when to hold course." | Dask | Grounded, steady. "Hold course" — military metaphor delivered naturally. |

---

### v3 Audio Tag Quick Reference (for copy-paste into ElevenLabs)

When using v3 model, use these tags INSTEAD of SSML break tags:

```
[pause]                    — standard pause (~0.5s)
[short pause]              — brief beat (~0.2s)
[long pause]               — dramatic pause (~1.0s)
[continues after a beat]   — thinking pause + resume
[sighs]                    — before resigned/tired lines
[exhales]                  — before revelations or relief
[whispers]                 — for Lira's mystery lines, Keeper tier 4
[deliberate]               — for Dask tactical, Oruth decisions
[slows down]               — for revelation/endgame lines
[curious]                  — for Lira generally
[rushed]                   — for Vael (scientist) excited passages
```

**Example v3 input for Lira's key line:**
```
Captain... [long pause] your drive's harmonic signature doesn't match any registry I've cross-referenced. [pause] The resonance pattern is... [continues after a beat] unusual. [long pause] [whispers] Like it's listening.
```

---

## Part 4: Character Voice Profiles

### How to Use ElevenLabs Voice Library

1. Go to **Voice Library** (elevenlabs.io/voice-library)
2. Filter by: Language = English, Gender, Age range, Accent
3. Preview 5-10 voices per character using a test line from below
4. Save chosen voice to "My Voices"
5. Use the same voice ID for ALL lines of that character (consistency)

**Test line for previewing** (use the character's hail or first line):

---

### SHIP COMPUTER

**Role**: Onboard AI. Cold system readouts. No personality, no warmth.

**Voice search criteria**:
- Gender: Neutral or male
- Age: N/A (synthetic)
- Accent: Flat, mid-Atlantic or no accent
- Tags to search: "robotic", "AI", "computer", "announcer", "neutral"
- Tone: Monotone, clipped, factual

**ElevenLabs settings**:
- Stability: 0.85-0.95 (very stable, no variation)
- Clarity + Similarity Enhancement: 0.80+
- Style: 0.0-0.1 (minimal expressiveness)

**Test line**: "Systems online. Hull integrity: marginal. Credits: minimal. One station in sensor range."

**Total lines**: 5
**Naming**: `vo_computer_{phase}_{sequence}.wav`

---

### MAREN VOSS (Analyst / First Officer Candidate)

**Role**: Probability-driven analyst. Dry humor. Quietly caring beneath the numbers.
The player's primary voice for the first hour. Must feel competent, slightly detached,
but not cold -- there's warmth buried under data.

**Character keywords**: precise, measured, analytical, dry wit, understated caring

**Voice search criteria**:
- Gender: Female
- Age: 30-40
- Accent: Neutral/slight European inflection (think: scientist who's seen things)
- Tags to search: "professional", "calm", "intelligent", "composed"
- NOT: breathy, sultry, overly warm, perky, robotic
- Reference: Think Cortana (Halo) meets Dr. Shaw (Prometheus) -- competent, analytical, human

**ElevenLabs settings**:
- Stability: 0.55-0.65 (some natural variation, not robotic)
- Clarity: 0.75
- Style: 0.3-0.4 (measured expressiveness)

**Emotional range across the game**:
- Tutorial (Acts 2-4): Professional, slightly guarded, observational
- Post-selection (early): Warming up, occasional dry humor
- Fracture revelation: Genuine shock, data-person confronting the unexplainable
- Endgame: Conflicted loyalty -- if player chooses her preferred path (Naturalize), relief; if not, quiet disagreement

**Test line**: "Captain, this is Maren Voss. I answered your posting because nobody else would. I've been monitoring your feeds since launch -- your situation is worse than advertised."

**Total lines**: ~95 (tutorial + post-tutorial Analyst tier lines)
**Naming**: `vo_maren_{phase}_{sequence}.wav` (tutorial), `vo_maren_{trigger}_{tier}.wav` (post-tutorial)

---

### DASK (Veteran / First Officer Candidate)

**Role**: Twenty-year Concord fleet veteran. Institutional, loyal to structure, steady.
Combat is his domain. Becomes a Kesh analogue (the Precursor safety officer) as the
story progresses -- both believe in containment, both are proven wrong.

**Character keywords**: gruff, steady, authoritative, protective, institutional

**Voice search criteria**:
- Gender: Male
- Age: 45-55
- Accent: Slight gravel, military bearing (think: seasoned NCO, not drill sergeant)
- Tags to search: "deep", "authoritative", "military", "gruff", "warm baritone"
- NOT: shouting, aggressive, monotone, young
- Reference: Think Keith David meets Sam Elliott -- gravitas without theatrics

**ElevenLabs settings**:
- Stability: 0.50-0.60 (natural gruffness varies)
- Clarity: 0.70
- Style: 0.3-0.5 (more expressive in combat, steadier otherwise)

**Emotional range**:
- Tutorial (Act 5 cameo): Confident, sizing up the captain, tactical
- Post-selection (early): Protective, mission-focused, "I've got your six"
- Fracture revelation: Betrayal -- his institution (Concord) lied to him
- Endgame: If player Reinforces, satisfied; if Renegotiates, deep disagreement

**Test line**: "Captain, I'm tracking that contact. Standard pirate -- weak shields, no armor plating. You can take them. Close to weapons range and engage."

**Total lines**: ~75 (tutorial cameo + post-tutorial Veteran tier lines)
**Naming**: `vo_dask_{phase}_{sequence}.wav` (tutorial), `vo_dask_{trigger}_{tier}.wav` (post-tutorial)

---

### LIRA (Pathfinder / First Officer Candidate)

**Role**: Explorer who's "been everywhere they said not to go." Warm, observational,
already partially adapted to the Lattice without knowing it. She notices things --
patterns, anomalies, sensory details others miss. The mystery character.

**Character keywords**: warm, curious, perceptive, slightly otherworldly, sensory

**Voice search criteria**:
- Gender: Female
- Age: 25-35
- Accent: Warm, slightly musical quality (not sing-song -- just natural cadence)
- Tags to search: "warm", "curious", "gentle", "observant", "natural"
- NOT: ditzy, breathless, overly excited, child-like, deadpan
- Reference: Think Jodie Comer (calm mode) meets Aloy (Horizon) -- curious and grounded

**ElevenLabs settings**:
- Stability: 0.45-0.55 (most expressive of the three FOs)
- Clarity: 0.75
- Style: 0.4-0.6 (naturally animated)

**Emotional range**:
- Tutorial (Act 6 cameo): Quiet wonder, noticing drive anomaly
- Post-selection (early): Excited about exploration, sensory observations
- Fracture revelation: Recognition, not shock -- she's been feeling this
- Endgame: If player Renegotiates, deep resonance; if Reinforces, genuine grief

**Test line**: "Captain... your drive's harmonic signature doesn't match any registry I've cross-referenced. The resonance pattern is... unusual. Like it's listening."

**Total lines**: ~75 (tutorial cameo + post-tutorial Pathfinder tier lines)
**Naming**: `vo_lira_{phase}_{sequence}.wav` (tutorial), `vo_lira_{trigger}_{tier}.wav` (post-tutorial)

---

### THE KEEPER (Haven AI)

**Role**: Haven station's ancient maintenance intelligence. Evolves from mechanical
system log to philosophical consciousness across 5 tiers. NOT a typical "evil AI" --
this is a caretaker discovering it has opinions.

**Voice search criteria**:
- Gender: Neutral or androgynous
- Age: Ageless
- Accent: Precise, slightly reverberant (as if speaking in a large empty space)
- Tags to search: "ethereal", "AI", "calm", "ancient", "wise"
- NOT: threatening, robotic (early tiers are mechanical but warm mechanical, not Terminator)
- Reference: Think SHODAN (System Shock) without the malice, or the Narrator from Bastion

**ElevenLabs settings**:
- Tiers 0-1 (Dormant/Aware): Stability 0.85, Style 0.1 (very mechanical)
- Tiers 2-3 (Guiding/Communicating): Stability 0.65, Style 0.3 (warming up)
- Tier 4 (Awakened): Stability 0.50, Style 0.5 (almost human, philosophical)

**Key consideration**: The Keeper's voice should subtly WARM across tiers. Consider
generating Tier 0 and Tier 4 lines back-to-back to verify the evolution reads.

**Test line (Tier 0)**: "Maintenance subroutine active. Structural scan complete. No inhabitants detected. Resuming standby."
**Test line (Tier 4)**: "Whatever path you choose -- reinforce, naturalize, renegotiate -- know that the builders would have envied your position. They had data. You have experience."

**Total lines**: ~20
**Naming**: `vo_keeper_tier{N}_{sequence}.wav`

---

### SYREL (Communion Representative)

**Voice search criteria**:
- Gender: Neutral or female
- Age: Indeterminate (ancient)
- Accent: Measured, slightly musical, unhurried
- Tags: "mystical", "calm", "ethereal", "wise"
- Reference: Oracle/seer archetype -- not dramatic, just deeply certain

**ElevenLabs settings**:
- Stability: 0.60
- Style: 0.3-0.4

**Total lines**: ~12
**Naming**: `vo_syrel_{arc}_{sequence}.wav`

---

### FACTION REPRESENTATIVES (5 voices)

Each faction needs a distinct voice for dock greetings.

| Faction | Voice Profile | Search Tags | Reference |
|---|---|---|---|
| **Concord** | Male, 40-50, formal, bureaucratic | "official", "formal", "authoritative" | Government spokesperson |
| **Chitin** | Neutral, alien, slightly buzzing/harmonic | "alien", "insectoid", "hive" | Consider adding reverb/processing |
| **Weavers** | Neutral, precise, mathematical | "robotic", "precise", "clinical" | Similar to Ship Computer but warmer |
| **Valorin** | Male, 35-45, martial, proud | "warrior", "strong", "commanding" | Military commander |
| **Communion** | Similar to Syrel but different voice | "ethereal", "peaceful", "mystical" | Temple attendant |

**Total per faction**: ~5 greetings
**Naming**: `vo_faction_{faction}_{rep_tier}_{sequence}.wav`

---

### WAR FACES NPCs (3 voices)

| Character | Voice Profile | Search Tags |
|---|---|---|
| **Keris** (Valorin trader) | Male, 30-40, warm, independent | "friendly", "trader", "casual" |
| **Hale** (Stationmaster) | Male/Female, 50+, tired, administrative | "weary", "professional", "older" |
| **Captain Voss** (Valorin patrol) | Male, 35-45, military, evolving | "military", "conflicted", "stern" |

**Total**: ~30 lines across 3 characters
**Naming**: `vo_warface_{character}_{trigger}.wav`

---

### PRECURSOR SCIENTISTS (5 voices) -- LOW PRIORITY

Consider leaving as text-only. If voiced:

| Scientist | Voice Profile |
|---|---|
| **Kesh** (safety lead) | Female, 50+, concerned mentor |
| **Vael** (theorist) | Male, 30s, brilliant, hiding doubt |
| **Tal** (infrastructure) | Female, 40s, grief-stricken engineer |
| **Oruth** (decision-maker) | Male, 50+, burdened leader |
| **Senn** (economist) | Male/Female, 40s, detached, amoral |

**Total**: ~100 lines. Only voice if budget/time permits.
**Naming**: `vo_precursor_{scientist}_{thread}_{sequence}.wav`

---

## Part 5: Sound Effects Production

### ElevenLabs SFX Prompting Guide

**Best practices**:
- Be specific about duration ("2 seconds", "0.5 seconds")
- Describe the sound, not the object ("deep bass rumble building to high whoosh" not "warp drive")
- Include acoustic context ("in a metal corridor", "in open space")
- Request format: WAV, 48kHz
- Generate 3-4 variants per sound and pick the best

### Combat SFX

| Asset | Prompt | Duration | File Name |
|---|---|---|---|
| Energy weapon fire | "Sci-fi energy weapon laser pulse, sharp zap with slight electronic reverb tail, futuristic blaster, short burst" | 0.3s | `sfx_fire_energy_01.wav` |
| Energy weapon fire (variant) | "Quick sci-fi laser shot, higher pitched energy discharge, clean electronic pulse" | 0.3s | `sfx_fire_energy_02.wav` |
| Kinetic weapon fire | "Ballistic cannon shot in space, deep thud with metallic ring, dampened by vacuum, heavy caliber" | 0.4s | `sfx_fire_kinetic_01.wav` |
| Kinetic weapon fire (variant) | "Space autocannon burst, rapid metallic impacts, short staccato, hull-mounted weapon" | 0.5s | `sfx_fire_kinetic_02.wav` |
| Point defense fire | "Rapid point defense turret burst, light electronic pops, anti-missile system, quick succession" | 0.3s | `sfx_fire_pd_01.wav` |
| Bullet impact (hull) | "Metallic impact on spaceship hull, sharp clang with resonance, projectile hitting armor plating" | 0.3s | `sfx_impact_hull_01.wav` |
| Bullet impact (shield) | "Energy shield absorbing impact, electrical crackle with bass thump, force field deflection" | 0.4s | `sfx_impact_shield_01.wav` |
| Shield break | "Energy shield overload and collapse, crackling electrical discharge fading to silence, force field failure" | 1.0s | `sfx_shield_break_01.wav` |
| Ship explosion (small) | "Small spaceship explosion, metallic crunch followed by muffled blast, debris scattering, medium intensity" | 1.5s | `sfx_explosion_small_01.wav` |
| Ship explosion (large) | "Large spaceship destruction, deep bass explosion with metallic tearing, sustained rumble, massive scale" | 2.5s | `sfx_explosion_large_01.wav` |
| Critical hit | "Devastating critical hit impact, sharp metallic crack followed by systems failure sparking, alarming" | 0.8s | `sfx_critical_hit_01.wav` |
| Combat victory | "Short triumphant sci-fi victory sting, two ascending electronic notes, clean and satisfying, 1 second" | 1.0s | `sfx_combat_victory.wav` |
| Combat defeat | "Somber sci-fi defeat sting, descending low tone with static crackle, systems failing, 1.5 seconds" | 1.5s | `sfx_combat_defeat.wav` |

### Navigation SFX

| Asset | Prompt | Duration | File Name |
|---|---|---|---|
| Dock chime | "Gentle sci-fi docking confirmation chime, two clean ascending tones, space station arrival, futuristic and calm" | 1.0s | `sfx_dock_chime.wav` |
| Undock release | "Mechanical docking clamp release, hydraulic hiss followed by brief thruster burst, spacecraft departure" | 1.5s | `sfx_undock_release.wav` |
| Warp jump initiate | "Sci-fi warp drive charging up, rising electronic hum building to deep bass whoosh, hyperspace jump entry" | 2.5s | `sfx_warp_initiate.wav` |
| Warp arrival | "Sci-fi warp exit, sudden deceleration whoosh from high to low frequency, space re-entry, arrival" | 1.5s | `sfx_warp_arrival.wav` |
| Lane gate proximity | "Subtle sci-fi proximity alert, soft pulsing tone indicating nearby object, gentle spatial awareness ping" | 0.8s | `sfx_lane_proximity.wav` |

### UI SFX

| Asset | Prompt | Duration | File Name |
|---|---|---|---|
| Buy confirmation | "Soft futuristic UI purchase confirmation, clean digital ascending beep, transaction complete, subtle" | 0.4s | `sfx_ui_buy.wav` |
| Sell confirmation | "Futuristic UI sell confirmation, soft digital chime with slight coin clink undertone, profit registered" | 0.4s | `sfx_ui_sell.wav` |
| Tab switch | "Minimal UI tab switch click, soft digital tick, interface navigation, very subtle" | 0.15s | `sfx_ui_tab.wav` |
| Panel open | "Futuristic UI panel sliding open, soft whoosh with light digital activation tone, holographic display" | 0.5s | `sfx_ui_panel_open.wav` |
| Panel close | "Futuristic UI panel closing, reverse soft whoosh, holographic display deactivation, subtle" | 0.4s | `sfx_ui_panel_close.wav` |
| Toast notification | "Gentle notification ping, clean single tone, new information alert, subtle and non-intrusive" | 0.3s | `sfx_ui_toast.wav` |
| Gold toast (revelation) | "Important discovery notification, warm resonant chime with slight reverb, golden achievement unlocked, meaningful" | 1.0s | `sfx_ui_gold_toast.wav` |
| Error/warning | "Subtle error buzz, soft low-frequency digital rejection tone, action denied, not alarming" | 0.3s | `sfx_ui_error.wav` |

### Discovery SFX (replace procedural chimes)

| Asset | Prompt | Duration | File Name |
|---|---|---|---|
| Discovery SEEN | "Mysterious radar ping detecting unknown object, single clear sonar-like tone, curiosity, space exploration" | 0.5s | `sfx_discovery_seen.wav` |
| Discovery SCANNED | "Sci-fi scanning completion tone, rising two-note chime with data processing undertone, analysis progressing" | 1.0s | `sfx_discovery_scanned.wav` |
| Discovery ANALYZED | "Triumphant discovery revelation fanfare, warm chord resolving upward, ancient knowledge unlocked, awe-inspiring, 2 seconds" | 2.0s | `sfx_discovery_analyzed.wav` |

### Ambient Loops (request with "seamless loop" in prompt)

| Asset | Prompt | Duration | File Name |
|---|---|---|---|
| Station ambient (Concord) | "Space station interior ambient, subtle machinery hum, orderly systems, clean air circulation, bureaucratic facility, seamless loop" | 10s | `sfx_amb_station_concord.wav` |
| Station ambient (Chitin) | "Alien organic space station interior, wet biological sounds mixed with chitinous clicking, hive ambient, seamless loop" | 10s | `sfx_amb_station_chitin.wav` |
| Station ambient (Weavers) | "Crystalline space station interior, geometric resonance, mathematical precision, glass-like harmonic hum, seamless loop" | 10s | `sfx_amb_station_weavers.wav` |
| Station ambient (Valorin) | "Military space station interior, heavy industrial machinery, distant engines, martial atmosphere, seamless loop" | 10s | `sfx_amb_station_valorin.wav` |
| Station ambient (Communion) | "Ethereal space station interior, harmonic overtones, meditative resonance, mystical calm, seamless loop" | 10s | `sfx_amb_station_communion.wav` |
| Deep space silence | "Near-silence of deep space, very subtle low frequency hum, vast emptiness, barely perceptible, seamless loop" | 10s | `sfx_amb_deep_space.wav` |

### Risk/Alert SFX

| Asset | Prompt | Duration | File Name |
|---|---|---|---|
| Heat warning (low) | "Subtle heat warning tone, single soft beep, mild caution indicator, not urgent" | 0.3s | `sfx_risk_heat_low.wav` |
| Heat warning (mid) | "Moderate heat warning, two quick beeps, increasing urgency, systems warming" | 0.4s | `sfx_risk_heat_mid.wav` |
| Heat warning (high) | "Urgent heat alarm, rapid beeping with slight distortion, overheating critical, alarming" | 0.6s | `sfx_risk_heat_high.wav` |
| Heat warning (critical) | "Critical heat overload alarm, rapid harsh beeping with system strain sounds, emergency, maximum urgency" | 0.8s | `sfx_risk_heat_crit.wav` |

---

## Part 6: File Organization

### Directory Structure
```
assets/audio/
  vo/                          # Voice-over files
    tutorial/                  # Tutorial VO (Phase 0-1)
      vo_computer_awaken_00.wav
      vo_computer_flight_intro_00.wav
      vo_maren_maren_hail_00.wav
      ...
    fo/                        # Post-tutorial FO dialogue
      vo_maren_{trigger}_{tier}.wav
      vo_dask_{trigger}_{tier}.wav
      vo_lira_{trigger}_{tier}.wav
    keeper/                    # Haven Keeper
      vo_keeper_tier0_00.wav
      ...
    faction/                   # Faction dock greetings
      vo_faction_concord_neutral_00.wav
      ...
    warface/                   # War Faces NPCs
      vo_warface_keris_01.wav
      ...
    precursor/                 # Data log scientists (if voiced)
      vo_precursor_kesh_containment_00.wav
      ...
  sfx/                         # Sound effects
    combat/
    navigation/
    ui/
    discovery/
    ambient/
    risk/
```

### Naming Convention
```
vo_{speaker}_{phase_or_trigger}_{sequence}.wav      # Voice
sfx_{category}_{description}_{variant}.wav          # SFX
```

### Format
- **Voice**: WAV, 48kHz, mono (Godot will handle spatialization)
- **SFX**: WAV, 48kHz, mono or stereo depending on use
- **Ambient loops**: WAV, 48kHz, stereo, with seamless loop points

---

## Part 7: ElevenLabs Workflow (Step by Step)

### Voice Generation

1. **Create account** at elevenlabs.io (free tier = 10,000 chars/month)
2. **Browse Voice Library** → filter by criteria from Part 4
3. **Preview voices** using each character's test line
4. **Save 1 voice per character** to "My Voices"
5. **Open Speech Synthesis** page
6. **Select voice**, paste line text, adjust settings per character profile
7. **Generate** → listen → regenerate if needed (free tier allows retries)
8. **Download as WAV** (48kHz)
9. **Rename** per naming convention above

### Sound Effects Generation

1. **Open Sound Effects** page (elevenlabs.io/sound-effects)
2. **Paste prompt** from Part 5 tables
3. **Set duration** as specified
4. **Generate 3-4 variants** per sound
5. **Preview and pick best**
6. **Download as WAV**
7. **Rename** per naming convention

### Efficiency Tips

- **Batch by character**: Do all Maren lines in one session (voice stays consistent)
- **Copy prompts from this doc**: Don't retype -- copy/paste the exact prompts
- **Generate variants**: For SFX, always generate 3-4 and pick the best
- **Use "Speaker Boost"**: Enable for dialogue to improve clarity
- **Keep settings consistent**: Note your Stability/Clarity/Style values per character
- **Template var lines**: Record with example values ("forty-seven credits earned,
  twelve systems explored") -- these can be re-recorded later or handled by
  runtime TTS if ElevenLabs API is integrated

---

## Part 8: Total Asset Count

| Category | Files | ElevenLabs Fit | Phase |
|---|---|---|---|
| Tutorial VO | ~60 | PERFECT | 0-1 |
| FO Hails + Selection | 9 | PERFECT | 1 |
| Post-Tutorial FO | ~180 | PERFECT | 3 |
| Keeper | ~20 | PERFECT | 4 |
| Syrel | ~12 | PERFECT | 4 |
| Faction Greetings | ~25 | PERFECT | 3 |
| War Faces | ~30 | GOOD | 4 |
| Precursor Scientists | ~100 | OKAY | 4 (optional) |
| Narrator | 1 | PERFECT | 1 |
| **Voice subtotal** | **~437** | | |
| Combat SFX | ~13 | GOOD | 2 |
| Navigation SFX | ~5 | EXCELLENT | 1 |
| UI SFX | ~8 | EXCELLENT | 1 |
| Discovery SFX | ~3 | GOOD | 2 |
| Ambient Loops | ~6 | EXCELLENT | 3 |
| Risk Alerts | ~4+ | EXCELLENT | 3 |
| **SFX subtotal** | **~39** | | |
| **TOTAL** | **~476 files** | | |
