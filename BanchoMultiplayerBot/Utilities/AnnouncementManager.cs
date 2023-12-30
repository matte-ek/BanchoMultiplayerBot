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
        
        /// <summary>
        /// Sends the specified message in all lobbies, with a unique identifier for each lobby.
        /// </summary>
        public void SendAnnouncementMessage(string message)
        {
            try
            {
                int index = 0;
                
                foreach (var lobby in _bot.Lobbies)
                {
                    // We use the same trick that we use to avoid pinging people in the queue message here,
                    // basically add x amount of zero-width spaces for each lobby, which will make them unique.
                    // We do this to avoid bancho silently dropping the messages. However they still appear 
                    // the same for the user.
                    var uniqueMessage = $"{message[0]}{string.Join("", Enumerable.Repeat("\u200B", index))}{message[1..]}";
                    
                    lobby.SendMessage(uniqueMessage);
                    
                    index++;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while sending announcement: {e.Message}");
            }
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

                        SendAnnouncementMessage(announcement.Message);
                    }
                }
            }
        }
    }
}
