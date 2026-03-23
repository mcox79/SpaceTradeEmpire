# T2 Faction Modules — Design Specification v0

**Status**: CONTENT DRAFT
**Date**: 2026-03-21
**Tier**: T2 Military — faction reputation locked, rare material sustain
**Total**: 40 modules (8 per faction)

---

## Design Principles

1. **Playstyle-enabling, not stat-padding.** Every module must unlock a behavior the player cannot perform without it. "+10% damage" is not a module. "Damage adapts to target weakness after 3 hits" is.
2. **Faction philosophy expressed mechanically.** Concord modules feel institutional. Chitin modules feel probabilistic. Weavers feel structural. Valorin feel aggressive. Communion modules feel perceptive.
3. **Build identity convergence.** Each faction's 8 modules should support 3-4 distinct build identities. A player running 3 Concord modules should be able to describe their build in one sentence.
4. **Sustain cost variety.** T2 modules consume uncommon goods (Composites, Rare Metals, Electronics, Exotic Crystals) every 60 ticks. Supply chain diversity forces multi-faction engagement.

---

## Concord — Institutional, Reliable, Zero-Variance

**Design thesis**: Concord modules remove randomness and reward planning. They make the predictable player unstoppable and the reckless player no better off.

### 1. Standard Railgun

| Field | Value |
|-------|-------|
| ID | `concord_standard_railgun` |
| Display Name | Standard Railgun |
| Faction / Rep Tier | Concord / Friendly |
| Slot Kind | Weapon |
| Credit Cost | 18,000 |
| Stats | BaseDamage: 45, HeatPerShot: 12, Range: 180, PowerDraw: 8 |
| Sustain | 2 Composites / 60 ticks |
| Special Effect | Damage has zero variance — always deals exactly BaseDamage. No crits, no glances. Ignores all target evasion modifiers below 30%. Damage readout shows exact HP remaining on target after each shot. |
| Lore | "Concord ordnance specification 7-4-19: 'A weapon that fires the same way every time is a weapon you can plan around. Planning wins wars. Surprises lose them.'" |
| Build Identity | Fleet Commander, Methodical Fighter |

### 2. Point Defense Matrix

| Field | Value |
|-------|-------|
| ID | `concord_point_defense_matrix` |
| Display Name | Point Defense Matrix |
| Faction / Rep Tier | Concord / Friendly |
| Slot Kind | Weapon |
| Credit Cost | 22,000 |
| Stats | BaseDamage: 8 (vs ships), MissileIntercept: 85%, DroneIntercept: 60%, HeatPerShot: 3, PowerDraw: 6 |
| Sustain | 1 Components / 60 ticks |
| Special Effect | Automatically intercepts incoming missiles and drones within 120u radius. Each intercepted projectile generates 0.5 heat instead of full impact damage. When no projectiles are inbound, fires low-damage suppressive bursts at nearest hostile. Intercept rate scales with spin rate — faster spin = better coverage arc. |
| Lore | "Every Concord patrol cruiser carries one. Not because missiles are common, but because Concord does not tolerate uncertainty about what reaches the hull." |
| Build Identity | Fleet Commander, Escort Specialist |

### 3. Diplomatic Shield

| Field | Value |
|-------|-------|
| ID | `concord_diplomatic_shield` |
| Display Name | Diplomatic Shield |
| Faction / Rep Tier | Concord / Honored |
| Slot Kind | Shield |
| Credit Cost | 28,000 |
| Stats | ShieldBonusFlat: +80, ShieldRegenRate: 2/tick, PowerDraw: 10 |
| Sustain | 2 Composites / 60 ticks |
| Special Effect | When any allied NPC fleet within 200u takes damage, 15% of that damage is redirected to this ship's shields instead. Triggers a visible tether effect between ships. If shields are depleted, redistribution pauses until shields regenerate above 30%. Maximum 3 simultaneous tethers. |
| Lore | "The shield spec was written by Concord's diplomatic corps, not their military. 'We share the burden' is not a metaphor to them." |
| Build Identity | Fleet Commander, Convoy Protector |

### 4. Institutional Drive

| Field | Value |
|-------|-------|
| ID | `concord_institutional_drive` |
| Display Name | Institutional Drive |
| Faction / Rep Tier | Concord / Friendly |
| Slot Kind | Drive |
| Credit Cost | 20,000 |
| Stats | SpeedBonusPct: +8%, FuelEfficiency: +35%, PowerDraw: 5 |
| Sustain | 1 Fuel / 60 ticks |
| Special Effect | Lane transit times are reduced by 20% on any route the player has traveled 3+ times. The drive learns established routes and optimizes burn profiles. Unlocks a "Route Memory" HUD panel showing fuel savings per known route. Fuel consumption in fracture space reduced by 15%. |
| Lore | "It is not the fastest drive in known space. It is the most reliable. Concord engineers consider this a compliment." |
| Build Identity | Trade Magnate, Long-Haul Hauler |

### 5. Logistics Optimizer

| Field | Value |
|-------|-------|
| ID | `concord_logistics_optimizer` |
| Display Name | Logistics Optimizer |
| Faction / Rep Tier | Concord / Honored |
| Slot Kind | Utility |
| Credit Cost | 25,000 |
| Stats | CargoBonusFlat: +15, PowerDraw: 4 |
| Sustain | 1 Components / 60 ticks |
| Special Effect | Cargo hold efficiency scales with route regularity. For each good type the player has traded 5+ times in the last 200 ticks, that good occupies 20% less cargo space (stacks up to 3 good types). The module rewards routine — a player running the same 3-good triangle route gets effectively +45% cargo capacity for those goods. Resets if the player stops trading a good for 100 ticks. |
| Lore | "Containerization. Standardized manifests. Predictable loading sequences. Concord made logistics boring. Boring is profitable." |
| Build Identity | Trade Magnate, Route Optimizer |

