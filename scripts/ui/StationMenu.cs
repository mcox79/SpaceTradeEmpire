using Godot;
using System;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
    private Panel _panel;
    private Label _title;
    private Button _undockBtn;
    
    public override void _Ready()
    {
        // 1. Setup Full Screen Overlay
        AnchorRight = 1;
        AnchorBottom = 1;
        Visible = false;

        // 2. Create Panel
        _panel = new Panel();
        _panel.SetAnchorsPreset(LayoutPreset.Center);
        _panel.Size = new Vector2(400, 300);
        _panel.Position = new Vector2((GetViewportRect().Size.X - 400) / 2, (GetViewportRect().Size.Y - 300) / 2);
        AddChild(_panel);

        // 3. Title
        _title = new Label();
        _title.Text = "STATION INTERFACE";
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        _title.Position = new Vector2(0, 20);
        _title.Size = new Vector2(400, 40);
        _panel.AddChild(_title);

        // 4. Undock Button
        _undockBtn = new Button();
        _undockBtn.Text = "UNDOCK";
        _undockBtn.Size = new Vector2(200, 50);
        _undockBtn.Position = new Vector2(100, 200);
        _undockBtn.Pressed += OnUndockPressed;
        _panel.AddChild(_undockBtn);
    }

    public void Open(string stationName)
    {
        _title.Text = $"DOCKED AT: {stationName}";
        Visible = true;
        GetTree().Paused = true; // Pause physics while docked
        GD.Print($"[UI] Station Menu Opened: {stationName}");
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
        GD.Print("[UI] Station Menu Closed");
    }

    private void OnUndockPressed()
    {
        Close();
    }

    // This matches the signal signature from player.gd: signal shop_toggled(is_open, station_ref)
    public void OnShopToggled(bool isOpen, Node stationNode)
    {
        if (isOpen && stationNode != null)
        {
            // Try to get the Star name
            string name = stationNode.Name;
            if (stationNode.Get("NodeId") is string id) name = id;
            
            Open(name);
        }
        else
        {
            Close();
        }
    }
}