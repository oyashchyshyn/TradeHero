namespace TradeHero.Core.Types.Settings.AppSettings;

public class InternetConnectionSettings
{
    public string PingUrl { get; set; } = string.Empty;
    public int PingTimeOutMilliseconds { get; set; }
    public int IterationWaitMilliseconds { get; set; }
    public int ReconnectionAttempts { get; set; }
}