# 54_EPICS

This is the canonical development ledger and roadmap.
Architecture and design-law docs define the spec. This doc defines execution order and tracking.

Generated artifacts (status packets, scans, test outputs) live under docs/generated and are evidence only.

Status meanings:
- TODO
- IN_PROGRESS
- DONE
- DEFERRED

Note: Status tokens are exact: TODO, IN_PROGRESS, DONE, DEFERRED (no spaces, no variants).

Update rule:
- Each dev session should target 1–3 gate movements or epic movements, and end with an update here plus evidence references.

Primary anchors:
- docs/50_51_52_53_Docs_Combined.md (slice locks, gate tests)
- docs/30_CONNECTIVITY_AND_INTERFACES.md
- docs/20_TESTING_AND_DETERMINISM.md
- docs/21_90_TERMS_UNITS_IDS.md
- docs/40_TOOLING_AND_HOOKS.md

---

## Status semantics (source of truth)
- docs/56_SESSION_LOG.md is authoritative for what was completed (PASS entries).
- docs/55_GATES.md is the authoritative current status ledger (must match session log).
- Epic and Slice statuses in this file are summaries derived from gates (see docs/generated/epic_status_v0.md).
- If any mismatch exists, fix docs/55_GATES.md first, then regenerate epic status.

---

## A. Slice map (Layer 1)

## Canonical Epic Bullets (authoritative for scanning and next-gate selection)
## Rule: epics listed here are eligible for scanning and next-gate selection; each EPIC ID must appear exactly once in this section.
## Evidence rule: epic descriptions state evidence needed, but must NOT hardcode evidence output paths. Evidence paths are chosen during gate authoring and recorded in docs/55_GATES.md and the queue tasks.

