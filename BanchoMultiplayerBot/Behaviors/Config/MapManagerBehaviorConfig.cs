namespace BanchoMultiplayerBot.Behaviors.Config;

public class MapManagerBehaviorConfig
{
    public bool LimitStarRating { get; set; } = true;
    public float MinimumStarRating { get; set; } = 0;
    public float MaximumStarRating { get; set; } = 10;
    
    public float? StarRatingErrorMargin { get; set; } = 0.5f;
    
    public bool LimitMapLength { get; set; } = true;
    public int MinimumMapLength { get; set; } = 30;
    public int MaximumMapLength { get; set; } = 600;
    
    public bool AutomaticallySkipHostViolations { get; set; } = true;
    public int MaximumHostViolations { get; set; } = 3;

    public bool AllowDoubleTime { get; set; } = true;
}