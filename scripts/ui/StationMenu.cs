using Godot;

namespace SpaceTradeEmpire.UI;

public partial class StationMenu : Control
{
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private Label _body = null!;
    private Button _closeBtn = null!;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;

        _panel = new PanelContainer();
        _panel.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_panel);

        // Center-ish placement without relying on anchors
        _panel.Position = new Vector2(24, 24);
        _panel.CustomMinimumSize = new Vector2(360, 180);

        var vbox = new VBoxContainer();
        vbox.MouseFilter = MouseFilterEnum.Stop;
        _panel.AddChild(vbox);

        _title = new Label { Text = "Station" };
        _title.MouseFilter = MouseFilterEnum.Stop;
        vbox.AddChild(_title);

        _body = new Label { Text = "" };
        _body.MouseFilter = MouseFilterEnum.Stop;
        vbox.AddChild(_body);

        _closeBtn = new Button { Text = "Close" };
        _closeBtn.MouseFilter = MouseFilterEnum.Stop;
        _closeBtn.Pressed += Close;
        vbox.AddChild(_closeBtn);
    }

    public void Open(string nodeId)
    {
        Visible = true;
        _title.Text = $"Node: {nodeId}";
        _body.Text = "Click a connected star to travel.\nClick Close to dismiss.";
    }

    public void Close()
    {
        Visible = false;
    }
}
