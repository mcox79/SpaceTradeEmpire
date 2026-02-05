\# Space Trade Empire

\## Economy and Trade Design Law (Implementation-Oriented, Canonical)



\### Status

Canonical design law for the economy, markets, logistics, automation doctrine, enforcement, information fog, determinism, and regression gating. Intended to drive SimCore implementation, tooling, and content authoring.



See also:

\- `docs/52\_DEVELOPMENT\_LOCK\_RECOMMENDATIONS.md` (time contract: tick = 1 game minute, 60x mapping, published price cadence guidance, logistics spine locks)

\- `docs/53\_PROGRAMS\_FLEETS\_DOCTRINES\_CONTROL\_SURFACE.md` (Programs/Fleets/Doctrines control surface, Liaison Quote contract, explain JSON)

\- `docs/20\_TESTING\_AND\_DETERMINISM.md` (test philosophy, harnesses, golden runs)

\- `docs/30\_CONNECTIVITY\_AND\_INTERFACES.md` (module boundaries and interfaces)



---



\## 0. Non-Goals

\- No timed missions. The game will not require “deliver X by date” gameplay to succeed.

\- No infinite money loops. Any strategy that scales fleet volume must encounter binding constraints that are legible to the player.

\- No fake shocks. World events may change world state, but never directly set prices.



---



\## 1. Economic Causality Loop (Must Be Closed)

All major price movement must be explainable via a causal chain that exists in-world.



\### 1.1 Actors and nodes

Nodes:

\- Producers: extractors (raw), farms (organics), fuel plants (fuel)

\- Processors: refineries, smelters, chemical plants

\- Manufacturers: components, ship parts, consumer goods

\- Consumers: stations (population), fleets, warfront, construction projects

\- Logistics: ports, lanes, convoys, storage

\- Services: labor pools, maintenance yards, insurers, financiers (represented as stateful actors, not UI-only concepts)



\### 1.2 Flows

Flows:

\- Production creates inventory at nodes

\- Logistics moves inventory with losses and delays

\- Consumption depletes inventory

\- Inventory levels determine prices and spreads

\- Investment changes capacity at nodes or in logistics

\- Capacity changes future production and transport costs

\- Service availability (labor, maintenance, insurance) modulates throughput, risk, and downtime



\### 1.3 Shock rule

Rule:

\- No “random shock” may change prices directly.

\- Shocks change state (capacity offline, piracy capacity up, tariff schedule change, lane closure, labor strike).

\- Price changes are a consequence of state.



---



\## 2. Ledger and Accounting Law (No Ghost Money)

All money and inventory movement must be explainable and auditable.

### Ledger event payload minimums (LOCKED)

CashDelta minimum fields:
- EventId
- TickIndex
- ActorId (ProgramId | FleetId | FactionId | JurisdictionId | System)
- Category (one of the canonical categories)
- AmountCredits (signed; positive = inflow, negative = outflow)
- CounterpartyId (optional, stable ID if known)
- RelatedIds (optional: ContractId, QuoteId, TradeFillId, InspectionEventId)
- ReasonCode (required for non-TradePnL deltas)

InventoryDelta minimum fields:
- EventId
- TickIndex
- ActorId (ProgramId | FleetId | StationId | System)
- StationId (required unless the delta is explicitly in-transit state with its own stable holder)
- GoodId
- QuantityUnits (signed; positive = inventory added, negative = removed)
- Reason (one of the canonical movement reasons)
- RelatedIds (optional: ContractId, QuoteId, TradeFillId, LossEventId)
- ReasonCode (required for Confiscated/Stolen/Leaked/Spoiled)

Rule:
- No module may change credits or inventory without emitting one of these events with the minimum fields populated.


\### 2.1 Cash deltas

Requirement:

\- Every credit change must be represented as a `CashDelta` event with exactly one ledger category:

&nbsp; - `TradePnL`

&nbsp; - `Fees`

&nbsp; - `InsurancePremium`

&nbsp; - `InsurancePayout`

&nbsp; - `Maintenance`

&nbsp; - `Payroll`

&nbsp; - `FinancingInterest`

&nbsp; - `FinancingPrincipal`

&nbsp; - `Tariffs`

&nbsp; - `Fines`

&nbsp; - `ConfiscationLoss`

