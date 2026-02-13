You are in STAGE_1: Ledger Extraction Mode (capped, no inference).

Goal
Extract a capped list of queue-eligible work items from docs/55_GATES.md into a deterministic intermediate representation (IR) for a Stage 2 tool that will do deep repo scanning and compile schema v2.2 queue objects.
Queue-eligible means: gate_status is TODO or IN_PROGRESS.

Attachments (only)
1) docs/55_GATES.md
2) docs/gates/gates.schema.json
3) docs/generated/01_CONTEXT_PACKET.md

Cap and ordering
- cap = 8
- ORDER_V1: sort by gate_status rank (IN_PROGRESS before TODO), then gate_id lex ascending
- Extract all eligible gates first, compute totals, then apply the cap.

Hard rules
- Output must be exactly one JSON object inside a single ```json code fence.
- Output JSON only. No prose before or after the code fence.
- No inference:
  - Do NOT invent shards. Only extract shards if explicitly present in 55 as distinct sub-items.
  - Do NOT invent evidence paths. Only extract paths explicitly listed in 55.
  - Do NOT invent expected_touch_paths. Only include if explicitly present in 55; otherwise omit the field.
  - Do NOT compute file membership booleans. Stage 2 will compute membership using the context packet file map.
- Normalize extracted paths:
  - use / separators
  - trim leading/trailing whitespace
  - reject any path containing ./ or .. segments
  - preserve exact casing
- HEAD must be copied verbatim from docs/generated/01_CONTEXT_PACKET.md line: head: <sha>

IR schema (exact keys only, no extras)
{
  "ir_version": "1.3",
  "head": "<sha copied verbatim>",
  "source_files": {
    "ledger": "docs/55_GATES.md",
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
      "acceptance_text": "verbatim from 55 (may be empty)",
      "evidence_universe": ["repo/relative.ext", "..."],
      "shards": [
        {
          "shard_title": "verbatim from 55",
          "shard_status": "TODO|IN_PROGRESS",
          "evidence_subset": ["repo/relative.ext", "..."]
        }
      ],
      "notes": "verbatim from 55 (may be empty)"
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

Self-check computation rules (must be computed, not asserted)
- head_present is true iff head is a 40-hex sha.
- eligible_counts_consistent is true iff:
    eligible_total_emitted == length(eligible_gates)
    eligible_total_found == eligible_total_emitted + length(dropped_gate_ids_by_cap)
- cap_applied_correctly is true iff:
    eligible_total_emitted <= cap
    and if eligible_total_found > cap then eligible_total_emitted == cap else eligible_total_emitted == eligible_total_found

STOP rules
- If you cannot locate the HEAD line in the context packet, output inside the code fence:
  { "ir_version":"1.3", "fatal":"MISSING_HEAD_IN_CONTEXT_PACKET", "details":[...] }
- If you cannot find any TODO or IN_PROGRESS gates in 55, output inside the code fence:
  { "ir_version":"1.3", "fatal":"NO_QUEUE_ELIGIBLE_GATES_FOUND", "details":[...] }
