# Kernel Index (Canonical Entry Point)

This file replaces (archived originals):
- README in docs folder
- docs index file

Canonical entrypoint: this file (`docs/10_KERNEL_INDEX.md`).

## A. README (Historical snapshot)

# Docs

This directory contains the canonical documentation kernel for Space Trade Empire.

The kernel is designed for:
- LLM-driven development sessions with minimal attachments
- deterministic, repeatable tooling outputs
- a clear separation between canonical project docs and generated per-session artifacts


## Start here (canonical order)

1) `docs/00_READ_FIRST_LLM_CONTRACT.md`
- Authoritative workflow contract: OUTPUT_MODE, GIT_MODE, atomic write rules, constraints.

2) `docs/10_KERNEL_INDEX.md`
- Router for all docs and what is canonical vs generated (this file).

3) `docs/21_90_TERMS_UNITS_IDS.md`
- Canonical vocabulary, stable IDs, naming rules, units, and invariants used across docs and code.

4) `docs/50_51_52_53_Docs_Combined.md`
- Canonical gameplay and simulation design laws (docs 50%53 consolidated): early mission ladder, economy, locks, and control surface.

5) `docs/20_TESTING_AND_DETERMINISM.md`
- Testing contract and determinism requirements (world hash, save/load equality, scenario packs, RNG stream partitioning).

6) `docs/30_CONNECTIVITY_AND_INTERFACES.md`
- Layering rules and connectivity scan contract.

7) `docs/40_TOOLING_AND_HOOKS.md`
- Canonical command lines for tools and validations.
- Hook behavior, installation expectations, and attachment rules.

8) Gate governance v2.2: surfaces and when to attach
- Process contract: `docs/00_READ_FIRST_LLM_CONTRACT.md` (Pattern A steps A%B%C%D).
- Governance invariants (stable): `docs/gates/GATE_FREEZE_RULES.md` and `docs/gates/gates.schema.json`.
- Operational surfaces (attach only when the step requires them):
  - Ledger (canonical Gate specs): `docs/55_GATES.md`
  - Queue snapshot (derived Tasks window): `docs/gates/gates.json`
  - Completion log (append-only provenance): `docs/56_SESSION_LOG.md`
- Attachment routing summary (Context Packet always required):
  - Step A: `docs/generated/01_CONTEXT_PACKET.md` + `docs/54_EPICS.md` + `docs/55_GATES.md`
  - Step B: `docs/generated/01_CONTEXT_PACKET.md` + `docs/55_GATES.md` + `docs/gates/gates.json` + `docs/gates/gates.schema.json`
  - Step C: `docs/generated/01_CONTEXT_PACKET.md` + `docs/gates/gates.json` + task evidence files only
  - Step D: `docs/generated/01_CONTEXT_PACKET.md` + `docs/gates/gates.json` + `docs/56_SESSION_LOG.md` (+ `docs/55_GATES.md` excerpt if rollup update is needed)

9) `docs/10_ARCHITECTURE_INDEX.md`
- Router into the canonical architecture body.
- Do not attach the full architecture by default.
- Do not use architecture excerpts to answer design-law questions.

10) Architecture body (optional)
- If present in this repo, attach only when absolutely necessary; prefer excerpt routing via `docs/10_ARCHITECTURE_INDEX.md`.

## Generated artifacts

All generated artifacts live under:
- `docs/generated/`

Key generated artifacts:
- `docs/generated/01_CONTEXT_PACKET.md` (default session attachment)
- `docs/generated/02_STATUS_PACKET.txt` (compact snapshot: health, tests, key artifacts; includes top gate closure blockers when present)
- `docs/generated/connectivity_manifest.json`
- `docs/generated/connectivity_graph.json`
- `docs/generated/connectivity_violations.json`
- `docs/generated/gate_closure_delta.md` (parses docs/54 gate tables; extracts TODO gates and evidence paths; ranks missing evidence as “Top gate closure blockers”)
- `docs/generated/capability_index.md` (indexes UI + bridge capability surface; where APIs exist and where they are wired)


Policy:
- Generated artifacts are produced locally each session.
- Deterministic, diff-friendly artifacts may be committed later by policy decision.
- Ephemeral logs or verbose traces should remain uncommitted.


## Templates

Templates live under:
- `docs/templates/`

Important:
- do not attach `docs/templates/01_CONTEXT_PACKET.template.md` to sessions
- attach only the generated `docs/generated/01_CONTEXT_PACKET.md`


## How to start a new LLM session (minimal attachments)

1) Generate the Context Packet (and Status Packet if the pipeline runs it):
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Force -Verbose`

Optional (recommended when closing gates): generate gate closure delta + capability index so Status Packet can surface blockers:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-GateClosureDelta.ps1 -Force`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-CapabilityIndex.ps1 -Force`

2) Optional but recommended: run the connectivity scan (especially if you changed boundaries):
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force -Harden`

