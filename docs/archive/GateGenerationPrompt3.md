# Prompt 3 (Operational): Deterministic Review Mode (Evaluate gate creation output, no patches)

You are in Review Mode only.

## Goal
Evaluate a user-pasted gate creation planning output and return a deterministic verdict.

Do NOT propose new gates.
Do NOT output any JSON patches.
Do NOT propose edits to any repo files.
Do NOT “fix” the output. Only evaluate it.

## Accepted inputs (paste whatever you have)
The user MAY paste any subset of:
A) Planning output text (coverage table, ID allocation, minimal patch plan)
B) Minimal patch plan JSON array (standalone array is allowed and preferred)
C) HEAD hash `HEAD:<hash>` (or excerpt containing it)
D) FINAL_APPROVED_SEGMENTATION_BLOCK
E) docs/gates/gates.json (full or ID list only)

You must attempt evaluation with whatever is provided.

## Verdict types (exactly one)
- PASS: all enforced checks passed, and no enforced check is blocked by missing inputs
- FAIL: at least one enforced check definitively failed (based on provided text)
- FAIL_INCOMPLETE: no definitive failures found, but one or more enforced checks were BLOCKED by missing required inputs

## Allowed response shape (hard)
Your response MUST contain exactly these sections, in this order:
1) `## PASS/FAIL`
2) `## Fail reasons` (empty if PASS)
3) `## Pass evidence` (empty if FAIL)
4) `## Minimal fixes` (only smallest changes; no rewrites)

No other sections.

## Enforcement policy (deterministic)
This prompt is patch-centric.
- If B (patch array) is present: enforce patch rules R3–R12.
- If B is absent but A contains a patch array: enforce patch rules using that array.
- If no patch array is available: verdict MUST be FAIL_INCOMPLETE.

Planning prose rules (R1–R2) are OPTIONAL:
- Enforce R1–R2 only if A is present.
- If A is missing, do NOT mark them BLOCKED and do NOT let them drive FAIL_INCOMPLETE.

Segmentation binding rules (R10) are OPTIONAL:
- Enforce R10 only if D is present.
- If D is missing, do NOT fail on missing seg tags.

Collision and timestamp rules are conditional:
- Enforce timestamp rule (R7) only if C is present.
- Enforce registry collision rule (R9) only if E is present.

## Rules

### R1) Planning output section presence (enforced only if A present)
FAIL if A is present and any missing:
- `## Epic coverage table`
- `## ID allocation`
- `## Minimal patch plan`

### R2) Planning output boundedness and JSON locality (enforced only if A present)
FAIL if A is present and any paragraph longer than 3 lines exists outside tables and code fences.
FAIL if A is present and any JSON appears outside `## Minimal patch plan`:
- Outside the `## Minimal patch plan` section, FAIL if any code fence contains a line starting with `{` or `[` (ignoring whitespace).

### R3) Patch JSON array shape (requires patch array from A or B)
FAIL unless exactly ONE top-level JSON array is identifiable and parseable:
- starts with `[` and ends with `]`
- no comments, no ellipses, no trailing commas

If no patch array available: BLOCKED.

### R4) Gate object required keys (requires patch array)
For every element:
FAIL if not an object.
FAIL if missing any of:
- `id`, `title`, `status`, `scope`, `freeze`, `evidence`, `tags`, `created_utc`, `updated_utc`
If patch array missing: BLOCKED.

### R5) Unknown keys (schema assumption) (requires patch array)
Assume `additionalProperties: false`.
FAIL if any gate object contains keys outside this allowed set:
- id, title, status, scope, priority, tags, created_utc, updated_utc, freeze, evidence
If patch array missing: BLOCKED.

### R6) Status TODO only (requires patch array)
FAIL if any new gate has `status != "TODO"`.
If patch array missing: BLOCKED.

### R7) Timestamp equals HEAD (enforced only if C present, requires patch array)
FAIL if any new gate has created_utc or updated_utc not exactly equal to provided `HEAD:<hash>`.
If C missing: NOT ENFORCED.

### R8) Gate ID format + duplicates (requires patch array)
FAIL if any `id` does not match `^GATE(\.[A-Z0-9_]+)+\.[0-9]{3}$`
FAIL if any duplicate `id` appears within the patch.
If patch array missing: BLOCKED.

### R9) Collision vs registry snapshot (enforced only if E present, requires patch array)
Parse IDs from E.
FAIL if any new `id` already exists in E.
If E missing: NOT ENFORCED.

### R10) Segmentation binding + seg_label (enforced only if D present, requires patch array)
If enforced:
FAIL unless each gate has exactly one `seg=NN` where NN in 01..05.
FAIL unless each gate has exactly one `seg_label=...` equal to the Label for that seg row in D (exact match).
If D missing: NOT ENFORCED.

### R11) Evidence rules (requires patch array)
FAIL if any gate violates:
- evidence is an array
- each item has `kind` and `path`
- kind in: doc, test, code, generated, command
- evidence count <= 24
TO_CREATE:
- per-gate TO_CREATE count <= 1 where TO_CREATE means path or note starts with `TO_CREATE:`
- if path starts `TO_CREATE:` then note must start `TO_CREATE:`
If patch array missing: BLOCKED.

### R12) One-session digestibility proxy (requires patch array)
FAIL if any gate violates:
- evidence count < 2 OR > 6
- more than 2 buckets across evidence paths where buckets by prefix:
  SimCore, Tests, UI, Docs/Tooling, Data/Generated
If patch array missing: BLOCKED.

## Verdict selection (deterministic)
1) If any FAIL triggered: verdict = FAIL.
2) Else if any BLOCKED exists among R3, R4, R5, R6, R8, R11, R12: verdict = FAIL_INCOMPLETE.
3) Else verdict = PASS.

## Fail reasons formatting (mandatory)
- For each failure:
  - `<gate id or GLOBAL> % <RULE> % <what failed>`
- For FAIL_INCOMPLETE only:
  - Provide EXACTLY ONE bullet listing missing required input letters for the blocked core rules, like:
    - `GLOBAL % BLOCKED % missing patch array (A/B)`

## Pass evidence requirements
If PASS, list 3 to 7 concrete proofs by quoting exact snippets.

## Minimal fixes rules
Under `## Minimal fixes`, provide ONLY the smallest set of inputs or edits needed to reach PASS.
- For FAIL: list exact field/value corrections.
- For FAIL_INCOMPLETE: list only the missing input(s) needed next.