&nbsp; - `TheftLoss`

&nbsp; - `FacilityOPEX`

&nbsp; - `ContractSettlement`



\### 2.2 Inventory deltas

Requirement:

\- Every inventory movement must be represented as an `InventoryDelta` event with exactly one movement reason:

&nbsp; - `Produced`

&nbsp; - `Consumed`

&nbsp; - `Loaded`

&nbsp; - `Unloaded`

&nbsp; - `Spoiled`

&nbsp; - `Leaked`

&nbsp; - `Stolen`

&nbsp; - `Confiscated`

&nbsp; - `Manufactured`

&nbsp; - `Reprocessed`



\### 2.3 Mutation prohibition

Rules:

\- No module may directly mutate credits or inventories without emitting the corresponding event.

\- Any “partial fill,” “interrupted transfer,” or “confiscation” must resolve into a consistent set of events that balances.



Answerability requirement:

\- If a player asks “where did the $ go,” the answer must be reconstructible from events, not inferred after the fact.



---



\## 3. Goods Model and Economic Roles (LOCKED)

This section aligns the economy law with the physical goods locks and the “no chore fuel” policy.



\### 3.1 Physical units

Locked:

\- Goods exist as integer units.

\- Each good declares `unit\_mass` and `unit\_volume`.

\- Cargo capacity is constrained by both mass and volume.



\### 3.2 Handling and decay

Every commodity must declare:

\- Handling class: `Bulk`, `Container`, `Liquid`, `Hazardous`, `Refrigerated`

\- Handling requirements: port equipment tier, storage type, legal restrictions

\- Decay model: none, spoilage, leakage, theft shrink (with declared parameters)

\- Substitution group: demand can switch within the group when relative prices change

\- Economic role (drives world function and explanation):

&nbsp; - Essential (fuel, food, spare parts): shortage degrades station/port function and raises risk

&nbsp; - Strategic (military/industrial inputs): couples strongly to warfront and faction policy

&nbsp; - Luxury (non-essential): affects reputation, soft power, and margins but not survival



\### 3.3 Minimum substitution

Minimum substitution requirements:

\- At least 3 substitution groups that matter economically (example: structural materials, fuels, industrial inputs).

\- Substitution must be partial, not perfect.



\### 3.4 Fuel policy (canonical resolution)

Fuel exists as a simulated good and an economic/logistics constraint.

\- Player-facing “refuel busywork” is prohibited in early slices.

\- Fuel constraints, if surfaced to the player, are expressed as service costs, readiness impacts, routing constraints, or Program/Fleet doctrine, not manual per-ship refuel steps.



---



\## 4. Logistics is the Primary Constraint (Throughput, Queueing, Handling)

Transport is not “time only.” It is throughput, queueing, handling, and services.



\### 4.1 Hard constraints

Hard constraints enforced everywhere:

\- Berth slots and service time (queues)

\- Loading/unloading throughput per port and per ship or abstract flow unit

\- Storage capacity per commodity class and storage type

\- Fuel consumption and refuel availability (simulated constraint, not chore UI)

\- Maintenance readiness (utilization increases wear and failure probability)

\- Service availability:

&nbsp; - Skilled labor affects docking service time, maintenance turnaround, and manufacturing throughput

&nbsp; - Frontier regions can be labor-limited, not just cargo-limited



\### 4.2 Scaling bound rule

Rule:

\- Any profitable loop must be bounded by at least 3 hard constraints at scale (typically berth queues, spread/depth, and fuel or maintenance or labor).



---



\## 5. Market Structure (Spot + Contracts Without Deadlines)

The economy supports two market modes.



\### 5.1 Spot

Spot:

\- Immediate buy/sell at a bid/ask spread

\- Spread widens with volatility, low depth, low trust, and frontier risk



\### 5.2 Contracts (no deadlines)

Contracts (no deadlines):

\- Procurement contracts: deliver X units eventually, paid on fulfillment, price formula defined up front

\- Freight contracts: paid per unit moved, shipper owns goods (reduces player capital lockup)

\- Capacity reservation: pay for priority berth/convoy slot (reduces queue time)

\- Standing supply agreements: continuous flow with adjustable quantity bands



Contract stakes without deadlines:

\- Capital lockup (escrow, margin, reserves)

