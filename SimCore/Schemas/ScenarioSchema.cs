using System.Collections.Generic;

namespace SimCore.Schemas
{
    /// <summary>
    /// Defines a deterministic simulation run for headless testing.
    /// </summary>
    public class ScenarioDefinition
    {
        public string ScenarioId { get; set; } = "default_scenario";
        public int InitialSeed { get; set; }
        public int StopAtDay { get; set; }
        public List<PlayerCommandEntry> CommandScript { get; set; } = new List<PlayerCommandEntry>();
        public MetricThresholds ExpectedResults { get; set; } = new MetricThresholds();
    }

    public class PlayerCommandEntry
    {
        public int Day { get; set; }
        public string ActionId { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class MetricThresholds
    {
        public double MaxInflation { get; set; } = 1000000.0;
        public double MaxTraceLevel { get; set; } = 1.0;
    }
}