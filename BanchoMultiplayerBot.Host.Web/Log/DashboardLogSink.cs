using Serilog.Core;
using Serilog.Events;

namespace BanchoMultiplayerBot.Host.Web.Log
{
    public class DashboardLogSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public static List<Tuple<string, LogEventLevel>> Logs = new(); 

        public DashboardLogSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(_formatProvider);

            if (Logs.Count > 512)
                Logs.RemoveAt(Logs.Count - 1);

            Logs.Insert(0, Tuple.Create(message, logEvent.Level));
        }
    }
}
