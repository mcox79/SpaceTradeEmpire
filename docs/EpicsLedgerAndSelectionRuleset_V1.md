Epics Ledger and Selection Ruleset v1.0
0) Intent of this ruleset

This ruleset governs:

how to edit docs/54_EPICS.md safely

what is allowed to be changed in epics vs what must be changed in gates

how deterministic selection chooses the next work

how epic edits translate into gates without drifting into tooling chores

If anything here conflicts with repo contracts or freeze rules, freeze rules win.

1) Document roles and authority
1.1 What docs/54_EPICS.md is for

Execution order and tracking ledger.

A human-readable roadmap that is machine-scannable for selection.

It must remain stable enough for deterministic parsing.

1.2 What docs/54_EPICS.md is not for

It is not the source of truth for completion.

It should not hardcode implementation paths, touched files, or evidence output paths.

It should not include duplicated epic definitions in multiple places that can confuse parsers.

1.3 Source of truth hierarchy (hard)

When editing epics, treat these as the truth order:

docs/56_SESSION_LOG.md: PASS entries are the ground truth of “done”

docs/55_GATES.md: authoritative status ledger, must match session log

docs/gates/gates.json: authoritative machine registry for gate IDs and statuses

docs/54_EPICS.md: summary and plan derived from gates, plus future TODO items

Rule: If there is mismatch, fix docs/55_GATES.md and/or gates.json first, then update epics as a summary.

2) The canonical selection surface in epics.md
2.1 Single authoritative list (hard)

Only ## Canonical Epic Bullets is eligible for deterministic selection and scanning.

Every EPIC ID must appear exactly once in Canonical Epic Bullets.

Tooling MUST NOT scan below that section for selection purposes.

2.2 Slice sections are informational (hard)

Slice sections may explain intent, dependencies, and scope.

Slice sections MUST NOT be treated as selection input.

If slice sections contain epic bullets, they are non-authoritative duplicates and risk parser mistakes.

Recommended: slice sections list EPIC IDs only (no status token, no (gates: ...)).

3) Epic bullet requirements
3.1 Required canonical epic bullet format (hard)

Each canonical bullet must be:

- EPIC.<...> [TODO|IN_PROGRESS|DONE|DEFERRED]: <description> (gates: <selector or list>)

3.2 Status tokens (hard)

Status tokens must be exactly: TODO, IN_PROGRESS, DONE, DEFERRED.

3.3 Gate binding (hard)

Each epic must include (gates: ...).

If you introduce a new epic whose gates do not yet exist, it still needs a gate selector prefix that will exist later.

If no gate selector exists, the epic cannot ever be marked DONE.

3.4 Evidence rule (hard)

Epic descriptions state evidence needed, not the evidence path.

Do not write emits docs/generated/... in epics.

Evidence paths are chosen during gate authoring and recorded in docs/55_GATES.md.

Good: “evidence: deterministic proof report, stable ordering, no timestamps”
Bad: “emits docs/generated/exploration_momentum_proof_v0.txt”

4) Status computation rules (how epic statuses mean something)

If the epic has (gates: ...):

DONE: all matched gates are DONE

IN_PROGRESS: some matched gates are DONE or IN_PROGRESS, but not all DONE

TODO: all matched gates are TODO

DEFERRED: used only if intentionally postponed. Must include a short rationale in the epic description.

Rule: Never mark an epic DONE because it “feels done.” Only gates can prove DONE.

5) What is safe to edit in epics.md vs what must be done elsewhere
5.1 Safe edits in epics.md

Reordering canonical bullets to change roadmap order

Adding new epic bullets (with unique EPIC IDs and gate selectors)

Improving descriptions so they translate cleanly into gate-shaped work

Adding or tightening contracts and slice scope language

Adding evidence-needed language (no paths)

5.2 Not safe in epics.md (do these in gates instead)

Changing completion status that conflicts with gates or session log

Inventing or changing gate IDs without updating gates.json and gates.md

Adding concrete file paths or touched-file lists

Naming evidence output file paths

6) Deterministic selection and segmentation process

This is the process used when the LLM is asked “choose the next work.”

6.1 Step 1: Segmentation (path-free)

Inputs:

docs/54_EPICS.md

docs/gates/gates.json and schema

freeze rules

Selection:

Choose TARGET_EPIC as the first TODO or IN_PROGRESS epic in Canonical Epic Bullets,
excluding EPIC.X.* and EPIC.S0.* unless explicitly overridden.

Output:

6 to 10 candidate gate-shaped items (target 8)

Each item includes:

Proposed gate ID

GateType (CONTRACT, CORE_LOGIC, DETERMINISM, UI_MIN, EXPLAINABILITY, SCENARIO_PROOF)

Axis (ordering, serialization, id_stability, replay_diagnostics, rng_streams, time_sources)

Primary deliverable (test or command)

Failure mode + explain surface

Evidence needed (no paths)

New-files budget (none, 1, 2_plus)

An explicit order and dependency rationale

Hard constraint:

No repo file paths.

No file-map anchor proofs.

6.2 Slice completion preference (hard)

Segmentation must prefer completing a coherent tranche when possible within 6 to 10 items.
A coherent tranche includes at minimum:

1 CORE_LOGIC gate

1 UI_MIN or SCENARIO_PROOF gate

1 EXPLAINABILITY gate

enough CONTRACT or DETERMINISM work to be regression-safe

If completing the tranche exceeds 10 items:

prioritize reaching the first SCENARIO_PROOF gate and defer the rest

6.3 Step 2: Gate authoring (paths and NEW files)

Inputs:

confirmed candidate gate set

docs/generated/01_CONTEXT_PACKET.md (file map)

docs/55_GATES.md

gates.json and schema

freeze rules

Output per gate:

concrete touched paths (FOUND or NEW)

proof commands

evidence outputs (now paths are chosen)

determinism notes

failure mode + explain surface

copy/paste docs/55_GATES.md rows

file map proof for anchor paths

NEW file rationales

NEW file policy:

max NEW per gate and per batch

each NEW path must be justified: why an existing harness cannot be extended

7) Practical editing checklist for epics.md sessions

When modifying docs/54_EPICS.md, the LLM must ensure:

Canonical Epic Bullets remains the only selection surface.

No duplicate EPIC IDs in Canonical Epic Bullets.

Every epic bullet has (gates: ...).

Epic text describes evidence needed but does not hardcode file paths.

If an epic is marked DONE or IN_PROGRESS, gates.json must support it.

Slice sections do not introduce conflicting epic definitions.

Changes do not break deterministic parsing (no truncation, no placeholders, no malformed tokens).
