namespace SimCore.Entities;

public class Industry
{
    public string Id { get; set; } = "";
    public string RecipeId { get; set; } = "";
    public int Progress { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    // For Slice 2: Simple state tracking.
    // If Progress > 0, we are "working".
}