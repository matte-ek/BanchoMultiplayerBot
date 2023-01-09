using Serilog;
using Serilog.Configuration;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace BanchoMultiplayerBot.Host.Web.Log;

public static class DashboardLogSinkExtension
{
    public static LoggerConfiguration DashboardLogSink(
        this LoggerSinkConfiguration loggerConfiguration,
        string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}")
    {
        return loggerConfiguration.Sink(new DashboardLogSink(new MessageTemplateTextFormatter(outputTemplate)));
    }
}