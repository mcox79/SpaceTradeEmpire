# SPACE TRADE EMPIRE: PROJECT CONTEXT

<!-- CONTEXTGEN:BEGIN_PART_A -->
## PART A: REPO INTERACTION CONTRACT (CANONICAL FOR CODING)

## 1. The Founder Protocol (STRICT AI OUTPUT CONSTRAINTS)

**Voice:** Pragmatic, commercial, technical. Value-oriented and high-signal.

**THE MASTER OUTPUT RULE:** Never output partial file contents intended for manual insertion. All runnable output must be a single, executable PowerShell block that writes files using the Atomic Write Pattern.

**The Automated Deployment Pattern:**
I interact with PowerShell using a strict, automated pipeline designed to ensure code safety, validation, and correct formatting before committing changes.

### 1.1 The Atomic Write Pattern (Strict)
I do not output raw code for manual copy-pasting. Instead, I generate a single, executable PowerShell block that handles file I/O safely.

- **Binary-Safe Writing:** Use `[System.IO.File]::WriteAllText` with `UTF8Encoding($false)` (No BOM). Avoid redirection and avoid tools that inject BOMs or normalize whitespace.
- **Array-of-Strings Format:** Define file content as an array of strings (example: `@("line 1", "line 2")`), not a multi-line string.
- **Explicit Tabs in .gd:** Use explicit `` `t `` characters for all indentation in `.gd` files (tabs-only policy).
- **Infrastructure-First:** Always `New-Item -ItemType Directory -Force` for the target directory before writing files.

### 1.2 CI/CD Gatekeeper (Strict)
Every time I write a script, I immediately verify it. The PowerShell block includes commands to:

- **Validate Syntax:** Invoke `Validate-GodotScript "path/to/script.gd"` immediately after writing.
- **Run Integration Tests:** If the change touches the economy, run the headless test runner (`test_economy_core.tscn`).

### 1.3 Dynamic Path Resolution (Strict)
I never assume where the project is located.

- **Protocol:** Use `git rev-parse --show-toplevel` to find the repo root dynamically.
- **The "No-Assumption" Path Rule:** Never hardcode `.sln` or `.csproj` paths. Resolve dynamically.
- **Documentation-only example:** The snippet below is an illustrative example only. It must not be emitted as standalone runnable output. All runnable output must be a single PowerShell block per the Master Output Rule.

    $root = (& git rev-parse --show-toplevel).Trim()
    $target = Get-ChildItem -Path $root -Include "*.sln","*.csproj" -Recurse -Depth 2 |
              Where-Object { $_.FullName -notmatch "godot" } |
              Select-Object -First 1

### 1.4 Environment Safety (Strict)
- **No Raw Variables:** Do not call environment variables like `$GodotExe` directly.
- **Safe Inspection:** Inspect safely and provide fallbacks to system defaults to prevent pipeline halts.

## 2. Recovery and Toolchain

If the repo gets into a broken state, get back to a clean baseline before doing more work.

### 2.1 Baseline sanity checks (run from repo root)
- git status -sb
- pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\check_tabs.ps1
- cmd.exe /c ".git\hooks\pre-commit.cmd & if errorlevel 1 (echo HOOK_RC=1) else (echo HOOK_RC=0)"

### 2.2 PowerShell path gotcha
- In some shells, `[Environment]::CurrentDirectory` can differ from your visible prompt path.
- If you see paths resolving under `C:\WINDOWS\system32`, fix it with:
  - `[Environment]::CurrentDirectory = (Resolve-Path -LiteralPath ".").Path`

### 2.3 Markdown corruption recovery
If `_PROJECT_CONTEXT.md` or other markdown gets corrupted (unclosed fences, bad paste):
- Prefer restoring from git or a known-good backup.
- Prefer editing in your editor, not inside an interactive PowerShell prompt.
- If you must patch via PowerShell, use the Atomic Write Pattern described in Section 1.1 (array-of-strings + UTF-8 no BOM).

### 2.4 Context dump toolchain
- DevTool.ps1 provides `Run-ContextGen`, which writes:
  - `_scratch\_FullProjectContext.txt`
- When running headless (avoid UI prompts), set:
  - `$global:DEVTOOL_HEADLESS=$true`

### 2.5 Canonical files policy (Strict)
The canonical sources of truth are:
- `_PROJECT_CONTEXT.md`: workflow rules, guardrails, and contracts
- `DevTool.ps1`: context dump generator (`Run-ContextGen`)
- `scripts\check_tabs.ps1` and `scripts\tools\check_tabs_lib.ps1`: staged tabs-only gate for `.gd`
- `scripts\tools\install_hooks.ps1`: installs Git pre-commit hooks for Windows

Context dumps:
- Output file: `_scratch\_FullProjectContext.txt`
- Contract: the tree and file-contents sections must exclude:
  - scratch directories (`_scratch/`, `._scratch/`)
  - addon content (`addons/`)
  - scripting/tooling and transient files: `.ps1`, `.uid`, `.bak`, `.lnk`, files named with `- Copy`, `temp_validator.gd`
- Narrative mentions in `_PROJECT_CONTEXT.md` are allowed; the contract applies to enumerated tree entries and dumped file contents.

If there is any discrepancy between a chat instruction and these canonical files, treat the canonical files as authoritative and update them first.

### 2.6 Git Hygiene and Session Checkpoints (Strict)
- **The "Clean Workbench" Protocol:** Never transition between major architectural Slices or AI chat sessions with a dirty Git working tree.
- **Milestone Commits:** At the conclusion of a feature vertical, execute cleanup to purge build artifacts (`.uid`, temp files) and seal the state with a distinct milestone commit (example: `git commit -m "feat(milestone): complete slice 6..."`).
- **The Rollback Guarantee:** This ensures `HEAD` is always a verified, commercially viable baseline.

### 2.7 The "Seal-then-Validate" Protocol (Strict)
The `Validate-GodotScript` tool enforces a clean working tree to prevent drift. When refactoring multiple interdependent files (where File A depends on uncommitted changes in File B):
1. **Write:** Generate all updated files using the Atomic Write Pattern.
2. **Seal:** Immediately execute a WIP commit: `git add -A; git commit -m "wip: [feature] pending validation"`.
3. **Validate:** Run `Validate-GodotScript` on the target files.
4. **Rectify:** If validation fails, fix the errors, then `git add -A` and `git commit --amend --no-edit`.
5. **Finalize:** Only proceed to the next Phase once validation passes on the sealed commit.

## 3. Architecture and Standards (Strict)

### 3.1 LLM-First Modularity Protocol (Strict)

Goal: keep code changes safely within an LLM-sized working set while maintaining architectural integrity.

#### A) File size and coupling budgets (practical, not aesthetic)
- Soft target: 150 to 350 lines per file.
- Review trigger: if a file exceeds 350 lines, the change must either:
  - split responsibilities into smaller files, or
  - justify why the file is “bounded glue” (adapter/registry) in the Contract Header.
- Strong cap: 600 lines per file except for rare, explicitly labeled adapters/registries.

A file must be split when it violates any of the following:
- More than one primary responsibility (ex: “route evaluation” plus “UI rendering”).
- More than 4 non-standard-library dependencies (C#: non-BCL `using` dependencies. GDScript: `preload/load`, autoload access, hard-coded node paths, cross-system signals, or direct references into other systems).
- It exposes more than 7 public methods that are not trivial accessors.
- It mixes domain logic with engine/UI concerns (except in adapters by design).

#### B) Contract Headers (required at the top of every non-trivial file)
Definition of “non-trivial file”:
- Any file with more than ~30 lines, or any file containing domain logic (not just constants, pure data, or a tiny glue shim).

Every non-trivial file must begin with a Contract Header describing:
- Purpose: what this file owns (one sentence).
- Layer: SimCore vs GameShell vs Adapter vs Tooling.
- Dependencies: the specific modules/types it is allowed to call.
- Public API: the functions/classes other files are allowed to use.
- Events/Signals: what it emits and what it listens to (if applicable).
- Invariants: 2 to 5 rules that must remain true.
- Tests: the test file(s) that validate this behavior, or “none yet”.

Canonical templates (use one of these formats exactly):

GDScript (`.gd`):
```gdscript
# Contract Header
# Purpose:
# Layer:
# Dependencies:
# Public API:
# Signals:
# Invariants:
# Tests:
```

C# (`.cs`):
```csharp
// Contract Header
// Purpose:
// Layer:
// Dependencies:
// Public API:
// Events:
// Invariants:
// Tests:
```

#### C) Contracts live in one place, not everywhere
To prevent drift, shared assumptions and integration points must be expressed as contracts (interfaces/DTOs/events), not repeated prose across files.

Avoid copying the same explanation into multiple files.

Shared assumptions must be represented as:
- a contract/interface file (preferred), or
- a data schema/DTO definition, or
- a single canonical doc section referenced by name.

#### D) LLM Module Packets (required for any coding session)
Any request to an LLM to implement a change must include a “Module Packet” containing:
1) Scope statement (what outcome is required).
2) A list of files allowed to change (default: <= 6).
3) For each file:
   - the Contract Header
   - its public API surface
   - explicit dependencies and extension points
4) Validation commands to run after writing (Validate-GodotScript, relevant tests).
5) Definition of Done: observable behavior changes and tests passing.

Use Run-ContextGen to produce the repo snapshot, but the Module Packet is the curated working set that keeps the LLM from wandering.

#### E) Dependency direction is enforced
- SimCore or headless domain logic must not depend on Godot runtime objects (follow the non-negotiable architecture invariants).
- GameShell can depend on SimCore.
- Adapters are the only layer allowed to “touch both sides”.

Violations must be treated as architecture bugs, not style issues.

### NON-NEGOTIABLE ARCHITECTURE INVARIANTS

#### 1. The Sim Core Data Purity Rule
The headless simulation (`res://scripts/core/sim/`) is the sole authoritative source of truth. To guarantee deterministic replays and network-safe states, it is subject to a strict type blacklist.
* **PROHIBITED in the Sim Core:** Godot `Node`, `Resource`, `AStar3D`, `Vector3`, `RandomNumberGenerator`, and any class inheriting from `RefCounted` that calls engine-specific physics or rendering APIs.
* **ALLOWED in the Sim Core:** Standard GDScript primitives (`int`, `float`, `String`, `Array`, `Dictionary`) and Plain Old Data (POD) structs.