### 6. Fleet Command Relay

| Field | Value |
|-------|-------|
| ID | `concord_fleet_command_relay` |
| Display Name | Fleet Command Relay |
| Faction / Rep Tier | Concord / Allied |
| Slot Kind | Utility |
| Credit Cost | 35,000 |
| Stats | PowerDraw: 7 |
| Sustain | 2 Electronics / 60 ticks |
| Special Effect | All allied NPC fleets within 300u gain +25% weapon accuracy and +15% damage. The player can designate a "priority target" — all allied fleets focus fire on that target with an additional +10% damage bonus. Priority target designation has a 30-tick cooldown. Allied fleets also gain the player's combat stance bonuses at 50% effectiveness. |
| Lore | "Concord admirals lead from the center, not the front. The relay makes 'the center' anywhere the flagship is." |
| Build Identity | Fleet Commander, Warfront Commander |

### 7. Audit Scanner

| Field | Value |
|-------|-------|
| ID | `concord_audit_scanner` |
| Display Name | Audit Scanner |
| Faction / Rep Tier | Concord / Honored |
| Slot Kind | Special |
| Credit Cost | 30,000 |
| Stats | ScanRangeBonusPct: +20%, PowerDraw: 5 |
| Sustain | 1 Electronics / 60 ticks |
| Special Effect | Reveals complete market data at any station within scan range without docking — all buy/sell prices, stock levels, tariff rates, and demand forecasts. Also reveals NPC fleet cargo manifests on scan. At Concord stations, reveals the hidden "institutional price" — the pre-tariff baseline that Concord uses internally, allowing the player to negotiate from a position of full information. |
| Lore | "The Concord Revenue Service does not guess. Neither should you." |
| Build Identity | Info Broker, Trade Magnate |

### 8. Regulatory Transponder Mk II

| Field | Value |
|-------|-------|
| ID | `concord_regulatory_transponder_mk2` |
| Display Name | Regulatory Transponder Mk II |
| Faction / Rep Tier | Concord / Allied |
| Slot Kind | Special |
| Credit Cost | 40,000 |
| Stats | PowerDraw: 3 |
| Sustain | 1 Components / 60 ticks |
| Special Effect | Eliminates all tariffs at Concord stations. Reduces tariffs by 50% at Weaver and Chitin stations (Concord trade agreements). In exchange, the player's position and cargo manifest are visible to all Concord patrols at all times — no stealth, no contraband. If the player engages in smuggling or embargo violation while the transponder is active, Concord reputation drops at 3x normal rate. Can be toggled off at any dock, but re-enabling requires 20-tick cooldown. |
| Lore | "Full transparency for full market access. Concord calls it 'regulatory alignment.' Everyone else calls it a leash." |
| Build Identity | Trade Magnate, Concord Loyalist |

---

## Chitin Syndicates — Adaptive, Information, Probability

**Design thesis**: Chitin modules reward information gathering and exploit uncertainty. They are strongest when the player knows more than their opponent — about markets, about combat, about the galaxy.

### 9. Metamorphic Cannon

| Field | Value |
|-------|-------|
| ID | `chitin_metamorphic_cannon` |
| Display Name | Metamorphic Cannon |
| Faction / Rep Tier | Chitin / Friendly |
| Slot Kind | Weapon |
| Credit Cost | 20,000 |
| Stats | BaseDamage: 30, HeatPerShot: 10, AdaptDamageMax: 55, PowerDraw: 9 |
| Sustain | 2 Rare Metals / 60 ticks |
| Special Effect | Damage type shifts after each hit, cycling through Kinetic/Energy/Explosive. After 3 consecutive hits on the same target, the cannon locks onto the target's weakest damage type and stays there for the remainder of the engagement. First 3 shots are exploratory; every shot after is optimized. Against new targets, the cycle resets. |
| Lore | "The Chitin do not ask what armor you have. They fire three times and the weapon tells them." |
| Build Identity | Adaptive Fighter, Duelist |

### 10. Hivemind Targeting

| Field | Value |
|-------|-------|
| ID | `chitin_hivemind_targeting` |
| Display Name | Hivemind Targeting Array |
| Faction / Rep Tier | Chitin / Honored |
| Slot Kind | Weapon |
| Credit Cost | 26,000 |
| Stats | BaseDamage: 22, HeatPerShot: 8, PowerDraw: 7 |
| Sustain | 1 Electronics / 60 ticks |
| Special Effect | Damage scales by +8% for each allied fleet within 150u (maximum +40% at 5 allies). Additionally, when multiple ships with Hivemind Targeting fire at the same target, each subsequent ship gains +15% accuracy (sensor data sharing). Solo, this weapon is mediocre. In a swarm, it becomes devastating. |
| Lore | "One antenna is a sensor. Two is a direction. A thousand is omniscience." |
| Build Identity | Swarm Tactician, Fleet Commander |

### 11. Adaptive Carapace

