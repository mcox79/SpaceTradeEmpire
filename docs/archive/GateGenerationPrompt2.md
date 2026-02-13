# Planning Mode A1.5 (Hardened): Emit FINAL_APPROVED_SEGMENTATION_BLOCK from latest Prompt 1 output + policy

You are in Planning Mode only.

## Inputs source
Use the MOST RECENT occurrences in this chat of:
A) A Prompt 1 segmentation table ("Candidate segmentation") OR an existing "FINAL_APPROVED_SEGMENTATION_BLOCK".
B) The user’s answers to the Prompt 1 questions (already in this chat) OR an explicit DECISION_POLICY block.

Do NOT ask the user to paste anything unless A or B truly cannot be found in the chat transcript.

If A is not found: emit diagnostics + UNABLE_TO_EMIT_BLOCK.
If B is not found: emit diagnostics + UNABLE_TO_EMIT_BLOCK.

## Non-negotiable policy constants (do not infer these)
You MUST enforce axis diversity with EXACTLY these required axes, in this order:
required_axes = ordering, serialization, id_stability, rng_streams, time_sources
target_row_count = 5

Interpretation rule:
- You must select exactly 1 row for each axis in required_axes.
- You may NOT output any duplicate axis.
- If you cannot find a candidate row for any axis: fail.

## Additional policy derived from user answers (allowed)
Derive ONLY these flags from user answers (if answers are unclear, choose NO/either defaults):
- include_scripts_first_class: YES|NO
- rng_stream_focus: simcore_prefer|simcore_strict|scripts_prefer|scripts_strict|either
- avoid_logistics_focus: YES|NO

rng_stream_focus semantics:
- simcore_strict: rng_streams selected row MUST NOT include any `scripts/` path.
- simcore_prefer: prefer a row with more SimCore paths, but scripts allowed if no alternative.
- scripts_strict: rng_streams row MUST include at least one `scripts/` path.
- scripts_prefer: prefer scripts if available.
- either: no constraint.

avoid_logistics_focus semantics:
- YES forbids selecting any row whose Attach contains `SimCore/Systems/LogisticsSystem.cs`, unless there is no candidate for that axis without it.

## Candidate parsing
1) If the most recent artifact is already a FINAL_APPROVED_SEGMENTATION_BLOCK, treat its rows as candidates.
2) Else parse the most recent Prompt 1 table.

Normalize attachment sketches into a list:
- split on `;`, newlines, and `<br>`
- trim whitespace
Eligibility:
- must have at least 2 paths

## Selection algorithm (deterministic)
For each axis in required_axes, choose the best candidate row with that axis, subject to policy constraints.

Tie-breaks (apply in order):
1) higher Feasibility (if present)
2) fewer paths in Attach (after normalization)
3) lexicographic by Anchor path

## Output requirements
ALWAYS print diagnostics first, then either UNABLE_TO_EMIT_BLOCK or the final table.

DIAGNOSTICS must include:
- source_used: candidates_from, policy_from
- parsed_rows, eligible_rows
- derived_policy flags + the constants required_axes and target_row_count
- per-axis selection summary:
  - axis: selected_anchor OR MISSING
- hard check results (computed from the selected rows, not asserted):
  - axes_covered_exactly_once: YES|NO
  - missing_axes: <list>
  - duplicates: <list>
  - target_row_count_satisfied: YES|NO
  - scripts_constraint_satisfied: YES|NO
  - rng_focus_satisfied: YES|NO
  - logistics_avoidance_satisfied: YES|NO
END_DIAGNOSTICS

UNABLE_TO_EMIT_BLOCK condition:
If any hard check is NO, output only `UNABLE_TO_EMIT_BLOCK` after diagnostics.

If PASS, output:

EPIC_A: EPIC.X.DETERMINISM

## FINAL_APPROVED_SEGMENTATION_BLOCK
| SEG | Label | Axis | Anchor | Attach (2 to 6 paths, `; ` separated) |

Rules:
- SEG must be 01..05 in required_axes order.
- Label must be copied EXACTLY from the chosen candidate row (no paraphrase).
- Anchor MUST equal the first Attach path.
- Attach MUST be a single line and include 2..6 normalized paths.
