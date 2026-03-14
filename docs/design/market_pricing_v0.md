# Market Pricing & Trade Economy — Design Spec

> Mechanical specification for inventory-based pricing, tariff layering,
> instability effects, embargo enforcement, and the price formation pipeline
> that makes trading profitable. Companion to `trade_goods_v0.md` (good
> definitions), `npc_industry_v0.md` (supply/demand drivers), and
> `dynamic_tension_v0.md` (economic pressure philosophy).

---

## AAA Reference Comparison

| Game | Price Model | Spread/Margin Design | Anti-Manipulation | Player Experience |
|------|------------|---------------------|-------------------|-------------------|
| **X4 Foundations** | Per-station fill-level pricing. `price = base × (1 + modifier)` where modifier = lerp(-0.5, +0.5, fill_level). Buy and sell prices set independently per station per ware. | ±50% hard band around base price. NPC traders arbitrage differentials constantly. Thin margins on common goods, fat margins on rare. | Hard ±50% cap prevents runaway prices. NPC trader volume smooths spikes. Zero stock = offer disappears (not infinite price). | Price is where the action is. Player builds stations to outproduce NPCs. Price is the scoreboard. |
| **Starsector** | Colony market with accessibility multiplier. `market_price = base × demand_mod × supply_mod × (1/accessibility)`. Low accessibility = 2x prices. | Discrete supply/demand tiers. Surplus → 0.5× floor. Deficit → 2.0× ceiling. Tariff 30% default (Free Port removes). | Accessibility investment as strategic choice. Disruptions are temporary (raids degrade supply tiers, recover over time). | Price breakdown visible in UI — player sees WHY prices are what they are. Transparency drives decisions. |
| **Elite Dangerous** | BGS bracket system. Supply level maps to price bracket (5 tiers). States (Boom/Bust/Famine/War) add ±20-50% modifiers. | Galactic average as fixed reference. Low supply = 130-150% of avg. High supply = 70-90%. State modifiers stack additively. | Weekly BGS tick prevents instant flipping. Galactic average is fixed — cannot inflate a commodity permanently. | State-driven opportunities. Arriving during Famine = massive food price spike. Timing matters. |
| **Eve Online** | Pure player-driven order book. No formula generates prices. NPC seed orders set 10-20% floor and 200-300% ceiling. | Player-determined spread via buy/sell orders. Hub markets have tight spreads. Null-sec has 15-40% premiums from hauling risk. | Transaction tax (0.3-1.5%) + broker fee (1-4%) makes wash trading uneconomical. Volume in hubs prevents cornering. | Market IS the game for traders. Station trading, hauling arbitrage, and manipulation are viable careers. |
| **Offworld Trading Company** | Unit-discrete commodity market. `price = base + (demand_pressure × price_step)`. Each transaction moves price by fixed step. | Discrete price steps create legible manipulation. Every purchase has predictable price impact. | Manipulation IS the gameplay. No prevention — exploitation is the design intent. | Gold standard for market transparency. Player can calculate exact cost to move price to target. |
| **Port Royale / Patrician** | City satisfaction meter. `sell = base × (1 - satisfaction/max × 0.5)`. High satisfaction = cheap. Zero satisfaction = 1.5× base. | 0.5-1.5× band. Satisfaction decays naturally (city consumes goods). Loading a city suppresses price temporarily. | Sell suppression prevents dump-and-buy-back. Satisfaction decay forces route cycling. | Route optimization. Sell to most-deprived cities first, cycle back after satisfaction decays. |
| **STE (Ours)** | Linear scarcity model. `MidPrice = BasePrice + (IdealStock - CurrentStock)`. Shield/hull pricing via bid/ask spread. Published prices on 720-tick cadence. | 10% spread (SpreadBps=1000). Floor at 1 credit. Practical band: goods range ~50-2000 credits depending on scarcity. | NPC demand drain prevents equilibrium. Tariff + transaction fee extract credits. Embargo blocks specific goods. Published price cadence delays information. | Inventory drives everything. Low stock = high price. Player profits from scarcity gradients between nodes. |

