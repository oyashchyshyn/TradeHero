namespace TradeHero.Core.Types.Settings.AppSettings;

public class DatabaseSettings
{
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabasePassword { get; set; } = string.Empty;
    public string UserFileName { get; set; } = string.Empty;
    public string ConnectionFileName { get; set; } = string.Empty;
    public string StrategyFileName { get; set; } = string.Empty;
}