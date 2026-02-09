# Terms, Units, Invariants, and IDs (Canonical)

This file consolidates:
- Units and invariants doc (archived)
- Glossary and IDs doc (archived)

## A. Units and Invariants (Historical snapshot)

\# Space Trade Empire

\## Units and Invariants (Canonical)

\### Status

Canonical units, normalization rules, invariants, and validator expectations shared by SimCore, UI, tests, and tooling. This document exists to prevent silent unit mismatches and to enable deterministic regression gating.

See also:

\- `docs/52\_DEVELOPMENT\_LOCK\_RECOMMENDATIONS.md` (time contract and tick-boundary state changes)

\- `docs/51\_ECONOMY\_AND\_TRADE\_DESIGN\_LAW.md` (ledger law, market mechanics, invariants)

\- `docs/53\_PROGRAMS\_FLEETS\_DOCTRINES\_CONTROL\_SURFACE.md` (Programs/Fleets/Quotes/Explain, KPI fields)

\- `docs/20\_TESTING\_AND\_DETERMINISM.md` (test gates and runners)

Canonical consistency rule:

\- If any system or doc uses a conflicting unit or meaning, update the canonical doc first and align dependents. Do not allow divergent unit systems to coexist.

---

\## 1. Time units (LOCKED)

\### 1.1 Tick

Locked:

\- 1 tick = 1 game minute.

\### 1.2 Time scale

Locked:

\- 1 real second = 1 game minute (60x).

\### 1.3 Derived units

Derived units must be computed deterministically:

\- 1 game hour = 60 ticks

\- 1 game day = 1,440 ticks

\- 1 game week = 10,080 ticks

Rule:

\- All “per day” rates are per game day (1,440 ticks), not per real-time day.

---

\## 2. Currency and accounting units

\### 2.1 Credits

Lock requirement:

\- Choose one representation for credits in SimCore and keep it consistent.

Recommended:

\- Use integer credits in SimCore (no fractional cents) to avoid floating point drift.

\- If fractional pricing is desired in UI, represent it as integer “millicredits” or as fixed-point.

Rule:

\- No floating point math is allowed for authoritative credit balances.

\### 2.2 Ledger event consistency

Locked:

\- Every credit mutation must be represented as exactly one `CashDelta` event with exactly one category.

\- Wallet changes must equal the sum of `CashDelta` entries over the interval.

---

\## 3. Goods, cargo, and physical units (LOCKED)

\### 3.1 Goods units

Locked:

\- Goods exist as integer units.

\### 3.2 Mass and volume units

Lock requirement:

\- Choose one canonical unit system for mass and volume.

Recommended:

\- `unit\_mass` in kilograms (kg)

\- `unit\_volume` in cubic meters (m3)

Rule:

\- Cargo capacity constraints must consider both:

&nbsp; - mass capacity (kg)

&nbsp; - volume capacity (m3)

\### 3.3 Conversions

Rule:

\- Conversions must be centralized and deterministic.

\- No ad hoc conversions in UI or per-module logic.

---

\## 4. Rates and normalization rules

\### 4.1 Cost rates

Locked:

\- Unless explicitly stated otherwise, operating costs, burn rates, and OPEX are expressed as credits per game day.

Normalization:

\- If any system computes a per-tick cost, it must derive it from per-day values:

&nbsp; - `cost\_per\_tick = cost\_per\_day / 1,440`

\- If using integer math, use fixed-point or remainder accumulation (deterministic) rather than float division.

\### 4.2 Throughput rates

Locked:

\- “Throughput” defaults to `WorkUnitsPerDay` (WUD) for abstract fleets and ports.

\- Lane capacity must be representable as a per-tick increment derived from per-day capacity.

Normalization:

\- `work\_units\_per\_tick = work\_units\_per\_day / 1,440` (fixed-point or remainder accumulation)

\### 4.3 Risk units

Locked:

\- Risk is represented on a 0–100 scale.

Rule:

\- Any “risk ceiling” or “risk posture” uses the same 0–100 scale.

\### 4.4 Probability bands

Locked:

\- `p10`, `p50`, `p90` represent the 10th, 50th, and 90th percentile outcomes across a declared stochastic model.

Rule:

\- The distribution model used to compute bands must be stable and versioned.

\- If bands are approximated, the approximation method must be consistent and deterministic.

---

\## 5. Integer math and determinism rules

\### 5.1 Floating point prohibition for authoritative state

Locked:

