# Tutorial & Onboarding Design — v0

Authoritative spec for the 7-act tutorial. Supersedes any prior tutorial
documentation. All dialogue, phase transitions, and gate conditions are
implemented in `SimCore/Systems/TutorialSystem.cs`,
`SimCore/Content/TutorialContentV0.cs`, and
`scripts/ui/tutorial_director.gd`.

---

## Philosophy

The tutorial serves five goals from `first_hour_experience_v0.md`:

1. **Flow** — player earns credits within 2 minutes
2. **Tension** — the galaxy feels alive and dangerous
3. **Immersion** — characters feel real, not like tooltips
4. **Competence** — player masters trade loop before automation
5. **Curiosity** — mystery seeds pull player forward

Industry references: Factorio (pain before relief), Subnautica
(world-first explanation-second), Portal (one mechanic per encounter),
Hades (character reactivity), Outer Wilds (knowledge gates).

---

## 7-Act Structure

### Act 1: Cold Open (Ship Computer)

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| Awaken | 1 | Ship Computer | DialogueDismissed | Setting, tone |
| Flight_Intro | 2 | Ship Computer | DialogueDismissed | WASD, click-to-fly |
| First_Dock | 3 | — | Bridge dock notify | Docking (E key) |

### Act 2: The Crew (Maren)

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| Module_Calibration_Notice | 32 | Ship Computer | DialogueDismissed | Mystery seed (NarrativeDesign.md) |
| Maren_Hail | 4 | Maren | DialogueDismissed | FO introduction |
| Maren_Settle | 5 | Maren | DialogueDismissed | Warfront context, economy |
| Market_Explain | 6 | Maren | DialogueDismissed | Supply/demand basics |
| Buy_Prompt | 7 | — | Cargo > 0 | Market tab, buying |
| Buy_React | 8 | Maren | DialogueDismissed | Confirmation, margin |

### Act 3: The Trade Loop (Maren) — 3 manual trades required

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| Cruise_Intro | 16 | Ship Computer | DialogueDismissed | Cruise drive (C key) |
| Travel_Prompt | 9 | — | NodesVisited increased | Lane gates, travel |
| Jump_Anomaly | 33 | Maren | DialogueDismissed | World-is-watching seed (first trade only) |
| Arrival_Dock | 10 | — | Bridge dock notify | — |
| Sell_Prompt | 11 | — | GoodsTraded increased | Selling for profit |
| First_Profit | 12 | Maren | DialogueDismissed | Profit confirmation |

First_Profit increments `ManualTradesCompleted`. If < 3, loops back to
Travel_Prompt. If >= 3, advances to World_Intro. Jump_Anomaly only fires
on the first trade (`ManualTradesCompleted == 0`).

### Act 4: The World (Maren)

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| World_Intro | 14 | Maren | DialogueDismissed | Galaxy opens up |
| Explore_Prompt | 15 | — | NodesVisited >= ExploreCompleteNodes | Exploration |
| Galaxy_Map_Prompt | 17 | Maren | DialogueDismissed | Galaxy map (M key) |

### Act 5: The Threat (Dask)

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| Threat_Warning | 18 | Dask | DialogueDismissed | Danger exists |
| Dask_Hail | 19 | Dask | DialogueDismissed | Dask introduction |
| Combat_Engage | 20 | — | NpcFleetsDestroyed increased | Combat basics |
| Combat_Debrief | 21 | Dask | DialogueDismissed | Combat aftermath, fuel |
| Repair_Prompt | 22 | — | Hull restored to max | Ship repair |

### Act 6: The Upgrade (Lira)

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| Module_Intro | 23 | Lira | DialogueDismissed | Modules exist |
| Module_Equip | 24 | — | Module installed | Module equipping |
| Module_React | 25 | Lira | DialogueDismissed | Module confirmation |
| Lira_Tease | 26 | Lira | DialogueDismissed | Drive anomaly seed |

### Act 7: The Empire + Graduation

| Phase | ID | Speaker | Gate | Teaches |
|-------|-----|---------|------|---------|
| Automation_Intro | 27 | Maren | DialogueDismissed | Automation concept |
| Automation_Create | 28 | — | Program created | Trade charter setup |
| Automation_Running | 29 | — | AutomationWaitTicks elapsed | Automation patience |
| Automation_React | 30 | Maren | DialogueDismissed | Automation payoff |
| FO_Selection | 13 | — | Candidate selected | Choose first officer |
| Mystery_Reveal | 41 | Selected FO | DialogueDismissed | Lore escalation |
| Graduation_Summary | 42 | Ship Computer | DialogueDismissed | Stats recap |
| FO_Farewell | 43 | Selected FO | DialogueDismissed | FO personality |
| Milestone_Award | 44 | Selected FO | DialogueDismissed | Achievement |
| Tutorial_Complete | 45 | — | — | Tutorial ends |

---

## Dialogue Principles

### 1. FOs observe/react, never instruct
HUD objectives handle "Press M for galaxy map." FOs say "The map should
show contested zones — prices shift near the front lines." This maintains
character voice and avoids FOs sounding like tooltips.

### 2. Cover-story naming enforced
No "fracture," "adaptation," "ancient," or "organism" before Module
Revelation (~Hour 8). Use generic terms: "the drive," "SRE,"
"long-range." See Cover-Story Enforcement Table below.

### 3. Character voice consistency
- **Maren** = probability framing ("73% chance the margin holds")
- **Dask** = military assessment ("Hostiles confirmed. Weapons free.")
- **Lira** = sensory observation ("Your drive's harmonic signature...")

