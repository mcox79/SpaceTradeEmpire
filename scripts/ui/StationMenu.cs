using Godot;
using System.Linq;
using System.Collections.Generic;
using SimCore;
using SimCore.Systems;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
		[Signal] public delegate void RequestUndockEventHandler();

		private Label _titleLabel;
		private VBoxContainer _marketList;
		private VBoxContainer _trafficList;
		private VBoxContainer _sustainmentList;
		private Label _creditsLabel;

		private SimBridge _bridge;
				private string _currentMarketId = "";
				private string _resolvedMarketId = "";
				private ProgramsMenu _programsMenu;

				private bool _playerSignalsConnected = false;


				public override void _Ready()
						{
								_bridge = GetNode<SimBridge>("/root/SimBridge");

								// Find ProgramsMenu anywhere in the scene tree.
								var pm = GetTree().Root.FindChild("ProgramsMenu", true, false);
								if (pm is ProgramsMenu found)
								{
																_programsMenu = found;
																_programsMenu.Connect("RequestClose", new Callable(this, nameof(OnProgramsCloseRequested)));
								}
								else
								{
																GD.PrintErr("[StationMenu] ProgramsMenu not found in scene tree.");
								}

								SetupUI();
								Visible = false;

								// Defer wiring until PlayerShip has joined group "Player".
								CallDeferred(nameof(ConnectPlayerSignals));
						}

