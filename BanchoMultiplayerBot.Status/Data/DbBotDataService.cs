using BanchoMultiplayerBot.Database.Status.Models;
using BanchoMultiplayerBot.Database.Status.Repositories;

namespace BanchoMultiplayerBot.Status.Data
{
    public class DbBotDataService : IDisposable
    {
        private readonly BotSnapshotRepository _snapshotRepository;
        private bool _hasRetrievedData;

        public IReadOnlyList<BotSnapshot>? Data { get; private set; }

        public DbBotDataService(BotSnapshotRepository snapshotRepository)
        {
            _snapshotRepository = snapshotRepository;
        }

        public async Task RetrieveData()
        {
            if (_hasRetrievedData)
                return;

            Data = await _snapshotRepository.GetSnapshots();

            _hasRetrievedData = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _snapshotRepository.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
