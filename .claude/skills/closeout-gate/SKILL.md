---
name: closeout-gate
description: "Close out completed gates: PASS to session log, DONE in 55_GATES.md, remove from gates.json, run RoadmapConsistency. Single or batch."
argument-hint: "<GATE_ID> [GATE_ID2 ...] [sha256=<SHA256>]"
---

# Gate Closeout Skill

You are running the `/closeout-gate` skill for Space Trade Empire.
Parse `$ARGUMENTS` for:
- One or more `GATE_ID` values — required, e.g. `GATE.S1.HERO_SHIP_LOOP.LANE.001`
- `sha256=<value>` — optional SHA256 from headless proof run (applies to all gates in batch)

If multiple gate IDs are provided, run in **batch mode** (see below).

---

## Single Gate Mode (1 gate ID)

### STEP 1 — Read only what you need (parallel, targeted)

Do NOT read full files. Use Grep and targeted Read with offset+limit.

1. **Grep `docs/55_GATES.md` for the gate row** (quick-ref and detail section):
   - `Grep pattern="GATE_ID" path="docs/55_GATES.md" output_mode="content"`
   - If not found: STOP and tell user the gate ID was not found in 55_GATES.md.

2. **Grep `docs/56_SESSION_LOG.md` for last PASS entry** (to check for duplicates):
   - `Grep pattern="GATE_ID" path="docs/56_SESSION_LOG.md" output_mode="content"`
   - If already present: STOP and tell user the gate is already logged.

3. **Read `docs/gates/gates.json`** — needed in full to remove task and re-sort.

4. **Grep `docs/56_SESSION_LOG.md` for the last line** to find insertion point:
   - Read the last 10 lines: `Read file_path="docs/56_SESSION_LOG.md" offset=-10`
   - (If offset not supported, read the whole file and use the end.)

Do all four in parallel.

---

### STEP 2 — Compose PASS entry

Format the session log entry exactly:

```
## PASS — GATE_ID — <TODAY_DATE>

| Field | Value |
|---|---|
| Gate | GATE_ID |
| Status | PASS |
| Date | <TODAY_DATE> |
| SHA256 | <sha256 value or N/A> |
| Notes | Headless proof passed. Gate closed. |
```

---

### STEP 3 — Apply edits (in parallel where safe)

#### 3a. Append PASS to `docs/56_SESSION_LOG.md`
Use Edit tool to append the PASS entry from Step 2 at the end of the file.

#### 3b. Update `docs/55_GATES.md` quick-ref row
The quick-ref table is near the top of the file. Find the row:
`| GATE_ID | TODO | ...`
Change `TODO` to `DONE`.

#### 3c. Update `docs/55_GATES.md` detail section row
Find the detail row (further down in the appropriate slice section):
`| GATE_ID | TODO | ...`
Change `TODO` to `DONE`.

Run 3a, 3b, 3c in parallel.

---

### STEP 4 — Remove gate from gates.json

1. Find the task object where `"gate_id": "GATE_ID"` in the parsed JSON.
2. Remove that task object from the `tasks` array.
3. Update `generated_utc` to the current ISO-8601 timestamp.
4. Re-sort `tasks` by MULTIKEY_V1:
   `(status_rank asc, evidence_count asc, bucket_count asc, gate_id lex, task_id lex)`
   where IN_PROGRESS=0, TODO=1.
5. Write the updated JSON back to `docs/gates/gates.json` using the Write tool.

**CRITICAL**: Write the file with standard ASCII double-quotes only.
Do NOT use Unicode curly quotes (U+201C/U+201D). Use the Write tool, not Edit,
to avoid encoding corruption.

---

### STEP 5 — Validate

Run this command:
```
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "RoadmapConsistency"
```

If it passes: report success to the user.
If it fails: show the error and STOP — do not attempt to fix gates.json manually.

---

### STEP 5b — Update Active Issues (if applicable)

