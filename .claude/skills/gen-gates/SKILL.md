---
name: gen-gates
description: >
  Generate new gate definitions for Space Trade Empire and write them to
  docs/55_GATES.md and docs/gates/gates.json. Invoke with /gen-gates when
  you need to plan the next tranche of work from the epic backlog.
argument-hint: "[ANCHOR_EPIC_OVERRIDE: EPIC.xxx]"
---

# Gate Generation Skill

You are running the `/gen-gates` skill for Space Trade Empire. This replaces
the old multi-session pipeline (Segmentation + Authoring + IR Extraction +
copy-paste). You will do all three phases in this conversation, then write
directly to disk.

Check `$ARGUMENTS` for an optional `ANCHOR_EPIC_OVERRIDE: EPIC.<token>`. If
present, use that epic as the anchor instead of the default.

---

## STEP 1 — Read required files (do all reads in parallel)

Read these files before doing anything else:

1. `docs/54_EPICS.md` — epic backlog
2. `docs/55_GATES.md` — existing gate ledger (avoid ID collisions)
3. `docs/gates/GATE_FREEZE_RULES.md` — freeze rules
4. `docs/generated/01_CONTEXT_PACKET.md` — repo file map (HEAD sha + all paths)
5. `docs/gates/gates.json` — current queue (to understand what is already scheduled)

If `docs/generated/01_CONTEXT_PACKET.md` is missing or has no `head:` line,
stop and tell the user to click **"Refresh Context (Full)"** in DevTool (or run
`pwsh DevTool.ps1` and use the Generate Next Gates section).

---

## STEP 2 — Segmentation (produce candidate gate set)

### 2a. Anchor epic selection

- If `$ARGUMENTS` contains `ANCHOR_EPIC_OVERRIDE: EPIC.<token>`, use that.
- Otherwise: in `docs/54_EPICS.md`, under `## Canonical Epic Bullets`, find the
  FIRST epic whose status is TODO or IN_PROGRESS, excluding epics whose key
  starts with `EPIC.X.` or `EPIC.S0.`. That is ANCHOR_EPIC.

### 2b. Scope expansion

- Start with ANCHOR_EPIC. Expand to up to 3 additional TODO/IN_PROGRESS epics
  from the same or adjacent slices when they share SimCore systems, entities,
  or UI surfaces with ANCHOR_EPIC.
- Stop expanding when you have material for 6–10 gates.
- If ANCHOR_EPIC alone yields 6–10 realistic gates, do not expand.

### 2c. Gate sizing (hard)

- **Too small (merge up):** single field addition, one test assertion, moving a
  constant, renaming a type.
- **Right size:** one new SimCore system/subsystem; one new intent + command +
  contract test; one new SimBridge query + UI readout; one end-to-end scenario
  proof; a combined contract+core gate when the contract is trivial.
- **Too large (split):** touching >4 unrelated systems, or mixing SimCore logic
  + persistence + UI + scenario proof.

### 2d. Feasibility scoring

Score each candidate 1–5. Exclude scores ≤ 2.

- 5: Prior art exists, clear acceptance test, no ambiguous design decisions.
- 4: Pattern exists nearby, one bounded design decision.
- 3: New pattern, design decisions ambiguous but bounded, no blocking unknowns.
- 2: Depends on a gate not in this set and not already DONE. **Exclude.**
- 1: Blocking unknown or circular dependency. **Exclude.**

If exclusions drop the count below 6, expand to the next adjacent epic.

### 2e. Constraints

- Output exactly 6–10 candidates.
- At least one must reach a milestone: PLAYABLE_BEAT, HEADLESS_PROOF, or
  REGRESSION_ANCHOR. (PLAYABLE_BEAT requires godot --headless or a GDScript
  test that boots a scene. dotnet-test-only does NOT qualify.)
- At most 2 items may be pure CONTRACT or pure DETERMINISM.
- At least 3 must be CORE_LOGIC, UI_MIN, EXPLAINABILITY, SCENARIO_PROOF, or
  IN_ENGINE.
- If the last 3 completed non-EPIC.X gates in `docs/55_GATES.md` all have
  anchors exclusively in SimCore/ or SimCore.Tests/, at least 1 candidate must
  have its primary anchor in scripts/ or scenes/.
- Gate IDs must follow the pattern `GATE.[A-Z0-9_]+(\.[A-Z0-9_]+)*\.[0-9]{3}`
  and must not already exist in `docs/55_GATES.md` or `docs/gates/gates.json`.
- Do NOT propose: rerunning validators as standalone gates, doc-only updates,
  test-only additions with no behavior change, single-field schema additions.

### 2f. GateType enum

