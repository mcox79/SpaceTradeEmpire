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

    private static readonly bool DEBUG_UI = false;

    private Label _titleLabel;
    private Label _marketStatusLabel;
    private VBoxContainer _marketList;
    private VBoxContainer _trafficList;
    private VBoxContainer _sustainmentList;
    private Label _creditsLabel;


    private SimBridge _bridge;

    // What we get from the PlayerShip/shop toggle payload. Might be a node id or a market id depending on the caller.
    private string _currentMarketId = "";

    // Canonical market id used for market + intents.
    private string _resolvedMarketId = "";

    private ProgramsMenu _programsMenu;
    private FleetMenu _fleetMenu;

    private bool _playerSignalsConnected = false;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");

        // Ensure this Control receives _UnhandledInput even when not visible.
        SetProcessUnhandledInput(true);

        SetupUI();

        Visible = false;

        // Defer wiring until PlayerShip has joined group "Player" and has the signal.
        CallDeferred(nameof(ConnectPlayerSignals));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey k || !k.Pressed || k.Echo) return;

        // F9 is the deterministic fallback: open/close StationMenu using the player's current location,
        // even if the Player ship does not emit shop_toggled for non-station docking targets.
        // F1 is reserved for FleetMenu.
        if (k.Keycode == Key.F9)

        {
            if (Visible)
            {
                if (EnsureProgramsMenu()) _programsMenu.Close();
                if (EnsureFleetMenu()) _fleetMenu.Close();

                Visible = false;
                _currentMarketId = "";
                _resolvedMarketId = "";

                GetViewport().SetInputAsHandled();
                return;
            }

            var snap = _bridge?.GetPlayerSnapshot();
            var loc = (snap != null && snap.ContainsKey("location")) ? snap["location"].ToString() : "";

            Visible = true;
            _currentMarketId = loc ?? "";
            _resolvedMarketId = string.IsNullOrWhiteSpace(_currentMarketId) ? "" : ResolveCanonicalMarketId(_currentMarketId);

            // Defer UI build to avoid first-open hitch on the input frame.
            CallDeferred(nameof(Refresh));

            GetViewport().SetInputAsHandled();
            return;
        }

        if (!Visible) return;

        if (k.Keycode == Key.Escape)
        {
            // If ProgramsMenu is open, let it handle Escape (it closes itself).
            if (EnsureProgramsMenu() && _programsMenu.Visible)
            {
                return;
            }

            // If FleetMenu is open, let it handle Escape (it closes itself).
            if (EnsureFleetMenu() && _fleetMenu.Visible)
            {
                return;
            }

            // Otherwise Escape means "undock / close station menu"
            EmitSignal(SignalName.RequestUndock);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Dbg(string msg)
    {
        if (DEBUG_UI) GD.Print(msg);
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

        if (!player.HasSignal("shop_toggled"))
        {
            // Player exists but the signal is not ready yet.
            CallDeferred(nameof(ConnectPlayerSignals));
            return;
        }

        var callable = new Callable(this, nameof(OnShopToggled));

        // Guard against double-connect
        if (!player.IsConnected("shop_toggled", callable))
        {
            player.Connect("shop_toggled", callable);
        }

        _playerSignalsConnected = true;
    }

    private bool EnsureProgramsMenu()
    {
        if (_programsMenu != null && IsInstanceValid(_programsMenu)) return true;

        var pm = GetTree().Root.FindChild("ProgramsMenu", true, false);
        if (pm is ProgramsMenu found && IsInstanceValid(found))
        {
            _programsMenu = found;

            var closeCallable = new Callable(this, nameof(OnProgramsCloseRequested));
            if (!_programsMenu.IsConnected("RequestClose", closeCallable))
            {
                _programsMenu.Connect("RequestClose", closeCallable);
            }

            return true;
        }

        _programsMenu = null;
        return false;
    }

    private bool EnsureFleetMenu()
    {
        if (_fleetMenu != null && IsInstanceValid(_fleetMenu)) return true;

        var fm = GetTree().Root.FindChild("FleetMenu", true, false);
        if (fm is FleetMenu found && IsInstanceValid(found))
        {
            _fleetMenu = found;

            var closeCallable = new Callable(this, nameof(OnFleetCloseRequested));
            if (!_fleetMenu.IsConnected("RequestClose", closeCallable))
            {
                _fleetMenu.Connect("RequestClose", closeCallable);
            }

            return true;
        }

        _fleetMenu = null;
        return false;
    }

    private void SetupUI()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.Center);

        // Wider so Buy/Sell/AutoBuy + Start/Pause/Cancel all remain visible.
        panel.CustomMinimumSize = new Vector2(980, 600);

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
            if (!EnsureProgramsMenu())
            {
                GD.PrintErr("[StationMenu] Programs button pressed but ProgramsMenu not found.");
                return;
            }

            _programsMenu.Open();
        };
        marketHeader.AddChild(btnPrograms);

        var btnFleets = new Button { Text = "Fleets" };
        btnFleets.Pressed += () =>
        {
            if (!EnsureFleetMenu())
            {
                GD.PrintErr("[StationMenu] Fleets button pressed but FleetMenu not found.");
                return;
            }

            _fleetMenu.Open();
        };
        marketHeader.AddChild(btnFleets);

        var btnSave = new Button { Text = "Save" };
        btnSave.Pressed += () => { _bridge.RequestSave(); };
        marketHeader.AddChild(btnSave);

        var btnLoad = new Button { Text = "Load" };
        btnLoad.Pressed += () => { _bridge.RequestLoad(); };
        marketHeader.AddChild(btnLoad);

        var btnExplain = new Button { Text = "Explain Dump" };
        btnExplain.Pressed += () =>
        {
            var mid = _resolvedMarketId;
            if (string.IsNullOrWhiteSpace(mid)) mid = _currentMarketId;
            var t = _bridge.GetMarketExplainTranscript(mid);
            if (string.IsNullOrWhiteSpace(t))
            {
                GD.Print("[StationMenu] ExplainDump: (empty)");
                return;
            }

            foreach (var line in t.Split('\n'))
            {
                GD.Print(line);
            }
        };
        marketHeader.AddChild(btnExplain);

        _marketStatusLabel = new Label { Text = "", Visible = false };
        vbox.AddChild(_marketStatusLabel);

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
        closeBtn.Pressed += () =>
        {
            if (EnsureProgramsMenu()) _programsMenu.Close();
            if (EnsureFleetMenu()) _fleetMenu.Close();
            EmitSignal(SignalName.RequestUndock);
        };
        vbox.AddChild(closeBtn);
    }

    // Supports either existing callers (string marketId) or PlayerShip (station object ref).
    public void OnShopToggled(bool isOpen, Variant stationOrMarketId)
    {
        Visible = isOpen;

        if (!isOpen)
        {
            if (EnsureProgramsMenu()) _programsMenu.Close();
            _currentMarketId = "";
            _resolvedMarketId = "";
            return;
        }

        _currentMarketId = ResolveMarketId(stationOrMarketId);

        // Non-station docking is allowed. If we can't resolve any id at all, keep the menu open but disable market UI.
        if (string.IsNullOrWhiteSpace(_currentMarketId))
        {
            _currentMarketId = "";
            _resolvedMarketId = "";
            Refresh();
            return;
        }

        // Resolve canonical market id immediately so intents do not rely on Refresh() side effects.
        // If resolution fails (ex: star_0), keep the menu open with market UI disabled (no implicit defaults).
        _resolvedMarketId = ResolveCanonicalMarketId(_currentMarketId);

        Refresh();
    }

    private string ResolveCanonicalMarketId(string maybeNodeOrMarketId)
    {
        if (_bridge == null) return "";

        string result = maybeNodeOrMarketId;

        if (!_bridge.TryExecuteSafeRead(state =>
        {
            // If it is already a market key, keep it.
            if (state.Markets.ContainsKey(maybeNodeOrMarketId))
            {
                result = maybeNodeOrMarketId;
                return;
            }

            // If it is a node id, convert to node.MarketId.
            if (state.Nodes.TryGetValue(maybeNodeOrMarketId, out var node))
            {
                if (!string.IsNullOrWhiteSpace(node.MarketId) && state.Markets.ContainsKey(node.MarketId))
                {
                    result = node.MarketId;
                    return;
                }
            }

            result = "";
        }, timeoutMs: 0))
        {
            // Sim is stepping and holds the write lock. Never stall the UI thread.
            result = "";
        }

        Dbg($"[StationMenu] ResolveCanonicalMarketId '{maybeNodeOrMarketId}' => '{result}'");
        return result;
    }

    private static string ResolveMarketId(Variant stationOrMarketId)
    {
        // Quiet extractor only. Non-station docking is valid, so do not emit ERROR logs here.
        if (stationOrMarketId.VariantType == Variant.Type.String)
        {
            return stationOrMarketId.AsString();
        }

        var obj = stationOrMarketId.AsGodotObject();
        if (obj is null)
        {
            return "";
        }

        // Try GDScript export: @export var sim_market_id
        var simMarketIdVar = obj.Get("sim_market_id");
        var simMarketIdValue = simMarketIdVar.AsString();
        if (!string.IsNullOrWhiteSpace(simMarketIdValue))
        {
            return simMarketIdValue;
        }

        // Metadata fallback
        if (obj is Node metaNode && metaNode.HasMeta("sim_market_id"))
        {
            var meta = metaNode.GetMeta("sim_market_id").AsString();
            if (!string.IsNullOrWhiteSpace(meta))
            {
                return meta;
            }
        }

        // Method fallback
        if (obj.HasMethod("get_sim_market_id"))
        {
            var viaMethod = obj.Call("get_sim_market_id").AsString();
            if (!string.IsNullOrWhiteSpace(viaMethod))
            {
                return viaMethod;
            }
        }

        return "";
    }

    public void Refresh()
    {
        if (_bridge == null) return;
        if (string.IsNullOrWhiteSpace(_currentMarketId))
        {
            if (_marketStatusLabel != null) { _marketStatusLabel.Visible = true; _marketStatusLabel.Text = "NO DOCK TARGET (market disabled)"; }
            return;
        }

        var snapshot = _bridge.GetPlayerSnapshot();

        long playerCredits = 0;
        if (snapshot.ContainsKey("credits"))
            playerCredits = System.Convert.ToInt64(snapshot["credits"]);

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
        // Avoid heavy snapshot + quote fetch when market UI is disabled (ex: docked at star_16).
        var programsByMarketGood = new Dictionary<string, Godot.Collections.Dictionary>();
        var programQuotesById = new Dictionary<string, Godot.Collections.Dictionary>();
        if (!string.IsNullOrWhiteSpace(_resolvedMarketId))
        {
            var progArr = _bridge.GetProgramExplainSnapshot();
            foreach (var v in progArr)
            {
                if (v.Obj is not Godot.Collections.Dictionary d) continue;
                var id = d.ContainsKey("id") ? d["id"].ToString() : "";
                var m = d.ContainsKey("market_id") ? d["market_id"].ToString() : "";
                var g = d.ContainsKey("good_id") ? d["good_id"].ToString() : "";
                if (string.IsNullOrWhiteSpace(m) || string.IsNullOrWhiteSpace(g)) continue;

                programsByMarketGood[$"{m}::{g}"] = d;

                if (!string.IsNullOrWhiteSpace(id) && !programQuotesById.ContainsKey(id))
                {
                    // Safe: acquires its own read lock; this happens outside ExecuteSafeRead.
                    programQuotesById[id] = _bridge.GetProgramQuote(id);
                }
            }
        }

        var marketId = _resolvedMarketId;

        // If we couldn't resolve earlier due to lock contention, try again nonblocking.
        if (string.IsNullOrWhiteSpace(marketId) && !string.IsNullOrWhiteSpace(_currentMarketId))
        {
            marketId = ResolveCanonicalMarketId(_currentMarketId);
            _resolvedMarketId = marketId;
        }

        if (!_bridge.TryExecuteSafeRead(state =>
        {
            if (state.Nodes.ContainsKey(_currentMarketId))
                _titleLabel.Text = state.Nodes[_currentMarketId].Name.ToUpper();
            else
                _titleLabel.Text = _currentMarketId;

            bool marketEnabled = !string.IsNullOrWhiteSpace(marketId) && state.Markets.ContainsKey(marketId);

            if (_marketStatusLabel != null)
            {
                _marketStatusLabel.Visible = true;

                if (!marketEnabled)
                {
                    _marketStatusLabel.Text = $"NO MARKET AT THIS LOCATION (node_id='{_currentMarketId}')";
                }
                else if (_currentMarketId != marketId)
                {
                    _marketStatusLabel.Text = $"MARKET RESOLVED: node_id='{_currentMarketId}' % market_id='{marketId}'";
                }
                else
                {
                    _marketStatusLabel.Text = $"MARKET: '{marketId}'";
                }
            }

            foreach (var child in _marketList.GetChildren()) child.QueueFree();

            if (marketEnabled && state.Markets.TryGetValue(marketId, out var market))
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

                    var hbox = new HBoxContainer
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
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
                        string why = "";
                        if (!string.IsNullOrWhiteSpace(progId) && programQuotesById.TryGetValue(progId, out var q))
                        {
                            var fails = new System.Collections.Generic.List<string>(4);

                            bool getb(string k)
                            {
                                if (!q.ContainsKey(k)) return false;
                                return (bool)q[k];
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

                        // Make room for the extra program control buttons.
                        CustomMinimumSize = new Vector2(420, 0),
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                    };
                    hbox.AddChild(lbl);

                    var btnBuy = new Button { Text = "Buy 1", CustomMinimumSize = new Vector2(70, 0) };
                    btnBuy.Disabled = (marketQty <= 0 || playerCredits < price);
                    btnBuy.Pressed += () => SubmitTradeIntent(good, 1, true);
                    hbox.AddChild(btnBuy);

                    var btnSell = new Button { Text = "Sell 1", CustomMinimumSize = new Vector2(70, 0) };
                    btnSell.Disabled = (playerQty <= 0);
                    btnSell.Pressed += () => SubmitTradeIntent(good, 1, false);
                    hbox.AddChild(btnSell);

                    // Treat Cancelled as "no active program" for control purposes,
                    // so the player can create a new one.
                    var hasActiveProgram = !string.IsNullOrWhiteSpace(progId) && progStatus != "Cancelled";

                    if (!hasActiveProgram)
                    {
                        var btnCreate = new Button { Text = "AutoBuy", CustomMinimumSize = new Vector2(80, 0) };
                        btnCreate.Pressed += () =>
                                                {
                                                    _bridge.CreateAutoBuyProgram(marketId, good, quantity: 1, cadenceTicks: 10);

                                                    Refresh();

                                                    var timer = GetTree().CreateTimer(0.05);
                                                    timer.Timeout += () => Refresh();
                                                };
                        hbox.AddChild(btnCreate);
                    }
                    else
                    {
                        var btnStart = new Button { Text = "Start", CustomMinimumSize = new Vector2(70, 0) };
                        btnStart.Disabled = progStatus == "Running";
                        btnStart.Pressed += () => { _bridge.StartProgram(progId); Refresh(); };
                        hbox.AddChild(btnStart);

                        var btnPause = new Button { Text = "Pause", CustomMinimumSize = new Vector2(70, 0) };
                        btnPause.Disabled = progStatus == "Paused";
                        btnPause.Pressed += () => { _bridge.PauseProgram(progId); Refresh(); };
                        hbox.AddChild(btnPause);

                        var btnCancel = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(75, 0) };
                        btnCancel.Disabled = false;
                        btnCancel.Pressed += () => { _bridge.CancelProgram(progId); Refresh(); };
                        hbox.AddChild(btnCancel);
                    }
                }
            }
            else
            {
                _marketList.AddChild(new Label { Text = "(market disabled at this location)" });
            }

            foreach (var child in _trafficList.GetChildren()) child.QueueFree();
            var fleets = state.Fleets.Values.Where(f => f.CurrentNodeId == _currentMarketId).Take(5);
            foreach (var f in fleets)
            {
                _trafficList.AddChild(new Label { Text = $"> {f.Id}: {f.CurrentTask}" });
            }

            foreach (var child in _sustainmentList.GetChildren()) child.QueueFree();

            if (!marketEnabled)
            {
                _sustainmentList.AddChild(new Label { Text = "(sustainment unavailable: no market at this location)" });
                return;
            }

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
        }, timeoutMs: 0))
        {
            // Sim is stepping and holds the write lock.
            // Skip this refresh; next input/frame can try again.
            return;
        }
    }

    private void SubmitTradeIntent(string good, int qty, bool isBuy)
    {
        if (_bridge == null) return;
        if (string.IsNullOrWhiteSpace(good)) return;
        if (qty <= 0) return;

        if (string.IsNullOrWhiteSpace(_resolvedMarketId)) return;

        if (isBuy) _bridge.SubmitBuyIntent(_resolvedMarketId, good, qty);
        else _bridge.SubmitSellIntent(_resolvedMarketId, good, qty);

        Refresh();
    }

    private void OnProgramsCloseRequested()
    {
        if (!EnsureProgramsMenu()) return;
        _programsMenu.Close();
    }

    private void OnFleetCloseRequested()
    {
        if (!EnsureFleetMenu()) return;
        _fleetMenu.Close();
    }

    public void Close() => Visible = false;
}
