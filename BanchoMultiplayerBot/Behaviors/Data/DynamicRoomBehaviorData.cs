namespace BanchoMultiplayerBot.Behaviors.Data;

public class DynamicRoomBehaviorData
{
    public bool HasPendingUpdate { get; set; } = false;
    
    public float StarRatingMinimum { get; set; } = 0f;
    public float StarRatingMaximum { get; set; } = 10f;
}