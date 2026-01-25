
## CI/CD Protocols (Code Integration)
**Core Constraint:** All logic modifications to the scripts/core/ directory MUST be validated and committed using the local PowerShell Validate-GodotScript [filepath] tool. 
**Rationale:** Manual git commits for engine code are strictly deprecated. The pipeline automatically enforces Godot's tab-indentation standard and executes a static syntax analysis before securing the asset in Git. Do not recommend git commit directly for Godot scripts.