- EPIC.S2_5.WGEN.DISTINCTNESS.REPORT.V0 [DONE]: Deterministic world class stats report (byte-for-byte stable, no timestamps) over Seeds 1..100 using worldgen-era signals only (gates: GATE.S2_5.WGEN.DISTINCTNESS.REPORT.*)
- EPIC.S2_5.WGEN.DISTINCTNESS.TARGETS.V0 [DONE]: Enforce class separation targets using report metrics; violations list seeds + deltas sorted; exits nonzero on failure (gates: GATE.S2_5.WGEN.DISTINCTNESS.TARGETS.*)
- EPIC.S3.RISK_MODEL.V0 [DONE]: Deterministic lane%route risk bands emit schema-bound SecurityEvents (delay, loss, inspection) with deterministic cause chains; surfaced in Station timeline; save%load preserved; no Slice 5 combat coupling (gates: GATE.S3.RISK_MODEL.*)
- EPIC.S3_5.CONTENT_PACK_CONTRACT.V0 [DONE]: Versioned registries (goods%recipes%modules) with schema validation, canonical hashing, deterministic load order (gates: GATE.X.CONTENT_SUBSTRATE.001, GATE.S3_5.CONTENT_SUBSTRATE.001)
- EPIC.S3_5.PACK_VALIDATION_REPORT.V0 [DONE]: Deterministic validation report with stable ordering and nonzero exit on invalid packs (gates: GATE.S3_5.CONTENT_SUBSTRATE.002)
- EPIC.S3_5.WORLD_BINDING.V0 [DONE]: World identity binds pack digest and persists through save%load; repro surface includes pack id%version (gates: GATE.S3_5.CONTENT_SUBSTRATE.003)
- EPIC.S3_5.HARDCODE_GUARD.V0 [DONE]: Deterministic scan or contract test flags new hardcoded content IDs in systems that must be data-driven; violations sorted and reproducible (gates: GATE.S3_5.CONTENT_SUBSTRATE.004)
- EPIC.S2_5.WGEN.DISCOVERY_SEEDING.V0 [DONE]: Deterministic seeding of anomaly families, corridor traces, and resource pool markers with per-seed-class guarantees (gates: GATE.S2_5.WGEN.DISCOVERY_SEEDING.*)
- EPIC.S3_6.DISCOVERY_STATE.V0 [DONE]: Minimal discovery state v0 (seen%scanned%analyzed) + deterministic persistence (gates: GATE.S3_6.DISCOVERY_STATE.*)
- EPIC.S3_6.DISCOVERY_UNLOCK_CONTRACT.V0 [DONE]: Unlock contract v0 (Permit, Broker, Recipe, SiteBlueprint, CorridorAccess, SensorLayer) with explicit economic effects (gates: GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.*)
- EPIC.S3_6.RUMOR_INTEL_MIN.V0 [DONE]: Rumor%Intel substrate v0 for lore leads discovered via exploration%hub analysis; deterministic hints (region tags, coarse location, prerequisites); UI surfacing; save%load; no quest treadmill (gates: GATE.S3_6.RUMOR_INTEL_MIN.*)
- EPIC.S3_6.EXPEDITION_PROGRAMS.V0 [DONE]: ExpeditionProgram v0 focused on discovery (survey, sample, salvage, analyze); produces unlock inputs; no rescue treadmill requirement (gates: GATE.S3_6.EXPEDITION_PROGRAMS.*)
- EPIC.S1.HERO_SHIP_LOOP.V0 [DONE]: Player flies their ship in a physics-simulated solar system scene: thrust, inertia, collision; local space is 20,000u radius with star at origin and orbital objects (station, planets, asteroid clusters, lane gates) spawned from SimCore system data — not hardcoded scene positions (architectural constraint: data-driven from day one or procedural generation requires rework); named player states declared in GameShell (InFlight, Docked, InLaneTransit) even if InLaneTransit is minimal in v0 (architectural constraint: named state enables future interdiction%instability without restructuring); dock proximity trigger at 150u range hands off to existing station_interface or DiscoverySite scan flow; lane transit sequence: enter gate → fade → arrive at adjacent system gate, SimCore game-time advances per existing LaneFlowSystem cost; scale constants (scene radius, ship speed, dock range) declared as GameShell-only config not scene literals; basic camera; proven by Godot headless scene boot and GDScript smoke test; no combat required; satisfies Architecture 1.1 “The Player is a Pilot” invariant (gates: GATE.S1.HERO_SHIP_LOOP.*)
- EPIC.S1.GALAXY_MAP_PROTO.V0 [DONE]: Minimal galaxy map UI (Zone C zoom-out) backed by a new GetGalaxySnapshotV0 SimBridge contract; shows all systems with discovery states (Hidden=not shown, Rumored=??? node from RumorLead location tags, Visited=named node, Mapped=named+object list), lane connections, player current system highlighted, fleet unit counts per system node; read-only in v0 (no fracture plotting); accessible via Tab zoom-out from local space; contract derives entirely from existing SimCore discovery state, RumorLead, and LaneFlowSystem data — no new SimCore simulation logic required; prerequisite for EPIC.S6.MAP_GALAXY (gates: GATE.S1.GALAXY_MAP.*)
- EPIC.S3_6.UI_DISCOVERY_MIN.V0 [DONE]: Discovery UI v0 + unlock surfaces + “deploy package” controls; deterministic exception summaries and suggested policy actions (gates: GATE.S3_6.UI_DISCOVERY_MIN.*)
- EPIC.S3_6.EXPLOITATION_PACKAGES.V0 [DONE]: Exploitation packages v0 (TradeCharter, ResourceTap) with remote exception policies and deterministic reporting (gates: GATE.S3_6.EXPLOITATION_PACKAGES.*)
- EPIC.S3_6.PLAY_LOOP_PROOF.V0 [DONE]: Headless proof of first 60 minutes: discover -> dock at hub -> identify 1 trade loop -> acquire 1 starter freighter -> assign TradeCharter -> discover 1 mineable site -> deploy ResourceTap -> complete 1 research%refit tech unlock -> surface 1 lore lead -> trigger 1 piracy pressure incident with explainable cause chain -> keep exploring; deterministic, no timestamps, stable ordering (gates: GATE.S3_6.PLAY_LOOP_PROOF.*)
- EPIC.S4.CATALOG.V0 [DONE]: Starter catalog v0 shipped as content packs (goods%recipes%modules%weapons) with named chains and deterministic validation (gates: GATE.S4.CATALOG.*)
- EPIC.S4.MODULE_MODEL.V0 [DONE]: Hero slot model v0 + fleet capability packages (no per-ship fitting), content-driven modules and prereqs (gates: GATE.S4.MODULE_MODEL.*)
- EPIC.S1.PLAYABLE_BEAT.V0 [DONE]: First playable station loop with live in-game feedback — buy/sell buttons wired, ship input frozen while docked, market rows refresh after trade, collision layer fix; depends on EPIC.S1.HERO_SHIP_LOOP.V0 DONE (gates: GATE.S1.PLAYABLE_BEAT.*)
- EPIC.S4.INDU_STRUCT [DONE]: Industry structure v0: bounded production chain graph that is content-ID-driven and deterministic; recipe binding, chain graph validation, shortfall event log, and deterministic chain report (gates: GATE.S4.INDU_STRUCT.*)
- EPIC.S5.COMBAT_LOCAL [DONE]: Starcom-like hero combat v0 (shields%hull; turrets%missiles; 1 counter family; deterministic replay proof); depends on EPIC.S1.HERO_SHIP_LOOP.V0 and EPIC.S4.MODULE_MODEL.V0 (gates: GATE.S5.COMBAT_LOCAL.*)
- EPIC.S5.COMBAT_PLAYABLE.V0 [DONE]: In-engine combat encounters — fleet substantiation at system nodes, player-initiated combat trigger, combat loop headless proof; depends on EPIC.S5.COMBAT_LOCAL DONE (gates: GATE.S5.COMBAT_PLAYABLE.*)
- EPIC.S1.DISCOVERY_INTERACT.V0 [DONE]: Discovery site dock interaction — minimal panel (site_id, phase, undock) wired to existing SimBridge discovery queries (gates: GATE.S1.DISCOVERY_INTERACT.*)
- EPIC.X.CODE_HEALTH [DONE]: Code health hygiene — GalaxyGenerator report extraction, discovery seed extraction, StationMenu per-tab split (gates: GATE.X.HYGIENE.*)
- EPIC.S1.VISUAL_POLISH.V0 [DONE]: Visual presentation polish v0 — skybox, celestial bodies, station/gate geometry, fleet AI, combat visuals, HUD, galaxy map styling; GameShell-only, no SimCore changes (gates: GATE.S1.VISUAL_POLISH.*)
- EPIC.S6.FRACTURE_COMMERCE.V0 [DONE]: Off-lane commerce v0 designed for high leverage niches and elite hulls, feeding lane economy (gates: GATE.S6.FRACTURE_COMMERCE.*)
- EPIC.S6.FRACTURE_ECON_INVARIANTS.V0 [DONE]: Deterministic scenario-pack invariants proving fracture does not replace lanes (deterministic, no timestamps, stable ordering; hard-fails on drift) (gates: GATE.S6.FRACTURE_ECON_INVARIANTS.*)
- EPIC.S5.COMBAT_DOCTRINE.V0 [DONE]: Combat doctrine v0: point defense counter family, escort doctrine, strategic fleet-vs-fleet resolver, deterministic combat replay proof; completes Slice 5 content wave (gates: GATE.S5.COMBAT.*)
- EPIC.S1.AUDIO_MIN.V0 [DONE]: Minimal audio v0: engine thrust, turret fire, bullet hit, explosion SFX, ambient space drone, warp transit, dock chimes; GameShell-only, no SimCore (gates: GATE.S1.AUDIO.*)
- EPIC.S1.SAVE_LOAD_UI.V0 [DONE]: Save/load UI v0: escape pause menu, 3 save slots with metadata, wires existing SimBridge save/load; GameShell-only (gates: GATE.S1.SAVE_UI.*)
- EPIC.S1.MISSION_RUNNER.V0 [DONE]: Deterministic mission runner v0 — mission schema (mission_id, prerequisites, steps, triggers, assertions), headless executor, Mission 1 "Matched Luggage" proof, tutorial determinism clamp; no timed missions, no forced modals; gates the "first 60 minutes" Greatness Spec validation (gates: GATE.S1.MISSION.*)
- EPIC.X.PRESSURE_FORMALIZATION.V0 [DONE]: Pressure state ladder formalization — 5-state enum (Normal/Strained/Unstable/Critical/Collapsed) with mandatory direction indicator (Improving/Stable/Worsening), max-one-state-jump-per-window enforcement, intervention budget QA metric (1-3 alerts per 10 min, up to 5 in crisis), headless alert-count scenario test; binding across all pressure-emitting systems (gates: GATE.X.PRESSURE.*)
- EPIC.X.EXPERIENCE_PROOF.V0 [DONE]: Automated experience validation suite — ExperienceObserver reads scene tree as structured JSON (HUD text, materials, particles, audio, camera state); ExperienceTimeline tracks trajectories over time (credits growth, state coverage, pacing); 5 headless scenario playthroughs (early game trade loop, combat integration, galaxy map, discovery, full 60s capstone); AestheticAudit with 14 visual quality flags (critical: hard-fail; non-critical: warn); ExperienceMetrics with diagnostic output pointing to investigate paths; SystemConnectivity C# invariant; reports/experience/latest_report.json for LLM-assisted review; enables Claude iteration loop without human-in-the-loop (gates: GATE.X.EXP.*)
- EPIC.S1.CAMERA_POLISH.V0 [DONE]: Camera presentation polish v0 — Phantom Camera addon integration, flight/orbit/station follow modes, combat shake on turret fire and damage; GameShell-only, no SimCore changes (gates: GATE.S1.CAMERA.*)
- EPIC.S1.SPATIAL_AUDIO_DEPTH.V0 [DONE]: Spatial audio depth v0 — engine thrust AudioStreamRandomizer, positional combat SFX (fire/impact), ambient station hum and lane drone; AudioStreamPlayer3D with 3D falloff; GameShell-only, no SimCore changes (gates: GATE.S1.SPATIAL_AUDIO.*)
- EPIC.S1.VISUAL_UPGRADE.V0 [DONE]: Addon-powered visual upgrade v0 — Starlight procedural skybox, 3D planet generator with atmosphere shader, Kenney Space Kit models for player ship, stations, and fleet markers; GameShell-only, no SimCore changes (gates: GATE.S1.VISUAL_UPGRADE.*)
- EPIC.S1.FLEET_VISUAL.V0 [DONE]: Fleet ship substantiation v0 — replace generic wedge markers with Kenney Space Kit craft models matched to FleetRole (Trader→craft_cargoA, Hauler→craft_cargoB, Patrol→craft_speederA); galaxy map + local system view; headless proof (gates: GATE.S1.FLEET_VISUAL.*)
- EPIC.S5.NPC_TRADE.V0 [DONE]: NPC trade circulation v0 — autonomous NPC trader fleets evaluate adjacent markets, execute profitable buy/sell/travel, stabilize price differentials, create visible lane traffic; depends on existing fleet + market systems (gates: GATE.S5.NPC_TRADE.*)
- EPIC.S10.TRADE_DISCOVERY.V0 [DONE]: Trade intel UI surface — bridge queries for scanner/route/price intel, program creation surface (AutoSell, TradeCharter, ResourceTap, Expedition, ConstrCap), TradeCharter real market price fix, dock menu trade routes, research sustain status, headless proof; builds on already-implemented IntelSystem mechanics (gates: GATE.S10.TRADE_INTEL.*, GATE.S10.TRADE_PROG.*, GATE.S10.TECH_EFFECTS.*)
- EPIC.S10.EMPIRE_MGMT.V0 [DONE]: Empire management dashboard — unified modal panel (E key) with overview/trade/production/programs/intel tabs, galaxy map overlays (trade flow, intel freshness); player command center for managing all operations at a glance (gates: GATE.S10.EMPIRE.*)
- EPIC.S10.VOICE_PLAYBACK.V0 [TODO]: Game-wide voice-over playback system — VO audio bus with Music/Ambient ducking, file lookup by speaker+key+sequence with graceful fallback, fo_dialogue_box.gd integration, ship computer voice preset selection (male/female/neutral), bridge vo_key field, headless proof. GameShell + SimBridge. Content-agnostic infrastructure. Fixes ACTIVE_ISSUES F3 (gates: GATE.T51.VO.*)
- EPIC.S11.GAME_FEEL.V0 [DONE]: Game feel and player feedback v0 — toast notifications, tech tree UI, NPC route visualization, galaxy map node interaction, mission/research HUD indicators, keybindings help overlay, combat event log; making existing systems visible and the game world feel alive (gates: GATE.S11.GAME_FEEL.*)
- EPIC.S12.FLEET_SUBSTANCE.V0 [DONE]: Fleet visual substance — replace generic wedge markers with Quaternius spaceship models matched to fleet roles (trader/hauler/patrol/player), hash-based model variety, NPC circuit routes with animated flow visualization and trade volume labels (gates: GATE.S12.FLEET_SUBSTANCE.*, GATE.S12.NPC_CIRC.*)
- EPIC.S12.UX_POLISH.V0 [DONE]: UX polish and player progression — buy/sell quantity controls, display name formatting, first-dock onboarding, cargo display, trade feedback toasts, player stats tracking, milestone system with dashboard integration (gates: GATE.S12.UX_POLISH.*, GATE.S12.PROGRESSION.*)
- EPIC.S13.FEEL_OVERHAUL.V0 [DONE]: Playtest-driven feel overhaul — top-down camera, persistent rotation, ship turning/speed retuning, dock menu tabs with progressive disclosure, empire dashboard gating, label distance clamping, gate arrival/direction fixes, galaxy map centering, hostile labels, NPC behavior tuning, terminology cleanup (gates: GATE.S13.*)
- EPIC.S14.ALIVE_GALAXY.V0 [DONE]: Make systems feel alive — fix NPC fleet survival through WorldLoader, role diversity, gate transit warp effect, Kenney gate models, star tint fix, starter star guarantee, asteroid variety, dock visual framing, docking proximity, HUD cleanup, galaxy map player indicator, mission prompt (gates: GATE.S14.*)
- EPIC.S15.EXPLORATION_FEEL.V0 [DONE]: Exploration atmosphere — star-class lighting, jump events (salvage/signal/turbulence), faction territory mapping + labels, ambient particles, NPC freighter proximity substantiation, exploration headless proof (gates: GATE.S15.FEEL.*)
- EPIC.S16.NPC_SHIPS_ALIVE.V0 [DONE]: NPC fleet ships as physical 3D entities with LimboAI behavior trees, sim-driven movement, role-based AI (Trader/Hauler/Patrol), player-NPC combat feedback to SimCore, warp-in/out effects, fleet destruction + respawn (gates: GATE.S16.NPC_ALIVE.*)
- EPIC.S17.REAL_SPACE.V0 [DONE]: Continuous real-space galaxy with physical lane traversal, LOD system detail rendering, warp tunnel VFX, and high-altitude galaxy map (gates: GATE.S17.REAL_SPACE.*)
- EPIC.S18.TRADE_GOODS.V0 [DONE]: Trade goods overhaul from 10 to 13 goods with 9 recipes, geographic distribution, price bands, sustain alignment, NPC trade update, and headless proof per docs/design/trade_goods_v0.md (gates: GATE.S18.TRADE_GOODS.*)
- EPIC.S18.SHIP_MODULES.V0 [DONE]: Ship module system foundation with zone armor (4-directional HP), 8 ship classes, 3-constraint fitting (slots/power/sustain), and combat zone integration per docs/design/ship_modules_v0.md (gates: GATE.S18.SHIP_MODULES.*)
- EPIC.S18.EMPIRE_DASH.V0 [DONE]: Empire dashboard overhaul with 5-tab dock menu, Overview tab (F1), Economy tab (F2), Ship tab, Station tab per docs/design/EmpireDashboard.md (gates: GATE.S18.EMPIRE_DASH.*)
- EPIC.S5.COMBAT_LOOT.V0 [DONE]: Combat drop loot from destroyed ships, tractor beam interaction, rarity tiers (gates: GATE.S5.COMBAT_LOOT.*)
- EPIC.S5.TRACTOR_SYSTEM.V0 [DONE]: Tractor beam module system per ship_modules_v0.md — 3-tier module-gated range (T1 Magnetic Grapple 15u, T2 EM Tractor Array 30u auto-target, T3 Graviton Tether 50u grab disabled hulks), CollectLootCommand range check against equipped tractor module, visual tractor beam VFX (energy beam pulling loot), Weaver faction variant (Spindle Tractor 25u auto-salvage). **T33**: tractor ranges, 3 module content entries, SimBridge GetTractorRangeV0, tractor beam VFX. **T34**: Weaver Spindle Tractor variant (25u, auto-salvage flag), auto-target nearest loot (GetNearestLootV0). Extends EPIC.S5.COMBAT_LOOT.V0. SimCore + GameShell (gates: GATE.S5.TRACTOR.*)
- EPIC.S6.FRACTURE_DISCOVERY_EVENT.V0 [DONE]: Fracture module discovery gating at ~tick 300+ via frontier derelict encounter (gates: GATE.S6.FRACTURE_DISCOVERY.*)
- EPIC.S7.PRODUCTION_CHAINS.V0 [DONE]: Instantiate remaining 6/9 production recipes as industry sites in worldgen (gates: GATE.S7.PRODUCTION_CHAINS.*)
- EPIC.S7.SUSTAIN_ENFORCEMENT.V0 [DONE]: Fleet fuel consumption + module sustain resource deduction — dynamic_tension Pillar 2 (gates: GATE.S7.SUSTAIN_ENFORCEMENT.*)
- EPIC.S7.POWER_BUDGET.V0 [DONE]: Enforce PowerDraw ≤ BasePower, mount types, module degradation (gates: GATE.S7.POWER_BUDGET.*)
- EPIC.S7.INSTABILITY_EFFECTS.V0 [DONE]: Wire instability phase effects into MarketSystem + LaneFlowSystem (gates: GATE.S7.INSTABILITY_EFFECTS.*)
- EPIC.S7.T2_MODULE_CATALOG.V0 [DONE]: ~25 T2 modules with faction rep unlock thresholds. Tranche 23: faction rep gating in RefitSystem + T2 catalog entries added. Tranche 24: expanded to ~25 modules across all weapon/defense/engine/utility families (gates: GATE.S7.T2_MODULES.*)
- EPIC.S7.STARTER_PLACEMENT.V0 [DONE]: Player start system borders contested warfront node (gates: GATE.S7.STARTER_PLACEMENT.*)
- EPIC.S7.FACTION_VISUALS.V0 [DONE]: Faction-specific ship liveries, station aesthetics, UI themes (gates: GATE.S7.FACTION_VISUALS.*)
- EPIC.S7.SHIP_PROGRESSION.V0 [DONE]: Ship purchase system — 23 ship classes (8 base + 3 ancient + 12 faction variants), ShipyardSystem (purchase/sell-back), progressive catalog disclosure (mid-tier 3+ systems, capital rep 25+, variant rep 75+), module transfer on purchase, insurance model (2% premium, 70% payout), NPC faction fleet assignment (role-aware variants), fleet roster with insurance premium, ship comparison panel. T59: foundation. T62: stat balance, disclosure, NPCs, insurance, module reassign, roster/shipyard/intel UI (gates: GATE.T59.SHIP.*, GATE.T62.SHIP.*)
- EPIC.T60.BATTLE_STATIONS_LIVE.V0 [DONE]: Complete battle stations from broken tick decrement through keybind, dock reset, auto-trigger, combat effects (spin armor + heat rejection), visual rotation, turn feel, audio/VFX, camera shake, tutorial integration. Fixes critical spin-up tick bug. **T60**: tick decrement fix, B key toggle, dock reset + auto-trigger, spin armor + heat rejection, visual rotation, turn feel, gyro audio + running lights, camera shake, FO tutorial hint, E2E headless proof. SimCore + GameShell + SimBridge (gates: GATE.T60.SPIN.*, GATE.T60.PROOF.SPIN_E2E.*)
- EPIC.S7.ENFORCEMENT_ESCALATION.V0 [DONE]: Pattern-based heat accumulation (volume + route + counterparty signals), confiscation event type, fine system, heat decay window. Trace enforcement only — risk meter UI visualization moved to EPIC.S7.RISK_METER_UI.V0. Extends existing SecurityLaneSystem Edge.Heat field. Hash-affecting (gates: GATE.S7.ENFORCEMENT.*)
- EPIC.S7.HUD_ARCHITECTURE.V0 [DONE]: HUD information architecture overhaul — screen zone enforcement (Zones A-G per HudInformationArchitecture.md), toast notification priority levels and bundling, toast action bridges (clickable actions), progressive disclosure rules (Tier 1/2/3), alert badge in Zone A, Zone G bottom bar framework for risk meters and minimap. GameShell-only (gates: GATE.S7.HUD_ARCH.*)
- EPIC.S7.COMBAT_JUICE.V0 [DONE]: Combat feel and feedback — kill explosion VFX (fireball + debris + smoke), shield ripple shader on hit, shield break flash + audio, floating damage numbers, weapon trail differentiation by damage family, screen shake discipline per CombatFeel.md intensity scale, wire combat audio pool (exists but never called). GameShell-only (gates: GATE.S7.COMBAT_JUICE.*)
- EPIC.S7.COMBAT_FEEL_POLISH.V0 [DONE]: Combat feel polish — VFX scaled ~4x for altitude 80 camera (explosion, damage numbers, shield ripple, bullet hits), CombatAudio max_distance extended, shield VFX (ripple + break flash + hull sparks), weapon family visual differentiation. Addressed ACTIVE_ISSUES C1, C2, C3, C4, C5, C6, C7, A1. GameShell-only (gates: GATE.S7.COMBAT_FEEL_POLISH.*)
- EPIC.S7.RISK_METER_UI.V0 [DONE]: Risk meter visualization — Heat/Influence/Trace meter widgets in Zone G (bottom bar), 5 named thresholds (Calm/Noticed/Elevated/High/Critical) with distinct visual states, trend arrows (rising/decaying/stable), decay rate display, threshold transition toasts, screen-edge ambient tinting at High+, compound threat indicators when multiple meters elevated. Per RiskMeters.md. GameShell-only; SimBridge queries: GetRiskMetersV0, GetRiskDecayRateV0, GetCompoundThreatV0 (gates: GATE.S7.RISK_METER_UI.*)
- EPIC.S7.AUDIO_WIRING.V0 [DONE]: Audio asset wiring and bus architecture — connect 6 existing unused audio assets (combat pool, station ambient, dock chime, explosion, warp whoosh), implement 5-layer audio bus separation (Music/Ambient/SFX/UI/Alert) with ducking priority, discovery phase transition chimes, risk threshold alert sounds. Per AudioDesign.md. GameShell-only (gates: GATE.S7.AUDIO_WIRING.*)
- EPIC.S7.GALAXY_MAP_V2.V0 [DONE]: Galaxy map evolution — 5 new overlay modes (Faction Territory, Exploration, Fleet Positions, Warfronts, Heat), route planner with multi-hop path display, galaxy search (find system by name), semantic zoom (detail levels by altitude), constellation clustering, icon toggles. Per GalaxyMap.md. GameShell-only; SimBridge queries: GetFactionTerritoryOverlayV0, GetFleetPositionsV0, GetWarfrontOverlayV0, GetHeatOverlayV0, GetRouteV0, GetSystemSearchV0. All 9 gates DONE (gates: GATE.S7.GALAXY_MAP_V2.*)
- EPIC.S7.NARRATIVE_DELIVERY.V0 [DONE]: Narrative text delivery system — flavor_text/description fields in IntelBook entities, discovery narrative templates in DiscoveryOutcomeSystem, faction station greeting text keyed to reputation tier, narrative text display panel with faction voice styling. Per NarrativeDesign.md + content specs in docs/design/content/NarrativeContent_TBA.md. Hash-affecting for entity field additions (gates: GATE.S7.NARRATIVE_DELIVERY.*)
- EPIC.S7.AUTOMATION_MGMT.V0 [DONE]: Automation management layer — program performance tracking (profitability, activity stats), failure reason UI with actionable explanations, program budget caps, doctrine system (fleet-wide behavior rules), program templates. **T32**: ProgramMetricsSystem, ProgramHistorySystem, DoctrineSystem, SimBridge queries, Empire Dashboard automation tab. **T33**: ProgramTemplateContentV0 (5 templates), SimBridge GetProgramTemplatesV0, template picker UI. Per AutomationPrograms.md (gates: GATE.S7.AUTOMATION_MGMT.*)
- EPIC.S7.RUNTIME_STABILITY.V0 [DONE]: Runtime stability and visual bug fixes from full eval 2026-03-10 — HUD parse error fix, faction color crash, planet/camera warp arrival, galaxy view Z-ordering, ship visibility, combat VFX scaling, UI polish, VFX polish. Fixes ACTIVE_ISSUES R1, R2, V1, V4-V12, C10, U2-U5. GameShell+SimBridge fixes. All 8 gates DONE (gates: GATE.S7.RUNTIME_STABILITY.*)
- EPIC.S7.FLEET_TAB.V0 [DONE]: Empire Dashboard Fleet tab (F3) — master-detail fleet list with per-fleet cargo, installed modules, assigned programs, status, doctrine; action buttons (Recall, Dismiss, Rename), program assignment view. Pulled forward from S9 — fleet management is core gameplay, not polish. Per EmpireDashboard.md. GameShell-only (gates: GATE.S7.FLEET_TAB.*)
- EPIC.S7.MAIN_MENU.V0 [DONE]: Main menu game shell — main_menu.tscn as project main scene, adaptive menu list, new voyage wizard (captain name, galaxy seed, 3-tier difficulty with SimCore multipliers), save slot metadata preview, auto-save slot (dock/warp/mission triggers), pause menu overlay with Save and Quit. Per MainMenu.md. SimCore changes: difficulty multiplier fields in SimState, captain name persistence. GameShell + SimBridge + SimCore (gates: GATE.S7.MAIN_MENU.*)
- EPIC.S8.ADAPTATION_FRAGMENTS.V0 [DONE]: 16 Adaptation fragments (4 kinds: Biological/Structural/Energetic/Cognitive) + 8 resonance pairs, prerequisite for win scenarios. **T34**: AdaptationFragment entity + 16 content defs, AdaptationFragmentSystem (collect + resonance pair detection), DiscoverySeedGen placement at void sites, SimBridge queries (GetAdaptationFragmentsV0, GetResonancePairsV0, CollectFragmentV0, DepositFragmentV0), haven_panel.gd fragment display. Content in docs/design/content/LoreContent_TBA.md (gates: GATE.S8.ADAPTATION.*)
- EPIC.S8.HAVEN_STARBASE.V0 [DONE]: Hidden Precursor starbase per haven_starbase_v0.md — 5-tier upgrade tree, Haven Residents, Trophy Wall, Haven market, Hangar system, 20 data logs, Keeper system, Resonance Chamber, accommodation threads, endgame paths. **T33**: HavenStarbase entity, SeedHavenV0, HavenUpgradeSystem, HavenHangarSystem, deferred Haven market, SimBridge.Haven.cs, Haven dock panel, galaxy map icon. **T34**: Haven Residents (Keeper + FO candidates), Trophy Wall (deposit + resonance display), 19 Haven data logs, Fragment Geometry display, Ancient Hull restoration at T3+, headless proof. **T37**: Keeper 5-tier evolution, Resonance Chamber, Fabricator, Haven market restocking, depth bridge queries. **T41**: Research lab, module transfer, accommodation bonuses, faction ally reveal. **T47**: Coming Home arrival cinematic (warm amber letterbox, slow zoom, FO toast), visual geometry per tier (T1-T5 Precursor meshes: rings/satellites/hex frames/beacons), Communion Representative dialogue (8 themed lines, cycling, tier 3+ gated). (gates: GATE.S8.HAVEN.*)
- EPIC.S8.LATTICE_DRONES.V0 [DONE]: Instability-phase-linked hostile Lattice drone NPCs. **T36**: Entity, spawn system, combat integration, bridge queries. (gates: GATE.S8.LATTICE_DRONES.*)
- EPIC.S8.NARRATIVE_CONTENT.V0 [DONE]: Authored story content. **T38**: FactionDialogueContentV0 (15 entries), WarfrontCommentaryContentV0 (25 entries), RevelationTextV0 (5 gold toasts). **T46**: Haven starbase logs (26 entries across 6 tiers), endgame path narratives (5 paths with epilogues + FO farewells), expanded fragment lore (bridge wiring for cover/revealed lore), faction dock greetings (25) + station descriptions (25). Content specs in docs/design/content/NarrativeContent_TBA.md + docs/design/content/LoreContent_TBA.md (gates: GATE.S8.NARRATIVE.*)
- EPIC.S8.T3_PRECURSOR_MODULES.V0 [DONE]: ~13 T3 discovery-only modules, exotic matter sustain. **T34**: 9 T3 module content definitions (weapons, defense, engines, utility), exotic matter sustain wiring via SustainInputs dict, discovery-only acquisition gate (IsDiscoveryOnly flag on ModuleDef, CanInstall tech gating), SimBridge GetT3ModuleCatalogV0. (gates: GATE.S8.T3_MODULES.*)
- EPIC.S9.MISSION_LADDER.V0 [DONE]: Missions M2-M6 delivered (Mining Survey, Ore Extraction, First Research, Research Materials, First Build, Station Expansion). Patrol and fracture mission content deferred to MISSION_FOUNDATION Phase 2+ (systemic, template, faction storylines per mission_design_v0.md). T29: content + bridge extensions (gates: GATE.S9.MISSIONS.*)
- EPIC.S9.MISSION_FOUNDATION.V0 [DONE]: Mission system Phase 1 per mission_design_v0.md — 4 new trigger types (ReputationMin, CreditsMin, TechUnlocked, TimerExpired), non-credit rewards (reputation, access, intel), mission failure/abandonment with tick deadlines, CHOICE branching steps with conditional paths, faction contract gating by reputation tier. Foundation for all future mission layers. T30: all 5 MISSION_EVOL gates + CONTRACT tests + REPUTATION.CONTRACTS (gates: GATE.S9.MISSION_EVOL.*)
- EPIC.S9.SYSTEMIC_MISSIONS.V0 [DONE]: Mission system Phase 2 per mission_design_v0.md — procedural mission generator reading world state (market shortages, warfront demand, discovery leads), station context system (dock shows situation not board). **T28**: SystemicMissionSystem (4 trigger types: SUPPLY_SHORTAGE, WAR_DEMAND, TRADE_OPPORTUNITY, ANOMALY_LEAD), SystemicOfferGen, SimBridge queries, headless proof. **T32-T33**: StationContextSystem (per-station economic context), context bridge + UI. Zero-authoring-cost missions that emerge from simulation (gates: GATE.S9.SYSTEMIC.*)
- EPIC.S9.TEMPLATE_MISSIONS.V0 [DONE]: Mission system Phase 3 per mission_design_v0.md — 76 authored mission templates with procedural variables (14 supply, 11 explore, 12 combat, 9 politics, 10 diplomacy, 10 smuggling), twist slot system (2 slots per template), reward scaling formula (BaseCredits + PerTwistBonusBps). T61: 4 diplomacy + 4 smuggling + 4 advanced twists + board filter. T62: audit confirmed archetype balance + twist + reward coverage (gates: GATE.S9.TEMPLATES.*)
- EPIC.S9.FACTION_STORYLINES.V0 [TODO]: Mission system Phase 4 per mission_design_v0.md — 5 hand-crafted faction storyline chains (8 missions each: Concord/Chitin/Valorin/Weavers/Communion), Act 1-3 progression, late-game exclusivity, cross-faction interweave points (gates: GATE.S9.FACTION_STORY.*)
- EPIC.S9.MISSION_POLISH.V0 [TODO]: Mission system Phase 5 per mission_design_v0.md — FO commentary per mission archetype per personality, mission consequence propagation (failure spawns new missions), Haven mission integration, milestone mission cinematics, knowledge graph mission integration (gates: GATE.S9.MISSION_POLISH.*)
- EPIC.S9.SETTINGS.V0 [DONE]: Full options screen — 4 tabs (Gameplay, Display, Audio, Accessibility), auto-save on change to user://settings.json, shared between main menu and pause menu. Display: resolution/display-mode/vsync/AA/quality-preset with 15s revert timer. Audio: 5 sliders mapped to AudioDesign.md bus architecture (Master/Music/SFX/Ambient/UI). Gameplay: difficulty (per-save), auto-pause, tutorial toasts, tooltip delay, language. Accessibility: colorblind modes (deut/prot/trit), high contrast, reduced shake, font size override (100-200%), HUD opacity. Reset-to-defaults per tab. Per MainMenu.md. Depends on EPIC.S7.MAIN_MENU.V0 (menu scene exists) and EPIC.S7.AUDIO_WIRING.V0 (bus architecture). GameShell-only (gates: GATE.S9.SETTINGS.*)
- EPIC.S9.MUSIC.V0 [DONE]: Dynamic music system with placeholder stems. **T46**: 4-layer stem pipeline (bass/pad/melody/percussion) with state-driven crossfade (SILENCE/EXPLORATION/COMBAT/TENSION/DOCK), placeholder sine-wave stems, combat/dock/warfront music triggers wired to game_manager. **T47**: Discovery stingers (minor/major/revelation with stem ducking), FRACTURE music state (detuned frequencies, LFO tremolo), faction ambient layer (5 characteristic drones per faction with 2s crossfade), comprehensive music_production_brief_v0.md (29-file spec for external composer). Real audio stems await external composition. (gates: GATE.S9.MUSIC.*)
- EPIC.S9.MENU_ATMOSPHERE.V0 [DONE]: Title screen atmosphere and polish — parallax starfield shader (4 GPU layers: deep stars, nebula noise x2, mid-field stars), foreground silhouette adaptive to game state (gate=no saves, player ship class=mid-campaign, Haven=completed), title treatment (clean sans-serif, fade-in, rotating Precursor fragment subtitle per session), menu audio timing (first-launch: 2s silence -> single note -> drone -> theme per AudioDesign.md silence palette; returning: quick fade-in), galaxy generation screen with thematic progress messages ("Charting the void..." / "Igniting warfronts..."), "Press any key to begin" gate after generation. Per MainMenu.md. Depends on EPIC.S7.MAIN_MENU.V0 (menu scene exists). GameShell-only (gates: GATE.S9.MENU_ATMOSPHERE.*)
- EPIC.S9.ACCESSIBILITY.V0 [DONE]: First-launch accessibility prompt (font size, colorblind mode, UI scale — shown once when no settings.json exists), colorblind post-processing shader (Deuteranopia/Protanopia/Tritanopia), font size override system (100-200% scaling), high contrast UI mode, reduced screen shake toggle. Settings integration via EPIC.S9.SETTINGS.V0 Accessibility tab. Per MainMenu.md. GameShell-only (gates: GATE.S9.ACCESSIBILITY.*)
- EPIC.S9.MILESTONES_CREDITS.V0 [DONE]: **T38**: milestone_viewer.gd (card grid + stats sidebar), GetLifetimeStatsV0, credits_scroll.gd, main menu buttons. Per MainMenu.md. GameShell-only (gates: GATE.S9.MILESTONES_CREDITS.*)
- EPIC.S9.STEAM.V0 [IN_PROGRESS]: Steamworks SDK integration. **T46**: SteamInterface stub autoload (graceful fallback when GodotSteam absent), Steam init wired to game_manager startup, rich presence (Exploring/Docked/Combat/Warping), AchievementMapper (18 milestones mapped), export_presets.cfg + Build-Release.ps1. Remaining: actual GodotSteam addon download, cloud saves, Steamworks app config. (gates: GATE.S9.STEAM.*)
- EPIC.S9.TELEMETRY.V0 [IN_PROGRESS]: Opt-in session telemetry + crash reporting. **T48**: TelemetrySystem.cs (per-100-tick economy snapshots) + crash hook in game_manager.gd. Remaining: opt-in UI, backend/local-file storage, player death/quit point tracking. (gates: GATE.S9.TELEMETRY.*)
- EPIC.S9.FLEET_TAB.V0 [DEFERRED]: Pulled forward to EPIC.S7.FLEET_TAB.V0 — fleet management is core gameplay, not polish (gates: GATE.S7.FLEET_TAB.*)
- EPIC.S9.MARKET_DEPTH.V0 [DONE]: Bid/ask spread + depth-dependent pricing — depth field, EMA smoothing, bridge queries, trade UI. T61: all gates DONE (gates: GATE.S9.MARKET_DEPTH.*)
- EPIC.S9.PROGRAM_POSTMORTEMS.V0 [DONE]: Automation failure taxonomy — 7 cause codes, fact store, bridge queries, UI display. T61: all gates DONE (gates: GATE.S9.POSTMORTEMS.*)
- EPIC.S9.L10N_DECISION.V0 [TODO]: Localization architecture decision + string audit (gates: GATE.S9.L10N.*)
- EPIC.T48.ANOMALY_CHAINS.V0 [DONE]: Multi-site escalating discovery arcs — anomaly chain engine + 6 chains (3 original + 3 T48). **T48**: CHAIN_SYSTEM + CHAIN_CONTENT. (gates: GATE.T48.ANOMALY.*)
- EPIC.T48.MAINTENANCE_TREADMILL.V0 [DONE]: Fleet upkeep drain per dynamic_tension_v0.md Pillar 2 — fuel burn, crew wages, hull degradation + upkeep bridge display. **T48**: MAINTENANCE + UPKEEP_BRIDGE. (gates: GATE.T48.TENSION.*)
- EPIC.X.LEDGER_EVENTS.V0 [DONE]: Event-sourced economic ledger — CashDelta/InventoryDelta on every change, transaction log (5000 max), ledger integrity invariant, cost-basis tracking (avg buy price, realized P/L), SimBridge queries (GetTransactionLogV0, GetCargoWithCostBasisV0). **T28**: TX_MODEL + BRIDGE + INTEGRITY. **T33**: COST_BASIS + COST_BASIS_BRIDGE. (gates: GATE.X.LEDGER_EVENTS.*)
- EPIC.T18.NARRATIVE_FOUNDATION.V0 [DONE]: Narrative layer data model — DataLog, FirstOfficer, StationMemory, WarConsequence, NarrativeNpc, KnowledgeConnection entities; Node.MutableTopology, Edge.IsMutable/MutationEpoch, Fleet.CargoOriginPhase additions; SimState dictionaries + hydration + signature; NarrativeTweaksV0, FractureWeightTweaksV0, TopologyShiftTweaksV0, RouteUncertaintyTweaksV0 constants. Phase 0 foundation for all narrative systems. Hash-affecting (gates: GATE.T18.NARRATIVE.*)
- EPIC.T18.EXPERIENTIAL_MECHANICS.V0 [DONE]: Five experiential mechanics that change how the player PLAYS in unstable space — FractureWeight (cargo weight shifts by instability phase, dynamic ratios), RouteUncertainty (ETA variance + scanner adaptation via FractureExposureJumps), StationMemory (per-station per-good delivery tracking), WarConsequence (delayed downstream feedback from war goods deliveries), TopologyShift (Phase 3+ edge mutations on arrival, connectivity-preserving). Hash-affecting (gates: GATE.T18.EXPERIENTIAL.*)
- EPIC.T18.DATA_LOG_CONTENT.V0 [DONE]: ~25 ancient scientist conversation scripts across 6 threads (Containment, Lattice, Departure, Accommodation, Warning, EconTopology) with 5 voices (Kesh, Vael, Oruth, Tal, Senn). Every log has personal texture — scientists have relationships beyond positions. Placement algorithm: fixed landmarks + type-based matching. Knowledge graph connections. Kepler narrative chain (6-piece proof-of-concept). Hash-affecting for placement (gates: GATE.T18.DATALOG.*)
- EPIC.T18.CHARACTER_SYSTEMS.V0 [DONE]: First Officer relationship arc (3 candidates: Analyst/Veteran/Pathfinder, each with blind spot and endgame lean, 5 dialogue tiers, 26 action-triggered dialogue tokens x 3 variants = 78 lines); three War Faces — Regular (silent disappearance + ghost mentions), Stationmaster (delivery-responsive dialogue), Enemy (4 recontextualization variants based on player actions). Score-based tier advancement + promotion window. FO panel with dialogue history, promotion UI, War Faces display. Hash-affecting for promotion state and NPC alive state (gates: GATE.T18.CHARACTER.*)
- EPIC.T18.NARRATIVE_BRIDGE.V0 [DONE]: SimBridge.Narrative.cs partial — FirstOfficer, DataLog, KnowledgeGraph, StationMemory, WarConsequence, NarrativeNpc, FractureWeight, RouteUncertainty, InstrumentDisagreement queries. Non-hash-affecting (gates: GATE.T18.NARRATIVE_BRIDGE.*)
- EPIC.T18.MORAL_ARCHITECTURE.V0 [DONE]: Lore document revisions — Communion flaw (species privilege, not wisdom), Reinforce reframe (responsible caution, unproven at scale), Naturalize personal cost (Stationmaster distress beacon). Updates to factions_and_lore_v0.md and NarrativeDesign.md (gates: GATE.T18.MORAL_ARCH.*)
- EPIC.T18.INSTRUMENT_DISAGREEMENT.V0 [DONE]: Dual-readout system — standard sensor vs fracture module disagree in unstable space. Neither always right: standard accurate for pricing, fracture accurate for navigation. Deterministic divergence from hash(nodeId, tick, goodId). Non-hash-affecting pure query (gates: GATE.T18.INSTRUMENT.*)
- EPIC.X.SHIP_CLASS_ENFORCEMENT.V0 [DONE]: Enforce ship class stats that are currently data-only — CargoCapacity limits (reject buy/pickup when full), Mass-based speed penalty (heavier ships slower), ScanRange-gated discovery scanning in IntelSystem (currently `_ = fleetId` discards fleet arg). Three of six ShipClassDef stats in ShipClassContentV0.cs have no enforcement code. Hash-affecting (gates: GATE.X.SHIP_CLASS_ENFORCE.*)
- EPIC.X.COVER_STORY_NAMING.V0 [DONE]: Pre-revelation naming discipline per NarrativeDesign.md — "fracture" forbidden in player-facing text before Revelation 1 ("It's Not a Drive"). Module names use cover stories ("Structural Resonance Engine" not "Fracture Drive"), HUD labels and bridge snapshots switch post-revelation. CI grep enforcement on string literals. Depends on EPIC.S8.STORY_STATE_MACHINE for revelation trigger. Non-hash-affecting GameShell + SimBridge (gates: GATE.X.COVER_STORY.*)
- EPIC.X.INSTABILITY_PRICE_WIRING.V0 [DONE]: Wire instability-aware pricing into BuyCommand/SellCommand execution path — MarketSystem.GetEffectivePrice has volatility/jitter/void-closure logic but BuyCommand calls market.GetBuyPrice directly, bypassing instability effects. Follow-up to EPIC.S7.INSTABILITY_EFFECTS.V0. Hash-affecting (gates: GATE.X.INSTABILITY_PRICE_WIRE.*)
- EPIC.X.MODULE_SUSTAIN_GOODS.V0 [DONE]: Per-module good consumption during sustain cycle — weapons consume munitions, T3 modules consume exotic matter, scanners consume energy cells. Add SustainGoodId to ModuleDef, SustainSystem deducts from fleet cargo or market. Currently only binary fuel check (FuelCurrent > 0). Follow-up to EPIC.S7.SUSTAIN_ENFORCEMENT.V0. Hash-affecting (gates: GATE.X.MODULE_SUSTAIN_GOODS.*)
- EPIC.S7.COMBAT_PHASE2.V0 [DONE]: Phase 2 combat per ship_modules_v0.md and CombatFeel.md. **Phase 2a (T31)**: SimCore heat accumulation + overheat cascade, battle stations state machine, radiator module HP + cooling bonus, SimBridge combat queries, heat gauge HUD widget. **Phase 2b (T32)**: spin turn penalty (gyroscopic precession, TurnPenaltyBpsPerRpm), mount type system (Standard/Broadside/Spinal arc restrictions on ModuleSlot), spin-fire cadence (per-mount engagement arc fractions), spinal mount fire (axis-aligned, no rotation penalty), overheat VFX (screen edge shimmer at 75%+, vent burst flash at lockout), radiator status display, zone HUD (spin RPM + radiator readouts in combat_hud.gd), headless proof (test_combat_spin_proof_v0.gd). Visual ship rotation deferred to future aesthetic epic. Hash-affecting SimCore + GameShell (gates: GATE.S7.COMBAT_PHASE2.*)
- EPIC.S8.PENTAGON_BREAK.V0 [DONE]: Pentagon dependency cascade — the game's #1 narrative moment per NarrativeDesign.md. **T38**: PentagonBreakSystem (detection + cascade), PentagonBreakTweaksV0, StoryState PentagonCascadeActive/Tick, SimBridge GetPentagonStateV0 + GetCascadeEffectsV0, GetRevelationTextV0 (R1-R5 gold toast text), galaxy map pentagon overlay (5 faction home edges, red on cascade), revelation toast delivery system (latest_revelation_id tracking). Hash-affecting (gates: GATE.S8.PENTAGON.*)
- EPIC.S8.ANCIENT_SHIP_HULLS.V0 [DONE]: Three ancient ship hulls per ship_modules_v0.md — Bastion (tank, 8 slots, 90 power, 180 hull), Seeker (explorer, 7 slots, 150 scan range, 1200 fuel), Threshold (fracture specialist, 9 slots, 85 power, 120 hull). **T34**: 3 ShipClassDef entries in ShipClassContentV0, AncientHullTweaksV0 stat constants, RestoreAncientHullV0 in SimBridge (creates fleet in hangar at Haven T3+), GetAncientHullsV0 catalog query, haven_panel.gd hull restoration UI. Pre-revelation names ("Hull Type XV-1/2/3"). Depends on EPIC.S8.HAVEN_STARBASE.V0. Hash-affecting (gates: GATE.S8.ANCIENT_HULLS.*)
- EPIC.X.PRESSURE_INJECTION_WIRING.V0 [DONE]: Wire PressureSystem.InjectDelta into gameplay systems per dynamic_tension_v0.md. **T35**: Wired 4 core systems — WarfrontDemandSystem (intensity changes), InstabilitySystem (phase transitions), MarketSystem (trade failures), SustainSystem (supply shortages). Remaining 3 (FractureSystem, SecurityLaneSystem, ReputationSystem) deferred to future gates. Hash-affecting (gates: GATE.X.PRESSURE_INJECT.*)
- EPIC.S7.TERRITORY_SHIFT.V0 [DONE]: Dynamic territory recomputation after warfront objective capture per warfront_mechanics_v0.md. **T35**: BFS territory recompute on capture (RECOMPUTE.001), tariff/embargo regime switch (REGIME_FLIP.001), galaxy map territory disc update (MAP_UPDATE.001). Hash-affecting (gates: GATE.S7.TERRITORY_SHIFT.*)
- EPIC.T18.KNOWLEDGE_GRAPH_SEEDING.V0 [DONE]: Knowledge graph connection generation pipeline per knowledge_graph_mechanics_v0.md. **T35**: Template token resolution in GalaxyGenerator Phase 9.5 (RESOLVE.001), procedural proximity + faction link connections via BFS in NarrativePlacementGen (PROXIMITY.001). Hash-affecting (gates: GATE.T18.KG_SEED.*)
- EPIC.X.FLEET_STANDING_COSTS.V0 [DONE]: Fleet upkeep / standing costs per fleet_logistics_v0.md. **T35**: Per-cycle credit drain by ship class (DRAIN.001), delinquency grace + cascading module failure (DELINQUENCY.001), SimBridge upkeep queries (BRIDGE.001). Hash-affecting (gates: GATE.X.FLEET_UPKEEP.*)
- EPIC.T57.CENTAUR_MODEL.V0 [DONE]: Kasparov's Advanced Chess for trade — FO competence tiers (Novice/Competent/Master, crisis-gated), confidence language (personality-colored: Analyst=%, Veteran=experience, Pathfinder=feelings), world adaptation (route flagging/pausing on 5 event types), boredom circuit breakers (5 FO triggers for stagnation detection). Per fo_trade_manager_v0.md Phase 1. **T57**: CompetenceTiers (FOCompetenceState + 3-tier gate checks), ConfidenceLang (route confidence 0-100, text generation), WorldAdapt (DetectWorldEvent + MapEventToAction), BoredomTriggers (5 stagnation checks + FO dialogue), SimBridge.Centaur.cs (GetFOCompetenceTierV0, GetRouteConfidenceV0, GetFOAdaptationLogV0, DemoteFOCompetenceV0). Hash-affecting (gates: GATE.T57.CENTAUR.*)
- EPIC.T57.EXPLORATION_PIPELINE.V0 [DONE]: Discovery→automation pipeline per fo_trade_manager_v0.md — EconomicIntel typed entity (5 intel types with distance-band freshness decay), DISCOVERY_OPPORTUNITY FO trigger on analyzed discoveries, margin buffer widening at decay thresholds, chain intel with FO personality commentary, discovery failure system (6 failure types via FNV1a hash, partial success path, cooldown), audio card hooks (AudioCue field on FleetEvents for discovery/scan/failure sounds). **T57**: EconomicIntel entity + GenerateEconomicIntel in DiscoveryOutcomeSystem, RollScanFailure + PickFailureType, MapAnalyzeOutcomeToAudioCue, ChainIntel FO commentary, ScannerSweepVfx (ring-expanding sweep animation), SimBridge.Discovery.cs extensions. Hash-affecting (gates: GATE.T57.PIPELINE.*, GATE.T57.FEEL.*)
- EPIC.T57.KNOWLEDGE_GRAPH_V2.V0 [DONE]: Knowledge graph evolution — link state machine (Speculative→Plausible→Confirmed→Contradicted with shared-attribute and chain-proof advancement), 3-link insight bonus (KG_INSIGHT_BONUS FO trigger), 6 player verbs (Pin/Unpin/Annotate/LinkSpeculative/FlagForFO/Compare via SimBridge), dual-mode bridge query (geographic + relational positions for all discoveries/connections/pins/annotations). **T57**: ProcessLinkStateMachine in KnowledgeGraphSystem, SimBridge.Discovery.cs KG verbs, GetKGDualModeV0 bridge query. Hash-affecting for link state machine (gates: GATE.T57.KG.*)

