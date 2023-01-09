using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace BanchoMultiplayerBot.Host.Web.Log
{
    public class DashboardLogSink : ILogEventSink
    {
        private readonly ITextFormatter _textFormatter;

        public static readonly List<Tuple<string, LogEventLevel>> Logs = new(); 

        public DashboardLogSink(ITextFormatter textFormatter)
        {
            _textFormatter = textFormatter;
        }

        public void Emit(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            
            _textFormatter.Format(logEvent, writer);

            var message = writer.ToString();

            if (Logs.Count > 512)
                Logs.RemoveAt(Logs.Count - 1);

            Logs.Insert(0, Tuple.Create(message, logEvent.Level));
        }
    }
}
