# SPACE TRADE EMPIRE: PROJECT CONTEXT

## 1. The Founder Protocol

- **Voice:** Pragmatic, commercial, technical
- **Output pattern:** Direct code blocks → implementation steps → verification
- **Code blocks:** Fence by language. PowerShell blocks contain only PowerShell commands. GDScript blocks are fenced as `gdscript` (or plain) and must never be presented as terminal commands.
- **Meta-rule:** If the project structure changes significantly (new systems), update this file

## 2. Recovery and Toolchain

### DevTools Recovery Block
If `DevTool.ps1` is lost, recreate a script that:
1. Concatenates all `.gd` and `.tscn` (and other first-party text assets) into `_FullProjectContext.txt`
2. Wraps `git add/commit` and `git reset --hard` into GUI buttons

## Canonical files policy
- `_PROJECT_CONTEXT.md` is canonical and must only be edited by hand.
- Dev tooling must never overwrite canonical docs.
- Generated artifacts (context dumps, inventories, transcripts) must be written under `_scratch/`.
- If a tool proposes changes to canonical docs, it must output a patch or diff, not overwrite.

## 3. Architecture and Standards (Strict)

### File organization
- `/scenes`: visuals and prefabs
- `/scripts`: logic
- `/assets`: raw imports (models, sounds)

### Naming
- Nodes: PascalCase (example: `ScoreLabel`)
- Scripts and files: snake_case (example: `game_manager.gd`)

### Design patterns
- **Signal bus:** Use `GameManager` (Autoload) to pass data between unrelated objects (example: Asteroid → UI)
- **Composition:** Avoid deep inheritance. Prefer child nodes to add features

### Camera rule
- **The drone camera** must be top-level or code-spawned
- It must never be parented to Player

## 4. GDScript Editing and Indentation Policy (Strict)

This project enforces a tabs-only indentation policy for all `.gd` files. Violations are treated as build-breaking errors.

### Rationale
GDScript is indentation-sensitive. Once a file establishes tab-based indentation, introducing spaces (or mixing tabs and spaces) causes parse errors that are hard to diagnose after the fact. Automated edits are especially prone to this failure.

### Indentation rules
1. **Tabs only for leading indentation**
   - All leading indentation in `.gd` files must be literal tab characters
   - Spaces are forbidden for indentation
   - Mixed tabs and spaces in leading whitespace are forbidden
2. **Spaces allowed only after code starts**
   - Spaces may be used after the first non-whitespace character on a line
   - Spaces must never appear before the first non-whitespace character

### Tab policy enforcement workflow

This repo treats leading-space indentation in `.gd` files as build-breaking.

#### Required workflow
1. Before opening Godot, run the indentation gate.
2. After any automated edit (PowerShell patch, search/replace, generated code), run the indentation gate again.
3. If the gate fails, normalize indentation, then re-run the gate. Do not launch Godot until clean.

#### Canonical scripts (PowerShell)

Create these scripts in the repo root (or `scripts/` if you prefer). They are the only sanctioned way to normalize and validate indentation.

##### `Normalize-GDScriptIndent.ps1`
- Converts leading indentation from 4-space groups to literal tabs **only when the leading whitespace is spaces-only and divisible by 4**.
- **Fails** (exit 1) if it finds mixed tabs+spaces or leading spaces not divisible by 4. This forces manual cleanup so the repo converges to tabs-only.
- Does not change spaces after the first non-whitespace character.

```powershell
$bad = @()
$files = Get-ChildItem -Path . -Recurse -File -Filter *.gd | Where-Object {
	$_.FullName -notmatch '\addons\' -and
	$_.FullName -notmatch '\.godot\' -and
	$_.FullName -notmatch '\.git\'
}

foreach ($f in $files) {
	$path  = $f.FullName
	$lines = Get-Content $path
	$fixed = @()
	for ($i = 0; $i -lt $lines.Count; $i++) {
		$line = $lines[$i]
		if ($line -match '^(?<ws>[\t ]+)(?=\S)') {
			$ws = $Matches.ws
			$hasTab   = $ws -match "\t"
			$hasSpace = $ws -match " "
			if ($hasTab -and $hasSpace) {
				$bad += "{0}:{1}: Mixed tabs+spaces in leading whitespace" -f $path, ($i + 1)
				$fixed += $line
				continue
			}
			if ($hasSpace) {
				$spaces = ($ws -replace "\t", "").Length
				if (($spaces % 4) -ne 0) {
					$bad += "{0}:{1}: Leading spaces not divisible by 4" -f $path, ($i + 1)
					$fixed += $line
					continue
				}
				$tabs = [int]($spaces / 4)
				$fixed += ("`t" * $tabs) + $line.Substring($ws.Length)
				continue
			}
		}
		$fixed += $line
	}
	Set-Content -Path $path -Value $fixed -Encoding UTF8
}

if ($bad.Count -gt 0) {
	"FAIL: Normalize found indentation that cannot be safely auto-fixed:"
	$bad
	exit 1
}
"OK: Normalize completed with no unsafe indentation found."
exit 0
```

##### `Check-GDScriptIndent.ps1`
- Fails if any non-empty `.gd` line has **any space** in the leading whitespace before code (tabs-only indentation).

```powershell
$bad = @()
$files = Get-ChildItem -Path . -Recurse -File -Filter *.gd | Where-Object {
	$_.FullName -notmatch '\addons\' -and
	$_.FullName -notmatch '\.godot\' -and
	$_.FullName -notmatch '\.git\'
}

foreach ($f in $files) {
	Select-String -Path $f.FullName -Pattern '^[\t ]* [\t ]*\S' | ForEach-Object {
		$bad += "{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line
	}
}

if ($bad.Count -gt 0) {
	"FAIL: Found spaces in leading indentation of .gd files:"
	$bad
	exit 1
}
"OK: Tabs-only indentation verified."
exit 0
```

#### How to use
1. One-time cleanup (or after a large merge):
   - Run `Normalize-GDScriptIndent.ps1`
   - Run `Check-GDScriptIndent.ps1` and confirm it passes
2. Day-to-day:
   - Run `Check-GDScriptIndent.ps1` before launching Godot
   - Run `Check-GDScriptIndent.ps1` after any scripted edit

### Editing rules (PowerShell and automation)
1. **No in-place line edits inside functions**
   - Automated edits must replace entire function blocks, not partial snippets
   - Patching individual lines inside an indented block is prohibited
2. **All generated GDScript must emit tabs explicitly**
   - PowerShell scripts must emit literal tab characters (`\t`) for indentation
   - When using double-quoted here-strings, indentation must use `` `t ``
   - Single-quoted here-strings are discouraged for `.gd` output
3. **Bounded replacements only**
   - Replacements must be bounded by:
     - `func <name>(...)` → next `func` or end-of-file
   - Blind global replacements inside `.gd` files are forbidden

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
