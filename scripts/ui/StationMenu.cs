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
        AnchorRight = 1; AnchorBottom = 1; // Full screen overlay to block mouse
        
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(600, 400);
        AddChild(_panel);

        var vbox = new VBoxContainer();
        _panel.AddChild(vbox);

        // Header
        var header = new HBoxContainer();
        _title = new Label { Text = "STATION MARKET", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _credits = new Label { Text = "CREDITS: 0", HorizontalAlignment = HorizontalAlignment.Right };
        header.AddChild(_title);
        header.AddChild(_credits);
        vbox.AddChild(header);
        vbox.AddChild(new HSeparator());

        // Market Rows
        _marketList = new VBoxContainer();
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        scroll.AddChild(_marketList);
        vbox.AddChild(scroll);

        // Footer
        vbox.AddChild(new HSeparator());
        _closeBtn = new Button { Text = "UNDOCK" };
        _closeBtn.Pressed += Close;
        vbox.AddChild(_closeBtn);
    }

    public void Open(string nodeId)
    {
        _currentNodeId = nodeId;
        Visible = true;
        
        // Try to locate the Market ID from the Node
        if (_bridge.Kernel.State.Nodes.TryGetValue(nodeId, out var node))
        {
            _title.Text = $"{node.Name} MARKET";
        }

        Refresh();
        GD.Print($"[UI] Opened Market at {_currentNodeId}");
    }

    public void Close()
    {
        Visible = false;
        _currentNodeId = null;
    }

    private void Refresh()
    {
        // CLEAR OLD
        foreach (var c in _marketList.GetChildren()) c.QueueFree();

        if (_bridge == null || _bridge.Kernel == null) return;
        var state = _bridge.Kernel.State;

        // UPDATE HEADER
        _credits.Text = $"CREDITS: {state.PlayerCredits:N0}";

        // FIND MARKET
        if (!state.Nodes.TryGetValue(_currentNodeId, out var node)) return;
        if (!state.Markets.TryGetValue(node.MarketId, out var market)) return;

        // RENDER ROWS
        foreach (var kvp in market.Inventory)
        {
            string goodId = kvp.Key;
            int supply = kvp.Value;
            int price = market.GetPrice(goodId);
            int playerStock = state.PlayerCargo.ContainsKey(goodId) ? state.PlayerCargo[goodId] : 0;

            var row = new HBoxContainer();
            
            // Info Label
            var info = new Label 
            { 
                Text = $"{goodId.ToUpper()} | Price: {price} | Supply: {supply} | You Have: {playerStock}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill 
            };
            row.AddChild(info);

            // Buy Button
            var btnBuy = new Button { Text = "BUY" };
            btnBuy.Pressed += () => { ExecuteTrade(new BuyCommand(market.Id, goodId, 1)); };
            row.AddChild(btnBuy);

            // Sell Button
            var btnSell = new Button { Text = "SELL", Disabled = (playerStock <= 0) };
            btnSell.Pressed += () => { ExecuteTrade(new SellCommand(market.Id, goodId, 1)); };
            row.AddChild(btnSell);

            _marketList.AddChild(row);
        }
    }

    private void ExecuteTrade(ICommand cmd)
    {
        // 1. Enqueue Command to Kernel
        _bridge.Kernel.EnqueueCommand(cmd);
        
        // 2. Force a single Simulation Step immediately (for responsive UI in Slice 1)
        // In Slice 2, we will wait for the tick. For Slice 1 "Trucker", instant feedback is better.
        // We call Kernel.Step() safely via the Bridge loop, or we just wait for the next frame?
        // Let's just wait for the next frame update to refresh, but we can optimistically refresh.
        
        // Force the kernel to process the command queue NOW so we see the update instantly
        // This is a "Micro-Tick" for UI interaction.
        _bridge.Kernel.Step();
        
        Refresh();
    }
}