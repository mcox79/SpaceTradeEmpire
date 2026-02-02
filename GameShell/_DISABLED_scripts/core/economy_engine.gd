extends RefCounted
# class_name removed to prevent global namespace shadowing

const MIN_PRICE = 1

static func calculate_price(good, local_supply: int, base_demand: int) -> int:
	var base = 10
	if good and good.get("base_price"): base = good.base_price
	var scarcity = float(base_demand) / max(1.0, float(local_supply))
	return max(MIN_PRICE, int(base * scarcity))

static func can_afford(balance: int, price: int, qty: int) -> bool:
	return balance >= (price * qty)

static func has_cargo_space(cur_vol: float, max_vol: float, item_vol: float, qty: int) -> bool:
	return (cur_vol + (item_vol * qty)) <= max_vol