\- Opportunity cost (inventory committed cannot be redeployed)

\- Reputation and access (breach reduces trust and future terms)

\- Covenant pressure (financing triggers based on liquidity and performance)



Rule:

\- Contracts cannot require timers to create stakes. Stakes come from capital structure, reputation, access, and risk.



Implementation note (anti-stall without timers):

\- Prolonged non-performance may worsen terms, consume escrow, or reduce access, but must not be expressed as a hidden countdown fail.



---



\## 6. Price Formation (Depth, Spreads, Bands, and Hysteresis)

Each station-market for each good has:

\- Inventory

\- Target band (desired inventory range)

\- Depth (volume that can move before marginal price impact becomes severe)

\- Spread model inputs (inventory risk + volatility + trust + heat)



\### 6.1 Requirements

Price model requirements:

\- Prices move with deviation from target band

\- Depth determines marginal impact of trade volume on price

\- Spread increases with volatility, low trust, and inspection heat

\- Substitution shifts demand as relative prices diverge

\- NPC arbitrage exists by default, reducing trivial cross-market alpha



\### 6.2 Published price cadence (LOCKED to time model)

Because time scale is 60x (1 real second = 1 game minute), published price updates must avoid real-time thrash.

Published price is a derived output (LOCKED):
- Published prices are an informational and quoting surface.
- Ledger calculations, contract settlement, and inventory valuation must not read back from published price as a source of truth.
- Trades emit TradeFill with explicit executed unit price and quantity; ledgers derive from fills, not from published price snapshots.


Canonical rule:

\- Underlying scarcity state may update every tick.

\- Published prices update on a cadence, default: every 12 game hours.

\- Prices are smoothed (exponential smoothing) across publish updates.



This makes prices legible without acceleration modes.



\### 6.3 Hysteresis (anti-snap behavior)

Hysteresis requirements:

\- Producers/processors/manufacturers have ramp rates and minimum run behavior

\- Ports have service backlogs that take time to clear

\- Some prices can be sticky via contracts and regulation (slow adjustment bands)



Rule:

\- The economy must exhibit path dependence. It must not snap to equilibrium immediately after a shock ends.



---



\## 7. Competition and Saturation (NPC Actors are Real and Bounded)

NPC traders run the same rules as the player:

\- Allocate capital, choose routes, respond to margins

\- Compete for throughput and contracts

\- Suffer the same information limits, travel limits, and service constraints



NPC bounds:

\- NPCs have capital pools and risk tolerances

\- NPCs have home regions and cannot teleport

\- NPC information freshness and coverage are limited and costed (same model as player)

\- NPC volume is limited by logistics and services



Rule:

\- If the player discovers a high-margin lane, NPC volume follows unless the player has a defensible moat (permits, relationships, infrastructure, intelligence).



Implementation guidance (throttling, required):

\- NPC response has reaction time and volume limits tied to intel freshness, distance, capital, and berth queues.

\- NPC arbitrage reduces trivial alpha but cannot erase all opportunity instantly.



---



\## 8. Player Advantage (Earned, Narrow, Defensible)

The player’s advantage is not “always better prices.” It is access and capability:

\- Information coverage and freshness (intel network)

\- Permits and jurisdictional privileges (reduced tariffs, faster clearance)

\- Relationships (better spreads, priority berths, better credit terms)

\- Infrastructure (warehouses, service depots, convoy escorts, maintenance yards)

\- Doctrine control (risk, liquidity, exposure caps, strategic stockpiles)



Cost and counterforce requirement:

\- Every advantage must carry financing cost, OPEX, political cost, or maintenance burden

\- Every advantage must have a counterforce (competition, enforcement scrutiny, sabotage, regulatory shifts)



Rule:

\- Player advantage must be explainable as “this is what you built,” not “the game likes you.”



---



\## 9. Automation is Policy Execution, Not Alpha Creation

Automation runs Programs that are constrained optimizers.



\### 9.1 Program fields (required set)

Program fields (minimum):

\- Commodity whitelist/blacklist by class and legality

\- Risk band: piracy threshold, inspection heat threshold

\- Liquidity reserve: keep $X or X days burn in cash

\- Exposure caps: per commodity, per route, per jurisdiction

\- Objective weights: Profit, Stability, Reputation, Strategic Stockpile

