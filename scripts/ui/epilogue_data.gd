class_name EpilogueData
extends RefCounted

# Epilogue text data for victory and loss states.
# Each victory path contains 5 narrative cards displayed in sequence.
# Loss frames are single-card end states.
#
# Writing conventions:
#   - Epilogue paragraphs: second person ("You have...")
#   - FO farewell (final card): first person ("It was an honor, Captain.")
#   - Paragraph length: 3-5 sentences per card body
#   - Tone: atmospheric, emotionally resonant, AAA quality

# ---------------------------------------------------------------------------
# VICTORY PATH: REINFORCE
# Player chose to strengthen the containment threads alongside the Concord
# and the Weavers. Safety over understanding.
# ---------------------------------------------------------------------------
const REINFORCE_PATH = [
	{
		"title": "The Choice That Held",
		"body": "You stood at the edge of the void and chose to pull back. Where others saw the fracture as invitation, you saw liability — a wound left open long enough becomes a door, and not everything that knocks deserves to enter. You poured credits, alliances, and years of hard-won reputation into a single wager: that the galaxy is safer stitched closed than left to breathe.",
		"duration_secs": 7.0
	},
	{
		"title": "A Galaxy Held Together",
		"body": "The Concord's containment engineers celebrated in pressurized corridors across a dozen systems. The Weavers completed thread-hardening projects they had shelved for decades, finally flush with the resources and political cover your reputation provided. Trade lanes that had flickered for a generation burned steady. Tariffs normalized. The fracture zones grew quiet — not healed, but leashed.",
		"duration_secs": 7.0
	},
	{
		"title": "What Was Left Behind",
		"body": "You sealed the wound, and something was silenced inside it. The Communion's warnings about accommodation were filed away as fringe theology. The signals that had bled through the instability zones — patterns that no one had fully decoded — stopped. Whether they were noise or conversation, no one will know now. You chose safety. The price of safety is everything that was not safe to ask.",
		"duration_secs": 7.0
	},
	{
		"title": "The Galaxy After",
		"body": "You leave behind a galaxy of certain lanes and predictable commerce. The pentagon dependency remains — no faction escaped it — but none will fall beneath it either. The threads hold. The stars are where they were. The market moves goods the same direction it always has, only now with less friction and more stability than anyone dared expect a decade ago. Whether that is freedom or its polished imitation is a question your children's children may still be arguing.",
		"duration_secs": 7.0
	},
	{
		"title": "Farewell, Captain",
		"body": "[wave][color=aaccee]It was an honor, Captain. Every route we charted, every margin we squeezed, every thread we reinforced — I watched you calculate the odds and choose the answer that let others sleep safely. I won't pretend I never disagreed. But I will say this: the galaxy is quieter tonight than it was the day we met, and that is entirely your doing. Fly well, wherever the lanes take you next.[/color][/wave]",
		"duration_secs": 9.0
	}
]

# ---------------------------------------------------------------------------
# VICTORY PATH: NATURALIZE
# Player chose to let the fracture grow — accepting instability as the galaxy's
# natural state. Freedom over safety.
# ---------------------------------------------------------------------------
const NATURALIZE_PATH = [
	{
		"title": "The Door You Left Open",
		"body": "You looked into the fracture and chose not to flinch. Where others scrambled to reinforce the walls, you questioned whether the walls deserved to stand. The containment threads were built by factions that benefit from containment — you saw that, and you acted on it, and the galaxy will never entirely forgive you for being right.",
		"duration_secs": 7.0
	},
	{
		"title": "The Communion's Vindication",
		"body": "The Communion spread the news across their drift-routes before the last containment station had finished failing: the galaxy had changed sides. Their philosophy of accommodation — laughed at in Concord briefing rooms, dismissed as mysticism by the Weavers — proved prescient. Accommodation geometry propagated outward from Haven. New stable zones crystallized in the shimmer, in places no thread had ever reached, in shapes no faction had predicted.",
		"duration_secs": 7.0
	},
	{
		"title": "The Cost of Freedom",
		"body": "Freedom, you learned, has a body count. Stations that had anchored themselves to containment infrastructure went dark one by one — their trade routes evaporated, their populations displaced. The Stationmaster's distress beacon that first crossed your comm channel was one of thousands. You knew this was coming. You chose naturalization anyway. History will have to decide whether that makes you a liberator or something harder to name.",
		"duration_secs": 7.0
	},
	{
		"title": "The Galaxy After",
		"body": "You leave behind a galaxy reborn in chaos and possibility. The fracture is no longer the edge of things — it is the center. New civilizations will form in the shimmer where old ones refused to look. The factions that survived are leaner, stranger, and more honest about what they are. The ones that did not survive knew the risk and built their houses on ground that could not hold. That was not your fault. That was physics.",
		"duration_secs": 7.0
	},
	{
		"title": "Farewell, Captain",
		"body": "[wave][color=aaffcc]It was an honor, Captain — every uncertain jump, every lane that hadn't been mapped yet, every morning we weren't sure the hull would hold. You taught me that the galaxy doesn't owe us predictability. I think I believed in the old order longer than I should have. You were right before I was ready to say so. That matters. Travel well — the shimmer is beautiful now, and some of it is because of you.[/color][/wave]",
		"duration_secs": 9.0
	}
]