Note: Slice tables below are informational. Canonical Epic Bullets drive scanning and next-gate selection.

### Cross-cutting epics (apply to all slices)
These epics are always-on dependencies. Each slice should reference them as needed and must not violate them.

Epics:
- EPIC.X.DETERMINISM: Deterministic ordering rules, stable IDs, canonical serialization, replayable outcomes
- EPIC.X.TWEAKS: Versioned tweak config with canonical hashing and deterministic injection; numeric-literal routing guard to enforce “knobs not constants”
- EPIC.X.CONTENT_SUBSTRATE: Registries + schemas + authoring tools + validators for goods%recipes%modules%weapons%tech%anomalies%factions%warfronts%story beats
- EPIC.X.WORLDGEN: Seed plumbing + deterministic world generation + invariant suites + injectors + seed explorer tooling (Civ-like procedural requirement)
- EPIC.X.UI_EXPLAINABILITY: Explain-events everywhere; UI must surface “why” (profit, loss, blocked, shifts, escalations)
- EPIC.X.PRESSURE_DOMAINS: Multi-domain pressure framework (piracy%authority%fracture threat) with shared vocabulary, forecasts, response budgets, and explainability surfaces
- EPIC.X.STORY_INTEL: Rumor%intel substrate for lore leads discovered via exploration%analysis; no quest treadmill
- EPIC.X.PLAYER_LOOP_CONTRACT: “Greatness spec” player loops and non-negotiables (see below)
- EPIC.X.CODE_HEALTH: Code health hygiene — GalaxyGenerator split continuation, StationMenu per-tab split (gates: GATE.X.HYGIENE.*)
- EPIC.X.PRESSURE_FORMALIZATION.V0: Pressure state ladder formalization — 5-state enum, direction indicators, max-one-jump enforcement, intervention budget QA metric (gates: GATE.X.PRESSURE.*)
- EPIC.X.EXPERIENCE_PROOF.V0: Automated experience validation — headless scenario playthroughs, trajectory analysis, aesthetic audit, diagnostic reports for LLM-assisted iteration (gates: GATE.X.EXP.*)
- EPIC.X.EVAL_HARDENING.V0 [DONE]: Eval framework depth & accuracy — fix 5 eval bot data contracts, expand visual sweep (12 new panel phases), harden coverage scanner + optimize scanner, stress bot idle reduction, dead bridge cleanup, pipeline automation (Run-EvalBot.ps1, Run-AuditQuick.ps1). Closes audit_8 coverage gaps (gates: GATE.T49.*)
- EPIC.X.PERF_BUDGET [DONE]: Tick budget tests across all system slices (security, exploration, warfront, endgame). Single profiling pass covering all SimCore systems, not per-slice. Replaces individual S5/S6/S7/S8 PERF_BUDGET epics (gates: GATE.X.PERF_BUDGET.*)
- EPIC.X.PERF_OPTIMIZATION.V0 [DONE]: Runtime performance optimization pass targeting P1 17fps — throttle LatticeDroneCombatSystem (3-5 tick cadence), NPC Node3D replacement (drop physics solver), camera ref caching + LOD throttle, NpcTradeSystem BFS cache, IntelSystem/FirstOfficerSystem allocation reduction, reputation query TTL cache, tick budget regression tests, FPS headless proof (gates: GATE.T50.PERF.*)
- EPIC.X.GALAXY_MAP_VISUAL.V0 [DONE]: Galaxy map visual richness — node variety by station tier/industry, faction territory color overlay, economic indicators (price differentials, trade volume). Fixes ACTIVE_ISSUES F10. Non-hash-affecting GameShell (gates: GATE.T50.VISUAL.GALAXY_*)
- EPIC.X.STATION_IDENTITY.V0 [DONE]: Station visual variety. **T46**: StationIdentity class (faction color tinting with 9 faction colors, tier classification outpost/hub/capital), detail meshes (antenna arrays for hubs, orbital rings for capitals), integrated into galaxy_spawner.gd via bridge queries (faction/industry/market data), per-faction dock greetings + station descriptions. Fixes ACTIVE_ISSUES F6. Non-hash-affecting GameShell (gates: GATE.X.STATION_IDENTITY.*)
- EPIC.T44.ECONOMY_VISUALS.V0 [DONE]: Ambient economic life visuals per economy_simulation_v0.md Phase 3. **T47**: Station traffic shuttles (orbiting BoxMesh, count from NpcTradeActivity), mining extraction beams (CylinderMesh to asteroids, pulsing emission), station prosperity lighting tiers (emission multiplier by market breadth, golden orb for capitals), lane traffic sprites (PrismMesh lerping along intra-region lanes), market alert toasts (colored by type: stockout/shortage/spike/drop, station display names), economy snapshot dock panel (per-good supply rows with role tags + trend arrows), megaproject galaxy map markers (type-specific meshes: hex/diamond/cone), construction VFX (rotating spars + blinking sparks). All non-hash-affecting GameShell/bridge. (gates: GATE.T44.AMBIENT.*, GATE.T44.DIGEST.*, GATE.T44.SIGNAL.*)
- EPIC.X.LEDGER_EVENTS.V0 [DONE]: Event-sourced economic ledger — CashDelta and InventoryDelta events on every credit/goods change with ActorId, Category, ReasonCode; ledger integrity invariant (sum of CashDeltas = wallet delta); cost-basis tracking; enables automation postmortems and "where did the money go?" reconstruction (gates: GATE.X.LEDGER_EVENTS.*)
- EPIC.X.SHIP_CLASS_ENFORCEMENT.V0 [DONE]: Enforce ship class stats that are currently data-only — CargoCapacity limits, Mass-based speed penalty, ScanRange-gated discovery scanning. Three of six ShipClassDef stats in ShipClassContentV0.cs have no enforcement code. Hash-affecting (gates: GATE.X.SHIP_CLASS_ENFORCE.*)
- EPIC.X.COVER_STORY_NAMING.V0 [DONE]: Pre-revelation naming discipline per NarrativeDesign.md — "fracture" forbidden in player-facing text before Revelation 1. Cover names for modules/systems, CI grep enforcement. Depends on EPIC.S8.STORY_STATE_MACHINE. Non-hash-affecting (gates: GATE.X.COVER_STORY.*)
- EPIC.X.INSTABILITY_PRICE_WIRING.V0 [DONE]: Wire instability-aware pricing into BuyCommand/SellCommand — currently bypasses MarketSystem.GetEffectivePrice. Follow-up to EPIC.S7.INSTABILITY_EFFECTS.V0. Hash-affecting (gates: GATE.X.INSTABILITY_PRICE_WIRE.*)
- EPIC.X.MODULE_SUSTAIN_GOODS.V0 [DONE]: Per-module good consumption during sustain cycle — SustainGoodId on ModuleDef, deduction from fleet cargo. Follow-up to EPIC.S7.SUSTAIN_ENFORCEMENT.V0. Hash-affecting (gates: GATE.X.MODULE_SUSTAIN_GOODS.*)
- EPIC.X.PRESSURE_INJECTION_WIRING.V0 [DONE]: Wire PressureSystem.InjectDelta into 4 core gameplay systems (T35). Hash-affecting (gates: GATE.X.PRESSURE_INJECT.*)
- EPIC.X.FLEET_STANDING_COSTS.V0 [DONE]: Fleet upkeep / standing costs (T35). Hash-affecting (gates: GATE.X.FLEET_UPKEEP.*)
- EPIC.X.MARKET_PRICING_WIRING.V0 [DONE]: Wire dormant pricing pipeline steps into BuyCommand/SellCommand per market_pricing_v0.md. **T35**: Transaction fees wired (FEE_WIRE.001), reputation pricing wired (REP_WIRE.001), price breakdown bridge + tooltip UI (BREAKDOWN_BRIDGE.001, BREAKDOWN_UI.001). Hash-affecting (gates: GATE.X.MARKET_PRICING.*)
- EPIC.S7.COMBAT_DEPTH_V2.V0 [DONE]: Combat tactical depth layer 2 per combat_mechanics_v0.md — pre-combat outcome projection (estimated win % + hull damage before engaging, Starsector pattern), tracking/evasion by weapon size (weapon TrackingBps vs ship EvasionBps, deterministic hit probability via FNV1a64), Fore zone soft-kill (weapons offline when Fore zone HP depleted — Aft→radiator already done), damage variance (±20% per hit via deterministic hash, makes near-equal fights unpredictable), armor penetration stat (WeaponInfo.ArmorPenetrationPct bypasses zone armor %, creates armor-stripper weapon role). Extends StrategicResolverV0 + CombatSystem. **T36**: all 6 gates DONE. Hash-affecting (gates: GATE.S7.COMBAT_DEPTH2.*)
- EPIC.S7.FACTION_COMMISSION.V0 [DONE]: Faction commission system + reputation depth per reputation_factions_v0.md. **T35**: Commission entity + passive rep + stipend (ENTITY.001), infamy accumulator caps max rep tier (INFAMY.001), commission bridge + modifier stack (BRIDGE.001). Hash-affecting (gates: GATE.S7.FACTION_COMMISSION.*)
- EPIC.S1.CAMERA_POLISH.V0: Camera presentation polish — Phantom Camera addon, follow modes, combat shake (gates: GATE.S1.CAMERA.*)
- EPIC.S1.SPATIAL_AUDIO_DEPTH.V0: Spatial audio depth — engine thrust, combat SFX, ambient audio with 3D falloff (gates: GATE.S1.SPATIAL_AUDIO.*)
- EPIC.T63.FIRST_HOUR_DEPTH.V0 [TODO]: First-hour depth pass driven by fh_5 audit (32 issues, 5 critical): pacing injection (reward desert + late dead zone), Power moment surfacing (refit tab badge + FO prompt), spatial clarity (camera tune + heading indicator + lane labels), juice feedback (damage flash + warp VFX + credit roll), progressive disclosure (station section gating), economy tension (route decay), combat pacing (early pirate + loot guarantee), FO dock greeting. Also advances LOSS_RECOVERY, HAVEN_STARBASE, NARRATIVE_CONTENT (gates: GATE.T63.*)

