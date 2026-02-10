# Planning Mode: Build out gates from epics

You are in Planning Mode only.

## Goal
Expand `docs/gates/gates.json` (and optionally `docs/55_GATES.md` as a human mirror) by deriving missing gates from `docs/54_EPICS.md`.

## What to attach (only these)
1) `docs/generated/01_CONTEXT_PACKET.md` (so you know current repo state and what is already done)  
2) `docs/54_EPICS.md`  
3) `docs/gates/gates.json`  
4) `docs/gates/gates.schema.json`  
5) `docs/gates/GATE_FREEZE_RULES.md`  
6) `docs/55_GATES.md` (only if you want the human mirror updated)

Do not request or use any other files.

## Hard constraints
- Do not propose new features.
- Do not expand scope beyond translating epics into executable gates.
- In this planning session, only propose edits to `docs/gates/gates.json` and optionally `docs/55_GATES.md`. No other files.
- Gate IDs are immutable once created.
- Do not delete any existing gate IDs.
- Splits must be done by adding new subgates; parent remains.
- Keep gates granular enough to execute one gate per conversation.
- Cap: each gate should be executable with <= 6 attachments (excluding `docs/generated/01_CONTEXT_PACKET.md`). If not, split it.

## Gate ID issuance (required)
- New IDs must follow: `GATE.<EPIC>.<NNN>`
- `<EPIC>` is a stable short code derived from the epic (reuse existing epic codes if present).
- `<NNN>` is the next unused number in that epic namespace based on existing `docs/gates/gates.json`.
- Output an **ID allocation** section listing every new ID and how you chose its number.
- Do not reuse numbers. Do not collide with existing IDs.

## Gate content requirements (each new gate must include)
- `id`, `title`, `scope`, `priority`, `status` (TODO), `definition_of_done`, `evidence` (paths with kind), and `parent_id` if it is a subgate.

## Definition of done (DoD) rules
- DoD must be checkable and specific.
- DoD must be expressible as evidence in one of: `[test]`, `[code]`, `[doc]` paths.
- Prefer wording that is objectively verifiable (e.g., specific tests, specific doc section updates).

## Evidence path rules (required)
- Evidence paths must be repo-relative.
- Evidence paths must either:
  1) already exist in the repo, OR
  2) be explicitly created by this gate’s DoD (include “create <path>” in DoD).
- Do not invent evidence paths without putting them in DoD.
- For each evidence path, include a short reason why it proves the gate is DONE.

## Output required
1) **ID allocation**: list each new gate ID and the chosen `<NNN>` with a collision-check statement.
2) **Minimal patch plan**: exact JSON objects to add to `docs/gates/gates.json` (do not rewrite unrelated entries).
3) For each new gate: justify why it is one-conversation sized and list the minimal evidence paths.
4) If updating `docs/55_GATES.md`: provide exact insertion text and exact location.