CONTRACT, CORE_LOGIC, DETERMINISM, UI_MIN, EXPLAINABILITY, SCENARIO_PROOF,
IN_ENGINE

### 2g. Present for review

Show the user a markdown table (columns: Order, Proposed Gate ID, Label,
GateType, Milestone, Feasibility, Epics advanced, Evidence needed).

Below the table, add a short rationale section and a list of 3–5 questions.

**Ask the user to confirm or redirect before proceeding to STEP 3.**
If they request changes (merge, split, reorder, add), apply them and
re-present the table. Iterate until the user says "proceed" or equivalent.

---

## STEP 3 — Authoring (concrete paths + proof commands)

For each confirmed gate:

### 3a. File path lookup

- Every referenced path MUST appear in the file map section of
  `docs/generated/01_CONTEXT_PACKET.md`, EXCEPT paths prefixed with `NEW:`.
- Paths ending in `.uid` are forbidden.
- Max 2 NEW paths per gate; max 6 NEW paths across the whole batch.
- NEW paths must include a 1-sentence rationale (why no existing path works).

### 3b. Touched paths rule

- Each gate: 2–12 touched paths total (FOUND + NEW combined).
- At least 1 path must be in SimCore/, SimCore.Tests/, scripts/, or scenes/
  (unless GateType is a pure DocsTooling gate — rare).
- For IN_ENGINE gates: at least 1 path in scripts/ or scenes/.

### 3c. Proof command (required per gate)

- dotnet test: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release`
  optionally with `--filter "..."`.
- Godot/GDScript: `godot --headless --path . res://scripts/tests/test_foo.gd`
  or equivalent. IN_ENGINE gates must use godot, NOT dotnet test alone.
- powershell validators: `pwsh scripts/tools/Validate-GodotScript.ps1`

### 3d. Evidence paths for gates.json

Pick 2–6 paths from touched paths that are the most concrete evidence the gate
is done. These go in `evidence_paths` in the task object.

### 3e. Present for final approval

Show the user the proposed `docs/55_GATES.md` rows (the quick-reference table
additions + the detail section rows) AND the proposed `gates.json` additions.

**Ask: "Ready to write to disk?"** Wait for explicit yes.

---

## STEP 4 — Write to disk

On explicit user approval:

### 4a. Update docs/55_GATES.md

1. **ACTIVE GATES quick-reference table** — append new rows:
   `| GATE.X.Y.001 | TODO | short one-line summary |`

2. **Detail section** — find or create the appropriate section for the slice
   (e.g., `## B. Slice 1 and 1.5 gates`, or create a new section if needed).
   Append new rows in the detail table:
   `| GATE.X.Y.001 | TODO | Full description including proof command | evidence paths |`

### 4b. Update docs/gates/gates.json

Add new task objects to the `tasks` array. Each task object:

```json
{
  "gate_id": "GATE.X.Y.001",
  "task_id": "GATE.X.Y.001.T<3-digit-random>",
  "status": "TODO",
  "title": "<gate_id>: <brief description, ≤100 chars>",
  "intent": "<full acceptance criteria, ≤400 chars>",
  "evidence_paths": ["path/a.cs", "path/b.gd"],
  "constraints": [
    "Single-session scope: stay within evidence and expected touch paths.",
    "If evidence is missing or tests fail, escalate via DEFAULT."
  ],
  "completion_hint": ["<proof command>"],
  "task_preflight": ["anchor=<primary evidence path>"],
  "escalation_rules": [
    { "when": "DEFAULT", "route": "STOP", "note": "Escalate if blocked or scope expands." }
  ]
}
```

- `task_id` T-number: pick a random 3-digit integer (001–999) not already used
  in the file.
- Re-sort the `tasks` array by MULTIKEY_V1:
  `(status_rank asc, evidence_count asc, bucket_count asc, gate_id lex, task_id lex)`
  where IN_PROGRESS=0, TODO=1.
- Update `generated_utc` to the current ISO-8601 timestamp.
- Update `queue_intent` to reference the current HEAD sha from the context packet.

### 4c. Run validation

After writing, tell the user:
> "Files updated. Run `pwsh scripts/tools/Validate-Gates.ps1` to confirm no
> freeze violations, then `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release`
> to confirm tests still pass."

---

## Hard invariants (never violate)

- Never modify existing gate rows (id, title, created_utc are immutable per freeze rules).
- Never delete gates from gates.json or 55_GATES.md.
- Never invent file paths not in the context packet file map (unless prefixed NEW:).
- Never write to disk without explicit user approval in STEP 3.
- Never claim a gate is DONE unless it has DONE status in 55_GATES.md.
- Gate IDs are immutable once written — double-check for collisions before writing.
