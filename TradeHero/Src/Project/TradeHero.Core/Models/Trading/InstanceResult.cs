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
    public Mood SignalsMood { get; set; }
    public decimal ShortSignalMoodPercent { get; set; }
    public decimal LongsSignalMoodPercent { get; set; }
    public decimal ShortSignalsCount => ShortSignals.Count;
    public decimal LongSignalsCount => LongSignals.Count;
    public List<SymbolMarketInfo> ShortSignals { get; } = new();
    public List<SymbolMarketInfo> LongSignals { get; } = new();
}