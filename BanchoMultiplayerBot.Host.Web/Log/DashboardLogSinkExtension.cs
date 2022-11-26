using Serilog;
using Serilog.Configuration;

namespace BanchoMultiplayerBot.Host.Web.Log;

public static class DashboardLogSinkExtension
{
    public static LoggerConfiguration DashboardLogSink(
        this LoggerSinkConfiguration loggerConfiguration,
        IFormatProvider formatProvider = null!)
    {
        return loggerConfiguration.Sink(new DashboardLogSink(formatProvider));
    }
}