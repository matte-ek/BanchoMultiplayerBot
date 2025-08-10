namespace BanchoMultiplayerBot.Behaviors.Data;

public class DynamicRoomBehaviorData
{
    public bool HasPendingUpdate { get; set; } = false;

    public float StarRatingTarget { get; set; } = 5f;
}