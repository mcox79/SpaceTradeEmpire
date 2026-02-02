\# 90\_GLOSSARY\_AND\_IDS



\## Layers

\- SimCore: authoritative headless simulation domain

\- GameShell: Godot presentation and input layer

\- Adapter: thin boundary layer allowed to reference both sides (bridge, wrappers) :contentReference\[oaicite:39]{index=39}



\## Testing terms

\- Determinism: same InitialSeed + same CommandList => bitwise-identical FinalState :contentReference\[oaicite:40]{index=40}

\- Invariant: assertion checked every DayTick; failure halts simulation :contentReference\[oaicite:41]{index=41}

\- Scenario: versioned file defining a headless run; smallest integration unit :contentReference\[oaicite:42]{index=42}



\## Process terms

\- Module Packet: curated working set for an LLM session (scope, allowed files, validation steps, DoD) :contentReference\[oaicite:43]{index=43}

\- Clean Workbench: no dirty git tree between major slices/sessions :contentReference\[oaicite:44]{index=44}

\- Seal-then-Validate: WIP commit first, then validate and amend until passing :contentReference\[oaicite:45]{index=45}



\## Explainability

\- ReasonCode: attached to meaningful state changes so UI can explain “why”

\- EntityID: stable identifier used to link notifications to inspectable objects :contentReference\[oaicite:46]{index=46}



