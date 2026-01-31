extends RefCounted

# INDUSTRIAL LOGIC KERNEL
const RECIPES = {
	'mining': { 
		'tick_cost': 4, 
		'inputs': {}, 
		'outputs': {'ore_iron': 5, 'ore_gold': 1} 
	},
	'agri': { 
		'tick_cost': 10, 
		'inputs': {}, 
		'outputs': {'rations': 20} 
	},
	'refinery': { 
		'tick_cost': 2, 
		'inputs': {'ore_iron': 2}, 
		'outputs': {'fuel': 10} 
	}
}

static func process_production(market_state, tick: int) -> void:
	if market_state.industries.is_empty():
		return

	for ind_id in market_state.industries.keys():
		var recipe = RECIPES.get(ind_id)
		if not recipe: continue

		# Rate Limiting
		if tick % recipe.tick_cost != 0: continue

		# Input Check
		var can_produce = true
		for input_id in recipe.inputs.keys():
			var required = recipe.inputs[input_id]
			if market_state.inventory.get(input_id, 0) < required:
				can_produce = false
				break

		if not can_produce: continue

		# Consume Inputs
		for input_id in recipe.inputs.keys():
			market_state.inventory[input_id] -= recipe.inputs[input_id]

		# Generate Outputs
		for output_id in recipe.outputs.keys():
			var current = market_state.inventory.get(output_id, 0)
			market_state.inventory[output_id] = current + recipe.outputs[output_id]