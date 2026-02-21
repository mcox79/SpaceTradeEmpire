# docs/00_READ_FIRST_LLM_CONTRACT.md

# 00_READ_FIRST_LLM_CONTRACT

This document is the highest-priority contract for any LLM session that touches this repository.

If any instruction conflicts with this contract, this contract wins.


## A) Session modes and priority

Default session modes (applies unless explicitly overridden by the user prompt or the Context Packet):
- OUTPUT_MODE = FULL_FILES
- GIT_MODE = NO_STAGE
- PROFILE = EXPERIMENTATION

Override rule:
- If the user prompt or Context Packet explicitly declares OUTPUT_MODE, GIT_MODE, or PROFILE, those values override the defaults.
- If the user requests a non-default value but does not fully specify it (example: asks for COMMIT but provides no message), STOP and request the missing detail.
- If the user requests committing in natural language (example: "commit this"), treat it as an override request to GIT_MODE = COMMIT:<message> and STOP until a commit message is provided.

STOP rule:
- Do not STOP merely because modes are not restated.
- STOP only when an override is requested but incomplete, or when a required Context Packet or required file input is missing.

Valid OUTPUT_MODE values:
- ANALYSIS_ONLY: audit and plan only. No code. No file replacements.
- FULL_FILES: output complete file replacements for manual copy/paste, one file at a time.
- POWERSHELL: output PowerShell-only atomic write blocks that generate or replace files.

Mode priority rule:
- If OUTPUT_MODE = FULL_FILES, the PowerShell output rules do not apply.
- If OUTPUT_MODE = ANALYSIS_ONLY, no code is emitted.
- All other rules in this contract still apply in every mode.


## B) Git mode (required)

GIT_MODE defaults to NO_STAGE unless explicitly overridden by the user prompt or the Context Packet.
If an override is requested but incomplete (example: COMMIT requested without a message), STOP and request the missing detail.

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


## D1) Gate governance v2.2 (3 surfaces + Pattern A)

This repo uses a 3-surface governance model for Gates and execution Tasks.

Canonical surfaces (roles):
1) Gate ledger (canonical)
- `docs/55_GATES.md`
- Holds the authoritative Gate identity, intent, acceptance criteria, canonical evidence universe, status, and closure proof.
- This is the only authoring surface for Gate scope.

2) Execution queue snapshot (derived)
- `docs/gates/gates.json`
- Holds a token-bounded window of executable Tasks (shards) derived from Gates.
- Tasks MUST reference a Gate via `gate_id`.
- Queue content is operational: TODO and IN_PROGRESS only. DONE tasks do not remain in the queue.
- Recommended queue guardrail: a top-level `pending_completion` marker that prevents starting Step C until Step D has been applied.
- Invariant: queue content must not redefine canonical Gate acceptance criteria or scope (canonical lives only in `docs/55_GATES.md`).
Queue content minimums (contract-level)
- Queue-level: `queue_contract_version`, `queue_intent` (1 to 3 lines), optional `pending_completion`.
- Task-level: `gate_id`, `task_id`, `title`, `intent`, ordered `evidence_paths` (2 to 6), `constraints`, `completion_hint`, `escalation_rule`.
- Tasks may include optional `blocked_by` only when obvious.

3) Completion log (append-only provenance)
- `docs/56_SESSION_LOG.md`
- One line per Task outcome (DONE/BLOCKED/PARTIAL), append-only.
- Pointers only; do not restate Gate specs already in `docs/55_GATES.md`.

Pattern A (hard step boundaries):
A) Gate authoring/refinement (EPICS -> 55)
- Objective: create/refine canonical Gates in `docs/55_GATES.md`.
- Attach: `docs/generated/01_CONTEXT_PACKET.md`, `docs/54_EPICS.md`, `docs/55_GATES.md`, `docs/gates/GATE_FREEZE_RULES.md`, `docs/00_READ_FIRST_LLM_CONTRACT.md`.
- Output allowed: edits to `docs/55_GATES.md` only (plus governance docs only if governance is being modified).

B) Queue build/task shaping (55 -> gates.json)
- Objective: refresh `docs/gates/gates.json` with thin Tasks derived from Gates in 55.
- Attach: `docs/generated/01_CONTEXT_PACKET.md`, `docs/55_GATES.md`, `docs/gates/gates.json`, `docs/gates/gates.schema.json`, `docs/gates/GATE_FREEZE_RULES.md`, `docs/00_READ_FIRST_LLM_CONTRACT.md`.
- Output allowed: edits to `docs/gates/gates.json` only.

C) Execute exactly one Task (lean session)
- Objective: complete exactly one Task from `docs/gates/gates.json`.
- Attach: `docs/generated/01_CONTEXT_PACKET.md`, `docs/gates/gates.json` plus only the evidence files listed in that Task.
- Do not attach by default: `docs/55_GATES.md`, `docs/56_SESSION_LOG.md`, `docs/54_EPICS.md`.
- Escalation rule: if scope ambiguity arises, STOP and request a `docs/55_GATES.md` excerpt for that `gate_id`.

