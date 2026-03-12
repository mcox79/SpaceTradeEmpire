# Player-Visible Systems — Current State

What a player should see and interact with right now, based on DONE epics/gates.
Used by `/screenshot eval` and manual playtesting to verify nothing has regressed.

**Rules:**
- Update when epics move to DONE or IN_PROGRESS
- Each item has a **Verify** hint: what to look for in screenshots or bot runs
- Items marked (IN_PROGRESS) are partially built — note what's expected vs not-yet

Last updated: 2026-03-11 (post Tranche 29)

---

## Flight & Navigation

| System | Verify | Source Epic |
|--------|--------|------------|
| Hero ship — physics flight, persistent rotation, flame trail | Ship model visible (scale 0.7) with thruster VFX during movement | S1.HERO_SHIP_LOOP |
| Top-down camera (Phantom Camera) with combat shake | Camera follows player, shakes on turret fire/damage | S1.CAMERA_POLISH |
| Real-space galaxy — continuous space, no loading screens | Flying between objects is seamless within a system | S17.REAL_SPACE |
| Physical thread traversal between systems | Player flies along thread geometry to reach gate | S17.REAL_SPACE |
| Warp tunnel VFX — dual-layer tunnel with 3 particle layers | Dramatic warp tunnel: color-shifting walls, streaking stars, speed lines, pulsating emission | S17.REAL_SPACE + S7.RUNTIME_STABILITY |
| Warp arrival — camera zooms to altitude 80 at destination | Camera properly resets after flyby cinematic, no stuck-at-galactic-altitude | S7.RUNTIME_STABILITY |
| Gate vortex models (Kenney Space Kit) | 3D gate mesh at thread endpoints | S14.ALIVE_GALAXY |
| Gate transit popup on arrival | Toast or popup confirming arrival at destination | S14.ALIVE_GALAXY |
| Galaxy map (Tab key) — high-altitude camera | Zooms out to show star nodes, threads, player indicator. Blocked while docked. | S1.GALAXY_MAP_PROTO |
| Galaxy map overlays (F/L/H/E/W) | Faction territory (F), Fleet positions (L), Heat (H), Exploration (E), Warfront (W) colored discs | S7.GALAXY_MAP_V2 |
| Galaxy map route planner (Shift+Click) | Green polyline path + waypoint markers + travel time label. Escape cancels | S7.GALAXY_MAP_V2 |
| Galaxy map search (Ctrl+F) | Search bar with partial-match results, Enter/click snaps camera to node | S7.GALAXY_MAP_V2 |
| Galaxy map semantic zoom — 3 altitude bands | Close <500u full detail, Medium 500-2000u names+faction, Galaxy >2000u minimal dots | S7.GALAXY_MAP_V2 |
| Camera altitude bounds (max 3000u) | Camera cannot zoom beyond local system range; auto-clamps to PAN_THRESHOLD on warp arrival | X.UI_POLISH |
| Galaxy map full topology (altitude 2500u) | All star nodes and threads visible at galaxy map altitude; labels cleaned up on mode exit | X.UI_POLISH |
| Label anti-collision — vertical stacking offset | Overlapping Label3D names (stations, gates) offset vertically instead of overlapping | X.UI_POLISH |
| Star-class lighting — each star tints its local system | Different colored ambient light per star type | S15.EXPLORATION_FEEL |
| Procedural skybox (Starlight addon) | Nebula/star background, not solid black | S1.VISUAL_UPGRADE |
| 3D planets with atmosphere shaders | Spheres with visible atmosphere glow near stars | S1.VISUAL_UPGRADE |
| Engine trail — always-on idle emission | Engine trail particles emit at low level even when stationary (no dead particles) | S7.RUNTIME_STABILITY |

## Combat

