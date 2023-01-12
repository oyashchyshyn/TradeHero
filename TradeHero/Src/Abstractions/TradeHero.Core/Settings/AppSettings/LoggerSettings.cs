using Microsoft.Extensions.Logging;

namespace TradeHero.Core.Settings.AppSettings;

public class LoggerSettings
{
    public LogLevel LogLevel { get; set; }
    public LogLevel RestClientLogLevel { get; set; }
    public LogLevel SocketClientLogLevel { get; set; }
    public string AppFileName { get; set; } = string.Empty;
    public string LauncherFileName { get; set; } = string.Empty;
    public string LogTemplate { get; set; } = string.Empty;
}