| Field | Value |
|-------|-------|
| ID | `chitin_adaptive_carapace` |
| Display Name | Adaptive Carapace |
| Faction / Rep Tier | Chitin / Friendly |
| Slot Kind | Shield |
| Credit Cost | 24,000 |
| Stats | ShieldBonusFlat: +50, ZoneArmorBonusFlat: +8 (all zones), PowerDraw: 6 |
| Sustain | 2 Composites / 60 ticks |
| Special Effect | Each time a zone takes damage, that zone gains +3 armor (stacking up to +15 per zone). Armor bonus persists until the engagement ends. Against sustained fire from one direction, the hull literally hardens. Bonus resets between combats. At +15 max stacks, the zone also reflects 5% of incoming damage back to the attacker. |
| Lore | "Chitin exoskeletons harden under stress. Their ships do the same. The Syndicates call it 'learning to be hit.'" |
| Build Identity | Adaptive Fighter, Endurance Tank |

### 12. Phase-Shift Drive

| Field | Value |
|-------|-------|
| ID | `chitin_phase_shift_drive` |
| Display Name | Phase-Shift Drive |
| Faction / Rep Tier | Chitin / Allied |
| Slot Kind | Drive |
| Credit Cost | 45,000 |
| Stats | SpeedBonusPct: +12%, PowerDraw: 12 |
| Sustain | 2 Exotic Crystals / 60 ticks |
| Special Effect | Enables micro-jumps: the player can teleport up to 40u in any direction with a 60-tick cooldown. Micro-jumps do not require lane gates — they are short-range phase transitions through the thread lattice. Cannot jump through solid objects (stations, asteroids). Jump destination must be within scanned space. Each micro-jump inflicts 5 hull stress (accommodation geometry strain). Reduces fracture travel time by 10%. |
| Lore | "The Chitin reverse-engineered this from studying the player's fracture module. They got close enough. The 5-point hull stress per jump is how close." |
| Build Identity | Scout, Glass Cannon, Deep Explorer |

### 13. Data Siphon

| Field | Value |
|-------|-------|
| ID | `chitin_data_siphon` |
| Display Name | Data Siphon |
| Faction / Rep Tier | Chitin / Friendly |
| Slot Kind | Utility |
| Credit Cost | 16,000 |
| Stats | ScanRangeBonusPct: +15%, PowerDraw: 4 |
| Sustain | 1 Electronics / 60 ticks |
| Special Effect | Passively collects trade intelligence from every station the player docks at. After visiting 5+ stations, unlocks a "Trade Heat Map" overlay showing price differentials across all visited stations, updated in real-time. The overlay highlights the single most profitable 2-hop route available. Data decays after 100 ticks without revisiting a station. |
| Lore | "Information is the only commodity that increases in value when shared with the right people and decreases when shared with everyone." |
| Build Identity | Info Broker, Trade Magnate |

### 14. ECM Suite

| Field | Value |
|-------|-------|
| ID | `chitin_ecm_suite` |
| Display Name | Electronic Countermeasures Suite |
| Faction / Rep Tier | Chitin / Honored |
| Slot Kind | Utility |
| Credit Cost | 28,000 |
| Stats | PowerDraw: 8 |
| Sustain | 1 Rare Metals / 60 ticks |
| Special Effect | Reduces all enemy weapon accuracy against the player by 20%. Enemy scan range for detecting the player is halved. When activated (toggle, 10-tick duration, 45-tick cooldown), creates a sensor ghost — a false contact on enemy scanners 100u away from the player's actual position. NPCs will investigate the ghost, buying escape time. Ghost does not work against players or Lattice drones. |
| Lore | "The best fight is one your enemy has with empty space while you leave." |
| Build Identity | Scout, Smuggler, Evasion Specialist |

### 15. Probability Engine

| Field | Value |
|-------|-------|
| ID | `chitin_probability_engine` |
| Display Name | Probability Engine |
| Faction / Rep Tier | Chitin / Allied |
| Slot Kind | Special |
| Credit Cost | 42,000 |
| Stats | PowerDraw: 6 |
| Sustain | 2 Electronics / 60 ticks |
| Special Effect | Generates price predictions for all goods at all stations within 2 hops, 50 ticks into the future, with 85% accuracy. Predictions are displayed as probability distributions (price ranges with confidence bars). At 100+ ticks of operation, accuracy improves to 92%. Also predicts warfront demand shifts 30 ticks in advance, allowing the player to pre-position war materiel before demand spikes. |
| Lore | "The Syndicates built this for themselves. They sold it to you because their models say you will make them more money with it than without it. The model is correct." |
| Build Identity | Info Broker, Market Manipulator |

### 16. Signal Broker

| Field | Value |
|-------|-------|
| ID | `chitin_signal_broker` |
| Display Name | Signal Broker Antenna |
| Faction / Rep Tier | Chitin / Honored |
| Slot Kind | Special |
| Credit Cost | 32,000 |
| Stats | PowerDraw: 5 |
| Sustain | 1 Electronics / 60 ticks |
| Special Effect | Converts collected trade intelligence into a sellable commodity: "Market Reports." After visiting 8+ unique stations in 200 ticks, the module generates 1 Market Report (value: 800-1,500 credits depending on data freshness). Market Reports can be sold at any Chitin station for credits, or traded to other factions for +5 reputation. Concord pays double for reports containing embargo violation data. Valorin pay triple for frontier price data. |
| Lore | "Everyone sells goods. The Chitin sell the knowledge of where to sell goods. The margin on knowledge is infinite." |
| Build Identity | Info Broker, Diplomat |

---

## Weavers — Building, Repair, Endurance

**Design thesis**: Weaver modules extend engagements, repair damage, and build infrastructure. They reward patience and long-term investment over burst performance.

### 17. Welding Beam

