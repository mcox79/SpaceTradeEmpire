# Production Chain Phase 2 Implementation — Detailed Guide

**Target**: Deploy all 6 remaining production recipes as industry sites
**Scope**: MarketInitGen.cs modifications only (no new systems required)
**Estimated token cost**: ~800 tokens (5 copy-paste patterns, 1 minor geographic logic addition)
**Risk**: Low (existing pattern is proven; recipes already defined; no cross-system dependencies)

---

## The Pattern (Already Proven in Code)

The three existing industry site deployments follow this pattern in MarketInitGen.InitMarkets():

```csharp
// PATTERN: Deploy site at nodes matching a modulus condition
if (i % MODULUS == OFFSET)
{
    state.IndustrySites[$"<sitetype>_{i}"] = new IndustrySite
    {
        Id = $"<sitetype>_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.<RecipeName>,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.<GoodId>, <Qty> }
            // ... additional inputs
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.<OutputGood>, <Qty> } },
        BufferDays = <Int>,
        DegradePerDayBps = <Int>
    };
}
```

Existing examples:
- **ExtractOre**: `i % 2 == 0` (50% of nodes)
- **RefineMetal**: `i % 2 == 1` (50% of nodes)
- **ManufactureMunitions**: `i % CatalogTweaksV0.MunitionsNodeModulus == CatalogTweaksV0.MunitionsNodeOffset` (~14%)

To add the 6 remaining recipes, replicate this pattern with different modulus/offset pairs.

---

## Recipe 1: ProcessFood (Organics + Fuel → Food)

### Placement Logic
- **When**: At agri nodes (where Organics were seeded)
- **Where**: Odd nodes only (to avoid overlap with mines on even nodes)
- **Why**: Agri nodes have Organics; odd nodes are the opposite parity of mines

### Code to Add
Insert this in MarketInitGen.InitMarkets() after RefineMetal deployment (~line 183):

```csharp
// GATE.S7.PRODUCTION.FULL_DEPLOY.001: ProcessFood at agri nodes (odd parity to avoid mine overlap)
bool hasOrganics = geoHash < CatalogTweaksV0.OrganicsNodePct;
if (hasOrganics && i % 2 == 1)
{
    state.IndustrySites[$"foodproc_{i}"] = new IndustrySite
    {
        Id = $"foodproc_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.ProcessFood,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.Organics, CatalogTweaksV0.FoodProcessorOrganicsInput },
            { WellKnownGoodIds.Fuel, CatalogTweaksV0.FoodProcessorFuelInput }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Food, CatalogTweaksV0.FoodProcessorFoodOutput } },
        BufferDays = 2,
        DegradePerDayBps = CatalogTweaksV0.FoodProcessorDegradeBps
    };
    node.Name += " (Food Processor)";
}
```

### Tweaks to Add to CatalogTweaksV0.cs
```csharp
public const int FoodProcessorOrganicsInput = 2;
public const int FoodProcessorFuelInput = 1;
public const int FoodProcessorFoodOutput = 3;
public const int FoodProcessorDegradeBps = 500; // 5% per day at full undersupply
```

### Reasoning
- **Inputs**: Matches recipe definition (2 Organics, 1 Fuel)
- **Outputs**: Matches recipe definition (3 Food)
- **BufferDays**: 2 days (Food is perishable; keep smaller buffer to encourage turnover)
- **DegradePerDayBps**: 500 bps (same as metal refineries; balanced decay)
- **Parity**: Odd nodes only prevents conflicts with mines (which are even nodes)

---

## Recipe 2: FabricateComposites (Metal + Organics → Composites)

### Placement Logic
- **When**: At nodes with both Ore (mining) AND Organics (agri) access
- **Where**: Distributed via modulus to avoid concentration
- **Why**: Composites require industrial + agri infrastructure; can be imported but colocating reduces transport costs

### Code to Add
Insert after ProcessFood deployment (~line 200):

```csharp
// GATE.S7.PRODUCTION.FULL_DEPLOY.001: FabricateComposites at industrial hubs (distributed)
if (i % CatalogTweaksV0.CompositesNodeModulus == CatalogTweaksV0.CompositesNodeOffset)
{
    state.IndustrySites[$"compfab_{i}"] = new IndustrySite
    {
        Id = $"compfab_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.FabricateComposites,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.Metal, CatalogTweaksV0.CompositesMetalInput },
            { WellKnownGoodIds.Organics, CatalogTweaksV0.CompositesOrganicsInput }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Composites, CatalogTweaksV0.CompositesOutput } },
        BufferDays = 2,
        DegradePerDayBps = CatalogTweaksV0.CompositesDegradeBps
    };
    node.Name += " (Composites Fabrication)";
}
```

