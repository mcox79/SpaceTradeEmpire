# Copilot Code Review Instructions — SpaceTradeEmpire

## Architecture (hard rules — flag violations as errors)

- **SimCore** (`SimCore/`) is headless and deterministic. It must have ZERO Godot dependencies. No `using Godot`, no `Godot.*` references.
- **SimBridge** (`scripts/bridge/SimBridge.cs`) is the ONLY allowed crossing point between C# sim and GDScript presentation.
- Never `using SimCore.Entities` in SimBridge — it conflicts with `Godot.Node`. Use fully-qualified names.
- GDScript must NOT access sim state directly. All UI reads go through SimBridge query contracts.

## Determinism (flag violations as errors)

- No `DateTime.Now/UtcNow`, `Environment.TickCount`, `Guid.NewGuid()`, or `new Random()` in SimCore.
- All randomness must use `SimState.Rng` (seeded from world seed).
- String collections in SimCore must use `StringComparer.Ordinal` for deterministic iteration.
- Hash-affecting changes (files in `SimCore/Systems/`, `Entities/`, `Gen/`, `World/`) require determinism test updates.

## Threading

- SimCore is single-threaded. No `Task.Run`, `Thread`, or `ThreadPool` in SimCore.
- SimBridge uses `ReaderWriterLockSlim` — always pair Enter/Exit in try/finally.
- UI snapshot reads use `TryEnterReadLock(0)` with cached fallback — never block the game frame.

## Test expectations

- Changes to SimCore tick logic should update golden hashes if signatures change.
- New SimCore systems need corresponding tests in `SimCore.Tests/`.
- Price invariants: buy >= sell, all prices >= 1. Inventory: never negative.
