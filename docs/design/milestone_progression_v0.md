# Milestone & Progression — Design Spec

> Mechanical specification for player progression tracking, milestone
> achievements, stat accumulation, and the feedback loop that makes
> the player feel their journey matters. Companion to `dynamic_tension_v0.md`
> (pressure philosophy) and `EmpireDashboard.md` (UI surface).

---

## AAA Reference Comparison

| Game | Progression Model | Milestone Design | Feedback Loop | Player Motivation |
|------|-------------------|-----------------|---------------|-------------------|
| **Civilization** | Era progression — research drives era transitions. Each era unlocks units/buildings/wonders. Clear "you are HERE" indicator. | Wonders, Great People, era transitions as milestones. Toast + music cue on era change. | Cumulative — every turn contributes. No wasted actions. | "One more turn." Always progressing toward something. |
| **Stellaris** | Tradition trees + Ascension perks. Fill tradition tree → unlock perk slot. Clear cost/benefit. | First contact, federation founding, crisis survival. Events mark narrative milestones. | Monthly resource → tradition points → tree completion. Slow burn with periodic payoff. | Strategic identity — "I'm a materialist empire" expressed through tradition choices. |
| **Starsector** | Level-based. XP from combat + exploration. Skills unlock ship/fleet bonuses. | No formal milestones — progression is continuous. Colony founding is the de facto milestone. | Combat/exploration → XP → level → skill → capability. | Power fantasy. "I am getting stronger." |
| **Elite Dangerous** | Rank per activity (Combat/Trade/Explorer). Each rank requires cumulative stats. | Rank promotions (Harmless→Elite). Permit unlocks. | Cumulative credits/kills/distance → rank threshold → promotion screen + permit. | Prestige + access. Elite rank is bragging rights + Sol permit. |
| **STE (Ours)** | Stat-based milestones. 6 stat keys tracked, 8 milestones with thresholds. Threshold-based evaluation per tick. | First Trade, Explorer (5 nodes), Merchant (1000 credits), Researcher, Captain, Bulk Trader, Pathfinder, Tycoon. | Cumulative stats → threshold check → milestone achieved → toast notification. | Breadcrumb trail. "I'm making progress." Not power gates — recognition gates. |

### Best Practice Synthesis

1. **Milestones should celebrate, not gate** (Civilization eras) — earning a milestone should feel like a reward, not a prerequisite. Our milestones unlock nothing mechanically (no gating).
2. **Cumulative stats prevent wasted effort** (Elite Dangerous) — every trade, every jump, every mission counts toward something. No action is "wasted."
3. **Multiple progression axes** (Elite Dangerous ranks) — trade, exploration, combat, research as independent progress tracks. A player who only trades still progresses.
4. **Visible progress toward next milestone** (Stellaris traditions) — the player should always see "X/Y toward next milestone." Our dashboard can show this.
5. **Milestone density matters** — too few milestones and the player forgets they exist. Too many and they lose meaning. 8 milestones across 5 stat keys is a good starting density.

---

## Current Implementation

### System

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `MilestoneSystem` | `SimCore/Systems/MilestoneSystem.cs` | Per-tick milestone evaluation | Implemented |
| `MilestoneContentV0` | `SimCore/Content/MilestoneContentV0.cs` | Milestone definitions | Implemented |

### Entity

```
PlayerStats
  NodesVisited: long
  GoodsTraded: long
  TotalCreditsEarned: long
  TechsUnlocked: long
  MissionsCompleted: long
  AchievedMilestoneIds: List<string>
```

---

## Mechanical Specification

### 1. Stat Tracking

Five stat keys are tracked cumulatively across the player's career:

| Stat Key | Source | Incremented By |
|----------|--------|---------------|
| `NodesVisited` | MovementSystem arrival | +1 per unique node first visit |
| `GoodsTraded` | BuyCommand / SellCommand | +quantity per transaction |
| `TotalCreditsEarned` | SellCommand | +revenue per sale |
| `TechsUnlocked` | ResearchSystem completion | +1 per tech researched |
| `MissionsCompleted` | MissionSystem completion | +1 per mission completed |

### 2. Milestone Definitions

| ID | Name | Stat Key | Threshold | Typical Timing |
|----|------|----------|-----------|---------------|
| `first_trade` | First Trade | GoodsTraded | 1 | Tick 10-50 |
| `explorer_5` | Explorer | NodesVisited | 5 | Tick 50-200 |
| `merchant_1000` | Merchant | TotalCreditsEarned | 1,000 | Tick 100-400 |
| `researcher_1` | Researcher | TechsUnlocked | 1 | Tick 200-600 |
| `captain_1` | Captain | MissionsCompleted | 1 | Tick 100-500 |
| `trader_100` | Bulk Trader | GoodsTraded | 100 | Tick 300-800 |
| `explorer_15` | Pathfinder | NodesVisited | 15 | Tick 500-1500 |
| `tycoon_10000` | Tycoon | TotalCreditsEarned | 10,000 | Tick 1000-3000 |

