# Tweak Routing Policy (Binding)

This policy is binding for all LLM-generated code changes.

## Scope

Applies to all code under `SimCore/` and all changes proposed by an LLM.

Goal: keep gameplay-affecting values centralized so balancing can happen later without hunting magic numbers.

## Definitions

Numeric literal includes any hardcoded number: integers, floats, hex, negative values, 0, 1, sizes, thresholds, weights, probabilities, caps, and loop bounds.

SimCore means any file under `SimCore/` including `SimCore/Gen/` and `SimCore/Systems/`.

Tweaks-routed means the value is sourced from an approved versioned Tweaks type under `SimCore/Tweaks/*V0.cs` and passed into the logic from that surface. It is not redefined as a constant or literal at the use site.

Gameplay-affecting includes anything that can change simulation outcomes or balance, including but not limited to: worldgen classification thresholds, weights, caps, probabilities, pass/fail thresholds, risk or economic parameters, inventory or market behaviors, scoring or routing quantization, candidate selection limits, and any value used to bucket, classify, or rank.

Structural means it cannot change simulation outcomes. It may affect performance, IO chunking, formatting, logging, reflection flags, or internal buffer sizing only.

## Approved Tweaks roots

Approved Tweaks roots:
- `SimCore/Tweaks/` only

Any new gameplay-affecting knob must be added to a versioned type under this folder (example: `WorldGenTweaksV0.cs`).

Do not create alternate constants classes, new tweak roots, or “Config” holders elsewhere as a substitute.

## Canonical representation

### Gameplay-affecting values (required)
- Must be Tweaks-routed from `SimCore/Tweaks/*V0.cs`.
- Do not introduce new gameplay-affecting literals or constants outside Tweaks.

### Structural constants (allowed, narrow)
Outside `SimCore/Tweaks/`, structural constants are allowed only as:
- `private const` or `internal const`
- name must start with `STRUCT_`
- declaration line must include `STRUCTURAL:`
- semantic name only (no numeric aliases)
- prefer declaring in the same file near first use

Example:
`private const int STRUCT_HASH_CHUNK_BYTES = 4096; // STRUCTURAL: hashing IO chunk size; not gameplay`

## Rule 1: No new numeric literals in SimCore by default

Introducing new numeric literals anywhere in SimCore is forbidden unless it matches one of the approved exceptions below.

If the tweak routing guard reports a violation, treat the guard output as authoritative.

## Approved exceptions in SimCore

A new numeric value is allowed in SimCore only if it is exactly one of:

### A) Tweaks-routed gameplay knob
Any gameplay-affecting value must be routed through Tweaks.

### B) STRUCTURAL const (non-gameplay)
Allowed only as a structural constant that matches the Canonical representation rules above.

## Rule 2: Gate evidence emitters should not live in SimCore

If a report emitter exists only to satisfy a gate or generate evidence, implement it in `SimCore.Tests` or tooling.

Only keep an emitter in SimCore if it is part of a runtime gameplay or debugging surface explicitly approved in a canonical doc.

Canonical approval doc:
- `docs/DEBUG_SURFACES.md`

If `docs/DEBUG_SURFACES.md` does not exist or does not list the emitter, treat it as unapproved and put the emitter in tests or tooling.

## Rule 3: Tests are the default home for evidence and formatting literals

In `SimCore.Tests`, literals are allowed for:
- harness configuration
- report formatting
- invariant thresholds that assert stability or correctness

Tests must not encode balance targets as literals.

## Forbidden representations (outside Tweaks)

Forbidden outside `SimCore/Tweaks/`:
- numeric alias constants like `One`, `Zero`, `Thousand`, `Million`, `N1`, `N2`, etc.
- `static readonly` numeric constants used as a substitute for Tweaks routing
- creating new ad hoc constants classes outside the approved Tweaks root
- using `public const` for new constants (gameplay or structural)

## Legacy tracking (visibility for cleanup)

The repo currently contains pre-existing constants outside `SimCore/Tweaks/` (example: `public const` values in `SimCore/SimState.cs`, `Market.cs`, `GalaxyGenerator.cs`, and others).

These are legacy patterns. Do not copy them into new code.

The guard will produce a deterministic report of legacy constants so they can be migrated gradually without blocking unrelated work.

Do not migrate legacy constants as a side effect. Only migrate legacy constants when a gate explicitly calls for it, and keep the migration confined to the touched subsystem.

## When the guard fires (mandatory procedure)

You must do one of the following, in this priority order:

1) Move the feature into tests or tooling so SimCore gains no new literals.
2) Route the value through Tweaks.
3) Use the STRUCT_ % STRUCTURAL structural const convention (structural only).
4) If your repo has an explicit allowlist mechanism, add the value there only for STRUCTURAL const cases.

If you do not have the violation report and the exact offending excerpt with line numbers, do not propose a patch. Request those artifacts.
