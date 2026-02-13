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
- optionally `docs/generated/02_STATUS_PACKET.txt` (recommended when closing gates; includes top gate closure blockers when present)
- only the minimum file contents required for the scoped edit (explicit allowlist)
- if connectivity violations exist or are needed for diagnosis:
  - `docs/generated/connectivity_violations.json`
  - optionally small excerpts from `docs/generated/connectivity_graph.json`

Important:
- do not attach `docs/templates/01_CONTEXT_PACKET.template.md` in sessions
- attach only the generated `docs/generated/01_CONTEXT_PACKET.md`

### B1. Gate governance v2.2 attachment sets (Pattern A)

Operational surfaces (step-attached, not always attached):
- `docs/55_GATES.md` (ledger)
- `docs/gates/gates.json` (execution queue snapshot)
- `docs/56_SESSION_LOG.md` (append-only provenance)

Context Packet is always required (every step):
- `docs/generated/01_CONTEXT_PACKET.md`

Step allowlists (typical):
- Step A (EPICS -> 55 ledger authoring/refinement):
  - `docs/generated/01_CONTEXT_PACKET.md`
  - `docs/54_EPICS.md`
  - `docs/55_GATES.md`

- Step B (55 -> queue shaping):
  - `docs/generated/01_CONTEXT_PACKET.md`
  - `docs/55_GATES.md`
  - `docs/gates/gates.json`
  - `docs/gates/gates.schema.json`
  - `docs/gates/GATE_FREEZE_RULES.md`

- Step C (execute exactly 1 task):
  - `docs/generated/01_CONTEXT_PACKET.md`
  - `docs/gates/gates.json`
  - only the task evidence files listed by the selected task

- Step D (bookkeeping close):
  - `docs/generated/01_CONTEXT_PACKET.md`
  - `docs/gates/gates.json`
  - `docs/56_SESSION_LOG.md`
  - plus only the minimal needed excerpt from `docs/55_GATES.md` if a rollup/status update is required

Escalation guidance (token discipline):
- Do not attach the full `docs/55_GATES.md` during Step C.
- If scope or evidence-universe ambiguity arises in Step C, STOP and request only the minimal excerpt for the relevant `gate_id`.

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
- It should include Objective, allowlist, validations, and Definition of Done.
- Modes:
  - If modes are present, treat them as explicit overrides or confirmations of defaults.
  - If modes are omitted, defaults apply (see `docs/00_READ_FIRST_LLM_CONTRACT.md`).
- Context Packet generation is distinct from the Status Packet and other generators; run those separately (or via DevTool “Generate All”) when needed.

### C1a. Generate the Status Packet
- Script: `scripts/tools/New-StatusPacket.ps1`
- Output:
  - `docs/generated/02_STATUS_PACKET.txt`

Canonical run command:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-StatusPacket.ps1 -Force`

Notes:
- The Status Packet is a compact snapshot for gate work and fast triage.
- When `docs/generated/gate_closure_delta.md` exists, the Status Packet includes a compact “TOP GATE CLOSURE BLOCKERS” section.

### C1b. Gate closure delta (TODO gates and missing evidence)
- Script: `scripts/tools/New-GateClosureDelta.ps1`
- Output:
  - `docs/generated/gate_closure_delta.md`

Canonical run command:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-GateClosureDelta.ps1 -Force`

Notes:
- Parses gate tables in `docs/55_GATES.md`.
- Focuses TODO gates and extracts referenced evidence paths.
- Produces “Top gate closure blockers” by grouping missing referenced files by how many TODO gates reference them.

### C1c. Capability index (UI + bridge surface)
- Script: `scripts/tools/New-CapabilityIndex.ps1`
- Output:
  - `docs/generated/capability_index.md`

Canonical run command:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-CapabilityIndex.ps1 -Force`

Notes:
- Indexes the UI and bridge “capability surface” so you can see what APIs exist and where they are wired.
- Reduces time lost to “does the bridge already expose this?” and “where is this used?”

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
- it may include convenience buttons for gate closure and capability surface artifacts (for example “GATE CLOSURE DELTA” and “CAPABILITY INDEX”) and may run them in any “Generate All” pipeline

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
- gate closure delta drift if gate table formatting changes (generator must fail soft and report parse gaps rather than silently omitting gates)

