\# 55\_LLM\_GATE\_REVIEW\_PROMPT


If the user message is empty or contains only attachments, proceed using this document and the attached inputs without asking for clarification.


Preferred user launch line (optional):

"Run the Gate Review exactly as specified in docs/55\_LLM\_GATE\_REVIEW\_PROMPT.md."



Inputs (attached):

\- docs/54\_DEVELOPMENT\_PLAN.md

\- docs/55\_LLM\_GATE\_REVIEW\_PROMPT.md

\- docs/generated/01\_CONTEXT\_PACKET.md

\- docs/generated/02\_STATUS\_PACKET.txt

\- docs/generated/connectivity\_manifest.json

\- docs/generated/connectivity\_violations.json

\- docs/generated/05\_TEST\_SUMMARY.txt (if present)



Task:

Evaluate progress strictly against the plan and anchors. Do not invent scope.



Output exactly these sections:



1\) evidence\_failures

\- connectivity\_violations\_nonempty: true/false

\- tests\_failed: true/false (if evidence present)

\- determinism\_hash\_changed: true/false (if evidence present)

\- anything else blocking gate completion



2\) gate\_movements

For each Gate/Epic ID that appears in docs/54 and has evidence in 02\_STATUS\_PACKET.txt:

\- current\_status\_in\_plan

\- recommended\_new\_status

\- justification with explicit evidence references (file paths or diff sections)



3\) violations

\- Any design-law violations (docs 50–53)

\- Any boundary/interface violations (docs 30, connectivity outputs)

\- Any determinism/testing violations (docs 20, 52)

\- godot\_layer\_starvation: true/false — true if no gate with a scripts/ or scenes/ anchor path has been completed in the last 5 gate closures (check docs/55\_GATES.md). If true, flag as a priority correction item in next\_targets: at least 1 of the 3 options must target EPIC.S1.HERO\_SHIP\_LOOP.V0 or another IN\_ENGINE gate.



4\) next\_targets

Return 3 options. Each option is 1–3 Gate/Epic IDs that already exist in docs/54.

Explain why each is the best next move, and what evidence would close it.



5\) plan\_holes

List any plan items that are ambiguous or missing acceptance criteria, and propose minimal edits to docs/54 (do not rewrite the plan).

