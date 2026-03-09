# Gate Freeze Rules (MVP)

Hard guardrails (enforced by Validate-Gates.ps1):
- Gate IDs are immutable once merged.
- No deletions by default.
- Planning commits are separate from execution commits.
- DevTool must continue generating docs/generated/01_CONTEXT_PACKET.md. New prompt outputs are additive.

## What "freeze" means in MVP

For any gate that existed at the baseline git ref (default: HEAD~1):

Immutable fields:
- id
- title
- created_utc

Allowed changes:
- status
- updated_utc
- priority
- tags
- owner
- evidence (additive or edits allowed, but still schema-valid)

No deletions:
- Any gate present at baseline must still exist.

## Registry scope in MVP

- docs/gates/gates.json contains only the next 10 to 25 active gates.
- No epic compiler.
- Validator checks schema + freeze rules only.
- No requirement to reconcile docs/55_GATES.md in MVP.

## NEW file path limits

When generating gates, paths not already in the context packet file map must be
prefixed with `NEW:` and are subject to these limits:

- Max **3 NEW paths per gate**.
- Max **10 NEW paths per tranche** (entire batch).
- Each NEW path must include a 1-sentence rationale explaining why no existing
  path works.

These limits prevent gate generation from over-speculating on file structure.

## Hash-affecting gate ordering

Gates with `hash_affecting: true` in the same tier share golden hash baselines.
Each hash-affecting gate changes the expected hash for the next.

- Multiple hash-affecting gates **within the same tier** must form a dependency
  chain via `blocks` (each gate except the first must block on another
  hash-affecting gate in that tier).
- Validate-Gates.ps1 enforces this: N hash-affecting gates in one tier require
  N-1 blocking edges.
- Non-hash-affecting gates can fully parallelize with no coordination.

## Attachments cap rule (LLM sessions)

- Max 6 attachments per LLM session, excluding docs/generated/01_CONTEXT_PACKET.md.
- The attachments list is written to docs/generated/llm_attachments.txt.
