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
        panel.CustomMinimumSize = new Vector2(400, 300);
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        _titleLabel = new Label { Text = "STATION MENU", HorizontalAlignment = HorizontalAlignment.Center };
        vbox.AddChild(_titleLabel);

        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "MARKET" });
        _marketList = new VBoxContainer();
        vbox.AddChild(_marketList);

        vbox.AddChild(new HSeparator());
        vbox.AddChild(new Label { Text = "INDUSTRY" });
        _industryList = new VBoxContainer();
        vbox.AddChild(_industryList);

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

            // 3. Industry Sites (Slice 2 Feature)
            foreach (var child in _industryList.GetChildren()) child.QueueFree();
            var localSites = state.IndustrySites.Values.Where(s => s.NodeId == _currentNodeId);
            foreach (var site in localSites)
            {
                // Format: "Refinery: Ore(10) -> Metal(5)"
                var inputs = string.Join(",", site.Inputs.Select(i => $"{i.Key}({i.Value})"));
                var outputs = string.Join(",", site.Outputs.Select(o => $"{o.Key}({o.Value})"));
                var lbl = new Label { Text = $"FACILITY: {inputs} => {outputs}" };
                lbl.Modulate = new Color(0.7f, 1f, 0.7f); // Light green
                _industryList.AddChild(lbl);
            }
        });
    }
    
    public void Close() => Visible = false;
}