\- Authoritative SimCore state must not depend on floating point operations that can drift across platforms.

Allowed:

\- Fixed-point arithmetic (integer with implied scale)

\- Deterministic rounding rules

\- Explicit remainder carry-forward

\### 5.2 Ordering stability

Locked:

\- Any iteration over collections that can affect outcomes must be explicitly sorted by stable IDs.

Rule:

\- Adding logging must not change outcomes.

\- Changes that alter iteration order must be treated as breaking changes and require golden-run updates.

---

\## 6. Invariants (build/test gates)

These invariants are required in tests and recommended as runtime assertions in debug builds.

\### 6.1 State validity invariants

\- No NaNs (should be impossible if floats are excluded from authoritative state)

\- No invalid IDs (every referenced ID must exist in registries)

\- No negative time, negative tick indices, or time regressions

\### 6.2 Inventory invariants

\- No negative inventories

\- Storage constraints respected (mass and volume)

\- Conservation checks across `InventoryDelta`:

&nbsp; - Any shrink/decay/spoilage must be represented as explicit deltas

&nbsp; - Transfers must balance: loaded/unloaded pairs reconcile to the same quantities over time

\### 6.3 Ledger invariants

\- Wallet change equals sum of `CashDelta` events

\- No unclassified credit changes

\- No negative wallet unless explicitly allowed by financing rules (and if allowed, it must be represented by financing events)

\### 6.4 Market invariants

\- Published price is never negative

\- Spread is non-negative and bounded by configured limits

\- Bounded inflation regime:

&nbsp; - Define acceptable drift ranges per scenario pack

&nbsp; - CI fails if inflation index exceeds thresholds

\### 6.5 “No money printer” invariant

\- Under fixed scenario packs, no baseline doctrine preset yields unbounded exponential credit growth over long horizons.

\- CI gate must include at least one “max-scale” run that asserts saturation behavior.

---

\## 7. Validator expectations (required)

\### 7.1 Runtime validators

Requirements:

\- A SimCore validator runs at least:

&nbsp; - after intent application

&nbsp; - after tick stepping

&nbsp; - on save

&nbsp; - on load

\- Validator produces deterministic error output (stable ordering of findings).

\### 7.2 Test harness validators

Requirements:

\- Headless harness must be able to:

&nbsp; - run N ticks

&nbsp; - emit a world hash

&nbsp; - round-trip save/load

&nbsp; - re-run and confirm identical hash at the same tick

---

\## 8. Metrics normalization for regression suites

Metrics produced by scenario packs must declare:

\- units for each metric (per day, per tick, absolute, index)

\- the aggregation window in ticks (start/end)

Rule:

\- Metrics output must be deterministic and stable across platforms.

\- Adding a new metric must not change existing metric values.

---

\## 9. Minimum schema fields that must declare units

Any schema field in these families must declare units in name or documentation:



\- costs (`\*\_per\_day`, `\*\_per\_tick`)

\- throughput (`\*\_work\_units\_per\_day`, `\*\_units\_per\_tick`)

\- time (`\*\_ticks`, `\*\_game\_hours`, `\*\_game\_days`)

\- risk (`\*\_risk\_0\_100`)

\- probability bands (`\*\_p10`, `\*\_p50`, `\*\_p90`)

\- mass and volume (`\*\_kg`, `\*\_m3`)

Rule:

\- If a field cannot state its unit clearly, it does not belong in v1.

## A. Units and Invariants (Historical snapshot)

# 90_GLOSSARY_AND_IDS

Canonical vocabulary and stable identifiers referenced across docs and code.

This file exists to prevent drift and ambiguous terminology.

Glossary precedence rule:
- If a term is used inconsistently across canonical docs or code, this glossary wins. Fix the conflicting doc/code to match the glossary.

See also:
- `docs/20_TESTING_AND_DETERMINISM.md`
- `docs/21_90_TERMS_UNITS_IDS.md`
- `docs/50_51_52_53_Docs_Combined.md`

## A. Workflow modes

OUTPUT_MODE

- Meaning: how the LLM must output changes in a coding session.
- Values:

  - ANALYSIS_ONLY: no code or file replacements; audit and plan only.
  - FULL_FILES: output complete file replacements (one file at a time).
  - POWERSHELL: output PowerShell-only atomic write blocks that generate or replace files.

- Rule: the session Context Packet must declare OUTPUT_MODE.

GIT_MODE

