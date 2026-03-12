# First-Hour Experience — Design Bible

> The first hour is the game's entire pitch. This doc defines what we're building
> toward — the emotional arc, the teaching sequence, and the hooks that make a
> player stay. Everything else (bot assertions, verified numbers) is in the
> appendix. This section is the north star.
>
> Companion to `NarrativeDesign.md`, `CombatFeel.md`, `dynamic_tension_v0.md`,
> `HudInformationArchitecture.md`, `camera_cinematics_v0.md`.
> Epic: EPIC.S19.FIRST_HOUR_EXPERIENCE.V0

---

## The Five Goals

Everything in the first hour serves exactly one of these goals:

### 1. The Galaxy Is Already Alive

The player enters a world that was here before them and will continue without
them. NPC traders fly trade routes. Factions hold territory. Prices reflect
supply and demand shaped by war, geography, and production chains. The player
is not the center of the universe — they are a newcomer in a functioning
economy that they can exploit, disrupt, or depend on.

**What this means in practice:**
- NPC ships are visibly doing things when the player arrives — not spawning, not
  idling, but mid-route
- Station markets have prices that make geographic sense (mining hubs sell ore
  cheap, refineries buy it dear) without explanation
- Faction territory is visible through color, labels, and NPC density — not
  through exposition
- The war is felt through prices before it's seen through combat. Munitions cost
  more near the front. The economy tells the story

**The test:** A player who does nothing for 60 seconds should still see movement,
activity, and evidence of a world operating on its own logic.

### 2. Every Action Teaches Something

No tutorials. No popups. No "Press X to continue." The player learns by doing,
and every action has a visible consequence that teaches the next action.

**The teaching sequence:**
- Flying teaches that the galaxy has places to go (lane gates glow, stations
  have proximity markers)
- Docking teaches that stations have things to offer (market prices, missions,
  modules)
- Buying teaches that cargo has weight and cost
- Warping teaches that different places have different economies
- Selling teaches that geography is money
- Combat teaches that danger exists and rewards follow
- Upgrading teaches that the ship can grow
- The galaxy map teaches that everything seen so far is a fraction of what exists

**The principle:** One system per encounter. The first dock teaches buying. The
second teaches selling. The third teaches missions. Never two new systems at
once. The player should feel like they're discovering mechanics, not being shown
them.

**The test:** A player should be able to articulate "how to make money" within
10 minutes without ever reading an instruction.

### 3. The First Officer Is a Person, Not a Tooltip

The FO is the player's emotional anchor and the game's primary voice. They are
not a tutorial system wearing a character skin. They have a personality, a
perspective, and reactions that feel authored — because they are.

**What the FO must accomplish in the first hour:**
- Establish a distinct voice within the first warp (three archetypes: Analyst
  Maren's dry precision, Veteran Dask's institutional warmth, Pathfinder Lira's
  sensory curiosity)
- React to the same events the player just experienced, not predict them.
  Commentary after the fact ("That margin was generous — I've noted the route")
  feels like a companion. Advice before the fact ("You should buy ore") feels
  like a tutorial
- Make one observation that surprises the player — something they didn't notice.
  Lira at a warzone station: *"The air recyclers here taste different. Fear, I
  think. Or just overdue maintenance."* This is not gameplay information. It is
  atmosphere. It signals that the FO sees the world, not just the UI
- Never command. Suggest, observe, react. The player is the captain

**The relationship goal:** By minute 15, the player should feel a preference for
their FO — not because they chose optimally, but because the voice feels like
it belongs to a person they're starting to know.

**The test:** If you replaced every FO line with a generic tooltip, the player
should feel a loss.

### 4. Profit Feels Like Discovery

The first trade is not a mechanical exercise. It is the moment the player
discovers that the galaxy's geography creates opportunity — that paying attention
to the world is rewarded. This must feel like a personal insight, not an
assigned task.

**The emotional sequence:**
1. The player docks. Ore is cheap (2 cr). They might buy some, or might not
2. They warp to the next system. They dock. Ore sells for 142 cr
3. The credits counter jumps. A toast celebrates. The FO reacts
4. The player thinks: *"Wait — I could have bought MORE."*

That fourth beat — the retrospective optimization — is the hook. It's the moment
the player becomes a trader. Not because the game told them to trade, but
because they saw an opportunity they partially missed and want to do better.

**What this requires:**
- The starter economy must produce an unmissable arbitrage opportunity (mine
  adjacent to refinery, 70x margin on ore)
- The FO must react to the profit, not predict it. After-the-fact commentary
  transforms a mechanical event into a shared experience
- The profit must be large enough to buy something meaningful immediately (any
  starter module at 50-60 cr, with 680 cr in pocket)
