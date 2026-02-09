\# Space Trade Empire

\## Early Mission Ladder and Travel/Exploration Design Laws (Canonical)



\### Status

Canonical design guidance for early scripted missions, travel rules, and the off-lane exploration layer. Intended to drive implementation, tooling, and content authoring.



See also:

\- `docs/20\_TESTING\_AND\_DETERMINISM.md`

\- `docs/30\_CONNECTIVITY\_AND\_INTERFACES.md`

\- `docs/10\_ARCHITECTURE\_INDEX.md` (ARCH-006 for runners, ARCH-002 for determinism contract)



---



\## 1. Goals for early scripted missions

Early missions are not narrative-first content. They are deterministic vertical slices that:



1\) Force core systems to exist (contracts, travel, docking, market buy/sell, inventory, transfers).

2\) Define core UI flows (accept, navigate, act, complete, understand outcomes).

3\) Validate a deterministic mission runner and headless testability.

4\) Unlock empire levers in a deliberate progression: manual loop once, then automation unlock.



Key principle:

\- The first time the player learns a loop, they do it manually.

\- Immediately after, the game unlocks automation of that same loop (freighters, patrol assignments, station supply routes).



---



\## 2. Mission archetypes (reusable primitives)

Early mission content must be composed from these archetypes:



1\) Deliver X to Y (trade, construction supplies, research supplies)

2\) Extract X from node (mining)

3\) Defend or escort route (patrol)

4\) Investigate anomaly (research, fracture discovery)



Avoid bespoke mechanics per mission until after these archetypes are stable.



---



\## 3. Mission ladder (system unlock plan)

Each mission introduces exactly one major new capability, then unlocks automation for it.



\### M1: Matched Luggage (trade intro % automation unlock)

Unlocks:

\- Basic trade route automation via hireable freighter contracts



Forces (minimal systems):

\- Mission accept/track/complete

\- Lane booking UI (with capacity display)

\- Docking events

\- Cargo transfer to NPC

\- Market buy/sell

\- Profit summary UI

\- Freighter hire contract creation (periodic payout)



Scope constraints:

\- Deterministic profit trade. No RNG dependency for tutorial success.

\- Lane capacity is shown and explained, but does not block the tutorial mission.



\### M2: Mining (extraction intro % mining automation unlock)

Unlocks:

\- Mining activity

\- Mining freighter contract type (passive extraction loop)



Forces:

\- Resource node discovery (1 node)

\- Time-based extraction (no deep sim)

\- Haul to a single sale/refine destination



Scope constraints:

\- 1 mineral, 1 node, binary “mining fitted” requirement.



\### M3: Patrol (route security intro % escort automation unlock)

Unlocks:

\- Patrol/escort assignments for freighters

\- Route risk legibility (before/after expected value)



Forces:

\- Deterministic scripted attack event

\- Escort contract role that reduces expected loss or reduces disruption



Scope constraints:

\- Early harm model is mild: cargo loss and delay before any ship destruction mechanics.



\### M4: Construction (build outpost intro % station supply automation unlock)

Unlocks:

\- Construction supply chain to a build queue

\- Outpost or station module existence



Forces:

\- Build site + fixed recipe

\- Delivery into build queue

\- Station exists as a location type



Scope constraints:

\- No station designer yet.

\- Only 1 buildable module type (eg “Extractor Outpost”).



\### M5: Research (anomaly intro % research pipeline unlock)

Unlocks:

\- Research pipeline at a science station

\- Tech unlock as a real gameplay effect

\- Anomaly maintenance/upkeep loop



Forces:

\- Anomaly entity with stable identity

\- Science station module

\- Single active research project at a time

\- Resource-fed progress bar



Scope constraints:

\- No multi-stream research until later.



\### M6: Fracture Drive (off-lane layer intro % mystery layer unlock)

Unlocks:

\- Practical off-lane exploration

\- New map layer of mysteries between lanes

\- Existential-threat attention vector (gated and controlled)



Forces:

\- Interdiction encounter

\- Salvage and attach damaged drive module

\- Repair/research pipeline at science station

\- Practical off-lane traversal rules and constraints



Scope constraints:

\- Must be gated behind having science station capability (from M5).

\- Must not trivialize lanes, capacity, and patrol gameplay.



---



\## 4. Mission 1 spec: Matched Luggage (final)

\### Player-facing beats

1\) Accept contract at Origin Port

