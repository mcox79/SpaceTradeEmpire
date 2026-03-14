# EA Readiness Report — 2026-03-13

## Executive Summary

Space Trade Empire is approaching Early Access readiness with strong core systems,
comprehensive test coverage, and a clear remaining work list. The primary gaps are
in narrative content depth, some advanced UI polish, and Steam platform finalization.

---

## 1. Save Robustness

| Area | Status | Notes |
|------|--------|-------|
| Save format | DONE | SaveEnvelope v2 with Version, Seed, State |
| Migration pipeline | DONE | v1→v2 migration chain, extensible for future versions |
| Corruption detection | DONE | TryDeserializeSafe — malformed JSON, truncated, missing fields |
| Post-load validation | DONE | Fleet→node, edge→node, credits, megaproject references |
| Bridge integrity check | DONE | GetSaveIntegrityV0(slot) for UI |
| Auto-save | TODO | Timer-based auto-save not yet implemented |
| Cloud saves | TODO | Requires GodotSteam addon (placeholder wired) |

**Assessment**: Save system is robust for EA. Auto-save is a nice-to-have.

---

## 2. Balance Lock

| Area | Status | Notes |
|------|--------|-------|
| Tweak baseline snapshot | DONE | ~330 const values in balance_baseline_v0.json |
| Drift detection | DONE | BalanceLockTests compare against baseline |
| TweakRoutingGuard | DONE | Scans SimCore systems for unrouted numeric literals |
| Allowlist discipline | DONE | Only structural constants allowed |

**Assessment**: Balance is locked. Any gameplay value change will be detected.

---

## 3. Content Completeness

| System | Status | Depth |
|--------|--------|-------|
| Trade goods | DONE | 13 goods, 9 production chains, 4 price tiers |
| Ship classes | DONE | 8 classes (Shuttle→Dreadnought), 3-constraint fitting |
| Modules | DONE | T1 + T2 (40 faction-specific) + T3 (13 discovery-only) |
| Factions | DONE | 5 factions with doctrine, tariffs, reputation |
| Missions | IN_PROGRESS | 6 content missions + mission evolution (trigger, branching, failure) |
| Megaprojects | IN_PROGRESS | 3 types (FractureAnchor, TradeCorridor, SensorPylon), UI done |
| Haven | IN_PROGRESS | 5-tier upgrades, research lab, drydock, market, keeper, fabricator |
| Discoveries | DONE | Scan→Analyze→Outcome pipeline, 6 types, loot, celebration |
| Data logs | DONE | 25 logs, BFS placement, Kepler chain |
| Knowledge graph | IN_PROGRESS | Templates exist, seeding pipeline fixed (T41) |
| Narrative NPCs | DONE | First Officer (4 tiers), War Faces (Regular/Enemy), ghost mentions |
| Story state machine | DONE | 5 revelations, cover-story naming, pentagon break |

**Assessment**: Core content is sufficient for EA. Narrative depth (Haven logs,
endgame narratives) is the main content gap.

---

## 4. Performance Budget

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Test count | 1350 | — | Healthy |
| Full test time | ~69s | <120s | PASS |
| Determinism tests | 31, all PASS | — | Stable |
| SimCore LOC | ~25K | — | Manageable |
| Bridge + UI LOC | ~30K | — | Manageable |
| Tick time | Not profiled | <16ms (60fps) | NEEDS PROFILING |
| Memory | Not profiled | <512MB | NEEDS PROFILING |

**Assessment**: Need performance profiling pass before EA launch. Test suite is
healthy and fast.

---

## 5. Steam Status

| Area | Status | Notes |
|------|--------|-------|
| steam_appid.txt | DONE | Placeholder (480 = Spacewar) |
| Init + fallback | DONE | Graceful degradation when Steam not present |
| Achievements | DONE | 18 milestones mapped to Steam achievement IDs |
| GodotSteam addon | TODO | Not yet installed (placeholder wiring only) |
| Cloud saves | TODO | Requires addon |
| Build pipeline | TODO | Godot 4 + C#/.NET 8 export templates |

**Assessment**: Steam integration is structured but not testable until addon is
installed. This is a distribution blocker.

---

## 6. Code Quality

| Metric | Value |
|--------|-------|
| Gate velocity | 5.4/day avg, 21/tranche burst |
| Total gates DONE | ~930+ |
| Total gates TODO | ~53 |
| Epics DONE | ~264 |
| Epics IN_PROGRESS | ~20 |
| Epics TODO | ~50 |
| Golden hash stability | 31 determinism tests, all PASS |
| Invariant guards | TweakRoutingGuard, RoadmapConsistency, BalanceLock, SystemIO ban |

---

## 7. Critical Path to EA

### Must-have (blockers)
1. **GodotSteam addon install** — can't ship without Steam integration
2. **Performance profiling** — tick time + memory budget validation
3. **Build pipeline** — Godot 4 export with C#/.NET 8

### Should-have (polish)
4. Auto-save
5. Megaproject galaxy map markers
6. Haven visual tiers
7. Narrative content expansion (Haven logs, endgame narratives)
8. Music/audio stems

### Nice-to-have (post-EA)
9. Localization
10. Mod support
11. Cloud saves
12. Telemetry

---

## 8. Recommendation

**EA target: achievable with 2-3 more tranches** focused on:
- T42: Steam finalization + performance profiling + build pipeline
- T43: Content polish + narrative depth + remaining UI gaps
- T44: Final QA pass + store page assets
