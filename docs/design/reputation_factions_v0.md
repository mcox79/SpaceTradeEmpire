# Reputation & Faction Systems — Design Spec

> Mechanical specification for faction reputation, trade access gating,
> territory regimes, tariff scaling, embargo enforcement, and the diplomatic
> pressure that forces the player to choose sides. Companion to
> `factions_and_lore_v0.md` (faction identities), `warfront_mechanics_v0.md`
> (warfront integration), and `dynamic_tension_v0.md` (Pillar 4: Shrinking
> Middle).

---

## AAA Reference Comparison

| Game | Rep Model | Anti-Max-Rep | Access Gating | Economic Impact |
|------|-----------|-------------|---------------|-----------------|
| **Starsector** | Single axis [-100, +100], 7 tiers. Commission passive decay on rivals. Transponder toggle mediates patrol encounters. Crime tracking per faction. | Commission: +1/month with employer, -1/month with rivals. Permanent consequences for station attacks (-100 instant). No passive forgiveness — recovery requires rare missions. | Inhospitable (-60): market access blocked. Hostile (-100): shoot on sight. Cooperative (+80): blueprints, rare ships, best prices. | Tariff 30% default. Free Port removes. Rep tiers gate access to increasingly profitable equipment. |
| **New Vegas** | Dual-axis: Fame (0-100) + Infamy (0-100) per faction. Infamy is permanent — cannot be undone. Display tier computed from ratio. | Infamy permanence IS the mechanism. Actions that gain fame with one faction typically gain infamy with the other. Mutually exclusive endgames. | Vilified: attacked on sight. Idolized: VIP treatment, unique quests, best vendor prices. Disguise system provides counterplay. | Vendor discounts scale with fame. Vilified locks out entire supply chains. Economy is secondary to narrative. |
| **Stellaris** | Modifier stack. Named modifiers with values and timers. Border friction, ethics opposition, trade deals all visible in tooltip. Trust as separate axis from opinion. | Border friction (neighbors automatically degrade). Ethics opposition (some empires literally cannot like you). Rivalry gives prestige (rewarding hostility). | Opinion gates diplomatic actions: trade deals, migration, federation membership, defensive pact. | Envoy assignments, trade value, diplomatic weight in galactic community voting. Reputation IS political currency. |
| **Mount & Blade** | Dual-axis: Lord-to-Lord relation (personal) + Faction relation (institutional). Lords have personality traits (Mercy/Valor/Honor) that make some choices gain relation with one lord and lose with another. | Zero-sum fief allocation. Giving a fief to Lord A angers Lord B who wanted it. Cannot satisfy all lords simultaneously. Commission forces side-choosing. | Lord relation gates: vassalage offers, marriage, army recruitment, information sharing. | Trade access unaffected by rep in base game. Caravans route based on faction state, not player rep. |
| **Elite Dangerous** | Superpower rank (Federation/Empire/Alliance) + minor faction standing per system. BGS states (Boom/Famine/War) modify rep gain rates. | Time cost: grinding both Federation and Empire rank simultaneously is very slow. Ship unlocks split between superpowers. | Rank gates specific ships (Federal Corvette, Imperial Cutter). Permit-locked systems require specific rank. | Minor faction standing affects mission availability and payout multipliers. |
| **Star Traders: Frontiers** | Contact-first: relationships with named NPCs aggregate to faction standing. Trade permits are layered (Basic → Trade → Military → Diplomatic). | Active decay: 60+ days without contact = -2 rep. Faction wars accelerate gain with one and loss with other. | Permit hierarchy gates goods access. Military permit for military goods. Diplomatic permit for faction agreements. | Commission income. Contract reputation is a loan — failing costs more than not taking. |
| **STE (Ours)** | Single axis [-100, +100], 5 tiers (Enemy/Hostile/Neutral/Friendly/Allied). 1440-tick decay toward zero. Trade gain +1, attack loss -25, war profiteer +2/-1. | Neutrality tax (+5-15% at war zones). War profiteering: helping one side costs the other. Tariff scaling (2× at rep -100). Embargo blocks faction-critical goods. | Enemy (<-75): dock blocked. Hostile (≥-75): trade blocked. Neutral: full access. Friendly (+25): tech purchases. Allied (+75): 15% price discount. | Tariff = baseBps × (100-rep)/100 + war surcharge + neutrality tax. Pentagon ring embargoes. Reputation directly converts to profit margin. |

