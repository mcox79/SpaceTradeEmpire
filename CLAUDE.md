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