2\) Book Star Lane to Destination

3\) Travel and dock

4\) Deliver luggage to recipient

5\) Recipient tips a trade opportunity (deterministic)

6\) Player buys tutorial commodity at Destination

7\) Player returns to Origin and sells for profit

8\) Freighters unlock with a prompt (Hire now / Later)



\### Lane fee policy for Mission 1 (no vouchers)

\- No voucher items unless vouchers become a permanent system later.

\- Lane fee exists and is shown.

\- Mission sponsor covers the fee for this run.

\- Booking UI must display: fee, sponsor coverage, net cost $0.



Anti-exploit law:

\- Sponsor coverage is single-use, route-bound, and consumed only by the mission-required booking.

\- If the player cancels that booking, the coverage is invalidated and does not re-apply.

\- Sponsor coverage must appear as a transparent accounting line item (not hidden).



\### Lane capacity policy for Mission 1

\- Capacity must be displayed and explained.

\- Tutorial lane must always have capacity to book successfully.

\- At least one other lane in the UI should be visibly full or congested to communicate scarcity without blocking progress.

\- UI text must explicitly state the tutorial lane is guaranteed for training.



\### Unlock behavior

\- After the player realizes profit by trading, unlock “Hire Freighter.”

\- Prompt: “Automate this run?” with options:

&nbsp; - Hire now

&nbsp; - Later

\- Do not force a modal tutorial that blocks the player.



---



\## 5. Time and travel design laws (fixed tick)

\### Global time

\- The simulation uses a fixed game-time tick.

\- The tick represents the same seconds of game-time always.

\- Game-time does not slow down or speed up depending on what the player is doing.

\- No timed missions by design.



\### Travel layers

There are two layers:



1\) Strategic travel layer (default)

\- Deterministic travel resolution: time = distance / speed

\- Player, NPC ships, and freighters can be represented without entering a physics bubble

\- Freighters remain abstracted in strategic travel (no physics bubble instantiation)



2\) Physics bubble layer (local interaction)

\- Used for docking, combat, salvage, anomalies, interdictions

\- Triggered only by arrivals, deterministic encounters, or explicit interaction regions



---



\## 6. Lanes vs off-lane travel rules

\### Lane travel

\- Lanes are the empire backbone for logistics, capacity constraints, and route warfare.

\- Lane travel speed is massively higher than off-lane travel (example: 1000x off-lane cruise).



\### Off-lane travel (baseline, any ship)

\- Off-lane travel is allowed for any ship.

\- Off-lane baseline travel is intentionally impractical for interstellar movement (example: on the order of a week of game-time).

\- Design intent: continuous off-lane cruising is dead time and not a normal strategy.



\### Fracture drive travel (practical off-lane)

\- Fracture drive makes off-lane traversal practically useful.

\- Fracture drive has its own speed and constraints.

\- Fracture drive unlocks the “between lanes” mystery layer.



---



\## 7. Off-lane guardrails (non-negotiable)

Because off-lane baseline travel can effectively self-lock a player, the game must include these protections:



\### 7.1 Off-lane commitment warning

\- If a player commits to off-lane travel that exceeds a configured threshold (in game-time), the game must show a clear warning and require confirmation.

\- Warning must include a clear statement that off-lane without fracture is intentionally impractical.



\### 7.2 Return mechanism (bounded, not free)

\- The player must have an always-available “Return to last lane entry” option.

\- It is not instant. It is an autopilot burn that consumes deterministic game-time.

\- It returns to a specific lane entry node, not “nearest safe place.”

\- It cannot be used during interdiction/combat.

\- It may be interrupted by deterministic encounters if exposure/conditions apply.



\### 7.3 Persistent value for off-lane discoveries

Off-lane exploration must never be “you found something, but it is meaningless.”



Rule:

\- Even without fracture/science capability, discoveries must yield persistent value via intel markers.



Implementation:

\- Discoveries generate a stable marker ID and can be bookmarked.

\- Without required tech, the player can scan enough to produce intel, but not exploit the anomaly.



---



\## 8. Procedural persistence law (bookmarking)

Procedural content must be reproducible and re-findable.



\- Discoveries are identified by stable IDs and seeds.

\- Map interactions target discovery IDs (and region radii), not raw floating point coordinates.

\- “Arrive at discovery” means entering a defined region, not hitting an exact point.



---



\## 9. Fuel policy