### Best Practice Synthesis

1. **Inventory-to-price must be transparent** (Starsector, OTC) — the player should see exactly why a price is what it is. Our bridge exposes base price, tariff, rep modifier, and instability as separate line items.
2. **Hard price bands prevent runaway** (X4 ±50%, Starsector 0.5-2.0×) — without a ceiling, shortages create infinite profit loops. Our practical ceiling is BasePrice + IdealStock (when stock = 0).
3. **Multiple modifier layers create depth without opacity** (Starsector) — base × tariff × rep × instability is rich but each layer is independently legible.
4. **NPC activity prevents market death** (X4, all games) — without constant perturbation, all arbitrage gaps close and trading becomes pointless. Our NpcIndustrySystem demand drain + NPC trader arbitrage serves this.
5. **Transaction costs prevent exploitation** (Eve) — fees and tariffs extract credits per transaction, making wash trading uneconomical and creating natural inflation control.
6. **Published prices create information asymmetry** (real markets) — our 720-tick cadence means published prices lag real prices, creating opportunity for informed players.

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `MarketSystem` | `SimCore/Systems/MarketSystem.cs` | Price computation, tariff calculation, instability pricing, embargo checks | Implemented |
| `Market` (entity) | `SimCore/Entities/Market.cs` | Per-node inventory, price getters, published price cache | Implemented |
| `MarketInitGen` | `SimCore/Gen/MarketInitGen.cs` | Deterministic inventory seeding, starter arbitrage guarantee | Implemented |
| `BuyCommand` | `SimCore/Commands/BuyCommand.cs` | Player purchase execution with credit/inventory validation | Implemented |
| `SellCommand` | `SimCore/Commands/SellCommand.cs` | Player sale execution with revenue calculation | Implemented |

---

## Mechanical Specification

### 1. Core Price Formula

The market uses a **deterministic linear scarcity model** — all integer arithmetic, no floats.

```
MidPrice = BasePrice + (IdealStock - CurrentStock)

BuyPrice  = MidPrice + (Spread / 2)    // Ask: what player pays
SellPrice = MidPrice - (Spread / 2)    // Bid: what player receives

Where:
  Spread = max(MinSpread, round(MidPrice × SpreadBps / 10000))
```

**Constants**:
- `BasePrice` = 100 (default per good, overridable via ContentRegistry)
- `IdealStock` = 50 (equilibrium inventory level)
- `MinSpread` = 2 (absolute minimum bid-ask gap)
- `SpreadBps` = 1000 (10% of mid price)

**Behavior at extremes**:
- Stock = 0: MidPrice = 150 (maximum scarcity premium)
- Stock = 50: MidPrice = 100 (equilibrium — no premium)
- Stock = 100: MidPrice = 50 (abundance discount)
- Stock > IdealStock: MidPrice falls below BasePrice (surplus pricing)

### 2. The Complete Pricing Pipeline

For a **BUY** transaction at a faction market:

```
Step 1: Base Price
  basePrice = ContentRegistry[goodId].BasePrice or default (100)

Step 2: Mid Price (scarcity)
  midPrice = max(1, basePrice + (idealStock - currentStock))

Step 3: Spread
  pctSpread = round(midPrice × 1000 / 10000)
  spread = max(2, pctSpread)
  buyPrice = midPrice + (spread / 2)

Step 4: Reputation Modifier
  repBps = GetRepPricingBps(factionId)
    Allied:   -1500 bps (-15%)
    Friendly: -500 bps  (-5%)
    Neutral:  0 bps
    Hostile:  +2000 bps (+20%)
  pricedWithRep = max(1, buyPrice × (10000 + repBps) / 10000)

Step 5: Instability Modifier
  volatilityBps = instabilityLevel × 5000 / 150
  securitySkew = (phase >= Drift) ? SecurityDemandSkewBps × (phase - 1) : 0
  instabilityMult = (10000 + volatilityBps + securitySkew)
  volatilePrice = pricedWithRep × instabilityMult / 10000

Step 6: Transaction Fee
  effectiveFeeBps = baseFeeBps × feeMultiplier  (0 if Broker unlock)
  fee = ceil(volatilePrice × effectiveFeeBps / 10000)

Step 7: Tariff
  effectiveTariff = computeEffectiveTariffBps(factionId, rep, warIntensity)
  tariff = ceil(volatilePrice × effectiveTariff / 10000)

Step 8: Final Cost
  unitCost = volatilePrice + fee + tariff
  totalCost = unitCost × quantity
```

