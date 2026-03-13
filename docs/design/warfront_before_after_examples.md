# Warfront Mechanics: Before/After Examples

Visual and mechanical examples showing what changes when you implement the 80/20 warfront system.

---

## Example 1: Territory Capture in Action

### BEFORE: Territory Discs Never Change

```
Tick 0 (Start)                          Tick 200 (Mid-game)
┌─────────────────────────────┐        ┌─────────────────────────────┐
│                             │        │                             │
│  🟠 Valorin                 │        │  🟠 Valorin                 │
│     ○ Proxima               │        │     ○ Proxima               │
│     ○ Kepler                │        │     ○ Kepler                │
│  🔵 Weavers                 │        │  🔵 Weavers                 │
│     ○ Vega                  │        │     ○ Vega                  │
│     ○ Altair                │        │     ○ Altair                │
│                             │        │                             │
│  [War] Valorin vs Weavers   │        │  [War] Valorin vs Weavers   │
│  Intensity: OpenWar         │        │  Intensity: OpenWar         │
│                             │        │                             │
│  NO VISIBLE CHANGE.         │        │  NO VISIBLE CHANGE.         │
│  Are Valorin winning? No    │        │  Are Valorin winning? ???   │
│  idea.                      │        │  Still no idea.             │
│                             │        │                             │
└─────────────────────────────┘        └─────────────────────────────┘
```

**Problem**: Player doesn't know who's winning. Wars feel cosmetic.

---

### AFTER: Territory Discs Change Color

```
Tick 0 (Start)                          Tick 200 (Valorin Dominates)
┌─────────────────────────────┐        ┌─────────────────────────────┐
│                             │        │                             │
│  🟠 Valorin                 │        │  🟠 Valorin (expanded!)     │
│     ○ Proxima               │        │     ⭕ Proxima [CAPTURED]   │
│     ○ Kepler                │        │     ⭕ Kepler [CAPTURED]    │
│  🔵 Weavers                 │        │  🔵 Weavers (shrinking)     │
│     ○ Vega                  │        │     ○ Vega                  │
│     ○ Altair                │        │     ○ Altair                │
│                             │        │                             │
│  [War] Valorin vs Weavers   │        │  [War] Valorin vs Weavers   │
│  Intensity: OpenWar         │        │  Intensity: TotalWar        │
│                             │        │                             │
│  VISIBLE FLOW:              │        │  CLEAR OUTCOME:             │
│  Valorin capturing nodes!   │        │  Valorin is WINNING.        │
│  Toast: "Valorin gains      │        │  Toast: "Valorin gains      │
│  Proxima" (at tick 117)     │        │  Kepler" (at tick 190)      │
│                             │        │  [Toast alert!]             │
│                             │        │                             │
└─────────────────────────────┘        └─────────────────────────────┘
```

**Improvement**: Player sees **who's winning in real-time**. Maps create narrative.

---

## Example 2: Escalation Pressure

### BEFORE: No Natural Escalation

```
Game State Over 500 Ticks
───────────────────────────────────────────────────────────────

Tick 0:   Warfront seeded at Intensity 3 (OpenWar) — war is hot
Tick 100: Still Intensity 3 — war is... still hot?
Tick 200: Still Intensity 3 — player could ignore forever
Tick 300: Still Intensity 3 — prices stay high, but no new pressure
Tick 400: Still Intensity 3 — eventually supply delivery threshold is hit, reduces to Intensity 2
Tick 500: Intensity 2 — war cools, prices normalize

PROBLEM: No pressure to engage. War is static. Player has zero time pressure.
```

---

### AFTER: Natural Escalation on Supply Neglect

```
Game State Over 500 Ticks (with escalation)
───────────────────────────────────────────────────────────────

Tick 0:     Warfront seeded at Intensity 1 (Tension) — war is cold
Tick 100:   Still Tension (no supplies, but not enough time yet)
Tick 200:   ESCALATE to Skirmish!
            Toast: "Warfront heats up: now Skirmish"
            Munitions demand 2x (not 1x) → prices spike
            Tariff +500bps (neutrality cost increases)

Tick 250:   Player delivers 100 Munitions
            Toast: "Supply stabilizes warfront"
            LastSupplyDeliveryTick = 250 (escalation timer resets)

Tick 450:   Still Skirmish (but no supplies for 200 ticks again)
Tick 451:   ESCALATE to OpenWar!
            Toast: "Warfront escalates: now OpenWar"
            Munitions demand 3x → more spike
            Tariff +1000bps (doubled)
            Fleet strength drains faster

Tick 500:   Player forced to act: supply or fracture-route or accept losses

IMPROVEMENT: Passive pressure escalates. Player has time pressure (escalation timer).
Supply is consequential (resets timer). Ignoring war becomes costly (tariffs increase).
```

