using Godot;
using System.Linq;
using SimCore;
using SimCore.Commands; // Now we can use TradeCommand
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
    [Signal] public delegate void RequestUndockEventHandler();

    private Label _titleLabel;
    private VBoxContainer _marketList;
    private VBoxContainer _industryList;
    private VBoxContainer _trafficList;
    private Label _creditsLabel; // New
    
    private SimBridge _bridge;
    private string _currentNodeId = "";

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
        panel.CustomMinimumSize = new Vector2(800, 600); // Wider for buttons
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        _titleLabel = new Label { Text = "STATION MENU", HorizontalAlignment = HorizontalAlignment.Center };
        vbox.AddChild(_titleLabel);
        
        _creditsLabel = new Label { Text = "CREDITS: 0", HorizontalAlignment = HorizontalAlignment.Right, Modulate = Colors.Gold };
        vbox.AddChild(_creditsLabel);

        // --- MARKET ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "MARKET (Buy/Sell)", Modulate = new Color(0.7f, 0.7f, 1f) });
        
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 200) };
        vbox.AddChild(scroll);
        _marketList = new VBoxContainer();
        scroll.AddChild(_marketList);

        // --- TRAFFIC ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "TRAFFIC MONITOR", Modulate = new Color(1f, 0.7f, 0.7f) });
        _trafficList = new VBoxContainer();
        vbox.AddChild(_trafficList);

        // --- FOOTER ---
        vbox.AddChild(new HSeparator());
        var closeBtn = new Button { Text = "Undock" };
        closeBtn.Pressed += () => EmitSignal(SignalName.RequestUndock);
        vbox.AddChild(closeBtn);
    }

    public void OnShopToggled(bool isOpen, string nodeId)
    {
        Visible = isOpen;
        _currentNodeId = nodeId;
        if (isOpen) Refresh();
    }

    public void Refresh()
    {
        if (_bridge == null || string.IsNullOrEmpty(_currentNodeId)) return;

        // Get Player State for Credits/Cargo
        var snapshot = _bridge.GetPlayerSnapshot();
        int playerCredits = snapshot.Contains("credits") ? (int)snapshot["credits"] : 0;
        var playerCargo = snapshot.Contains("cargo") ? (Godot.Collections.Dictionary)snapshot["cargo"] : new Godot.Collections.Dictionary();
        
        _creditsLabel.Text = $"CREDITS: {playerCredits:N0}";

        _bridge.ExecuteSafeRead(state =>
        {
            if (state.Nodes.ContainsKey(_currentNodeId))
                _titleLabel.Text = state.Nodes[_currentNodeId].Name.ToUpper();

            // 1. Market with Buttons
            foreach (var child in _marketList.GetChildren()) child.QueueFree();
            
            if (state.Markets.TryGetValue(_currentNodeId, out var market))
            {
                // Combine Market Inventory + Player Cargo (to allow selling things the market doesn't have yet)
                var allGoods = market.Inventory.Keys.Union(playerCargo.Keys.Cast<string>()).Distinct().OrderBy(k => k);

                foreach (var good in allGoods)
                {
                    int marketQty = market.Inventory.GetValueOrDefault(good, 0);
                    int playerQty = playerCargo.Contains(good) ? (int)playerCargo[good] : 0;
                    int price = market.GetPrice(good);
                    
                    var hbox = new HBoxContainer();
                    _marketList.AddChild(hbox);
                    
                    // Info
                    var infoColor = (price > 110) ? Colors.Salmon : (price < 90 ? Colors.LightGreen : Colors.White);
                    var lbl = new Label { 
                        Text = $"{good.PadRight(10)} | Stock: {marketQty} | Price: ${price} | You: {playerQty}",
                        Modulate = infoColor,
                        CustomMinimumSize = new Vector2(400, 0)
                    };
                    hbox.AddChild(lbl);
                    
                    // BUY Button (1 Unit)
                    var btnBuy = new Button { Text = "Buy 1" };
                    btnBuy.Disabled = (marketQty <= 0 || playerCredits < price);
                    btnBuy.Pressed += () => SendTrade(good, 1, TradeType.Buy);
                    hbox.AddChild(btnBuy);
                    
                    // SELL Button (1 Unit)
                    var btnSell = new Button { Text = "Sell 1" };
                    btnSell.Disabled = (playerQty <= 0);
                    btnSell.Pressed += () => SendTrade(good, 1, TradeType.Sell);
                    hbox.AddChild(btnSell);
                }
            }
            
            // 2. Traffic (Simplified for space)
            foreach (var child in _trafficList.GetChildren()) child.QueueFree();
            var fleets = state.Fleets.Values.Where(f => f.CurrentNodeId == _currentNodeId).Take(5);
            foreach(var f in fleets)
            {
                _trafficList.AddChild(new Label { Text = $"> {f.Id}: {f.CurrentTask}" });
            }
        });
    }

    private void SendTrade(string good, int qty, TradeType type)
    {
        var cmd = new TradeCommand("player", _currentNodeId, good, qty, type);
        _bridge.EnqueueCommand(cmd);
        
        // Small delay to allow Sim to process, then refresh UI
        // In a real app we'd listen for an event, but polling/timer works for prototype
        GetTree().CreateTimer(0.1).Timeout += Refresh;
    }

    public void Close() => Visible = false;
}