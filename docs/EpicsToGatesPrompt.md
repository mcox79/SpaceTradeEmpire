You are in STAGE_1: Ledger Extraction Mode (capped, no inference).

Goal
Extract a capped list of queue-eligible work items from docs/55_GATES.md into a deterministic intermediate representation (IR) for a Stage 2 tool that will perform deep repo scanning and compile schema v2.2 queue objects.
Queue-eligible means: gate_status is TODO or IN_PROGRESS.

Attachments (only)
1) docs/55_GATES.md
2) docs/gates/gates.schema.json
3) docs/generated/01_CONTEXT_PACKET.md

Cap
- cap = 8 (if the user specifies a different cap, use that value)
- Apply cap deterministically after extraction using ordering rule ORDER_V1 defined below.

Hard rules
- Output must be a single JSON object, and it MUST be inside one ```json code fence.
- Output JSON only. No prose before or after the code fence.
- Do NOT infer or invent anything:
  - Do NOT invent shards. Only extract shards if they are explicitly present in 55 as distinct sub-items under a gate.
  - Do NOT invent evidence paths. Evidence paths must come only from paths explicitly listed in 55 for that gate or shard.
  - Do NOT invent expected_touch_paths. Only include expected_touch_paths if 55 explicitly provides them. Otherwise omit the field entirely.
  - Do NOT compute file membership booleans. Stage 2 will compute membership from the context packet.
- Normalize extracted paths:
  - use / separators
  - trim leading/trailing whitespace
  - reject any path containing ./ or .. segments
  - preserve exact casing
- HEAD must be copied verbatim from docs/generated/01_CONTEXT_PACKET.md line: head: <sha>

ORDER_V1 (deterministic ordering for cap)
- Sort extracted eligible gates by:
  1) gate_status rank: IN_PROGRESS before TODO
  2) gate_id lex ascending
- Then keep the first cap gates after sorting.

IR schema to emit (exact keys only, no extras)
{
  "ir_version": "1.3",
  "head": "<sha copied verbatim from context packet>",
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
      "acceptance_text": "verbatim acceptance or completion text from 55 (may be empty)",
      "evidence_universe": ["repo/relative.ext", "..."],
      "shards": [
        {
          "shard_title": "verbatim shard title from 55",
          "shard_status": "TODO|IN_PROGRESS",
          "evidence_subset": ["repo/relative.ext", "..."]
        }
      ],
      "notes": "verbatim short notes from 55 that constrain scope (may be empty)"
    }
  ],
  "dropped_gate_ids_by_cap": ["GATE.X.Y.001", "..."],
  "extraction_warnings": ["..."]
}

Extraction requirements
- Extract ALL eligible gates first (TODO + IN_PROGRESS) so you can compute eligible_total_found and dropped_gate_ids_by_cap, then apply the cap.
- evidence_universe must be an array of strings. If 55 has no explicit evidence paths for an eligible gate, use [] and add an extraction_warnings entry.
- shards:
  - If 55 has no explicit shards: emit "shards": [].
  - If 55 has shards: include only shards whose shard_status is TODO or IN_PROGRESS.
  - If 55 does not specify shard status, inherit the parent gate_status.

STOP rules
- If you cannot locate the HEAD line in the context packet, output inside the code fence:
  { "ir_version":"1.3", "fatal":"MISSING_HEAD_IN_CONTEXT_PACKET", "details":[...] }
- If you cannot find any TODO or IN_PROGRESS gates in 55, output inside the code fence:
  { "ir_version":"1.3", "fatal":"NO_QUEUE_ELIGIBLE_GATES_FOUND", "details":[...] }
