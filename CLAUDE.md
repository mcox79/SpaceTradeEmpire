# CLAUDE.md — SpaceTradeEmpire repo-level rules

These rules are active in every session. They override default behavior.

---

## File reading discipline

**NEVER read full files when targeted search works.**

| Task | Correct tool | Wrong tool |
|---|---|---|
| Find a gate row in 55_GATES.md | `Grep pattern="GATE.X.Y.001"` | `Read docs/55_GATES.md` |
| Find a session log entry | `Grep pattern="GATE.X.Y.001" path="docs/56_SESSION_LOG.md"` | `Read docs/56_SESSION_LOG.md` |
| Find a C# method | `Grep pattern="MethodName" type="cs"` | Read the whole file |
| Check if a gate is DONE | `Grep pattern="GATE.X.Y.001.*DONE"` | Read 55_GATES.md from top |

Large files and their approximate token cost at full read:
- `docs/55_GATES.md` — ~100 KB → ~25 000 tokens (DO NOT read in full)
- `docs/56_SESSION_LOG.md` — grows unboundedly (DO NOT read in full)
- `docs/54_EPICS.md` — ~50 KB (read only when planning gates)

---

## Token efficiency rules

### Read once, read fully
- If you need multiple sections of a file during a gate, read it once in full upfront (if < 300 lines). Multiple partial reads of the same file are always a net loss.
- Files < 100 lines: always Read directly in main context. Never spin a subagent for a file this small.

