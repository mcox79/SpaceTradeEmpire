# Space Trade Empire
## Programs, Fleets, Doctrines Control Surface (Canonical)

### Status
Canonical design law for the player control surface and the automation-first “empire operations” model. This document defines what the player can and cannot do, the core abstractions used by SimCore, the Liaison Quote contract, the planning cadence model, and the minimum UI surfaces. Optimized for LLM-driven implementation, deterministic replay, and small vertical slices.

See also:
- `docs/52_DEVELOPMENT_LOCK_RECOMMENDATIONS.md` (tick = 1 game minute, 60x mapping, tick boundary rules, intent ordering lock)
- `docs/51_ECONOMY_AND_TRADE_DESIGN_LAW.md` (ledger law, automation penalties, failure taxonomy, determinism invariants)
- `docs/20_TESTING_AND_DETERMINISM.md` (golden runs, world hash regression)
- `docs/30_CONNECTIVITY_AND_INTERFACES.md` (layer boundaries, interface discipline)

Canonical consistency rule:
- If any chat instruction or implementation proposal conflicts with the disallowed actions or abstraction boundaries in this document, update the canonical doc first and align dependents.

---

## 0. Design-Law (Non-Negotiables)
- No ship-level UI for fleets. No per-ship fitting for fleets. No per-ship waypointing for fleets.
- Single exception: the Player Ship is a unique entity with ship-level UI and module fitting. This exception does not extend to fleets or NPC ships.
- Player controls Programs, Fleets as capacity pools, Doctrines, Upgrade Packages, Budgets, and Policies.
- SimCore owns dispatch and routing. GameShell renders and issues intents.
- Every meaningful simulation outcome must be deterministic and explainable.
- Explain outputs are structured JSON (schema-bound). Free-text explanations are prohibited in early slices.

Agency law (required to keep this fun without ship micromanagement):
- Every player action must have a legible lever and a short feedback loop measured in minutes of real time.

Friction law (required to prevent “optimizer magic”):
- The Liaison Quote and Explain events must be consistently true. If the system cannot explain it, it must not do it.

Override law (required to prevent helplessness):
- Players must have high-level interrupts (Alerts and mitigations) that can meaningfully change outcomes without ship-level orders.

---

0.1 Player Ship exception (bounded)

- The Player Ship is the only ship that may be directly piloted and fitted.
- Player Ship fitting is allowed via a dedicated UI surface (Loadout).
- Player Ship modules are inventory items (tradeable) and install via intents at tick boundaries.
- No other ships expose ship-level UI, fitting, waypointing, or per-ship inventories.

---

## 1. Player control surface (allowed actions only)

Allowed actions:
- Create Program
- Fund Program and set `BudgetCap`
- Pause Program
- Set Target and `PriorityWeight`
- Set Doctrine (Program and Fleet defaults)
- Apply Upgrade Packages to fleets or fleet groups
- Allocate fleet capacity to Programs (assign or slider)
- Manage Fleet Groups
- Respond to Alerts (high-level mitigations only)

Explicitly disallowed:
- Per-ship orders (fleets only)
- Per-ship upgrades (fleets only)
- Route waypointing and per-fleet routing (fleets only)
- Any UI that exposes individual ship inventories as a primary control surface (fleets and NPC ships only, aggregates only)

Explicitly allowed for Player Ship:
- Pilot locally
- Fit and uninstall modules
- See Player Ship inventory (cargo plus installed modules)

---

## 2. Core abstractions (authoritative definitions)

### 2.1 Program
Program is the primary player intent object:
- intent + constraints + KPIs + explain
- owns: scope, policy, budget, priorities, targets, alert thresholds
- produces: expected output bands and postmortems

### 2.2 Fleet
Fleet is a capacity pool:
- capacity + doctrine + packages + ownership class
- fleets are not individual ships in the player UI
- fleets can be grouped for allocation and package management

### 2.3 Fleet Group
A named collection of fleets:
- used for allocation, doctrine defaults, and package application
- may be scoped to a region/corridor for UI convenience only (does not imply routing control)

### 2.4 Upgrade Package
A fleet-wide tech bundle with install and sustain:
- never per ship
- changes exposed fleet stats and capability tags
- defines degraded mode when sustain fails

### 2.5 Doctrine
Behavior parameters and tradeoffs:
- small set in v1 (expand later)
- used by planners and dispatchers, not manual routing

### 2.6 Liaison Quote
A deterministic preflight summary for any create/change action:
- cost, time, prerequisites, required capacity, expected outputs and bands, confidence inputs, risks, mitigations
- quote is stored and later used for explain and postmortems

### 2.7 Construction Project
A staged facility build pipeline managed by a Program:
- no manual logistics
- stateful progression with explicit stage gates

---

## 3. Programs in v1 (canonical set)

Programs are the only “automation” unit the player operates. A program must be definable as a schema and runnable headlessly.

Each program outputs:
- expected output bands (`p10`, `p50`, `p90`)
- `CostPerDay`
- incident summary (by taxonomy)
- confidence and intel staleness
- structured Explain event (schema-bound)