### 4. FO dialogue budget
15-line FO budget in tutorial (half the 30-line/20hr cap from
NarrativeDesign.md). Each line must earn its place.

### 5. Warfront felt through prices from tick 1
Maren_Settle references the warfront affecting trade: "Prices here are
distorted — the warfront's got the mining runs backed up."

### 6. One system per encounter
Never introduce two new game concepts simultaneously. Each phase teaches
exactly one mechanic or narrative beat.

### 7. Pain before relief
3 manual trades required before automation unlocks (Factorio principle).
The player must feel the tedium of manual trading to appreciate
automation as the core loop.

---

## Progressive Disclosure Map

Systems NOT taught in tutorial (trigger post-tutorial via gameplay):

| System | Trigger | Design Note |
|--------|---------|-------------|
| Commissions | First commission encounter at station | Natural discovery |
| Haven | Player discovers Haven through gameplay | World-first, not menu-first |
| Research | First Haven research lab visit | Unlocks organically |
| Knowledge Web | First K-key press or discovery connection | Curiosity-driven |
| Frontier/Fracture | First fracture opportunity after Module Revelation | Cover-story safe |

---

## Cover-Story Enforcement Table

Words forbidden before Module Revelation (~Hour 8):

| Forbidden | Allowed Alternative | Context |
|-----------|-------------------|---------|
| fracture | the drive, SRE, long-range | Travel method |
| adaptation | enhancement, modification | Module effects |
| ancient | old, pre-Concord | Historical references |
| organism | anomaly, signature, pattern | Drive behavior |

---

## FO Voice Reference

### Maren (Analyst)
- "73% chance the margin holds at the next port. I've seen worse odds."
- "Profit logged. Margin held to within 2 credits of my estimate."
- "Did you see that? Scanner went dark for 0.3 seconds during transit."

### Dask (Veteran)
- "Hostiles confirmed on long-range. Weapons free when you're ready."
- "Hull took damage. Nothing structural. Dock and repair when convenient."
- "Also noted the fuel gauge — worth topping off when you repair."

### Lira (Pathfinder)
- "Your drive's harmonic signature doesn't match any registry I've cross-referenced."
- "The resonance pattern is unlike anything in the standard catalogs."
- "I found a match for that harmonic anomaly. One match. In a fragment from a pre-Concord survey station."

---

## Rotating FO System

Before FO selection (Acts 2-7), each act has a designated rotating FO:

| Act | Rotating FO | Rationale |
|-----|------------|-----------|
| 2 (The Crew) | Maren (Analyst) | First contact, economy focus |
| 3 (Trade Loop) | Maren (Analyst) | Continuity with trade introduction |
| 4 (The World) | Maren (Analyst) | Exploration is economic scouting |
| 5 (The Threat) | Dask (Veteran) | Combat specialist |
| 6 (The Upgrade) | Lira (Pathfinder) | Technology/anomaly specialist |
| 7 (The Empire) | Maren (Analyst) | Automation is economic mastery |

After FO selection (Mystery_Reveal onward), the selected FO speaks all
remaining dialogue.

---

## Verification

### Bot commands
```bash
# Single seed
godot --headless --path . -s res://scripts/tests/test_tutorial_proof_v0.gd -- --seed=42

# 3-seed sweep (covers all FO rotations: 42=Analyst, 100=Veteran, 1001=Pathfinder)
powershell -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script tutorial -Seeds 42,100,1001
```

### What the bot validates
- All ~30 active phases visited in order
- No duplicate dialogue
- Speaker correctness per act (rotating FO pre-selection, selected FO post)
- Trade loop: 3 iterations with profit verification
- Objective text matches player action
- Tab disclosure at correct milestones
- Save/load round-trip preserves tutorial state
- SkipTutorialV0 works correctly
- New Voyage re-initialization cleans state

### Known headless warns (0-6)
- `correct_station_not_flagged_bad` — bot picks suboptimal sell station on some seeds
- `sell_profit` — zero profit at bad station (trade 3)
- `manual_trades_gte_3` — bridge reads before sim increments (timing)
- `trade_loop_travel_prompt_count` — Travel_Prompt raced past on first trade
- `phase_timing_anomalies` — fast dialogue transitions in headless
- `objective_coverage` — headless race skips objective capture

---

## Dead Phases (save compatibility)

These enum values are preserved for save compatibility but never entered
in the 7-act flow:

| ID | Name | Reason |
|-----|------|--------|
| 31 | Commission_Intro | Moved to post-tutorial |
| 34 | Haven_Upgrade_Prompt | Moved to post-tutorial |
| 35 | Haven_React | Moved to post-tutorial |
| 36 | Research_Intro | Moved to post-tutorial |
| 37 | Research_Start | Moved to post-tutorial |
| 38 | Research_React | Moved to post-tutorial |
| 39 | Knowledge_Intro | Moved to post-tutorial |
| 40 | Frontier_Tease | Moved to post-tutorial |

---

## Key Files

| File | Role |
|------|------|
| `SimCore/Entities/TutorialState.cs` | Phase enum, state fields |
| `SimCore/Tweaks/TutorialTweaksV0.cs` | Balance constants (RequiredManualTrades=3) |
| `SimCore/Content/TutorialContentV0.cs` | All dialogue, objectives, rotating FO mapping |
| `SimCore/Systems/TutorialSystem.cs` | State machine, gate evaluation |
| `scripts/bridge/SimBridge.Tutorial.cs` | GDScript ↔ SimCore bridge |
| `scripts/ui/tutorial_director.gd` | Presentation, UI orchestration |
| `scripts/tests/test_tutorial_proof_v0.gd` | Headless verification bot |
