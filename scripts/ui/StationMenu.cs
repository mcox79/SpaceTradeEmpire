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
        // Basic Panel Setup
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(500, 400);
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        _titleLabel = new Label { Text = "STATION MENU", HorizontalAlignment = HorizontalAlignment.Center };
        vbox.AddChild(_titleLabel);

        // --- MARKET SECTION ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "MARKET INVENTORY", Modulate = new Color(0.7f, 0.7f, 1f) });
        _marketList = new VBoxContainer();
        vbox.AddChild(_marketList);

        // --- INDUSTRY SECTION ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "LOCAL INDUSTRY", Modulate = new Color(0.7f, 1f, 0.7f) });
        _industryList = new VBoxContainer();
        vbox.AddChild(_industryList);

        // --- TRAFFIC SECTION (NEW) ---
        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "INBOUND TRAFFIC", Modulate = new Color(1f, 0.7f, 0.7f) });
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
        if (isOpen)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (_bridge == null || string.IsNullOrEmpty(_currentNodeId)) return;
        
        _bridge.ExecuteSafeRead(state => 
        {
            // 1. Update Title
            if (state.Nodes.ContainsKey(_currentNodeId))
                _titleLabel.Text = state.Nodes[_currentNodeId].Name.ToUpper();

            // 2. Market Inventory
            foreach (var child in _marketList.GetChildren()) child.QueueFree();
            if (state.Markets.TryGetValue(_currentNodeId, out var market))
            {
                foreach (var kv in market.Inventory)
                {
                    var row = new Label { Text = $"{kv.Key}: {kv.Value}" };
                    _marketList.AddChild(row);
                }
            }

            // 3. Industry Sites
            foreach (var child in _industryList.GetChildren()) child.QueueFree();
            var localSites = state.IndustrySites.Values.Where(s => s.NodeId == _currentNodeId);
            foreach (var site in localSites)
            {
                var inputs = string.Join(",", site.Inputs.Select(i => $"{i.Key}({i.Value})"));
                var outputs = string.Join(",", site.Outputs.Select(o => $"{o.Key}({o.Value})"));
                var lbl = new Label { Text = $"{site.Id}: {inputs} => {outputs}" };
                _industryList.AddChild(lbl);
            }

            // 4. Traffic Monitor
            foreach (var child in _trafficList.GetChildren()) child.QueueFree();
            var inbound = state.Fleets.Values.Where(f => f.DestinationNodeId == _currentNodeId);
            foreach (var fleet in inbound)
            {
                var progress = (int)(fleet.TravelProgress * 100);
                var lbl = new Label { Text = $"FLEET {fleet.Id} [{fleet.OwnerId}] :: {fleet.CurrentTask} ({progress}%)" };
                _trafficList.AddChild(lbl);
            }
        });
    }
    
    public void Close() => Visible = false;
}