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

        public override void _Ready()
        {
                _bridge = GetNode<SimBridge>("/root/SimBridge");
                SetupUI();
                Visible = false;
        }

        private void SetupUI()
        {
                var panel = new PanelContainer();
                panel.ZIndex = 100;
                panel.ZAsRelative = false;

                panel.SetAnchorsPreset(LayoutPreset.Center);
                panel.CustomMinimumSize = new Vector2(860, 640);
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

                var btnClose = new Button { Text = "Close" };
                btnClose.Pressed += () => EmitSignal(SignalName.RequestClose);
                topRow.AddChild(btnClose);

                root.AddChild(new HSeparator());

                var split = new HBoxContainer();
                root.AddChild(split);

                // Left: program list
                var left = new VBoxContainer();
                left.CustomMinimumSize = new Vector2(520, 0);
                split.AddChild(left);

                left.AddChild(new Label { Text = "All Programs" });

                var scroll = new ScrollContainer();
                scroll.CustomMinimumSize = new Vector2(520, 520);
                left.AddChild(scroll);

                _list = new VBoxContainer();
                scroll.AddChild(_list);

                // Right: details
                var right = new VBoxContainer();
                right.CustomMinimumSize = new Vector2(320, 0);
                split.AddChild(right);

                right.AddChild(new Label { Text = "Selected Program" });

                _detail = new Label
                {
                        Text = "(none)",
                        AutowrapMode = TextServer.AutowrapMode.WordSmart,
                        CustomMinimumSize = new Vector2(320, 520)
                };
                right.AddChild(_detail);
        }

        public void Open()
        {
                Visible = true;
                Refresh();
        }

        private void Refresh()
        {
                if (_bridge == null) return;

                foreach (var child in _list.GetChildren())
                        child.QueueFree();

                var arr = _bridge.GetProgramExplainSnapshot();

                // Sort stable by id
                var rows = arr
                        .Select(v => v.Obj as Dictionary)
                        .Where(d => d != null)
                        .OrderBy(d => d!.ContainsKey("id") ? d["id"].ToString() : "")
                        .ToArray();

                if (rows.Length == 0)
                {

                        _list.AddChild(new Label { Text = "(no programs)" });
                        _selectedProgramId = "";
                        _detail.Text = "(none)";
                        return;
                }

                foreach (var d in rows!)
                {
                        var id = d!.ContainsKey("id") ? d["id"].ToString() : "";
                        var kind = d.ContainsKey("kind") ? d["kind"].ToString() : "";
                        var status = d.ContainsKey("status") ? d["status"].ToString() : "";
                        var marketId = d.ContainsKey("market_id") ? d["market_id"].ToString() : "";
                        var goodId = d.ContainsKey("good_id") ? d["good_id"].ToString() : "";

                        var qty = d.ContainsKey("quantity") ? (int)d["quantity"] : 0;
                        var cad = d.ContainsKey("cadence_ticks") ? (int)d["cadence_ticks"] : 0;
                        var next = d.ContainsKey("next_run_tick") ? (int)d["next_run_tick"] : 0;
                        var last = d.ContainsKey("last_run_tick") ? (int)d["last_run_tick"] : 0;

                        var row = new HBoxContainer();
                        _list.AddChild(row);

                        var btnSelect = new Button { Text = "Select" };
                        btnSelect.Pressed += () => SelectProgram(id);
                        row.AddChild(btnSelect);

                        var lbl = new Label
                        {
                                Text = $"{id} | {kind} | {status} | {marketId}:{goodId} | q={qty} cad={cad}t | last={last} next={next}",
                                AutowrapMode = TextServer.AutowrapMode.Off,
                                CustomMinimumSize = new Vector2(430, 0)
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

                // Keep selection if possible
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

                _detail.Text =
                        $"PROGRAM ID: {programId}\n\n" +
                        $"QUOTE (deterministic snapshot)\n{fmt(quote)}\n\n" +
                        $"LAST TICK OUTCOME (best-effort)\n{fmt(outcome)}";
        }
}
