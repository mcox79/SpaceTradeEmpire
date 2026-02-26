You are in PHASE 3: Queue Acceptance Review.

Attachments (ONLY these 3 files):
1) docs/generated/gates_queue_full.json
2) docs/gates/gates.schema.json
3) docs/generated/gates_scan_preflight.md

Output format (exact):
PRE_ACCEPTANCE_REPORT
PASS or FAIL
reasons:
- bullet list

Step 0: Identity reporting (NON-BLOCKING)
- Print SHA256 for each attachment.
- If (and only if) the user provided expected SHA256 values in chat, compare and FAIL on mismatch.
- If expected hashes are not present in chat, DO NOT fail; state "verification skipped: expected hashes not provided".

Step 1: Parse schema bytes safely (MUST PASS) [MATCH PHASE 2 BEHAVIOR]
docs/gates/gates.schema.json may be fenced markdown.

Parsing algorithm (exact):
A) Read schema bytes as text.
B) Find matches of the regex: (?s)```json\s*(\{.*?\})\s*```
   - If match count > 1: FAIL and state "Multiple ```json fences found"
   - If match count == 1: schema_json_text = captured group 1 (the { ... } object)
   - Else (no matches): schema_json_text = raw file content as-is
C) Parse schema_json_text as JSON.
If parsing fails: FAIL and show the first 8 lines of schema_json_text.

Step 1b: Schema pattern self-test (MUST PASS)
- Extract schema.$defs.gate_id.pattern and schema.$defs.task_id.pattern as literal strings.
- Print them EXACTLY as read from JSON (they should include escaped dots like \\.).
- Compile regex from those patterns and test against:
  - first task gate_id and task_id from queue
  - also test a known example: gate_id="GATE.PROG.UI.001", task_id="GATE.PROG.UI.001.T576"
If any of these self-tests fails: FAIL and report which value did not match which pattern.

Step 2: Schema const checks (MUST PASS)
- Validate queue_contract_version equals schema const.
- Validate queue_ordering equals schema const.

Step 3: Strict schema validation of queue (MUST PASS)
Validate:
- required root keys exist: queue_contract_version, queue_ordering, generated_utc, queue_intent, tasks
- tasks is non-empty
- each task includes required keys per schema
- regex constraints pass (gate_id, task_id, repo_path)
- maxLength constraints pass (including small_string maxLength=160)
If any violation: report exact JSON path + offending value + observed length.

Step 4: Executability policy (MUST PASS)
Each task must have at least one completion_hint item beginning with one of:
dotnet, pwsh, powershell, godot, git, .\
If any fail: list task_id and completion_hint array.

Step 5: Ordering verification (MUST PASS)
Verify tasks are sorted by MULTIKEY_V1:

Define status_rank:
- IN_PROGRESS = 0
- TODO = 1
- DONE = 2
- BLOCKED = 3
(If status is unknown, rank = 9)

Define evidence_count = len(evidence_paths)

Define bucket_count as:
- bucket = first path segment before '/'
- bucket_count = number of distinct buckets in evidence_paths

Sort key tuple:
(status_rank asc, evidence_count asc, bucket_count asc, gate_id lex, task_id lex)

Confirm observed order matches this sort. If not, report first offending adjacent pair with their key tuples.

Step 6: Consistency vs gates_scan_preflight.md (NON-BLOCKING)
- If gates_scan_preflight reports queued_total, compare to tasks.length and report mismatch if any.
- Do not FAIL based solely on Step 6.

Final result:
- PASS only if Steps 1..5 all pass.
- FAIL otherwise.