- Meaning: how Git actions are handled in the session workflow.
- Values:

  - NO_STAGE: do not stage or commit anything in-session.
  - STAGE: staging is allowed once validation passes.
  - COMMIT:<message>: commit is allowed once validation passes with the provided message.

- Rule: the session Context Packet must declare GIT_MODE.



Experimentation mode

- Meaning: exploratory changes and audits. Default Git mode is NO_STAGE. No commits.
- Switching rule: move to Normal mode only after the workflow is stable, outputs are deterministic, and the plan is explicit.



Normal mode

- Meaning: standard development, validation, and integration.
- Git mode: STAGE or COMMIT:<message>.



## B. Validation tiers



Tier 0

- Meaning: fast checks that should run whenever relevant files change.
- Typical examples:

  - `.gd` validation and parse gate (`scripts/tools/Validate-GodotScript.ps1`)
  - PowerShell parse checks (when `.ps1` changes)
  - `dotnet build` (when `.cs` changes)



Tier 1

- Meaning: correctness gates when wiring or simulation changes occur.
- Typical examples:

  - connectivity scan (`scripts/tools/Scan-Connectivity.ps1`)
  - `dotnet test` for relevant test projects (for example `SimCore.Tests/`)
  - smoke tests (when present)



Tier 2

- Meaning: slower runs intended for CI/nightly or intentional regression detection.
- Typical examples:

  - multi-seed determinism regressions
  - performance regressions
  - long-horizon scenario runs



## C. Session artifacts



Context Packet (generated)

- Path: `docs/generated/01_CONTEXT_PACKET.md`
- Meaning: default attachment for new LLM sessions.
- Must include:

  - objective
  - OUTPUT_MODE and GIT_MODE
  - explicit allowlist of files to modify
  - validation commands
  - definition of done



Context Packet template

- Path: `docs/templates/01_CONTEXT_PACKET.template.md`
- Meaning: the template used to generate the Context Packet.
- Rule: do not attach the template in sessions; attach only the generated packet.



Generated artifacts directory

- Path: `docs/generated/`
- Meaning: location for deterministic, diff-friendly tool outputs.



Connectivity scan outputs (v0)

- Paths:

  - `docs/generated/connectivity_manifest.json`
  - `docs/generated/connectivity_graph.json`
  - `docs/generated/connectivity_violations.json`

- Meaning:

  - manifest: tool and scope summary (top-level keys include tool, scope, counts, total_hits, files)
  - graph: nodes and edges (top-level keys include tool, nodes, edges)
  - violations: findings and rule list (top-level keys include tool, rules, violations, counts)

- Session rule: attach `connectivity_violations.json` only when non-empty or needed for diagnosis.



## D. Architecture layer terms



SimCore

- Meaning: headless simulation engine. Must not depend on Godot runtime objects or Godot namespaces.



GameShell

- Meaning: Godot-facing application layer. May depend on SimCore.



Adapter

- Meaning: glue layer allowed to touch both GameShell and SimCore. Used to bridge runtime and translate inputs/outputs.



## E. Determinism terms



Deterministic

- Meaning: repeated runs with the same deterministic inputs produce identical outputs (as defined by `docs/20_TESTING_AND_DETERMINISM.md`).



Deterministic inputs

- Meaning: explicit, serializable inputs to a simulation run (seed, scenario/config, command list).



Diff-friendly artifacts

- Meaning: outputs whose ordering and formatting are stable, so Git diffs are meaningful.



Ephemeral logs

- Meaning: outputs that are not required to be deterministic and should not be committed (verbose traces, timestamped logs).



## F. Stable identifiers (general)



EntityID

- Meaning: stable identifier for a game entity in SimCore.
- Requirement: must be stable within a deterministic run and suitable for referencing in logs and UI.



ReasonCode

- Meaning: stable code describing why an outcome occurred (failure, rejection, insufficient resources).
- Requirement: stable and UI-displayable; preferred over free-form strings for critical outcomes.



## G. Canonical ID conventions (LOCKED)



Global ID rules

- IDs are stable, opaque strings.
- IDs never depend on raw floating point coordinates.
- IDs are deterministic for procedural content (derived from seeds) and stable for authored content (hand-authored strings).
- Any output ordering that affects results or diffs must sort by stable IDs, not by incidental memory order.



Canonical ID field names (preferred)