| System | Verify | Source Epic |
|--------|--------|------------|
| Real-time hero combat — shields/hull, turrets, missiles | Player can fire at NPCs, damage is dealt | S5.COMBAT_LOCAL |
| Point defense counter family | PD weapons intercept missiles | S5.COMBAT_DOCTRINE |
| Kill explosion VFX — fireball (scale 12) + debris + smoke | Large visible explosion when NPC ship destroyed | S7.COMBAT_JUICE |
| Shield ripple shader on hit (radius 14) | Blue/white ripple on shield surface when hit, visible at altitude 80 | S7.COMBAT_JUICE + S7.RUNTIME_STABILITY |
| Shield break flash (radius 18) + discharge particles | Flash + 30 particles when shields reach 0 | S7.COMBAT_JUICE + S7.RUNTIME_STABILITY |
| Floating damage numbers (font 96-160, pixel_size 0.12) | Large numeric values float up from hit location, visible at game altitude | S7.COMBAT_JUICE + S7.RUNTIME_STABILITY |
| Weapon trail differentiation by damage family | Kinetic/Energy/Neutral/PD have distinct colors and trail shapes | S7.COMBAT_FEEL_POLISH |
| Bullet trails (scale 2.5x, emission 5) | Projectile visuals visible at default camera distance | S7.RUNTIME_STABILITY |
| Screen shake — intensity-scaled | Camera shake on fire and damage events | S7.COMBAT_JUICE |
| Combat audio — spatial 3D (turret fire, bullet hit, explosion) | Positional SFX audible during combat | S1.SPATIAL_AUDIO_DEPTH |
| Combat loot drops — cargo + salvage from destroyed ships | Loot items appear after kills, tractor beam pickup | S5.COMBAT_LOOT |
| Combat log panel | Event feed showing combat actions | S11.GAME_FEEL |
| NPC ships at scale 2.0 with tightened patrol radius (15-45) | NPC ships large enough to see at game altitude, clustered closer to systems | S7.RUNTIME_STABILITY |

## Economy & Trade

| System | Verify | Source Epic |
|--------|--------|------------|
| 13 trade goods with geographic distribution | Market shows varied goods per station type | S18.TRADE_GOODS |
| 9 production recipes across industry sites | PRODUCTION section visible in dock market tab | S7.PRODUCTION_CHAINS |
| Buy/sell UI with quantity controls + instant cargo refresh | +/- buttons on market rows, trade toasts, cargo updates immediately after buy/sell | S12.UX_POLISH + S7.RUNTIME_STABILITY |
| NPC trade circulation — autonomous traders | NPC trader fleets moving between systems | S5.NPC_TRADE |
| Market view at docked stations (per-node inventory) | Dock market tab shows buy/sell prices, stock, supply | S1.PLAYABLE_BEAT |
| Sustain enforcement — fleet fuel drain, module sustain | Fuel consumption visible in fleet status | S7.SUSTAIN_ENFORCEMENT |
| Instability effects — price jitter, trade failure, closure | Market behavior changes in unstable systems | S7.INSTABILITY_EFFECTS |
| Instability-aware pricing in buy/sell | BuyCommand/SellCommand use GetEffectivePrice with volatility/jitter/void-closure modifiers | X.INSTABILITY_PRICE_WIRING |
| Ship class cargo capacity enforcement | Buy/pickup rejected when fleet cargo exceeds ship class CargoCapacity | X.SHIP_CLASS_ENFORCEMENT |
| Module sustain goods consumption | Weapons consume munitions, T3 modules consume exotic matter, scanners consume energy cells per sustain cycle | X.MODULE_SUSTAIN_GOODS |
| Price bands and spreads per good | Different goods have different price ranges | S18.TRADE_GOODS |

## Fleet & Empire Management