#### 2. Headless Pathfinding Standard
No Godot-native navigation nodes may be used for world logic. Strategic map routing must utilize a custom, array-based Graph Search algorithm (BFS/Dijkstra) running entirely on standard Dictionaries and Arrays.

#### 3. The Golden Replay Blocker
No system is considered "complete" until it passes `test_replay_golden.gd`. This test is the source of truth for the tick count, seed, and hash/signature method. If any of those baselines change, the change must be explicit, justified, and treated as a golden replay baseline update (never accidental drift).

### File organization
- `/scenes`: visuals and prefabs
- `/scripts`: logic
- `/assets`: raw imports (models, sounds)

### Naming
- Nodes: PascalCase (example: `ScoreLabel`)
- Scripts and files: snake_case (example: `game_manager.gd`)

### Design patterns
- **Signal bus:** Use `GameManager` (Autoload) to pass data between unrelated objects (example: Asteroid ? UI)
- **Composition:** Avoid deep inheritance. Prefer child nodes to add features

### Camera rule
- **The drone camera** must be top-level or code-spawned
- It must never be parented to Player

### The Game Loop (Strict)
- Decoupled Economy: The economic simulation must never run on the render framerate (_process). It must execute on a deterministic, fixed interval using a Timer node or manual delta tracking.

