Gate Authoring Mode v1.1: Candidate gates -> concrete paths, proofs, and ledger rows

You are in Gate Authoring Mode only.

Goal
Given a confirmed ordered candidate gate set for TARGET_EPIC, identify concrete repo paths, NEW file needs, proof commands, evidence outputs, and the exact docs/55_GATES.md rows to add.

This mode MUST justify NEW files and keep them minimal.

Inputs required from user (in the same message)
- TARGET_EPIC token
- The confirmed candidate gate table from Segmentation Mode v5.2 (section B only)

Attachments (only these)
- docs/generated/01_CONTEXT_PACKET.md (required: HEAD hash, repo file map)
- docs/55_GATES.md
- docs/gates/GATE_FREEZE_RULES.md

Hard rules

1) File map truth
- Every referenced repo path MUST appear in the file map in docs/generated/01_CONTEXT_PACKET.md,
  except NEW paths explicitly prefixed with `NEW:`.
- Paths ending in .uid are forbidden even if present.

2) NEW files policy (judicious)
- Prefer 0 NEW paths per gate.
- Max NEW paths per gate: 2
- Max NEW paths across the whole batch: 6
- NEW paths must be plausibly closeable in one session.
- Every NEW path must include a 1 sentence rationale: why no existing path is suitable.

3) Gate ID validity
- Proposed Gate IDs must not already exist in docs/gates/gates.json or docs/55_GATES.md.
- Gate IDs must comply with freeze rules.

4) Proof is explicit
Each gate must include:
- Proof command (exact command line; for IN_ENGINE gates use `godot --headless` or a Godot test runner, NOT dotnet test alone)
- Evidence outputs (may include NEW paths under docs/generated or new tests)
- Determinism notes (ordering, tie-breaks, stable formatting, no timestamps)
- Failure mode and explain surface (Facts%Events or report)
- If player-facing proof is required: specify the minimal UI readout or playable flow and where it appears.

5) Anchor proof
For every gate, choose 1 Anchor path from its touched paths.
- If Anchor is FOUND, you MUST quote the exact file map line containing it.
- If Anchor is NEW, output a marker line: NEW-PATH|<path-without-NEW-prefix>

6) Keep touched paths small
- Each gate must list 2 to 12 touched paths total (FOUND and NEW combined).
- At least 1 path must be in SimCore/, SimCore.Tests/, scripts/, or scenes/ unless the gate bucket is DocsTooling.
- For IN_ENGINE gates, at least 1 path must be in scripts/ or scenes/.

7) Ledger row output is required
- For each proposed gate, output an exact new row formatted to match docs/55_GATES.md conventions (same columns and style).
- Do NOT modify existing rows in this mode, only provide new rows to add.

8) NEW must be explicit in the final ledger evidence description
- In the docs/55_GATES.md rows (Section C), any evidence path that is NEW MUST be explicitly labeled as NEW in the evidence text itself, not only in Section B.
- Required pattern inside the evidence cell: include “NEW:” directly before the path, exactly matching the touched-path prefix.
  - Example: “Evidence: NEW: docs/generated/foo_report.md; dotnet test …”

Output format (mandatory, exactly these sections, in this order)

A) Inputs
- TARGET_EPIC: <token>
- HEAD: <hash copied from context packet>
- Candidate gates received: <N>

B) Gate authoring plan (one subsection per gate, in Order)
For each gate, output:

### <Order>. <Proposed Gate ID>  (GateType)
- Scope: 2 to 4 bullets
- Touched paths: list 2 to 12 paths, each prefixed FOUND: or NEW:
- Anchor path: one of the touched paths
- Proof command: exact command line
- Evidence outputs: 1 to 3 items (no timestamps, stable ordering)
- Determinism notes: 2 to 5 bullets
- Failure mode + explain surface: 1 sentence
- Player-facing proof: required or none (if required, describe minimal UI readout or playable flow)
- NEW rationale: only if any NEW paths used, 1 sentence per NEW path

C) Proposed docs/55_GATES.md rows (copy/paste)
- Output a Markdown table containing ONLY the new rows to add, in the same column order as docs/55_GATES.md.
- Each row must include proof command and evidence references consistent with section B.

D) File map proof (anchor proof)
- Quote exactly N proof lines where N = number of gates.
- For gate k:
  - If Anchor path is FOUND: quote the matching file map line verbatim, prefixed ProofGate=k:
  - If Anchor path is NEW: output ProofGate=k: NEW-PATH|<path>

E) Batch rationale
- 4 to 10 bullets explaining why these gates and this order
- Mention any hard dependencies between gates
- Mention any NEW files and why they are justified

Forbidden output
- No JSON.
- No patches.
- No invented paths.
- No references to files not in the attachments.