---

## Example 3: Supply Chain Cascade

### BEFORE: Embargo Blocks One Good

```
Warfront Embargo: Munitions (at Proxima node)

Market State:
──────────────────────────
Proxima (embargoed):
  Munitions:  0 (blocked by embargo)

Remote node (Vega, no embargo):
  Composites: 100 (normal production)
  Refits:     50 (normal production)

PROBLEM: Embargo blocks Munitions, but production chains don't care.
Composites still produce normally (demand for Munitions input is ignored).
Refits still produce normally (no supply chain dependency detected).
War has NO ECONOMIC CONSEQUENCE beyond tariffs.
```

---

### AFTER: Embargo Cascades Through Production Chain

```
Production Chain: Munitions → Components → Refits

Warfront Embargo: Munitions (at Proxima node where Components producer is)

Market State Over Time:
──────────────────────────────────────────────────────────────

Tick 100 (embargo starts):
  Proxima market:
    Munitions: 0 (embargoed)
    Components production: STALLED (missing Munitions input)
    Components inventory: 100 (from previous ticks)
    Refits inventory: 50 (normal)

Tick 105:
  Proxima Components inventory drops (consumption, no production)
  Components: 85

Tick 110:
  Proxima Components: 70
  Refits production starts to stall (missing Components input)

Tick 115:
  Proxima Components: 55
  Refits: 50 (inventory not yet depleted)

Tick 120:
  Proxima Components: 40
  Refits production now STALLED too
  Toast: "Trade route broken: Components shortage prevents Refit production"

Tick 125+:
  Downstream nodes (depending on Refits) also stall
  Prices cascade:
    Refits: 100 → 150 → 200 → 300 credits (supply shock!)
    Components: 50 → 70 → 90 (derivative shock)

  Player's automation program:
    Status: BROKEN
    Diagnostic: "Refit Margin negative (300 cred/unit cost, 250 cred sell price)"
    Suggestion: "Reroute via Fracture to remote producer?"

IMPROVEMENT: Embargo has TEETH. Supply chains break. Prices cascade.
War creates economic CRISES, not just tariff adjustments.
Player must adapt (reroute, fracture-trade, or find new supplier).
```

---

## Example 4: Territory Control → Tariff Impact

### BEFORE: Static Factions, Fixed Tariffs

```
Player's Trade Route: Sol → Proxima

Trade Setup:
──────────────────────────────────────────────────────────
Route Node      Owner           Tariff   Player Status
─────────────────────────────────────────────────────────
Sol             Valorin         +10%     Allied
Proxima         Valorin         +10%     Allied
(no change ever)

Profit:
Sell at Sol: 100 units × 50 cred = 5000 cred
Tariff: 5000 × 10% = -500 cred
Tariff: 5000 × 10% = -500 cred
Net: 5000 - 1000 = 4000 cred

This route is stable. Player can repeat forever.
```

---

### AFTER: Territory Shifts Create Tariff Shocks

```
Player's Trade Route: Sol → Proxima

Initial Setup (Tick 100):
──────────────────────────────────────────────────────────
Route Node      Owner           Tariff   Player Status
─────────────────────────────────────────────────────────
Sol             Valorin         +10%     Allied
Proxima         Valorin         +10%     Allied (secure)

Profit: 4000 cred (stable)

WARFRONT ESCALATES...

Tick 200:
  Toast: "Weavers gain Proxima!"
  Route Node      Owner           Tariff   Player Status
  ────────────────────────────────────────────────────────
  Sol             Valorin         +10%     Allied
  Proxima         🔴 Weavers      +20%     Hostile (now enemy!)

  Player is now "Hostile" at Proxima (was Allied to Valorin)
  Regime shifted: Open → Hostile (patrol attacks on sight if cargo is war-relevant)

  Profit: 5000 - 500 - 1000 = 3500 cred (margin eroded 12.5%)

Tick 201-250:
  Player choices:
  Option A: Find new Proxima supplier (Weavers-aligned)
  Option B: Reroute via Fracture (bypass Weavers territory)
  Option C: Accept lower margin (3500 cred)
  Option D: Fight back (supply Valorin to help them recapture?)

This forces strategic decisions. War is no longer cosmetic.
```

---

## Example 5: Escalation Countdown HUD

### WARFRONT DASHBOARD — Live Example