### Tweaks to Add to CatalogTweaksV0.cs
```csharp
public const int CompositesNodeModulus = 12;
public const int CompositesNodeOffset = 3; // nodes 3, 15, 27, 39, ... => ~8% deployment
public const int CompositesMetalInput = 3;
public const int CompositesOrganicsInput = 2;
public const int CompositesOutput = 2;
public const int CompositesDegradeBps = 500; // 5% per day
```

### Reasoning
- **Modulus/Offset**: `12 % 3` deploys at nodes 3, 15, 27, 39, ... (every 12th node, offset 3) → ~8% of galaxy
- **Inputs**: Matches recipe (3 Metal, 2 Organics)
- **Outputs**: Matches recipe (2 Composites)
- **BufferDays/DegradePerDayBps**: Same as food (strategic material, should be kept in circulation)

Note: No parity constraint (unlike ProcessFood). Composites need both Ore and Organics, which can be at any node type. The modulus distribution spreads factories geographically to create multi-hop trade routes.

---

## Recipe 3: AssembleElectronics (Exotic Crystals + Fuel → Electronics)

### Placement Logic
- **When**: ONLY at fracture-border nodes (geographic scarcity driver)
- **Where**: Rare node type (only ~5% of galaxy)
- **Why**: Electronics must be supply-constrained by Exotic Crystals (fracture-only). If Electronics were common, the pentagon ring's economic engineering collapses. See trade_goods_v0.md "Electronics Chain (Intentional Design)".

### Code to Add
Insert after FabricateComposites (~line 220):

```csharp
// GATE.S7.PRODUCTION.FULL_DEPLOY.001: AssembleElectronics at fracture-border nodes ONLY
// This is the geographic constraint that makes Electronics valuable (supply-limited by Exotic Crystals).
// Deployment: Every (1 + rare_metals_node_indicator) node => ~5% of galaxy (overlap with rare metal detection).
if (geoHash >= (100 - CatalogTweaksV0.RareMetalsNodePct)) // rare metal deposit marker
{
    state.IndustrySites[$"elec_{i}"] = new IndustrySite
    {
        Id = $"elec_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.AssembleElectronics,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.ExoticCrystals, CatalogTweaksV0.ElectronicsExoticCrystalsInput },
            { WellKnownGoodIds.Fuel, CatalogTweaksV0.ElectronicsFuelInput }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Electronics, CatalogTweaksV0.ElectronicsOutput } },
        BufferDays = 1,
        DegradePerDayBps = CatalogTweaksV0.ElectronicsDegradeBps
    };
    node.Name += " (Electronics Assembly)";
}
```

### Tweaks to Add to CatalogTweaksV0.cs
```csharp
public const int ElectronicsExoticCrystalsInput = 1;
public const int ElectronicsFuelInput = 1;
public const int ElectronicsOutput = 2;
public const int ElectronicsDegradeBps = 300; // 3% per day (valuable, worth protecting)
```

### Reasoning
- **Placement**: Uses the rare-metals detection flag (geoHash >= 85, assuming 15% rare metal nodes). This colocates Electronics assembly with rare deposit systems, which are geographically sparse (~5 nodes in a 100-node galaxy).
- **Inputs**: Matches recipe (1 Exotic Crystals, 1 Fuel)
- **Outputs**: Matches recipe (2 Electronics)
- **BufferDays**: 1 day (minimal; this is a rare node, should maintain tight supply)
- **Critical constraint**: This is the ONLY place Electronics are produced. If you have 3+ fracture-border nodes, you have supply diversity. If you have only 1, you have a single point of failure — which is intentional for the design.

---

## Recipe 4: AssembleComponents (Electronics + Metal → Components)

### Placement Logic
- **When**: At industrial hubs with good Metal access
- **Where**: Distributed via different modulus from Composites (avoid collision)
- **Why**: Components are the economic sink (automation, refits, fleet upkeep). High demand everywhere. Multiple production sites prevent shortages.

### Code to Add
Insert after AssembleElectronics (~line 240):

```csharp
// GATE.S7.PRODUCTION.FULL_DEPLOY.001: AssembleComponents at industrial hubs
if (i % CatalogTweaksV0.ComponentsNodeModulus == CatalogTweaksV0.ComponentsNodeOffset)
{
    state.IndustrySites[$"comp_{i}"] = new IndustrySite
    {
        Id = $"comp_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.AssembleComponents,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.Electronics, CatalogTweaksV0.ComponentsElectronicsInput },
            { WellKnownGoodIds.Metal, CatalogTweaksV0.ComponentsMetalInput }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Components, CatalogTweaksV0.ComponentsOutput } },
        BufferDays = 2,
        DegradePerDayBps = CatalogTweaksV0.ComponentsDegradeBps
    };
    node.Name += " (Component Assembly)";
}
```