Greatness spec (non-negotiables, enforced by gates over time):
- Primary loop: explore -> find cool things -> convert to empire leverage -> explore further -> pursue win scenarios
- Every major failure has an explainable cause chain surfaced in UI
- Every seed guarantees early viability (>= 3 viable early trade loops) and reachability (paths exist to industry, exploration, factions, endgame)
- Exploration-first momentum: most player travel is frontier expansion or intentional hub returns (repair%refit%research%upgrade), not logistics babysitting
- Pressure is multi-domain and attributable: profit builds piracy pressure; fracture use builds existential pressure; responses cite reasons and surface mitigations
- Lore provides forward pull: at least 1 lore lead is discovered through exploration%analysis in the early game and expands reach or options
- No rescue treadmill: automation disruptions default to remote policy resolution; travel-required stabilization is disallowed as mandatory progression; optional travel requires upside (unique unlock, permanent leverage, major payout, frontier access)
- Every major discovery introduces a new strategic option (unlock contracts), not only numeric upgrades
- Discoveries must unlock economic leverage via explicit unlock contracts (permits, brokers, recipes, site blueprints, corridor access, sensor layers)
- Fracture is a high-cost leverage tool, not a bulk replacement for lanes; lanes remain the empire backbone
- Warfront outcomes persist and reshape lane regimes, not just prices
- Automation feels competent: predictable, diagnosable, tunable (not dice)
- Explainability always maps to player actions (intervention verbs)
- Procedural worlds feel distinct by class (not just cosmetic differences)

Status: IN_PROGRESS (ALWAYS_ON discipline; do not mark DONE)

---

### Contracts (LOCKED, apply to all slices)
These are binding architectural rules. If any slice requires breaking a contract, update this section first and record the rationale in the session log.

#### CONTRACT.X.API_BOUNDARIES
Rules:
- SimCore owns truth. GameShell owns presentation. No exceptions.
- GameShell must not read SimCore entity graphs directly for UI. All UI reads go through SimBridge query contracts.
- SimCore must not depend on Godot types or UI constructs.
- SimCore exposes:
  - Facts: stable state snapshots intended for UI consumption (query outputs)
  - Events: explainable change narratives with cause chains (event stream)
- GameShell responsibilities:
  - Build view models from Facts%Events
  - Layout, navigation, input binding, rendering, audio, camera, moment-to-moment feel
- Adapters are the only allowed crossing point (SimBridge and other explicitly named adapter layers).

Acceptance proof:
- A deterministic guard exists ensuring UI code cannot access SimCore entity graphs directly (static scan enforced in tests; see GATE.X.API_BOUNDARIES.GUARD.001)
- All UI-facing reads are traceable to a SimBridge query contract

#### CONTRACT.X.EVENT_TAXONOMY
Events are the language of explainability. Keep the vocabulary small and stable.

Event categories (stable):
- EVT.MARKET.* (price update, shortage, surplus, trade execution)
- EVT.TRAVEL.* (route planned, lane blocked, delay incurred, arrival)
- EVT.PROGRAM.* (quote issued, job created, job failed, job completed)
- EVT.LOGISTICS.* (shipment created, shipment stalled, buffer underflow, buffer overflow)
- EVT.SECURITY.* (incident, interdiction, inspection, loss, salvage)
- EVT.DISCOVERY.* (anomaly found, hypothesis advanced, artifact contained, tech lead created)
- EVT.WARFRONT.* (theater created, state advanced, regime changed, objective captured)
- EVT.POLICING.* (trace threshold crossed, action taken, escalation phase change, counterplay success)
- EVT.PROJECT.* (construction started, stage advanced, blocker surfaced, stage completed)

Event schema requirements (all events):
- event_id (stable, deterministic)
- tick
- category
- subject_ids (stable IDs of primary entities)
- cause_ids (0..N references to prior events)
- summary (short)
- details (structured)
- severity (0..3)
- suggested_actions (0..N, optional, structured, see CONTRACT.X.INTERVENTION_VERBS)

Acceptance proof:
- Each new epic emits at least 1 new event that appears in an incident timeline
- Each slice has at least 1 failure mode with a traversable cause chain

#### CONTRACT.X.EXPLORATION_MOMENTUM
Purpose: Keep exploration and lore discovery as the primary player focus while automation runs the empire behind the player.

Rules:
- Default loop biases player time toward frontier exploration plus periodic intentional hub returns for repair%refit%research%upgrade
- Automation disruptions must always present at least 1 remote intervention verb that resolves or contains the problem (reroute, pay fee, throttle, insure, substitute inputs, pause, accept degraded throughput)
- Travel-required stabilization of automation is disallowed as mandatory progression
- Travel interventions are allowed when OPTIONAL and justified by upside (unique unlock, permanent leverage, major payout, frontier access)
- “Rescue freighter” interventions must not be a dominant pattern; if present, they must be forward-directed (securing a frontier corridor ahead), not backward-directed (fixing old routes)

Acceptance proof:
- A headless scenario pack run demonstrates:
  - >= 2 frontier discoveries and >= 1 hub return
  - >= 1 exploitation package deployed that continues producing value while player explores
  - >= 2 automation disruptions resolved via remote policy verbs (no mandatory travel)
- Evidence: deterministic proof report (no timestamps, stable ordering) recorded in gate evidence

#### CONTRACT.X.PRESSURE_DOMAINS
Purpose: Provide drama via multiple pressure domains (piracy%authority%fracture threat) without refactors, using shared explainability and determinism contracts.

Rules:
- Pressure is multi-domain and additive; domains have stable IDs (example: PIRACY, AUTHORITY, FRACTURE_THREAT)
- Systems may emit PressureDelta entries with: domain_id, reason_code, magnitude, target_ref, source_ref; reason_code must map to explainable UI text
- Domain state exposes: tier (Strained%Unstable%Critical%Collapsed) and direction (Improving%Stable%Worsening), plus a forecast as trajectory (no exact timers)
- Domain responses are selected deterministically from eligible candidates; response budgets prevent cascades and death spirals
- Every pressure incident must surface: domain, top reasons, and >= 2 mitigations mapped to intervention verbs (policy changes or actions)
- Domains may be dormant early; framework and surfaces must exist early so later domains are additive

Acceptance proof:
- Headless scenario pack run triggers >= 1 piracy pressure incident tied to profitable automation and surfaces a stable cause chain plus mitigation suggestions
- Evidence: deterministic proof report (no timestamps, stable ordering) recorded in gate evidence

#### CONTRACT.X.UI_INFORMATION_ARCHITECTURE
This is about data organization, not layout. Pages are stable even if UI layout changes.

Canonical pages (stable):
- Bridge (Throneroom): empire posture, tech posture, faction posture, warfront posture, trace posture, megaproject posture
- Station: market, inventory, services, contracts, local intel
- Fleet: fleet state, route, cargo, role%doctrine, incidents
- Programs: program list, quotes, execution timeline, outcomes, explain failures
- Logistics: shipments, buffers, bottlenecks, capacity, flow summaries
- Discoveries: anomaly catalog, hypotheses, artifacts, tech leads, expeditions
- Warfronts: theaters, supply needs, projected outcomes, regime map
- Policing%Trace: trace meters, escalation phase, actions, counterplay options
- Projects: construction pipelines, megaproject pipelines, blockers, time-to-capability

View model rule:
- Each page consumes only:
  - Facts returned from a SimBridge query (schema versioned)
  - Events from the event stream (schema versioned)
- No page may depend on internal SimCore classes directly.

Acceptance proof:
- Each new slice adds or extends at least 1 page schema and includes a minimal UI readout using Facts%Events
- A page-level schema exists for each canonical page (even if minimal)

#### CONTRACT.X.SLICE_COMPLETION
A slice is not DONE without player-facing proof and regression protection.

DONE requires:
- 1 forced playable path (mission or guided flow) that uses the new capability end-to-end
- 1 explainability path that answers “why” for a representative failure mode
- Regression suite updates:
- Golden replay updated or expanded
- If worldgen is touched: N-seed invariant suite passes (N = 100)
- If content schemas change: compatibility checks pass (old packs load or fail with explicit reasons)
- Evidence recorded in docs (gates moved, tests listed, artifacts referenced)

Also required:
- If UI is touched: GameShell smoke test passes (load minimal scene, run N ticks, exit; see GATE.X.GAMESHELL.SMOKE.001)
- If save or world identity is touched: save/load identity regression passes (see CONTRACT.X.SAVE_IDENTITY)

#### CONTRACT.X.INTERFACE_FREEZE_MILESTONES
Purpose: Preserve late-stage flexibility by stabilizing the right interfaces early.

Rules:
- Before Slice 3 starts: schemas and events may change, but must remain deterministic and versioned.
- After Slice 3 is marked DONE:
  - Additive-only for: Event categories, registry IDs, and page schemas for Bridge, Station, Fleet, Programs, Logistics
  - Breaking changes require: major version bump, explicit migration story, and session log entry
- After Slice 6 is marked DONE:
  - Additive-only for: Discoveries schemas (Discoveries page, anomaly families, artifact lead shapes)
  - Breaking changes require the same major version process
- After Slice 7 is marked DONE:
  - Additive-only for: Warfront and Territory regime schemas (Warfronts page, regime change events)
  - Breaking changes require the same major version process

Acceptance proof:
- A compatibility test exists for schema version loading and clear failure messages

#### CONTRACT.X.PACING_CONSTANTS
Purpose: Prevent late-stage rework by keeping time scales within designed ranges.

Rules:
- Declare pacing constants as ranges, not exact numbers, until Slice 9 lock.
- Each slice that changes pacing must update these ranges and add a regression test that asserts values remain within range.

Initial pacing ranges [TBD, tune later]:
- Early manual trade loop: 5 to 15 minutes per meaningful run
- Late automated trade cycle: 1 to 5 minutes per cycle per program (player review cadence)
- Warfront meaningful shift: 30 to 120 minutes of play
- Policing escalation phase: hours of play, not minutes
- Megaproject stage: 1 to 3 hours of play per stage (multiple stages per project)

Acceptance proof:
- At least 1 scenario sim test asserts that measured cycle times fall within current ranges [OPEN: measurement method]

#### CONTRACT.X.PERF_BUDGETS
Purpose: Prevent sim complexity from exploding unnoticed.

Rules:
- Each slice that adds simulation complexity must add or extend at least 1 performance test:
  - Either a tick-cost budget assertion, or a micro-benchmark for the new system
- Budgets start loose, tighten over time. Slice 9 locks final budgets.

Acceptance proof:
- A perf regression test exists and runs in CI-like local workflow (see GATE.S3.PERF_BUDGET.001)

#### CONTRACT.X.SAVE_IDENTITY
Purpose: Ensure procedural worlds are stable and replayable, and prevent save from becoming a Slice 9 surprise.

Rules:
- From Slice 2.5 onward, save must preserve world identity:
  - Save includes the seed and all generation-relevant parameters (world class, injectors config versions)
  - Load must reproduce the exact same world identity and determinism hashes
- Save schemas are versioned; breaking changes require migration story (even if minimal).
- Save/load hash equivalence is required for the deterministic subset of state.

Acceptance proof:
- A save/load identity regression test exists:
  - generate world with seed S
  - save
  - load
  - world hash matches and core invariants still hold

#### CONTRACT.X.DIFFICULTY_CURVES
Purpose: Difficulty is systemic and procedural, not enemy HP.

Rules:
- Difficulty modifies curves and budgets, not fundamental rules.
- Pressure sources (all must have tunable curves):
  - Security risk (incidents%loss rate)
  - Warfront intensity (demand and territorial volatility)
  - Policing escalation (trace thresholds and response rate)
  - Economic volatility (price dispersion and shock frequency)
- World class defines baseline curves; difficulty shifts within bounded ranges.

Acceptance proof:
- Seed-suite produces a “pressure profile report” per world class and per difficulty [OPEN: report format]
- No difficulty setting yields dead-on-arrival seeds (see CONTRACT.X.ONBOARDING_INVARIANTS)

#### CONTRACT.X.ONBOARDING_INVARIANTS
Purpose: Every seed must be playable and teach the loops.

Rules (must hold for all generated worlds):
- Starter region includes:
  - 1 stable hub station with basic services
  - >= 3 viable early trade loops within the starter region graph
  - At most 1 high-risk chokepoint on required M1 routes
  - Availability of required starter goods and basic ship loadout support
  - At least 1 accessible path to a starter freighter hull (purchase within first-hour economics or scripted acquisition)
  - At least 1 mineable site that can be deployed as a ResourceTap and serviced by a starter freighter
  - At least 1 early tech lead that can be completed via research%refit and results in a tangible capability upgrade
  - At least 1 lore lead (rumor thread, log, transmission) discoverable via exploration%analysis that points forward
- Early threats exist but are legible and avoid unavoidable early losses.

Acceptance proof:
- N-seed invariant suite includes onboarding checks and reports failures with a minimizable seed repro
- Golden path check: a scenario pack can complete first-hour beats (dock, trade loop, freighter automation, resource tap, 1 research unlock, 1 lore lead) without requiring rescue travel

#### CONTRACT.X.ANTI_EXPLOIT
Purpose: Prevent trivial money printers while preserving satisfying arbitrage and scaling.

Rules:
- Any profitable loop at scale must be bounded by at least 2 friction sources from:
  - transport time, lane slots, inspections, spoilage, tariffs, information staleness, risk, capital lockup
- The game must preserve “small profitable runs” early and “portfolio management” late without a single dominant loop.
- Fixes should prefer adding friction and counterplay, not nerfing rewards into boredom.

Acceptance proof:
- Balance harness includes an “exploit sweep” scenario suite [OPEN: suite definition]
- No scenario shows unbounded growth without binding constraints

#### CONTRACT.X.MOD_SAFETY
Purpose: Keep determinism and stability while allowing content extensibility.

Rules:
- Content packs are data-only. No code execution in packs.
- Packs cannot override base registry IDs unless explicitly declared as total conversion [OPEN: whether total conversions are in scope]
- Packs must declare compatibility range for schema versions.
- Validators must reject unsafe or inconsistent packs with explicit errors.

Acceptance proof:
- Pack validator tests exist (good pack loads, bad pack rejects with clear message)

#### CONTRACT.X.GAMESHELL_SMOKE_TESTS
Purpose: Prevent GameShell drift while SimCore evolves.

Rules:
- Any slice that touches UI must include at least 1 headless GameShell smoke test:
  - load minimal scene
  - bind SimBridge
  - run N ticks
  - exit cleanly
- Smoke tests must be deterministic and run in the standard local workflow.

Acceptance proof:
- Smoke test runs in CI-like local script and is referenced in evidence

---

### Content waves (REQUIRED, keeps progress fun and validates systems)
Purpose: Prevent “infrastructure-only” slices and ensure player-facing proof exists continuously.

Rule:
- Each slice that introduces a new system must ship a minimal content wave that exercises it end-to-end.

Wave requirements (minimums, numbers are [TBD] but the existence is mandatory):
- Slice 1: Starter goods (>= 10), 2 stations, 1 lane, 1 basic ship loadout, 1 simple contract flow (M1)
- Slice 2: 1 program type (TradeProgram), 1 doctrine stub, 1 quote flow, 1 failure reason surfaced
- Slice 2.5: >= 3 world classes (CORE, FRONTIER, RIM), 3 to 5 factions, seed suite produces distinct early loops and onboarding validity
- Slice 3: 3 fleet roles (trader%hauler%patrol), multi-route choices, 1 congestion scenario, 1 bottleneck fix visible in UI, headless playable trade loop proof (incl save%load)
- Slice 3.6: >= 2 discovery families, >= 1 unlock (Broker or Permit), acquire 1 starter freighter and deploy >= 1 exploitation package (TradeCharter or ResourceTap), establish 1 mineable site via ResourceTap, complete 1 research unlock that changes capability, surface 1 lore lead (rumor thread), experience >= 1 legible piracy pressure incident, resolve >= 2 automation exceptions remotely, evidence: deterministic exploration momentum proof report (no timestamps, stable ordering)
- Slice 4: Starter Catalog v0 shipped via content packs (goods%recipes%modules%weapons) plus:
  - 1 reverse-engineer chain (lead -> prototype -> manufacturable unlock)
  - 1 named manufacturing chain v0 (example: ORE -> INGOT -> HULL_PLATING)
  - 1 named combat supply chain v0 (example: CHEMICALS + METALS -> MISSILE_AMMO or REPAIR_KIT)
  - 1 refit kit pipeline surface (even if time costs are [TBD] in v0)
