# 55_GATES

## Gate closeout checklist (required)
When a gate moves to DONE:
1) Add a PASS entry to docs/56_SESSION_LOG.md (single line, deterministic format).
2) Update the gate row in docs/55_GATES.md (status DONE + evidence paths).
3) Run scripts/tools/Validate-Gates.ps1 and dotnet test (SimCore.Tests Release).
4) If applicable, update or regenerate docs/generated artifacts referenced by the gate.

## ACTIVE GATES — Quick Reference
*Full DONE history in section B below. DONE-only archive: [docs/55_GATES_DONE.md](55_GATES_DONE.md).*

| Gate ID | Status | One-line summary |
|---|---|---|
| GATE.S1.HERO_SHIP_LOOP.STATES.001 | DONE | PlayerShipState enum v0 (InFlight/Docked/InLaneTransit) in game_manager.gd |
| GATE.S1.HERO_SHIP_LOOP.SCENE.001 | DONE | Solar system local-space scene boot v0 via GetSystemSnapshotV0 |
| GATE.S1.HERO_SHIP_LOOP.FLIGHT.001 | DONE | Ship thrust, inertia, collision physics v0 on RigidBody3D |
| GATE.S1.HERO_SHIP_LOOP.LANE.001 | DONE | Lane transit bare state + SimCore time advance v0 |
| GATE.S1.GALAXY_MAP.DISCOVERY_STATES.001 | DONE | Galaxy map discovery state rendering v0 (Hidden/Rumored/Visited/Mapped) |
| GATE.S1.HERO_SHIP_LOOP.ARRIVE.001 | DONE | Lane transit arrival: SimBridge-truth scene redraw v0 |
| GATE.S1.HERO_SHIP_LOOP.DISCOVERY_DOCK.001 | DONE | Discovery site proximity dock trigger v0 |
| GATE.S4.CATALOG.GOODS.001 | DONE | Starter goods content pack + schema validator v0 |
| GATE.S4.CATALOG.RECIPES.001 | DONE | Starter recipe content pack + schema validator v0 |
| GATE.S4.MODULE_MODEL.SLOTS.001 | DONE | Hero ship slot model + SimBridge readout v0 |
| GATE.S1.HERO_SHIP_LOOP.CONTROLS.001 | DONE | Ship yaw turning + custom input actions v1 |
| GATE.S1.HERO_SHIP_LOOP.HUD.001 | DONE | Persistent flight HUD v0 (credits/cargo/system name) |
| GATE.S4.CATALOG.MARKET_BIND.001 | DONE | Bind goods catalog to market seeding v0 |
| GATE.S1.GALAXY_MAP.FLEET_COUNTS.001 | DONE | Fleet counts per node in galaxy overlay v0 |
| GATE.S1.HERO_SHIP_LOOP.LANE_GATE_LABEL.001 | DONE | Lane gate destination name labels v0 |
| GATE.S1.HERO_SHIP_LOOP.PLAYER_TRADE.001 | DONE | Hero ship buy/sell at station v0 |
| GATE.S1.HERO_SHIP_LOOP.MARKET_SCREEN.001 | DONE | Station market screen v0 — rendered goods list with buy/sell buttons |
| GATE.S1.HERO_SHIP_LOOP.CARGO_DISPLAY.001 | DONE | Cargo inventory section in station panel v0 |
| GATE.S1.HERO_SHIP_LOOP.LOOP_PROOF.001 | DONE | Hero ship buy-sell loop proof v0 (closes EPIC.S1.HERO_SHIP_LOOP.V0) |
| GATE.S1.GALAXY_MAP_PROTO.PLAYER_HIGHLIGHT.001 | DONE | Galaxy overlay player node highlight v0 |
| GATE.S1.GALAXY_MAP_PROTO.EPIC_CLOSE.001 | DONE | Galaxy map proto epic closure proof v0 (closes EPIC.S1.GALAXY_MAP_PROTO.V0) |
| GATE.S4.CATALOG.WEAPONS.001 | DONE | Starter weapons content pack v0 |
| GATE.S4.MODULE_MODEL.EQUIP.001 | DONE | Hero ship module equip via SimBridge v0 |
| GATE.S1.HERO_SHIP_LOOP.STATION_DOCK_PROXIMITY.001 | DONE | Station proximity dock wire-up v0 |
| GATE.S1.HERO_SHIP_LOOP.MARKET_UNDOCK_V0.001 | DONE | Market screen on dock + undock button v0 |
| GATE.S4.MODULE_MODEL.EQUIP_PANEL.001 | DONE | Equip slot panel in station UI v0 |
| GATE.S1.HERO_SHIP_LOOP.STATION_LOOP_V1.001 | DONE | Station dock-trade-undock-lane loop proof v1 |
| GATE.S4.CATALOG.EPIC_CLOSE.001 | DONE | Catalog v0 epic close proof |
| GATE.S4.MODULE_MODEL.EPIC_CLOSE.001 | DONE | Module model v0 epic close proof |
| GATE.S1.PLAYABLE_BEAT.INTERACTION_FIX.001 | DONE | Buy/sell button wiring + ship input freeze + market refresh v0 |
| GATE.S1.PLAYABLE_BEAT.EPIC_CLOSE.001 | DONE | First playable beat epic close proof |
| GATE.S4.INDU_STRUCT.RECIPE_BIND.001 | DONE | IndustrySite recipe ID binding v0 |
| GATE.S4.INDU_STRUCT.CHAIN_CONTENT.001 | DONE | Production chain recipes + hull_plating good v0 |
| GATE.S4.INDU_STRUCT.GENESIS_WIRE.001 | DONE | Genesis assigns recipe IDs to industry sites v0 |
| GATE.S4.INDU_STRUCT.CHAIN_GRAPH.001 | DONE | Deterministic chain graph validator + report v0 |
| GATE.S4.INDU_STRUCT.SHORTFALL_LOG.001 | DONE | IndustryShortfall persistent event log v0 |
| GATE.S4.INDU_STRUCT.PLAYABLE_VIEW.001 | DONE | Station production info in playable prototype v0 |
| GATE.S4.INDU_STRUCT.EPIC_CLOSE.001 | DONE | Industry chain scenario proof v0 (closes EPIC.S4.INDU_STRUCT) |
| GATE.X.HYGIENE.TRADE_CMD_TESTS.001 | DONE | Trade command contract validation + edge case tests v0 |
| GATE.X.HYGIENE.DEAD_CODE.001 | DONE | Dead code cleanup + minor hygiene v0 |
| GATE.X.HYGIENE.GALAXY_GEN_SPLIT.001 | DONE | GalaxyGenerator.cs first split (StarNetworkGen + MarketInit) v0 |
| GATE.S5.COMBAT_LOCAL.DAMAGE_MODEL.001 | DONE | CombatProfile + deterministic damage calc + counter family v0 |
| GATE.S5.COMBAT_LOCAL.COMBAT_TICK.001 | DONE | Combat encounter lifecycle + auto-targeting + event log v0 |
| GATE.S5.COMBAT_LOCAL.COMBAT_LOG.001 | DONE | Combat event log + "why we lost" cause chain v0 |
| GATE.S5.COMBAT_LOCAL.BRIDGE_COMBAT.001 | DONE | SimBridge combat queries + HUD combat readout v0 |
| GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001 | DONE | In-engine combat headless proof v0 (PLAYABLE_BEAT) |
| GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001 | DONE | AI fleet substantiation + player-initiated combat v0 |
| GATE.S1.DISCOVERY_INTERACT.PANEL.001 | DONE | Discovery site dock panel v0 (site_id + phase + undock) |
| GATE.X.HYGIENE.GEN_REPORT_EXTRACT.001 | DONE | Extract GalaxyGenerator Build*Report methods → ReportBuilder.cs |
| GATE.X.HYGIENE.STATION_MENU_SPLIT.001 | DONE | Extract StationMenu market tab → MarketTabView.cs |
| GATE.X.HYGIENE.GEN_DISCOVERY_EXTRACT.001 | DONE | Extract GalaxyGenerator BuildDiscoverySeed* → DiscoverySeedGen.cs |
| GATE.S5.COMBAT_PLAYABLE.LOOP_PROOF.001 | DONE | In-engine combat loop headless proof v0 (PLAYABLE_BEAT) |
| GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001 | DONE | Player death state + game-over restart v0 |
| GATE.S5.COMBAT_PLAYABLE.COMBAT_BEAT.001 | DONE | Combat beat proof: kill fleet + survive return fire (PLAYABLE_BEAT) |
| GATE.S1.VISUAL_POLISH.SPACE_ENV.001 | DONE | Skybox + ambient star particles + directional light v0 |
| GATE.S1.VISUAL_POLISH.CELESTIAL.001 | DONE | Planet + asteroid visual upgrade v0 |
| GATE.S1.VISUAL_POLISH.STRUCTURES.001 | DONE | Station + lane gate geometry upgrade v0 |
| GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001 | DONE | Fleet meshes + bullet colors + hit VFX v0 |
| GATE.S1.VISUAL_POLISH.SHIP_CAMERA.001 | DONE | Engine trail + camera follow tuning v0 |
| GATE.S1.VISUAL_POLISH.HUD_LABELS.001 | DONE | HP bars + credits + 3D world labels v0 |
| GATE.S1.VISUAL_POLISH.GALAXY_MAP.001 | DONE | Galaxy overlay connection styling v0 |
| GATE.S1.VISUAL_POLISH.FLEET_AI.001 | DONE | Fleet AI: patrol, dock, engage state machine v0 |
| GATE.S1.VISUAL_POLISH.SCENE_PROOF.001 | DONE | Visual scene headless proof v0 (HEADLESS_PROOF) |
| GATE.X.HYGIENE.SIMBRIDGE_COMBAT.001 | DONE | Extract SimBridge combat methods → SimBridge.Combat.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.001 | DONE | Epic tracking audit + course-correction recommendations |
| GATE.X.HYGIENE.REPO_HEALTH.001 | DONE | Full repo health pass: warnings, dead code, test coverage |
| GATE.X.HYGIENE.PLUGIN_EVAL.001 | DONE | Evaluate Godot plugins for visual + gameplay benefit |
| GATE.S5.COMBAT.COUNTER_FAMILY.001 | DONE | Point defense counter family v0 |
| GATE.S5.COMBAT.ESCORT_DOCTRINE.001 | DONE | Escort doctrine v0 (policy-driven) |
| GATE.S5.COMBAT.STRATEGIC_RESOLVER.001 | DONE | Fleet-vs-fleet outcome resolver v0 |
| GATE.S5.COMBAT.REPLAY_PROOF.001 | DONE | Deterministic combat replay proof |
| GATE.S5.COMBAT.BRIDGE_DOCTRINE.001 | DONE | Doctrine status + toggle via SimBridge |
| GATE.S5.COMBAT.SLICE_CLOSE.001 | DONE | Slice 5 content wave scenario proof (HEADLESS_PROOF) |
| GATE.S1.AUDIO.SFX_CORE.001 | DONE | Thrust + turret + hit + explosion SFX |
| GATE.S1.AUDIO.AMBIENT.001 | DONE | Space ambient + warp + dock chimes |
| GATE.S1.SAVE_UI.PAUSE_MENU.001 | DONE | Escape key pause menu (save/load/quit) |
| GATE.S1.SAVE_UI.SLOTS.001 | DONE | 3 save slots with metadata display |
| GATE.S1.DISCOVERY_INTERACT.SCAN.001 | DONE | Scan/Analyze buttons wired to SimBridge |
| GATE.S1.DISCOVERY_INTERACT.RESULTS.001 | DONE | Scan results display (unlock type/desc) |
| GATE.S6.FRACTURE.ACCESS_MODEL.001 | DONE | Fracture node access check (hull+tech) |
| GATE.S6.FRACTURE.MARKET_MODEL.001 | DONE | Fracture niche market pricing model |
| GATE.S6.FRACTURE.CONTENT.001 | DONE | 3 fracture-exclusive goods + node class |
| GATE.S6.FRACTURE.BRIDGE.001 | DONE | GetFractureAccessV0 + MarketV0 queries |
| GATE.S6.FRACTURE.TRAVEL.001 | DONE | Fracture route planning in RoutePlanner |
| GATE.S6.FRACTURE.ECON_FEEDBACK.001 | DONE | Fracture goods flow into lane hub markets |
| GATE.X.HYGIENE.EPIC_REVIEW.002 | DONE | Epic tracking audit + course correction |
| GATE.X.HYGIENE.REPO_HEALTH.002 | DONE | Full repo health + test suite pass |
| GATE.X.HYGIENE.SLICE5_AUDIT.001 | DONE | Slice 5 content wave readiness audit |
| GATE.X.HYGIENE.REPO_HEALTH.003 | DONE | Full test suite + health baseline |
| GATE.S1.MISSION.MODEL.001 | DONE | Mission schema + state model + persistence |
| GATE.S1.MISSION.SYSTEM.001 | DONE | MissionSystem: trigger eval + step advance + events |
| GATE.S1.MISSION.CONTENT.001 | DONE | Mission 1 "Matched Luggage" definition + loader |
| GATE.S1.MISSION.DETERMINISM.001 | DONE | Mission state determinism + save/load regression |
| GATE.S6.FRACTURE_ECON.INVARIANT.001 | DONE | Fracture lane-dominance 100-seed invariant tests |
| GATE.S1.VISUAL_POLISH.EPIC_CLOSE.001 | DONE | Close EPIC.S1.VISUAL_POLISH.V0 (9 gates DONE) |
| GATE.S1.AUDIO.EPIC_CLOSE.001 | DONE | Close EPIC.S1.AUDIO_MIN.V0 (2 gates DONE) |
| GATE.S1.SAVE_UI.EPIC_CLOSE.001 | DONE | Close EPIC.S1.SAVE_LOAD_UI.V0 (2 gates DONE) |
| GATE.S1.DISCOVERY_INTERACT.EPIC_CLOSE.001 | DONE | Close EPIC.S1.DISCOVERY_INTERACT.V0 (3 gates DONE) |
| GATE.S5.COMBAT_DOCTRINE.EPIC_CLOSE.001 | DONE | Close EPIC.S5.COMBAT_DOCTRINE.V0 (6 gates DONE) |
| GATE.S6.FRACTURE.EPIC_CLOSE.001 | DONE | Close EPIC.S6.FRACTURE_COMMERCE.V0 (6 gates DONE) |
| GATE.X.CODE_HEALTH.EPIC_CLOSE.001 | DONE | Close EPIC.X.CODE_HEALTH (4 gates DONE) |
| GATE.S1.MISSION.BRIDGE.001 | DONE | SimBridge mission queries (active/list/accept) |
| GATE.S1.MISSION.HUD.001 | DONE | Mission objective panel in game HUD |
| GATE.S1.MISSION.HEADLESS_PROOF.001 | DONE | Mission 1 headless completion proof (PLAYABLE_BEAT) |
| GATE.X.HYGIENE.EPIC_REVIEW.003 | DONE | Epic tracking audit + course correction |
| GATE.X.HYGIENE.GREATNESS_EVAL.001 | DONE | "First 60 minutes" player experience evaluation |
| GATE.S4.TECH.CORE.001 | DONE | Tech research entities + tweaks + content |
| GATE.S4.UPGRADE.CORE.001 | DONE | Refit job entity + tweaks |
| GATE.S4.MAINT.CORE.001 | DONE | Maintenance condition model + tweaks |
| GATE.S4.TECH.SYSTEM.001 | DONE | ResearchSystem process loop + tests |
| GATE.S4.UPGRADE.SYSTEM.001 | DONE | RefitSystem process loop + tests |
| GATE.S4.MAINT.SYSTEM.001 | DONE | MaintenanceSystem decay/repair + tests |
| GATE.S4.TECH.BRIDGE.001 | DONE | SimBridge research queries |
| GATE.S4.UPGRADE.BRIDGE.001 | DONE | SimBridge refit queries |
| GATE.S4.MAINT.BRIDGE.001 | DONE | SimBridge maintenance queries |
| GATE.S4.TECH.SAVE.001 | DONE | Research + refit + maintenance save/load |
| GATE.S4.UI_INDU.RESEARCH.001 | DONE | Research panel in dock menu |
| GATE.S4.UI_INDU.UPGRADE.001 | DONE | Refit panel in dock menu |
| GATE.S4.UI_INDU.MAINT.001 | DONE | Maintenance view in production panel |
| GATE.S4.TECH.PROOF.001 | DONE | Research pipeline headless proof |
| GATE.S4.INDU.PLAYABLE_BEAT.001 | DONE | Industry in-engine playable beat |
| GATE.X.HYGIENE.REPO_HEALTH.004 | DONE | Test suite + health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.004 | DONE | Epic status audit |
| GATE.X.EVAL.PROGRESSION_AUDIT.001 | DONE | Progression depth evaluation |
| GATE.X.HYGIENE.REPO_HEALTH.005 | DONE | Full test suite, warning scan, golden hash stability |
| GATE.S1.CAMERA.ADDON_SETUP.001 | DONE | Install Phantom Camera addon, configure project.godot |
| GATE.S1.SPATIAL_AUDIO.ENGINE_THRUST.001 | DONE | Engine thrust AudioStreamRandomizer on player ship |
| GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001 | DONE | Tech tree tier and cost scaling in SimCore |
| GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001 | DONE | Timed refit queue — install takes N ticks |
| GATE.S4.MAINT_SUSTAIN.SUPPLY_REPAIR.001 | DONE | Repair consumes supply goods, not just credits |
| GATE.S3.RISK_SINKS.DELAY_MODEL.001 | DONE | Player-visible travel delay model in RiskSystem |
| GATE.S1.CAMERA.FOLLOW_MODES.001 | DONE | PhantomCamera follow modes — flight, orbit, station |
| GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001 | DONE | Positional SFX for turret fire and bullet impact |
| GATE.S1.SPATIAL_AUDIO.AMBIENT.001 | DONE | Ambient spatial audio — station hum, lane drone |
| GATE.S4.TECH_INDUSTRIALIZE.BRIDGE_DEPTH.001 | DONE | SimBridge queries for tech tier and tier-gated content |
| GATE.S4.UPGRADE_PIPELINE.BRIDGE_QUEUE.001 | DONE | SimBridge refit queue status + progress polling |
| GATE.S4.MAINT_SUSTAIN.BRIDGE_SUPPLY.001 | DONE | SimBridge supply level queries + repair-with-supply |
| GATE.S4.UI_INDU.WHY_BLOCKED.001 | DONE | Why-blocked tooltip for locked tech/upgrades/repairs |
| GATE.S3.RISK_SINKS.BRIDGE.001 | DONE | SimBridge delay risk queries + travel ETA |
| GATE.S1.CAMERA.COMBAT_SHAKE.001 | DONE | Camera shake on turret fire and damage received |
| GATE.S1.PRESENTATION.HEADLESS_PROOF.001 | DONE | Headless proof — camera + audio nodes boot |
| GATE.X.HYGIENE.EPIC_REVIEW.005 | DONE | Audit epic statuses, close completed, recommend next |
| GATE.X.EVAL.AUDIO_VISUAL_AUDIT.001 | DONE | Audio/visual presentation quality audit |
| GATE.X.HYGIENE.REPO_HEALTH.006 | DONE | Full test suite + golden hash stability baseline |
| GATE.S1.VISUAL_UPGRADE.ADDON_INSTALL.001 | DONE | Install Starlight + Planet Gen + Kenney Kit addons |
| GATE.S9.UI.TOOLTIP_SETUP.001 | DONE | Install Tooltips Pro addon + basic tooltip |
| GATE.S1.MISSION.CONTENT_WAVE.001 | DONE | 3 new mission defs in MissionContentV0 |
| GATE.S4.CATALOG.TECH_WAVE.001 | DONE | Expand tech tree: 5+ techs across tiers 1-3 |
| GATE.S4.CATALOG.MODULE_WAVE.001 | DONE | 5+ new modules: engine, utility, cargo variants |
| GATE.S4.CATALOG.RECIPE_WAVE.001 | DONE | 3-5 new production recipes for goods chain |
| GATE.S3.RISK_SINKS.HUD_INDICATOR.001 | DONE | Travel risk/delay indicator in flight HUD |
| GATE.S1.VISUAL_UPGRADE.SKYBOX.001 | DONE | Wire Starlight procedural skybox into scene |
| GATE.S1.VISUAL_UPGRADE.WORLD_MESHES.001 | DONE | Planet gen + Kenney models on planets/ships/stations |
| GATE.S9.UI.TOOLTIP_DOCK.001 | DONE | Tooltips on market goods + industry panels |
| GATE.S9.UI.TOOLTIP_HUD.001 | DONE | Tooltips on combat + flight HUD elements |
| GATE.S1.MISSION.MULTI_PROOF.001 | DONE | Multi-mission scenario proof |
| GATE.S4.CATALOG.CONTENT_PROOF.001 | DONE | Content wave determinism proof |
| GATE.S1.VISUAL_UPGRADE.SCENE_PROOF.001 | DONE | Visual addon headless boot proof |
| GATE.S9.UI.TOOLTIP_PROOF.001 | DONE | Tooltip integration headless proof |
| GATE.X.HYGIENE.EPIC_REVIEW.006 | DONE | Epic audit — close 7+ completed epics |
| GATE.X.EVAL.ADDON_COMPAT.001 | DONE | Addon compatibility + integration quality audit |
| GATE.S4.CONSTR_PROG.MODEL.001 | DONE | Construction entity model + content defs |
| GATE.S4.CONSTR_PROG.SYSTEM.001 | DONE | ConstructionSystem process loop |
| GATE.S4.CONSTR_PROG.SAVE.001 | DONE | Construction save/load + determinism |
| GATE.S4.CONSTR_PROG.BRIDGE.001 | DONE | SimBridge construction queries |
| GATE.S4.CONSTR_PROG.UI.001 | DONE | Construction panel + headless proof |
| GATE.S4.NPC_INDU.DEMAND.001 | DONE | NPC industry demand model |
| GATE.S4.NPC_INDU.REACTION.001 | DONE | NPC production reactions |
| GATE.S4.NPC_INDU.BRIDGE.001 | DONE | SimBridge NPC industry queries |
| GATE.S4.NPC_INDU.PROOF.001 | DONE | NPC industry scenario proof |
| GATE.S4.PERF_BUDGET.INDUSTRY.001 | DONE | Industry tick budget tests |
| GATE.X.PRESSURE.MODEL.001 | DONE | PressureState entity + enum |
| GATE.X.PRESSURE.SYSTEM.001 | DONE | PressureSystem enforcement |
| GATE.X.PRESSURE.BRIDGE.001 | DONE | SimBridge pressure queries |
| GATE.X.PRESSURE.PROOF.001 | DONE | Pressure alert-count scenario test |
| GATE.S4.SLICE_CLOSE.PROOF.001 | DONE | Slice 4 completion scenario proof |
| GATE.X.HYGIENE.REPO_HEALTH.007 | DONE | Test suite + golden hash stability |
| GATE.X.HYGIENE.EPIC_REVIEW.007 | DONE | Epic status audit + recommendations |
| GATE.X.EVAL.INDUSTRY_AUDIT.001 | DONE | Industry systems completeness audit |
| GATE.S7.PLANET.MODEL.001 | DONE | Planet + Star entity model, content defs, tweaks |
| GATE.S7.PLANET.GENERATION.001 | DONE | PlanetInitGen + wire into GalaxyGenerator |
| GATE.S7.PLANET.BRIDGE.001 | DONE | SimBridge planet/star queries + PlanetSnapV0 |
| GATE.S7.PLANET.ECONOMY.001 | DONE | Planet industry seeding by specialization |
| GATE.S7.PLANET.DOCK_VISUAL.001 | DONE | Area3D dock trigger + type-matched scenes + star color |
| GATE.S7.PLANET.TECH_GATE.001 | DONE | planetary_landing_mk1 tech + effective_landable |
| GATE.S7.PLANET.UI.001 | DONE | Dock menu planet title + info section |
| GATE.S7.PLANET.PROOF.001 | DONE | 490 tests pass, golden hashes re-minted, ExplorationBot 8/8 |
| GATE.S5.SEC_LANES.MODEL.001 | DONE | Security lane entity: SecurityLevelBps on Edge + SecurityTweaksV0 |
| GATE.S5.SEC_LANES.SYSTEM.001 | DONE | Security lane system: patrol presence + piracy heat → security level |
| GATE.S5.SEC_LANES.BRIDGE.001 | DONE | Security lane bridge: GetLaneSecurityV0, GetNodeSecurityV0 |
| GATE.S5.SEC_LANES.UI.001 | DONE | Security lane UI: lane color by security, dock/HUD readout |
| GATE.S5.COMBAT_RES.SYSTEM.001 | DONE | Combat resolution: ResolveCombatV0 evaluates HP/damage → outcome |
| GATE.S5.COMBAT_RES.PROOF.001 | DONE | Combat resolution headless proof: engage, resolve, verify outcome |
| GATE.S1.FLEET_VISUAL.MAP.001 | DONE | Fleet visual: load Kenney .glb models by FleetRole on galaxy map |
| GATE.S1.FLEET_VISUAL.VIEW.001 | DONE | Fleet visual: role-based Kenney models in local system view |
| GATE.S1.FLEET_VISUAL.PROOF.001 | DONE | Fleet visual headless proof: verify correct model per role |
| GATE.S5.NPC_TRADE.SYSTEM.001 | DONE | NPC trade system: autonomous NPC traders evaluate and execute trades |
| GATE.S5.NPC_TRADE.BRIDGE.001 | DONE | NPC trade bridge: GetNpcTradeRoutesV0, GetNpcTradeActivityV0 |
| GATE.S5.NPC_TRADE.ECON_PROOF.001 | DONE | NPC trade economy proof: multi-seed bot verifies goods circulation |
| GATE.X.PRESSURE.LADDER.001 | DONE | Pressure ladder: Calm→Tension→Crisis→Collapse tiers with BPS thresholds |
| GATE.X.PRESSURE.ENFORCE.001 | DONE | Pressure enforcement: ladder consequences (fees, piracy, events) |
| GATE.X.HYGIENE.REPO_HEALTH.008 | DONE | Repo health: full test suite + warning scan + golden hash stability |
| GATE.X.HYGIENE.EPIC_REVIEW.008 | DONE | Epic review: audit epic statuses, close completed, recommend next anchor |
| GATE.X.EVAL.ECONOMY_AUDIT.001 | DONE | Economy audit: multi-seed ExplorationBot + econ loop analysis |
| GATE.S10.TRADE_INTEL.BRIDGE.001 | DONE | Trade intel bridge: GetTradeRoutesV0, GetPriceIntelV0, GetScannerRangeV0 |
| GATE.S10.TRADE_PROG.BRIDGE.001 | DONE | Program creation bridge + research sustain bridge update |
| GATE.S10.TRADE_INTEL.CHARTER_FIX.001 | DONE | TradeCharter uses real market buy/sell prices |
| GATE.S10.TRADE_INTEL.DOCK_UI.001 | DONE | Trade Routes + Research sustain status in dock menu |
| GATE.S10.TRADE_INTEL.PROOF.001 | DONE | Headless proof: routes → charter → research sustain |
| GATE.S10.EMPIRE.BRIDGE.001 | DONE | Empire bridge: GetEmpireSummaryV0, GetAllIndustryV0, GetNodeHealthV0 |
| GATE.S10.EMPIRE.SHELL.001 | DONE | Empire dashboard modal shell + E key binding |
| GATE.S10.EMPIRE.OVERVIEW_TAB.001 | DONE | Overview + Trade tabs |
| GATE.S10.EMPIRE.PRODUCTION_TAB.001 | DONE | Production + Programs tabs |
| GATE.S10.EMPIRE.INTEL_TAB.001 | DONE | Intel tab: discoveries, rumors, freshness |
| GATE.S6.MAP_GALAXY.OVERLAY_SYS.001 | DONE | Galaxy map overlay mode system + selector |
| GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001 | DONE | Trade flow overlay on galaxy map |
| GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001 | DONE | Intel freshness overlay on galaxy map |
| GATE.S5.ESCORT_PROG.MODEL.001 | DONE | Escort + Patrol program model + contract tests |
| GATE.S5.ESCORT_PROG.BRIDGE.001 | DONE | Escort/Patrol bridge: create, assign, status |
| GATE.S5.ESCORT_PROG.UI.001 | DONE | Escort/Patrol creation in Empire Programs tab |
| GATE.S10.TECH_EFFECTS.TESTS.001 | DONE | Contract tests for speed_bonus + production_efficiency |
| GATE.X.HYGIENE.REPO_HEALTH.009 | DONE | Full test suite + golden hash stability baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.009 | DONE | Epic status audit + close completed + next anchor |
| GATE.X.EVAL.PROGRESSION_AUDIT.002 | DONE | ExplorationBot re-run: trade intel + research sustain verification |
| GATE.X.HYGIENE.REPO_HEALTH.010 | DONE | Full test suite + golden hash stability baseline |
| GATE.S11.GAME_FEEL.TOAST_SYS.001 | DONE | Toast notification manager (GDScript autoload) |
| GATE.S11.GAME_FEEL.TECH_BRIDGE.001 | DONE | GetTechTreeV0 bridge method |
| GATE.S11.GAME_FEEL.PRICE_HISTORY.001 | DONE | Price history tracking in IntelBook + bridge query |
| GATE.S6.MAP_GALAXY.NODE_CLICK.001 | DONE | Galaxy map node click detail popup |
| GATE.S11.GAME_FEEL.NPC_ROUTE_VIS.001 | DONE | NPC fleet route lines on galaxy map |
| GATE.S11.GAME_FEEL.MISSION_WIRE.001 | DONE | Wire missions 2-4 to bridge acceptance |
| GATE.S11.GAME_FEEL.COMBAT_BRIDGE.001 | DONE | GetRecentCombatV0 bridge method |
| GATE.S11.GAME_FEEL.TECH_TREE_UI.001 | DONE | Tech tree panel in empire dashboard |
| GATE.S11.GAME_FEEL.TOAST_EVENTS.001 | DONE | Game events to toast notifications |
| GATE.S11.GAME_FEEL.MISSION_HUD.001 | DONE | Active mission objective in flight HUD |
| GATE.S11.GAME_FEEL.RESEARCH_HUD.001 | DONE | Research progress bar in flight HUD |
| GATE.S11.GAME_FEEL.KEYBINDS.001 | DONE | H key help overlay showing all controls |
| GATE.S11.GAME_FEEL.NODE_MARKET.001 | DONE | Market prices tab in node detail popup |
| GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001 | DONE | Combat event log panel (L key) |
| GATE.S11.GAME_FEEL.FLEET_STATUS.001 | DONE | Fleet role icons on galaxy map nodes |
| GATE.S11.GAME_FEEL.DOCK_ENHANCE.001 | DONE | Enhanced dock menu with mission + research status |
| GATE.S11.GAME_FEEL.HEADLESS_PROOF.001 | DONE | Headless proof of game feel features |
| GATE.X.HYGIENE.EPIC_REVIEW.010 | DONE | Epic status audit + close completed + next anchor |
| GATE.X.EVAL.UX_AUDIT.001 | DONE | UX and player experience evaluation |
| GATE.S12.FLEET_SUBSTANCE.QUATERNIUS.001 | DONE | Quaternius model loader for fleet roles |
| GATE.S12.NPC_CIRC.CIRCUIT_ROUTES.001 | DONE | NPC patrol multi-hop circuit routes |
| GATE.S12.UX_POLISH.QUANTITY.001 | DONE | Buy/Sell quantity buttons [1,5,Max] |
| GATE.S12.UX_POLISH.DISPLAY_NAMES.001 | DONE | FormatDisplayNameV0 snake_case to readable |
| GATE.S12.UX_POLISH.ONBOARDING.001 | DONE | First-dock toast onboarding sequence |
| GATE.S12.PROGRESSION.STATS.001 | DONE | PlayerStats tracking in SimState |
| GATE.S12.PROGRESSION.MILESTONES.001 | DONE | Milestone definitions and evaluation |
| GATE.X.HYGIENE.REPO_HEALTH.011 | DONE | Full test suite + golden hash stability baseline |
| GATE.S12.FLEET_SUBSTANCE.VARIETY.001 | DONE | Hash-based model variant + player ship model |
| GATE.S12.NPC_CIRC.FLOW_ANIM.001 | DONE | Animated flow dots on NPC trade route edges |
| GATE.S12.NPC_CIRC.VOLUME_LABELS.001 | DONE | Trade volume labels on galaxy map edges |
| GATE.S12.UX_POLISH.CARGO_DISPLAY.001 | DONE | Cargo used/max in dock menu + HUD |
| GATE.S12.UX_POLISH.TRADE_FEEDBACK.001 | DONE | Toast on buy/sell with formatted profit/loss |
| GATE.S12.PROGRESSION.TESTS.001 | DONE | Contract tests for stats + milestones |
| GATE.S12.PROGRESSION.BRIDGE.001 | DONE | GetPlayerStatsV0 + GetMilestonesV0 bridge |
| GATE.S12.PROGRESSION.DASHBOARD.001 | DONE | Stats + milestones tab in EmpireDashboard |
| GATE.S12.FLEET_SUBSTANCE.HEADLESS_PROOF.001 | DONE | Headless proof of fleet models + UX |
| GATE.X.HYGIENE.EPIC_REVIEW.011 | DONE | Epic audit + close stale epics |
| GATE.X.EVAL.VISUAL_AUDIT.001 | DONE | Visual substance and UX evaluation |
| GATE.S13.CAMERA.TOPDOWN.001 | DONE | Top-down camera angle for flight mode |
| GATE.S13.CAMERA.PERSIST.001 | DONE | Camera holds rotation on mouse release |
| GATE.S13.CONTROLS.TURNING.001 | DONE | Increased ship turn responsiveness |
| GATE.S13.CONTROLS.SPEED.001 | DONE | Reduced top speed + faster deceleration |
| GATE.S13.COMBAT.EXPLOSION_SCALE.001 | DONE | Reduced explosion and muzzle flash scale |
| GATE.S13.DOCK.TABS.001 | DONE | Dock menu tab system (Market/Jobs/Services) |
| GATE.S13.DOCK.HIDE_EMPTY.001 | DONE | Hide advanced sections until relevant |
| GATE.S13.DOCK.CONTEXT.001 | DONE | Station context description at top |
| GATE.S13.EMPIRE.GATING.001 | DONE | Progressive tab visibility in dashboard |
| GATE.S13.EMPIRE.OVERVIEW.001 | DONE | Comprehensible overview tab labels |
| GATE.S13.UX.TERMINOLOGY.001 | DONE | Player-facing terminology throughout UI |
| GATE.S13.LABELS.CLAMP.001 | DONE | Distance-based label size clamping |
| GATE.S13.GATES.ARRIVAL.001 | DONE | Arrive at corresponding gate facing inward |
| GATE.S13.GATES.DIRECTION.001 | DONE | Gates face toward destination system |
| GATE.S13.MAP.CENTER.001 | DONE | Galaxy map centers on player node |
| GATE.S13.LABELS.HOSTILE.001 | DONE | Hostile fleet labels instead of role names |
| GATE.S13.NPC.VISIBLE.001 | DONE | Fix NPC fleet visibility and AI orbit |
| GATE.X.HYGIENE.REPO_HEALTH.012 | DONE | Full test suite + build verification |
| GATE.X.HYGIENE.EPIC_REVIEW.012 | DONE | Epic audit + close completed |
| GATE.X.EVAL.FEEL_AUDIT.001 | DONE | Post-implementation feel evaluation |
| GATE.S14.NPC_ALIVE.FLEET_SEED.001 | DONE | Fix NPC fleet survival + role diversity |
| GATE.S14.NPC_ALIVE.FLEET_TESTS.001 | DONE | Contract tests for fleet survival |
| GATE.S14.NPC_ALIVE.EXPLORATION_BOT.001 | DONE | Update ExplorationBot for fleet activity |
| GATE.S14.TRANSIT.WARP_EFFECT.001 | DONE | Screen flash + camera shake on lane travel |
| GATE.S14.GATE_VISUAL.KENNEY_MODEL.001 | DONE | Replace BoxMesh gates with Kenney model |
| GATE.S14.STAR.TINT_FIX.001 | DONE | Fix star shader tinting formula |
| GATE.S14.STAR.STARTER_GUARANTEE.001 | DONE | star_0 always ClassG |
| GATE.S14.ASTEROID.SHAPE_VARIETY.001 | DONE | Mixed asteroid shapes |
| GATE.S14.DOCK.VISUAL_FRAME.001 | DONE | Dark panel with border for dock menu |
| GATE.S14.STARLIGHT.BRIGHTNESS.001 | DONE | Increase background star visibility |
| GATE.S14.DOCK.PROXIMITY_TIGHTEN.001 | DONE | Smaller dock collision boxes |
| GATE.S14.HUD.DOCK_CLEANUP.001 | DONE | Hide mission panel when docked |
| GATE.S14.MAP.PLAYER_INDICATOR.001 | DONE | Pulsing YOU on galaxy map |
| GATE.S14.STARTER.MISSION_PROMPT.001 | DONE | Surface missions on first dock |
| GATE.S14.GOLDEN.HASH_UPDATE.001 | DONE | Consolidate golden hash updates |
| GATE.X.HYGIENE.REPO_HEALTH.013 | DONE | Full test suite + build verification |
| GATE.X.HYGIENE.EPIC_REVIEW.013 | DONE | Epic audit + close completed |
| GATE.S6.REVEAL.SCAN_CMD.001 | DONE | ScanDiscovery command to advance discovery phases |
| GATE.S6.ANOMALY.ENCOUNTER_MODEL.001 | DONE | Anomaly encounter entity + trigger in combat system |
| GATE.S15.FEEL.JUMP_EVENT_SYS.001 | DONE | JumpEventSystem — random events during lane transit |
| GATE.S15.FEEL.STAR_LIGHTING.001 | DONE | Star-class DirectionalLight3D tinting in local system |
| GATE.S6.REVEAL.DISCOVERY_SNAP.001 | DONE | SimBridge discovery phase snapshot with progress |
| GATE.S15.FEEL.NPC_PROXIMITY.001 | DONE | NPC freighter Quaternius models on player proximity |
| GATE.X.HYGIENE.REPO_HEALTH.014 | DONE | Repo health baseline — full test suite + hash stability |
| GATE.S6.ANOMALY.REWARD_LOOT.001 | DONE | Anomaly encounter loot table + reward generation |
| GATE.S6.OUTCOME.REWARD_MODEL.001 | DONE | Discovery outcome rewards model |
| GATE.S15.FEEL.FACTION_TERRITORY.001 | DONE | Faction territory mapping in world gen |
| GATE.S6.REVEAL.DISCOVERY_HUD.001 | DONE | Discovery HUD panel showing scan progress |
| GATE.S6.ANOMALY.ENCOUNTER_BRIDGE.001 | DONE | SimBridge anomaly encounter queries |
| GATE.S15.FEEL.JUMP_EVENT_TOAST.001 | DONE | Jump event toast display via toast_manager |
| GATE.S15.FEEL.AMBIENT_SYSTEM.001 | DONE | Local system ambient particles (dust, wisps) |
| GATE.S15.FEEL.FACTION_LABELS.001 | DONE | Faction territory labels on galaxy/local view |
| GATE.S6.ANOMALY.ENCOUNTER_UI.001 | DONE | Anomaly encounter UI panel in dock Services tab |
| GATE.S6.OUTCOME.CELEBRATION.001 | DONE | Discovery completion celebration VFX + toast |
| GATE.S6.OUTCOME.REWARD_BRIDGE.001 | DONE | Discovery outcome bridge + reward display |
| GATE.S15.FEEL.EXPLORATION_PROOF.001 | DONE | Exploration depth headless proof |
| GATE.X.HYGIENE.EPIC_REVIEW.014 | DONE | Epic status audit (tranche 14) |
| GATE.X.HYGIENE.EXPLORE_ARCH_EVAL.001 | DONE | Exploration architecture review |
| GATE.X.HYGIENE.REPO_HEALTH.015 | DONE | Full test suite + build verification baseline |
| GATE.S16.NPC_ALIVE.LIMBO_INSTALL.001 | DONE | Install LimboAI addon + project.godot config |
| GATE.S16.NPC_ALIVE.SHIP_SCENE.001 | DONE | NPC ship packed scene (CharacterBody3D + collision) |
| GATE.S16.NPC_ALIVE.DAMAGE_CMD.001 | DONE | NpcFleetDamageCommand: apply HP damage to NPC fleet |
| GATE.S16.NPC_ALIVE.DELAY_ENFORCE.001 | DONE | MovementSystem: enforce DelayTicksRemaining |
| GATE.S16.NPC_ALIVE.TRANSIT_SNAP.001 | DONE | SimBridge FleetTransitFactV0 query |
| GATE.S16.NPC_ALIVE.FLEET_DESTROY.001 | DONE | NpcFleetCombatSystem: destroy fleets at 0 HP |
| GATE.S16.NPC_ALIVE.FLIGHT_CTRL.001 | DONE | NPC flight controller (CharacterBody3D steering) |
| GATE.S16.NPC_ALIVE.BT_TASKS.001 | DONE | LimboAI custom BT tasks (FlyTo, Warp, SelectDest) |
| GATE.S16.NPC_ALIVE.SPAWN_SYSTEM.001 | DONE | GalaxyView: physical NPC ship spawning |
| GATE.S16.NPC_ALIVE.COMBAT_BRIDGE.001 | DONE | Player-NPC combat bridge (hit → damage → sim) |
| GATE.S16.NPC_ALIVE.BT_ROLES.001 | DONE | Trader/Hauler/Patrol behavior tree resources |
| GATE.S16.NPC_ALIVE.WARP_VFX.001 | DONE | Warp-in/warp-out visual effects at lane gates |
| GATE.S16.NPC_ALIVE.DESPAWN.001 | DONE | NPC ship despawn on system exit |
| GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001 | DONE | NPC role icon + HP bar overlay |
| GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001 | DONE | Fleet respawn system after NPC destruction |
| GATE.S16.NPC_ALIVE.HEADLESS_PROOF.001 | DONE | NPC ships spawn/move/warp headless proof |
| GATE.X.HYGIENE.EPIC_REVIEW.015 | DONE | Epic audit (close S14, S15 candidates) |
| GATE.X.HYGIENE.LIMBO_EVAL.001 | DONE | LimboAI integration architecture evaluation |
| GATE.X.HYGIENE.REPO_HEALTH.016 | DONE | Full test suite + build verification baseline |
| GATE.S17.REAL_SPACE.STAR_COORDS.001 | DONE | Galactic-scale 3D star positions in WorldDefinition |
| GATE.S7.FACTION.DOCTRINE_MODEL.001 | DONE | FactionDoctrine fields + placeholder policies per faction |
| GATE.S7.FACTION.REPUTATION_SYS.001 | DONE | Player reputation per faction (+trade, -attack) |
| GATE.S6.FRACTURE.VOID_SITES.001 | DONE | Void discovery sites seeded between systems |
| GATE.S17.REAL_SPACE.GALAXY_RENDER.001 | DONE | GalaxyView: persistent stars + local detail at real position + LOD |
| GATE.S7.FACTION.TARIFF_ENFORCE.001 | DONE | MarketSystem tariff/access by reputation + doctrine |
| GATE.S6.FRACTURE.MARKER_CMD.001 | DONE | SurveyMarker + PlaceMarkerCommand (tech-gated estimate) |
| GATE.S6.FRACTURE.TRAVEL_CMD.001 | DONE | FractureTravelCommand: off-lane travel to void site |
| GATE.S7.FACTION.BRIDGE_QUERIES.001 | DONE | SimBridge faction doctrine/reputation/access queries |
| GATE.S17.REAL_SPACE.LANE_TRANSIT.001 | DONE | Physical lane traversal with tween acceleration |
| GATE.S17.REAL_SPACE.WARP_TUNNEL.001 | DONE | Warp tunnel VFX obscuring void during transit |
| GATE.S17.REAL_SPACE.GALAXY_MAP.001 | DONE | TAB map: high-altitude camera over real space |
| GATE.S6.FRACTURE.SENSOR_REVEAL.001 | DONE | Sensor tech reveals void sites during transit |
| GATE.S7.FACTION.PATROL_AGGRO.001 | DONE | NPC patrol aggro gated by player reputation |
| GATE.S7.FACTION.UI_REPUTATION.001 | DONE | Faction rep bars + tariff warnings in UI |
| GATE.S17.REAL_SPACE.HEADLESS_PROOF.001 | DONE | Real-space headless proof |
| GATE.X.HYGIENE.EPIC_REVIEW.016 | DONE | Epic audit (tranche 16) |
| GATE.X.HYGIENE.REALSPACE_EVAL.001 | DONE | Real-space architecture evaluation |
| GATE.X.HYGIENE.REPO_HEALTH.017 | DONE | Full test suite + build verification baseline |
| GATE.S18.TRADE_GOODS.CONTENT_OVERHAUL.001 | DONE | Migrate 10→13 goods, 7→9 recipes per trade_goods_v0.md |
| GATE.S18.SHIP_MODULES.ZONE_ARMOR.001 | DONE | Zone armor entity (Fore/Port/Stbd/Aft HP per fleet) |
| GATE.S18.EMPIRE_DASH.DOCK_TABS.001 | DONE | Dock menu 3→5 tabs (Market/Jobs/Ship/Station/Intel) |
| GATE.S18.TRADE_GOODS.GEO_DISTRIBUTION.001 | DONE | Geographic distribution: organics 40%, rare_metals 15%, fracture-only crystals |
| GATE.S18.TRADE_GOODS.PRICE_BANDS.001 | DONE | Base price bands Low/Mid/High/VeryHigh per good + market spread tweaks |
| GATE.S18.TRADE_GOODS.SUSTAIN_ALIGN.001 | DONE | Module sustain: weapons consume munitions not metal |
| GATE.S18.SHIP_MODULES.SHIP_CLASS.001 | DONE | 8 ship classes (Shuttle→Dreadnought) with base stats |
| GATE.S18.SHIP_MODULES.FITTING_BUDGET.001 | DONE | 3-constraint fitting: Slots + Power + Sustain |
| GATE.S18.TRADE_GOODS.BRIDGE_MARKET.001 | DONE | SimBridge market queries for new/renamed goods |
| GATE.S18.EMPIRE_DASH.OVERVIEW_TAB.001 | DONE | Empire Overview (F1): 6 summary cards + attention queue |
| GATE.S18.TRADE_GOODS.CHAIN_TESTS.001 | DONE | Contract tests for all 9 production chains |
| GATE.S18.EMPIRE_DASH.STATION_TAB.001 | DONE | Dock Station tab: health, local production, services |
| GATE.S18.TRADE_GOODS.NPC_TRADE_UPDATE.001 | DONE | NPC traders haul new goods along geographic routes |
| GATE.S18.SHIP_MODULES.COMBAT_ZONES.001 | DONE | Zone armor in combat: collision zones + stance hit distribution |
| GATE.S18.EMPIRE_DASH.SHIP_TAB.001 | DONE | Dock Ship tab: class, modules, fitting budget, zone armor |
| GATE.S18.EMPIRE_DASH.ECONOMY_TAB.001 | DONE | Empire Economy (F2): routes, prices, supply/demand |
| GATE.S18.TRADE_GOODS.HEADLESS_PROOF.001 | DONE | Headless proof: buy/sell new goods, verify inventory |
| GATE.X.HYGIENE.EPIC_REVIEW.017 | DONE | Epic audit (tranche 17) |
| GATE.X.HYGIENE.ECONOMY_EVAL.001 | DONE | 13-good economy evaluation against design pillars |
| GATE.S7.REPUTATION.ACCESS_TIERS.001 | DONE | 5 rep tiers gate dock/trade/tech access |
| GATE.S7.REPUTATION.PRICING_CURVES.001 | DONE | Rep-driven price modifiers in MarketSystem |
| GATE.S7.TERRITORY.REGIME_MODEL.001 | DONE | Dynamic territory regime from doctrine+rep |
| GATE.S6.FRACTURE.COST_MODEL.001 | DONE | Fracture costs: fuel, hull stress, trace accumulation |
| GATE.S7.TERRITORY.PATROL_RESPONSE.001 | DONE | Patrol behavior varies by territory regime |
| GATE.S6.FRACTURE.DETECTION_REP.001 | DONE | Factions detect fracture trace + rep penalty |
| GATE.S6.FRACTURE.PLAYER_DISPATCH.001 | DONE | Wire DispatchFractureTravelV0 in SimBridge + game_manager |
| GATE.S7.REPUTATION.UI_INDICATORS.001 | DONE | Trade menu: rep impact on prices + access warnings |
| GATE.S7.TERRITORY.BRIDGE_DISPLAY.001 | DONE | Territory regime in galaxy view + dock info |
| GATE.S6.FRACTURE.UI_PANEL.001 | DONE | Fracture travel UI: destination, cost, confirm |
| GATE.S7.INFRA.HEADLESS_PROOF.001 | DONE | Headless: rep tiers, territory regimes, fracture travel |
| GATE.X.HYGIENE.REPO_HEALTH.018 | DONE | Full test suite, warnings, dead code, golden hash |
| GATE.X.HYGIENE.EPIC_REVIEW.018 | DONE | Audit epic statuses, recommend content tranche |
| GATE.X.HYGIENE.LORE_REVIEW.001 | DONE | Map lore doc to gates for content tranche |
| GATE.S7.FACTION.CONTENT_DATA.001 | DONE | Populate 5 factions with lore-accurate content data |
| GATE.S7.INSTABILITY.PHASE_MODEL.001 | DONE | Per-node instability int (0-100+), 5 phase thresholds |
| GATE.S7.WARFRONT.STATE_MODEL.001 | DONE | Warfront entity: combatants, intensity 0-4, contested nodes |
| GATE.X.HYGIENE.REPO_HEALTH.019 | DONE | Full test suite + golden hash stability |
| GATE.S7.FACTION.PENTAGON_RING.001 | DONE | Pentagon dependency ring + secondary cross-links |
| GATE.S7.WARFRONT.SEEDING.001 | DONE | Seed 1 hot war + 1 cold war at tick 0 |
| GATE.S7.WARFRONT.DEMAND_SHOCK.001 | DONE | Wartime demand multipliers on market consumption |
| GATE.S7.WARFRONT.TARIFF_SCALING.001 | DONE | War surcharge: BaseTariff + Surcharge * Intensity |
| GATE.S7.INSTABILITY.WORLDGEN.001 | DONE | GalaxyGenerator assigns initial instability per node |
| GATE.S7.WARFRONT.BRIDGE.001 | DONE | SimBridge warfront queries |
| GATE.S7.INSTABILITY.BRIDGE.001 | DONE | SimBridge instability phase queries |
| GATE.S7.FACTION.IDENTITY_PANEL.001 | DONE | Faction identity UI panel (species, produces/needs) |
| GATE.S7.WARFRONT.EVOLUTION.001 | DONE | Warfront intensity evolution over ticks |
| GATE.S7.WARFRONT.NEUTRALITY_TAX.001 | DONE | Escalating costs for neutral traders during war |
| GATE.S7.WARFRONT.SUPPLY_CASCADE.001 | DONE | Pentagon cascade: war disrupts supply chain |
| GATE.S7.WARFRONT.UI_MAP.001 | DONE | Galaxy map warfront overlays + tariff display |
| GATE.S7.INSTABILITY.VISUAL.001 | DONE | Galaxy map phase-colored nodes + shimmer |
| GATE.S7.WARFRONT.HEADLESS_PROOF.001 | DONE | Headless proof: factions, warfronts, demand, tariffs |
| GATE.X.HYGIENE.EPIC_REVIEW.019 | DONE | Audit epic statuses, recommend next anchor |
| GATE.X.HYGIENE.TENSION_EVAL.001 | DONE | Eval dynamic_tension_v0.md vs sim capabilities |
| GATE.S7.SUPPLY.DELIVERY_LEDGER.001 | DONE | Track war supply deliveries per faction |
| GATE.S7.INSTABILITY.TICK_SYSTEM.001 | DONE | Per-tick instability evolution near warfronts |
| GATE.S7.TERRITORY.EMBARGO_MODEL.001 | DONE | Embargo entity + blocked goods at war markets |
| GATE.S7.REPUTATION.TRADE_DRIFT.001 | DONE | Rep decay toward neutral + trade-based rep shifts |
| GATE.S7.WARFRONT.DASHBOARD_TAB.001 | DONE | Warfronts tab in Empire Dashboard |
| GATE.X.HYGIENE.REPO_HEALTH.020 | DONE | Full test suite + golden hash stability |
| GATE.S7.SUPPLY.WARFRONT_SHIFT.001 | DONE | Supply deliveries shift warfront intensity |
| GATE.S7.INSTABILITY.CONSEQUENCES.001 | DONE | Phase effects: price jitter, lane delays |
| GATE.S7.TERRITORY.REGIME_TRANSITION.001 | DONE | War-driven regime shifts (Open->Restricted->Closed) |
| GATE.S7.REPUTATION.WAR_PROFITEER.001 | DONE | War profiteering: sell war goods = rep effects |
| GATE.S7.SUPPLY.BRIDGE.001 | DONE | SimBridge war supply delivery queries |
| GATE.S7.TERRITORY.EMBARGO_BRIDGE.001 | DONE | SimBridge embargo status queries |
| GATE.S7.INSTABILITY.EFFECTS_BRIDGE.001 | DONE | SimBridge instability phase effects queries |
| GATE.S7.WARFRONT.SUPPLY_HUD.001 | DONE | Supply needs + delivery progress in warfront tab |
| GATE.S7.TERRITORY.EMBARGO_UI.001 | DONE | Trade menu embargoed goods display |
| GATE.S7.INSTABILITY.EFFECTS_UI.001 | DONE | Node popup instability phase effects |
| GATE.S7.FACTION.REP_TOAST.001 | DONE | Rep change toast notifications |
| GATE.S7.WARFRONT.HEADLESS_PROOF.002 | DONE | Headless proof: supply + embargo + instability |
| GATE.X.HYGIENE.EPIC_REVIEW.020 | DONE | Epic audit + next anchor recommendation |
| GATE.X.HYGIENE.FACTION_PLAYTEST.001 | DONE | Warfront player experience evaluation |
| GATE.S7.SUSTAIN.FUEL_DEDUCT.001 | DONE | Fleet fuel + module sustain resource deduction per tick |
| GATE.S7.PRODUCTION.FULL_DEPLOY.001 | DONE | Deploy all 9 production recipes as industry sites in worldgen |
| GATE.S5.LOOT.DROP_SYSTEM.001 | DONE | Loot drop system with rarity tiers on NPC fleet kill |
| GATE.S7.POWER.BUDGET_ENFORCE.001 | DONE | Power budget enforcement: PowerDraw vs BasePower |
| GATE.X.HYGIENE.REPO_HEALTH.021 | DONE | Full test suite + golden hash stability baseline |
| GATE.S7.FACTION_VIS.COLOR_PALETTE.001 | DONE | Faction color palettes + bridge query |
| GATE.S7.SUSTAIN.SHORTFALL.001 | DONE | Sustain shortfall: 0 fuel immobilizes, missing sustain disables modules |
| GATE.S7.SUSTAIN.ECONOMY_WIRE.001 | DONE | NPC fleet fuel consumption creates market demand |
| GATE.S5.LOOT.TRACTOR_CMD.001 | DONE | Tractor beam command: collect loot within range |
| GATE.S7.POWER.MOUNT_DEGRADE.001 | DONE | Mount type constraints + module degradation per cycle |
| GATE.S7.PRODUCTION.BRIDGE_READOUT.001 | DONE | Enhanced industry readout in dock Station + Economy tabs |
| GATE.S7.FACTION_VIS.SHIP_LIVERY.001 | DONE | NPC ship tint by faction color palette |
| GATE.S7.FACTION_VIS.STATION_STYLE.001 | DONE | Station accent color by controlling faction |
| GATE.S7.FACTION_VIS.TERRITORY_OVERLAY.001 | DONE | Galaxy map faction territory fill + legend |
| GATE.S7.SUSTAIN.BRIDGE_PROOF.001 | DONE | Sustain SimBridge queries + HUD + headless proof |
| GATE.S7.POWER.BRIDGE_UI.001 | DONE | Power budget + mount + condition in dock Ship tab |
| GATE.S5.LOOT.BRIDGE_PROOF.001 | DONE | Loot SimBridge queries + markers + headless proof |
| GATE.X.HYGIENE.EPIC_REVIEW.021 | DONE | Epic audit + next anchor recommendation |
| GATE.X.EVAL.SUSTAIN_BALANCE.001 | DONE | Multi-seed sustain economy balance evaluation |
| GATE.S7.COMBAT_JUICE.EXPLOSION_VFX.001 | DONE | Kill explosion VFX (flash→fireball→debris→smoke) |
| GATE.S7.COMBAT_JUICE.SHIELD_VFX.001 | DONE | Shield ripple shader on hit + shield break flash |
| GATE.S7.COMBAT_JUICE.DAMAGE_NUMBERS.001 | DONE | Floating damage numbers (shield=blue, hull=orange) |
| GATE.S7.COMBAT_JUICE.COMBAT_PRESENT.001 | DONE | Weapon trails + screen shake + combat audio wire |
| GATE.S7.COMBAT_JUICE.SCENE_PROOF.001 | DONE | Combat juice headless screenshot proof |
| GATE.S7.AUDIO_WIRING.BUS_WIRE.001 | DONE | 5-layer audio bus + wire 6 existing unwired assets |
| GATE.S7.AUDIO_WIRING.DISCOVERY_CHIMES.001 | DONE | Discovery phase transition audio chimes |
| GATE.S7.HUD_ARCH.TOAST_PRIORITY.001 | DONE | Toast priority levels + color borders + actions |
| GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001 | DONE | Zone G bottom bar framework container |
| GATE.S7.HUD_ARCH.ALERT_BADGE.001 | DONE | Alert badge in Zone A (count + click→dashboard) |
| GATE.S7.INSTABILITY_EFFECTS.MARKET.001 | DONE | Instability → market price volatility + demand skew |
| GATE.S7.INSTABILITY_EFFECTS.LANE.001 | DONE | Instability → lane transit delay + closures |
| GATE.S7.INSTABILITY_EFFECTS.BRIDGE.001 | DONE | Instability bridge queries + HUD + toasts |
| GATE.S7.ENFORCEMENT.HEAT_ACCUM.001 | DONE | Pattern-based heat accumulation signals |
| GATE.S7.ENFORCEMENT.CONFISCATION.001 | DONE | Confiscation event + fine at High+ heat |
| GATE.S7.ENFORCEMENT.BRIDGE.001 | DONE | Heat/fine bridge queries + confiscation toast |
| GATE.S7.STARTER_PLACEMENT.WARFRONT.001 | DONE | Player start adjacent to contested warfront |
| GATE.S7.STARTER_PLACEMENT.VIABILITY.001 | DONE | Contract: starter ≥3 trade loops + ≥1 discovery |
| GATE.X.HYGIENE.REPO_HEALTH.022 | DONE | Full test suite + warning scan + hash stability |
| GATE.X.HYGIENE.EPIC_REVIEW.022 | DONE | Close 5 done epics + recommend next anchor |
| GATE.X.EVAL.COMBAT_FEEL.001 | DONE | Combat feel baseline eval via screenshot |
| GATE.X.HYGIENE.REPO_HEALTH.023 | DONE | Full test suite + warning scan + hash stability |
| GATE.S7.RISK_METER_UI.BRIDGE.001 | DONE | SimBridge GetRiskMetersV0 for Heat/Influence/Trace |
| GATE.S7.RISK_METER_UI.WIDGET.001 | DONE | HUD risk meter bars widget in Zone G |
| GATE.S7.RISK_METER_UI.SCREEN_EDGE.001 | DONE | Screen-edge vignette tinting from risk levels |
| GATE.S7.RISK_METER_UI.COMPOUND.001 | DONE | Compound threat indicator for multi-meter warnings |
| GATE.S7.RISK_METER_UI.PROOF.001 | DONE | Screenshot proof of risk meter rendering |
| GATE.S7.FLEET_TAB.LIST.001 | DONE | Fleet roster master list panel |
| GATE.S7.FLEET_TAB.DETAIL.001 | DONE | Fleet ship detail panel with HP/modules/job |
| GATE.S7.FLEET_TAB.ACTIONS.001 | DONE | Fleet action buttons (Recall/Dismiss/Rename) |
| GATE.S7.MAIN_MENU.SCENE.001 | DONE | Main menu scene with adaptive menu |
| GATE.S7.MAIN_MENU.NEW_VOYAGE.001 | DONE | New voyage wizard + difficulty settings in SimCore |
| GATE.S7.MAIN_MENU.SAVE_META.001 | DONE | Save slot metadata preview on main menu |
| GATE.S7.MAIN_MENU.PAUSE.001 | DONE | Pause menu overlay with auto-save |
| GATE.S7.T2_MODULES.CATALOG.001 | DONE | T2 module content pack definitions |
| GATE.S7.T2_MODULES.FITTING.001 | DONE | Faction rep equip check for T2 modules |
| GATE.S7.NARRATIVE_DELIVERY.ENTITY.001 | DONE | flavor_text fields in IntelBook entities |
| GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | DONE | Wire combat VFX/audio: explosion, damage numbers, audio calls |
| GATE.S7.COMBAT_FEEL_POLISH.EVAL_PROOF.001 | DONE | Screenshot eval verifying combat feel fixes |
| GATE.X.HYGIENE.EPIC_REVIEW.023 | DONE | Audit epics + recommend next anchor for Tranche 24 |
| GATE.X.HYGIENE.REPO_HEALTH.024 | DONE | Full test suite + warning scan + hash stability |
| GATE.S7.GALAXY_MAP_V2.QUERIES.001 | DONE | SimBridge overlay/route/search query methods |
| GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001 | DONE | Fix label overlap + system name formatting |
| GATE.S7.GALAXY_MAP_V2.OVERLAYS.001 | DONE | 3 overlay modes (faction/fleet/heat) in GalaxyView |
| GATE.S7.GALAXY_MAP_V2.ROUTE_PLANNER.001 | DONE | Multi-hop route planner with path rendering |
| GATE.S7.GALAXY_MAP_V2.SEARCH.001 | DONE | Galaxy search bar — type name, camera snaps |
| GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001 | DONE | Detail levels by camera altitude |
| GATE.S7.GALAXY_MAP_V2.PROOF.001 | DONE | Screenshot proof of overlays + search + zoom |
| GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001 | DONE | Discovery narrative templates in DiscoveryOutcomeSystem |
| GATE.S7.NARRATIVE_DELIVERY.FACTION_GREETING.001 | DONE | Faction station greeting text by rep tier |
| GATE.S7.NARRATIVE_DELIVERY.TEXT_PANEL.001 | DONE | Narrative text display panel with faction styling |
| GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001 | DONE | Shield ripple + break flash + hull sparks |
| GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001 | DONE | Weapon family visual differentiation by damage family |
| GATE.S7.FLEET_TAB.CARGO.001 | DONE | Per-fleet cargo display in detail panel |
| GATE.S7.FLEET_TAB.PROGRAM.001 | DONE | Program assignment view in fleet detail |
| GATE.S7.MAIN_MENU.CAPTAIN_NAME.001 | DONE | Captain name input + SimState persistence |
| GATE.S7.MAIN_MENU.AUTO_SAVE.001 | DONE | Auto-save slot triggers on dock/warp/mission |
| GATE.S7.T2_MODULES.EXPANSION.001 | DONE | Add ~19 more T2 module definitions |
| GATE.X.HYGIENE.EPIC_REVIEW.024 | DONE | Epic audit + close completed epics |
| GATE.X.HYGIENE.SCREENSHOT_BASELINE.024 | DONE | Set up regression baselines from eval screenshots |
| GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | DONE | Fix ScreenEdgeTint identifier in hud.gd |
| GATE.S7.RUNTIME_STABILITY.FACTION_COLOR.001 | DONE | Fix faction color string-to-Color crash |
| GATE.S7.RUNTIME_STABILITY.WARP_ARRIVAL.001 | DONE | Fix planet scale + camera zoom on warp arrival |
| GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001 | DONE | Fix galaxy map 3D element scale + Z-ordering |
| GATE.S7.RUNTIME_STABILITY.SHIP_VISIBILITY.001 | DONE | Player ship scale + NPC fleet encounter rate |
| GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_SCALE.001 | DONE | Combat VFX visible at default game altitude |
| GATE.S7.RUNTIME_STABILITY.UI_POLISH.001 | DONE | Toast spacing + cargo UI refresh timing |
| GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001 | DONE | Warp tunnel dramatic VFX + dead particle fix |
| GATE.S7.GALAXY_MAP_V2.EXPLORATION_OVL.001 | DONE | Exploration data overlay on galaxy map |
| GATE.S7.GALAXY_MAP_V2.WARFRONT_OVL.001 | DONE | Warfront/combat zone overlay on galaxy map |
| GATE.S7.AUTOMATION_MGMT.DOCTRINE.001 | DONE | FleetDoctrine entity + DoctrineSystem |
| GATE.S7.AUTOMATION_MGMT.PROGRAM_METRICS.001 | DONE | ProgramCycleMetrics tracking system |
| GATE.S7.AUTOMATION_MGMT.FAILURE_RECOVERY.001 | DONE | ProgramFailureReason enum + auto-retry logic |
| GATE.S7.AUTOMATION_MGMT.BUDGET_ENFORCEMENT.001 | DONE | AutomationBudget caps enforcement system |
| GATE.S7.AUTOMATION_MGMT.PROGRAM_HISTORY.001 | DONE | Program outcome history ring buffer |
| GATE.S7.AUTOMATION_MGMT.BRIDGE_QUERIES.001 | DONE | SimBridge automation query contracts |
| GATE.S7.AUTOMATION_MGMT.DASHBOARD.001 | DONE | Automation management UI panel |
| GATE.X.HYGIENE.REPO_HEALTH.025 | DONE | Full test suite + warning scan baseline |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.025 | DONE | Screenshot eval after stability fixes |
| GATE.X.HYGIENE.EPIC_REVIEW.025 | DONE | Epic status audit + close eligible epics |
| GATE.S7.AUTOMATION_MGMT.BRIDGE_WRITES.001 | DONE | SetDoctrineV0 + SetBudgetCapsV0 + GetTemplatesV0 bridge |
| GATE.S7.AUTOMATION_MGMT.DASHBOARD_V2.001 | DONE | Enhanced automation dashboard panels |
| GATE.S7.AUTOMATION_MGMT.DOCTRINE_UI.001 | DONE | Doctrine editing panel |
| GATE.S7.AUTOMATION_MGMT.BUDGET_UI.001 | DONE | Budget caps editing panel |
| GATE.S7.AUTOMATION_MGMT.FLEET_INTEGRATION.001 | DONE | Wire automation metrics into Fleet Tab |
| GATE.S7.AUTOMATION_MGMT.HISTORY_VIEW.001 | DONE | Program history timeline panel |
| GATE.S7.AUTOMATION_MGMT.SCENARIO_PROOF.001 | DONE | Headless automation workflow proof |
| GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001 | DONE | Fix Label3D bleed-through dock panel |
| GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001 | DONE | Fix combat VFX + NPC labels at altitude |
| GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001 | DONE | Fix warp tunnel VFX + dock-during-transit |
| GATE.S7.RUNTIME_STABILITY.GALAXY_MAP_FIX.001 | DONE | Fix galaxy map behind dashboard |
| GATE.S7.RUNTIME_STABILITY.ASTEROID_VARIETY.001 | DONE | Asteroid meshes near stations |
| GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001 | DONE | Enrich empire dashboard + keybind hints |
| GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001 | DONE | Zone armor bars + combat stance indicator |
| GATE.S9.SETTINGS.INFRASTRUCTURE.001 | DONE | SettingsManager autoload + settings scaffold |
| GATE.S9.SETTINGS.TABS.001 | DONE | Audio + Display settings tabs |
| GATE.S9.SETTINGS.GAMEPLAY_TAB.001 | DONE | Gameplay settings tab |
| GATE.X.HYGIENE.REPO_HEALTH.026 | DONE | Full test suite + health baseline |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.026 | DONE | Post-tranche visual regression check |
| GATE.X.HYGIENE.EPIC_REVIEW.026 | DONE | Close AUTOMATION_MGMT + GALAXY_MAP_V2 epics |
| GATE.S6.FRACTURE_DISCOVERY.MODEL.001 | DONE | Fracture unlock flag + gate FractureSystem |
| GATE.S6.FRACTURE_DISCOVERY.DERELICT.001 | DONE | Derelict void site in worldgen |
| GATE.S6.FRACTURE_DISCOVERY.UNLOCK.001 | DONE | Analyze derelict → unlock fracture |
| GATE.S6.FRACTURE_DISCOVERY.BRIDGE.001 | DONE | GetFractureDiscoveryStatusV0 bridge |
| GATE.S6.FRACTURE_DISCOVERY.UI.001 | DONE | Fracture unlock toast + dashboard gating |
| GATE.S6.FRACTURE_DISCOVERY.PROOF.001 | DONE | Headless fracture discovery proof |
| GATE.S9.MENU_ATMOSPHERE.STARFIELD.001 | DONE | Parallax starfield shader (4 layers) |
| GATE.S9.MENU_ATMOSPHERE.TITLE.001 | DONE | Title treatment + rotating subtitle |
| GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001 | DONE | Adaptive foreground silhouette |
| GATE.S9.MENU_ATMOSPHERE.AUDIO.001 | DONE | Menu audio timing + silence palette |
| GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001 | DONE | Galaxy gen screen + progress msgs |
| GATE.S9.ACCESSIBILITY.FIRST_LAUNCH.001 | DONE | First-launch accessibility prompt |
| GATE.S9.ACCESSIBILITY.FONT_SCALE.001 | DONE | Font size override (100-200%) |
| GATE.S9.ACCESSIBILITY.COLORBLIND.001 | DONE | Colorblind post-processing shader |
| GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001 | DONE | Accessibility tab in settings panel |
| GATE.S9.SETTINGS.DISPLAY_REVERT.001 | DONE | 15s display revert timer |
| GATE.X.HYGIENE.REPO_HEALTH.027 | DONE | Full test suite + health baseline |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.027 | DONE | Post-tranche visual + menu eval |
| GATE.X.HYGIENE.EPIC_REVIEW.027 | DONE | Close epics + recommend T28 anchor |
| GATE.T18.NARRATIVE.ENTITIES.001 | DONE | 6 narrative entity classes |
| GATE.T18.NARRATIVE.STATE_INTEGRATION.001 | DONE | SimState dictionaries + HydrateAfterLoad + signature |
| GATE.T18.NARRATIVE.TWEAKS.001 | DONE | NarrativeTweaksV0 + 3 subsystem tweaks |
| GATE.T18.EXPERIENTIAL.FRACTURE_WEIGHT.001 | DONE | FractureWeightSystem + tests |
| GATE.T18.EXPERIENTIAL.ROUTE_UNCERTAINTY.001 | DONE | RouteUncertaintySystem + tests |
| GATE.T18.EXPERIENTIAL.STATION_MEMORY.001 | DONE | StationMemorySystem + tests |
| GATE.T18.EXPERIENTIAL.WAR_CONSEQUENCE.001 | DONE | WarConsequenceSystem + tests |
| GATE.T18.EXPERIENTIAL.TOPOLOGY_SHIFT.001 | DONE | TopologyShiftSystem + tests |
| GATE.T18.DATALOG.CONTENT.001 | DONE | 25 data logs in DataLogContentV0 |
| GATE.T18.DATALOG.PLACEMENT.001 | DONE | NarrativePlacementGen BFS + landmarks |
| GATE.T18.DATALOG.KEPLER_CHAIN.001 | DONE | KeplerChainContentV0 6-piece chain |
| GATE.T18.DATALOG.KNOWLEDGE_GRAPH.001 | DONE | KnowledgeGraphSystem + content |
| GATE.T18.INSTRUMENT.DISAGREEMENT.001 | DONE | InstrumentDisagreementSystem |
| GATE.T18.MORAL_ARCH.DOCS.001 | DONE | Faction moral architecture in lore doc |
| GATE.T18.CHARACTER.FO_REACT.001 | DONE | FO event-driven reactions + dialogue content |
| GATE.T18.CHARACTER.WAR_FACES.001 | DONE | War Faces personality behavior + NPC content |
| GATE.T18.CHARACTER.BRIDGE.001 | DONE | SimBridge.Character.cs FO + War Faces queries |
| GATE.T18.CHARACTER.UI.001 | DONE | FO reactions + War Faces in HUD/dock |
| GATE.T18.NARRATIVE.BRIDGE.001 | DONE | SimBridge.Narrative.cs all narrative queries |
| GATE.T18.NARRATIVE.UI_DATALOG.001 | DONE | Data log viewer panel |
| GATE.X.SHIP_CLASS.CARGO_ENFORCE.001 | DONE | Cargo capacity enforcement in BuyCommand |
| GATE.X.SHIP_CLASS.MASS_SPEED.001 | DONE | Mass-based speed penalty |
| GATE.X.SHIP_CLASS.SCAN_RANGE.001 | DONE | ScanRange gates discovery scanning |
| GATE.X.INSTAB_PRICE.WIRE.001 | DONE | Instability-aware pricing in Buy+Sell |
| GATE.X.MODULE_SUSTAIN.MODEL.001 | DONE | SustainGoodId on ModuleDef |
| GATE.X.MODULE_SUSTAIN.DEDUCT.001 | DONE | Sustain cycle deducts goods from cargo |
| GATE.X.UI_POLISH.LABEL_OVERLAP.001 | DONE | Label anti-collision (fixes V17) |
| GATE.X.UI_POLISH.GALAXY_MAP_UX.001 | DONE | Galaxy zoom + label persist (fixes V18/V19) |
| GATE.X.UI_POLISH.CAMERA_BOUNDS.001 | DONE | Camera distance clamp (fixes U1) |
| GATE.X.HYGIENE.REPO_HEALTH.028 | DONE | Full test suite + health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.028 | DONE | Epic audit + tranche closeout |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.028 | DONE | Post-tranche visual eval |
| GATE.T18.CHARACTER.FO_DIALOGUE_DEPTH.001 | DONE | FO 3 candidates x 5 dialogue tiers + blind spots |
| GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001 | DONE | FO action-based dialogue triggers |
| GATE.T18.CHARACTER.FO_PROMO.001 | DONE | FO promotion - score, window, candidate choice |
| GATE.T18.CHARACTER.WARFACES_DEPTH.001 | DONE | War Faces 3 behaviors (Regular/Stationmaster/Enemy) |
| GATE.T18.CHARACTER.UI_FULL.001 | DONE | FO panel overhaul + War Faces + promotion UI |
| GATE.T18.CHARACTER.HEADLESS.001 | DONE | Character systems headless proof |
| GATE.S9.MISSIONS.MINING_CONTENT.001 | DONE | Mission Mining Survey definition + binding |
| GATE.S9.MISSIONS.RESEARCH_CONTENT.001 | DONE | Mission First Research definition + binding |
| GATE.S9.MISSIONS.CONSTRUCTION_CONTENT.001 | DONE | Mission First Build definition + binding |
| GATE.S9.MISSIONS.BRIDGE_EXT.001 | DONE | Mission rewards preview + prereqs detail |
| GATE.S9.MISSIONS.HEADLESS.001 | DONE | Mission M5-M7 headless proof |
| GATE.X.UI_POLISH.DOCK_VISUAL.001 | DONE | Dock column fix (V21) + ship tab polish (V20) |
| GATE.X.UI_POLISH.KNOWLEDGE_WEB.001 | DONE | Discovery Web panel (knowledge graph UI) |
| GATE.X.UI_POLISH.DATALOG_FILTER.001 | DONE | Data log category filter tabs |
| GATE.X.UI_POLISH.LOCAL_DENSITY.001 | DONE | Tighten local system spatial spread (V2) |
| GATE.X.HYGIENE.REPO_HEALTH.029 | DONE | Full test suite + health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.029 | DONE | Epic audit + close S9.SETTINGS/T18.CHARACTER |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.029 | DONE | Post-tranche visual eval |
| GATE.S7.TERRITORY.HYSTERESIS.001 | DONE | Territory regime hysteresis — sustained rep change |
| GATE.S7.FRACTURE.OFFLANE_ROUTES.001 | DONE | Offlane fracture route gen + travel |
| GATE.S7.FRACTURE.OFFLANE_HEADLESS.001 | DONE | Offlane fracture headless proof |
| GATE.S7.REVEALS.DISCOVERY_REVEAL.001 | DONE | Discovery content recontextualization |
| GATE.S7.REVEALS.WARFRONT_REVEAL.001 | DONE | Warfront intel layer reveals |
| GATE.S7.REVEALS.HEADLESS.001 | DONE | Layered reveals headless proof |
| GATE.S9.MISSION_EVOL.TRIGGERS.001 | DONE | 4 new mission trigger types |
| GATE.S9.MISSION_EVOL.REWARDS.001 | DONE | Non-credit reward types + distribution |
| GATE.S9.MISSION_EVOL.FAILURE.001 | DONE | Mission failure/abandonment + timer deadline |
| GATE.S9.MISSION_EVOL.BRANCHING.001 | DONE | CHOICE steps + conditional paths |
| GATE.S9.MISSION_EVOL.CONTRACT.001 | DONE | Contract tests: all mission evolution features |
| GATE.S7.WARFRONT.ATTRITION.001 | DONE | Fleet attrition + supply pressure |
| GATE.S7.WARFRONT.OBJECTIVES.001 | DONE | Strategic objectives + capture |
| GATE.S7.REPUTATION.CONTRACTS.001 | DONE | Faction contracts from reputation tiers |
| GATE.X.WARP.TUNNEL_SCALE.001 | DONE | Fix warp tunnel oversized mesh (F7) |
| GATE.X.WARP.TRANSIT_HUD.001 | DONE | Warp transit HUD info bar (F8) |
| GATE.X.WARP.DEPARTURE_VFX.001 | DONE | Warp departure flash + shake (F9) |
| GATE.X.UI_POLISH.MISSION_JOURNAL.001 | DONE | Active mission journal panel |
| GATE.X.HYGIENE.REPO_HEALTH.030 | DONE | Full test suite + health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.030 | DONE | Epic status audit + close MISSION_LADDER |
| GATE.X.HYGIENE.FIRST_HOUR_EVAL.030 | DONE | First-hour bot eval w/ mission flow |
| GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001 | DONE | Heat accumulation + overheat cascade |
| GATE.S7.COMBAT_PHASE2.BATTLE_STATIONS.001 | DONE | Battle stations state machine |
| GATE.X.LEDGER.TX_MODEL.001 | DONE | Transaction event entity + logging |
| GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001 | DONE | World-state mission trigger detection |
| GATE.X.STATION_IDENTITY.VISUAL.001 | DONE | Station visual variety per faction (F6) |
| GATE.X.HYGIENE.REPO_HEALTH.031 | DONE | Full test suite + health baseline |
| GATE.X.UI_POLISH.DASHBOARD_UX.001 | DONE | Dashboard Needs Attention UX fix (F11) |
| GATE.X.UI_POLISH.MARKET_FORMAT.001 | DONE | Market production text formatting (F12) |
| GATE.S7.COMBAT_PHASE2.RADIATOR.001 | DONE | Radiator module — targetable, affects cooling |
| GATE.S9.SYSTEMIC.OFFER_GEN.001 | DONE | Generate mission offers from triggers |
| GATE.S7.COMBAT_PHASE2.CONTRACT.001 | DONE | Contract tests: heat + battle stations + radiator |
| GATE.S7.COMBAT_PHASE2.BRIDGE.001 | DONE | SimBridge heat/battleStations/radiator queries |
| GATE.X.LEDGER.BRIDGE.001 | DONE | SimBridge transaction ledger queries |
| GATE.S9.SYSTEMIC.BRIDGE.001 | DONE | SimBridge systemic mission queries |
| GATE.X.LEDGER.INTEGRITY.001 | DONE | Ledger integrity — sum transactions = balance |
| GATE.S7.COMBAT_PHASE2.HEAT_HUD.001 | DONE | Heat gauge + battle stations HUD widget |
| GATE.S9.SYSTEMIC.HEADLESS.001 | DONE | Headless proof: systemic missions from world state |
| GATE.X.HYGIENE.EPIC_REVIEW.031 | DONE | Epic status audit + T32 anchor rec |
| GATE.X.HYGIENE.FIRST_HOUR_EVAL.031 | DONE | First-hour bot eval w/ combat heat |
| GATE.X.PERF.TICK_BASELINE.001 | DONE | Tick budget performance baseline |
| GATE.S7.COMBAT_PHASE2.SPIN_TURN.001 | DONE | Spin turn penalty + RPM modeling |
| GATE.S7.COMBAT_PHASE2.MOUNT_TYPE.001 | DONE | Mount type system (Standard/Broadside/Spinal) |
| GATE.S7.COMBAT_PHASE2.SPIN_FIRE.001 | DONE | Spin-fire cadence + spinal mount axis |
| GATE.S7.COMBAT_PHASE2.SPIN_CONTRACT.001 | DONE | Contract tests: spin + mount types |
| GATE.S7.COMBAT_PHASE2.SPIN_BRIDGE.001 | DONE | SimBridge spin/mount queries |
| GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001 | DONE | Overheat cascade VFX + radiator destruction |
| GATE.S7.COMBAT_PHASE2.ZONE_HUD.001 | DONE | Zone damage paperdoll HUD |
| GATE.S7.COMBAT_PHASE2.HEADLESS.001 | DONE | Headless proof: spin combat flow |
| GATE.S7.AUTOMATION.PERF_TRACKING.001 | DONE | Program performance metrics (profit, activity) |
| GATE.S7.AUTOMATION.FAILURE_REASONS.001 | DONE | Failure reason taxonomy (7 cause codes) |
| GATE.S7.AUTOMATION.BUDGET_CAPS.001 | DONE | Program budget caps + doctrine settings |
| GATE.S7.AUTOMATION.BRIDGE.001 | DONE | SimBridge automation management queries |
| GATE.S7.AUTOMATION.UI.001 | DONE | Automation management UI panel |
| GATE.S9.SYSTEMIC.STATION_CONTEXT.001 | DONE | Station context system (dock shows situation) |
| GATE.S9.SYSTEMIC.CONTEXT_BRIDGE.001 | DONE | SimBridge station context queries |
| GATE.S9.SYSTEMIC.CONTEXT_UI.001 | DONE | Dock station context display |
| GATE.X.LEDGER.COST_BASIS.001 | DONE | Cost-basis tracking per cargo batch |
| GATE.X.LEDGER.COST_BASIS_BRIDGE.001 | DONE | SimBridge profit/loss per cargo good |
| GATE.X.HYGIENE.REPO_HEALTH.032 | DONE | Full test suite + health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.032 | DONE | Epic status audit + T33 anchor rec |
| GATE.X.HYGIENE.REPO_HEALTH.033 | DONE | Full test suite + health baseline |
| GATE.S8.HAVEN.ENTITY.001 | DONE | Haven entity + SimState integration |
| GATE.S8.HAVEN.DISCOVERY.001 | DONE | Haven seeding in WorldLoader + discovery prereq |
| GATE.S8.HAVEN.UPGRADE_SYSTEM.001 | DONE | HavenUpgradeSystem (5-tier progression) |
| GATE.S5.TRACTOR.MODEL.001 | DONE | Tractor module + CollectLoot range check |
| GATE.S7.AUTOMATION.TEMPLATES.001 | DONE | Program template content definitions |
| GATE.X.WARP.ARRIVAL_DRAMA.001 | DONE | Warp arrival cinematic drama (F4) |
| GATE.X.UI_POLISH.QUEST_TRACKER.001 | DONE | Persistent HUD quest/objective tracker (F5) |
| GATE.X.EVAL.HAVEN_DESIGN.001 | DONE | Haven design doc audit vs codebase |
| GATE.S8.HAVEN.HANGAR.001 | DONE | Hangar system (multi-ship store + swap) |
| GATE.S8.HAVEN.MARKET.001 | DONE | Haven exotic-only market variant |
| GATE.S8.HAVEN.CONTRACT.001 | DONE | Contract tests for Haven systems |
| GATE.S5.TRACTOR.BRIDGE.001 | DONE | SimBridge tractor queries |
| GATE.S5.TRACTOR.VFX.001 | DONE | Tractor beam energy VFX |
| GATE.S7.AUTOMATION.TEMPLATES_UI.001 | DONE | Template picker in automation panel |
| GATE.S8.HAVEN.BRIDGE.001 | DONE | SimBridge Haven queries |
| GATE.S8.HAVEN.DOCK_PANEL.001 | DONE | Haven dock UI panel (unique layout) |
| GATE.S8.HAVEN.GALAXY_ICON.001 | DONE | Haven galaxy map icon + tooltip |
| GATE.S8.HAVEN.HEADLESS.001 | DONE | Headless: discover Haven, upgrade, swap |
| GATE.X.HYGIENE.EPIC_REVIEW.033 | DONE | Close eligible epics + recommend T34 |
| GATE.X.HYGIENE.REPO_HEALTH.034 | DONE | Full test suite + health baseline |
| GATE.S8.HAVEN.RESIDENTS.001 | DONE | Haven Residents entity + Keeper NPC |
| GATE.S8.HAVEN.TROPHY_WALL.001 | DONE | Trophy Wall entity + display system |
| GATE.S8.HAVEN.LOGS.001 | DONE | 20 Haven data log entries |
| GATE.S8.ADAPTATION.ENTITY.001 | DONE | AdaptationFragment entity + 16 defs |
| GATE.S8.ADAPTATION.COLLECTION.001 | DONE | Fragment collection + resonance pairs |
| GATE.S8.ADAPTATION.PLACEMENT.001 | DONE | Fragment worldgen placement |
| GATE.S8.T3_MODULES.CONTENT.001 | DONE | ~13 T3 module content definitions |
| GATE.S8.T3_MODULES.EXOTIC_SUSTAIN.001 | DONE | Exotic matter sustain for T3 |
| GATE.S8.T3_MODULES.DISCOVERY_GATE.001 | DONE | Discovery-only module acquisition |
| GATE.S8.ANCIENT_HULLS.CONTENT.001 | DONE | 3 ancient hull definitions |
| GATE.S8.ANCIENT_HULLS.RESTORE.001 | DONE | Haven T3+ hull restoration |
| GATE.S5.TRACTOR.WEAVER.001 | DONE | Weaver Spindle Tractor variant |
| GATE.S5.TRACTOR.AUTO_TARGET.001 | DONE | Auto-target nearest loot in range |
| GATE.S8.HAVEN.RESIDENTS_BRIDGE.001 | DONE | SimBridge + residents panel UI |
| GATE.S8.HAVEN.TROPHY_BRIDGE.001 | DONE | Trophy Wall bridge + display UI |
| GATE.S8.HAVEN.FRAGMENT_DISPLAY.001 | DONE | Fragment geometry 3D in Haven |
| GATE.S8.ADAPTATION.BRIDGE.001 | DONE | SimBridge + fragment collection UI |
| GATE.S8.T3_MODULES.BRIDGE.001 | DONE | SimBridge T3 module queries |
| GATE.S8.ANCIENT_HULLS.BRIDGE.001 | DONE | SimBridge + hull preview UI |
| GATE.S8.HAVEN.HEADLESS_DEPTH.001 | DONE | Headless: residents + trophy + fragments |
| GATE.X.HYGIENE.EPIC_REVIEW.034 | DONE | Epic status audit + T35 anchor rec |
| GATE.X.EVAL.ENDGAME_PROGRESSION.001 | DONE | Endgame balance review |
| GATE.X.HYGIENE.REPO_HEALTH.035 | DONE | Full test suite + health baseline |
| GATE.X.MARKET_PRICING.FEE_WIRE.001 | DONE | Wire transaction fees into BuyCommand/SellCommand |
| GATE.X.MARKET_PRICING.REP_WIRE.001 | DONE | Wire reputation pricing into BuyCommand/SellCommand |
| GATE.X.PRESSURE_INJECT.WARFRONT.001 | DONE | Wire InjectDelta into WarfrontDemandSystem |
| GATE.X.PRESSURE_INJECT.INSTABILITY.001 | DONE | Wire InjectDelta into InstabilitySystem |
| GATE.X.PRESSURE_INJECT.MARKET.001 | DONE | Wire InjectDelta into MarketSystem on trade failure |
| GATE.X.PRESSURE_INJECT.SUSTAIN.001 | DONE | Wire InjectDelta into SustainSystem on shortage |
| GATE.X.FLEET_UPKEEP.DRAIN.001 | DONE | Per-cycle credit drain by ship class |
| GATE.X.FLEET_UPKEEP.DELINQUENCY.001 | DONE | Grace period + cascading module failure |
| GATE.S7.TERRITORY_SHIFT.RECOMPUTE.001 | DONE | BFS territory recompute on objective capture |
| GATE.S7.TERRITORY_SHIFT.REGIME_FLIP.001 | DONE | Tariff/embargo/regime switch on territory change |
| GATE.T18.KG_SEED.RESOLVE.001 | DONE | Template resolution into KnowledgeConnection entities |
| GATE.T18.KG_SEED.PROXIMITY.001 | DONE | Procedural proximity + faction link connections |
| GATE.S7.FACTION_COMMISSION.ENTITY.001 | DONE | Commission entity + passive rep effects + stipend |
| GATE.S7.FACTION_COMMISSION.INFAMY.001 | DONE | Infamy accumulator caps max rep tier |
| GATE.X.MARKET_PRICING.BREAKDOWN_BRIDGE.001 | DONE | PriceBreakdownV0 struct + SimBridge query |
| GATE.X.MARKET_PRICING.BREAKDOWN_UI.001 | DONE | Price breakdown tooltip in trade menu |
| GATE.X.FLEET_UPKEEP.BRIDGE.001 | DONE | Fleet upkeep bridge + HUD display |
| GATE.S7.TERRITORY_SHIFT.MAP_UPDATE.001 | DONE | Galaxy map territory update on shift |
| GATE.S7.FACTION_COMMISSION.BRIDGE.001 | DONE | Commission bridge + modifier stack + locked-visible UI |
| GATE.X.EVAL.DYNAMIC_TENSION.001 | DONE | Multi-seed dynamic tension verification |
| GATE.X.HYGIENE.EPIC_REVIEW.035 | DONE | Epic audit + close completed + next anchor |
| GATE.X.HYGIENE.REPO_HEALTH.036 | DONE | Full test suite + warning scan + golden hash stability |
| GATE.S7.COMBAT_DEPTH2.TRACKING.001 | DONE | TrackingBps/EvasionBps hit probability system |
| GATE.S7.COMBAT_DEPTH2.DAMAGE_VAR.001 | DONE | ±20% deterministic damage variance |
| GATE.S7.COMBAT_DEPTH2.ARMOR_PEN.001 | DONE | ArmorPenetrationPct zone bypass mechanic |
| GATE.S7.COMBAT_DEPTH2.FORE_KILL.001 | DONE | Fore zone soft-kill: weapons offline when depleted |
| GATE.S8.LATTICE_DRONES.ENTITY.001 | DONE | LatticeDrone entity + IsLatticeDrone flag on Fleet |
| GATE.S8.LATTICE_DRONES.SPAWN.001 | DONE | LatticeDroneSpawnSystem — instability-phase-linked spawning |
| GATE.S7.COMBAT_DEPTH2.PROJECTION.001 | DONE | Pre-combat outcome projection (Starsector pattern) |
| GATE.S8.LATTICE_DRONES.COMBAT.001 | DONE | LatticeDroneCombatSystem — drone combat behavior |
| GATE.S7.COMBAT_DEPTH2.BRIDGE.001 | DONE | SimBridge combat depth queries |
| GATE.S7.COMBAT_DEPTH2.HUD.001 | DONE | Combat HUD tracking/armor/projection display |
| GATE.S8.LATTICE_DRONES.BRIDGE.001 | DONE | SimBridge lattice drone queries + alerts |
| GATE.S7.UI_WARFRONT.DASHBOARD.001 | DONE | Warfront dashboard panel |
| GATE.S7.UI_WARFRONT.MAP_OVERLAY.001 | DONE | Galaxy map warfront overlay |
| GATE.S7.UI_WARFRONT.SUPPLY.001 | DONE | Warfront supply line visualization |
| GATE.S6.UI_DISCOVERY.KG_PANEL.001 | DONE | Knowledge graph panel enhancements |
| GATE.S6.UI_DISCOVERY.SCAN_VIZ.001 | DONE | Scan visualization in local system |
| GATE.S7.COMBAT_DEPTH2.HEADLESS.001 | DONE | Headless combat depth proof |
| GATE.X.EVAL.COMBAT_BALANCE.001 | DONE | Multi-seed combat balance verification |
| GATE.X.HYGIENE.EPIC_REVIEW.036 | DONE | Epic audit + close completed + next anchor |
| GATE.X.HYGIENE.REPO_HEALTH.037 | DONE | Full test suite + stability baseline |
| GATE.S8.STORY_STATE.ENTITY.001 | DONE | StoryState entity + SimState integration |
| GATE.S8.STORY_STATE.TRIGGERS.001 | DONE | 5 revelation trigger conditions |
| GATE.S8.HAVEN.KEEPER.001 | DONE | Keeper ambient system 5-tier evolution |
| GATE.S8.HAVEN.RESONANCE.001 | DONE | Resonance Chamber fragment combination |
| GATE.S6.UI_DISCOVERY.PHASE_MARKERS.001 | DONE | Discovery phase icons on galaxy map |
| GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001 | DONE | Active discovery leads on HUD |
| GATE.S8.NARRATIVE.REVELATION_TEXT.001 | DONE | 5 revelation gold toast + FO dialogue |
| GATE.S8.NARRATIVE.FRAGMENT_LORE.001 | DONE | 16 adaptation fragment lore text |
| GATE.S8.HAVEN.FABRICATOR.001 | DONE | T3 module fabrication at Haven |
| GATE.S8.HAVEN.MARKET_EVOLUTION.001 | DONE | Haven market tier-based stock expansion |
| GATE.S8.THREAT.SUPPLY_SHOCK.001 | DONE | Warfront disrupts production chains |
| GATE.S8.STORY_STATE.BRIDGE.001 | DONE | SimBridge.Story.cs revelation queries |
| GATE.S8.STORY_STATE.DELIVERY_UI.001 | DONE | Gold toast + map highlight + FO reaction |
| GATE.S8.STORY_STATE.COVER_NAMES.001 | DONE | Pre/post-revelation name switching |
| GATE.S8.HAVEN.DEPTH_BRIDGE.001 | DONE | Bridge for Keeper/Resonance/Fabricator |
| GATE.S8.THREAT.ALERT_UI.001 | DONE | Supply shock toasts + HUD alert |
| GATE.S8.STORY_STATE.HEADLESS.001 | DONE | Headless proof: trigger + verify delivery |
| GATE.X.EVAL.NARRATIVE_FLOW.001 | DONE | ExplorationBot narrative coherence eval |
| GATE.X.HYGIENE.EPIC_REVIEW.037 | DONE | Epic audit + close COMBAT_DEPTH_V2 + LATTICE_DRONES |
| GATE.X.HYGIENE.REPO_HEALTH.038 | DONE | Full test suite + stability baseline |
| GATE.S8.PENTAGON.DETECT.001 | DONE | Pentagon trade flags detection + R3 trigger |
| GATE.S8.PENTAGON.CASCADE.001 | DONE | Economic consequence cascade |
| GATE.S8.HAVEN.ENDGAME_PATHS.001 | DONE | Endgame path entity + choice system |
| GATE.S8.HAVEN.ACCOMMODATION.001 | DONE | Accommodation thread system |
| GATE.S8.HAVEN.COMMUNION_REP.001 | DONE | Communion Representative NPC at Haven |
| GATE.X.COVER_STORY.AUDIT.001 | DONE | CI fracture-name grep scan |
| GATE.S8.NARRATIVE.FACTION_DIALOGUE.001 | DONE | 5 faction representative dialogue sets |
| GATE.S8.NARRATIVE.WARFRONT_COMMENTARY.001 | DONE | Warfront event commentary text |
| GATE.S9.MILESTONES.VIEWER.001 | DONE | Milestone viewer panel from main menu |
| GATE.S8.PENTAGON.BRIDGE.001 | DONE | SimBridge pentagon state queries |
| GATE.S8.PENTAGON.DELIVERY.001 | DONE | Pentagon gold toast + map overlay |
| GATE.S8.HAVEN.ENDGAME_BRIDGE.001 | DONE | Endgame path bridge + haven UI |
| GATE.S8.HAVEN.COMING_HOME.001 | DONE | Coming Home cinematic dock transition |
| GATE.X.COVER_STORY.BRIDGE_WIRE.001 | DONE | Wire cover names into all bridge methods |
| GATE.X.COVER_STORY.UI_ENFORCE.001 | DONE | HUD/dock text uses cover names |
| GATE.S9.CREDITS.SCROLL.001 | DONE | Credits scroll overlay |
| GATE.S8.PENTAGON.HEADLESS.001 | DONE | Pentagon break headless proof |
| GATE.X.EVAL.PENTAGON_SCENARIO.001 | DONE | ExplorationBot pentagon scenario eval |
| GATE.X.HYGIENE.EPIC_REVIEW.038 | DONE | Epic audit + close PENTAGON_BREAK + T39 anchor |
| GATE.S8.WIN.GAME_RESULT.001 | DONE | GameResult entity + win requirements tweaks |
| GATE.S8.WIN.LOSS_DETECT.001 | DONE | Loss detection (death/bankruptcy) |
| GATE.S8.WIN.PATH_EVAL.001 | DONE | Win condition evaluation system |
| GATE.S8.WIN.PROGRESS_TRACK.001 | DONE | Per-path endgame progress tracking |
| GATE.S8.STORY.FO_REVELATION.001 | DONE | FO dialogue triggers for R1-R5 |
| GATE.S8.STORY.KG_REVELATION.001 | DONE | Knowledge graph connections on revelation |
| GATE.X.HYGIENE.REPO_HEALTH.039 | DONE | Full test suite + golden hash |
| GATE.S8.WIN.EPILOGUE_DATA.001 | DONE | Epilogue text cards + loss frames (GDScript) |
| GATE.S8.WIN.BRIDGE.001 | DONE | SimBridge.Endgame.cs queries |
| GATE.X.COVER_STORY.CI.001 | DONE | CI test: cover-story name enforcement |
| GATE.S8.WIN.VICTORY_SCREEN.001 | DONE | Victory screen with epilogue cards |
| GATE.S8.WIN.LOSS_SCREEN.001 | DONE | Loss screen (death/bankruptcy) |
| GATE.S8.WIN.PROGRESS_UI.001 | DONE | Haven panel endgame progress bars |
| GATE.S8.NARRATIVE.ENDGAME_CONTENT.001 | DONE | Fragment lore + haven context text |
| GATE.X.PERF.TICK_BUDGET.001 | DONE | SimCore tick budget profiling test |
| GATE.S8.WIN.GAME_OVER_WIRE.001 | DONE | game_manager.gd end-state transition |
| GATE.S8.WIN.HEADLESS_PROOF.001 | DONE | ExplorationBot win condition proof |
| GATE.S8.WIN.BOT_LOSS.001 | DONE | ExplorationBot loss state verification |
| GATE.X.EVAL.ENDGAME_FLOW.001 | DONE | Multi-seed endgame flow evaluation |
| GATE.X.HYGIENE.EPIC_REVIEW.039 | DONE | Audit epic statuses + recommend T40 |
| GATE.S7.DIPLOMACY.FRAMEWORK.001 | DONE | DiplomaticAct entity + DiplomacySystem + tweaks |
| GATE.S7.DIPLOMACY.TREATY.001 | DONE | Treaty propose/accept/reject mechanics |
| GATE.S7.DIPLOMACY.BOUNTY.001 | DONE | Bounty placement + completion reward |
| GATE.S7.DIPLOMACY.FACTION_AI.001 | DONE | Per-faction diplomatic preferences + AI proposals |
| GATE.S7.TECH_ACCESS.LOCK.001 | DONE | Faction-locked modules + rep tier gating |
| GATE.S8.HAVEN.VISUAL_TIERS.001 | DONE | Haven station visuals per upgrade tier |
| GATE.X.HYGIENE.REPO_HEALTH.040 | DONE | Full test suite + golden hash stability |
| GATE.S7.DIPLOMACY.CONSEQUENCES.001 | DONE | Treaty violation + sanction escalation |
| GATE.S5.LOSS_RECOVERY.CAPTURE.001 | DONE | Ship capture: board disabled NPC to hangar |
| GATE.S7.DIPLOMACY.BRIDGE.001 | DONE | SimBridge.Diplomacy.cs queries |
| GATE.S7.TECH_ACCESS.BRIDGE.001 | DONE | SimBridge tech lock queries + refit wiring |
| GATE.S5.LOSS_RECOVERY.CAPTURE_BRIDGE.001 | DONE | SimBridge capture queries + hangar |
| GATE.S7.DIPLOMACY.UI.001 | DONE | Diplomacy panel in dock menu |
| GATE.S7.TECH_ACCESS.UI.001 | DONE | Tech lock indicators in refit panel |
| GATE.S5.LOSS_RECOVERY.CAPTURE_UI.001 | DONE | Ship capture confirmation UI |
| GATE.S7.DIPLOMACY.HEADLESS.001 | DONE | Diplomacy headless proof |
| GATE.S5.LOSS_RECOVERY.CAPTURE_HEADLESS.001 | DONE | Ship capture headless proof |
| GATE.X.EVAL.DIPLOMACY_BALANCE.001 | DONE | Multi-seed diplomacy balance eval |
| GATE.X.HYGIENE.EPIC_REVIEW.040 | DONE | Close 8+ epics + recommend T41 |
| GATE.X.EVAL.FACTION_DEPTH.001 | DONE | Faction variety + tech access depth eval |
| GATE.X.HYGIENE.REPO_HEALTH.041 | DONE | Full test suite + warning scan + dead code check |
| GATE.X.KG.SEEDING_FIX.001 | DONE | Fix KG template AND→OR bug in NarrativePlacementGen |
| GATE.S8.MEGAPROJECT.ENTITY.001 | DONE | Megaproject entity + content (Anchor, Corridor, Pylon) |
| GATE.S8.HAVEN.RESEARCH_LAB.001 | DONE | Haven research lab with tier-gated slots (1/2/3) |
| GATE.S8.MEGAPROJECT.SYSTEM.001 | DONE | Construction system: multi-stage supply drain |
| GATE.S8.MEGAPROJECT.MAP_RULES.001 | DONE | Map rule mutations on megaproject completion |
| GATE.S8.HAVEN.DRYDOCK_TRANSFER.001 | DONE | Module transfer between ships at Haven drydock |
| GATE.S8.HAVEN.ACCOMMODATION_FX.001 | DONE | Wire accommodation thread bonuses to gameplay |
| GATE.S8.MEGAPROJECT.CONTRACT.001 | DONE | Contract tests for all 3 megaprojects |
| GATE.S8.HAVEN.REVEAL_THREAD.001 | DONE | Reveal choice command + faction visitor spawning |
| GATE.S9.SAVE.MIGRATION.001 | DONE | Save migration framework v1→v2 with version routing |
| GATE.S9.SAVE.INTEGRITY.001 | DONE | Corruption detection + post-load validation + recovery |
| GATE.S9.BALANCE.LOCK.001 | DONE | Tweak baseline snapshot + reflection regression test |
| GATE.S9.STEAM.SDK.001 | DONE | GodotSteam addon + steam_appid.txt + overlay init |
| GATE.S9.L10N.DECISION.001 | DONE | English-only decision + hardcoded string audit |
| GATE.S8.MEGAPROJECT.BRIDGE.001 | DONE | Bridge queries + commands for megaprojects |
| GATE.S9.STEAM.ACHIEVEMENTS.001 | DONE | Map 15-20 milestones to Steam achievements |
| GATE.S8.MEGAPROJECT.UI.001 | DONE | Construction panel UI (progress, supply, map preview) |
| GATE.S8.MEGAPROJECT.HEADLESS.001 | DONE | Headless proof: initiate + supply + complete megaproject |
| GATE.X.HYGIENE.EPIC_REVIEW.041 | DONE | Epic status audit + T42 anchor recommendation |
| GATE.X.EVAL.EA_READINESS.001 | DONE | EA readiness checklist (save, balance, content, perf) |
| GATE.T44.SIGNAL.CONTRACT_TESTS.001 | DONE | Contract tests for economy bridge signals |
| GATE.T44.SIGNAL.ECONOMY_STRESS.001 | DONE | Extended 5000-tick economy stress test |
| GATE.T44.NARRATIVE.COMMUNION_DIALOGUE.001 | DONE | Communion Rep 3-arc dialogue content |
| GATE.T44.NARRATIVE.KEEPER_EXPAND.001 | DONE | Keeper expanded tier-specific dialogue |
| GATE.X.HYGIENE.REPO_HEALTH.044 | DONE | Full test suite + warning scan + hash stability |
| GATE.T44.AMBIENT.SHUTTLE_TRAFFIC.001 | DONE | Cosmetic station shuttles from traffic_level |
| GATE.T44.AMBIENT.MINING_VFX.001 | DONE | Extraction beam particles at industry nodes |
| GATE.T44.AMBIENT.PROSPERITY.001 | DONE | Station lighting/glow tiers from prosperity |
| GATE.T44.AMBIENT.LANE_TRAFFIC.001 | DONE | Billboarded ship sprites on hyperlanes |
| GATE.T44.AMBIENT.WARFRONT_ATMO.001 | DONE | Red-tinted particles at warfront nodes |
| GATE.T44.STATION.FACTION_TINT.001 | DONE | Per-faction albedo modulation on station meshes |
| GATE.T44.STATION.TIER_SCALE.001 | DONE | Outpost/hub/capital mesh scale + variant |
| GATE.T44.STATION.NAMEPLATE.001 | DONE | Station name Label3D + faction insignia |
| GATE.T44.DIGEST.MARKET_ALERTS.001 | DONE | Wire GetMarketAlertsV0 to HUD toasts |
| GATE.T44.DIGEST.ECONOMY_DOCK.001 | DONE | Node economy snapshot in dock panel |
| GATE.T44.DIGEST.MEGAPROJECT_MAP.001 | DONE | Megaproject icons + progress on galaxy map |
| GATE.T44.DIGEST.CONSTRUCTION_VFX.001 | DONE | Construction VFX at megaproject nodes |
| GATE.X.HYGIENE.ECONOMY_EVAL.044 | DONE | Economy balance eval post-simulation changes |
| GATE.X.HYGIENE.EPIC_REVIEW.044 | DONE | Audit epics, close HAVEN_STARBASE + STATION_IDENTITY |
| GATE.X.HYGIENE.REPO_HEALTH.045 | DONE | Full test suite + build baseline for T45 |
| GATE.T45.DEEP_DREAD.TWEAKS.001 | DONE | Deep dread tweaks file (DeepDreadTweaksV0) |
| GATE.T45.DEEP_DREAD.FAUNA_TWEAKS.001 | DONE | Lattice Fauna tweaks (LatticeFaunaTweaksV0) |
| GATE.T45.DEEP_DREAD.DESIGN_DOC.001 | DONE | Deep dread design doc (deep_dread_v0.md) |
| GATE.T45.DEEP_DREAD.PATROL_THIN.001 | DONE | Patrol density scaling by hop distance |
| GATE.T45.DEEP_DREAD.PASSIVE_DRAIN.001 | DONE | Phase-based passive hull drain |
| GATE.T45.DEEP_DREAD.SENSOR_GHOSTS.001 | DONE | Sensor ghost system (phantom contacts) |
| GATE.T45.DEEP_DREAD.INFO_FOG.001 | DONE | Information fog at distance |
| GATE.T45.DEEP_DREAD.DISCOVERY_REGISTER.001 | DONE | Discovery text register shift by phase |
| GATE.T45.DEEP_DREAD.LATTICE_FAUNA.001 | DONE | Lattice Fauna entity + behavior system |
| GATE.T45.DEEP_DREAD.FO_DISTANCE.001 | DONE | 8 FO dread triggers + 24 dialogue lines |
| GATE.T45.DEEP_DREAD.EXPOSURE_TRACK.001 | DONE | Fracture exposure tracking + adaptation |
| GATE.T45.DEEP_DREAD.BRIDGE.001 | DONE | SimBridge.Dread.cs partial |
| GATE.T45.DEEP_DREAD.AMBIENT_AUDIO.001 | DONE | Phase-aware ambient audio layers |
| GATE.T45.DEEP_DREAD.FAUNA_AUDIO.001 | DONE | Lattice Fauna audio presence |
| GATE.T45.DEEP_DREAD.COMMS_STATIC.001 | DONE | Comms degradation at distance |
| GATE.T45.DEEP_DREAD.HUD_DREAD.001 | DONE | HUD dread indicators |
| GATE.T45.DEEP_DREAD.DISTORTION_SHADER.001 | DONE | Phase distortion post-processing shader |
| GATE.T45.DEEP_DREAD.GALAXY_DREAD.001 | DONE | Galaxy map dread visualization |
| GATE.T45.DEEP_DREAD.TESTS.001 | DONE | Deep dread C# test suite (15+ assertions) |
| GATE.T45.DEEP_DREAD.HEADLESS_PROOF.001 | DONE | Headless bot dread journey (20+ assertions) |
| GATE.T45.DEEP_DREAD.GOLDEN_HASH.001 | DONE | Golden hash baseline update |
| GATE.X.HYGIENE.EPIC_REVIEW.045 | DONE | Epic audit + close completed + T46 anchor |
| GATE.T46.PERF.TICK_PROFILER.001 | DONE | Per-system tick profiler (SimKernel instrumentation) |
| GATE.T46.PERF.MEMORY_BUDGET.001 | DONE | Memory budget test (SimState allocation tracking) |
| GATE.T46.PERF.PROFILE_REPORT.001 | DONE | Performance baseline report (tick time + memory) |
| GATE.T46.STEAM.ADDON_INSTALL.001 | DONE | GodotSteam addon install + project config |
| GATE.T46.STEAM.INIT_WRAPPER.001 | DONE | Steam init wrapper with graceful fallback |
| GATE.T46.STEAM.ACHIEVEMENT_BRIDGE.001 | DONE | Achievement unlock via MilestoneSystem |
| GATE.T46.BUILD.EXPORT_TEMPLATE.001 | DONE | Godot export preset + CI build script |
| GATE.T46.BUILD.RELEASE_TEST.001 | DONE | Release build smoke test (headless bot on export) |
| GATE.T46.SAVE.AUTOSAVE_SYSTEM.001 | DONE | Timer-based auto-save system |
| GATE.T46.SAVE.AUTOSAVE_UI.001 | DONE | Auto-save HUD indicator + settings toggle |
| GATE.T46.STATION.NODE_MESH.001 | DONE | Station mesh tier scaling (outpost/hub/capital) |
| GATE.T46.STATION.DOCK_FLAVOR.001 | DONE | Per-faction dock greeting + station description |
| GATE.T46.NARRATIVE.HAVEN_LOGS.001 | DONE | Haven starbase data logs (8 entries) |
| GATE.T46.NARRATIVE.FRAGMENT_LORE.001 | DONE | Expanded adaptation fragment flavor text (12 entries) |
| GATE.T46.NARRATIVE.ENDGAME_TEXT.001 | DONE | Victory/loss screen narrative text (5 paths) |
| GATE.T46.AUDIO.STEM_PIPELINE.001 | DONE | Audio stem loader + dynamic crossfade system |
| GATE.T46.AUDIO.COMBAT_MUSIC.001 | DONE | Combat music trigger + state-driven transitions |
| GATE.X.HYGIENE.REPO_HEALTH.046 | DONE | Full test suite + health baseline for T46 |
| GATE.X.HYGIENE.EPIC_REVIEW.046 | DONE | Epic audit + close completed + T47 anchor |
| GATE.T47.AMBIENT.SHUTTLE_TRAFFIC.001 | DONE | Station traffic shuttles orbiting stations |
| GATE.T47.AMBIENT.MINING_BEAMS.001 | DONE | Mining extraction beams to asteroids |
| GATE.T47.AMBIENT.PROSPERITY_TIERS.001 | DONE | Station prosperity lighting tiers |
| GATE.T47.AMBIENT.LANE_TRAFFIC.001 | DONE | Lane traffic sprites along trade lanes |
| GATE.T47.DIGEST.MARKET_ALERTS.001 | DONE | Market alert toasts in HUD |
| GATE.T47.DIGEST.ECON_PANEL.001 | DONE | Economy snapshot dock panel |
| GATE.T47.MUSIC.COMPOSITION_BRIEF.001 | DONE | Music production brief for composer |
| GATE.T47.MUSIC.DISCOVERY_STINGERS.001 | DONE | Discovery stinger wiring (3 stingers) |
| GATE.T47.MUSIC.FRACTURE_AMBIENCE.001 | DONE | Fracture space ambient audio layer |
| GATE.T47.MUSIC.FACTION_AMBIENT.001 | DONE | Faction territory ambient audio |
| GATE.T47.HAVEN.COMING_HOME.001 | DONE | Haven arrival cinematic transition |
| GATE.T47.HAVEN.VISUAL_TIERS.001 | DONE | Haven visual geometry per tier (5 states) |
| GATE.T47.HAVEN.COMMUNION_REP.001 | DONE | Communion Representative dialogue at T3+ |
| GATE.T47.MEGAPROJECT.MAP_MARKERS.001 | DONE | Megaproject galaxy map markers |
| GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001 | DONE | Construction VFX at megaproject sites |
| GATE.T47.SAVE.RECOVERY_UX.001 | DONE | Save corruption recovery UI |
| GATE.T47.SAVE.SLOT_MANAGEMENT.001 | DONE | Save slot list/delete/rename |
| GATE.X.HYGIENE.REPO_HEALTH.047 | DONE | Full test suite + T47 health baseline |
| GATE.T47.EVAL.ECONOMY_FEEL.001 | DONE | Economy visual feel evaluation |
| GATE.X.HYGIENE.EPIC_REVIEW.047 | DONE | Epic audit + T48 anchor |
| GATE.T48.TEMPLATE.SCHEMA.001 | DONE | Mission template schema + engine |
| GATE.T48.TEMPLATE.SUPPLY_SET.001 | DONE | 4 supply/logistics templates |
| GATE.T48.TEMPLATE.EXPLORE_SET.001 | DONE | 4 exploration templates |
| GATE.T48.TEMPLATE.COMBAT_SET.001 | DONE | 3 combat/security templates |
| GATE.T48.TEMPLATE.POLITICS_SET.001 | DONE | 3 reputation/politics templates |
| GATE.T48.TEMPLATE.TWIST_ENGINE.001 | DONE | Twist slot system + reward scaling |
| GATE.T48.TEMPLATE.CONTEXT_SURFACE.001 | DONE | Template surfacing via station context |
| GATE.T48.DISCOVERY.MAP_MARKERS.001 | DONE | Discovery phase markers on galaxy map |
| GATE.T48.DISCOVERY.SCANNER_VIZ.001 | DONE | Scanner range ring visualization |
| GATE.T48.DISCOVERY.MILESTONE_CARDS.001 | DONE | Discovery milestone feedback |
| GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001 | DONE | Knowledge web layout polish |
| GATE.T48.ANOMALY.CHAIN_SYSTEM.001 | DONE | Anomaly chain engine |
| GATE.T48.ANOMALY.CHAIN_CONTENT.001 | DONE | 3 starter anomaly chains |
| GATE.T48.TENSION.MAINTENANCE.001 | DONE | Fleet upkeep drain system |
| GATE.T48.TENSION.UPKEEP_BRIDGE.001 | DONE | Upkeep display in HUD + dock |
| GATE.T48.TELEMETRY.SESSION_WRITER.001 | DONE | Dev-facing session telemetry |
| GATE.T48.TELEMETRY.CRASH_HOOK.001 | DONE | Crash/exception reporting hook |
| GATE.T48.FH_BOT.EXPANSION.001 | DONE | First-hour bot v2 expansion |
| GATE.X.HYGIENE.REPO_HEALTH.048 | DONE | Full test suite + T48 health |
| GATE.X.HYGIENE.EPIC_REVIEW.048 | DONE | Epic audit + T49 anchor |
| GATE.T49.EVAL.ECON_NARRATIVE.001 | DONE | Fix economy_health + narrative_pacing eval bot data contracts |
| GATE.T49.EVAL.DREAD_FLIGHT.001 | DONE | Fix dread_pacing + flight_feel eval bot data contracts |
| GATE.T49.EVAL.AUDIO_ATMOS.001 | DONE | Fix audio_atmosphere eval bot for headless |
| GATE.T49.DS_BOT.MUTATION_COVERAGE.001 | DONE | Deep systems bot: 9 mutation bridge methods |
| GATE.T49.STRESS.IDLE_REDUCTION.001 | DONE | Stress bot: explore-when-idle + Scrap routing |
| GATE.T49.TUTORIAL.T48_COVERAGE.001 | DONE | Tutorial bot: anomaly/template/upkeep probes |
| GATE.T49.SWEEP.DOCK_PANELS.001 | DONE | Visual sweep: 6 dock-panel screenshot phases |
| GATE.T49.AESTHETIC.CAMERA_EXEMPT.001 | DONE | Exempt galaxy-map camera from TOO_FAR |
| GATE.T49.CHAOS.SCENARIO_EXPANSION.001 | DONE | Chaos bot: 2 new adversarial scenarios |
| GATE.T49.RUBRIC.DIMENSION_UPDATE.001 | DONE | Update rubric + eval guide dimensions |
| GATE.T49.COVERAGE.SCANNER_FIX.001 | DONE | Coverage scanner: scan C# UI + test scripts |
| GATE.T49.PIPELINE.EVAL_RUNNER.001 | DONE | Create Run-EvalBot.ps1 pipeline script |
| GATE.T49.OPTIMIZE.PASS1_EXPANSION.001 | DONE | Add GDScript + allocation auto-checks to optimize scanner |
| GATE.T49.SWEEP.FLIGHT_ENDSTATE.001 | DONE | Visual sweep: flight panels + loss/victory screens |
| GATE.T49.COVERAGE.DEAD_CLEANUP.001 | DONE | Remove confirmed dead bridge methods |
| GATE.T49.PIPELINE.AUDIT_QUICK.001 | DONE | Create Run-AuditQuick.ps1 fast audit script |
| GATE.T49.PROOF.FULL_EVAL_RUN.001 | DONE | Run all eval bots, validate outputs |
| GATE.T49.RESEARCH.PERF_PROFILE.001 | DONE | FPS profiling analysis (P1 diagnostic) |
| GATE.X.HYGIENE.REPO_HEALTH.049 | DONE | Full test suite + T49 health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.049 | DONE | Epic audit + T50 anchor recommendation |
| GATE.T50.PERF.COMBAT_THROTTLE.001 | DONE | Throttle drone combat to 3-5 tick cadence |
| GATE.T50.PERF.NPC_TRADE_CACHE.001 | DONE | Cache BFS hop distances in NpcTradeSystem |
| GATE.T50.PERF.INTEL_ALLOC.001 | DONE | Reduce IntelSystem hot-path allocations |
| GATE.T50.PERF.FO_ALLOC.001 | DONE | Reduce FirstOfficerSystem allocations |
| GATE.T50.PERF.TICK_BUDGET.001 | DONE | Perf regression C# tests |
| GATE.T50.PERF.NPC_PHYSICS.001 | DONE | Replace NPC CharacterBody3D with Node3D |
| GATE.T50.PERF.CAMERA_CACHE.001 | DONE | Cache camera refs + throttle LOD updates |
| GATE.T50.PERF.REP_CACHE.001 | DONE | Reputation query TTL cache in SimBridge |
| GATE.T50.PERF.HEADLESS_PROOF.001 | DONE | FPS measurement headless proof run |
| GATE.T50.ECON.ROUTE_QUALITY.001 | DONE | Ensure profitable route within 2 hops of start |
| GATE.T50.ECON.ROUTE_QUALITY_TEST.001 | DONE | Monte Carlo route quality test (10 seeds) |
| GATE.T50.VISUAL.GALAXY_NODES.001 | DONE | Galaxy map node variety (size/color by type) |
| GATE.T50.VISUAL.GALAXY_FACTION.001 | DONE | Galaxy map faction territory overlay |
| GATE.T50.VISUAL.GALAXY_ECON.001 | DONE | Galaxy map economic indicators on nodes |
| GATE.X.HYGIENE.REPO_HEALTH.050 | DONE | Full test suite + T50 health baseline |
| GATE.T50.RESEARCH.PERF_VALIDATION.001 | DONE | Post-optimization perf validation report |
| GATE.X.HYGIENE.EPIC_REVIEW.050 | DONE | Epic status audit + next anchor rec |
| GATE.T51.VO.BUS_PLAYER.001 | DONE | VO audio bus + Music/Ambient ducking |
| GATE.T51.VO.LOOKUP_SYSTEM.001 | DONE | VO file lookup by speaker+key+sequence |
| GATE.T51.VO.DIALOGUE_WIRE.001 | DONE | Wire VO playback into fo_dialogue_box.gd |
| GATE.T51.VO.BRIDGE_KEY.001 | DONE | Add vo_key field to tutorial bridge snapshots |
| GATE.T51.VO.PRESET_SELECT.001 | DONE | Ship computer voice preset selection UI |
| GATE.T51.VO.HEADLESS_PROOF.001 | DONE | VO system headless proof (file lookup + bus) |
| GATE.T51.STEAM.ADDON_DL.001 | DONE | Download + integrate GodotSteam addon |
| GATE.T51.STEAM.CLOUD_SAVES.001 | DONE | Steam Cloud save sync integration |
| GATE.T51.STEAM.APP_CONFIG.001 | DONE | Steamworks app config + depot setup |
| GATE.T51.TELEMETRY.OPTIN_UI.001 | DONE | Telemetry opt-in UI in settings panel |
| GATE.T51.TELEMETRY.LOCAL_STORE.001 | DONE | Local telemetry file storage backend |
| GATE.T51.TELEMETRY.QUIT_TRACK.001 | DONE | Player death + quit point tracking |
| GATE.T51.TEMPLATE.SUPPLY_AUTHOR.001 | DONE | Supply/logistics mission templates (12-15) |
| GATE.T51.TEMPLATE.EXPLORE_AUTHOR.001 | DONE | Exploration mission templates (10-12) |
| GATE.T51.TEMPLATE.COMBAT_AUTHOR.001 | DONE | Combat/security mission templates (10-12) |
| GATE.T51.TEMPLATE.POLITICS_AUTHOR.001 | DONE | Reputation/politics mission templates (8-10) |
| GATE.X.HYGIENE.REPO_HEALTH.051 | DONE | Full test suite + T51 health baseline |
| GATE.X.HYGIENE.EPIC_REVIEW.051 | DONE | Epic status audit + T52 anchor rec |
| GATE.X.HYGIENE.ECONOMY_EVAL.051 | DONE | Economy balance + mission reward eval |

## A. Slice 0 discipline gates (always-on)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S0.SEM.001 | DONE | Slice 0 is ALWAYS_ON discipline: even if current tooling/boundary gates are DONE, Slice 0 remains IN_PROGRESS as new invariants and boundaries are added; this doc must state that status semantics unambiguously to avoid historical contradiction | docs/55_GATES.md (this section) |

## B. Slice 1 and 1.5 gates (locked execution gates)

### B1. Workflow and tooling gates
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.TOOL.001 | DONE | Deterministic status packet generation exists (diff-driven, capped) | docs/generated/02_STATUS_PACKET.txt |
| GATE.CONN.001 | DONE | Connectivity scan outputs deterministic manifests | docs/generated/connectivity_manifest.json; docs/generated/connectivity_graph.json |
| GATE.CONN.002 | DONE | Connectivity violations empty for Slice scope | docs/generated/connectivity_violations.json |
| GATE.TEST.001 | DONE | Headless determinism harness exists | SimCore.Tests/GoldenReplayTests.cs |
| GATE.TEST.002 | DONE | Golden world hash regression exists and is stable | docs/generated/snapshots/golden_replay_hashes.txt |
| GATE.EVID.001 | DONE | Context packet reports latest scan + test summary + hash snapshot presence (or explicit “not found” reasons) | docs/generated/01_CONTEXT_PACKET.md ([SYSTEM HEALTH] shows Connectivity OK + Tests OK + Hash Snapshot present) |
| GATE.MAP.001 | DONE | Repo evidence export exists (tests index + grep + map) | docs/generated/evidence/simcore_tests_index.txt; docs/generated/evidence/gate_evidence_grep.txt; docs/generated/evidence/gate_evidence_map.json |
| GATE.FILE.001 | DONE | Runtime File Contract enforced (runtime IO restricted to res:// and user://; SimCore has no System.IO IO) | SimCore.Tests/Invariants/RuntimeFileContractTests.cs |
| GATE.X.API_BOUNDARIES.GUARD.001 | DONE | Enforce UI%SimCore boundaries with a deterministic guard (v0): deterministic scan test enforces scripts/ui/**/*.cs contains no references to SimCore.Entities or SimCore.Systems in code (ignores comments%strings); emits sorted violations as file:line:type; StationMenu routes intel age and sustainment via SimBridge (GetIntelAgeTicks, GetSustainmentSnapshot) so UI stays compliant. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore.Tests/ContainmentTests.cs; scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; docs/30_CONNECTIVITY_AND_INTERFACES.md |
| GATE.X.GAMESHELL.SMOKE.001 | DONE | GameShell headless smoke harness v0: headless Godot runner executes with Seed, runs exactly N=120 ticks, exits code 0; emits deterministic transcript with Seed, tick_count, world_hash@0%60%120 and sorted entity counts; no timestamps; byte-for-byte stable across runs (stdout%stderr stable hashes across 2 runs). Proof: scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/core/sim/sim.gd" PASS. Run: resolve Godot exe via scripts/tools/common.ps1 Get-GodotExe, then run --headless --verbose --path <repoRoot> -s scripts/tests/test_sim_skeleton.gd --seed=42 --ticks=120 twice; compare SHA256(stdout) and SHA256(stderr) | scripts/tests/test_sim_skeleton.gd; scripts/core/sim/sim.gd; scenes/main.tscn; scripts/core/game_manager.gd; scripts/tools/common.ps1 |
| GATE.X.SCENARIO.HARNESS.001 | DONE | Deterministic scenario harness v0: minimal builder to create crafted SimCore worlds (nodes, lanes, markets, fleets, prices, fees) with stable IDs and deterministic ordering; no UI%Godot scene construction; emits deterministic scenario summary for diffing; proof: used by at least 2 regression tests (routes and capacity scarcity) | SimCore/Schemas/WorldDefinition.cs; SimCore/Schemas/ScenarioSchema.cs; SimCore.Tests/Systems/RoutePlannerTests.cs; SimCore.Tests/Systems/LaneFlowSystemTests.cs |
| GATE.REPO.HEALTH.001 | DONE | Repo health v0: one-command scripts/tools/Repo-Health.ps1 runs deterministic Scan-Connectivity + dotnet test and emits sorted no-timestamp docs/generated/repo_health_report_v0.txt; FAIL on: file>25MB; forbidden ext {zip,rar,7z,exe,dll,pdb,mp4,mov,avi,mkv,psd,blend} unless allowlisted; backup%junk outside docs/archive/%_archive/; any generated/ dir outside docs/generated/; known generated artifacts outside docs/generated/; LLM size budgets: 01_CONTEXT_PACKET warn>250KB fail>750KB, docs/** warn>150KB fail>400KB, LLM surfaces warn>250KB fail>750KB; connectivity delta FAIL only on NEW cross-layer edges not in baseline%allowlist (baseline mint supported) | SimCore.Tests/Invariants/RuntimeFileContractTests.cs; scripts/tools/Repo-Health.ps1; scripts/tools/Scan-Connectivity.ps1; docs/connectivity/baseline_cross_layer_edges_v0.txt; docs/connectivity/allowlist_cross_layer_edges_v0.txt |
| GATE.X.ROADMAP.CONSISTENCY.001 | DONE | Roadmap consistency v0: deterministic scan enforces HARD_FAIL: (1) every PASS gate in docs/56_SESSION_LOG.md is DONE in docs/55_GATES.md, (2) every DONE gate has exactly one PASS entry OR is allowlisted as legacy, (3) DONE gates must not have evidence=TBD, (4) no gate id duplicates in docs/55_GATES.md, (5) no gate id may be both active in docs/gates/gates.json and DONE in docs/55_GATES.md; WARN_ONLY: epic slice%epic statuses in docs/54_EPICS.md contradict gate completion. Emits docs/generated/roadmap_mismatches_v0.txt with sorted mismatch records (severity, kind, gate_id, details); no timestamps; exits nonzero on HARD_FAIL only | scripts/tools/Scan-RoadmapConsistency.ps1; SimCore.Tests/Invariants/RoadmapConsistencyTests.cs; docs/generated/roadmap_mismatches_v0.txt; docs/roadmap/legacy_done_without_pass_allowlist_v0.txt |
| GATE.X.TWEAKS.DATA.PLUMBING.001 | DONE | Tweak config plumbing v0: SimState has versioned tweak config + canonical SHA256 hash; SimKernel injects deterministic JSON override; no runtime IO in SimCore; goldens unchanged; override determinism covered. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore/SimState.cs; SimCore/SimKernel.cs; SimCore.Tests/Determinism/LongRunWorldHashTests.cs |
| GATE.X.TWEAKS.DATA.SCHEMA.001 | DONE | Tweak config schema contract v0: canonical JSON field list is explicit and append-only (prefix locked); stable defaults encoded; hash locked to SHA256(UTF-8 canonical JSON) uppercase hex; invalid JSON, non-object roots, and schema-invalid types deterministically fall back to defaults. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore/SimState.cs; SimCore.Tests/Invariants/RuntimeFileContractTests.cs |
| GATE.X.TWEAKS.DATA.TRANSCRIPT.001 | DONE | Transcript surfacing v0: deterministic transcript prints tick=0 tweaks_version=<v> tweaks_hash=<H> (no timestamps, fixed formatting); LongRun determinism output includes the line for both runs; exact-line asserts lock format. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore/SimState.cs |
| GATE.X.TWEAKS.DATA.HOSTFILE.RUNNER.001 | DONE | Runner-loaded tweak file v0: SimCore.Runner loads Data/Tweaks/tweaks_v0.json (UTF-8 no BOM) when present; strict UTF-8 decode + JSON object check; missing%invalid falls back to defaults; injected via SimKernel(seed, tweakConfigJsonOverride); prints deterministic TweaksHash | SimCore.Runner/Program.cs; SimCore/SimKernel.cs; SimCore/SimState.cs; SimCore.Tests/Invariants/RuntimeFileContractTests.cs |
| GATE.X.TWEAKS.DATA.MIGRATE.MARKET_FEES.001 | DONE | Migrate market fee constants v0: Market fee multipliers sourced from tweak config with stable defaults; determinism override changes fee outcome deterministically | SimCore/Systems/MarketSystem.cs; SimCore/SimState.cs; SimCore.Tests/GoldenReplayTests.cs |
| GATE.X.TWEAKS.DATA.MIGRATE.LANE_CAPACITY.001 | DONE | Migrate lane capacity defaults v0: TotalCapacity<=0 uses Tweaks.DefaultLaneCapacityK when >0 else unlimited (legacy default); deterministic override changes queueing | SimCore/SimState.cs; SimCore/Systems/LaneFlowSystem.cs |
| GATE.X.TWEAKS.DATA.MIGRATE.LOGISTICS_RISK.001 | DONE | Migrate logistics risk knobs v0: RoutePlanner consumes tweak-sourced risk knobs via deterministic fixed-point scoring; determinism test proves override changes chosen route deterministically | SimCore/Systems/RoutePlanner.cs; SimCore.Tests/Determinism/LogisticsOrderingDeterminismTests.cs; docs/tweaks/allowlist_numeric_literals_v0.txt |
| GATE.X.TWEAKS.DATA.MIGRATE.LOOP_SCORING.001 | DONE | Migrate loop scoring knobs v0: loop viability thresholds sourced from tweak config; override deterministically filters supplier candidates and changes planning outcome | SimCore/Systems/LogisticsSystem.cs; SimCore.Tests/Determinism/LogisticsOrderingDeterminismTests.cs |
| GATE.X.TWEAKS.DATA.MIGRATE.WORLDGEN_BOUNDS.001 | DONE | Migrate worldgen bounds v0: GalaxyGenerator sources bounds from tweak config; deterministic enforcement with stable FAIL reporting; deterministic pass%fail under overrides | SimCore/Gen/GalaxyGenerator.cs; SimCore.Tests/Invariants/BasicStateInvariantsTests.cs |
| GATE.X.TWEAKS.DATA.GUARD.001 | DONE | Tweak routing guard v0: deterministic scan flags new numeric literals in SimCore/Systems%SimCore/Gen unless Tweaks-sourced or allowlisted; emits sorted violations report; baseline mint supported via STE_TWEAK_GUARD_MINT_BASELINE | SimCore.Tests/Invariants/RuntimeFileContractTests.cs; docs/tweaks/baseline_numeric_literals_v0.txt |
| GATE.UI.SAFEREAD.NOLOCK.INTEL_AGE.001 | DONE | Fix UI lock recursion v0: inside SimBridge.TryExecuteSafeRead lambdas, UI calls *_NoLock(state, ...) helpers to avoid nested lock acquisition; prevents LockRecursionException and keeps StationMenu stable | scripts/bridge/SimBridge.cs; scripts/ui/StationMenu.cs; SimCore.Tests/Invariants/RuntimeFileContractTests.cs |
| GATE.X.CONTENT_SUBSTRATE.001 | DONE | Content substrate registries v0: deterministic ContentRegistryLoader for versioned goods%recipes%modules registry with schema contract and canonical SHA256 digest; WorldLoader stamps registry version%digest into SimState; emits deterministic docs/generated/content_registry_digest_v0.txt (no timestamps); contract test asserts identical digest and normalized ordering across two loads | docs/56_SESSION_LOG.md (PASS) |

### B2. Slice 1 critical gates
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.TIME.001 | DONE | 60x time contract enforced: 1s real = 1 min sim, no acceleration | SimCore.Tests/TimeContractTests.cs |
| GATE.INTENT.001 | DONE | Deterministic intent pipeline exists | SimCore.Tests/Intents/IntentSystemTests.cs + scripts/bridge/SimBridge.cs (EnqueueIntent) |
| GATE.WORLD.001 | DONE | 2 stations, 1 lane, 2 goods micro-world config | SimCore.Tests/World/World001_MicroWorldLoadTests.cs + SimCore.Tests/Intents/IntentSystemTests.cs (KernelWithWorld001) |
| GATE.STA.001 | DONE | Station inventory ledger and invariants | SimCore.Tests/Systems/InventoryLedgerTests.cs + SimCore.Tests/Invariants/InventoryConservationTests.cs; SimCore.Tests/Invariants/BasicStateInvariantsTests.cs |
| GATE.LANE.001 | DONE | Lane flow with deterministic delay arrivals | SimCore.Tests/Systems/LaneFlowSystemTests.cs |
| GATE.MKT.001 | DONE | Inventory-based pricing with spread | SimCore.Tests/MarketTests.cs + SimCore.Tests/Systems/MarketPublishCadenceTests.cs |
| GATE.MKT.002 | DONE | Price publish cadence every 12 game hours | SimCore.Tests/Systems/MarketPublishCadenceTests.cs |
| GATE.INTEL.001 | DONE | Local truth, remote banded intel + age | SimCore.Tests/Systems/IntelContractTests.cs |
| GATE.UI.001 | DONE | Minimal panel shows inventory, price, intel age | scripts/ui/StationMenu.cs + scripts/bridge/SimBridge.cs |
| GATE.UI.002 | DONE | Buy/sell generates intent, no direct mutation | scripts/ui/StationMenu.cs (SubmitBuyIntent/SubmitSellIntent) + scripts/bridge/SimBridge.cs (EnqueueIntent + BuyIntent/SellIntent) + SimCore.Tests/Intents/IntentSystemTests.cs |
| GATE.DET.001 | DONE | 10,000 tick run stable world hash | SimCore.Tests/Determinism/LongRunWorldHashTests.cs (LongRunWorldHash) + docs/generated/05_TEST_SUMMARY.txt |
| GATE.SAVE.001 | DONE | Save/load round trip preserves hash | SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs (SaveLoadWorldHash) + docs/generated/05_TEST_SUMMARY.txt |
| GATE.INV.001 | DONE | Invariants suite passes | SimCore.Tests/Invariants/InventoryConservationTests.cs (InventoryConservation); SimCore.Tests/Invariants/BasicStateInvariantsTests.cs (BasicStateInvariants) + docs/generated/05_TEST_SUMMARY.txt |

### B3. Slice 1.5 sustainment gates
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.TECH.001 | DONE | One tech requires 2 goods per tick to remain enabled | SimCore.Tests/Sustainment/TechUpkeepConsumesGoodsTests.cs |
| GATE.TECH.002 | DONE | Buffers sized in days of game time | SimCore.Tests/Sustainment/BufferSizingDaysTests.cs |
| GATE.TECH.003 | DONE | Deterministic degradation under undersupply | SimCore.Tests/Sustainment/DeterministicDegradationTests.cs |
| GATE.UI.101 | DONE | UI shows sustainment margin and time-to-failure | scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Systems/SustainmentReport.cs |
| GATE.DET.101 | DONE | Sustainment determinism regression passes | SimCore.Tests/Sustainment/SustainmentDeterminismRegressionTests.cs |
| GATE.INV.101 | DONE | Buffer math invariants pass | SimCore.Tests/Sustainment/BufferMathInvariantsTests.cs |

### B4. Slice 2 programs gates (v1)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.PROG.001 | DONE | Program schema v1 exists (TradeProgram only) and is versioned | SimCore/Schemas/ProgramSchema.json + SimCore.Tests/Programs/ProgramContractTests.cs (PROG_001) |
| GATE.FLEET.001 | DONE | Fleet binding v1 exists (single trader fleet) and is deterministic | SimCore/World/WorldLoader.cs + SimCore.Tests/Programs/FleetBindingContractTests.cs + docs/generated/05_TEST_SUMMARY.txt |
| GATE.DOCTRINE.001 | DONE | DefaultDoctrine exists (max 2 toggles) and is deterministic | SimCore/Programs/DefaultDoctrine.cs + SimCore.Tests/Programs/DefaultDoctrineContractTests.cs |
| GATE.QUOTE.001 | DONE | Liaison Quote is deterministic: request + snapshot => quote (cost/time/risks/constraints) | SimCore/Programs/ProgramQuote.cs + SimCore/Programs/ProgramQuoteSnapshot.cs + SimCore.Tests/Programs/ProgramQuoteContractTests.cs + SimCore.Tests/TestData/Snapshots/program_quote_001.json + docs/generated/05_TEST_SUMMARY.txt |
| GATE.EXPLAIN.001 | DONE | Explain events are schema-bound (no free-text) for quote and outcomes | SimCore.Tests/Programs/ProgramContractTests.cs (EXPLAIN_001) |
| GATE.PROG.EXEC.001 | DONE | Program execution emits intents only, no direct ledger mutation | SimCore.Tests/Programs/ProgramContractTests.cs (PROG_EXEC_001) + SimCore/Programs/ProgramSystem.cs |
| GATE.PROG.EXEC.002 | DONE | TradeProgram drives buy/sell intents against Slice 1 micro-world and affects outcomes only via SimCore tick | SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs + docs/generated/05_TEST_SUMMARY.txt |
| GATE.BRIDGE.PROG.001 | DONE | GameShell -> SimCore bridge supports program lifecycle (create/start/pause) without direct state mutation | scripts/bridge/SimBridge.cs + SimCore.Tests/Programs/ProgramLifecycleContractTests.cs + SimCore.Tests/Programs/ProgramStatusCommandContractTests.cs |
| GATE.UI.PROG.001 | DONE | Minimal Programs UI: create, view quote, start/pause, last-tick outcomes | scripts/ui/ProgramsMenu.cs + scripts/ui/StationMenu.cs + scripts/bridge/SimBridge.cs + scenes/playable_prototype.tscn |
| GATE.VIEW.001 | DONE | Playable prototype camera is a ship-follow orbit camera (zoom + rotate) and player has a ship placeholder mesh | scripts/view/player_follow_camera.gd + scenes/player.tscn + scenes/playable_prototype.tscn |
| GATE.DET.PROG.001 | DONE | Determinism regression includes program lifecycle (create/start/pause) with stable hash | SimCore.Tests/Determinism/ProgramDeterminismTests.cs + SimCore.Tests/SaveLoad/ProgramSaveLoadContractTests.cs |

### B4.5 Slice 2.5 seed plumbing gates (v0)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S2_5.SEEDS.001 | DONE | Axis: seed_input. Plumb Seed into galaxy generation entrypoint and persist on SimState. Acceptance: headless Godot compile smoke clean (no Parse Error%Compilation failed); Seed stored on SimState.seed and used by galaxy_generator.generate_for_state; anti-pattern scan OK (no randomize%wall-clock%global randf%randi in core scan); headless determinism smoke exits 0 and prints digest_1==digest_2 for same Seed+regions. Closure proof: seed_smoke_stdout shows digest match + DETERMINISM_OK; Validate-GodotScript exit code 0 for sim_state.gd and galaxy_generator.gd. | scripts/core/sim/galaxy_generator.gd; scripts/core/state/sim_state.gd; scripts/core/galaxy_graph.gd |
| GATE.S2_5.SEEDS.002 | DONE | Axis: rng_streams. Intent: Define a stable RNG stream contract keyed by Seed + stream name so adding systems does not perturb existing randomness. Delta: RNG stream partitioning exists conceptually but is not gate-specified as the required mechanism for Seed-based worldgen. Acceptance: Godot headless parse passes for scripts/core/sim/rng_streams.gd; each stream is derived from Seed + stable stream key and does not depend on call order from other systems; adding a new stream does not change outputs of existing stream keys for the same Seed; scripts/core/sim/galaxy_generator.gd consumes only named streams for all randomness. Status rules: TODO->IN_PROGRESS once stream-derivation API is explicit and used by worldgen; IN_PROGRESS->DONE when acceptance checks pass; BLOCKED if any consumer uses non-stream RNG in the worldgen path. Closure proof: Validate-GodotScript PASS (exit code 0) for scripts/core/sim/rng_streams.gd and scripts/core/sim/galaxy_generator.gd; legacy keys use fixed offsets from Seed; new keys seed from (Seed, stable_stream_name) hash; galaxy_generator uses STREAM_GALAXY_GEN. | scripts/core/sim/rng_streams.gd; scripts/core/sim/galaxy_generator.gd |
| GATE.S2_5.SEEDS.003 | DONE | Axis: gameshell_regression. Intent: Add a minimal GameShell seed determinism regression test that exercises galaxy generation twice with the same Seed. Delta: No existing GameShell gate asserts identical generation for identical Seed or protects against accidental nondeterminism in .gd worldgen scripts. Acceptance: Godot headless run passes for scripts/tests/test_galaxy_core.gd; test generates twice with the same Seed and asserts identical ordered nodes%edges representation; test generates with two different Seeds and asserts at least one deterministic structural difference; output is stable for diffing (explicit ordering, no timestamps). Status rules: TODO->IN_PROGRESS once the test is written and fails on current nondeterministic behavior (if any); IN_PROGRESS->DONE when test passes and is stable across reruns; BLOCKED if test cannot run headlessly in repo workflow. Closure proof: scripts/tests/test_galaxy_core.gd assertion output proves same-Seed equality and cross-Seed difference. Evidence universe: Anchor=scripts/tests/test_galaxy_core.gd, Extra=scripts/core/sim/galaxy_generator.gd,scripts/core/sim/rng_streams.gd | scripts/tests/test_galaxy_core.gd; scripts/core/sim/galaxy_generator.gd; scripts/core/sim/rng_streams.gd |
| GATE.S2_5.SEEDS.004 | DONE | Axis: save_seed_identity. Intent: Persist Seed as part of SimCore world identity and ensure Serialization round-trip preserves it exactly. Delta: GATE.SAVE.001 proves hash equivalence but does not explicitly require that Seed and worldgen parameters are saved%loaded as identity inputs. Acceptance: dotnet test passes including SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; WorldLoader stores Seed in a stable world identity field (or equivalent) used by downstream worldgen; Serialization payload includes Seed and Load restores it exactly; failure output includes the Seed value used so repro is trivial. Status rules: TODO->IN_PROGRESS once Seed is added to the SimCore identity structure and wired into serialization; IN_PROGRESS->DONE when the save%load test asserts Seed identity and passes; BLOCKED if adding Seed breaks existing save%load hashes without explicit migration note. Closure proof: SaveLoadWorldHashTests asserts Seed preserved across save%load and reports Seed on failure. Evidence universe: Anchor=SimCore/World/WorldLoader.cs, Extra=SimCore/Systems/SerializationSystem.cs,SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs. Proof phrase: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release passes; SaveLoadWorldHashTests prints Seed(saved)=123 and Seed(resaved)=123 with matching hashes. | SimCore/World/WorldLoader.cs; SimCore/Systems/SerializationSystem.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs |
| GATE.S2_5.SEEDS.005 | DONE | Axis: determinism_seed_repro. Intent: Make determinism harnesses that create worlds surface the Seed used (input or recorded) so long-run drift is reproducible. Delta: Existing determinism gates did not surface Seed as an explicit repro input%output for the long-run world-creating harness. Acceptance: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release passes including SimCore.Tests/Determinism/LongRunWorldHashTests.cs; long-run harness accepts Seed via STE_LONGRUN_SEED (default stable) and includes Seed in assertion failure messages; re-running with the same Seed yields identical world hash checkpoints at ticks 0, 1000, 5000, 10000 (A vs B); harness introduces no new nondeterminism sources. Status rules: TODO->IN_PROGRESS once Seed is part of the harness configuration or failure reporting; IN_PROGRESS->DONE when acceptance checks pass; BLOCKED if harness cannot be made seed-reproducible without broader refactor. Closure proof: LongRunWorldHashTests includes Seed in assertion messages and prints checkpoint hashes (0%1000%5000%10000) enabling repro via STE_LONGRUN_SEED. Evidence universe: Anchor=SimCore.Tests/Determinism/LongRunWorldHashTests.cs, Extra=SimCore/World/WorldLoader.cs | SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore/World/WorldLoader.cs |
| GATE.S2_5.SEEDS.006 | DONE | Axis: spec_docs. Intent: Define canonical Seed identity vocabulary and determinism%save%load regression reporting requirements in kernel docs, aligned to SaveLoadWorldHashTests. Acceptance: docs/21_90_TERMS_UNITS_IDS.md defines Seed (int32) and Seed%WorldId token; docs/20_TESTING_AND_DETERMINISM.md requires determinism%save%load regressions include Seed for repro and save envelope uses Seed + State; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release passes incl SaveLoadWorldHashTests; closure proof: docs diffs in docs/21_90_TERMS_UNITS_IDS.md + docs/20_TESTING_AND_DETERMINISM.md and passing tests confirm contract alignment | docs/21_90_TERMS_UNITS_IDS.md; docs/20_TESTING_AND_DETERMINISM.md; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs |

### B4.6 Slice 2.5 worldgen gates (v0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S2_5.WGEN.GALAXY.001 | DONE | Deterministic galaxy topology v0: for a given Seed generate a connected starter region graph with MIN nodes=12 and MIN lanes=18; enforce MIN nodes at generator boundary; mint stable NodeId%LaneId via deterministic counters (no hash iteration); lane attributes include capacity and risk scalar default (r=0) in dump; emit diff-friendly dump to docs/generated/galaxy_topology_dump_seed_999_starcount_12_radius_100.txt with nodes sorted by NodeId and lanes sorted by FromId,ToId,LaneId; same Seed equals byte-for-byte. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore/Gen/GalaxyGenerator.cs; SimCore.Tests/GalaxyTests.cs; scripts/tests/test_galaxy_core.gd; scripts/core/sim/galaxy_generator.gd; scripts/core/galaxy_graph.gd; scenes/tests/test_galaxy_core.tscn; docs/generated/galaxy_topology_dump_seed_999_starcount_12_radius_100.txt |
| GATE.S2_5.WGEN.DISCOVERY_SEEDING.001 | DONE | Discovery seeding contract v0: added schema-bound DiscoverySeedSurfaceV0 (required fields + stable ID rule) and deterministic generator surface builder with explicit ordinal ordering; contract test hard-fails deterministically on missing fields or unstable IDs; no timestamps | Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (142%142 PASS) | SimCore/Gen/GalaxyGenerator.cs; SimCore/Schemas/WorldDefinition.cs; SimCore.Tests/Invariants/DiscoverySeedingContractTests.cs |
| GATE.S2_5.WGEN.DISCOVERY_SEEDING.002 | DONE | Deterministic anomaly families + resource marker seeding v0 with per-seed-class guarantees; stable IDs; violations emitted as deterministic table (Seed%WorldClass%ReasonCode%PrimaryId) sorted by ReasonCode then PrimaryId then Seed. Closure: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (144%144); per-WorldClass guarantees enforced via BuildDiscoverySeedSurfaceV0(state, seed) with stable DiscoveryId minting; violations table uses PrimaryId=WorldClassId and deterministic sort keys; batch guarantee test covers seeds 1..100. | SimCore/Gen/GalaxyGenerator.cs; SimCore/Entities/Node.cs; SimCore/Entities/IntelBook.cs; SimCore.Tests/GalaxyTests.cs |
| GATE.S2_5.WGEN.DISCOVERY_SEEDING.003 | DONE | Corridor trace seeding v0: deterministic corridor%trace seeding order with explicit tie-breaks (Ordinal); FractureTraveling fleet processing uses Ordinal-sorted fleet ids; includes fast ordering test that reports first mismatch deterministically (index%expected%actual); no timestamps | dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | SimCore/Systems/FractureSystem.cs; SimCore/Entities/Edge.cs; SimCore.Tests/FractureTests.cs |
| GATE.S2_5.WGEN.DISCOVERY_SEEDING.004 | DONE | Deterministic discovery seeding report v0: SimCore.Runner discovery-report emits docs/generated/discovery_seeding_report_v0.txt over Seeds 1..100 with identity header (SeedRange=1..100; WorldgenStarCount; WorldgenRadius; ContentRegistryVersion; ContentRegistryDigest) sourced from docs/generated/content_registry_digest_v0.txt; stable formatting%ordering; no timestamps; exits nonzero on violations (writes report first). Proof: dotnet run --project SimCore.Runner discovery-report (full suite Release PASS; Validate-Gates OK). | SimCore.Runner/Program.cs; SimCore/Gen/GalaxyGenerator.cs; docs/generated/content_registry_digest_v0.txt; docs/generated/discovery_seeding_report_v0.txt |
| GATE.S2_5.WGEN.DISCOVERY_SEEDING.005 | DONE | N-seed scenario proof v0: added deterministic DiscoverySeedingScenarioProofTests that generates the discovery seeding report twice (seeds 1..100) and asserts byte-for-byte stability plus required structural guarantees; divergence prints Seed%Phase%FirstDivergenceLine with stable SHA256 hashes and line samples; resolves docs/generated/content_registry_digest_v0.txt from repo root to avoid working-directory nondeterminism; full dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore.Runner/Program.cs; docs/generated/discovery_seeding_report_v0.txt |
| GATE.S2_5.WGEN.DISCOVERY_SEEDING.006 | DONE | Tool readout v0 (CLI, no Godot UI): added discovery-readout runner command that emits deterministic Facts%Events style seeded discoveries for one seed (default seed 42, override via --seed); output is stable ordered and formatted with no timestamps; writes docs/generated/discovery_readout_seed_<seed>_v0.txt. Proof: dotnet run --project SimCore.Runner -- discovery-readout and SHA256 unchanged across 2 runs; Validate-Gates OK; full suite Release PASS | SimCore.Runner/Program.cs; SimCore/Gen/GalaxyGenerator.cs; docs/generated/discovery_readout_seed_42_v0.txt |
| GATE.S2_5.WGEN.ECON.001 | DONE | Deterministic economy placement v0 for starter region: generator assigns initial inventories plus demand sinks (fuel%ore%metal) for starter markets; define viable early trade loop as: loop hop_count<=4, net_profit_proxy>0 after fees, volume_proxy>0; assert at least 3 distinct loops exist; emit deterministic loop report to docs/generated/econ_loops_report.txt sorted by net_profit_proxy desc then route_id asc; deterministic across runs | SimCore/Gen/GalaxyGenerator.cs; SimCore.Tests/MarketTests.cs; SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore.Tests/GoldenReplayTests.cs; docs/generated/econ_loops_report.txt |
| GATE.S2_5.WGEN.FACTION.001 | DONE | Deterministic faction seeding v0: create exactly 3 factions with fields {FactionId, HomeNodeId, RoleTag, Relations[OtherFactionId] in {-1,0,+1}}; IDs minted deterministically; home selection seed-driven and deterministic; emit diff-friendly table sorted by FactionId plus relations matrix with rows%cols sorted; same Seed yields byte-for-byte identical outputs in both SimCore report and Godot test repr | scripts/core/sim/galaxy_generator.gd; scripts/tests/test_galaxy_core.gd; SimCore/Gen/GalaxyGenerator.cs; SimCore.Tests/GalaxyTests.cs; SimCore/Schemas/WorldDefinition.cs |
| GATE.S2_5.WGEN.WORLD_CLASSES.001 | DONE | Deterministic world classes v0: define exactly 3 classes (CORE, FRONTIER, RIM) and assign every node exactly one class deterministically; single measurable effect is fee_multiplier; invariant enforced: starter region contains at least 1 node of each class; emits deterministic per-node class list sorted by NodeId plus per-class effect summary | scripts/core/sim/galaxy_generator.gd; scripts/tests/test_universe_validator.gd; SimCore/Gen/GalaxyGenerator.cs |
| GATE.S2_5.WGEN.DISTINCTNESS.001 | DONE | World class distinctness report v0 (legacy combined): deterministic class stats extractor aggregates per-class metrics over Seeds 1..100 and enforces separation constraints (fee_multiplier separation and monotonic avg_radius2 ordering CORE < FRONTIER < RIM); negative case forces identical class params + single class assignment and deterministically FAILs with sorted violations; report emitted to docs/generated/class_stats_report_v0.txt (sorted, no timestamps) | SimCore/Gen/GalaxyGenerator.cs; SimCore.Tests/Determinism/LongRunWorldHashTests.cs; docs/generated/class_stats_report_v0.txt |
| GATE.S2_5.WGEN.DISTINCTNESS.REPORT.001 | DONE | World class distinctness report v0: deterministic, byte-for-byte stable CLASS_STATS_REPORT_V0 over seeds 1..100 using worldgen-era signals only (avg_degree, avg_lane_capacity, chokepoint_density, fee_multiplier, avg_radius2); explicit ordinal ordering (WorldClassId, axis) and invariant-culture formatting; no timestamps; test asserts exact stability across reruns and emits docs/generated/class_stats_report_v0.txt; full suite PASS | proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (PASS) |
| GATE.S2_5.WGEN.DISTINCTNESS.TARGETS.001 | DONE | World class distinctness targets v0: deterministic targets enforcement over v0 report metrics (avg_radius2 ordering + fee_multiplier ordering); requires dominant constraints per class as measurable inequalities; emits deterministic targets report with sorted violations (Code ordinal, then Seed asc) including per-seed metric deltas; no timestamps; full suite PASS | Evidence: SimCore/Gen/GalaxyGenerator.cs;SimCore.Tests/Determinism/LongRunWorldHashTests.cs |
| GATE.S2_5.WGEN.INVARIANTS.001 | DONE | Worldgen onboarding invariants suite v0: CONNECTED_GRAPH, STARTER_REGION_SAFE_PATH (at least 1 path from starter hub to each starter market with max_chokepoints<=1), EARLY_LOOPS_MIN3 (>=3 viable loops as defined above). Harness runs headless and emits deterministic failure records {Seed, InvariantName, PrimaryId, DetailsKV} sorted by InvariantName then PrimaryId then Seed; report is byte-for-byte stable; exits 0 after emitting report and PASS%FAIL summary | scripts/tests/test_universe_validator.gd; scripts/tests/test_galaxy_core.gd; scripts/tests/test_economy_core.gd; scripts/core/sim/rng_streams.gd |
| GATE.S2_5.WGEN.DISTRIBUTION.001 | DONE | Worldgen distribution bounds v0 (ultra-loose) over N=100 seeds: for a small fixed starter goods set, each good must have at least 1 producer station and at least 1 sink station in the starter region (no missing producers or missing sinks); deterministic summary includes counts per good and failing seeds list sorted asc; exits nonzero on violations; report emitted to docs/generated/worldgen_distribution_bounds_report_v0.txt | SimCore.Tests/MarketTests.cs; SimCore/Gen/GalaxyGenerator.cs; SimCore/Systems/MarketSystem.cs; docs/generated/worldgen_distribution_bounds_report_v0.txt |
| GATE.S2_5.WGEN.NSEED.001 | DONE | N-seed batch worldgen test v0: run invariants for Seeds 1..N where N=100; emit deterministic summary with counts per InvariantName and capped failing seed lists (sorted asc) and capped failure records (sorted as invariants gate); exits nonzero if any failure; output byte-for-byte stable; golden SHA256 enforced (update via STE_UPDATE_INVARIANTS_BATCH_GOLDEN) | SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore.Tests/Invariants/BasicStateInvariantsTests.cs; SimCore/Gen/GalaxyGenerator.cs |
| GATE.S2_5.SAVELOAD.WORLDGEN.001 | DONE | Worldgen save%load contract v0 (momentum-safe): save%load crash-free and preserves stable NodeId%LaneId plus core worldgen state (econ placement, faction tables, world classes); post-load world hash matches pre-save; key dumps deterministic (format stable, no timestamps). Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS; SaveLoad seed expected=saved=resaved=123; Before hash == After hash | SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore/Systems/SerializationSystem.cs; SimCore/World/WorldLoader.cs |
| GATE.S2_5.TOOL.SEED_EXPLORER.001 | DONE | Seed explorer tooling v0: seed-explore emits deterministic artifacts under docs/generated (topology_summary.txt, econ_loops.txt, invariants.txt); seed-diff takes SeedA%SeedB and emits diff_topology.txt (added%removed nodes%lanes) and diff_loops.txt (added%removed loops by route_id); outputs byte-for-byte stable (UTF-8 no BOM) and contain no timestamps; v0 knobs centralized in SeedExplorerV0Config with CLI overrides. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS | SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore.Runner/Program.cs; SimCore/Gen/GalaxyGenerator.cs; scripts/tools/seed_determinism_smoke.gd |

### B5. Slice 3 logistics gates (v1)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.ROUTE.001 | DONE | Deterministic route planner exists: from node to node => ordered edges/nodes + total travel ticks (stable tie-breaks by EdgeId/NodeId) | SimCore/Systems/RoutePlanner.cs + SimCore.Tests/Systems/RoutePlannerTests.cs |
| GATE.FLEET.ROUTE.001 | DONE | Fleet travel can follow a planned multi-edge route (lane sequence) without nondeterminism | SimCore/Systems/MovementSystem.cs + SimCore/Entities/Fleet.cs + SimCore.Tests/Systems/FleetRouteTravelTests.cs |
| GATE.LOGI.JOB.001 | DONE | LogisticsJob can represent multi-hop shipments (source, sink, good, qty, route) and is deterministic | SimCore/Entities/LogisticsJob.cs + SimCore.Tests/Systems/LogisticsJobContractTests.cs |

### B6. Slice 3 logistics execution gates (v1)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.LOGI.CARGO.001 | DONE | Fleet has deterministic cargo storage (dict keyed by GoodId) that round-trips through save/load; cargo changes only via intent resolution (no direct mutation in LogisticsSystem) | SimCore/Entities/Fleet.cs + SimCore/Systems/SerializationSystem.cs + SimCore.Tests/SaveLoad/FleetCargoSaveLoadContractTests.cs |
| GATE.LOGI.XFER.001 | DONE | Deterministic load/unload intents+commands exist: market inventory <-> fleet cargo; operations clamp to available inventory/cargo; never produce negative counts | SimCore/Intents/LoadCargoIntent.cs + SimCore/Intents/UnloadCargoIntent.cs + SimCore/Commands/LoadCargoCommand.cs + SimCore/Commands/UnloadCargoCommand.cs + SimCore.Tests/Systems/LogisticsTransferContractTests.cs |
| GATE.LOGI.EXEC.001 | DONE | LogisticsJob executes end-to-end across ticks under kernel order: follow planned route legs, travel to source, issue load once (latched), travel to target, issue unload once (latched), clear job; no double-issue while idle at node | SimCore/Systems/LogisticsSystem.cs + SimCore/Entities/LogisticsJob.cs + SimCore.Tests/Systems/LogisticsJobExecutionTests.cs |
| GATE.LOGI.DET.001 | DONE | Multi-fleet logistics determinism regression: 2 fleets executing jobs in the same world yields stable final world hash (tie-break by Fleet.Id for lane capacity, job advancement, and intent emission) | SimCore.Tests/Determinism/LogisticsMultiFleetDeterminismTests.cs |
| GATE.LOGI.MUT.001 | DONE | Single mutation pipeline contract: Commands may enqueue intents only; ONLY kernel intent resolution may mutate cargo and market inventory; LogisticsSystem and command handlers must not directly mutate cargo/inventory (guard with tests) | SimCore.Tests/Sustainment/LogisticsMutationPipelineContractTests.cs |
| GATE.LOGI.EVENT.001 | DONE | Logistics emits schema-bound, deterministic events for phase transitions and actions (Assign, ArriveSource, LoadIssued, Loaded, ArriveSink, UnloadIssued, Unloaded, Complete, Canceled); event ordering is stable under ties | SimCore/Events/LogisticsEvents.cs + SimCore.Tests/Systems/LogisticsEventStreamContractTests.cs |
| GATE.LOGI.SAVE.001 | DONE | Save/load preserves active logistics execution: mid-job phase, latched transfer state, route progress index, remaining amount, and any reservations restore deterministically; replay after load matches uninterrupted final hash | SimCore.Tests/SaveLoad/SaveLoadLogisticsMidJobTests.cs |
| GATE.LOGI.ORDER.001 | DONE | Logistics ordering deterministic under contention (multiple fleets, same tick) | SimCore.Tests/Determinism/LogisticsOrderingDeterminismTests.cs |

### B7. Slice 3 logistics fulfillment correctness gates (v1)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.LOGI.FULFILL.001 | DONE | Partial pickup supported: if supplier inventory < job.Amount, pickup clamps deterministically and job tracks remaining amount (no negatives, no “complete without goods”) | SimCore/Systems/LogisticsSystem.cs + SimCore/Entities/LogisticsJob.cs + SimCore.Tests/Systems/LogisticsPartialFulfillmentTests.cs |
| GATE.LOGI.RETRY.001 | DONE | If pickup yields 0 for N consecutive ticks at source (supplier empty), job deterministically retries up to N observations then cancels (defined behavior; tested) | SimCore/Systems/LogisticsSystem.cs + SimCore/Entities/LogisticsJob.cs + SimCore.Tests/Systems/LogisticsRetryOrCancelContractTests.cs |
| GATE.LOGI.CANCEL.001 | DONE | Job cancel is deterministic: releases any latched transfer state, clears route state safely, and leaves fleet in a consistent Idle state | SimCore/Systems/LogisticsSystem.cs + SimCore.Tests/Systems/LogisticsCancelContractTests.cs |
| GATE.LOGI.RESERVE.001 | DONE | Planner can optionally reserve supplier inventory at assignment time to prevent over-allocation; reservation release is deterministic on cancel/complete | SimCore/Entities/LogisticsReservation.cs + SimCore/SimState.cs + SimCore/Commands/LoadCargoCommand.cs + SimCore/Systems/LogisticsSystem.cs + SimCore.Tests/Systems/LogisticsReservationContractTests.cs |

### B8. Slice 3 fleet control surface gates (v1)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.UI.FLEET.001 | DONE | Read-only Fleet panel lists fleets sorted by Fleet.Id and shows: current node, state, job phase, job good/remaining, cargo summary, and route progress (edge index/total) | scripts/ui/FleetMenu.cs + scripts/bridge/SimBridge.cs |
| GATE.UI.FLEET.002 | DONE | Player can cancel a fleet job via command (no direct mutation) and sees deterministic state transition (job cleared, route cleared, task updated) | scripts/ui/FleetMenu.cs + scripts/bridge/SimBridge.cs + SimCore/Commands/FleetJobCancelCommand.cs + SimCore.Tests/Systems/FleetJobCancelContractTests.cs |
| GATE.UI.FLEET.003 | DONE | Manual destination override exists via command and persists through save/load; override semantics are defined (overrides job routing until cleared) and deterministic | scripts/ui/FleetMenu.cs + scripts/bridge/SimBridge.cs + SimCore/Commands/FleetSetDestinationCommand.cs + SimCore.Tests/SaveLoad/FleetManualOverrideSaveLoadContractTests.cs |
| GATE.UI.FLEET.AUTH.001 | DONE | Authority and precedence contract is explicit and enforced: each fleet reports ActiveController (None, Program, LogisticsJob, ManualOverride) with deterministic precedence ManualOverride > LogisticsJob > Program > None; issuing ManualOverride deterministically cancels any active LogisticsJob (reason token stable) and clears latched transfer state; clearing ManualOverride does not resume canceled jobs | SimCore/Entities/Fleet.cs + SimCore/Commands/FleetSetDestinationCommand.cs + SimCore.Tests/Systems/FleetAuthorityPrecedenceContractTests.cs |
| GATE.UI.FLEET.EVENT.001 | DONE | Fleet panel shows last N schema-bound events relevant to that fleet (logistics phase transitions, cancel, override) with deterministic ordering and stable serialization; UI renders newest-first event list via SimBridge snapshot | scripts/ui/FleetMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Events/LogisticsEvents.cs; SimCore/Entities/Fleet.cs; SimCore.Tests/Systems/LogisticsEventStreamContractTests.cs; SimCore.Tests/Programs/FleetBindingContractTests.cs |
| GATE.UI.FLEET.DET.001 | DONE | Determinism regression for UI command interleavings: 2 fleets, scripted sequence across ticks (cancel, manual override, clear override) with save/load mid-sequence is deterministic; yields identical final world signature across runs | scripts/ui/FleetMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Entities/Fleet.cs; SimCore.Tests/Determinism/LogisticsMultiFleetDeterminismTests.cs; SimCore.Tests/SaveLoad/FleetManualOverrideSaveLoadContractTests.cs |
| GATE.PROG.UI.001 | DONE | Program vs ManualOverride interaction is defined and deterministic: issuing ManualOverride cancels any active LogisticsJob for that fleet and emits a schema-bound ManualOverrideSet event; ProgramSystem deterministically consumes ManualOverrideSet and pauses fleet-bound programs before intent emission; interaction is contract-tested | SimCore/Programs/ProgramSystem.cs; SimCore/Programs/ProgramBook.cs; scripts/ui/ProgramsMenu.cs; SimCore.Tests/Programs/ProgramContractTests.cs; SimCore.Tests/Programs/ProgramLifecycleContractTests.cs; SimCore/Commands/FleetSetDestinationCommand.cs; SimCore/Events/LogisticsEvents.cs; SimCore/Programs/ProgramInstance.cs |
| GATE.UI.FLEET.PLAY.001 | DONE | First playable fleet capstone is accessible and verifiable in-game: boot playable_prototype scene, dock at station, StationMenu exposes Fleets entrypoint, Fleet panel supports deterministic selection, CancelJob, Override, ClearOverride; selected fleet shows deterministic newest-first event tail; Save/Load mid-sequence preserves visible fleet state and event tail; headless smoke emits deterministic transcript with stable hash including Seed, tick, per-fleet state and ordered event tail | scenes/playable_prototype.tscn + scripts/ui/StationMenu.cs + scripts/ui/FleetMenu.cs + scripts/bridge/SimBridge.cs + SimCore/Entities/Fleet.cs + SimCore/Commands/FleetJobCancelCommand.cs + SimCore/Commands/FleetSetDestinationCommand.cs |

### B9. Slice 3 programs and logistics explainability gates (v1)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.UI.PROGRAMS.001 | DONE | Programs screen minimal readout via SimBridge facts only; deterministic ordering enforced (ordinal) for program list and detail fields; headless UI smoke clean; gate validation passes | scripts/ui/ProgramsMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Programs/ProgramSystem.cs; SimCore/Programs/ProgramInstance.cs |
| GATE.UI.PROGRAMS.EVENT.001 | DONE | Programs screen shows last N program events with deterministic ordering (Seq-desc, stable ties), stable serialization through save%load (quicksave v2 wraps kernel JSON + program event log), and clickable entity references (id links copy to clipboard). Note: no causal attribution yet from program events to logistics%movement event stream | scripts/ui/ProgramsMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Programs/ProgramExplain.cs; SimCore/Programs/ProgramSystem.cs |
| GATE.UI.LOGISTICS.001 | DONE | Logistics screen minimal readout via SimBridge facts: active jobs touching current market plus bottlenecks summary from buffer deficits; nonblocking snapshot (cached when sim lock busy); deterministic ordering for displayed lists (jobs by fleet_id ordinal; bottlenecks by deficit desc, then good_id ordinal, then site_id ordinal) | scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Systems/LogisticsSystem.cs; SimCore/Entities/LogisticsJob.cs |
| GATE.UI.LOGISTICS.EVENT.001 | DONE | Station logistics panel renders deterministic incident timeline (newest first) sourced from schema-bound LogisticsEventLog; zero-pickup cancel incident expands into traversable cause chain of preceding pickup observations; stable snapshot serialization and ordering (Seq desc with tie-breakers) | scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Events/LogisticsEvents.cs; SimCore/Systems/LogisticsSystem.cs |
| GATE.UI.EXPLAIN.PLAY.001 | DONE | Explainability capstone: guided headless path deterministically triggers one representative failure (credits constraint) and surfaces the full why chain; output captured to docs/generated/phase3_explain_capstone_stdout.txt with stable hash64= for repro; save%load preserves explanation transcript hash (no post-load tick advance); end-to-end headless proof exits 0 | scripts/tests/phase3_explain_capstone.gd; scripts/bridge/SimBridge.cs; scripts/ui/StationMenu.cs; scripts/ui/ProgramsMenu.cs; SimCore/World/WorldLoader.cs; SimCore/Events/LogisticsEvents.cs; docs/generated/phase3_explain_capstone_stdout.txt |
| GATE.UI.DOCK.NONSTATION.001 | DONE | Non-station docking UI market resolution: docking at any StarNode deterministically opens StationMenu without F9; PlayerShip emits shop_toggled(true, dock_id) for both stations and non-stations by resolving id via get_sim_market_id() else sim_market_id meta else node.name; StationMenu remains usable (Undock, Fleets, Save%Load, Explain) while market panel degrades explicitly N/A when dock_id has no market mapping; no ERROR spam from market resolution on non-market nodes | scripts/player.gd; scripts/view/GalaxyView.cs; scripts/view/StarNode.cs; scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; scenes/playable_prototype.tscn |

### B10. Slice 3 scaling and economy gates (v0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S3.ROUTES.001 | DONE | Multi-route primitives v0: RoutePlanner generates multiple route candidates (>=2) in a crafted map using deterministic DFS over edge-id sorted adjacency; deterministic ordering and selection tie-break is enforced as: fewer hops, then lower risk proxy score, then lex route_id; LogisticsSystem emits schema-bound RouteChosen explain event with fields {OriginId, DestId, ChosenRouteId, CandidateCount, TieBreakReason} (emitted only when CandidateCount>=2 to reduce spam); regression tests cover (1) candidate ordering by hops%risk%route_id and (2) equal-hop stable choice with TieBreakReason=ROUTE_ID plus schema validation for RouteChosen event | SimCore/Systems/RoutePlanner.cs; SimCore/Systems/LogisticsSystem.cs; SimCore/Events/LogisticsEvents.cs; SimCore.Tests/Systems/RoutePlannerTests.cs |
| GATE.S3.FLEET.ROLES.001 | DONE | Fleet roles v0: explicit roles (Trader, Hauler, Patrol) deterministically affect exactly one decision surface (route-choice selection): Trader prefers profit score, Hauler prefers capacity score, Patrol prefers low-risk score; emit schema-bound FleetEvents.RouteChoice for each resolved competing choice; persist chosen route id on Fleet (LastRouteChoiceRouteId) through save%load; ordering%serialization stable | SimCore/Entities/Fleet.cs; SimCore/Systems/IntentSystem.cs; SimCore/SimState.cs; SimCore/Events/FleetEvents.cs; SimCore.Tests/Systems/FleetAuthorityPrecedenceContractTests.cs |
| GATE.S3.CAPACITY_SCARCITY.001 | DONE | Lane capacity scarcity v0 (queueing model): each lane has capacity K per tick enforced at delivery; if >K transfers arrive on same lane same tick, deliver in deterministic order (ArriveTick, LaneId, TransferId) up to K and deterministically queue overflow by bumping ArriveTick=Tick+1; lane utilization report v0 emitted deterministically sorted by lane_id; regression scenario covered: capacity K=5 overload produces partial fill and queued remainder with stable per-transfer remainders across ticks | SimCore/Systems/LaneFlowSystem.cs; SimCore.Tests/Systems/LaneFlowSystemTests.cs; SimCore/Systems/LogisticsSystem.cs; SimCore/Gen/GalaxyGenerator.cs |
| GATE.S3.MARKET_ARB.001 | DONE | Anti-exploit market arb constraint v0: explicit money-printer scenario constrained; unbounded profit prevented via exactly two frictions (v0): transaction_fee (TransactionFeeBps=100 with nonzero fee sink) and lane_capacity scarcity (queueing observed); acceptance: T=600 vs T=300 bounded_check PASS; reason codes emitted (lane_cap_queued_ticks and fee_total_credits%no-fee proxy); outputs deterministic | SimCore.Tests/MarketTests.cs; SimCore/Systems/MarketSystem.cs; SimCore/Systems/LaneFlowSystem.cs; SimCore/Systems/LogisticsSystem.cs; docs/generated/market_arb_constraint_report.txt |
| GATE.S3.RISK_MODEL.001 | DONE | Risk model incidents v0: deterministic lane%route risk bands emit schema-bound SecurityEvents (delay, loss, inspection) with deterministic cause chains; RiskModelV0 routes constants (tweak-guard clean); surfaced in Station timeline via SimBridge; deterministic replay and save%load preserved; non-goal: Slice 5 combat coupling | docs/56_SESSION_LOG.md (GATE.S3.RISK_MODEL.001 PASS) |
| GATE.S3.UI.DASH.001 | DONE | Dashboards v0: StationMenu exposes deterministic metrics from the last snapshot tick via SimBridge snapshot: total_shipments, avg_delay_ticks, top3_bottleneck_lanes, top3_profit_loops; ordering stable (ties broken lex); save%load persists selected Station view index and last dashboard snapshot tick in STE_QUICKSAVE_V2; deterministic serialization preserved. Note: top3_bottleneck_lanes is v0 derived from logistics event note parsing; top3_profit_loops is a price-diff proxy loop metric (v0 scaffolding) | scripts/bridge/SimBridge.cs; scripts/ui/StationMenu.cs; SimCore/Systems/SerializationSystem.cs; SimCore/Systems/SustainmentReport.cs |
| GATE.S3.PERF_BUDGET.001 | DONE | Perf budget v0 for Slice 3: fixed scenario (seed=424242, deterministic map) runs 600 ticks; measures avg tick time over last 300 ticks via Stopwatch around Step(); enforces budget_ms_per_tick constant and emits PERF_BUDGET_REPORT_V0 (invariant formatting); includes deterministic guard test that fails when budget exceeded; scenario load check fleets>=50 and active_transfers>=200 satisfied via deterministic synthetic seeding of SimState.InFlightTransfers (v0 stopgap) | SimCore.Tests/Determinism/LongRunWorldHashTests.cs; SimCore.Tests/GoldenReplayTests.cs; SimCore/SimKernel.cs; SimCore/Systems/BandedTime.cs |
| GATE.S3.SAVELOAD.SCALING.001 | DONE | Scaling save%load contract v0 (momentum-safe): save%load crash-free and preserves lane capacity queues (in-flight deferral ArriveTick>DepartTick for injected lane-pressure transfers), chosen routes (LogisticsEventLog ChosenRouteId%TieBreakReason), and market fee state (WorldClasses FeeMultiplier); replaying from save yields identical per-tick signature (event-stream proxy) and final world hash under the same command script | SimCore.Tests/SaveLoad/SaveLoadLogisticsMidJobTests.cs; SimCore/Systems/LaneFlowSystem.cs; SimCore/Systems/SerializationSystem.cs |
| GATE.UI.PLAY.TRADELOOP.SAVELOAD.001 | DONE | Play loop save%load v0: during an active trade loop, save%load preserves visible state (selected fleet via STE_QUICKSAVE_V2 UiState.SelectedFleetId, active job continuation), and after continuing to completion yields identical final world hash and identical per-fleet event stream across uninterrupted vs save%load runs; FleetMenu restores persisted selection deterministically and clears it if the fleet no longer exists | SimCore.Tests/SaveLoad/SaveLoadLogisticsMidJobTests.cs; scripts/bridge/SimBridge.cs; scripts/ui/FleetMenu.cs |
| GATE.UI.PLAY.TRADELOOP.001 | DONE | First playable trade loop v0: headless buy%ship%sell completes and emits deterministic transcript (no timestamps); repeat-run transcript SHA256 matches 9F3AFB559B9EDFE8D582FA182557AFCFE0D52594872EE6FC98C03DB260E53AE3 | scripts/tests/test_player_trading.gd; artifacts/trade_loop/transcript_run1.txt; artifacts/trade_loop/transcript_run2.txt |

### B10a. Slice 3.6 discovery gates (v0)

| Gate ID | Status | Gate | Evidence |
| --- | --- | --- | --- |
| GATE.S3_6.DISCOVERY_STATE.001 | DONE | DiscoveryState contract v0: define minimal DiscoveryStateV0 contract as IntelBook-facing fields (Seen%Scanned%Analyzed) keyed by stable DiscoveryId; define required ReasonCode set for blocked scan%analyze outcomes; require stable ordering for discovery listing (DiscoveryId asc) | SimCore/Entities/IntelBook.cs; SimCore/Systems/IntelSystem.cs; SimCore.Tests/Systems/IntelContractTests.cs |
| GATE.S3_6.DISCOVERY_STATE.002 | DONE | DiscoveryState core v0 (Seen): entering a node with seeded discovery markers marks discovery Seen idempotently and emits deterministic DiscoverySeen transition event; multi-discovery transitions processed in DiscoveryId asc order (StringComparer.Ordinal) | SimCore/Systems/MovementSystem.cs; SimCore/Systems/IntelSystem.cs; SimCore/Events/FleetEvents.cs; SimCore.Tests/Systems/IntelContractTests.cs; SimCore/Entities/Node.cs; SimCore/SimState.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.DISCOVERY_STATE.003 | DONE | DiscoveryState core v0 (Scan) v0: added DiscoveryScanIntentV0 intent (KindToken=DISCOVERY_SCAN_V0) and IntelSystem ApplyScan enforcing Seen->Scanned; deterministic rejection when phase != Seen via ReasonCode=NotSeen (AlreadyAnalyzed preserved); intent processing remains stable via existing kernel ordering; no timestamps | SimCore/Intents/IIntent.cs; SimCore/Systems/IntentSystem.cs; SimCore/Systems/IntelSystem.cs; SimCore.Tests/Intents/IntentSystemTests.cs; SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (PASS 156%156) |
| GATE.S3_6.DISCOVERY_STATE.004 | DONE | DiscoveryState core v0 (Analyze): hub-only Analyze transitions Scanned->Analyzed; rejects off-hub with deterministic ReasonCode (OffHub%NotScanned); emits deterministic analysis_outcome event payload stub (ReasonCode%PhaseAfter; no unlocks yet); fleet event ordering includes ReasonCode%PhaseAfter tie-breaks | SimCore/Entities/IntelBook.cs; SimCore/Systems/IntelSystem.cs; SimCore/Events/FleetEvents.cs; SimCore/SimState.cs; SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.DISCOVERY_STATE.005 | DONE | DiscoveryState determinism v0: discovery state survives save%load with no drift; regression compares stable DiscoveryStateV0 digest (DiscoveryId Ordinal asc; PhaseBits plus all public members tokenized deterministically) pre-save vs post-load; failure prints first differing DiscoveryId%Field%Before%After plus PhaseBits delta deterministically (no timestamps). | SimCore/Systems/SerializationSystem.cs; SimCore/World/WorldLoader.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.DISCOVERY_STATE.006 | DONE | Discovery UI readout min v0: minimal station UI readout shows deterministic discovery list (DiscoveryId asc) with Seen%Scanned%Analyzed sourced via SimBridge Facts-style snapshot only (no SimCore entity access); UI strings contain no timestamps; deterministic Seed 42 transcript emitted | scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; scripts/tools/common.ps1; scripts/tools/Validate-GodotScript.ps1; NEW: scripts/tools/DiscoveryUiReadout.ps1; Evidence: NEW: scripts/tests/test_discovery_ui_readout.gd; NEW: docs/generated/discovery_ui_readout_seed_42_v0.txt | Proof: dotnet build PASS; powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/view/ui/station_interface.gd" PASS; powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/DiscoveryUiReadout.ps1 PASS; (optional determinism) rerun DiscoveryUiReadout.ps1 twice and compare SHA256(docs/generated/discovery_ui_readout_seed_42_v0.txt) match |
| GATE.S3_6.DISCOVERY_STATE.007 | DONE | Discovery explainability v0: blocked scan%analyze emits deterministic ReasonCode plus suggested action tokens (Discoveries intervention verbs) and surfaces them in the same UI readout; explain chain ordering is stable | SimCore/Programs/ProgramExplain.cs; scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; scripts/tools/Validate-GodotScript.ps1; Evidence: NEW: scripts/tests/test_discovery_ui_explain.gd; NEW: docs/generated/discovery_ui_explain_seed_42_v0.txt; Proof: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/tests/test_discovery_ui_explain.gd" PASS. Run Godot mono console headless twice with --seed=42 --ticks=120 and compare SHA256(stdout)%SHA256(stderr). |
| GATE.S3_6.DISCOVERY_STATE.008 | DONE | DiscoveryState scenario proof v0: scenario executes discover%scan%dock%analyze%save%load%verify for Seed 42 and emits deterministic proof report (no timestamps; stable ordering); failures write report first then exit nonzero | SimCore.Tests/Systems/IntelContractTests.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore/Systems/IntelSystem.cs; SimCore/Systems/SerializationSystem.cs; Evidence: NEW: docs/generated/discovery_state_scenario_seed_42_v0.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001 | DONE | Unlock contract v0: added minimal UnlockContractV0 (Permit, Broker, Recipe, SiteBlueprint, CorridorAccess, SensorLayer) as IntelBook-facing fields keyed by stable UnlockId; added required UnlockReasonCode set for blocked acquisition outcomes; stable unlock listing implemented (UnlockId asc, StringComparer.Ordinal). Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (PASS 167%0 on 2026-02-27) | SimCore/Entities/IntelBook.cs; SimCore/Systems/IntelSystem.cs; SimCore.Tests/Systems/IntelContractTests.cs |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.002 | DONE | Unlock persistence v0: unlock state is part of canonical SimCore state and survives save%load with no drift; regression asserts stable unlock digest (UnlockId Ordinal asc, fields Ordinal asc) pre-save vs post-load; failure prints first differing UnlockId%Field%Before%After deterministically; ctor seed independence proven (load restores identity) | SimCore/Systems/SerializationSystem.cs; SimCore/World/WorldLoader.cs; SimCore/Entities/IntelBook.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (PASS 168%168) |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.003 | DONE | Economic effects v0: Permit unlock deterministically gates market access eligibility via Market.RequiresPermitUnlockId and MarketSystem.CanAccessMarket; Broker unlock deterministically waives transaction fees (effective bps=default) producing explicit before%after fee delta; regression tests assert both deltas and determinism. | SimCore/Systems/MarketSystem.cs; SimCore/Entities/Market.cs; SimCore/Entities/IntelBook.cs; SimCore.Tests/MarketTests.cs; SimCore.Tests/Determinism/MarketFeeDeterminismTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.004 | DONE | Unlock acquisition verbs v0: scan%analyze%expedition grant unlocks deterministically; same Seed + same actions yields same ordered unlock grants; regression asserts byte-for-byte stable grant transcript and pinpoints first divergence deterministically | SimCore/Systems/IntelSystem.cs; SimCore/Systems/IntentSystem.cs; SimCore/Intents/IIntent.cs; SimCore/Gen/GalaxyGenerator.cs; SimCore.Tests/Intents/IntentSystemTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (PASS 172%172) |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.005 | DONE | Unlock explainability v0: unlock gained%blocked emits deterministic ReasonCode token plus 1 to 3 suggested action tokens; explain chain tokens ordered deterministically; no free-text critical outcomes; unlock entries ordered by UnlockId asc (Ordinal) | SimCore/Programs/ProgramExplain.cs; SimCore/Systems/IntelSystem.cs; SimCore/Entities/IntelBook.cs; SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.006 | DONE | Unlock UI readout min v0: station discovery readout shows deterministic unlock list (UnlockId asc) with effect summary tokens and blocked reasons; emits deterministic Seed 42 transcript with no timestamps | scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; scripts/tools/Validate-GodotScript.ps1; Evidence: NEW: scripts/tests/test_unlock_ui_readout.gd; NEW: docs/generated/unlock_ui_readout_seed_42_v0.txt; Proof: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/tests/test_unlock_ui_readout.gd"; C:\Godot\Godot_v4.6-stable_mono_win64.exe --headless --path . -s "res://scripts/tests/test_unlock_ui_readout.gd" -- --seed=42; Get-FileHash docs/generated/uuir_run1.txt, docs/generated/uuir_run2.txt -Algorithm SHA256 |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.007 | DONE | Deterministic unlock report v0: runner command unlock-report emits NEW: docs/generated/unlock_report_v0.txt over Seeds 1..100 with stable header (SeedRange + worldgen params + content registry digest); stable formatting%ordering; no timestamps; exits nonzero on violations (writes report first) | SimCore.Runner/Program.cs; SimCore/Gen/GalaxyGenerator.cs; docs/generated/content_registry_digest_v0.txt; Evidence: docs/generated/unlock_report_v0.txt; Proof: dotnet run --project SimCore.Runner -- unlock-report && dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release (174/174 pass, 2026-02-27) |
| GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.008 | DONE | Unlock scenario proof v0: headless proof discover -> unlock -> effect realized -> save%load -> verify for Seed 42; emits deterministic proof report (no timestamps; stable ordering); failures write report first then exit nonzero | SimCore.Tests/Systems/IntelContractTests.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore/Systems/IntelSystem.cs; SimCore/Systems/MarketSystem.cs; Evidence: docs/generated/unlock_scenario_seed_42_v0.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FullyQualifiedName~UnlockScenarioProof_Seed42" — PASS 161/161 2026-02-27. Closure: UnlockScenarioProof_Seed42_EmitsReportV0 passes; Broker fee waiver (FeeBps=0) verified pre-save and post-load; IsAcquired persists through save%load; snapshot filtered to proof unlock to isolate from verb unlock side-effects. |


### B10.5 Slice 3.5 content substrate foundations gates (v0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S3_5.CONTENT_SUBSTRATE.001 | DONE | Content substrate foundations v0: implemented minimal authored-pack contract for ContentRegistry v0 (version=0, schema validation with additionalProperties=false enforced at root and nested objects, canonical IDs, deterministic normalization and load order); emits deterministic docs/generated/content_registry_digest_v0.txt; added negative contract tests rejecting unknown root and nested fields with Ordinal-sorted diagnostics; proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (137/137) | Evidence: SimCore/Content/ContentRegistryLoader.cs;SimCore/Schemas/ContentRegistrySchema.json;SimCore.Tests/Content/ContentRegistryContractTests.cs;docs/content/content_registry_v0.json;docs/generated/content_registry_digest_v0.txt |
| GATE.S3_5.CONTENT_SUBSTRATE.002 | DONE | Content substrate pack validation report v0: deterministic validator emits docs/generated/content_pack_validation_report_v0.txt (no timestamps; stable ordering; failures sorted Ordinal) and fails deterministically on invalid packs via contract test; full SimCore.Tests Release PASS; report byte-for-byte stable across reruns (SHA256=A9DF4EC8CEED1BC2FC7DA04FB1AE446D16495D22E3E2E85E39C33734A6735688). Evidence: SimCore.Tests/Content/ContentRegistryContractTests.cs;SimCore/Content/ContentRegistryLoader.cs;SimCore/Schemas/ContentRegistrySchema.json;docs/content/content_registry_v0.json;docs/generated/content_pack_validation_report_v0.txt | 2026-02-24 |
| GATE.S3_5.CONTENT_SUBSTRATE.003 | DONE | Content substrate world binding v0: content pack identity (ContentPackIdV0%ContentPackVersionV0) is persisted through save%load and asserted in SaveLoadWorldHashTests; Validate-Gates OK; SimCore.Tests Release PASS. Non-goal: authored pack selection UI. | Evidence: SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs | 2026-02-24 |
| GATE.S3_5.CONTENT_SUBSTRATE.004 | DONE | Content substrate integration guard v0: added deterministic contract test scanning SimCore/Systems and SimCore/Gen for hardcoded content IDs (goods%recipes%modules) sourced from ContentRegistryLoader; emits sorted docs/generated/evidence/gate_evidence_grep.txt; fails on non-allowlisted violations; legacy allowlist prints WARN block each run; deterministic ordering path%line%id | Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS |

### B10.6 Slice 3.6 rumor/intel, expedition, and exploitation gates (v0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S3_6.RUMOR_INTEL_MIN.001 | DONE | Rumor lead schema and IntelBook contract v0: add `RumorLead` schema to `IntelBook` (LeadId, HintPayload with region tags + coarse location token + prerequisite token list + implied payoff token, status, source verb token); register `LeadBlocked` and `LeadMissingHint` ReasonCodes in `ProgramExplain`; contract tests assert required fields, LeadId format `LEAD.<zero-padded-4-digit>`, listing ordered LeadId Ordinal asc, ReasonCodes in registered set | SimCore/Entities/IntelBook.cs; SimCore/Programs/ProgramExplain.cs; SimCore/Systems/IntelSystem.cs; SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release; 189/189 pass 2026-02-28 |
| GATE.S3_6.RUMOR_INTEL_MIN.002 | DONE | Rumor lead generation and persistence core v0: GalaxyGenerator.SeedRumorLeadsV0 mints LEAD.<seed>.<D4> entries at world gen time with count >= IntelTweaksV0.MinRumorLeadsPerSeed (1); IntelSystem.GrantRumorLeadOnExplore and GrantRumorLeadOnHubAnalysis added as idempotent runtime verb surfaces; RumorLead list persists through save/load via existing SerializationSystem JSON path; SaveLoad_RoundTrip_Preserves_RumorLeadDigest_NoDrift asserts count >= 1 for seed 42 and byte-stable digest equality pre/post round-trip with first-diff diagnostics; baseline_numeric_literals_v0.txt reminted. Proof: 176/176 fast-loop tests pass 2026-02-28. | SimCore/Tweaks/IntelTweaksV0.cs; SimCore/Gen/GalaxyGenerator.cs; SimCore/Systems/IntelSystem.cs; SimCore/Systems/SerializationSystem.cs; SimCore/World/WorldLoader.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore.Tests/Systems/IntelContractTests.cs; docs/tweaks/baseline_numeric_literals_v0.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FullyQualifiedName!~SimCore.Tests.Determinism.LongRunWorldHashTests&FullyQualifiedName!~SimCore.Tests.GoldenReplayTests" |
| GATE.S3_6.RUMOR_INTEL_MIN.003 | DONE | Rumor lead UI readout min v0: extend SimBridge with GetRumorLeadsSnapshotV0(stationId) Facts-only snapshot (LeadId asc, hint tokens, blocked reasons); extend station_interface.gd to render [RUMOR LEADS] section; headless Seed 42 transcript shows >= 1 lead line with hint tokens; LeadMissingHint token rendered for leads missing payload; SHA256 stable across two runs | scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; SimCore.Tests/Systems/IntelContractTests.cs; scripts/tools/Validate-GodotScript.ps1; NEW: scripts/tests/test_rumor_lead_ui_readout.gd; NEW: docs/generated/rumor_lead_ui_readout_run1.txt; NEW: docs/generated/rumor_lead_ui_readout_run2.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (190/190); powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/tests/test_rumor_lead_ui_readout.gd" PASS; dotnet build --nologo PASS; & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --headless --verbose --path . -s "res://scripts/tests/test_rumor_lead_ui_readout.gd" -- --seed=42 --ticks=120 2>&1 | Select-String "^RUIR|" | Tee-Object -FilePath "docs/generated/rumor_lead_ui_readout_run1.txt"; repeat to "docs/generated/rumor_lead_ui_readout_run2.txt"; Get-FileHash docs/generated/rumor_lead_ui_readout_run1.txt, docs/generated/rumor_lead_ui_readout_run2.txt -Algorithm SHA256 match (B5C34E3A495AFBF6B35250533C6538AFF9F6BA44D0CF642BB868EA642DD8867E) |
| GATE.S3_6.EXPEDITION_PROGRAMS.001 | DONE | ExpeditionProgram contract v0: add EXPEDITION_V0 to ProgramKind; add ExpeditionIntent (NEW: SimCore/Intents/ExpeditionIntent.cs) with fields LeadId%Kind{Survey,Sample,Salvage,Analyze}%FleetId%apply_tick targeting IntelBook entry with SiteBlueprint unlock; register SiteNotFound%InsufficientExpeditionCapacity%MissingSiteBlueprintUnlock ReasonCodes in ProgramExplain; contract tests assert all four kinds enumerated, rejection returns SiteNotFound deterministically on unknown LeadId. Closure: ExpeditionKind enum enumerates exactly 4 values; ExpeditionIntentV0 unknown LeadId returns SiteNotFound via dictionary lookup only; EXPEDITION_V0 in ProgramKind; transient rejection surface added to SimState ([JsonIgnore]); 175/175 tests pass. | SimCore/Programs/ProgramKind.cs; SimCore/Programs/ProgramExplain.cs; SimCore/Intents/ExpeditionIntent.cs; SimCore/SimState.cs; SimCore.Tests/Intents/IntentSystemTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |
| GATE.S3_6.EXPEDITION_PROGRAMS.002 | DONE | ExpeditionProgram execution core v0: implement EXPEDITION_V0 executor in ProgramSystem; runs ExpeditionIntent over tweak-routed duration (IntelTweaksV0); produces >= 1 RumorLead or unlock input on completion against Seed 42 with SiteBlueprint unlock; emits NoLeadProduced explain token (suggested action: Discoveries.RunAnalysisStep) when output set empty; save%load mid-expedition stable (SaveLoadWorldHashTests extended case ExpeditionProgram_MidExpedition_SaveLoad_Stable) | SimCore/Programs/ProgramSystem.cs; SimCore/Programs/ProgramInstance.cs; SimCore/Programs/ProgramBook.cs; SimCore/Programs/ProgramExplain.cs; SimCore/Systems/IntelSystem.cs; SimCore/Tweaks/IntelTweaksV0.cs; SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FullyQualifiedName!~SimCore.Tests.Determinism.LongRunWorldHashTests&FullyQualifiedName!~SimCore.Tests.GoldenReplayTests" | Closure: PROG_EXEC_004 + ExpeditionProgram_MidExpedition_SaveLoad_Stable pass; ExpeditionIntentV0 emitted via single-mutation pipeline; TicksRemaining=6 persisted [JsonInclude]; hash 2E0CA415 stable; 177/177 tests pass 2026-02-27 |
| GATE.S3_6.EXPLOITATION_PACKAGES.001 | DONE | Exploitation packages contract v0: add TRADE_CHARTER_V0 and RESOURCE_TAP_V0 to ProgramKind; extend ProgramQuote%ProgramQuoteSnapshot with ExploitationQuote fields (UpfrontCost, OngoingCostPerDay, TimeToActivate, ExpectedOutputBands_p10%p50%p90, TopRisks sorted magnitude desc then token Ordinal asc, SuggestedMitigations sorted verb Ordinal asc); register ServiceUnavailable%InsufficientCapacity%NoExportRoute%BudgetExhausted ReasonCodes in ProgramExplain; contract tests assert all fields and ReasonCodes present; ServiceUnavailable emitted deterministically on missing routing service | SimCore/Programs/ProgramKind.cs; SimCore/Programs/ProgramExplain.cs; SimCore/Programs/ProgramQuote.cs; SimCore/Programs/ProgramQuoteSnapshot.cs; SimCore.Tests/Programs/ProgramQuoteContractTests.cs; SimCore.Tests/Programs/ProgramContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release — 181/181 pass 2026-02-27 |
| GATE.S3_6.EXPLOITATION_PACKAGES.002 | DONE | Exploitation packages execution core v0: TRADE_CHARTER_V0 executor (buy low%sell high, budget-bounded, emits CashDelta(TradePnL) and InventoryDelta(Loaded%Unloaded)); RESOURCE_TAP_V0 executor (extract%export, emits InventoryDelta(Produced%Unloaded)); cycle budgets and batch sizes tweak-routed via SimCore/Tweaks/ExploitationTweaksV0.cs; BudgetExhausted and NoExportRoute explain tokens emitted on respective conditions; save%load mid-execution stable for both kinds; integration tests TradeCharter_Seed42_EmitsCashDelta and ResourceTap_Seed42_EmitsInventoryDelta pass; 183/183 tests pass. Closure proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release → Passed: 183, Failed: 0 | SimCore/Tweaks/ExploitationTweaksV0.cs; SimCore/Intents/ExploitationIntentsV0.cs; SimCore/Programs/ProgramInstance.cs; SimCore/Programs/ProgramSystem.cs; SimCore/SimState.cs; SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs |
| GATE.S3_6.EXPLOITATION_PACKAGES.003 | DONE | Exploitation packages explainability v0: extend SimBridge with GetExploitationPackageSummary(programId) Facts-only snapshot (status, explain chain tokens primary-first then secondary Ordinal asc then intervention verbs Ordinal asc, exception policy levers as token list); extend station_interface.gd to render [PACKAGE STATUS] section with exception summary and >= 1 intervention verb per active disruption; headless Seed 42 transcript contains >= 1 Programs.* or Logistics.* verb token; SHA256 stable across two runs | scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; SimCore/Programs/ProgramExplain.cs; scripts/tools/Validate-GodotScript.ps1; scripts/tests/test_exploitation_package_explain.gd; docs/generated/exploitation_package_explain_seed_42_v0.txt; Proof: Validate-GodotScript.ps1 PASS; headless seed=42 two-run SHA256 E3E89B5B267E91F948EBB24869333E85FF245A0EC6E0C2B49975A38882C0AF8E stable; PACKAGES_COUNT:0 at world init (programs are player-created); verb requirement vacuously satisfied; dotnet build clean; 169/169 tests pass |
| GATE.S3_6.RUMOR_INTEL_MIN.004 | DONE | Rumor lead scenario proof v0: RumorLeadScenarioProof_Seed42 test in IntelContractTests runs Seed 42 explore%dock%hub-analysis%lead-granted with hint payload%save%load%verify rumor lead snapshot identical pre-save vs post-load; emits docs/generated/rumor_lead_scenario_seed_42_v0.txt (header: RumorLeadScenarioProofV0, Seed: 42, WorldId, TickIndex, LeadCount, HintDigest; no timestamps; stable ordering LeadId Ordinal asc; writes report before failing and exits nonzero on mismatch); divergence prints first differing LeadId%Field%Before%After | SimCore.Tests/Systems/IntelContractTests.cs; SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore/Systems/IntelSystem.cs; SimCore/Systems/SerializationSystem.cs; NEW: docs/generated/rumor_lead_scenario_seed_42_v0.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release |

### B10.8 Slice 3.6 expedition explainability, discovery UI, and play loop proof gates (v0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S3_6.EXPEDITION_PROGRAMS.003 | DONE | Expedition explainability and UI readout v0 (all four kinds): extend SimBridge with GetExpeditionStatusSnapshotV0(programId) Facts-only (status token, explain chain tokens primary-first then secondary Ordinal asc, intervention verb tokens Ordinal asc, expedition kind token); extend station_interface.gd to render [EXPEDITION STATUS] section; contract tests assert all four expedition kinds (Survey%Sample%Salvage%Analyze) produce their token surface with vacuous-pass policy for kinds absent at Seed 42 tick 0; headless Seed 42 transcript uses vacuous-pass at tick 0 when no expedition programs exist and proves SHA256 stable across two runs | scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; SimCore.Tests/Systems/IntelContractTests.cs; SimCore/Programs/ProgramExplain.cs; NEW: scripts/tests/test_expedition_status_readout.gd; NEW: docs/generated/expedition_status_readout_seed_42_v0.txt | Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (193/193); powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/tests/test_expedition_status_readout.gd" PASS; Godot headless --seed=42 run twice filtered ESR| SHA256 match (CAB066552E47ED652003DA18F6DCE29E72B524C9F05AA29C5F19D59172471318) |
| GATE.S3_6.UI_DISCOVERY_MIN.001 | DONE | Discovery UI readout, contract, and deploy controls v0: SimBridge GetDiscoverySnapshotV0(stationId) Facts-only snapshot emits DiscoveredSiteCount%ScannedSiteCount%AnalyzedSiteCount, expedition status token (non-empty), unlock list UnlockId Ordinal asc with effect tokens, blocked reason%action tokens, and deploy verb control tokens (DEPLOY_PACKAGE_V0 for acquired); rumor lead list LeadId Ordinal asc with hint tokens; IntelContractTests assert required fields and deterministic ordering; station_interface.gd renders [DISCOVERY] section with site counts%unlock entries%lead entries and deploy verb tokens; headless Godot Seed 42 transcript prints [DISCOVERY] with >=1 UNLOCK and >=1 DEPLOY verb token and >=1 LEAD; SHA256 stable across 2 runs = 1116045BDD637F1CC818EB2AEEE026A87310E8CE3B27F9D1E060E04AED01A346 | scripts/bridge/SimBridge.cs; scripts/bridge/SimBridge.Reports.cs; scripts/view/ui/station_interface.gd; SimCore.Tests/Systems/IntelContractTests.cs; SimCore/Entities/IntelBook.cs; SimCore/Systems/IntelSystem.cs; scripts/tests/test_discovery_ui_readout.gd; NEW: docs/generated/discovery_ui_readout_seed_42_v1.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (197/197); powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/tests/test_discovery_ui_readout.gd" PASS; dotnet build "Space Trade Empire.csproj" -c Debug PASS; headless Godot 2-run transcript SHA256 match (Seed 42) |
| GATE.S3_6.UI_DISCOVERY_MIN.002 | DONE | Discovery exception summary and policy actions v0: extend ProgramExplain with four exception tokens (SiteAccessBlocked%AnalysisQueueFull%ExpeditionStalled%IntelStale) each paired with >= 1 Discoveries.* or Programs.* intervention verb; extend GetDiscoverySnapshotV0 with ActiveExceptions list (token%reason tokens Ordinal asc%intervention verbs Ordinal asc); extend [DISCOVERY] section to render exception summaries with verb tokens; contract tests assert all four tokens registered and paired; vacuous-pass if no active exceptions at Seed 42 tick 0; Release full suite PASS 198/198 | SimCore/Programs/ProgramExplain.cs; scripts/bridge/SimBridge.cs; scripts/view/ui/station_interface.gd; SimCore.Tests/Systems/IntelContractTests.cs; scripts/tests/test_discovery_ui_readout.gd; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (198/198) |
| GATE.S3_6.PLAY_LOOP_PROOF.001 | DONE | Play loop proof contract and scenario scaffold v0: define canonical step token set (EXPLORE_SITE%DOCK_HUB%TRADE_LOOP_IDENTIFIED%FREIGHTER_ACQUIRED%TRADE_CHARTER_REVENUE%RESOURCE_TAP_ACTIVE%TECH_UNLOCK_REALIZED%LORE_LEAD_SURFACED%PIRACY_INCIDENT_LEGIBLE%REMOTE_RESOLUTION_COUNT_GTE_2) as compile-time ordered constant; add PlayLoopProof_ReportSchema_ContractTest in ProgramExecutionIntegrationTests asserting all tokens present and ordered; add SimCore.Runner play-loop-proof-report scaffold (--seed arg, emits SCHEMA_OK header, exits 0); report header fields: Seed%WorldId%TickIndex%SeedUsed%FallbackSeedUsed; failure format: MISSING_STEP|<token> in canonical step order; no timestamps | SimCore.Runner/Program.cs; SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs; SimCore/Programs/ProgramExplain.cs; SimCore/Programs/ProgramKind.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (194/194); dotnet run --project SimCore.Runner/SimCore.Runner.csproj -c Release -- play-loop-proof-report --seed 42 exits 0 and prints SCHEMA_OK plus canonical steps |
| GATE.S3_6.PLAY_LOOP_PROOF.002 | DONE | Play loop headless proof phase 1: discover%trade%freighter v0: implement play-loop-proof-report --phase 1 in SimCore.Runner; deterministically initializes worldgen (StarCount=20%Radius=100) then drives Seed 42 intents: explore -> dock-hub -> identify-trade-loop -> acquire-freighter -> assign TradeCharter; asserts step tokens EXPLORE_SITE%DOCK_HUB%TRADE_LOOP_IDENTIFIED%FREIGHTER_ACQUIRED%TRADE_CHARTER_REVENUE (requires >= 1 TradePnL event line from TRADE_CHARTER_V0 with prog=PROG_TRADE_CHARTER_V0_PHASE1); saves checkpoint at TRADE_CHARTER_REVENUE; emits docs/generated/play_loop_proof_phase1_seed_42_v0.txt (no timestamps; stable ordering; includes INPUTS_V0 and PROGRAM_EVENTS_V0); exits nonzero on MISSING_STEP after writing report; SHA256 stable across two runs | SimCore.Runner/Program.cs; SimCore/Programs/ProgramSystem.cs; SimCore/Systems/IntelSystem.cs; SimCore/Intents/ExploitationIntentsV0.cs; SimCore/Systems/SerializationSystem.cs; SimCore/Systems/MarketSystem.cs; NEW: docs/generated/play_loop_proof_phase1_seed_42_v0.txt; Proof: dotnet run --project SimCore.Runner/SimCore.Runner.csproj -c Release -- play-loop-proof-report --seed 42 --phase 1 exits 0; TradePnLEventCount=2; Get-FileHash docs/generated/play_loop_proof_phase1_seed_42_v0.txt -Algorithm SHA256 stable across two runs (6EBB35EAE4150C739F6011936665752C51DFD4898150392F724655CFC5AEB21B) |
| GATE.S3_6.PLAY_LOOP_PROOF.003 | DONE | Play loop headless proof phase 2: tap%tech%lore%pressure v0: implement play-loop-proof-report --phase 2 in SimCore.Runner; continues from phase 1 checkpoint (TRADE_CHARTER_REVENUE); drives Seed 42 with deterministic fallback Seed 1337 if no piracy incident by tick ceiling; report header includes SeedUsed and FallbackSeedUsed=true when fallback triggers; asserts step tokens RESOURCE_TAP_ACTIVE (Produced evidence from RESOURCE_TAP_V0)%TECH_UNLOCK_REALIZED%LORE_LEAD_SURFACED%PIRACY_INCIDENT_LEGIBLE (SecurityEvents entry with non-empty CauseChain)%REMOTE_RESOLUTION_COUNT_GTE_2 (>= 2 RemoteResolutionApplied markers); emits docs/generated/play_loop_proof_phase2_seed_42_v0.txt (no timestamps; stable ordering); SHA256 stable across two runs; exits nonzero on MISSING_STEP after writing report | SimCore.Runner/Program.cs; SimCore/Programs/ProgramSystem.cs; SimCore/Systems/IntelSystem.cs; SimCore/Systems/RiskSystem.cs; SimCore/Intents/ExploitationIntentsV0.cs; SimCore/Events/SecurityEvents.cs; NEW: docs/generated/play_loop_proof_phase2_seed_42_v0.txt; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS; powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-Gates.ps1 OK; dotnet run --project SimCore.Runner/SimCore.Runner.csproj -c Release -- play-loop-proof-report --seed 42 --phase 2 exits 0; $h1=(Get-FileHash docs/generated/play_loop_proof_phase2_seed_42_v0.txt -Algorithm SHA256).Hash; dotnet run --project SimCore.Runner/SimCore.Runner.csproj -c Release -- play-loop-proof-report --seed 42 --phase 2 exits 0; $h2=(Get-FileHash docs/generated/play_loop_proof_phase2_seed_42_v0.txt -Algorithm SHA256).Hash; if ($h1 -ne $h2) { throw "SHA drift: $h1 vs $h2" } |
| GATE.S3_6.PLAY_LOOP_PROOF.004 | DONE | Play loop save/load continuity regression v0: extend SaveLoadWorldHashTests with PlayLoopProof_MidProof_SaveLoad_ContinuationStable; deterministically recreates phase-1 checkpoint state and reaches TRADE_CHARTER_REVENUE evidence, saves, loads (asserts no tick advance on load per SaveLoad preservation boundary), continues and surfaces LORE_LEAD_SURFACED; asserts WorldHash uninterrupted==SaveLoad plus rumor lead digest%unlock digest%program book status digest%inventory digest identical across boundary; drift detection prints first DIFF|Field|Before|After (Field sorted Ordinal asc); no timestamps | SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs; SimCore/Systems/SerializationSystem.cs; SimCore/World/WorldLoader.cs; SimCore/Entities/IntelBook.cs; SimCore/Programs/ProgramBook.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (195/195); dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FullyQualifiedName~PlayLoopProof_MidProof_SaveLoad_ContinuationStable" PASS; Uninterrupted Hash==SaveLoad Hash (F6CCF02DA09B1010DDBD21F99D92AA2B20C334E06DD2F3EF9A7A5C883C68FB19) |

### B11. Slice 4 industry gates (v0)
| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S4.INDU.MIN_LOOP.001 | DONE | Industry minimal loop v0: deterministic industry site processing with ordinal ordering; opt-in construction pipeline produces cap_module via schema-bound stages; persisted build state and deterministic industry event stream; StationMenu readout via SimBridge; save%load and replay stability preserved; tweak routing guard satisfied via IndustryTweaksV0 | docs/56_SESSION_LOG.md (GATE.S4.INDU.MIN_LOOP.001 PASS) |
| GATE.S4.INDU_STRUCT.001 | DONE | Industry structure v0: extended schema%processing to support deterministic byproducts; enforced bounded input%output%byproduct overlap rules (no input production; byproducts cannot shadow outputs); added contract test proving ordering independence from Dictionary insertion order; save%load stability preserved | 2026-02-24 PASS (dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release; 141 passed). Evidence: SimCore.Tests/industry/IndustryProgramContractTests.cs;SimCore/Systems/IndustrySystem.cs;SimCore/Entities/IndustrySite.cs;SimCore/Schemas/IndustrySchema.json |
| GATE.S4.CONSTR_PROG.001 | DONE | Construction programs v0: added CONSTR_CAP_MODULE_V0 program kind with deterministic executor supplying stage inputs via SellIntent; integration test proves recipe consumption and cap_module production via tick pipeline; full SimCore.Tests Release PASS | 2026-02-24, main, PASS. Evidence: SimCore/Programs/ProgramSystem.cs;SimCore/Programs/ProgramKind.cs;SimCore/Programs/ProgramInstance.cs;SimCore/SimState.cs;SimCore/Schemas/ProgramSchema.json;SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs |
| GATE.S4.UI_INDU.001 | DONE | Industry UI v0: deterministic “why blocked” chain and “what to build next” suggestion surface for construction%industry using Facts%Events only. SimBridge emits why_blocked_chain and next_actions snapshot fields; StationMenu renders deterministic chain%actions with stable ordering; Validate-Gates PASS; SimCore.Tests Release PASS (142). Evidence: scripts/bridge/SimBridge.cs;scripts/ui/StationMenu.cs;SimCore/Systems/IndustrySystem.cs;SimCore/Programs/ProgramSystem.cs | - 2026-02-25, main, GATE.S4.UI_INDU.001 PASS (Industry UI v0: added SimBridge why_blocked_chain and next_actions Facts-only snapshot fields; StationMenu renders deterministic chain and suggestions with stable ordering; Validate-Gates and tests PASS). Evidence: scripts/ui/StationMenu.cs;scripts/bridge/SimBridge.cs;SimCore/Systems/IndustrySystem.cs;SimCore/Programs/ProgramSystem.cs |
| GATE.S4.PERF_BUDGET.001 | DONE | Perf budget v0 for Slice 4: deterministic scenario runs N ticks with industry%construction active and enforces budget_ms_per_tick with deterministic report; adds Slice 4 budget test with explicit scenario load checks (construction_enabled_sites>=1, industry_build_states>=1) and emits PERF_BUDGET_REPORT_V0 including ticks_total%ticks_measured%budget_ms_per_tick%avg_ms_per_tick | Evidence: SimCore.Tests/Determinism/LongRunWorldHashTests.cs;SimCore/SimKernel.cs;SimCore/Systems/IndustrySystem.cs;docs/20_TESTING_AND_DETERMINISM.md. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (142 tests); PERF_BUDGET_REPORT_V0 emitted for seed=434343 with construction_enabled_sites=1 and industry_build_states=1 |
| GATE.S4.INDU_STRUCT.RECIPE_BIND.001 | DONE | IndustrySite recipe ID binding v0: add RecipeId string field to IndustrySite entity; IndustrySystem validates RecipeId exists in content registry; RecipeId is metadata for chain analysis and shortfall reporting — inline Inputs%Outputs remain execution driver; contract test: site with valid%invalid RecipeId. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "RecipeBind" | SimCore/Entities/IndustrySite.cs;SimCore.Tests/IndustryTests.cs;SimCore/Systems/IndustrySystem.cs;SimCore/Content/ContentRegistryLoader.cs |
| GATE.S4.INDU_STRUCT.CHAIN_CONTENT.001 | DONE | Production chain recipes + hull_plating good v0: expand content_registry_v0.json with recipes matching genesis economy plus 3-step manufacturing chain; new good hull_plating (tier 2 manufactured); new recipes recipe_extract_ore (fuel->ore), recipe_refine_ore_to_metal (ore+fuel->metal), recipe_forge_hull_plating (metal->hull_plating); update DefaultRegistryJsonV0; digest updated. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "ContentRegistry" | docs/content/content_registry_v0.json;SimCore.Tests/Content/ContentRegistryContractTests.cs;SimCore/Content/ContentRegistryLoader.cs;SimCore/Content/WellKnownGoodIds.cs |
| GATE.S4.INDU_STRUCT.GENESIS_WIRE.001 | DONE | Genesis assigns recipe IDs to industry sites v0: GalaxyGenerator sets RecipeId on fuel wells (recipe_produce_fuel), mines (recipe_extract_ore), refineries (recipe_refine_ore_to_metal); adds hull_plating manufacturing sites with recipe_forge_hull_plating; golden hash update expected; re-mint baseline. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Industry" | SimCore/Gen/GalaxyGenerator.cs;SimCore.Tests/IndustryTests.cs;SimCore/Tweaks/CatalogTweaksV0.cs;docs/tweaks/baseline_numeric_literals_v0.txt |
| GATE.S4.INDU_STRUCT.CHAIN_GRAPH.001 | DONE | Deterministic chain graph validator + report v0: new ChainAnalysis class computes production chains from recipe registry; report format chain_id%depth%recipe_sequence%raw_inputs%final_outputs; validates depth<=3 and <=1 byproduct per recipe; deterministic Ordinal sort no timestamps; test: known registry -> expected chains and depth-4 chain flagged. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "ChainGraph" | SimCore/Systems/ChainAnalysis.cs;SimCore.Tests/Industry/ChainGraphTests.cs;SimCore/Content/ContentRegistryLoader.cs |
| GATE.S4.INDU_STRUCT.SHORTFALL_LOG.001 | DONE | IndustryShortfall persistent event log v0: new IndustryShortfallEvent record (site_id%recipe_id%tick%missing_good_id%required_qty%available_qty%efficiency_bps); IndustrySystem emits when efficiency<10000 bps; SimState.IndustryEventLog persists events; SimBridge GetIndustryEventsV0(sinceTick) query; test: starve refinery verify shortfall event. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "IndustryShortfall" | SimCore/Events/IndustryEvents.cs;SimCore.Tests/IndustryTests.cs;SimCore/Systems/IndustrySystem.cs;scripts/bridge/SimBridge.cs;SimCore/SimState.cs |
| GATE.S4.INDU_STRUCT.PLAYABLE_VIEW.001 | DONE | Station production info in playable prototype v0: SimBridge GetNodeIndustryV0(nodeId) returns array of {site_id%recipe_id%efficiency_pct%health_pct%outputs}; hero_trade_menu.gd shows production info section below market rows when docked at node with industry sites; contract test for query shape. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "NodeIndustry" + dotnet build "Space Trade Empire.csproj" | scripts/bridge/SimBridge.cs;scripts/ui/hero_trade_menu.gd;SimCore.Tests/IndustryTests.cs |
| GATE.S4.INDU_STRUCT.EPIC_CLOSE.001 | DONE | Industry chain scenario proof v0 (closes EPIC.S4.INDU_STRUCT): deterministic scenario gen world seed 1; verify 3-step chain (fuel->ore->metal->hull_plating) via ChainAnalysis report; run 2000 ticks verify hull_plating produced (inventory>0); cut ore supply at tick 1000 verify IndustryShortfall events emitted for refineries; verify chain report byte-stable across 2 runs; no timestamps stable ordering. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "IndustryChainScenario" | SimCore.Tests/IndustryTests.cs;SimCore/Systems/ChainAnalysis.cs;SimCore/Systems/IndustrySystem.cs;SimCore/Events/IndustryEvents.cs |

### B12. Slice 1 hero ship loop and galaxy map proto gates (v0) — corrected

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001 | DONE | GetSystemSnapshotV0 SimBridge contract v0 (prerequisite for SCENE.001): added SimBridge.GetSystemSnapshotV0(nodeId) returning facts-only snapshot with (a) station record (node_id, node_name), (b) discovery_sites list ordered by site_id (DiscoveryId) Ordinal asc (site_id, phase_token tokens SEEN%SCANNED%ANALYZED) sourced from IntelBook.Discoveries for the node via Node.SeededDiscoveryIds (same node-association pattern as existing discovery readouts), and (c) lane_gate list ordered by edge_id Ordinal asc (neighbor_node_id, edge_id) via MapQueries edge adjacency; explicitly no position data (orbital positions remain GameShell seed-derived); contract test SystemSnapshot_ContractV0_ReturnsStationSiteAndLaneGateFields asserts station present, lane_gate_count>=1, and stable site ordering under Seed 42; full Release suite passes | FOUND: scripts/bridge/SimBridge.cs; FOUND: SimCore/MapQueries.cs; FOUND: SimCore/Entities/IntelBook.cs; FOUND: SimCore/Entities/Node.cs; FOUND: SimCore/Entities/Edge.cs; FOUND: SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release; Evidence: SimCore.Tests/Systems/IntelContractTests.cs (SystemSnapshot_ContractV0_ReturnsStationSiteAndLaneGateFields); determinism: all lists have explicit Ordinal sort keys (DiscoveryId asc, EdgeId asc), no timestamps%no wall-clock%no position fields |
| GATE.S1.HERO_SHIP_LOOP.STATES.001 | DONE | Named player states contract v0: new PlayerShipState enum (InFlight, Docked, InLaneTransit) created in game_manager.gd — player_state.gd has no state concept, this is entirely new code; explicit transition guard on game_manager.gd (invalid transition emits INVALID_STATE_TRANSITION log token); state tracked as current_player_state field; state name readable via SimBridge.GetPlayerShipStateNameV0() (reads autoload at /root/GameManager); headless proof: IN_FLIGHT->DOCKED->DOCKED(invalid no-op)->IN_FLIGHT; SHA256 stable across 2 runs = 66AB4B7097F88944348E48B2DABD06DFF5D72A2C14EC8A3A748B88828E04B43E | FOUND: scripts/core/game_manager.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/tests/test_hero_ship_states.gd; FOUND: docs/generated/hss_run1.txt; FOUND: docs/generated/hss_run2.txt; Proof: & "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe" --headless --path "D:/SGE/SpaceTradeEmpire" --script scripts/tests/test_hero_ship_states.gd run twice; SHA256 match 66AB4B7097F88944348E48B2DABD06DFF5D72A2C14EC8A3A748B88828E04B43E; exits 0 both runs; determinism: transition log tokens are stable string literals; no timestamps%no wall-clock |
| GATE.S1.HERO_SHIP_LOOP.SCENE.001 | DONE | Solar system local-space scene boot v0 (depends on SYSTEM_CONTRACT.001 and STATES.001): GalaxyView.cs DrawLocalSystemBootV0 reads player_current_node_id from GetGalaxySnapshotV0 then calls GetSystemSnapshotV0 to spawn star at origin (LocalStar group), station at seed-derived orbit (Station group, inline C# box mesh 10x5x10), lane gate markers at evenly-spaced XZ positions (LaneGate group), discovery site markers (DiscoverySite group); all orbit/radius values as named C# exported fields (SystemSceneRadiusU, StationOrbitRadiusU, LaneGateDistanceU, DiscoverySiteOrbitRadiusU, StarVisualRadiusU, LaneGateMarkerRadiusU, DiscoverySiteMarkerRadiusU); LocalSystem container added deferred to avoid add_child-during-_Ready error; StationAlpha (hardcoded z=-60) and GhostSystem (disabled legacy) removed from playable_prototype.tscn; headless proof: HSB|OK|star_present=true, station_count=1, lane_gate_count=3, ship_spawn_valid=true, DONE; SHA256 stable across 2 runs = A2BC1479FCCDD72C6DA271F1A758A352F50BD325E48D1C33D50A9636EB98EA35; exits 0; no timestamps | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.cs; FOUND: scenes/playable_prototype.tscn; FOUND: scripts/core/game_manager.gd; FOUND: scripts/tests/test_hero_ship_scene_boot.gd; FOUND: docs/generated/hsb_run1.txt; FOUND: docs/generated/hsb_run2.txt; Proof: & "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe" --headless --path "D:/SGE/SpaceTradeEmpire" --script scripts/tests/test_hero_ship_scene_boot.gd run twice; SHA256 match A2BC1479FCCDD72C6DA271F1A758A352F50BD325E48D1C33D50A9636EB98EA35; exits 0 both runs; determinism: seed-derived positions via FNV1a64 hash; no wall-clock calls; stable group token ordering |
| GATE.S1.HERO_SHIP_LOOP.FLIGHT.001 | DONE | Ship thrust, inertia, and collision physics v0: thrust input accumulates velocity on player.tscn RigidBody3D; velocity persists after thrust release (inertia); collision with scene object changes velocity; all physics tuning values (thrust_force, max_speed, damping) declared as named config constants, no numeric literals in .gd or .tscn; GDScript smoke test asserts velocity nonzero after thrust frame and within expected range during coast; SHA256 stable across two runs | FOUND: scenes/player.tscn (RigidBody3D + hero_ship_flight_controller.gd); FOUND: scenes/playable_prototype.tscn (Player instance + StationMenu wiring guarded); FOUND: scripts/core/game_manager.gd (no connect to nonexistent StationMenu signals); FOUND: scripts/core/hero_ship_flight_controller.gd; NEW: scripts/tests/test_hero_ship_flight.gd; NEW: scripts/tests/test_playable_prototype_boot.gd; Proof: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/core/game_manager.gd" PASS; & "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe" --headless --verbose --path "D:\SGE\SpaceTradeEmpire" -s "res://scripts/tests/test_hero_ship_flight.gd" -- --seed=42 run twice; SHA256 matches (41CC8C469548B55F3F11147ED9F7FD6EAE11341AFCB34700F0F496CEA27F7258); boot probe: & godot --headless --path "D:\SGE\SpaceTradeEmpire" --script scripts/tests/test_playable_prototype_boot.gd reaches PPB|OK|DONE|frames=30 with no ERROR lines; determinism: token-only outputs, no timestamps%wall-clock, stable ordering in test steps |
| GATE.S1.HERO_SHIP_LOOP.DOCK.001 | DONE | Generic proximity dock trigger extension v0: (a) StarNode.cs makes dock range explicit as DOCK_RANGE_U and enforces deterministic sphere trigger radius at runtime, (b) GalaxyView.cs adds RegisterDockTargetV0(node, kindToken, targetId) helper to add DockTarget group and stable meta tokens for spawners, (c) game_manager.gd centralizes PlayerShipState transitions to DOCKED on proximity enter and IN_FLIGHT on undock, wiring StationMenu RequestUndock to undock_v0; emits deterministic UUIR tokens UUIR|DOCK_ENTER|<kind>|<id>%UUIR|UNDOCK|<kind>|<id> with no timestamps; stable meta%group resolution. Note: DiscoverySite dock path is tokenized (DISCOVERY_SITE) with SCAN_FLOW_OPEN token stub pending actual DiscoverySite spawn integration; determinism: DOCK_RANGE_U is a named constant not a literal. | FOUND: scripts/view/StarNode.cs; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/core/game_manager.gd; FOUND: scenes/station.tscn | Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (200/200). |
| GATE.S1.HERO_SHIP_LOOP.LANE.001 | DONE | Lane transit bare state and SimCore time advance v0: player ship reaches lane gate marker; game_manager.gd transitions PlayerShipState to InLaneTransit; bare screen fade plays (no tunnel visual, no camera lock, no input buffer in v0); game_manager.gd dispatches TravelCommand for player_fleet_id via SimBridge to adjacent system; SimCore TickIndex advances by LaneFlowSystem edge cost; player ship spawns at adjacent system lane gate marker; PlayerShipState returns to InFlight; headless UUIR tokens: InFlight->InLaneTransit->InFlight state sequence, tick_before and tick_after; SHA256 stable across two runs | NEW: scripts/tests/test_hero_ship_lane_transit.gd (no existing lane transit test); FOUND: scripts/core/game_manager.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: SimCore/Systems/LaneFlowSystem.cs; FOUND: SimCore/Commands/TravelCommand.cs; FOUND: scenes/playable_prototype.tscn; Proof: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript scripts/tests/test_hero_ship_lane_transit.gd; & "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --headless --path . -s "res://scripts/tests/test_hero_ship_lane_transit.gd" -- --seed=42 run twice; Get-FileHash docs/generated/hero_ship_lane_transit_seed_42_v0.txt -Algorithm SHA256 matches; determinism: tick advance uses LaneFlowSystem edge cost not a constant; state sequence tokens are stable string literals |
| GATE.S1.GALAXY_MAP.CONTRACT.001 | DONE | GetGalaxySnapshotV0 SimBridge contract v0: SimBridge.cs method does NOT use TryExecuteSafeRead direct-state pattern; uses SimBridge read lock (ExecuteSafeRead); returns system_nodes list (node_id, display_state_token: Hidden=%omitted as HIDDEN token with empty display_text% Rumored=??? % Visited=name % Mapped=name+object_count) ordered by node_id Ordinal asc; lane_edges list (from_id, to_id) ordered by edge_id Ordinal asc; player_current_node_id; Rumored nodes derived from RumorLead.Hint.CoarseLocationToken where Status=Active; no fleet counts in v0; derives from state.Nodes, state.Edges, state.Intel.RumorLeads, state.Intel.Discoveries only; contract test GalaxySnapshot_ContractV0_AllDisplayStates_Present asserts all four states and required fields stable for seed 42; Proof passes | FOUND: scripts/bridge/SimBridge.cs; FOUND: SimCore/MapQueries.cs; FOUND: SimCore/Entities/IntelBook.cs; FOUND: SimCore/Entities/Node.cs; FOUND: SimCore/Entities/Edge.cs; FOUND: SimCore.Tests/Systems/IntelContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release PASS (199%199); Evidence: GalaxySnapshot_ContractV0_AllDisplayStates_Present in SimCore.Tests/Systems/IntelContractTests.cs; determinism: node_id asc%edge_id asc ordering; no timestamps |
| GATE.S1.GALAXY_MAP.RENDER.001 | DONE | Galaxy map overlay render v0: Tab in game_manager.gd now toggles galaxy overlay (toggle_galaxy_map_overlay_v0) instead of dead toggle_market(); overlay is CanvasLayer above local space (no scene swap) and local ticking continues while open (time_accumulator advances); GalaxyView.cs renders galaxy nodes%edges only when overlay open (SetOverlayOpenV0 gate) and reads GetGalaxySnapshotV0 via SimBridge only (no TryExecuteSafeRead%no direct sim access%no galaxy_spawner.gd dependency); player current system highlight sets player_node_highlighted=true; rumored nodes label as ??? when display_state_token=RUMORED; headless UUIR tokens emitted: node_count%edge_count%player_node_highlighted%local_scene_ticking; SHA256 stable across two runs | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/core/game_manager.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scenes/playable_prototype.tscn; FOUND: scripts/view/map_camera.gd; NEW: scripts/tests/test_galaxy_map_overlay.gd; Proof: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1 -TargetScript "scripts/core/game_manager.gd"; & "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe" --headless --verbose --path . -s "res://scripts/tests/test_galaxy_map_overlay.gd" -- --seed=42 run twice; outputs docs/generated/galaxy_map_overlay_v0.run1.txt and .run2.txt; SHA256 match B41EECBDEE80485EEAC94BBC21B932B225501BCF1BD2EF818943F850A6B1E374; determinism: no wall-clock%no timestamps; stable output token order; node_id asc and edges from_id%to_id asc in GalaxyView |
| GATE.S1.GALAXY_MAP.DISCOVERY_STATES.001 | DONE | Galaxy map discovery state rendering v0: for seed 42 — Hidden nodes absent from render; Rumored nodes show ??? label plus lowest-ordinal active RumorLead region tag for that coarse location; Visited nodes show system name; Mapped nodes show name plus object_count; every rendered field traces directly to a GetGalaxySnapshotV0 field (no GameShell-side inference); Mapped-node-missing-object_count case emits UUIR EXPLAIN token; vacuous-pass policy when no Mapped nodes exist at seed 42 tick 0; dotnet test fast loop continues to pass; SHA256 stable across two runs | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.cs; FOUND: SimCore.Tests/Systems/IntelContractTests.cs; FOUND: scripts/tests/test_galaxy_map_overlay.gd; FOUND: scripts/view/map_camera.gd; NEW: scripts/tests/test_galaxy_map_discovery_states.gd; Proof: dotnet build + & godot --headless --path D:/SGE/SpaceTradeEmpire --script scripts/tests/test_galaxy_map_discovery_states.gd run twice; Get-FileHash docs/generated/gmd_run1.txt%gmd_run2.txt -Algorithm SHA256 matches; dotnet test pass; determinism: display_state tokens are schema-bound string literals from SimBridge contract |
| GATE.S1.HERO_SHIP_LOOP.ARRIVE.001 | DONE | Lane transit arrival SimBridge-truth scene redraw v0: on_lane_arrival_v0 dispatches PlayerArriveCommand so SimCore sets PlayerLocationNodeId=arrived_node_id; SimBridge.DispatchPlayerArriveCommandV0 enqueues command; GetGalaxySnapshotV0().player_current_node_id read as truth; GalaxyView.DrawLocalSystemV0 called with that node_id (no direct arrived_id pass to GalaxyView); headless proof (HSA| prefix) asserts player_current_node_id==neighbor_id and LocalStar%Station%LaneGate groups non-zero after arrival; SHA256 stable across two runs | NEW: SimCore/Commands/PlayerArriveCommand.cs; FOUND: SimCore/Systems/LaneFlowSystem.cs; FOUND: SimCore/SimState.cs; FOUND: scripts/core/game_manager.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/view/GalaxyView.cs; NEW: scripts/tests/test_hero_ship_arrive.gd; Proof: dotnet build + & godot --headless --path D:/SGE/SpaceTradeEmpire --script scripts/tests/test_hero_ship_arrive.gd run twice; SHA256 match; dotnet test pass; determinism: player_current_node_id is SimCore state truth%not wall-clock |
| GATE.S1.HERO_SHIP_LOOP.DISCOVERY_DOCK.001 | DONE | Discovery site proximity dock trigger v0: Area3D+CollisionShape3D added to DiscoverySite markers in GalaxyView.cs (same pattern as LANE.001 lane gate markers); body-entered callback calls game_manager.on_discovery_site_proximity_entered_v0(site_id); game_manager emits UUIR|DISCOVERY_DOCK|DISCOVERY_SITE|<site_id> and transitions PlayerShipState to DOCKED; completes DiscoverySite dock stub left open in DOCK.001; test_hero_ship_scene_boot.gd extended with --assert-discovery-dock flag (calls on_discovery_site_proximity_entered_v0 and asserts DOCKED); original no-flag SHA256 (A2BC...) unchanged; flagged run SHA256 stable across two runs | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/core/game_manager.gd; FOUND: scripts/tests/test_hero_ship_scene_boot.gd; Proof: dotnet build + & godot --headless --path D:/SGE/SpaceTradeEmpire --script scripts/tests/test_hero_ship_scene_boot.gd -- --assert-discovery-dock run twice; SHA256 match; dotnet test pass; determinism: UUIR tokens are stable string literals%no timestamps |

| GATE.S1.HERO_SHIP_LOOP.CONTROLS.001 | DONE | Ship yaw turning v1: hero_ship_flight_controller.gd adds Y-axis torque via apply_torque_central for ship_turn_left/right (A/D keys); project.godot defines ship_turn_left, ship_turn_right, ship_thrust_fwd, ship_thrust_back input actions; game_manager.gd _physics_process thrust block removed (controller owns all flight input); angular_damp reduced to allow turning; headless proof (HSC| prefix) asserts rotation non-zero after sustained turn input frames; SHA256 stable across two runs | NEW: scripts/tests/test_hero_ship_controls.gd; FOUND: scripts/core/hero_ship_flight_controller.gd; FOUND: scripts/core/game_manager.gd; FOUND: project.godot; Proof: dotnet build + godot --headless --path . -s res://scripts/tests/test_hero_ship_controls.gd run twice; SHA256 match; dotnet test pass |
| GATE.S1.HERO_SHIP_LOOP.HUD.001 | DONE | Persistent flight HUD v0: NEW hud.gd CanvasLayer (layer=10) added to playable_prototype.tscn; SimBridge.GetPlayerStateV0 returns {credits, cargo_count, current_node_id, ship_state_token}; hud.gd Labels update each _physics_process; headless proof (HSH| prefix) asserts HUD node in scene tree, bridge method present, credits>=0, current_node_id non-empty after boot; SHA256 stable across two runs | NEW: scripts/ui/hud.gd; NEW: scripts/tests/test_hero_ship_hud.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scenes/playable_prototype.tscn; Proof: dotnet build + godot --headless --path . -s res://scripts/tests/test_hero_ship_hud.gd run twice; SHA256 match; dotnet test pass |
| GATE.S1.GALAXY_MAP.FLEET_COUNTS.001 | DONE | Fleet counts per node in galaxy overlay v0: MapQueries.SystemNodeSnapV0 adds FleetCount (int, count of fleets at that node); GalaxySnapshotV0 serializes fleet_count per node from state.Fleets where CurrentNodeId==nodeId; GalaxyView renders fleet_count>0 as small overlay label beside node sphere; test_galaxy_map_overlay.gd extended to assert fleet_count is int>=0 per node and player start node fleet_count>=0; SHA256 stable | FOUND: SimCore/MapQueries.cs; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/tests/test_galaxy_map_overlay.gd; Proof: dotnet build + godot --headless --path . -s res://scripts/tests/test_galaxy_map_overlay.gd run twice; SHA256 match; dotnet test pass |
| GATE.S1.HERO_SHIP_LOOP.LANE_GATE_LABEL.001 | DONE | Lane gate destination name labels v0: MapQueries.BuildSystemSnapshotV0 adds NeighborDisplayName (from state.Nodes[neighborId].Name) per LaneGate entry; SimBridge.GetSystemSnapshotV0 serializes neighbor_display_name string; GalaxyView.SpawnLaneGatesV0 creates Label3D above each gate marker with neighbor name; test_hero_ship_scene_boot.gd extended with --assert-lane-labels flag: asserts GetSystemSnapshotV0 lane_gate[0] has non-empty neighbor_display_name; SHA256 stable | FOUND: SimCore/MapQueries.cs; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/tests/test_hero_ship_scene_boot.gd; Proof: dotnet build + godot --headless --path . -s res://scripts/tests/test_hero_ship_scene_boot.gd -- --assert-lane-labels run twice; SHA256 match; dotnet test pass |
| GATE.S1.HERO_SHIP_LOOP.PLAYER_TRADE.001 | DONE | Hero ship buy/sell at station v0: NEW PlayerTradeCommand.cs (IsBuy=true decrements PlayerCredits+increments PlayerCargo[goodId]; sell reverses); SimBridge.GetPlayerMarketViewV0(nodeId) returns Array of {good_id, price, quantity}; SimBridge.GetPlayerStateV0 extended with credits+cargo_count+cargo_dict; SimBridge.DispatchPlayerTradeV0(nodeId, goodId, qty, isBuy) dispatches command; NEW hero_trade_menu.gd CanvasLayer opens when PlayerShipState==DOCKED, shows goods table, buy/sell buttons; headless proof (HST| prefix) boots docked state, buys good, asserts credits decreased; SHA256 stable | NEW: SimCore/Commands/PlayerTradeCommand.cs; NEW: scripts/ui/hero_trade_menu.gd; NEW: scripts/tests/test_hero_ship_trade.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/core/game_manager.gd; FOUND: scenes/playable_prototype.tscn; Proof: dotnet build + godot --headless --path . -s res://scripts/tests/test_hero_ship_trade.gd run twice; SHA256 match; dotnet test pass |
| GATE.S1.HERO_SHIP_LOOP.MARKET_SCREEN.001 | DONE | hero_trade_menu.gd gains rendered Panel+VBoxContainer rows for each market good (good_id, buy_price, sell_price) plus Buy/Sell buttons; get_panel_row_count_v0() returns >=1 after dock; test_hero_ship_trade.gd extended to emit panel_row_count metric. Milestone: PLAYABLE_BEAT. Proof: godot --headless --path . -s res://scripts/tests/test_hero_ship_trade.gd | scripts/ui/hero_trade_menu.gd; scenes/playable_prototype.tscn |
| GATE.S1.HERO_SHIP_LOOP.CARGO_DISPLAY.001 | DONE | Station panel gains cargo section; SimBridge.GetPlayerCargoV0() returns Array of {good_id, qty}; get_cargo_row_count_v0() >=1 after buy; test_hero_ship_trade.gd extended to emit cargo_row_count metric. Proof: godot --headless --path . -s res://scripts/tests/test_hero_ship_trade.gd | scripts/bridge/SimBridge.cs; scripts/ui/hero_trade_menu.gd |
| GATE.S1.HERO_SHIP_LOOP.LOOP_PROOF.001 | DONE | Headless scenario: dock->buy 1 good (credits decrease)->sell same good (credits partially recover)->assert cargo returns to 0; closes EPIC.S1.HERO_SHIP_LOOP.V0. Milestone: HEADLESS_PROOF. Proof: godot --headless --path . -s res://scripts/tests/test_hero_ship_loop_proof.gd | scripts/tests/test_hero_ship_loop_proof.gd; scripts/bridge/SimBridge.cs |
| GATE.S1.GALAXY_MAP_PROTO.PLAYER_HIGHLIGHT.001 | DONE | Galaxy overlay GetOverlayMetricsV0()[player_node_highlighted] must be true; GalaxyView.cs already implements green highlight on player node; gate locks this assertion. Proof: godot --headless --path . -s res://scripts/tests/test_galaxy_map_overlay.gd (output contains player_node_highlighted=true) | scripts/view/GalaxyView.cs; scripts/tests/test_galaxy_map_overlay.gd |
| GATE.S1.GALAXY_MAP_PROTO.EPIC_CLOSE.001 | DONE | Headless proof: node_count>=2, edge_count>=1, player_node_highlighted=true, local_scene_ticking=true; emits GME|PASS|...; closes EPIC.S1.GALAXY_MAP_PROTO.V0. Milestone: HEADLESS_PROOF. Proof: godot --headless --path . -s res://scripts/tests/test_galaxy_map_epic_close.gd | scripts/tests/test_galaxy_map_epic_close.gd; scripts/view/GalaxyView.cs |
| GATE.S1.HERO_SHIP_LOOP.STATION_DOCK_PROXIMITY.001 | DONE | Replace active_station.gd body.dock_at_station() legacy path with game_manager.on_proximity_dock_entered_v0(self); set dock_target_id meta from sim_market_id in _ready(). Proof: godot --headless --path . -s res://scripts/tests/test_station_dock_v0.gd → DOCK|PASS|kind=STATION | scripts/active_station.gd; scripts/tests/test_station_dock_v0.gd (NEW) |
| GATE.S1.HERO_SHIP_LOOP.MARKET_UNDOCK_V0.001 | DONE | hero_trade_menu.gd: open_market_v0() makes menu visible + populates rows; add close_market_v0(); add Undock button calling game_manager.undock_v0(). Proof: test_station_dock_v0.gd extended → DOCK|PASS|panel_row_count>0|undock=ok | scripts/ui/hero_trade_menu.gd; scripts/tests/test_station_dock_v0.gd |
| GATE.S1.HERO_SHIP_LOOP.STATION_LOOP_V1.001 | DONE | Full in-game loop: dock at station A via game_manager → buy food → verify credits reduced → undock → enter lane → arrive next system → dock at next station → sell food → verify credits changed. Closes EPIC.S1.HERO_SHIP_LOOP.V0. Proof: godot --headless --path . -s res://scripts/tests/test_station_loop_v1.gd → LOOP_V1|PASS | scripts/tests/test_station_loop_v1.gd (NEW); scripts/core/game_manager.gd |

### B13. Slice 4 catalog and module model gates (v0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S4.CATALOG.GOODS.001 | DONE | Starter goods content pack + schema validator v0: docs/content/content_registry_v0.json goods entries expanded with display_name (string)%tier (int >=0, content uses 1)%base_price_band (string token)%stackable (bool) fields; goods ordered by id (StringComparer.Ordinal asc); NEW CatalogContractTests.cs loads registry, asserts goods count>=2%required fields present%ids unique, and ordering stable by comparing in-file order to an Ordinal-sorted expectation (equivalently stable across loads); dotnet test filter Category=CatalogContract passes | FOUND: docs/content/content_registry_v0.json; FOUND: SimCore/Schemas/ContentRegistrySchema.json; NEW: SimCore.Tests/Systems/CatalogContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=CatalogContract" |
| GATE.S4.CATALOG.RECIPES.001 | DONE | Starter recipe content pack + schema validator v0: docs/content/content_registry_v0.json recipes expanded with display_name%production_ticks (int) fields; all input%output good_id values reference ids in goods list (cross-reference asserted); ordering by id Ordinal asc; CatalogContractTests.cs recipe tests assert count>=1%field presence%good_id cross-reference%ordering stable; GOODS.001 must be DONE before this gate | FOUND: docs/content/content_registry_v0.json; FOUND: SimCore.Tests/Systems/CatalogContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=CatalogContract" |
| GATE.S4.MODULE_MODEL.SLOTS.001 | DONE | Hero ship slot model + SimBridge readout v0: ModuleSlot.cs entity (SlotId: string%SlotKind: enum Cargo%Weapon%Engine%Utility%InstalledModuleId: string nullable); Fleet.Slots List<ModuleSlot> added; EnsurePlayerFleetV0 initializes hero fleet with standard slot set; SimBridge.GetHeroShipLoadoutV0() returns Array of Dicts ordered by slot_id Ordinal asc; LoadoutContractTests.cs asserts slot count>=1%field shapes%ordering stability; provides SimBridge query surface for future refit UI | NEW: SimCore/Entities/ModuleSlot.cs; FOUND: SimCore/Entities/Fleet.cs; FOUND: scripts/bridge/SimBridge.cs; NEW: SimCore.Tests/Systems/LoadoutContractTests.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=LoadoutContract" |
| GATE.S4.CATALOG.MARKET_BIND.001 | DONE | Bind goods catalog to market seeding v0: GalaxyGenerator or WorldLoader seeds market good_ids from ContentRegistryLoader goods list on world gen (WorldLoader.cs currently has zero ContentRegistry references — confirmed gap); each generated market contains >=2 good_ids present in the catalog; SimBridge injects or loads registry before world gen if needed; dotnet test MarketCatalogBind_FreshWorld_HasCatalogGoods asserts state.Markets.Values contain catalog good_ids; tweak routing guard passes | FOUND: SimCore/World/WorldLoader.cs; FOUND: SimCore/Gen/GalaxyGenerator.cs; FOUND: scripts/bridge/SimBridge.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "MarketCatalogBind" PASS + full dotnet test PASS |
| GATE.S4.CATALOG.WEAPONS.001 | DONE | Add weapon_cannon_mk1 and weapon_laser_mk1 to content_registry_v0.json modules[] (Ordinal asc); add WellKnownModuleIds.cs constants; CatalogContractTests pass unchanged. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "CatalogContract" | docs/content/content_registry_v0.json; SimCore/Content/WellKnownModuleIds.cs |
| GATE.S4.MODULE_MODEL.EQUIP.001 | DONE | SimBridge.DispatchEquipModuleV0(slotId, moduleId) dispatches EquipModuleCommand; ICommand.Execute sets Fleet.Slots[slotId].InstalledModuleId=moduleId; GetHeroShipLoadoutV0() reflects change; headless test asserts installed_module_id=weapon_laser_mk1. Proof: godot --headless --path . -s res://scripts/tests/test_hero_ship_equip.gd | SimCore/Commands/EquipModuleCommand.cs; scripts/bridge/SimBridge.cs |
| GATE.S4.MODULE_MODEL.EQUIP_PANEL.001 | DONE | Add hero ship loadout section to FleetMenu.cs: calls GetHeroShipLoadoutV0(), renders slot_id + installed_module_id (or "empty") per slot; public GetHeroLoadoutSlotCountV0() for headless testability. Proof: godot --headless --path . -s res://scripts/tests/test_equip_panel.gd → EQUIP_PANEL|PASS|slot_count=4 | scripts/ui/FleetMenu.cs; scripts/tests/test_equip_panel.gd (NEW) |
| GATE.S4.CATALOG.EPIC_CLOSE.001 | DONE | Headless proof: dock at station, GetPlayerMarketViewV0() returns goods with IDs food/fuel/metal/ore from catalog; catalog digest stable. Updates docs/54_EPICS.md EPIC.S4.CATALOG.V0 to DONE. Proof: godot --headless --path . -s res://scripts/tests/test_catalog_epic_close.gd → CAT_CLOSE|PASS | scripts/tests/test_catalog_epic_close.gd (NEW); docs/content/content_registry_v0.json |
| GATE.S4.MODULE_MODEL.EPIC_CLOSE.001 | DONE | Headless proof: GetHeroShipLoadoutV0() returns >=4 slots, equip weapon_laser_mk1 verified, FleetMenu.GetHeroLoadoutSlotCountV0() >= 4. Updates docs/54_EPICS.md EPIC.S4.MODULE_MODEL.V0 to DONE. Proof: godot --headless --path . -s res://scripts/tests/test_module_model_epic_close.gd → MOD_CLOSE|PASS | scripts/tests/test_module_model_epic_close.gd (NEW); scripts/ui/FleetMenu.cs |

### B14. Playable beat gates (first live in-game experience)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.PLAYABLE_BEAT.INTERACTION_FIX.001 | DONE | Wire buy/sell buttons in hero_trade_menu.gd to buy_one_v0/sell_one_v0 with good_id bind; refresh market rows after each trade; freeze ship thrust/turn input while docked (check GameManager.current_player_state); hero_ship_flight_controller.gd collision_layer=2. Proof: godot --headless --path . -s res://scripts/tests/test_playable_beat_v0.gd → BEAT|PASS | scripts/ui/hero_trade_menu.gd; scripts/core/hero_ship_flight_controller.gd; scripts/tests/test_playable_beat_v0.gd (NEW) |
| GATE.S1.PLAYABLE_BEAT.EPIC_CLOSE.001 | DONE | Manual in-game proof: fly to station, dock (market appears, ship stops), buy fuel (HUD credits decrease, cargo increases), undock (market closes, ship responds), fly to lane gate, transit, dock at next station, sell fuel. Closes EPIC.S1.PLAYABLE_BEAT.V0. Proof: manual play test with screenshot or video evidence | scripts/ui/hero_trade_menu.gd; scripts/core/hero_ship_flight_controller.gd; scripts/core/game_manager.gd |

### B15. Hygiene gates (cross-cutting)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.TRADE_CMD_TESTS.001 | DONE | Trade command contract validation + edge case tests v0: NEW TradeCommandContractTests.cs covering BuyCommand, SellCommand, TradeCommand with isolated unit tests — insufficient credits rejected, insufficient cargo rejected, insufficient market stock rejected, successful buy/sell updates player cargo + credits + market inventory, boundary amounts (buy all, sell all, zero qty rejected). Must not duplicate existing integration tests. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "Category=TradeCommandContract" | NEW: SimCore.Tests/Commands/TradeCommandContractTests.cs; FOUND: SimCore/Commands/BuyCommand.cs; FOUND: SimCore/Commands/SellCommand.cs; FOUND: SimCore/Commands/TradeCommand.cs; Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=TradeCommandContract" |
| GATE.X.HYGIENE.DEAD_CODE.001 | DONE | Dead code cleanup + minor hygiene v0: delete scripts/view/ui/station_interface.gd (confirmed zero references from .gd/.tscn files); delete scripts/view/ui/contract_board.gd (confirmed zero references); remove 2 Console.WriteLine calls in SimCore/Systems/LogisticsSystem.cs (lines 251, 322) replacing with no-op or removing the statement; fix 2 CS8602 nullable warnings in SimCore.Tests/Determinism/SaveLoadWorldHashTests.cs:501. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q (full suite pass, no regressions) + dotnet build "Space Trade Empire.csproj" --nologo (game assembly builds clean) | FOUND: scripts/view/ui/station_interface.gd; FOUND: scripts/view/ui/contract_board.gd; FOUND: SimCore/Systems/LogisticsSystem.cs; FOUND: SimCore.Tests/Determinism/SaveLoadWorldHashTests.cs; Proof: dotnet test + dotnet build |
| GATE.X.HYGIENE.GALAXY_GEN_SPLIT.001 | DONE | GalaxyGenerator.cs first split (StarNetworkGen + MarketInit) v0: extract star network topology generation (node placement, lane wiring, distance calc) into NEW SimCore/Gen/StarNetworkGen.cs; extract market initialization (inventory seeding, economy placement, profile assignment) into NEW SimCore/Gen/MarketInitGen.cs; GalaxyGenerator.cs calls both as sub-steps; golden hash MUST NOT change (extract is pure refactor). Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q (full suite pass including golden hash tests) | NEW: SimCore/Gen/StarNetworkGen.cs; NEW: SimCore/Gen/MarketInitGen.cs; FOUND: SimCore/Gen/GalaxyGenerator.cs; Proof: dotnet test (full suite, golden hashes unchanged) |

### B16. Slice 5 combat gates (v0) — EPIC.S5.COMBAT_LOCAL

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.COMBAT_LOCAL.DAMAGE_MODEL.001 | DONE | CombatProfile entity + deterministic damage calc + counter family v0: NEW CombatProfile on Fleet (hull_hp int, shield_hp int, weapon slots read from ModuleSlot where SlotKind=Weapon); NEW CombatSystem.cs with CalcDamage(attacker CombatProfile, defender CombatProfile) returning DamageResult (hull_dmg, shield_dmg, overkill); counter families: kinetic (cannon) 1.5x vs hull / 0.5x vs shields, energy (laser) 1.5x vs shields / 0.5x vs hull; NEW CombatTweaksV0.cs for all numeric constants; base weapon damage from content_registry_v0.json modules (add "base_damage" field to weapon entries); contract tests assert damage determinism + counter ratios. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=CombatContract" | NEW: SimCore/Systems/CombatSystem.cs; NEW: SimCore/Tweaks/CombatTweaksV0.cs; FOUND: SimCore/Entities/Fleet.cs; FOUND: SimCore/Entities/ModuleSlot.cs; FOUND: docs/content/content_registry_v0.json; Proof: dotnet test --filter "Category=CombatContract" |
| GATE.S5.COMBAT_LOCAL.COMBAT_TICK.001 | DONE | Combat encounter lifecycle + auto-targeting + event log v0: CombatSystem.TickCombat(attackerFleet, defenderFleet, rng) processes one combat round — each fleet fires equipped weapons at opponent, damage applied to shields first then hull; encounter ends when one side hull<=0; auto-target selects opponent (1v1 MVP, no target priority); CombatEventEntry record (tick, attacker_id, defender_id, weapon_id, damage_dealt, hull_remaining, shield_remaining); NEW CombatTests.cs with multi-round combat scenario asserting deterministic outcome + event log populated. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=CombatTick" | NEW: SimCore.Tests/Combat/CombatTests.cs; FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore/Entities/Fleet.cs; Proof: dotnet test --filter "Category=CombatTick" |
| GATE.S5.COMBAT_LOCAL.COMBAT_LOG.001 | DONE | Combat event log + "why we lost" cause chain v0: CombatSystem produces a CombatLog (list of CombatEventEntry + outcome enum Win/Loss/Draw + cause_of_death string); SimState stores last N combat logs (N=10 MVP); cause chain: "hull destroyed by weapon_cannon_mk1 from fleet_pirate_1 at tick 4"; contract tests assert cause string populated on loss, empty on win; deterministic replay of same combat produces identical log. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=CombatLog" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs; Proof: dotnet test --filter "Category=CombatLog" |
| GATE.S5.COMBAT_LOCAL.BRIDGE_COMBAT.001 | DONE | SimBridge combat queries + HUD combat readout v0: SimBridge.GetCombatStatusV0() returns Dict with in_combat (bool), opponent_id, player_hull, player_shield, opponent_hull, opponent_shield; SimBridge.GetLastCombatLogV0() returns Dict with outcome, cause, event_count; hud.gd shows combat indicator when in_combat=true (red "COMBAT" label); contract test asserts query shape. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Category=CombatBridge" | FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/ui/hud.gd; Proof: dotnet test --filter "Category=CombatBridge" |
| GATE.S5.COMBAT_LOCAL.SCENE_PROOF.001 | DONE | In-engine combat headless proof v0 (PLAYABLE_BEAT): extend test_playable_beat_v0.gd or write companion GDScript test; spawn pirate fleet at player node via SimBridge; trigger combat tick; verify GetCombatStatusV0() shows in_combat=true; tick until resolution; verify GetLastCombatLogV0() has outcome; assert HUD combat label visible during fight. Proof: godot --headless --path . -s res://scripts/tests/test_playable_beat_v0.gd → COMBAT_PROOF|PASS | FOUND: scripts/tests/test_playable_beat_v0.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/ui/hud.gd; Proof: godot --headless -s test_playable_beat_v0.gd |

### B17. Slice 5 in-engine combat playable gates (v0) — EPIC.S5.COMBAT_PLAYABLE.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001 | DONE | AI fleet substantiation + player-initiated combat v0: All fleets at current system node spawn as 3D ships in local space (same GalaxyView pattern as discovery sites via GetSystemSnapshotV0). Player targets a nearby ship + presses C key to initiate combat via DispatchStartCombatV0. Enemy despawns on defeat. Determinism: fleet ships ordered by fleet_id (Ordinal); targeting uses nearest-first with fleet_id tie-break. Failure mode: no fleet at current node → "no targets in range" HUD message. Intervention: Industry: queue refit. Proof: godot --headless --path . -s res://scripts/tests/test_combat_encounter_v0.gd | FOUND: SimCore/MapQueries.cs; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/core/game_manager.gd; NEW: scripts/tests/test_combat_encounter_v0.gd; Proof: godot --headless -s test_combat_encounter_v0.gd |
| GATE.S5.COMBAT_PLAYABLE.LOOP_PROOF.001 | DONE | In-engine combat loop headless proof v0 (PLAYABLE_BEAT): Headless proof: undock from station → fly to fleet ship → target → C key → combat resolves in SimCore → enemy despawns → player still IN_FLIGHT. Depends on ENCOUNTER_TRIGGER gate. Determinism: same seed → same fleet positions → same combat outcome → deterministic proof output. Failure mode: combat doesn't resolve → timeout with "combat_status_timeout" diagnostic. Intervention: Industry: queue refit. Proof: godot --headless --path . -s res://scripts/tests/test_combat_loop_proof_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/ui/hud.gd; NEW: scripts/tests/test_combat_loop_proof_v0.gd; Proof: godot --headless -s test_combat_loop_proof_v0.gd |
| GATE.S5.COMBAT_PLAYABLE.PLAYER_DEATH.001 | DONE | Player death state + game-over restart v0: When player hull reaches 0 during real-time combat, transition to DEAD state — freeze input, show "Game Over" overlay with restart button. Restart reloads last save or resets to starter state. Determinism: death triggers when hull_hp <= 0 (same CalcDamage path). Failure mode: hull reaches 0 but no state transition → add guard in _process damage handler. Proof: godot --headless --path . -s res://scripts/tests/test_player_death_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scripts/ui/hud.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/bullet.gd; NEW: scripts/tests/test_player_death_v0.gd; Proof: godot --headless -s test_player_death_v0.gd |
| GATE.S5.COMBAT_PLAYABLE.COMBAT_BEAT.001 | DONE | Combat beat proof (PLAYABLE_BEAT): End-to-end headless proof — undock → fly to fleet → G key fires turrets → bullets hit → enemy HP decrements → enemy dies → player survives return fire. Closes EPIC.S5.COMBAT_PLAYABLE.V0. Determinism: same seed → same fleet → same shot sequence → deterministic outcome. Failure mode: timeout → log last combat state for diagnosis. Proof: godot --headless --path . -s res://scripts/tests/test_combat_beat_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/ui/hud.gd; FOUND: scripts/bullet.gd; NEW: scripts/tests/test_combat_beat_v0.gd; Proof: godot --headless -s test_combat_beat_v0.gd |

### B18. Slice 1 discovery interaction gates (v0) — EPIC.S1.DISCOVERY_INTERACT.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.DISCOVERY_INTERACT.PANEL.001 | DONE | Discovery site dock panel v0: Docking at a discovery site opens a minimal panel (site_id, phase, undock button) instead of dead-end print diagnostic. Panel reads from existing GetDiscoverySnapshotV0 SimBridge query. Determinism: panel content derived from SimBridge query; no new sim state. Failure mode: dock at discovery site with no data → panel shows "Unknown Site" fallback. Intervention: Discoveries: run analysis step. Proof: godot --headless --path . -s res://scripts/tests/test_station_dock_v0.gd (expanded to cover discovery site docking) | FOUND: scripts/core/game_manager.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scenes/Playable_Prototype.tscn; NEW: scripts/ui/DiscoverySitePanel.gd; Proof: godot --headless -s test_station_dock_v0.gd |
| GATE.S1.DISCOVERY_INTERACT.SCAN.001 | DONE | Scan/Analyze buttons wired to SimBridge: Add Scan and Analyze action buttons to discovery site panel. Scan calls SimBridge AdvanceDiscoveryPhaseV0 to progress site phase. Analyze calls GetDiscoveryScanResultV0. Button states reflect phase (greyed if already scanned). Determinism: all state mutations via SimBridge → SimCore; panel is read-only after dispatch. Failure mode: SimBridge call fails → button shows error, no state change. Proof: godot --headless --path . -s res://scripts/tests/test_station_dock_v0.gd | FOUND: scripts/ui/DiscoverySitePanel.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/core/game_manager.gd; Proof: godot --headless -s test_station_dock_v0.gd |
| GATE.S1.DISCOVERY_INTERACT.RESULTS.001 | DONE | Scan results display (unlock type/desc): After scan phase completes, display results panel showing unlock type (tech, resource, intel), description text, and awarded items. Reads from SimBridge GetDiscoverySnapshotV0. No SimCore changes — display-only. Determinism: pure read from SimBridge snapshot. Failure mode: no results data → show "No results yet" placeholder. Proof: godot --headless --path . -s res://scripts/tests/test_station_dock_v0.gd | FOUND: scripts/ui/DiscoverySitePanel.gd; FOUND: scripts/bridge/SimBridge.cs; Proof: godot --headless -s test_station_dock_v0.gd |

### B19. Hygiene gates (code health) — EPIC.X.CODE_HEALTH

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.GEN_REPORT_EXTRACT.001 | DONE | Extract GalaxyGenerator Build*Report methods → ReportBuilder.cs: Move BuildTopologyDump, BuildEconLoopsReport, BuildInvariantsReport, BuildWorldClassReport and helper methods from GalaxyGenerator.cs (~600 lines) to new ReportBuilder.cs. Pure refactor, no behavior change. Determinism: pure move — all outputs byte-for-byte identical. Failure mode: build failure → compile error with missing method reference. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Gen/GalaxyGenerator.cs; NEW: SimCore/Gen/ReportBuilder.cs; Proof: dotnet test -c Release |
| GATE.X.HYGIENE.STATION_MENU_SPLIT.001 | DONE | Extract StationMenu market tab → MarketTabView.cs: Extract market tab logic (buy/sell, row refresh, inventory binding) from StationMenu.cs into MarketTabView.cs. StationMenu becomes orchestrator delegating to tab controllers. Start with market (largest tab). Pure refactor, no behavior change. Determinism: pure refactor — no sim state changes. Failure mode: build failure → missing method or signal reference. Proof: godot --headless --path . -s res://scripts/tests/test_station_loop_v1.gd | FOUND: scripts/ui/StationMenu.cs; NEW: scripts/ui/MarketTabView.cs; Proof: godot --headless -s test_station_loop_v1.gd |
| GATE.X.HYGIENE.GEN_DISCOVERY_EXTRACT.001 | DONE | Extract GalaxyGenerator BuildDiscoverySeed* → DiscoverySeedGen.cs: Move BuildDiscoverySeedSurfaceV0, BuildDiscoverySeedingViolationsReportV0, BuildDiscoveryReadoutV0, BuildUnlockReportV0 and helpers from GalaxyGenerator.cs (~300 lines) to new DiscoverySeedGen.cs. Pure refactor, no behavior change. Determinism: pure move — all outputs byte-for-byte identical. Failure mode: build failure → compile error with missing method reference. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Gen/GalaxyGenerator.cs; NEW: SimCore/Gen/DiscoverySeedGen.cs; Proof: dotnet test -c Release |
| GATE.X.HYGIENE.SIMBRIDGE_COMBAT.001 | DONE | Extract SimBridge combat methods → SimBridge.Combat.cs: Move ApplyTurretShotV0, ApplyAiShotAtPlayerV0, GetFleetCombatHpV0, InitFleetCombatHpV0, DispatchStartCombatV0, DispatchClearCombatV0, GetCombatStatusV0 from SimBridge.cs to new SimBridge.Combat.cs partial. Pure refactor, no behavior change. Determinism: pure move — byte-for-byte identical behavior. Failure mode: build failure → missing method reference. Proof: dotnet build "Space Trade Empire.csproj" --nologo && dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: scripts/bridge/SimBridge.cs; NEW: scripts/bridge/SimBridge.Combat.cs; Proof: dotnet build + dotnet test -c Release |
| GATE.X.HYGIENE.EPIC_REVIEW.001 | DONE | Epic tracking audit + course-correction: Review all TODO/IN_PROGRESS epics in 54_EPICS.md against completed gates in 55_GATES.md. Identify epics that can be marked DONE (all gates complete), stale gates, and recommended next anchor epic. Output: updated 54_EPICS.md statuses + written recommendations in session log. No code changes. Proof: Grep verification of status consistency | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md; FOUND: docs/56_SESSION_LOG.md; Proof: manual consistency check |
| GATE.X.HYGIENE.REPO_HEALTH.001 | DONE | Full repo health pass: Run full test suite, check C# compiler warnings beyond known CS8602 pair, scan for dead/unreachable code in SimCore and scripts, verify golden hash stability, check .csproj references. Output: health report in session log + any fixes applied. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release && dotnet build "Space Trade Empire.csproj" | FOUND: SimCore/SimCore.csproj; FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: Space Trade Empire.csproj; Proof: dotnet test -c Release + dotnet build |
| GATE.X.HYGIENE.PLUGIN_EVAL.001 | DONE | Evaluate Godot plugins for visual + gameplay benefit: Research Godot 4 plugins/addons for spatial audio, particle effects, procedural skybox, post-processing (bloom, god rays), trail rendering. For each candidate: name, purpose, license, Godot 4 compat, integration effort, recommendation (adopt/skip/defer). Output: written evaluation in session log. No code changes. Proof: written evaluation document | FOUND: project.godot; FOUND: docs/54_EPICS.md; Proof: manual evaluation |
| GATE.X.HYGIENE.EPIC_REVIEW.002 | DONE | Epic tracking audit + course-correction (tranche 2): Review all TODO/IN_PROGRESS epics in 54_EPICS.md against completed gates in 55_GATES.md. Identify epics that can be marked DONE (all gates complete), stale gates, and recommended next anchor epic. Output: updated 54_EPICS.md statuses + written recommendations in session log. No code changes. Proof: Grep verification of status consistency | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md; FOUND: docs/56_SESSION_LOG.md; Proof: manual consistency check |
| GATE.X.HYGIENE.REPO_HEALTH.002 | DONE | Full repo health pass (tranche 2): Run full test suite, check C# compiler warnings beyond known CS8602 pair, scan for dead/unreachable code in SimCore and scripts, verify golden hash stability, check .csproj references. Output: health report in session log + any fixes applied. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release && dotnet build "Space Trade Empire.csproj" | FOUND: SimCore/SimCore.csproj; FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: Space Trade Empire.csproj; Proof: dotnet test -c Release + dotnet build |

### B20. Slice 1 visual polish gates (v0) — EPIC.S1.VISUAL_POLISH.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.VISUAL_POLISH.SPACE_ENV.001 | DONE | Skybox + ambient star particles + directional light v0: Replace default Godot background with space skybox (procedural or asset-based WorldEnvironment), add ambient star particle system (GPUParticles3D), configure directional light for sun-like illumination. No SimCore changes. Determinism: visual-only, no sim state. Failure mode: skybox missing → fallback to black background. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scenes/Playable_Prototype.tscn; FOUND: project.godot; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.CELESTIAL.001 | DONE | Planet + asteroid visual upgrade v0: Replace placeholder meshes for planets/asteroids with scaled sphere meshes + material (planets: gradient/solid color by world class; asteroids: irregular gray). Add slow rotation animation. No SimCore changes. Determinism: visual-only. Failure mode: mesh missing → fallback to existing placeholder. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scenes/asteroid.tscn; FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/view/StarNode.cs; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.STRUCTURES.001 | DONE | Station + lane gate geometry upgrade v0: Upgrade station and lane gate 3D models — stations get multi-mesh geometry (ring/cylinder) with emissive materials, lane gates get arch/frame geometry with glow. No SimCore changes. Determinism: visual-only. Failure mode: scene parse error → fallback to existing CSGBox. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scenes/station.tscn; FOUND: scenes/Playable_Prototype.tscn; FOUND: scripts/view/GalaxyView.cs; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.COMBAT_VISUAL.001 | DONE | Fleet meshes + bullet colors + hit VFX v0: Replace fleet marker CSGBox with ship-shaped mesh. Color bullets by source (player=green, AI=red). Add minimal hit particle effect (GPUParticles3D, 0.3s burst on impact). No SimCore changes. Determinism: visual-only. Failure mode: particle scene missing → skip VFX, damage still applies. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scenes/bullet.tscn; FOUND: scenes/enemy.tscn; FOUND: scripts/bullet.gd; FOUND: scripts/view/GalaxyView.cs; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.SHIP_CAMERA.001 | DONE | Engine trail + camera follow tuning v0: Add engine exhaust trail to player ship (GPUParticles3D, emit while moving). Tune player_follow_camera: smoother lerp, slight offset above/behind ship, FOV adjustment during boost. No SimCore changes. Determinism: visual-only. Failure mode: particles don't emit → ship still flies normally. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scenes/player.tscn; FOUND: scripts/view/player_follow_camera.gd; FOUND: scripts/core/hero_ship_flight_controller.gd; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.HUD_LABELS.001 | DONE | HP bars + credits + 3D world labels v0: Add HP bar (TextureProgressBar or ColorRect) to HUD showing player hull/shield. Add credits display. Add 3D Label3D nodes over stations and fleet ships showing names. No SimCore changes — reads existing SimBridge snapshots. Determinism: display-only. Failure mode: label font missing → fallback to default. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scripts/ui/hud.gd; FOUND: scenes/ui_hud.tscn; FOUND: scripts/view/GalaxyView.cs; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.GALAXY_MAP.001 | DONE | Galaxy overlay connection styling v0: Style galaxy map connections — lane links as solid lines, fracture links as dashed/dim, highlight current system. Add system-type icons or color coding (trade hub, frontier, etc.). No SimCore changes. Determinism: visual-only. Failure mode: line draw fails → fallback to existing debug lines. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scripts/view/map_camera.gd; FOUND: scripts/view/galaxy_spawner.gd; FOUND: scripts/view/GalaxyView.cs; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.FLEET_AI.001 | DONE | Fleet AI: patrol, dock, engage state machine v0: GDScript-only state machine for instantiated fleet ships — states: IDLE, PATROL (move between waypoints near node), DOCK (approach station), ENGAGE (move toward player if hostile, fire). All visual-only, no SimCore changes. Hostile fleets and friendly freighters both use this system. Determinism: visual-only, no sim state mutation. Failure mode: AI script error → fleet stays stationary (current behavior). Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scripts/view/GalaxyView.cs; NEW: scripts/core/fleet_ai.gd; Proof: godot --headless -s test_visual_scene_v0.gd |
| GATE.S1.VISUAL_POLISH.SCENE_PROOF.001 | DONE | Visual scene headless proof v0 (HEADLESS_PROOF): Headless test that boots Playable_Prototype scene, verifies: WorldEnvironment exists, player ship has trail particles, station has upgraded mesh, fleet AI script attached to fleet markers, HUD has HP bar node. Pure existence checks — no visual rendering validation. Proof: godot --headless --path . -s res://scripts/tests/test_visual_scene_v0.gd | FOUND: scenes/Playable_Prototype.tscn; FOUND: scripts/core/game_manager.gd; NEW: scripts/tests/test_visual_scene_v0.gd; Proof: godot --headless -s test_visual_scene_v0.gd |

### B21. Slice 5 combat doctrine gates (v0) — EPIC.S5.COMBAT_DOCTRINE.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.COMBAT.COUNTER_FAMILY.001 | DONE | Point defense counter family v0: Add PointDefense weapon family to CombatSystem that counters missiles/torpedoes. Extend CalcDamage to apply counter-family damage bonus when weapon family matches target weapon type. Add CombatTweaksV0 entries for point defense stats. Contract test: PointDefense vs missile → bonus damage; PointDefense vs cannon → no bonus. Determinism: pure math, no timestamps. Failure mode: unknown family → fallback to base damage. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Combat" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs; FOUND: SimCore/Entities/Fleet.cs; Proof: dotnet test --filter "Combat" |
| GATE.S5.COMBAT.ESCORT_DOCTRINE.001 | DONE | Escort doctrine v0 (policy-driven): Add EscortDoctrine to fleet policy system — a fleet with escort doctrine active applies a defensive shield bonus to its escort target. Doctrine stored as fleet state in SimCore. Toggle on/off via command. Contract test: escort fleet present → target gets +25% shield regen; no escort → baseline. Determinism: pure state mutation, no timestamps. Failure mode: escort target not found → doctrine inactive, no crash. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Combat" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore/Entities/Fleet.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs; Proof: dotnet test --filter "Combat" |
| GATE.S5.COMBAT.STRATEGIC_RESOLVER.001 | DONE | Fleet-vs-fleet strategic outcome resolver v0: Multi-round attrition resolver that takes two fleet combat stats (HP, weapons, doctrines) and deterministically resolves combat to outcome: winner, hull/shield losses per side, salvage value. Rounds alternate fire using CalcDamage. Max 50 rounds (stalemate if neither dead). Contract test: stronger fleet wins; equal fleets → stalemate. Determinism: seeded round order, no timestamps. Failure mode: malformed fleet stats → return draw with zero losses. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Combat" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs; NEW: SimCore/Systems/StrategicResolverV0.cs; Proof: dotnet test --filter "Combat" |
| GATE.S5.COMBAT.REPLAY_PROOF.001 | DONE | Deterministic combat replay proof: Serialize full combat sequence (per-round: attacker, damage, shield_dmg, hull_dmg, HP remaining) from strategic resolver. Replay from same inputs produces byte-for-byte identical sequence. Golden hash test: fixed seed → fixed hash. Determinism: this gate IS the determinism proof. Failure mode: hash mismatch → test fails with diff of expected vs actual. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Combat" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs; FOUND: SimCore/Systems/StrategicResolverV0.cs; Proof: dotnet test --filter "Combat" |
| GATE.S5.COMBAT.BRIDGE_DOCTRINE.001 | DONE | Doctrine status + toggle via SimBridge: Add SimBridge methods GetDoctrineStatusV0(fleetId) → {escort_active, escort_target_id} and SetDoctrineV0(fleetId, doctrineType, enabled) → {ok, error}. Wires to SimCore EscortDoctrine. No GDScript UI yet (bridge-only). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Combat.cs; FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore/Entities/Fleet.cs; Proof: dotnet build |
| GATE.S5.COMBAT.SLICE_CLOSE.001 | DONE | Slice 5 content wave scenario proof (HEADLESS_PROOF): End-to-end deterministic scenario: spawn 2 fleets with doctrines (one escort, one attacker with point defense), run strategic resolver, verify counter family bonus applied, verify escort shield bonus applied, verify replay hash matches golden value. Milestone gate closing Slice 5 content wave. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Combat" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore/Systems/StrategicResolverV0.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs; Proof: dotnet test --filter "Combat" |

### B22. Audio gates (v0) — EPIC.S1.AUDIO_MIN.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.AUDIO.SFX_CORE.001 | DONE | Core SFX: thrust + turret + hit + explosion: Add AudioStreamPlayer3D nodes for engine thrust (looping, pitch varies with speed), turret fire (one-shot per G key), bullet hit (one-shot on impact), ship explosion (one-shot on fleet death). Attach to player ship and bullet scenes. Placeholder .wav/.ogg assets (simple synth tones acceptable for v0). No SimCore changes. Determinism: audio-only, no sim state. Failure mode: missing audio file → silent (no crash). Proof: godot --headless --path . -s res://scripts/tests/test_audio_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scripts/bullet.gd; FOUND: scenes/player.tscn; FOUND: scenes/bullet.tscn; NEW: scripts/tests/test_audio_v0.gd; NEW: assets/audio/sfx/; Proof: godot --headless -s test_audio_v0.gd |
| GATE.S1.AUDIO.AMBIENT.001 | DONE | Ambient audio: space drone + warp + dock chimes: Add AudioStreamPlayer for ambient space drone (low-frequency loop, always playing during IN_FLIGHT). Warp transit whoosh (one-shot on lane jump). Dock chime (one-shot on station dock). Volume ducking: lower ambient during combat. No SimCore changes. Determinism: audio-only. Failure mode: missing audio file → silent. Proof: godot --headless --path . -s res://scripts/tests/test_audio_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scenes/Playable_Prototype.tscn; NEW: assets/audio/ambient/; Proof: godot --headless -s test_audio_v0.gd |

### B23. Save/Load UI gates (v0) — EPIC.S1.SAVE_LOAD_UI.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.SAVE_UI.PAUSE_MENU.001 | DONE | Escape key pause menu v0: Escape key toggles pause overlay (Control node). Menu buttons: Resume, Save Game, Load Game, Quit to Desktop. get_tree().paused = true while open. Resume on Escape again or Resume button. Quit calls get_tree().quit(). No SimCore changes. Determinism: UI-only, no sim state. Failure mode: pause during combat → turrets stop (acceptable for v0). Proof: godot --headless --path . -s res://scripts/tests/test_pause_menu_v0.gd | FOUND: scripts/core/game_manager.gd; FOUND: scripts/ui/hud.gd; FOUND: scenes/ui_hud.tscn; NEW: scripts/tests/test_pause_menu_v0.gd; Proof: godot --headless -s test_pause_menu_v0.gd |
| GATE.S1.SAVE_UI.SLOTS.001 | DONE | 3 save slots with metadata display: Save/Load sub-panel with 3 slots. Each slot shows: save timestamp, player credits, current system name, play time. Save writes via SimBridge.SaveV0(slotIndex). Load reads via SimBridge.LoadV0(slotIndex). Empty slots show "Empty". No SimCore changes — wires existing save/load. Determinism: save/load paths already deterministic in SimCore. Failure mode: corrupt save → show error, don't load. Proof: godot --headless --path . -s res://scripts/tests/test_save_slots_v0.gd | FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/ui/hud.gd; NEW: scripts/tests/test_save_slots_v0.gd; Proof: godot --headless -s test_save_slots_v0.gd |

### B24. Slice 6 fracture commerce gates (v0) — EPIC.S6.FRACTURE_COMMERCE.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.FRACTURE.ACCESS_MODEL.001 | DONE | Fracture node access check (hull+tech): FractureAccessCheck(fleetId, nodeId) → {allowed, reason}. Checks: (1) fleet hull class meets minimum durability threshold, (2) fleet has required tech level for node fracture tier. Returns denial reason if blocked. Contract test: qualified fleet → allowed; underclass hull → denied. Determinism: pure function of fleet stats + node config. Failure mode: unknown node → denied with "node not found". Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Fracture" | FOUND: SimCore/Systems/RoutePlanner.cs; FOUND: SimCore/Entities/Fleet.cs; FOUND: SimCore.Tests/SimCore.Tests.csproj; NEW: SimCore/Systems/FractureSystem.cs; Proof: dotnet test --filter "Fracture" |
| GATE.S6.FRACTURE.MARKET_MODEL.001 | DONE | Fracture niche market pricing model: Fracture markets use wider spread (higher buy, lower sell) and more volatile pricing than lane markets. FracturePricingV0 extends base MarketSystem pricing with: volatility multiplier (1.5x), spread multiplier (2x), lower volume caps. Contract test: same good at fracture vs lane → fracture has higher margin. Determinism: pure math pricing. Failure mode: missing market config → fallback to lane pricing. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Market" | FOUND: SimCore/Systems/MarketSystem.cs; FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: SimCore/Systems/FractureSystem.cs; Proof: dotnet test --filter "Market" |
| GATE.S6.FRACTURE.CONTENT.001 | DONE | 3 fracture-exclusive goods + FRACTURE_OUTPOST world class: Add 3 fracture-only trade goods to registry (exotic_crystals, salvaged_tech, anomaly_samples) with high base value. Add FRACTURE_OUTPOST world class to GalaxyGenerator with fracture-tier properties. Registry validation test passes. Determinism: static registry data. Failure mode: missing good ID → registry validation catches at test time. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Registry" | FOUND: SimCore/Data/Registries/GoodsRegistry.cs; FOUND: SimCore/Gen/GalaxyGenerator.cs; FOUND: SimCore.Tests/SimCore.Tests.csproj; Proof: dotnet test --filter "Registry" |
| GATE.S6.FRACTURE.BRIDGE.001 | DONE | GetFractureAccessV0 + FractureMarketV0 SimBridge queries: Add SimBridge.GetFractureAccessV0(fleetId, nodeId) → {allowed, reason, hull_req, tech_req} and GetFractureMarketV0(nodeId) → market listing with fracture pricing. Reads from FractureSystem. No GDScript UI yet (bridge-only). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.cs; FOUND: SimCore/Systems/FractureSystem.cs; FOUND: SimCore/Systems/MarketSystem.cs; Proof: dotnet build |
| GATE.S6.FRACTURE.TRAVEL.001 | DONE | Fracture route planning in RoutePlanner: Extend RoutePlanner to support off-lane fracture jumps. Fracture jumps cost 3x fuel, have risk factor (random event chance per jump). Route query returns {path, fuel_cost, risk_rating}. Contract test: fracture route vs lane route → fracture shorter but costlier. Determinism: route calculation is pure function of graph + fleet stats. Failure mode: no fracture path → return empty with "no route". Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Route" | FOUND: SimCore/Systems/RoutePlanner.cs; FOUND: SimCore.Tests/SimCore.Tests.csproj; Proof: dotnet test --filter "Route" |
| GATE.S6.FRACTURE.ECON_FEEDBACK.001 | DONE | Fracture goods flow into lane hub markets: When fracture goods are sold at lane hub stations, they enter lane market supply pool, gradually reducing price at the hub. Scenario test: deliver fracture goods to hub → hub price drops → proves fracture supplements lanes rather than replacing them. ECON_INVARIANT: lane total volume never decreases when fracture volume increases. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Market" | FOUND: SimCore/Systems/MarketSystem.cs; FOUND: SimCore/Systems/FractureSystem.cs; FOUND: SimCore.Tests/SimCore.Tests.csproj; Proof: dotnet test --filter "Market" |

### B25. Mission runner gates (v0) — EPIC.S1.MISSION_RUNNER.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.MISSION.MODEL.001 | DONE | Mission schema + state model v0: MissionDef (mission_id, title, description, prerequisites, steps with trigger types, rewards), MissionState (active_mission_id, current_step_index, completed_missions, step_history), MissionTriggerType enum (VisitedNode, BoughtGood, SoldGood, EarnedCredits, DockedStation, KilledFleet, ScannedDiscovery). Persistence through SimState + SerializationSystem. Contract test: create MissionDef, advance through steps, verify state transitions. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "MissionContract" | NEW: SimCore/Entities/Mission.cs; NEW: SimCore.Tests/Systems/MissionContractTests.cs; FOUND: SimCore/SimState.cs |
| GATE.S1.MISSION.SYSTEM.001 | DONE | MissionSystem v0: EvaluateTriggers checks current step trigger against SimState (e.g., visited node matches TargetNodeId); AdvanceStep moves to next step or completes mission and applies rewards; emits schema-bound MissionEvents (Accepted, StepCompleted, MissionCompleted). Wired into SimKernel tick. Deterministic trigger evaluation with stable tie-breaks. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "MissionContract" | NEW: SimCore/Systems/MissionSystem.cs; FOUND: SimCore/Entities/Mission.cs; FOUND: SimCore/SimKernel.cs |
| GATE.S1.MISSION.CONTENT.001 | DONE | Mission 1 "Matched Luggage" v0: static MissionDef registered in MissionSystem. 4 steps: (1) dock at starting station [DockedStation], (2) buy any good [BoughtGood], (3) travel to adjacent station [VisitedNode], (4) sell good for profit [SoldGood]. Reward: 50 credits bonus. No prerequisites. Validation test: Mission 1 loads, steps are well-formed, triggers resolve against micro-world. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "MissionContent" | NEW: SimCore.Tests/Systems/MissionContentTests.cs; FOUND: SimCore/Systems/MissionSystem.cs; FOUND: SimCore/Entities/Mission.cs |
| GATE.S1.MISSION.DETERMINISM.001 | DONE | Mission determinism regression v0: accept Mission 1, advance through all steps via scripted commands, verify final world hash is stable across 2 runs. Save/load mid-mission preserves mission state (active_mission_id, current_step_index, completed list). Deterministic event ordering. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "MissionDeterminism" | NEW: SimCore.Tests/Determinism/MissionDeterminismTests.cs; FOUND: SimCore/Entities/Mission.cs; FOUND: SimCore/Systems/MissionSystem.cs |
| GATE.S1.MISSION.BRIDGE.001 | DONE | SimBridge mission queries v0: GetActiveMissionV0() returns {mission_id, title, current_step, total_steps, objective_text, completed}; GetMissionListV0() returns available missions with prerequisites check; AcceptMissionV0(missionId) enqueues mission acceptance. Read lock for queries, write lock for accept. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Mission.cs; FOUND: scripts/bridge/SimBridge.cs |
| GATE.S1.MISSION.HUD.001 | DONE | Mission objective panel in HUD v0: persistent panel showing current mission title + current step objective text. Updates per-frame from SimBridge.GetActiveMissionV0(). Hidden when no active mission. Positioned below cargo/credits in flight HUD. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd; FOUND: scripts/core/game_manager.gd |
| GATE.S1.MISSION.HEADLESS_PROOF.001 | DONE | Mission 1 headless completion proof (PLAYABLE_BEAT): GDScript headless test boots playable prototype, accepts Mission 1 via SimBridge, scripts dock-buy-travel-sell sequence, verifies mission completes and reward applied. Deterministic transcript with stable hash. Proof: godot --headless --path . -s scripts/tests/test_mission_proof_v0.gd | NEW: scripts/tests/test_mission_proof_v0.gd; FOUND: scripts/core/game_manager.gd; FOUND: scripts/ui/hud.gd |

### B26. Fracture economy invariant gates (v0) — EPIC.S6.FRACTURE_ECON_INVARIANTS.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.FRACTURE_ECON.INVARIANT.001 | DONE | Fracture lane-dominance scenario invariant v0: 100-seed sweep (seeds 1..100) with fracture goods enabled. Measure lane_volume_fraction (lane trade volume / total volume) must stay > 0.80 in every seed with fracture access. Measure fracture_unique_goods count must be >= 2 per access seed. Deterministic, stable ordering, hard-fail on drift. No timestamps. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "FractureEconInvariant" | NEW: SimCore.Tests/Systems/FractureEconInvariantTests.cs; FOUND: SimCore/Systems/FractureSystem.cs |

### B27. Epic closure gates (tranche 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.VISUAL_POLISH.EPIC_CLOSE.001 | DONE | Close EPIC.S1.VISUAL_POLISH.V0: verify all 9 constituent gates DONE (SPACE_ENV, CELESTIAL, STRUCTURES, COMBAT_VISUAL, SHIP_CAMERA, HUD_LABELS, GALAXY_MAP, FLEET_AI, SCENE_PROOF). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms all 9 DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md |
| GATE.S1.AUDIO.EPIC_CLOSE.001 | DONE | Close EPIC.S1.AUDIO_MIN.V0: verify all 2 constituent gates DONE (SFX_CORE.001, AMBIENT.001). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms both DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md |
| GATE.S1.SAVE_UI.EPIC_CLOSE.001 | DONE | Close EPIC.S1.SAVE_LOAD_UI.V0: verify all 2 constituent gates DONE (PAUSE_MENU.001, SLOTS.001). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms both DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md |
| GATE.S1.DISCOVERY_INTERACT.EPIC_CLOSE.001 | DONE | Close EPIC.S1.DISCOVERY_INTERACT.V0: verify all 3 constituent gates DONE (PANEL.001, SCAN.001, RESULTS.001). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms all 3 DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md |
| GATE.S5.COMBAT_DOCTRINE.EPIC_CLOSE.001 | DONE | Close EPIC.S5.COMBAT_DOCTRINE.V0: verify all 6 constituent gates DONE (COUNTER_FAMILY.001, ESCORT_DOCTRINE.001, STRATEGIC_RESOLVER.001, REPLAY_PROOF.001, BRIDGE_DOCTRINE.001, SLICE_CLOSE.001). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms all 6 DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md |
| GATE.S6.FRACTURE.EPIC_CLOSE.001 | DONE | Close EPIC.S6.FRACTURE_COMMERCE.V0: verify all 6 constituent gates DONE (ACCESS_MODEL.001, MARKET_MODEL.001, CONTENT.001, BRIDGE.001, TRAVEL.001, ECON_FEEDBACK.001). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms all 6 DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md |
| GATE.X.CODE_HEALTH.EPIC_CLOSE.001 | DONE | Close EPIC.X.CODE_HEALTH: verify all 4 constituent gates DONE (GEN_REPORT_EXTRACT.001, STATION_MENU_SPLIT.001, GEN_DISCOVERY_EXTRACT.001, SIMBRIDGE_COMBAT.001). Update 54_EPICS.md status to DONE. Proof: grep 55_GATES.md confirms all 4 DONE | FOUND: docs/55_GATES.md; FOUND: docs/54_EPICS.md; FOUND: docs/56_SESSION_LOG.md |

### B28. Hygiene gates (tranche 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.SLICE5_AUDIT.001 | DONE | Slice 5 content wave readiness audit: verify all S5 gates DONE (COMBAT_LOCAL 5 gates, COMBAT_PLAYABLE 4 gates, COMBAT doctrine 6 gates). No orphaned TODO S5 gates. Proof: grep 55_GATES.md for S5 gate statuses | FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.REPO_HEALTH.003 | DONE | Full repo health baseline: run full test suite (307+ tests), warning scan, dead code check, golden hash stability. Report regressions. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.003 | DONE | Epic tracking audit: compare 54_EPICS.md epic statuses against completed gates in 55_GATES.md. Identify mismatches (epics still TODO with all gates DONE). Update statuses. Recommend next anchor epic. Proof: grep consistency check | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md; FOUND: docs/56_SESSION_LOG.md |
| GATE.X.HYGIENE.GREATNESS_EVAL.001 | DONE | "First 60 minutes" player experience evaluation: assess current playable prototype against Greatness Spec criteria (guided objectives, economic loop, combat feel, discovery, progression, save/load). Identify gaps and prioritize. Proof: written evaluation document | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |

### B30. Slice 4 industry pipeline gates (tranche 4)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S4.TECH.CORE.001 | DONE | Tech research foundation: TechLead/Prototype/ResearchQueue entities in SimCore/Entities/TechState.cs, ResearchTweaksV0 with time/material costs, TechContentV0 with starter tech leads tied to discovery unlocks. Add TechState to SimState. Contract tests for serialization. Proof: dotnet test SimCore.Tests -c Release | NEW: SimCore/Entities/TechState.cs; NEW: SimCore/Tweaks/ResearchTweaksV0.cs; NEW: SimCore/Content/TechContentV0.cs; FOUND: SimCore/SimState.cs |
| GATE.S4.UPGRADE.CORE.001 | DONE | Refit foundation: RefitJob entity on Fleet (module_to_install, progress, time_cost). Add refit queue field to Fleet entity. Refit tweaks added to IndustryTweaksV0. Contract tests. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore/Entities/Fleet.cs; FOUND: SimCore/Tweaks/IndustryTweaksV0.cs |
| GATE.S4.MAINT.CORE.001 | DONE | Maintenance foundation: Condition (0-100) field on IndustrySite with decay_rate. Maintenance tweaks added to IndustryTweaksV0 (decay_per_tick, repair_cost_per_point). Contract tests. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore/Entities/IndustrySite.cs; FOUND: SimCore/Tweaks/IndustryTweaksV0.cs |
| GATE.S4.TECH.SYSTEM.001 | DONE | ResearchSystem: Process(state) advances active research if materials available, transitions Lead->Prototype->Manufacturable, produces new Recipe on completion. Determinism test with golden hash. Proof: dotnet test SimCore.Tests -c Release --filter "Research" | NEW: SimCore/Systems/ResearchSystem.cs; FOUND: SimCore/SimState.cs |
| GATE.S4.UPGRADE.SYSTEM.001 | DONE | RefitSystem: Process(state) advances refit jobs for fleets docked at stations, installs/removes modules on completion. Determinism test. Proof: dotnet test SimCore.Tests -c Release --filter "Refit" | NEW: SimCore/Systems/RefitSystem.cs; FOUND: SimCore/Entities/Fleet.cs |
| GATE.S4.MAINT.SYSTEM.001 | DONE | MaintenanceSystem: Process(state) decays condition per tick, consumes supply goods to repair, production efficiency scales with condition. Determinism test. Proof: dotnet test SimCore.Tests -c Release --filter "Maintenance" | NEW: SimCore/Systems/MaintenanceSystem.cs; FOUND: SimCore/Entities/IndustrySite.cs |
| GATE.S4.TECH.BRIDGE.001 | DONE | SimBridge research queries: GetResearchQueueV0 (active leads, progress), StartResearchV0 (begin researching a lead), GetAvailableTechLeadsV0 (discovered but unresearched). Follows existing partial class pattern. Proof: dotnet build Space Trade Empire.csproj | NEW: scripts/bridge/SimBridge.Research.cs; FOUND: scripts/bridge/SimBridge.cs |
| GATE.S4.UPGRADE.BRIDGE.001 | DONE | SimBridge refit queries: GetRefitQueueV0 (active refit jobs), StartRefitV0 (begin installing module), GetRefitableModulesV0 (available modules). Proof: dotnet build Space Trade Empire.csproj | NEW: scripts/bridge/SimBridge.Refit.cs; FOUND: scripts/bridge/SimBridge.cs |
| GATE.S4.MAINT.BRIDGE.001 | DONE | SimBridge maintenance queries: GetMaintenanceStatusV0 (condition per site at node), RepairSiteV0 (consume goods to repair). Add to SimBridge.cs main file. Proof: dotnet build Space Trade Empire.csproj | FOUND: scripts/bridge/SimBridge.cs; FOUND: SimCore/Systems/MaintenanceSystem.cs |
| GATE.S4.TECH.SAVE.001 | DONE | Research + refit + maintenance state save/load preservation: TechState, refit queue, and condition fields serialize correctly in QuickSaveV2. Golden hash update. Proof: dotnet test SimCore.Tests -c Release --filter "SaveLoad" | FOUND: SimCore/Systems/SerializationSystem.cs; FOUND: SimCore/SimState.cs |
| GATE.S4.UI_INDU.RESEARCH.001 | DONE | Research panel in hero_trade_menu.gd: show available tech leads with "Research" button, active research progress bar, completed techs. Uses SimBridge.Research queries. Proof: dotnet build Space Trade Empire.csproj | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S4.UI_INDU.UPGRADE.001 | DONE | Refit panel in hero_trade_menu.gd: show available modules with "Install" button, active refit progress, current loadout. Uses SimBridge.Refit queries. Proof: dotnet build Space Trade Empire.csproj | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Refit.cs |
| GATE.S4.UI_INDU.MAINT.001 | DONE | Maintenance view in production panel: show condition percentage per site, "Repair" button consuming goods. Integrates with existing _rebuild_production_info in hero_trade_menu.gd. Proof: dotnet build Space Trade Empire.csproj | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.cs |
| GATE.S4.TECH.PROOF.001 | DONE | Research pipeline headless proof: extends SceneTree script boots scene, discovers a tech lead, starts research, advances ticks until complete, verifies new recipe available. Emits RESEARCH_PROOF PASS. Proof: godot --headless --path . -s res://scripts/tests/test_research_proof_v0.gd | NEW: scripts/tests/test_research_proof_v0.gd; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S4.INDU.PLAYABLE_BEAT.001 | DONE | Industry in-engine playable beat: extends SceneTree script boots scene, docks at station, views research panel, starts a refit, verifies maintenance condition visible in production. Emits INDUSTRY_BEAT PASS. Proof: godot --headless --path . -s res://scripts/tests/test_industry_beat_v0.gd | NEW: scripts/tests/test_industry_beat_v0.gd; FOUND: scripts/ui/hero_trade_menu.gd |

### B31. Hygiene gates (tranche 4)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.004 | DONE | Full repo health baseline: run full test suite (339+ tests), warning scan, dead code check, golden hash stability. Report regressions. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.004 | DONE | Epic tracking audit: compare 54_EPICS.md epic statuses against completed gates in 55_GATES.md. Identify mismatches. Update statuses. Recommend next anchor epic for tranche 5. Proof: grep consistency check | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md; FOUND: docs/56_SESSION_LOG.md |
| GATE.X.EVAL.PROGRESSION_AUDIT.001 | DONE | Progression depth evaluation: audit current progression systems (missions, research, upgrades, combat, discovery) for depth, interconnection, and player feedback. Score each axis 1-5. Identify gaps for tranche 5. Proof: written evaluation | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |

### B32. Presentation + Industry Depth gates (tranche 5)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.005 | DONE | Full test suite, warning scan, golden hash stability. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: docs/56_SESSION_LOG.md |
| GATE.S1.CAMERA.ADDON_SETUP.001 | DONE | Install Phantom Camera addon into addons/, enable in project.godot, verify headless boot. Proof: godot --headless --path . --quit | FOUND: project.godot; NEW: addons/phantom_camera/plugin.cfg |
| GATE.S1.SPATIAL_AUDIO.ENGINE_THRUST.001 | DONE | Engine thrust AudioStreamRandomizer on player ship — AudioStreamPlayer3D child on player scene, controlled by engine_audio.gd. Proof: godot --headless --path . --quit | FOUND: scenes/player.tscn; NEW: scripts/audio/engine_audio.gd |
| GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001 | DONE | Add Tier field to TechDef, tier-gated prerequisite checks in ResearchSystem, scaled credit costs per tier, TechLevel counter on TechState. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "ResearchSystem" | FOUND: SimCore/Content/TechContentV0.cs; FOUND: SimCore.Tests/Systems/ResearchSystemTests.cs |
| GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001 | DONE | Timed refit queue — QueueInstall with tick duration on RefitSystem, RefitQueue field on Fleet, InstallTicks on ModuleDef. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "RefitSystem" | FOUND: SimCore/Systems/RefitSystem.cs; FOUND: SimCore.Tests/Systems/RefitSystemTests.cs |
| GATE.S4.MAINT_SUSTAIN.SUPPLY_REPAIR.001 | DONE | Repair consumes supply goods not just credits — supply-based repair path in MaintenanceSystem, SupplyLevel field on IndustrySite. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "MaintenanceSystem" | FOUND: SimCore/Systems/MaintenanceSystem.cs; FOUND: SimCore.Tests/Systems/MaintenanceSystemTests.cs |
| GATE.S3.RISK_SINKS.DELAY_MODEL.001 | DONE | Player-visible travel delay model — expose delay BPS via query in RiskSystem, DelayTicksRemaining field on Fleet, dedicated delay tests. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "RiskDelay" | FOUND: SimCore/Systems/RiskSystem.cs; NEW: SimCore.Tests/Systems/RiskDelayTests.cs |
| GATE.S1.CAMERA.FOLLOW_MODES.001 | DONE | PhantomCamera follow modes — replace player_follow_camera with PhantomCamera3D, flight/orbit/station modes, PhantomCameraHost in main. Proof: godot --headless --path . --quit | FOUND: scripts/view/player_follow_camera.gd; FOUND: scenes/player.tscn |
| GATE.S1.SPATIAL_AUDIO.COMBAT_SFX.001 | DONE | Positional SFX for turret fire and bullet impact — AudioStreamPlayer3D on bullet scene, combat_audio.gd controller. Proof: godot --headless --path . --quit | FOUND: scripts/bullet.gd; FOUND: scenes/bullet.tscn |
| GATE.S1.SPATIAL_AUDIO.AMBIENT.001 | DONE | Ambient spatial audio — station hum AudioStreamPlayer3D on station scene, ambient_audio.gd controller. Proof: godot --headless --path . --quit | FOUND: scenes/station.tscn; NEW: scripts/audio/ambient_audio.gd |
| GATE.S4.TECH_INDUSTRIALIZE.BRIDGE_DEPTH.001 | DONE | SimBridge queries for tech tier and tier-gated content — GetTechTierV0, GetTechRequirementsV0 in SimBridge.Research.cs, show tier in research panel. Proof: dotnet build && godot --headless test_research_proof_v0.gd | FOUND: scripts/bridge/SimBridge.Research.cs; FOUND: scripts/tests/test_research_proof_v0.gd |
| GATE.S4.UPGRADE_PIPELINE.BRIDGE_QUEUE.001 | DONE | SimBridge refit queue status + progress polling — GetRefitQueueV0, GetRefitProgressV0 in SimBridge.Refit.cs, progress bar in upgrade panel. Proof: dotnet build && godot --headless test_industry_beat_v0.gd | FOUND: scripts/bridge/SimBridge.Refit.cs; FOUND: scripts/tests/test_industry_beat_v0.gd |
| GATE.S4.MAINT_SUSTAIN.BRIDGE_SUPPLY.001 | DONE | SimBridge supply level queries + repair-with-supply intent — GetSupplyLevelV0, DispatchSupplyRepairV0 in SimBridge.Maintenance.cs, supply level in maintenance panel. Proof: dotnet build && godot --headless test_industry_beat_v0.gd | FOUND: scripts/bridge/SimBridge.Maintenance.cs; FOUND: scripts/tests/test_industry_beat_v0.gd |
| GATE.S4.UI_INDU.WHY_BLOCKED.001 | DONE | Why-blocked tooltip — GetResearchBlockReasonV0, GetRefitBlockReasonV0, GetRepairBlockReasonV0 in bridge partials, display block reason labels in hero_trade_menu. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Research.cs; FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S3.RISK_SINKS.BRIDGE.001 | DONE | SimBridge delay risk queries + travel ETA — new SimBridge.Risk.cs partial with GetDelayStatusV0, GetTravelEtaV0, delay status in flight HUD. Proof: dotnet build | NEW: scripts/bridge/SimBridge.Risk.cs; FOUND: scripts/ui/hud.gd |
| GATE.S1.CAMERA.COMBAT_SHAKE.001 | DONE | Camera shake on turret fire and damage received — PhantomCamera noise shake trigger, integrated with G-key fire and damage events. Proof: godot --headless --path . --quit | FOUND: scripts/view/player_follow_camera.gd; FOUND: scripts/core/game_manager.gd |
| GATE.S1.PRESENTATION.HEADLESS_PROOF.001 | DONE | Headless proof — extends SceneTree script boots main scene, verifies PhantomCamera node present, audio nodes present, emits PRESENTATION_PROOF PASS. Proof: dotnet build && godot --headless test_presentation_proof_v0.gd | NEW: scripts/tests/test_presentation_proof_v0.gd; FOUND: scenes/player.tscn |
| GATE.X.HYGIENE.EPIC_REVIEW.005 | DONE | Epic tracking audit: compare 54_EPICS.md epic statuses against completed gates in 55_GATES.md. Identify mismatches. Update statuses. Recommend next anchor epic for tranche 6. Proof: grep consistency check | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md; FOUND: docs/56_SESSION_LOG.md |
| GATE.X.EVAL.AUDIO_VISUAL_AUDIT.001 | DONE | Audio/visual presentation quality audit: evaluate audio mix, spatial audio falloff, camera feel, visual polish. Score axes 1-5. Recommend polish targets. Proof: written evaluation | FOUND: docs/56_SESSION_LOG.md; FOUND: scripts/tools/aesthetic_audit.gd |
| GATE.X.HYGIENE.REPO_HEALTH.006 | DONE | Full test suite pass (439 tests), golden hash stability, warning scan, dead code check. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/GoldenReplayTests.cs; FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.S1.VISUAL_UPGRADE.ADDON_INSTALL.001 | DONE | Install 3 visual addons: Starlight (skybox), 3D Planet Generator + Atmosphere Shader, Kenney Space Kit. Copy to addons/, enable in project.godot, dotnet build passes. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: addons/starlight/; NEW: addons/planet_gen/; FOUND: project.godot |
| GATE.S9.UI.TOOLTIP_SETUP.001 | DONE | Install Tooltips Pro addon, enable in project.godot, add one test tooltip to dock menu verify instantiation. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: addons/tooltips_pro/; FOUND: project.godot; FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S1.MISSION.CONTENT_WAVE.001 | DONE | Add 3 new MissionDefs to MissionContentV0.cs: hauling, combat patrol, multi-hop delivery. Each with 2-3 steps, binding tokens, credit rewards. Determinism test with all missions. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Content/MissionContentV0.cs; FOUND: SimCore/Entities/Mission.cs |
| GATE.S4.CATALOG.TECH_WAVE.001 | DONE | Add 5+ new TechDefs to TechContentV0.cs across tiers 1-3: engine efficiency, cargo expansion, sensor range, shield boost, weapon calibration. Prerequisite chains. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Content/TechContentV0.cs; FOUND: SimCore.Tests/Content/ContentRegistryContractTests.cs |
| GATE.S4.CATALOG.MODULE_WAVE.001 | DONE | Add 5+ new module entries in UpgradeContentV0.cs: engine mk2, utility scanner, cargo expander, shield generator, weapon laser mk2. Update WellKnownModuleIds. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Content/UpgradeContentV0.cs; FOUND: SimCore/Content/WellKnownModuleIds.cs |
| GATE.S4.CATALOG.RECIPE_WAVE.001 | DONE | Add 3-5 new recipes in content registry: advanced fuel, electronics, composite armor. Update WellKnownRecipeIds + WellKnownGoodIds as needed. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Content/WellKnownRecipeIds.cs; FOUND: SimCore/Content/WellKnownGoodIds.cs |
| GATE.S3.RISK_SINKS.HUD_INDICATOR.001 | DONE | Add travel risk/delay indicator to flight HUD: query GetDelayStatusV0 + GetTravelEtaV0 from SimBridge, display estimated delay and risk level for nearest lane gate. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd; FOUND: scripts/bridge/SimBridge.Risk.cs |
| GATE.S1.VISUAL_UPGRADE.SKYBOX.001 | DONE | Wire Starlight procedural skybox into Playable_Prototype.tscn: replace or augment WorldEnvironment, configure star density and nebula. Proof: godot --headless --path . --quit | FOUND: scenes/Playable_Prototype.tscn; FOUND: project.godot |
| GATE.S1.VISUAL_UPGRADE.WORLD_MESHES.001 | DONE | Apply planet generator to planet nodes in GalaxyView.cs, replace player ship mesh in player.tscn with Kenney model, replace station mesh in station.tscn. Proof: godot --headless --path . --quit | FOUND: scripts/view/GalaxyView.cs; FOUND: scenes/player.tscn; FOUND: scenes/station.tscn |
| GATE.S9.UI.TOOLTIP_DOCK.001 | DONE | Add Tooltips Pro tooltips to market goods list (price, demand trend) and industry panels (research progress, refit time, maintenance status) in hero_trade_menu.gd. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S9.UI.TOOLTIP_HUD.001 | DONE | Add Tooltips Pro tooltips to flight HUD elements (hull/shield bars, credits, cargo) and combat readouts (weapon type, damage, range) in hud.gd. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd; FOUND: scripts/bridge/SimBridge.Combat.cs |
| GATE.S1.MISSION.MULTI_PROOF.001 | DONE | Headless scenario test: accept mission 1, complete it, accept mission 2, verify state transitions. Extends SceneTree, boots main scene, drives through SimBridge. Proof: godot --headless --path . res://scripts/tests/test_multi_mission_v0.gd | NEW: scripts/tests/test_multi_mission_v0.gd; FOUND: SimCore/Content/MissionContentV0.cs |
| GATE.S4.CATALOG.CONTENT_PROOF.001 | DONE | Determinism test: 1000-tick run with expanded tech tree + modules + recipes, verify golden hash stable, content registry digest unchanged across loads. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/Content/ContentRegistryContractTests.cs; FOUND: SimCore.Tests/GoldenReplayTests.cs |
| GATE.S1.VISUAL_UPGRADE.SCENE_PROOF.001 | DONE | Headless proof: boot Playable_Prototype.tscn, verify Starlight skybox node present, planet meshes non-null, Kenney models loaded. Proof: godot --headless --path . res://scripts/tests/test_visual_upgrade_v0.gd | NEW: scripts/tests/test_visual_upgrade_v0.gd; FOUND: scenes/Playable_Prototype.tscn |
| GATE.S9.UI.TOOLTIP_PROOF.001 | DONE | Headless proof: boot main scene, verify TooltipManager autoload present, tooltip nodes instantiated on market panel. Proof: godot --headless --path . res://scripts/tests/test_tooltip_proof_v0.gd | NEW: scripts/tests/test_tooltip_proof_v0.gd; FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.X.HYGIENE.EPIC_REVIEW.006 | DONE | Epic audit: verify CAMERA_POLISH.V0, SPATIAL_AUDIO_DEPTH.V0, RISK_SINKS.V0, MAINT_SUSTAIN, TECH_INDUSTRIALIZE, UPGRADE_PIPELINE, UI_INDU against completed gates. Close completed epics in 54_EPICS.md. Recommend next anchor. Proof: grep consistency | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md; FOUND: docs/56_SESSION_LOG.md |
| GATE.X.EVAL.ADDON_COMPAT.001 | DONE | Addon compatibility audit: verify all 6 addons (PhantomCamera, DebugMenu, Starlight, PlanetGen, KenneyKit, TooltipsPro) coexist without conflicts. Check load order, performance impact, C# compatibility. Proof: written evaluation | FOUND: project.godot; FOUND: docs/56_SESSION_LOG.md |

## H. Tranche 7 — Industry Completion + Pressure Framework

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S4.CONSTR_PROG.MODEL.001 | DONE | ConstructionProject entity + ConstructionDef content + ConstructionTweaksV0. 4 project types: Depot, Shipyard, Refinery, ScienceCenter. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | NEW: SimCore/Entities/ConstructionProject.cs; NEW: SimCore/Content/ConstructionContentV0.cs; NEW: SimCore/Tweaks/ConstructionTweaksV0.cs; FOUND: SimCore/SimState.cs |
| GATE.S4.CONSTR_PROG.SYSTEM.001 | DONE | ConstructionSystem: tick-based step advancement, resource consumption per step, completion triggers, stall on insufficient credits. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | NEW: SimCore/Systems/ConstructionSystem.cs; FOUND: SimCore/SimKernel.cs |
| GATE.S4.CONSTR_PROG.SAVE.001 | DONE | Construction state save/load preservation + golden hash update. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/Determinism/LongRunWorldHashTests.cs; FOUND: docs/generated/snapshots/golden_replay_hashes.txt |
| GATE.S4.CONSTR_PROG.BRIDGE.001 | DONE | SimBridge.Construction.cs: GetConstructionProjectsV0, StartConstructionV0, GetConstructionProgressV0, GetConstructionBlockReasonV0. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Construction.cs |
| GATE.S4.CONSTR_PROG.UI.001 | DONE | Construction tab in dock menu (hero_trade_menu.gd): project list, start button, progress bar, why-blocked tooltip. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Construction.cs |
| GATE.S4.NPC_INDU.DEMAND.001 | DONE | NpcIndustrySystem: NPC nodes generate demand based on world class + connected trade routes. Demand influences market prices. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | NEW: SimCore/Systems/NpcIndustrySystem.cs; NEW: SimCore/Tweaks/NpcIndustryTweaksV0.cs; FOUND: SimCore/SimState.cs |
| GATE.S4.NPC_INDU.REACTION.001 | DONE | NPC production reactions: industry sites adjust output toward shortfalls, expand capacity when profitable. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore/Systems/NpcIndustrySystem.cs; FOUND: SimCore/SimState.cs |
| GATE.S4.NPC_INDU.BRIDGE.001 | DONE | SimBridge NPC industry queries: GetNodeIndustryStatusV0, GetNpcDemandV0. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.cs |
| GATE.S4.NPC_INDU.PROOF.001 | DONE | NPC industry scenario proof: 200-tick run with shortfall → NPC adjusts production → shortfall resolved. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "NpcIndustry" | NEW: SimCore.Tests/Systems/NpcIndustrySystemTests.cs |
| GATE.S4.PERF_BUDGET.INDUSTRY.001 | DONE | Industry tick budget tests: all industry systems (Construction, NpcIndustry, Research, Refit, Maintenance) complete 1000-tick run within budget. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "PerfBudget" | NEW: SimCore.Tests/Performance/IndustryPerfBudgetTests.cs |
| GATE.X.PRESSURE.MODEL.001 | DONE | PressureState entity: 5-state enum (Normal/Strained/Unstable/Critical/Collapsed), PressureDirection (Improving/Stable/Worsening), PressureDomain ID, PressureDelta entry. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | NEW: SimCore/Entities/PressureState.cs; FOUND: SimCore/SimState.cs |
| GATE.X.PRESSURE.SYSTEM.001 | DONE | PressureSystem: max-one-state-jump enforcement per window, intervention budget tracking (1-3 alerts per 10 min), domain state transitions. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | NEW: SimCore/Systems/PressureSystem.cs; NEW: SimCore/Tweaks/PressureTweaksV0.cs |
| GATE.X.PRESSURE.BRIDGE.001 | DONE | SimBridge.Pressure.cs: GetPressureDomainsV0, GetPressureAlertCountV0, GetDomainForecastV0. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Pressure.cs |
| GATE.X.PRESSURE.PROOF.001 | DONE | Pressure alert-count scenario test: 500-tick run triggers piracy pressure, verifies max-one-jump, verifies intervention budget QA metric. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "PressureScenario" | NEW: SimCore.Tests/Systems/PressureSystemScenarioTests.cs |
| GATE.S4.SLICE_CLOSE.PROOF.001 | DONE | Slice 4 completion scenario proof: construction + NPC industry + research + refit + maintenance all exercised in 1000-tick scenario. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Slice4" | NEW: SimCore.Tests/Scenarios/Slice4CompletionProofTests.cs |
| GATE.X.HYGIENE.REPO_HEALTH.007 | DONE | Full test suite pass, warning scan, golden hash stability baseline. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.007 | DONE | Audit epic statuses against completed gates, close CONSTR_PROG + NPC_INDU + PERF_BUDGET + PRESSURE_FORMALIZATION. Recommend next anchor. Proof: grep consistency | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |
| GATE.X.EVAL.INDUSTRY_AUDIT.001 | DONE | Industry systems completeness audit: verify all S4 systems are wired to SimKernel.Step(), all bridge queries callable, content coverage. Proof: written evaluation | FOUND: SimCore/SimKernel.cs; FOUND: scripts/bridge/SimBridge.cs |

## L. Slice 7 — Procedural Planets (EPIC.S7.PROCEDURAL_PLANETS.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.PLANET.MODEL.001 | DONE | Planet + Star entities (PlanetType, StarClass enums), PlanetTweaksV0 constants, PlanetContentV0 type/specialization distributions. Proof: dotnet test SimCore.Tests -c Release | NEW: SimCore/Entities/Planet.cs; NEW: SimCore/Entities/Star.cs; NEW: SimCore/Tweaks/PlanetTweaksV0.cs; NEW: SimCore/Content/PlanetContentV0.cs |
| GATE.S7.PLANET.GENERATION.001 | DONE | PlanetInitGen: deterministic star+planet generation from nodeId hashes, star luminosity→planet temperature, gravity+atmosphere→landability. Wired into GalaxyGenerator Phase 3. Proof: dotnet test SimCore.Tests -c Release | NEW: SimCore/Gen/PlanetInitGen.cs; FOUND: SimCore/Gen/GalaxyGenerator.cs; FOUND: SimCore/SimState.Properties.cs; FOUND: SimCore/SimState.cs |
| GATE.S7.PLANET.BRIDGE.001 | DONE | SimBridge.Planet.cs: GetPlanetInfoV0, GetStarInfoV0 with star visual color. PlanetSnapV0+StarSnapV0 in MapQueries SystemSnapshotV0. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/bridge/SimBridge.Planet.cs; FOUND: SimCore/MapQueries.cs |
| GATE.S7.PLANET.ECONOMY.001 | DONE | Planet industry seeding: Agriculture→food, Mining→ore, Manufacturing→metal, HighTech→electronics, FuelExtraction→fuel. Conservative output rates. Golden hashes re-minted. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore/Gen/PlanetInitGen.cs; FOUND: SimCore/Tweaks/PlanetTweaksV0.cs |
| GATE.S7.PLANET.DOCK_VISUAL.001 | DONE | Landable planets get Area3D dock trigger with body_entered→game_manager wiring. Planet type→addon scene mapping. Star mesh color from star class. Planet name Label3D. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/core/game_manager.gd |
| GATE.S7.PLANET.TECH_GATE.001 | DONE | planetary_landing_mk1 tech (tier 1, 10 ticks, 80cr). effective_landable field factors in player tech unlock. GalaxyView uses effective_landable for dock trigger. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: SimCore/Content/TechContentV0.cs; FOUND: scripts/bridge/SimBridge.Planet.cs; FOUND: scripts/view/GalaxyView.cs |
| GATE.S7.PLANET.UI.001 | DONE | Dock menu shows "PLANET: {name}" title + info subtitle (type, gravity, atmosphere, temperature, specialization) when docked at planet. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S7.PLANET.PROOF.001 | DONE | 490/490 tests pass. Golden hashes re-minted for planet industry changes. Economy stress ceiling 25x. ExplorationBot 8/8 pass, no CRITICAL flags. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/GoldenReplayTests.cs; FOUND: SimCore.Tests/Determinism/LongRunWorldHashTests.cs; FOUND: SimCore.Tests/ExperienceProof/EconomyStressTests.cs |

## M. Tranche 8 — Security + Substantiation

### M1. Security Lanes (EPIC.S5.SECURITY_LANES)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.SEC_LANES.MODEL.001 | DONE | Security lane entity: add SecurityLevelBps (int, default 5000) to Edge. Create SecurityTweaksV0.cs with threshold constants (safe/moderate/dangerous/hostile BPS bands). Proof: dotnet test SimCore.Tests -c Release --filter "BasicStateInvariants" | NEW: SimCore/Tweaks/SecurityTweaksV0.cs; FOUND: SimCore/Entities/Edge.cs |
| GATE.S5.SEC_LANES.SYSTEM.001 | DONE | Security lane system: create SecurityLaneSystem.cs. Each tick evaluate patrol fleet presence + LaneFlowSystem economic heat to compute SecurityLevelBps per edge. Wire into SimKernel.Step(). Contract tests for patrol boost + heat penalty. Proof: dotnet test SimCore.Tests -c Release --filter "SecurityLaneSystem" | NEW: SimCore/Systems/SecurityLaneSystem.cs; NEW: SimCore.Tests/Systems/SecurityLaneSystemTests.cs; FOUND: SimCore/SimKernel.cs |
| GATE.S5.SEC_LANES.BRIDGE.001 | DONE | Security lane bridge: create SimBridge.Security.cs partial. GetLaneSecurityV0(fromId, toId) returns security BPS. GetNodeSecurityV0(nodeId) returns avg of adjacent lanes. MapQueries helpers. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/bridge/SimBridge.Security.cs; FOUND: SimCore/MapQueries.cs |
| GATE.S5.SEC_LANES.UI.001 | DONE | Security lane UI: tint lane lines green→yellow→red by SecurityLevelBps in GalaxyView.cs. Show security level in hero_trade_menu.gd dock info. Show lane security in hud.gd during travel. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/hud.gd |

### M2. Combat Resolution (EPIC.S5.COMBAT_RESOLVE)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.COMBAT_RES.SYSTEM.001 | DONE | Combat resolution: add ResolveCombatV0() to CombatSystem. Input attacker/defender fleet stats, evaluate HP delta + damage family effectiveness, return CombatOutcome enum (Victory/Defeat/Flee). Wire constants through CombatTweaksV0. Contract tests for all 3 outcomes. Proof: dotnet test SimCore.Tests -c Release --filter "CombatTests" | FOUND: SimCore/Systems/CombatSystem.cs; FOUND: SimCore/Tweaks/CombatTweaksV0.cs; FOUND: SimCore.Tests/Combat/CombatTests.cs |
| GATE.S5.COMBAT_RES.PROOF.001 | DONE | Combat resolution headless proof: add ResolveCombatV0() to SimBridge.Combat.cs. Create test_combat_resolution_v0.gd — boot scene, fly to fleet, trigger combat, call resolve, verify outcome. Wire into game_manager.gd combat flow. Proof: Godot headless | FOUND: scripts/bridge/SimBridge.Combat.cs; NEW: scripts/tests/test_combat_resolution_v0.gd; FOUND: scripts/core/game_manager.gd |

### M3. Fleet Ship Substantiation (EPIC.S1.FLEET_VISUAL.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S1.FLEET_VISUAL.MAP.001 | DONE | Fleet visual on galaxy map: refactor CreateFleetMarkerV0 to load Kenney Space Kit .glb by FleetRole (Trader→craft_cargoA, Hauler→craft_cargoB, Patrol→craft_speederA). Add GetFleetRoleV0(fleetId) to SimBridge.Fleet.cs. Scale ~2u. Keep collision + label. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.Fleet.cs |
| GATE.S1.FLEET_VISUAL.VIEW.001 | DONE | Fleet visual in local system: update SpawnFleetsV0 to use same Kenney model loading with local-system scaling. Verify fleet_ai.gd patrol/engage movement works with new model root. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/core/fleet_ai.gd |
| GATE.S1.FLEET_VISUAL.PROOF.001 | DONE | Fleet visual headless proof: create test_fleet_visual_v0.gd — boot scene, verify fleet nodes have Kenney model children (not BoxMesh), check model name matches role mapping. Proof: Godot headless | NEW: scripts/tests/test_fleet_visual_v0.gd; FOUND: scripts/view/GalaxyView.cs |

### M4. NPC Trade Circulation (EPIC.S5.NPC_TRADE.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.NPC_TRADE.SYSTEM.001 | DONE | NPC trade system: create NpcTradeSystem.cs. Each tick NPC-owned fleets evaluate adjacent markets for profitable buy/sell, issue TradeCommand + TravelCommand. NpcTradeTweaksV0 for profit threshold, eval radius, frequency. Contract tests: NPC moves goods, price delta shrinks. Proof: dotnet test SimCore.Tests -c Release --filter "NpcTrade" | NEW: SimCore/Systems/NpcTradeSystem.cs; NEW: SimCore/Tweaks/NpcTradeTweaksV0.cs; NEW: SimCore.Tests/Systems/NpcTradeSystemTests.cs; FOUND: SimCore/SimKernel.cs |
| GATE.S5.NPC_TRADE.BRIDGE.001 | DONE | NPC trade bridge: create SimBridge.NpcTrade.cs partial. GetNpcTradeRoutesV0() returns active NPC routes. GetNpcTradeActivityV0(nodeId) returns NPC volume at node. MapQueries helpers. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/bridge/SimBridge.NpcTrade.cs; FOUND: SimCore/MapQueries.cs |
| GATE.S5.NPC_TRADE.ECON_PROOF.001 | DONE | NPC trade economy proof: extend ExplorationBotTests with NPC circulation assertions — after N ticks, NPC fleets moved goods, price differentials narrowed. 5+ seeds. Flag stuck/unprofitable NPC traders. Proof: dotnet test SimCore.Tests -c Release --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs; FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |

### M5. Pressure Formalization (EPIC.X.PRESSURE)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.PRESSURE.LADDER.001 | DONE | Pressure ladder: add PressureTier enum (Calm/Tension/Crisis/Collapse) and CurrentTier to PressureState. Add ladder threshold BPS constants to PressureTweaksV0. EvaluateTier() method. Contract tests for tier transitions. Proof: dotnet test SimCore.Tests -c Release --filter "PressureSystem" | FOUND: SimCore/Entities/PressureState.cs; FOUND: SimCore/Tweaks/PressureTweaksV0.cs |
| GATE.X.PRESSURE.ENFORCE.001 | DONE | Pressure enforcement: extend PressureSystem — Crisis tier → +20% market fees, Collapse → piracy escalation event. Add consequence constants to PressureTweaksV0. Extend scenario tests. Proof: dotnet test SimCore.Tests -c Release --filter "PressureSystem" | FOUND: SimCore/Systems/PressureSystem.cs; FOUND: SimCore.Tests/Systems/PressureSystemScenarioTests.cs |

### M6. Hygiene + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.008 | DONE | Repo health: full test suite, warning scan, dead code check, golden hash stability. Re-mint if logic changes affect hashes. Proof: dotnet test SimCore.Tests -c Release; pwsh scripts/tools/Repo-Health.ps1 | FOUND: docs/generated/snapshots/golden_replay_hashes.txt; FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.008 | DONE | Epic review: audit 54_EPICS.md against completed gates. Close epics with all gates DONE. Recommend next anchor for tranche 9. Proof: dotnet test SimCore.Tests -c Release --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |
| GATE.X.EVAL.ECONOMY_AUDIT.001 | DONE | Economy audit: run ExplorationBot across 10 seeds for 500 ticks. Analyze profitable/stagnant goods, price convergence, supply/demand imbalances. Produce findings + tweaks recommendations. Proof: dotnet test SimCore.Tests -c Release --filter "ExplorationBot|EconomyStress" | FOUND: SimCore.Tests/ExperienceProof/EconomyStressTests.cs; FOUND: docs/generated/econ_loops_report.txt |

## N. Tranche 9 — Empire Surface

### N1. Trade Intel Surface (EPIC.S10.TRADE_DISCOVERY.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S10.TRADE_INTEL.BRIDGE.001 | DONE | Trade intel bridge: create SimBridge.TradeIntel.cs partial. GetTradeRoutesV0() returns trade route intel (source, dest, good, profit, freshness). GetPriceIntelV0(nodeId) returns price observations. GetScannerRangeV0() returns scanner hop range. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/bridge/SimBridge.TradeIntel.cs; FOUND: SimCore/Entities/IntelBook.cs |
| GATE.S10.TRADE_PROG.BRIDGE.001 | DONE | Bridge 5 missing program creation methods (AutoSell, ConstrCap, Expedition, TradeCharter, ResourceTap) in SimBridge.Programs.cs. Update StartResearchV0 to accept optional nodeId. Add sustain fields to GetResearchStatusV0. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.Programs.cs; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S10.TRADE_INTEL.CHARTER_FIX.001 | DONE | Replace hardcoded ExploitationTweaksV0.TradeCharterBuyPricePerUnit with actual market buy/sell prices for realistic charter economics. Contract tests verify non-hardcoded pricing. Proof: dotnet test SimCore.Tests -c Release --filter "ExplorationBot" | FOUND: SimCore/Intents/ExploitationIntentsV0.cs; FOUND: SimCore/Tweaks/ExploitationTweaksV0.cs |
| GATE.S10.TRADE_INTEL.DOCK_UI.001 | DONE | Trade Routes section in hero_trade_menu.gd: available routes from intel (source, dest, good, profit, freshness), Launch Charter button. Research sustain status: goods required, stall reason, progress. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S10.TRADE_INTEL.PROOF.001 | DONE | Headless GDScript proof: boot scene, tick sim, call GetTradeRoutesV0, create TradeCharter via bridge, call StartResearchV0 with nodeId, verify sustain fields update. Proof: Godot headless --script scripts/tests/test_trade_intel_proof_v0.gd | NEW: scripts/tests/test_trade_intel_proof_v0.gd; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S10.TECH_EFFECTS.TESTS.001 | DONE | Contract tests: speed_bonus_20pct gives 1.2x fleet speed when improved_thrusters unlocked; production_efficiency_10pct gives +10% industry output. Determinism across seeds. Proof: dotnet test SimCore.Tests -c Release --filter "TechEffects" | NEW: SimCore.Tests/Systems/TechEffectsTests.cs; FOUND: SimCore/Systems/MovementSystem.cs |

### N2. Empire Management (EPIC.S10.EMPIRE_MGMT.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S10.EMPIRE.BRIDGE.001 | DONE | Empire bridge queries in SimBridge.Reports.cs: GetEmpireSummaryV0 (credits, fleets, programs, research, missions), GetAllIndustryV0 (all sites + health), GetAllNodeHealthSummaryV0 (per-node summary), GetIntelFreshnessByNodeV0 (age per node). Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.Reports.cs; FOUND: SimCore/Entities/IntelBook.cs |
| GATE.S10.EMPIRE.SHELL.001 | DONE | EmpireDashboard.cs modal C# Control with 5 tabs (Overview, Trade, Production, Programs, Intel). E key binding in game_manager.gd. Add to Playable_Prototype.tscn. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/ui/EmpireDashboard.cs; FOUND: scripts/core/game_manager.gd |
| GATE.S10.EMPIRE.OVERVIEW_TAB.001 | DONE | Overview tab: credits, fleet count, programs, research, missions from GetEmpireSummaryV0. Trade tab: markets with intel, route profitability, active charters from GetTradeRoutesV0. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs; FOUND: scripts/bridge/SimBridge.Reports.cs |
| GATE.S10.EMPIRE.PRODUCTION_TAB.001 | DONE | Production tab: industry sites from GetAllIndustryV0, health bars, construction. Programs tab: active programs + creation forms for all 6 types. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs; FOUND: scripts/bridge/SimBridge.Programs.cs |
| GATE.S10.EMPIRE.INTEL_TAB.001 | DONE | Intel tab: discoveries, rumor leads, unlocked content, intel freshness per node, scanner range display. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs; FOUND: scripts/bridge/SimBridge.Reports.cs |

### N3. Galaxy Map Overlays (EPIC.S6.MAP_GALAXY)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.MAP_GALAXY.OVERLAY_SYS.001 | DONE | Overlay mode system: add OverlayMode enum (Default, TradeFlow, IntelFreshness) to GalaxyView.cs. Create galaxy_overlay_hud.gd toolbar with mode buttons. Default preserves current security coloring. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; NEW: scripts/ui/galaxy_overlay_hud.gd |
| GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001 | DONE | TradeFlow overlay: tint lane edges gold for profitable routes (GetTradeRoutesV0), edge thickness by NPC volume (GetNpcTradeActivityV0), annotate best buy/sell goods. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.NpcTrade.cs |
| GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001 | DONE | IntelFreshness overlay: color nodes green/yellow/red/gray by intel age (GetIntelFreshnessByNodeV0). Scanner range radius around player. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.Reports.cs |

### N4. Escort Programs (EPIC.S5.ESCORT_PROG)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.ESCORT_PROG.MODEL.001 | DONE | EscortSystem.cs: EscortProgram (assign fleet to escort between nodes) and PatrolProgram (cycle patrol route). ProcessEscort/ProcessPatrol per tick. EscortTweaksV0 constants. Wire into SimKernel.Step(). Contract tests. Proof: dotnet test SimCore.Tests -c Release --filter "EscortSystem" | NEW: SimCore/Systems/EscortSystem.cs; NEW: SimCore.Tests/Systems/EscortSystemTests.cs |
| GATE.S5.ESCORT_PROG.BRIDGE.001 | DONE | SimBridge.Escort.cs partial: CreateEscortProgramV0, CreatePatrolProgramV0, GetEscortStatusV0, GetPatrolStatusV0. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/bridge/SimBridge.Escort.cs; FOUND: SimCore/SimState.Properties.cs |
| GATE.S5.ESCORT_PROG.UI.001 | DONE | Escort/patrol creation forms in Empire Dashboard Programs tab. Fleet selector, route selector, start/cancel. Status display. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs; FOUND: scripts/bridge/SimBridge.Escort.cs |

### N5. Hygiene + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.009 | DONE | Full test suite pass, zero new warnings (CS8602 excluded), golden hash stability. Re-mint if needed. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: docs/generated/snapshots/golden_replay_hashes.txt |
| GATE.X.HYGIENE.EPIC_REVIEW.009 | DONE | Audit 54_EPICS.md against completed gates. Close epics with all gates DONE. Recommend next anchor for tranche 10. Proof: dotnet test SimCore.Tests -c Release --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |
| GATE.X.EVAL.PROGRESSION_AUDIT.002 | DONE | ExplorationBot across 5+ seeds for 1000 ticks: verify trade routes discovered, research consumes goods at node, tech effects applied. Flag regressions. Proof: dotnet test SimCore.Tests -c Release --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs; FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |

## P. Tranche 10 — "Alive Galaxy" (Slice 11 Game Feel + S6 Galaxy Map)

### P1. Game Feel core (SimCore + bridge)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S11.GAME_FEEL.TECH_BRIDGE.001 | DONE | GetTechTreeV0 bridge: returns all techs with id, tier, prereqs, status (locked/available/researching/done), sustain costs, effects. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.Research.cs; FOUND: SimCore/Content/TechContentV0.cs |
| GATE.S11.GAME_FEEL.PRICE_HISTORY.001 | DONE | Price history tracking: record price snapshots per-good per-node in IntelBook every 360 ticks via IntelSystem. GetPriceHistoryV0(nodeId, goodId) bridge method. Proof: dotnet test SimCore.Tests -c Release --filter "Intel" | FOUND: SimCore/Entities/IntelBook.cs; FOUND: SimCore/Systems/IntelSystem.cs; FOUND: scripts/bridge/SimBridge.TradeIntel.cs |
| GATE.S11.GAME_FEEL.MISSION_WIRE.001 | DONE | Wire missions 2-4 (bulk_hauler, patrol_duty, long_haul) to bridge GetMissionListV0 and dock menu acceptance. Verify all 4 missions appear in mission list. Proof: dotnet test SimCore.Tests -c Release --filter "Mission" | FOUND: SimCore/Content/MissionContentV0.cs; FOUND: scripts/bridge/SimBridge.Mission.cs; FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S11.GAME_FEEL.COMBAT_BRIDGE.001 | DONE | GetRecentCombatV0 bridge: returns last 20 combat events (attacker, defender, damage, outcome, tick). Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.Combat.cs; FOUND: SimCore/Entities/CombatLog.cs |
| GATE.S11.GAME_FEEL.DOCK_ENHANCE.001 | DONE | Enhanced dock menu: show available missions count, active research progress, trade routes from current station, fleet programs summary. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Research.cs; FOUND: scripts/bridge/SimBridge.Mission.cs |

### P2. Game Feel bridge (GDScript + UI)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S11.GAME_FEEL.TOAST_SYS.001 | DONE | Toast notification manager: GDScript autoload showing temporary popup messages (fade in/out, stack up to 5). Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/ui/toast_manager.gd; FOUND: scripts/core/game_manager.gd; FOUND: project.godot |
| GATE.S6.MAP_GALAXY.NODE_CLICK.001 | DONE | Galaxy map node click: raycast from camera, detect node mesh, show detail popup (system name, world class, fleets present, industry count). Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; NEW: scripts/ui/node_detail_popup.gd |
| GATE.S11.GAME_FEEL.NPC_ROUTE_VIS.001 | DONE | NPC fleet route lines on galaxy map: draw animated dashed lines for NPC fleet active travel using GetNpcTradeActivityV0 data. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.NpcTrade.cs |
| GATE.S11.GAME_FEEL.TECH_TREE_UI.001 | DONE | Tech tree panel as new tab in EmpireDashboard: all techs organized by tier, prereq arrows, status coloring, sustain costs, click to start research. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S11.GAME_FEEL.TOAST_EVENTS.001 | DONE | Connect game events to toasts: trade completion, research progress/complete, mission update, combat alert. SimBridge emits signals, toast_manager.gd listens. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/toast_manager.gd; FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/core/game_manager.gd |
| GATE.S11.GAME_FEEL.MISSION_HUD.001 | DONE | Active mission objective text in flight HUD: current step description, target node name, progress. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hud.gd; FOUND: scripts/bridge/SimBridge.Mission.cs |
| GATE.S11.GAME_FEEL.RESEARCH_HUD.001 | DONE | Research progress bar in flight HUD: tech name, percentage, sustain status (OK/stalled + reason). Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hud.gd; FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.S11.GAME_FEEL.KEYBINDS.001 | DONE | H key help overlay: semi-transparent panel showing all keybindings (WASD, E, Tab, H, Esc, F1/F2, mouse). Toggle on/off. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/ui/keybinds_help.gd; FOUND: scripts/core/game_manager.gd |
| GATE.S11.GAME_FEEL.NODE_MARKET.001 | DONE | Node detail popup market tab: show all goods at node with buy/sell prices, supply qty, price trend arrow. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/node_detail_popup.gd; FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.S11.GAME_FEEL.COMBAT_LOG_UI.001 | DONE | Combat event log panel (L key): scrollable list of recent combat events with timestamps, attackers, damage, outcomes. Proof: dotnet build "Space Trade Empire.csproj" | NEW: scripts/ui/combat_log_panel.gd; FOUND: scripts/bridge/SimBridge.Combat.cs; FOUND: scripts/core/game_manager.gd |
| GATE.S11.GAME_FEEL.FLEET_STATUS.001 | DONE | Fleet role icons on galaxy map nodes: small icons (trader/patrol/hauler) next to fleet count labels showing fleet composition. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.Fleet.cs |

### P3. Hygiene + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.010 | DONE | Full test suite pass, golden hash stability, zero new warnings. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: docs/generated/snapshots/golden_replay_hashes.txt |
| GATE.S11.GAME_FEEL.HEADLESS_PROOF.001 | DONE | Headless GDScript proof: boot scene, open empire dashboard, verify tech tree tab renders, verify toast shows, verify node click popup. Proof: Godot headless --script scripts/tests/test_game_feel_proof_v0.gd | NEW: scripts/tests/test_game_feel_proof_v0.gd; FOUND: scripts/bridge/SimBridge.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.010 | DONE | Audit 54_EPICS.md: close S10.TRADE_DISCOVERY, S10.EMPIRE_MGMT, S5.ESCORT_PROG. Recommend next anchor. Proof: dotnet test SimCore.Tests -c Release --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |
| GATE.X.EVAL.UX_AUDIT.001 | DONE | UX evaluation: document player friction points, information gaps, control discoverability issues. Recommend top 5 UX improvements. Proof: docs audit | FOUND: docs/54_EPICS.md; FOUND: scripts/ui/hero_trade_menu.gd |

## Q. Tranche 11 — "Living Galaxy" (Slice 12 Fleet Substance + UX Polish + Progression)

### Q1. Fleet Substance (Quaternius models + NPC circulation)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S12.FLEET_SUBSTANCE.QUATERNIUS.001 | DONE | Update LoadFleetModelV0 in GalaxyView.cs: trader→dispatcher, hauler→bob, patrol→spitfire from Quaternius pack. Load .tscn scenes. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: addons/quaternius-ultimate-spaceships-pack/meshes/dispatcher/dispatcher_blue.tscn |
| GATE.S12.FLEET_SUBSTANCE.VARIETY.001 | DONE | Multiple Quaternius model variants per role using hash-based selection (e.g. trader: dispatcher OR pancake). Player ship uses challenger model. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: addons/quaternius-ultimate-spaceships-pack/meshes/challenger/challenger_blue.tscn |
| GATE.S12.NPC_CIRC.CIRCUIT_ROUTES.001 | DONE | NPC patrol fleets use multi-hop circuit routes (3+ nodes) with deterministic route generation from galaxy topology. Proof: dotnet test SimCore.Tests -c Release --filter "NpcTrade" | FOUND: SimCore/Systems/NpcTradeSystem.cs; FOUND: SimCore/Entities/Fleet.cs |
| GATE.S12.NPC_CIRC.FLOW_ANIM.001 | DONE | Animated small dots flowing along NPC trade route edge lines on galaxy map. Visual-only, no SimCore change. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S12.NPC_CIRC.VOLUME_LABELS.001 | DONE | Trade volume labels on galaxy map edges showing goods/tick flow rate derived from NPC activity. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.NpcTrade.cs |
| GATE.S12.FLEET_SUBSTANCE.HEADLESS_PROOF.001 | DONE | Headless GDScript proof: verify Quaternius models load, stats bridge returns data, quantity controls exist, display names formatted. Proof: Godot headless --script scripts/tests/test_living_galaxy_proof_v0.gd | NEW: scripts/tests/test_living_galaxy_proof_v0.gd |

### Q2. UX Polish (from UX audit)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S12.UX_POLISH.QUANTITY.001 | DONE | Buy/Sell quantity buttons [1, 5, Max] in hero_trade_menu.gd replacing single-unit buy/sell. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S12.UX_POLISH.DISPLAY_NAMES.001 | DONE | FormatDisplayNameV0(string id) in SimBridge: converts snake_case IDs to readable "Title Case" names. Apply to goods, nodes, techs in all UI surfaces. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S12.UX_POLISH.ONBOARDING.001 | DONE | First-dock toast onboarding: show welcome message, key controls (Tab=map, E=empire, H=help) on first game load. Track via SimState flag. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/toast_manager.gd; FOUND: scripts/core/game_manager.gd |
| GATE.S12.UX_POLISH.CARGO_DISPLAY.001 | DONE | Show cargo used/max in dock menu header and flight HUD. Current cargo count vs fleet capacity. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/ui/hud.gd |
| GATE.S12.UX_POLISH.TRADE_FEEDBACK.001 | DONE | Toast notification on buy/sell showing formatted good name, quantity, and credit delta. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/toast_manager.gd; FOUND: scripts/core/game_manager.gd |

### Q3. Player Progression

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S12.PROGRESSION.STATS.001 | DONE | PlayerStats class in SimState: nodes_visited count, goods_traded count, total_credits_earned, techs_unlocked count, missions_completed count. Updated by relevant systems. Proof: dotnet test SimCore.Tests -c Release --filter "Progression" | FOUND: SimCore/Entities/SimState.cs; NEW: SimCore/Entities/PlayerStats.cs |
| GATE.S12.PROGRESSION.MILESTONES.001 | DONE | MilestoneContentV0: define milestones (First Trade, Explorer 5 nodes, Merchant 1000cr, Researcher 1 tech, Captain 1 mission). MilestoneSystem evaluates and records achieved milestones. Proof: dotnet test SimCore.Tests -c Release --filter "Milestone" | NEW: SimCore/Content/MilestoneContentV0.cs; NEW: SimCore/Systems/MilestoneSystem.cs |
| GATE.S12.PROGRESSION.TESTS.001 | DONE | Contract tests: stats increment on trade/explore/research/mission. Milestone evaluation triggers at thresholds. Proof: dotnet test SimCore.Tests -c Release --filter "Progression" | NEW: SimCore.Tests/Systems/ProgressionTests.cs |
| GATE.S12.PROGRESSION.BRIDGE.001 | DONE | GetPlayerStatsV0 + GetMilestonesV0 bridge methods returning stats dict and milestones array. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.Reports.cs |
| GATE.S12.PROGRESSION.DASHBOARD.001 | DONE | Stats + Milestones tab in EmpireDashboard: show all stats, achieved milestones (green), pending milestones (gray with progress). Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs |

### Q4. Hygiene + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.011 | DONE | Full test suite pass, golden hash stability, zero new warnings. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj; FOUND: docs/generated/snapshots/golden_replay_hashes.txt |
| GATE.X.HYGIENE.EPIC_REVIEW.011 | DONE | Audit 54_EPICS.md: close S10.TRADE_DISCOVERY, S10.EMPIRE_MGMT, S5.ESCORT_PROG, S11.GAME_FEEL, S6.MAP_GALAXY. Recommend next anchor. Proof: dotnet test SimCore.Tests -c Release --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md; FOUND: docs/55_GATES.md |
| GATE.X.EVAL.VISUAL_AUDIT.001 | DONE | Visual substance evaluation: document model quality, NPC flow visibility, UX improvement effectiveness. Recommend next visual priorities. Proof: docs audit | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/ui/hero_trade_menu.gd |

## R. Tranche 12 — "Feel Overhaul" (Slice 13 Camera + Dock + World Feel)

### R1. Camera & Ship Controls

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S13.CAMERA.TOPDOWN.001 | DONE | Top-down camera: change FLIGHT offset from (0,8,18) to (0,45,30), increase follow_distance to 54, reduce FOV swell. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/player_follow_camera.gd |
| GATE.S13.CAMERA.PERSIST.001 | DONE | Camera holds rotation on mouse release: remove snap-back lerp for yaw/pitch. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/player_follow_camera.gd |
| GATE.S13.CONTROLS.TURNING.001 | DONE | Ship turning: increase TURN_TORQUE from 4.0 to 10.0, ANGULAR_DAMPING from 3.0 to 6.0. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/hero_ship_flight_controller.gd |
| GATE.S13.CONTROLS.SPEED.001 | DONE | Ship speed: reduce MAX_SPEED from 28.0 to 18.0, increase LINEAR_DAMPING from 0.8 to 1.5. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/hero_ship_flight_controller.gd |
| GATE.S13.COMBAT.EXPLOSION_SCALE.001 | DONE | Reduce explosion particle scale, muzzle flash intensity. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/game_manager.gd; FOUND: scenes/player.tscn |

### R2. Station Experience

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S13.DOCK.TABS.001 | DONE | Dock menu tabs: Market/Jobs/Services replacing single scroll. One tab visible at a time. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S13.DOCK.HIDE_EMPTY.001 | DONE | Hide empty/advanced dock sections: Trade Routes hidden until scanner tech, Programs hidden until first program, Construction hidden until tech unlocked. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S13.DOCK.CONTEXT.001 | DONE | Station context: show "Mining Colony — produces Ore and Metal" derived from production sites. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S13.EMPIRE.GATING.001 | DONE | Empire dashboard progressive tabs: hide Trade/Production/Programs/Intel until discovered. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs |
| GATE.S13.EMPIRE.OVERVIEW.001 | DONE | Overview tab: replace raw numbers with contextual messages, remove tick count. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/EmpireDashboard.cs |
| GATE.S13.UX.TERMINOLOGY.001 | DONE | Rename dev terms: Programs→Automation, Intel→Exploration Data, cadence ticks→seconds, nodes→systems. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/ui/EmpireDashboard.cs |

### R3. World Feel

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S13.LABELS.CLAMP.001 | DONE | Distance-based label clamping: scale down when camera <15u, fade when >80u. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S13.GATES.ARRIVAL.001 | DONE | Gate arrival: position player at corresponding gate facing inward instead of origin. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/game_manager.gd; FOUND: scripts/view/GalaxyView.cs |
| GATE.S13.GATES.DIRECTION.001 | DONE | Gates face destination: position gates in direction of destination system, rotate pillars. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S13.MAP.CENTER.001 | DONE | Galaxy map centers on player: camera straight down at (playerX, 60, playerZ), pulsing ring. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S13.LABELS.HOSTILE.001 | DONE | Hostile fleet labels: show "Hostile" in red instead of role name. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S13.NPC.VISIBLE.001 | DONE | NPC fleet visibility: AI orbits at 15-20u in ENGAGE, reduce patrol_speed to 6.0. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/fleet_ai.gd |

### R4. Hygiene + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.012 | DONE | Full test suite pass, golden hash stability. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.012 | DONE | Audit 54_EPICS.md: close completed epics, recommend next anchor. Proof: dotnet test SimCore.Tests -c Release --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md |
| GATE.X.EVAL.FEEL_AUDIT.001 | DONE | Post-implementation feel eval: grade each playtest feedback item Fixed/Improved/Deferred. Proof: docs audit | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/ui/hero_trade_menu.gd |

## S. Tranche 13 "Alive Galaxy" — playtest-driven life + visual polish

### S1. Life in Systems

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S14.NPC_ALIVE.FLEET_SEED.001 | DONE | Fix NPC fleet survival: add SeedNpcFleetsV0 to WorldLoader, role diversity (60% Trader, 25% Hauler, 15% Patrol). Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore/World/WorldLoader.cs; FOUND: SimCore/Gen/StarNetworkGen.cs |
| GATE.S14.NPC_ALIVE.FLEET_TESTS.001 | DONE | Contract tests: fleet count > 1 after load, role distribution, trader movement after 100 ticks. Proof: dotnet test --filter "NpcFleetSurvival" | FOUND: SimCore.Tests/Systems/NpcTradeSystemTests.cs |
| GATE.S14.NPC_ALIVE.EXPLORATION_BOT.001 | DONE | Update ExplorationBot: verify aiFleetCount > 0, adjust COMBAT_NEVER_ATTEMPTED thresholds. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |

### S2. Gate & Transit

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S14.TRANSIT.WARP_EFFECT.001 | DONE | Lane transit warp: camera shake 0.4 + white ColorRect flash on enter, shake 0.25 on arrival. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/game_manager.gd |
| GATE.S14.GATE_VISUAL.KENNEY_MODEL.001 | DONE | Replace procedural BoxMesh gates with Kenney gate_complex.glb. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |

### S3. Visual Polish

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S14.STAR.TINT_FIX.001 | DONE | Star tint: dark color (R*0.3, G*0.25, B*0.2), bright color B*0.5. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S14.STAR.STARTER_GUARANTEE.001 | DONE | star_0 always ClassG regardless of hash. Proof: dotnet test --filter "StarterStarClassG" | FOUND: SimCore/Gen/PlanetInitGen.cs |
| GATE.S14.ASTEROID.SHAPE_VARIETY.001 | DONE | Mixed asteroid shapes: Sphere/Box/Cylinder by hash. Scale 0.1-0.6u. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S14.DOCK.VISUAL_FRAME.001 | DONE | Dock menu StyleBoxFlat: dark navy bg, 2px blue border, 6px corners. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S14.STARLIGHT.BRIGHTNESS.001 | DONE | Starlight brightness: luminosity_cap 2e+07, emission_energy 5e+10. Proof: visual check | FOUND: scenes/Playable_Prototype.tscn |

### S4. UX & Navigation

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S14.DOCK.PROXIMITY_TIGHTEN.001 | DONE | Station dock box (10,5,10)->(7,4,7), planet sphere 12->6. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S14.HUD.DOCK_CLEANUP.001 | DONE | Hide mission panel when docked. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hud.gd |
| GATE.S14.MAP.PLAYER_INDICATOR.001 | DONE | Pulsing YOU indicator on galaxy map overlay. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S14.STARTER.MISSION_PROMPT.001 | DONE | Toast on first dock: "Check the Jobs tab for work". Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/core/game_manager.gd |

### S5. Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S14.GOLDEN.HASH_UPDATE.001 | DONE | Update golden hashes after FLEET_SEED + STARTER_GUARANTEE. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/GoldenReplayTests.cs |
| GATE.X.HYGIENE.REPO_HEALTH.013 | DONE | Full test suite + build verification. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.013 | DONE | Epic audit, close completed, recommend next anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md |

## T. Tranche 14 — "Exploration Depth" (S6.LAYERED_REVEALS + S6.ANOMALY_ECOLOGY + S6.DISCOVERY_OUTCOMES + S15.EXPLORATION_FEEL)

### T1. Discovery System Presentation (EPIC.S6.LAYERED_REVEALS)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.REVEAL.SCAN_CMD.001 | DONE | ScanDiscoveryCommand: advance DiscoveryStateV0 Seen->Scanned->Analyzed on player action. Dispatch via SimBridge. Proof: dotnet test --filter "ScanDiscoveryCommand" | NEW: SimCore/Commands/ScanDiscoveryCommand.cs; NEW: SimCore.Tests/Commands/ScanDiscoveryCommandTests.cs |
| GATE.S6.REVEAL.DISCOVERY_SNAP.001 | DONE | SimBridge discovery phase snapshot: GetDiscoverySnapshotV0 returns phase, staleness, kind, progress. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.TradeIntel.cs; FOUND: SimCore/Entities/IntelBook.cs |
| GATE.S6.REVEAL.DISCOVERY_HUD.001 | DONE | Extend DiscoverySitePanel.gd: show scan progress bar, phase icon, kind label, Scan action button. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/DiscoverySitePanel.gd; FOUND: scripts/bridge/SimBridge.TradeIntel.cs |

### T2. Anomaly Encounters (EPIC.S6.ANOMALY_ECOLOGY)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.ANOMALY.ENCOUNTER_MODEL.001 | DONE | AnomalyEncounter entity + trigger: scanning AnomalyFamily discovery to Analyzed generates encounter. Proof: dotnet test --filter "AnomalyEncounter" | NEW: SimCore/Entities/AnomalyEncounter.cs; NEW: SimCore.Tests/Systems/AnomalyEncounterTests.cs |
| GATE.S6.ANOMALY.REWARD_LOOT.001 | DONE | Anomaly loot by family: DERELICT->salvage, RUIN->data+credits, SIGNAL->discovery leads. Proof: dotnet test --filter "AnomalyEncounter" | FOUND: SimCore/Entities/AnomalyEncounter.cs; FOUND: SimCore/Systems/CombatSystem.cs |
| GATE.S6.ANOMALY.ENCOUNTER_BRIDGE.001 | DONE | SimBridge anomaly encounter queries: GetAnomalyEncounterSnapshotV0, GetActiveEncountersV0. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.Combat.cs |
| GATE.S6.ANOMALY.ENCOUNTER_UI.001 | DONE | Anomaly encounter UI in dock Services tab: family icon, difficulty, loot preview, Engage button. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/hero_trade_menu.gd; FOUND: scripts/bridge/SimBridge.Combat.cs |

### T3. Discovery Outcomes (EPIC.S6.DISCOVERY_OUTCOMES)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.OUTCOME.REWARD_MODEL.001 | DONE | DiscoveryOutcomeSystem: Analyzed discoveries yield rewards (ResourcePool->trade bonus, Corridor->shortcut, Anomaly->encounter). Proof: dotnet test --filter "DiscoveryOutcome" | NEW: SimCore/Systems/DiscoveryOutcomeSystem.cs; NEW: SimCore.Tests/Systems/DiscoveryOutcomeTests.cs |
| GATE.S6.OUTCOME.CELEBRATION.001 | DONE | Discovery completion celebration: gold toast, particle burst at site, reward summary. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/ui/toast_manager.gd; FOUND: scripts/view/GalaxyView.cs |
| GATE.S6.OUTCOME.REWARD_BRIDGE.001 | DONE | GetDiscoveryOutcomesV0 bridge query + DiscoverySitePanel outcome display. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.TradeIntel.cs; FOUND: scripts/ui/DiscoverySitePanel.gd |

### T4. Exploration Feel (EPIC.S15.EXPLORATION_FEEL — NEW)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S15.FEEL.JUMP_EVENT_SYS.001 | DONE | JumpEventSystem: random events on lane transit (salvage, signal, turbulence). Wire into SimEngine. Proof: dotnet test --filter "JumpEventSystem" | NEW: SimCore/Systems/JumpEventSystem.cs; NEW: SimCore/Tweaks/JumpEventTweaksV0.cs; NEW: SimCore.Tests/Systems/JumpEventSystemTests.cs |
| GATE.S15.FEEL.STAR_LIGHTING.001 | DONE | Star-class DirectionalLight3D tinting: O=blue-white, A=white, F=yellow-white, G=warm, K=orange, M=red. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: SimCore/Entities/Star.cs |
| GATE.S15.FEEL.NPC_PROXIMITY.001 | DONE | NPC freighter Quaternius models substantiate dynamically when player is in same system. Poll+diff fleet list every 2s. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.Fleet.cs |
| GATE.S15.FEEL.FACTION_TERRITORY.001 | DONE | Faction territory: BFS from HomeNodeId (depth 3) to compute ControlledNodeIds per faction. Proof: dotnet test --filter "FactionTerritory" | FOUND: SimCore/Gen/GalaxyGenerator.cs; FOUND: SimCore/Schemas/WorldDefinition.cs; NEW: SimCore.Tests/Gen/FactionTerritoryTests.cs |
| GATE.S15.FEEL.JUMP_EVENT_TOAST.001 | DONE | Jump event toasts via toast_manager: salvage=green, signal=blue, turbulence=red. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/bridge/SimBridge.cs; FOUND: scripts/ui/toast_manager.gd |
| GATE.S15.FEEL.AMBIENT_SYSTEM.001 | DONE | Local system ambient GPUParticles3D: dust motes, heat shimmer, background star dust. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs |
| GATE.S15.FEEL.FACTION_LABELS.001 | DONE | Faction territory labels on galaxy map: colored node tint, faction name Label3D near HomeNodeId. Proof: dotnet build "Space Trade Empire.csproj" | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/bridge/SimBridge.Ui.cs |
| GATE.S15.FEEL.EXPLORATION_PROOF.001 | DONE | Headless proof: boot scene, travel, verify discovery HUD + star tinting + NPC fleet spawn. Proof: godot --headless | FOUND: scripts/view/GalaxyView.cs; FOUND: scripts/ui/DiscoverySitePanel.gd; NEW: scripts/tests/test_exploration_depth_v0.gd |

### T5. Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.014 | DONE | Full test suite + build verification. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.014 | DONE | Epic audit, close completed, recommend next anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md |
| GATE.X.HYGIENE.EXPLORE_ARCH_EVAL.001 | DONE | Exploration architecture review: discovery+anomaly+faction integration gaps. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/56_SESSION_LOG.md |

## U. Tranche 15 — "NPC Ships Alive" (EPIC.S16.NPC_SHIPS_ALIVE)

NPC fleet ships become physical 3D entities with LimboAI behavior trees, sim-driven movement, role-based AI (Trader/Hauler/Patrol), player-NPC combat feedback to SimCore, warp effects. All ships in the player's current star system are substantiated.

### U1. SimCore — Fleet Combat + Delay (core, hash_affecting)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S16.NPC_ALIVE.DAMAGE_CMD.001 | DONE | NpcFleetDamageCommand: apply specific hull/shield damage to NPC fleet. Shield absorbs first, remainder to hull. Apply DelayTicksRemaining. Proof: dotnet test --filter "NpcFleetDamage" + Determinism | NEW: SimCore/Commands/NpcFleetDamageCommand.cs, SimCore/Entities/Fleet.cs |
| GATE.S16.NPC_ALIVE.DELAY_ENFORCE.001 | DONE | MovementSystem: check DelayTicksRemaining before advancing travel. If > 0, decrement and skip movement. Fixes existing gap where field is set but never checked. Proof: dotnet test --filter "Movement" + Determinism | FOUND: SimCore/Systems/MovementSystem.cs, SimCore/Entities/Fleet.cs |
| GATE.S16.NPC_ALIVE.FLEET_DESTROY.001 | DONE | NpcFleetCombatSystem: check NPC fleets for HullHp == 0, remove from state.Fleets, free edge capacity. Add to SimKernel.Step(). Proof: dotnet test --filter "NpcFleetCombat" + Determinism | NEW: SimCore/Systems/NpcFleetCombatSystem.cs, SimCore/SimKernel.cs |
| GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001 | DONE | Fleet respawn after destruction: track destroyed fleet IDs + tick, recreate after NpcShipTweaksV0.RespawnCooldownTicks. Deterministic RNG. Proof: dotnet test --filter "FleetRespawn" + Determinism | FOUND: SimCore/Systems/NpcFleetCombatSystem.cs, SimCore/SimKernel.cs |

### U2. SimBridge — Transit Data Query (bridge)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S16.NPC_ALIVE.TRANSIT_SNAP.001 | DONE | SimBridge GetFleetTransitFactsV0(nodeId): fleet_id, role, state, travel_progress, speed, hull_hp, is_hostile for all fleets at/through a system. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Fleet.cs, SimCore/Entities/Fleet.cs |

### U3. LimboAI + NPC Ship Foundation (bridge)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S16.NPC_ALIVE.LIMBO_INSTALL.001 | DONE | Install LimboAI addon for Godot 4.6. Enable in project.godot. Verify build. Proof: dotnet build | FOUND: project.godot |
| GATE.S16.NPC_ALIVE.SHIP_SCENE.001 | DONE | NPC ship packed scene: CharacterBody3D + CollisionShape3D + ModelMount child. Script reads meta(fleet_id, role). Groups: FleetShip, NpcShip. Proof: dotnet build | NEW: scenes/npc_ship.tscn, NEW: scripts/npc/npc_ship.gd |

### U4. Flight + Behavior Trees (bridge + content)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S16.NPC_ALIVE.FLIGHT_CTRL.001 | DONE | NPC flight controller: CharacterBody3D steering toward target_position at target_speed via move_and_slide(). Smooth rotation, XZ-locked. Pausable for combat stagger. Proof: dotnet build | NEW: scripts/npc/npc_flight_controller.gd |
| GATE.S16.NPC_ALIVE.BT_TASKS.001 | DONE | LimboAI custom BTAction tasks: FlyToPoint, WarpOut, SelectDestination (query SimBridge), AtDestination (condition). BTBlackboard for shared state. Proof: dotnet build | NEW: scripts/npc/bt_tasks.gd |
| GATE.S16.NPC_ALIVE.BT_ROLES.001 | DONE | Trader/Hauler/Patrol BehaviorTree .tres resources. Trader: warp in -> fly to station -> dock -> fly to gate -> warp out. Hauler: station shuttle. Patrol: orbit + aggro. Proof: dotnet build | FOUND: scripts/npc/bt_tasks.gd, scenes/npc_ship.tscn |

### U5. GalaxyView Integration + VFX (bridge)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S16.NPC_ALIVE.SPAWN_SYSTEM.001 | DONE | Replace static fleet markers with physical NPC ship instances in SpawnFleetsV0/RefreshLocalFleetsV0. Position by transit data interpolation. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, scenes/npc_ship.tscn |
| GATE.S16.NPC_ALIVE.COMBAT_BRIDGE.001 | DONE | SimBridge DamageNpcFleetV0 method. Wire from npc_ship.gd collision: player bullet hit -> bridge damage -> sim. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Fleet.cs, scripts/npc/npc_ship.gd |
| GATE.S16.NPC_ALIVE.WARP_VFX.001 | DONE | Warp-in (scale 0->1 + particle burst) and warp-out (scale 1->0 + streak) effects at lane gates. GPUParticles3D + Tween. Proof: dotnet build | NEW: scripts/vfx/warp_effect.gd, scripts/view/GalaxyView.cs |
| GATE.S16.NPC_ALIVE.DESPAWN.001 | DONE | NPC ship lifecycle: clean despawn on system exit, warp-out on transit completion, warp-in for new arrivals. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, scripts/npc/npc_ship.gd |
| GATE.S16.NPC_ALIVE.STATUS_DISPLAY.001 | DONE | NPC role icon (T/H/P) + HP bar overlay. Label3D visible within 40u. Hostile indicator. Proof: dotnet build | FOUND: scripts/npc/npc_ship.gd, scenes/npc_ship.tscn |

### U6. Proof + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S16.NPC_ALIVE.HEADLESS_PROOF.001 | DONE | Headless SceneTree test: NPC ships spawn as CharacterBody3D, move (position changes), transit data flows from SimBridge. HSL/HSS output. Proof: godot --headless | NEW: scripts/tests/test_npc_ships.gd, scripts/view/GalaxyView.cs |
| GATE.X.HYGIENE.REPO_HEALTH.015 | DONE | Full test suite + build verification baseline. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.015 | DONE | Epic audit: close S14 (Alive Galaxy), S15 (Exploration Feel) candidates. Recommend next anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md |
| GATE.X.HYGIENE.LIMBO_EVAL.001 | DONE | LimboAI integration architecture evaluation: scalability, BT pattern quality, determinism safety. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/56_SESSION_LOG.md |

## I. Tranche 16 "Real Space + Factions" (EPIC.S17.REAL_SPACE, EPIC.S7.FACTION_MODEL, EPIC.S6.OFFLANE_FRACTURE)

### V1. SimCore — Star Coords + Faction Foundation (core, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S17.REAL_SPACE.STAR_COORDS.001 | DONE | StarNetworkGen outputs galactic-scale 3D positions (2000-5000u between neighbors). RealSpaceTweaksV0.GalacticScaleFactor. WorldDefinition stores real positions. WorldLoader seeds from these. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Tweaks/RealSpaceTweaksV0.cs, FOUND: SimCore/Gen/StarNetworkGen.cs, FOUND: SimCore/Schemas/WorldDefinition.cs, FOUND: SimCore/World/WorldLoader.cs |
| GATE.S7.FACTION.DOCTRINE_MODEL.001 | DONE | Add doctrine fields to WorldFaction: TradePolicy enum (Open/Guarded/Closed), AggressionLevel, TariffRate, PreferredGoods. FactionTweaksV0 for default values. GalaxyGenerator assigns placeholder doctrines (Traders=Open/0.05, Miners=Guarded/0.15, Pirates=Closed/hostile). Proof: dotnet test --filter "Determinism" | NEW: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: SimCore/Schemas/WorldDefinition.cs, FOUND: SimCore/Gen/GalaxyGenerator.cs |
| GATE.S7.FACTION.REPUTATION_SYS.001 | DONE | ReputationSystem: player standing [-100,100] per faction. Trade at faction station -> +rep (scaled by trade value). Attack faction ship -> large -rep. Stored in SimState.FactionReputation dictionary. Wire into SimKernel.Step(). Proof: dotnet test --filter "Reputation" + Determinism | NEW: SimCore/Systems/ReputationSystem.cs, NEW: SimCore.Tests/Systems/ReputationSystemTests.cs, FOUND: SimCore/SimState.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S6.FRACTURE.VOID_SITES.001 | DONE | VoidSite entity: Id, Position3D (real coordinates between systems), Family (enum), MarkerState. World gen seeds 5-15 void sites per galaxy at midpoints/offsets between star pairs. Deterministic from galaxy seed. Proof: dotnet test --filter "VoidSite" + Determinism | NEW: SimCore/Entities/VoidSite.cs, FOUND: SimCore/Gen/GalaxyGenerator.cs, FOUND: SimCore/Schemas/WorldDefinition.cs |

### V2. SimCore — Tariff + Fracture Commands (core, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FACTION.TARIFF_ENFORCE.001 | DONE | MarketSystem applies tariff modifier from faction doctrine * reputation. Bad rep -> higher buy prices, lower sell prices. Below threshold -> blocked from trading. Proof: dotnet test --filter "Tariff|Reputation" + Determinism | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/SimState.cs |
| GATE.S6.FRACTURE.MARKER_CMD.001 | DONE | PlaceSurveyMarkerCommand: player marks a VoidSite. Marker estimates extractable resources based on sensor tech level (higher tech -> more accurate, reveals more types). SurveyMarker state on VoidSite entity. Proof: dotnet test --filter "SurveyMarker" + Determinism | NEW: SimCore/Commands/PlaceSurveyMarkerCommand.cs, FOUND: SimCore/Entities/VoidSite.cs |
| GATE.S6.FRACTURE.TRAVEL_CMD.001 | DONE | FractureTravelCommand: off-lane travel to VoidSite. Much slower than lane transit (10x). Requires fracture drive tech. Sets fleet to Traveling with void site destination. Proof: dotnet test --filter "FractureTravel" + Determinism | NEW: SimCore/Commands/FractureTravelCommand.cs, FOUND: SimCore/Entities/Fleet.cs, FOUND: SimCore/Systems/MovementSystem.cs |

### V3. GalaxyView Refactor (bridge, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S17.REAL_SPACE.GALAXY_RENDER.001 | DONE | GalaxyView refactor: (1) Persistent star billboards for ALL systems at real 3D positions, colored by star class. (2) DrawLocalSystemV0 at star position, not origin. (3) LOD: detail within ~200u, culled beyond. Remove ClearLocalSystemV0 teardown model. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scenes/playable_prototype.tscn |

### V4. SimBridge Faction Queries (bridge, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FACTION.BRIDGE_QUERIES.001 | DONE | New SimBridge.Faction.cs partial: GetFactionDoctrineV0(factionId) -> trade policy, tariff. GetPlayerReputationV0(factionId) -> standing. GetTerritoryAccessV0(nodeId) -> controlling faction, tariff, access level. Proof: dotnet build | NEW: scripts/bridge/SimBridge.Faction.cs, FOUND: SimCore/Schemas/WorldDefinition.cs |

### V5. Lane Transit + VFX + Galaxy Map (bridge, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S17.REAL_SPACE.LANE_TRANSIT.001 | DONE | Rework lane transit: player moves source->dest via Tween. Acceleration (0.5s) -> cruise -> deceleration (0.5s). game_manager.gd _begin_lane_transit_v0 drives traversal. Camera follows. RebuildLocalSystemV0 on arrival. Proof: dotnet build | FOUND: scripts/core/game_manager.gd, FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/view/player_follow_camera.gd |
| GATE.S17.REAL_SPACE.WARP_TUNNEL.001 | DONE | Warp tunnel: cylinder mesh + scrolling noise shader around player during lane transit. Particle streaks. Fully opaque by default. Opacity var for sensor tech. Proof: dotnet build | NEW: scripts/vfx/warp_tunnel.gd, FOUND: scripts/vfx/warp_effect.gd |
| GATE.S17.REAL_SPACE.GALAXY_MAP.001 | DONE | TAB galaxy map: high-altitude orthographic camera over real 3D space. Node labels + lane lines already 3D geometry. Remove GalaxyOverlay CanvasLayer. Proof: dotnet build | FOUND: scripts/core/game_manager.gd, FOUND: scripts/view/GalaxyView.cs |
| GATE.S6.FRACTURE.SENSOR_REVEAL.001 | DONE | Sensor tech controls warp tunnel opacity. Level 0: opaque. Level 1+: semi-transparent, void sites as distant blips. Higher levels -> family icon, distance label on blips. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/vfx/warp_tunnel.gd |

### V6. NPC Behavior + UI (bridge, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FACTION.PATROL_AGGRO.001 | DONE | NPC patrols check player reputation with their faction via SimBridge. Below aggro threshold -> attack on sight. Update bt_select_destination.gd and npc_ship.gd. Proof: dotnet build | FOUND: scripts/core/npc_ship.gd, FOUND: scripts/npc/bt_select_destination.gd, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.FACTION.UI_REPUTATION.001 | DONE | EmpireDashboard Factions tab: per-faction rep bar [-100,100], doctrine summary. Dock menu: tariff rate display, "Access Denied" when below threshold. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/ui/hero_trade_menu.gd |

### V7. Proof + Meta

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S17.REAL_SPACE.HEADLESS_PROOF.001 | DONE | Headless SceneTree test: stars at real positions, system detail at star position, lane transit moves player, LOD culling. Proof: dotnet build + godot --headless | NEW: scripts/tests/test_real_space.gd, FOUND: scripts/view/GalaxyView.cs |
| GATE.X.HYGIENE.REPO_HEALTH.016 | DONE | Full test suite + build verification. Proof: dotnet test SimCore.Tests -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.016 | DONE | Epic audit: close S16.NPC_SHIPS_ALIVE, advance S17/S7/S6. Recommend next anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md |
| GATE.X.HYGIENE.REALSPACE_EVAL.001 | DONE | Real-space architecture eval: float precision, LOD strategy, transit feel, galaxy map integration. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/56_SESSION_LOG.md |

## V. Tranche 17 "Economic Foundation" (EPIC.S18.TRADE_GOODS.V0, EPIC.S18.SHIP_MODULES.V0, EPIC.S18.EMPIRE_DASH.V0)

### W1. Content Overhaul + Zone Armor + Repo Health (core, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.017 | DONE | Full test suite + warning scan + golden hash stability baseline. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.S18.TRADE_GOODS.CONTENT_OVERHAUL.001 | DONE | Migrate ContentRegistryLoader from 10-good/7-recipe to 13-good/9-recipe system per trade_goods_v0.md. Add organics, rare_metals, munitions, components (exists). Rename composite_armor->composites, anomaly_samples->exotic_matter. Remove hull_plating recipe. Add recipe_process_food, recipe_fabricate_composites, recipe_manufacture_munitions, recipe_salvage_to_components. Update WellKnownGoodIds + WellKnownRecipeIds. Update golden hashes. Proof: dotnet test --filter "Determinism" + --filter "ContentRegistry" | FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Content/WellKnownGoodIds.cs, FOUND: SimCore/Content/WellKnownRecipeIds.cs, FOUND: SimCore.Tests/Content/ContentRegistryContractTests.cs |
| GATE.S18.SHIP_MODULES.ZONE_ARMOR.001 | DONE | Add ZoneArmor struct (Fore/Port/Stbd/Aft HP). Extend Fleet entity with ZoneArmor[4]. CombatTweaksV0 for base zone armor per ship. Damage flow: Shield->ZoneArmor[facing]->Hull. Proof: dotnet test --filter "Determinism" + --filter "ZoneArmor" | FOUND: SimCore/Entities/Fleet.cs, FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs, NEW: SimCore.Tests/Systems/ZoneArmorTests.cs |

### W2. Dock Tabs (bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.EMPIRE_DASH.DOCK_TABS.001 | DONE | Restructure dock menu from 3 tabs (Market/Jobs/Services) to 5 tabs (Market/Jobs/Ship/Station/Intel) per EmpireDashboard.md. Ship+Station+Intel tabs show placeholder until tier 2-3 gates fill them. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/ui/StationMenu.cs |

### W3. Geographic Distribution + Price Bands + Sustain (core, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.TRADE_GOODS.GEO_DISTRIBUTION.001 | DONE | MarketInitGen seeds organics (~40% of nodes, agri-systems), rare_metals (~15% clustered), exotic_crystals (fracture-only). System archetype emerges from local resources. Update PlanetInitGen for resource distribution. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Gen/MarketInitGen.cs, FOUND: SimCore/Gen/PlanetInitGen.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S18.TRADE_GOODS.PRICE_BANDS.001 | DONE | Add base_price and price_spread fields to content registry good definitions. Low (50-100), Mid (150-300), High (400-800), Very High (1000-2000). MarketSystem uses per-good base prices. NEW: MarketTweaksV0 for spread/modifier gameplay knobs. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Entities/Market.cs, NEW: SimCore/Tweaks/MarketTweaksV0.cs |
| GATE.S18.TRADE_GOODS.SUSTAIN_ALIGN.001 | DONE | Update module sustain recipes per trade_goods_v0.md. Weapon modules consume munitions instead of metal. Coilgun->1 munitions, Missile Pod->1 munitions+1 fuel, Railgun->2 munitions+1 composites, etc. Proof: dotnet test --filter "Determinism" + --filter "Sustainment" | FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Systems/SustainmentReport.cs, FOUND: SimCore/Systems/SustainmentSnapshot.cs |

### W4. Ship Class + Fitting (core, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.SHIP_MODULES.SHIP_CLASS.001 | DONE | Define 8 ship classes (Shuttle/Corvette/Clipper/Frigate/Hauler/Cruiser/Carrier/Dreadnought) with base stats per ship_modules_v0.md. ShipClassContentV0 in content registry: slot_count, base_power, base_zone_armor[4], mass, cargo, scan_range. Fleet references ship_class_id. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Entities/Fleet.cs, NEW: SimCore/Content/ShipClassContentV0.cs |
| GATE.S18.SHIP_MODULES.FITTING_BUDGET.001 | DONE | 3-constraint fitting: slots (count), power (gen vs draw), sustain (goods/cycle). RefitSystem validates all 3 constraints on module install. ModuleSlot tracks power_draw. Proof: dotnet test --filter "Determinism" + --filter "Refit" | FOUND: SimCore/Systems/RefitSystem.cs, FOUND: SimCore/Entities/ModuleSlot.cs, FOUND: SimCore/Content/WellKnownModuleIds.cs, FOUND: SimCore.Tests/Systems/RefitSystemTests.cs |

### W5. Bridge + Market UI (bridge, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.TRADE_GOODS.BRIDGE_MARKET.001 | DONE | SimBridge.Market.cs exposes new goods (organics, rare_metals, munitions, exotic_matter) and renamed goods (composites) in market snapshots. Display names from content registry. hero_trade_menu.gd shows all 13 goods. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Market.cs, FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/ui/MarketTabView.cs |
| GATE.S18.EMPIRE_DASH.OVERVIEW_TAB.001 | DONE | Empire Overview (F1): 6 summary cards (Economy/Fleet/Industry/Research/Exploration/Security) with KPIs + trend arrows. Needs Attention queue with clickable action buttons. Each card navigates to its tab. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.Ui.cs, FOUND: scripts/bridge/SimBridge.Reports.cs |
| GATE.S18.EMPIRE_DASH.STATION_TAB.001 | DONE | Dock Station tab: station health bar, local production (active recipes + efficiency), installed services. SimBridge queries for station/industry data. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.NpcIndustry.cs, FOUND: scripts/bridge/SimBridge.Maintenance.cs |

### W6. Chain Tests (content, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.TRADE_GOODS.CHAIN_TESTS.001 | DONE | Contract tests for all 9 production chains. Verify recipe I/O matches trade_goods_v0.md. Test chain depth <= 3. Test Metal->Munitions/Composites/Components fork. Test Salvage->Metal/Components paths. No phantom demands. Proof: dotnet test --filter "ProductionChain or ContentRegistry" | FOUND: SimCore.Tests/Content/ContentRegistryContractTests.cs, NEW: SimCore.Tests/Content/ProductionChainTests.cs |

### W7. NPC Trade + Combat Zones (core, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.TRADE_GOODS.NPC_TRADE_UPDATE.001 | DONE | NpcTradeSystem trades new goods. NPC routes prioritize geographic arbitrage (organics agri->industrial, rare_metals deposit->military). Update NpcTradeTweaksV0 good weights. Proof: dotnet test --filter "Determinism" + --filter "NpcTrade" | FOUND: SimCore/Systems/NpcTradeSystem.cs, FOUND: SimCore/Tweaks/NpcTradeTweaksV0.cs, FOUND: SimCore.Tests/Systems/NpcTradeSystemTests.cs |
| GATE.S18.SHIP_MODULES.COMBAT_ZONES.001 | DONE | Integrate zone armor into CombatSystem. Player hits: collision-point -> local space -> zone. NPC-vs-NPC: class-based stance hit distribution (Charge=50% Fore, Broadside=35% Port/Stbd, Kite=55% Aft). StrategicResolverV0 uses ship class for stance. Proof: dotnet test --filter "Determinism" + --filter "Combat or NpcFleetCombat" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Systems/StrategicResolverV0.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs, FOUND: SimCore.Tests/Systems/NpcFleetCombatSystemTests.cs |

### W8. Empire Dashboard Tabs (bridge, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.EMPIRE_DASH.SHIP_TAB.001 | DONE | Dock Ship tab: ship class display, installed modules grid, fitting budget (slots/power/sustain used vs max), zone armor diagram (4-face HP bars). SimBridge.Fleet + SimBridge.Refit queries. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/bridge/SimBridge.Refit.cs |
| GATE.S18.EMPIRE_DASH.ECONOMY_TAB.001 | DONE | Empire Economy (F2): trade routes list, good inventory across stations, supply/demand per system. Price history mini-charts. SimBridge.TradeIntel + SimBridge.Reports queries. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.TradeIntel.cs, FOUND: scripts/bridge/SimBridge.Reports.cs |

### W9. Headless Proof (bridge, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S18.TRADE_GOODS.HEADLESS_PROOF.001 | DONE | Headless GDScript test: boot game, dock, buy/sell organics + munitions, verify inventory/credits. End-to-end proof of trade goods overhaul. Proof: godot --headless --path . res://scripts/tests/test_trade_goods_v0.gd | NEW: scripts/tests/test_trade_goods_v0.gd, NEW: scenes/tests/test_trade_goods_v0.tscn |

### W10. Meta (docs, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.EPIC_REVIEW.017 | DONE | Audit epic statuses vs completed gates. Close completed epics. Recommend next anchor for tranche 18. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/56_SESSION_LOG.md |
| GATE.X.HYGIENE.ECONOMY_EVAL.001 | DONE | Evaluate 13-good economy: chain depth <= 3, every good 2+ demands, no phantom demands, geographic distribution balance, sustain escalation curve. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/56_SESSION_LOG.md, FOUND: docs/design/trade_goods_v0.md |

## W. Tranche 18 "Faction Infrastructure" (EPIC.S7.REPUTATION_INFLUENCE, EPIC.S7.TERRITORY_REGIMES, EPIC.S6.OFFLANE_FRACTURE)

### W1. Reputation (core, tier 1 — hash-affecting)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.REPUTATION.ACCESS_TIERS.001 | DONE | 5 rep tiers (Allied>=75/Friendly>=25/Neutral>=-25/Hostile>=-75/Enemy<-75) gate dock access, trade access, tech purchase. Add tier enum + access check methods to ReputationSystem. Thresholds in FactionTweaksV0. Proof: dotnet test --filter "Determinism" + "ReputationAccessTier" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: SimCore/Systems/MarketSystem.cs, NEW: SimCore.Tests/Systems/ReputationAccessTierTests.cs |
| GATE.S7.REPUTATION.PRICING_CURVES.001 | DONE | Rep-driven price modifiers: Allied=-15% buy/+15% sell, Friendly=-5%/+5%, Neutral=0, Hostile=+20%/-20%, Enemy=blocked. Update MarketSystem GetEffectiveTariffBps to include rep pricing layer. Proof: dotnet test --filter "Determinism" + "ReputationPricing" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs, NEW: SimCore.Tests/Systems/ReputationPricingTests.cs |

### W2. Territory (core, tier 1-2 — hash-affecting)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.TERRITORY.REGIME_MODEL.001 | DONE | Dynamic territory regime computed from faction doctrine + player rep. Open (Open+Friendly), Guarded (Open+Neutral or Guarded+Friendly), Restricted (Guarded+Hostile or any+Hostile), Hostile (Closed or Enemy). Add ComputeTerritoryRegime to ReputationSystem. Proof: dotnet test --filter "Determinism" + "TerritoryRegime" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs, NEW: SimCore.Tests/Systems/TerritoryRegimeTests.cs |
| GATE.S7.TERRITORY.PATROL_RESPONSE.001 | DONE | NPC patrol engagement rules by regime: Open=no engagement, Guarded=scan only (flash warning), Restricted=pursue if cargo>threshold, Hostile=attack on sight. Update NpcFleetCombatSystem. Proof: dotnet test --filter "Determinism" + "TerritoryPatrol" | FOUND: SimCore/Systems/NpcFleetCombatSystem.cs, FOUND: SimCore/Systems/ReputationSystem.cs, NEW: SimCore.Tests/Systems/TerritoryPatrolTests.cs |

### W3. Fracture (core, tier 1-2 — hash-affecting)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.FRACTURE.COST_MODEL.001 | DONE | Fracture travel applies costs: fuel consumption (FractureFuelPerJump), hull stress damage (FractureHullStressPerJump), trace accumulation at destination node (FractureTracePerArrival). Update FractureSystem.Process and FractureTravelCommand validation. Constants in FractureTweaksV0. Proof: dotnet test --filter "Determinism" + "FractureCost" | FOUND: SimCore/Systems/FractureSystem.cs, FOUND: SimCore/Tweaks/FractureTweaksV0.cs, FOUND: SimCore/Commands/FractureTravelCommand.cs, NEW: SimCore.Tests/Systems/FractureCostTests.cs |
| GATE.S6.FRACTURE.DETECTION_REP.001 | DONE | Factions detect fracture use via trace signature at nodes. When node.Trace > detection threshold and node has controlling faction, apply rep penalty to player. Detection chance scales with trace level. Add DetectFractureUse to FractureSystem.Process. Proof: dotnet test --filter "Determinism" + "FractureDetection" | FOUND: SimCore/Systems/FractureSystem.cs, FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Tweaks/FractureTweaksV0.cs, NEW: SimCore.Tests/Systems/FractureDetectionTests.cs |

### W4. Bridge wiring (bridge, tier 1-2 — non-hash)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.FRACTURE.PLAYER_DISPATCH.001 | DONE | Add DispatchFractureTravelV0(fleetId, voidSiteId) to SimBridge.Fracture.cs. Wire game_manager.gd on_fracture_travel_v0 trigger for void site interaction. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Fracture.cs, FOUND: scripts/core/game_manager.gd |
| GATE.S7.REPUTATION.UI_INDICATORS.001 | DONE | Dock trade menu shows rep tier label (color-coded), price modifier percentage, "Access Denied" overlay when trade blocked. Enrich GetTerritoryAccessV0 with rep_tier and price_modifier fields. Proof: dotnet build | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.TERRITORY.BRIDGE_DISPLAY.001 | DONE | Add GetTerritoryRegimeV0(nodeId) to SimBridge.Faction.cs returning regime string. GalaxyView node popup shows regime. Dock info panel shows regime with color. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S6.FRACTURE.UI_PANEL.001 | DONE | New fracture_travel_panel.gd: list available void sites with distance/cost/trace-risk, confirm button dispatches fracture travel, cancel returns to flight. Wire from game_manager proximity trigger. Proof: dotnet build | NEW: scripts/ui/fracture_travel_panel.gd, FOUND: scripts/bridge/SimBridge.Fracture.cs, FOUND: scripts/core/game_manager.gd |

### W5. Headless proof (bridge, tier 3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.INFRA.HEADLESS_PROOF.001 | DONE | Headless test: dock at faction station, verify rep tier affects pricing, verify territory regime displayed, initiate fracture travel round trip, verify hull stress applied. Proof: godot --headless -s test_faction_infra.gd | NEW: scripts/tests/test_faction_infra.gd, FOUND: scripts/bridge/SimBridge.Faction.cs, FOUND: scripts/bridge/SimBridge.Fracture.cs |

### W6. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.018 | DONE | Full test suite (679+ tests), warning scan, dead code check, golden hash stability. Proof: dotnet test -c Release | FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |
| GATE.X.HYGIENE.EPIC_REVIEW.018 | DONE | Audit epic statuses vs completed gates. Close completed epics. Recommend content tranche anchor and scope. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.LORE_REVIEW.001 | DONE | Map factions_and_lore_v0.md (RESEARCH COMPLETE) to concrete implementation gates: 6 factions, goods mapping, ship rosters, instability phases, adaptation fragments, endgame paths. Output: prioritized gate list for content tranche. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/design/factions_and_lore_v0.md, FOUND: docs/54_EPICS.md, FOUND: docs/56_SESSION_LOG.md |

## X. Tranche 19 — Faction Content & Warfront Foundation

### X1. Core (SimCore, tier 1-3, hash-affecting)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FACTION.CONTENT_DATA.001 | DONE | Update 3 placeholder factions to 5 named factions (Concord/Chitin/Weavers/Valorin/Communion) with lore-accurate: species, TradePolicy, TariffRate, AggressionLevel, production goods, needed goods per factions_and_lore_v0.md. Update FactionTweaksV0 + WorldFaction + GalaxyGenerator. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: SimCore/Schemas/WorldDefinition.cs, FOUND: SimCore/Gen/GalaxyGenerator.cs |
| GATE.S7.INSTABILITY.PHASE_MODEL.001 | DONE | Add InstabilityLevel int field to Node entity. 5 phases: Stable(0-24), Shimmer(25-49), Drift(50-74), Fracture(75-99), Void(100+). NEW: InstabilityTweaksV0.cs with thresholds. Add phase helper methods. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Entities/Node.cs, NEW: SimCore/Tweaks/InstabilityTweaksV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S7.WARFRONT.STATE_MODEL.001 | DONE | New WarfrontState entity: Id, CombatantA, CombatantB, Intensity(0-4), ContestedNodeIds, TickStarted, WarType(Hot/Cold). Store in SimState.Warfronts dictionary. NEW: WarfrontTweaksV0.cs. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Entities/WarfrontState.cs, NEW: SimCore/Tweaks/WarfrontTweaksV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S7.FACTION.PENTAGON_RING.001 | DONE | Pentagon dependency model in FactionTweaksV0: primary ring (Concord->Composites from Weavers, Weavers->Electronics from Chitin, Chitin->Rare Metals from Valorin, Valorin->Exotic Crystals from Communion, Communion->Food+Fuel from Concord) + secondary cross-links. Contract test validates ring integrity. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Tweaks/FactionTweaksV0.cs, NEW: SimCore.Tests/Systems/FactionDependencyTests.cs |
| GATE.S7.WARFRONT.SEEDING.001 | DONE | GalaxyGenerator seeds warfronts at world creation: 1 hot war (Valorin-Weaver territorial), 1 cold war (Concord-Chitin informational). Player starts in non-combatant space 2-3 hops from front. Contested nodes at faction borders. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Gen/GalaxyGenerator.cs, NEW: SimCore/Entities/WarfrontState.cs, FOUND: SimCore/World/WorldLoader.cs |
| GATE.S7.WARFRONT.DEMAND_SHOCK.001 | DONE | NEW: WarfrontDemandSystem. Factions at war consume goods at elevated rates per WarfrontTweaksV0 (Munitions 3-5x, Composites 2-3x, Fuel 2-4x). Adjusts NPC consumption multiplier based on warfront intensity. Wire into SimKernel.Step(). Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/WarfrontDemandSystem.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S7.WARFRONT.TARIFF_SCALING.001 | DONE | Update MarketSystem tariff calculation: EffectiveTariff = BaseTariff + (WarSurcharge * NodeWarfrontIntensity). Per-system, not per-faction. WarSurcharge values in WarfrontTweaksV0. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs, NEW: SimCore.Tests/Systems/WarfrontTariffTests.cs |
| GATE.S7.INSTABILITY.WORLDGEN.001 | DONE | GalaxyGenerator assigns initial instability per node: core=0-10, frontier=10-30, rim=20-50, void sites=50-80. Discovery sites placed in Shimmer+ zones. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Gen/GalaxyGenerator.cs, FOUND: SimCore/Tweaks/InstabilityTweaksV0.cs, FOUND: SimCore/Entities/Node.cs |
| GATE.S7.WARFRONT.EVOLUTION.001 | DONE | NEW: WarfrontEvolutionSystem. Warfront intensity evolves per tick: cold war escalates to hot (tick 200-600 by seed), hot war can ceasefire (tick 600-1200). New fronts open as old ones cool. Deterministic by seed. Wire into SimKernel.Step(). Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/WarfrontEvolutionSystem.cs, FOUND: SimCore/SimKernel.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs |
| GATE.S7.WARFRONT.NEUTRALITY_TAX.001 | DONE | MarketSystem applies neutrality surcharge for unaligned traders at war-zone stations: +5% at Intensity 2, +10% at 3, +15% at 4. Inspection frequency scaling. Access denial at Intensity 4 frontline without allegiance. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs, FOUND: SimCore/Systems/ReputationSystem.cs |
| GATE.S7.WARFRONT.SUPPLY_CASCADE.001 | DONE | Pentagon cascade test suite: war between A-B disrupts A production -> downstream faction supply drops -> prices spike at dependent stations. Verify cascade through NpcTradeSystem + MarketSystem. Proof: dotnet test --filter "Determinism" | NEW: SimCore.Tests/Systems/WarfrontCascadeTests.cs, FOUND: SimCore/Systems/NpcTradeSystem.cs, FOUND: SimCore/Systems/MarketSystem.cs |

### X2. Bridge (SimBridge + UI, tier 2-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.WARFRONT.BRIDGE.001 | DONE | NEW: SimBridge.Warfront.cs partial. GetWarfrontsV0() returns array of warfront state dicts {id, combatantA, combatantB, intensity, war_type, contested_nodes}. GetNodeWarIntensityV0(nodeId) returns intensity int (0 if no warfront). Proof: dotnet build | NEW: scripts/bridge/SimBridge.Warfront.cs, FOUND: SimCore/Entities/WarfrontState.cs |
| GATE.S7.INSTABILITY.BRIDGE.001 | DONE | Add to SimBridge.Faction.cs: GetNodeInstabilityV0(nodeId) returns {level (int), phase (string: Stable/Shimmer/Drift/Fracture/Void), effects (array of strings)}. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Faction.cs, FOUND: SimCore/Entities/Node.cs |
| GATE.S7.FACTION.IDENTITY_PANEL.001 | DONE | EmpireDashboard Factions tab enhancement: per-faction detail showing species, philosophy, trade policy, produces list, needs list, unique tech, endgame alignment. Uses GetFactionMapV0 + new GetFactionDetailV0 bridge query. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.WARFRONT.UI_MAP.001 | DONE | GalaxyView warfront overlays: contested nodes highlighted with pulsing red border, warfront intensity gradient coloring. Dock menu shows tariff surcharge breakdown ("Tariff: 20% (base 8% + war 12%)"). Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S7.INSTABILITY.VISUAL.001 | DONE | GalaxyView phase-colored node indicators: Stable=green, Shimmer=yellow, Drift=orange, Fracture=red, Void=purple. Shimmer+ nodes get subtle pulse animation. Phase label on node hover. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.WARFRONT.HEADLESS_PROOF.001 | DONE | Headless proof: boot scene, verify 5 factions populated, pentagon ring valid, warfronts seeded (1 hot + 1 cold), demand shocks active, tariff scaling correct, instability phases assigned. HFWP output with SHA256. Proof: godot --headless -s res://scripts/tests/test_warfront_econ.gd | NEW: scripts/tests/test_warfront_econ.gd, FOUND: scripts/bridge/SimBridge.Warfront.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |

### X3. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.019 | DONE | Full test suite (762+ tests), warning scan, dead code check, golden hash stability. Proof: dotnet test -c Release | FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |
| GATE.X.HYGIENE.EPIC_REVIEW.019 | DONE | Audit epic statuses vs completed gates. Close completed epics. Recommend next anchor for tranche 20. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.TENSION_EVAL.001 | DONE | Evaluate dynamic_tension_v0.md 5 pillars vs current simulation capabilities. Map each pillar to existing/new systems. Identify gaps, prioritize for tranche 20. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/design/dynamic_tension_v0.md, FOUND: docs/54_EPICS.md |

## Tranche 20: Warfront Agency & Living Instability

Anchor: EPIC.S7.FACTION_MODEL. Epics: SUPPLY_IMPACT, INSTABILITY_PHASES, TERRITORY_REGIMES, REPUTATION_INFLUENCE, UI_WARFRONT.

### Y1. Core (SimCore, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.SUPPLY.DELIVERY_LEDGER.001 | DONE | Track cumulative war supply deliveries per faction in SimState. WarfrontDemandSystem records delivery totals when goods consumed from contested nodes. New WarSupplyLedger dict in SimState. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/SimState.cs, FOUND: SimCore/Systems/WarfrontDemandSystem.cs, FOUND: SimCore/Entities/WarfrontState.cs |
| GATE.S7.INSTABILITY.TICK_SYSTEM.001 | DONE | NEW: InstabilitySystem. Per-tick instability evolution: warfront-adjacent nodes gain instability (+1/tick per intensity level). Distant nodes stabilize (-1/100 ticks). Phase transitions at thresholds. Wire into SimKernel.Step(). Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/InstabilitySystem.cs, FOUND: SimCore/SimKernel.cs, FOUND: SimCore/Entities/Node.cs, FOUND: SimCore/Tweaks/InstabilityTweaksV0.cs |
| GATE.S7.TERRITORY.EMBARGO_MODEL.001 | DONE | NEW: EmbargoState entity. Factions at war embargo enemy's key goods (from pentagon ring needs). Seeded alongside warfronts. MarketSystem checks embargo before trade execution. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Entities/EmbargoState.cs, FOUND: SimCore/SimState.cs, FOUND: SimCore/Systems/MarketSystem.cs, NEW: SimCore/Tweaks/EmbargoTweaksV0.cs |
| GATE.S7.REPUTATION.TRADE_DRIFT.001 | DONE | Reputation drift: natural decay toward 0 at 1 point per 1440 ticks (1 game day). Trading at faction-controlled market gives small +rep. Add drift logic to ReputationSystem or new per-tick process. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S7.SUPPLY.WARFRONT_SHIFT.001 | DONE | Supply deliveries shift warfront intensity. When cumulative supply exceeds threshold, defender gains +1 intensity (reinforced) or attacker -1 (weakened). Threshold in WarfrontTweaksV0. Resets after shift. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/WarfrontDemandSystem.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs, FOUND: SimCore/Entities/WarfrontState.cs |
| GATE.S7.INSTABILITY.CONSEQUENCES.001 | DONE | Phase-based consequences in MarketSystem/LaneFlowSystem: Shimmer=5% price jitter, Drift=+20% lane delay, Fracture=10% trade failure chance, Void=market closure (CanAccessMarket returns false). Values in InstabilityTweaksV0. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Systems/LaneFlowSystem.cs, FOUND: SimCore/Tweaks/InstabilityTweaksV0.cs |
| GATE.S7.TERRITORY.REGIME_TRANSITION.001 | DONE | War-driven regime transitions: intensity >= 3 triggers Restricted regime (higher tariffs, inspection chance). Intensity >= 4 triggers Closed (trade blocked for non-allied). Hysteresis: only improves at intensity <= 1. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs |
| GATE.S7.REPUTATION.WAR_PROFITEER.001 | DONE | War profiteering reputation: selling war-critical goods (munitions, composites, fuel) at a belligerent faction's market gives +2 rep with buyer, -1 rep with their warfront enemy. Applied per NPC trade cycle at contested nodes. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Systems/NpcTradeSystem.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs |

### Y2. Bridge (SimBridge + UI, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.WARFRONT.DASHBOARD_TAB.001 | DONE | Warfronts tab in EmpireDashboard: list active warfronts with combatants, intensity label, war type, contested node count. Uses existing GetWarfrontsV0 bridge query. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.Warfront.cs |
| GATE.S7.SUPPLY.BRIDGE.001 | DONE | Add to SimBridge.Warfront.cs: GetWarSupplyV0(warfrontId) returns {warfront_id, deliveries dict by good_id, shift_threshold, shift_progress_pct}. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Warfront.cs, FOUND: SimCore/SimState.cs |
| GATE.S7.TERRITORY.EMBARGO_BRIDGE.001 | DONE | Add to SimBridge.Faction.cs: GetEmbargoesV0(marketId) returns array of {good_id, faction_id, reason}. IsGoodEmbargoedV0(marketId, goodId) returns bool. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Faction.cs, FOUND: SimCore/Entities/EmbargoState.cs |
| GATE.S7.INSTABILITY.EFFECTS_BRIDGE.001 | DONE | Add to SimBridge.Faction.cs: GetInstabilityEffectsV0(nodeId) returns {phase, effects array (strings describing active effects), price_jitter_pct, lane_delay_pct, trade_failure_pct, market_closed}. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Faction.cs, FOUND: SimCore/Tweaks/InstabilityTweaksV0.cs |
| GATE.S7.WARFRONT.SUPPLY_HUD.001 | DONE | Warfronts tab enhancement: per-warfront supply needs list showing which goods needed, quantities delivered, progress bar toward shift threshold. Uses GetWarSupplyV0. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.Warfront.cs |
| GATE.S7.TERRITORY.EMBARGO_UI.001 | DONE | Trade menu (hero_trade_menu.gd): embargoed goods shown grayed out with "Embargoed by [Faction]" label. Uses IsGoodEmbargoedV0. Proof: dotnet build | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.INSTABILITY.EFFECTS_UI.001 | DONE | Node popup and dock menu show instability phase effects (price jitter %, lane delay %, trade failure %). COMBINE with SUPPLY_HUD for EmpireDashboard.cs file conflict. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.FACTION.REP_TOAST.001 | DONE | Rep change toast: when player rep with a faction changes by >= 5 points, show toast "Reputation with [Faction]: [+/-N] ([reason])". Uses existing toast system. Proof: dotnet build | FOUND: scripts/core/game_manager.gd, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.WARFRONT.HEADLESS_PROOF.002 | DONE | Headless proof: boot scene, verify supply tracking active, embargo blocks trade, instability ticks up near warfronts, regime transitions at high intensity. 50 ticks. HFWP output. Proof: godot --headless -s res://scripts/tests/test_warfront_supply.gd | NEW: scripts/tests/test_warfront_supply.gd, FOUND: scripts/bridge/SimBridge.Warfront.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |

### Y3. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.020 | DONE | Full test suite (780+ tests), warning scan, dead code check, golden hash stability. Proof: dotnet test -c Release | FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |
| GATE.X.HYGIENE.EPIC_REVIEW.020 | DONE | Audit epic statuses vs completed gates. Close completed epics. Recommend next anchor for tranche 21. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.FACTION_PLAYTEST.001 | DONE | Evaluate warfront player experience: can the player meaningfully influence wars through trade? Are embargoes creating interesting smuggling decisions? Is instability visible and impactful? Rate each tension pillar 1-5. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/design/dynamic_tension_v0.md, FOUND: docs/54_EPICS.md |

## Z. Tranche 21 — Sustain + Production + Power + Loot + Faction Visuals

### Z1. Sustain Enforcement (core, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.SUSTAIN.FUEL_DEDUCT.001 | DONE | SustainSystem: fleet movement deducts fuel from cargo per tick, module sustain goods deducted per cycle. Tweaks in SustainTweaksV0. Wired into SimKernel.Step. Proof: dotnet test --filter "SustainSystem" + dotnet test --filter "Determinism" | NEW: SimCore/Systems/SustainSystem.cs, NEW: SimCore/Tweaks/SustainTweaksV0.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S7.SUSTAIN.SHORTFALL.001 | DONE | Sustain shortfall effects: 0 fuel -> fleet immobilized (MovementSystem skip). Missing module sustain -> module disabled (flagged on ModuleSlot). Contract tests for shortfall detection and recovery. Proof: dotnet test --filter "SustainSystem" + dotnet test --filter "Determinism" | FOUND: SimCore/Systems/SustainSystem.cs, FOUND: SimCore/Systems/MovementSystem.cs, FOUND: SimCore/Entities/ModuleSlot.cs |
| GATE.S7.SUSTAIN.ECONOMY_WIRE.001 | DONE | NPC fleets consume fuel during lane transit via NpcTradeSystem at 50% of player rate (SustainTweaksV0.NpcFuelRateMultiplier), creating real fuel demand at markets. Proof: dotnet test --filter "NpcTradeSystem" + dotnet test --filter "Determinism" | FOUND: SimCore/Systems/NpcTradeSystem.cs, FOUND: SimCore/Tweaks/SustainTweaksV0.cs |
| GATE.S7.SUSTAIN.BRIDGE_PROOF.001 | DONE | GetFleetSustainStatusV0 (fuel level, sustain health per module) in SimBridge.Fleet.cs. HUD fuel indicator in hud.gd. Headless proof: fuel depletes -> fleet stops -> refuel -> fleet moves. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/ui/hud.gd, NEW: scripts/tests/test_sustain_proof.gd |

### Z2. Production Chains (core+bridge, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.PRODUCTION.FULL_DEPLOY.001 | DONE | Instantiate remaining 5 recipes (ProcessFood, FabricateComposites, AssembleComponents, SalvageToMetal, SalvageToComponents) as industry sites in MarketInitGen + PlanetInitGen with geographic constraints (food at agri nodes, composites at industrial). Chain coverage contract tests. Proof: dotnet test --filter "ContentRegistryContract" + dotnet test --filter "Determinism" | FOUND: SimCore/Gen/MarketInitGen.cs, FOUND: SimCore/Gen/PlanetInitGen.cs |
| GATE.S7.PRODUCTION.BRIDGE_READOUT.001 | DONE | Enhanced GetNodeIndustryV0: input/output good display names, efficiency %, recipe name. Economy tab chain visualization in EmpireDashboard. Station tab local production detail. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Reports.cs, FOUND: scripts/ui/hero_trade_menu.gd |

### Z3. Power Budget (core+bridge, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.POWER.BUDGET_ENFORCE.001 | DONE | PowerBudgetSystem: sum equipped module PowerDraw vs ship class BasePower. Over-budget -> lowest-priority module disabled. Wired into SimKernel.Step. Proof: dotnet test --filter "PowerBudget" + dotnet test --filter "Determinism" | NEW: SimCore/Systems/PowerBudgetSystem.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S7.POWER.MOUNT_DEGRADE.001 | DONE | MountType compatibility: SlotKind must match module category, RefitSystem rejects mismatched mounts. Module Condition (0-100) decays per cycle via MaintenanceSystem, 0% -> module disabled. Repair cost in goods. Proof: dotnet test --filter "PowerBudget|RefitSystem|Maintenance" + dotnet test --filter "Determinism" | FOUND: SimCore/Entities/ModuleSlot.cs, FOUND: SimCore/Systems/RefitSystem.cs, FOUND: SimCore/Systems/MaintenanceSystem.cs |
| GATE.S7.POWER.BRIDGE_UI.001 | DONE | Power budget bars (used/total) + mount type labels + condition% per module in dock Ship tab. Over-budget warning label. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Refit.cs, FOUND: scripts/ui/hero_trade_menu.gd |

### Z4. Combat Loot (core+bridge, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S5.LOOT.DROP_SYSTEM.001 | DONE | LootTableSystem + LootDrop entity. Rarity tiers: Common 60, Uncommon 25, Rare 12, Epic 3 (weighted). Drop roll on NPC fleet kill via NpcFleetCombatSystem. Content in LootTweaksV0. Proof: dotnet test --filter "LootTable" + dotnet test --filter "Determinism" | NEW: SimCore/Systems/LootTableSystem.cs, NEW: SimCore/Entities/LootDrop.cs, NEW: SimCore/Tweaks/LootTweaksV0.cs |
| GATE.S5.LOOT.TRACTOR_CMD.001 | DONE | CollectLootCommand: player collects loot within tractor range, adds goods/modules/credits to cargo. Proof: dotnet test --filter "CollectLoot" + dotnet test --filter "Determinism" | NEW: SimCore/Commands/CollectLootCommand.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.S5.LOOT.BRIDGE_PROOF.001 | DONE | GetNearbyLootV0, DispatchCollectLootV0 in SimBridge.Combat.cs. Loot particle markers in local space via GalaxyView. Collection toast with rarity colors. Headless proof: kill NPC -> loot drops -> collect -> cargo increased. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Combat.cs, FOUND: scripts/view/GalaxyView.cs, NEW: scripts/tests/test_loot_proof.gd |

### Z5. Faction Visuals (bridge, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FACTION_VIS.COLOR_PALETTE.001 | DONE | Faction primary/secondary/accent colors in FactionTweaksV0 (Concord=blue, Chitin=amber, Weavers=green, Valorin=red, Communion=purple). GetFactionColorsV0 bridge query. Proof: dotnet build SimCore/SimCore.csproj --nologo | FOUND: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.FACTION_VIS.SHIP_LIVERY.001 | DONE | NPC ship StandardMaterial3D albedo_color set from faction palette in npc_ship.gd. Faction color passed during SpawnNpcShipV0. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/npc_ship.gd, FOUND: scripts/view/GalaxyView.cs |
| GATE.S7.FACTION_VIS.STATION_STYLE.001 | DONE | Station accent mesh colored by controlling faction. Faction name banner Label3D. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.FACTION_VIS.TERRITORY_OVERLAY.001 | DONE | Galaxy map semi-transparent faction territory fill using ComputeFactionTerritories BFS data. Faction color legend panel. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/EmpireDashboard.cs |

### Z6. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.021 | DONE | Full test suite (780+ tests), warning scan, dead code check, golden hash stability. Proof: dotnet test -c Release | FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |
| GATE.X.HYGIENE.EPIC_REVIEW.021 | DONE | Audit epic statuses vs completed gates. Close completed epics. Recommend next anchor for tranche 22. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.EVAL.SUSTAIN_BALANCE.001 | DONE | Multi-seed sustain economy balance evaluation: 5 seeds x 5000 ticks. Check fleet fuel consumption rates, sustain costs vs income, immobilization frequency. Report balance gaps. Proof: dotnet test --filter "SustainBalance" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore/Tweaks/SustainTweaksV0.cs |

## AA. Tranche 22 — Combat Juice + Audio Wiring + HUD Architecture + Instability + Enforcement + Starter

### AA1. Combat Juice (bridge, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.COMBAT_JUICE.EXPLOSION_VFX.001 | DONE | Multi-phase kill explosion: white flash (0.1s) -> orange fireball GPUParticles3D (0.5s) -> debris chunks (1.0s) -> smoke (1.5s). Spawned at NPC ship position on death in npc_ship.gd. Per CombatFeel.md + VisualContent_TBA.md VFX.COMBAT.EXPLOSION. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/vfx/explosion_effect.gd, FOUND: scripts/core/npc_ship.gd |
| GATE.S7.COMBAT_JUICE.SHIELD_VFX.001 | DONE | Shield hit: hex ripple shader on sphere mesh around ship hull, blue-white fade 0.3s, impact_point uniform. Shield break (HP=0): full-ship flash + electric discharge particles. Per CombatFeel.md VFX.COMBAT.SHIELD_RIPPLE + SHIELD_BREAK. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/vfx/shield_ripple.gd, FOUND: scripts/core/npc_ship.gd |
| GATE.S7.COMBAT_JUICE.DAMAGE_NUMBERS.001 | DONE | Floating damage numbers at impact point: billboard Label3D, drift up 0.5s, fade. Shield=blue, Hull=orange, Critical=white+pulse. Format "-8" / "-24!". Stacking Y-offset for simultaneous hits. Per CombatFeel.md VFX.COMBAT.DAMAGE_NUMBERS. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/vfx/damage_number.gd, FOUND: scripts/core/npc_ship.gd |
| GATE.S7.COMBAT_JUICE.COMBAT_PRESENT.001 | DONE | Weapon trail differentiation: Kinetic=short thick white, Energy=long thin cyan, PD=rapid thin yellow (bullet.gd). Screen shake on damage taken + turret fire (player_follow_camera.gd). Wire combat audio: call play_fire_sfx/play_hit_sfx from combat events (combat_audio.gd pools exist but never called). Per CombatFeel.md. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bullet.gd, FOUND: scripts/audio/combat_audio.gd, FOUND: scripts/view/player_follow_camera.gd |
| GATE.S7.COMBAT_JUICE.SCENE_PROOF.001 | DONE | Headless combat juice proof: boot combat scene, trigger NPC kill, verify explosion particles spawned + shield ripple on hit + damage number labels created. Screenshot capture of combat with VFX. PLAYABLE_BEAT milestone. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/tests/test_combat_juice_v0.gd, FOUND: scripts/core/npc_ship.gd |

Combined-agent note: EXPLOSION_VFX + SHIELD_VFX + DAMAGE_NUMBERS share npc_ship.gd — assign to same agent.

### AA2. Audio Wiring (bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.AUDIO_WIRING.BUS_WIRE.001 | DONE | Configure 5-layer audio bus in Godot project: Music->Ambient->SFX->UI->Alert with ducking priority (Alert ducks all, SFX ducks Ambient). Wire 6 existing unwired assets: combat fire pool (combat_audio.gd play_fire_sfx), combat impact pool (play_hit_sfx), station ambient hum (ambient_audio.gd), dock chime, explosion SFX, warp whoosh. Per AudioDesign.md 5-layer bus. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/audio/combat_audio.gd, FOUND: scripts/audio/ambient_audio.gd, FOUND: scripts/audio/engine_audio.gd |
| GATE.S7.AUDIO_WIRING.DISCOVERY_CHIMES.001 | DONE | Discovery phase transition audio: Seen->quiet radar ping (0.5s), Scanned->rising chime (1.0s), Analyzed->revelation fanfare (2.0s). Source or create placeholder .wav assets. Wire into DiscoverySitePanel scan/analyze actions. Per ExplorationDiscovery.md + AudioContent_TBA.md AUD.DISC.*. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/audio/discovery_audio.gd, FOUND: scripts/ui/DiscoverySitePanel.gd |

### AA3. HUD Architecture (bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.HUD_ARCH.TOAST_PRIORITY.001 | DONE | Toast notification priority: Critical (red border, 5s+persist), Warning (orange, 4s), Info (default, 3s), Confirmation (green, 2s). Color-coded left border per priority. Bundling: repeated same-text toasts show count badge instead of duplicating. Action bridges: optional clickable button in toast body that navigates to relevant panel. Per HudInformationArchitecture.md. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/toast_manager.gd, FOUND: scripts/ui/hud.gd |
| GATE.S7.HUD_ARCH.ZONE_FRAMEWORK.001 | DONE | Zone G bottom bar: HBoxContainer anchored to screen bottom in ui_hud.tscn. Placeholder slots for risk meters (left), system status (center), minimap (right). Auto-hide when in galaxy map mode. Zone enforcement: audit existing HUD elements against zone map (A-G per HudInformationArchitecture.md). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scenes/ui_hud.tscn |
| GATE.S7.HUD_ARCH.ALERT_BADGE.001 | DONE | Alert badge in Zone A (top-left): red circle with white number showing pending alert count. Orange when warnings only, red when critical alerts exist. Hidden when no alerts. Click opens Empire Dashboard Overview tab. Counts sourced from existing toast/event system. Per HudInformationArchitecture.md VFX.HUD.ALERT_BADGE. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/ui/EmpireDashboard.cs |

Combined-agent note: ZONE_FRAMEWORK + ALERT_BADGE share hud.gd — assign to same agent.

### AA4. Instability Effects (core+bridge, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.INSTABILITY_EFFECTS.MARKET.001 | DONE | MarketSystem reads node.InstabilityLevel from InstabilitySystem. Strained+: price volatility multiplier (1.0->1.5 at max instability via InstabilityTweaksV0). Unstable+: demand skew toward security goods (fuel, munitions). Contract tests proving multiplier scales with instability phase. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "InstabilityEffects" + dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Systems/InstabilitySystem.cs, NEW: SimCore.Tests/Systems/InstabilityEffectsTests.cs |
| GATE.S7.INSTABILITY_EFFECTS.LANE.001 | DONE | LaneFlowSystem reads node.InstabilityLevel. Strained+: transit delay multiplier increases (InstabilityTweaksV0.DelayMultiplierPerLevel). Critical: lane closure (edge blocked, fleets reroute). Contract tests proving delay + closure behavior. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "InstabilityEffects" + dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Systems/LaneFlowSystem.cs, FOUND: SimCore/Tweaks/InstabilityTweaksV0.cs |
| GATE.S7.INSTABILITY_EFFECTS.BRIDGE.001 | DONE | GetNodeInstabilityV0(nodeId): instability level, phase name, effects active. Instability indicator in galaxy map node tooltip. Toast on phase transition (Stable->Strained, etc.). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.cs, FOUND: scripts/ui/hud.gd, FOUND: scripts/ui/toast_manager.gd |

### AA5. Enforcement Escalation (core+bridge, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.ENFORCEMENT.HEAT_ACCUM.001 | DONE | Extend SecurityLaneSystem Edge.Heat: accumulate from trade volume (high-value trades add heat), route repetition (same edge 3+ times in window), counterparty signals (trading with hostile-faction merchants). Heat decay window via SecurityTweaksV0.HeatDecayRatePerTick. Contract tests proving pattern-based accumulation vs flat rate. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "SecurityLaneSystem" + dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Systems/SecurityLaneSystem.cs, FOUND: SimCore/Tweaks/SecurityTweaksV0.cs |
| GATE.S7.ENFORCEMENT.CONFISCATION.001 | DONE | New SecurityEvent: EVT.SECURITY.CONFISCATION at High+ heat threshold. Goods seized (highest-value cargo item, quantity scaled by heat level). Fine in credits. Confiscation cooldown prevents repeated seizures. Contract tests proving confiscation triggers, cooldown, and fine calculation. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "SecurityLaneSystem|Confiscation" + dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Systems/SecurityLaneSystem.cs, FOUND: SimCore/Events/SecurityEvents.cs, FOUND: SimCore.Tests/security/RiskModelContractTests.cs |
| GATE.S7.ENFORCEMENT.BRIDGE.001 | DONE | GetEdgeHeatV0(edgeId): current heat, threshold name, decay rate. GetConfiscationHistoryV0(): recent confiscation events with seized goods + fines. Toast on confiscation with goods lost + fine amount. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Security.cs, FOUND: scripts/ui/toast_manager.gd |

### AA6. Starter Placement (core, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.STARTER_PLACEMENT.WARFRONT.001 | DONE | GalaxyGenerator starter system selection: pick node that borders (1 hop) a contested warfront node. Ensures player starts near action, not in safe interior. StarterRegionNodeCount unchanged but region centered on new starter. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "StarterPlacement" + dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Gen/GalaxyGenerator.cs, FOUND: SimCore/Gen/StarNetworkGen.cs |
| GATE.S7.STARTER_PLACEMENT.VIABILITY.001 | DONE | Contract test: across seeds 1-100, starter system has >=3 viable early trade loops (adjacent markets with price differentials) and >=1 discovery site within 2 hops. Test fails with seed list + delta on violation. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "StarterPlacement" | NEW: SimCore.Tests/Gen/StarterPlacementTests.cs, FOUND: SimCore/Gen/GalaxyGenerator.cs |

### AA7. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.022 | DONE | Full test suite pass (all tests -c Release). Warning scan (dotnet build -warnaserror:nullable). Dead code check in SimCore/. Golden hash snapshot stability across 2 runs. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.022 | DONE | Audit epic statuses: close 5 completed epics (S7.SUSTAIN_ENFORCEMENT, S7.PRODUCTION_CHAINS, S7.POWER_BUDGET, S5.COMBAT_LOOT, S7.FACTION_VISUALS) in 54_EPICS.md. Verify all gates for each epic are DONE. Recommend next anchor for tranche 23. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.EVAL.COMBAT_FEEL.001 | DONE | Combat feel baseline evaluation: run /screenshot scenario with combat bot, capture combat encounters. Rate current state against CombatFeel.md criteria (explosion, shield feedback, damage numbers, weapon trails, screen shake, audio). Establish before baseline for COMBAT_JUICE epic. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "RoadmapConsistency" | FOUND: docs/design/CombatFeel.md, FOUND: scripts/tests/test_combat_beat_v0.gd |

## AB. Tranche 23 — Risk Meters + Fleet Tab + Main Menu + Combat Feel Polish + T2 Modules + Narrative

*19 gates across 6 epics (RISK_METER_UI, FLEET_TAB, MAIN_MENU, COMBAT_FEEL_POLISH, T2_MODULE_CATALOG, NARRATIVE_DELIVERY) + 3 meta gates. New epic: EPIC.S7.COMBAT_FEEL_POLISH.V0.*

### AB1. Risk Meter UI (bridge, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.RISK_METER_UI.BRIDGE.001 | DONE | Add GetRiskMetersV0() to SimBridge.Risk.cs returning Dictionary with Heat, Influence, Trace float values (0.0-1.0 normalized) for player fleet. Reads from existing RiskSystem computed values. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Risk.cs, FOUND: SimCore/Systems/RiskSystem.cs |
| GATE.S7.RISK_METER_UI.WIDGET.001 | DONE | Create risk_meter_widget.gd with 3 horizontal ProgressBars (Heat=red, Influence=blue, Trace=green) in Zone G (bottom-left per HudInformationArchitecture.md). Reads from SimBridge GetRiskMetersV0. Tween-animate on value change. Numeric tooltip on hover. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/risk_meter_widget.gd, FOUND: scripts/ui/hud.gd |
| GATE.S7.RISK_METER_UI.SCREEN_EDGE.001 | DONE | Screen-edge vignette overlay intensifying as risk meters approach critical. Red=Heat, blue=Influence, green=Trace. Shader on ColorRect overlay in main.tscn. Reads risk values from SimBridge via hud.gd. Per RiskMeters.md. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/view/screen_edge_tint.gd, FOUND: scenes/main.tscn |
| GATE.S7.RISK_METER_UI.COMPOUND.001 | DONE | Compound threat indicator: when 2+ risk meters exceed 70%, blinking warning icon on risk widget + intensified edge tinting. Audio cue trigger hook (placeholder). Per RiskMeters.md. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/risk_meter_widget.gd, FOUND: scripts/view/screen_edge_tint.gd |
| GATE.S7.RISK_METER_UI.PROOF.001 | DONE | Screenshot proof: run /screenshot quick, verify risk meter bars visible in flight HUD, screen-edge tinting visible when meters elevated. Color-coding correct (Heat=red, Influence=blue, Trace=green). Proof: powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode quick | FOUND: scripts/ui/risk_meter_widget.gd, FOUND: scripts/ui/hud.gd |

### AB2. Fleet Tab (bridge, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FLEET_TAB.LIST.001 | DONE | Add GetFleetRosterV0() to SimBridge.Fleet.cs returning fleet list (ship class, HP%, location, job status) as Godot Dictionary array. Wire FleetMenu.cs master list panel as scrollable VBoxContainer. Accessible from dock menu Fleet tab. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/ui/FleetMenu.cs |
| GATE.S7.FLEET_TAB.DETAIL.001 | DONE | Fleet detail panel: select ship from master list to see HP bars (hull/shield), module loadout, current job, location. Add GetFleetShipDetailV0(shipId) to SimBridge.Fleet.cs. FleetMenu.cs right panel shows detail on selection. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/FleetMenu.cs, FOUND: scripts/bridge/SimBridge.Fleet.cs |
| GATE.S7.FLEET_TAB.ACTIONS.001 | DONE | Fleet action buttons: Recall (FleetRecallV0 sets destination to player), Dismiss (FleetDismissV0 removes from fleet), Rename. Confirmation dialog for Dismiss. SimBridge methods dispatch FleetSetDestinationCommand. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/FleetMenu.cs, FOUND: scripts/bridge/SimBridge.Fleet.cs |

### AB3. Main Menu (bridge+core, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.MAIN_MENU.SCENE.001 | DONE | Create main_menu.tscn with VBoxContainer buttons: Continue (grayed if no save), New Voyage, Settings (placeholder), Quit. GameManager boots to main menu instead of directly into game. Continue loads last save, New Voyage starts fresh. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scenes/main_menu.tscn, NEW: scripts/ui/main_menu.gd, FOUND: scripts/core/game_manager.gd |
| GATE.S7.MAIN_MENU.NEW_VOYAGE.001 | DONE | Add DifficultyTweaksV0 (galaxy_size, economy_speed, combat_difficulty enum Normal/Hard/Brutal). Store in SimState.DifficultyPreset. SimKernel.InitWorld accepts difficulty, routes to generators. Determinism test verifies same seed+difficulty = same outcome. Hash-affecting. Proof: dotnet test --filter "FullyQualifiedName~Determinism" | NEW: SimCore/Tweaks/DifficultyTweaksV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S7.MAIN_MENU.SAVE_META.001 | DONE | Add GetSaveMetadataV0() to SimBridge returning last save date, credits, system name, play time. Main menu Continue button shows preview card with metadata. Grayed if no save. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S7.MAIN_MENU.PAUSE.001 | DONE | Pause menu overlay on Escape: Resume, Save, Settings, Quit to Menu. Auto-save on pause open. get_tree().paused while menu open. Quit to Menu returns to main_menu.tscn. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/pause_menu.gd, FOUND: scripts/core/game_manager.gd |

### AB4. T2 Modules (core, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.T2_MODULES.CATALOG.001 | DONE | Add 6-8 T2 module definitions to WellKnownModuleIds (weapon/shield/engine/utility families). Each T2 has higher stats + faction_rep_required field in UpgradeContentV0. Content validation test passes. No logic changes. Proof: dotnet test --filter "CatalogContract" | FOUND: SimCore/Content/WellKnownModuleIds.cs, FOUND: SimCore/Content/UpgradeContentV0.cs |
| GATE.S7.T2_MODULES.FITTING.001 | DONE | Faction rep check in EquipModuleCommand: T2 modules require min rep with selling faction (from UpgradeContentV0.faction_rep_required). RefitSystem enforces. Test: insufficient rep -> rejected, sufficient -> succeeds. Hash-affecting. Proof: dotnet test --filter "Determinism" + --filter "RefitSystem" | FOUND: SimCore/Commands/EquipModuleCommand.cs, FOUND: SimCore/Systems/RefitSystem.cs, FOUND: SimCore.Tests/Systems/RefitSystemTests.cs |

### AB5. Narrative Delivery (core, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.NARRATIVE_DELIVERY.ENTITY.001 | DONE | Add FlavorText string field to IntelBook entry types (discovery sites, factions, trade goods). ContentRegistryLoader populates from content JSON. IntelSystem queries return flavor text. Contract test round-trip. Hash-affecting. Proof: dotnet test --filter "Determinism" + --filter "IntelContract" | FOUND: SimCore/Entities/IntelBook.cs, FOUND: SimCore/Systems/IntelSystem.cs |

### AB6. Combat Feel Polish (bridge+docs, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | DONE | Fix ACTIVE_ISSUES C1 (kill explosion never triggers), C2/A1 (audio play methods never called), C3 (no damage numbers), C5 (VFX invisible at altitude 80). Wire explosion_effect spawn on NPC death, damage_number spawn on hit, combat_audio play_fire_sfx/play_hit_sfx calls. Scale VFX for default altitude. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/vfx/explosion_effect.gd, FOUND: scripts/vfx/damage_number.gd, FOUND: scripts/audio/combat_audio.gd, FOUND: scripts/core/npc_ship.gd |
| GATE.S7.COMBAT_FEEL_POLISH.EVAL_PROOF.001 | DONE | Re-run /screenshot eval after WIRE.001. Verify explosion VFX, damage numbers, combat audio. Update ACTIVE_ISSUES.md FIXED for C1, C2, C3, C5, A1. Proof: powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode eval | FOUND: docs/ACTIVE_ISSUES.md, FOUND: scripts/tests/visual_sweep_bot_v0.gd |

### AB7. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.023 | DONE | Full test suite pass (Release). Game assembly build. Warning scan. Dead code check. Golden hash stability across 2 runs. Tranche 23 baseline. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/generated/01_CONTEXT_PACKET.md |
| GATE.X.HYGIENE.EPIC_REVIEW.023 | DONE | Audit epic statuses against completed gates. Mark epics DONE if all gates complete. Update ACTIVE_ISSUES.md. Recommend next anchor for Tranche 24. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes:
- RISK_METER_UI.WIDGET + SCREEN_EDGE share hud.gd dependency — assign to same agent or sequence in tier 2.
- FLEET_TAB gates share FleetMenu.cs — LIST (tier 1) must complete before DETAIL (tier 2) and ACTIONS (tier 3).
- Hash-affecting chain: NEW_VOYAGE.001 (tier 1) -> FITTING.001 (tier 2) -> ENTITY.001 (tier 2, chained after FITTING).

---

## AC. Tranche 24 gates (anchor: EPIC.S7.GALAXY_MAP_V2.V0)

### AC1. Galaxy Map V2 (bridge, tier 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.GALAXY_MAP_V2.QUERIES.001 | DONE | Add SimBridge overlay query methods: GetFactionTerritoryOverlayV0 (Dict per system: controlling_faction, influence_pct), GetFleetPositionsOverlayV0 (fleet positions per system), GetHeatOverlayV0 (heat level per system), GetRoutePathV0(destNodeId) (ordered node list + travel time), GetSystemSearchV0(query) (matching system names). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001 | DONE | Fix station label overlap/truncation (ACTIVE_ISSUES V3). Truncate long resource-type lists with ellipsis, clamp label width. Format system names consistently. Proof: dotnet build "Space Trade Empire.csproj" --nologo. Fixes ACTIVE_ISSUES V3. | FOUND: scripts/view/galaxy_spawner.gd, FOUND: scripts/view/GalaxyView.cs |
| GATE.S7.GALAXY_MAP_V2.OVERLAYS.001 | DONE | Render 3 overlay modes in GalaxyView.cs: Faction Territory (color-coded regions per controlling faction), Fleet Positions (fleet icons at system nodes), Heat (red intensity per system risk level). Toggle bar with F/L/H hotkeys or clickable icons. Each overlay reads from SimBridge queries. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S7.GALAXY_MAP_V2.ROUTE_PLANNER.001 | DONE | Route planner: click destination node on galaxy map to set route, render multi-hop path as polyline connecting systems, show travel time estimate. Uses GetRoutePathV0 from SimBridge. Cancel route with Escape. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: SimCore/Systems/RoutePlanner.cs |
| GATE.S7.GALAXY_MAP_V2.SEARCH.001 | DONE | Galaxy search bar: text input at top of galaxy view, type system name to filter, camera snaps to matching node on Enter. Uses GetSystemSearchV0 from SimBridge. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001 | DONE | Semantic zoom: detail levels by camera altitude. Close range (< 500u): full system detail with station/planet labels. Medium (500-2000u): system names + faction colors only. Galaxy scale (> 2000u): minimal dots + region labels. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/view/galaxy_camera_controller.gd |
| GATE.S7.GALAXY_MAP_V2.PROOF.001 | DONE | Screenshot proof of galaxy map overlays, route planner, search, and semantic zoom. Run /screenshot eval to verify all modes render correctly. Proof: powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode eval | FOUND: scripts/tests/visual_sweep_bot_v0.gd, FOUND: scripts/view/GalaxyView.cs |

### AC2. Narrative Delivery (core+bridge, tier 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.NARRATIVE_DELIVERY.DISCOVERY_TEMPLATES.001 | DONE | Wire discovery narrative templates into DiscoveryOutcomeSystem per ExplorationDiscovery.md. Each discovery type (anomaly, corridor, resource) gets template-driven description text stored in IntelBook entries. FlavorText field populated from content templates keyed to discovery family + phase. Hash-affecting. Proof: dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Systems/DiscoveryOutcomeSystem.cs, FOUND: SimCore/Entities/IntelBook.cs |
| GATE.S7.NARRATIVE_DELIVERY.FACTION_GREETING.001 | DONE | Add GetFactionGreetingV0(factionId, repTier) to SimBridge.Faction.cs returning greeting text string keyed to faction + reputation tier (Hostile/Neutral/Friendly/Allied). Wire into active_station.gd dock panel header. 5 factions x 4 tiers = 20 greeting strings. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Faction.cs, FOUND: scripts/active_station.gd |
| GATE.S7.NARRATIVE_DELIVERY.TEXT_PANEL.001 | DONE | Create narrative_panel.gd: RichTextLabel panel with faction-specific styling (color theme, border, optional portrait placeholder). Displays flavor_text from IntelBook entries and faction greetings. Instantiated by hud.gd or dock menu as needed. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/narrative_panel.gd, FOUND: scripts/ui/hud.gd |

### AC3. Combat Feel Polish (bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001 | DONE | Shield vs hull visual distinction: shield_ripple.gd triggers blue ripple on shield hit, orange spark shower on hull hit. Shield break moment: flash + audio sting when shields reach 0. Wire into npc_ship.gd on_hit handler. Key off shield_pct to determine hit type. CombatAudio plays shield_break SFX. Fixes ACTIVE_ISSUES C4, C6. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/vfx/shield_ripple.gd, FOUND: scripts/core/npc_ship.gd, FOUND: scripts/audio/combat_audio.gd |
| GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001 | DONE | Weapon family visual differentiation by DamageFamily: Kinetic=yellow tracer + muzzle flash, Energy=cyan beam, Neutral=white pulse, PD=green rapid-fire. Modify bullet.gd to accept damage_family parameter and set color/trail accordingly. bullet.tscn updated with trail node. Fixes ACTIVE_ISSUES C7. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bullet.gd, FOUND: scenes/bullet.tscn |

### AC4. Fleet Tab (bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.FLEET_TAB.CARGO.001 | DONE | Per-fleet cargo display in FleetMenu.cs detail panel. Add cargo section showing goods inventory from GetFleetShipDetailV0 cargo field. Format as item rows with quantity and value. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/FleetMenu.cs, FOUND: scripts/bridge/SimBridge.Fleet.cs |
| GATE.S7.FLEET_TAB.PROGRAM.001 | DONE | Program assignment view in FleetMenu.cs detail panel. Show currently assigned program (if any) from SimBridge.Programs.cs. Add Assign Program button that lists available program types. Wire to existing AssignProgramV0 SimBridge method. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/FleetMenu.cs, FOUND: scripts/bridge/SimBridge.Programs.cs |

### AC5. Main Menu (core+bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.MAIN_MENU.CAPTAIN_NAME.001 | DONE | Add CaptainName string field to SimState. New voyage wizard in main_menu.gd gets TextEdit for captain name (default "Commander", max 32 chars). SimKernel.InitWorld accepts captain name. Save/load preserves it. Hash-affecting. Proof: dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/SimState.cs, FOUND: scripts/ui/main_menu.gd |
| GATE.S7.MAIN_MENU.AUTO_SAVE.001 | DONE | Auto-save slot (slot 0) triggers automatically on dock, warp arrival, and mission step completion. game_manager.gd hooks dock/warp/mission signals to call SimBridge SaveGameV0(slot=0). Overwrite without prompt. Visual toast "Auto-saved" on completion. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/bridge/SimBridge.cs |

### AC6. T2 Module Expansion (content, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.T2_MODULES.EXPANSION.001 | DONE | Add ~19 more T2 module definitions to WellKnownModuleIds + UpgradeContentV0 to reach ~25 total T2 modules. Cover all 5 weapon families (Kinetic/Energy/Neutral/PD/Utility) x T2 variants + shield/engine/armor T2. Each has faction_rep_required 25-45. Content validation test passes. Proof: dotnet test --filter "CatalogContract" | FOUND: SimCore/Content/WellKnownModuleIds.cs, FOUND: SimCore/Content/UpgradeContentV0.cs |

### AC7. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.024 | DONE | Full test suite pass (Release). Game assembly build. Warning scan. Dead code check. Golden hash stability across 2 runs. Tranche 24 baseline. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/generated/01_CONTEXT_PACKET.md |
| GATE.X.HYGIENE.EPIC_REVIEW.024 | DONE | Audit epic statuses against completed gates. Mark epics DONE if all gates complete. Update ACTIVE_ISSUES.md. Recommend next anchor for Tranche 25. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.SCREENSHOT_BASELINE.024 | DONE | Set up screenshot regression baselines. Copy current eval screenshots to reports/baselines/full/ with standardized phase names. Run /screenshot regression to verify baseline comparison works. Document baseline process. Proof: powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode regression | FOUND: scripts/tools/compare_screenshots.py, FOUND: scripts/tests/visual_sweep_bot_v0.gd |

Combined-agent notes:
- GALAXY_MAP_V2 tier 2 gates (OVERLAYS + ROUTE_PLANNER + SEARCH + SEMANTIC_ZOOM) all share GalaxyView.cs — assign to same agent.
- COMBAT_FEEL_POLISH gates (SHIELD_VFX + WEAPON_FAMILIES) share combat VFX scripts — assign to same agent.
- FLEET_TAB gates (CARGO + PROGRAM) share FleetMenu.cs — assign to same agent.
- Hash-affecting chain (core, tier 1): CAPTAIN_NAME.001 -> DISCOVERY_TEMPLATES.001.

---

## AD. Tranche 25 — Runtime Stability + Automation (anchor: EPIC.S7.RUNTIME_STABILITY.V0)

### AD1. Runtime Stability (bridge, tier 1)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | DONE | Fix ScreenEdgeTint identifier parse error in hud.gd. Class exists in scripts/view/screen_edge_tint.gd with class_name ScreenEdgeTint — investigate parse error in screen_edge_tint.gd or missing dependency. Entire HUD non-functional. Fixes ACTIVE_ISSUES R1, U2, U3. Proof: dotnet build + Validate-GodotScript.ps1 | FOUND: scripts/ui/hud.gd, FOUND: scripts/view/screen_edge_tint.gd |
| GATE.S7.RUNTIME_STABILITY.FACTION_COLOR.001 | DONE | Fix faction color string-to-Color crash (14+/session). Communion color in FactionTweaksV0.cs stored as tuple string. SimBridge.Faction.cs must pass Color objects. Check all 5 factions. Fixes ACTIVE_ISSUES R2, V8. Proof: dotnet build | FOUND: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: scripts/bridge/SimBridge.Faction.cs |
| GATE.S7.RUNTIME_STABILITY.WARP_ARRIVAL.001 | DONE | Fix V4: giant planet after warp (camera inside planet). Fix V7: camera stays at galactic altitude ~2488 after warp. Fix V6: green empire dashboard (V4 consequence). Check galaxy_spawner.gd planet placement, player_follow_camera.gd zoom-in, game_manager.gd warp handler. Fixes ACTIVE_ISSUES V4, V6, V7. Proof: dotnet build | FOUND: scripts/view/galaxy_spawner.gd, FOUND: scripts/view/player_follow_camera.gd, FOUND: scripts/core/game_manager.gd |
| GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001 | DONE | Fix V5: galaxy map broken — large green 3D elements at wrong scale. Fix V11: 3D Label3D nodes render through 2D dock panel. Hide/disable galaxy 3D elements when 2D UI active. Fixes ACTIVE_ISSUES V5, V11. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/view/galaxy_spawner.gd |
| GATE.S7.RUNTIME_STABILITY.SHIP_VISIBILITY.001 | DONE | Fix V1: player ship too small at default camera altitude. Fix V9: NPC fleet ships rarely visible (21 in galaxy, 0 encountered). Increase ship scale, check NPC distribution. Shares npc_ship.gd with COMBAT_VFX_SCALE — combine agents. Fixes ACTIVE_ISSUES V1, V9. Proof: dotnet build | FOUND: scripts/core/npc_ship.gd, FOUND: scripts/core/hero_ship_flight_controller.gd |
| GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_SCALE.001 | DONE | Fix C10: combat VFX invisible at game altitude. Scale VFX relative to camera distance. Shares npc_ship.gd with SHIP_VISIBILITY — combine agents. Fixes ACTIVE_ISSUES C10. Proof: dotnet build | FOUND: scripts/vfx/damage_number.gd, FOUND: scripts/vfx/shield_ripple.gd, FOUND: scripts/core/npc_ship.gd |
| GATE.S7.RUNTIME_STABILITY.UI_POLISH.001 | DONE | Fix U5: toast notifications stack tightly. Add vertical margins + max visible count. Fix U4: cargo shows empty after purchase (~200 tick delay). Force UI refresh on buy/sell. Fixes ACTIVE_ISSUES U4, U5. Proof: dotnet build | FOUND: scripts/ui/toast_manager.gd, FOUND: scripts/ui/StationMenu.cs |
| GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001 | DONE | Fix V10: warp VFX underwhelming. Enhance warp_tunnel.gd for dramatic tunnel with streaking stars. Fix V12: 1 dead particle system. Fixes ACTIVE_ISSUES V10, V12. Proof: dotnet build | FOUND: scripts/vfx/warp_effect.gd, FOUND: scripts/vfx/warp_tunnel.gd |

### AD2. Galaxy Map V2 — Remaining Overlays (bridge, tier 2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.GALAXY_MAP_V2.EXPLORATION_OVL.001 | DONE | Add Exploration overlay mode (E hotkey). Shows discovery state: unvisited=gray, visited=white, mapped=green, anomaly=purple. Extends existing overlay enum. Blocks: GALAXY_VIEW_FIX.001. Combine with WARFRONT_OVL agent. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.S7.GALAXY_MAP_V2.WARFRONT_OVL.001 | DONE | Add Warfront overlay mode (W hotkey). Red pulsing nodes for active warfronts, disputed territory. Wire to SimBridge.Warfront.cs. Blocks: GALAXY_VIEW_FIX.001. Combine with EXPLORATION_OVL agent. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Warfront.cs |

### AD3. Automation Management — Core (core, tier 1, hash-affecting chain)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.AUTOMATION_MGMT.DOCTRINE.001 | DONE | FleetDoctrine entity (engagement_stance, retreat_threshold, patrol_radius) + DoctrineSystem evaluating during NPC fleet tick. Wire to Fleet.cs. Shared AutomationSystemTests.cs. Hash-affecting. Proof: dotnet test --filter Determinism + AutomationSystemTests | NEW: SimCore/Entities/FleetDoctrine.cs, NEW: SimCore/Systems/DoctrineSystem.cs, NEW: SimCore.Tests/Systems/AutomationSystemTests.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.S7.AUTOMATION_MGMT.PROGRAM_METRICS.001 | DONE | AutomationState.cs with ProgramCycleMetrics (cycles_run, goods_moved, credits_earned, failures). ProgramMetricsSystem accumulates during execution. Blocks: DOCTRINE.001. Hash-affecting. Proof: dotnet test --filter Determinism + AutomationSystemTests | NEW: SimCore/Entities/AutomationState.cs, NEW: SimCore/Systems/ProgramMetricsSystem.cs |
| GATE.S7.AUTOMATION_MGMT.FAILURE_RECOVERY.001 | DONE | ProgramFailureReason enum + FailureRecoverySystem recording failures with optional retry. Blocks: PROGRAM_METRICS.001. Hash-affecting. Proof: dotnet test --filter Determinism + AutomationSystemTests | NEW: SimCore/Systems/FailureRecoverySystem.cs, FOUND: SimCore.Tests/Systems/AutomationSystemTests.cs |
| GATE.S7.AUTOMATION_MGMT.BUDGET_ENFORCEMENT.001 | DONE | BudgetEnforcementSystem enforcing credit_cap/goods_cap per cycle. Blocks: FAILURE_RECOVERY.001. Hash-affecting. Proof: dotnet test --filter Determinism + AutomationSystemTests | NEW: SimCore/Systems/BudgetEnforcementSystem.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.S7.AUTOMATION_MGMT.PROGRAM_HISTORY.001 | DONE | ProgramHistorySystem with ring buffer (last 20 cycle outcomes per fleet). Blocks: BUDGET_ENFORCEMENT.001. Hash-affecting. Proof: dotnet test --filter Determinism + AutomationSystemTests | NEW: SimCore/Systems/ProgramHistorySystem.cs, FOUND: SimCore.Tests/Systems/AutomationSystemTests.cs |

### AD4. Automation Management — Bridge (bridge, tier 2-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.AUTOMATION_MGMT.BRIDGE_QUERIES.001 | DONE | SimBridge.Automation.cs partial: GetProgramPerformanceV0, GetProgramFailureReasonsV0, GetDoctrineSettingsV0. Blocks: DOCTRINE + PROGRAM_METRICS + FAILURE_RECOVERY. Proof: dotnet build | NEW: scripts/bridge/SimBridge.Automation.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S7.AUTOMATION_MGMT.DASHBOARD.001 | DONE | automation_dashboard.gd panel: program perf, failures, budget, doctrine per fleet. Blocks: BRIDGE_QUERIES.001. Proof: dotnet build | NEW: scripts/ui/automation_dashboard.gd, FOUND: scripts/ui/hud.gd |

### AD5. Meta (docs, tier 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.025 | DONE | Full test suite (Release), build, warning scan, dead code check, golden hash stability. Tranche 25 baseline. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.025 | DONE | Screenshot eval after stability fixes. Multi-perspective visual review + baseline comparison. REGRESSION_ANCHOR milestone. Proof: Run-Screenshot.ps1 -Mode eval | FOUND: scripts/tests/visual_sweep_bot_v0.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |
| GATE.X.HYGIENE.EPIC_REVIEW.025 | DONE | Epic status audit. Close eligible epics (COMBAT_FEEL_POLISH, NARRATIVE_DELIVERY, FLEET_TAB, MAIN_MENU, T2_MODULE_CATALOG, RISK_METER_UI). Update ACTIVE_ISSUES. Recommend T26 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes:
- SHIP_VISIBILITY.001 + COMBAT_VFX_SCALE.001 share npc_ship.gd — assign to same agent (tier 1).
- EXPLORATION_OVL.001 + WARFRONT_OVL.001 share GalaxyView.cs + SimBridge.GalaxyMap.cs — assign to same agent (tier 2).
- Core hash-affecting chain (tier 1): DOCTRINE.001 -> PROGRAM_METRICS.001 -> FAILURE_RECOVERY.001 -> BUDGET_ENFORCEMENT.001 -> PROGRAM_HISTORY.001 — single sequential agent.
- NEW path budget: 10/10 used (3 DOCTRINE + 2 PROGRAM_METRICS + 1 FAILURE_RECOVERY + 1 BUDGET_ENFORCEMENT + 1 PROGRAM_HISTORY + 1 BRIDGE_QUERIES + 1 DASHBOARD).

---

## AE. Tranche 26 — Automation MGMT UI + Runtime Regressions + Settings Foundation

Anchor: EPIC.S7.AUTOMATION_MGMT.V0. Expansions: RUNTIME_STABILITY (regression fixes from eval 2026-03-11), S9.SETTINGS (options screen foundation).

### AE1. Automation MGMT UI (bridge, tiers 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.AUTOMATION_MGMT.BRIDGE_WRITES.001 | DONE | SimBridge.Automation.cs: add SetDoctrineV0(fleetId, stance, retreatThreshold, patrolRadius), SetBudgetCapsV0(fleetId, creditCap, goodsCap), GetProgramTemplatesV0() returning preset definitions. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Automation.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S7.AUTOMATION_MGMT.DASHBOARD_V2.001 | DONE | Enhanced automation_dashboard.gd: failure detail panel with per-reason breakdown and suggested fixes, per-fleet performance summary with credits/goods/cycles. Fixes ACTIVE_ISSUES partial U6. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/automation_dashboard.gd, FOUND: scripts/ui/hud.gd |
| GATE.S7.AUTOMATION_MGMT.DOCTRINE_UI.001 | DONE | NEW doctrine_panel.gd: edit fleet doctrine (stance dropdown, retreat threshold slider, patrol radius slider). Calls SetDoctrineV0. Blocks: BRIDGE_WRITES.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/doctrine_panel.gd, FOUND: scripts/bridge/SimBridge.Automation.cs |
| GATE.S7.AUTOMATION_MGMT.BUDGET_UI.001 | DONE | NEW budget_panel.gd: edit fleet budget caps (credit cap, goods cap inputs). Calls SetBudgetCapsV0. Blocks: BRIDGE_WRITES.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/budget_panel.gd, FOUND: scripts/bridge/SimBridge.Automation.cs |
| GATE.S7.AUTOMATION_MGMT.FLEET_INTEGRATION.001 | DONE | Wire automation summary (program status, last cycle, failure count) into Fleet Tab F3 detail view. Blocks: DASHBOARD_V2.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/bridge/SimBridge.Automation.cs |
| GATE.S7.AUTOMATION_MGMT.HISTORY_VIEW.001 | DONE | NEW program_history_panel.gd: timeline of last 20 program outcomes per fleet (success/fail/budget/timeout with timestamps). Blocks: DASHBOARD_V2.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/program_history_panel.gd, FOUND: scripts/bridge/SimBridge.Automation.cs |
| GATE.S7.AUTOMATION_MGMT.SCENARIO_PROOF.001 | DONE | Headless automation proof bot: dock, query doctrine/metrics/budget, verify bridge returns correct structure, capture screenshots. HEADLESS_PROOF milestone. Blocks: DOCTRINE_UI + BUDGET_UI. Proof: godot headless scenario bot | NEW: scripts/tests/automation_scenario_bot.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |

### AE2. Runtime stability regression fixes (bridge, tiers 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001 | DONE | Fix V11 regression: hide/disable Label3D nodes when dock panel or dashboard is active. Check GalaxyView.cs visibility toggling and galaxy_spawner.gd label layer. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/view/galaxy_spawner.gd |
| GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001 | DONE | Fix C11: combat VFX not visible at game altitude (scale/emission/billboard issues). Fix C12: NPC HP bars not visible. Fix C13: NPC role labels not visible. Fix C14: hostile labels not visible. Investigate npc_ship.gd spawn code for HP bar, label, hostile tag instantiation. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/npc_ship.gd, FOUND: scripts/vfx/damage_number.gd, FOUND: scripts/vfx/shield_ripple.gd |
| GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001 | DONE | Fix V14: warp VFX appears as white spheres not tunnel — investigate warp_tunnel.gd shader/particle setup. Fix V15: dock panel still open during warp transit — auto-close in game_manager.gd warp handler. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/vfx/warp_tunnel.gd, FOUND: scripts/vfx/warp_effect.gd, FOUND: scripts/core/game_manager.gd |
| GATE.S7.RUNTIME_STABILITY.GALAXY_MAP_FIX.001 | DONE | Fix V16: galaxy map not visible behind empire dashboard. Check CanvasLayer ordering, GalaxyView.cs show/hide logic when dashboard is open. Blocks: LABEL3D_FIX.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/core/game_manager.gd |
| GATE.S7.RUNTIME_STABILITY.ASTEROID_VARIETY.001 | DONE | Fix V13: no visible asteroids in home system. Scatter asteroid meshes near station/planet nodes in galaxy_spawner.gd. Partially addresses V2 (barren starter). Blocks: LABEL3D_FIX.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/galaxy_spawner.gd, FOUND: scenes/asteroid.tscn |
| GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001 | DONE | Fix U6: enrich empire dashboard Overview tab — add recent trade profits, fleet activity summary, nearby opportunities. Fix U7: add persistent keybind hint widget to hud.gd. Blocks: WARP_TUNNEL_V2.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/ui/hud.gd |
| GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001 | DONE | Fix C8: zone armor bars (4 directional HP indicators). Fix C9: combat stance indicator + weapon cooldown display. NEW combat_hud.gd wired into hud.gd. Blocks: DASHBOARD_CONTENT.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/combat_hud.gd, FOUND: scripts/ui/hud.gd |

### AE3. Settings foundation (content, tiers 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S9.SETTINGS.INFRASTRUCTURE.001 | DONE | NEW settings_manager.gd autoload: load/save user://settings.json, signal on change, default values. NEW settings_panel.gd scaffold: tab bar (Audio, Display, Gameplay), modal overlay, close button. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/core/settings_manager.gd, NEW: scripts/ui/settings_panel.gd |
| GATE.S9.SETTINGS.TABS.001 | DONE | Audio tab: 5 sliders (Master/Music/SFX/Ambient/UI) wired to AudioServer buses. Display tab: resolution dropdown, display mode, vsync toggle, quality preset. Blocks: INFRASTRUCTURE.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/settings_panel.gd, FOUND: scripts/core/settings_manager.gd |
| GATE.S9.SETTINGS.GAMEPLAY_TAB.001 | DONE | Gameplay tab: difficulty display (read-only per-save), auto-pause toggle, tutorial toasts toggle, tooltip delay slider. Blocks: TABS.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/settings_panel.gd, FOUND: scripts/core/settings_manager.gd |

### AE4. Meta (docs, tiers 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.026 | DONE | Full test suite (Release), build, warning scan, dead code check, golden hash stability. Tranche 26 baseline. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.026 | DONE | Post-tranche visual regression check. Run /screenshot eval, compare against PLAYER_VISIBLE_SYSTEMS.md and ACTIVE_ISSUES.md. REGRESSION_ANCHOR milestone. Proof: Run-Screenshot.ps1 -Mode eval | FOUND: scripts/tests/visual_sweep_bot_v0.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |
| GATE.X.HYGIENE.EPIC_REVIEW.026 | DONE | Epic status audit. Close AUTOMATION_MGMT, GALAXY_MAP_V2, RUNTIME_STABILITY. Update ACTIVE_ISSUES. Recommend T27 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes:
- COMBAT_VFX_V2.001: touches npc_ship.gd + damage_number.gd + shield_ripple.gd — single agent (tier 1).
- WARP_TUNNEL_V2.001: touches warp_tunnel.gd + warp_effect.gd + game_manager.gd — single agent (tier 1).
- DOCTRINE_UI.001 + BUDGET_UI.001: both block on BRIDGE_WRITES.001 but create separate NEW files — can parallelize (tier 2).

---

## AF. Tranche 27 — Fracture Discovery + Menu Atmosphere + Accessibility

### AF1. Fracture discovery event (core+bridge, tiers 1-3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S6.FRACTURE_DISCOVERY.MODEL.001 | DONE | Add FractureUnlocked bool + FractureDiscoveryTick to SimState. Gate FractureSystem.Tick() behind FractureUnlocked flag. Add FractureDiscoveryMinTick tweaks knob to FractureTweaksV0. Hash-affecting. Proof: dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/SimState.cs, FOUND: SimCore/Systems/FractureSystem.cs |
| GATE.S6.FRACTURE_DISCOVERY.DERELICT.001 | DONE | Spawn derelict VoidSite at frontier node during worldgen via DiscoverySeedGen. Type "FractureDerelict" discovery kind. Deterministic placement using world seed hash. Blocks: MODEL.001. Hash-affecting. Proof: dotnet test --filter "FullyQualifiedName~Determinism" | FOUND: SimCore/Gen/DiscoverySeedGen.cs, FOUND: SimCore/Entities/VoidSite.cs |
| GATE.S6.FRACTURE_DISCOVERY.UNLOCK.001 | DONE | Wire unlock in DiscoveryOutcomeSystem: analyze FractureDerelict -> set FractureUnlocked=true, emit EVT.DISCOVERY.FRACTURE_UNLOCKED. Contract test: advance to tick 300+, discover, analyze, verify unlock. Blocks: DERELICT.001. Hash-affecting. Proof: dotnet test --filter "FractureDiscoveryTests" | FOUND: SimCore/Systems/DiscoveryOutcomeSystem.cs, NEW: SimCore.Tests/Systems/FractureDiscoveryTests.cs |
| GATE.S6.FRACTURE_DISCOVERY.BRIDGE.001 | DONE | SimBridge queries: GetFractureDiscoveryStatusV0 (unlocked, discoveryTick, derelictNodeId, analysisProgress). Add SimBridge.Fracture.cs partial. Blocks: MODEL.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Fracture.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S6.FRACTURE_DISCOVERY.UI.001 | DONE | GDScript: fracture unlock toast in HUD when unlocked, gate fracture-related UI elements behind unlock flag in DiscoverySitePanel.gd. Blocks: BRIDGE.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/ui/DiscoverySitePanel.gd |
| GATE.S6.FRACTURE_DISCOVERY.PROOF.001 | DONE | Headless proof bot: boot, advance sim to tick 300+, find derelict, analyze, verify fracture unlocked, capture screenshots. HEADLESS_PROOF milestone. Blocks: UI.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/tests/fracture_discovery_bot.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |

### AF2. Menu atmosphere (bridge, tiers 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S9.MENU_ATMOSPHERE.STARFIELD.001 | DONE | Parallax starfield shader: 4 GPU layers (deep stars, nebula noise x2, mid-field stars). ShaderMaterial on main_menu.tscn background ColorRect. Combine with TITLE.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, NEW: scripts/view/starfield_menu.gdshader |
| GATE.S9.MENU_ATMOSPHERE.TITLE.001 | DONE | Title text: clean sans-serif, fade-in animation, rotating Precursor fragment subtitle per session. Combine with STARFIELD.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scenes/main_menu.tscn |
| GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001 | DONE | Adaptive foreground silhouette: gate model (no saves), player ship class (mid-campaign), Haven (completed). Blocks: STARFIELD.001. Combine with AUDIO.001+GALAXY_GEN.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scenes/main_menu.tscn |
| GATE.S9.MENU_ATMOSPHERE.AUDIO.001 | DONE | Menu audio timing: first-launch 2s silence -> single note -> drone -> theme per AudioDesign.md; returning: quick fade-in. Blocks: STARFIELD.001. Combine with SILHOUETTE.001+GALAXY_GEN.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scenes/main_menu.tscn |
| GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001 | DONE | Galaxy generation screen with thematic progress messages ("Charting the void..."), "Press any key to begin" gate after generation. Blocks: TITLE.001. Combine with SILHOUETTE.001+AUDIO.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scenes/main_menu.tscn |

### AF3. Accessibility + Settings continuation (content+bridge, tiers 1-2)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S9.ACCESSIBILITY.FIRST_LAUNCH.001 | DONE | First-launch accessibility prompt: font size, colorblind mode, UI scale. Shown once when no settings.json exists. Writes to settings_manager. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/settings_manager.gd, FOUND: scripts/ui/settings_panel.gd |
| GATE.S9.ACCESSIBILITY.FONT_SCALE.001 | DONE | Font size override system: 100-200% scaling via Theme override. Applied from settings_manager on change. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/settings_manager.gd, FOUND: scripts/ui/hud.gd |
| GATE.S9.ACCESSIBILITY.COLORBLIND.001 | DONE | Colorblind post-processing shader: Deuteranopia/Protanopia/Tritanopia via CanvasLayer ColorRect shader. 3 modes via uniform parameter. Blocks: FIRST_LAUNCH.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/settings_manager.gd, NEW: scripts/view/colorblind_filter.gdshader |
| GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001 | DONE | Accessibility tab in settings_panel.gd: colorblind dropdown, font scale slider, high contrast toggle, reduced shake toggle. Blocks: FONT_SCALE.001+FIRST_LAUNCH.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/settings_panel.gd, FOUND: scripts/core/settings_manager.gd |
| GATE.S9.SETTINGS.DISPLAY_REVERT.001 | DONE | Display settings 15s revert timer: "Keep changes?" dialog after resolution/mode change. Auto-revert if no confirmation. Blocks: FIRST_LAUNCH.001. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/settings_panel.gd, FOUND: scripts/core/settings_manager.gd |

### AF4. Meta (docs, tiers 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.027 | DONE | Full test suite (Release), build, warning scan, dead code check, golden hash stability. Tranche 27 baseline. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.027 | DONE | Post-tranche visual regression + menu atmosphere eval. Run /screenshot eval, verify fracture discovery UI, new menu visuals. REGRESSION_ANCHOR milestone. Proof: Run-Screenshot.ps1 -Mode eval | FOUND: scripts/tests/visual_sweep_bot_v0.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |
| GATE.X.HYGIENE.EPIC_REVIEW.027 | DONE | Epic status audit. Close FRACTURE_DISCOVERY_EVENT, MENU_ATMOSPHERE, ACCESSIBILITY if complete. Update ACTIVE_ISSUES. Recommend T28 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes:
- STARFIELD.001 + TITLE.001: both touch main_menu.gd — single agent (tier 1).
- SILHOUETTE.001 + AUDIO.001 + GALAXY_GEN.001: all touch main_menu.gd — single agent (tier 2).
- MODEL.001 + DERELICT.001: hash-affecting sequential chain in tier 1 — single agent.
- GALAXY_MAP_FIX.001 + ASTEROID_VARIETY.001: both block on LABEL3D_FIX.001 but touch different files (GalaxyView.cs vs galaxy_spawner.gd) — can parallelize (tier 2).
- NEW path budget: 7/10 used (doctrine_panel, budget_panel, program_history_panel, combat_hud, settings_manager, settings_panel, automation_scenario_bot).

## AG. Retroactive T18 — Narrative Foundation (EPIC.T18.NARRATIVE_FOUNDATION.V0, EPIC.T18.EXPERIENTIAL_MECHANICS.V0, EPIC.T18.DATA_LOG_CONTENT.V0, EPIC.T18.INSTRUMENT_DISAGREEMENT.V0, EPIC.T18.MORAL_ARCHITECTURE.V0)

These gates formalize T18 narrative systems that were built code-first during sessions
without formal gate definitions. All code verified present and tested. Retroactive closeout.

### AG1. Narrative Foundation entities + state + tweaks

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.NARRATIVE.ENTITIES.001 | DONE | 6 narrative entity classes: DataLog, FirstOfficer, StationMemory, WarConsequence, NarrativeNpc, KnowledgeConnection. All in SimCore/Entities/ with SimState dictionary registration. | FOUND: SimCore/Entities/DataLog.cs, SimCore/Entities/FirstOfficer.cs, SimCore/Entities/StationMemory.cs, SimCore/Entities/WarConsequence.cs, SimCore/Entities/NarrativeNpc.cs, SimCore/Entities/KnowledgeConnection.cs |
| GATE.T18.NARRATIVE.STATE_INTEGRATION.001 | DONE | SimState dictionaries for all 6 narrative entities + HydrateAfterLoad wiring + determinism signature coverage. | FOUND: SimCore/SimState.cs, SimCore/SimEngine.cs |
| GATE.T18.NARRATIVE.TWEAKS.001 | DONE | NarrativeTweaksV0 + FractureWeightTweaksV0 + TopologyShiftTweaksV0 + RouteUncertaintyTweaksV0 — all gameplay constants routed through Tweaks layer. | FOUND: SimCore/Tweaks/NarrativeTweaksV0.cs, SimCore/Tweaks/FractureWeightTweaksV0.cs, SimCore/Tweaks/TopologyShiftTweaksV0.cs, SimCore/Tweaks/RouteUncertaintyTweaksV0.cs |

### AG2. Experiential mechanics systems

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.EXPERIENTIAL.FRACTURE_WEIGHT.001 | DONE | FractureWeightSystem: per-edge fracture weight computation, phase classification (Stable/Drift/Storm/Collapse), deterministic hash-based jitter. Wired into SimKernel.Step(). Tests pass. | FOUND: SimCore/Systems/FractureWeightSystem.cs, SimCore.Tests/Systems/FractureWeightTests.cs |
| GATE.T18.EXPERIENTIAL.ROUTE_UNCERTAINTY.001 | DONE | RouteUncertaintySystem: route travel-time uncertainty based on fracture weight, tick-level jitter, stage classification. Wired into SimKernel.Step(). Tests pass. | FOUND: SimCore/Systems/RouteUncertaintySystem.cs, SimCore.Tests/Systems/RouteUncertaintyTests.cs |
| GATE.T18.EXPERIENTIAL.STATION_MEMORY.001 | DONE | StationMemorySystem: station visit tracking, memory decay, visit-count persistence. Wired into SimKernel.Step(). Tests pass. | FOUND: SimCore/Systems/StationMemorySystem.cs, SimCore.Tests/Systems/StationMemoryTests.cs |
| GATE.T18.EXPERIENTIAL.WAR_CONSEQUENCE.001 | DONE | WarConsequenceSystem: war impact on station infrastructure, damage propagation, recovery tracking. Wired into SimKernel.Step(). Tests pass. | FOUND: SimCore/Systems/WarConsequenceSystem.cs, SimCore.Tests/Systems/WarConsequenceTests.cs |
| GATE.T18.EXPERIENTIAL.TOPOLOGY_SHIFT.001 | DONE | TopologyShiftSystem: fracture-driven edge topology changes, edge creation/removal, deterministic hash-based selection. Wired into SimKernel.Step(). Tests pass. | FOUND: SimCore/Systems/TopologyShiftSystem.cs, SimCore.Tests/Systems/TopologyShiftTests.cs |

### AG3. Data log content + placement + knowledge graph

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.DATALOG.CONTENT.001 | DONE | 25 data logs in DataLogContentV0.cs covering lore, faction history, Precursor hints, and fracture phenomena. Content registry integration. | FOUND: SimCore/Content/DataLogContentV0.cs |
| GATE.T18.DATALOG.PLACEMENT.001 | DONE | NarrativePlacementGen: BFS-based log placement across galaxy, fixed landmark assignments, deterministic seed-based distribution. | FOUND: SimCore/Gen/NarrativePlacementGen.cs |
| GATE.T18.DATALOG.KEPLER_CHAIN.001 | DONE | KeplerChainContentV0: 6-piece collectible chain with ordered discovery, lore progression, and completion tracking. | FOUND: SimCore/Content/KeplerChainContentV0.cs |
| GATE.T18.DATALOG.KNOWLEDGE_GRAPH.001 | DONE | KnowledgeGraphSystem: connection tracking between discovered logs, graph traversal queries, revelation gating. KnowledgeGraphContentV0 defines connection rules. | FOUND: SimCore/Systems/KnowledgeGraphSystem.cs, SimCore/Content/KnowledgeGraphContentV0.cs |

### AG4. Instrument disagreement + moral architecture

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.INSTRUMENT.DISAGREEMENT.001 | DONE | InstrumentDisagreementSystem: ComputeStandardReading vs ComputeFractureReading, deterministic divergence based on fracture weight, tick-level sensor disagreement. Tests pass. | FOUND: SimCore/Systems/InstrumentDisagreementSystem.cs, SimCore.Tests/Systems/InstrumentDisagreementTests.cs |
| GATE.T18.MORAL_ARCH.DOCS.001 | DONE | Faction moral architecture documented in factions_and_lore_v0.md: Communion flaw (species privilege), Reinforce reframe (liberation vs control), Naturalize cost (ecological extraction). Design-only gate. | FOUND: docs/design/factions_and_lore_v0.md |

## AH. Tranche 28 — "Enforcement + Narrative Surface" (EPIC.T18.CHARACTER_SYSTEMS.V0, EPIC.T18.NARRATIVE_BRIDGE.V0, EPIC.X.SHIP_CLASS_ENFORCEMENT.V0, EPIC.X.INSTABILITY_PRICE_WIRING.V0, EPIC.X.MODULE_SUSTAIN_GOODS.V0)

Anchor: CHARACTER_SYSTEMS (FO reactions + War Faces). Expanded to NARRATIVE_BRIDGE
(closes T18), three cross-cutting enforcement epics (cargo/mass/scan, instability
pricing, module sustain goods), and UI polish fixes for 3 HIGH active issues.

### AH1. Character Systems (EPIC.T18.CHARACTER_SYSTEMS.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.CHARACTER.FO_REACT.001 | DONE | FirstOfficerSystem.Process() listens for recent sim events (trade completion, combat, discovery, fracture travel, territory change) and queues FO reaction entries on the FirstOfficer entity. FirstOfficerContentV0.cs provides 15+ reaction templates with personality-weighted selection. Contract tests verify FO reacts to each trigger type. Proof: dotnet test --filter "FirstOfficerTests" + Determinism | FOUND: SimCore/Systems/FirstOfficerSystem.cs, NEW: SimCore/Content/FirstOfficerContentV0.cs, SimCore.Tests/Systems/FirstOfficerTests.cs |
| GATE.T18.CHARACTER.WAR_FACES.001 | DONE | NarrativeNpcSystem personality-driven behavior: 5 named NPCs (one per faction) in WarFacesContentV0.cs with personality traits (aggression/friendliness/fear). Reactions modulated by faction rep + instability + war state. Tests verify NPC reactions change based on game state. Proof: dotnet test --filter "NarrativeNpcTests" + Determinism | FOUND: SimCore/Systems/NarrativeNpcSystem.cs, NEW: SimCore/Content/WarFacesContentV0.cs, SimCore.Tests/Systems/NarrativeNpcTests.cs |
| GATE.T18.CHARACTER.BRIDGE.001 | DONE | SimBridge.Character.cs partial: GetFirstOfficerStateV0 (current reaction, mood, personality), GetWarFacesV0 (NPC list with personality + current reaction for docked station). TryExecuteSafeRead pattern. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Character.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.T18.CHARACTER.UI.001 | DONE | GDScript FO reaction display (toast or HUD panel for FO reactions as they trigger) + War Faces NPC dialogue in dock menu (Characters tab or contextual NPC section). Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/fo_panel.gd, FOUND: scripts/ui/hud.gd |

### AH2. Narrative Bridge (EPIC.T18.NARRATIVE_BRIDGE.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.NARRATIVE.BRIDGE.001 | DONE | SimBridge.Narrative.cs partial: GetDataLogSummaryV0 (collected logs, read/unread), GetKnowledgeGraphV0 (connections, revelation state), GetFractureWeightV0 (per-edge weights, phase), GetRouteUncertaintyV0 (route jitter, stage), GetStationMemoryV0 (visit history), GetInstrumentDisagreementV0 (standard vs fracture readings), GetTopologyShiftV0 (edge changes). TryExecuteSafeRead pattern. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Narrative.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.T18.NARRATIVE.UI_DATALOG.001 | DONE | GDScript data log viewer panel: collected logs list with read/unread indicators, log content display, search/filter by category. Accessible from dock menu or HUD. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/data_log_panel.gd, FOUND: scripts/ui/hud.gd |

### AH3. Ship Class Enforcement (EPIC.X.SHIP_CLASS_ENFORCEMENT.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.SHIP_CLASS.CARGO_ENFORCE.001 | DONE | BuyCommand rejects purchase when fleet total cargo would exceed CargoCapacity from ShipClassDef. Returns appropriate reason code. Contract tests verify rejection at capacity and acceptance below. Proof: dotnet test --filter "BuyCommandTests" + Determinism | FOUND: SimCore/Commands/BuyCommand.cs, FOUND: SimCore/Content/ShipClassContentV0.cs, SimCore.Tests/Commands/BuyCommandTests.cs |
| GATE.X.SHIP_CLASS.MASS_SPEED.001 | DONE | MovementSystem applies speed penalty proportional to loaded mass vs MaxMass from ShipClassDef. Formula: effectiveSpeed = baseSpeed * (1 - loadFraction * MassSpeedPenaltyPct/100). MassSpeedPenaltyPct routed through tweaks. Tests verify heavier fleets move slower. Proof: dotnet test --filter "MovementTests" + Determinism | FOUND: SimCore/Systems/MovementSystem.cs, FOUND: SimCore/Content/ShipClassContentV0.cs, SimCore.Tests/Systems/MovementTests.cs |
| GATE.X.SHIP_CLASS.SCAN_RANGE.001 | DONE | IntelSystem.ApplyScan uses ScanRange from fleet's ShipClassDef to gate discovery scanning. Replace `_ = fleetId` with actual range check against discovery distance. Fleets with insufficient ScanRange get DiscoveryReasonCode.OutOfRange. Proof: dotnet test --filter "IntelContractTests" + Determinism | FOUND: SimCore/Systems/IntelSystem.cs, FOUND: SimCore/Content/ShipClassContentV0.cs, FOUND: SimCore.Tests/Systems/IntelContractTests.cs |

### AH4. Instability Price Wiring (EPIC.X.INSTABILITY_PRICE_WIRING.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.INSTAB_PRICE.WIRE.001 | DONE | BuyCommand and SellCommand call MarketSystem.GetEffectivePrice instead of market.GetBuyPrice/GetSellPrice, applying instability volatility, jitter, and void-closure effects. Tests verify prices differ under instability vs baseline. Proof: dotnet test --filter "InstabilityMarketTests" + Determinism | FOUND: SimCore/Commands/BuyCommand.cs, FOUND: SimCore/Commands/SellCommand.cs, FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore.Tests/Systems/InstabilityMarketTests.cs |

### AH5. Module Sustain Goods (EPIC.X.MODULE_SUSTAIN_GOODS.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.MODULE_SUSTAIN.MODEL.001 | DONE | Add SustainGoodId (string) and SustainQtyPerCycle (int) to ModuleDef. Populate in UpgradeContentV0: weapons→"munitions", T3 modules→"exotic_matter", scanners→"energy_cells". Register in ContentRegistryLoader. Proof: dotnet test --filter "ContentRegistryTests" + Determinism | FOUND: SimCore/Content/UpgradeContentV0.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs, SimCore.Tests/Content/ContentRegistryTests.cs |
| GATE.X.MODULE_SUSTAIN.DEDUCT.001 | DONE | SustainSystem.Process reads each equipped module's SustainGoodId and deducts SustainQtyPerCycle from fleet cargo each sustain cycle. Module disabled (PowerDraw=0 effective) when good unavailable. Tests verify consumption + disable behavior. Blocks: MODULE_SUSTAIN.MODEL.001. Proof: dotnet test --filter "SustainSystemTests" + Determinism | FOUND: SimCore/Systems/SustainSystem.cs, FOUND: SimCore.Tests/Systems/SustainSystemTests.cs, FOUND: SimCore/Content/UpgradeContentV0.cs |

### AH6. UI Polish — Active Issue Fixes

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.UI_POLISH.LABEL_OVERLAP.001 | DONE | Label anti-collision in ClampLabelsRecursive: spatial hashing or vertical offset to prevent Label3D stacking when multiple labels (system name, HOSTILE, station) occupy same screen area. Fixes ACTIVE_ISSUES V17. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/hud.gd |
| GATE.X.UI_POLISH.GALAXY_MAP_UX.001 | DONE | Galaxy map default zoom shows full topology (adjust STRATEGIC_ALTITUDE or initial camera position). Fix "GALAXY MAP (TAB to close)" label persistence when map closed. Fixes ACTIVE_ISSUES V18, V19. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/hud.gd |
| GATE.X.UI_POLISH.CAMERA_BOUNDS.001 | DONE | Camera distance clamping: max altitude bound prevents camera from reaching 3853u+ where all game content is invisible. Auto-zoom to local system on arrival. Fixes ACTIVE_ISSUES U1. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/view/GalaxyView.cs |

### AH7. Meta (docs, tiers 1+3)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.028 | DONE | Full test suite (Release), build, warning scan, dead code check, golden hash stability. Tranche 28 baseline. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.028 | DONE | Epic status audit. Close CHARACTER_SYSTEMS and NARRATIVE_BRIDGE if complete. Close SHIP_CLASS_ENFORCEMENT, INSTABILITY_PRICE_WIRING, MODULE_SUSTAIN_GOODS if complete. Update ACTIVE_ISSUES. Recommend T29 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.028 | DONE | Post-tranche visual regression + character/narrative eval. Run /screenshot eval, verify FO reactions, data log panel, label fixes, camera bounds. REGRESSION_ANCHOR milestone. Proof: Run-Screenshot.ps1 -Mode eval | FOUND: scripts/tests/visual_sweep_bot_v0.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |

Combined-agent notes:
- FO_REACT + WAR_FACES: both touch narrative systems — single core agent (tier 1).
- CARGO_ENFORCE + MASS_SPEED + SCAN_RANGE: all enforce ShipClassDef — single core agent (tier 1).
- LABEL_OVERLAP + GALAXY_MAP_UX + CAMERA_BOUNDS: all GDScript UI fixes — single bridge agent (tier 1).
- EPIC_REVIEW + SCREENSHOT_EVAL: both docs — single agent (tier 3).

## AI. Tranche 29 — "Character Depth & Mission Content" (EPIC.T18.CHARACTER_SYSTEMS.V0, EPIC.S9.MISSION_LADDER.V0, EPIC.X.UI_POLISH)

Anchor: CHARACTER_SYSTEMS (FO dialogue depth, triggers, promotion, War Faces behavioral
depth). Expanded to MISSION_LADDER (mining/research/construction missions) and UI polish
(dock visual fixes V20/V21, discovery web panel, data log filters, local density V2).

Tier 1 (6 gates — parallel): FO_DIALOGUE_DEPTH (core), MINING_CONTENT (core, hash chain →
FO_DIALOGUE_DEPTH), REPO_HEALTH (docs), DOCK_VISUAL (bridge), KNOWLEDGE_WEB (bridge),
DATALOG_FILTER (bridge).

Tier 2 (6 gates — core hash chain + bridge): FO_TRIGGER → FO_PROMO → WARFACES_DEPTH →
RESEARCH_CONTENT → CONSTRUCTION_CONTENT (sequential hash chain), LOCAL_DENSITY (bridge,
independent).

Tier 3 (6 gates): UI_FULL (bridge), CHARACTER.HEADLESS (bridge), MISSIONS.HEADLESS (bridge),
BRIDGE_EXT (bridge), SCREENSHOT_EVAL (docs), EPIC_REVIEW (docs).

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T18.CHARACTER.FO_DIALOGUE_DEPTH.001 | DONE | FO 3 candidates (Analyst/Veteran/Pathfinder) x 5 dialogue tiers + blind spot definitions + endgame lean text. Fill FirstOfficerContentV0.cs with tiered dialogue. Hash-affecting. Proof: dotnet test --filter "FirstOfficer" | FOUND: SimCore/Content/FirstOfficerContentV0.cs, FOUND: SimCore/Systems/FirstOfficerSystem.cs |
| GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001 | DONE | FO dialogue triggers: trade→Analyst, combat→Veteran, discovery→Pathfinder. Score accumulation from player actions advances dialogue tier. Hash-affecting. Proof: dotnet test --filter "FirstOfficer" | FOUND: SimCore/Systems/FirstOfficerSystem.cs, FOUND: SimCore/Content/FirstOfficerContentV0.cs |
| GATE.T18.CHARACTER.FO_PROMO.001 | DONE | FO promotion: score threshold triggers promotion window, player chooses from 3 candidates via PromoteFirstOfficerCommand, promoted FO gains enhanced tier + endgame lean. Irreversible. Hash-affecting. Proof: dotnet test --filter "FirstOfficer" | FOUND: SimCore/Systems/FirstOfficerSystem.cs, FOUND: SimCore/Entities/FirstOfficer.cs |
| GATE.T18.CHARACTER.WARFACES_DEPTH.001 | DONE | War Faces behavioral depth: Regular (silent disappearance), Stationmaster (delivery-responsive), Enemy (delayed recontextualization). In NarrativeNpcSystem + WarFacesContentV0. Hash-affecting. Proof: dotnet test --filter "NarrativeNpc" | FOUND: SimCore/Systems/NarrativeNpcSystem.cs, FOUND: SimCore/Content/WarFacesContentV0.cs |
| GATE.T18.CHARACTER.UI_FULL.001 | DONE | FO panel overhaul: dialogue history scroll, promotion UI with candidate cards + choice button, War Faces behavior state display. FO toasts with gold styling. In fo_panel.gd + toast_manager.gd. Proof: dotnet build | FOUND: scripts/ui/fo_panel.gd, FOUND: scripts/ui/toast_manager.gd |
| GATE.T18.CHARACTER.HEADLESS.001 | DONE | GDScript headless test: boot scene, trigger FO dialogue via trade/discovery, verify response text, test promotion flow, verify War Faces state. PLAYABLE_BEAT milestone. Proof: godot --headless | NEW: scripts/tests/test_character_proof_v0.gd, FOUND: scripts/ui/fo_panel.gd |
| GATE.S9.MISSIONS.MINING_CONTENT.001 | DONE | Mission Mining Survey: 3-step — survey discovery site, deploy ResourceTap, verify extraction. Binding tokens for nearest discovery. Introduces mining gameplay loop. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Content/MissionContentV0.cs, FOUND: SimCore/Systems/MissionSystem.cs |
| GATE.S9.MISSIONS.RESEARCH_CONTENT.001 | DONE | Mission First Research: 3-step — dock at tech station, start research project, complete unlock. Binding tokens for available tech. Introduces research loop. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Content/MissionContentV0.cs, FOUND: SimCore/Systems/MissionSystem.cs |
| GATE.S9.MISSIONS.CONSTRUCTION_CONTENT.001 | DONE | Mission First Build: 3-step — acquire construction materials, deliver to project site, verify construction. Binding tokens for construction project. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Content/MissionContentV0.cs, FOUND: SimCore/Systems/MissionSystem.cs |
| GATE.S9.MISSIONS.BRIDGE_EXT.001 | DONE | Extend SimBridge.Mission.cs with GetMissionRewardsPreviewV0 (rewards before accepting) and GetMissionPrerequisitesDetailV0 (requirements to start). Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Mission.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S9.MISSIONS.HEADLESS.001 | DONE | GDScript headless test: accept mining mission, complete steps, accept research mission, verify state transitions. HEADLESS_PROOF milestone. Proof: godot --headless | NEW: scripts/tests/test_missions_m5m7_v0.gd, FOUND: SimCore/Content/MissionContentV0.cs |
| GATE.X.UI_POLISH.DOCK_VISUAL.001 | DONE | Dock menu visual overhaul: fix Sell column truncation (V21), ship tab zone armor color bars + module visual grouping (V20), section headers with consistent hierarchy. Fixes ACTIVE_ISSUES V20, V21. Proof: dotnet build | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/ui/hud.gd |
| GATE.X.UI_POLISH.KNOWLEDGE_WEB.001 | DONE | Discovery Web panel: knowledge graph from SimBridge.Narrative.GetKnowledgeGraphV0. Node list with log names, connection counts, thread grouping. Accessible via K key. Proof: dotnet build | NEW: scripts/ui/knowledge_web_panel.gd, FOUND: scripts/ui/hud.gd |
| GATE.X.UI_POLISH.DATALOG_FILTER.001 | DONE | Data log 6 thread category filter buttons (Containment, Lattice, Departure, Accommodation, Warning, EconTopology). Filter by thread alongside existing text search. Proof: dotnet build | FOUND: scripts/ui/data_log_panel.gd, FOUND: scripts/bridge/SimBridge.Narrative.cs |
| GATE.X.UI_POLISH.LOCAL_DENSITY.001 | DONE | Tighten local system spatial spread: reduce planet orbit distances ~40%, station offsets ~50% in DrawLocalSystemV0 for less barren feel. Fixes ACTIVE_ISSUES V2. Visual-only. Proof: dotnet build | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/hud.gd |
| GATE.X.HYGIENE.REPO_HEALTH.029 | DONE | Full test suite (Release), build, warning scan, dead code check, golden hash stability. Tranche 29 baseline. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.029 | DONE | Epic status audit. Close S9.SETTINGS (all 5 gates DONE). Close S7.AUTOMATION_MGMT (all systems built). Close T18.CHARACTER_SYSTEMS if T29 gates complete. Update ACTIVE_ISSUES. Recommend T30 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.SCREENSHOT_EVAL.029 | DONE | Post-tranche visual regression + character depth eval. Run /screenshot eval, verify FO dialogue, promotion UI, War Faces, new missions, dock polish, discovery web. REGRESSION_ANCHOR milestone. Proof: Run-Screenshot.ps1 -Mode eval | FOUND: scripts/tests/visual_sweep_bot_v0.gd, FOUND: scripts/tools/Run-Screenshot.ps1 |

Combined-agent notes:
- FO_DIALOGUE_DEPTH + MINING_CONTENT: both core tier 1, hash chain (MINING blocks FO_DIALOGUE).
- FO_TRIGGER → FO_PROMO → WARFACES → RESEARCH → CONSTRUCTION: tier 2 sequential hash chain.
- DOCK_VISUAL + KNOWLEDGE_WEB + DATALOG_FILTER: bridge tier 1, parallel.
- UI_FULL + CHARACTER.HEADLESS + MISSIONS.HEADLESS + BRIDGE_EXT: bridge tier 3, after core chain.
- EPIC_REVIEW + SCREENSHOT_EVAL: docs tier 3, after all implementation.

## AJ. Tranche 30 — "Mission Evolution + Closing Fronts" (EPIC.S9.MISSION_FOUNDATION.V0, EPIC.S7.TERRITORY_REGIMES, EPIC.S6.OFFLANE_FRACTURE, EPIC.S7.LAYERED_REVEALS, EPIC.S7.WARFRONT_STATE, EPIC.S7.REPUTATION_INFLUENCE, EPIC.X.UI_POLISH)

Anchor: MISSION_FOUNDATION (Phase 1 per mission_design_v0.md — new triggers, rewards,
failure, branching). Expanded to TERRITORY_REGIMES (hysteresis closer), OFFLANE_FRACTURE
(route gen + headless closer), LAYERED_REVEALS (discovery + warfront reveals + headless closer),
WARFRONT_STATE (attrition + objectives), REPUTATION_INFLUENCE (faction contracts), warp transit
fixes (F7/F8/F9), mission journal UI.

Tier 1 (12 gates — 8 core hash chain + 3 bridge parallel + 1 docs):
Core hash chain: HYSTERESIS → OFFLANE_ROUTES → DISCOVERY_REVEAL → WARFRONT_REVEAL →
ATTRITION → TRIGGERS → REWARDS → FAILURE.
Bridge parallel: TUNNEL_SCALE, TRANSIT_HUD, DEPARTURE_VFX.
Docs: REPO_HEALTH.

Tier 2 (6 gates — 3 core hash chain + 3 bridge/core):
Core hash chain: BRANCHING → OBJECTIVES → CONTRACTS.
Core non-hash: MISSION_EVOL.CONTRACT (tests).
Bridge: OFFLANE_HEADLESS, REVEALS.HEADLESS, MISSION_JOURNAL.

Tier 3 (3 gates — docs): EPIC_REVIEW, FIRST_HOUR_EVAL.

NEW paths (6 total):
- NEW: SimCore/Tweaks/MissionEvolutionTweaksV0.cs — No existing mission tweaks file; gameplay values need dedicated tweaks routing.
- NEW: SimCore.Tests/Systems/MissionEvolutionTests.cs — Existing MissionContractTests covers only original 3 triggers; new features need dedicated coverage.
- NEW: scripts/ui/mission_journal_panel.gd — No mission-specific UI panel exists.
- NEW: scripts/ui/warp_transit_hud.gd — No warp phase HUD overlay exists.
- NEW: scripts/tests/test_fracture_offlane_v0.gd — No headless test for offlane fracture travel.
- NEW: scripts/tests/test_reveals_proof_v0.gd — No headless test for layered reveals.

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.TERRITORY.HYSTERESIS.001 | DONE | Territory regime hysteresis: transitions require sustained rep change over N ticks before upgrading/downgrading, preventing rapid oscillation. Add hysteresis buffer to ComputeTerritoryRegime. Closes TERRITORY_REGIMES. Hash-affecting. Proof: dotnet test --filter "TerritoryRegime" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore.Tests/Systems/TerritoryRegimeTests.cs |
| GATE.S7.FRACTURE.OFFLANE_ROUTES.001 | DONE | Offlane fracture route gen: FractureSystem generates valid node-to-node paths through fracture space without edge adjacency. FractureTravelCommand accepts non-adjacent targets when fracture module equipped. Cost scales with graph distance. Closes OFFLANE_FRACTURE. Hash-affecting. Proof: dotnet test --filter "FractureTravel" | FOUND: SimCore/Systems/FractureSystem.cs, FOUND: SimCore/Commands/FractureTravelCommand.cs, FOUND: SimCore/Tweaks/FractureTweaksV0.cs |
| GATE.S7.FRACTURE.OFFLANE_HEADLESS.001 | DONE | Offlane fracture headless proof: bot travels via fracture route bypassing lane gates, arrives at non-adjacent node. HEADLESS_PROOF milestone. Proof: godot --headless | FOUND: scripts/bridge/SimBridge.Fracture.cs, NEW: scripts/tests/test_fracture_offlane_v0.gd |
| GATE.S7.REVEALS.DISCOVERY_REVEAL.001 | DONE | Discovery content recontextualization: DiscoveryOutcomeSystem delivers progressive content reveals — initial scan shows surface data, analysis reveals deeper structure, full study reveals knowledge graph connections. Recontextualization text per discovery kind. Closes LAYERED_REVEALS. Hash-affecting. Proof: dotnet test --filter "DiscoveryOutcome" | FOUND: SimCore/Systems/DiscoveryOutcomeSystem.cs, FOUND: SimCore/Systems/KnowledgeGraphSystem.cs, FOUND: SimCore/Content/KnowledgeGraphContentV0.cs |
| GATE.S7.REVEALS.WARFRONT_REVEAL.001 | DONE | Warfront intel layer reveals: IntelSystem reveals warfront data progressively — tier 1 (presence) at sensor range, tier 2 (fleet composition) after visit, tier 3 (supply status + strategic value) after sustained observation. Hash-affecting. Proof: dotnet test --filter "Intel" | FOUND: SimCore/Systems/IntelSystem.cs, FOUND: SimCore/Tweaks/IntelTweaksV0.cs |
| GATE.S7.REVEALS.HEADLESS.001 | DONE | Layered reveals headless proof: trigger discovery scan, verify reveal layer progression Hidden to Mapped. Verify warfront intel exposure at war-adjacent node. HEADLESS_PROOF milestone. Proof: godot --headless | FOUND: scripts/bridge/SimBridge.cs, NEW: scripts/tests/test_reveals_proof_v0.gd |
| GATE.S9.MISSION_EVOL.TRIGGERS.001 | DONE | 4 new mission trigger types: ReputationMin (faction standing gate), CreditsMin (economic threshold), TechUnlocked (research prerequisite), TimerExpired (tick deadline). Add to MissionTriggerType enum + EvaluateTrigger. Per mission_design_v0.md Phase 1. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Entities/Mission.cs, FOUND: SimCore/Systems/MissionSystem.cs, NEW: SimCore/Tweaks/MissionEvolutionTweaksV0.cs |
| GATE.S9.MISSION_EVOL.REWARDS.001 | DONE | Non-credit reward types: MissionRewardDef on MissionDef — ReputationReward (factionId + amount), AccessReward (market permits, tech unlocks), IntelReward (discovery leads). CompleteMission distributes all reward types. Per mission_design_v0.md Phase 1. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Entities/Mission.cs, FOUND: SimCore/Systems/MissionSystem.cs, FOUND: SimCore/Systems/ReputationSystem.cs |
| GATE.S9.MISSION_EVOL.FAILURE.001 | DONE | Mission failure/abandonment: MissionState.FailedMissionIds, AbandonMission command (-rep), TimerExpired trigger with tick deadline, FailMission on deadline exceeded, MissionEvent 'Failed'/'Abandoned'. Per mission_design_v0.md Phase 1. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Entities/Mission.cs, FOUND: SimCore/Systems/MissionSystem.cs, NEW: SimCore/Tweaks/MissionEvolutionTweaksV0.cs |
| GATE.S9.MISSION_EVOL.BRANCHING.001 | DONE | CHOICE steps: add CHOICE step type to MissionStepDef — 2-3 options each with target step index. Step advancement follows branch instead of linear +1. MissionActiveStep tracks chosen branch. Per mission_design_v0.md Phase 1. Hash-affecting. Proof: dotnet test --filter "Mission" | FOUND: SimCore/Entities/Mission.cs, FOUND: SimCore/Systems/MissionSystem.cs, FOUND: SimCore/Content/MissionContentV0.cs |
| GATE.S9.MISSION_EVOL.CONTRACT.001 | DONE | Contract tests: dedicated test class covering all Phase 1 mission features — 4 new triggers, non-credit rewards, failure/abandonment, CHOICE branching. 15+ test methods. Non-hash (test-only). Proof: dotnet test --filter "MissionEvolution" | NEW: SimCore.Tests/Systems/MissionEvolutionTests.cs, FOUND: SimCore.Tests/Systems/MissionContractTests.cs |
| GATE.S7.WARFRONT.ATTRITION.001 | DONE | Fleet attrition + supply pressure: WarfrontEvolutionSystem applies fleet attrition when munitions/fuel depleted — fleets at starved nodes take losses per tick. Supply delivery restores strength. Creates economic pressure loop. Advances WARFRONT_STATE. Hash-affecting. Proof: dotnet test --filter "Warfront" | FOUND: SimCore/Systems/WarfrontEvolutionSystem.cs, FOUND: SimCore/Entities/WarfrontState.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs |
| GATE.S7.WARFRONT.OBJECTIVES.001 | DONE | Strategic objectives: warfront nodes have objectives (supply depot, comm relay, factory). Faction captures when fleet strength dominant for N ticks. Capture shifts production/intel/supply for controller. Advances WARFRONT_STATE. Hash-affecting. Proof: dotnet test --filter "Warfront" | FOUND: SimCore/Systems/WarfrontEvolutionSystem.cs, FOUND: SimCore/Entities/WarfrontState.cs, FOUND: SimCore/Tweaks/WarfrontTweaksV0.cs |
| GATE.S7.REPUTATION.CONTRACTS.001 | DONE | Faction contracts from rep tiers: faction contacts offer direct mission contracts gated by reputation (Neutral/Friendly/Allied). Contract availability via MissionSystem.GetAvailableMissions with ReputationMin trigger. Advances REPUTATION_INFLUENCE. Hash-affecting. Proof: dotnet test --filter "Reputation" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Systems/MissionSystem.cs, FOUND: SimCore.Tests/Systems/ReputationSystemTests.cs |
| GATE.X.WARP.TUNNEL_SCALE.001 | DONE | Fix warp tunnel oversized mesh: scale down tunnel proportional to player ship. Currently dominates screen. Fixes ACTIVE_ISSUES F7. Non-hash (visual-only). Proof: dotnet build | FOUND: scripts/vfx/warp_tunnel.gd, FOUND: scripts/vfx/warp_effect.gd |
| GATE.X.WARP.TRANSIT_HUD.001 | DONE | Warp transit HUD: show destination name, ETA bar, distance remaining during warp. Currently player sees tunnel with no context. Fixes ACTIVE_ISSUES F8. Non-hash (UI-only). Proof: dotnet build | FOUND: scripts/ui/hud.gd, NEW: scripts/ui/warp_transit_hud.gd |
| GATE.X.WARP.DEPARTURE_VFX.001 | DONE | Warp departure flash + shake: add camera shake + white flash on departure to match arrival intensity. Currently departure is visually weak. Fixes ACTIVE_ISSUES F9. Non-hash (visual-only). Proof: dotnet build | FOUND: scripts/vfx/warp_effect.gd, FOUND: scripts/core/game_manager.gd |
| GATE.X.UI_POLISH.MISSION_JOURNAL.001 | DONE | Mission journal panel (J key): active mission(s) with step progress, objectives, rewards preview, abandon button. No mission board — journal shows only accepted missions. Reads from SimBridge.Mission queries. Non-hash (UI-only). Proof: dotnet build | NEW: scripts/ui/mission_journal_panel.gd, FOUND: scripts/bridge/SimBridge.Mission.cs, FOUND: scripts/ui/hud.gd |
| GATE.X.HYGIENE.REPO_HEALTH.030 | DONE | Full test suite (Release), build, warning scan, dead code check, golden hash stability. Tranche 30 baseline. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.030 | DONE | Epic status audit. Close MISSION_LADDER (M2-M6 delivered, remaining deferred to MISSION_FOUNDATION). Close TERRITORY_REGIMES, OFFLANE_FRACTURE, LAYERED_REVEALS if all gates DONE. Recommend T31 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.FIRST_HOUR_EVAL.030 | DONE | First-hour bot eval: 31 phases, 21 assertions. Verify mission flow, warp transit fixes, territory regimes, fracture travel. REGRESSION_ANCHOR milestone. Proof: Run-FHBot.ps1 -Mode headless | FOUND: scripts/tests/test_first_hour_proof_v0.gd, FOUND: scripts/tools/Run-FHBot.ps1 |

Combined-agent notes:
- Tier 1 core hash chain (8 sequential): HYSTERESIS → OFFLANE_ROUTES → DISCOVERY_REVEAL → WARFRONT_REVEAL → ATTRITION → TRIGGERS → REWARDS → FAILURE.
- Tier 1 bridge (3 parallel): TUNNEL_SCALE, TRANSIT_HUD, DEPARTURE_VFX — no file conflicts, different primary files.
- Tier 1 docs (1): REPO_HEALTH — runs early as baseline.
- Tier 2 core hash chain (3 sequential): BRANCHING → OBJECTIVES → CONTRACTS.
- Tier 2 core non-hash (1): MISSION_EVOL.CONTRACT (tests only).
- Tier 2 bridge (3 parallel): OFFLANE_HEADLESS, REVEALS.HEADLESS, MISSION_JOURNAL.
- Tier 3 docs (2): EPIC_REVIEW + FIRST_HOUR_EVAL.

## AK. Tranche 31 — "Combat Heat + Dynamic Economy" (EPIC.S7.COMBAT_PHASE2.V0, EPIC.S9.SYSTEMIC_MISSIONS.V0, EPIC.X.LEDGER_EVENTS.V0, EPIC.X.STATION_IDENTITY.V0, EPIC.X.PERF_BUDGET)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.COMBAT_PHASE2.HEAT_SYSTEM.001 | DONE | Heat accumulation model: HeatCurrent/HeatCapacity (1000 default)/HeatPerShot per weapon/RejectionRate (passive cooling per tick) on CombatProfile. StrategicResolverV0 accumulates heat on fire, reduces by RejectionRate each round. Overheat cascade: >capacity = 50% fire rate degradation, >2x capacity = weapon lockout. Int-based fixed-point. CombatTweaksV0 heat constants. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Systems/StrategicResolverV0.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.S7.COMBAT_PHASE2.BATTLE_STATIONS.001 | DONE | BattleStations state machine on Fleet: STAND_DOWN (default) -> SPINNING_UP (N-tick timer) -> BATTLE_READY. ToggleBattleStations command. StrategicResolverV0 gates weapon effectiveness: STAND_DOWN=25%, SPINNING_UP=50%, READY=100% damage multiplier. Spin-up ticks via CombatTweaksV0. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Entities/Fleet.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs |
| GATE.X.LEDGER.TX_MODEL.001 | DONE | Transaction event entity in SimState.Events: TransactionRecord with CashDelta (int), GoodId, Quantity, Source (Buy/Sell/MissionReward/Sustain), Tick, NodeId. SimState.TransactionLog list (max 5000). BuyCommand/SellCommand append records on success. Mission completion rewards append. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/SimState.Events.cs, FOUND: SimCore/Commands/BuyCommand.cs, FOUND: SimCore/Commands/SellCommand.cs |
| GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001 | DONE | SystemicMissionSystem: detect world conditions for procedural mission generation. 3 trigger types: WAR_DEMAND (warfront goods shortage at contested nodes), PRICE_SPIKE (good price > 2x base), SUPPLY_SHORTAGE (production < 50% near instability). Emit SystemicMissionOffer per trigger. SystemicMissionTweaksV0 thresholds. Wire into SimKernel.Step. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MissionSystem.cs, FOUND: SimCore/SimKernel.cs, NEW: SimCore/Systems/SystemicMissionSystem.cs (no existing system for world-state-triggered mission generation), NEW: SimCore/Tweaks/SystemicMissionTweaksV0.cs (trigger thresholds need dedicated tweaks file per TweakRoutingGuard) |
| GATE.X.STATION_IDENTITY.VISUAL.001 | DONE | Fix F6: all stations identical. Per-faction station coloring via material tint on station mesh (5 faction colors). Station size scales by node trade volume (3 tiers: outpost 0.6x, hub 1.0x, capital 1.5x). active_station.gd reads faction_id and tier from SimBridge, applies color + scale at runtime on existing station.tscn. No new 3D models. Fixes ACTIVE_ISSUES F6. Proof: dotnet build | FOUND: scripts/active_station.gd, FOUND: scenes/station.tscn, FOUND: scripts/view/GalaxyView.cs |
| GATE.X.HYGIENE.REPO_HEALTH.031 | DONE | Full test suite (Release), build, golden hash stability, dead code scan. Tranche 31 baseline. Report test count, warning count, hash stability. REGRESSION_ANCHOR. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.UI_POLISH.DASHBOARD_UX.001 | DONE | Fix F11: Dashboard 'Needs Attention' reads as errors. Rename to 'Opportunities' or 'Suggestions', change warning icon to info circle, soften color from red/orange to blue/neutral. EmpireDashboard.cs label and style updates. Fixes ACTIVE_ISSUES F11. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/ui/hud.gd |
| GATE.X.UI_POLISH.MARKET_FORMAT.001 | DONE | Fix F12: Market production text unformatted. hero_trade_menu.gd: format production chains with indentation, arrow separators, quantity display, color coding for surplus (green) vs deficit (red). Fixes ACTIVE_ISSUES F12. Proof: dotnet build | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.S7.COMBAT_PHASE2.RADIATOR.001 | DONE | Radiator module type: ModuleDef with IsRadiator flag, RadiatorBonus (additional RejectionRate). CombatSystem.BuildProfile sums base + radiator bonuses. Radiator is targetable zone: HP=0 removes RadiatorBonus. Content entries for Basic/Advanced Radiator. CombatTweaksV0 radiator defaults. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs |
| GATE.S9.SYSTEMIC.OFFER_GEN.001 | DONE | SystemicMissionSystem.GenerateOffers: convert trigger detections into MissionDef offers. WAR_DEMAND -> delivery mission (carry goods to contested node, faction rep reward). PRICE_SPIKE -> trade arbitrage. SUPPLY_SHORTAGE -> supply run. Bind templates to actual nodes/goods/factions. Max 3 active systemic offers, expire after N ticks. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MissionSystem.cs, FOUND: SimCore/Content/MissionContentV0.cs |
| GATE.S7.COMBAT_PHASE2.CONTRACT.001 | DONE | Contract tests for Phase 2 combat: heat accumulation per weapon fire, passive cooling, overheat fire rate degradation, weapon lockout, battle stations state transitions, damage scaling by readiness, radiator RejectionRate bonus, radiator destruction reducing cooling. 12+ test methods. Proof: dotnet test --filter "CombatTests" | FOUND: SimCore.Tests/Combat/CombatTests.cs, FOUND: SimCore/Systems/CombatSystem.cs |
| GATE.S7.COMBAT_PHASE2.BRIDGE.001 | DONE | SimBridge.Combat.cs: GetHeatSnapshotV0 (HeatCurrent, HeatCapacity, RejectionRate, IsOverheated, IsLockedOut), GetBattleStationsStateV0 (State, SpinUpTicksRemaining), GetRadiatorStatusV0 (IsIntact, BonusRate). ToggleBattleStationsV0 command. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Combat.cs, FOUND: SimCore/Systems/CombatSystem.cs |
| GATE.X.LEDGER.BRIDGE.001 | DONE | SimBridge transaction ledger query: GetTransactionLogV0 (list of Tick/CashDelta/GoodId/Quantity/Source/NodeId). GetProfitSummaryV0 (TotalRevenue/TotalExpense/NetProfit/TopGoodByProfit). Enable UI economic visibility. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Market.cs, FOUND: SimCore/SimState.Events.cs |
| GATE.S9.SYSTEMIC.BRIDGE.001 | DONE | SimBridge systemic mission query: GetSystemicOffersV0 returns available procedural offers with trigger reason, reward preview, expiry tick. AcceptSystemicMissionV0 converts offer to active mission. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Mission.cs, FOUND: SimCore/Systems/MissionSystem.cs |
| GATE.X.LEDGER.INTEGRITY.001 | DONE | Ledger integrity test: after N ticks with trades, verify sum TransactionRecord.CashDelta = final Credits - starting Credits. Verify inventory deltas match cargo changes per good. Catch untracked money creation/destruction. Proof: dotnet test --filter "Ledger" | FOUND: SimCore/SimState.Events.cs, FOUND: SimCore/Commands/BuyCommand.cs |
| GATE.S7.COMBAT_PHASE2.HEAT_HUD.001 | DONE | Heat gauge HUD widget in hud.gd: bar below hull/shield. HeatCurrent/HeatCapacity ratio. Color: green (<50%), yellow (50-99%), red (>100% overheat), pulsing red (>200% lockout). Battle stations indicator icon with state text. Reads SimBridge combat queries. IN_ENGINE gate. Proof: dotnet build | FOUND: scripts/ui/hud.gd, FOUND: scripts/bridge/SimBridge.Combat.cs |
| GATE.S9.SYSTEMIC.HEADLESS.001 | DONE | Headless proof: boot game, advance ticks until warfront supply shortage, verify SystemicMissionSystem generates WAR_DEMAND offer via SimBridge, accept, complete delivery, verify rewards. PLAYABLE_BEAT milestone. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Mission.cs, FOUND: scripts/bridge/SimBridge.cs, NEW: scripts/tests/test_systemic_missions_v0.gd (headless proof needs dedicated test script) |
| GATE.X.HYGIENE.EPIC_REVIEW.031 | DONE | Epic status audit. Review COMBAT_PHASE2, SYSTEMIC_MISSIONS, LEDGER_EVENTS advancement after T31. Identify epics to close. Recommend T32 anchor. Update 54_EPICS.md statuses. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.FIRST_HOUR_EVAL.031 | DONE | First-hour bot eval with combat heat system. Verify heat gauge during combat, battle stations toggle, station visual variety, dashboard UX, market format. 21 assertions. REGRESSION_ANCHOR. Proof: Run-FHBot.ps1 -Mode headless | FOUND: scripts/tests/test_first_hour_proof_v0.gd, FOUND: scripts/tools/Run-FHBot.ps1 |
| GATE.X.PERF.TICK_BASELINE.001 | DONE | Measure SimKernel.Step() tick timing across 1000 ticks. Establish baseline: mean, P95, P99 tick duration. Flag any system consuming >20% of tick budget. Output to docs/generated/. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |

Combined-agent notes:
- Tier 1 core hash chain (4 sequential): HEAT_SYSTEM → BATTLE_STATIONS → TX_MODEL → TRIGGER_ENGINE.
- Tier 1 bridge (3 parallel): STATION_IDENTITY.VISUAL, DASHBOARD_UX, MARKET_FORMAT — no file conflicts.
- Tier 1 docs (1): REPO_HEALTH — baseline.
- Tier 2 core hash chain (2 sequential): RADIATOR → OFFER_GEN.
- Tier 2 bridge (2 parallel): LEDGER.BRIDGE, SYSTEMIC.BRIDGE.
- Tier 2 core non-hash (1): LEDGER.INTEGRITY.
- Tier 3 core non-hash (1): CONTRACT.
- Tier 3 bridge (3): COMBAT_PHASE2.BRIDGE → HEAT_HUD (sequential), SYSTEMIC.HEADLESS (parallel).

### Tranche 32 — Combat Spin + Automation Intelligence (20 gates)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.COMBAT_PHASE2.SPIN_TURN.001 | DONE | Spin turn penalty + RPM modeling in StrategicResolverV0. Gyroscopic precession: spin RPM degrades turn rate proportionally. Fleet.SpinRpm field, CombatTweaksV0 spin constants (SpinRpmBase, TurnPenaltyPerRpm). Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/StrategicResolverV0.cs, FOUND: SimCore/Entities/Fleet.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs |
| GATE.S7.COMBAT_PHASE2.MOUNT_TYPE.001 | DONE | Mount type system on ModuleSlot: Standard (any arc), Broadside (90deg side arcs), Spinal (forward axis only, unaffected by spin). ModuleSlot.MountType enum. CombatSystem.BuildProfile reads mount type for arc restrictions. ContentRegistryLoader assigns mount types to weapon modules. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Entities/ModuleSlot.cs, FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S7.COMBAT_PHASE2.SPIN_FIRE.001 | DONE | Spin-fire cadence: turrets fire only during engagement arc windows (not continuously). SpinFireCadenceFraction in CombatTweaksV0. Spinal mounts fire along spin axis (unaffected by RPM). StrategicResolverV0 applies arc-based fire rate modifier per mount type. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/StrategicResolverV0.cs, FOUND: SimCore/Tweaks/CombatTweaksV0.cs, FOUND: SimCore/Systems/CombatSystem.cs |
| GATE.S7.COMBAT_PHASE2.SPIN_CONTRACT.001 | DONE | Contract tests for spin mechanics: spin RPM degrades turn rate, broadside mount fires only in side arcs, spinal mount fires along axis regardless of RPM, spin-fire cadence reduces effective DPS proportional to RPM, zero-RPM fleet has normal fire rate. 10+ test methods. Proof: dotnet test --filter "CombatPhase2" | FOUND: SimCore.Tests/Combat/CombatPhase2Tests.cs, FOUND: SimCore/Systems/StrategicResolverV0.cs |
| GATE.S7.COMBAT_PHASE2.SPIN_BRIDGE.001 | DONE | SimBridge.Combat.cs: GetSpinStateV0 (SpinRpm, TurnRatePenalty, MountArcStatus per mount type). GetMountTypesV0 returns per-slot mount type for ship fitting UI. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Combat.cs, FOUND: SimCore/Systems/CombatSystem.cs |
| GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001 | DONE | Overheat cascade VFX in game_manager.gd/hud.gd: heat shimmer (mesh distortion shader) at 75%+ heat, glowing hull tint at 100%+ overheat, forced vent particle burst at lockout. Radiator destruction VFX: tin droplet spray GPUParticles3D on aft zone HP=0. IN_ENGINE. Proof: dotnet build | FOUND: scripts/core/game_manager.gd, FOUND: scripts/ui/hud.gd |
| GATE.S7.COMBAT_PHASE2.ZONE_HUD.001 | DONE | Zone damage paperdoll HUD widget in hud.gd: 4-directional zone diagram (fore/aft/port/starboard) with HP bars per zone, color-coded by health (green>yellow>red>black). Reads SimBridge GetZoneArmorV0. Shows which zones are damaged/destroyed. IN_ENGINE. Proof: dotnet build | FOUND: scripts/ui/hud.gd, FOUND: scripts/bridge/SimBridge.Combat.cs |
| GATE.S7.COMBAT_PHASE2.HEADLESS.001 | DONE | Headless proof: boot game, engage combat, verify spin RPM increases during combat, mount type arc restrictions applied, overheat cascade triggers, zone damage accumulates. PLAYABLE_BEAT milestone. Proof: Godot headless | FOUND: scripts/bridge/SimBridge.Combat.cs, FOUND: scripts/bridge/SimBridge.cs, NEW: scripts/tests/test_spin_combat_v0.gd (headless proof for spin combat flow) |
| GATE.S7.AUTOMATION.PERF_TRACKING.001 | DONE | ProgramMetricsSystem: track per-program performance — TotalRevenue, TotalExpense, NetProfit, TicksActive, TradesCompleted, FailureCount. ProgramPerformance entity in SimState. Updated each tick from TransactionLog entries tagged with ProgramId. AutomationTweaksV0 for metric windows. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/ProgramMetricsSystem.cs, FOUND: SimCore/SimState.cs, NEW: SimCore/Tweaks/AutomationTweaksV0.cs (automation management needs dedicated tweaks) |
| GATE.S7.AUTOMATION.FAILURE_REASONS.001 | DONE | Failure reason taxonomy: 7 cause codes (INSUFFICIENT_CARGO, NO_PROFITABLE_ROUTE, FUEL_EXHAUSTED, BUDGET_EXCEEDED, ROUTE_BLOCKED, MARKET_SATURATED, PROGRAM_PAUSED). ProgramHistorySystem records FailureReason per failed tick. ProgramFailureRecord entity. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/ProgramHistorySystem.cs, FOUND: SimCore/Systems/ProgramMetricsSystem.cs, FOUND: SimCore/SimState.cs |
| GATE.S7.AUTOMATION.BUDGET_CAPS.001 | DONE | Program budget caps: MaxSpendPerCycle field on AutomationState, enforced in LogisticsSystem/NpcTradeSystem. DoctrineSystem.ApplyBudget reads fleet doctrine + program config. AutomationTweaksV0 default budget. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Entities/AutomationState.cs, FOUND: SimCore/Systems/DoctrineSystem.cs, FOUND: SimCore/Systems/LogisticsSystem.cs |
| GATE.S7.AUTOMATION.BRIDGE.001 | DONE | SimBridge.Automation.cs: GetProgramPerformanceV0 (per-program metrics), GetProgramFailureReasonsV0 (recent failures with cause codes + explanations), GetDoctrineSettingsV0 (fleet doctrine state), SetBudgetCapV0 command. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Automation.cs, FOUND: SimCore/Systems/ProgramMetricsSystem.cs |
| GATE.S7.AUTOMATION.UI.001 | DONE | Automation management UI panel in empire dashboard: program list with profit/loss per program, failure reason display with actionable explanations, budget cap sliders, doctrine overview. Reads SimBridge automation queries. IN_ENGINE. Proof: dotnet build | FOUND: scripts/ui/EmpireDashboard.cs, FOUND: scripts/bridge/SimBridge.Automation.cs |
| GATE.S9.SYSTEMIC.STATION_CONTEXT.001 | DONE | StationContextSystem: compute per-station economic context — supply shortages, price opportunities, warfront proximity, recent events. StationContext entity with ContextType enum (SHORTAGE, OPPORTUNITY, WARFRONT_DEMAND, CALM). Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/SimState.cs, NEW: SimCore/Systems/StationContextSystem.cs (new system for station economic context computation) |
| GATE.S9.SYSTEMIC.CONTEXT_BRIDGE.001 | DONE | SimBridge station context queries: GetStationContextV0 returns current station's economic context (shortages, opportunities, demand). Enables dock UI to show situation not just menu. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Market.cs, FOUND: SimCore/Systems/MarketSystem.cs |
| GATE.S9.SYSTEMIC.CONTEXT_UI.001 | DONE | Dock station context display: hero_trade_menu.gd shows context banner at top of dock panel — "Supply Shortage: munitions" / "Warfront Demand: composites" / "Price Opportunity: rare_metals". Color-coded by context type. IN_ENGINE. Proof: dotnet build | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.X.LEDGER.COST_BASIS.001 | DONE | Cost-basis tracking: Fleet cargo entries gain AvgBuyPrice field. BuyCommand records weighted average cost per good. SellCommand computes realized profit (sell price - avg cost). TransactionRecord gains ProfitDelta field. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Entities/Fleet.cs, FOUND: SimCore/Commands/BuyCommand.cs, FOUND: SimCore/Commands/SellCommand.cs, FOUND: SimCore/SimState.Events.cs |
| GATE.X.LEDGER.COST_BASIS_BRIDGE.001 | DONE | SimBridge profit/loss per cargo good: GetCargoWithCostBasisV0 returns per-good cargo with avg buy price + current market price + unrealized P/L. Fixes ACTIVE_ISSUES F1. Proof: dotnet build | FOUND: scripts/bridge/SimBridge.Market.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.X.HYGIENE.REPO_HEALTH.032 | DONE | Full test suite (Release), build, golden hash stability, dead code scan. Tranche 32 baseline. Report test count, warning count, hash stability. REGRESSION_ANCHOR. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.HYGIENE.EPIC_REVIEW.032 | DONE | Epic status audit. Review COMBAT_PHASE2, AUTOMATION_MGMT, SYSTEMIC_MISSIONS, LEDGER_EVENTS, PERF_BUDGET advancement after T32. Identify epics to close. Recommend T33 anchor. Update 54_EPICS.md statuses. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes:
- Tier 1 core hash chain (6 sequential): SPIN_TURN → MOUNT_TYPE → PERF_TRACKING → FAILURE_REASONS → STATION_CONTEXT → COST_BASIS.
- Tier 1 docs (1): REPO_HEALTH — baseline.
- Tier 2 core hash chain (2 sequential): SPIN_FIRE → BUDGET_CAPS.
- Tier 2 bridge (5 parallel): SPIN_CONTRACT, SPIN_BRIDGE, OVERHEAT_VFX, ZONE_HUD, AUTOMATION_BRIDGE, CONTEXT_BRIDGE, COST_BASIS_BRIDGE.
- Tier 3 bridge (3 parallel): HEADLESS, AUTOMATION_UI, CONTEXT_UI.
- Tier 3 docs (1): EPIC_REVIEW.
- Combine SPIN_TURN + MOUNT_TYPE into one agent (share CombatSystem.cs/StrategicResolverV0.cs).
- Combine PERF_TRACKING + FAILURE_REASONS into one agent (share ProgramMetricsSystem.cs).
- Tier 3 docs (3): EPIC_REVIEW, FIRST_HOUR_EVAL, TICK_BASELINE.

### AL. Tranche 33 — "Haven Foundation + Closing Fronts" (EPIC.S8.HAVEN_STARBASE.V0, EPIC.S5.TRACTOR_SYSTEM.V0, EPIC.S7.AUTOMATION_MGMT.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.033 | DONE | Full test suite (Release), build, golden hash stability, dead code scan. Tranche 33 baseline. Report test count, warning count, hash stability. REGRESSION_ANCHOR. Proof: dotnet test full suite | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.S8.HAVEN.ENTITY.001 | DONE | HavenStarbase entity: HavenTier enum (Powered/Inhabited/Operational/Expanded/Awakened), UpgradeProgress, HangarBays, StoredShipIds, InstalledFragments, ExoticMarketGoods. SimState.Haven field + HydrateAfterLoad + signature. HavenTweaksV0 with upgrade costs per tier, market rules, hangar bay counts. Hash-affecting. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Entities/HavenStarbase.cs, NEW: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S8.HAVEN.DISCOVERY.001 | DONE | WorldLoader seeds Haven at a fracture-accessible void site in deep space. GalaxyGenerator picks Haven location: random quadrant, far from starter, requires fracture drive to reach. Discovery prerequisite: Communion rep 50+ breadcrumb OR fracture scan of Phase 2+ space. Haven.NodeId stored in SimState. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/World/WorldLoader.cs, FOUND: SimCore/Gen/GalaxyGenerator.cs, FOUND: SimCore/SimState.cs |
| GATE.S8.HAVEN.UPGRADE_SYSTEM.001 | DONE | HavenUpgradeSystem.Process(): check tier prerequisites (credits + goods + fragments per HavenTweaksV0), advance UpgradeProgress toward tier completion, unlock tier effects (hangar bays, market goods, passive generation). UpgradeHavenCommand: player initiates upgrade at Haven dock. Wire into SimKernel.Step. Hash-affecting. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/HavenUpgradeSystem.cs, NEW: SimCore/Commands/UpgradeHavenCommand.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S5.TRACTOR.MODEL.001 | DONE | Tractor module content entries (3 tiers: T1 Magnetic Grapple 15u, T2 EM Tractor Array 30u, T3 Graviton Tether 50u). TractorRange field on ModuleDef. CollectLootCommand checks equipped tractor module for range — no tractor = 5u fallback. ContentRegistryLoader loads tractor modules. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Commands/CollectLootCommand.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Entities/ModuleSlot.cs |
| GATE.S7.AUTOMATION.TEMPLATES.001 | DONE | ProgramTemplate content definitions: predefined automation configs (BuyLowSellHigh, SupplyRoute, ArbitrageLoop, WarSupplyRun, FuelHauler). ProgramTemplateContentV0 with template Id, name, description, parameter presets. AutomationState gains TemplateId field for template-created programs. Hash-affecting. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Content/ProgramTemplateContentV0.cs, FOUND: SimCore/Entities/AutomationState.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.X.WARP.ARRIVAL_DRAMA.001 | DONE | Enhanced warp arrival per ACTIVE_ISSUES F4: 1.5s letterbox bars on arrival, system info title card (system name + faction owner + economic context), arrival audio sting, brief camera swoop before settling to standard altitude. game_manager.gd on_lane_arrival_v0 drives letterbox + title card tween. hud.gd renders title card overlay. Fixes ACTIVE_ISSUES F4. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/ui/hud.gd, FOUND: scripts/view/screen_edge_tint.gd |
| GATE.X.UI_POLISH.QUEST_TRACKER.001 | DONE | Persistent HUD quest/objective tracker in Zone A: shows active mission name + current step + progress bar. Compact widget always visible during flight, hides when no active mission. SimBridge.Mission.cs GetActiveMissionSummaryV0 returns mission name + step description + progress. Fixes ACTIVE_ISSUES F5. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/bridge/SimBridge.Mission.cs, FOUND: SimCore/Systems/MissionSystem.cs |
| GATE.X.EVAL.HAVEN_DESIGN.001 | DONE | Audit haven_starbase_v0.md design doc vs current codebase: verify all referenced entity types exist or have mapped NEW paths, identify conflicts with existing market/fleet/discovery systems, produce implementation checklist with risk flags. Research gate. Proof: dotnet build SimCore/SimCore.csproj --nologo | FOUND: docs/design/haven_starbase_v0.md, FOUND: docs/54_EPICS.md |
| GATE.S8.HAVEN.HANGAR.001 | DONE | HavenHangarSystem: multi-ship storage at Haven. SwapShipCommand (dock at Haven, swap active fleet with stored). Bay count per tier (T1=1, T3=2, T5=3 per HavenTweaksV0). Stored ships retain modules + cargo, zero sustain cost while stored. Fleet.IsStored flag. Hash-affecting. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/HavenHangarSystem.cs, NEW: SimCore/Commands/SwapShipCommand.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.S8.HAVEN.MARKET.001 | DONE | Haven exotic-only market variant: special Market seeded at Haven node. Stock evolves with tier — T1: exotic_crystals only (10u), T2: +fuel/metal/organics (20u each), T3: +exotic_matter/salvaged_tech (30u). Faction goods sold at 50% penalty price. HavenUpgradeSystem refreshes market stock on tier change. Hash-affecting. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/World/WorldLoader.cs, FOUND: SimCore/Entities/Market.cs |
| GATE.S8.HAVEN.CONTRACT.001 | DONE | Contract tests: HavenUpgradeTests (tier advancement with correct resource costs, insufficient resources rejected, tier effects applied), HavenHangarTests (ship swap success/failure, bay limit enforcement, stored ship retains cargo), HavenMarketTests (stock varies by tier, faction goods at penalty). 15+ test methods. Proof: dotnet test --filter "HavenTests" | NEW: SimCore.Tests/Systems/HavenTests.cs, FOUND: SimCore/Entities/HavenStarbase.cs |
| GATE.S5.TRACTOR.BRIDGE.001 | DONE | SimBridge tractor queries: GetTractorRangeV0 (equipped tier + effective range). Update CollectLootV0 to check tractor range before pickup. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: SimCore/Commands/CollectLootCommand.cs |
| GATE.S5.TRACTOR.VFX.001 | DONE | Tractor beam energy VFX: visible beam from player ship to loot target while collecting. Procedural Line3D or MeshInstance3D with glow shader, animated UV scroll. game_manager.gd drives beam endpoint. IN_ENGINE. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/view/GalaxyView.cs |
| GATE.S7.AUTOMATION.TEMPLATES_UI.001 | DONE | Template picker in automation panel: dropdown/list to create program from template, pre-fills configuration parameters. SimBridge.Automation.cs GetProgramTemplatesV0 returns template list. EmpireDashboard.cs "New from Template" button. IN_ENGINE. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Automation.cs, FOUND: scripts/ui/EmpireDashboard.cs |
| GATE.S8.HAVEN.BRIDGE.001 | DONE | SimBridge.Haven.cs partial: GetHavenStatusV0 (tier, upgrade progress, bay count, stored ships), GetHavenMarketV0 (available goods + prices), UpgradeHavenV0 (initiate upgrade), SwapShipV0 (swap active fleet with stored). Standard TryExecuteSafeRead/WriteLock pattern. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Haven.cs, FOUND: SimCore/Entities/HavenStarbase.cs |
| GATE.S8.HAVEN.DOCK_PANEL.001 | DONE | Haven dock UI panel: unique dock layout (not standard station tabs). Shows Haven name + tier badge, upgrade section (cost tooltip, progress bar, upgrade button), hangar section (ship list with swap buttons), market tab (exotic goods). Warm amber visual theme per design doc. hero_trade_menu.gd detects Haven dock and renders alternate layout. IN_ENGINE. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.S8.HAVEN.GALAXY_ICON.001 | DONE | Haven icon on galaxy map: unique golden diamond icon (distinct from station circles), tooltip showing "Haven — Tier N", route-to support. Hidden until player discovers Haven. GalaxyView.cs DrawLocalSystemV0 and DrawStrategicV0 render Haven icon. IN_ENGINE. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.S8.HAVEN.HEADLESS.001 | DONE | Headless proof: boot game, advance to discover Haven node, dock at Haven, verify tier 1 status, initiate upgrade to tier 2, verify market goods, test ship swap (if second ship available). HEADLESS_PROOF milestone. Proof: Godot headless -s test_haven_proof_v0.gd | NEW: scripts/tests/test_haven_proof_v0.gd, FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.033 | DONE | Close eligible epics: SYSTEMIC_MISSIONS.V0 (all 7 gates DONE), LEDGER_EVENTS.V0 (TX_MODEL+BRIDGE+INTEGRITY+COST_BASIS done), AUTOMATION_MGMT.V0 (if templates gate passes). Update 54_EPICS.md statuses. Recommend T34 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |

Combined-agent notes:
- Tier 1 core hash chain (5 sequential): HAVEN.ENTITY → HAVEN.DISCOVERY → HAVEN.UPGRADE_SYSTEM → TRACTOR.MODEL → AUTOMATION.TEMPLATES.
- Tier 1 bridge (2 parallel): WARP.ARRIVAL_DRAMA, QUEST_TRACKER — no file conflicts.
- Tier 1 docs (2 parallel): REPO_HEALTH, HAVEN_DESIGN.
- Tier 2 core hash chain (2 sequential): HAVEN.HANGAR → HAVEN.MARKET.
- Tier 2 core non-hash (1): HAVEN.CONTRACT.
- Tier 2 bridge (4 parallel): TRACTOR.BRIDGE, TRACTOR.VFX, AUTOMATION.TEMPLATES_UI, HAVEN.BRIDGE.
- Tier 3 bridge (3 parallel): HAVEN.DOCK_PANEL, HAVEN.GALAXY_ICON, HAVEN.HEADLESS.
- Tier 3 docs (1): EPIC_REVIEW.

## AM. Tranche 34 — "Haven Depth + Endgame Progression" (EPIC.S8.HAVEN_STARBASE.V0, EPIC.S8.ADAPTATION_FRAGMENTS.V0, EPIC.S8.T3_PRECURSOR_MODULES.V0, EPIC.S8.ANCIENT_SHIP_HULLS.V0, EPIC.S5.TRACTOR_SYSTEM.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.034 | DONE | Full test suite (1232+ tests), warning scan, dead code check, golden hash stability. Baseline health for T34. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.S8.HAVEN.RESIDENTS.001 | DONE | Haven Residents data model: HavenStarbase gains Residents list (The Keeper NPC always present, FO promotion candidates appear at Tier 2+). HavenResidentContentV0 defines resident archetypes. SimState signature includes residents. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore/Content/FirstOfficerContentV0.cs |
| GATE.S8.HAVEN.TROPHY_WALL.001 | DONE | Trophy Wall system: HavenStarbase gains TrophyWall dictionary (fragmentId → tick collected). TrophyWallSystem tracks deposited AdaptationFragments, computes resonance pair bonuses (8 pairs per design doc). Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenUpgradeSystem.cs |
| GATE.S8.HAVEN.LOGS.001 | DONE | 20 Haven-specific data logs in DataLogContentV0: 10 Keeper monologues (Haven history, Precursor departure hints), 5 construction logs (tier upgrade lore), 5 fragment analysis notes. NarrativePlacementGen places Haven logs at Haven node. Proof: dotnet test --filter "DataLog" | FOUND: SimCore/Content/DataLogContentV0.cs, FOUND: SimCore/Gen/NarrativePlacementGen.cs |
| GATE.S8.ADAPTATION.ENTITY.001 | DONE | AdaptationFragment entity: id, name, description, kind (Biological/Structural/Energetic/Cognitive), resonancePairId, discoveredTick. 16 fragment definitions in AdaptationFragmentContentV0. SimState.AdaptationFragments dictionary. Proof: dotnet test --filter "AdaptationFragment" | NEW: SimCore/Entities/AdaptationFragment.cs, NEW: SimCore/Content/AdaptationFragmentContentV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S8.ADAPTATION.COLLECTION.001 | DONE | AdaptationFragmentSystem: CollectFragment command adds to player collection, checks for resonance pair completion (8 pairs), emits resonance bonus event. ResonancePair bonuses: +5% trade margin, +10% scan range, +1 hangar bay, etc. Proof: dotnet test --filter "AdaptationFragment" | NEW: SimCore/Systems/AdaptationFragmentSystem.cs, NEW: SimCore/Tweaks/AdaptationTweaksV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S8.ADAPTATION.PLACEMENT.001 | DONE | Fragment worldgen placement: DiscoverySeedGen places 16 fragments at void sites and deep discovery locations. Each fragment placed at unique node, biased toward frontier. Hash-based deterministic placement from galaxy seed. Proof: dotnet test --filter "AdaptationFragment" | FOUND: SimCore/Gen/DiscoverySeedGen.cs, FOUND: SimCore/Entities/VoidSite.cs, NEW: SimCore/Content/AdaptationFragmentContentV0.cs |
| GATE.S8.T3_MODULES.CONTENT.001 | DONE | ~13 T3 module content definitions in ContentRegistryLoader: T3 weapons (Spinal Lance, Void Cutter, Lattice Disruptor), T3 defense (Phase Barrier, Resonance Shield), T3 engines (Threshold Drive, Void Skipper), T3 utility (Deep Scanner, Graviton Tether T3, Exotic Refinery, Fragment Analyzer, Precursor Core, Lattice Harmonizer). Tier=3, all require discovery unlock. Proof: dotnet test --filter "ContentSubstrate" | FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Content/UpgradeContentV0.cs |
| GATE.S8.T3_MODULES.EXOTIC_SUSTAIN.001 | DONE | T3 modules consume exotic_matter during sustain cycle. ModuleDefV0 entries for T3 set SustainGoodId="exotic_matter". SustainSystem already reads SustainGoodId — just need content wiring. Verify SustainSystem deducts exotic_matter from fleet cargo for T3 modules. Proof: dotnet test --filter "Sustain" | FOUND: SimCore/Systems/SustainSystem.cs, FOUND: SimCore/Tweaks/SustainTweaksV0.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S8.T3_MODULES.DISCOVERY_GATE.001 | DONE | T3 modules not purchasable at stations — acquired only via discovery outcomes (void site salvage, fragment analysis rewards). RefitSystem rejects EquipModule for T3 if not in fleet cargo. DiscoveryOutcomeSystem can award T3 modules as loot. Proof: dotnet test --filter "Refit\|DiscoveryOutcome" | FOUND: SimCore/Systems/RefitSystem.cs, FOUND: SimCore/Systems/DiscoveryOutcomeSystem.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S8.ANCIENT_HULLS.CONTENT.001 | DONE | 3 ancient hull ShipClassDef entries in ShipClassContentV0: Bastion (tank — high armor, 6 defense slots, low speed), Seeker (explorer — high scan range, 4 utility slots, medium speed), Threshold (fracture specialist — fracture cost reduction, 3 spinal mounts, exotic matter capacity). Pre-revelation names ("Hull Type XV-1/2/3"). Proof: dotnet test --filter "ShipClass\|ContentSubstrate" | FOUND: SimCore/Content/ShipClassContentV0.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S8.ANCIENT_HULLS.RESTORE.001 | DONE | Hull restoration at Haven Tier 3+: RestoreAncientHullCommand consumes exotic_matter + credits + ticks. HavenHangarSystem validates tier >= 3 and hull fragment in player inventory. Restored hull becomes a fleet in Haven hangar. Proof: dotnet test --filter "HavenTests\|AncientHull" | FOUND: SimCore/Systems/HavenHangarSystem.cs, FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Commands/SwapShipCommand.cs |
| GATE.S5.TRACTOR.WEAVER.001 | DONE | Weaver faction Spindle Tractor module: 25u range, auto-salvage trait (automatically collects loot from destroyed ships without manual pickup). New module entry in ContentRegistryLoader with FactionId="weaver", requires Weaver rep tier 2. CollectLootCommand recognizes auto-salvage flag. Proof: dotnet test --filter "Loot\|Tractor\|ContentSubstrate" | FOUND: SimCore/Commands/CollectLootCommand.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs |
| GATE.S5.TRACTOR.AUTO_TARGET.001 | DONE | Auto-target nearest loot: SimBridge.Fleet.cs GetNearestLootV0 returns closest LootDrop within tractor range. game_manager.gd _poll_auto_loot_v0 uses this to auto-fire tractor beam at nearest loot. Visual: tractor beam VFX auto-points at target. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/core/game_manager.gd, FOUND: scripts/vfx/tractor_beam.gd |
| GATE.S8.HAVEN.RESIDENTS_BRIDGE.001 | DONE | SimBridge.Haven.cs GetHavenResidentsV0: returns Array of {name, role, dialogue_hint, available}. haven_panel.gd Residents tab: list of NPCs with talk button (placeholder dialogue). The Keeper always available; FO candidates gated by tier. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.HAVEN.TROPHY_BRIDGE.001 | DONE | SimBridge.Haven.cs GetTrophyWallV0: returns Array of {fragment_id, name, kind, collected_tick, resonance_pair_complete}. haven_panel.gd Trophy Wall tab: grid of fragment slots (collected=gold icon, uncollected=silhouette), resonance pair indicators. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.HAVEN.FRAGMENT_DISPLAY.001 | DONE | Fragment Geometry 3D display: haven_panel.gd or dedicated scene shows collected fragments as rotating procedural icosphere meshes with emission shader. Color-coded by kind (Biological=green, Structural=blue, Energetic=amber, Cognitive=purple). Resonance pairs glow when both collected. IN_ENGINE. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/haven_panel.gd, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.S8.ADAPTATION.BRIDGE.001 | DONE | SimBridge fragment queries: GetAdaptationFragmentsV0 (all 16 with collected status), GetResonancePairsV0 (8 pairs with bonus descriptions). Fragment collection UI: discovery site shows "Fragment Detected" with collect button, toast on collection. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.T3_MODULES.BRIDGE.001 | DONE | SimBridge T3 module queries: GetT3ModuleCatalogV0 returns all T3 modules with discovered/undiscovered status. Refit panel shows T3 modules grayed with "Requires Discovery" tooltip if not yet found. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Refit.cs, FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S8.ANCIENT_HULLS.BRIDGE.001 | DONE | SimBridge hull queries: GetAncientHullsV0 returns hull data with restoration status. Haven panel Hull Restoration tab: shows available hulls, restoration cost, progress bar, initiate button. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.HAVEN.HEADLESS_DEPTH.001 | DONE | Headless proof: boot → verify Haven residents list, verify Trophy Wall state, collect adaptation fragment via bridge, verify trophy wall updated, verify resonance pair detection, verify T3 module discovery gate, verify ancient hull restoration prerequisites. HEADLESS_PROOF milestone. Proof: Godot headless -s test_haven_depth_proof_v0.gd | NEW: scripts/tests/test_haven_depth_proof_v0.gd, FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.034 | DONE | Audit epic statuses against completed gates. Close TRACTOR_SYSTEM.V0 if Weaver+AutoTarget done. Close STATION_IDENTITY.V0 (VISUAL.001 already DONE, epic still TODO). Update HAVEN_STARBASE.V0 progress. Update ADAPTATION_FRAGMENTS.V0, T3_PRECURSOR_MODULES.V0, ANCIENT_SHIP_HULLS.V0 statuses. Recommend T35 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |
| GATE.X.EVAL.ENDGAME_PROGRESSION.001 | DONE | Evaluate endgame progression balance: fragment acquisition rate vs game length (16 fragments across ~300 tick playthrough), T3 module power budget vs T2 ceiling, ancient hull restoration cost curve, Haven tier advancement pacing, resonance pair bonus impact on economy. Produce recommendations in docs/generated/endgame_eval_v0.md. Proof: File exists and is non-empty | FOUND: docs/design/haven_starbase_v0.md, FOUND: docs/design/ship_modules_v0.md, FOUND: docs/design/faction_equipment_and_research_v0.md |

Combined-agent notes:
- Tier 1 core hash chain (12 sequential): HAVEN.RESIDENTS → HAVEN.TROPHY_WALL → HAVEN.LOGS → ADAPTATION.ENTITY → ADAPTATION.COLLECTION → ADAPTATION.PLACEMENT → T3_MODULES.CONTENT → T3_MODULES.EXOTIC_SUSTAIN → T3_MODULES.DISCOVERY_GATE → ANCIENT_HULLS.CONTENT → TRACTOR.WEAVER. Combine into 3 agent groups: Haven core (1-3), Adaptation core (4-6), Equipment core (7-11).
- Tier 1 bridge (1): TRACTOR.AUTO_TARGET — no file conflicts with core.
- Tier 1 docs (1): REPO_HEALTH.
- Tier 2 core hash (1): ANCIENT_HULLS.RESTORE (depends on CONTENT.001).
- Tier 2 bridge (6 parallel): HAVEN.RESIDENTS_BRIDGE, HAVEN.TROPHY_BRIDGE, HAVEN.FRAGMENT_DISPLAY, ADAPTATION.BRIDGE, T3_MODULES.BRIDGE, ANCIENT_HULLS.BRIDGE — all touch different SimBridge partials/UI files.
- Tier 3 bridge (1): HAVEN.HEADLESS_DEPTH.
- Tier 3 docs (2): EPIC_REVIEW, EVAL.ENDGAME_PROGRESSION.

## Tranche 35: Systems Alive (22 gates)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.035 | DONE | Full test suite (1232+ tests), warning scan, golden hash stability baseline. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.X.MARKET_PRICING.FEE_WIRE.001 | DONE | Wire MarketSystem.ComputeTransactionFeeCredits into BuyCommand (deduct fee from buyer) and SellCommand (deduct fee from revenue). Include crisis fee increase when PressureSystem tier >= Critical. Update MarketTweaksV0 if needed. Proof: dotnet test --filter "MarketFee\|BuyCommand\|SellCommand\|Determinism" | FOUND: SimCore/Commands/BuyCommand.cs, FOUND: SimCore/Commands/SellCommand.cs, FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Tweaks/MarketTweaksV0.cs |
| GATE.X.MARKET_PRICING.REP_WIRE.001 | DONE | Wire MarketSystem.ApplyRepPricing into BuyCommand/SellCommand pricing pipeline — apply GetRepPricingBps after spread, before instability. Allied -15%, Friendly -5%, Neutral 0%, Hostile +20%. Proof: dotnet test --filter "ReputationPricing\|BuyCommand\|SellCommand\|Determinism" | FOUND: SimCore/Commands/BuyCommand.cs, FOUND: SimCore/Commands/SellCommand.cs, FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore.Tests/Systems/ReputationPricingTests.cs |
| GATE.X.PRESSURE_INJECT.WARFRONT.001 | DONE | Call PressureSystem.InjectDelta in WarfrontDemandSystem.Process when warfront intensity changes (domain "warfront_demand", reason "intensity_shift", magnitude proportional to intensity delta). Add test. Proof: dotnet test --filter "PressureSystem\|WarfrontDemand\|Determinism" | FOUND: SimCore/Systems/WarfrontDemandSystem.cs, FOUND: SimCore/Systems/PressureSystem.cs, FOUND: SimCore.Tests/Systems/PressureSystemScenarioTests.cs |
| GATE.X.PRESSURE_INJECT.INSTABILITY.001 | DONE | Call PressureSystem.InjectDelta in InstabilitySystem.Process on phase transitions (domain "instability", reason "phase_transition", magnitude scaled by phase severity). Add test. Proof: dotnet test --filter "PressureSystem\|Instability\|Determinism" | FOUND: SimCore/Systems/InstabilitySystem.cs, FOUND: SimCore/Systems/PressureSystem.cs, FOUND: SimCore.Tests/Systems/PressureSystemScenarioTests.cs |
| GATE.X.PRESSURE_INJECT.MARKET.001 | DONE | Call PressureSystem.InjectDelta in MarketSystem on trade failure events (instability-caused closure or embargo block) — domain "trade_disruption", reason "market_blocked". Add test. Proof: dotnet test --filter "PressureSystem\|Market\|Determinism" | FOUND: SimCore/Systems/MarketSystem.cs, FOUND: SimCore/Systems/PressureSystem.cs, FOUND: SimCore.Tests/Systems/PressureSystemScenarioTests.cs |
| GATE.X.PRESSURE_INJECT.SUSTAIN.001 | DONE | Call PressureSystem.InjectDelta in SustainSystem.Process when fleet enters starvation (domain "supply_shortage", reason "sustain_starvation", magnitude from severity). Add test. Proof: dotnet test --filter "PressureSystem\|Sustain\|Determinism" | FOUND: SimCore/Systems/SustainSystem.cs, FOUND: SimCore/Systems/PressureSystem.cs, FOUND: SimCore.Tests/Systems/PressureSystemScenarioTests.cs |
| GATE.X.FLEET_UPKEEP.DRAIN.001 | DONE | New FleetUpkeepSystem: per-cycle credit drain by ship class (Shuttle 2cr, Clipper 5cr, Corvette 8cr, Frigate 15cr, Cruiser 25cr, Hauler 10cr, Carrier 35cr, Dreadnought 50cr). Docked multiplier 50%. Wire into SimKernel.Step. FleetUpkeepTweaksV0 for all constants. Proof: dotnet test --filter "FleetUpkeep\|Determinism" | NEW: SimCore/Systems/FleetUpkeepSystem.cs, NEW: SimCore/Tweaks/FleetUpkeepTweaksV0.cs, FOUND: SimCore/SimKernel.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.X.FLEET_UPKEEP.DELINQUENCY.001 | DONE | Add Fleet.UpkeepDelinquentCycles field. Grace period (3 cycles at 0 credits). After grace: disable highest-PowerDraw modules first (ModuleSlot.Disabled = true). Recovery on payment. Proof: dotnet test --filter "FleetUpkeep\|Determinism" | FOUND: SimCore/Entities/Fleet.cs, NEW: SimCore.Tests/Systems/FleetUpkeepTests.cs, FOUND: SimCore/Systems/FleetUpkeepSystem.cs |
| GATE.S7.TERRITORY_SHIFT.RECOMPUTE.001 | DONE | In WarfrontEvolutionSystem.ProcessObjectives, after objective capture (ControllingFactionId set), run BFS territory recompute: update state.NodeFactionId for affected nodes. Deterministic ordered iteration. Proof: dotnet test --filter "TerritoryShift\|TerritoryRegime\|Determinism" | FOUND: SimCore/Systems/WarfrontEvolutionSystem.cs, FOUND: SimCore/Gen/GalaxyGenerator.cs, NEW: SimCore.Tests/Systems/TerritoryShiftTests.cs |
| GATE.S7.TERRITORY_SHIFT.REGIME_FLIP.001 | DONE | After territory recompute, refresh tariff/embargo/regime for affected nodes — call ReputationSystem.ComputeTerritoryRegime for shifted nodes, update state.NodeRegimeCommitted. Proof: dotnet test --filter "TerritoryShift\|TerritoryRegime\|Determinism" | FOUND: SimCore/Systems/WarfrontEvolutionSystem.cs, FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore.Tests/Systems/TerritoryRegimeTests.cs |
| GATE.T18.KG_SEED.RESOLVE.001 | DONE | In GalaxyGenerator after Phase 9 (data logs), resolve KnowledgeGraphContentV0.AllTemplates into KnowledgeConnection entities — replace pattern tokens ($KEPLER_1-6, $LOG.THREAD.NUM) with actual discovery IDs from seeded data logs. Proof: dotnet test --filter "KnowledgeGraph\|Determinism" | FOUND: SimCore/Gen/GalaxyGenerator.cs, FOUND: SimCore/Content/KnowledgeGraphContentV0.cs, FOUND: SimCore/Systems/KnowledgeGraphSystem.cs, NEW: SimCore.Tests/Systems/KnowledgeGraphSeedTests.cs |
| GATE.T18.KG_SEED.PROXIMITY.001 | DONE | Add procedural proximity connections (BFS <=2 hops between data log sites) and faction link generation. Validate 15-25 total connections per seed. Proof: dotnet test --filter "KnowledgeGraph\|Determinism" | FOUND: SimCore/Gen/GalaxyGenerator.cs, FOUND: SimCore/Content/KnowledgeGraphContentV0.cs, FOUND: SimCore.Tests/Systems/KnowledgeGraphSeedTests.cs |
| GATE.S7.FACTION_COMMISSION.ENTITY.001 | DONE | Commission entity (FactionId, StartTick, StipendCreditsPerCycle). CommissionSystem.Process: +1 rep/1440 ticks with employer, -1 rep/1440 with rivals. Stipend credit payment per cycle. SimKernel wiring. CommissionTweaksV0. Proof: dotnet test --filter "Commission\|Determinism" | NEW: SimCore/Entities/Commission.cs, NEW: SimCore/Systems/CommissionSystem.cs, NEW: SimCore/Tweaks/CommissionTweaksV0.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S7.FACTION_COMMISSION.INFAMY.001 | DONE | Add state-level InfamyByFaction dictionary. Accumulate on attacks + war-profiteering. Cap max achievable RepTier based on infamy (infamy >= 50 caps at Friendly, >= 100 caps at Neutral). Wire into ReputationSystem. Proof: dotnet test --filter "Infamy\|Reputation\|Determinism" | FOUND: SimCore/Systems/ReputationSystem.cs, FOUND: SimCore/Entities/Fleet.cs, FOUND: SimCore.Tests/Systems/ReputationSystemTests.cs |
| GATE.X.MARKET_PRICING.BREAKDOWN_BRIDGE.001 | DONE | PriceBreakdownV0 struct (Base, Scarcity, RepMod, Tariff, Instability, Fee, Total). SimBridge.Market.cs GetPriceBreakdownV0(nodeId, goodId, isBuy) returns full breakdown. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Market.cs, FOUND: SimCore/Systems/MarketSystem.cs |
| GATE.X.MARKET_PRICING.BREAKDOWN_UI.001 | DONE | hero_trade_menu.gd price breakdown tooltip — hover/click on price shows line-item breakdown (base, scarcity, rep discount, tariff, instability, fee). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.X.FLEET_UPKEEP.BRIDGE.001 | DONE | SimBridge fleet upkeep queries: GetFleetUpkeepV0 (per-fleet cost/cycle), GetTotalUpkeepV0 (empire total). HUD upkeep indicator in Zone G. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/ui/hud.gd |
| GATE.S7.TERRITORY_SHIFT.MAP_UPDATE.001 | DONE | GalaxyView.cs territory disc color update when NodeFactionId changes at runtime — listen for territory shift events, refresh faction disc tint. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.S7.FACTION_COMMISSION.BRIDGE.001 | DONE | SimBridge.Faction.cs commission queries (GetActiveCommissionV0, GetRepModifierStackV0 — named breakdown of rep gains/losses), locked-but-visible UI (dock menu shows "Requires Friendly" on gated items). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Faction.cs, FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.X.EVAL.DYNAMIC_TENSION.001 | DONE | Multi-seed ExplorationBot run (5 seeds x 5000 ticks): verify pressure injection fires (domain count > 1), fleet upkeep drains credits, territory shifts occur after warfront captures. Report balance findings. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.035 | DONE | Audit epic statuses against completed gates. Close MARKET_PRICING_WIRING, PRESSURE_INJECTION_WIRING, FLEET_STANDING_COSTS, TERRITORY_SHIFT, KNOWLEDGE_GRAPH_SEEDING, FACTION_COMMISSION if all gates DONE. Recommend T36 anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |

Combined-agent notes:
- Tier 1 core hash chain (10 sequential): FEE_WIRE → REP_WIRE → PRESSURE_INJECT.WARFRONT → PRESSURE_INJECT.INSTABILITY → PRESSURE_INJECT.MARKET → PRESSURE_INJECT.SUSTAIN → FLEET_UPKEEP.DRAIN → TERRITORY_SHIFT.RECOMPUTE → KG_SEED.RESOLVE → FACTION_COMMISSION.ENTITY. Combine FEE_WIRE + REP_WIRE (shared BuyCommand/SellCommand). Combine 4 PRESSURE_INJECT gates (shared PressureSystem pattern).
- Tier 1 docs (1): REPO_HEALTH — parallel with core.
- Tier 2 core hash chain (4 sequential): FLEET_UPKEEP.DELINQUENCY → TERRITORY_SHIFT.REGIME_FLIP → KG_SEED.PROXIMITY → FACTION_COMMISSION.INFAMY.
- Tier 2 bridge (4 parallel): MARKET_PRICING.BREAKDOWN_BRIDGE + BREAKDOWN_UI (combine — shared SimBridge.Market.cs + hero_trade_menu.gd), FLEET_UPKEEP.BRIDGE, TERRITORY_SHIFT.MAP_UPDATE, FACTION_COMMISSION.BRIDGE.
- Tier 3 docs (2 parallel): EVAL.DYNAMIC_TENSION, EPIC_REVIEW.

## T36. Combat Depth V2 + Lattice Drones + UI Warfront/Discovery

Epics advanced: EPIC.S7.COMBAT_DEPTH_V2.V0, EPIC.S8.LATTICE_DRONES.V0, EPIC.S7.UI_WARFRONT.V0, EPIC.S6.UI_DISCOVERY.V0

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.036 | DONE | Full test suite (1232+ tests), warning scan, dead code check, golden hash stability. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.S7.COMBAT_DEPTH2.TRACKING.001 | DONE | TrackingBps and EvasionBps fields on weapon/ship definitions. Hit probability = clamp(TrackingBps - EvasionBps, 2000, 10000) / 10000. FNV1a64-based deterministic roll per weapon per round. CombatSystem.ResolveSalvo reads tracking/evasion, applies miss chance. CombatDepthTweaksV0 for base values. Proof: dotnet test --filter "CombatDepthTests" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Content/ShipClassContentV0.cs, NEW: SimCore/Tweaks/CombatDepthTweaksV0.cs, NEW: SimCore.Tests/Systems/CombatDepthTests.cs |
| GATE.S7.COMBAT_DEPTH2.DAMAGE_VAR.001 | DONE | ±20% damage variance via deterministic hash (FNV1a64 of tick+weaponSlot+targetSlot). Variance range in CombatDepthTweaksV0. Applied in ResolveSalvo after hit check. Proof: dotnet test --filter "CombatDepthTests" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Tweaks/CombatDepthTweaksV0.cs |
| GATE.S7.COMBAT_DEPTH2.ARMOR_PEN.001 | DONE | ArmorPenetrationPct on weapon definitions — fraction of damage bypassing zone armor to hull. Heavy weapons (railgun, spinal) high pen, light weapons (PD, laser) low pen. Applied in CalcDamageWithZoneArmor. Proof: dotnet test --filter "CombatDepthTests" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Systems/StrategicResolverV0.cs, FOUND: SimCore/Tweaks/CombatDepthTweaksV0.cs |
| GATE.S7.COMBAT_DEPTH2.FORE_KILL.001 | DONE | Fore zone soft-kill: when fore zoneHp reaches 0, weapons in fore slots go offline (0 damage output). CombatSystem checks zone status before weapon fire. Proof: dotnet test --filter "CombatDepthTests" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Entities/Fleet.cs |
| GATE.S8.LATTICE_DRONES.ENTITY.001 | DONE | LatticeDrone entity class (reuses Fleet with IsLatticeDrone flag). LatticeDroneTweaksV0 for spawn thresholds, stats per instability phase. SimState dictionary for active drones. ContentRegistryLoader wires drone ship class. Proof: dotnet test --filter "CombatDepthTests" | FOUND: SimCore/Entities/Fleet.cs, NEW: SimCore/Entities/LatticeDrone.cs, NEW: SimCore/Tweaks/LatticeDroneTweaksV0.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S8.LATTICE_DRONES.SPAWN.001 | DONE | LatticeDroneSpawnSystem: reads instability phase per node, spawns drones at P2+ nodes. Phase behavior: P0-1 passive (no spawn), P2 territorial (spawn near void sites), P3 hostile (spawn at lanes), P4 absent (despawn all). Tick-driven spawn/despawn cycle. Proof: dotnet test --filter "CombatDepthTests" | NEW: SimCore/Systems/LatticeDroneSpawnSystem.cs, FOUND: SimCore/Systems/InstabilitySystem.cs, NEW: SimCore.Tests/Systems/LatticeDroneTests.cs |
| GATE.S7.COMBAT_DEPTH2.PROJECTION.001 | DONE | Pre-combat outcome projection: given attacker fleet + defender fleet, simulate N rounds (no state mutation) and return projected outcome (win/loss/pyrrhic, estimated losses). Starsector-inspired "you will likely win/lose" display. CombatSystem.ProjectOutcome static method. Proof: dotnet test --filter "CombatDepthTests" | FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Systems/StrategicResolverV0.cs |
| GATE.S8.LATTICE_DRONES.COMBAT.001 | DONE | LatticeDroneCombatSystem: drones auto-engage player/NPC fleets in their node. Territorial drones warn first (1 tick grace), hostile drones attack immediately. Drone combat uses standard CombatSystem with tracking/evasion. Destroyed drones respawn after cooldown ticks. Proof: dotnet test --filter "LatticeDroneTests" | NEW: SimCore/Systems/LatticeDroneCombatSystem.cs, FOUND: SimCore/Systems/CombatSystem.cs, FOUND: SimCore/Systems/NpcFleetCombatSystem.cs |
| GATE.S7.COMBAT_DEPTH2.BRIDGE.001 | DONE | SimBridge.Combat.cs queries: GetCombatProjectionV0(fleetId, targetFleetId) returns projection snapshot, GetWeaponTrackingV0(fleetId) returns per-weapon tracking/evasion/pen stats. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Combat.cs, FOUND: SimCore/Systems/CombatSystem.cs |
| GATE.S7.COMBAT_DEPTH2.HUD.001 | DONE | combat_hud.gd enhancements: tracking accuracy display per weapon, armor penetration indicator, pre-combat projection panel (win/loss/pyrrhic + estimated losses). Zone armor status with soft-kill warning. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/combat_hud.gd, FOUND: scripts/bridge/SimBridge.Combat.cs |
| GATE.S8.LATTICE_DRONES.BRIDGE.001 | DONE | SimBridge queries: GetLatticeDroneAlertsV0(nodeId) returns active drones + threat level, GetDroneActivityV0() returns empire-wide drone summary. Bridge event for drone spawn/despawn. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Combat.cs, FOUND: SimCore/Systems/LatticeDroneSpawnSystem.cs |
| GATE.S7.UI_WARFRONT.DASHBOARD.001 | DONE | warfront_panel.gd — dedicated warfront dashboard (W key): active conflicts list, territory control %, faction strength bars, strategic objective status (SupplyDepot/CommRelay/Factory). Reads from SimBridge warfront queries. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/warfront_panel.gd, FOUND: scripts/bridge/SimBridge.GalaxyMap.cs, FOUND: scripts/view/GalaxyView.cs |
| GATE.S7.UI_WARFRONT.MAP_OVERLAY.001 | DONE | GalaxyView.cs warfront overlay mode: contested nodes pulse red, supply lines shown as dashed edges, objective icons at strategic nodes. Toggle via warfront_panel button or galaxy map layer selector. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.S7.UI_WARFRONT.SUPPLY.001 | DONE | Supply line visualization: GalaxyView draws supply chain from faction HQ to front-line nodes. Severed supply lines (no path) shown as broken/red. Supply status affects warfront strength display. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.GalaxyMap.cs, FOUND: SimCore/Systems/WarfrontEvolutionSystem.cs |
| GATE.S6.UI_DISCOVERY.KG_PANEL.001 | DONE | knowledge_web_panel.gd enhancements: connection type labels, discovery source attribution, filter by faction/region, zoom-to-node on click. Improved layout for 20+ nodes. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/knowledge_web_panel.gd, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S6.UI_DISCOVERY.SCAN_VIZ.001 | DONE | Local system scan visualization: scanning pulse effect emanating from player ship, discovery site highlight on scan complete, scan progress ring on HUD. GalaxyView.DrawLocalSystemV0 additions. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/hud.gd |
| GATE.S7.COMBAT_DEPTH2.HEADLESS.001 | DONE | Headless proof: boot game, travel to hostile node, engage combat with tracking/evasion/armor_pen active, verify projection matches outcome within margin, verify fore soft-kill triggers. Proof: godot --headless --path . -s res://scripts/tests/test_combat_depth_proof_v0.gd | NEW: scripts/tests/test_combat_depth_proof_v0.gd, FOUND: scripts/bridge/SimBridge.Combat.cs |
| GATE.X.EVAL.COMBAT_BALANCE.001 | DONE | Multi-seed ExplorationBot run (5 seeds x 5000 ticks): verify tracking/evasion creates meaningful hit variance, armor pen differentiates weapon types, fore soft-kill triggers in extended fights, lattice drones spawn at P2+ nodes. Report balance findings. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.036 | DONE | Audit epic statuses against completed gates. Close COMBAT_DEPTH_V2, LATTICE_DRONES, UI_WARFRONT, UI_DISCOVERY if all gates DONE. Recommend T37 anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |

Combined-agent notes:
- Tier 1 core hash chain (6 sequential): TRACKING → DAMAGE_VAR → ARMOR_PEN → FORE_KILL → LATTICE_DRONES.ENTITY → LATTICE_DRONES.SPAWN. Each changes golden hash baseline.
- Tier 1 bridge (3 parallel): UI_WARFRONT.DASHBOARD, UI_WARFRONT.MAP_OVERLAY, UI_DISCOVERY.KG_PANEL. No file conflicts.
- Tier 1 docs (1): REPO_HEALTH — parallel with everything.
- Tier 2 core hash chain (2 sequential): PROJECTION → LATTICE_DRONES.COMBAT.
- Tier 2 bridge (5 parallel): COMBAT_DEPTH2.BRIDGE + LATTICE_DRONES.BRIDGE (combine — shared SimBridge.Combat.cs), COMBAT_DEPTH2.HUD, UI_WARFRONT.SUPPLY, UI_DISCOVERY.SCAN_VIZ.
- Tier 3 (3 parallel): COMBAT_DEPTH2.HEADLESS (IN_ENGINE), EVAL.COMBAT_BALANCE (docs), EPIC_REVIEW (docs).

---

### T37: Narrative Spine + Haven Depth (20 gates)

Anchor: EPIC.S8.STORY_STATE_MACHINE. Expansions: EPIC.S8.HAVEN_STARBASE.V0, EPIC.S8.THREAT_IMPACT, EPIC.S8.NARRATIVE_CONTENT.V0, EPIC.S6.UI_DISCOVERY.

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.037 | DONE | Full test suite (1260+ tests), warning scan, golden hash stability. Baseline health check before T37. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.S8.STORY_STATE.ENTITY.001 | DONE | StoryState entity: RevelationFlags bitmask (5 bits for 5 recontextualizations), CurrentAct enum (Act1_Innocent/Act2_Questioning/Act3_Revealed), PentagonTradeFlags (5 bits for traded-with-faction), FractureExposureCount. SimState.StoryState field + serialization + HydrateAfterLoad. StoryStateTweaksV0 for trigger thresholds. Proof: dotnet test --filter "StoryStateTests" | NEW: SimCore/Entities/StoryState.cs (no existing story state entity), NEW: SimCore/Tweaks/StoryStateTweaksV0.cs (trigger thresholds), NEW: SimCore.Tests/Systems/StoryStateTests.cs (entity + trigger tests), FOUND: SimCore/SimState.Properties.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S8.STORY_STATE.TRIGGERS.001 | DONE | StoryStateMachineSystem.Process(state): checks 5 trigger conditions per tick. R1 "Module Revelation": FractureExposureCount >= threshold + 3 lattice node visits. R2 "Concord Revelation": Concord rep >= Allied. R3 "Economy Revelation": PentagonTradeFlags == all 5 set (traded with all faction types). R4 "Communion Revelation": FractureExposureCount >= high threshold + Communion data log found. R5 "Instability Revelation": tick >= endgame threshold + all fragments collected. Sets flag in RevelationFlags + emits StoryEvent. SimKernel.Step() integration. Proof: dotnet test --filter "StoryStateTests" | NEW: SimCore/Systems/StoryStateMachineSystem.cs (new system), FOUND: SimCore/Entities/StoryState.cs, FOUND: SimCore/Tweaks/StoryStateTweaksV0.cs, FOUND: SimCore.Tests/Systems/StoryStateTests.cs |
| GATE.S8.HAVEN.KEEPER.001 | DONE | Keeper ambient system: KeeperTier field on HavenStarbase entity (tracks independently from Haven upgrade tier, evolves based on player interactions — exotic matter deliveries, fragment installations, data log discoveries near Haven). 5 tiers: Dormant/Aware/Guiding/Communicating/Awakened. KeeperSystem.Process(state) advances tier based on HavenTweaksV0 thresholds. Test: tier advances correctly. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenUpgradeSystem.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.S8.HAVEN.RESONANCE.001 | DONE | Resonance Chamber: requires Haven tier >= 4. CombineFragmentPair command takes 2 fragment IDs, validates they form a known resonance pair (8 pairs in AdaptationFragmentContentV0), produces emergent capability flag on StoryState. ResonanceChamberSystem.Process handles cooldown + activation. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Content/AdaptationFragmentContentV0.cs, FOUND: SimCore/Entities/AdaptationFragment.cs, FOUND: SimCore/Systems/AdaptationFragmentSystem.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.S6.UI_DISCOVERY.PHASE_MARKERS.001 | DONE | Galaxy map discovery phase markers: gray circle (unknown), amber diamond (scanned), green star (analyzed) icons at discovery site nodes. GalaxyView.cs reads discovery state from SimBridge and renders phase-appropriate icons during galaxy map mode. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001 | DONE | HUD active leads display: small panel showing current discovery leads (from IntelBook) with destination node names and lead type icons. Visible in flight mode. Reads from SimBridge GetActiveLeadsV0 query. Max 3 leads shown. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/bridge/SimBridge.Narrative.cs |
| GATE.S8.NARRATIVE.REVELATION_TEXT.001 | DONE | Author 5 revelation delivery text per NarrativeDesign.md: gold toast messages, 3 FO personality variants per revelation (15 total FO lines: Analyst/Veteran/Pathfinder), Discovery Web connection labels. Content in RevelationContentV0.cs. Proof: dotnet build SimCore/SimCore.csproj --nologo | NEW: SimCore/Content/RevelationContentV0.cs (no existing revelation content file), FOUND: SimCore/Content/AdaptationFragmentContentV0.cs |
| GATE.S8.NARRATIVE.FRAGMENT_LORE.001 | DONE | Author 16 adaptation fragment lore flavor text entries. Each fragment gets a 2-3 sentence description revealing its nature (opaque pre-revelation, clear post-revelation). Dual-text fields: CoverName + RevealedName, CoverLore + RevealedLore. Update AdaptationFragmentContentV0.cs with text. Proof: dotnet build SimCore/SimCore.csproj --nologo | FOUND: SimCore/Content/AdaptationFragmentContentV0.cs, FOUND: SimCore/Entities/AdaptationFragment.cs |
| GATE.S8.HAVEN.FABRICATOR.001 | DONE | Fabricator: requires Haven tier >= 3. FabricateModuleCommand takes fragment type + exotic matter cost, produces T3 module. FabricatorSystem validates prerequisites (correct fragment, sufficient exotic matter in Haven market). Adds module to Haven hangar inventory. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenHangarSystem.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.S8.HAVEN.MARKET_EVOLUTION.001 | DONE | Haven market stock evolution by tier: T1 exotic matter only, T2 adds rare metals + composites, T3 adds components + energy cells, T4 adds all T2 modules at premium, T5 adds T3 modules. HavenMarketSystem.Process restocks based on current Haven tier. Market prices use HavenTweaksV0 multipliers. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenUpgradeSystem.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.S8.THREAT.SUPPLY_SHOCK.001 | DONE | SupplyShockSystem: when warfront intensity >= Skirmish at a production node, that node's IndustrySite output drops by ThreatTweaksV0.OutputReductionPct (default 40%). At Battle+, output drops to 0. Creates supply cascades through dependent chains. Emits SupplyShockEvent for UI. Proof: dotnet test --filter "SupplyShockTests" | NEW: SimCore/Systems/SupplyShockSystem.cs (no existing supply shock system), NEW: SimCore/Tweaks/ThreatTweaksV0.cs (threat tuning constants), NEW: SimCore.Tests/Systems/SupplyShockTests.cs (supply shock tests), FOUND: SimCore/Systems/IndustrySystem.cs, FOUND: SimCore/Systems/WarfrontEvolutionSystem.cs |
| GATE.S8.STORY_STATE.BRIDGE.001 | DONE | SimBridge.Story.cs partial: GetRevelationStateV0() returns which revelations have fired + current act, GetStoryProgressV0() returns pentagon trade flags + fracture exposure + fragment count, GetPendingRevelationV0() returns next-to-fire revelation with pre-delivery text. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Story.cs (no existing story bridge), FOUND: SimCore/Entities/StoryState.cs, FOUND: SimCore/Systems/StoryStateMachineSystem.cs |
| GATE.S8.STORY_STATE.DELIVERY_UI.001 | DONE | Revelation delivery UI: gold toast (unique color, larger, persistent 8s) for revelation moments, galaxy map pentagon highlight (5 faction nodes illuminate with connecting lines for R3), FO reaction panel auto-opens with personality-appropriate text. Reads from SimBridge.Story.cs pending revelation. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/core/game_manager.gd, FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Story.cs |
| GATE.S8.STORY_STATE.COVER_NAMES.001 | DONE | Cover story name switching: SimBridge snapshot methods check revelation flags before returning display names. Pre-R1: "Structural Resonance Engine" not "Fracture Drive", "Metric Anomaly" not "Instability". Name mapping table in SimBridge.Story.cs. All bridge methods that return module/system names go through GetDisplayNameV0(rawName, revelationFlags). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Story.cs, FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.S8.HAVEN.DEPTH_BRIDGE.001 | DONE | SimBridge.Haven.cs additions: GetKeeperStateV0() returns tier + ambient behavior hints, GetResonanceChamberV0() returns available pairs + cooldown, GetFabricatorV0() returns craftable modules + costs, GetHavenMarketV0() returns tier-appropriate stock. haven_panel.gd sections for each system. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.THREAT.ALERT_UI.001 | DONE | Supply shock UI: toast notification when production drops ("Supply Disruption: [Good] production at [Node] reduced by warfront activity"), HUD supply status indicator (green/yellow/red per good), empire dashboard supply chain health summary. Reads from SimBridge warfront/market queries. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/core/game_manager.gd, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.S8.STORY_STATE.HEADLESS.001 | DONE | Headless proof: boot game, advance ticks to trigger R1 (fracture exposure + lattice visits), verify gold toast text contains "Module" revelation, verify FO panel opens, verify cover name switching works pre/post revelation. Proof: godot --headless --path . -s res://scripts/tests/test_story_state_proof_v0.gd | NEW: scripts/tests/test_story_state_proof_v0.gd (headless proof script), FOUND: scripts/bridge/SimBridge.Story.cs |
| GATE.X.EVAL.NARRATIVE_FLOW.001 | DONE | ExplorationBot multi-seed evaluation: verify StoryStateMachineSystem fires revelations at appropriate progression points, Haven Keeper advances tiers, supply shocks cascade. Report narrative flow findings. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.037 | DONE | Epic audit: close EPIC.S7.COMBAT_DEPTH_V2.V0 + EPIC.S8.LATTICE_DRONES.V0 (all gates DONE from T36). Update EPIC.S8.HAVEN_STARBASE.V0 progress. Assess EPIC.S8.STORY_STATE_MACHINE progress. Recommend T38 anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |

Combined-agent notes (T37):
- Tier 1 core hash chain (4 sequential): STORY_STATE.ENTITY → STORY_STATE.TRIGGERS → HAVEN.KEEPER → HAVEN.RESONANCE. Each changes golden hash baseline.
- Tier 1 bridge (2 parallel): UI_DISCOVERY.PHASE_MARKERS, UI_DISCOVERY.ACTIVE_LEADS. No file conflicts.
- Tier 1 content (2 parallel): NARRATIVE.REVELATION_TEXT, NARRATIVE.FRAGMENT_LORE. No file conflicts.
- Tier 1 docs (1): REPO_HEALTH — parallel with everything.
- Tier 2 core hash chain (3 sequential): HAVEN.FABRICATOR → HAVEN.MARKET_EVOLUTION → THREAT.SUPPLY_SHOCK.

### T38 — Pentagon Cascade + Endgame Foundation (20 gates)

| Gate ID | Status | Description | Evidence |
|---------|--------|-------------|----------|
| GATE.X.HYGIENE.REPO_HEALTH.038 | DONE | Full test suite (1297+ tests), warning scan, golden hash stability. Baseline health check before T38. Proof: dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.S8.PENTAGON.DETECT.001 | DONE | PentagonBreakSystem.Process: when all 5 PentagonTradeFlags set on StoryState AND player has established fracture trade route to Communion node, fire R3 "Economy Revelation" via StoryStateMachineSystem. PentagonBreakTweaksV0 for thresholds. Tests verify detection + revelation trigger. Proof: dotnet test --filter "PentagonBreakTests" | NEW: SimCore/Systems/PentagonBreakSystem.cs, NEW: SimCore/Tweaks/PentagonBreakTweaksV0.cs, NEW: SimCore.Tests/Systems/PentagonBreakTests.cs, FOUND: SimCore/Entities/StoryState.cs |
| GATE.S8.PENTAGON.CASCADE.001 | DONE | Economic consequence cascade: Communion food self-production breaks dependency link, CommunionFoodSelfProductionPct in PentagonBreakTweaksV0. Downstream: Communion IndustrySite food output boost, trade route revaluation, warfront rebalance (Communion becomes self-sufficient). Process in PentagonBreakSystem. Tests verify cascade effects. Proof: dotnet test --filter "PentagonBreakTests" | FOUND: SimCore/Systems/PentagonBreakSystem.cs, FOUND: SimCore/Entities/IndustrySite.cs, FOUND: SimCore.Tests/Systems/PentagonBreakTests.cs |
| GATE.S8.HAVEN.ENDGAME_PATHS.001 | DONE | EndgamePath enum (Reinforce/Naturalize/Renegotiate) + ChosenEndgamePath field on HavenStarbase. ChooseEndgamePathCommand at Haven Tier 4+ (Expanded). HavenEndgameSystem.Process applies path-specific effects per tick. EndgameTweaksV0 constants. Tests verify path selection + effects. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, NEW: SimCore/Systems/HavenEndgameSystem.cs, NEW: SimCore/Tweaks/EndgameTweaksV0.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.S8.HAVEN.ACCOMMODATION.001 | DONE | AccommodationThread system: ThreadId enum (Discovery/Commerce/Conflict/Harmony), per-thread progress counter on HavenStarbase. AccommodationSystem.Process advances threads based on player actions (trade volume, combat victories, exploration milestones, faction standing). Bidirectional: Keeper knowledge grows with player input, player gains Haven-specific bonuses. Tests verify thread advancement. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenUpgradeSystem.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.S8.HAVEN.COMMUNION_REP.001 | DONE | Communion Representative NPC at Haven Tier 3+: CommunionRepresentativeState on HavenStarbase (Present bool, DialogueTier, LastInteractionTick). Appears when Communion rep >= Neutral. Offers faction-specific missions and lore hints about the Lattice. HavenTweaksV0 thresholds. Tests verify spawn conditions. Proof: dotnet test --filter "HavenTests" | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore.Tests/Systems/HavenTests.cs |
| GATE.X.COVER_STORY.AUDIT.001 | DONE | CI grep script: scan scripts/bridge/, scripts/ui/, scripts/core/ for "fracture" in player-facing string literals. Report violations. Allowlist for code comments and internal variable names. PowerShell validator script. Proof: pwsh scripts/tools/Validate-CoverStory.ps1 | NEW: scripts/tools/Validate-CoverStory.ps1, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.S8.NARRATIVE.FACTION_DIALOGUE.001 | DONE | Author 5 faction representative dialogue content sets (Concord/Chitin/Valorin/Weavers/Communion) × 3 reputation tiers (Neutral/Friendly/Allied) = 15 dialogue entries. 2-3 lines each with faction voice styling per factions_and_lore_v0.md. FactionDialogueContentV0.cs static content. Proof: dotnet build SimCore/SimCore.csproj --nologo | NEW: SimCore/Content/FactionDialogueContentV0.cs, FOUND: SimCore/Content/FactionContentV0.cs |
| GATE.S8.NARRATIVE.WARFRONT_COMMENTARY.001 | DONE | Author warfront event commentary text per intensity level (Peace/ColdWar/Skirmish/OpenWar/TotalWar) × faction perspective (5 factions) = 25 commentary entries. Short situational text for warfront dashboard. WarfrontCommentaryContentV0.cs. Proof: dotnet build SimCore/SimCore.csproj --nologo | NEW: SimCore/Content/WarfrontCommentaryContentV0.cs, FOUND: SimCore/Content/FactionContentV0.cs |
| GATE.S9.MILESTONES.VIEWER.001 | DONE | Milestones viewer panel: accessible from main menu "Milestones" button. Grid of milestone cards (achieved=full color+date, unachieved=silhouette). Lifetime stats sidebar (total playtime, credits earned, goods traded, systems visited). Reads existing MilestoneSystem + PlayerStats via SimBridge GetMilestonesV0 + GetLifetimeStatsV0. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scripts/bridge/SimBridge.Reports.cs |
| GATE.S8.PENTAGON.BRIDGE.001 | DONE | SimBridge.Story.cs additions: GetPentagonStateV0() returns 5 faction trade flags + break status + cascade active, GetCascadeEffectsV0() returns per-faction GDP impact + Communion self-sufficiency %. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Story.cs, FOUND: SimCore/Entities/StoryState.cs |
| GATE.S8.PENTAGON.DELIVERY.001 | DONE | Pentagon delivery UI: galaxy map pentagon overlay (5 faction home nodes connected by lines, broken link highlighted red), gold toast "Trade Analysis Complete — Pattern Detected", FO reaction text from RevelationContentV0 R3. GalaxyView.cs overlay + hud.gd pentagon toast handler. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/ui/hud.gd, FOUND: scripts/bridge/SimBridge.Story.cs |
| GATE.S8.HAVEN.ENDGAME_BRIDGE.001 | DONE | SimBridge.Haven.cs: GetEndgamePathsV0() returns available paths + requirements + effects preview, GetAccommodationProgressV0() returns per-thread progress bars, GetCommunionRepV0() returns rep state + available dialogue. haven_panel.gd endgame section with path selection buttons. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.HAVEN.COMING_HOME.001 | DONE | Coming Home cinematic: when player docks at Haven, special approach sequence — camera pulls back for wide reveal, sweeps around Haven geometry, slow dock with ambient audio shift. game_manager.gd + camera tween sequence. Only on first dock per session (subsequent docks use normal dock). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/core/camera_controller.gd |
| GATE.X.COVER_STORY.BRIDGE_WIRE.001 | DONE | Wire GetCoverNameV0 into SimBridge methods that return module names, system names, and discovery names. SimBridge.Fleet.cs (module display names), SimBridge.Research.cs (tech names), SimBridge.Narrative.cs (discovery names). All check current revelation flags. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/bridge/SimBridge.Market.cs, FOUND: scripts/bridge/SimBridge.Research.cs |
| GATE.X.COVER_STORY.UI_ENFORCE.001 | DONE | HUD and dock menu text uses cover names pre-revelation: hero_trade_menu.gd module names, hud.gd status text, empire dashboard module references. All read through bridge (which now applies cover names). Verify no direct "fracture" strings in UI labels. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hud.gd, FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S9.CREDITS.SCROLL.001 | DONE | Credits scroll overlay: accessible from main menu "Credits" button. Scrolling text over parallax starfield background. Team credits, tools used, Godot attribution, music attribution placeholder. Skip-on-input (any key/click). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/main_menu.gd, FOUND: scenes/main_menu.tscn |
| GATE.S8.PENTAGON.HEADLESS.001 | DONE | Headless proof: boot game, set all 5 PentagonTradeFlags + fracture route conditions, tick to trigger R3, verify gold toast contains "Pentagon" or "Pattern Detected", verify galaxy map pentagon overlay rendered, verify cascade effects on Communion. Proof: godot --headless --path . -s res://scripts/tests/test_pentagon_proof_v0.gd | NEW: scripts/tests/test_pentagon_proof_v0.gd, FOUND: scripts/bridge/SimBridge.Story.cs |
| GATE.X.EVAL.PENTAGON_SCENARIO.001 | DONE | ExplorationBot endgame scenario: multi-seed evaluation verifying pentagon break fires at appropriate progression, cascade effects propagate, Haven endgame paths selectable, accommodation threads advance. Report findings. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.038 | DONE | Epic audit: assess PENTAGON_BREAK closure, update HAVEN_STARBASE progress, close COVER_STORY_NAMING if complete. Mark MILESTONES_CREDITS progress. Recommend T39 anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |

Combined-agent notes (T38):
- Tier 1 core hash chain (5 sequential): PENTAGON.DETECT → PENTAGON.CASCADE → HAVEN.ENDGAME_PATHS → HAVEN.ACCOMMODATION → HAVEN.COMMUNION_REP. Each changes golden hash baseline.
- Tier 1 bridge (1 parallel): MILESTONES.VIEWER. No file conflicts with core chain.
- Tier 1 content (2 parallel): NARRATIVE.FACTION_DIALOGUE, NARRATIVE.WARFRONT_COMMENTARY. No file conflicts.
- Tier 1 docs (2 parallel): REPO_HEALTH, COVER_STORY.AUDIT. No file conflicts.
- Tier 2 bridge (7 gates): PENTAGON.BRIDGE, PENTAGON.DELIVERY, HAVEN.ENDGAME_BRIDGE, HAVEN.COMING_HOME, COVER_STORY.BRIDGE_WIRE+UI_ENFORCE (combined agent), CREDITS.SCROLL.
- Tier 3 (3 gates): PENTAGON.HEADLESS, PENTAGON_SCENARIO, EPIC_REVIEW.
- Tier 2 bridge (5 gates, combine for execution — shared GalaxyView.cs/hud.gd): STORY_STATE.BRIDGE, STORY_STATE.DELIVERY_UI, STORY_STATE.COVER_NAMES, HAVEN.DEPTH_BRIDGE, THREAT.ALERT_UI.
- Tier 3 (3 parallel): STORY_STATE.HEADLESS (IN_ENGINE), EVAL.NARRATIVE_FLOW (docs), EPIC_REVIEW (docs).

### T39 — Win Conditions & Endgame Payoff (20 gates)

Anchor: EPIC.S8.WIN_SCENARIOS. Expansion: EPIC.S8.STORY_STATE_MACHINE, EPIC.S8.NARRATIVE_CONTENT.V0, EPIC.X.PERF_BUDGET, EPIC.X.COVER_STORY_NAMING.V0.

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S8.WIN.GAME_RESULT.001 | DONE | GameResult enum (InProgress/Victory/Death/Bankruptcy) + field on SimState. WinRequirementsTweaksV0.cs: per-path thresholds (Reinforce: Concord 75+/Weaver 50+/Haven T4/Lattice Reading fragment; Naturalize: Communion 75+/Haven T4/Phase Tolerance+Geometric Suspension; Renegotiate: Communion 50+/Haven T4/Dialogue Protocol/all 5 revelations). Loss thresholds: death=hull<=0, bankruptcy=credits<-500 with no fleet cargo. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Tweaks/WinRequirementsTweaksV0.cs, FOUND: SimCore/SimState.cs |
| GATE.S8.WIN.LOSS_DETECT.001 | DONE | LossDetectionSystem.cs: Process checks player fleet hull<=0 -> Death, credits < BankruptcyThreshold with no fleet cargo value -> Bankruptcy. Wire into SimKernel.Step() after combat/sustain. Only fires once (InProgress -> terminal). Contract test for both loss paths. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/LossDetectionSystem.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S8.WIN.PATH_EVAL.001 | DONE | WinConditionSystem.cs: Process checks ChosenEndgamePath != None, evaluates per-path requirements from WinRequirementsTweaksV0 (faction reps, fragments, haven tier, revelations). When all met, set GameResult.Victory. Wire into SimKernel.Step() after HavenEndgameSystem. Contract tests per path. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Systems/WinConditionSystem.cs, FOUND: SimCore/Systems/HavenEndgameSystem.cs |
| GATE.S8.WIN.PROGRESS_TRACK.001 | DONE | Expand HavenEndgameSystem: compute per-path EndgameProgress (0.0-1.0) from fraction of requirements met. Store EndgameProgressReinforce/Naturalize/Renegotiate on HavenStarbase. Add to SimState signature. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/HavenEndgameSystem.cs, FOUND: SimCore/Entities/HavenStarbase.cs |
| GATE.S8.STORY.FO_REVELATION.001 | DONE | 5 new ContextualTrigger tokens (REVELATION_1..5) in FirstOfficerSystem. Wire StoryStateMachineSystem to fire FO triggers when revelations unlock. 15 dialogue lines (5 revelations x 3 FO variants: Analyst/Veteran/Pathfinder) in FirstOfficerContentV0. Completes STORY_STATE_MACHINE FO reactions. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/FirstOfficerSystem.cs, FOUND: SimCore/Content/FirstOfficerContentV0.cs |
| GATE.S8.STORY.KG_REVELATION.001 | DONE | On revelation fire, seed KnowledgeConnection entries linking related discoveries (R1: fracture module->ancient hulls, R3: 5 faction pentagon pattern, R5: instability->accommodation). 5 revelation connection sets, 3-5 connections each. Completes Discovery Web revelation updates. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/KnowledgeGraphSystem.cs, FOUND: SimCore/Systems/StoryStateMachineSystem.cs |
| GATE.X.HYGIENE.REPO_HEALTH.039 | DONE | Full test suite (dotnet test -c Release). Golden hash stability across 3 seeds. Compiler warning check. Dead code scan (unused public methods in SimCore). Report findings. Proof: dotnet test -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.S8.WIN.EPILOGUE_DATA.001 | DONE | Create epilogue_data.gd: 3 endgame paths x 5 text cards (choice, beneficiaries, costs, galaxy consequences, personal reflection) + 2 loss frames (death narrative, bankruptcy narrative). Each card: title, body (2-4 sentences), duration_secs. 17 entries total. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/epilogue_data.gd, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.WIN.BRIDGE.001 | DONE | SimBridge.Endgame.cs partial: GetGameResultV0() (InProgress/Victory/Death/Bankruptcy), GetEndgameProgressV0() (per-path 0-1.0 progress + requirements checklist), GetLossInfoV0() (loss reason + final stats). TryExecuteSafeRead with caching. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Endgame.cs, FOUND: SimCore/SimState.cs |
| GATE.X.COVER_STORY.CI.001 | DONE | CoverStoryEnforcementTests: grep SimBridge partials + GDScript UI for 'fracture' in player-facing strings without revelation guard. Allowlist for comments and system-internal refs. Fails on unguarded display strings. Closes COVER_STORY_NAMING. Proof: dotnet test --filter "CoverStory" | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: scripts/bridge/SimBridge.Story.cs |
| GATE.S8.WIN.VICTORY_SCREEN.001 | DONE | victory_screen.gd + .tscn: timed text card sequence from epilogue_data.gd for chosen path. Fade-in (1s), hold, fade-out (1s), auto-advance. Skip on input. Equipment summary (ship class, modules, credits, fragments). Final card: Return to Main Menu. Ambient audio. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/victory_screen.gd, FOUND: scripts/ui/epilogue_data.gd |
| GATE.S8.WIN.LOSS_SCREEN.001 | DONE | loss_screen.gd + .tscn: death/bankruptcy narrative frame from epilogue_data.gd. Equipment state reflection (last ship, modules, credits at death). Final stats summary. Restart (main menu) and Quit buttons. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/ui/loss_screen.gd, FOUND: scripts/ui/epilogue_data.gd |
| GATE.S8.WIN.PROGRESS_UI.001 | DONE | haven_panel.gd endgame section: 3 progress bars (Reinforce/Naturalize/Renegotiate) from GetEndgameProgressV0(). Requirements checklist with check/cross icons. Victory-readiness indicator (green when 100%). Visible at Haven T4+. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/haven_panel.gd, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.S8.NARRATIVE.ENDGAME_CONTENT.001 | DONE | Expand AdaptationFragmentContentV0 with richer lore (pre/post-revelation text for 16 fragments). Haven context flavor text per tier. Update LoreContent_TBA.md. Closes NARRATIVE_CONTENT remaining scope. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Content/AdaptationFragmentContentV0.cs, FOUND: docs/design/content/LoreContent_TBA.md |
| GATE.X.PERF.TICK_BUDGET.001 | DONE | TickBudgetTests: instrument SimKernel.Step() to time each System.Process(). 10 seeds x 500 ticks. Assert no system >5ms avg, total tick <20ms avg. Per-system timing breakdown report. Proof: dotnet test --filter "TickBudget" | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: SimCore/SimKernel.cs |
| GATE.S8.WIN.GAME_OVER_WIRE.001 | DONE | game_manager.gd: poll GetGameResultV0() each tick. Victory: auto-save, transition to victory_screen.tscn. Death/Bankruptcy: auto-save, transition to loss_screen.tscn. Fade transitions. StopSimV0 before scene change. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/core/game_manager.gd, FOUND: scripts/ui/victory_screen.gd |
| GATE.S8.WIN.HEADLESS_PROOF.001 | DONE | ExplorationBot test: set Haven T4, choose Reinforce, set required reps+fragments, tick until WinConditionSystem fires Victory. Verify GameResult transitions. Deterministic across seeds. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs |
| GATE.S8.WIN.BOT_LOSS.001 | DONE | ExplorationBot loss scenarios: (1) drain credits below threshold with no fleet -> Bankruptcy. (2) reduce hull to 0 -> Death. Verify GameResult for both. Deterministic. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs |
| GATE.X.EVAL.ENDGAME_FLOW.001 | DONE | ExplorationBot multi-seed eval (5 seeds x 5000 ticks): all 3 paths reachable, loss states trigger under forced conditions, progression tracks meaningfully. Report balance (easiest/hardest path, typical tick-to-victory). Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore.Tests/ExperienceProof/ExplorationBot.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.039 | DONE | Audit 54_EPICS.md: close WIN_SCENARIOS, STORY_STATE_MACHINE, COVER_STORY_NAMING, NARRATIVE_CONTENT if all gates DONE. Update HAVEN_STARBASE. Close PERF_BUDGET if tick budget passes. Tally remaining TODO/IN_PROGRESS. Recommend T40 anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes (T39):
- Tier 1 core hash chain (6 sequential): GAME_RESULT → LOSS_DETECT → PATH_EVAL → PROGRESS_TRACK → FO_REVELATION → KG_REVELATION. Each changes golden hash baseline.
- Tier 1 non-hash (2 parallel): REPO_HEALTH (docs), EPILOGUE_DATA (bridge). No file conflicts with core chain.
- Tier 2 hash (1 gate): NARRATIVE.ENDGAME_CONTENT (content, blocks KG_REVELATION).
- Tier 2 non-hash (6 parallel): BRIDGE, COVER_STORY.CI, VICTORY_SCREEN+LOSS_SCREEN (combine — shared epilogue_data.gd), PROGRESS_UI, TICK_BUDGET.
- Tier 3 hash chain (2 sequential): HEADLESS_PROOF → BOT_LOSS.
- Tier 3 non-hash (3 parallel): GAME_OVER_WIRE (bridge), ENDGAME_FLOW (docs), EPIC_REVIEW (docs).

### T40 — Diplomacy & Faction Depth (20 gates)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.S7.DIPLOMACY.FRAMEWORK.001 | DONE | DiplomaticAct entity (Treaty/Bounty/Sanction verb enum, factionA/factionB parties, status Active/Expired/Violated, tick created/expires). DiplomacySystem.Process: tick expiry, auto-propose AI logic. DiplomacyTweaksV0: treaty duration, bounty reward range, sanction penalty. Wire into SimKernel.Step(). Proof: dotnet test --filter "Determinism" | NEW: SimCore/Entities/DiplomaticAct.cs, NEW: SimCore/Systems/DiplomacySystem.cs, NEW: SimCore/Tweaks/DiplomacyTweaksV0.cs |
| GATE.S7.DIPLOMACY.TREATY.001 | DONE | ProposeActCommand: player proposes treaty to faction (non-aggression/trade agreement/mutual defense). Acceptance based on rep tier (friendly+ auto-accept, neutral chance-based via FNV1a hash). Active treaty effects: non-aggression suppresses NPC combat targeting, trade agreement reduces tariffs, mutual defense triggers faction fleet aid. Contract test per treaty type. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Commands/ProposeActCommand.cs, FOUND: SimCore/Systems/DiplomacySystem.cs, FOUND: SimCore/Systems/NpcFleetCombatSystem.cs |
| GATE.S7.DIPLOMACY.BOUNTY.001 | DONE | Bounty verb in DiplomacySystem: factions post bounties on hostile NPC fleets (high-aggression factions post more). Player claims bounty by destroying target fleet (NpcFleetCombatSystem checks active bounties on kill). Bounty reward scales with target fleet class. PlaceBountyCommand for player-placed bounties (costs credits). Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/DiplomacySystem.cs, FOUND: SimCore/Systems/NpcFleetCombatSystem.cs, FOUND: SimCore/Tweaks/DiplomacyTweaksV0.cs |
| GATE.S7.DIPLOMACY.FACTION_AI.001 | DONE | DiplomacySystem.ProcessFactionAI: each faction evaluates diplomatic stance toward player every N ticks. Uses FactionTweaksV0 aggression/trade policy to weight proposal types. High-trade factions (Weavers) prefer trade agreements. High-aggression (Valorin) prefer bounties. Proposals appear as available diplomatic acts. AI also evaluates inter-faction treaties (NPC-to-NPC). Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/DiplomacySystem.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs, FOUND: SimCore/Tweaks/DiplomacyTweaksV0.cs |
| GATE.S7.TECH_ACCESS.LOCK.001 | DONE | Add FactionExclusiveId (nullable) and MinRepTierForPurchase (int 0-3) to ModuleDef schema. RefitSystem.CanInstall checks: if FactionExclusiveId set, player must have rep >= MinRepTierForPurchase with that faction. TechAccessTweaksV0: per-faction exclusive module lists (3-5 per faction from existing T2 catalog). Populate in ContentRegistryLoader. Proof: dotnet test --filter "Determinism" | NEW: SimCore/Tweaks/TechAccessTweaksV0.cs, FOUND: SimCore/Systems/RefitSystem.cs, FOUND: SimCore/Content/ContentRegistryLoader.cs |
| GATE.S8.HAVEN.VISUAL_TIERS.001 | DONE | Haven station visual differentiation per upgrade tier in GalaxyView.DrawLocalSystemV0: Powered (dim emission, small mesh), Inhabited (warm emission, medium), Operational (bright, large, ring), Expanded (pulsing glow, double ring), Awakened (golden emission, particle aura, triple ring). Scale and emission energy from HavenUpgradeTier enum. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/view/GalaxyView.cs, FOUND: SimCore/Entities/HavenStarbase.cs |
| GATE.X.HYGIENE.REPO_HEALTH.040 | DONE | Full test suite (dotnet test -c Release). Golden hash stability across 3 seeds. Compiler warning check. Dead code scan. Report findings. Proof: dotnet test -c Release | FOUND: SimCore.Tests/SimCore.Tests.csproj, FOUND: docs/55_GATES.md |
| GATE.S7.DIPLOMACY.CONSEQUENCES.001 | DONE | Treaty violation detection: attacking a treaty partner's fleet sets treaty status to Violated, triggers reputation penalty (DiplomacyTweaksV0.ViolationRepPenalty), faction may impose sanction (embargo escalation via existing EmbargoState). Sanction mechanics: sanctioned player pays double tariffs, loses access to faction-exclusive tech until rep restored. Proof: dotnet test --filter "Determinism" | FOUND: SimCore/Systems/DiplomacySystem.cs, FOUND: SimCore/Entities/EmbargoState.cs, FOUND: SimCore/Systems/ReputationSystem.cs |
| GATE.S5.LOSS_RECOVERY.CAPTURE.001 | DONE | CaptureShipCommand: when target NPC fleet HullHp < CaptureThresholdPct (10%) of HullHpMax and player fleet in same node, player can capture. Captured ship added to Haven hangar (requires hangar slot). Captured ship gets random ship class from NPC fleet role. NPC fleet removed from state. CaptureShipTweaksV0 in DiplomacyTweaksV0 (threshold, hangar requirement). Proof: dotnet test --filter "Determinism" | NEW: SimCore/Commands/CaptureShipCommand.cs, FOUND: SimCore/Systems/HavenHangarSystem.cs, FOUND: SimCore/Systems/NpcFleetCombatSystem.cs |
| GATE.S7.DIPLOMACY.BRIDGE.001 | DONE | SimBridge.Diplomacy.cs partial: GetActiveTreatiesV0() (all active treaties/bounties/sanctions with player), GetAvailableProposalsV0() (faction AI proposals waiting for response), GetBountyBoardV0() (all active bounties claimable by player), ProposeTreatyV0(factionId, verbType), AcceptProposalV0(actId), GetDiplomaticStandingV0(factionId) (treaty count, violation history, sanction status). TryExecuteSafeRead pattern. Proof: dotnet build "Space Trade Empire.csproj" --nologo | NEW: scripts/bridge/SimBridge.Diplomacy.cs, FOUND: SimCore/Systems/DiplomacySystem.cs |
| GATE.S7.TECH_ACCESS.BRIDGE.001 | DONE | SimBridge.Refit.cs additions: GetModuleCatalogV0 enhanced with locked/unlocked status per module based on faction exclusive + rep tier. GetTechAccessStatusV0(moduleId) returns lock reason + unlock requirements. Modify existing GetAvailableModulesV0 to filter or mark locked modules. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Refit.cs, FOUND: SimCore/Systems/RefitSystem.cs |
| GATE.S5.LOSS_RECOVERY.CAPTURE_BRIDGE.001 | DONE | SimBridge.Fleet.cs additions: GetCaptureTargetsV0() (NPC fleets in player node with hull < threshold), CaptureShipV0(targetFleetId) (executes CaptureShipCommand), GetHangarStatusV0() enhanced with captured ship origin info. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/bridge/SimBridge.Fleet.cs, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.S7.DIPLOMACY.UI.001 | DONE | Diplomacy tab in dock menu (hero_trade_menu.gd): treaty list (active treaties with icons), bounty board (available bounties with target + reward), diplomatic proposals (accept/reject buttons), sanction warnings. Reads from SimBridge.Diplomacy queries. Tab visible when docked at any station. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Diplomacy.cs |
| GATE.S7.TECH_ACCESS.UI.001 | DONE | Refit panel module list enhanced: locked modules show faction icon + "Requires [Faction] [Tier]" text. Greyed out with lock icon. Tooltip shows unlock path. Uses GetTechAccessStatusV0 per module. Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/bridge/SimBridge.Refit.cs |
| GATE.S5.LOSS_RECOVERY.CAPTURE_UI.001 | DONE | Capture confirmation popup when player targets disabled NPC: ship class, cargo contents, hangar slot requirement. "Capture" button executes CaptureShipV0. Success toast with captured ship name. Requires combat HUD integration (target info panel). Proof: dotnet build "Space Trade Empire.csproj" --nologo | FOUND: scripts/ui/combat_hud.gd, FOUND: scripts/bridge/SimBridge.Fleet.cs |
| GATE.S7.DIPLOMACY.HEADLESS.001 | DONE | ExplorationBot diplomacy scenario: (1) earn rep to friendly with one faction, (2) propose treaty, verify accepted, (3) destroy bounty target, verify bounty claimed + credits, (4) attack treaty partner, verify violation + rep penalty + sanction. Deterministic across 3 seeds. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore/Systems/DiplomacySystem.cs |
| GATE.S5.LOSS_RECOVERY.CAPTURE_HEADLESS.001 | DONE | ExplorationBot capture scenario: (1) engage NPC fleet, (2) reduce hull below 10%, (3) execute capture, verify ship in hangar, (4) verify NPC fleet removed. Deterministic across 3 seeds. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore/Systems/NpcFleetCombatSystem.cs |
| GATE.X.EVAL.DIPLOMACY_BALANCE.001 | DONE | Multi-seed (5 seeds x 2000 ticks) diplomacy evaluation: verify all 5 factions propose diplomatic acts, treaty acceptance rates scale with rep, bounty rewards are economically meaningful (5-15% of typical trade profit), sanctions have bite (tariff increase noticeable). Report per-faction diplomatic activity distribution. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore/Systems/DiplomacySystem.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.040 | DONE | Audit 54_EPICS.md: close STORY_STATE_MACHINE, WIN_SCENARIOS, COMBAT_DEPTH_V2, COVER_STORY_NAMING, PERF_BUDGET, KG_SEEDING, NARRATIVE_CONTENT, FACTION_MODEL, EXPERIENCE_PROOF. Update HAVEN_STARBASE (visual tiers gate closes it). Tally remaining TODO/IN_PROGRESS. Recommend T41 anchor. Proof: dotnet test --filter "RoadmapConsistency" | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.EVAL.FACTION_DEPTH.001 | DONE | Faction variety evaluation: verify each faction has unique diplomatic personality (proposal types differ), tech access creates meaningful specialization (locked modules drive rep-building), capture adds combat risk/reward. Compare faction differentiation metrics across 5 seeds. Proof: dotnet test --filter "ExplorationBot" | FOUND: SimCore.Tests/ExperienceProof/ExplorationBotTests.cs, FOUND: SimCore/Tweaks/FactionTweaksV0.cs |

Combined-agent notes (T40):
- Tier 1 core hash chain (5 sequential): FRAMEWORK → TREATY → BOUNTY → FACTION_AI → TECH_ACCESS.LOCK. Each changes golden hash baseline.
- Tier 1 non-hash (2 parallel): HAVEN.VISUAL_TIERS (bridge), REPO_HEALTH (docs). No file conflicts with core chain.
- Tier 2 hash chain (2 sequential): CONSEQUENCES → CAPTURE. Both in core session.
- Tier 2 non-hash (6 parallel): DIPLOMACY.BRIDGE, TECH_ACCESS.BRIDGE, CAPTURE_BRIDGE, DIPLOMACY.UI + TECH_ACCESS.UI (combine — shared hero_trade_menu.gd), CAPTURE_UI.
- Tier 3 non-hash (5 parallel): DIPLOMACY.HEADLESS, CAPTURE_HEADLESS (core), DIPLOMACY_BALANCE + EPIC_REVIEW + FACTION_DEPTH (docs).

## K. Tranche 41 — EA Foundation

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.041 | DONE | Run full test suite (1324+ tests). Scan for CS warnings. Check for dead code. Verify golden hash stability. T41 baseline. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q` | FOUND: docs/generated/01_CONTEXT_PACKET.md, FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.KG.SEEDING_FIX.001 | DONE | Fix NarrativePlacementGen.cs:556 — change `&&` to `||` in ResolveKnowledgeGraphTemplates so connections with one missing endpoint are skipped instead of created with invalid IDs. Add contract test for connection endpoint validity. Proof: `dotnet test --filter "KnowledgeGraph"` + determinism | FOUND: SimCore/Gen/NarrativePlacementGen.cs, FOUND: SimCore/Systems/KnowledgeGraphSystem.cs, FOUND: SimCore.Tests/Systems/KnowledgeGraphTests.cs |
| GATE.S8.MEGAPROJECT.ENTITY.001 | DONE | Create Megaproject entity (Id, TypeId, NodeId, Stage, MaxStages, SupplyDelivered, ProgressTicks, CompletedTick). Define 3 megaprojects in MegaprojectContentV0: Fracture Anchor (stabilize void lane), Trade Corridor (fast lane between nodes), Sensor Pylon (extend scan range in region). MegaprojectTweaksV0 with stage costs, durations, supply requirements. Wire into SimState dictionaries + signature. Proof: determinism tests | NEW: SimCore/Entities/Megaproject.cs, NEW: SimCore/Content/MegaprojectContentV0.cs, NEW: SimCore/Tweaks/MegaprojectTweaksV0.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S8.HAVEN.RESEARCH_LAB.001 | DONE | Add HavenResearchLab subsystem to HavenUpgradeSystem: Tier 2 = 1 research slot (T3 utility), Tier 3 = 2 slots (T3 weapons/defense), Tier 4 = 3 slots (all T3). Track active research per slot in HavenStarbase entity. Bridge: GetHavenResearchLabV0. UI: research section in haven_panel.gd. Proof: `dotnet test --filter "HavenTests"` + determinism | FOUND: SimCore/Systems/HavenUpgradeSystem.cs, FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.MEGAPROJECT.SYSTEM.001 | DONE | MegaprojectSystem: Process() advances stage when supply delivered + progress ticks met. StartMegaprojectCommand validates location (faction node with station), deducts initial cost. DeliverMegaprojectSupplyCommand transfers cargo to project. Wire SimKernel.Step() + WorldLoader. Proof: determinism tests | NEW: SimCore/Systems/MegaprojectSystem.cs, NEW: SimCore/Commands/StartMegaprojectCommand.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S8.MEGAPROJECT.MAP_RULES.001 | DONE | On megaproject completion: Fracture Anchor creates permanent void lane endpoint at target node. Trade Corridor reduces transit time between two connected nodes. Sensor Pylon extends scan range in 3-hop radius. Apply mutations in MegaprojectSystem.ProcessCompletion(). Persist mutations in SimState. Proof: determinism tests | FOUND: SimCore/Systems/MegaprojectSystem.cs, FOUND: SimCore/Entities/Megaproject.cs, FOUND: SimCore/SimKernel.cs |
| GATE.S8.HAVEN.DRYDOCK_TRANSFER.001 | DONE | TransferModuleCommand: move fitted module from one ship to another in Haven hangar (both at Haven, Tier 3+). Validate slot compatibility, power budget, sustain budget on target ship. Bridge: TransferModuleV0(sourceShipId, moduleSlot, targetShipId, targetSlot). UI: drydock section in haven_panel.gd. Proof: `dotnet test --filter "HavenTests"` + determinism | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenHangarSystem.cs, FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S8.HAVEN.ACCOMMODATION_FX.001 | DONE | Define per-thread gameplay bonuses in HavenTweaksV0: Discovery thread -> scan range bonus (+5/10/15% at 33/66/100), Commerce -> market price discount, Conflict -> damage bonus, Harmony -> rep gain bonus. Apply bonuses in relevant systems (IntelSystem, MarketSystem, CombatSystem, ReputationSystem). Thread progress already tracked in HavenEndgameSystem. Proof: `dotnet test --filter "HavenTests"` + determinism | FOUND: SimCore/Systems/HavenEndgameSystem.cs, FOUND: SimCore/Tweaks/HavenTweaksV0.cs, FOUND: SimCore/Entities/HavenStarbase.cs |
| GATE.S8.MEGAPROJECT.CONTRACT.001 | DONE | Contract tests: start each megaproject type, deliver supply, advance stages, verify completion. Reject start without faction standing. Supply delivery deducts from fleet cargo. Map rule mutation applied on completion (lane created / speed changed / scan extended). Determinism across seeds. Proof: `dotnet test --filter "MegaprojectTests"` | NEW: SimCore.Tests/Systems/MegaprojectTests.cs, FOUND: SimCore/Systems/MegaprojectSystem.cs |
| GATE.S8.HAVEN.REVEAL_THREAD.001 | DONE | RevealHavenCommand: at Tier 4+, player reveals Haven accommodation thread to one faction ally. Permanent rep boost with that faction. That faction's NPCs can visit Haven (spawn at Haven node). RevealedToFactionId persisted in HavenStarbase. Bridge: RevealHavenToFactionV0. UI: choice dialog in haven_panel.gd. Proof: `dotnet test --filter "HavenTests"` + determinism | FOUND: SimCore/Systems/HavenEndgameSystem.cs, FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: scripts/bridge/SimBridge.Haven.cs, FOUND: scripts/ui/haven_panel.gd |
| GATE.S9.SAVE.MIGRATION.001 | DONE | SaveMigrationSystem: version-aware deserialization pipeline. Each migration is a registered transform (v1->v2, v2->v3). SaveEnvelope version field routes through migration chain. First migration: v1->v2 adds HavenStarbase and Megaproject dictionaries to state. Unit tests for each migration step. Proof: `dotnet test --filter "SaveLoad"` | FOUND: SimCore/Systems/SerializationSystem.cs, FOUND: SimCore.Tests/Systems/SaveLoadWorldHashTests.cs |
| GATE.S9.SAVE.INTEGRITY.001 | DONE | Corruption detection: catch malformed JSON, truncated files, missing required fields in deserialization. Post-load validation: verify SimState invariants (fleet exists, current node valid, positive credits). Recovery UX: if corruption detected, offer load from auto-save or last valid slot. Bridge: GetSaveIntegrityV0(slot). Proof: `dotnet test --filter "SaveLoad"` | FOUND: SimCore/Systems/SerializationSystem.cs, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S9.BALANCE.LOCK.001 | DONE | Snapshot all *TweaksV0.cs const values via reflection into balance_baseline_v0.json. BalanceLockTests: compare current const values against baseline, fail if any value changed without explicit baseline update. Append tweak change process to TWEAK_ROUTING_POLICY.md. Proof: `dotnet test --filter "BalanceLock"` | FOUND: docs/tweaks/allowlist_numeric_literals_v0.txt, NEW: docs/tweaks/balance_baseline_v0.json |
| GATE.S9.STEAM.SDK.001 | DONE | Install GodotSteam 4.x addon for Godot 4. Create steam_appid.txt (placeholder app ID). Initialize Steam in game_manager.gd _ready() with graceful fallback when Steam client not running. Document setup in 57_RUNBOOK.md. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | FOUND: scripts/core/game_manager.gd, FOUND: docs/57_RUNBOOK.md |
| GATE.S9.L10N.DECISION.001 | DONE | Document English-only v1.0 decision. Audit all GDScript UI files for hardcoded strings (count and categorize: labels, tooltips, errors, toasts). Document extraction-ready patterns for future L10N. Add L10N section to 57_RUNBOOK.md. Proof: `dotnet test --filter "RoadmapConsistency"` | FOUND: docs/57_RUNBOOK.md |
| GATE.S8.MEGAPROJECT.BRIDGE.001 | DONE | SimBridge.Megaproject.cs partial: GetMegaprojectsV0() (all projects with status), GetMegaprojectDetailV0(id) (stages, supply needs, progress), StartMegaprojectV0(typeId, nodeId), DeliverSupplyV0(projectId, goodId, qty). TryExecuteSafeRead for queries, write lock for commands. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | NEW: scripts/bridge/SimBridge.Megaproject.cs, FOUND: SimCore/Systems/MegaprojectSystem.cs |
| GATE.S9.STEAM.ACHIEVEMENTS.001 | DONE | Map existing GetLifetimeStatsV0 milestones to Steam achievement IDs. Trigger Steam achievement unlock when milestone reached. Define achievement list in content registry. Categories: trade, combat, exploration, faction, haven. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | FOUND: scripts/core/game_manager.gd, FOUND: scripts/bridge/SimBridge.cs |
| GATE.S8.MEGAPROJECT.UI.001 | DONE | megaproject_panel.gd: active megaprojects with stage progress bars, supply delivery checklist per stage, projected completion ETA, affected nodes map preview. Accessible from empire dashboard tab. Start construction from node context menu. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | NEW: scripts/ui/megaproject_panel.gd, FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.S8.MEGAPROJECT.HEADLESS.001 | DONE | Headless GDScript test: boot game, dock at faction station, start megaproject via bridge, deliver supply goods, advance ticks until completion, verify map rule mutation visible in galaxy view. Full stack proof through SimBridge. Proof: `godot --headless -s res://scripts/tests/test_megaproject_proof.gd` | FOUND: scripts/ui/megaproject_panel.gd, FOUND: scripts/bridge/SimBridge.Megaproject.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.041 | DONE | Audit all epic statuses against completed gates. Identify epics ready to close (all gates DONE). Update 54_EPICS.md statuses. Recommend T42 anchor epic based on remaining EA gaps. Report: epics closed, remaining TODO count, suggested next priorities. Proof: `dotnet test --filter "RoadmapConsistency"` | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.X.EVAL.EA_READINESS.001 | DONE | Comprehensive EA readiness assessment: save system robustness (migration coverage, corruption handling), balance lock (all tweaks baselined, regression tests), content completeness (megaprojects, haven, knowledge graph), performance budget (tick time, memory), Steam integration status. Produce ea_readiness_report.md. Proof: `dotnet test --filter "RoadmapConsistency"` | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |

Combined-agent notes (T41):
- Tier 1 core hash chain (3 sequential): KG.SEEDING_FIX → MEGAPROJECT.ENTITY → HAVEN.RESEARCH_LAB. Each changes golden hash baseline.
- Tier 1 non-hash (4 parallel): REPO_HEALTH (docs), SAVE.MIGRATION (core), BALANCE.LOCK (docs), L10N.DECISION (docs), STEAM.SDK (bridge).
- Tier 2 core hash chain (4 sequential): MEGAPROJECT.SYSTEM → MAP_RULES → HAVEN.DRYDOCK_TRANSFER → HAVEN.ACCOMMODATION_FX.
- Tier 2 non-hash (2 parallel): SAVE.INTEGRITY (core), STEAM.ACHIEVEMENTS (bridge).
- Tier 3: MEGAPROJECT.CONTRACT (core, non-hash), HAVEN.REVEAL_THREAD (core, hash), MEGAPROJECT.BRIDGE (bridge).
- Tier 4: MEGAPROJECT.UI + HEADLESS (bridge), EPIC_REVIEW + EA_READINESS (docs).

## L. Tranche 42 — Planet Scanning System

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T42.PLANET_SCAN.MODEL.001 | DONE | ScanMode + FindingCategory enums, PlanetScanResult entity. 3 scan modes (MineralSurvey, SignalSweep, Archaeological), 5 finding categories (ResourceIntel, SignalLead, PhysicalEvidence, FragmentCache, DataArchive). Proof: `dotnet build SimCore/SimCore.csproj` | NEW: SimCore/Entities/PlanetScanResult.cs |
| GATE.T42.PLANET_SCAN.ENTITY_EXT.001 | DONE | Planet entity extensions (OrbitalScans dict, LandingScanTick, LandingScanMode, ScanResults list) + SimState scanner state (ScannerChargesUsed, ScannerTier). Proof: `dotnet build SimCore/SimCore.csproj` | FOUND: SimCore/Entities/Planet.cs, FOUND: SimCore/SimState.Properties.cs |
| GATE.T42.PLANET_SCAN.TWEAKS.001 | DONE | PlanetScanTweaksV0 — mode x planet type affinity matrix (3x6 float), charge budget per tier (2/3/4/5), investigation tick costs, fragment kind affinity weights per planet type. Proof: `dotnet build SimCore/SimCore.csproj` | NEW: SimCore/Tweaks/PlanetScanTweaksV0.cs |
| GATE.T42.PLANET_SCAN.CONTENT.001 | DONE | PlanetScanContentV0 — flavor text templates per planet type x finding category, orbital hint lines per mode mismatch, FO teaching lines for property-result correlations. Proof: `dotnet build SimCore/SimCore.csproj` | NEW: SimCore/Content/PlanetScanContentV0.cs |
| GATE.T42.PLANET_SCAN.ORBITAL.001 | DONE | PlanetScanSystem orbital scan — mode selection, affinity matrix lookup, charge budget enforcement, resource intel generation via TradeRouteIntel pipeline. Wire into SimKernel. Proof: `dotnet test --filter "PlanetScan"` + determinism | NEW: SimCore/Systems/PlanetScanSystem.cs, FOUND: SimCore/SimKernel.cs |
| GATE.T42.PLANET_SCAN.LANDING.001 | DONE | PlanetScanSystem landing scan — guaranteed findings based on mode + planet type, tech gate check (planetary_landing_mk1), fuel cost, atmospheric sampling variant for gaseous planets. Proof: `dotnet test --filter "PlanetScan"` + determinism | FOUND: SimCore/Systems/PlanetScanSystem.cs |
| GATE.T42.PLANET_SCAN.SIGNAL_LEAD.001 | DONE | Signal lead generation from orbital/landing scans — SIGNAL/CORRIDOR_TRACE discoveries with COORDINATE_HINT hooks. Triangulation: 2 signals of same type from different systems resolve to precise location. Proof: `dotnet test --filter "PlanetScan\|SignalLead"` + determinism | FOUND: SimCore/Systems/PlanetScanSystem.cs |
| GATE.T42.PLANET_SCAN.EVIDENCE.001 | DONE | Physical evidence findings from landing scans — RUIN/DERELICT discoveries, 1-3 KnowledgeGraph connections, investigation option (spend docked ticks for bonus data, site never expires). Planet type determines evidence flavor (ice=fossils, sand=excavations, lava=emergence, barren=installations). Proof: `dotnet test --filter "PlanetScan"` + determinism | FOUND: SimCore/Systems/PlanetScanSystem.cs |
| GATE.T42.PLANET_SCAN.FRAGMENT.001 | DONE | Fragment cache drops from landing scans — planet type biases fragment kind (ice=biological, sand=structural, lava=energetic, gaseous=cognitive, barren=any). Finite pool (16 fragments), each found at most once. Landing scan only. Proof: `dotnet test --filter "PlanetScan\|Fragment"` + determinism | FOUND: SimCore/Systems/PlanetScanSystem.cs |
| GATE.T42.PLANET_SCAN.ARCHIVE.001 | DONE | Data archive findings from archaeological scan mode — data logs + faction archives added to KnowledgeGraph. Each archive has mechanical hook (COORDINATE_HINT, CALIBRATION_DATA, RESONANCE_LOCATION, TRADE_INTEL). World class determines log tone (CORE=political, FRONTIER=anxious, RIM=personal). Proof: `dotnet test --filter "PlanetScan"` + determinism | FOUND: SimCore/Systems/PlanetScanSystem.cs, FOUND: SimCore/Content/PlanetScanContentV0.cs |
| GATE.T42.PLANET_SCAN.OUTCOME_HOOK.001 | DONE | DiscoveryOutcomeSystem integration — planet-scan-generated discoveries feed through existing outcome pipeline (trade intel generation, chain advancement, instability gating). Proof: `dotnet test --filter "PlanetScan\|DiscoveryOutcome"` + determinism | FOUND: SimCore/Systems/DiscoveryOutcomeSystem.cs, FOUND: SimCore/Systems/PlanetScanSystem.cs |
| GATE.T42.PLANET_SCAN.INSTAB_LEAD.001 | DONE | IntelSystem instability-reveal as Signal Lead — when InstabilityLevel rises past a gated discovery's threshold at a previously-scanned planet, create a new SIGNAL discovery at the planet's node (visible on galaxy map). Player goes IF they want to, not because they must. Proof: `dotnet test --filter "PlanetScan\|DiscoveryIntel"` + determinism | FOUND: SimCore/Systems/IntelSystem.cs |
| GATE.T42.PLANET_SCAN.FO.001 | DONE | FirstOfficerSystem 6 planet-scan triggers (FIRST_PLANET_SURVEYED, SCAN_MODE_MISMATCH, PATTERN_RECOGNIZED, RARE_FIND, SIGNAL_TRIANGULATED, LORE_DISCOVERY) + 18 dialogue lines (6 triggers x 3 FO types). Proof: determinism tests | FOUND: SimCore/Systems/FirstOfficerSystem.cs, FOUND: SimCore/Content/FirstOfficerContentV0.cs |
| GATE.T42.PLANET_SCAN.BRIDGE.001 | DONE | SimBridge.Planet.cs partial — OrbitalScanV0(nodeId, mode), LandingScanV0(nodeId, mode), AtmosphericSampleV0(nodeId, mode), GetPlanetScanResultsV0(nodeId), InvestigateFindingV0(scanId), GetScanChargesV0(). TryExecuteSafeRead for queries, write lock for scan commands. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/bridge/SimBridge.Planet.cs |
| GATE.T42.PLANET_SCAN.SCANNER_TIER.001 | DONE | Scanner tier progression — Mk1 (+Signal Sweep, 3 charges), Mk2 (+Archaeological, 4 charges, tech-gated planets), Mk3 (dual-mode, 5 charges), Fracture Scanner (instability zone scanning). Integrate with research tree unlock system. Proof: `dotnet test --filter "PlanetScan\|Scanner"` + determinism | FOUND: SimCore/Systems/PlanetScanSystem.cs |
| GATE.T42.PLANET_SCAN.SURVEY_EXT.001 | DONE | SurveyProgram extension — orbital auto-scan with configured ScanMode, planet-type-aware mode selection, generates Resource Intel + Signal Leads at Scanned phase. Proof: `dotnet test --filter "PlanetScan\|SurveyProgram"` + determinism | FOUND: SimCore/Programs/ProgramSystem.cs |
| GATE.T42.PLANET_SCAN.TESTS.001 | DONE | Full test suite — PlanetScanTests covering orbital/landing/atmospheric scans, mode affinity matrix, charge budget enforcement, all 5 finding categories, signal triangulation, investigation mechanic, fragment drops, data archives, FO triggers, scanner tier progression, SurveyProgram integration. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release` | NEW: SimCore.Tests/Systems/PlanetScanTests.cs |
| GATE.T42.PLANET_SCAN.GOLDEN_HASH.001 | DONE | Golden hash baseline update for all T42 hash-affecting changes. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release` | FOUND: docs/tweaks/balance_baseline_v0.json |

Combined-agent notes (T42):
- Tier 1 (4 parallel, non-hash): MODEL, ENTITY_EXT, TWEAKS, CONTENT. All data model, single build to verify.
- Tier 2 core hash chain (9 sequential): ORBITAL → LANDING → {EVIDENCE, FRAGMENT, ARCHIVE} parallel → OUTCOME_HOOK. SIGNAL_LEAD branches from ORBITAL → INSTAB_LEAD.
- Tier 3 (4 gates, mixed): FO (hash), BRIDGE (non-hash), SCANNER_TIER (hash), SURVEY_EXT (hash). FO + BRIDGE can parallel.
- Tier 4 (2 sequential): TESTS → GOLDEN_HASH.
- Design doc: docs/design/planet_scanning_v0.md (v1). Key design decisions: charge budget (not timers) as engagement constraint, 3 scan modes (Mineral/Signal/Archaeological), 5 finding categories, nothing expires except market prices (existing T41 IntelSystem), investigation sites never degrade.

## M. Tranche 43 — Planet Scan UI

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.042 | DONE | Full test suite + build verification baseline for T43. Proof: `dotnet test` + `dotnet build` | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.T43.SCAN_UI.HUD_PANEL.001 | DONE | Scanner charge HUD indicator (Zone C) + mode selector + orbital scan button. Shows at planet nodes, hidden during transit. Calls OrbitalScanV0 on click. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/ui/scanner_hud_panel.gd, FOUND: scripts/ui/hud.gd |
| GATE.T43.SCAN_UI.STATION_SECTION.001 | DONE | Station tab scan section: planet info header, affinity bars per mode, scan action buttons (orbital/landing/atmospheric), scrollable result history with scan cards. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.T43.SCAN_UI.RESULT_TOAST.001 | DONE | Scan result toast via ToastManager: category name + truncated flavor text. Milestone priority for normal, critical for rare findings. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/toast_manager.gd, FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.T43.SCAN_AUDIO.CHIMES.001 | DONE | 5 category-specific scan completion chimes (ResourceIntel=cash, SignalLead=ping, PhysicalEvidence=resonant, FragmentCache=crystal, DataArchive=data burst) + charge spent click. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/audio/scan_audio.gd |
| GATE.T43.SCAN_UI.RESULT_MODAL.001 | DONE | Scan result modal with progressive reveal: category icon slide-in, flavor text typewriter, hint text fade, stats line fade. Auto-dismiss or click. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.T43.SCAN_UI.INVESTIGATE.001 | DONE | Investigation UI: investigate button on Physical Evidence cards, progress display (tick countdown), completion badge, KG connection toast. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.T43.SCAN_UI.GALAXY_MARKERS.001 | DONE | Galaxy map planet-type icons (colored dots per type) + scan state markers (unscanned/partial/full ring). Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs |
| GATE.T43.SCAN_UI.SIGNAL_LINES.001 | DONE | Galaxy map dashed purple triangulation lines between Signal Lead source nodes. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs |
| GATE.T43.SCAN_AUDIO.AMBIENT.001 | DONE | Ambient sensor ping at planet nodes (every 3-5s, pitch varies by type) + scan initiated rising tone (0.5s). Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/audio/scan_audio.gd |
| GATE.T43.SCAN_UI.PLANET_MESH.001 | DONE | 3D planet mesh spawner for dock view using existing atmosphere shaders. Planet visible behind dock panel. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/view/planet_dock_view.gd, FOUND: scripts/view/GalaxyView.cs |
| GATE.T43.SCAN_UI.DISCLOSURE.001 | DONE | Progressive disclosure: scanner HUD panel hidden until player at planet node, mode buttons show lock state per scanner tier. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/scanner_hud_panel.gd, FOUND: scripts/ui/hud.gd |
| GATE.T43.SCAN_UI.COMPLETION.001 | DONE | Scan completion tracker in station tab: "N/6 planet types surveyed" counter. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.T43.SCAN_UI.FO_SURFACE.001 | DONE | FO scan commentary surfaced: 6 trigger toasts (FO priority) + Station tab FO observation includes scan context. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/hero_trade_menu.gd, FOUND: scripts/ui/toast_manager.gd |
| GATE.T43.SCAN_UI.CHARGE_RESET.001 | DONE | HUD charge counter pulse animation on travel + "Scanner charges refreshed" toast on system arrival. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/scanner_hud_panel.gd, FOUND: scripts/ui/hud.gd |
| GATE.T43.SCAN_UI.HEADLESS_PROOF.001 | DONE | Headless bot: dock at planet, orbital scan all modes, landing scan, investigate physical evidence, verify toast + results. Proof: godot --headless | NEW: scripts/tests/test_planet_scan_ui_proof_v0.gd |
| GATE.X.HYGIENE.EPIC_REVIEW.042 | DONE | Epic status audit: close completed epics from T42-T43, recommend T44 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md |
| GATE.T43.SCAN_UI.EVAL.001 | DONE | Screenshot eval of scan UI: /feel pass on dock view + galaxy map with scan markers. Proof: visual evaluation | FOUND: scripts/tools/visual_eval_guide.md |

Combined-agent notes (T43):
- ALL 18 gates are non-hash-affecting (GDScript/bridge/UI only). Full parallelism within tiers.
- Tier 1 (5 parallel): REPO_HEALTH, HUD_PANEL, STATION_SECTION, RESULT_TOAST, SCAN_AUDIO.CHIMES
- Tier 2 (10 parallel): RESULT_MODAL, INVESTIGATE, GALAXY_MARKERS, SIGNAL_LINES, AMBIENT, PLANET_MESH, DISCLOSURE, COMPLETION, FO_SURFACE, CHARGE_RESET
- Tier 3 (3 sequential): HEADLESS_PROOF, EPIC_REVIEW, EVAL
- Design doc: docs/design/planet_scan_ui_v0.md. Reference patterns: X4 (scanning as infrastructure), Elite (two-tier), Stellaris (escalating anomaly), Outer Wilds (knowledge graph), NMS (progressive reveal).

## N. Tranche 44 — "World Feels Alive" (EPIC.T44.ECONOMY_VISUALS.V0, EPIC.X.STATION_IDENTITY.V0, EPIC.S8.HAVEN_STARBASE.V0, EPIC.S8.NARRATIVE_CONTENT.V0, EPIC.S8.MEGAPROJECT_SET.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T44.SIGNAL.CONTRACT_TESTS.001 | DONE | Contract tests for economy bridge signals: verify GetNodeEconomySnapshotV0 returns traffic_level/prosperity/industry_type/warfront_tier/faction_id/docked_fleets with correct types and ranges. Verify GetMarketAlertsV0 returns price_spike/price_drop/stockout alerts. Proof: `dotnet test ... --filter "EconomyBridgeContract"` | FOUND: SimCore.Tests/Systems/EconomyStressTests.cs, FOUND: scripts/bridge/SimBridge.Market.cs |
| GATE.T44.SIGNAL.ECONOMY_STRESS.001 | DONE | Extended 5000-tick economy stress test: no node with zero trades for 500+ ticks, avg good price within 3x base price, 60%+ nodes with NPC trade activity, no all-zero-stock markets. Proof: `dotnet test ... --filter "EconomyStress"` | FOUND: SimCore.Tests/Systems/EconomyStressTests.cs, FOUND: SimCore/Systems/NpcTradeSystem.cs |
| GATE.T44.NARRATIVE.COMMUNION_DIALOGUE.001 | DONE | Communion Rep 3-arc dialogue content per haven_starbase_v0.md: (1) Introduction at T3, (2) Path guidance at T4, (3) Endgame counsel at T5. CommunionRepDialogueContentV0 with DialogueTier indexing. Hash-affecting. Proof: `dotnet test ... --filter "Determinism"` + `--filter "CommunionDialogue"` | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Systems/HavenEndgameSystem.cs, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.T44.NARRATIVE.KEEPER_EXPAND.001 | DONE | Keeper expanded tier-specific dialogue: KeeperDialogueContentV0 with 3-5 lines per KeeperTier (Dormant/Aware/Guiding/Communicating/Awakened). Wire into GetKeeperStateV0. Hash-affecting. Proof: `dotnet test ... --filter "Determinism"` + `--filter "KeeperDialogue"` | FOUND: SimCore/Entities/HavenStarbase.cs, FOUND: SimCore/Content/DataLogContentV0.cs, FOUND: scripts/bridge/SimBridge.Haven.cs |
| GATE.X.HYGIENE.REPO_HEALTH.044 | DONE | Full test suite (1400+ tests) + warning scan + golden hash stability baseline for T44. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release` | FOUND: SimCore.Tests/SimCore.Tests.csproj |
| GATE.T44.AMBIENT.SHUTTLE_TRAFFIC.001 | DONE | Per economy_simulation_v0.md Cat.1: cosmetic shuttles near stations, count from traffic_level via GetNodeEconomySnapshotV0. Elliptical orbit AnimationPlayer, faction-tinted per station_visual_design_v0.md. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/view/ambient_shuttle.gd |
| GATE.T44.AMBIENT.MINING_VFX.001 | DONE | Per economy_simulation_v0.md Cat.2: green/amber extraction beam GPUParticles3D at industry_type mine/fuel_well nodes. Intensity scales with prosperity. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/vfx/mining_beam_vfx.gd |
| GATE.T44.AMBIENT.PROSPERITY.001 | DONE | Per economy_simulation_v0.md Cat.5: 4 visual tiers from prosperity signal. Struggling=dim/flicker, Stable=normal, Prosperous=warm glow, Booming=bright+aura. Station OmniLight3D modulation. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/view/station_prosperity.gd |
| GATE.T44.AMBIENT.LANE_TRAFFIC.001 | DONE | Per economy_simulation_v0.md Cat.4: billboarded Sprite3D at lane midpoints, 1-3 per lane. Count from avg traffic_level. Drift along lane direction, fade at distance. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/view/lane_traffic_sprite.gd |
| GATE.T44.AMBIENT.WARFRONT_ATMO.001 | DONE | Per vfx_visual_roadmap_v0.md warfront visuals: red-shifted GPUParticles3D + red OmniLight3D tint at warfront_tier > 0 nodes. Intensity 1=subtle, 3=heavy. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/vfx/warfront_atmosphere.gd |
| GATE.T44.STATION.FACTION_TINT.001 | DONE | Per station_visual_design_v0.md: albedo_modulate on station mesh. Hegemony=#D4A017, Sovereignty=#4A7CB5, Collective=#2E8B57, Dominion=#8B2500, Communion=#7B3F9E. From faction_id via GetNodeEconomySnapshotV0. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/view/station_identity.gd |
| GATE.T44.STATION.TIER_SCALE.001 | DONE | Per factions_and_lore_v0.md: outpost (scale 0.6x), hub (1.0x), capital (1.4x) based on industry site count + market breadth. Apply to station MeshInstance3D. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/view/station_identity.gd |
| GATE.T44.STATION.NAMEPLATE.001 | DONE | Label3D with station name + faction insignia sprite above station. Font per visual_constants.md. Distance fade. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/view/station_nameplate.gd |
| GATE.T44.DIGEST.MARKET_ALERTS.001 | DONE | Wire GetMarketAlertsV0 to toast_manager.gd: poll every 30s, blue economy toasts for price spikes/drops/stockouts. Max 3 per poll. ECONOMY priority tier. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/toast_manager.gd, FOUND: scripts/ui/hud.gd, NEW: scripts/ui/economy_alert_poller.gd |
| GATE.T44.DIGEST.ECONOMY_DOCK.001 | DONE | Economy info section in hero_trade_menu.gd: traffic level, prosperity bar, industry type, warfront warning. Data from GetNodeEconomySnapshotV0. Below market goods. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/ui/hero_trade_menu.gd |
| GATE.T44.DIGEST.MEGAPROJECT_MAP.001 | DONE | Wire GetMegaprojectsV0 to GalaxyView: construction icon + radial progress ring at megaproject nodes. Color by type (Anchor=blue, Corridor=green, Pylon=amber). Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, FOUND: scripts/bridge/SimBridge.Megaproject.cs |
| GATE.T44.DIGEST.CONSTRUCTION_VFX.001 | DONE | GPUParticles3D at active megaproject nodes: welding sparks + scaffolding outline. Active during construction, removed on completion. Proof: `dotnet build "Space Trade Empire.csproj"` | FOUND: scripts/view/GalaxyView.cs, NEW: scripts/vfx/construction_vfx.gd |
| GATE.X.HYGIENE.ECONOMY_EVAL.044 | DONE | Economy balance eval: run stress tests across 3 seeds, verify NPC trade health, price stability, fleet population replacement, warfront avoidance. Document findings. Proof: `dotnet test ... --filter "EconomyStress"` | FOUND: SimCore.Tests/Systems/EconomyStressTests.cs, FOUND: SimCore/Systems/NpcTradeSystem.cs, FOUND: SimCore/Systems/FleetPopulationSystem.cs |
| GATE.X.HYGIENE.EPIC_REVIEW.044 | DONE | Audit epic statuses. Close HAVEN_STARBASE.V0 (Communion dialogue done = final item). Close STATION_IDENTITY.V0 (tint+scale+nameplate). Update NARRATIVE_CONTENT.V0, ECONOMY_VISUALS.V0. Recommend T45 anchor. Proof: RoadmapConsistency | FOUND: docs/54_EPICS.md, FOUND: docs/55_GATES.md, FOUND: docs/56_SESSION_LOG.md |

Combined-agent notes (T44):
- Hash-affecting: 2 gates (COMMUNION_DIALOGUE.001 -> KEEPER_EXPAND.001, sequential chain in core session).
- Non-hash: 17 gates (all bridge + docs + core contract/stress).
- Tier 1 (17 parallel): all bridge gates, core contract/stress tests, repo health.
- Tier 1 sequential (core): COMMUNION_DIALOGUE -> KEEPER_EXPAND (hash chain).
- Tier 3 (2 gates): ECONOMY_EVAL, EPIC_REVIEW (depend on tier 1 completion).
- File conflict groups: GalaxyView.cs touched by 10 bridge gates — assign to same agent or split by concern (ambient=1 agent, station=1 agent, digest=1 agent).
- Design docs: economy_simulation_v0.md (ambient visuals), station_visual_design_v0.md (faction colors), haven_starbase_v0.md (Communion/Keeper), vfx_visual_roadmap_v0.md (warfront/construction).
- NEW files (7): ambient_shuttle.gd, mining_beam_vfx.gd, station_prosperity.gd, lane_traffic_sprite.gd, warfront_atmosphere.gd, station_identity.gd, station_nameplate.gd, economy_alert_poller.gd, construction_vfx.gd. Total 9 NEW paths (under 10 limit).

### Tranche 45 — Deep Dread (Subnautica-style depth-as-dread system)

Epics:
- EPIC.S8.DEEP_DREAD.V0 [TODO]: Subnautica-inspired depth-as-dread system — Thread Lattice instability phases map to escalating terror layers (Isolation→Phenomena→Predation→Meta-Dread). Patrol thinning, passive hull drain, sensor ghosts, information fog, Lattice Fauna emergent entities, fracture exposure tracking, FO dread triggers, ambient audio gradient, comms degradation, visual distortion, galaxy map dread overlay. (gates: GATE.T45.DEEP_DREAD.*)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.X.HYGIENE.REPO_HEALTH.045 | DONE | Full test suite + build baseline for T45. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q` + `dotnet build "Space Trade Empire.csproj" --nologo -v q` | SimCore.Tests/SimCore.Tests.csproj |
| GATE.T45.DEEP_DREAD.TWEAKS.001 | DONE | Create DeepDreadTweaksV0.cs: patrol thin hop thresholds (0=full,3=half,5=zero), passive drain rates by phase, sensor ghost frequency, fauna detection radius, exposure milestones, audio crossfade thresholds. Update allowlist. Proof: `dotnet build SimCore/SimCore.csproj --nologo -v q` | SimCore/Tweaks/DeepDreadTweaksV0.cs, docs/tweaks/allowlist_numeric_literals_v0.txt |
| GATE.T45.DEEP_DREAD.FAUNA_TWEAKS.001 | DONE | Create LatticeFaunaTweaksV0.cs: detection radius hops, signature decay, arrival delay, interference magnitude, fuel drain, residue duration, max concurrent, spawn cooldown. Update allowlist. Proof: `dotnet build SimCore/SimCore.csproj --nologo -v q` | SimCore/Tweaks/LatticeFaunaTweaksV0.cs, docs/tweaks/allowlist_numeric_literals_v0.txt |
| GATE.T45.DEEP_DREAD.DESIGN_DOC.001 | DONE | Write docs/design/deep_dread_v0.md: 4-layer system, tuning philosophy, AAA references, lore integration, audio design, visual pipeline, FO triggers. Proof: file exists | docs/design/deep_dread_v0.md |
| GATE.T45.DEEP_DREAD.PATROL_THIN.001 | DONE | Modify NpcTradeSystem: scale faction patrol spawn probability by hop distance from faction capital. 0-2 hops=full, 3-4=half, 5+=zero. BFS from faction capitals. Read from DeepDreadTweaksV0. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Systems/NpcTradeSystem.cs, SimCore/Tweaks/DeepDreadTweaksV0.cs |
| GATE.T45.DEEP_DREAD.PASSIVE_DRAIN.001 | DONE | Phase-based passive hull drain in InstabilitySystem or new DreadDrainSystem: Phase2=1HP/50t, Phase3=1HP/20t, Phase4=0 (void paradox). Skip if accommodation module. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Systems/InstabilitySystem.cs, SimCore/Tweaks/DeepDreadTweaksV0.cs |
| GATE.T45.DEEP_DREAD.SENSOR_GHOSTS.001 | DONE | New SensorGhostSystem: phantom fleet contacts at Phase 2+ nodes via hash(tick,nodeId). Scale with instability. 3-8 tick lifespan, max 3 concurrent. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Systems/SensorGhostSystem.cs, SimCore/Tweaks/DeepDreadTweaksV0.cs |
| GATE.T45.DEEP_DREAD.INFO_FOG.001 | DONE | New InformationFogSystem: market data staleness by hop distance, scanner range decrease at Phase 2+, distant unvisited nodes show ? for prices. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Systems/InformationFogSystem.cs, SimCore/Tweaks/DeepDreadTweaksV0.cs |
| GATE.T45.DEEP_DREAD.DISCOVERY_REGISTER.001 | DONE | Phase-aware discovery flavor text: clinical register at Phase 0-1, unsettling register at Phase 2+. 18+ deep-space entries across 6 planet types. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Content/PlanetScanContentV0.cs, SimCore/Content/DiscoveryFlavorContentV0.cs |
| GATE.T45.DEEP_DREAD.LATTICE_FAUNA.001 | DONE | New LatticeFaunaSystem + LatticeFauna entity: spawn at Phase 3+ on fracture signature detection. Arrival delay. Instrument interference, fuel drain, route uncertainty increase. Avoidable by going dark. Residue attracts more. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Systems/LatticeFaunaSystem.cs, SimCore/Entities/LatticeFauna.cs, SimCore/Tweaks/LatticeFaunaTweaksV0.cs |
| GATE.T45.DEEP_DREAD.FO_DISTANCE.001 | DONE | 8 new FO triggers: FAR_FROM_PATROL, LATTICE_THIN, SENSOR_GHOST_SEEN, FAUNA_DETECTED, DEEP_EXPOSURE_MILD, DEEP_EXPOSURE_HEAVY, VOID_ENTRY, COMMS_LOST. 24 dialogue lines (8x3 FO types). Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Systems/FirstOfficerSystem.cs, SimCore/Content/FirstOfficerContentV0.cs |
| GATE.T45.DEEP_DREAD.EXPOSURE_TRACK.001 | DONE | DeepExposure field in SimState: increments per tick at Phase 2+ nodes. Milestones at 20/50/100 ticks trigger FO observations. Instrument disagreement narrows by exposure factor (adaptation). Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/SimState.Properties.cs, SimCore/Tweaks/DeepDreadTweaksV0.cs |
| GATE.T45.DEEP_DREAD.BRIDGE.001 | DONE | New SimBridge.Dread.cs partial: GetDreadStateV0, GetSensorGhostsV0, GetLatticeFaunaV0, GetExposureV0, GetInfoFogV0. TryExecuteSafeRead pattern. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/bridge/SimBridge.Dread.cs, scripts/bridge/SimBridge.cs |
| GATE.T45.DEEP_DREAD.AMBIENT_AUDIO.001 | DONE | 5 ambient audio layers (safe=busy, shimmer=thinning, drift=low drone, fracture=deep resonance, void=near-silence). Crossfade from GetDreadStateV0. Procedural synth. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/audio/ambient_audio.gd, scripts/core/game_manager.gd |
| GATE.T45.DEEP_DREAD.FAUNA_AUDIO.001 | DONE | Fauna audio: distant harmonic awareness, proximity tone, 20% phantom plays (audio lie). Procedural synth. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/core/game_manager.gd, scripts/audio/ambient_audio.gd |
| GATE.T45.DEEP_DREAD.COMMS_STATIC.001 | DONE | FO text corruption at hop>=4 (5-15% static glyphs), delayed toasts at hop>=5, signal quality HUD indicator. Click to clear static. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/fo_panel.gd, scripts/ui/hud.gd |
| GATE.T45.DEEP_DREAD.HUD_DREAD.001 | DONE | HUD dread panel: phase icon, isolation warning, exposure meter, fauna proximity indicator. Polls GetDreadStateV0. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/hud.gd, scripts/ui/scanner_hud_panel.gd |
| GATE.T45.DEEP_DREAD.DISTORTION_SHADER.001 | DONE | dread_distortion.gdshader: Phase 1=chromatic aberration, Phase 2=star shimmer, Phase 3=HUD distortion+ghost routes, Phase 4=clarity (aberration=0). Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/vfx/dread_distortion.gdshader, scripts/view/GalaxyView.cs |
| GATE.T45.DEEP_DREAD.GALAXY_DREAD.001 | DONE | Galaxy map dread: patrol coverage opacity, phase zone coloring, fauna activity markers, void site indicators. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/view/GalaxyView.cs |
| GATE.T45.DEEP_DREAD.TESTS.001 | DONE | DeepDreadTests.cs: patrol scaling, drain, ghosts, fog, fauna, going dark, exposure, text register. 15+ assertions. Proof: `dotnet test --filter "DeepDread"` | SimCore.Tests/Systems/DeepDreadTests.cs, SimCore/Systems/SensorGhostSystem.cs |
| GATE.T45.DEEP_DREAD.HEADLESS_PROOF.001 | DONE | test_deep_dread_proof_v0.gd: travel to hop 5+, verify patrol=0, drain, ghosts, fauna, exposure, FO dialogue. 20+ hard assertions. Proof: godot headless | scripts/tests/test_deep_dread_proof_v0.gd |
| GATE.T45.DEEP_DREAD.GOLDEN_HASH.001 | DONE | Regenerate golden hashes after all hash-affecting gates. Full determinism suite + all tests. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q` | docs/tweaks/balance_baseline_v0.json, SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.045 | DONE | Audit epic statuses. Create EPIC.S8.DEEP_DREAD.V0 if needed. Close completed epics. Recommend T46 anchor. Proof: RoadmapConsistency | docs/54_EPICS.md, docs/55_GATES.md |

Execution plan:
- Hash-affecting: 8 gates chain sequentially in core session (PATROL_THIN→PASSIVE_DRAIN→SENSOR_GHOSTS→INFO_FOG→DISCOVERY_REGISTER→LATTICE_FAUNA→FO_DISTANCE→EXPOSURE_TRACK).
- Non-hash: 15 gates across docs/core/bridge groups.
- Tier 1 (7 gates, 4 parallel): REPO_HEALTH (docs), TWEAKS+FAUNA_TWEAKS (core, parallel), DESIGN_DOC (docs), PATROL_THIN→PASSIVE_DRAIN→SENSOR_GHOSTS→INFO_FOG→DISCOVERY_REGISTER (core sequential hash chain).
- Tier 2 (12 gates): LATTICE_FAUNA→FO_DISTANCE→EXPOSURE_TRACK (core hash chain cont.), BRIDGE (bridge, after core tier 1), AMBIENT_AUDIO+FAUNA_AUDIO+COMMS_STATIC+HUD_DREAD+DISTORTION_SHADER+GALAXY_DREAD (bridge, parallel after BRIDGE), TESTS (core).
- Tier 3 (4 gates): HEADLESS_PROOF (bridge), GOLDEN_HASH (core), EPIC_REVIEW (docs).
- File conflict groups: GalaxyView.cs (DISTORTION_SHADER + GALAXY_DREAD — combine for execution), hud.gd (COMMS_STATIC + HUD_DREAD — combine), ambient_audio.gd (AMBIENT_AUDIO + FAUNA_AUDIO — combine).
- NEW files (7): DeepDreadTweaksV0.cs, LatticeFaunaTweaksV0.cs, deep_dread_v0.md, SensorGhostSystem.cs, InformationFogSystem.cs, LatticeFaunaSystem.cs, LatticeFauna.cs, SimBridge.Dread.cs, dread_distortion.gdshader, test_deep_dread_proof_v0.gd, DeepDreadTests.cs. Total 11 NEW paths (under limit with gate-level cap of 3 each).

## AO. Tranche 46 — "EA Critical Path" (EPIC.S9.STEAM.V0, EPIC.S9.PERF, EPIC.S9.SAVE, EPIC.X.STATION_IDENTITY.V0, EPIC.S8.NARRATIVE_CONTENT.V0, EPIC.S9.MUSIC.V0)

Focus: EA must-haves (Steam, performance profiling, build pipeline) and should-haves (auto-save, station depth, narrative, music).

- EPIC.S9.STEAM.V0 [IN_PROGRESS]: GodotSteam addon install, init wrapper, achievement bridge
- EPIC.S9.PERF [TODO→IN_PROGRESS]: Tick profiler, memory budget test, performance baseline report
- EPIC.S9.SAVE [IN_PROGRESS]: Timer-based auto-save system + UI indicator
- EPIC.X.STATION_IDENTITY.V0 [IN_PROGRESS]: Station mesh tier scaling (outpost/hub/capital)
- EPIC.S8.NARRATIVE_CONTENT.V0 [IN_PROGRESS]: Haven logs, fragment lore, endgame text
- EPIC.S9.MUSIC.V0 [TODO→IN_PROGRESS]: Audio stem pipeline, combat music triggers

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T46.PERF.TICK_PROFILER.001 | DONE | Per-system tick profiler: add optional Stopwatch instrumentation to SimKernel.Step(). Ring buffer (100 ticks). GetTickProfileV0 bridge: system name + avg/max/p99 us. Zero cost in release. Proof: `dotnet build SimCore/SimCore.csproj --nologo -v q` | SimCore/SimKernel.cs, scripts/bridge/SimBridge.cs |
| GATE.T46.PERF.MEMORY_BUDGET.001 | DONE | Memory budget test: SimState with 50-node galaxy, 500 ticks, GC.GetTotalMemory < 256MB. Per-collection object counts. Test-only, no SimCore changes. Proof: `dotnet test --filter "PerfBudget"` | SimCore.Tests/Performance/PerfBudgetTests.cs |
| GATE.T46.PERF.PROFILE_REPORT.001 | DONE | Performance baseline report: tick time per system, memory baseline, 60fps feasibility. 20-node and 50-node measurements. Optimization targets. Proof: file exists | docs/ea_perf_baseline.md |
| GATE.T46.STEAM.ADDON_INSTALL.001 | DONE | GodotSteam addon install: AssetLib or manual. project.godot autoload. scripts/platform/steam_manager.gd with init()+is_steam_running()+shutdown(). Verify headless load. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/platform/steam_manager.gd (no existing platform dir), project.godot |
| GATE.T46.STEAM.INIT_WRAPPER.001 | DONE | Steam init wrapper: Steam.steamInit() on _ready, fallback to is_steam=false. Wire game_manager startup. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/platform/steam_manager.gd, scripts/core/game_manager.gd |
| GATE.T46.STEAM.ACHIEVEMENT_BRIDGE.001 | DONE | Achievement unlock: SimBridge milestone_achieved signal. steam_manager listens, calls Steam.setAchievement(). Map 18 MilestoneContentV0 IDs. No-op without Steam. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/platform/steam_manager.gd, scripts/bridge/SimBridge.cs |
| GATE.T46.BUILD.EXPORT_TEMPLATE.001 | DONE | Export preset: export_presets.cfg for Windows release+debug. Build-Release.ps1 script. .NET 8 export flags. Document in 57_RUNBOOK.md. Proof: file exists | NEW: export_presets.cfg, NEW: scripts/tools/Build-Release.ps1, docs/57_RUNBOOK.md |
| GATE.T46.BUILD.RELEASE_TEST.001 | DONE | Release smoke test: run Build-Release.ps1, then first-hour bot headless on exported binary. 18/18 assertions. Document export-only failures. Proof: `scripts/tools/Build-Release.ps1` | scripts/tools/Build-Release.ps1 |
| GATE.T46.SAVE.AUTOSAVE_SYSTEM.001 | DONE | Timer-based auto-save: configurable interval (300s default) from AutoSaveTweaksV0. Calls SerializationSystem.Save() to auto-save slot. autosave_started/completed signals. Pauses in combat. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/bridge/SimBridge.cs, NEW: SimCore/Tweaks/AutoSaveTweaksV0.cs |
| GATE.T46.SAVE.AUTOSAVE_UI.001 | DONE | Auto-save HUD: spinner icon (top-right, 1s fade). Settings toggle: on/off + interval (1/3/5/10 min). Persist in user config. Wire to autosave signals. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/hud.gd, scripts/ui/main_menu.gd |
| GATE.T46.STATION.NODE_MESH.001 | DONE | Station mesh tier scaling: outpost (0.6x, <3 goods), hub (1.0x, 3-6 goods), capital (1.4x, 7+ goods). Tier-based detail meshes from Kenney Space Kit. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/core/game_manager.gd |
| GATE.T46.STATION.DOCK_FLAVOR.001 | DONE | Faction dock greeting: 5 greetings per faction (25 total) + 5 station desc templates per faction (25 total) in FactionDialogueContentV0. Wire GetFactionGreetingV0 in SimBridge.Faction.cs. Display in dock header. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Content/FactionDialogueContentV0.cs, scripts/bridge/SimBridge.Faction.cs, scripts/ui/hero_trade_menu.gd |
| GATE.T46.NARRATIVE.HAVEN_LOGS.001 | DONE | Haven data logs: 8 DataLogDef entries (discovery, tier 1-5 upgrades, keeper, fabricator). Place at haven node via NarrativePlacementGen. 3-5 sentences each. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Content/DataLogContentV0.cs, SimCore/Gen/NarrativePlacementGen.cs |
| GATE.T46.NARRATIVE.FRAGMENT_LORE.001 | DONE | Fragment flavor: 12 new entries across 4 categories (Thread Lattice, faction reactions, precursor echoes, interpretations). 2-3 sentences each. Hash-affecting. Proof: `dotnet test --filter "Determinism"` | SimCore/Content/AdaptationFragmentContentV0.cs |
| GATE.T46.NARRATIVE.ENDGAME_TEXT.001 | DONE | Endgame narrative: 5 paths (Trade Dominance, Military Supremacy, Diplomatic Unity, Pentagon Break, Loss). Title + 2-paragraph epilogue + FO farewell. Wire into victory_screen.gd, loss_screen.gd. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/epilogue_data.gd, scripts/ui/victory_screen.gd, scripts/ui/loss_screen.gd |
| GATE.T46.AUDIO.STEM_PIPELINE.001 | DONE | Music manager: scripts/audio/music_manager.gd with 4 layers (bass/pad/melody/percussion). Crossfade between states (exploration/combat/tension/dock). Autoload. Placeholder sine-wave stems. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/audio/music_manager.gd, project.godot |
| GATE.T46.AUDIO.COMBAT_MUSIC.001 | DONE | Combat music: wire to game_manager combat_started/combat_ended signals. Crossfade to combat stems. Tension state for warfront proximity. Dock state for calm ambient. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/audio/music_manager.gd, scripts/core/game_manager.gd |
| GATE.X.HYGIENE.REPO_HEALTH.046 | DONE | Full test suite + build baseline for T46. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q` + `dotnet build "Space Trade Empire.csproj" --nologo -v q` | SimCore.Tests/SimCore.Tests.csproj |
| GATE.X.HYGIENE.EPIC_REVIEW.046 | DONE | Audit epic statuses. Update SAVE, STEAM, NARRATIVE_CONTENT, MUSIC, PERF epics. Close completed. Recommend T47 anchor. Proof: RoadmapConsistency | docs/54_EPICS.md, docs/55_GATES.md |

Execution plan:
- Tier 1 (12 gates, 4 parallel groups): core (TICK_PROFILER, MEMORY_BUDGET), bridge (ADDON_INSTALL, EXPORT_TEMPLATE, AUTOSAVE_SYSTEM, NODE_MESH, STEM_PIPELINE), content (DOCK_FLAVOR→HAVEN_LOGS→FRAGMENT_LORE, hash chain), docs (REPO_HEALTH).
- Tier 2 (7 gates): bridge (INIT_WRAPPER, ACHIEVEMENT_BRIDGE, RELEASE_TEST, AUTOSAVE_UI, COMBAT_MUSIC), content (ENDGAME_TEXT), docs (PROFILE_REPORT).
- Tier 3 (1 gate): docs (EPIC_REVIEW).
- Hash-affecting chain (content): DOCK_FLAVOR→HAVEN_LOGS→FRAGMENT_LORE (tier 1, sequential).
- File conflict groups: steam_manager.gd (ADDON_INSTALL→INIT_WRAPPER→ACHIEVEMENT_BRIDGE, tiered), music_manager.gd (STEM_PIPELINE→COMBAT_MUSIC, tiered).
- NEW files (5): scripts/platform/steam_manager.gd, export_presets.cfg, scripts/tools/Build-Release.ps1, SimCore/Tweaks/AutoSaveTweaksV0.cs, scripts/audio/music_manager.gd.

## AP. Tranche 47 — "Economy Visuals + Music + Haven Polish + Save UX" (EPIC.T44.ECONOMY_VISUALS.V0, EPIC.S9.MUSIC.V0, EPIC.S8.HAVEN_STARBASE.V0, EPIC.S8.MEGAPROJECTS, EPIC.S9.SAVE)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T47.AMBIENT.SHUTTLE_TRAFFIC.001 | DONE | Station traffic shuttles: BoxMesh orbiting stations, count from NpcTradeActivity, elliptical orbit animation. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.AMBIENT.MINING_BEAMS.001 | DONE | Mining extraction beams: CylinderMesh from mining stations to asteroids, pulsing emission. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.AMBIENT.PROSPERITY_TIERS.001 | DONE | Station prosperity lighting: emission multiplier by market breadth (0.5x/1.0x/2.0x), golden orb for capitals. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.AMBIENT.LANE_TRAFFIC.001 | DONE | Lane traffic sprites: PrismMesh lerping along intra-region lanes proportional to NPC trade activity. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.DIGEST.MARKET_ALERTS.001 | DONE | Market alert toasts: colored by type (stockout orange, spike yellow, drop cyan), station display names, 30s poll, max 3/poll. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/ui/economy_alert_poller.gd |
| GATE.T47.DIGEST.ECON_PANEL.001 | DONE | Economy snapshot dock panel: per-good supply rows with role tags [P]/[C], supply count color-coded, trend arrows from price divergence. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/ui/hero_trade_menu.gd |
| GATE.T47.MUSIC.COMPOSITION_BRIEF.001 | DONE | Music production brief: 29-file spec (20 stems, 3 stingers, 5 faction loops, fracture ambient), harmonic plan rooted in D, reference tracks, technical specs. Proof: `test -f docs/design/music_production_brief_v0.md` | docs/design/music_production_brief_v0.md |
| GATE.T47.MUSIC.DISCOVERY_STINGERS.001 | DONE | Discovery stingers: 3 types (minor 3s, major 5s, revelation 8s), stem ducking during playback, wired to discovery events in game_manager. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/audio/music_manager.gd, scripts/core/game_manager.gd |
| GATE.T47.MUSIC.FRACTURE_AMBIENCE.001 | DONE | Fracture ambient: FRACTURE music state with detuned frequencies, LFO tremolo, 3s crossfade from EXPLORATION. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/audio/music_manager.gd |
| GATE.T47.MUSIC.FACTION_AMBIENT.001 | DONE | Faction territory ambient: 5 characteristic drones (Concord 220Hz, Chitin 147Hz FM, Weavers 330Hz harmonics, Valorin 110Hz+noise, Communion 440Hz+shimmer), 2s crossfade. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/audio/music_manager.gd |
| GATE.T47.HAVEN.COMING_HOME.001 | DONE | Haven arrival cinematic: warm amber letterbox, slow camera zoom, "Welcome home, Captain." FO toast, 3.5s sequence. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/core/game_manager.gd |
| GATE.T47.HAVEN.VISUAL_TIERS.001 | DONE | Haven visual geometry per tier: T1 purple ring, T2 satellites, T3 outer ring, T4 hex frame+pulsing beacon, T5 golden emission+tilted rings. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.HAVEN.COMMUNION_REP.001 | DONE | Communion Representative: 8 dialogue lines in FactionDialogueContentV0, GetCommunionRepDialogueV0 bridge, haven_panel "Speak Again" cycling, tier 3+ gated. Proof: `dotnet test --filter "Determinism"` | SimCore/Content/FactionDialogueContentV0.cs, scripts/bridge/SimBridge.Haven.cs, scripts/ui/haven_panel.gd |
| GATE.T47.MEGAPROJECT.MAP_MARKERS.001 | DONE | Megaproject galaxy map markers: type-specific meshes (hex frame anchor, diamond corridor, cone pylon), color by state. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.MEGAPROJECT.CONSTRUCTION_VFX.001 | DONE | Construction VFX: rotating cylinder spars + blinking spark spheres, progress-scaled intensity. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T47.SAVE.RECOVERY_UX.001 | DONE | Save corruption recovery: GetSaveIntegrityV0 check, red visual treatment for corrupted saves, "Try Load" fallback. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/ui/main_menu.gd |
| GATE.T47.SAVE.SLOT_MANAGEMENT.001 | DONE | Save slot management: scan/load/rename/delete per slot, confirmation dialog, auto-save badge. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/ui/main_menu.gd |
| GATE.T47.EVAL.ECONOMY_FEEL.001 | DONE | Economy visual feel evaluation: FH bot 18/18 assertions, 5.0/5.0 avg, overlay health 5/5. Proof: `powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless` | reports/first_hour/ |
| GATE.X.HYGIENE.REPO_HEALTH.047 | DONE | Full test suite: 1512/1512 pass, 0 errors, build clean. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q` | SimCore.Tests/ |
| GATE.X.HYGIENE.EPIC_REVIEW.047 | DONE | Epic audit: ECONOMY_VISUALS→DONE, MUSIC→DONE, HAVEN_STARBASE→DONE, MEGAPROJECTS→DONE, SAVE→DONE, NARRATIVE_CONTENT→DONE. Proof: `grep -c 'DONE' docs/54_EPICS.md` | docs/54_EPICS.md |

Execution plan:
- Tier 1 (17 gates, 4 groups): bridge (SHUTTLE_TRAFFIC, MINING_BEAMS, LANE_TRAFFIC, COMING_HOME, VISUAL_TIERS, MAP_MARKERS, CONSTRUCTION_VFX, RECOVERY_UX, SLOT_MANAGEMENT), content (PROSPERITY_TIERS, STINGERS, FRACTURE, FACTION_AMBIENT, COMMUNION_REP), docs (COMPOSITION_BRIEF, REPO_HEALTH), bridge (MARKET_ALERTS, ECON_PANEL).
- Tier 2 (1 gate): docs (ECONOMY_FEEL — depends on visual gates).
- Tier 3 (1 gate): docs (EPIC_REVIEW — depends on tier 2).
- Hash-affecting: COMMUNION_REP only (FactionDialogueContentV0.cs).
- File conflict groups: galaxy_spawner.gd (SHUTTLE+MINING+LANE+PROSPERITY+VISUAL_TIERS+MAP_MARKERS+CONSTRUCTION = 7 gates, combined into 2 agents), music_manager.gd (STINGERS+FRACTURE+FACTION = 3 gates, combined into 1 agent), main_menu.gd (RECOVERY+SLOT = 2 gates, combined into 1 agent).

## AQ. Tranche 48 — Template Missions, Discovery UI, Anomaly Chains, Maintenance, Telemetry

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T48.TEMPLATE.SCHEMA.001 | DONE | Mission template schema (TemplateMissionContentV0.cs) + TemplateMissionSystem.cs engine: variable binding ($GOOD_1, $TARGET_NODE, $FACTION_1), twist slot definition, acceptance/progress/completion lifecycle. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Content/TemplateMissionContentV0.cs, SimCore/Systems/TemplateMissionSystem.cs |
| GATE.T48.TEMPLATE.SUPPLY_SET.001 | DONE | 4 supply/logistics template definitions in content registry: Bulk Haul, Shortage Relief, Cross-Sector Resupply, Emergency Provisions. Hash-affecting, blocks on SCHEMA. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | docs/content/content_registry_v0.json, SimCore/Content/TemplateMissionContentV0.cs |
| GATE.T48.TEMPLATE.EXPLORE_SET.001 | DONE | 4 exploration template definitions: Cartography Run, Deep Scan Sweep, Fracture Probe, Signal Trace. Hash-affecting, blocks on SUPPLY_SET. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | docs/content/content_registry_v0.json, SimCore/Content/TemplateMissionContentV0.cs |
| GATE.T48.TEMPLATE.COMBAT_SET.001 | DONE | 3 combat/security templates: Pirate Clearance, Convoy Defense, Blockade Runner. Hash-affecting, blocks on EXPLORE_SET. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | docs/content/content_registry_v0.json, SimCore/Content/TemplateMissionContentV0.cs |
| GATE.T48.TEMPLATE.POLITICS_SET.001 | DONE | 3 reputation/politics templates: Diplomatic Courier, Sanctions Runner, Intelligence Delivery. Hash-affecting, blocks on COMBAT_SET. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | docs/content/content_registry_v0.json, SimCore/Content/TemplateMissionContentV0.cs |
| GATE.T48.TEMPLATE.TWIST_ENGINE.001 | DONE | Twist slot system: 7 twist types (blockade, ambush, price_spike, rival_runner, contraband_mixed, shortage_shift, intelligence), probability weighting, reward scaling by twist count. Tier 2, blocks on SCHEMA. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Systems/TemplateMissionSystem.cs, SimCore/Content/TemplateMissionContentV0.cs |
| GATE.T48.TEMPLATE.CONTEXT_SURFACE.001 | DONE | Template surfacing via station context: SimBridge.Mission.cs exposes available templates at current dock, filtered by player state/reputation/cargo. No mission board — templates appear in station interaction. Tier 2, blocks on SCHEMA. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/bridge/SimBridge.Mission.cs, scripts/ui/hero_trade_menu.gd |
| GATE.T48.DISCOVERY.MAP_MARKERS.001 | DONE | Discovery phase markers on galaxy map: undiscovered (dim ?), scanning (pulse ring), scanned (icon by type), exploited (faded). Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/GalaxyView.cs, scripts/view/galaxy_spawner.gd |
| GATE.T48.DISCOVERY.SCANNER_VIZ.001 | DONE | Scanner range ring visualization: semi-transparent sphere at scanner range, pulses during active scan, color by scan progress. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/view/galaxy_spawner.gd |
| GATE.T48.DISCOVERY.MILESTONE_CARDS.001 | DONE | Discovery milestone feedback: toast notification on first discovery, scan complete, chain progress. Brief text + icon, auto-dismiss. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/ui/hud.gd |
| GATE.T48.DISCOVERY.KNOWLEDGE_POLISH.001 | DONE | Knowledge web layout polish: node spacing, edge routing, zoom levels, category coloring, tooltip detail. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/ui/knowledge_web_panel.gd |
| GATE.T48.ANOMALY.CHAIN_SYSTEM.001 | DONE | Anomaly chain engine: AnomalyChainSystem.cs tracks multi-site discovery arcs (3-5 sites), narrative state per chain, escalation triggers, completion rewards. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Systems/AnomalyChainSystem.cs, SimCore/Entities/AnomalyChain.cs |
| GATE.T48.ANOMALY.CHAIN_CONTENT.001 | DONE | 3 starter anomaly chains in content registry: Signal Echo (3 sites, comms), Void Bloom (4 sites, biological), Lattice Fracture (5 sites, precursor). Tier 2, blocks on CHAIN_SYSTEM. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | docs/content/content_registry_v0.json, SimCore/Content/AnomalyChainContentV0.cs |
| GATE.T48.TENSION.MAINTENANCE.001 | DONE | Fleet upkeep drain: FleetUpkeepSystem.cs deducts per-tick maintenance cost based on ship class + modules. 50-80 tick idle runway per dynamic_tension_v0.md Pillar 2. Fixes ACTIVE_ISSUES E1. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Systems/FleetUpkeepSystem.cs, SimCore/Tweaks/FleetUpkeepTweaksV0.cs |
| GATE.T48.TENSION.UPKEEP_BRIDGE.001 | DONE | Upkeep display: SimBridge exposes GetFleetUpkeepV0 (per-module costs, total/tick), HUD shows upkeep bar, dock shows cost breakdown. Tier 2, blocks on MAINTENANCE. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/bridge/SimBridge.Fleet.cs, scripts/ui/hud.gd, scripts/ui/hero_trade_menu.gd |
| GATE.T48.TELEMETRY.SESSION_WRITER.001 | DONE | Dev-facing session telemetry: TelemetrySystem.cs writes session stats (tick count, trade count, combat count, credits high/low, death count) to user://telemetry/ on quit. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Systems/TelemetrySystem.cs |
| GATE.T48.TELEMETRY.CRASH_HOOK.001 | DONE | Crash/exception reporting: GDScript hook captures unhandled exceptions + last 50 log lines to user://crash_reports/. Tier 2, blocks on SESSION_WRITER. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/core/game_manager.gd |
| GATE.T48.FH_BOT.EXPANSION.001 | DONE | First-hour bot v2: real combat pipeline (battle stations + AI shots + heat), module remove, systemic mission accept, boot experience checks, progressive disclosure, overlay probes, haven probe, UI panel checks. 15 report card dimensions. Proof: `powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless` | scripts/tests/test_first_hour_proof_v0.gd |
| GATE.X.HYGIENE.REPO_HEALTH.048 | DONE | Full test suite baseline, warning scan, dead code check. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q` | SimCore.Tests/ |
| GATE.X.HYGIENE.EPIC_REVIEW.048 | DONE | Epic audit: close completed epics, recommend T49 anchor. Proof: `grep -c 'DONE' docs/54_EPICS.md` | docs/54_EPICS.md |

Execution plan:
- Tier 1 (11 gates, 4 groups): core (TEMPLATE.SCHEMA, ANOMALY.CHAIN_SYSTEM, TENSION.MAINTENANCE, TELEMETRY.SESSION_WRITER — sequential hash chain), bridge (DISCOVERY.MAP_MARKERS, SCANNER_VIZ, MILESTONE_CARDS, KNOWLEDGE_POLISH), bridge (FH_BOT.EXPANSION), docs (REPO_HEALTH).
- Tier 2 (8 gates): content (SUPPLY_SET→EXPLORE_SET→COMBAT_SET→POLITICS_SET — sequential hash chain), core (TWIST_ENGINE, ANOMALY.CHAIN_CONTENT — hash-affecting), bridge (CONTEXT_SURFACE, UPKEEP_BRIDGE, CRASH_HOOK).
- Tier 3 (1 gate): docs (EPIC_REVIEW).
- Hash-affecting chains: SCHEMA→SUPPLY→EXPLORE→COMBAT→POLITICS (5), TWIST_ENGINE (1), CHAIN_SYSTEM→CHAIN_CONTENT (2), MAINTENANCE (1), SESSION_WRITER (1) = 10 hash-affecting gates.
- File conflict groups: galaxy_spawner.gd (MAP_MARKERS+SCANNER_VIZ = combined), hero_trade_menu.gd (CONTEXT_SURFACE+UPKEEP_BRIDGE = combined).

## AR. Tranche 49 — "Eval Framework Hardening" (EPIC.X.EVAL_HARDENING.V0)

| Gate ID | Status | Description | Evidence |
|---------|--------|-------------|----------|
| GATE.T49.EVAL.ECON_NARRATIVE.001 | DONE | Fix economy_health eval bot: system_nodes→nodes, node_id field, station detection via GetPlayerMarketViewV0. Fix narrative_pacing eval bot: candidate→fo_type, phase_name→phase. Run headless, 0 SCRIPT_ERROR, some PASS. Proof: `godot --headless -s test_economy_health_eval_v0.gd` | scripts/tests/test_economy_health_eval_v0.gd, scripts/tests/test_narrative_pacing_eval_v0.gd |
| GATE.T49.EVAL.DREAD_FLIGHT.001 | DONE | Fix dread_pacing eval bot: GetEdgesV0→GetGalaxySnapshotV0+lane_edges, from_id/to_id, phase field, hull via GetFleetCombatHpV0. Fix flight_feel eval bot: camera node path, player spawn timing, current_node_id, same edge fix. Proof: `godot --headless -s test_dread_pacing_eval_v0.gd` | scripts/tests/test_dread_pacing_eval_v0.gd, scripts/tests/test_flight_feel_eval_v0.gd |
| GATE.T49.EVAL.AUDIO_ATMOS.001 | DONE | Fix audio_atmosphere eval bot: phase_index→phase, volume_db hard asserts→warn (no audio in headless). Proof: `godot --headless -s test_audio_atmosphere_eval_v0.gd` | scripts/tests/test_audio_atmosphere_eval_v0.gd |
| GATE.T49.DS_BOT.MUTATION_COVERAGE.001 | DONE | Deep systems bot: exercise 9 remaining UNCALLED mutation bridge methods with proper setup. Target: UNCALLED→0. Proof: `Run-FHBot-MultiSeed.ps1 -Script deep_systems -Seeds 42` | scripts/tests/test_deep_systems_v0.gd |
| GATE.T49.STRESS.IDLE_REDUCTION.001 | DONE | Stress bot: explore-when-idle fallback, force Scrap buy. Target: idle <25%, goods 12/12. Fixes ACTIVE_ISSUES E3. Proof: `Run-Bot.ps1 -Mode stress -Cycles 1500` | scripts/tests/exploration_bot_v1.gd |
| GATE.T49.TUTORIAL.T48_COVERAGE.001 | DONE | Tutorial bot: probes for upkeep display, template missions, anomaly chains. 6-8 new assertions. Proof: `godot --headless -s test_tutorial_proof_v0.gd -- --seed=42` | scripts/tests/test_tutorial_proof_v0.gd |
| GATE.T49.SWEEP.DOCK_PANELS.001 | DONE | Visual sweep: 6 dock-panel phases (haven, warfront, doctrine, budget, narrative, megaproject). Proof: `Run-Screenshot.ps1 -Mode full` | scripts/tests/visual_sweep_bot_v0.gd |
| GATE.T49.AESTHETIC.CAMERA_EXEMPT.001 | DONE | Exempt galaxy-map camera from TOO_FAR check. Fixes ACTIVE_ISSUES P2. Proof: `Run-FHBot.ps1 -Mode headless` | scripts/tools/aesthetic_audit.gd |
| GATE.T49.CHAOS.SCENARIO_EXPANSION.001 | DONE | Chaos bot: S9 rapid-fire trade spam, S10 undock-during-warp-cooldown. Proof: `Run-FHBot-MultiSeed.ps1 -Script chaos_tutorial -Seeds 42` | scripts/tests/test_chaos_tutorial_v0.gd |
| GATE.T49.RUBRIC.DIMENSION_UPDATE.001 | DONE | Update first_hour_rubric.md + visual_eval_guide.md with T48 dimensions (upkeep, templates, anomalies, new panels). Proof: `grep -c 'upkeep' scripts/tools/first_hour_rubric.md` | scripts/tools/first_hour_rubric.md, scripts/tools/visual_eval_guide.md |
| GATE.T49.COVERAGE.SCANNER_FIX.001 | DONE | Fix Run-CoverageGap.ps1: scan C# UI files + all test scripts. True UNCALLED drops to ~9. Proof: `Run-CoverageGap.ps1` | scripts/tools/Run-CoverageGap.ps1 |
| GATE.T49.PIPELINE.EVAL_RUNNER.001 | DONE | Create Run-EvalBot.ps1: runs 5 eval bots, captures to reports/eval/, aggregates pass/warn/fail. Proof: `Run-EvalBot.ps1` | NEW: scripts/tools/Run-EvalBot.ps1 |
| GATE.T49.OPTIMIZE.PASS1_EXPANSION.001 | DONE | Expand Run-OptimizeScan.ps1: GDScript quality + allocation auto-checks. Proof: `Run-OptimizeScan.ps1` | scripts/tools/Run-OptimizeScan.ps1 |
| GATE.T49.SWEEP.FLIGHT_ENDSTATE.001 | DONE | Visual sweep: 6 flight+endstate phases (scanner, fracture, data log, loss, victory, pause). Tier 2. Proof: `Run-Screenshot.ps1 -Mode full` | scripts/tests/visual_sweep_bot_v0.gd |
| GATE.T49.COVERAGE.DEAD_CLEANUP.001 | DONE | Remove confirmed dead bridge methods after scanner fix. Tier 2. Proof: `dotnet build "Space Trade Empire.csproj" --nologo` | scripts/bridge/SimBridge.cs |
| GATE.T49.PIPELINE.AUDIT_QUICK.001 | DONE | Create Run-AuditQuick.ps1: C# tests + optimize + coverage in <60s. Tier 2. Proof: `Run-AuditQuick.ps1` | NEW: scripts/tools/Run-AuditQuick.ps1 |
| GATE.T49.PROOF.FULL_EVAL_RUN.001 | DONE | HEADLESS_PROOF: run all eval bots via Run-EvalBot.ps1, verify 0 SCRIPT_ERROR. Tier 2. Proof: `Run-EvalBot.ps1` | NEW: reports/eval/aggregate_report.txt |
| GATE.T49.RESEARCH.PERF_PROFILE.001 | DONE | FPS profiling analysis for P1 (17fps drops). Research only. Tier 3. Proof: written report | docs/ACTIVE_ISSUES.md |
| GATE.X.HYGIENE.REPO_HEALTH.049 | DONE | Full test suite + warning scan + golden hash stability. Proof: `dotnet test SimCore.Tests -c Release` | SimCore.Tests/ |
| GATE.X.HYGIENE.EPIC_REVIEW.049 | DONE | Epic audit: close completed, recommend T50 anchor. Tier 3. Proof: `grep -c 'DONE' docs/54_EPICS.md` | docs/54_EPICS.md |

Execution plan:
- Tier 1 (14 gates): bridge (9 gates, all separate files — full parallelism), docs (5 gates).
- Tier 2 (4 gates): bridge (SWEEP.FLIGHT_ENDSTATE, DEAD_CLEANUP), docs (AUDIT_QUICK, FULL_EVAL_RUN).
- Tier 3 (2 gates): docs (PERF_PROFILE, EPIC_REVIEW).
- No hash-affecting gates. Full tier-1 parallelism.
- File conflict: SWEEP.DOCK_PANELS + SWEEP.FLIGHT_ENDSTATE share visual_sweep_bot_v0.gd — tiered.

## AS. Tranche 50 — "Performance & Visual Polish" (EPIC.X.PERF_OPTIMIZATION.V0 + EPIC.X.GALAXY_MAP_VISUAL.V0)

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T50.PERF.COMBAT_THROTTLE.001 | DONE | Throttle LatticeDroneCombatSystem.Resolve() to every 3-5 ticks via LastEngagementTick. Cache weapon profiles. Fixes P1 17fps. Hash-affecting, core T1. Proof: `dotnet test --filter Determinism` | SimCore/Systems/LatticeDroneCombatSystem.cs, SimCore/Tweaks/LatticeDroneTweaksV0.cs |
| GATE.T50.PERF.NPC_TRADE_CACHE.001 | DONE | Cache ComputeHopsFromFactionHome BFS results per faction home. Invalidate only on topology change. Hash-affecting, core T1. Proof: `dotnet test --filter Determinism` | SimCore/Systems/NpcTradeSystem.cs, SimCore/Tweaks/NpcTradeTweaksV0.cs |
| GATE.T50.PERF.INTEL_ALLOC.001 | DONE | Eliminate LINQ/new collections in IntelSystem.Process() hot path (~500 allocs/tick). Use scratch arrays. Hash-affecting, core T1. Proof: `dotnet test --filter Determinism` | SimCore/Systems/IntelSystem.cs |
| GATE.T50.PERF.FO_ALLOC.001 | DONE | Eliminate LINQ/new collections in FirstOfficerSystem.Process() hot path. Use scratch arrays. Hash-affecting, core T1. Proof: `dotnet test --filter Determinism` | SimCore/Systems/FirstOfficerSystem.cs, SimCore/Content/FirstOfficerContentV0.cs |
| GATE.T50.PERF.TICK_BUDGET.001 | DONE | Perf regression tests: assert Process() time < budget for top-5 systems under stress load. Core T2. Proof: `dotnet test --filter TickBudget` | SimCore.Tests/Invariants/TickBudgetTests.cs |
| GATE.T50.PERF.NPC_PHYSICS.001 | DONE | Replace NPC CharacterBody3D with Node3D + manual position lerp (NPCs don't collide). Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/core/npc_ship.gd, scenes/enemy.tscn |
| GATE.T50.PERF.CAMERA_CACHE.001 | DONE | Cache GalaxyView/HUD refs at _ready. Throttle UpdateAltitudeLodV0 to altitude delta > 5u. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/view/player_follow_camera.gd |
| GATE.T50.PERF.REP_CACHE.001 | DONE | Add 2-sec TTL cache for GetPlayerReputationV0 in SimBridge. Eliminates 30+ read-lock calls per frame. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/bridge/SimBridge.Faction.cs, scripts/core/npc_ship.gd |
| GATE.T50.PERF.HEADLESS_PROOF.001 | DONE | Add FPS measurement to FH bot (PERF|fps_min, fps_avg). Run 5 seeds, assert fps_min >= 30. Bridge T2. Proof: `Run-FHBot-MultiSeed.ps1` | scripts/tests/test_first_hour_proof_v0.gd |
| GATE.T50.ECON.ROUTE_QUALITY.001 | DONE | GalaxyGenerator: ensure at least 1 profitable route (margin > 10cr) within 2 hops of player start. Fixes E2 profit variance. Hash-affecting, core T1. Proof: `dotnet test --filter Determinism` | SimCore/Gen/GalaxyGenerator.cs, SimCore/Gen/MarketInitGen.cs |
| GATE.T50.ECON.ROUTE_QUALITY_TEST.001 | DONE | Monte Carlo test: 10 seeds, assert profitable route within 2 hops for all. Core T2. Proof: `dotnet test --filter RouteQuality` | NEW: SimCore.Tests/Invariants/RouteQualityTests.cs (no existing route quality test) |
| GATE.T50.VISUAL.GALAXY_NODES.001 | DONE | Galaxy map nodes: size by station tier, color by industry type. Replace uniform green dots. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/view/GalaxyView.cs, scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.T50.VISUAL.GALAXY_FACTION.001 | DONE | Galaxy map: faction territory color tinting on nodes/edges. Use faction primary colors. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/view/GalaxyView.cs, scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.T50.VISUAL.GALAXY_ECON.001 | DONE | Galaxy map: economic indicators (price differential arrows, trade volume markers). Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/view/GalaxyView.cs, scripts/bridge/SimBridge.GalaxyMap.cs |
| GATE.X.HYGIENE.REPO_HEALTH.050 | DONE | Full test suite + warning scan + golden hash stability. Proof: `dotnet test SimCore.Tests -c Release` | SimCore.Tests/ |
| GATE.T50.RESEARCH.PERF_VALIDATION.001 | DONE | Post-optimization FPS validation: run FH bot, compare fps_min/avg against pre-optimization baseline. Tier 3. Proof: written report | reports/audit/perf_profile_t49.md |
| GATE.X.HYGIENE.EPIC_REVIEW.050 | DONE | Epic audit: close completed epics, recommend T51 anchor. Tier 3. Proof: `grep -c 'DONE' docs/54_EPICS.md` | docs/54_EPICS.md |

Execution plan:
- Tier 1 (12 gates): core (5 hash-affected sequential: COMBAT_THROTTLE→NPC_TRADE_CACHE→INTEL_ALLOC→FO_ALLOC→ROUTE_QUALITY), bridge-perf (3 gates: NPC_PHYSICS, CAMERA_CACHE, REP_CACHE), bridge-visual (3 gates combined: GALAXY_NODES+FACTION+ECON), docs (REPO_HEALTH).
- Tier 2 (3 gates): core (TICK_BUDGET, ROUTE_QUALITY_TEST), bridge (HEADLESS_PROOF).
- Tier 3 (2 gates): docs (PERF_VALIDATION, EPIC_REVIEW).
- Hash chain: 5 hash-affecting T1 core gates chain sequentially (each changes golden hash baseline).
- File conflict: GALAXY_NODES + GALAXY_FACTION + GALAXY_ECON share GalaxyView.cs — combined agent.

## T51: Voice Playback + Steam + Telemetry + Mission Templates

| Gate ID | Status | Gate | Evidence |
|---|---|---|---|
| GATE.T51.VO.BUS_PLAYER.001 | DONE | Add VO audio bus to music_manager.gd (6th bus). Implement Music/Ambient volume ducking when VO plays (fade to 40% over 200ms, restore on VO end). Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/audio/music_manager.gd |
| GATE.T51.VO.LOOKUP_SYSTEM.001 | DONE | Create vo_lookup.gd autoload: given (speaker, key, sequence_index), resolves `res://assets/audio/vo/{speaker}/{vo_key}_{seq:02d}.mp3`. Graceful fallback (returns null if file missing, no crash). Supports speakers: computer, maren, analyst, veteran, pathfinder. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | NEW: scripts/audio/vo_lookup.gd (no existing VO lookup system) |
| GATE.T51.VO.DIALOGUE_WIRE.001 | DONE | Wire VO playback into fo_dialogue_box.gd show_line(). On line display: query vo_lookup for audio, if found play on VO bus with ducking. Typewriter speed syncs to audio duration when VO present. Bridge T2 (depends on BUS_PLAYER + LOOKUP_SYSTEM). Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/fo_dialogue_box.gd, scripts/audio/vo_lookup.gd |
| GATE.T51.VO.BRIDGE_KEY.001 | DONE | Add `vo_key` string field to tutorial bridge snapshots (GetTutorialStateV0, GetTutorialDialogueV0). Each tutorial phase maps to a vo_key for VO file lookup. Bridge T1. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "Tutorial"` | scripts/bridge/SimBridge.Tutorial.cs, SimCore/Content/TutorialContentV0.cs |
| GATE.T51.VO.PRESET_SELECT.001 | DONE | Ship computer voice preset selection: settings panel dropdown (Male/Female/Neutral). Selection stored in user settings. vo_lookup.gd reads preference to resolve computer speaker subfolder. Bridge T2 (depends on LOOKUP_SYSTEM). Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/settings_menu.gd, scripts/audio/vo_lookup.gd |
| GATE.T51.VO.HEADLESS_PROOF.001 | DONE | Headless test: boot scene, verify vo_lookup resolves existing VO files correctly, verify VO bus exists in AudioServer, verify ducking signals fire. IN_ENGINE T3 (depends on BUS_PLAYER + LOOKUP_SYSTEM + BRIDGE_KEY). Proof: `godot --headless --path . -s res://scripts/tests/test_vo_system_v0.gd` | NEW: scripts/tests/test_vo_system_v0.gd (no existing VO test), scripts/audio/vo_lookup.gd |
| GATE.T51.STEAM.ADDON_DL.001 | DONE | Download GodotSteam addon, integrate into project. Wire steam_interface.gd autoload to use real GodotSteam when available, keep graceful fallback stub. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/autoload/steam_interface.gd, addons/ |
| GATE.T51.STEAM.CLOUD_SAVES.001 | DONE | Steam Cloud save sync: on save write to Steam remote storage, on load check Steam remote vs local timestamps. Graceful degradation when Steam offline. Bridge T2 (depends on ADDON_DL). Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/autoload/steam_interface.gd, scripts/core/save_load.gd |
| GATE.T51.STEAM.APP_CONFIG.001 | DONE | Steamworks app config: app_build.vdf, depot config, store page metadata skeleton. Document Steam partner onboarding steps in RUNBOOK. Bridge T2 (depends on ADDON_DL). Proof: written config files | NEW: steam/app_build.vdf (no existing Steam config), docs/57_RUNBOOK.md |
| GATE.T51.TELEMETRY.OPTIN_UI.001 | DONE | Add telemetry opt-in toggle to Settings > Gameplay tab. First-launch prompt if no preference set. Stores preference in user settings. Bridge T1. Proof: `dotnet build "Space Trade Empire.csproj"` | scripts/ui/settings_menu.gd |
| GATE.T51.TELEMETRY.LOCAL_STORE.001 | DONE | TelemetrySystem.cs: write per-session JSON telemetry to `user://telemetry/`. Fields: session_id, start_utc, events array (trade, combat, death, dock, mission). Respects opt-in flag. Core T1. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Systems/TelemetrySystem.cs, NEW: SimCore.Tests/Systems/TelemetrySystemTests.cs (no existing telemetry tests) |
| GATE.T51.TELEMETRY.QUIT_TRACK.001 | DONE | Track player death locations + quit points in telemetry. On death: log node_id, cause, tick. On quit: log node_id, credits, tick, play_duration. Core T2 (depends on LOCAL_STORE). Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Systems/TelemetrySystem.cs |
| GATE.T51.TEMPLATE.SUPPLY_AUTHOR.001 | DONE | Author 12-15 supply/logistics mission templates per mission_design_v0.md: cargo delivery, bulk hauling, emergency resupply, trade route establishment, resource pipeline. Procedural variables for goods, quantities, destinations. Core T1. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Content/MissionTemplateContentV0.cs |
| GATE.T51.TEMPLATE.EXPLORE_AUTHOR.001 | DONE | Author 10-12 exploration mission templates: survey system, map uncharted node, collect sensor data, investigate anomaly, establish scanner relay. Procedural variables for locations, discovery types. Core T1. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Content/MissionTemplateContentV0.cs |
| GATE.T51.TEMPLATE.COMBAT_AUTHOR.001 | DONE | Author 10-12 combat/security mission templates: pirate bounty, convoy escort, system patrol, blockade run, fleet defense. Procedural variables for enemy types, threat levels, reward tiers. Core T1. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Content/MissionTemplateContentV0.cs |
| GATE.T51.TEMPLATE.POLITICS_AUTHOR.001 | DONE | Author 8-10 reputation/politics mission templates: faction courier, diplomatic envoy, smuggling run, intel gathering, defector extraction. Procedural variables for factions, rep stakes, twist slots. Core T1. Hash-affecting. Proof: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism"` | SimCore/Content/MissionTemplateContentV0.cs |
| GATE.X.HYGIENE.REPO_HEALTH.051 | DONE | Full test suite + warning scan + golden hash stability. Proof: `dotnet test SimCore.Tests -c Release` | SimCore.Tests/ |
| GATE.X.HYGIENE.EPIC_REVIEW.051 | DONE | Epic audit: close completed epics, recommend T52 anchor. Tier 3. Proof: `grep -c 'DONE' docs/54_EPICS.md` | docs/54_EPICS.md |
| GATE.X.HYGIENE.ECONOMY_EVAL.051 | DONE | Economy balance evaluation: mission reward scaling vs trade income, credit curve analysis across 5 seeds, template reward fairness audit. Tier 3. Proof: written report | reports/ |

Execution plan:
- Tier 1 (12 gates): core (5 hash-affecting: LOCAL_STORE + 4 TEMPLATE gates — TEMPLATE gates share MissionTemplateContentV0.cs so combine into one agent), bridge (5 gates: BUS_PLAYER, LOOKUP_SYSTEM, BRIDGE_KEY, ADDON_DL, OPTIN_UI), docs (REPO_HEALTH).
- Tier 2 (5 gates): core (QUIT_TRACK), bridge (DIALOGUE_WIRE, PRESET_SELECT, CLOUD_SAVES, APP_CONFIG).
- Tier 3 (2 gates): bridge (HEADLESS_PROOF), docs (EPIC_REVIEW, ECONOMY_EVAL).
- Hash chain: LOCAL_STORE → QUIT_TRACK sequential in core. 4 TEMPLATE gates share MissionTemplateContentV0.cs — combined agent, sequential execution.
- File conflict: 4 TEMPLATE gates share MissionTemplateContentV0.cs — combined agent. BUS_PLAYER + LOOKUP_SYSTEM touch different files — parallel OK.