- The opportunity must degrade naturally as the player trades it, forcing them
  to discover new routes — transitioning from "tutorial economy" to "real
  economy" without a seam

**The test:** The player's second trade should be self-directed, not suggested.

### 5. The Promise of Depth

By minute 30, the player should understand enough to know how much more there
is. They've seen 35% of the galaxy map. They've found 3 of 12 technologies.
They've met one faction's stations but seen borders of two others. They've
fought once but seen NPC patrols with ships much larger than theirs.

**The layering:**
- Minutes 1-5: The core loop exists (fly, trade, fly)
- Minutes 5-10: There are other things to do (missions, combat)
- Minutes 10-15: The ship can grow (modules, fitting)
- Minutes 15-30: The galaxy is vast and varied (map reveal, faction territory,
  price diversity, tech tree)
- Minute 30+: Hints of something deeper (fracture rumors, strange readings,
  the FO noticing things they can't explain yet)

The critical principle: **each layer must be earned, not dumped.** The galaxy map
is most impressive after you've manually flown 3 systems and then see 17 more
you haven't touched. The tech tree is most compelling after you've wished your
ship could do something it can't. The fracture tease is most effective after
you've trusted the lanes for 30 minutes.

**The test:** At minute 30, the player should be able to name three things they
want to do next — and those three things should be different from player to
player.

---

## The Emotional Arc

The first hour has a shape. It is not flat. It is not a steady climb. It has
valleys that make the peaks feel earned.

```
Intensity
  ^
  |                                              * The Promise
  |                                             / \
  |            * First Sale                    /   \
  |           / \          * First Kill       /     ~~~~~ Deepening
  |          /   \        / \                /
  |    * Dock     \      /   \    * Upgrade /
  |   /    \       \    /     \  /  \      /
  |  /      ~~~~~~~  \/       \/    \    /
  | /                                  \/
  |/ Cold Open                     Sustain pressure
  +-------------------------------------------------> Time
  0    5    10    15    20    25    30    45    60
```

**The valleys matter.** The quiet between first sale and first combat is where the
player develops their own rhythm. The dip after first upgrade is where sustain
pressure begins and the player realizes the galaxy has a cost. The deepening
after minute 30 is where the game stops teaching and starts trusting.

---

## What Makes STE's First Hour Different

Most space trading games front-load either spectacle (No Man's Sky) or systems
(X4). STE front-loads **consequence**. Every action in the first 10 minutes has
a visible, meaningful result:

- Buy ore -> your cargo counter changes, your credits drop
- Warp -> you're in a new place with different prices
- Sell -> your credits jump, a milestone fires, the FO reacts
- Fight -> the enemy drops loot, your hull is scratched
- Upgrade -> your ship has a new module, a slot is filled

No action is cosmetic. No action is deferred. The player never does something
and wonders "did that matter?" The answer is always visible, always immediate.

**The identity statement:** STE is a game where geography is destiny, where
paying attention to the world is the primary skill, and where your First Officer
is the only person in the galaxy who knows your name. The first hour must
establish all three.

---

## The Gaps Between Here and There

The mechanical loop works. The bot passes 21 assertions. But the *experience*
has gaps between "functional" and "compelling":

| Gap | What's Missing | Why It Matters |
|-----|---------------|---------------|
| **The FO is late** | No companion voice before promotion. The first 2-3 minutes are narratively silent | The cold open needs a voice. Not instructions — atmosphere |
| **Combat is silent** | No weapon audio, no explosion on kill, no shield-break flash | First kill should be a *moment*. Currently ships just vanish |
| **No urgency clock** | Maintenance costs exist but don't create pressure until late | The "I need to find a route before I go broke" feeling is absent. The scramble phase is too comfortable |
| **Stations feel identical** | Same model, same UI, same atmosphere regardless of faction | The promise of faction diversity isn't delivered visually. A Concord station should feel different from Communion |
| **The FO doesn't tip you off** | No contextual trade advice ("ore is cheap here, refineries pay premium") | The first trade works mechanically but the FO doesn't help the player discover it |
| **No cost-basis display** | Player can't see "bought at X, sells for Y here" | The geography-is-money lesson requires the player to remember prices across stations |
| **Tab reveal isn't milestone-gated** | All dock tabs visible from first dock (if non-empty) | Progressive disclosure is data-driven (hide-empty) not experience-driven (milestone-gated) |
| **Warzone atmosphere is theoretical** | Prices reflect war but no visual/audio differentiation | Lira's "air recyclers taste different" line has no visual backing — the station looks the same |
| **Galaxy map isn't a revelation** | Always available via G key | Should be formally introduced after 3-5 system visits, making the scale reveal a designed moment |
| **Loot pickup is invisible** | No "loot available" indicator, no pickup animation | The reward from combat is mechanical, not experiential |

These are the implementation targets for making the first hour feel right. The
loop is there. The numbers work. The *feeling* needs these gaps closed.

---

## Industry Reference

### Winners and Losers

| Game | First 5 min | Verdict |
|------|-------------|---------|
| **Freelancer** | Cinematic explosion -> scripted escape -> narrative landing -> clear purpose | **Gold standard** — story makes every system introduction feel natural |
| **FTL** | Full gameplay loop in 2 minutes. Jump -> encounter -> combat -> loot -> jump | **Density king** — maximum decisions per minute |
| **No Man's Sky** | Crash-landed, survival pressure, first flight is a peak emotional moment | **Spectacle hook** — ground-to-orbit transition sells the game |
| **Rebel Galaxy** | Combat within 60 seconds, rock/blues soundtrack, clear upgrade path | **Feel-first** — makes the genre accessible in 5 minutes |
| **Starsector** | Tutorial combat, then fuel/supply pressure creates urgency immediately | **Stakes hook** — you're always running low on something |
| **Elite Dangerous** | Undock from station with zero guidance, fly into void | **The cautionary tale** — players quit because they don't know what to do |
| **X4: Foundations** | Walk slowly around empty station, board ship, fight the UI | **The complexity trap** — all systems exposed, none explained |
| **EVE Online** | Click-to-move tutorial, auto-attack weak NPC, dense UI | **"Trust me, it gets good later"** — shouldn't take 10 hours |

### Seven Commandments

1. **Minute 1 must establish identity.** The world, not a menu
2. **Core loop in 10 minutes.** Fly -> dock -> trade -> fly -> encounter
3. **First trade = heist, not homework.** Framing over mechanics
4. **Front-load upgrades, not information.** Tangible improvement in 15 minutes
5. **One system per encounter.** Never dump two systems simultaneously
6. **Direction AND permission.** Default path for uncertain, side paths for curious
7. **Never lost for 30 seconds.** Always a visible next step in the world

---

## Appendix A: Verified Bot Data

> All numbers measured from `test_first_hour_proof_v0.gd`. 21/21 assertions PASS.
> Run: `powershell -File scripts/tools/Run-FHBot.ps1 -Mode headless`

| Milestone | Verified Value |
|-----------|---------------|
| Starting credits | 1000 |
| Starting node | star_10 (mining hub) |
| NPC count at start | 3 |
| Goods at first dock | 7 |
| First trade good | ore (buy 2 cr, sell ~142 cr) |
| First trade profit | +680 cr (68% of starting capital) |
| Missions at 2nd station | 3 |
| Combat hits / damage | 5 / 100 |
| Player hull after combat | 100 (unscathed) |
| Modules available | 38 |
| Module installed | fuel_tank_mk1 (60 cr, slot 2) |
| Galaxy size | 20 nodes, 26 edges |
| Nodes visited | 7 of 20 (35%) |
| Trades completed | 3 |
| Unique price profiles | 4 |
| Technologies available | 12 |
| Fuel at end | 457 |
| Aesthetic audit | 11/12 PASS (1 false positive: camera altitude) |
| Flags | 0 |
| Screenshots captured | 20 |

### Economic Design

The starter economy is a teaching machine — mine adjacent to refinery, 70x
margin on ore. Pricing: `mid = 100 + (50 - stock)`, 10% spread. At 500 stock:
buy ~2 cr. At 0 stock: sell ~142 cr. The spread is structural (galaxy
generator's even/odd node assignment), not random.

```
   Mining Hub (star_10)          Refinery (star_9)
   ┌─────────────────┐          ┌─────────────────┐
   │ Ore:   500 (2cr)│ ──────> │ Ore:     0 (142cr)│
   │ Fuel:  120      │          │ Metal: 200       │
   │ Metal:  10      │          │ Fuel:   10       │
   └─────────────────┘          └─────────────────┘
         BUY HERE                    SELL HERE
```

---

## Appendix B: Proof Testing

### GDScript First-Hour Bot

`scripts/tests/test_first_hour_proof_v0.gd` — 31 phases, 6 acts, 21 hard
assertions, 20 screenshot captures.

```bash
# Headless (CI)
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless

# Visual (screenshots)
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode visual
```

### C# ExplorationBot First-Hour Extension

`SimCore.Tests/ExperienceProof/ExplorationBotTests.cs` ->
`Bot_FirstHourExperience_Across5Seeds()` — 200 ticks across 5 seeds.

```bash
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q \
  --filter "FirstHour"
```