For a **SELL** transaction: same pipeline, but fees and tariff are **subtracted** from revenue.

### 3. Tariff System

Tariffs are the primary economic lever for faction relationships:

```
effectiveTariffBps = baseTariffBps × (100 - reputation) / 100
                   + warSurcharge
                   + neutralityTax

Where:
  baseTariffBps = factionTariffRate × 10000
  warSurcharge = WarSurchargeBpsPerIntensity (300) × nodeWarIntensity
  neutralityTax = (applied only at Neutral rep in war zones)
    Skirmish:  +500 bps  (+5%)
    OpenWar:   +1000 bps (+10%)
    TotalWar:  +1500 bps (+15%)
```

**Per-Faction Base Tariff Rates**:

| Faction | Rate | Base Bps | At Rep 0 | At Rep +75 | At Rep -50 |
|---------|------|----------|----------|------------|------------|
| Concord | 5% | 500 | 500 bps | 125 bps | 750 bps |
| Chitin | 15% | 1500 | 1500 bps | 375 bps | 2250 bps |
| Weavers | 8% | 800 | 800 bps | 200 bps | 1200 bps |
| Valorin | 20% | 2000 | 2000 bps | 500 bps | 3000 bps |
| Communion | 3% | 300 | 300 bps | 75 bps | 450 bps |

**Reputation Scaling**: Tariff scales as `(100 - rep) / 100`:
- Allied (rep +100): 0× base → zero tariff
- Neutral (rep 0): 1× base → full tariff
- Hostile (rep -100): 2× base → double tariff

### 4. Instability Price Effects

Five instability phases modify pricing at affected nodes:

| Phase | Instability | Price Multiplier | Special Effects |
|-------|-------------|-----------------|-----------------|
| **Stable** | 0-24 | 1.0× | None |
| **Shimmer** | 25-49 | 1.0-1.25× | ±5% price jitter |
| **Drift** | 50-74 | 1.25-1.5× | Fuel/munitions surcharge +2000 bps |
| **Fracture** | 75-99 | 1.5× max | Fuel/munitions surcharge +4000 bps |
| **Void** | 100+ | Market CLOSED | No trading possible |

**Volatility formula** (linear from instability level):
```
volatilityBps = instabilityLevel × 5000 / 150
  Level 0:   0 bps (1.0×)
  Level 75:  2500 bps (1.25×)
  Level 150: 5000 bps (1.5×)
```

### 5. Embargo System

Embargoes block specific goods at faction markets during wartime:

```
IsGoodEmbargoed(state, marketId, goodId):
  for each embargo in state.Embargoes:
    if embargo.EnforcingFactionId == marketControllerFaction
       AND embargo.GoodId == goodId:
      return true  // trade blocked
  return false
```

**Pentagon Ring Embargoes** (activated at warfront intensity ≥ 2):
- Concord ↔ Composites (needs from Weavers)
- Weavers ↔ Electronics (needs from Chitin)
- Chitin ↔ Rare Metals (needs from Valorin)
- Valorin ↔ Exotic Crystals (needs from Communion)
- Communion ↔ Food (needs from Concord)
- **Munitions**: ALWAYS embargoed during any wartime

### 6. Published Price Cadence

Markets publish prices on a **720-tick cadence** (12 game hours):

```
if state.Tick % PublishIntervalTicks == 0:
  for each good in market.Inventory:
    market.PublishedMid[good] = computeMidPrice(good)
    market.PublishedBuy[good] = computeBuyPrice(good)
    market.PublishedSell[good] = computeSellPrice(good)
```