\- Avoid player fuel if it is annoying and adds chore gameplay.

\- If any resource-like constraint is later needed, it should be localized to fracture usage and kept non-annoying:

&nbsp; - no refuel busywork

&nbsp; - no accidental strand states

&nbsp; - constraints expressed as charges, cooldowns, maintenance cycles, or station service



---



\## 10. Fracture drive constraint model (mandatory, locked for implementation)

Goal: fracture expands the game without collapsing lane relevance.



The fracture model must be deterministic and legible:



\### 10.1 Range

\- Fracture hop has a fixed maximum range in map units.

\- A hop is a discrete action with a deterministic travel time (not continuous cruising).



\### 10.2 Maintenance and service

\- Fracture has a maintenance meter.

\- Each hop consumes a fixed maintenance amount.

\- Maintenance is reset only by a “service fracture” action at a science station module.



\### 10.3 Exposure (attention vector)

\- Each hop increments an exposure counter.

\- Exposure drives encounter eligibility and long-term consequences.



\### 10.4 Encounter schedule is deterministic

\- Encounters are not RNG rolls at runtime.

\- Encounter triggers are derived deterministically from: route\_id or region\_id + exposure tier + global tick index.

\- For Slice 1, use a simple threshold schedule:

&nbsp; - exposure tier changes at fixed hop counts

&nbsp; - interdiction encounter triggers on the first hop that crosses a tier boundary



\### 10.5 Lane relevance protection

\- Lanes remain the default logistics backbone.

\- Early game freighters remain lane-only.

\- Fracture-capable freighters are a late unlock, expensive, and increase exposure materially.



---



\## 11. Travel and bubble trigger rules (implementation law)

\### 11.1 Enter physics bubble when

\- Player arrives within station interaction radius

\- A mission-scripted encounter fires

\- Player initiates interaction at a discovery marker region

\- A deterministic interdiction fires (fracture exposure tier boundary or mission script)



\### 11.2 Never enter physics bubble for

\- Abstracted freighter movement

\- Background NPC logistics that are not interacting with the player



\### 11.3 Encounter determinism requirements

\- Encounter selection and parameters must be reproducible from stable IDs and tick indices.

\- Avoid floating point coordinate triggers. Use region IDs and radii.



\### 11.4 Performance guardrails

\- Bubble has a hard cap on active entities.

\- If the cap would be exceeded, defer non-critical spawns deterministically.



---



\## 12. Mission authoring and test contract (non-negotiable)

To keep missions deterministic, headless, and authorable, missions must be data-driven.



\### 12.1 Mission schema (minimum fields)

Each mission definition must include:



\- mission\_id (stable)

\- prerequisites (flags, inventory, location)

\- steps\[] where each step has:

&nbsp; - step\_id (stable)

&nbsp; - objective\_text\_id (UI key)

&nbsp; - allowed\_actions\[] (from a fixed command set)

&nbsp; - triggers (event predicates)

&nbsp; - completion\_conditions (state predicates)

&nbsp; - rewards (inventory, wallet, unlock flags)

&nbsp; - assertions (must be true at step completion)



\### 12.2 Allowed command set (minimum)

Headless runner must support these player-intent commands:



\- accept\_mission(mission\_id)

\- book\_lane(route\_id, sponsor\_coverage\_id?)

\- travel\_to(destination\_id)

\- dock(location\_id)

\- transfer\_cargo(item\_id, qty, target\_id)

\- buy(item\_id, qty)

\- sell(item\_id, qty)

\- hire\_freighter(contract\_params)



\### 12.3 Tutorial determinism clamp

During tutorial missions, mission-critical state must be frozen or reserved:



\- Tutorial commodity spread is fixed and reserved (bounded quantity) until mission completion.

\- Lane booking required for tutorial cannot fail due to capacity.

\- Background sim cannot mutate mission-critical station inventories/prices.

\- Mission-scripted encounters override ambient encounter generation.



\### 12.4 Assertions per mission (required)

Every mission must have an automated deterministic test scenario:

\- start state

\- action sequence (commands only)

\- expected end state:

&nbsp; - flags unlocked

&nbsp; - contracts created

&nbsp; - inventory and wallet deltas

&nbsp; - any spawned entities have stable IDs



Mission 1 must be runnable headlessly:

\- accept % book\_lane (sponsor pays) % travel % dock % deliver % buy % return % sell % unlock freighters