- Slice 5: Local combat v0 is playable (turrets + missiles + shields%hull) plus:
  - 1 counter family v0 (point defense or ECM, pick 1)
  - 1 escort doctrine (policy-driven)
  - 1 strategic resolver scenario (deterministic)
  - 1 deterministic combat replay proof (same input stream => identical end state)
- Slice 6: >= 5 anomaly families, 1 extinct-tech lead family, 1 containment failure mode with counterplay, 1 layered reveal tech that changes interpretation of an existing discovery
- Slice 7: >= 2 warfront theater types, 1 territory regime flip, 1 faction-unique tech gate
- Slice 8: >= 2 policing phases, >= 1 megaproject chain, >= 2 win scenarios wired into state machine
- Slice 9: final content expansion within locked constraints + balance targets

---

### Epic and gate template (REQUIRED for all new work)

#### Epic bullet format v1 (REQUIRED)
Every epic line must be machine-scannable and gate-derived.

- Format:
  - EPIC: `- EPIC.<...> [TODO|IN_PROGRESS|DONE]: <description> (gates: <selector or list>)`

- Examples:
  - `- EPIC.S2_5.SEEDS [DONE]: Seed plumbing everywhere (world, save/load, tests, tools) (gates: GATE.S2_5.SEEDS.*)`
  - `- EPIC.S3.LOGI.ROUTES [DONE]: Route planning + explainability (gates: GATE.ROUTE.*, GATE.S3.ROUTES.*)`
  - `- EPIC.X.CONTENT_SUBSTRATE [TODO]: Content substrate v0 (gates: GATE.X.CONTENT_SUBSTRATE.*)`

- Status computation (no exceptions if a gates selector exists):
  - DONE: all matched gates are DONE
  - IN_PROGRESS: some matched gates are DONE or IN_PROGRESS, but not all DONE
  - TODO: all matched gates are TODO
  - If no `(gates: ...)` is present, the epic cannot be marked DONE.

- OPEN items rule:
  - Anything that blocks DONE must be represented by a gate and included in `(gates: ...)`.
  - Otherwise it must be moved to a different epic or explicitly declared non-blocking.

#### Gate naming
- Preferred: `GATE.<slice_or_domain>.<topic>.<NNN>` where NNN is 001, 002, ...
- Rule: a gate must belong to at least 1 epic via an epic `(gates: ...)` selector.
  - If you create a new gate prefix, also add or update the owning epic selector.

#### Every gate must include (minimum metadata)
- Scope: smallest meaningful vertical slice
- Files: expected touched paths
- Tests: at least 1 new or expanded test
- Evidence: objective completion proof (test filter, artifact path, deterministic transcript, screenshot if applicable)
- Determinism notes: ordering%IDs%serialization%tie-break rules
- Failure mode: 1 explicit failure and how it is exposed (Facts%Events or scan output)
- Intervention verbs: what the player can do about it (see CONTRACT.X.INTERVENTION_VERBS)

#### Standard 5-gate decomposition (default)
1) CONTRACT gate
   - Schema/query contract and event types
2) CORE LOGIC gate
   - Minimal SimCore behavior
3) DETERMINISM gate
   - Stable IDs%tie-breaks%serialization%golden replay coverage
4) UI gate
   - Minimal readout using Facts%Events (no layout polish)
5) EXPLAIN gate
   - Cause chain + suggested actions surfaced for the failure mode

If any gate exceeds caps, split it.

#### Caps (hard limits for a single gate)
- Net change <= 500 lines (measured by `git diff --stat`)
- New tests <= 3
- New schemas <= 1 version bump
- New content packs <= 1 starter or incremental pack

#### Acceptance proof for a gate
A gate is DONE only if all are true:
- Proof command passes
  - `dotnet test` passes (filtered ok for the gate, but the final gate closing an epic requires full suite)
- Gate ledger updated
  - `55_GATES.md` row set to DONE with proof command and evidence paths
- Session log appended
  - `56_SESSION_LOG.md` includes a PASS entry for the gate
- Epic status stays consistent
  - Epic `(gates: ...)` selector would compute DONE/IN_PROGRESS/TODO matching the epic marker
- Connectivity violations remain empty for slice scope

#### CONTRACT.X.INTERVENTION_VERBS (binding list, extend additively)
Purpose: Explainability must always connect to player agency.

Rules:
- Each explain chain must map to 1 to 3 intervention verbs that are available on a relevant canonical page.
- Verbs are coarse, policy-driven, and program-centric (no per-ship micromanagement leaks).

Initial verb set (extend additively):
- Programs: raise budget, lower budget, pause program, resume program, change doctrine toggle, change risk tolerance, change route preference
- Logistics: reprioritize shipment class, allocate capacity to route, reroute around chokepoint, schedule convoy window
- Industry: queue build stage, queue refit, build depot, build shipyard stage, allocate science throughput
- Discoveries: run expedition, escalate containment, run analysis step, defer exploitation, mark as hazard
- Warfronts: commit supply package, commit escort package, change faction alignment stance, negotiate access
- Policing: run counterplay action, reduce fracture usage policy, deploy scrubber project stage, misdirect [OPEN: in-scope set]

Acceptance proof:
- For each slice, at least 1 failure mode presents a suggested action that is executable via a UI control

---

### Slice 0: Repo + determinism foundation (pre-slice, always on)
Purpose: Make LLM-driven development safe, deterministic, and boundary-respecting.

Epics:
- EPIC.S0.TOOLING: DevTool commands for repeatable workflows (packets, scans, test runs)
- EPIC.S0.DETERMINISM: Golden hashes, replay harness, stable world hashing
- EPIC.S0.CONNECTIVITY: Connectivity scanner and zero-violation policy for Slice scope
- EPIC.S0.QUALITY: Minimal CI-like local scripts (format, build, tests)
- EPIC.S0.EVIDENCE: Context packet must include or reference latest scan + test + hash artifacts
- EPIC.S0.REPO_HEALTH: One-command repo health runner enforcing generated hygiene, forbidden artifact policy, LLM size budgets, and connectivity delta discipline

Exit criteria for DONE:
- Context packet reliably surfaces scan + test + determinism evidence, or explicitly reports why missing
- Connectivity violations remain empty for current slice scope
- Golden replay + long-run + save/load determinism regressions are stable

Status: IN_PROGRESS (ALWAYS_ON discipline; do not mark DONE. New invariants and boundaries will continue to be added over time.)

---

### Slice 1 (LOCKED): Logistics + Market + Intel micro-world
Purpose: Prove the core economic simulation loop in a tiny world, deterministically, with minimal UI.

Gates: see docs/55_GATES.md (source of truth)
Status: DONE

### Slice 1 — Physical World Epics (post-lock additions, S1-prefixed IDs)
Purpose: Build the spatial world the player inhabits. Not part of the original Slice 1 scope lock but tracked here because the epic IDs carry S1 prefix. These are next-up after Slice 3.6 DONE and are prerequisites for Slice 5 combat.

Dependency order:
- EPIC.S1.HERO_SHIP_LOOP.V0 first (physics world, dock, transit)
- EPIC.S1.GALAXY_MAP_PROTO.V0 second (depends on SimCore data from S3.6 discovery + RumorLead, which is now DONE)
- Both must be DONE before EPIC.S5.COMBAT_LOCAL starts

Epics:
- EPIC.S1.HERO_SHIP_LOOP.V0 [DONE]: see canonical epic bullets above (gates: GATE.S1.HERO_SHIP_LOOP.*)
- EPIC.S1.GALAXY_MAP_PROTO.V0 [DONE]: see canonical epic bullets above (gates: GATE.S1.GALAXY_MAP.*)
- EPIC.S1.DISCOVERY_INTERACT.V0 [DONE]: Discovery site dock interaction — minimal panel wired to SimBridge discovery queries (gates: GATE.S1.DISCOVERY_INTERACT.*)
- EPIC.S1.MISSION_RUNNER.V0 [DONE]: Deterministic mission runner v0 — mission schema, headless executor, Mission 1 "Matched Luggage" proof, tutorial determinism clamp (gates: GATE.S1.MISSION.*)
- EPIC.S1.FLEET_VISUAL.V0 [DONE]: Fleet ship substantiation — Kenney Space Kit craft models by FleetRole, galaxy map + local view (gates: GATE.S1.FLEET_VISUAL.*)

Status: IN_PROGRESS

---

### Slice 1.5 (LOCKED): Tech sustainment via supply chain
Purpose: Prove “industry enablement depends on supply” with clear failure modes and UI.

Gates: see docs/55_GATES.md (source of truth)
Status: DONE

---

### Slice 2: Programs as the primary player control surface
Purpose: Shift player power from manual micromanagement to programs, quotes, doctrines.

v1 scope (LOCK ONCE SLICE 2 STARTS):
- One program type: TradeProgram only
- One fleet binding: single trader fleet only
- One doctrine: DefaultDoctrine only (max 2 toggles if needed)
- No mining, patrol, construction, staffing, or multi-route automation in Slice 2

Epics:
- EPIC.S2.PROG.MODEL [DONE]: Program, Fleet, Doctrine core models align to docs/53 (gates: GATE.PROG.001, GATE.FLEET.001, GATE.DOCTRINE.001)
- EPIC.S2.PROG.QUOTE [DONE]: Liaison Quote flow for “do X”, cost, time, risks, constraints (gates: GATE.QUOTE.001)
- EPIC.S2.PROG.EXEC [DONE]: Program execution pipeline (intent-driven, deterministic) (gates: GATE.PROG.EXEC.001, GATE.PROG.EXEC.002)
- EPIC.S2.PROG.UI [DONE]: Control surface UI for creating programs and reading outcomes (gates: GATE.UI.PROG.001, GATE.VIEW.001)
- EPIC.S2.PROG.SAFETY [DONE]: Guardrails against direct state mutation, only intents (gates: GATE.BRIDGE.PROG.001, GATE.PROG.EXEC.001)
- EPIC.S2.EXPLAIN [DONE]: Schema-bound “Explain” events for program outcomes and constraints (gates: GATE.EXPLAIN.001)

Status: DONE

---

### Slice 2.5: Worldgen foundations (Civ-like procedural requirement)
Purpose: Procedural galaxy%economy%factions become real and testable, not just anomalies.

Epics:
- EPIC.S2_5.SEEDS [DONE]: Seed plumbing everywhere (world, save/load, tests, tools) (gates: GATE.S2_5.SEEDS.*)
- EPIC.S2_5.WGEN.GALAXY.V0 [DONE]: Topology, lanes, chokepoints, capacities, regimes; starter safe region (gates: GATE.S2_5.WGEN.GALAXY.001)
- EPIC.S2_5.WGEN.ECON.V0 [DONE]: Role distribution, recipe placement, demand sinks, initial inventories; early loop guarantees (gates: GATE.S2_5.WGEN.ECON.001)
- EPIC.S2_5.WGEN.DISCOVERY_SEEDING.V0 [DONE]: Deterministic seeding of anomaly families, corridor traces, and resource pool markers; guarantees at least 1 frontier discovery chain and 1 monetizable resource opportunity per seed class (CORE%FRONTIER%RIM) (gates: GATE.S2_5.WGEN.DISCOVERY_SEEDING.*)
- EPIC.S2_5.WGEN.FACTION.V0 [DONE]: 3 to 5 factions, home regions, doctrines, initial relations (gates: GATE.S2_5.WGEN.FACTION.001)
- EPIC.S2_5.WGEN.WORLD_CLASSES.V0 [DONE]: World classes v0 implemented (CORE, FRONTIER, RIM) with deterministic assignment and measurable effect (fee_multiplier) (gates: GATE.S2_5.WGEN.WORLD_CLASSES.001)
- EPIC.S2_5.WGEN.INVARIANTS [DONE]: Connectivity, early viability, reachability, onboarding invariants (gates: GATE.S2_5.WGEN.INVARIANTS.001)
- EPIC.S2_5.WGEN.N_SEED_TESTS [DONE]: Distribution bounds over N seeds (v0 uses N = 100; can increase later) (gates: GATE.S2_5.WGEN.DISTRIBUTION.001, GATE.S2_5.WGEN.NSEED.001)
- EPIC.S2_5.WGEN.DISTINCTNESS.REPORT.V0 [DONE]: Deterministic seed-suite stats report for class differences using worldgen-only signals (gates: GATE.S2_5.WGEN.DISTINCTNESS.REPORT.*)
- EPIC.S2_5.WGEN.DISTINCTNESS.TARGETS.V0 [DONE]: Enforce class separation targets using report metrics; violations list seeds + deltas sorted; exits nonzero on failure (gates: GATE.S2_5.WGEN.DISTINCTNESS.TARGETS.*)
- EPIC.S2_5.SAVE_IDENTITY [DONE]: Save seed%params, load exact identity, hash equivalence regression (gates: GATE.S2_5.SAVELOAD.WORLDGEN.001)

Status: DONE

---

### Slice 3: Fleet automation and logistics scaling
Purpose: Multi-route trade, hauling, and supply operations at scale without micromanagement.

Epics:
- EPIC.S3.LOGI.ROUTES [DONE]: Route planning primitives (multi-candidate, stable tie-breaks) (gates: GATE.ROUTE.001, GATE.S3.ROUTES.001)
- EPIC.S3.LOGI.EXEC [DONE]: Logistics job model and execution pipeline (cargo, xfer, reserve, fulfill, cancel, determinism, save%load) (gates: GATE.LOGI.*, GATE.FLEET.ROUTE.001)
- EPIC.S3.FLEET_ROLES [DONE]: Fleet roles and constraints (trader, hauler, patrol) that deterministically influence route-choice selection (gates: GATE.S3.FLEET.ROLES.001)
- EPIC.S3.MARKET_ARB [DONE]: Automation that exploits spreads but is not money-printing (anti-exploit constraints enforced) (gates: GATE.S3.MARKET_ARB.001)
- EPIC.S3.RISK_SINKS.V0 [DONE]: Predictable risk frictions for automation (delays%losses%insurance-like sinks) without requiring Slice 5 combat (gates: GATE.S3.RISK_SINKS.*)
- EPIC.S3.CAPACITY_SCARCITY [DONE]: Lane slot scarcity model (queueing v0) (gates: GATE.S3.CAPACITY_SCARCITY.001)
- EPIC.S3.UI_DASH [DONE]: Dashboards for flows, margins, bottlenecks, intel quality (gates: GATE.S3.UI.DASH.001)
- EPIC.S3.UI_LOGISTICS [DONE]: Logistics UI readout and incident timeline (Facts%Events, deterministic ordering) (gates: GATE.UI.LOGISTICS.001, GATE.UI.LOGISTICS.EVENT.001)
- EPIC.S3.UI_FLEET [DONE]: Fleet UI playability surface (select, cancel job, override, save%load visible state, deterministic event tail) (gates: GATE.UI.FLEET.*, GATE.UI.FLEET.PLAY.001)
- EPIC.S3.EXPLAINABILITY [DONE]: Explain capstone plus cross-surface “why” chains for representative failures (gates: GATE.UI.EXPLAIN.PLAY.001, GATE.UI.PROGRAMS.001, GATE.UI.PROGRAMS.EVENT.001, GATE.PROG.UI.001, GATE.UI.DOCK.NONSTATION.001)
- EPIC.S3.PERF_BUDGET [DONE]: Tick budget tests extended for logistics scaling (gates: GATE.S3.PERF_BUDGET.001)
- EPIC.S3.PLAY_LOOP_PROOF [DONE]: Headless playable trade loop proof, including deterministic save%load continuation (gates: GATE.UI.PLAY.TRADELOOP.001, GATE.UI.PLAY.TRADELOOP.SAVELOAD.001, GATE.S3.SAVELOAD.SCALING.001)

Status: DONE — All 12 sub-epics complete. Fleet automation, logistics, route planning, UI, explainability all shipped.

---

### Slice 3.5: Content substrate foundations (prereq for Slice 4+)
Purpose: Prevent hardcoded content. Establish deterministic registries%schemas%validators%minimal authoring loop.

Epics:
- EPIC.S3_5.CONTENT_PACK_CONTRACT.V0 [DONE]: Versioned registries (goods%recipes%modules) with schema validation, canonical hashing, deterministic load order (gates: GATE.X.CONTENT_SUBSTRATE.001, GATE.S3_5.CONTENT_SUBSTRATE.001)
- EPIC.S3_5.PACK_VALIDATION_REPORT.V0 [DONE]: Deterministic validation report with stable ordering and nonzero exit on invalid packs (gates: GATE.S3_5.CONTENT_SUBSTRATE.002)
- EPIC.S3_5.WORLD_BINDING.V0 [DONE]: World identity binds pack digest and persists through save%load; repro surface includes pack id%version (gates: GATE.S3_5.CONTENT_SUBSTRATE.003)
- EPIC.S3_5.HARDCODE_GUARD.V0 [DONE]: Deterministic scan or contract test flags new hardcoded content IDs in systems that must be data-driven; violations sorted and reproducible (gates: GATE.S3_5.CONTENT_SUBSTRATE.004)

Status: DONE

---

### Slice 3.6: Exploration minimum loop and exploitation templates
Purpose: Prove the core loop early: discover -> (optional hub return) -> unlock leverage -> deploy template -> keep exploring. Also prove the first-hour golden path beats (freighter%mining%tech%lore%pressure) are achievable without rescue travel.

Epics:
- EPIC.S3_6.DISCOVERY_STATE.V0 [DONE]: Minimal discovery state v0 (seen%scanned%analyzed), bookmarking, deterministic persistence and UI surfacing (gates: GATE.S3_6.DISCOVERY_STATE.*)
- EPIC.S3_6.DISCOVERY_UNLOCK_CONTRACT.V0 [DONE]: Schema-bound unlocks with stable IDs and world-binding:
  - Unlock types: Permit, Broker, Recipe, SiteBlueprint, CorridorAccess, SensorLayer
  - Each unlock declares explicit effects on: markets, authorities, programs, industry eligibility
  - Unlock acquisition verbs: scan, analyze at hub, complete expedition step, trade with contact (gates: GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.*)
- EPIC.S3_6.RUMOR_INTEL_MIN.V0 [DONE]: Rumor%Intel substrate v0 for lore leads:
  - Lore leads are discovered via exploration%expeditions%hub analysis (not scripted tutorials)
  - Each lead carries deterministic hint payload (region tags, coarse location, prerequisites, implied payoff)
  - Leads persist through save%load and surface in UI as “go look here because X”
  - No quest treadmill requirement; leads are optional but forward-directed (gates: GATE.S3_6.RUMOR_INTEL_MIN.*)
- EPIC.S3_6.EXPEDITION_PROGRAMS.V0 [DONE]: ExpeditionProgram v0 focused on discovery (survey, sample, salvage, analyze); no rescue treadmill requirement (gates: GATE.S3_6.EXPEDITION_PROGRAMS.*)
- EPIC.S3_6.UI_DISCOVERY_MIN.V0 [DONE]: Discovery UI v0 + unlock surfaces + “deploy package” controls; shows deterministic exception summaries and suggested policy actions (gates: GATE.S3_6.UI_DISCOVERY_MIN.*)
- EPIC.S3_6.EXPLOITATION_PACKAGES.V0 [DONE]: Template-driven exploitation packages deployed from unlocks and designed to run on lanes by default:
  - TradeCharter v0: buy%sell bands, stockpile targets, route constraints, risk posture
  - ResourceTap v0: extract -> refine -> export loop with buffers and substitution policies
  - Packages must support remote exception policies (pause, reroute, substitute, insure, pay fee, throttle)
  - Each package must produce a deterministic Quote summary before activation (expected profit bands, time-to-cash, primary risks, required services, and the top 3 policy levers) (gates: GATE.S3_6.EXPLOITATION_PACKAGES.*)
