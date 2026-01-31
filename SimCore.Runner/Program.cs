using System;
using System.IO;
using System.Text.Json;
using SimCore;
using SimCore.Schemas;

namespace SimCore.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: SimCore.Runner <scenario.json>");
                Environment.Exit(1);
            }

            string scenarioPath = args[0];
            if (!File.Exists(scenarioPath))
            {
                Console.Error.WriteLine($"Error: Scenario file not found at {scenarioPath}");
                Environment.Exit(1);
            }

            try
            {
                string json = File.ReadAllText(scenarioPath);
                var scenario = JsonSerializer.Deserialize<ScenarioDefinition>(json);

                if (scenario == null)
                {
                    Console.Error.WriteLine("Error: Failed to deserialize scenario definition.");
                    Environment.Exit(1);
                }

                RunScenario(scenario);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CRITICAL FAILURE: {ex.Message}\\n{ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        private static void RunScenario(ScenarioDefinition scenario)
        {
            Console.WriteLine($"--- RUNNING SCENARIO: {scenario.ScenarioId} ---");
            Console.WriteLine($"Seed: {scenario.InitialSeed} | Duration: {scenario.StopAtDay} Days");

            var kernel = new SimKernel(scenario.InitialSeed);
            var startTime = DateTime.UtcNow;

            for (int day = 0; day < scenario.StopAtDay; day++)
            {
                // Future: Inject Player Commands from scenario.CommandScript here
                kernel.Step();
            }

            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"--- COMPLETED in {duration.TotalMilliseconds:F2} ms ---");
            Console.WriteLine($"Final Tick: {kernel.State.Tick}");
        }
    }
}