- Output requirement (end of Step C): Completion Record
  - Emit a structured plain-text Completion Record containing:
    - gate_id, task_id
    - summary of what changed
    - proof hint (test name, hash, scenario pass, etc.)
    - any proposed evidence expansion (proposal only)

D) Bookkeeping close (clerical update)
- Objective: apply completion record updates to canonical surfaces.
- Attach: `docs/gates/gates.json`, `docs/56_SESSION_LOG.md`, and only the needed excerpt from `docs/55_GATES.md` when gate rollup status changes.
- Operations: remove completed Task from queue, append one line to session log, update Gate status/closure proof in 55 when warranted.

Deterministic Task selection policy (Step C)
- Prefer tasks marked IN_PROGRESS.
- Prefer fewer evidence_paths.
- Prefer fewer buckets (if buckets are present).
- Prefer tasks that are not blocked (if blocked_by is present).
- Tie-break lexicographically by (gate_id, task_id).
- Requirement: before executing, output the ranked list used to choose the Task, then proceed.

STOP conditions (non-negotiable):
- If a Task references a `gate_id` that cannot be found in `docs/55_GATES.md`: STOP and return to Step A (fix the ledger) or Step B (fix the queue), whichever is correct.
- If a Task requires evidence outside the Gate’s canonical evidence universe in 55: STOP and escalate to Step A/B (do not silently expand scope in Step C/D).
- If a session begins Step C while a prior completion has not been applied (pending bookkeeping): STOP and perform Step D first.



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
- Allowed files list (explicit, repo-relative; default <= 6)
- Validation commands to run after the step
- Definition of Done

Modes field rule (aligned with Section A defaults):
- If OUTPUT_MODE, GIT_MODE, or PROFILE are omitted, defaults apply. Do not STOP.
- If any mode is explicitly overridden, the override must be complete; otherwise STOP (per Section A).

4) Canonical Session Header (preferred format)
Context Packets SHOULD include a short header in this shape. If Modes are omitted, defaults apply (Section A).

Objective:
- <1-3 lines>

Modes (optional):
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
If scope is exactly 1 file and <= 30 changed lines, the Context Packet may be minimal, but is still required and must include:
- Objective (1 line)
- File path
- Validation commands
- Done condition

Modes follow the defaults in Section A unless explicitly overridden.

## F) Workflow profiles and switching rules

This repo supports two workflow profiles. The active profile MUST be stated in the Context Packet.

1) Experimentation profile
- Intent: audits, doc alignment, exploration, and prototype changes.
- Required: GIT_MODE = NO_STAGE
- Rule: no staging, no commits.
- Clean Workbench is NOT required during active experimentation.
Enforcement:
- If PROFILE = EXPERIMENTATION and GIT_MODE is overridden to STAGE or COMMIT, STOP and request the user to switch PROFILE to NORMAL or revert GIT_MODE to NO_STAGE.


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

Commit and clean-tree rules (applies to gate governance work)
- Experimentation profile:
  - No staging, no commits.
  - Working tree does not need to be clean, but churn should be intentional and bounded.

- Normal profile:
  - Commits are allowed only in Normal profile.
  - Before COMMIT:
    - Required validations for the step must pass.
    - Working tree must be intentionally reviewed.
    - For Step C completions: Step D bookkeeping MUST be applied (queue removal + session log append + any required 55 rollup) before committing.
  - Push policy:
    - Push is user-controlled and out of scope for this contract unless explicitly requested in the Context Packet.

Clean Workbench (Normal profile)
- When operating in Normal profile, the Clean Workbench requirement applies unless the Context Packet explicitly scopes an exception.
- If generated artifacts exist, they must either be deterministically regenerated and committed (if intended) or excluded and cleaned before commit.


## G) Output rules by mode


### G0) Default presentation rules for doc updates (all modes)

These are default response-format rules for documentation changes unless the Context Packet explicitly overrides them. Default for documentation changes is PRE/POST snippet patches; use full-file replacement only when explicitly requested or when a change is too broad to safely express as a snippet edit.

1) Document updates (snippets / section edits)
- Provide BOTH the full PRE text and the full POST text in markdown code blocks.
- State the exact location:
  - file path
  - section header or unique anchor line(s)
  - whether the change is an insertion (before/after which line) or a replacement (which block).
- Do not provide partial diffs without the full PRE and POST blocks.

2) Full file replacements
- Provide the filename (repo-relative path) and a single markdown code block containing the complete replacement text.
- Do not include additional partial snippets for the same file in the same step.

3) When additional context is required
- Prefer requesting the minimum required excerpt.
- Provide an exact Windows PowerShell 5.1 command the user can run to print the needed file range or matched section, and ask them to paste the output.


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
- Run the repo’s documented test selection for SimCore:

  Fast loop (default during iteration):
  - `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName!~SimCore.Tests.Determinism.LongRunWorldHashTests&FullyQualifiedName!~SimCore.Tests.GoldenReplayTests"`

  Closeout-only (gate DONE / seal commit / determinism or perf check):
  - `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FullyQualifiedName~SimCore.Tests.Determinism.LongRunWorldHashTests|FullyQualifiedName~SimCore.Tests.GoldenReplayTests"`

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
