using Godot;
using System;
using SimCore;
using SimCore.Commands;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
    private PanelContainer _panel;
    private Label _title;
    private Label _credits;
    private VBoxContainer _marketList;
    private Button _closeBtn;
    
    private SimBridge _bridge;
    private string _currentNodeId;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        Visible = false;
        
        // UI SETUP
        AnchorRight = 1; AnchorBottom = 1;
        
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(600, 400);
        AddChild(_panel);

        var vbox = new VBoxContainer();
        _panel.AddChild(vbox);

        var header = new HBoxContainer();
        _title = new Label { Text = "STATION MARKET", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _credits = new Label { Text = "CREDITS: 0", HorizontalAlignment = HorizontalAlignment.Right };
        header.AddChild(_title);
        header.AddChild(_credits);
        vbox.AddChild(header);
        vbox.AddChild(new HSeparator());

        _marketList = new VBoxContainer();
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.AddChild(_marketList);
        vbox.AddChild(scroll);

        vbox.AddChild(new HSeparator());
        _closeBtn = new Button { Text = "UNDOCK" };
        
        // ACTIVE ACTION: Button triggers the Player Physics
        _closeBtn.Pressed += RequestUndock;
        vbox.AddChild(_closeBtn);
    }

    // PASSIVE ACTION: Responds to Player/System signals
    public void OnShopToggled(bool isOpen, Node stationNode)
    {
        if (isOpen)
        {
            Open("star_0");
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Close(); // Safe to call (Passive)
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public void Open(string nodeId)
    {
        _currentNodeId = nodeId;
        Visible = true;
        
        if (_bridge.Kernel.State.Nodes.TryGetValue(nodeId, out var node))
        {
            _title.Text = $"{node.Name} MARKET";
        }

        Refresh();
        GD.Print($"[UI] Opened Market at {_currentNodeId}");
    }

    // RESTORED API: Passive Close (UI Only)
    // Used by GalaxyView and OnShopToggled
    public void Close()
    {
        Visible = false;
        _currentNodeId = null;
    }

    // NEW METHOD: Active Undock (Physics + UI)
    // Used ONLY by the Button
    private void RequestUndock()
    {
        var player = GetTree().GetFirstNodeInGroup("Player");
        if (player != null && player.HasMethod("undock"))
        {
            // Triggers Player -> emit_signal(false) -> OnShopToggled(false) -> Close()
            // This breaks the recursion loop.
            player.Call("undock");
        }
        else
        {
            Close();
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void Refresh()
    {
        foreach (var c in _marketList.GetChildren()) c.QueueFree();

        if (_bridge == null || _bridge.Kernel == null) return;
        var state = _bridge.Kernel.State;

        _credits.Text = $"CREDITS: {state.PlayerCredits:N0}";

        if (!state.Nodes.TryGetValue(_currentNodeId, out var node)) return;
        if (!state.Markets.TryGetValue(node.MarketId, out var market)) return;

        foreach (var kvp in market.Inventory)
        {
            string goodId = kvp.Key;
            int supply = kvp.Value;
            int price = market.GetPrice(goodId);
            int playerStock = state.PlayerCargo.ContainsKey(goodId) ? state.PlayerCargo[goodId] : 0;

            var row = new HBoxContainer();
            
            var info = new Label 
            { 
                Text = $"{goodId.ToUpper()} | Price: {price} | Supply: {supply} | You Have: {playerStock}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill 
            };
            row.AddChild(info);

            var btnBuy = new Button { Text = "BUY" };
            btnBuy.Pressed += () => { ExecuteTrade(new BuyCommand(market.Id, goodId, 1)); };
            row.AddChild(btnBuy);

            var btnSell = new Button { Text = "SELL", Disabled = (playerStock <= 0) };
            btnSell.Pressed += () => { ExecuteTrade(new SellCommand(market.Id, goodId, 1)); };
            row.AddChild(btnSell);

            _marketList.AddChild(row);
        }
    }

    private void ExecuteTrade(ICommand cmd)
    {
        _bridge.Kernel.EnqueueCommand(cmd);
        _bridge.Kernel.Step();
        Refresh();
    }
}