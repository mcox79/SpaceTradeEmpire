using Godot;
using SimCore.Commands;
using SpaceTradeEmpire.Bridge;
using System.Linq;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : CanvasLayer
{
    private SimBridge _bridge;
    private string _currentNodeId;
    
    // UI Elements
    private PanelContainer _panel;
    private Label _lblTitle;
    private Label _lblPrice;
    private Label _lblStock;
    private Label _lblPlayer;
    private Button _btnBuy;
    private Button _btnClose;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        SetupUI();
        Visible = false; // Start hidden
    }

    public void Open(string nodeId)
    {
        _currentNodeId = nodeId;
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!Visible || string.IsNullOrEmpty(_currentNodeId)) return;
        
        var state = _bridge.Kernel.State;
        if (!state.Nodes.ContainsKey(_currentNodeId)) return;

        var node = state.Nodes[_currentNodeId];
        var market = state.Markets[node.MarketId];

        // Update Labels
        _lblTitle.Text = $"SYSTEM: {node.Name}";
        _lblPrice.Text = $"PRICE: {market.CurrentPrice} Credits";
        _lblStock.Text = $"STOCK: {market.Inventory} Units";
        _lblPlayer.Text = $"MY CREDITS: {state.PlayerCredits}\nMY CARGO: {GetTotalCargo()}";

        // Enable/Disable Buy Button
        _btnBuy.Disabled = (state.PlayerCredits < market.CurrentPrice || market.Inventory <= 0);
    }

    private int GetTotalCargo()
    {
        var state = _bridge.Kernel.State;
        // Check global cargo (Slice 1)
        return state.PlayerCargo.Values.Sum();
    }

    private void OnBuyPressed()
    {
        if (string.IsNullOrEmpty(_currentNodeId)) return;
        var node = _bridge.Kernel.State.Nodes[_currentNodeId];
        
        // Send Command
        var cmd = new BuyCommand(node.MarketId, 1);
        _bridge.Kernel.EnqueueCommand(cmd);
    }

    private void SetupUI()
    {
        // Root Panel
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(300, 200);
        AddChild(_panel);

        var vbox = new VBoxContainer();
        _panel.AddChild(vbox);

        // Header
        _lblTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _lblTitle.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(_lblTitle);
        vbox.AddChild(new HSeparator());

        // Info
        _lblPrice = new Label();
        vbox.AddChild(_lblPrice);
        
        _lblStock = new Label();
        vbox.AddChild(_lblStock);
        
        vbox.AddChild(new HSeparator());

        // Player Info
        _lblPlayer = new Label { Modulate = new Color(0, 1, 0) }; // Green
        vbox.AddChild(_lblPlayer);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) }); // Spacer

        // Actions
        _btnBuy = new Button { Text = "BUY (1 Unit)" };
        _btnBuy.Pressed += OnBuyPressed;
        vbox.AddChild(_btnBuy);

        _btnClose = new Button { Text = "CLOSE" };
        _btnClose.Pressed += () => Visible = false;
        vbox.AddChild(_btnClose);
    }
}