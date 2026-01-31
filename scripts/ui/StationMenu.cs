using Godot;
using System.Linq;
using SimCore;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
    [Signal] public delegate void RequestUndockEventHandler();

    private Label _titleLabel;
    private VBoxContainer _marketList;
    private VBoxContainer _industryList;
    private VBoxContainer _trafficList;
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
        panel.CustomMinimumSize = new Vector2(600, 500);
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        _titleLabel = new Label { Text = "STATION MENU", HorizontalAlignment = HorizontalAlignment.Center };
        vbox.AddChild(_titleLabel);

        // --- MARKET ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "MARKET INVENTORY (Qty | Price)", Modulate = new Color(0.7f, 0.7f, 1f) });
        _marketList = new VBoxContainer();
        vbox.AddChild(_marketList);

        // --- INDUSTRY ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "LOCAL INDUSTRY", Modulate = new Color(0.7f, 1f, 0.7f) });
        _industryList = new VBoxContainer();
        vbox.AddChild(_industryList);

        // --- TRAFFIC ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "TRAFFIC MONITOR (Active Logistics)", Modulate = new Color(1f, 0.7f, 0.7f) });
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

        _bridge.ExecuteSafeRead(state =>
        {
            if (state.Nodes.ContainsKey(_currentNodeId))
                _titleLabel.Text = state.Nodes[_currentNodeId].Name.ToUpper();

            // 1. Market with Pricing
            foreach (var child in _marketList.GetChildren()) child.QueueFree();
            if (state.Markets.TryGetValue(_currentNodeId, out var market))
            {
                foreach (var kv in market.Inventory)
                {
                    string good = kv.Key;
                    int qty = kv.Value;
                    int price = market.GetPrice(good);
                    
                    // Note: Single quotes protect the $ here
                    var lbl = new Label { Text = $"{good}: {qty}  (${price})" };
                    
                    if (price > 110) lbl.Modulate = new Color(1f, 0.5f, 0.5f); // Red
                    else if (price < 90) lbl.Modulate = new Color(0.5f, 1f, 0.5f); // Green
                    
                    _marketList.AddChild(lbl);
                }
            }

            // 2. Industry
            foreach (var child in _industryList.GetChildren()) child.QueueFree();
            var localSites = state.IndustrySites.Values.Where(s => s.NodeId == _currentNodeId);
            foreach (var site in localSites)
            {
                var inputs = string.Join(",", site.Inputs.Select(i => $"{i.Key}({i.Value})"));
                var outputs = string.Join(",", site.Outputs.Select(o => $"{o.Key}({o.Value})"));
                _industryList.AddChild(new Label { Text = $"{site.Id}: {inputs} => {outputs}" });
            }

            // 3. Traffic
            foreach (var child in _trafficList.GetChildren()) child.QueueFree();
            var relevantFleets = state.Fleets.Values.Where(f =>
                f.DestinationNodeId == _currentNodeId ||
                (f.CurrentNodeId == _currentNodeId && f.CurrentJob != null)
            );

            foreach (var fleet in relevantFleets)
            {
                string status = fleet.DestinationNodeId == _currentNodeId ? "INBOUND" : "OUTBOUND";
                var progress = (int)(fleet.TravelProgress * 100);
                var lbl = new Label { Text = $"[{status}] {fleet.Id}: {fleet.CurrentTask} ({progress}%)" };
                if (status == "OUTBOUND") lbl.Modulate = new Color(1, 1, 0.5f);
                _trafficList.AddChild(lbl);
            }
        });
    }

    public void Close() => Visible = false;
}