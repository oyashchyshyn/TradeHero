using Binance.Net.Enums;
using TradeHero.Core.Enums;

namespace TradeHero.Core.Models.Trading;

public class InstanceResult
{
    public KlineInterval Interval { get; init; }
    public DateTime StartFrom { get; set; }
    public DateTime EndTo { get; set; }
    public Market Market { get; set; }
    public PositionSide Side { get; set; }
    public Mood MarketMood { get; set; }
    public decimal ShortMarketMoodPercent { get; set; }
    public decimal LongsMarketMoodPercent { get; set; }
    public List<SymbolMarketInfo> Signals { get; } = new();
}