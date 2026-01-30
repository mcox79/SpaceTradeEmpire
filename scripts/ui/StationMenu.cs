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

    // --- NEW: Signal Handler for Player.gd ---
    public void OnShopToggled(bool isOpen, Node stationNode)
    {
        if (isOpen)
        {
            // SLICE 1 HACK: Map the physical station to the first star in the Sim
            Open("star_0");
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Close();
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

    public void Close()
    {
        Visible = false;
        _currentNodeId = null;
        
        // Tell player to undock (restore physics)
        var player = GetTree().GetFirstNodeInGroup("Player");
        if (player != null && player.HasMethod("undock"))
        {
            player.Call("undock");
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