| System | Verify | Source Epic |
|--------|--------|------------|
| Empire Dashboard (E key) — modal panel | Opens with Overview, Research, Stats, Warfronts tabs | S18.EMPIRE_DASH |
| 5-tab dock menu — Market, Jobs, Ship, Station, Intel | All tabs accessible when docked | S18.EMPIRE_DASH |
| Fleet roles — Trader/Hauler/Patrol with role-based 3D models | Different Quaternius ship models per role | S12.FLEET_SUBSTANCE |
| Programs — AutoSell, TradeCharter, ResourceTap, Expedition, etc. | Program creation and assignment UI | S10.TRADE_DISCOVERY |
| Fleet tab (F3) — master-detail fleet list | Per-fleet list view with cargo, modules, programs, status | S7.FLEET_TAB |
| Module fitting — 8 ship classes, 34 modules (15 T1 + 19 T2), zone armor, power budget | Ship tab shows slots, power, zone HP bars | S18.SHIP_MODULES + S7.POWER_BUDGET + S7.T2_MODULE_CATALOG |
| Mass-based speed penalty | Heavier ships move slower proportional to loaded mass vs ship class Mass stat | X.SHIP_CLASS_ENFORCEMENT |
| Scan range gating | Discovery scanning range limited by ship class ScanRange stat and equipped scanner modules | X.SHIP_CLASS_ENFORCEMENT |
| Refit system — upgrade pipeline with yard capacity | REFIT button in Ship tab, time-based upgrades | S4.UPGRADE_PIPELINE |
| Milestone/progression system | Dashboard integration for player milestones | S12.UX_POLISH |
| NPC circuit routes with flow visualization + trade volume labels | Animated route lines with volume text | S12.FLEET_SUBSTANCE |
| Automation dashboard — program performance, failures, budget, doctrine | PanelContainer showing cycles, goods moved, credits, failure tracking, budget caps, doctrine stance | S7.AUTOMATION_MGMT |
| Doctrine system — engagement stance, retreat threshold, patrol radius | FleetDoctrine settings per fleet (Aggressive/Defensive/Evasive) | S7.AUTOMATION_MGMT |
| Program metrics — cycle tracking, success/failure rates | Per-fleet automation metrics: cycles run, goods moved, failures, history | S7.AUTOMATION_MGMT |
| Budget enforcement — per-cycle credit/goods caps | Spending limits on automated programs | S7.AUTOMATION_MGMT |
| Failure recovery — auto-retry with exponential backoff | Consecutive failure tracking, reason codes (InsufficientFunds, NoRoute, TargetGone, Timeout, BudgetExceeded) | S7.AUTOMATION_MGMT |

## Factions & Diplomacy

| System | Verify | Source Epic |
|--------|--------|------------|
| 5 factions with territories, doctrines, tariffs | Faction names and territories on galaxy map | S2_5.WGEN.FACTION |
| Faction-specific ship liveries/tints | NPC ships tinted by faction color | S7.FACTION_VISUALS |
| Faction station aesthetic differentiation | Stations look different per faction | S7.FACTION_VISUALS |
| UI color themes for faction contexts | Faction-colored UI headers/accents | S7.FACTION_VISUALS |
| HUD tints when in faction territory | Screen edge color shift by faction ownership | S7.FACTION_VISUALS |
| Faction territory overlay on galaxy map (F key) | Colored discs showing territorial control + influence strength | S7.GALAXY_MAP_V2 |
| Faction greeting on dock | Faction-specific greeting text in station menu, colored by faction | S7.NARRATIVE_DELIVERY |
| Reputation-driven access — dock/trade/tech gating | Access denied or tariffs at hostile factions | S7.REPUTATION_INFLUENCE |
| Warfront theaters — contested nodes from geography | Warfront markers on galaxy map | S7.WARFRONT_THEATERS |
| Warfront overlay on galaxy map (W key) | Red intensity discs showing active combat zones, disputed territory | S7.GALAXY_MAP_V2 |
| Faction territory labels on galaxy map | Text labels showing faction ownership | S15.EXPLORATION_FEEL |
| Embargo system + tariff scaling | Trade restrictions at hostile factions | S7.TERRITORY_REGIMES |

## Exploration & Discovery