- EPIC.S3_6.PLAY_LOOP_PROOF.V0 [DONE]: Headless playable proof of exploration-first economy:
  - Player discovers 2 sites and docks at 1 hub station
  - Player identifies 1 viable early trade loop and acquires 1 starter freighter
  - Player assigns 1 TradeCharter and sees automation generate revenue while player explores
  - Player discovers 1 mineable site and deploys 1 ResourceTap serviced by the starter freighter
  - Player completes 1 research%refit tech unlock that changes capability (module or access)
  - Player surfaces 1 lore lead via Rumor%Intel substrate (forward-directed)
  - Player experiences >= 1 piracy pressure incident that is legible (why it happened + mitigations)
  - >= 2 disruptions resolved via remote policy verbs (no mandatory rescue travel)
  - Evidence: deterministic proof report (no timestamps, stable ordering) recorded in gate evidence (gates: GATE.S3_6.PLAY_LOOP_PROOF.*)

Status: DONE

---

### Slice 4: Industry, construction, and technology industrialization
Purpose: Convert discoveries into sustainable capability via supply-bound projects.

Dependency:
- Slice 4 requires Slice 3.5 content substrate DONE.

v0 scope (LOCK ONCE SLICE 4 STARTS):
- Starter Catalog v0 is mandatory and content-defined (packs), not code-defined:
  - Goods: must cover bulk, intermediates, manufactured, consumables, specials, contraband
  - Recipes: must include at least:
    - 1 refining chain (ORE -> INGOT)
    - 1 manufacturing chain (INGOT + INTERMEDIATE -> COMPONENT or PLATING)
    - 1 combat supply chain (inputs -> MISSILE_AMMO or REPAIR_KIT)
  - Modules: must include at least:
    - 1 shield module, 1 turret module, 1 missile module, and 1 counter module (PD or ECM)
    - 1 power module and 1 sensors module
- Module model v0 must be explicit:
  - Hero ship supports a slot model for combat-facing loadout decisions
  - Fleets remain package-driven (no per-ship fitting; capability is tags on the fleet)
- Unlock paths must exist (even if minimal): buy, research, permit, manufacture
- Evidence must be deterministic: stable ordering, stable IDs, and no timestamps in emitted reports

Epics:
- EPIC.S4.INDU_STRUCT [DONE]: Industry structure v0: bounded production chain graph that is content-ID-driven and deterministic (gates: GATE.S4.INDU_STRUCT.*)
  - Definition: a “chain” is a multi-step recipe path with explicit inputs%outputs (by stable IDs), executed over time
  - v0 bounds: max depth 3 steps per chain; max 1 byproduct per recipe; deterministic recipe ordering and tie-breaks
  - Required outputs: IndustryShortfall style explain events when blocked (missing input, storage full, no capacity, no permit)
  - Evidence: a deterministic chain report over a fixed scenario (no timestamps; stable sort order)
- EPIC.S4.CONSTR_PROG [DONE]: Construction programs (depots, shipyards, refineries, science centers) (gates: GATE.S4.CONSTR_PROG.*)
- EPIC.S4.MAINT_SUSTAIN [DONE]: Maintenance as sustained supply (no repair minigame) (gates: GATE.S4.MAINT_SUSTAIN.*)
- EPIC.S4.TECH_INDUSTRIALIZE [DONE]: Reverse engineering pipeline (lead -> prototype -> manufacturable) (gates: GATE.S4.TECH_INDUSTRIALIZE.*)
- EPIC.S4.UPGRADE_PIPELINE [DONE]: Refit kits, install queues, yard capacity, time costs (gates: GATE.S4.UPGRADE_PIPELINE.*)
- EPIC.S4.CATALOG.V0 [DONE]: Starter Catalog v0 (goods%recipes%modules%weapons) shipped as content packs with deterministic validation (gates: GATE.S4.CATALOG.*)
  - Scope: create a small but expressive authored catalog that supports:
    - >= 3 viable early trade loops (Greatness spec requirement)
    - 1 combat loop where loadout choice matters (shield%turret%missile%counter)
    - 1 sustain hook (ammo and/or repair consumes a good)
  - Required named chains (must be explicit IDs in recipes, not prose):
    - Refining chain v0: ORE -> INGOT
    - Manufacturing chain v0: INGOT + INTERMEDIATE -> COMPONENT or PLATING
    - Combat supply chain v0: inputs -> MISSILE_AMMO or REPAIR_KIT
  - Determinism requirements:
    - stable IDs for all catalog entries
    - deterministic load ordering
    - validator outputs have no timestamps and stable sorting
  - Evidence:
    - `SimCore.Tests/Content/ContentRegistryContractTests.cs` expanded to assert required IDs exist and schema invariants hold
    - Validation report remains byte-for-byte stable across reruns (path recorded in gate evidence)
  - Failure mode (must be explainable):
    - “module unavailable” or “recipe blocked” surfaces missing prerequisite (permit, research, missing intermediate)
  - Intervention verbs:
    - Discoveries: run analysis step
    - Industry: queue refit
  - Design decision (2026-03-03, GATE.S4.CATALOG.EPIC_CLOSE.001):
    - GetCatalogGoodsV0() is the correct query for catalog completeness — returns all registered good IDs from the content registry regardless of market stock.
    - GetPlayerMarketViewV0(nodeId) is a market inventory snapshot — shows only goods with keys in that node's market inventory. Do NOT use it to prove catalog completeness.
    - food is NOT seeded universally at world genesis. food is a production good requiring agricultural node profiles. Seeding it globally would undermine market class distinctness.
    - Long-term architecture: markets should have node profiles (AgriHub, MiningColony, FuelDepot, TradeHub) that define what they trade. Market view will be driven by node profile, not inventory key presence. The MarketProfile resource (active_station.gd @export var market_profile) is the right seam for this.
    - Any change to world genesis market state (even adding a zero-stock key) changes the determinism golden hash. Never add goods to market.Inventory unless they have an economy reason to be there.

- EPIC.S4.MODULE_MODEL.V0 [DONE]: Hero module slot model v0 + fleet capability packages, all content-driven (gates: GATE.S4.MODULE_MODEL.*)
  - Hero ship:
    - slot families v0 must include: Weapons, Defense, Power, Sensors, Utility (you may add Cargo%Drive later)
    - modules declare slot family, tags, and capability effects (no hardcoded “if weapon == X” logic)
  - Fleets:
    - remain capacity pools; capabilities are packages (tag bundles), never per-ship fitting
    - packages declare capability tags and sustain requirements (goods/day or periodic consumption)
  - Determinism requirements:
    - deterministic tie-breaks for target selection and hit resolution (stable ordering by entity id)
    - save%load preserves installed hero modules and fleet packages exactly
  - Evidence:
    - new or expanded contract tests asserting slot compatibility and package application determinism
    - a minimal station refit UI readout exists (even if ugly) showing installed modules and why a module cannot be installed
  - Failure mode (must be explainable):
    - incompatible module install attempt surfaces specific reason (missing slot, missing prereq, restricted)
  - Intervention verbs:
    - Industry: queue refit
- EPIC.S4.UI_INDU [DONE]: Dependency graphs, time-to-capability, “why blocked” and “what to build next” (gates: GATE.S4.UI_INDU.*)
- EPIC.S4.NPC_INDU [DONE]: NPC industry reacts to incentives and war demand (gates: GATE.S4.NPC_INDU.*)
- EPIC.S4.PERF_BUDGET [DONE]: Tick budget tests extended for industry (gates: GATE.S4.PERF_BUDGET.*)

Status: DONE

---

### Slice 5: Security and combat (local real-time + strategic resolution)
Purpose: Force matters economically, hero combat is real, fleets resolve at scale.

Default coupling rule (until overridden by an explicit gate):
- Local combat primarily affects: tactical incidents, local security posture, salvage, short-term access
- Strategic warfront outcomes primarily move via: supply delivery and the strategic resolver
- If local combat creates strategic impact, it must be via explicit events and bounded effects (no continuous hidden coupling)

v0 scope (LOCK ONCE SLICE 5 STARTS):
- Local combat is real-time and playable in the existing `Playable_Prototype` loop (fly%undock%dock remains functional)
- Docked state disables weapons and prevents input conflicts
- 1v1 encounter is mandatory proof: player ship vs 1 enemy ship
- Both ships have: shields, hull, shield regen rules, death state, HUD readout
- Weapons v0 are non-aimed:
  - Turrets: auto-target within range (deterministic tie-breaks)
  - Missiles: lock-on at launch with deterministic guidance
- Counter family v0: pick exactly 1 (Point Defense or ECM)
- Deterministic replay is mandatory:
  - Record input stream + RNG stream ids
  - Replay produces identical end state for the same inputs
  - Proof exists as a GameShell test (add a new `scripts/tests/test_combat_replay_v0.gd` if needed)

Epics:
- EPIC.S5.SECURITY_LANES [DONE]: Risk, delay, inspections, insurance sinks, lane regimes (gates: GATE.S5.SECURITY_LANES.*)
- EPIC.S5.COMBAT_LOCAL [DONE]: Hero ship real-time combat v0 (Starcom-like) with shields%hull and non-aimed weapons (turrets, missiles), deterministic replay, and “why we lost” explainability (gates: GATE.S5.COMBAT_LOCAL.*)
  - Must not require manual aiming for baseline effectiveness (turret targeting is primary)
  - Evidence: deterministic combat replay proof as part of GameShell test suite
  - Failure mode: player loss produces a cause chain (damage timeline + missing counter) and 1 to 2 suggested actions
  - Intervention verbs:
    - Industry: queue refit
    - Programs: change doctrine toggle (when escort doctrine exists)
- EPIC.S5.COMBAT_PLAYABLE.V0 [DONE]: In-engine combat encounters — fleet substantiation, player-initiated combat trigger, combat loop headless proof (gates: GATE.S5.COMBAT_PLAYABLE.*)
- EPIC.S5.COMBAT_RESOLVE [DONE]: Deterministic strategic resolver (attrition, outcomes, salvage) (gates: GATE.S5.COMBAT_RESOLVE.*)
- EPIC.S5.ESCORT_PROG [DONE]: Escort, patrol, interdiction, convoy programs (policy-driven) (gates: GATE.S5.ESCORT_PROG.*)
- EPIC.S5.LOSS_RECOVERY [IN_PROGRESS]: Salvage, capture, replacement pipelines tied to industry. **Prerequisite**: Composites + Components production chains instantiated (trade_goods_v0.md Phase 2) (gates: GATE.S5.LOSS_RECOVERY.*)
- EPIC.S5.UI_SECURITY [DONE]: Threat maps, convoy planning, incident timelines, “why we lost” explain chains. T61: all gates DONE (gates: GATE.S5.UI_SECURITY.*)
- EPIC.S5.COUPLING_LIMITS.V0 [TODO]: Explicit bounded coupling limits and event contracts for local -> strategic influence (gates: GATE.S5.COUPLING_LIMITS.*)
- EPIC.S5.NPC_TRADE.V0 [DONE]: NPC trade circulation — autonomous NPC traders evaluate markets, execute trades, stabilize prices (gates: GATE.S5.NPC_TRADE.*)
- EPIC.S5.COMBAT_LOOT.V0 [DONE]: Combat drop loot from destroyed ships (cargo + salvage), tractor beam module interaction, rarity tier visual coding, derelict yields; extends existing combat + discovery loot systems (gates: GATE.S5.COMBAT_LOOT.*)
- EPIC.S5.TRACTOR_SYSTEM.V0 [DONE]: Tractor beam module system per ship_modules_v0.md — 3-tier module-gated loot collection, Weaver faction variant Spindle Tractor (25u auto-salvage), auto-target nearest loot. **T33**: ranges, content, bridge, VFX. **T34**: Weaver variant, auto-target. Extends EPIC.S5.COMBAT_LOOT.V0. SimCore + GameShell (gates: GATE.S5.TRACTOR.*)

Status: IN_PROGRESS — Combat local, playable, doctrine, NPC trade, escort, security lanes, combat loot, tractor system all DONE (14 epics). Remaining: Loss Recovery (capture done, salvage/replacement pipelines remain), UI Security, Coupling Limits.

---

### Slice 6: Exploration, anomalies, extinct infrastructure, artifact tech
Purpose: Crazy discoveries create leverage and new strategies, feeding industry.

Epics:
- EPIC.S6.MAP_GALAXY [DONE]: Full galaxy map v1 — builds on and extends EPIC.S1.GALAXY_MAP_PROTO.V0 (prerequisite): adds fracture jump plotting (cost, Trace risk, accuracy radius, confirmation), layered reveal overlays integrating sensor unlock states, expedition planning and bookmarking, anomaly catalog overlays, system deep-inspection panel (full object list, unlock status, site phases); requires EPIC.S1.GALAXY_MAP_PROTO.V0 DONE before starting (gates: GATE.S6.MAP_GALAXY.*)
- EPIC.S6.OFFLANE_FRACTURE [DONE]: Fracture travel rules, risk bands, stable discovery markers, trace generation, off-lane route generation, headless proof. T30: offlane routes + headless proof (gates: GATE.S6.OFFLANE_FRACTURE.*)
- EPIC.S6.FRACTURE_COMMERCE.V0 [DONE]: Off-lane commerce v0 that is expensive but worth it:
  - Designed for small volume%high leverage (time-critical, high value, rare goods, frontier access), not bulk freight
  - Supports limited elite freighters with fracture, not mass fleet conversion
  - Enables discovered shortcuts and frontier outposts to feed lane economy
  - Integrates with exploitation packages and remote policy verbs (gates: GATE.S6.FRACTURE_COMMERCE.*)
- EPIC.S6.FRACTURE_ECON_INVARIANTS.V0 [DONE]: Scenario-pack invariants proving fracture does not replace lanes:
  - Lane wins for bulk and routine freight under normal conditions
  - Fracture wins only in defined niches and under defined frictions (tariffs, closures, extreme distance)
  - Evidence: deterministic invariants report (no timestamps, stable ordering) recorded in gate evidence; hard-fails on drift (gates: GATE.S6.FRACTURE_ECON_INVARIANTS.*)
- EPIC.S6.ANOMALY_ECOLOGY [TODO] [FO_TRADE_MANAGER_PREREQ]: Procedural anomaly distribution with deterministic seeds and spatial logic (gates: GATE.S6.ANOMALY_ECOLOGY.*)
- EPIC.S6.LAYERED_REVEALS.V0 [DONE]: New tech reveals new layers in previously discovered places. Sensor reveal, tech unlock, discovery content recontextualization, warfront intel tiering (presence→composition→supply→strategic via sustained observation), headless proof. T30: discovery reveal + warfront reveal + headless proof (gates: GATE.S6.LAYERED_REVEALS.*)
- EPIC.S6.DISCOVERY_OUTCOMES [DONE]: Persistent value outputs (intel, resources, artifacts, maps, leads). DiscoveryOutcomeSystem complete with family-specific loot (SalvagedTech, ExoticMatter, credits), discovery leads, and AnomalyEncounter records (gates: GATE.S6.DISCOVERY_OUTCOMES.*)
- EPIC.S6.ARTIFACT_RESEARCH [TODO] [FO_TRADE_MANAGER_PREREQ]: Identification, containment, experiments, failure modes (trace spikes, incidents). **Prerequisite**: T2/T3 module catalog expansion (ship_modules_v0.md Phase 2). **FO dependency**: unblocks Ancient Tech Multipliers (fo_trade_manager_v0.md Phase 3-5) (gates: GATE.S6.ARTIFACT_RESEARCH.*)
- EPIC.S6.TECH_LEADS [TODO] [FO_TRADE_MANAGER_PREREQ]: Tech leads become prototype candidates, gated by science throughput. **Prerequisite**: T2/T3 modules must exist as unlock targets (gates: GATE.S6.TECH_LEADS.*)
- EPIC.S6.EXPEDITION_PROG [TODO] [FO_TRADE_MANAGER_PREREQ]: Survey, salvage, multi-step expedition programs; escort optional (not a rescue treadmill). **FO dependency**: automation graduation for scanning (fo_trade_manager_v0.md Phase 3) (gates: GATE.S6.EXPEDITION_PROG.*)
- EPIC.S6.SCIENCE_CENTER [TODO] [FO_TRADE_MANAGER_PREREQ]: Analysis throughput, reverse engineering gates, special material handling. **FO dependency**: artifact research queue throughput (fo_trade_manager_v0.md Phase 4-5) (gates: GATE.S6.SCIENCE_CENTER.*)
- EPIC.S6.UI_DISCOVERY [TODO] [FO_TRADE_MANAGER_PREREQ]: Discovery UI overhaul — discovery phase markers on galaxy map (gray/amber/green icons), scanner range visualization, discovery milestone feedback cards (scan complete/analysis complete per ExplorationDiscovery.md), Knowledge Graph / Discovery Web UI (Outer Wilds Ship Log-inspired relationship map), breadcrumb trail visualization, scanner sweep animation on system entry, active leads display. **FO dependency**: player-facing feedback for exploration→automation pipeline. Per ExplorationDiscovery.md (gates: GATE.S6.UI_DISCOVERY.*)
- EPIC.S6.CLASS_DISCOVERY_PROFILES.V0 [TODO]: World class influences discovery families and outcomes (integrates Slice 2.5 classes with Slice 6) (gates: GATE.S6.CLASS_DISCOVERY_PROFILES.*)
- EPIC.S6.MYSTERY_MARKERS.V0 [TODO]: Mystery style policy and UI contracts (systemic mystery vs explicit markers) (gates: GATE.S6.MYSTERY_MARKERS.*)
- EPIC.S6.FRACTURE_DISCOVERY_EVENT.V0 [DONE]: Fracture module discovery gating — module unavailable until player discovers frontier derelict near warfront at ~tick 300+; derelict encounter, module acquisition narrative beat, gates the Revelation phase of the player arc per dynamic_tension_v0.md Pillar 5. Extends EPIC.S6.OFFLANE_FRACTURE (gates: GATE.S6.FRACTURE_DISCOVERY.*)
- EPIC.S6.DISCOVERY_TRADE_PIPELINE.V0 [DONE]: Exploration->automation pipeline wiring — EconomicIntel entity, DISCOVERY_OPPORTUNITY FO trigger, intel decay->margin confidence (two-phase linear decay), survey bridge queries, intel UI with freshness indicators. T57: foundation. T62: FO intel trigger, margin confidence BPS, survey results bridge, intel UI freshness/confidence (gates: GATE.T57.PIPELINE.*, GATE.T62.PIPELINE.*)
- EPIC.S6.FO_CENTAUR.V0 [TODO]: Centaur model implementation — FO competence tiers (3-tier per fo_trade_manager_v0.md), personality-colored confidence language (not bars), world adaptation (5 event types, FO adapts not errs), boredom circuit breakers (5 spectator-trough triggers). Per ExplorationDiscovery.md §Centaur Model + fo_trade_manager_v0.md §Competence Model (gates: GATE.T57.CENTAUR.*)
- EPIC.S6.KNOWLEDGE_GRAPH_V2.V0 [TODO]: Knowledge Graph player interaction — 5 player verbs (Pin, Annotate, Link, Flag, Compare), speculative link state machine (Obra Dinn batch confirmation), dual-mode data model (geographic + relational). Per ExplorationDiscovery.md §KG Player Verbs + §Link Feedback + §Dual-Mode Display (gates: GATE.T57.KG.*)
- EPIC.S6.DISCOVERY_FEEL.V0 [TODO]: Discovery feel polish — 4 audio signature stubs + phase hooks, milestone card audio wiring (extend T52 visual cards), 6 discovery failure types + partial success, scanner sweep animation on system entry. Per ExplorationDiscovery.md §Audio Vocabulary + §Discovery Failure States (gates: GATE.T57.FEEL.*)

Status: IN_PROGRESS — Fracture travel, commerce, econ invariants, discovery outcomes, galaxy map, Fracture Discovery Event, Layered Reveals, Planet Scan (T42-T43) all DONE. Remaining: Anomaly Ecology, Artifact Research (T2/T3 modules exist now), Tech Leads (T2/T3 modules exist now), Science Center, Expedition programs, UI Discovery, Class Discovery Profiles, Mystery Markers. **6 remaining epics tagged [FO_TRADE_MANAGER_PREREQ]** — these are prerequisites for the FO Trade Manager system (fo_trade_manager_v0.md), not standalone exploration features. **T57 adds 4 new epics**: Discovery Trade Pipeline (P0-P1 pipeline wiring), FO Centaur (competence + adaptation), Knowledge Graph V2 (player verbs + link feedback), Discovery Feel (audio + failures). Build order: T57 pipeline+centaur+KG first, then T58 re-exploration+trade-history+late-game, then P2 artifact research + science center. See ExplorationDiscovery.md §Implementation Roadmap for full dependency graph.

