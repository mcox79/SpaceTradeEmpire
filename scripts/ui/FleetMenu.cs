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

    private LineEdit _overrideTarget = null!;

    private string _selectedFleetId = "";
    private Label _selectedLabel = null!;

    // GATE.S7.FLEET_TAB.ACTIONS.001: Dismiss confirmation state.
    private bool _dismissConfirmPending = false;
    private string _dismissConfirmShipId = "";

    private double _accumMs = 0.0;

    private ulong _lastRefreshMs = 0;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");

        // Refresh UI after save/load completes so visible state matches persisted state.
        _bridge.SimLoaded += OnSimLoaded;
        _bridge.SaveCompleted += OnSaveCompleted;

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

        _selectedLabel = new Label { Text = "Selected: (none)", HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(_selectedLabel);

        root.AddChild(new HSeparator());

        var topRow = new HBoxContainer();
        root.AddChild(topRow);

        var btnRefresh = new Button { Text = "Refresh" };
        btnRefresh.Pressed += Refresh;
        topRow.AddChild(btnRefresh);

        var btnSave = new Button { Text = "Save" };
        btnSave.Pressed += () =>
        {
            _bridge.RequestSave();
        };
        topRow.AddChild(btnSave);

        var btnLoad = new Button { Text = "Load" };
        btnLoad.Pressed += () =>
        {
            _bridge.RequestLoad();
        };
        topRow.AddChild(btnLoad);

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

        topRow.AddChild(new Label { Text = "OverrideTarget:" });

        _overrideTarget = new LineEdit
        {
            PlaceholderText = "node id (blank clears via Clear button)",
            CustomMinimumSize = new Vector2(260, 0)
        };
        topRow.AddChild(_overrideTarget);

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

    private void OnSimLoaded()
    {
        if (Visible) Refresh();
    }

    private void OnSaveCompleted()
    {
        if (Visible) Refresh();
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

    private static long GetLong(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return 0L;
        var v = d[key];
        try { return (long)v; } catch { return 0L; }
    }

    private static float GetFloat(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return 0f;
        var v = d[key];
        try { return (float)v; } catch { return 0f; }
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

        // Restore persisted selection from quicksave (if any).
        // Determinism: scalar string only; validation happens below against sorted rows.
        _selectedFleetId = _bridge.GetUiSelectedFleetId();

        _lastRefreshMs = Time.GetTicksMsec();
        _heartbeat.Text = $"Refreshed: {_lastRefreshMs}ms";

        if (rows.Length == 0)
        {
            _list.AddChild(new Label { Text = "(no fleets)" });
            return;
        }

        // --- FLEET ROSTER (master list panel, GATE.S7.FLEET_TAB.LIST.001) ---
        _list.AddChild(new Label
        {
            Text = "FLEET ROSTER",
            Modulate = new Color(0.7f, 1f, 0.85f),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _list.AddChild(new Label
        {
            Text = "Ship | Class | Hull | Shield | Location | Job",
            Modulate = new Color(0.6f, 0.6f, 0.8f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        _list.AddChild(new HSeparator());

        var roster = _bridge.GetFleetRosterV0();
        foreach (var rosterVar in roster)
        {
            var rd = rosterVar.Obj as Dictionary;
            if (rd == null) continue;

            var rShipId = GetStr(rd, "ship_id");
            var rShipClass = GetStr(rd, "ship_class");
            var rHullPct = GetFloat(rd, "hull_hp_pct");
            var rShieldPct = GetFloat(rd, "shield_hp_pct");
            var rLocation = GetStr(rd, "location_name");
            var rJobStatus = GetStr(rd, "job_status");

            var rosterRow = new HBoxContainer();

            // Select button for roster row (GATE.S7.FLEET_TAB.DETAIL.001).
            var capturedShipId = rShipId; // capture for closure
            var btnRosterSelect = new Button
            {
                Text = (rShipId == _selectedFleetId) ? ">>>" : "Select",
                CustomMinimumSize = new Vector2(60, 0)
            };
            btnRosterSelect.Disabled = (rShipId == _selectedFleetId);
            btnRosterSelect.Pressed += () =>
            {
                _selectedFleetId = capturedShipId;
                _bridge.SetUiSelectedFleetId(capturedShipId);
                Refresh();
            };
            rosterRow.AddChild(btnRosterSelect);

            // Ship ID + class.
            rosterRow.AddChild(new Label
            {
                Text = $"{rShipId}",
                CustomMinimumSize = new Vector2(160, 0)
            });
            rosterRow.AddChild(new Label
            {
                Text = rShipClass,
                CustomMinimumSize = new Vector2(90, 0),
                Modulate = new Color(0.85f, 0.85f, 1f)
            });

            // Hull HP bar.
            var hullBar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 1,
                Value = rHullPct,
                CustomMinimumSize = new Vector2(100, 18),
                ShowPercentage = false
            };
            var hullBarSb = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.15f) };
            hullBarSb.SetContentMarginAll(0);
            hullBar.AddThemeStyleboxOverride("background", hullBarSb);
            var hullFillSb = new StyleBoxFlat { BgColor = rHullPct > 0.5f ? new Color(0.2f, 0.8f, 0.2f) : rHullPct > 0.25f ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.9f, 0.2f, 0.2f) };
            hullFillSb.SetContentMarginAll(0);
            hullBar.AddThemeStyleboxOverride("fill", hullFillSb);

            var hullBox = new HBoxContainer();
            hullBox.AddChild(new Label { Text = "H:", CustomMinimumSize = new Vector2(20, 0) });
            hullBox.AddChild(hullBar);
            hullBox.AddChild(new Label { Text = $"{(int)(rHullPct * 100)}%", CustomMinimumSize = new Vector2(40, 0) });
            rosterRow.AddChild(hullBox);

            // Shield HP bar.
            var shieldBar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 1,
                Value = rShieldPct,
                CustomMinimumSize = new Vector2(100, 18),
                ShowPercentage = false
            };
            var shieldBarSb = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.15f) };
            shieldBarSb.SetContentMarginAll(0);
            shieldBar.AddThemeStyleboxOverride("background", shieldBarSb);
            var shieldFillSb = new StyleBoxFlat { BgColor = new Color(0.2f, 0.5f, 1f) };
            shieldFillSb.SetContentMarginAll(0);
            shieldBar.AddThemeStyleboxOverride("fill", shieldFillSb);

            var shieldBox = new HBoxContainer();
            shieldBox.AddChild(new Label { Text = "S:", CustomMinimumSize = new Vector2(20, 0) });
            shieldBox.AddChild(shieldBar);
            shieldBox.AddChild(new Label { Text = $"{(int)(rShieldPct * 100)}%", CustomMinimumSize = new Vector2(40, 0) });
            rosterRow.AddChild(shieldBox);

            // Location.
            rosterRow.AddChild(new Label
            {
                Text = rLocation,
                CustomMinimumSize = new Vector2(120, 0),
                ClipText = true
            });

            // Job status.
            rosterRow.AddChild(new Label
            {
                Text = rJobStatus,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true,
                Modulate = new Color(0.9f, 0.9f, 0.7f)
            });

            _list.AddChild(rosterRow);
        }

        _list.AddChild(new HSeparator());

        // --- SHIP DETAIL PANEL (GATE.S7.FLEET_TAB.DETAIL.001) ---
        // Shows detailed info for the selected ship when a roster row is clicked.
        if (!string.IsNullOrWhiteSpace(_selectedFleetId))
        {
            var detail = _bridge.GetFleetShipDetailV0(_selectedFleetId);
            if (detail != null && detail.Count > 0)
            {
                var detailPanel = new VBoxContainer();

                // Header
                detailPanel.AddChild(new Label
                {
                    Text = $"SHIP DETAIL: {GetStr(detail, "ship_id")}",
                    Modulate = new Color(0.85f, 0.95f, 1f),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                var detailClass = GetStr(detail, "ship_class");
                detailPanel.AddChild(new Label
                {
                    Text = $"Class: {detailClass}",
                    Modulate = new Color(0.85f, 0.85f, 1f)
                });

                // Hull HP bar with numeric values
                int hullHp = GetInt(detail, "hull_hp");
                int hullHpMax = GetInt(detail, "hull_hp_max");
                float detailHullPct = (hullHpMax > 0) ? Math.Clamp((float)hullHp / hullHpMax, 0f, 1f) : 1f;

                var detailHullRow = new HBoxContainer();
                detailHullRow.AddChild(new Label { Text = "Hull:", CustomMinimumSize = new Vector2(50, 0) });
                var detailHullBar = new ProgressBar
                {
                    MinValue = 0, MaxValue = 1, Value = detailHullPct,
                    CustomMinimumSize = new Vector2(200, 20), ShowPercentage = false
                };
                var detailHullBg = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.15f) };
                detailHullBg.SetContentMarginAll(0);
                detailHullBar.AddThemeStyleboxOverride("background", detailHullBg);
                var detailHullFill = new StyleBoxFlat
                {
                    BgColor = detailHullPct > 0.5f ? new Color(0.2f, 0.8f, 0.2f)
                        : detailHullPct > 0.25f ? new Color(0.9f, 0.7f, 0.1f)
                        : new Color(0.9f, 0.2f, 0.2f)
                };
                detailHullFill.SetContentMarginAll(0);
                detailHullBar.AddThemeStyleboxOverride("fill", detailHullFill);
                detailHullRow.AddChild(detailHullBar);
                detailHullRow.AddChild(new Label { Text = $"  {hullHp} / {hullHpMax}" });
                detailPanel.AddChild(detailHullRow);

                // Shield HP bar with numeric values
                int shieldHp = GetInt(detail, "shield_hp");
                int shieldHpMax = GetInt(detail, "shield_hp_max");
                float detailShieldPct = (shieldHpMax > 0) ? Math.Clamp((float)shieldHp / shieldHpMax, 0f, 1f) : 1f;

                var detailShieldRow = new HBoxContainer();
                detailShieldRow.AddChild(new Label { Text = "Shield:", CustomMinimumSize = new Vector2(50, 0) });
                var detailShieldBar = new ProgressBar
                {
                    MinValue = 0, MaxValue = 1, Value = detailShieldPct,
                    CustomMinimumSize = new Vector2(200, 20), ShowPercentage = false
                };
                var detailShieldBg = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.15f) };
                detailShieldBg.SetContentMarginAll(0);
                detailShieldBar.AddThemeStyleboxOverride("background", detailShieldBg);
                var detailShieldFill = new StyleBoxFlat { BgColor = new Color(0.2f, 0.5f, 1f) };
                detailShieldFill.SetContentMarginAll(0);
                detailShieldBar.AddThemeStyleboxOverride("fill", detailShieldFill);
                detailShieldRow.AddChild(detailShieldBar);
                detailShieldRow.AddChild(new Label { Text = $"  {shieldHp} / {shieldHpMax}" });
                detailPanel.AddChild(detailShieldRow);

                // Speed
                var detailSpeed = GetFloat(detail, "speed");
                detailPanel.AddChild(new Label { Text = $"Speed: {detailSpeed:F2} AU/tick" });

                // Location
                var detailLocation = GetStr(detail, "location_name");
                detailPanel.AddChild(new Label
                {
                    Text = $"Location: {detailLocation}",
                    Modulate = new Color(0.9f, 0.9f, 0.7f)
                });

                // Job status
                var detailJob = GetStr(detail, "job_status");
                detailPanel.AddChild(new Label
                {
                    Text = $"Job: {detailJob}",
                    Modulate = new Color(0.9f, 0.85f, 0.7f)
                });

                // Module loadout list
                detailPanel.AddChild(new HSeparator());
                detailPanel.AddChild(new Label
                {
                    Text = "Module Loadout",
                    Modulate = new Color(0.7f, 1f, 0.85f)
                });

                if (detail.ContainsKey("modules"))
                {
                    var modules = detail["modules"].AsGodotArray();
                    if (modules != null && modules.Count > 0)
                    {
                        foreach (var modVar in modules)
                        {
                            var md = modVar.Obj as Dictionary;
                            if (md == null) continue;

                            var modSlotId = GetStr(md, "slot_id");
                            var modModuleId = GetStr(md, "module_id");
                            var modDisplayName = GetStr(md, "display_name");

                            string modText;
                            if (string.IsNullOrEmpty(modModuleId))
                            {
                                modText = $"  [{modSlotId}] (empty)";
                            }
                            else
                            {
                                var nameLabel = string.IsNullOrEmpty(modDisplayName) ? modModuleId : modDisplayName;
                                modText = $"  [{modSlotId}] {nameLabel}";
                            }

                            detailPanel.AddChild(new Label
                            {
                                Text = modText,
                                Modulate = string.IsNullOrEmpty(modModuleId)
                                    ? new Color(0.5f, 0.5f, 0.5f)
                                    : new Color(0.85f, 0.95f, 0.85f)
                            });
                        }
                    }
                    else
                    {
                        detailPanel.AddChild(new Label { Text = "  (no modules)", Modulate = new Color(0.5f, 0.5f, 0.5f) });
                    }
                }
                else
                {
                    detailPanel.AddChild(new Label { Text = "  (no module data)", Modulate = new Color(0.5f, 0.5f, 0.5f) });
                }

                // --- CARGO DISPLAY (GATE.S7.FLEET_TAB.CARGO.001) ---
                detailPanel.AddChild(new HSeparator());
                detailPanel.AddChild(new Label
                {
                    Text = "Cargo",
                    Modulate = new Color(0.7f, 1f, 0.85f)
                });

                // Cargo data comes from the explain snapshot for this fleet (rows array).
                // Find the matching row to extract structured cargo info.
                var matchedRow = rows.FirstOrDefault(r => GetStr(r!, "id") == _selectedFleetId);
                if (matchedRow != null)
                {
                    var cargoSummary = GetStr(matchedRow!, "cargo_summary");
                    if (string.IsNullOrWhiteSpace(cargoSummary) || cargoSummary == "(empty)")
                    {
                        detailPanel.AddChild(new Label
                        {
                            Text = "  Cargo: Empty",
                            Modulate = new Color(0.5f, 0.5f, 0.5f)
                        });
                    }
                    else
                    {
                        // cargo_summary format: "good_id:qty, good_id:qty"
                        var cargoEntries = cargoSummary.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var entry in cargoEntries)
                        {
                            var parts = entry.Split(':', 2);
                            string goodId = parts.Length > 0 ? parts[0].Trim() : "";
                            string qtyStr = parts.Length > 1 ? parts[1].Trim() : "0";

                            // Format good name: capitalize first letter for display.
                            string displayName = string.IsNullOrEmpty(goodId)
                                ? "Unknown"
                                : char.ToUpperInvariant(goodId[0]) + goodId.Substring(1).Replace('_', ' ');

                            detailPanel.AddChild(new Label
                            {
                                Text = $"  {displayName}: {qtyStr}",
                                Modulate = new Color(0.9f, 0.9f, 0.8f)
                            });
                        }
                    }
                }
                else
                {
                    detailPanel.AddChild(new Label
                    {
                        Text = "  Cargo: Empty",
                        Modulate = new Color(0.5f, 0.5f, 0.5f)
                    });
                }

                // --- PROGRAM ASSIGNMENT VIEW (GATE.S7.FLEET_TAB.PROGRAM.001) ---
                detailPanel.AddChild(new HSeparator());
                detailPanel.AddChild(new Label
                {
                    Text = "Program",
                    Modulate = new Color(0.7f, 1f, 0.85f)
                });

                // Program ID comes from the explain snapshot for this fleet.
                string programId = "";
                if (matchedRow != null)
                {
                    programId = GetStr(matchedRow!, "program_id");
                }

                if (string.IsNullOrWhiteSpace(programId))
                {
                    detailPanel.AddChild(new Label
                    {
                        Text = "  Program: None",
                        Modulate = new Color(0.5f, 0.5f, 0.5f)
                    });
                }
                else
                {
                    detailPanel.AddChild(new Label
                    {
                        Text = $"  Program: {programId}",
                        Modulate = new Color(0.85f, 0.95f, 0.85f)
                    });
                }

                var programActionRow = new HBoxContainer();

                var btnAssignProgram = new Button
                {
                    Text = "Assign Program (Coming Soon)",
                    CustomMinimumSize = new Vector2(220, 30),
                    Disabled = true
                };
                programActionRow.AddChild(btnAssignProgram);

                detailPanel.AddChild(programActionRow);

                // --- FLEET ACTION BUTTONS (GATE.S7.FLEET_TAB.ACTIONS.001) ---
                detailPanel.AddChild(new HSeparator());
                detailPanel.AddChild(new Label
                {
                    Text = "Actions",
                    Modulate = new Color(0.7f, 1f, 0.85f)
                });

                var actionRow = new HBoxContainer();
                var capturedActionShipId = _selectedFleetId; // capture for closures

                // Recall button: sends the ship back to the player's current node.
                var btnRecall = new Button { Text = "Recall", CustomMinimumSize = new Vector2(80, 30) };
                btnRecall.Pressed += () =>
                {
                    _bridge.FleetRecallV0(capturedActionShipId);
                    _dismissConfirmPending = false;
                    _dismissConfirmShipId = "";
                    Refresh();
                };
                actionRow.AddChild(btnRecall);

                // Dismiss button: two-click confirmation. First click changes to "Confirm Dismiss?",
                // second click actually dismisses. Cannot dismiss the player's own ship.
                bool isPlayerShip = string.Equals(capturedActionShipId, "fleet_trader_1", StringComparison.Ordinal);

                var btnDismiss = new Button { CustomMinimumSize = new Vector2(130, 30) };
                if (isPlayerShip)
                {
                    btnDismiss.Text = "Dismiss (Your Ship)";
                    btnDismiss.Disabled = true;
                }
                else if (_dismissConfirmPending && string.Equals(_dismissConfirmShipId, capturedActionShipId, StringComparison.Ordinal))
                {
                    btnDismiss.Text = "Confirm Dismiss?";
                    btnDismiss.Modulate = new Color(1f, 0.4f, 0.4f);
                    btnDismiss.Pressed += () =>
                    {
                        _bridge.FleetDismissV0(capturedActionShipId);
                        _dismissConfirmPending = false;
                        _dismissConfirmShipId = "";
                        _selectedFleetId = "";
                        _bridge.SetUiSelectedFleetId("");
                        Refresh();
                    };
                }
                else
                {
                    btnDismiss.Text = "Dismiss";
                    btnDismiss.Pressed += () =>
                    {
                        _dismissConfirmPending = true;
                        _dismissConfirmShipId = capturedActionShipId;
                        Refresh();
                    };
                }
                actionRow.AddChild(btnDismiss);

                // Rename button: placeholder, disabled.
                var btnRename = new Button
                {
                    Text = "Rename (Coming Soon)",
                    CustomMinimumSize = new Vector2(160, 30),
                    Disabled = true
                };
                actionRow.AddChild(btnRename);

                detailPanel.AddChild(actionRow);

                _list.AddChild(detailPanel);
            }
        }

        _list.AddChild(new HSeparator());

        // --- DETAILED FLEET VIEW (existing) ---
        _list.AddChild(new Label
        {
            Text = "FLEET DETAIL",
            Modulate = new Color(0.7f, 0.7f, 1f),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _list.AddChild(new Label
        {
            Text = "ID | Role | Node | State | Ctrl | Override | Task | JobPhase | JobGood | Remaining | Cargo | Route | Actions",
            Modulate = new Color(0.6f, 0.6f, 0.8f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _list.AddChild(new HSeparator());

        // Keep selection stable if possible; if selection disappears, clear it.
        if (!string.IsNullOrWhiteSpace(_selectedFleetId) && !rows.Any(r => GetStr(r!, "id") == _selectedFleetId))
        {
            _selectedFleetId = "";
            _bridge.SetUiSelectedFleetId("");
        }
        if (_selectedLabel != null)
        {
            _selectedLabel.Text = string.IsNullOrWhiteSpace(_selectedFleetId) ? "Selected: (none)" : $"Selected: {_selectedFleetId}";
        }

        foreach (var d in rows)
        {
            var id = GetStr(d!, "id");
            var node = GetStr(d!, "current_node_id");
            var state = GetStr(d!, "state");
            var task = GetStr(d!, "task");

            var role = GetStr(d!, "role");

            var ctrl = GetStr(d!, "active_controller");
            var overrideNode = GetStr(d!, "manual_override_node_id");

            var jobPhase = GetStr(d!, "job_phase");
            var jobGood = GetStr(d!, "job_good_id");
            var remaining = GetInt(d!, "job_remaining");

            var cargo = GetStr(d!, "cargo_summary");
            var route = GetStr(d!, "route_progress");

            var row = new HBoxContainer();

            var btnSelect = new Button { Text = (id == _selectedFleetId) ? "Selected" : "Select" };
            btnSelect.Disabled = (id == _selectedFleetId);
            btnSelect.Pressed += () =>
            {
                _selectedFleetId = id;
                _bridge.SetUiSelectedFleetId(id);
                Refresh();
            };
            row.AddChild(btnSelect);

            var label = new Label
            {
                Text = $"{id} | {role} | {node} | {state} | {ctrl} | {overrideNode} | {task} | {jobPhase} | {jobGood} | {remaining} | {cargo} | {route}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(820, 0)
            };
            row.AddChild(label);

            var btnCancel = new Button { Text = "CancelJob" };
            btnCancel.Disabled = string.IsNullOrWhiteSpace(jobPhase);
            btnCancel.Pressed += () =>
            {
                _bridge.CancelFleetJob(id, "ui_cancel");
                Refresh();
            };
            row.AddChild(btnCancel);

            var btnOverride = new Button { Text = "Override" };
            btnOverride.Pressed += () =>
            {
                var target = (_overrideTarget != null) ? _overrideTarget.Text : "";
                _bridge.SetFleetDestination(id, target ?? "", "ui_override");
                Refresh();
            };
            row.AddChild(btnOverride);

            var btnClear = new Button { Text = "ClearOverride" };
            btnClear.Pressed += () =>
            {
                _bridge.SetFleetDestination(id, "", "ui_clear_override");
                Refresh();
            };
            row.AddChild(btnClear);

            _list.AddChild(row);

            // Event timeline: render only for the selected fleet to make selection explicit and usable.
            if (!string.IsNullOrWhiteSpace(_selectedFleetId) && id == _selectedFleetId)
            {
                // Last N schema-bound logistics events for this fleet (newest-first, deterministic order).
                var eventsArr = _bridge.GetFleetEventLogSnapshot(id, 25);

                if (eventsArr != null && eventsArr.Count > 0)
                {
                    var evBox = new VBoxContainer();
                    evBox.AddChild(new Label
                    {
                        Text = "Events:",
                        Modulate = new Color(0.8f, 0.85f, 1f),
                        AutowrapMode = TextServer.AutowrapMode.WordSmart
                    });

                    foreach (var ev in eventsArr)
                    {
                        var ed = ev.Obj as Dictionary;
                        if (ed == null) continue;

                        var seq = GetLong(ed, "seq");
                        var tick = GetLong(ed, "tick");
                        var type = GetInt(ed, "type");
                        var note = GetStr(ed, "note");

                        var srcNode = GetStr(ed, "source_node_id");
                        var dstNode = GetStr(ed, "target_node_id");
                        var srcMkt = GetStr(ed, "source_market_id");
                        var dstMkt = GetStr(ed, "target_market_id");
                        var good = GetStr(ed, "good_id");
                        var amt = GetInt(ed, "amount");

                        var line = $"seq={seq} tick={tick} type={type}";
                        if (!string.IsNullOrWhiteSpace(srcNode) || !string.IsNullOrWhiteSpace(dstNode))
                            line += $" nodes={srcNode}->{dstNode}";
                        if (!string.IsNullOrWhiteSpace(srcMkt) || !string.IsNullOrWhiteSpace(dstMkt))
                            line += $" mkts={srcMkt}->{dstMkt}";
                        if (!string.IsNullOrWhiteSpace(good) || amt != 0)
                            line += $" good={good} amt={amt}";
                        if (!string.IsNullOrWhiteSpace(note))
                            line += $" note={note}";

                        evBox.AddChild(new Label
                        {
                            Text = line,
                            AutowrapMode = TextServer.AutowrapMode.WordSmart
                        });
                    }

                    _list.AddChild(evBox);
                }

                _list.AddChild(new HSeparator());
            }
        }

        // Hero ship loadout section.
        _list.AddChild(new HSeparator());
        _list.AddChild(new Label { Text = "HERO SHIP LOADOUT", Modulate = new Color(0.7f, 1f, 0.7f) });

        var loadout = _bridge.GetHeroShipLoadoutV0();
        if (loadout.Count == 0)
        {
            _list.AddChild(new Label { Text = "  (no slots)" });
        }
        else
        {
            foreach (var slotVar in loadout)
            {
                var sd = slotVar.Obj as Godot.Collections.Dictionary;
                if (sd == null) continue;
                var slotId = GetStr(sd, "slot_id");
                var installed = GetStr(sd, "installed_module_id");
                _list.AddChild(new Label
                {
                    Text = $"  {slotId}: {(string.IsNullOrEmpty(installed) ? "empty" : installed)}"
                });
            }
        }
    }

    public int GetHeroLoadoutSlotCountV0()
    {
        if (_bridge == null) return 0;
        return _bridge.GetHeroShipLoadoutV0().Count;
    }
}