### Best Practice Synthesis

1. **Modifier transparency** (Stellaris) — the player should see NAMED reasons for their rep, not just a number. "Your standing with Concord: Friendly (42). +15 completed trade contracts, +8 war goods delivery, -5 fracture detection." Our bridge should expose a modifier stack.

2. **Infamy permanence** (New Vegas) — some actions should leave permanent marks. Attacking a faction's station or selling war goods to their enemy should accumulate permanent infamy that fame cannot fully offset. Our current system allows full recovery through trade grinding.

3. **Commission as passive forcing function** (Starsector) — signing a faction commission should give income + passive rep gain, while slowly eroding rival rep. This is the cleanest "shrinking middle" mechanism. Not yet implemented.

4. **Contact maintenance** (Star Traders) — rep should decay if you stop engaging. Our 1440-tick decay toward zero serves this but is uniform. Should be slower for Allied (invested relationships are stickier) and faster for Neutral (casual acquaintances forget quickly).

5. **Reputation as political currency** (Stellaris, Bannerlord) — high rep should let you INFLUENCE faction behavior (supply priorities, warfront stance), not just gate access. This transforms rep from a key to a lever.

6. **Locked-but-visible options** (Mass Effect) — show the player what higher rep would unlock. "Requires Friendly standing with Valorin" on a locked tech creates aspiration. Hidden unlocks create no motivation.

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `ReputationSystem` | `SimCore/Systems/ReputationSystem.cs` | Rep gain/loss, tier computation, natural decay | Implemented |
| `MarketSystem` | `SimCore/Systems/MarketSystem.cs` | Tariff calculation, rep-based pricing, trade access check | Implemented |
| Territory Regime | `SimCore/Systems/MarketSystem.cs` | Territory access regime computation with hysteresis | Implemented |

### Entity

```
SimState.FactionReputation: Dictionary<string, int>
  Key: factionId (string)
  Value: reputation score [-100, +100]
  Default: 0 (Neutral) for unknown factions
```

---

## Mechanical Specification

### 1. Reputation Tiers

```
Allied    ≥ +75    Best prices (-15%), tech purchases, full trust
Friendly  ≥ +25    Good prices (-5%), tech purchases enabled
Neutral   ≥ -25    Standard pricing, full trade access
Hostile   ≥ -75    Trade blocked, +20% surcharge (if somehow trading)
Enemy     < -75    Dock blocked, shoot on sight
```

### 2. Reputation Gain/Loss Sources

| Source | Amount | Trigger | System |
|--------|--------|---------|--------|
| Trade at faction station | +1 | Each completed buy/sell | ReputationSystem |
| War profiteering (buyer) | +2 | Sell war goods at combatant market | ReputationSystem |
| War profiteering (enemy) | -1 | Sell war goods to faction's warfront opponent | ReputationSystem |
| Attack faction ship | -25 | Damage/destroy NPC fleet | ReputationSystem |
| Mission completion | +N | Per mission reward definition | MissionSystem |
| Mission abandonment | -10 | Abandon active mission (ALL factions) | MissionSystem |
| Fracture detection | -10 | Fracture trace exceeds threshold in faction space | FractureSystem |

**War Profiteering Details**: Triggered when selling war-critical goods
(munitions, composites, fuel) at a market controlled by an active warfront
combatant. The buyer faction gains +2, the opposing combatant loses -1.
This creates the "arms dealer" archetype — profitable but reputation-costly.

### 3. Natural Decay

```
Every RepDecayIntervalTicks (1440 ticks ≈ 1 game day):
  For each faction:
    if rep > 0: rep -= RepDecayAmount (1)  // drift toward neutral
    if rep < 0: rep += RepDecayAmount (1)  // drift toward neutral
    // Rep 0 stays at 0
```

**Design Intent**: Decay prevents permanent Allied status from early-game
grinding. The player must actively maintain relationships. At 1 decay per
day, a reputation of +75 (Allied threshold) takes 75 game days to decay
to Neutral — long enough to feel earned, short enough to require maintenance.