| System | Verify | Source Epic |
|--------|--------|------------|
| Discovery sites — seen/scanned/analyzed states | Sites show discovery phase in dock panel | S3_6.DISCOVERY_STATE |
| Discovery site dock interaction panel | Panel with site_id, phase, undock button | S1.DISCOVERY_INTERACT |
| Anomaly families, corridor traces, resource pools | Different discovery types seeded per system | S2_5.WGEN.DISCOVERY_SEEDING |
| Unlock system — Permit, Broker, Recipe, etc. | Unlocks awarded from discovery analysis | S3_6.DISCOVERY_UNLOCK |
| Rumor/Intel substrate — lore leads | Intel entries from exploration and hub analysis | S3_6.RUMOR_INTEL_MIN |
| Expedition programs — survey, sample, salvage, analyze | Assignable to fleets | S3_6.EXPEDITION_PROGRAMS |
| Exploitation packages — TradeCharter, ResourceTap | Deployable from discovery unlocks | S3_6.EXPLOITATION_PACKAGES |
| Exploration overlay on galaxy map (E key) | Colored discs: unvisited=gray, visited=white, mapped=green, anomaly=purple | S7.GALAXY_MAP_V2 |
| Discovery template text on discoveries | Flavor text from narrative template system | S7.NARRATIVE_DELIVERY |
| Fracture travel — off-thread jumps with Trace risk | Fracture travel panel with cost/risk info; gated behind fracture unlock | S6.OFFLANE_FRACTURE |
| Fracture discovery gating — derelict encounter unlocks fracture drive | Fracture drive unavailable until frontier derelict surveyed (~tick 300+). "FRACTURE DRIVE UNLOCKED" critical toast on unlock. Derelict analysis progress shown in DiscoverySitePanel | S6.FRACTURE_DISCOVERY_EVENT |
| Jump events — salvage/signal/turbulence on transit | Toast notifications during thread travel | S15.EXPLORATION_FEEL |

## Risk & Enforcement

| System | Verify | Source Epic |
|--------|--------|------------|
| Heat/Influence/Trace risk model | Three risk dimensions tracked in sim | S3.RISK_MODEL |
| Security events — delay, loss, inspection | Events fired during thread travel | S5.SECURITY_LANES |
| Enforcement escalation — heat accumulation, confiscation, fines | Pattern-based heat from volume/route/counterparty | S7.ENFORCEMENT_ESCALATION |
| Heat decay window | Heat reduces over time when not accumulating | S7.ENFORCEMENT_ESCALATION |
| Risk meter widgets in Zone G | Heat/Influence/Trace bars in bottom HUD bar | S7.RISK_METER_UI |
| Screen-edge tint at High+ threat | Ambient red/orange glow at screen edges when risk is elevated | S7.RISK_METER_UI |
| Compound risk meter — combined threat level | Aggregate risk indicator summarizing all three meters | S7.RISK_METER_UI |

## HUD & UI

