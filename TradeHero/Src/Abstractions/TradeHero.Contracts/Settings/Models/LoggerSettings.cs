using Microsoft.Extensions.Logging;

namespace TradeHero.Contracts.Settings.Models;

public class LoggerSettings
{
    public LogLevel LogLevel { get; set; }
    public LogLevel RestClientLogLevel { get; set; }
    public LogLevel SocketClientLogLevel { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string LogTemplate { get; set; } = string.Empty;
}