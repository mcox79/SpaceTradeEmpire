# T67 Design Research Reference

Compiled from fh_10 audit verification + industry analysis. These benchmarks
informed the T67 "First-Hour Quality Fixes" gate implementations.

---

## 1. Route Grind & Trade Dampening

**Problem**: Route diversity 0.16 (target >0.3), repeat max 45 (target <15).
Players found one profitable route and never left.

**Industry benchmarks**:
- **Elite Dangerous BGS**: ~25% demand decay per trade. Stations fully deplete
  in 4-5 repeated trades. Forces route rotation or commodity switching.
- **X4 Foundations**: Dynamic pricing with mean-reversion. Oversupply crashes
  prices within 3-4 deliveries. Factory consumption creates natural rotation.
- **Exponential decay (general)**: Quadratic/exponential penalty after threshold
  creates a "cliff" that feels fair (first few repeats are fine, then margins
  collapse). Better than linear which feels like constant punishment.

**T67 implementation**: RecentTradeDampenBps 2000->4000, DecayTicks 250->150,
exponential penalty (count^2 scaling) after 5+ repeats. NoveltyBonusBps 3000->5000
for unvisited routes. AbsoluteDampenCapBps 9500 (always leaves 5% floor).

---

## 2. Economy Sinks & Faucet-Sink Balance

**Problem**: sink_faucet ratio 0.042 (target >0.05). Credits accumulated with
near-zero outflow. Exponential growth curve.

**Industry benchmarks**:
- **EVE Online**: 4-6% transaction tax (broker fee + sales tax). Scales with
  skills/standings. Creates meaningful sink without feeling punitive.
- **Factorio tier scaling**: Costs scale 2x-4x per tier. Early items cheap,
  late items expensive. Progression is spending MORE, not hoarding.
- **Elite Dangerous**: Rebuy cost (5% ship value) on death. Docking fees at
  stations. Fuel costs per jump. All feel like "operating costs" not punishment.

**Design philosophy applied**: Per user mandate, NO time pressure on player ship.
Costs must feel like investment. Shuttle upkeep (~50cr/cycle) + docking fees
(15-25cr) create baseline outflow without urgency. Fleet upkeep belongs to empire
phase, not starter ship.

**T67 implementation**: ShuttleUpkeepCr=50, DockingFeeBase=15, DockingFeePerTier=5.
Transaction fee 2% (200 bps) on all trades. Target: sink_faucet >0.05.

---

## 3. FO Dialogue & Silence Breaking

**Problem**: FO never spoke in 100 decisions. Max silence 254-396 decisions across
seeds. Dock greeting absent.

**Industry benchmarks**:
- **Hades**: 20,000+ unique voice lines. Priority queue system ensures variety.
  Characters react to SPECIFIC events, not generic triggers. 70/30 reactive/proactive.
- **Valve Response System (TF2/L4D)**: Hard floor of 15-20 seconds between any
  character silence. System forces ambient barks if nothing contextual fires.
  Priority cascade: event-specific > situation-aware > ambient.
- **Stellaris advisors**: Distinct voice per advisor type. Mix of useful intel
  (prices, threats) and personality. Never goes silent for more than 2 actions.
- **FTL crew**: Terse, contextual. Personality in word choice, not length. Never
  blocks gameplay.

**T67 implementation**: Decision-based silence counter (not just ticks). Hard floor
25 decisions. Forced ambient observation from pool. 15-20 new ambient lines with
70/30 reactive/proactive split. Pre-promotion fallback greeting for first dock.

---

## 4. Combat Loot & Pity Timers

**Problem**: 40% loot rate (4/10 combats). 60% one-shot rate. Bimodal round
distribution (1-round or 16+).

**Industry benchmarks**:
- **FTL**: Guaranteed scrap on every kill (15-30 scrap). Bonus weapon/augment
  drops on top. Player always gets SOMETHING. Scrap is universal currency.
- **Destiny pity timer**: Exotic drop rate increases with each non-drop. After
  ~20 activities without exotic, guaranteed drop. Resets on receipt.
- **Diablo 3 smart loot**: 85% of drops are class-appropriate. Minimum floor of
  materials/gold even on "bad" drops. Salvage system ensures nothing is wasted.
- **Borderlands**: Guaranteed ammo/health from every enemy. Gear drops are the
  variable reward. Base drops keep you playing, gear drops create excitement.

**Design principle**: Every combat encounter must feel like it was worth the risk.
Guaranteed minimum salvage (fuel/ore at GuaranteedScrapQty) ensures the player
never fights for nothing. Pity timer (PityThreshold 5->3) accelerates uncommon
drops for unlucky streaks.

**T67 implementation**: GuaranteedScrapQty=1 (fuel+ore per kill), PityThreshold
5->3, shield grace period (2 rounds absorb all damage), hull damage cap 33%/round,
attrition escalation +25%/round after round 6. MaxCombatRounds=12.

---

## Cross-Cutting Theme: "Investment, Not Punishment"

All four fixes share a design philosophy: the player should feel like they're
making strategic investments, not being punished by systems. Route decay makes
exploration feel rewarding (not repetition punishing). Sinks feel like operating
costs (not taxes). FO silence breaking feels like companionship (not nagging).
Loot floors feel like guaranteed returns on risk (not charity).

This aligns with the reference games: Factorio (pain before relief), Subnautica
(world teaches), Outer Wilds (knowledge gates). The player's time is always
respected.
