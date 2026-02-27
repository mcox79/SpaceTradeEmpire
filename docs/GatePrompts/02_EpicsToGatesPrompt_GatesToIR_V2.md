You are in STAGE_1C: Proposed Gate Table IR Extraction Mode (capped, no inference).

Goal
Convert the proposed gates from the current session message (provided below) into a deterministic intermediate representation (IR) for a Stage 2 tool that will do deep repo scanning and compile schema v2.2 queue objects.

[**markdown gates from session go here**]

Queue eligible means: gate_status is TODO or IN_PROGRESS.

Attachments (only)
docs/gates/gates.schema.json
docs/generated/01_CONTEXT_PACKET.md

Input format (required)
The message that runs this prompt MUST include a markdown table where each row is:

| <gate_id> | <status> | <title_and_optional_proof> | <evidence_paths_only> |

Critical input constraints (hard)
1) Evidence column is PATHS ONLY
- The evidence cell MUST contain only repo-relative path items separated by semicolons.
- If an item is NEW, it MUST be written exactly as: NEW:<path> (no spaces, no "Evidence:" prefix).
  Valid: NEW:docs/generated/foo.txt
  Invalid: Evidence: NEW:docs/generated/foo.txt
  Invalid: EVIDENCE: New: docs/generated/foo.txt
- The evidence cell MUST NOT contain "Proof:" or any command text.
  If "Proof:" appears in column 4, treat that item as INVALID and drop it with a warning:
  "EVIDENCE_ITEM_REJECTED_NON_PATH:<gate_id>:<item>"

2) Proof commands live in the title cell (column 3) only
- If a proof command is present, it MUST appear inside column 3 as a suffix segment that starts with "Proof:".
  Example:
  Unlock contract v0 ... ; Proof: dotnet test ... --filter "..."
- Stage_1C will extract this Proof segment and store it in acceptance_text.
- Column 4 must remain paths-only even if the source table includes proofs elsewhere.

Parsing rules (deterministic, no inference)

Identify the first markdown table in the message that has at least 4 pipe separated columns per row and where column 1 values start with "GATE." for at least one row. That is the Proposed Gate Table. Ignore all other content.

For each row in the Proposed Gate Table:

gate_id = column 1 trimmed

gate_status = column 2 trimmed (must be one of TODO, IN_PROGRESS, DONE, BLOCKED, CANCELLED; unknown values are invalid)

title_cell_raw = column 3 trimmed (verbatim)

evidence_cell_raw = column 4 trimmed (may be empty)

Title/proof parsing (deterministic)

- If title_cell_raw contains the substring "; Proof:" (case-sensitive, exact), split on the FIRST occurrence:
  gate_title = left part trimmed
  acceptance_text = "Proof:" + right part trimmed (must start with "Proof:")
- Else if title_cell_raw contains the substring "Proof:" (case-sensitive, exact) anywhere:
  split on the FIRST occurrence of "Proof:":
  gate_title = left part trimmed
  acceptance_text = "Proof:" + right part trimmed
- Else:
  gate_title = title_cell_raw
  acceptance_text = ""

Evidence parsing (paths only)

Split evidence_cell_raw on semicolons.
Trim each item.
Drop empty items.
For each remaining item:
- If item contains "Proof:" then reject and warn:
  "EVIDENCE_ITEM_REJECTED_NON_PATH:<gate_id>:<item>"
- If item starts with "Evidence:" or "EVIDENCE:" (any casing of the word Evidence), reject and warn:
  "EVIDENCE_ITEM_REJECTED_EVIDENCE_PREFIX:<gate_id>:<item>"
- Otherwise treat item as a path candidate and apply normalization rules below.

shards:
Not present in this table format. Set shards = [] for all gates.

notes:
Not present in this table format. Set notes = "" for all gates.

Eligibility rules

Eligible statuses: TODO, IN_PROGRESS

If a row has status DONE or anything else, do not emit it into eligible_gates and add an extraction_warnings entry:
"PROPOSED_GATE_NOT_ELIGIBLE_STATUS:<gate_id>:<status>"

If a row has an invalid or missing status, do not emit it and add:
"PROPOSED_GATE_INVALID_STATUS:<gate_id>:<raw_status>"

Path normalization (for evidence items)

Convert \ to /
Trim leading and trailing whitespace
Reject any path containing "./" or "../" segments or any ".." path element (do not include it; add warning "EVIDENCE_PATH_REJECTED_DOT_SEGMENTS:<gate_id>:<path>")
Preserve exact casing
Do not attempt to validate existence in repo here. Stage 2 will do that.

HEAD rule

HEAD must be copied verbatim from docs/generated/01_CONTEXT_PACKET.md line: head: <sha>

Cap and ordering

cap = 8

ORDER_V1: sort by gate_status rank (IN_PROGRESS before TODO), then gate_id lex ascending

Extract all eligible proposed gates first, compute totals, then apply the cap.

Hard rules

Output must be exactly one JSON object inside a single ```json code fence.
Output JSON only. No prose before or after the code fence.

No inference:

Do NOT invent acceptance text beyond the extracted Proof segment rules above.
Do NOT invent shards, notes, or extra evidence beyond the table.
Do NOT add expected_touch_paths. Omit the field entirely.
Do NOT compute file membership or repo existence. Stage 2 handles it.

IR schema (exact keys only, no extras)
{
"ir_version": "1.3",
"head": "<sha copied verbatim>",
"source_files": {
"ledger": "PROPOSED_GATE_TABLE_INPUT",
"schema": "docs/gates/gates.schema.json",
"context_packet": "docs/generated/01_CONTEXT_PACKET.md"
},
"cap": 8,
"ordering": "ORDER_V1",
"eligible_total_found": <int>,
"eligible_total_emitted": <int>,
"eligible_gates": [
{
"gate_id": "...",
"gate_title": "...",
"gate_status": "TODO|IN_PROGRESS",
"acceptance_text": "",
"evidence_universe": ["repo/relative.ext", "..."],
"shards": [],
"notes": ""
}
],
"dropped_gate_ids_by_cap": ["..."],
"extraction_warnings": ["..."],
"self_check": {
"head_present": true|false,
"cap_applied_correctly": true|false,
"eligible_counts_consistent": true|false
}
}

Self check computation rules (must be computed, not asserted)

head_present is true iff head is a 40 hex sha.

eligible_counts_consistent is true iff:
eligible_total_emitted == length(eligible_gates)
eligible_total_found == eligible_total_emitted + length(dropped_gate_ids_by_cap)

cap_applied_correctly is true iff:
eligible_total_emitted <= cap
and if eligible_total_found > cap then eligible_total_emitted == cap else eligible_total_emitted == eligible_total_found

STOP rules

If you cannot locate the HEAD line in the context packet, output inside the code fence:
{ "ir_version":"1.3", "fatal":"MISSING_HEAD_IN_CONTEXT_PACKET", "details":[...] }

If no parseable gate rows (with column 1 starting "GATE.") are found in the message, output inside the code fence:
{ "ir_version":"1.3", "fatal":"MISSING_PROPOSED_GATE_TABLE_INPUT", "details":[...] }

If after applying eligibility rules no TODO or IN_PROGRESS gates remain, output inside the code fence:
{ "ir_version":"1.3", "fatal":"NO_QUEUE_ELIGIBLE_GATES_FOUND", "details":[...] }
