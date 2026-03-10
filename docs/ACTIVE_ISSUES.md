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
| V2 | MEDIUM | Starter system feels barren — content IS spawned (star, planet, station, NPCs, dust) but spread over large area at small scale | OPEN | — | Playtesting 2026-03-10 |
| V3 | MEDIUM | Station label text overlaps/truncates when multiple resource types in name | FIXED | GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001 | Screenshot eval |
| V10 | MEDIUM | Warp VFX underwhelming — blue sphere + ring of dots across 4 frames. Functional but not dramatic. Not the "warp tunnel" moment the design calls for | FIXED | GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V11 | MEDIUM | 3D Label3D nodes render through 2D dock panel — "System 10 (RareMin)(Mining)... Station" renders through dock menu in 12+ frames. Z-ordering mismatch between 3D world and 2D UI. Previously FIXED but regressed | FIXED | GATE.S7.RUNTIME_STABILITY.LABEL3D_FIX.001 | Full eval 2026-03-10 / Fixed 2026-03-11 / Regressed / Re-fixed 2026-03-11 |
| V12 | MEDIUM | 1 dead particle system — aesthetic audit: emitting=2 / total=3 | FIXED | GATE.S7.RUNTIME_STABILITY.VFX_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V13 | LOW | No visible asteroids in home system — only System 11 shows particle debris | FIXED | GATE.S7.RUNTIME_STABILITY.ASTEROID_VARIETY.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| V14 | HIGH | Warp VFX appears as kill explosion debris (white/gray spheres), not dramatic tunnel — no color-shifting walls, streaking stars, or speed lines visible. warp_vfx_f01-f04 all show same sphere cluster. V10 fix may not have been effective or warp tunnel shader not rendering | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| V15 | MEDIUM | Warp transit frames show dock panel instead of tunnel — warp_transit_f01-f03 all show market tab still open with station behind it. No warp tunnel VFX visible during actual transit phase | FIXED | GATE.S7.RUNTIME_STABILITY.WARP_TUNNEL_V2.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |
| V16 | MEDIUM | Galaxy map not visible behind empire dashboard — galaxy_map frame shows only dashboard overlay on dark/pink background, no star nodes, threads, or map features visible beneath | FIXED | GATE.S7.RUNTIME_STABILITY.GALAXY_MAP_FIX.001 | Full eval 2026-03-11 / Fixed 2026-03-11 |

## UI / UX

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| U2 | HIGH | HUD values never populate — Credits stuck at 0, System always "?", State always "?" across all 24 frames. Consequence of R1 hud.gd parse failure | FIXED | GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U3 | HIGH | No Zone G bottom bar — risk meters, minimap slot, bottom bar framework all absent. Consequence of R1 | FIXED | GATE.S7.RUNTIME_STABILITY.HUD_PARSE.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U4 | MEDIUM | Cargo shows "empty" immediately after purchase — UI refresh delay. Correct value appears ~200 ticks later | FIXED | GATE.S7.RUNTIME_STABILITY.UI_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U5 | MEDIUM | Toast notifications stack tightly — 5 toasts in top-right with minimal spacing, hard to parse individually | FIXED | GATE.S7.RUNTIME_STABILITY.UI_POLISH.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U6 | LOW | Empire dashboard content sparse — placeholder messages ("None — visit a station"), limited orientation value for new players | FIXED | GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U7 | LOW | No persistent keybinding hints in flight — welcome text fades, no reminder of Tab/E/H bindings | FIXED | GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001 | Full eval 2026-03-10 / Fixed 2026-03-11 |
| U1 | LOW | CAMERA_TOO_FAR aesthetic warning fires at galactic scale (distance=2500+) — metric measures to galactic objects, not local system | OPEN | — | Aesthetic audit |

## Audio

| # | Severity | Issue | Status | Gate | Source |
|---|----------|-------|--------|------|--------|
| A1 | CRITICAL | Combat fire/impact SFX never play — AudioStreamPlayers exist but play methods not called | FIXED | GATE.S7.COMBAT_FEEL_POLISH.WIRE.001 | COMBAT_FEEL.001 eval (same as C2) |

## Resolved (Archive)

| # | Issue | Status | Gate | Date |
|---|-------|--------|------|------|
| R1-old | Station keeps orbiting when player is docked | FIXED | — (hotfix) | 2026-03-10 |
| R2-old | Screenshot runner doesn't clean up old video/audio files | FIXED | — (hotfix) | 2026-03-10 |