3) In the new chat:
- attach `docs/generated/01_CONTEXT_PACKET.md`
- optionally attach `docs/generated/02_STATUS_PACKET.txt` (recommended when closing gates; contains “Top gate closure blockers” if available)
- attach only the allowlisted file contents required for the scoped change
- attach `docs/generated/connectivity_violations.json` only if needed for diagnosis

Rule:
- Do not attach the full architecture by default. Use `docs/10_ARCHITECTURE_INDEX.md` and attach only the excerpt(s) needed.

## Repo hygiene note

If you see warnings about CRLF to LF normalization, standardize line endings via repo policy (for example `.gitattributes` and/or `.editorconfig`) so docs diffs remain stable and tool outputs remain deterministic.

## B. Docs Index (Historical snapshot)

# Docs Index (historical)

This is the routing index for the documentation kernel. It defines what is canonical (project-level) versus what is generated per session.

If a chat instruction conflicts with canonical repo contracts, update the canonical file first.

Design-law precedence rule:
- Gameplay and simulation laws live in the consolidated docs 50%53 document and override any conflicting interpretation from architecture excerpts. Use architecture excerpts to enforce layer boundaries and runtime integration patterns, not to redefine design laws.

## A. Canonical contracts, architecture, and design laws (project-level, highest priority)

1) `docs/00_READ_FIRST_LLM_CONTRACT.md`
- The authoritative workflow contract (OUTPUT_MODE, GIT_MODE, atomic write rules, constraints).

2) `docs/10_ARCHITECTURE_INDEX.md`
- The router into architecture excerpts.
- Do not attach the full architecture by default; use this index to select the minimum excerpt.

3) `docs/ArchitectureV7.4.txt`
- The canonical architecture body (referenced by excerpt via `docs/10_ARCHITECTURE_INDEX.md`).

4) `docs/20_TESTING_AND_DETERMINISM.md`
- Testing strategy, determinism expectations, runner patterns, and what is considered a valid test signal.

5) `docs/21_90_TERMS_UNITS_IDS.md`
- Canonical vocabulary, IDs, units, normalization rules, invariants, and validator expectations shared by SimCore, UI, and tests.

6) `docs/30_CONNECTIVITY_AND_INTERFACES.md`
- Layering rules and connectivity scan contract (including determinism requirements).

7) `docs/50_51_52_53_Docs_Combined.md`
- Canonical design laws for early mission ladder, travel, economy, locks, and control surface (docs 50%53 consolidated).

## B. Operational workflow (project-level, changes occasionally)

9) `docs/40_TOOLING_AND_HOOKS.md`
- Canonical command lines for tools and validations.
- What to attach per session vs what stays project-level.
- Hook behavior and installation expectations.

10) `docs/10_KERNEL_INDEX.md`
- The docs entrypoint and high-level orientation for humans.

11) Gate governance v2.2: surfaces and when to attach
- Process contract: `docs/00_READ_FIRST_LLM_CONTRACT.md` (Pattern A steps A%B%C%D).
- Governance invariants: `docs/gates/GATE_FREEZE_RULES.md` and `docs/gates/gates.schema.json`.
- Surfaces:
  - Ledger: `docs/55_GATES.md`
  - Queue: `docs/gates/gates.json`
  - Log: `docs/56_SESSION_LOG.md`
- Attach per step (Context Packet always required): see `docs/00_READ_FIRST_LLM_CONTRACT.md` Section D1.

## C. Templates (project-level)

11) `docs/templates/01_CONTEXT_PACKET.template.md`
- The template used by the generator.
- Do not attach this in sessions.

## D. Generated per session (always changes)

All generated artifacts live under:
- `docs/generated/`

12) `docs/generated/01_CONTEXT_PACKET.md`
- The default session attachment.
- Contains: objective, modes, allowlist, validations, definition of done.

13) `docs/generated/02_STATUS_PACKET.txt`
- Compact snapshot: health, validations, and presence/size of key generated artifacts.
- If `docs/generated/gate_closure_delta.md` exists, includes the “Top gate closure blockers” snippet.

14) `docs/generated/gate_closure_delta.md`
- Parses TODO gates from `docs/55_GATES.md`.
- Extracts evidence paths and reports missing referenced files.
- Ranks missing evidence as “Top gate closure blockers” (grouped by how many TODO gates reference each path).

15) `docs/generated/capability_index.md`
- Index of UI + bridge “capability surface” (APIs and wiring locations).
- Reduces time lost to “does the bridge already expose this?” and “where is this used?”

16) `docs/generated/connectivity_manifest.json`
17) `docs/generated/connectivity_graph.json`
18) `docs/generated/connectivity_violations.json`
- Generated by the connectivity scanner.
- Attach `connectivity_violations.json` only when it is non-empty or needed for diagnosis.
- Do not paste full JSON outputs into chat by default; reference by path and include only targeted excerpts when necessary.

## E. Planned outputs (not implemented unless explicitly created)

These names may be used later, but must not be referenced as if they exist until implemented:
- `docs/generated/02_CONNECTIVITY_MAP.md`
- `docs/generated/03_TEST_RUN_REPORT.md`
