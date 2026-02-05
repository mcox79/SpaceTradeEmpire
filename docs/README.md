# Docs

This directory contains the canonical documentation kernel for Space Trade Empire.

The kernel is designed for:
- LLM-driven development sessions with minimal attachments
- deterministic, repeatable tooling outputs
- a clear separation between canonical project docs and generated per-session artifacts


## Start here (canonical order)

1) `docs/00_READ_FIRST_LLM_CONTRACT.md`
- Authoritative workflow contract: OUTPUT_MODE, GIT_MODE, atomic write rules, constraints.

2) `docs/10_DOCS_INDEX.md`
- Router for all docs and what is canonical vs generated.

3) `docs/90_GLOSSARY_AND_IDS.md`
- Canonical vocabulary, stable IDs, and naming rules used across docs and code.
- Glossary definitions override conflicting usage elsewhere.

4) `docs/21_UNITS_AND_INVARIANTS.md`
- Canonical units, normalization rules, invariants, and validator expectations shared by SimCore, UI, and tests.

5) `docs/52_DEVELOPMENT_LOCK_RECOMMENDATIONS.md`
- Locked time model (tick = 1 game minute, 60x), tick-boundary authority, intent ordering, travel timing targets, market publish cadence, intel policy, and slice gates.

6) `docs/53_PROGRAMS_FLEETS_DOCTRINES_CONTROL_SURFACE.md`
- Canonical player control surface: Programs, Fleets as capacity pools, Doctrines, Upgrade Packages, Liaison Quote contract, planning cadence, Explain JSON contract.
- No ship UI, no per-ship fitting, no waypointing.

7) `docs/51_ECONOMY_AND_TRADE_DESIGN_LAW.md`
- Canonical economy laws: causality loop, ledger/event rules, logistics constraints, automation doctrine, enforcement/heat, security state, determinism harness, invariants, and scenario packs.

8) `docs/50_EARLY_MISSION_LADDER_AND_TRAVEL.md`
- Canonical early mission ladder and travel laws: reusable mission primitives, system unlock ladder, lanes vs off-lane guardrails, fracture constraints, deterministic mission tests.

9) `docs/20_TESTING_AND_DETERMINISM.md`
- Testing contract and determinism requirements (world hash, save/load equality, scenario packs, RNG stream partitioning).

10) `docs/30_CONNECTIVITY_AND_INTERFACES.md`
- Layering rules and connectivity scan contract.

11) `docs/40_TOOLING_AND_HOOKS.md`
- Canonical command lines for tools and validations.
- Hook behavior, installation expectations, and attachment rules.

12) `docs/10_ARCHITECTURE_INDEX.md`
- Router into the canonical architecture body.
- Do not attach the full architecture by default.
- Do not use architecture excerpts to answer design-law questions.

13) `docs/ArchitectureV7.4.txt`
- Canonical architecture body (attach only when absolutely necessary; prefer excerpt routing via the index).


## Generated artifacts

All generated artifacts live under:
- `docs/generated/`

Key generated artifacts:
- `docs/generated/01_CONTEXT_PACKET.md` (default session attachment)
- `docs/generated/connectivity_manifest.json`
- `docs/generated/connectivity_graph.json`
- `docs/generated/connectivity_violations.json`

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

1) Generate the Context Packet:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Force -Verbose`

2) Optional but recommended: run the connectivity scan (especially if you changed boundaries):
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force -Harden`

3) In the new chat:
- attach `docs/generated/01_CONTEXT_PACKET.md`
- attach only the allowlisted file contents required for the scoped change
- attach `docs/generated/connectivity_violations.json` only if needed for diagnosis

Rule:
- Do not attach the full architecture by default. Use `docs/10_ARCHITECTURE_INDEX.md` and attach only the excerpt(s) needed.


## Repo hygiene note

If you see warnings about CRLF to LF normalization, standardize line endings via repo policy (for example `.gitattributes` and/or `.editorconfig`) so docs diffs remain stable and tool outputs remain deterministic.