## 4. GDScript Editing and Indentation Policy (Strict)

This project enforces tabs-only indentation for all .gd files. Violations are build-breaking.

### What is forbidden in .gd
- Any leading spaces used as indentation
- Mixed indentation where tabs are followed by spaces
- Trailing whitespace at end of line
- UTF-8 BOM
- Zero-width characters: FEFF, 200B, 200C, 200D, 2060

### Enforcement: staged gate (primary)
The commit gate runs on staged .gd files only (not the working tree) and ignores:
- addons/
- _scratch/
- ._scratch/

Files:
- scripts\check_tabs.ps1 (runner)
- scripts\tools\check_tabs_lib.ps1 (library with the checks)

Manual run:
- pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\check_tabs.ps1

### Git hooks on Windows (Git for Windows)
Git for Windows runs hooks under sh, so we install:
- .git\hooks\pre-commit (sh wrapper)
- .git\hooks\pre-commit.cmd (cmd runner that calls PowerShell)

Install or refresh hooks:
- pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\tools\install_hooks.ps1

Smoke test hook result (cmd-native, reliable):
- cmd.exe /c ".git\hooks\pre-commit.cmd & if errorlevel 1 (echo HOOK_RC=1) else (echo HOOK_RC=0)"

### Secondary enforcement: Validate-GodotScript
Validate-GodotScript.ps1 performs deeper validation and also applies a narrow auto-fix:
- It converts runs of 4 leading spaces into tabs, then fails if any leading spaces remain
- It can detect some hidden character problems

