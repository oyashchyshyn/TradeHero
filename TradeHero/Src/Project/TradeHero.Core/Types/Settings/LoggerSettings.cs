using Microsoft.Extensions.Logging;

namespace TradeHero.Core.Types.Settings;

public class LoggerSettings
{
    public LogLevel LogLevel { get; set; }
    public LogLevel RestClientLogLevel { get; set; }
    public LogLevel SocketClientLogLevel { get; set; }

    public LoggerInstanceSettings AppInstance { get; set; } = new();
    public LoggerInstanceSettings LauncherInstance { get; set; } = new();
}