---

### Slice 7: Factions, warfronts, governance, and map change
Purpose: Logistics shapes wars and the galaxy’s political topology, with lasting consequences.

Epics:
- EPIC.S7.FACTION_MODEL [DONE]: Goals, doctrines, policies, constraints, tech preferences. Core DONE (5 factions, territories, doctrines, tariffs, aggression via FactionTweaksV0.cs + GalaxyGenerator.cs). Remaining: faction identity redesign (see EPIC.S7.FACTION_IDENTITY_REDESIGN.V0), unique faction mechanics (gates: GATE.S7.FACTION_MODEL.*)
- EPIC.S7.WARFRONT_THEATERS [DONE]: Procedural warfront seeding from geography and faction goals. SeedWarfrontsV0 implements geography-based contested-node detection with BFS adjacency from faction territories (gates: GATE.S7.WARFRONT_THEATERS.*)
- EPIC.S7.WARFRONT_STATE [DONE]: Front lines, objectives, supply demand, attrition. Core state + intensity transitions, supply demand, fleet attrition (supply-gated, intensity-scaled), strategic objectives (SupplyDepot/CommRelay/Factory capture by fleet dominance). T30: attrition + objectives (gates: GATE.S7.WARFRONT_STATE.*)
- EPIC.S7.SUPPLY_IMPACT [DONE]: Delivered goods and services move warfront state with persistent consequences. WarfrontDemandSystem supply ledger tracks cumulative deliveries; intensity shifts when threshold met (gates: GATE.S7.SUPPLY_IMPACT.*)
- EPIC.S7.TERRITORY_REGIMES [DONE]: Permissions, tariffs, embargoes, inspections, closures; hysteresis rules. Embargo system, tariff scaling, regime matrix, neutrality tax, asymmetric hysteresis (instant worsen, delayed improve). T30: hysteresis (gates: GATE.S7.TERRITORY_REGIMES.*)
- EPIC.S7.TERRITORY_SHIFT.V0 [DONE]: Dynamic territory recomputation (T35). Hash-affecting (gates: GATE.S7.TERRITORY_SHIFT.*)
- EPIC.S7.TECH_ACCESS [DONE]: Exclusives, embargoed tech, licensing, doctrine-based variants (gates: GATE.S7.TECH_ACCESS.*)
- EPIC.S7.DIPLOMACY_VERBS.V0 [DONE]: Diplomacy verbs set definition and contracts (treaties%bounties%sanctions%privateering) (gates: GATE.S7.DIPLOMACY_VERBS.*)
- EPIC.S7.REPUTATION_INFLUENCE [DONE]: Reputation drives access, pricing, inspection posture, deal availability, and faction contracts. Rep-driven dock/trade/tech access, tariff scaling, trade drift, war profiteering, faction contract gating by reputation tier (Neutral/Friendly/Allied). T30: faction contracts (gates: GATE.S7.REPUTATION_INFLUENCE.*)
- EPIC.S7.UI_DIPLO [TODO]: Faction intel, deal making, “why policy changed” (gates: GATE.S7.UI_DIPLO.*)
- EPIC.S7.UI_WARFRONT [TODO]: Dashboards, projected outcomes, intervention options, supply checklists (gates: GATE.S7.UI_WARFRONT.*)
- EPIC.S7.BRIDGE_THRONEROOM_V0 [TODO]: Bridge layer as strategic view + unlock surface tied to factions%warfronts%tech posture (gates: GATE.S7.BRIDGE_THRONEROOM_V0.*)
- EPIC.S7.CLASS_WARFRONT_PROFILES.V0 [TODO]: World class influences warfront seeding and supply shapes (integrates Slice 2.5 classes with Slice 7) (gates: GATE.S7.CLASS_WARFRONT_PROFILES.*)
- EPIC.S7.FO_TRADE_MANAGER.V0 [DONE]: FO trade management system per fo_trade_manager_v0.md v6 — Level of Autonomy (LOA) per domain with 200-tick revert window, decision dialogue framework (5 presentation rules), empire health indicator (green/yellow/red HUD diamond), dock arrival recap ("While You Were Away" batch summary), flip moment celebration (6-channel multi-sensory), FO service record (trust-building transparency), belt-watching visualization (route throughput + bottleneck indicators), dashboard consolidation (9→5 tabs), unified "Route" terminology. SimCore + SimBridge + GameShell. Hash-affecting for core systems (gates: GATE.T58.FO.*, GATE.T58.UI.*, GATE.T58.AUDIO.*, GATE.T58.PROOF.*, GATE.T58.EVAL.*). Closed T67: 19/20 DONE + 1 SUPERSEDED (FO_MANAGER_E2E.001).
- EPIC.S7.INSTABILITY_PHASES [DONE]: Per-node instability model with 5 phases (Stable/Shimmer/Drift/Fracture/Void), worldgen assignment, phase transition mechanics. InstabilitySystem.cs implements tick-based evolution, phase thresholds (0/25/50/75/100), warfront-adjacent gain, distant decay, bridge queries. Note: mechanical effects (price jitter, lane delay, trade failure, market closure) are defined in tweaks but not yet applied to MarketSystem/LaneFlowSystem — integration deferred to Phase 2 gates (gates: GATE.S7.INSTABILITY.*)
- EPIC.S7.FACTION_IDENTITY_REDESIGN.V0 [TODO]: Holistic redesign of faction economic identities — tariff rates, aggression levels, trade policies aligned to species/philosophy lore; 40 faction-specific T2 modules (8 per faction); 5 faction signature mechanics (Zero Variance, Metamorphic Adaptation, Silk Lattice, Cache Beacon, Metric Harmonics); 12 faction ship variants; faction station dialogue registers; faction-specific market mechanics (Chitin bet-framing, Weaver delayed pricing, Valorin swarm density, Communion outreach). Per faction_equipment_and_research_v0.md. Updates FactionTweaksV0.cs + factions_and_lore_v0.md. Code + doc together (gates: GATE.S7.FACTION.IDENTITY_REDESIGN.*)
- EPIC.S7.PROCEDURAL_PLANETS.V0 [DONE]: Procedural planet + star generation per system node: Star class (G/K/M/F/A/O) with luminosity influencing planet temperature, PlanetType (Terrestrial/Ice/Sand/Lava/Gaseous/Barren) with gravity+atmosphere landability rules, specialization-driven planet industries (Agriculture/Mining/Manufacturing/HighTech/FuelExtraction), tech-gated landing for harsh environments, dockable Area3D trigger for landable planets, dock menu planet info UI (gates: GATE.S7.PLANET.*)
- EPIC.S7.PRODUCTION_CHAINS.V0 [DONE]: Instantiate remaining 6/9 production recipes (ProcessFood, FabricateComposites, AssembleElectronics, AssembleComponents, SalvageToMetal, SalvageToComponents) as industry sites in worldgen; populate GoodDefV0.BasePrice and PriceSpread; add ProductionTicks to recipes; geographic clustering for RareMetals. **Prerequisite for**: economic cascades (dynamic_tension Pillar 3), EPIC.S5.LOSS_RECOVERY. Hash-affecting (gates: GATE.S7.PRODUCTION_CHAINS.*)
- EPIC.S7.SUSTAIN_ENFORCEMENT.V0 [DONE]: Fleet fuel consumption (passive drain while docked or flying), module sustain resource deduction per 60-tick cycle, starvation reduced-power state (50% at zero sustain, 20% safety floor), ~50-80 tick runway tuning for starter ship. Implements dynamic_tension Pillar 2 (Maintenance Treadmill). Hash-affecting (gates: GATE.S7.SUSTAIN_ENFORCEMENT.*)
- EPIC.S7.POWER_BUDGET.V0 [DONE]: Enforce sum(PowerDraw) ≤ BasePower at fitting time; modules over budget blocked; mount type implementation (Standard/Broadside/Spinal arc restrictions + damage bonuses); module degradation from zone damage. Extends EPIC.S18.SHIP_MODULES.V0 schema. Hash-affecting (gates: GATE.S7.POWER_BUDGET.*)
- EPIC.S7.INSTABILITY_EFFECTS.V0 [DONE]: Wire instability phase mechanical effects into MarketSystem (price jitter at Drift, trade failure chance at Fracture, market closure at Void) and LaneFlowSystem (throughput reduction at Drift+); metric arbitrage opportunity in Phase 2+ space. Effects defined in InstabilityTweaksV0.cs, need system integration. Hash-affecting (gates: GATE.S7.INSTABILITY_EFFECTS.*)
- EPIC.S7.T2_MODULE_CATALOG.V0 [DONE]: Add ~25 T2 modules to content registry: weapons (Railgun, FEL, Particle Beam, Plasma Carronade, Torpedo, Swarm Battery, Casaba Lance), defense (Reactive Plating, SiC Composite, Angled Deflector, Shield Capacitor, Magnetic Confinement), engines (Fusion Torch, D-He3 Drive, Antimatter Catalyst), utility (Compact Tokamak, Gravimetric Sensor, ECM Suite, Fuel Processor, Cargo Concealer). Faction rep unlock thresholds and rare material sourcing. **Prerequisite for**: EPIC.S6.ARTIFACT_RESEARCH, EPIC.S6.TECH_LEADS. Hash-affecting (gates: GATE.S7.T2_MODULES.*)
- EPIC.S7.STARTER_PLACEMENT.V0 [DONE]: GalaxyGenerator constraint ensuring player start system borders at least one contested warfront node; verification test over N seeds. Implements dynamic_tension Pillar 1 requirement. Hash-affecting (gates: GATE.S7.STARTER_PLACEMENT.*)
- EPIC.S7.FACTION_VISUALS.V0 [DONE]: Faction-specific visual identity — ship liveries/tints per faction, station aesthetic differentiation, UI color themes for faction contexts, HUD tints when in faction territory. GameShell-only (gates: GATE.S7.FACTION_VISUALS.*)
- EPIC.S7.ENFORCEMENT_ESCALATION.V0 [DONE]: Pattern-based heat accumulation (volume + route + counterparty signals), confiscation event type, fine system, heat decay window. Trace enforcement only — risk meter UI visualization in EPIC.S7.RISK_METER_UI.V0. Extends existing SecurityLaneSystem Edge.Heat field. Hash-affecting (gates: GATE.S7.ENFORCEMENT.*)
- EPIC.S7.HUD_ARCHITECTURE.V0 [DONE]: HUD information architecture overhaul — screen zone enforcement (Zones A-G per HudInformationArchitecture.md), toast notification priority levels (Critical/Warning/Info/Confirmation) and bundling, toast action bridges, progressive disclosure rules (Tier 1/2/3), alert badge in Zone A, Zone G bottom bar framework. GameShell-only (gates: GATE.S7.HUD_ARCH.*)
- EPIC.S7.COMBAT_JUICE.V0 [DONE]: Combat feel and feedback stack per CombatFeel.md — kill explosion VFX (fireball + debris + smoke), shield ripple shader, shield break flash + audio, floating damage numbers, weapon trail differentiation by damage family (Kinetic/Energy/Neutral/PD), screen shake intensity scale, wire combat audio pool. GameShell-only (gates: GATE.S7.COMBAT_JUICE.*)
- EPIC.S7.COMBAT_FEEL_POLISH.V0 [DONE]: Combat feel polish — VFX scaled ~4x for altitude 80 camera, shield VFX (ripple + break flash + hull sparks), weapon family visual differentiation. Addressed ACTIVE_ISSUES C1-C7, A1. GameShell-only (gates: GATE.S7.COMBAT_FEEL_POLISH.*)
- EPIC.S7.RISK_METER_UI.V0 [DONE]: Risk meter visualization per RiskMeters.md — Heat/Influence/Trace widgets in Zone G, 5 named thresholds, trend arrows, decay rate display, threshold transition toasts, screen-edge ambient tinting at High+, compound threat indicators. GameShell-only; SimBridge queries: GetRiskMetersV0, GetRiskDecayRateV0, GetCompoundThreatV0 (gates: GATE.S7.RISK_METER_UI.*)
- EPIC.S7.AUDIO_WIRING.V0 [DONE]: Audio asset wiring + bus architecture per AudioDesign.md — connect 6 existing unused audio assets, 5-layer bus separation (Music/Ambient/SFX/UI/Alert), discovery phase chimes, risk threshold alerts. GameShell-only. Content specs in docs/design/content/AudioContent_TBA.md (gates: GATE.S7.AUDIO_WIRING.*)
- EPIC.S7.GALAXY_MAP_V2.V0 [DONE]: Galaxy map evolution per GalaxyMap.md — 5 new overlay modes (Faction Territory/Exploration/Fleet Positions/Warfronts/Heat), route planner, galaxy search, semantic zoom, constellation clustering, icon toggles. All 9 gates DONE. GameShell-only (gates: GATE.S7.GALAXY_MAP_V2.*)
- EPIC.S7.NARRATIVE_DELIVERY.V0 [DONE]: Narrative text delivery system per NarrativeDesign.md — flavor_text fields in IntelBook entities, discovery narrative templates, faction station greetings, text display panel with faction voice styling. Hash-affecting for entity fields (gates: GATE.S7.NARRATIVE_DELIVERY.*)
- EPIC.S7.AUTOMATION_MGMT.V0 [DONE]: Automation management layer per AutomationPrograms.md — program performance tracking, failure reason UI, budget caps, doctrine system, program templates. **T32**: ProgramMetricsSystem, ProgramHistorySystem, DoctrineSystem, SimBridge queries, Empire Dashboard automation tab. **T33**: ProgramTemplateContentV0 (5 templates), SimBridge GetProgramTemplatesV0, template picker UI. (gates: GATE.S7.AUTOMATION_MGMT.*)
- EPIC.S7.RUNTIME_STABILITY.V0 [DONE]: Runtime stability and visual bug fixes from full eval 2026-03-10 — HUD parse error, faction color crash, planet/camera warp arrival, galaxy view Z-ordering, ship visibility, combat VFX scaling, UI polish, VFX polish. Fixes ACTIVE_ISSUES R1, R2, V1, V4-V12, C10, U2-U5. All 8 gates DONE. GameShell+SimBridge (gates: GATE.S7.RUNTIME_STABILITY.*)
- EPIC.S7.FLEET_TAB.V0 [DONE]: Empire Dashboard Fleet tab (F3) per EmpireDashboard.md — master-detail fleet list, per-fleet cargo/modules/programs/status/doctrine, action buttons (Recall/Dismiss/Rename), program assignment. Pulled forward from S9. GameShell-only (gates: GATE.S7.FLEET_TAB.*)
- EPIC.S7.MAIN_MENU.V0 [DONE]: Main menu game shell per MainMenu.md — main_menu.tscn as project main scene, adaptive menu list, new voyage wizard (name/seed/difficulty), save slot metadata, auto-save slot, pause menu overlay, captain name persistence. GameShell + SimBridge + SimCore (gates: GATE.S7.MAIN_MENU.*)
- EPIC.S7.COMBAT_PHASE2.V0 [DONE]: Phase 2 combat per ship_modules_v0.md and CombatFeel.md. **Phase 2a (T31)**: SimCore heat accumulation + overheat cascade, battle stations state machine, radiator module HP + cooling bonus, SimBridge combat queries, heat gauge HUD widget. **Phase 2b (T32)**: spin turn penalty (gyroscopic precession, TurnPenaltyBpsPerRpm), mount type system (Standard/Broadside/Spinal arc restrictions on ModuleSlot), spin-fire cadence (per-mount engagement arc fractions), spinal mount fire (axis-aligned, no rotation penalty), overheat VFX (screen edge shimmer at 75%+, vent burst flash at lockout), radiator status display, zone HUD (spin RPM + radiator readouts in combat_hud.gd), headless proof (test_combat_spin_proof_v0.gd). Visual ship rotation deferred to future aesthetic epic. Hash-affecting SimCore + GameShell (gates: GATE.S7.COMBAT_PHASE2.*)
- EPIC.S7.COMBAT_DEPTH_V2.V0 [DONE]: Combat tactical depth layer 2 per combat_mechanics_v0.md — pre-combat outcome projection, tracking/evasion by weapon size, Fore zone soft-kill, damage variance, armor penetration stat. **T36**: tracking system, damage variance, armor penetration, fore kill, bridge+HUD+projection, headless proof. Hash-affecting (gates: GATE.S7.COMBAT_DEPTH2.*)
- EPIC.S7.FACTION_COMMISSION.V0 [DONE]: Faction commission system + reputation depth (T35). Hash-affecting (gates: GATE.S7.FACTION_COMMISSION.*)

Status: IN_PROGRESS — 51 DONE epics in S7. Remaining TODO: UI Diplo, UI Warfront, Bridge Throneroom V0, Class Warfront Profiles, Faction Identity Redesign.

---

### Slice 8: Fracture policing, existential pressure, megaproject endgames
Purpose: Lane builders police fracture; pressure escalates; win via massive supply-bound projects under multiple scenarios.

