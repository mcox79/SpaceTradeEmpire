\# Docs Kernel



This folder contains the canonical documentation kernel used to keep LLM-driven development repeatable and low-friction.



\## What belongs here

This directory contains the highest-priority, rarely-changing rules and indexes.



Core (always authoritative):

\- `00\_READ\_FIRST\_LLM\_CONTRACT.md`

\- `10\_DOCS\_INDEX.md`

\- `10\_ARCHITECTURE\_INDEX.md`

\- `20\_TESTING\_AND\_DETERMINISM.md`

\- `30\_CONNECTIVITY\_AND\_INTERFACES.md`

\- `40\_TOOLING\_AND\_HOOKS.md`

\- `90\_GLOSSARY\_AND\_IDS.md`



Templates:

\- `templates/01\_CONTEXT\_PACKET.template.md`



\## What should NOT be attached every chat

Do not attach large repo dumps or large architecture documents by default.



Instead:

\- Keep the canonical architecture document in this repo (recommended: `docs/ArchitectureV7.4.txt`).

\- Attach a small per-chat Context Packet generated from the template, plus only the minimum relevant files/modules.



\## How to start a new coding chat

Provide:

1\) A filled `01\_CONTEXT\_PACKET.md` (based on the template)

2\) Any file contents referenced in the packet that are required to edit (only the scoped files)



The packet must include:

\- Objective

\- OUTPUT\_MODE and GIT\_MODE

\- Explicit allowed file list

\- Validation commands

\- Definition of Done



\## Generated artifacts (recommended)

Create a folder `docs/generated/` for deterministic, diffable outputs produced by tools:

\- connectivity graph outputs

\- test run reports

\- repo maps/changelogs



These outputs should be referenced by the Context Packet, not pasted into prompts.