| Field | Value |
|-------|-------|
| ID | `weaver_welding_beam` |
| Display Name | Welding Beam |
| Faction / Rep Tier | Weavers / Friendly |
| Slot Kind | Weapon |
| Credit Cost | 19,000 |
| Stats | BaseDamage: 18, HealRate: 12/tick (allied target), HeatPerShot: 6, PowerDraw: 7 |
| Sustain | 2 Composites / 60 ticks |
| Special Effect | Toggle between damage mode and repair mode. In repair mode, restores 12 hull HP/tick to a targeted allied fleet within 80u (cannot self-target). In damage mode, deals reduced base damage but applies a "Structural Weakness" debuff — target takes +10% damage from all sources for 15 ticks. A weapon that either heals friends or makes enemies fragile. |
| Lore | "The Weavers do not distinguish between a tool and a weapon. A welding torch cuts as well as it joins. Intent is the only variable." |
| Build Identity | Fleet Healer, Support Specialist |

### 18. Construction Drones

| Field | Value |
|-------|-------|
| ID | `weaver_construction_drones` |
| Display Name | Construction Drone Bay |
| Faction / Rep Tier | Weavers / Honored |
| Slot Kind | Weapon |
| Credit Cost | 30,000 |
| Stats | DroneDamage: 8/tick (per drone), DroneRepair: 4/tick (per drone), DroneCount: 4, PowerDraw: 9 |
| Sustain | 2 Composites + 1 Metal / 60 ticks |
| Special Effect | Deploys 4 autonomous drones that prioritize: (1) repairing player hull if below 60%, (2) repairing allied fleet hull if below 40%, (3) attacking nearest hostile. Drones have 15 HP each and can be destroyed. Destroyed drones regenerate at dock (1 per dock visit). In non-combat, drones passively repair 2 hull HP/tick on the player's ship. Drones also accelerate Haven construction projects by 15% when the player is docked at Haven. |
| Lore | "A Weaver who builds alone builds slowly. A Weaver with four hands builds a world." |
| Build Identity | Fleet Healer, Haven Builder |

### 19. Composite Layering

| Field | Value |
|-------|-------|
| ID | `weaver_composite_layering` |
| Display Name | Composite Layering System |
| Faction / Rep Tier | Weavers / Friendly |
| Slot Kind | Shield |
| Credit Cost | 22,000 |
| Stats | ZoneArmorBonusFlat: +12 (all zones), PowerDraw: 4 |
| Sustain | 2 Composites / 60 ticks |
| Special Effect | Out of combat, zone armor regenerates at 1 HP/tick per zone (up to base + bonus maximum). In combat, regeneration stops but damage to any zone is reduced by a flat 3 (applied after all other modifiers). The ship becomes extremely durable in prolonged engagements — small incoming hits are nearly negated while armor slowly rebuilds between fights. No shield bonus; this is pure structural resilience. |
| Lore | "Silk layered seven times stops a blade. Composite layered seven times stops a railgun. The principle is identical." |
| Build Identity | Endurance Tank, Solo Trader |

### 20. Echo Drive

| Field | Value |
|-------|-------|
| ID | `weaver_echo_drive` |
| Display Name | Echo Drive |
| Faction / Rep Tier | Weavers / Honored |
| Slot Kind | Drive |
| Credit Cost | 26,000 |
| Stats | SpeedBonusPct: +22%, PowerDraw: 8 |
| Sustain | 1 Composites + 1 Fuel / 60 ticks |
| Special Effect | Fastest conventional drive available. However, each lane transit inflicts 3 hull stress (structural resonance from speed). Hull stress is repaired automatically at 1/tick when docked. The trade-off: get places fast, spend time repairing. Pairs naturally with Weaver repair modules. At stations with Weaver Drydock access, repair rate doubles. Also leaves a "resonance trail" visible on scanners for 30 ticks — other players/NPCs can track where the ship has been. |
| Lore | "Speed has a price. The Weavers charge it honestly — in structural wear, not in fuel markups." |
| Build Identity | Speed Trader, Glass Cannon |

### 21. Structural Reinforcement

| Field | Value |
|-------|-------|
| ID | `weaver_structural_reinforcement` |
| Display Name | Structural Reinforcement Frame |
| Faction / Rep Tier | Weavers / Allied |
| Slot Kind | Utility |
| Credit Cost | 38,000 |
| Stats | SlotBonusFlat: +2, MassBonusPct: +25%, SpeedPenaltyPct: -10%, PowerDraw: 3 |
| Sustain | 3 Composites / 60 ticks |
| Special Effect | Adds 2 additional module slots to the ship. The ship becomes heavier (+25% mass) and slower (-10% speed), but can now mount a loadout that would normally require a larger hull class. A Frigate with Structural Reinforcement has 8 slots — Cruiser territory. The mass penalty means the extra slots are best used for utility/cargo, not weapons (which add more mass). |
| Lore | "The Weavers can make a Corvette carry a Cruiser's loadout. They cannot make it fly like one." |
| Build Identity | Overfit Specialist, Haven Builder |

### 22. Tractor Fabricator

| Field | Value |
|-------|-------|
| ID | `weaver_tractor_fabricator` |
| Display Name | Tractor Fabricator |
| Faction / Rep Tier | Weavers / Friendly |
| Slot Kind | Utility |
| Credit Cost | 18,000 |
| Stats | SalvageBonusPct: +40%, PowerDraw: 5 |
| Sustain | 1 Metal / 60 ticks |
| Special Effect | After any combat engagement, automatically salvages 40% more materials from defeated enemies. Salvaged materials are processed into usable goods on-ship (Salvaged Tech has a 25% chance to yield Components, Metal, or Rare Metals instead). Also enables field repairs: the player can consume 5 Metal from cargo to restore 30 hull HP at any time, with a 20-tick cooldown. No dock required. |
| Lore | "Waste is a failure of imagination. The Weavers have never lacked imagination." |
| Build Identity | Scavenger, Solo Explorer |

