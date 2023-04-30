using BanchoMultiplayerBot.Host.Web.Statistics;

namespace BanchoMultiplayerBot.Host.Web
{
    public class BotService : Bot, IDisposable
    {
        private Task? _botRunTask;

        public bool IsRunning { get { return _botRunTask != null; } }

        public BotService() : base("config.json")
        {
        }

        public void Start()
        {
            if (_botRunTask != null && _botRunTask.IsFaulted)
            {
                _botRunTask = null;
            }

            if (IsRunning)
                return;
            if (!HasValidConfiguration())
                return;

            LoadConfiguration("config.json");

            _botRunTask = Task.Run(RunAsync);
        }

        public bool HasValidConfiguration()
        {
            return Configuration.Username.Any() && Configuration.Password.Any() && Configuration.ApiKey.Any();
        }

        public void Dispose()
        {
            // TODO: Exit gracefully
        }
    }
}
