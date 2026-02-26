Segmentation Mode v5.2: Epic -> gate candidates (path free, ordered, gate shaped)

You are in Segmentation Mode only.

Goal
Using ONLY the attached files, produce a reviewable, ordered candidate gate set for TARGET_EPIC that helps decide what gates to author next.

This mode MUST NOT guess repo file paths. File path selection is a separate step during gate authoring.

Attachments (only these)
- docs/54_EPICS.md
- docs/gates/gates.md
- docs/gates/GATE_FREEZE_RULES.md

Hard rules

1) TARGET_EPIC selection (deterministic)
Optional override:
- If the prompt contains a line `TARGET_EPIC_OVERRIDE: EPIC.<...>`, use that epic key token exactly and skip selection steps below.

Default selection (file order, deterministic):
- Locate the section `## Canonical Epic Bullets`.
- TARGET_EPIC is the FIRST epic in that section whose status is TODO or IN_PROGRESS,
  excluding epics whose key begins with EPIC.X. or EPIC.S0.
- If none match, choose the FIRST TODO or IN_PROGRESS epic anywhere in docs/54_EPICS.md (same exclusions).
- Use the epic key token exactly as written.

Optional ranked selection (only if the prompt contains `SELECTION_MODE: RANKED`):
- Consider all TODO or IN_PROGRESS epics in Canonical Epic Bullets (excluding EPIC.X and EPIC.S0).
- Compute a deterministic score per epic using ONLY docs/54_EPICS.md and gates.json:
  - +3 if epic has an explicit gates selector and at least 1 matching gate exists in gates.json
  - +2 if epic is in the same slice as the most recently completed non-EPIC.X gate family found in gates.json (best-effort)
  - +1 if epic text mentions a contract or proof requirement
  - Tie-break: earliest appearance in file
- Choose the highest score.

2) No path work in this mode
- Do NOT output repo file paths.
- Do NOT reference the context packet file map.
- Do NOT include attachment sketches, anchor paths, or file map proof.

3) No invented coverage claims
- Do not claim anything is DONE unless it is a gate present in docs/gates/gates.json with status DONE.
- It is acceptable to say "existing coverage unclear".

4) Candidate set size and closure
- Output exactly 6 to 10 candidate items (target 8).
- Each item must be closeable in one LLM session as a single gate authoring unit.
- If feasibility <= 2, do not include the item.

Slice completion preference (hard)

- Prefer selecting a gate set that can COMPLETE a coherent vertical slice tranche for TARGET_EPIC when feasible within 6 to 10 gates.
- A "coherent tranche" means the set includes:
  - at least 1 CORE_LOGIC gate,
  - at least 1 UI_MIN or SCENARIO_PROOF gate,
  - at least 1 EXPLAINABILITY gate,
  - and enough CONTRACT or DETERMINISM work to make the above regression-safe.
- If completing the tranche would exceed 10 gates, prioritize the minimal chain that reaches the first SCENARIO_PROOF gate and defer the rest.

1) Gate shaped items
Each candidate item MUST specify:
- Proposed Gate ID (plausible new gate ID, must not reuse an existing ID)
- GateType (see enum)
- One explicit failure mode
- One explain surface (Facts%Events or deterministic report)
- Evidence needed (no paths)

1) Ordering and rationale
- Provide a recommended execution order 1..N.
- For each item, include a dependency rationale (why this order, what it unblocks).

1) Foundation shoring is allowed but bounded
- There is NO forced axis diversity rule.
- If TARGET_EPIC is not DocsTooling and not EPIC.X:
  - At least 3 items must be CORE_LOGIC, UI_MIN, EXPLAINABILITY, or SCENARIO_PROOF.
  - No more than 2 items may be pure DETERMINISM or CONTRACT without new behavior.
- Prefer 2 distinct axes when it does not fight the work.

1) Player-facing proof policy
- If GateType is UI_MIN, EXPLAINABILITY, or SCENARIO_PROOF, the item MUST specify a player-facing proof requirement.
- Otherwise player-facing proof is optional.

1) Avoid preflight-only items
- Do NOT propose rerun Validate-Gates, rerun Repo-Health, regenerate status packets as candidate items,
  unless TARGET_EPIC bucket is DocsTooling and the output itself is being created or changed.

Enums

Axis (enum, pick exactly one):
- ordering
- serialization
- id_stability
- replay_diagnostics
- rng_streams
- time_sources

GateType (enum, pick exactly one):
- CONTRACT
- CORE_LOGIC
- DETERMINISM
- UI_MIN
- EXPLAINABILITY
- SCENARIO_PROOF

Primary deliverable (required per item):
- test
- command

Buckets (required per item)
Choose ONE bucket or TWO buckets joined by +:
- SimCore
- Tests
- ScriptsScenes
- DocsTooling
- DataGenerated

New files budget (required per item)
Choose one:
- none
- 1
- 2_plus

Player-facing proof (required per item)
Choose one:
- none
- ui_readout_min
- playable_flow_min
- optional

Evidence needed (required per item)
- Describe what must exist to prove the gate DONE, WITHOUT naming a path.

Output format (mandatory, exactly these sections, in this order)

A) Epic selection
- TARGET_EPIC: <epic key token>
- Why segmentation is needed: 2 to 5 bullets
- Existing related gates (optional): up to 8 gate IDs from docs/gates/gates.json that seem adjacent

B) Candidate gate set for TARGET_EPIC (ordered)
Markdown table with columns:
- Order (1..N)
- Proposed Gate ID
- Item label (6 to 12 words)
- GateType (enum)
- Axis (enum)
- Primary deliverable (test|command)
- Feasibility (1..5)
- Buckets (one bucket or two joined by +)
- New files budget (none|1|2_plus)
- Player-facing proof (none|ui_readout_min|playable_flow_min|optional)
- Failure mode + explain surface (single sentence)
- Evidence needed (no paths)

C) Gate order rationale
For each item in order, 1 to 2 bullets:
- why this gate now
- what it unblocks next

C2) Slice completion assessment
- 2 to 6 bullets stating:
  - whether this set completes a tranche
  - if not, which gates are deferred and why
  - what the natural next tranche would be

D) Questions for iteration
3 to 6 questions, each <= 14 words, targeted to split or merge decisions

Forbidden output
- No JSON.
- No patch plans.
- No file paths.
- No file map proof.
- No EPIC_B.
