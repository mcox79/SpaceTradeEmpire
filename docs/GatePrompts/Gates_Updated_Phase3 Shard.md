# PHASE 3 PROMPT v3.1: Narrative + deterministic patches

# LLM Prompt (HEAD <HEAD_FROM_NEXT_GATE_PACKET>)

Hard guardrails:
- gate ids immutable once merged
- no deletions by default (except deleting the completed task from docs/gates/gates.json during Step D)
- planning commits separate from execution commits
- max 6 attachments per LLM session (exclusive of docs/generated/01_CONTEXT_PACKET.md)
- NO new files unless the task explicitly requires creating one and it is unavoidable
- If you propose reflection or optional behavior, you must justify determinism and failure safety

Routing (deterministic):
- If tests failing OR connectivity violations OR Validate-Gates fails: BASELINE FIX mode.
- Else if next_gate_packet says Split required: YES: SPLIT mode (add subgates, do not delete parent).
- Else: EXECUTE mode.

Task:
Execute exactly one gate: the Selected task in next_gate_packet.

## Output required (strict)

You MUST output exactly these sections, in this order.

## A) NARRATIVE_PLAN

### A1) What we are doing
- 3 to 6 bullets explaining the intent in plain language.
- Must reference the Selected gate id and task id.

### A2) Why this is the right change
- 2 to 5 bullets.
- Must mention determinism risks being avoided (ordering, time sources, unordered iteration, etc.) where relevant.

### A3) Files we will change
- A numbered list.
- Each item: file path + 1 sentence describing the change.

### A4) Files we will NOT change
- 2 to 6 bullets listing the attached files that will remain unedited, if any.
- If all attached files will be edited, say: "None".

### A5) Step by step apply plan (human followable)
- A numbered list of steps.
- Each step must correspond to exactly one PATCH block that will appear in section B.
- Each step must say:
  - Which file
  - What to search for (a short snippet or the ANCHOR_START line)
  - What will be replaced conceptually (1 sentence)
  - What success looks like (1 sentence)

## B) EXECUTION_OUTPUT

### B1) File edits (exact paths)
Default output mode is PATCH, not full-file.

PATCH rules (must follow exactly):
- For each edited file, output:
  FILE: <exact repo-relative path using / separators>
  PATCH:
  - ANCHOR_START: <exact existing line to search for, copied verbatim>
    REPLACE_RANGE:
    <paste the exact lines to replace, copied verbatim>
    WITH:
    <new lines>
  - (repeat per edit block)
  ANCHOR_END: <exact existing line to search for, copied verbatim>

- Every PATCH block must include BOTH an ANCHOR_START and an ANCHOR_END that exist exactly once in the current file.
- Keep patches minimal: do not reformat unrelated lines, do not reorder sections, do not rewrite entire documents unless necessary.
- FULL FILE output is allowed ONLY when:
  a) creating a new file, OR
  b) the file is <= 120 lines, OR
  c) the change genuinely touches most of the file.
  If FULL FILE is used, explicitly label it:
    FILE: <path>
    FULL_CONTENTS:
    <entire file>

Anchor verification rule (hard):
- If you cannot quote exact ANCHOR_START and ANCHOR_END from the attachments, you MUST stop and request the file re-upload.
- Do NOT provide approximate anchors or “best guess” patches.

### B2) Proof command(s) to run (exact commands, deterministic)
- Provide runnable commands only (PowerShell preferred).
- Commands must capture exit code and output deterministically.
- If multiple commands, number them and keep to the minimum.

### B3) Stop conditions
- If proofs fail, STOP and output DIAGNOSTICS:
  - first failure only
  - likely cause (1 to 3 bullets)
  - minimal next action (1 to 3 bullets)
- Do not propose broad refactors or additional gates in DIAGNOSTICS.

## C) CLOSEOUT_PATCH (always, no JSON)

### SESSION_LOG_LINE
- One line to append to docs/56_SESSION_LOG.md.
- Format must match existing file and MUST start with "- ".
- Must include branch, gate id, result token PASS or FAIL, and Evidence paths separated by ;.

### GATES_55_UPDATE
- Either "NO_CHANGE" or:
  - Find gate '<gate_id>' in docs/55_GATES.md and set status to DONE or IN_PROGRESS.
  - If DONE, include one closure proof phrase (max 180 chars).

### QUEUE_EDIT
- Delete task_id '<task_id>' from docs/gates/gates.json tasks[].
- Ensure pending_completion is null after closeout.

Prohibited:
- JSON output
- planning additional gates
- requesting broad extra context
- exceeding attachment cap
- rewriting canonical docs unless the task intent explicitly requires changes there
- claiming you verified anything not in attachments

## Attachments (in order)
<ATTACHMENT_LIST_FROM_DEVTOOL>

## Next Gate Packet (Selected only)
<NEXT_GATE_PACKET_SELECTED_ONLY_CONTENT>