Between cadence ticks, `GetPublishedXyzPrice()` returns the cached value.
Real-time `GetBuyPrice()` / `GetSellPrice()` always compute from current stock.

**Design Intent**: Published prices create **information asymmetry**. A player at Node A sees published prices for Node B (720 ticks stale), but actual prices may have shifted. Players who visit more frequently have better information. NPC traders similarly use published prices for their evaluations, making them slightly suboptimal.

### 7. Transaction Fees

```
baseFeeBps = 100 (1%)

if player has Broker unlock:
  effectiveFeeBps = 0
else:
  effectiveFeeBps = baseFeeBps × feeMultiplier (from tweaks)

fee = ceil((gross × effectiveFeeBps + 9999) / 10000)
  min fee = 1 credit if gross > 0 and bps > 0
```

**Crisis fee increase**: At PressureSystem tier ≥ Critical, fees increase by
`CrisisFeeIncreaseBps` (2000 bps = +20%).

### 8. Inventory Initialization

Markets are seeded deterministically at worldgen:

**Base allocation** (all nodes):
- Fuel: 500 units (bootstrap — ensures early-game mobility)

**Geographic distribution** (hash-based):
- Organics: 40% of nodes at 300 qty
- Rare Metals: 15% of nodes at 150 qty
- Industrial goods distributed via IndustrySite output assignments

**Starter Arbitrage Guarantee** (`MarketInitGen.GuaranteeStarterArbitrageV0`):
```
Ensures ≥50 cr/unit profit between player start and adjacent nodes:
  starterHighStock = 145 → pushes buy price to ~5 credits
  starterLowStock = 10   → pushes sell price to ~80 credits
```

### 9. Trade Access Gating

Multiple layers gate whether trade can occur:

| Check | Threshold | Effect |
|-------|-----------|--------|
| `CanTradeByReputation` | rep ≥ -50 | Below: all trades blocked |
| `CanDock` | rep ≥ -75 | Below: cannot dock at station |
| `IsGoodEmbargoed` | per-good check | Specific goods blocked in wartime |
| Instability Void phase | instability ≥ 100 | Market closed entirely |
| Territory regime Hostile | regime = Hostile | All access denied |

---

## Player Experience

### Price Formation in Practice

```
Tick 0:    All markets near equilibrium (stock ~50, price ~100)
Tick 10:   NPC demand drain fires — 2 units per input consumed
           Stock at production sites drops to 48 → price rises to 102
Tick 50:   War zone markets: munitions stock drains to 20 → price 130
           Adjacent peaceful nodes: munitions stock still 45 → price 105
           Player sees 25 cr/unit spread → profitable trade route
Tick 100:  Player buys 10 munitions at peaceful node: pays 105 × 10 = 1050
           Tariff (Concord 5%): +53 credits
           Fee (1%): +11 credits
           Total cost: 1114 credits
Tick 105:  Player sells at war zone: receives 130 × 10 = 1300
           Tariff (war surcharge +300 bps = 8%): -104
           Fee (1%): -13
           Net revenue: 1183 credits
           Profit: 1183 - 1114 = 69 credits (6.2% margin)
Tick 200:  If player has Allied rep with buyer faction:
           Tariff drops to ~0%, rep discount -15%
           Same trade now yields ~200 credits profit (18% margin)
           Reputation IS profit.
```

### The Five Price Signals

The player reads price through five simultaneous signals:

1. **Scarcity gradient** (stock difference between nodes) → "where to trade"
2. **Tariff differential** (faction rep at each end) → "who to trade with"
3. **War premium** (elevated demand at contested nodes) → "when to trade"
4. **Instability volatility** (unstable nodes = higher prices but higher risk) → "how much risk to take"
5. **Embargo gaps** (blocked goods create secondary markets) → "what to smuggle"

---

## System Interactions

