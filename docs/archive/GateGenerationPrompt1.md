# Segmentation Mode v4.1: Epic segmentation with anchor proofs (no patches)

You are in Segmentation Mode only.

## Goal
Using ONLY the attached files, produce a reviewable segmentation for EPIC_A with:
- exactly 4 to 6 candidate items for EPIC_A,
- one-session feasibility estimate per item,
- attachment sketches VERIFIED to exist via the context packet file map,
- a compact proof section quoting file-map lines for anchor paths,
- axis diversity so the epic is not reduced to one subsystem.

Do NOT output any JSON patches. Do NOT propose edits to any repo files.

## Attachments (only these)
1) docs/generated/01_CONTEXT_PACKET.md (required: HEAD hash, repo file map)
2) docs/54_EPICS.md
3) docs/gates/gates.json
4) docs/gates/gates.schema.json
5) docs/gates/GATE_FREEZE_RULES.md

Do not request or use any other files.

## Non-negotiable rules (hard)
1) File-map truth
- You may ONLY reference repo paths that appear in the file map inside docs/generated/01_CONTEXT_PACKET.md.
- If a candidate path is not found, you MUST replace it with a found path.
- Your final output must contain ZERO non-existent paths.

2) No invented coverage claims
- Do not claim anything is DONE unless it is a gate present in docs/gates/gates.json.
- Existing coverage for EPIC_A may be “unclear”; that is allowed.

3) Output boundedness
- EPIC_A: exactly 4 to 6 candidate items.
- EPIC_B: omitted in this mode.
- No essays. Only the required sections.

4) Formatting hard rules
- Output must be pure Markdown. HTML tags (including <br>) are forbidden.
- Attachment sketch must be ONE line: paths separated by `; ` (semicolon+space).

## Deterministic EPIC_A selection (hard)
- Scan docs/54_EPICS.md in order.
- Choose EPIC_A as the FIRST epic whose epic key token begins with `EPIC.X.`.
- If no epic key begins with `EPIC.X.`, choose the FIRST epic key in the file.
- Use the epic key token exactly as written in docs/54_EPICS.md.

## Determinism axis (hard)
Each candidate item MUST declare exactly one axis from this enum (no other values):
- ordering
- serialization
- id_stability
- replay_diagnostics
- rng_streams
- time_sources

Axis diversity rule:
- Across EPIC_A’s 4 to 6 items, include at least 3 distinct axes.
- No single axis may appear more than 2 times.

## Candidate item rules (hard)
Each item label:
- 6 to 12 words
- must NOT contain the standalone word "and" or "including"
- must NOT contain commas, slashes, "plus", or multi-list phrases
- must be single-outcome and closeable in one LLM session

Primary deliverable (required per item):
- one of: test, command
- If you believe ui or code is necessary, you must replace the item with a test or command version.

Buckets (required per item)
Choose either ONE bucket, or TWO buckets joined by a plus sign `+`.
Buckets must be chosen from this fixed set (no other values):
- SimCore (paths starting with SimCore/)
- Tests (paths starting with SimCore.Tests/)
- ScriptsScenes (paths starting with scripts/ or scenes/)
- DocsTooling (paths starting with docs/ or scripts/tools/)
- DataGenerated (paths starting with scenarios/ or docs/generated/ or SimCore.Tests/TestData/)

Bucket formatting rules (hard):
- If two buckets, format exactly like: `Tests+SimCore`
- The `%` character is forbidden in buckets.

Attachment sketch (required per item):
- 2 to 6 repo-relative paths
- Every path MUST be FOUND in the file map
- Paths must be consistent with the chosen buckets (prefix must match)
- test: include at least one SimCore.Tests/ path
- command: include at least one scripts/tools/ path
- Avoid doc-only attachment sketches.

Anchor path (new, required per item):
- Must be exactly ONE of the paths listed in Attachment sketch.
- Prefer selecting the primary proof file (usually the test file for test deliverable).

Feasibility (required per item):
- 1..5, where 5 is easiest in one session.
- If feasibility <= 2, do not include the item (replace it).

## REQUIRED validation inside the output (hard)
For every candidate item row, the PathCheck column must be exactly:
- `FOUND`

## Output format (mandatory, exactly these sections, in this order)
## A) Epic selection
- EPIC_A: <epic key token>
- Why segmentation is needed: 2 to 5 bullets
- Existing related gates (optional): list up to 6 gate IDs from docs/gates/gates.json that seem adjacent

## B) Candidate segmentation for EPIC_A
- Markdown table with columns:
  - Item label (6 to 12 words)
  - Axis (enum)
  - Primary deliverable (test|command)
  - Feasibility (1..5)
  - Buckets (one bucket or two joined by +)
  - Attachment sketch (2 to 6 existing paths, single line, `; ` separated)
  - Anchor path (must be one of the listed paths)
  - PathCheck (must be exactly `FOUND`)

## C) File-map proof (anchor proof)
Quote file-map lines copied verbatim from docs/generated/01_CONTEXT_PACKET.md.

Rules:
- Quote exactly N lines where N = number of rows in section B.
- Each proof line must correspond to the Anchor path for that row.
- Prepend `ProofRow=Bn:` for each line, matching the row number in section B.
- Each quoted line must include the full anchor path string.

## D) Questions for iteration
- 3 to 6 questions, each <= 14 words, targeted to split/merge decisions.

## Forbidden output
- No JSON.
- No patch plans.
- No “Remaining items” lists.
- No discussion of gates.json schema fields.
- No EPIC_B.
