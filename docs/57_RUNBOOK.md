# Runbook (canonical commands)

All commands run from repo root.

## Core (SimCore)

Purpose: validate kernel correctness and determinism in the .NET simulation layer.

- SimCore fast loop (default during iteration)
  - dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName!~SimCore.Tests.Determinism.LongRunWorldHashTests&FullyQualifiedName!~SimCore.Tests.GoldenReplayTests"

- SimCore closeout-only slow suites (intentional)
  - dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FullyQualifiedName~SimCore.Tests.Determinism.LongRunWorldHashTests|FullyQualifiedName~SimCore.Tests.GoldenReplayTests"

- SimCore full run (fallback when doing a final check and no stable targeted selection exists)
  - dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release

## Governance + tooling

Purpose: validate gate registry schema + run deterministic repo hygiene + connectivity.

- Validate gates
  - powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-Gates.ps1

- Repo health
  - powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Repo-Health.ps1

- Connectivity scan (default)
  - powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force

- Connectivity scan (hardened)
  - powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force -Harden

## Godot headless (Mono)

Purpose: validate GameShell wiring and capstone scripts with deterministic stdout%stderr.

Prereq: use the Godot Mono console binary (the `*_console.exe` build). Configure it via the repo’s Godot exe lookup (see scripts/tools/common.ps1 Get-GodotExe) or pass an explicit path in your local environment.

- Parse gate for a .gd file (tabs-only + headless parse)
  - powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "<repo-relative-path-to-file.gd>"

- Smoke harness (GATE.X.GAMESHELL.SMOKE.001)
  Purpose: load minimal scene + bind SimBridge + run exactly N=120 ticks + print Seed, tick_count, world_hash@0%60%120 and deterministic entity counts.
  Canonical run (from gate proof):
  - Godot 4.6 mono --headless --verbose --path . -s scripts/tests/test_sim_skeleton.gd --seed=42 --ticks=120
  Determinism check:
  - capture stdout and stderr for 2 runs and compare SHA256 (must match)

- Universe validator (worldgen invariants suite, GameShell side)
  Purpose: headless worldgen constraints validation driven by the GameShell generator.
  Entry script (present in repo):
  - scripts/tests/test_universe_validator.gd
  Canonical run pattern:
  - Godot 4.6 mono --headless --verbose --path . -s scripts/tests/test_universe_validator.gd
  Note: if this script supports CLI args (seed range, counts), keep those args in this runbook once they are explicitly defined by the script and recorded in a gate proof.

- Galaxy core headless regression (GameShell worldgen determinism spot check)
  Purpose: exercise galaxy generator headlessly as a deterministic regression.
  - Godot --headless --quit --script res://scripts/tests/test_galaxy_core.gd

## Notes (determinism + bookkeeping)

- All harness outputs must be deterministic: no timestamps, stable ordering, stable formatting; stdout%stderr hashes must be stable across reruns on unchanged repo state.
- Any determinism repro or regression artifact must include Seed (and TickIndex where applicable).
- Session log is authoritative: update docs/56_SESSION_LOG.md and docs/55_GATES.md in the same change set as the PASS entry.