### Tweaks to Add to CatalogTweaksV0.cs
```csharp
public const int ComponentsNodeModulus = 10;
public const int ComponentsNodeOffset = 4; // nodes 4, 14, 24, 34, ... => ~10% deployment
public const int ComponentsElectronicsInput = 2;
public const int ComponentsMetalInput = 3;
public const int ComponentsOutput = 1;
public const int ComponentsDegradeBps = 300; // 3% per day (high-value strategic resource)
```

### Reasoning
- **Modulus/Offset**: `10 % 4` deploys at ~10% of nodes (higher frequency than Composites because Components have universal demand)
- **Inputs**: Matches recipe (2 Electronics, 3 Metal)
- **Outputs**: Matches recipe (1 Components per batch)
- **BufferDays**: 2 days (components are valuable; maintain buffer for stability)
- **Constraint awareness**: Electronics input means Component production is gated by Electronics availability, which is gated by Exotic Crystals. This creates the intended supply chain: Exotic Crystals → Electronics → Components → Automation/Refits. Breaking the chain at any point cascades backward.

---

## Recipe 5 & 6: Salvage Conversions

These recipes are discovered-only (player picks up Salvaged Tech from derelicts), not production sites. However, you can optionally deploy salvage processors at random locations to let NPCs/player recycle discoveries.

### Code to Add (Optional)

```csharp
// GATE.S7.PRODUCTION.FULL_DEPLOY.001: Salvage processors (optional, low-frequency)
// Salvage sites are rare (player-triggered via exploration). Deployment is random.

// SalvageToMetal: convert Salvaged Tech → Metal (bulk path)
if (i % CatalogTweaksV0.SalvageNodeModulus == CatalogTweaksV0.SalvageNodeOffset)
{
    state.IndustrySites[$"salvage_metal_{i}"] = new IndustrySite
    {
        Id = $"salvage_metal_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.SalvageToMetal,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.SalvagedTech, 1 }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Metal, CatalogTweaksV0.SalvageMetalOutput } },
        BufferDays = 1,
        DegradePerDayBps = 0
    };
}

// SalvageToComponents: convert Salvaged Tech + Electronics → Components (valuable path)
if (i % CatalogTweaksV0.SalvageComponentsNodeModulus == CatalogTweaksV0.SalvageComponentsNodeOffset)
{
    state.IndustrySites[$"salvage_comp_{i}"] = new IndustrySite
    {
        Id = $"salvage_comp_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.SalvageToComponents,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.SalvagedTech, 1 },
            { WellKnownGoodIds.Electronics, 1 }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Components, CatalogTweaksV0.SalvageComponentsOutput } },
        BufferDays = 1,
        DegradePerDayBps = 0
    };
}
```

### Tweaks to Add to CatalogTweaksV0.cs
```csharp
public const int SalvageNodeModulus = 20;
public const int SalvageNodeOffset = 7; // nodes 7, 27, 47, ... => ~5% deployment
public const int SalvageComponentsNodeModulus = 20;
public const int SalvageComponentsNodeOffset = 15; // nodes 15, 35, 55, ... => ~5% deployment (different offset to avoid collision)
public const int SalvageMetalOutput = 5;
public const int SalvageComponentsOutput = 2;
```

### Reasoning
- **Optional deployment**: Unlike Ore/Metal/Munitions/Food/Composites/Electronics which are must-have, salvage processors are nice-to-have. You can ship Phase 2 without them (they only matter once players find Salvaged Tech from derelicts).
- **Inputs/Outputs**: Match recipe definitions
- **Frequency**: Lower than other factories (~5% each) because they only activate when Salvaged Tech is available locally

---

## Phase 2 Checklist

### Code Changes
- [ ] Add ProcessFood deployment block to MarketInitGen.InitMarkets()
- [ ] Add FabricateComposites deployment block
- [ ] Add AssembleElectronics deployment block
- [ ] Add AssembleComponents deployment block
- [ ] Add SalvageToMetal deployment block (optional)
- [ ] Add SalvageToComponents deployment block (optional)
- [ ] Add all tweak constants to CatalogTweaksV0.cs (16 new constants)
- [ ] Verify WellKnownRecipeIds has all recipe IDs defined

### Testing
- [ ] Run `dotnet build` (should compile without errors)
- [ ] Run golden hash tests (they should update to new world seed)
- [ ] Run ExplorationBot (should not break with new industry sites)
- [ ] Manual: Seed 42 world gen, verify all 13 goods appear in markets
- [ ] Manual: Run 1000 ticks, verify Composites/Electronics/Components appear in trades

