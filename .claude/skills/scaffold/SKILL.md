---
name: scaffold
description: >
  Generate boilerplate files for common gate patterns (SimCore command, system,
  SimBridge query, GDScript UI, headless test). Fills in structure; developer
  adds business logic.
argument-hint: "<pattern-type> <Name>"
---

# Scaffold Skill

Generates boilerplate files for common Space Trade Empire gate patterns.
After scaffolding, the developer fills in business logic at `// TODO` markers.

**This skill does NOT run builds or tests** — that is the developer's job.

Parse `$ARGUMENTS` for: `<pattern-type> <Name>`

Supported pattern types:
- `simcore-command <Name>` (e.g., `simcore-command FleetRepair`)
- `simcore-system <Name>` (e.g., `simcore-system MaintenanceSystem`)
- `simbridge-query <Name>` (e.g., `simbridge-query GetRepairSnapshot`)
- `gdscript-ui <name>` (e.g., `gdscript-ui repair_panel`)
- `headless-test <name>` (e.g., `headless-test test_repair`)

If the pattern type is not recognized, show the list above and stop.

---

## Pattern: `simcore-command <Name>`

**hash_affecting: true**

### 1. Create `SimCore/Commands/<Name>Command.cs`

```csharp
namespace SimCore.Commands;

public sealed class <Name>Command : ICommand
{
    public string FleetId { get; }

    public <Name>Command(string fleetId)
    {
        FleetId = fleetId;
    }

    public void Execute(SimState state)
    {
        // TODO: implement command logic
    }
}
```

### 2. Add dispatch method to SimBridge

Find the appropriate SimBridge partial file in `scripts/bridge/`. Choose based
on domain (Fleet, Market, Combat, etc.). If unsure, use `SimBridge.cs` (core).

Add:

```csharp
public void Dispatch<Name>V0(string fleetId)
{
    EnqueueCommand(new <Name>Command(fleetId));
}
```

### 3. Create test stub `SimCore.Tests/Commands/<Name>CommandTests.cs`

```csharp
using SimCore.Commands;
using Xunit;

namespace SimCore.Tests.Commands;

public sealed class <Name>CommandTests
{
    [Fact]
    public void <Name>Command_BasicExecution_Succeeds()
    {
        // TODO: arrange state, execute command, assert results
        Assert.True(true, "Scaffold placeholder — replace with real test");
    }
}
```

### 4. Report

Print:
```
Scaffolded simcore-command: <Name>
  Created: SimCore/Commands/<Name>Command.cs
  Modified: scripts/bridge/SimBridge.<Partial>.cs (added Dispatch<Name>V0)
  Created: SimCore.Tests/Commands/<Name>CommandTests.cs

Next steps:
  1. Fill in Execute() logic in <Name>Command.cs
  2. Fill in test assertions in <Name>CommandTests.cs
  3. dotnet build SimCore/SimCore.csproj --nologo -v q
  4. dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "<Name>Command"
```

---

## Pattern: `simcore-system <Name>`

**hash_affecting: true**

### 1. Create `SimCore/Systems/<Name>.cs`

```csharp
namespace SimCore.Systems;

public static class <Name>
{
    public static void Advance(SimState state)
    {
        // TODO: implement per-tick logic
    }
}
```

### 2. Wire into SimEngine.cs

Find the tick loop in `SimCore/SimEngine.cs` and add `<Name>.Advance(state);`
at the appropriate position. Read the file first to understand ordering.

### 3. Create test stub `SimCore.Tests/Systems/<Name>Tests.cs`

```csharp
using SimCore.Systems;
using Xunit;

namespace SimCore.Tests.Systems;

public sealed class <Name>Tests
{
    [Fact]
    public void <Name>_Advance_BasicBehavior()
    {
        // TODO: arrange state, call Advance, assert results
        Assert.True(true, "Scaffold placeholder — replace with real test");
    }
}
```

### 4. Report