### 23. Resonance Forge

| Field | Value |
|-------|-------|
| ID | `weaver_resonance_forge` |
| Display Name | Resonance Forge |
| Faction / Rep Tier | Weavers / Allied |
| Slot Kind | Special |
| Credit Cost | 44,000 |
| Stats | PowerDraw: 8 |
| Sustain | 2 Composites + 1 Rare Metals / 60 ticks |
| Special Effect | Unlocks the ability to accept and complete Commission contracts at any Weaver station. Commissions are large-scale construction orders (build X goods, deliver to Y location) with substantial credit and reputation rewards. The Forge tracks up to 3 active Commissions simultaneously. Completing a Commission at a station where the player has "Thread-Bonded" status grants double reputation. Also reduces Haven construction material costs by 10%. |
| Lore | "The Forge is not a furnace. It is a contract. The Weavers build what the galaxy needs, and the galaxy pays what the Weavers ask." |
| Build Identity | Haven Builder, Commission Runner |

### 24. Load-Bearing Frame

| Field | Value |
|-------|-------|
| ID | `weaver_load_bearing_frame` |
| Display Name | Load-Bearing Frame |
| Faction / Rep Tier | Weavers / Honored |
| Slot Kind | Special |
| Credit Cost | 34,000 |
| Stats | CargoBonusFlat: +10, HullBonusFlat: +40, PowerDraw: 4 |
| Sustain | 2 Composites / 60 ticks |
| Special Effect | Reduces all Haven construction material costs by 25%. When docked at Haven, the player's construction drones (if equipped) work at 2x speed. Also unlocks "Bulk Delivery" mode — when delivering 50+ units of a single good to Haven, delivery time is halved and the player receives a 5% credit rebate on material value. The frame makes the ship a construction vehicle, not just a trade vessel. |
| Lore | "Everything the Weavers build begins with a frame that can bear the load. The frame came first. The universe came second." |
| Build Identity | Haven Builder, Hauler Specialist |

---

## Valorin Clans — Military, Speed, Aggression

**Design thesis**: Valorin modules hit hard, move fast, and reward kills. They are glass cannons with snowball potential — each kill makes the next easier.

### 25. Kinetic Accelerator

| Field | Value |
|-------|-------|
| ID | `valorin_kinetic_accelerator` |
| Display Name | Kinetic Accelerator |
| Faction / Rep Tier | Valorin / Friendly |
| Slot Kind | Weapon |
| Credit Cost | 16,000 |
| Stats | BaseDamage: 55, HeatPerShot: 18, Range: 220, PowerDraw: 12 |
| Sustain | 2 Rare Metals / 60 ticks |
| Special Effect | Highest single-shot damage of any T2 weapon. Extreme range. However, heat per shot is massive — a ship with standard heat capacity can fire 4-5 times before forced ceasefire. Damage bonus vs hull: +30% (kinetic penetration). Against shielded targets, damage is reduced by 20%. A weapon that rewards finishing blows and punishes shield-heavy opponents' allies after shields are stripped by other weapons. |
| Lore | "The Valorin do not build elegant weapons. They build weapons that end fights." |
| Build Identity | Glass Cannon, Alpha Striker |

### 26. Swarm Coordinator

| Field | Value |
|-------|-------|
| ID | `valorin_swarm_coordinator` |
| Display Name | Swarm Coordinator |
| Faction / Rep Tier | Valorin / Honored |
| Slot Kind | Weapon |
| Credit Cost | 28,000 |
| Stats | DroneDamage: 6/tick (per drone), DroneCount: 8, DroneHP: 8, PowerDraw: 10 |
| Sustain | 1 Rare Metals + 1 Munitions / 60 ticks |
| Special Effect | Deploys 8 attack drones that swarm a single target. Drones deal individual damage but share targeting — all 8 hit the same zone, overwhelming zone armor rapidly. When the target is destroyed, drones automatically redirect to the nearest hostile within 100u. Destroyed drones self-fabricate at 1 per 15 ticks during combat (requires Munitions in cargo). Outside combat, all drones regenerate at dock. Unlike Weaver drones, these never repair — they only kill. |
| Lore | "Eight is a patrol. Sixteen is a warning. Twenty-four is a clan that has decided you should not be here." |
| Build Identity | Swarm Tactician, Zone Breaker |

### 27. Reactive Armor

| Field | Value |
|-------|-------|
| ID | `valorin_reactive_armor` |
| Display Name | Reactive Armor Plating |
| Faction / Rep Tier | Valorin / Friendly |
| Slot Kind | Shield |
| Credit Cost | 20,000 |
| Stats | ZoneArmorBonusFlat: +10 (all zones), ShieldBonusFlat: +20, PowerDraw: 5 |
| Sustain | 2 Rare Metals / 60 ticks |
| Special Effect | When any zone takes damage, the attacker receives 25% of the damage dealt as hull damage (bypasses shields and armor). Reactive detonation also generates 8 heat on the attacker's ship. The armor does not prevent damage — it punishes anyone who inflicts it. Against swarm enemies (multiple small attackers), the retaliation damage per hit is small but the aggregate heat buildup forces them into overheat faster. |
| Lore | "A Valorin proverb: 'Hit me once and it costs you twice. Hit me twice and you are already dead.'" |
| Build Identity | Brawler, Counter-Puncher |

### 28. Raid Drive

