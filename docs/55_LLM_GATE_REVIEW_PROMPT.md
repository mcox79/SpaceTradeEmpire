\# 55\_LLM\_GATE\_REVIEW\_PROMPT



Inputs:

\- docs/54\_DEVELOPMENT\_PLAN.md

\- Latest docs/generated/status\_packets/status\_\*.txt

\- Canonical anchors already embedded in the status packet



Task:

Evaluate progress strictly against the plan and anchors. Do not invent scope.



Output exactly these sections:



1\) evidence\_failures

\- connectivity\_violations\_nonempty: true/false

\- tests\_failed: true/false (if evidence present)

\- determinism\_hash\_changed: true/false (if evidence present)

\- anything else blocking gate completion



2\) gate\_movements

For each Gate/Epic ID mentioned in status packet "targets":

\- current\_status\_in\_plan

\- recommended\_new\_status

\- justification with explicit evidence references (file paths or diff sections)



3\) violations

\- Any design-law violations (docs 50–53)

\- Any boundary/interface violations (docs 30, connectivity outputs)

\- Any determinism/testing violations (docs 20, 52)



4\) next\_targets

Return 3 options. Each option is 1–3 Gate/Epic IDs that already exist in docs/54.

Explain why each is the best next move, and what evidence would close it.



5\) plan\_holes

List any plan items that are ambiguous or missing acceptance criteria, and propose minimal edits to docs/54 (do not rewrite the plan).



