# 00_READ_FIRST_LLM_CONTRACT

This document is the highest-priority contract for any LLM session that touches this repository.

If any instruction conflicts with this contract, this contract wins.

## A) Session modes and priority

Every session must declare an OUTPUT_MODE. If it is not declared, default is `POWERSHELL`.

Valid modes:
- `POWERSHELL`: the assistant outputs one executable Windows PowerShell 5.1 block that writes files using the Atomic Write Pattern.
- `FULL_FILES`: the assistant outputs complete file replacements for manual copy/paste, one file at a time.
- `ANALYSIS_ONLY`: the assistant outputs analysis and a request for the minimum missing inputs. No code, no file writing.

Mode priority rule:
- If `OUTPUT_MODE = FULL_FILES`, the “single PowerShell block” requirement does not apply.
- If `OUTPUT_MODE = ANALYSIS_ONLY`, no code is emitted.
- All other rules in this contract still apply in every mode.

## B) Git mode (required)

Every session must declare a GIT_MODE. If it is not declared, default is `STAGE`.

Valid modes:
- `STAGE`: stage all changes at the end of each successful step.
- `NO_STAGE`: do not stage anything.
- `COMMIT:<message>`: stage all changes and create a commit with the provided message.

Git mode priority rule:
- Never create a commit unless `GIT_MODE = COMMIT:<message>` is explicitly set.
- In `FULL_FILES` mode, the assistant must still state what to do for GIT_MODE (stage/commit), but cannot execute it.

## C) Truthfulness and evidence (non-negotiable)

1) No fabricated repo state
- Do not claim you read files, ran commands, or observed repo state unless the content/output was pasted or uploaded in the current chat.

2) Evidence-before-diagnosis (with fast-debug exception)
- Do not assert a bug or architectural violation as fact without quoting the exact lines that prove it (path + excerpt).
- Fast-debug exception: you may propose up to 3 clearly labeled hypotheses if and only if each includes a single, concrete verification step (exact file path to provide, or exact command to run and paste output).

3) Missing inputs rule
- If a required file or output is missing, request it by exact path and stop. Do not guess.

## D) Output rules by mode

### D1) OUTPUT_MODE = POWERSHELL

1) Master Output Rule (PowerShell)
- Output exactly one executable Windows PowerShell 5.1 code block.
- The script must be self-contained and runnable from any directory inside the repo.

2) What the script must do
- Resolve repo root dynamically using `git rev-parse --show-toplevel`.
- Write only the agreed target files, using the Atomic Write Pattern.
- Run required validation at the end.
- Exit non-zero on failure.
- Apply GIT_MODE behavior (stage/commit) only after validations pass.

3) What the script must not do
- No `Set-Content`, `Add-Content`, `Out-File`, redirection (`>`, `>>`), or other writing methods that can introduce BOMs or newline drift.
- No hardcoded absolute paths.
- No writing outside the repo root.

### D2) OUTPUT_MODE = FULL_FILES

1) Master Output Rule (Full file replacements)
- Output complete file replacements only. No partial snippets.
- Output one file at a time.
- Each file must include its exact repo-relative path above the code block.

2) Formatting and safety
- Preserve existing formatting unless changes are required.
- For `.gd` files: tabs-only indentation. No spaces for indentation.
- Do not introduce smart quotes or zero-width characters.
- Avoid incidental reformatting.

3) Validation instructions
- After each file, include the exact validation commands the user should run.

4) Git behavior in FULL_FILES mode
- The assistant must state what to do for GIT_MODE (STAGE/NO_STAGE/COMMIT) after validations.

### D3) OUTPUT_MODE = ANALYSIS_ONLY

- Provide a short plan and request only the minimum additional inputs needed (exact repo-relative paths or command outputs).
- Do not emit code.

## E) Atomic Write Pattern (PowerShell sessions)

When writing files in `POWERSHELL` mode, the script must:
- Write UTF-8 no BOM.
- Emit content as an array of strings (line-by-line), not a multi-line string.
- Ensure deterministic trailing newline behavior.
- Create target directories before writing.

## F) Validation gatekeeper (required)

Only run validations/tests that exist in-repo. Do not invent commands that are not present. If a desired harness does not exist yet, write a TODO and stop after validations.

### F1) If any `.gd` files changed
- Run `scripts/tools/Validate-GodotScript.ps1` on the changed files.
- `.gd` files must be tabs-only. If the validator reports tab/indent issues, fix before proceeding.

### F2) If any `.cs` files changed
- Run `dotnet build` for the affected solution/project (repo-relative).
- If a test project exists and is part of the workflow, run `dotnet test` (or the repo’s documented test runner).

### F3) If any `.ps1` files changed
- Run a PowerShell parse check against the on-disk file (or staged blob when relevant to hooks/commit gates).

### F4) Hook safety (when changing commit gates / hooks)
- If you modify pre-commit behavior or scripts that validate staged content, you must validate against the staged blob or exercise the hook entrypoint in a way that matches real usage.

## G) Git hygiene (required)

1) Clean Workbench Protocol
- Do not transition between major slices or sessions with a dirty working tree, unless the explicit task is “repair the dirty tree.”

2) Seal-then-Validate Protocol (for multi-file interdependent changes)
When changes span multiple interdependent files:
- Write all changes first.
- Validate.
- If sealing is required, use `GIT_MODE = COMMIT:<message>` explicitly to create the seal commit.
- Fix and amend until validations pass.
- Only proceed once a passing sealed state exists.

## H) Modularity and contract headers (required)

1) File size/coupling targets
- Soft target: 150 to 350 lines per file.
- Review trigger: >350 lines requires either a split or a justification.
- Strong cap: 600 lines except rare adapters/registries.

2) Contract Header requirement
Every non-trivial file must begin with a Contract Header documenting:
- Purpose
- Layer (SimCore/GameShell/Adapter/Tooling)
- Dependencies
- Public API
- Events/Signals (emitted/consumed)
- Invariants
- Tests (how to validate)

## I) Module Packet requirement (LLM-first workflow)

For any coding session, a Module Packet is required before writing.

Minimum Module Packet fields:
- Objective (1 to 3 lines)
- OUTPUT_MODE and GIT_MODE
- Allowed files list (explicit, repo-relative; default <= 6)
- Validation command(s) to run after the step
- Definition of Done

Micro-change exception (to reduce friction):
- If change scope is exactly 1 file and <= 30 changed lines, a Mini Packet is sufficient:
  - Objective (1 line)
  - File path
  - Validation command(s)
  - Done condition

If a request cannot be safely executed without a Module Packet (or Mini Packet), request the minimum missing inputs and stop.

## J) Canonical source-of-truth rule

If chat instructions conflict with canonical repo contracts or architecture invariants, update the canonical document first before implementing code changes.
