using Godot;
using System;
using System.Linq;
using Godot.Collections;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class FleetMenu : Control
{
    [Signal] public delegate void RequestCloseEventHandler();

    private SimBridge _bridge = null!;

    private VBoxContainer _list = null!;
    private Label _title = null!;
    private Label _heartbeat = null!;
    private Control _dimmer = null!;
    private bool _modalApplied = false;

    private CheckBox _autoRefresh = null!;
    private SpinBox _refreshMs = null!;

    private double _accumMs = 0.0;
    private ulong _lastRefreshMs = 0;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        SetupUI();
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        if (_autoRefresh == null || !_autoRefresh.ButtonPressed) return;

        _accumMs += delta * 1000.0;

        var interval = (_refreshMs != null) ? _refreshMs.Value : 250.0;
        if (interval < 50.0) interval = 50.0;

        if (_accumMs >= interval)
        {
            _accumMs = 0.0;
            Refresh();
        }
    }

    private void SetupUI()
    {
        ZIndex = 210;
        ZAsRelative = false;
        MouseFilter = MouseFilterEnum.Stop;

        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = 0;
        OffsetTop = 0;
        OffsetRight = 0;
        OffsetBottom = 0;

        var dim = new ColorRect();
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        dim.OffsetLeft = 0;
        dim.OffsetTop = 0;
        dim.OffsetRight = 0;
        dim.OffsetBottom = 0;
        dim.Color = new Color(0f, 0f, 0f, 0.65f);
        dim.MouseFilter = MouseFilterEnum.Stop;
        dim.ZIndex = 109;
        dim.ZAsRelative = false;
        AddChild(dim);
        _dimmer = dim;
        _dimmer.Visible = false;

        var panel = new PanelContainer();
        panel.ZIndex = 110;
        panel.ZAsRelative = false;
        panel.MouseFilter = MouseFilterEnum.Stop;

        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.OffsetLeft = -560;
        panel.OffsetTop = -340;
        panel.OffsetRight = 560;
        panel.OffsetBottom = 340;
        panel.CustomMinimumSize = new Vector2(1120, 680);

        var sb = new StyleBoxFlat();
        sb.BgColor = new Color(0.06f, 0.06f, 0.08f, 0.98f);
        sb.BorderColor = new Color(0.20f, 0.20f, 0.25f, 1.0f);
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(10);
        panel.AddThemeStyleboxOverride("panel", sb);

        AddChild(panel);

        var root = new VBoxContainer();
        panel.AddChild(root);

        _title = new Label { Text = "FLEETS", HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(_title);

        _heartbeat = new Label { Text = "Refreshed: (never)", HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(_heartbeat);

        root.AddChild(new HSeparator());

        var topRow = new HBoxContainer();
        root.AddChild(topRow);

        var btnRefresh = new Button { Text = "Refresh" };
        btnRefresh.Pressed += Refresh;
        topRow.AddChild(btnRefresh);

        _autoRefresh = new CheckBox { Text = "Auto", ButtonPressed = true };
        topRow.AddChild(_autoRefresh);

        topRow.AddChild(new Label { Text = "ms:" });

        _refreshMs = new SpinBox
        {
            MinValue = 50,
            MaxValue = 5000,
            Step = 50,
            Value = 250,
            CustomMinimumSize = new Vector2(100, 0)
        };
        topRow.AddChild(_refreshMs);

        var btnClose = new Button { Text = "Close" };
        btnClose.Pressed += () => { Close(); EmitSignal(SignalName.RequestClose); };
        topRow.AddChild(btnClose);

        root.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(1100, 580);
        root.AddChild(scroll);

        _list = new VBoxContainer();
        scroll.AddChild(_list);
    }

    public void Open()
    {
        Visible = true;
        if (_dimmer != null) _dimmer.Visible = true;

        ApplyModal(true);

        _accumMs = 0.0;
        Refresh();
    }

    public void Close()
    {
        Visible = false;
        if (_dimmer != null) _dimmer.Visible = false;

        ApplyModal(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo)
        {
            if (k.Keycode == Key.F1)
            {
                if (Visible) Close();
                else Open();

                GetViewport().SetInputAsHandled();
                return;
            }

            if (Visible && k.Keycode == Key.Escape)
            {
                Close();
                EmitSignal(SignalName.RequestClose);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    private void ApplyModal(bool enable)
    {
        if (enable && _modalApplied) return;
        if (!enable && !_modalApplied) return;

        var player = GetTree().GetFirstNodeInGroup("Player");
        if (player != null && player.HasMethod("set_input_enabled"))
        {
            player.Call("set_input_enabled", !enable);
        }

        _modalApplied = enable;
    }

    private static string GetStr(Dictionary d, string key)
    {
        if (d.ContainsKey(key))
        {
            var v = d[key];
            if (v.VariantType == Variant.Type.Nil) return "";
            return v.ToString();
        }
        return "";
    }

    private static int GetInt(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return 0;
        var v = d[key];
        try { return (int)v; } catch { return 0; }
    }

    private void Refresh()
    {
        if (_bridge == null) return;

        foreach (var child in _list.GetChildren())
            child.QueueFree();

        var arr = _bridge.GetFleetExplainSnapshot();

        var rows = arr
            .Select(v => v.Obj as Dictionary)
            .Where(d => d != null)
            .OrderBy(d => GetStr(d!, "id"), StringComparer.Ordinal)
            .ToArray();

        _lastRefreshMs = Time.GetTicksMsec();
        _heartbeat.Text = $"Refreshed: {_lastRefreshMs}ms";

        if (rows.Length == 0)
        {
            _list.AddChild(new Label { Text = "(no fleets)" });
            return;
        }

        _list.AddChild(new Label
        {
            Text = "ID | Node | State | Task | JobPhase | JobGood | Remaining | Cargo | Route",
            Modulate = new Color(0.7f, 0.7f, 1f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _list.AddChild(new HSeparator());

        foreach (var d in rows)
        {
            var id = GetStr(d!, "id");
            var node = GetStr(d!, "current_node_id");
            var state = GetStr(d!, "state");
            var task = GetStr(d!, "task");

            var jobPhase = GetStr(d!, "job_phase");
            var jobGood = GetStr(d!, "job_good_id");
            var remaining = GetInt(d!, "job_remaining");

            var cargo = GetStr(d!, "cargo_summary");
            var route = GetStr(d!, "route_progress");

            _list.AddChild(new Label
            {
                Text = $"{id} | {node} | {state} | {task} | {jobPhase} | {jobGood} | {remaining} | {cargo} | {route}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(1080, 0)
            });
        }
    }
}