### 4. Trade Access Gating

Multiple overlapping checks determine whether trade is possible:

```
CanDock(factionId):
  return reputation >= -75   // Only Enemy blocked

CanTrade(factionId):
  return reputation >= -50   // Hard trade block below -50
  // Note: -50 is between Neutral (-25) and Hostile (-75) thresholds

CanBuyTech(factionId):
  return reputation >= +25   // Requires Friendly tier

IsGoodEmbargoed(marketId, goodId):
  return matching embargo exists  // War-driven, per-good
```

### 5. Reputation-Based Pricing

Per-tier price modifiers applied to all goods at faction markets:

```
GetRepPricingBps(factionId):
  tier = GetRepTier(reputation)
  return tier switch:
    Allied:   -1500 bps  (-15% discount)
    Friendly: -500 bps   (-5% discount)
    Neutral:  0 bps
    Hostile:  +2000 bps  (+20% surcharge)
    Enemy:    (trade blocked, not applied)

AdjustedPrice = basePrice × (10000 + repBps) / 10000
  min 1 credit
```

**Economic Impact**: Allied reputation with a faction that has 20% base tariff
(Valorin) reduces effective tariff to 0% AND gives 15% price discount.
Combined savings: ~35% compared to Neutral. Over 100 trade runs at 1000
credits each: 35,000 credits saved. Reputation IS profit.

### 6. Tariff Calculation (Full Formula)

```
effectiveTariffBps = baseTariffBps × (100 - reputation) / 100
                   + warSurcharge
                   + neutralityTax

Where:
  baseTariffBps = factionTariffRate × 10000

  warSurcharge = WarSurchargeBpsPerIntensity (300) × nodeWarIntensity
    Intensity 1 (Tension): +300 bps
    Intensity 2 (Skirmish): +600 bps
    Intensity 3 (OpenWar): +900 bps
    Intensity 4 (TotalWar): +1200 bps

  neutralityTax (only for Neutral rep in war zones):
    Intensity 2: +500 bps
    Intensity 3: +1000 bps
    Intensity 4: +1500 bps

Reputation scaling: (100 - rep) / 100
  Rep +100 (Allied): 0× base → zero tariff
  Rep 0 (Neutral): 1× base → full tariff
  Rep -100 (Enemy): 2× base → double tariff
```

### 7. Territory Regime System

A 4-level access regime computed from trade policy + reputation tier:

```
TerritoryRegime: { Open, Guarded, Restricted, Hostile }

ComputeRegime(tradePolicy, repTier):
             Allied/Friendly  Neutral    Hostile    Enemy
  Open:      Open             Guarded    Restricted Hostile
  Guarded:   Guarded          Restricted Restricted Hostile
  Closed:    Hostile           Hostile    Hostile    Hostile
```

**Warfront Intensity Override**:
- TotalWar (intensity ≥ 4): non-Allied → regime Hostile
- OpenWar (intensity ≥ 3): minimum regime Restricted

**Hysteresis** (asymmetric transitions):
- Worsening (Open → Hostile): commits **instantly**
- Improvement (Hostile → Open): requires **30+ sustained ticks** at proposed level
- Prevents flickering when reputation hovers near tier boundaries

### 8. Embargo Mechanics

Pentagon ring dependencies are embargoed during wartime (intensity ≥ 2):

```
Concord    embargoes Composites   (needs from Weavers)
Weavers    embargo   Electronics  (needs from Chitin)
Chitin     embargo   Rare Metals  (needs from Valorin)
Valorin    embargo   Exotic Crystals (needs from Communion)
Communion  embargo   Food         (needs from Concord)
Munitions: ALWAYS embargoed in any wartime
```

**Embargo creates smuggling opportunity**: The embargoed good still exists in
the galaxy — it's just blocked at that faction's markets. A player who can
source the good from a third party and deliver it to a black market or
allied faction member profits from the artificial scarcity. This is the
"blockade runner" gameplay arc.

### 9. NPC Patrol Hostility

Patrol fleets check reputation before engaging:

