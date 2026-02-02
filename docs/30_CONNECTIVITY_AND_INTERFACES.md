\# 30\_CONNECTIVITY\_AND\_INTERFACES



\## A. Purpose

Define the rules and the single canonical vocabulary for “what is allowed to talk to what”, plus how we will produce an always-up-to-date connectivity map.



\## B. Layering rules (locked contract)

\- SimCore must not depend on Godot runtime objects.

\- GameShell may depend on SimCore.

\- Adapters are the only layer allowed to touch both sides.

Violations are architecture bugs. :contentReference\[oaicite:33]{index=33}



\## C. Connectivity map: required outputs (locked contract)

The connectivity scanner must emit a diffable, deterministic map:



1\) connectivity\_manifest.json

\- commit hash (if available), scan timestamp, tool version, repo root

\- exclusions applied (addons, scratch, transient files)



2\) connectivity\_graph.json

\- nodes: file path, layer (SimCore/GameShell/Adapter/Tooling), public API symbols (best-effort), events/signals emitted and consumed (best-effort)

\- edges:

&nbsp; - type: "calls", "imports", "signal\_connect", "event\_publish", "event\_subscribe", "resource\_load", "scene\_instantiates"

&nbsp; - from, to

&nbsp; - evidence: file path + line span



3\) connectivity\_violations.json

\- any forbidden dependency direction

\- any plugin owning authority (if detected)

\- any UI computing logic (best-effort heuristics)



\## D. Explainability and reason codes (locked contract)

Any meaningful negative outcome in SimCore must carry a ReasonCode that UI can display. Events must include stable EntityID for click-through to the relevant inspector context. :contentReference\[oaicite:34]{index=34}



\## E. Scanner-needed sections (do not attempt manually)

\- The definitive list of current signals/events and their producers/consumers

\- The authoritative list of adapters and bridge entrypoints

\- The full call graph between SimCore systems and commands

These must be produced by the tool to avoid drift.



