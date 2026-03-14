class_name EpilogueData
extends RefCounted

# Epilogue text data for victory and loss states
# Each path contains 5 narrative cards in sequence
# Loss frames are final end states with no progression

const REINFORCE_PATH = [
	{
		"title": "Your Choice",
		"body": "You chose to reinforce the containment threads — the proven infrastructure that holds the galaxy together.",
		"duration_secs": 6.0
	},
	{
		"title": "The Beneficiaries",
		"body": "The Concord and Weavers prospered. Their engineering expertise became the galaxy's most valued resource. Trade lanes stabilized. Tariffs normalized.",
		"duration_secs": 6.0
	},
	{
		"title": "The Cost",
		"body": "The fracture zones were sealed. Whatever lived in instability — whatever tried to communicate — was silenced. The Communion's warnings about accommodation went unheard. You chose safety over understanding.",
		"duration_secs": 6.0
	},
	{
		"title": "The Galaxy After",
		"body": "A galaxy of certain lanes and predictable commerce. The pentagon dependency remains, but stable. No faction can break free, but none will fall. The threads hold.",
		"duration_secs": 6.0
	},
	{
		"title": "Reflection",
		"body": "You built an empire on containment. The galaxy is safer. Whether it is better — whether the threads are walls or chains — that question will wait for someone braver or more foolish than you.",
		"duration_secs": 7.0
	}
]

const NATURALIZE_PATH = [
	{
		"title": "Your Choice",
		"body": "You chose to let the fracture grow — to accept instability not as damage but as the galaxy's natural state.",
		"duration_secs": 6.0
	},
	{
		"title": "The Beneficiaries",
		"body": "The Communion rejoiced. Their drifter philosophy proved prescient. Accommodation geometry spread from Haven to the wider galaxy. New stable zones emerged in places no thread had ever reached.",
		"duration_secs": 6.0
	},
	{
		"title": "The Cost",
		"body": "The old trade lanes withered. Stations dependent on containment infrastructure faced collapse. The Stationmaster's distress beacon — the one you heard — was one of thousands. Freedom has a body count.",
		"duration_secs": 6.0
	},
	{
		"title": "The Galaxy After",
		"body": "A galaxy reborn in chaos and possibility. Fracture space is no longer the edge — it is the center. New civilizations will grow in the shimmer. Old ones must adapt or perish.",
		"duration_secs": 6.0
	},
	{
		"title": "Reflection",
		"body": "You set the galaxy free. Whether freedom was the right gift for species that never asked for it — whether your conviction about accommodation was wisdom or projection — you will never know for certain.",
		"duration_secs": 7.0
	}
]

const RENEGOTIATE_PATH = [
	{
		"title": "Your Choice",
		"body": "You chose the impossible — to speak with the wound. To treat instability not as enemy or friend, but as interlocutor.",
		"duration_secs": 6.0
	},
	{
		"title": "The Beneficiaries",
		"body": "No faction fully supported you. The Communion came closest, but even they hesitated at the threshold. What you found in the deepest void was neither malice nor welcome — it was a pattern waiting to be read.",
		"duration_secs": 6.0
	},
	{
		"title": "The Cost",
		"body": "The cost was trust. Every faction questioned your loyalty. Every alliance frayed. You spent political capital that took hundreds of trade runs to build, and you spent it on a conversation no one believed was possible.",
		"duration_secs": 6.0
	},
	{
		"title": "The Galaxy After",
		"body": "The galaxy changed in ways no one predicted. The threads did not break. The fracture did not consume. Something in between emerged — a negotiated topology where containment and accommodation coexist. Imperfect. Unprecedented.",
		"duration_secs": 6.0
	},
	{
		"title": "Reflection",
		"body": "You spoke to the void and it answered. Whether what you heard was truth or echo — whether dialogue with the incomprehensible is communion or madness — the galaxy will debate for generations. You will not be there to settle it.",
		"duration_secs": 7.0
	}
]

const DEATH_FRAME = {
	"title": "Lost to the Void",
	"body": "Your ship's hull gave way — torn apart by forces you could not outrun. The galaxy does not mourn individuals. Trade lanes carry goods, not grief. Somewhere, a Stationmaster will note the loss of a regular customer. Somewhere else, an NPC fleet will fill the route you left empty. The threads hold. The fracture grows. The pentagon turns. And you are gone.",
	"duration_secs": 8.0
}

const BANKRUPTCY_FRAME = {
	"title": "Economic Collapse",
	"body": "Credits spent, cargo empty, debts unpayable. Your empire dissolved not in fire but in arithmetic. The galaxy's economy absorbed your failure without a ripple — your trade routes reassigned, your programs cancelled, your name removed from faction ledgers. You learned what every trader eventually learns: the galaxy does not owe you solvency. The threads hold. The fracture grows. And the market moves on.",
	"duration_secs": 8.0
}

# Helper to retrieve a path by name
static func get_path(path_name: String) -> Array:
	match path_name:
		"reinforce":
			return REINFORCE_PATH
		"naturalize":
			return NATURALIZE_PATH
		"renegotiate":
			return RENEGOTIATE_PATH
		_:
			return []

# Helper to retrieve a loss frame by name
static func get_loss_frame(frame_name: String) -> Dictionary:
	match frame_name:
		"death":
			return DEATH_FRAME
		"bankruptcy":
			return BANKRUPTCY_FRAME
		_:
			return {}
