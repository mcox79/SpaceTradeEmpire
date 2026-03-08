# /visual-eval — Visual Sweep Evaluation Skill

Runs the visual sweep bot, reads all captured screenshots, and performs aggressive
multi-perspective evaluation.

## Trigger

User says `/visual-eval` or asks to "run the visual sweep" or "evaluate screenshots."

## Steps

### 1. Build & Run Sweep

```bash
cd "c:\Users\marsh\Documents\Space Trade Empire"
dotnet build "Space Trade Empire.csproj" --nologo -v q
rm -f reports/visual_eval/*.png reports/visual_eval/*.json
timeout 75 "C:\Godot\Godot_v4.6-stable_mono_win64.exe" --path . \
  -s "res://scripts/tests/visual_sweep_bot_v0.gd" 2>&1 \
  | grep -E "^(VSWP|EXPV0|SCRIPT ERROR|ERROR|.*Parse Error)" | head -80
```

Verify: count PNGs in `reports/visual_eval/`. Expect 20-24 screenshots.
If 0 screenshots, check for SCRIPT ERROR in output — likely a parse error.

### 2. Read All Screenshots

Read every `*_1772*.png` (latest timestamp batch) using the Read tool.
Read in parallel batches of 6 for efficiency.

### 3. Evaluate from Four Perspectives

For EACH screenshot, apply these four lenses. Be harsh — this is about finding
problems, not confirming things work.

#### A. Player First Impression
- Would a new player understand what they're seeing?
- Does this screen make them want to keep playing?
- Is there a "wow" moment or is it flat/boring?
- Does the visual quality match the price point of games they compare to?

#### B. Art Director
- **Composition**: Is the frame balanced? Is there a focal point?
- **Color**: Is there a coherent palette? Do things contrast against space?
- **Visual hierarchy**: What does the eye go to first? Is that the right thing?
- **Readability**: Can you tell what each object IS at a glance?
- **Label/text rendering**: Any overlap, clipping, wrong size, wrong position?
- **Lighting**: Does the scene feel lit, or flat/unlit?
- **Object scale**: Do ships/stations/planets feel the right relative size?

#### C. UX Designer
- **Information density**: Too much? Too little? Right amount?
- **Text readability**: Font size, contrast, background for legibility
- **Toast/notification visibility**: Can the player notice them?
- **Menu layout**: Alignment, spacing, visual grouping, tab clarity
- **Feedback loops**: When the player acts, do they see a response?
- **State communication**: Does the HUD tell you what state you're in?
- **Debug leaks**: Any developer-facing text visible to the player?

#### D. Game Designer
- **Feeling**: Does this moment create the intended emotion?
  - Boot → wonder/exploration
  - Dock → commerce/safety
  - Combat → tension/danger
  - Warp → speed/journey
  - Galaxy map → strategic overview/scale
  - Empire dashboard → accomplishment/planning
- **Differentiation**: Do different systems/stations feel different?
- **Progression signal**: Can you see your progress in the world?
- **Drama**: Are dramatic moments (warp, combat, discovery) visually dramatic?
- **Pacing**: Does the visual intensity match the gameplay intensity?

### 4. Output Format

Produce a severity-ranked issue table:

| # | Severity | Issue | Screenshots |
|---|----------|-------|-------------|
| 1 | CRITICAL | Description | which frames |
| 2 | HIGH | Description | which frames |
| ... | MEDIUM/LOW | ... | ... |

**Severity definitions:**
- **CRITICAL**: Visually broken — overlapping UI, invisible elements, crashes appearance
- **HIGH**: Significantly harms the player experience — boring, confusing, or ugly
- **MEDIUM**: Noticeable but not dealbreaking — could improve but playable
- **LOW**: Minor polish — nice-to-have improvements

Also note **what's working well** — things that should be preserved or expanded.

### 5. Comparison (if prior run exists)

If there are screenshots from a prior run (different timestamp in the same
directory), compare equivalent phases to track improvement/regression.

## Bot Reference

The sweep bot is at `scripts/tests/visual_sweep_bot_v0.gd`.
It captures these phases (24 screenshots typical):

| Phase | What it shows |
|-------|--------------|
| boot | First view of starting system |
| dock_market | Station market tab |
| dock_jobs | Station jobs/missions tab |
| dock_services | Station services tab |
| post_buy | Market after purchasing a good |
| flight_cargo | Undocked, flying with cargo |
| npc_closeup | Close-up of NPC fleet |
| npc_combat_f01-f03 | Combat burst (3 frames) |
| warp_vfx_f01-f04 | Warp-in effect burst (4 frames) |
| galaxy_map | Galaxy map overlay |
| empire_dashboard | Empire dashboard overlay |
| warp_transit_f01-f03 | Lane transit burst (3 frames) |
| system_2 | Arrived at second system |
| system_2_dock | Docked at second system station |
| system_3 | Docked at third system station |
| tick_200 | Close-up variety shot at tick 200 |
| final | Wide shot with aesthetic audit |
