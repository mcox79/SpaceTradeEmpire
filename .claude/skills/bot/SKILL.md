---
name: bot
description: "Run autonomous game bots. Modes: trade (economy), combat (fighting), stress (long-run stability), full (all systems)."
argument-hint: "<mode> [--cycles N]"
---

# /bot — Autonomous Game Logic Bot

Runs the exploration bot headless through SimBridge, validating game logic
(economy, combat, exploration). Parse `$ARGUMENTS` for mode (first word) and
optional `--cycles N`. Default to `trade` if empty.

## Modes

| Mode | What it does | Default cycles | Key flags |
|------|-------------|---------------|-----------|
| `trade` | Autonomous buy/sell/explore | 400 | TRADE_NO_EFFECT, NET_LOSS, NEVER_BOUGHT, STUCK |
| `combat` | Find NPC fleets, engage, verify damage | 200 | NEVER_FOUGHT, DAMAGE_NOT_APPLIED, PLAYER_DIED |
| `stress` | Extended trade run for economy stability | 1500 | PRICE_COLLAPSE, ECONOMY_STALL, CREDIT_PLATEAU |
| `full` | Trade + combat combined | 600 | All flags from both |

---

## Step 1: Parse Arguments

Extract mode from `$ARGUMENTS` (first word). If a `--cycles N` pair is present,
pass it through. Default mode is `trade`.

---

## Step 2: Run the Bot

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Bot.ps1 -Mode <mode> [-Cycles N]
```

The runner:
1. Builds the C# project
2. Launches Godot **headless** with `exploration_bot_v1.gd`
3. Parses `BOT|` prefixed output lines
4. Reports pass/fail verdict

Check exit code: 0 = PASS, 1 = FAIL.

---

## Step 3: Report Results

1. Read `reports/bot/<mode>/stdout.txt` for full bot output if needed.
2. Read `reports/bot/<mode>/report.json` for structured results.
3. Present a summary table:

   | Metric | Value |
   |--------|-------|
   | Mode | trade/combat/stress/full |
   | Cycles | N |
   | Net Profit | credits |
   | Buys/Sells/Travels | counts |
   | Combats/Kills | counts (combat/full only) |
   | Nodes Visited | X/Y |
   | Flags | count |

4. If FAIL: list all CRITICAL flags with their diagnostic paths.
5. If PASS: brief confirmation.

---

## Step 4: Diagnostic Guidance (on FAIL)

For each CRITICAL flag, suggest investigation paths:

| Flag | Where to look |
|------|--------------|
| `TRADE_NO_EFFECT` | SimBridge.Market.cs, BuyCommand.cs/SellCommand.cs |
| `NEVER_BOUGHT` | Market seeding, GetPlayerMarketViewV0 |
| `NEVER_SOLD` | Sell path, cargo state |
| `NEVER_FOUGHT` | GetFleetTransitFactsV0, fleet spawning |
| `DAMAGE_NOT_APPLIED` | CombatSystem.cs, ResolveCombatV0 |
| `PRICE_COLLAPSE` | Economy balance, market tick logic |
| `STUCK_NO_ACTIONS` | Decision loop, market/graph connectivity |

---

## Bot Script Reference

| Mode | Script | Prefix | Output dir |
|------|--------|--------|-----------|
| All | `scripts/tests/exploration_bot_v1.gd` | `BOT\|` | `reports/bot/<mode>/` |

## Troubleshooting

- **No output**: Check stderr for `SCRIPT ERROR` or `Parse Error` (GDScript issue).
- **Timeout**: Bot didn't finish. Increase: `-TimeoutSec 300`.
- **Bridge not found**: C# build may have failed. Check `dotnet build` output.
- **No combats in combat mode**: No hostile fleets spawned. Check fleet generation.