### Tuning
- [ ] Run deterministic simulations (seeds 42, 100, 200) and measure:
  - Average price per good
  - Price volatility (StdDev / mean)
  - Good availability (% of nodes with non-zero stock)
  - Production site health (% at full efficiency)
- [ ] If good is too scarce (availability <20%), increase factory deployment % (lower modulus)
- [ ] If good is too abundant (price never spikes), reduce factory count or initial stock
- [ ] If scarcity is correct but price volatility is too high (>60% CV), increase NPC trader efficiency

---

## Risk Mitigation

### Risk: Composites Never Reach Buyers (Organics Supply Breaks)
**Symptom**: Composites price spikes to 999, production stops, factories degrade.
**Root cause**: Organics is 40% of nodes, distributed randomly. Some regions might have no nearby Organics.
**Fix**: If playtesting shows regional Organics deserts, either:
1. Increase OrganicsNodePct in CatalogTweaksV0 from 40 to 50%
2. Reduce CompositesNodeModulus from 12 to 10 (more factories, more demand elasticity)
3. Add inter-regional NPC trade specifically for Organics (hardcoded "Organics shortage response")

### Risk: Electronics Dominated by One Fracture-Border Node
**Symptom**: All Electronics flow from one node; embargo it and game breaks.
**Root cause**: Rare metal detection only triggers ~15% of nodes; one node might be excluded.
**Fix**: Change placement logic from exact overlap to: `if (i % CatalogTweaksV0.ElectronicsNodeModulus == CatalogTweaksV0.ElectronicsNodeOffset)` with separate modulus/offset, guaranteeing multiple Electronics sites.

### Risk: Components Too Hard to Access
**Symptom**: Player can buy Electronics but no Component factories are nearby (long supply chains).
**Root cause**: ComponentsNodeOffset lands far from Electronics or Metal sources.
**Fix**: Use a smaller modulus (8 instead of 10) to deploy more factories, reducing average distance.

### Risk: Phase 2 Breaks Golden Hash
**Symptom**: `dotnet test SimCore.Tests --filter RoadmapConsistency` fails.
**Root cause**: WorldLoader uses the same seed-based initialization; new industry sites change world hash.
**Fix**: Golden hash MUST be updated as part of Phase 2 commit. Run `dotnet test ... --filter GoldenHashUpdate` to refresh.

---

## Appendix: All Tweak Constants (Consolidated)

Copy-paste this into CatalogTweaksV0.cs after existing constants:

```csharp
// GATE.S7.PRODUCTION.FULL_DEPLOY.001: Phase 2 recipe deployment
// ProcessFood
public const int FoodProcessorOrganicsInput = 2;
public const int FoodProcessorFuelInput = 1;
public const int FoodProcessorFoodOutput = 3;
public const int FoodProcessorDegradeBps = 500;

// FabricateComposites
public const int CompositesNodeModulus = 12;
public const int CompositesNodeOffset = 3;
public const int CompositesMetalInput = 3;
public const int CompositesOrganicsInput = 2;
public const int CompositesOutput = 2;
public const int CompositesDegradeBps = 500;

// AssembleElectronics (fracture-border only, deployed via rare-metal overlap)
public const int ElectronicsExoticCrystalsInput = 1;
public const int ElectronicsFuelInput = 1;
public const int ElectronicsOutput = 2;
public const int ElectronicsDegradeBps = 300;

// AssembleComponents
public const int ComponentsNodeModulus = 10;
public const int ComponentsNodeOffset = 4;
public const int ComponentsElectronicsInput = 2;
public const int ComponentsMetalInput = 3;
public const int ComponentsOutput = 1;
public const int ComponentsDegradeBps = 300;

// Salvage processors (optional)
public const int SalvageNodeModulus = 20;
public const int SalvageNodeOffset = 7;
public const int SalvageComponentsNodeModulus = 20;
public const int SalvageComponentsNodeOffset = 15;
public const int SalvageMetalOutput = 5;
public const int SalvageComponentsOutput = 2;
```

---

## Estimation

**Lines of code to write**: ~120 (6 blocks × 20 lines each)
**Tweaks to add**: 16 constants (~10 lines)
**Total effort**: 2-3 hours (write, test, tune, validate)
**Risk**: Low (proven pattern, no cross-system dependencies)
**Impact**: Unlocks entire production chain simulation and economic pressure systems

Once complete, module sustain enforcement can be turned on, and the economy becomes mechanically complete.

---

**End of Document**