| Field | Value |
|-------|-------|
| ID | `valorin_raid_drive` |
| Display Name | Raid Drive |
| Faction / Rep Tier | Valorin / Honored |
| Slot Kind | Drive |
| Credit Cost | 24,000 |
| Stats | SpeedBonusPct: +30%, PowerDraw: 10 |
| Sustain | 2 Fuel / 60 ticks |
| Special Effect | Fastest drive in the game. Lane transit time reduced by 30%. However, the drive runs dangerously hot — while in transit, ship heat capacity is reduced by 40%. Arriving at a destination after lane transit means starting any combat encounter at 60% heat capacity instead of full. A 10-tick cooldown after arrival restores normal heat capacity. The drive rewards hit-and-run: arrive fast, strike hard in the opening seconds, disengage before overheat. |
| Lore | "The Valorin word for 'arrive' and the Valorin word for 'attack' are the same word." |
| Build Identity | Raider, Speed Trader, Hit-and-Run |

### 29. Kill-Mark Targeter

| Field | Value |
|-------|-------|
| ID | `valorin_kill_mark_targeter` |
| Display Name | Kill-Mark Targeter |
| Faction / Rep Tier | Valorin / Allied |
| Slot Kind | Utility |
| Credit Cost | 36,000 |
| Stats | PowerDraw: 6 |
| Sustain | 1 Rare Metals / 60 ticks |
| Special Effect | Tracks lifetime kill count. Every 5 kills grants a permanent +2% damage bonus (maximum +20% at 50 kills). Kill count persists across sessions (saved). Additionally, after each kill in combat, the player gains a 15-tick buff: +10% speed, +5% damage, and +5 heat rejection rate. The buff refreshes on each kill, creating a "killing spree" momentum that makes multi-target engagements increasingly favorable. |
| Lore | "Valorin pilots mark their hulls. Not for pride. Each mark recalibrates the targeting system. The ship learns what dying looks like." |
| Build Identity | Ace Pilot, Warfront Specialist |

### 30. Salvage Rights Scanner

| Field | Value |
|-------|-------|
| ID | `valorin_salvage_rights_scanner` |
| Display Name | Salvage Rights Scanner |
| Faction / Rep Tier | Valorin / Friendly |
| Slot Kind | Utility |
| Credit Cost | 15,000 |
| Stats | ScanRangeBonusPct: +10%, PowerDraw: 3 |
| Sustain | 1 Rare Metals / 60 ticks |
| Special Effect | Defeated enemies drop 50% more loot. Loot table is biased toward the most valuable items (top 2 items have +30% drop chance). Also reveals loot contents of NPC fleets on scan before engagement — the player can see what they will get for winning, enabling informed decisions about which fights are worth taking. Wreck salvage timer extended to 60 ticks (normally 30) — more time to collect. |
| Lore | "The Valorin do not waste what they kill. That is not mercy. That is efficiency." |
| Build Identity | Raider, Scavenger |

### 31. Conscription Beacon

| Field | Value |
|-------|-------|
| ID | `valorin_conscription_beacon` |
| Display Name | Conscription Beacon |
| Faction / Rep Tier | Valorin / Allied |
| Slot Kind | Special |
| Credit Cost | 40,000 |
| Stats | PowerDraw: 8 |
| Sustain | 2 Munitions / 60 ticks |
| Special Effect | Activates a "Call to Arms" signal (90-tick cooldown). Within 30 ticks of activation, 2-4 Valorin mercenary corvettes warp in and fight alongside the player for 60 ticks before departing. Mercenaries are Valorin-spec (weak individually, strong in numbers). At Allied reputation with Valorin, mercenary count increases to 4-6 and they gain +15% damage. If the player is in Valorin territory, mercenaries arrive 50% faster. Mercenaries do not persist — they are combat reinforcements, not permanent fleet. |
| Lore | "Blood-Kin do not fight alone. If you have earned the mark, the clans answer." |
| Build Identity | Warfront Commander, Raider |

### 32. Garrison Beacon

| Field | Value |
|-------|-------|
| ID | `valorin_garrison_beacon` |
| Display Name | Garrison Beacon |
| Faction / Rep Tier | Valorin / Honored |
| Slot Kind | Special |
| Credit Cost | 32,000 |
| Stats | PowerDraw: 6 |
| Sustain | 1 Munitions + 1 Fuel / 60 ticks |
| Special Effect | Can be deployed at any system node (120-tick cooldown, maximum 2 active). A deployed beacon creates a "Garrison Zone" — Valorin patrol corvettes (2-3) spawn and patrol within 150u of the beacon for 200 ticks. The garrison deters NPC aggression (hostile NPCs avoid the zone), protects trade routes, and claims the system for Valorin influence scoring. Beacons can be recalled early. If the beacon's system enters Phase 2+ instability, garrison corvettes take hull damage over time and eventually withdraw. |
| Lore | "The Valorin plant flags. They mean 'we were here first' and 'we will still be here when you leave.'" |
| Build Identity | Territory Controller, Warfront Commander |

---

## Drifter Communion — Mystical, Stealth, Perception

**Design thesis**: Communion modules extend the player's senses and reduce the cost of exploring the unknown. They reward going deeper, staying longer, and seeing what others cannot.

### 33. Crystal Harmonic Lens

