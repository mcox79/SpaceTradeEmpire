# SPACE TRADE EMPIRE: PROJECT CONTEXT

## 1. The Founder Protocol (STRICT AI OUTPUT CONSTRAINTS)

**Voice:** Pragmatic, commercial, technical. Value-oriented and high-signal.

**THE MASTER OUTPUT RULE:** You must NEVER output raw GDScript for the user to copy-paste manually. Manual code insertion is strictly deprecated.

**The Automated Deployment Pattern:** Whenever you generate or modify Godot code (`.gd` files), your output MUST be a single, complete PowerShell code block that executes the following pipeline automatically:
1. **Write the Asset:** Use `Set-Content` with a Here-String (`@"..."@`) to write the new GDScript directly to the correct filepath.
2. **Execute CI/CD Gatekeeper:** Immediately invoke `Validate-GodotScript "path/to/script.gd"` to validate syntax, format tabs, and secure the git commit.
3. **Execute Integration Test:** If the script affects the economy, immediately invoke Godot headless testing: `& $GodotExe --headless -s "scenes/tests/test_economy_core.tscn"`.
4. **Pathing Safety:** You MUST use git rev-parse --show-toplevel within PowerShell to dynamically resolve the project root. Never use relative paths for Set-Content.
5. **Iterative Engineering:** Do not output massive code blocks. Write the structural skeleton first, validate the CI/CD pipeline, and only fill in complex logic in subsequent turns.

**Goal:** The user should only ever have to click "Copy" on a single PowerShell block and paste it into their terminal to deploy, test, and commit your logic.

## 2. Recovery and Toolchain

If the repo gets into a broken state, get back to a clean baseline before doing more work.

Baseline sanity checks (run from repo root):
- git status -sb
- pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\check_tabs.ps1
- cmd.exe /c ".git\hooks\pre-commit.cmd & if errorlevel 1 (echo HOOK_RC=1) else (echo HOOK_RC=0)"

PowerShell path gotcha:
- In some shells, [Environment]::CurrentDirectory can differ from your visible prompt path.
- If you see paths resolving under C:\WINDOWS\system32, fix it with:
  - [Environment]::CurrentDirectory = (Resolve-Path -LiteralPath ".").Path

If _PROJECT_CONTEXT.md or other markdown gets corrupted (unclosed fences, bad paste):
- Restore from git or a known-good backup.
- Prefer editing in your editor, not inside an interactive PowerShell prompt.
- If you must patch via PowerShell, build an array of lines and write with UTF-8 no BOM using:
  - $enc = New-Object System.Text.UTF8Encoding($false)
  - [System.IO.File]::WriteAllText($abs, $content, $enc)

Context dump toolchain:
- DevTool.ps1 provides Run-ContextGen, which writes:
  - _scratch\_FullProjectContext.txt
- When running headless (avoid UI prompts), set:
  - $global:DEVTOOL_HEADLESS=$true

## Canonical files policy

The canonical sources of truth are:
- _PROJECT_CONTEXT.md: workflow rules, guardrails, and contracts
- DevTool.ps1: context dump generator (Run-ContextGen)
- scripts\check_tabs.ps1 and scripts\tools\check_tabs_lib.ps1: staged tabs-only gate for .gd
- scripts\tools\install_hooks.ps1: installs Git pre-commit hooks for Windows

Context dumps:
- Output file: _scratch\_FullProjectContext.txt
- Contract: the tree and file-contents sections must exclude:
  - scratch directories (_scratch/, ._scratch/)
  - addon content (addons/)
  - scripting/tooling and transient files: .ps1, .uid, .bak, .lnk, files named with "- Copy", temp_validator.gd
- Narrative mentions in _PROJECT_CONTEXT.md are allowed; the contract applies to enumerated tree entries and dumped file contents.

If there is any discrepancy between a chat instruction and these canonical files, treat the canonical files as authoritative and update them first.

### 2.3 Git Hygiene and Session Checkpoints (Strict)
- **The "Clean Workbench" Protocol:** Never transition between major architectural Slices or AI chat sessions with a dirty Git working tree. 
- **Milestone Commits:** At the conclusion of a feature vertical, you MUST execute a cleanup script to purge build artifacts (`.uid`, temp files) and seal the state with a distinct milestone commit (e.g., `git commit -m "feat(milestone): complete slice 6..."`).
- **The Rollback Guarantee:** This ensures `HEAD` is always a verified, commercially viable baseline.

## 3. Architecture and Standards (Strict)

### NON-NEGOTIABLE ARCHITECTURE INVARIANTS

#### 1. The Sim Core Data Purity Rule
The headless simulation (`res://scripts/core/sim/`) is the sole authoritative source of truth. To guarantee deterministic replays and network-safe states, it is subject to a strict type blacklist.
* **PROHIBITED in the Sim Core:** Godot `Node`, `Resource`, `AStar3D`, `Vector3`, `RandomNumberGenerator`, and any class inheriting from `RefCounted` that calls engine-specific physics or rendering APIs.
* **ALLOWED in the Sim Core:** Standard GDScript primitives (`int`, `float`, `String`, `Array`, `Dictionary`) and Plain Old Data (POD) structs.

#### 2. Headless Pathfinding Standard
No Godot-native navigation nodes may be used for world logic. Strategic map routing must utilize a custom, array-based Graph Search algorithm (BFS/Dijkstra) running entirely on standard Dictionaries and Arrays.

#### 3. The Golden Replay Blocker
No system is considered "complete" until it passes `test_replay_golden.gd`. This automated test asserts that 10,000 headless simulation ticks using a fixed seed produce the exact same final-state SHA-256 hash across all hardware configurations.

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

### 4.6 The View-Sim Data Contract (Strict)
- **Passive Renderers Only:** View layer scripts (`_process` loops) MUST NOT perform any simulation math or interpolation. They must only read the current state directly from the headless backend (e.g., `visual_node.position = fleet.current_pos`). If a backend data primitive is refactored, the View Layer contract must be updated in the exact same commit.

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