Epics:
- EPIC.S8.POLICING_SIM [TODO]: Trace-driven escalation model, legible actions, counterplay verbs (gates: GATE.S8.POLICING_SIM.*)
- EPIC.S8.THREAT_IMPACT [TODO]: Supply shocks, lane disruption, interdiction waves, faction realignment (gates: GATE.S8.THREAT_IMPACT.*)
- EPIC.S8.PLAYER_COUNTERPLAY.V0 [TODO]: Counter-programs, corridor hardening, trace scrubbers, misdirection (gates: GATE.S8.PLAYER_COUNTERPLAY.*)
- EPIC.S8.MEGAPROJECT_SET.V0 [TODO]: Canonical megaproject set and their rule changes (anchors, stabilizers, pylons, corridors) (gates: GATE.S8.MEGAPROJECT_SET.*)
- EPIC.S8.MEGAPROJECTS [DONE]: Multi-stage projects that reshape map rules under supply constraints. **T41**: Megaproject entity, MegaprojectSystem (start/deliver/process/mutate), MegaprojectContentV0 (3 types: fracture_anchor, trade_corridor, sensor_pylon), map rule mutations (IsFractureNode, SpeedMultiplierPct, SensorPylonNodes), DeliverMegaprojectSupplyCommand, StartMegaprojectCommand, SimBridge.Megaproject.cs (5 queries), megaproject_panel.gd (M key), 9 contract tests + 4 integration tests. **T47**: Galaxy map megaproject markers (type-specific meshes: hex frame anchor, diamond corridor, cone pylon), construction VFX (rotating cylinder spars + blinking spark spheres, progress-scaled intensity). (gates: GATE.S8.MEGAPROJECTS.*)
- EPIC.S8.WIN_SCENARIOS [DONE]: Three endgame paths (Reinforce/Naturalize/Renegotiate) per factions_and_lore_v0.md, each with distinct gameplay requirements, faction reputation thresholds, and fragment prerequisites; explicit loss states (death/bankruptcy with narrative frames); epilogue system (45-90 sec text cards showing costs of paths not chosen); equipment state reflection in epilogue. Supersedes older containment/alliance/dominance terminology (gates: GATE.S8.WIN_SCENARIOS.*)
- EPIC.S8.UI_WARROOM [TODO]: Warfronts + policing + megaproject pipelines + bottlenecks (gates: GATE.S8.UI_WARROOM.*)
- EPIC.S8.STORY_STATE_MACHINE [DONE]: Story beats via discovery/trace/warfront phases, not timed missions; Five Recontextualizations delivery system — revelation triggers (module predates threads, threads aren't protection, economy is a cage, module is changing me, instability is not entropy), gold toast + galaxy map highlights + FO reactions + Discovery Web updates per NarrativeDesign.md; cover-story naming state transitions (pre/post-revelation name switching). Hash-affecting (gates: GATE.S8.STORY_STATE_MACHINE.*)
- EPIC.S8.BRIDGE_THRONEROOM_V1 [TODO]: Endgame readiness, scenario selection, empire posture surface (gates: GATE.S8.BRIDGE_THRONEROOM_V1.*)
- EPIC.S8.ADAPTATION_FRAGMENTS.V0 [DONE]: 16 named Adaptation fragments discoverable at void sites; opaque-name reveal mechanic; 8 resonance pairs that unlock emergent capabilities when combined (fracture navigation, exotic crystal extraction, T3 fabrication, lattice fast-travel, endgame path unlocks); fragment progression drives discovery arc. **T34**: entity, content (16 defs), collection system, worldgen placement, bridge+UI, trophy wall deposit, headless proof. **Prerequisite for**: EPIC.S8.WIN_SCENARIOS. Per factions_and_lore_v0.md "Adaptation Fragment Web" (gates: GATE.S8.ADAPTATION.*)
- EPIC.S8.HAVEN_STARBASE.V0 [DONE]: Hidden Precursor starbase in stable Phase-0 space per haven_starbase_v0.md; dormant until player docks; one-way outbound secret lane initially; 5-tier upgrade tree (Powered/Inhabited/Operational/Expanded/Awakened); bidirectional travel at tier 3; Haven Residents; Trophy Wall; Haven market; Hangar; 20 data logs; lore delivery hub; strategic asset for all three endgame paths. **T33**: HavenStarbase entity, SeedHavenV0, HavenUpgradeSystem, HavenHangarSystem, deferred Haven market, SimBridge.Haven.cs, dock panel, galaxy icon. **T34**: Haven Residents (Keeper + FO candidates), Trophy Wall (deposit + resonance display), Fragment Geometry display, 19 Haven data logs, Ancient Hull restoration, headless proof. **T37**: Keeper 5-tier evolution, Resonance Chamber, Fabricator, Haven market restocking, depth bridge queries. **T41**: Research lab, module transfer, accommodation bonuses, faction ally reveal. **T47**: Coming Home arrival cinematic (warm amber letterbox + slow camera zoom + "Welcome home, Captain." FO toast), visual geometry per tier (T1 purple ring, T2 satellites, T3 outer ring, T4 hex frame + pulsing beacon, T5 golden emission + tilted rings), Communion Representative dialogue (8 themed lines from FactionDialogueContentV0, "Speak Again" cycling, tier 3+ gated). **Prerequisite for**: EPIC.S8.WIN_SCENARIOS + EPIC.S8.T3_PRECURSOR_MODULES.V0. Per factions_and_lore_v0.md + haven_starbase_v0.md (gates: GATE.S8.HAVEN.*)
- EPIC.S8.LATTICE_DRONES.V0 [DONE]: Lattice maintenance drone NPC entity type with instability-phase-linked AI escalation. **T36**: entity, spawn system, combat integration, bridge queries. Per factions_and_lore_v0.md (gates: GATE.S8.LATTICE_DRONES.*)
- EPIC.S8.NARRATIVE_CONTENT.V0 [DONE]: Authored story content for the story state machine. **T38**: FactionDialogueContentV0 (15 entries, 5 factions x 3 rep tiers), WarfrontCommentaryContentV0 (25 entries, 5 factions x 5 intensities), AdaptationFragmentContentV0 cover/revealed lore pairs, RevelationTextV0 (5 gold toast entries). **T46**: Haven starbase logs (26 entries across 6 tiers), endgame path narratives (5 paths with epilogues + FO farewells), expanded fragment lore (bridge wiring), faction dock greetings (25) + station descriptions (25). Content specs in docs/design/content/NarrativeContent_TBA.md + docs/design/content/LoreContent_TBA.md (gates: GATE.S8.NARRATIVE.*)
- EPIC.T18.KNOWLEDGE_GRAPH_SEEDING.V0 [DONE]: Knowledge graph connection generation pipeline per knowledge_graph_mechanics_v0.md. **T35**: Template token resolution in GalaxyGenerator Phase 9.5 (RESOLVE.001), procedural proximity + faction link connections via BFS in NarrativePlacementGen (PROXIMITY.001). Hash-affecting (gates: GATE.T18.KG_SEED.*)
- EPIC.S8.T3_PRECURSOR_MODULES.V0 [DONE]: ~13 T3 discovery-only modules: weapons (Graviton Shear, Annihilation Beam, Void Lance, Void Seekers), defense (Null-Mass Lattice, Gravitational Lens, Phase Matrix), engines (Metric Drive Core, Void Sail), utility (Quantum Vacuum Cell, Graviton Tether, Resonance Comm, Seed Fabricator). Exotic matter sustain, cannot be manufactured — discovery and Haven fabrication only. **T34**: 9 content defs, exotic sustain wiring, discovery-only gate, bridge queries. **Prerequisite**: EPIC.S8.HAVEN_STARBASE.V0 tier 3+ (gates: GATE.S8.T3_MODULES.*)
- EPIC.S8.PENTAGON_BREAK.V0 [DONE]: Pentagon dependency cascade — the game's #1 narrative moment. **T38**: PentagonBreakSystem (detection + cascade), PentagonBreakTweaksV0, SimBridge pentagon queries, galaxy map pentagon overlay, revelation toast delivery. Hash-affecting (gates: GATE.S8.PENTAGON.*)
- EPIC.S8.ANCIENT_SHIP_HULLS.V0 [DONE]: Three ancient ship hulls per ship_modules_v0.md — Bastion (tank), Seeker (explorer), Threshold (fracture specialist). Discovered at deep void sites, restored at Haven Tier 3+. Pre-revelation names ("Hull Type XV-1/2/3"). **T34**: 3 ShipClassDef entries, AncientHullTweaksV0, RestoreAncientHullV0 bridge, hull restoration UI. **Depends on**: EPIC.S8.HAVEN_STARBASE.V0. Hash-affecting (gates: GATE.S8.ANCIENT_HULLS.*)
- EPIC.S8.DEEP_DREAD.V0 [DONE]: Subnautica-inspired depth-as-dread system — Thread Lattice instability phases map to escalating terror layers (Isolation→Phenomena→Predation→Meta-Dread). Patrol thinning by hop distance, passive hull drain at Phase 2+, sensor ghost phantom contacts, information fog at distance, Lattice Fauna emergent entities (instrument interference, fuel drain, avoidable by going dark), fracture exposure tracking with adaptation, 8 FO dread triggers with 24 dialogue lines, phase-aware ambient audio gradient, comms degradation with text corruption, visual distortion shader, galaxy map dread overlay. Per deep_dread_v0.md. Hash-affecting (gates: GATE.T45.DEEP_DREAD.*)

Status: IN_PROGRESS — 11 DONE epics (Win Scenarios, Story State Machine, Pentagon Break, Adaptation Fragments, Haven Starbase partial, Lattice Drones, T3 Modules, Ancient Hulls, KG Seeding, Narrative Content partial, Deep Dread). Remaining TODO: Policing Sim, Threat Impact, Player Counterplay, Megaproject Set, UI Warroom, Bridge Throneroom V1. IN_PROGRESS: Haven Starbase (Coming Home transitions, visual geometry, Communion dialogue remain), Megaprojects (visual markers, VFX, more types), Narrative Content (Haven logs, endgame narratives, fragment lore).

---

### Slice 9: Polish, UX hardening, and mod hooks
Purpose: Make it shippable and extendable without breaking determinism.

Epics:
- EPIC.S9.SAVE [DONE]: Robust save UX, migrations, corruption handling. **T41**: SaveEnvelope version-aware serialization (CurrentVersion=2), v1→v2 migration, TryDeserializeSafe, ValidateState, GetSaveIntegrityV0. 11 migration+integrity tests. **T46**: Timer-based auto-save (5min default, combat pause, dedicated slot), AutosaveTweaksV0, HUD "SAVING..." indicator with fade, settings panel toggle. **T47**: Save slot management panel (scan/load/rename/delete per slot, delete confirmation dialog), corruption recovery (integrity check + red visual treatment for corrupted saves, "Try Load" fallback). Cloud save deferred to Steam integration. (gates: GATE.S9.SAVE.*)
- EPIC.S9.UI [TODO]: Information architecture cleanup, tooltips, clarity passes, onboarding guidance (gates: GATE.S9.UI.*)
- EPIC.S9.MOD [TODO]: Content packs, compatibility rules, safe mod surface, validation tooling (gates: GATE.S9.MOD.*)
- EPIC.S9.PERF [DONE]: Performance profiling. **T46**: Per-system Stopwatch instrumentation in SimKernel (63 call sites, 100-tick ring buffer), PerfBudgetTests (50-node galaxy, 500 ticks, 4.33MB heap, <256MB budget), TickBudgetTests (3.3-3.9ms avg tick), ea_perf_baseline.md report (60fps confirmed, 6.8ms headroom). (gates: GATE.S9.PERF.*)
- EPIC.S9.ACCESS.V0 [DEFERRED]: Superseded by EPIC.S9.ACCESSIBILITY.V0 — expanded scope with first-launch prompt, colorblind shaders, font scaling. See MainMenu.md (gates: GATE.S9.ACCESS.*)
- EPIC.S9.BALANCE_LOCK.V0 [DONE]: Tuning targets and regression bounds locked. **T41**: Reflection-based snapshot of all *TweaksV0 const values into balance_baseline_v0.json (~992 lines, ~330 constants). BalanceLockTests: compare current values against baseline, fail on drift. Regenerate baseline by deleting JSON and re-running test (gates: GATE.S9.BALANCE_LOCK.*)
- EPIC.S9.CONTENT_WAVES [TODO]: Final archetype families, world classes, endgame megaproject variety (gates: GATE.S9.CONTENT_WAVES.*)
- EPIC.S9.MISSION_LADDER.V0 [IN_PROGRESS]: Missions M2-M6 using existing mission runner framework — M2 Mining (extraction + mining automation), M3 Patrol (route security + escort), M4 Construction (outpost build + station supply), M5 Research (anomaly + science pipeline), M6 Fracture Drive (off-lane intro + derelict salvage). T29: Mining Survey, Ore Extraction, First Research, Research Materials, First Build, Station Expansion content + bridge extensions. Per 50_51_52_53 §50 (gates: GATE.S9.MISSIONS.*)
- EPIC.S9.SETTINGS.V0 [DONE]: Full options screen — 4 tabs (Gameplay, Display, Audio, Accessibility), auto-save on change, shared between main menu and pause menu. Per MainMenu.md. Depends on EPIC.S7.MAIN_MENU.V0 + EPIC.S7.AUDIO_WIRING.V0. GameShell-only. All 5 gates DONE (T27-T29). (gates: GATE.S9.SETTINGS.*)
- EPIC.S9.MENU_ATMOSPHERE.V0 [DONE]: Title screen atmosphere — parallax starfield shader, adaptive foreground silhouette, menu audio timing, galaxy generation screen, Precursor subtitle quotes. Per MainMenu.md. GameShell-only (gates: GATE.S9.MENU_ATMOSPHERE.*)
- EPIC.S9.ACCESSIBILITY.V0 [DONE]: First-launch accessibility prompt, colorblind shaders, font size override, high contrast, reduced shake. Per MainMenu.md. GameShell-only (gates: GATE.S9.ACCESSIBILITY.*)
- EPIC.S9.MILESTONES_CREDITS.V0 [DONE]: Milestones viewer + credits scroll. **T38**: milestone_viewer.gd (card grid + lifetime stats sidebar), GetLifetimeStatsV0 bridge, credits_scroll.gd (scrolling text, skip-on-input), main menu buttons. Per MainMenu.md. GameShell-only (gates: GATE.S9.MILESTONES_CREDITS.*)
- EPIC.S9.MUSIC.V0 [DONE]: Dynamic music system with placeholder stems. **T46**: 4-layer stem pipeline (bass/pad/melody/percussion) with state-driven crossfade (SILENCE/EXPLORATION/COMBAT/TENSION/DOCK), placeholder sine-wave stems, combat/dock/warfront music triggers wired to game_manager. **T47**: Discovery stingers (minor/major/revelation with stem ducking), FRACTURE music state (detuned frequencies, LFO tremolo), faction ambient layer (5 characteristic drones per faction with 2s crossfade), comprehensive music_production_brief_v0.md (29-file spec for external composer). Real audio stems await external composition. (gates: GATE.S9.MUSIC.*)
- EPIC.S9.STEAM.V0 [IN_PROGRESS]: Steam platform integration — Steamworks SDK, Steam cloud saves wired to existing save system, 15-20 Steam achievements mapped from milestone system, Steam overlay compatibility, build/export pipeline for Godot 4 + C#/.NET 8. Distribution prerequisite. **T41**: steam_appid.txt (placeholder 480), _init_steam_v0() with graceful fallback, is_steam_enabled(), 18 milestones (6 trade, 3 explore, 3 research, 3 mission, 3 combat) mapped to Steam achievement IDs, _unlock_steam_achievement_v0() on milestone celebration. Remaining: GodotSteam 4.x addon install, cloud saves, overlay, build pipeline (gates: GATE.S9.STEAM.*)
- EPIC.S9.TELEMETRY.V0 [TODO]: Opt-in anonymous telemetry — session length, player death locations, trade loop profitability, system visit frequency, quit points; crash/exception reporting hook via Godot notification; simple backend or local-file fallback. EA feedback prerequisite (gates: GATE.S9.TELEMETRY.*)
- EPIC.S9.FLEET_TAB.V0 [DEFERRED]: Pulled forward to EPIC.S7.FLEET_TAB.V0 — fleet management is core gameplay, not polish (gates: GATE.S7.FLEET_TAB.*)
- EPIC.S9.MARKET_DEPTH.V0 [DONE]: Bid/ask spread and depth-dependent pricing. T61: all gates DONE (gates: GATE.S9.MARKET_DEPTH.*)
- EPIC.S9.PROGRAM_POSTMORTEMS.V0 [DONE]: Automation failure taxonomy — 7 cause codes. T61: all gates DONE (gates: GATE.S9.POSTMORTEMS.*)
- EPIC.T48.ANOMALY_CHAINS.V0 [TODO]: Multi-site escalating discovery arcs per ExplorationDiscovery.md — anomaly chain engine (3-5 site sequences with narrative continuity, each site's analysis reveals the next site's location), starter chain content (3 chains across derelict/precursor/biological families, 6-15 total sites), chain state tracking in SimState, bridge queries for chain progress + next-site hints. Discovery-first: player finds each link by exploring, not by following a quest marker. Late-game chains spawn from economy state (not just map position) to prevent discovery dry-up. Hash-affecting (gates: GATE.T48.ANOMALY.*)
- EPIC.T48.MAINTENANCE_TREADMILL.V0 [TODO]: Fleet upkeep drain per dynamic_tension_v0.md Pillar 2 — standing still costs money with ~50-80 tick runway if idle. FleetUpkeepSystem processes per-tick costs (fuel consumption, crew wages, hull degradation, module wear). Costs scale with ship class and module count. Visible in HUD (burn rate indicator) and dock economy panel (projected runway). Creates the "pain before relief" pressure that makes automation feel earned — manual trade barely covers upkeep, automation generates surplus. Upkeep pauses during dock (safe harbor). Per dynamic_tension_v0.md "Maintenance Treadmill" pillar. Hash-affecting (gates: GATE.T48.TENSION.*)
- EPIC.T52.AUDIT_FIXES.V0 [DONE]: Audit-driven fixes from full game audit (2026-03-23) — economy trade diversity (E4), tariff wiring, upkeep tuning, dread exposure scaling + secondary stressors, narrative dialogue extraction to JSON + voice differentiation + sequence variants, NPC combat labels + target highlight + hitstop, discovery UI (scanner vis, phase markers, breadcrumbs, milestone cards). 23 gates across 5 epics. (gates: GATE.T52.*). Closed T67: 18/21 DONE + 3 SUPERSEDED (STRESS_PROOF, COMBAT_VISUAL_PROOF, AUDIT_DELTA).
- EPIC.T68.FH_QUALITY.V0 [DONE]: First-hour quality pass driven by fh_12 audit (2026-03-28) — route grind fix (per-good dampening, exploration bonus), economy sinks (percentage-based transaction tax), competence margin fix (-64%->-20%), FO ambient content expansion (50+ lines, target max_silence<50), pacing event interrupts (break 120-action monotone), combat VFX (damage flash, vignette, shake, banner), credit feedback animation, keybind hints, toast type differentiation, lane 3D visibility, FPS optimization pass 2, content completeness validator. 20 gates. Retroactive fh_12 fixes: NPC respawn stats, FO silence content, transit fee parity. **Closed T68**: 20/20 gates DONE. fh_13 score: 4.6/5.0 avg. Key improvements: combat_one_shot 60%->0%, loot_rate 40%->100%, longest_streak 86->10. Remaining systemic (T69): route grind (diversity 0.21), economy sinks (exponential credit curve), FO decision-based silence. (gates: GATE.T68.*)
- EPIC.T70.DANGER_GRADIENT.V0 [TODO]: Exploration danger gradient + fundamental fixes driven by fh_11/fh_14 audit. Three layers: (1) Fix broken fundamentals — market price volatility, munitions price floor, FO LRU token selection, discovery seeding, combat juice VFX, competence margin decay. (2) Travel danger gradient — Node.ThreatLevel (BFS 0-5 from Haven), NPC difficulty scaling, trade margin risk premium, FO threat-reactive dialogue, lane transit events, galaxy map threat visualization. (3) Progression hooks — discovery placement by TL, contract tests, fh_15 re-audit. Design doc: docs/design/travel_danger_gradient_v0.md. 21 gates. (gates: GATE.T70.*)
- EPIC.T69.FH_VERIFICATION.V0 [DONE]: First-hour verification pass driven by fh_14 audit (2026-03-28) — VFY bot V3 (FO promotion, 3-method tracking, extended runtime), route diversity V3 (faster decay 80→50, harsher exponential 1000→2000 bps/sq), competence margin V3 (10x experienced decay, 70% late variance), economy sinks V3 (trade tax 3.5%), streak breaker V2 (monotone 15→10, event interrupt 20→15), safety net (waive passive costs <500cr), test adaptation (upkeep tests + golden hashes + balance baseline). 8 gates. **Closed T69**: all gates DONE. (gates: GATE.T69.*)
- EPIC.T41.AUDIT_FEEL_FIXES.V0 [TODO]: First-hour feel fixes driven by fh_4 audit (2026-03-26) — combat VFX regression + tuning, FO ambient cadence + silence cap, spatial clarity (galaxy map marker, lane gates, arrival orientation), juice feedback (trade buy/sell, module install), UI theme (panel chrome, font hierarchy, tabular numerals, faction colors, control hints), pacing (moment spacing), stability (seed 1001 timeout). 21 gates targeting 3 CRITICAL + 10 MAJOR issues for EA readiness. Design: fo_trade_manager_v0.md, dynamic_tension_v0.md, camera_cinematics_v0.md, first_hour_rubric.md. (gates: GATE.T41.*)
- EPIC.T53.AI_PLAYTEST.V0 [TODO]: Full-game AI playtesting infrastructure — scripted victory-path bot (playthrough_bot_v0.gd) + expanded RL agent (rl_agent_bot.gd ~120 actions, ~500-dim obs) covering all player verbs (trade, combat, modules, haven, missions, diplomacy, research, construction, megaprojects, fragments, fracture travel). Missing bridge endpoints filled (StartResearchLabSlotV0, StartFabricationV0). Scripted bot proves game completable. RL bot covers full action space for difficulty tuning. Python Gymnasium + PPO harness. Bot analytics + findings report. 15 gates. (gates: GATE.T53.*)
- EPIC.S9.L10N_DECISION.V0 [DONE]: Localization architecture decision — English-only v1.0 (EA launch). **T41**: Decision documented in 57_RUNBOOK.md. String audit: ~786 hardcoded English strings across UI/bridge/content. Extraction-ready patterns documented for future L10N (Godot tr(), DisplayNameRegistry, content keys) (gates: GATE.S9.L10N.*)

Status: IN_PROGRESS — 6 DONE epics (Balance Lock, Settings, Menu Atmosphere, Accessibility, Milestones/Credits, L10N Decision). 3 IN_PROGRESS (Save, Mission Ladder, Steam). Remaining TODO: UI, Mod, Perf, Content Waves, Music, Telemetry, Market Depth, Program Postmortems, Template Missions, Faction Storylines, Mission Polish.
