\# Space Trade Empire

\## Development Lock Recommendations (Canonical)



\### Status

Authoritative summary for memorialization and engineering execution, optimized for LLM-driven implementation and small vertical slices. This document is canonical.



See also:

\- `docs/50\_EARLY\_MISSION\_LADDER\_AND\_TRAVEL.md` (early mission ladder and travel guardrails)

\- `docs/51\_ECONOMY\_AND\_TRADE\_DESIGN\_LAW.md` (economy causality, ledger law, automation doctrine, invariants)

\- `docs/53\_PROGRAMS\_FLEETS\_DOCTRINES\_CONTROL\_SURFACE.md` (Programs/Fleets/Doctrines control surface, Liaison Quote contract)

\- `docs/20\_TESTING\_AND\_DETERMINISM.md` (test philosophy and harness expectations)

\- `docs/10\_ARCHITECTURE\_INDEX.md` (use ARCH excerpts for SimCore/GameShell boundaries and runner patterns)



Canonical consistency rule:

\- If any chat instruction or implementation decision conflicts with the locks in this document, update the canonical doc first and align dependents. Do not allow two conflicting “truths” to coexist across canonical docs.



---



\## A. Time model and pacing targets

\### A1. Core time contract (LOCKED)

\- Time is continuous in presentation (movement, combat visuals, docking, UI animation).

\- Simulation is tick-based and authoritative.



Locked constants:

\- Tick size: 1 tick = 1 game minute.

\- Time scale: 1 real second = 1 game minute (60x).

\- No acceleration modes. No cruise scaling. No fast-forward.



Pause behavior:

\- Menus/Escape pause freezes sim time.

\- UI may queue intents; they apply on the next tick after unpause.



\### A2. Tick boundary rule (authoritative state changes)

Only these change SimCore state, and only on tick boundaries:



\- Inventory deltas (production, consumption, transfers)

\- Trade execution and credit changes

\- Price publishing updates

\- Contract progression

\- Repair, maintenance, wear, buffer drains

\- Tech enablement, sustainment checks, degradation

\- NPC background logistics flow

\- Any “world clock” events (rumors, faction actions, etc.)



Rule:

\- Continuous systems (travel/combat/docking) are GameShell-only and must not directly mutate authoritative state.

\- Continuous systems post intents that are applied on the next tick.



---



\## B. Travel timing (LOCKED)

\### B1. Real-time travel feel

Typical in-system hop target:

\- 20 to 60 seconds real time (default target 35 seconds).



Ranges:

\- Short hop: 15 to 25 seconds.

\- Long hop: 60 to 90 seconds (rare).



\### B2. What this means in game time (given 60x)

Given 60x:

\- 35 real seconds = 35 game minutes.

\- 60 real seconds = 60 game minutes = 1 game hour.



Design implication:

\- The universe calendar advances quickly.

\- Long-horizon systems must be expressed in days and weeks of game time (not hours) to avoid real-time thrash and constant re-planning.



---



\## C. Simulation architecture contract

\### C1. SimCore responsibilities

\- Deterministic tick stepping.

\- Canonical world state.

\- Intent application on tick boundaries.

\- Save/load of the entire canonical state.

\- World hash for regression tests.



\### C2. GameShell responsibilities

\- Continuous presentation and input.

\- UI reads sim snapshots.

\- Issues intents (trade orders, routing settings, tech toggles, fleet policies).

\- No direct writes to sim state.



\### C3. Intent model (required)

\- Intents are timestamped to apply at the next tick.

\- Intents are validated in SimCore (bounds checking, legality, permissions).

\- Intent application is deterministic and order-defined (stable ordering rules).



\#### C3.1 Intent ordering lock (required for deterministic replay)

Intent application order must be stable and defined.



Canonical ordering:

\- Primary sort: `apply\_tick` (ascending)

\- Secondary sort: `intent\_kind\_priority` (ascending, fixed table)

\- Tertiary sort: `actor\_id` (ascending)

\- Quaternary sort: `insertion\_index` (ascending, deterministic at creation time)



Rules:

\- The `intent\_kind\_priority` table is canonical and versioned in code (not data-driven at runtime).

\- Adding a new intent kind requires assigning a priority and adding/adjusting golden tests.


`actor_id` definition (LOCKED):
- For player-originated intents: the stable PlayerId (or the issuing ProgramId if intents are program-scoped).
- For program-generated intents: `Program:<ProgramId>`.
- For faction/system intents: `Faction:<FactionId>` or `System`.
Rule:
- actor_id must be stable, explicit, and serializable. Never generate random GUIDs for actor_id.



---



\## D. Economy and market model

\### D1. Physical goods (LOCKED)

\- Goods exist as integer units.

\- Each good has `unit\_mass` and `unit\_volume`.

\- Cargo capacity is constrained by mass and volume.



\### D2. Stations have inventories (LOCKED)

\- Stations produce/consume goods each tick.

\- Stations can hit scarcity and empty out.

\- Logistics physically moves goods between stations.