Use it as a validator and repair aid, not as the primary gate.
Primary gate is the staged check_tabs hook.

### Editing rules that avoid tool breakage
- Do not paste partial here-strings or incomplete blocks into an interactive PowerShell prompt.
- When writing scripts programmatically, prefer arrays of lines plus WriteAllText with UTF-8 no BOM.
- After changing tooling scripts, run a parse check:
  - $tokens=$null; $errors=$null; [void][System.Management.Automation.Language.Parser]::ParseFile($abs,[ref]$tokens,[ref]$errors); $errors

### Minimal pre-commit workflow
- Edit files
- Stage changes
- git commit (hook runs and blocks on violations)

### 4.4 Environment Resilience and Cache Management (Strict)
- **The Cache Lock:** Avoid rapidly overwriting files with the `class_name` header via terminal scripts. Godot's global cache locks these files. If a fatal namespace collision occurs, remove `class_name` and rely on `extends [Type]` to bypass the cache lock until the feature is stable.
- **Environment Inspection:** Automation scripts MUST NOT call raw environment variables (e.g., `$GodotExe`). Scripts must perform a safe inspection and fall back to a system default to prevent pipeline halts on diverse machines.

### 4.5 Headless CI/CD and File I/O Guardrails (Strict)
- **The Autoload Trap:** Headless syntax validators do not load the `project.godot` Autoload registry. Scripts MUST NOT use global Autoload namespaces directly (e.g., `GameManager.sim`). You must use absolute pathing (e.g., `get_node("/root/GameManager").sim`) to ensure the code passes CI/CD without breaking runtime behavior.
- **Infrastructure-First File I/O:** `Set-Content` cannot create directories. Before generating a new script, the automation pipeline MUST explicitly create the target directory using `New-Item -ItemType Directory -Force`. Failing to do so breaks the deployment chain.

### 4.6 The Hybrid Authority Contract (Strict)
To support both "Starcom-style" flight and "Eve-style" economy:

1.  **The Tactical Bubble:**
    * When Undocked, the **GameShell (Godot)** is the Authority for the Player Ship's physics (Position, Velocity, Rotation, Fuel Burn).
    * The SimCore tracks the player's "Macro Location" (Node ID) but yields micro-control to Godot.

2.  **Entity Injection (The Ghost System):**
    * SimCore entities (Fleets) sharing a Node with the Player are **Injected** into Godot as "Ghosts."
    * Ghosts are fully physical `CharacterBody3D` or `RigidBody3D` objects with local AI.
    * **Result Reporting:** If a Ghost takes damage or is destroyed in Godot, GameShell must emit a `CombatResult` event to SimCore to update the persistent state.

3.  **Passive Renderers (Remote Views):**
    * Views displaying entities *outside* the player's bubble (e.g., the Galaxy Map) must remain **Passive**. They read SimCore state directly and do not simulate physics.

### 4.7 File Writing and Formatting Safety (Strict)
All file writing and formatting safety rules are defined once, canonically, in Section 1.1 (The Atomic Write Pattern). Architecture work must follow that protocol exactly.

If any section below appears to contradict file-writing rules, Section 1.1 is authoritative.

### 4.8 The View-Persistence Contract (Strict)
To prevent visual artifacts (e.g., "flying ships") and crashes during Save/Load:
1.  **Signal-Driven Resets:** `SimBridge` emits `SimLoaded` after a successful state hydration.
    * All View components MUST connect to this signal.
    * On signal, Views must: `ClearVisuals()` (destroy meshes), `ResetState()`, and `Rebuild()` from the new `SimState`.
2.  **Atomic Loading Gate:** `SimBridge` sets `IsLoading = true` during deserialization.
    * All View `_Process` loops MUST check this flag and abort execution if true to prevent accessing invalid memory.
3.  **UI Isolation:** All interactive UI (`Control` nodes) MUST be parented to a `CanvasLayer` (Layer 1+).
    * Placing Controls directly in the 3D scene tree is prohibited as it causes invisible input blocking.

<!-- CONTEXTGEN:END_PART_A -->
## PART B: GAME DESIGN REFERENCE (NOT INCLUDED IN MODULE PACKETS BY DEFAULT)
<!-- CONTEXTGEN:BEGIN_PART_B -->

STOP: The content below is design reference. Do not include it in LLM Module Packets unless the task explicitly requires game design context. For most coding work, Part A is sufficient and is the canonical contract.

## 5. Game Definition (Locked)

### Core fantasy
You are not the hero pilot.
You are the logistics backbone of a civilization at war.

