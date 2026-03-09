# Automation & Fleet Programs — UX Design Bible

> Design doc for the player-facing automation system: program creation, transparency,
> override mechanics, failure feedback, and the doctrine layer.
> Companion to `EmpireDashboard.md` (Programs tab) and `ship_modules_v0.md`.

## Why This Doc Exists

Automation is the game's core identity. The tagline is "manage an automated fleet empire."
Every game that documents automation design up front (Factorio) succeeds through
transparent, deterministic primitives. Every game that designs it ad-hoc (Victoria 3,
Distant Worlds 2) fails through the same trap: opaque AI that players cannot understand,
predict, or override.

This doc prevents us from building Victoria 3's autonomous investment system.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| 8 program kinds (AutoBuy/Sell, Charter, Tap, Expedition, Escort, Patrol, Construction) | Done | Intent-based, deterministic |
| Paused/Running/Cancelled lifecycle | Done | SetProgramStatusCommand |
| Cadence scheduling (NextRunTick, LastRunTick) | Done | Default 60 ticks (1 game day) |
| ProgramExplain snapshot (status + token metadata) | Done | Schema-bound tokens, no free text |
| ProgramQuote pre-flight (pricing, constraints, risks) | Done | AutoBuy/AutoSell only |
| ProgramEventLog (CREATED/STATUS/RAN events, 25 max) | Done | Deterministic ring buffer |
| ProgramsMenu (list + detail modal) | Done | Start/Pause/Cancel controls |
| Manual override pauses fleet-bound programs | Done | ManualOverrideSet event |
| Budget caps / cost limits | Not implemented | Programs run unconstrained |
| Doctrine system (risk tolerance, automation levels) | Not implemented | Design pillar, no code |
| Profitability tracking per program | Not implemented | No P&L history |
| Failure reason display in UI | Not implemented | Internal reason codes only |
| "Why did it do that?" explainer | Partial | Explain tokens exist, not surfaced clearly |
| Fleet group allocation | Not implemented | No multi-fleet coordination |
| Program templates / presets | Not implemented | Every program built from scratch |

---

## Design Principles

1. **The player is the architect, not the operator.** The player designs automation; the
   game executes it. Nothing should happen that the player didn't set up. This is the
   Factorio principle: you build the factory, the factory runs itself, and when something
   goes wrong you can trace it back to YOUR design. Contrast with Victoria 3 where the AI
   makes investment decisions that contradict the player's intentions.

2. **Every automated action must be predictable.** Before a program runs, the player must
   be able to answer: "What will it do? When will it do it? What could go wrong?" If the
   answer is "I don't know," the transparency has failed. The ProgramQuote system exists
   for this reason — it's the pre-flight checklist.

3. **Override in one action.** When automation does the wrong thing, the player must be
   able to intervene with a single click. Distant Worlds 2 requires 4 clicks per ship to
   override automation (160 clicks for 40 ships). Our rule: Pause/Cancel is always one
   button. Batch override (pause all programs of type X) is one action.

4. **Failure is visible, not silent.** When a program can't execute (no credits, no cargo
   space, no goods available), the failure must be displayed with the specific reason AND
   a suggested fix. A program that silently skips execution is indistinguishable from a
   program that's working. This is the single most common automation UX failure across
   games.

5. **Automation earns trust gradually.** New programs start Paused (existing behavior).
   The player must explicitly start them. As trust builds, offer "auto-start similar"
   presets. Never auto-create programs the player didn't request.

---

## The Automation Spectrum

Not everything should be automatable at the same granularity. Define the spectrum
explicitly to avoid the Distant Worlds 2 "all or nothing" trap.

### What the Player Always Controls (Never Automated)

| Decision | Why |
|----------|-----|
| Which goods to trade | Strategic identity — "what kind of empire am I building?" |
| Which systems to expand into | Territory is a strategic commitment |
| Research direction | Tech tree choices define build identity |
| Fleet composition | Ship class selection is an expression of playstyle |
| Diplomatic posture | Faction relationships have permanent consequences |

