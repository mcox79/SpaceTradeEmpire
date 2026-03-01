#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    public int GetUiStationViewIndex() => _uiStationViewIndex;

    public void SetUiStationViewIndex(int idx)
    {
        _uiStationViewIndex = Math.Clamp(idx, 0, 3);
    }

    public int GetUiDashboardLastSnapshotTick() => _uiDashboardLastSnapshotTick;

    public string GetUiSelectedFleetId() => _uiSelectedFleetId;

    public void SetUiSelectedFleetId(string fleetId)
    {
        _uiSelectedFleetId = fleetId ?? "";
    }
}
