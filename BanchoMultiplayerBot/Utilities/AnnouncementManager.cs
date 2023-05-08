using BanchoMultiplayerBot.Data;
using Serilog;

namespace BanchoMultiplayerBot.Utilities
{
    public class AnnouncementManager
    {
        public List<Announcement> Announcements { get; } = new();

        private Bot _bot = null!;
        private bool _exitRequested;

        ~AnnouncementManager()
        {
            _exitRequested = true;
        }

        public void Run(Bot bot)
        {
            _bot = bot;

            if (_bot.Configuration.Announcements != null)
                Announcements.AddRange(_bot.Configuration.Announcements);

            Announcements.ForEach(x => x.LastSent = DateTime.Now);

            Task.Run(AnnouncementSenderTask);
        }

        public void Save()
        {
            _bot.Configuration.Announcements = Announcements.ToArray();
        }
        
        private async Task AnnouncementSenderTask()
        {
            while (!_exitRequested)
            {
                await Task.Delay(1000);

                foreach (var announcement in Announcements)
                {
                    if (announcement.Frequency <= 5)
                        continue;
                    
                    if (DateTime.Now >= announcement.LastSent.AddSeconds(announcement.Frequency))
                    {
                        announcement.LastSent = DateTime.Now;

                        await SendAnnouncementMessage(announcement);
                    }
                }
            }
        }

        /// <summary>
        /// Sends the specified message in all lobbies, with a small delay in between all lobbies
        /// </summary>
        private async Task SendAnnouncementMessage(Announcement announcement)
        {
            try
            {
                foreach (var lobby in _bot.Lobbies)
                {
                    lobby.SendMessage(announcement.Message);

                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while sending announcement: {e.Message}");
            }
        }
    }
}
