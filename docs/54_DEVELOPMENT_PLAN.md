# 54_DEVELOPMENT_PLAN

This file has been split for the MVP transition.

- Epics and slice map: `docs/54_EPICS.md`
- Gate ledger (execution tracking): `docs/55_GATES.md`
- Session log (append only): `docs/56_SESSION_LOG.md`

Hard guardrails (unchanged):
- Gate IDs are immutable once merged
- No deletions by default (additive changes preferred)
- Planning commits separate from execution commits
- DevTool must continue generating `docs/generated/01_CONTEXT_PACKET.md`
- Any new prompt outputs are additive, not a replacement
