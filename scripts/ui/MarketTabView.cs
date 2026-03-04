using Godot;
using System;
using System.Collections.Generic;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

// GATE.X.HYGIENE.STATION_MENU_SPLIT.001: Market tab logic extracted from StationMenu.
public class MarketTabView
{
	private readonly SimBridge _bridge;
	private readonly VBoxContainer _marketList;
	private readonly Action _refreshCallback;

	public MarketTabView(SimBridge bridge, VBoxContainer marketList, Action refreshCallback)
	{
		_bridge = bridge;
		_marketList = marketList;
		_refreshCallback = refreshCallback;
	}

	public void RenderMarketRows(
		SimCore.SimState state,
		string marketId,
		long playerCredits,
		Godot.Collections.Dictionary playerCargo,
		Dictionary<string, Godot.Collections.Dictionary> programsByMarketGood,
		Dictionary<string, Godot.Collections.Dictionary> programQuotesById,
		SceneTree tree)
	{
		foreach (var child in _marketList.GetChildren()) child.QueueFree();

		bool marketEnabled = !string.IsNullOrWhiteSpace(marketId) && state.Markets.ContainsKey(marketId);

		if (marketEnabled && state.Markets.TryGetValue(marketId, out var market))
		{
			var allGoods = new HashSet<string>();
			foreach (var k in market.Inventory.Keys) allGoods.Add(k);
			foreach (var k in playerCargo.Keys) allGoods.Add(k.ToString());

			var sortedGoods = System.Linq.Enumerable.OrderBy(allGoods, k => k);

			foreach (var good in sortedGoods)
			{
				int marketQty = market.Inventory.GetValueOrDefault(good, 0);

				int playerQty = 0;
				if (playerCargo.ContainsKey(good)) playerQty = (int)playerCargo[good];

				int price = market.GetPrice(good);

				// Slice 1 UI requirement: show intel age (must go through bridge, not SimCore.Systems)
				int ageTicks = _bridge != null ? _bridge.GetIntelAgeTicks_NoLock(state, marketId, good) : -1;
				string ageText = (ageTicks < 0) ? "?" : ageTicks.ToString();

				// Two-line row to prevent label/button overlap:
				// line 1: label (wraps)
				// line 2: buttons
				var row = new VBoxContainer
				{
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
				};
				_marketList.AddChild(row);

				var buttons = new HBoxContainer
				{
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
				};

				var infoColor = (price > 110) ? Colors.Salmon : (price < 90 ? Colors.LightGreen : Colors.White);

				// Program info (one per market+good for now)
				var key = $"{marketId}::{good}";
				string progSuffix = "";
				string progId = "";
				string progStatus = "";
				if (programsByMarketGood.TryGetValue(key, out var pd))
				{
					progId = pd.ContainsKey("id") ? pd["id"].ToString() : "";
					progStatus = pd.ContainsKey("status") ? pd["status"].ToString() : "";
					var cadence = pd.ContainsKey("cadence_ticks") ? (int)pd["cadence_ticks"] : 0;
					var qty = pd.ContainsKey("quantity") ? (int)pd["quantity"] : 0;
					string why = "";
					if (!string.IsNullOrWhiteSpace(progId) && programQuotesById.TryGetValue(progId, out var q))
					{
						var fails = new System.Collections.Generic.List<string>(4);

						bool getb(string k2)
						{
							if (!q.ContainsKey(k2)) return false;
							return (bool)q[k2];
						}

						if (!getb("market_exists")) fails.Add("market_missing");
						if (!getb("has_enough_credits_now")) fails.Add("no_credits");
						if (!getb("has_enough_supply_now")) fails.Add("no_supply");
						if (!getb("has_enough_cargo_now")) fails.Add("no_cargo");

						if (fails.Count == 0) why = "OK";
						else why = "BLOCKED:" + string.Join(",", fails);
					}

					if (string.IsNullOrWhiteSpace(why))
						progSuffix = $" | AutoBuy: {progStatus} (q={qty}, cad={cadence}t)";
					else
						progSuffix = $" | AutoBuy: {progStatus} (q={qty}, cad={cadence}t) | {why}";
				}


				var lbl = new Label
				{
					Text = $"{good.PadRight(10)} | Stock: {marketQty} | Price: ${price} | You: {playerQty} | IntelAge(t): {ageText}{progSuffix}",
					Modulate = infoColor,

					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
					AutowrapMode = TextServer.AutowrapMode.WordSmart,

					// Force no ellipsis even if the theme sets it globally.
					ClipText = false,
					TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
				};
				row.AddChild(lbl);
				row.AddChild(buttons);


				var btnBuy = new Button { Text = "Buy 1", CustomMinimumSize = new Vector2(70, 0) };
				btnBuy.Disabled = (marketQty <= 0 || playerCredits < price);
				btnBuy.Pressed += () => SubmitTradeIntent(marketId, good, 1, true);
				buttons.AddChild(btnBuy);

				var btnSell = new Button { Text = "Sell 1", CustomMinimumSize = new Vector2(70, 0) };
				btnSell.Disabled = (playerQty <= 0);
				btnSell.Pressed += () => SubmitTradeIntent(marketId, good, 1, false);
				buttons.AddChild(btnSell);

				// Treat Cancelled as "no active program" for control purposes,
				// so the player can create a new one.
				var hasActiveProgram = !string.IsNullOrWhiteSpace(progId) && progStatus != "Cancelled";

				if (!hasActiveProgram)
				{
					var btnCreate = new Button { Text = "AutoBuy", CustomMinimumSize = new Vector2(80, 0) };
					btnCreate.Pressed += () =>
											{
												_bridge.CreateAutoBuyProgram(marketId, good, quantity: 1, cadenceTicks: 10);

												_refreshCallback?.Invoke();

												var timer = tree.CreateTimer(0.05);
												timer.Timeout += () => _refreshCallback?.Invoke();
											};
					buttons.AddChild(btnCreate);
				}
				else
				{
					var btnStart = new Button { Text = "Start", CustomMinimumSize = new Vector2(70, 0) };
					btnStart.Disabled = progStatus == "Running";
					btnStart.Pressed += () => { _bridge.StartProgram(progId); _refreshCallback?.Invoke(); };
					buttons.AddChild(btnStart);

					var btnPause = new Button { Text = "Pause", CustomMinimumSize = new Vector2(70, 0) };
					btnPause.Disabled = progStatus == "Paused";
					btnPause.Pressed += () => { _bridge.PauseProgram(progId); _refreshCallback?.Invoke(); };
					buttons.AddChild(btnPause);

					var btnCancel = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(75, 0) };
					btnCancel.Disabled = false;
					btnCancel.Pressed += () => { _bridge.CancelProgram(progId); _refreshCallback?.Invoke(); };
					buttons.AddChild(btnCancel);
				}
			}
		}
		else
		{
			_marketList.AddChild(new Label { Text = "(market disabled at this location)" });
		}
	}

	public void SubmitTradeIntent(string marketId, string good, int qty, bool isBuy)
	{
		if (_bridge == null) return;
		if (string.IsNullOrWhiteSpace(good)) return;
		if (qty <= 0) return;
		if (string.IsNullOrWhiteSpace(marketId)) return;

		if (isBuy) _bridge.SubmitBuyIntent(marketId, good, qty);
		else _bridge.SubmitSellIntent(marketId, good, qty);

		_refreshCallback?.Invoke();
	}
}