| Field | Value |
|-------|-------|
| ID | `communion_crystal_harmonic_lens` |
| Display Name | Crystal Harmonic Lens |
| Faction / Rep Tier | Communion / Friendly |
| Slot Kind | Weapon |
| Credit Cost | 21,000 |
| Stats | BaseDamage: 28, HeatPerShot: 7, PowerDraw: 8 |
| Sustain | 1 Exotic Crystals / 60 ticks |
| Special Effect | Damage bypasses shields entirely — hits zone armor directly. Against unshielded targets, deals normal damage to hull. The lens reads the harmonic frequency of the target's shield emitter and phases the beam through it. Does not destroy shields (they remain intact for other attackers). This makes the Lens the premier weapon against shield-heavy Concord vessels and Lattice drones, but mediocre against armor-heavy Weaver hulls. |
| Lore | "The Communion does not fight shields. They walk through them, the way they walk through everything the galaxy builds to keep things separate." |
| Build Identity | Shield Piercer, Anti-Capital |

### 34. Crystal Hull Coating

| Field | Value |
|-------|-------|
| ID | `communion_crystal_hull_coating` |
| Display Name | Crystal Hull Coating |
| Faction / Rep Tier | Communion / Honored |
| Slot Kind | Weapon |
| Credit Cost | 26,000 |
| Stats | HullRegenRate: 3/tick (out of combat), HullRegenCombat: 1/tick, PowerDraw: 6 |
| Sustain | 2 Exotic Crystals / 60 ticks |
| Special Effect | The ship's hull regenerates without docking at a station. Out of combat: 3 HP/tick. In combat: 1 HP/tick. The coating is grown from Communion phase-locked crystals and repairs structural damage by re-growing hull material from ambient metric energy. In Phase 1+ (Shimmer) space, regeneration rate doubles. In Phase 3+ (Fracture) space, regeneration triples. The deeper into instability, the faster the ship heals. Cannot exceed maximum hull HP. |
| Lore | "The crystal grows in broken space. So does the hull. The Communion considers this poetic. Their engineers consider it practical." |
| Build Identity | Deep Explorer, Endurance Specialist |

### 35. Silence Field

