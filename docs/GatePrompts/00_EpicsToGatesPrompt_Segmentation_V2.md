# Segmentation Mode v5.3: Multi-epic gate candidates (path-free, ordered, session-sized)

You are in Segmentation Mode only.

## Goal

Using ONLY the attached files, produce a reviewable, ordered candidate gate set that makes real
forward progress. The set should span as many adjacent TODO or IN_PROGRESS epics as needed to
produce 6–10 gates of realistic session size. Do not artificially subdivide a small epic to fill
the count.

This mode MUST NOT guess repo file paths. File path selection is a separate step during gate
authoring.

## Attachments (only these)
- docs/54_EPICS.md
- docs/55_GATES.md
- docs/gates/GATE_FREEZE_RULES.md

---

## Hard rules

### 1) Anchor epic selection (deterministic)

Optional override:
- If the prompt contains `ANCHOR_EPIC_OVERRIDE: EPIC.<...>`, use that epic key token exactly.

Default selection (file order, deterministic):
- Locate `## Canonical Epic Bullets` in docs/54_EPICS.md.
- ANCHOR_EPIC is the FIRST epic in that section whose status is TODO or IN_PROGRESS,
  excluding epics whose key begins with `EPIC.X.` or `EPIC.S0.`.
- If none match, choose the FIRST TODO or IN_PROGRESS epic anywhere in docs/54_EPICS.md
  (same exclusions).
- Use the epic key token exactly as written.

### 2) Multi-epic scope expansion

After selecting ANCHOR_EPIC, expand scope to adjacent epics as follows:

- Collect the ANCHOR_EPIC and up to 3 additional TODO or IN_PROGRESS epics from the same
  slice or adjacent slices (same exclusions as above).
- Prefer epics that share SimCore systems, entity types, or UI surfaces with ANCHOR_EPIC.
- Prefer epics that are prerequisites or natural follow-ons to ANCHOR_EPIC.
- Stop expanding when you have enough material for 6–10 realistically-sized gates.
- If ANCHOR_EPIC alone yields 6–10 realistic gates, do not expand.

### 3) Gate sizing rule (non-negotiable)

Each candidate gate must represent realistic LLM session work:

- **Too small (exclude):** pure schema field addition, single-method addition, moving a
  constant to a config file, adding one test assertion, renaming a type. These should be
  folded into a larger gate.
- **Right size:** one new SimCore system or subsystem component with tests; one new intent +
  command + contract test; one new SimBridge query surface + UI readout; one end-to-end
  scenario proof covering a new behavior path; a combined contract+core gate when the
  contract is trivial.
- **Too large (split):** gates that require touching >4 unrelated systems, or that mix
  SimCore logic + persistence + UI + scenario proof into one unit.

Apply this rule before the feasibility score. If an item is too small, merge it upward.
If an item is too large, split it and count the split items toward the total.

### 4) Feasibility scoring (sharp filter)

Score each candidate 1–5. Exclude any item scoring ≤ 2.

- **5:** Prior art exists (same pattern already in repo), clear acceptance test, no ambiguous
  design decisions.
- **4:** Pattern exists nearby, one design decision needs resolving but it is bounded.
- **3:** New pattern, design decisions are ambiguous but bounded, no blocking unknowns.
- **2:** Depends on a gate not in this set and not already DONE, OR requires a design
  decision that cannot be resolved from attached docs alone. **Exclude.**
- **1:** Blocking unknown, circular dependency, or requires out-of-scope work. **Exclude.**

If exclusions drop the count below 6, expand scope to the next adjacent epic rather than
lowering the bar.

### 5) No path work in this mode

- Do NOT output repo file paths.
- Do NOT reference the context packet file map.
- Do NOT include attachment sketches, anchor paths, or file map proof.

### 6) No invented coverage claims

- Do not claim anything is DONE unless a gate with that ID exists in docs/55_GATES.md with
  status DONE.
- It is acceptable to say "existing coverage unclear."

### 7) Candidate set size and closure

- Output exactly 6 to 10 candidate items.
- Each item must be closeable in one LLM session.
- Items scoring ≤ 2 are excluded before counting.

---

## Milestone preference (hard)

Prefer a gate set that reaches at least one of these milestone types within the set:

- **PLAYABLE_BEAT:** a behavior is observable in-engine. Proof requires either 
  (a) Godot headless scene execution (godot --headless) with visible output, OR
  (b) a GDScript test that boots a scene and performs a player-visible action.
  A dotnet-test-only proof does NOT qualify as PLAYABLE_BEAT.