```
╔══════════════════════════════════════════════════════════════════╗
║                        WARFRONT STATUS                           ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ┌─ VALORIN vs WEAVERS ─────────────────────────────────────┐  ║
║  │ Type: Hot War    Intensity: OpenWar (3)                  │  ║
║  │ Duration: 1800 ticks (60 days)  Started: Tick 300        │  ║
║  │                                                           │  ║
║  │ Fleet Strength:                                          │  ║
║  │   Valorin:  ████████░░ 82%                               │  ║
║  │   Weavers:  ████░░░░░░ 45%                               │  ║
║  │                                                           │  ║
║  │ Contested Nodes: Proxima, Kepler, Vega [3 nodes]         │  ║
║  │                                                           │  ║
║  │ Strategic Objectives:                                    │  ║
║  │   ⚙ Supply Depot @ Proxima                               │  ║
║  │     Control: Valorin  [Dominance: 14/20 ticks]           │  ║
║  │     ▌▌▌▌▌▌▌░░░░░ (70% to next swing)                     │  ║
║  │                                                           │  ║
║  │   ⦿ Comm Relay @ Kepler                                  │  ║
║  │     Control: Weavers  [Dominance: 2/20 ticks]            │  ║
║  │     ▌░░░░░░░░░░ (10% to next swing)                      │  ║
║  │                                                           │  ║
║  │   ◼ Factory @ Vega                                       │  ║
║  │     Control: Weavers  [Dominance: 19/20 ticks]           │  ║
║  │     CAPTURING NEXT TICK ⚠️                                │  ║
║  │                                                           │  ║
║  │ ╔═ ESCALATION COUNTDOWN ═════════════════════════════╗  │  ║
║  │ ║ Last supplies delivered: 150 ticks ago             ║  │  ║
║  │ ║ Next escalation: 50 ticks                          ║  │  ║
║  │ ║ ████████░░░░░░░░░░░░░░ (75% to escalation)         ║  │  ║
║  │ ║ ⚠️  Deliver supplies to stabilize!                  ║  │  ║
║  │ ╚═══════════════════════════════════════════════════╝  │  ║
║  │                                                           │  ║
║  │ Your Contribution This Session:                           │  ║
║  │   Munitions:  450 units (Valorin +30 rep, Weavers -15 rep)  │  ║
║  │   Composites: 200 units (Valorin +10 rep)                   │  ║
║  │   Fuel:       100 units                                      │  ║
║  │                                                           │  ║
║  └───────────────────────────────────────────────────────────┘  ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

**What This Tells Player**:
- ✅ Valorin is winning (82% vs 45% fleet strength)
- ✅ Objectives are the tactical focus (Factory about to flip)
- ✅ **Escalation countdown is critical** — only 50 ticks before war heats up
- ✅ Supplies have impact (reputation gains show contribution)
- ✅ Next action is clear: deliver 200+ Munitions to stabilize

---

## Example 6: Economic Pressure Over 1000 Ticks

### Scenario: Player Ignores Warfront

```
Tick 0: Warfront seeded at Tension. Tariffs +500bps (neutral cost).
        Player thinks: "War is far away, I'll ignore."

Tick 200: No supplies delivered.
          ESCALATE: Intensity → Skirmish
          Munitions demand: 1x → 2x (prices spike 100%)
          Tariff: +500bps → +1000bps
          Toast: "Warfront heats up!"
          Player realizes: "Oops, prices doubled."

Tick 250: Player still doesn't deliver supplies.
          Fleet strength drains: Valorin 100 → 92, Weavers 100 → 85

Tick 400: Still no supplies.
          ESCALATE: Intensity → OpenWar
          Munitions demand: 2x → 3x (another 50% spike)
          Tariff: +1000bps → +1500bps
          Toast: "Warfront reaches critical levels!"
          Player forced to act: supply or abandon the region.

Tick 500: Territory shift.
          Weavers capture Proxima (14-20 tick dominance completed).
          Toast: "Weavers gain Proxima!"
          Player's favorite trading node is now enemy territory.
          Regime: Allied → Hostile (patrol attacks cargo)

Tick 600: Player finally supplies 200 Munitions.
          Intensity: OpenWar → Skirmish (decreases by 1)
          Toast: "Warfront stabilizes."
          Tariff: +1500bps → +1000bps
          LastSupplyDeliveryTick = 600 (escalation timer resets)

Tick 800: Still no more supplies.
          ESCALATE: Intensity → OpenWar (again)
          Tariff: +1000bps → +1500bps

LESSON: Ignoring the war has COSTS.
        - Prices escalate to unaffordable
        - Territory shifts to enemies
        - Tariffs grow to 15% (was 5% at start)
        - Profit margins evaporate