Print:
```
Scaffolded simcore-system: <Name>
  Created: SimCore/Systems/<Name>.cs
  Modified: SimCore/SimEngine.cs (added Advance call)
  Created: SimCore.Tests/Systems/<Name>Tests.cs

  WARNING: Golden hash will change. After implementing, run:
  dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"
  Update expected hashes if the change is intentional.

Next steps:
  1. Fill in Advance() logic in <Name>.cs
  2. Fill in test assertions in <Name>Tests.cs
  3. dotnet build SimCore/SimCore.csproj --nologo -v q
  4. dotnet test --filter "<Name>Tests"
  5. Update golden hashes
```

---

## Pattern: `simbridge-query <Name>`

**hash_affecting: false**

### 1. Add query method to appropriate SimBridge partial

Choose the right partial file based on domain. Read the partial first to
understand existing patterns. Add:

```csharp
public Godot.Collections.Dictionary <Name>V0(string id)
{
    var dict = new Godot.Collections.Dictionary();
    TryExecuteSafeRead(state =>
    {
        // TODO: read from state, populate dict
        dict["placeholder"] = "TODO";
    });
    return dict;
}
```

### 2. Report

Print:
```
Scaffolded simbridge-query: <Name>
  Modified: scripts/bridge/SimBridge.<Partial>.cs (added <Name>V0)

Next steps:
  1. Fill in TryExecuteSafeRead body with actual state reads
  2. dotnet build "Space Trade Empire.csproj" --nologo
  3. Call from GDScript: bridge.call("<Name>V0", args)
```

---

## Pattern: `gdscript-ui <name>`

**hash_affecting: false**

### 1. Create `scripts/ui/<name>.gd`

```gdscript
extends Control

var _bridge = null

func _ready():
    var gm = get_node_or_null("/root/GameManager")
    if gm:
        _bridge = gm.get("bridge")
    _refresh()

func _refresh():
    if not _bridge:
        return
    # TODO: call bridge snapshot and update UI
    # var snap = _bridge.call("GetFooSnapshotV0", args)

```

### 2. Report

Print:
```
Scaffolded gdscript-ui: <name>
  Created: scripts/ui/<name>.gd

Next steps:
  1. Fill in _refresh() with bridge.call() and UI updates
  2. Add to a scene (.tscn) as a child node
  3. dotnet build "Space Trade Empire.csproj" --nologo
  4. Test in-engine or via headless script
```

---

## Pattern: `headless-test <name>`

**hash_affecting: false**

### 1. Create `scripts/tests/<name>.gd`

```gdscript
extends SceneTree

func _init():
    var scene = load("res://scenes/playable_prototype.tscn").instantiate()
    root.add_child(scene)
    await create_timer(0.5).timeout

    var gm = root.get_node_or_null("GameManager")
    if not gm:
        print("HSS: FAIL — GameManager not found")
        quit()
        return

    var bridge = gm.get("bridge")
    if not bridge:
        print("HSS: FAIL — bridge not found")
        quit()
        return

    # TODO: add test logic here
    print("HSS: PASS — <name>")

    bridge.call("StopSimV0")
    quit()
```

### 2. Report

Print:
```
Scaffolded headless-test: <name>
  Created: scripts/tests/<name>.gd

Next steps:
  1. Fill in test logic after bridge initialization
  2. dotnet build "Space Trade Empire.csproj" --nologo
  3. Run: godot --headless --path . -s res://scripts/tests/<name>.gd
  4. Redirect stderr on first run to catch parse errors
```

---

## Rules

- NEVER overwrite an existing file. If the target file already exists, STOP and
  tell the user.
- Use `Write` tool for new files, `Edit` tool for adding to existing files.
- Replace `<Name>` with PascalCase and `<name>` with snake_case as appropriate.
- Do not add `using` statements that aren't needed by the scaffold.
- Do not run `dotnet build` or `dotnet test` — just create/modify files and report.
