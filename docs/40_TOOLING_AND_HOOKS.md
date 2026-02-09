# 40_TOOLING_AND_HOOKS

Operational reference for local validation, hooks, and tooling conventions.

This doc must stay aligned with `docs/00_READ_FIRST_LLM_CONTRACT.md`.

See also:
- `docs/20_TESTING_AND_DETERMINISM.md` (testing contract and determinism gates)
- `docs/21_90_TERMS_UNITS_IDS.md` (units, invariants, validator expectations)
- `docs/30_CONNECTIVITY_AND_INTERFACES.md` (connectivity contract and boundary rules)
- `docs/50_51_52_53_Docs_Combined.md` (tick boundary rule, intent ordering lock)

## A. Purpose

This document answers:
- what to run locally before or after a change
- what Git hooks run (and what they must validate)
- where generated artifacts land (and what is expected to be deterministic)
- what to attach per LLM session vs what stays project-level

## B. What is project-level vs per-session

Project-level (do not attach every chat)
- `docs/` kernel files (indexes and contracts)
- the canonical architecture document (referenced via `docs/10_ARCHITECTURE_INDEX.md`)
- tooling scripts in `scripts/` and `scripts/tools/`
- repo-tracked hooks in `.githooks/`

Per-session (the default attachment set)
- `docs/generated/01_CONTEXT_PACKET.md`
- only the minimum file contents required for the scoped edit (explicit allowlist)
- if connectivity violations exist or are needed for diagnosis:
  - `docs/generated/connectivity_violations.json`
  - optionally small excerpts from `docs/generated/connectivity_graph.json`

Important:
- do not attach `docs/templates/01_CONTEXT_PACKET.template.md` in sessions
- attach only the generated `docs/generated/01_CONTEXT_PACKET.md`



## C. Canonical commands to run (manual workflow)

All commands below are intended to run from repo root.

### C1. Generate the Context Packet
- Script: `scripts/tools/New-ContextPacket.ps1`
- Output:
  - `docs/generated/01_CONTEXT_PACKET.md`

Canonical run command:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Force -Verbose`

Notes:
- The Context Packet is the primary mechanism for minimizing per-chat attachments.
- It should include Objective, OUTPUT_MODE, GIT_MODE, allowlist, validations, and Definition of Done.

### C2. Godot script validation and parse gate
- Script: `scripts/tools/Validate-GodotScript.ps1`
- When required: any `.gd` file change
- Policy: tabs-only leading indentation in `.gd`

Canonical run command (per file):
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "<path-to-file.gd>"`

Optional: normalize leading 4-space blocks into tabs (writes UTF-8 no BOM)
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "<path-to-file.gd>" -NormalizeIndentation`

Notes:
- This validator enforces tabs-only indentation and runs a Godot headless parse gate.
- It may create and clean up a temporary script under `_scratch/` during parsing; it attempts to avoid dirtying the repo.

### C3. Connectivity scan (file-level)
- Script: `scripts/tools/Scan-Connectivity.ps1`
- Outputs (written to `docs/generated/`):
  - `connectivity_manifest.json`
  - `connectivity_graph.json`
  - `connectivity_violations.json`

Canonical run commands:
- Default (minimal excludes):
  - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force`
