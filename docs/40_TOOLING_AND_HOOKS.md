# 40_TOOLING_AND_HOOKS

Operational reference for local validation, hooks, and tooling conventions.

This doc must stay aligned with `docs/00_READ_FIRST_LLM_CONTRACT.md`.

## Known validation entrypoints

### Godot script validation

- Script: `scripts/tools/Validate-GodotScript.ps1`
- When required: any `.gd` file change
- Policy: tabs-only indentation in `.gd`

If you change `.gd` files, you must run:

- `scripts/tools/Validate-GodotScript.ps1` on the changed files

### Connectivity scan (file-level)

- Script: `scripts/tools/Scan-Connectivity.ps1`
- Purpose: best-effort file-level connectivity graph for interface review and invariants enforcement
- Outputs (written to `docs/generated/`):
  - `connectivity_manifest.json`
  - `connectivity_graph.json`
  - `connectivity_violations.json`

Canonical run commands:

- Default (excludes only: `addons/`, `_scratch/`, `docs/generated/`, `.git/`):
  - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force`

- Hardened (recommended for human review and CI signal):
  - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force -Harden`

Notes:
- Hardened mode additionally excludes churn/noise sources:
  - `.godot/`, `_archive/`, `GameShell/_DISABLED_scripts/`, `SimCore/bin/`, `SimCore/obj/`
- Determinism requirement: repeated runs on an unchanged repo should produce identical JSON files.

### PowerShell parse check

If a `.ps1` file is modified, verify it parses.

- Example:
  - `powershell -NoProfile -Command "Get-Content -LiteralPath '<path>' -Raw | Out-Null; [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath '<path>' -Raw), [ref]$null) | Out-Null"`

Important: only run commands that exist in-repo. Do not invent tooling.

## Recommended validation tiers

Tier 0 (fast, always)
- `.gd` validation (if any `.gd` changed)
- PowerShell parse check (if any `.ps1` changed)
- `dotnet build` (if any `.cs` changed)

Tier 1 (core correctness, when relevant)
- Connectivity scan (recommended if any of the following changed):
  - scripts that wire signals, events, or message buses
  - scene loading, resource loading, or `.tscn` references
  - bridge/adapters between GameShell and SimCore
- `dotnet test` for SimCore tests (if a test project exists and is part of the workflow)
- headless scenario runner (if implemented)

Tier 2 (slow, CI/nightly)
- long-horizon scenario batch runs
- performance regression checks

## Connectivity scan: what it guarantees (v0)

Scope
- File-level edges only (no symbol-level call graph)
- Best-effort string/pattern scan

Edge types (best-effort detection)
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

Determinism checks
- Recommended: hash outputs across repeated runs on an unchanged repo.
  - `Get-FileHash docs/generated/connectivity_*.json -Algorithm SHA256`

Evidence requirement per edge
- Each edge includes evidence buckets with:
  - source file path
  - 1-based line number(s)
  - optional short snippet (best-effort)

Violations (v0 hard invariant)
- Any SimCore file referencing `Godot.` is a violation (severity: error).
- Violations are emitted to `docs/generated/connectivity_violations.json`.

## Hooks

### Pre-commit / commit-time validation

If the repo uses Git hooks:
- Hooks must validate the same way in real usage as they do in your local workflow.
- If hooks validate staged content, ensure they read the staged blob, not the working tree.

If you modify hook behavior:
- Exercise the hook entrypoint the same way Git does.
- Confirm behavior with staged-only changes.

## Atomic write expectations (summary)

When a session runs in `OUTPUT_MODE = POWERSHELL`:
- writes must be UTF-8 no BOM
- avoid PowerShell writers that may add BOMs or normalize newlines
- deterministic trailing newline

Full details are in `docs/00_READ_FIRST_LLM_CONTRACT.md`.

## “Run all validations” entrypoint (recommended)

Create a single entrypoint script (PowerShell) that:
- detects changed files
- runs required validators based on file types
- exits non-zero on failure

If this script does not exist yet, track it as a TODO and keep using the manual tier-0 steps above.

## Common failure modes

- `.gd` indentation drift (spaces instead of tabs)
- hook validation reading working tree instead of staged content
- newline/BOM changes introduced by PowerShell file writers
- tests that are non-deterministic due to time, randomness, or ordering
- connectivity scan noise from generated/archival/build directories when not using `-Harden`
