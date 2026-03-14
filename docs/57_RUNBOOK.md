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

- TweakRoutingGuard baseline re-mint (ONLY after routing new literals to Tweaks/ — see rule below)
  - STE_TWEAK_GUARD_MINT_BASELINE=1 dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "TweakRoutingGuard_NewNumericLiterals"
  - PowerShell: $env:STE_TWEAK_GUARD_MINT_BASELINE="1"; dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "TweakRoutingGuard_NewNumericLiterals"
  - Rule: re-mint ONLY when literals have been moved from SimCore/ to SimCore/Tweaks/*V0.cs AND the edit shifted line numbers for pre-existing (already-baselined) literals. Do NOT re-mint to cover up new un-routed literals. Always read violations report first (docs/generated/tweak_routing_guard_violations_v0.txt) and confirm all remaining violations are line-shift artifacts, not new literal additions.

- Golden hash update (REQUIRES explicit user approval — never do autonomously)
  - STE_UPDATE_GOLDEN=1 dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "LongRun_10000Ticks_Matches_Golden|Simulation_Is_Deterministic_With_Input"
  - Rule: NEVER update golden hashes without explicit user sign-off. A golden hash mismatch means world simulation state changed. Either it is a real determinism bug (fix the bug, do not update the hash) or it is a deliberate world-gen design change (discuss with user first). After approval, paste PASTE_LONGRUN_GENESIS and PASTE_LONGRUN_FINAL values into SimCore.Tests/Determinism/LongRunWorldHashTests.cs ExpectedGenesisHash/ExpectedFinalHash, and similarly for SimCore.Tests/GoldenReplayTests.cs.

## Godot headless (Mono)

Purpose: validate GameShell wiring and capstone scripts with deterministic stdout%stderr.

Prereq: use the Godot Mono console binary (the `*_console.exe` build). Configure it via the repo's Godot exe lookup (see scripts/tools/common.ps1 Get-GodotExe) or pass an explicit path in your local environment.

Prereq (C# bridge methods): if any C# SimBridge method has changed since the last build, run `dotnet build --nologo` on the game assembly before running headless tests. Without this the Godot assembly is stale and bridge methods will report as missing at runtime with no other error. The canonical build command is:
  - dotnet build "Space Trade Empire.csproj" --nologo

Prereq (C# script nodes in headless): C# node scripts (StationMenu.cs, GalaxyView.cs, etc.) are NOT auto-compiled by the headless runner. Without a prior build, those nodes degrade to their base Godot type (e.g. Control instead of StationMenu) and have no custom signals or methods. Always build the game assembly before running tests that load scenes with C# nodes. Use has_signal() and has_method() guards in GDScript when the C# node may be absent.

Prereq (PowerShell invocation): when calling Godot from PowerShell with a quoted exe path, the & call operator is mandatory. Without it, PowerShell treats the quoted string as an expression and --headless triggers a parser error.
  - Correct:   & "C:\...\Godot.exe" --headless --path "D:/..." --script scripts/tests/<script>.gd
  - Wrong:     "C:\...\Godot.exe" --headless ...  (ParserError: UnexpectedToken)

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

## Notes (headless script tests)

Godot binary (path varies by machine — update when switching):
  - Laptop:  C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe
  - Desktop: <update when confirmed>

Canonical pattern for -s script tests (substitute correct path for machine):
  - & "<godot-exe>" --headless --path . -s "res://scripts/tests/<script>.gd" -- --seed=<N>
  - The -- separator is required. Everything after -- is passed to OS.GetCmdlineUserArgs() in C#.
  - Without --, --seed=N is consumed by the engine and never reaches ApplyCmdlineOverrides().

Filtering UUIR transcript output:
  - Append: 2>&1 | Select-String "^UUIR\|"

Determinism proof pattern (two runs, hashes must match):
  - & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --headless --path . -s "res://scripts/tests/<script>.gd" -- --seed=42 2>&1 | Select-String "^UUIR\|" | Tee-Object -FilePath "docs/generated/<script>_run1.txt"
  - & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --headless --path . -s "res://scripts/tests/<script>.gd" -- --seed=42 2>&1 | Select-String "^UUIR\|" | Tee-Object -FilePath "docs/generated/<script>_run2.txt"
  - Get-FileHash docs/generated/<script>_run1.txt, docs/generated/<script>_run2.txt -Algorithm SHA256 | Select-Object Hash, Path

Exit code check:
  - & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --headless --path . -s "res://scripts/tests/<script>.gd" -- --seed=42 2>&1 | Out-Null; $LASTEXITCODE
  - Expected: 0

Process-hang pattern (SimBridge background thread): after quit() prints its final token, the Godot process does not return to the shell prompt promptly. Root cause: SimBridge autoload starts a C# Task (simulation thread) that keeps .NET alive. Fix: call StopSimV0() on the SimBridge node before quit() in every test script.
  - Pattern:
      var bridge = get_root().get_node_or_null("SimBridge")
      if bridge and bridge.has_method("StopSimV0"):
          bridge.call("StopSimV0")
      quit(code)
  - If process still hangs after StopSimV0, Ctrl+C is safe — output is already flushed before quit() is called.

C# signal names from GDScript: Godot 4 exposes C# [Signal] delegate void FooEventHandler() to GDScript as "foo" (snake_case, EventHandler suffix stripped). Use has_signal("foo") guard before connecting to tolerate the headless case where the C# assembly is not loaded.

GalaxyView.GetOverlayMetricsV0() metric semantics (confirmed headless, 2026-03-03):
  - node_count     = rendered (non-HIDDEN) nodes only. In headless fresh worldgen, only the player's
                     home system is DISCOVERED, so node_count=1 even with a full galaxy loaded.
  - edge_count     = total lane connections in the galaxy snapshot, including lanes between HIDDEN nodes.
                     Typical fresh-gen value: ~26. Does NOT reflect visible-only edges.
  - player_node_highlighted = true when the player fleet's current node was found and rendered green.
  - Write gate assertions against node_count>=1 (not >=2) unless the test explicitly seeds multiple
    DISCOVERED nodes before opening the overlay.

SceneTree script contract (extends SceneTree):
  - _initialize() must not block the main thread (no OS.delay_msec polling loops).
  - All SimBridge readiness polling must use _process() so the engine can yield between frames.
  - SimBridge._Ready() runs on the main thread; blocking _initialize() prevents it from ever executing.
  - SimBridge is a scene node, not an autoload. Acquire it via root.get_node_or_null("SimBridge"), not load() or preload().

Vacuous-pass pattern (player-created state):
  - Some bridge snapshots (programs, exploitation packages, etc.) return empty results at world init because the state is player-created, not NPC-seeded.
  - The correct headless assertion pattern is: verify the method exists and returns the correct schema, emit the count, and PASS vacuously if count is zero with an explanatory token (e.g. PASS|found_intervention_verb=vacuous_no_programs_at_init).
  - Do NOT fail a headless test solely because player-created state is absent at seed=N tick=0. Fail only if the method is missing, the schema is wrong, or count > 0 and the assertion is unmet.

## Notes (determinism + bookkeeping)

Content registry dual-source invariant (confirmed 2026-03-03):
  - docs/content/content_registry_v0.json and SimCore/Content/ContentRegistryLoader.DefaultRegistryJsonV0
    (embedded C# string) must always be kept in sync.
  - ContentRegistryV0_LoadTwice_DigestAndOrderingStable_AndEmitDigestReport computes digests from both
    and asserts equality. Any registry content change requires updating BOTH files or the test fails.
  - The embedded string uses id-only module entries ({ "id": "..." }) while the docs JSON has full fields;
    only the parsed IDs affect the digest, so format differences are acceptable.

- All harness outputs must be deterministic: no timestamps, stable ordering, stable formatting; stdout%stderr hashes must be stable across reruns on unchanged repo state.
- Any determinism repro or regression artifact must include Seed (and TickIndex where applicable).
- Session log is authoritative: update docs/56_SESSION_LOG.md and docs/55_GATES.md in the same change set as the PASS entry.

Session log entry format:
  - DATE, branch, GATE.X.Y.001 PASS (description). [Fix: <what broke and how it was resolved>.] Evidence: <files>.
  - The Fix: field is required whenever a correction was made during the gate: wrong assertion, wrong API name,
    wrong constant value, build step missing, behavioral misunderstanding, etc.
  - Omit Fix: only when implementation matched the gate intent exactly with no corrections.
  - Fix: goes between the description and Evidence:, on the same line.

## Localization (L10N) — GATE.S9.L10N.DECISION.001

**Decision: English-only for v1.0 (EA launch).**

Rationale: Single developer, narrative-heavy game with lore-specific terminology
(Communion, Fracture, Lattice Drones, Adaptation Fragments). Translation quality
matters for story impact. Defer L10N to post-EA based on community demand.

### Current string audit (2026-03-13)

| Category | Count | Location |
|----------|-------|----------|
| UI label assignments (`.text = "..."`) | ~456 | `scripts/ui/`, `scripts/view/` |
| Toast/notification strings | ~50 | `scripts/ui/toast_system.gd`, various |
| SimBridge display names | ~80 | `scripts/bridge/SimBridge.*.cs` |
| Content registry names | ~200 | `SimCore/Content/*ContentV0.cs` |

Total: ~786 hardcoded English strings.

### Extraction-ready patterns for future L10N

When L10N is added post-EA:

1. **GDScript**: Replace `label.text = "MARKET"` with `label.text = tr("MARKET")`.
   Godot's built-in `tr()` function reads from `.po`/`.csv` translation files.
2. **SimBridge display names**: Route through a `DisplayNameRegistry` that maps
   internal IDs to localized strings.
3. **Content registry**: Add `display_name_key` field to content definitions,
   resolve via translation table.
4. **Lore text** (data logs, FO dialogue, mission descriptions): Keep in separate
   translation files organized by content type.
5. **Number/date formatting**: Use `CultureInfo` in SimBridge for locale-aware
   number formatting.

### Do NOT localize

- Gate IDs, debug logs, test assertions, console output
- Internal entity IDs and content type strings
- File paths and JSON keys

## Steam Integration — GATE.S9.STEAM.SDK.001

**Addon**: GodotSteam 4.x (GDExtension). Not yet installed — placeholder wiring only.

### Setup steps (when ready to install)

1. Download GodotSteam 4.x from https://godotsteam.com/
2. Extract to `addons/godotsteam/`
3. Enable in Project Settings → Plugins
4. Replace placeholder App ID (`480` = Spacewar test app) in `steam_appid.txt`
   with actual Steam App ID from Steamworks partner dashboard
5. Rebuild: `dotnet build "Space Trade Empire.csproj" --nologo`

### Runtime behavior

- `game_manager.gd::_init_steam_v0()` initializes Steam on startup
- Graceful fallback: if GodotSteam addon not present or Steam client not running,
  `_steam_enabled = false` and game runs normally
- Check `game_manager.is_steam_enabled()` before calling Steam APIs
- `steam_appid.txt` must be in project root for dev builds (Steam ignores it for
  shipped builds — the app ID comes from the Steam client)

### Files

- `steam_appid.txt` — placeholder App ID (480 = Spacewar test app)
- `scripts/core/game_manager.gd` — `_init_steam_v0()`, `is_steam_enabled()`
