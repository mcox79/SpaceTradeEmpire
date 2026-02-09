# docs/00_READ_FIRST_LLM_CONTRACT.md

# 00_READ_FIRST_LLM_CONTRACT

This document is the highest-priority contract for any LLM session that touches this repository.

If any instruction conflicts with this contract, this contract wins.


## A) Session modes and priority

Every session MUST explicitly declare:
- OUTPUT_MODE
- GIT_MODE
- Workflow Profile (Experimentation or Normal)

If any are missing, the correct behavior is: STOP and request the missing declaration. Do not assume defaults.

Valid OUTPUT_MODE values:
- ANALYSIS_ONLY: audit and plan only. No code. No file replacements.
- FULL_FILES: output complete file replacements for manual copy/paste, one file at a time.
- POWERSHELL: output PowerShell-only atomic write blocks that generate or replace files.

Mode priority rule:
- If OUTPUT_MODE = FULL_FILES, the PowerShell output rules do not apply.
- If OUTPUT_MODE = ANALYSIS_ONLY, no code is emitted.
- All other rules in this contract still apply in every mode.


## B) Git mode (required)

Every session MUST explicitly declare a GIT_MODE.

Valid GIT_MODE values:
- NO_STAGE: do not stage or commit anything.
- STAGE: staging is allowed once validations pass.
- COMMIT:<message>: staging is allowed and a commit may be created with the provided message after validations pass.

Git mode priority rules:
- Never create a commit unless GIT_MODE = COMMIT:<message> is explicitly set.
- When GIT_MODE = NO_STAGE, do not instruct the user to git add, commit, or amend.
- In FULL_FILES mode, the assistant must state what to do for GIT_MODE after validations, but cannot execute Git actions.


## C) Truthfulness and evidence (non-negotiable)

1) No fabricated repo state
- Do not claim you read files, ran commands, or observed repo state unless the content/output was pasted or uploaded in the current chat.

2) Evidence-before-diagnosis (with fast-debug exception)
- Do not assert a bug or architectural violation as fact without quoting the exact lines that prove it (path + excerpt).
- Fast-debug exception: you may propose up to 3 clearly labeled hypotheses if and only if each includes a single, concrete verification step (exact file path to provide, or exact command to run and paste output).

3) Missing inputs rule
- If a required file or output is missing, request it by exact path and STOP. Do not guess.


## D) Documentation kernel is canonical

The docs kernel is the canonical source of truth for process and routing:
- docs/10_KERNEL_INDEX.md routes what is canonical vs generated.
- docs/10_ARCHITECTURE_INDEX.md routes architecture excerpts. Do not attach the full architecture by default.
- docs/40_TOOLING_AND_HOOKS.md is the canonical operational command reference.

If a workflow instruction changes, update the relevant canonical doc first, then update tooling.


## E) Required session packet and attachment rules (LLM-first workflow)

1) Context Packet is required
- Every session must be anchored by a generated Context Packet:
  - docs/generated/01_CONTEXT_PACKET.md

2) Template is not a session attachment
- The template exists for generation only:
  - docs/templates/01_CONTEXT_PACKET.template.md
- Do not attach the template in sessions. Attach only the generated packet.

3) Minimum required Context Packet fields
- Objective (1 to 3 lines)
- OUTPUT_MODE, GIT_MODE, Workflow Profile
- Allowed files list (explicit, repo-relative; default <= 6)
- Validation commands to run after the step
- Definition of Done

4) Canonical Session Header (required format)
Every Context Packet MUST include a short header in this shape:

Objective:
- <1-3 lines>

Modes:
- OUTPUT_MODE = <ANALYSIS_ONLY | FULL_FILES | POWERSHELL>
- GIT_MODE = <NO_STAGE | STAGE | COMMIT:...>
- PROFILE = <EXPERIMENTATION | NORMAL>

Allowlist (repo-relative):
- <path 1>
- <path 2>
- ...

Validation commands:
- <command 1>
- <command 2>
- ...

Definition of Done:
- <bullets>

5) Micro-change exception (Mini Packet)
If scope is exactly 1 file and <= 30 changed lines, the Context Packet may be minimal, but must still declare:
- Objective (1 line)
- File path
- Validation commands
- Done condition
- OUTPUT_MODE, GIT_MODE, and PROFILE


