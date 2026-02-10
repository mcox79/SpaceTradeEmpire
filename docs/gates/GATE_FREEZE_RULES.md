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

## Attachments cap rule (LLM sessions)

- Max 6 attachments per LLM session, excluding docs/generated/01_CONTEXT_PACKET.md.
- The attachments list is written to docs/generated/llm_attachments.txt.