- Hardened (recommended for signal and review):
  - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force -Harden`

Notes:
- Hardened mode excludes additional churn/noise sources owned by the tool.
- Determinism requirement: repeated runs on an unchanged repo must produce identical JSON files.
- Determinism check (recommended when changing scanner behavior or exclusions):
  - `Get-FileHash docs/generated/connectivity_*.json -Algorithm SHA256`

### C4. PowerShell parse check
If a `.ps1` file is modified, verify it parses.

Example:
- `powershell -NoProfile -Command "Get-Content -LiteralPath '<path>' -Raw | Out-Null; [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath '<path>' -Raw), [ref]$null) | Out-Null"`

Important:
- only run commands that exist in-repo
- do not invent tooling or entrypoints

### C5. .NET build and tests
If any `.cs` files changed:

Build:
- `dotnet build`

Tests:
- If the change affects SimCore logic, run the SimCore test project(s) under `SimCore.Tests/` (and any other relevant test projects).
- If you have no stable test selection yet, `dotnet test` is acceptable as a starting point.



## D. Determinism and regression gates (operationalization)

This section maps `docs/20_TESTING_AND_DETERMINISM.md` requirements into operational expectations.

### D1. Required gates when SimCore logic changes
When SimCore logic changes (economy, routing, planning, events, intent processing, save/load):

Must run (minimum):
- `dotnet test` (or targeted `SimCore.Tests/` selection)

Strongly recommended (as soon as the runner exists):
- deterministic world hash regression run
- save/load roundtrip hash equality run

Until the runner exists:
- treat runner creation as a required milestone (not optional) for long-horizon balancing.

### D2. Headless runner commands (placeholders until implemented)
Do not claim these commands exist until implemented in-repo. When implemented, this doc must be updated to reference the exact entrypoint.

Target capabilities (runner surface):
- run one ScenarioId with a seed for N ticks
- emit world hash checkpoints
- emit deterministic metrics snapshot
- run save/load roundtrip and confirm hash equality
- exit non-zero on invariant or determinism failure

When the runner exists, document canonical commands here, for example:
- `dotnet run --project <RunnerProject> -- --scenario <ScenarioId> --seed <Seed> --ticks <N> --out <dir>`
- `dotnet run --project <RunnerProject> -- --scenario-pack <PackName> --out <dir>`

### D3. Scenario packs (required once supported)
Scenario packs required by design laws (see doc 51 and doc 20):
- Calm core region economy
- Frontier piracy spiral
- Major refinery outage (fuel shock via capacity loss)
- Tariff regime shift across a border
- Labor strike at a key port (service availability shock)

Operational rule:
- Once scenario packs exist, any SimCore change that can affect economy/logistics/planning must run the relevant pack(s) before merge.



## E. Recommended validation tiers

Tier 0 (fast, always)
- `.gd` validation and parse gate (if any `.gd` changed)
- PowerShell parse check (if any `.ps1` changed)
- `dotnet build` (if any `.cs` changed)

Tier 1 (core correctness, when relevant)
- Connectivity scan (recommended if any of the following changed):
  - adapters and bridge code between GameShell and SimCore
  - scripts that wire signals, events, or message buses
  - scene loading, resource loading, or `.tscn` references
- `dotnet test` for SimCore tests (when present and relevant)
- smoke test runner (if present and relevant)

Tier 2 (slow, CI or nightly)
- long-horizon scenario batch runs
- multi-seed determinism regressions
- performance regression checks
- golden scenario packs with drift thresholds (once implemented)



## F. Connectivity scan: what it guarantees (v0)

Scope
- file-level edges only (no symbol-level call graph)
- best-effort string and pattern scan

Edge detection categories (best-effort)
- Godot signals:
  - `signal `
  - `.connect(`
  - `Connect(`
  - `EmitSignal`
- Resource loads:
  - `load(`
  - `preload(`
  - `ResourceLoader.Load`
- Scene references:
  - `.tscn` string refs
  - PackedScene mentions (best-effort)
- C# events (best-effort):
  - `event `
  - `+=`
  - `-=`
- Messaging (best-effort):
  - `Publish(`
  - `Subscribe(`

Evidence requirement per edge (best-effort)
- repo-relative source file path
- 1-based line number(s)
- optional short snippet (must remain deterministic)

Violations
- The authoritative list of scanner rules is in `docs/generated/connectivity_violations.json` under `rules`.
- `docs/generated/connectivity_violations.json` is the canonical output consumed by the workflow review gate.



## G. Hooks

### G1. Hook directory model
This repo uses a repo-tracked hook directory:
- `.githooks/`

The intended behavior is:
- hooks run via Git in a way that matches real usage
- if hooks validate staged content, they must read staged blobs, not the working tree

### G2. Pre-commit behavior (current)
The pre-commit entrypoint calls a Windows wrapper which runs PowerShell:
- `.githooks/pre-commit` (sh entrypoint) calls `.githooks/pre-commit.cmd`
- `.githooks/pre-commit.cmd` runs:
  - `scripts/check_tabs.ps1 -Exit`

`check_tabs.ps1` loads:
- `scripts/tools/check_tabs_lib.ps1`

And validates:
- staged `.gd` files only
- excludes `addons/` and scratch folders
- fails commit on:
  - tabs-only policy violations
  - mixed indentation or trailing whitespace
  - BOM or zero-width characters detected in staged blob text

Hook editing rule:
- if you change hook behavior, you must test with staged-only changes and confirm it reads staged blobs.

### G3. Hook install
If you use a hook installer script, it must:
- configure `core.hooksPath` to `.githooks`
- avoid machine-specific absolute paths
- be safe to re-run

(If no installer exists, install can be done manually by setting `git config core.hooksPath .githooks`.)



## H. Atomic write expectations (summary)

When a session runs in `OUTPUT_MODE = POWERSHELL`:
- writes must be UTF-8 no BOM
- avoid writers that may add BOMs or normalize newlines
- deterministic trailing newline

Full details are in `docs/00_READ_FIRST_LLM_CONTRACT.md`.



## I. DevTool integration policy (optional)

If `DevTool.ps1` is used as a convenience entrypoint:
- it may provide wrappers to run Tier 0 and Tier 1 validations
- it must not change the authoritative behavior of the underlying scripts
- wrappers must preserve exit codes and must not mask failures
- wrappers must respect `GIT_MODE` (including `NO_STAGE`) and never stage or commit when `GIT_MODE = NO_STAGE`

If no stable wrapper exists yet:
- keep using the canonical commands in section C
- track wrapper work as a separate tooling task



## J. Common failure modes

- `.gd` indentation drift (spaces instead of tabs)
- hook validation reading working tree instead of staged content
- newline or BOM changes introduced by file writers
- tests that are non-deterministic due to time, randomness, or ordering
- connectivity scan noise from generated, archival, or build directories when not using `-Harden`
- unit drift (per tick vs per day, game time vs real time) when schema fields do not declare units
- missing determinism regression gates (world hash, save/load hash equality) when sim complexity increases
