using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Providers;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Behaviors;

public class AnnouncementBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    public async Task SaveData() => await _dataProvider.SaveData();

    private readonly BehaviorDataProvider<AnnouncementBehaviorData> _dataProvider = new(context.Lobby);
    private AnnouncementBehaviorData Data => _dataProvider.Data;

    [BotEvent(BotEventType.Initialize)]
    public void OnInitialize()
    {
        context.TimerProvider.FindOrCreateTimer("NoticeTimer").Start(TimeSpan.FromMinutes(90));
    }
    
    [BotEvent(BotEventType.TimerElapsed, "NoticeTimer")]
    public async Task OnNoticeTimerElapsed()
    {
        await using var dbContext = new BotDbContext();

        // Grabbing all messages won't be a problem, makes life easier.
        var notices = await dbContext.NoticeMessages.ToListAsync();

        if (notices.Count == 0)
        {
            return;
        }
        
        var noticeMessage = notices.FirstOrDefault(x => x.Id == Data.NextNoticeMessageId) ?? notices.First();
        var spamFilter = new string('\u200B', context.Lobby.LobbyConfigurationId);
        
        context.SendMessage($"Notice: {noticeMessage.Message} {spamFilter}");

        var noticeId = notices.IndexOf(noticeMessage) + 1;

        if (noticeId >= notices.Count)
        {
            Data.NextNoticeMessageId = 0;
        }
        else
        {
            Data.NextNoticeMessageId = notices[noticeId].Id;
        }
        
        context.TimerProvider.FindOrCreateTimer("NoticeTimer").Start(TimeSpan.FromMinutes(90));
    }
}