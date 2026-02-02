\# 20\_TESTING\_AND\_DETERMINISM



\## A. Core axiom

SimCore must be testable via a headless console app, decoupled from Godot, to enable simulating years in seconds and keep LLM changes bounded. :contentReference\[oaicite:16]{index=16}



\## B. Headless harness (locked contract)

1\) Strict determinism

\- Given InitialSeed and identical CommandList, SimCore must produce bitwise-identical FinalState. :contentReference\[oaicite:17]{index=17}



2\) No Godot dependencies

\- Test runner is a standard .NET console app referencing SimCore library, not Godot. :contentReference\[oaicite:18]{index=18}



3\) Step API

\- Runner interacts only via SimCore.Initialize(seed) and SimCore.Step(days). :contentReference\[oaicite:19]{index=19}



Default v0 smoke test

\- Generate universe (seed 12345), simulate 365 days, assert no crashes and no NaN values, pass/fail via invariants.

Performance budget: 1 simulated year under 5 seconds real time in headless mode. :contentReference\[oaicite:20]{index=20}



\## C. Invariants (silent drift catchers)

Automated assertions run at end of every DayTick in debug/test builds; failure halts the sim immediately. :contentReference\[oaicite:21]{index=21}



Minimum v0 invariant set

\- Assert\_NoInfiniteMoney(): total currency < InflationCap

\- Assert\_InventoryConservation(): produced + imported = consumed + exported + stockpile

\- Assert\_SignalBounds(): Heat/Influence/Trace remain within bounds

\- Assert\_PlannerSolubility(): planner can route between connected safe nodes :contentReference\[oaicite:22]{index=22}



Extension point

\- ISimInvariant interface to add checks over time. :contentReference\[oaicite:23]{index=23}



\## D. Scenarios (integration unit)

A “Scenario” is the smallest unit of sim integration testing. Every non-trivial SimCore change must be verifiable by running at least one scenario headlessly. :contentReference\[oaicite:24]{index=24}



Locked contract

\- Scenario is a versioned file fully defining a headless run

\- Minimum fields: ScenarioId, InitialSeed, StopCondition, optional BalanceConfig overrides, optional CommandScript, expected checks

\- Deterministic inputs only (no wall clock, locale, machine ordering, Godot frame timing)

\- Repro-first: add failing scenario first, then fix until it passes :contentReference\[oaicite:25]{index=25}:contentReference\[oaicite:26]{index=26}



\## E. Headless runner (CLI surface)

Locked contract

\- Commands: run one scenario; run batch across N seeds; emit artifacts to output dir

\- Exit codes: non-zero for determinism failure, invariant failure, schema failure, or crash

\- CI mode includes explicit runtime caps per scenario :contentReference\[oaicite:27]{index=27}



\## F. Run artifacts and regression semantics

Minimum stable outputs

\- run\_manifest.json (scenario id, seed, config hash, build/version id, git commit if available)

\- metrics.json (schema, units, cadence)

\- events.jsonl (structured event log with tick/day and stable entity ids)

Optional snapshots behind a flag with size limits :contentReference\[oaicite:28]{index=28}



Scorecards and suite tiers

\- Hard fail: determinism, invariants, schema validity, NaN, overflow, impossible state

\- Soft fail: metric drift beyond allowed deltas, performance regressions within tolerance

\- Suite tiers: per-commit fast, CI, nightly long-horizon (optional until needed)

\- Change isolation rule: each feature change must come with measurable proof (scenario, metric, or invariant) :contentReference\[oaicite:29]{index=29}