After validation passes, check `docs/ACTIVE_ISSUES.md` for any OPEN issues
that this gate addresses. Look for issues that reference this gate ID, or
whose description matches the gate's work.

If matches are found:
1. Read the relevant section of `docs/ACTIVE_ISSUES.md` (Grep for the issue ID)
2. Change the issue's Status from `OPEN` to `FIXED`
3. Add the gate ID in the Gate column
4. Add today's date

If the gate's intent field mentions "Fixes ACTIVE_ISSUES C1, C2" etc., use
those references directly. Otherwise, use best judgment based on the gate's
evidence paths and intent.

Skip this step if no issues match.

---

### STEP 6 — Report

Tell the user:
```
Gate GATE_ID closed.
- Session log: PASS entry appended to docs/56_SESSION_LOG.md
- docs/55_GATES.md: quick-ref + detail rows -> DONE
- gates.json: task removed, queue re-sorted
- RoadmapConsistency: PASS
- Active issues updated: <list of issues marked FIXED, or "none">
```

---

## Batch Mode (2+ gate IDs)

When multiple gate IDs are provided, process them efficiently with shared I/O.

### BATCH STEP 1 — Read (one pass)

Do all reads in parallel:

1. **For each GATE_ID**: Grep `docs/55_GATES.md` for the gate row (content mode).
   - Collect all found rows. If any gate ID is not found, report it and skip that gate.

2. **For each GATE_ID**: Grep `docs/56_SESSION_LOG.md` for duplicates.
   - If any gate is already logged, report it and skip that gate.

3. **Read `docs/gates/gates.json`** in full (once).

4. **Read the last 20 lines of `docs/56_SESSION_LOG.md`** for insertion point.

### BATCH STEP 2 — Compose all PASS entries

For each gate, compose the PASS entry (same format as single mode).
Concatenate all entries into one block for a single append.

### BATCH STEP 3 — Apply all edits

#### 3a. Append ALL PASS entries to `docs/56_SESSION_LOG.md` in one Edit.

#### 3b-c. Update `docs/55_GATES.md`
For each gate, update both the quick-ref row and the detail row (TODO -> DONE).
Apply these as sequential Edits to the same file (one per gate, quick-ref first,
then detail rows). Do NOT parallelize edits to the same file.

### BATCH STEP 4 — Remove all gates from gates.json (one Write)

1. Remove ALL closed gate task objects from the `tasks` array.
2. Update `generated_utc` once.
3. Re-sort remaining tasks by MULTIKEY_V1.
4. Write once with the Write tool (ASCII quotes only).

### BATCH STEP 5 — Validate once

Run RoadmapConsistency **once** after all edits:
```
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "RoadmapConsistency"
```

### BATCH STEP 5b — Update Active Issues (if applicable)

After validation, check `docs/ACTIVE_ISSUES.md` for OPEN issues addressed by
any of the closed gates. Update matching issues to FIXED with gate ID and date.
Apply all edits to ACTIVE_ISSUES.md sequentially (same file).

### BATCH STEP 6 — Report summary

```
Batch closeout complete: N gates closed.
- Closed: GATE.X.Y.001, GATE.X.Y.002, ...
- Skipped (already logged): GATE.A.B.001 (if any)
- Skipped (not found): GATE.C.D.001 (if any)
- RoadmapConsistency: PASS
- Active issues updated: <list of issues marked FIXED, or "none">
```

---

## Hard invariants

- Never modify existing gate IDs or titles.
- Never delete other gates from gates.json or 55_GATES.md.
- Never write curly quotes to gates.json — use Write tool, not Edit.
- Never mark a gate DONE without a confirmed PASS (SHA256 or explicit user confirmation).
- Never read full 55_GATES.md or 56_SESSION_LOG.md — always Grep first.
- In batch mode: if one gate fails validation, STOP and report. Do not continue closing remaining gates.