```
MarketSystem
  ← reads Market.Inventory (stock levels → price)
  ← reads FactionReputation (tier → pricing modifier)
  ← reads WarfrontState (intensity → tariff surcharge)
  ← reads InstabilityPhase (level → volatility multiplier)
  ← reads EmbargoState (goods blocking)
  ← reads PressureSystem (crisis → fee increase)
  → computes BuyPrice, SellPrice, MidPrice
  → computes EffectiveTariff
  → publishes prices on 720-tick cadence

BuyCommand / SellCommand
  ← reads MarketSystem prices
  → writes Market.Inventory (stock change)
  → writes PlayerCredits (cost/revenue)
  → writes TransactionRecord (ledger entry)
  → triggers ReputationSystem (trade rep gain)
  → triggers WarProfiteer check (war goods at contested nodes)

NpcIndustrySystem
  → writes Market.Inventory (demand drain, reaction boost)
  → creates the baseline scarcity that makes trading profitable

NpcTradeSystem
  ← reads published prices for opportunity evaluation
  → writes Market.Inventory (NPC buy/sell)
  → creates price convergence between adjacent nodes

IndustrySystem
  → writes Market.Inventory (production output, input consumption)
  → ShortfallEvents when inputs undersupplied

WarfrontDemandSystem
  → writes Market.Inventory (war goods drain at contested nodes)
  → creates wartime price spikes
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Price breakdown UI** | CRITICAL | 1 gate | Player cannot see WHY a price is what it is. Need tooltip showing: base + scarcity + rep modifier + tariff + instability + fee as separate line items. Starsector's modifier stack pattern. |
| **Dump-and-wait exploit** | HIGH | 1 gate | Selling large quantities depresses price, but stock recovers via NPC reaction. Patient players can dump → wait → sell again for infinite credits. Fix: longer recovery timer or permanent satisfaction-like mechanic (Port Royale). |
| **Price history depth** | HIGH | 1 gate | IntelSystem.ProcessPriceHistory records snapshots but no analysis. Need trend indicators (rising/falling/stable) and historical spread visualization. |
| **Commodity-class price bands** | MEDIUM | 1 gate | All goods use same BasePrice (100). Should have per-class bands: essential goods (fuel/food) narrow 0.8-1.2×, industrial medium 0.5-2.0×, luxury/rare wide 0.3-3.0× (Starsector tiered band pattern). |
| **State-driven demand events** | MEDIUM | 2 gates | No BGS-style state modifiers (Boom/Famine/Outbreak). Gate 1: state model on nodes with transition logic. Gate 2: price modifier integration + toast notifications. |
| **Broker tiers** | LOW | 1 gate | Broker unlock is binary (0% or 1% fee). Should have 3 tiers: Basic (0.75%), Advanced (0.5%), Master (0%). |
| **Futures/hedging** | FUTURE | 3 gates | No speculative instruments. Player can only profit from physical arbitrage. Gate 1: futures contract entity. Gate 2: settlement logic. Gate 3: UI for contract management. OTC pattern. |
| **Player-set sell orders** | FUTURE | 2 gates | Player cannot leave sell orders at a station and leave. Must be present for every transaction. Gate 1: standing order entity + execution. Gate 2: bridge + dock UI. |

---

## Constants Reference

All values in `SimCore/Tweaks/MarketTweaksV0.cs`:

```
# Core Pricing
BasePrice                    = 100
IdealStock                   = 50
MinSpread                    = 2
SpreadBps                    = 1000  (10%)

# Transaction Fees
TransactionFeeBps            = 100   (1%)
CrisisFeeIncreaseBps         = 2000  (+20% at crisis)

# Published Prices
PublishIntervalTicks         = 720   (12 game hours)

# Instability
VolatilityMaxBps             = 5000  (max +50% at level 150)
ShimmerPriceJitterPct        = 5     (±5%)
SecurityDemandSkewBps        = 2000  (per phase above Shimmer)

# Starter Arbitrage
StarterHighStock             = 145
StarterLowStock              = 10
MinArbitrageMargin           = 50 credits/unit

# Tariff
WarSurchargeBpsPerIntensity  = 300
NeutralityTax_Skirmish       = 500 bps
NeutralityTax_OpenWar        = 1000 bps
NeutralityTax_TotalWar       = 1500 bps
```