### What the Player Configures, the Game Executes (Programs)

| Program | Player Configures | Game Executes |
|---------|------------------|---------------|
| TradeCharter | Source, destination, buy good, sell good, cadence | Buy/sell intents each cadence tick |
| ResourceTap | Source market, extract good, cadence | Extract intent each cadence tick |
| AutoBuy/Sell | Market, good, quantity, cadence | Purchase/sale intent each cadence tick |
| Expedition | Discovery lead, fleet, cadence | Tick-down exploration counter, emit expedition intent |
| Escort | Fleet, origin, destination | Move fleet one-way to destination |
| Patrol | Fleet, node A, node B | Ping-pong fleet between nodes |
| Construction | Site, cadence | Supply missing stage inputs to construction pipeline |

### What the Game Does Automatically (No Player Setup)

| Behavior | Why No Setup Needed |
|----------|-------------------|
| NPC trade convoys | World simulation — NPCs have their own agency |
| Market price adjustment | Supply/demand is deterministic from the rules |
| Fleet combat when attacked | Defense is reflexive, not strategic |
| Shield regeneration | Passive recovery doesn't require player intent |

---

## Program Status Communication

### The Three Questions

Every program display must answer three questions at a glance:

1. **Is it working?** → Status indicator (Running/Paused/Stalled/Failed)
2. **What is it doing?** → Current action description ("buying 10 ore @ Sirius")
3. **Is it worth it?** → Profitability summary ("+340 cr/run" or "-120 cr/run net loss")

### Status Indicators — Visual Language

```
● RUNNING     (green pulse)   — executing normally, last run succeeded
◐ WAITING     (blue static)   — running but between cadence ticks
▲ STALLED     (yellow flash)  — attempted execution but couldn't complete (reason shown)
✕ FAILED      (red static)    — execution impossible, player intervention required
‖ PAUSED      (gray static)   — player-paused, not executing
⊘ CANCELLED   (dim, struck)   — terminal state, cannot restart
```

### Stall vs. Failure

This distinction is critical. Most games collapse these into one state, losing information:

| State | Meaning | Auto-Recovery? | Player Action |
|-------|---------|---------------|---------------|
| **Stalled** | Temporary resource shortage — program will retry next cadence | Yes — will succeed when resources available | Optional: replenish supply, or wait |
| **Failed** | Structural impossibility — market destroyed, route severed, fleet dead | No — requires reconfiguration | Required: edit or cancel program |

```
▲ Trade Charter: STALLED
  Last attempt: Tick 4,230 — insufficient ore at Sirius (need 10, have 3)
  Next retry: Tick 4,290 (60t cadence)
  Suggestion: Check ore supply at Sirius or reduce quantity

✕ Trade Charter: FAILED
  Cause: Destination market "Proxima" no longer exists (system lost to warfront)
  Action required: [Edit Route] or [Cancel Program]
```

---

## Program Creation Flow

### Current: Manual Parameter Entry

The player creates each program by selecting parameters individually. This works for
early game (1-3 programs) but scales poorly.

### Aspiration: Contextual Program Suggestions

When the player discovers a profitable trade route (via IntelBook price comparison),
offer a one-click "Create Trade Charter" with pre-filled parameters:

```
┌─ TRADE OPPORTUNITY ──────────────────────────────────┐
│                                                        │
│  Ore: Buy @ Sirius (12 cr) → Sell @ Proxima (18 cr)  │
│  Margin: +6/unit × 10 units = +60 cr/run              │
│  Est. cadence: 60 ticks (1 day)                        │
│                                                        │
│  [Create Trade Charter]  [Dismiss]                     │
│                                                        │
└────────────────────────────────────────────────────────┘
```

This follows the info-action principle: the intel system found the opportunity, the
program system can act on it, and the player bridges the two with one click.

### Program Templates

For late-game (10+ programs), offer templates:

