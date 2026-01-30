namespace SimCore.Entities;

public enum JobStage
{
    EnRouteToSource = 0,
    Loading = 1,
    EnRouteToTarget = 2,
    Unloading = 3
}

public class LogisticsJob
{
    public string GoodId { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public int Quantity { get; set; } = 0;
    public JobStage Stage { get; set; } = JobStage.EnRouteToSource;
}