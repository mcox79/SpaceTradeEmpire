// GATE.S10.EMPIRE.SHELL.001, GATE.S10.EMPIRE.OVERVIEW_TAB.001,
// GATE.S10.EMPIRE.PRODUCTION_TAB.001, GATE.S10.EMPIRE.INTEL_TAB.001,
// GATE.S5.ESCORT_PROG.UI.001
// GATE.S7.FACTION.UI_REPUTATION.001
using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace SpaceTradeEmpire.Ui;

public partial class EmpireDashboard : Control
{
    // ── Bridge ──────────────────────────────────────────────────────────────
    private Bridge.SimBridge _bridge = null!;

    // ── Tab state ───────────────────────────────────────────────────────────
    // GATE.T58.UI.DASHBOARD_OVERHAUL.001: Internal 9-tab enum preserved for panel logic.
    // UI shows 5 consolidated tabs: Overview | Routes | Operations | Intel | Empire.
    // "Trade" → "Routes" terminology throughout player-facing strings.
    private enum Tab { Overview, Trade, Production, Programs, Intel, Research, Stats, Factions, Warfronts }
    private Tab _activeTab = Tab.Overview;
    // GATE.T58.UI.DASHBOARD_OVERHAUL.001: Consolidated display tab names (5 visible).
    private static readonly string[] ConsolidatedTabNames = { "Overview", "Routes", "Operations", "Intel", "Empire" };
    // Map consolidated button index → default internal tab shown.
    private static readonly Tab[] ConsolidatedDefaultTab = { Tab.Overview, Tab.Trade, Tab.Production, Tab.Intel, Tab.Stats };

    // ── Tab buttons ─────────────────────────────────────────────────────────
    private Button[] _tabBtns = System.Array.Empty<Button>();

    // ── Content panels (one per tab) ────────────────────────────────────────
    private Control[] _tabPanels = System.Array.Empty<Control>();

    // ── Overview labels ─────────────────────────────────────────────────────
    private Label _ovCredits = null!;
    private Label _ovFleets = null!;
    private Label _ovPrograms = null!;
    private Label _ovResearch = null!;
    private Label _ovMissions = null!;
    private Label _ovIndustry = null!;
    // GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: Enriched overview (U6)
    private Label _ovRecentActivity = null!;
    private Label _ovSystemInfo = null!;
    private Label _ovTradeStats = null!;
    // Spark-chart for credit trend in Economy card
    private HBoxContainer _sparkChart = null!;
    private Label _sparkChartSummary = null!;

    // ── Trade list ──────────────────────────────────────────────────────────
    private VBoxContainer _tradeList = null!;
    // GATE.S18.EMPIRE_DASH.ECONOMY_TAB.001: Economy overview (inventory across stations)
    private VBoxContainer _econList = null!;

    // ── Production list ─────────────────────────────────────────────────────
    private VBoxContainer _prodList = null!;

    // ── Programs list ───────────────────────────────────────────────────────
    private VBoxContainer _progList = null!;

    // ── Programs creation form ───────────────────────────────────────────────
    private OptionButton _progKindDropdown = null!;
    private LineEdit _progField1 = null!; // fleetId / marketId / sourceMarketId
    private LineEdit _progField2 = null!; // originNodeId / goodId / destMarketId
    private LineEdit _progField3 = null!; // destNodeId / qty / buyGoodId
    private LineEdit _progField4 = null!; // cadence / cadence / sellGoodId
    private LineEdit _progField5 = null!; // (blank) / (blank) / cadence
    private Label _progField1Label = null!;
    private Label _progField2Label = null!;
    private Label _progField3Label = null!;
    private Label _progField4Label = null!;
    private Label _progField5Label = null!;
    private Label _progCreateStatus = null!;

    // ── Intel list ──────────────────────────────────────────────────────────
    private VBoxContainer _intelList = null!;

    // ── Research list ─────────────────────────────────────────────────────
    private VBoxContainer _researchList = null!;

    // ── Stats / milestones list (GATE.S12.PROGRESSION.DASHBOARD.001) ────
    private VBoxContainer _statsList = null!;
    private VBoxContainer _milestonesList = null!;

    // ── Factions list (GATE.S7.FACTION.UI_REPUTATION.001) ─────────────
    private VBoxContainer _factionList = null!;

    // ── Warfronts list (GATE.S7.WARFRONT.DASHBOARD_TAB.001) ────────
    private VBoxContainer _warfrontList = null!;

    // ── Godot lifecycle ─────────────────────────────────────────────────────
    public override void _Ready()
    {
        _bridge = GetNodeOrNull<Bridge.SimBridge>("/root/SimBridge")!;
        BuildUI();
        Visible = false;
        VisibilityChanged += OnVisibilityChanged;
    }

    private void OnVisibilityChanged()
    {
        if (Visible)
        {
            RefreshTabVisibility();
            RefreshCurrentTab();
        }
    }

    // GATE.S13.EMPIRE.GATING.001: Progressive tab visibility
    private void RefreshTabVisibility()
    {
        if (_bridge == null) return;

        // Overview + Research + Stats: always visible
        _tabBtns[(int)Tab.Overview].Visible = true;
        _tabBtns[(int)Tab.Research].Visible = true;
        _tabBtns[(int)Tab.Stats].Visible = true;

        // Trade: visible if any trade routes exist
        bool showTrade = false;
        var routes = _bridge.GetTradeRoutesV0();
        if (routes != null && routes.Count > 0) showTrade = true;
        _tabBtns[(int)Tab.Trade].Visible = showTrade;

        // Production: visible after visiting 3+ nodes
        bool showProd = false;
        var summary = _bridge.GetEmpireSummaryV0();
        if (summary != null)
        {
            int nodesVisited = GetInt(summary, "nodes_visited");
            if (nodesVisited >= 3) showProd = true;
        }
        _tabBtns[(int)Tab.Production].Visible = showProd;

        // Automation (Programs): visible if any programs exist
        bool showProg = false;
        var progs = _bridge.GetProgramExplainSnapshot();
        if (progs != null && progs.Count > 0) showProg = true;
        _tabBtns[(int)Tab.Programs].Visible = showProg;

        // Exploration (Intel): visible if any intel observations exist
        bool showIntel = false;
        var intel = _bridge.GetIntelFreshnessByNodeV0();
        if (intel != null && intel.Count > 0) showIntel = true;
        _tabBtns[(int)Tab.Intel].Visible = showIntel;

        // Factions: visible if any factions exist (GATE.S7.FACTION.UI_REPUTATION.001)
        bool showFactions = false;
        var factions = _bridge.GetAllFactionsV0();
        if (factions != null && factions.Count > 0) showFactions = true;
        _tabBtns[(int)Tab.Factions].Visible = showFactions;

        // GATE.S7.WARFRONT.DASHBOARD_TAB.001: Warfronts tab visible if any warfronts exist
        bool showWarfronts = false;
        var warfronts = _bridge.GetWarfrontsV0();
        if (warfronts != null && warfronts.Count > 0) showWarfronts = true;
        _tabBtns[(int)Tab.Warfronts].Visible = showWarfronts;

        // If current tab is hidden, switch to Overview
        if (!_tabBtns[(int)_activeTab].Visible)
            SwitchTab(Tab.Overview);
    }

    // ── Public API ──────────────────────────────────────────────────────────
    /// <summary>Toggle visibility; refreshes data when opening.</summary>
    public void ToggleVisibility()
    {
        if (Visible)
        {
            Hide();
        }
        else
        {
            Show();
            RefreshCurrentTab();
        }
    }

    // ── Build UI ─────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        // Full-screen modal — blocks input behind it
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = 0; OffsetTop = 0; OffsetRight = 0; OffsetBottom = 0;
        ZIndex = 300;
        ZAsRelative = false;
        MouseFilter = MouseFilterEnum.Stop;
        Name = "EmpireDashboard";

        // Dimmer
        var dim = new ColorRect();
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        // FEEL_POST_FIX_8: Increased from 0.55 to 0.85 to fully obscure 3D world behind panel.
        dim.Color = new Color(0f, 0f, 0f, 0.85f);
        dim.MouseFilter = MouseFilterEnum.Stop;
        dim.ZIndex = 0;
        dim.ZAsRelative = false;
        AddChild(dim);

        // Outer panel — leaves 60 px margin on each side
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.OffsetLeft   =  60;
        panel.OffsetTop    =  60;
        panel.OffsetRight  = -60;
        panel.OffsetBottom = -60;
        panel.ZIndex = 1;
        panel.ZAsRelative = false;
        panel.MouseFilter = MouseFilterEnum.Stop;