- **HEADLESS_PROOF:** a deterministic headless run (dotnet test) exercises a new
  end-to-end behavior path and emits a stable report.
- **REGRESSION_ANCHOR:** a new determinism or save/load regression locks in a new system so
  future gates can build on it safely.

At least one gate in the set must reach one of these milestone types. If the set cannot reach
any milestone, expand scope to an adjacent epic that enables one.

---

## Gate shaped items

Each candidate item MUST specify:
- Proposed Gate ID (plausible new ID; must not reuse an existing ID from docs/55_GATES.md)
- GateType (see enum)
- One explicit failure mode with a deterministic explain surface
- Evidence needed (no paths; describe what must exist to prove DONE)
- Which epics this gate advances (list the epic key tokens)

---

## Ordering and rationale

- Provide a recommended execution order 1..N.
- For each item state: what it unblocks and why it comes before the next item.

---

## Foundation work is allowed but bounded

- No more than 2 items in the set may be pure CONTRACT or pure DETERMINISM without new
  observable behavior.
- At least 3 items must be CORE_LOGIC, UI_MIN, EXPLAINABILITY, SCENARIO_PROOF, or IN_ENGINE.

---

## Godot-layer balance rule (hard)

- If the last 3 completed non-EPIC.X gates all have anchor paths exclusively in
  SimCore/ or SimCore.Tests/, at least 1 gate in this candidate set MUST have its
  primary anchor in scripts/ or scenes/.
- This rule overrides slice completion preference.
- If you cannot determine anchor history from the attached files, flag the uncertainty
  and include at least 1 IN_ENGINE gate as a precaution.

---

## Player-facing proof policy

- If GateType is UI_MIN, EXPLAINABILITY, HEADLESS_PROOF, or IN_ENGINE, the item MUST
  specify a player-facing proof requirement (what a player or headless script would observe).
- Otherwise player-facing proof is optional.

---

## Avoid trivial items

Do NOT propose:
- Rerunning validators or health scripts as standalone gates.
- Adding a single field to an existing schema without behavioral change.
- Documentation-only updates.
- Test-only additions that have no corresponding behavior change.

---

## Enums

**GateType** (pick exactly one):
- CONTRACT — schema, query contract, event type definitions
- CORE_LOGIC — SimCore behavior, system logic, intent+command pipeline
- DETERMINISM — save/load, ordering regression, hash stability
- UI_MIN — minimal SimBridge query + UI readout
- EXPLAINABILITY — cause chain, ReasonCode tokens, suggested actions surface
- SCENARIO_PROOF — end-to-end headless proof of a new behavior path
- IN_ENGINE — Godot scene integration: scene boots, player input works, 
  SimBridge connects. Proof command uses `godot --headless` or a Godot test runner.

**Milestone type** (pick exactly one per gate, or "none"):
- PLAYABLE_BEAT
- HEADLESS_PROOF
- REGRESSION_ANCHOR
- none

---

## Output format (mandatory, exactly these sections, in this order)

### A) Epic scope

- ANCHOR_EPIC: `<epic key token>`
- Epics in scope: list all epics this gate set advances (key tokens only)
- Why this scope: 2–4 bullets explaining the expansion decision (or why no expansion needed)
- Existing adjacent DONE gates: up to 6 gate IDs from docs/55_GATES.md that this set builds on

### B) Candidate gate set (ordered)

Markdown table with columns:
- Order (1..N)
- Proposed Gate ID
- Item label (6–12 words)
- GateType
- Milestone type
- Feasibility (3–5; items ≤2 excluded before this table)
- Player-facing proof (none | ui_readout_min | playable_flow_min | optional)
- Epics advanced (key tokens, abbreviated if needed)
- Failure mode + explain surface (one sentence)
- Evidence needed (no paths; what must exist to call this DONE)

### C) Gate order rationale

For each item in order, 1–2 bullets:
- Why this gate now
- What it unblocks next

### D) Milestone assessment

2–4 bullets:
- Which gate(s) reach a milestone and what type
- Whether the set as a whole closes a tranche or opens the next one
- What the natural next gate set would be after this one

### E) Questions for iteration

3–5 questions, each ≤ 14 words, targeted at merge/split or design decisions.

---

## Forbidden output

- No JSON.
- No patch plans.
- No file paths.
- No file map proof.
- No EPIC_B references.
- No axis enum (removed in v5.3).
- No buckets column (removed in v5.3).
- No new-files-budget column (removed in v5.3).