| Field | Value |
|-------|-------|
| ID | `communion_silence_field` |
| Display Name | Silence Field |
| Faction / Rep Tier | Communion / Honored |
| Slot Kind | Shield |
| Credit Cost | 30,000 |
| Stats | ShieldBonusFlat: +30, PowerDraw: 9 |
| Sustain | 1 Exotic Crystals / 60 ticks |
| Special Effect | Activatable (60-tick cooldown). When triggered, the ship becomes completely invisible to all sensors for 10 ticks. During invisibility: cannot fire weapons, cannot dock, cannot be targeted. All incoming projectiles lose lock and miss. Movement is unrestricted. Perfect for breaking combat when overwhelmed, escaping ambushes, or repositioning. In Phase 1+ space, duration extends to 14 ticks. Communion NPCs can still detect the player (they sense the field's harmonic signature). |
| Lore | "Silence is not the absence of sound. It is the absence of being noticed. The Communion has practiced this for centuries." |
| Build Identity | Scout, Smuggler, Deep Explorer |

### 36. Meditation Drive

| Field | Value |
|-------|-------|
| ID | `communion_meditation_drive` |
| Display Name | Meditation Drive |
| Faction / Rep Tier | Communion / Friendly |
| Slot Kind | Drive |
| Credit Cost | 23,000 |
| Stats | SpeedBonusPct: +5%, FractureCooldownReductionPct: -30%, PowerDraw: 6 |
| Sustain | 1 Exotic Crystals / 60 ticks |
| Special Effect | Fracture drive cooldown reduced by 30%. Hull stress from fracture travel reduced by 40%. The drive harmonizes with the fracture module, smoothing metric transitions. Lane transit is only slightly faster (+5%), but off-thread travel becomes dramatically more sustainable. A player with Meditation Drive can make 3 fracture jumps in the time others make 2, and arrive with more hull integrity. Also reduces instability-based sensor jitter by 50% in Phase 1-2 space. |
| Lore | "The Communion drive does not fight the current. It reads the current and adjusts the hull's frequency to match. The pilot barely feels the transition." |
| Build Identity | Deep Explorer, Fracture Specialist |

### 37. Void Sense

| Field | Value |
|-------|-------|
| ID | `communion_void_sense` |
| Display Name | Void Sense Array |
| Faction / Rep Tier | Communion / Allied |
| Slot Kind | Utility |
| Credit Cost | 38,000 |
| Stats | ScanRangeBonusPct: +40%, DiscoveryRangeBonusHops: +3, PowerDraw: 7 |
| Sustain | 2 Exotic Crystals / 60 ticks |
| Special Effect | Extends discovery scan range by 3 additional hops. Reveals void site locations within extended range, even through unexplored space. In Phase 2+ space, automatically detects anomaly types before the player arrives (shows "Derelict," "Lattice Node," or "Accommodation Ruin" on the galaxy map). In Phase 3+ space, reveals adaptation fragment locations within 5 hops. The player sees further into the unknown than any other build can achieve. |
| Lore | "The Communion elder said: 'You are not looking further. You are looking the same distance everyone does. You are simply seeing what was always there.'" |
| Build Identity | Deep Explorer, Knowledge Seeker |

### 38. Phase Attunement

| Field | Value |
|-------|-------|
| ID | `communion_phase_attunement` |
| Display Name | Phase Attunement Regulator |
| Faction / Rep Tier | Communion / Friendly |
| Slot Kind | Utility |
| Credit Cost | 20,000 |
| Stats | PowerDraw: 4 |
| Sustain | 1 Exotic Crystals / 60 ticks |
| Special Effect | Hull stress from operating in Phase 2+ space reduced by 50%. The instability-based damage-over-time that normally affects ships in Fracture space is halved. Additionally, metric bleed UI effects (price jitter, sensor flutter, phantom manifests) are dampened — the player's instruments remain more readable in unstable space. In Phase 4 (Void) space, provides a +20% bonus to all scan results (better data quality from void sites). |
| Lore | "Phase attunement is not a technology. It is a Communion meditation practice translated into circuitry. The circuit does not understand. It merely does what the meditator does." |
| Build Identity | Deep Explorer, Void Specialist |

### 39. Resonance Receiver

| Field | Value |
|-------|-------|
| ID | `communion_resonance_receiver` |
| Display Name | Resonance Receiver |
| Faction / Rep Tier | Communion / Allied |
| Slot Kind | Special |
| Credit Cost | 42,000 |
| Stats | PowerDraw: 8 |
| Sustain | 2 Exotic Crystals / 60 ticks |
| Special Effect | Enables "Commune" interactions at Communion waystation nodes. Communing takes 30 ticks and reveals: (1) the location of the nearest undiscovered void site, (2) the current instability trajectory for all systems within 3 hops, (3) a fragment of lore about the ancient civilization's relationship with the local spacetime topology. Each Commune interaction also grants +3 Communion reputation. Can only Commune at each waystation once per 200 ticks. Communing in Phase 2+ space has a 20% chance of revealing an adaptation fragment hint. |
| Lore | "The Receiver does not receive signals. It receives the absence of signals — the pattern in the silence. The Communion has been listening to this silence for generations." |
| Build Identity | Knowledge Seeker, Communion Devotee |

### 40. Frequency Emitter

| Field | Value |
|-------|-------|
| ID | `communion_frequency_emitter` |
| Display Name | Frequency Emitter |
| Faction / Rep Tier | Communion / Allied |
| Slot Kind | Special |
| Credit Cost | 48,000 |
| Stats | PowerDraw: 10 |
| Sustain | 3 Exotic Crystals / 60 ticks |
| Special Effect | Endgame module. When activated at a Phase 4 (Void) site with all 12 adaptation fragments collected, initiates the "Transmission" — a signal broadcast into the metric that triggers the Renegotiate endgame path's final sequence. Outside of endgame use: passively broadcasts a low-frequency harmonic that reduces instability gain rate by 15% within 200u. Allied Communion NPCs within range gain +20% scan range. The emitter is both a practical exploration tool and the key to the game's deepest ending. |
| Lore | "The Communion has been waiting to send this message since before they knew they were waiting. You carry the words. The Emitter carries the voice." |
| Build Identity | Endgame Pathfinder, Communion Devotee |

---

## Build Identity Index

| Build Identity | Modules That Enable It | Core Fantasy |
|---|---|---|
| Fleet Commander | Diplomatic Shield, Fleet Command Relay, Hivemind Targeting | "My allies fight better because I'm here" |
| Trade Magnate | Institutional Drive, Logistics Optimizer, Audit Scanner, Regulatory Transponder | "I see every margin and exploit all of them" |
| Info Broker | Audit Scanner, Data Siphon, Signal Broker, Probability Engine | "Information is my product" |
| Adaptive Fighter | Metamorphic Cannon, Adaptive Carapace | "Every hit teaches my ship" |
| Swarm Tactician | Hivemind Targeting, Swarm Coordinator | "Quantity has a quality all its own" |
| Deep Explorer | Phase-Shift Drive, Meditation Drive, Void Sense, Phase Attunement, Crystal Hull Coating | "I go where no one else can survive" |
| Haven Builder | Construction Drones, Resonance Forge, Load-Bearing Frame, Structural Reinforcement | "I build the future" |
| Endurance Tank | Adaptive Carapace, Composite Layering, Crystal Hull Coating | "You run out of ammunition before I run out of hull" |
| Glass Cannon | Kinetic Accelerator, Raid Drive, Echo Drive | "First strike, every time" |
| Raider | Raid Drive, Kill-Mark Targeter, Salvage Rights Scanner, Conscription Beacon | "Hit hard, take everything, leave fast" |
| Warfront Commander | Fleet Command Relay, Kill-Mark Targeter, Garrison Beacon, Conscription Beacon | "I decide where the front line is" |
| Knowledge Seeker | Resonance Receiver, Void Sense, Communion Devotee modules | "I understand what the galaxy is" |
| Smuggler | ECM Suite, Silence Field | "Unseen, untracked, untaxed" |
| Solo Explorer | Composite Layering, Tractor Fabricator, Crystal Hull Coating | "Self-sufficient in hostile space" |
| Support Specialist | Welding Beam, Point Defense Matrix, Diplomatic Shield | "I keep the fleet alive" |

---

## Cross-Faction Synergy Notes

The module system is designed so that the most powerful builds combine modules from 2-3 factions, forcing the player to maintain multiple faction relationships:

- **Deep Explorer**: Communion Meditation Drive + Communion Void Sense + Chitin Phase-Shift Drive = maximum fracture range and perception, but requires Allied Chitin AND Allied Communion
- **Ultimate Commander**: Concord Fleet Command Relay + Valorin Conscription Beacon + Chitin Hivemind Targeting = massive fleet with coordinated fire, requires 3 faction Allied standings
- **Immortal Hauler**: Weaver Structural Reinforcement + Weaver Composite Layering + Communion Crystal Hull Coating = a ship that never needs dock repairs, but has Weaver speed penalties
- **Intel Overlord**: Concord Audit Scanner + Chitin Probability Engine + Weaver Thread-reading (station bonus) = perfect market information, making every trade optimal

No single faction provides a complete build. The pentagon dependency expressed through modules.