- WorldId
- TickIndex
- Seed
- StationId
- MarketId
- GoodId
- LaneId
- RouteId
- JurisdictionId
- FactionId
- ProgramId
- FleetId
- FleetGroupId
- DoctrineId
- PackageId
- ProjectId
- ContractId
- QuoteId
- EventId
- ScopeId
- ScopeHash
- DiscoveryId
- RegionId
- NodeId
- EdgeId
- ScenarioId



## H. Time model vocabulary (LOCKED)



Tick

- The authoritative SimCore step unit.
- 1 tick = 1 game minute.



Time scale (60x)

- 1 real second = 1 game minute (60x).



Tick boundary rule

- Authoritative state changes occur only on tick boundaries inside SimCore.
- Continuous presentation (movement, combat visuals, docking, UI animation) is GameShell-only.
- GameShell posts Intents; SimCore applies them at the next tick boundary.



Derived time units (reference)

- 1 game hour = 60 ticks
- 1 game day = 1,440 ticks
- 1 game week = 10,080 ticks



## I. Intent model terms (LOCKED)



Intent

- Meaning: a request from GameShell (or an Adapter) to SimCore to change canonical state.
- Rule: Intents apply on tick boundaries only.



apply_tick

- Meaning: the tick index at which an Intent becomes eligible to apply (default: next tick after issuance).



Intent validation

- Meaning: SimCore checks legality, bounds, permissions, and invariants before applying an Intent.
- Rule: rejected intents must return stable ReasonCode values (no free-form rejection strings for critical outcomes).



Stable intent ordering

- Rule: Intent application order must be deterministic and explicit.
- Default required ordering keys:

  1) apply_tick ascending
  2) intent_kind_priority ascending (fixed table)
  3) actor_id ascending (stable ID)
  4) insertion_index ascending (deterministic creation order)

## J. Economy and market vocabulary

Good

- Meaning: a tradable unit type.
- Locked: goods exist as integer units.
- Goods declare unit_mass and unit_volume (canonical units are defined in `docs/21_90_TERMS_UNITS_IDS.md`).

Handling class

- Meaning: logistical handling category for a Good.
- Canonical set:

  - Bulk
  - Container
  - Liquid
  - Hazardous
  - Refrigerated

Decay model

- Meaning: declared loss process applied to inventory over time or handling.
- Canonical set:

  - None
  - Spoilage
  - Leakage
  - TheftShrink

- Rule: all losses must be represented as InventoryDelta events.

Substitution group

- Meaning: a partial demand substitution cluster (not perfect substitutes).
- Rule: substitution is partial to preserve commodity identity.

Station inventory

- Meaning: authoritative on-hand quantity per station per Good.
- Rule: production, consumption, and transfers change inventory on tick boundaries only.

Scarcity band (remote intel)

- Meaning: coarse band derived from station inventory for remote views.
- Canonical set:

  - Empty
  - Low
  - OK
  - High
  - Full

Published price

- Meaning: the price exposed to players and used for most remote decisions.
- Rule: published prices update on a cadence (default: every 12 game hours) and may be smoothed to avoid thrash.

Spread

- Meaning: bid/ask gap.
- Typical drivers: low depth, volatility, low trust, high heat, frontier risk.

Depth

- Meaning: how much volume can trade before marginal price impact becomes severe.

Target band

- Meaning: the desired inventory range for a station-good; prices respond to deviation from this band.

## K. Ledger and event vocabulary (LOCKED)

Event-sourced core

- Meaning: simulation state is a pure fold over immutable events.
- Rule: no module may directly mutate credits or inventories without emitting the corresponding event.

CashDelta

- Meaning: immutable event representing a credit change.
- Rule: exactly 1 category per CashDelta event.

Canonical categories:

- TradePnL
- Fees
- InsurancePremium
- InsurancePayout
- Maintenance
- Payroll
- FinancingInterest
- FinancingPrincipal
- Tariffs
- Fines
- ConfiscationLoss
- TheftLoss
- FacilityOPEX
- ContractSettlement

InventoryDelta

- Meaning: immutable event representing an inventory change.
- Rule: exactly 1 movement reason per InventoryDelta event.

Canonical reasons:

- Produced
- Consumed
- Loaded
- Unloaded
- Spoiled
- Leaked
- Stolen
- Confiscated
- Manufactured
- Reprocessed

TradeFill

- Meaning: immutable event representing an executed trade (quantity, price, fees, counterparties).
- Rule: a TradeFill implies corresponding CashDelta and InventoryDelta entries (directly or via a deterministic expansion step).