| Template | Pre-filled | Player Adjusts |
|----------|-----------|----------------|
| "Mirror Route" | Same source/dest as existing charter, opposite goods | Good selection |
| "Resource Pipeline" | Tap → Charter chain for a production input | Source node |
| "Patrol Circuit" | Patrol between two nodes with highest security incidents | Fleet assignment |

---

## The Doctrine Layer (Future)

Doctrine is the meta-automation: rules that govern HOW programs behave, not WHAT they do.

### Doctrine Axes

| Axis | Low Setting | High Setting |
|------|------------|-------------|
| **Risk tolerance** | Cancel if any security incident on route | Accept losses up to 20% of cargo value |
| **Budget cap** | Never spend more than X credits/day on this program | Unlimited spending |
| **Priority** | This program yields to higher-priority programs for fleet time | This program always runs first |
| **Profitability floor** | Pause if margin drops below X cr/run | Run regardless of margin |

### Why Doctrine Matters

Without doctrine, the player must micromanage every program individually when conditions
change. With doctrine, the player sets policy once, and programs self-adjust:

```
Doctrine: Conservative Trade
  Risk tolerance: LOW (pause on hostile lanes)
  Budget cap: 500 cr/day
  Profitability floor: 20 cr/run minimum
  Priority: NORMAL

→ Trade Charter A: RUNNING (margin +45, within budget, safe route)
→ Trade Charter B: PAUSED BY DOCTRINE (margin +12, below floor)
→ Trade Charter C: PAUSED BY DOCTRINE (route now hostile, risk tolerance LOW)
```

The player sees exactly WHY each program is paused and can adjust doctrine globally
rather than editing three programs individually.

---

## Programs Tab in Empire Dashboard

(Cross-reference: `EmpireDashboard.md` Programs tab section)

### Master-Detail Layout