### 3.1 Trade Program
Intent:
- earn profit moving goods within declared scope and policy.

Fields (minimum):
- `Scope`
- `TargetProfitPerDay` OR `TargetThroughput`
- `CommodityPolicy`
- `RiskCeiling`
- `LegalityPosture` (v1: `Comply` or `Gray`)
- `PriorityWeight`
- `BudgetCap`
- `DoctrineRef`
- `AutomationLevel`
- Constraints:
  - avoid systems
  - required hubs
  - escort required

Outputs:
- `ProfitPerDay_p10/p50/p90`
- `CostPerDay`
- incident rate summary
- confidence and intel staleness
- Explain

### 3.2 Mining Program
Intent:
- produce commodity and deliver to a sink.

Fields (minimum):
- `Scope`
- `OutputCommodityTag`
- `DeliveryPolicy` (to refinery, to market, stockpile)
- `RiskCeiling`
- `PriorityWeight`
- `BudgetCap`
- `DoctrineRef`
- `AutomationLevel`
- `EscortRequired`

Outputs:
- `UnitsPerWeek_p10/p50/p90`
- `CostPerDay`
- incidents and attrition summary
- Explain

### 3.3 Patrol Program
Intent:
- reduce threat in corridor or region.

Fields (minimum):
- `Scope`
- `CoverageTarget` OR `ThreatReductionTarget`
- `EngagementPolicy`
- `PriorityWeight`
- `BudgetCap`
- `DoctrineRef`

Outputs:
- `ThreatDeltaInScope`
- `CostPerDay`
- incidents
- Explain

Guardrail (non-negotiable):
- Patrol cannot be a mandatory tax.
- Alternative mitigations must exist: cautious doctrine, escorts, route avoidance, intel improvement.

### 3.4 Construction Program
Intent:
- build facilities via a project pipeline, no manual logistics.

Project fields (minimum):
- `Site` and `Slot`
- `ProjectType` and `Tier` (starport, refinery, science center)
- `PriorityWeight`
- `BudgetCap`
- `SourcingPolicy` (allow imports, require local, legal-only vs gray)
- `ReliabilityTarget` (fast vs resilient vs cheap)

Project model (v1):
- Up to 3 stages in v1
- Explicit states:
  - `Active`
  - `AwaitingInputs`
  - `Degraded`
  - `AtRisk`
  - `Complete`

Hybrid boundary rule (canonical):
- Bulk inputs are abstracted by market access plus logistics throughput.
- Complex components require delivery or local production (explicit list required in schema).
- Player never chooses delivery routes.

Construction outputs:
- `BurnRatePerDay`
- `ETA_p10/p50/p90`
- `MissingInputs` list
- `RiskList` and Explain

---

## 4. Fleet model (capacity pool, exposed stats only)

Fleet exposes only:
- `ThroughputWorkUnitsPerDay`
- `RangeTier`
- `CombatRating`
- `SignatureRating`
- `OperatingCostPerDay`
- `ReliabilityRating`
- `CapabilityTags`:
  - `Bulk`
  - `HighValue`
  - `Hazardous`
  - `Industrial`
  - `StealthCapable`
  - `FractureCapable` (late unlock)

Fleet metadata:
- `OwnershipClass`: `Contracted`, `Leased`, `Owned`
- `GroupId` optional
- installed Packages
- default DoctrineRef

### 4.1 Ownership best-practice tradeoffs (canonical)
- Contracted:
  - fastest spin up
  - higher cost
  - limited doctrine and package access
  - cancellation risk (must produce warnings)
- Leased:
  - moderate cost
  - moderate control
  - some packages
- Owned:
  - capex and sustainment
  - full doctrine and package access
  - requires spares pipeline
  - fracture later

---

## 5. Doctrine model (v1 parameters)

Doctrine parameters in v1:
- `RiskPosture`
- `InspectionStrategy`
- `PiracyResponse`
- `StealthRunning`
- `ObjectiveBias`

Rule:
- Doctrines must be small in v1. Expand later via additional parameters or doctrine presets.

---

## 6. Upgrade Packages (fleet-wide only)

Categories in v1:
- Drive Package
- Defense Package
- Industrial Package

Optional later:
- weapons, sensors, stealth, mining, fracture

Each package defines:
- eligibility rules
- install requirements: port tier, time, parts
- sustain requirements: spares and trained crew per day
- effects on exposed fleet stats and capability tags
- degraded mode behavior when sustain fails

Rule:
- Packages are never per ship.

---

## 7. Liaison Quote contract (required preflight for all changes)

For any create or change action, SimCore must produce a stored quote with:
- `UpfrontCost`
- `OngoingCostPerDay`
- `TimeToActivate`
- `Prerequisites`
- `RequiredCapacity`
- `ExpectedOutputBands_p10/p50/p90`
- Confidence inputs:
  - intel staleness
  - volatility
  - threat state
