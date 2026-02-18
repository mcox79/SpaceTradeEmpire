using Godot;
using System;
using System.Linq;
using Godot.Collections;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.UI;

public partial class ProgramsMenu : Control
{
    [Signal] public delegate void RequestCloseEventHandler();

    private SimBridge _bridge = null!;

    private VBoxContainer _list = null!;
    private Label _detail = null!;
    private Label _title = null!;

    private string _selectedProgramId = "";

    private Control _dimmer = null!;
    private bool _modalApplied = false;

    private CheckBox _showCancelled = null!;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        SetupUI();
        Visible = false;
    }

    private void SetupUI()
    {
        ZIndex = 200;
        ZAsRelative = false;
        MouseFilter = MouseFilterEnum.Stop;

        // Cover the whole viewport
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = 0;
        OffsetTop = 0;
        OffsetRight = 0;
        OffsetBottom = 0;

        // Modal dimmer (blocks clicks behind the menu)
        var dim = new ColorRect();
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        dim.OffsetLeft = 0;
        dim.OffsetTop = 0;
        dim.OffsetRight = 0;
        dim.OffsetBottom = 0;
        dim.Color = new Color(0f, 0f, 0f, 0.65f);
        dim.MouseFilter = MouseFilterEnum.Stop;
        dim.ZIndex = 99;
        dim.ZAsRelative = false;
        AddChild(dim);
        _dimmer = dim;
        _dimmer.Visible = false;

        // Opaque panel
        var panel = new PanelContainer();
        panel.ZIndex = 100;
        panel.ZAsRelative = false;
        panel.MouseFilter = MouseFilterEnum.Stop;

        // Wider to reduce truncation
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.OffsetLeft = -520;
        panel.OffsetTop = -320;
        panel.OffsetRight = 520;
        panel.OffsetBottom = 320;
        panel.CustomMinimumSize = new Vector2(1040, 640);

        var sb = new StyleBoxFlat();
        sb.BgColor = new Color(0.06f, 0.06f, 0.08f, 0.98f);
        sb.BorderColor = new Color(0.20f, 0.20f, 0.25f, 1.0f);
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(10);
        panel.AddThemeStyleboxOverride("panel", sb);

        AddChild(panel);

        var root = new VBoxContainer();
        panel.AddChild(root);

        _title = new Label { Text = "PROGRAMS", HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(_title);

        root.AddChild(new HSeparator());

        var topRow = new HBoxContainer();
        root.AddChild(topRow);

        var btnRefresh = new Button { Text = "Refresh" };
        btnRefresh.Pressed += Refresh;
        topRow.AddChild(btnRefresh);

        _showCancelled = new CheckBox { Text = "Show cancelled" };
        _showCancelled.Toggled += _ => Refresh();
        topRow.AddChild(_showCancelled);

        var btnClose = new Button { Text = "Close" };
        btnClose.Pressed += () => { Close(); EmitSignal(SignalName.RequestClose); };
        topRow.AddChild(btnClose);

        root.AddChild(new HSeparator());

        var split = new HBoxContainer();
        root.AddChild(split);

        // Left: program list
        var left = new VBoxContainer();
        left.CustomMinimumSize = new Vector2(680, 0);
        split.AddChild(left);

        left.AddChild(new Label { Text = "All Programs" });

        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(680, 520);
        left.AddChild(scroll);

        _list = new VBoxContainer();
        scroll.AddChild(_list);

        // Right: details
        var right = new VBoxContainer();
        right.CustomMinimumSize = new Vector2(340, 0);
        split.AddChild(right);

        right.AddChild(new Label { Text = "Selected Program" });

        _detail = new Label
        {
            Text = "(none)",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(340, 520)
        };
        right.AddChild(_detail);
    }

    public void Open()
    {
        Visible = true;

        if (_dimmer != null)
            _dimmer.Visible = true;

        ApplyModal(true);
        Refresh();
    }

    public void Close()
    {
        Visible = false;

        if (_dimmer != null)
            _dimmer.Visible = false;

        ApplyModal(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
        {
            Close();
            EmitSignal(SignalName.RequestClose);
            GetViewport().SetInputAsHandled();
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

        var arr = _bridge.GetProgramExplainSnapshot();

        var rows = arr
                .Select(v => v.Obj as Dictionary)
                .Where(d => d != null)
                .OrderBy(d => GetStr(d!, "id"))
                .ToArray();

        if (_showCancelled != null && !_showCancelled.ButtonPressed)
        {
            rows = rows
                    .Where(d => GetStr(d!, "status") != "Cancelled")
                    .ToArray();
        }

        if (rows.Length == 0)
        {
            _list.AddChild(new Label { Text = "(no programs)" });
            _selectedProgramId = "";
            _detail.Text = "(none)";
            return;
        }

        foreach (var d in rows)
        {
            var id = GetStr(d!, "id");
            var kind = GetStr(d!, "kind");
            var status = GetStr(d!, "status");
            var marketId = GetStr(d!, "market_id");
            var goodId = GetStr(d!, "good_id");

            var qty = GetInt(d!, "quantity");
            var cad = GetInt(d!, "cadence_ticks");
            var next = GetInt(d!, "next_run_tick");
            var last = GetInt(d!, "last_run_tick");

            var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            _list.AddChild(row);

            var btnSelect = new Button { Text = "Select" };
            btnSelect.Pressed += () => SelectProgram(id);
            row.AddChild(btnSelect);

            // Wrap instead of clipping
            var lbl = new Label
            {
                Text = $"{id} | {kind} | {status} | {marketId}:{goodId} | q={qty} cad={cad}t | last={last} next={next}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(520, 0)
            };
            row.AddChild(lbl);

            var btnStart = new Button { Text = "Start" };
            btnStart.Disabled = status == "Running" || status == "Cancelled";
            btnStart.Pressed += () => { _bridge.StartProgram(id); Refresh(); };
            row.AddChild(btnStart);

            var btnPause = new Button { Text = "Pause" };
            btnPause.Disabled = status == "Paused" || status == "Cancelled";
            btnPause.Pressed += () => { _bridge.PauseProgram(id); Refresh(); };
            row.AddChild(btnPause);

            var btnCancel = new Button { Text = "Cancel" };
            btnCancel.Disabled = status == "Cancelled";
            btnCancel.Pressed += () => { _bridge.CancelProgram(id); Refresh(); };
            row.AddChild(btnCancel);
        }

        if (!string.IsNullOrWhiteSpace(_selectedProgramId))
        {
            SelectProgram(_selectedProgramId);
        }
    }

    private void SelectProgram(string programId)
    {
        _selectedProgramId = programId;

        var quote = _bridge.GetProgramQuote(programId);
        var outcome = _bridge.GetProgramOutcome(programId);

        string fmt(Dictionary d)
        {
            var parts = d.Keys
                    .Select(k => $"{k}: {d[k]}")
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToArray();
            return string.Join("\n", parts);
        }

        string BuildWhy(Dictionary q)
        {
            if (q == null || q.Count == 0) return "(no quote)";
            bool getb(string k)
            {
                if (!q.ContainsKey(k)) return false;
                return (bool)q[k];
            }

            var fails = new System.Collections.Generic.List<string>(6);
            if (!getb("market_exists")) fails.Add("market_missing");
            if (!getb("has_enough_credits_now")) fails.Add("no_credits");
            if (!getb("has_enough_supply_now")) fails.Add("no_supply");
            if (!getb("has_enough_cargo_now")) fails.Add("no_cargo");

            var risks = new System.Collections.Generic.List<string>();
            if (q.ContainsKey("risks") && q["risks"].Obj is Godot.Collections.Array a)
            {
                foreach (var v in a)
                {
                    if (v.Obj == null) continue;
                    risks.Add(v.Obj.ToString() ?? "");
                }
            }

            risks = risks.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s, StringComparer.Ordinal).ToList();

            var parts = new System.Collections.Generic.List<string>(12);
            parts.Add(fails.Count == 0 ? "OK" : "BLOCKED: " + string.Join(", ", fails));
            if (risks.Count > 0) parts.Add("RISKS: " + string.Join(", ", risks));
            return string.Join("\n", parts);
        }

        _detail.Text =
                $"PROGRAM ID: {programId}\n\n" +
                $"WHY (derived, deterministic)\n{BuildWhy(quote)}\n\n" +
                $"QUOTE (deterministic snapshot)\n{fmt(quote)}\n\n" +
                $"LAST TICK OUTCOME (best-effort)\n{fmt(outcome)}";

    }
}
