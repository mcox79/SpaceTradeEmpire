# Active Issues

Living tracker for known bugs, visual gaps, and polish items discovered through
playtesting, screenshot evals, bot runs, and manual inspection. This file is
referenced by `/gen-gates` (to prioritize work) and `/closeout-gate` (to mark
issues FIXED when a gate addresses them).

**Rules:**
- Add issues as they're discovered (evals, playtesting, user reports)
- When a gate fixes an issue, change status to FIXED and add the gate ID
- Periodically prune FIXED items (move to bottom archive or delete)
- Severity: CRITICAL = visually broken/unplayable, HIGH = harms experience,
  MEDIUM = noticeable, LOW = polish/nice-to-have

---

## Runtime Errors

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| R1 | CRITICAL | hud.gd fails to parse — `ScreenEdgeTint` identifier not declared. Entire HUD non-functional: no Zone G bottom bar, no risk meters, no proper status display. Only fallback text visible. Likely caused by in-progress tranche work on `screen_edge_tint.gd` not yet registered | FIXED | GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| R2 | HIGH | Faction color crash loop — `ArgumentOutOfRangeException: Invalid Color Name: (0.6, 0.2, 0.8, 1.0)` fires 14+ times per session. Communion faction primary color from FactionTweaksV0.cs:147 passed as string color name instead of Color object. Likely breaks faction territory visuals | FIXED | GATE.S7.RUNTIME_STABILITY.FACTION_COLOR.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |

## Combat

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| C1 | CRITICAL | Ships disappear on death — no explosion VFX, no debris, no kill confirmation | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | COMBAT_FEEL.001 eval |
| C2 | CRITICAL | Combat audio pool (16 spatial players) allocated but never called — combat is silent | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | COMBAT_FEEL.001 eval |
| C3 | HIGH | No floating damage numbers — hits register only on HP bar, violating "3 channels per hit" | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | COMBAT_FEEL.001 eval |
| C4 | HIGH | No shield vs hull visual distinction — no ripple (shield) or spark shower (hull) | FIXED | GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001 | COMBAT_FEEL.001 eval |
| C5 | HIGH | Combat VFX (bullets, impacts, NPC HP bars) invisible at default camera altitude 80 | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | COMBAT_FEEL.001 eval |
| C10 | HIGH | Combat VFX still effectively invisible at game altitude — bot dealt 20 dmg in 5 hits but damage numbers, shield ripples, weapon trails, impact particles not visible in any of 3 combat frames. C5 4x scale fix may be insufficient or camera at different altitude than tested | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_SCALE.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| C6 | MEDIUM | No shield break moment — shields hitting 0 has no flash/sound/hitstop | FIXED | GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001 | COMBAT_FEEL.001 eval |
| C7 | MEDIUM | All weapon families look identical — kinetic/energy/neutral/PD share same visual | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WEAPON_FAMILIES.001 | COMBAT_FEEL.001 eval |
| C8 | LOW | Zone armor (4-zone system) invisible to player — no HUD representation | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001 | COMBAT_FEEL.001 eval / Fixed 2026-03-11 |
| C9 | LOW | No combat HUD — no crosshair, cooldown indicator, or stance display | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_HUD.001 | COMBAT_FEEL.001 eval / Fixed 2026-03-11 |
| C11 | HIGH | Combat VFX not visible in eval screenshots — bot dealt 20 dmg (5 hits) but npc_combat_f01-f03 show no damage numbers, shield ripples, weapon trails, or impact particles at game altitude. Kill explosion (white spheres) visible in f03 only. Previous COMBAT_VFX_SCALE fix may be insufficient or bot camera at different altitude | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| C12 | MEDIUM | No NPC HP bars visible at game altitude — npc_closeup shows fleet ship at close range but no HP bar despite PLAYER_VISIBLE_SYSTEMS spec (scale 5.0x0.4, emission 3.0) | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| C13 | MEDIUM | No NPC role labels visible — NPC ship in npc_closeup has no role text label (spec: font 48, pixel_size 0.05) | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| C14 | MEDIUM | No hostile labels on NPC ships — despite combat engagement, no red HOSTILE text visible on target NPC (spec: visible at 120u) | FIXED | GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |

## Visual / Polish

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| V4 | CRITICAL | Giant green planet fills ~60% of screen after warp to star_9 — camera positioned inside or too close to planet. Persists through docking, empire dashboard, and next warp (6 frames affected) | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_ARRIVAL.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V5 | CRITICAL | Galaxy map view broken — large green 3D elements (labels, node markers, or overlays at wrong scale) obscure entire view when toggled while docked | FIXED | GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V6 | HIGH | Empire dashboard background is solid bright green (the misrendered planet from V4) — extremely jarring. Visible in 4 consecutive frames | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_ARRIVAL.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V7 | HIGH | Camera stays at galactic altitude (~2488) after warp — destination system appears as just a blue dot with thread line, no system detail visible. Camera doesn't zoom into arrived system | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_ARRIVAL.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V8 | HIGH | No faction territory visual indicators visible in any frame — no boundary lines, no territory shading, no regime tinting, despite EPIC.S7.FACTION_VISUALS.V0 DONE. Possibly caused by R2 faction color crash | FIXED | GATE.S7.RUNTIME_STABILITY.FACTION_COLOR.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V9 | HIGH | NPC fleet ships rarely visible during normal play — 21 in galaxy per dashboard but zero naturally encountered. Bot had to navigate specifically to find one. Final system had fleet_count=0. Sim-substantiated ships not rendering or too spread/small | FIXED | GATE.S7.RUNTIME_STABILITY.SHIP_VISIBILITY.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V1 | HIGH | Player ship small at default camera altitude — scale 0.35 helps but still hard to spot | FIXED | GATE.S7.RUNTIME_STABILITY.SHIP_VISIBILITY.001 | Playtesting 2026-03-10 / Fixed 2026-03-11 |
| V2 | MEDIUM | Starter system feels barren — content IS spawned (star, planet, station, NPCs, dust) but spread over large area at small scale | FIXED | GATE.X.UI_POLISH.LOCAL_DENSITY.001 | Playtesting 2026-03-10 / Fixed 2026-03-11 |
| V3 | MEDIUM | Station label text overlaps/truncates when multiple resource types in name | FIXED | GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001 | Screenshot eval |
| V10 | MEDIUM | Warp VFX underwhelming — blue sphere + ring of dots across 4 frames. Functional but not dramatic. Not the "warp tunnel" moment the design calls for | FIXED | GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V11 | MEDIUM | 3D Label3D nodes render through 2D dock panel — "System 10 (RareMin)(Mining)... Station" renders through dock menu in 12+ frames. Z-ordering mismatch between 3D world and 2D UI. Previously FIXED but regressed | FIXED | GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001 | Full eval 2026-03-10 / Fixed 2026-03-11 / Regressed / Re-fixed 2026-03-11 |
| V12 | MEDIUM | 1 dead particle system — aesthetic audit: emitting=2 / total=3 | FIXED | GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V13 | LOW | No visible asteroids in home system — only System 11 shows particle debris | FIXED | GATE.S7.RUNTIME_STABILITY.ASTEROID_VARIETY.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V14 | HIGH | Warp VFX appears as kill explosion debris (white/gray spheres), not dramatic tunnel — no color-shifting walls, streaking stars, or speed lines visible. warp_vfx_f01-f04 all show same sphere cluster. V10 fix may not have been effective or warp tunnel shader not rendering | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| V15 | MEDIUM | Warp transit frames show dock panel instead of tunnel — warp_transit_f01-f03 all show market tab still open with station behind it. No warp tunnel VFX visible during actual transit phase | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| V16 | MEDIUM | Galaxy map not visible behind empire dashboard — galaxy_map frame shows only dashboard overlay on dark/pink background, no star nodes, threads, or map features visible beneath | FIXED | GATE.S7.RUNTIME_STABILITY.GALAXY_MAP_FIX.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| V17 | HIGH | Label stacking/overlap — no anti-collision logic in Label3D system. System name + "HOSTILE" + station label render on top of each other at same camera distance. ClampLabelsRecursive manages per-label visibility by distance but has zero overlap avoidance | FIXED | GATE.X.UI_POLISH.LABEL_OVERLAP.001 | Eval 2026-03-10 (T27) / Fixed 2026-03-11 |
| V18 | HIGH | Galaxy map default zoom shows only 1 node — at current camera position only the current system node visible with lane lines going off-screen. Cannot plan routes or see galaxy topology. May be camera altitude issue | FIXED | GATE.X.UI_POLISH.GALAXY_MAP_UX.001 | Eval 2026-03-10 (T27) / Fixed 2026-03-11 |
| V19 | MEDIUM | "GALAXY MAP (TAB to close)" text persists when map overlay is closed — label visible in 6+ frames where bot has explicitly toggled map off. UI label not hidden with map toggle | FIXED | GATE.X.UI_POLISH.GALAXY_MAP_UX.001 | Eval 2026-03-10 (T27) / Fixed 2026-03-11 |
| V20 | MEDIUM | Ship tab (services) layout flat/uninspiring — zone armor bars are plain gray lines, module slots list "Empty" with no icons or visual grouping. Reads like debug output, not ship customization UI | FIXED | GATE.X.UI_POLISH.DOCK_VISUAL.001 | Eval 2026-03-10 (T27) / Fixed 2026-03-11 |
| V21 | LOW | "Sal" column header truncated in market tab — "Sell" header cut off, likely column width too narrow | FIXED | GATE.X.UI_POLISH.DOCK_VISUAL.001 | Eval 2026-03-10 (T27) / Fixed 2026-03-11 |