\### D3. Price model (recommended default for this game)

Use inventory-based pricing, not order books, at least through Slice 1 and 2.



Each station-good has:

\- base price

\- stock bands (min, ideal, max)

\- price curve based on stock ratio

\- buy/sell spread



\#### D3.1 Price publishing cadence (LOCKED guidance for 60x)

Because time scale is 60x, do not publish prices “hourly” in game time.



Canonical publishing rule:

\- Underlying scarcity state may update every tick.

\- Published prices update on cadence, default: every 12 game hours.

\- At 60x, 12 game hours is 12 real minutes.

\- Published prices are smoothed (exponential smoothing) to prevent thrash.



\### D4. Background transport (recommended)

World should not feel dead. Use 2 tiers:



1\) Abstract lane flow:

\- per lane capacity and delay

\- net goods movement per tick without simulating every ship



2\) Physical entities:

\- player freighters and select important convoys exist as actual ships when they matter to gameplay



Determinism requirement:

\- Abstract lane flow must be deterministic (pure function of state + seed) and must not depend on entity spawn order or frame timing.



---



\## E. Logistics spine (LOCKED priority)

Logistics is the backbone for trading, construction, repairs, tech sustainment, faction war supply, and station growth.



Minimum lane model:

\- capacity (units per tick, or per hour expressed in per-tick increments)

\- delay (ticks of travel time)

\- optional risk (loss/interdiction events, deterministic/seeded)



Rule:

\- All downstream systems must consume from inventories and respect logistics lead time.



---



\## F. Information model (player signals)

\### F1. Local truth (LOCKED)

When docked or with a direct local link:

\- show exact inventory and exact prices



\### F2. Remote intel (recommended default)

Remote view defaults to:

\- inventory band (`Empty`, `Low`, `OK`, `High`, `Full`)

\- published price

\- intel age timestamp



Exact remote data requires one of:

\- owned asset in-system (agent/sat/outpost)

\- paid market subscription

\- recent visit



Rule:

\- This creates meaningful scouting and infrastructure loops without pure fog-of-war frustration.



---



\## G. Automation philosophy (ties into development)

Automation exists to let players operate an empire, not to print money.



Player advantage is:

\- better information networks

\- better routing policies

\- better tech enablement choices

\- better risk posture (turtle mode vs profit mode)



Automation must be constrained by:

\- limited intel freshness

\- logistics bottlenecks

\- spreads, fees, and slippage

\- risk events and disruptions



---



\## H. “Turtle mode” (must exist for recovery)

Define a recoverable failure state that is not death.



Baseline turtle policy effects (initial default):

\- lower signature and engagement likelihood

\- lower speed and lower profit rate

\- prioritizes repairs, resupply, and safe routing

\- preserves capital and avoids spiral failures



Rule:

\- Turtle mode must be a first-class doctrine/policy with explicit costs and clear postmortems (opportunity cost, slower throughput).



---



\## I. Testing and determinism (non-negotiable)

\### I1. Golden path invariants

Create a deterministic “first 5 minutes” runner and assert invariants.



Given 60x:

\- “first 5 minutes” means 5 real minutes = 5 game hours.



Golden invariants (minimum):

\- no negative inventory

\- no NaNs

\- valid IDs

\- credits within expected bounds

\- expected location sequence

\- tutorial flags set once



\### I2. World hash regression

\- Headless sim runs N ticks and produces a world hash.

\- Save/load roundtrip must reproduce the same hash at the same tick.



\### I3. System boundaries

\- SimCore is pure logic; no rendering, no frame timing dependence.

\- GameShell cannot mutate sim state directly.



---



\## J. Immediate development slice (next work item)

\### Slice 1: Logistics + Market + Intel micro-world

Minimum content:

\- 2 stations, 1 lane, 2 goods

\- station inventories, production/consumption

\- abstract lane flow with delay

\- inventory-based price model with buy/sell spread

\- published price cadence every 12 game hours (12 real minutes)

\- intel model: exact local, banded remote with intel age

\- one UI panel showing inventory, price, intel age, and a buy/sell action via intents

\- headless deterministic test: run 10,000 ticks and assert world hash stability



Gate tests:

\- world hash stability test

\- save/load hash equality test

\- invariants test (no invalid states)



\### Slice 1.5: Tech sustainment via supply chain

Add:

\- 1 tech requiring 2 goods per tick to remain enabled

\- buffers sized in days of game time

\- degradation rules when supply drops

\- UI shows sustainment margin and time-to-failure



Gate tests:

\- sustainment degradation determinism test

\- buffer math invariants test



---



\## K. Open questions to resolve next (ranked)

1\) Combat resolution abstraction:

\- purely continuous with tick-applied outcomes, or partial tick-based resolution for strategic fights?



2\) Lane risk model:

\- when and how interdiction triggers, and what variables influence it.



3\) Goods list v0:

\- the first 6 to 12 goods to validate cargo sizing, price curves, and scarcity.



4\) Construction program interface:

\- how stations request goods and how lead times are surfaced to the player.