# ---------------------------------------------------------------------------
# VICTORY PATH: RENEGOTIATE
# Player chose to open dialogue with the fracture itself — treating instability
# as interlocutor rather than enemy or resource. The impossible path.
# ---------------------------------------------------------------------------
const RENEGOTIATE_PATH = [
	{
		"title": "The Conversation No One Believed In",
		"body": "You spent political capital that took years of hard trade to build, and you spent it on a conversation that no one thought was possible. Every faction questioned your loyalty. Every alliance frayed at least once. The Concord called it reckless. The Communion called it presumptuous. The Weavers filed three formal complaints. You kept going anyway, because you had read the signals closely enough to believe something was listening.",
		"duration_secs": 7.0
	},
	{
		"title": "What You Found in the Deep",
		"body": "What you found in the deepest void was neither malice nor welcome. It was a pattern — vast, patient, and old in a way that made the galaxy's oldest civilizations feel recent. It did not want the threads removed. It did not want to consume. It wanted, as best you could translate, for the conversation to continue. You were the first being in the galaxy to answer that request in a language it recognized.",
		"duration_secs": 7.0
	},
	{
		"title": "The Price of Understanding",
		"body": "You paid for this in isolation. There were months when no faction would dock you, no station would extend credit. You survived on routes that no one else ran, on margins no one else would touch. The Haven became a refuge not because it was safe but because it was yours — the one place in the galaxy where your choices were treated as legitimate rather than eccentric. That solitude shaped you. It may have been necessary.",
		"duration_secs": 7.0
	},
	{
		"title": "The Galaxy After",
		"body": "You leave behind a galaxy that surprised everyone, including you. The threads did not break. The fracture did not consume. Something in between emerged — a negotiated topology, imperfect and unprecedented, where containment and accommodation coexist in unstable equilibrium. Scholars will spend careers arguing about what it means. Factions will spend decades adapting to it. Whether it holds is a question for the future. You started the conversation. Someone else will have to finish it.",
		"duration_secs": 7.0
	},
	{
		"title": "Farewell, Captain",
		"body": "[wave][color=ddccff]It was an honor, Captain — and I mean that with more precision than the phrase usually carries. I have served captains who were brave, captains who were clever, and captains who were right. You were the only one I served who was all three, and wrong about almost nothing that mattered. What you did in the deep will be debated long after both of us are gone. I hope the answer they settle on is the generous one. You earned it.[/color][/wave]",
		"duration_secs": 9.0
	}
]

# ---------------------------------------------------------------------------
# LOSS FRAMES
# Single-card end states displayed on the loss screen.
# ---------------------------------------------------------------------------

const DEATH_FRAME = {
	"title": "Lost to the Void",
	"body": "Your ship's hull gave way beneath forces you could not outrun. There was no dramatic last stand — just the arithmetic of damage exceeding tolerance, and then the quiet that follows arithmetic. The galaxy does not mourn individuals. Trade lanes carry goods, not grief. Somewhere, a Stationmaster will note the absence of a regular customer. Somewhere else, a route you once ran will be picked up by a crew who never knew your name. The threads hold. The fracture grows. The pentagon turns. And the stars are exactly where they were.",
	"duration_secs": 9.0
}

const BANKRUPTCY_FRAME = {
	"title": "The Ledger Closes",
	"body": "Credits exhausted. Cargo empty. Debts compounded past the point of recovery. Your empire dissolved not in fire but in arithmetic — the quiet, merciless kind that does not care about intent or effort or how close you came. The galaxy absorbed your failure without a ripple. Your trade routes were redistributed within hours. Your programs were cancelled. Your name was removed from faction ledgers as a routine administrative task. The market, as it always does, moved on. It did not wish you ill. It simply did not notice.",
	"duration_secs": 9.0
}

# ---------------------------------------------------------------------------
# Helper accessors
# ---------------------------------------------------------------------------

static func get_path(path_name: String) -> Array:
	match path_name.to_lower():
		"reinforce":
			return REINFORCE_PATH
		"naturalize":
			return NATURALIZE_PATH
		"renegotiate":
			return RENEGOTIATE_PATH
		_:
			return []

static func get_loss_frame(frame_name: String) -> Dictionary:
	match frame_name.to_lower():
		"death":
			return DEATH_FRAME
		"bankruptcy":
			return BANKRUPTCY_FRAME
		_:
			return {}
