using BanchoMultiplayerBot.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanchoMultiplayerBot
{
    public class AnnouncementManager
    {
        public List<Announcement> Announcements { get; } = new();

        private Bot _bot = null!;
        private bool _exitRequested = false;

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

        public void Exit()
        {
            Save();
            _exitRequested = true;
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
