using Binance.Net.Enums;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Models.Trading;

public class BaseInstanceOptions
{
    public KlineInterval Interval { get; set; }
    public virtual Market Market { get; set; }
    public int ItemsInTask { get; set; }
    public bool RunImmediately { get; set; }
    public long? TelegramChannelId { get; set; }
    public string? TelegramChannelName { get; set; }
    public bool? TelegramIsNeedToSendMessages { get; set; }
    public List<string> QuoteAssets { get; set; } = new();
    public List<string> BaseAssets { get; set; } = new();
    public List<string> ExcludeAssets { get; set; } = new();
}