| System | Verify | Source Epic |
|--------|--------|------------|
| Zone-based HUD layout (Zones A-G) | Status in A (top-left), alerts in B, etc. | S7.HUD_ARCHITECTURE |
| Toast notifications — 4 visible max, proper spacing, fade-out | Toasts appear top-right, color-coded by severity, 10px margins, graceful fade | S7.HUD_ARCHITECTURE + S7.RUNTIME_STABILITY |
| Toast action bridges — clickable actions | Some toasts have clickable buttons | S7.HUD_ARCHITECTURE |
| Progressive disclosure — Tier 1/2/3 info density | Basic info always shown, detail on hover/focus | S7.HUD_ARCHITECTURE |
| Alert badge in Zone A | Badge indicator for pending alerts | S7.HUD_ARCHITECTURE |
| Zone G bottom bar framework | Bottom bar slot for risk meters and minimap | S7.HUD_ARCHITECTURE |
| Narrative text panel — faction-colored, 5s auto-hide | RichTextLabel popup for narrative moments with faction border/text color, click dismiss | S7.NARRATIVE_DELIVERY |
| Keybindings help overlay (H key) | Overlay showing all keybindings | S11.GAME_FEEL |
| Hostile labels on NPC ships (visible at 120u) | Red text labels on hostile NPCs, enlarged font for altitude visibility | S13.FEEL_OVERHAUL + S7.RUNTIME_STABILITY |
| Node detail popup | Popup with system/node info on click | S11.GAME_FEEL |
| Galaxy overlay HUD | Overlay controls for galaxy map view | S10.EMPIRE_MGMT |
| Tech tree UI | Research tree visualization | S11.GAME_FEEL |
| First Officer panel — promotion, dialogue history, reactions | HUD panel with: 3 candidate cards at promotion window (choose FO type), 5-line scrollable dialogue history, FO name/tier/score display, gold "fo" toasts for FO comments | T18.CHARACTER_SYSTEMS |
| FO contextual triggers — score-based tier advancement | FO dialogue advances via RelationshipScore thresholds (not just ticks): SUPPLY_CHAIN_NOTICED after 3 missions, KNOWLEDGE_WEB_INSIGHT after 3 connections, faction-aware recontextualization variants | T18.CHARACTER_SYSTEMS |
| Data log viewer (L key) — list, detail, search, thread filter | Panel listing discovered logs with [NEW] indicators, detail view with BBCode, search filter. Thread filter buttons to filter logs by narrative thread (e.g. Kepler Chain) | T18.DATA_LOG_CONTENT |
| Knowledge Web panel (K key) — connection graph | Panel showing discovered knowledge connections between lore entries. Nodes + edges visualize how narrative threads interconnect | T18.DATA_LOG_CONTENT |
| War Faces NPCs — alive dialogue + ghost mentions | FO panel NPC section shows alive NPCs with location-aware dialogue. Dead Regular NPCs show ghost mentions ("Someone left flowers at docking bay 7..."). Enemy NPCs show recontextualization text based on player post-interdiction actions (priority-based variant selection) | T18.CHARACTER_SYSTEMS |

## NPC Life

| System | Verify | Source Epic |
|--------|--------|------------|
| LimboAI behavior trees — Trader/Hauler/Patrol AI | NPCs move autonomously with role-based goals | S16.NPC_SHIPS_ALIVE |
| NPC fleet ships at scale 2.0 as physical 3D entities | Large 3D ship models visible at game camera altitude | S16.NPC_SHIPS_ALIVE + S7.RUNTIME_STABILITY |
| Warp-in/out effects on NPC fleet ships | Visual effect when NPCs enter/leave systems | S16.NPC_SHIPS_ALIVE |
| Fleet destruction + respawn | NPCs can be destroyed and eventually respawn | S16.NPC_SHIPS_ALIVE |
| NPC HP bars (scale 5.0x0.4, emission 3.0) | Health bars visible at game altitude | S7.RUNTIME_STABILITY |
| NPC role labels (font 48, pixel_size 0.05) | Role text labels readable at game altitude | S7.RUNTIME_STABILITY |
| NPC freighter proximity substantiation | Freighters appear when player is nearby | S15.EXPLORATION_FEEL |
| Quaternius ship models matched to fleet roles | Different 3D models per fleet role | S12.FLEET_SUBSTANCE |

## Audio

| System | Verify | Source Epic |
|--------|--------|------------|
| 5-layer audio bus — Music/Ambient/SFX/UI/Alert | Separate volume channels with ducking | S7.AUDIO_WIRING |
| Engine thrust AudioStreamRandomizer | Varied thrust sounds during flight | S1.SPATIAL_AUDIO_DEPTH |
| Positional combat SFX (fire/impact) | Spatial audio from combat events | S1.SPATIAL_AUDIO_DEPTH |
| Station hum (ambient) | Hum audible when near stations | S1.SPATIAL_AUDIO_DEPTH |
| Thread drone (ambient) | Ambient sound near/on trade threads | S1.SPATIAL_AUDIO_DEPTH |
| Dock chimes | Chime on docking | S1.AUDIO_MIN |
| Warp whoosh | Sound effect on warp transit | S1.AUDIO_MIN |
| Discovery phase transition chimes | Chime when discovery advances phase | S7.AUDIO_WIRING |
| Risk threshold alert sounds | Alert sound at risk threshold crossings | S7.AUDIO_WIRING |

## Save/Load & Menus

