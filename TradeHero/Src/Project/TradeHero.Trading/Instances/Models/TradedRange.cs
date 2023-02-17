using TradeHero.Core.Models.Client;

namespace TradeHero.Trading.Instances.Models;

internal class TradedRange
{
    public int Index { get; init; }
    public decimal StartPrice { get; set; }
    public decimal EndPrice { get; set; }
    public List<BinanceClusterVolume> Clusters { get; } = new();
}