namespace SimCore.Tweaks;

// GATE.S6.FRACTURE.MARKER_CMD.001: Survey marker tuning constants.
public static class SurveyTweaksV0
{
    // Tech IDs that improve survey accuracy.
    public static string SensorSuiteTechId { get; } = "sensor_suite";
    public static string AdvancedSensorsTechId { get; } = "advanced_sensors";

    // Base resource values by void site family.
    public static int ResourceDepositBase { get; } = 500;
    public static int AsteroidFieldBase { get; } = 300;
    public static int AbandonedStationBase { get; } = 400;
    public static int NebulaRemnantBase { get; } = 200;
    public static int AnomalyRiftBase { get; } = 600;
    public static int DefaultBase { get; } = 250;

    // Resource estimation parameters.
    public static int MinResourceValue { get; } = 10;
    public static int VarianceDivisor { get; } = 2;
    public static int PercentDivisor { get; } = 100;

    // Sensor accuracy noise percentages.
    public static int LowNoisePercent { get; } = 40;     // Level 0: ±40% noise
    public static int MidNoisePercent { get; } = 20;      // Level 1: ±20% noise
    public static int MidSensorLevel { get; } = 1;        // sensor_suite threshold
    public static int ExactSensorLevel { get; } = 2;      // advanced_sensors -> exact
}
