using System;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.S6.FRACTURE.MARKER_CMD.001: Player places a survey marker on a void site.
// Transitions site from Unknown/Discovered -> Surveyed.
// Estimates resource value based on sensor tech level.
public sealed class PlaceSurveyMarkerCommand : ICommand
{
    public string VoidSiteId { get; }

    public PlaceSurveyMarkerCommand(string voidSiteId)
    {
        VoidSiteId = voidSiteId;
    }

    public void Execute(SimState state)
    {
        if (state is null) return;
        if (string.IsNullOrEmpty(VoidSiteId)) return;

        if (!state.VoidSites.TryGetValue(VoidSiteId, out var site)) return;

        // Already surveyed — no-op.
        if (site.MarkerState == VoidSiteMarkerState.Surveyed) return;

        site.MarkerState = VoidSiteMarkerState.Surveyed;

        // Estimate resource value deterministically from site properties.
        int sensorLevel = GetSensorTechLevel(state);
        site.EstimatedResourceValue = EstimateResources(site, sensorLevel);
    }

    // GATE.S6.FRACTURE.MARKER_CMD.001: Sensor tech level determines estimation accuracy.
    // 0 = no tech, 1 = sensor_suite, 2+ = advanced.
    private static int GetSensorTechLevel(SimState state)
    {
        int level = 0;
        if (state.Tech?.UnlockedTechIds != null)
        {
            if (state.Tech.UnlockedTechIds.Contains(SurveyTweaksV0.SensorSuiteTechId))
                level++;
            if (state.Tech.UnlockedTechIds.Contains(SurveyTweaksV0.AdvancedSensorsTechId))
                level++;
        }
        return level;
    }

    // Deterministic resource estimate: base value from family + hash variance, scaled by sensor accuracy.
    private static int EstimateResources(VoidSite site, int sensorLevel)
    {
        int baseValue = site.Family switch
        {
            VoidSiteFamily.ResourceDeposit => SurveyTweaksV0.ResourceDepositBase,
            VoidSiteFamily.AsteroidField => SurveyTweaksV0.AsteroidFieldBase,
            VoidSiteFamily.AbandonedStation => SurveyTweaksV0.AbandonedStationBase,
            VoidSiteFamily.NebulaRemnant => SurveyTweaksV0.NebulaRemnantBase,
            VoidSiteFamily.AnomalyRift => SurveyTweaksV0.AnomalyRiftBase,
            _ => SurveyTweaksV0.DefaultBase,
        };

        // Hash-based variance: +/- 50% of base.
        int hash = (site.Id ?? "").GetHashCode() & 0x7FFFFFFF;
        int variance = (hash % (baseValue + 1)) - baseValue / SurveyTweaksV0.VarianceDivisor;
        int trueValue = Math.Max(SurveyTweaksV0.MinResourceValue, baseValue + variance);

        // Sensor accuracy: level 0 -> ±40% noise, level 1 -> ±20%, level 2+ -> exact.
        if (sensorLevel >= SurveyTweaksV0.ExactSensorLevel) return trueValue;

        int noisePct = sensorLevel >= SurveyTweaksV0.MidSensorLevel
            ? SurveyTweaksV0.MidNoisePercent
            : SurveyTweaksV0.LowNoisePercent;
        int noiseRange = trueValue * noisePct / SurveyTweaksV0.PercentDivisor;
        int noise = (hash % (noiseRange * SurveyTweaksV0.VarianceDivisor + 1)) - noiseRange;
        return Math.Max(SurveyTweaksV0.MinResourceValue, trueValue + noise);
    }
}