```
IsPatrolHostile(fleet):
  if fleet.Role != Patrol: return false
  rep = GetReputation(fleet.OwnerId)
  return rep < AggroReputationThreshold (-50)
```

- Default rep (0): NOT hostile — patrols are friendly
- Rep -50 to -75 (Hostile tier entry zone): patrols engage
- Rep < -75 (Enemy): all patrols hostile + dock blocked

### 10. Faction Doctrine

Each faction has distinct trade policies and personality:

| Faction | Tariff | Trade Policy | Aggression | Pentagon Need | Pentagon Source |
|---------|--------|-------------|------------|---------------|----------------|
| **Concord** | 5% | Open | Peaceful | Composites | Weavers |
| **Chitin** | 15% | Open | Defensive | Rare Metals | Valorin |
| **Weavers** | 8% | Open | Peaceful | Electronics | Chitin |
| **Valorin** | 20% | Open | Hostile | Exotic Crystals | Communion |
| **Communion** | 3% | Open | Peaceful | Food | Concord |

**Design Intent**: Faction diversity creates distinct trade environments.
Valorin's 20% tariff is a hard barrier for Neutral traders but drops to
zero at Allied. Communion's 3% tariff is nearly free for everyone, creating
a "safe harbor" faction. These rates express faction identity through mechanics.

---

## Player Experience

### The Reputation Arc

```
Tick 0-200: EXPLORATION
  Player visits multiple faction stations. Rep with all factions: 0.
  All tariffs at base rate. No access restrictions.
  "Everyone treats me the same."

Tick 200-600: RELATIONSHIP BUILDING
  Player trades regularly at 2-3 faction stations. Rep climbing +1/trade.
  After 25 trades at Concord: Friendly tier → tech purchases unlocked.
  After 25 trades at Valorin: Friendly tier → tariff drops from 20% to ~5%.
  "I'm building something with these factions."

Tick 600-1200: THE SHRINKING MIDDLE
  War erupts. Neutrality tax at contested nodes (+5-15%).
  War profiteering: selling munitions to Valorin → +2 Valorin, -1 Communion.
  Embargo blocks pentagon goods. Supply chains disrupted.
  "I can't be friends with everyone anymore."

Tick 1200+: COMMITMENT
  Player has Allied with one faction, Hostile with another.
  Allied markets: zero tariff, -15% prices. Hostile markets: blocked.
  The player's trade routes are shaped by their reputation map.
  "I am a Concord captain. This is who I am."
```

### The Pentagon Dilemma

The pentagon dependency ring creates structural faction tension:

```
Concord needs Composites from Weavers
  → If Concord and Weavers go to war: Concord loses Composites supply
  → Concord stations embargo Composites
  → Player who supplies Composites to Concord earns premium + rep

But: Weavers need Electronics from Chitin
  → Helping Concord may weaken Weavers' supply chain
  → Which ripples to Chitin (Electronics demand drops)
  → Which affects Valorin (Rare Metals demand from Chitin drops)

The player who understands the pentagon can predict price cascades
and position themselves ahead of the ripple.
```

---

## System Interactions