        // L1.1: Ship computer panel style.
        var sb = new StyleBoxFlat();
        sb.BgColor = new Color(0.05f, 0.07f, 0.12f, 0.94f);
        sb.BorderColor = new Color(0.3f, 0.6f, 1.0f, 0.7f);
        sb.SetBorderWidthAll(1);
        sb.BorderWidthLeft = 2; // Thicker left edge — ship computer readout feel
        sb.SetCornerRadiusAll(2); // Sharp corners — military precision
        sb.ContentMarginLeft = 12;
        sb.ContentMarginRight = 12;
        sb.ContentMarginTop = 10;
        sb.ContentMarginBottom = 10;
        sb.ShadowColor = new Color(0.0f, 0.1f, 0.2f, 0.3f);
        sb.ShadowSize = 4;
        sb.ShadowOffset = new Vector2(0, 2);
        panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(panel);

        // Root VBox inside panel
        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.SizeFlagsVertical   = SizeFlags.ExpandFill;
        panel.AddChild(root);

        // ── Title row ──────────────────────────────────────────────────────
        var titleRow = new HBoxContainer();
        titleRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.AddChild(titleRow);

        var title = new Label { Text = "EMPIRE DASHBOARD" };
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1.0f, 1.0f));
        titleRow.AddChild(title);

        var btnClose = new Button { Text = "X" };
        btnClose.TooltipText = "Close [E]";
        btnClose.Pressed += () => Hide();
        titleRow.AddChild(btnClose);

        root.AddChild(new HSeparator());

        // ── Tab bar ────────────────────────────────────────────────────────
        var tabBar = new HBoxContainer();
        tabBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.AddChild(tabBar);

        // GATE.S13.TERMINOLOGY.001: Player-friendly tab names
        // GATE.T58.UI.DASHBOARD_OVERHAUL.001: "Route" terminology + consolidated naming.
        var tabNames = new[] { "Overview", "Routes", "Production", "Operations", "Intel", "Research", "Empire", "Factions", "Warfronts" };
        _tabBtns = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            var idx = i; // capture
            var btn = new Button { Text = tabNames[i], ToggleMode = true };
            btn.Pressed += () => SwitchTab((Tab)idx);
            tabBar.AddChild(btn);
            _tabBtns[i] = btn;
        }
        _tabBtns[0].ButtonPressed = true;

        root.AddChild(new HSeparator());

        // ── Content area ────────────────────────────────────────────────────
        var contentScroll = new ScrollContainer();
        contentScroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        contentScroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        root.AddChild(contentScroll);

        var contentHost = new VBoxContainer();
        contentHost.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        contentHost.SizeFlagsVertical   = SizeFlags.ExpandFill;
        contentScroll.AddChild(contentHost);

        _tabPanels = new Control[tabNames.Length];
        _tabPanels[(int)Tab.Overview]    = BuildOverviewTab();
        _tabPanels[(int)Tab.Trade]       = BuildTradeTab();
        _tabPanels[(int)Tab.Production]  = BuildProductionTab();
        _tabPanels[(int)Tab.Programs]    = BuildProgramsTab();
        _tabPanels[(int)Tab.Intel]       = BuildIntelTab();
        _tabPanels[(int)Tab.Research]    = BuildResearchTab();
        _tabPanels[(int)Tab.Stats]       = BuildStatsTab();
        _tabPanels[(int)Tab.Factions]    = BuildFactionsTab();
        _tabPanels[(int)Tab.Warfronts]   = BuildWarfrontsTab();

        foreach (var p in _tabPanels)
        {
            p.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            p.SizeFlagsVertical   = SizeFlags.ExpandFill;
            contentHost.AddChild(p);
        }

        // Footer dismiss hint
        var footerHint = new Label { Text = "Press E to close" };
        footerHint.HorizontalAlignment = HorizontalAlignment.Center;
        footerHint.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.65f, 1f));
        footerHint.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(footerHint);

        // Start on Overview
        SwitchTab(Tab.Overview);
    }

    // ── Tab switching ────────────────────────────────────────────────────────
    private void SwitchTab(Tab tab)
    {
        _activeTab = tab;
        for (int i = 0; i < _tabPanels.Length; i++)
        {
            _tabPanels[i].Visible = (i == (int)tab);
            _tabBtns[i].ButtonPressed = (i == (int)tab);
        }
        RefreshCurrentTab();
    }

    private void RefreshCurrentTab()
    {
        if (_bridge == null) return;
        switch (_activeTab)
        {
            case Tab.Overview:   RefreshOverview();    break;
            case Tab.Trade:      RefreshTrade();       break;
            case Tab.Production: RefreshProduction();  break;
            case Tab.Programs:   RefreshPrograms();    break;
            case Tab.Intel:      RefreshIntel();       break;
            case Tab.Research:   RefreshResearch();    break;
            case Tab.Stats:      RefreshStats();       break;
            case Tab.Factions:   RefreshFactions();    break;
            case Tab.Warfronts:  RefreshWarfronts();   break;
        }
    }

    // ── Handle Escape / E key while open ─────────────────────────────────────
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        if (@event is InputEventKey k && k.Pressed && !k.Echo)
        {
            if (k.Keycode == Key.Escape || k.Keycode == Key.E)
            {
                Hide();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OVERVIEW TAB
    // ═══════════════════════════════════════════════════════════════════════
    // GATE.S18.EMPIRE_DASH.OVERVIEW_TAB.001: Needs Attention queue label
    private VBoxContainer _ovAttentionList = null!;

    private Control BuildOverviewTab()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);

        // GATE.S18.EMPIRE_DASH.OVERVIEW_TAB.001: 2x3 card grid layout
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 8);
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        Label Card(string title, Color accent)
        {
            var card = new VBoxContainer();
            card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var cardBg = new PanelContainer();
            cardBg.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var sb = new StyleBoxFlat();
            sb.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.9f);
            sb.BorderColor = accent;
            sb.SetBorderWidthAll(1);
            sb.SetCornerRadiusAll(4);
            sb.SetContentMarginAll(8);
            cardBg.AddThemeStyleboxOverride("panel", sb);
            var inner = new VBoxContainer();
            inner.AddThemeConstantOverride("separation", 2);
            var hdr = new Label { Text = title };
            hdr.AddThemeColorOverride("font_color", accent);
            hdr.AddThemeFontSizeOverride("font_size", 13);
            inner.AddChild(hdr);
            var val = new Label { Text = "—" };
            val.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            inner.AddChild(val);
            cardBg.AddChild(inner);
            grid.AddChild(cardBg);
            return val;
        }

        _ovCredits  = Card("Economy",     new Color(0.4f, 0.8f, 0.4f));
        // Spark-chart: mini bar chart inside Economy card
        var sparkOuter = new VBoxContainer();
        sparkOuter.AddThemeConstantOverride("separation", 2);
        var sparkLabel = new Label { Text = "Credit Flow" };
        sparkLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 0.5f, 0.7f));
        sparkLabel.AddThemeFontSizeOverride("font_size", 10);
        sparkOuter.AddChild(sparkLabel);
        _sparkChart = new HBoxContainer();
        _sparkChart.AddThemeConstantOverride("separation", 2);
        _sparkChart.CustomMinimumSize = new Vector2(0, 36);
        sparkOuter.AddChild(_sparkChart);
        _sparkChartSummary = new Label { Text = "" };
        _sparkChartSummary.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 0.6f, 0.8f));
        _sparkChartSummary.AddThemeFontSizeOverride("font_size", 10);
        sparkOuter.AddChild(_sparkChartSummary);
        _ovCredits.GetParent().AddChild(sparkOuter);
        _ovFleets   = Card("Fleet",       new Color(0.4f, 0.6f, 0.9f));
        _ovIndustry = Card("Industry",    new Color(0.9f, 0.7f, 0.3f));
        _ovResearch = Card("Research",    new Color(0.7f, 0.4f, 0.9f));
        _ovMissions = Card("Exploration", new Color(0.3f, 0.8f, 0.8f));
        _ovPrograms = Card("Automation",  new Color(0.8f, 0.5f, 0.3f));

        box.AddChild(grid);

        // Thin rule separating cards from detail sections
        var rule1 = new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = new Color(0.3f, 0.6f, 0.8f, 0.25f) };
        rule1.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rule1.MouseFilter = MouseFilterEnum.Ignore;
        box.AddChild(rule1);

        // GATE.S18.EMPIRE_DASH.OVERVIEW_TAB.001: Needs Attention queue
        // GATE.X.UI_POLISH.DASHBOARD_UX.001: Renamed to "Opportunities" with info-blue tone.
        var attHdr = new Label { Text = "Opportunities" };
        attHdr.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 1.0f));
        attHdr.AddThemeFontSizeOverride("font_size", 14);
        box.AddChild(attHdr);

        _ovAttentionList = new VBoxContainer();
        _ovAttentionList.AddThemeConstantOverride("separation", 4);
        _ovAttentionList.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(_ovAttentionList);

        // Thin rule before system section
        var rule2 = new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = new Color(0.3f, 0.6f, 0.8f, 0.25f) };
        rule2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rule2.MouseFilter = MouseFilterEnum.Ignore;
        box.AddChild(rule2);

        // GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: System & recent activity sections (U6)
        // Two-column layout for system info and activity side by side
        var bottomGrid = new GridContainer { Columns = 2 };
        bottomGrid.AddThemeConstantOverride("h_separation", 24);
        bottomGrid.AddThemeConstantOverride("v_separation", 4);
        bottomGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var sysCol = new VBoxContainer();
        sysCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        sysCol.AddThemeConstantOverride("separation", 2);
        var sysHdr = new Label { Text = "Current System" };
        sysHdr.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 0.9f));
        sysHdr.AddThemeFontSizeOverride("font_size", 14);
        sysCol.AddChild(sysHdr);
        _ovSystemInfo = new Label { Text = "—" };
        _ovSystemInfo.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _ovSystemInfo.AddThemeFontSizeOverride("font_size", 12);
        _ovSystemInfo.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        sysCol.AddChild(_ovSystemInfo);
        bottomGrid.AddChild(sysCol);

        var actCol = new VBoxContainer();
        actCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actCol.AddThemeConstantOverride("separation", 2);
        var actHdr = new Label { Text = "Recent Fleet Activity" };
        actHdr.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 0.5f));
        actHdr.AddThemeFontSizeOverride("font_size", 14);
        actCol.AddChild(actHdr);
        _ovRecentActivity = new Label { Text = "—" };
        _ovRecentActivity.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _ovRecentActivity.AddThemeFontSizeOverride("font_size", 12);
        _ovRecentActivity.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        actCol.AddChild(_ovRecentActivity);
        bottomGrid.AddChild(actCol);

        box.AddChild(bottomGrid);

        // Trade Performance footer — fills remaining space with lifetime stats
        var rule3 = new ColorRect { CustomMinimumSize = new Vector2(0, 1), Color = new Color(0.3f, 0.6f, 0.8f, 0.25f) };
        rule3.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rule3.MouseFilter = MouseFilterEnum.Ignore;
        box.AddChild(rule3);

        var tradeHdr = new Label { Text = "Trade Performance" };
        tradeHdr.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.3f));
        tradeHdr.AddThemeFontSizeOverride("font_size", 14);
        box.AddChild(tradeHdr);

        _ovTradeStats = new Label { Text = "—" };
        _ovTradeStats.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _ovTradeStats.AddThemeFontSizeOverride("font_size", 12);
        _ovTradeStats.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _ovTradeStats.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(_ovTradeStats);

        return box;
    }

    // GATE.S13.EMPIRE.OVERVIEW.001: Contextual, player-friendly overview labels
    // GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: Enriched overview (U6)
    private void RefreshOverview()
    {
        var d = _bridge.GetEmpireSummaryV0();
        if (d == null) return;

        var credits = GetInt(d, "credits");
        var tick = GetInt(d, "tick");
        // L3.1: Credit trend arrow from profit summary
        string trendStr = "";
        if (_bridge.HasMethod("GetProfitSummaryV0"))
        {
            var profitData = _bridge.Call("GetProfitSummaryV0").AsGodotDictionary();
            if (profitData != null)
            {
                int netProfit = GetInt(profitData, "net_profit");
                if (netProfit > 0) trendStr = $"  \u2191 +{FormatNum(netProfit)} cr";
                else if (netProfit < 0) trendStr = $"  \u2193 {FormatNum(netProfit)} cr";
            }
        }
        _ovCredits.Text = $"{FormatNum(credits)} credits  |  Tick {FormatNum(tick)}{trendStr}";

        // Spark-chart: mini credit trend bars
        RefreshSparkChart();

        // Fleet card: player fleets + current system context
        var playerFleets = GetInt(d, "player_fleet_count");
        var totalFleets = GetInt(d, "fleet_count");
        var ps = _bridge.GetPlayerStateV0();
        var nodeName = ps != null ? GetStr(ps, "node_name") : "";
        // FEEL_POST_FIX_8: Strip parenthesized production tags for clean display.
        var parenIdx = nodeName.IndexOf('(');
        if (parenIdx > 0) nodeName = nodeName[..parenIdx].Trim();
        _ovFleets.Text = string.IsNullOrEmpty(nodeName)
            ? $"{playerFleets} player, {totalFleets} in galaxy"
            : $"{playerFleets} player, {totalFleets} total  |  At: {nodeName}";

        // Automation card: show cycle/credits if running, guidance if not
        var progCount = GetInt(d, "program_count");
        if (progCount > 0)
        {
            var perf = _bridge.GetProgramPerformanceV0("fleet_trader_1");
            if (perf != null && perf.Count > 0)
            {
                var cycles = GetInt(perf, "cycles_run");
                var earned = GetInt(perf, "credits_earned");
                var moved = GetInt(perf, "goods_moved");
                _ovPrograms.Text = $"{progCount} running  |  {FormatNum(cycles)} cycles, {FormatNum(earned)} cr earned, {FormatNum(moved)} goods";
            }
            else
            {
                _ovPrograms.Text = $"{progCount} running";
            }
        }
        else
        {
            _ovPrograms.Text = "No active programs \u2014 open Automation (A key)";
        }

        // Exploration card
        var missionCount = GetInt(d, "active_mission_count");
        if (missionCount > 0)
        {
            _ovMissions.Text = $"{missionCount} active";
        }
        else
        {
            _ovMissions.Text = "No active missions \u2014 dock at a station (M key)";
        }

        // Industry card
        var activeIndustry = GetInt(d, "active_industry_count");
        var totalIndustry = GetInt(d, "industry_site_count");
        if (activeIndustry == totalIndustry && totalIndustry > 0)
            _ovIndustry.Text = $"All {totalIndustry} sites operational";
        else if (totalIndustry > 0)
            _ovIndustry.Text = $"{activeIndustry} of {totalIndustry} sites active  |  {totalIndustry - activeIndustry} idle";
        else
            _ovIndustry.Text = "No production discovered yet";

        // Research card
        var tech = GetStr(d, "research_tech_id");
        var unlockedCount = GetInt(d, "unlocked_tech_count");
        if (string.IsNullOrWhiteSpace(tech))
        {
            _ovResearch.Text = unlockedCount > 0
                ? $"Idle  |  {unlockedCount} tech unlocked"
                : "No research in progress";
        }
        else
        {
            var pct = GetInt(d, "research_progress_pct");
            _ovResearch.Text = unlockedCount > 0
                ? $"{tech} ({pct}%)  |  {unlockedCount} unlocked"
                : $"{tech} ({pct}%)";
        }

        // L3.1: Proactive Opportunities — trade intel, research guidance, warfront impact.
        if (_ovAttentionList != null)
        {
            foreach (var c in _ovAttentionList.GetChildren()) c.QueueFree();

            // Collect proactive items: (text, color)
            var items = new System.Collections.Generic.List<(string text, Color color)>();
            var infoBlue = new Color(0.55f, 0.75f, 0.95f);
            var gold = new Color(1.0f, 0.85f, 0.4f);
            var warningOrange = new Color(1.0f, 0.6f, 0.2f);
            var green = new Color(0.4f, 0.8f, 0.4f);

            // Best trade route from GetTradeRoutesV0
            var routes = _bridge.GetTradeRoutesV0();
            if (routes != null && routes.Count > 0)
            {
                int bestProfit = 0;
                string bestGood = "";
                string bestDest = "";
                foreach (var r in routes)
                {
                    if (r.AsGodotDictionary() is not Dictionary rd) continue;
                    int profit = GetInt(rd, "estimated_profit_per_unit");
                    if (profit > bestProfit)
                    {
                        bestProfit = profit;
                        bestGood = GetStr(rd, "good_id");
                        bestDest = GetStr(rd, "dest_node_id");
                    }
                }
                if (bestProfit > 0 && !string.IsNullOrEmpty(bestGood))
                {
                    // Strip paren tags from dest name
                    var destDisplay = bestDest;
                    var pi = destDisplay.IndexOf('(');
                    if (pi > 0) destDisplay = destDisplay[..pi].Trim();
                    items.Add(($"Best trade: {bestGood} +{bestProfit} cr/unit \u2192 {destDisplay}", gold));
                }
            }

            // Research: show available tech count if idle
            if (string.IsNullOrWhiteSpace(tech))
            {
                var techTree = _bridge.GetTechTreeV0();
                int availCount = 0;
                if (techTree != null)
                {
                    foreach (var t in techTree)
                    {
                        if (t.AsGodotDictionary() is Dictionary td && GetStr(td, "status") == "available")
                            availCount++;
                    }
                }
                if (availCount > 0)
                    items.Add(($"{availCount} tech available \u2014 dock at a station to research", infoBlue));
            }

            // Warfront impact
            var warfronts = _bridge.GetWarfrontsV0();
            int activeWarfronts = 0;
            if (warfronts != null)
            {
                foreach (var w in warfronts)
                {
                    if (w.AsGodotDictionary() is Dictionary wd)
                    {
                        int intensity = GetInt(wd, "intensity");
                        if (intensity > 0) activeWarfronts++;
                    }
                }
            }
            if (activeWarfronts > 0)
                items.Add(($"{activeWarfronts} warfront{(activeWarfronts > 1 ? "s" : "")} active \u2014 prices disrupted in contested zones", warningOrange));

            // Original attention items (lower priority)
            if (credits < 100) items.Add(("Low credits \u2014 trade or complete missions", warningOrange));
            if (activeIndustry < totalIndustry && totalIndustry > 0)
                items.Add(($"{totalIndustry - activeIndustry} production sites idle", infoBlue));
            if (progCount == 0) items.Add(("No automation running \u2014 press A to configure", infoBlue));
            if (missionCount == 0) items.Add(("No active missions \u2014 dock to find work", infoBlue));

            if (items.Count == 0)
            {
                var ok = new Label { Text = "All systems nominal." };
                ok.AddThemeColorOverride("font_color", green);
                _ovAttentionList.AddChild(ok);
            }
            else
            {
                foreach (var (itemText, itemColor) in items)
                {
                    var lbl = new Label { Text = $"  \u2022 {itemText}" };
                    lbl.AddThemeColorOverride("font_color", itemColor);
                    _ovAttentionList.AddChild(lbl);
                }
            }
        }

        // GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: System economy info (U6)
        if (_ovSystemInfo != null)
        {
            var nodeId = ps != null ? GetStr(ps, "current_node_id") : "";
            if (!string.IsNullOrEmpty(nodeId))
            {
                var routes = _bridge.GetTradeRoutesV0();
                int routeCount = 0;
                if (routes != null)
                {
                    foreach (var r in routes)
                    {
                        if (r.AsGodotDictionary() is Dictionary rd)
                        {
                            var src = GetStr(rd, "source_node_id");
                            var dst = GetStr(rd, "dest_node_id");
                            if (src == nodeId || dst == nodeId) routeCount++;
                        }
                    }
                }
                // L3.1: Show best trade from current system
                string bestRouteStr = "";
                if (routes != null)
                {
                    int bestP = 0; string bestG = "";
                    foreach (var r in routes)
                    {
                        if (r.AsGodotDictionary() is not Dictionary rd2) continue;
                        var src = GetStr(rd2, "source_node_id");
                        if (src != nodeId) continue;
                        int p = GetInt(rd2, "estimated_profit_per_unit");
                        if (p > bestP) { bestP = p; bestG = GetStr(rd2, "good_id"); }
                    }
                    if (bestP > 0 && !string.IsNullOrEmpty(bestG))
                        bestRouteStr = $"  |  Best: {bestG} +{bestP}/u";
                }
                _ovSystemInfo.Text = string.IsNullOrEmpty(nodeName)
                    ? $"Node {nodeId}  |  {routeCount} route(s){bestRouteStr}"
                    : $"{nodeName}  |  {routeCount} route(s){bestRouteStr}";
            }
            else
            {
                _ovSystemInfo.Text = "Not docked at any system";
            }
        }

        // GATE.S7.RUNTIME_STABILITY.DASHBOARD_CONTENT.001: Recent fleet activity (U6)
        if (_ovRecentActivity != null)
        {
            var perf = _bridge.GetProgramPerformanceV0("fleet_trader_1");
            if (perf != null && perf.Count > 0)
            {
                var history = perf.ContainsKey("history") ? perf["history"].AsGodotArray() : null;
                if (history != null && history.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    int shown = 0;
                    foreach (var entry in history)
                    {
                        if (shown >= 3) break;
                        if (entry.AsGodotDictionary() is Dictionary hd)
                        {
                            var hTick = GetInt(hd, "tick");
                            var success = hd.ContainsKey("success") && (bool)hd["success"];
                            var hCredits = GetInt(hd, "credits_earned");
                            var hGoods = GetInt(hd, "goods_moved");
                            var reason = GetStr(hd, "failure_reason");
                            if (success)
                                sb.AppendLine($"  Tick {hTick}: +{FormatNum(hCredits)} cr, {hGoods} goods moved");
                            else
                                sb.AppendLine($"  Tick {hTick}: Failed \u2014 {reason}");
                            shown++;
                        }
                    }
                    _ovRecentActivity.Text = shown > 0 ? sb.ToString().TrimEnd() : "No recent activity";
                }
                else
                {
                    _ovRecentActivity.Text = "Automation active \u2014 no history yet";
                }
            }
            else
            {
                _ovRecentActivity.Text = "No fleet automation configured";
            }
        }

        // Trade Performance stats
        if (_ovTradeStats != null && _bridge.HasMethod("GetProfitSummaryV0"))
        {
            var profitData = _bridge.Call("GetProfitSummaryV0").AsGodotDictionary();
            if (profitData != null)
            {
                var revenue = GetInt(profitData, "total_revenue");
                var expense = GetInt(profitData, "total_expense");
                var netProfit = GetInt(profitData, "net_profit");
                var topGood = GetStr(profitData, "top_good");
                if (revenue > 0 || expense > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"Revenue: {FormatNum(revenue)} cr  |  Spent: {FormatNum(expense)} cr  |  Net: {(netProfit >= 0 ? "+" : "")}{FormatNum(netProfit)} cr");
                    if (!string.IsNullOrEmpty(topGood))
                        sb.Append($"\nTop good: {topGood}");
                    _ovTradeStats.Text = sb.ToString();
                    _ovTradeStats.AddThemeColorOverride("font_color", netProfit >= 0
                        ? new Color(0.5f, 0.9f, 0.5f)
                        : new Color(0.9f, 0.5f, 0.4f));
                }
                else
                {
                    _ovTradeStats.Text = "No trades yet \u2014 dock at a station to buy goods";
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TRADE TAB
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildTradeTab()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);

        // ── Economy Overview section ──
        // GATE.S18.EMPIRE_DASH.ECONOMY_TAB.001
        var econHdr = new Label { Text = "ECONOMY — Goods Across All Stations" };
        econHdr.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 0.6f, 1f));
        box.AddChild(econHdr);

        var econColHdr = new Label { Text = "Good              Total  Stations  Avg Price  Range" };
        econColHdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(econColHdr);

        _econList = new VBoxContainer();
        _econList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(_econList);

        box.AddChild(new HSeparator());

        // ── Trade Routes section ──
        var routeHdr = new Label { Text = "TRADE ROUTES — Discovered" };
        routeHdr.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 0.6f, 1f));
        box.AddChild(routeHdr);

        var hdr = new Label { Text = "Good  |  Source → Dest  |  Profit/unit  |  Status  |  Last Validated" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(hdr);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 200);

        _tradeList = new VBoxContainer();
        _tradeList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_tradeList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshSparkChart()
    {
        if (_sparkChart == null || !_bridge.HasMethod("GetCreditHistoryV0")) return;
        // Clear old bars
        foreach (var child in _sparkChart.GetChildren())
            child.QueueFree();

        var histData = _bridge.Call("GetCreditHistoryV0").AsGodotDictionary();
        if (histData == null) return;
        var points = histData["points"].AsGodotArray();
        if (points == null || points.Count == 0) return;

        // Find max absolute value for normalization
        long maxAbs = 1;
        foreach (var p in points)
        {
            long v = Math.Abs(p.AsInt64());
            if (v > maxAbs) maxAbs = v;
        }

        long netTotal = 0;
        foreach (var p in points)
        {
            long val = p.AsInt64();
            netTotal += val;
            float ratio = (float)Math.Abs(val) / maxAbs;
            var bar = new ColorRect();
            bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            bar.SizeFlagsVertical = SizeFlags.ShrinkEnd;
            bar.CustomMinimumSize = new Vector2(0, Math.Max(3, ratio * 36));
            bar.Color = val >= 0
                ? new Color(0.3f, 0.8f, 0.5f, 0.7f)   // profit = green tint
                : new Color(0.9f, 0.35f, 0.25f, 0.7f); // loss = red tint
            _sparkChart.AddChild(bar);
        }

        // Summary label: net credit flow
        if (_sparkChartSummary != null)
        {
            string sign = netTotal >= 0 ? "+" : "";
            _sparkChartSummary.Text = $"Net: {sign}{netTotal:N0} cr";
            _sparkChartSummary.AddThemeColorOverride("font_color",
                netTotal >= 0
                    ? new Color(0.3f, 0.8f, 0.5f, 0.8f)
                    : new Color(0.9f, 0.35f, 0.25f, 0.8f));
        }
    }

    private void RefreshTrade()
    {
        // ── Economy overview ──
        // GATE.S18.EMPIRE_DASH.ECONOMY_TAB.001
        ClearChildren(_econList);
        var econ = _bridge.GetEconomyOverviewV0();
        if (econ != null && econ.Count > 0)
        {
            foreach (var v in econ)
            {
                var d = v.Obj as Dictionary;
                if (d == null) continue;
                var name    = GetStr(d, "display_name");
                var total   = GetInt(d, "total_qty");
                var stations = GetInt(d, "station_count");
                var avg     = GetInt(d, "avg_price");
                var min     = GetInt(d, "min_price");
                var max     = GetInt(d, "max_price");
                var lbl = new Label
                {
                    Text = $"{name,-18}{total,5}    {stations,3}     {avg,6} cr   {min}-{max}",
                    AutowrapMode = TextServer.AutowrapMode.Off
                };
                _econList.AddChild(lbl);
            }
        }
        else
        {
            _econList.AddChild(new Label { Text = "No market data available." });
        }

        // ── Trade routes ──
        ClearChildren(_tradeList);
        var routes = _bridge.GetTradeRoutesV0();
        if (routes == null || routes.Count == 0)
        {
            _tradeList.AddChild(new Label { Text = "No trade routes discovered yet." });
            return;
        }

        foreach (var v in routes)
        {
            var d = v.Obj as Dictionary;
            if (d == null) continue;

            var good   = GetStr(d, "good_id");
            var src    = GetStr(d, "source_node_id");
            var dest   = GetStr(d, "dest_node_id");
            var profit = GetInt(d, "estimated_profit_per_unit");
            var status = GetStr(d, "status");
            var lastV  = GetInt(d, "last_validated_tick");

            var lbl = new Label
            {
                Text = $"{good,-16}  {src} → {dest,-16}  +{profit,6} cr/u  {status,-12}  @tick {lastV}",
                AutowrapMode = TextServer.AutowrapMode.Off
            };
            _tradeList.AddChild(lbl);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRODUCTION TAB
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildProductionTab()
    {
        var box = new VBoxContainer();

        var hdr = new Label { Text = "Site  |  Node  |  Recipe  |  Health%  |  Efficiency%  |  Active" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(hdr);
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 300);

        _prodList = new VBoxContainer();
        _prodList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_prodList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshProduction()
    {
        ClearChildren(_prodList);
        var sites = _bridge.GetAllIndustryV0();
        if (sites == null || sites.Count == 0)
        {
            _prodList.AddChild(new Label { Text = "No industry sites." });
            return;
        }

        foreach (var v in sites)
        {
            var d = v.Obj as Dictionary;
            if (d == null) continue;

            var siteId   = GetStr(d, "site_id");
            var nodeId   = GetStr(d, "node_id");
            var recipe   = GetStr(d, "recipe_id");
            var health   = GetFloat(d, "health_pct");
            var eff      = GetFloat(d, "efficiency");
            var active   = GetBool(d, "active") ? "YES" : "no";

            var lbl = new Label
            {
                Text = $"{siteId,-20}  {nodeId,-16}  {recipe,-20}  hp={health:P0}  eff={eff:P0}  {active}",
                AutowrapMode = TextServer.AutowrapMode.Off
            };
            _prodList.AddChild(lbl);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PROGRAMS TAB  (includes Escort / Patrol creation — GATE.S5.ESCORT_PROG.UI.001)
    // ═══════════════════════════════════════════════════════════════════════
    private static readonly string[] ProgKinds =
        { "AutoSell", "TradeCharter", "ResourceTap", "Escort", "Patrol" };

    private Control BuildProgramsTab()
    {
        var outerBox = new VBoxContainer();
        outerBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        outerBox.SizeFlagsVertical   = SizeFlags.ExpandFill;

        // ── Program list ────────────────────────────────────────────────────
        var hdr = new Label { Text = "ID  |  Kind  |  Status  |  Market  |  Good  |  Qty" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        outerBox.AddChild(hdr);
        outerBox.AddChild(new HSeparator());

        var listScroll = new ScrollContainer();
        listScroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        listScroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        listScroll.CustomMinimumSize   = new Vector2(0, 220);

        _progList = new VBoxContainer();
        _progList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        listScroll.AddChild(_progList);
        outerBox.AddChild(listScroll);

        outerBox.AddChild(new HSeparator());

        // ── Creation form ────────────────────────────────────────────────────
        var formBox = new VBoxContainer();
        formBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        outerBox.AddChild(formBox);

        var formTitle = new Label { Text = "Create Program" };
        formTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1.0f, 1f));
        formBox.AddChild(formTitle);

        // Kind row
        var kindRow = new HBoxContainer();
        kindRow.AddChild(new Label { Text = "Kind:", CustomMinimumSize = new Vector2(120, 0) });
        _progKindDropdown = new OptionButton();
        foreach (var k in ProgKinds) _progKindDropdown.AddItem(k);
        _progKindDropdown.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _progKindDropdown.ItemSelected += _ => UpdateFormLabels();
        kindRow.AddChild(_progKindDropdown);
        formBox.AddChild(kindRow);

        // Field rows (5 generic slots)
        Control FieldRow(out Label lbl, out LineEdit edit, string defLabel)
        {
            var row = new HBoxContainer();
            lbl = new Label { Text = defLabel, CustomMinimumSize = new Vector2(160, 0) };
            edit = new LineEdit();
            edit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(lbl);
            row.AddChild(edit);
            return row;
        }

        formBox.AddChild(FieldRow(out _progField1Label, out _progField1, "Field 1:"));
        formBox.AddChild(FieldRow(out _progField2Label, out _progField2, "Field 2:"));
        formBox.AddChild(FieldRow(out _progField3Label, out _progField3, "Field 3:"));
        formBox.AddChild(FieldRow(out _progField4Label, out _progField4, "Field 4:"));
        formBox.AddChild(FieldRow(out _progField5Label, out _progField5, "Field 5:"));

        // Cadence row (reuse field5 for cadence on kinds that need ≤4 fields)
        var btnCreate = new Button { Text = "Create & Start" };
        btnCreate.Pressed += OnCreateProgram;
        formBox.AddChild(btnCreate);

        _progCreateStatus = new Label { Text = "" };
        formBox.AddChild(_progCreateStatus);

        // Initialise labels for first kind
        UpdateFormLabels();

        return outerBox;
    }

    private void UpdateFormLabels()
    {
        int idx = _progKindDropdown.Selected;
        var kind = idx >= 0 && idx < ProgKinds.Length ? ProgKinds[idx] : "AutoSell";

        // Reset all
        _progField1.Visible = true;
        _progField2.Visible = true;
        _progField3.Visible = true;
        _progField4.Visible = true;
        _progField5.Visible = true;
        _progField1Label.Visible = true;
        _progField2Label.Visible = true;
        _progField3Label.Visible = true;
        _progField4Label.Visible = true;
        _progField5Label.Visible = true;

        switch (kind)
        {
            case "AutoSell":
                // marketId, goodId, quantity, cadenceTicks
                _progField1Label.Text = "Market ID:";
                _progField2Label.Text = "Good ID:";
                _progField3Label.Text = "Quantity:";
                _progField4Label.Text = "Cadence (ticks):";
                _progField5.Visible = false;
                _progField5Label.Visible = false;
                break;

            case "TradeCharter":
                // sourceMarketId, destMarketId, buyGoodId, sellGoodId, cadenceTicks
                _progField1Label.Text = "Source Market ID:";
                _progField2Label.Text = "Dest Market ID:";
                _progField3Label.Text = "Buy Good ID:";
                _progField4Label.Text = "Sell Good ID:";
                _progField5Label.Text = "Cadence (ticks):";
                break;

            case "ResourceTap":
                // sourceMarketId, extractGoodId, cadenceTicks
                _progField1Label.Text = "Source Market ID:";
                _progField2Label.Text = "Extract Good ID:";
                _progField3Label.Text = "Cadence (ticks):";
                _progField4.Visible = false;
                _progField4Label.Visible = false;
                _progField5.Visible = false;
                _progField5Label.Visible = false;
                break;

            case "Escort":
                // fleetId, originNodeId, destNodeId, cadenceTicks
                _progField1Label.Text = "Fleet ID:";
                _progField2Label.Text = "Origin Node ID:";
                _progField3Label.Text = "Dest Node ID:";
                _progField4Label.Text = "Cadence (ticks):";
                _progField5.Visible = false;
                _progField5Label.Visible = false;
                break;

            case "Patrol":
                // fleetId, nodeA, nodeB, cadenceTicks
                _progField1Label.Text = "Fleet ID:";
                _progField2Label.Text = "Node A:";
                _progField3Label.Text = "Node B:";
                _progField4Label.Text = "Cadence (ticks):";
                _progField5.Visible = false;
                _progField5Label.Visible = false;
                break;
        }
    }

    private void OnCreateProgram()
    {
        _progCreateStatus.Text = "";
        int idx = _progKindDropdown.Selected;
        var kind = idx >= 0 && idx < ProgKinds.Length ? ProgKinds[idx] : "AutoSell";

        var f1 = _progField1.Text.Trim();
        var f2 = _progField2.Text.Trim();
        var f3 = _progField3.Text.Trim();
        var f4 = _progField4.Text.Trim();
        var f5 = _progField5.Text.Trim();

        string programId;
        try
        {
            switch (kind)
            {
                case "AutoSell":
                {
                    if (!int.TryParse(f3, out var qty)) { _progCreateStatus.Text = "ERR: qty must be int"; return; }
                    if (!int.TryParse(f4, out var cad)) { _progCreateStatus.Text = "ERR: cadence must be int"; return; }
                    programId = _bridge.CreateAutoSellProgram(f1, f2, qty, cad);
                    break;
                }
                case "TradeCharter":
                {
                    if (!int.TryParse(f5, out var cad)) { _progCreateStatus.Text = "ERR: cadence must be int"; return; }
                    programId = _bridge.CreateTradeCharterProgram(f1, f2, f3, f4, cad);
                    break;
                }
                case "ResourceTap":
                {
                    if (!int.TryParse(f3, out var cad)) { _progCreateStatus.Text = "ERR: cadence must be int"; return; }
                    programId = _bridge.CreateResourceTapProgram(f1, f2, cad);
                    break;
                }
                case "Escort":
                {
                    if (!int.TryParse(f4, out var cad)) { _progCreateStatus.Text = "ERR: cadence must be int"; return; }
                    programId = _bridge.CreateEscortProgramV0(f1, f2, f3, cad);
                    break;
                }
                case "Patrol":
                {
                    if (!int.TryParse(f4, out var cad)) { _progCreateStatus.Text = "ERR: cadence must be int"; return; }
                    programId = _bridge.CreatePatrolProgramV0(f1, f2, f3, cad);
                    break;
                }
                default:
                    _progCreateStatus.Text = "ERR: unknown kind";
                    return;
            }

            if (string.IsNullOrWhiteSpace(programId))
            {
                _progCreateStatus.Text = "ERR: bridge returned empty id (check inputs)";
                return;
            }

            _bridge.StartProgram(programId);
            _progCreateStatus.Text = $"OK: {programId}";
            RefreshPrograms();
        }
        catch (Exception ex)
        {
            _progCreateStatus.Text = $"ERR: {ex.Message}";
        }
    }

    private void RefreshPrograms()
    {
        ClearChildren(_progList);

        // GATE.S7.AUTOMATION.UI.001: Performance summary + budget header.
        var perf = _bridge.GetProgramPerformanceV0("fleet_trader_1");
        if (perf != null && perf.Count > 0)
        {
            var netProfit = GetLong(perf, "net_profit");
            var trades = GetInt(perf, "trades_completed");
            var failures = GetInt(perf, "consecutive_failures");
            var lastReason = GetStr(perf, "last_failure_reason");
            var budgetCap = GetLong(perf, "budget_credit_cap");
            var spentCycle = GetLong(perf, "spent_credits_this_cycle");

            var profitColor = netProfit >= 0
                ? new Color(0.4f, 0.9f, 0.4f)
                : new Color(0.9f, 0.4f, 0.4f);
            var profitSign = netProfit >= 0 ? "+" : "";
            var summaryLbl = new Label
            {
                Text = $"Net P/L: {profitSign}{netProfit:N0} cr  |  Trades: {trades}  |  Failures: {failures}",
            };
            summaryLbl.AddThemeColorOverride("font_color", profitColor);
            _progList.AddChild(summaryLbl);

            // Budget line
            if (budgetCap > 0)
            {
                var budgetLbl = new Label
                {
                    Text = $"Budget: {spentCycle:N0} / {budgetCap:N0} cr this cycle",
                };
                budgetLbl.AddThemeColorOverride("font_color",
                    spentCycle > budgetCap ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.8f));
                _progList.AddChild(budgetLbl);
            }

            // Failure reason (if consecutive failures > 0)
            if (failures > 0 && !string.IsNullOrEmpty(lastReason) && lastReason != "None")
            {
                var reasonLbl = new Label
                {
                    Text = $"Last failure: {FormatFailureReason(lastReason)}",
                };
                reasonLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.6f, 0.3f));
                _progList.AddChild(reasonLbl);
            }

            _progList.AddChild(new HSeparator());
        }

        var arr = _bridge.GetProgramExplainSnapshot();
        if (arr == null || arr.Count == 0)
        {
            _progList.AddChild(new Label { Text = "(no programs)" });
            return;
        }

        var rows = arr
            .Select(v => v.Obj as Dictionary)
            .Where(d => d != null)
            .OrderBy(d => GetStr(d!, "id"), StringComparer.Ordinal)
            .ToArray();

        foreach (var d in rows)
        {
            var id      = GetStr(d!, "id");
            var kind    = GetStr(d!, "kind");
            var status  = GetStr(d!, "status");
            var market  = GetStr(d!, "market_id");
            var good    = GetStr(d!, "good_id");
            var qty     = GetInt(d!, "quantity");

            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _progList.AddChild(row);

            var lbl = new Label
            {
                Text = $"{id,-28}  {kind,-14}  {status,-10}  {market,-18}  {good,-16}  qty={qty}",
                AutowrapMode = TextServer.AutowrapMode.Off,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            row.AddChild(lbl);

            var btnStart = new Button { Text = "Start" };
            btnStart.Disabled = status == "Running" || status == "Cancelled";
            btnStart.Pressed += () => { _bridge.StartProgram(id); RefreshPrograms(); };
            row.AddChild(btnStart);

            var btnPause = new Button { Text = "Pause" };
            btnPause.Disabled = status == "Paused" || status == "Cancelled";
            btnPause.Pressed += () => { _bridge.PauseProgram(id); RefreshPrograms(); };
            row.AddChild(btnPause);

            var btnCancel = new Button { Text = "Cancel" };
            btnCancel.Disabled = status == "Cancelled";
            btnCancel.Pressed += () => { _bridge.CancelProgram(id); RefreshPrograms(); };
            row.AddChild(btnCancel);
        }
    }

    // GATE.S7.AUTOMATION.UI.001: Human-readable failure reasons.
    private static string FormatFailureReason(string reason)
    {
        return reason switch
        {
            "InsufficientCredits" => "Not enough credits to execute trade",
            "InsufficientCargo" => "No cargo available to sell",
            "MarketNotFound" => "Target market no longer accessible",
            "BudgetExceeded" => "Spending would exceed budget cap",
            "FleetNotAtMarket" => "Fleet not at the designated market",
            "GoodNotAvailable" => "Good not available at this market",
            _ => reason,
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INTEL TAB
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildIntelTab()
    {
        var box = new VBoxContainer();

        var hdr = new Label { Text = "Node  |  Observations  |  Age (ticks)" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(hdr);
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 300);

        _intelList = new VBoxContainer();
        _intelList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_intelList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshIntel()
    {
        ClearChildren(_intelList);
        var entries = _bridge.GetIntelFreshnessByNodeV0();
        if (entries == null || entries.Count == 0)
        {
            _intelList.AddChild(new Label { Text = "No intel observations." });
            return;
        }

        var sorted = entries
            .Select(v => v.Obj as Dictionary)
            .Where(d => d != null)
            .OrderBy(d => GetStr(d!, "node_id"), StringComparer.Ordinal)
            .ToArray();

        foreach (var d in sorted)
        {
            var node = GetStr(d!, "node_id");
            var obs  = GetInt(d!, "observation_count");
            var age  = GetInt(d!, "age_ticks");

            _intelList.AddChild(new Label
            {
                Text = $"{node,-24}  obs={obs,5}  age={age,6} ticks",
                AutowrapMode = TextServer.AutowrapMode.Off
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RESEARCH TAB  (GATE.S11.GAME_FEEL.TECH_TREE_UI.001)
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildResearchTab()
    {
        var box = new VBoxContainer();
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical   = SizeFlags.ExpandFill;

        var hdr = new Label { Text = "Tech  |  Status  |  Tier  |  Prerequisites  |  Sustain  |  Effects" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(hdr);
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 300);

        _researchList = new VBoxContainer();
        _researchList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_researchList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshResearch()
    {
        ClearChildren(_researchList);
        var techs = _bridge.GetTechTreeV0();
        if (techs == null || techs.Count == 0)
        {
            _researchList.AddChild(new Label { Text = "No technologies available." });
            return;
        }

        // Group by tier, sorted ascending
        var grouped = techs
            .Select(v => v.Obj as Dictionary)
            .Where(d => d != null)
            .OrderBy(d => GetInt(d!, "tier"))
            .ThenBy(d => GetStr(d!, "tech_id"), StringComparer.Ordinal)
            .GroupBy(d => GetInt(d!, "tier"))
            .OrderBy(g => g.Key);

        foreach (var tierGroup in grouped)
        {
            // Tier header
            var tierLabel = new Label { Text = $"--- Tier {tierGroup.Key} ---" };
            tierLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f, 1f));
            _researchList.AddChild(tierLabel);

            foreach (var d in tierGroup)
            {
                var techId    = GetStr(d!, "tech_id");
                var name      = GetStr(d!, "display_name");
                var status    = GetStr(d!, "status");
                var prereqs   = GetStr(d!, "prereqs");
                var sustain   = GetStr(d!, "sustain_inputs");
                var effects   = GetStr(d!, "effects");
                var ticks     = GetInt(d!, "research_ticks");
                var cost      = GetInt(d!, "credit_cost");

                // Color-coded status
                Color statusColor = status switch
                {
                    "done"        => new Color(0.3f, 1.0f, 0.3f, 1f),   // green
                    "researching" => new Color(1.0f, 1.0f, 0.3f, 1f),   // yellow
                    "available"   => new Color(1.0f, 1.0f, 1.0f, 1f),   // white
                    _             => new Color(0.5f, 0.5f, 0.5f, 1f),   // gray (locked)
                };

                var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                _researchList.AddChild(row);

                var infoText = $"{name,-24}  [{status,-12}]  ticks={ticks}  cost={cost}";
                if (!string.IsNullOrEmpty(prereqs))
                    infoText += $"  prereqs=[{prereqs}]";
                if (!string.IsNullOrEmpty(sustain))
                    infoText += $"  sustain=[{sustain}]";
                if (!string.IsNullOrEmpty(effects))
                    infoText += $"  effects=[{effects}]";

                var lbl = new Label
                {
                    Text = infoText,
                    AutowrapMode = TextServer.AutowrapMode.Off,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                lbl.AddThemeColorOverride("font_color", statusColor);
                row.AddChild(lbl);

                // "Start Research" button for available techs
                if (status == "available")
                {
                    var btnStart = new Button { Text = "Start Research" };
                    var capturedId = techId;
                    btnStart.Pressed += () =>
                    {
                        _bridge.StartResearchV0(capturedId, "");
                        RefreshResearch();
                    };
                    row.AddChild(btnStart);
                }
            }

            _researchList.AddChild(new HSeparator());
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STATS TAB  (GATE.S12.PROGRESSION.DASHBOARD.001)
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildStatsTab()
    {
        var box = new VBoxContainer();
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical   = SizeFlags.ExpandFill;

        // Stats section
        var statsHdr = new Label { Text = "Player Statistics" };
        statsHdr.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1.0f, 1f));
        box.AddChild(statsHdr);
        box.AddChild(new HSeparator());

        _statsList = new VBoxContainer();
        _statsList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _statsList.AddThemeConstantOverride("separation", 4);
        box.AddChild(_statsList);

        box.AddChild(new HSeparator());

        // Milestones section
        var msHdr = new Label { Text = "Milestones" };
        msHdr.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1.0f, 1f));
        box.AddChild(msHdr);
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 300);

        _milestonesList = new VBoxContainer();
        _milestonesList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_milestonesList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshStats()
    {
        ClearChildren(_statsList);
        ClearChildren(_milestonesList);

        // Player stats
        var stats = _bridge.GetPlayerStatsV0();
        if (stats != null && stats.Count > 0)
        {
            AddStatRow("Nodes Visited", GetInt(stats, "nodes_visited"));
            AddStatRow("Goods Traded", GetInt(stats, "goods_traded"));
            AddStatRow("Total Credits Earned", GetInt(stats, "total_credits_earned"));
            AddStatRow("Techs Unlocked", GetInt(stats, "techs_unlocked"));
            AddStatRow("Missions Completed", GetInt(stats, "missions_completed"));
        }
        else
        {
            _statsList.AddChild(new Label { Text = "No stats available." });
        }

        // Milestones
        var milestones = _bridge.GetMilestonesV0();
        if (milestones == null || milestones.Count == 0)
        {
            _milestonesList.AddChild(new Label { Text = "No milestones defined." });
            return;
        }

        foreach (var v in milestones)
        {
            var d = v.Obj as Dictionary;
            if (d == null) continue;

            var name     = GetStr(d, "name");
            var achieved = GetBool(d, "achieved");
            var current  = GetInt(d, "current");
            var threshold = GetInt(d, "threshold");

            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _milestonesList.AddChild(row);

            // Icon: checkmark or empty
            var icon = new Label { Text = achieved ? "[X]" : "[ ]", CustomMinimumSize = new Vector2(40, 0) };
            icon.AddThemeColorOverride("font_color", achieved
                ? new Color(0.3f, 1.0f, 0.3f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f));
            row.AddChild(icon);

            // Name
            var nameLbl = new Label { Text = name, CustomMinimumSize = new Vector2(200, 0) };
            nameLbl.AddThemeColorOverride("font_color", achieved
                ? new Color(0.3f, 1.0f, 0.3f, 1f)
                : new Color(1.0f, 1.0f, 1.0f, 1f));
            row.AddChild(nameLbl);

            // Progress
            if (achieved)
            {
                var doneLbl = new Label { Text = "Complete!" };
                doneLbl.AddThemeColorOverride("font_color", new Color(0.3f, 1.0f, 0.3f, 1f));
                row.AddChild(doneLbl);
            }
            else
            {
                var progBar = new ProgressBar();
                progBar.MinValue = 0;
                progBar.MaxValue = threshold > 0 ? threshold : 1;
                progBar.Value = current;
                progBar.CustomMinimumSize = new Vector2(120, 20);
                progBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(progBar);

                var progLbl = new Label { Text = $"{current} / {threshold}" };
                progLbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f, 1f));
                row.AddChild(progLbl);
            }
        }
    }

    private void AddStatRow(string label, int value)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(220, 0) });
        var val = new Label { Text = FormatNum(value) };
        val.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(val);
        _statsList.AddChild(row);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FACTIONS TAB  (GATE.S7.FACTION.UI_REPUTATION.001)
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildFactionsTab()
    {
        var box = new VBoxContainer();
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical   = SizeFlags.ExpandFill;

        var hdr = new Label { Text = "Faction  |  Reputation  |  Trade Policy  |  Tariff  |  Territory" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(hdr);
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 300);

        _factionList = new VBoxContainer();
        _factionList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _factionList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_factionList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshFactions()
    {
        ClearChildren(_factionList);
        var factions = _bridge.GetAllFactionsV0();
        if (factions == null || factions.Count == 0)
        {
            _factionList.AddChild(new Label { Text = "No factions discovered." });
            return;
        }

        foreach (var v in factions)
        {
            var d = v.Obj as Dictionary;
            if (d == null) continue;

            var factionId     = GetStr(d, "faction_id");
            var reputation    = GetInt(d, "reputation");
            var tradePolicy   = GetStr(d, "trade_policy");
            var tariffRate    = GetFloat(d, "tariff_rate");
            var territoryCount = GetInt(d, "territory_count");

            var row = new VBoxContainer();
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _factionList.AddChild(row);

            // GATE.S7.FACTION.IDENTITY_PANEL.001: Fetch detailed faction identity.
            var detail = _bridge.GetFactionDetailV0(factionId);
            var species    = GetStr(detail, "species");
            var philosophy = GetStr(detail, "philosophy");
            var producesArr = detail.ContainsKey("produces") ? detail["produces"].As<Godot.Collections.Array>() : null;
            var needsArr    = detail.ContainsKey("needs") ? detail["needs"].As<Godot.Collections.Array>() : null;

            // Top line: faction name + species + philosophy + policy + tariff + territory
            var topRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddChild(topRow);

            var nameLbl = new Label { Text = factionId, CustomMinimumSize = new Vector2(140, 0) };
            nameLbl.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1.0f, 1f));
            topRow.AddChild(nameLbl);

            if (!string.IsNullOrEmpty(species))
            {
                var speciesLbl = new Label { Text = species, CustomMinimumSize = new Vector2(90, 0) };
                speciesLbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f, 1f));
                topRow.AddChild(speciesLbl);
            }

            if (!string.IsNullOrEmpty(philosophy))
            {
                var philLbl = new Label { Text = $"\"{philosophy}\"", CustomMinimumSize = new Vector2(110, 0) };
                philLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.75f, 0.5f, 1f));
                topRow.AddChild(philLbl);
            }

            var policyLbl = new Label { Text = tradePolicy, CustomMinimumSize = new Vector2(80, 0) };
            Color policyColor = tradePolicy switch
            {
                "Open"    => new Color(0.3f, 1.0f, 0.3f, 1f),
                "Guarded" => new Color(1.0f, 0.85f, 0.2f, 1f),
                "Closed"  => new Color(1.0f, 0.15f, 0.15f, 1f),
                _         => new Color(0.5f, 0.5f, 0.5f, 1f),
            };
            policyLbl.AddThemeColorOverride("font_color", policyColor);
            topRow.AddChild(policyLbl);

            var tariffPct = (tariffRate * 100f).ToString("F1") + "%";
            var tariffLbl = new Label { Text = $"Tariff: {tariffPct}", CustomMinimumSize = new Vector2(100, 0) };
            topRow.AddChild(tariffLbl);

            var territoryLbl = new Label { Text = $"{territoryCount} systems" };
            territoryLbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            topRow.AddChild(territoryLbl);

            // Second line: produces + needs (pentagon ring dependencies)
            if ((producesArr != null && producesArr.Count > 0) || (needsArr != null && needsArr.Count > 0))
            {
                var econRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                row.AddChild(econRow);

                if (producesArr != null && producesArr.Count > 0)
                {
                    var prodStr = string.Join(", ", producesArr);
                    var prodLbl = new Label { Text = $"  Produces: {prodStr}" };
                    prodLbl.AddThemeColorOverride("font_color", new Color(0.5f, 0.9f, 0.5f, 1f));
                    econRow.AddChild(prodLbl);
                }

                if (needsArr != null && needsArr.Count > 0)
                {
                    var needStr = string.Join(", ", needsArr);
                    var needLbl = new Label { Text = $"  Needs: {needStr}" };
                    needLbl.AddThemeColorOverride("font_color", new Color(1.0f, 0.6f, 0.4f, 1f));
                    econRow.AddChild(needLbl);
                }
            }

            // Reputation bar row
            var repRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddChild(repRow);

            var repLabel = new Label { Text = "Rep:", CustomMinimumSize = new Vector2(40, 0) };
            repRow.AddChild(repLabel);

            // Color-coded reputation bar: red < -25, yellow -25..25, green > 25
            Color repColor;
            if (reputation > 25)
                repColor = new Color(0.3f, 1.0f, 0.3f, 1f);   // green
            else if (reputation >= -25)
                repColor = new Color(1.0f, 0.85f, 0.2f, 1f);  // yellow
            else
                repColor = new Color(1.0f, 0.15f, 0.15f, 1f); // red

            var repBar = new ProgressBar();
            repBar.MinValue = -100;
            repBar.MaxValue = 100;
            repBar.Value = reputation;
            repBar.CustomMinimumSize = new Vector2(200, 20);
            repBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            // Style the bar fill with reputation color
            var fillSb = new StyleBoxFlat();
            fillSb.BgColor = repColor;
            fillSb.SetCornerRadiusAll(3);
            repBar.AddThemeStyleboxOverride("fill", fillSb);

            var bgSb = new StyleBoxFlat();
            bgSb.BgColor = new Color(0.15f, 0.15f, 0.2f, 1f);
            bgSb.SetCornerRadiusAll(3);
            repBar.AddThemeStyleboxOverride("background", bgSb);

            repRow.AddChild(repBar);

            // Reputation label text
            string repText = reputation switch
            {
                >= 75  => "Allied",
                >= 25  => "Friendly",
                >= -25 => "Neutral",
                >= -75 => "Hostile",
                _      => "Enemy",
            };
            var repValLbl = new Label { Text = $"{reputation} ({repText})", CustomMinimumSize = new Vector2(120, 0) };
            repValLbl.AddThemeColorOverride("font_color", repColor);
            repRow.AddChild(repValLbl);

            // Separator between factions
            row.AddChild(new HSeparator());
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GATE.S7.WARFRONT.DASHBOARD_TAB.001: Warfronts tab
    // GATE.S7.WARFRONT.SUPPLY_HUD.001: Supply delivery progress
    // GATE.S7.INSTABILITY.EFFECTS_UI.001: Instability effects per contested node
    // ═══════════════════════════════════════════════════════════════════════
    private Control BuildWarfrontsTab()
    {
        var box = new VBoxContainer();
        box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.SizeFlagsVertical   = SizeFlags.ExpandFill;

        var hdr = new Label { Text = "Warfront  |  Combatants  |  Intensity  |  Type  |  Contested Nodes" };
        hdr.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.9f, 1f));
        box.AddChild(hdr);
        box.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize   = new Vector2(0, 300);

        _warfrontList = new VBoxContainer();
        _warfrontList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _warfrontList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_warfrontList);
        box.AddChild(scroll);

        return box;
    }

    private void RefreshWarfronts()
    {
        ClearChildren(_warfrontList);
        var warfronts = _bridge.GetWarfrontsV0();
        if (warfronts == null || warfronts.Count == 0)
        {
            _warfrontList.AddChild(new Label { Text = "No active warfronts." });
            return;
        }

        foreach (var v in warfronts)
        {
            var d = v.Obj as Dictionary;
            if (d == null) continue;

            var wfId           = GetStr(d, "id");
            var combatantA     = GetStr(d, "combatant_a");
            var combatantB     = GetStr(d, "combatant_b");
            var intensity      = GetInt(d, "intensity");
            var intensityLabel = GetStr(d, "intensity_label");
            var warType        = GetStr(d, "war_type");
            var contestedCount = GetInt(d, "contested_count");

            var row = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _warfrontList.AddChild(row);

            // Top line: combatants + intensity + type + contested
            var topRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.AddChild(topRow);

            var combLbl = new Label { Text = $"{combatantA} vs {combatantB}", CustomMinimumSize = new Vector2(200, 0) };
            combLbl.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 1.0f, 1f));
            topRow.AddChild(combLbl);

            Color intensityColor = intensity switch
            {
                0 => new Color(0.5f, 0.5f, 0.5f, 1f),
                1 => new Color(1.0f, 0.85f, 0.2f, 1f),
                2 => new Color(1.0f, 0.6f, 0.2f, 1f),
                3 => new Color(1.0f, 0.3f, 0.15f, 1f),
                _ => new Color(1.0f, 0.1f, 0.1f, 1f),
            };
            var intLbl = new Label { Text = intensityLabel, CustomMinimumSize = new Vector2(100, 0) };
            intLbl.AddThemeColorOverride("font_color", intensityColor);
            topRow.AddChild(intLbl);

            var typeLbl = new Label { Text = warType, CustomMinimumSize = new Vector2(60, 0) };
            topRow.AddChild(typeLbl);

            var contestedLbl = new Label { Text = $"{contestedCount} contested" };
            contestedLbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            topRow.AddChild(contestedLbl);

            // GATE.S7.WARFRONT.SUPPLY_HUD.001: Supply delivery progress
            var supply = _bridge.GetWarSupplyV0(wfId);
            if (supply != null)
            {
                int progressPct = GetInt(supply, "shift_progress_pct");
                int threshold   = GetInt(supply, "shift_threshold");

                var supplyRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                row.AddChild(supplyRow);

                var supplyLbl = new Label { Text = "Supply:", CustomMinimumSize = new Vector2(60, 0) };
                supplyLbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f, 1f));
                supplyRow.AddChild(supplyLbl);

                var supplyBar = new ProgressBar();
                supplyBar.MinValue = 0;
                supplyBar.MaxValue = 100;
                supplyBar.Value = progressPct;
                supplyBar.CustomMinimumSize = new Vector2(200, 18);
                supplyBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;

                var sFill = new StyleBoxFlat();
                sFill.BgColor = new Color(0.3f, 0.7f, 1.0f, 1f);
                sFill.SetCornerRadiusAll(3);
                supplyBar.AddThemeStyleboxOverride("fill", sFill);
                var sBg = new StyleBoxFlat();
                sBg.BgColor = new Color(0.15f, 0.15f, 0.2f, 1f);
                sBg.SetCornerRadiusAll(3);
                supplyBar.AddThemeStyleboxOverride("background", sBg);
                supplyRow.AddChild(supplyBar);

                var pctLbl = new Label { Text = $"{progressPct}% toward shift ({threshold} units)" };
                supplyRow.AddChild(pctLbl);

                // Show deliveries by good
                var deliveries = supply.ContainsKey("deliveries") ? supply["deliveries"].As<Dictionary>() : null;
                if (deliveries != null && deliveries.Count > 0)
                {
                    var delRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                    row.AddChild(delRow);

                    var delLbl = new Label { Text = "  Deliveries:" };
                    delLbl.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 0.5f, 1f));
                    delRow.AddChild(delLbl);

                    foreach (var key in deliveries.Keys)
                    {
                        var goodId = key.ToString();
                        int qty = 0;
                        try { qty = (int)deliveries[key]; } catch { }
                        var gLbl = new Label { Text = $"  {goodId}: {qty}" };
                        delRow.AddChild(gLbl);
                    }
                }
            }

            // Separator between warfronts
            row.AddChild(new HSeparator());
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════
    private static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
            child.QueueFree();
    }

    private static string GetStr(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return "";
        var v = d[key];
        return v.VariantType == Variant.Type.Nil ? "" : v.ToString();
    }

    private static int GetInt(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return 0;
        try { return (int)d[key]; } catch { return 0; }
    }

    // GATE.S7.AUTOMATION.UI.001: Long variant for credit values.
    private static long GetLong(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return 0;
        try { return (long)d[key]; } catch { return 0; }
    }

    private static float GetFloat(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return 0f;
        try { return (float)d[key]; } catch { return 0f; }
    }

    private static bool GetBool(Dictionary d, string key)
    {
        if (!d.ContainsKey(key)) return false;
        try { return (bool)d[key]; } catch { return false; }
    }

    private static string FormatNum(int n) => n.ToString("N0");
}
