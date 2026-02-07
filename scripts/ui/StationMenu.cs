using Godot;
using System.Linq;
using System.Collections.Generic;
using SimCore;
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

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        SetupUI();
        Visible = false;
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
        vbox.AddChild(new Label { Text = "MARKET (Buy/Sell)", Modulate = new Color(0.7f, 0.7f, 1f) });

        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 220) };
        vbox.AddChild(scroll);

        _marketList = new VBoxContainer();
        scroll.AddChild(_marketList);

        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "TRAFFIC MONITOR", Modulate = new Color(1f, 0.7f, 0.7f) });

        _trafficList = new VBoxContainer();
        vbox.AddChild(_trafficList);

        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "SUSTAINMENT", Modulate = new Color(0.8f, 1f, 0.8f) });

        var susScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 140) };
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

        _bridge.ExecuteSafeRead(state =>
        {
            if (state.Nodes.ContainsKey(_currentMarketId))
                _titleLabel.Text = state.Nodes[_currentMarketId].Name.ToUpper();
            else
                _titleLabel.Text = _currentMarketId;

            foreach (var child in _marketList.GetChildren()) child.QueueFree();

            if (state.Markets.TryGetValue(_currentMarketId, out var market))
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
                    int ageTicks = _bridge.GetIntelAgeTicks(_currentMarketId, good);
                    string ageText = (ageTicks < 0) ? "?" : ageTicks.ToString();

                    var hbox = new HBoxContainer();
                    _marketList.AddChild(hbox);

                    var infoColor = (price > 110) ? Colors.Salmon : (price < 90 ? Colors.LightGreen : Colors.White);
                    var lbl = new Label
                    {
                        Text = $"{good.PadRight(10)} | Stock: {marketQty} | Price: ${price} | You: {playerQty} | IntelAge(t): {ageText}",
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
                }
            }

            foreach (var child in _trafficList.GetChildren()) child.QueueFree();
            var fleets = state.Fleets.Values.Where(f => f.CurrentNodeId == _currentMarketId).Take(5);
            foreach (var f in fleets)
            {
                _trafficList.AddChild(new Label { Text = $"> {f.Id}: {f.CurrentTask}" });
            }

                        foreach (var child in _sustainmentList.GetChildren()) child.QueueFree();

            var sus = _bridge.GetSustainmentSnapshot(_currentMarketId);
            if (sus.Count == 0)
            {
                _sustainmentList.AddChild(new Label { Text = "(no active industry sites)" });
            }
            else
            {
                foreach (var v in sus)
                {
                    if (v.Obj is not Godot.Collections.Dictionary d) continue;

                    string siteId = d.ContainsKey("site_id") ? d["site_id"].ToString() : "?";
                    int healthBps = d.ContainsKey("health_bps") ? (int)d["health_bps"] : 0;
                    int effBps = d.ContainsKey("eff_bps_now") ? (int)d["eff_bps_now"] : 0;

                    float worstMargin = d.ContainsKey("worst_buffer_margin") ? (float)d["worst_buffer_margin"] : 0f;

                    int tStarve = d.ContainsKey("time_to_starve_ticks") ? (int)d["time_to_starve_ticks"] : -1;
                    float dStarve = d.ContainsKey("time_to_starve_days") ? (float)d["time_to_starve_days"] : 0f;

                    int tFail = d.ContainsKey("time_to_failure_ticks") ? (int)d["time_to_failure_ticks"] : -1;
                    float dFail = d.ContainsKey("time_to_failure_days") ? (float)d["time_to_failure_days"] : 0f;

                    var header = new Label
                    {
                        Text = $"{siteId} | Health(bps): {healthBps} | Eff(bps): {effBps} | Margin: {worstMargin:0.00} | Starve: {tStarve}t ({dStarve:0.00}d) | Fail: {tFail}t ({dFail:0.00}d)"
                    };
                    _sustainmentList.AddChild(header);

                    if (d.ContainsKey("inputs") && d["inputs"].Obj is Godot.Collections.Array inputsArr)
                    {
                        foreach (var iv in inputsArr)
                        {
                            if (iv.Obj is not Godot.Collections.Dictionary id) continue;

                            string good = id.ContainsKey("good_id") ? id["good_id"].ToString() : "?";
                            int have = id.ContainsKey("have_units") ? (int)id["have_units"] : 0;
                            int perTick = id.ContainsKey("per_tick_required") ? (int)id["per_tick_required"] : 0;
                            int target = id.ContainsKey("buffer_target_units") ? (int)id["buffer_target_units"] : 0;
                            int covT = id.ContainsKey("coverage_ticks") ? (int)id["coverage_ticks"] : 0;
                            float covD = id.ContainsKey("coverage_days") ? (float)id["coverage_days"] : 0f;
                            float m = id.ContainsKey("buffer_margin") ? (float)id["buffer_margin"] : 0f;

                            _sustainmentList.AddChild(new Label
                            {
                                Text = $"  - {good}: have {have}, req/t {perTick}, target {target}, cover {covT}t ({covD:0.00}d), margin {m:0.00}"
                            });
                        }
                    }
                }
            }

        });

        GetTree().CreateTimer(0.1).Timeout += Refresh;
    }

    private void SubmitTradeIntent(string good, int qty, bool isBuy)
    {
        if (_bridge == null || string.IsNullOrEmpty(_currentMarketId)) return;

        if (isBuy) _bridge.SubmitBuyIntent(_currentMarketId, good, qty);
        else _bridge.SubmitSellIntent(_currentMarketId, good, qty);
    }

    public void Close() => Visible = false;
}
