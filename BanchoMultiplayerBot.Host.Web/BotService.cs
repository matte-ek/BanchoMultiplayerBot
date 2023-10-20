namespace BanchoMultiplayerBot.Host.Web
{
    public class BotService : Bot, IDisposable
    {
        private Task? _botRunTask;

        private bool IsRunning => _botRunTask != null;

        public void Start()
        {
            if (_botRunTask is { IsFaulted: true })
            {
                _botRunTask = null;
            }

            if (IsRunning)
                return;
            if (!HasValidConfiguration())
                return;

            ConfigurationManager.LoadConfiguration();

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
