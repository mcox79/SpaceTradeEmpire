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

3) `docs/10_ARCHITECTURE_INDEX.md`
- Router into the canonical architecture body.
- Do not attach the full architecture by default.

4) `docs/20_TESTING_AND_DETERMINISM.md`
- Testing contract and determinism requirements.

5) `docs/30_CONNECTIVITY_AND_INTERFACES.md`
- Layering rules and connectivity scan contract.

6) `docs/40_TOOLING_AND_HOOKS.md`
- Canonical command lines for tools and validations.
- Hook behavior, installation expectations, and attachment rules.

7) `docs/90_GLOSSARY_AND_IDS.md`
- Canonical vocabulary and stable IDs used across docs and code.


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


## How to start a new LLM session

1) Generate the Context Packet:
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Force -Verbose`

2) Optional but recommended: run the connectivity scan (especially if you changed boundaries):
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Force -Harden`

3) In the new chat:
- attach `docs/generated/01_CONTEXT_PACKET.md`
- attach only the allowlisted file contents required for the scoped change
- attach `docs/generated/connectivity_violations.json` only if needed for diagnosis


## Repo hygiene note

If you see warnings about CRLF to LF normalization, standardize line endings via repo policy (for example `.gitattributes` and/or `.editorconfig`) so docs diffs remain stable and tool outputs remain deterministic.
