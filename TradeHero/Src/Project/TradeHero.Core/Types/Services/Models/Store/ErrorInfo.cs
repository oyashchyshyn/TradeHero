namespace TradeHero.Core.Types.Services.Models.Store;

public class ErrorInfo
{
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
}