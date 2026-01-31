namespace SimCore.Entities;

public class LogisticsJob
{
    public string GoodId { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    
    // Added for LogisticsSystem compatibility
    public int Amount { get; set; } = 0;
}