| System | Verify | Source Epic |
|--------|--------|------------|
| Pause menu (Escape) | Pause overlay with save/resume/quit | S1.SAVE_LOAD_UI |
| 3 save slots with metadata | Slot selection with playtime/system info | S1.SAVE_LOAD_UI |
| Mission runner — 7 missions with prerequisite chains | Tutorial mission (Matched Luggage), then unlocks: Mining Survey, Ore Extraction, First Research, Research Materials, First Build, Station Expansion. Each requires completing prior mission(s) | S1.MISSION_RUNNER + S9.MISSION_LADDER |
| Mission rewards preview in dock | Dock Jobs tab shows credit reward and step count for available missions before accepting | S9.MISSION_LADDER |
| Mission prerequisites detail in dock | Dock Jobs tab shows which prerequisite missions are needed and whether they're completed, explaining why locked missions are unavailable | S9.MISSION_LADDER |
| Main menu as project start scene | main_menu.tscn with new voyage wizard | S7.MAIN_MENU |
| Menu starfield shader — 4-layer parallax background | Deep stars, nebula noise x2, mid-field stars drifting behind menu | S9.MENU_ATMOSPHERE |
| Title treatment — fade-in + rotating Precursor subtitle | "Space Trade Empire" fades in, subtitle cycles from phrase pool per session | S9.MENU_ATMOSPHERE |
| Adaptive foreground silhouette | 3D ship/gate model in SubViewport, selected by save state (gate=new, ship=mid, haven=end) | S9.MENU_ATMOSPHERE |
| Menu audio timing | First-launch: 2s silence then swell; returning: quick fade-in | S9.MENU_ATMOSPHERE |
| Galaxy generation loading screen | Thematic progress messages ("Charting the void...") with progress bar before game start | S9.MENU_ATMOSPHERE |
| Captain name input | Name entry persisted in SimState | S7.MAIN_MENU |
| Auto-save on dock/warp/mission | Automatic save slot triggers with toast notification | S7.MAIN_MENU |
| Difficulty selection | DifficultyTweaksV0 multipliers on new voyage | S7.MAIN_MENU |

## Accessibility

| System | Verify | Source Epic |
|--------|--------|------------|
| First-launch accessibility prompt | Shown once on first boot (no settings file): font size, colorblind mode selectors | S9.ACCESSIBILITY |
| Colorblind post-processing shader | Deuteranopia/Protanopia/Tritanopia modes via CanvasLayer shader | S9.ACCESSIBILITY |
| Font size override (100-200%) | ThemeDB.fallback_base_scale applied globally from settings | S9.ACCESSIBILITY |
| High contrast mode toggle | High contrast UI option in Accessibility settings tab | S9.ACCESSIBILITY |
| Reduced screen shake toggle | Disables camera shake for motion-sensitive players | S9.ACCESSIBILITY |
| Accessibility tab in Settings panel | Colorblind dropdown, font scale slider, high contrast toggle, reduced shake toggle | S9.SETTINGS |
| Display revert timer (15s) | "Keep changes?" dialog after resolution/mode change, auto-reverts on timeout | S9.SETTINGS |

## Ambient & Polish

| System | Verify | Source Epic |
|--------|--------|------------|
| Asteroid variety near nodes | Multiple asteroid meshes/sizes near stations | S14.ALIVE_GALAXY |
| Ambient particles in space | Dust/particle effects in flight | S15.EXPLORATION_FEEL |
| Starter star guarantee | Home system always has a visible star | S14.ALIVE_GALAXY |
| Label distance clamping | Labels fade/shrink at extreme distances | S13.FEEL_OVERHAUL |
| Kenney Space Kit station models | 3D station meshes at dockable nodes | S14.ALIVE_GALAXY |
| Tighter local system density | Planets ~40% closer to star, stations 4u from planet (was 8u), belt at 28u (was 45u), lane gates at 55u (was 90u). Systems feel populated, not barren | X.UI_POLISH |
| Dock visual polish — sell column + ship tab | Sell column properly aligned in market tab, ship tab visual cleanup | X.UI_POLISH |