Your influence is indirect. You shape outcomes by moving goods, scaling fleets, stabilizing supply chains, and starving enemies of resources.

Combat exists to protect trade. Trade exists to win the war.

### Genre
Single-player, long-horizon space trading and fleet management game with light combat and a persistent galactic conflict.

Inspirational reference points: Endless Sky, Starcom  
Structural focus: trading-first, fleet-scale decision making.

### Primary loop (Non-negotiable)
1. Scout markets and routes
2. Acquire goods (mining, purchase, salvage)
3. Move goods through contested space
4. Sell or allocate goods to stations and factions
5. Reinvest profits into ships, fleets, and infrastructure
6. Repeat at larger scale under increasing pressure

If a mechanic does not reinforce this loop, it is secondary.

### Progression axes
A. **Economic scale (primary)**
- Capital
- Cargo throughput
- Market leverage
- Access to scarce goods and high-impact routes

B. **Fleet control (secondary, differentiating)**
- Multiple ship roles: traders, miners, escorts, patrols
- Delegated behavior: routes, objectives, risk tolerance
- Loss is possible and meaningful

C. **Technology (tertiary)**
- Unlocks new economic and logistical behaviors
- Military tech is an enabler, not the end goal

### The war (Framing constraint)
The war is a systemic pressure, not a narrative RPG.
- Two sides: human civilization vs an aggressive alien force
- The war advances regardless of player action
- Player influence is expressed through supply, logistics, and economic stabilization
- No chosen one narrative

If the player stops trading, the war continues and goes badly.

### Run structure
- Endless, evolving sandbox
- No hard win or game over state
- Major inflection points and regional outcomes exist
- The universe reacts, but does not conclude

### Pressure and failure
At least two of the following must always apply:
- Escalating external threats (pirates, incursions)
- Capital risk (ship loss, sunk cost, opportunity loss)
- Strategic time pressure (markets and war state evolve)

Fleet management must involve real risk, not just optimization.

### Design invariants
- Trade dominance is the primary source of power
- Logistics beats tactics
- Determinism over spectacle
- Systems over scripts
- Player agency through scale, not micromanagement

### Explicit non-goals
- No realtime multiplayer
- No live-service mechanics
- No narrative-heavy branching RPG
- No crew simulation
- No high-fidelity physics
- No tactical fleet combat micromanagement

## 6. Technology and Strategic Influence (Locked Principles)

### Technology structure
Technology progression is a directed acyclic graph (DAG), not a strict tree.
- Each node may have multiple prerequisites
- Prerequisites may span multiple domains
- Cycles are not allowed
- Unlock logic must be deterministic and testable

Purpose: multiple viable pathways to similar capabilities, reflecting strategic choices rather than linear progression.

### Technology domains
- Trade and logistics (primary)
- Fleet operations
- Mining and extraction
- Industry and manufacturing
- Defense and combat
- Intelligence and espionage

Domains exist to enforce design constraints, not to silo systems.

### Cross-domain governance rules
- A technology node may have at most 2 cross-domain prerequisites
- Any combat-domain tech must require at least 1 prerequisite from:
  - Trade and logistics, or
  - Fleet operations
- Trade and logistics tech may never require combat prerequisites

### Unlock vectors
1. **Economic contribution**
   - Credits accumulated
   - Goods delivered
   - Sustained trade volume
2. **Strategic support**
   - Supplying specific regions or fronts
   - Maintaining logistics under pressure
   - Stabilizing critical systems
3. **Intelligence actions**
   - Espionage investments
   - Information acquisition
   - Long-horizon, probabilistic outcomes

Unlock conditions may combine vectors, but must be expressible as explicit rules.

### Regional and faction specialization
Some technologies are contextually unlocked:
- Certain regions or factions enable specialized tech paths
- Access is earned through sustained logistical or strategic support
- These unlocks reflect capability transfer, not allegiance or narrative choice

Access may be global once unlocked or restricted by context, but must be explicit per node.

### Espionage system boundary
Espionage is a strategic, abstract system:
- Player commits resources
- Time passes
- Outcomes are uncertain but bounded
- Results unlock information, tech access, or economic advantages

Espionage does not involve tactical gameplay, character control, or stealth mechanics.

### Technology effects
Technology unlocks grant capabilities, not narrative rewards.
Examples:
- New ship behaviors
- New fleet-level automation
- Access to new goods or manufacturing
- Improved routing, sensing, or risk mitigation