SOLUTION: Engage with warfront (supply, fracture-trade, or commit to faction).
```

---

## Example 7: Objective Capture Mechanics

### Dominance Tracking (Real-Time Battle for a Node)

```
⚙ Supply Depot @ Proxima

Tick 100:
  Controlling: Valorin
  Dominant: Valorin (no opposition)
  Dominance Ticks: 0/20
  ░░░░░░░░░░░░░░░░░░ (0%)

Tick 110: Weavers NPC fleet arrives
  Controlling: Valorin (committed)
  Dominant: Weavers (currently winning)
  Dominance Ticks: 0/20 (reset, Weavers now tracking)
  ░░░░░░░░░░░░░░░░░░ (0%)

Tick 115: Weavers fleet wins skirmish
  Dominant: Weavers
  Dominance Ticks: 5/20
  ▌▌░░░░░░░░░░░░░░░░ (25%)

Tick 120: Weavers win again
  Dominance Ticks: 10/20
  ▌▌▌▌▌░░░░░░░░░░░░ (50%)

Tick 125: Valorin counterattack, PLAYER helps
  Dominant: Valorin (fleet victory)
  Dominance Ticks: 0/20 (reset, control swinging back)
  ░░░░░░░░░░░░░░░░░░ (0%)

Tick 130: Valorin maintains presence
  Dominance Ticks: 5/20
  ▌▌░░░░░░░░░░░░░░░░ (25%)

Tick 140: Weavers regain advantage
  Dominant: Weavers
  Dominance Ticks: 0/20 (reset again)
  ░░░░░░░░░░░░░░░░░░ (0%)

Tick 150: Weavers consolidate
  Dominance Ticks: 10/20
  ▌▌▌▌▌░░░░░░░░░░░░ (50%)

Tick 160: Weavers continue
  Dominance Ticks: 20/20 ✅ THRESHOLD REACHED
  ▌▌▌▌▌▌▌▌▌▌░░░░░░░░ (100%)

  CONTROL SHIFTS: Valorin → Weavers
  Toast: "Weavers capture Supply Depot!"
  Proxima now belongs to Weavers.
  Future dominance resets.

Tick 165:
  Controlling: Weavers (new holder)
  Dominant: Weavers
  Dominance Ticks: 5/20 (Valorin would need 20 ticks to retake)
  ▌▌░░░░░░░░░░░░░░░░ (25% progress to counter-capture)
```

---

## Example 8: Dynamic NPC Behavior (Optional Tier)

### NPC Patrol Frequency Scales with Warfront Intensity

```
Patrol Activity Over Time
─────────────────────────────────────────────────────

Tension (Intensity 1):
  Patrol frequency: 1 fleet every 50 ticks
  Scan range: 10 AU
  Aggressiveness: Scan only (no attack)
  ░░░░░░░░░░░░░░░░░░ (baseline)

Skirmish (Intensity 2):
  Patrol frequency: 1 fleet every 25 ticks (2x)
  Scan range: 15 AU
  Aggressiveness: Scan + warning shots
  ▌▌░░░░░░░░░░░░░░░░ (2x baseline)

OpenWar (Intensity 3):
  Patrol frequency: 1 fleet every 12 ticks (4x)
  Scan range: 20 AU
  Aggressiveness: Attack escort-sized fleets
  ▌▌▌▌▌░░░░░░░░░░░░ (4x baseline)

TotalWar (Intensity 4):
  Patrol frequency: 1 fleet every 6 ticks (8x)
  Scan range: 25 AU
  Aggressiveness: Attack anything with war cargo
  ▌▌▌▌▌▌▌▌░░░░░░░░ (8x baseline)

PLAYER EXPERIENCE:
- At Tension: "I barely see patrols. War feels distant."
- At OpenWar: "Patrols are everywhere! This sector is HOT."
- At TotalWar: "I can't move without a patrol encounter. War is inescapable."

This makes warfront intensity FELT, not just numerical.
```

---

## Summary: The Transformation

### BEFORE (Current):
- Wars are **invisible** (no territory shifts)
- Wars are **passive** (no escalation pressure)
- Wars are **isolated** (don't affect supply chains)
- Wars feel like **flavor text** (not gameplay)
- Player can **ignore** warfronts (tariffs are survivable)

### AFTER (With 80/20 System):
- Wars are **visible** (territory moves on map)
- Wars are **active** (escalate without supplies)
- Wars are **cascading** (break supply chains)
- Wars feel like **crises** (require player action)
- Player must **engage** with warfronts (adapt or lose)

**Result**: Warfronts go from **backdrop to pillar**. This is the difference between "interesting world" and "world that shapes your strategy."