private void ConnectPlayerSignals()
{
		if (_playerSignalsConnected) return;

		var player = GetTree().GetFirstNodeInGroup("Player");
		if (player == null)
		{
				CallDeferred(nameof(ConnectPlayerSignals));
				return;
		}

		var callable = new Callable(this, nameof(OnShopToggled));

		// Guard against double-connect (this is the error you are seeing)
		if (!player.IsConnected("shop_toggled", callable))
		{
				player.Connect("shop_toggled", callable);
		}

		_playerSignalsConnected = true;
}


		private void SetupUI()
		{
				var panel = new PanelContainer();
				panel.SetAnchorsPreset(LayoutPreset.Center);
				panel.CustomMinimumSize = new Vector2(800, 600);
				AddChild(panel);

				var vbox = new VBoxContainer();
				panel.AddChild(vbox);

				_titleLabel = new Label { Text = "STATION MENU", HorizontalAlignment = HorizontalAlignment.Center };
				vbox.AddChild(_titleLabel);

				_creditsLabel = new Label { Text = "CREDITS: 0", HorizontalAlignment = HorizontalAlignment.Right, Modulate = Colors.Gold };
				vbox.AddChild(_creditsLabel);

				vbox.AddChild(new HSeparator());

				var marketHeader = new HBoxContainer();
				vbox.AddChild(marketHeader);

				marketHeader.AddChild(new Label { Text = "MARKET (Buy/Sell + Programs)", Modulate = new Color(0.7f, 0.7f, 1f) });

				var btnPrograms = new Button { Text = "Programs" };
								btnPrograms.Pressed += () =>
								{
										// Resolve lazily in case _Ready ran before ProgramsMenu was present/ready.
										if (_programsMenu == null)
										{
												var pm = GetTree().Root.FindChild("ProgramsMenu", true, false);
												if (pm is ProgramsMenu found)
												{
														_programsMenu = found;
														if (!_programsMenu.IsConnected("RequestClose", new Callable(this, nameof(OnProgramsCloseRequested))))
																_programsMenu.Connect("RequestClose", new Callable(this, nameof(OnProgramsCloseRequested)));
												}
										}

										if (_programsMenu == null)
										{
												GD.PrintErr("[StationMenu] Programs button pressed but ProgramsMenu not found.");
												return;
										}

										_programsMenu.Open();
								};
								marketHeader.AddChild(btnPrograms);



				var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 260) };
				vbox.AddChild(scroll);

				_marketList = new VBoxContainer();
				scroll.AddChild(_marketList);

				vbox.AddChild(new HSeparator());
				vbox.AddChild(new Label { Text = "TRAFFIC MONITOR", Modulate = new Color(1f, 0.7f, 0.7f) });

				_trafficList = new VBoxContainer();
				vbox.AddChild(_trafficList);

				vbox.AddChild(new HSeparator());
				vbox.AddChild(new Label { Text = "SUSTAINMENT (banded, no exact countdowns)", Modulate = new Color(0.8f, 1f, 0.8f) });

				var susScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 120) };
				vbox.AddChild(susScroll);

				_sustainmentList = new VBoxContainer();
				susScroll.AddChild(_sustainmentList);

				vbox.AddChild(new HSeparator());
				var closeBtn = new Button { Text = "Undock" };
				closeBtn.Pressed += () => EmitSignal(SignalName.RequestUndock);
				vbox.AddChild(closeBtn);
		}

		// Supports either existing callers (string marketId) or PlayerShip (station object ref).
		public void OnShopToggled(bool isOpen, Variant stationOrMarketId)
		{
				Visible = isOpen;

				if (!isOpen)
				{
						_currentMarketId = "";
						return;
				}

				_currentMarketId = ResolveMarketId(stationOrMarketId);

								// If we cannot resolve a market id, do not show an empty menu.
								if (string.IsNullOrWhiteSpace(_currentMarketId))
								{
										GD.PrintErr("[StationMenu] OnShopToggled opened, but ResolveMarketId returned empty. Closing menu.");
										Visible = false;
										return;
								}

								Refresh();

		}

		private static string ResolveMarketId(Variant stationOrMarketId)
		{
				if (stationOrMarketId.VariantType == Variant.Type.String)
				{
						return stationOrMarketId.AsString();
				}

				var obj = stationOrMarketId.AsGodotObject();
				if (obj is null) return "";

				// ActiveStation exports: sim_market_id
				if (obj.HasMeta("sim_market_id"))
				{
						var v = obj.GetMeta("sim_market_id");
						if (v.VariantType == Variant.Type.String) return v.AsString();
				}

				// Prefer property lookup (works for GDScript exports)
				try
				{
						var p = obj.Get("sim_market_id");
						if (p.VariantType == Variant.Type.String) return p.AsString();
				}
				catch
				{
						// Ignore
				}

				return "";
		}

		public void Refresh()
		{
				if (_bridge == null || string.IsNullOrEmpty(_currentMarketId)) return;

				var snapshot = _bridge.GetPlayerSnapshot();

				int playerCredits = 0;
				if (snapshot.ContainsKey("credits"))
						playerCredits = (int)snapshot["credits"];

				var playerCargo = new Godot.Collections.Dictionary();
				if (snapshot.ContainsKey("cargo"))
				{
						var variant = snapshot["cargo"];
						if (variant.Obj is Godot.Collections.Dictionary nested)
						{
								playerCargo = nested;
						}
				}

				_creditsLabel.Text = $"CREDITS: {playerCredits:N0}";

				// Programs snapshot (schema-bound via ProgramExplain -> JSON -> dicts)
				var progArr = _bridge.GetProgramExplainSnapshot();
				var programsByMarketGood = new Dictionary<string, Godot.Collections.Dictionary>();
				foreach (var v in progArr)
				{
						if (v.Obj is not Godot.Collections.Dictionary d) continue;
						var m = d.ContainsKey("market_id") ? d["market_id"].ToString() : "";
						var g = d.ContainsKey("good_id") ? d["good_id"].ToString() : "";
						if (string.IsNullOrWhiteSpace(m) || string.IsNullOrWhiteSpace(g)) continue;

						// One program per (market, good) for now.
						programsByMarketGood[$"{m}::{g}"] = d;
				}

					_bridge.ExecuteSafeRead(state =>
								{
										// Resolve market id robustly:
										// - _currentMarketId is often a node id
										// - markets are keyed by node.MarketId
																				var marketId = _currentMarketId;
										if (!state.Markets.ContainsKey(marketId) && state.Nodes.TryGetValue(_currentMarketId, out var node))
										{
												if (!string.IsNullOrWhiteSpace(node.MarketId))
														marketId = node.MarketId;
										}

										_resolvedMarketId = marketId;

																				GD.Print($"[StationMenu] _currentMarketId='{_currentMarketId}' resolved marketId='{marketId}' markets={state.Markets.Count} nodes={state.Nodes.Count}");
																				if (state.Markets.Count > 0)
																				{
																						var sample = string.Join(",", state.Markets.Keys.Take(5));
																						GD.Print($"[StationMenu] market keys sample: {sample}");
																				}



										if (state.Nodes.ContainsKey(_currentMarketId))
												_titleLabel.Text = state.Nodes[_currentMarketId].Name.ToUpper();
										else
												_titleLabel.Text = _currentMarketId;

										foreach (var child in _marketList.GetChildren()) child.QueueFree();

										if (state.Markets.TryGetValue(marketId, out var market))
										{

								var allGoods = new HashSet<string>();
								foreach (var k in market.Inventory.Keys) allGoods.Add(k);
								foreach (var k in playerCargo.Keys) allGoods.Add(k.ToString());

								var sortedGoods = allGoods.OrderBy(k => k);

								foreach (var good in sortedGoods)
								{
										int marketQty = market.Inventory.GetValueOrDefault(good, 0);

										int playerQty = 0;
										if (playerCargo.ContainsKey(good)) playerQty = (int)playerCargo[good];

										int price = market.GetPrice(good);

										// Slice 1 UI requirement: show intel age
																			   int ageTicks = IntelSystem.GetMarketGoodView(state, marketId, good).AgeTicks;
																			   string ageText = (ageTicks < 0) ? "?" : ageTicks.ToString();


										var hbox = new HBoxContainer();
										_marketList.AddChild(hbox);

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
												progSuffix = $" | AutoBuy: {progStatus} (q={qty}, cad={cadence}t)";
										}


										var lbl = new Label
										{
												Text = $"{good.PadRight(10)} | Stock: {marketQty} | Price: ${price} | You: {playerQty} | IntelAge(t): {ageText}{progSuffix}",
												Modulate = infoColor,
												CustomMinimumSize = new Vector2(520, 0)
										};
										hbox.AddChild(lbl);

										var btnBuy = new Button { Text = "Buy 1" };
										btnBuy.Disabled = (marketQty <= 0 || playerCredits < price);
										btnBuy.Pressed += () => SubmitTradeIntent(good, 1, true);
										hbox.AddChild(btnBuy);

										var btnSell = new Button { Text = "Sell 1" };
										btnSell.Disabled = (playerQty <= 0);
										btnSell.Pressed += () => SubmitTradeIntent(good, 1, false);
										hbox.AddChild(btnSell);

																				// Program controls
										if (string.IsNullOrWhiteSpace(progId))
										{
												var btnCreate = new Button { Text = "AutoBuy" };
												btnCreate.Pressed += () =>
												{
														// Default: buy 1 every 10 ticks, starts paused (explicit Start button).
														_bridge.CreateAutoBuyProgram(marketId, good, quantity: 1, cadenceTicks: 10);

														// Program creation is on sim thread; explain snapshot can lag.
														// Refresh now, then again shortly after.
														Refresh();

														var timer = GetTree().CreateTimer(0.05);
														timer.Timeout += () => Refresh();
												};
												hbox.AddChild(btnCreate);
										}
										else
										{
												var btnStart = new Button { Text = "Start" };
												btnStart.Disabled = progStatus == "Running" || progStatus == "Cancelled";
												btnStart.Pressed += () => { _bridge.StartProgram(progId); Refresh(); };
												hbox.AddChild(btnStart);

												var btnPause = new Button { Text = "Pause" };
												btnPause.Disabled = progStatus == "Paused" || progStatus == "Cancelled";
												btnPause.Pressed += () => { _bridge.PauseProgram(progId); Refresh(); };
												hbox.AddChild(btnPause);

												var btnCancel = new Button { Text = "Cancel" };
												btnCancel.Disabled = progStatus == "Cancelled";
												btnCancel.Pressed += () => { _bridge.CancelProgram(progId); Refresh(); };
												hbox.AddChild(btnCancel);
										}

								}
						}

						foreach (var child in _trafficList.GetChildren()) child.QueueFree();
						var fleets = state.Fleets.Values.Where(f => f.CurrentNodeId == _currentMarketId).Take(5);
						foreach (var f in fleets)
						{
								_trafficList.AddChild(new Label { Text = $"> {f.Id}: {f.CurrentTask}" });
						}

												foreach (var child in _sustainmentList.GetChildren()) child.QueueFree();

												var sites = SustainmentSnapshot.BuildForNode(state, marketId);

												if (sites.Count == 0)
												{
														_sustainmentList.AddChild(new Label { Text = "(no active industry sites)" });
												}
												else
												{
														foreach (var s in sites)
														{
																var header = new Label
																{
																		Text = $"{s.SiteId} | Health(bps): {s.HealthBps} | Eff(bps): {s.EffBpsNow} | Margin: {s.WorstBufferMargin:0.00} | Starve: {s.StarveBand} | Fail: {s.FailBand}"
																};
																_sustainmentList.AddChild(header);

																foreach (var inp in s.Inputs)
																{
																		_sustainmentList.AddChild(new Label
																		{
																				Text = $"  - {inp.GoodId}: have {inp.HaveUnits}, req/t {inp.PerTickRequired}, target {inp.BufferTargetUnits}, cover {inp.CoverageBand}, margin {inp.BufferMargin:0.00}"
																		});
																}
														}
												}

				});
		}

		private void SubmitTradeIntent(string good, int qty, bool isBuy)
		{
				if (_bridge == null) return;
				if (string.IsNullOrWhiteSpace(good)) return;
				if (qty <= 0) return;

				// Use the resolved market id (set during Refresh via ExecuteSafeRead)
				if (string.IsNullOrWhiteSpace(_resolvedMarketId)) return;

				if (isBuy) _bridge.SubmitBuyIntent(_resolvedMarketId, good, qty);
				else _bridge.SubmitSellIntent(_resolvedMarketId, good, qty);

				Refresh();
		}


				private void OnProgramsCloseRequested()
				{
						if (_programsMenu != null) _programsMenu.Visible = false;
				}

		public void Close() => Visible = false;
}
