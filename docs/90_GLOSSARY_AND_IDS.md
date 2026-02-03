\# 90\_GLOSSARY\_AND\_IDS



Canonical vocabulary and stable identifiers referenced across docs and code.



This file exists to prevent drift and ambiguous terminology.





\## A. Workflow modes



OUTPUT\_MODE

\- Meaning: how the LLM must output changes in a coding session.

\- Values:

&nbsp; - ANALYSIS\_ONLY: no code or file replacements; audit and plan only.

&nbsp; - FULL\_FILES: output complete file replacements (one file at a time).

&nbsp; - POWERSHELL: output PowerShell-only atomic write blocks that generate or replace files.

\- Rule: the session Context Packet must declare OUTPUT\_MODE.



GIT\_MODE

\- Meaning: how Git actions are handled in the session workflow.

\- Values:

&nbsp; - NO\_STAGE: do not stage or commit anything in-session.

&nbsp; - STAGE: staging is allowed once validation passes.

&nbsp; - COMMIT:<message>: commit is allowed once validation passes with the provided message.

\- Rule: the session Context Packet must declare GIT\_MODE.



Experimentation mode

\- Meaning: exploratory changes and audits. Default Git mode is NO\_STAGE. No commits.

\- Switching rule: move to Normal mode only after the workflow is stable, outputs are deterministic, and the plan is explicit.



Normal mode

\- Meaning: standard development, validation, and integration.

\- Git mode: STAGE or COMMIT:<message>.





\## B. Validation tiers



Tier 0

\- Meaning: fast checks that should run whenever relevant files change.

\- Typical examples:

&nbsp; - `.gd` validation and parse gate (`scripts/tools/Validate-GodotScript.ps1`)

&nbsp; - PowerShell parse checks (when `.ps1` changes)

&nbsp; - `dotnet build` (when `.cs` changes)



Tier 1

\- Meaning: correctness gates when wiring or simulation changes occur.

\- Typical examples:

&nbsp; - connectivity scan (`scripts/tools/Scan-Connectivity.ps1`)

&nbsp; - `dotnet test` for relevant test projects (for example `SimCore.Tests/`)

&nbsp; - smoke tests (when present)



Tier 2

\- Meaning: slower runs intended for CI/nightly or intentional regression detection.

\- Typical examples:

&nbsp; - multi-seed determinism regressions

&nbsp; - performance regressions

&nbsp; - long-horizon scenario runs





\## C. Session artifacts



Context Packet (generated)

\- Path: `docs/generated/01\_CONTEXT\_PACKET.md`

\- Meaning: default attachment for new LLM sessions.

\- Must include:

&nbsp; - objective

&nbsp; - OUTPUT\_MODE and GIT\_MODE

&nbsp; - explicit allowlist of files to modify

&nbsp; - validation commands

&nbsp; - definition of done



Context Packet template

\- Path: `docs/templates/01\_CONTEXT\_PACKET.template.md`

\- Meaning: the template used to generate the Context Packet.

\- Rule: do not attach the template in sessions; attach only the generated packet.



Generated artifacts directory

\- Path: `docs/generated/`

\- Meaning: location for deterministic, diff-friendly tool outputs.



Connectivity scan outputs (v0)

\- Paths:

&nbsp; - `docs/generated/connectivity\_manifest.json`

&nbsp; - `docs/generated/connectivity\_graph.json`

&nbsp; - `docs/generated/connectivity\_violations.json`

\- Meaning:

&nbsp; - manifest: tool and scope summary (top-level keys include tool, scope, counts, total\_hits, files)

&nbsp; - graph: nodes and edges (top-level keys include tool, nodes, edges)

&nbsp; - violations: findings and rule list (top-level keys include tool, rules, violations, counts)

\- Session rule: attach `connectivity\_violations.json` only when non-empty or needed for diagnosis.





\## D. Architecture layer terms



SimCore

\- Meaning: headless simulation engine. Must not depend on Godot runtime objects or Godot namespaces.



GameShell

\- Meaning: Godot-facing application layer. May depend on SimCore.



Adapter

\- Meaning: glue layer allowed to touch both GameShell and SimCore. Used to bridge runtime and translate inputs/outputs.





\## E. Determinism terms



Deterministic

\- Meaning: repeated runs with the same deterministic inputs produce identical outputs (as defined by `docs/20\_TESTING\_AND\_DETERMINISM.md`).



Deterministic inputs

\- Meaning: explicit, serializable inputs to a simulation run (seed, scenario/config, command list).



Diff-friendly artifacts

\- Meaning: outputs whose ordering and formatting are stable, so Git diffs are meaningful.



Ephemeral logs

\- Meaning: outputs that are not required to be deterministic and should not be committed (verbose traces, timestamped logs).





\## F. Stable identifiers (general)



EntityID

\- Meaning: stable identifier for a game entity in SimCore.

\- Requirement: must be stable within a deterministic run and suitable for referencing in logs and UI.



ReasonCode

\- Meaning: stable code describing why an outcome occurred (failure, rejection, insufficient resources).

\- Requirement: stable and UI-displayable; preferred over free-form strings for critical outcomes.



