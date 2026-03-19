---
name: optimize
description: "Multi-pass repo optimization scanner. Modes: full (all passes), perf (hot-path allocations), arch (architecture violations), determinism, dead-code, security, gdscript."
argument-hint: "[mode] [--scope path/] [--severity critical|warning|all]"
---

# /optimize — Multi-Pass Repo Optimization Scanner

A comprehensive, LLM-powered code quality and optimization scanner designed for
the Space Trade Empire codebase. Combines automated Grep-based pattern detection
with targeted LLM deep review across multiple passes.

## Parse Arguments

Extract from `$ARGUMENTS`:
- **mode** (first word): `full` | `perf` | `arch` | `determinism` | `dead-code` | `security` | `gdscript` | `consistency` | `alloc-hygiene` | `quick`
  - Default: `quick` (runs passes 1-2 only — fast, high-signal)
  - `full` runs ALL passes (expensive but thorough)
  - Named modes run only that specific pass
- **--scope path/**: Limit scan to a subdirectory (e.g., `--scope SimCore/Systems/`)
  - Default: entire repo (SimCore/ + scripts/)
- **--severity level**: Filter output (default: `warning`)
  - `critical` — only show blockers
  - `warning` — critical + warnings (default)
  - `all` — include suggestions

---

## Architecture Overview

The scanner runs 7 passes. Each pass produces findings in a standard format:

```
[SEVERITY] category | file:line | description | suggested fix
```

Severity: `CRITICAL` > `WARNING` > `SUGGESTION`

Passes are independent and can run in any combination. `full` mode runs all 7.
`quick` mode runs passes 1 + 2 (automated scans + hot-path perf — highest signal/cost ratio).

---

## PASS 1: Automated Pattern Scans (Grep-based, near-zero cost)

Run these Grep scans in parallel. These are mechanical checks with near-zero
false positive rates.

### 1A: Determinism Violations in SimCore/

Grep SimCore/ (excluding Tests) for each pattern. Any match is CRITICAL.

| Pattern | Grep regex | Severity |
|---------|-----------|----------|
| Unseeded Random | `new Random\(\)` | CRITICAL |
| DateTime.Now | `DateTime\.(Now\|UtcNow)` | CRITICAL |
| DateTimeOffset.Now | `DateTimeOffset\.Now` | CRITICAL |
| Guid.NewGuid | `Guid\.NewGuid` | CRITICAL |
| Environment.TickCount | `Environment\.TickCount` | CRITICAL |
| Stopwatch | `Stopwatch\.(StartNew\|GetTimestamp)` | CRITICAL |
| Task.Run | `Task\.Run\(` | CRITICAL |
| Parallel.For | `Parallel\.(For\|ForEach)` | CRITICAL |
| PLINQ | `\.AsParallel\(\)` | CRITICAL |
| async keyword | `\basync\b` in Systems/ | WARNING |
| ConcurrentDictionary | `ConcurrentDictionary` | WARNING |
| Thread.Sleep | `Thread\.(Sleep\|Yield)` | WARNING |
| string.GetHashCode | `\.GetHashCode\(\)` (in Systems/) | WARNING |

### 1B: Architecture Violations

| Check | Grep pattern | Scope | Severity |
|-------|-------------|-------|----------|
| Godot in SimCore | `using Godot` | SimCore/ | CRITICAL |
| sim/sim_ref in GDScript | `\bsim_ref\b\|\bsim\.` | scripts/*.gd (exclude bridge/) | CRITICAL |
| Static mutable state in SimCore | `static\s+((?!readonly\|const).)*=` | SimCore/ (heuristic, verify matches) | WARNING |
| Magic numbers in Systems/ | Deferred to TweakRoutingGuard test | — | — |

### 1C: Security Checks

| Check | Grep pattern | Scope | Severity |
|-------|-------------|-------|----------|
| BinaryFormatter | `BinaryFormatter` | *.cs | CRITICAL |
| Unsafe deserialization | `JsonSerializer\.Deserialize.*typeof` (heuristic) | *.cs | WARNING |
| Hardcoded credentials | `password\s*=\s*"[^"]+"` (case-insensitive) | *.cs, *.gd | WARNING |
| SQL string concat | `"SELECT.*"\s*\+` | *.cs | WARNING |

### 1D: Dead Code Signals

| Check | Grep pattern | Scope | Severity |
|-------|-------------|-------|----------|
| Commented-out code blocks | `//\s*(public\|private\|internal\|protected\|var\|if\|for\|while\|return)` | SimCore/ | SUGGESTION |
| TODO/HACK/FIXME markers | `(TODO\|HACK\|FIXME\|XXX\|TEMP):` | *.cs, *.gd | SUGGESTION |
| Empty catch blocks | `catch\s*(\([^)]*\))?\s*\{\s*\}` (multiline) | *.cs | WARNING |

### 1E: Allocation Hygiene (Grep-based)

Fast mechanical checks for common per-tick allocation patterns. Run in parallel with 1A-1D.

| Check | Grep pattern | Scope | Severity |
|-------|-------------|-------|----------|
| LINQ in Systems | `using System\.Linq` | SimCore/Systems/ | WARNING |
| Copy-constructor List | `new List<.*>\(.*\.(Keys\|Values)\)` | SimCore/Systems/ | WARNING |
| Copy-constructor array | `\.(Keys\|Values)\.ToArray\(\)` | SimCore/Systems/ | WARNING |
| RemoveAll lambda | `\.RemoveAll\(` | SimCore/Systems/ | WARNING |
| String.Split in tick path | `\.Split\(` | SimCore/Systems/ | SUGGESTION |
| String interpolation in tick path | `\$"` | SimCore/Systems/ | SUGGESTION |
| Bare 0 in Systems (TweakRoutingGuard) | `= 0;` or `> 0` without `default(int)` | SimCore/Systems/ | SUGGESTION |
| Missing scratch pattern | Systems with `Process(SimState` but no `ConditionalWeakTable` | SimCore/Systems/ | WARNING |

**LINQ removal progress metric:** Count remaining `using System.Linq` in Systems/ files.
Report as: "LINQ-free: X/Y system files (Z% coverage)". Target: 100%.

**Scratch coverage metric:** Count System files with `Process(SimState` that have
`ConditionalWeakTable`. Report as: "Scratch pattern: X/Y systems (Z% coverage)".

### 1F: Scratch Field Health (Grep-based)

Cross-reference scratch field declarations against usage in the same file:
1. Grep for `public readonly` fields in `private sealed class Scratch` blocks
2. For each field name, Grep for its usage in the same file (outside the declaration)
3. Flag fields with zero usage as SUGGESTION ("dead scratch field")

This catches scratch fields that were added but never wired up, or fields left
behind after refactoring.

### Execution

Run ALL Grep scans in **one parallel message** (up to 20 simultaneous Grep calls).
Collect results. Format as findings table. This is the cheapest pass — always run it.

---

## PASS 2: Hot-Path Performance Analysis (LLM, targeted)

**Goal:** Find per-tick allocations and O(n^2) patterns in the simulation loop.

### Step 2A: Identify the hot path

Read `SimCore/SimEngine/SimKernel.cs` to find the `Step()` method and all
`System.Process()` calls it makes. Build the call tree (1 level deep).

### Step 2B: Scan each System file

For each system file in `SimCore/Systems/` that has a `Process()` method,
dispatch a **Haiku agent** (subagent_type=general-purpose, model=haiku) with this prompt:

```
Review this C# file for performance issues in hot-path code (called every tick).
Flag ANY of these patterns:
1. `new` expressions (heap allocation) — CRITICAL
2. LINQ methods (.Where, .Select, .Any, .Count(), .ToList, .ToArray, .First, .OrderBy) — CRITICAL
3. String concatenation or interpolation ($"..." in tick path) — WARNING
4. Boxing (value type → object) — WARNING
5. Lambda/delegate creation (including .RemoveAll(lambda)) — WARNING
6. params array usage — WARNING
7. ToString() on enums — SUGGESTION
8. Dictionary.ContainsKey + indexer (use TryGetValue) — WARNING
9. Nested loops over collections (O(n^2)) — CRITICAL
10. foreach over Dictionary (allocates enumerator) — SUGGESTION
11. `new List<T>(collection.Keys)` or `.Keys.ToArray()` copy-constructor — CRITICAL
    Fix: use scratch List<T>, .Clear(), foreach add, .Sort()
12. `string.Split('|')` or similar splits in tick path — WARNING
    Fix: cache or restructure data to avoid per-tick parsing
13. Missing scratch pattern: if Process() allocates but file has no ConditionalWeakTable — WARNING
    Fix: add private sealed class Scratch with reusable collections
14. Bare `0` literal in Systems/ (TweakRoutingGuard violation) — WARNING
    Fix: use `default(int)` or `STRUCT_*` const with `STRUCTURAL:` comment

For each finding: quote the exact line, state severity, suggest zero-allocation alternative.
If the method is NOT in the tick hot path (e.g., only called on player action), note that.
Distinguish between "ensure key exists" ContainsKey (ok) vs "read after check" ContainsKey (use TryGetValue).
Return "CLEAN" if no issues found.
```

**Scope control:** If `--scope` is set, only scan files in that path.
If mode is `perf`, only run this pass.

**Batch strategy:** Dispatch up to 5 Haiku agents in parallel, each reviewing
3-4 system files. This keeps cost low (~2K tokens per agent) while parallelizing.

---

## PASS 3: Architecture Deep Review (LLM, targeted)

**Goal:** Verify architectural invariants that Grep can't fully catch.

Dispatch a **Sonnet agent** (or do in main context) to review:

### 3A: Lock Discipline Audit

Read all SimBridge partial files. For each public method, verify:
- Read-only methods use `TryEnterReadLock(0)` pattern
- Mutating methods use `EnterWriteLock()` pattern
- No lock is held across async operations or long computations
- All locks released in finally blocks
- No nested lock acquisition (NoRecursion policy)

### 3B: Bridge Method Contract Audit

Cross-reference GDScript `.call("MethodNameV0")` invocations against actual
SimBridge public methods. Flag mismatches (method renamed/removed but GDScript
not updated).

Steps:
1. Grep all `.call(` patterns in `scripts/**/*.gd`
2. Grep all `public.*V0` method signatures in `scripts/bridge/SimBridge*.cs`
3. Compare lists — flag any GDScript call that has no matching bridge method

### 3C: Layer Violation Deep Scan

Beyond the Grep check in Pass 1, use LLM to detect subtle violations:
- SimCore types leaking into GDScript via intermediate helper classes
- Bridge methods that expose internal SimCore state directly (should return snapshots)
- GDScript files that reconstruct simulation logic instead of querying bridge

---

## PASS 4: Code Consistency Review (LLM, per-module)

**Goal:** Find inconsistencies across similar subsystems.

### 4A: System Consistency

Pick 3-4 representative System files (e.g., TradeSystem, CombatSystem, EscortSystem).
Review for consistent patterns:
- Error handling (do they all guard the same way?)
- Logging conventions
- State access patterns
- Method naming
- Return value conventions

### 4B: Bridge Partial Consistency

Compare 3-4 SimBridge partials for:
- Consistent snapshot construction patterns
- Consistent lock usage
- Consistent null handling
- Consistent V0 method signatures

### 4C: Content File Consistency

Compare 3-4 Content files (e.g., TradeContentV0, CombatContentV0) for:
- Consistent Tweaks referencing pattern
- Consistent struct layout
- Consistent default value handling

Report inconsistencies as SUGGESTION severity with "adopt pattern from [file]" recommendations.

---

## PASS 5: Dead Code & Duplication Detection (LLM + Grep)

### 5A: Unused Public Methods

Strategy:
1. Grep for all `public` method signatures in SimCore/
2. For each method, Grep for its name across the entire codebase
3. If only found at its declaration site → candidate for removal
4. Dispatch Haiku agent to verify candidates aren't used via reflection/serialization

### 5B: Semantic Duplication

Dispatch a Sonnet agent to compare groups of related files:
- All `*System.cs` files — any duplicated logic patterns?
- All `*ContentV0.cs` files — any copy-paste structures?
- All SimBridge partials — any repeated boilerplate that could be extracted?

Report duplicated blocks > 10 lines as WARNING, > 20 lines as CRITICAL.

### 5C: Orphaned Files

1. Glob all `.cs` files in SimCore/
2. Grep for each filename (without extension) across `.csproj`, other `.cs`, and `.gd` files
3. Flag files with zero cross-references as SUGGESTION (may be unused)

---

## PASS 6: GDScript Quality Scan (LLM + Grep)

### 6A: Bridge Call Correctness

(Covered in Pass 3B — skip if already run)

### 6B: GDScript Anti-Patterns

Grep for common issues:
| Pattern | Grep regex | Severity |
|---------|-----------|----------|
| load() in _process | `func _process` → check for `load(` nearby | CRITICAL |
| get_node in _process | `func _process` → check for `get_node(` nearby | WARNING |
| Untyped variables | `var \w+ =` (without `: Type`) | SUGGESTION |
| Signal string typos | `.emit_signal("` patterns vs declared signals | WARNING |

### 6C: Scene File Health

1. Glob all `.tscn` files
2. For any > 200 lines, flag as WARNING (complex scene)
3. Grep for `ext_resource` paths and verify the referenced files exist
4. Flag orphaned scenes (not loaded by any script or other scene)

---

## PASS 7: Security & Dependency Audit

### 7A: NuGet Vulnerability Check

```bash
cd SimCore && dotnet list package --vulnerable 2>/dev/null || echo "SKIPPED"
cd SimCore.Tests && dotnet list package --vulnerable 2>/dev/null || echo "SKIPPED"
```

### 7B: Save File Safety

Read save/load code (QuickSaveV2 handling) and verify:
- No `BinaryFormatter` usage
- JsonSerializer uses safe options (no `TypeNameHandling`)
- No user-controlled type instantiation
- Input bounds checking on deserialized values

### 7C: Godot Export Safety

Check if `.godot/export_presets.cfg` exposes debug flags in release builds.

---

## Report Generation

After all passes complete, generate a consolidated report.

### Report Structure

```markdown
# Optimization Scan Report — [date] [mode]

## Summary
| Severity | Count |
|----------|-------|
| CRITICAL | N |
| WARNING  | N |
| SUGGESTION | N |

## Critical Findings (fix immediately)
| # | Pass | File:Line | Category | Issue | Fix |
|---|------|-----------|----------|-------|-----|
| 1 | ... | ... | ... | ... | ... |

## Warnings (fix before release)
[same table format]

## Suggestions (improve when convenient)
[same table format]

## Pass Results
### Pass 1: Automated Scans — X findings
### Pass 2: Hot-Path Performance — X findings
[etc.]

## Metrics
- Files scanned: N
- Systems reviewed: N
- Bridge partials audited: N
- GDScript files checked: N
- Total scan time: ~Xm
```

### Output

1. **Print the Summary and Critical Findings** to the user immediately
2. **Save full report** to `reports/optimization/scan_[date].md`
3. If `--severity critical` was specified, only show critical findings

---

## Mode Quick Reference

| Mode | Passes Run | Typical Time | Token Cost |
|------|-----------|-------------|------------|
| `quick` | 1, 2 | 2-3 min | ~50K |
| `full` | 1-7 | 10-15 min | ~200K |
| `perf` | 1E + 2 | 2-3 min | ~40K |
| `arch` | 1B + 3 | 3-5 min | ~60K |
| `determinism` | 1A only | 30 sec | ~5K |
| `dead-code` | 1D + 5 | 5-8 min | ~80K |
| `security` | 1C + 7 | 1-2 min | ~20K |
| `gdscript` | 6 | 2-3 min | ~30K |
| `consistency` | 4 | 3-5 min | ~50K |
| `alloc-hygiene` | 1E + 1F | 30 sec | ~5K |

---

## Implementation Notes

### Token Efficiency
- Pass 1 uses Grep exclusively — near-zero LLM token cost
- Pass 2 uses Haiku agents batched 3-4 files each — cheapest LLM option
- Passes 3-5 use Sonnet agents or main context — reserve for high-value analysis
- Never read files > 300 lines in full without a specific reason
- Use Grep to pre-filter before LLM review (e.g., only review System files that actually have LINQ)

### False Positive Management
- Pass 1 patterns are tuned for < 5% false positive rate
- LLM passes should mark confidence: CERTAIN / LIKELY / POSSIBLE
- Static mutable state check (1B) requires manual verification — many matches will be `static readonly`
- Dead code candidates (5A) must be verified — reflection, serialization, and framework callbacks create false positives
- Always note when a finding is in test code vs production code

### Incremental Scanning
- If the user provides `--scope`, respect it for ALL passes
- For repeat scans, compare against previous report in `reports/optimization/` to highlight NEW findings
- If a previous report exists, start the new report with a delta summary

### Integration with Existing Tests
- Do NOT duplicate what TweakRoutingGuard already checks (magic numbers in Systems/)
- Do NOT duplicate what RoadmapConsistency checks (gate alignment)
- Do NOT duplicate what golden hash tests check (determinism correctness)
- Focus on what tests CANNOT catch: performance, architecture quality, code clarity, security

### Known Codebase Patterns to Respect
- `_stateLock` is `ReaderWriterLockSlim(NoRecursion)` — defensive locking is intentional, don't flag as "excessive"
- `TryEnterReadLock(0)` + cache pattern is intentional — don't flag as "ignoring lock failure"
- Private inner classes in SimBridge.cs are intentional (used by save/load)
- `GATE.xxx` comments are traceability markers — don't flag as dead comments
- `DEBUG_*` prints are intentional per project policy — don't flag as debug clutter
- Snapshot methods returning cached values on lock failure is a design choice, not a bug
- `ConditionalWeakTable<SimState, Scratch>` is the canonical zero-allocation pattern — all Systems should use it
- `default(int)` is used instead of bare `0` in Systems/ to satisfy TweakRoutingGuard
- `STRUCT_*` consts with `STRUCTURAL:` comments are structural constants exempt from TweakRoutingGuard
- `.RemoveAll(lambda)` allocates a delegate per call — flag it, but fix is non-trivial (manual loop + reverse removal)
- `ContainsKey` followed by `[key]` is the anti-pattern; `ContainsKey` for "ensure exists then set" is fine
- Stopwatch behind `STE_LOGI_BREAKDOWN` env var guard is intentional profiling — don't flag
- IntentSystem.cs LINQ is acceptable — operates on tiny collections, complex transform, low risk/benefit for rewrite

---

## Example Invocations

```
/optimize                          # quick mode — passes 1+2
/optimize full                     # all 7 passes, full report
/optimize perf --scope SimCore/Systems/   # hot-path scan, Systems only
/optimize determinism              # just determinism grep checks
/optimize arch                     # architecture violations
/optimize dead-code --severity all # dead code with suggestions included
/optimize security                 # security + dependency audit
/optimize gdscript                 # GDScript quality scan
/optimize consistency              # cross-file consistency review
/optimize alloc-hygiene            # allocation + scratch coverage checks (grep-only, fast)
```