PriceQuote

- Meaning: immutable event representing a published price snapshot at a given publish tick.

ContractStateChange

- Meaning: immutable event representing contract lifecycle transitions (created, active, fulfilled, breached, etc.).

QueueEvent

- Meaning: immutable event representing queueing changes (berth slots, service backlog, reservations).

LossEvent

- Meaning: immutable event representing a loss incident (piracy, spoilage, leakage, theft) with explicit drivers and implications.

InspectionEvent

- Meaning: immutable event representing enforcement interaction (inspection, confiscation, fines, warnings).

SecurityStateChange

- Meaning: immutable event representing change in security capacity or posture in a region or corridor.

ServiceAvailabilityChange

- Meaning: immutable event representing changes in labor, maintenance, parts availability that modulate throughput and downtime.

ExplainEvent

- Meaning: immutable schema-bound explanation emitted by SimCore for outcomes, especially automation outcomes.
- Rule: ExplainEvent is structured data, not free text, in early slices.

## L. Programs, fleets, doctrines, packages (LOCKED)

Program

- Meaning: the primary player intent object for automation.
- Program owns: intent, constraints, policy, budget, targets, priority weights.
- Program outputs: KPI bands (p10/p50/p90), cost rates, incident summaries, explain events.
- Rule: Programs are schema-defined and headless-runnable.

Fleet (capacity pool)

- Meaning: a pool of capacity, not an individual ship control surface.
- Rule: v1 exposes only aggregate stats and capability tags. No ship-level UI, no per-ship fitting, no per-ship waypointing.

FleetGroup

- Meaning: a named set of fleets used for allocation, package application, doctrine defaults, and reporting.

Doctrine

- Meaning: a small parameter set defining behavior and tradeoffs (risk posture, inspection strategy, piracy response, stealth running, objective bias).

Upgrade Package

- Meaning: fleet-wide tech bundle with install and sustain requirements.
- Rule: packages are never per ship.

OwnershipClass

- Meaning: how a fleet is owned and controlled.
- Canonical set:

  - Contracted
  - Leased
  - Owned

Liaison Quote

- Meaning: deterministic preflight output for any create or change action.
- Required fields:

  - UpfrontCost
  - OngoingCostPerDay
  - TimeToActivate
  - Prerequisites
  - RequiredCapacity
  - ExpectedOutputBands_p10/p50/p90
  - Confidence inputs (intel staleness, volatility, threat state)
  - TopRisks with drivers
  - SuggestedMitigations

- Fairness rule: no catastrophic outcome without precursor signals reflected in quote risk or confidence.

Planning cycle

- Meaning: periodic cadence where programs are evaluated, quoted, and dispatched.
- Rule: planning runs on a fixed interval (every N ticks), not every tick.

Commitment window

- Meaning: minimum time before allocations can churn again; prevents rapid reallocation exploits and LLM-authored thrash.

## M. Automation failure taxonomy (LOCKED)

Primary failure cause

- Rule: every negative outcome attributes exactly 1 primary failure cause (plus optional secondary causes).
- The primary cause must be justified by stored decision-time facts, not reconstructed later.

Canonical primary causes:

- BadInfo
- Slippage
- Queueing
- Heat
- LossEvent
- CapitalLockup
- ServiceShortage

## N. Scope, regions, corridors (LOCKED)

Scope

- Meaning: stable selection of nodes and edges used by Programs and Projects.
- Rule: scope resolution must produce a sorted NodeId list and sorted EdgeId list, and emit a stable ScopeHash.

ScopeHash

- Meaning: deterministic hash derived from resolved scope (sorted IDs).
- Rule: ScopeHash is stored in quotes and explain events to guarantee reproducibility.

Region

- Meaning: tag-based or authored grouping used to define scope and world rules.
- Rule: regions must resolve deterministically to node and edge sets.

Corridor

- Meaning: a named set of edges used for patrol coverage and risk modeling.

## O. Naming and unit annotation rules (required)

Unit declaration rule

- Any field that can be misread must declare units in name or documentation, including:

  - costs: *_per_day, *_per_tick
  - throughput: *_work_units_per_day, *_units_per_tick
  - time: *_ticks, *_game_hours, *_game_days
  - risk: *_risk_0_100
  - probability bands: *_p10, *_p50, *_p90
  - mass and volume: *_kg, *_m3

- Rule: if a field cannot state its unit clearly, it does not belong in v1.