\- Capital allocation: max $ at risk per ship/fleet and per Program

\- Rebalance rules: reposition empty only under explicit conditions

\- Procurement vs freight preference (capital-light vs margin-seeking behavior)



\### 9.2 Execution penalties (must exist)

Execution penalties (must exist):

\- Imperfect information unless paid for (freshness and coverage constraints)

\- Worse fills when trust is low or heat is high

\- Queueing friction without reservations or relationships

\- Self-impact: high volume worsens your own future prices via depth limits

\- Operational overhead: maintaining intel, relationships, and infrastructure consumes budget and attention



\### 9.3 Failure taxonomy (required for explainability)

Automation failure taxonomy (exactly 1 primary cause, optional secondary causes):

\- `BadInfo` (stale or low-confidence data)

\- `Slippage` (depth impact and spread widening)

\- `Queueing` (berth or service backlog)

\- `Heat` (inspection escalation, confiscation risk)

\- `LossEvent` (piracy, spoilage, leakage, theft)

\- `CapitalLockup` (escrow/margin/deductible constraints)

\- `ServiceShortage` (labor/parts unavailability)



Rule:

\- Every negative outcome must attribute exactly 1 primary failure cause from the taxonomy, with optional secondary causes.



Instrumentation requirement:

\- The decision-time facts that justify the label must be stored at decision time (not reconstructed later).



---



\## 10. Money Sinks That Feel Like Empire Operations (Structural, Scaling)

Required structural sinks:

\- Crew payroll, training, retention

\- Maintenance and spare parts supply chain

\- Insurance premiums and deductibles (route and loss-history dependent)

\- Facility OPEX: warehouses, agents, service depots, intel operations

\- Financing costs: interest, covenants, margin requirements

\- Depreciation and replacement cycles for ships and facilities



Rule:

\- Sinks must scale with utilization and footprint, not as arbitrary taxes.



---



\## 11. Law, Enforcement, and Heat (Systemic, Not RNG)

Each jurisdiction defines:

\- Enforcement intensity and doctrine

\- Contraband categories

\- Inspection triggers (pattern recognition: volumes, routes, counterparties, repeat behavior)

\- Penalties (confiscation, fines, license suspensions, access bans)



\### 11.1 Heat model

Heat model:

\- Heat increases with suspicious patterns, contraband volume, and incident history

\- Heat decays with time and compliance

\- Relationships and compliance spend can reduce heat growth, never eliminate it



\### 11.2 Manipulation and counterplay

Market manipulation and counterplay:

\- Pump/dump and spoofing behavior must be detectable by enforcement systems

\- Liquidity providers respond by widening spreads, lowering depth, or refusing counterparties

\- Repeated manipulation increases heat and reduces trust



Rule:

\- Illegal profit exists but is constrained by heat, confiscation risk, and long-term access loss.

\- Manipulation is a strategy with systemic consequences, not a free exploit.



---



\## 12. Security is State (Not Just a Scalar)

Security must be represented as stateful capacity:

\- Pirate capacity and behavior by region (not just “chance”)

\- Patrol/convoy capacity by jurisdiction

\- Insurance terms and exclusions depend on route security state

\- Player investment (escorts, intelligence, diplomacy) changes security state



Rule:

\- Risk must feel causal. Loss events may be stochastic, but their probability is driven by visible security state.



---



\## 13. Information Economy (Fog of Market)

Market visibility is not global.



\- Data has freshness, coverage radius, and reliability

\- Intel costs money and infrastructure (agents, subscriptions, sensors)

\- Rumors exist with confidence levels and can be manipulated by factions



Rule:

\- The “alive universe” experience comes from learning why things change, not from random price noise.



---



\## 14. Pressure Without Timed Missions (Endogenous and Exogenous)

The game must create urgency without mission timers.



Endogenous pressure sources:

\- Payroll cadence and retention risk

\- Maintenance cycles and parts shortages

\- Insurance renewals and premium increases after incidents

\- Financing covenants and liquidity thresholds

\- Heat decay windows (opportunity cost to lie low vs keep operating)

\- Facility downtime and staffing churn



Exogenous pressure sources:

\- Warfront intensity shifts

\- Faction policy and tariff regime changes

\- Lane closures and security state deterioration

\- Production outages and service strikes



Rule:

\- Pressure comes from interacting systems, not from countdown clocks.



---



\## 15. UX Requirements (Dashboard-First, Minutes-Per-Loop)

Every economic outcome must be explainable with a postmortem.



\### 15.1 Decision-time facts (stored)

Automation must emit decision-time facts (stored, not reconstructed):

\- Profit breakdown: spread, fees, fuel/service cost, queue time, losses, insurance, financing cost

\- Binding constraint: what capped returns (depth, berth, heat, fuel/service, maintenance, labor)

\- Causal chain: which world-state changes drove the result

\- Recommended doctrine delta: smallest setting change that would likely have prevented a loss



Rule:

\- No hidden dice. Randomness is allowed only when attributed to explicit risk factors and shown.



\### 15.2 Published price legibility

Legibility rule:

\- Scarcity bands and key state indicators may update every tick.

\- Published prices update on cadence (default 12 game hours) and are smoothed.

\- UI must show both: current scarcity band and published price age.



---



\## 16. Determinism, Harness, and Invariants (SimCore)

Economy sim must be reproducible.



\### 16.1 Seeded randomness

\- Seeded PRNG for events and risk sampling.



\### 16.2 RNG stream partitioning law

Each subsystem uses a dedicated RNG stream key:

\- `Pricing`

\- `Piracy`

\- `Enforcement`

\- `Rumors`

\- `NPCDecisions`

\- `FacilityFailures`

\- `WeatherOrHazards` (if used)



Rules:

\- Collection iteration order must be stable and explicitly sorted.

\- Adding logging must not change outcomes.



\### 16.3 Abstract lane flow determinism (required)

If the world uses “abstract lane flow” (aggregate transport rather than ship-per-ship), it must be deterministic:

\- The aggregate shipped amount per tick is a pure function of:

&nbsp; - lane capacity

&nbsp; - backlog queues

&nbsp; - station inventories and reservation rules

&nbsp; - policy/contract allocations

&nbsp; - deterministic loss sampling (seeded, stream-keyed)

\- It must not depend on runtime entity spawn order, frame timing, or UI.



Physical entities (player freighters and selected convoys) may exist, but the abstract tier must remain a deterministic aggregate.



\### 16.4 Invariants (build/test gate)

Invariants (build/test gate):

\- No negative inventories

\- Conservation checks on flows (accounting for declared shrink/decay)

\- Ledger integrity: sum of `CashDelta` entries matches wallet changes, no unclassified deltas

\- Bounded inflation regime (define acceptable range)

\- Regression test: no doctrine preset yields unbounded exponential credit growth over long horizons



Rule:

\- If a change breaks invariants or introduces a money printer, it fails the build/test gate.



---



\## 17. LLM-Friendly Development Laws (Prevent Spec Drift)

Schema-first, event-sourced core.



\### 17.1 Required immutable event types

Define immutable event types for:

\- `InventoryDelta`

\- `CashDelta`

\- `TradeFill`

\- `PriceQuote`

\- `ContractStateChange`

\- `QueueEvent`

\- `LossEvent`

\- `InspectionEvent`

\- `SecurityStateChange`

\- `ServiceAvailabilityChange`



Rule:

\- Simulation state is a pure fold over events.



\### 17.2 Strict module boundaries

Strict module boundaries:

\- Pricing reads only pricing inputs (inventory, depth, volatility index, trust score, heat score as a scalar). Pricing never reads enforcement internals.

\- Enforcement writes heat and confiscation events, never prices.

\- Security writes security state changes, never prices.

\- UI explanations cite stored decision-time fields from events.



\### 17.3 Golden-run regression suite (required)

Fixed seed scenario pack, outputs metrics JSON:

\- inflation index by region

\- volatility by commodity and role

\- average spread by risk band and trust band

\- utilization and queue times

\- loss rates by cause

\- top profit sources by ledger category



CI fails if metrics drift beyond tolerances.



\### 17.4 Scenario packs (required)

Scenario packs (required):

\- Calm core region economy

\- Frontier piracy spiral

\- Major refinery outage (fuel shock via capacity loss)

\- Tariff regime shift across a border

\- Labor strike at a key port (service availability shock)



Rule:

\- Any LLM-generated change must be evaluated by replaying scenario packs and passing invariants before merge.