Technologies should enable new economic behaviors, not just numeric stat boosts.

## 7. Information Flow and Player Knowledge (Locked Principles)

Information is incomplete, delayed, and unevenly distributed.
Perfect knowledge is never assumed. Improving information quality is a core axis of progression.

### Baseline visibility
At any moment, the player has access to:
- Confirmed information about:
  - Their own assets (ships, fleets, cargo, credits)
  - Locations and status of owned fleets
- Partial or delayed information about:
  - Market prices outside visited regions
  - Route safety and threat levels
  - War state beyond nearby fronts

Unvisited or unsupported regions are opaque by default.

### Discovery and first contact
The player ship is the primary tool for initial information acquisition.
- New stations, regions, and factions require physical presence to:
  - Reveal market structure
  - Establish trade access
  - Unlock regional information flow
- Fleets cannot operate autonomously in unknown regions until discovery is complete

### Information delay and uncertainty
Information degrades with distance, time, and lack of infrastructure.
Delays are intentional and legible.
The player should understand:
- What is known
- What is estimated
- What is unknown

### Improving information quality
Information quality improves through:
- Sustained trade and presence in a region
- Intelligence or sensor technologies
- Espionage investments
- Infrastructure (relay stations, hubs, etc.)

Higher quality reduces variance and delay, but never eliminates uncertainty.

### Feedback and causality
Outcomes must be explainable, even when uncertain.
When events occur, the player should be able to trace them to:
- Known risks
- Incomplete information
- Strategic choices

Surprise is acceptable. Confusion is not.

### War state visibility
The galactic war is visible only in aggregate:
- Frontline movement
- Regional instability
- Supply shortages

The player does not see tactical details, only logistical consequences.

### Relationship to automation
Automation does not grant perfect information.
- Automated fleets operate on available data
- Poor information leads to suboptimal execution
- Better intelligence improves automation outcomes

## 8. Work Orders and Economic Flow (Locked Principles)

All non-player-ship activity is expressed through a single abstraction: work orders.
A work order is an ongoing or time-bound directive assigned to a fleet.

### Types of work orders
1. **Route orders (persistent)**
   - Player-defined
   - Long-term trade or supply infrastructure
   - Execute continuously until modified or cancelled
2. **Contract orders (temporary)**
   - Generated by the world based on economic and war conditions
   - Time-bound and situational
   - Shortages, emergencies, opportunities, strategic needs
3. **Special orders (strategic)**
   - Unlocked via technology or intelligence
   - Recon, covert delivery, strategic intervention
   - Often high-risk and high-impact

### Order definition
A work order defines:
- Objective (deliver, mine, patrol, escort, recon)
- Origin and destination or operating region
- Duration (persistent, fixed-term, one-off)
- Priority and urgency
- Risk tolerance
- Expected outcome (explicit or estimated)

Orders are evaluated continuously against available information and conditions.

### Economic flow and world reaction
The world reacts to work orders.
- Sustained routes stabilize regions and markets
- Ignored contracts worsen shortages or instability
- War pressure alters contract urgency and mix
- Player infrastructure influences what opportunities appear

### Player interaction model
- Early: player personally executes work orders via their ship
- Mid: fleets execute orders under player direction
- Late: player orchestrates multiple overlapping orders at scale

## 9. Early Game Contract and Player Authority (Locked Principles)

### Early game contract (First 60 to 90 minutes)
Purpose: establish trust, tension, and legibility.
- Player controls a single ship
- Limited early space
- Markets are fragile, visible, and reactive
- Losses are survivable but instructive

Player must personally:
- Discover locations
- Make first contact
- Execute initial trades and deliveries
- Observe cause and effect between trade actions and world state

### Transition to delegation
Delegation is earned:
- Fleet control unlocks only after core economic actions are executed personally
- Early fleets are limited in scope and autonomy
- Automation quality starts poor and visibly imperfect

### Player authority and intervention
Player may:
- Personally execute or override critical work orders
- Intervene in high-risk or high-impact situations
- Act as discovery and crisis-response asset

Player may not:
- Micromanage routine execution
- Override outcomes after commitments are made
- Eliminate risk through manual control

Authority is situational and strategic.

### Failure semantics
Failure is informative, not punitive:
- Failed routes create shortages, instability, or opportunity elsewhere
- Ignored contracts escalate pressure rather than ending the game
- Lost fleets alter the landscape but do not invalidate progress

<!-- CONTEXTGEN:END_PART_B -->