```
ReputationSystem
  ← reads FactionReputation (current scores)
  ← reads WarfrontState (war profiteering context)
  → writes FactionReputation (gain/loss)
  → natural decay toward zero

MarketSystem
  ← reads FactionReputation (tariff scaling, price modifier)
  ← reads WarfrontState (war surcharge, neutrality tax)
  ← reads EmbargoState (goods blocking)
  → computes EffectiveTariff, RepPricingBps
  → computes TerritoryRegime with hysteresis
  → gates CanTrade, CanDock, CanBuyTech

WarfrontEvolutionSystem
  ← reads WarSupplyLedger (player deliveries)
  → triggers war profiteering rep adjustments
  → intensity changes affect tariff surcharges

MissionSystem
  → writes FactionReputation (mission completion/abandonment)
  → faction-specific mission rewards tied to rep tiers

FractureSystem
  → writes FactionReputation (detection penalties)

NpcTradeSystem
  ← reads patrol hostility (AggroReputationThreshold)
  → patrol engagement based on player rep

SimBridge.Faction
  → exposes GetFactionDoctrineV0, GetFactionDetailV0, GetAllFactionsV0
  → exposes GetFactionGreetingV0 (tier-specific dialogue)
  → faction color palette for UI/map rendering
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Commission system** | CRITICAL | 2 gates | No faction commission (Starsector model). Gate 1: Commission entity + passive rep gain/rival decay. Gate 2: Monthly stipend + bridge + UI. This is the cleanest "shrinking middle" mechanism. |
| **Reputation modifier stack** | HIGH | 1 gate | Player sees a single number, not named reasons. Need modifier stack (Stellaris pattern): "+15 completed contracts, -5 fracture detection." Bridge query + UI tooltip. |
| **Infamy permanence** | HIGH | 1 gate | All reputation is reversible through trade grinding. Add permanent infamy accumulator (New Vegas model): attacking stations, war profiteering against a faction accumulates infamy that caps max achievable tier. |
| **Locked-but-visible options** | HIGH | 1 gate | Player doesn't see what higher rep would unlock. Add "Requires Friendly" labels on locked tech, "Requires Allied" on locked prices. Mass Effect visible-lock pattern. |
| **Reputation as political currency** | MEDIUM | 2 gates | High rep only gates access, doesn't grant influence. Gate 1: "spend" 10 rep to trigger faction action (redirect supply, pressure warfront). Gate 2: bridge + UI for political actions. |
| **Tiered rep decay** | MEDIUM | 1 gate | Decay is uniform (1/day regardless of tier). Allied relationships should decay slower (invested = stickier). Neutral should decay faster (casual = forgettable). |
| **Disguise/transponder** | LOW | 2 gates | No identity toggle. Gate 1: transponder state (on = legible, off = anonymous but flagged if caught). Gate 2: patrol interaction logic for transponder-off state. Starsector direct adaptation. |
| **Faction greeting depth** | LOW | 1 gate | Per-tier faction greetings exist but are static. Add context-aware greetings referencing player's recent actions (Star Traders contact model). |
| **Cross-faction rep spillover** | FUTURE | 2 gates | No automatic rep changes from faction alliances/rivalries. Gate 1: faction relationship matrix. Gate 2: spillover calculation (helping Concord's ally = +0.5 with Concord). |

---

## Constants Reference

All values in `SimCore/Tweaks/ReputationTweaksV0.cs`, `FactionTweaksV0.cs`:

```
# Reputation Bounds
MinReputation                = -100
MaxReputation                = +100
DefaultReputation            = 0

# Tier Thresholds
AlliedThreshold              = 75
FriendlyThreshold            = 25
NeutralThreshold             = -25
HostileThreshold             = -75

# Gain/Loss
TradeRepGain                 = 1    (per transaction)
AttackRepLoss                = -25  (per fleet engagement)
WarProfiteerBuyerGain        = 2    (selling war goods to combatant)
WarProfiteerEnemyLoss        = -1   (opposing combatant)
AbandonMissionPenalty        = -10  (all factions)
FractureDetectionPenalty     = -10  (controlling faction)

# Decay
RepDecayIntervalTicks        = 1440 (1 game day)
RepDecayAmount               = 1    (toward zero)

# Access
DockBlockedBelowRep          = -75  (Enemy tier)
TradeBlockedBelowRep         = -50  (hard cutoff)
TechRequiresMinRep           = 25   (Friendly tier)
AggroReputationThreshold     = -50  (patrol hostility)

# Pricing
AlliedPricingBps             = -1500 (-15%)
FriendlyPricingBps           = -500  (-5%)
HostilePricingBps            = +2000 (+20%)

# Territory Regime
RegimeHysteresisMinTicks     = 30

# Faction Tariff Rates
Concord                      = 5%   (500 bps)
Chitin                       = 15%  (1500 bps)
Weavers                      = 8%   (800 bps)
Valorin                      = 20%  (2000 bps)
Communion                    = 3%   (300 bps)

# War Surcharge
WarSurchargeBpsPerIntensity  = 300
NeutralityTax (Skirmish)     = 500 bps
NeutralityTax (OpenWar)      = 1000 bps
NeutralityTax (TotalWar)     = 1500 bps
```
