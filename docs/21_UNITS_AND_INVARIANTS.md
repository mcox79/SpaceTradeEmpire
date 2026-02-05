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



