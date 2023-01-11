using Binance.Net.Enums;

namespace TradeHero.Contracts.Trading.Models.Instance;

public class BaseInstanceOptions
{
    public KlineInterval Interval { get; set; }
    public PositionSide Side { get; set; }
    public int ItemsInTask { get; set; }
    public bool RunImmediately { get; set; }
    public long? TelegramChannelId { get; set; }
    public string? TelegramChannelName { get; set; }
    public bool? TelegramIsNeedToSendMessages { get; set; }
    public List<string> QuoteAssets { get; set; } = new();
    public List<string> BaseAssets { get; set; } = new();
    public List<string> ExcludeAssets { get; set; } = new();
}