### 3. Evaluation Loop

```
MilestoneSystem.Process(state):
  For each milestone in MilestoneContentV0.All:
    if already achieved: skip
    current = GetStatValue(stats, milestone.StatKey)
    if current >= milestone.Threshold:
      stats.AchievedMilestoneIds.Add(milestone.Id)
```

**Properties**:
- Evaluated every tick (cheap — 8 comparisons)
- One-way: milestones never un-achieve
- Deterministic: sorted by definition order in MilestoneContentV0.All
- No side effects beyond recording the achievement

### 4. Stat Value Resolution

```csharp
GetStatValue(stats, statKey) → statKey switch {
    "NodesVisited"       → stats.NodesVisited,
    "GoodsTraded"        → stats.GoodsTraded,
    "TotalCreditsEarned" → stats.TotalCreditsEarned,
    "TechsUnlocked"      → stats.TechsUnlocked,
    "MissionsCompleted"  → stats.MissionsCompleted,
    _                    → 0
}
```

---

## Progression Arc

### The Breadcrumb Trail

```
Tick 20:    First Trade milestone → toast: "First Trade"
            Player feels: "I did it. The game noticed."

Tick 100:   Explorer milestone (5 nodes) → toast: "Explorer"
            Player feels: "I'm discovering the galaxy."

Tick 300:   Merchant milestone (1000 credits) → toast: "Merchant"
            Player feels: "I'm building wealth."

Tick 500:   Bulk Trader (100 goods) → toast: "Bulk Trader"
            Player feels: "I'm a serious trader now."

Tick 1500:  Pathfinder (15 nodes) → toast: "Pathfinder"
            Player feels: "I've explored deep into the galaxy."

Tick 2500:  Tycoon (10000 credits) → toast: "Tycoon"
            Player feels: "I've mastered the economy."
```

Each milestone is a dopamine hit that validates the player's approach. They are
spaced to provide reinforcement during the pressure phases described in
`dynamic_tension_v0.md`:
- **First Trade** and **Explorer** during the Scramble phase (ticks 0-150)
- **Merchant** and **Captain** during the Establish phase (ticks 150-400)
- **Bulk Trader** and **Researcher** during the Choose phase (ticks 600-1200)
- **Pathfinder** and **Tycoon** during the Commit phase (ticks 1200-2000)

---

## System Interactions

```
PlayerStats
  ← incremented by: BuyCommand, SellCommand, MovementSystem,
     ResearchSystem, MissionSystem
  → read by: MilestoneSystem, Empire Dashboard, First Officer

MilestoneSystem
  ← reads PlayerStats (stat values)
  ← reads MilestoneContentV0 (definitions)
  → writes PlayerStats.AchievedMilestoneIds

Empire Dashboard
  ← reads PlayerStats for display
  ← reads AchievedMilestoneIds for milestone list
  → shows progress toward next milestone in each category

Toast System
  ← reads newly achieved milestones
  → displays celebration toast with milestone name
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Milestone rewards** | HIGH | 1 gate | Achievements are recognition-only. Should grant small credits/rep/cosmetic rewards. MilestoneSystem + content table. |
| **Tiered milestones** | HIGH | 1 gate | Only 1-2 milestones per stat. Need 4-5 tiers (Bronze/Silver/Gold/Platinum/Diamond) per stat. Content expansion + UI tier display. |
| **Combat stats** | MEDIUM | 1 gate | No combat-related milestones (kills, damage dealt, ships destroyed). Add CombatEncounters stat key + 3-4 milestones. |
| **Milestone UI panel** | MEDIUM | 2 gates | No dedicated milestone viewer. Gate 1: bridge query for progress data. Gate 2: GDScript "Progress" tab with progress bars. |
| **Time-based milestones** | LOW | 1 gate | No milestones for "survive N ticks" or "maintain profit for N ticks." Add TicksSurvived stat key + consistency milestones. |
| **Faction-specific milestones** | LOW | 1 gate | No "reach Allied with Valorin" milestones. Add FactionRepEarned stat key + per-faction tracking. |
| **Milestone notifications** | LOW | 1 gate | Current toast is generic. Add milestone-specific celebration (sound cue, FO commentary via MILESTONE_ACHIEVED trigger). |
| **Leaderboard seeds** | FUTURE | 2 gates | Compare milestone timing across seeds. Gate 1: persist milestone tick timestamps. Gate 2: cross-seed comparison UI. |

---

## Constants Reference

```
# Stat Keys
NodesVisited, GoodsTraded, TotalCreditsEarned, TechsUnlocked, MissionsCompleted

# Milestone Thresholds
first_trade:     GoodsTraded        >= 1
explorer_5:      NodesVisited       >= 5
merchant_1000:   TotalCreditsEarned >= 1000
researcher_1:    TechsUnlocked      >= 1
captain_1:       MissionsCompleted  >= 1
trader_100:      GoodsTraded        >= 100
explorer_15:     NodesVisited       >= 15
tycoon_10000:    TotalCreditsEarned >= 10000
```