## UI / UX

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| U2 | HIGH | HUD values never populate — Credits stuck at 0, System always "?", State always "?" across all 24 frames. Consequence of R1 hud.gd parse failure | FIXED | GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U3 | HIGH | No Zone G bottom bar — risk meters, minimap slot, bottom bar framework all absent. Consequence of R1 | FIXED | GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U4 | MEDIUM | Cargo shows "empty" immediately after purchase — UI refresh delay. Correct value appears ~200 ticks later | FIXED | GATE.S7.RUNTIME_STABILITY.UI_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U5 | MEDIUM | Toast notifications stack tightly — 5 toasts in top-right with minimal spacing, hard to parse individually | FIXED | GATE.S7.RUNTIME_STABILITY.UI_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U6 | LOW | Empire dashboard content sparse — placeholder messages ("None — visit a station"), limited orientation value for new players | FIXED | GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U7 | LOW | No persistent keybinding hints in flight — welcome text fades, no reminder of Tab/E/H bindings | FIXED | GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U1 | HIGH | CAMERA_TOO_FAR — bot camera reaches 3853u in late phases, well beyond label visibility cutoff (150u). tick_200 and final frames show empty starfield with zero game content visible. Labels, ships, planets all hidden at this distance. Needs camera distance bounds or auto-zoom to local system | FIXED | GATE.X.UI_POLISH.CAMERA_BOUNDS.001 | Aesthetic audit + Eval 2026-03-10 (T27) / Fixed 2026-03-11 |

## Audio

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| A1 | CRITICAL | Combat fire/impact SFX never play — AudioStreamPlayers exist but play methods not called | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | COMBAT_FEEL.001 eval (same as C2) |

## First Hour / Onboarding — Flagged for Later

Identified during aggressive screenshot eval against Freelancer/Elite/Starsector.
These require significant effort (new systems, art assets, or audio) and are
deferred to future tranches.

| # | Severity | Issue | Effort | Source |
|---|----------|-------|--------|--------|
| F1 | MEDIUM | No cost-basis tracking in cargo — player can't see profit/loss per good without remembering buy price. Every competitor (X4, Elite, Freelancer) shows this | Large — needs SimCore accounting entity (buy price per good per batch) | Screenshot eval 2026-03-11 |
| F2 | MEDIUM | FO panel has no portrait/avatar — dialogue appears as plain text. Competitors use character portraits for emotional connection (Freelancer, Starsector) | Medium + art — needs 3 character art assets + UI redesign | Screenshot eval 2026-03-11 |
| F3 | LOW | No voiced FO reactions or audio cues — all feedback is visual text. Audio responses dramatically improve engagement (Freelancer's Trent/Juni voices) | Large + audio — needs audio pipeline + Sound Manager addon | Screenshot eval 2026-03-11 |
| F4 | MEDIUM | Warp arrival lacks drama — current flyby works functionally but doesn't create the "wow" moment of Freelancer's jump gate arrivals or Elite's witch-space exit | Medium — needs camera choreography overhaul | Screenshot eval 2026-03-11 |
| F5 | MEDIUM | No persistent objective/quest log UI — player has only edgedar waypoint + mission HUD panel. No way to review past/current objectives. Starsector's intel screen is the benchmark | Medium — needs dedicated panel design | Screenshot eval 2026-03-11 |
| F6 | HIGH | All stations look identical — every station uses the same model with no faction visual identity. Freelancer's stations are instantly recognizable by faction. Starsector has unique station sprites | Large + art — needs per-faction station models/color schemes | Screenshot eval 2026-03-11 |
| F7 | HIGH | Warp tunnel cone oversized — fills 40-50% of screen during transit, no ship visible, no spatial reference. Needs camera pullback + tunnel radius reduction (currently 8u radius, 200u height) | Medium — warp_tunnel.gd radius/camera rework | Eval iter3 2026-03-11 |
| F8 | HIGH | HUD absent during warp transit — no destination name, no ETA, no "IN TRANSIT" indicator. Player has zero info during most frequent gameplay transition | Medium — needs transit-specific overlay state in hud.gd | Eval iter3 2026-03-11 |
| F9 | HIGH | Warp VFX undersells departure — expanding sphere reads as "bright light appeared" not "initiating warp". No directional movement, no ship visible, no tunnel/streaks | Medium — warp_effect.gd needs directional streaks + ship visibility | Eval iter3 2026-03-11 |
| F10 | MEDIUM | Galaxy map nearly empty — one green dot, thin blue lines, plain black background. No node variety, no faction colors, no size/color differentiation, no strategic info | Large — needs node visual diversity, faction overlay, economic hints | Eval iter3 2026-03-11 |
| F11 | MEDIUM | Empire Dashboard "Needs Attention" reads as errors — "! No automation running" in bright red feels like failure, not onboarding invitation | Small — UX copywriting pass on dashboard prompts | Eval iter3 2026-03-11 |
| F12 | MEDIUM | Market production text unformatted — "[Mine] Extract Ore [fuel:1, ore:0] eff:100%" renders as dense inline text, not structured production chain | Medium — needs production chain visualization component | Eval iter3 2026-03-11 |

## Resolved (Archive)

| # | Issue | Status | Gate | Date |
|---|-------|--------|------|------|
| R1-old | Station keeps orbiting when player is docked | FIXED | — (hotfix) | 2026-03-10 |
| R2-old | Screenshot runner doesn't clean up old video/audio files | FIXED | — (hotfix) | 2026-03-10 |
