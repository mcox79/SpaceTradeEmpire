# 30_CONNECTIVITY_AND_INTERFACES


## A. Purpose

Define:
- the rules for what is allowed to talk to what (dependency direction)
- how we generate an always-up-to-date connectivity map
- how the workflow treats violations

This document is the contract for:
- dependency direction rules across layers
- scanner outputs and how they are consumed
- what counts as a connectivity violation


## B. Layering rules (locked contract)

Hard rules:
- SimCore must not depend on Godot runtime objects or Godot namespaces.
- GameShell may depend on SimCore.
- Adapters are the only layer allowed to touch both sides.

Interpretation:
- Any dependency direction violation is an architecture bug, not a style issue.
- If a rule is unclear, treat it as forbidden until explicitly allowed here (or in the canonical architecture).


## C. Connectivity map (v0): current scanner behavior (what exists today)

The connectivity scanner v0 is a best-effort, file-level pattern scan. It emits deterministic, diff-friendly artifacts to:

- `docs/generated/`

### C1. Output files (v0)

1) `connectivity_manifest.json` (v0)
Top-level keys (verified):
- tool
- scope
- counts
- total_hits
- files

Required semantics:
- tool: identifies tool name and version
- scope: scan scope settings and applied excludes
- counts: counts of nodes, edges, and other summary counts owned by the tool
- total_hits: total pattern hits across scanned files
- files: per-file hit summary in stable ordering

2) `connectivity_graph.json` (v0)
Top-level keys (verified):
- tool
- nodes
- edges

Required semantics:
- nodes: file-level nodes (repo-relative paths) owned by the tool
- edges: file-level edges owned by the tool
- evidence is best-effort and must remain deterministic (repo-relative path and stable line references)

3) `connectivity_violations.json` (v0)
Top-level keys (verified):
- tool
- rules
- violations
- counts

Required semantics:
- rules: rule ids and descriptions owned by the tool
- violations: deterministic, sorted list of findings
- counts: summary counts by severity (at minimum errors; warnings allowed as the tool expands)

### C2. Exclusions (v0)

The scanner applies repo-relative exclusions. At minimum it excludes:
- `addons/`
- `_scratch/`
- `docs/generated/`
- `.git/`

Optional hardened excludes may be enabled via `-Harden` to reduce churn and improve signal. The exact list is owned by the tool and must remain deterministic.

Determinism status:
- Verified: repeated `-Harden` runs on an unchanged repo produced identical SHA256 hashes for `docs/generated/connectivity_*.json`.


## D. Determinism requirement for scan outputs (locked contract)

CONN-001 Deterministic outputs
- Repeated runs on an unchanged repo must produce byte-identical JSON outputs for:
  - `connectivity_manifest.json`
  - `connectivity_graph.json`
  - `connectivity_violations.json`

CONN-002 Deterministic inputs
- The scan must not depend on:
  - wall clock time
  - locale-specific sorting or formatting
  - machine-specific absolute paths
  - filesystem nondeterminism (ordering must be explicitly stabilized)

CONN-003 Diff-friendly JSON
- Stable ordering for:
  - node lists
  - edge lists
  - lists inside objects (exclusions, evidence arrays, per-file summaries)
- Stable keys and normalization (best-effort within PowerShell JSON constraints)

NOTE: do not include wall-clock timestamps inside deterministic artifacts. If a human timestamp is needed, write it to console output or to an ephemeral log file that is not part of the deterministic set.


## E. Connectivity violations (v0 rules, locked intent)

Violation categories:

1) Hard invariant violations (errors)
- Dependency direction violations that break the layering rules.
- SimCore references `Godot.` namespace anywhere in a SimCore file (error).

2) Best-effort findings (warnings)
- v0 may emit only errors today, but the system supports warnings when heuristic rules expand.
- Any warnings must be clearly labeled with rule id and severity.

Policy:
- errors must be fixed before considering a change complete
- warnings must be consciously accepted or fixed (document the decision in the session Context Packet)


## F. Planned extensions (v1 target, not yet required)

These items are desired, but not required for v0 and must not be assumed to exist unless implemented in tooling:

1) Enriched manifest fields
- commit hash and branch
- explicit tool flags affecting scan (example: `-Harden`)
- stable schema version for outputs

2) Richer node and edge metadata
- layer classification (SimCore | GameShell | Adapter | Tooling | Docs | Unknown)
- public API symbols (best-effort)
- events/signals emitted and consumed (best-effort)
- typed edges beyond simple pattern categories:
  - signal_connect
  - event_publish
  - event_subscribe
  - scene_instantiates
  - resource_load

If we promote any of the above into "locked contract", we must update the scanner in the same change-set.


## G. How the workflow uses the connectivity scan

When to run:
- Any time you change boundaries or wiring:
  - adapters and bridge code
  - event or signal wiring
  - scene instantiation or resource loading patterns
  - anything that might create cross-layer coupling

How to consume:
- Treat `connectivity_violations.json` as a review gate:
  - errors: must fix
  - warnings: must accept or fix (document in Context Packet)

What to attach in LLM sessions:
- Do not paste JSON outputs into chat by default.
- Attach or paste only:
  - `docs/generated/connectivity_violations.json` (if non-empty or needed for diagnosis)
  - small, targeted excerpts from `connectivity_graph.json` when diagnosing a specific edge
- Otherwise reference the outputs by path.


## H. Output directory policy (local now, commit later)

Default location:
- `docs/generated/`

Policy intent:
- Outputs are generated locally per session.
- Deterministic, diff-friendly artifacts are eligible for later commit.
- Ephemeral logs and verbose traces should remain uncommitted.
