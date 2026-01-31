using Godot;
using System.Linq;
using System.Collections.Generic; // For C# Dictionary extensions
using SimCore;
using SimCore.Commands;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
    [Signal] public delegate void RequestUndockEventHandler();

    private Label _titleLabel;
    private VBoxContainer _marketList;
    private VBoxContainer _industryList;
    private VBoxContainer _trafficList;
    private Label _creditsLabel;
    
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
        panel.CustomMinimumSize = new Vector2(800, 600);
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
        
        // FIX: Godot Dictionary uses .ContainsKey (or cast to IDictionary)
        int playerCredits = 0;
        if (snapshot.ContainsKey("credits")) 
            playerCredits = (int)snapshot["credits"];
        
        // FIX: Safe Casting of nested dictionary
        var playerCargo = new Godot.Collections.Dictionary();
        if (snapshot.ContainsKey("cargo")) 
        {
             // Safely cast the variant to a Dictionary
             var variant = snapshot["cargo"];
             if (variant.Obj is Godot.Collections.Dictionary nested)
             {
                 playerCargo = nested;
             }
        }
        
        _creditsLabel.Text = $"CREDITS: {playerCredits:N0}";

        _bridge.ExecuteSafeRead(state =>
        {
            if (state.Nodes.ContainsKey(_currentNodeId))
                _titleLabel.Text = state.Nodes[_currentNodeId].Name.ToUpper();

            foreach (var child in _marketList.GetChildren()) child.QueueFree();
            
            if (state.Markets.TryGetValue(_currentNodeId, out var market))
            {
                // FIX: Manually build list of goods to avoid LINQ casting issues with Godot Variants
                var allGoods = new HashSet<string>();
                foreach(var k in market.Inventory.Keys) allGoods.Add(k);
                foreach(var k in playerCargo.Keys) allGoods.Add(k.ToString());
                
                var sortedGoods = allGoods.OrderBy(k => k);

                foreach (var good in sortedGoods)
                {
                    int marketQty = market.Inventory.GetValueOrDefault(good, 0);
                    
                    int playerQty = 0;
                    if (playerCargo.ContainsKey(good)) playerQty = (int)playerCargo[good];
                    
                    int price = market.GetPrice(good);
                    
                    var hbox = new HBoxContainer();
                    _marketList.AddChild(hbox);
                    
                    var infoColor = (price > 110) ? Colors.Salmon : (price < 90 ? Colors.LightGreen : Colors.White);
                    var lbl = new Label { 
                        Text = $"{good.PadRight(10)} | Stock: {marketQty} | Price: ${price} | You: {playerQty}",
                        Modulate = infoColor,
                        CustomMinimumSize = new Vector2(400, 0)
                    };
                    hbox.AddChild(lbl);
                    
                    var btnBuy = new Button { Text = "Buy 1" };
                    btnBuy.Disabled = (marketQty <= 0 || playerCredits < price);
                    btnBuy.Pressed += () => SendTrade(good, 1, TradeType.Buy);
                    hbox.AddChild(btnBuy);
                    
                    var btnSell = new Button { Text = "Sell 1" };
                    btnSell.Disabled = (playerQty <= 0);
                    btnSell.Pressed += () => SendTrade(good, 1, TradeType.Sell);
                    hbox.AddChild(btnSell);
                }
            }
            
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
        GetTree().CreateTimer(0.1).Timeout += Refresh;
    }

    public void Close() => Visible = false;
}