```
┌─ PROGRAMS ───────────────────────────────────────────────────────────────┐
│                                                                           │
│  Doctrine: [Conservative ▼]   Budget: 1,240/2,000 cr/day   8 active     │
│                                                                           │
│  ┌─ PROGRAM LIST ─────────────────┐  ┌─ PROGRAM DETAIL ──────────────┐  │
│  │ Sort: [Status ▼] Filter: [▽]  │  │ Trade Charter: Sirius→Proxima  │  │
│  │                                │  │                                 │  │
│  │ ● Charter Sirius→Prox  +340   │  │ Status: ● RUNNING              │  │
│  │ ● Charter Vega→Sol     +280   │  │ Cadence: 60t (1 day)           │  │
│  │ ● AutoBuy Ore @Sirius   -120  │  │ Last run: Tick 4,230 (12t ago) │  │
│  │ ▲ Tap Fuel @Barnard    stall  │  │ Next run: Tick 4,290 (48t)     │  │
│  │ ● Patrol Vega↔Sol      active │  │                                 │  │
│  │ ‖ Expedition @Kepler   paused │  │ ── PROFITABILITY ──            │  │
│  │                                │  │ Revenue: +380 cr/run           │  │
│  │                                │  │ Costs:   -40 cr/run (transit)  │  │
│  │                                │  │ Net:     +340 cr/run           │  │
│  │                                │  │ Lifetime: +4,080 cr (12 runs) │  │
│  │                                │  │                                 │  │
│  │                                │  │ ── EVENT LOG ──                │  │
│  │                                │  │ 4230: RAN — bought 10 ore      │  │
│  │                                │  │ 4170: RAN — sold 10 metal      │  │
│  │                                │  │ 4100: STATUS — Started          │  │
│  │                                │  │ 4100: CREATED                   │  │
│  │                                │  │                                 │  │
│  │                                │  │ [Pause] [Edit] [Cancel]        │  │
│  └────────────────────────────────┘  └─────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

### List Row Format

Each row shows the three-questions summary:

```
[status icon] [program type + route] [profitability or status text]
```

- Profitable programs: net profit in green
- Loss-making programs: net loss in red
- Stalled programs: "stall" in yellow with tooltip showing reason
- Paused programs: "paused" in gray

### Filtering & Sorting

| Filter | Options |
|--------|---------|
| Status | All, Running, Stalled, Paused, Failed |
| Type | All, Trade, Resource, Expedition, Patrol, Construction |
| Fleet | All, specific fleet name |

Sort by: Status (problems first), Profitability (best first), Last Run (recent first),
Name (alphabetical).

---

## Failure Feedback Taxonomy

Every program failure must display a specific reason code and suggested action.

| Failure Code | Description | Suggested Action |
|-------------|-------------|-----------------|
| `NO_CREDITS` | Insufficient credits for purchase | "Sell cargo or wait for income" |
| `NO_SUPPLY` | Market has insufficient stock of target good | "Wait for restocking or change source" |
| `NO_CARGO_SPACE` | Fleet cargo full, can't buy more | "Sell cargo first or assign larger fleet" |
| `MARKET_GONE` | Target market no longer exists | "Edit route to different market" |
| `ROUTE_HOSTILE` | Lane security is hostile (doctrine blocks) | "Wait for security improvement or raise risk tolerance" |
| `FLEET_DESTROYED` | Assigned fleet is dead | "Assign new fleet or cancel program" |
| `FLEET_BUSY` | Fleet is in manual override | "Release manual control to resume automation" |
| `EMBARGO_ACTIVE` | Faction embargo prevents trade at destination | "Wait for embargo to lift or reroute" |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Opaque AI decisions** | Victoria 3 Autonomous Investment | Every automated action has a visible reason chain |
| **All-or-nothing automation** | Distant Worlds 2 | Granular per-program control, never per-system on/off |
| **Override requires N clicks per entity** | Distant Worlds 2 (160 clicks for 40 ships) | One-click pause/cancel, batch operations for groups |
| **Silent failure** | Any game where automation "just stops" | Stall/Fail states with reason codes and suggested fixes |
| **Auto-created programs** | Any game that automates without consent | Programs always start Paused, player explicitly starts |
| **No profitability tracking** | Games where "is this trade route worth it?" requires mental math | Per-program P&L with lifetime totals |

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Transparent automation | Factorio | Player builds automation from deterministic primitives — you can trace any behavior |
| Program status display | RimWorld work priorities | Single-number priority visible in a grid, override is one drag |
| Failure feedback | Factorio inserter/belt warnings | Specific icon + tooltip showing exactly what's blocked and why |
| Pre-flight analysis | EVE Online fitting simulator | Show consequences before committing (our ProgramQuote) |
| Doctrine/policy layer | Distant Worlds 2 (concept) | Global rules that govern automation behavior (DW2 executes poorly but the idea is right) |
| Batch operations | Stellaris sector management | Select multiple, apply policy once |

---

## SimBridge Queries — Existing and Needed

### Existing
| Query | Purpose |
|-------|---------|
| `CreateTradeCharterProgram(...)` | Create new charter |
| `CreateAutoBuyProgram(...)` / `CreateAutoSellProgram(...)` | Create auto-trade |
| `CreateExpeditionProgram(...)` | Create exploration program |
| `CreateResourceTapProgram(...)` | Create extraction program |
| `StartProgram(id)` / `PauseProgram(id)` / `CancelProgram(id)` | Lifecycle control |
| `GetProgramExplainSnapshot()` | All programs with status metadata |
| `GetProgramQuote(id)` | Pre-flight pricing and constraints |
| `GetProgramOutcome(id)` | Last execution result |
| `GetProgramEventLogSnapshot(id, max)` | Event history |

### Needed
| Query | Purpose |
|-------|---------|
| `GetProgramProfitabilityV0(id)` | Lifetime revenue, costs, net P&L |
| `BatchPauseProgramsV0(kind)` | Pause all programs of a given type |
| `GetDoctrineV0()` / `SetDoctrineV0(...)` | Read/write doctrine policy |
| `GetProgramSuggestionsV0(nodeId)` | Contextual program suggestions from intel data |
| `GetStallReasonV0(id)` | Specific failure code + suggested action string |