### Grep beats subagents for known-location searches
- `Grep` for a known pattern in a known file costs ~50 tokens.
- A Haiku agent doing the same search costs 5 000–20 000 tokens.
- Use Haiku agents only when the search is open-ended (don't know which file) or multi-file exploration. Never for "find X in file Y."

### Subagent sizing guide
| Task | Right tool |
|---|---|
| Find method/pattern in known file | `Grep` |
| Read small file (< 100 lines) | `Read` |
| Search across unknown files | Haiku `Explore` agent |
| Gate closeout (mechanical) | Haiku agent |
| Multi-step implementation writing | Main context (Sonnet) |

### Batch reads before writes
- Do ALL reads in one parallel message before writing anything.
- Pattern: one round-trip of reads → one round-trip of writes → verification.
- Never interleave reads with edits to the same file.
- If you plan to **Edit** a file, include at least one `Read` of that file in the initial batch. `Grep` alone does not satisfy the Edit tool's read precondition.
- The Edit precondition resets between messages. A `Read` from 2+ messages ago is not reliable — read the file in the **same message** as the Edit, or the message immediately before it.

### Headless verification scripts
- Always write temp `.ps1` scripts to `D:\SGE\SpaceTradeEmpire\` — `/tmp` is inaccessible to PowerShell on this machine.
- Use one combined script: run1 → run2 → SHA256 comparison. One `Write` + one `Bash`, not two of each.
- Clean up temp `.ps1` files after use.
- Use `($array) -join ""` for string joining. `Join-String` requires PS 6.2+ and is absent on this machine (Windows PowerShell 5.1).

---

## gates.json encoding invariant

`docs/gates/gates.json` **must contain only ASCII double-quotes**.

- VS Code extensions may silently replace `"` with Unicode curly quotes (U+201C/U+201D).
- This causes `ConvertFrom-Json` to throw in PowerShell, which hangs the
  `RoadmapConsistency_Scan_HardFailOnly` test (its subprocess never exits).
- **After every write to gates.json**, grep for curly quotes to verify:
  ```
  Grep pattern="[\u201C\u201D]" path="docs/gates/gates.json"
  ```
  If any matches appear, rewrite the file with the Write tool immediately.
- Always use the **Write tool** (not Edit) when rewriting gates.json — Edit
  can inherit encoding from the existing file.

---

## Gate closeout process

**Invoke closeout as a Haiku subagent, NOT via the Skill tool.**
The Skill tool runs in main context at Sonnet pricing. Closeout is mechanical — use Agent(haiku).

```python
Agent(
  subagent_type="general-purpose",
  model="haiku",
  description="Gate closeout for GATE.X.Y.Z",
  prompt="""
Close out gate GATE.X.Y.Z (sha256=<value>) for SpaceTradeEmpire at d:/SGE/SpaceTradeEmpire.

STEPS (all reads must use Grep or targeted Read — never full-file reads):

1. PARALLEL:
   a. Grep docs/55_GATES.md for "GATE.X.Y.Z" (content mode) — find quick-ref line number and detail line number
   b. Grep docs/56_SESSION_LOG.md for "GATE.X.Y.Z" — if found, STOP (already logged)
   c. Read docs/gates/gates.json in full
   d. Read docs/56_SESSION_LOG.md offset=150 limit=20 to find the last entry

2. PARALLEL edits:
   a. Append to docs/56_SESSION_LOG.md: "- <TODAY_DATE>, main, GATE.X.Y.Z PASS (<1-line summary>). Evidence: <key files>"
   b. In docs/55_GATES.md quick-ref row: change TODO → DONE
   c. In docs/55_GATES.md detail row: change TODO → DONE

3. Remove the GATE.X.Y.Z task from gates.json tasks array. Update generated_utc to now.
   Write with Write tool (ASCII quotes only — never Unicode curly quotes).

4. Run: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "RoadmapConsistency"
   Report PASS or FAIL.

Hard rules:
- Never read 55_GATES.md or 56_SESSION_LOG.md in full — Grep only
- Write gates.json with Write tool, not Edit (encoding safety)
- Never modify other gates
"""
)
```

Manual closeout checklist (if agent unavailable):
- [ ] Append PASS to `docs/56_SESSION_LOG.md`
- [ ] `docs/55_GATES.md` quick-ref row → DONE
- [ ] `docs/55_GATES.md` detail row → DONE
- [ ] Remove task from `docs/gates/gates.json` (Write tool)
- [ ] `dotnet test ... --filter "RoadmapConsistency"` passes

---

## Test commands

```powershell
# Full SimCore test suite (200 tests)
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q

# Single filter
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "RoadmapConsistency"

# If tests hang — kill stale testhost processes first
Stop-Process -Name testhost, dotnet -Force -ErrorAction SilentlyContinue

# Build game assembly (required before headless Godot runs)
dotnet build "Space Trade Empire.csproj" --nologo
```

Godot binary location: see `docs/57_RUNBOOK.md` → "Godot binary" section.

---

## Godot headless gotchas

- Always `dotnet build` before headless — otherwise C# node scripts degrade to
  base Godot types (no custom signals/methods).
- Call `bridge.call("StopSimV0")` before `quit()` in test scripts — SimBridge
  starts a C# background thread; without this the process hangs.
- C# signal `FooEventHandler` → GDScript name is `"foo"` (snake_case).
- Dual GameManager: autoload at `/root/GameManager`, scene child at
  `/root/Main/GameManager`. SimBridge reads from autoload. Tests must use
  `get_root().get_node_or_null("GameManager")`.
- **`get_tree()` is unavailable in `extends SceneTree` scripts** — `self` IS the
  tree. Use `create_timer()` and `physics_frame` directly. Wrong usage causes a
  silent parse error: process starts, prints the Godot banner, then hangs with
  zero output.
- **Always redirect stderr on the first headless run** — a process that hangs
  with no stdout HSL/HSS/etc. output is almost always a GDScript parse error.
  Check `-RedirectStandardError` output for `SCRIPT ERROR:` lines before
  iterating on logic.
- **`SimCore.Entities` conflicts with `Godot.Node`** — never `using SimCore.Entities`
  in SimBridge.cs. Use fully-qualified `SimCore.Entities.Fleet` etc. instead.

---

## Architecture constraints

- SimCore (C#) is headless and deterministic — zero Godot dependencies.
- GameShell (Godot/GDScript) is presentation only.
- SimBridge.cs (`scripts/bridge/SimBridge.cs`) is the ONLY allowed crossing point.
- All UI reads go through SimBridge query contracts (Facts = snapshots, Events = streams).
- Do NOT add new features via `sim` or `sim_ref` in GDScript — use SimBridge only.