- `TopRisks` with drivers
- `SuggestedMitigations`

Fairness rule (non-negotiable):
- No catastrophic outcome without precursor signals reflected in quote risk or confidence.

Storage rule:
- Quotes are stored and referenced by ID in later Explain and postmortems.

---

## 8. Dispatch, planning cadence, and determinism

### 8.1 Determinism contract
- Same initial state plus same seed plus same player intents equals same results.
- Randomness only occurs in explicit incident rolls and is logged.

### 8.2 Planning structure (fits the architecture)
Planning must be a pure function:
- `PlanningInput` snapshot -> `PlanningOutput` decisions

Rules:
- Shell applies deltas (intent application), never computes plans.
- Decision log emitted every planning cycle.

### 8.3 Program cadence vs tick cadence
- Tick accumulates travel progress, costs, wear.
- Planning runs on a fixed interval: every N ticks (not every tick).
- Allocation changes limited by commitment windows and benefit thresholds.

Canonical default (v1):
- Planning interval: every 60 ticks (every 1 game hour, every 60 real seconds at 60x)
- Planning interval is a versioned constant in code (not data-driven at runtime) through Slice 1 and Slice 1.5.
- Commitment window: 12 game hours minimum before reallocations can take effect, unless Emergency Doctrine is enabled
- Reallocation threshold: require improvement > configured threshold (default 10%) to prevent churn

### 8.4 Arbitration mechanics (required)
- `PriorityWeight` drives allocation among competing programs.
- Commitment window prevents rapid churn.
- Reallocation only if improvement exceeds threshold.
- Player can pin or do-not-preempt critical programs.

### 8.5 Explain format (locked)
- Explain is structured JSON, schema-bound.
- Explain must include:
  - primary failure cause (from automation failure taxonomy)
  - binding constraint(s)
  - top drivers (causal chain references)
  - confidence and staleness
  - suggested mitigation
  - evidence refs (IDs for events/quotes/scopes)

Free-text explain is prohibited in early slices.

---

## 9. UI screens (no ship screen)

Minimum UI surfaces:
- Opportunities:
  - ranked program opportunities with confidence and staleness
- Programs:
  - KPIs, variance, incidents, controls, explain
- Fleets:
  - cards, packages, doctrine, groups, capacity allocation
- Projects:
  - construction pipeline, missing inputs, burn, ETA

Rule:
- No ship screen exists.

---

## 10. Important gaps to close now (dev-blocking)

These gaps are dev-blocking or will cause drift with LLM coding.

### Gap 1: Schema ownership and versioning choice
Recommendation (canonical for now):
- C# records as source of truth now plus strict validator and schema version tag.
- Plan to generate JSON schema later if needed.

### Gap 2: Scope resolution spec
Must define how scope resolves to node and edge sets and produces a stable hash.

Recommendation:
- tag-based regions plus explicit corridor edge lists
- resolved scope yields sorted `NodeIds` and `EdgeIds`
- `ScopeHash` is computed from sorted IDs and included in quotes and explain events

### Gap 3: Units and invariants
Without this, you will get silent unit mismatches.

Recommendation:
- a single Units and Invariants doc plus runtime validation:
  - risk is 0-100
  - cost is per day
  - throughput is work units per day
  - confidence bands are `p10`, `p50`, `p90`

### Gap 4: Incidents vs failure causes (required distinction)
Two layers exist and must not be conflated:

1) Incident type (what happened operationally):
- `Delay`
- `Loss`
- `Damage`
- `Hold`
- `Shortage`

2) Primary failure cause (why it happened, automation postmortem):
- `BadInfo`
- `Slippage`
- `Queueing`
- `Heat`
- `LossEvent`
- `CapitalLockup`
- `ServiceShortage`

Rules:
- Every negative outcome must include exactly 1 primary failure cause.
- Incidents may be multiple, and each incident includes:
  - `causeTag`
  - severity
  - duration (ticks)
  - mitigation hints
  - referenced entities (IDs) when applicable

### Gap 5: Construction complex component tag list
Hybrid boundary requires an explicit list.

Recommendation:
- start with 3 to 6 complex component tags only, everything else bulk

### Gap 6: Golden sims and validation gates
Deterministic regression tests are required.

Recommendation:
- add two tiny golden worlds with fixed seeds and expected JSON snapshots:
  - KPIs
  - allocations
  - incidents
  - explain

### Gap 7: KPI definitions
Lock formulas and naming so UI and optimizer agree:
- `ProfitPerDay` definition
- variance definition
- confidence band meaning

---

## 11. Recommended locking order (implementation)

Lock the schemas and enums now, in one contracts folder or assembly.
- Build a validator and make it a hard gate in devtools and pre-commit.
- Add golden sims that output deterministic snapshots: KPIs, allocations, incidents, explain.
- Implement the minimal planner:
  - simple allocation with hysteresis
  - naive dispatch
  - quote generator

Only then expand:
- program types
- packages
- optimizer sophistication

This order keeps the LLM loop safe and prevents architecture drift.