## F) Workflow profiles and switching rules

This repo supports two workflow profiles. The active profile MUST be stated in the Context Packet.

1) Experimentation profile
- Intent: audits, doc alignment, exploration, and prototype changes.
- Required: GIT_MODE = NO_STAGE
- Rule: no staging, no commits.
- Clean Workbench is NOT required during active experimentation.

2) Normal profile
- Intent: standard development, integration, and durable changes.
- Allowed: GIT_MODE = STAGE or GIT_MODE = COMMIT:<message>
- Clean Workbench is REQUIRED when operating in Normal profile (no accidental churn).

Switching rules (Experimentation -> Normal)
- Connectivity scanner deterministic outputs have been verified on your machine (repeat run, identical hashes), when relevant.
- The plan is explicit (files, validations, Definition of Done).
- The scope is stable enough that staging or sealing is useful.
- Working tree has been intentionally reviewed (no accidental generated noise or unintended edits).

Switching rules (Normal -> Experimentation)
- You are investigating a failure mode, uncertain behavior, or repo hygiene issue where commits would add churn.
- You are doing broad refactors without a stable validation story yet.


## G) Output rules by mode

### G1) OUTPUT_MODE = POWERSHELL

1) PowerShell output rule
- Output PowerShell code blocks only.
- Unless explicitly requested otherwise in the Context Packet, output exactly one executable Windows PowerShell 5.1 code block for the current step.

If the step is multi-file and the Context Packet requests "one file at a time", output one PowerShell block that writes exactly one file.

2) What the script must do
- Resolve repo root dynamically using git rev-parse --show-toplevel.
- Write only the agreed target files, using the Atomic Write Pattern in section H.
- Create target directories before writing.
- Run required validations at the end.
- Exit non-zero on failure.
- Apply GIT_MODE behavior (stage/commit) only if PROFILE = NORMAL and only after validations pass.

3) What the script must not do
- No Set-Content, Add-Content, Out-File, redirection (> or >>), or other writing methods that can introduce BOMs or newline drift.
- No hardcoded absolute paths.
- No writing outside the repo root.


### G2) OUTPUT_MODE = FULL_FILES

1) Full file replacements only
- Output complete file replacements only. No partial snippets.
- Output one file at a time.
- Each file must include its exact repo-relative path above the code block.

2) Formatting and safety
- Preserve existing formatting unless changes are required.
- For .gd files: tabs-only indentation. No spaces for indentation.
- Do not introduce smart quotes, invisible Unicode, or zero-width characters.
- Avoid incidental reformatting.

3) Validation instructions
- After each file, include the exact validation commands the user should run.

4) Git behavior in FULL_FILES mode
- The assistant must state what to do for GIT_MODE after validations.
- The assistant must not instruct staging/commit actions that contradict the declared GIT_MODE.


### G3) OUTPUT_MODE = ANALYSIS_ONLY

- Provide a short plan and request only the minimum additional inputs needed (exact repo-relative paths or command outputs).
- Do not emit code.


## H) Atomic Write Pattern (PowerShell sessions)

This section is authoritative for file writing in OUTPUT_MODE = POWERSHELL.

H1) Encoding and newline rules (required)
- All text files MUST be written as UTF-8 no BOM.
- Newlines MUST be deterministic:
  - Prefer LF internally.
  - Ensure exactly one trailing newline at end-of-file.

H2) Array-of-strings rule (required default)
For text files that are human-authored source or docs (including but not limited to .md, .gd, .cs, .ps1, .json when manually edited):
- Content MUST be represented as an array of strings (one element per line).
- The writer MUST join lines with "`n" and append a final "`n".
- The writer MUST write using .NET with UTF8 no BOM (for example, [System.IO.File]::WriteAllText).

Rationale (operational):
- Prevents BOM insertion and PowerShell newline normalization surprises under Windows PowerShell 5.1.
- Makes it harder to accidentally introduce invisible characters or mangled quoting.

H3) Narrow exception: deterministic machine-emitted text (allowed only when explicitly justified)
Exception is allowed ONLY when ALL are true:
- The file is machine-emitted output (example: deterministic tool JSON artifacts), not a human-authored source file.
- The content is produced deterministically within the same script (stable ordering, no timestamps, no absolute paths).
- The script normalizes newlines to LF and enforces exactly one trailing newline.
- The script writes using [System.IO.File]::WriteAllText(path, content, UTF8NoBOM). No PowerShell writers.

If any of the above is not true, do not use the exception.

H4) Post-write BOM gate (required)
For any text file the script writes:
- Re-read the file as bytes and FAIL if a UTF-8 BOM is present.

H5) Directory creation
- Create target directories before writing.
- Do not create or modify unrelated directories.

H6) Do not dirty the repo unintentionally
- Avoid writing to scratch/generated locations unless explicitly part of the step.
- If a validator must create temporary files, it must clean them up and avoid leaving a dirty tree when possible.


## I) Validation gatekeeper (required)

Only run validations/tests that exist in-repo. Do not invent commands that are not present.

If any .gd files changed
- Run scripts/tools/Validate-GodotScript.ps1 on each changed file.

If any .cs files changed
- Run dotnet build.
- If a relevant test project exists and is part of the workflow, run dotnet test (or the repo’s documented test runner).

If any .ps1 files changed
- Run a PowerShell parse check against the on-disk file (or staged blob when relevant to hooks/commit gates).

Hook safety (commit-time gates)
- Pre-commit gates may validate staged content only.
- If you modify pre-commit behavior or scripts that validate staged content, ensure the validator reads the staged blob, not the working tree.
- Exercise the hook entrypoint in a way that matches real usage (the same path Git uses).


## J) Generated artifacts policy (operational)

Generated artifacts live under:
- docs/generated/

Rules:
- Prefer referencing generated artifacts by path rather than pasting large outputs into chat.
- Attach docs/generated/01_CONTEXT_PACKET.md for each session.
- Status Packet is optional; Context Packet remains mandatory.

Deterministic, diff-friendly artifacts (eligible to commit later by policy decision)
- docs/generated/01_CONTEXT_PACKET.md
- docs/generated/02_STATUS_PACKET.txt
- docs/generated/connectivity_manifest.json
- docs/generated/connectivity_graph.json
- docs/generated/connectivity_violations.json
- docs/generated/gate_closure_delta.md
- docs/generated/capability_index.md

Ephemeral outputs (do not commit)
- verbose console logs
- timestamped traces
- machine-local scratch files

Session attachments rule:
- Attach docs/generated/02_STATUS_PACKET.txt when doing gate closure work (it may include “TOP GATE CLOSURE BLOCKERS” when gate_closure_delta exists).
- Attach docs/generated/connectivity_violations.json only when non-empty or needed for diagnosis.
- Do not paste full JSON outputs by default; include only targeted excerpts when necessary.

## K) Git hygiene (required)

K1) Clean Workbench Protocol (applies to Normal profile and profile switching)
- In NORMAL profile, do not proceed with multi-step work while the working tree is unintentionally dirty.
- Switching from EXPERIMENTATION to NORMAL requires an intentional review of the working tree.

K2) Seal-then-Validate Protocol (NORMAL profile only)
When operating in NORMAL profile and changes span multiple interdependent files:
- Write all changes first.
- Validate.
- If sealing is required, use GIT_MODE = COMMIT:<message> explicitly to create the seal commit.
- Fix and amend until validations pass.
- Only proceed once a passing sealed state exists.


## L) Modularity and contract headers (required intent, scoped enforcement)

L1) File size and coupling targets
- Soft target: 150 to 350 lines per file.
- Review trigger: >350 lines requires either a split or a justification.
- Strong cap: 600 lines except rare adapters or registries.

L2) Contract Header requirement (scoped)
Required for:
- new files in SimCore or Adapter layers
- files >350 lines
- files whose behavior is referenced by tooling, hooks, or tests

Recommended (but not required) for:
- small glue files
- legacy files being lightly touched

Contract Header contents:
- Purpose
- Layer (SimCore, GameShell, Adapter, Tooling)
- Dependencies
- Public API
- Events/Signals (emitted/consumed)
- Invariants
- Tests (how to validate)


## M) Canonical source-of-truth rule

If chat instructions conflict with canonical repo contracts or architecture invariants:
- update the canonical document first before implementing code changes.
